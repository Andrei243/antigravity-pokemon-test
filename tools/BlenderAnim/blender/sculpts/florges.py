# Florges (the red flower) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/florges.py -- <the export's Florges.glb> <out.glb>
#
# Grown from Flabébé's and Floette's scripts (sibling("flabébé"), sibling("floette")): the same dark glinting eye, the
# same teal strands over the brows, the same pointed leaves. From the references: a slender lady rising from two great
# teal leaves that sweep up and out from her base like a V (the widest thing about her); her body teal, a mermaid's
# tail drawn in at the waist and the knee and ending in a little split leaf; a black collar under her chin; thin white
# arms; a white face, eyes half shut under green lids; and round her head a great thick wreath of red flowers, the
# largest mass by volume, dotted with small yellow and pink flowers, lilac strands of round buds hanging from its sides
# down to the shoulders and lilac clusters at its top corners.
# (The art flings one arm wide: carried forward and down here. The art shows one eye behind a strand: both are shown.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, args, outward, sibling, lumpy, polygon, eye_look

fb = sibling("flabébé")
fe = sibling("floette")

WHITE_ = fb.WHITE_
TEAL, LEAF_T, LEAF_DARK = "#1CAE8E", "#1E9E80", "#168A6E"
COLLAR = "#2C2A34"
WREATH, WREATH_BACK = "#E24858", "#CE3E4E"
LILAC, LILAC_PALE = "#C48ACC", "#E2B2E2"
SMALL_YELLOW, SMALL_PINK = "#F6D65A", "#F7BCD0"
LID_GREEN = srgb("#24A27E")
EYE_DARK = srgb("#24242E")

# --- Florges's frame (joints: body (0, 0, 0.3), arms (+-0.03, 0, 0.36), head (0, 0, 0.38)) ------------------------
HC = np.array([0.0, -0.012, 0.442])
HR = np.array([0.058, 0.052, 0.06])
EYE_D = (0.38, -0.92, -0.04)
EYE_HALF = (0.019, 0.021)
W = np.array([0.0, 0.024, 0.455])          # the wreath's middle


def lidded_eye(u, v):
    """Half shut: a green lid over the top, a dark eye under it with a small glint."""
    c = np.tile(EYE_DARK, (len(u), 1))
    c[((u - 0.1) / 0.28) ** 2 + ((v + 0.12) / 0.24) ** 2 < 1] = fb.GLINT
    lid = 0.22 - 0.18 * u
    c[v > lid] = LID_GREEN
    return c


def wreath(k):
    """A thick round of lumpy red flowers about the face, a mass behind, small flowers on its front."""
    k.add("wreathback", ell(W + (0, 0.026, 0.006), (0.135, 0.062, 0.108), deform=lumpy(5, 4, 0.07)), WREATH_BACK,
          "head")
    marks = []
    for i, deg in enumerate(range(-140, 141, 28)):
        t = math.radians(deg)
        c = W + np.array([0.122 * math.sin(t), -0.004, 0.1 * math.cos(t)])
        r = 0.052 if abs(deg) < 100 else 0.046
        k.add(f"wreath{i}", ell(c, (r, r * 0.9, r * 0.92), rot=(0, 0, deg * 0.3), deform=lumpy(3, 2, 0.12)), WREATH,
              "head")
        marks.append((deg, c, r))
    # small yellow five-petalled and pink four-petalled flowers on the wreath's front
    five = lambda th: 0.55 + 0.45 * abs(math.cos(2.5 * th))
    four = lambda th: 0.5 + 0.5 * abs(math.cos(2 * th))
    for deg, c, r in marks:
        if deg in (-84, -28, 28, 84, 140, -140, 0, -56, 56):
            n = unit(0.35 * math.sin(math.radians(deg)), -1, 0.35 * math.cos(math.radians(deg)))
            p = c + n * r * 0.82
            yellow = deg in (-84, 28, 140, -28)
            k.decal(p, n, (0.016, 0.016), SMALL_YELLOW if yellow else SMALL_PINK, outline=five if yellow else four,
                    eps=0.0012, rings=10, segs=90)
    # lilac strands of buds down the sides and clusters at the top corners
    for sx in (1, -1):
        for j, (x0, y0, z0, z1) in enumerate(((0.152, 0.0, 0.44, 0.3), (0.118, 0.04, 0.41, 0.32))):
            n = 6 if j == 0 else 5
            for m in range(n):
                f = m / (n - 1)
                r = 0.021 - 0.007 * f
                p = (sx * (x0 + 0.012 * f), y0, z0 + (z1 - z0) * f)
                k.add(f"bud{j}{m}" + ("R" if sx > 0 else "L"), ell(p, (r, r, r), sub=3),
                      LILAC_PALE if m == n - 1 else LILAC, "head")
        for m, (x, y, z) in enumerate(((0.1, 0.02, 0.565), (0.128, 0.03, 0.54), (0.078, 0.04, 0.58), (0.112, 0.05, 0.575))):
            k.add(f"cluster{m}" + ("R" if sx > 0 else "L"), ell((sx * x, y, z), (0.022, 0.022, 0.022), sub=3), LILAC,
                  "head")


def florges(src, out):
    k = Kit(src, coat="leaf")
    # --- the body: a teal mermaid's tail, a black collar, thin white arms ---
    k.add("chest", ell((0, 0.0, 0.33), (0.046, 0.036, 0.05)), TEAL, "body")
    k.add("hips", ell((0, 0.002, 0.25), (0.042, 0.034, 0.05)), TEAL, "body")
    k.add("tail", tube([(0, 0.0, 0.3), (0, -0.004, 0.2), (0.004, -0.004, 0.12), (0, 0.0, 0.05)],
                       [0.036, 0.03, 0.02, 0.013]), TEAL, "body")
    k.add("collar", ell((0, -0.012, 0.372), (0.034, 0.028, 0.024)), COLLAR, "body")
    k.add("neck", tube([(0, -0.006, 0.37), (0, -0.01, 0.41)], [0.016, 0.016]), WHITE_, "head")
    for sx in (1, -1):
        side = "R" if sx > 0 else "L"
        k.add("arm" + side, tube([(0.032 * sx, -0.004, 0.364), (0.064 * sx, -0.024, 0.322), (0.038 * sx, -0.066, 0.33)],
                                 [0.0115, 0.0105, 0.0105]), WHITE_, "arm" + side)
        k.add("hand" + side, ell((0.034 * sx, -0.074, 0.332), (0.012, 0.012, 0.013)), WHITE_, "arm" + side)
    # the little split leaf at the base, and the two great leaves sweeping up and out
    for sx in (1, -1):
        side = "R" if sx > 0 else "L"
        k.add("foot" + side, fb.pt.leaf_blade((0.004 * sx, 0.0, 0.058), (0.65 * sx, -0.15, -1.0), (0.2 * sx, -1.0, 0),
                                               0.06, 0.02, thick=0.014, bend=0.02, cup=0.1), LEAF_DARK, "body",
              fuse=False, surface="leaf")
        k.add("leaf" + side, fb.pt.leaf_blade((0.012 * sx, -0.008, 0.05), (0.57 * sx, -0.06, 0.82), (0.3 * sx, -1.0, 0.0),
                                               0.45, 0.068, thick=0.017, bend=0.03, cup=0.12), LEAF_T, "body",
              fuse=False, surface="leaf")
    # --- the head, its strands and the wreath ---
    k.add("head", ell(HC, HR), WHITE_, "head")
    fe.strands(k, HC, HR, fe.TEAL, "head", size=1.15,
               dirs=((0.1, -0.78, 0.6), (0.3, -0.62, 0.74), (0.52, -0.42, 0.74), (0.7, -0.3, 0.62)))
    wreath(k)
    k.build(voxel=0.0048, smooth=4, band=0.005, wband=0.04)

    def eyes(state):
        if state == "Open":
            look = lidded_eye
        else:
            look = eye_look(state, lidded_eye, srgb(WHITE_), fb.INK, line=0.16)
        p, nrm = fb.on_ell(HC, HR, EYE_D)
        k.decal(p, nrm, EYE_HALF, look, eps=0.002, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    florges(*args())
