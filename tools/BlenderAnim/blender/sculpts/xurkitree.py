# Xurkitree sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/xurkitree.py -- <the export's Xurkitree.glb> <out.glb>
#
# From the references: a figure made of black cables. For a head a great white star of crystal spikes, pale and icy, a
# third of the height across, with no face (hidden eyes: patches of the core's own white); a thin black neck; a body
# of cables twisted into a long bundle; arms of cable that loop out and back to a black knot (carried forward and down
# here) from which copper wires splay like stripped ends; legs of twisted cable to black plug-like feet with copper
# prongs; white bands clamped round the cables at neck, body, arms and legs, each with a pale little leaf-tab.
# The skeleton: body (0, 0, 0.55), armL/R (-+0.02, 0, 0.75), head (0, 0, 0.8), legL/R (-+0.05, 0, 0.44).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, leaf, ring_band, outward, srgb, unit, axes, eye_look, args)

BLACK, BAND, TAB, COPPER = "#2C2E33", "#E8EDF0", "#CFE2EA", "#D27A36"
ICE, ICE2, CORE = "#F1F6F8", "#D8E7EE", "#EEF4F7"

HC = np.array([0.0, 0.0, 0.815])
VOXEL = 0.0055


def bundle(k, name, bone, pts, n, r, spread, turns, mirror=False):
    """n cables twisted round a line through pts (a helix of turns about it), each a tube of radius r."""
    P = np.array(pts, float)
    t = np.linspace(0, 1, 9)
    L = np.vstack([np.interp(t, np.linspace(0, 1, len(P)), P[:, j]) for j in range(3)]).T
    d = unit(*(P[-1] - P[0]))
    a, b = axes(d)
    for i in range(n):
        ph = 2 * math.pi * i / n
        Q = [L[j] + spread * (math.cos(ph + 2 * math.pi * turns * t[j]) * a + math.sin(ph + 2 * math.pi * turns * t[j]) * b)
             for j in range(len(t))]
        k.add(f"{name}{i}_", tube(Q, [r] * len(Q)), BLACK, bone, mirror=mirror)


def band(k, name, bone, c, axis, radius, tab_dir=None):
    """A white clamp round a cable, with a pale leaf-tab standing out of it."""
    k.add(name, ring_band(c, axis, radius, 0.022, thick=0.008), BAND, bone, fuse=False)
    if tab_dir is not None:
        d = unit(*tab_dir)
        k.add(name + "tab", outward(leaf(np.array(c) + d * (radius - 0.004), d, 0.055, 0.016, thick=0.01)), TAB, bone,
              fuse=False)


def xurkitree(src, out):
    k = Kit(src, coat="scales")

    # --- the body: three cables twisted into a long bundle, a neck to the head ----------------------------------------
    bundle(k, "trunk", "body", [(0, 0.0, 0.44), (0, -0.005, 0.58), (0, 0.0, 0.72)], 3, 0.026, 0.024, 0.8)
    k.add("neck", tube([(0, 0.0, 0.7), (0, -0.005, 0.76), (0, 0.0, 0.8)], [0.028, 0.024, 0.026]), BLACK, "head")
    k.add("hips", ell((0, 0.0, 0.445), (0.07, 0.04, 0.035)), BLACK, "body")

    # --- the arms: a cable out to the elbow and on to a black knot before the body, a second looping under it ------
    SH, EL, WR = np.array([0.03, 0.0, 0.7]), np.array([0.15, -0.01, 0.62]), np.array([0.13, -0.14, 0.53])
    k.add("arm", tube([SH, (SH + EL) / 2 + (0, 0, 0.01), EL, (EL + WR) / 2 + (0.02, 0, 0), WR],
                      [0.024, 0.023, 0.022, 0.022, 0.021]), BLACK, "arm{s}", mirror=True)
    k.add("loop", tube([(0.06, -0.005, 0.67), (0.13, 0.01, 0.56), (0.17, -0.04, 0.5), (0.16, -0.11, 0.5), WR],
                       [0.017, 0.017, 0.017, 0.017, 0.017]), BLACK, "arm{s}", mirror=True)
    k.add("knot", ell(WR + (0, -0.012, 0), (0.03, 0.03, 0.03)), BLACK, "arm{s}", mirror=True)
    # copper wires splaying from the knot, bent once each
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        w = (WR + (0, -0.03, 0)) * m
        for j, ang in enumerate((-60, -25, 10, 45, 80)):
            a = math.radians(ang)
            d1 = unit(0.55 * math.cos(a) * s, -0.7, 0.55 * math.sin(a))
            d2 = unit(0.8 * math.cos(a) * s, -0.35, 0.8 * math.sin(a) - 0.25)
            p1 = w + d1 * 0.05
            k.add(f"wire{side}{j}", outward(tube([w - d1 * 0.01, p1, p1 + d2 * 0.045], [0.0125, 0.012, 0.011])), COPPER,
                  f"arm{side}", fuse=False, surface="metal")

    # --- the legs: twisted cables to black plug feet with copper prongs ----------------------------------------------
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        bundle(k, f"leg{side}", f"leg{side}", [np.array(p) * m for p in [(0.045, 0.0, 0.46), (0.07, -0.02, 0.26),
                                                                         (0.078, 0.0, 0.06)]], 2, 0.023, 0.017, 0.6)
        F = np.array([0.08, -0.025, 0.034]) * m
        k.add(f"foot{side}", ell(F, (0.042, 0.064, 0.034)), BLACK, f"leg{side}")
        for j, o in enumerate((-0.022, 0.0, 0.022)):
            b = F + np.array([o, -0.05, -0.012])
            k.add(f"prong{side}{j}", hull([b + (-0.009, 0.01, -0.009), b + (0.009, 0.01, -0.009), b + (0, 0.01, 0.009),
                                           b + (o * 0.3, -0.04, -0.016)]), COPPER, f"leg{side}", fuse=False,
                  surface="metal")
        band(k, f"knee{side}", f"leg{side}", np.array([0.07, -0.02, 0.27]) * m, (0.15 * s, 0.05, 1), 0.044,
             (1.0 * s, -0.2, 0.2))
        band(k, f"ankle{side}", f"leg{side}", np.array([0.077, -0.004, 0.11]) * m, (0, 0, 1), 0.042, (0.6 * s, 0.6, 0.0))
        band(k, f"elbow{side}", f"arm{side}", EL * m, (1.0 * s, 0, -0.5), 0.028, (0.2 * s, 0.4, 1.0))

    # --- white clamps round the body and the neck --------------------------------------------------------------------
    band(k, "waist", "body", (0, 0.0, 0.5), (0, 0, 1), 0.053, (-1.0, 0.3, 0.1))
    band(k, "chest", "body", (0, 0.0, 0.64), (0, 0, 1), 0.053, (1.0, 0.2, 0.2))
    band(k, "collar", "head", (0, -0.004, 0.745), (0, 0, 1), 0.03, None)

    # --- the head: a white star of crystal spikes round a white core -------------------------------------------------
    k.add("core", ell(HC, (0.06, 0.055, 0.06)), CORE, "head")
    golden = math.pi * (3 - math.sqrt(5))
    n = 24
    for i in range(n):
        u = -0.75 + 1.65 * (i + 0.5) / n
        ring = math.sqrt(max(1 - u * u, 0))
        a = i * golden
        d = unit(math.sin(a) * ring, -math.cos(a) * ring * 0.55 - 0.15, u)      # fanned toward the front plane
        L = 0.15 if i % 3 else 0.12
        p, q = axes(d)
        base = HC + d * 0.035
        pts = [base + 0.026 * (math.cos(t) * p + math.sin(t) * q) for t in (0.3, 2.4, 4.4)]
        k.add(f"spike{i}", hull(pts + [HC + d * L]), ICE if i % 2 else ICE2, "head", fuse=False, surface="shell")

    k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.05)

    # --- no face: the eyes hidden under the star, patches of the core's own white -------------------------------------
    def lay(state):
        for s in (1, -1):
            d = unit(0.35 * s, -0.9, 0.1)
            c = HC + d / math.sqrt(((d / np.array([0.06, 0.055, 0.06])) ** 2).sum())
            k.decal(c, d, (0.012, 0.012), CORE, eps=0.0015, rings=20, segs=56)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    xurkitree(*args())
