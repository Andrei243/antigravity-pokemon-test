# Panusa sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/panusa.py -- <the export's Panusa.glb> <out.glb>
#
# Carpathia No. 10042 (plan 46, CP-dex-1b, "42. Panusa: the stubble-field doll"), grown from Panusel's script
# (sibling("panusel")): the line's husk and its folds, its string, its blades and its corn silk. From the project's
# picture: a tall husk maiden who floats over the stubble; a pale faceless mask of husk with a seam down it and a
# pointed chin, under a hood of husk; two glowing amber seeds for eyes in dark slanted sockets; long orange corn silk
# streaming back and out to the +X side as if in a wind; string wound round the neck, bands of husk crossed over the
# breast, a sash of string at the waist; ragged sleeves tied at the elbow and wrist, long husk fingers (the arms hang
# out and down in the picture, carried forward and down here); a long skirt of three ragged tiers, straw, olive and
# brown, to two pointed husk feet.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, unit, eye_look, inside, args, outward, polygon, sibling

pl = sibling("panusel")

HUSK, HUSK_FOLD, HUSK_GREEN, HUSK_DEEP = pl.HUSK, pl.HUSK_FOLD, pl.HUSK_GREEN, pl.HUSK_DEEP
HUSK_BROWN, HOOD = "#B09060", "#D2BE90"
MASK, SEAM = "#EEE6CE", "#A89070"
SILK, SILK_DEEP = "#F0A030", "#D87820"
SOCKET = srgb("#2A1A10")
AMBER, AMBER_CORE = srgb("#F29A1E"), srgb("#FFE07A")

# --- Panusa's frame (joints: body (0, 0, 0.707), arms (+-0.1, 0, 0.777), head (0, 0, 0.867), legs (+-0.07, 0, 0.367))
HC = np.array([0.0, -0.008, 0.93])        # the mask's middle
HR = np.array([0.064, 0.061, 0.08])
NECK = 0.85
WAIST = 0.68


def chin(V):
    """The unit sphere made the mask: a pointed chin, the top broad."""
    V = V.copy()
    s = 1 - 0.45 * np.clip(-V[:, 2], 0, 1) ** 1.6
    V[:, 0] *= s
    V[:, 1] = np.where(V[:, 1] < 0, V[:, 1] * 1.05, V[:, 1])
    return V


def head(k):
    k.add("mask", ell(HC, HR, deform=chin), MASK, "head")
    # the hood: a shell of husk round the back and top, its edge framing the mask
    k.add("hood", ell(HC + np.array([0, 0.03, 0.02]), (0.08, 0.07, 0.092)), HOOD, "head")
    k.add("hoodfall", tube([(0, 0.04, 0.95), (0, 0.06, 0.88), (0, 0.05, 0.83)], [0.06, 0.055, 0.05]), HOOD, "head")
    k.paint(pl.folds(HC, 14, 3.0, 1.0, 0.83, phase=4, wobble=2.0), HUSK_FOLD, on=["hood"])
    # the seam down the mask and two creases beside it
    def seam(P):
        front = inside(P[:, 1] - (HC[1] - 0.03), 0.003)
        m = inside(np.abs(P[:, 0]) - 0.0024, 0.0012) * inside(P[:, 2] - 0.97, 0.004) * inside(0.875 - P[:, 2], 0.004)
        return m * front
    k.paint(seam, SEAM, on=["mask"])
    pl.cord_ring(k, "neckcord", (0, 0.004, NECK + 0.02), 0.04, 0.011, "body", wraps=4, dz=0.014)


def hair(k):
    """Long orange corn silk streaming back and out to the +X side: a mass behind the hood and thick locks."""
    k.add("silkmass", ell((0.04, 0.06, 0.94), (0.07, 0.055, 0.08)), SILK, "head")
    locks = ((0.05, 0.9, 0.3, 0.26, 0.05, 0.028), (0.06, 0.94, 0.45, 0.3, 0.12, 0.028),
             (0.05, 0.99, 0.6, 0.26, 0.22, 0.026), (0.02, 0.98, 0.2, 0.22, 0.18, 0.024),
             (0.06, 0.87, 0.35, 0.24, -0.08, 0.026), (-0.02, 0.94, -0.2, 0.18, 0.05, 0.024),
             (0.04, 1.01, 0.4, 0.22, 0.32, 0.022), (0.07, 0.92, 0.7, 0.28, 0.0, 0.026),
             (-0.03, 0.89, -0.3, 0.16, -0.1, 0.022))
    for i, (x, z, out, L, rise, r) in enumerate(locks):
        root = np.array([x, 0.065, z])
        d = (0.75 + out, 0.42, rise - 0.12)
        pl.silk_strand(k, f"silk{i}", root, d, L, r, "head", fall=0.35, curl=0.16 * (1 if i % 2 else -1),
                       colour=SILK if i % 3 else SILK_DEEP)


def body(k):
    k.add("bodice", ell((0, 0, 0.76), (0.078, 0.058, 0.09), deform=pl.gathered(0.32, 1.6)), HUSK, "body")
    k.add("shoulders", tube([(-0.1, 0.0, 0.79), (0, 0.0, 0.81), (0.1, 0.0, 0.79)], [0.04, 0.045, 0.04]), HUSK, "body")
    k.paint(pl.folds((0, 0), 12, 2.6, 0.84, 0.68, phase=10, wobble=2.0), HUSK_FOLD, on=["bodice", "shoulders"])
    # husk bands crossed over the breast, front and back
    def cross(P):
        z = P[:, 2] - 0.755
        m = np.zeros(len(P))
        for s in (1, -1):
            m = np.maximum(m, inside(np.abs(P[:, 0] - s * 0.55 * z) - 0.009, 0.0025))
        return m * inside(np.abs(z) - 0.07, 0.004)
    k.paint(cross, HUSK_BROWN, on=["bodice"])
    # frayed husk at the shoulders
    for s, nm in ((1, "R"), (-1, "L")):
        for j, (dy, dz) in enumerate(((-0.02, 0.0), (0.02, 0.01))):
            r = np.array([0.1 * s, dy, 0.81 + dz])
            k.add(f"fray{nm}{j}", hull([r + (0, -0.018, -0.02), r + (0, 0.018, -0.02), r + (-0.03 * s, 0, 0.02),
                                       r + (0.06 * s, 0.0, 0.03), r + (0.02 * s, 0.0, -0.025)]), HUSK_DEEP, "body",
                  fuse=False, surface="leaf")
    pl.cord_ring(k, "sash", (0, 0, WAIST + 0.02), 0.068, 0.012, "body", wraps=3, dz=0.016,
                 tails=((1, -0.2), 0.06))
    # the skirt: a core, then three ragged tiers, the longest under
    k.add("core", tube([(0, 0, 0.68), (0, 0, 0.5), (0, 0, 0.3)], [0.07, 0.12, 0.13]), HUSK_DEEP, "body")
    pl.skirt_ring(k, "tierC", (0, 0, 0.47), 0.11, 0.15, 0.24, 15, 0.07, 0.024,
                  [HUSK_BROWN, HUSK_GREEN, HUSK_DEEP, HUSK_FOLD, HUSK_GREEN], drops=(0.0, 0.05, 0.02), phase=4,
                  sway=0.12)
    pl.skirt_ring(k, "tierB", (0, 0, 0.6), 0.09, 0.31, 0.215, 14, 0.07, 0.024,
                  [HUSK_GREEN, HUSK, HUSK_DEEP, HUSK_BROWN], drops=(0.0, 0.04), phase=15, sway=0.1)
    pl.skirt_ring(k, "tierA", (0, 0, 0.675), 0.07, 0.46, 0.17, 13, 0.064, 0.024,
                  [HUSK, HUSK_DEEP, HUSK_GREEN], drops=(0.0, 0.03, 0.015), phase=8, sway=0.08)
    # the pointed husk feet under it
    for s, nm in ((1, "R"), (-1, "L")):
        k.add(f"leg{nm}", tube([(0.07 * s, 0, 0.37), (0.06 * s, -0.01, 0.2), (0.055 * s, -0.03, 0.14)],
                               [0.03, 0.026, 0.022]), HUSK_DEEP, f"leg{nm}")
        k.add(f"toe{nm}", hull([(0.04 * s, -0.04, 0.15), (0.075 * s, -0.04, 0.15), (0.057 * s, -0.01, 0.16),
                                (0.058 * s, -0.08, 0.112)]), HUSK_FOLD, f"leg{nm}", fuse=False, surface="leaf")


def arms(k):
    """Ragged sleeves tied at the elbow and the wrist, long husk fingers; held forward and down."""
    for s, nm in ((1, "R"), (-1, "L")):
        sh = np.array([0.1 * s, 0.0, 0.78])
        el = np.array([0.16 * s, -0.04, 0.65])
        wr = np.array([0.18 * s, -0.11, 0.54])
        bone = f"arm{nm}"
        k.add(f"arm{nm}", tube([sh, el, wr], [0.036, 0.03, 0.026]), HUSK, bone)
        d = unit(*(wr - el))
        pl.cord_ring_at(k, f"elbow{nm}", el + d * 0.01, d, 0.031, 0.009, bone)
        pl.cord_ring_at(k, f"wrist{nm}", wr - d * 0.01, d, 0.027, 0.009, bone)
        # the ragged sleeve: blades hanging from the forearm, out and back
        for j, (ang, L) in enumerate(((-60, 0.1), (20, 0.13), (100, 0.11))):
            t = math.radians(ang)
            a = np.cross(d, (0, 0, 1.0)); a /= np.linalg.norm(a)
            b = np.cross(d, a)
            radial = math.cos(t) * a + math.sin(t) * b
            root = el + d * 0.02 + radial * 0.025
            down = unit(*(d * 0.6 + radial * 0.35 + np.array([0, 0, -0.5])))
            tang = np.cross(down, radial); tang /= np.linalg.norm(tang)
            k.add(f"sleeve{nm}{j}", pl.slab(pl.blade_outline(L, 0.05), root, down, tang, 0.02),
                  HUSK_DEEP if j % 2 else HUSK_GREEN, bone, fuse=False, surface="leaf")
        # long fingers of husk
        for j, spread in enumerate((-0.5, 0.0, 0.5)):
            side = np.cross(d, (0, 0, 1.0)); side /= np.linalg.norm(side)
            f = unit(*(d + side * spread * 0.5 + np.array([0, 0, -0.35])))
            base = wr + d * 0.01 + side * spread * 0.02
            k.add(f"finger{nm}{j}", outward(tube([base, base + f * 0.045, base + f * 0.085 + (0, 0, -0.012)],
                                                 [0.016, 0.013, 0.008], seg=10, per=5)), HUSK_FOLD, bone,
                  fuse=False, surface="leaf")


def eye_open(u, v):
    """A dark slanted socket with a glowing amber seed in it."""
    c = np.tile(SOCKET, (len(u), 1))
    r = np.sqrt((u * 1.3) ** 2 + (v * 1.15) ** 2)
    c[r < 0.62] = AMBER
    c[r < 0.3] = AMBER_CORE
    return c


def panusa(src, out):
    k = Kit(src, coat="leaf")
    head(k)
    hair(k)
    body(k)
    arms(k)
    k.build(voxel=0.007, smooth=4, band=0.006)

    d = unit(0.42, -0.9, 0.04)
    p = HC + d * np.array([0.06, 0.066, 0.08]) + np.array([0, 0, 0.012])
    # almond, its outer corner higher: the eerie slant
    shape = polygon([(-1.0, -0.25), (-0.4, -0.75), (0.5, -0.55), (1.0, 0.25), (0.4, 0.7), (-0.5, 0.35)])
    def eyes(state):
        k.decal(p, d, (0.025, 0.019), eye_look(state, eye_open, srgb(MASK), SOCKET, line=0.2), eps=0.0045,
                outline=shape, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    panusa(*args())
