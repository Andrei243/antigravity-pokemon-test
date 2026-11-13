# Giratina (Altered Forme) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/giratina.py -- <the export's Giratina.glb> <out.glb>
# The export's joints: body (0, 0.05, 0.44), tail (0, 0.45, 0.44), wings (+-0.2, -0.05, 0.62), head (0, -0.36, 0.6) at
# the neck's foot, legs (+-0.16, y, 0.36) at y -0.2 (leg), 0.05 (hind) and 0.3 (hind.001).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, tri, unit, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

GREY, GOLD, RED, BLACK, DARK, PINK = "#B4B8C2", "#E9C23A", "#D23A4C", "#2E2B33", "#4A4D58", "#C96A7C"

# --- The masses: a heavy barrel slung low between six stumps, a thick neck rising from its front, a long tail ---
k.add("body", ell((0, 0.1, 0.43), (0.29, 0.46, 0.27)), GREY, "body")
k.add("bodyfront", ell((0, -0.16, 0.47), (0.26, 0.2, 0.24)), GREY, "body")
k.add("tail", tube([(0, 0.42, 0.42), (0.02, 0.6, 0.3), (0.08, 0.74, 0.16), (0.17, 0.82, 0.06), (0.27, 0.84, 0.03)],
                   [0.18, 0.13, 0.08, 0.04, 0.01]), GREY, "tail")

# The neck's line, which the stripes and the half-rings follow: its centre's y and its radius at a height
NZ = np.array([0.46, 0.62, 0.76, 0.88])
NY = np.array([-0.2, -0.29, -0.34, -0.37])
NR = np.array([0.215, 0.18, 0.15, 0.125])
def ny(z): return np.interp(z, NZ, NY)
def nr(z): return np.interp(z, NZ, NR)
k.add("neck", tube([(0, NY[0], NZ[0]), (0, NY[1], NZ[1])], NR[:2]), GREY, "body")
k.add("throat", tube([(0, -0.28, 0.6), (0, NY[2], NZ[2]), (0, NY[3], NZ[3])], [0.18, NR[2], NR[3]]), GREY, "head")

# The head: a grey dome behind, the face in front (black, a gold jaw under it)
H = np.array([0, -0.38, 0.93])
k.add("skull", ell(H, (0.115, 0.13, 0.105)), GREY, "head")
k.add("snout", ell((0, -0.47, 0.9), (0.085, 0.09, 0.075)), BLACK, "head")

# --- Six legs: short and thick, banded in gold, a crown of gold claws at each foot ---
LEGS = [(-0.2, "leg{s}"), (0.05, "hind{s}"), (0.3, "hind{s}.001")]
for i, (y, bone) in enumerate(LEGS):
    k.add(f"leg{i}", tube([(0.17, y, 0.34), (0.215, y, 0.2), (0.225, y, 0.1)], [0.105, 0.1, 0.092]), GREY, bone, mirror=True)
    # the cuff's two points, out and a little forward and back
    k.add(f"cuff{i}", blade([(0.28, y - 0.01, 0.22), (0.34, y - 0.03, 0.2), (0.4, y - 0.06, 0.16)],
                            [0.04, 0.03, 0.004], (0, 0, 1), 0.45), GOLD, bone, fuse=False, mirror=True, surface="shell")
    for j, (dx, dy) in enumerate([(0.0, -1.0), (0.75, -0.65), (1.0, 0.15)]):
        d = np.array([dx, dy]) / np.hypot(dx, dy)
        a = np.array([0.225 + d[0] * 0.06, y + d[1] * 0.06, 0.06])
        b = np.array([0.225 + d[0] * 0.16, y + d[1] * 0.16, 0.012])
        k.add(f"claw{i}{j}", blade([a, (a + b) / 2 + [0, 0, 0.005], b], [0.036, 0.024, 0.004], (0, 0, 1), 0.5), GOLD,
              bone, fuse=False, mirror=True, surface="shell")
k.paint(lambda P: np.maximum(inside(np.abs(P[:, 2] - 0.2) - 0.025), inside(P[:, 2] - 0.07)), GOLD, on=["leg"])

# --- The gold: the crown, the half-rings round the neck ---
# A crest sweeping up and back from the brow, a broad horn out from each cheek
k.add("crest", blade([(0, -0.46, 0.97), (0, -0.47, 1.04), (0, -0.43, 1.08), (0, -0.35, 1.1)], [0.05, 0.05, 0.038, 0.005],
                     (1, 0, 0), 0.55), GOLD, "head", fuse=False, surface="shell")
k.add("horn", blade([(0.06, -0.44, 0.93), (0.14, -0.47, 0.925), (0.21, -0.5, 0.91), (0.27, -0.52, 0.89)],
                    [0.05, 0.045, 0.03, 0.004], (0, 0.3, 1), 0.4), GOLD, "head", fuse=False, mirror=True, surface="shell")
# the brow's gold plate, over the face from the crest to the horns
k.add("brow", blade([(0.0, -0.48, 0.985), (0.05, -0.475, 0.975), (0.09, -0.455, 0.95)], [0.03, 0.028, 0.024],
                    (0, 1, 0.3), 0.6), GOLD, "head", fuse=False, mirror=True, surface="shell")

# Half-rings hugging the neck's sides, each ending in a point out and down; larger toward the shoulders
def ring(z, ext, thick):
    c = np.array([0, ny(z), z]); r = nr(z)
    def at(a, rr, dz):
        a = math.radians(a)
        return c + np.array([rr * math.sin(a), rr * math.cos(a), dz])
    pts = [at(15, r * 0.8, 0.015), at(55, r - 0.005, 0.01), at(95, r + 0.005, 0.0), at(122, r + ext * 0.45, -0.03),
           at(132, r + ext, -0.075)]
    return blade(pts, [thick * 0.6, thick, thick * 1.05, thick * 0.75, 0.005], (0, 0, 1), 0.7)
for i, (z, ext, th) in enumerate([(0.83, 0.09, 0.05), (0.71, 0.11, 0.058), (0.59, 0.13, 0.064), (0.47, 0.15, 0.07)]):
    k.add(f"collar{i}", ring(z, ext, th), GOLD, "head" if z > 0.6 else "body", fuse=False, mirror=True, surface="shell")

# --- The wings: ragged black sails high on the back, three fingers off the lower edge, each tipped in a red spike ---
def ragged(V):
    V = V.copy()
    th = np.arctan2(V[:, 2], V[:, 0])
    s = 1 + 0.16 * np.sin(5 * th + 1.0) + 0.07 * np.sin(13 * th)
    V[:, 0] *= s; V[:, 2] *= s
    return V
k.add("arm", tube([(0.14, 0.0, 0.62), (0.28, 0.04, 0.8), (0.4, 0.06, 0.9)], [0.05, 0.04, 0.03]), BLACK, "wing{s}",
      fuse=False, mirror=True)
k.add("sail", ell((0.56, 0.06, 0.86), (0.36, 0.028, 0.18), rot=(0, -12, 0), deform=ragged), BLACK, "wing{s}",
      fuse=False, mirror=True)
for i, (x, z0, ln) in enumerate([(0.4, 0.74, 0.17), (0.56, 0.73, 0.2), (0.72, 0.75, 0.23)]):
    k.add(f"finger{i}", tube([(x + 0.02, 0.06, z0 + 0.06), (x, 0.055, z0 - 0.04)], [0.04, 0.03]), BLACK, "wing{s}",
          fuse=False, mirror=True)
    k.add(f"spike{i}", tube([(x, 0.055, z0 - 0.02), (x - 0.015, 0.05, z0 - 0.06), (x - 0.04, 0.045, z0 - ln)],
                            [0.018, 0.036, 0.005]), RED, "wing{s}", fuse=False, mirror=True, surface="shell")

# --- The hide's plates: dark seams down the flanks and one along each side; the tail darkening to its end ---
def seams(P):
    y, z = P[:, 1], P[:, 2]
    s = y + 0.35 * (0.45 - z)
    across = inside(np.abs(((s + 0.05) % 0.2) - 0.1) - 0.007) * (y > -0.12) * (z > 0.24)
    along = inside(np.abs(z - 0.31) - 0.006) * (np.abs(P[:, 0]) > 0.2) * (y > -0.1)
    return np.maximum(across, along)
k.paint(seams, DARK, on=["body"])
k.paint(lambda P: smoothstep(0.62, 0.85, P[:, 1]), DARK, on=["tail"])

# --- The bands: red and black down the front of the throat and chest, black under the belly ---
def front(P):
    x, y, z = P[:, 0], P[:, 1], P[:, 2]
    zc = np.clip(z, NZ[0], NZ[-1])
    neck = (y < ny(zc) - 0.5 * nr(zc)) * (z > 0.42) * (z < 0.82)
    w = np.where(z > 0.42, 0.55 * nr(zc), 0.11)
    chest = (y < -0.28) * (z <= 0.42) * (z > 0.2)
    return inside(np.abs(x) - w) * np.maximum(neck, chest)
k.paint(front, BLACK, on=["neck", "throat", "body"])
k.paint(lambda P: front(P) * (np.floor((P[:, 2] - 0.3) / 0.06) % 2 == 0), RED, on=["neck", "throat", "body"])
k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.13) * inside(P[:, 2] - 0.2) * (P[:, 1] > -0.3) * (P[:, 1] < 0.45), BLACK,
        on=["body"])

# The face: black, a gold jaw under it with a small pink mouth, gold round its sides
k.paint(lambda P: inside(P[:, 2] - 0.875), GOLD, on=["snout"])
k.paint(lambda P: np.maximum(inside(0.07 - np.abs(P[:, 0])), inside(0.965 - P[:, 2])) * inside(P[:, 1] + 0.39), GOLD,
        on=["skull"])
k.add("chin", ell((0, -0.49, 0.83), (0.075, 0.065, 0.03)), GOLD, "head", fuse=False, surface="shell")
k.add("mouth", ell((0, -0.548, 0.87), (0.034, 0.03, 0.012)), PINK, "head", carve=True)

EYE, PUPIL, LID = srgb("#E8303C"), srgb("#1A1418"), srgb(BLACK)
def eye(u, v):
    c = np.tile(EYE, (len(u), 1))
    c[(u / 0.35) ** 2 + (v / 0.8) ** 2 < 1] = PUPIL
    return c
de = unit(0.5, -0.9, 0.12)
def eyes(state):
    k.decal(np.array([0.035, -0.47, 0.925]) + de * 0.06, de, (0.024, 0.015), eye_look(state, eye, LID, EYE, line=0.3),
            eps=0.006, rings=10, segs=32, mirror=True)

k.build(voxel=0.0095)
k.eye_states(eyes)
k.export(out)
