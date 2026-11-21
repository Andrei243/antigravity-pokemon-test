# Skeledirge sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/skeledirge.py -- <the export's Skeledirge.glb> <out.glb>
# Fuecoco and Crocalor grown into a great red crocodile carried level on four short splayed legs (built by
# fuecoco.py's functions: the line's red and cream, its dome and jaws, claws and limbs): a huge white head like a skull
# leaning forward and low, its long snout shut, a red stripe down the snout's top set with purple marks, the jaw's line
# a zigzag of red teeth over a purple band; yellow eyes with black slits in black sockets at the back of the head, a
# red point over each and a purple band over the crown; two great plumes of orange flame with yellow blobs off the
# sides of the head behind the eyes; a small yellow bird of flame with a dark ringed eye perched on the snout's tip;
# a long red body with a pale ribbed belly, a black band along each flank and a pale grey lattice over the back; white
# bands round the legs, black claws; a long thick tail sweeping back and out to +X and rising to a white curled tip.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from types import SimpleNamespace
import numpy as np
from sculptkit import (Kit, ell, tube, hull, lathe, ring_band, srgb, inside, smoothstep, unit, polygon, eye_look, tri,
                       args, sibling)

F = sibling("fuecoco")
RED, BONE = "#D8432F", "#F2EEEA"
PURPLE = "#7A3C9A"
BLACK = "#24222A"
BELLY, RIB = "#E9E6E6", "#BDB7BC"
LATTICE, LATTICE_LINE = "#E4DEE2", "#B8B0B6"
ORANGE, GLOW_ORANGE = "#F7862A", "#FF8A2A"
GOLD, GLOW_GOLD = "#FFD640", "#FFD640"
EYE_YELLOW = srgb("#F2D23A")
VOXEL = 0.0068

L = SimpleNamespace(**vars(F.L))
# The skull is half the creature: long, deep, flat-topped, as wide as the body, carried low and level
L.H, L.HR, L.dome_box = np.array([0.0, -0.22, 0.28]), np.array([0.18, 0.16, 0.14]), 2.6
L.dome_col = BONE
L.hinge = (0.0, -0.3, 0.175)
L.gape_up, L.gape_down = 1, 2
L.upper_len, L.upper_r, L.upper_tip, L.upper_col, L.jaw_box = 0.45, (0.165, 0, 0.08), 0.8, BONE, 3.0
L.lower_len, L.lower_r, L.lower_tip, L.lower_col = 0.41, (0.145, 0, 0.052), 0.8, BONE
L.open = False
L.eye_dir, L.eye_shift, L.eye_h = (0.8, -0.34, 0.46), (0, -0.004, 0), (0.043, 0.04)
L.lid, L.glint = BLACK, (-0.25, 0.35, 0.22)


def eye_open():
    """The open eye: yellow with an upright black slit, a dark rim."""
    def look(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(EYE_YELLOW, (len(u), 1))
        c[np.abs(u + 0.05) < 0.2 * np.sqrt(np.clip(1 - v * v, 0, 1))] = F.INK
        c[r >= 0.84] = F.INK
        return c
    return look


L.eye_look = eye_open()


def teardrop(V):
    """The unit sphere made a flame's tongue: round below, pointed on top (local +z)."""
    V = V.copy()
    t = np.clip(V[:, 2], -1, 1)
    w = np.sqrt(np.clip(1 + t, 0, None)) * np.clip(1 - t, 0, None) ** 1.2 / 1.16
    V[:, 0] *= w; V[:, 1] *= w
    return V


def billow(V):
    """The unit sphere made a billow of flame: full and round, drawn to a soft point on top (local +z)."""
    V = V.copy()
    t = np.clip(V[:, 2], -1, 1)
    w = 1 - 0.55 * np.clip(t, 0, 1) ** 2
    V[:, 0] *= w; V[:, 1] *= w
    V[:, 2] = np.where(t > 0, t * (1 + 0.25 * t), t)
    return V


def plume(k, side, s, root, sc=1.0):
    """A plume of flame off the side of the head: tongues rising up and out and back from a round foot, yellow blobs
    standing out of its faces."""
    root = np.array(root, float)
    m = np.array([s, 1, 1.0])
    # A broad billow: a round foot and four thick rounded lobes leaning back and out, each only a little pointed
    tongues = [((0.0, 0.0, 0.0), (0.085, 0.08, 0.075), (0, 0, 0), False),
               ((0.03, 0.04, 0.09), (0.068, 0.06, 0.085), (-35, 30, 0), True),
               ((0.09, 0.0, 0.05), (0.06, 0.055, 0.075), (-15, 60, 0), True),
               ((-0.01, 0.1, 0.06), (0.062, 0.056, 0.075), (-65, 20, 0), True),
               ((0.08, 0.09, 0.1), (0.05, 0.046, 0.065), (-45, 50, 0), True)]
    shapes = []
    for (o, r, rot, pointed) in tongues:
        c = (root + np.array(o) * sc) * m
        shapes.append(ell(c, np.array(r) * sc, rot=(rot[0], rot[1] * s, 0), deform=billow if pointed else None, sub=3))
    k.add(f"plume{side}", F.join(*shapes), ORANGE, "head", fuse=False, glow=GLOW_ORANGE)
    blobs = []
    for o, r in [((0.07, -0.03, 0.02), 0.04), ((0.035, 0.05, 0.11), 0.034), ((0.1, 0.04, 0.07), 0.03),
                 ((0.0, 0.11, 0.08), 0.03)]:
        c = (root + np.array(o) * sc) * m
        blobs.append(ell(c, (r * sc, r * sc * 0.8, r * sc), sub=2))
    k.add(f"plumegold{side}", F.join(*blobs), GOLD, "head", fuse=False, glow=GLOW_GOLD)


def bird(k, at, sc=2.0):
    """The small bird of flame on the snout: a yellow ball, a dark ring of an eye on its front, a tuft of flame."""
    at = np.array(at, float)
    q = sc
    k.add("bird", ell(at, (0.03 * q, 0.028 * q, 0.03 * q), sub=3), GOLD, "head", fuse=False, glow=GLOW_GOLD)
    d = unit(0.3, -1, 0.15)
    k.add("birdring", lathe(at + d * 0.026 * q, d, [(0.0, -0.004 * q), (0.017 * q, -0.004 * q), (0.017 * q, 0.004 * q),
                                                   (0.0, 0.004 * q)]), BLACK, "head", fuse=False)
    k.add("birdeye", lathe(at + d * 0.03 * q, d, [(0.0, -0.003 * q), (0.0095 * q, -0.003 * q), (0.0095 * q, 0.003 * q),
                                                 (0.0, 0.003 * q)]), GOLD, "head", fuse=False, glow=GLOW_GOLD)
    tuft = F.join(ell(at + np.array((0.0, 0.01, 0.045)) * q, np.array((0.016, 0.014, 0.03)) * q, rot=(-20, 0, 0),
                      deform=teardrop, sub=3),
                  ell(at + np.array((0.0, 0.03, 0.03)) * q, np.array((0.012, 0.011, 0.024)) * q, rot=(-55, 0, 0),
                      deform=teardrop, sub=3))
    k.add("birdflame", tuft, ORANGE, "head", fuse=False, glow=GLOW_ORANGE)


def skeledirge(src, out):
    k = Kit(src, coat="scales")

    # The body: long and level, low over the legs, a thick neck into the head
    # (a low wedge behind the great head: deepest at the shoulders, falling toward the rump)
    k.add("torso", ell((0, 0.08, 0.17), (0.18, 0.22, 0.108)), RED, "body")
    k.add("chest", ell((0, -0.06, 0.185), (0.175, 0.12, 0.118)), RED, "body")
    k.add("neck", ell((0, -0.13, 0.21), (0.165, 0.11, 0.12)), RED, "head")
    k.add("rump", ell((0, 0.24, 0.155), (0.15, 0.1, 0.095)), RED, "body")

    # The legs: short, splayed, haunches fused into the flanks; broad feet with four black claws; a white band
    claws = [(-0.036, 0.024, 0.016, (-0.3, -1, -0.25)), (-0.012, 0.026, 0.017, (-0.1, -1, -0.25)),
             (0.012, 0.026, 0.017, (0.1, -1, -0.25)), (0.036, 0.024, 0.016, (0.3, -1, -0.25))]
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        for name, y, bone in (("fore", -0.06, f"leg{side}"), ("hind", 0.2, f"hind{side}")):
            k.add(f"{name}haunch{side}", ell(np.array((0.15, y, 0.125)) * m, (0.085, 0.09, 0.075)), RED, bone)
            k.add(f"{name}shin{side}", tube(np.array([(0.18, y, 0.11), (0.235, y - 0.01, 0.07), (0.25, y - 0.02, 0.05)]) * m,
                                            [0.058, 0.052, 0.044]), RED, bone)
            k.add(f"{name}foot{side}", ell(np.array((0.255, y - 0.04, 0.019)) * m, (0.056, 0.068, 0.021)), RED, bone)
            for j, (dx, ln, w, d) in enumerate(claws):
                base = np.array((0.255 + dx, y - 0.04 - 0.056, 0.019))
                k.add(f"{name}claw{side}{j}", F.fang(base * m, np.array(unit(*d)) * m, ln, w, w * 0.8), BLACK, bone,
                      fuse=False, surface="shell")
            k.add(f"{name}band{side}", ring_band(np.array((0.212, y - 0.006, 0.087)) * m,
                                                 np.array((0.055, -0.01, -0.04)) * m, 0.054, 0.024), BONE, bone,
                  fuse=False, surface="scales")

    # The tail: thick at the rump, back and out to +X, rising to a white curled tip
    TAIL = np.array([(0, 0.28, 0.16), (0.04, 0.39, 0.12), (0.1, 0.47, 0.12), (0.15, 0.52, 0.17), (0.17, 0.53, 0.25)])
    TAIL_R = [0.1, 0.078, 0.06, 0.046, 0.036]
    k.add("tail", tube(TAIL, TAIL_R), RED, "tail")
    k.add("tailtip", tube([(0.17, 0.53, 0.24), (0.17, 0.53, 0.32), (0.148, 0.52, 0.38), (0.115, 0.505, 0.4)],
                          [0.034, 0.026, 0.018, 0.012]), BONE, "tail")

    # The pale ribbed belly, the black band along each flank, the lattice over the back
    def belly(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        return inside(z - 0.115, 0.003) * inside(y - 0.3, 0.004) * inside(-0.17 - y, 0.004)
    k.paint(belly, BELLY, on=["torso", "chest", "rump"])
    def ribs(P):
        y = P[:, 1]
        return belly(P) * inside(np.abs(((y + 0.2) % 0.06) - 0.03) - 0.0035, 0.002)
    k.paint(ribs, RIB, on=["torso", "chest", "rump"])
    def flank(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        return inside(np.abs(z - 0.14) - 0.02, 0.003) * inside(np.abs(y - 0.06) - 0.1, 0.004) * (np.abs(x) > 0.08)
    k.paint(flank, BLACK, on=["torso", "chest"])
    def lattice(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        return inside(0.235 - z, 0.003) * inside(np.abs(y - 0.08) - 0.15, 0.004)
    k.paint(lattice, LATTICE, on=["torso"])
    def lattice_lines(P):
        x, y = P[:, 0], P[:, 1]
        a = inside(np.abs(((y + x * 0.9) % 0.07) - 0.035) - 0.0035, 0.002)
        b = inside(np.abs(((y - x * 0.9) % 0.07) - 0.035) - 0.0035, 0.002)
        return lattice(P) * np.maximum(a, b)
    k.paint(lattice_lines, LATTICE_LINE, on=["torso"])

    # The head: a white skull, the long snout shut
    J = F.head(k, L)
    hinge = J.hinge

    def mouthline(P):
        rel = P - hinge
        return -rel[:, 1], rel[:, 2]                 # forward of the hinge, height over the jaw's line
    def onhead(P):
        return inside(np.abs(P[:, 0]) - 0.21, 0.004)
    # The purple band along the jaw's line and red teeth biting down into it in a zigzag
    def jawband(P):
        fwd, dz = mouthline(P)
        return onhead(P) * inside(np.abs(dz + 0.008) - 0.032, 0.0025) * (fwd > -0.02) * (np.abs(P[:, 0]) > 0.03)
    k.paint(jawband, PURPLE, on=["upperjaw", "lowerjaw", "dome"])
    def redteeth(P):
        fwd, dz = mouthline(P)
        edge = 0.0 + 0.036 * (0.5 + 0.5 * tri(fwd / 0.08))
        return onhead(P) * inside(-0.008 + edge - dz, 0.002) * inside(dz - 0.058, 0.0025) * (fwd > -0.02) \
            * (np.abs(P[:, 0]) > 0.025)
    k.paint(redteeth, RED, on=["upperjaw", "lowerjaw", "dome"])
    # The red stripe down the snout's top, with purple marks along it
    def stripe(P):
        fwd, dz = mouthline(P)
        return inside(np.abs(P[:, 0]) - (0.05 - 0.012 * fwd), 0.002) * inside(0.07 - dz, 0.003) * (fwd > 0.04) \
            * (fwd < 0.42)
    k.paint(stripe, RED, on=["upperjaw"])
    def marks(P):
        fwd, dz = mouthline(P)
        t = (fwd - 0.08) / 0.08
        return stripe(P) * inside(np.abs(t - np.round(t)) * 0.08 - 0.018, 0.002) * (fwd < 0.36) \
            * inside(np.abs(P[:, 0]) - 0.03, 0.002)
    k.paint(marks, PURPLE, on=["upperjaw"])
    # The purple band over the crown
    def crownband(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        return inside(np.abs(x) - 0.06, 0.002) * inside(np.abs(y + 0.22) - 0.12, 0.003) * inside(0.38 - z, 0.003)
    k.paint(crownband, PURPLE, on=["dome"])

    # The black sockets, and a red point over each eye
    E, de, right, up = F.eye_frame(L)
    k.decal(E + up * 0.002, de, (0.071, 0.065), BLACK, eps=0.0012, rings=12, segs=48, mirror=True)
    k.decal(E + up * 0.085, de + np.array([0, 0, 0.5]), (0.04, 0.04), RED, eps=0.0012, rings=8, segs=40,
            mirror=True, outline=polygon([(-0.9, -0.8), (0.9, -0.8), (0.0, 1.0)]))

    # The plumes of flame off the sides of the head, broad billows leaning back and out; the bird on the snout's tip
    for s, side in ((1, "R"), (-1, "L")):
        plume(k, side, s, (0.175, -0.1, 0.32), 1.28)
    tip = J.hinge + J.du * L.upper_len * 0.86 + J.upu * L.upper_r[2] * 1.05
    bird(k, tip + np.array([0, 0, 0.055]), 2.0)

    # The eyes: as the line's, but a shut eye's line is pale, since the lid is the black socket
    lid, pale = srgb(L.lid), srgb("#E6DCD6")
    def lay(state):
        fn = eye_look(state, L.eye_look, lid, F.INK if state in ("Open", "Fierce") else pale, line=0.17)
        k.decal(E, de, L.eye_h, fn, eps=0.0018, rings=20, segs=56, mirror=True)
        if state == "Open":
            g = E + right * L.glint[0] * L.eye_h[0] + up * L.glint[1] * L.eye_h[1]
            k.decal(g, de, (L.eye_h[0] * L.glint[2], L.eye_h[0] * L.glint[2]), "#FFFFFF", eps=0.0032, rings=5,
                    segs=20, mirror=True)
    k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.06)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    skeledirge(*args())
