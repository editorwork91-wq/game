using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CreatorVerse
{
    public sealed class CreatorVerseHubController : MonoBehaviour
    {
        public GameObject homePanel, profilePanel, avatarPanel, settingsPanel;
        public InputField displayNameInput;
        public Text profileStatsText, statusText, audioButtonText;
        public Renderer avatarPreviewRenderer;
        private bool muted;

        private void Awake()
        {
            CreatorVerseProfileService.LoadProfile();
            Time.timeScale = 1f;
        }

        private void Start()
        {
            ShowPanel(homePanel);
            RefreshProfile();
            ApplyAvatar();
        }

        public void PlayLastLift()
        {
            if (!Application.CanStreamedLevelBeLoaded("LastLift_Level01"))
            {
                SetStatus("Generate the LAST LIFT scene in the Unity Editor first.");
                return;
            }
            SceneManager.LoadScene("LastLift_Level01");
        }

        public void CreateWorld()
        {
            if (!Application.CanStreamedLevelBeLoaded("CreatorWorldEditor"))
            {
                SetStatus("Generate the CREATORVERSE platform scenes in the Unity Editor first.");
                return;
            }
            SceneManager.LoadScene("CreatorWorldEditor");
        }

        public void OpenProfile() { RefreshProfile(); ShowPanel(profilePanel); }
        public void OpenAvatar() { ShowPanel(avatarPanel); }
        public void OpenSettings()
        {
            if (audioButtonText) audioButtonText.text = muted ? "AUDIO: MUTED" : "AUDIO: ON";
            ShowPanel(settingsPanel);
        }

        public void SaveProfileName()
        {
            if (displayNameInput) CreatorVerseProfileService.SetDisplayName(displayNameInput.text);
            RefreshProfile();
            SetStatus("Profile saved on this device.");
        }

        public void SelectAvatarStyle(int index)
        {
            CreatorVerseProfileService.SetAvatarStyle(index);
            ApplyAvatar();
            RefreshProfile();
            SetStatus("Avatar style saved locally.");
        }

        public void ToggleAudio()
        {
            muted = !muted;
            AudioListener.volume = muted ? 0f : 1f;
            if (audioButtonText) audioButtonText.text = muted ? "AUDIO: MUTED" : "AUDIO: ON";
        }

        public void BackHome()
        {
            ShowPanel(homePanel);
            SetStatus("OFFLINE MODE • progress is saved on this device.");
        }

        public void ShowComingSoon(string name) { SetStatus(name + " is planned, but not playable yet."); }

        private void ShowPanel(GameObject target)
        {
            if (homePanel) homePanel.SetActive(target == homePanel);
            if (profilePanel) profilePanel.SetActive(target == profilePanel);
            if (avatarPanel) avatarPanel.SetActive(target == avatarPanel);
            if (settingsPanel) settingsPanel.SetActive(target == settingsPanel);
        }

        private void RefreshProfile()
        {
            if (displayNameInput) displayNameInput.text = CreatorVerseProfileService.Profile.displayName;
            if (profileStatsText)
            {
                CreatorVerseProfile p = CreatorVerseProfileService.Profile;
                profileStatsText.text = "PLAYER  " + p.displayName + "\nWORLDS SAVED  " + p.worldsCreated + "\nACCOUNT  LOCAL / OFFLINE";
            }
        }

        private void ApplyAvatar()
        {
            if (!avatarPreviewRenderer) return;
            Color color;
            switch (CreatorVerseProfileService.Profile.avatarStyle)
            {
                case 1: color = new Color(1f, .42f, .22f); break;
                case 2: color = new Color(.6f, .36f, 1f); break;
                case 3: color = new Color(.2f, .86f, .56f); break;
                default: color = new Color(.16f, .66f, 1f); break;
            }
            foreach (Material m in avatarPreviewRenderer.materials)
            {
                if (m && m.HasProperty("_Color")) m.color = color;
                if (m && m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            }
        }

        private void SetStatus(string message) { if (statusText) statusText.text = message; }
    }
}