using System;
using UnityEngine;

namespace MaxionAssignment.Initialize
{
    [Serializable]
    public class InitializeSettings
    {
        [Tooltip("Scene to load once the backend is connected. Leave empty to stay in Initialize.")]
        [SerializeField] string nextSceneName = "";
        [SerializeField, Min(1)] int healthCheckAttempts = 3;
        [SerializeField, Min(0f)] float healthCheckRetryDelaySeconds = 1f;

        public string NextSceneName => nextSceneName;
        public int HealthCheckAttempts => healthCheckAttempts;
        public float HealthCheckRetryDelaySeconds => healthCheckRetryDelaySeconds;
    }
}
