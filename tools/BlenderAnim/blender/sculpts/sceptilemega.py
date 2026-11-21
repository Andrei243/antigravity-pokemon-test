# Mega Sceptile sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sceptilemega.py -- <the export's Sceptile-Mega.glb> <out.glb>
# Sceptile (sceptile.py: its head, slit eyes under the brow, red jaw, neck, pear of a body, haunches, feet and claws,
# arms and three fingers) grown into its Mega, as the two pictures draw it: the largest change is the tail, now long
# and thick, back, low and out to the +X side, banded in red, a fern of dark leaflets along its top and lighter ones
# under it, ending in a great red arrowhead; a fan of long dark pointed leaves spread wide from the shoulders; the
# seeds now in a column down the back, three yellow pairs beside a red stripe that runs from the nape down the spine
# and on along the tail's top, an orange pair low over the hips and a red pair on the tail's root; one long leaf blade
# off each forearm, rising up and back with a red point; the crest swept up into a taller point. Both pictures show it
# from behind with the head turned in profile, so the front (the red jaw and the belly's red band) is Sceptile's.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, args, sibling

SC = sibling("sceptile")
T, G, L = SC.T, SC.G, SC.L
GREEN, RED = SC.GREEN, T.RED
BLADE, FERN, CLAW = SC.BLADE, SC.FERN, SC.CLAW
UNDER = "#4FA64A"                 # the lighter leaflets under the tail
SEED = SC.SEED
ORANGE = "#F2862C"
REDSEED = "#DC4A40"
VOXEL = 0.0068


def mega_tail(k, root, d, length):
    """The tail: a thick stalk from root along d (back, low and out to +X), red bands across it, a fern of dark
    leaflets fanned up along its top and lighter ones down under it, a great red arrowhead at its end."""
    d = unit(*d)
    up = np.array([0, 0, 1.0]); up = up - d * (up @ d); up /= np.linalg.norm(up)
    side = np.cross(d, up)
    root = np.array(root, float)
    stalk = [root + d * length * t + up * 0.03 * math.sin(math.pi * t) for t in np.linspace(0, 1, 6)]
    k.add("tail", tube(stalk, [0.06, 0.053, 0.047, 0.041, 0.035, 0.03]), GREEN, "tail")
    # Red bands across it
    for tb in (0.3, 0.55, 0.8):
        def band(P, tb=tb):
            t = (P - root) @ d / length
            return inside(np.abs(t - tb) - 0.032, 0.004) * inside(np.linalg.norm(P - root - np.outer((P - root) @ d, d),
                                                                                  axis=1) - 0.075, 0.004)
        k.paint(band, RED, on=["tail"])

    # The back's red stripe running on along the tail's top
    def top(P):
        q = P - root
        t = q @ d
        perp = q - np.outer(t, d)
        return inside(np.abs(perp @ side) - 0.022, 0.004) * inside(0.012 - perp @ up, 0.004) * inside(-t, 0.004) \
            * inside(t - length, 0.004)
    k.paint(top, RED, on=["tail"])
    # The fern: leaflets fanned up and back along the top (dark), down and back under it (lighter)
    n = 7
    for i in range(n):
        t = 0.16 + 0.78 * i / (n - 1)
        p = root + d * length * t + up * 0.03 * math.sin(math.pi * t)
        ln = 0.15 + 0.12 * math.sin(math.pi * min(1.0, t * 1.05))
        for j, (ang, lift, col) in enumerate(((58, 0.35, FERN), (40, -0.35, FERN), (-48, 0.3, UNDER), (-62, -0.3, UNDER))):
            a = math.radians(ang)
            dd = math.cos(a) * d + math.sin(a) * up + side * lift * 0.6
            face = side + up * lift * 0.4
            k.add(f"frond{i}{j}", T.leaf_blade(p, dd, face, ln * (1 - 0.15 * abs(lift)), 0.032, 0.032, widest=0.3), col,
                  "tail", fuse=False, surface="leaf")
    # The arrowhead: broad, red, its face tilted toward the cameras
    end = root + d * length
    k.add("arrow", T.leaf_blade(end - d * 0.03, d + up * 0.05, side + up * 0.7, 0.26, 0.11, 0.045, widest=0.2),
          RED, "tail", fuse=False, surface="leaf")
    # The red pair of seeds on the root
    for s in (1, -1):
        c = root + d * 0.15 + up * 0.04 + side * 0.033 * s
        k.add(f"redseed{'R' if s > 0 else 'L'}", ell(c, (0.034, 0.034, 0.034), sub=3), REDSEED, "tail", fuse=False,
              surface="shell")


def sceptilemega(src, out):
    k = Kit(src, coat="scales")

    # The body as Sceptile's: a heavy pear over the haunches, a long neck carrying the head forward
    def pear(V):
        V = V.copy()
        low = np.clip(-V[:, 2], 0, 1)
        V[:, :2] *= (1 + 0.32 * low)[:, None]
        return V
    k.add("torso", ell((0, 0.005, 0.57), (0.12, 0.11, 0.17), deform=pear, rot=(6, 0, 0)), GREEN, "body")
    k.add("hips", ell((0, 0.02, 0.43), (0.165, 0.13, 0.11)), GREEN, "body")
    k.add("neck", tube([(0, -0.015, 0.68), (0, -0.045, 0.76), (0, -0.075, 0.83)], [0.074, 0.066, 0.062]), GREEN, "head")
    G.GREEN = GREEN
    G.head(k, L)
    # The crest: the crown swept up and back into a tall point
    H = L.H
    k.add("crest", blade([H + (0, 0.01, 0.045), H + (0, 0.1, 0.09), H + (0, 0.2, 0.165)], [0.066, 0.046, 0.014],
                         (1, 0, 0), 0.55), GREEN, "head")

    # The legs as Sceptile's
    toes = [(0.55, -1, 0.05), (0.05, -1, 0.05), (-0.45, -1, 0.05)]
    for s, side in ((1, "R"), (-1, "L")):
        T.leg(k, side, s, (0.112, -0.005, 0.27), (0.102, 0.118, 0.205),
              [(0.122, -0.02, 0.1), (0.126, -0.02, 0.06)], [0.062, 0.058],
              (0.128, -0.058, 0.026), (0.058, 0.098, 0.026), toes, 0.05, 0.017, 0.0, color=GREEN)
        base = np.array((0.128, -0.058, 0.026)) + np.array([0, -0.098 * 0.7, -0.026 * 0.15])
        SC.claws(k, side, s, base, toes, 0.05, f"leg{side}")

    # The arms: forward and a little down, three fingers; one long blade off each forearm, up and back, a red point
    for s, side in ((1, "R"), (-1, "L")):
        sh, el, wr = np.array((0.09, -0.02, 0.67)), np.array((0.135, -0.07, 0.59)), np.array((0.125, -0.175, 0.565))
        T.arm(k, side, s, [sh, el, wr], [0.04, 0.035, 0.031], wr + (0, -0.028, 0), (0.034, 0.034, 0.029),
              [(0.5, -0.9, -0.2), (0.05, -1, -0.25), (-0.4, -0.9, -0.2)], 0.04, 0.012, 0.0, color=GREEN)
        m = np.array([s, 1, 1.0])
        root = el + (wr - el) * 0.45 + np.array([0.02, 0.0, 0.01])
        d = unit(0.42, 0.42, 0.8)
        face = np.array((0.3, -1, 0.2))
        k.add(f"armblade{side}", T.leaf_blade(root * m, d * m, face * m, 0.27, 0.036, 0.032, widest=0.3, tip=0.02),
              BLADE, f"arm{side}", fuse=False, surface="leaf")
        k.add(f"armpoint{side}", T.leaf_blade((root + d * 0.25) * m, d * m, face * m, 0.11, 0.025, 0.036, widest=0.15),
              RED, f"arm{side}", fuse=False, surface="leaf")

    # The fan of long dark leaves spread wide behind the shoulders
    for j, (d, ln, w) in enumerate([((0.8, 0.45, 0.42), 0.27, 0.05), ((0.9, 0.42, 0.02), 0.31, 0.052),
                                    ((0.78, 0.5, -0.36), 0.24, 0.046), ((0.45, 0.75, 0.5), 0.22, 0.044)]):
        for s, side in ((1, "R"), (-1, "L")):
            m = np.array([s, 1, 1.0])
            rt = np.array((0.1, 0.045, 0.665))
            k.add(f"backleaf{side}{j}", T.leaf_blade(rt * m, np.array(d) * m, np.array((0.15, 1, 0.35)) * m, ln, w, 0.034,
                                                      widest=0.3), FERN, "body", fuse=False, surface="leaf")

    # The seeds down the back: three yellow pairs beside the stripe, an orange pair low over the hips
    for (x, y, z, r, col) in ((0.046, 0.082, 0.7, 0.032, SEED), (0.051, 0.106, 0.615, 0.033, SEED),
                              (0.055, 0.12, 0.53, 0.034, SEED), (0.06, 0.14, 0.465, 0.035, ORANGE)):
        k.add("seed" if col == SEED else "oseed", ell((x, y, z), (r, r * 0.85, r), sub=3), col, "body", fuse=False,
              mirror=True, surface="shell")

    # The tail
    mega_tail(k, (0.0, 0.07, 0.44), (0.42, 0.85, -0.25), 0.78)

    # The red of the lower jaw and the band across the belly as Sceptile's; a red stripe down the back to the tail
    k.paint(T.red_front(L), RED)
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.026, 0.0045) * inside(P[:, 2] - 0.81, 0.005)
            * inside(0.4 - P[:, 2], 0.005) * (P[:, 1] > 0.03), RED, on=["torso", "hips", "neck"])
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.49) - 0.022, 0.0045) * inside(np.abs(P[:, 0]) - 0.14, 0.0045)
            * (P[:, 1] < -0.02), RED)

    lay, E, de = T.eyes(k, L)
    k.build(voxel=VOXEL, smooth=4, band=0.006)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    sceptilemega(*args())
