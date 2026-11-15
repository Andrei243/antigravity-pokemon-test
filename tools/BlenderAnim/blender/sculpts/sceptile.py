# Sceptile sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sceptile.py -- <the export's Sceptile.glb> <out.glb>
# Grovyle grown tall and heavy (built by treecko.py's and grovyle.py's functions: the line's green and red, its slit
# eyes under the scowling brow, its long head and red jaw, digits, legs, arms and thick leaves): the head long with a
# pointed snout and its crown running back into a pointed crest, the red of the lower jaw only; a long neck; a heavy
# pear of a body, wide in the belly, a red band across its front; four yellow seed balls on the back, a pair high
# behind the shoulders and a pair lower down; arms forward and a little down, three fingers, two pointed leaf blades
# standing up and back off each forearm; great haunches fused into the hips, short thick shins, feet of three toes
# with pale claws; a tail like a fern, a dark green fan of pointed leaflets low behind it and out to the +X side.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from types import SimpleNamespace
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, args, sibling

T = sibling("treecko")
G = sibling("grovyle")
GREEN, RED = "#5EBE52", T.RED
BLADE = "#3E9C44"
FERN = "#2F7A34"
SEED = "#F2C83C"
CLAW = "#E6EEC8"
VOXEL = 0.0068

L = SimpleNamespace(**vars(G.L))
L.H, L.HR = np.array([0.0, -0.13, 0.88]), np.array([0.108, 0.167, 0.09])
L.eye_dir, L.eye_shift, L.eye_h = (0.82, -0.42, 0.38), (0, 0, 0.0), (0.043, 0.033)
L.eye_outline = T.brow_cut(0.4, 0.34)
L.eye_look = T.slit_eye(L.eye_outline, rim=0.78)
L.lid = GREEN
L.red = [(0.808, 0.0), (0.815, 0.055), (0.84, 0.075)]
L.red_axis = [(0.7, -0.05), (0.9, -0.07)]
L.smile = (0.848, 0.012, 1.45)


def claws(k, side, s, base, dirs, length, bone):
    """Pale claws at the toes' ends."""
    m = np.array([s, 1, 1.0])
    for j, d in enumerate(dirs):
        d = unit(*d)
        b = np.array(base, float) + d * length
        k.add(f"claw{side}{j}", T.leaf_blade(b * m, (d + np.array([0, 0, -0.35])) * m, (0, 0, 1), 0.03, 0.011, 0.016,
                                              widest=0.2), CLAW, bone, fuse=False, surface="shell")


def fern(k, root, d, length, n, col):
    """The tail: a stalk from root along d (back, low and out to +X) with pointed leaflets fanned up and down along it in
    the plane of d and up, their faces to the side, the outer ones longest; one leaflet on from its end."""
    d = unit(*d)
    up = np.array([0, 0, 1.0]); up = up - d * (up @ d); up /= np.linalg.norm(up)
    side = np.cross(d, up)
    root = np.array(root, float)
    stalk = [root + d * length * t + up * 0.02 * math.sin(math.pi * t) for t in np.linspace(0, 1, 5)]
    k.add("tail", tube(stalk, [0.05, 0.04, 0.032, 0.024, 0.018]), GREEN, "tail")
    for i in range(n):
        t = 0.18 + 0.8 * i / (n - 1)
        p = root + d * length * t
        ln = 0.13 + 0.12 * math.sin(math.pi * min(1.0, t * 1.1))
        for j, (ang, lift) in enumerate(((60, 0.0), (-50, 0.0), (35, 0.5), (-25, -0.5))):
            a = math.radians(ang - 15 * t)
            dd = math.cos(a) * d + math.sin(a) * up + side * lift * 0.5
            face = side - dd * 0.0 + up * lift * 0.3
            k.add(f"frond{i}{j}", T.leaf_blade(p, dd, face, ln * (1 - 0.18 * abs(lift)), 0.03, 0.03, widest=0.3), col,
                  "tail", fuse=False, surface="leaf")
    end = root + d * length
    k.add("frondtip", T.leaf_blade(end, d + up * 0.15, side, 0.17, 0.035, 0.03), col, "tail", fuse=False,
          surface="leaf")


def sceptile(src, out):
    k = Kit(src, coat="scales")

    # The body: a heavy pear, wide and low in the belly over the haunches, a long neck carrying the head forward
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
    # The crest: the crown running back and up into a point
    H = L.H
    k.add("crest", blade([H + (0, -0.02, 0.035), H + (0, 0.1, 0.06), H + (0, 0.21, 0.085)], [0.065, 0.042, 0.013],
                         (1, 0, 0), 0.55), GREEN, "head")

    # The legs: great haunches fused into the hips, short thick shins, feet of three toes with pale claws
    toes = [(0.55, -1, 0.05), (0.05, -1, 0.05), (-0.45, -1, 0.05)]
    for s, side in ((1, "R"), (-1, "L")):
        T.leg(k, side, s, (0.112, -0.005, 0.27), (0.102, 0.118, 0.205),
              [(0.122, -0.02, 0.1), (0.126, -0.02, 0.06)], [0.062, 0.058],
              (0.128, -0.058, 0.026), (0.058, 0.098, 0.026), toes, 0.05, 0.017, 0.0, color=GREEN)
        base = np.array((0.128, -0.058, 0.026)) + np.array([0, -0.098 * 0.7, -0.026 * 0.15])
        claws(k, side, s, base, toes, 0.05, f"leg{side}")

    # The arms: forward and a little down, three fingers; two pointed blades up and back off each forearm
    for s, side in ((1, "R"), (-1, "L")):
        sh, el, wr = np.array((0.09, -0.02, 0.67)), np.array((0.135, -0.07, 0.59)), np.array((0.125, -0.175, 0.565))
        T.arm(k, side, s, [sh, el, wr], [0.04, 0.035, 0.031], wr + (0, -0.028, 0), (0.034, 0.034, 0.029),
              [(0.5, -0.9, -0.2), (0.05, -1, -0.25), (-0.4, -0.9, -0.2)], 0.04, 0.012, 0.0, color=GREEN)
        m = np.array([s, 1, 1.0])
        for j, (t, d, ln) in enumerate([(0.3, (0.55, 0.5, 0.68), 0.24), (0.7, (0.75, 0.55, 0.36), 0.18)]):
            root = el + (wr - el) * t + np.array([0.015, 0.0, 0.005])
            k.add(f"armblade{side}{j}", T.leaf_blade(root * m, np.array(d) * m, (0.3 * s, -1, 0.2), ln, 0.034, 0.03,
                                                     widest=0.25), BLADE, f"arm{side}", fuse=False, surface="leaf")

    # The seeds: a pair high behind the shoulders, a pair lower on the back
    for (x, y, z, r) in ((0.075, 0.075, 0.69, 0.046), (0.095, 0.085, 0.55, 0.04)):
        k.add("seed", ell((x, y, z), (r, r, r), sub=3), SEED, "body", fuse=False, mirror=True, surface="shell")

    # The tail: a fern low behind it, back and out to the +X side
    fern(k, (0.0, 0.07, 0.44), (0.45, 0.82, -0.36), 0.42, 6, FERN)

    # The red of the lower jaw; a red band across the belly's front
    k.paint(T.red_front(L), RED)
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.49) - 0.022, 0.0045) * inside(np.abs(P[:, 0]) - 0.14, 0.0045)
            * (P[:, 1] < -0.02), RED)

    lay, E, de = T.eyes(k, L)
    k.build(voxel=VOXEL, smooth=4, band=0.006)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    sceptile(*args())
