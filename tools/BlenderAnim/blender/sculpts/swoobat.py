# Swoobat sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/swoobat.py -- <the export's Swoobat.glb> <out.glb>
# Grown from Woobat's script (sibling("woobat")): the same soft fur spikes (its pale ruff), the same heart for a nose
# and the same charcoal bat wings, grown tall. Swoobat is a teal-blue head drawn up into a teardrop's point, the upper
# front of it a pale window parted by two dark lines; round white eyes with black pupils at the sides of a pink heart
# of a nose with a black heart in it, two white fangs under its eyes; a pale ruff of fur round its neck over a small
# blue body, two little dark feet of three claws, a short pink tail ending in a heart; great wings raised in a V.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, eye_look, args, sibling

w = sibling("woobat")

BLUE, PALE, RUFF = "#2F78A8", "#AFCDE2", "#C4DDEE"
PINK, HEART, WING = w.PINK, w.HEART, w.WING
FOOT, TAIL, FANG = "#36343C", "#EE9AAA", "#F8F6F4"

H = np.array([0.0, -0.012, 0.322])              # the head's round lower part, over its joint (0, -0.01, 0.313)
HR = np.array([0.062, 0.058, 0.06])
RUFFC, RUFFR = np.array([0.0, 0.0, 0.25]), np.array([0.062, 0.058, 0.04])
VOXEL = 0.0034


def swoobat(src, out):
    k = Kit(src, coat="fur")
    # the head: a round lower part drawn up into a point leaning a little back
    k.add("head", ell(H, HR), BLUE, "head")
    k.add("headtop", tube([H + (0, 0.002, 0.02), (0, 0.004, 0.39), (0, 0.012, 0.432)], [0.052, 0.03, 0.008]), BLUE,
          "head")
    # a slight swell under the nose
    k.add("snout", ell((0, -0.05, 0.322), (0.034, 0.016, 0.03)), BLUE, "head")

    # the ruff: a pale collar of fur spikes round the neck, over a small blue body
    k.add("ruff", ell(RUFFC, RUFFR), RUFF, "body")
    w.spikes(k, "ruffspike", RUFFC, RUFFR, 46, RUFF, "body", lambda d: d[2] < 0.6, spike=(0.022, 0.004, 0.026),
             droop=0.35, seed=3)
    k.add("belly", ell((0, 0.004, 0.198), (0.04, 0.036, 0.048)), BLUE, "body")

    # two little dark feet, three claws each
    for s, side in ((1, "R"), (-1, "L")):
        f = np.array((0.024 * s, -0.008, 0.158))
        k.add(f"foot{side}", ell(f, (0.016, 0.016, 0.017)), FOOT, f"leg{side}", fuse=False)
        for i, a in enumerate((-1, 0, 1)):
            base = f + np.array((0.009 * a, -0.004, -0.008))
            tip = base + np.array((0.008 * a, -0.006, -0.026))
            pts = [base + (dx, dy, 0) for dx in (-0.006, 0.006) for dy in (-0.005, 0.005)] + [tip]
            k.add(f"claw{side}{i}", hull(pts), FOOT, f"leg{side}", fuse=False, surface="shell")

    # the tail: a short pink cord down from under the body, ending in a heart turned a little to the +X side
    t = np.linspace(0, 2 * math.pi, 49)
    hx = 16 * np.sin(t) ** 3
    hy = 13 * np.cos(t) - 5 * np.cos(2 * t) - 2 * np.cos(3 * t) - np.cos(4 * t)
    sc = 0.034 / 32
    ex, ey = unit(math.cos(math.radians(30)), math.sin(math.radians(30)), 0), np.array([0, 0, 1.0])
    top = np.array((0.0, 0.006, 0.112))           # the heart's notch
    loop = [top + ex * x * sc + ey * (y - 5) * sc for x, y in zip(hx, hy)]
    k.add("tail", tube([(0, 0.01, 0.17), (0, 0.016, 0.14), top + (0, 0, 0.004)], [0.007, 0.0068, 0.0066]), TAIL,
          "tail", fuse=False)
    k.add("tailheart", tube(loop, [0.0065] * len(loop), per=3), TAIL, "tail", fuse=False)

    # the wings: Woobat's grown tall, raised in a V, a claw where the leading edge bends
    root = np.array((0.0, -0.02)); spur = np.array((0.025, 0.11)); tip = np.array((0.13, 0.205))
    pts = [np.array((0.158, 0.125)), np.array((0.14, 0.045)), np.array((0.085, -0.025))]
    outline = w.wing_outline(root, spur, tip, pts, np.array((0.07, 0.085)), lead=0.2, depth=0.4)
    w.wing(k, "wing", "wing{s}", (0.042, 0.035, 0.29), unit(1, 0.3, 0), unit(0, 0.2, 1), outline, 0.012)

    # the pale window up the front of the head's point, parted by two dark lines and edged by one below
    def window(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        m = inside(0.352 - z, 0.0015) * inside(y - (H[1] + 0.008), 0.002) * inside(np.abs(x) - 0.052, 0.002)
        gap = inside(np.abs(np.abs(x) - 0.014) - 0.0032, 0.0012)
        return m * (1 - gap)
    k.paint(window, PALE, on=["head"])

    # the nose: a pink heart with a black heart in it
    n = unit(0, -1, 0.05)
    k.decal((0, -0.07, 0.322), n, (0.031, 0.03), PINK, outline=w.heart_outline(), rings=16, segs=72, eps=0.003)
    k.decal((0, -0.07, 0.32), n, (0.017, 0.0165), HEART, outline=w.heart_outline(), rings=12, segs=72, eps=0.006)

    # the mouth: a thin dark line curving under the nose, a white fang hanging from each end
    CB, CI, CF = srgb(BLUE), srgb("#142838"), srgb(FANG)
    def mouth(u, v):
        c = np.tile(CB, (len(u), 1))
        line = 0.25 + 0.25 * u * u
        c[(np.abs(v - line) < 0.13) & (np.abs(u) < 0.86)] = CI
        for x in (-0.62, 0.62):
            fang = (v < line) & (v > -0.8) & (np.abs(u - x) < 0.2 * np.clip((v + 0.8) / (line + 0.8), 0, 1))
            c[fang] = CF
        return c
    k.decal((0, -0.064, 0.293), unit(0, -1, -0.25), (0.046, 0.017), mouth, rings=16, segs=64, eps=0.004)

    # the two fangs themselves, loose so that they keep their points, under the mouth's ends
    for s, side in ((1, "R"), (-1, "L")):
        b = np.array((0.029 * s, -0.06, 0.296))
        pts = [b + (dx, dy, 0) for dx in (-0.0065, 0.0065) for dy in (-0.004, 0.004)]
        pts.append(b + (0.001 * s, -0.007, -0.02))
        k.add(f"fang{side}", hull(pts), FANG, "head", fuse=False, surface="shell")

    k.build(voxel=VOXEL, smooth=3, band=0.005)

    # the eyes: round, white with a thin dark rim and a big black pupil toward the nose
    LID, INK, WH = srgb(BLUE), srgb("#0E1620"), srgb("#F8F8F6")
    def eye(u, v):
        c = np.tile(WH, (len(u), 1))
        c[u * u + v * v > 0.86 ** 2] = INK
        c[((u + 0.22) / 0.5) ** 2 + ((v + 0.02) / 0.56) ** 2 < 1] = INK
        c[((u + 0.36) / 0.15) ** 2 + ((v - 0.22) / 0.15) ** 2 < 1] = WH
        return c
    ED = unit(0.6, -0.76, -0.12)
    E = H + ED / np.linalg.norm(ED / HR)
    def eyes(state):
        k.decal(E, ED, (0.019, 0.021), eye_look(state, eye, LID, INK, line=0.18), rings=20, segs=56, eps=0.004,
                mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    swoobat(*args())
