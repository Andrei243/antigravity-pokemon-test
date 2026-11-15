# Zangoose sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/zangoose.py -- <the export's Zangoose.glb> <out.glb>
# From the references: a stout white mongoose standing upright, pear-shaped, wide in the belly on short legs with
# clawed toes; a broad round head on no neck, a ruff of spiky fur round the shoulders. Two tall pointed ears: the left
# one white and upright, the right one red and swept out sideways, and from the red ear's root a red zigzag runs down
# round the outside of the right eye to the cheek. Angry eyes, a pink iris and a dark pupil under a scowl sloping to
# the nose; a small pink nose, a frowning little mouth. A red zigzag like a lightning bolt across the chest; the
# forearms and hands red, their top a jagged edge at the elbow; two long curved black claws on each hand hanging down.
# A big white tail of three broad points rising out behind to the right.
# (The task's "standing upright": stout, not slender: the pictures' belly is wider than the head by half again.)
# The export's joints: body (0, 0, 0.32), arms (+-0.1, -0.01, 0.4), head (0, -0.02, 0.46), ears (+-0.05, -0.01, 0.6),
# legs (+-0.07, 0.01, 0.2), tail (0, 0.08, 0.24).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, args, outward, tri, smoothstep

WHITE, RED, CLAW = "#F2F0F5", "#E2405C", "#36343C"
IRIS, PUPIL, INK, NOSE, MOUTH = "#DA6A86", "#2A1018", "#2A2026", "#EE9EB2", "#5A3A44"

B = np.array([0, 0.0, 0.29])                 # the belly's middle
BR = np.array([0.168, 0.145, 0.19])
H = np.array([0, -0.035, 0.575])             # the head's middle
HR = np.array([0.122, 0.104, 0.098])
SH = np.array([0.135, -0.015, 0.425])        # shoulder, elbow, wrist, hand
EL = np.array([0.188, -0.04, 0.335])
WR = np.array([0.2, -0.07, 0.245])
HAND = np.array([0.2, -0.08, 0.215])


def polyline_dist(x, z, pts):
    """Distance from points (x, z) to a broken line through pts."""
    p = np.array(pts, float)
    q = np.stack([x, z], 1)
    d = np.full(len(x), 9.0)
    for a, b in zip(p[:-1], p[1:]):
        ab = b - a
        t = np.clip(((q - a) @ ab) / (ab @ ab), 0, 1)
        d = np.minimum(d, np.linalg.norm(q - (a + t[:, None] * ab), axis=1))
    return d


def head_coords(P):
    """Points about the head: the distance round its upright axis from the front (x = angle times radius) and z."""
    a = np.arctan2(P[:, 0] - H[0], -(P[:, 1] - H[1]))
    return a * HR[0], P[:, 2]


def add_body(k):
    def pear(V):
        V = V.copy()
        s = 1 + 0.06 * np.clip(-V[:, 2], 0, 1) - 0.2 * np.clip(V[:, 2], 0, 1) ** 1.4
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("belly", ell(B, BR, deform=pear), WHITE, "body")
    k.add("chest", ell((0, -0.01, 0.45), (0.13, 0.11, 0.08)), WHITE, "body")
    k.add("hips", ell((0, 0.01, 0.17), (0.155, 0.13, 0.1)), WHITE, "body")
    # the ruff: spiky tufts round the shoulders, out and a little down, longest at the sides
    for i in range(11):
        a = math.radians(-100 + 200 * i / 10)            # round the front from side to side
        side = abs(math.sin(a))
        r = 0.12
        base = np.array([r * math.sin(a), -0.01 - 0.1 * math.cos(a), 0.48 - 0.02 * (1 - side)])
        d = unit(math.sin(a) * 1.0, -math.cos(a) * 0.8, -0.5)
        L = 0.035 + 0.035 * side
        k.add(f"ruff{i}", blade([base, base + d * L * 0.5, base + d * L], [0.034, 0.022, 0.005], (0, 0, 1), 0.6),
              WHITE, "body")


def add_legs(k):
    k.add("thigh", ell((0.095, 0.0, 0.14), (0.075, 0.085, 0.09)), WHITE, "leg{s}", mirror=True)
    k.add("shin", tube([(0.1, -0.005, 0.1), (0.104, -0.02, 0.045)], [0.05, 0.044]), WHITE, "leg{s}", mirror=True)
    k.add("foot", ell((0.105, -0.045, 0.026), (0.048, 0.07, 0.027)), WHITE, "leg{s}", mirror=True)
    for i, dx in enumerate((-0.026, 0.0, 0.026)):
        a = np.array([0.105 + dx, -0.1, 0.02])
        k.add(f"toe{i}", outward(blade([a, a + (dx * 0.3, -0.022, -0.004), a + (dx * 0.5, -0.04, -0.014)],
                                       [0.011, 0.008, 0.002], (0, 0, 1), 0.6)),
              WHITE, "leg{s}", fuse=False, mirror=True, surface="shell")


def add_arms(k):
    k.add("arm", tube([SH - (0.03, 0, 0), SH, EL, WR], [0.052, 0.05, 0.046, 0.042]), WHITE, "arm{s}", mirror=True)
    k.add("hand", ell(HAND, (0.044, 0.042, 0.042)), WHITE, "arm{s}", mirror=True)
    d = unit(*(WR - EL))
    # the forearm and hand red, below a jagged edge just under the elbow
    def red_arm(P):
        Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
        t = (Q - EL) @ d
        across = Q - EL - np.outer(t, d)
        ang = np.arctan2(across[:, 2], across[:, 0] - 0.0)
        edge = 0.01 + 0.016 * tri(ang * 3 / (2 * math.pi))
        return inside(edge - t, 0.0025) * (np.linalg.norm(Q - HAND, axis=1) < 0.16) * (Q[:, 2] < 0.36)
    k.paint(red_arm, RED, on=["arm", "hand"])
    # two long curved black claws hanging from each hand, down and a little in
    for s, bone in ((1, "armR"), (-1, "armL")):
        for j, dx in enumerate((-0.018, 0.02)):
            a = HAND * np.array([s, 1, 1]) + np.array([(dx + 0.004) * s, -0.02, -0.03])
            pts = [a, a + (0.002 * s, -0.012, -0.045), a + (-0.004 * s, -0.004, -0.09), a + (-0.012 * s, 0.014, -0.118)]
            k.add(f"claw{bone[-1]}{j}", outward(blade(pts, [0.016, 0.013, 0.008, 0.002], (s, 0, 0), 0.55)), CLAW, bone,
                  fuse=False, surface="shell")


def add_head(k):
    def jowl(V):
        V = V.copy()
        low = np.clip(-V[:, 2], 0, 1)
        V[:, 0] *= 1 + 0.06 * low
        return V
    k.add("head", ell(H, HR, deform=jowl), WHITE, "head")
    k.add("cheekfur", ell(H + (0, 0.005, -0.055), (0.125, 0.09, 0.05)), WHITE, "head")
    k.add("muzzle", ell(H + (0, -0.088, -0.03), (0.042, 0.03, 0.03)), WHITE, "head")
    # the left ear white and upright, the right one red and swept out
    a = np.array([-0.06, 0.0, 0.645]); t = np.array([-0.092, 0.012, 0.815])
    k.add("earL", blade([a, a + (t - a) * 0.45, t], [0.05, 0.032, 0.005], (0, 1, 0), 0.5), WHITE, "earL")
    a = np.array([0.075, -0.005, 0.635]); t = np.array([0.25, 0.01, 0.745])
    k.add("earR", blade([a, a + (t - a) * 0.45, t], [0.042, 0.03, 0.004], (0, 1, 0), 0.5), RED, "earR")
    # a white root to the red ear, a jagged line between
    def ear_root(P):
        t = (P - np.array([0.075, -0.005, 0.635])) @ unit(0.175, 0.015, 0.11)
        across = P[:, 1] + P[:, 2] * 0.2
        return inside(t - 0.035 - 0.012 * tri(across * 30), 0.0025)
    k.paint(ear_root, WHITE, on=["earR"])
    # the red zigzag from the red ear's root down round the outside of the right eye to the cheek
    line = [(0.118, 0.672), (0.086, 0.628), (0.114, 0.602), (0.096, 0.57), (0.058, 0.548)]
    def stripe(P):
        x, z = head_coords(P)
        return inside(polyline_dist(x, z, line) - 0.016, 0.0025) * (P[:, 1] < H[1] + 0.05) * (P[:, 0] > 0.02)
    k.paint(stripe, RED, on=["head", "cheekfur", "earR"])
    # the small pink nose and the frowning mouth
    k.decal(H + (0, -0.118, -0.018), unit(0, -1, 0.35), (0.014, 0.01),
            lambda u, v: np.tile(srgb(NOSE), (len(u), 1)), eps=0.004, rings=8, segs=32,
            outline=lambda th: 1.0 if math.sin(th) > 0 else 0.6 + 0.4 * abs(math.cos(th)))
    CM, CW = srgb(MOUTH), srgb(WHITE)
    def frown(u, v):
        c = np.tile(CW, (len(u), 1))
        c[np.abs(v - (0.35 - 0.7 * np.abs(u))) < 0.26] = CM
        c[(np.abs(u) < 0.12) & (v > 0.0)] = CM
        return c
    k.decal(H + (0, -0.112, -0.052), unit(0, -1, -0.1), (0.022, 0.012), frown, eps=0.004, rings=10, segs=40)


def add_tail(k):
    """A big white tail of three broad points, rising out behind to the right."""
    root = np.array([0.02, 0.12, 0.22])
    d = unit(0.5, 0.75, 0.42)
    k.add("tailroot", tube([root - d * 0.03, root + d * 0.08, root + d * 0.16], [0.06, 0.07, 0.06]), WHITE, "tail")
    for j, (up, L, w) in enumerate(((0.3, 0.26, 0.075), (0.02, 0.3, 0.085), (-0.24, 0.21, 0.07))):
        dj = unit(*(d + np.array([0, 0, up]) + np.array([0.12, 0, 0]) * (j - 1)))
        a = root + d * 0.12
        k.add(f"tailpoint{j}", blade([a, a + dj * L * 0.45, a + dj * L], [w, w * 0.7, 0.006],
                                     np.cross(dj, (0, 0, 1)), 0.6), WHITE, "tail")


def lay_eyes(k):
    CW, CI, CP, CR = srgb(WHITE), srgb(INK), srgb(PUPIL), srgb(IRIS)
    def scowl(u):
        return 0.1 + 0.45 * u
    def eye(u, v):
        c = np.tile(CW, (len(u), 1))
        cut = scowl(u)
        open_ = (v < cut) & (u * u + v * v < 0.95)
        c[open_] = CW
        iris = open_ & (((u + 0.12) / 0.46) ** 2 + ((v + 0.12) / 0.6) ** 2 < 1)
        c[iris] = CR
        c[iris & (((u + 0.14) / 0.2) ** 2 + ((v + 0.12) / 0.32) ** 2 < 1)] = CP
        c[(np.abs(v - cut) < 0.16) & (u * u + v * v < 1.0)] = CI
        c[open_ & (u * u + v * v > 0.8 ** 2)] = CI
        return c
    de = unit(0.48, -0.86, 0.12)
    E = H + np.array([0.05, -0.09, 0.012])
    def eyes(state):
        if state == "Fierce":
            look = lambda u, v: eye(u, v - 0.25 * u + 0.1)
        else:
            look = eye_look(state, eye, CW, CI, line=0.18)
        k.decal(E, de, (0.03, 0.027), look, eps=0.005, rings=20, segs=56, mirror=True)
    return eyes


def zangoose(src, out):
    k = Kit(src, coat="fur")
    add_body(k)
    add_legs(k)
    add_arms(k)
    add_head(k)
    add_tail(k)
    # the red bolt across the chest
    bolt = [(-0.15, 0.31), (-0.05, 0.455), (-0.012, 0.37), (0.06, 0.485)]
    k.paint(lambda P: inside(polyline_dist(P[:, 0], P[:, 2], bolt) - 0.02, 0.0025) * (P[:, 1] < -0.05)
            * (np.abs(P[:, 0]) < 0.17), RED, on=["belly", "chest"])
    k.build(voxel=0.0058, band=0.005)
    k.eye_states(lay_eyes(k))
    k.export(out)


if __name__ == "__main__":
    zangoose(*args())
