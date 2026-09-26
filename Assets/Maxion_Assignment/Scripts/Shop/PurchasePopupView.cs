using System;
using System.Globalization;
using MaxionAssignment.Backend;
using MaxionAssignment.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MaxionAssignment.Shop
{
    // Popup that shows one product and lets the player pick a quantity before buying.
    // The view owns the quantity (clamped to minQuantity..maxQuantity) and raises QuantityChanged.
    // PurchasePresenter wires the buy/close buttons and sends the order.
    public class PurchasePopupView : PopupView
    {
        [Header("Product")]
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text priceText;
        [SerializeField] TMP_Text totalText;
        [Tooltip("string.Format pattern for prices. {0} is the price as a double.")]
        [SerializeField] string priceFormat = "{0:N2}";

        [Header("Quantity")]
        [SerializeField] TMP_InputField quantityInput;
        [SerializeField] Button minusButton;
        [SerializeField] Button plusButton;
        [SerializeField, Min(1)] int minQuantity = 1;
        [SerializeField, Min(1)] int maxQuantity = 99;

        [Header("Actions")]
        [SerializeField] Button buyButton;
        [SerializeField] Button closeButton;
        [SerializeField] TMP_Text statusText;
        [SerializeField] Color infoColor = new(0.85f, 0.88f, 0.95f);
        [SerializeField] Color errorColor = new(1f, 0.45f, 0.45f);
        [Tooltip("Buying always shows the loading state for at least this long, even if the server answers sooner.")]
        [SerializeField, Min(0f)] float minimumLoadingSeconds = 1f;

        int quantity = 1;
        bool locked;

        public Button BuyButton => buyButton;
        public Button CloseButton => closeButton;
        public int Quantity => quantity;
        public float MinimumLoadingSeconds => minimumLoadingSeconds;

        public event Action<int> QuantityChanged;

        protected override void Awake()
        {
            base.Awake();

            buyButton.AddClickAnimation();
            closeButton.AddClickAnimation();
            minusButton.AddClickAnimation();
            plusButton.AddClickAnimation();

            minusButton.onClick.AddListener(() => SetQuantity(quantity - 1));
            plusButton.onClick.AddListener(() => SetQuantity(quantity + 1));
            quantityInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            quantityInput.onEndEdit.AddListener(OnQuantityEdited);
        }

        // Fills in the product, resets the quantity and status, and opens the popup.
        public void Open(Product product)
        {
            nameText.text = product.name;
            priceText.text = "Price: " + FormatPrice(product.price);
            SetInteractable(true);
            ShowInfo("");
            SetQuantity(minQuantity);
            Show();
        }

        public void SetQuantity(int value)
        {
            quantity = Mathf.Clamp(value, minQuantity, Mathf.Max(minQuantity, maxQuantity));
            quantityInput.SetTextWithoutNotify(quantity.ToString(CultureInfo.InvariantCulture));
            RefreshQuantityButtons();
            QuantityChanged?.Invoke(quantity);
        }

        // Anything that isn't a number (e.g. an empty field) falls back to the current quantity.
        void OnQuantityEdited(string text)
        {
            SetQuantity(int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : quantity);
        }

        public void ShowTotal(double total) => totalText.text = "Total: " + FormatPrice(total);

        public string FormatPrice(double price) => string.Format(CultureInfo.InvariantCulture, priceFormat, price);

        public void SetInteractable(bool interactable)
        {
            locked = !interactable;
            quantityInput.interactable = interactable;
            buyButton.interactable = interactable;
            closeButton.interactable = interactable;
            RefreshQuantityButtons();
        }

        void RefreshQuantityButtons()
        {
            minusButton.interactable = !locked && quantity > minQuantity;
            plusButton.interactable = !locked && quantity < maxQuantity;
        }

        public void ShowInfo(string message) => SetStatus(message, infoColor);
        public void ShowError(string message) => SetStatus(message, errorColor);

        void SetStatus(string message, Color color)
        {
            statusText.text = message;
            statusText.color = color;
        }
    }
}
