# Shiftry sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/shiftry.py -- <the export's Shiftry.glb> <out.glb>
#
# Nuzleaf grown (nuzleaf.py: its brown, its green, its ink), from the references: a hunched brown tengu. Its head is
# lost in a great mane of white hair in spiky locks that runs down its back and sweeps out behind it to a broad brush
# low on its +X side; white locks hang from its chin over a brown chest. A long brown pointed nose; fierce yellow eyes
# with a black pupil under a slanting white brow; two pointed brown ears, black inside. Its arms are brown sticks out
# of round shoulder joints, each ending in a fan of three green leaves with a brown rib down each. Thick brown thighs
# bent forward, short shins, and wooden clogs on one tooth for feet.
# The skeleton: body (0, 0, 0.42), arms (+-0.07, 0, 0.5), head (0, -0.02, 0.55), legs (+-0.07, 0, 0.3).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, polygon, hull, slab, sibling, args

nz = sibling("nuzleaf")
BROWN, INK, GREEN = "#B47B4E", nz.INK, "#2E9C48"     # Nuzleaf's brown and ink; its leaf's green grown deeper
RIB, CLOG, WHITE, WHITE_D, YELLOW, EAR_IN = "#A06A40", "#94603A", "#F4F4F6", "#DCDDE4", "#F2CC2A", "#2A1E1C"

HEAD_C = np.array([0.0, -0.055, 0.645])
HEAD_R = np.array([0.105, 0.1, 0.098])


def lock(k, name, base, d, length, w, t, bone, color=WHITE, mirror=False):
    """A lock of hair: a solid tapering blade from base along d, wide across w, t thick (flat across t's way)."""
    d = unit(*d)
    a = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a = a / np.linalg.norm(a)
    b = np.cross(d, a)
    base = np.array(base, float)
    pts = []
    for sw in (1, -1):
        for st in (1, -1):
            pts.append(base + a * w * sw + b * t * st)
            pts.append(base + d * length * 0.45 + a * w * 0.7 * sw + b * t * 0.8 * st)
    pts.append(base + d * length)
    k.add(name, hull(pts), color, bone, fuse=False, mirror=mirror, surface="fur")


def head(k):
    k.add("head", ell(HEAD_C, HEAD_R, sub=5), WHITE, "head")
    # the mane: white masses behind the head and down the back
    k.add("mane", ell((0, 0.035, 0.63), (0.118, 0.1, 0.12)), WHITE, "head")
    k.add("maneback", ell((0, 0.075, 0.5), (0.11, 0.085, 0.13)), WHITE, "body")
    k.add("manelow", ell((0, 0.085, 0.385), (0.1, 0.075, 0.09)), WHITE, "body")
    # the nose: long, brown, forward and a little down
    root = np.array([0, HEAD_C[1] - HEAD_R[1] + 0.015, 0.618])
    d = unit(0, -1, -0.18)
    k.add("nose", tube([root, root + d * 0.08, root + d * 0.165], [0.03, 0.019, 0.006]), BROWN, "head")
    # the ears: pointed, brown, a black hollow on the front
    for s in (1, -1):
        b = HEAD_C + np.array([s * 0.07, 0.01, 0.075])
        tip = b + np.array([s * 0.05, 0.02, 0.115])
        pts = [b + (s * 0.03, -0.018, 0), b + (-s * 0.03, -0.018, 0), b + (s * 0.03, 0.02, 0),
               b + (-s * 0.03, 0.02, 0), b + (0, -0.02, -0.02), tip]
        k.add(f"ear{'R' if s > 0 else 'L'}", hull(pts), BROWN, "head", fuse=False, surface="fur")
        q = b + np.array([0, -0.019, 0.012])
        tq = tip + np.array([-s * 0.006, -0.012, -0.03])
        pts = [q + (s * 0.019, 0, 0), q + (-s * 0.019, 0, 0), q + (s * 0.017, 0.006, 0), q + (-s * 0.017, 0.006, 0),
               tq, tq + (0, 0.006, 0)]
        k.add(f"earin{'R' if s > 0 else 'L'}", hull(pts), EAR_IN, "head", fuse=False, surface="fur")


def mane_locks(k):
    """Spiky white locks round the head and down the back, the beard hanging over the chest, and the brush behind."""
    # round the back and sides of the head: (azimuth from the front toward +X in degrees, height, length)
    for i, (az, z, ln) in enumerate(((70, 0.69, 0.1), (95, 0.62, 0.12), (110, 0.7, 0.11), (130, 0.6, 0.12),
                                     (145, 0.68, 0.11), (165, 0.62, 0.12), (180, 0.7, 0.1))):
        a = math.radians(az)
        out = np.array([math.sin(a), -math.cos(a), 0.0])
        base = np.array([0, 0.035, z]) + out * 0.085
        d = out * 0.8 + np.array([0, 0.45, 0.05 if z > 0.65 else -0.15])
        mirror = az != 180
        lock(k, f"lock{i}", base, d, ln, 0.032, 0.014, "head", mirror=mirror)
    # down the back
    for i, (x, z, ln) in enumerate(((0.07, 0.52, 0.09), (0.0, 0.47, 0.1), (0.06, 0.42, 0.09))):
        lock(k, f"back{i}", (x, 0.13, z), (x * 4, 0.7, -0.5), ln, 0.035, 0.015, "body", mirror=x != 0)
    # the beard: locks hanging from the chin and cheeks over the chest
    for i, (x, ln) in enumerate(((0.0, 0.1), (0.04, 0.09), (0.075, 0.075))):
        lock(k, f"beard{i}", (x, -0.105 + 0.03 * abs(x) / 0.075, 0.58), (x * 1.5, -0.25, -1), ln, 0.026, 0.012,
             "head", mirror=x != 0)
    # the brush behind: a fan of broad locks from the small of the back, back, down and out to +X
    for i, (dx, dz, ln) in enumerate(((0.2, -0.45, 0.2), (0.45, -0.4, 0.22), (0.7, -0.5, 0.2), (0.3, -0.75, 0.18),
                                      (0.62, -0.78, 0.17))):
        lock(k, f"brush{i}", (0.03, 0.14, 0.38), (dx, 0.8, dz), ln, 0.05, 0.018, "body")


def body(k):
    k.add("chest", ell((0, -0.03, 0.44), (0.118, 0.095, 0.11)), BROWN, "body")
    k.add("hips", ell((0, 0.0, 0.33), (0.11, 0.09, 0.08)), BROWN, "body")


def arms(k):
    """Round shoulder joints, brown sticks forward and down, a fan of three green leaves at each end."""
    k.add("shoulder", ell((0.11, -0.01, 0.5), (0.048, 0.048, 0.048)), BROWN, "arm{s}", mirror=True)
    hand = np.array([0.19, -0.12, 0.4])
    k.add("stick", tube([(0.13, -0.025, 0.49), (0.165, -0.075, 0.445), hand], [0.024, 0.022, 0.022]), BROWN, "arm{s}",
          mirror=True)
    k.add("hub", ell(hand, (0.03, 0.03, 0.03)), BROWN, "arm{s}", mirror=True)
    a = unit(0.55, 0.3, -0.75)
    n0 = unit(0.05, -1, 0.2)
    n = n0 - a * (a @ n0); n /= np.linalg.norm(n)
    spread = np.cross(n, a)
    L, W = 0.21, 0.052
    leaf = [(t * L, W * math.sin(math.pi * t ** 0.75)) for t in np.linspace(0, 1, 18)]
    leaf += [(t * L, -W * math.sin(math.pi * t ** 0.75)) for t in np.linspace(1, 0, 18)[1:-1]]
    rib = [(0.0, 0.011), (0.14, 0.006), (0.15, 0.0), (0.14, -0.006), (0.0, -0.011)]
    for j, deg in enumerate((-28, 0, 28)):
        th = math.radians(deg)
        d = a * math.cos(th) + spread * math.sin(th)
        ez = np.cross(n, d)
        o = hand + d * 0.015
        k.add(f"fan{j}", slab(leaf, o, d, ez, 0.022), GREEN, "arm{s}", fuse=False, mirror=True, surface="leaf")
        k.add(f"rib{j}", slab(rib, o, d, ez, 0.028), RIB, "arm{s}", fuse=False, mirror=True, surface="leaf")


def legs(k):
    """Thick thighs bent forward, short shins, clogs with one tooth."""
    k.add("thigh", ell((0.1, -0.05, 0.24), (0.09, 0.1, 0.085), rot=(25, 0, 0)), BROWN, "leg{s}", mirror=True)
    k.add("shin", tube([(0.118, -0.09, 0.18), (0.12, -0.08, 0.13), (0.12, -0.078, 0.095)], [0.046, 0.038, 0.034]),
          BROWN, "leg{s}", mirror=True)
    c = np.array([0.12, -0.08, 0.08])
    box = [c + (sx * 0.042, sy * 0.068, sz * 0.012) for sx in (1, -1) for sy in (1, -1) for sz in (1, -1)]
    k.add("clog", hull(box), CLOG, "leg{s}", fuse=False, mirror=True, surface="shell")
    t = c + np.array([0, 0, -0.04])
    box = [t + (sx * 0.034, sy * 0.016, sz * 0.0398) for sx in (1, -1) for sy in (1, -1) for sz in (1, -1)]
    k.add("tooth", hull(box), CLOG, "leg{s}", fuse=False, mirror=True, surface="shell")


# The eyes: yellow under a white brow that slants down to the nose, a black pupil toward the inner corner
EYE_HALF = (0.032, 0.02)
CW, CY, CI = srgb(WHITE), srgb(YELLOW), srgb(INK)


def eye_open(u, v):
    c = np.tile(CW, (len(u), 1))
    top = 0.15 + 0.55 * u
    bottom = -0.7 + 0.25 * u * u
    lens = (v < top) & (v > bottom) & (np.abs(u) < 0.95)
    c[lens] = CI
    inner = (v < top - 0.2) & (v > bottom + 0.2) & (np.abs(u) < 0.8)
    c[inner] = CY
    c[inner & (((u + 0.18) / 0.26) ** 2 + ((v + 0.15) / 0.5) ** 2 < 1)] = CI
    return c


def eyes(k, state):
    p = np.array([0.05, -0.14, 0.652])
    n = unit(0.5, -1, 0.08)
    k.decal(p, n, EYE_HALF, eye_look(state, eye_open, CW, CI, line=0.2), eps=0.004, rings=20, segs=56, mirror=True)


def shiftry(src, out):
    k = Kit(src, coat="fur")
    head(k)
    mane_locks(k)
    body(k)
    arms(k)
    legs(k)
    # the back white where the mane covers it, the chest brown
    k.paint(lambda P: smoothstep(0.035, 0.06, P[:, 1]) * (P[:, 2] > 0.33), WHITE, on=["chest", "hips"])
    k.build(voxel=0.006, smooth=4, band=0.006)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    shiftry(*args())
