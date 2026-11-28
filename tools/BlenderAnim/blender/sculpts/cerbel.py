# Cerbel (No. 10059, Carpathia, Grass), sculpted from nothing on the skeleton and clips of the game's own export:
#   blender --background --factory-startup --python sculpts/cerbel.py -- <the export's Cerbel.glb> <out.glb>
# From the picture (docs/art/reference/gemini/carpathia/10059-Cerbel.jpg) and the brief (CP-dex-2-mountains.md, 59):
# a red deer fawn with a reddish-brown coat; cream under the belly, up the front of the chest and throat to the chin
# and round the nose; white spots over the back and the upper flanks; big dark round eyes, each in a pale patch, with
# a white glint; a small black nose and a little smile; big soft ears out and up, a pale pink lining; two green shoots
# with tiny leaves and a bud at each tip between the ears, where its antlers will grow; a green vine with leaves
# round the base of the neck; a short tail flicked up, cream beneath; long legs (here shortened and thickened), the
# hind leg with its hock, dark at the fetlock, dark hooves.
#
# Written to be grown from (cerbanor.py imports it with sibling("cerbel")): the palette, the eyes, the leg, the ear,
# the spots, a leaf and the vine are the functions below; the calls that build Cerbel are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, slab, lathe, leaf, outward, srgb, inside, smoothstep, unit, eye_look,
                       args)

RED, CREAM, LINING, PATCH = "#BE6238", "#F3E2C4", "#F2C9BA", "#EBCDAE"
HOOF, FETLOCK, NOSE = "#463830", "#5E5136", "#2A211E"
GREEN, LEAFC, BUDC = "#6EAE48", "#5E9E3C", "#9ACB62"
SPOT = "#FBF4E6"
INK, IRIS, GLINT = srgb("#120E0E"), srgb("#3C2E2A"), np.array([1.0, 1.0, 1.0])


def on_ell(c, r, d):
    """The point of an ellipsoid's surface in the direction d from its middle."""
    d = unit(*d)
    t = 1.0 / math.sqrt(((d / np.array(r)) ** 2).sum())
    return np.array(c, float) + d * t


def ell_normal(c, r, p):
    g = (np.array(p) - np.array(c)) / np.array(r) ** 2
    return g / np.linalg.norm(g)


def leg(k, name, pts, radii, bone, color, hoof=HOOF, fetlock=FETLOCK, band=(0.024, 0.058)):
    """A leg: a tapering tube through its joints to a dark hoof; a dark fetlock band above the hoof."""
    k.add(name, tube(pts, radii), color, bone, mirror=True)
    x, y = pts[-1][0], pts[-1][1]
    hr = radii[-1] * 1.12
    h = hr * 1.3
    k.add(name + "hoof", lathe((x, y - 0.002 * hr / 0.02, 0.0), (0, 0, 1),
                               [(0, 0), (hr, 0), (hr * 1.04, h * 0.25), (hr * 0.86, h), (0, h)]),
          hoof, bone, fuse=False, mirror=True, surface="shell")
    lo, hi = band
    k.paint(lambda P: inside(P[:, 2] - hi, 0.004) * inside(lo - P[:, 2], 0.002), fetlock, on=[name])


def ear(k, root, d, n, length, half, outer, inner, thick, bone="ear{s}"):
    """An ear: a flat leaf-shaped plate from root along d, its face to n, a lining laid on its face."""
    d = unit(*d); n = np.array(n, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    ez = np.cross(n, d)
    L, W = length, half
    # a soft oval with a rounded point: wide in its lower half, narrowing to the tip
    ts = np.linspace(0, 1, 14)
    prof = [W * (0.55 + 0.45 * math.sin(math.pi * min(t / 0.4, 1) * 0.5)) * (1 - max(t - 0.4, 0) / 0.6) ** 0.7
            for t in ts]
    out_ = [(L * t, -w) for t, w in zip(ts, prof)][:-1] + [(L, 0)] + [(L * t, w) for t, w in zip(ts, prof)][::-1][1:]
    k.add("ear", slab(out_, root, d, ez, thick), outer, bone, fuse=False, mirror=True, surface="fur")
    lin = [(0.14 * L + 0.72 * a, 0.62 * b) for a, b in out_]
    k.add("earin", slab(lin, np.array(root) + n * (thick * 0.5 + 0.002), d, ez, thick * 0.4), inner, bone,
          fuse=False, mirror=True, surface="skin")


def spots(k, c, r, dirs, color=SPOT, eps=0.003):
    """White spots over a back: small oval patches on the ellipsoid (c, r) in the directions dirs, both sides."""
    for d, a, b in dirs:
        p = on_ell(c, r, d)
        n = ell_normal(c, r, p)
        k.decal(p, n, (a, b), color, up=(0, 1, 0), eps=eps, rings=6, segs=28, mirror=d[0] != 0)


def a_leaf(k, name, base, d, length, width, bone, color=LEAFC, thick=0.012):
    """A small loose leaf from base along d."""
    k.add(name, outward(leaf(base, d, length, width, thick=thick)), color, bone, fuse=False, surface="leaf")


def vine(k, c, axis, radius, rad, bone, leaves, color=GREEN, leafc=LEAFC, leaf_size=(0.03, 0.012)):
    """A green vine wound round a neck (a closed loose tube, its seam behind) with leaves along it."""
    axis = unit(*axis)
    a = np.array([1.0, 0, 0]); b = np.cross(axis, a); b /= np.linalg.norm(b)
    if b[1] > 0:
        b = -b
    c = np.array(c, float)
    # from behind round and back, a little past, so the ends overlap at the back
    ts = np.linspace(-0.5 * math.pi - 0.25, 1.5 * math.pi + 0.25, 28)
    pts = [c + radius * (math.cos(t) * a + math.sin(t) * b) for t in ts]
    k.add("vine", outward(tube(pts, [rad] * len(pts), seg=12, per=4)), color, bone, fuse=False, surface="leaf")
    for i, (t, out_, down) in enumerate(leaves):
        p = c + radius * (math.cos(t) * a + math.sin(t) * b)
        o = p - c; o /= np.linalg.norm(o)
        a_leaf(k, f"vineleaf{i}", p, o * out_ + np.array([0, 0, -down]), leaf_size[0], leaf_size[1], bone, leafc)


def dark_eye(u, v):
    """The open eye: dark brown-grey, a big black pupil, a white glint up and out and a small one low."""
    c = np.tile(INK, (len(u), 1))
    c[np.hypot(u, v) < 0.86] = IRIS
    c[np.hypot(u + 0.04, v + 0.04) < 0.56] = INK
    c[((u - 0.18) / 0.27) ** 2 + ((v - 0.32) / 0.25) ** 2 < 1] = GLINT
    c[np.hypot(u + 0.3, v + 0.42) < 0.12] = GLINT
    return c


def eyes_pair(k, E, de, half, look_open, lid, line=0.15, outline=None, eps=0.006):
    """Lays a state's eyes: the right as it is, the left with its picture (and outline) turned over."""
    m = np.array([-1, 1, 1.0])
    def lay(state):
        lk = eye_look(state, look_open, lid, INK, line=line)
        k.decal(E, de, half, lk, eps=eps, rings=20, segs=56, outline=outline)
        k.decal(E * m, de * m, half, lambda u, v: lk(-u, v), eps=eps, rings=20, segs=56,
                outline=(lambda th: outline(math.pi - th)) if outline else None)
    return lay


def cerbel(src, out):
    k = Kit(src, coat="fur")

    # --- the trunk: chest, barrel and rump, lowered for a short leg showing ------------------------------------
    BC, BR = np.array([0, 0.005, 0.305]), np.array([0.08, 0.165, 0.094])
    k.add("barrel", ell(BC, BR), RED, "body")
    k.add("chest", ell((0, -0.092, 0.322), (0.074, 0.078, 0.096)), RED, "body")
    k.add("rump", ell((0, 0.1, 0.322), (0.08, 0.08, 0.092)), RED, "body")

    # --- legs: shoulders and thighs fused over the top, the hind leg with its hock, dark hooves ----------------
    k.add("shoulder", ell((0.046, -0.088, 0.262), (0.036, 0.05, 0.078)), RED, "leg{s}", mirror=True)
    k.add("thigh", ell((0.048, 0.098, 0.268), (0.043, 0.068, 0.09)), RED, "hind{s}", mirror=True)
    leg(k, "fore", [(0.043, -0.09, 0.24), (0.043, -0.093, 0.15), (0.043, -0.088, 0.075), (0.043, -0.092, 0.024)],
        [0.027, 0.021, 0.018, 0.017], "leg{s}", RED)
    leg(k, "hind", [(0.045, 0.1, 0.24), (0.045, 0.088, 0.19), (0.045, 0.124, 0.13), (0.045, 0.112, 0.024)],
        [0.03, 0.025, 0.02, 0.017], "hind{s}", RED)

    # --- the neck up to a big head; a muzzle forward and down, a small black nose ------------------------------
    k.add("neck", tube([(0, -0.08, 0.35), (0, -0.122, 0.44), (0, -0.142, 0.5)], [0.055, 0.047, 0.045]), RED, "head")
    H, HR = np.array([0, -0.155, 0.535]), np.array([0.064, 0.066, 0.068])
    k.add("head", ell(H, HR), RED, "head")
    MC = np.array([0, -0.218, 0.507])
    k.add("muzzle", ell(MC, (0.035, 0.052, 0.034), rot=(12, 0, 0)), RED, "head")
    k.add("nose", ell((0, -0.268, 0.511), (0.015, 0.011, 0.012)), NOSE, "head", fuse=False, surface="skin")

    # cream round the nose and under the muzzle, down the throat and the chest, under the belly
    k.paint(lambda P: inside(np.linalg.norm(P - MC, axis=1) - 0.075, 0.004) * inside(P[:, 2] - 0.508, 0.003)
            * inside(P[:, 1] + 0.17, 0.004), CREAM)
    k.paint(lambda P: inside(np.linalg.norm(P - (0, -0.258, 0.512), axis=1) - 0.022, 0.003), CREAM)
    def throat(P):
        yc = -0.08 - 0.42 * (P[:, 2] - 0.35)
        return (inside(P[:, 1] - (yc - 0.026), 0.004) * inside(np.abs(P[:, 0]) - 0.03, 0.006)
                * inside(P[:, 2] - 0.49, 0.004) * inside(0.25 - P[:, 2], 0.004))
    k.paint(throat, CREAM)
    k.paint(lambda P: inside(P[:, 2] - 0.24, 0.004) * inside(P[:, 1] - 0.12, 0.004) * inside(-0.12 - P[:, 1], 0.004)
            * inside(0.17 - P[:, 2], 0.004) * inside(np.abs(P[:, 0]) - 0.04, 0.006), CREAM,
            on=["barrel", "chest", "rump"])

    # --- the tail: short, flicked up and back with a round end, cream beneath ----------------------------------
    k.add("tail", blade([(0, 0.13, 0.37), (0.004, 0.163, 0.402), (0.01, 0.19, 0.422)],
                        [0.027, 0.025, 0.019], (1, 0, 0), 0.72), RED, "tail")
    k.paint(lambda P: inside(0.004 - (P[:, 1] - (0.13 + 0.85 * (P[:, 2] - 0.37))), 0.003), CREAM, on=["tail"])

    # --- the spots: white over the back and the upper flanks ---------------------------------------------------
    spots(k, BC, BR, [((0.45, 0.05, 1), 0.011, 0.009), ((0.55, -0.35, 0.9), 0.009, 0.008),
                      ((0.7, 0.3, 0.75), 0.01, 0.008), ((0.25, 0.4, 1), 0.009, 0.008),
                      ((0.8, -0.1, 0.55), 0.009, 0.007), ((0.6, 0.55, 0.65), 0.008, 0.007),
                      ((0.3, -0.25, 1), 0.008, 0.007)])

    # --- the ears: big and soft, out and up, a pale pink lining ------------------------------------------------
    ear(k, (0.04, -0.15, 0.583), (1, 0.3, 0.85), unit(0.15, -0.9, 0.35), 0.098, 0.034, RED, LINING, 0.02)

    # --- the shoots: two green stems between the ears, tiny leaves, a bud at each tip --------------------------
    for s, nm in ((1, "R"), (-1, "L")):
        m = np.array((s, 1, 1.0))
        pts = [np.array(p) * m for p in ((0.017, -0.158, 0.592), (0.026, -0.156, 0.622), (0.019, -0.152, 0.652),
                                         (0.027, -0.154, 0.68))]
        k.add("shoot" + nm, outward(tube(pts, [0.012, 0.011, 0.0105, 0.01], seg=12, per=5)), GREEN, "head",
              fuse=False, surface="leaf")
        a_leaf(k, "shootleafa" + nm, pts[1], (0.9 * s, -0.4, 0.35), 0.042, 0.017, "head")
        a_leaf(k, "shootleafb" + nm, pts[2] + (0, 0, 0.006), (0.55 * s, -0.7, 0.45), 0.038, 0.015, "head")
        for j, o in enumerate(((0, 0, 0.012), (0.009 * s, -0.004, 0.004), (-0.008 * s, 0.004, 0.005))):
            k.add(f"bud{j}" + nm, ell(pts[3] + np.array(o), (0.011, 0.011, 0.013), sub=3), BUDC, "head",
                  fuse=False, surface="leaf")

    # --- the vine round the base of the neck, leaves at the front and the sides -------------------------------
    vine(k, (0, -0.105, 0.41), (0, -0.42, 1), 0.053, 0.0105, "body",
         [(-0.5 * math.pi + 0.45, 0.6, 0.6), (-0.5 * math.pi - 0.45, 0.6, 0.6), (0.15, 0.7, 0.5),
          (math.pi - 0.15, 0.7, 0.5), (-0.5 * math.pi + 1.1, 0.6, 0.3), (-0.5 * math.pi - 1.1, 0.6, 0.3)])

    # --- the face: a pale patch round each eye, a little smile ------------------------------------------------
    de = unit(0.62, -0.72, 0.2)
    E = on_ell(H, HR, de)
    k.decal(E + np.array([0.002, -0.002, -0.002]), de, (0.031, 0.033), PATCH, eps=0.003, rings=8, segs=40, mirror=True)
    k.decal((0.023, -0.247, 0.49), unit(0.5, -0.75, -0.3), (0.011, 0.003), INK, up=unit(0, 0.35, 1), eps=0.003,
            rings=3, segs=24, mirror=True)

    k.build(voxel=0.0042, smooth=4, band=0.006)
    k.eye_states(eyes_pair(k, E, de, (0.0245, 0.0275), dark_eye, srgb(PATCH)))
    k.export(out)


if __name__ == "__main__":
    cerbel(*args())
