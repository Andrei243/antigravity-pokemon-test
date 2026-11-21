# Pignite sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Tepig's script (tepig.py, imported: its colours, snout, eye, curly tail, hooves and the way the head's cap is painted):
#   blender --background --factory-startup --python sculpts/pignite.py -- <the export's Pignite.glb> <out.glb>
# From the references: a stout pig standing upright, its head and body one orange egg wider below, no neck; a dark cap
# over the crown coming down to a point between the eyes, its edge the fierce brows; two tall dark ears standing up in
# a narrow V; a small red snout with two nostrils; round eyes, white with a black pupil, cut by the brows; a straight
# mouth with two small white tusks up at its corners; a dark brown singlet: the lower body dark, rising up the belly in
# three flame points, straps up the chest's sides to the shoulders; a yellow rope coiled into a swirl hanging on each
# side of the chest; thick arms, orange above and dark brown below a yellow cuff, big dark fists with two pale
# knuckles; short orange legs on pale hooves; a dark curly tail ending in a puff of grey smoke.
# (The pictures overrule the task's text: the body pattern is a dark singlet with yellow rope swirls, not stripes.)
# Reference ratios (3D picture, feet to ear tips): ears above the crown / height 0.23; the egg's width / its height
# 0.73; legs showing under it / height 0.07; an arm's thickness / height 0.16; a swirl / height 0.2.
# The export's joints: body (0, 0, 0.28), arms (+-0.13, 0, 0.32), head (0, -0.02, 0.36), ears (+-0.06, -0.02, 0.46),
# legs (+-0.07, 0, 0.12), tail (0, 0.1, 0.18).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, boxy, eye_look, args, outward, ring_band, \
    lumpy, hull, sibling

tp = sibling("tepig")

ORANGE, YELLOW, INK, WHITE = tp.ORANGE, tp.YELLOW, tp.INK, tp.WHITE
DARK = "#4A3934"            # the singlet, the cap, the ears, the forearms
SNOUT = "#D8524C"
PALE = "#E4E2E8"            # knuckles and hooves
SMOKE = "#5A5658"
MOUTHLINE = "#3A2422"

# --- the frame ----------------------------------------------------------------------------------------------------------
T = np.array([0.0, 0.0, 0.275])             # the egg of the body
TR = np.array([0.172, 0.152, 0.215])
H = np.array([0.0, -0.018, 0.418])          # the head's part of the egg, standing forward a little for the face
HR = np.array([0.15, 0.132, 0.102])
EYE_X, EYE_Z, EYE_HALF = 0.07, 0.448, (0.03, 0.035)


def pear(V):
    V = V.copy()
    s = 1 + 0.1 * np.clip(-V[:, 2], 0, 1) - 0.1 * np.clip(V[:, 2], 0, 1) ** 1.5
    V[:, 0] *= s; V[:, 1] *= s
    return V


def on_torso(x, z, lift=0.0):
    """A point on the egg's front at (x, z), its widening counted, and its normal."""
    t = (z - T[2]) / TR[2]
    s = 1 + 0.1 * max(-t, 0) - 0.1 * max(t, 0) ** 1.5
    rx, ry = TR[0] * s, TR[1] * s
    q = max(0.0, 1 - t * t - (x / rx) ** 2)
    p = np.array([x, T[1] - ry * math.sqrt(q), z])
    n = unit(x / rx ** 2, (p[1] - T[1]) / ry ** 2, t / TR[2] * 0.5)
    return p + n * lift, n


def add_body(k):
    k.add("torso", ell(T, TR, deform=pear), ORANGE, "body")
    k.add("head", ell(H, HR, deform=boxy(2.2)), ORANGE, "head")
    k.add("cheek", ell(H + (0, -0.02, -0.04), (0.14, 0.12, 0.07)), ORANGE, "head")


def add_ears(k):
    for side, s in (("R", 1), ("L", -1)):
        d = unit(0.2 * s, 0.22, 0.95)
        base = np.array((0.058 * s, 0.0, 0.49))
        flat = np.cross(d, unit(1.0 * s, 0.3, 0)); flat /= np.linalg.norm(flat)
        pts = [base, base + d * 0.05, base + d * 0.1, base + d * 0.15, base + d * 0.185]
        k.add("ear" + side, blade(pts, [0.03, 0.036, 0.032, 0.02, 0.006], flat, 0.45), DARK, "ear" + side)


def zig(x):
    """The singlet's top edge on the belly: three flame points, the middle one tallest."""
    ax = np.abs(x)
    mid = 0.305 - 1.9 * ax
    side = 0.27 - 2.2 * np.abs(ax - 0.068)
    return np.maximum(np.maximum(mid, side), 0.205)


def paint_body(k):
    def singlet(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        ax = np.abs(x)
        front = (y < 0.02)
        belly = inside(z - zig(x), 0.003)
        strap = inside(0.118 + 0.12 * np.clip(z - 0.25, 0, 1) - ax, 0.003) * (z < 0.37)
        back = inside(z - 0.33, 0.003)
        m = np.where(front, np.maximum(belly, strap), np.maximum(back, strap))
        return m * (z > 0.05)
    k.paint(singlet, DARK, on=["torso"])
    def cap(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        thr = 0.452 + 0.2 * np.abs(x) - 0.6 * np.clip(y + 0.04, 0, 1)
        return inside(thr - z, 0.003) * (z > 0.36)
    k.paint(cap, DARK, on=["head", "torso", "cheek", "ear"])


def add_arms(k):
    """Thick arms carried forward and a little down: orange above, a yellow cuff, a dark forearm and a big fist."""
    SH, EL, WR = np.array((0.145, 0.0, 0.345)), np.array((0.21, -0.065, 0.27)), np.array((0.205, -0.105, 0.222))
    FIST = np.array((0.205, -0.138, 0.198))
    k.add("shoulder", ell(SH + (0.01, 0, 0), (0.06, 0.06, 0.062)), ORANGE, "arm{s}", mirror=True)
    k.add("upper", tube([SH, EL], [0.054, 0.052]), ORANGE, "arm{s}", mirror=True)
    k.add("fore", tube([EL, WR], [0.055, 0.058]), DARK, "arm{s}", mirror=True)
    k.add("fist", ell(FIST, (0.066, 0.064, 0.068), deform=boxy(2.4)), DARK, "arm{s}", mirror=True)
    d = unit(*(WR - EL))
    def forearm(P):
        Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
        return inside(-((Q - EL - d * 0.012) @ d), 0.0025) * (Q[:, 0] > 0.12) * inside(np.linalg.norm(Q - FIST, axis=1)
                                                                                         - 0.17, 0.004)
    k.paint(forearm, DARK, on=["upper", "fore", "fist", "shoulder"])
    def upperarm(P):
        Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
        return inside((Q - EL - d * 0.012) @ d, 0.0025) * (Q[:, 0] > 0.12) * inside(np.linalg.norm(Q - EL, axis=1)
                                                                                     - 0.1, 0.004)
    k.paint(upperarm, ORANGE, on=["upper", "fore", "shoulder"])
    # the yellow cuff at the top of the dark forearm
    k.add("cuff", ring_band(EL + d * 0.03, d, 0.056, 0.03, thick=0.008), YELLOW, "arm{s}", fuse=False, mirror=True,
          surface="cloth")
    # two pale knuckles on the fist's front top
    for i, dx in enumerate((-0.024, 0.024)):
        k.add(f"knuckle{i}", outward(ell(FIST + (dx, -0.058, 0.03), (0.022, 0.02, 0.02))), PALE, "arm{s}", fuse=False,
              mirror=True, surface="shell")


def add_legs(k):
    k.add("haunch", ell((0.085, 0.0, 0.105), (0.07, 0.075, 0.07)), ORANGE, "leg{s}", mirror=True)
    k.add("leg", tube([(0.088, -0.008, 0.085), (0.092, -0.02, 0.05)], [0.052, 0.046]), ORANGE, "leg{s}", mirror=True)
    tp.hoof(k, (0.093, -0.034, 0.017), (0.05, 0.058, 0.019), "leg{s}", color=PALE)
    k.paint(lambda P: inside(P[:, 2] - 0.032, 0.0025) * (np.abs(np.abs(P[:, 0]) - 0.092) < 0.06), PALE,
            on=["hoof", "leg"])


def add_swirls(k):
    """A yellow rope on each side of the chest: down from the strap's foot and coiled into a swirl, loose."""
    for side, s in (("R", 1), ("L", -1)):
        cx, cz = 0.098, 0.225
        pts = []
        # the hook at the top, out from the strap
        for a in np.linspace(0, 1, 6):
            pts.append((cx + 0.012 - 0.03 * a, cz + 0.095 - 0.045 * a))
        th = np.linspace(0, 2 * math.pi * 1.15, 40)
        r = 0.048 - 0.032 * th / th[-1]
        for t_, r_ in zip(th, r):
            ang = math.pi / 2 + t_ * 1.0                  # from the top round the inner side and down
            pts.append((cx + r_ * math.cos(ang) * -1.0 + 0.0, cz + 0.002 + r_ * math.sin(ang)))
        P = []
        for x, z in pts:
            p, n = on_torso(x, z, 0.004)
            P.append(p * np.array([s, 1, 1]))
        k.add("swirl" + side, outward(tube(P, [0.0135] * len(P), seg=14, per=2)), YELLOW, "body", fuse=False,
              surface="cloth")


def add_tail(k):
    end = tp.tail(k, (0.0, 0.15, 0.16), 0.036, unit(0.55, 0.83, 0), color=DARK, r=0.013,
                  end=(0.15, 0.32, 0.12))
    for i, (o, r) in enumerate((((0, 0, 0), 0.05), ((0.03, 0.035, 0.03), 0.04), ((-0.025, 0.03, 0.025), 0.038),
                                ((0.01, 0.055, -0.01), 0.036), ((0.045, 0.0, -0.025), 0.034))):
        k.add(f"puff{i}", outward(ell(np.array(end) + o, (r, r, r * 0.92), deform=lumpy(5, 3, 0.08), sub=3)), SMOKE,
              "tail", fuse=False)


def add_face(k):
    snout = tp.snout(k, H + (0, -0.128, -0.02), (0.04, 0.024, 0.028), "head", color=SNOUT, tilt=6)
    CM, CO = srgb(MOUTHLINE), srgb(ORANGE)
    def mouth(u, v):
        c = np.tile(CO, (len(u), 1))
        c[(np.abs(v - (-0.1 * u * u)) < 0.3) & (np.abs(u) < 0.95)] = CM
        return c
    p, n = on_torso(0.0, 0.378)
    p2 = H + (0, -0.118, -0.06)
    k.decal(p2, unit(0, -1, -0.15), (0.058, 0.011), mouth, eps=0.004, rings=8, segs=56)
    # two small tusks up at the mouth's corners
    for s in (1, -1):
        b = H + np.array((0.054 * s, -0.128, -0.06))
        k.add("tusk" + ("R" if s > 0 else "L"),
              hull([b + (0.012, 0.004, 0), b + (-0.012, 0.004, 0), b + (0, -0.012, 0), b + (0, 0.014, 0),
                    b + (0.004 * s, -0.01, 0.036)]), WHITE, "head", fuse=False, surface="shell")


def lay_eyes(k, state):
    CD = srgb(DARK)
    base = tp.round_eye(pupil=(-0.12, -0.08, 0.46, 0.56), glint=(-0.02, 0.12, 0.15))
    def eye(u, v):
        c = base(u, v)
        c[v > 0.5 + 0.5 * u] = CD                  # the brow, lower toward the nose
        return c
    if state == "Fierce":
        def look(u, v):
            c = base(u, v)
            c[v > 0.25 + 0.6 * u] = CD
            return c
    else:
        look = eye_look(state, eye, srgb(ORANGE), srgb(INK), line=0.17)
    p, n = tp.on_ell(EYE_X, EYE_Z, H, HR)
    n = unit(n[0] * 1.1, n[1], n[2] * 0.6)
    k.decal(p, n, EYE_HALF, look, eps=0.004, rings=20, segs=56, mirror=True)


def pignite(src, out):
    k = Kit(src, coat="fur")
    add_body(k)
    add_ears(k)
    add_legs(k)
    add_arms(k)
    paint_body(k)
    add_swirls(k)
    add_tail(k)
    add_face(k)
    k.build(voxel=0.0065, band=0.005)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    pignite(*args())
