using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MaxionAssignment.Backend;
using UnityEngine;
using VContainer.Unity;

namespace MaxionAssignment.Login
{
    // Creates an email account with POST /api/auth/register, then sends the player back to the login popup
    // with the email filled in. The server returns a token on register, but it is dropped so that signing in
    // always goes through the login popup.
    public class RegisterPresenter : IStartable, IDisposable
    {
        readonly RegisterView view;
        readonly LoginView loginView;
        readonly BackendApiClient api;
        readonly PlayerSession session;
        readonly CancellationTokenSource cts = new();
        bool busy;

        public RegisterPresenter(RegisterView view, LoginView loginView, BackendApiClient api, PlayerSession session)
        {
            this.view = view;
            this.loginView = loginView;
            this.api = api;
            this.session = session;
        }

        public void Start()
        {
            view.RegisterButton.onClick.AddListener(OnRegisterClicked);
            view.BackButton.onClick.AddListener(OnBackClicked);
            view.ShowInfo("");
        }

        public void Dispose()
        {
            cts.Cancel();
            cts.Dispose();
        }

        void OnBackClicked()
        {
            if (busy) return;
            view.Hide();
            view.ClearForm();
            loginView.Show();
        }

        void OnRegisterClicked()
        {
            var email = view.Email;
            var password = view.Password;

            if (!email.Contains('@'))
            {
                view.ShowError("Please enter a valid email.");
                return;
            }
            if (password != view.ConfirmPassword)
            {
                view.ShowError("Passwords do not match.");
                return;
            }

            RegisterAsync(email, password).Forget();
        }

        async UniTaskVoid RegisterAsync(string email, string password)
        {
            if (busy) return;
            busy = true;
            view.SetInteractable(false);
            view.ShowInfo("Creating account...");

            try
            {
                var auth = await api.RegisterAsync(email, password, cts.Token);
                session.Clear();
                Debug.Log($"[Register] Registered userId={auth.userId}");

                busy = false;
                view.SetInteractable(true);
                view.Hide();
                view.ClearForm();

                loginView.SetEmail(email);
                loginView.ShowInfo("Account created. Please log in.");
                loginView.Show();
                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (BackendException e)
            {
                view.ShowError(ToMessage(e));
                Debug.LogWarning($"[Register] Register failed: {e.Message}");
            }

            busy = false;
            view.SetInteractable(true);
        }

        static string ToMessage(BackendException e)
        {
            if (e.IsNetworkError) return "Cannot connect to the server.";
            if (e.Status == 409) return "This email is already registered.";
            if (e.Status == 400) return "Invalid email or password format.";
            if (e.Status >= 500) return "Server error. Please try again.";
            return $"Register failed ({e.Code}).";
        }
    }
}
