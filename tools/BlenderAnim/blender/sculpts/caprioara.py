# Caprioara sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/caprioara.py -- <the export's Caprioara.glb> <out.glb>
# Capriorel grown (built by capriorel.py's own roe(): its trunk, legs, hooves, ears, eyes and spots, the doe's
# skeleton being the fawn's at 1.5): from the picture (10095-Caprioara.jpg), the roe doe of the beech woods: the same
# reddish coat still dotted with cream spots, a cream throat and a pale underside, a white rump patch at the back of
# the haunches; a longer muzzle, large dark eyes with a dark upper lid and lashes; her ears and her neck wreathed in
# wild flowers (pink-white anemones with a rose middle, hanging bluebells, small blue stars, leaves), an upright fern
# frond laid back between her ears where the fawn's sprout was, fern fronds sweeping from her tail and a sprig on
# each hock. The picture's lifted foreleg is the export's rest pose: every foot down.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, lathe, srgb, inside, smoothstep, unit, args, sibling

R = sibling("capriorel")
D = sibling("deerling")          # flower: petals of flat-faced plates round a middle

ANEMONE, ROSE = "#F7DCDD", "#C24E68"
BLUEBELL = "#6C8ADB"
STAR, STARMID = "#7FA4EC", "#F3E07A"
WHITE = "#F6F1E8"
S = 1.5


def frond(k, name, base, d, L, W, thick, colour, bone, across):
    """A fern frond: a flat plate with a hard rim and a serrated outline of leaflets, tapering to its tip."""
    d = unit(*d)
    ex = np.array(across, float); ex = ex - d * (ex @ d); ex /= np.linalg.norm(ex)
    right = [(0.18 * W, 0.0)]
    n = 7
    for j in range(n):
        t0 = 0.06 + 0.86 * j / n
        taper = (1 - t0) ** 0.55
        right += [(W * taper, (t0 + 0.06) * L), (0.42 * W * taper, (t0 + 0.11) * L)]
    out = right + [(0, L)] + [(-a, b) for a, b in right[::-1]]
    k.add(name, slab(out, base, ex, d, thick), colour, bone, fuse=False, surface="leaf")


def bluebell(k, name, top, s, bone):
    """A bell hanging from a short green stalk: a lathe open downward, closed at its lip."""
    top = np.array(top, float)
    k.add(name + "stalk", tube([top + np.array([0, 0, 0.012 * s]), top], [0.0075 * s, 0.007 * s]), R.LEAFDK, bone,
          fuse=False, surface="leaf")
    k.add(name, lathe(top, (0, 0, -1), [(0, 0), (0.008 * s, 0.002 * s), (0.011 * s, 0.012 * s), (0.014 * s, 0.022 * s),
                                        (0.016 * s, 0.026 * s), (0.0, 0.024 * s)]),
          BLUEBELL, bone, fuse=False, surface="leaf")


def eye_lashes(u, v):
    """The fawn's eye with the doe's dark upper lid and a flick of lashes at its outer corner (inside the circle)."""
    c = R.eye_open(u, v)
    r = np.hypot(u, v)
    c[(r > 0.72) & (v > 0.15)] = R.INK
    c[(u > 0.45) & (v > 0.35) & (r > 0.62) & (r < 0.98)] = R.INK
    return c


def doe(k, s, f):
    H, HR = f["H"], f["HR"]
    leaf = f["leaf"]
    # --- the cream throat (a V narrowing down the neck's front), the pale underside, the white rump --------------
    def axis_y(z):
        return -0.192 - (z - 0.555) * 0.053 / 0.24
    e = 0.0045
    def throat(P):
        z = P[:, 2]
        wide = 0.012 + 0.13 * np.clip(z - 0.62, 0, None)
        return (inside(P[:, 1] - (axis_y(z) - 0.05), e) * inside(np.abs(P[:, 0]) - wide, e) * inside(0.63 - z, e) *
                inside(z - 0.80, e))
    k.paint(throat, R.CREAM)
    k.paint(lambda P: inside(P[:, 2] - 0.37, e) * inside(0.29 - P[:, 1], e) * inside(P[:, 1] + 0.27, e),
            R.CREAM, on=["barrel", "chest"])
    # the rump patch: a rounded white patch on the midline under the tail, wider than tall, onto both haunches
    RC, RX, RZ = (0.0, 0.55), 0.095, 0.068
    def rump_d(x, z):
        return np.hypot((x - RC[0]) / RX, (z - RC[1]) / RZ)
    k.paint(lambda P: smoothstep(1.0, 0.72, rump_d(P[:, 0], P[:, 2])) * smoothstep(0.19, 0.24, P[:, 1]),
            WHITE, on=["rump", "thigh", "barrel"])
    # the spots (laid by roe before this hook) kept off it
    k.decals[:] = [d for d in k.decals if not (d["c"][1] > 0.17 and rump_d(d["c"][0], d["c"][2]) < 1.3)]

    # --- flowers caught at each ear's root: an anemone, two bluebells, a leaf ---------------------------------
    for sd in (1, -1):
        D.flower(k, (sd * 0.077, -0.277, 0.92), (sd * 0.45, -0.85, 0.3), 0.034, ANEMONE, ROSE, bone="head",
                 turn=0.3 * sd)
        bluebell(k, f"earbell{sd}a", (sd * 0.118, -0.252, 0.9), s / S, "head")
        bluebell(k, f"earbell{sd}b", (sd * 0.135, -0.235, 0.875), s / S * 0.85, "head")
        leaf(k, f"earleaf{sd}", (sd * 0.09, -0.255, 0.91), (sd * 0.9, 0.15, -0.35), 0.06, 0.02, 0.012, R.LEAF, "head")
    # --- an upright fern frond laid back between the ears, under their tips ------------------------------------
    top = H[2] + HR[2]
    frond(k, "crownfrond", (0, H[1] + 0.012, top - 0.012), (0, 0.42, 1.0), 0.12, 0.028, 0.018, R.LEAF, "head",
          (1, 0, 0))
    D.flower(k, (0, H[1] - 0.04, top - 0.006), (0, -0.7, 0.75), 0.022, STAR, STARMID, bone="head")

    # --- the garland on each side of the neck's base ----------------------------------------------------------
    for sd in (1, -1):
        D.flower(k, (sd * 0.082, -0.245, 0.64), (sd * 0.82, -0.52, 0.12), 0.036, ANEMONE, ROSE, bone="head",
                 turn=0.6 * sd)
        D.flower(k, (sd * 0.042, -0.29, 0.6), (sd * 0.35, -0.92, 0.1), 0.024, STAR, STARMID, bone="head")
        bluebell(k, f"neckbell{sd}", (sd * 0.1, -0.205, 0.6), s / S, "head")
        leaf(k, f"neckleaf{sd}a", (sd * 0.088, -0.22, 0.64), (sd * 0.8, 0.45, 0.4), 0.065, 0.022, 0.013, R.LEAFDK, "head")
        leaf(k, f"neckleaf{sd}b", (sd * 0.075, -0.27, 0.615), (sd * 0.5, -0.5, -0.7), 0.055, 0.02, 0.013, R.LEAF, "head")

    # --- fern fronds sweeping from the tail toward +X, a sprig on each hock -------------------------------------
    tt = np.array([0, 0.305, 0.72])
    for i, (d, L) in enumerate((((0.5, 0.55, 0.68), 0.15), ((0.05, 0.75, 0.66), 0.13), ((0.75, 0.45, 0.25), 0.11))):
        d = unit(*d)
        frond(k, f"tailfrond{i}", tt, d, L, 0.032, 0.018, R.LEAF if i != 1 else R.LEAFDK, "tail",
              np.cross(d, (0, 0, 1.0)))
    for sd in (1, -1):
        d = unit(sd * 0.55, 0.5, 0.65)
        frond(k, f"hockfrond{sd}", (sd * 0.085, 0.235, 0.2), d, 0.085, 0.022, 0.016, R.LEAF, f"hind{'R' if sd > 0 else 'L'}",
              np.cross(d, (0, 0, 1.0)))


doe.eye = eye_lashes


def caprioara(src, out_path):
    R.roe(src, out_path, S=S, head=1.0, muzzle=1.15, ears=0.95, eye=0.97, leg=1.12, coat="#B0593A", extras=doe, sprout=False)


if __name__ == "__main__":
    caprioara(*args())
