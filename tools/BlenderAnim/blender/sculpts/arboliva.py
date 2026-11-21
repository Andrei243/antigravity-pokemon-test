# Arboliva sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/arboliva.py -- <the export's Arboliva.glb> <out.glb>
#
# Grown from Smoliv's script (sibling("smoliv")) through Dolliv's: the same olives with their sockets (now ripe and
# dark), the same fleshy leaves, a pale face. From the references: an olive tree of a lady. A small cream face with its
# eyes always shut in calm arcs; a flame of upright leaves on her crown; a high collar of pointed leaves rising round
# her cheeks and hiding her mouth; from her shoulders two great leafy boughs, the design's largest mass, dipping out
# and down and curling up at their ends, thick with pointed leaves along their tops, three dark purple olives hanging
# under each; a tan trunk with pale under-bark showing at the chest, narrowing to a waist and spreading into thick
# roots over the ground, pale shoots between them.
# (The task's "wide crown of leaves bearing olives like a hat ... skirt of leaves" is not the design: the pictures
# have the olives on two boughs held out like arms, a small flame of leaves on the head, and roots, not a skirt.)
# The boughs are held out (the design's largest masses, as Necrozma's hands): swept some 27 degrees forward and
# dipping, so the model is about as wide as it is tall rather than the art's 1.3 times.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, eye_look, args, outward, sibling

sm = sibling("smoliv")

FACE = "#F4F2E2"
GREEN, GREEN_LIGHT, GREEN_DARK = "#56AC3C", "#7CC452", "#428E32"
TRUNK, BARK, UNDER = "#C9944E", "#A8763A", "#F0EEDC"
OLIVE, OLIVE_RIM, OLIVE_SOCKET = "#5E1E4C", "#7C3A66", "#2E0A24"
LID_INK = srgb("#6E2A34")

# --- Arboliva's frame (joints: body (0, 0, 0.42), arms (+-0.05, 0, 0.6), head (0, 0, 0.65), legs (+-0.03, -0.03,
#     0.2); about 0.95 tall) --------------------------------------------------------------------------------------
HC = np.array([0.0, -0.012, 0.72])       # the face's middle
HR = np.array([0.096, 0.088, 0.092])
EYE_D = (0.44, -0.88, 0.04)
EYE_HALF = (0.034, 0.02)
# the bough's line on the +X side: from the shoulder out and down, then forward and up to its curled end
BOUGH = np.array([(0.06, 0.0, 0.62), (0.16, -0.04, 0.53), (0.26, -0.1, 0.47), (0.35, -0.17, 0.47), (0.41, -0.22, 0.53),
                  (0.43, -0.25, 0.6)])
BOUGH_R = [0.07, 0.07, 0.066, 0.06, 0.05, 0.04]


def bough_point(t):
    """The point t (0..1) along the bough's line (by its control points), and the radius there."""
    s = t * (len(BOUGH) - 1)
    i = min(int(s), len(BOUGH) - 2)
    f = s - i
    return BOUGH[i] * (1 - f) + BOUGH[i + 1] * f, BOUGH_R[i] * (1 - f) + BOUGH_R[i + 1] * f


def spike(root, d, length, width, thick, side=None):
    """A thick pointed leaf-tuft: a flat-faced kite, broad at its root, along d, its face across side."""
    d = unit(*d)
    s = np.cross(d, (0, 0, 1.0)) if side is None else np.array(side, float)
    s = s - d * (s @ d); s /= np.linalg.norm(s)
    n = np.cross(s, d)
    root = np.array(root, float)
    return hull([root - s * width, root + s * width, root + d * length * 0.35 + s * width * 0.8,
                 root + d * length * 0.35 - s * width * 0.8, root + n * thick, root - n * thick,
                 root + d * length * 0.3 + n * thick * 0.7, root + d * length * 0.3 - n * thick * 0.7,
                 root + d * length])


def bough(k, sx):
    side = "R" if sx > 0 else "L"
    m = np.array([sx, 1, 1.0])
    k.add("bough" + side, tube(BOUGH * m, BOUGH_R), GREEN, "arm" + side)
    # a flatter leafy skirt under the line, so the bough is broad rather than a pipe
    for t, r in ((0.2, 0.06), (0.42, 0.058), (0.64, 0.05)):
        p, _ = bough_point(t)
        k.add(f"boughfill{side}{t}", ell(p * m + (0, 0.02, -0.012), (r * 1.3, r * 1.1, r * 0.8)), GREEN,
              "arm" + side)
    # broad leaves along its top, out and up, short at the shoulder and long at the end, where they rise in a plume
    rs = np.random.RandomState(7)
    leaves = [(0.12, (0.55, -0.1, 1.0), 0.09), (0.28, (0.6, -0.2, 1.0), 0.11), (0.44, (0.55, -0.35, 1.0), 0.12),
              (0.58, (0.6, -0.5, 1.0), 0.13), (0.72, (0.5, -0.45, 1.0), 0.15), (0.86, (0.45, -0.3, 1.0), 0.16),
              (0.96, (0.15, -0.35, 1.0), 0.18), (1.0, (0.45, -0.45, 1.0), 0.14), (0.9, (-0.2, -0.5, 1.0), 0.15)]
    for i, (t, d, L) in enumerate(leaves):
        p, r = bough_point(t)
        L *= 0.9 + 0.2 * rs.rand()
        root = p + np.array([0, 0, r * 0.4])
        dd = unit(*(np.array(d) * m))
        col = GREEN_LIGHT if i % 2 else GREEN
        k.add(f"tuft{side}{i}", sm.fleshy_leaf(root * m, dd, (0, -1.0, 0.25), L * 1.25, 0.056, 0.028, bend=-0.12, cup=0.3),
              col, "arm" + side, fuse=False, surface="leaf")
        # a smaller leaf behind each, for depth from the back camera
        k.add(f"tuftb{side}{i}", sm.fleshy_leaf(root * m + (0, 0.03, 0), unit(*(np.array(d) * m + (0, 0.9, 0))),
                                                (0, 1.0, 0.25), L * 0.9, 0.046, 0.026, bend=-0.1, cup=0.3),
              GREEN_DARK, "arm" + side, fuse=False, surface="leaf")
    # three dark olives hanging under it, seated against its underside
    for i, t in enumerate((0.3, 0.56, 0.8)):
        p, r = bough_point(t)
        c = p + np.array([0.0, -0.01, -r * 0.8 - 0.04])
        sm.olive(k, f"olive{side}{i}", c * m, (0.04, 0.038, 0.05), unit(0.1 * sx, -0.15, -1.0), OLIVE,
                 "arm" + side, fuse=False, socket=(OLIVE_SOCKET, OLIVE_RIM), surface="shell")


def root(k, a, bone, n):
    d = np.array([math.sin(a), -math.cos(a), 0.0])
    pts = [d * 0.03 + (0, 0, 0.27), d * 0.1 + (0, 0, 0.14), d * 0.19 + (0, 0, 0.05), d * 0.245 + (0, 0, 0.026)]
    k.add(f"root{n}", tube(pts, [0.052, 0.04, 0.027, 0.02]), TRUNK, bone)


def closed_eye(tilt=0.0):
    """Her eye, always shut: a calm dark arc curving down, thick at its middle (u outward, v up)."""
    lid = srgb(FACE)
    def look(u, v):
        c = np.tile(lid, (len(u), 1))
        mid = 0.1 - 0.42 * (1 - u * u) + tilt * u
        w = 0.24 * (1 - 0.5 * u * u)
        c[(np.abs(u) < 0.9) & (np.abs(v - mid) < w)] = LID_INK
        return c
    return look


def arboliva(src, out_path):
    k = Kit(src, coat="leaf")
    # --- the trunk, its waist and its roots
    k.add("trunk", tube([(0, 0.0, 0.62), (0, 0.004, 0.5), (0, 0.006, 0.38), (0, 0.004, 0.27)],
                        [0.082, 0.068, 0.058, 0.062]), TRUNK, "body")
    for i, a in enumerate((35, -35, 110, -110, 180)):
        bone = "legR" if i == 0 else "legL" if i == 1 else "body"
        root(k, math.radians(a), bone, i)
    # the darker line of bark down the trunk's front
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.008, 0.003) * inside(P[:, 1] + 0.03, 0.004)
            * inside(P[:, 2] - 0.56, 0.004) * inside(0.3 - P[:, 2], 0.004), BARK, on=["trunk"])
    # pale under-bark at the chest's sides and pale shoots between the front roots
    for sx in (1, -1):
        k.add("under" + ("R" if sx > 0 else "L"), ell((0.05 * sx, -0.03, 0.56), (0.03, 0.03, 0.05), rot=(0, -20 * sx, 0)),
              UNDER, "body")
    for i, x in enumerate((-0.018, 0.018)):
        k.add(f"shoot{i}", sm.fleshy_leaf((x, -0.04, 0.2), (x * 3, -0.35, -1.0), (0, -1.0, 0.2), 0.12, 0.02, 0.018,
                                          bend=0.05), UNDER, "body", fuse=False, surface="leaf")
    # --- the shoulders' mantle of leaves and the boughs
    k.add("mantle", ell((0, 0.012, 0.6), (0.13, 0.1, 0.07)), GREEN, "body")
    for sx in (1, -1):
        bough(k, sx)
    # --- the high collar: pointed leaves rising round the cheeks, hiding the mouth, below the eyes
    for sx in (1, -1):
        for j, (ox, ang, L) in enumerate(((0.014, 0.3, 0.1), (0.034, 0.65, 0.105), (0.056, 1.05, 0.1))):
            d = unit(ang * sx, -0.3, 1.0)
            k.add(f"collar{'R' if sx > 0 else 'L'}{j}",
                  spike((ox * sx, -0.082 + 0.012 * j, 0.585), d, L, 0.046, 0.02, side=(1.0, 0.25 * sx, 0)),
                  GREEN if j % 2 == 0 else GREEN_DARK, "body", fuse=False, surface="leaf")
    # --- the head, its flame of leaves
    k.add("neck", tube([(0, 0.0, 0.6), (0, -0.006, 0.66)], [0.04, 0.042]), FACE, "head")
    k.add("head", ell(HC, HR, sub=5), FACE, "head")
    k.add("crownbase", outward(ell(HC + (0, 0.024, 0.052), (0.072, 0.064, 0.05))), GREEN, "head", fuse=False, surface="leaf")
    for i, (d, L, col) in enumerate((((0.0, 0.1, 1.0), 0.15, GREEN_LIGHT), ((0.5, 0.15, 1.0), 0.11, GREEN),
                                     ((-0.5, 0.15, 1.0), 0.11, GREEN), ((0.25, -0.45, 1.0), 0.09, GREEN_LIGHT),
                                     ((-0.25, -0.45, 1.0), 0.09, GREEN_LIGHT), ((0.0, 0.7, 0.8), 0.09, GREEN_DARK))):
        k.add(f"flame{i}", sm.fleshy_leaf(HC + (d[0] * 0.02, 0.016 + d[1] * 0.02, 0.07), d, (0, -1.0, 0.1), L, 0.034,
                                          0.024, bend=-0.08, cup=0.25), col, "head", fuse=False, surface="leaf")
    k.build(voxel=0.0065, smooth=4, band=0.006)

    ep, en = sm.on_ell(HC, HR, EYE_D)
    lid = srgb(FACE)
    def eyes(state):
        if state in ("Open", "Shut"):
            look = closed_eye()
        elif state == "Fierce":
            look = closed_eye(tilt=0.35)
        else:
            look = eye_look(state, closed_eye(), lid, LID_INK, line=0.2)
        k.decal(ep, en, EYE_HALF, look, eps=0.002, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out_path)


if __name__ == "__main__":
    arboliva(*args())
