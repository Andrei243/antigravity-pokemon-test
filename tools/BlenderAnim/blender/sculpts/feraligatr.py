# Feraligatr sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/feraligatr.py -- <the export's Feraligatr.glb> <out.glb>
# Totodile and Croconaw grown into a massive blue crocodile (built by totodile.py's functions: the line's blue, red
# and cream, its jaw, fangs, spines, hands, feet and eyes): a great pear of a body low over thick short legs, a cream
# chevron across the belly split by a seam down its middle; the head carried forward on a thick neck, the mouth open
# wide (pink within, a cream lip along the lower jaw, white fangs above and below); small fierce eyes under a brow
# (a yellow iris); two red crests of three points each laid back off the top of the head in a V; big arms carried
# forward and down, dark ridges round the forearm and a plate on the shoulder, four white claws a hand; ridges round
# the shins, three white claws a foot; red spines down the back and along the thick tail, which curves out to +X.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from types import SimpleNamespace
import numpy as np
from sculptkit import Kit, ell, tube, hull, ring_band, srgb, inside, unit, args, sibling

T = sibling("totodile")
BLUE, CREAM, RED = T.BLUE, T.CREAM, T.RED
RIDGE = "#2F7FA8"
SEAM = "#C9B46E"
IRIS = srgb("#F2C230")
VOXEL = 0.008

L = SimpleNamespace(**vars(T.L))
L.H, L.HR = np.array([0.0, -0.1, 0.805]), np.array([0.15, 0.14, 0.12])
L.cheek_at, L.cheek_r = np.array([0.0, -0.06, -0.08]), (0.15, 0.13, 0.105)
L.hinge = (0.0, -0.15, 0.75)
L.gape_up, L.gape_down = 16, 22
L.upper_len, L.upper_r, L.upper_tip = 0.3, (0.112, 0, 0.058), 0.72
L.lower_len, L.lower_r, L.lower_tip, L.lower_colour = 0.29, (0.124, 0, 0.06), 0.74, BLUE
L.maw_w, L.crease, L.lip, L.tongue_drop = 0.085, 0.02, 0.008, 0.008
L.upper_teeth = [(0.8, 0.042, 0.055, 0.032), (0.56, 0.066, 0.045, 0.028)]
L.lower_teeth = [(0.78, 0.046, 0.045, 0.028), (0.55, 0.072, 0.038, 0.026)]
L.tooth_seat = 0.014
L.nostril_x, L.nostril_r = 0.032, 0.011
L.eye_dir, L.eye_shift, L.eye_h = (0.72, -0.45, 0.45), (0, 0, 0), (0.034, 0.03)
L.eye_outline = T.brow_cut(0.5, 0.45)
L.eye_look = T.lidded_eye(IRIS, L.eye_outline, band=0.22, iris_r=0.56, pupil_r=0.26)
L.lid, L.glint = BLUE, False


def feraligatr(src, out):
    k = Kit(src, coat="scales")

    # The body: a great pear widest at the belly, low over the legs, a chest under a thick neck
    def pear(V):
        V = V.copy()
        low = np.clip(-V[:, 2], 0, 1)
        V[:, :2] *= (1 + 0.14 * low)[:, None]
        return V
    k.add("torso", ell((0, 0.02, 0.4), (0.25, 0.22, 0.27), deform=pear), BLUE, "body")
    k.add("hips", ell((0, 0.035, 0.22), (0.24, 0.2, 0.14)), BLUE, "body")
    k.add("chest", ell((0, -0.02, 0.58), (0.2, 0.17, 0.13)), BLUE, "body")
    k.add("neck", tube([(0, 0.0, 0.6), (0, -0.06, 0.69), (0, -0.11, 0.75)], [0.15, 0.135, 0.125]), BLUE, "head")

    # The cream chevron across the belly, a seam down its middle
    k.paint(T.chevron(0.58, 0.21, 0.19, 0.85, cy=0.02, reach=0.27, bottom=0.37), CREAM)
    def seam(P):
        return T.chevron(0.58, 0.21, 0.19, 0.85, cy=0.02, reach=0.27, bottom=0.37)(P) * inside(np.abs(P[:, 0]) - 0.0045,
                                                                                             0.002)
    k.paint(seam, SEAM)

    # The legs: thick thighs fused into the hips, short shins ringed by ridges, three white claws a foot
    for s, side in ((1, "R"), (-1, "L")):
        T.leg(k, side, s, (0.17, -0.01, 0.2), (0.13, 0.15, 0.14),
              [(0.18, -0.03, 0.12), (0.185, -0.04, 0.055)], [0.088, 0.082],
              (0.185, -0.085, 0.03), (0.09, 0.12, 0.034),
              [(0.185 + dx, -0.18, 0.026) for dx in (-0.048, 0.0, 0.048)], (0.026, 0.032, 0.024),
              claws=(0.05, 0.03))
        m = np.array([s, 1, 1.0])
        for j, z in enumerate((0.1, 0.07)):
            k.add(f"shinridge{side}{j}", ring_band(np.array((0.182, -0.035, z)) * m, (0, 0.05, 1), 0.088, 0.016),
                  RIDGE, f"leg{side}", fuse=False, surface="scales")

    # The arms: big, forward and down, dark ridges round the forearm, a plate on the shoulder, four white claws
    for s, side in ((1, "R"), (-1, "L")):
        sh, el, wr = np.array((0.148, -0.037, 0.52)), np.array((0.255, -0.11, 0.47)), np.array((0.28, -0.24, 0.41))
        T.hand(k, side, s, tuple(sh), tuple(el), tuple(wr), [0.082, 0.072, 0.064], (0.064, 0.058, 0.058),
               [(0.5, -1, 0.0), (0.18, -1, -0.15), (-0.12, -1, -0.25), (-0.45, -0.9, -0.3)], 0.034, 0.02,
               claws=(0.045, 0.026))
        m = np.array([s, 1, 1.0])
        ax = wr - el
        for j, t in enumerate((0.25, 0.5, 0.75)):
            c = el + ax * t
            r = 0.072 + (0.064 - 0.072) * t
            k.add(f"armridge{side}{j}", ring_band(c * m, ax * m, r + 0.003, 0.018), RIDGE, f"arm{side}", fuse=False,
                  surface="scales")
        # A ridge round the upper arm, by the shoulder
        ua = el - sh
        k.add(f"shoulderridge{side}", ring_band((sh + ua * 0.55) * m, ua * m, 0.08, 0.02), RIDGE, f"arm{side}",
              fuse=False, surface="scales")

    # The tail: thick at the hips, back low and out to the +X side, red spines along it
    TAIL = np.array([(0, 0.16, 0.27), (0.03, 0.33, 0.17), (0.12, 0.47, 0.12), (0.25, 0.56, 0.11)])
    TAIL_R = [0.14, 0.1, 0.065, 0.032]
    k.add("tail", tube(TAIL, TAIL_R), BLUE, "tail")
    for j, (i, ln) in enumerate(((1, 0.1), (2, 0.08))):
        c = TAIL[i] + np.array([0, 0, TAIL_R[i] * 0.75])
        a = TAIL[i + 1] - TAIL[i - 1]
        T.spine(k, f"tailspine{j}", c, unit(*(a / np.linalg.norm(a) * 0.7 + np.array([0.15, 0, 1.0]))), ln, 0.085,
                0.045, "tail", across=tuple(np.cross(a, (0, 0, 1))))
    # Red spines down the back
    for j, (y, z, ln, lean) in enumerate([(0.17, 0.62, 0.11, 1), (0.22, 0.51, 0.13, -1), (0.24, 0.4, 0.12, 1),
                                          (0.23, 0.3, 0.1, -1)]):
        T.spine(k, f"spine{j}", (0.0, y, z), (0.12 * lean, 0.9, 0.55), ln, 0.1, 0.05, "body")

    # The head: the jaws open wide, a cream lip along the lower one, the teeth, the nostrils, a brow over each eye
    J = T.jaws(k, L)
    hinge = J.hinge
    mid = math.radians((L.gape_up - L.gape_down) / 2)
    half = math.radians((L.gape_up + L.gape_down) / 2)
    def lip(P):
        rel = P - hinge
        fwd = -rel[:, 1]
        dz = rel[:, 2] - fwd * math.tan(mid)
        gap = np.maximum(fwd * math.tan(half), L.crease) - L.lip
        return inside(-dz - gap - 0.03, 0.0025) * inside(gap - 0.002 + dz, 0.0025) * (fwd > 0.03) \
            * inside(np.abs(P[:, 0]) - 0.13, 0.003)
    k.paint(lip, CREAM)
    T.teeth(k, L, J)
    T.nostrils(k, L, J)
    k.add("brow", ell(L.H + (0.075, -0.075, 0.05), (0.06, 0.06, 0.03), rot=(-10, 0, -15)), BLUE, "head",
          mirror=True)

    # The crests: two red fans of three points each, off the top of the head in a V, laid back
    H = L.H
    for s, side in ((1, "R"), (-1, "L")):
        for j, (y, ln, w, back) in enumerate([(-0.05, 0.17, 0.09, 0.55), (0.03, 0.2, 0.095, 0.8),
                                              (0.1, 0.13, 0.08, 1.1)]):
            base = H + np.array([0.05 * s, y, 0.095 - 0.02 * j])
            T.spine(k, f"crest{side}{j}", base, (0.4 * s, back * 1.2, 1.0), ln, w, 0.05, "head", across=(1, 0, 0.3 * s))

    lay, E, de = T.eyes(k, L)
    k.build(voxel=VOXEL, smooth=4, band=0.005)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    feraligatr(*args())
