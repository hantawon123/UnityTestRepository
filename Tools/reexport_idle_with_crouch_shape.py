"""Add the crouch correction to the existing Unity idle FBX without losing breathing."""
import sys
from pathlib import Path
import bpy

source, destination = sys.argv[sys.argv.index("--") + 1:]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=source, use_anim=True, ignore_leaf_bones=False)
body = bpy.data.objects["Body"]
keys = body.data.shape_keys
print("IMPORTED_KEYS", [key.name for key in keys.key_blocks])
key = keys.key_blocks.get("Crouch_Groin_Flat") or body.shape_key_add(name="Crouch_Groin_Flat")
basis = keys.key_blocks[0]
for point, base in zip(key.data, basis.data):
    point.co = base.co
key.value = 0.0
key.keyframe_insert("value", frame=0)
key.keyframe_insert("value", frame=60)
for action in bpy.data.actions:
    for layer in action.layers:
        for strip in layer.strips:
            for slot in action.slots:
                bag = strip.channelbag(slot)
                if bag:
                    for curve in bag.fcurves:
                        for point in curve.keyframe_points:
                            point.interpolation = "LINEAR"
bpy.ops.object.select_all(action="DESELECT")
for obj in bpy.context.scene.objects:
    if obj.type in {"ARMATURE", "MESH"}:
        obj.select_set(True)
Path(destination).parent.mkdir(parents=True, exist_ok=True)
bpy.ops.export_scene.fbx(filepath=destination, use_selection=True,
    object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False, bake_anim=True,
    bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False,
    bake_anim_use_all_actions=False, bake_anim_force_startend_keying=True,
    bake_anim_step=1, bake_anim_simplify_factor=0,
    axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_ALL', armature_nodetype='NULL',
    primary_bone_axis='Y', secondary_bone_axis='X', use_armature_deform_only=False,
    mesh_smooth_type='FACE', path_mode='COPY', embed_textures=True)
print("EXPORTED", destination)
