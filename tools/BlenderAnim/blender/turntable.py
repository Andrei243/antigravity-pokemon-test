# A still turntable of a .glb to look at a sculpt: its front, front three-quarter, side and back on one sheet, in the
# file's own colours (vertex colours and textures, through the materials the importer makes). Run by Blender itself:
#   blender --background --factory-startup --python turntable.py -- <in.glb> <out.png> [size] [engine] [eyes]
# It prints what the file holds (TT: lines): meshes, colour attributes, materials, clips and bounds.
import bpy, math, os, sys
import numpy as np
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:]
src, out = args[0], args[1]
size = int(args[2]) if len(args) > 2 else 480
# EEVEE renders with no window on Windows; "BLENDER_WORKBENCH" is the fallback where it can't
engine = args[3] if len(args) > 3 else "BLENDER_EEVEE_NEXT"
# Which of the eyes' five states to show (Open, Shut, Squeeze, Fierce, Smile)
eyes = (args[4] if len(args) > 4 else "Open").lower()

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
# The importer draws bones with an icosphere of its own, which is a mesh in the scene
shapes = {b.custom_shape for o in bpy.data.objects if o.type == "ARMATURE" for b in o.pose.bones if b.custom_shape}
for o in shapes:
    bpy.data.objects.remove(o, do_unlink=True)

# The sculpt as it was made, not the first frame of whichever clip the importer left playing
for o in bpy.data.objects:
    if o.type == "ARMATURE":
        o.data.pose_position = "REST"
bpy.context.view_layer.update()

# Of the eyes' five states (pieces named eyes.<State>) only the open ones, as the game draws a model at rest
for o in bpy.data.objects:
    if o.type == "MESH" and o.name.lower().startswith("eyes.") and not o.name.lower().startswith("eyes." + eyes):
        bpy.data.objects.remove(o, do_unlink=True)
meshes = [o for o in bpy.data.objects if o.type == "MESH"]
print("TT: meshes", [(o.name, len(o.data.vertices), len(o.data.polygons)) for o in meshes])
for o in meshes:
    print("TT: colours", o.name, [(a.name, a.domain, a.data_type) for a in o.data.color_attributes],
          "uv", [u.name for u in o.data.uv_layers], "materials", [m.name for m in o.data.materials if m])
print("TT: clips", [(a.name, tuple(a.frame_range)) for a in bpy.data.actions])

lo, hi = Vector((1e9,) * 3), Vector((-1e9,) * 3)
for o in meshes:
    for c in o.bound_box:
        p = o.matrix_world @ Vector(c)
        lo, hi = Vector(map(min, lo, p)), Vector(map(max, hi, p))
print("TT: bounds", tuple(round(v, 3) for v in lo), tuple(round(v, 3) for v in hi))
reach = max(hi.x - lo.x, hi.y - lo.y, hi.z - lo.z)
middle = (lo + hi) / 2

scene = bpy.context.scene
scene.render.engine = engine
scene.render.resolution_x = scene.render.resolution_y = size
# "Standard": the colours as painted. The default, Filmic or AgX, greys them
scene.view_settings.view_transform = "Standard"
world = bpy.data.worlds.new("sky")
scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.62, 0.66, 0.72, 1)
if engine == "BLENDER_WORKBENCH":
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "VERTEX"

sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
sun.data.energy = 3.0
sun.rotation_euler = (math.radians(50), 0, math.radians(-30))
scene.collection.objects.link(sun)

cam = bpy.data.objects.new("camera", bpy.data.cameras.new("camera"))
scene.collection.objects.link(cam)
scene.camera = cam
cam.data.type = "ORTHO"
cam.data.ortho_scale = reach * 1.25

# The game's front is glTF's +Z, which the importer makes Blender's -Y: a camera at -Y looks at the face
tiles = []
for name, degrees in (("front", 0), ("threeq", 40), ("side", 90), ("back", 180)):
    a = math.radians(degrees)
    cam.location = middle + Vector((math.sin(a), -math.cos(a), 0.25)).normalized() * reach * 4
    cam.rotation_euler = (middle - cam.location).to_track_quat("-Z", "Y").to_euler()
    path = f"{out}.{name}.png"
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    img = bpy.data.images.load(path)
    tiles.append(np.array(img.pixels[:]).reshape(img.size[1], img.size[0], 4))
    bpy.data.images.remove(img)
    os.remove(path)
sheet = np.concatenate(tiles, axis=1)
im = bpy.data.images.new("sheet", sheet.shape[1], sheet.shape[0])
im.pixels = sheet.ravel()
im.filepath_raw = out
im.file_format = "PNG"
im.save()
print("TT: wrote", out)
