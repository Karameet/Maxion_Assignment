using MaxionAssignment.Initialize;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace MaxionAssignment.Login
{
    // BackendApiClient and PlayerSession come from InitializeLifetimeScope, so the Login scene has to be
    // loaded from Initialize. Playing Login.unity on its own fails with a parent-not-found error.
    public class LoginLifetimeScope : LifetimeScope
    {
        [Tooltip("Found in the scene automatically if left empty.")]
        [SerializeField] LoginView view;
        [Tooltip("Found in the scene automatically if left empty. Usually starts inactive.")]
        [SerializeField] RegisterView registerView;
        [SerializeField] LoginSettings settings = new();

        protected override void Awake()
        {
            parentReference = ParentReference.Create<InitializeLifetimeScope>();
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            if (view == null)
                view = FindFirstObjectByType<LoginView>(FindObjectsInactive.Include);
            if (registerView == null)
                registerView = FindFirstObjectByType<RegisterView>(FindObjectsInactive.Include);

            builder.RegisterInstance(settings);
            builder.RegisterComponent(view);
            builder.RegisterComponent(registerView);
            builder.RegisterEntryPoint<LoginPresenter>();
            builder.RegisterEntryPoint<RegisterPresenter>();
        }
    }
}
