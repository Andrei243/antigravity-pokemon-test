# Beldum sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/beldum.py -- <the export's Beldum.glb> <out.glb>
#
# From the references: a blue steel body lying level and floating, like a dumbbell. At its front a faceted head, end
# on to the viewer, with one round red eye in the middle of its front face, set in a black frame with four lobes. A
# narrow black joint behind the head, then a faceted bulb that widens backward into a round cuff; from the cuff's back
# face three white claws point back and out. Over the top a flat steel fin runs forward from the bulb past the head's
# front, a little clear of the head.
#
# Written to be grown from (metang.py and metagross.py import it with sibling): the colours, the faceted pieces
# (gem, claw), the eye's black frame and its look are here; the build is under the main guard.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, unit, eye_look, args, hull, slab, lathe, axes

STEEL, SHADE, DEEP = "#6F9FC8", "#5B88B2", "#4A7299"     # the steel, its darker plates, the underside's
JOINT, CLAW, SILVER = "#1F232B", "#E6E8EE", "#C9CDD4"
IRIS, PUPIL, GLINT, INK = "#E2445C", "#4A0F1C", "#FFC6CE", "#15171C"


# --- hard-edged pieces --------------------------------------------------------------------------------------------
def gem(c, axis, rings, up=None):
    """A faceted solid about an axis: rings of (distance along the axis, radius across a, radius across b, sides,
    turn in degrees), joined as one convex hull (flat faces, hard edges)."""
    d = np.array(axis, float); d /= np.linalg.norm(d)
    if up is None:
        a, b = axes(d)
    else:
        b = np.array(up, float); b = b - d * (b @ d); b /= np.linalg.norm(b)
        a = np.cross(b, d)
    c = np.array(c, float)
    P = []
    for t, ra, rb, n, turn in rings:
        if ra <= 0:
            P.append(c + d * t)
            continue
        for i in range(n):
            th = 2 * math.pi * i / n + math.radians(turn)
            P.append(c + d * t + a * ra * math.cos(th) + b * rb * math.sin(th))
    return hull(P)


def claw(base, direction, length, r, sides=5):
    """A white claw: a straight pointed cone with flat faces."""
    d = unit(*direction)
    a, b = axes(d)
    base = np.array(base, float)
    P = [base + d * length]
    for i in range(sides):
        th = 2 * math.pi * i / sides
        P.append(base + r * (math.cos(th) * a + math.sin(th) * b))
        P.append(base + d * length * 0.3 + 0.82 * r * (math.cos(th) * a + math.sin(th) * b))
    P.append(base - d * r * 0.6)
    return hull(P)


def flower(n=4, lobe=0.16, turn=45.0, k=72):
    """An outline of n round lobes (the eye's black frame), radius 1 on average."""
    th = np.linspace(0, 2 * math.pi, k, endpoint=False)
    r = 1 + lobe * np.cos(n * (th - math.radians(turn)))
    return np.stack([r * np.cos(th), r * np.sin(th)], axis=1) / (1 + lobe)


def eye_frame(k, name, centre, normal, r, bone, lobe=0.16, thick=0.012, up=(0, 0, 1)):
    """The black frame an eye sits in: a thin plate fused just proud of a facet (the eye's patch lies on it)."""
    n = unit(*normal)
    u = np.array(up, float); u = u - n * (u @ n); u /= np.linalg.norm(u)
    ex = np.cross(u, n)              # ex x ez = n
    k.add(name, slab(flower(lobe=lobe) * r, np.array(centre, float) - n * thick * 0.25, ex, u, thick), JOINT, bone)


CI, CP, CG, CK, CS = srgb(IRIS), srgb(PUPIL), srgb(GLINT), srgb(INK), srgb(STEEL)


def eye_open(u, v):
    """A round red eye: no rim (the frame is black), a dark pupil a little tall, a pale glint up and out."""
    c = np.tile(CI, (len(u), 1))
    c[(u / 0.34) ** 2 + (v / 0.44) ** 2 < 1] = CP
    c[((u - 0.36) / 0.17) ** 2 + ((v - 0.4) / 0.15) ** 2 < 1] = CG
    c[u * u + v * v > 0.93 ** 2] = CK
    return c


def lay_eye(k, state, centre, normal, half, up=(0, 0, 1), lid=STEEL, look=eye_open, mirror=False):
    k.decal(centre, normal, half, eye_look(state, look, srgb(lid), CK, line=0.17), up=up, eps=0.003,
            rings=20, segs=56, mirror=mirror)


# --- Beldum ---------------------------------------------------------------------------------------------------------
HC = np.array([0.0, -0.17, 0.107])        # the head's middle (the head bone)
HEAD = [  # along -Y from the head's middle: (distance, radius across, radius up, sides, turn)
    (-0.085, 0.0, 0.0, 1, 0), (-0.078, 0.045, 0.045, 8, 22.5), (-0.05, 0.078, 0.076, 8, 0),
    (-0.005, 0.088, 0.086, 8, 22.5), (0.045, 0.078, 0.076, 8, 0), (0.078, 0.05, 0.05, 8, 22.5)]
BULB = [  # along +Y from y = -0.075
    (0.0, 0.042, 0.042, 8, 0), (0.035, 0.07, 0.068, 8, 22.5), (0.095, 0.082, 0.08, 8, 0), (0.16, 0.084, 0.082, 8, 22.5)]
CUFF_Y, CUFF_R, CUFF_L = 0.07, 0.095, 0.085
EYE_C = HC + np.array([0, -0.08, -0.002])


def beldum(src, out):
    k = Kit(src, coat="metal")
    # the head: faceted, end on to the front; on its front face the eye's black frame
    k.add("head", gem(HC, (0, -1, 0), HEAD, up=(0, 0, 1)), STEEL, "head", fuse=False, surface="metal")
    eye_frame(k, "frame", EYE_C, (0, -1, 0), 0.046, "head")
    # the black joint behind it
    k.add("joint", ell((0, -0.083, 0.107), (0.05, 0.03, 0.05)), JOINT, "body")
    # the bulb, widening back into the cuff
    k.add("bulb", gem((0, -0.075, 0.107), (0, 1, 0), BULB, up=(0, 0, 1)), STEEL, "body", fuse=False, surface="metal")
    k.add("cuff", lathe((0, CUFF_Y, 0.107), (0, 1, 0), [(0, 0.0), (CUFF_R - 0.012, 0.0), (CUFF_R, 0.012),
                                                         (CUFF_R + 0.004, CUFF_L), (0, CUFF_L)], seg=32),
          SHADE, "tail", fuse=False, surface="metal")
    # three claws from the cuff's back face, back and out
    for i, a in enumerate((90, 210, 330)):
        t = math.radians(a)
        radial = np.array([math.cos(t), 0, math.sin(t)])
        base = np.array([0, CUFF_Y + CUFF_L - 0.01, 0.107]) + radial * 0.058
        k.add(f"claw{i}", claw(base, radial * 0.42 + np.array([0, 1.0, 0]), 0.085, 0.022), CLAW, "tail", fuse=False,
              surface="metal")
    # the fin over the top: a flat plate from the bulb forward past the head's front, a little clear of it
    fin = []
    for y, z, w, t in ((0.075, 0.178, 0.036, 0.03), (0.0, 0.198, 0.044, 0.028), (-0.1, 0.212, 0.046, 0.024),
                       (-0.2, 0.218, 0.044, 0.024), (-0.265, 0.21, 0.036, 0.028)):
        for sx in (1, -1):
            for sz in (0, 1):
                fin.append((sx * w, y, z - t / 2 + sz * t))
    k.add("finback", hull(fin[:12]), STEEL, "body", fuse=False, surface="metal")
    k.add("finfront", hull(fin[8:]), STEEL, "body", fuse=False, surface="metal")
    k.add("finroot", hull([(sx * 0.03, y, z) for sx in (1, -1) for y, z in
                           ((0.11, 0.17), (0.0, 0.17), (0.02, 0.2), (0.09, 0.19), (0.12, 0.14), (0.0, 0.15))]),
          STEEL, "body", fuse=False, surface="metal")
    k.build(voxel=0.0035, smooth=3)
    k.eye_states(lambda state: lay_eye(k, state, EYE_C, (0, -1, 0), (0.031, 0.031)))
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    beldum(src, out)
