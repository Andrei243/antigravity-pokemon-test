# Rampardos sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/rampardos.py -- <the export's Rampardos.glb> <out.glb>
#
# Cranidos grown (cranidos.py: the same hard dome made the same way, the same eyes and pale spikes), from the
# references: a heavy slate-grey dinosaur hunched over a great round body, its head low in front. The blue dome is
# smaller against the body now and sits in a black band that wraps the back of the skull, white spikes standing out of
# the band: two large horns up and back, small ones round its rim. Small red eyes in the black under the dome's edge,
# a red mouth under the grey chin. A big white spike on each shoulder; blue stripes along the upper sides of the back,
# a blue collar round the neck, blue bands round the rising tail, wrists and ankles, a blue patch on each thigh with
# three white spikes at the knee; short arms with dark hands, dark feet with pale claws.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

GREY, BLUE, BLACK, SPIKE, DARK, MOUTH = "#7A8392", "#3E8FDA", "#24262D", "#E4E6EA", "#4C515C", "#C83E48"


def dome(c, r, ea, eb, na=128, nt=36):
    """The hard shell of cranidos.py: the ellipsoid down to a rim on z = ea + eb y (unit), a lip turned in under it."""
    rings = []
    for t in np.linspace(0, 1, nt):
        ring = []
        for a in np.linspace(0, 2 * math.pi, na, endpoint=False):
            sa, ca = math.sin(a), math.cos(a)
            ths = np.linspace(0, math.pi, 721)
            f = np.cos(ths) - (ea + eb * np.sin(ths) * ca)
            th = ths[np.argmax(f < 0)] * t
            ring.append((math.sin(th) * sa, math.sin(th) * ca, math.cos(th)))
        rings.append(np.array(ring))
    last = rings[-1]
    for sc, dz in ((0.93, 0.0), (0.8, 0.05), (0.62, 0.12)):
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


def spike(b, d, l, w):
    b = np.array(b, float); d = unit(*d)
    return blade(np.array([b - d * 0.025, b + d * l * 0.5, b + d * l]), [w, w * 0.6, 0.003], (0, 1, 0), 0.6)


def pale(name, b, d, l, w, bone):
    k.add(name, spike(b, d, l, w), SPIKE, bone, fuse=False, surface="shell")


# --- the body: a great round mass hunched forward, heavy hips ------------------------------------------------------
k.add("torso", ell((0, 0.07, 0.44), (0.24, 0.3, 0.27), rot=(28, 0, 0)), GREY, "body")
k.add("hips", ell((0, 0.14, 0.33), (0.225, 0.2, 0.18)), GREY, "body")
k.add("neck", tube([(0, -0.06, 0.5), (0, -0.19, 0.52)], [0.15, 0.125]), GREY, "body")

# --- the head: grey skull, black band behind the dome, the dome loose ------------------------------------------------
H = np.array([0.0, -0.29, 0.555])
k.add("head", ell(H, (0.125, 0.185, 0.13)), GREY, "head")
k.add("snout", ell((0, -0.37, 0.47), (0.1, 0.09, 0.072), rot=(10, 0, 0)), GREY, "head")
DC, DR = np.array([0.0, -0.335, 0.6]), np.array([0.112, 0.155, 0.112])
EA, EB = -0.33, 0.0
k.add("dome", dome(DC, DR, EA, EB), BLUE, "head", fuse=False, surface="shell")


# The black band: the skull behind and under the dome down to the cheeks, its lower edge sloping up to the back of
# the head; grey jaw and neck under it
def band(P):
    lower = 0.5 + 0.3 * (P[:, 1] + 0.3)
    return inside(lower - P[:, 2], 0.003) * inside(P[:, 1] + 0.07, 0.003)


k.paint(band, BLACK, on=["head", "snout", "neck"])

# Horns out of the band: two large up and back, small ones round its rim
for s in (1, -1):
    m = np.array([s, 1, 1.0])
    pale(f"horn{s}", np.array((0.07, -0.13, 0.63)) * m, (0.4 * s, 0.75, 1), 0.19, 0.048, "head")
    pale(f"cheek{s}", np.array((0.12, -0.25, 0.575)) * m, (s, 0.25, 0.25), 0.05, 0.022, "head")
    pale(f"jowl{s}", np.array((0.115, -0.3, 0.51)) * m, (s, -0.2, -0.45), 0.045, 0.02, "head")
    pale(f"nape{s}", np.array((0.1, -0.14, 0.56)) * m, (0.7 * s, 0.7, 0.1), 0.045, 0.02, "head")

# --- blue stripes: along the upper sides of the back, a collar round the neck -----------------------------------------


def side_stripes(P):
    # a band down each upper flank, front to back, following the round of the body
    a = np.arctan2(np.abs(P[:, 0]), P[:, 2] - 0.38)        # 0 straight up, pi/2 at the side
    return inside(np.abs(a - 0.72) - 0.17, 0.004) * inside(P[:, 1] - 0.25, 0.004) * inside(-0.08 - P[:, 1], 0.004)


def collar(P):
    d = (P[:, 1] + 0.14) + 0.35 * (P[:, 2] - 0.53)
    return inside(np.abs(d) - 0.022, 0.003)


k.paint(side_stripes, BLUE, on=["torso", "hips"])
k.paint(collar, BLUE, on=["neck"])
# A big spike on each shoulder, up and out at the stripe's front
for s in (1, -1):
    pale(f"shoulder{s}", np.array((0.19, 0.0, 0.64)) * np.array([s, 1, 1]), (0.6 * s, 0.35, 0.7), 0.11, 0.038,
         "body")

# --- the tail: thick, rising back, two blue bands -------------------------------------------------------------------
k.add("tail", tube([(0, 0.22, 0.42), (0, 0.38, 0.47), (0, 0.52, 0.56), (0, 0.64, 0.66)], [0.165, 0.11, 0.066, 0.018]),
      GREY, "tail")
k.paint(lambda P: sum(inside(np.abs(P[:, 1] - y) - 0.028, 0.003) for y in (0.4, 0.52)), BLUE, on=["tail"])

# --- arms: short, down and forward, blue wrist bands, dark hands ----------------------------------------------------
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    k.add(f"arm{side}", tube(np.array([(0.115, -0.15, 0.43), (0.15, -0.22, 0.37), (0.16, -0.25, 0.31)]) * m,
                             [0.045, 0.038, 0.034]), GREY, f"arm{side}")
    k.add(f"hand{side}", ell(np.array((0.162, -0.262, 0.285)) * m, (0.035, 0.035, 0.03)), BLACK, f"arm{side}")
    for j, dx in enumerate((-0.02, 0.0, 0.02)):
        p = np.array((0.162 + dx, -0.28, 0.27))
        k.add(f"finger{side}{j}", blade(np.array([p, p + (dx * 0.4, -0.012, -0.02), p + (dx * 0.6, -0.02, -0.035)]) * m,
                                        [0.011, 0.008, 0.002], (0, 1, 0), 0.6), BLACK, f"arm{side}", fuse=False,
              surface="shell")
k.paint(lambda P: inside(np.abs(P[:, 2] - 0.335) - 0.016, 0.003) * inside(P[:, 1] + 0.18, 0.003), BLUE, on=["arm"])

# --- legs: heavy thighs with a blue patch and three knee spikes, dark feet with pale claws and a blue ankle band ------
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    k.add(f"thigh{side}", ell(np.array((0.15, 0.05, 0.27)) * m, (0.115, 0.14, 0.15), rot=(-15, 0, 0)), GREY,
          f"leg{side}")
    k.add(f"shin{side}", tube(np.array([(0.15, 0.06, 0.18), (0.155, 0.08, 0.1), (0.155, 0.05, 0.05)]) * m,
                              [0.07, 0.06, 0.055]), GREY, f"leg{side}")
    k.add(f"foot{side}", ell(np.array((0.155, -0.01, 0.035)) * m, (0.072, 0.115, 0.038)), DARK, f"leg{side}")
    for j, dx in enumerate((-0.042, 0.0, 0.042)):
        b = np.array((0.155 + dx, -0.105, 0.03))
        k.add(f"toe{side}{j}", blade(np.array([b, b + (dx * 0.3, -0.03, -0.006), b + (dx * 0.45, -0.052, -0.02)]) * m,
                                     [0.019, 0.012, 0.002], (0, 0, 1), 0.55), SPIKE, f"leg{side}", fuse=False,
              surface="shell")
    for j, dz in enumerate((-0.065, 0.0, 0.065)):
        b = np.array((0.235 - abs(dz) * 0.3, -0.05, 0.27 + dz))
        pale(f"knee{side}{j}", b * m, (0.7 * s, -0.35, 0.65), 0.075, 0.026, f"leg{side}")


def thigh_patch(P):
    # an oval of blue on the outer front of each thigh, round the knee spikes
    q = np.c_[np.abs(P[:, 0]) - 0.24, P[:, 1] + 0.03, P[:, 2] - 0.27]
    return inside(np.sqrt((q[:, 1] / 0.075) ** 2 + (q[:, 2] / 0.075) ** 2) - 1, 0.04) * inside(0.11 - np.abs(P[:, 0]), 0.004)


k.paint(thigh_patch, BLUE, on=["thigh"])
k.paint(lambda P: inside(np.abs(P[:, 2] - 0.085) - 0.018, 0.003), BLUE, on=["shin"])

# --- the mouth: red under the grey chin -------------------------------------------------------------------------------
k.decal((0, -0.445, 0.43), unit(0, -0.75, -0.65), (0.042, 0.022), MOUTH, eps=0.004, rings=10, segs=40)

# --- the eyes: small, red with a black pupil and a glint, in the black under the dome's edge -------------------------
INK, RED, LID = srgb("#141518"), srgb("#E0262E"), srgb(BLACK)
LINE = srgb("#8A8E98")             # a light line for the closed states on the black


def eye(u, v):
    rho = np.sqrt(u * u + v * v)
    c = np.tile(INK, (len(u), 1))
    c[rho < 0.86] = RED
    c[((u + 0.1) / 0.3) ** 2 + ((v + 0.0) / 0.36) ** 2 < 1] = INK
    return c


de = unit(0.84, -0.53, -0.05)
EC = np.array([0.112, -0.33, 0.535])
m = np.array([-1, 1, 1.0])


def eyes(state):
    look = eye_look(state, eye, LID, LINE if state not in ("Open", "Fierce") else INK, line=0.16)
    k.decal(EC, de, (0.028, 0.032), look, eps=0.006, rings=20, segs=56)
    k.decal(EC * m, de * m, (0.028, 0.032), lambda u, v: look(-u, v), eps=0.006, rings=20, segs=56)
    if state in ("Open", "Fierce"):
        right = np.cross((0, 0, 1.0), de); right /= np.linalg.norm(right)
        up = np.cross(de, right)
        g = EC + right * 0.003 + up * 0.009
        k.decal(g, de, (0.006, 0.007), "#FFFFFF", eps=0.009, rings=5, segs=20)
        k.decal(g * m, de * m, (0.006, 0.007), "#FFFFFF", eps=0.009, rings=5, segs=20)


k.build(voxel=0.008, smooth=3)
k.eye_states(eyes)
k.export(out)
