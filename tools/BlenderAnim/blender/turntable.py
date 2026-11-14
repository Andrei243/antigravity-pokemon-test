# A still turntable of a .glb to look at a sculpt: its front, front three-quarter, side and back on one sheet, and
# after them the two views the game's own boards take (`dex` and `dex --back`: 10 degrees up, 31.5 degrees round to
# one side from the front, and from behind over the other shoulder), in the file's own colours (vertex colours and
# textures, through the materials the importer makes). A face seen from its inner side is drawn in ink, so a piece
# wound inside out, which the game shows grey and see-through, is black here. Run by Blender itself:
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

# The game draws a face from its outer side alone and its outline from the inner. Blender draws both sides alike
# unless told, and a piece wound inside out then looks right here and thin, grey and see-through there (Togetic's
# wings, plan 48): so a face seen from its inner side is drawn here in ink
INK = (0.05, 0.05, 0.06, 1)
for m in bpy.data.materials:
    tree = m.node_tree if m.use_nodes else None
    end = next((n for n in tree.nodes if n.type == "OUTPUT_MATERIAL" and n.is_active_output), None) if tree else None
    if end is None or not end.inputs["Surface"].is_linked:
        m.use_backface_culling = True
        continue
    # A face seen from its inner side is drawn in ink, which is what the game's outline pass makes of it
    was = end.inputs["Surface"].links[0].from_socket
    side = tree.nodes.new("ShaderNodeNewGeometry")
    ink = tree.nodes.new("ShaderNodeEmission")
    ink.inputs["Color"].default_value = INK
    mix = tree.nodes.new("ShaderNodeMixShader")
    tree.links.new(side.outputs["Backfacing"], mix.inputs[0])
    tree.links.new(was, mix.inputs[1])
    tree.links.new(ink.outputs[0], mix.inputs[2])
    tree.links.new(mix.outputs[0], end.inputs["Surface"])
    m.use_backface_culling = False
scene.display.shading.show_backface_culling = True

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
# The last two are the game's: PokemonStudio turns the model by -0.55 rad (2.6 from behind) under a camera 10 degrees up
GAME = math.tan(math.radians(10))
tiles = []
for name, degrees, rise in (("front", 0, 0.25), ("threeq", 40, 0.25), ("side", 90, 0.25), ("back", 180, 0.25),
                            ("game", math.degrees(0.55), GAME), ("gameback", math.degrees(-2.6), GAME)):
    a = math.radians(degrees)
    cam.location = middle + Vector((math.sin(a), -math.cos(a), rise)).normalized() * reach * 4
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
