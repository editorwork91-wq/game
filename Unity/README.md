# Yomy Unity Character Physics

This folder contains a non-destructive Unity setup helper for BOY VHARACTER ANIMATED.fbx.

## What it changes

The editor helper adds/configures:
- CharacterController fitted from the visible renderer bounds.
- YomyCharacterGravity, which applies the project's Physics.gravity vertically.
- Animator settings with Apply Root Motion disabled so code-driven movement and gravity remain separate from animation displacement.

## What it does not change

It does not rewrite the FBX, mesh vertices, materials, bone hierarchy, skin weights, or animation clips.

It also does not add a Rigidbody, avoiding a second movement/physics authority on the same character.

## How to use

1. Copy Runtime/YomyCharacterGravity.cs into Assets/Scripts/Yomy/Character/.
2. Copy Editor/YomyCharacterPhysicsSetup.cs into Assets/Editor/Yomy/.
3. Import BOY VHARACTER ANIMATED.fbx into the Unity project.
4. Drag the FBX model into the Scene.
5. Select the character root in the Hierarchy.
6. Run Yomy > Character > Setup Physics on Selected.
7. Save the configured character as a prefab.

The setup uses CharacterController. Horizontal movement can remain owned by the project's existing movement system. If an existing controller already applies gravity, disable YomyCharacterGravity instead of running two gravity systems.

## Important

The current GitHub repository does not contain the actual Unity game project. Project-specific movement, camera, input system, layers, and existing scripts are intentionally not overwritten here.
