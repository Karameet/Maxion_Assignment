using MaxionAssignment.Backend;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace MaxionAssignment.Initialize
{
    // Root scope for the backend services. It survives scene loads so later scenes can use it as their
    // LifetimeScope parent and resolve BackendApiClient, PlayerSession, and ProductCatalog.
    public class InitializeLifetimeScope : LifetimeScope
    {
        [SerializeField] BackendConfig backendConfig = new();
        [SerializeField] InitializeSettings initializeSettings = new();

        protected override void Awake()
        {
            DontDestroyOnLoad(gameObject);
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(backendConfig);
            builder.RegisterInstance(initializeSettings);

            builder.Register<PlayerSession>(Lifetime.Singleton);
            builder.Register<ProductCatalog>(Lifetime.Singleton);
            builder.Register<BackendApiClient>(Lifetime.Singleton);

            builder.RegisterEntryPoint<InitializeEntryPoint>();
        }
    }
}
