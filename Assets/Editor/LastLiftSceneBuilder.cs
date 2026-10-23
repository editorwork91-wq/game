#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using LastLift;

[InitializeOnLoad]
public static class LastLiftSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/LastLift_Level01.unity";
    private static Material floorMat, platformMat, accentMat, dangerMat, coinMat, playerMat, darkMat;

    static LastLiftSceneBuilder()
    {
        EditorApplication.delayCall += EnsureStarterScene;
    }

    private static void EnsureStarterScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return;
        BuildFirstLevel();
    }

    [MenuItem("Tools/LAST LIFT/Build First Level")]
    public static void BuildFirstLevel()
    {
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Materials");
        EnsureFolder("Assets/Characters");

        floorMat = MakeMaterial("LL_Floor", new Color(0.055f, 0.075f, 0.12f));
        platformMat = MakeMaterial("LL_Platform", new Color(0.12f, 0.2f, 0.31f));
        accentMat = MakeMaterial("LL_Cyan", new Color(0.1f, 0.82f, 0.94f));
        dangerMat = MakeMaterial("LL_Hazard", new Color(1f, 0.12f, 0.25f));
        coinMat = MakeMaterial("LL_Coin", new Color(1f, 0.68f, 0.08f));
        playerMat = MakeMaterial("LL_Player", new Color(0.2f, 0.62f, 1f));
        darkMat = MakeMaterial("LL_UI_Dark", new Color(0.025f, 0.04f, 0.075f));

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.42f, 0.48f, 0.61f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.025f, 0.04f, 0.075f);
        RenderSettings.fogDensity = 0.008f;

        GameObject lightObject = new GameObject("Key Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.color = new Color(0.76f, 0.88f, 1f);
        lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);

        // A compact, readable first level assembled from native primitives.
        MakeCube("Start Platform", new Vector3(0f, -0.5f, 0f), new Vector3(8f, 1f, 8f), floorMat);
        GameObject lift = MakeCube("Moving Lift", new Vector3(0f, 0.2f, 7f), new Vector3(4f, 0.5f, 4f), accentMat);
        lift.AddComponent<MovingPlatform>();
        MakeCube("Platform 02", new Vector3(2f, 1.0f, 12f), new Vector3(3.2f, 0.5f, 3.2f), platformMat);
        MakeCube("Platform 03", new Vector3(-1f, 2f, 17f), new Vector3(3.2f, 0.5f, 3.2f), platformMat);
        MakeCube("Bridge", new Vector3(0f, 2.6f, 22f), new Vector3(4.2f, 0.5f, 3.2f), platformMat);
        MakeCube("Final Platform", new Vector3(0f, 3.2f, 28f), new Vector3(6f, 0.6f, 6f), floorMat);
        AddNeonEdge(new Vector3(0f, 0.05f, 7f), new Vector3(4.15f, 0.08f, 0.12f));
        AddNeonEdge(new Vector3(0f, 0.05f, 7f), new Vector3(0.12f, 0.08f, 4.15f));

        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(0f, 0.02f, 0f);
        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.36f;
        controller.center = new Vector3(0f, 0.9f, 0f);
        controller.stepOffset = 0.3f;
        controller.slopeLimit = 45f;
        PlayerMotor motor = player.AddComponent<PlayerMotor>();

        GameObject visual = CreateCharacterVisual(player.transform);
        visual.name = "Character Visual";
        motor.ApplySkin(0);

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 60f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 250f;
        cameraObject.AddComponent<AudioListener>();
        FollowCamera follow = cameraObject.AddComponent<FollowCamera>();
        follow.SetTarget(player.transform);
        motor.SetCamera(cameraObject.transform);

        AddCoin(new Vector3(0f, 1.0f, 4f));
        AddCoin(new Vector3(2f, 2.0f, 12f));
        AddCoin(new Vector3(-1f, 3.0f, 17f));
        AddCoin(new Vector3(0f, 3.4f, 24f));

        GameObject laser = MakeCube("Laser Hazard", new Vector3(0f, 3.25f, 22f), new Vector3(3.5f, 0.22f, 0.28f), dangerMat);
        laser.GetComponent<BoxCollider>().isTrigger = true;
        laser.AddComponent<Hazard>();

        GameObject finish = MakeCube("Finish Portal", new Vector3(0f, 4.4f, 30.6f), new Vector3(3.5f, 2.2f, 0.24f), accentMat);
        finish.GetComponent<BoxCollider>().isTrigger = true;
        finish.AddComponent<LevelFinish>();
        AddPortalSide(new Vector3(-1.75f, 4.4f, 30.6f));
        AddPortalSide(new Vector3(1.75f, 4.4f, 30.6f));

        GameFlow flow = new GameObject("Game Flow").AddComponent<GameFlow>();
        BuildInterface(flow, motor);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("LAST LIFT: generated the Level 01 scene. Press Play to test.");
    }

    private static GameObject CreateCharacterVisual(Transform parent)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/BOY VHARACTER ANIMATED.fbx");
        if (model == null) model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/BOY VHARACTER.fbx");
        if (model != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            foreach (Collider col in instance.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
            return instance;
        }

        GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        fallback.transform.SetParent(parent, false);
        fallback.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        fallback.transform.localScale = new Vector3(0.72f, 0.9f, 0.72f);
        Collider capsule = fallback.GetComponent<Collider>();
        if (capsule) Object.DestroyImmediate(capsule);
        fallback.GetComponent<Renderer>().sharedMaterial = playerMat;
        return fallback;
    }

    private static void BuildInterface(GameFlow flow, PlayerMotor motor)
    {
        GameObject canvasObject = new GameObject("LAST LIFT UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        GameObject hud = EmptyRect("Gameplay HUD", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Text coins = MakeText(hud.transform, "COINS 000", 28, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -45f), new Vector2(320f, 60f));
        Button pause = MakeButton(hud.transform, "Ⅱ", 30, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-72f, -58f), new Vector2(95f, 74f));

        MakeControl(hud.transform, motor, "◀", MobileInputButton.ActionType.Move, new Vector2(-1f, 0f), new Vector2(0f, 0f), new Vector2(92f, 90f), new Vector2(100f, 84f));
        MakeControl(hud.transform, motor, "▶", MobileInputButton.ActionType.Move, new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(300f, 90f), new Vector2(100f, 84f));
        MakeControl(hud.transform, motor, "▲", MobileInputButton.ActionType.Move, new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(196f, 176f), new Vector2(100f, 84f));
        MakeControl(hud.transform, motor, "▼", MobileInputButton.ActionType.Move, new Vector2(0f, -1f), new Vector2(0f, 0f), new Vector2(196f, 90f), new Vector2(100f, 84f));
        MakeControl(hud.transform, motor, "JUMP", MobileInputButton.ActionType.Jump, Vector2.zero, new Vector2(1f, 0f), new Vector2(-136f, 128f), new Vector2(164f, 118f));

        GameObject main = MakeFullscreenPanel(canvasObject.transform, "Main Menu", new Color(0.018f, 0.03f, 0.06f, 0.80f));
        MakeText(main.transform, "LAST LIFT", 76, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(760f, 120f));
        MakeText(main.transform, "CLIMB HIGHER. DON'T FALL.", 24, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -286f), new Vector2(760f, 60f));
        Button play = MakeButton(main.transform, "PLAY", 30, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(420f, 92f));
        Button profile = MakeButton(main.transform, "PLAYER PROFILE", 25, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(420f, 78f));
        Button avatar = MakeButton(main.transform, "AVATAR / SKINS", 25, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -96f), new Vector2(420f, 78f));
        Button settings = MakeButton(main.transform, "SETTINGS", 25, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -192f), new Vector2(420f, 78f));
        MakeText(main.transform, "OFFLINE PLAYER PROFILE  •  LEVEL 01", 18, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 55f), new Vector2(720f, 45f));

        GameObject profilePanel = MakeFullscreenPanel(canvasObject.transform, "Profile Panel", new Color(0.015f, 0.025f, 0.05f, 0.96f));
        MakeText(profilePanel.transform, "PLAYER PROFILE", 52, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(760f, 90f));
        MakeText(profilePanel.transform, "DISPLAY NAME", 22, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), new Vector2(540f, 48f));
        InputField nameInput = MakeInputField(profilePanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(560f, 74f));
        Button saveName = MakeButton(profilePanel.transform, "SAVE NAME", 24, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(330f, 68f));
        Text profileStats = MakeText(profilePanel.transform, "COINS 0", 23, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(650f, 150f));
        Button profileBack = MakeButton(profilePanel.transform, "BACK", 24, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 65f), new Vector2(280f, 70f));

        GameObject avatarPanel = MakeFullscreenPanel(canvasObject.transform, "Avatar Panel", new Color(0.015f, 0.025f, 0.05f, 0.96f));
        MakeText(avatarPanel.transform, "CHOOSE YOUR LOOK", 50, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -185f), new Vector2(820f, 90f));
        MakeText(avatarPanel.transform, "Your selection is saved on this device.", 22, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -260f), new Vector2(820f, 54f));
        Button skin0 = MakeButton(avatarPanel.transform, "SKY", 24, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-260f, 0f), new Vector2(210f, 100f));
        Button skin1 = MakeButton(avatarPanel.transform, "EMBER", 24, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(210f, 100f));
        Button skin2 = MakeButton(avatarPanel.transform, "VIOLET", 24, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(260f, 0f), new Vector2(210f, 100f));
        Button skin3 = MakeButton(avatarPanel.transform, "MINT", 24, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -125f), new Vector2(210f, 85f));
        Button avatarBack = MakeButton(avatarPanel.transform, "BACK", 24, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 65f), new Vector2(280f, 70f));

        GameObject settingsPanel = MakeFullscreenPanel(canvasObject.transform, "Settings Panel", new Color(0.015f, 0.025f, 0.05f, 0.96f));
        MakeText(settingsPanel.transform, "SETTINGS", 52, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -185f), new Vector2(700f, 90f));
        MakeText(settingsPanel.transform, "No account or network connection is needed to play.", 22, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(900f, 80f));
        Button audio = MakeButton(settingsPanel.transform, "AUDIO: ON", 24, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -25f), new Vector2(360f, 80f));
        Text settingsAudio = audio.GetComponentInChildren<Text>();
        Button settingsBack = MakeButton(settingsPanel.transform, "BACK", 24, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 65f), new Vector2(280f, 70f));

        GameObject pausePanel = MakeFullscreenPanel(canvasObject.transform, "Pause Panel", new Color(0.015f, 0.025f, 0.05f, 0.97f));
        MakeText(pausePanel.transform, "PAUSED", 56, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(600f, 90f));
        Button resume = MakeButton(pausePanel.transform, "RESUME", 26, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 45f), new Vector2(360f, 80f));
        Button restartPause = MakeButton(pausePanel.transform, "RESTART LEVEL", 26, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(360f, 80f));
        Button menuPause = MakeButton(pausePanel.transform, "MAIN MENU", 24, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -165f), new Vector2(360f, 76f));

        GameObject resultPanel = MakeFullscreenPanel(canvasObject.transform, "Result Panel", new Color(0.015f, 0.025f, 0.05f, 0.97f));
        Text resultTitle = MakeText(resultPanel.transform, "LEVEL COMPLETE", 48, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(900f, 100f));
        Button retry = MakeButton(resultPanel.transform, "PLAY AGAIN", 27, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(360f, 84f));
        Button resultMenu = MakeButton(resultPanel.transform, "MAIN MENU", 25, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -105f), new Vector2(360f, 76f));

        flow.mainMenu = main;
        flow.profilePanel = profilePanel;
        flow.avatarPanel = avatarPanel;
        flow.settingsPanel = settingsPanel;
        flow.pausePanel = pausePanel;
        flow.resultPanel = resultPanel;
        flow.gameplayHud = hud;
        flow.nameInput = nameInput;
        flow.profileStatsText = profileStats;
        flow.settingsAudioText = settingsAudio;
        flow.hudCoinText = coins;
        flow.resultTitleText = resultTitle;
        flow.playerMotor = motor;

        play.onClick.AddListener(flow.BeginGame);
        profile.onClick.AddListener(flow.OpenProfile);
        avatar.onClick.AddListener(flow.OpenAvatar);
        settings.onClick.AddListener(flow.OpenSettings);
        saveName.onClick.AddListener(flow.SaveProfileName);
        profileBack.onClick.AddListener(flow.BackToMenu);
        avatarBack.onClick.AddListener(flow.BackToMenu);
        settingsBack.onClick.AddListener(flow.BackToMenu);
        skin0.onClick.AddListener(() => flow.SelectSkin(0));
        skin1.onClick.AddListener(() => flow.SelectSkin(1));
        skin2.onClick.AddListener(() => flow.SelectSkin(2));
        skin3.onClick.AddListener(() => flow.SelectSkin(3));
        audio.onClick.AddListener(flow.ToggleAudio);
        pause.onClick.AddListener(flow.TogglePause);
        resume.onClick.AddListener(flow.Resume);
        restartPause.onClick.AddListener(flow.Restart);
        menuPause.onClick.AddListener(flow.BackToMenu);
        retry.onClick.AddListener(flow.Restart);
        resultMenu.onClick.AddListener(flow.BackToMenu);

        profilePanel.SetActive(false);
        avatarPanel.SetActive(false);
        settingsPanel.SetActive(false);
        pausePanel.SetActive(false);
        resultPanel.SetActive(false);
        hud.SetActive(false);
        main.SetActive(true);
    }

    private static void MakeControl(Transform parent, PlayerMotor motor, string label, MobileInputButton.ActionType action, Vector2 direction, Vector2 anchor, Vector2 position, Vector2 size)
    {
        Button button = MakeButton(parent, label, 22, anchor, anchor, position, size);
        MobileInputButton hold = button.gameObject.AddComponent<MobileInputButton>();
        hold.Setup(motor, action, direction);
    }

    private static GameObject MakeFullscreenPanel(Transform parent, string name, Color color)
    {
        GameObject go = EmptyRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image image = go.AddComponent<Image>();
        image.color = color;
        return go;
    }

    private static GameObject EmptyRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return go;
    }

    private static Text MakeText(Transform parent, string content, int size, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 dimensions)
    {
        GameObject go = new GameObject("Text - " + content, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        Text text = go.GetComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.fontStyle = FontStyle.Bold;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Button MakeButton(Transform parent, string label, int fontSize, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 dimensions)
    {
        GameObject go = new GameObject("Button - " + label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        Image image = go.GetComponent<Image>();
        image.color = new Color(0.04f, 0.48f, 0.62f, 0.96f);
        Button button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.07f, 0.52f, 0.68f, 0.96f);
        colors.highlightedColor = new Color(0.12f, 0.72f, 0.85f, 1f);
        colors.pressedColor = new Color(0.03f, 0.35f, 0.45f, 1f);
        button.colors = colors;
        Text text = MakeText(go.transform, label, fontSize, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        text.raycastTarget = false;
        return button;
    }

    private static InputField MakeInputField(Transform parent, Vector2 anchor, Vector2 position, Vector2 dimensions)
    {
        GameObject go = new GameObject("Display Name Input", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        go.GetComponent<Image>().color = new Color(0.09f, 0.12f, 0.18f, 1f);

        Text text = MakeText(go.transform, "Rookie", 25, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.offsetMin = new Vector2(18f, 8f);
        textRect.offsetMax = new Vector2(-18f, -8f);
        text.alignment = TextAnchor.MiddleLeft;

        Text placeholder = MakeText(go.transform, "Enter a player name", 23, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RectTransform placeholderRect = placeholder.GetComponent<RectTransform>();
        placeholderRect.offsetMin = new Vector2(18f, 8f);
        placeholderRect.offsetMax = new Vector2(-18f, -8f);
        placeholder.color = new Color(0.55f, 0.61f, 0.7f, 0.9f);
        placeholder.alignment = TextAnchor.MiddleLeft;
        go.GetComponent<InputField>().textComponent = text;
        go.GetComponent<InputField>().placeholder = placeholder;
        go.GetComponent<InputField>().characterLimit = 18;
        return go.GetComponent<InputField>();
    }

    private static void AddCoin(Vector3 position)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "Collectible Coin";
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.48f, 0.08f, 0.48f);
        go.GetComponent<Renderer>().sharedMaterial = coinMat;
        go.GetComponent<Collider>().isTrigger = true;
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        go.AddComponent<Collectible>();
    }

    private static void AddNeonEdge(Vector3 position, Vector3 scale)
    {
        GameObject edge = MakeCube("Lift Accent", position, scale, accentMat);
        edge.GetComponent<Collider>().enabled = false;
    }

    private static void AddPortalSide(Vector3 position)
    {
        GameObject side = MakeCube("Portal Frame", position, new Vector3(0.22f, 2.4f, 0.35f), accentMat);
        side.GetComponent<Collider>().enabled = false;
    }

    private static GameObject MakeCube(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    private static Material MakeMaterial(string name, Color color)
    {
        string path = "Assets/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        string folder = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
#endif
