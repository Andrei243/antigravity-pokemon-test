# Altaria sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/altaria.py -- <the export's Altaria.glb> <out.glb>
# Swablu grown: the same sky blue and the same white cotton. A huge round cloud of fused puffs is its body and its
# folded wings, lower at the front where a long curved blue neck rises out of it; a small egg of a head on top, with
# a round white beak and two white cheek puffs beside it, black oval eyes with a glint over them, and two long flat
# ribbons of feathers from the crown sweeping up, out and back; long flat blue tail feathers hanging from under the
# cloud to the ground behind; small blue feet of three toes splayed out under the cloud's sides.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, lumpy, args

src, out = args()
k = Kit(src, coat="fur")

BLUE, CLOUD, SHADE, BEAK, BEAKLOW = "#58B6EA", "#F7F9FC", "#D3DDEC", "#ECEEF2", "#BEC4D0"

# --- The cloud: a core and puffs on its surface, the outer ones on the wings' bones -------------------------------
C = np.array([0.0, 0.035, 0.36])
H = np.array([0.27, 0.205, 0.225])          # the core's half sizes
k.add("cloudcore", ell(C, H * 0.9), CLOUD, "body")
rs = np.random.RandomState(334)
puffs = []
for i_, lat in enumerate(np.linspace(-58, 66, 5)):
    n = max(4, int(round(11 * math.cos(math.radians(lat)))))
    for j in range(n):
        lon = 360 * (j + 0.5 * (i_ % 2)) / n + rs.uniform(-10, 10)
        la, lo = math.radians(lat + rs.uniform(-6, 6)), math.radians(lon)
        d = np.array([math.cos(la) * math.sin(lo), -math.cos(la) * math.cos(lo), math.sin(la)])
        p = C + d * H * 0.9
        # the dip at the front where the neck comes out: no puffs over its base
        if p[2] > C[2] + 0.11 and abs(p[0]) < 0.1 and p[1] < C[1] - 0.06:
            continue
        r = rs.uniform(0.1, 0.125) * (0.9 if lat < -30 else 1.0)
        puffs.append((p, r))
for i, (p, r) in enumerate(puffs):
    bone = "wingR" if p[0] > 0.12 else "wingL" if p[0] < -0.12 else "body"
    k.add(f"cloud{i}_", ell(p, (r, r * 0.9, r * 0.92), deform=lumpy(3, 2, 0.06)), CLOUD, bone)
# the wings' tops stand a little higher beside the neck, as the cloud folds round it
for s in (1, -1):
    for j, (x, y, z, r) in enumerate([(0.12, -0.1, 0.52, 0.085), (0.19, 0.0, 0.54, 0.09), (0.07, 0.06, 0.57, 0.09)]):
        k.add(f"cloudtop{j}{'R' if s > 0 else 'L'}", ell((x * s, y, z), (r, r * 0.9, r * 0.9), deform=lumpy(3, 2, 0.06)),
              CLOUD, "wingR" if s > 0 else "wingL")
# the soft blue-grey shade under the cloud, as on Swablu's
k.paint(lambda P: smoothstep(C[2] - 0.05, C[2] - 0.2, P[:, 2]) * 0.8, SHADE, on=["cloud"])

# --- The neck: out of the dip, forward, up and a little back under the head ---------------------------------------
NECK = [(0, -0.06, 0.44), (0, -0.15, 0.5), (0, -0.19, 0.57), (0, -0.165, 0.63), (0, -0.115, 0.67)]
k.add("necklow", tube(NECK[:3], [0.05, 0.042, 0.037]), BLUE, "body")
k.add("neckup", tube(NECK[2:], [0.037, 0.034, 0.036]), BLUE, "head")

# --- The head: a small egg, a little narrower at the top ----------------------------------------------------------
HC = np.array([0.0, -0.105, 0.715])
def egg(V):
    V = V.copy()
    V[:, :2] *= (1 - 0.14 * np.clip(V[:, 2], 0, 1))[:, None]
    return V
k.add("head", ell(HC, (0.054, 0.056, 0.068), deform=egg), BLUE, "head")
k.add("face", ell(HC + (0, -0.018, -0.008), (0.047, 0.045, 0.055)), BLUE, "head")

# the beak: a round white bill with a pale lower bill under it, as Swablu's
def bill(V):
    V = V.copy()
    t = np.clip(-V[:, 1], 0, 1)
    V[:, 2] -= 0.18 * t ** 2
    return V
k.add("beak", ell((0, -0.163, 0.69), (0.024, 0.024, 0.021), deform=bill), BEAK, "head", fuse=False, surface="shell")
k.add("beaklow", ell((0, -0.156, 0.671), (0.018, 0.017, 0.009)), BEAKLOW, "head", fuse=False, surface="shell")
# two white cheek puffs beside the bill
k.add("cheek", ell((0.03, -0.142, 0.684), (0.023, 0.02, 0.019), deform=lumpy(3, 2, 0.08)), CLOUD, "head", fuse=False,
      mirror=True)

# the two long ribbons from the crown: up, then out and back, flat and turning down at their tips
for s in (1, -1):
    pts = np.array([(0.01 * s, -0.1, 0.77), (0.025 * s, -0.08, 0.835), (0.06 * s, -0.02, 0.87), (0.12 * s, 0.07, 0.875),
                    (0.19 * s, 0.17, 0.85), (0.25 * s, 0.26, 0.8)])
    k.add(f"plume{'R' if s > 0 else 'L'}", blade(pts, [0.012, 0.022, 0.028, 0.028, 0.024, 0.006], (0.25 * s, 0, 1), 0.22),
          BLUE, "head", fuse=False)

# --- Long flat tail feathers from under the cloud to the ground behind ------------------------------------------
for i, x in enumerate((-0.07, -0.025, 0.02, 0.065)):
    ln = 1.0 if i % 3 else 0.9
    pts = [(x, 0.12, 0.2), (x * 1.15, 0.2, 0.11), (x * 1.3, 0.25 + 0.03 * ln, 0.025)]
    k.add(f"tail{i}", blade(pts, [0.016, 0.017, 0.006], (0, 0.6, 0.8), 0.3), BLUE, "tail", fuse=False)

# --- Small blue feet: a short leg out of the cloud and three toes, splayed out at the sides -----------------------
for s in (1, -1):
    sd = "R" if s > 0 else "L"
    top, ank = np.array([0.05 * s, -0.02, 0.2]), np.array([0.16 * s, -0.12, 0.075])
    k.add(f"leg{sd}", tube([top, (0.12 * s, -0.08, 0.12), ank], [0.02, 0.017, 0.014]), BLUE, f"leg{sd}", fuse=False)
    for j, a in enumerate((-35, 0, 35)):
        r_ = math.radians(a)
        d = unit(math.sin(r_) * 0.8 + 0.35 * s, -math.cos(r_) * 0.8, -0.55)
        k.add(f"toe{sd}{j}", tube([ank, ank + d * 0.04], [0.009, 0.005]), BLUE, f"leg{sd}", fuse=False)

# --- The eyes: black ovals with a white glint, over the beak, as Swablu's -----------------------------------------
INK, LID = srgb("#121216"), srgb(BLUE)
def eye(u, v):
    c = np.tile(INK, (len(u), 1))
    c[((u - 0.2) / 0.36) ** 2 + ((v - 0.38) / 0.3) ** 2 < 1] = (1, 1, 1)
    return c
de = unit(0.42, -0.88, 0.2)
def eyes(state):
    k.decal(HC + (0.024, -0.03, 0.022), de, (0.0115, 0.017), eye_look(state, eye, LID, INK, line=0.2), eps=0.004,
            rings=20, segs=56, mirror=True)

k.build(voxel=0.0065)
k.eye_states(eyes)
k.export(out)
