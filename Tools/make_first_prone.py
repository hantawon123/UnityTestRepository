"""Build First 포복 clips: belly on the floor, hands planted, no rifle pose.

Blender --background --python Tools/make_first_prone.py -- IDLE.fbx CRAWL.fbx OUT_DIR
"""
import math
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector


idle_path, crawl_path, out_dir = sys.argv[sys.argv.index("--") + 1 :]
out_dir = Path(out_dir)
out_dir.mkdir(parents=True, exist_ok=True)


def sample_basis(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=path, use_anim=True, ignore_leaf_bones=False)
    rig = next(obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE")
    action = rig.animation_data.action
    first, last = (int(action.frame_range[0]), int(action.frame_range[1]))
    samples = {}
    for frame in range(first, last + 1):
        bpy.context.scene.frame_set(frame)
        bpy.context.view_layer.update()
        samples[frame] = {bone.name: bone.matrix_basis.copy() for bone in rig.pose.bones}
    return samples, first, last


def apply_world_rotation(bone, world_rot):
    axes = bone.matrix.to_3x3()
    local = axes.inverted() @ world_rot.to_matrix() @ axes
    bone.matrix_basis = bone.matrix_basis @ local.to_4x4()


def aim_bone(bone, desired_dir, weight=1.0):
    current = bone.tail - bone.head
    if current.length < 1e-8 or weight <= 0:
        return
    blended = current.normalized().lerp(desired_dir.normalized(), weight)
    if blended.length < 1e-8:
        return
    apply_world_rotation(bone, current.normalized().rotation_difference(blended.normalized()))


def lerp_matrix(a, b, t):
    a_loc, a_rot, a_scale = a.decompose()
    b_loc, b_rot, b_scale = b.decompose()
    return Matrix.LocRotScale(a_loc.lerp(b_loc, t), a_rot.slerp(b_rot, t), a_scale.lerp(b_scale, t))


def smooth(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3.0 - 2.0 * t)


def key_pose(rig, frame):
    for bone in rig.pose.bones:
        bone.rotation_mode = "QUATERNION"
        bone.keyframe_insert("location", frame=frame, group=bone.name)
        bone.keyframe_insert("rotation_quaternion", frame=frame, group=bone.name)
        bone.keyframe_insert("scale", frame=frame, group=bone.name)


def export_fbx(rig, path, first, last):
    scene = bpy.context.scene
    scene.frame_start = first
    scene.frame_end = last
    scene.frame_set(first)
    bpy.ops.object.select_all(action="DESELECT")
    meshes = [obj for obj in scene.objects if obj.type == "MESH"]
    for obj in [rig, *meshes]:
        obj.hide_set(False)
        obj.select_set(True)
    bpy.context.view_layer.objects.active = rig
    path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=str(path),
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        use_mesh_modifiers=False,
        add_leaf_bones=False,
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=False,
        bake_anim_force_startend_keying=True,
        bake_anim_step=1,
        bake_anim_simplify_factor=0,
        axis_forward="-Z",
        axis_up="Y",
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        armature_nodetype="NULL",
        primary_bone_axis="Y",
        secondary_bone_axis="X",
        use_armature_deform_only=False,
        mesh_smooth_type="FACE",
        path_mode="COPY",
        embed_textures=True,
    )
    print("EXPORTED", path, first, last)


crawl_samples, crawl_first, crawl_last = sample_basis(crawl_path)
crawl_span = max(1, crawl_last - crawl_first)
crawl_ref = crawl_samples[crawl_first]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=idle_path, use_anim=True, ignore_leaf_bones=False)
scene = bpy.context.scene
rig = next(obj for obj in scene.objects if obj.type == "ARMATURE")
body = bpy.data.objects.get("Body")
scene.frame_set(int(rig.animation_data.action.frame_range[0]))
bpy.context.view_layer.update()
idle_basis = {bone.name: bone.matrix_basis.copy() for bone in rig.pose.bones}
ordered = sorted(rig.pose.bones, key=lambda bone: len(bone.parent_recursive))
for bone in ordered:
    bone.rotation_mode = "QUATERNION"
    bone.matrix_basis = idle_basis[bone.name]
bpy.context.view_layer.update()

# Belly-down: spine points forward (+Z), legs trail backward.
rig.pose.bones["Hips"].matrix_basis = idle_basis["Hips"] @ Matrix.Rotation(math.pi / 2.0, 4, "X")
bpy.context.view_layer.update()
hips = rig.pose.bones["Hips"]
hips.location.y += 0.26 - hips.head.y
bpy.context.view_layer.update()

floor_y = 0.10


def plant_end(control_name, end_name, target_y):
    control = rig.pose.bones[control_name]
    end = rig.pose.bones[end_name]
    lift = target_y - end.head.y
    if abs(lift) < 0.001:
        return
    current = control.tail - control.head
    target = Vector((current.x, current.y + lift, current.z))
    if target.length > 1e-8:
        apply_world_rotation(control, current.normalized().rotation_difference(target.normalized()))
    bpy.context.view_layer.update()


# Face currently points at the floor after the belly-down hip pitch; rotate it forward.
aim_bone(rig.pose.bones["Spine"], Vector((0.0, 0.10, 0.995)))
bpy.context.view_layer.update()
aim_bone(rig.pose.bones["Neck"], Vector((0.0, 0.22, 0.975)))
bpy.context.view_layer.update()
aim_bone(rig.pose.bones["Head"], Vector((0.0, 0.18, 0.984)))
bpy.context.view_layer.update()
apply_world_rotation(rig.pose.bones["Neck"], Matrix.Rotation(math.radians(-38.0), 4, "X").to_quaternion())
bpy.context.view_layer.update()
apply_world_rotation(rig.pose.bones["Head"], Matrix.Rotation(math.radians(-8.0), 4, "X").to_quaternion())
bpy.context.view_layer.update()
# ㄴ arms: upper arm down toward the floor, forearm forward along the ground.
aim_bone(rig.pose.bones["UpperArm.L"], Vector((0.28, -0.70, 0.66)))
bpy.context.view_layer.update()
aim_bone(rig.pose.bones["UpperArm.R"], Vector((-0.28, -0.70, 0.66)))
bpy.context.view_layer.update()
aim_bone(rig.pose.bones["Arm.L"], Vector((0.12, 0.02, 0.993)))
bpy.context.view_layer.update()
aim_bone(rig.pose.bones["Arm.R"], Vector((-0.12, 0.02, 0.993)))
bpy.context.view_layer.update()
for side in ("L", "R"):
    if f"Finger_T1.{side}" in rig.pose.bones:
        aim_bone(rig.pose.bones[f"Finger_T1.{side}"], Vector((0.0, -0.08, 0.997)), 0.5)
        bpy.context.view_layer.update()
aim_bone(rig.pose.bones["UpperLeg.L"], Vector((0.12, -0.18, -0.976)))
bpy.context.view_layer.update()
aim_bone(rig.pose.bones["UpperLeg.R"], Vector((-0.12, -0.18, -0.976)))
bpy.context.view_layer.update()
aim_bone(rig.pose.bones["Leg.L"], Vector((0.04, -0.22, -0.975)))
bpy.context.view_layer.update()
aim_bone(rig.pose.bones["Leg.R"], Vector((-0.04, -0.22, -0.975)))
bpy.context.view_layer.update()
aim_bone(rig.pose.bones["Foot.L"], Vector((0.06, -0.52, -0.85)))
bpy.context.view_layer.update()
aim_bone(rig.pose.bones["Foot.R"], Vector((-0.06, -0.52, -0.85)))
bpy.context.view_layer.update()
for side in ("L", "R"):
    toe = rig.pose.bones.get(f"FootToe1.{side}")
    if toe is not None:
        aim_bone(toe, Vector((0.06 if side == "L" else -0.06, -0.58, -0.81)))
        bpy.context.view_layer.update()
        plant_end(f"Foot.{side}", f"FootToe1.{side}", floor_y)
    else:
        plant_end(f"Leg.{side}", f"Foot.{side}", floor_y)

prone_bind = {bone.name: bone.matrix_basis.copy() for bone in rig.pose.bones}
leg_bones = [
    name for name in prone_bind
    if name.startswith("UpperLeg") or name.startswith("Leg.")
]


def crawl_delta(frame, last, reverse=False):
    progress = (frame - 1) / max(1, last - 1)
    if reverse:
        progress = 1.0 - progress
    source = crawl_first + progress * crawl_span
    low = int(math.floor(source))
    high = min(crawl_last, low + 1)
    t = source - low
    low = max(crawl_first, min(crawl_last, low))
    high = max(crawl_first, min(crawl_last, high))
    return low, high, t


def apply_crawl(frame, last, reverse=False, sway=0.0):
    low, high, t = crawl_delta(frame, last, reverse)
    for bone in ordered:
        bind = prone_bind[bone.name]
        if bone.name in leg_bones and bone.name in crawl_ref:
            delta = crawl_ref[bone.name].inverted() @ lerp_matrix(
                crawl_samples[low][bone.name], crawl_samples[high][bone.name], t
            )
            bone.matrix_basis = bind @ lerp_matrix(Matrix.Identity(4), delta, 0.40)
        else:
            bone.matrix_basis = bind
    bpy.context.view_layer.update()
    phase = math.sin((frame - 1) / max(1, last - 1) * math.pi * 2.0)
    if reverse:
        phase = -phase
    for side, sign in (("L", 1.0), ("R", -1.0)):
        apply_world_rotation(
            rig.pose.bones[f"UpperArm.{side}"],
            Matrix.Rotation(phase * sign * 0.18, 4, "X").to_quaternion(),
        )
        bpy.context.view_layer.update()
        plant_end(f"Arm.{side}", f"Hand.{side}", floor_y)
    if sway:
        hips = rig.pose.bones["Hips"]
        hips.location.x += phase * sway
        bpy.context.view_layer.update()


def write_action(name, last, filler):
    action = bpy.data.actions.new(name)
    action.use_fake_user = True
    rig.animation_data_create().action = action
    if action.slots:
        rig.animation_data.action_slot = action.slots[0]
    for frame in range(1, last + 2):
        filler(frame, last)
        key_pose(rig, frame)
        if body and body.data.shape_keys:
            for key in body.data.shape_keys.key_blocks[1:]:
                key.value = 0.0
                key.keyframe_insert("value", frame=frame)
    return action


def idle_fill(frame, last):
    for bone in ordered:
        bone.matrix_basis = prone_bind[bone.name]
    bpy.context.view_layer.update()
    pulse = math.sin((frame - 1) / 60.0 * math.pi * 2.0) * 0.02
    hips = rig.pose.bones["Hips"]
    hips.location.y += pulse * 0.4
    bpy.context.view_layer.update()


def start_fill(frame, last):
    t = smooth((frame - 1) / max(1, last - 1))
    for bone in ordered:
        bone.matrix_basis = lerp_matrix(idle_basis[bone.name], prone_bind[bone.name], t)
    bpy.context.view_layer.update()


def end_fill(frame, last):
    t = smooth((frame - 1) / max(1, last - 1))
    for bone in ordered:
        bone.matrix_basis = lerp_matrix(prone_bind[bone.name], idle_basis[bone.name], t)
    bpy.context.view_layer.update()


clips = [
    ("Prone_Idle", 60, idle_fill),
    ("Crawl_Forward", 36, lambda frame, last: apply_crawl(frame, last)),
    ("Crawl_Back", 36, lambda frame, last: apply_crawl(frame, last, reverse=True)),
    ("Crawl_Left", 36, lambda frame, last: apply_crawl(frame, last, sway=0.03)),
    ("Crawl_Right", 36, lambda frame, last: apply_crawl(frame, last, sway=-0.03)),
    ("Prone_Start", 24, start_fill),
    ("Prone_End", 24, end_fill),
]

for name, last, filler in clips:
    write_action(name, last, filler)
    export_fbx(rig, out_dir / f"FirstPlayerCapsule_{name}.fbx", 1, last)

print("PRONE_CLIPS", out_dir)
