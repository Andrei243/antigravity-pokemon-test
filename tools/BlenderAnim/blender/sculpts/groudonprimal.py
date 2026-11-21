# Primal Groudon sculpted on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/groudonprimal.py -- <the export's Groudon-Primal.glb> <out.glb>
#
# Grown from groudon.py through sibling: its body, legs, arms, tail and head are called with its colours set again
# (the grey underside black, the black seams pale yellow), and groudon() itself is copied here so the Primal's own
# paint can go in before the build. From the references: the same leaning red titan, the warm grey of its throat,
# belly, jaw and inner limbs turned charcoal black; its largest change, its armour broken into red plates by a
# network of broad glowing pale-yellow seams over the back, shoulders, thighs, arms and tail; the round marks on the
# thighs and shoulders yellow rings round a red disc; every spike (shoulders, back, elbows, knees, tail) black where
# Groudon's are white; the claws and teeth stay white; the eyes yellow under the black brow.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, srgb, inside, unit, args, sibling, smoothstep

g = sibling("groudon")

RED, BLACK, SEAM = "#D6263A", "#2A2228", "#F6EC9A"
g.RED, g.GREY, g.INKC = RED, BLACK, SEAM
g.CR = srgb(RED)

SPIKES = ("back", "knee", "elbow", "tspike")


def seams(k):
    """The glowing network: pale yellow along the edges of cells laid over the armour, never on the black."""
    rng = np.random.default_rng(383)
    # cells over the trunk, the thighs, the shoulders, the arms and the tail (by place, in the model's own frame)
    C = np.vstack([
        rng.uniform((-0.24, -0.25, 0.32), (0.24, 0.25, 0.75), (38, 3)),
        rng.uniform((-0.34, -0.12, 0.1), (0.34, 0.2, 0.42), (26, 3)),
        rng.uniform((-0.34, -0.33, 0.3), (0.34, -0.05, 0.62), (22, 3)),
        rng.uniform((-0.12, 0.15, 0.05), (0.3, 0.78, 0.45), (30, 3)),
    ])

    def belly(P):
        q = (P - np.array([0, -0.2, 0.36])) / np.array([0.15, 0.17, 0.27])
        return inside(np.linalg.norm(q, axis=1) - 1, 0.004) * (P[:, 1] < 0.12)

    def net(P):
        d = np.linalg.norm(P[:, None, :] - C[None, :, :], axis=2)
        d.sort(axis=1)
        edge = d[:, 1] - d[:, 0]
        return inside(edge - 0.011, 0.003) * (1 - belly(P)) * (P[:, 2] > 0.06)
    k.paint(net, SEAM, on=["torso", "hump", "thigh", "shoulder", "arm", "tail", "shin"])

    # the black underside again over the seams, and black on the inner faces of the upper arms
    k.paint(belly, BLACK, on=["torso", "chest", "neck", "hump"])
    # the trunk's flanks below the hump, the upper arms and the tail's underside black, as the pictures have them
    k.paint(lambda P: inside(P[:, 2] - 0.53 - 0.2 * np.clip(-P[:, 1], 0, 1), 0.004), BLACK,
            on=["torso", "chest", "hump"])
    k.paint(lambda P: inside(0.4 - P[:, 2], 0.004) * inside(P[:, 1] + 0.2, 0.004), BLACK, on=["arm"])
    k.paint(lambda P: inside(P[:, 2] - (0.33 - 0.55 * (P[:, 1] - 0.18)), 0.004) * smoothstep(0.26, 0.34, P[:, 1]), BLACK,
            on=["tail"])

    # the round marks: a yellow ring round a red disc, with a yellow line down from it, on thighs and shoulders
    def ring(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(srgb(RED), (len(u), 1))
        c[(r > 0.62) | (np.abs(u) < 0.12) & (v < 0)] = srgb(SEAM)
        return c
    k.decal((0.312, 0.05, 0.27), (1, 0, 0), (0.095, 0.095), ring, eps=0.005, rings=24, segs=64, mirror=True)
    k.decal((0.285, -0.1, 0.56), unit(1, -0.25, 0.1), (0.07, 0.07), ring, eps=0.005, rings=24, segs=64, mirror=True)


def groudonprimal(src, out):
    k = Kit(src, coat="shell")
    g.body(k)
    g.legs(k)
    g.arms(k)
    g.tail(k)
    n0 = len(k.parts)
    g.head(k)
    for p in k.parts[n0:]:
        p["V"] = g.J + (p["V"] - g.J) * g.HS + g.SHIFT
    # every spike black
    black = srgb(BLACK)
    for p in k.parts:
        if p["name"].startswith(SPIKES):
            p["color"] = black
    seams(k)
    k.build(voxel=0.0085, smooth=4, band=0.006)
    k.eye_states(lambda state: g.eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    groudonprimal(*args())
