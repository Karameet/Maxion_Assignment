using MaxionAssignment.Initialize;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace MaxionAssignment.Shop
{
    // BackendApiClient and ProductCatalog come from InitializeLifetimeScope, so the Shop scene has to be
    // loaded from Initialize. Playing Shop.unity on its own fails with a parent-not-found error.
    public class ShopLifetimeScope : LifetimeScope
    {
        [Tooltip("Found in the scene automatically if left empty.")]
        [SerializeField] ShopView view;
        [Tooltip("Found in the scene automatically if left empty. Usually starts inactive.")]
        [SerializeField] PurchasePopupView purchasePopup;

        protected override void Awake()
        {
            parentReference = ParentReference.Create<InitializeLifetimeScope>();
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            if (view == null)
                view = FindFirstObjectByType<ShopView>(FindObjectsInactive.Include);

            if (purchasePopup == null)
                purchasePopup = FindFirstObjectByType<PurchasePopupView>(FindObjectsInactive.Include);

            builder.RegisterComponent(view);
            builder.RegisterComponent(purchasePopup);
            builder.RegisterEntryPoint<ShopPresenter>();
            builder.RegisterEntryPoint<PurchasePresenter>();
        }
    }
}
