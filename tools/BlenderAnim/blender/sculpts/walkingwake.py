# Walking Wake sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/walkingwake.py -- <the export's Walking Wake.glb> <out.glb>
#
# Suicune's ancient kin, built anew on raikou.py's eyes (through sibling("raikou")). From the references (which
# overrule the task's "two legs": it stands on four, the forelegs thinner and bent under the mane): a teal dinosaur
# leaning forward. The design's largest mass is the great purple mane, a fused billowing mass over the shoulders, the
# neck and the chest, trailing a lobe up behind; a row of flame-like spikes, pink at the root and orange at the tip,
# stands along its top, swept back. The head is carried low and forward out of the mane's front: teal above, a white
# jaw with two fangs and a white beard below, a yellow eye under a heavy brow with red round it. From the forehead a
# tall dark teal crest rises in a closed loop, a branch running back from it. Behind the mane a lean teal trunk, great
# haunches fused into it, short bent shins, long feet of three white claws; the forelegs bent, three white claws each.
# White diamonds on the haunches and shins, a white stripe down the back of each hind leg. The tail is teal at its root
# with white teeth along its top, then a long white whip curling up and out to the side.
# Reference ratios (3D picture, crest to toe 1.0): the mane 0.67 tall and 0.8 long (the largest mass), the head
# without crest 0.21 tall (built 0.23), the crest 0.37 over the head, the foreleg 0.07 across, the thigh 0.17 across,
# length 1.36 of the height (built about 1.1: the tail curled up and out).
# Bones: body (0, 0, 0.44); head (0, -0.2, 0.54); legs (+-0.1, -0.16, 0.4); hind legs (+-0.1, 0.1, 0.4);
# tail (0, 0.18, 0.46).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, outward, srgb, inside, smoothstep, tri, unit, polygon, lumpy, args,
                       sibling)

r = sibling("raikou")

TEAL = "#3FB0C0"
DEEP = "#2C7E8E"
WHITE = "#EEF2F6"
MANE = "#9E58B0"
MANE_DARK = "#7E4294"
CREST = "#2F8896"
CREST_HI = "#62C6D2"
SPIKE_ROOT = "#DC3E6C"
SPIKE_TIP = "#F49A3C"
RED = "#D8343C"
EYE, IRIS, PUPIL = "#F8DC48", "#F2A622", "#1A1012"

H = np.array([0.0, -0.37, 0.6])          # the head, carried low and forward out of the mane
HR = np.array([0.088, 0.105, 0.08])
EYE_DIR = unit(0.6, -0.74, 0.3)
EYE_R = 0.028


def kite(base, d, length, width, flat=0.55, up=(0, 0, 1.0), mid=0.45):
    """A flat-faced solid spike or claw from base along d: a kite at its root tapering to a point."""
    b = np.array(base, float)
    d = unit(*d)
    a = np.cross(d, up)
    if np.linalg.norm(a) < 1e-3:
        a = np.cross(d, (1.0, 0, 0))
    a /= np.linalg.norm(a)
    n = np.cross(a, d)
    w = width
    m = b + d * length * mid
    pts = [b + a * w, b - a * w, b + n * w * flat, b - n * w * flat,
           m + a * w * 0.62, m - a * w * 0.62, m + n * w * flat * 0.6, m - n * w * flat * 0.6, b + d * length]
    return hull(pts)


def add_head(k):
    k.add("head", ell(H, HR), TEAL, "head")
    k.add("brow", ell(H + (0, -0.02, 0.035), (0.08, 0.08, 0.05)), TEAL, "head")
    k.add("snout", ell(H + (0, -0.1, -0.012), (0.058, 0.075, 0.05), rot=(10, 0, 0)), TEAL, "head")
    k.add("jaw", ell(H + (0, -0.085, -0.06), (0.06, 0.075, 0.035), rot=(-12, 0, 0)), WHITE, "head")
    k.add("beard", ell(H + (0, -0.03, -0.085), (0.05, 0.05, 0.05)), WHITE, "head")
    k.add("neck", tube([(0, -0.18, 0.5), (0, -0.25, 0.56), (0, -0.31, 0.59)], [0.1, 0.09, 0.08]), TEAL, "head")
    # White below a line from the nose's root back under the eye
    near = lambda P: (np.linalg.norm((P - H) / (0.16, 0.2, 0.14), axis=1) < 1) & (P[:, 1] < H[1] + 0.06)
    k.paint(lambda P: inside(P[:, 2] - (H[2] - 0.04 + 0.18 * (P[:, 1] - (H[1] - 0.12))), 0.0025) * near(P), WHITE)
    # A red smear round the back of each eye (in every state)
    p, n = r.on_ell(H, HR, EYE_DIR)
    k.decal(p + np.array((0.004, 0.016, 0.0)), n, (0.042, 0.03), RED, eps=0.0025, rings=12, segs=44, mirror=True)
    # Fangs and the beard's points: white, hanging from the jaw
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        k.add(f"fang{sd}", kite(H + (0.03 * s, -0.16, -0.035), (0.1 * s, -0.25, -1), 0.05, 0.014, up=(0, 1.0, 0)),
              WHITE, "head", fuse=False, surface="shell")
        k.add(f"beardpt{sd}", kite(H + (0.035 * s, -0.04, -0.11), (0.35 * s, -0.1, -1), 0.06, 0.022, up=(0, 1.0, 0)),
              WHITE, "head", fuse=False, surface="shell")
    k.add("beardpt", kite(H + (0, -0.07, -0.11), (0, -0.25, -1), 0.065, 0.024, up=(0, 1.0, 0)), WHITE, "head",
          fuse=False, surface="shell")
    # The mouth: a dark line along the jaw's top edge
    def mouth(P):
        q = P - H
        line = np.abs(q[:, 2] - (-0.04 + 0.18 * (q[:, 1] + 0.12))) < 0.006
        return line * (q[:, 1] < -0.06) * (np.abs(q[:, 0]) < 0.06) * near(P)
    k.paint(mouth, "#2A1C24")


def add_crest(k):
    """The tall crest: a closed loop rising from the forehead, its plane turned a little out to +X, a branch back."""
    # the loop's back toward +X so its opening faces the game's front three-quarter camera
    turn = math.radians(-34)
    loop = [(0, -0.07, 0.06), (0, -0.1, 0.16), (0, -0.09, 0.27), (0, -0.05, 0.36), (0, 0.01, 0.37), (0, 0.04, 0.3),
            (0, 0.035, 0.2), (0, 0.02, 0.1), (0, 0.0, 0.07)]
    pts = []
    for q in loop:
        x = -q[1] * math.sin(turn)
        pts.append(H + np.array((x, q[1] * math.cos(turn), q[2])))
    k.add("crest", outward(tube(pts, [0.026, 0.032, 0.034, 0.032, 0.03, 0.03, 0.03, 0.028, 0.028])), CREST, "head",
          fuse=False, surface="shell")
    branch = [(0, 0.02, 0.2), (0, 0.1, 0.24), (0, 0.17, 0.3)]
    bp = [H + np.array((-q[1] * math.sin(turn), q[1] * math.cos(turn), q[2])) for q in branch]
    k.add("crestbranch", outward(tube(bp, [0.024, 0.024, 0.02])), CREST, "head", fuse=False, surface="shell")
    k.add("crestknob", outward(ell(bp[-1] + (0, 0.012, 0.012), (0.028, 0.034, 0.026), sub=3)), CREST_HI, "head",
          fuse=False, surface="shell")
    # a paler crown knob where the loop meets the head
    k.add("crestroot", outward(ell(H + (0, -0.03, 0.075), (0.05, 0.07, 0.032), sub=3)), CREST, "head", fuse=False,
          surface="shell")


def add_mane(k):
    """The great purple mane: billowing over the shoulders, neck and chest, a lobe trailing up behind."""
    blobs = [((0, -0.14, 0.6), (0.2, 0.2, 0.22), "body"), ((0, -0.04, 0.66), (0.19, 0.18, 0.18), "body"),
             ((0, 0.08, 0.66), (0.15, 0.14, 0.14), "body"), ((0, -0.24, 0.5), (0.15, 0.1, 0.17), "head"),
             ((0.13, -0.16, 0.46), (0.1, 0.13, 0.15), "body"), ((-0.13, -0.16, 0.46), (0.1, 0.13, 0.15), "body"),
             ((0, -0.2, 0.74), (0.13, 0.12, 0.1), "head"), ((0, 0.17, 0.73), (0.08, 0.1, 0.1), "body")]
    for i, (c, rr, bone) in enumerate(blobs):
        k.add(f"mane{i}", ell(c, rr, deform=lumpy(4, 3, 0.03)), MANE, bone)
    # Ragged points along the mane's hem, hanging over the chest and shoulders
    for j, (x, y, z, d) in enumerate(((0.0, -0.3, 0.36, (0, -0.3, -1)), (0.1, -0.27, 0.36, (0.3, -0.3, -1)),
                                      (0.19, -0.18, 0.37, (0.6, 0, -1)), (0.2, -0.06, 0.42, (0.7, 0.3, -1)))):
        for s in ((1, -1) if x else (1,)):
            sd = "R" if s > 0 else "L"
            k.add(f"manehem{j}{sd}", kite((x * s, y, z), (d[0] * s, d[1], d[2]), 0.09, 0.05, flat=0.7), MANE_DARK,
                  "body", fuse=False, surface="fur")
    # The lobe trailing up behind, swept back and a little out to +X
    k.add("manelobe", kite((0.03, 0.18, 0.72), (0.35, 0.8, 0.6), 0.2, 0.06, flat=0.65), MANE, "body", fuse=False,
          surface="fur")


def add_spikes(k):
    """Flame-like spikes along the mane's top, swept back: pink at the root, orange at the tip."""
    rows = [((0.0, -0.22, 0.82), (0, 0.55, 1), 0.17), ((0.07, -0.12, 0.82), (0.35, 0.7, 0.8), 0.17),
            ((-0.07, -0.12, 0.82), (-0.35, 0.7, 0.8), 0.17), ((0.0, -0.03, 0.82), (0, 0.8, 0.85), 0.16),
            ((0.1, 0.04, 0.77), (0.45, 0.8, 0.6), 0.14), ((-0.1, 0.04, 0.77), (-0.45, 0.8, 0.6), 0.14),
            ((0.0, 0.1, 0.77), (0.1, 0.9, 0.7), 0.13)]
    for i, (b, d, ln) in enumerate(rows):
        b = np.array(b); dd = unit(*d)
        k.add(f"spike{i}", kite(b, dd, ln * 0.55, 0.036, flat=0.6, up=(0, 1.0, 0)), SPIKE_ROOT, "body", fuse=False,
              surface="shell")
        k.add(f"spiketip{i}", kite(b + dd * ln * 0.42, dd, ln * 0.58, 0.026, flat=0.6, up=(0, 1.0, 0)), SPIKE_TIP,
              "body", fuse=False, surface="shell")


def add_trunk(k):
    """A lean teal trunk behind the mane, leaning forward, a white underside."""
    k.add("chest", ell((0, -0.1, 0.44), (0.13, 0.13, 0.13)), TEAL, "body")
    k.add("middle", ell((0, 0.02, 0.42), (0.115, 0.15, 0.115)), TEAL, "body")
    k.add("hips", ell((0, 0.13, 0.43), (0.12, 0.11, 0.115)), TEAL, "body")
    k.paint(lambda P: inside(P[:, 2] - 0.34, 0.003) * inside(np.abs(P[:, 0]) - 0.07, 0.004) * (P[:, 2] > 0.22)
            * (P[:, 1] > -0.05) * (P[:, 1] < 0.26), WHITE, on=["middle", "hips", "chest"])


def add_legs(k):
    # Forelegs: from under the mane, the elbow back, the forearm forward and down, three white claws
    k.add("shoulder", ell((0.1, -0.15, 0.36), (0.065, 0.075, 0.1)), TEAL, "leg{s}", mirror=True)
    k.add("fore", tube([(0.1, -0.16, 0.38), (0.12, -0.12, 0.25), (0.125, -0.21, 0.12), (0.125, -0.25, 0.05)],
                       [0.05, 0.042, 0.034, 0.032]), TEAL, "leg{s}", mirror=True)
    k.add("forefoot", ell((0.125, -0.27, 0.03), (0.04, 0.055, 0.026)), TEAL, "leg{s}", mirror=True)
    # Hind legs: great haunches fused into the hips down to the knee, short thick shins, long feet
    k.add("thigh", ell((0.115, 0.1, 0.33), (0.09, 0.13, 0.16), rot=(-25, 0, 0)), TEAL, "hind{s}", mirror=True)
    k.add("shin", tube([(0.125, 0.04, 0.22), (0.13, 0.13, 0.1), (0.13, 0.12, 0.05)], [0.058, 0.045, 0.04]), TEAL,
          "hind{s}", mirror=True)
    k.add("hindfoot", ell((0.13, 0.06, 0.03), (0.048, 0.085, 0.028)), TEAL, "hind{s}", mirror=True)
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        for j, dx in enumerate((-0.026, 0.0, 0.026)):
            k.add(f"foreclaw{sd}{j}", kite((s * (0.125 + dx), -0.31, 0.022), (s * dx * 4, -1, -0.3), 0.055, 0.018,
                                           up=(0, 0, 1.0)), WHITE, f"leg{sd}", fuse=False, surface="shell")
            k.add(f"hindclaw{sd}{j}", kite((s * (0.13 + dx * 1.2), -0.01, 0.022), (s * dx * 5, -1, -0.3), 0.065, 0.02,
                                           up=(0, 0, 1.0)), WHITE, f"hind{sd}", fuse=False, surface="shell")
        k.add(f"spur{sd}", kite((s * 0.13, 0.14, 0.03), (0, 1, -0.2), 0.05, 0.018), WHITE, f"hind{sd}", fuse=False,
              surface="shell")
    # A white stripe down the back of each hind leg and the forearm's back
    def stripe(P):
        ax = np.abs(P[:, 0])
        hind = (ax > 0.07) & (P[:, 1] > 0.12) & (P[:, 2] < 0.3) & (P[:, 2] > 0.07)
        back = P[:, 1] - (0.175 + 0.3 * (P[:, 2] - 0.2))
        return inside(-back, 0.003) * hind * inside(np.abs(ax - 0.13) - 0.03, 0.004)
    k.paint(stripe, WHITE, on=["thigh", "shin"])
    # White diamonds on the haunches and shins
    D = polygon([(0, -1), (1, 0), (0, 1), (-1, 0)])
    for c, n, s in (((0.2, 0.08, 0.34), (1, 0, 0.15), 0.05), ((0.2, 0.15, 0.27), (1, 0.2, 0), 0.035),
                    ((0.19, 0.02, 0.4), (1, -0.3, 0.3), 0.03), ((0.18, 0.08, 0.13), (1, 0.3, 0), 0.028)):
        k.decal(c, n, (s * 0.7, s), WHITE, eps=0.003, outline=D, rings=8, segs=40, mirror=True)


def add_tail(k):
    """Teal at the root with white teeth along its top, then a long white whip curling up and out to +X."""
    root = [(0, 0.2, 0.45), (0.02, 0.3, 0.47), (0.05, 0.38, 0.5)]
    k.add("tail", tube(root, [0.08, 0.06, 0.045]), TEAL, "tail")
    whip = [(0.05, 0.37, 0.5), (0.1, 0.46, 0.5), (0.16, 0.53, 0.45), (0.23, 0.56, 0.38), (0.29, 0.53, 0.33),
            (0.3, 0.47, 0.35)]
    k.add("whip", outward(tube(whip, [0.04, 0.034, 0.03, 0.026, 0.022, 0.016])), WHITE, "tail", fuse=False,
          surface="scales")
    teeth = [(np.array(root[0]) + (0, 0.04, 0.07), (0, 0.6, 1)), (np.array(root[1]) + (0, 0.0, 0.055), (0.1, 0.7, 1)),
             (np.array(root[2]) + (0, 0.0, 0.04), (0.2, 0.7, 1)), (np.array(whip[1]) + (0, 0, 0.03), (0.3, 0.6, 1)),
             (np.array(whip[2]) + (0, 0.01, 0.026), (0.3, 0.6, 1)), (np.array(whip[3]) + (0.01, 0.012, 0.02), (0.5, 0.6, 0.6))]
    for j, (b, d) in enumerate(teeth):
        k.add(f"tooth{j}", kite(b, d, 0.05, 0.02, flat=0.6, up=(0, 1.0, 0)), WHITE, "tail", fuse=False,
              surface="shell")


SHUT_LINE = "#163038"


def eye_top(u):
    return 0.22 + 0.5 * u


def eyes(k, state):
    p, n = r.on_ell(H, HR, EYE_DIR)
    r.lay_eyes(k, state, p, n, EYE_R, r.eye_open(EYE, IRIS, PUPIL, iris_r=(0.62, 0.8)), TEAL, SHUT_LINE, eye_top)


if __name__ == "__main__":
    src, dst = args()
    k = Kit(src, coat="scales")
    add_trunk(k)
    add_legs(k)
    add_head(k)
    add_mane(k)
    add_tail(k)
    add_crest(k)
    add_spikes(k)
    k.build(voxel=0.008, band=0.004)
    k.eye_states(lambda s: eyes(k, s))
    k.export(dst)
