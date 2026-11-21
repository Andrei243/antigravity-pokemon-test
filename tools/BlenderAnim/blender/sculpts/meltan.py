# Meltan sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/meltan.py -- <the export's Meltan.glb> <out.glb>
#
# From the references: a little blob of liquid silver with a great gold hex nut for a head, the nut more than half
# the whole height, standing on its flat (its points at the sides), tipped a little so its right side rises (the
# viewer's right, +X), its round hole facing forward, the hole's wall a darker gold. In the hole hangs the one eye, a
# dark grey metal ball with a white glint, set toward the hole's upper +X side. The silver blob below is soft and
# lumpy with a flat bottom, a lobe reaching up the nut's +X side; a thin red wire of a tail comes out of its -X side,
# kinked, two copper strands at its tip. Written to be grown from (melmetal.py imports it with sibling): the nut, the
# eye and the colours are names here.
# Skeleton: body (0, 0, 0.08), head (0, 0, 0.09), tail (0, 0.05, 0.04).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, ring_band, srgb, unit, eye_look, boxy, args

GOLD, GOLD_IN, SILVER, BALL, WIRE, COPPER = "#EBC560", "#B98A30", "#A6A9B1", "#4A494E", "#D63A2E", "#B8743A"
GLINT, BALL_DARK, LINE = "#F7F7F7", "#2C2B30", "#9C9BA2"
VOXEL = 0.0023

NC = np.array([0.0, -0.004, 0.158])     # the nut's middle
AP = 0.068                              # from its middle to each flat
HOLE = 0.038                            # the hole's radius
DEPTH = 0.042                           # its thickness, front to back
TILT = 11.0                             # degrees, its +X side raised


def in_plane(c, a, b, tilt=TILT, depth=0.0):
    """A point of the nut's face: a toward +X, b up, turned by tilt about the axis toward the viewer."""
    t = math.radians(tilt)
    x, z = a * math.cos(t) - b * math.sin(t), a * math.sin(t) + b * math.cos(t)
    return np.array(c, float) + (x, depth, z)


def nut(c, ap, hole, depth, tilt=TILT, slices=4):
    """A hex nut on its flat, facing -Y: slices of flat-faced hulls between the round hole and the six flats, as one
    piece (each slice convex, so each is a hull of its own corners)."""
    Vs, Fs, off = [], [], 0
    for side in range(6):
        for j in range(slices):
            a0 = math.radians(side * 60 + j * 60 / slices)
            a1 = math.radians(side * 60 + (j + 1) * 60 / slices)
            pts = []
            for a in (a0, a1):
                ro = ap / math.cos((a - math.radians(side * 60 + 30)))
                for r in (hole, ro):
                    for y in (-depth / 2, depth / 2):
                        pts.append(in_plane(c, r * math.cos(a), r * math.sin(a), tilt, y))
            V, F = hull(pts)
            Vs.append(V); Fs.extend([[i + off for i in f] for f in F]); off += len(V)
    return np.vstack(Vs), Fs


def add_nut(k, c, ap, hole, depth, bone, tilt=TILT, name="nut"):
    k.add(name, nut(c, ap, hole, depth, tilt), GOLD, bone, fuse=False, surface="metal")
    # the hole's wall, darker
    k.add(name + "wall", ring_band(c, (0, 1, 0), hole * 0.982, depth * 0.96, thick=0.0016), GOLD_IN, bone,
          fuse=False, surface="metal")


def eye_ball(k, c, r, bone):
    k.add("eyeball", ell(c, (r, r, r)), BALL, bone)


CB, CD, CG, CL = srgb(BALL), srgb(BALL_DARK), srgb(GLINT), srgb(LINE)


def ball_look(u, v):
    rr = np.hypot(u, v)
    c = CB[None] * (1 - 0.45 * rr[:, None] ** 2) + CD[None] * 0.45 * rr[:, None] ** 2
    c[((u + 0.28) / 0.26) ** 2 + ((v - 0.3) / 0.22) ** 2 < 1] = CG
    return c


def eye_decal(k, state, c, r):
    look = eye_look(state, ball_look, CB, CL, line=0.15)
    k.decal(c - (0, r, 0), (0, -1, 0), (r * 0.92, r * 0.92), look, eps=0.0008, rings=20, segs=56)


EYE_C = in_plane(NC, 0.009, 0.007, depth=-0.004)
EYE_R = 0.023


def blob(k):
    def drop(V):
        # a slumped cushion: squared, soft corners, sagging wider toward a flat bottom
        V = boxy(3)(V)
        V[:, 2] = np.maximum(V[:, 2], -0.85)
        th = np.arctan2(V[:, 1], V[:, 0])
        low = np.clip(-V[:, 2], 0, 1)
        s = 1 + 0.06 * np.sin(4 * th + 0.7) * low + 0.12 * low
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("blob", ell((0.0, 0.004, 0.048), (0.066, 0.046, 0.048), deform=drop, sub=5), SILVER, "body")
    # the silver pulled up the nut's +X corner, low and broad (not an ear); a sag at the -X foot
    k.add("lobeR", ell((0.045, 0.004, 0.082), (0.026, 0.03, 0.026), rot=(0, -30, 0)), SILVER, "body")
    k.add("lobeL", ell((-0.05, 0.004, 0.03), (0.026, 0.032, 0.026)), SILVER, "body")
    k.add("drip", ell((0.03, -0.03, 0.012), (0.026, 0.02, 0.012)), SILVER, "body")


def tail(k):
    pts = [(-0.045, 0.012, 0.03), (-0.064, 0.008, 0.036), (-0.078, 0.004, 0.026), (-0.093, 0.004, 0.032),
           (-0.108, 0.002, 0.018)]
    k.add("wire", tube(pts, [0.0058] * 5), WIRE, "tail")
    for s in (1, -1):
        tip = np.array([-0.112, 0.002, 0.014])
        k.add("strand" + ("A" if s > 0 else "B"),
              hull([tip + (0.0, 0.004, 0.004), tip + (0.0, -0.004, -0.004), tip + (0.004, 0.004 * s, -0.004 * s),
                    tip + (-0.014, 0.005 * s, -0.008), tip + (-0.013, 0.005 * s + 0.0035, -0.008)]), COPPER, "tail",
              fuse=False, surface="metal")


if __name__ == "__main__":
    src, dst = args()
    k = Kit(src, coat="metal")
    blob(k)
    tail(k)
    eye_ball(k, EYE_C, EYE_R, "head")
    add_nut(k, NC, AP, HOLE, DEPTH, "head")
    k.build(voxel=VOXEL, smooth=5, band=0.003, wband=0.02)
    k.eye_states(lambda state: eye_decal(k, state, EYE_C, EYE_R))
    k.export(dst)
