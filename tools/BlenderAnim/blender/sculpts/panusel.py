# Panusel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/panusel.py -- <the export's Panusel.glb> <out.glb>
#
# Carpathia No. 10041 (plan 46, CP-dex-1b, "41. Panusel: a corn-husk doll"), from the project's own picture: a doll
# of dried corn husk; a big oval head of husk gathered at the neck, its folds running down it all round; two dark
# seeds for eyes and no mouth; a messy tuft of golden corn silk on top, falling out to the sides; coarse brown string
# tied round the neck and the waist, the knots' ends sticking out on the +X side; a little bodice of husk; arms of
# rolled husk tied at the wrists, their ends flaring like little fists (carried forward and down here, where the
# picture raises one); a skirt of long pointed husk blades, pale straw with streaks of green, to the ground, two
# stubs of husk feet under it.
#
# Written to be grown from (panusa.py imports it with sibling("panusel")): the line's palette, the husk's folds, the
# cords, the blades and the corn silk are the functions below; the calls that build Panusel are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, lathe, srgb, unit, eye_look, inside, args, outward, smoothstep

# --- the line's palette -----------------------------------------------------------------------------------------
HUSK, HUSK_FOLD, HUSK_GREEN, HUSK_DEEP = "#E4D2A2", "#B89C66", "#C8C88E", "#CBB27E"
CORD, CORD_DARK = "#8A6640", "#6A4A2C"
SILK, SILK_DEEP = "#EEC046", "#D89A2C"
SEED = srgb("#3A2214")
SEED_GLINT = srgb("#8A5A3A")

# --- Panusel's frame (joints: body (0, 0, 0.3), arms (+-0.05, 0, 0.335), head (0, 0, 0.38), legs (+-0.035, 0, 0.13))
HC = np.array([0.0, 0.0, 0.42])          # the head's middle
HR = np.array([0.098, 0.086, 0.092])
NECK = 0.335
WAIST = 0.245


def gathered(pinch=0.55, power=2.2):
    """The unit sphere made a head of husk gathered at the neck: narrowing toward its bottom."""
    def f(V):
        V = V.copy()
        s = 1 - pinch * np.clip(-V[:, 2], 0, 1) ** power
        V[:, 0] *= s; V[:, 1] *= s
        return V
    return f


def folds(c, n, width_deg, z0, z1, phase=0.0, wobble=4.0):
    """A mask of the husk's folds: lines running down a part round its upright axis through c, n of them."""
    step = 360.0 / n
    def mask(P):
        th = np.degrees(np.arctan2(P[:, 0] - c[0], -(P[:, 1] - c[1])))
        th = th + wobble * np.sin((P[:, 2] - z0) * 60)
        line = step / 2 - np.abs(((th - phase) % step) - step / 2)          # degrees from the nearest fold
        return inside(line - width_deg / 2, 0.5) * inside(P[:, 2] - z0, 0.004) * inside(z1 - P[:, 2], 0.004)
    return mask


def cord_ring(k, name, c, r, thick, bone, wraps=1, dz=0.0, colour=CORD, tails=None):
    """String wound round a part: loops of a tube, each wrap a little lower, and two frayed ends at a knot."""
    for w in range(wraps):
        pts = []
        for i in range(29):
            a = 2 * math.pi * i / 26
            pts.append((c[0] + r * math.sin(a), c[1] - r * math.cos(a), c[2] - w * dz))
        k.add(f"{name}{w}", outward(tube(pts, [thick] * len(pts), seg=10, per=4)), colour if w % 2 == 0 else CORD_DARK,
              bone, fuse=False, surface="cloth")
    if tails:
        side, length = tails
        base = np.array([c[0] + r * side[0], c[1] + r * side[1], c[2] - (wraps - 1) * dz / 2])
        for j, (dx, dzz) in enumerate(((1.0, 0.35), (0.9, -0.5))):
            d = unit(side[0] * dx, side[1] * dx - 0.2, dzz)
            k.add(f"{name}tail{j}", outward(tube([base, base + d * length * 0.6, base + d * length],
                                                [thick * 1.1, thick * 0.9, thick * 0.6], seg=10, per=4)),
                  CORD, bone, fuse=False, surface="cloth")


def blade_outline(L, w, ragged=1):
    """A husk blade: narrow at its root, broad, ending in ragged points."""
    if ragged == 0:
        return [(0.0, -0.5 * w), (0.3 * L, -w), (0.7 * L, -0.85 * w), (L, 0.0), (0.7 * L, 0.85 * w), (0.3 * L, w),
                (0.0, 0.5 * w)]
    return [(0.0, -0.5 * w), (0.3 * L, -w), (0.72 * L, -0.92 * w), (0.9 * L, -0.62 * w), (L, -0.42 * w),
            (0.84 * L, -0.08 * w), (0.96 * L, 0.32 * w), (0.8 * L, 0.78 * w), (0.32 * L, w), (0.0, 0.5 * w)]


def skirt_ring(k, name, c, r_top, z_tip, r_tip, count, width, thick, colours, bone="body", phase=0.0, drops=(0.0,),
               ragged=1, sway=0.0):
    """A ring of loose husk blades from a waist out and down to a hem, their faces outward."""
    for i in range(count):
        a = math.radians(phase + 360 * i / count)
        radial = np.array([math.sin(a), -math.cos(a), 0.0])
        tangent = np.array([math.cos(a), math.sin(a), 0.0])
        root = np.array(c, float) + radial * r_top
        tip = np.array([c[0], c[1], z_tip + drops[i % len(drops)]]) + radial * r_tip + tangent * sway * r_tip
        d = tip - root
        L = float(np.linalg.norm(d))
        k.add(f"{name}{i}", slab(blade_outline(L, width, ragged), root, d / L, tangent, thick),
              colours[i % len(colours)], bone, fuse=False, surface="leaf")


def silk_strand(k, name, root, d, length, radius, bone, fall=0.4, colour=SILK, curl=0.0):
    """A lock of corn silk from a root along d, falling under its weight, tapering to its end."""
    d = unit(*d)
    pts, radii = [], []
    side = np.cross(d, (0, 0, 1.0))
    side = side / np.linalg.norm(side) if np.linalg.norm(side) > 1e-3 else np.array([1.0, 0, 0])
    for i in range(6):
        t = i / 5
        p = (np.array(root, float) + d * length * t + np.array([0, 0, -1.0]) * fall * length * t * t
             + side * curl * length * math.sin(t * math.pi))
        pts.append(p); radii.append(radius * (1 - 0.55 * t))
    k.add(name, outward(tube(pts, radii, seg=10, per=5)), colour, bone, fuse=False, surface="hair")


def seed_eye(u, v):
    """A dark seed: brown-black with a soft glint high to the outside."""
    c = np.tile(SEED, (len(u), 1))
    c[((u - 0.25) / 0.3) ** 2 + ((v - 0.35) / 0.26) ** 2 < 1] = SEED_GLINT
    return c


# --- Panusel -----------------------------------------------------------------------------------------------------
def head(k):
    k.add("head", ell(HC, HR, deform=gathered(0.6)), HUSK, "head")
    k.add("neck", tube([(0, 0, 0.36), (0, 0, 0.31)], [0.036, 0.04]), HUSK, "head")
    k.paint(folds(HC, 12, 2.4, 0.5, 0.33), HUSK_FOLD, on=["head", "neck"])
    # the corn silk's mass on the crown, and its locks
    k.add("silkcap", ell((0, 0.008, 0.49), (0.088, 0.078, 0.05)), SILK, "head")
    locks = ((0, 0.0, 1.0, 0.11, 0.0), (35, 0.6, 0.8, 0.12, 0.3), (-35, 0.6, 0.8, 0.12, -0.3), (80, 1.0, 0.4, 0.13, 0.4),
             (-80, 1.0, 0.4, 0.13, -0.4), (125, 1.0, 0.25, 0.12, 0.2), (-125, 1.0, 0.25, 0.12, -0.2),
             (180, 0.8, 0.5, 0.11, 0.0), (155, 0.5, 0.9, 0.1, 0.3), (-155, 0.5, 0.9, 0.1, -0.3), (60, 0.3, 1.0, 0.1, 0.2),
             (-60, 0.3, 1.0, 0.1, -0.2), (100, 1.0, 0.0, 0.12, 0.3), (-100, 1.0, 0.0, 0.12, -0.3))
    for i, (deg, out, up, L, curl) in enumerate(locks):
        a = math.radians(deg)
        radial = np.array([math.sin(a), -math.cos(a), 0.0])
        root = np.array([0, 0.008, 0.5]) + radial * 0.05
        d = radial * out + np.array([0, 0, up])
        silk_strand(k, f"silk{i}", root, d, L, 0.011, "head", fall=0.35 + 0.25 * out, curl=curl,
                    colour=SILK if i % 3 else SILK_DEEP)
    cord_ring(k, "neckcord", (0, 0, NECK), 0.04, 0.0085, "body", wraps=2, dz=0.012, tails=((1, -0.2), 0.04))


def body(k):
    k.add("bodice", ell((0, 0, 0.29), (0.058, 0.048, 0.06), deform=gathered(0.3, 1.6)), HUSK, "body")
    k.paint(folds((0, 0), 10, 2.2, 0.33, 0.245, phase=8), HUSK_FOLD, on=["bodice"])
    cord_ring(k, "waistcord", (0, 0, WAIST), 0.05, 0.009, "body", wraps=2, dz=0.012, tails=((1, -0.1), 0.045))
    # the skirt: a core of husk, a ring of long blades to the ground and a shorter ring over it
    k.add("core", tube([(0, 0, 0.245), (0, 0, 0.17), (0, 0, 0.12)], [0.05, 0.085, 0.095]), HUSK_DEEP, "body")
    skirt_ring(k, "skirt", (0, 0, 0.245), 0.045, 0.005, 0.18, 14, 0.052, 0.016,
               [HUSK, HUSK_GREEN, HUSK_DEEP, HUSK, HUSK_FOLD, HUSK_GREEN], drops=(0.0, 0.03, 0.012), phase=6)
    skirt_ring(k, "overskirt", (0, 0, 0.24), 0.052, 0.09, 0.15, 11, 0.05, 0.016, [HUSK, HUSK_DEEP, HUSK_GREEN],
               drops=(0.0, 0.02), phase=20, ragged=0)
    # the feet's stubs under the skirt
    k.add("leg", tube([(0.035, 0.0, 0.13), (0.04, -0.01, 0.03)], [0.022, 0.02]), HUSK_DEEP, "leg{s}", mirror=True)
    k.add("foot", ell((0.042, -0.03, 0.016), (0.026, 0.036, 0.016)), HUSK_DEEP, "leg{s}", mirror=True)


def arms(k):
    """Rolled husk, tied at the wrist, the end flaring into a little fist; held forward and down."""
    for s, nm in ((1, "R"), (-1, "L")):
        sh = np.array([0.05 * s, 0.0, 0.335])
        el = np.array([0.098 * s, -0.03, 0.3])
        wr = np.array([0.112 * s, -0.08, 0.27])
        k.add(f"arm{nm}", tube([sh, el, wr], [0.026, 0.023, 0.02]), HUSK, f"arm{nm}")
        d = unit(*(wr - el))
        k.add(f"fist{nm}", lathe(wr + d * 0.004, d, [(0.0, 0.0), (0.019, 0.0), (0.024, 0.014), (0.032, 0.034),
                                                   (0.022, 0.038), (0.0, 0.03)], seg=12), HUSK, f"arm{nm}",
              fuse=False, surface="leaf")
        cord_ring_at(k, f"wrist{nm}", wr, d, 0.022, 0.007, f"arm{nm}")


def cord_ring_at(k, name, c, axis, r, thick, bone):
    """A loop of string round a limb, square to its axis."""
    a = np.cross(axis, (0, 0, 1.0)); a /= np.linalg.norm(a)
    b = np.cross(axis, a)
    pts = [np.array(c) + r * (math.cos(t) * a + math.sin(t) * b) for t in np.linspace(0, 2 * math.pi * 29 / 26, 29)]
    k.add(name, outward(tube(pts, [thick] * len(pts), seg=10, per=4)), CORD, bone, fuse=False, surface="cloth")


def panusel(src, out):
    k = Kit(src, coat="leaf")
    head(k)
    body(k)
    arms(k)
    k.build(voxel=0.0055, smooth=4, band=0.006)

    d = unit(0.42, -0.9, 0.0)
    p = HC + d * np.array([0.085, 0.072, 0.08]) + np.array([0, 0, 0.004])
    def eyes(state):
        k.decal(p, d, (0.015, 0.019), eye_look(state, seed_eye, srgb(HUSK), SEED, line=0.2), eps=0.0045,
                rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    panusel(*args())
