# Solrock sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/solrock.py -- <the export's Solrock.glb> <out.glb>
#
# The design, from the references: a ball of orange rock floating, facing the front, with eight long yellow rays round
# it in its own plane like the sun's, each a flame-like crystal wide at its root, kinked, narrowing to a point; pale
# yellow chips of stone stuck about the ball; two round yellow eyes in a dark rim, a black bar straight across each with
# a small red pupil in its middle, and short black ticks round each eye like a sun's rays. The eyes are always this
# half-shut slit: the open state is the bar, and the others are written by hand so the yellow disc and its ticks stay.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, srgb, unit, args

ORANGE, RAY, CHIP, DISC, INK, RED = "#E2772A", "#F6CE46", "#F3D67A", "#F8DC5C", "#1E1A18", "#E8303A"

C = np.array([0.0, 0.0, 0.317])           # the ball's middle, on the body bone
BR = 0.132
RAYS = [12 + 45 * i for i in range(8)]   # the rays' angles in the ball's plane, from the model's +X toward the top
RAY_Y = 0.018                             # the rays' plane, a little behind the ball's middle


def ray(k, i, ang):
    a = math.radians(ang)
    d = np.array([math.cos(a), 0, math.sin(a)])
    p = np.array([-math.sin(a), 0, math.cos(a)])
    y = np.array([0, 1.0, 0])
    o = C + y * RAY_Y
    kink = 1 if i % 2 else -1
    # root to the kink: wide and thick
    root = [o + d * 0.085 + p * w + y * t for w in (0.036, -0.036) for t in (0.022, -0.022)]
    mid = o + d * 0.19 + p * 0.014 * kink
    midpts = [mid + p * w + y * t for w in (0.029, -0.029) for t in (0.017, -0.017)]
    k.add(f"ray{i}a", hull(root + midpts), RAY, "rays", fuse=False, surface="shell")
    # the kink to the point, turned a little to the other side
    tip = o + d * 0.305 - p * 0.012 * kink
    k.add(f"ray{i}b", hull(midpts + [mid + d * 0.02 + p * 0.03, tip, tip + y * 0.004]), RAY, "rays", fuse=False,
          surface="shell")


CHIPS = [(-0.62, -0.62, 0.48), (0.08, -0.5, 0.86), (-0.12, -0.92, -0.36), (0.35, -0.82, -0.45), (-0.55, -0.62, -0.55),
         (0.72, -0.5, 0.48), (-0.3, -0.62, 0.72), (0.6, 0.6, 0.5), (-0.5, 0.7, -0.4), (0.1, 0.9, 0.4), (0.82, -0.3, -0.48),
         (-0.86, -0.2, 0.0)]


def chips(k):
    rs = np.random.RandomState(338)
    for i, d in enumerate(CHIPS):
        n = unit(*d)
        cc = C + n * (BR - 0.004)
        s = 0.012 + 0.008 * rs.rand()
        pts = [cc + (rs.rand(3) - 0.5) * 2 * s * np.array([1, 1, 1]) for _ in range(7)] + [cc + n * s * 0.8]
        k.add(f"chip{i}", hull(pts), CHIP, "body", fuse=False, surface="shell")


EYE_X, EYE_Z = 0.5, 0.03
DISC_R = 0.72                             # the disc's radius in the patch (the rest is ticks on the orange)


def eye_face(state):
    """The patch's colour in a state: the ticks and the yellow disc in its rim stay; the line across changes."""
    O, Y, K, Rd = srgb(ORANGE), srgb(DISC), srgb(INK), srgb(RED)

    def look(u, v):
        r = np.sqrt(u * u + v * v)
        th = np.arctan2(v, u)
        c = np.tile(O, (len(u), 1))
        near = np.abs(((th / (math.pi / 4)) + 0.5) % 1 - 0.5) * (math.pi / 4) * r
        c[(r > 0.76) & (r < 0.98) & (near < 0.075)] = K
        c[r < DISC_R] = K
        inner = r < DISC_R - 0.1
        c[inner] = Y
        q = u / (DISC_R - 0.1)
        if state == "Open":
            c[inner & (np.abs(v) < 0.095)] = K
            c[inner & (np.abs(u) < 0.14) & (np.abs(v) < 0.06)] = Rd
        elif state == "Fierce":
            edge = 0.22 * q
            c[inner & (np.abs(v - edge) < 0.1)] = K
            c[inner & (np.abs(u) < 0.13) & (np.abs(v - edge) < 0.06)] = Rd
        elif state == "Shut":
            c[inner & (np.abs(v - (0.02 - 0.2 * (1 - q * q))) < 0.085)] = K
        elif state == "Smile":
            c[inner & (np.abs(v - (-0.16 + 0.26 * (1 - q * q))) < 0.085)] = K
        else:
            c[inner & (np.abs(np.abs(v) - 0.22 * (q + 1) / 2) < 0.08)] = K
        return c
    return look


def eyes(k, state):
    y = -math.sqrt(1 - EYE_X ** 2 - EYE_Z ** 2)
    n = np.array([EYE_X, y, EYE_Z])
    k.decal(C + n * BR, n, (0.064, 0.064), eye_face(state), eps=0.0045, rings=40, segs=128, mirror=True)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    k.add("ball", ell(C, (BR, BR * 0.97, BR)), ORANGE, "body")
    for i, a in enumerate(RAYS):
        ray(k, i, a)
    chips(k)
    k.build(voxel=0.004, smooth=3)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
