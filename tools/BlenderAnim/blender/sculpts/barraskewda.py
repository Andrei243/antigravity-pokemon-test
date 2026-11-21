# Barraskewda sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/barraskewda.py -- <the export's Barraskewda.glb> <out.glb>
#
# Arrokuda grown (arrokuda.py: its round tube of a body, its eye of a white with a black rim and a big black pupil,
# its fangs as small white hulls, its fins as plates turned out to +X). From the references: a long brown barracuda
# like a spear, a third of it head: a long pointed snout over a long lower jaw, both brown, a dark mouth line between
# them with white teeth standing up from the jaw and hanging from the snout. A small eye under a brown brow with a
# dark patch behind it. A big brown spike sweeping back off the crown and two smaller ones down the back. A pale
# yellow panel down each flank under a jagged brown edge, a white fishbone on it (a spine and ribs slanting back).
# Fins of flame orange with jagged edges: a pectoral pair hanging down and back, a pelvic pair, an anal fin, a
# dorsal near the tail, and a great forked tail like a propeller behind a white jagged collar. The joints: body
# (0, 0, 0.155), head (0, -0.18, 0.155), dorsal0..3 (0, -0.04..0.2, 0.215), tail (0, 0.29, 0.155), finR/L
# (+-0.05, -0.08, 0.125).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, slab, srgb, inside, unit, tri, args, sibling

ar = sibling("arrokuda")

BROWN, PANEL, BELLY, ORANGE, WHITE, INK, MOUTH, SHADE = ("#8C7462", "#EEE6A2", "#E8DFA8", "#F25A24", "#F6F6F2",
                                                         "#18161A", "#3A2C26", "#5E4D42")
Z0 = 0.155


def body(k):
    tube = ar.body_tube
    # the trunk: long, deepest behind the head, the tail's root dipping a little
    k.add("trunk", tube([(0, -0.17, Z0 + 0.012), (0, -0.06, Z0 + 0.01), (0, 0.06, Z0 + 0.002), (0, 0.17, Z0 - 0.008)],
                        [0.064, 0.068, 0.06, 0.044], flat=0.8), BROWN, "body")
    k.add("stalk", tube([(0, 0.16, Z0 - 0.008), (0, 0.24, Z0 - 0.014), (0, 0.29, Z0 - 0.016)], [0.045, 0.028, 0.018],
                        flat=0.78), BROWN, "tail")
    # the head: a long cone to the snout's point, the lower jaw a second cone under it, as long
    k.add("head", tube([(0, -0.43, Z0 + 0.004), (0, -0.35, Z0 + 0.014), (0, -0.26, Z0 + 0.024), (0, -0.16, Z0 + 0.022)],
                       [0.006, 0.022, 0.044, 0.062], flat=0.78), BROWN, "head")
    k.add("jaw", tube([(0, -0.445, Z0 - 0.012), (0, -0.36, Z0 - 0.016), (0, -0.26, Z0 - 0.024), (0, -0.18, Z0 - 0.03)],
                      [0.006, 0.018, 0.03, 0.04], flat=0.82), BROWN, "head")
    # the brow over each eye
    k.add("brow", ell((0.026, -0.25, Z0 + 0.04), (0.02, 0.04, 0.012), rot=(0, -20, 0)), BROWN, "head", mirror=True)


def mouth_z(y):
    y = np.asarray(y, float)
    return Z0 - 0.006 + np.clip((y + 0.26) / 0.06, 0, 1) ** 2 * 0.02


def panel_edges(y):
    """The pale panel's top and bottom along the body, each jagged."""
    y = np.asarray(y, float)
    top = np.interp(y, [-0.18, -0.12, 0.1, 0.2, 0.24], [Z0 + 0.02, Z0 + 0.045, Z0 + 0.04, Z0 + 0.02, Z0])
    bot = np.interp(y, [-0.18, -0.1, 0.1, 0.2, 0.24], [Z0 - 0.0, Z0 - 0.04, Z0 - 0.036, Z0 - 0.02, Z0])
    zig = 0.007 * tri(y / 0.04)
    return top + zig, bot - zig


def markings(k):
    # the pale belly
    k.paint(lambda P: inside(P[:, 2] - (Z0 - 0.045), 0.002) * inside(P[:, 1] - 0.2, 0.01) * inside(-0.42 - P[:, 1], 0.01),
            BELLY, on=["trunk", "stalk", "jaw"])
    k.paint(lambda P: inside(P[:, 2] - (Z0 - 0.045), 0.002) * inside(P[:, 1] + 0.18, 0.004),
            BROWN, on=["jaw", "head"])

    # the yellow panel down each flank, jagged above and below
    def panel(P):
        y = P[:, 1]
        top, bot = panel_edges(y)
        lim = inside(-0.16 - y, 0.003) * inside(y - 0.235, 0.003)
        return inside(P[:, 2] - top, 0.0018) * inside(bot - P[:, 2], 0.0018) * lim
    k.paint(panel, PANEL, on=["trunk", "stalk"])

    # the fishbone: a spine along the panel's middle and ribs slanting back from it
    def bone(P):
        y, dz = P[:, 1], P[:, 2] - Z0
        m = inside(np.abs(dz) - 0.0055, 0.0014) * inside(-0.13 - y, 0.003) * inside(y - 0.2, 0.003)
        for y0, h in ((-0.085, 0.032), (-0.02, 0.033), (0.045, 0.03), (0.11, 0.025)):
            t = y - (y0 + 0.9 * np.abs(dz))
            m = np.maximum(m, inside(np.abs(t) - 0.0065, 0.0014) * inside(np.abs(dz) - h, 0.002))
        return m * inside(0.02 - np.abs(P[:, 0]), 0.002)
    k.paint(bone, WHITE)

    # the white jagged collar where the tail fans out
    def collar(P):
        y, dz = P[:, 1], P[:, 2] - Z0
        edge = 0.248 + 0.008 * tri(dz / 0.02)
        return inside(edge - y, 0.002) * inside(y - 0.29, 0.002)
    k.paint(collar, WHITE, on=["stalk"])

    # the mouth line, rising at its back end under the eye
    k.paint(lambda P: inside(np.abs(P[:, 2] - mouth_z(P[:, 1])) - 0.0032, 0.0016) * inside(P[:, 1] + 0.2, 0.002)
            * inside(-0.44 - P[:, 1], 0.002), MOUTH, on=["head", "jaw"])
    # the dark patch behind the eye, down toward the mouth's end
    def patch(P):
        y, z = P[:, 1], P[:, 2]
        r = np.sqrt(((y + 0.215) / 0.03) ** 2 + ((z - (Z0 + 0.02)) / 0.022) ** 2)
        return inside(r - 1, 0.04) * inside(0.03 - np.abs(P[:, 0]), 0.003)
    k.paint(patch, SHADE, on=["head", "jaw", "brow"])


def teeth(k):
    """White teeth: three standing up from the lower jaw and two hanging from the snout, a side each."""
    for i, y in enumerate((-0.4, -0.355, -0.305)):
        z = float(mouth_z(y)) - 0.004
        x = 0.012 + (y + 0.44) * 0.12
        ar.fang(k, f"lower{i}", (x, y, z), -0.014 - 0.002 * i, "head")
    for i, y in enumerate((-0.38, -0.33)):
        z = float(mouth_z(y)) + 0.005
        x = 0.013 + (y + 0.44) * 0.12
        ar.fang(k, f"upper{i}", (x, y, z), 0.014 + 0.002 * i, "head")


def spikes(k):
    """Brown spikes on the midline sweeping back: a big one off the crown and two smaller down the back."""
    for name, (y, z), length, rise, w, bone in (("crown", (-0.2, Z0 + 0.075), 0.11, 0.06, 0.03, "head"),
                                               ("back0", (-0.03, Z0 + 0.068), 0.07, 0.035, 0.022, "dorsal0"),
                                               ("back1", (0.08, Z0 + 0.06), 0.06, 0.028, 0.02, "dorsal2")):
        base = np.array([0.0, y, z - 0.02])
        tip = base + np.array([0.0, length, rise + 0.02])
        pts = [base + (w * 0.55, -0.03, 0), base + (-w * 0.55, -0.03, 0), base + (w * 0.5, 0.035, 0),
               base + (-w * 0.5, 0.035, 0), base + (0, 0.0, 0.03), tip, base + (0, 0.04, 0.03)]
        k.add(name, hull(pts), BROWN, bone, fuse=False, surface="shell")


def fan(hw, tips, notch=0.78):
    """A ragged fin's outline: a root hw high at a = 0, then tips (angle in degrees from the a axis, length) from the
    top one down, with a notch between each two at notch of their lengths; the reference's fins are scalloped so."""
    pts = [(0.0, hw)]
    for i, (ang, length) in enumerate(tips):
        a = math.radians(ang)
        pts.append((length * math.cos(a), length * math.sin(a)))
        if i + 1 < len(tips):
            b = math.radians((ang + tips[i + 1][0]) / 2)
            m = notch * (length + tips[i + 1][1]) / 2
            pts.append((m * math.cos(b), m * math.sin(b)))
    pts.append((0.0, -hw))
    return pts


PEC = fan(0.014, [(-20, 0.075), (-38, 0.085), (-56, 0.07)])
PELVIC = fan(0.012, [(-25, 0.06), (-45, 0.066), (-62, 0.052)])
ANAL = fan(0.014, [(-15, 0.06), (-35, 0.064), (-55, 0.05)])
DORSAL = fan(0.014, [(55, 0.05), (35, 0.062), (15, 0.055)])
TAIL_UP = fan(0.018, [(62, 0.11), (44, 0.15), (28, 0.12)], notch=0.8)
TAIL_DOWN = fan(0.018, [(-26, 0.1), (-44, 0.14), (-62, 0.1)], notch=0.8)


def fins(k):
    ex = ar.yaw_ex(45)
    fin = ar.fin
    fin(k, "pec", PEC, (0.045, -0.1, Z0 - 0.035), unit(0.5, 0.85, -0.05), unit(0.25, 0.1, 1), "fin{s}",
        color=ORANGE, thick=0.009, mirror=True)
    fin(k, "pelvic", PELVIC, (0.03, 0.05, Z0 - 0.048), unit(0.45, 0.88, -0.05), unit(0.3, 0.1, 1), "body",
        color=ORANGE, thick=0.009, mirror=True)
    fin(k, "anal", ANAL, (0.0, 0.15, Z0 - 0.035), ex, (0, 0, 1), "body", color=ORANGE, thick=0.009)
    fin(k, "dorsal", DORSAL, (0.0, 0.15, Z0 + 0.038), ex, (0, 0, 1), "dorsal3", color=ORANGE, thick=0.009)
    o = (0.0, 0.28, Z0 - 0.016)
    fin(k, "tailup", TAIL_UP, o, ex, (0, 0, 1), "tail", color=ORANGE, thick=0.011)
    fin(k, "taildown", TAIL_DOWN, o, ex, (0, 0, 1), "tail", color=ORANGE, thick=0.011)


EYE_AT = np.array([0.034, -0.245, Z0 + 0.03])
EYE_N = unit(0.78, -0.55, 0.28)
EYE_HALF = (0.017, 0.017)


def barraskewda(src, out):
    k = Kit(src, coat="scales")
    body(k)
    markings(k)
    teeth(k)
    spikes(k)
    fins(k)
    k.build(voxel=0.0038, smooth=4, band=0.004, wband=0.04)
    k.eye_states(lambda state: ar.lay_eyes(k, state, EYE_AT, EYE_N, EYE_HALF, BROWN, pupil_off=(-0.002, -0.001),
                                           pupil=0.55))
    k.export(out)


if __name__ == "__main__":
    barraskewda(*args())
