# Archen sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/archen.py -- <the export's Archen.glb> <out.glb>
#
# From the references: a small bird-lizard, its head as big as the rest of it: a long rounded red snout, the back of
# the skull blue; a big round eye on each side of the head just before the blue, white with a black pupil inside a
# bold black ring; two small white fangs under the snout. A ruff of red feathers round the neck, a round yellow body.
# Yellow arms with three white claws at the hand and a fringe of yellow feathers, blue flight feathers at their ends,
# held forward and a little down (the art flings them out); red feathers on the back; thin red legs with three toes
# and white claws; a thin red tail rising behind to a blue kite-shaped fan.
#
# Written to be grown from (Archeops: sibling("archen")): the line's colours, its eyes, fangs, wings, tail fan and
# feet are the functions below; the calls that build Archen are under __main__. The red scale lines on the snout are
# left out: at the game's size they are noise.
# Reference ratios (3D picture): head height / total 0.42, head length / total 0.65, body width / head width 0.75,
# legs showing 0.2 of the height, the eye 0.15 of the height across.
# The export's joints: body (0, 0, 0.18), wings (+-0.05, -0.02, 0.21), head (0, -0.04, 0.24), legs (+-0.03, 0, 0.13),
# tail (0, 0.06, 0.16); the export stands 0.354 tall.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, srgb, inside, unit, args, sibling

tc = sibling("torchic")

# --- the line's colours ---------------------------------------------------------------------------------------------
RED, BLUE, YELLOW, WHITE, INK = "#DE4A42", "#2E80D4", "#F4CC52", "#F6F4EE", "#16141C"
CLAW = "#F2F0EA"


def eye_open(ring=0.22, pupil=0.36):
    """A round eye: a bold black ring, a white, a round black pupil in the middle with a glint."""
    K, W = srgb(INK), srgb("#FFFFFF")
    def eye(u, v):
        c = np.tile(W, (len(u), 1))
        r = np.sqrt(u * u + v * v)
        c[r > 1 - ring] = K
        c[((u + 0.05) / pupil) ** 2 + ((v + 0.02) / (pupil * 1.15)) ** 2 < 1] = K
        c[((u + 0.0) / 0.12) ** 2 + ((v - 0.15) / 0.12) ** 2 < 1] = W
        return c
    return eye


def fang(base, length, r, d=(0, -0.2, -1)):
    """A small white fang hanging from the upper jaw: a flat-faced point."""
    return tc.spike(base, d, length, r, r * 0.8, across=(1, 0, 0))


def wing(k, shoulder, arm_d, arm_len, arm_r, hang, feathers, claws, bone="wing{s}", thick=0.012):
    """
    An arm with its feathers, mirrored: a tube from the shoulder along arm_d, and plates hung from it in the plane of
    arm_d and hang, each (colour, outline in units of arm_len as (along the arm, down the hang), lift off the plane).
    claws: (length, radius) of three white claws at the hand, pointing on along the arm.
    """
    ex = unit(*arm_d)
    ez = np.array(hang, float); ez = ez - ex * (ez @ ex); ez /= np.linalg.norm(ez)
    n = np.cross(ex, ez)
    s = np.array(shoulder, float)
    wrist = s + ex * arm_len
    k.add("arm", tube([s, s + ex * arm_len * 0.5, wrist], [arm_r * 1.15, arm_r, arm_r * 0.85]), YELLOW, bone,
          mirror=True)
    for i, (col, outline, lift) in enumerate(feathers):
        k.add(f"feather{i}", slab([(a * arm_len, b * arm_len) for a, b in outline], s + n * lift, ex, ez, thick),
              col, bone, fuse=False, mirror=True)
    if claws:
        ln, r = claws
        for j, sp in enumerate((-0.6, 0.0, 0.6)):
            d = ex + ez * sp * 0.5 - n * 0.25 * abs(sp)
            d = d / np.linalg.norm(d)
            b = wrist + ez * sp * arm_r * 0.9 + ex * arm_r * 0.4
            k.add(f"handclaw{j}", tc.spike(b, d, ln, r, r * 0.8), CLAW, bone, fuse=False, mirror=True, surface="shell")
    return wrist


def kite(tip, d, side, length, width, thick=0.012):
    """A tail's fan: a long four-pointed kite from tip along d, its face across side."""
    return slab([(0, 0), (-width, length * 0.35), (0, length), (width, length * 0.35)], tip, side, d, thick)


def zig(a0, a1, b_top, b_bottom, n, depth):
    """A plate's outline from (a0, 0) to (a1, 0) along the arm, down to a jagged lower edge of n points."""
    pts = [(a0, b_top), (a1, b_top)]
    for i in range(n * 2 + 1):
        a = a1 - (a1 - a0) * i / (n * 2)
        pts.append((a, b_bottom - depth if i % 2 else b_bottom))
    return pts


# --- Archen's frame -------------------------------------------------------------------------------------------------
C = np.array([0.0, -0.035, 0.282])            # the skull's centre, a tenth over the measure
CR = np.array([0.074, 0.08, 0.072])
DE = unit(0.8, -0.48, 0.3)
EYE_HALF = (0.027, 0.027)
VOXEL = 0.0038


def archen(src, out):
    k = Kit(src, coat="fur")

    # The head: a round skull, blue behind, and a long red snout before it
    k.add("skull", ell(C, CR), RED, "head")
    k.add("snout", ell((0, -0.13, 0.272), (0.054, 0.1, 0.048), rot=(-6, 0, 0)), RED, "head")
    k.paint(lambda P: inside(C[1] + 0.022 - 0.25 * (P[:, 2] - C[2]) - P[:, 1], 0.002) * (P[:, 2] > 0.19), BLUE,
            on=["skull"])
    for s in (1, -1):
        k.add(f"fang{'R' if s > 0 else 'L'}", fang((0.026 * s, -0.15, 0.236), 0.024, 0.009), WHITE, "head",
              fuse=False, surface="shell")

    # The neck and a round yellow body under the red ruff
    k.add("neck", tube([(0, -0.01, 0.17), (0, -0.025, 0.235)], [0.042, 0.044]), RED, "head")
    k.add("body", ell((0, 0.005, 0.15), (0.06, 0.066, 0.072)), YELLOW, "body")
    k.add("ruff", tc.ruff((0, -0.01, 0.205), (0.062, 0.06, 0.03), 9, 0.9, 0.35), RED, "body")
    for i, a in enumerate(np.radians([-50, -20, 20, 50, 160, 200])):
        d = unit(math.sin(a), -math.cos(a), -0.35)
        k.add(f"ruffspike{i}", tc.spike((0.05 * math.sin(a), -0.01 - 0.05 * math.cos(a), 0.205), d, 0.04, 0.016,
                                        0.012), RED, "body", fuse=False)

    # The wings, held forward and a little down: yellow arms and fringes, blue flight feathers at the ends
    wing(k, (0.05, -0.02, 0.208), (0.78, -0.5, -0.38), 0.12, 0.018, (0.0, 0.3, -1.0),
         [(BLUE, zig(0.4, 1.3, -0.05, 0.72, 3, 0.14), 0.002),
          (YELLOW, zig(-0.05, 1.15, -0.18, 0.42, 4, 0.11), -0.004)],
         (0.03, 0.008))

    # Red feathers on the back, fanned back and up
    for i, (x, d, ln) in enumerate(((0.035, (0.45, 0.75, 0.5), 0.07), (-0.035, (-0.45, 0.75, 0.5), 0.07),
                                     (0.0, (0.0, 0.8, 0.6), 0.08))):
        k.add(f"plume{i}", tc.feather((x, 0.04, 0.19), d, ln, 0.024, thick=0.012), RED, "body", fuse=False)

    # The tail: thin and red, rising behind and out to +X, a blue kite at its end
    TAIL = [(0, 0.05, 0.13), (0.02, 0.12, 0.15), (0.05, 0.19, 0.2), (0.075, 0.24, 0.245)]
    k.add("tail", tube(TAIL, [0.022, 0.016, 0.013, 0.011]), RED, "tail")
    e = np.array(TAIL[-1]); d = unit(*(e - np.array(TAIL[-2])))
    k.add("fan", kite(e - d * 0.01, d, unit(0.85, -0.45, 0.0), 0.11, 0.032), BLUE, "tail", fuse=False)

    # Thin red legs, three toes with white claws
    k.add("thigh", ell((0.036, 0.0, 0.105), (0.034, 0.038, 0.042)), YELLOW, "leg{s}", mirror=True)
    k.add("leg", tube([(0.038, 0.0, 0.09), (0.042, -0.008, 0.045), (0.044, -0.01, 0.022)], [0.017, 0.015, 0.014]),
          RED, "leg{s}", mirror=True)
    tc.foot(k, (0.044, -0.012, 0.016), 0.04, 0.0105, RED, claws=CLAW, claw_len=0.02)
    k.paint(lambda P: inside(P[:, 2] - 0.074, 0.003), RED, on=["thigh", "leg"])

    k.build(voxel=VOXEL, smooth=3, band=0.005)

    E = tc.on_ell(C, CR, DE)
    lid = srgb(RED)
    k.eye_states(lambda state: tc.lay_eyes(k, state, E, DE, EYE_HALF, eye_open(), lid, srgb(INK), eps=0.003))
    k.export(out)


if __name__ == "__main__":
    archen(*args())
