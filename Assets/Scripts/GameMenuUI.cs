using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Pixension.UI
{
    public class GameMenuUI : MonoBehaviour
    {
        [Header("Main Menu")]
        public GameObject mainMenuPanel;
        public TMP_InputField seedInput;
        public TMP_Dropdown saveDropdown;
        public Button newGameButton;
        public Button loadGameButton;
        public Button deleteGameButton;
        public TextMeshProUGUI saveInfoText;

        [Header("In-Game Menu")]
        public GameObject ingameMenuPanel;
        public Button saveButton;
        public Button loadButton;
        public Button quitButton;
        public TextMeshProUGUI autoSaveTimerText;
        public Slider autoSaveProgressBar;

        [Header("Settings")]
        public bool showIngameMenu = false;

        private SaveSystem.SaveLoadManager saveLoadManager;
        private GameManager gameManager;
        private List<SaveSystem.SaveFileInfo> availableSaves;

        private void Start()
        {
            saveLoadManager = SaveSystem.SaveLoadManager.Instance;
            gameManager = GameManager.Instance;

            SetupMainMenu();
            SetupIngameMenu();

            RefreshSavesList();

            // Verstecke beide Panels initial
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(false);

            if (ingameMenuPanel != null)
                ingameMenuPanel.SetActive(false);
        }

        private void SetupMainMenu()
        {
            if (newGameButton != null)
            {
                newGameButton.onClick.AddListener(OnNewGameClicked);
            }

            if (loadGameButton != null)
            {
                loadGameButton.onClick.AddListener(OnLoadGameClicked);
            }

            if (deleteGameButton != null)
            {
                deleteGameButton.onClick.AddListener(OnDeleteGameClicked);
            }

            if (saveDropdown != null)
            {
                saveDropdown.onValueChanged.AddListener(OnSaveDropdownChanged);
            }

            // Generiere zufälligen Seed
            if (seedInput != null)
            {
                seedInput.text = Random.Range(int.MinValue, int.MaxValue).ToString();
            }
        }

        private void SetupIngameMenu()
        {
            if (saveButton != null)
            {
                saveButton.onClick.AddListener(OnSaveClicked);
            }

            if (loadButton != null)
            {
                loadButton.onClick.AddListener(OnLoadClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitClicked);
            }
        }

        private void Update()
        {
            // ESC zum Öffnen/Schließen des Ingame-Menüs
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ToggleIngameMenu();
            }

            // Update Auto-Save Anzeige
            UpdateAutoSaveDisplay();
        }

        private void UpdateAutoSaveDisplay()
        {
            if (autoSaveTimerText != null && gameManager != null)
            {
                autoSaveTimerText.text = $"Auto-Save: {gameManager.GetAutoSaveTimeRemaining()}";
            }

            if (autoSaveProgressBar != null && gameManager != null)
            {
                autoSaveProgressBar.value = gameManager.GetAutoSaveProgress();
            }
        }

        public void ShowMainMenu()
        {
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(true);
                RefreshSavesList();
            }

            if (ingameMenuPanel != null)
            {
                ingameMenuPanel.SetActive(false);
            }
        }

        public void ShowIngameMenu()
        {
            if (ingameMenuPanel != null)
            {
                ingameMenuPanel.SetActive(true);
            }

            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);
            }
        }

        public void ToggleIngameMenu()
        {
            showIngameMenu = !showIngameMenu;

            if (ingameMenuPanel != null)
            {
                ingameMenuPanel.SetActive(showIngameMenu);
            }
        }

        private void RefreshSavesList()
        {
            availableSaves = saveLoadManager.GetSaveFiles();

            if (saveDropdown != null)
            {
                saveDropdown.ClearOptions();

                List<string> saveNames = new List<string>();
                foreach (var save in availableSaves)
                {
                    saveNames.Add(save.saveName);
                }

                if (saveNames.Count == 0)
                {
                    saveNames.Add("No saves found");
                }

                saveDropdown.AddOptions(saveNames);

                // Zeige Info zum ersten Save
                if (availableSaves.Count > 0)
                {
                    UpdateSaveInfo(0);
                }
            }
        }

        private void OnSaveDropdownChanged(int index)
        {
            UpdateSaveInfo(index);
        }

        private void UpdateSaveInfo(int index)
        {
            if (saveInfoText == null || availableSaves == null || index >= availableSaves.Count)
                return;

            SaveSystem.SaveFileInfo info = availableSaves[index];

            string infoText = $"<b>{info.saveName}</b>\n" +
                             $"Date: {info.GetSaveDateTime():yyyy-MM-dd HH:mm:ss}\n" +
                             $"Seed: {info.seed}\n" +
                             $"Chunks: {info.totalChunks}\n" +
                             $"Voxels: {info.totalVoxels}\n" +
                             $"Size: {info.GetFileSizeFormatted()}\n" +
                             $"Version: {info.saveVersion}";

            saveInfoText.text = infoText;
        }

        private void OnNewGameClicked()
        {
            if (seedInput == null || gameManager == null)
                return;

            int seed;
            if (int.TryParse(seedInput.text, out seed))
            {
                gameManager.CreateNewWorldWithSeed(seed);
                Debug.Log($"Creating new game with seed: {seed}");
            }
            else
            {
                Debug.LogWarning("Invalid seed input");
            }

            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(false);
        }

        private void OnLoadGameClicked()
        {
            if (saveDropdown == null || availableSaves == null || gameManager == null)
                return;

            int selectedIndex = saveDropdown.value;

            if (selectedIndex < availableSaves.Count)
            {
                string saveName = availableSaves[selectedIndex].saveName;
                gameManager.LoadWorldByName(saveName);
                Debug.Log($"Loading game: {saveName}");
            }

            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(false);
        }

        private void OnDeleteGameClicked()
        {
            if (saveDropdown == null || availableSaves == null)
                return;

            int selectedIndex = saveDropdown.value;

            if (selectedIndex < availableSaves.Count)
            {
                string saveName = availableSaves[selectedIndex].saveName;

                // Bestätigung (in echtem Spiel Dialog verwenden)
                bool confirmed = true; // TODO: Zeige Confirmation Dialog

                if (confirmed)
                {
                    saveLoadManager.DeleteSave(saveName);
                    Debug.Log($"Deleted save: {saveName}");
                    RefreshSavesList();
                }
            }
        }

        private void OnSaveClicked()
        {
            if (gameManager == null)
                return;

            string saveName = $"save_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            gameManager.SaveWorld(saveName);
            Debug.Log($"Saved game as: {saveName}");
        }

        private void OnLoadClicked()
        {
            // Zeige Main Menu zum Laden
            ShowMainMenu();
        }

        private void OnQuitClicked()
        {
            Debug.Log("Quitting game...");

            // Speichere vor dem Beenden
            if (gameManager != null)
            {
                gameManager.SaveWorld("autosave");
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}