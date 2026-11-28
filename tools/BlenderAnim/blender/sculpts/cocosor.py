# Cocosor sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cocosor.py -- <the export's Cocosor.glb> <out.glb>
# Cocosel grown (built with cocosel.py's own functions: its comb, its folded wing, its legs and feet, its eyes): from
# the picture (10034-Cocosor.jpg), a proud fighting cockerel standing tall, crowing: a towering red comb of flame-like
# tongues, a golden head and great golden hackles flaring round the neck over the chest and shoulders, a red face
# round a fierce orange eye, red wattles hanging under its beak, the beak wide open mid-crow (a Sound type: its voice
# is its instrument, so the open beak and the head raised are what is seen first); a wine-red back and wings with a
# green bar, a dark green breast and belly, golden saddle feathers at the tail's root and a great sweep of long
# dark green sickle feathers arching up and over behind; dark green thighs, long yellow legs with pale spurs.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, outward, srgb, inside, smoothstep, unit, eye_look, polygon, axes,
                       args, sibling)

C = sibling("cocosel")
T = C.T

WINE = "#8C2B23"
GOLD = "#E3A83A"
GOLDLO = "#C98A2A"
BREAST = "#24472F"
SICKLE, SICKLE2 = "#1D4A2D", "#2C6B42"
COMB = "#E2403A"
FACE = "#C8352E"
BEAK = "#E9B94E"
MOUTH = "#5C1616"
SPUR = "#EEDDB0"
BROW = "#5A1410"
INK = srgb("#161112")
WHITE = np.array([1.0, 1.0, 1.0])

# --- the frame (joints: body (0, 0, 0.517), wings (+-0.13, -0.06, 0.597), tail (0, 0.18, 0.577), head
#     (0, -0.13, 0.677), jaw (0, -0.25, 0.847), legs (+-0.065, 0, 0.437)) -------------------------------------------
H = np.array([0.0, -0.195, 0.842])          # a third over the picture's: a small head on a tall bird reads small
HR = np.array([0.08, 0.082, 0.08])
DE = unit(0.62, -0.7, 0.22)
EYE_HALF = (0.023, 0.023)


def eye_open(u, v):
    """A fierce round eye: an orange iris in a dark rim, a black pupil a little forward, a glint."""
    r = np.hypot(u, v)
    c = np.tile(srgb("#F09A2A"), (len(u), 1))
    c[r < 0.55] = srgb("#E27A18")
    c[np.hypot(u + 0.12, v + 0.02) < 0.36] = INK
    c[np.hypot(u + 0.0, v - 0.24) < 0.14] = WHITE
    c[r > 0.84] = INK
    return c


def sickles(k, base, sweep=18):
    """The great tail: long tapering sickle feathers, each arching up and back from the rump and falling at its end,
    fanned out to the sides and the whole swept some degrees toward +X so both cameras see their breadth."""
    plan = [  # (yaw, length share, height share, radius, colour)
        (0, 1.0, 1.0, 0.05, SICKLE), (-11, 0.95, 0.9, 0.048, SICKLE2), (11, 0.95, 0.9, 0.048, SICKLE2),
        (-22, 0.86, 0.74, 0.045, SICKLE), (22, 0.86, 0.74, 0.045, SICKLE), (-33, 0.74, 0.55, 0.042, SICKLE2),
        (33, 0.74, 0.55, 0.042, SICKLE2)]
    curve = [(0.0, 0.0), (0.07, 0.13), (0.18, 0.23), (0.31, 0.22), (0.41, 0.1), (0.45, -0.05), (0.43, -0.15)]
    for i, (yaw, ls, hs, r, col) in enumerate(plan):
        y = math.radians(yaw + sweep)
        back = np.array([math.sin(y), math.cos(y), 0.0])
        side = np.array([math.cos(y), -math.sin(y), 0.0])
        b = np.array(base, float) + side * 0.012 * yaw / 11
        pts = [b + back * a * ls + np.array([0, 0, 1.0]) * h * hs * (0.85 + 0.15 * ls) for a, h in curve]
        radii = [r * 0.7, r, r, r * 0.9, r * 0.75, r * 0.55, r * 0.35]
        k.add(f"sickle{i}", outward(blade(pts, radii, side, 0.42)), col, "tail", fuse=False, surface="fur")


def cocosor(src, out_path):
    k = Kit(src, coat="fur")
    # The body: a tall, deep fighting bird, chest up, a dark green breast and belly under a wine-red back
    k.add("body", ell((0, 0.03, 0.5), (0.15, 0.2, 0.16), rot=(-24, 0, 0)), WINE, "body")
    k.add("breast", ell((0, -0.11, 0.56), (0.125, 0.11, 0.135)), WINE, "body")
    k.add("neck", ell((0, -0.16, 0.7), (0.075, 0.075, 0.12), rot=(-15, 0, 0)), GOLD, "head")
    k.add("head", ell(H, HR), GOLD, "head")
    k.add("thigh", ell((0.068, 0.025, 0.41), (0.066, 0.08, 0.095)), BREAST, "leg{s}", mirror=True)
    def breast(P):
        y, z = P[:, 1], P[:, 2]
        zg = 0.52 + 0.5 * np.clip(y + 0.05, -0.1, 0.25)
        return smoothstep(0.03, -0.03, z - zg)
    k.paint(breast, BREAST, on=["body", "breast", "thigh"])

    # The great golden hackles: a mane round the neck drawn down into points over the chest and the shoulders, and
    # loose points flaring out of it
    k.add("hackle", T.ruff((0, -0.15, 0.7), (0.112, 0.105, 0.06), 12, 2.0, 0.45), GOLD, "head")
    for i in range(12):
        a = math.pi * (-0.6 + 1.2 * i / 11)                          # round the front and the sides
        root = np.array([math.sin(a) * 0.085, -0.16 - math.cos(a) * 0.075, 0.69])
        d = unit(math.sin(a) * 0.55, -math.cos(a) * 0.45 + 0.2, -0.95)
        k.add(f"hacklept{i}", T.spike(root, d, 0.13 + 0.03 * math.cos(a), 0.034, 0.016,
                                      across=(math.cos(a), math.sin(a), 0)), GOLD if i % 2 else GOLDLO, "head",
              fuse=False, surface="fur")
    for i, x in enumerate((-0.05, 0.0, 0.05)):
        root = np.array([x, -0.06, 0.73])
        k.add(f"hackleback{i}", T.spike(root, (x * 2.5, 0.75, -0.65), 0.16, 0.036, 0.016, across=(1, 0, 0)), GOLD,
              "head", fuse=False, surface="fur")

    # Folded wings: wine-red, a green bar across, dark green flight feathers at their lower edge
    C.wing(k, [(0.13, -0.09, 0.63), (0.155, -0.01, 0.59), (0.145, 0.09, 0.53), (0.105, 0.2, 0.47)],
           [0.05, 0.075, 0.062, 0.016], WINE, SICKLE, None, lambda y: 0.52 + 0.3 * y, scallop=0.016, period=0.05)
    k.paint(lambda P: inside(np.abs(P[:, 2] - (0.585 + 0.3 * P[:, 1])) - 0.016, 0.004) * (P[:, 1] < 0.12),
            SICKLE2, on=["wing"])

    # The tail: golden saddle feathers at its root and the sickles
    for i, (x, ln) in enumerate(((-0.06, 0.12), (0.0, 0.14), (0.06, 0.12))):
        k.add(f"saddle{i}", T.spike((x, 0.13, 0.62), (x * 2, 0.8, -0.35), ln, 0.034, 0.016, across=(1, 0, 0)), GOLD,
              "tail", fuse=False, surface="fur")
    sickles(k, (0, 0.17, 0.6))

    # The towering comb, tongues of flame, on a fused ridge; red face, wattles and earlobes
    k.add("combbase", ell((0, H[1] + 0.005, H[2] + HR[2] * 0.9), (0.022, 0.066, 0.03)), COMB, "head", surface="skin")
    C.comb(k, H, HR, [(-0.055, 0.065, 0.034, 0.022, -0.55), (-0.028, 0.11, 0.04, 0.023, -0.25),
                      (0.002, 0.13, 0.042, 0.024, 0.05), (0.032, 0.11, 0.04, 0.023, 0.4),
                      (0.058, 0.075, 0.034, 0.022, 0.7)], color=COMB)
    k.add("wattle", ell((0.014, -0.258, 0.765), (0.022, 0.028, 0.052)), COMB, "jaw", mirror=True, surface="skin")
    k.add("earlobe", ell((0.07, -0.185, 0.812), (0.016, 0.02, 0.022)), COMB, "head", mirror=True, surface="skin")
    def face(P):
        x, y, z = np.abs(P[:, 0]), P[:, 1], P[:, 2]
        return inside(np.hypot((y - (H[1] - 0.04)) / 0.065, (z - (H[2] - 0.012)) / 0.05) - 1, 0.004) * (x > 0.02)
    k.paint(face, FACE, on=["head"])

    # The beak wide open mid-crow, a dark mouth inside
    k.add("beak", ell((0, -0.298, 0.862), (0.026, 0.05, 0.019), rot=(-24, 0, 0), deform=T.pointed), BEAK, "head",
          fuse=False, surface="shell")
    k.add("beaklow", ell((0, -0.29, 0.822), (0.021, 0.044, 0.013), rot=(26, 0, 0), deform=T.pointed), BEAK, "jaw",
          fuse=False, surface="shell")
    k.add("mouth", outward(ell((0, -0.268, 0.84), (0.02, 0.03, 0.017), sub=3)), MOUTH, "jaw", fuse=False,
          surface="skin")

    # Long yellow legs with pale spurs behind
    C.legs(k, 0.068, 0.36, 0.05, 0.0, 0.019, 0.088, 0.0145, claw_len=0.026)
    for s in (1, -1):
        root = np.array([0.072 * s, 0.012, 0.17])
        k.add("spur" + ("R" if s > 0 else "L"), T.spike(root, (0.15 * s, 1.0, -0.35), 0.055, 0.017, 0.014), SPUR,
              "leg" + ("R" if s > 0 else "L"), fuse=False, surface="shell")

    E = T.on_ell(H, HR, DE)
    C.brows(k, E, DE, EYE_HALF, 0.022, color=BROW, tilt=24, size=(0.85, 0.24))
    k.build(voxel=0.0085, smooth=4, band=0.006, wband=0.06)
    k.eye_states(lambda s: C.lay_eyes(k, s, E, DE, EYE_HALF, eye_open, srgb(FACE)))
    k.export(out_path)


if __name__ == "__main__":
    cocosor(*args())
