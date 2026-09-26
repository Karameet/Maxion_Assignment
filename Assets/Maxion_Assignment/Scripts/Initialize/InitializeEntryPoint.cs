using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MaxionAssignment.Backend;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace MaxionAssignment.Initialize
{
    // Boot flow for the Initialize scene:
    // 1. GET /healthz (with retry) to make sure the server and DB are up
    // 2. GET /api/products into ProductCatalog
    // 3. Load the next scene, if one is set
    // Sign-in, including resuming a saved token, happens on the Login scene.
    public class InitializeEntryPoint : IAsyncStartable
    {
        readonly BackendApiClient api;
        readonly ProductCatalog catalog;
        readonly InitializeSettings settings;
        readonly BackendConfig config;

        public InitializeEntryPoint(BackendApiClient api, ProductCatalog catalog,
            InitializeSettings settings, BackendConfig config)
        {
            this.api = api;
            this.catalog = catalog;
            this.settings = settings;
            this.config = config;
        }

        public async UniTask StartAsync(CancellationToken cancellation = default)
        {
            try
            {
                await WaitForServerAsync(cancellation);

                catalog.Set(await api.GetProductsAsync(cancellation));
                Debug.Log($"[Initialize] Loaded {catalog.Products.Count} products");

                if (!string.IsNullOrEmpty(settings.NextSceneName))
                    await SceneManager.LoadSceneAsync(settings.NextSceneName).ToUniTask(cancellationToken: cancellation);
            }
            catch (OperationCanceledException)
            {
            }
            catch (BackendException e)
            {
                Debug.LogError($"[Initialize] Backend connection failed: {e.Message}");
            }
        }

        async UniTask WaitForServerAsync(CancellationToken ct)
        {
            for (var attempt = 1; ; attempt++)
            {
                Debug.Log($"[Initialize] Checking {config.BaseUrl}/healthz ({attempt}/{settings.HealthCheckAttempts})");
                var startedAt = Time.realtimeSinceStartup;
                try
                {
                    var health = await api.GetHealthAsync(ct);
                    var elapsedMs = (Time.realtimeSinceStartup - startedAt) * 1000f;
                    Debug.Log($"[Initialize] /healthz status=\"{health.status}\" in {elapsedMs:0} ms");
                    return;
                }
                catch (BackendException e) when (e.IsRetryable && attempt < settings.HealthCheckAttempts)
                {
                    Debug.LogWarning($"[Initialize] Health check {attempt}/{settings.HealthCheckAttempts} failed: {e.Message}");
                    await UniTask.Delay(TimeSpan.FromSeconds(settings.HealthCheckRetryDelaySeconds), cancellationToken: ct);
                }
            }
        }
    }
}
