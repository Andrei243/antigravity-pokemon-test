# Racoroi sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/racoroi.py -- <the export's Racoroi.glb> <out.glb>
#
# Carpathia No. 10154, an armoured crayfish, grown from Racut (racut.py: its carapace, tail, legs, pincer and feelers,
# at the scale K of the larger skeleton). From its picture: a large crayfish plated in dull dark bronze like a
# knight: overlapping plates across the carapace with rivets at their ends, a helmet over the head whose brow
# overhangs two small black eyes, a pointed rostrum like a visor's nose. One claw much bigger than the other: on +X a
# crushing hammer, a great blunt block of plate with a maroon finger closing along its inner face, raised high; on -X
# a small pincer of bronze with maroon fingers. Orange feelers with jagged barbs; four plated legs a side ending in
# sharp points; the plated tail raised behind it, ending in a fan of plates swept out toward +X.
#
# Joints: body (0, 0, 0.187), head (0, -0.174, 0.216), arm{s} (+-0.087, -0.145, 0.187), leg{s} (+-0.072, 0, 0.143),
# tail (0, 0.116, 0.187). The hammer is on +X, where the game's front camera sees it; neither claw is before the face.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, hull, sheet, ring_band, srgb, inside, unit, eye_look, args, outward, sibling

rc = sibling("racut")

BRONZE, BDARK, BUNDER, MAROON, RIVET, LEGC, FEELER = ("#5C4828", "#2A2012", "#6C5634", "#5A2220", "#B0945C",
                                                      "#5C4828", "#CC7A2E")
WORN = "#9C7E4C"                 # the lighter worn edge of each plate
K = 1.47


# --- plates over the carapace, each a curved band standing just off it, with rivets at its ends --------------------
CARA_C = np.array([0.0, -0.02, 0.135]) * K
CARA_R = np.array([0.07, 0.1, 0.06]) * K


def plate_band(y0, width, rise=1.07, span=74, thick=0.012):
    """A plate across the carapace at y0: an arc over the top from one side to the other."""
    def at(u, v):
        th = np.radians(-span + 2 * span * u)
        y = y0 + (v - 0.5) * width
        # the carapace's section at y, a little larger
        q = np.sqrt(np.clip(1 - ((y - CARA_C[1]) / (CARA_R[1] * 1.02)) ** 2, 0.15, 1))
        x = CARA_R[0] * rise * q * np.sin(th)
        z = CARA_C[2] + CARA_R[2] * rise * q * np.cos(th) + 0.004 * (v - 0.5) * 2
        return np.stack([x, y, z], axis=-1)
    return outward(sheet(at, 25, 4, thick))


def armour(k):
    for i, y0 in enumerate((-0.098, -0.04, 0.018, 0.074)):
        k.add(f"plate{i}", plate_band(y0, 0.066, rise=1.05 + 0.012 * i), BRONZE, "body", fuse=False, surface="metal")
        # the plate's worn rear edge, lighter, standing a little proud of it, and the dark joint under the next
        k.add(f"plateedge{i}", plate_band(y0 + 0.026, 0.014, rise=1.06 + 0.012 * i, span=76, thick=0.014), WORN,
              "body", fuse=False, surface="metal")
        for s in (1, -1):
            th = math.radians(62)
            q = math.sqrt(max(1 - ((y0 - CARA_C[1]) / (CARA_R[1] * 1.02)) ** 2, 0.15))
            p = (s * CARA_R[0] * 1.12 * q * math.sin(th), y0, CARA_C[2] + CARA_R[2] * 1.12 * q * math.cos(th))
            k.add(f"rivet{i}{'R' if s > 0 else 'L'}", ell(p, (0.008,) * 3, sub=2), RIVET, "body", fuse=False,
                  surface="metal")


# --- the helmet: a brow of plate over the head, its front edge over the eyes ---------------------------------------
EYE_BALL = np.array([0.044, -0.222, 0.27])
EYE_R = 0.023


def helmet(k):
    # a curved plate over the head's top, its front edge just over the eyes (a hull would fill in under the curve)
    def at(u, v):
        th = np.radians(-72 + 144 * u)
        y = -0.24 + 0.14 * v
        return np.stack([0.084 * np.sin(th), y, 0.25 + 0.066 * np.cos(th) - 0.012 * v], axis=-1)
    k.add("helmet", outward(sheet(at, 25, 6, 0.014)), BRONZE, "head", fuse=False, surface="metal")
    # a ridge down its middle, like a helm's crest
    k.add("helmetridge", hull([(x, y, z) for x in (-0.012, 0.012) for y, z in ((-0.236, 0.31), (-0.1, 0.298))]
                              + [(0, -0.236, 0.328), (0, -0.1, 0.314)]), BDARK, "head", fuse=False, surface="metal")
    for s in (1, -1):
        k.add(f"hrivet{'R' if s > 0 else 'L'}", ell((s * 0.05, -0.16, 0.305), (0.008,) * 3, sub=2),
              RIVET, "head", fuse=False, surface="metal")


def eyes(k):
    k.add("stalk", tube([(0.018, -0.19, 0.24), EYE_BALL], [0.013, 0.012], seg=14), BRONZE, "head", mirror=True)
    k.add("eyeball", ell(EYE_BALL, (EYE_R,) * 3), BRONZE, "head", mirror=True)


def lay_eyes(k, state):
    n = unit(0.3, -1.0, 0.05)
    c = EYE_BALL + n * EYE_R
    look = eye_look(state, rc.open_eye, srgb(BRONZE), srgb(rc.INK), line=0.16)
    k.decal(c, n, (0.0165, 0.0165), look, eps=0.003, rings=20, segs=56, mirror=True)


# --- the hammer claw on +X and the small pincer on -X --------------------------------------------------------------
HAMMER_ARM = [(0.087, -0.145, 0.187), (0.18, -0.17, 0.28), (0.215, -0.19, 0.375)]
SMALL_ARM = [(0.087, -0.145, 0.187), (0.15, -0.2, 0.22), (0.168, -0.22, 0.27)]


def box(c, a, n, d, half, ch):
    """A box in the frame (a, n, d) about c with chamfered edges: a flat-faced hull."""
    pts = []
    for sa in (-1, 1):
        for sn_ in (-1, 1):
            for sd in (-1, 1):
                for e in range(3):
                    o = [half[0] * sa, half[1] * sn_, half[2] * sd]
                    o[e] -= ch * [sa, sn_, sd][e]
                    pts.append(c + a * o[0] + n * o[1] + d * o[2])
    return hull(pts)


def hammer(k):
    """The crushing claw: on a thick jointed arm, a palm, the heavy squared upper finger (a block with raised worn
    bands, its inner face maroon) and a shorter maroon lower finger closing against it across a gap the front camera
    sees, raised high and out to +X, clear of the face."""
    rc.arm(k, 1, 1.0, pts=HAMMER_ARM, r=(0.04, 0.037, 0.04), color=BRONZE)
    for i, (p, r) in enumerate(((HAMMER_ARM[1], 0.041), (HAMMER_ARM[2], 0.043))):
        q = np.array(HAMMER_ARM[min(i + 2, 2)]) - np.array(HAMMER_ARM[i])
        k.add(f"armjoint{i}", ring_band(p, q, r, 0.016, thick=0.01), BDARK, "armR", fuse=False, surface="metal")
    W = np.array(HAMMER_ARM[-1], float)
    d = unit(0.12, -0.4, 1.0)
    n = np.array([0.3, -1.0, 0.0]); n = n - d * (n @ d); n /= np.linalg.norm(n)
    a = np.cross(n, d)
    if a[0] < 0:
        a = -a
    k.add("hammerpalm", box(W + d * 0.045 - a * 0.012, a, n, d, (0.095, 0.06, 0.05), 0.016), BRONZE, "armR", fuse=False,
          surface="metal")
    bc = W + d * 0.165 + a * 0.035
    k.add("hammer", box(bc, a, n, d, (0.055, 0.062, 0.085), 0.02), BRONZE, "armR", fuse=False, surface="metal")
    for i, t in enumerate((-0.04, 0.03)):
        k.add(f"hammerband{i}", box(bc + d * t, a, n, d, (0.061, 0.068, 0.011), 0.008), WORN, "armR", fuse=False,
              surface="metal")
    # the block's inner face maroon, seen in the gap
    k.add("hammerpad", box(bc - a * 0.052, a, n, d, (0.007, 0.05, 0.072), 0.004), MAROON, "armR", fuse=False,
          surface="metal")
    # the lower finger: shorter, its tip hooking toward the block
    pts = []
    for dd, a0, a1, t in ((0.075, -0.1, -0.064, 0.028), (0.15, -0.096, -0.064, 0.023), (0.2, -0.082, -0.058, 0.013)):
        for aa in (a0, a1):
            for s in (-1, 1):
                pts.append(W + d * dd + a * aa + n * s * t)
    pts.append(W + d * 0.228 - a * 0.05)
    k.add("hammerfinger", hull(pts), MAROON, "armR", fuse=False, surface="metal")
    for i, (x, z) in enumerate(((0.55, 0.065), (-0.2, 0.065), (0.55, -0.065))):
        k.add(f"hrv{i}", ell(bc + a * x * 0.055 - n * 0.064 + d * z, (0.008,) * 3, sub=2), RIVET, "armR",
              fuse=False, surface="metal")


def small_claw(k):
    rc.arm(k, -1, 1.0, pts=SMALL_ARM, r=(0.026, 0.024, 0.024), color=BRONZE)
    rc.claw(k, -1, 1.0, SMALL_ARM[-1], (-0.1, -0.2, 1.0), (0.3, -1.0, 0.1), size=1.15, green=BRONZE, red=MAROON)


# --- the plated tail raised behind, its fan of plates turned out to +X -------------------------------------------
TAIL = [(0, 0.11, 0.2), (0.005, 0.17, 0.215), (0.015, 0.225, 0.245), (0.03, 0.265, 0.29), (0.045, 0.29, 0.335), (0.055, 0.3, 0.375)]
TAIL_R = [0.075, 0.07, 0.064, 0.057, 0.05, 0.043]


def tail_fan(k):
    end = np.array(TAIL[-1], float)
    up = unit(0.05, 0.3, 1.0)
    out = unit(0.82, -0.57, 0.0)             # the fan's plane turned some 35 degrees to +X from across
    for i, ang in enumerate((-50, -25, 0, 25, 50)):
        th = math.radians(ang)
        dirn = up * math.cos(th) + out * math.sin(th)
        side = np.cross(dirn, np.cross(out, up)); side /= np.linalg.norm(side)
        L = 0.125 - 0.016 * abs(ang) / 25
        w = 0.034
        outline = [(-w * 0.6, 0.0), (w * 0.6, 0.0), (w, L * 0.75), (w * 0.4, L), (-w * 0.4, L), (-w, L * 0.75)]
        k.add(f"fan{i}", slab(outline, end + dirn * 0.01, side, dirn, 0.018), BRONZE if i % 2 else "#7A6238", "tail",
              fuse=False, surface="metal")


# --- plated legs ending in sharp points ---------------------------------------------------------------------------
LEGS = [(-0.1, 0.0), (-0.05, 0.01), (0.0, 0.015), (0.05, 0.01)]        # (y of the root, more reach)


def legs(k):
    """Four plated legs a side, bent: up and out to a high knee, then down to a sharp dark point on the ground."""
    for i, (y, more) in enumerate(LEGS):
        root = np.array((0.066, y, 0.15))
        knee = np.array((0.15 + more, y * 1.2, 0.2))
        ankle = np.array((0.19 + more, y * 1.4, 0.075))
        k.add(f"leg{i}", outward(tube([root, knee, ankle], [0.019, 0.016, 0.014], seg=12, per=6)), LEGC, "leg{s}",
              fuse=False, mirror=True, surface="metal")
        foot = np.array((0.205 + more, y * 1.5, 0.0))
        d = unit(*(foot - ankle))
        a = np.cross(d, (0, 1.0, 0)); a /= np.linalg.norm(a)
        b = np.cross(d, a)
        base = ankle - d * 0.01
        pts = [base + (a * math.cos(t) + b * math.sin(t)) * 0.019 for t in np.linspace(0, 2 * math.pi, 6,
                                                                                         endpoint=False)]
        pts.append(foot)
        k.add(f"legtip{i}", hull(pts), BDARK, "leg{s}", fuse=False, mirror=True, surface="metal")
        k.add(f"knee{i}", ring_band(knee, ankle - root, 0.02, 0.014, thick=0.008), BDARK, "leg{s}", fuse=False,
              mirror=True, surface="metal")


def wear(k):
    """What tells the armour from Racut's shell: the carapace dark where its plates' joints show between them, and a
    lighter worn edge just before each dark joint of the tail."""
    k.paint(lambda P: inside(CARA_C[2] + 0.01 - P[:, 2], 0.006), "#3A2C18", on=["carapace"])
    P0 = np.array(TAIL, float)
    def edges(Q):
        m = np.zeros(len(Q))
        for i in range(len(P0) - 1):
            b = (P0[i] + P0[i + 1]) / 2
            t = unit(*(P0[i + 1] - P0[i]))
            s = (Q - b) @ t
            m = np.maximum(m, inside(np.abs(s + 0.009) - 0.0045, 0.0015)
                           * inside(np.linalg.norm(Q - b, axis=1) - 0.1, 0.004))
        return m
    k.paint(edges, WORN, on=["tail"])


def racoroi(src, out):
    k = Kit(src, coat="metal")
    rc.carapace(k, K, color=BRONZE, under=BUNDER)
    rc.grooves(k, K, line=BDARK)
    rc.tail(k, 1.0, color=BRONZE, line=BDARK, pts=TAIL, radii=TAIL_R, fan=False)
    wear(k)
    eyes(k)
    hammer(k)
    small_claw(k)
    rc.rostrum(k, K, color=BRONZE)
    armour(k)
    helmet(k)
    tail_fan(k)
    legs(k)
    rc.feelers(k, K, color=FEELER, root=(0.046, -0.15, 0.19), up=(0.5, -0.25, 1.0), scale=(0.62, 1.15))
    k.build(voxel=0.004, smooth=3, band=0.005, wband=0.03)
    k.eye_states(lambda state: lay_eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    racoroi(*args())
