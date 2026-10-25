# Writes what a rigged .glb holds: its bones, the bones found for each role, its clips and its size.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import rig

j = rig.job()
arm = rig.load(j["input"])
lo, hi = rig.bounds()
world = arm.matrix_world
rig.report(j["report"], {
    "bones": [{"name": b.name, "parent": b.parent.name if b.parent else None,
               "head": [round(v, 4) for v in world @ b.head_local]} for b in arm.data.bones],
    "roles": rig.find_roles(arm, j.get("map")),
    "actions": [{"name": a.name, "frames": list(a.frame_range)} for a in rig.bpy.data.actions],
    "size": [round(v, 4) for v in hi - lo],
    "fps": rig.bpy.context.scene.render.fps,
})
