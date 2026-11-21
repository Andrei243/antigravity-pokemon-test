# Klinklang sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# klang.py and klink.py:
#   blender --background --factory-startup --python sculpts/klinklang.py -- <the export's Klinklang.glb> <out.glb>
#
# From the references: Klang's two gears (the small faced one at the upper left in front, the great darker one behind
# with its pale gear and big teal ball on its front), the great gear's black line now an arc over its top and round
# its right ending in a black dot, the black square at its upper left. Under them, at the level of the ring, a darker
# gear of nine teeth lying nearly flat (its front a little lower), a red-pink dome in its middle; the great gear's
# lower teeth pass down through it. Round everything a wide steel band, a halo seen nearly edge on, tilted a little
# lower to the right, with thorn-like spikes swept along it, the longest at the right. (The task's text put the red
# core on a ring gear behind and the band below: the pictures put the core on the flat gear inside the band.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, args, hull, ring_band, axes, sibling

ka = sibling("klang")
kl = ka.kl
BAND = "#C3C7D2"

SMALL_R, SMALL_T = 0.11, 0.032
SMALL = np.array([-0.14, -0.013, 0.376])
GREAT_R, GREAT_T = 0.155, 0.04
GREAT = np.array([0.07, 0.030, 0.298])
HUB_R, HUB_T = 0.092, 0.030
HUB = np.array([0.065, -0.0085, 0.297])
FACE = (12, 120, -10, 25, 225, 30)

# The flat gear on the ring bone, its red dome; the halo
FLAT_C, FLAT_R, FLAT_T, FLAT_TILT = np.array([0.005, 0.0, 0.18]), 0.12, 0.03, 15
HALO_C, HALO_R, HALO_H, HALO_T, HALO_TILT = np.array([0.01, 0.0, 0.208]), 0.29, 0.032, 0.016, 4
SPIKES = [(0, 0.095), (62, 0.06), (118, 0.06), (180, 0.08), (242, 0.06), (298, 0.06)]   # azimuth, length
SWEEP = 35            # degrees the spikes lean along the band


def spike(base, d, length, r, sides=5):
    """A thorn: a straight pointed cone with flat faces, its root sunk into the band."""
    a, b = axes(d)
    base = np.array(base, float)
    P = [base + d * length, base - d * r * 0.5]
    for i in range(sides):
        th = 2 * math.pi * i / sides
        P.append(base + r * (math.cos(th) * a + math.sin(th) * b))
    return hull(P)


def halo(k):
    t = math.radians(HALO_TILT)
    ax = np.array([math.sin(t), 0, math.cos(t)])
    k.add("halo", ring_band(HALO_C, ax, HALO_R, HALO_H, thick=HALO_T), BAND, "ring", fuse=False, surface="metal")
    u1, u2 = np.array([math.cos(t), 0, -math.sin(t)]), np.array([0, 1.0, 0])
    sw = math.radians(SWEEP)
    for i, (az, length) in enumerate(SPIKES):
        f = math.radians(az)
        radial = math.cos(f) * u1 + math.sin(f) * u2
        tangent = -math.sin(f) * u1 + math.cos(f) * u2
        d = math.cos(sw) * radial + math.sin(sw) * tangent + 0.12 * ax
        d /= np.linalg.norm(d)
        base = HALO_C + radial * (HALO_R + HALO_T / 2 - 0.004)
        k.add(f"spike{i}", spike(base, d, length, 0.02), BAND, "ring", fuse=False, surface="metal")


def flat_gear(k):
    t = math.radians(FLAT_TILT)
    fr = kl.frame((0, -math.sin(t), math.cos(t)))
    kl.gear(k, "flat", "ring", FLAT_C, FLAT_R, 9, fr, FLAT_T, colour=kl.STEEL, turn=10)
    kl.core(k, "redcore", "ring", FLAT_C, FLAT_R, fr, FLAT_T, share=0.42, colour=kl.RED, squash=0.5)


def klinklang(src, out):
    k = Kit(src, coat="metal")
    turn_, e, p, pr, m, mr = FACE
    fr = kl.FR
    kl.faced_gear(k, "small", "head", SMALL, SMALL_R, 7, fr, SMALL_T, turn_, p, pr, m, mr)
    ka.great_gear(k, "body", GREAT, GREAT_R, fr, GREAT_T, HUB, HUB_R, HUB_T, turn_=9, hub_turn=20)
    ka.great_face(k, "body", GREAT, fr, GREAT_T, 0.108, arc=(-24, 96), square_at=126, dot_at=-24, size=0.012)
    flat_gear(k)
    halo(k)
    k.build(voxel=0.0035, smooth=3, wband=0.01)
    k.eye_states(lambda state: kl.eye(k, state, SMALL, SMALL_R, fr, SMALL_T, e))
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    klinklang(src, out)
