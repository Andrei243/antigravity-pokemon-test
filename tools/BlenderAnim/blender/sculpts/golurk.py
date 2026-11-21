# Golurk sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/golurk.py -- <the export's Golurk.glb> <out.glb>
#
# Grown from Golett's script: its deep blue and pale teal clay, its faceted stones, its brown and its raised yellow
# square spirals, its eyes of yellow glyphs.
# The design, from the references: a towering golem of pale teal clay. A great barrel of a chest, its upper half deep
# blue and cracked with yellow light under a brown oval seal (two holes in its ends) laid slanting across it, its
# lower half pale teal; below it a short skirt of pale clay over thick column legs, each banded at the ankle in brown,
# on flat wide feet. A small head of deep blue sunk into the chest's top, a knot tied on its crown, two yellow glyphs
# for eyes ("I" and "L"). Great faceted shoulders with a horn rising up and out from each and a yellow spiral on its
# face; short upper arms to huge forearms banded in brown at the elbow, a deep blue panel with a yellow spiral on the
# back of each, and huge fists. The forearms are carried a little forward here.
# The task's text spoke of a yellow-strapped chest: the pictures show a brown oval seal over yellow cracks and no
# strap round the chest (the brown is at the elbows and ankles).
# The skeleton: body (0, 0, 0.4), arms (+-0.16, 0, 0.56), head (0, -0.02, 0.62), legs (+-0.08, 0, 0.24).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, smoothstep, inside, eye_look, args, boxy, hull, polygon, slab, \
    lathe, tri, sibling

go = sibling("golett")
stone, spiral_panel, RECT = go.stone, go.spiral_panel, go.RECT

DEEP, ROCK, ROCKSHADE, STRAP, SPIRAL, GLOW = go.DEEP, go.ROCK, go.ROCKSHADE, go.STRAP, go.SPIRAL, go.GLOW
CRACK, HOLE, UNDER = "#F4D670", "#5A3A22", "#2A3036"
STRAPDARK = "#7C5434"

# --- the frame --------------------------------------------------------------------------------------------------------
CHEST_C, CHEST_R = np.array([0.0, 0.0, 0.592]), np.array([0.174, 0.142, 0.152])
HEAD_C, HEAD_R = np.array([0.0, -0.022, 0.772]), np.array([0.082, 0.072, 0.056])
EYE_Z = 0.775
SHOULDER = np.array([0.245, 0.0, 0.665])
FORE = np.array([0.272, -0.06, 0.415])
FIST = np.array([0.276, -0.085, 0.305])
LEG_X = 0.095
VOXEL = 0.0065


def band(c, r_in, r_out, height, axis=(0, 0, 1.0), capped=False):
    """A brown band of two ridges round a limb: a ring turned on axis, its middle at c."""
    h = height
    prof = [(r_in, -h / 2), (r_out, -h / 2), (r_out + 0.004, -h * 0.3), (r_out - 0.004, 0.0),
            (r_out + 0.004, h * 0.3), (r_out, h / 2), (r_in, h / 2), (r_in, -h / 2)]
    if capped:                      # a drum shut at its top, where the upper arm goes into it
        prof = [(0, -h / 2)] + prof[1:6] + [(0, h / 2)]
    return lathe(c, axis, prof, seg=48)


def chest(k):
    k.add("chest", ell(CHEST_C, CHEST_R, deform=boxy(2.4)), DEEP, "body")
    k.add("waist", ell((0, 0.0, 0.49), (0.158, 0.13, 0.06)), ROCK, "body")
    # a raised belt of clay where the chest meets the skirt
    k.add("belt", lathe((0, 0, 0.468), (0, 0, 1), [(0.12, -0.022), (0.166, -0.022), (0.171, 0.0), (0.166, 0.022),
                                                     (0.12, 0.022), (0.12, -0.022)], seg=56), ROCKSHADE, "body",
          fuse=False)
    # the lower half pale, its edge jagged where the clay is cracked
    def lower(P):
        th = np.arctan2(P[:, 0], -P[:, 1])
        edge = 0.545 + 0.018 * tri(th * 2.2)
        return inside(P[:, 2] - edge, 0.0025)
    k.paint(lower, ROCK, on=["chest", "waist"])
    # yellow cracks across the upper front, as lines of light
    cracks = [[(-0.15, 0.6), (-0.1, 0.645), (-0.045, 0.62), (0.01, 0.68), (0.06, 0.66), (0.14, 0.7)],
              [(-0.03, 0.62), (-0.06, 0.575), (-0.1, 0.56)],
              [(0.06, 0.66), (0.09, 0.61), (0.075, 0.57)],
              [(0.0, 0.745), (-0.02, 0.705), (0.01, 0.68)]]
    def lines(P):
        m = np.zeros(len(P))
        front = (P[:, 1] < -0.03).astype(float)
        for pl in cracks:
            for (x0, z0), (x1, z1) in zip(pl, pl[1:]):
                a, b = np.array([x0, z0]), np.array([x1, z1])
                q = np.stack([P[:, 0], P[:, 2]], 1)
                t = np.clip(((q - a) @ (b - a)) / ((b - a) @ (b - a)), 0, 1)
                d = np.linalg.norm(q - (a + t[:, None] * (b - a)), axis=1)
                m = np.maximum(m, inside(d - 0.0065, 0.002))
        return m * front * (P[:, 2] > 0.55)
    k.paint(lines, CRACK, on=["chest"])
    # the seal: a brown oval laid slanting across the chest, a hole in each end
    p = go.ell_point(CHEST_C, CHEST_R, unit(0.0, -1, 0.15))
    n = unit(0.0, -1, 0.1)
    a = math.radians(-32)
    ex = np.array([math.cos(a), 0, math.sin(a)])
    ex = ex - n * (ex @ n); ex /= np.linalg.norm(ex)
    ez = np.cross(n, ex)
    L, R = 0.09, 0.047
    outline = [((L - R) * (1 if math.cos(t) >= 0 else -1) + R * math.cos(t), R * math.sin(t))
               for t in np.linspace(0, 2 * math.pi, 48, endpoint=False)]
    k.add("seal", slab(outline, p - n * 0.006, ex, ez, 0.022), STRAP, "body", fuse=False, surface="leather")
    for sx in (-1, 1):
        hc = p + ex * sx * (L - R) - n * 0.006
        k.add(f"sealhole{'R' if sx > 0 else 'L'}", lathe(hc, -n, [(0, 0.008), (0.022, 0.008), (0.024, 0.0125), (0, 0.0135)]),
              HOLE, "body", fuse=False, surface="leather")


def head(k):
    """A small deep blue head sunk into the chest's top, a knot tied on its crown."""
    def drum(V):
        n = ((V[:, 0] ** 2 + V[:, 1] ** 2) ** 2 + V[:, 2] ** 4) ** 0.25
        return V / n[:, None]
    k.add("head", ell(HEAD_C, HEAD_R, deform=drum), DEEP, "head")
    k.add("knot", tube([HEAD_C + (0, 0.005, 0.04), HEAD_C + (0, 0.008, 0.07), HEAD_C + (0, 0.012, 0.095)],
                       [0.035, 0.018, 0.009]), DEEP, "head")
    k.add("tie", ell(HEAD_C + (0, 0.006, 0.06), (0.026, 0.024, 0.012)), DEEP, "head")


def shoulders(k):
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"shoulder{side}", stone(SHOULDER * m, (0.09, 0.088, 0.092), rot=(0, 0, 8 * s), seed=21 + (s > 0),
                                       cham=0.32, jit=0.06), ROCK, f"arm{side}", fuse=False)
        base = (SHOULDER + (0.035, 0.0, 0.07)) * m
        tip = (SHOULDER + (0.085, 0.01, 0.175)) * m
        pts = [base + np.array([dx, dy, 0.0]) for dx in (-0.035, 0.035) for dy in (-0.034, 0.034)]
        pts += [base + np.array([dx * 0.6, dy * 0.6, -0.02]) for dx in (-0.035, 0.035) for dy in (-0.034, 0.034)]
        pts.append(tip)
        k.add(f"horn{side}", hull(pts), ROCK, f"arm{side}", fuse=False)
        # a yellow spiral on the shoulder's face, out and to the front
        nrm = unit(0.75 * s, -0.62, 0.15)
        c = SHOULDER * m + nrm * 0.083
        spiral_panel(k, f"spiral{side}", c, nrm, (0, 0, 1), 0.04, f"arm{side}", plate=ROCK, turn=0)


def arms(k):
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"arm{side}", tube([np.array([0.2, 0.0, 0.6]) * m, np.array([0.255, -0.03, 0.53]) * m,
                                  (FORE + (0, 0.02, 0.05)) * m], [0.048, 0.044, 0.048]), ROCK, f"arm{side}")
        k.add(f"fore{side}", stone(FORE * m, (0.086, 0.084, 0.08), rot=(0, 0, 0), seed=31 + (s > 0), cham=0.42,
                                   jit=0.05), ROCK, f"arm{side}", fuse=False)
        k.add(f"fist{side}", stone(FIST * m, (0.09, 0.088, 0.072), rot=(0, 0, -6 * s), seed=35 + (s > 0), cham=0.36,
                                   jit=0.07), ROCK, f"arm{side}", fuse=False)
        k.add(f"knuckle{side}", stone((FIST + (0.0, -0.055, -0.01)) * m, (0.08, 0.04, 0.055), seed=39 + (s > 0),
                                      cham=0.4), ROCKSHADE, f"arm{side}", fuse=False)
        # the brown band at the forearm's top, round its axis
        k.add(f"elbowband{side}", band((FORE + (0.0, 0.0, 0.045)) * m, 0.07, 0.094, 0.055, capped=True), STRAP, f"arm{side}",
              fuse=False, surface="leather")
        # a deep blue panel with a yellow spiral on the forearm's outer front
        nrm = unit(0.8 * s, -0.6, 0.0)
        c = FORE * m + np.array([0.0, 0.0, -0.01]) + nrm * 0.088
        spiral_panel(k, f"forepanel{side}", c, nrm, (0, 0, 1), 0.048, f"arm{side}", plate=DEEP, turn=0)


def legs(k):
    # a short skirt of pale clay over the tops of the legs, dark beneath
    skirt = [(0, 0.385), (0.13, 0.385), (0.13, 0.33), (0.172, 0.325), (0.168, 0.4), (0.158, 0.47), (0, 0.47)]
    k.add("skirt", lathe((0, 0, 0), (0, 0, 1), skirt, seg=56), ROCK, "body", fuse=False)
    k.add("under", lathe((0, 0, 0), (0, 0, 1), [(0, 0.378), (0.129, 0.378), (0.129, 0.384), (0, 0.384)], seg=40),
          UNDER, "body", fuse=False)
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"leg{side}", tube([np.array([0.085, 0.0, 0.38]) * m, np.array([LEG_X, -0.005, 0.2]) * m,
                                  np.array([LEG_X, -0.01, 0.07]) * m], [0.066, 0.062, 0.063]), ROCK, f"leg{side}")
        k.add(f"ankle{side}", band(np.array([LEG_X, -0.01, 0.115]) * m, 0.045, 0.073, 0.062), STRAP, f"leg{side}",
              fuse=False, surface="leather")
        k.add(f"foot{side}", stone(np.array([LEG_X + 0.004, -0.035, 0.03]) * m, (0.086, 0.106, 0.032), seed=41 + (s > 0),
                                   cham=0.3, jit=0.04), ROCK, f"leg{side}", fuse=False)


# the glyphs in units of GLYPH, each about a point inside it (a patch's outline must hold its own middle): an "I", and
# an "L" about its corner; MID is where the middle of each glyph's box lies from that point
GLYPH = 0.025
EYE_I = polygon([(-0.32, -0.9), (0.32, -0.9), (0.32, 0.9), (-0.32, 0.9)])
EYE_L = polygon([(-0.32, -0.32), (1.1, -0.32), (1.1, 0.32), (0.32, 0.32), (0.32, 1.48), (-0.32, 1.48)])
GLYPHS = ((-0.036, EYE_I, (0.0, 0.0)), (0.034, EYE_L, (0.39, 0.58)))


def lay_eyes(k, state):
    """The glyphs: an "I" on the viewer's left, an "L" on the right; shut, a yellow line on the head's blue."""
    n = (0, -1.0, 0)
    y = HEAD_C[1] - HEAD_R[1] * 0.99
    for x, shape, mid in GLYPHS:
        c = (x - mid[0] * GLYPH, y, EYE_Z - mid[1] * GLYPH)
        if state in ("Open", "Fierce"):
            k.decal(c, n, (GLYPH, GLYPH), GLOW, eps=0.0025, outline=shape, rings=12, segs=120)
        if state == "Fierce":
            i = -1 if x > 0 else 1
            lid = polygon([(-i * 1.0, 1.0), (-i * 1.0, 0.1), (i * 1.0, -0.9), (i * 1.0, 1.0)])
            k.decal((x, y, EYE_Z + 0.014), n, (0.025, 0.014), DEEP, eps=0.004, outline=lid, rings=10, segs=72)
        elif state != "Open":
            look = eye_look(state, lambda u, v: np.tile(srgb(GLOW), (len(u), 1)), srgb(DEEP), srgb(GLOW), line=0.22)
            k.decal((x, y, EYE_Z), n, (0.022, 0.025), look, eps=0.0025, outline=RECT, rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    chest(k)
    head(k)
    shoulders(k)
    arms(k)
    legs(k)
    k.build(voxel=VOXEL, smooth=4, band=0.005)
    k.eye_states(lambda state: lay_eyes(k, state))
    k.export(out)
