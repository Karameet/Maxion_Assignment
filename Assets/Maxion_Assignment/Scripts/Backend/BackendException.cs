using System;

namespace MaxionAssignment.Backend
{
    // Branch on Status + Code only. Message is human-readable and may change on the server.
    public class BackendException : Exception
    {
        public const string NetworkError = "NETWORK_ERROR";
        public const string Unknown = "UNKNOWN";

        public long Status { get; }
        public string Code { get; }

        public BackendException(long status, string code, string message)
            : base($"[{status}] {code}: {message}")
        {
            Status = status;
            Code = code;
        }

        public bool IsNetworkError => Status == 0;
        public bool IsUnauthorized => Status == 401;

        // Network errors and 5xx are safe to retry (for orders: with the same Idempotency-Key).
        public bool IsRetryable => Status == 0 || Status >= 500;
    }
}
