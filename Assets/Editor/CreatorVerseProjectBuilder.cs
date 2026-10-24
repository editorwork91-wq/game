#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using CreatorVerse;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CreatorVerseProjectBuilder
{
    private const string HomePath = "Assets/Scenes/CreatorVerseHome.unity";
    private const string EditorPath = "Assets/Scenes/CreatorWorldEditor.unity";

    static CreatorVerseProjectBuilder() { EditorApplication.delayCall += EnsureScenes; }

    private static void EnsureScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(HomePath) && AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorPath)) return;
        BuildPlatformPrototype();
    }

    [MenuItem("Tools/CREATORVERSE/Build Platform Prototype")]
    public static void BuildPlatformPrototype()
    {
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Materials");
        BuildHomeScene();
        BuildEditorScene();
        RefreshBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("CREATORVERSE: generated home and world editor scenes. Open CreatorVerseHome and press Play.");
    }

    private static void BuildHomeScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Lighting(new Color(.035f, .055f, .1f));
        Material floor = Mat("CV_Backdrop", new Color(.025f, .04f, .075f));
        Material platform = Mat("CV_Platform", new Color(.07f, .13f, .22f));
        Material cyan = Mat("CV_Cyan", new Color(.12f, .8f, .94f));
        Material avatarMat = Mat("CV_Avatar", new Color(.16f, .66f, 1f));
        Cube("Hub Ground", new Vector3(0,-1,1), new Vector3(28,1,28), floor);
        Cube("Avatar Pedestal", new Vector3(3.4f,-.05f,4), new Vector3(4,.35f,4), platform);
        GameObject accent = Cube("Pedestal Accent", new Vector3(3.4f,.15f,4), new Vector3(4.08f,.07f,4.08f), cyan);
        accent.GetComponent<Collider>().enabled = false;
        GameObject avatarModel = BuildAvatar(new Vector3(3.4f,.2f,4), avatarMat);

        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = 45f;
        cam.farClipPlane = 200f;
        camObj.transform.position = new Vector3(9,6.2f,-8);
        camObj.transform.LookAt(new Vector3(2.4f,1.5f,3.5f));
        camObj.AddComponent<AudioListener>();
        GameObject floating = Cube("Floating Tile", new Vector3(7,2.2f,7), new Vector3(1.3f,.25f,1.3f), cyan);
        floating.transform.rotation = Quaternion.Euler(0,28,18);
        floating.GetComponent<Collider>().enabled = false;

        GameObject canvas = CanvasRoot("CREATORVERSE UI", new Vector2(1920,1080));
        GameObject home = Panel(canvas.transform, "Home Panel", new Color(.018f,.032f,.065f,.90f));
        TextLabel(home.transform,"CREATORVERSE",64,new Vector2(0,1),new Vector2(0,1),new Vector2(500,-85),new Vector2(760,80),TextAnchor.MiddleLeft);
        TextLabel(home.transform,"PLAY WORLDS. BUILD YOURS.",24,new Vector2(0,1),new Vector2(0,1),new Vector2(505,-150),new Vector2(760,42),TextAnchor.MiddleLeft);
        TextLabel(home.transform,"A new world starts with one block.",22,new Vector2(0,1),new Vector2(0,1),new Vector2(505,-205),new Vector2(760,42),TextAnchor.MiddleLeft);

        Button play = ButtonUI(home.transform,"PLAY LAST LIFT",24,new Vector2(0,1),new Vector2(0,1),new Vector2(315,-300),new Vector2(420,70),true);
        Button create = ButtonUI(home.transform,"CREATE WORLD",24,new Vector2(0,1),new Vector2(0,1),new Vector2(315,-386),new Vector2(420,70),true);
        Button profile = ButtonUI(home.transform,"PLAYER PROFILE",21,new Vector2(0,1),new Vector2(0,1),new Vector2(315,-470),new Vector2(420,62),false);
        Button avatar = ButtonUI(home.transform,"AVATAR STUDIO",21,new Vector2(0,1),new Vector2(0,1),new Vector2(315,-546),new Vector2(420,62),false);
        Button settings = ButtonUI(home.transform,"SETTINGS",21,new Vector2(0,1),new Vector2(0,1),new Vector2(315,-622),new Vector2(420,62),false);

        TextLabel(home.transform,"EXPERIENCE DISCOVERY",22,new Vector2(1,1),new Vector2(1,1),new Vector2(-385,-205),new Vector2(560,42),TextAnchor.MiddleLeft);
        ExperienceCard(home.transform,"LAST LIFT","3D PLATFORM CHALLENGE","PLAYABLE",new Vector2(-385,-280),new Vector2(560,112),true);
        Button obby = ButtonUI(home.transform,"OBBY LAB  •  COMING SOON",18,new Vector2(1,1),new Vector2(1,1),new Vector2(-385,-405),new Vector2(560,65),false);
        Button city = ButtonUI(home.transform,"SKY CITY  •  COMING SOON",18,new Vector2(1,1),new Vector2(1,1),new Vector2(-385,-482),new Vector2(560,65),false);
        TextLabel(home.transform,"OFFLINE PROTOTYPE • SAVES ARE LOCAL",16,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,30),new Vector2(900,32),TextAnchor.MiddleCenter);

        GameObject profilePanel = Panel(canvas.transform,"Profile Panel",new Color(.012f,.025f,.05f,.98f));
        TextLabel(profilePanel.transform,"PLAYER PROFILE",46,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-145),new Vector2(700,80),TextAnchor.MiddleCenter);
        TextLabel(profilePanel.transform,"DISPLAY NAME",22,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,160),new Vector2(540,42),TextAnchor.MiddleCenter);
        InputField nameInput = InputUI(profilePanel.transform,new Vector2(.5f,.5f),new Vector2(0,82),new Vector2(560,68));
        Button saveName = ButtonUI(profilePanel.transform,"SAVE PROFILE",22,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,0),new Vector2(330,66),true);
        Text profileStats = TextLabel(profilePanel.transform,"LOCAL PLAYER",22,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,-125),new Vector2(760,130),TextAnchor.MiddleCenter);
        Button profileBack = ButtonUI(profilePanel.transform,"BACK TO HUB",21,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,60),new Vector2(290,62),false);

        GameObject avatarPanel = Panel(canvas.transform,"Avatar Panel",new Color(.012f,.025f,.05f,.98f));
        TextLabel(avatarPanel.transform,"AVATAR STUDIO",46,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-145),new Vector2(700,80),TextAnchor.MiddleCenter);
        TextLabel(avatarPanel.transform,"Choose a style for your local avatar.",21,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-210),new Vector2(760,48),TextAnchor.MiddleCenter);
        Button sky = ButtonUI(avatarPanel.transform,"SKY BLUE",21,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-285,0),new Vector2(190,76),true);
        Button ember = ButtonUI(avatarPanel.transform,"EMBER",21,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-95,0),new Vector2(190,76),false);
        Button violet = ButtonUI(avatarPanel.transform,"VIOLET",21,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(95,0),new Vector2(190,76),false);
        Button mint = ButtonUI(avatarPanel.transform,"MINT",21,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(285,0),new Vector2(190,76),false);
        Button avatarBack = ButtonUI(avatarPanel.transform,"BACK TO HUB",21,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,60),new Vector2(290,62),false);

        GameObject settingsPanel = Panel(canvas.transform,"Settings Panel",new Color(.012f,.025f,.05f,.98f));
        TextLabel(settingsPanel.transform,"SETTINGS",46,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-155),new Vector2(700,80),TextAnchor.MiddleCenter);
        TextLabel(settingsPanel.transform,"Offline profile. Public accounts and multiplayer are not enabled.",21,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,75),new Vector2(950,80),TextAnchor.MiddleCenter);
        Button audio = ButtonUI(settingsPanel.transform,"AUDIO: ON",21,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,-35),new Vector2(310,68),true);
        Text audioText = audio.GetComponentInChildren<Text>();
        Button settingsBack = ButtonUI(settingsPanel.transform,"BACK TO HUB",21,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,60),new Vector2(290,62),false);
        Text status = TextLabel(canvas.transform,"OFFLINE MODE • progress saved on this device.",16,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,10),new Vector2(1000,30),TextAnchor.MiddleCenter);

        CreatorVerseHubController hub = new GameObject("CreatorVerse Hub Controller").AddComponent<CreatorVerseHubController>();
        hub.homePanel=home; hub.profilePanel=profilePanel; hub.avatarPanel=avatarPanel; hub.settingsPanel=settingsPanel;
        hub.displayNameInput=nameInput; hub.profileStatsText=profileStats; hub.statusText=status; hub.audioButtonText=audioText;
        hub.avatarPreviewRenderer=avatarModel.GetComponentInChildren<Renderer>();

        play.onClick.AddListener(hub.PlayLastLift); create.onClick.AddListener(hub.CreateWorld);
        profile.onClick.AddListener(hub.OpenProfile); avatar.onClick.AddListener(hub.OpenAvatar); settings.onClick.AddListener(hub.OpenSettings);
        saveName.onClick.AddListener(hub.SaveProfileName); profileBack.onClick.AddListener(hub.BackHome);
        avatarBack.onClick.AddListener(hub.BackHome); settingsBack.onClick.AddListener(hub.BackHome);
        sky.onClick.AddListener(()=>hub.SelectAvatarStyle(0)); ember.onClick.AddListener(()=>hub.SelectAvatarStyle(1));
        violet.onClick.AddListener(()=>hub.SelectAvatarStyle(2)); mint.onClick.AddListener(()=>hub.SelectAvatarStyle(3));
        audio.onClick.AddListener(hub.ToggleAudio);
        obby.onClick.AddListener(()=>hub.ShowComingSoon("Obby Lab")); city.onClick.AddListener(()=>hub.ShowComingSoon("Sky City"));

        profilePanel.SetActive(false); avatarPanel.SetActive(false); settingsPanel.SetActive(false); home.SetActive(true);
        EditorSceneManager.SaveScene(scene,HomePath);
    }

    private static void BuildEditorScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Lighting(new Color(.04f,.06f,.1f));
        Material floorMat=Mat("CV_EditorGround",new Color(.065f,.09f,.14f));
        Material gridMat=Mat("CV_EditorGrid",new Color(.11f,.16f,.22f));
        Material accent=Mat("CV_EditorAccent",new Color(.1f,.74f,.89f));
        GameObject ground=Cube("Build Ground",new Vector3(0,-.5f,0),new Vector3(26,1,26),floorMat);
        for(int i=-12;i<=12;i++)
        {
            GameObject gx=Cube("Grid X "+i,new Vector3(i,.008f,0),new Vector3(.018f,.012f,26),gridMat); gx.GetComponent<Collider>().enabled=false;
            GameObject gz=Cube("Grid Z "+i,new Vector3(0,.009f,i),new Vector3(26,.012f,.018f),gridMat); gz.GetComponent<Collider>().enabled=false;
        }
        GameObject marker=Cube("Spawn Marker",new Vector3(0,.015f,-5),new Vector3(1.5f,.03f,1.5f),accent); marker.GetComponent<Collider>().enabled=false;
        GameObject camObj=new GameObject("Main Camera"); camObj.tag="MainCamera";
        Camera camera=camObj.AddComponent<Camera>(); camera.fieldOfView=55; camera.farClipPlane=250;
        camObj.transform.position=new Vector3(0,15,-17); camObj.transform.rotation=Quaternion.Euler(48,0,0); camObj.AddComponent<AudioListener>();

        GameObject canvas=CanvasRoot("WORLD BUILDER UI",new Vector2(1920,1080));
        GameObject panel=Panel(canvas.transform,"Editor Panel",new Color(.015f,.025f,.05f,.88f));
        TextLabel(panel.transform,"WORLD BUILDER",34,new Vector2(0,1),new Vector2(0,1),new Vector2(225,-42),new Vector2(390,55),TextAnchor.MiddleLeft);
        InputField title=InputUI(panel.transform,new Vector2(0,1),new Vector2(220,-103),new Vector2(360,52));
        Text selected=TextLabel(panel.transform,"MODE: BUILD\nSELECTED: BLOCK\nHEIGHT: 0",18,new Vector2(0,1),new Vector2(0,1),new Vector2(220,-169),new Vector2(360,84),TextAnchor.UpperLeft);
        Button block=ButtonUI(panel.transform,"BLOCK",18,new Vector2(0,1),new Vector2(0,1),new Vector2(105,-262),new Vector2(170,50),true);
        Button platform=ButtonUI(panel.transform,"PLATFORM",18,new Vector2(0,1),new Vector2(0,1),new Vector2(290,-262),new Vector2(190,50),false);
        Button sphere=ButtonUI(panel.transform,"SPHERE",18,new Vector2(0,1),new Vector2(0,1),new Vector2(105,-322),new Vector2(170,50),false);
        Button cylinder=ButtonUI(panel.transform,"CYLINDER",18,new Vector2(0,1),new Vector2(0,1),new Vector2(290,-322),new Vector2(190,50),false);
        Button wall=ButtonUI(panel.transform,"WALL",18,new Vector2(0,1),new Vector2(0,1),new Vector2(105,-382),new Vector2(170,50),false);
        Button heightUp=ButtonUI(panel.transform,"HEIGHT +",17,new Vector2(0,1),new Vector2(0,1),new Vector2(290,-382),new Vector2(190,50),false);
        Button heightDown=ButtonUI(panel.transform,"HEIGHT -",17,new Vector2(0,1),new Vector2(0,1),new Vector2(105,-442),new Vector2(170,50),false);
        Button rotate=ButtonUI(panel.transform,"ROTATE LAST",17,new Vector2(0,1),new Vector2(0,1),new Vector2(290,-442),new Vector2(190,50),false);
        Button erase=ButtonUI(panel.transform,"DELETE MODE",17,new Vector2(0,1),new Vector2(0,1),new Vector2(200,-502),new Vector2(360,50),false);
        Button save=ButtonUI(panel.transform,"SAVE WORLD",18,new Vector2(0,1),new Vector2(0,1),new Vector2(200,-562),new Vector2(360,54),true);
        Button load=ButtonUI(panel.transform,"LOAD SAVED WORLD",16,new Vector2(0,1),new Vector2(0,1),new Vector2(200,-623),new Vector2(360,50),false);
        Button clear=ButtonUI(panel.transform,"CLEAR WORLD",16,new Vector2(0,1),new Vector2(0,1),new Vector2(200,-681),new Vector2(360,50),false);
        Button test=ButtonUI(panel.transform,"TEST WORLD",18,new Vector2(0,1),new Vector2(0,1),new Vector2(200,-741),new Vector2(360,54),true);
        Button home=ButtonUI(panel.transform,"BACK TO PLATFORM",16,new Vector2(0,0),new Vector2(0,0),new Vector2(200,40),new Vector2(360,50),false);
        Text status=TextLabel(panel.transform,"Click floor or an object to place blocks.",15,new Vector2(0,0),new Vector2(0,0),new Vector2(200,94),new Vector2(360,54),TextAnchor.MiddleLeft);

        CreatorWorldEditorController editor=new GameObject("World Editor Controller").AddComponent<CreatorWorldEditorController>();
        editor.buildCamera=camera; editor.ground=ground; editor.statusText=status; editor.selectedText=selected; editor.worldNameInput=title;
        block.onClick.AddListener(()=>editor.SelectKind(0)); platform.onClick.AddListener(()=>editor.SelectKind(1));
        sphere.onClick.AddListener(()=>editor.SelectKind(2)); cylinder.onClick.AddListener(()=>editor.SelectKind(3));
        wall.onClick.AddListener(()=>editor.SelectKind(4)); heightUp.onClick.AddListener(()=>editor.AdjustHeight(1));
        heightDown.onClick.AddListener(()=>editor.AdjustHeight(-1)); rotate.onClick.AddListener(editor.RotateLatest);
        erase.onClick.AddListener(editor.ToggleEraseMode); save.onClick.AddListener(editor.SaveWorld);
        load.onClick.AddListener(editor.LoadWorld); clear.onClick.AddListener(editor.ClearWorld);
        test.onClick.AddListener(editor.ToggleTestMode); home.onClick.AddListener(editor.ReturnHome);
        EditorSceneManager.SaveScene(scene,EditorPath);
    }

    private static void RefreshBuildSettings()
    {
        List<string> paths=new List<string>();
        AddScene(paths,HomePath); AddScene(paths,EditorPath); AddScene(paths,"Assets/Scenes/LastLift_Level01.unity");
        EditorBuildSettings.scenes=new EditorBuildSettingsScene[paths.Count];
        for(int i=0;i<paths.Count;i++) EditorBuildSettings.scenes[i]=new EditorBuildSettingsScene(paths[i],true);
        PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
    }

    private static void AddScene(List<string> paths,string path)
    {
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>(path)!=null) paths.Add(path);
    }

    private static void Lighting(Color fog)
    {
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight=new Color(.43f,.48f,.59f);
        RenderSettings.fog=true; RenderSettings.fogColor=fog; RenderSettings.fogDensity=.008f;
        GameObject key=new GameObject("Key Light");
        Light light=key.AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.3f;
        light.color=new Color(.75f,.86f,1f); key.transform.rotation=Quaternion.Euler(48,-32,0);
    }

    private static GameObject BuildAvatar(Vector3 pos,Material bodyMat)
    {
        GameObject root=new GameObject("Avatar Preview"); root.transform.position=pos;
        AddAvatarPart(root.transform,"Body",PrimitiveType.Capsule,new Vector3(0,1.05f,0),new Vector3(.8f,.68f,.55f),bodyMat);
        AddAvatarPart(root.transform,"Head",PrimitiveType.Sphere,new Vector3(0,2.02f,0),new Vector3(.65f,.65f,.65f),Mat("CV_AvatarHead",new Color(1,.76f,.55f)));
        Material limb=Mat("CV_AvatarLimb",new Color(.14f,.18f,.27f));
        AddAvatarPart(root.transform,"Left Arm",PrimitiveType.Capsule,new Vector3(-.62f,1.15f,0),new Vector3(.22f,.7f,.25f),limb);
        AddAvatarPart(root.transform,"Right Arm",PrimitiveType.Capsule,new Vector3(.62f,1.15f,0),new Vector3(.22f,.7f,.25f),limb);
        AddAvatarPart(root.transform,"Left Leg",PrimitiveType.Capsule,new Vector3(-.23f,.35f,0),new Vector3(.28f,.7f,.35f),limb);
        AddAvatarPart(root.transform,"Right Leg",PrimitiveType.Capsule,new Vector3(.23f,.35f,0),new Vector3(.28f,.7f,.35f),limb);
        foreach(Collider c in root.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        return root;
    }

    private static void AddAvatarPart(Transform parent,string name,PrimitiveType type,Vector3 pos,Vector3 scale,Material mat)
    {
        GameObject go=GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(parent,false);
        go.transform.localPosition=pos; go.transform.localScale=scale; go.GetComponent<Renderer>().sharedMaterial=mat;
    }

    private static void ExperienceCard(Transform parent,string title,string subtitle,string status,Vector2 pos,Vector2 size,bool primary)
    {
        GameObject card=new GameObject("Experience Card - "+title,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        card.transform.SetParent(parent,false); RectTransform rt=card.GetComponent<RectTransform>();
        rt.anchorMin=rt.anchorMax=new Vector2(1,1); rt.anchoredPosition=pos; rt.sizeDelta=size;
        card.GetComponent<Image>().color=primary?new Color(.06f,.3f,.42f,.98f):new Color(.09f,.12f,.18f,.98f);
        TextLabel(card.transform,title,23,new Vector2(0,1),new Vector2(0,1),new Vector2(18,-26),new Vector2(size.x-36,32),TextAnchor.MiddleLeft);
        TextLabel(card.transform,subtitle,15,new Vector2(0,1),new Vector2(0,1),new Vector2(18,-58),new Vector2(size.x-36,26),TextAnchor.MiddleLeft);
        TextLabel(card.transform,status,14,new Vector2(1,0),new Vector2(1,0),new Vector2(-70,18),new Vector2(125,24),TextAnchor.MiddleRight);
    }

    private static GameObject CanvasRoot(string name,Vector2 resolution)
    {
        GameObject go=new GameObject(name,typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler=go.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=resolution; scaler.matchWidthOrHeight=.5f;
        new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
        return go;
    }

    private static GameObject Panel(Transform parent,string name,Color color)
    {
        GameObject go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)); go.transform.SetParent(parent,false);
        RectTransform rt=go.GetComponent<RectTransform>(); rt.anchorMin=Vector2.zero; rt.anchorMax=Vector2.one; rt.offsetMin=Vector2.zero; rt.offsetMax=Vector2.zero;
        Image panelImage=go.GetComponent<Image>(); panelImage.color=color;
        // Full-screen decor must not block world placement raycasts on the editor camera.
        panelImage.raycastTarget=false;
        return go;
    }

    private static Text TextLabel(Transform parent,string value,int size,Vector2 amin,Vector2 amax,Vector2 pos,Vector2 dims,TextAnchor anchor)
    {
        GameObject go=new GameObject("Text - "+value,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text)); go.transform.SetParent(parent,false);
        RectTransform rt=go.GetComponent<RectTransform>(); rt.anchorMin=amin; rt.anchorMax=amax; rt.anchoredPosition=pos; rt.sizeDelta=dims;
        Text text=go.GetComponent<Text>(); text.text=value; text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize=size; text.alignment=anchor; text.color=Color.white; text.fontStyle=FontStyle.Bold;
        text.horizontalOverflow=HorizontalWrapMode.Wrap; text.verticalOverflow=VerticalWrapMode.Overflow; return text;
    }

    private static Button ButtonUI(Transform parent,string label,int size,Vector2 amin,Vector2 amax,Vector2 pos,Vector2 dims,bool primary)
    {
        GameObject go=new GameObject("Button - "+label,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button)); go.transform.SetParent(parent,false);
        RectTransform rt=go.GetComponent<RectTransform>(); rt.anchorMin=amin; rt.anchorMax=amax; rt.anchoredPosition=pos; rt.sizeDelta=dims;
        Image image=go.GetComponent<Image>(); image.color=primary?new Color(.055f,.49f,.63f,.98f):new Color(.09f,.13f,.2f,.98f);
        Button button=go.GetComponent<Button>(); ColorBlock cb=button.colors; cb.normalColor=image.color;
        cb.highlightedColor=new Color(.13f,.65f,.8f,1); cb.pressedColor=new Color(.035f,.34f,.45f,1); button.colors=cb;
        Text labelText=TextLabel(go.transform,label,size,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,TextAnchor.MiddleCenter);
        labelText.raycastTarget=false; return button;
    }

    private static InputField InputUI(Transform parent,Vector2 anchor,Vector2 pos,Vector2 dims)
    {
        GameObject go=new GameObject("Display Name Input",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(InputField)); go.transform.SetParent(parent,false);
        RectTransform rt=go.GetComponent<RectTransform>(); rt.anchorMin=anchor; rt.anchorMax=anchor; rt.anchoredPosition=pos; rt.sizeDelta=dims;
        go.GetComponent<Image>().color=new Color(.07f,.1f,.16f,1);
        Text value=TextLabel(go.transform,"New Creator",21,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,TextAnchor.MiddleLeft);
        RectTransform vrt=value.GetComponent<RectTransform>(); vrt.offsetMin=new Vector2(14,4); vrt.offsetMax=new Vector2(-14,-4); value.raycastTarget=false;
        Text placeholder=TextLabel(go.transform,"Enter display name",21,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,TextAnchor.MiddleLeft);
        RectTransform prt=placeholder.GetComponent<RectTransform>(); prt.offsetMin=new Vector2(14,4); prt.offsetMax=new Vector2(-14,-4);
        placeholder.color=new Color(.55f,.62f,.72f); placeholder.raycastTarget=false;
        InputField field=go.GetComponent<InputField>(); field.textComponent=value; field.placeholder=placeholder; field.characterLimit=18; return field;
    }

    private static GameObject Cube(string name,Vector3 pos,Vector3 scale,Material mat)
    {
        GameObject go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.position=pos; go.transform.localScale=scale; go.GetComponent<Renderer>().sharedMaterial=mat; return go;
    }

    private static Material Mat(string name,Color color)
    {
        string path="Assets/Materials/"+name+".mat";
        Material mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!mat)
        {
            Shader shader=Shader.Find("Standard"); if(!shader) shader=Shader.Find("Universal Render Pipeline/Lit");
            mat=new Material(shader); AssetDatabase.CreateAsset(mat,path);
        }
        mat.color=color; EditorUtility.SetDirty(mat); return mat;
    }

    private static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path)) return;
        string parent=Path.GetDirectoryName(path).Replace("\\","/"); string folder=Path.GetFileName(path);
        if(!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent,folder);
    }
}
#endif
