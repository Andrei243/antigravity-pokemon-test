# Mamoswine sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/mamoswine.py -- <the export's Mamoswine.glb> <out.glb>
#
# Piloswine grown (piloswine.py, swinub.py): the same brown fur, round pink snout with its two dark nostrils and white
# tusks, now a mammoth. The design, from the references: a huge rounded body of brown fur, a hump over the shoulders
# running forward into the head, which is one with it; a face of pale tan fur round the pink snout, its edge a fringe
# of points like a beard over the chest; above it a band of pale blue (ice-like fur) across the small eyes, framed all
# round by a thick white band that runs down the sides of the face; small eyes, white with a big dark pupil in a dark
# rim, their top cut flat by the mask (a grumpy look); two great white tusks from beside the snout, sweeping down and
# out and then up, the tips curving in; four short thick legs, darker brown, with cuffs of cream fur and dark hooves;
# a ragged hem of fur round the body above the legs; a small tail.
# The skeleton: body (0, 0.04, 0.42), head (0, -0.22, 0.5), tail (0, 0.35, 0.42), legs (+-0.17, -0.14, 0.18), hind legs
# (+-0.17, 0.22, 0.18).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, tri, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

BROWN, LEG, TAN, BLUE, RIM, CREAM = "#9C6A40", "#7A5030", "#E6C995", "#5CC4E6", "#EEEEF2", "#E9E1D3"
PINK, NOSTRIL, HOOF, TUSK = "#F2A0B0", "#B45468", "#3E3A44", "#F1EEE7"

# --- The body: a huge mound with a hump over the shoulders, the head one with it ----------------------------------
def mound(V):
    """One mass: highest over the shoulders, sloping down behind, the upper front running forward into the head."""
    V = V.copy()
    back = np.clip(V[:, 1], 0, 1); up = np.clip(V[:, 2], 0, 1)
    V[:, 2] -= 0.2 * up * back ** 1.5
    V[:, 1] -= 0.16 * np.clip(-V[:, 1], 0, 1) * np.clip(V[:, 2] + 0.3, 0, 1.2)
    low = np.clip(-V[:, 2], 0, 1)
    V[:, :2] *= (1 + 0.22 * np.minimum(low, 0.75))[:, None]          # a full belly over the legs, not a ball on posts
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.88, V[:, 2])
    return V
k.add("body", ell((0, 0.05, 0.43), (0.32, 0.38, 0.315), deform=mound), BROWN, "body")
HC = np.array([0.0, -0.24, 0.58])
k.add("head", ell(HC, (0.205, 0.16, 0.17)), BROWN, "head")
k.add("snout", ell((0, -0.405, 0.535), (0.068, 0.05, 0.05), deform=lambda V: np.where(
    np.arange(3)[None] == 1, np.maximum(V, -0.8), V)), PINK, "head", surface="skin")

# --- Legs: short and thick, darker, cream cuffs, dark hooves -------------------------------------------------------
for s in (1, -1):
    sd = "R" if s > 0 else "L"
    for name, y, bone in (("leg", -0.14, f"leg{sd}"), ("hind", 0.22, f"hind{sd}")):
        k.add(f"{name}{sd}", tube([(0.17 * s, y, 0.34), (0.18 * s, y - 0.005, 0.18), (0.185 * s, y - 0.01, 0.092)],
                                  [0.1, 0.098, 0.098]), LEG, bone)
        for j, dx in enumerate((-0.045, 0.0, 0.045)):
            k.add(f"hoof{name}{sd}{j}", ell((0.185 * s + dx, y - 0.085 + 0.012 * abs(dx) / 0.04, 0.018),
                                             (0.028, 0.03, 0.022)), HOOF, bone, fuse=False, surface="shell")
def cuff(P):
    a = np.arctan2(P[:, 0], P[:, 1])
    return inside(P[:, 2] - (0.085 + 0.02 * tri(a * 4.0)), 0.003) * (np.abs(P[:, 0]) > 0.08)
k.paint(cuff, CREAM, on=["leg", "hind"])

# --- A ragged hem of fur round the body above the legs, and a small tail ------------------------------------------
n_ = 40
for i in range(n_):
    a = math.radians(-180 + 360 * (i + 0.5) / n_)
    if math.cos(a) > 0.55:                       # the beard is the front's
        continue
    o = np.array([math.sin(a), -math.cos(a), 0.0])
    base = np.array([0.3 * o[0], 0.05 + 0.35 * o[1], 0.22])
    tip = base + o * (0.05 + 0.012 * (i % 3)) + np.array([0, 0, -0.09 + 0.015 * ((i * 5) % 2)])
    k.add(f"hem{i}", blade([base, (base + tip) / 2 + o * 0.006, tip], [0.034, 0.024, 0.004], o, 0.45), BROWN, "body")
k.add("tail", blade([(0, 0.38, 0.5), (0, 0.43, 0.47), (0, 0.445, 0.42)], [0.035, 0.028, 0.006], (1, 0, 0), 0.6),
      BROWN, "tail")

# --- The face: tan fur round the snout, the white frame and the blue band across the eyes (painted round the head) --
def face_coords(P):
    a = np.arctan2(P[:, 0], -(P[:, 1] - HC[1]))
    return a * 0.19, P[:, 2], (np.abs(a) < 1.6) & (P[:, 1] < -0.12)
def rrect(s, z, hw, z0, z1, r):
    """A signed measure of a rounded rectangle |s| < hw, z0 < z < z1 with corners of radius r."""
    qx = np.abs(s) - (hw - r)
    qz = np.abs(z - (z0 + z1) / 2) - ((z1 - z0) / 2 - r)
    return np.sqrt(np.maximum(qx, 0) ** 2 + np.maximum(qz, 0) ** 2) + np.minimum(np.maximum(qx, qz), 0) - r
def tan(P):
    s, z, front = face_coords(P)
    bottom = 0.405 + 0.03 * (1 - np.abs(tri(s * 9)))
    return inside(np.maximum(np.abs(s) - 0.2, bottom - z), 0.007) * (z < 0.62) * front
def white(P):
    s, z, front = face_coords(P)
    frame = rrect(s, z, 0.18, 0.593, 0.742, 0.05)
    cheek = rrect(s, z, 0.205, 0.5, 0.66, 0.04)
    cheek = np.maximum(cheek, 0.145 - np.abs(s))                     # the cheeks' bands alone, not the middle
    return inside(np.minimum(frame, cheek), 0.007) * front
def blue(P):
    s, z, front = face_coords(P)
    return inside(rrect(s, z, 0.145, 0.618, 0.705, 0.035), 0.006) * front
k.paint(tan, TAN)
k.paint(white, RIM)
k.paint(blue, BLUE)
k.paint(lambda P: np.ones(len(P)), PINK, on=["snout"])                  # the snout keeps its pink under the tan

# the beard's points round its edge, loose so they keep their edges
for s in (1, -1):
    for j, (z, dx, dz, ln) in enumerate(((0.56, 1.0, -0.45, 0.05), (0.51, 0.9, -0.7, 0.055), (0.465, 0.6, -1.0, 0.05))):
        base = np.array([0.175 * s, -0.31, z])
        d = unit(dx * s, -0.35, dz)
        k.add(f"beard{'R' if s > 0 else 'L'}{j}", blade([base, base + d * ln * 0.55, base + d * ln],
                                                         [0.025, 0.018, 0.003], (0, 1, 0), 0.45), TAN, "head", fuse=False)
for j, x in enumerate((-0.12, -0.06, 0.0, 0.06, 0.12)):
    base = np.array([x, -0.335 + 0.15 * x * x, 0.43])
    d = unit(x * 0.8, -0.35, -1)
    k.add(f"chin{j}", blade([base, base + d * 0.03, base + d * 0.055], [0.024, 0.017, 0.003], (0, 1, 0), 0.45), TAN,
          "head", fuse=False)

# --- The tusks: great white curves from beside the snout, down and out and up, the tips curving in -----------------
k.add("tusk", tube([(0.07, -0.385, 0.5), (0.13, -0.44, 0.4), (0.2, -0.48, 0.31), (0.255, -0.49, 0.3),
                    (0.29, -0.47, 0.39), (0.285, -0.44, 0.52), (0.26, -0.41, 0.6)],
                   [0.048, 0.05, 0.047, 0.042, 0.034, 0.021, 0.007]), TUSK, "head", fuse=False, mirror=True,
      surface="shell")

# --- The nostrils: two short upright dark marks on the snout's front ---------------------------------------------
for x in (0.022, -0.022):
    k.decal((x, -0.455, 0.538), (0, -1, 0.05), (0.0065, 0.014), NOSTRIL, eps=0.004, rings=6, segs=24)

k.build(voxel=0.0085, smooth=7, band=0.012)

# --- The eyes: small, white with a big dark pupil in a dark rim, the top cut flat by the mask -----------------------
INK, WHITE, PUP, LID = srgb("#1E1E26"), srgb("#F6F6F6"), srgb("#1A1A22"), srgb(BLUE)
def eye(u, v):
    c = np.tile(INK, (len(u), 1))
    top = 0.45 + 0.25 * u                       # sloping down to the nose: grumpy
    c[(u ** 2 + v ** 2 < 0.74 ** 2) & (v < top - 0.18)] = WHITE
    c[((u + 0.12) / 0.42) ** 2 + ((v + 0.12) / 0.45) ** 2 < 1] = PUP
    c[v > top] = LID
    return c
de = unit(0.36, -0.92, 0.12)
def eyes(state):
    k.decal((0.07, -0.37, 0.66), de, (0.034, 0.03), eye_look(state, eye, LID, INK, line=0.2), eps=0.005, rings=20,
            segs=56, mirror=True)

k.eye_states(eyes)
k.export(out)
