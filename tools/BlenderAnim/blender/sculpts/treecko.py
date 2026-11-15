# Treecko sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/treecko.py -- <the export's Treecko.glb> <out.glb>
# A small light green gecko standing upright, its head the biggest part of it: a tall rounded head, two low ridges
# over the crown with a crease between them, a snout bulging forward at its foot with two nostril dots; great eyes on
# the sides of the head, all yellow-orange with a black rim and a black upright slit, their tops cut straight by the
# brow; a wide mouth whose red runs down the throat into a red oval belly with a seam low across it. A small pear of a
# body, thighs fused into the hips, short shins and long feet of three toes ending in round pads; thin arms forward and
# a little down, three fingers a hand ending in round pads; a broad dark green tail like a fat leaf, back and out to
# the +X side and low, its end curled up.
#
# Written to be grown from (Grovyle and Sceptile import it with sibling("treecko")): the line's colours, its eyes,
# red throat and belly, padded digits, legs, arms and leaves are the functions below, placed by a frame of named
# numbers (L); the calls that build Treecko are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from types import SimpleNamespace
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, eye_look, args

# --- the line's colours ---------------------------------------------------------------------------------------------
GREEN = "#98D45A"           # the coat
DARK = "#3A9C5E"            # Treecko's tail
RED = "#E2525A"             # throat, mouth and belly
SEAM = "#A8303A"            # the line across the belly
NOSTRIL = "#3C6A2C"
INK, WHITE = srgb("#15161C"), np.array([1.0, 1.0, 1.0])
GOLD, AMBER = srgb("#FAD232"), srgb("#F09A1C")
VOXEL = 0.0055


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre."""
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r, float)) ** 2).sum())


def ell_frame(c, r, ex, ey, ez, deform=None, sub=4):
    """An ellipsoid of radii r along the axes ex, ey, ez (each a direction) about c."""
    V, F = ell((0, 0, 0), r, deform=deform, sub=sub)
    M = np.stack([unit(*ex), unit(*ey), unit(*ez)])
    return V @ M + np.array(c, float), F


# --- the eyes ---------------------------------------------------------------------------------------------------------
def brow_cut(top, slope):
    """An eye's outline cut straight along the brow v = top + slope u (u outward, toward the back): its reach."""
    def cut(th):
        s = math.sin(th) - slope * math.cos(th)
        return min(1.0, top / s) if s > 1e-6 else 1.0
    return cut


def slit_eye(cut, rim=0.8, slit=None, slit_c=(0.0, -0.04), iris=None):
    """The open eye: yellow-gold all over, amber toward its rim, a black rim (u outward, v up); the slit is a patch of
    its own (eyes(), L.slit), or its edge steps at the rings. iris: a gold disc smaller than the eye with a pale ring
    round it."""
    def eye(u, v):
        reach = np.array([cut(t) for t in np.arctan2(v, u)])
        rho = np.sqrt(u * u + v * v) / reach
        w = smoothstep(0.35, 0.78, rho)[:, None]
        c = GOLD[None] * (1 - w) + AMBER[None] * w
        if iris is not None:
            r_i, pale = iris
            ri = np.hypot(u - slit_c[0], v - slit_c[1])
            c[ri > r_i] = srgb(pale)
        if slit is not None:
            c[((u - slit_c[0]) / slit[0]) ** 2 + ((v - slit_c[1]) / slit[1]) ** 2 < 1] = INK
        c[rho >= rim] = INK
        return c
    return eye


def eyes(k, L):
    """The eyes' layer for a state: a big oval on each side of the head, cut along the brow; the left laid by hand."""
    de = unit(*L.eye_dir)
    E = on_ell(L.H, L.HR, de) + np.array(L.eye_shift, float)
    look = L.eye_look
    lid = srgb(L.lid)
    cut = L.eye_outline

    def fierce(u, v):
        c = look(u, v)
        edge = 0.42 - 0.35 * u                       # the brow slanting down toward the nose (u outward is back)
        c[v > edge] = lid
        c[np.abs(v - edge) < 0.13] = INK
        return c

    def lay(state):
        fn = fierce if state == "Fierce" else eye_look(state, look, lid, INK, line=0.15)
        m = np.array([-1, 1, 1.0])
        k.decal(E, de, L.eye_h, fn, eps=0.006, rings=20, segs=56, outline=cut)
        k.decal(E * m, de * m, L.eye_h, lambda u, v: fn(-u, v), eps=0.006, rings=20, segs=56,
                outline=lambda th: cut(math.pi - th))
        if state in ("Open", "Fierce"):
            # The slit: a lens of its own over the eye, shorter under the fierce brow
            (sw, sh), (sx, sy) = L.slit, L.slit_at
            if state == "Fierce":
                sh, sy = sh * 0.7, sy - sh * 0.3
            right = np.cross((0, 0, 1.0), de); right /= np.linalg.norm(right)
            upv = np.cross(de, right)
            c = E + right * sx * L.eye_h[0] + upv * sy * L.eye_h[1]
            k.decal(c, de, (sw * L.eye_h[0], sh * L.eye_h[1]), INK, eps=0.0085, rings=8, segs=40, mirror=True)
    return lay, E, de


# --- the red of the throat and belly ------------------------------------------------------------------------------------
def red_front(L):
    """
    The red from the mouth down: on the face below the smile line (low at the snout, rising to its corners under the
    eyes), down the throat and into the belly's oval. L.red: (z, half width) pairs from the bottom of the belly up to
    the mouth, measured on the front; L.smile: the line's height at the middle, its rise at the corners and how far
    round the head it runs (radians from the front). L.red_axis: the y of the upright axis the angles are measured
    round, a number or (z, y) pairs for a body that leans.
    """
    zs = np.array([p[0] for p in L.red]); ws = np.array([p[1] for p in L.red])
    z0, rise, reach = L.smile
    ax = L.red_axis

    def mask(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        yc = ax if np.isscalar(ax) else np.interp(z, [p[0] for p in ax], [p[1] for p in ax])
        a = np.arctan2(np.abs(x), -(y - yc))
        w = np.interp(z, zs, ws, left=-1, right=-1)
        body = inside(np.abs(x) - w, 0.0045) * inside(a - 1.0, 0.01) * (z <= zs[-1])
        line = z0 + rise * (a / reach) ** 2
        face = inside(z - line, 0.0045) * inside(a - reach, 0.03) * (z > zs[-1] - 0.03) * (z < z0 + rise + 0.02)
        return np.maximum(body, face)
    return mask


def belly(k, L):
    """The red belly: an oval patch down the front (so its edge is crisp), a thin dark seam low across it.
    L.belly: centre, half sizes, the seam's height in v (-1..1) or None."""
    c, half, seam_v = L.belly
    red, dark = srgb(RED), srgb(SEAM)

    def look(u, v):
        col = np.tile(red, (len(u), 1))
        if seam_v is not None:
            col[np.abs(v - (seam_v + 0.18 * u * u)) < 0.055] = dark
        return col
    k.decal(c, (0, -1, 0), half, look, eps=0.003, rings=44, segs=64)


# --- digits, legs and arms ----------------------------------------------------------------------------------------------
def digits(k, side, s, base, dirs, length, r, pad, bone, color=GREEN):
    """Fingers or toes from base, each a short tube ending in a round pad (pad: its radius, 0 for none)."""
    m = np.array([s, 1, 1.0])
    base = np.array(base, float)
    for j, d in enumerate(dirs):
        d = unit(*d)
        tip = base + d * length
        k.add(f"digit{bone}{j}", tube(np.array([base, tip]) * m, [r * 1.1, r]), color, bone)
        if pad:
            k.add(f"pad{bone}{j}", ell((tip + d * pad * 0.35) * m, (pad, pad, pad * 0.9), sub=3), color, bone)


def leg(k, side, s, thigh_c, thigh_r, shin, shin_r, foot_c, foot_r, toes, toe_len, toe_r, pad, color=GREEN):
    """A leg: a thigh fused into the hips, a short shin, a long foot with three toes ending in round pads."""
    m = np.array([s, 1, 1.0])
    bone = f"leg{side}"
    k.add(f"thigh{side}", ell(np.array(thigh_c) * m, thigh_r), color, bone)
    k.add(f"shin{side}", tube(np.array(shin) * m, shin_r), color, bone)
    k.add(f"foot{side}", ell(np.array(foot_c) * m, foot_r), color, bone)
    fc = np.array(foot_c, float)
    base = fc + np.array([0, -foot_r[1] * 0.7, -foot_r[2] * 0.15])
    digits(k, side, s, base, toes, toe_len, toe_r, pad, bone, color)


def arm(k, side, s, pts, radii, hand_c, hand_r, fingers, finger_len, finger_r, pad, color=GREEN):
    """An arm forward and a little down: a tube from the shoulder through the elbow to the wrist, a palm, fingers."""
    m = np.array([s, 1, 1.0])
    bone = f"arm{side}"
    k.add(f"arm{side}", tube(np.array(pts) * m, radii), color, bone)
    k.add(f"hand{side}", ell(np.array(hand_c) * m, hand_r), color, bone)
    digits(k, side, s, hand_c, fingers, finger_len, finger_r, pad, bone, color)


# --- leaves -------------------------------------------------------------------------------------------------------------
def leaf_blade(base, d, face, length, width, thick, widest=0.35, tip=0.0, n=9):
    """
    A thick pointed leaf as one solid with flat faces: from base along d, its face looking along `face`, widest at
    `widest` of its length, a kite section (thick at the midrib, thin at the edges). tip: the width at the end (0 a
    point). For Grovyle's and Sceptile's leaves and blades.
    """
    d = unit(*d)
    f = np.array(face, float); f = f - d * (f @ d); f /= np.linalg.norm(f)
    a = np.cross(f, d)
    base = np.array(base, float)
    pts = []
    for i in range(n + 1):
        t = i / n
        if t <= widest:
            w = width * (0.45 + 0.55 * math.sin(math.pi / 2 * t / widest))
        else:
            w = tip + (width - tip) * math.cos(math.pi / 2 * (t - widest) / (1 - widest)) ** 0.9
        p = base + d * length * t
        pts += [p + a * w, p - a * w]
        th = thick * (1 - 0.75 * t)
        pts += [p + f * th / 2, p - f * th / 2]
    pts.append(base + d * length * 1.02)
    return hull(pts, merge=0)


# --- Treecko's frame ----------------------------------------------------------------------------------------------------
L = SimpleNamespace(
    H=np.array([0.0, -0.025, 0.535]), HR=np.array([0.112, 0.105, 0.13]),
    eye_dir=(0.8, -0.5, 0.1), eye_shift=(0, 0, 0.012), eye_h=(0.048, 0.054),
    eye_outline=brow_cut(0.7, 0.18), lid=GREEN, slit=(0.17, 0.66), slit_at=(0.0, -0.06),
    red=[(0.335, 0.035), (0.36, 0.04), (0.38, 0.042), (0.42, 0.06), (0.445, 0.085)],
    smile=(0.448, 0.045, 1.05), red_axis=-0.01,
    belly=((0, -0.07, 0.272), (0.058, 0.105), -0.62),
)
L.eye_look = slit_eye(L.eye_outline)


def head(k, L):
    """The head: a tall egg narrowing to its crown, two low ridges over the crown with a crease between them, the
    snout bulging forward at its foot, full jaws under the eyes."""
    H, HR = L.H, L.HR

    def crown(V):
        V = V.copy()
        up = np.clip(V[:, 2], 0, 1)
        V[:, 0] *= 1 - 0.22 * up ** 1.5
        V[:, 1] *= 1 - 0.08 * up
        return V
    k.add("head", ell(H, HR, deform=crown), GREEN, "head")
    for s in (1, -1):
        k.add(f"ridge{'R' if s > 0 else 'L'}", ell(H + (0.036 * s, 0.025, 0.06), (0.05, 0.085, 0.075), rot=(-25, 0, 0)),
              GREEN, "head")
    k.add("snout", ell(H + (0, -0.075, -0.085), (0.078, 0.06, 0.05)), GREEN, "head")
    k.add("jaw", ell(H + (0, -0.03, -0.095), (0.1, 0.085, 0.06)), GREEN, "head")
    # Two nostril dots on the snout's top front
    k.decal(H + (0.02, -0.13, -0.065), unit(0.2, -1, 0.7), (0.008, 0.0065), NOSTRIL, eps=0.003, rings=4, segs=16,
            mirror=True)


def treecko(src, out):
    k = Kit(src, coat="scales")

    # The body: a small pear, the hips low over the legs, a short neck into the head
    def pear(V):
        V = V.copy()
        low = np.clip(-V[:, 2], 0, 1)
        V[:, :2] *= (1 + 0.16 * low)[:, None]
        return V
    k.add("torso", ell((0, 0.005, 0.27), (0.074, 0.068, 0.11), deform=pear), GREEN, "body")
    k.add("hips", ell((0, 0.012, 0.19), (0.08, 0.072, 0.06)), GREEN, "body")
    k.add("neck", tube([(0, 0.0, 0.34), (0, -0.008, 0.39), (0, -0.015, 0.43)], [0.06, 0.056, 0.056]), GREEN, "head")
    head(k, L)

    # The legs: thighs fused into the hips, short shins, long feet of three toes with round pads
    for s, side in ((1, "R"), (-1, "L")):
        leg(k, side, s, (0.062, 0.0, 0.165), (0.052, 0.058, 0.062),
            [(0.066, -0.006, 0.125), (0.07, -0.014, 0.04)], [0.036, 0.032],
            (0.072, -0.045, 0.019), (0.038, 0.062, 0.019),
            [(0.55, -1, 0.08), (0.05, -1, 0.08), (-0.4, -1, 0.08)], 0.04, 0.0135, 0.0165)

    # The arms: thin, forward and a little down, three fingers a hand with round pads, curling up a little
    for s, side in ((1, "R"), (-1, "L")):
        arm(k, side, s, [(0.062, 0.0, 0.36), (0.12, -0.045, 0.325), (0.14, -0.11, 0.3)], [0.026, 0.022, 0.02],
            (0.142, -0.13, 0.298), (0.024, 0.022, 0.02),
            [(0.55, -0.8, 0.45), (0.05, -1, 0.5), (-0.45, -0.8, 0.45)], 0.035, 0.011, 0.0145)

    # The tail: a broad fat leaf, back, out to the +X side and low, its end curling up
    d = unit(0.5, 0.78, -0.36)
    up = np.array([0, 0, 1.0]); up = up - d * (up @ d); up /= np.linalg.norm(up)
    side_ax = np.cross(d, up)
    root = np.array([0.0, 0.05, 0.19])
    length, width, thick = 0.33, 0.085, 0.034

    def fat_leaf(V):
        V = V.copy()
        t = (V[:, 1] + 1) / 2                       # 0 at the root, 1 at the end
        w = 0.45 + 0.75 * np.sin(np.pi * np.clip(t, 0, 1) ** 0.8) ** 0.8
        V[:, 2] *= w; V[:, 0] *= 0.6 + 0.4 * w
        return V
    c = root + d * length * 0.5
    k.add("tail", ell_frame(c, (thick, length * 0.5, width), side_ax, d, up, deform=fat_leaf), DARK, "tail")
    end = root + d * length * 0.96
    curl = [end + d * 0.045 * math.sin(th) + up * 0.045 * (1 - math.cos(th)) * (1 - 0.1 * th)
            for th in np.linspace(0, 1.75 * math.pi, 9)]
    k.add("curl", tube(curl, list(np.linspace(0.03, 0.019, 9))), DARK, "tail")

    # The red from the mouth down the throat into the belly, a seam across it
    k.paint(red_front(L), RED)
    belly(k, L)

    lay, E, de = eyes(k, L)
    k.build(voxel=VOXEL, smooth=4, band=0.006)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    treecko(*args())
