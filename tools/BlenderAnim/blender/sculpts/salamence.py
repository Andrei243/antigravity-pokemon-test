# Salamence sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/salamence.py -- <the export's Salamence.glb> <out.glb>
# From the references: a blue dragon on four legs, its body low and long, the deep chest and belly a pale grey-white
# crossed by darker plates (not red: the pictures overrule the brief), a thick neck rising forward to a long head held
# high with its mouth open (pink inside, white fangs down from the upper jaw, the lower jaw pale), a red cap over the
# brow and the back of the skull, red down the throat; three blue crests a side, flat-faced blades swept back off the
# skull (the tallest up and back, one out to the side, one down off the jaw); small angry eyes under the red brow. Thick
# legs splayed out, three white claws a foot; a long flat tail out behind, red beneath; two great red crescent wings
# off the shoulders, thick plates raised in a V and swept back.
# Bagon's line (sibling("bagon")): its colours, its claws and fangs and its angry eye cut by a brow.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, slab, outward, srgb, inside, unit, polygon, eye_look, args, sibling

bg = sibling("bagon")

BLUE = "#3FA6D6"
RED, DARKRED = "#D33A4E", "#A82A3C"
BELLY, PLATE = "#E6E8EE", "#B4B8C4"
MOUTH = "#C9566A"
JAW = "#E8EAF0"
FANG = bg.FANG
EYE_RED = srgb("#B82634")

HC = np.array([0.0, -0.345, 0.612])         # the skull's middle
HS = 1.18                                   # the head a size larger than first measured
HR = np.array([0.072, 0.085, 0.066]) * HS


def spike(base, d, length, width, thick, up):
    """A crest or spike: a flat-faced blade from base along d, wide across `up`, thick along it."""
    base, d = np.array(base, float), unit(*d)
    up = np.array(up, float); up = up - d * (up @ d); up /= np.linalg.norm(up)
    side = np.cross(d, up)
    b = base - d * 0.02
    return hull([b + side * thick / 2, b - side * thick / 2, b + up * width / 2, b - up * width / 2,
                 base + d * length * 0.5 + side * thick * 0.3, base + d * length * 0.5 - side * thick * 0.3,
                 base + d * length * 0.5 + up * width * 0.3, base + d * length * 0.5 - up * width * 0.3,
                 base + d * length])


# --- the wings: crescents off the shoulders, raised in a V and swept back -----------------------------------------------
WING_ROOT = np.array([0.075, -0.01, 0.39])
WING_EX = unit(0.3, 0.95, 0.12)            # back, a little out and up
WING_EZ = unit(0.85, -0.05, 0.8)           # up, leaning out (the V)
WING_SCALE = 0.86
WING = [(0.0, 0.0), (-0.02, 0.15), (0.0, 0.3), (0.06, 0.41), (0.13, 0.46), (0.25, 0.47), (0.37, 0.42),
        (0.47, 0.32), (0.54, 0.18), (0.58, 0.03), (0.6, -0.1), (0.52, 0.0), (0.43, 0.09), (0.33, 0.14),
        (0.23, 0.14), (0.14, 0.1), (0.07, 0.03), (0.05, -0.04)]
LEAD = [(0.0, 0.0), (-0.015, 0.15), (0.0, 0.3), (0.06, 0.41), (0.13, 0.46), (0.25, 0.47), (0.37, 0.42),
        (0.47, 0.32), (0.54, 0.18), (0.58, 0.03), (0.6, -0.1)]


def wing_frame():
    ex = WING_EX
    ez = WING_EZ - ex * (WING_EZ @ ex); ez /= np.linalg.norm(ez)
    return ex, ez, np.cross(ex, ez)


def wing_point(a, b, lift=0.0):
    ex, ez, n = wing_frame()
    return WING_ROOT + WING_SCALE * (a * ex + b * ez) + n * lift


def wings(k):
    ex, ez, n = wing_frame()
    P = np.array(WING) * WING_SCALE
    k.add("wing", slab([tuple(p) for p in P], WING_ROOT, ex, ez, 0.026), RED, "wing{s}", fuse=False, mirror=True)
    # A thick rounded leading edge round the crescent's outer side
    lead = outward(tube([wing_point(*p) for p in LEAD], [0.03, 0.028, 0.026, 0.024, 0.022, 0.021, 0.02, 0.018,
                                                          0.016, 0.013, 0.008]))
    k.add("wingedge", lead, DARKRED, "wing{s}", fuse=False, mirror=True)


def tail_under(path, rads, share=0.2):
    """The red under a tube's line: points below its centre by more than share of its radius."""
    fine = []
    for a, b, ra, rb in zip(path[:-1], path[1:], rads[:-1], rads[1:]):
        for t in np.linspace(0, 1, 30, endpoint=False):
            fine.append((*(np.array(a) + (np.array(b) - np.array(a)) * t), ra + (rb - ra) * t))
    fine = np.array(fine)
    C, R = fine[:, :3], fine[:, 3]
    def mask(P):
        j = np.concatenate([((P[a:a + 3000, None, :] - C[None]) ** 2).sum(2).argmin(1) for a in range(0, len(P), 3000)])
        return inside(P[:, 2] - C[j, 2] + share * R[j], 0.003) * (j > 6)
    return mask


def claws(k, foot, front, spread, bone):
    for j, dx in enumerate((-spread, 0.0, spread)):
        b = np.array((foot[0] + dx, front, 0.025))
        k.add(f"{bone.split('{')[0]}claw{j}", bg.fang(b, (dx * 1.2, -1, -0.3), 0.04, 0.026, up=(0, 0, 1)), FANG, bone,
              fuse=False, mirror=True, surface="shell")


def salamence(src, out):
    k = Kit(src, coat="scales")

    # The body: low and long, a deep chest, the belly hanging low between the legs
    k.add("torso", ell((0, 0.02, 0.245), (0.145, 0.22, 0.13)), BLUE, "body")
    k.add("chest", ell((0, -0.1, 0.27), (0.13, 0.115, 0.135)), BLUE, "body")
    k.add("rump", ell((0, 0.15, 0.24), (0.13, 0.1, 0.115)), BLUE, "body")

    # The neck: thick, rising forward to the head
    k.add("neck", tube([(0, -0.13, 0.33), (0, -0.21, 0.44), (0, -0.27, 0.53), (0, -0.31, 0.58)],
                       [0.098, 0.08, 0.068, 0.06]), BLUE, "head")
    # The throat: red down the front of the neck
    def throat(P):
        t = np.clip((P[:, 2] - 0.33) / 0.24, 0, 1)
        cy = -0.13 - 0.18 * t
        return inside(P[:, 1] - cy + 0.03, 0.003) * inside(np.abs(P[:, 0]) - 0.04, 0.003) * \
            (P[:, 2] > 0.34) * (P[:, 2] < 0.555)
    k.paint(throat, RED, on=["neck"])

    # The head: a long skull, the upper jaw raised over an open mouth, the pale lower jaw dropped
    k.add("skull", ell(HC, HR), BLUE, "head")
    k.add("snout", ell(HC + HS * np.array((0, -0.1, 0.014)), HS * np.array((0.056, 0.095, 0.034)), rot=(-10, 0, 0)),
          BLUE, "head")
    k.add("jaw", ell(HC + HS * np.array((0, -0.075, -0.068)), HS * np.array((0.05, 0.088, 0.026)), rot=(18, 0, 0)),
          JAW, "head")
    k.add("mouth", ell(HC + HS * np.array((0, -0.066, -0.03)), HS * np.array((0.046, 0.08, 0.034)), rot=(4, 0, 0)),
          MOUTH, "head")
    # The red cap over the brow and the back of the skull
    def cap(P):
        q = (P - HC) / HS
        return inside(-(q[:, 2] - 0.022 - 0.35 * (q[:, 1] + 0.03)), 0.003) * inside(np.linalg.norm(q, axis=1) - 0.11,
                                                                                    0.003)
    k.paint(cap, RED, on=["skull"])

    # The fangs down from the front of the upper jaw
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"fang{side}", bg.fang(HC + HS * np.array((0.03 * s, -0.17, -0.008)), unit(0, -0.2, -1), 0.042, 0.026),
              FANG, "head", fuse=False, surface="shell")

    # The crests: flat-faced blue blades swept back off the skull
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"crestup{side}", spike(HC + HS * np.array([0.035, 0.02, 0.035]) * m, unit(0.38 * s, 0.6, 0.7), 0.21, 0.075,
                                      0.034, (0, -0.6, 0.8)), BLUE, "head", fuse=False)
        k.add(f"crestside{side}", spike(HC + HS * np.array([0.055, 0.0, 0.0]) * m, unit(0.75 * s, 0.6, 0.15), 0.14,
                                        0.056, 0.03, (0, 0, 1)), BLUE, "head", fuse=False)
        k.add(f"crestjaw{side}", spike(HC + HS * np.array([0.04, -0.02, -0.055]) * m, unit(0.35 * s, 0.45, -0.8), 0.11,
                                       0.05, 0.03, (0, -1, 0)), BLUE, "head", fuse=False)

    # The legs: heavy masses fused into the shoulders and haunches, short thick lower legs splayed out to broad feet
    k.add("shoulder", ell((0.11, -0.12, 0.24), (0.085, 0.085, 0.095)), BLUE, "leg{s}", mirror=True)
    k.add("foreleg", tube([(0.13, -0.13, 0.2), (0.17, -0.17, 0.11), (0.19, -0.2, 0.045)], [0.07, 0.06, 0.053]),
          BLUE, "leg{s}", mirror=True)
    k.add("forefoot", ell((0.192, -0.215, 0.027), (0.062, 0.07, 0.03)), BLUE, "leg{s}", mirror=True)
    claws(k, (0.192, -0.215), -0.275, 0.036, "leg{s}")
    k.add("haunch", ell((0.12, 0.13, 0.22), (0.095, 0.11, 0.11)), BLUE, "hind{s}", mirror=True)
    k.add("hindleg", tube([(0.14, 0.14, 0.16), (0.175, 0.12, 0.09), (0.19, 0.1, 0.045)], [0.07, 0.06, 0.053]),
          BLUE, "hind{s}", mirror=True)
    k.add("hindfoot", ell((0.192, 0.075, 0.027), (0.062, 0.07, 0.03)), BLUE, "hind{s}", mirror=True)
    claws(k, (0.192, 0.075), 0.015, 0.036, "hind{s}")

    # The tail: long and flat, out behind and round to the +X side, red beneath
    TAIL = [(0, 0.22, 0.26), (0.03, 0.36, 0.18), (0.1, 0.47, 0.12), (0.19, 0.55, 0.09), (0.28, 0.6, 0.075)]
    TR = [0.09, 0.072, 0.058, 0.042, 0.02]
    k.add("tail", blade(TAIL, TR, (0, 0, 1), 0.62), BLUE, "tail")
    k.paint(tail_under(TAIL, TR), RED, on=["tail"])

    wings(k)

    # The belly: pale grey-white from the chest down between the legs, crossed by darker plates
    P_, B_ = srgb(BELLY), srgb(PLATE)
    def plates(u, v):
        c = np.tile(P_, (len(u), 1))
        for r in (-0.55, -0.15, 0.25, 0.62):
            c[np.abs(v - r) < 0.05] = B_
        return c
    k.decal((0, -0.01, 0.14), unit(0, -0.25, -1), (0.1, 0.21), plates, up=(0, -1, 0), eps=0.004, rings=24, segs=72)
    k.decal((0, -0.2, 0.25), unit(0, -1, -0.3), (0.085, 0.08), BELLY, eps=0.0035, rings=12, segs=48)

    # The eyes: small and angry under the red brow, which slopes down toward the snout
    de = unit(0.66, -0.66, 0.3)
    E = bg.on_ell(HC, HR, de)
    cut = bg.brow_cut(0.25, 0.5)
    eyes = bg.eyes_under_brow(k, E, de, (0.03, 0.027), bg.angry_eye(cut, iris=EYE_RED, pupil_c=(-0.2, -0.15),
                                                                      pupil_r=0.26, iris_r=0.5), cut, srgb(BLUE))

    k.build(voxel=0.0065, smooth=3, band=0.004)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    salamence(*args())
