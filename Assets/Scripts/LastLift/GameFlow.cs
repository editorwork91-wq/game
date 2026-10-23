using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastLift
{
    public sealed class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        [Header("Panels")]
        public GameObject mainMenu;
        public GameObject profilePanel;
        public GameObject avatarPanel;
        public GameObject settingsPanel;
        public GameObject pausePanel;
        public GameObject resultPanel;
        public GameObject gameplayHud;

        [Header("Profile UI")]
        public InputField nameInput;
        public Text profileStatsText;
        public Text settingsAudioText;
        public Text hudCoinText;
        public Text resultTitleText;

        [Header("Player")]
        public PlayerMotor playerMotor;

        private bool ended;
        private bool audioMuted;

        private void Awake()
        {
            Instance = this;
            LocalProfileService.Load();
            Time.timeScale = 1f;
            if (playerMotor) playerMotor.CanControl = false;
            SetPanels(mainMenu, null);
            if (gameplayHud) gameplayHud.SetActive(false);
        }

        private void Start()
        {
            RefreshProfileUI();
            RefreshHUD();
            ApplySelectedSkin();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }

        public void BeginGame()
        {
            ended = false;
            CloseMenus();
            if (gameplayHud) gameplayHud.SetActive(true);
            if (playerMotor) playerMotor.CanControl = true;
            Time.timeScale = 1f;
        }

        public void OpenProfile()
        {
            CloseMenus();
            if (profilePanel) profilePanel.SetActive(true);
            RefreshProfileUI();
        }

        public void OpenAvatar()
        {
            CloseMenus();
            if (avatarPanel) avatarPanel.SetActive(true);
        }

        public void OpenSettings()
        {
            CloseMenus();
            if (settingsPanel) settingsPanel.SetActive(true);
            RefreshProfileUI();
        }

        public void BackToMenu()
        {
            CloseMenus();
            if (mainMenu) mainMenu.SetActive(true);
            if (playerMotor) { playerMotor.CanControl = false; playerMotor.SetMobileInput(Vector2.zero); }
            if (gameplayHud) gameplayHud.SetActive(false);
            Time.timeScale = 1f;
        }

        public void SaveProfileName()
        {
            if (nameInput) LocalProfileService.SetName(nameInput.text);
            RefreshProfileUI();
        }

        public void SelectSkin(int skinIndex)
        {
            LocalProfileService.SetSkin(skinIndex);
            ApplySelectedSkin();
            RefreshProfileUI();
        }

        public void ToggleAudio()
        {
            audioMuted = !audioMuted;
            AudioListener.volume = audioMuted ? 0f : 1f;
            if (settingsAudioText) settingsAudioText.text = audioMuted ? "AUDIO: MUTED" : "AUDIO: ON";
        }

        public void TogglePause()
        {
            if (ended) return;
            bool opening = pausePanel && !pausePanel.activeSelf;
            if (pausePanel) pausePanel.SetActive(opening);
            Time.timeScale = opening ? 0f : 1f;
        }

        public void Resume()
        {
            if (pausePanel) pausePanel.SetActive(false);
            Time.timeScale = 1f;
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void Win()
        {
            if (ended) return;
            ended = true;
            LocalProfileService.RecordLevelComplete(1);
            ShowResult("LEVEL COMPLETE");
        }

        public void Fail()
        {
            if (ended) return;
            ended = true;
            ShowResult("TRY AGAIN");
        }

        public void RefreshHUD()
        {
            if (hudCoinText) hudCoinText.text = "COINS  " + LocalProfileService.Profile.coins.ToString("000");
        }

        private void ShowResult(string message)
        {
            if (mainMenu) mainMenu.SetActive(false);
            if (profilePanel) profilePanel.SetActive(false);
            if (avatarPanel) avatarPanel.SetActive(false);
            if (settingsPanel) settingsPanel.SetActive(false);
            if (pausePanel) pausePanel.SetActive(false);
            if (gameplayHud) gameplayHud.SetActive(false);
            if (resultTitleText) resultTitleText.text = message;
            if (resultPanel) resultPanel.SetActive(true);
            Time.timeScale = 0f;
        }

        private void RefreshProfileUI()
        {
            if (nameInput) nameInput.text = LocalProfileService.Profile.displayName;
            if (profileStatsText)
                profileStatsText.text = "COINS   " + LocalProfileService.Profile.coins +
                    "\nBEST UNLOCKED LEVEL   " + LocalProfileService.Profile.bestLevel +
                    "\nPROFILE MODE   OFFLINE";
            if (settingsAudioText) settingsAudioText.text = audioMuted ? "AUDIO: MUTED" : "AUDIO: ON";
        }

        private void ApplySelectedSkin()
        {
            if (playerMotor) playerMotor.ApplySkin(LocalProfileService.Profile.selectedSkin);
        }

        private void CloseMenus()
        {
            if (mainMenu) mainMenu.SetActive(false);
            if (profilePanel) profilePanel.SetActive(false);
            if (avatarPanel) avatarPanel.SetActive(false);
            if (settingsPanel) settingsPanel.SetActive(false);
            if (pausePanel) pausePanel.SetActive(false);
            if (resultPanel) resultPanel.SetActive(false);
        }

        private static void SetPanels(GameObject visible, GameObject ignored)
        {
            if (visible) visible.SetActive(true);
        }
    }
}