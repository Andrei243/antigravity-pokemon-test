# Dracovish sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dracovish.py -- <the export's Dracovish.glb> <out.glb>
#
# From the references: a great fish's head stuck on the tip of a dinosaur's tail (dracozolt.py's green legs, red
# bands and pink claws). The dinosaur is its hips and two great legs; its tail rises from behind the hips and arches
# up, over and forward, a thick green band whose inner side is red and bristles with five pink spikes pointing in
# toward the middle of the arch, and the fish's head hangs on its end in front, high, the largest mass after the arch.
# The head: long and pale blue, rounded at the front, a red lens-shaped patch along the top edged in white; small
# white eyes with an upright black bar on the front of its sides; the jaws open wide at the front, a red mouth, two
# round pale blue fangs hanging from the upper lip's front corners and a ragged lower lip; a little blue knob under
# the chin and two broad white fins hanging from under the jaw. A red ring round a red dot on each hip.
# Written to be grown from: arctovish.py takes the head (`fish_head`).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, srgb, inside, unit, eye_look, args, sibling

dz = sibling("dracozolt")

PALE = "#A8D2F0"
RIM = "#DCEEFA"
TOPRED = "#D9505E"
MOUTH = "#C33F4E"
THROAT = "#8E2638"
FINWHITE = "#EEF0F5"
INK = "#1E2026"


def fish_head(k, C, G=1.0, pitch=0.0, body=PALE, top=TOPRED, rim=RIM, open_mouth=True, head="head", jaw="head",
              fin_bone=None, mouth=MOUTH):
    """
    The fish's head about its middle C, its local front -Y and top +Z turned by pitch degrees about X (positive lowers
    the snout). Returns (to_world, to_local, skull centre, skull radii) for the eyes and marks laid after it.
    """
    C = np.array(C, float)
    a = math.radians(pitch)
    R = np.array([[1, 0, 0], [0, math.cos(a), -math.sin(a)], [0, math.sin(a), math.cos(a)]])

    def W(p):
        return C + (np.array(p, float) * G) @ R.T

    def place(shape):
        V, F = shape
        return C + (V * G) @ R.T, F

    def local(P):
        return ((P - C) @ R) / G

    SC, SR = np.array([0, 0.02, 0.02]), np.array([0.125, 0.17, 0.1])
    k.add("fhead", place(ell(SC, SR)), body, head)
    k.add("fsnout", place(ell((0, -0.115, -0.015), (0.108, 0.09, 0.08))), body, head)
    if open_mouth:
        k.add("fmouth", place(ell((0, -0.085, -0.085), (0.082, 0.085, 0.05))), mouth, head, surface="skin")
        k.add("fjaw", place(ell((0, -0.075, -0.13), (0.088, 0.105, 0.034), rot=(-6, 0, 0))), body, jaw)
        k.add("fchin", place(ell((0, 0.01, -0.13), (0.06, 0.06, 0.05))), body, jaw)
        k.add("fknob", place(ell((0, 0.04, -0.175), (0.035, 0.035, 0.03))), body, jaw)
        # the jaw's top is the mouth's colour, only a rim of blue showing
        jc = np.array([0, -0.075, -0.13])

        def jaw_top(P):
            q = local(P) - jc
            return inside(np.hypot(q[:, 0] / 0.088, q[:, 1] / 0.105) - 0.78, 0.04) * inside(-q[:, 2], 0.006)
        k.paint(jaw_top, mouth, on=["fjaw"])
        k.paint(lambda P: inside(local(P)[:, 1] + 0.02, 0.01), THROAT, on=["fmouth"])
        # two round fangs from the upper lip's front corners, and a ragged lower lip
        for s in (1, -1):
            sd = "R" if s > 0 else "L"
            dz.spike(k, f"fang{sd}", W((0.05 * s, -0.17, -0.07)), (R @ np.array([0.1 * s, -0.25, -1.0])), 0.06 * G,
                     0.045 * G, body, head, up=tuple(R @ np.array([0, 1.0, 0])), thick=0.035 * G, surface=None)
            for j, x in enumerate((0.035, 0.07)):
                dz.spike(k, f"lip{sd}{j}", W((x * s, -0.16 + 0.03 * j, -0.13)), (R @ np.array([0.1 * s, -0.3, 1.0])),
                         0.035 * G, 0.035 * G, body, jaw, up=tuple(R @ np.array([0, 1.0, 0])), thick=0.026 * G,
                         surface=None)
    else:
        k.add("fjaw", place(ell((0, -0.04, -0.07), (0.115, 0.15, 0.075))), body, jaw)
    # the top patch, edged in a paler rim
    def patch(scale):
        def m(P):
            q = local(P)
            e = np.hypot(q[:, 0] / (0.1 * scale), (q[:, 1] - 0.02) / (0.15 * scale))
            return inside(e - 1, 0.006) * (1 - inside(q[:, 2] - 0.045, 0.008))
        return m
    if rim:
        k.paint(patch(1.0), rim, on=["fhead"])
    if top:
        k.paint(patch(0.8), top, on=["fhead"])
    if fin_bone:
        for s in (1, -1):
            sd = "R" if s > 0 else "L"
            FO = [(0.0, -0.03), (0.07, -0.04), (0.13, -0.02), (0.16, 0.005), (0.12, 0.035), (0.05, 0.04), (0.0, 0.03)]
            ex = R @ unit(0.3 * s, 0.25, -1)
            ez = R @ unit(0.15 * s, -1, 0.2)
            k.add(f"jawfin{sd}", slab([(x * G, y * G) for x, y in FO], W((0.07 * s, -0.02, -0.12)), ex, ez,
                                      0.022 * G), FINWHITE, fin_bone.format(s=sd), fuse=False, surface="skin")
    return W, local, SC, SR, R


def fish_eyes(k, W, SC, SR, R, G=1.0, lid=PALE, bar="upright", d=(0.74, -0.52, 0.42), half=0.03):
    """Small white eyes with a black bar (upright for Dracovish, level for Arctovish). Returns lay(state)."""
    WH, I, L = srgb("#FAFCFF"), srgb(INK), srgb(lid)
    dl = unit(*d)
    E = W(dz.on_ell(SC, SR, dl))
    de = R @ dl

    def look(u, v):
        c = np.tile(L, (len(u), 1))
        r = np.hypot(u, v)
        c[r < 0.8] = WH
        c[r >= 0.8] = I
        if bar == "upright":
            c[(np.abs(u) < 0.2) & (np.abs(v) < 0.62)] = I
        else:
            c[(np.abs(v) < 0.2) & (np.abs(u) < 0.62)] = I
        return c

    def lay(state):
        k.decal(E, de, (half * G, half * G * 0.9), eye_look(state, look, L, I, line=0.18), eps=0.0018, rings=20,
                segs=56, mirror=True)
    return lay


HIPS_C = np.array([0.0, 0.1, 0.33])
ARCH = [(0, 0.12, 0.36), (0, 0.26, 0.42), (0, 0.345, 0.52), (0, 0.32, 0.625), (0, 0.22, 0.67), (0, 0.1, 0.665)]
ARCH_R = [0.1, 0.078, 0.07, 0.068, 0.07, 0.078]
O = np.array([0.0, 0.17, 0.52])         # the arch's middle, where its spikes point
HEAD_C = np.array([0.0, -0.075, 0.585])
HEAD_G = 1.0


def dracovish(src, out):
    k = Kit(src, coat="scales")

    # --- the dinosaur: hips, legs, the arching tail --------------------------------------------------------------
    k.add("hips", ell(HIPS_C, (0.13, 0.15, 0.13)), dz.GREEN, "body")
    k.add("rump", ell((0, 0.19, 0.33), (0.1, 0.09, 0.1)), dz.GREEN, "tail")
    dz.dino_leg(k, (0.105, 0.06, 0.26), G=0.95, stripes=True)
    k.add("arch", tube(ARCH[:4], ARCH_R[:4]), dz.GREEN, "body")
    k.add("archtop", tube(ARCH[2:], ARCH_R[2:]), dz.GREEN, "head")

    # the red inner side of the arch
    ay = np.array([p[1] for p in ARCH]) - O[1]; az = np.array([p[2] for p in ARCH]) - O[2]
    ath = np.unwrap(np.arctan2(az, ay)); arho = np.hypot(ay, az)

    def inner(P):
        th = np.arctan2(P[:, 2] - O[2], P[:, 1] - O[1])
        rho = np.hypot(P[:, 2] - O[2], P[:, 1] - O[1])
        rc = np.interp(th, ath, arho)
        return inside(rho - (rc - 0.035), 0.006) * (1 - inside(0.38 - P[:, 2], 0.01))
    k.paint(inner, dz.RED, on=["arch"])

    # five pink spikes along the inner side, pointing in toward the arch's middle
    for i, t in enumerate(np.linspace(-0.55, 1.6, 5)):
        rc = np.interp(t, ath, arho) - 0.05
        base = O + np.array([0, math.cos(t) * rc, math.sin(t) * rc])
        d = O - base + np.array([0, -0.03, 0])
        dz.spike(k, f"aspike{i}", base, d, 0.13 - 0.01 * abs(i - 2), 0.07, dz.PINK if i % 2 == 0 else dz.ROSE,
                 "body" if i < 3 else "head", up=(1, 0, 0), thick=0.03)

    # --- the fish's head on the arch's end -----------------------------------------------------------------------
    W, local, SC, SR, R = fish_head(k, HEAD_C, G=HEAD_G, pitch=-4, jaw="jaw", fin_bone="jaw")

    # a red ring round a red dot on each hip
    G2 = srgb(dz.GREEN)
    RD = srgb(dz.RED)

    def target(u, v):
        r = np.hypot(u, v)
        c = np.tile(G2, (len(u), 1))
        c[(r > 0.55) & (r < 0.85)] = RD
        c[r < 0.27] = RD
        return c
    k.decal((0.2, 0.06, 0.28), (1, 0.05, 0.1), (0.07, 0.07), target, eps=0.003, rings=16, segs=56, mirror=True)
    lay = fish_eyes(k, W, SC, SR, R, G=HEAD_G)

    k.build(voxel=0.0068, smooth=4, band=0.006, wband=0.06)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    dracovish(*args())
