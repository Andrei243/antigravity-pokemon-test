# Dragonite sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dragonite.py -- <the export's Dragonite.glb> <out.glb>
# From the references: a big, rounded orange dragon standing on two short heavy legs, its body a pear widest at the
# belly; a cream belly from the throat to between the legs, crossed by thin brown ribs, and cream under the thick tail,
# which sweeps back and round to the +X side down to the ground. A short thick neck, a round head with a broad rounded
# snout, small dark kind eyes with a glint, a little smile, a small horn on top, two long pale antennae rising from the
# brow and curling out. Short arms held forward and a little down, three white claws a hand and a foot. Two small wings
# off the shoulders behind, raised in a V and swept back: teal membrane on the front face, orange behind and along the
# edges.
# The export's joints: body (0, 0, 0.45), head (0, -0.03, 0.72), arms (+-0.17, -0.06, 0.6), wings (+-0.09, 0.13,
# 0.62), tail (0, 0.16, 0.3), legs (+-0.12, 0, 0.24).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, outward, srgb, inside, unit, eye_look, args

ORANGE, CREAM, RIB, TEAL = "#F2A048", "#F5E1A4", "#B88550", "#2A9FA0"
CLAW, ANTENNA, INK, MOUTH = "#F5F3EC", "#F0DA98", "#231814", "#6A3A20"


def claw(base, d, length, r, up=(0, 0, 1.0)):
    """A small solid claw: a flat-sided wedge from base along d."""
    d = unit(*d); up = np.array(up, float)
    a = np.cross(d, up); a /= np.linalg.norm(a); b = np.cross(a, d)
    base = np.array(base, float)
    return hull([base + a * r, base - a * r, base + b * r * 0.9, base - b * r * 0.6, base + d * length])


def cone(base, d, length, r, n=8):
    d = unit(*d)
    a = np.cross(d, (0, 1.0, 0)); a /= np.linalg.norm(a); b = np.cross(d, a)
    base = np.array(base, float)
    ring = [base + r * (math.cos(t) * a + math.sin(t) * b) for t in np.linspace(0, 2 * math.pi, n, endpoint=False)]
    return hull(ring + [base + d * length])


# --- the wings: small plates off the shoulders, raised in a V, swept back -------------------------------------------
WING_ROOT = np.array([0.08, 0.15, 0.66])
WING_EX = unit(0.8, 0.55, 0.2)             # out, swept back, a little up
WING_EZ = unit(0.3, 0.25, 1.0)             # up, leaning out (the V) and back
WING_SCALE = 0.7
WING = [(0.0, 0.03), (0.2, 0.3), (0.46, -0.06), (0.4, -0.05), (0.35, -0.13), (0.29, -0.08), (0.23, -0.15),
        (0.16, -0.09), (0.1, -0.12), (0.05, -0.05), (0.0, -0.06)]
TOP, TIP = (0.2, 0.3), (0.46, -0.06)


def wing_frame():
    ex = WING_EX
    ez = WING_EZ - ex * (WING_EZ @ ex); ez /= np.linalg.norm(ez)
    return ex, ez, np.cross(ex, ez)          # n: out and to the front, the teal face


def wing_point(a, b, lift=0.0):
    ex, ez, n = wing_frame()
    return WING_ROOT + WING_SCALE * (a * ex + b * ez) + n * lift


def wings(k):
    ex, ez, n = wing_frame()
    P = np.array(WING) * WING_SCALE
    mid = P.mean(axis=0)
    inner = [tuple(mid + (p - mid) * 0.86 + (0.008, 0.0)) for p in P]
    k.add("wingback", slab([tuple(p) for p in P], WING_ROOT, ex, ez, 0.018), ORANGE, "wing{s}", fuse=False, mirror=True)
    k.add("wingface", slab(inner, WING_ROOT + n * 0.0105, ex, ez, 0.006), TEAL, "wing{s}", fuse=False, mirror=True)
    lead = outward(tube([wing_point(0.0, 0.02), wing_point(0.1, 0.17), wing_point(*TOP), wing_point(0.34, 0.12),
                         wing_point(*TIP)], [0.03, 0.025, 0.02, 0.016, 0.01]))
    k.add("wingedge", lead, ORANGE, "wing{s}", fuse=False, mirror=True)


def tail_under(path, rads, share=0.25):
    """The cream under a tube's line: points below its centre by more than share of its radius."""
    fine = []
    for a, b, ra, rb in zip(path[:-1], path[1:], rads[:-1], rads[1:]):
        for t in np.linspace(0, 1, 30, endpoint=False):
            fine.append((*(np.array(a) + (np.array(b) - np.array(a)) * t), ra + (rb - ra) * t))
    fine = np.array(fine)
    C, R = fine[:, :3], fine[:, 3]
    def mask(P):
        j = np.concatenate([((P[a:a + 3000, None, :] - C[None]) ** 2).sum(2).argmin(1) for a in range(0, len(P), 3000)])
        dz = P[:, 2] - C[j, 2]
        return inside(dz + share * R[j], 0.004) * (j > 8)
    return mask


def dragonite(src, out):
    k = Kit(src, coat="scales")

    # The body: a great pear, widest at the belly, a narrower chest, a short thick neck
    k.add("torso", ell((0, 0.0, 0.42), (0.215, 0.2, 0.25)), ORANGE, "body")
    k.add("chest", ell((0, 0.0, 0.6), (0.165, 0.15, 0.14)), ORANGE, "body")
    k.add("neck", tube([(0, 0.01, 0.64), (0, -0.01, 0.73), (0, -0.04, 0.8)], [0.105, 0.088, 0.078]), ORANGE, "head")

    # The head: round, a broad rounded snout, a small horn on top
    H, HR = np.array([0.0, -0.06, 0.855]), np.array([0.097, 0.1, 0.09])
    k.add("skull", ell(H, HR), ORANGE, "head")
    k.add("snout", ell(H + (0, -0.093, -0.033), (0.077, 0.083, 0.064), rot=(-6, 0, 0)), ORANGE, "head")
    k.add("horn", cone(H + (0, -0.01, 0.07), unit(0, 0.45, 1), 0.06, 0.022), ORANGE, "head", fuse=False)

    # The antennae: from the brow over each eye, up, curling out and a little back
    k.add("antenna", tube([(0.03, -0.1, 0.9), (0.04, -0.1, 0.96), (0.07, -0.09, 1.01), (0.115, -0.06, 1.025),
                           (0.145, -0.035, 1.005)], [0.016, 0.015, 0.014, 0.013, 0.012]), ANTENNA, "head",
          fuse=False, mirror=True)

    # The belly: cream from the throat down between the legs, thin brown ribs across it
    BC, BH = np.array([0.0, -0.12, 0.47]), (0.145, 0.26)
    k.decal(BC, unit(0, -1, 0.15), BH, CREAM, eps=0.003, rings=24, segs=72)
    for z in np.linspace(BC[2] - 0.22, BC[2] + 0.19, 8):
        w = BH[0] * math.sqrt(max(0.0, 1 - ((z - BC[2]) / BH[1]) ** 2)) * 0.94
        k.decal((0, -0.12, z), (0, -1, 0), (w, 0.0045), RIB, eps=0.0055, rings=4, segs=48)

    # The arms: short, forward and a little down, three white claws a hand
    k.add("arm", tube([(0.15, -0.03, 0.63), (0.215, -0.09, 0.575), (0.225, -0.17, 0.535)], [0.058, 0.05, 0.044]),
          ORANGE, "arm{s}", mirror=True)
    HC = np.array((0.225, -0.205, 0.53))
    k.add("hand", ell(HC, (0.045, 0.042, 0.04)), ORANGE, "arm{s}", mirror=True)
    for j, d in enumerate([(0.45, -0.8, 0.1), (0.05, -0.95, 0.0), (-0.35, -0.85, 0.05)]):
        d = unit(*d)
        k.add(f"handclaw{j}", claw(HC + d * 0.036, d, 0.032, 0.012), CLAW, "arm{s}", fuse=False, mirror=True,
              surface="shell")

    # The legs: heavy thighs fused into the hips, short shins, broad feet with three white claws
    k.add("thigh", ell((0.14, -0.02, 0.24), (0.115, 0.125, 0.14), rot=(-10, 0, 0)), ORANGE, "leg{s}", mirror=True)
    k.add("shin", tube([(0.15, -0.035, 0.14), (0.155, -0.04, 0.06)], [0.075, 0.068]), ORANGE, "leg{s}", mirror=True)
    k.add("foot", ell((0.155, -0.08, 0.035), (0.078, 0.112, 0.037)), ORANGE, "leg{s}", mirror=True)
    for j, dx in enumerate((-0.04, 0.0, 0.04)):
        b = np.array((0.155 + dx, -0.175, 0.024))
        k.add(f"toe{j}", claw(b, (dx * 0.5, -1, -0.15), 0.036, 0.016), CLAW, "leg{s}", fuse=False, mirror=True,
              surface="shell")

    # The tail: thick, out back from the hips and round to the +X side down to the ground, cream beneath
    TAIL = [(0.0, 0.12, 0.32), (0.04, 0.27, 0.2), (0.13, 0.38, 0.12), (0.25, 0.43, 0.07), (0.36, 0.44, 0.04)]
    TR = [0.13, 0.1, 0.075, 0.05, 0.022]
    k.add("tail", tube(TAIL, TR), ORANGE, "tail")
    k.paint(tail_under(TAIL, TR), CREAM, on=["tail"])

    wings(k)

    # The face: a little smile and two nostrils on the snout
    MC = H + np.array((0, -0.17, -0.062))
    O, M = srgb(ORANGE), srgb(MOUTH)
    def smile(u, v):
        c = np.tile(O, (len(u), 1))
        c[np.abs(v - (0.55 * u * u - 0.25)) < 0.2] = M
        return c
    k.decal(MC, unit(0, -1, -0.25), (0.04, 0.018), smile, eps=0.004, rings=8, segs=40)
    k.decal(H + (0.024, -0.178, -0.01), unit(0.2, -0.8, 0.5), (0.007, 0.005), MOUTH, eps=0.004, rings=4, segs=16,
            mirror=True)

    # The eyes: small, dark and kind, a glint up and out, on the sides above the snout
    K, G = srgb(INK), srgb("#FFFFFF")
    def eye(u, v):
        c = np.tile(K, (len(u), 1))
        c[((u - 0.25) / 0.3) ** 2 + ((v - 0.35) / 0.3) ** 2 < 1] = G
        return c
    de = unit(0.58, -0.74, 0.34)
    E = H + de * HR
    def eyes(state):
        k.decal(E, de, (0.022, 0.032), eye_look(state, eye, O, K, line=0.18), eps=0.005, rings=20, segs=56,
                mirror=True)

    k.build(voxel=0.0075, smooth=4)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    dragonite(*args())
