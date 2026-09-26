using MaxionAssignment.Login;
using MaxionAssignment.Shop;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MaxionAssignment.EditorTools
{
    // Builds the shop page (title, refresh button, scrolling product list, status text) under the first Canvas
    // in the open scene and wires ShopView + ShopLifetimeScope. Running it again replaces the old shop page.
    // The product row is saved as a prefab (ItemPrefabPath) that ShopView clones once per product.
    // "Build Purchase Popup" adds just the purchase popup, for a shop page that already exists.
    static class ShopUIBuilder
    {
        const string UndoName = "Build Shop UI";
        const string FontPath = "Assets/Maxion_Assignment/Font/GoogleSans-Medium SDF.asset";
        const string ItemPrefabPath = "Assets/Maxion_Assignment/prefabs/UI/ProductItemView.prefab";

        static readonly Color PanelColor = new(0.09f, 0.1f, 0.14f, 0.92f);
        static readonly Color ItemColor = new(1f, 1f, 1f, 0.08f);
        static readonly Color PriceColor = new(1f, 0.82f, 0.35f);
        static readonly Color BackdropColor = new(0f, 0f, 0f, 0.6f);

        [MenuItem("MaxionAssignment/Build Shop UI")]
        static void Build()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                EditorUtility.DisplayDialog(UndoName, "Open the Shop scene first. No Canvas was found.", "OK");
                return;
            }

            var old = Object.FindFirstObjectByType<ShopView>(FindObjectsInactive.Include);
            if (old != null)
                Undo.DestroyObjectImmediate(old.gameObject);

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            var root = CreateRect("Shop", canvas.transform);
            Undo.RegisterCreatedObjectUndo(root.gameObject, UndoName);
            Stretch(root, 0, 0, 0, 0);

            var panel = CreateRect("Panel", root);
            panel.gameObject.AddComponent<Image>().color = PanelColor;
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(1000, 1400);

            var title = CreateText("Text_Title", panel, "Shop", 72, font, TextAlignmentOptions.Left);
            Anchor(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            title.rectTransform.offsetMin = new Vector2(50, -150);
            title.rectTransform.offsetMax = new Vector2(-300, -40);

            var refresh = CreateButton("Button_Refresh", panel, "Refresh", font);
            var refreshRect = (RectTransform)refresh.transform;
            Anchor(refreshRect, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1));
            refreshRect.sizeDelta = new Vector2(220, 80);
            refreshRect.anchoredPosition = new Vector2(-50, -55);

            var status = CreateText("Text_Status", panel, "", 34, font, TextAlignmentOptions.Center);
            Anchor(status.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0));
            status.rectTransform.offsetMin = new Vector2(50, 30);
            status.rectTransform.offsetMax = new Vector2(-50, 100);

            var content = CreateScrollList(panel);
            var template = SaveItemPrefab(CreateItemTemplate(content, font));

            var view = root.gameObject.AddComponent<ShopView>();
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("listContent").objectReferenceValue = content;
            viewSo.FindProperty("itemTemplate").objectReferenceValue = template;
            viewSo.FindProperty("refreshButton").objectReferenceValue = refresh;
            viewSo.FindProperty("statusText").objectReferenceValue = status;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            var scopeSo = new SerializedObject(GetOrCreateScope(canvas));
            scopeSo.FindProperty("view").objectReferenceValue = view;
            scopeSo.ApplyModifiedProperties();

            BuildPurchasePopup(canvas, font);

            if (Object.FindFirstObjectByType<LoginLifetimeScope>(FindObjectsInactive.Include) != null)
                Debug.LogWarning("[ShopUIBuilder] This scene still has a LoginLifetimeScope (copied from the Login scene). " +
                                 "Delete it and the login popups, or the login flow will run here too.");

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("[ShopUIBuilder] Built the shop page. Save the scene to keep it.");
        }

        [MenuItem("MaxionAssignment/Build Purchase Popup")]
        static void BuildPurchasePopupMenu()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                EditorUtility.DisplayDialog(UndoName, "Open the Shop scene first. No Canvas was found.", "OK");
                return;
            }

            var popup = BuildPurchasePopup(canvas, AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath));
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Selection.activeGameObject = popup.gameObject;
            Debug.Log("[ShopUIBuilder] Built the purchase popup. It starts inactive. Save the scene to keep it.");
        }

        // Full-screen backdrop plus a centered popup: name, unit price, [-] quantity [+], total, status,
        // Cancel / Buy. Replaces any existing PurchasePopupView and assigns it to ShopLifetimeScope.
        static PurchasePopupView BuildPurchasePopup(Canvas canvas, TMP_FontAsset font)
        {
            var old = Object.FindFirstObjectByType<PurchasePopupView>(FindObjectsInactive.Include);
            if (old != null)
                Undo.DestroyObjectImmediate(old.gameObject);

            var root = CreateRect("PurchasePopup", canvas.transform);
            Undo.RegisterCreatedObjectUndo(root.gameObject, UndoName);
            Stretch(root, 0, 0, 0, 0);
            root.SetAsLastSibling();
            // Raycast target stays on, so clicks can't reach the shop list behind the popup.
            var backdrop = root.gameObject.AddComponent<Image>();
            backdrop.color = BackdropColor;

            var popup = CreateRect("Popup", root);
            popup.gameObject.AddComponent<Image>().color = new Color(PanelColor.r, PanelColor.g, PanelColor.b, 1f);
            popup.anchorMin = popup.anchorMax = new Vector2(0.5f, 0.5f);
            popup.sizeDelta = new Vector2(800, 0);
            var column = popup.gameObject.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(50, 50, 50, 50);
            column.spacing = 24;
            column.childAlignment = TextAnchor.UpperCenter;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            popup.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var nameText = CreateText("Text_Name", popup, "Product Name", 56, font, TextAlignmentOptions.Center);
            SetHeight(nameText, 80);
            var priceText = CreateText("Text_Price", popup, "Price: 0.00", 40, font, TextAlignmentOptions.Center);
            priceText.color = PriceColor;
            SetHeight(priceText, 60);

            var quantityRow = CreateRow("Row_Quantity", popup, 100);
            var quantityLabel = CreateText("Text_Label", quantityRow, "Quantity", 40, font, TextAlignmentOptions.Right);
            SetWidth(quantityLabel, 200);
            var minus = CreateButton("Button_Minus", quantityRow, "-", font);
            SetWidth(minus, 90);
            var quantityInput = CreateQuantityInput(quantityRow, font);
            SetWidth(quantityInput, 160);
            var plus = CreateButton("Button_Plus", quantityRow, "+", font);
            SetWidth(plus, 90);

            var totalText = CreateText("Text_Total", popup, "Total: 0.00", 48, font, TextAlignmentOptions.Center);
            SetHeight(totalText, 70);
            var statusText = CreateText("Text_Status", popup, "", 32, font, TextAlignmentOptions.Center);
            SetHeight(statusText, 90);

            var actionRow = CreateRow("Row_Actions", popup, 100);
            var close = CreateButton("Button_Cancel", actionRow, "Cancel", font);
            SetWidth(close, 280);
            var buy = CreateButton("Button_Buy", actionRow, "Buy", font);
            buy.targetGraphic.color = PriceColor;
            SetWidth(buy, 280);

            var view = root.gameObject.AddComponent<PurchasePopupView>();
            var so = new SerializedObject(view);
            so.FindProperty("popup").objectReferenceValue = popup;
            so.FindProperty("backdrop").objectReferenceValue = backdrop;
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("priceText").objectReferenceValue = priceText;
            so.FindProperty("totalText").objectReferenceValue = totalText;
            so.FindProperty("quantityInput").objectReferenceValue = quantityInput;
            so.FindProperty("minusButton").objectReferenceValue = minus;
            so.FindProperty("plusButton").objectReferenceValue = plus;
            so.FindProperty("buyButton").objectReferenceValue = buy;
            so.FindProperty("closeButton").objectReferenceValue = close;
            so.FindProperty("statusText").objectReferenceValue = statusText;
            so.ApplyModifiedPropertiesWithoutUndo();
            root.gameObject.SetActive(false);

            var scopeSo = new SerializedObject(GetOrCreateScope(canvas));
            scopeSo.FindProperty("purchasePopup").objectReferenceValue = view;
            scopeSo.ApplyModifiedProperties();
            return view;
        }

        static TMP_InputField CreateQuantityInput(RectTransform parent, TMP_FontAsset font)
        {
            var go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources
            {
                inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            });
            go.name = "InputField_Quantity";
            go.transform.SetParent(parent, false);

            var input = go.GetComponent<TMP_InputField>();
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.characterLimit = 3;
            input.text = "1";
            if (font != null)
                input.fontAsset = font;
            input.pointSize = 40;
            input.textComponent.alignment = TextAlignmentOptions.Center;
            if (input.placeholder is TMP_Text placeholder)
            {
                placeholder.text = "";
                placeholder.alignment = TextAlignmentOptions.Center;
            }
            return input;
        }

        static RectTransform CreateRow(string name, RectTransform parent, float height)
        {
            var row = CreateRect(name, parent);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            return row;
        }

        static void SetHeight(Component target, float height) =>
            target.gameObject.AddComponent<LayoutElement>().preferredHeight = height;

        static void SetWidth(Component target, float width) =>
            target.gameObject.AddComponent<LayoutElement>().preferredWidth = width;

        static ShopLifetimeScope GetOrCreateScope(Canvas canvas)
        {
            var scope = Object.FindFirstObjectByType<ShopLifetimeScope>(FindObjectsInactive.Include);
            if (scope != null)
                return scope;

            var scopeGo = new GameObject("ShopLifetimeScope");
            Undo.RegisterCreatedObjectUndo(scopeGo, UndoName);
            SceneManager.MoveGameObjectToScene(scopeGo, canvas.gameObject.scene);
            return scopeGo.AddComponent<ShopLifetimeScope>();
        }

        // Turns the scene item template of an already-built shop page into the ProductItemView prefab.
        [MenuItem("MaxionAssignment/Save Product Item Prefab")]
        static void SaveItemPrefabFromScene()
        {
            var view = Object.FindFirstObjectByType<ShopView>(FindObjectsInactive.Include);
            if (view == null)
            {
                EditorUtility.DisplayDialog(UndoName, "Open the Shop scene first. No ShopView was found.", "OK");
                return;
            }

            var viewSo = new SerializedObject(view);
            var prop = viewSo.FindProperty("itemTemplate");
            var template = prop.objectReferenceValue as ProductItemView;
            if (template == null || EditorUtility.IsPersistent(template))
            {
                EditorUtility.DisplayDialog(UndoName, "ShopView has no scene item template to save. " +
                                                      "It may already point at a prefab.", "OK");
                return;
            }

            prop.objectReferenceValue = SaveItemPrefab(template);
            viewSo.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
            Debug.Log($"[ShopUIBuilder] Saved {ItemPrefabPath}. Save the scene to keep the new reference.");
        }

        // Saves the scene template as the prefab (active, so clones show up) and removes it from the scene.
        static ProductItemView SaveItemPrefab(ProductItemView template)
        {
            var go = template.gameObject;
            go.name = "ProductItemView";
            go.SetActive(true);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, ItemPrefabPath);
            Undo.DestroyObjectImmediate(go);
            return prefab.GetComponent<ProductItemView>();
        }

        // Vertical-only scroll view whose content grows with its children. Returns the content RectTransform.
        static RectTransform CreateScrollList(RectTransform parent)
        {
            var scroll = CreateRect("ScrollView_Products", parent);
            Stretch(scroll, 50, 50, 170, 120);
            var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.scrollSensitivity = 30;

            var viewport = CreateRect("Viewport", scroll);
            Stretch(viewport, 0, 0, 0, 0);
            viewport.gameObject.AddComponent<RectMask2D>();
            // A transparent graphic so drags on empty space still scroll the list.
            viewport.gameObject.AddComponent<Image>().color = Color.clear;

            var content = CreateRect("Content", viewport);
            Anchor(content, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            content.sizeDelta = Vector2.zero;
            content.anchoredPosition = Vector2.zero;

            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            return content;
        }

        static ProductItemView CreateItemTemplate(RectTransform parent, TMP_FontAsset font)
        {
            var item = CreateRect("ProductItem_Template", parent);
            var background = item.gameObject.AddComponent<Image>();
            background.color = ItemColor;
            var button = item.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            var element = item.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 120;
            element.preferredHeight = 120;

            var row = item.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(40, 40, 0, 0);
            row.spacing = 20;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;

            var nameText = CreateText("Text_Name", item, "Product Name", 42, font, TextAlignmentOptions.Left);
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            nameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            var priceText = CreateText("Text_Price", item, "0.00", 42, font, TextAlignmentOptions.Right);
            priceText.color = PriceColor;
            priceText.gameObject.AddComponent<LayoutElement>().preferredWidth = 260;

            var view = item.gameObject.AddComponent<ProductItemView>();
            var so = new SerializedObject(view);
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("priceText").objectReferenceValue = priceText;
            so.FindProperty("button").objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        static Button CreateButton(string name, RectTransform parent, string label, TMP_FontAsset font)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var text = CreateText("Text", rect, label, 34, font, TextAlignmentOptions.Center);
            text.color = new Color(0.1f, 0.1f, 0.12f);
            Stretch(text.rectTransform, 0, 0, 0, 0);
            return button;
        }

        static TextMeshProUGUI CreateText(string name, RectTransform parent, string value, float size,
            TMP_FontAsset font, TextAlignmentOptions alignment)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null)
                text.font = font;
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            Anchor(rect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
        }
    }
}
