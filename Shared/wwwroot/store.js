// Heimdall owns transport, storage commands, payload binding, and HTML swaps.
// These helpers only prepare the next page's handoff and order full-page navigation.
(() => {
    const key = "teststore.pending-cart";
    function showError(text) {
        const message = document.getElementById("request-error");
        if (message) { message.hidden = false; message.textContent = text; }
    }
    window.Store = {
        prepareCartLoad(request) {
            const element = document.getElementById("store-region");
            try {
                let pending;
                try { pending = JSON.parse(sessionStorage.getItem(key)); } catch { pending = null; }
                // Never let stale storage replace the cart identity supplied by AppHost.
                const payload = pending?.cartId === request.cartId
                    ? { ...request, productId: pending.productId, operationId: pending.operationId }
                    : request;
                sessionStorage.setItem(key, JSON.stringify(payload));
                element.removeAttribute("heimdall-payload");
                element.setAttribute("heimdall-payload-from", "session-storage:" + key);
            } catch {
                // Existing-cart reads still work using the inline server payload if storage is unavailable.
            }
        },
        openCart(route, operationId) {
            try {
                const pending = JSON.parse(sessionStorage.getItem(key));
                if (!pending || pending.operationId !== operationId) throw new Error("Handoff was not saved.");
                if (!/^\/(?:[A-Za-z0-9_-]+\/?)*$/.test(route)) throw new Error("Invalid shell route.");
                window.location.assign(route);
            } catch {
                showError("Your browser couldn't save the selection for the next page. Allow session storage, then try again.");
            }
        }
    };
    document.addEventListener("heimdall:error", () => showError("We couldn't reach this part of the store. Check that its backend is running, then refresh to try again."));
})();
