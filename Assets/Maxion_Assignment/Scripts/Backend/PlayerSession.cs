using System;
using System.Globalization;
using UnityEngine;

namespace MaxionAssignment.Backend
{
    // Holds the logged-in account and persists the JWT in PlayerPrefs so the next launch can skip login.
    public class PlayerSession
    {
        const string DeviceIdKey = "order_api_device_id";
        const string TokenKey = "order_api_token";
        const string ExpiresAtKey = "order_api_token_expires_at";

        // Treat the token as expired a bit early so it doesn't run out mid-session.
        static readonly TimeSpan ExpiryMargin = TimeSpan.FromMinutes(5);

        public string Token { get; private set; }
        public DateTime ExpiresAtUtc { get; private set; }
        public string UserId { get; private set; }
        public string AccountType { get; private set; }
        public string Email { get; private set; }

        public bool HasValidToken => !string.IsNullOrEmpty(Token) && DateTime.UtcNow + ExpiryMargin < ExpiresAtUtc;
        public bool IsGuest => AccountType == "guest";

        public PlayerSession()
        {
            Token = PlayerPrefs.GetString(TokenKey, "");
            ExpiresAtUtc = ParseUtc(PlayerPrefs.GetString(ExpiresAtKey, ""));
        }

        public void Apply(AuthResponse auth)
        {
            Token = auth.token;
            ExpiresAtUtc = ParseUtc(auth.expiresAt);
            UserId = auth.userId;
            AccountType = auth.accountType;

            PlayerPrefs.SetString(TokenKey, Token);
            PlayerPrefs.SetString(ExpiresAtKey, auth.expiresAt);
            PlayerPrefs.Save();
        }

        public void Apply(MeResponse me)
        {
            UserId = me.userId;
            AccountType = me.accountType;
            Email = me.email;
        }

        public void Clear()
        {
            Token = "";
            ExpiresAtUtc = DateTime.MinValue;
            UserId = null;
            AccountType = null;
            Email = null;

            PlayerPrefs.DeleteKey(TokenKey);
            PlayerPrefs.DeleteKey(ExpiresAtKey);
            PlayerPrefs.Save();
        }

        // SystemInfo.deviceUniqueIdentifier is "n/a" on WebGL, which the server rejects (deviceId must be 16-128 chars).
        public static string GetDeviceId()
        {
            var id = PlayerPrefs.GetString(DeviceIdKey, "");
            if (id.Length >= 16 && id.Length <= 128)
                return id;

            var sys = SystemInfo.deviceUniqueIdentifier;
            id = sys != SystemInfo.unsupportedIdentifier && sys.Length >= 16 && sys.Length <= 128
                ? sys
                : Guid.NewGuid().ToString();

            PlayerPrefs.SetString(DeviceIdKey, id);
            PlayerPrefs.Save();
            return id;
        }

        static DateTime ParseUtc(string value)
        {
            return DateTime.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var result)
                ? result
                : DateTime.MinValue;
        }
    }
}
