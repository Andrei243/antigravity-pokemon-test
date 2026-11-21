# Skiddo sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/skiddo.py -- <the export's Skiddo.glb> <out.glb>
# Written to be grown from: gogoat.py imports its frame (sibling("skiddo")): the leafy spike, the mane's core and its
# rings of leaves, the back sprig, the horn with its dark bands, the shaggy fringe, the leg with its sock and hoof,
# the face's paint round the head's axis.
#
# The design, from the references: a small kid goat; a big round head some three eighths of its height, brown over
# the crown with a white face from the snout up between the eyes and back past them to a point; a big dark grey nose
# on the snout's tip, the mouth open and pink under it; upright black eyes with a white glint; round brown ears out to
# the sides; grey horns from the crown curving up and back, banded darker; a thick collar of green leaves round the
# neck and over the chest (its largest mass after the body), a little white leaf under the chin; a shaggy brown body
# with a jagged fringe under the belly and the rump; a green sprig of leaves standing on the back; dark legs with
# white socks and orange hooves.
# Reference ratios (3D picture): head 0.37 of the height, leg showing under the fringe 0.27, length 0.8 of the height.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, ring_band, outward, srgb, inside, unit, lumpy, eye_look, args, sibling

t = sibling("teddiursa")
dl = sibling("deerling")

BROWN, DARK, SOCK, HOOF = "#8A735F", "#4B3F45", "#ECE8F2", "#EE7A38"
GREEN, GREEN2, WHITE, NOSE = "#2C9A5C", "#3DB06C", "#F7F6F2", "#5C5E66"
HORN, BAND = "#6A686E", "#3E3C42"
MOUTH, TONGUE, INK = "#8C2E38", "#EE8A92", "#16141A"


def spike(k, name, root, d, length, width, thick, bone, color, side=None, surface="fur", mid=0.4, belly=0.75):
    """A solid kite-sectioned spike from root along d (a leaf, a tuft of fur), broadest at mid of its length."""
    root, d = np.array(root, float), unit(*d)
    a = np.cross(d, (0, 0, 1.0)) if side is None else np.array(side, float)
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a = a - d * (a @ d); a /= np.linalg.norm(a)
    b = np.cross(d, a)
    k.add(name, hull([root + a * width * 0.6, root - a * width * 0.6, root + b * thick, root - b * thick,
                      root + d * length * mid + a * width, root + d * length * mid - a * width,
                      root + d * length * mid + b * thick * belly, root + d * length * mid - b * thick * belly,
                      root + d * length]), color, bone, fuse=False, surface=surface)


def mane(k, cores, rings, colours, bone="body", seed=3):
    """
    A mane of leaves: fused lumpy cores (centre, radii) and rings of loose leaves, each ring (centre, radii of the
    ellipse its roots lie on, angles from straight ahead in degrees, length, width, thick, droop). The leaves point
    out from the ring's centre and down by the droop; their colours alternate.
    """
    for i, (c, r) in enumerate(cores):
        k.add(f"mane{i}", ell(c, r, deform=lumpy(5, 3, 0.12)), colours[0], bone, surface="leaf")
    n = 0
    rs = np.random.RandomState(seed)
    for c, r, angles, L, W, T, droop in rings:
        c = np.array(c, float)
        for a in np.radians(angles):
            o = np.array([math.sin(a), -math.cos(a), 0.0])
            root = c + o * np.array([r[0], r[1], 0]) + (0, 0, rs.uniform(-0.006, 0.006))
            d = o + np.array([0, 0, -droop])
            side = np.cross(unit(*d), (0, 0, 1.0))
            spike(k, f"leaf{n}", root, d, L * rs.uniform(0.88, 1.08), W, T, bone, colours[n % len(colours)],
                  side=side if np.linalg.norm(side) > 1e-3 else None, surface="leaf")
            n += 1


def sprig(k, root, leaves, colours, bone="tail"):
    """A sprig of leaves standing from root: each (direction, length, width), splayed as a cone."""
    for i, (d, L, W) in enumerate(leaves):
        d = unit(*d)
        side = np.cross(d, (0, 1.0, 0.3)); side /= np.linalg.norm(side)
        spike(k, f"sprig{i}", root, d, L, W, max(0.0045, W * 0.3), bone, colours[i % len(colours)], side=side,
              surface="leaf", mid=0.45, belly=0.8)


def horn(k, pts, radii, bands, side, colour=HORN, band=BAND, bone="head"):
    """A horn: a loose tube through pts (authored on +X), darker bands round it at the given points' indices."""
    m = np.array([side, 1, 1.0])
    P = [np.array(p) * m for p in pts]
    nm = "R" if side > 0 else "L"
    k.add("horn" + nm, outward(tube(P, radii, seg=16, per=6)), colour, bone, fuse=False, surface="shell")
    for j, i in enumerate(bands):
        c = P[i]; ax = P[i + 1] - P[i - 1]
        k.add(f"hband{nm}{j}", ring_band(c, ax, radii[i] + 0.0008, radii[i] * 0.45, thick=0.003), band, bone,
              fuse=False, surface="shell")


def fringe(k, roots, colour, length, width, thick, bone="body", tag="fr"):
    """A shaggy fringe: tufts from each (root, direction), alternating a little in length, mirrored by hand."""
    for i, (root, d) in enumerate(roots):
        L = length * (1.0 if i % 2 else 0.78)
        for s in ((1, -1) if root[0] > 0.004 else (1,)):
            m = np.array([s, 1, 1.0])
            spike(k, f"{tag}{i}{'R' if s > 0 else 'L'}", np.array(root) * m, np.array(d) * m, L, width, thick, bone,
                  colour)


def goat_eye(ink, glint=(1, 1, 1)):
    """An upright black eye with a white glint high on its outer side and a small one low."""
    ink, glint = np.array(ink, float), np.array(glint, float)
    def look(u, v):
        c = np.tile(ink, (len(u), 1))
        c[((u - 0.22) / 0.34) ** 2 + ((v - 0.38) / 0.3) ** 2 < 1] = glint
        c[np.hypot(u + 0.2, v + 0.5) < 0.14] = glint
        return c
    return look


def skiddo(src, out_path):
    k = Kit(src, coat="fur")
    H = np.array([0, -0.155, 0.33])             # the head's middle (its bone is at 0, -0.12, 0.27)
    HR = np.array([0.066, 0.066, 0.068])
    B = np.array([0, 0.015, 0.2])               # the trunk's middle

    # --- the trunk: a deep barrel, the belly low, shoulders and thighs fused over the legs --------------------
    k.add("barrel", ell(B, (0.078, 0.15, 0.082)), BROWN, "body")
    k.add("rump", ell((0, 0.105, 0.205), (0.08, 0.07, 0.08)), BROWN, "body")
    k.add("chest", ell((0, -0.08, 0.205), (0.075, 0.07, 0.08)), BROWN, "body")
    k.add("belly", ell((0, 0.015, 0.155), (0.07, 0.125, 0.06)), BROWN, "body")
    k.add("shoulder", ell((0.054, -0.11, 0.15), (0.04, 0.048, 0.066)), BROWN, "leg{s}", mirror=True)
    k.add("thigh", ell((0.056, 0.12, 0.155), (0.044, 0.054, 0.07)), BROWN, "hind{s}", mirror=True)
    dl.leg(k, "fore", 0.058, -0.115, 0.13, (0.032, 0.027, 0.024), "leg{s}", color=DARK, hoof=HOOF)
    dl.leg(k, "hind", 0.059, 0.122, 0.13, (0.033, 0.027, 0.024), "hind{s}", color=DARK, hoof=HOOF, knee=0.006)
    k.paint(lambda P: inside(P[:, 2] - 0.068, 0.0022) * inside(np.abs(P[:, 0]) - 0.12, 0.004), SOCK)

    # --- the neck under the collar, the head: big and round, a short muzzle; a big grey nose --------------
    k.add("neck", tube([(0, -0.095, 0.235), (0, -0.125, 0.29)], [0.048, 0.044]), BROWN, "head")
    HEAD = ell(H, HR)
    k.add("head", HEAD, BROWN, "head")
    MUZ = ell(H + (0, -0.046, -0.032), (0.046, 0.044, 0.038), rot=(10, 0, 0))
    k.add("muzzle", MUZ, BROWN, "head")
    k.add("nose", outward(ell(H + (0, -0.085, -0.02), (0.026, 0.018, 0.02), rot=(-15, 0, 0))), NOSE, "head",
          fuse=False, surface="skin")

    # the face white from the snout up between the eyes and back past them to a point; the crown brown
    FRONT = lambda P: (P[:, 1] < H[1] + 0.025) * (P[:, 2] > 0.24)
    z0 = H[2]
    FACE = t.mirrored([(0.0, z0 + 0.05), (0.028, z0 + 0.042), (0.055, z0 + 0.034), (0.08, z0 + 0.024),
                       (0.098, z0 + 0.012), (0.085, z0 - 0.02), (0.07, z0 - 0.06), (0.055, z0 - 0.12),
                       (0.0, z0 - 0.12)])
    k.paint(lambda P: t.poly_mask(*t.round_about(P, H, HR[0]), FACE, edge=0.002) * FRONT(P), WHITE,
            on=["head", "muzzle"])

    # the mouth open under the nose: a dark red crescent with the pink tongue in it
    CM, CT, CW = srgb(MOUTH), srgb(TONGUE), srgb(WHITE)
    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        c[(v < -0.15) & (np.hypot(u / 0.7, (v + 0.6) / 0.45) < 1)] = CT
        return c
    MC = t.hit(MUZ, H + (0, -1, -0.058), (0, 1, 0))
    k.decal(MC, unit(0, -1, -0.45), (0.026, 0.016), mouth, eps=0.003, rings=10, segs=56,
            outline=lambda th: 1.0 if math.sin(th) < 0 else 0.55)

    # --- round brown ears out to the sides --------------------------------------------------------------------
    k.add("ear", ell((0.072, -0.145, 0.378), (0.034, 0.016, 0.024), rot=(0, -28, -18)), BROWN, "ear{s}", mirror=True)

    # --- grey horns from the crown inside the ears, up and back, banded darker ---------------------------------
    for s in (1, -1):
        horn(k, [(0.018, -0.15, 0.385), (0.024, -0.134, 0.418), (0.034, -0.1, 0.436), (0.046, -0.062, 0.438),
                 (0.058, -0.028, 0.426), (0.066, -0.004, 0.406)], [0.0135, 0.0125, 0.011, 0.0095, 0.008, 0.0065],
             [2, 3], s)

    # --- the collar of leaves round the neck and over the chest; a little white leaf under the chin ---------
    mane(k, [((0, -0.105, 0.23), (0.084, 0.072, 0.085)), ((0, -0.095, 0.3), (0.07, 0.05, 0.05))],
         [((0, -0.105, 0.17), (0.07, 0.065), np.linspace(-125, 125, 11), 0.05, 0.022, 0.0075, 0.9),
          ((0, -0.1, 0.24), (0.082, 0.07), np.linspace(-115, 115, 9), 0.042, 0.022, 0.0075, 0.5),
          ((0, -0.085, 0.315), (0.07, 0.05), np.linspace(-150, -50, 3).tolist() + np.linspace(50, 150, 3).tolist(),
           0.036, 0.02, 0.007, 0.3)],
         [GREEN, GREEN2])
    for i, (x, d) in enumerate(((0.0, (0, -0.35, -1)), (0.013, (0.4, -0.3, -1)), (-0.013, (-0.4, -0.3, -1)))):
        spike(k, f"bib{i}", (x, -0.188, 0.262), d, 0.03, 0.011, 0.006, "head", WHITE, side=(1, 0, 0))

    # --- the shaggy fringe under the belly and round the rump ---------------------------------------------------
    roots = [((0.064, y, 0.145), (0.6, 0, -1)) for y in np.linspace(-0.05, 0.15, 6)]
    roots += [((0.03, y, 0.11), (0.3, 0.1, -1)) for y in (-0.02, 0.05)]
    roots += [((0.045, 0.175, 0.18), (0.4, 0.8, -0.6)), ((0.0, 0.185, 0.21), (0, 1, -0.4))]
    fringe(k, roots, BROWN, 0.042, 0.018, 0.0075)

    # --- the green sprig standing on the back -----------------------------------------------------------------
    sprig(k, (0, 0.07, 0.27), [((0, 0.3, 1), 0.105, 0.026), ((0.6, 0.3, 0.85), 0.09, 0.024),
                                ((-0.6, 0.3, 0.85), 0.09, 0.024), ((0.4, 0.85, 0.45), 0.075, 0.022),
                                ((-0.4, 0.85, 0.45), 0.075, 0.022)], [GREEN, GREEN2])

    # --- the eyes: upright, black with a white glint, on the white face -------------------------------------
    de = unit(0.55, -0.82, 0.1)
    E = t.hit(HEAD, H + (0.036, -1, 0.006), (0, 1, 0))
    lay = t.eyes(k, E, de, (0.016, 0.022), srgb(WHITE), srgb(INK), goat_eye(srgb(INK)))

    k.build(voxel=0.0045, band=0.005, wband=0.04)
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    skiddo(*args())
