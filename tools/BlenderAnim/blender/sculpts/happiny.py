# Happiny sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/happiny.py -- <the export's Happiny.glb> <out.glb>
#
# The design, from the references, as the young of Chansey's script (its pinks and its way of making eyes): a pink egg
# of a body with no head of its own; a row of round curls across the top of the face; a deeper pink bead on top and
# out of it a pink ponytail that rises and curls over to the model's +X side; black upright eyes with a white glint;
# round deeper pink cheeks; a big open smile with a tongue; a thick white ring round the waist like the rim of a
# dress, a deeper pink skirt below it and two small deeper pink feet under that; tiny arms holding a white round
# stone that rests on the ring at the front.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

PINK, DEEP, CHEEK, WHITE = "#F6B4C8", "#DE8199", "#EF7E98", "#FAF8F4"
INKH, MOUTHH, TONGUEH = "#1C1A22", "#B0283E", "#F28A9C"
C = np.array([0, 0.0, 0.29])             # the egg of a body's middle
R = np.array([0.19, 0.18, 0.225])


def egg(V):
    """Wider below its middle, narrower toward the top, as an egg stands."""
    V = V.copy()
    s = 1 + 0.06 * np.clip(-V[:, 2], 0, 1) - 0.14 * np.clip(V[:, 2], 0, 1) ** 1.5
    # the skirt below the ring: fuller and squarer, its bottom flattened
    s = s + 0.22 * np.clip(-V[:, 2], 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.where(V[:, 2] < 0, np.maximum(V[:, 2] * 1.12, -0.98), V[:, 2])
    return V


def on_body(d, lift=0.0):
    """The point of the (undeformed) egg along d from its middle, a rough place to aim a patch."""
    d = unit(*d)
    s = 1 / math.sqrt(((d / R) ** 2).sum())
    return C + d * (s + lift)


def torus(c, big, small, zs, n=72, m=20):
    """A ring about the upright axis through c: big its radius, small the tube's across, zs its tube's height."""
    V, F = [], []
    for i in range(n):
        a = 2 * math.pi * i / n
        for j in range(m):
            b = 2 * math.pi * j / m
            r = big + small * math.cos(b)
            V.append((c[0] + r * math.cos(a), c[1] + r * math.sin(a), c[2] + zs * math.sin(b)))
    for i in range(n):
        for j in range(m):
            a, b = i * m + j, i * m + (j + 1) % m
            a2, b2 = ((i + 1) % n) * m + j, ((i + 1) % n) * m + (j + 1) % m
            F.append([a, a2, b2, b])
    return np.array(V), F


k.add("body", ell(C, R, deform=egg), PINK, "body")
# the top of the egg on the head's bone, so the face and the curls move with it
k.add("crown", ell((0, 0.0, 0.43), (0.135, 0.13, 0.08)), PINK, "head")

# The skirt: the egg's lower part, deeper pink below the white ring
k.paint(lambda P: inside(P[:, 2] - 0.2, 0.003), DEEP, on=["body"])
k.add("ring", torus((0, 0, 0.2), 0.196, 0.05, 0.042), WHITE, "body", fuse=False, surface="cloth")

# Two small deeper pink feet under the skirt
k.add("foot", ell((0.075, -0.035, 0.036), (0.046, 0.052, 0.036)), DEEP, "leg{s}", mirror=True)

# Five round curls across the top of the face, and the bead and ponytail on top
for az, el, r in ((0, 0.7, 0.046), (25, 0.64, 0.045), (47, 0.6, 0.042)):
    for s in ((1,) if az == 0 else (1, -1)):
        a = math.radians(az)
        d = unit(s * math.sin(a) * math.cos(el), -math.cos(a) * math.cos(el), math.sin(el))
        k.add(f"curl{az}{s}", ell(on_body(d, -0.014), (r, r, r)), PINK, "head")
k.add("bead", ell((0, 0.012, 0.522), (0.066, 0.062, 0.046)), DEEP, "head", fuse=False)
# a thick lock, as wide as the bead where it leaves it, tapering only a little to a fat curled end
tail = [(0, 0.02, 0.52), (0.0, 0.028, 0.57), (0.018, 0.036, 0.6), (0.055, 0.042, 0.632), (0.1, 0.045, 0.638),
        (0.138, 0.045, 0.615), (0.148, 0.045, 0.576), (0.124, 0.045, 0.553), (0.1, 0.045, 0.566)]
k.add("ponytail", tube(tail, [0.05, 0.048, 0.046, 0.044, 0.042, 0.04, 0.038, 0.036, 0.033]), PINK, "head")

# Tiny arms reaching round to hold the stone at its two sides
k.add("arm", tube([(0.12, -0.08, 0.29), (0.112, -0.15, 0.28), (0.085, -0.205, 0.268)], [0.032, 0.03, 0.028]),
      PINK, "arm{s}", mirror=True)

# The white round stone held at the front between the hands, above the ring: one smooth egg
k.add("stone", ell((0, -0.218, 0.252), (0.064, 0.05, 0.058), sub=5), WHITE, "body", fuse=False, surface="shell")

INK, MOUTH, TONGUE, LID = srgb(INKH), srgb(MOUTHH), srgb(TONGUEH), srgb(PINK)

# Round deeper pink cheeks
dc = unit(0.72, -0.62, 0.16)
k.decal(on_body(dc), dc, (0.036, 0.032), CHEEK, eps=0.003, rings=10, segs=40, mirror=True)

# The big open smile: flat across its top, round below, with a tongue
def mouth(u, v):
    c = np.tile(MOUTH, (len(u), 1))
    c[(u / 0.62) ** 2 + ((v + 0.75) / 0.55) ** 2 < 1] = TONGUE
    return c
def mouth_shape(th):
    s = math.sin(th)
    return min(1.0, 0.25 / s) if s > 0 else 1.0
dm = unit(0, -1, 0.26)
k.decal(on_body(dm), dm, (0.038, 0.04), mouth, eps=0.003, outline=mouth_shape, rings=20, segs=64)

# The eyes: black and upright, with a glint high toward the outer side
def eye(u, v):
    c = np.tile(LID, (len(u), 1))
    c[(u / 0.62) ** 2 + (v / 0.92) ** 2 < 1] = INK
    c[((u - 0.12) / 0.24) ** 2 + ((v - 0.4) / 0.24) ** 2 < 1] = (1, 1, 1)
    return c
de = unit(0.34, -0.9, 0.44)
E = on_body(de)
def eyes(state):
    k.decal(E, de, (0.024, 0.03), eye_look(state, eye, LID, INK, line=0.16), eps=0.004, rings=20, segs=56,
            mirror=True)

k.build(voxel=0.0072)
k.eye_states(eyes)
k.export(out)
