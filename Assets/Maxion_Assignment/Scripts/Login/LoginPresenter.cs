using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MaxionAssignment.Backend;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace MaxionAssignment.Login
{
    // Owns every sign-in in the game. Nothing happens until the player presses a button:
    // - Email + password: POST /api/auth/login
    // - Guest: POST /api/auth/guest with this device's id
    // Each path ends with GET /api/me so PlayerSession has the account type and email.
    // On success it loads LoginSettings.NextSceneName (Shop). The register button swaps this popup for RegisterView.
    public class LoginPresenter : IStartable, IDisposable
    {
        readonly LoginView view;
        readonly RegisterView registerView;
        readonly BackendApiClient api;
        readonly PlayerSession session;
        readonly LoginSettings settings;
        readonly CancellationTokenSource cts = new();
        bool busy;

        public LoginPresenter(LoginView view, RegisterView registerView, BackendApiClient api, PlayerSession session,
            LoginSettings settings)
        {
            this.view = view;
            this.registerView = registerView;
            this.api = api;
            this.session = session;
            this.settings = settings;
        }

        public void Start()
        {
            view.LoginButton.onClick.AddListener(OnLoginClicked);
            view.GuestButton.onClick.AddListener(OnGuestClicked);
            if (view.RegisterButton != null)
                view.RegisterButton.onClick.AddListener(OnRegisterClicked);
            view.ShowInfo("");
        }

        public void Dispose()
        {
            cts.Cancel();
            cts.Dispose();
        }

        void OnLoginClicked()
        {
            var email = view.Email;
            var password = view.Password;

            if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            {
                view.ShowError("Please enter a valid email.");
                return;
            }
            if (string.IsNullOrEmpty(password))
            {
                view.ShowError("Please enter your password.");
                return;
            }

            AuthenticateAsync("Logging in...", async ct => await api.LoginAsync(email, password, ct)).Forget();
        }

        void OnRegisterClicked()
        {
            if (busy) return;
            view.Hide();
            view.ShowInfo("");
            registerView.Show();
        }

        void OnGuestClicked()
        {
            AuthenticateAsync("Logging in as guest...", async ct => await api.GuestLoginAsync(PlayerSession.GetDeviceId(), ct)).Forget();
        }

        async UniTaskVoid AuthenticateAsync(string pendingMessage, Func<CancellationToken, UniTask> login)
        {
            if (busy) return;
            busy = true;
            view.SetInteractable(false);
            view.ShowInfo(pendingMessage);

            try
            {
                await login(cts.Token);
                await api.GetMeAsync(cts.Token);

                var who = session.IsGuest ? "guest" : session.Email;
                view.ShowInfo($"Logged in as {who}");
                Debug.Log($"[Login] Logged in userId={session.UserId} ({session.AccountType})");

                // Leave the form locked. Loading the next scene tears this scope down.
                SceneManager.LoadSceneAsync(settings.NextSceneName);
                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (BackendException e)
            {
                view.ShowError(ToMessage(e));
                Debug.LogWarning($"[Login] Login failed: {e.Message}");
            }

            busy = false;
            view.SetInteractable(true);
        }

        static string ToMessage(BackendException e)
        {
            if (e.IsNetworkError) return "Cannot connect to the server.";
            if (e.IsUnauthorized) return "Incorrect email or password.";
            if (e.Status == 400) return "Invalid email or password format.";
            if (e.Status >= 500) return "Server error. Please try again.";
            return $"Login failed ({e.Code}).";
        }
    }
}
