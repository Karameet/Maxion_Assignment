using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MaxionAssignment.Backend;
using UnityEngine;
using VContainer.Unity;

namespace MaxionAssignment.Shop
{
    // Opens PurchasePopupView when a product row is clicked and buys it with POST /api/orders.
    // One idempotency key covers one purchase. It is reused when the player presses Buy again after a
    // network error or 5xx, so a retry can't create a second order. Changing the quantity, opening another
    // product, a 4xx error, or a successful purchase starts a new key.
    public class PurchasePresenter : IStartable, IDisposable
    {
        readonly ShopView shopView;
        readonly PurchasePopupView popup;
        readonly BackendApiClient api;
        readonly CancellationTokenSource cts = new();
        Product product;
        string idempotencyKey;
        bool busy;

        public PurchasePresenter(ShopView shopView, PurchasePopupView popup, BackendApiClient api)
        {
            this.shopView = shopView;
            this.popup = popup;
            this.api = api;
        }

        public void Start()
        {
            shopView.ProductClicked += OnProductClicked;
            popup.QuantityChanged += OnQuantityChanged;
            popup.BuyButton.onClick.AddListener(OnBuyClicked);
            popup.CloseButton.onClick.AddListener(OnCloseClicked);
        }

        public void Dispose()
        {
            shopView.ProductClicked -= OnProductClicked;
            popup.QuantityChanged -= OnQuantityChanged;
            cts.Cancel();
            cts.Dispose();
        }

        void OnProductClicked(Product clicked)
        {
            if (busy) return;
            product = clicked;
            idempotencyKey = null;
            popup.Open(clicked);
        }

        void OnQuantityChanged(int quantity)
        {
            idempotencyKey = null;
            if (product != null)
                popup.ShowTotal(product.price * quantity);
        }

        void OnCloseClicked()
        {
            if (busy) return;
            popup.Hide();
        }

        void OnBuyClicked() => BuyAsync().Forget();

        async UniTaskVoid BuyAsync()
        {
            if (busy || product == null) return;
            busy = true;
            popup.SetInteractable(false);
            popup.ShowInfo("Processing your order...");

            idempotencyKey ??= BackendApiClient.NewIdempotencyKey();
            var quantity = popup.Quantity;
            var ct = cts.Token;
            var startedAt = Time.realtimeSinceStartup;

            OrderResponse order = null;
            var replayed = false;
            BackendException error = null;
            try
            {
                (order, replayed) = await api.CreateOrderAsync(idempotencyKey, product.id, quantity, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (BackendException e)
            {
                error = e;
            }

            // Fake loading: keep the "processing" state up for at least MinimumLoadingSeconds, counted from the
            // Buy click, so a fast server doesn't make the purchase flash by. Applies to errors too.
            var remaining = popup.MinimumLoadingSeconds - (Time.realtimeSinceStartup - startedAt);
            if (remaining > 0f &&
                await UniTask.Delay(TimeSpan.FromSeconds(remaining), DelayType.Realtime, cancellationToken: ct)
                    .SuppressCancellationThrow())
                return;

            busy = false;
            popup.SetInteractable(true);

            if (error != null)
            {
                // The server rejected this request outright, so a new attempt should be a new purchase.
                if (!error.IsRetryable)
                    idempotencyKey = null;
                popup.ShowError(ToMessage(error));
                Debug.LogWarning($"[Shop] Purchase failed: {error.Message}");
                return;
            }

            idempotencyKey = null;
            Debug.Log($"[Shop] Order {order.id}: {order.quantity} x {product.name}, total {order.total}" +
                      (replayed ? " (replayed)" : ""));

            popup.Hide();
            shopView.ShowInfo($"Purchased {order.quantity} x {product.name} for {popup.FormatPrice(order.total)}.");
        }

        static string ToMessage(BackendException e)
        {
            if (e.IsNetworkError) return "Cannot connect to the server. Press Buy to try again.";
            if (e.IsUnauthorized) return "Your session has expired. Please log in again.";
            if (e.Status == 404) return "This product is no longer available.";
            if (e.Status == 400) return "Invalid quantity.";
            if (e.Status >= 500) return "Server error. Press Buy to try again.";
            return $"Purchase failed ({e.Code}).";
        }
    }
}
