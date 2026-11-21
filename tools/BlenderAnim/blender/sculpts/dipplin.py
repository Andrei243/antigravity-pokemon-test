# Dipplin sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dipplin.py -- <the export's Dipplin.glb> <out.glb>
#
# Grown from Applin's script (sibling("applin")): its apple's shape (apple), on_ell, skin_coords, its lobe and the way
# an eye sits on a lobe's face. From the references: a candy apple. The apple is glossy red, wider at its shoulders,
# its upper half under a coat of brighter syrup whose lower edge hangs in round-ended drips of different lengths, a fat
# drop at the end of each; the apple stands in a flat puddle of the syrup with a lobed edge. Out of its top a green
# collar and a tall thin stalk, green at its foot and pale yellow-green toward its point, leaning a little to the left
# (-X). Either side of the stalk a pale yellow lobe, leaning out, each with one small black eye glinting white. Out of
# the apple's lower left (-X) side the worm's short tail, green above and cream below, two dark green leaves at its tip.
# (The task gave it "one small worm's head at its top": the pictures show no head but the two lobes, an eye on each, as
# Applin's; and the stalk is the picture's tall stalk, shortened: it stands 1.8 apple-heights over the apple in the
# pictures and 1.05 here, so that the apple stays the model's largest mass in its cell.)
# Written to be grown from (hydrapple.py takes candy_apple, worm_tail and the colours with sibling("dipplin")).
# The skeleton: body (0, 0, 0.13), head (0, 0, 0.22), ears (+-0.02, -0.01, 0.25), tail (-0.1, 0.04, 0.08).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, outward, sibling

a = sibling("applin")

# --- the line's colours -------------------------------------------------------------------------------------------
GLAZE, RED_DEEP, PUDDLE = "#F2483A", "#A81A24", "#E8392F"
STALK, STALK_TIP, COLLAR = "#5E9A3C", "#D6E07A", "#4E8A34"
LOBE = "#F0E48C"
TAIL, TAIL_BELLY, LEAF = "#6E9E3E", "#E2D7A0", "#3E7A34"
INK, GLINT = "#16160F", "#FFFFFF"

# --- Dipplin's frame (about 0.5 tall) -----------------------------------------------------------------------------
APPLE_C = np.array([0.0, 0.0, 0.128])
APPLE_R = np.array([0.13, 0.125, 0.118])
PUDDLE_C, PUDDLE_R = np.array([0.0, -0.004, 0.012]), np.array([0.168, 0.155, 0.013])
GLAZE_BASE = 0.26                    # the syrup's edge between drips (in the apple's height, -1 foot .. 1 top)
DRIPS = [(-2.5, 0.24, 0.32), (-1.75, 0.2, 0.5), (-1.05, 0.26, 0.26), (-0.42, 0.22, 0.46), (0.25, 0.24, 0.3),
         (0.9, 0.22, 0.58), (1.6, 0.26, 0.3), (2.3, 0.22, 0.48), (3.0, 0.2, 0.26)]   # (angle, half width, length)
NECK = np.array([0.0, 0.002, 0.236])
STALK_PTS = [(0.0, 0.004, 0.22), (-0.004, 0.006, 0.3), (-0.014, 0.008, 0.39), (-0.028, 0.008, 0.47)]
STALK_R = [0.0125, 0.0105, 0.0088, 0.0076]
LOBE_TILT, LOBE_YAW = 34.0, 14.0
LOBE_HALF = np.array([0.034, 0.0135, 0.05])
LOBE_REACH = 0.05
EYE_HALF = (0.0095, 0.0145)
TAIL_PTS = [(-0.075, 0.03, 0.08), (-0.15, 0.036, 0.06), (-0.205, 0.024, 0.068), (-0.232, 0.006, 0.092)]
TAIL_R = [0.025, 0.021, 0.017, 0.0135]
VOXEL = 0.0038


def wrap(d):
    return (d + math.pi) % (2 * math.pi) - math.pi


def glaze_edge(th, base, drips):
    """The syrup's lower edge as a height for each angle: the base, hanging lower in round-ended drips."""
    e = np.full_like(th, base)
    for t0, w, L in drips:
        x = wrap(th - t0) / w
        e = np.minimum(e, np.where(np.abs(x) < 1, base - L + w * 0.9 * (1 - np.sqrt(np.clip(1 - x * x, 0, 1))), base))
    return e


def candy_shape(V):
    """Applin's apple, its lower half drawn in more: broad shoulders over a narrower foot, the foot a little flat."""
    V = a.apple(V)
    z = V[:, 2]
    s = 0.9 + 0.1 * np.clip(z + 0.2, -1, 1) - 0.06 * np.clip(-z, 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.where(z < -0.75, -0.75 + (z + 0.75) * 0.5, z)
    return V


def candy_apple(k, c, r, puddle_c, puddle_r, base, drips, bone="body", glaze=GLAZE, deep=RED_DEEP, puddle=PUDDLE,
                bulb=None):
    """The apple under its syrup: the apple fused, its upper half painted the syrup's brighter red down to the drips,
    a fat drop at each drip's end, and the flat puddle with a lobed edge it stands in."""
    c, r = np.array(c, float), np.array(r, float)
    k.add("apple", ell(c, r, deform=candy_shape, sub=5), deep, bone, surface="shell")
    def coat(P):
        th, h, nr = a.skin_coords(P, c, r)
        return inside(glaze_edge(th, base, drips) - h, 0.03) * (nr > 0.6) * (nr < 1.35)
    k.paint(coat, glaze, on=["apple", "drop"])
    for i, (t0, w, L) in enumerate(drips if bulb else []):
        h = base - L + 0.02
        d = np.array([math.sin(t0) * math.sqrt(1 - h * h), -math.cos(t0) * math.sqrt(1 - h * h), h])
        p, n = a.on_ell(c, r, d)
        k.add(f"drop{i}", ell(p - n * bulb * 0.6, (bulb, bulb, bulb * 1.2)), glaze, bone, surface="shell")
    def lobed(V):
        V = V.copy()
        th = np.arctan2(V[:, 1], V[:, 0])
        s = 1 + 0.08 * np.sin(5 * th + 0.7) + 0.045 * np.sin(9 * th + 2.0)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("puddle", ell(puddle_c, puddle_r, deform=lobed), puddle, "root" if "root" in k.bones else bone,
          surface="shell")


def worm_tail(k, pts, radii, bone="tail", leaf_len=0.05, leaf_w=0.016, green=TAIL, belly=TAIL_BELLY, leaf=LEAF):
    """The worm's tail out of the apple: green above, cream under it, two dark green leaves at its tip."""
    k.add("tail", tube(pts, radii), green, bone, surface="scales")
    P = np.array(pts, float)
    # the centre line drawn fine, with its radius, so the belly's edge is smooth
    t = np.linspace(0, len(P) - 1, 60)
    CL = np.stack([np.interp(t, np.arange(len(P)), P[:, j]) for j in range(3)], axis=1)
    LR = np.interp(t, np.arange(len(P)), np.array(radii, float))
    def under(Q):
        # by the centre line: the cream below it
        d = ((Q[:, None, :] - CL[None]) ** 2).sum(2)
        i = d.argmin(1)
        return inside((Q[:, 2] - CL[i, 2]) + 0.2 * LR[i], 0.005)
    k.paint(under, belly, on=["tail"])
    tip = P[-1]
    d = unit(*(P[-1] - P[-2]))
    for j, dd in enumerate([unit(d[0] * 0.45, d[1] * 0.45 + 0.2, 1.0), unit(d[0] * 1.1, d[1] * 1.1 - 0.4, 0.8)]):
        base = tip + d * radii[-1] * 0.5
        L = leaf_len * (1.0 if j == 0 else 0.82)
        side = np.cross(dd, (0, 0, 1.0))
        if np.linalg.norm(side) < 1e-3:
            side = np.array([1.0, 0, 0])
        side = side / np.linalg.norm(side)
        face = np.cross(side, dd)
        def shape(V):
            V = V.copy()
            t = (V[:, 2] + 1) / 2
            w = np.sqrt(np.clip(t, 0, 1)) * (1 - t ** 1.6) * 1.6
            V[:, 0] *= w; V[:, 1] *= w
            return V
        V, F = ell((0, 0, 0), (leaf_w, leaf_w * 0.38, L / 2), deform=shape, sub=3)
        V = V @ np.stack([side, face, dd], axis=1).T + base + dd * L / 2
        k.add(f"leaf{j}", outward((V, F)), leaf, bone, fuse=False, surface="leaf")


def stalk(k, pts, radii, bone="head"):
    k.add("stalk", tube(pts[:-1] + [tuple(np.array(pts[-1]) - unit(*(np.array(pts[-1]) - np.array(pts[-2]))) * 0.008)],
                        radii), STALK, bone, surface="leaf")
    P = np.array(pts, float)
    d = unit(*(P[-1] - P[-2]))
    tipbase = P[-1] - d * 0.01
    from sculptkit import lathe
    k.add("stalktip", lathe(tipbase, d, [(0, 0), (radii[-1] * 1.02, 0), (0, 0.04)], seg=24), STALK_TIP, bone,
          fuse=False, surface="leaf")
    z0, z1 = P[0, 2], P[-1, 2]
    k.paint(lambda Q: np.clip((Q[:, 2] - (z0 + 0.35 * (z1 - z0))) / (0.6 * (z1 - z0)), 0, 1), STALK_TIP, on=["stalk"])


def eye_open(u, v):
    c = np.tile(srgb(INK), (len(u), 1))
    c[((u - 0.28) / 0.3) ** 2 + ((v - 0.38) / 0.24) ** 2 < 1] = srgb(GLINT)
    return c


def lay_eyes(k, state, centre, m, half, local=(-0.12, -0.18), eye_half=None):
    hx, hy, hz = half
    p = centre + m @ np.array([local[0] * hx, -hy * 1.02, local[1] * hz])
    n = m @ np.array([0, -1.0, 0])
    up = m @ np.array([0, 0, 1.0])
    k.decal(p, n, eye_half or EYE_HALF, eye_look(state, eye_open, srgb(LOBE), srgb(INK), line=0.2), up=up, eps=0.0016,
            rings=20, segs=56, mirror=True)


def dipplin(src, out_path):
    k = Kit(src, coat="leaf")
    candy_apple(k, APPLE_C, APPLE_R, PUDDLE_C, PUDDLE_R, GLAZE_BASE, DRIPS)
    k.add("collar", ell(NECK - (0, 0, 0.006), (0.032, 0.03, 0.013)), COLLAR, "head", surface="leaf")
    stalk(k, STALK_PTS, STALK_R)
    centre, m = a.lobe(k, NECK + (0.02, 0, 0.0), LOBE_TILT, LOBE_YAW, LOBE_HALF, LOBE_REACH, LOBE, "ear{s}")
    worm_tail(k, TAIL_PTS, TAIL_R)
    k.build(voxel=VOXEL, smooth=4, band=0.004, wband=0.03)
    k.eye_states(lambda state: lay_eyes(k, state, centre, m, LOBE_HALF))
    k.export(out_path)


if __name__ == "__main__":
    src, out_path = args()
    dipplin(src, out_path)
