using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Pixension.UI
{
    /// <summary>
    /// Procedurally generates the game menu UI at runtime
    /// No manual Unity Editor setup required
    /// </summary>
    public class GameMenuUIGenerator : MonoBehaviour
    {
        private Canvas canvas;
        private GameMenuUI menuUI;

        private void Start()
        {
            CreateUICanvas();
            CreateMainMenuPanel();
            CreateIngameMenuPanel();

            // Add GameMenuUI component after creating all UI elements
            menuUI = gameObject.AddComponent<GameMenuUI>();

            // Link the generated UI elements to GameMenuUI
            LinkUIElements();

            Debug.Log("GameMenuUI generated procedurally");
        }

        private void CreateUICanvas()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("GameMenuCanvas");
            canvasObj.transform.SetParent(transform);
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            // Add CanvasScaler for responsive UI
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Add GraphicRaycaster for UI interaction
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        private void CreateMainMenuPanel()
        {
            // Main Menu Panel
            GameObject mainPanel = CreatePanel("MainMenuPanel", canvas.transform);
            SetAnchor(mainPanel, AnchorPreset.StretchAll);

            Image panelImage = mainPanel.GetComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            // Title
            GameObject title = CreateText("Title", "PIXENSION", mainPanel.transform, 60);
            SetAnchor(title, AnchorPreset.TopCenter);
            title.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -100);

            // Seed Input
            GameObject seedLabel = CreateText("SeedLabel", "World Seed:", mainPanel.transform, 24);
            SetAnchor(seedLabel, AnchorPreset.MiddleCenter);
            seedLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-200, 150);

            GameObject seedInput = CreateInputField("SeedInput", "Enter seed...", mainPanel.transform);
            SetAnchor(seedInput, AnchorPreset.MiddleCenter);
            seedInput.GetComponent<RectTransform>().anchoredPosition = new Vector2(100, 150);
            seedInput.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 40);

            // Save Dropdown
            GameObject saveLabel = CreateText("SaveLabel", "Saved Games:", mainPanel.transform, 24);
            SetAnchor(saveLabel, AnchorPreset.MiddleCenter);
            saveLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-200, 50);

            GameObject saveDropdown = CreateDropdown("SaveDropdown", mainPanel.transform);
            SetAnchor(saveDropdown, AnchorPreset.MiddleCenter);
            saveDropdown.GetComponent<RectTransform>().anchoredPosition = new Vector2(100, 50);
            saveDropdown.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 40);

            // Save Info Text
            GameObject saveInfo = CreateText("SaveInfoText", "", mainPanel.transform, 18);
            SetAnchor(saveInfo, AnchorPreset.MiddleCenter);
            saveInfo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -80);
            saveInfo.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 150);
            TextMeshProUGUI infoTMP = saveInfo.GetComponent<TextMeshProUGUI>();
            infoTMP.alignment = TextAlignmentOptions.Center;

            // Buttons
            GameObject newGameBtn = CreateButton("NewGameButton", "New Game", mainPanel.transform);
            SetAnchor(newGameBtn, AnchorPreset.BottomCenter);
            newGameBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-220, 150);

            GameObject loadGameBtn = CreateButton("LoadGameButton", "Load Game", mainPanel.transform);
            SetAnchor(loadGameBtn, AnchorPreset.BottomCenter);
            loadGameBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 150);

            GameObject deleteGameBtn = CreateButton("DeleteGameButton", "Delete Save", mainPanel.transform);
            SetAnchor(deleteGameBtn, AnchorPreset.BottomCenter);
            deleteGameBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(220, 150);

            mainPanel.SetActive(false);
        }

        private void CreateIngameMenuPanel()
        {
            // Ingame Menu Panel
            GameObject ingamePanel = CreatePanel("IngameMenuPanel", canvas.transform);
            SetAnchor(ingamePanel, AnchorPreset.MiddleCenter);
            ingamePanel.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 400);

            Image panelImage = ingamePanel.GetComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            // Title
            GameObject title = CreateText("Title", "Game Menu (ESC)", ingamePanel.transform, 36);
            SetAnchor(title, AnchorPreset.TopCenter);
            title.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);

            // Auto-Save Timer
            GameObject autoSaveTimer = CreateText("AutoSaveTimerText", "Auto-Save: 00:00", ingamePanel.transform, 20);
            SetAnchor(autoSaveTimer, AnchorPreset.TopCenter);
            autoSaveTimer.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -100);

            // Auto-Save Progress Bar
            GameObject progressBar = CreateSlider("AutoSaveProgressBar", ingamePanel.transform);
            SetAnchor(progressBar, AnchorPreset.TopCenter);
            progressBar.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -140);
            progressBar.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 20);

            // Buttons
            GameObject saveBtn = CreateButton("SaveButton", "Save Game", ingamePanel.transform);
            SetAnchor(saveBtn, AnchorPreset.MiddleCenter);
            saveBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 20);

            GameObject loadBtn = CreateButton("LoadButton", "Load Game", ingamePanel.transform);
            SetAnchor(loadBtn, AnchorPreset.MiddleCenter);
            loadBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -40);

            GameObject quitBtn = CreateButton("QuitButton", "Quit to Desktop", ingamePanel.transform);
            SetAnchor(quitBtn, AnchorPreset.MiddleCenter);
            quitBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -100);

            ingamePanel.SetActive(false);
        }

        private void LinkUIElements()
        {
            // Main Menu
            menuUI.mainMenuPanel = canvas.transform.Find("MainMenuPanel").gameObject;
            menuUI.seedInput = menuUI.mainMenuPanel.transform.Find("SeedInput").GetComponent<TMP_InputField>();
            menuUI.saveDropdown = menuUI.mainMenuPanel.transform.Find("SaveDropdown").GetComponent<TMP_Dropdown>();
            menuUI.newGameButton = menuUI.mainMenuPanel.transform.Find("NewGameButton").GetComponent<Button>();
            menuUI.loadGameButton = menuUI.mainMenuPanel.transform.Find("LoadGameButton").GetComponent<Button>();
            menuUI.deleteGameButton = menuUI.mainMenuPanel.transform.Find("DeleteGameButton").GetComponent<Button>();
            menuUI.saveInfoText = menuUI.mainMenuPanel.transform.Find("SaveInfoText").GetComponent<TextMeshProUGUI>();

            // Ingame Menu
            menuUI.ingameMenuPanel = canvas.transform.Find("IngameMenuPanel").gameObject;
            menuUI.saveButton = menuUI.ingameMenuPanel.transform.Find("SaveButton").GetComponent<Button>();
            menuUI.loadButton = menuUI.ingameMenuPanel.transform.Find("LoadButton").GetComponent<Button>();
            menuUI.quitButton = menuUI.ingameMenuPanel.transform.Find("QuitButton").GetComponent<Button>();
            menuUI.autoSaveTimerText = menuUI.ingameMenuPanel.transform.Find("AutoSaveTimerText").GetComponent<TextMeshProUGUI>();
            menuUI.autoSaveProgressBar = menuUI.ingameMenuPanel.transform.Find("AutoSaveProgressBar").GetComponent<Slider>();
        }

        // UI Creation Helper Methods

        private GameObject CreatePanel(string name, Transform parent)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent);
            RectTransform rt = panel.AddComponent<RectTransform>();
            panel.AddComponent<CanvasRenderer>();
            panel.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            return panel;
        }

        private GameObject CreateText(string name, string text, Transform parent, int fontSize)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent);
            RectTransform rt = textObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400, 60);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return textObj;
        }

        private GameObject CreateButton(string name, string text, Transform parent)
        {
            GameObject button = new GameObject(name);
            button.transform.SetParent(parent);
            RectTransform rt = button.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 50);

            Image img = button.AddComponent<Image>();
            img.color = new Color(0.3f, 0.5f, 0.8f, 1f);

            Button btn = button.AddComponent<Button>();
            btn.targetGraphic = img;

            // Button colors
            ColorBlock colors = btn.colors;
            colors.normalColor = new Color(0.3f, 0.5f, 0.8f, 1f);
            colors.highlightedColor = new Color(0.4f, 0.6f, 0.9f, 1f);
            colors.pressedColor = new Color(0.2f, 0.4f, 0.7f, 1f);
            colors.selectedColor = new Color(0.3f, 0.5f, 0.8f, 1f);
            btn.colors = colors;

            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(button.transform);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            SetAnchor(textObj, AnchorPreset.StretchAll);
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return button;
        }

        private GameObject CreateInputField(string name, string placeholder, Transform parent)
        {
            GameObject inputField = new GameObject(name);
            inputField.transform.SetParent(parent);
            RectTransform rt = inputField.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 40);

            Image img = inputField.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            TMP_InputField inputComp = inputField.AddComponent<TMP_InputField>();

            // Text Area
            GameObject textArea = new GameObject("Text Area");
            textArea.transform.SetParent(inputField.transform);
            RectTransform textAreaRt = textArea.AddComponent<RectTransform>();
            SetAnchor(textArea, AnchorPreset.StretchAll);
            textAreaRt.offsetMin = new Vector2(10, 6);
            textAreaRt.offsetMax = new Vector2(-10, -7);
            textArea.AddComponent<RectMask2D>();

            // Text
            GameObject text = new GameObject("Text");
            text.transform.SetParent(textArea.transform);
            RectTransform textRt = text.AddComponent<RectTransform>();
            SetAnchor(text, AnchorPreset.StretchAll);
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = text.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 20;
            tmp.color = Color.white;

            // Placeholder
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(textArea.transform);
            RectTransform placeholderRt = placeholderObj.AddComponent<RectTransform>();
            SetAnchor(placeholderObj, AnchorPreset.StretchAll);
            placeholderRt.offsetMin = Vector2.zero;
            placeholderRt.offsetMax = Vector2.zero;

            TextMeshProUGUI placeholderTMP = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholderTMP.text = placeholder;
            placeholderTMP.fontSize = 20;
            placeholderTMP.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            placeholderTMP.fontStyle = FontStyles.Italic;

            inputComp.textViewport = textAreaRt;
            inputComp.textComponent = tmp;
            inputComp.placeholder = placeholderTMP;

            return inputField;
        }

        private GameObject CreateDropdown(string name, Transform parent)
        {
            GameObject dropdown = new GameObject(name);
            dropdown.transform.SetParent(parent);
            RectTransform rt = dropdown.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 40);

            Image img = dropdown.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            TMP_Dropdown dropdownComp = dropdown.AddComponent<TMP_Dropdown>();

            // Label
            GameObject label = new GameObject("Label");
            label.transform.SetParent(dropdown.transform);
            RectTransform labelRt = label.AddComponent<RectTransform>();
            SetAnchor(label, AnchorPreset.StretchAll);
            labelRt.offsetMin = new Vector2(10, 6);
            labelRt.offsetMax = new Vector2(-25, -7);

            TextMeshProUGUI labelTMP = label.AddComponent<TextMeshProUGUI>();
            labelTMP.fontSize = 20;
            labelTMP.color = Color.white;
            labelTMP.alignment = TextAlignmentOptions.Left;

            // Arrow
            GameObject arrow = new GameObject("Arrow");
            arrow.transform.SetParent(dropdown.transform);
            RectTransform arrowRt = arrow.AddComponent<RectTransform>();
            arrowRt.anchorMin = new Vector2(1, 0.5f);
            arrowRt.anchorMax = new Vector2(1, 0.5f);
            arrowRt.pivot = new Vector2(0.5f, 0.5f);
            arrowRt.sizeDelta = new Vector2(20, 20);
            arrowRt.anchoredPosition = new Vector2(-15, 0);

            Image arrowImg = arrow.AddComponent<Image>();
            arrowImg.color = Color.white;

            // Template
            GameObject template = new GameObject("Template");
            template.transform.SetParent(dropdown.transform);
            RectTransform templateRt = template.AddComponent<RectTransform>();
            templateRt.anchorMin = new Vector2(0, 0);
            templateRt.anchorMax = new Vector2(1, 0);
            templateRt.pivot = new Vector2(0.5f, 1);
            templateRt.anchoredPosition = new Vector2(0, 2);
            templateRt.sizeDelta = new Vector2(0, 150);

            Image templateImg = template.AddComponent<Image>();
            templateImg.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            ScrollRect scrollRect = template.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(template.transform);
            RectTransform viewportRt = viewport.AddComponent<RectTransform>();
            SetAnchor(viewport, AnchorPreset.StretchAll);
            viewportRt.offsetMin = new Vector2(0, 0);
            viewportRt.offsetMax = new Vector2(0, 0);
            viewport.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 1f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform);
            RectTransform contentRt = content.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.sizeDelta = new Vector2(0, 28);

            // Item
            GameObject item = new GameObject("Item");
            item.transform.SetParent(content.transform);
            RectTransform itemRt = item.AddComponent<RectTransform>();
            SetAnchor(item, AnchorPreset.TopStretch);
            itemRt.sizeDelta = new Vector2(0, 28);

            Toggle itemToggle = item.AddComponent<Toggle>();
            item.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 1f);

            GameObject itemLabel = new GameObject("Item Label");
            itemLabel.transform.SetParent(item.transform);
            RectTransform itemLabelRt = itemLabel.AddComponent<RectTransform>();
            SetAnchor(itemLabel, AnchorPreset.StretchAll);
            itemLabelRt.offsetMin = new Vector2(10, 1);
            itemLabelRt.offsetMax = new Vector2(-10, -2);

            TextMeshProUGUI itemLabelTMP = itemLabel.AddComponent<TextMeshProUGUI>();
            itemLabelTMP.fontSize = 18;
            itemLabelTMP.color = Color.white;

            itemToggle.targetGraphic = item.GetComponent<Image>();
            itemToggle.isOn = true;

            scrollRect.content = contentRt;
            scrollRect.viewport = viewportRt;

            dropdownComp.template = templateRt;
            dropdownComp.captionText = labelTMP;
            dropdownComp.itemText = itemLabelTMP;

            template.SetActive(false);

            return dropdown;
        }

        private GameObject CreateSlider(string name, Transform parent)
        {
            GameObject slider = new GameObject(name);
            slider.transform.SetParent(parent);
            RectTransform rt = slider.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 20);

            Slider sliderComp = slider.AddComponent<Slider>();

            // Background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(slider.transform);
            RectTransform bgRt = background.AddComponent<RectTransform>();
            SetAnchor(background, AnchorPreset.StretchAll);
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            Image bgImg = background.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(slider.transform);
            RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
            SetAnchor(fillArea, AnchorPreset.StretchAll);
            fillAreaRt.offsetMin = new Vector2(5, 5);
            fillAreaRt.offsetMax = new Vector2(-5, -5);

            // Fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform);
            RectTransform fillRt = fill.AddComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0, 0);
            fillRt.anchorMax = new Vector2(0, 1);
            fillRt.pivot = new Vector2(0.5f, 0.5f);
            fillRt.sizeDelta = new Vector2(10, 0);

            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.3f, 0.7f, 0.3f, 1f);

            sliderComp.fillRect = fillRt;
            sliderComp.minValue = 0;
            sliderComp.maxValue = 1;
            sliderComp.value = 0;

            return slider;
        }

        private void SetAnchor(GameObject obj, AnchorPreset preset)
        {
            RectTransform rt = obj.GetComponent<RectTransform>();
            if (rt == null) return;

            switch (preset)
            {
                case AnchorPreset.TopLeft:
                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(0, 1);
                    rt.pivot = new Vector2(0, 1);
                    break;
                case AnchorPreset.TopCenter:
                    rt.anchorMin = new Vector2(0.5f, 1);
                    rt.anchorMax = new Vector2(0.5f, 1);
                    rt.pivot = new Vector2(0.5f, 1);
                    break;
                case AnchorPreset.TopRight:
                    rt.anchorMin = new Vector2(1, 1);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(1, 1);
                    break;
                case AnchorPreset.MiddleLeft:
                    rt.anchorMin = new Vector2(0, 0.5f);
                    rt.anchorMax = new Vector2(0, 0.5f);
                    rt.pivot = new Vector2(0, 0.5f);
                    break;
                case AnchorPreset.MiddleCenter:
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    break;
                case AnchorPreset.MiddleRight:
                    rt.anchorMin = new Vector2(1, 0.5f);
                    rt.anchorMax = new Vector2(1, 0.5f);
                    rt.pivot = new Vector2(1, 0.5f);
                    break;
                case AnchorPreset.BottomLeft:
                    rt.anchorMin = new Vector2(0, 0);
                    rt.anchorMax = new Vector2(0, 0);
                    rt.pivot = new Vector2(0, 0);
                    break;
                case AnchorPreset.BottomCenter:
                    rt.anchorMin = new Vector2(0.5f, 0);
                    rt.anchorMax = new Vector2(0.5f, 0);
                    rt.pivot = new Vector2(0.5f, 0);
                    break;
                case AnchorPreset.BottomRight:
                    rt.anchorMin = new Vector2(1, 0);
                    rt.anchorMax = new Vector2(1, 0);
                    rt.pivot = new Vector2(1, 0);
                    break;
                case AnchorPreset.StretchAll:
                    rt.anchorMin = new Vector2(0, 0);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    break;
                case AnchorPreset.TopStretch:
                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(0.5f, 1);
                    break;
            }
        }

        private enum AnchorPreset
        {
            TopLeft, TopCenter, TopRight,
            MiddleLeft, MiddleCenter, MiddleRight,
            BottomLeft, BottomCenter, BottomRight,
            StretchAll, TopStretch
        }
    }
}
