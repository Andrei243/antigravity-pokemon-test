# Mega Medicham sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/medichammega.py -- <the export's Medicham-Mega.glb> <out.glb>
# Grown from medicham.py (which builds as it is read, so its way is copied here): the same pale white, the same pink
# and yellow, the same pouting lips with a carved mouth, the same eyes under a heavy lid sloping down to the nose.
#
# What the Mega changes, from the references, largest mass first:
#   1. four great white ribbons, arms of its aura, out of its back: a pair rising up and out to the height of its
#      crown, a pair out to the sides at its face, each a broad wavy band widening to a stepped square end, about the
#      model's height across in the art (drawn in here and swept back a little);
#   2. the pink cap becomes a white headdress of thick petals like a lotus (one upright in the middle, two leaning out
#      beside it, two lying out at the sides), a gold drop at its front and the pink bulb on top, small and round;
#   3. it floats sitting cross-legged: the trousers are two great pink balls, one low (-X) with a thin pale foot
#      pointing down under it, one raised and folded (+X), gold spots on both; a coiled gold sash at the waist; three
#      blue drops hanging at its -X side;
#   4. the hands are pressed together before the chest in prayer, gold bands at the elbows;
#   5. the eyes turn blue under grey lids.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, ring_band, srgb, inside, smoothstep, unit, eye_look, args

src, dst = args()
k = Kit(src, coat="fur")

PALE, PETAL, PINK, YELLOW, GOLD, LIPS, MOUTH, INK = ("#E4E5EC", "#F4F4F8", "#E8457E", "#F4CC6A", "#F0B048", "#F06E98",
                                                    "#9E2450", "#1A1A22")
RIBBON, DROP = "#EEEEF3", "#46B4E6"

# --- the trousers: two great pink balls, the -X leg low, the +X leg raised and folded -------------------------------
def egg(V):
    V = V.copy()
    t = np.clip((V[:, 2] + 1) / 2, 0, 1)
    s = 1.04 - 0.22 * smoothstep(0.55, 1.0, t)
    V[:, 0] *= s; V[:, 1] *= s
    return V
LOW = np.array([-0.04, -0.004, 0.205]); LOWR = (0.118, 0.11, 0.148)
HIGH = np.array([0.088, -0.022, 0.355]); HIGHR = (0.104, 0.1, 0.112)
k.add("trouserL", ell(LOW, LOWR, deform=egg), PINK, "legL")
k.add("trouserR", ell(HIGH, HIGHR, rot=(0, 18, 0)), PINK, "legR")
k.add("seat", ell((0.01, 0.01, 0.4), (0.1, 0.09, 0.075)), PINK, "body")
# the -X foot: a thin pale point down from under the low ball; the +X foot tucked out from under the raised one
k.add("foot", tube([(0.03, -0.01, 0.1), (0.045, -0.02, 0.045), (0.05, -0.028, 0.006)], [0.024, 0.017, 0.006]), PALE,
      "legL", fuse=False)
k.add("toe", tube([(0.15, 0.0, 0.29), (0.185, -0.006, 0.255), (0.2, -0.014, 0.23)], [0.02, 0.014, 0.006]), PALE,
      "legR", fuse=False)

# gold spots on the balls, two to each
for name, c, r, d, size in (("spotL0", LOW, LOWR, unit(-0.55, -0.75, 0.1), 0.046),
                            ("spotL1", LOW, LOWR, unit(0.15, -0.85, -0.5), 0.05),
                            ("spotR0", HIGH, HIGHR, unit(0.1, -0.95, 0.05), 0.044),
                            ("spotR1", HIGH, HIGHR, unit(0.9, -0.35, -0.2), 0.04)):
    p = c + d * np.array(r) * 0.98
    k.decal(p, d, (size, size), YELLOW, eps=0.003, rings=12, segs=44)

# --- the body: a thin pale body, the coiled gold sash at the waist -------------------------------------------------
k.add("waist", ell((0, 0.0, 0.5), (0.04, 0.036, 0.07)), PALE, "body")
k.add("chest", ell((0, 0.0, 0.585), (0.052, 0.042, 0.06)), PALE, "body")
k.add("neck", tube([(0, 0.0, 0.6), (0, -0.004, 0.65), (0, -0.006, 0.69)], [0.023, 0.02, 0.021]), PALE, "head")
spiral = []
for a in np.linspace(0, 2.2 * 2 * math.pi, 70):
    f = a / (2.2 * 2 * math.pi)
    r = 0.118 - 0.07 * f
    spiral.append((0.015 + r * math.sin(a + 0.6), 0.004 - r * math.cos(a + 0.6), 0.418 + 0.09 * f))
k.add("sash", tube(spiral, [0.03 - 0.01 * i / 69 for i in range(70)], seg=12, per=3), GOLD, "body", fuse=False)

# three blue drops hanging at the -X side
for i, (x, y, z) in enumerate(((-0.165, -0.05, 0.34), (-0.13, -0.09, 0.31), (-0.17, 0.0, 0.29))):
    def drip(V):
        V = V.copy()
        t = np.clip((V[:, 2] + 1) / 2, 0, 1)
        V[:, 0] *= 1 - 0.75 * t ** 1.4; V[:, 1] *= 1 - 0.75 * t ** 1.4
        return V
    k.add(f"drop{i}", ell((x, y, z), (0.017, 0.017, 0.028), deform=drip), DROP, "body", fuse=False)

# --- the arms: out and down to the elbows, the forearms up and in, the hands pressed together before the chest ----
for s, side in ((1, "R"), (-1, "L")):
    SH, EL, WR = (0.045 * s, 0.0, 0.61), (0.112 * s, -0.035, 0.548), (0.03 * s, -0.098, 0.6)
    k.add("arm" + side, tube([SH, (0.085 * s, -0.012, 0.582), EL], [0.026, 0.025, 0.024]), PALE, "arm" + side)
    k.add("fore" + side, tube([EL, (0.075 * s, -0.075, 0.57), WR], [0.024, 0.022, 0.02]), PALE, "arm" + side)
    k.add("band" + side, ring_band((0.104 * s, -0.028, 0.553), unit(-0.15 * s, -0.95, 0.3), 0.028, 0.03, thick=0.008),
          GOLD, "arm" + side, fuse=False)
    # a flat palm, its fingers up, pressed against the other
    k.add("hand" + side, ell((0.014 * s, -0.104, 0.632), (0.013, 0.024, 0.04), rot=(-8, 0, 0)), PALE, "arm" + side,
          fuse=False)
    k.add("thumb" + side, tube([(0.024 * s, -0.09, 0.61), (0.026 * s, -0.122, 0.625)], [0.009, 0.007]), PALE,
          "arm" + side, fuse=False)

# --- the head: a pale face under the headdress -------------------------------------------------------------------
FC = np.array([0, -0.01, 0.73])
k.add("face", ell(FC, (0.082, 0.072, 0.075)), PALE, "head")
k.add("cap", ell((0, 0.008, 0.8), (0.1, 0.094, 0.062)), PETAL, "head")
# the face's own colour back under the cap's edge, a straight line just over the eyes
k.paint(lambda P: inside(P[:, 2] - 0.783, 0.004) * (P[:, 2] > 0.62) * (np.hypot(P[:, 0], P[:, 1]) < 0.1), PALE)
k.paint(lambda P: inside(0.783 - P[:, 2], 0.004) * (P[:, 2] < 0.9) * (np.hypot(P[:, 0], P[:, 1]) < 0.12), PETAL)

# the pink pouting lips, a slot of a mouth carved in them
k.add("lips", ell((0, -0.074, 0.684), (0.024, 0.02, 0.017)), LIPS, "head")
k.add("mouth", ell((0, -0.094, 0.684), (0.012, 0.009, 0.007)), MOUTH, "head", carve=True)


# the petals of the headdress: thick, rounded, coming to a soft point at their tops (loose, so each keeps its edge)
def petal_shape(V):
    V = V.copy()
    t = np.clip((V[:, 2] + 1) / 2, 0, 1)
    w = 1 - 0.55 * smoothstep(0.45, 1.0, t)
    V[:, 0] *= w; V[:, 1] *= 0.8 + 0.2 * w
    return V
k.add("petalM", ell((0, -0.012, 0.895), (0.052, 0.04, 0.1), rot=(-8, 0, 0), deform=petal_shape), PETAL, "head",
      fuse=False)
k.add("petalU", ell((0.088, 0.004, 0.888), (0.052, 0.04, 0.096), rot=(-4, 36, 0), deform=petal_shape), PETAL, "head",
      fuse=False, mirror=True)
k.add("petalS", ell((0.14, -0.004, 0.808), (0.046, 0.042, 0.078), rot=(0, 76, 0), deform=petal_shape), PETAL, "head",
      fuse=False, mirror=True)
k.add("petalB", ell((0, 0.06, 0.868), (0.07, 0.04, 0.085), rot=(14, 0, 0), deform=petal_shape), PETAL, "head",
      fuse=False)
# the gold drop at the front, point up
def drop_up(V):
    V = V.copy()
    t = np.clip((V[:, 2] + 1) / 2, 0, 1)
    V[:, 0] *= 1 - 0.7 * t ** 1.3; V[:, 1] *= 1 - 0.5 * t ** 1.3
    return V
k.add("jewel", ell((0, -0.07, 0.834), (0.024, 0.016, 0.046), rot=(-10, 0, 0), deform=drop_up), GOLD, "head",
      fuse=False)
# the pink bulb on top, round, on a short stalk hidden among the petals
k.add("bulb", ell((0, 0.004, 1.022), (0.044, 0.04, 0.062), deform=lambda V: V * np.stack([1 - 0.3 * np.clip(-V[:, 2], 0, 1)] * 2 + [np.ones(len(V))], axis=1)), PINK, "head", fuse=False)
k.add("stalk", tube([(0, 0.004, 0.93), (0, 0.004, 0.975)], [0.016, 0.014]), PINK, "head", fuse=False)


# --- the four ribbons: broad wavy bands out of the back, the upper pair rising, widening to a stepped square end -----
def ribbon(s, z0, rise, length, back, w0, w1, phase):
    def at(u, v):
        uu = 0.02 + 0.98 * u
        step = 1 - 0.07 * smoothstep(0.4, 0.6, 1 - v) * smoothstep(0.85, 1.0, uu)   # the lower half shorter
        q = uu * step
        x = s * (0.03 + length * q)
        y = 0.03 + back * q ** 1.2
        z = z0 + rise * q ** 1.3 + 0.022 * np.sin(2 * math.pi * (1.0 * q + phase)) * smoothstep(0.1, 0.4, q)
        w = w0 + (w1 - w0) * q
        z = z + (v - 0.5) * w
        y = y - (v - 0.5) * w * 0.25 * np.sin(2 * math.pi * (1.4 * q + phase + 0.25))
        return np.stack([x, y, z], axis=-1)
    return sheet(at, 70, 8, 0.016)
k.add("ribbonR0", ribbon(1, 0.645, 0.3, 0.4, 0.14, 0.05, 0.1, 0.0), RIBBON, "ribbonR0", fuse=False)
k.add("ribbonL0", ribbon(-1, 0.645, 0.3, 0.4, 0.14, 0.05, 0.1, 0.0), RIBBON, "ribbonL0", fuse=False)
k.add("ribbonR1", ribbon(1, 0.585, 0.12, 0.4, 0.16, 0.046, 0.092, 0.4), RIBBON, "ribbonR1", fuse=False)
k.add("ribbonL1", ribbon(-1, 0.585, 0.12, 0.4, 0.16, 0.046, 0.092, 0.4), RIBBON, "ribbonL1", fuse=False)

# --- the eyes: Medicham's, blue now, under a grey lid sloping down toward the nose -----------------------------------
CW, CI, CB, CP, CLID = srgb("#F6F6FA"), srgb(INK), srgb("#3FA8DA"), srgb("#14243A"), srgb("#5C5E6A")
CL = srgb(PALE)
def top(u):
    return 0.3 + 0.26 * u
def eye(u, v):
    c = np.tile(CL, (len(u), 1))
    e = (u / 0.98) ** 2 + (v / 0.98) ** 2
    under = (v < top(u)) & (e < 1)
    c[e < 1] = CLID
    c[under] = CI
    c[under & (e < 0.72)] = CW
    ir = np.hypot((u + 0.04) / 0.56, (v + 0.16) / 0.6)
    c[under & (ir < 1)] = CB
    c[under & (np.hypot((u + 0.04) / 0.24, (v + 0.2) / 0.28) < 1)] = CP
    c[under & (np.hypot((u - 0.12) / 0.14, (v + 0.02) / 0.14) < 1)] = (1, 1, 1)
    c[(np.abs(v - top(u)) < 0.15) & (e < 0.98)] = CI
    return c
de = unit(0.4, -0.92, 0.02)
E = np.array([0.038, -0.07, 0.74])
def eyes(state):
    k.decal(E, de, (0.03, 0.028), eye_look(state, eye, CL, CI, line=0.16), eps=0.004, rings=20, segs=56, mirror=True)


k.build(voxel=0.0055)
k.eye_states(eyes)
k.export(dst)
