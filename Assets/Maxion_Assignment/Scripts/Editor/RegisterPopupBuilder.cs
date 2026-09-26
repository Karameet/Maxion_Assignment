using MaxionAssignment.Login;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MaxionAssignment.EditorTools
{
    // Builds the register popup by cloning the login popup in the open scene, so both share the same look.
    // Running it again throws away the old register popup and clones the login popup afresh.
    static class RegisterPopupBuilder
    {
        const string UndoName = "Build Register Popup";

        [MenuItem("MaxionAssignment/Build Register Popup")]
        static void Build()
        {
            var loginView = Object.FindFirstObjectByType<LoginView>(FindObjectsInactive.Include);
            if (loginView == null)
            {
                EditorUtility.DisplayDialog(UndoName, "Open the Login scene first. No LoginView was found.", "OK");
                return;
            }

            var old = Object.FindFirstObjectByType<RegisterView>(FindObjectsInactive.Include);
            if (old != null)
                Undo.DestroyObjectImmediate(old.gameObject);

            var registerGo = CloneSibling(loginView.gameObject, "Register");
            var copy = registerGo.GetComponent<LoginView>();
            var copySo = new SerializedObject(copy);

            // Instantiate remaps references inside the cloned hierarchy, so these all point at the copy's children.
            var popup = Ref<RectTransform>(copySo, "popup");
            var backdrop = Ref<Image>(copySo, "backdrop");
            var email = Ref<TMP_InputField>(copySo, "emailInput");
            var password = Ref<TMP_InputField>(copySo, "passwordInput");
            var submit = Ref<Button>(copySo, "loginButton");
            var back = Ref<Button>(copySo, "guestButton");
            var openRegister = Ref<Button>(copySo, "registerButton");
            var status = Ref<TMP_Text>(copySo, "statusText");
            var openDuration = copySo.FindProperty("openDuration").floatValue;
            var startScale = copySo.FindProperty("startScale").floatValue;

            if (openRegister != null)
                Object.DestroyImmediate(openRegister.gameObject);
            Object.DestroyImmediate(copy);

            var confirm = CloneSibling(password.gameObject, "InputField_ConfirmPassword").GetComponent<TMP_InputField>();
            SetPlaceholder(confirm, "Confirm Password");

            email.text = "";
            password.text = "";
            confirm.text = "";
            status.text = "";

            submit.name = "Button_Register";
            SetLabel(submit, "Register");
            back.name = "Button_Back";
            SetLabel(back, "Back to Login");

            var view = registerGo.AddComponent<RegisterView>();
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("popup").objectReferenceValue = popup;
            viewSo.FindProperty("backdrop").objectReferenceValue = backdrop;
            viewSo.FindProperty("openDuration").floatValue = openDuration;
            viewSo.FindProperty("startScale").floatValue = startScale;
            viewSo.FindProperty("emailInput").objectReferenceValue = email;
            viewSo.FindProperty("passwordInput").objectReferenceValue = password;
            viewSo.FindProperty("confirmPasswordInput").objectReferenceValue = confirm;
            viewSo.FindProperty("registerButton").objectReferenceValue = submit;
            viewSo.FindProperty("backButton").objectReferenceValue = back;
            viewSo.FindProperty("statusText").objectReferenceValue = status;
            viewSo.ApplyModifiedPropertiesWithoutUndo();
            registerGo.SetActive(false);

            AddOpenRegisterButton(loginView);

            var scope = Object.FindFirstObjectByType<LoginLifetimeScope>(FindObjectsInactive.Include);
            if (scope != null)
            {
                var scopeSo = new SerializedObject(scope);
                scopeSo.FindProperty("registerView").objectReferenceValue = view;
                scopeSo.ApplyModifiedProperties();
            }

            EditorSceneManager.MarkSceneDirty(loginView.gameObject.scene);
            Selection.activeGameObject = registerGo;
            Debug.Log("[RegisterPopupBuilder] Built the register popup. Save the scene to keep it.");
        }

        // Adds a "Create Account" button under the guest button on the login popup, unless one is already assigned.
        static void AddOpenRegisterButton(LoginView loginView)
        {
            var loginSo = new SerializedObject(loginView);
            var prop = loginSo.FindProperty("registerButton");
            if (prop.objectReferenceValue != null)
                return;

            var guest = Ref<Button>(loginSo, "guestButton");
            var button = CloneSibling(guest.gameObject, "Button_CreateAccount").GetComponent<Button>();
            SetLabel(button, "Create Account");

            prop.objectReferenceValue = button;
            loginSo.ApplyModifiedProperties();
        }

        static GameObject CloneSibling(GameObject source, string name)
        {
            var clone = Object.Instantiate(source, source.transform.parent);
            clone.name = name;
            clone.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);
            Undo.RegisterCreatedObjectUndo(clone, UndoName);
            return clone;
        }

        static T Ref<T>(SerializedObject so, string property) where T : Object
        {
            return so.FindProperty(property).objectReferenceValue as T;
        }

        static void SetLabel(Button button, string text)
        {
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = text;
        }

        static void SetPlaceholder(TMP_InputField input, string text)
        {
            if (input.placeholder is TMP_Text placeholder)
                placeholder.text = text;
        }
    }
}
