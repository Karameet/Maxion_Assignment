using System;
using UnityEngine;

namespace MaxionAssignment.Backend
{
    [Serializable]
    public class BackendConfig
    {
        // Editor / PC: http://localhost:8080 · Android Emulator: http://10.0.2.2:8080 · Mobile on LAN: http://<server-ip>:8080
        [SerializeField] string baseUrl = "http://localhost:8080";
        [SerializeField, Min(1)] int timeoutSeconds = 10;

        public string BaseUrl => baseUrl.TrimEnd('/');
        public int TimeoutSeconds => timeoutSeconds;
    }
}
