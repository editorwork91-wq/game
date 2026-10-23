using UnityEngine;

namespace LastLift
{
    [System.Serializable]
    public sealed class LocalPlayerProfile
    {
        public string displayName = "Rookie";
        public int coins;
        public int bestLevel = 1;
        public int selectedSkin;
    }

    // Offline storage boundary. Future cloud sync should be implemented as a separate service.
    public static class LocalProfileService
    {
        private const string ProfileKey = "lastlift.profile.v1";
        private static LocalPlayerProfile cached;

        public static LocalPlayerProfile Profile
        {
            get
            {
                if (cached == null) Load();
                return cached;
            }
        }

        public static void Load()
        {
            string json = PlayerPrefs.GetString(ProfileKey, string.Empty);
            cached = string.IsNullOrEmpty(json) ? new LocalPlayerProfile() : JsonUtility.FromJson<LocalPlayerProfile>(json);
            if (cached == null) cached = new LocalPlayerProfile();
            cached.displayName = string.IsNullOrWhiteSpace(cached.displayName) ? "Rookie" : cached.displayName.Trim();
            cached.coins = Mathf.Max(0, cached.coins);
            cached.bestLevel = Mathf.Max(1, cached.bestLevel);
            cached.selectedSkin = Mathf.Clamp(cached.selectedSkin, 0, 3);
        }

        public static void Save()
        {
            if (cached == null) cached = new LocalPlayerProfile();
            PlayerPrefs.SetString(ProfileKey, JsonUtility.ToJson(cached));
            PlayerPrefs.Save();
        }

        public static void AddCoins(int amount)
        {
            Profile.coins = Mathf.Max(0, Profile.coins + amount);
            Save();
        }

        public static void RecordLevelComplete(int completedLevel)
        {
            Profile.bestLevel = Mathf.Max(Profile.bestLevel, completedLevel + 1);
            Save();
        }

        public static void SetName(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                string clean = value.Trim();
                Profile.displayName = clean.Substring(0, Mathf.Min(18, clean.Length));
            }
            Save();
        }

        public static void SetSkin(int value)
        {
            Profile.selectedSkin = Mathf.Clamp(value, 0, 3);
            Save();
        }
    }
}