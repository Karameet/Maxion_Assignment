using System;
using UnityEngine;

namespace MaxionAssignment.Login
{
    [Serializable]
    public class LoginSettings
    {
        [Tooltip("Scene to load after a successful login. Must be in the build's scene list.")]
        [SerializeField] string nextSceneName = "Shop";

        public string NextSceneName => nextSceneName;
    }
}
