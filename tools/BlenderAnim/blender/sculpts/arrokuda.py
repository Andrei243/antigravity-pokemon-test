# Arrokuda sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/arrokuda.py -- <the export's Arrokuda.glb> <out.glb>
#
# From the references: a long brown pike, round in section and thick enough to read, tapering to a pointed snout. A
# cream lower jaw that reaches as far forward as the brown upper one, a dark mouth line between them curving up toward
# the eye, and one small white fang hanging from the upper jaw. A big round eye high on the cheek: white, a thin black
# rim and a large black pupil looking forward. Behind the eye a crescent of orange-red round the gill, under the chin
# too. A cream stripe along the lower flank from the jaw to the tail, which takes the whole tail's root. Three white
# chevrons on each flank pointing back toward the tail. Small cream fins: a dorsal swept back, a pectoral pair, a
# pelvic pair and an anal fin; the tail of two rounded brown lobes, the upper one larger with a cream leading edge.
# The midline fins are turned 45 degrees to +X so the game's cameras see their faces. The joints: body (0, 0, 0.085),
# head (0, -0.13, 0.085), dorsal (0, 0.08, 0.135), tail (0, 0.19, 0.085), finR/L (+-0.04, -0.04, 0.065).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, hull, srgb, inside, unit, eye_look, args

BROWN, CREAM, FIN, ORANGE, WHITE, INK, MOUTH = ("#8E7A66", "#EEE6B2", "#EAE6C8", "#E0502C", "#F8F8F4",
                                                "#18161A", "#4A3A30")
Z0 = 0.085                       # the body's centre line


def body_tube(pts, radii, flat=0.84):
    """A round tube squashed from side to side about the midline (a fish's body is a little narrower than deep)."""
    V, F = tube(pts, radii, seg=24)
    V[:, 0] *= flat
    return V, F


def yaw_ex(deg):
    """The direction along a midline plate turned deg degrees to +X out of the body's own plane."""
    a = math.radians(deg)
    return (math.sin(a), math.cos(a), 0.0)


# --- the eye: white, a thin black rim, a large black pupil forward ----------------------------------------------------
CW, CK = srgb(WHITE), srgb(INK)


def eye_open(u, v):
    c = np.tile(CW, (len(u), 1))
    c[u * u + v * v > 0.86 ** 2] = CK                  # the rim, between two rings
    return c


def lay_eyes(k, state, at, n, half, lid, pupil_off=(-0.004, -0.002), pupil=0.48):
    """Both eyes: the white with its rim (a patch, the state's look) and a round black pupil (its own patch)."""
    look = eye_look(state, eye_open, srgb(lid), CK, line=0.16)
    for s in (1, -1):
        nn = np.array(n, float) * np.array([s, 1, 1])
        c = np.array(at, float) * np.array([s, 1, 1])
        k.decal(c, nn, half, look if s > 0 else (lambda u, v, f=look: f(-u, v)), eps=0.0025, rings=20, segs=56)
        if state in ("Open", "Fierce"):
            # the pupil toward the snout: offset along the body (y) and a little down
            k.decal(c + np.array([0.0, pupil_off[0], pupil_off[1]]), nn, (half[0] * pupil, half[1] * pupil), INK,
                    eps=0.0034, rings=8, segs=36)


def fang(k, name, root, length, bone, mirror=True):
    """A small white fang hanging from the upper jaw: a flat-faced hull."""
    r = np.array(root, float)
    w = length * 0.42
    pts = [r + (0, -w, 0), r + (0, w, 0), r + (w * 0.55, 0, 0.004), r + (-w * 0.3, 0, 0.004),
           r + (w * 0.15, -w * 0.2, -length)]
    k.add(name, hull(pts), WHITE, bone, fuse=False, mirror=mirror, surface="shell")


def fin(k, name, outline, origin, ex, ez, bone, color=FIN, thick=0.006, mirror=False):
    k.add(name, slab(outline, origin, ex, ez, thick), color, bone, fuse=False, mirror=mirror)


def body(k):
    # the trunk, deepest just behind the gill, tapering into the tail's root
    k.add("trunk", body_tube([(0, -0.12, Z0 + 0.003), (0, -0.04, Z0), (0, 0.05, Z0 - 0.001), (0, 0.13, Z0 - 0.002)],
                             [0.046, 0.052, 0.046, 0.032]), BROWN, "body")
    k.add("stalk", body_tube([(0, 0.12, Z0 - 0.002), (0, 0.18, Z0 - 0.002), (0, 0.215, Z0 - 0.001)],
                             [0.032, 0.02, 0.014], flat=0.8), CREAM, "tail")
    # the head: brown above, tapering to the upper jaw's point
    k.add("head", body_tube([(0, -0.2, Z0 + 0.008), (0, -0.15, Z0 + 0.006), (0, -0.1, Z0 + 0.004)],
                            [0.024, 0.04, 0.047]), BROWN, "head")
    k.add("snout", body_tube([(0, -0.255, Z0 + 0.0), (0, -0.22, Z0 + 0.006), (0, -0.18, Z0 + 0.01)],
                             [0.006, 0.016, 0.028], flat=0.9), BROWN, "head")
    # the cream lower jaw, its tip as far forward as the snout's, a little proud of the head below the mouth
    k.add("jaw", body_tube([(0, -0.255, Z0 - 0.012), (0, -0.2, Z0 - 0.017), (0, -0.14, Z0 - 0.02),
                            (0, -0.09, Z0 - 0.022)], [0.008, 0.02, 0.028, 0.03], flat=0.92), CREAM, "head")
    # the cheeks the eyes lie on
    k.add("cheek", ell((0.026, -0.115, Z0 + 0.018), (0.018, 0.03, 0.026)), BROWN, "head", mirror=True)


def mouth_z(y):
    """The mouth line along the jaw: low at the tip, rising toward the eye at its back end."""
    y = np.asarray(y, float)
    return Z0 - 0.006 + np.clip((y + 0.15) / 0.05, 0, 1) ** 2 * 0.006


def markings(k):
    # the cream stripe along the lower flank, rising into the tail's root
    def belly(P):
        y = P[:, 1]
        edge = np.interp(y, [-0.12, -0.06, 0.06, 0.11, 0.15], [Z0 - 0.012, Z0 - 0.016, Z0 - 0.018, Z0 - 0.006,
                                                                  Z0 + 0.06])
        return inside(P[:, 2] - edge, 0.0018) * (y > -0.12)
    k.paint(belly, CREAM, on=["trunk", "stalk"])
    # the mouth line from the tip back under the eye
    k.paint(lambda P: inside(np.abs(P[:, 2] - mouth_z(P[:, 1])) - 0.0028, 0.0014) * inside(P[:, 1] + 0.128, 0.002)
            * inside(-0.252 - P[:, 1], 0.002), MOUTH,
            on=["head", "snout", "jaw", "cheek"])

    # the orange-red crescent round the gill, bulging back, from the top of the flank round under the chin
    def gill(P):
        dz = P[:, 2] - Z0
        yc = -0.07 - 10.0 * np.clip(dz, None, 0) ** 2 - 16.0 * np.clip(dz, 0, None) ** 2
        return inside(np.abs(P[:, 1] - yc) - 0.012, 0.0016) * inside(dz - 0.032, 0.002)
    k.paint(gill, ORANGE)

    # three white chevrons each side, their points toward the tail
    def chevrons(P):
        m = np.zeros(len(P))
        dz = P[:, 2] - (Z0 + 0.004)
        side = inside(0.016 - np.abs(P[:, 0]), 0.002)
        for y0, h in ((-0.04, 0.03), (0.012, 0.027), (0.06, 0.023)):
            t = P[:, 1] - (y0 + 0.7 * (h - np.abs(dz)))
            m = np.maximum(m, inside(np.abs(t) - 0.0095, 0.0014) * inside(np.abs(dz) - h, 0.0016))
        return m * side
    k.paint(chevrons, WHITE)


# --- fins (cream plates) and the tail ------------------------------------------------------------------------------------
DORSAL = [(0.0, 0.0), (0.012, 0.03), (0.04, 0.042), (0.062, 0.036), (0.046, 0.016), (0.05, 0.0)]
PEC = [(0.0, 0.008), (0.03, 0.014), (0.058, -0.002), (0.062, -0.026), (0.04, -0.036), (0.016, -0.022), (0.0, -0.008)]
PELVIC = [(0.0, 0.006), (0.03, 0.006), (0.05, -0.014), (0.044, -0.034), (0.02, -0.03), (0.004, -0.014)]
ANAL = [(0.0, 0.0), (0.032, 0.0), (0.048, -0.022), (0.03, -0.034), (0.008, -0.016)]


def rounded(c, rx, ry, a0, a1, n=12, root=((0.0, 0.0),)):
    """An outline: an arc of an ellipse about c from angle a0 to a1 (degrees), closed through the root points."""
    pts = [(c[0] + rx * math.cos(math.radians(a)), c[1] + ry * math.sin(math.radians(a)))
           for a in np.linspace(a0, a1, n)]
    return list(root) + pts


UPPER = rounded((0.062, 0.05), 0.036, 0.034, 200, -40, root=((0.0, -0.006), (0.004, 0.012)))
UPPER_EDGE = [(0.004, 0.012), (0.026, 0.044), (0.046, 0.074), (0.06, 0.086), (0.06, 0.076), (0.04, 0.054),
              (0.018, 0.018), (0.002, -0.004)]
LOWER = rounded((0.044, -0.024), 0.028, 0.024, 160, -95, n=11, root=((0.0, 0.006),))


def fins(k):
    ex = yaw_ex(45)
    fin(k, "dorsal", DORSAL, (0.0, 0.055, Z0 + 0.044), ex, (0, 0, 1), "dorsal", thick=0.007)
    fin(k, "anal", ANAL, (0.0, 0.1, Z0 - 0.026), ex, (0, 0, 1), "body", thick=0.007)
    # the pectorals: swept back and out under the gill, faces toward the front
    fin(k, "pec", PEC, (0.03, -0.05, Z0 - 0.022), unit(0.55, 0.8, -0.25), unit(0.15, 0.2, 1), "fin{s}", mirror=True,
        thick=0.006)
    fin(k, "pelvic", PELVIC, (0.018, 0.035, Z0 - 0.04), unit(0.5, 0.82, -0.3), unit(0.2, 0.25, 1), "body",
        mirror=True, thick=0.006)
    # the tail: two brown lobes turned out to +X, the upper larger with a cream leading edge
    o = (0.0, 0.205, Z0)
    tex = yaw_ex(45)
    fin(k, "tailup", UPPER, o, tex, (0, 0, 1), "tail", color=BROWN, thick=0.008)
    fin(k, "tailedge", UPPER_EDGE, o, tex, (0, 0, 1), "tail", color=CREAM, thick=0.011)
    fin(k, "taildown", LOWER, o, tex, (0, 0, 1), "tail", color=BROWN, thick=0.008)


EYE_AT = np.array([0.04, -0.118, Z0 + 0.022])
EYE_N = unit(0.72, -0.62, 0.3)
EYE_HALF = (0.021, 0.021)


def arrokuda(src, out):
    k = Kit(src, coat="scales")
    body(k)
    markings(k)
    fang(k, "fang", (0.0175, -0.19, Z0 - 0.005), 0.017, "head")
    fins(k)
    k.build(voxel=0.0026, smooth=4, band=0.004, wband=0.03)
    k.eye_states(lambda state: lay_eyes(k, state, EYE_AT, EYE_N, EYE_HALF, BROWN))
    k.export(out)


if __name__ == "__main__":
    arrokuda(*args())
