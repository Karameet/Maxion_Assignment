using MaxionAssignment.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MaxionAssignment.Login
{
    // Passive view for the login popup. LoginPresenter wires the buttons and drives the status text.
    public class LoginView : PopupView
    {
        [Header("Form")]
        [SerializeField] TMP_InputField emailInput;
        [SerializeField] TMP_InputField passwordInput;
        [SerializeField] Button loginButton;
        [SerializeField] Button guestButton;
        [SerializeField] Button registerButton;
        [SerializeField] TMP_Text statusText;
        [SerializeField] Color infoColor = new(0.85f, 0.88f, 0.95f);
        [SerializeField] Color errorColor = new(1f, 0.45f, 0.45f);

        bool locked;

        public Button LoginButton => loginButton;
        public Button GuestButton => guestButton;
        public Button RegisterButton => registerButton;
        public string Email => emailInput.text.Trim();
        public string Password => passwordInput.text;

        protected override void Awake()
        {
            base.Awake();

            loginButton.AddClickAnimation();
            guestButton.AddClickAnimation();
            if (registerButton != null)
                registerButton.AddClickAnimation();

            emailInput.onValueChanged.AddListener(_ => RefreshLoginButton());
            passwordInput.onValueChanged.AddListener(_ => RefreshLoginButton());
            RefreshLoginButton();
        }

        public void SetInteractable(bool interactable)
        {
            locked = !interactable;
            emailInput.interactable = interactable;
            passwordInput.interactable = interactable;
            guestButton.interactable = interactable;
            if (registerButton != null)
                registerButton.interactable = interactable;
            RefreshLoginButton();
        }

        // Login stays disabled until both fields have something in them, and while a request is in flight.
        void RefreshLoginButton()
        {
            loginButton.interactable = !locked && Email.Length > 0 && Password.Length > 0;
        }

        public void SetEmail(string email)
        {
            emailInput.text = email;
            passwordInput.text = "";
        }

        public void ShowInfo(string message) => SetStatus(message, infoColor);
        public void ShowError(string message) => SetStatus(message, errorColor);

        void SetStatus(string message, Color color)
        {
            statusText.text = message;
            statusText.color = color;
        }
    }
}
