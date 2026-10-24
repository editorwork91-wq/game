using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CreatorVerse
{
    public sealed class CreatorWorldEditorController : MonoBehaviour
    {
        public Camera buildCamera;
        public GameObject ground;
        public Text statusText, selectedText;
        public InputField worldNameInput;

        private readonly List<PlacedObject> objects = new List<PlacedObject>();
        private int selectedKind;
        private int heightSteps;
        private bool eraseMode, testMode;
        private GameObject testPlayer;
        private static readonly string[] Names = { "BLOCK", "PLATFORM", "SPHERE", "CYLINDER", "WALL" };

        private sealed class PlacedObject
        {
            public GameObject go;
            public CreatorVerseBlockData data;
        }

        private void Start()
        {
            CreatorVerseProfileService.LoadProfile();
            CreatorVerseWorldData saved = CreatorVerseProfileService.LoadWorld();
            if (worldNameInput && CreatorVerseProfileService.HasSavedWorld()) worldNameInput.text = saved.worldName;
            Rebuild(saved);
            RefreshSelection();
            SetStatus("Click the floor or a block to place. This draft stays offline on this device.");
        }

        private void Update()
        {
            if (testMode) return;
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.R)) RotateLatest();
            if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace)) ToggleEraseMode();
            if (Input.GetMouseButtonDown(0)) TryPlace();
#endif
        }

        public void SelectKind(int kind)
        {
            selectedKind = Mathf.Clamp(kind, 0, Names.Length - 1);
            eraseMode = false;
            RefreshSelection();
        }

        public void AdjustHeight(int direction)
        {
            heightSteps = Mathf.Clamp(heightSteps + direction, 0, 15);
            RefreshSelection();
            SetStatus("Placement height: " + heightSteps);
        }

        public void RotateLatest()
        {
            if (objects.Count == 0) { SetStatus("Place an object before rotating."); return; }
            PlacedObject item = objects[objects.Count - 1];
            item.data.rotationY = (item.data.rotationY + 90f) % 360f;
            item.go.transform.rotation = Quaternion.Euler(0f, item.data.rotationY, 0f);
            SaveWorld(false);
            SetStatus("Last object rotated.");
        }

        public void ToggleEraseMode()
        {
            eraseMode = !eraseMode;
            RefreshSelection();
            SetStatus(eraseMode ? "DELETE MODE: click a placed object." : "BUILD MODE: click floor or a block.");
        }

        public void SaveWorld() { SaveWorld(true); }

        private void SaveWorld(bool showMessage)
        {
            string title = worldNameInput ? worldNameInput.text.Trim() : "My First World";
            if (string.IsNullOrWhiteSpace(title)) title = "My First World";
            if (title.Length > 32) title = title.Substring(0, 32);
            CreatorVerseWorldData save = new CreatorVerseWorldData { worldName = title, blocks = new List<CreatorVerseBlockData>() };
            foreach (PlacedObject item in objects)
                if (item != null && item.go != null) save.blocks.Add(item.data);
            CreatorVerseProfileService.SaveWorld(save);
            if (showMessage) SetStatus("Saved locally: " + title + " • " + save.blocks.Count + " objects. Not published online.");
        }

        public void LoadWorld()
        {
            Rebuild(CreatorVerseProfileService.LoadWorld());
            SetStatus("Loaded the local saved draft.");
            RefreshSelection();
        }

        public void ClearWorld()
        {
            foreach (PlacedObject item in objects) if (item.go) Destroy(item.go);
            objects.Clear();
            SaveWorld(false);
            RefreshSelection();
            SetStatus("World cleared. Save to keep this empty draft.");
        }

        public void ToggleTestMode()
        {
            if (testMode) ExitTestMode();
            else EnterTestMode();
        }

        public void ReturnHome()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("CreatorVerseHome");
        }

        private void TryPlace()
        {
            if (!buildCamera || !ground) return;
            if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;
            Ray ray = buildCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f)) return;
            PlacedObject existing = FindObject(hit.collider.gameObject);

            if (eraseMode)
            {
                if (existing != null)
                {
                    objects.Remove(existing);
                    Destroy(existing.go);
                    RefreshSelection();
                    SetStatus("Object removed.");
                }
                return;
            }

            if (existing == null && hit.collider.gameObject != ground) return;
            Vector3 dims = Dimensions(selectedKind);
            float y = existing == null
                ? dims.y * .5f + heightSteps
                : existing.data.y + Dimensions(existing.data.kind).y * .5f + dims.y * .5f + heightSteps;
            CreatorVerseBlockData data = new CreatorVerseBlockData
            {
                kind = selectedKind,
                x = Mathf.Round(hit.point.x),
                y = y,
                z = Mathf.Round(hit.point.z),
                rotationY = 0f
            };
            Spawn(data);
            RefreshSelection();
            SetStatus(Names[selectedKind] + " placed. Save World to keep it.");
        }

        private void EnterTestMode()
        {
            testMode = true;
            eraseMode = false;
            testPlayer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            testPlayer.name = "Local Test Avatar";
            testPlayer.transform.position = new Vector3(0f, 1.2f, -5f);
            Renderer r = testPlayer.GetComponent<Renderer>();
            if (r) r.material.color = AvatarColor(CreatorVerseProfileService.Profile.avatarStyle);
            Collider old = testPlayer.GetComponent<Collider>();
            if (old) Destroy(old);
            CharacterController cc = testPlayer.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = .36f;
            cc.center = new Vector3(0f, .9f, 0f);
            CreatorWorldTestMotor motor = testPlayer.AddComponent<CreatorWorldTestMotor>();
            motor.Configure(buildCamera);
            SetStatus("TEST MODE • WASD/arrows to move • Space to jump • click TEST WORLD to return.");
            RefreshSelection();
        }

        private void ExitTestMode()
        {
            testMode = false;
            if (testPlayer) Destroy(testPlayer);
            testPlayer = null;
            if (buildCamera)
            {
                buildCamera.transform.position = new Vector3(0f, 15f, -17f);
                buildCamera.transform.rotation = Quaternion.Euler(48f, 0f, 0f);
            }
            SetStatus("Back in build mode.");
            RefreshSelection();
        }

        private void Rebuild(CreatorVerseWorldData data)
        {
            foreach (PlacedObject item in objects) if (item.go) Destroy(item.go);
            objects.Clear();
            if (data == null || data.blocks == null) return;
            foreach (CreatorVerseBlockData item in data.blocks)
                if (item != null && item.kind >= 0 && item.kind < Names.Length) Spawn(item);
        }

        private void Spawn(CreatorVerseBlockData data)
        {
            PrimitiveType type = PrimitiveType.Cube;
            if (data.kind == 2) type = PrimitiveType.Sphere;
            if (data.kind == 3) type = PrimitiveType.Cylinder;
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = "Creator Object - " + Names[Mathf.Clamp(data.kind, 0, Names.Length - 1)];
            go.transform.position = new Vector3(data.x, data.y, data.z);
            go.transform.rotation = Quaternion.Euler(0f, data.rotationY, 0f);
            go.transform.localScale = Dimensions(data.kind);
            go.GetComponent<Renderer>().material.color = ObjectColor(data.kind);
            objects.Add(new PlacedObject { go = go, data = data });
        }

        private PlacedObject FindObject(GameObject go)
        {
            for (int i = objects.Count - 1; i >= 0; i--)
                if (objects[i].go == go) return objects[i];
            return null;
        }

        private static Vector3 Dimensions(int kind)
        {
            switch (kind)
            {
                case 1: return new Vector3(3f, .5f, 3f);
                case 2: return Vector3.one;
                case 3: return new Vector3(1f, 1.2f, 1f);
                case 4: return new Vector3(3f, 2f, .45f);
                default: return Vector3.one;
            }
        }

        private static Color ObjectColor(int kind)
        {
            switch (kind)
            {
                case 1: return new Color(.1f, .78f, .9f);
                case 2: return new Color(.95f, .55f, .25f);
                case 3: return new Color(.72f, .44f, 1f);
                case 4: return new Color(.24f, .82f, .52f);
                default: return new Color(.28f, .55f, .95f);
            }
        }

        private static Color AvatarColor(int style)
        {
            switch (style)
            {
                case 1: return new Color(1f, .42f, .22f);
                case 2: return new Color(.6f, .36f, 1f);
                case 3: return new Color(.2f, .86f, .56f);
                default: return new Color(.16f, .66f, 1f);
            }
        }

        private void RefreshSelection()
        {
            if (selectedText) selectedText.text = (eraseMode ? "MODE: DELETE" : "MODE: BUILD") +
                "\nSELECTED: " + Names[Mathf.Clamp(selectedKind, 0, Names.Length - 1)] +
                "\nHEIGHT: " + heightSteps + " • OBJECTS: " + objects.Count;
        }

        private void SetStatus(string value) { if (statusText) statusText.text = value; }
    }
}