# Lupling (Carpathia No. 10055) sculpted from nothing on the skeleton and clips of the game's own export of it:
#   blender --background --factory-startup --python sculpts/lupling.py -- <the export's Lupling.glb> <out.glb>
# From its picture (docs/art/reference/gemini/carpathia/10055-Lupling.jpg) and its brief (CP-dex-2-mountains.md, 55):
# a grey wolf pup howling, its muzzle raised and its mouth open; a pale mask over the muzzle, the cheeks and the
# throat, the grey crown and brow cutting the top of amber eyes; a black nose; oversized pointed ears flung up and
# out, grey outside, pale inside, their tips dark; a messy tuft of pale and dark fur falling over the forehead; a dark
# saddle over the nape, the shoulders and the back with a ragged lower edge, the flanks and the legs grey, the belly
# a paler lavender grey; big soft paws; a bushy tail curling up behind, a dark band across it and a pale tip.
#
# Written to be grown from (lupodac.py imports it with sibling("lupling")): the spikes, the ears, the eyes, the tail's
# bands and the colours are the functions and constants below; the calls that build Lupling are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, outward, srgb, inside, smoothstep, tri, unit, eye_look, polygon,
                       args)

GREY, DARK, PALE, BELLY = "#8F8F96", "#3C3C44", "#E4E4E8", "#B4B2C0"
NOSE, MOUTH, TONGUE = "#1E1E24", "#4A1E26", "#C46A7A"
AMBER, PUPIL, INK = "#D08A2E", "#1A1210", "#141418"


# --- shared by the line ----------------------------------------------------------------------------------------------
def on_ell(c, r, d):
    """The point of an ellipsoid's surface along a direction from its middle."""
    d = unit(*d)
    return np.array(c, float) + d / np.linalg.norm(d / np.array(r, float))


def spike(base, d, length, w, t, side=None):
    """A solid tapering spike (a tuft of fur, a fang, a claw): a flat-faced hull, w wide and t thick at its root."""
    base = np.array(base, float)
    d = unit(*d)
    if side is None:
        side = np.cross(d, (0, 0, 1.0))
        if np.linalg.norm(side) < 1e-3:
            side = np.array([1.0, 0, 0])
    side = np.array(side, float); side = side - d * (side @ d); side /= np.linalg.norm(side)
    face = np.cross(d, side)
    pts = [base + side * w, base - side * w, base + face * t, base - face * t,
           base + d * length * 0.45 + side * w * 0.6, base + d * length * 0.45 - side * w * 0.6,
           base + d * length]
    return hull(pts)


def along(P, pts):
    """For points P, how far along a polyline (0 at its start, 1 at its end) the nearest point of it lies, and how far
    from it they are."""
    pts = np.array(pts, float)
    seg = np.vstack([pts[i] + (pts[i + 1] - pts[i]) * t for i in range(len(pts) - 1)
                     for t in np.linspace(0, 1, 30, endpoint=False)] + [pts[-1:]])
    L = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(seg, axis=0), axis=1))])
    best = np.zeros(len(P), int); dist = np.full(len(P), 9.0)
    for i in range(0, len(seg), 1):
        d = np.linalg.norm(P - seg[i], axis=1)
        m = d < dist
        dist[m] = d[m]; best[m] = i
    return L[best] / L[-1], dist


def ear(k, base, tip, root_r, facing, outer, lining, tipcol, bone="ear{s}", tip_from=0.7, lining_w=0.62):
    """A big pointed ear, fused: a blade flat to its facing, the outer colour; a pale lining painted on its front face
    and a dark tip, crisp (no on), on both sides."""
    base, tip = np.array(base, float), np.array(tip, float)
    mid = base + (tip - base) * 0.5 + np.array(facing, float) * 0.004
    k.add("ear", blade([base, mid, tip], [root_r, root_r * 0.66, root_r * 0.08], facing, 0.38), outer, bone,
          mirror=True)
    d = tip - base; L = np.linalg.norm(d); d /= L
    f = unit(*facing); f = f - d * (f @ d); f /= np.linalg.norm(f)

    def frame(P, sx):
        m = np.array([sx, 1, 1.0])
        q = P - base * m
        dd, ff = d * m, f * m
        t = q @ dd
        n = q @ ff
        across = q - np.outer(t, dd) - np.outer(n, ff)
        return t / L, n, np.linalg.norm(across, axis=1), np.sign(P[:, 0]) == sx

    def lin(P):
        m = np.zeros(len(P))
        for sx in (1, -1):
            t, n, a, side = frame(P, sx)
            w = root_r * lining_w * (1 - np.clip(t / 0.92, 0, 1)) ** 0.8
            m = np.maximum(m, inside(a - w, 0.0025) * (t > 0.06) * (n < 0.001) * (np.abs(n) < root_r) * side)
        return m

    def tipm(P):
        m = np.zeros(len(P))
        for sx in (1, -1):
            t, n, a, side = frame(P, sx)
            m = np.maximum(m, inside(tip_from - t, 0.004) * inside(a - root_r * 1.4, 0.004) * (t < 1.3) * side)
        return m
    k.paint(lin, lining)
    k.paint(tipm, tipcol)


def clipped(top, n=96):
    """The unit circle cut off above v = top(u): an eye's outline under a brow."""
    pts = []
    for th in np.linspace(0, 2 * math.pi, n, endpoint=False):
        du, dv = math.cos(th), math.sin(th)
        t = 1.0
        if dv > top(du):
            lo, hi = 0.0, 1.0
            for _ in range(40):
                t = (lo + hi) / 2
                if t * dv > top(t * du):
                    hi = t
                else:
                    lo = t
            t = lo
        pts.append((t * math.cos(th), t * math.sin(th)))
    return pts


def eye_open(iris, top, pupil=PUPIL, iris_c=(-0.08, -0.06), pupil_r=0.42, rim=0.86):
    """An amber eye with a black pupil toward the nose, a white glint up and out, a dark rim and the brow's dark edge
    on top."""
    I, P, K, W = srgb(iris), srgb(pupil), srgb(INK), np.array([1.0, 1.0, 1.0])

    def look(u, v):
        c = np.tile(I, (len(u), 1))
        c[np.hypot(u, v) > rim] = K
        c[np.hypot((u - iris_c[0]) / 1.0, (v - iris_c[1]) / 1.15) < pupil_r] = P
        c[((u - iris_c[0] - 0.2) / 0.17) ** 2 + ((v - iris_c[1] - 0.24) / 0.15) ** 2 < 1] = W
        c[v > top(u) - 0.11] = K
        return c
    return look


def lay_eyes(k, state, E, n, half, look, outline, lid, line_col, up=(0, 0, 1)):
    """Both eyes laid by hand, a side each, u outward on both."""
    f = eye_look(state, look, srgb(lid), srgb(line_col), line=0.15)
    for side in (1, -1):
        m = np.array([side, 1, 1.0])
        out_ = polygon([(side * a, b) for a, b in outline]) if state in ("Open", "Fierce") else None
        col = f if side == 1 else (lambda a, b, g=f: g(-a, b))
        k.decal(np.array(E) * m, np.array(n) * m, half, col, up=np.array(up, float) * m, eps=0.0035, outline=out_,
                rings=20, segs=56)


def banded_tail(k, pts, radii, colours, bands, tips, tip_len, w, t, bone="tail"):
    """A bushy tail: a fused tube with tufts at its end; bands: (from, to, colour) along it (0 the root, 1 the tip),
    painted crisp on the tail alone."""
    k.add("tail", tube(pts, radii, seg=20), colours, bone)
    tip = np.array(pts[-1], float)
    for i, (d, ln, col) in enumerate(tips):
        k.add(f"tailtuft{i}", spike(tip - unit(*d) * 0.015 * ln / tip_len, d, ln, w, t), col, bone, fuse=False,
              surface="fur")
    for a, b, col in bands:
        def mask(P, a=a, b=b):
            s, dist = along(P, pts)
            near = dist < max(radii) * 1.6
            return inside(a - s, 0.006) * inside(s - b, 0.006) * near
        k.paint(mask, col, on=["tail"])


# --- Lupling ----------------------------------------------------------------------------------------------------------
LX = 0.05                       # the legs' x
FY, HY = -0.075, 0.09           # forelegs' and hind legs' y
BZ = 0.2                        # the trunk's middle
HC = np.array([0.0, -0.118, 0.335])      # the head's middle (its bone is at 0, -0.1, 0.3)
HR = np.array([0.078, 0.072, 0.071])
BROW = (0.5, 0.12)              # the eye's top: v = BROW[0] + BROW[1] u, nearly level (a pup's calm brow)


def eye_top(u):
    return BROW[0] + BROW[1] * u


def lupling(src, out_path):
    k = Kit(src, coat="fur")

    # --- a short round trunk, the belly low; shoulders and thighs fused over the legs --------------------------
    k.add("chest", ell((0, -0.052, BZ + 0.008), (0.062, 0.066, 0.074)), GREY, "body")
    k.add("middle", ell((0, 0.02, BZ - 0.002), (0.057, 0.115, 0.064)), GREY, "body")
    k.add("rump", ell((0, 0.085, BZ + 0.004), (0.06, 0.062, 0.068)), GREY, "body")
    k.add("neck", tube([(0, -0.068, 0.24), (0, -0.092, 0.285), (0, -0.104, 0.31)], [0.053, 0.05, 0.048]), GREY, "head")

    k.add("shoulder", ell((LX - 0.006, FY + 0.006, 0.15), (0.036, 0.043, 0.062)), GREY, "leg{s}", mirror=True)
    k.add("leg", tube([(LX, FY, 0.15), (LX + 0.003, FY - 0.003, 0.08), (LX + 0.004, FY - 0.006, 0.03)],
                      [0.032, 0.028, 0.027]), GREY, "leg{s}", mirror=True)
    k.add("paw", ell((LX + 0.004, FY - 0.019, 0.02), (0.031, 0.039, 0.021)), GREY, "leg{s}", mirror=True)
    k.add("thigh", ell((LX - 0.004, HY + 0.006, 0.155), (0.042, 0.056, 0.066)), GREY, "hind{s}", mirror=True)
    k.add("hind", tube([(LX, HY + 0.01, 0.14), (LX + 0.003, HY + 0.016, 0.08), (LX + 0.004, HY + 0.006, 0.03)],
                       [0.032, 0.028, 0.027]), GREY, "hind{s}", mirror=True)
    k.add("hpaw", ell((LX + 0.004, HY - 0.012, 0.02), (0.031, 0.039, 0.021)), GREY, "hind{s}", mirror=True)

    # --- the head, tilted for the howl: the muzzle raised, the jaw dropped under it ------------------------------
    k.add("head", ell(HC, HR), GREY, "head")
    k.add("cheek", ell(HC + (0.036, -0.03, -0.024), (0.034, 0.036, 0.034)), PALE, "head", mirror=True)
    MD = unit(0, -0.74, 0.67)                        # the muzzle's way: forward and 42 degrees up
    MC = HC + np.array((0, -0.046, -0.006)) + MD * 0.036
    k.add("muzzle", ell(MC, (0.034, 0.054, 0.031), rot=(-42, 0, 0)), PALE, "head")
    k.add("jaw", ell(HC + (0, -0.072, -0.04), (0.03, 0.045, 0.021), rot=(-12, 0, 0)), PALE, "head")
    k.add("nose", outward(ell(MC + MD * 0.052 + (0, 0, 0.004), (0.017, 0.014, 0.014), rot=(-42, 0, 0))), NOSE,
          "head", fuse=False, surface="skin")

    # the pale mask: the muzzle, the cheeks and the lower face up to the eyes, the throat down to the chest
    def mask(P):
        d = P - HC
        face = inside(d[:, 2] - (0.006 + 0.42 * np.clip(-d[:, 1] - 0.03, 0, None)), 0.003) * (d[:, 1] < 0.02)
        near = inside(np.linalg.norm((P - HC) / (HR * 1.6), axis=1) - 1, 0.01)
        throat = (inside(P[:, 1] - (-0.098 - 0.55 * np.clip(0.29 - P[:, 2], 0, None) + 0.012 * tri(P[:, 2] * 30)), 0.003)
                  * (P[:, 2] > 0.19) * inside(np.abs(P[:, 0]) - 0.04, 0.006))
        return np.maximum(face * near, throat * (P[:, 2] < HC[2]))
    k.paint(mask, PALE, on=["head", "cheek", "muzzle", "jaw", "neck", "chest"])
    # the belly a paler lavender grey
    k.paint(lambda P: inside(P[:, 2] - (BZ - 0.035), 0.006) * inside(np.abs(P[:, 0]) - 0.05, 0.01)
            * (P[:, 1] > -0.085) * (P[:, 1] < 0.12), BELLY, on=["chest", "middle", "rump"])
    # the dark saddle over the nape, the shoulders and the back, its lower edge ragged
    def saddle(P):
        y = P[:, 1]
        line = BZ + 0.028 + 0.012 * tri(y * 22) + 0.4 * np.clip(y - 0.05, 0, None)
        back = inside(line - P[:, 2], 0.003) * (y > -0.09) * (y < 0.165)
        nape = inside(-(P[:, 1] - (-0.072 + 0.45 * (P[:, 2] - 0.25) + 0.01 * tri(P[:, 2] * 32))), 0.003) * (P[:, 2] > 0.22) * (P[:, 2] < 0.31) \
            * (P[:, 1] > -0.1)
        return np.maximum(back, nape)
    k.paint(saddle, DARK, on=["chest", "middle", "rump", "neck", "shoulder", "thigh"])

    # --- oversized ears, up and out and a little back -----------------------------------------------------------
    ear(k, (0.042, -0.108, 0.378), (0.104, -0.086, 0.488), 0.044, unit(0.3, 1, 0.12), GREY, PALE, DARK, lining_w=0.55)

    # --- the tuft of fur over the forehead, pale and dark, falling forward --------------------------------------
    for i, (x, d, ln, col) in enumerate(((0.0, (0, -0.75, 0.55), 0.058, PALE), (0.018, (0.35, -0.7, 0.5), 0.05, PALE),
                                         (-0.018, (-0.35, -0.7, 0.5), 0.05, PALE), (0.01, (0.15, -0.5, 0.8), 0.045, PALE),
                                         (-0.01, (-0.2, -0.45, 0.85), 0.044, DARK))):
        k.add(f"fringe{i}", spike(HC + (x * 1.3, -0.034, 0.052), d, ln * 1.25, 0.022, 0.014), col, "head", fuse=False, surface="fur")

    # --- the bushy tail curling up behind, out to the model's right ---------------------------------------------
    TAIL = [(0, 0.135, 0.225), (0.02, 0.185, 0.24), (0.048, 0.228, 0.272), (0.066, 0.25, 0.315), (0.07, 0.245, 0.352)]
    banded_tail(k, TAIL, [0.028, 0.038, 0.037, 0.028, 0.012], GREY,
                [(0.28, 0.6, DARK), (0.82, 1.1, PALE)],
                [((0.1, -0.1, 1), 0.045, PALE), ((0.6, 0.2, 0.75), 0.04, PALE), ((-0.4, 0.3, 0.8), 0.036, PALE)],
                0.045, 0.018, 0.012)

    # --- the open, howling mouth on the front of the raised muzzle ---------------------------------------------
    CM, CT, CP = srgb(MOUTH), srgb(TONGUE), srgb(PALE)
    def mouth(u, v):
        c = np.tile(CP, (len(u), 1))
        r = np.hypot(u / 0.86, (v + 0.05) / 0.92)
        c[r < 1] = CM
        c[(r < 1) & (v < -0.35)] = CT
        return c
    k.decal(HC + (0, -0.095, -0.006), unit(0, -1, -0.05), (0.024, 0.027), mouth, eps=0.003, rings=14, segs=48)

    k.build(voxel=0.0048, smooth=3, band=0.006)

    # --- the eyes: amber, under the grey brow, on the mask's top edge, looking a little up ---------------------
    de = unit(0.56, -0.78, 0.24)
    E = on_ell(HC, HR, de) + (0, 0, 0.002)
    out_ = clipped(eye_top)
    look = eye_open(AMBER, eye_top)

    def eyes(state):
        lay_eyes(k, state, E, de, (0.022, 0.019), look, out_, PALE, GREY)
    k.eye_states(eyes)
    k.export(out_path)


if __name__ == "__main__":
    lupling(*args())
