# Mega Meganium sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/meganiummega.py -- <the export's Meganium-Mega.glb> <out.glb>
#
# The design, from the references: Meganium whose flower has grown into the largest mass of the design, a great
# rosette of four tiers round the head, stepping back and down and growing wider: red pointed petals round the face,
# then pink, then pale pink, then white, the widest, below; thin white streaks down the red and pink petals and white
# points between the pink ones. The head sits in the red heart with no neck showing, its mouth open (a dark mouth with
# a pink tongue), eyes of yellow with a dark middle. The feelers rise and end each in a yellow loop. A longer, lower
# body than Meganium's with a long tapering tail, short legs on darker green feet with a frilled cuff and white claws.
#
# Grown from meganium.py and chikorita.py (sibling): their trunk, legs, claws, way of making eyes; the Mega's flower,
# head, mouth, feelers, feet and tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, sheet, srgb, unit, inside, args, outward, sibling

c = sibling("chikorita")
m = sibling("meganium")

GREEN = m.GREEN
FOOT = "#4F9E45"
RED, PINK, BLUSH, WHITE_P = "#E0343F", "#EE7396", "#F7B6C8", "#FBF3F4"
YELLOW = m.YELLOW
CLAW = c.CLAW
IRIS, PUPIL, WHITE, INK = m.IRIS, m.PUPIL, c.WHITE, c.INK
MOUTH, TONGUE = "#5C1A26", "#E9708A"

H = np.array([0, -0.275, 0.70])                         # the head's middle, in the red heart
HR = np.array([0.1, 0.112, 0.094])
SNOUT = (H + (0, -0.072, -0.03), (0.077, 0.07, 0.066))
NECK = [(0, -0.08, 0.38), (0, -0.16, 0.52), (0, -0.22, 0.64)]
NECK_R = [0.11, 0.085, 0.075]
TRUNK = [((0, -0.06, 0.32), (0.15, 0.15, 0.15)),            # chest
         ((0, 0.09, 0.32), (0.155, 0.18, 0.148)),          # middle
         ((0, 0.23, 0.3), (0.14, 0.13, 0.138)),            # rump
         ((0, 0.07, 0.2), (0.135, 0.21, 0.12))]            # belly, low
LEGS = dict(m.LEGS)
EYE_D = unit(0.64, -0.72, 0.2)
EYE_HALF = (0.033, 0.04)

FN = unit(0, -1, 0.22)                                  # the way the rosette faces
# (tier, heart, petals, inner radius, outer radius, half-span in degrees, colour, turn)
TIERS = [("white", (0, -0.025, 0.535), 12, 0.06, 0.4, 18, WHITE_P, 0.5),
         ("blush", (0, -0.065, 0.575), 11, 0.06, 0.34, 20, BLUSH, 0.0),
         ("pink", (0, -0.13, 0.625), 10, 0.06, 0.28, 22, PINK, 0.5),
         ("red", (0, -0.19, 0.675), 8, 0.05, 0.215, 26, RED, 0.0)]


def tier(k, tag, F, n, r0, R, span_deg, col, turn, cup=0.08, thick=0.022, streak=None, points=None, bone="body"):
    """One ring of broad petals, each rounded to a point, cupped forward, round the heart F in the plane facing FN;
    a white streak down each petal's middle and white points between petals where asked."""
    F = np.array(F, float)
    span = math.radians(span_deg)
    e1 = np.cross(FN, (0, 0, 1.0)); e1 /= np.linalg.norm(e1)
    e2 = np.cross(FN, e1)
    for i in range(n):
        a = 2 * math.pi * (i + turn) / n + math.pi / 2
        lift = 0.007 * (i % 2)

        def at(u, v, a=a, lift=lift):
            phi = a + span * (2 * (0.015 + 0.97 * v) - 1)
            w = (phi - a) / span
            Rp = R * (0.7 + 0.3 * np.clip(1 - np.abs(w), 0, 1) ** 1.4)
            t = 0.015 + 0.97 * u
            r = r0 + (Rp - r0) * t
            ph = a + (phi - a) * (0.55 + 0.45 * np.sqrt(t))
            cp = cup * t ** 1.5 + lift
            return (F[None, None] + (r * np.cos(ph))[..., None] * e1[None, None]
                    + (r * np.sin(ph))[..., None] * e2[None, None] + cp[..., None] * FN[None, None])
        k.add(f"{tag}{i}", outward(sheet(at, 22, 16, thick)), col, bone, fuse=False, surface="leaf")
        if streak:
            o = math.cos(a) * e1 + math.sin(a) * e2
            tg = -math.sin(a) * e1 + math.cos(a) * e2
            def mid(t):
                r = r0 + (R - r0) * t
                return F + o * r + FN * (cup * t ** 1.5 + lift + thick / 2)
            p0, p1, pm = mid(0.5), mid(0.93), mid(0.68)
            pts = [p0 + tg * 0.009, p0 - tg * 0.009, p0 + FN * 0.004 + tg * 0.006, p0 + FN * 0.004 - tg * 0.006,
                   p1 + FN * 0.002, pm + FN * 0.006, pm - FN * 0.001]
            k.add(f"{tag}streak{i}", hull(pts), streak, bone, fuse=False, surface="leaf")
    if points:
        for i in range(n):
            a = 2 * math.pi * (i + turn + 0.5) / n + math.pi / 2
            o = math.cos(a) * e1 + math.sin(a) * e2
            tg = -math.sin(a) * e1 + math.cos(a) * e2
            base = F + o * (R * 0.72) + FN * 0.02
            pts = [base + tg * 0.022, base - tg * 0.022, base + FN * 0.016, base - FN * 0.012,
                   base + o * (R * 0.42) + FN * 0.004]
            k.add(f"{tag}point{i}", hull(pts), points, bone, fuse=False, surface="leaf")


def feelers(k):
    """Two feelers from the crown, each rising out and ending in a loop facing forward."""
    top = c.hit([(H, HR)], H, (0, 0.05, 1))
    for s in (1, -1):
        b = top + (0.03 * s, 0.0, -0.014)
        Lc = b + (0.065 * s, 0.01, 0.14)
        R = 0.043
        pts = [b, b + (0.01 * s, 0.004, 0.045), b + (0.026 * s, 0.008, 0.08)]
        for th in np.radians(np.linspace(-125, 205, 34)):
            pts.append(Lc + (R * math.cos(th) * s, 0.0, R * math.sin(th)))
        radii = [0.016, 0.0155, 0.015] + [0.0145] * 32 + [0.013, 0.012]
        k.add(f"feeler{'R' if s > 0 else 'L'}", outward(tube(pts, radii, seg=12, per=3)), YELLOW, "head",
              fuse=False, surface="leaf")


def cuffs(k):
    """A frilled cuff of darker green lobes round each ankle."""
    for name, y, bone in (("cuff", LEGS["front"], "leg{s}"), ("hcuff", LEGS["hind"], "hind{s}")):
        cx, cy = LEGS["x"] + 0.006, y - 0.006
        for j in range(9):
            t = 2 * math.pi * (j + 0.5) / 9
            p = (cx + 0.066 * math.cos(t), cy + 0.066 * math.sin(t), 0.07 + 0.008 * (j % 2))
            k.add(f"{name}{j}", ell(p, (0.013, 0.027, 0.024), rot=(0, 0, math.degrees(t)), sub=2), FOOT, bone,
                  fuse=False, mirror=True, surface="fur")


def mouth_look(u, v):
    """An open mouth: a wide dark opening, its top edge a gentle arc, a pink tongue in its floor."""
    G, D, T = srgb(GREEN), srgb(MOUTH), srgb(TONGUE)
    col = np.tile(G, (len(u), 1))
    top = 0.45 - 0.35 * u * u
    open_ = v < top
    col[open_] = D
    col[open_ & ((u / 0.62) ** 2 + ((v + 0.75) / 0.55) ** 2 < 1)] = T
    return col


def meganium_mega(src, out):
    k = Kit(src, coat="fur")
    c.trunk(k, TRUNK, GREEN)
    c.legs(k, LEGS, GREEN, CLAW)
    k.add("neck", tube(NECK, NECK_R), GREEN, "head")
    k.add("head", ell(H, HR), GREEN, "head")
    k.add("snout", ell(*SNOUT), GREEN, "head")
    k.add("tail", tube([(0, 0.26, 0.31), (0, 0.4, 0.3), (0, 0.53, 0.25), (0, 0.65, 0.2), (0, 0.72, 0.205)],
                       [0.095, 0.068, 0.042, 0.022, 0.013]), GREEN, "tail")
    # the feet darker below the cuff
    k.paint(lambda P: inside(P[:, 2] - 0.072, 0.004) * inside(np.abs(P[:, 0]) - 0.2), FOOT)
    cuffs(k)

    for tag, F, n, r0, R, span, col, turn in TIERS:
        tier(k, tag, F, n, r0, R, span, col, turn, cup={"white": 0.0, "blush": 0.02}.get(tag, 0.08),
             streak=WHITE_P if tag in ("red", "pink") else None,
             points=WHITE_P if tag == "pink" else None)
    feelers(k)

    k.decal(c.hit([SNOUT], SNOUT[0], (0, -1, -0.62)), unit(0, -1, -0.55), (0.048, 0.032), mouth_look,
            eps=0.003, rings=18, segs=72)

    k.build(voxel=0.0078, band=0.008)
    centre = c.hit([(H, HR)], H, EYE_D)
    look = c.eye_colours(WHITE, IRIS, PUPIL, INK, at=(-0.08, -0.04), size=(0.7, 0.8), mid=(0.3, 0.55),
                         light=((0.16, 0.34), (0.2, 0.18)))
    k.eye_states(lambda state: c.lay_eyes(k, state, centre, EYE_D, EYE_HALF, look, GREEN, INK))
    k.export(out)


if __name__ == "__main__":
    meganium_mega(*args())
