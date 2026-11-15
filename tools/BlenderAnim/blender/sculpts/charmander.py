# Charmander sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/charmander.py -- <the export's Charmander.glb> <out.glb>
# A small orange lizard standing on two short legs, most of it a big round head: the head an egg a little taller than
# wide with the snout bulging forward at its foot, large upright oval eyes (black rim, white, a teal iris round a black
# pupil, a glint), a wide open smile (pink inside, a small white fang at each upper corner), two nostril dots. A pale
# cream oval down the belly, short arms held forward with three small fingers, thick short legs fused into the hips,
# three white claws a foot, a tail running back and curling up on the model's +X side to its flame: yellow at its
# foot, red-orange above with an orange heart showing on both faces.
#
# Written to be grown from (Charmeleon, Charizard import it with sibling("charmander")): the line's colours, its claws,
# flame, eyes and mouth are the functions below; the calls that build Charmander are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, polygon, eye_look, args

# --- the line's colours -------------------------------------------------------------------------------------------
ORANGE, CREAM = "#F28A2E", "#F8E39C"
CLAW = "#F4F2EE"
MOUTH, TONGUE, LIP = "#9E2E36", "#F08A96", "#6E1E22"
NOSTRIL = "#7A3016"
FLAME_RED, FLAME_ORANGE, FLAME_YELLOW = "#EE3B1E", "#FF8A1E", "#FFD23A"
GLOW_RED, GLOW_ORANGE, GLOW_YELLOW = "#FF4A22", "#FF8A2A", "#FFE070"
INK, WHITE = srgb("#15161C"), np.array([1.0, 1.0, 1.0])
TEAL = srgb("#1E8FA8")


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre."""
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r, float)) ** 2).sum())


def join(*shapes):
    """Several shapes as one (a flame's tongues of one colour are one piece and one material)."""
    V, F, off = [], [], 0
    for v, f in shapes:
        V.append(v); F.extend([[i + off for i in q] for q in f]); off += len(v)
    return np.vstack(V), F


def claw(base, d, length, width, thick=None, up=(0, 0, 1)):
    """A claw: a flat-faced solid from a kite at its root (width across, thick up) to its point along d."""
    d = unit(*d)
    a = np.cross(d, np.array(up, float)); a /= np.linalg.norm(a)
    b = np.cross(a, d)
    thick = thick if thick is not None else width * 0.8
    base = np.array(base, float)
    mid = base + d * length * 0.45 - b * length * 0.06
    pts = [base + a * width / 2, base - a * width / 2, base + b * thick / 2, base - b * thick / 2 - d * 0.004,
           mid + a * width * 0.28, mid - a * width * 0.28, mid + b * thick * 0.3,
           base + d * length - b * length * 0.22]
    return hull(pts)


def teardrop(V):
    """The unit sphere made a flame's tongue: round below its widest, a third of the way up, and pointed on top."""
    V = V.copy()
    t = np.clip(V[:, 2], -1, 1)
    w = np.sqrt(np.clip(1 + t, 0, None)) * np.clip(1 - t, 0, None) ** 1.2 / 1.16
    V[:, 0] *= w; V[:, 1] *= w
    return V


def flame(k, tip, size, lean=(0, 0, 0)):
    """
    The tail's flame standing on the tail's tip: a yellow foot, a red-orange tongue rising out of it with two smaller
    tongues licking out to the sides, and an orange heart showing through both faces. size scales it (Charmander 1).
    """
    tip = np.array(tip, float)
    s = size
    rot = lean
    foot = ell(tip + (0, 0, 0.045 * s), (0.058 * s, 0.05 * s, 0.062 * s), rot=rot)
    c = tip + (0, 0, 0.165 * s)
    main = ell(c, (0.07 * s, 0.058 * s, 0.135 * s), rot=rot, deform=teardrop)
    left = ell(c + (-0.045 * s, 0, 0.0), (0.03 * s, 0.03 * s, 0.075 * s), rot=(rot[0], -28 + rot[1], rot[2]),
               deform=teardrop)
    right = ell(c + (0.045 * s, 0.0, 0.03 * s), (0.026 * s, 0.026 * s, 0.065 * s), rot=(rot[0], 30 + rot[1], rot[2]),
                deform=teardrop)
    heart = ell(c - (0, 0, 0.035 * s), (0.052 * s, 0.063 * s, 0.1 * s), rot=rot, deform=teardrop)
    k.add("flame", join(main, left, right), FLAME_RED, "flame", fuse=False, glow=GLOW_RED)
    k.add("flameheart", heart, FLAME_ORANGE, "flame", fuse=False, glow=GLOW_ORANGE)
    k.add("flamefoot", foot, FLAME_YELLOW, "flame", fuse=False, glow=GLOW_YELLOW)


def eye_open(iris, rim=0.82, iris_r=(0.66, 0.66), iris_c=(0.0, 0.0), pupil_r=(0.38, 0.38), pupil_c=(0.0, 0.0)):
    """
    The open eye: a black rim, white, a coloured iris, a black pupil (the glint is a patch of its own). Iris and pupil
    are circles about the patch's middle, so their edges run along its rings and stay crisp.
    """
    def eye(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(INK, (len(u), 1))
        c[r < rim] = WHITE
        c[((u - iris_c[0]) / iris_r[0]) ** 2 + ((v - iris_c[1]) / iris_r[1]) ** 2 < 1] = iris
        c[((u - pupil_c[0]) / pupil_r[0]) ** 2 + ((v - pupil_c[1]) / pupil_r[1]) ** 2 < 1] = INK
        c[r >= rim] = INK
        return c
    return eye


def glint(k, E, de, half, at=(-0.2, 0.42), size=0.24):
    """A round white glint, a patch of its own over the eye (toward the nose and up), on both eyes."""
    right = np.cross((0, 0, 1.0), de); right /= np.linalg.norm(right)
    up = np.cross(de, right)
    g = E + right * at[0] * half[0] + up * at[1] * half[1]
    k.decal(g, de, (half[0] * size, half[0] * size * 1.1), "#FFFFFF", eps=0.011, rings=5, segs=20, mirror=True)


def mouth_look(fangs=True):
    """A wide open smile seen from the front: dark red inside, a pink tongue low in it, a white fang each upper corner."""
    dark, tongue, lip = srgb(MOUTH), srgb(TONGUE), srgb(LIP)
    def look(u, v):
        c = np.tile(dark, (len(u), 1))
        c[((u) / 0.62) ** 2 + ((v + 0.62) / 0.5) ** 2 < 1] = tongue
        if fangs:
            for s in (-1, 1):
                fu = s * 0.58
                c[(v < 0.34) & (v > 0.34 - 0.7 * (1 - np.abs(u - fu) / 0.17)) & (np.abs(u - fu) < 0.17)] = WHITE
        return c
    return look


def fangs(k, c, n, half, at=0.58, v=-0.04, size=(0.012, 0.018)):
    """A white fang hanging from each upper corner of a mouth patch (c, n, half): a triangle patch of its own over it,
    so its edges are crisp."""
    n = unit(*n)
    right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right)
    up = np.cross(n, right)
    p = np.array(c, float) + right * at * half[0] + up * v * half[1]
    k.decal(p, n, size, "#FFFFFF", eps=0.0075, outline=polygon([(-1, 1), (1, 1), (0, -1)]), rings=6, segs=48,
            mirror=True)


SMILE = [(-1.0, 0.55), (-0.7, 0.32), (-0.3, 0.28), (0.3, 0.28), (0.7, 0.32), (1.0, 0.55), (0.82, 0.0), (0.5, -0.55),
         (0.0, -0.8), (-0.5, -0.55), (-0.82, 0.0)]


def charmander(src, out):
    k = Kit(src, coat="scales")

    # The head: an egg a little taller than wide, the snout bulging forward at its foot
    H, HR = np.array([0.0, -0.035, 0.575]), np.array([0.142, 0.135, 0.158])
    k.add("head", ell(H, HR), ORANGE, "head")
    k.add("snout", ell((0, -0.1, 0.495), (0.112, 0.098, 0.078)), ORANGE, "head")
    k.add("cheek", ell((0, -0.04, 0.47), (0.128, 0.11, 0.075)), ORANGE, "head")

    # The body: a short pear, wider at the hips, the head sitting on it with hardly a neck
    k.add("torso", ell((0, -0.005, 0.3), (0.118, 0.105, 0.15)), ORANGE, "body")
    k.add("hips", ell((0, 0.01, 0.2), (0.135, 0.118, 0.105)), ORANGE, "body")
    # The belly: a cream oval down the front, a patch so that its edge is crisp
    k.decal((0, -0.11, 0.27), (0, -1, 0), (0.088, 0.135), CREAM, eps=0.003, rings=16, segs=56)

    # The arms: short, the upper arm down along the side, the forearm forward, the hands in front of the belly's edge
    # at the hips' height, three small fingers pointing forward
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"arm{side}", tube(np.array([(0.1, -0.02, 0.35), (0.152, -0.045, 0.295), (0.14, -0.12, 0.262)]) * m,
                                 [0.04, 0.035, 0.031]), ORANGE, f"arm{side}")
        HC = np.array((0.135, -0.148, 0.255))
        k.add(f"hand{side}", ell(HC * m, (0.03, 0.028, 0.026)), ORANGE, f"arm{side}")
        for j, d in enumerate([(0.35, -1, 0.45), (0.1, -1, 0.0), (-0.15, -1, -0.45)]):
            d = unit(*d)
            b = HC + d * 0.022
            k.add(f"finger{side}{j}", tube(np.array([b, b + d * 0.022]) * m, [0.011, 0.009]), ORANGE, f"arm{side}")

    # The legs: thick thighs fused into the hips, short shins, broad feet with three white claws
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array((0.095, -0.005, 0.17)) * m, (0.074, 0.08, 0.085)), ORANGE, f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.1, -0.012, 0.12), (0.106, -0.022, 0.055)]) * m, [0.058, 0.054]), ORANGE,
              f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.106, -0.048, 0.033)) * m, (0.06, 0.086, 0.034)), ORANGE, f"leg{side}")
        for j, dx in enumerate((-0.03, 0.0, 0.03)):
            b = np.array((0.106 + dx, -0.118, 0.022))
            k.add(f"toe{side}{j}", claw(b * m, np.array((dx * 0.6, -1, -0.35)) * m, 0.032, 0.022, 0.018), CLAW,
                  f"leg{side}", fuse=False, surface="shell")

    # The tail: thick at the hips, back along the ground and curling up on the model's +X side to its flame
    k.add("tail", tube([(0, 0.08, 0.2), (0.012, 0.2, 0.125), (0.06, 0.31, 0.105), (0.118, 0.385, 0.15),
                        (0.15, 0.42, 0.225)], [0.072, 0.055, 0.042, 0.033, 0.028]), ORANGE, "tail")
    flame(k, (0.15, 0.42, 0.235), 1.0)

    # The mouth: one open smile on the front of the snout, under it the lip of the chin
    MC, MN, MH = (0, -0.2, 0.468), (0, -1, -0.2), (0.082, 0.046)
    k.decal(MC, unit(*MN), MH, mouth_look(fangs=False), eps=0.004, outline=polygon(SMILE), rings=14, segs=64)
    fangs(k, MC, MN, MH)
    # The nostrils: two small dark dots on the top of the snout
    k.decal((0.024, -0.19, 0.53), unit(0.15, -1, 0.45), (0.0085, 0.0065), NOSTRIL, eps=0.004, rings=4, segs=16,
            mirror=True)

    # The eyes: large upright ovals high on the sides of the face, turned a little out
    de = unit(0.6, -0.78, 0.14)
    E = on_ell(H, HR, de) + (0, 0, 0.0)
    half = (0.034, 0.056)
    eye = eye_open(TEAL)
    LID = srgb(ORANGE)
    def eyes(state):
        k.decal(E, de, half, eye_look(state, eye, LID, INK, line=0.15), eps=0.007, rings=20, segs=56, mirror=True)
        if state == "Open":
            glint(k, E, de, half, at=(-0.15, 0.45), size=0.27)

    k.build(voxel=0.007, smooth=4)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    charmander(*args())
