# Keys the game's battle clips on a rigged .glb and exports it again with them.
#
# Each clip is a few poses at times in seconds, each pose what a role's bones do: "pitch" tips the front down
# (degrees), "roll" turns about the body's length, "yaw" about the vertical, and "move" shifts the root forward, up
# and to the side by shares of the model's height, so one table suits a model of any size. A role the rig has
# several bones for shares a turn between them (the spine) or gives each the whole of it (the legs). Between poses
# Blender's Bézier curves ease. The times follow the battle's (docs/model-files.md, "Animations"): a move takes 0.65 s
# and lands at 0.35 s, a dodge is furthest aside at 0.35 s, and the game stretches a clip over its own time anyway.
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import rig
from rig import bpy, Quaternion, Vector

j = rig.job()
arm = rig.load(j["input"])
FPS = 24
bpy.context.scene.render.fps = FPS
roles = rig.find_roles(arm, j.get("map"))
lo, hi = rig.bounds()
height = max(hi.z - lo.z, 1e-3)
A3 = arm.matrix_world.to_3x3()

dropped = []
for name in j.get("drop", []):
    act = bpy.data.actions.get(name)
    if act:
        bpy.data.actions.remove(act)
        dropped.append(name)
if arm.animation_data:
    arm.animation_data.action = None
    for track in list(arm.animation_data.nla_tracks):
        arm.animation_data.nla_tracks.remove(track)


def frame_of(bone):
    return (A3 @ arm.data.bones[bone].matrix_local.to_3x3()).normalized()


def turn(bone, axis, degrees):
    """A turn about an axis of the world, as the pose bone's own rotation."""
    m = frame_of(bone)
    r = Quaternion(axis, math.radians(degrees)).to_matrix()
    return (m.inverted() @ r @ m).to_quaternion()


AXES = {"pitch": rig.PITCH, "roll": rig.ROLL, "yaw": rig.UP}
SHARED = {"spine", "neck", "head", "tail"}   # roles whose turn is shared out between their bones


def bones_for(role):
    if role == "head" and not roles["head"]:
        return roles["neck"]
    return roles.get(role, [])


def pose_bones(pose):
    """The pose's turns and moves, bone by bone."""
    out = {}
    for role, parts in pose.items():
        bones = bones_for(role)
        if not bones:
            continue
        share = 1.0 / len(bones) if role in SHARED else 1.0
        for bone in bones:
            q, loc = out.get(bone, (Quaternion(), Vector()))
            for kind, value in parts.items():
                if kind == "move":
                    fwd, up, side = (v * height for v in value)
                    world = rig.FORWARD * fwd + rig.UP * up + rig.PITCH * side
                    loc = loc + frame_of(bone).inverted() @ world
                else:
                    q = turn(bone, AXES[kind], value * share) @ q
            out[bone] = (q, loc)
    return out


def make(name, keys):
    act = bpy.data.actions.new(name)
    arm.animation_data_create()
    arm.animation_data.action = act
    posed = [(t, pose_bones(p)) for t, p in keys]
    touched = sorted({b for _, p in posed for b in p})
    if not touched:
        bpy.data.actions.remove(act)
        return None
    for b in arm.pose.bones:
        b.rotation_mode = "QUATERNION"
    for t, bones in posed:
        frame = 1 + round(t * FPS)
        for b in touched:
            q, loc = bones.get(b, (Quaternion(), Vector()))
            pb = arm.pose.bones[b]
            pb.rotation_quaternion = q
            pb.location = loc
            pb.keyframe_insert("rotation_quaternion", frame=frame)
            pb.keyframe_insert("location", frame=frame)
    for fc in act.fcurves:
        for k in fc.keyframe_points:
            k.interpolation = "BEZIER"
    act.use_fake_user = True
    arm.animation_data.action = None
    for b in arm.pose.bones:
        b.rotation_quaternion = Quaternion()
        b.location = Vector()
    return act


REST = {"root": {"move": (0, 0, 0), "pitch": 0, "roll": 0}, "spine": {"pitch": 0}, "head": {"pitch": 0, "yaw": 0},
        "front": {"pitch": 0}, "back": {"pitch": 0}, "tail": {"pitch": 0, "yaw": 0}, "wings": {"roll": 0}}


def p(**roles_):
    """A pose: every role at rest but those given."""
    pose = {r: dict(v) for r, v in REST.items()}
    for r, v in roles_.items():
        pose[r].update(v)
    return pose


CLIPS = {
    # A breath, looped: the chest lifts, the head bobs, the tail sways
    "idle": [(0.0, p()),
             (1.0, p(root={"move": (0, 0.012, 0)}, spine={"pitch": -3}, head={"pitch": 4}, tail={"yaw": 6})),
             (2.0, p())],
    # A blow: gather back and low, lunge with the head down and the forefeet reaching, settle
    "physical": [(0.0, p()),
                 (0.18, p(root={"move": (-0.12, -0.05, 0)}, spine={"pitch": 6}, head={"pitch": -12}, front={"pitch": 10}, back={"pitch": -6})),
                 (0.35, p(root={"move": (0.26, 0.02, 0)}, spine={"pitch": -6}, head={"pitch": 22}, front={"pitch": -30}, back={"pitch": 14})),
                 (0.65, p())],
    # A move sent out: rear up, then thrust the head forward
    "special": [(0.0, p()),
                (0.2, p(root={"move": (-0.04, 0.03, 0)}, spine={"pitch": -12}, head={"pitch": -16}, front={"pitch": -10})),
                (0.35, p(root={"move": (0.08, 0, 0)}, spine={"pitch": 4}, head={"pitch": 12}, front={"pitch": -4})),
                (0.65, p())],
    # A move on itself: a crouch and a hop, head held high
    "status": [(0.0, p()),
               (0.15, p(root={"move": (0, -0.04, 0)}, spine={"pitch": 4}, front={"pitch": 6}, back={"pitch": -6})),
               (0.35, p(root={"move": (0, 0.12, 0)}, head={"pitch": -10}, front={"pitch": -10}, back={"pitch": 10})),
               (0.5, p(root={"move": (0, -0.02, 0)})),
               (0.65, p())],
    # Struck: knocked back with the head thrown up, then steady again
    "hit": [(0.0, p()),
            (0.06, p(root={"move": (-0.14, 0.03, 0)}, spine={"pitch": 8}, head={"pitch": -25}, tail={"pitch": -10})),
            (0.25, p(root={"move": (-0.06, 0, 0)}, spine={"pitch": 3}, head={"pitch": -8})),
            (0.55, p())],
    # Fainting: a sway, a sag, over onto its side, a small bounce, still
    "faint": [(0.0, p()),
              (0.25, p(root={"roll": -8, "move": (0, -0.04, 0)}, head={"pitch": 15})),
              (0.7, p(root={"roll": 80, "move": (0, -0.18, 0.12)}, head={"pitch": 30}, front={"pitch": -15}, back={"pitch": 15})),
              (0.82, p(root={"roll": 74, "move": (0, -0.17, 0.12)}, head={"pitch": 34}, front={"pitch": -15}, back={"pitch": 15})),
              (1.0, p(root={"roll": 80, "move": (0, -0.18, 0.12)}, head={"pitch": 34}, front={"pitch": -15}, back={"pitch": 15}))],
    # Out of the ball: rise up and cry out, the head tossed from side to side
    "entry": [(0.0, p()),
              (0.25, p(root={"move": (0, 0.03, 0)}, spine={"pitch": -6}, head={"pitch": -22}, front={"pitch": -8})),
              (0.45, p(root={"move": (0, 0.03, 0)}, spine={"pitch": -6}, head={"pitch": -24, "yaw": 10})),
              (0.6, p(root={"move": (0, 0.03, 0)}, spine={"pitch": -6}, head={"pitch": -24, "yaw": -10})),
              (0.8, p())],
    # A miss dodged: duck and lean away (the step aside is the battle's own)
    "dodge": [(0.0, p()),
              (0.35, p(root={"move": (0, -0.06, 0), "roll": 12}, spine={"pitch": 5}, head={"pitch": 8})),
              (0.8, p())],
    # In the red, looped: head hung low, the body sunk, a slow heave
    "tired": [(0.0, p(root={"move": (0, -0.03, 0)}, spine={"pitch": 5}, head={"pitch": 18}, tail={"pitch": 12})),
              (1.0, p(root={"move": (0, -0.04, 0)}, spine={"pitch": 7}, head={"pitch": 24}, tail={"pitch": 14})),
              (2.0, p(root={"move": (0, -0.03, 0)}, spine={"pitch": 5}, head={"pitch": 18}, tail={"pitch": 12}))],
    # A win: two hops for joy
    "cheer": [(0.0, p()),
              (0.15, p(root={"move": (0, -0.04, 0)}, spine={"pitch": 4})),
              (0.3, p(root={"move": (0, 0.15, 0)}, head={"pitch": -15}, front={"pitch": -14}, back={"pitch": 10}, tail={"yaw": 12})),
              (0.45, p(root={"move": (0, -0.02, 0)})),
              (0.6, p(root={"move": (0, -0.04, 0)}, spine={"pitch": 4})),
              (0.75, p(root={"move": (0, 0.15, 0)}, head={"pitch": -15}, front={"pitch": -14}, back={"pitch": 10}, tail={"yaw": -12})),
              (0.9, p(root={"move": (0, -0.02, 0)})),
              (1.2, p())],
}

written = []
for role, name in j["clips"].items():
    if make(name, CLIPS[role]):
        written.append(name)

bpy.ops.export_scene.gltf(filepath=j["output"], export_format="GLB", export_animations=True,
                          export_animation_mode="ACTIONS", export_force_sampling=True)
rig.report(j["report"], {"roles": roles, "written": written, "dropped": dropped,
                         "kept": [a.name for a in bpy.data.actions if a.name not in written], "height": round(height, 4)})
