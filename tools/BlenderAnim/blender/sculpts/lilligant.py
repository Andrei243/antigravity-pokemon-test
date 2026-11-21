# Lilligant sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/lilligant.py -- <the export's Lilligant.glb> <out.glb>
#
# Grown from Petilil's script (sibling("petilil")): the same onion of lobes (now the gown, upside down), the same
# leaves and the same upright eyes. From the references: a flower lady; a pale green gown like a bell of rounded lobes
# from a narrow waist to the ground, three yellow nubs of feet peeping under its front; big dark green leaves falling
# from the waist behind the gown at the sides and back; a slim white chest under a collar of yellow petals; a white
# head under a hood of light green hair whose two broad locks hang down beside the face; upright orange eyes with a
# dark rim and a glint, set close; no mouth; long dark green leaves for arms (out and down in the art, carried forward
# and down here); and on the head's +X side a great orange flower of four puffy petals round a little yellow crown
# with orange-tipped stamens, dark green leaves behind it.
# (The task called the crown red and white: the pictures have an orange flower with a yellow crown.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, unit, args, outward, sibling

pt = sibling("petilil")

GOWN, WHITE_ = "#B4DC8C", "#F8F8F4"
HAIR = "#B8E29C"
DARK = "#2E8A3C"
ARM = "#3E9E4A"
YELLOW, FEET = "#F2CC3A", "#D2B43C"
PETAL, PETAL_BACK = "#EC6C2A", "#C24E1E"
STAMEN = "#EE7A2E"
ORANGE, ORANGE_DARK, RIM = srgb("#F07A30"), srgb("#C4501A"), srgb("#5A2410")

# --- Lilligant's frame (joints: body (0, 0, 0.2), head (0, -0.01, 0.34), arms (+-0.04, 0, 0.32)) ------------------
HC = np.array([0.0, -0.012, 0.37])       # the head's middle
HR = np.array([0.064, 0.058, 0.06])
GC = np.array([0.0, 0.0, 0.13])          # the gown's middle
EYE_D = (0.33, -0.93, -0.08)
EYE_HALF = (0.0105, 0.017)
FLOWER = np.array([0.046, -0.004, 0.45])  # the flower's middle
FLOWER_N = unit(0.38, -0.58, 0.72)        # its face


def gown(k):
    """A bell of rounded lobes from the waist to the ground, drawn in at the waist (Petilil's onion, grown)."""
    k.add("gown", ell(GC, (0.12, 0.112, 0.13), deform=pt.onion(0.62, 1.08)), GOWN, "body")
    for i in range(6):
        a = 2 * math.pi * (i + 0.5) / 6
        d = np.array([math.sin(a), -math.cos(a), 0])
        k.add(f"gownlobe{i}", ell(GC + d * 0.058 + (0, 0, -0.006), (0.088, 0.088, 0.122), deform=pt.onion(0.66, 1.06)),
              GOWN, "body")
    # three yellow nubs of feet peeping under the front
    for x, y in ((0.0, -0.104), (0.05, -0.088), (-0.05, -0.088)):
        k.add("foot", outward(ell((x, y, 0.012), (0.024, 0.028, 0.014), sub=3)), FEET, "body", fuse=False)
    # big dark leaves from the waist behind the gown, at the sides and the back
    for i, (sx, back) in enumerate(((1, 0.35), (-1, 0.35), (0.55, 1.0), (-0.55, 1.0))):
        root = np.array([0.09 * sx, 0.07 * back, 0.21])
        d = unit(0.4 * sx, 0.32 * back, -0.86)
        n = unit(0.9 * sx, -0.3 + 0.6 * back, 0.1)
        k.add(f"skirtleaf{i}", pt.leaf_blade(root, d, n, 0.24, 0.07, thick=0.016, bend=-0.08, cup=0.1), DARK, "body",
              fuse=False, surface="leaf")


def collar(k):
    """Yellow pointed petals round the neck, out and a little down."""
    for i, a in enumerate((0, 62, -62, 130, -130)):
        t = math.radians(a)
        d = unit(math.sin(t), -math.cos(t), -0.35)
        k.add(f"collar{i}", pt.leaf_blade((0.0, -0.004, 0.31) + d * 0.02, d, (0, 0, 1.0), 0.05, 0.024, thick=0.014,
                                          bend=-0.1, cup=0.1), YELLOW, "body", fuse=False, surface="leaf")


def flower(k):
    """The great flower: four puffy cupped petals round a hub, a yellow crown and stamens tipped orange."""
    n = FLOWER_N
    a = np.cross(n, (0, 0, 1.0)); a /= np.linalg.norm(a)
    b = np.cross(n, a)
    for i in range(4):
        t = math.radians(45 + 90 * i + 12)
        d = math.cos(t) * a + math.sin(t) * b
        cup = unit(*(d * math.cos(math.radians(22)) + n * math.sin(math.radians(22))))
        e = np.cross(n, cup); e /= np.linalg.norm(e)
        nn = np.cross(cup, e)
        V, F = ell((0, 0, 0), (0.056, 0.046, 0.03), sub=3)
        M = np.stack([cup, e, nn], axis=1)
        k.add(f"petal{i}", outward((V @ M.T + FLOWER + cup * 0.05, F)), PETAL, "head", fuse=False, surface="leaf")
    V, F = ell((0, 0, 0), (0.056, 0.056, 0.03), sub=3)
    k.add("hub", outward((V @ np.stack([a, b, n], axis=1).T + FLOWER - n * 0.012, F)), PETAL_BACK, "head", fuse=False,
          surface="leaf")
    # the crown: five points round a short ring, standing out of the middle
    base = FLOWER + n * 0.02
    for i in range(5):
        t = 2 * math.pi * i / 5
        w = math.cos(t) * a + math.sin(t) * b
        tip = base + w * 0.03 + n * 0.042
        k.add(f"crownpt{i}", hull([base + w * 0.012 + n * 0.004 + s * 0.012 * np.cross(n, w) for s in (-1, 1)]
                                  + [base + w * 0.018 - n * 0.004 + s * 0.014 * np.cross(n, w) for s in (-1, 1)]
                                  + [tip, base + w * 0.006]), YELLOW, "head", fuse=False, surface="leaf")
    k.add("crownring", outward(tube([base - n * 0.004, base + n * 0.012], [0.02, 0.02])), YELLOW, "head", fuse=False,
          surface="leaf")
    # stamens: short stalks out of the crown, each tipped with an orange ball
    for i, t in enumerate((0.3, 1.5, 2.7, 3.9, 5.1)):
        w = math.cos(t) * a + math.sin(t) * b
        s0 = base + n * 0.01
        s1 = base + n * 0.05 + w * 0.03 + np.array([0, 0.01, 0.036])
        k.add(f"stalk{i}", outward(tube([s0, s1], [0.0105, 0.0095])), YELLOW, "head", fuse=False, surface="leaf")
        k.add(f"anther{i}", outward(ell(s1 + (s1 - s0) / np.linalg.norm(s1 - s0) * 0.01, (0.016,) * 3, sub=3)),
              STAMEN, "head", fuse=False, surface="leaf")
    # dark leaves behind the flower and the head
    k.add("headleafL", pt.leaf_blade((-0.03, 0.03, 0.41), (-0.55, 0.35, 0.76), (-0.3, -0.8, 0.2), 0.12, 0.036,
                                     bend=0.06), DARK, "head", fuse=False, surface="leaf")
    k.add("headleafR", pt.leaf_blade((0.07, 0.03, 0.42), (0.85, 0.35, 0.1), (0.2, -0.7, 0.6), 0.1, 0.034, bend=0.08),
          DARK, "head", fuse=False, surface="leaf")


def lilligant(src, out):
    k = Kit(src, coat="leaf")
    gown(k)
    k.add("chest", ell((0.0, -0.004, 0.29), (0.036, 0.032, 0.056)), WHITE_, "body")
    collar(k)
    k.add("neck", ell((0.0, -0.008, 0.325), (0.026, 0.024, 0.03)), WHITE_, "head")
    k.add("head", ell(HC, HR), WHITE_, "head")
    # the hood of hair over the crown and the back, and its two locks hanging beside the face
    k.add("hood", ell(HC + (0, 0.014, 0.018), (0.07, 0.062, 0.058)), HAIR, "head")
    def hood_edge(P):
        q = P - HC
        # the face stays white in front, under a hairline that dips to the brow's middle
        face = (q[:, 1] < -0.028) & (q[:, 2] < 0.03 - 0.6 * np.abs(q[:, 0]) + 0.01)
        return (1 - face) * (P[:, 2] > 0.33)
    k.paint(hood_edge, HAIR, on=["hood"])
    for sx in (1, -1):
        k.add("lock" + ("R" if sx > 0 else "L"),
              pt.leaf_blade((0.058 * sx, -0.012, 0.402), (0.22 * sx, -0.06, -1.0), (0.75 * sx, -0.65, 0), 0.17, 0.034,
                            thick=0.016, bend=-0.06, cup=0.16), HAIR, "head", fuse=False, surface="leaf")
    # leaf arms out of the shoulders, carried forward and down
    for sx in (1, -1):
        side = "R" if sx > 0 else "L"
        root = np.array([0.036 * sx, -0.006, 0.305])
        k.add("armstalk" + side, outward(tube([root - (0.01 * sx, 0, 0), root + np.array([0.022 * sx, -0.012, -0.006])],
                                              [0.016, 0.015])), ARM, "arm" + side, fuse=False, surface="leaf")
        k.add("arm" + side, pt.leaf_blade(root + np.array([0.016 * sx, -0.008, -0.004]), (0.78 * sx, -0.48, -0.4),
                                          (0, -0.5, 1.0), 0.17, 0.048, thick=0.016, bend=0.1, cup=0.14), ARM,
              "arm" + side, fuse=False, surface="leaf")
    flower(k)
    k.build(voxel=0.005, smooth=4, band=0.005)

    look = pt.red_eye(fill=ORANGE, dark=ORANGE_DARK, rim=RIM)
    def eyes(state):
        pt.lay_eyes(k, HC, HR, EYE_D, EYE_HALF, look, WHITE_, state)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    lilligant(*args())
