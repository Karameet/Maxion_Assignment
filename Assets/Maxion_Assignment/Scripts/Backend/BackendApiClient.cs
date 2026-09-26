using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace MaxionAssignment.Backend
{
    // Thin wrapper over the Order Backend REST API (see Maxion_Assignment_Backend/Docs/api-reference.md).
    // Every failure surfaces as BackendException with the HTTP status and the server's error code.
    public class BackendApiClient
    {
        readonly BackendConfig config;
        readonly PlayerSession session;

        public BackendApiClient(BackendConfig config, PlayerSession session)
        {
            this.config = config;
            this.session = session;
        }

        public async UniTask<HealthResponse> GetHealthAsync(CancellationToken ct = default)
        {
            var res = await SendAsync("GET", "/healthz", null, false, null, ct);
            return Parse<HealthResponse>(res);
        }

        public async UniTask<AuthResponse> GuestLoginAsync(string deviceId, CancellationToken ct = default)
        {
            var body = JsonUtility.ToJson(new GuestLoginRequest { deviceId = deviceId });
            var res = await SendAsync("POST", "/api/auth/guest", body, false, null, ct);
            var auth = Parse<AuthResponse>(res);
            session.Apply(auth);
            return auth;
        }

        public async UniTask<AuthResponse> RegisterAsync(string email, string password, CancellationToken ct = default)
        {
            var body = JsonUtility.ToJson(new EmailCredentialsRequest { email = email, password = password });
            var res = await SendAsync("POST", "/api/auth/register", body, false, null, ct);
            var auth = Parse<AuthResponse>(res);
            session.Apply(auth);
            return auth;
        }

        public async UniTask<AuthResponse> LoginAsync(string email, string password, CancellationToken ct = default)
        {
            var body = JsonUtility.ToJson(new EmailCredentialsRequest { email = email, password = password });
            var res = await SendAsync("POST", "/api/auth/login", body, false, null, ct);
            var auth = Parse<AuthResponse>(res);
            session.Apply(auth);
            return auth;
        }

        // Links email/password to the current guest account. The server returns a new token that replaces the guest one.
        public async UniTask<AuthResponse> LinkEmailAsync(string email, string password, CancellationToken ct = default)
        {
            var body = JsonUtility.ToJson(new EmailCredentialsRequest { email = email, password = password });
            var res = await SendAsync("POST", "/api/auth/link", body, true, null, ct);
            var auth = Parse<AuthResponse>(res);
            session.Apply(auth);
            return auth;
        }

        public async UniTask<MeResponse> GetMeAsync(CancellationToken ct = default)
        {
            var res = await SendAsync("GET", "/api/me", null, true, null, ct);
            var me = Parse<MeResponse>(res);
            session.Apply(me);
            return me;
        }

        public async UniTask<List<Product>> GetProductsAsync(CancellationToken ct = default)
        {
            var res = await SendAsync("GET", "/api/products", null, false, null, ct);
            return Parse<ProductListResponse>(res).products ?? new List<Product>();
        }

        // Create one idempotency key per purchase click (NewIdempotencyKey) and reuse it when retrying
        // after a network error or 5xx. Replayed is true when the server returned an order it already created.
        public async UniTask<(OrderResponse Order, bool Replayed)> CreateOrderAsync(
            string idempotencyKey, string productId, int quantity, CancellationToken ct = default)
        {
            var body = JsonUtility.ToJson(new CreateOrderRequest { productId = productId, quantity = quantity });
            var headers = new Dictionary<string, string> { ["Idempotency-Key"] = idempotencyKey };
            var res = await SendAsync("POST", "/api/orders", body, true, headers, ct);
            var replayed = string.Equals(res.GetHeader("Idempotent-Replayed"), "true", StringComparison.OrdinalIgnoreCase);
            return (Parse<OrderResponse>(res), replayed);
        }

        public async UniTask<List<OrderHistoryItem>> GetOrdersAsync(CancellationToken ct = default)
        {
            var res = await SendAsync("GET", "/api/orders", null, true, null, ct);
            return Parse<OrderListResponse>(res).orders ?? new List<OrderHistoryItem>();
        }

        public static string NewIdempotencyKey() => Guid.NewGuid().ToString();

        readonly struct RawResponse
        {
            public readonly long Status;
            public readonly string Text;
            readonly Dictionary<string, string> headers;

            public RawResponse(long status, string text, Dictionary<string, string> headers)
            {
                Status = status;
                Text = text;
                this.headers = headers;
            }

            public string GetHeader(string name)
            {
                if (headers == null) return null;
                foreach (var pair in headers)
                {
                    if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
                        return pair.Value;
                }
                return null;
            }
        }

        async UniTask<RawResponse> SendAsync(string method, string path, string jsonBody, bool authorized,
            Dictionary<string, string> extraHeaders, CancellationToken ct)
        {
            using var req = new UnityWebRequest(config.BaseUrl + path, method);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.timeout = config.TimeoutSeconds;

            if (jsonBody != null)
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
                req.SetRequestHeader("Content-Type", "application/json");
            }

            if (authorized)
                req.SetRequestHeader("Authorization", "Bearer " + session.Token);

            if (extraHeaders != null)
            {
                foreach (var pair in extraHeaders)
                    req.SetRequestHeader(pair.Key, pair.Value);
            }

            try
            {
                await req.SendWebRequest().WithCancellation(ct);
            }
            catch (UnityWebRequestException)
            {
                // UniTask throws on any non-success result. Inspect the request below instead.
            }

            if (req.result == UnityWebRequest.Result.ConnectionError)
                throw new BackendException(0, BackendException.NetworkError, req.error);

            var text = req.downloadHandler?.text ?? "";
            if (req.result == UnityWebRequest.Result.Success && req.responseCode >= 200 && req.responseCode < 300)
                return new RawResponse(req.responseCode, text, req.GetResponseHeaders());

            var error = ParseError(text);
            var code = string.IsNullOrEmpty(error?.code) ? BackendException.Unknown : error.code;
            var message = string.IsNullOrEmpty(error?.message) ? req.error : error.message;
            throw new BackendException(req.responseCode, code, message);
        }

        static T Parse<T>(RawResponse res)
        {
            try
            {
                return JsonUtility.FromJson<T>(res.Text);
            }
            catch (ArgumentException e)
            {
                throw new BackendException(res.Status, BackendException.Unknown, "invalid response body: " + e.Message);
            }
        }

        // A proxy or load balancer can return a non-JSON body (e.g. an HTML 502 page), which JsonUtility throws on.
        static ApiErrorDetail ParseError(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            try
            {
                return JsonUtility.FromJson<ApiErrorResponse>(text)?.error;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
