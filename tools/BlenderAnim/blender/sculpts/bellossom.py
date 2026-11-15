# Bellossom sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/bellossom.py -- <the export's Bellossom.glb> <out.glb>
#
# Grown from Oddish's line (sibling("oddish")): the line's way of laying eyes and an open mouth on a fused round
# head. From the references: a small light-green dancer, a round head fused down into a short body; two big red
# flowers on its head, each five puffy rounded petals about a raised yellow ring with a dark hollow, their faces
# turned forward and out; round dark navy eyes with a white glint, red cheeks, an open happy mouth with a pink
# tongue; two stubby arms held forward; a skirt of leaves from the waist to the ground: big bulbous green leaves
# pointing down (two in front), golden leaves between them flaring out at the hem, small dark-green points under all.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, lathe, srgb, unit, eye_look, args, sibling, outward, smoothstep

od = sibling("oddish")

# --- colours ------------------------------------------------------------------------------------------------------
SKIN = "#A6D47E"
PETAL, PETAL_BACK = "#E8402E", "#C8322A"
RING, HOLE = "#F4C434", "#8A6A1A"
GREEN, GOLD, DARK = "#4C9E3E", "#F2C93A", "#2E6E34"
CHEEK = "#EE6A72"
EYE_BLACK, EYE_BLUE, EYE_RIM = srgb("#1A1E30"), srgb("#3E5EA8"), srgb("#10121C")
MOUTH, TONGUE = "#9E2032", "#F07890"

# --- the frame ----------------------------------------------------------------------------------------------------
HC = np.array([0.0, 0.0, 0.362])          # the head's middle
HR = np.array([0.1, 0.09, 0.088])
BODY = np.array([0.0, -0.004, 0.272])     # the short body the head runs down into
BODY_R = np.array([0.066, 0.06, 0.062])
WAIST = 0.288                              # where the skirt's leaves start


def skirt_leaf(centre, hw, r, z, thick, bulge=0.012, pinch=0.0):
    """
    A leaf of the skirt round the waist: centre its angle from the front (degrees, +X side positive), hw(v) its half
    width in degrees, r(v) its distance from the axis and z(v) its height from the waist (v 0) to its tip (v 1).
    Convex across its face. A loose piece, wound outward.
    """
    c0 = math.radians(centre)
    def at(u, v):
        v = 0.015 + 0.97 * v
        c = 2 * u - 1
        th = c0 + c * np.radians(hw(v))
        rr = r(v) + bulge * (1 - c * c) * np.sin(np.pi * v) - pinch * c ** 4 * smoothstep(0.2, 1.0, v)
        return np.stack([np.sin(th) * rr, -np.cos(th) * rr, z(v)], axis=-1)
    return outward(sheet(at, 13, 34, thick))


def green_r(v):
    return 0.06 + 0.118 * np.sin(np.pi * v * 0.8) ** 0.8


def green_hw(v):
    return 36 * np.sin(np.pi * v ** 1.05) ** 0.85 + 9 * (1 - v) + 0.4


def gold_r(v, flare=0.105):
    return green_r(v) - 0.014 + flare * smoothstep(0.3, 1.0, v) ** 1.3


def gold_hw(v, most=40):
    return most * np.sin(np.pi * v ** 1.3) ** 0.95 + 7 * (1 - v) + 0.4


def skirt(k):
    # the big green leaves, two to the front and two behind
    for i, a in enumerate((30, 150, 210, 330)):
        k.add(f"skirtG{i}", skirt_leaf(a, green_hw, green_r, lambda v: WAIST - 0.254 * v, 0.02), GREEN, "body",
              fuse=False, surface="leaf")
    # golden leaves between them, under the green at the waist and flaring out past it toward the hem: wide at the
    # sides, a narrower one showing its point between the front pair and the back pair
    for i, (a, most, fl) in enumerate(((0, 24, 0.035), (90, 46, 0.105), (180, 24, 0.035), (270, 46, 0.105))):
        k.add(f"skirtY{i}", skirt_leaf(a, lambda v, m=most: gold_hw(v, m), lambda v, f_=fl: gold_r(v, f_),
                                       lambda v: WAIST - 0.01 - 0.24 * v, 0.018, pinch=0.06), GOLD, "body",
              fuse=False, surface="leaf")
    # small dark points under everything, peeping out at the ground
    for i in range(12):
        a = 15 + 30 * i
        k.add(f"skirtD{i}", skirt_leaf(a, lambda v: 15 * np.sin(np.pi * v ** 0.7) ** 0.6 + 0.4,
                                       lambda v: 0.07 + 0.085 * v, lambda v: 0.17 - 0.165 * v, 0.016, bulge=0.004),
              DARK, "body", fuse=False, surface="leaf")


def flower(k, side):
    """A flower on the head's +X (side 1) or -X (-1) side: five puffy petals about a raised ring with its hollow."""
    centre = np.array([0.092 * side, 0.012, 0.458])
    n = unit(0.5 * side, -0.68, 0.54)                      # the flower's face
    a = np.cross(n, (0, 0, 1.0)); a /= np.linalg.norm(a)
    b = np.cross(n, a)
    name = "R" if side > 0 else "L"
    for i in range(5):
        t = math.radians(90 + 72 * i)
        d = math.cos(t) * a + math.sin(t) * b               # out from the middle in the flower's plane
        cup = unit(*(d * math.cos(math.radians(14)) + n * math.sin(math.radians(14))))
        e = np.cross(n, cup); e /= np.linalg.norm(e)
        nn = np.cross(cup, e)
        V, F = ell((0, 0, 0), (0.054, 0.046, 0.017), sub=3)
        M = np.stack([cup, e, nn], axis=1)                  # x along the petal, y across, z its face
        V = V @ M.T + centre + cup * 0.056
        k.add(f"petal{name}{i}", outward((V, F)), PETAL, "head", fuse=False, surface="leaf")
    # a red hub behind, so no gap shows between the petals from behind
    V, F = ell((0, 0, 0), (0.05, 0.05, 0.022), sub=3)
    M = np.stack([a, b, n], axis=1)
    k.add(f"hub{name}", outward((V @ M.T + centre - n * 0.008, F)), PETAL_BACK, "head", fuse=False, surface="leaf")
    base = centre + n * 0.01
    k.add(f"ring{name}", lathe(base, n, [(0, 0.0), (0.04, 0.0), (0.046, 0.012), (0.043, 0.022), (0.034, 0.027),
                                         (0.024, 0.024), (0.021, 0.014), (0.021, 0.006), (0, 0.006)], seg=40),
          RING, "head", fuse=False, surface="leaf")
    k.add(f"hole{name}", lathe(base, n, [(0, 0.006), (0.0215, 0.006), (0.0215, 0.018), (0, 0.018)], seg=40),
          HOLE, "head", fuse=False, surface="leaf")


def navy_eye():
    """The line's round eye, dark: near-black above, blue in its lower half, a white glint."""
    return od.round_eye(red=EYE_BLACK, dark=EYE_BLUE * 1.6 - EYE_BLACK * 0.6, rim=EYE_RIM)


def bellossom(src, out):
    k = Kit(src, coat="leaf")
    k.add("head", ell(HC, HR), SKIN, "head")
    k.add("torso", ell(BODY, BODY_R), SKIN, "body")
    # stubby arms held forward and a little down
    k.add("arm", tube([(0.05, -0.016, 0.276), (0.096, -0.052, 0.262), (0.122, -0.086, 0.244)],
                      [0.026, 0.025, 0.024]), SKIN, "arm{s}", mirror=True)
    # short legs inside the skirt, for the leg bones to carry something
    k.add("leg", tube([(0.034, 0.0, 0.2), (0.036, 0.0, 0.08), (0.04, -0.012, 0.03)], [0.026, 0.024, 0.024]),
          SKIN, "leg{s}", mirror=True)
    skirt(k)
    flower(k, 1)
    flower(k, -1)
    # cheeks and the open, happy mouth
    p, nrm = od.on_ell(HC, HR, (0.66, -0.72, -0.16))
    k.decal(p, nrm, (0.017, 0.011), CHEEK, eps=0.004, rings=8, segs=36, mirror=True)
    od.open_mouth(k, HC, HR, (0, -1, -0.3), (0.026, 0.024), MOUTH, TONGUE)
    k.build(voxel=0.005, smooth=4, band=0.006)

    lid = srgb(SKIN)
    def eyes(state):
        od.lay_eyes(k, HC, HR, (0.4, -0.88, 0.1), (0.018, 0.024), navy_eye(), lid, od.INK, state)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    bellossom(*args())
