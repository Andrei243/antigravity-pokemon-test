# Heracross sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/heracross.py -- <the export's Heracross.glb> <out.glb>
#
# From the references: a blue beetle standing upright, its head and body one tall egg of shell. One great horn rises
# from between the eyes, in front of the dome, and parts at its top into two flat prongs, each with a barb on its outer
# edge (the male's plain fork). Two short antennae with round tips on the dome. Round yellow eyes in a black rim with a
# black pupil and a glint; a small dark mouth under the horn's foot. A plate over the belly, outlined dark, split down
# the middle; wing cases behind with a seam. Round shoulders, thin upper arms and broad flat hands with spikes on their
# edges and two white talons curving in. Round thighs with a spike, short shins, feet with two white claws.
# The export's joints: body (0, 0, 0.5), head (0, -0.02, 0.7), ears (+-0.07, -0.04, 0.85), arms (+-0.19, -0.02, 0.64),
# legs (+-0.11, 0, 0.36).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="shell")

BLUE, DEEP, LIGHT, LINE = "#4D86CB", "#3C6CAE", "#5F96D6", "#22385E"
CLAW, YELLOW, INK, MOUTH = "#F2F2EE", "#F6C631", "#16181E", "#26324E"

BC = np.array([0.0, 0.0, 0.51])          # the egg's middle
BR = np.array([0.245, 0.2, 0.33])       # and its radii

# --- the egg of head and body -----------------------------------------------------------------------------------
k.add("body", ell(BC, BR), BLUE, "body")
k.add("dome", ell((0, -0.01, 0.7), (0.21, 0.178, 0.15)), BLUE, "head")
# the horn's foot: a ridge on the face between the eyes, running down to just over the mouth
k.add("hornfoot", ell((0, -0.18, 0.67), (0.055, 0.045, 0.075)), BLUE, "head")

# --- the horn: a stem rising in front of the dome, leaning back a little, a fork of two flat prongs at its top --------
STEM = [(0, -0.188, 0.66), (0, -0.178, 0.78), (0, -0.14, 0.9), (0, -0.105, 1.0), (0, -0.09, 1.045)]
k.add("horn", tube(STEM, [0.058, 0.05, 0.039, 0.031, 0.03]), BLUE, "head", fuse=False)
FORK = np.array([0, -0.088, 1.04])
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    tip = np.array([0.125, -0.07, 1.245]) if s == 1 else np.array([0.11, -0.072, 1.225])
    pts = [FORK + np.array([0.008 * s, 0, 0]), FORK + np.array([0.05 * s, 0.006, 0.09]), tip * m]
    k.add(f"prong{side}", blade(pts, [0.036, 0.04, 0.006], (0, 1, 0), 0.35), BLUE, "head", fuse=False,
          surface="shell")
    # a barb on the prong's outer edge, pointing out and down
    b0 = FORK + np.array([0.06 * s, 0.006, 0.125])
    k.add(f"barb{side}", blade([b0, b0 + np.array([0.035 * s, 0.002, -0.012]), b0 + np.array([0.06 * s, 0.003, -0.04])],
                               [0.018, 0.012, 0.003], (0, 1, 0), 0.45), BLUE, "head", fuse=False, surface="shell")

# --- antennae: short, up and out from the dome, round tips -----------------------------------------------------------
k.add("feeler", tube([(0.065, -0.05, 0.84), (0.11, -0.06, 0.9), (0.15, -0.065, 0.935)], [0.013, 0.011, 0.01]),
      BLUE, "ear{s}", fuse=False, mirror=True)
k.add("knob", ell((0.155, -0.066, 0.945), (0.026, 0.026, 0.026), sub=3), BLUE, "ear{s}", fuse=False, mirror=True)

# --- arms: a round shoulder, a thin upper arm, a broad flat hand with spiky edges and two white talons -------------
k.add("shoulder", ell((0.225, -0.015, 0.64), (0.07, 0.065, 0.066)), BLUE, "arm{s}", mirror=True)
k.add("upper", tube([(0.26, -0.03, 0.6), (0.29, -0.05, 0.54), (0.315, -0.065, 0.49)], [0.032, 0.028, 0.03]), DEEP,
      "arm{s}", mirror=True)
HAND = np.array([0.345, -0.085, 0.39])
k.add("hand", ell(HAND, (0.052, 0.07, 0.115), rot=(-12, -14, 0)), BLUE, "arm{s}", mirror=True)
for i, (dz, L) in enumerate(((0.06, 0.05), (0.0, 0.055), (-0.055, 0.045))):
    b0 = HAND + np.array([0.035, 0.0, dz])
    k.add(f"hspike{i}", blade([b0, b0 + np.array([L * 0.75, 0.005, -L * 0.5]), b0 + np.array([L, 0.008, -L * 0.85])],
                              [0.02, 0.012, 0.002], (0, 1, 0), 0.5), BLUE, "arm{s}", mirror=True)
# two white talons out of the hand's foot, curving down and in
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    for j, (dx, dy) in enumerate(((0.02, -0.03), (-0.025, 0.0))):
        b0 = HAND + np.array([dx, dy - 0.02, -0.085])
        pts = np.array([b0, b0 + np.array([-0.008, -0.025, -0.045]), b0 + np.array([-0.03, -0.035, -0.08])]) * m
        k.add(f"talon{j}{side}", blade(pts, [0.022, 0.015, 0.002], (1, 0, 0), 0.55), CLAW, f"arm{side}", fuse=False,
              surface="shell")

# --- legs: a round thigh with a spike, a short shin, a foot with two white claws -----------------------------------
k.add("thigh", ell((0.21, -0.01, 0.24), (0.11, 0.1, 0.1)), BLUE, "leg{s}", mirror=True)
b0 = np.array([0.26, 0.0, 0.31])
k.add("tspike", blade([b0, b0 + (0.035, 0.005, 0.04), b0 + (0.055, 0.01, 0.075)], [0.026, 0.015, 0.002],
                      (0, 1, 0), 0.5), BLUE, "leg{s}", mirror=True)
k.add("shin", tube([(0.21, -0.025, 0.18), (0.215, -0.04, 0.11), (0.215, -0.05, 0.07)], [0.045, 0.04, 0.036]), DEEP,
      "leg{s}", mirror=True)
k.add("foot", ell((0.215, -0.07, 0.04), (0.05, 0.065, 0.038)), BLUE, "leg{s}", mirror=True)
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    for j, dx in enumerate((-0.025, 0.028)):
        b0 = np.array([0.215 + dx, -0.115, 0.035])
        pts = np.array([b0, b0 + np.array([dx * 0.5, -0.04, -0.004]), b0 + np.array([dx * 1.0, -0.075, -0.022])]) * m
        k.add(f"claw{j}{side}", blade(pts, [0.019, 0.013, 0.002], (0, 0, 1), 0.6), CLAW, f"leg{side}", fuse=False,
              surface="shell")

# --- the belly's plate: lighter, outlined dark, split down the middle -----------------------------------------------
TOP = 0.565


def angle(P):
    return np.arctan2(P[:, 0], -P[:, 1])


ON = ["body", "dome"]


def plate_d(P):
    """Signed distance-ish to the plate's edge (negative inside), in metres along the surface."""
    a = angle(P)
    r = np.sqrt(P[:, 0] ** 2 + P[:, 1] ** 2)
    side = (np.abs(a) - 0.98) * r
    top = P[:, 2] - TOP
    low = 0.19 - P[:, 2]
    return np.maximum(np.maximum(side, top), low) * (P[:, 1] < 0.02) + (P[:, 1] >= 0.02) * 1.0


k.paint(lambda P: inside(plate_d(P) + 0.006, 0.0025), LIGHT, on=ON)
k.paint(lambda P: inside(np.abs(plate_d(P) + 0.0) - 0.0055, 0.0022), LINE, on=ON)
k.paint(lambda P: inside(np.abs(P[:, 0] - 0.004) - 0.0045, 0.0022) * inside(plate_d(P), 0.002) * (P[:, 1] < 0), LINE,
        on=ON)
# the wing cases' seam down the back
k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.0045, 0.0022) * (P[:, 1] > 0.05) * inside(P[:, 2] - 0.82, 0.004)
        * inside(0.2 - P[:, 2], 0.004), LINE, on=ON)

# --- the mouth: a small dark curve under the horn's foot -------------------------------------------------------------
CB, CM = srgb(BLUE), srgb(MOUTH)


def mouth(u, v):
    c = np.tile(CB, (len(u), 1))
    c[np.abs(v - (-0.25 + 0.55 * u * u)) < 0.38] = CM
    return c


k.decal((0, -0.185, 0.592), unit(0, -1, -0.15), (0.048, 0.016), mouth, eps=0.004, rings=10, segs=48)

k.build(voxel=0.0075)

# --- the eyes: round yellow in a black rim, a black pupil, a glint -------------------------------------------------
CY, CK, CW = srgb(YELLOW), srgb(INK), np.array([1.0, 1.0, 1.0])


def eye(u, v):
    r = u * u + v * v
    c = np.tile(CK, (len(u), 1))
    c[r < 0.86 ** 2] = CY
    c[((u + 0.06) / 0.25) ** 2 + ((v + 0.02) / 0.34) ** 2 < 1] = CK
    c[((u + 0.14) / 0.11) ** 2 + ((v - 0.14) / 0.11) ** 2 < 1] = CW
    return c


de = unit(0.4, -0.88, 0.22)
E = np.array([0.104, -0.152, 0.676])


def eyes(state):
    k.decal(E, de, (0.05, 0.058), eye_look(state, eye, CB, CK, line=0.15), eps=0.005, rings=20, segs=56,
            mirror=True)


k.eye_states(eyes)
k.export(out)
