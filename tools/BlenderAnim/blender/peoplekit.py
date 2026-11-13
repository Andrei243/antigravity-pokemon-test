# Helpers for the people's files (style guide, "Characters from files"): the engine's frames, shapes written in the
# game's own axes (x to the character's left, y up, z to the front), and the zones a look's colours fill.
#
# A file is one build's wardrobe: a node a piece, named <slot>.<Shape>, skinned to the engine's skeleton for the build
# (people_frames.json, written by the engine's own test), with extras "colours" (zones or true), "tint" (whose colours
# fill the zones) and "hides" (the body's parts the piece covers).
import json, math, os, sys
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from sculptkit import Kit, ell, tube, boxy, srgb, inside, smoothstep, unit

# Zones: red the primary colour, green the accent, blue white, none ink; two full together are the paired zones
P = np.array([1.0, 0, 0])
A = np.array([0, 1.0, 0])
W = np.array([0, 0, 1.0])
K = np.zeros(3)
STRIPE = np.array([0, 1.0, 1.0])      # the accent on a look with stripes, else the primary
SHADE = np.array([1.0, 1.0, 0])       # the primary, darker
THIRD = np.array([1.0, 0, 1.0])       # the tint's third colour

HERE = os.path.dirname(os.path.abspath(__file__))


def frames():
    with open(os.path.join(HERE, "people_frames.json")) as fh:
        return json.load(fh)


def B(p):
    """A point or a direction of the game's (x, y up, z front) in Blender's (x, -z, y)."""
    return (p[0], -p[2], p[1])


def V(p):
    return np.array(p, float)


def skeleton_of(entry):
    """The build's bones for the kit, parents before children, in Blender's axes."""
    left = {b[0]: b for b in entry["bones"]}
    out, done = [], set()
    while left:
        for name, (n, parent, joint) in list(left.items()):
            if parent is None or parent in done:
                out.append((n, parent, B(joint)))
                done.add(n)
                del left[name]
    return out


def E(c, r, pitch=0.0, yaw=0.0, roll=0.0, deform=None, sub=3):
    """An ellipsoid in the game's axes: radii (across, up, front to back); turns in radians as the engine's Pitch."""
    return ell(B(c), (r[0], r[2], r[1]), rot=(math.degrees(pitch), math.degrees(-roll), math.degrees(yaw)),
               deform=deform, sub=sub)


def Box(c, half, pitch=0.0, yaw=0.0, roll=0.0, n=6, sub=3):
    """A rounded box."""
    return E(c, half, pitch, yaw, roll, deform=boxy(n), sub=sub)


def T(pts, radii, seg=12, per=4):
    """A round tube through points of the game's axes."""
    return tube([B(p) for p in pts], radii, seg=seg, per=per)


def _turn(Vs, pitch, yaw, roll):
    from mathutils import Euler
    if pitch or yaw or roll:
        m = np.array(Euler((pitch, -roll, yaw)).to_matrix())
        Vs = Vs @ m.T
    return Vs


def lathe(c, profile, pitch=0.0, yaw=0.0, roll=0.0, sx=1.0, sz=1.0, seg=24, closed=False):
    """
    A shape turned about the up axis: profile is (radius, height) pairs from the bottom up (the outer side going up),
    c its origin; sx and sz stretch it across and front to back. closed joins the last ring to the first (a torus).
    """
    ang = np.linspace(0, 2 * math.pi, seg, endpoint=False)
    Vs, F = [], []
    for r, h in profile:
        for a in ang:
            Vs.append((r * math.cos(a) * sx, r * math.sin(a) * sz, h))
    k = len(profile)
    for i in range(k if closed else k - 1):
        j = (i + 1) % k
        for s in range(seg):
            a, b = i * seg + s, i * seg + (s + 1) % seg
            F.append([a, b, j * seg + (s + 1) % seg, j * seg + s])
    if not closed:
        lo, hi = len(Vs), len(Vs) + 1
        Vs.append((0, 0, profile[0][1])); Vs.append((0, 0, profile[-1][1]))
        last = (k - 1) * seg
        for s in range(seg):
            F.append([lo, (s + 1) % seg, s])
            F.append([hi, last + s, last + (s + 1) % seg])
    Vs = _turn(np.array(Vs, float), pitch, yaw, roll) + np.array(B(c))
    return Vs, F


def torus(c, R, r, pitch=0.0, yaw=0.0, roll=0.0, sx=1.0, sz=1.0, seg=24, ring=8):
    """A ring lying flat about the up axis."""
    prof = [(R + r * math.cos(t), r * math.sin(t)) for t in np.linspace(0, 2 * math.pi, ring, endpoint=False)]
    return lathe(c, prof, pitch, yaw, roll, sx, sz, seg, closed=True)


def cone(a, b, ra, rb, seg=10):
    """A straight tapering spike from a to b (game's axes)."""
    return T([a, b], [ra, rb], seg=seg, per=2)


class People:
    """A build's file being made: the kit on the build's skeleton, and the pieces' names and extras."""

    def __init__(self, build):
        self.entry = frames()[build]
        self.f = self.entry["frame"]
        self.build = build
        self.k = Kit(name=build, skeleton=skeleton_of(self.entry), sides=("L", "R"))
        self.voxel = 0.010
        self.made = []

    def add(self, name, shape, zone, bone, fuse=True, mirror=False):
        self.k.add(name, shape, zone, bone, fuse=fuse, mirror=mirror)

    def paint(self, mask, zone, on=None):
        """mask takes points in the game's axes."""
        self.k.paint(lambda Pb: mask(np.stack([Pb[:, 0], Pb[:, 2], -Pb[:, 1]], axis=1)), zone, on=on)

    def decal(self, c, n, half, zone, up=(0, 1, 0), **kw):
        self.k.decal(B(c), B(n), half, zone, up=B(up), **kw)

    def piece(self, name, tint, hides="", colours="zones", cut=None, surface=None, voxel=None, smooth=3):
        cutb = None
        if cut is not None:
            cutb = lambda Pb: cut(np.stack([Pb[:, 0], Pb[:, 2], -Pb[:, 1]], axis=1))
        self.k.build(voxel=voxel or self.voxel, smooth=smooth, name=name,
                     props={"colours": colours, "tint": tint, "hides": hides}, cut=cutb, surface=surface)
        self.made.append(name)

    def export(self, out):
        self.k.export(out)
        print("PEOPLE:", self.build, len(self.made), "pieces")
