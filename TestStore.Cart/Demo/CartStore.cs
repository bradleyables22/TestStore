using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TestStore.Shared;

namespace TestStore.Cart.Demo;

public sealed class CartDocument
{
    public Dictionary<string, int> Items { get; set; } = new();
    public List<string> Operations { get; set; } = new();
    public string? Receipt { get; set; }
}

public sealed class CartStore
{
    private readonly BlobContainerClient _container;
    private readonly bool _useAzurite;
    public CartStore(IConfiguration configuration, IHostEnvironment environment)
    {
        var containerUri = configuration["CartStorage:ContainerUri"];
        if (!string.IsNullOrWhiteSpace(containerUri))
        {
            var uri = new Uri(containerUri);
            if (uri.Scheme != "https" || uri.Query != "") throw new InvalidOperationException("CartStorage:ContainerUri must be an HTTPS container URL without SAS.");
            var clientId = configuration["CartStorage:ManagedIdentityClientId"];
            TokenCredential credential = new ManagedIdentityCredential(string.IsNullOrWhiteSpace(clientId)
                ? ManagedIdentityId.SystemAssigned : ManagedIdentityId.FromUserAssignedClientId(clientId));
            _container = new BlobContainerClient(uri, credential);
        }
        else if (environment.IsDevelopment() && configuration["CartStorage:ConnectionString"] == "UseDevelopmentStorage=true")
        {
            _useAzurite = true;
            _container = new BlobContainerClient("UseDevelopmentStorage=true", "teststore-carts");
        }
        else
            throw new InvalidOperationException("Configure CartStorage:ContainerUri for Azure, or CartStorage:ConnectionString=UseDevelopmentStorage=true for local Azurite.");
    }

    public async Task<CartDocument> UpdateAsync(string cartId, Action<CartDocument> update, CancellationToken ct)
    {
        if (!Guid.TryParseExact(cartId, "N", out _)) throw new ArgumentException("Invalid cart ID.");
        // Only create the emulator container automatically; deployed containers are provisioned separately.
        if (_useAzurite) await _container.CreateIfNotExistsAsync(cancellationToken: ct);
        {
            var blob = _container.GetBlobClient(cartId + ".json");
            for (var attempt = 0; attempt < 8; attempt++)
            {
                var document = new CartDocument();
                ETag? etag = null;
                try
                {
                    var download = await blob.DownloadContentAsync(ct);
                    document = download.Value.Content.ToObjectFromJson<CartDocument>() ?? new();
                    etag = download.Value.Details.ETag;
                }
                catch (RequestFailedException ex) when (ex.Status == 404 && ex.ErrorCode == "BlobNotFound") { }
                update(document);
                try
                {
                    await blob.UploadAsync(BinaryData.FromObjectAsJson(document), new BlobUploadOptions
                    {
                        Conditions = etag.HasValue ? new BlobRequestConditions { IfMatch = etag.Value } : new BlobRequestConditions { IfNoneMatch = ETag.All },
                        HttpHeaders = new BlobHttpHeaders { ContentType = "application/json" }
                    }, ct);
                    return document;
                }
                catch (RequestFailedException ex) when (ex.Status is 409 or 412)
                { await Task.Delay(20 * (attempt + 1), ct); }
            }
            throw new InvalidOperationException("The cart is busy. Please try again.");
        }

    }

    public static void Once(CartDocument cart, string? operationId, Action mutation)
    {
        if (!Guid.TryParseExact(operationId, "N", out _)) throw new ArgumentException("Invalid operation ID.");
        if (cart.Operations.Contains(operationId!)) return;
        mutation();
        cart.Operations.Add(operationId!);
        // Bounded for this throwaway demo; use an enduring idempotency record for real purchases.
        if (cart.Operations.Count > 200) cart.Operations.RemoveAt(0);
    }
}
