# Metang sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/metang.py -- <the export's Metang.glb> <out.glb>
#
# Grown from Beldum's script (the same steel, white claws, black joints and red eye in a black frame, every part a
# faceted hull). From the references: a floating saucer of blue steel plates, a darker underside; on its top front a
# raised brow with a dark trapezoid slot, one round red eye peering out of it (the pictures show one eye, in the slot's
# middle); two white spikes on each side of its top pointing out and up (the near side shows two, the far one hides
# its second). From under the saucer's rim a short dark rod with a ball each end runs out to each arm: a great steel
# block standing on the ground, narrow at the top and wide at its foot, with a round cuff and three white claws pointing
# down at its front.
#
# Written to be grown from (metagross.py imports it with sibling): column, arm, cuff and claws are functions.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, args, hull, slab, lathe, outward, polygon, sibling

b = sibling("beldum")
STEEL, SHADE, DEEP, JOINT, CLAW = b.STEEL, b.SHADE, b.DEEP, b.JOINT, b.CLAW


def column(stations, n=8, turn=22.5, tips=()):
    """A faceted solid up a line of stations: (centre, radius across x, radius across y) each, n sides; tips are
    single points added to the hull (a ridge, a point)."""
    P = []
    for c, ra, rb in stations:
        c = np.array(c, float)
        for i in range(n):
            th = 2 * math.pi * i / n + math.radians(turn)
            P.append(c + (ra * math.cos(th), rb * math.sin(th), 0))
    P.extend(np.array(t, float) for t in tips)
    return hull(P)


def cuff_and_claws(k, name, foot, r, bone, z0, height, claws=3, spread=55.0, length=0.09, cr=0.024, mirror=True,
                   facing=0.0):
    """The round cuff at an arm's foot (z0 to z0 + height) and its white claws pointing down from the front."""
    f = np.array(foot, float)
    k.add(name + "cuff", lathe((f[0], f[1], z0), (0, 0, 1), [(0, 0), (r * 0.9, 0), (r, 0.014), (r * 0.97, height),
                                                             (0, height)], seg=28), SHADE, bone, fuse=False,
          mirror=mirror, surface="metal")
    angs = np.linspace(-spread, spread, claws) if claws > 1 else [0.0]
    for i, a in enumerate(angs):
        t = math.radians(a + facing)        # facing: degrees round from the front toward +X
        radial = np.array([math.sin(t), -math.cos(t), 0.0])
        base = np.array([f[0], f[1], z0 + 0.008]) + radial * r * 0.7
        k.add(f"{name}claw{i}", b.claw(base, radial * 0.3 + np.array([0, 0, -1.0]), length, cr), CLAW, bone,
              fuse=False, mirror=mirror, surface="metal")


def rod(k, name, a, c, r, bone, ball=1.45):
    """A dark rod with a ball each end (the joint between a body and an arm)."""
    k.add(name, outward(tube([a, c], [r, r], seg=16, per=4)), SHADE, bone, fuse=False, mirror=True, surface="metal")
    for j, p in enumerate((a, c)):
        k.add(f"{name}ball{j}", outward(ell(p, (r * ball,) * 3, sub=3)), SHADE, bone, fuse=False, mirror=True,
              surface="metal")


# --- Metang -----------------------------------------------------------------------------------------------------------
SC = np.array([0.0, 0.02, 0.40])          # the saucer's middle
TOP = [  # (height over the middle, radius across x, radius across y)
    (-0.035, 0.26, 0.232), (0.035, 0.26, 0.232), (0.09, 0.22, 0.196), (0.13, 0.14, 0.125)]
UNDER = [(-0.13, 0.1, 0.09), (-0.09, 0.205, 0.18), (-0.029, 0.252, 0.224)]
BROW = np.array([0.0, -0.08, 0.51])       # the raised brow round the slot
SLOT_C, SLOT_N = np.array([0.0, -0.237, 0.475]), b.unit(0, -1, 0.12)
ARM = [((0.35, -0.045, 0.14), 0.086, 0.092), ((0.345, -0.045, 0.28), 0.08, 0.086), ((0.335, -0.04, 0.39), 0.066, 0.072)]
ARM_TIP = [(0.33, -0.02, 0.44), (0.33, 0.02, 0.45), (0.335, 0.045, 0.43)]
FOOT = (0.35, -0.045)


def saucer(k):
    k.add("top", column([(SC + (0, 0, t), ra, rb) for t, ra, rb in TOP], n=10, turn=0), STEEL, "body", fuse=False,
          surface="metal")
    k.add("under", column([(SC + (0, 0, t), ra, rb) for t, ra, rb in UNDER], n=10, turn=0), DEEP, "body",
          fuse=False, surface="metal")
    k.add("brow", column([(BROW + (0, 0, -0.075), 0.17, 0.17), (BROW + (0, 0, 0.0), 0.17, 0.17),
                          (BROW + (0, 0, 0.035), 0.125, 0.125), (BROW + (0, 0, 0.06), 0.06, 0.06)], n=8, turn=22.5),
          STEEL, "head", fuse=False, surface="metal")


SLOT = [(-1.0, 0.6), (1.0, 0.6), (0.78, -0.62), (-0.78, -0.62)]


def slot(k):
    """The dark slot the eye peers out of: a thin plate fused just proud of the brow's front."""
    n = SLOT_N
    up = np.array([0, 0, 1.0]); up = up - n * (up @ n); up /= np.linalg.norm(up)
    ex = np.cross(up, n)
    k.add("slot", slab(np.array(SLOT) * (0.074, 0.05), SLOT_C - n * 0.003, ex, up, 0.012), JOINT, "head")


def spikes(k):
    for name, at, d, L, r in (("spikefront", (0.2, -0.07, 0.505), (1, -0.35, 0.55), 0.12, 0.034),
                              ("spikeback", (0.18, 0.08, 0.505), (0.85, 0.35, 0.7), 0.1, 0.03)):
        k.add(name, b.claw(at, d, L, r), CLAW, "head", fuse=False, mirror=True, surface="metal")


def arms(k):
    k.add("arm", column(ARM, n=8, turn=22.5, tips=ARM_TIP), STEEL, "arm{s}", fuse=False, mirror=True,
          surface="metal")
    rod(k, "rod", (0.2, -0.03, 0.355), (0.272, -0.035, 0.355), 0.019, "arm{s}")
    cuff_and_claws(k, "foot", (FOOT[0], FOOT[1], 0), 0.1, "arm{s}", 0.07, 0.085)


def metang(src, out):
    k = Kit(src, coat="metal")
    saucer(k)
    spikes(k)
    arms(k)
    slot(k)
    k.build(voxel=0.0035, smooth=3)
    k.eye_states(lambda state: b.lay_eye(k, state, SLOT_C + SLOT_N * 0.004, SLOT_N, (0.03, 0.03)))
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    metang(src, out)
