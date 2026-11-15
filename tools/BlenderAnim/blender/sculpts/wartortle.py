# Wartortle sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown
# from Squirtle's script (its torso, rim, plates, limbs, paws, eyes and mouth):
#   blender --background --factory-startup --python sculpts/wartortle.py -- <the export's Wartortle.glb> <out.glb>
#
# The design, from the references: Squirtle grown taller and a darker lavender blue; brown eyes under a scowling
# brow, a wide mouth with a small white fang at each side; large pale blue feathery ears standing up and out from
# the head's sides; a cream plastron, a darker brown shell rimmed in white; arms held out with three white claws,
# feet with white claws; a great fluffy pale blue tail rising behind like a wave, curling over at its top, swirls
# drawn in it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, hull, slab, sibling, args

sq = sibling("squirtle")

BLUE, CREAM, SHELL, RIM_COL = "#8C9AE0", sq.CREAM, "#8E5636", sq.RIM
FLUFF, SWIRL, CLAW = "#CDE4F8", "#92B4E6", "#F6F6F4"
IRIS = "#8A4A26"

H = np.array([0, -0.035, 0.6])
HR = np.array([0.148, 0.138, 0.124])
TC = np.array([0, -0.01, 0.34])
TR = np.array([0.165, 0.145, 0.165])
SC = np.array([0, 0.06, 0.36])
SR = np.array([0.185, 0.14, 0.2])
RIM_R = 0.028
RIM_PLANE = sq.plane(0.025, 0.15, SC[2])
TAIL_D = unit(0.57, 0.82, 0)              # the tail's plane: back and out to +X, so its face shows
VOXEL = 0.006


def claws(k, name, c, d, spread, size, bone, n=3):
    """n white claws in a row across a paw's front at c, pointing along d: small solid wedges."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    side = np.cross(d, (0, 0, 1.0)); side /= np.linalg.norm(side)
    up = np.cross(side, d)
    for i in range(n):
        o = np.array(c, float) + side * spread * (i - (n - 1) / 2)
        pts = [o - side * size * 0.45, o + side * size * 0.45, o + up * size * 0.5, o - up * size * 0.3,
               o + d * size * 1.1 + up * size * 0.05]
        k.add(f"{name}{i}", hull(pts), CLAW, bone, fuse=False, mirror=True, surface="shell")


def ears(k):
    """Tall feathery ears: a pale solid plate a side, standing up and out, its front lined in a deeper blue."""
    o = np.array([0.1, -0.02, 0.665])
    back = math.radians(20)                     # swept back: the frame turned about x, its top toward +Y
    def swept(v):
        v = np.array(v, float)
        return np.array([v[0], v[1] * math.cos(back) + v[2] * math.sin(back),
                         -v[1] * math.sin(back) + v[2] * math.cos(back)])
    ez = swept(unit(0.62, 0.15, 1))
    ex = swept(unit(1, -0.3, -0.62))
    # A fan: narrow at the head, wide at the top, four pointed lobes along its top and outer edge
    EAR = [(-0.025, 0.0), (0.025, 0.0), (0.062, 0.065), (0.122, 0.135), (0.086, 0.148), (0.098, 0.205),
           (0.064, 0.188), (0.042, 0.242), (0.012, 0.2), (-0.022, 0.236), (-0.052, 0.168), (-0.046, 0.08)]
    LINING = [(0.0, 0.03), (0.04, 0.065), (0.078, 0.135), (0.062, 0.17), (0.03, 0.198), (-0.008, 0.196),
              (-0.03, 0.15), (-0.026, 0.075)]
    k.add("ear", slab(EAR, o, ex, ez, 0.034), FLUFF, "ear{s}", fuse=False, mirror=True, surface="hair")
    k.add("earlining", slab(rounded(LINING), o, ex, ez, 0.042), SWIRL, "ear{s}", fuse=False, mirror=True,
          surface="hair")


def rounded(pts, scale=1.0, per=6):
    """A closed outline smoothed through its points (Catmull-Rom), so a plate's rim curves instead of turning corners."""
    p = np.array(pts, float) * scale
    m = len(p)
    out = []
    for i in range(m):
        p0, p1, p2, p3 = p[i - 1], p[i], p[(i + 1) % m], p[(i + 2) % m]
        for j in range(per):
            t = j / per
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                              + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    return [tuple(q) for q in out]


def wave_tail(k):
    """The tail: puffs along a curl in its plane, rising behind the shell and curling over toward the body."""
    base = np.array([0.0, 0.14, 0.2])
    d, up = TAIL_D, np.array([0, 0, 1.0])
    n = np.cross(d, up)
    path = [(0.0, 0.0, 0.055), (0.1, 0.02, 0.07), (0.18, 0.1, 0.085), (0.21, 0.21, 0.09), (0.19, 0.32, 0.085),
            (0.13, 0.37, 0.07), (0.08, 0.33, 0.05)]
    def flat(shape, f=0.72):
        V, F = shape
        return V - np.outer((V - base) @ n * (1 - f), n), F
    pts = [base + d * s + up * t for s, t, _ in path]
    k.add("tail", flat(tube(pts, [r for _, _, r in path])), FLUFF, "tail")
    # Three soft crests along the outer edge: the wave's
    for i, (s, t, r) in enumerate([(0.25, 0.14, 0.06), (0.26, 0.27, 0.058), (0.2, 0.38, 0.055)]):
        k.add(f"tailcrest{i}", flat(ell(base + d * s + up * t, (r, r, r))), FLUFF, "tail")
    # Flatten nothing by hand: the swirls, drawn on both faces of the curl
    swirls = []
    for cs, ct, r0 in ((0.14, 0.27, 0.06), (0.17, 0.08, 0.045)):
        prev = None
        for th in np.linspace(0, 1.6 * math.pi, 18):
            r = r0 * (1 - 0.6 * th / (1.6 * math.pi))
            p = (cs + r * math.cos(th + 1.0), ct + r * math.sin(th + 1.0))
            if prev is not None:
                swirls.append((prev, p))
            prev = p
    def mask(P):
        Q = P - base
        st = np.stack([Q @ d, Q[:, 2]], axis=1)
        return sq.segments_mask(st, swirls, 0.0075, 0.003) * (Q @ d > 0.03)
    k.paint(mask, SWIRL, on=["tail"])


def wartortle(src, out):
    k = Kit(src, coat="scales")

    k.add("head", ell(H, HR), BLUE, "head")
    k.add("snout", ell((0, H[1] - 0.09, H[2] - 0.05), (0.112, 0.075, 0.068)), BLUE, "head")
    k.add("neck", tube([(0, -0.02, 0.44), (0, -0.03, 0.54)], [0.085, 0.09]), BLUE, "head")

    sq.torso(k, TC, TR, SC, SR, RIM_PLANE, RIM_R, CREAM, SHELL, RIM_COL)
    sq.plastron_lines(k, TC, TR, RIM_PLANE, scale=1.3, w=0.0038)
    sq.shell_plates(k, SC, SR, RIM_PLANE, RIM_R, w=0.0042, hexr=0.07, reach=0.25)

    # Arms out, forward and a little down; three white claws on each hand
    sq.limb(k, "arm", [(0.11, -0.03, 0.41), (0.165, -0.07, 0.34), (0.175, -0.13, 0.29)], [0.05, 0.046, 0.042],
            BLUE, "arm{s}")
    sq.paw(k, "hand", (0.178, -0.152, 0.28), (0.048, 0.056, 0.04), (0.25, -1), BLUE, "arm{s}", amp=0.2)
    claws(k, "handclaw", (0.192, -0.203, 0.272), (0.25, -1, -0.3), 0.026, 0.024, "arm{s}")
    # Thick legs set wide, feet with three white claws
    sq.limb(k, "leg", [(0.09, 0.0, 0.23), (0.11, -0.015, 0.12), (0.118, -0.02, 0.065)], [0.092, 0.08, 0.066], BLUE,
            "leg{s}")
    sq.paw(k, "foot", (0.122, -0.05, 0.036), (0.072, 0.1, 0.038), (0.12, -1), BLUE, "leg{s}", amp=0.13)
    claws(k, "footclaw", (0.128, -0.145, 0.03), (0.1, -1, -0.1), 0.03, 0.026, "leg{s}")

    ears(k)
    wave_tail(k)

    # The mouth: a wide line, nearly straight, a small white fang hanging at each side
    sq.smile(k, (0, H[1] - 0.163, H[2] - 0.06), unit(0, -1, 0.15), (0.07, 0.024), curve=0.25, thick=0.5)
    for s in (1, -1):
        o = np.array([0.038 * s, H[1] - 0.158, H[2] - 0.057])
        k.add(f"fang{'R' if s > 0 else 'L'}", hull([o + (-0.009, 0, 0.004), o + (0.009, 0, 0.004),
                                                    o + (0, 0.012, 0.004), o + (0, -0.002, -0.024)]),
              CLAW, "head", fuse=False, surface="shell")

    # Fierce brown eyes: the brow cuts each one's top, low at the inner corner
    base = sq.round_eye(iris=IRIS, look_in=0.12)
    LID, INK = srgb(BLUE), srgb(sq.INK)
    def eye(u, v):
        c = base(u, v)
        edge = 0.42 + 0.38 * u
        c[v > edge + 0.16] = LID
        c[np.abs(v - edge - 0.03) <= 0.13] = INK
        return c
    de = unit(0.5, -0.82, 0.2)
    def eyes(state):
        sq.eye_patch(k, state, H, 0.138, de, (0.036, 0.046), eye, BLUE, sq.INK)

    k.build(voxel=VOXEL, band=0.004)
    k.eye_states(eyes)
    k.export(out)



if __name__ == "__main__":
    wartortle(*args())
