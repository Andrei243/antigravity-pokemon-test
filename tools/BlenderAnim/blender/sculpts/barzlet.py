# Barzlet sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/barzlet.py -- <the export's Barzlet.glb> <out.glb>
#
# The design, from its picture (docs/art/reference/gemini/carpathia/10025-Barzlet.jpg) and brief (CP-dex-1-plateau,
# 25): a gangly white stork chick sitting in a round nest of twigs that it wears round its middle like a skirt, its
# long thin pink legs hanging out under the nest (one straight, one bent at the heel with the foot swung back); a
# white body with grey down over the back and the shoulders, folded white wings with black tips at their backs, a
# short white tail raised behind; a thick white neck; a round head with grey down over the crown and the back, a few
# wisps standing up; big round black eyes with a glint; a short thick orange-pink beak.
# Reference ratios (the picture): the head 0.15 of the height (built a tenth larger); the nest 0.28 of the height
# deep and 0.56 of it across; the legs showing 0.42 of the height, 0.03 of it thick (built 0.045, so that they read).
#
# Written to be grown from (barzanor.py imports it with sibling("barzlet")): the colours, the nest's twigs, the legs,
# the feet and the eye are functions here; the build is under __main__.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, lathe, srgb, inside, unit, eye_look, args, outward

# --- the line's colours --------------------------------------------------------------------------------------------
WHITE, GREY, GREY_LIGHT, BLACK = "#F5F5F4", "#A9ACB4", "#CDD0D6", "#26262C"
CHICK_BEAK, CHICK_LEG = "#F2946E", "#EBA3A5"
TWIG, TWIG_LIGHT, TWIG_DARK = "#927448", "#B09464", "#6A5236"
EYE_DARK, GLINT = srgb("#1E1A1A"), srgb("#FFFFFF")


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre, and its normal."""
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def frame():
    """Barzlet's frame (joints: body (0, 0, 0.432), wings (+-0.09, -0.04, 0.472), head (0, -0.06, 0.492), legs
    (+-0.04, -0.01, 0.352))."""
    F = SimpleNamespace()
    F.BODY, F.BODY_R = np.array([0.0, 0.01, 0.49]), np.array([0.12, 0.15, 0.1])
    F.HEAD, F.HEAD_R = np.array([0.0, -0.085, 0.69]), np.array([0.07, 0.076, 0.068])
    F.NEST_C, F.NEST_PROFILE = np.array([0.0, 0.0, 0.3]), [(0.0, 0.0), (0.1, 0.0), (0.17, 0.04), (0.205, 0.1),
                                                          (0.2, 0.165), (0.178, 0.186), (0.15, 0.172), (0.0, 0.152)]
    F.EYE_D = unit(0.64, -0.62, 0.3)
    F.EYE_HALF = (0.022, 0.023)
    F.VOXEL = 0.0065
    return F


# --- the parts -------------------------------------------------------------------------------------------------------
def nest_radius(profile, z):
    """The nest's outer radius at a height z over its base (the profile's outer wall)."""
    outer = [(r, h) for r, h in profile[1:5]] + [profile[5]]
    hs = [h for _, h in outer]; rs = [r for r, _ in outer]
    return float(np.interp(z, hs, rs))


def nest(k, name, c, profile, bone, n=16, stick=0.011, seed=4, strays=6, up=(0, 0, 1.0)):
    """A round nest of twigs: a woven bowl, arcs of twigs wound round its outer wall in three colours, a few
    twigs' ends sticking out and up over the rim."""
    c = np.array(c, float)
    up = unit(*up)
    a_, b_ = np.cross(up, (1.0, 0, 0)), None
    if np.linalg.norm(a_) < 1e-3:
        a_ = np.array([0, 1.0, 0])
    a_ = a_ / np.linalg.norm(a_); b_ = np.cross(up, a_)
    k.add(name, lathe(c, up, profile, seg=48), TWIG_DARK, bone, fuse=False, surface="wood")
    rs = np.random.RandomState(seed)
    top = profile[5][1]
    cols = (TWIG, TWIG_LIGHT, TWIG, TWIG_DARK)
    for i in range(n):
        a0 = rs.uniform(0, 2 * math.pi)
        span = rs.uniform(1.6, 2.6)
        h0 = profile[2][1] + (top - profile[2][1]) * ((i + 0.5) / n)
        tilt = rs.uniform(-0.35, 0.35) * (top - profile[2][1])
        pts = []
        for t in np.linspace(0, 1, 9):
            a = a0 + span * t
            h = h0 + tilt * (t - 0.5) * 2 + 0.012 * math.sin(t * 3 * math.pi + i)
            h = min(max(h, profile[2][1]), top - 0.004)
            r = nest_radius(profile, h) + stick * 0.4
            pts.append(c + up * h + r * (math.cos(a) * a_ + math.sin(a) * b_))
        # a twig's ends lift off the wall
        pts[0] = pts[0] + (pts[0] - (c + up * h0)) * 0.08
        pts[-1] = pts[-1] + (pts[-1] - (c + up * h0)) * 0.1
        k.add(f"{name}twig{i}", outward(tube(pts, [stick] * 9, seg=10, per=4)), cols[i % 4], bone, fuse=False,
              surface="wood")
    for j in range(strays):
        a = 2 * math.pi * (j + rs.uniform(-0.2, 0.2)) / strays
        r = profile[5][0]
        p0 = c + up * (top - 0.012) + r * 0.96 * (math.cos(a) * a_ + math.sin(a) * b_)
        out = math.cos(a) * a_ + math.sin(a) * b_
        p1 = p0 + out * rs.uniform(0.03, 0.05) + up * rs.uniform(0.015, 0.035)
        k.add(f"{name}stray{j}", outward(tube([p0, (p0 + p1) / 2, p1], [stick, stick * 0.95, stick * 0.85], seg=10,
                                              per=4)), cols[j % 3], bone, fuse=False, surface="wood")


def leg(k, name, pts, radii, bone, colour):
    """A long thin leg through its joints (hip, heel, ankle), a loose round tube."""
    k.add(name, outward(tube(pts, radii, seg=14, per=5)), colour, bone, fuse=False, surface="scales")


def foot(k, name, ank, fwd, bone, colour, toe=0.06, r=0.011, down=0.0):
    """Three toes forward and spread, one behind, from the ankle; down bends them toward the ground (a hanging foot)."""
    fwd = unit(*fwd)
    side = np.cross(fwd, (0, 0, 1.0)); side = side / np.linalg.norm(side)
    dn = np.array([0, 0, -1.0])
    for j, (sp, L) in enumerate(((-0.5, 0.9), (0.0, 1.0), (0.5, 0.9), (0.0, -0.45))):
        d = unit(*(fwd * (1 if L > 0 else -1) + side * sp + dn * down))
        tip = np.array(ank) + d * toe * abs(L)
        k.add(f"{name}toe{j}", outward(tube([ank, (np.array(ank) + tip) / 2 + dn * 0.003, tip], [r, r * 0.92, r * 0.7],
                                           seg=10, per=4)), colour, bone, fuse=False, surface="scales")


def eye_open(u, v):
    """A big round black eye with a glint high toward the front."""
    col = np.tile(EYE_DARK, (len(u), 1))
    col[np.hypot(u + 0.28, v - 0.3) < 0.24] = GLINT
    col[np.hypot(u - 0.3, v + 0.36) < 0.1] = GLINT
    return col


def eyes(k, centre, radii, d, half, lid=WHITE, line=srgb("#2A2626")):
    E, N = on_ell(centre, radii, d)
    def lay(state):
        k.decal(E, N, half, eye_look(state, eye_open, srgb(lid), line, line=0.16), eps=0.002, rings=20, segs=56,
                mirror=True)
    return lay


def feather(k, name, root, d, side, length, width, thick, bone, colour):
    """A flat tapering feather from root along d, its face across side: a loose hull with a kite section."""
    root, d, side = np.array(root, float), unit(*d), unit(*side)
    n = unit(*np.cross(side, d))
    pts = [root + side * width * 0.5, root - side * width * 0.5, root + n * thick / 2, root - n * thick / 2,
           root + d * length * 0.55 + side * width * 0.55, root + d * length * 0.55 - side * width * 0.55,
           root + d * length * 0.55 + n * thick * 0.4, root + d * length * 0.55 - n * thick * 0.4,
           root + d * length]
    k.add(name, hull(pts), colour, bone, fuse=False, mirror=True, surface="fur")


# --- Barzlet --------------------------------------------------------------------------------------------------------
def barzlet(src, out_path):
    F = frame()
    k = Kit(src, coat="fur")
    # --- the body sitting in the nest, the grey down over its back ------------------------------------------------
    k.add("body", ell(F.BODY, F.BODY_R, rot=(-8, 0, 0)), WHITE, "body")
    k.add("chest", ell((0.0, -0.08, 0.53), (0.085, 0.07, 0.075)), WHITE, "body")
    k.add("tail", ell((0.0, 0.15, 0.555), (0.05, 0.05, 0.03), rot=(-35, 0, 0)), WHITE, "body")
    k.add("wing", ell((0.105, 0.035, 0.51), (0.042, 0.125, 0.07), rot=(-12, 0, 4)), WHITE, "wing{s}", mirror=True)
    def back(P):
        """The upper back and the shoulders: over 0.55, behind the neck's root."""
        return inside(0.55 - P[:, 2], 0.01) * inside(P[:, 2] - 0.65, 0.01) * inside(-(P[:, 1] + 0.04), 0.01)
    k.paint(back, GREY, on=["body", "wing", "tail"])
    k.paint(lambda P: inside(np.linalg.norm((P - np.array([0.09, -0.09, 0.5])) / np.array([0.05, 0.05, 0.05]), axis=1)
                             - 1, 0.01), GREY, on=["chest", "body"])
    k.paint(lambda P: inside(np.linalg.norm((P - np.array([-0.09, -0.09, 0.5])) / np.array([0.05, 0.05, 0.05]),
                                            axis=1) - 1, 0.01), GREY, on=["chest", "body"])
    # black tips at the back of each folded wing
    for j, (dz, L) in enumerate(((0.035, 0.12), (0.008, 0.13), (-0.02, 0.11))):
        feather(k, f"tip{j}", (0.13 + 0.006 * j, 0.06, 0.505 + dz), (0.3, 1, -0.3 - 0.18 * j), (0.4, 0, 1), L, 0.065,
                0.024, "wing{s}", BLACK)

    # --- the thick neck and the round head, grey down over the crown and the back -------------------------------
    k.add("neck", tube([(0, -0.06, 0.52), (0, -0.085, 0.6), (0, -0.078, 0.665)], [0.05, 0.043, 0.042]), WHITE,
          "head")
    k.add("head", ell(F.HEAD, F.HEAD_R), WHITE, "head")
    def cap(P):
        q = P - F.HEAD
        crown = inside(-(q[:, 2] - 0.035 + 0.5 * q[:, 1]), 0.008)
        nape = inside(-(q[:, 1] - 0.03), 0.008) * inside(-(P[:, 2] - 0.6), 0.01)
        return np.maximum(crown, nape) * inside(P[:, 2] - 0.78, 0.01)
    k.paint(cap, GREY, on=["head", "neck"])
    for j, (x, y, a) in enumerate(((0.0, -0.075, 0.0), (-0.025, -0.06, -0.35), (0.025, -0.06, 0.35))):
        root = np.array([x, y, F.HEAD[2] + F.HEAD_R[2] - 0.01])
        d = unit(math.sin(a) * 0.6, 0.55, 1)
        k.add(f"wisp{j}", hull([root + np.array([0.008, 0, 0]), root - np.array([0.008, 0, 0]),
                                root + np.array([0, 0.008, 0]), root - np.array([0, 0.008, 0]),
                                root + d * 0.04]), GREY, "head", fuse=False, surface="fur")

    # --- the short thick beak ------------------------------------------------------------------------------------
    k.add("beak", blade([(0, -0.13, 0.69), (0, -0.18, 0.682), (0, -0.225, 0.67), (0, -0.258, 0.658)],
                        [0.03, 0.026, 0.019, 0.009], (1, 0, 0), 0.78), CHICK_BEAK, "head", fuse=False,
          surface="shell")

    # --- the nest round its middle -----------------------------------------------------------------------------
    nest(k, "nest", F.NEST_C, F.NEST_PROFILE, "body")

    # --- the long legs: the right straight down, the left bent at the heel, its foot swung back ---------------
    leg(k, "legR", [(0.04, -0.01, 0.4), (0.042, -0.005, 0.2), (0.044, 0.0, 0.022)], [0.017, 0.019, 0.016], "legR",
        CHICK_LEG)
    foot(k, "footR", (0.044, 0.0, 0.012), (0.1, -1, 0), "legR", CHICK_LEG)
    leg(k, "legL", [(-0.04, -0.01, 0.4), (-0.046, -0.03, 0.2), (-0.05, 0.07, 0.11)], [0.017, 0.019, 0.016], "legL",
        CHICK_LEG)
    foot(k, "footL", (-0.05, 0.07, 0.11), (-0.1, -1, 0), "legL", CHICK_LEG, toe=0.05, down=1.4)

    k.build(voxel=F.VOXEL, smooth=4, band=0.006, wband=0.05)
    k.eye_states(eyes(k, F.HEAD, F.HEAD_R, F.EYE_D, F.EYE_HALF))
    k.export(out_path)


if __name__ == "__main__":
    barzlet(*args())
