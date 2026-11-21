# Mega Feraligatr sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/feraligatrmega.py -- <the export's Feraligatr-Mega.glb> <out.glb>
# Feraligatr (feraligatr.py, whose function takes no arguments, so its frame is copied here and changed) grown into its
# Mega Evolution, from the references: the design's largest mass is a great red sail rising from the back of the head
# and running down the neck to the shoulders, taller than the whole head and body above the hips, its rim cut into
# spiky points and a darker red in its middle; the chest yellow from the jaw down to a V at the belly, a dark blue
# band in a V under it; the lower jaw yellow; red cuffs with spikes on the forearms and a red band with a spike on
# each upper arm; dark blue pads on the knees; three red spines along the tail. The rest is Feraligatr's: the open
# jaws, fangs, small fierce eyes with a yellow iris, the red crests off the head, the arms forward and down, the legs.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from types import SimpleNamespace
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, ring_band, srgb, inside, unit, args, sibling

T = sibling("totodile")
BLUE, RED = T.BLUE, T.RED
YELLOW = "#F2D27A"
NAVY = "#2D6488"
DARKRED = "#96203A"
RIDGE = "#2F7FA8"
SEAM = "#C9AE5E"
IRIS = srgb("#F2C230")
VOXEL = 0.008

L = SimpleNamespace(**vars(T.L))
L.H, L.HR = np.array([0.0, -0.1, 0.805]), np.array([0.15, 0.14, 0.12])
L.cheek_at, L.cheek_r = np.array([0.0, -0.06, -0.08]), (0.15, 0.13, 0.105)
L.hinge = (0.0, -0.15, 0.75)
L.gape_up, L.gape_down = 16, 22
L.upper_len, L.upper_r, L.upper_tip = 0.3, (0.112, 0, 0.058), 0.72
L.lower_len, L.lower_r, L.lower_tip, L.lower_colour = 0.29, (0.124, 0, 0.06), 0.74, YELLOW
L.maw_w, L.crease, L.lip, L.tongue_drop = 0.085, 0.02, 0.008, 0.008
L.upper_teeth = [(0.8, 0.042, 0.055, 0.032), (0.56, 0.066, 0.045, 0.028)]
L.lower_teeth = [(0.78, 0.046, 0.045, 0.028), (0.55, 0.072, 0.038, 0.026)]
L.tooth_seat = 0.014
L.nostril_x, L.nostril_r = 0.032, 0.011
L.eye_dir, L.eye_shift, L.eye_h = (0.72, -0.45, 0.45), (0, 0, 0), (0.036, 0.032)
L.eye_outline = T.brow_cut(0.5, 0.45)
L.eye_look = T.lidded_eye(IRIS, L.eye_outline, band=0.22, iris_r=0.56, pupil_r=0.26)
L.lid, L.glint = BLUE, False

# The sail: its outline in its own plane, along the back (a) and up (b), from a root inside the back of the head.
# It stands along the midline as the pictures draw it: the game's two cameras, 31.5 degrees off the front and off
# the back, see its face (two halves splayed apart in a V were tried: they read as two ears from both cameras).
SAIL_AT = np.array([0.0, -0.005, 0.85])
SAIL_BACK = unit(0, 1, 0.0)
SAIL_SCALE = (1.25, 0.88)                  # along the back, up: the pictures draw it nearly as long as tall
SAIL = [(-0.1, 0.02), (-0.135, 0.16), (-0.075, 0.17), (-0.105, 0.32), (-0.035, 0.32), (-0.045, 0.48), (0.035, 0.46),
        (0.055, 0.63), (0.125, 0.55), (0.2, 0.68), (0.25, 0.56), (0.335, 0.6), (0.355, 0.47), (0.45, 0.45),
        (0.42, 0.34), (0.5, 0.27), (0.43, 0.2), (0.46, 0.1), (0.38, 0.06), (0.36, -0.05), (0.28, -0.09),
        (0.2, -0.17), (0.15, -0.29), (0.1, -0.18), (0.03, -0.08), (-0.04, -0.04)]


def sail(k):
    """The great red sail, a thick plate with a darker red middle that shows on both of its faces."""
    outline = [(a * SAIL_SCALE[0], b * SAIL_SCALE[1]) for a, b in SAIL]
    mid = np.array([0.2, 0.3]) * SAIL_SCALE
    inner = [tuple(mid + (np.array(p) - mid) * 0.64) for p in outline]
    up = np.array([0, 0, 1.0])
    k.add("sail", slab(outline, SAIL_AT, SAIL_BACK, up, 0.055), RED, "head", fuse=False, surface="scales")
    k.add("sailheart", slab(inner, SAIL_AT, SAIL_BACK, up, 0.062), DARKRED, "head", fuse=False, surface="scales")
    # a hump of the neck behind the head, where the sail's root stands
    k.add("hump", ell((0, 0.05, 0.72), (0.11, 0.12, 0.11)), BLUE, "body")


def feraligatr_mega(src, out):
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

    # The yellow chest down to a V, a dark blue V band under it, a seam down the yellow
    yellow = T.chevron(0.7, 0.36, 0.2, 0.85, cy=0.02, reach=0.28)
    k.paint(yellow, YELLOW)
    k.paint(lambda P: yellow(P) * inside(np.abs(P[:, 0]) - 0.0045, 0.002), SEAM)
    k.paint(T.chevron(0.7, 0.24, 0.235, 0.85, cy=0.02, reach=0.29, bottom=0.36), NAVY)

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
                  NAVY, f"leg{side}", fuse=False, surface="scales")
    # dark blue pads on the knees
    def knees(P):
        d = ((np.abs(P[:, 0]) - 0.18) / 0.055) ** 2 + ((P[:, 1] + 0.15) / 0.05) ** 2 + ((P[:, 2] - 0.21) / 0.042) ** 2
        return inside(d - 1, 0.08) * (P[:, 1] < -0.08) * inside(P[:, 2] - 0.29)
    k.paint(knees, NAVY)

    # The arms: big, forward and down, a red cuff with two spikes on each forearm, a red band and a spike above
    for s, side in ((1, "R"), (-1, "L")):
        sh, el, wr = np.array((0.148, -0.037, 0.52)), np.array((0.255, -0.11, 0.47)), np.array((0.28, -0.24, 0.41))
        T.hand(k, side, s, tuple(sh), tuple(el), tuple(wr), [0.082, 0.072, 0.064], (0.064, 0.058, 0.058),
               [(0.5, -1, 0.0), (0.18, -1, -0.15), (-0.12, -1, -0.25), (-0.45, -0.9, -0.3)], 0.034, 0.02,
               claws=(0.045, 0.026))
        m = np.array([s, 1, 1.0])
        ax = wr - el
        cuff = el + ax * 0.42
        k.add(f"cuff{side}", ring_band(cuff * m, ax * m, 0.072, 0.075, thick=0.01), RED, f"arm{side}", fuse=False,
              surface="scales")
        for j, (t, d, ln) in enumerate(((0.3, (1, 0.25, 0.65), 0.1), (0.55, (1, 0.1, 0.25), 0.085))):
            base = el + ax * t + np.array([0.058, 0.0, 0.025])
            T.spine(k, f"armspike{side}{j}", tuple(base * m), (d[0] * s, d[1], d[2]), ln, 0.07, 0.04, f"arm{side}",
                    across=(0, 1, 0))
        ua = el - sh
        k.add(f"shoulderband{side}", ring_band((sh + ua * 0.55) * m, ua * m, 0.081, 0.03), RED, f"arm{side}",
              fuse=False, surface="scales")
        base = sh + ua * 0.6 + np.array([0.03, 0.02, 0.06])
        T.spine(k, f"shoulderspike{side}", tuple(base * m), (0.6 * s, 0.4, 1.0), 0.09, 0.065, 0.04, f"arm{side}",
                across=(0, 1, 0))

    # The tail: thick at the hips, back low and out to the +X side, three red spines along it
    TAIL = np.array([(0, 0.16, 0.27), (0.03, 0.33, 0.17), (0.12, 0.47, 0.12), (0.25, 0.56, 0.11)])
    TAIL_R = [0.14, 0.1, 0.065, 0.032]
    k.add("tail", tube(TAIL, TAIL_R), BLUE, "tail")
    for j, (t, ln, w) in enumerate(((0.75, 0.12, 0.09), (1.4, 0.1, 0.08), (2.1, 0.075, 0.06))):
        i = int(t); f = t - i
        c = TAIL[i] * (1 - f) + TAIL[i + 1] * f
        r = TAIL_R[i] * (1 - f) + TAIL_R[i + 1] * f
        a = TAIL[i + 1] - TAIL[i]
        T.spine(k, f"tailspine{j}", c + np.array([0, 0, r * 0.75]),
                unit(*(a / np.linalg.norm(a) * 0.7 + np.array([0.15, 0, 1.0]))), ln, w, 0.045, "tail",
                across=tuple(np.cross(a, (0, 0, 1))))
    # Red spines down the lower back, under the sail
    for j, (y, z, ln, lean) in enumerate([(0.24, 0.42, 0.12, 1), (0.23, 0.31, 0.1, -1)]):
        T.spine(k, f"spine{j}", (0.0, y, z), (0.12 * lean, 0.9, 0.55), ln, 0.1, 0.05, "body")

    # The head: the jaws open wide, a lip along the lower one, the teeth, the nostrils, a brow over each eye
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
    k.paint(lip, YELLOW)
    T.teeth(k, L, J)
    T.nostrils(k, L, J)
    k.add("brow", ell(L.H + (0.075, -0.075, 0.05), (0.06, 0.06, 0.03), rot=(-10, 0, -15)), BLUE, "head",
          mirror=True)

    # The crests: two red fans of three points each, off the top of the head in a V, laid back into the sail
    H = L.H
    for s, side in ((1, "R"), (-1, "L")):
        for j, (y, ln, w, back) in enumerate([(-0.05, 0.17, 0.09, 0.55), (0.03, 0.2, 0.095, 0.8),
                                              (0.1, 0.13, 0.08, 1.1)]):
            base = H + np.array([0.05 * s, y, 0.095 - 0.02 * j])
            T.spine(k, f"crest{side}{j}", base, (0.4 * s, back * 1.2, 1.0), ln, w, 0.05, "head", across=(1, 0, 0.3 * s))
    sail(k)

    lay, E, de = T.eyes(k, L)
    k.build(voxel=VOXEL, smooth=4, band=0.005)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    feraligatr_mega(*args())
