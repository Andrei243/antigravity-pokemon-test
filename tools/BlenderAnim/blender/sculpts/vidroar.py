# Vidroar sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/vidroar.py -- <the export's Vidroar.glb> <out.glb>
#
# Carpathia No. 10148, a wrestling river otter (CP-dex-3b-river-and-caves-more.md, 148; picture 10148-Vidroar.jpg).
# Grown from vidrut.py (sibling("vidrut")): the line's glossy brown, its cream face, throat and belly, its blue fin
# ears, its blue two-coloured fin at the tail's end, its dark nose and its short whiskers; the eyes are open now.
# The masses, largest first (picture): the hunched shoulders and deep chest (the shoulders 0.68 of the height across);
# the thick tail curving down to the ground and up again into a broad blue fan of a fin (0.3 of the height tall);
# great thighs on short bent shins and broad clawed feet in a wide braced stance; the head (0.25 of the height,
# 0.45 of the shoulders' span) sunk between the shoulders, with a cream muzzle and cheeks, heavy glowering brows over
# dark eyes, a smirk with two small fangs; fists raised in a wrestler's guard (the lead fist on +X before the
# shoulder, the other across the chest), both under the chin; blue claws on the feet and fists.
# Skeleton: body (0, 0, 0.5), arms (+-0.2, -0.02, 0.66), head (0, -0.03, 0.74), legs (+-0.11, 0, 0.32),
# tail (0, 0.14, 0.32). Export about 1.0 tall.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, hull, srgb, inside, unit, eye_look, polygon, args, sibling

vr = sibling("vidrut")
BROWN, CREAM, FIN, FIN_DARK, NOSE, INK = "#54423A", vr.CREAM, vr.FIN, vr.FIN_DARK, vr.NOSE, vr.INK
BROW = "#3A2C26"
PUPIL, GLINT = srgb("#1A1410"), srgb("#FFFFFF")
FANG = "#F6F2E8"

FACE = unit(0, -1, 0.06)
HUP = unit(0, 0.06, 1)
X = np.array([1.0, 0, 0])
HC = np.array([0.0, -0.075, 0.862])
HR = np.array([0.148, 0.132, 0.126])


def hd(a, f, u):
    return a * X + f * FACE + u * HUP


MC = HC + FACE * 0.112 - HUP * 0.045
MR = np.array([0.084, 0.064, 0.058])


def claws(k, name, centre, forward, across, n, r, bone):
    """A row of small blue claws standing forward out of a paw (loose hulls)."""
    forward = unit(*forward); across = unit(*across)
    up = np.cross(forward, across)
    for i in range(n):
        p = np.array(centre, float) + across * (i - (n - 1) / 2) * r * 2.3
        pts = [p + across * r, p - across * r, p + up * r * 0.8, p - up * r * 0.8, p + forward * r * 2.2 - up * r * 0.4]
        k.add(f"{name}{i}", hull(pts), FIN, bone, fuse=False, surface="shell")


def vidroar(src, out):
    k = Kit(src, coat="fur")
    # The trunk: a deep chest under hunched shoulders, the belly, the hips
    k.add("chest", ell((0, -0.03, 0.63), (0.215, 0.16, 0.16)), BROWN, "body")
    k.add("belly", ell((0, -0.035, 0.47), (0.18, 0.15, 0.16)), BROWN, "body")
    k.add("hips", ell((0, 0.02, 0.34), (0.19, 0.15, 0.12)), BROWN, "body")
    k.add("hunch", ell((0, 0.02, 0.76), (0.17, 0.12, 0.085)), BROWN, "body")
    k.add("throat", ell((0, -0.125, 0.745), (0.095, 0.07, 0.075)), CREAM, "head")
    k.add("head", ell(HC, HR), BROWN, "head")
    k.add("muzzle", ell(MC, MR), CREAM, "head")
    for i, (a, f, u, r) in enumerate(((0.7, 0.55, -0.35, 0.066), (0.55, 0.45, -0.7, 0.058))):
        p, n = vr.on_ell(HC, HR, hd(a, f, u))
        k.add(f"cheek{i}", ell(p - n * 0.03, (r, r * 0.9, r * 0.85)), CREAM, "head", mirror=True)
    ep, en = vr.on_ell(HC, HR, hd(0.74, 0.0, 0.66))
    vr.fin_ear(k, ep - en * 0.008, unit(*(en * 0.6 + HUP * 0.6 + X * 0.15)), FACE + X * 0.2, 0.12, 0.018, "head",
               stub=BROWN)

    # The shoulders' great masses, the arms bent, fists in a guard under the chin
    k.add("delt", ell((0.235, -0.02, 0.7), (0.115, 0.115, 0.115)), BROWN, "arm{s}", mirror=True)
    k.add("uparmR", tube([(0.26, -0.03, 0.66), (0.31, -0.1, 0.55), (0.3, -0.16, 0.5)], [0.085, 0.078, 0.072]), BROWN, "armR")
    k.add("forearmR", tube([(0.3, -0.16, 0.5), (0.26, -0.24, 0.56), (0.21, -0.27, 0.615)], [0.072, 0.07, 0.064]), BROWN, "armR")
    k.add("fistR", ell((0.2, -0.29, 0.638), (0.074, 0.07, 0.072)), BROWN, "armR")
    k.add("uparmL", tube([(-0.26, -0.03, 0.66), (-0.31, -0.1, 0.54), (-0.27, -0.17, 0.47)], [0.085, 0.078, 0.072]), BROWN, "armL")
    k.add("forearmL", tube([(-0.27, -0.17, 0.47), (-0.19, -0.24, 0.51), (-0.1, -0.26, 0.55)], [0.072, 0.07, 0.064]), BROWN, "armL")
    k.add("fistL", ell((-0.075, -0.275, 0.56), (0.074, 0.07, 0.072)), BROWN, "armL")
    claws(k, "fistclawR", (0.2, -0.35, 0.62), (0, -1, -0.3), (1, 0, 0), 3, 0.011, "armR")
    claws(k, "fistclawL", (-0.075, -0.335, 0.545), (0, -1, -0.3), (1, 0, 0), 3, 0.011, "armL")

    # Great thighs fused into the hips, short bent shins, broad feet in a wide stance
    k.add("thigh", ell((0.15, -0.0, 0.27), (0.115, 0.125, 0.145), rot=(10, 0, -8)), BROWN, "leg{s}", mirror=True)
    k.add("shin", tube([(0.18, -0.02, 0.18), (0.205, -0.02, 0.1), (0.21, -0.04, 0.05)], [0.08, 0.07, 0.062]), BROWN,
          "leg{s}", mirror=True)
    k.add("foot", ell((0.215, -0.1, 0.036), (0.085, 0.13, 0.036)), BROWN, "leg{s}", mirror=True)
    for j in range(3):
        k.add(f"toe{j}", ell((0.215 + (j - 1) * 0.045, -0.21, 0.032), (0.028, 0.034, 0.03)), BROWN, "leg{s}", mirror=True)
    for s, bone in ((1, "legR"), (-1, "legL")):
        claws(k, f"toeclaw{s}", (0.215 * s, -0.245, 0.03), (0, -1, -0.15), (1, 0, 0), 3, 0.012, bone)

    # The thick tail: down to the ground, back and out to +X, and up into the fin
    H = unit(0.57, 0.82, 0)
    T0 = np.array([0, 0.13, 0.33])
    pts = [T0, T0 + H * 0.1 + np.array([0, 0, -0.14]), T0 + H * 0.24 + np.array([0, 0, -0.25]),
           T0 + H * 0.37 + np.array([0, 0, -0.21]), T0 + H * 0.45 + np.array([0, 0, -0.1]),
           T0 + H * 0.48 + np.array([0, 0, 0.0])]
    k.add("tail", tube(pts, [0.1, 0.085, 0.07, 0.056, 0.044, 0.034]), BROWN, "tail")
    TIP = pts[-1] - np.array([0, 0, 0.02])
    # The fin: a fan of five plates of the two blues, in the plane of the tail's run and the vertical
    R = 0.27
    for i in range(5):
        a0, a1 = math.radians(28 + i * 17), math.radians(28 + (i + 1) * 17)
        outline = [(0, 0)] + [(R * (1 - 0.06 * math.sin(math.pi * t)) * math.cos(a0 + (a1 - a0) * t),
                               R * (1 - 0.06 * math.sin(math.pi * t)) * math.sin(a0 + (a1 - a0) * t))
                              for t in np.linspace(0, 1, 5)]
        k.add(f"fin{i}", slab(outline, TIP, H, (0, 0, 1), 0.022 if i % 2 else 0.026), FIN if i % 2 == 0 else FIN_DARK,
              "tail", fuse=False, surface="scales")

    # Whiskers, the nose, the fangs
    wp, wn = vr.on_ell(MC, MR, hd(0.75, 0.55, 0.1))
    vr.whiskers(k, [wp - wn * 0.004, wp - wn * 0.004 - HUP * 0.02],
                [wn + X * 0.5 + HUP * 0.15, wn + X * 0.5 - HUP * 0.12], 0.075, 0.0075, "head")
    npt, nn = vr.on_ell(MC, MR, hd(0, 0.82, 0.55))
    k.add("nose", ell(npt + nn * 0.004, (0.03, 0.021, 0.022), rot=(-15, 0, 0)), NOSE, "head", fuse=False, surface="skin")
    mp, mn = vr.on_ell(MC, MR, hd(0, 0.55, -0.83))
    for s in (1, -1):
        f0 = mp + X * 0.024 * s + HUP * 0.002
        k.add(f"fang{s}", hull([f0 + X * 0.008, f0 - X * 0.008, f0 + mn * 0.006, f0 - mn * 0.008,
                                f0 - HUP * 0.022 + mn * 0.002]), FANG, "head", fuse=False, surface="shell")

    # Paint: the cream bib (throat, chest and belly in front), the lower face
    def bib(P):
        z = P[:, 2]
        w = np.interp(z, [0.22, 0.3, 0.45, 0.6, 0.72, 0.8], [0.0, 0.1, 0.13, 0.12, 0.1, 0.09])
        return inside(np.abs(P[:, 0]) - w, 0.006) * inside(P[:, 1] + 0.07, 0.01) * inside(z - 0.82, 0.006) * inside(0.22 - z)
    k.paint(bib, CREAM, on=["chest", "belly", "hips", "throat", "hunch"])
    def lower_face(P):
        q = P - HC
        return inside(q @ HUP + 0.018, 0.005) * inside(-0.03 - q @ FACE, 0.006) * inside(q[:, 2] + 0.2)
    k.paint(lower_face, CREAM, on=["head", "muzzle", "cheek", "throat"])
    k.paint(lambda P: inside(P[:, 2] - 0.06, 0.004), "#46362E", on=["foot", "toe", "shin"])
    # The smirk
    vr.mouth_line(k, mp + HUP * 0.003, mn, HUP, 0.05, 0.014, 0.0068, shape=lambda t: 0.6 * t * t - 0.1 * t, dots=13)
    # Dark eyes cut by a heavy brow sloping down toward the nose; the brows are in every state
    ep, en = vr.on_ell(HC, HR, hd(0.4, 0.84, 0.36))
    BROW_PTS = [(-1, -0.2), (0, -0.5), (1, 0.1), (0.9, 0.7), (0, 0.4), (-0.9, 0.3)]
    brows(k, ep + HUP * 0.03 + np.array([0.004, 0, 0]), en, BROW_PTS)
    k.build(voxel=0.0085, smooth=4, band=0.01, wband=0.06)
    vr.board(k.ob)

    PTS = [(-1, -0.15), (-0.75, -0.7), (0, -0.95), (0.75, -0.7), (1, -0.05), (0.85, 0.55), (0.35, 0.5), (-0.4, 0.2),
           (-0.9, 0.0)]
    OUT_R, OUT_L = polygon(PTS), polygon([(-a, b) for a, b in PTS])
    LID = srgb(BROWN)
    def open_look(u, v):
        c = np.tile(PUPIL, (len(u), 1))
        c[((u - 0.25) / 0.28) ** 2 + ((v + 0.05) / 0.28) ** 2 < 1] = GLINT
        return c
    def fierce(u, v):
        c = open_look(u, v)
        c[v > -0.1 + 0.45 * u] = LID
        return c
    def lay(state):
        look = fierce if state == "Fierce" else eye_look(state, open_look, LID, PUPIL, line=0.2)
        k.decal(ep, en, (0.03, 0.032), look, up=HUP, eps=0.0034, outline=OUT_R, rings=20, segs=56)
        m = np.array([-1, 1, 1.0])
        k.decal(ep * m, en * m, (0.03, 0.032), lambda u, v: look(-u, v), up=HUP, eps=0.0034, outline=OUT_L, rings=20,
                segs=56)
    k.eye_states(lay)
    k.export(out)


def brows(k, bp, en, pts):
    """The heavy brow over each eye, sloping down toward the nose (in every state: laid with each state's eyes)."""
    m = np.array([-1, 1, 1.0])
    k.decal(bp, en, (0.04, 0.016), BROW, up=HUP, eps=0.0042, outline=polygon(pts), rings=6, segs=36)
    k.decal(bp * m, en * m, (0.04, 0.016), BROW, up=HUP, eps=0.0042, outline=polygon([(-a, b) for a, b in pts]),
            rings=6, segs=36)


if __name__ == "__main__":
    vidroar(*args())
