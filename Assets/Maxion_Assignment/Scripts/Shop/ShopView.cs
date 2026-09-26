using System;
using System.Collections.Generic;
using MaxionAssignment.Backend;
using MaxionAssignment.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MaxionAssignment.Shop
{
    // Passive view for the shop page. ShopPresenter feeds it the product list and drives the status text.
    // PurchasePresenter listens to ProductClicked to open the purchase popup.
    public class ShopView : MonoBehaviour
    {
        [Header("List")]
        [SerializeField] RectTransform listContent;
        [Tooltip("ProductItemView prefab, cloned once per product. A scene object also works if it is kept inactive.")]
        [SerializeField] ProductItemView itemTemplate;

        [Header("Controls")]
        [SerializeField] Button refreshButton;
        [SerializeField] TMP_Text statusText;
        [SerializeField] Color infoColor = new(0.85f, 0.88f, 0.95f);
        [SerializeField] Color errorColor = new(1f, 0.45f, 0.45f);

        readonly List<ProductItemView> items = new();

        public Button RefreshButton => refreshButton;

        public event Action<Product> ProductClicked;

        void Awake()
        {
            if (refreshButton != null)
                refreshButton.AddClickAnimation();
        }

        public void ShowProducts(IReadOnlyList<Product> products)
        {
            ClearProducts();
            foreach (var product in products)
            {
                var item = Instantiate(itemTemplate, listContent);
                item.name = $"Product_{product.id}";
                item.gameObject.SetActive(true);
                item.Bind(product);
                item.Clicked += OnItemClicked;
                items.Add(item);
            }
        }

        public void ClearProducts()
        {
            foreach (var item in items)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
            items.Clear();
        }

        void OnItemClicked(ProductItemView item) => ProductClicked?.Invoke(item.Product);

        public void SetInteractable(bool interactable)
        {
            if (refreshButton != null)
                refreshButton.interactable = interactable;
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
