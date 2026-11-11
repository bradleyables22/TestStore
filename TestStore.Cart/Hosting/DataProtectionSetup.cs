using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TestStore.Cart.Hosting
{
	internal static class DataProtectionSetup
	{
		// All replicas of this app must use the same key store and application name.
		// Independent apps/environments should use separate stores and names.
		public static void AddApplicationDataProtection(this IServiceCollection services,
			IConfiguration configuration, IHostEnvironment environment)
		{
			var protection = services.AddDataProtection().SetApplicationName(
				configuration["DataProtection:ApplicationName"] ?? "TestStore.Cart");
			var blobUri = configuration["DataProtection:BlobUri"];
			var directory = configuration["DataProtection:KeyDirectory"];
			if (!string.IsNullOrWhiteSpace(blobUri) && !string.IsNullOrWhiteSpace(directory))
				throw new InvalidOperationException("Configure either DataProtection:BlobUri or DataProtection:KeyDirectory, not both.");

			if (!string.IsNullOrWhiteSpace(blobUri))
			{
				if (!Uri.TryCreate(blobUri, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
					!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) || !string.IsNullOrEmpty(uri.UserInfo))
					throw new InvalidOperationException("DataProtection:BlobUri must be an HTTPS blob URL without a SAS token, query, or fragment. Access uses Azure identity.");
				var clientId = configuration["DataProtection:ManagedIdentityClientId"];
				TokenCredential credential = environment.IsDevelopment()
					? new DefaultAzureCredential()
					: string.IsNullOrWhiteSpace(clientId)
						? new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned)
						: new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(clientId));
				protection.PersistKeysToAzureBlobStorage(uri, credential);
			}
			else if (!string.IsNullOrWhiteSpace(directory))
			{
				// This must be a persistent shared mount when the app has multiple instances.
				protection.PersistKeysToFileSystem(new DirectoryInfo(directory));
			}
			// With neither setting, use ASP.NET Core's host-specific defaults. No Azure access is
			// required locally. Verify that the deployed hosting plan provides shared persistent keys.
		}
	}
}
