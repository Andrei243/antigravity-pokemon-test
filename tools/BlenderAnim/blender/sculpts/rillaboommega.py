# Mega Rillaboom (a Mega of our own, which no game has) sculpted from nothing on the skeleton and clips of the game's
# own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/rillaboommega.py -- <the export's Rillaboom-Mega.glb> <out.glb>
#
# No pictures: the design is the game's own sculptor (PokemonModels.Ours.Megas.cs, RillaboomMega) and the look of its
# export. Grown from rillaboom.py (sibling): its body, legs, arms, belt, mane and face are built by its own functions,
# through a kit that stretches every part by the Mega's own skeleton (bigger and taller: 1.06 across, 1.04 deep,
# 1.08 up, as the sculptor stretches it and the export's joints stand). What the Mega adds, largest first:
# - the stump drum strapped across its back: a great log on its side, as long as the shoulders are wide, a pale face
#   at each end ringed dark, moss over its top;
# - the harness of vines from the drum over the shoulders and crossed over the chest;
# - its mane spread over its shoulders as pads of broad pointed leaves;
# - a thick branch with a ball of leaves at its end in each fist for a drumstick.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, lumpy, outward, srgb, inside, unit, eye_look, polygon, args, sibling

rl = sibling("rillaboom")
g = rl.g
BROWN, GREEN, GREEN_D, BLACK, IRIS, INK = rl.BROWN, rl.GREEN, rl.GREEN_D, rl.BLACK, rl.IRIS, rl.INK
LIGHT, WOOD, WOOD_D, FACE, FACE_D, VINE, MOSS = ("#60AC5C", "#8C6446", "#5E3F2A", "#DEC696", "#B49C70", "#2E6A32",
                                                  "#467A3C")
K = np.array([1.06, 1.04, 1.08])            # the Mega's stretch (Blender's x, y, z)


class Stretched:
    """The kit as rillaboom.py's functions see it: every part, paint and patch stretched by K."""
    def __init__(self, k):
        self.k = k

    def add(self, name, shape, color, bone, **kw):
        V, F = shape
        self.k.add(name, (np.asarray(V) * K, F), color, bone, **kw)

    def paint(self, mask, color, on=None):
        self.k.paint(lambda P, mask=mask: mask(P / K), color, on=on)

    def decal(self, center, normal, half, color, **kw):
        n = np.array(normal, float) / K
        self.k.decal(np.array(center, float) * K, n / np.linalg.norm(n), tuple(h * 1.06 for h in half), color, **kw)


def join(shapes):
    V, F, off = [], [], 0
    for v, f in shapes:
        V.append(np.asarray(v)); F.extend([[i + off for i in q] for q in f]); off += len(v)
    return np.vstack(V), F


def ring(c, d, r_in, r_out, half, seg=48):
    """A flat ring (a hoop round a drum's face) as a solid of turning."""
    return lathe(c, d, [(r_in, -half), (r_out, -half), (r_out, half), (r_in, half), (r_in, -half)], seg=seg)


DRUM = np.array([0.0, 0.275, 0.83])
DRUM_HALF, DRUM_R = 0.25, 0.148


def drum(k):
    """The stump drum across the back: a log on its side, pale faces ringed dark, moss over its top."""
    prof = [(0, -DRUM_HALF), (DRUM_R * 0.93, -DRUM_HALF), (DRUM_R, -DRUM_HALF * 0.8), (DRUM_R * 1.02, 0),
            (DRUM_R, DRUM_HALF * 0.8), (DRUM_R * 0.93, DRUM_HALF), (0, DRUM_HALF)]
    k.add("drum", lathe(DRUM, (1, 0, 0), prof, seg=56), WOOD, "body", fuse=False, surface="shell")
    faces, hoops, rings = [], [], []
    for s in (1, -1):
        f = DRUM + (s * DRUM_HALF, 0, 0)
        faces.append(lathe(f, (s, 0, 0), [(0, -0.004), (DRUM_R * 0.9, -0.004), (DRUM_R * 0.9, 0.006), (0, 0.006)],
                           seg=56))
        hoops.append(ring(f, (1, 0, 0), DRUM_R * 0.86, DRUM_R * 1.06, 0.02))
        rings.append(ring(f + (s * 0.006, 0, 0), (1, 0, 0), DRUM_R * 0.42, DRUM_R * 0.52, 0.004))
    k.add("drumfaces", join(faces), FACE, "body", fuse=False, surface="shell")
    k.add("drumhoops", join(hoops), WOOD_D, "body", fuse=False, surface="shell")
    k.add("drumrings", join(rings), FACE_D, "body", fuse=False, surface="shell")
    # moss over the top
    k.add("moss", outward(ell(DRUM + (0.04, -0.01, DRUM_R * 0.86), (0.13, 0.095, 0.04), deform=lumpy(5, 3, 0.12))),
          MOSS, "body", fuse=False, surface="leaf")


def harness(k):
    """Vines from the drum over each shoulder, crossed over the chest."""
    for s in (1, -1):
        pts = [(0.16 * s, 0.2, 0.75), (0.235 * s, 0.03, 0.74), (0.19 * s, -0.19, 0.645), (0.06 * s, -0.255, 0.565),
               (-0.08 * s, -0.255, 0.49), (-0.17 * s, -0.2, 0.42), (-0.2 * s, -0.08, 0.385)]
        k.add("harness" + ("R" if s > 0 else "L"), tube(pts, [0.019] * len(pts), seg=14, per=6), VINE, "body")


def pads(k):
    """The mane spread over the shoulders as pads of broad pointed leaves, out and down."""
    for s in (1, -1):
        for i in range(5):
            y = -0.1 + 0.05 * i
            base = (0.2 * s, y * 1.04, 0.755 - 0.01 * abs(i - 2))
            d = (0.9 * s, y * 0.7, -0.42 - 0.04 * abs(i - 2))
            L = 0.17 - 0.015 * abs(i - 2)
            k.add(f"pad{'R' if s > 0 else 'L'}{i}", rl.spike(base, d, L, 0.058, 0.032, up=(0, 0, 1)),
                  GREEN_D if i % 2 else LIGHT, "arm" + ("R" if s > 0 else "L"), fuse=False, surface="leaf")


def sticks(k):
    """A thick branch in each fist, a ball of leaves at its end."""
    S, E, W = np.array((0.2, -0.05, 0.6)), np.array((0.29, -0.13, 0.4)), np.array((0.255, -0.24, 0.24))
    d = unit(*(W - E))
    P = (W + d * 0.06) * K
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        grip = P * m + np.array([-0.012 * s, 0.01, 0.0])
        end = grip + np.array([0.05 * s, -0.17, 0.25])
        bone = "arm" + ("R" if s > 0 else "L")
        side = "R" if s > 0 else "L"
        k.add("stick" + side, outward(tube([grip - (end - grip) * 0.12, (grip + end) / 2, end], [0.027, 0.024, 0.02],
                                            seg=14, per=5)), WOOD, bone, fuse=False, surface="shell")
        k.add("leafball" + side, outward(ell(end + unit(0.0, -0.2, 1) * 0.03, (0.058, 0.055, 0.052),
                                             deform=lumpy(6, 3, 0.12))), GREEN, bone, fuse=False, surface="leaf")
        leaves = []
        for j, dd in enumerate([(1, -0.2, 0.4), (-1, -0.2, 0.4), (0, -1, 0.5), (0.3, 0.3, 1), (-0.4, 0.5, 0.8),
                                (0.5, -0.8, -0.2)]):
            dd = unit(*dd)
            leaves.append(rl.spike(end + unit(0, -0.2, 1) * 0.03 + dd * 0.035, dd, 0.075, 0.03, 0.022,
                                   up=(0, 0, 1) if abs(dd[2]) < 0.9 else (0, 1, 0)))
        k.add("ballleaves" + side, join(leaves), LIGHT, bone, fuse=False, surface="leaf")


def rillaboom_mega(src, out):
    k = Kit(src, coat="fur")
    st = Stretched(k)
    rl.body(st)
    rl.legs(st)
    rl.arms(st)
    rl.belt(st)
    HEAD = ell(rl.H, rl.HR)
    st.add("head", HEAD, BROWN, "head")
    rl.mane(st)
    EZ = rl.face(st, HEAD)
    drum(k)
    harness(k)
    pads(k)
    sticks(k)

    # The eyes as Rillaboom's, stern in the black band
    OUT = [(-1.0, -0.1), (-0.6, -0.75), (0.2, -0.85), (0.85, -0.4), (1.0, 0.35), (0.3, 0.45), (-0.6, 0.05)]
    def open_look(u, v):
        c = np.tile(srgb(INK), (len(u), 1))
        c[(u / 0.8) ** 2 + ((v + 0.15) / 0.6) ** 2 < 1] = srgb(IRIS)
        c[((u + 0.1) / 0.3) ** 2 + ((v + 0.15) / 0.4) ** 2 < 1] = srgb(INK)
        c[((u + 0.32) / 0.13) ** 2 + ((v - 0.05) / 0.13) ** 2 < 1] = np.array([1.0, 1.0, 1.0])
        return c
    de = unit(0.4, -0.91, 0.04)
    E = g.on_ell(rl.H, rl.HR, de)
    E[2] = EZ
    def lay(state):
        look = eye_look(state, open_look, srgb(BLACK), srgb("#B8A8A0"), line=0.2)
        if state in ("Open", "Fierce"):
            look = eye_look(state, open_look, srgb(BLACK), srgb(INK), line=0.2)
        for s in (1, -1):
            out_ = polygon(OUT if s > 0 else [(-a, b) for a, b in OUT])
            col = look if s > 0 else (lambda u, v, look=look: look(-u, v))
            st.decal(E * np.array([s, 1, 1]), de * np.array([s, 1, 1]), (0.04, 0.032), col, outline=out_,
                     eps=0.002, rings=20, segs=56)

    k.build(voxel=0.0092, smooth=4, band=0.006, wband=0.07)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    rillaboom_mega(*args())
