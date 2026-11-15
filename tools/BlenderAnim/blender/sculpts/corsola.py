# Corsola sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/corsola.py -- <the export's Corsola.glb> <out.glb>
#
# The design, from the references: a round pink body, its lower part pale sea blue with a ragged edge and a few blue
# flecks over it; four short blue stub legs under it, the belly near the ground; short blunt pink stubs out of its
# lower sides; a small face low on the front, two black oval eyes with a white glint and a thin smile; and out of its
# back and top great pink branches of coral, thick and blunt, each with a stub or two of its own, spreading up, out
# to the left and back.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, tri, args

PINK, BLUE, INK, WHITE = "#F28A9A", "#9AD8E6", "#1E1C26", "#FFFFFF"

C = np.array([0.0, 0.0, 0.176])        # the body's middle, on the body bone
RB = (0.162, 0.155, 0.155)


def edge_z(P):
    """The ragged line under which the body is blue."""
    th = np.arctan2(P[:, 1] - C[1], P[:, 0] - C[0])
    return C[2] - 0.075 + 0.015 * tri(th * 7 / (2 * math.pi) + 0.15) + 0.008 * np.sin(3 * th + 0.6)


FLECKS = [(-2.3, 0.022, 0.016), (-0.55, 0.024, 0.014), (0.9, 0.02, 0.017), (2.5, 0.024, 0.015), (-1.25, 0.05, 0.011)]


def flecks(P):
    m = np.zeros(len(P))
    for th, up, r in FLECKS:
        n = unit(math.cos(th), math.sin(th), -0.3)
        q = C + n * RB[0]
        q[2] = C[2] - 0.075 + up
        m = np.maximum(m, inside(np.linalg.norm(P - q, axis=1) - r, 0.0025))
    return m


REACH = 0.86                            # the branches' length, a share of the points written below


def branch(k, name, pts, r0, r1, stubs=()):
    """A thick blunt branch of coral through pts, tapering r0 to r1, with stubs (a point along it, a direction, a
    length) of its own."""
    pts = np.array(pts, float)
    o = pts[0]
    pts = o + (pts - o) * REACH
    k.add(name, tube(pts, np.linspace(r0, r1, len(pts))), PINK, "coral")
    for i, (at, d, ln) in enumerate(stubs):
        at = o + (np.array(at, float) - o) * REACH; d = unit(*d)
        k.add(f"{name}s{i}", tube([at, at + d * ln * 0.6, at + d * ln], [r1 * 0.9, r1 * 0.85, r1 * 0.85]), PINK,
              "coral")


def body(k):
    k.add("body", ell(C, RB), PINK, "body")
    for s, (x, y, bone) in enumerate(((0.07, -0.06, "leg{s}"), (0.07, 0.06, "hind{s}"))):
        k.add(f"leg{s}", ell((x, y, 0.04), (0.036, 0.036, 0.038)), BLUE, bone, mirror=True)
    # Blunt pink stubs out of the lower sides
    for i, d in enumerate((unit(1, -0.55, -0.22), unit(0.75, 0.75, -0.2))):
        a = C + d * 0.12
        k.add(f"stub{i}", tube([a, C + d * 0.185, C + d * 0.212], [0.031, 0.029, 0.028]), PINK, "body", mirror=True)
    k.paint(lambda P: np.maximum(inside(P[:, 2] - edge_z(P), 0.0025), flecks(P)), BLUE, on=["body", "leg"])

    branch(k, "b1", [(0.04, 0.02, 0.26), (0.085, 0.03, 0.37), (0.125, 0.04, 0.48)], 0.046, 0.034,
           [((0.095, 0.032, 0.39), (1, 0.1, 0.35), 0.06)])
    branch(k, "b2", [(-0.035, -0.01, 0.26), (-0.075, -0.02, 0.37), (-0.105, -0.03, 0.47)], 0.044, 0.033,
           [((-0.08, -0.022, 0.38), (-1, -0.3, 0.3), 0.055)])
    branch(k, "b3", [(-0.08, 0.04, 0.24), (-0.17, 0.035, 0.3), (-0.265, 0.025, 0.35)], 0.045, 0.032,
           [((-0.2, 0.033, 0.315), (-0.15, -0.2, 1), 0.06), ((-0.24, 0.028, 0.335), (-0.2, 0.3, -1), 0.045)])
    branch(k, "b4", [(0.02, 0.07, 0.25), (0.055, 0.14, 0.35), (0.085, 0.2, 0.44)], 0.044, 0.032,
           [((0.06, 0.15, 0.36), (0.6, 1, 0.1), 0.055)])
    branch(k, "b5", [(0.09, 0.0, 0.22), (0.15, -0.005, 0.25), (0.2, -0.01, 0.275)], 0.038, 0.03)

    # The smile, low on the front
    def smile(u, v):
        c = np.tile(srgb(PINK), (len(u), 1))
        c[(np.abs(v - (0.62 * u * u - 0.25)) < 0.24) & (np.abs(u) < 0.8)] = srgb(INK)
        c[(np.abs(u) > 0.62) & (np.abs(u) < 0.9) & (np.abs(v - (0.15 + 0.9 * (np.abs(u) - 0.62))) < 0.2)] = srgb(INK)
        return c
    k.decal(C + unit(0, -1, -0.32) * 0.155, unit(0, -1, -0.32), (0.034, 0.016), smile, eps=0.004, rings=12, segs=56)


CK, CW, CP = srgb(INK), srgb(WHITE), srgb(PINK)
EYE_D = unit(0.36, -1.0, 0.0)
EYE_HALF = (0.02, 0.03)


def eye_open(u, v):
    c = np.tile(CK, (len(u), 1))
    c[((u + 0.3) / 0.3) ** 2 + ((v - 0.4) / 0.26) ** 2 < 1] = CW
    return c


def eyes(k, state):
    k.decal(C + EYE_D * 0.155, EYE_D, EYE_HALF, eye_look(state, eye_open, CP, CK, line=0.2), eps=0.004, rings=20,
            segs=56, mirror=True)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    body(k)
    k.build(voxel=0.006, smooth=4, band=0.006)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
