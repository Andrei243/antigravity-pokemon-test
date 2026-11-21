# Mega Altaria sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/altariamega.py -- <the export's Altaria-Mega.glb> <out.glb>
# Grown from altaria.py (which builds as it is read, so its way is copied here): the same white cotton and the same
# way of making the eyes, now a slender pale-blue bird that hovers. From the pictures, the masses largest first:
#   1. two great raised wings of cloud, rising up and out in a V from the shoulders and joined by a mass of cloud
#      behind the back, together some four to five times as wide as the body, lavender and pink under their puffs;
#   2. a plump pale-blue pear of a body standing in front of the cloud, a short neck into
#   3. a round head (a quarter of the height above the belly), a white cotton collar under its jaw, a white cotton
#      crest on its crown with one short curl, an open white beak with a dark red mouth, white eyes with black pupils;
#   4. three broad flat streamers of tail from under the body, here sent back, down and out to +X to keep the body
#      large in its cell (in the pictures they hang a body's height below it);
#   5. two small three-toed feet tucked on the belly (the skeleton has no leg bones: they are on the body).
# The export's joints: body (0, 0, 0.542), wings (+-0.1, 0.02, 0.582), tail (0, 0.12, 0.422), head (0, -0.1, 0.862).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, lumpy, args

src, dst = args()
k = Kit(src, coat="fur")

BLUE = "#ACDCEE"            # the pictures' pale ice blue, far paler than Altaria's sky blue
BELLY = "#CDEBF4"           # the front of the body, lighter
CLOUD, SHADE, PINK = "#F7F9FC", "#D9DCEE", "#EBD2E6"
BEAK, MOUTH = "#F4F6F8", "#7E2232"

# --- the body: a plump pear, narrower at the top where the neck rises ---------------------------------------------
BC = np.array([0.0, -0.02, 0.565])
def pear(V):
    V = V.copy()
    V[:, :2] *= (1 - 0.3 * np.clip(V[:, 2], 0, 1) + 0.06 * np.clip(-V[:, 2], 0, 1))[:, None]
    return V
k.add("body", ell(BC, (0.118, 0.108, 0.175), rot=(-8, 0, 0), deform=pear), BLUE, "body")
k.add("belly", ell(BC + (0, -0.03, -0.05), (0.1, 0.09, 0.12)), BLUE, "body")
k.paint(lambda P: inside(P[:, 1] - (BC[1] - 0.05), 0.012) * inside(P[:, 2] - 0.72, 0.01)
        * inside(np.abs(P[:, 0]) - 0.09, 0.01), BELLY, on=["body", "belly"])

# --- the neck: short, from the top of the pear forward and up into the head -------------------------------------
NECK = [(0, -0.03, 0.68), (0, -0.07, 0.77), (0, -0.11, 0.83)]
k.add("neck", tube(NECK, [0.06, 0.052, 0.05]), BLUE, "head")

# --- the head: round, a little larger than measured -------------------------------------------------------------
HC = np.array([0.0, -0.125, 0.9])
HR = np.array([0.09, 0.086, 0.09])
k.add("head", ell(HC, HR), BLUE, "head")
# the crest: a white cotton puff on the crown, set back
k.add("crest", ell(HC + (0, 0.025, 0.07), (0.055, 0.05, 0.042), deform=lumpy(3, 2, 0.1)), CLOUD, "head")
k.add("crest2", ell(HC + (0, 0.06, 0.04), (0.045, 0.04, 0.04), deform=lumpy(3, 2, 0.1)), CLOUD, "head")
# one short curl rising from the crest's front, thick enough to read
CURL = [(0, -0.135, 0.975), (0, -0.14, 1.015), (0, -0.115, 1.04), (0, -0.093, 1.028)]
k.add("curl", tube(CURL, [0.016, 0.015, 0.014, 0.012]), BLUE, "head", fuse=False)

# the collar: a ring of cotton puffs under the jaw, framing the lower face, highest at the cheeks
for i in range(11):
    a = 2 * math.pi * i / 11
    p = np.array([0.078 * math.sin(a), -0.115 - 0.07 * math.cos(a), 0.815 + 0.025 * math.sin(a) ** 2 - 0.02 * math.cos(a)])
    r = 0.042 if math.cos(a) > -0.3 else 0.036
    k.add(f"collar{i}", ell(p, (r, r, r * 0.9), deform=lumpy(3, 2, 0.08)), CLOUD, "head")

# the beak: an open white bill, the dark red mouth between its halves (loose, so its edges stay crisp)
def bill(V):
    V = V.copy()
    t = np.clip(-V[:, 1], 0, 1)
    V[:, 2] -= 0.15 * t ** 2
    return V
k.add("beak", ell((0, -0.203, 0.893), (0.036, 0.034, 0.021), deform=bill), BEAK, "head", fuse=False, surface="shell")
k.add("jaw", ell((0, -0.196, 0.834), (0.031, 0.03, 0.015)), BEAK, "head", fuse=False, surface="shell")
k.add("mouth", ell((0, -0.19, 0.862), (0.033, 0.03, 0.026)), MOUTH, "head", fuse=False)

# --- the cloud: two raised wings and a mass behind the back ------------------------------------------------------
rs = np.random.RandomState(10067)
puffs = []          # (centre, radius, bone)

def lobe(c, r, tilt, n, rr, bone):
    """A cloud lobe: a core ellipsoid and puffs over its surface, the core turned about y by tilt."""
    k.add(f"cloudcore{len(puffs)}", ell(c, np.array(r) * 0.88, rot=(0, tilt, 0)), CLOUD, bone)
    m = np.array(__import__("mathutils").Euler((0, math.radians(tilt), 0)).to_matrix())
    for i in range(n):
        z = rs.uniform(-0.95, 0.95)
        t = rs.uniform(0, 2 * math.pi)
        d = np.array([math.sqrt(1 - z * z) * math.cos(t), math.sqrt(1 - z * z) * math.sin(t), z])
        p = np.array(c) + m @ (d * np.array(r) * 0.86)
        puffs.append((p, rs.uniform(rr[0], rr[1]), bone))

for s, sd in ((1, "R"), (-1, "L")):
    # the wing: rising up and out from the shoulder, its outer end highest
    lobe((0.2 * s, 0.07, 0.63), (0.13, 0.11, 0.14), 0, 16, (0.07, 0.09), f"wing{sd}")
    lobe((0.36 * s, 0.07, 0.8), (0.15, 0.115, 0.19), -36 * s, 22, (0.075, 0.1), f"wing{sd}")
    lobe((0.33 * s, 0.09, 0.52), (0.11, 0.1, 0.1), 0, 10, (0.06, 0.085), f"wing{sd}")
# behind the back: the cloud the body stands in front of
lobe((0.0, 0.11, 0.66), (0.16, 0.09, 0.17), 0, 18, (0.07, 0.095), "body")
for i, (p, r, bone) in enumerate(puffs):
    # keep the front of the body and the face clear: puffs in front of the body's plane go back
    if abs(p[0]) < 0.17 and p[1] < 0.0:
        p = p.copy(); p[1] = 0.02 + 0.5 * (p[1] - 0.0)
        if p[1] < 0.0:
            continue
    k.add(f"cloud{i}_", ell(p, (r, r * 0.92, r * 0.9), deform=lumpy(3, 2, 0.07)), CLOUD, bone)

# lavender in the hollows under the puffs and pink on their lowest edges
def under(P, base, amt):
    return smoothstep(base, base - 0.14, P[:, 2]) * amt
k.paint(lambda P: smoothstep(0.62, 0.48, P[:, 2]) * 0.85, SHADE, on=["cloud"])
k.paint(lambda P: smoothstep(0.5, 0.42, P[:, 2]) * 0.8, PINK, on=["cloud"])

# --- the tail: three broad flat streamers, back, down and out to +X --------------------------------------------
for i, x in enumerate((-0.05, 0.0, 0.05)):
    ln = (0.92, 1.0, 0.86)[i]
    pts = np.array([(x, 0.1, 0.44), (x + 0.03, 0.2, 0.33), (x + 0.1 * ln, 0.31, 0.22),
                    (x + 0.2 * ln, 0.4, 0.14), (x + 0.3 * ln, 0.47 * ln + 0.02, 0.1)])
    d = pts[-1] - pts[0]
    nrm = np.cross(d, (1, 0.25, 0)); nrm /= np.linalg.norm(nrm)
    k.add(f"tail{i}", blade(pts, [0.03, 0.036, 0.036, 0.034, 0.006], nrm, 0.42),
          "#B8E2F1" if i == 1 else BLUE, "tail", fuse=False)

# --- the feet: small, tucked on the belly, three toes each ---------------------------------------------------
for s in (1, -1):
    sd = "R" if s > 0 else "L"
    top, ank = np.array([0.07 * s, -0.09, 0.5]), np.array([0.105 * s, -0.15, 0.455])
    k.add(f"leg{sd}", tube([top, (0.09 * s, -0.125, 0.48), ank], [0.024, 0.02, 0.017]), BLUE, "body", fuse=False)
    for j, a in enumerate((-35, 0, 35)):
        r_ = math.radians(a)
        d = unit(math.sin(r_) * 0.8 + 0.3 * s, -math.cos(r_) * 0.7, -0.6)
        k.add(f"toe{sd}{j}", tube([ank, ank + d * 0.042], [0.015, 0.009]), BLUE, "body", fuse=False)

# --- the eyes: white with a large black pupil and a glint, a dark rim, as the pictures show ----------------------
INK, LID, WHITE = srgb("#121216"), srgb(BLUE), np.array((1.0, 1.0, 1.0))
def eye(u, v):
    c = np.tile(INK, (len(u), 1))
    c[(u / 0.8) ** 2 + (v / 0.82) ** 2 < 1] = srgb("#FAFAFA")
    c[((u + 0.06) / 0.56) ** 2 + ((v + 0.02) / 0.62) ** 2 < 1] = INK
    c[((u - 0.1) / 0.17) ** 2 + ((v - 0.26) / 0.18) ** 2 < 1] = WHITE
    return c
de = unit(0.5, -0.84, 0.18)
def eyes(state):
    k.decal(HC + (0.044, -0.066, 0.022), de, (0.023, 0.029), eye_look(state, eye, LID, INK, line=0.18), eps=0.003,
            rings=20, segs=56, mirror=True)

k.build(voxel=0.0082, smooth=4, band=0.006)
k.eye_states(eyes)
k.export(dst)
