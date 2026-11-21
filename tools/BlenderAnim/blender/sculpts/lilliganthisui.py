# Lilligant-Hisui sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/lilliganthisui.py -- <the export's Lilligant-Hisui.glb> <out.glb>
#
# Grown from lilligant.py and through it petilil.py (sibling("petilil")): the same white face under a hood of light
# green hair, the same upright eyes and leaf blades, the same yellow collar and a flower on the head's +X side. From the
# references: a dancer on long legs; a white face under a green cap of hair whose fringe comes forward over the brow,
# a short lock at each side and dark leaves behind; almond eyes, pink to magenta with a dark rim, under the fringe;
# a yellow bow of petals at the throat; a short green bodice like a bud, flaring into a short tutu of four leaf points
# at the hips; long legs white at the top turning pink below the knee, like tights; yellow pointed boots whose toes
# curl up; long dark green leaf arms, paler at their tips (one raised in the art, both carried forward and down here);
# on the head's +X side a pink flower like a closed rose, a small yellow crown on it with pink beads on its points.
# (The task spoke of a white-and-green tutu and a long stamen like a ponytail: the pictures have a green bodice and
# tutu, and a small crown with beads, no stamen ponytail.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, unit, args, outward, inside, smoothstep, sibling

pt = sibling("petilil")

WHITE_ = "#F8F8F4"
HAIR, HAIR_DARK = "#9ED67A", "#5DB24E"
BODICE, TUTU = "#7CC85A", "#9AD46E"
DARK, ARM = "#2E8A3C", "#3E9E4A"
YELLOW, BOOT = "#F2CC3A", "#E8C232"
PINK, PINK_DEEP, BEAD = "#F2B6D8", "#DC7CB8", "#E68AC4"
TIGHTS = "#D86CB0"
EYE, EYE_DARK, RIM = srgb("#E074BC"), srgb("#A63486"), srgb("#4A1838")

# --- the frame (joints: body (0, 0, 0.42), head (0, -0.01, 0.49), arms (+-0.04, 0, 0.47), legs (+-0.025, 0, 0.36))
HC = np.array([0.0, -0.012, 0.548])       # the head's middle
HR = np.array([0.07, 0.064, 0.066])
EYE_D = (0.34, -0.92, -0.12)
EYE_HALF = (0.0135, 0.02)
FLOWER = np.array([0.058, 0.0, 0.632])
FLOWER_N = unit(0.5, -0.45, 0.74)
LEGX = 0.03
NECK, G = np.array([0.0, -0.01, 0.49]), 1.35   # the head and all it wears are grown about the neck by G


def legs(k):
    """Long legs, white thighs turning pink below the knee, thick enough to read; yellow boots whose toes curl up."""
    for sx in (1, -1):
        side = "R" if sx > 0 else "L"
        x = LEGX * sx
        k.add("leg" + side, tube([(x, 0.0, 0.39), (x * 1.1, -0.004, 0.25), (x * 1.15, 0.0, 0.08)], [0.034, 0.025, 0.019]),
              WHITE_, "leg" + side)
        # the boot: a shaft round the ankle flaring a little at its top, and a pointed toe curling up
        xb = x * 1.15
        k.add("boot" + side, outward(tube([(xb, 0.0, 0.105), (xb, 0.0, 0.06), (xb, -0.012, 0.03)], [0.026, 0.024, 0.026])),
              BOOT, "leg" + side, fuse=False, surface="leaf")
        k.add("toe" + side, outward(tube([(xb, -0.02, 0.026), (xb, -0.06, 0.02), (xb, -0.09, 0.035), (xb, -0.1, 0.062)],
                                         [0.026, 0.021, 0.013, 0.006])), BOOT, "leg" + side, fuse=False, surface="leaf")
        # the boot's pointed flap at the front of its top
        c = np.array((xb, -0.024, 0.105))
        k.add("flap" + side, hull([c + (0.018, 0.0, -0.02), c + (-0.018, 0.0, -0.02), c + (0.014, 0.012, -0.02),
                                   c + (-0.014, 0.012, -0.02), c + (0, -0.006, 0.035)]), BOOT, "leg" + side, fuse=False,
              surface="leaf")
    # white turning pink below the knee
    k.paint(lambda P: smoothstep(0.27, 0.19, P[:, 2]) * (P[:, 2] < 0.36), TIGHTS, on=["leg"])


def bodice(k):
    """A short green bodice like a bud, and a tutu of four leaf points round the hips."""
    k.add("bodice", ell((0, -0.002, 0.415), (0.056, 0.05, 0.072), deform=pt.onion(0.38, 1.0)), BODICE, "body")
    k.add("hips", ell((0, 0.0, 0.375), (0.048, 0.044, 0.035)), BODICE, "body")
    for i, a in enumerate((50, -50, 140, -140)):
        t = math.radians(a)
        d = unit(math.sin(t), -math.cos(t), -0.35)
        k.add(f"tutu{i}", pt.leaf_blade((0.0, 0.0, 0.375) + d * 0.035, d, (0, 0, 1.0), 0.085, 0.034, thick=0.016,
                                        bend=-0.08, cup=0.1), TUTU, "body", fuse=False, surface="leaf")
    # the yellow bow at the throat
    for i, a in enumerate((55, -55, 0)):
        t = math.radians(a)
        d = unit(math.sin(t) * 1.2, -math.cos(t), -0.3 - 0.5 * (a == 0))
        k.add(f"collar{i}", pt.leaf_blade((0.0, -0.022, 0.478) + d * 0.008, d, (0, -0.3, 1.0), 0.038, 0.02, thick=0.014,
                                          bend=-0.1, cup=0.1), YELLOW, "body", fuse=False, surface="leaf")


def flower(k):
    """A pink flower like a closed rose: cupped petals round a ball, a small yellow crown with pink beads on top."""
    n = FLOWER_N
    a = np.cross(n, (0, 0, 1.0)); a /= np.linalg.norm(a)
    b = np.cross(n, a)
    k.add("hub", outward(ell(FLOWER, (0.042, 0.042, 0.042), sub=3)), PINK_DEEP, "head", fuse=False, surface="leaf")
    for i in range(5):
        t = math.radians(72 * i + 20)
        d = math.cos(t) * a + math.sin(t) * b
        cup = unit(*(d * math.cos(math.radians(40)) + n * math.sin(math.radians(40))))
        e = np.cross(n, cup); e /= np.linalg.norm(e)
        nn = np.cross(cup, e)
        V, F = ell((0, 0, 0), (0.042, 0.04, 0.024), sub=3)
        M = np.stack([cup, e, nn], axis=1)
        k.add(f"petal{i}", outward((V @ M.T + FLOWER + d * 0.03 + n * 0.0, F)), PINK, "head", fuse=False,
              surface="leaf")
    # the crown: three points with a bead on each
    base = FLOWER + n * 0.04
    k.add("crownring", outward(tube([base - n * 0.006, base + n * 0.008], [0.016, 0.016])), YELLOW, "head", fuse=False,
          surface="leaf")
    for i in range(3):
        t = 2 * math.pi * i / 3 + 0.4
        w = math.cos(t) * a + math.sin(t) * b
        tip = base + w * 0.018 + n * 0.034
        side = np.cross(n, w)
        k.add(f"crownpt{i}", hull([base + w * 0.006 + side * 0.009, base + w * 0.006 - side * 0.009,
                                   base + w * 0.014 - n * 0.004 + side * 0.01, base + w * 0.014 - n * 0.004 - side * 0.01,
                                   tip]), YELLOW, "head", fuse=False, surface="leaf")
        k.add(f"bead{i}", outward(ell(tip, (0.0105,) * 3, sub=3)), BEAD, "head", fuse=False, surface="leaf")
    # dark leaves behind the flower and the head
    k.add("headleafL", pt.leaf_blade((-0.03, 0.03, 0.6), (-0.55, 0.4, 0.72), (-0.3, -0.8, 0.2), 0.1, 0.032, bend=0.06),
          DARK, "head", fuse=False, surface="leaf")
    k.add("headleafR", pt.leaf_blade((0.075, 0.035, 0.6), (0.85, 0.35, 0.05), (0.2, -0.7, 0.6), 0.1, 0.032, bend=0.08),
          DARK, "head", fuse=False, surface="leaf")


def lilligant_hisui(src, out):
    k = Kit(src, coat="leaf")
    legs(k)
    bodice(k)
    k.add("neck", ell((0.0, -0.008, 0.484), (0.024, 0.022, 0.026)), WHITE_, "head")
    k.add("head", ell(HC, HR), WHITE_, "head")
    # the cap of hair over the crown and the back, its fringe forward over the brow
    k.add("hood", ell(HC + (0, 0.01, 0.02), (0.077, 0.07, 0.062)), HAIR, "head")
    k.add("fringe", ell(HC + (0, -0.03, 0.04), (0.07, 0.05, 0.03), rot=(-12, 0, 0)), HAIR, "head")
    def hood_edge(P):
        q = NECK + (P - NECK) / G - HC
        # the face stays white under a hairline curving down a little at the sides
        face = (q[:, 1] < -0.02) & (q[:, 2] < 0.012 - 0.25 * q[:, 0] ** 2 / 0.004)
        return (1 - face) * (q[:, 2] > -0.048 - 0.022 * (q[:, 1] > 0.01))
    k.paint(hood_edge, HAIR, on=["hood", "fringe"])
    for sx in (1, -1):
        k.add("lock" + ("R" if sx > 0 else "L"),
              pt.leaf_blade((0.064 * sx, -0.016, 0.57), (0.3 * sx, -0.1, -1.0), (0.75 * sx, -0.65, 0), 0.085, 0.03,
                            thick=0.016, bend=-0.06, cup=0.16), HAIR_DARK, "head", fuse=False, surface="leaf")
    # long leaf arms out of the shoulders, carried forward and down, paler toward the tip
    for sx in (1, -1):
        side = "R" if sx > 0 else "L"
        root = np.array([0.042 * sx, -0.006, 0.46])
        k.add("armstalk" + side, outward(tube([root - (0.012 * sx, 0, 0), root + np.array([0.02 * sx, -0.012, -0.006])],
                                              [0.017, 0.016])), ARM, "arm" + side, fuse=False, surface="leaf")
        k.add("arm" + side, pt.leaf_blade(root + np.array([0.016 * sx, -0.008, -0.004]), (0.62 * sx, -0.42, -0.66),
                                          (0, -0.45, 1.0), 0.23, 0.046, thick=0.018, bend=0.1, cup=0.14), ARM,
              "arm" + side, fuse=False, surface="leaf")
    flower(k)
    # the head and all it wears grown about the neck: the pictures' head is near a third of the height
    for p in k.parts:
        if p["bone"] == "head" and p["name"] != "neck":
            p["V"] = NECK + (np.asarray(p["V"]) - NECK) * G
    k.build(voxel=0.0055, smooth=4, band=0.005)

    look = pt.red_eye(fill=EYE, dark=EYE_DARK, rim=RIM)
    def eyes(state):
        pt.lay_eyes(k, NECK + (HC - NECK) * G, HR * G, EYE_D, (EYE_HALF[0] * G, EYE_HALF[1] * G), look, WHITE_, state)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    lilligant_hisui(*args())
