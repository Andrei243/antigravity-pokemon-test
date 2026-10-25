# What the three scripts share: loading a .glb into an empty scene, finding the parts of its rig by role, and the
# body's own axes. Runs inside Blender (bpy); tools/BlenderAnim starts it.
#
# The game's front is glTF's +Z, which Blender's importer makes -Y; up is +Z. Every pose is written in those axes,
# whatever way the rig's bones point, so a clip means the same on any rig.
import bpy, json, math, re, sys
from mathutils import Quaternion, Vector

FORWARD = Vector((0, -1, 0))
UP = Vector((0, 0, 1))
# Turning about this axis by a positive angle tips a part's front down (a nose, a reaching foot goes back)
PITCH = UP.cross(FORWARD)
# Turning about the body's length: a positive angle rolls its top toward +X, the model's left
ROLL = -FORWARD


def job():
    """The job file tools/BlenderAnim wrote, named after the "--" on Blender's command line."""
    path = sys.argv[sys.argv.index("--") + 1]
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def report(path, data):
    with open(path, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)


def load(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=path)
    # The importer draws bones with an icosphere of its own: it isn't the model's, and must not be exported
    shapes = {b.custom_shape for o in bpy.data.objects if o.type == "ARMATURE" for b in o.pose.bones if b.custom_shape}
    for o in shapes:
        bpy.data.objects.remove(o, do_unlink=True)
    arms = [o for o in bpy.data.objects if o.type == "ARMATURE"]
    if not arms:
        raise SystemExit("BA:ERROR the file has no armature: rig it first (tools/TripoSculpt rig)")
    return arms[0]


def meshes():
    return [o for o in bpy.data.objects if o.type == "MESH"]


def bounds():
    lo = Vector((1e9, 1e9, 1e9))
    hi = Vector((-1e9, -1e9, -1e9))
    for o in meshes():
        for c in o.bound_box:
            p = o.matrix_world @ Vector(c)
            lo = Vector(map(min, lo, p))
            hi = Vector(map(max, hi, p))
    return lo, hi


def depth(bone):
    d = 0
    while bone.parent:
        bone = bone.parent
        d += 1
    return d


def find_roles(arm, given=None):
    """
    Bones by role: root, spine (several), neck, head, front and back legs (each a chain's first bone), tail, wings.
    The trunk (root, spine, neck, head) is found by name; legs and the tail by where their chains go, since a rigger's
    names can't be trusted for them (Tripo has called a tail a limb and put "Tail_0" in a paw): a chain off the trunk
    that drops to the floor more steeply than it runs is a leg, in front of the body's middle or behind it, and the one
    that runs furthest back from the rear half is the tail.
    """
    bones = list(arm.data.bones)
    names = {b.name: b.name.lower() for b in bones}
    world = arm.matrix_world

    def like(pattern):
        return [b for b in bones if re.search(pattern, names[b.name])]

    def at(b, tail=False):
        return world @ (b.tail_local if tail else b.head_local)

    roles = {}
    roots = [b for b in bones if b.parent is None]
    named = [b for b in roots if re.search(r"root|hips|pelvis", names[b.name])]
    root = (named or sorted(roots, key=lambda b: -len(b.children_recursive)))[0]
    roles["root"] = [root.name]
    heads = sorted(like(r"head"), key=depth)
    head = heads[0] if heads else None
    necks = sorted(like(r"neck"), key=depth)
    roles["head"] = [head.name] if head else []
    roles["neck"] = [necks[0].name] if necks else []
    # The spine is what carries the head; a "spine" bone off that line is a shoulder or a hip
    spine = [b for b in sorted(like(r"spine|chest|torso"), key=depth) if head is None or head in b.children_recursive]
    roles["spine"] = [b.name for b in spine]
    roles["wings"] = [b.name for b in like(r"wing") if not (b.parent and re.search(r"wing", names[b.parent.name]))]

    lo, hi = bounds()
    height, length = max(hi.z - lo.z, 1e-3), max(hi.y - lo.y, 1e-3)
    middle = (lo.y + hi.y) / 2
    trunk = {root} | set(spine) | ({head} if head else set()) | set(sorted(like(r"neck"), key=depth)[:1])
    away = set(head.children_recursive) if head else set()
    wings = {w for n in roles["wings"] for w in [arm.data.bones[n]] + list(arm.data.bones[n].children_recursive)}
    chains = [c for t in trunk for c in t.children if c not in trunk and c not in away and c not in wings]
    front, back, tails = [], [], []
    for top in chains:
        start = at(top)
        points = [at(b, True) for b in [top] + list(top.children_recursive)]
        lowest = min(points, key=lambda v: v.z)
        drop = start.z - lowest.z
        run = (lowest.xy - start.xy).length
        # A rig's last bone may stop short of the foot's sole by a quarter of the height (Ratchisel's)
        if lowest.z < lo.z + 0.4 * height and drop > 1.2 * run:
            (front if start.y < middle else back).append(top.name)
            continue
        furthest = max(points, key=lambda v: v.y)
        reach = furthest.y - start.y
        if start.y >= middle - 0.1 * length and reach > 0.1 * length:
            tails.append((reach, top.name))
    roles["front"], roles["back"] = front, back
    roles["tail"] = [max(tails)[1]] if tails else []

    for role, chosen in (given or {}).items():
        for bone in chosen:
            if bone not in arm.data.bones:
                raise SystemExit(f"BA:ERROR --map {role}={bone}: the rig has no bone of that name")
        roles[role] = list(chosen)
    return roles


def actions_of(arm):
    return [a.name for a in bpy.data.actions]
