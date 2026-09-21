import bpy
import os
import sys

def arg_value(name, default=None):
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    for i, a in enumerate(args):
        if a == name and i + 1 < len(args):
            return args[i + 1]
    return default

PATH = arg_value("--input", "BOY VHARACTER ANIMATED.fbx")
EXPECTED = {"Idle", "Walk", "Run", "Jump", "Attack_Combo", "Block_Hit"}

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=os.path.abspath(PATH), use_anim=True)

armatures = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"]
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]

if len(armatures) != 1:
    raise RuntimeError(f"Expected exactly 1 armature, found {len(armatures)}")
if not meshes:
    raise RuntimeError("No meshes found after FBX re-import")

arm = armatures[0]
if len(arm.data.bones) < 15:
    raise RuntimeError(f"Unexpectedly small skeleton: {len(arm.data.bones)} bones")

# Every imported mesh must have a skinning modifier pointing to the single skeleton.
bad_meshes = []
for m in meshes:
    arm_mods = [mod for mod in m.modifiers if mod.type == "ARMATURE"]
    if not arm_mods or any(mod.object != arm for mod in arm_mods):
        bad_meshes.append(m.name)
if bad_meshes:
    raise RuntimeError("Meshes without a valid link to the imported skeleton: " + ", ".join(bad_meshes))

# No constraints are required by the exported asset; runtime should rely only on baked FBX data.
constraints = [(pb.name, c.type) for pb in arm.pose.bones for c in pb.constraints]
if constraints:
    raise RuntimeError(f"Unexpected runtime constraints in exported FBX: {constraints}")

actions = {a.name for a in bpy.data.actions}
missing = EXPECTED - actions
if missing:
    raise RuntimeError(f"Missing expected animation clips after re-import: {sorted(missing)}")

# Ensure each expected clip contains actual animation curves.
empty = [name for name in EXPECTED if not bpy.data.actions.get(name) or not any(len(fc.keyframe_points) for fc in bpy.data.actions[name].fcurves)]
if empty:
    raise RuntimeError(f"Animation clips imported but contain no keyframes: {sorted(empty)}")

print("FBX_RUNTIME_VALIDATION_OK")
print("ARMATURES", len(armatures), "BONES", len(arm.data.bones), "MESHES", len(meshes))
print("ACTIONS", sorted(actions))
print("CONSTRAINTS", len(constraints))
