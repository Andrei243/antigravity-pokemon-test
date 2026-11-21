# Basculegion (the male) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py),
# grown from Basculin's script (basculin.py): the same way of making a fish's dome, its fins (white slabs, a grey root,
# the midline ones turned 40 degrees to +X), its eyes brought forward onto the cheeks.
#   blender --background --factory-startup --python sculpts/basculegion.py -- <the export's Basculegion.glb> <out.glb>
#
# The design, from the references: a long dark green fish with a blackish line along its back; a big jaw held wide
# open, dark brown inside, a row of white teeth along the upper jaw and a few on the lower; tufts of pink spikes at the
# brow and under the chin; a pink eye ringed white; pink lines like lightning along its flank from the eye back, a pink
# line under the dark back, pink spots over the hind flank; white fins: a jagged crest on the back, a long blade down
# and back from each side under the head, a blade under the belly. Its hind half breaks up into a great pink ghostly
# flame, the souls it carries: billowing lumps rising behind, darker pink swirls, a few wisps trailing off the end.
# (The task's text has the wisps white: the pictures have them pink, and they are the tail itself.)
# The joints: body (0, 0.04, 0.295), head (0, -0.12, 0.235), tail (0, 0.3, 0.295).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, hull, boxy, srgb, inside, unit, eye_look, polygon, tri, args

GREEN, BACK, PINK, PINKDARK, FIN, FINROOT = "#4F7A5C", "#22302C", "#EE3D7C", "#C21E5E", "#F0F2EE", "#AEB6B4"
WHITE, INK, MOUTH, IRIS = "#F7F7F3", "#16161C", "#5A2A1E", "#E8407C"

DOME_C, DOME_R = np.array([0.0, -0.1, 0.31]), np.array([0.108, 0.17, 0.13])
REAR_C, REAR_R = np.array([0.0, 0.08, 0.305]), np.array([0.1, 0.17, 0.125])


def top_edge(y):
    """The height of the dark back's lower edge along the body."""
    y = np.asarray(y, float)
    return np.where(y < -0.17, 0.405 + (-0.17 - y) * 1.2, 0.405 - (y + 0.17) * 0.08)


def body(k):
    k.add("dome", ell(DOME_C, DOME_R, sub=5), GREEN, "body")
    k.add("rear", ell(REAR_C, REAR_R, sub=5), GREEN, "body")
    k.add("waist", ell((0, -0.01, 0.31), (0.103, 0.12, 0.128), sub=5), GREEN, "body")
    # the great open jaw: an upper jaw under the dome's front, a lower jaw dropped, the mouth dark between
    k.add("upper", ell((0, -0.19, 0.275), (0.088, 0.1, 0.045), deform=boxy(2.6)), GREEN, "head")
    k.add("jaw", ell((0, -0.16, 0.175), (0.085, 0.12, 0.042), deform=boxy(2.6)), GREEN, "head")
    k.add("chin", ell((0, -0.06, 0.2), (0.088, 0.1, 0.06)), GREEN, "head")
    k.add("mouth", ell((0, -0.17, 0.225), (0.07, 0.09, 0.05)), MOUTH, "head")
    # the dark back, and the pink line under it
    k.paint(lambda P: inside((top_edge(P[:, 1]) - 0.011) - P[:, 2], 0.0025) * inside(P[:, 2] - top_edge(P[:, 1]), 0.0025)
            * (P[:, 1] < 0.18), PINK, on=["dome", "rear", "waist"])
    k.paint(lambda P: inside(top_edge(P[:, 1]) - P[:, 2], 0.0025), BACK, on=["dome", "rear", "waist", "upper"])
    # the lightning along each flank: a zigzag from behind the eye back to the flame, with two forks down
    def bolt(P):
        y, z = P[:, 1], P[:, 2]
        line = 0.335 - (y + 0.12) * 0.25 + 0.022 * tri((y + 0.12) / 0.07)
        m = inside(np.abs(z - line) - 0.007, 0.0022) * inside(y - 0.2, 0.002) * inside(-0.13 - y, -0.002)
        for y0 in (-0.06, 0.06):
            d = (y - y0) * 0.55 + (line - z) * 0.85              # a fork down and back from the line
            t_ = z - line
            m = np.maximum(m, inside(np.abs(d - 0.0) - 0.007, 0.0022) * inside(t_, 0.002) * inside(-0.07 - t_, 0.002))
        return m * (np.abs(P[:, 0]) > 0.05)
    k.paint(bolt, PINK, on=["dome", "rear", "waist"])


def flame(k):
    """The hind half breaking up into a pink ghostly flame: billowing lumps rising behind, swirls, wisps."""
    puffs = [(0.0, 0.2, 0.31, 0.1), (0.0, 0.28, 0.33, 0.1), (0.0, 0.36, 0.37, 0.088), (0.0, 0.29, 0.43, 0.07),
             (0.03, 0.43, 0.42, 0.07), (0.0, 0.42, 0.3, 0.072), (0.0, 0.2, 0.22, 0.07), (0.0, 0.34, 0.23, 0.06),
             (0.0, 0.5, 0.45, 0.05), (0.0, 0.51, 0.36, 0.05), (0.0, 0.47, 0.25, 0.045), (0.0, 0.22, 0.41, 0.06)]
    for i, (x, y, z, r) in enumerate(puffs):
        k.add(f"flame{i}_", ell((x, y, z), (r * 0.95, r, r)), PINK, "tail")
    # darker pink swirls on its sides: rings round the lumps' middles
    def swirls(P):
        m = np.zeros(len(P))
        for (y0, z0, r) in ((0.28, 0.33, 0.045), (0.37, 0.39, 0.04), (0.43, 0.29, 0.032), (0.22, 0.24, 0.03)):
            d = np.hypot(P[:, 1] - y0, P[:, 2] - z0)
            m = np.maximum(m, inside(np.abs(d - r) - 0.006, 0.002))
        return m * (np.abs(P[:, 0]) > 0.04)
    k.paint(swirls, PINKDARK, on=["flame"])
    # spots over the hind flank, on the green before the flame
    for y, z, r in ((0.04, 0.27, 0.014), (0.1, 0.25, 0.012), (0.08, 0.32, 0.011), (-0.02, 0.24, 0.011),
                    (0.13, 0.31, 0.012)):
        dy = (y - REAR_C[1]) / REAR_R[1]; dz = (z - REAR_C[2]) / REAR_R[2]
        dx = math.sqrt(max(1 - dy * dy - dz * dz, 0.05))
        for s in (1, -1):
            d = np.array([dx * s, dy, dz])
            p = REAR_C + d * REAR_R
            n = d / REAR_R; n /= np.linalg.norm(n)
            k.decal(p, n, (r, r), PINK, eps=0.003, rings=8, segs=32)
    # wisps trailing off the flame's end, up and back
    for i, (c, r) in enumerate((((0.0, 0.57, 0.47), 0.03), ((0.0, 0.615, 0.49), 0.02), ((0.0, 0.645, 0.47), 0.014))):
        k.add(f"wisp{i}", ell(c, (r, r, r), sub=3), PINK, "tail", fuse=False)


def spike(base, tip, r, n=5, flat=0.7):
    from sculptkit import axes
    base, tip = np.array(base, float), np.array(tip, float)
    a, b = axes(tip - base)
    pts = [base + r * (math.cos(t) * a + flat * math.sin(t) * b) for t in np.linspace(0, 2 * math.pi, n, endpoint=False)]
    return hull(pts + [tip])


def teeth_and_barbs(k):
    # white teeth along the upper jaw's front and sides, a few up from the lower jaw
    for i, (x, y) in enumerate(((0.0, -0.285), (0.035, -0.275), (0.062, -0.25), (0.078, -0.21), (0.085, -0.17))):
        for s in ((1,) if x == 0 else (1, -1)):
            b = np.array([x * s, y + 0.01, 0.248])
            k.add(f"tooth{i}{'R' if s > 0 else 'L'}", spike(b, b + (0, -0.004, -0.04), 0.013), WHITE, "head",
                  fuse=False, surface="shell")
    for i, (x, y) in enumerate(((0.05, -0.25), (0.075, -0.2))):
        for s in (1, -1):
            b = np.array([x * s, y + 0.012, 0.2])
            k.add(f"lower{i}{'R' if s > 0 else 'L'}", spike(b, b + (0, -0.004, 0.03), 0.011), WHITE, "head",
                  fuse=False, surface="shell")
    # pink spikes: a tuft at each brow, up and back, and a tuft hanging under the chin
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        for j, (b, t_) in enumerate((((0.06, -0.19, 0.39), (0.1, -0.23, 0.47)), ((0.075, -0.15, 0.39), (0.13, -0.12, 0.45)))):
            k.add(f"brow{side}{j}", spike(np.array(b) * [s, 1, 1], np.array(t_) * [s, 1, 1], 0.017), PINK, "head",
                  fuse=False, surface="scales")
        for j, (b, t_) in enumerate((((0.05, -0.12, 0.15), (0.08, -0.15, 0.09)), ((0.03, -0.07, 0.15), (0.045, -0.03, 0.09)))):
            k.add(f"barb{side}{j}", spike(np.array(b) * [s, 1, 1], np.array(t_) * [s, 1, 1], 0.015), PINK, "head",
                  fuse=False, surface="scales")


def fin(k, name, outline, root_outline, origin, ex, ez, bone, thick=0.024, scale=1.0):
    outline = [(a * scale, b * scale) for a, b in outline]
    root_outline = [(a * scale, b * scale) for a, b in root_outline]
    k.add(name, slab(outline, origin, ex, ez, thick), FIN, bone, fuse=False)
    k.add(name + "root", slab(root_outline, origin, ex, ez, thick * 1.35), FINROOT, bone, fuse=False)


# a long blade down and back from under the head, its face out and forward
PEC = [(0.0, 0.035), (0.07, 0.035), (0.22, -0.02), (0.11, -0.045), (0.0, -0.04)]
PEC_ROOT = [(0.0, 0.025), (0.04, 0.02), (0.045, -0.026), (0.0, -0.028)]
CREST = [(-0.07, -0.02), (-0.05, 0.03), (-0.02, 0.03), (0.0, 0.1), (0.02, 0.045), (0.045, 0.08), (0.05, 0.035),
         (0.075, 0.05), (0.07, -0.02)]
CREST_ROOT = [(-0.065, -0.02), (-0.055, 0.015), (0.065, 0.015), (0.065, -0.02)]
BELLY = [(-0.04, 0.015), (0.08, -0.08), (0.05, -0.03), (0.045, 0.015)]
BELLY_ROOT = [(-0.035, 0.015), (-0.02, -0.006), (0.04, -0.006), (0.04, 0.015)]


def fins(k):
    for s in (1, -1):
        o = np.array([0.075 * s, -0.04, 0.215])
        d = unit(0.45 * s, 0.6, -0.65)
        fin(k, f"pec{'R' if s > 0 else 'L'}", PEC, PEC_ROOT, o, d, unit(0.2 * s, -0.75, 0.4), "body")
    yaw = math.radians(40)
    ex = (math.sin(yaw), math.cos(yaw), 0.0)
    fin(k, "crest", CREST, CREST_ROOT, np.array([0.0, 0.0, 0.42]), ex, (0, 0, 1), "body", scale=1.35)
    fin(k, "belly", BELLY, BELLY_ROOT, np.array([0.0, 0.08, 0.2]), ex, (0, 0, 1), "body", scale=1.3)


# --- the eyes: white, a large pink iris, a small dark pupil; brought forward onto the cheeks ---------------------------
EYE_Y, EYE_Z, EYE_HALF = -0.19, 0.345, (0.03, 0.03)
EYE_N = unit(0.7, -0.66, 0.22)
CW, CK, CI, CG = srgb(WHITE), srgb(INK), srgb(IRIS), srgb(GREEN)


def eye_point(s):
    dy = (EYE_Y - DOME_C[1]) / DOME_R[1]; dz = (EYE_Z - DOME_C[2]) / DOME_R[2]
    x = DOME_R[0] * math.sqrt(max(1 - dy * dy - dz * dz, 0.02))
    return np.array([x * s, EYE_Y, EYE_Z])


def eye_open(u, v):
    c = np.tile(CW, (len(u), 1))
    c[((u + 0.05) / 0.62) ** 2 + (v / 0.62) ** 2 < 1] = CI
    c[((u + 0.05) / 0.24) ** 2 + (v / 0.3) ** 2 < 1] = CK
    c[((u + 0.25) / 0.12) ** 2 + ((v - 0.2) / 0.12) ** 2 < 1] = CW
    c[u * u + v * v > 0.86 ** 2] = CK
    return c


def eyes(k, state):
    for s in (1, -1):
        n = EYE_N * np.array([s, 1, 1])
        look = eye_look(state, eye_open, CG, CK, line=0.16)
        k.decal(eye_point(s), n, EYE_HALF, look if s > 0 else (lambda u, v, f=look: f(-u, v)), eps=0.004,
                rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="scales")
    body(k)
    flame(k)
    teeth_and_barbs(k)
    fins(k)
    k.build(voxel=0.005, smooth=4, band=0.004)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
