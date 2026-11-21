# Calyrex sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/calyrex.py -- <the export's Calyrex.glb> <out.glb>
#
# From the references: a small pale king floating in the air under a great dark green bulb of a head, wider than it
# is tall and the largest mass by far, a pale lavender cap on its top with notches round its edge. Four pale sepals
# hold the bulb as a calyx holds a bud: a pair rising from the brow up the bulb's front nearly to its top, a pair from
# under its sides curving out and up. Under the bulb's front a small pale face with a long muzzle and narrow blue eyes
# under a level lid. A collar of dark green beads round the neck, each with a blue diamond and a white heart, its end
# rolled up behind the left cheek. A small pale body, a skirt of pointed leaves round the waist (dark green with
# lavender middles), short pale arms with small hands held forward, and two long thin pale legs ending in points.
# Masses ranked from the pictures: the bulb, the skirt, the collar, the body, the face, the legs.
# Ratios (3D picture, the sepals included): bulb width / height 0.64 (built 0.6), bulb height / height 0.44 (0.43),
# legs below the skirt / height 0.26 (0.27), face height / height 0.13 (0.14).
# Bones: body (0, 0, 0.525); head (0, 0, 0.625); arms (+-0.05, 0, 0.575); legs (+-0.025, 0, 0.435).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, slab, outward, srgb, inside, unit, polygon, eye_look, args, axes, \
    sibling

lv = sibling("larvitar")              # the eye cut by a lid

BULB, CAP, PALE, SHADE = "#215E4C", "#A9AEE2", "#EDEBE4", "#D8D6D0"
BEAD, DIAMOND, HEART = "#1E5446", "#3C5EC2", "#EEF0F8"
LEAF, LEAFMID = "#1E5A48", "#9AA4DE"
IRIS = srgb("#4C78D8")

BC, BR = np.array([0.0, 0.02, 0.84]), np.array([0.272, 0.222, 0.19])      # the bulb
H, HR = np.array([0.0, -0.1, 0.6]), np.array([0.064, 0.068, 0.057])       # the face's skull
NECK_C, RING_R = np.array([0.0, -0.015, 0.548]), 0.072                    # the collar's ring


def join(*shapes):
    V, F, off = [], [], 0
    for sv, sf in shapes:
        V.append(sv); F.extend([[i + off for i in f] for f in sf]); off += len(sv)
    return np.vstack(V), F


def bead(k, name, c, r, out_dir):
    """A bead of the collar: a dark green ball, a blue diamond on its outer face and a white heart in it."""
    k.add(name, ell(c, (r, r, r), sub=3), BEAD, "body", fuse=False, surface="shell")
    n = unit(*out_dir)
    a, b = axes(n)                              # a level, b up across the face
    up = np.array([0, 0, 1.0]) - n * n[2]; up /= np.linalg.norm(up)
    side = np.cross(n, up)
    for nm, colour, s, lift, t in (("dia", DIAMOND, 0.72, 0.0, 0.007), ("heart", HEART, 0.34, 0.004, 0.006)):
        p0 = c + n * (r * 0.9 + lift)
        pts = []
        for dx, dz in ((0, 1), (1, 0), (0, -1), (-1, 0)):
            q = p0 + side * dx * r * s * 0.75 + up * dz * r * s
            pts += [q - n * t / 2, q + n * t / 2]
        k.add(name + nm, hull(pts), colour, "body", fuse=False, surface="shell")


def leaf_outline(L, W, n=12):
    t = np.linspace(0, 1, n)
    w = W * np.sin(np.pi * np.clip(t * 0.92 + 0.04, 0, 1)) ** 0.7 * (1 - 0.35 * t)
    return [(L * a, b) for a, b in zip(t, w)] + [(L * a, -b) for a, b in zip(t[::-1][1:-1], w[::-1][1:-1])]


def calyrex(src, out):
    k = Kit(src, coat="leaf")

    # --- the bulb: the great dark green head, a pale lavender cap with notches -------------------------------------
    k.add("bulb", ell(BC, BR), BULB, "head", surface="leaf")
    def cap_outline(th):
        return 1.0 - 0.22 * abs(math.sin(2.5 * th)) ** 2
    k.decal(BC + (0, 0, BR[2]), (0, 0, 1), (0.135, 0.12), CAP, up=(0, -1, 0), eps=0.003, rings=12, segs=80,
            outline=cap_outline, surface="leaf")

    # --- the face under the bulb's front: a small skull, a long muzzle forward and down ---------------------------
    k.add("skull", ell(H, HR), PALE, "head", surface="skin")
    def taper(V):
        V = V.copy()
        t = np.clip(-V[:, 1], 0, 1)
        V[:, 0] *= 1 - 0.45 * t; V[:, 2] *= 1 - 0.4 * t
        return V
    k.add("muzzle", ell((0, -0.168, 0.578), (0.04, 0.06, 0.035), rot=(-12, 0, 0), deform=taper), PALE, "head",
          surface="skin")
    k.add("neck", tube([(0, -0.03, 0.52), (0, -0.06, 0.575)], [0.034, 0.034]), PALE, "head", surface="skin")

    # --- the body, small and pale --------------------------------------------------------------------------------
    k.add("chest", ell((0, -0.01, 0.48), (0.064, 0.058, 0.08)), PALE, "body", surface="skin")
    k.add("hips", ell((0, 0.0, 0.415), (0.046, 0.044, 0.045)), PALE, "body", surface="skin")
    # short arms forward and down, small round hands in front of the belly
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add("arm" + side, tube([np.array((0.05, -0.0, 0.565)) * m, np.array((0.085, -0.035, 0.52)) * m,
                                  np.array((0.08, -0.08, 0.485)) * m], [0.021, 0.02, 0.019]), PALE, "arm" + side,
              surface="skin")
        k.add("hand" + side, ell(np.array((0.078, -0.093, 0.478)) * m, (0.022, 0.022, 0.02)), PALE, "arm" + side,
              surface="skin")
    # two long thin legs ending in points
    k.add("leg", tube([(0.026, 0.0, 0.43), (0.03, -0.004, 0.3), (0.03, -0.008, 0.162)], [0.023, 0.02, 0.018]),
          PALE, "leg{s}", mirror=True, surface="skin")

    # --- the four sepals: a pair up the bulb's front from the brow, a pair out from under its sides ---------------
    k.add("sepal", outward(blade([(0.04, -0.13, 0.64), (0.056, -0.2, 0.72), (0.066, -0.232, 0.82),
                                  (0.064, -0.212, 0.92), (0.052, -0.155, 0.99)],
                                 [0.017, 0.021, 0.021, 0.016, 0.004], (0, 1, 0), 0.6)),
          PALE, "head", fuse=False, mirror=True, surface="leaf")
    k.add("sidesepal", outward(blade([(0.18, 0.0, 0.66), (0.25, -0.02, 0.69), (0.3, -0.03, 0.77),
                                      (0.322, -0.032, 0.87)], [0.02, 0.025, 0.019, 0.004], (0, 1, 0), 0.55)),
          PALE, "head", fuse=False, mirror=True, surface="leaf")

    # --- the skirt: pointed leaves round the waist, out and down, dark green with lavender middles ----------------
    for i, az in enumerate((32, 72, 115, 158, -32, -72, -115, -158)):
        a = math.radians(az)
        o = np.array([math.sin(a), -math.cos(a), 0.0])
        d = unit(*(o * 0.88 + np.array([0, 0, -0.48])))
        tang = np.cross((0, 0, 1.0), o)
        root = np.array([0, -0.005, 0.475]) + o * 0.05
        ez = tang
        k.add(f"leaf{i}", slab(leaf_outline(0.13, 0.034), root, d, ez, 0.012), LEAF, "body", fuse=False,
              surface="leaf")
        k.add(f"leafmid{i}", slab(leaf_outline(0.095, 0.02), root + d * 0.016, d, ez, 0.018), LEAFMID, "body",
              fuse=False, surface="leaf")

    # --- the collar: a ring of beads round the neck, its end rolled up behind the left cheek ----------------------
    n = 0
    for j in range(10):
        a = 2 * math.pi * j / 10 + math.pi / 10
        o = np.array([math.sin(a), -math.cos(a), 0.0])
        c = NECK_C + o * RING_R + np.array([0, 0, 0.008 * math.cos(a)])
        bead(k, f"bead{n}", c, 0.03, o + np.array([0, 0, 0.25])); n += 1
    for c, r in (((-0.098, 0.025, 0.592), 0.024), ((-0.09, 0.04, 0.628), 0.018)):
        bead(k, f"bead{n}", np.array(c), r, (-1, -0.3, 0.3)); n += 1

    k.build(voxel=0.0055, smooth=4, band=0.005)

    # --- the eyes: narrow, blue, under a level pale lid --------------------------------------------------------
    de = unit(0.6, -0.76, 0.06)
    E = lv.on_ell(H, HR, de)
    cut = lv.brow_cut(0.18, 0.22)
    look = lv.angry_eye(cut, iris=IRIS, rim=0.82, iris_c=(-0.15, -0.12), iris_r=0.55, pupil_r=0.24)
    m = np.array([-1, 1, 1.0])
    LID, INK = srgb(PALE), lv.INK

    def eyes(state):
        lk = eye_look(state, look, LID, INK, line=0.17)
        k.decal(E, de, (0.026, 0.02), lk, eps=0.002, rings=20, segs=56, outline=cut)
        k.decal(E * m, de * m, (0.026, 0.02), lambda u, v: lk(-u, v), eps=0.002, rings=20, segs=56,
                outline=lambda th: cut(math.pi - th))

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    calyrex(*args())
