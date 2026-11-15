# Charizard sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/charizard.py -- <the export's Charizard.glb> <out.glb>
# Charmeleon grown into an orange dragon: a great pear of a body widest at the belly over heavy thighs, a cream belly
# and tail underside, a long neck rising in an S from a thick root and carrying a small head well forward, its snout
# long with a white fang at each side of the shut mouth, two horns lying back off the back of the skull, small fierce
# eyes under the brow (white, a teal iris, a black pupil). Short arms held in, the forearms forward, three fingers with
# white claws in front of the belly's edge, three white claws a foot, a thick tail curling up on the +X side to a large
# flame. Two great wings off the back: solid plates raised in a V and swept back, a thick orange leading edge with a
# claw at its top, the membrane teal on the front face (three orange struts lying on it) and plain orange behind, its
# trailing edge scalloped. Built by Charmander's and Charmeleon's own functions.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, outward, srgb, inside, unit, eye_look, args, sibling

cm = sibling("charmander")
cl = sibling("charmeleon")
ORANGE, CREAM = "#F08A2C", cm.CREAM
MEMBRANE = "#1F6F86"
TEAL = srgb("#1E7F94")

# The wing: an outline in its own plane (a out along the span, b up) times WING_SCALE, its struts, its plane
WING_ROOT = np.array([0.1, 0.15, 0.655])
WING_EX = unit(0.82, 0.48, 0.2)            # out, swept back, a little up
WING_EZ = unit(0.3, 0.22, 1.0)             # up, leaning out (the V) and back
WING_SCALE = 1.3
WING = [(0.0, 0.03), (0.2, 0.3), (0.46, -0.08), (0.41, -0.06), (0.37, -0.15), (0.31, -0.1), (0.25, -0.19),
        (0.18, -0.12), (0.12, -0.16), (0.06, -0.08), (0.0, -0.07)]
TOP, TIP = (0.2, 0.3), (0.46, -0.08)
STRUTS = [(0.37, -0.15), (0.25, -0.19), (0.12, -0.16)]


def wing_frame():
    ex = WING_EX
    ez = WING_EZ - ex * (WING_EZ @ ex); ez /= np.linalg.norm(ez)
    return ex, ez, np.cross(ex, ez)          # n: out and to the front, the membrane's teal face


def wing_point(a, b, lift=0.0):
    ex, ez, n = wing_frame()
    return WING_ROOT + WING_SCALE * (a * ex + b * ez) + n * lift


def wings(k):
    ex, ez, n = wing_frame()
    P = np.array(WING) * WING_SCALE
    mid = P.mean(axis=0)
    inner = [tuple(mid + (p - mid) * 0.91 + (0.006, 0.0)) for p in P]
    k.add("wingback", slab([tuple(p) for p in P], WING_ROOT, ex, ez, 0.016), ORANGE, "wing{s}", fuse=False,
          mirror=True)
    k.add("wingface", slab(inner, WING_ROOT + n * 0.0095, ex, ez, 0.006), MEMBRANE, "wing{s}", fuse=False,
          mirror=True)
    # The leading edge from the shoulder up to its top and out to the tip
    lead = outward(tube([wing_point(0.0, 0.02), wing_point(0.1, 0.17), wing_point(*TOP), wing_point(0.34, 0.12),
                         wing_point(*TIP)], [0.036, 0.029, 0.024, 0.018, 0.011]))
    k.add("wingedge", lead, ORANGE, "wing{s}", fuse=False, mirror=True)
    # Three struts from the top down to the scallops, lying on the teal face alone (the back stays plain)
    for j, e in enumerate(STRUTS):
        e = np.array(TOP) + (np.array(e) - np.array(TOP)) * 0.94
        st = outward(tube([wing_point(*TOP, lift=0.011), wing_point(*((np.array(TOP) + e) / 2), lift=0.011),
                           wing_point(*e, lift=0.011)], [0.015, 0.013, 0.009]))
        k.add(f"wingstrut{j}", st, ORANGE, "wing{s}", fuse=False, mirror=True)
    # The claw at the wing's top, pointing up and out
    t = wing_point(*TOP)
    k.add("wingclaw", cm.claw(t, unit(0.3, 0.2, 1), 0.05, 0.034, 0.028, up=(0, 1, 0)), ORANGE, "wing{s}",
          fuse=False, mirror=True)


def charizard(src, out):
    k = Kit(src, coat="scales")

    # The head: small, carried forward and up on the long neck, a long snout, two horns lying back off the skull.
    # Everything of the head is placed by its offset from H.
    H, HR = np.array([0.0, -0.15, 1.02]), np.array([0.088, 0.09, 0.078])
    def at(x, y, z):
        return H + np.array([x, y, z])
    k.add("head", ell(H, HR), ORANGE, "head")
    k.add("snout", ell(at(0, -0.095, -0.02), (0.062, 0.1, 0.052), rot=(-6, 0, 0)), ORANGE, "head")
    k.add("jaw", ell(at(0, -0.045, -0.055), (0.07, 0.1, 0.042), rot=(10, 0, 0)), ORANGE, "head")
    k.add("brow", ell(at(0, -0.045, 0.025), (0.075, 0.07, 0.035), rot=(12, 0, 0)), ORANGE, "head")
    for s in (1, -1):
        k.add(f"horn{'R' if s > 0 else 'L'}", cm.claw(at(0.042 * s, 0.065, 0.03), unit(0.25 * s, 1, 0.1), 0.14, 0.042,
                                                      0.036), ORANGE, "head", fuse=False, surface="scales")
    # The neck: a thick root on the shoulders rising, then curving forward to carry the head, tapering
    k.add("neckroot", tube([(0, 0.03, 0.62), (0, 0.035, 0.73), (0, 0.0, 0.83)], [0.115, 0.085, 0.068]), ORANGE, "body")
    k.add("neck", tube([(0, 0.0, 0.83), (0, -0.05, 0.91), (0, -0.11, 0.975)], [0.068, 0.061, 0.058]), ORANGE, "head")

    # The body: a great pear widest at the belly, the chest narrower under the neck, wide hips over heavy thighs
    k.add("chest", ell((0, 0.0, 0.6), (0.17, 0.14, 0.12)), ORANGE, "body")
    k.add("torso", ell((0, 0.0, 0.47), (0.23, 0.19, 0.21)), ORANGE, "body")
    k.add("hips", ell((0, 0.03, 0.35), (0.24, 0.18, 0.14)), ORANGE, "body")
    k.decal((0, -0.19, 0.46), (0, -1, 0), (0.16, 0.21), CREAM, eps=0.003, rings=18, segs=60)

    # The arms: short, the upper arm down along the side, the forearm forward, the hands with three fingers and white
    # claws in front of the belly's edge
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"arm{side}", tube(np.array([(0.15, -0.04, 0.65), (0.228, -0.145, 0.6), (0.205, -0.24, 0.55)]) * m,
                                 [0.05, 0.044, 0.038]), ORANGE, f"arm{side}")
        HC = np.array((0.198, -0.272, 0.536))
        k.add(f"hand{side}", ell(HC * m, (0.038, 0.036, 0.032)), ORANGE, f"arm{side}")
        for j, d in enumerate([(0.35, -0.9, -0.15), (0.0, -1, -0.3), (-0.35, -0.9, -0.15)]):
            d = unit(*d)
            b = HC + d * 0.03
            k.add(f"finger{side}{j}", tube(np.array([b, b + d * 0.03]) * m, [0.015, 0.012]), ORANGE, f"arm{side}")
            tip = b + d * 0.038
            k.add(f"claw{side}{j}", cm.claw(tip * m, np.array(d) * m, 0.026, 0.017, 0.014), cm.CLAW, f"arm{side}",
                  fuse=False, surface="shell")

    # The legs: heavy thighs fused into the hips, short shins, broad feet with three white claws
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array((0.19, -0.01, 0.3)) * m, (0.13, 0.15, 0.16), rot=(-12, 0, 0)), ORANGE,
              f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.2, -0.02, 0.18), (0.2, -0.015, 0.07)]) * m, [0.076, 0.068]), ORANGE,
              f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.2, -0.06, 0.038)) * m, (0.078, 0.112, 0.04)), ORANGE, f"leg{side}")
        for j, dx in enumerate((-0.036, 0.0, 0.036)):
            b = np.array((0.2 + dx, -0.152, 0.026))
            k.add(f"toe{side}{j}", cm.claw(b * m, np.array((dx * 0.6, -1, -0.35)) * m, 0.042, 0.028, 0.022),
                  cm.CLAW, f"leg{side}", fuse=False, surface="shell")

    # The tail: thick at the hips, back low and curling up on the +X side to its flame, cream beneath
    TAIL = [(0, 0.1, 0.36), (0.02, 0.27, 0.2), (0.09, 0.42, 0.14), (0.18, 0.5, 0.19), (0.22, 0.53, 0.28)]
    k.add("tail", tube(TAIL, [0.12, 0.085, 0.057, 0.041, 0.032]), ORANGE, "tail")
    k.paint(cl.tail_underside(TAIL, below=0.016, start=0.22), CREAM)
    cm.flame(k, (0.22, 0.53, 0.29), 1.35)

    wings(k)

    # The mouth shut: a dark line along each side of the snout, a white fang down from the lip at its front
    for s in (1, -1):
        k.decal(at(0.048 * s, -0.115, -0.043), unit(0.85 * s, -0.4, -0.2), (0.055, 0.005), "#5A1A16", up=(0, 0, 1),
                eps=0.004, rings=4, segs=32)
        k.add(f"fang{'R' if s > 0 else 'L'}", cm.claw(at(0.04 * s, -0.16, -0.038), (0, -0.2, -1), 0.026, 0.016, 0.012,
                                                      up=(0, 1, 0)), cm.CLAW, "head", fuse=False, surface="shell")
    k.decal(at(0.02, -0.185, 0.003), unit(0.2, -0.6, 0.8), (0.007, 0.0055), cm.NOSTRIL, eps=0.004, rings=4, segs=16,
            mirror=True)

    # The eyes: small and fierce under the brow, sloping down toward the snout
    cut = cl.brow_cut(0.32, 0.45)
    de = unit(0.68, -0.68, 0.26)
    E = cm.on_ell(H, HR, de)
    eyes = cl.eyes_under_brow(k, E, de, (0.026, 0.031), cl.fierce_eye(TEAL, cut), cut, srgb(ORANGE))

    k.build(voxel=0.008, smooth=4)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    charizard(*args())
