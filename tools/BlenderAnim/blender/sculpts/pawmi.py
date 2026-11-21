# Pawmi sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pawmi.py -- <the export's Pawmi.glb> <out.glb>
#
# The design, from the references: a round orange mouse lying low on its belly, head and body one soft mass, the face
# at its front and low down. Two big broad ears stand up from the top and lean a little out, each with a teal hollow in
# its upper half and a small second lobe in front of its root; a big onion-shaped tuft on the crown, its tip turned up
# and to the model's +X; large brown eyes with a dark pupil and two white glints, set low; yellow cheek patches low at
# the sides; a cream muzzle round a small frowning mouth and a dot of a nose; cream front paws peeping out at the front
# corners; small brown hind feet; a broad flat cream tail standing up behind to the +X side.
# The pictures overrule the brief: Pawmi lies on its belly (no paws walked on), the tuft is the crown's own lock.
# The export's joints: body (0, 0.02, 0.12), head (0, -0.05, 0.15), ears (+-0.06, -0.06, 0.24), legs (+-0.06, -0.06,
# 0.07), hinds (+-0.06, 0.07, 0.07), tail (0, 0.11, 0.14).
#
# The line's frame (Pawmo and Pawmot grow from it through sibling): colours, oriented(), ear(), eye_open(),
# lay_eyes(), pad(), tail_leaf().
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, polygon, args, outward, tri

# --- colours of the line ------------------------------------------------------------------------------------------------
ORANGE, CREAM, YELLOW, TEAL = "#EE8E22", "#F3DFB2", "#F6CE46", "#1E8C78"
BROWN, IRIS, PUPIL, INK = "#8A5432", "#7A4430", "#2A1410", "#1C1216"
MOUTH, TONGUE, NOSE, WHITE = "#8E2E34", "#EE8A92", "#5A3226", "#FFFFFF"


def oriented(c, r, d, face, deform=None, sub=4):
    """An ellipsoid of radii (across, thick, along) laid along d, its thin side toward face: right-handed."""
    d = unit(*d)
    f = np.array(face, float); f = f - d * (f @ d); f /= np.linalg.norm(f)
    side = np.cross(f, d)
    V, F = ell((0, 0, 0), r, deform=deform, sub=sub)
    M = np.stack([side, f, d], axis=1)
    return V @ M.T + np.array(c, float), F


def ear_shape(V):
    """Narrow at the root, broad through the upper half, a rounded point."""
    V = V.copy()
    t = (V[:, 2] + 1) / 2
    w = 0.55 + 0.5 * np.clip(t, 0, 1) ** 0.7 - 0.25 * np.clip(t, 0, 1) ** 6
    V[:, 0] *= w
    return V


def lining_shape(V):
    V = V.copy()
    t = (V[:, 2] + 1) / 2
    V[:, 0] *= 0.35 + 0.75 * np.clip(t, 0, 1) ** 0.8 - 0.2 * np.clip(t, 0, 1) ** 5
    return V


def ear(k, base, d, length, width, thick, bone, mirror=True, lobe=True, lining=0.62, color=ORANGE, front=(0, -1, 0)):
    """A broad flat ear from base along d, fused; its teal hollow and its front lobe loose before its face."""
    d = unit(*d)
    base = np.array(base, float)
    f = np.array(front, float); f = f - d * (f @ d); f /= np.linalg.norm(f)
    mid = base + d * length / 2
    k.add("ear", oriented(mid, (width, thick, length / 2), d, f, ear_shape), color, bone, mirror=mirror)
    # the hollow over the ear's upper part, just proud of its front face
    lc = base + d * length * 0.6 + f * thick * 0.55
    k.add("earlining", outward(oriented(lc, (width * lining, thick * 0.45, length * 0.3), d, f, lining_shape, sub=3)),
          TEAL, bone, fuse=False, mirror=mirror)
    if lobe:
        # a small second lobe of fur in front of the ear's root, on its inner side
        side = np.cross(f, d)
        inner = -side if side[0] > 0 else side          # toward the middle
        lb = base + d * length * 0.3 + f * thick * 1.1 + inner * width * 0.25
        dl = unit(*(d + inner * 0.25))
        k.add("earlobe", outward(oriented(lb, (width * 0.42, thick * 0.55, length * 0.2), dl, f, ear_shape, sub=3)),
              color, bone, fuse=False, mirror=mirror)


def eye_open(u, v):
    """A brown eye: a dark rim, the iris, a dark pupil and two white glints (u outward, v up)."""
    r = np.hypot(u, v)
    c = np.tile(srgb(INK), (len(u), 1))
    c[r < 0.83] = srgb(IRIS)
    c[r < 0.46] = srgb(PUPIL)
    c[((u - 0.26) / 0.27) ** 2 + ((v - 0.3) / 0.27) ** 2 < 1] = srgb(WHITE)
    c[((u + 0.28) / 0.13) ** 2 + ((v + 0.34) / 0.13) ** 2 < 1] = srgb(WHITE)
    return c


def cut_eye(a, b):
    """A round eye's outline cut along the line v = a + b u (a brow over it): its points (u, v), walked round."""
    pts = []
    for th in np.linspace(0, 2 * math.pi, 120, endpoint=False):
        u, v = math.cos(th), math.sin(th)
        pts.append((u, min(v, a + b * u)))
    return pts


def lay_eyes(k, state, E, n, half, lid, cut=None, eps=0.003, look=eye_open):
    """The two eyes laid by hand (+X first), so that a cut outline is mirrored too; u outward on both."""
    CI = srgb(INK)
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        f = eye_look(state, look, srgb(lid), CI)
        if s > 0:
            col = f
            out = polygon(cut) if cut else None
        else:
            col = (lambda f: (lambda u, v: f(-u, v)))(f)
            out = polygon([(-a, b) for a, b in cut[::-1]]) if cut else None
        k.decal(np.array(E, float) * m, np.array(n, float) * m, half, col, eps=eps, outline=out, rings=20, segs=56)


def star_pad(n_points=8, inner=0.62):
    """The outline of the paw's white star round its yellow pad."""
    pts = []
    for i in range(2 * n_points):
        th = math.pi * i / n_points + math.pi / 2
        r = 1.0 if i % 2 == 0 else inner
        pts.append((r * math.cos(th), r * math.sin(th)))
    return polygon(pts)


def pad(k, c, n, r, eps=0.003):
    """The palm's mark: a yellow round pad in a white star."""
    def look(u, v):
        col = np.tile(srgb(WHITE), (len(u), 1))
        col[np.hypot(u, v) < 0.5] = srgb(YELLOW)
        return col
    k.decal(c, n, (r, r), look, eps=eps, outline=star_pad(), rings=12, segs=96)


def tail_leaf(k, base, d, length, width, thick, bone, color=CREAM):
    """A broad flat tail, a paddle wider toward its end, its face turned toward the front three-quarter."""
    d = unit(*d)
    across = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(across) < 1e-3:
        across = np.array([1.0, 0, 0])
    across /= np.linalg.norm(across)            # the paddle's thin side: square to its plane
    def paddle(V):
        V = V.copy()
        t = (V[:, 2] + 1) / 2
        V[:, 0] *= 0.45 + 0.75 * np.clip(t, 0, 1) ** 0.6 - 0.15 * np.clip(t, 0, 1) ** 4
        return V
    k.add("tail", outward(oriented(np.array(base, float) + d * length / 2, (width, thick, length / 2), d, across,
                                   paddle)), color, bone, fuse=False)


# --- Pawmi's own frame --------------------------------------------------------------------------------------------------
HC, HR = np.array([0.0, -0.062, 0.112]), np.array([0.176, 0.124, 0.112])     # the head: the mass's front
BC, BR = np.array([0.0, 0.06, 0.104]), np.array([0.158, 0.13, 0.1])          # the body behind it


def seat(V):
    V = V.copy()
    V[:, 2] = np.where(V[:, 2] < -0.55, -0.55 + (V[:, 2] + 0.55) * 0.45, V[:, 2])      # a flat belly on the ground
    return V


def on_head(x, z, lift=0.0):
    """A point on the head's front at x, z and the outward normal there."""
    q = 1 - (x / HR[0]) ** 2 - ((z - HC[2]) / HR[2]) ** 2
    y = HC[1] - HR[1] * math.sqrt(max(q, 0.0))
    p = np.array([x, y, z])
    g = (p - HC) / HR ** 2
    g /= np.linalg.norm(g)
    return p + g * lift, g


def pawmi(src, dest):
    k = Kit(src, coat="fur")

    # --- one soft round mass lying on its belly ----------------------------------------------------------------------
    k.add("head", ell(HC, HR, deform=seat), ORANGE, "head")
    k.add("body", ell(BC, BR, deform=seat), ORANGE, "body")
    k.add("crown", ell((0, -0.02, 0.16), (0.12, 0.1, 0.07)), ORANGE, "head")

    # the crown's tuft: an onion of fur, its tip turned up and to +X
    k.add("tuft", tube([(0.0, -0.035, 0.19), (0.004, -0.035, 0.25), (0.02, -0.032, 0.298), (0.044, -0.028, 0.332),
                        (0.06, -0.026, 0.348)], [0.08, 0.066, 0.038, 0.018, 0.009]), ORANGE, "head")

    # --- big broad ears up and a little out, a teal hollow, a lobe before each root ----------------------------------
    ear(k, (0.092, -0.03, 0.17), (0.3, 0.1, 0.95), 0.24, 0.058, 0.026, "ear{s}")

    # --- cream front paws at the front corners, small brown hind feet --------------------------------------------------
    k.add("paw", ell((0.128, -0.128, 0.022), (0.048, 0.042, 0.024)), CREAM, "leg{s}", mirror=True)
    k.add("hind", ell((0.1, 0.13, 0.03), (0.042, 0.04, 0.03)), BROWN, "hind{s}", mirror=True)

    # --- the tail: a broad cream paddle standing up behind, run back and out to +X --------------------------------------
    td = unit(*(unit(0.6, 0.8, 0) * 0.5 + np.array([0, 0, 1.0]) * 0.86))
    tail_leaf(k, (0.04, 0.16, 0.14), td, 0.2, 0.05, 0.022, "tail")

    # --- the face, low on the mass's front ------------------------------------------------------------------------------
    for s in (1, -1):
        p, n = on_head(0.122 * s, 0.096)
        k.decal(p, n, (0.03, 0.027), YELLOW, eps=0.0025, rings=10, segs=40)
    p, n = on_head(0.0, 0.066)
    k.decal(p, unit(0, -1, -0.08), (0.068, 0.04), CREAM, eps=0.0025, rings=12, segs=56)
    p, n = on_head(0.0, 0.084)
    k.decal(p, unit(0, -1, -0.02), (0.007, 0.005), NOSE, eps=0.0045, rings=5, segs=24)
    # a small frown: a short arc curving down, one clean shape on a patch of the muzzle's own cream
    CM, CC = srgb(MOUTH), srgb(CREAM)
    def frown(u, v):
        c = np.tile(CC, (len(u), 1))
        c[(np.abs(v - (0.25 - 0.75 * u * u)) < 0.3) & (np.abs(u) < 0.95)] = CM
        return c
    p, n = on_head(0.0, 0.06)
    k.decal(p, unit(0, -1, -0.15), (0.018, 0.012), frown, eps=0.004, rings=12, segs=56)

    k.build(voxel=0.0042, band=0.006, wband=0.04)

    E, n = on_head(0.05, 0.112)
    k.eye_states(lambda state: lay_eyes(k, state, E, n, (0.026, 0.028), ORANGE))
    k.export(dest)


if __name__ == "__main__":
    pawmi(*args())
