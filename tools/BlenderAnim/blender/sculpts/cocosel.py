# Cocosel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cocosel.py -- <the export's Cocosel.glb> <out.glb>
# From the picture (10033-Cocosel.jpg, the project's own): a young rooster, still gangly: a big round russet head with
# a small red comb of three lobes, round cartoon eyes (white, a brown iris, a black pupil, a glint) under short dark
# brows, a small grey-tan beak; a russet neck and a chest held out proudly, the russet shading softly to green over
# the belly, the thighs and the back; folded wings russet above with a green feathered edge below; a short fan of
# green tail feathers raised behind; long thin yellow legs, three toes forward and one back with small claws.
#
# Written to be grown from (cocosor.py imports it with sibling("cocosel")): the line's frame is its functions (the
# body shaded to green, the folded wing, the tail fan, the comb, the beak, the legs and feet, the eyes).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, slab, outward, srgb, inside, smoothstep, unit, eye_look, polygon,
                       axes, args, sibling)

T = sibling("torchic")         # feather, spike, foot, pointed, ruff, on_ell, eye_open, lay_eyes

# --- the line's colours ----------------------------------------------------------------------------------------------
RUSSET = "#B5482A"
GREEN = "#3F9442"
DEEPGREEN = "#2B6E35"
COMB = "#E2403A"
BEAK = "#B9A88C"
SHANK = "#E6BE5A"
CLAW = "#9C7A3A"
BROW = "#5A2A1C"
INK = srgb("#1A1412")
WHITE = np.array([1.0, 1.0, 1.0])
SCLERA = srgb("#FBF6EE")
IRIS = srgb("#6A4226")


def soft(a, b, t):
    """A colour eased from a to b by t (0..1 an array)."""
    a, b = srgb(a) if isinstance(a, str) else a, srgb(b) if isinstance(b, str) else b
    return a[None] * (1 - t[:, None]) + b[None] * t[:, None]


# --- a folded wing along the side: a broad blade fused on, its lower edge a scallop of the second colour --------------
def wing(k, pts, radii, top, low, edge, z_at, scallop=0.012, period=0.035, f=0.4):
    k.add("wing", blade(pts, radii, (1, 0, 0), f), top, "wing{s}", mirror=True)
    def mask(P):
        y, z = P[:, 1], P[:, 2]
        zz = z_at(y) + scallop * np.abs(np.sin(y * math.pi / period))
        return inside(z - zz, 0.003)
    k.paint(mask, low, on=["wing"])
    if edge is not None:
        def mask2(P):
            return inside(P[:, 1] - edge[0], 0.004) * 0 + inside(edge[0] - P[:, 1], 0.004)
        k.paint(mask2, edge[1], on=["wing"])


# --- a fan of tail feathers from the rump, raised; across their faces so the back camera sees them -------------------
def tail_fan(k, base, feathers, thick, root=0.4):
    """feathers: (yaw out to the side in degrees, pitch up from straight back, length, width, colour) each."""
    for i, (yaw, pitch, ln, w, col) in enumerate(feathers):
        y, p = math.radians(yaw), math.radians(pitch)
        d = np.array([math.sin(y) * math.cos(p), math.cos(y) * math.cos(p), math.sin(p)])
        b = np.array(base, float) + np.array([math.sin(y) * 0.012, 0, 0])
        k.add(f"tailf{i}", T.feather(b, d, ln, w, thick=thick, root=root), col, "tail", fuse=False, surface="fur")


# --- the comb: lobes along the crown's midline, each a solid with width across the head ------------------------------
def comb(k, h, hr, lobes, color=COMB, bone="head"):
    """lobes: (along the head from its middle (+ back), height, half width along, half width across, lean back) each."""
    for i, (a, ht, wl, wx, lean) in enumerate(lobes):
        b = np.array([0.0, h[1] + a, h[2] + hr[2] * math.sqrt(max(0.0, 1 - (a / hr[1]) ** 2)) - 0.012])
        d = unit(0, lean, 1)
        k.add(f"comb{i}", T.spike(b, d, ht + 0.012, wl, wx, across=(0, 1, 0)), color, bone, fuse=False,
              surface="skin")


def round_comb(k, h, hr, lobes, color=COMB, bone="head"):
    """A young comb: round lobes fused along the crown's midline: (along (+ back), height, half width along) each."""
    for i, (a, ht, wl) in enumerate(lobes):
        top = h[2] + hr[2] * math.sqrt(max(0.0, 1 - (a / hr[1]) ** 2))
        k.add(f"comb{i}", ell((0.0, h[1] + a, top + ht * 0.35), (wl * 0.85, wl, ht * 0.75)), color, bone,
              surface="skin")


# --- long thin legs, three toes forward and one back -----------------------------------------------------------------
def legs(k, x, top, ankle, y, r, toe, toe_r, color=SHANK, claw=CLAW, claw_len=0.018):
    k.add("shin", tube([(x, y, top), (x + 0.003, y - 0.004, (top + ankle) / 2), (x + 0.005, y - 0.006, ankle)],
                       [r, r * 0.95, r * 0.92]), color, "leg{s}", mirror=True, surface="scales")
    T.foot(k, (x + 0.005, y - 0.008, ankle * 0.7), toe, toe_r, color, claws=claw, claw_len=claw_len, surface="scales")


# --- the eyes: round cartoon eyes on the head's sides toward the front --------------------------------------------------
def eye_open(sclera=SCLERA, iris=IRIS, ink=INK, iris_r=0.62, pupil_r=0.3, iris_c=(-0.14, -0.06), rim=0.86):
    def eye(u, v):
        r = np.hypot(u, v)
        c = np.tile(sclera, (len(u), 1))
        c[np.hypot(u - iris_c[0], v - iris_c[1]) < iris_r] = iris
        c[np.hypot(u - iris_c[0], v - iris_c[1] + 0.02) < pupil_r] = ink
        c[np.hypot(u - iris_c[0] - 0.14, v - iris_c[1] - 0.22) < 0.16] = WHITE
        c[r > rim] = ink
        return c
    return eye


def lay_eyes(k, state, E, de, half, look, lid, ink=INK, eps=0.003):
    T.lay_eyes(k, state, E, de, half, look, lid, ink, eps=eps)


def brows(k, E, de, half, dz, color=BROW, tilt=-10, size=(0.6, 0.18)):
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        a = math.radians(tilt * s)
        c = np.array(E, float) * m + np.array([0, 0.004, dz])
        k.decal(c, np.array(de) * m, (half[0] * size[0], half[1] * size[1]), color, up=unit(math.sin(a), 0, math.cos(a)),
                eps=0.002, rings=8, segs=36)


# --- Cocosel ----------------------------------------------------------------------------------------------------------
H = np.array([0.0, -0.085, 0.548])         # a big round head, a tenth over the picture's
HR = np.array([0.084, 0.08, 0.082])
DE = unit(0.55, -0.8, 0.16)
EYE_HALF = (0.024, 0.026)


def cocosel(src, out_path):
    k = Kit(src, coat="fur")
    # The body, tilted chest up, a proud chest, a neck into the big head
    k.add("body", ell((0, 0.02, 0.33), (0.1, 0.14, 0.105), rot=(-18, 0, 0)), RUSSET, "body")
    k.add("chest", ell((0, -0.075, 0.38), (0.082, 0.078, 0.09)), RUSSET, "body")
    k.add("neck", ell((0, -0.075, 0.46), (0.058, 0.058, 0.075)), RUSSET, "head")
    k.add("head", ell(H, HR), RUSSET, "head")
    # Green thighs fused under the body, feathers ending above the knee
    k.add("thigh", ell((0.045, 0.012, 0.255), (0.046, 0.058, 0.065)), GREEN, "leg{s}", mirror=True)
    # Russet shading softly into green over the belly, the thighs and the back
    def green(P):
        y, z = P[:, 1], P[:, 2]
        zg = 0.3 + 0.4 * np.clip(y + 0.06, -0.05, 0.3)
        return smoothstep(0.03, -0.03, z - zg)
    k.paint(green, GREEN, on=["body", "chest", "thigh"])
    k.paint(lambda P: smoothstep(0.03, 0.12, P[:, 1]) * (P[:, 2] < 0.42), GREEN, on=["body"])
    # Folded wings: russet above, a scalloped green edge below and green at the back
    wing(k, [(0.088, -0.06, 0.4), (0.105, -0.005, 0.37), (0.1, 0.06, 0.335), (0.07, 0.13, 0.3)],
         [0.035, 0.05, 0.042, 0.012], RUSSET, GREEN, None, lambda y: 0.345 + 0.35 * (y + 0.0))
    # A short fan of green tail feathers raised behind
    tail_fan(k, (0, 0.12, 0.37), [(0, 64, 0.17, 0.056, GREEN), (16, 56, 0.155, 0.05, DEEPGREEN),
                                  (-16, 56, 0.155, 0.05, DEEPGREEN), (30, 44, 0.135, 0.046, GREEN),
                                  (-30, 44, 0.135, 0.046, GREEN), (42, 30, 0.11, 0.04, DEEPGREEN),
                                  (-42, 30, 0.11, 0.04, DEEPGREEN)], thick=0.02)
    # The small comb of three lobes and the beak
    round_comb(k, H, HR, [(-0.034, 0.026, 0.016), (-0.006, 0.036, 0.019), (0.024, 0.028, 0.017)])
    k.add("beak", ell((0, -0.163, 0.53), (0.026, 0.04, 0.018), rot=(6, 0, 0), deform=T.pointed), BEAK, "head",
          fuse=False, surface="shell")
    # Long thin yellow legs
    legs(k, 0.045, 0.22, 0.03, 0.0, 0.0125, 0.058, 0.0095)

    E = T.on_ell(H, HR, DE)
    brows(k, E, DE, EYE_HALF, 0.034)
    k.build(voxel=0.0055, smooth=4, band=0.005, wband=0.04)
    k.eye_states(lambda s: lay_eyes(k, s, E, DE, EYE_HALF, eye_open(), srgb(RUSSET)))
    k.export(out_path)


if __name__ == "__main__":
    cocosel(*args())
