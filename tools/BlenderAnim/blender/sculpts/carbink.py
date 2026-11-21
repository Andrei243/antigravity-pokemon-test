# Carbink sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/carbink.py -- <the export's Carbink.glb> <out.glb>
#
# The design, from the references: a hovering lump of lilac-grey rock, broad and flat at its top and tapering to a
# blunt point below, like a heart turned over; pale blue crystals stand out of it (a small one high on its front to the
# left, one on each side, and a cluster of large ones at its point beneath). On its top a white cloud of a collar, wider
# than the head, its front swelling in two lobes like a moustache; out of the collar's middle a small grey dome of a
# head with two round bright blue eyes close together just over the collar, no mouth seen. A tall pale blue crystal
# stands on the crown, and from the head's upper sides two long flat ears reach out almost level, rising a little, each
# a kite broad past its middle and pointed, its tip darker.
#
# Written to be borrowed from (diancie.py imports it with sibling): the rock (rock), the ear (ear) and the eye's look
# (round_eye) are functions; the build is under the main guard. The faceted crystals are roggenrola.py's.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, unit, eye_look, args, hull, slab, sibling

R = sibling("roggenrola")                 # crystal (a faceted hull) and Glints (crystals joined a bone and colour)

# --- the colours ----------------------------------------------------------------------------------------------------
STONE, STONE_DK = "#8F8CA6", "#76738C"    # the rock, its darker underside
GREY, EAR_TIP = "#9A98B6", "#74728F"      # the head and ears, the ears' darker points
FLUFF = "#F3F2F8"
ICE, ICE_DK = "#BFE4F6", "#8DC5EA"        # the crystals
EYE_BLUE, EYE_LT, EYE_DK, EYE_INK = "#2C96E2", "#7CCBF6", "#1A55A8", "#15284E"

# --- the frame ------------------------------------------------------------------------------------------------------
HEAD_C = np.array([0.0, -0.008, 0.306])   # the dome's middle and radii
HEAD_R = np.array([0.077, 0.066, 0.06])
EYE_X, EYE_Z, EYE_HALF = 0.038, 0.322, 0.021


def on_ell(c, r, x, z, front=True):
    """The point of an ellipsoid's front (or back) with that x and z, and its outward normal there."""
    c, r = np.array(c, float), np.array(r, float)
    a = (x - c[0]) / r[0]; b = (z - c[2]) / r[2]
    dy = r[1] * math.sqrt(max(1 - a * a - b * b, 0.0))
    p = np.array([x, c[1] - dy if front else c[1] + dy, z])
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def rock(c, rings, seed, tip=None, jitter=0.12):
    """A lump of rock with flat faces: rings of (z, half x, half y, corners) about the axis through c, each corner
    jittered in radius and angle, made one hull. tip: an extra point (its lowest)."""
    rs = np.random.RandomState(seed)
    pts = []
    for z, hx, hy, n in rings:
        off = rs.rand() * 2 * math.pi
        for i in range(n):
            th = off + 2 * math.pi * (i + 0.35 * (rs.rand() - 0.5)) / n
            j = 1 + jitter * (rs.rand() - 0.5)
            pts.append((c[0] + hx * j * math.cos(th), c[1] + hy * j * math.sin(th), z + 0.012 * (rs.rand() - 0.5)))
    if tip is not None:
        pts.append(tip)
    return hull(np.array(pts))


def ear(k, name, root, out_dir, up_dir, length, width, thick, colour, tip_colour, tip_share, bone):
    """A flat ear, a kite broad past its middle and pointed, in the plane of out_dir and up_dir; its tip a thicker
    plate of the darker colour over it (so the mark shows on both faces). Mirrored to -X."""
    L, W = length, width
    outline = [(0, -0.22 * W), (0.55 * L, -0.55 * W), (L, 0.0), (0.52 * L, 0.5 * W), (0, 0.22 * W)]
    k.add(name, slab(outline, root, out_dir, up_dir, thick), colour, bone, fuse=False, mirror=True)
    if tip_colour:
        a = (1 - tip_share) * L
        lo = -0.55 * W * (L - a) / (0.45 * L) - 0.0015
        hi = 0.5 * W * (L - a) / (0.48 * L) + 0.0015
        k.add(name + "tip", slab([(a, lo), (L + 0.002, 0.0), (a, hi)], root, out_dir, up_dir, thick * 1.35),
              tip_colour, bone, fuse=False, mirror=True)


def round_eye(iris, light, deep, ink, white=(1, 1, 1)):
    """A round eye that is all iris: a dark rim, the colour lightening below, a large dark pupil and a highlight."""
    ci, cl, cd, ck = srgb(iris), srgb(light), srgb(deep), srgb(ink)
    def look(u, v):
        c = np.tile(ci, (len(u), 1))
        lower = np.clip((-v - 0.05) / 0.7, 0, 1)[:, None]
        c = c * (1 - lower) + cl * lower
        c[(u / 0.42) ** 2 + ((v - 0.04) / 0.48) ** 2 < 1] = cd
        c[u * u + v * v > 0.84 ** 2] = ck
        c[((u + 0.3) / 0.22) ** 2 + ((v - 0.34) / 0.22) ** 2 < 1] = white
        return c
    return look


def carbink(src, out):
    k = Kit(src, coat="shell")
    glints = R.Glints()

    # --- the rock: broad and flat at its top, tapering to a blunt point below -------------------------------------
    k.add("rock", rock((0.0, 0.004), [(0.262, 0.104, 0.086, 7), (0.236, 0.122, 0.102, 8), (0.18, 0.116, 0.098, 8),
                                      (0.12, 0.09, 0.078, 7), (0.07, 0.052, 0.046, 6)], 7, tip=(-0.008, 0.01, 0.045)),
          STONE, "body", fuse=False, surface="shell")

    # crystals: small one high on the front to the viewer's left, one on each side, a cluster beneath
    for base, d, ln, r, col in [((-0.052, -0.085, 0.2), (-0.35, -1, 0.25), 0.038, 0.016, ICE),
                                ((-0.108, -0.02, 0.135), (-1, -0.25, -0.35), 0.048, 0.019, ICE),
                                ((0.106, -0.03, 0.15), (1, -0.3, -0.1), 0.044, 0.018, ICE),
                                ((0.0, -0.01, 0.07), (0.05, -0.1, -1), 0.072, 0.028, ICE),
                                ((0.03, -0.03, 0.075), (0.6, -0.45, -0.7), 0.06, 0.022, ICE_DK),
                                ((-0.032, -0.03, 0.078), (-0.65, -0.4, -0.65), 0.06, 0.022, ICE),
                                ((0.02, 0.03, 0.075), (0.3, 0.6, -0.75), 0.058, 0.021, ICE_DK),
                                ((-0.024, 0.03, 0.08), (-0.35, 0.55, -0.75), 0.052, 0.02, ICE)]:
        glints.put(R.crystal(base, d, ln, r, sides=6, tip=0.35, turn=17), col, "body")

    # --- the collar: a cloud of fused white puffs on the rock's top, the head rising out of its middle -------------
    for i in range(24):
        th = 2 * math.pi * i / 24
        sx, sy = math.sin(th), -math.cos(th)             # th 0 at the front
        front = max(0.0, -sy)
        lobe = math.exp(-((abs(sx) - 0.45) / 0.3) ** 2) * front     # the moustache's two lobes
        rr = 0.03 + 0.008 * lobe + 0.004 * (i % 2)
        z = 0.268 + 0.012 * abs(sx) - 0.012 * front + 0.006 * lobe
        k.add(f"puff{i}", ell((0.108 * sx, -0.004 + 0.078 * sy, z), (rr * 1.15, rr, rr * 0.85)), FLUFF, "body")
    k.add("puffcore", ell((0.0, -0.004, 0.268), (0.1, 0.07, 0.03)), FLUFF, "body")

    # --- the head: a small dome rising out of the collar -----------------------------------------------------------
    k.add("head", ell(HEAD_C, HEAD_R), GREY, "head")
    # the crown's tall crystal, leaning a little back
    glints.put(R.crystal((0.0, 0.0, 0.352), (0.04, 0.12, 1), 0.075, 0.021, sides=6, tip=0.3, turn=30, flat=0.8),
               ICE, "head")
    glints.flush(k)

    # the ears: long flat kites reaching out almost level from the head's upper sides, rising a little
    ear(k, "ear", (0.058, 0.002, 0.338), unit(1, 0.12, 0.2), (0, 0.3, 1), 0.17, 0.07, 0.013, GREY, EAR_TIP, 0.32,
        "head")

    k.build(voxel=0.0035, smooth=6, band=0.004, wband=0.03)

    # --- the eyes: round, bright blue, close together just over the collar ----------------------------------------
    look = round_eye(EYE_BLUE, EYE_LT, EYE_DK, EYE_INK)
    lid, ink = srgb(GREY), srgb("#2E2C40")
    p, n = on_ell(HEAD_C, HEAD_R, EYE_X, EYE_Z)

    def eyes(state):
        k.decal(p, n, (EYE_HALF, EYE_HALF * 1.08), eye_look(state, look, lid, ink, line=0.15), eps=0.002,
                rings=20, segs=56, mirror=True)

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    carbink(*args())
