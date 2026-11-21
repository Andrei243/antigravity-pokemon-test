# Eldegoss sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/eldegoss.py -- <the export's Eldegoss.glb> <out.glb>
#
# Grown from Gossifleur's script (sibling("gossifleur")): the same small cream face with the crown's colour coming
# down to a point between the eyes, the same way of laying eyes and the smile. From the references (they overrule the
# brief, which put the cotton on its head: the cotton is a great ball BEHIND the whole body): a great round ball of
# white cotton, dotted with brown seeds, standing behind it from the ground to over its head, its largest mass by far;
# in front of it a little figure: a cream face framed by a crown of eight pointed olive-yellow leaves, two dark marks
# high on the forehead; sleepy almond eyes, brown-orange in a dark rim with a white glint, their top edge flat; a
# small smile; a wrap of yellow leaves round its neck like a scarf, tied in front; an olive body and below it a skirt
# of broad dark olive leaves fanning out to the ground.
# Reference ratios (3D picture): the ball 0.86 of the height wide and 0.84 of it tall; the face 0.18 of the height
# across, the crown 0.46 of it; the scarf and skirt 0.38 of the height wide.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, inside, unit, eye_look, args, lumpy, polygon, sibling

gf = sibling("gossifleur")
sk = sibling("skiddo")
on_ell = gf.on_ell

COTTON, COTTON_LOW = "#F6F4EA", "#E6DEC2"
SEED = "#BE7630"
CROWN, CROWN_DARK = "#C8C024", "#6E7A1E"
FACE = "#F6F2C2"
SCARF, SCARF_DEEP = "#F2CC14", "#E0B40C"
BODY, SKIRT, SKIRT2 = "#8E9C30", "#6C8A28", "#7A962C"
IRIS, RIM, GLINT = srgb("#C06A1E"), srgb("#4E2A10"), srgb("#FFFFFF")
INK = srgb("#4E2A10")

# --- Eldegoss's frame (joints: body (0, 0, 0.3), head (0, -0.15, 0.18)) -----------------------------------------
BALL, BALL_R = np.array([0.0, 0.025, 0.35]), np.array([0.25, 0.2, 0.248])
HC, HR = np.array([0.0, -0.21, 0.228]), np.array([0.068, 0.058, 0.063])
EYE_D = (0.52, -0.84, 0.06)
EYE_HALF = (0.022, 0.0168)


def almond_eye(lid_side):
    """The sleepy eye: brown-orange in a dark rim, a white glint high toward the nose; its top cut flat by the lid."""
    def look(u, v):
        r = np.sqrt(u * u + v * v)
        col = np.tile(IRIS, (len(u), 1))
        col[((u + 0.3) / 0.24) ** 2 + ((v - 0.05) / 0.26) ** 2 < 1] = GLINT
        col[r > 0.8] = RIM
        col[v > 0.32 + 0.12 * u - 0.17] = RIM          # the lid's dark line along the flat top
        return col
    return look


# the flat top of the eye: a line a little higher at the outer corner, the round below
LID = [(-1.3, 0.28), (1.3, 0.48), (1.3, -1.3), (-1.3, -1.3)]


def lay_eyes(k, state, lid=FACE):
    """The two eyes laid by hand (their outlines are not symmetric): u outward on both."""
    L = srgb(lid)
    for s in (1, -1):
        d = np.array(EYE_D) * np.array([s, 1, 1.0])
        p, nrm = on_ell(HC, HR, d)
        cut = polygon([(a * s, b) for a, b in LID])
        look = eye_look(state, almond_eye(s), L, INK, line=0.2)
        col = (lambda f, s: (lambda u, v: f(u * s, v)))(look, s)
        k.decal(p, nrm, EYE_HALF, col, eps=0.0018, rings=20, segs=56, outline=lambda th, c=cut: min(1.0, c(th)))


def eldegoss(src, out_path):
    k = Kit(src, coat="fur")
    # --- the great ball of cotton behind, dotted with seeds ----------------------------------------------------
    k.add("cotton", ell(BALL, BALL_R, deform=lumpy(9, 7, 0.018)), COTTON, "body")
    k.paint(lambda P: inside(P[:, 2] - 0.2, 0.08) * 0.7, COTTON_LOW, on=["cotton"])

    # --- the olive body, the scarf of yellow leaves round the neck, the skirt of leaves to the ground ----------
    k.add("body", ell((0, -0.15, 0.1), (0.08, 0.07, 0.08)), BODY, "body")
    k.add("scarf", ell((0, -0.17, 0.158), (0.1, 0.08, 0.05)), SCARF, "body")
    for i, a in enumerate(np.linspace(-140, 140, 6)):
        r = math.radians(a)
        o = np.array([math.sin(r), -math.cos(r), 0.0])
        root = np.array([0, -0.165, 0.162]) + o * np.array([0.07, 0.058, 0])
        sk.spike(k, f"scarfleaf{i}", root, o + np.array([0, 0, -0.5]), 0.08, 0.05, 0.013, "body",
                 SCARF if i % 2 else SCARF_DEEP, surface="leaf")
    # the knot in front: two short leaves hanging down from under the chin
    for i, (x, d) in enumerate(((0.012, (0.35, -0.4, -1)), (-0.012, (-0.35, -0.4, -1)))):
        sk.spike(k, f"knot{i}", (x, -0.244, 0.16), d, 0.06, 0.024, 0.01, "body", SCARF_DEEP, side=(1, 0, 0),
                 surface="leaf")
    for i, a in enumerate(np.linspace(-125, 125, 7)):
        r = math.radians(a)
        o = np.array([math.sin(r), -math.cos(r), 0.0])
        root = np.array([0, -0.13, 0.08]) + o * np.array([0.056, 0.05, 0])
        sk.spike(k, f"skirt{i}", root, o + np.array([0, 0, -0.6]), 0.14, 0.056, 0.014, "body",
                 SKIRT if i % 2 == 0 else SKIRT2, surface="leaf", mid=0.5, belly=0.8)

    # --- the face, framed by the crown of leaves ----------------------------------------------------------------
    gf.face(k, HC, HR, color=FACE)
    k.add("crownback", ell(HC + (0, 0.026, 0.014), (0.066, 0.05, 0.06)), CROWN, "head")
    gf.forehead_point(k, HC, HR, CROWN, 0.04, 1.0, on=("face", "crownback"))
    gf.point_patch(k, HC, HR, (0, -0.86, 0.5), (0.015, 0.016), CROWN)
    for i, a in enumerate((0, 38, -38, 76, -76, 112, -112, 180)):
        r = math.radians(a)
        d = np.array([math.sin(r), 0.22, math.cos(r)])
        root = HC + np.array([0, 0.02, 0]) + unit(*d) * 0.048
        L = 0.135 if abs(a) < 80 else 0.112
        sk.spike(k, f"crown{i}", root, d, L, 0.04, 0.014, "head", CROWN, side=np.cross(unit(*d), (0, 1.0, 0)),
                 surface="leaf", mid=0.32, belly=0.8)
    # the two dark marks high on the forehead
    for s in (1, -1):
        p, nrm = on_ell(HC, HR, (0.42 * s, -0.72, 0.56))
        k.decal(p, nrm, (0.005, 0.0125), CROWN_DARK, eps=0.0018, rings=8, segs=36,
                up=(-0.5 * s, 0, 1))
    gf.smile(k, HC, HR, (0, -1, -0.38), (0.014, 0.0072), FACE, ink="#5A3A1A")

    # --- the seeds on the cotton, where the figure doesn't stand in front ----------------------------------------
    rs = np.random.RandomState(11)
    golden = math.pi * (3 - math.sqrt(5))
    n = 34
    for i in range(n):
        z = 1 - 2 * (i + 0.5) / n
        rr = math.sqrt(max(1 - z * z, 0))
        th = golden * i
        d = np.array([rr * math.cos(th), rr * math.sin(th), z])
        if z < -0.55 or (d[1] < -0.3 and abs(d[0]) < 0.6 and z < 0.35):
            continue
        p, nrm = on_ell(BALL, BALL_R, d)
        a = rs.uniform(0, math.pi)
        k.decal(p, nrm, (0.0085, 0.015), SEED, eps=0.0014, rings=6, segs=24,
                up=np.cross(nrm, (math.cos(a), 0.3, math.sin(a))) + nrm * 0 + np.array([0, 0, 1e-3]))

    k.build(voxel=0.0055, smooth=4, band=0.006, wband=0.06)
    k.eye_states(lambda state: lay_eyes(k, state))
    k.export(out_path)


if __name__ == "__main__":
    eldegoss(*args())
