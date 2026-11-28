# Zimbrel (No. 10061, Carpathia, Normal), sculpted from nothing on the skeleton and clips of the game's own export:
#   blender --background --factory-startup --python sculpts/zimbrel.py -- <the export's Zimbrel.glb> <out.glb>
# From the picture (docs/art/reference/gemini/carpathia/10061-Zimbrel.jpg) and the brief (CP-dex-2-mountains.md, 61):
# a stocky woolly bison calf in pale tawny brown; a big round head carried low and forward; a short hump of wool over
# the shoulders, woollier and paler than the rest; a curly forelock over the crown; two little dark stub horns in the
# curls; ears out to the sides under them, pink-tan within; big round black eyes with a white glint; a small pale
# star on the forehead; a broad brown muzzle, two nostrils, a gentle mouth; shaggy cheeks and throat; short thick
# legs with a fringe over dark hooves; a short tail with a tuft; the belly darker tan.
#
# Written to be grown from (zimbrunt.py imports it with sibling("zimbrel")): the palette, the leg, the ear, the wool
# puffs, the eye and the star are the functions below; the calls that build Zimbrel are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, slab, lathe, lumpy, outward, srgb, inside, unit, polygon, eye_look, args)

TAWNY, WOOL, DEEP, MUZ = "#E1B978", "#EFD39C", "#C4955A", "#B5866A"
HORN, HOOF, LINING, STAR = "#58504C", "#584842", "#D8A088", "#F8EED6"
NOSTRIL = "#4A3430"
INK, IRIS, GLINT = srgb("#110D0C"), srgb("#3A2A22"), np.array([1.0, 1.0, 1.0])


def on_ell(c, r, d):
    d = unit(*d)
    t = 1.0 / math.sqrt(((d / np.array(r)) ** 2).sum())
    return np.array(c, float) + d * t


def leg(k, name, pts, radii, bone, color, hoof=HOOF, fringe=None, fringe_r=1.18):
    """A short thick leg to a dark hoof, a ring of shaggy fringe falling over the hoof's top."""
    k.add(name, tube(pts, radii), color, bone, mirror=True)
    x, y = pts[-1][0], pts[-1][1]
    hr = radii[-1] * 1.05
    h = hr * 0.85
    k.add(name + "hoof", lathe((x, y - 0.1 * hr, 0.0), (0, 0, 1),
                               [(0, 0), (hr, 0), (hr * 1.04, h * 0.3), (hr * 0.9, h), (0, h)]),
          hoof, bone, fuse=False, mirror=True, surface="shell")
    if fringe:
        r = radii[-1] * fringe_r
        k.add(name + "fringe", ell((x, y, h + r * 0.45), (r, r, r * 0.55), deform=lumpy(7, 3, 0.18), sub=3), fringe,
              bone, mirror=True)


def ear(k, root, d, n, length, half, outer, inner, thick, bone="ear{s}"):
    """An ear: a soft oval plate from root along d, its face to n, a lining laid on its face."""
    d = unit(*d); n = np.array(n, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    ez = np.cross(n, d)
    L, W = length, half
    ts = np.linspace(0, 1, 12)
    prof = [W * (0.6 + 0.4 * math.sin(math.pi * min(t / 0.45, 1) * 0.5)) * (1 - max(t - 0.45, 0) / 0.55) ** 0.6
            for t in ts]
    out_ = [(L * t, -w) for t, w in zip(ts, prof)][:-1] + [(L, 0)] + [(L * t, w) for t, w in zip(ts, prof)][::-1][1:]
    k.add("ear", slab(out_, root, d, ez, thick), outer, bone, fuse=False, mirror=True, surface="fur")
    lin = [(0.15 * L + 0.7 * a, 0.6 * b) for a, b in out_]
    k.add("earin", slab(lin, np.array(root) + n * (thick * 0.5 + 0.002), d, ez, thick * 0.4), inner, bone,
          fuse=False, mirror=True, surface="skin")


def puffs(k, name, c, r, n, size, colors, bone, below=-0.2, front=1.0, seed=0, jitter=0.15, sink=0.93):
    """Wool: fused round puffs spread over the upper part of an ellipsoid (c, r), on a golden spiral."""
    rs = np.random.RandomState(seed)
    c = np.array(c, float); r = np.array(r, float)
    for i in range(n):
        z = 1 - 2 * (i + 0.5) / n
        if z < below:
            continue
        a = i * math.pi * (3 - math.sqrt(5))
        q = math.sqrt(1 - z * z)
        p = np.array([q * math.cos(a), q * math.sin(a), z])
        if p[1] < -front:
            continue
        s = size * (1 + jitter * rs.uniform(-1, 1))
        k.add(f"{name}{i}", ell(c + p * r * sink, (s, s, s * 0.85), sub=3), colors[i % len(colors)], bone)


STAR5 = polygon([(math.cos(math.pi / 2 + i * math.pi / 5) * (1 if i % 2 == 0 else 0.45),
                  math.sin(math.pi / 2 + i * math.pi / 5) * (1 if i % 2 == 0 else 0.45)) for i in range(10)])


def round_eye(u, v):
    """The open eye: black, a dark brown rim of iris, a big white glint up and out and a small one low."""
    c = np.tile(IRIS, (len(u), 1))
    c[np.hypot(u, v) < 0.8] = INK
    c[((u - 0.2) / 0.3) ** 2 + ((v - 0.3) / 0.27) ** 2 < 1] = GLINT
    c[np.hypot(u + 0.32, v + 0.4) < 0.12] = GLINT
    return c


def eyes_pair(k, E, de, half, look_open, lid, line=0.15, outline=None, eps=0.006):
    m = np.array([-1, 1, 1.0])
    def lay(state):
        lk = eye_look(state, look_open, lid, INK, line=line)
        k.decal(E, de, half, lk, eps=eps, rings=20, segs=56, outline=outline)
        k.decal(E * m, de * m, half, lambda u, v: lk(-u, v), eps=eps, rings=20, segs=56,
                outline=(lambda th: outline(math.pi - th)) if outline else None)
    return lay


def zimbrel(src, out):
    k = Kit(src, coat="fur")

    # --- the trunk: a deep barrel low over short legs, a round rump, shoulders under the hump -------------------
    k.add("barrel", ell((0, 0.05, 0.318), (0.2, 0.3, 0.185)), TAWNY, "body")
    k.add("rump", ell((0, 0.22, 0.34), (0.185, 0.15, 0.17)), TAWNY, "body")
    k.add("chestmass", ell((0, -0.19, 0.32), (0.198, 0.14, 0.18)), TAWNY, "body")
    # the short hump of wool over the shoulders, paler and curly
    HC, HR_ = np.array([0, -0.1, 0.42]), np.array([0.212, 0.2, 0.19])
    k.add("hump", ell(HC, HR_, deform=lumpy(7, 5, 0.06)), WOOL, "body")
    puffs(k, "humppuff", HC, HR_, 40, 0.05, [WOOL, WOOL, TAWNY], "body", below=0.05, front=0.75, seed=61, sink=0.88)

    # --- legs: fat shoulders and thighs fused into the flanks, short thick shins, a fringe over the hoof ---------
    k.add("shoulder", ell((0.13, -0.19, 0.25), (0.078, 0.092, 0.115)), TAWNY, "leg{s}", mirror=True)
    k.add("thigh", ell((0.132, 0.24, 0.26), (0.082, 0.102, 0.122)), TAWNY, "hind{s}", mirror=True)
    leg(k, "foreleg", [(0.15, -0.19, 0.25), (0.152, -0.2, 0.13), (0.155, -0.2, 0.05)], [0.072, 0.059, 0.055],
        "leg{s}", TAWNY, fringe=WOOL)
    leg(k, "hindleg", [(0.15, 0.25, 0.25), (0.152, 0.24, 0.13), (0.155, 0.25, 0.05)], [0.075, 0.06, 0.055],
        "hind{s}", TAWNY, fringe=WOOL)
    # the belly and the backs of the legs a darker tan
    k.paint(lambda P: inside(P[:, 2] - 0.2, 0.012) * inside(np.abs(P[:, 0]) - 0.13, 0.03), DEEP,
            on=["barrel", "rump", "chestmass"])

    # --- the tail: short, a tuft at its end -----------------------------------------------------------------
    k.add("tail", tube([(0, 0.36, 0.4), (0.008, 0.41, 0.34), (0.02, 0.43, 0.28)], [0.026, 0.022, 0.02]), TAWNY,
          "tail")
    k.add("tailtuft", ell((0.024, 0.438, 0.238), (0.042, 0.04, 0.062), deform=lumpy(5, 3, 0.2), sub=3), WOOL, "tail")

    # --- the head: big and round, low in front; a broad brown muzzle; shaggy cheeks and throat ---------------
    H, HR = np.array([0, -0.4, 0.41]), np.array([0.15, 0.145, 0.15])
    k.add("head", ell(H, HR), TAWNY, "head")
    k.add("cheeks", ell((0, -0.37, 0.33), (0.165, 0.11, 0.11), deform=lumpy(6, 3, 0.1)), TAWNY, "head")
    k.add("throat", ell((0, -0.33, 0.25), (0.13, 0.1, 0.1), deform=lumpy(6, 3, 0.14)), TAWNY, "head")
    MZ = np.array([0, -0.532, 0.335])
    k.add("muzzle", ell(MZ, (0.102, 0.078, 0.075)), MUZ, "head")
    # the curly forelock over the crown
    for i, (x, y, z, r) in enumerate([(0, -0.43, 0.56, 0.06), (0.07, -0.43, 0.545, 0.052), (-0.07, -0.43, 0.545, 0.052),
                                      (0.035, -0.47, 0.525, 0.046), (-0.035, -0.47, 0.525, 0.046),
                                      (0.11, -0.4, 0.52, 0.045), (-0.11, -0.4, 0.52, 0.045),
                                      (0, -0.37, 0.57, 0.055), (0.06, -0.36, 0.56, 0.05), (-0.06, -0.36, 0.56, 0.05)]):
        k.add(f"curl{i}", ell((x, y, z), (r, r, r * 0.85), deform=lumpy(4, 3, 0.12), sub=3), WOOL, "head")

    # --- two dark stub horns in the curls, ears out to the sides under them ----------------------------------
    k.add("horn", outward(tube([(0.1, -0.375, 0.52), (0.135, -0.38, 0.565), (0.148, -0.39, 0.605)],
                               [0.026, 0.019, 0.009], seg=14, per=6)), HORN, "head", fuse=False, mirror=True,
          surface="shell")
    ear(k, (0.13, -0.39, 0.445), (1, 0.12, -0.12), unit(0.1, -0.9, 0.35), 0.1, 0.042, TAWNY, LINING, 0.026)

    # --- the face: nostrils, a gentle mouth, the star ---------------------------------------------------------
    for s in (1, -1):
        k.decal((0.036 * s, -0.606, 0.345), unit(0.25 * s, -1, 0.1), (0.016, 0.011), NOSTRIL, eps=0.003, rings=6,
                segs=28)
    k.decal((0, -0.59, 0.288), unit(0, -0.8, -0.6), (0.045, 0.006), NOSTRIL, eps=0.003, rings=3, segs=28)
    sd = unit(0, -0.95, 0.3)
    k.decal(on_ell(H, HR, sd), sd, (0.034, 0.034), STAR, eps=0.004, outline=STAR5, rings=10, segs=80)

    de = unit(0.55, -0.8, 0.22)
    E = on_ell(H, HR, de)
    k.build(voxel=0.0068, smooth=4, band=0.008)
    k.eye_states(eyes_pair(k, E, de, (0.03, 0.033), round_eye, srgb(TAWNY)))
    k.export(out)


if __name__ == "__main__":
    zimbrel(*args())
