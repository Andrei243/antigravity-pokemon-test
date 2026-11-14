# Cranidos sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cranidos.py -- <the export's Cranidos.glb> <out.glb>
#
# From the references: a small grey dinosaur, a short fat body leaning well forward on thick thighs, short shins and
# big feet, the head carried in front and about half the whole height. A hard blue dome runs from the back of the
# skull forward and down over the whole forehead to just above the snout; the grey of the face comes up its side in a
# jagged edge round the eye. Two pale horns stand up at the dome's back corners, one sticks out from each side of the
# head, small spikes run round the back rim. A large eye on each grey cheek, cut across its top by the dome's edge:
# white with a big red iris and a black pupil. A small open mouth under the snout with a pink tongue. A big blue patch
# over the upper back with a ridge and a zigzag lower edge; a short grey tail; small arms held up and forward with
# three pale claws; big feet with three pale claws and a spur behind.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, tri, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

GREY, BLUE, SPIKE, MOUTH, TONGUE = "#A4AAB4", "#5E9FDC", "#E2E5EA", "#7A2A34", "#E8848E"


def dome(c, r, rim, na=160, nt=36):
    """
    A hard shell: the ellipsoid from its top down to a rim whose unit height on each meridian is rim(a) (a the
    azimuth: x = sin a, y = cos a, so a = 0 is the back and a = pi the front), then a lip turned in under it, so the
    rim is a clean edge and nothing of the inside shows.
    """
    rings = []
    for t in np.linspace(0, 1, nt):
        ring = []
        for a in np.linspace(0, 2 * math.pi, na, endpoint=False):
            th = math.acos(float(np.clip(rim(a), -0.99, 0.99))) * t
            ring.append((math.sin(th) * math.sin(a), math.sin(th) * math.cos(a), math.cos(th)))
        rings.append(np.array(ring))
    last = rings[-1]
    for sc, dz in ((0.93, 0.0), (0.8, 0.05), (0.62, 0.12)):        # the lip, turned in and up under the shell
        rings.append(last * np.array([sc, sc, 1]) + np.array([0, 0, dz]))
    V = [np.array([0, 0, 1.0])]
    for ring in rings[1:]:
        V.extend(ring)
    V = np.array(V)
    F = [[0, 1 + j, 1 + (j + 1) % na][::-1] for j in range(na)]
    for i in range(len(rings) - 2):
        a0, b0 = 1 + i * na, 1 + (i + 1) * na
        for j in range(na):
            F.append([a0 + j, b0 + j, b0 + (j + 1) % na, a0 + (j + 1) % na][::-1])
    return V * np.array(r) + np.array(c), F


# --- the head: big and carried in front, a grey skull under a long blue dome -----------------------------------------
DC, DR = np.array([0.0, -0.31, 0.64]), np.array([0.172, 0.245, 0.19])
EYE_A = 2.0                                     # the azimuth of the eye on the dome's side, toward the front


def rim(a):
    """Low over the snout and at the back of the skull, high in a jagged notch round each eye."""
    d = abs((a + math.pi) % (2 * math.pi) - math.pi)     # 0 at the back, pi at the front
    base = -0.02 - 0.72 * (1 - math.cos(d)) / 2
    notch = 0.57 * math.exp(-((d - EYE_A) / 0.42) ** 2)
    jag = 0.07 * float(tri(d * 2.4)) * smoothstep(0.6, 1.2, d) * (1 - smoothstep(2.6, 2.9, d))
    return base + notch + jag


k.add("dome", dome(DC, DR, rim), BLUE, "head", fuse=False, surface="shell")

k.add("head", ell((0, -0.28, 0.6), (0.14, 0.19, 0.15)), GREY, "head")
k.add("snout", ell((0, -0.4, 0.475), (0.11, 0.095, 0.072), rot=(10, 0, 0)), GREY, "head")
k.add("nape", ell((0, -0.13, 0.55), (0.12, 0.1, 0.1)), GREY, "head")
# the cheeks swell out to the dome's surface in its notches, so the eyes stand where they are seen
CHEEK = np.array([0.1, -0.375, 0.595])
k.add("cheek", ell(CHEEK, (0.075, 0.08, 0.075)), GREY, "head", mirror=True)


def on_dome(u):
    u = unit(*u)
    return DC + DR * u


def spike(b, d, l, w):
    b = np.array(b, float); d = unit(*d)
    return blade(np.array([b - d * 0.02, b + d * l * 0.5, b + d * l]), [w, w * 0.6, 0.003], (0, 1, 0), 0.6)


for s in (1, -1):
    m = np.array([s, 1, 1.0])
    # Two horns up at the dome's back corners
    k.add(f"horn{s}", spike(on_dome((0.5 * s, 0.55, 0.67)), (0.35 * s, 0.4, 1), 0.085, 0.03), SPIKE, "head",
          fuse=False, surface="shell")
    # One out of each side of the dome behind the eye
    k.add(f"side{s}", spike(on_dome((0.97 * s, 0.2, 0.08)), (s, 0.5, 0.15), 0.075, 0.026), SPIKE, "head",
          fuse=False, surface="shell")
    # Small spikes round the dome's back rim
    for j, u in enumerate([(0.75, 0.55, 0.35), (0.4, 0.85, 0.35)]):
        uu = np.array(u) * m
        k.add(f"rim{s}{j}", spike(on_dome(uu), uu, 0.04, 0.017), SPIKE, "head", fuse=False, surface="shell")
k.add("rimback", spike(on_dome((0, 0.92, 0.4)), (0, 0.9, 0.4), 0.04, 0.017), SPIKE, "head", fuse=False,
      surface="shell")

# --- the body: a short fat egg leaning well forward, blue over the back to a ridge -----------------------------------
k.add("neck", tube([(0, -0.02, 0.44), (0, -0.14, 0.5)], [0.12, 0.11]), GREY, "body")
k.add("torso", ell((0, 0.02, 0.36), (0.155, 0.19, 0.16), rot=(40, 0, 0)), GREY, "body")
k.add("crest", blade([(0, -0.06, 0.53), (0, 0.06, 0.61), (0, 0.16, 0.65)], [0.085, 0.055, 0.012], (1, 0, 0), 0.55),
      BLUE, "body")
k.add("back", ell((0, 0.04, 0.46), (0.125, 0.13, 0.09), rot=(30, 0, 0)), BLUE, "body")


def blue_back(P):
    """The upper back blue, its lower edge a zigzag over the grey, from behind the head to the tail's root."""
    a = np.arctan2(P[:, 0], P[:, 1] + 0.3)
    zig = 0.38 - 0.06 * P[:, 1] + 0.025 * tri(a * 4.0)
    return inside(zig - P[:, 2], 0.003) * inside(-0.06 - P[:, 1] + 0.4 * (P[:, 2] - 0.55), 0.003) * \
        inside(P[:, 1] - 0.24, 0.003)


k.paint(lambda P: 1 - blue_back(P), GREY, on=["torso", "back", "neck", "crest", "tail", "thigh"])
k.paint(blue_back, BLUE, on=["torso", "back", "neck", "crest", "tail", "thigh"])

k.add("tail", tube([(0, 0.14, 0.33), (0, 0.27, 0.29), (0, 0.37, 0.26)], [0.08, 0.052, 0.018]), GREY, "tail")

# --- arms: small, held up and forward, three pale claws -------------------------------------------------------------
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    k.add(f"arm{side}", tube(np.array([(0.1, -0.14, 0.42), (0.135, -0.21, 0.41), (0.145, -0.26, 0.39)]) * m,
                             [0.042, 0.036, 0.032]), GREY, f"arm{side}")
    Hn = np.array([0.148, -0.285, 0.385])
    for j, d in enumerate([(0.45, -1, -0.35), (0.0, -1, -0.45), (-0.45, -1, -0.35)]):
        d = unit(*d)
        p = Hn + np.array([d[0] * 0.02, 0, 0])
        k.add(f"claw{side}{j}", blade(np.array([p, p + d * 0.02, p + d * 0.038]) * m, [0.011, 0.007, 0.002],
                                      (0, 0, 1), 0.6), SPIKE, f"arm{side}", fuse=False, surface="shell")

# --- legs: thick thighs fused into the body, short thick shins, big feet with three pale claws and a spur ---------
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    k.add(f"thigh{side}", ell(np.array((0.12, 0.04, 0.25)) * m, (0.1, 0.13, 0.13), rot=(-10, 0, 0)), GREY,
          f"leg{side}")
    k.add(f"shin{side}", tube(np.array([(0.13, 0.06, 0.16), (0.135, 0.06, 0.09), (0.135, 0.04, 0.05)]) * m,
                              [0.068, 0.06, 0.055]), GREY, f"leg{side}")
    k.add(f"foot{side}", ell(np.array((0.135, -0.025, 0.036)) * m, (0.072, 0.12, 0.038)), GREY, f"leg{side}")
    for j, dx in enumerate((-0.04, 0.0, 0.04)):
        b = np.array((0.135 + dx, -0.125, 0.028))
        k.add(f"toe{side}{j}", blade(np.array([b, b + (dx * 0.3, -0.028, -0.006), b + (dx * 0.45, -0.05, -0.018)]) * m,
                                     [0.019, 0.012, 0.002], (0, 0, 1), 0.55), SPIKE, f"leg{side}", fuse=False,
              surface="shell")
    b = np.array((0.135, 0.085, 0.03))
    k.add(f"spur{side}", blade(np.array([b, b + (0, 0.022, -0.008), b + (0, 0.04, -0.018)]) * m,
                               [0.015, 0.009, 0.002], (0, 0, 1), 0.55), SPIKE, f"leg{side}", fuse=False,
          surface="shell")

# --- the mouth: a small dark opening under the snout, a pink tongue out of it ---------------------------------------
k.decal((0, -0.48, 0.44), unit(0, -0.75, -0.65), (0.045, 0.026), MOUTH, eps=0.004, rings=10, segs=40)
k.add("tongue", ell((0.012, -0.495, 0.425), (0.024, 0.028, 0.011), rot=(-35, 0, 12)), TONGUE, "head", fuse=False,
      surface="skin")

# --- the eyes: large, white, a big red iris, a black pupil, cut across the top by the dome's edge -------------------
INK, WHITE, RED, LID = srgb("#1A1C22"), np.array([1.0, 1.0, 1.0]), srgb("#D8262E"), srgb(GREY)


def eye(u, v):
    rho = np.sqrt(u * u + v * v)
    c = np.tile(INK, (len(u), 1))
    c[rho < 0.86] = WHITE
    c[((u + 0.1) / 0.58) ** 2 + ((v + 0.05) / 0.62) ** 2 < 1] = RED
    c[((u + 0.12) / 0.24) ** 2 + ((v + 0.02) / 0.28) ** 2 < 1] = INK
    return c


de = unit(0.8, -0.58, 0.0)
EC = CHEEK + np.array([0.075, 0.08, 0.075]) * de
m = np.array([-1, 1, 1.0])


def eyes(state):
    look = eye_look(state, eye, LID, INK, line=0.16)
    k.decal(EC, de, (0.045, 0.05), look, eps=0.006, rings=20, segs=56)
    k.decal(EC * m, de * m, (0.045, 0.05), lambda u, v: look(-u, v), eps=0.006, rings=20, segs=56)
    if state in ("Open", "Fierce"):
        right = np.cross((0, 0, 1.0), de); right /= np.linalg.norm(right)
        up = np.cross(de, right)
        g = EC + right * 0.006 + up * 0.012
        k.decal(g, de, (0.008, 0.009), "#FFFFFF", eps=0.009, rings=5, segs=20)
        k.decal(g * m, de * m, (0.008, 0.009), "#FFFFFF", eps=0.009, rings=5, segs=20)


k.build(voxel=0.007, smooth=3)
k.eye_states(eyes)
k.export(out)
