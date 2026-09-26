using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MaxionAssignment.Backend;
using UnityEngine;
using VContainer.Unity;

namespace MaxionAssignment.Shop
{
    // Loads the product list with GET /api/products and shows it in ShopView.
    // Products fetched during Initialize are shown right away from ProductCatalog, then refreshed from the server.
    // The refresh button fetches again. The latest list is written back to ProductCatalog.
    public class ShopPresenter : IStartable, IDisposable
    {
        readonly ShopView view;
        readonly BackendApiClient api;
        readonly ProductCatalog catalog;
        readonly CancellationTokenSource cts = new();
        bool busy;

        public ShopPresenter(ShopView view, BackendApiClient api, ProductCatalog catalog)
        {
            this.view = view;
            this.api = api;
            this.catalog = catalog;
        }

        public void Start()
        {
            if (view.RefreshButton != null)
                view.RefreshButton.onClick.AddListener(OnRefreshClicked);

            if (catalog.Products.Count > 0)
                view.ShowProducts(catalog.Products);

            LoadProductsAsync().Forget();
        }

        public void Dispose()
        {
            cts.Cancel();
            cts.Dispose();
        }

        void OnRefreshClicked() => LoadProductsAsync().Forget();

        async UniTaskVoid LoadProductsAsync()
        {
            if (busy) return;
            busy = true;
            view.SetInteractable(false);
            view.ShowInfo("Loading products...");

            try
            {
                catalog.Set(await api.GetProductsAsync(cts.Token));
                view.ShowProducts(catalog.Products);
                view.ShowInfo(catalog.Products.Count == 0 ? "No products available." : "");
                Debug.Log($"[Shop] Loaded {catalog.Products.Count} products");
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (BackendException e)
            {
                // Keep whatever list is already on screen so a failed refresh doesn't empty the shop.
                view.ShowError(ToMessage(e));
                Debug.LogWarning($"[Shop] Loading products failed: {e.Message}");
            }

            busy = false;
            view.SetInteractable(true);
        }

        static string ToMessage(BackendException e)
        {
            if (e.IsNetworkError) return "Cannot connect to the server.";
            if (e.Status >= 500) return "Server error. Please try again.";
            return $"Could not load products ({e.Code}).";
        }
    }
}
