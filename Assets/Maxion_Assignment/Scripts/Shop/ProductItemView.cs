using System;
using System.Globalization;
using MaxionAssignment.Backend;
using MaxionAssignment.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MaxionAssignment.Shop
{
    // One row in the shop list. ShopView clones the ProductItemView prefab for every product.
    // Clicking the row raises Clicked, which ShopView forwards as ProductClicked to open the purchase popup.
    public class ProductItemView : MonoBehaviour
    {
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text priceText;
        [Tooltip("Button on this row. Uses the Button on this GameObject if left empty.")]
        [SerializeField] Button button;
        [Tooltip("string.Format pattern for the price. {0} is the price as a double.")]
        [SerializeField] string priceFormat = "{0:N2}";

        public Product Product { get; private set; }

        public event Action<ProductItemView> Clicked;

        void Awake()
        {
            if (button == null && !TryGetComponent(out button))
            {
                Debug.LogWarning($"[Shop] {name} has no Button, so it can't be clicked.", this);
                return;
            }

            button.AddClickAnimation();
            button.onClick.AddListener(() => Clicked?.Invoke(this));
        }

        public void Bind(Product product)
        {
            Product = product;
            nameText.text = product.name;
            priceText.text = string.Format(CultureInfo.InvariantCulture, priceFormat, product.price);
        }
    }
}
