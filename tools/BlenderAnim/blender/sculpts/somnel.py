# Somnel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/somnel.py -- <the export's Somnel.glb> <out.glb>
#
# From its picture (No. 10117, a catfish): a small, smooth, dark olive catfish, a broad flat head as wide as its plump
# body, a huge wide mouth across the whole front, open a little (a dark brown inside, a pale lower lip and chin), two
# very long whiskers hanging from the mouth's corners down to the ground and curling out, two short ones under the
# chin, sleepy eyes on bulges at the top corners of the head (pale, a small pupil low, a heavy olive lid over the top
# half), two nostrils between them; a pale yellow-green belly, a low hump of a dorsal fin, round paddles of pectoral and
# pelvic fins and a round tail; a little bubble at its lips like a snore (kept: the brief names it as its own).
# Somnoros grows from this script's frame (sibling("somnel")): its head, mouth, eyes and whiskers are made by the same
# functions with other numbers.
# Joints: body (0, 0.05, 0.208), head (0, -0.1, 0.208), tail (0, 0.24, 0.208), finR/L (+-0.13, -0.04, 0.128).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, tube, ell, slab, srgb, inside, unit, eye_look, args

OLIVE, DARK, BELLY, MOUTH, THROAT, LIP, SCLERA, INK, WHISKER = ("#6A6744", "#4E4C32", "#B9B48A", "#7E5E54",
                                                                "#4C3430", "#C4C99C", "#EEE8C4", "#18180E",
                                                                "#5C5A3C")


def board(k):
    V = np.array([v.co[:] for v in k.ob.data.vertices])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    sq = 1.3 * max(h, 0.85 * max(w, l))
    print("KIT: board: shows %.3f (corner sum %.3f) in a square of %.3f: %.2f of it" % (
        np.ptp(proj), 0.853 * w + 0.522 * l, sq, np.ptp(proj) / sq))


def flat_head(squash=0.22):
    """A catfish's head: flatter on top and under, broad at the front."""
    def f(V):
        V = V.copy()
        V[:, 2] *= 1 - squash * np.abs(V[:, 2]) ** 3
        front = np.clip(-V[:, 1], 0, 1)
        V[:, 0] *= 1 + 0.12 * front
        return V
    return f


def head_angle(P, hc):
    """Angle round the head's upright axis from straight ahead (+ toward +X), in degrees."""
    return np.degrees(np.arctan2(P[:, 0] - hc[0], -(P[:, 1] - hc[1])))


def mouth(k, hc, zm, half, opening, lip, behind):
    """
    The wide mouth across the front: a lens of dark open mouth (opening tall at the middle) between the olive upper lip
    and a pale lower lip lip tall, over half degrees each side of straight ahead; only in front of y = hc.y + behind.
    """
    def lens(P, up, down):
        a = head_angle(P, hc) / half
        f = np.clip(1 - a * a, 0, 1)
        z = P[:, 2] - zm
        return inside(np.abs(a) - 1, 0.03) * inside(z - up * f, 0.0025) * inside(-down * f - z, 0.0025) \
            * inside(P[:, 1] - (hc[1] + behind), 0.004)
    k.paint(lambda P: lens(P, opening * 0.45, opening * 0.55 + lip), LIP)
    k.paint(lambda P: lens(P, opening * 0.45, opening * 0.55), MOUTH)
    k.paint(lambda P: lens(P, opening * 0.45, -opening * 0.05), THROAT)
    k.paint(lambda P: lens(P, opening * 0.45 + 0.006, -opening * 0.45 + 0.0) * (1 - lens(P, opening * 0.45, 9)),
            INK)


def whisker(k, name, pts, r0, r1, colour=WHISKER, bone="head"):
    n = len(pts)
    k.add(name, tube(pts, list(np.linspace(r0, r1, n)), seg=12), colour, bone, fuse=False, mirror=True, surface="skin")


# --- the sleepy eyes: pale, a small dark pupil low, a heavy lid over the top half with its dark edge ------------------
CI, CS, CL = srgb(INK), srgb(SCLERA), srgb(OLIVE)


def sleepy(lid_colour, glint=False):
    cg = srgb("#FFFFFF")
    def look(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(CS, (len(u), 1))
        pr = np.sqrt((u + 0.05) ** 2 + (v + 0.28) ** 2)
        c[pr < 0.38] = CI
        if glint:
            c[(u + 0.18) ** 2 + (v + 0.16) ** 2 < 0.1 ** 2] = cg
        c[r > 0.84] = CI
        lid = 0.12 - 0.06 * u
        c[v > lid] = lid_colour
        c[np.abs(v - lid) < 0.13] = CI
        c[(r > 0.84) & (v > lid + 0.13)] = lid_colour
        return c
    return look


def eye_pair(k, state, at, n, half, lid_colour, glint=False):
    look = sleepy(lid_colour, glint)
    if state == "Fierce":
        # the lid lower still and slanting toward the nose
        def fierce(u, v):
            c = look(u, v)
            edge = -0.05 + 0.35 * u
            c[v > edge] = lid_colour
            c[np.abs(v - edge) < 0.13] = CI
            return c
        f = fierce
    else:
        f = eye_look(state, look, lid_colour, CI, line=0.15)
    k.decal(at, n, half, f, eps=0.002, rings=20, segs=56, mirror=True)


# --- Somnel ---------------------------------------------------------------------------------------------------------
HC, HR = np.array([0.0, -0.15, 0.215]), np.array([0.19, 0.15, 0.13])
ZM = 0.195
BULGE = np.array([0.125, -0.2, 0.3])
EYE_N = unit(0.5, -0.72, 0.45)
EYE_HALF = (0.034, 0.032)


def body(k):
    k.add("head", ell(HC, HR, deform=flat_head()), OLIVE, "head")
    k.add("bulge", ell(BULGE, (0.04, 0.04, 0.036)), OLIVE, "head", mirror=True)
    # behind the wide head, a fat tadpole's body that tapers on into the tail as one fish
    k.add("trunk", tube([(0, -0.08, 0.212), (0, 0.02, 0.212), (0, 0.12, 0.214), (0, 0.22, 0.218), (0, 0.3, 0.222),
                         (0, 0.36, 0.224)], [0.15, 0.15, 0.125, 0.09, 0.06, 0.042], seg=28), OLIVE, "body")
    k.add("stalk", tube([(0, 0.2, 0.218), (0, 0.29, 0.222), (0, 0.36, 0.224)], [0.092, 0.062, 0.042]), OLIVE, "tail")
    k.add("dorsal", ell((0, 0.06, 0.345), (0.026, 0.1, 0.04), rot=(-10, 0, 0)), DARK, "body")
    # the pectorals on the flanks behind the head, lying back along the body; small pelvics far back underneath
    k.add("pec", pectoral((0.15, -0.045, 0.205), 0.135, 0.052), DARK, "fin{s}", fuse=False, mirror=True, surface="skin")
    k.add("pel", ell((0.07, 0.2, 0.135), (0.012, 0.032, 0.026), rot=(15, 0, -45)), DARK, "body", mirror=True)
    k.add("anal", ell((0, 0.27, 0.17), (0.014, 0.045, 0.03)), DARK, "tail")


def pectoral(root, length, half, thick=0.014):
    """A flat rounded fin from its root on the flank back along the body, its face turned out and a little forward."""
    out = [(length * 0.5 * (1 - math.cos(t)), half * math.sin(t) * (0.6 + 0.4 * math.sin(t / 2) ** 2))
           for t in np.linspace(0, 2 * math.pi, 28, endpoint=False)]
    return slab(out, root, unit(0.3, 1, -0.28), (0, 0, 1), thick)


def tail_fin(k):
    # a round tail, its face turned 40 degrees toward +X so that both cameras see it
    k.add("caudal", round_tail((0, 0.35, 0.224), 0.13, 0.095, 40), DARK, "tail", fuse=False, surface="skin")


def round_tail(root, length, half, turn, thick=0.016):
    """A round fan of a tail from its root back, two soft lobes with a shallow notch between."""
    out = []
    for t in np.linspace(-math.pi / 2, math.pi / 2, 25):
        r = 1 - 0.1 * math.cos(2 * t) ** 8 * (abs(t) < 0.4)
        out.append((length * 0.5 + length * 0.5 * math.cos(t) * r, half * math.sin(t)))
    out += [(0.0, half * 0.35), (0.0, -half * 0.35)]
    a = math.radians(turn)
    return slab(out[::-1], root, (math.sin(a), math.cos(a), 0), (0, 0, 1), thick)


def markings(k):
    def belly(P):
        zb = 0.165 - 0.025 * np.clip((P[:, 1] + 0.05) / 0.3, 0, 1)
        return inside(P[:, 2] - zb, 0.004) * inside(P[:, 1] - 0.45, 0.01)
    k.paint(belly, BELLY, on=["trunk", "stalk", "head"])
    def chin(P):
        return inside(P[:, 2] - (ZM - 0.02), 0.004) * inside(P[:, 1] - (HC[1] + 0.06), 0.01)
    k.paint(chin, BELLY, on=["head"])
    mouth(k, HC, ZM, 62.0, 0.03, 0.016, -0.02)


def whiskers(k):
    whisker(k, "whisker", [(0.17, -0.25, 0.2), (0.205, -0.27, 0.165), (0.235, -0.27, 0.11), (0.26, -0.235, 0.065),
                           (0.285, -0.17, 0.05), (0.3, -0.09, 0.058)], 0.0095, 0.0065)
    whisker(k, "chin", [(0.055, -0.255, 0.115), (0.06, -0.268, 0.097), (0.066, -0.274, 0.082)], 0.0075, 0.0065)


def bubble(k):
    k.add("bubble", ell((0.23, -0.27, 0.215), (0.03, 0.032, 0.032), sub=3), "#CFE8F2", "head", fuse=False,
          surface="plastic")
    k.add("bubbleglint", ell((0.222, -0.298, 0.23), (0.009, 0.007, 0.009), sub=2), "#FFFFFF", "head", fuse=False,
          surface="plastic")


def nostrils(k):
    k.decal((0.04, -0.287, 0.262), unit(0.15, -0.8, 0.55), (0.009, 0.007), "#2E3420", eps=0.0025, rings=6, segs=24,
            mirror=True)


def eyes(k, state):
    p = BULGE + EYE_N * 0.036
    eye_pair(k, state, p, EYE_N, EYE_HALF, CL)


def somnel(src, out):
    k = Kit(src, coat="scales")
    body(k)
    tail_fin(k)
    markings(k)
    whiskers(k)
    bubble(k)
    nostrils(k)
    k.build(voxel=0.0042, smooth=5, band=0.006, wband=0.04)
    board(k)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    somnel(*args())
