# Tauros-Paldea-Combat-Breed sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py):
#   blender --background --factory-startup --python sculpts/taurospaldeacombatbreed.py -- <the export> <out.glb>
#
# Grown from tauros.py (which builds as it is read, so its frame is copied here once, as a function of the breed that
# taurospaldeablazebreed.py and taurospaldeaaquabreed.py call through `sibling`). The same bull as Kanto's: a deep
# barrel on thick legs, the head carried low in front of a great shaggy mane, small angry eyes under a brow slanting to
# the nose, studs on the brow, three whip tails with tufts. What the three Paldean pictures change, and share:
# - all three: a dark grey hide and black hooves; a black mane far larger than Kanto's, over the back of the head, the
#   neck and shoulders, hanging ragged under the chest to the knees and running half way along the back; three grey
#   studs in a column down the middle of the brow, the lowest the largest; a near-black muzzle; black tufts.
# - Combat: short thick grey horns out from the head's sides, curving a little forward and up; the mouth a little
#   open (pink inside); three whips trailing back and up, each with a black tuft.
# - Blaze: long straight grey horns reaching far out to the sides, a little forward and up; the mane wilder, its top
#   standing in flame-like points with a few red streaks in it (the mane itself is black: the task's "red mane like
#   flame" is the pictures' few red streaks); the three tails braided into one thick rope that rises behind and
#   curls forward over the back, ending in three black tufts.
# - Aqua: thick ribbed grey horns curling back and down round the head's sides like a ram's; blue drops on the front
#   of the black mane under the chin (the mane is black: the task's "blue mane" is the pictures' drops); three whips
#   trailing straight back.
# The export's joints: body (0, 0.04, 0.44), head (0, -0.3, 0.5), tail (0, 0.31, 0.5), legs (+-0.11, -0.2, 0.38),
# hind (+-0.11, 0.24, 0.38).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, lumpy, hull, outward, polygon, args

HIDE, MANE, HOOF = "#48484C", "#1E1D20", "#18181A"
HORN, STUD, NOSE, WHIP, TUFT = "#A9A9AF", "#B4B4BA", "#2C2C31", "#46464C", "#141416"
WHITE, INK = "#F6F4EE", "#121012"
RED, DROP = "#E0383A", "#2AA4E4"
H = np.array([0.0, -0.4, 0.47])         # the head's centre
HR = np.array([0.123, 0.143, 0.125])
MC = np.array([0.0, -0.13, 0.53])        # the mane's centre


def smooth_path(pts, n):
    """n points along a smooth curve through pts (Catmull-Rom)."""
    P = np.array(pts, float)
    P = np.vstack([2 * P[0] - P[1], P, 2 * P[-1] - P[-2]])
    out_pts = []
    segs = len(P) - 3
    for i in range(segs):
        p0, p1, p2, p3 = P[i], P[i + 1], P[i + 2], P[i + 3]
        for t in np.linspace(0, 1, max(2, n // segs), endpoint=(i == segs - 1)):
            out_pts.append(0.5 * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                                  + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    return np.array(out_pts)


def tuft(k, name, tip, d, size=1.0):
    """A black brush at a whip's end: three pointed lobes, the middle one longest."""
    a = np.cross(d, (0, 1.0, 0))
    if np.linalg.norm(a) < 1e-3:
        a = np.cross(d, (1.0, 0, 0))
    a /= np.linalg.norm(a)
    b = np.cross(d, a)
    for j, (sa, sb) in enumerate(((0.0, 0.0), (0.75, 0.3), (-0.75, -0.3))):
        dd = d + a * sa * 0.55 + b * sb * 0.55; dd /= np.linalg.norm(dd)
        w = (0.03 if j == 0 else 0.022) * size
        L = (0.095 if j == 0 else 0.07) * size
        base = tip - d * 0.018 * size
        k.add(f"{name}{j}", hull([base + a * w, base - a * w, base + b * w * 0.75, base - b * w * 0.75,
                                  base + dd * L * 0.45 + a * w * 0.95, base + dd * L * 0.45 - a * w * 0.95,
                                  base + dd * L * 0.45 + b * w * 0.6, base + dd * L * 0.45 - b * w * 0.6,
                                  base + dd * L]), TUFT, "tail", fuse=False, surface="hair")


def body(k):
    k.add("barrel", ell((0, 0.05, 0.44), (0.17, 0.3, 0.19)), HIDE, "body")
    k.add("belly", ell((0, 0.0, 0.3), (0.15, 0.23, 0.1)), HIDE, "body")
    k.add("chest", ell((0, -0.19, 0.46), (0.17, 0.14, 0.2)), HIDE, "body")
    k.add("rump", ell((0, 0.25, 0.49), (0.15, 0.125, 0.155)), HIDE, "body")


def head(k, mouth_open):
    k.add("neck", tube([(0, -0.18, 0.53), (0, -0.3, 0.5), (0, -0.37, 0.48)], [0.11, 0.09, 0.08]), HIDE, "head")
    k.add("head", ell(H, HR), HIDE, "head")
    # the muzzle carried low, turned down with the head
    k.add("muzzle", ell((0, -0.515, 0.38), (0.088, 0.086, 0.081), rot=(42, 0, 0)), HIDE, "head")
    # the muzzle dark below the eyes, softly
    k.paint(lambda P: inside((P[:, 1] + 0.55) + 0.7 * (P[:, 2] - 0.42), 0.015) * (P[:, 1] < -0.4)
            * inside(P[:, 2] - 0.47), NOSE)
    k.decal((0.034, -0.592, 0.35), unit(0.25, -0.75, -0.6), (0.013, 0.01), "#060606", eps=0.003, rings=4, segs=20,
            mirror=True)
    if mouth_open:
        # a little open under the muzzle's front: dark, the tongue pink in its lower half
        DK, PK = srgb("#3A1416"), srgb("#D8707A")

        def mouth(u, v):
            c = np.tile(DK, (len(u), 1))
            c[(u / 0.8) ** 2 + ((v + 0.75) / 0.6) ** 2 < 1] = PK
            return c
        k.decal((0, -0.548, 0.302), unit(0, -0.45, -0.9), (0.045, 0.024), mouth, eps=0.003, rings=12, segs=48,
                up=(0, -1, 0))
    # three grey studs down the middle of the brow, the lowest the largest
    for i, (z, r) in enumerate(((1.05, 0.0125), (0.75, 0.0135), (0.42, 0.0175))):
        d = unit(0, -0.8, z)
        n = d / HR ** 2; n /= np.linalg.norm(n)
        c = H + d / np.linalg.norm(d / HR)
        k.add(f"stud{i}", ell(c - n * r * 0.3, (r, r, r * 0.8), sub=3), STUD, "head", fuse=False, surface="shell")


def mane(k, breed):
    lump = 0.13 if breed == "Blaze" else 0.1
    k.add("mane", ell(MC, (0.205, 0.21, 0.22), deform=lumpy(7, 5, lump)), MANE, "body")
    k.add("manetop", ell((0, -0.26, 0.6), (0.145, 0.12, 0.13), deform=lumpy(6, 4, lump)), MANE, "head")
    # the drape hanging under the throat and chest
    k.add("drape", ell((0, -0.29, 0.37), (0.14, 0.1, 0.15), deform=lumpy(6, 4, 0.12)), MANE, "body")
    # the ragged fringe: tufts along the back edge, hanging under the chest, over the brow behind the horns
    tufts = [
        ([(0.0, 0.02, 0.68), (0.0, 0.1, 0.67), (0.0, 0.17, 0.63)], [0.085, 0.05, 0.014], (1, 0, 0)),
        ([(0.11, 0.0, 0.6), (0.13, 0.08, 0.56), (0.14, 0.13, 0.5)], [0.07, 0.04, 0.014], (1, 0, 0)),
        ([(0.07, -0.27, 0.32), (0.075, -0.29, 0.25), (0.07, -0.3, 0.19)], [0.065, 0.042, 0.014], (0, 1, 0)),
        ([(0.0, -0.3, 0.32), (0.0, -0.32, 0.24), (0.0, -0.32, 0.18)], [0.065, 0.042, 0.014], (0, 1, 0)),
        ([(0.14, -0.2, 0.34), (0.15, -0.21, 0.27), (0.15, -0.21, 0.22)], [0.06, 0.04, 0.014], (0, 1, 0)),
        ([(0.0, -0.33, 0.6), (0.0, -0.38, 0.61), (0.0, -0.42, 0.59)], [0.06, 0.04, 0.014], (0, 0, 1)),
    ]
    if breed == "Blaze":
        # the top standing in flame-like points, swept back
        tufts += [
            ([(0.0, -0.22, 0.68), (0.0, -0.21, 0.76), (0.0, -0.17, 0.82)], [0.06, 0.035, 0.02], (1, 0, 0)),
            ([(0.0, -0.1, 0.71), (0.0, -0.07, 0.79), (0.0, -0.02, 0.84)], [0.065, 0.035, 0.02], (1, 0, 0)),
            ([(0.0, 0.03, 0.69), (0.0, 0.08, 0.76), (0.0, 0.14, 0.79)], [0.06, 0.03, 0.02], (1, 0, 0)),
            ([(0.1, -0.16, 0.66), (0.13, -0.14, 0.73), (0.16, -0.1, 0.77)], [0.055, 0.03, 0.02], (0.3, 1, 0)),
            ([(0.12, -0.03, 0.66), (0.15, 0.0, 0.72), (0.19, 0.05, 0.74)], [0.055, 0.03, 0.02], (0.3, 1, 0)),
        ]
    for i, (pts, rr, ax) in enumerate(tufts):
        bone = "head" if pts[0][1] < -0.3 and pts[0][2] > 0.55 else "body"
        k.add(f"shag{i}", blade(pts, rr, ax, 0.55), MANE, bone, mirror=pts[0][0] > 0)
    # the hide back where the mane's lower rear edge bled onto the flanks and belly: a ragged line
    k.paint(lambda P: inside(P[:, 2] - 0.43 - 0.025 * np.sin(P[:, 1] * 45), 0.004)
            * smoothstep(-0.13, -0.09, P[:, 1]) * (P[:, 2] > 0.1), HIDE)
    k.paint(lambda P: inside(P[:, 2] - 0.37, 0.004) * smoothstep(-0.19, -0.15, P[:, 1]) * (P[:, 2] > 0.1), HIDE)

    if breed == "Blaze":
        # red streaks in the mane's upper flanks: short strokes painted round the mane's centre
        strokes = [((0.95, 0.66), (1.35, 0.54)), ((1.1, 0.34), (1.5, 0.25))]

        def streaks(P):
            q = P - MC
            az = np.arctan2(np.abs(q[:, 0]), -q[:, 1])           # 0 at the front, pi/2 at the side
            el = np.arctan2(q[:, 2], np.hypot(q[:, 0], q[:, 1]))
            m = np.zeros(len(P))
            for (a0, e0), (a1, e1) in strokes:
                A, E = np.array([a0, e0]), np.array([a1, e1])
                X = np.stack([az, el], 1)
                t = np.clip(((X - A) @ (E - A)) / ((E - A) @ (E - A)), 0, 1)
                dd = np.linalg.norm(X - (A + t[:, None] * (E - A)), axis=1)
                w = 0.055 * (1 - 0.6 * np.abs(2 * t - 1))       # tapering to both ends
                m = np.maximum(m, inside(dd - w, 0.012))
            return m * (np.linalg.norm(q, axis=1) > 0.17) * (q[:, 2] > 0.02)
        k.paint(streaks, RED, on=["mane", "manetop", "shag"])
    if breed == "Aqua":
        # blue drops on the front of the mane under the chin, pointed up, in a shallow smile
        # on the drape's front under the jaw, round its axis: (angle from the front, height)
        DC = np.array([0.0, -0.29, 0.37])
        drops = [(-0.75, 0.33), (-0.38, 0.305), (0.0, 0.295), (0.38, 0.305), (0.75, 0.33)]
        R = 0.019

        def bluedrops(P):
            m = np.zeros(len(P))
            for a, z in drops:
                n = np.array([math.sin(a), -math.cos(a), 0.0])
                t = np.array([math.cos(a), math.sin(a), 0.0])
                c = DC + np.array([math.sin(a) * 0.15, -math.cos(a) * 0.105, z - DC[2]])
                q = P - c
                du, dv, dn = q @ t / R, q[:, 2] / R, q @ n
                # a teardrop: round below, pointed above
                inn = np.where(dv < 0, np.hypot(du, dv) - 1, np.abs(du) - np.clip(1 - dv / 2.0, 0, 1))
                m = np.maximum(m, inside(inn * R, 0.002) * (dv < 2.0) * (np.abs(dn) < 0.05))
            return m
        k.paint(bluedrops, DROP, on=["drape", "mane", "shag"])


def horns(k, breed):
    if breed == "Combat":
        pts = [(0.06, -0.38, 0.52), (0.12, -0.385, 0.545), (0.165, -0.4, 0.575), (0.19, -0.43, 0.6),
               (0.195, -0.46, 0.615)]
        k.add("horn", outward(tube(pts, [0.042, 0.039, 0.033, 0.023, 0.012])), HORN, "head", fuse=False,
              mirror=True, surface="shell")
    elif breed == "Blaze":
        pts = [(0.06, -0.38, 0.52), (0.13, -0.39, 0.545), (0.21, -0.405, 0.57), (0.29, -0.42, 0.6),
               (0.35, -0.43, 0.635)]
        k.add("horn", outward(tube(pts, [0.042, 0.036, 0.029, 0.021, 0.014])), HORN, "head", fuse=False,
              mirror=True, surface="shell")
    else:
        # a ram's curl: out and up from the head's side, back, down and forward round the cheek, ribbed
        ctrl = [(0.06, -0.37, 0.53), (0.13, -0.37, 0.57), (0.19, -0.35, 0.6), (0.225, -0.3, 0.58),
                (0.235, -0.27, 0.52), (0.225, -0.3, 0.46), (0.205, -0.355, 0.445)]
        P = smooth_path(ctrl, 60)
        t = np.linspace(0, 1, len(P))
        r = 0.046 - 0.026 * t ** 1.4
        r = r * (1 + 0.2 * np.maximum(np.sin(t * 2 * math.pi * 6.5), 0) ** 0.6)
        k.add("horn", outward(tube(P, r, seg=18, per=2)), HORN, "head", fuse=False, mirror=True, surface="shell")


def legs(k):
    LEGS = (("fore", "leg{s}", [(0.1, -0.2, 0.42), (0.104, -0.215, 0.27), (0.106, -0.235, 0.15), (0.107, -0.25, 0.045)]),
            ("hind", "hind{s}", [(0.1, 0.26, 0.42), (0.104, 0.3, 0.27), (0.106, 0.31, 0.17), (0.107, 0.28, 0.045)]))
    for name, bone, pts in LEGS:
        k.add(name, tube(pts, [0.09, 0.07, 0.05, 0.047]), HIDE, bone, mirror=True)
        x, y = pts[-1][0], pts[-1][1]
        k.add(name + "hoof", ell((x, y - 0.01, 0.04), (0.052, 0.057, 0.036),
                                deform=lambda V: V * (1 + 0.12 * np.clip(-V[:, 2:3], 0, 1))), HOOF, bone, mirror=True,
              surface="shell")
    k.add("shoulder", ell((0.1, -0.205, 0.33), (0.098, 0.12, 0.185)), HIDE, "leg{s}", mirror=True)
    k.add("thigh", ell((0.1, 0.265, 0.33), (0.1, 0.145, 0.19)), HIDE, "hind{s}", mirror=True)


def tails(k, breed):
    TB = np.array([0.0, 0.36, 0.56])
    if breed == "Blaze":
        # three strands braided into one rope, rising behind and curling forward over the back
        C = smooth_path([(0, 0.33, 0.55), (0, 0.43, 0.58), (0, 0.49, 0.64), (0, 0.5, 0.71), (0, 0.465, 0.77),
                         (0, 0.4, 0.8)], 72)
        T = np.gradient(C, axis=0); T /= np.linalg.norm(T, axis=1, keepdims=True)
        A = np.cross(T, (1.0, 0, 0)); A /= np.linalg.norm(A, axis=1, keepdims=True)
        B = np.cross(T, A)
        s = np.linspace(0, 1, len(C))
        for i in range(3):
            ph = 2 * math.pi * i / 3 + s * 2 * math.pi * 3.2
            off = 0.022 * (np.cos(ph)[:, None] * A + np.sin(ph)[:, None] * B)
            rad = np.full(len(C), 0.024) * (1 - 0.15 * s)
            k.add(f"braid{i}", outward(tube(C + off, rad, seg=12, per=2)), WHIP, "tail", fuse=False, surface="hair")
        top = C[-1]
        for i, d in enumerate((unit(0.0, -1, 0.25), unit(0.6, -0.8, 0.35), unit(-0.6, -0.8, 0.35))):
            pts = np.array([top, top + d * 0.05, top + d * 0.09 + np.array([0, 0, 0.012])])
            k.add(f"whip{i}", outward(tube(pts, [0.017, 0.016, 0.015], seg=10)), WHIP, "tail", fuse=False,
                  surface="hair")
            dd = pts[-1] - pts[-2]; dd /= np.linalg.norm(dd)
            tuft(k, f"tuft{i}", pts[-1], dd, 1.15)
        return
    if breed == "Aqua":
        TAILS = [[TB, (0.01, 0.41, 0.61), (0.03, 0.44, 0.64), (0.06, 0.46, 0.66)],
                 [TB, (0.05, 0.41, 0.6), (0.13, 0.43, 0.61), (0.2, 0.43, 0.62)],
                 [TB, (-0.03, 0.41, 0.6), (-0.07, 0.44, 0.62), (-0.1, 0.45, 0.63)]]
    else:
        TAILS = [[TB, (0.01, 0.4, 0.64), (0.02, 0.42, 0.72), (0.03, 0.43, 0.78)],
                 [TB, (0.05, 0.4, 0.62), (0.13, 0.42, 0.68), (0.19, 0.42, 0.72)],
                 [TB, (-0.04, 0.4, 0.62), (-0.1, 0.42, 0.67), (-0.15, 0.42, 0.7)]]
    for i, pts in enumerate(TAILS):
        pts = np.array(pts, float)
        k.add(f"whip{i}", outward(tube(pts, [0.017, 0.016, 0.015, 0.015], seg=10)), WHIP, "tail", fuse=False,
              surface="hair")
        d = pts[-1] - pts[-2]; d /= np.linalg.norm(d)
        tuft(k, f"tuft{i}", pts[-1], d)


# --- the eyes: small and angry, white with a dark pupil, cut along the top by the brow slanting to the nose -----------
CH, CW, CK = srgb(HIDE), srgb(WHITE), srgb(INK)


def eye(u, v):
    c = np.tile(CW, (len(u), 1))
    c[((u + 0.15) / 0.42) ** 2 + ((v + 0.1) / 0.6) ** 2 < 1] = CK
    c[u * u + v * v > 0.74] = CK
    return c


SHAPE = [(-1.0, -0.15), (-0.3, 0.2), (1.0, 0.75), (1.0, -0.3), (0.4, -0.7), (-0.5, -0.6)]
DE = unit(0.72, -0.62, 0.25)
E = H + np.array([0, -0.04, 0.02]) + DE * 0.088


def eyes(k, state):
    col = eye_look(state, eye, CH, srgb("#0A0A0A"), line=0.15)
    k.decal(E, DE, (0.04, 0.034), col, eps=0.004, rings=20, segs=56, outline=polygon(SHAPE))
    m = np.array([-1, 1, 1.0])
    k.decal(E * m, DE * m, (0.037, 0.031), lambda u, v: col(-u, v), eps=0.004, rings=20, segs=56,
            outline=polygon([(-a, b) for a, b in SHAPE]))


def tauros(src, out_path, breed):
    k = Kit(src, coat="fur")
    body(k)
    head(k, mouth_open=(breed == "Combat"))
    mane(k, breed)
    horns(k, breed)
    legs(k)
    tails(k, breed)
    k.build(voxel=0.007, band=0.004)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out_path)


if __name__ == "__main__":
    src, out = args()
    tauros(src, out, "Combat")
