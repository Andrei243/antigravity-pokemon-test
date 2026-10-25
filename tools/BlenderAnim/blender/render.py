# Renders each clip of a rigged .glb at evenly spaced moments, side by side on one sheet a clip
# (<out>/<clip>.png), seen from the model's front right as the battle sees a foe.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import rig
from rig import bpy, Vector
import numpy as np

j = rig.job()
arm = rig.load(j["input"])
out, per, size = j["out"], max(2, j.get("frames", 6)), j.get("size", 400)
only = set(j.get("only") or [])
os.makedirs(out, exist_ok=True)

scene = bpy.context.scene
scene.render.engine = "BLENDER_WORKBENCH"
scene.display.shading.light = "STUDIO"
scene.display.shading.color_type = "TEXTURE"
scene.display.shading.show_shadows = True
scene.render.resolution_x = scene.render.resolution_y = size
scene.world = bpy.data.worlds.new("sky")

lo, hi = rig.bounds()
height = max(hi.z - lo.z, 1e-3)
reach = max(hi.x - lo.x, hi.y - lo.y, height)
middle = (lo + hi) / 2
bpy.ops.mesh.primitive_plane_add(size=reach * 8, location=(middle.x, middle.y, lo.z))

cam = bpy.data.objects.new("camera", bpy.data.cameras.new("camera"))
scene.collection.objects.link(cam)
scene.camera = cam
cam.data.lens = 50
# Far enough back that a lunge, a hop or a fall stays in the picture
target = Vector((middle.x, middle.y, lo.z + height * 0.45))
cam.location = target + Vector((-1.5, -2.0, 1.0)).normalized() * reach * 2.6
cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()

arm.animation_data_create()
for track in list(arm.animation_data.nla_tracks):
    arm.animation_data.nla_tracks.remove(track)

sheets = []
for act in list(bpy.data.actions):
    if only and act.name not in only:
        continue
    arm.animation_data.action = act
    a, b = act.frame_range
    tiles = []
    for i in range(per):
        f = a + (b - a) * i / (per - 1)
        scene.frame_set(int(f), subframe=f - int(f))
        path = os.path.join(out, f"_frame_{i}.png")
        scene.render.filepath = path
        bpy.ops.render.render(write_still=True)
        img = bpy.data.images.load(path)
        tiles.append(np.array(img.pixels[:]).reshape(img.size[1], img.size[0], 4))
        bpy.data.images.remove(img)
        os.remove(path)
    sheet = np.concatenate(tiles, axis=1)
    h, w = sheet.shape[:2]
    im = bpy.data.images.new(act.name, w, h)
    im.pixels = sheet.ravel()
    im.filepath_raw = os.path.join(out, f"{act.name}.png")
    im.file_format = "PNG"
    im.save()
    sheets.append({"clip": act.name, "file": im.filepath_raw, "seconds": round((b - a) / scene.render.fps, 3)})

rig.report(j["report"], {"sheets": sheets})
