# Golurk's Mega Evolution sculpted on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/golurkmega.py -- <the export's Golurk-Mega.glb> <out.glb>
#
# From the references: Golurk standing taller, its seal gone. Two great pale yellow balls of light behind the
# shoulders, each as wide as the chest is deep and standing higher than the head (the design's largest mass), and
# from each a tall wavy purple flame rising with a curl at its tip, a small purple wisp behind the head too; a broad
# X of yellow light bound across the chest, the arms of the X running from the shoulders down to the waist's sides,
# a small brown tab hanging under its crossing; the chest pale clay now, deep blue only at its top round the head;
# the shoulders' tops and their horns deep blue, a dark square spiral on a pale plate on each (the base's spiral was
# yellow: the pictures make it dark); the brown bands at the forearms and ankles grown into thick drums of yellow
# light with a brown stripe round their middle, yellow spikes standing out of them; the rest Golurk's: the knot
# headed small head with its two glyph eyes, the skirt, the column legs on flat feet, the huge fists.
# Masses, largest first (3D picture, against the chest's width 0.35): the balls (diameter 0.85x), the flames (each as
# tall as the chest), the chest, the forearms and fists, the ankle drums, the X (bands 0.13x wide).
# Grown from golurk.py (by sibling): its frame (moved up by the Mega's joints), its head, its stones and bands, its
# glyph eyes.
# The skeleton: body (0, 0, 0.4), arms (+-0.16, 0, 0.605), head (0, -0.02, 0.67), legs (+-0.08, 0, 0.259).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, inside, args, boxy, hull, slab, lathe, tri, sibling, blade, \
    outward, sheet

gk = sibling("golurk")
go = gk.go
stone, spiral_panel = go.stone, go.spiral_panel
DEEP, ROCK, ROCKSHADE, STRAP = gk.DEEP, gk.ROCK, gk.ROCKSHADE, gk.STRAP
LIGHT, LIGHTGLOW, SPIKE = "#F4D560", "#F2D058", "#F0CC50"
ORB, ORBGLOW = "#F6E6A6", "#F4E29C"
PURPLE, PURPLE2 ="#9C62B4", "#B98ACB"
PLATE, INKS, TAB, HOLE = "#B6D4D6", "#2B3A46", "#A8723F", "#5A3A22"

# --- the frame: Golurk's, moved up by the Mega's joints ----------------------------------------------------------------
UP = 0.045
gk.CHEST_C = CHEST_C = np.array([0.0, 0.0, 0.592 + 0.04])
CHEST_R = gk.CHEST_R
gk.HEAD_C = HEAD_C = np.array([0.0, -0.022, 0.772 + 0.05])
gk.EYE_Z = 0.775 + 0.05
SHOULDER = np.array([0.245, 0.0, 0.665 + UP])
FORE = np.array([0.272, -0.07, 0.415 + 0.035])
FIST = np.array([0.276, -0.095, 0.305 + 0.03])
LEG_X = 0.095
LIFT = 0.02                                   # the legs' and skirt's
ORB_C, ORB_R = np.array([0.3, 0.12, 0.8]), 0.15


def chest(k):
    k.add("chest", ell(CHEST_C, CHEST_R, deform=boxy(2.4)), ROCK, "body")
    k.add("waist", ell((0, 0.0, 0.49 + 0.04), (0.158, 0.13, 0.06)), ROCK, "body")
    k.add("belt", lathe((0, 0, 0.468 + 0.04), (0, 0, 1), [(0.12, -0.022), (0.166, -0.022), (0.171, 0.0),
                                                           (0.166, 0.022), (0.12, 0.022), (0.12, -0.022)], seg=56),
          ROCKSHADE, "body", fuse=False)
    # deep blue only at the top round the head, its edge jagged
    def top(P):
        th = np.arctan2(P[:, 0], -P[:, 1])
        edge = 0.72 + 0.015 * tri(th * 2.4)
        return inside(edge - P[:, 2], 0.0025)
    k.paint(top, DEEP, on=["chest"])


def chest_point(x, z, lift):
    """The chest's front surface over (x, z), and its normal, lifted off by lift: Golurk's chest is a superellipsoid."""
    n_ = 2.4
    a = np.abs(x / CHEST_R[0]) ** n_ + np.abs((z - CHEST_C[2]) / CHEST_R[2]) ** n_
    y = -CHEST_R[1] * np.clip(1 - a, 0.002, 1) ** (1 / n_)
    g = np.stack([np.sign(x) * np.abs(x / CHEST_R[0]) ** (n_ - 1) / CHEST_R[0],
                  np.sign(y) * np.abs(y / CHEST_R[1]) ** (n_ - 1) / CHEST_R[1],
                  np.sign(z - CHEST_C[2]) * np.abs((z - CHEST_C[2]) / CHEST_R[2]) ** (n_ - 1) / CHEST_R[2]], -1)
    g /= np.linalg.norm(g, axis=-1, keepdims=True)
    P = np.stack([x, y + CHEST_C[1], z], -1)
    return P + g * lift


def cross(k):
    """The X of yellow light across the chest: two bands laid on its front from the shoulders to the waist's sides,
    the second a little prouder at the crossing, and the brown tab under it."""
    for i, (x0, z0, x1, z1) in enumerate(((-0.14, 0.75, 0.145, 0.51), (0.14, 0.75, -0.145, 0.51))):
        def at(u, v, x0=x0, z0=z0, x1=x1, z1=z1, i=i):
            d = np.array([x1 - x0, z1 - z0]); d /= np.linalg.norm(d)
            nrm = np.array([-d[1], d[0]])
            x = x0 + (x1 - x0) * u + nrm[0] * (v - 0.5) * 0.05
            z = z0 + (z1 - z0) * u + nrm[1] * (v - 0.5) * 0.05
            return chest_point(x, z, 0.008 + 0.006 * i)
        k.add(f"cross{i}", outward(sheet(at, 30, 4, 0.012)), LIGHT, "body", fuse=False, glow=LIGHTGLOW,
              surface="skin")
    c = chest_point(np.array(0.0), np.array(0.585), 0.012)
    n = unit(0, -1, 0.12)
    ex_, ez_ = np.array([1.0, 0, 0]), np.cross(n, (1.0, 0, 0))
    ez_ = -ez_ if ez_[2] < 0 else ez_
    tab = [(-0.022, -0.05), (0.022, -0.05), (0.026, -0.04), (0.026, 0.03), (-0.026, 0.03), (-0.026, -0.04)]
    k.add("tab", slab(tab, c, ex_, ez_, 0.014), TAB, "body", fuse=False, surface="leather")
    k.add("tabhole", lathe(c + ez_ * (-0.022) - n * 0.004, -n, [(0, 0.004), (0.009, 0.004), (0.01, 0.009),
                                                                 (0, 0.0095)]), HOLE, "body", fuse=False,
          surface="leather")


def drum(c, r_in, r_out, height, axis, spikes, spike_len, name, k, bone, phase=0.0):
    """A thick drum of yellow light round a limb, a brown stripe round its middle and yellow spikes out of it."""
    h = height
    a = unit(*axis)
    k.add(name, lathe(c, a, [(r_in, -h / 2), (r_out, -h / 2), (r_out + 0.004, -h * 0.4), (r_out + 0.004, h * 0.4),
                             (r_out, h / 2), (r_in, h / 2), (r_in, -h / 2)], seg=56), LIGHT, bone, fuse=False,
          glow=LIGHTGLOW, surface="skin")
    k.add(name + "stripe", lathe(c, a, [(r_in + 0.01, -h * 0.13), (r_out + 0.009, -h * 0.13),
                                        (r_out + 0.009, h * 0.13), (r_in + 0.01, h * 0.13),
                                        (r_in + 0.01, -h * 0.13)], seg=56), STRAP, bone, fuse=False,
          surface="leather")
    e1 = np.cross(a, (0, 0, 1.0)) if abs(a[2]) < 0.9 else np.array([1.0, 0, 0])
    e1 /= np.linalg.norm(e1)
    e2 = np.cross(a, e1)
    for j, t in enumerate(spikes):
        t = math.radians(t) + phase
        rad = math.cos(t) * e1 + math.sin(t) * e2
        b = c + rad * (r_out - 0.005)
        w = 0.024
        tg = np.cross(a, rad)
        k.add(f"{name}spike{j}", hull([b + tg * w, b - tg * w, b + a * w * 0.9, b - a * w * 0.9,
                                       b + rad * spike_len + a * 0.004]), SPIKE, bone, fuse=False, surface="shell")


def shoulders(k):
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"shoulder{side}", stone(SHOULDER * m, (0.09, 0.088, 0.092), rot=(0, 0, 8 * s), seed=21 + (s > 0),
                                       cham=0.32, jit=0.06), ROCK, f"arm{side}", fuse=False)
        # the deep blue cap over the shoulder's top
        k.add(f"cap{side}", stone((SHOULDER + (0.005, 0.0, 0.05)) * m, (0.088, 0.086, 0.05), rot=(0, 0, 8 * s),
                                  seed=23 + (s > 0), cham=0.3, jit=0.05), DEEP, f"arm{side}", fuse=False)
        base = (SHOULDER + (0.035, 0.0, 0.08)) * m
        tip = (SHOULDER + (0.085, 0.01, 0.185)) * m
        pts = [base + np.array([dx, dy, 0.0]) for dx in (-0.035, 0.035) for dy in (-0.034, 0.034)]
        pts += [base + np.array([dx * 0.6, dy * 0.6, -0.02]) for dx in (-0.035, 0.035) for dy in (-0.034, 0.034)]
        pts.append(tip)
        k.add(f"horn{side}", hull(pts), DEEP, f"arm{side}", fuse=False)
        nrm = unit(0.75 * s, -0.62, 0.0)
        c = (SHOULDER + (0, 0, -0.012)) * m + nrm * 0.083
        spiral_panel(k, f"spiral{side}", c, nrm, (0, 0, 1), 0.042, f"arm{side}", plate=PLATE, ink=INKS, turn=0)


def orbs(k):
    """The great balls of light behind the shoulders and the purple flames rising from them."""
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        c = ORB_C * m
        k.add(f"orb{side}", ell(c, (ORB_R, ORB_R * 0.92, ORB_R), sub=4), ORB, f"arm{side}", fuse=False,
              glow=ORBGLOW, surface="skin")
        # a flame of smoke: broad at its foot in the ball's top, waving up and out, its tip curling inward
        pts = [c + np.array([s * x, y, z]) for x, y, z in
               ((0.0, 0.03, 0.08), (0.035, 0.04, 0.15), (-0.01, 0.05, 0.22), (0.04, 0.05, 0.28),
                (0.02, 0.05, 0.33), (-0.02, 0.05, 0.345), (-0.03, 0.05, 0.315))]
        k.add(f"flame{side}", outward(blade(pts, [0.1, 0.085, 0.07, 0.056, 0.042, 0.03, 0.022], (0, 1, 0), 0.45)),
              PURPLE, f"arm{side}", fuse=False, surface="skin")
        k.add(f"flamefoot{side}", ell(c + np.array([0, 0.03, 0.095]), (0.085, 0.04, 0.05)), PURPLE2, f"arm{side}",
              fuse=False, surface="skin")
    # a small wisp behind the head
    pts = [np.array(p) for p in ((0.0, 0.08, 0.78), (0.02, 0.1, 0.86), (-0.02, 0.11, 0.93), (0.0, 0.11, 0.98))]
    k.add("wisp", outward(blade(pts, [0.04, 0.032, 0.022, 0.012], (0, 1, 0), 0.5)), PURPLE, "body", fuse=False,
          surface="skin")


def arms(k):
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"arm{side}", tube([np.array([0.2, 0.0, 0.645]) * m, np.array([0.255, -0.035, 0.575]) * m,
                                  (FORE + (0, 0.02, 0.05)) * m], [0.048, 0.044, 0.048]), ROCK, f"arm{side}")
        k.add(f"fore{side}", stone(FORE * m, (0.086, 0.084, 0.08), seed=31 + (s > 0), cham=0.42, jit=0.05), ROCK,
              f"arm{side}", fuse=False)
        k.add(f"fist{side}", stone(FIST * m, (0.09, 0.088, 0.072), rot=(0, 0, -6 * s), seed=35 + (s > 0), cham=0.36,
                                   jit=0.07), ROCK, f"arm{side}", fuse=False)
        k.add(f"knuckle{side}", stone((FIST + (0.0, -0.055, -0.01)) * m, (0.08, 0.04, 0.055), seed=39 + (s > 0),
                                      cham=0.4), ROCKSHADE, f"arm{side}", fuse=False)
        drum((FORE + (0.0, 0.0, 0.04)) * m, 0.07, 0.112, 0.1, (0, 0, 1), (-90, -45, 0, 45, 90), 0.085,
             f"wristdrum{side}", k, f"arm{side}", phase=0 if s > 0 else math.pi)
        nrm = unit(0.8 * s, -0.6, 0.0)
        c = FORE * m + np.array([0.0, 0.0, -0.025]) + nrm * 0.088
        spiral_panel(k, f"forepanel{side}", c, nrm, (0, 0, 1), 0.04, f"arm{side}", plate=DEEP, ink=INKS, turn=0)


def legs(k):
    skirt = [(0, 0.385), (0.13, 0.385), (0.13, 0.33), (0.172, 0.325), (0.168, 0.4), (0.158, 0.47), (0, 0.47)]
    k.add("skirt", lathe((0, 0, LIFT + 0.02), (0, 0, 1), skirt, seg=56), ROCK, "body", fuse=False)
    k.add("under", lathe((0, 0, LIFT + 0.02), (0, 0, 1), [(0, 0.378), (0.129, 0.378), (0.129, 0.384), (0, 0.384)],
                         seg=40), gk.UNDER, "body", fuse=False)
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"leg{side}", tube([np.array([0.085, 0.0, 0.4 + LIFT]) * m, np.array([LEG_X, -0.005, 0.21]) * m,
                                  np.array([LEG_X, -0.01, 0.07]) * m], [0.068, 0.064, 0.064]), ROCK, f"leg{side}")
        drum(np.array([LEG_X, -0.01, 0.13]) * m, 0.05, 0.088, 0.09, (0, 0, 1), (-90, -30, 30, 90), 0.075,
             f"ankle{side}", k, f"leg{side}", phase=0 if s > 0 else math.pi)
        k.add(f"foot{side}", stone(np.array([LEG_X + 0.004, -0.035, 0.03]) * m, (0.086, 0.106, 0.032),
                                   seed=41 + (s > 0), cham=0.3, jit=0.04), ROCK, f"leg{side}", fuse=False)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    chest(k)
    cross(k)
    gk.head(k)
    shoulders(k)
    orbs(k)
    arms(k)
    legs(k)
    k.build(voxel=gk.VOXEL, smooth=4, band=0.005)
    k.eye_states(lambda state: gk.lay_eyes(k, state))
    k.export(out)
