using System;
using System.Collections.Generic;
using UnityEngine;

namespace CreatorVerse
{
    [Serializable]
    public sealed class CreatorVerseProfile
    {
        public string displayName = "New Creator";
        public int avatarStyle;
        public int worldsCreated;
    }

    [Serializable]
    public sealed class CreatorVerseBlockData
    {
        public int kind;
        public float x, y, z, rotationY;
    }

    [Serializable]
    public sealed class CreatorVerseWorldData
    {
        public string worldName = "My First World";
        public int schemaVersion = 1;
        public List<CreatorVerseBlockData> blocks = new List<CreatorVerseBlockData>();
    }

    // Local storage seam: cloud features should be implemented as a separate real service.
    public static class CreatorVerseProfileService
    {
        private const string ProfileKey = "creatorverse.profile.v1";
        private const string WorldKey = "creatorverse.world.v1";
        private static CreatorVerseProfile cached;

        public static CreatorVerseProfile Profile
        {
            get { if (cached == null) LoadProfile(); return cached; }
        }

        public static void LoadProfile()
        {
            string json = PlayerPrefs.GetString(ProfileKey, "");
            cached = string.IsNullOrEmpty(json) ? new CreatorVerseProfile() : JsonUtility.FromJson<CreatorVerseProfile>(json);
            if (cached == null) cached = new CreatorVerseProfile();
            if (string.IsNullOrWhiteSpace(cached.displayName)) cached.displayName = "New Creator";
            cached.displayName = cached.displayName.Trim();
            if (cached.displayName.Length > 18) cached.displayName = cached.displayName.Substring(0, 18);
            cached.avatarStyle = Mathf.Clamp(cached.avatarStyle, 0, 3);
            cached.worldsCreated = Mathf.Max(0, cached.worldsCreated);
        }

        public static void SaveProfile()
        {
            if (cached == null) cached = new CreatorVerseProfile();
            PlayerPrefs.SetString(ProfileKey, JsonUtility.ToJson(cached));
            PlayerPrefs.Save();
        }

        public static void SetDisplayName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            string clean = value.Trim();
            Profile.displayName = clean.Substring(0, Mathf.Min(18, clean.Length));
            SaveProfile();
        }

        public static void SetAvatarStyle(int index)
        {
            Profile.avatarStyle = Mathf.Clamp(index, 0, 3);
            SaveProfile();
        }

        public static void SaveWorld(CreatorVerseWorldData world)
        {
            if (world == null) return;
            world.schemaVersion = 1;
            if (world.blocks == null) world.blocks = new List<CreatorVerseBlockData>();
            PlayerPrefs.SetString(WorldKey, JsonUtility.ToJson(world));
            Profile.worldsCreated = Mathf.Max(Profile.worldsCreated, 1);
            SaveProfile();
        }

        public static CreatorVerseWorldData LoadWorld()
        {
            string json = PlayerPrefs.GetString(WorldKey, "");
            if (string.IsNullOrEmpty(json)) return new CreatorVerseWorldData();
            CreatorVerseWorldData data = JsonUtility.FromJson<CreatorVerseWorldData>(json);
            if (data == null) return new CreatorVerseWorldData();
            if (data.blocks == null) data.blocks = new List<CreatorVerseBlockData>();
            if (string.IsNullOrWhiteSpace(data.worldName)) data.worldName = "My First World";
            return data;
        }

        public static bool HasSavedWorld() { return PlayerPrefs.HasKey(WorldKey); }
    }
}