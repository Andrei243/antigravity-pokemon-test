# Reads a .glb and writes it out again as the game reads it, with an edit in between if one is given: the way a
# model exported by the harness (`ShotHarness export`) is changed in Blender with no window. Run by Blender itself:
#   blender --background --factory-startup --python roundtrip.py -- <in.glb> <out.glb> [edit.py]
# The edit is a Python file run after the import with `bpy`, `np`, `meshes` (the mesh objects) and `arm` (the
# armature or None) in its globals. With no edit the file comes back as it went in, which is how the path is checked.
import bpy, sys
import numpy as np

args = sys.argv[sys.argv.index("--") + 1:]
src, out = args[0], args[1]
edit = args[2] if len(args) > 2 else None

bpy.ops.wm.read_factory_settings(use_empty=True)
# The importer keys clips on whole frames of the scene's rate. At 60 a second every clip of the game's lasts a whole
# number of frames; at Blender's 24 a cheer of 28.8 frames came back 28 long
bpy.context.scene.render.fps = 60
bpy.ops.import_scene.gltf(filepath=src)
shapes = {b.custom_shape for o in bpy.data.objects if o.type == "ARMATURE" for b in o.pose.bones if b.custom_shape}
for o in shapes:
    bpy.data.objects.remove(o, do_unlink=True)

meshes = [o for o in bpy.data.objects if o.type == "MESH"]
arm = next((o for o in bpy.data.objects if o.type == "ARMATURE"), None)
print("RT: clips", [(a.name, tuple(a.frame_range)) for a in bpy.data.actions])

if edit:
    with open(edit, encoding="utf-8") as f:
        exec(compile(f.read(), edit, "exec"), {"bpy": bpy, "np": np, "meshes": meshes, "arm": arm})

bpy.ops.export_scene.gltf(
    filepath=out, export_format="GLB", export_yup=True,
    # The body's colours are its vertex colours, wired into its material by the importer
    export_vertex_color="MATERIAL", export_all_vertex_colors=False,
    export_skins=True, export_animations=True, export_animation_mode="ACTIONS", export_force_sampling=True,
    # What the game doesn't read (docs/model-files.md)
    export_draco_mesh_compression_enable=False, export_morph=False,
    export_cameras=False, export_lights=False)
print("RT: wrote", out)
