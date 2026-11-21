# Mega Dragonite sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dragonitemega.py -- <the export's Dragonite-Mega.glb> <out.glb>
# Dragonite (dragonite.py, whose helpers are imported; its body is copied here, since its function takes no arguments)
# grown heavier: the body a fuller pear with a rounded back, the cream belly crossed by slanting lines into plates. Its
# largest change is above its head: its two feelers have grown into great cream wings of feathers arching up from the
# top of its head and out, then drooping, their feathers hanging in a fringe tipped with white. Its small teal wings
# stay on its back; a cream tuft of feathers has grown on each upper arm; small white spikes stand on its back and the
# root of its tail; its horn is white; its eyes are teal. The tail sweeps round to the +X side and forward along the
# ground to a pearl, a small spike on it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, outward, axes, srgb, inside, unit, eye_look, args, sibling

dn = sibling("dragonite")
ORANGE, CREAM, LINE, CLAW, INK, MOUTH = dn.ORANGE, dn.CREAM, "#B88550", dn.CLAW, dn.INK, dn.MOUTH
FEATHER, TIPW, PEARL, PEARL2, EYE = "#F1E5C4", "#FFFFFF", "#5BA89E", "#B9A0D8", "#1E9C94"


def along(shape, base, d):
    """A shape made along +z (its root at the origin) laid along d from base, its frame right-handed."""
    V, F = shape
    a, b = axes(d)
    d = unit(*d)
    return outward((np.array(base, float) + V[:, :1] * a + V[:, 1:2] * b + V[:, 2:3] * d, F))


def feather(base, d, length, width, thick=0.024):
    """A flat feather from base along d, flat across the wing (its thin side along the axes' first direction)."""
    def shape(V):
        V = V.copy()
        t = (V[:, 2] + 1) / 2
        V[:, 1] *= 0.55 + 0.6 * np.sqrt(np.clip(t, 0, 1)) * (1.15 - 0.3 * t)
        return V
    return along(ell((0, 0, length / 2), (thick, width, length / 2), deform=shape, sub=3), base, d)


def wings_on_head(k, H):
    """The two great feathered wings: an arch from the top of the head up and out, drooping, a fringe below it."""
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        m = np.array([s, 1, 1.0])
        arch = np.array([(0.03, 0.03, 0.935), (0.1, 0.05, 1.07), (0.24, 0.08, 1.16), (0.38, 0.1, 1.13),
                         (0.48, 0.11, 1.02), (0.53, 0.12, 0.9)])
        k.add(f"plumearch{side}", outward(tube(arch * m, [0.04, 0.052, 0.058, 0.052, 0.042, 0.03])), FEATHER, "head",
              fuse=False, surface="fur")
        # The covert on top of the arch: broad plates laid along it, overlapping
        fine = np.vstack([arch[i] + (arch[i + 1] - arch[i]) * t for i in range(len(arch) - 1)
                          for t in np.linspace(0, 1, 10, endpoint=False)])
        # Coverts lying along the top of the arch, out and a little up, broadening it
        for j, i in enumerate([6, 14, 22, 30, 38]):
            t = fine[i + 3] - fine[i - 3]; t /= np.linalg.norm(t)
            d = unit(*(t + np.array((0, 0, 0.25))))
            k.add(f"covert{side}{j}", feather(fine[i] * m + (0, 0, 0.02), np.array(d) * m, 0.16, 0.07, thick=0.03),
                  FEATHER, "head", fuse=False, surface="fur")
        hang = [(10, 0.15, 0.07), (15, 0.2, 0.08), (20, 0.25, 0.085), (25, 0.28, 0.085), (30, 0.29, 0.082),
                (35, 0.27, 0.078), (40, 0.23, 0.07), (45, 0.18, 0.06)]
        for j, (i, L, w) in enumerate(hang):
            p = fine[i]
            out = 0.15 + 0.6 * i / 49
            d = unit(out * s, 0.12, -1)
            k.add(f"plume{side}{j}", feather(p * m, d, L, w), FEATHER, "head", fuse=False, surface="fur")
            tip = p * m + np.array(d) * (L * 0.82)
            k.add(f"plumetip{side}{j}", feather(tip, d, L * 0.24, w * 0.55, thick=0.028), TIPW, "head", fuse=False,
                  surface="fur")


def dragonite_mega(src, out):
    k = Kit(src, coat="scales")

    # The body: a fuller pear than Dragonite's, widest low at the belly, its back rounded
    k.add("torso", ell((0, 0.0, 0.42), (0.24, 0.225, 0.265)), ORANGE, "body")
    k.add("back", ell((0, 0.07, 0.55), (0.2, 0.17, 0.17)), ORANGE, "body")
    k.add("chest", ell((0, -0.01, 0.6), (0.17, 0.155, 0.14)), ORANGE, "body")
    k.add("neck", tube([(0, 0.01, 0.64), (0, -0.01, 0.73), (0, -0.04, 0.8)], [0.108, 0.09, 0.08]), ORANGE, "head")

    # The head: Dragonite's, a tenth larger, a white horn on top
    H, HR = np.array([0.0, -0.06, 0.855]), np.array([0.097, 0.1, 0.09]) * 1.1
    k.add("skull", ell(H, HR), ORANGE, "head")
    k.add("snout", ell(H + (0, -0.1, -0.035), (0.083, 0.088, 0.068), rot=(-6, 0, 0)), ORANGE, "head")
    k.add("horn", dn.cone(H + (0, -0.02, 0.075), unit(0, 0.3, 1), 0.07, 0.026), "#F7F2E4", "head", fuse=False,
          surface="shell")

    # The belly: cream from the throat down between the legs, slanting lines cutting it into plates
    BC, BH = np.array([0.0, -0.13, 0.47]), (0.16, 0.275)
    k.decal(BC, unit(0, -1, 0.15), BH, CREAM, eps=0.003, rings=24, segs=72)
    for j, z in enumerate(np.linspace(BC[2] - 0.2, BC[2] + 0.18, 6)):
        w = BH[0] * math.sqrt(max(0.0, 1 - ((z - BC[2]) / BH[1]) ** 2)) * 0.9
        a = math.radians(14 if j % 2 else -14)
        k.decal((0, -0.13, z), (0, -1, 0), (w, 0.0045), LINE, up=(math.sin(a), 0, math.cos(a)), eps=0.0055, rings=4,
                segs=48)

    # The arms: short, forward and a little down, three white claws a hand, a cream tuft on the upper arm
    k.add("arm", tube([(0.15, -0.03, 0.63), (0.22, -0.09, 0.575), (0.23, -0.17, 0.535)], [0.062, 0.054, 0.047]),
          ORANGE, "arm{s}", mirror=True)
    HC = np.array((0.23, -0.21, 0.53))
    k.add("hand", ell(HC, (0.048, 0.045, 0.043)), ORANGE, "arm{s}", mirror=True)
    for j, d in enumerate([(0.45, -0.8, 0.1), (0.05, -0.95, 0.0), (-0.35, -0.85, 0.05)]):
        d = unit(*d)
        k.add(f"handclaw{j}", dn.claw(HC + d * 0.038, d, 0.036, 0.013), CLAW, "arm{s}", fuse=False, mirror=True,
              surface="shell")
    for j, (b, d, L, w) in enumerate([((0.2, -0.02, 0.62), (0.7, 0.6, 0.35), 0.13, 0.045),
                                      ((0.215, -0.03, 0.59), (0.8, 0.55, 0.05), 0.11, 0.04)]):
        k.add(f"armtuft{j}", feather(b, unit(*d), L, w, thick=0.022), FEATHER, "arm{s}", fuse=False, mirror=True,
              surface="fur")

    # The legs: heavy thighs fused into the hips, short shins, broad feet with three white claws
    k.add("thigh", ell((0.145, -0.02, 0.24), (0.125, 0.135, 0.15), rot=(-10, 0, 0)), ORANGE, "leg{s}", mirror=True)
    k.add("shin", tube([(0.155, -0.035, 0.14), (0.16, -0.04, 0.06)], [0.08, 0.072]), ORANGE, "leg{s}", mirror=True)
    k.add("foot", ell((0.16, -0.08, 0.036), (0.082, 0.115, 0.038)), ORANGE, "leg{s}", mirror=True)
    for j, dx in enumerate((-0.04, 0.0, 0.04)):
        b = np.array((0.16 + dx, -0.178, 0.024))
        k.add(f"toe{j}", dn.claw(b, (dx * 0.5, -1, -0.15), 0.038, 0.017), CLAW, "leg{s}", fuse=False, mirror=True,
              surface="shell")

    # The tail: thick, out back from the hips and round to the +X side, forward along the ground to its pearl
    TAIL = [(0.0, 0.12, 0.32), (0.06, 0.3, 0.19), (0.22, 0.37, 0.1), (0.36, 0.24, 0.07), (0.41, 0.06, 0.07)]
    TR = [0.135, 0.1, 0.075, 0.056, 0.045]
    k.add("tail", tube(TAIL, TR), ORANGE, "tail")
    k.paint(dn.tail_under(TAIL, TR), CREAM, on=["tail"])
    PC = np.array((0.415, -0.02, 0.075))
    k.add("pearl", ell(PC, (0.07, 0.07, 0.07), sub=4), PEARL, "tail", fuse=False, surface="shell")
    k.add("pearlsheen", ell(PC + (0.012, -0.018, 0.022), (0.05, 0.05, 0.05), sub=3), PEARL2, "tail", fuse=False,
          surface="shell")
    k.add("pearlglint", ell(PC + (-0.02, -0.045, 0.04), (0.016, 0.012, 0.014), sub=2), "#FFFFFF", "tail", fuse=False,
          surface="shell")
    k.add("tailtip", dn.cone(np.array(TAIL[-1]) + (0.0, 0.02, 0.03), unit(0.2, -0.3, 1), 0.06, 0.02), "#F7F2E4",
          "tail", fuse=False, surface="shell")

    # White spikes: three on each side of the back, three along the root of the tail
    for j, (b, d, L) in enumerate([((0.17, 0.12, 0.58), (0.6, 0.6, 0.3), 0.06), ((0.2, 0.1, 0.5), (0.7, 0.6, 0.1), 0.055),
                                   ((0.21, 0.08, 0.42), (0.75, 0.55, -0.1), 0.05)]):
        k.add(f"backspike{j}", dn.cone(b, unit(*d), L, 0.02), CLAW, "body", fuse=False, mirror=True, surface="shell")
    for j, (b, L) in enumerate([((0.0, 0.25, 0.34), 0.065), ((0.03, 0.33, 0.27), 0.06), ((0.07, 0.4, 0.21), 0.05)]):
        k.add(f"tailspike{j}", dn.cone(b, unit(0.1, 0.6, 1), L, 0.021), CLAW, "tail", fuse=False, surface="shell")

    dn.wings(k)
    wings_on_head(k, H)

    # The face: a little smile and two nostrils on the snout
    MC = H + np.array((0, -0.18, -0.066))
    O, M = srgb(ORANGE), srgb(MOUTH)
    def smile(u, v):
        c = np.tile(O, (len(u), 1))
        c[np.abs(v - (0.55 * u * u - 0.25)) < 0.2] = M
        return c
    k.decal(MC, unit(0, -1, -0.25), (0.044, 0.019), smile, eps=0.004, rings=8, segs=40)
    k.decal(H + (0.026, -0.188, -0.012), unit(0.2, -0.8, 0.5), (0.0075, 0.0055), MOUTH, eps=0.004, rings=4, segs=16,
            mirror=True)

    # The eyes: teal with a dark pupil and a white glint, on the sides above the snout
    K, G, T = srgb(INK), srgb("#FFFFFF"), srgb(EYE)
    def eye(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(K, (len(u), 1))
        c[r < 0.8] = T
        c[((u + 0.05) / 0.36) ** 2 + ((v + 0.05) / 0.46) ** 2 < 1] = K
        c[((u - 0.25) / 0.26) ** 2 + ((v - 0.38) / 0.26) ** 2 < 1] = G
        return c
    de = unit(0.58, -0.74, 0.34)
    E = H + de * HR
    def eyes(state):
        k.decal(E, de, (0.025, 0.036), eye_look(state, eye, O, K, line=0.18), eps=0.005, rings=20, segs=56,
                mirror=True)

    k.build(voxel=0.0075, smooth=4)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    dragonite_mega(*args())
