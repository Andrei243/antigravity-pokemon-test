# Heatran-Mega sculpted on the skeleton and clips of the game's own export of the form (sculptkit.py), grown from
# heatran.py (which builds as it is read, so its helpers are copied here: the oriented ellipsoids, the frame on a
# surface, the ragged molten plates, the studs and the clawed feet):
#   blender --background --factory-startup --python sculpts/heatranmega.py -- <the export's Heatran-Mega.glb> <out.glb>
# From the references (they overrule the game's sculptor, whose comments give it two eyes in a pool of yellow):
# Heatran melted and grown tall. Largest mass first: four great arched legs of red-hot iron, rising from high
# shoulders, bowing out and coming down to grey cuffs on feet of yellow molten metal ringed with grey points, grey
# studs and red drips on them and a yellow blotch on each one's outer face; a dark maroon barrel of a belly slung low
# between them, yellow streaks running down its flanks; the head a tall column of yellow molten face in a red hood,
# ringed with grey studs and dripping red below, under a flat grey cap with points at its sides and grey drips
# hanging from its brim; ONE eye, yellow with a black slit, set under the cap's middle; a great grey plate standing
# on the back, grey plates over the rump, and a cloud of smoke rising from the back and drifting behind.
# Reference ratios (3D picture, feet to the cap's top, the smoke apart): eye height / height 0.89; the legs' arch top
# 0.70; the barrel 0.13 to 0.55; the yellow face 0.34 to 0.83; the smoke's top 1.19; the face's width / the cap's 0.5.
# The export's joints: body (0, 0, 0.3), head (0, -0.12, 0.36), legs (+-0.13, -0.12, 0.28), hind (+-0.13, 0.12, 0.28).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, srgb, inside, unit, boxy, eye_look, args, outward, hull, ring_band, polygon,
                       slab, lathe)

src, out = args()
k = Kit(src, coat="metal")

HIDE, MAROON, STREAK = "#D63E1C", "#6E1A1C", "#F6C42A"
YELLOW, MOLTEN, BLOTCH = "#F8C826", "#FCE46A", "#F28A1E"
STEEL, STUD = "#B2B6BE", "#AAAFB8"
SMOKE = "#BAAE96"
IRIS, PUPIL, INK = "#F8D02A", "#1E1410", "#3A1A10"

X = np.array([1.0, 0, 0])


def oriented(c, frame, r, deform=None, sub=3):
    """An ellipsoid about c whose radii lie along the three directions of frame."""
    V, F = ell((0, 0, 0), r, deform=deform, sub=sub)
    return outward((V @ np.stack(frame, axis=0) + np.array(c), F))


def frame_on(n, along):
    """A frame (t, b, n) on a surface of normal n, t as near along as the surface lets it."""
    n = np.array(n, float); n /= np.linalg.norm(n)
    t = np.array(along, float); t = t - n * (t @ n); t /= np.linalg.norm(t)
    return t, np.cross(n, t), n


# --- the body: a maroon barrel slung low, a red back over it between the legs' shoulders ---------------------------
k.add("barrel", ell((0, 0.07, 0.235), (0.165, 0.2, 0.165)), MAROON, "body", surface="shell")
k.add("back", ell((0, 0.03, 0.35), (0.19, 0.23, 0.115)), HIDE, "body")
k.add("rump", ell((0, 0.2, 0.3), (0.15, 0.1, 0.12)), HIDE, "body")


# Yellow streaks running down the barrel's flanks, slanting back as they go down
def streaks(P):
    x, y, z = P[:, 0], P[:, 1], P[:, 2]
    m = np.zeros(len(P))
    for y0 in (-0.03, 0.06, 0.15):
        m = np.maximum(m, inside(np.abs(y - (y0 - 0.45 * (z - 0.3))) - 0.006, 0.0025))
    return m * inside(0.08 - np.abs(x), 0.01) * inside(0.1 - z, 0.01) * inside(z - 0.33, 0.01)


k.paint(streaks, STREAK, on=["barrel"])

# --- the head: a tall yellow face in a red hood, under a flat grey cap ---------------------------------------------
k.add("hood", ell((0, -0.15, 0.4), (0.12, 0.085, 0.175)), HIDE, "head")
k.add("face", ell((0, -0.2, 0.385), (0.078, 0.06, 0.17)), YELLOW, "head", surface="shell")
k.add("chin", ell((0, -0.19, 0.25), (0.07, 0.055, 0.06)), YELLOW, "head", surface="shell")

# The cap: a wide flat grey disc, a ridge on its top, a point out of each side and drips hanging from its brim
CAP = np.array([0.0, -0.17, 0.585])
k.add("cap", ell(CAP, (0.145, 0.115, 0.034), deform=boxy(2.0)), STEEL, "head", surface="metal")
k.add("capridge", ell(CAP + (0, 0.025, 0.03), (0.06, 0.085, 0.03)), STEEL, "head", surface="metal")
k.add("cappoint", tube([CAP + (0.12, 0.0, 0.0), CAP + (0.17, 0.005, -0.004), CAP + (0.205, 0.01, -0.012)],
                       [0.032, 0.024, 0.012]), STEEL, "head", mirror=True, surface="metal")
for i, (dx, dy, ln) in enumerate(((0.072, -0.095, 0.055), (0.12, -0.06, 0.045), (0.15, 0.02, 0.04))):
    top = CAP + (dx, dy, -0.01)
    k.add(f"capdrip{i}", tube([top, top + (0.0, -0.004, -ln * 0.6), top + (0.0, 0.0, -ln)],
                              [0.024, 0.02, 0.022]), STEEL, "head", mirror=True, surface="metal")

# Grey studs ringing the face, long ovals lying on the hood's edge
for i, (z, dx) in enumerate(((0.475, 0.09), (0.405, 0.098), (0.335, 0.096), (0.27, 0.085))):
    c = np.array([dx, -0.225, z])
    n = unit(0.75, -0.6, 0.0)
    k.add(f"facestud{i}", oriented(c, frame_on(n, (0, 0, 1)), (0.03, 0.022, 0.02)), STUD, "head", fuse=False,
          mirror=True, surface="metal")
for i, dx in enumerate((-0.045, 0.045)):
    c = np.array([dx, -0.225, 0.205])
    k.add(f"chinstud{i}", oriented(c, frame_on(unit(0, -0.6, -0.8), X), (0.03, 0.022, 0.02)), STUD, "head",
          fuse=False, surface="metal")
# Red drips hanging under the face
for i, (dx, ln) in enumerate(((-0.075, 0.07), (0.0, 0.085), (0.075, 0.06))):
    top = np.array([dx, -0.21, 0.235])
    k.add(f"facedrip{i}", tube([top, top + (0, -0.004, -ln * 0.6), top + (0, 0.0, -ln)], [0.026, 0.021, 0.024]),
          HIDE, "head")

# --- the legs: four great arches of red iron from high shoulders down to grey cuffs on molten feet ----------------
LEGS = [("leg", -1.0), ("hind", 1.0)]
ARCHES = []
for name, fy in LEGS:
    bone = name + "{s}"
    # Narrow, steep arches with the feet in under the shoulders (the game's cell); the front pair turned outward at
    # the shoulder (less forward), so that the face column shows whole between them
    root = np.array([0.1, 0.12 * fy, 0.37])
    if fy < 0:
        top = np.array([0.2, -0.145, 0.44])
        knee = np.array([0.265, -0.175, 0.3])
        ankle = np.array([0.24, -0.185, 0.135])
    else:
        top = np.array([0.19, 0.155, 0.44])
        knee = np.array([0.26, 0.185, 0.3])
        ankle = np.array([0.24, 0.19, 0.135])
    k.add(name + "arch", tube([root, top, knee, ankle], [0.1, 0.098, 0.09, 0.08]), HIDE, bone, mirror=True)
    k.add(name + "shoulder", ell(top + (-0.03, 0, -0.03), (0.11, 0.1, 0.09)), HIDE, bone, mirror=True)
    foot = np.array([0.24, ankle[1], 0.0])
    # The molten foot under the cuff: a yellow pad, orange at its edge
    k.add(name + "sole", ell(foot + (0, 0, 0.038), (0.09, 0.09, 0.042)), YELLOW, bone, mirror=True, surface="shell")
    k.add(name + "cuff", outward(lathe(foot + (0, 0, 0.062), (0, 0, 1),
                                       [(0.0, 0.0), (0.088, 0.0), (0.09, 0.03), (0.086, 0.075), (0.0, 0.075)])),
          STEEL, bone, fuse=False, mirror=True, surface="metal")
    for j, q in enumerate((-70, -20, 30, 80, 150)):
        r = math.radians(q)
        dirn = unit(math.cos(r), fy * math.sin(r), 0.0)
        a = foot + dirn * 0.075 + np.array([0, 0, 0.03])
        b = foot + dirn * 0.108 + np.array([0, 0, 0.01])
        k.add(f"{name}claw{j}", outward(tube([a, b], [0.024, 0.012], seg=14, per=6)), STEEL, bone, fuse=False,
              mirror=True, surface="metal")

    # Grey studs along the arch's top and outer side, and red drips at its bend
    d = knee - top; d /= np.linalg.norm(d)
    for j, (p, n) in enumerate(((top + (0.0, 0.0, 0.09), unit(0.1, 0.2 * fy, 1)),
                                (top + (0.08, 0.02 * fy, 0.03), unit(0.8, 0.1 * fy, 0.6)),
                                (knee + (0.075, 0.01 * fy, 0.03), unit(1, 0.15 * fy, 0.4)),
                                (root + (0.03, -0.075 * fy, 0.05), unit(0.2, -fy, 0.5)))):
        k.add(f"{name}stud{j}", oriented(p, frame_on(n, unit(0, fy, 0.3)), (0.034, 0.024, 0.026)), STUD, bone,
              fuse=False, mirror=True, surface="metal")
    for j, (dx, ln) in enumerate(((0.03, 0.05), (-0.04, 0.065))):
        t0 = knee + (dx, -0.06 * fy, -0.02)
        k.add(f"{name}drip{j}", tube([t0, t0 + (0, 0, -ln * 0.6), t0 + (0, 0, -ln)], [0.024, 0.02, 0.022]),
              HIDE, bone, mirror=True)

    # A yellow blotch on the arch's outer face (an orange rim round a yellow heart): patches on the fused arch
    ARCHES.append((top, knee, ankle, fy))

# --- the back: a great grey plate standing over the hind shoulders, grey plates over the rump ----------------------
# The plate: a thick rounded shield standing on the back, leaning back, its face turned a little to +X
PN = unit(0.35, -0.55, 0.2)
k.add("backplate", oriented((0.03, 0.11, 0.5), frame_on(PN, (0, 0, 1)), (0.1, 0.075, 0.035), deform=boxy(1.6)),
      STEEL, "body", fuse=False, surface="metal")
for i, (c, n, r) in enumerate((((0.085, 0.255, 0.31), unit(0.35, 1, 0.25), (0.065, 0.05, 0.03)),
                                ((-0.085, 0.25, 0.27), unit(-0.35, 1, 0.1), (0.06, 0.05, 0.03)),
                                ((0.01, 0.265, 0.16), unit(0, 1, -0.3), (0.075, 0.052, 0.03)))):
    k.add(f"rumpplate{i}", oriented(np.array(c), frame_on(n, X), r, deform=boxy(1.8)), STEEL, "body", fuse=False,
          surface="metal")

# --- the smoke: a column of puffs from behind the plate, billowing into a cloud above and behind -------------------
PUFFS = [((0.05, 0.19, 0.53), 0.045), ((0.06, 0.21, 0.6), 0.05), ((0.08, 0.235, 0.665), 0.055),
         ((0.04, 0.26, 0.72), 0.06), ((0.12, 0.26, 0.72), 0.055), ((0.17, 0.285, 0.69), 0.05),
         ((0.09, 0.3, 0.75), 0.045), ((0.0, 0.29, 0.69), 0.045), ((0.2, 0.27, 0.75), 0.04)]
for i, (c, r) in enumerate(PUFFS):
    k.add(f"smoke{i}", ell(c, (r, r * 0.9, r * 0.85)), SMOKE, "body", surface="fur")

# --- the eye: one round yellow eye with a black slit, set in the face under the cap's middle ----------------------
CI, CP, CK, CY = srgb(IRIS), srgb(PUPIL), srgb(INK), srgb(YELLOW)
EC = np.array([0.0, -0.258, 0.51])
ED = unit(0, -1, 0.12)


def eye(u, v):
    c = np.tile(CK, (len(u), 1))
    r2 = u * u + v * v
    c[r2 < 0.74 ** 2] = CI
    c[(np.abs(u) < 0.2 * np.sqrt(np.clip(1 - (v / 0.7) ** 2, 0, 1))) & (np.abs(v) < 0.7)] = CP
    c[(u + 0.38) ** 2 + (v - 0.34) ** 2 < 0.14 ** 2] = srgb("#FFF6D0")
    return c


def fierce(u, v):
    c = eye(u, v)
    c[v > 0.32 - 0.25 * np.abs(u)] = CY
    c[np.abs(v - (0.32 - 0.25 * np.abs(u))) < 0.15] = CK
    return c


def lay(state):
    look = fierce if state == "Fierce" else eye_look(state, eye, CY, CK, line=0.17)
    k.decal(EC, ED, (0.041, 0.039), look, eps=0.004, rings=20, segs=56)


CB, CM = srgb(BLOTCH), srgb(MOLTEN)


def blotch_look(u, v):
    c = np.tile(CB, (len(u), 1))
    c[(u / 0.72) ** 2 + ((v + 0.05) / 0.66) ** 2 < 1] = CM
    return c


rs = np.random.RandomState(48510)


def ragged(n=9):
    th = np.linspace(0, 2 * math.pi, n, endpoint=False) + rs.rand(n) * 0.3
    r = 0.78 + 0.22 * rs.rand(n)
    return polygon(np.stack([r * np.cos(th), r * np.sin(th)], axis=1))


for top, knee, ankle, fy in ARCHES:
    # On the lower arch's outer face, the side toward the camera that sees it, and on the shoulder's top
    mid = knee * 0.45 + ankle * 0.55
    d = unit(*(ankle - knee))
    n = unit(0.85, 0.45 * fy, 0.0); n = n - d * (n @ d); n /= np.linalg.norm(n)
    k.decal(mid + n * 0.084, n, (0.05, 0.062), blotch_look, up=tuple(-d), outline=ragged(), rings=10, segs=36,
            mirror=True)
    n2 = unit(0.55, 0.2 * fy, 0.8)
    k.decal(top + (0.01, 0.0, -0.01) + n2 * 0.098, n2, (0.048, 0.04), blotch_look, up=(0, fy, 0),
            outline=ragged(), rings=10, segs=36, mirror=True)

k.build(voxel=0.0075, smooth=3, band=0.005)
k.eye_states(lay)
k.export(out)
