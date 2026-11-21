# Drizzile sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/drizzile.py -- <the export's Drizzile.glb> <out.glb>
#
# Sobble grown (sobble.py: the line's blues and dark spots, on_ell, the curled tail with a spot in its curl). From the
# references: a slouching lizard, deeper blue, on wide crouched legs. A long egg of a head carried forward on a thin
# neck, the snout rounded; a lavender fringe flops over the front of the crown and down over the -X eye, a dark blue
# band along its back edge, a dark blue tuft curling up from the crown. The eyes heavy-lidded: a lime lid over the top
# half, white below with a black pupil toward the outer corner; a dark blue spot behind each; a small frown. A slim
# trunk, a big white belly with a dark blue point of a collar over it; thin arms carried forward and down, great lime
# hands of three fingers and a thumb; fat haunches fused into the hips, short shins in lime socks, long lime feet; the
# tail back and out to +X, rising into a curl with a dark spot in it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, blade, outward, srgb, inside, unit, eye_look, args, sibling

sb = sibling("sobble")
BLUE = "#3C9ED6"
WHITE_ = "#F2F6F6"
LIME = "#B6E05C"
LAVENDER = "#C894E2"
DARK = sb.SPOT
INK, WHITE = sb.INK, sb.WHITE
VOXEL = 0.0058

H = np.array([0.0, -0.09, 0.495])           # the head: a long egg, carried forward
HR = np.array([0.114, 0.156, 0.114])


def drizzile_eye(lid_v=0.12, slope=-0.12):
    """The heavy-lidded eye (u outward, v up): lime lid above a line sloping a little, white below, a black pupil
    toward the outer corner just under the lid."""
    lime, white = srgb(LIME), WHITE

    def eye(u, v):
        c = np.tile(white, (len(u), 1))
        edge = lid_v + slope * u
        c[np.hypot((u - 0.32) / 0.34, (v + 0.12) / 0.48) < 1] = INK
        c[v > edge] = lime
        c[np.abs(v - edge) < 0.11] = INK
        c[np.hypot(u, v) > 0.88] = INK
        c[(np.hypot(u, v) > 0.88) & (v > edge)] = lime * 0.82
        return c
    return eye


def fringe_dir(az, el):
    return np.stack([np.sin(az) * np.cos(el), -np.cos(az) * np.cos(el), np.sin(el)], axis=-1)


def drizzile(src, out):
    k = Kit(src, coat="scales")

    # --- trunk: slim, leaning forward over round hips; a thin neck carrying the head forward -------------------------
    k.add("hips", ell((0, 0.02, 0.225), (0.074, 0.066, 0.062)), BLUE, "body")
    k.add("torso", ell((0, -0.012, 0.3), (0.064, 0.058, 0.098), rot=(14, 0, 0)), BLUE, "body")
    k.add("neck", tube([(0, -0.03, 0.36), (0, -0.045, 0.4), (0, -0.06, 0.43)], [0.046, 0.042, 0.044]), BLUE, "head")

    # the white belly and the dark point of the collar over it
    k.decal((0, -0.068, 0.27), unit(0, -1, 0.12), (0.05, 0.078), WHITE_, eps=0.003, rings=30, segs=64)
    k.paint(lambda P: inside(np.abs(P[:, 0]) - (P[:, 2] - 0.322) * 1.3, 0.002) * inside(P[:, 2] - 0.375, 0.003)
            * inside(P[:, 1] + 0.035, 0.004), DARK, on=["torso", "neck"])

    # --- the head: a long egg, a rounded snout ---------------------------------------------------------------------
    def egg(V):
        V = V.copy()
        back = np.clip(V[:, 1], 0, 1)
        V[:, 0] *= 1 + 0.08 * back
        V[:, 2] *= 1 + 0.06 * back
        return V
    k.add("head", ell(H, HR, deform=egg), BLUE, "head")
    k.add("snout", ell(H + (0, -0.11, -0.038), (0.079, 0.077, 0.068)), BLUE, "head")
    k.add("jaw", ell(H + (0, -0.055, -0.064), (0.092, 0.106, 0.053)), BLUE, "head")

    # the fringe: a lavender flap over the front of the crown and down over the -X eye, a dark band at its back edge
    azs = np.radians([-30, 0, 25, 45, 70, 92])
    lows = np.radians([36, 20, 0, -18, -14, -4])
    S = HR * 1.08

    def flap(a0, a1, el_top):
        def at(u, v):
            az = np.radians(a0) + u * np.radians(a1 - a0)
            lo = np.interp(az, azs, lows)
            el = lo + v * (np.radians(el_top) - lo)
            dd = fringe_dir(az, el)
            lift = 1 + 0.06 * (1 - v)                 # its lower edge stands a little off the head
            return (H + dd * S * lift[..., None]) * np.array([-1, 1, 1.0])     # over the -X eye
        return at
    k.add("fringe", outward(sheet(flap(-30, 72, 84), 40, 30, 0.02)), LAVENDER, "head", fuse=False)
    k.add("band", outward(sheet(flap(-46, -30, 84), 10, 30, 0.021)), DARK, "head", fuse=False)
    # the tuft: a dark blue teardrop curling up and back from the crown
    k.add("tuft", blade([H + (0.004, 0.005, 0.08), H + (0.01, 0.02, 0.13), H + (0.006, 0.05, 0.16),
                         H + (0.0, 0.075, 0.15)], [0.03, 0.032, 0.026, 0.014], (1, 0, 0), 0.45), DARK, "head")

    # the frown and the dark spots behind the eyes
    m = sb.on_ell(H + (0, -0.11, -0.038), (0.079, 0.077, 0.068), (0, -1, -0.45))
    def frown(u, v):
        c = np.tile(srgb(BLUE), (len(u), 1))
        c[np.abs(v - (0.3 - 0.75 * u * u)) < 0.34] = INK
        return c
    k.decal(m, unit(0, -1, -0.35), (0.036, 0.013), frown, eps=0.002, rings=10, segs=60)
    for s in (1, -1):
        p = sb.on_ell(H, HR, (0.95 * s, 0.1, -0.2))
        k.decal(p, sb.ell_normal(H, HR, p), (0.022, 0.02), DARK, eps=0.002, rings=10, segs=40)

    # --- arms: thin, carried forward and down; great lime hands, loose so that they keep their edge --------------------
    SH, EL, WR = np.array((0.055, -0.005, 0.33)), np.array((0.098, -0.04, 0.28)), np.array((0.075, -0.115, 0.255))
    k.add("arm", tube([SH, EL, WR], [0.019, 0.016, 0.015]), BLUE, "arm{s}", mirror=True)
    k.add("hand", ell(WR + (0, -0.022, -0.004), (0.03, 0.03, 0.02), rot=(15, 0, -15)), LIME, "arm{s}", fuse=False,
          mirror=True)
    for j, (dx, dy, dz) in enumerate([(0.02, -0.04, -0.03), (0.0, -0.046, -0.032), (-0.02, -0.04, -0.03)]):
        b = WR + (dx * 0.6, -0.04, -0.008)
        k.add(f"finger{j}", tube([b, b + (dx * 0.4, dy * 0.5, dz * 0.4), b + (dx * 0.5, dy * 0.6, dz)],
                                 [0.0115, 0.011, 0.0105]), LIME, "arm{s}", fuse=False, mirror=True)
    k.add("thumb", tube([WR + (-0.018, -0.02, 0.004), WR + (-0.034, -0.035, 0.006)], [0.011, 0.01]), LIME, "arm{s}",
          fuse=False, mirror=True)

    # --- legs: fat haunches fused into the hips, knees out, short shins in lime socks, long lime feet --------------------
    k.add("thigh", ell((0.088, -0.012, 0.15), (0.058, 0.064, 0.09), rot=(0, -18, 0)), BLUE, "leg{s}", mirror=True)
    k.add("shin", tube([(0.118, -0.05, 0.11), (0.118, -0.04, 0.06), (0.112, -0.03, 0.025)], [0.037, 0.033, 0.029]),
          BLUE, "leg{s}", mirror=True)
    k.add("foot", ell((0.114, -0.05, 0.014), (0.036, 0.06, 0.015)), LIME, "leg{s}", mirror=True)
    for j, d in enumerate([(0.45, -1, 0), (0.0, -1, 0), (-0.4, -1, 0)]):
        b = np.array((0.114, -0.09, 0.012))
        k.add(f"toe{j}", tube([b, b + unit(*d) * 0.035], [0.0115, 0.011]), LIME, "leg{s}", mirror=True)
    k.paint(lambda P: inside(P[:, 2] - 0.058, 0.0025) * inside(0.07 - np.abs(P[:, 0]), 0.004), LIME,
            on=["shin", "foot", "toe"])

    # --- the tail: back and out to +X, rising into a curl with a dark spot in it ---------------------------------------
    pts, rad, C = sb.curl((0.0, 0.05, 0.215), (0.5, 0.86, 0), 0.19, 0.105, 0.075, 0.012, 1.62, 0.034, 0.015)
    k.add("tail", tube(pts, rad, seg=20, per=4), BLUE, "tail")
    sb.spots(k, [C + (0, 0, 0.004)], 0.042, DARK, ["tail"])

    k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.04)

    # --- the eyes: heavy-lidded, lime above, white below with a black pupil to the outer corner -------------------------
    de = unit(0.78, -0.5, 0.28)
    E = sb.on_ell(H, HR, de)
    En = sb.ell_normal(H, HR, E)
    look = drizzile_eye()
    lime = srgb(LIME)

    def fierce(u, v):
        c = drizzile_eye(lid_v=0.02, slope=0.32)(u, v)
        return c

    def lay(state):
        fn = fierce if state == "Fierce" else eye_look(state, look, lime, INK, line=0.15)
        k.decal(E, En, (0.046, 0.04), fn, eps=0.0018, rings=20, segs=56, mirror=True)

    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    drizzile(*args())
