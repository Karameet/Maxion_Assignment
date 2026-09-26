using MaxionAssignment.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MaxionAssignment.Login
{
    // Passive view for the register popup. RegisterPresenter wires the buttons and drives the status text.
    public class RegisterView : PopupView
    {
        [Header("Form")]
        [SerializeField] TMP_InputField emailInput;
        [SerializeField] TMP_InputField passwordInput;
        [SerializeField] TMP_InputField confirmPasswordInput;
        [SerializeField] Button registerButton;
        [SerializeField] Button backButton;
        [SerializeField] TMP_Text statusText;
        [SerializeField] Color infoColor = new(0.85f, 0.88f, 0.95f);
        [SerializeField] Color errorColor = new(1f, 0.45f, 0.45f);

        bool locked;

        public Button RegisterButton => registerButton;
        public Button BackButton => backButton;
        public string Email => emailInput.text.Trim();
        public string Password => passwordInput.text;
        public string ConfirmPassword => confirmPasswordInput.text;

        protected override void Awake()
        {
            base.Awake();

            registerButton.AddClickAnimation();
            backButton.AddClickAnimation();

            emailInput.onValueChanged.AddListener(_ => RefreshRegisterButton());
            passwordInput.onValueChanged.AddListener(_ => RefreshRegisterButton());
            confirmPasswordInput.onValueChanged.AddListener(_ => RefreshRegisterButton());
            RefreshRegisterButton();
        }

        public void SetInteractable(bool interactable)
        {
            locked = !interactable;
            emailInput.interactable = interactable;
            passwordInput.interactable = interactable;
            confirmPasswordInput.interactable = interactable;
            backButton.interactable = interactable;
            RefreshRegisterButton();
        }

        public void ClearForm()
        {
            emailInput.text = "";
            passwordInput.text = "";
            confirmPasswordInput.text = "";
            ShowInfo("");
        }

        // Register stays disabled until all three fields have something in them, and while a request is in flight.
        void RefreshRegisterButton()
        {
            registerButton.interactable = !locked && Email.Length > 0 && Password.Length > 0 && ConfirmPassword.Length > 0;
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
