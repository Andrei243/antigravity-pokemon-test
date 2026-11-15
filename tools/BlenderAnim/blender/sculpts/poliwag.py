# Poliwag sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/poliwag.py -- <the export's Poliwag.glb> <out.glb>
#
# The design, from the references: a round blue tadpole of a body, a little taller than wide; big round eyes high on
# its front, white in a black rim, the black pupils looking up, each with a white glint; pink pursed lips between and
# below them; a large white disc on its belly with a black spiral of some two and a half turns; two short blue legs
# hanging under it with round feet; a long pale tail out of its back, a thin stem widening into a broad flat paddle
# with a pale blue rib down its middle.
#
# Written to be grown from (Poliwhirl, Poliwrath: sibling("poliwag")): the line's colours, its way of laying the
# spiral, an eye, a bump under an eye, a limb and a foot are one function each.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, slab, outward, args

# --- the line's colours ---------------------------------------------------------------------------------------------
BLUE, WHITE, INK, SCLERA = "#5A92D4", "#F6F8FB", "#17171F", "#FFFFFF"
LIP, LIP_LINE = "#EE9CBA", "#A8466C"
FIN, STEM, RIB = "#DCE9F6", "#B9D3EF", "#9BBDE6"

# --- the frame ------------------------------------------------------------------------------------------------------
B = np.array([0.0, -0.005, 0.25])         # the body's middle (the body bone is at 0, 0, 0.22)
BR = np.array([0.174, 0.166, 0.184])
EYE_DIR = unit(0.36, -0.76, 0.52)          # where each eye looks out from the body (the +X one)
EYE_HALF = (0.043, 0.05)
LIP_DIR = unit(0, -0.86, 0.5)
BELLY_DIR = unit(0, -1, -0.34)
BELLY_HALF = 0.118
TAIL_DIR = unit(0.57, 0.82, 0.0)          # back and out to +X, so the paddle's face shows to the game's cameras
VOXEL = 0.005


def on_ell(c, r, d, lift=0.0):
    """The point of an ellipsoid along direction d from its centre, and its outward normal there."""
    d = unit(*d)
    s = 1 / math.sqrt(((d / np.array(r)) ** 2).sum())
    p = np.array(c, float) + d * s
    n = (p - c) / np.array(r) ** 2
    n /= np.linalg.norm(n)
    return p + n * lift, n


# --- the spiral -----------------------------------------------------------------------------------------------------
def spiral_look(turns, w, spin, start=0.1, end=0.9, ground=WHITE, ink=INK, edge=0.018):
    """
    A white disc with a black spiral on it (u right, v up as seen from the front): an Archimedean line from start to
    end of the disc's radius over so many turns, w wide. spin +1 grows counter-clockwise outward on the screen, -1
    clockwise. Laid as one polar patch: the line runs round the patch's own rings, so its edges stay crisp.
    """
    G, K = srgb(ground), srgb(ink)
    b = (end - start) / turns
    def look(u, v):
        r = np.sqrt(u * u + v * v)
        th = np.arctan2(v, u)
        frac = np.mod(spin * th / (2 * math.pi), 1.0)
        n = np.round((r - start) / b - frac)
        s = n + frac
        d = np.abs(r - (start + b * s))
        # the line's ends rounded: past an end, the distance is to the end's own point
        for s_end in (0.0, turns):
            past = (s < s_end) if s_end == 0.0 else (s > turns)
            ang = spin * 2 * math.pi * s_end
            ex, ey = (start + b * s_end) * math.cos(ang), (start + b * s_end) * math.sin(ang)
            d = np.where(past, np.sqrt((u - ex) ** 2 + (v - ey) ** 2), d)
        return soft(G, K, w / 2 - d, edge)
    return look


def soft(ground, ink, d, edge):
    """ink where d is above nought, eased over edge either side: a vertex's colour then puts the line's edge between
    the patch's rings, where a hard choice of colour steps from ring to ring."""
    m = np.clip(0.5 + d / (2 * edge), 0, 1)[:, None]
    return ground[None] * (1 - m) + ink[None] * m


def belly(k, c, n, half, look):
    k.decal(c, n, (half, half), look, eps=0.004, rings=64, segs=224)


# --- an eye ---------------------------------------------------------------------------------------------------------
def round_eye(pupil=(-0.06, 0.12), pupil_r=(0.5, 0.56), rim=0.17, glint=(0.1, 0.36), glint_r=0.17):
    """A round eye: white in a black rim (a sixth of the eye's half size), a big black pupil and a white glint."""
    SC_, K, W = srgb(SCLERA), srgb(INK), np.ones(3)
    E = 0.035
    def eye(u, v):
        c = soft(SC_, K, np.sqrt(u * u + v * v) - (1 - rim), E)
        pr = np.sqrt(((u - pupil[0]) / pupil_r[0]) ** 2 + ((v - pupil[1]) / pupil_r[1]) ** 2)
        m = np.clip(0.5 + (1 - pr) * min(pupil_r) / (2 * E), 0, 1)[:, None]
        c = c * (1 - m) + K[None] * m
        gr = np.sqrt((u - glint[0]) ** 2 + (v - glint[1]) ** 2)
        m = np.clip(0.5 + (glint_r - gr) / (2 * E), 0, 1)[:, None]
        return c * (1 - m) + W[None] * m
    return eye


def eye_patch(k, state, at, n, half, eye, lid=BLUE, ink=INK, look=None):
    f = look(state) if look else eye_look(state, eye, srgb(lid), srgb(ink), line=0.2)
    k.decal(at, n, half, f, eps=0.005, rings=20, segs=56, mirror=True)


def eye_bump(k, c, r, d, size, bone="body"):
    """The swelling an eye sits on, a flattened ball fused into the body there, so the patch does not wrap."""
    p, n = on_ell(c, r, d)
    k.add("bump", ell(p - n * size[1] * 0.45, size, rot=(0, 0, 0)), BLUE, bone, mirror=True)
    return p, n


def lips(k, c, r, d, size, bone="body"):
    """Pursed lips: a pink oval, loose so its edge keeps, with a dark line across its middle (a thin dark disc that
    stands just out of the oval's front, so the line is crisp)."""
    p, n = on_ell(c, r, d)
    lc = p + n * 0.004 * size
    s = size
    rot = (-math.degrees(math.atan2(n[2], -n[1])), 0, 0)
    k.add("lips", outward(ell(lc, (0.041 * s, 0.024 * s, 0.021 * s), rot=rot)), LIP, bone, fuse=False,
          surface="shell")
    k.add("lipline", outward(ell(lc, (0.03 * s, 0.0258 * s, 0.0032 * s), rot=rot)), LIP_LINE, bone, fuse=False,
          surface="shell")


# --- limbs ----------------------------------------------------------------------------------------------------------
def limb(k, name, pts, radii, color, bone, mirror=True):
    k.add(name, tube(pts, radii), color, bone, mirror=mirror)


def foot(k, name, c, r, turn, color, bone, mirror=True):
    k.add(name, ell(c, r, rot=(0, 0, turn)), color, bone, mirror=mirror)


# --- the tail -------------------------------------------------------------------------------------------------------
def tail(k, root, d, stem_len, paddle_len, paddle_h, tilt=20):
    """A thin stem back from root along d, then a broad flat paddle with a rib down it, its plane upright, leaning
    tilt degrees so its top face shows a little."""
    d = unit(*d)
    side = np.cross((0, 0, 1.0), d); side /= np.linalg.norm(side)
    t = math.radians(tilt)
    ez = np.array([0, 0, 1.0]) * math.cos(t) + side * math.sin(t)
    root = np.array(root, float)
    s0 = root + d * stem_len
    k.add("tailstem", outward(tube([root - d * 0.04, root + d * stem_len * 0.5 + ez * 0.004, s0 + d * 0.03],
                                   [0.032, 0.022, 0.016])), STEM, "tail", fuse=False)
    # the paddle's outline: narrow at the stem, swelling to a broad rounded end, a little fuller above
    L = paddle_len
    a = np.linspace(0, 1, 28)
    h = paddle_h * (0.14 + 0.86 * a ** 0.8) * np.sqrt(np.clip(1 - (np.clip(a - 0.55, 0, 1) / 0.45) ** 2, 0, 1))
    h = np.maximum(h, 0.008)
    top = [(L * x, y) for x, y in zip(a, h)]
    bottom = [(L * x, -0.85 * y) for x, y in zip(a[::-1], h[::-1])]
    k.add("paddle", slab(top + bottom, s0, d, ez, 0.018), FIN, "tail", fuse=False)
    # the rib down its middle, proud of both faces
    k.add("tailrib", outward(tube([s0, s0 + d * L * 0.45, s0 + d * L * 0.86], [0.017, 0.016, 0.012])), RIB,
          "tail", fuse=False)


# --- Poliwag --------------------------------------------------------------------------------------------------------
def poliwag(src, out):
    k = Kit(src, coat="scales")

    # The round body, fuller below and a little narrower toward the top
    def tadpole(V):
        V = V.copy()
        s = 1 + 0.04 * np.clip(-V[:, 2], 0, 1) - 0.06 * np.clip(V[:, 2], 0, 1) ** 2
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell(B, BR, deform=tadpole), BLUE, "body")

    # The swellings the eyes sit on
    eye_bump(k, B, BR, EYE_DIR, (0.064, 0.034, 0.07))

    # Pink pursed lips
    lips(k, B, BR, LIP_DIR, 1.0)

    # Two short legs hanging under the body, round feet
    limb(k, "leg", [(0.09, -0.02, 0.12), (0.1, -0.035, 0.07), (0.108, -0.05, 0.04)], [0.038, 0.034, 0.032], BLUE,
         "leg{s}")
    foot(k, "foot", (0.11, -0.058, 0.03), (0.042, 0.05, 0.03), 8, BLUE, "leg{s}")

    # The white disc on the belly and its spiral
    bp, bn = on_ell(B, BR, BELLY_DIR)
    belly(k, bp, bn, BELLY_HALF, spiral_look(2.6, 0.15, 1, start=0.1, end=0.86))

    # The long tail: a stem out of the back, a broad pale paddle
    tail(k, (0.0, 0.13, 0.15), TAIL_DIR, 0.06, 0.26, 0.1)

    k.build(voxel=VOXEL, band=0.004)

    ep, en = on_ell(B, BR, EYE_DIR, 0.0)
    eye = round_eye()
    def eyes(state):
        eye_patch(k, state, ep + en * 0.02, en, EYE_HALF, eye)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    poliwag(*args())
