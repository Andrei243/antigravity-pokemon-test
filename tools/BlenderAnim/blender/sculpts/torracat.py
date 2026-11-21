# Torracat sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Litten's script (litten.py, imported as sibling("litten"): the same head and front coordinates, ears and lining, cut
# eye, solid cheek tufts, bands round a limb and spiky tail tuft):
#   blender --background --factory-startup --python sculpts/torracat.py -- <the export's Torracat.glb> <out.glb>
# From the references: a bigger, stronger cat, orange-red below and dark slate above. The dark head with tall pointed
# dark ears, each with a small pale mark low on its front; Litten's forehead mark grown into a tall orange horn of fur
# rising from the brow, its crossbar a broad orange band over the eyes, its outer ends turned down; fierce yellow eyes
# with black slit pupils under a dark lid sloping to the nose; the lower face orange with big orange tufts out from
# the cheeks; a small dark nose, a mouth with two small white fangs; a yellow bell of fire at the throat with an
# orange ring in it; the chest, belly and legs orange, the back and the rump's top dark with an orange branching mark
# on each flank; dark bands round the lower legs and dark claws; the tail dark with two orange bands and a big spiky
# dark tuft. (The pictures overrule the task's text and the C# sculpt: the body is orange below and dark above, not
# red with black stripes; the ears are dark, not red horns; the horn on the brow is the forehead mark's.)
# Reference ratios (3D picture, crouched): head without ears / height to the horn's tip 0.4 (0.38 here, standing);
# horn above the head's top / head height 0.6 (0.62 here); tufts' span / head width 1.5 (1.45 here); bell / head
# width 0.27 (0.28 here).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, outward, srgb, inside, smoothstep, unit, polygon, axes, args, sibling)

lt = sibling("litten")

DARK = lt.DARK
ORANGE = "#F05A2C"
EARIN = lt.EARIN
YELLOW, AMBER = lt.YELLOW, lt.AMBER
BELL, BELLRIM = "#FCC82E", "#F28A1E"
CLAW = "#2A2C34"
INK, NOSE, WHITE = lt.INK, lt.NOSE, lt.WHITE

# --- the frame (the skeleton's joints: body (0, 0, 0.17), head (0, -0.09, 0.23), legs (+-0.05, -0.08, 0.14), hind legs
#     (+-0.05, 0.08, 0.14), tail (0, 0.09, 0.19)) -------------------------------------------------------------------
H = np.array([0.0, -0.142, 0.272])
HR = np.array([0.081, 0.07, 0.07])
LEG_X = 0.05
FORE_Y, HIND_Y = -0.08, 0.082
LEG_R = (0.019, 0.016)


def add_body(k):
    k.add("chest", ell((0, -0.06, 0.172), (0.06, 0.062, 0.07)), ORANGE, "body")
    k.add("middle", ell((0, 0.01, 0.166), (0.055, 0.07, 0.064)), ORANGE, "body")
    k.add("rump", ell((0, 0.07, 0.172), (0.06, 0.056, 0.066)), ORANGE, "body")
    k.add("neck", ell((0, -0.1, 0.225), (0.046, 0.044, 0.05)), DARK, "head")
    # dark over the back and the neck's top, the line rising toward the shoulders so the chest stays orange
    def back(P):
        y, z = P[:, 1], P[:, 2]
        line = 0.188 + 0.08 * smoothstep(-0.08, -0.13, y)
        return inside(line - z, 0.0025)
    k.paint(back, DARK, on=["chest", "middle", "rump", "neck", "foretop", "hindtop"])
    # the throat under the chin orange, as the lower face is
    k.paint(lambda P: inside(P[:, 2] - 0.245, 0.003) * inside(P[:, 1] + 0.112, 0.003), ORANGE, on=["neck", "chest"])


def add_legs(k):
    for bone, y, nm in (("leg{s}", FORE_Y, "fore"), ("hind{s}", HIND_Y, "hind")):
        k.add(f"{nm}top", ell((LEG_X - 0.004, y + 0.003, 0.128), (0.032, 0.038, 0.058)), ORANGE, bone, mirror=True)
        k.add(f"{nm}leg", tube([(LEG_X, y, 0.135), (LEG_X + 0.001, y - 0.003, 0.075), (LEG_X + 0.002, y - 0.005, 0.026)],
                               [LEG_R[0], LEG_R[0] * 0.95, LEG_R[1]]), ORANGE, bone, mirror=True)
        k.add(f"{nm}paw", ell((LEG_X + 0.003, y - 0.016, 0.015), (0.022, 0.027, 0.015)), ORANGE, bone, mirror=True)
        lt.add_bands(k, f"{nm}band", (LEG_X + 0.001, y - 0.003, 0.075), (LEG_X + 0.002, y - 0.005, 0.026),
                     (LEG_R[0] * 0.95, LEG_R[1]), (0.15, 0.62), 0.011, color=DARK, bone=bone, thick=0.003)
        # three dark claws along the paw's front
        for j, dx in enumerate((-0.012, 0.0, 0.012)):
            b = np.array((LEG_X + 0.003 + dx, y - 0.038, 0.008))
            k.add(f"{nm}claw{j}", hull([b + (0.004, 0.006, 0.004), b + (-0.004, 0.006, 0.004), b + (0.004, 0.006, -0.004),
                                        b + (-0.004, 0.006, -0.004), b + (0, -0.008, -0.005)]), CLAW, bone,
                  fuse=False, mirror=True, surface="shell")


def add_flank_marks(k):
    """An orange branching mark on each flank's dark: a long stroke along the back and two short ones off it."""
    rect = polygon([(-1, -1), (1, -1), (1, 1), (-1, 1)])
    for s in (1, -1):
        for (y, z, half, tilt) in ((0.02, 0.214, (0.0065, 0.036), -78), (0.004, 0.222, (0.005, 0.016), -30),
                                   (0.034, 0.224, (0.005, 0.015), -30)):
            p = np.array([s * 0.046, y, z])
            n = unit(s * 0.75, 0, 0.66)
            t = math.radians(tilt)
            up = unit(*(np.array([0, math.sin(t), math.cos(t)]) - n * (n @ np.array([0, math.sin(t), math.cos(t)]))))
            k.decal(p, n, half, ORANGE, up=up, eps=0.002, outline=rect, rings=8, segs=40)


def add_horn(k):
    """The forehead mark's tall orange horn: a solid from the brow's middle, up and a little back."""
    root = H + np.array([0, -HR[1] * 0.62, HR[2] * 0.55])
    d = unit(0.0, 0.28, 1.0)
    tip = root + d * 0.11
    a = np.array([1.0, 0, 0]); b = unit(*np.cross(d, a))
    pts = [root + a * 0.017 + b * 0.012, root - a * 0.017 + b * 0.012, root + a * 0.017 - b * 0.016,
           root - a * 0.017 - b * 0.016, root + d * 0.05 + a * 0.012 + b * 0.008, root + d * 0.05 - a * 0.012 + b * 0.008,
           root + d * 0.05 + a * 0.012 - b * 0.008, root + d * 0.05 - a * 0.012 - b * 0.008, tip]
    k.add("horn", hull(pts), ORANGE, "head", fuse=False, surface="fur")


# The brow's band: two halves from the middle, each turned down at its outer end
MARK = [
    (0.0, 0.036, (0.0085, 0.02), 0),
    (0.019, 0.011, (0.022, 0.0062), -12),
    (-0.019, 0.011, (0.022, 0.0062), 12),
]


def add_bell(k):
    c = np.array([0, -0.168, 0.205])
    k.add("bell", outward(ell(c, (0.02, 0.016, 0.02), sub=3)), BELL, "head", fuse=False, glow=BELL, surface="plastic")
    k.add("bellring", outward(ell(c + (0, -0.014, -0.004), (0.0085, 0.004, 0.0085), sub=3)), BELLRIM, "head",
          fuse=False, surface="plastic")


def add_fangs(k, at):
    for s in (1, -1):
        b = at + np.array([s * 0.0085, -0.002, 0.0])
        k.add("fang" + ("R" if s > 0 else "L"), hull([b + (0.0035, 0, 0.002), b + (-0.0035, 0, 0.002), b + (0, 0.003, 0.002),
                                                      b + (0, -0.001, -0.007)]), WHITE, "head", fuse=False,
              surface="shell")


# --- the eyes: fierce, the top cut by a dark lid sloping more steeply to the nose, a black slit pupil -------------------
OUTLINE = [(-1.0, -0.02), (-0.5, 0.22), (0.2, 0.46), (0.86, 0.64), (1.0, 0.3), (0.86, -0.34), (0.42, -0.72),
           (-0.18, -0.8), (-0.68, -0.56), (-0.95, -0.24)]

TAIL = [(0.0, 0.11, 0.19), (0.0, 0.15, 0.205), (0.0, 0.18, 0.245), (0.0, 0.19, 0.29), (0.0, 0.182, 0.33),
        (0.0, 0.17, 0.352)]


def add_tail(k):
    pts = lt.tail_pts(TAIL)
    r = [0.012, 0.011, 0.0105, 0.01, 0.0098, 0.0095]
    k.add("tail", tube(pts, r), DARK, "tail")
    for i, (a, b) in enumerate(((1, 2), (2, 3))):
        lt.add_bands(k, f"tailband{i}", pts[a], pts[b], (r[a], r[b]), (0.5,), 0.012, color=ORANGE, bone="tail",
                     mirror=False, thick=0.003)
    d = pts[-1] - pts[-2]
    lt.add_tail_tuft(k, pts[-1] + unit(*d) * 0.006, d, 0.03, color=DARK, n=6)


def torracat(src, out_path):
    k = Kit(src, coat="fur")
    add_body(k)
    add_legs(k)
    lt.add_head(k, H, HR, color=DARK)
    lt.add_ears(k, H, HR, color=DARK, lining=EARIN, at=(0.6, -0.05, 0.6), tip=(0.034, 0.012, 0.08), base=0.03,
                lining_share=0.55, lining_len=0.5)
    add_horn(k)
    lt.add_tufts(k, H, HR, color=ORANGE, size=1.25)
    add_tail(k)
    add_bell(k)
    add_flank_marks(k)
    lt.add_lower_face(k, H, HR, outline=[(a * 1.12, z * 1.12) for a, z in lt.FACE], color=ORANGE)
    lt.add_mark(k, H, HR, MARK, color=ORANGE)
    mouth = H + np.array([0, -HR[1] * 1.08, -HR[2] * 0.6])
    lt.add_nose_mouth(k, mouth, (0.0115, 0.008), ground=ORANGE)
    add_fangs(k, mouth + np.array([0, -0.004, -0.004]))
    k.build(voxel=0.004, smooth=4, band=0.004, wband=0.035)
    look = lt.eye_colour(pupil=INK, outline=OUTLINE, pupil_w=0.16, pupil_h=0.62)
    k.eye_states(lambda s: lt.lay_eyes(k, s, H, HR, x=0.04, dz=-0.012, half=(0.024, 0.0185), lid=DARK, line=ORANGE,
                                       outline=OUTLINE, look=look))
    k.export(out_path)


if __name__ == "__main__":
    torracat(*args())
