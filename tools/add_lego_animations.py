import bpy
import math
import os
import sys
import re
from mathutils import Euler, Quaternion

# Procedural, non-power LEGO/NINJAGO-style movement set.
# The source FBX is never overwritten: an animated sibling file is exported.

def arg_value(name, default=None):
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    for i, a in enumerate(args):
        if a == name and i + 1 < len(args):
            return args[i + 1]
    return default

INPUT = arg_value("--input", "BOY VHARACTER.fbx")
OUTPUT = arg_value("--output", "BOY VHARACTER ANIMATED.fbx")

# Clean scene, then import the source exactly once.
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
try:
    bpy.ops.outliner.orphans_purge(do_recursive=True)
except Exception:
    pass

bpy.ops.import_scene.fbx(filepath=os.path.abspath(INPUT), use_anim=True)

armatures = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"]
if not armatures:
    raise RuntimeError("No ARMATURE found in the source FBX; refusing to export a potentially broken substitute.")
arm = armatures[0]

# Bone discovery is name-based but tolerant of common FBX/Blender conventions.
bones = list(arm.data.bones)

def norm(s):
    return re.sub(r"[^a-z0-9]+", "", s.lower())

def side_ok(n, side):
    n = norm(n)
    if side == "L":
        return any(t in n for t in ("left", "lft")) or n.endswith("l") or n.endswith("l001")
    if side == "R":
        return any(t in n for t in ("right", "rgt")) or n.endswith("r") or n.endswith("r001")
    return True

def find_bone(groups, side=None):
    # groups: ordered list of token-tuples. Earlier groups are preferred.
    for tokens in groups:
        for b in bones:
            n = norm(b.name)
            if side_ok(n, side) and all(t in n for t in tokens):
                return b.name
    return None

def exact_any(tokens):
    for b in bones:
        n = norm(b.name)
        if n in tokens:
            return b.name
    return None

B = {}
B["root"] = exact_any({"root", "master", "armatureroot"}) or find_bone([("root",)])
B["hips"] = find_bone([("pelvis",), ("hips",), ("hip",), ("pelvic",)])
B["spine"] = find_bone([("spine",), ("body",), ("waist",)])
B["chest"] = find_bone([("chest",), ("upperbody",), ("spine2",), ("spine01",)])
B["neck"] = find_bone([("neck",)])
B["head"] = find_bone([("head",)])

for s in ("L", "R"):
    B[f"upper_arm_{s}"] = find_bone([("upperarm",), ("upperarm", "bone"), ("arm",)], s)
    B[f"lower_arm_{s}"] = find_bone([("forearm",), ("lowerarm",), ("elbow",)], s)
    B[f"hand_{s}"] = find_bone([("hand",), ("wrist",)], s)
    B[f"upper_leg_{s}"] = find_bone([("thigh",), ("upperleg",), ("upleg",)], s)
    B[f"lower_leg_{s}"] = find_bone([("calf",), ("lowerleg",), ("shin",), ("knee",)], s)
    B[f"foot_{s}"] = find_bone([("foot",), ("ankle",)], s)

PB = {k: arm.pose.bones[v] for k, v in B.items() if v and v in arm.pose.bones}
if len(PB) < 3:
    raise RuntimeError("The armature was found, but too few usable bones were detected; refusing unsafe animation export.")

scene = bpy.context.scene
scene.render.fps = 30

# Capture the imported base pose and restore it for every sampled key.
base = {}
for name, p in PB.items():
    base[name] = {
        "mode": p.rotation_mode,
        "euler": p.rotation_euler.copy(),
        "quat": p.rotation_quaternion.copy(),
        "loc": p.location.copy(),
    }

def set_rot(key, x=0.0, y=0.0, z=0.0):
    p = PB.get(key)
    if not p:
        return
    bx = base[key]
    if p.rotation_mode == "QUATERNION":
        q = Quaternion((1, 0, 0), x) @ Quaternion((0, 1, 0), y) @ Quaternion((0, 0, 1), z)
        p.rotation_quaternion = bx["quat"] @ q
        p.keyframe_insert("rotation_quaternion", frame=scene.frame_current, group=key)
    elif p.rotation_mode == "AXIS_ANGLE":
        # Preserve the original axis; add a simple local tilt around it.
        p.rotation_axis_angle[3] = bx["quat"].angle + x + y + z
        p.keyframe_insert("rotation_axis_angle", frame=scene.frame_current, group=key)
    else:
        e = bx["euler"].copy()
        e.rotate_axis("X", x)
        e.rotate_axis("Y", y)
        e.rotate_axis("Z", z)
        p.rotation_euler = e
        p.keyframe_insert("rotation_euler", frame=scene.frame_current, group=key)

def set_loc(key, x=0.0, y=0.0, z=0.0):
    p = PB.get(key)
    if not p:
        return
    p.location = base[key]["loc"] + (x, y, z)
    p.keyframe_insert("location", frame=scene.frame_current, group=key)

def clear_to_base():
    for key, p in PB.items():
        bx = base[key]
        if p.rotation_mode == "QUATERNION":
            p.rotation_quaternion = bx["quat"]
        elif p.rotation_mode == "AXIS_ANGLE":
            # Keep the original axis-angle values from the imported pose.
            p.rotation_axis_angle = (bx["quat"].x, bx["quat"].y, bx["quat"].z, bx["quat"].angle)
        else:
            p.rotation_euler = bx["euler"]
        p.location = bx["loc"]

def keyframe_pose():
    for p in PB.values():
        if p.rotation_mode == "QUATERNION":
            p.keyframe_insert("rotation_quaternion", frame=scene.frame_current)
        elif p.rotation_mode == "AXIS_ANGLE":
            p.keyframe_insert("rotation_axis_angle", frame=scene.frame_current)
        else:
            p.keyframe_insert("rotation_euler", frame=scene.frame_current)
        p.keyframe_insert("location", frame=scene.frame_current)

def new_action(name, length):
    # Remove only an action with our exact generated name; leave source actions intact.
    if bpy.data.actions.get(name):
        bpy.data.actions.remove(bpy.data.actions.get(name))
    action = bpy.data.actions.new(name)
    action.use_fake_user = True
    arm.animation_data_create()
    arm.animation_data.action = action
    scene.frame_start = 1
    scene.frame_end = length
    return action

def frame(f):
    scene.frame_set(f)
    clear_to_base()

def finish(action, extrap="NOTHING"):
    for fc in action.fcurves:
        for kp in fc.keyframe_points:
            kp.interpolation = "BEZIER"
        fc.extrapolation = extrap
    action.frame_start = 1
    action.frame_end = scene.frame_end

def clip_idle():
    a = new_action("Idle", 60)
    for f, sway, nod in [(1,0.0,0.0),(15,0.018,0.010),(30,-0.014,-0.006),(45,0.016,0.008),(60,0.0,0.0)]:
        frame(f)
        set_rot("spine", 0, sway, 0)
        set_rot("chest", 0, -sway*0.6, 0)
        set_rot("head", nod, 0, -sway*0.25)
        set_rot("upper_arm_L", 0, 0, 0.05)
        set_rot("upper_arm_R", 0, 0, -0.05)
        keyframe_pose()
    finish(a, "REPEAT")

def clip_walk():
    a = new_action("Walk", 30)
    samples = [
        (1,  0.00,  0.32, -0.34, 0.20),
        (8,  0.08, -0.08,  0.08,-0.10),
        (16,-0.08,-0.32,  0.34,-0.20),
        (23, 0.08, 0.08, -0.08, 0.10),
        (30, 0.00, 0.32, -0.34, 0.20),
    ]
    for f, hip, legL, legR, armv in samples:
        frame(f)
        set_rot("hips", 0, hip*0.35, 0)
        set_rot("upper_leg_L", legL, 0, 0)
        set_rot("upper_leg_R", legR, 0, 0)
        set_rot("lower_leg_L", -legL*0.45 if legL < 0 else -legL*0.18, 0, 0)
        set_rot("lower_leg_R", -legR*0.45 if legR < 0 else -legR*0.18, 0, 0)
        set_rot("upper_arm_L", -armv, 0, 0)
        set_rot("upper_arm_R", armv, 0, 0)
        set_rot("head", 0, -hip*0.25, 0)
        keyframe_pose()
    finish(a, "REPEAT")

def clip_run():
    a = new_action("Run", 18)
    samples = [(1,0.34,-0.60,0.62),(5,-0.10,0.45,-0.50),(10,-0.34,0.60,-0.62),(14,0.10,-0.45,0.50),(18,0.34,-0.60,0.62)]
    for f, torso, legL, legR in samples:
        frame(f)
        set_rot("spine", torso*0.16, 0, 0)
        set_rot("chest", torso*0.10, 0, 0)
        set_rot("hips", 0, torso*0.30, 0)
        set_rot("upper_leg_L", legL, 0, 0)
        set_rot("upper_leg_R", legR, 0, 0)
        set_rot("lower_leg_L", -abs(legL)*0.20, 0, 0)
        set_rot("lower_leg_R", -abs(legR)*0.20, 0, 0)
        set_rot("upper_arm_L", legR*0.75, 0, 0)
        set_rot("upper_arm_R", legL*0.75, 0, 0)
        set_rot("head", -torso*0.08, 0, 0)
        keyframe_pose()
    finish(a, "REPEAT")

def clip_jump():
    a = new_action("Jump", 36)
    poses = [
        (1,  (0.20,0,0), 0.35, -0.50,  0.40),
        (8, (-0.10,0,0),-0.15,  0.18, -0.30),
        (18,(0.00,0,0), 0.08, -0.08,  0.12),
        (28,(-0.18,0,0),0.15, -0.10,  0.18),
        (36,(0.20,0,0),0.34, -0.34,  0.28),
    ]
    for f, torso, legL, legR, armv in poses:
        frame(f)
        set_rot("spine", *torso)
        set_rot("upper_leg_L", legL, 0, 0)
        set_rot("upper_leg_R", legR, 0, 0)
        set_rot("lower_leg_L", -legL*0.35, 0, 0)
        set_rot("lower_leg_R", -legR*0.35, 0, 0)
        set_rot("upper_arm_L", armv, 0, 0)
        set_rot("upper_arm_R", armv, 0, 0)
        set_loc("hips", 0, 0, 0.08 if f in (8,18,28) else 0)
        keyframe_pose()
    finish(a)

def clip_attack():
    a = new_action("Attack_Combo", 36)
    poses = [
        (1, 0.0,  0.15,-0.15, 0.05),
        (8, 0.20, -0.20,0.25, -0.40),
        (14,-0.25, 0.75,-0.20, -0.90),
        (20, 0.12,-0.45,0.35, 0.70),
        (27,-0.08, 0.60,-0.15, -0.65),
        (36, 0.0,  0.15,-0.15, 0.05),
    ]
    for f, twist, armL, armR, torso in poses:
        frame(f)
        set_rot("spine", 0, twist, torso*0.20)
        set_rot("chest", 0, twist*0.65, torso*0.12)
        set_rot("upper_arm_L", armL, 0, 0)
        set_rot("upper_arm_R", armR, 0, 0)
        set_rot("lower_arm_L", -armL*0.25, 0, 0)
        set_rot("lower_arm_R", -armR*0.25, 0, 0)
        set_rot("hips", 0, -twist*0.35, 0)
        keyframe_pose()
    finish(a)

def clip_block_hit():
    a = new_action("Block_Hit", 24)
    poses = [
        (1, 0.0, 0.20),
        (7, 0.10, 0.80),
        (13,-0.16,-0.35),
        (18, 0.08, 0.15),
        (24, 0.0, 0.0),
    ]
    for f, bend, guard in poses:
        frame(f)
        set_rot("spine", bend*0.8, 0, 0)
        set_rot("upper_arm_L", -guard, 0, 0)
        set_rot("upper_arm_R", -guard, 0, 0)
        set_rot("head", bend*0.35, 0, 0)
        set_loc("hips", 0, -abs(bend)*0.02, 0)
        keyframe_pose()
    finish(a)

clip_idle()
clip_walk()
clip_run()
clip_jump()
clip_attack()
clip_block_hit()

# Restore the original pose as the active action while keeping all generated actions.
arm.animation_data.action = bpy.data.actions.get("Idle")
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=os.path.abspath("generated_animation_source.blend"))

# Select all imported objects for export.
bpy.ops.object.select_all(action="SELECT")
for o in bpy.context.selected_objects:
    o.hide_viewport = False
    o.hide_render = False

# Export all actions while preserving the imported mesh/armature hierarchy.
bpy.ops.export_scene.fbx(
    filepath=os.path.abspath(OUTPUT),
    use_selection=True,
    object_types={"ARMATURE", "MESH", "EMPTY"},
    apply_unit_scale=False,
    apply_scale_options="FBX_SCALE_NONE",
    bake_anim=True,
    bake_anim_use_all_actions=True,
    bake_anim_use_nla_strips=False,
    bake_anim_force_startend_keying=True,
    bake_anim_simplify_factor=0.0,
    add_leaf_bones=False,
    primary_bone_axis="Y",
    secondary_bone_axis="X",
)

if not os.path.exists(OUTPUT) or os.path.getsize(OUTPUT) < 10000:
    raise RuntimeError("Animated FBX export failed or produced an unexpectedly small file.")

print("ANIMATION_BUILD_OK", OUTPUT, os.path.getsize(OUTPUT))
print("ACTIONS:", [a.name for a in bpy.data.actions if a.name in {"Idle","Walk","Run","Jump","Attack_Combo","Block_Hit"}])
