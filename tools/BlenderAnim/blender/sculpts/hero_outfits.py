# Dresses the hero's file (sculpts/hero.py) in several outfits and renders them on one board, each from the front
# and from behind, with an arm raised and a knee bent so that a garment is seen against the body under it. It reads
# the file as an engine would: pieces by their names, zones filled from each outfit's colours, and the body's parts
# a garment says it hides left out. It also prints what the file holds (HO: lines).
#   blender --background --factory-startup --python sculpts/hero_outfits.py -- <hero.glb> <board.png> [size]
import bpy, math, os, sys
import numpy as np
from mathutils import Matrix, Vector

a = sys.argv[sys.argv.index("--") + 1:]
src, out = a[0], a[1]
size = int(a[2]) if len(a) > 2 else 420


def srgb(h):
    h = h.lstrip("#")
    c = np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)])
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


LIGHT, INK = srgb("#F6F6FA"), srgb("#2A2438")
# An outfit: who (skin, hair, eyes) and each piece worn with its colour and accent
OUTFITS = [
    ("his own", dict(skin="#F8D3AE", hair="#46384E", eyes="#5A5578"), {
        "hair.Short": None, "hat.Beret": ("#DC383C", "#F6F6FA"), "top.Jacket": ("#3A4E8A", "#EC4646"),
        "bottoms.Trousers": ("#303246", None), "shoes.Sneakers": ("#C8463C", "#F0ECE4"),
        "bag.Backpack": ("#F2C446", "#E2B030"), "extra.Scarf": ("#EC4646", None)}),
    ("summer", dict(skin="#F8D3AE", hair="#46384E", eyes="#5A5578"), {
        "hair.Short": None, "hat.Cap": ("#3E6ED0", "#F6D24A"), "top.Tee": ("#F6F6F8", "#3E6ED0"),
        "bottoms.Shorts": ("#C8A26A", "#F6F6F8"), "shoes.Sneakers": ("#F6F6F8", "#3E6ED0"),
        "bag.Backpack": ("#4E8C5A", "#26603C")}),
    ("forest", dict(skin="#C68E62", hair="#B5532A", eyes="#3C7A4A"), {
        "hair.Short": None, "hat.Beret": ("#8CD8B8", "#F6F6F8"), "top.Jacket": ("#4E8C5A", "#26603C"),
        "bottoms.Shorts": ("#5A4632", "#5A4632"), "shoes.Sneakers": ("#5A4632", "#E8E0D0")}),
    ("plain", dict(skin="#F2C09A", hair="#E8C66A", eyes="#3E6ED0"), {
        "hair.Short": None, "top.Tee": ("#F6C83C", "#F6C83C"), "bottoms.Trousers": ("#3E6ED0", None),
        "shoes.Sneakers": ("#2C2C36", "#E84848")}),
]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
shapes = {b.custom_shape for o in bpy.data.objects if o.type == "ARMATURE" for b in o.pose.bones if b.custom_shape}
for o in shapes:
    bpy.data.objects.remove(o, do_unlink=True)
arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
pieces = {o.name: o for o in bpy.data.objects if o.type == "MESH"}

# What the file holds, as it came back through the exporter and the importer
masks = {}
for name, o in sorted(pieces.items()):
    me = o.data
    col = np.empty(len(me.loops) * 4, np.float32)
    me.color_attributes[0].data.foreach_get("color", col)
    col = col.reshape(-1, 4)[:, :3].copy()
    masks[name] = col
    pure = float(((col < 0.02) | (col > 0.98)).all(axis=1).mean())
    rig = [m.object.name for m in o.modifiers if m.type == "ARMATURE"]
    print("HO:", name, len(me.vertices), "vertices; extras", {k: o[k] for k in o.keys()}, "armature", rig,
          "pure zones", round(pure, 3), "groups", len(o.vertex_groups))

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE_NEXT"
scene.render.resolution_x = scene.render.resolution_y = size
scene.view_settings.view_transform = "Standard"
world = bpy.data.worlds.new("sky"); scene.world = world; world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.62, 0.66, 0.72, 1)
sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
sun.data.energy = 3.0; sun.rotation_euler = (math.radians(50), 0, math.radians(-30))
scene.collection.objects.link(sun)
cam = bpy.data.objects.new("camera", bpy.data.cameras.new("camera"))
scene.collection.objects.link(cam); scene.camera = cam
cam.data.type = "ORTHO"; cam.data.ortho_scale = 1.6
middle = Vector((0, 0, 0.64))


def turn(bone, axis, degrees):
    """Turns a bone about a world axis through its own joint, whatever way the bone itself lies."""
    pb = arm.pose.bones[bone]
    head = pb.matrix.translation.copy()
    pb.matrix = Matrix.Translation(head) @ Matrix.Rotation(math.radians(degrees), 4, axis) @ Matrix.Translation(-head) @ pb.matrix
    bpy.context.view_layer.update()


# An arm out to the side and up, the other forearm bent, a knee lifted
turn("upperArm.L", "Y", 70)
turn("foreArm.R", "X", -55)
turn("thigh.R", "X", -45)
turn("shin.R", "X", 50)

tiles = []
for label, who, worn in OUTFITS:
    hidden = set()
    for name in worn:
        hidden |= {"body." + part for part in str(pieces[name].get("hides", "")).split(",") if part}
    for name, o in pieces.items():
        show = (name in worn or name.startswith("body.") or name.startswith("face.")) and name not in hidden
        o.hide_render = not show
        if not show or o.get("colours") != "zones":
            continue
        tint = o.get("tint")
        primary, accent = (who[tint], None) if tint in who else worn[name]
        p = srgb(primary)
        acc = srgb(accent) if accent else p
        m = masks[name]
        rest = np.clip(1 - m.sum(axis=1, keepdims=True), 0, 1)
        rgb = m[:, 0:1] * p + m[:, 1:2] * acc + m[:, 2:3] * LIGHT + rest * INK
        o.data.color_attributes[0].data.foreach_set(
            "color", np.concatenate([rgb, np.ones((len(rgb), 1))], axis=1).astype(np.float32).ravel())
        o.data.update()
    print("HO: outfit", label, "wears", sorted(worn), "hides", sorted(hidden))
    for degrees in (25, 205):
        r = math.radians(degrees)
        cam.location = middle + Vector((math.sin(r), -math.cos(r), 0.2)).normalized() * 5
        cam.rotation_euler = (middle - cam.location).to_track_quat("-Z", "Y").to_euler()
        path = f"{out}.{label}.{degrees}.png"
        scene.render.filepath = path
        bpy.ops.render.render(write_still=True)
        img = bpy.data.images.load(path)
        tiles.append(np.array(img.pixels[:]).reshape(img.size[1], img.size[0], 4))
        bpy.data.images.remove(img)
        os.remove(path)
rows = [np.concatenate(tiles[i:i + 4], axis=1) for i in range(0, len(tiles), 4)]
sheet = np.concatenate(rows[::-1], axis=0)
im = bpy.data.images.new("board", sheet.shape[1], sheet.shape[0])
im.pixels = sheet.ravel(); im.filepath_raw = out; im.file_format = "PNG"; im.save()
print("HO: wrote", out)
