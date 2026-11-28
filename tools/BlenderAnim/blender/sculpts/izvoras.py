# Izvoras (Carpathia No. 10101, the little spring) sculpted from nothing on the skeleton and clips of the game's own
# export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/izvoras.py -- <the export's Izvoras.glb> <out.glb>
#
# From its picture (docs/art/reference/gemini/carpathia/10101-Izvoras.jpg) and its brief (CP-dex-2b-mountains-more.md,
# 101): a round drop of clear spring water sitting on a mossy grey boulder. The drop is as wide as it is tall, blue at
# its rim and turquoise in its middle, glossy, a pale wavy ripple round its crown; its top draws up into a wave that
# rises and curls over toward +X. Two dark navy oval eyes, set fairly close, and no mouth: only a wavy water line low
# across its front. A few bubbles cling to its +X side. The boulder under it is rounded grey stone, a little wider
# than the drop, flecked with moss on its top and a tuft at its +X front foot (the brief's base, kept on `root`).
# The line's frame for Izvorana (izvorana.py): the palette, on_ell and the way the eyes are made (navy ovals with one
# glint, the lid the water's own colour).
# Bones: root (0, 0, 0), body (0, 0, 0.307), crest (0.02, 0.01, 0.407).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, outward, srgb, inside, smoothstep, unit, eye_look, facets, lumpy, polygon, args

# --- the line's palette ----------------------------------------------------------------------------------------------
WATER, DEEP, TEAL, PALE, GLINT = "#5CB9E4", "#3A8FCF", "#60CCD4", "#BDEBF6", "#F4FCFF"
NAVY, INK = "#22386E", "#1A2A55"
STONE, STONEDARK, MOSS, MOSSDARK = "#8B8E88", "#6C6F6A", "#6E9A42", "#4F7A30"

# --- the frame ---------------------------------------------------------------------------------------------------
DC, DR = np.array([0.0, 0.0, 0.3]), np.array([0.152, 0.138, 0.134])       # the drop
SC, SR = np.array([0.0, 0.012, 0.102]), np.array([0.178, 0.152, 0.104])   # the boulder
EYE_AZ, EYE_EL, EYE_HALF = 22.0, 6.0, (0.0175, 0.026)
VOXEL = 0.0045


def on_ell(c, r, az, el, lift=0.0):
    """The point of an ellipsoid at an azimuth from the front (+ toward +X) and an elevation, in degrees, and its
    outward normal there."""
    a, e = math.radians(az), math.radians(el)
    d = np.array([math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)])
    p = np.array(c, float) + np.array(r, float) * d
    n = d / np.array(r, float); n /= np.linalg.norm(n)
    return p + n * lift, n


def dot_eye(white_glint=True):
    """The line's open eye: a navy oval with a darker lower half and one white glint high toward the outside."""
    Nv, Ik, Gl = srgb(NAVY), srgb(INK), srgb(GLINT)
    def look(u, v):
        c = np.tile(Nv, (len(u), 1))
        c[v < -0.35] = Ik
        if white_glint:
            c[((u - 0.28) / 0.26) ** 2 + ((v - 0.38) / 0.22) ** 2 < 1] = Gl
        return c
    return look


def lay_eyes(k, c, r, az, el, half, lid, state, bone_lift=0.0, eps=0.0022, line=0.17):
    """Both eyes in a state, mirrored by hand so that +u is outward on both."""
    look = eye_look(state, dot_eye(), srgb(lid), srgb(INK), line=line)
    for s in (1, -1):
        p, n = on_ell(c, r, az * s, el, bone_lift)
        col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
        k.decal(p, n, half, col, eps=eps, rings=20, segs=56)


def drop(k):
    k.add("drop", ell(DC, DR), WATER, "body", surface="shell")
    # the bottom settles a little onto the stone
    k.add("dropfoot", ell(DC + (0, 0.0, -0.075), (0.13, 0.118, 0.07)), WATER, "body", surface="shell")
    # the top draws up into the wave: a neck rising off the crown, leaning toward +X
    k.add("crown", tube([(0.0, 0.004, 0.37), (0.02, 0.004, 0.415), (0.05, 0.004, 0.452)], [0.074, 0.05, 0.034]),
          WATER, "crest", surface="shell")
    # the wave itself, rising and curling over toward +X
    k.add("wave", tube([(0.045, 0.004, 0.448), (0.08, 0.004, 0.484), (0.12, 0.004, 0.497), (0.153, 0.004, 0.484),
                        (0.164, 0.004, 0.458), (0.15, 0.004, 0.44)], [0.033, 0.027, 0.022, 0.018, 0.014, 0.011]),
          WATER, "crest", surface="shell")
    # a small second curl off the wave's underside, as the picture's lip of water
    k.add("lip", tube([(0.1, 0.004, 0.47), (0.118, 0.004, 0.452), (0.128, 0.004, 0.44)], [0.013, 0.011, 0.009]),
          WATER, "crest", surface="shell")


def colours(k):
    """The water's look: blue at the rim and behind, turquoise in the middle of its front under a wavy water line, a
    pale wavy ripple round the crown, a glossy streak high on its -X side."""
    def rim(P):
        q = P - DC
        th = np.abs(np.arctan2(q[:, 0], -q[:, 1]))
        return smoothstep(1.05, 1.55, th) * (P[:, 2] < 0.43) * (P[:, 2] > 0.17)
    k.paint(rim, DEEP, on=["drop"])
    def middle(P):
        q = P - DC
        th = np.arctan2(q[:, 0], -q[:, 1])
        line = -0.055 + 0.009 * np.sin(th * 7.0)              # the wavy water line
        return inside(q[:, 2] - line, 0.003) * inside(np.abs(th) - 1.25, 0.05) * (P[:, 2] > 0.18)
    k.paint(middle, TEAL, on=["drop"])
    def back(P):
        return smoothstep(0.0, 0.12, P[:, 1] - DC[1]) * (P[:, 2] < 0.44) * (P[:, 2] > 0.17)
    k.paint(back, DEEP, on=["drop"])
    def ripple(P):
        q = P - DC
        th = np.arctan2(q[:, 0], -q[:, 1])
        edge = 0.108 + 0.01 * np.sin(th * 5.0 + 0.6)
        band = inside(edge - q[:, 2], 0.003) * inside(q[:, 2] - edge - 0.011, 0.003)
        return band
    k.paint(ripple, PALE, on=["drop", "crown"])
    def gloss(P):
        q = P - (DC + np.array([-0.075, -0.07, 0.07]))
        return inside(np.linalg.norm(q / np.array([0.045, 0.06, 0.028]), axis=1) - 1, 0.08)
    k.paint(gloss, PALE, on=["drop"])
    def gloss_back(P):
        q = P - (DC + np.array([0.07, 0.08, 0.06]))
        return inside(np.linalg.norm(q / np.array([0.04, 0.05, 0.026]), axis=1) - 1, 0.08)
    k.paint(gloss_back, PALE, on=["drop"])
    def wavetop(P):
        return smoothstep(0.47, 0.5, P[:, 2]) * (P[:, 0] > 0.04)
    k.paint(wavetop, PALE, on=["wave"])


def bubbles(k):
    """Bubbles clinging to the +X side, pale and round, standing just out of the surface."""
    for i, (az, el, rr) in enumerate(((62, 18, 0.019), (80, -5, 0.015), (52, -22, 0.013), (-70, 10, 0.014))):
        p, n = on_ell(DC, DR, az, el)
        k.add(f"bubble{i}", outward(ell(p + n * rr * 0.35, (rr, rr, rr), sub=3)), PALE, "body", fuse=False,
              surface="shell")


def stone(k):
    k.add("stone", ell(SC, SR, deform=facets(14, 0.55, 10101, depth=(0.86, 1.0))), STONE, "root", surface="shell")
    k.add("stonetop", ell(SC + (0.0, 0.0, 0.05), (0.15, 0.125, 0.06), deform=facets(9, 0.4, 7, depth=(0.9, 1.0))),
          STONE, "root", surface="shell")
    def cracks(P):
        # a few long irregular cracks across the stone's face, not a pattern
        res = np.zeros(len(P))
        for a, b, c0 in ((0.9, 0.35, 0.02), (-0.7, 0.6, -0.03), (0.2, -1.0, 0.05)):
            line = a * P[:, 0] + b * P[:, 1] + 0.6 * (P[:, 2] - SC[2]) - c0 + 0.012 * np.sin(P[:, 0] * 60)
            res = np.maximum(res, inside(np.abs(line) - 0.004, 0.002))
        return res * (P[:, 2] < 0.16) * (P[:, 2] > 0.03)
    k.paint(cracks, STONEDARK, on=["stone"])
    def lower(P):
        return smoothstep(0.06, 0.0, P[:, 2])
    k.paint(lower, STONEDARK, on=["stone"])
    # moss flecks on the top and down the sides, more on +X and the front
    patches = [((0.1, -0.1, 0.175), 0.04), ((-0.11, -0.07, 0.172), 0.035), ((0.15, 0.03, 0.16), 0.032),
               ((-0.15, 0.04, 0.15), 0.03), ((0.03, -0.14, 0.12), 0.024), ((0.09, 0.11, 0.17), 0.03),
               ((-0.06, 0.12, 0.17), 0.028), ((0.17, -0.06, 0.08), 0.022), ((-0.13, -0.11, 0.07), 0.02)]
    def moss(P):
        res = np.zeros(len(P))
        for j, (c, r) in enumerate(patches):
            q = P - np.array(c)
            ang = np.arctan2(q[:, 1], q[:, 0])
            rr = r * (1 + 0.25 * np.sin(ang * 5 + j))
            res = np.maximum(res, inside(np.linalg.norm(q, axis=1) - rr, 0.003))
        return res
    k.paint(moss, MOSS, on=["stone"])
    # tufts: low lumps of moss round the drop's foot and one at the +X front of the stone
    for j, (x, y, z, r) in enumerate(((0.11, -0.09, 0.19, 0.026), (-0.12, -0.06, 0.19, 0.024), (0.14, 0.05, 0.18, 0.024),
                                      (-0.08, 0.1, 0.19, 0.022), (0.03, 0.13, 0.195, 0.02))):
        k.add(f"tuft{j}", outward(ell((x, y, z), (r * 1.3, r * 1.2, r * 0.75), deform=lumpy(4, 3, 0.25), sub=3)),
              MOSS if j % 2 == 0 else MOSSDARK, "root", fuse=False, surface="leaf")
    p, n = on_ell(SC, SR * 0.92, 38, -35)
    k.add("tuftfoot", outward(ell(p + n * 0.006, (0.034, 0.03, 0.026), deform=lumpy(5, 3, 0.3), sub=3)),
          MOSS, "root", fuse=False, surface="leaf")


def board(ob):
    V = np.array([v.co[:] for v in ob.data.vertices])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    sq = 1.3 * max(h, 0.85 * max(w, l))
    print("KIT: board: shows %.3f (corners %.3f) in a square of %.3f" % (np.ptp(proj), 0.853 * w + 0.522 * l, sq))


def izvoras(src, out):
    k = Kit(src, coat="shell")
    drop(k)
    stone(k)
    colours(k)
    bubbles(k)
    k.build(voxel=VOXEL, smooth=4, band=0.005, wband=0.04)
    board(k.ob)
    k.eye_states(lambda state: lay_eyes(k, DC, DR, EYE_AZ, EYE_EL, EYE_HALF, WATER, state))
    k.export(out)


if __name__ == "__main__":
    izvoras(*args())
