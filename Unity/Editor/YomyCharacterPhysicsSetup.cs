#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Yomy.Editor
{
    // Non-destructive setup helper for the imported Yomy character.
    // Only CharacterController, Animator settings, and the gravity helper are touched.
    public static class YomyCharacterPhysicsSetup
    {
        private const float HeightPadding = 0.96f;
        private const float RadiusPadding = 0.42f;
        private const float MinRadius = 0.05f;
        private const float MinHeight = 0.2f;

        [MenuItem("Yomy/Character/Setup Physics on Selected")]
        private static void SetupSelected()
        {
            GameObject root = Selection.activeGameObject;

            if (root == null)
            {
                EditorUtility.DisplayDialog(
                    "Yomy Character",
                    "Select the character root in the Hierarchy first.",
                    "OK");
                return;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            CharacterController controller = root.GetComponent<CharacterController>();
            if (controller == null)
                controller = Undo.AddComponent<CharacterController>(root);
            else
                Undo.RecordObject(controller, "Configure Yomy CharacterController");

            FitCharacterController(controller, root);

            Animator animator = root.GetComponent<Animator>();
            if (animator == null)
                animator = root.GetComponentInChildren<Animator>();

            if (animator == null)
                animator = Undo.AddComponent<Animator>(root);

            Undo.RecordObject(animator, "Configure Yomy Animator");
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            Yomy.Character.YomyCharacterGravity gravity =
                root.GetComponent<Yomy.Character.YomyCharacterGravity>();

            if (gravity == null)
                gravity = Undo.AddComponent<Yomy.Character.YomyCharacterGravity>(root);

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(gravity);

            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = root;

            EditorUtility.DisplayDialog(
                "Yomy Character",
                "Physics setup complete. Mesh, materials, bones and animations were not edited.",
                "OK");
        }

        [MenuItem("Yomy/Character/Setup Physics on Selected", true)]
        private static bool ValidateSetupSelected()
        {
            return Selection.activeGameObject != null;
        }

        private static void FitCharacterController(CharacterController controller, GameObject root)
        {
            if (!TryGetLocalRenderBounds(root.transform, out Bounds localBounds))
            {
                controller.height = 1.8f;
                controller.radius = 0.3f;
                controller.center = Vector3.up * (controller.height * 0.5f);
                controller.slopeLimit = 50f;
                controller.stepOffset = 0.25f;
                controller.skinWidth = 0.03f;
                controller.minMoveDistance = 0f;
                controller.detectCollisions = true;
                controller.enableOverlapRecovery = true;
                return;
            }

            float visibleHeight = Mathf.Max(MinHeight, localBounds.size.y);
            float radiusFromWidth = Mathf.Max(
                MinRadius,
                Mathf.Max(localBounds.size.x, localBounds.size.z) * RadiusPadding);

            float height = Mathf.Max(MinHeight, visibleHeight * HeightPadding);
            float radius = Mathf.Min(radiusFromWidth, height * 0.45f);
            radius = Mathf.Max(MinRadius, radius);

            float centerY = localBounds.min.y + (height * 0.5f);

            controller.height = height;
            controller.radius = radius;
            controller.center = new Vector3(
                (localBounds.min.x + localBounds.max.x) * 0.5f,
                centerY,
                (localBounds.min.z + localBounds.max.z) * 0.5f);

            controller.slopeLimit = 50f;
            controller.stepOffset = Mathf.Clamp(height * 0.15f, 0.05f, 0.3f);
            controller.skinWidth = Mathf.Clamp(radius * 0.10f, 0.01f, 0.08f);
            controller.minMoveDistance = 0f;
            controller.detectCollisions = true;
            controller.enableOverlapRecovery = true;
        }

        private static bool TryGetLocalRenderBounds(Transform root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

            if (renderers == null || renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bool initialized = false;
            bounds = default;

            foreach (Renderer renderer in renderers)
            {
                Bounds worldBounds = renderer.bounds;

                for (int x = 0; x <= 1; x++)
                {
                    for (int y = 0; y <= 1; y++)
                    {
                        for (int z = 0; z <= 1; z++)
                        {
                            Vector3 corner = new Vector3(
                                x == 0 ? worldBounds.min.x : worldBounds.max.x,
                                y == 0 ? worldBounds.min.y : worldBounds.max.y,
                                z == 0 ? worldBounds.min.z : worldBounds.max.z);

                            Vector3 local = root.InverseTransformPoint(corner);

                            if (!initialized)
                            {
                                bounds = new Bounds(local, Vector3.zero);
                                initialized = true;
                            }
                            else
                            {
                                bounds.Encapsulate(local);
                            }
                        }
                    }
                }
            }

            return initialized;
        }
    }
}
#endif
