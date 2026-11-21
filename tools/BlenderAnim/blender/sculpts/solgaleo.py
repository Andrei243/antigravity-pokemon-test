# Solgaleo sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/solgaleo.py -- <the export's Solgaleo.glb> <out.glb>
#
# Grown from Cosmog's line (sibling("cosmog")): its gold and its on_ell.
# From the references: a great white lion on thick white legs. Its largest mass is the mane, a sun of white flames
# round the head and down over the chest, each flame flicked back with a slash of orange on it, and eight gold rays
# radiating out of it from round the face, laid back. The face stands forward of the mane: a domed blue visor over the
# brow in which the eyes are pale slanting slits, a white muzzle under it, the mouth open dark with orange inside. A
# black patch on each flank behind the shoulder; each leg banded orange above a black paw with three gold toe stripes.
# A thin white tail rises behind in an S and ends in a black oval rimmed gold.
# The skeleton: body (0, 0.02, 0.44), head (0, -0.16, 0.5), legs (+-0.08, -0.17, 0.3), hind legs (+-0.08, 0.2, 0.3),
# tail (0, 0.23, 0.43). The export is 0.79 tall.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, ring_band, axes, outward, srgb, inside, smoothstep, unit, lumpy,
                       eye_look, polygon, args, sibling)

cg = sibling("cosmog")

WHITE, SHADE = "#F4F4F2", "#D8DCE2"
DARK = "#2E2E36"
ORANGE, GOLD = "#F0601E", "#F6C72A"
BLUE, BLUELIGHT, EYEW, EYEINK = "#1E3FB8", "#5A86F0", "#F2FAFF", "#0E1A50"
MOUTH, MOUTHIN = "#1A1418", "#E85A1C"

LEG_X = 0.1
FORE_Y, HIND_Y = -0.17, 0.2
H = np.array([0.0, -0.33, 0.65])             # the head, carried high in front of the mane
HR = np.array([0.095, 0.098, 0.092])
M = np.array([0.0, -0.24, 0.6])              # the mane's middle
FACE_N = unit(0, -1, 0.22)
VOXEL = 0.0072


def trunk(k):
    k.add("chest", ell((0, -0.12, 0.45), (0.135, 0.13, 0.16)), WHITE, "body")
    k.add("middle", ell((0, 0.04, 0.44), (0.118, 0.15, 0.13)), WHITE, "body")
    k.add("rump", ell((0, 0.2, 0.45), (0.122, 0.1, 0.135)), WHITE, "body")


def legs(k):
    """Thick white legs: shoulders and thighs fused over the upper halves, black paws, an orange band above each."""
    for nm, bone, y, top in (("f", "leg{s}", FORE_Y, (0.095, FORE_Y, 0.38, 0.082, 0.095, 0.15)),
                             ("h", "hind{s}", HIND_Y, (0.095, HIND_Y + 0.01, 0.38, 0.088, 0.12, 0.16))):
        k.add(f"{nm}top", ell(top[:3], top[3:]), WHITE, bone, mirror=True)
        k.add(f"{nm}leg", tube([(LEG_X, y, 0.33), (LEG_X + 0.003, y - 0.01, 0.18), (LEG_X + 0.005, y - 0.014, 0.07)],
                               [0.07, 0.062, 0.057]), WHITE, bone, mirror=True)
        k.add(f"{nm}paw", ell((LEG_X + 0.005, y - 0.03, 0.036), (0.068, 0.082, 0.04)), DARK, bone, mirror=True)
        k.add(f"{nm}band", ring_band((LEG_X + 0.005, y - 0.013, 0.105), (0, 0, 1), 0.057, 0.03, thick=0.008), ORANGE,
              bone, fuse=False, mirror=True)
    # the paws black, crisp below their top
    k.paint(lambda P: inside(P[:, 2] - 0.066, 0.003) * (np.abs(P[:, 0]) > 0.03), DARK)


def toe_stripes(k):
    for y in (FORE_Y, HIND_Y):
        for s in (1, -1):
            for dx in (-0.026, 0.0, 0.026):
                c = np.array([(LEG_X + 0.005 + dx) * s, y - 0.03 - 0.078, 0.036])
                k.decal(c, unit(dx * 6 * s, -1, 0.25), (0.0065, 0.019), GOLD, eps=0.003, rings=6, segs=28)


def flank(k):
    """The black patch on each flank behind the shoulder, and the belly between."""
    def patch(P):
        e = ((P[:, 1] - 0.0) / 0.055) ** 2 + ((P[:, 2] - 0.33) / 0.15) ** 2
        return inside(e - 1, 0.04) * (P[:, 2] > 0.3)
    k.paint(patch, DARK, on=["middle", "chest", "rump"])


def tail(k):
    pts = [(0, 0.27, 0.47), (0.0, 0.37, 0.44), (0.025, 0.44, 0.49), (0.05, 0.47, 0.58), (0.07, 0.46, 0.66)]
    k.add("tail", tube(pts, [0.024, 0.019, 0.018, 0.018, 0.017]), WHITE, "tail")
    end = np.array(pts[-1]) + (0.006, 0.0, 0.045)
    n = unit(0.82, -0.57, 0.0)                 # the oval's face turned back and out to +X
    a, b = axes(n)
    def disc(c, ra, rb, th):
        V, F = ell((0, 0, 0), (ra, rb, th), sub=4)
        V = V[:, 0:1] * a + V[:, 1:2] * b + V[:, 2:3] * n + c
        return outward((V, F))
    # a and b are square to n; set the oval upright: b's turn into up
    up = np.array([0, 0, 1.0]); a = np.cross(up, n); a /= np.linalg.norm(a); b = np.cross(n, a)
    k.add("tailrim", disc(end, 0.04, 0.054, 0.014), GOLD, "tail", fuse=False, surface="shell")
    k.add("tailtip", disc(end, 0.026, 0.038, 0.02), DARK, "tail", fuse=False)


def mane(k):
    """The white flames round the head and over the chest, flicked back, an orange slash on each."""
    k.add("manecore", ell(M + (0, 0.04, 0), (0.2, 0.14, 0.21), deform=lumpy(6, 4, 0.05)), WHITE, "head")
    k.add("bib", ell((0, -0.25, 0.42), (0.13, 0.1, 0.14), deform=lumpy(5, 3, 0.05)), WHITE, "body")
    k.add("neck", ell((0, -0.24, 0.55), (0.1, 0.11, 0.12)), WHITE, "head")
    n_l = 14
    for i in range(n_l):
        a = math.radians(i * 360 / n_l + 6)
        d = np.array([math.cos(a), 0, math.sin(a) * 0.9])
        L = 0.29 + 0.07 * abs(math.cos(a))
        root = M + d * 0.12 + (0, 0.03, 0)
        mid = M + d * (L * 0.72) + (0, 0.07, 0)
        tip = M + d * L + (0, 0.14, 0)
        side = np.array([-d[2], 0, d[0]]); side /= np.linalg.norm(side)
        k.add(f"lobe{i:02d}", blade([root, mid, tip], [0.1, 0.066, 0.006], side, 0.45), WHITE,
              "body" if d[2] < -0.5 else "head")
        if i % 2 == 0:
            r0 = L * 0.62
            k.paint(lambda P, d=d, r0=r0: inside(np.abs(np.linalg.norm((P - M)[:, [0, 2]], axis=1) - r0) - 0.03, 0.004)
                    * inside(np.abs((P - M)[:, [0, 2]] @ np.array([-d[2], d[0]]) / np.linalg.norm(d[[0, 2]])) - 0.03, 0.004),
                    ORANGE, on=[f"lobe{i:02d}"])
    # flames down over the chest
    for i, x in enumerate((-0.09, 0.0, 0.09)):
        root = np.array([x, -0.3, 0.42])
        tip = np.array([x * 1.6, -0.33, 0.27])
        k.add(f"lobebib{i}", blade([root, (root + tip) / 2 + (0, -0.01, 0), tip], [0.06, 0.04, 0.004], (1, 0, 0), 0.6),
              WHITE, "body")


def rays(k):
    """Eight gold rays radiating out of the mane from round the face, laid back: solid kites."""
    c = H + (0, -0.03, 0.01)
    for i in range(8):
        a = math.radians(i * 45 + 22.5)
        d = np.array([math.cos(a), 0, math.sin(a)])
        L = 0.3 if i % 2 == 0 else 0.23
        side = np.array([-d[2], 0, d[0]])
        root = c + d * 0.075
        tip = c + d * L + (0, 0.12, 0)
        mid = root + (tip - root) * 0.3
        n = np.array([0, -1.0, 0])
        pts = [root + side * 0.02 + n * 0.012, root - side * 0.02 + n * 0.012, root + side * 0.02 - n * 0.012,
               root - side * 0.02 - n * 0.012, mid + side * 0.046 + n * 0.014, mid - side * 0.046 + n * 0.014,
               mid + side * 0.046 - n * 0.01, mid - side * 0.046 - n * 0.01, tip]
        k.add(f"ray{i}", hull(pts), GOLD, "head", fuse=False, surface="shell")


def head(k):
    k.add("head", ell(H, HR), WHITE, "head")
    k.add("muzzle", ell(H + (0, -0.068, -0.052), (0.066, 0.058, 0.05)), WHITE, "head")
    k.add("chin", ell(H + (0, -0.055, -0.095), (0.048, 0.046, 0.03)), WHITE, "head")


def visor(k):
    """The domed blue visor over the brow: a shield, its point up between the eyes' tops."""
    p, n = cg.on_ell(H, HR, (0, -1, 0.42))
    cb, cl = srgb(BLUE), srgb(BLUELIGHT)
    def look(u, v):
        c = np.tile(cb, (len(u), 1))
        c[np.hypot(u + 0.35, v - 0.35) < 0.22] = cl
        return c
    out = polygon([(1.0, 0.15), (0.55, 0.55), (0.0, 1.0), (-0.55, 0.55), (-1.0, 0.15), (-0.7, -0.6), (0.0, -0.85),
                   (0.7, -0.6)])
    k.decal(p, n, (0.084, 0.06), look, eps=0.0025, outline=out, rings=14, segs=64)


def mouth(k):
    cm, ci = srgb(MOUTH), srgb(MOUTHIN)
    def look(u, v):
        c = np.tile(cm, (len(u), 1))
        c[(u * u + ((v + 0.15) / 0.75) ** 2) < 0.45] = ci
        return c
    out = polygon([(1.0, 0.6), (0.0, 0.35), (-1.0, 0.6), (-0.8, -0.4), (0.0, -1.0), (0.8, -0.4)])
    k.decal(H + (0, -0.12, -0.07), unit(0, -1, -0.35), (0.04, 0.035), look, eps=0.0025, outline=out, rings=12,
            segs=64)


CB, CW, CI = srgb(BLUE), srgb(EYEW), srgb(EYEINK)
SLIT = polygon([(1.0, 0.1), (0.3, 0.55), (-1.0, 0.0), (0.3, -0.45)])


def eye_open(u, v):
    c = np.tile(CW, (len(u), 1))
    c[np.hypot(u + 0.2, v) < 0.28] = CI
    return c


def eyes(k, state):
    for s in (1, -1):
        p, n = cg.on_ell(H, HR, unit(0.42 * s, -1, 0.32))
        look = eye_look(state, eye_open, CB, CW, line=0.2)
        # the slit slants down to the nose: up turned so the inner corner is low
        up = unit(-0.35 * s, 0, 1)
        if state in ("Open", "Fierce"):
            k.decal(p, n, (0.028, 0.0155), look if s > 0 else (lambda a, b, f=look: f(-a, b)), up=up, eps=0.0045,
                    outline=polygon([(s * a, b) for a, b in [(1.0, 0.1), (0.3, 0.55), (-1.0, 0.0), (0.3, -0.45)]]),
                    rings=20, segs=56)
        else:
            k.decal(p, n, (0.028, 0.017), look if s > 0 else (lambda a, b, f=look: f(-a, b)), up=up, eps=0.0045,
                    rings=20, segs=56)


def solgaleo(src, out):
    k = Kit(src, coat="fur")
    trunk(k)
    legs(k)
    flank(k)
    tail(k)
    mane(k)
    head(k)
    rays(k)
    visor(k)
    mouth(k)
    toe_stripes(k)
    k.build(voxel=VOXEL, smooth=4, band=0.004, wband=0.06)
    k.eye_states(lambda st: eyes(k, st))
    k.export(out)


if __name__ == "__main__":
    solgaleo(*args())
