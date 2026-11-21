# Blitzle sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/blitzle.py -- <the export's Blitzle.glb> <out.glb>
# From the references: a dark grey zebra foal with a big head on a short thick neck; a big round black nose at the
# end of a tapering muzzle; large round eyes, a yellow ring round a blue iris and a dark pupil; small round ears,
# dark, blue within; a tall white lightning bolt standing from the crown, jagged on its back edge, and a row of white
# spikes of mane down the back of the head; white jagged marks: a collar where the neck meets the chest, a bolt along
# each shoulder and a slash across the rump; each leg grey at the top with a white band at the elbow or stifle and a
# white sock, black hooves; a short white spiky tuft of a tail.
#
# Written to be grown from (zebstrika.py imports it with sibling("blitzle")): the line's colours, its legs, hooves,
# spikes, painted marks and its eye are the functions below; the calls that build Blitzle are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, slab, lathe, srgb, inside, smoothstep, unit, eye_look, args, sibling)

el = sibling("electrike")             # poly_mask: a polygon painted with a crisp edge

# --- the line's colours -------------------------------------------------------------------------------------------
GREY, WHITE, MANE = "#55575E", "#F4F4F2", "#E9EAEE"
HOOF, NOSE = "#1C1C21", "#1E1E23"
EAR_RIM, EAR_IN = "#2A2B31", "#3AA6E2"
EYE_YELLOW, EYE_BLUE = srgb("#F6D23A"), srgb("#2E9BE0")
INK, GLINT = srgb("#121418"), np.array([1.0, 1.0, 1.0])
LINE = srgb("#C9CBD2")                # a closed eye's line: light, on a body dark all round


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre."""
    d = unit(*d)
    t = 1.0 / math.sqrt(((d / np.array(r)) ** 2).sum())
    return np.array(c, float) + d * t


def spike(a, b, wx, wy, up=(0, 0, 1)):
    """A solid spike from a (a kite wx by wy at its foot) to the point b: a flat-faced hull."""
    return el.spike(a, b, wx, wy, up)


def leg(k, name, x, y, top, knee, r, bone, hoof_r=None, color=GREY, hoof=HOOF):
    """A leg: a broad mass fused over its upper part (shoulder or thigh), a short tapering shin, a black hoof."""
    rt, rk, rb = r
    k.add(name, tube([(x, y, top), (x + 0.001, y + knee, top * 0.5), (x + 0.001, y, 0.03)], [rt, rk, rb]), color,
          bone, mirror=True)
    hr = hoof_r or rb * 1.12
    k.add(name + "hoof", lathe((x + 0.001, y - 0.002, 0.0), (0, 0, 1),
                               [(0, 0), (hr, 0), (hr * 1.04, hr * 0.3), (hr * 0.86, hr * 1.2), (0, hr * 1.2)]),
          hoof, bone, fuse=False, mirror=True, surface="shell")


def leg_mask(legs, z0, z1, reach, edge=0.002):
    """Painted white round the legs between two heights: legs is [(x, y), ...] of each leg's axis."""
    def mask(P):
        ax = np.abs(P[:, 0])
        d = np.min([np.hypot(ax - x, P[:, 1] - y) for x, y in legs], axis=0)
        return inside(d - reach, edge) * inside(P[:, 2] - z1, edge) * inside(z0 - P[:, 2], edge)
    return mask


def side_mark(pts, xmin=0.0, edge=0.0025):
    """A white mark from a polygon in (y, z), painted through the body on both sides where |x| is over xmin."""
    def mask(P):
        return el.poly_mask(P[:, 1], P[:, 2], pts, edge) * inside(xmin - np.abs(P[:, 0]), 0.004)
    return mask


def collar(A, B, s0, s1, amp, teeth, reach, edge=0.0025):
    """A jagged band round a neck from A to B: between s0 and s1 along it, the edges zigzagging by amp."""
    A, B = np.array(A, float), np.array(B, float)
    a = (B - A) / np.linalg.norm(B - A)
    e1 = np.array([1.0, 0, 0]); e2 = np.cross(a, e1)
    def mask(P):
        Q = P - A
        s = Q @ a
        R = Q - np.outer(s, a)
        ang = np.arctan2(R @ e2, R @ e1)
        z = amp * (1 - 2 * np.abs(((ang / (2 * math.pi) * teeth) % 1) - 0.5) * 2)
        return (inside(s0 + z - s, edge) * inside(s - s1 - z * 0.7, edge)
                * inside(np.linalg.norm(R, axis=1) - reach, 0.004))
    return mask


def round_eye(u, v):
    """The open eye: a dark rim, a yellow ring, a blue iris, a dark pupil and a glint (u outward, v up)."""
    r = np.sqrt(u * u + v * v)
    c = np.tile(INK, (len(u), 1))
    c[r < 0.84] = EYE_YELLOW
    c[r < 0.58] = EYE_BLUE
    c[np.hypot(u + 0.04, v + 0.02) < 0.27] = INK
    c[np.hypot(u - 0.16, v - 0.26) < 0.16] = GLINT
    return c


def blitzle(src, out):
    k = Kit(src, coat="fur")

    # --- the trunk: a deep barrel, the chest and rump round masses ----------------------------------------------
    k.add("barrel", ell((0, 0.005, 0.228), (0.064, 0.13, 0.063)), GREY, "body")
    k.add("chest", ell((0, -0.078, 0.232), (0.062, 0.068, 0.068)), GREY, "body")
    k.add("rump", ell((0, 0.088, 0.232), (0.067, 0.072, 0.067)), GREY, "body")

    # --- legs: shoulders and thighs fused over the upper half, thick shins, black hooves ------------------------
    k.add("shoulder", ell((0.04, -0.07, 0.18), (0.036, 0.048, 0.066)), GREY, "leg{s}", mirror=True)
    k.add("thigh", ell((0.04, 0.088, 0.186), (0.039, 0.06, 0.074)), GREY, "hind{s}", mirror=True)
    leg(k, "fore", 0.041, -0.07, 0.17, -0.003, (0.026, 0.0205, 0.019), "leg{s}")
    leg(k, "hind", 0.042, 0.09, 0.17, 0.006, (0.028, 0.0205, 0.019), "hind{s}")
    LEGS = [(0.042, -0.07), (0.043, 0.09)]
    k.paint(leg_mask(LEGS, 0.012, 0.066, 0.036), WHITE)
    k.paint(leg_mask(LEGS, 0.104, 0.136, 0.036), WHITE)

    # --- a short thick neck to a big head; a tapering muzzle with a big round black nose -------------------------
    k.add("neck", tube([(0, -0.07, 0.232), (0, -0.098, 0.3), (0, -0.112, 0.34)], [0.058, 0.05, 0.046]), GREY, "head")
    H, HR = np.array([0, -0.125, 0.362]), np.array([0.064, 0.074, 0.062])
    k.add("head", ell(H, HR), GREY, "head")
    k.add("muzzle", ell((0, -0.196, 0.34), (0.044, 0.058, 0.041), rot=(10, 0, 0)), GREY, "head")
    k.add("nose", ell((0, -0.236, 0.335), (0.031, 0.026, 0.029)), NOSE, "head", fuse=False, surface="skin")

    # --- round ears on top, a dark rim and blue within --------------------------------------------------------
    EC = np.array([0.046, -0.104, 0.43])
    k.add("ear", ell(EC, (0.023, 0.011, 0.028), rot=(0, 12, 20)), GREY, "ear{s}", mirror=True)
    en = unit(0.34, -0.94, 0.05)
    k.decal(EC + en * 0.0105, en, (0.017, 0.022), EAR_RIM, up=unit(-0.2, 0, 1), eps=0.003, rings=8, segs=40,
            mirror=True)
    k.decal(EC + en * 0.0105, en, (0.0105, 0.014), EAR_IN, up=unit(-0.2, 0, 1), eps=0.0045, rings=8, segs=40,
            mirror=True)

    # --- the bolt: a tall white lightning standing from the crown, jagged on its back edge; its plane turned 55
    #     degrees out to +X so that the game's cameras see a face of it ------------------------------------------
    BOLT = [(-0.024, 0.0), (-0.04, 0.16), (-0.012, 0.112), (0.03, 0.128), (0.006, 0.066), (0.046, 0.088),
            (0.022, 0.028), (0.038, 0.0)]
    ex = unit(0.8, 0.6, 0.0)
    k.add("bolt", slab(BOLT, (0.0, -0.118, 0.39), ex, (0, 0, 1), 0.034), WHITE, "head", fuse=False, surface="hair")
    # the mane: white spikes down the back of the head and the nape, leaning back
    for i, (y, z, L) in enumerate([(-0.088, 0.415, 0.06), (-0.066, 0.402, 0.058), (-0.05, 0.38, 0.052),
                                   (-0.04, 0.352, 0.046), (-0.036, 0.322, 0.038)]):
        sx = 0.008 if i % 2 else -0.008
        k.add(f"mane{i}", spike((sx, y, z - 0.012), (sx * 2, y + 0.034, z + L * 0.85), 0.017, 0.013, up=(1, 0, 0)),
              MANE, "head", fuse=False, surface="hair")

    # --- the tail: a short tuft of white spikes from the rump ------------------------------------------------
    for i, d in enumerate([(0, 0.7, 1.0), (0.65, 0.8, 0.8), (-0.65, 0.8, 0.8), (0, 1, 0.45)]):
        base = np.array((0, 0.148, 0.262))
        k.add(f"tail{i}", spike(base, base + unit(*d) * (0.075 if i == 0 else 0.06), 0.022, 0.018, up=(1, 0, 0)),
              MANE, "tail", fuse=False, surface="hair")

    # --- the white marks: a jagged collar at the base of the neck, a bolt on each shoulder, a slash over the rump --
    k.paint(collar((0, -0.07, 0.232), (0, -0.112, 0.34), 0.012, 0.042, 0.009, 5, 0.08), WHITE)
    k.paint(side_mark([(-0.02, 0.246), (-0.056, 0.238), (-0.05, 0.25), (-0.1, 0.226), (-0.062, 0.222),
                       (-0.067, 0.208), (-0.016, 0.226)], xmin=0.035), WHITE)
    k.paint(side_mark([(0.036, 0.3), (0.074, 0.3), (0.06, 0.258), (0.092, 0.252), (0.058, 0.188), (0.066, 0.238),
                       (0.038, 0.244)]), WHITE)

    # --- the eyes: large and round, on the sides of the head, turned a little forward --------------------------
    de = unit(0.72, -0.6, 0.22)
    E = on_ell(H, HR, de)
    G = srgb(GREY)
    def eyes(state):
        k.decal(E, de, (0.025, 0.029), eye_look(state, round_eye, G, LINE, line=0.16), up=unit(0, 0.15, 1),
                eps=0.006, rings=20, segs=56, mirror=True)

    k.build(voxel=0.0042, smooth=4)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    blitzle(*args())
