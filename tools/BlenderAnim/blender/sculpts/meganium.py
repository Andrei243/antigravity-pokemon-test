# Meganium sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/meganium.py -- <the export's Meganium.glb> <out.glb>
#
# The design, from the references: Bayleef grown into a tall pale green dinosaur: a deep barrel of a body on four
# thick legs with white claws, a short pointed tail; a long thick neck rising from the middle of a great pink flower
# of five broad petals edged in ruffled white, five yellow points round the neck at the flower's heart; a small head
# with a round snout, eyes of yellow with a dark middle under a dark rim, a small smile; two long yellow feelers with
# round tips curving up and back from the crown.
#
# Grown from chikorita.py and bayleef.py (sibling): their trunk, legs, claws, way of making eyes and smile, and
# Bayleef's neck and tail; Meganium's sizes, colours, flower and feelers.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, sheet, srgb, unit, args, outward, sibling

c = sibling("chikorita")
bl = sibling("bayleef")

GREEN = "#A6D873"
PINK, PETALWHITE, YELLOW = "#EF6A8E", "#FBF2F4", "#F4CF3C"
CLAW = c.CLAW
IRIS, PUPIL, WHITE, INK = "#EAA626", "#1E1410", c.WHITE, c.INK
SMILE = "#3E6A34"

H = np.array([0, -0.205, 0.80])                         # the head's middle
HR = np.array([0.094, 0.11, 0.083])
SNOUT = (H + (0, -0.07, -0.024), (0.072, 0.068, 0.061))
NECK = [(0, -0.12, 0.36), (0, -0.175, 0.5), (0, -0.175, 0.63), (0, -0.16, 0.73), (0, -0.18, 0.79)]
NECK_R = [0.115, 0.088, 0.078, 0.074, 0.07]
TRUNK = [((0, -0.08, 0.33), (0.15, 0.14, 0.15)),            # chest
         ((0, 0.07, 0.33), (0.16, 0.18, 0.15)),            # middle
         ((0, 0.2, 0.31), (0.145, 0.12, 0.14)),            # rump
         ((0, 0.06, 0.2), (0.14, 0.2, 0.125))]             # belly, low
LEGS = dict(x=0.108, front=-0.08, hind=0.2, top=0.24, ankle=0.058, r=0.062, shoulder=(0.078, 0.088, 0.12),
            thigh=(0.084, 0.098, 0.125), foot=(0.064, 0.072, 0.034), toe=0.012, claws=3, spread=0.03, claw=0.024,
            haunch_at=(0.016, 0.0, 0.03))
EYE_D = unit(0.66, -0.7, 0.2)
EYE_HALF = (0.029, 0.035)

F = np.array([0, -0.16, 0.43])                           # the flower's heart
FN = unit(0, -1, 0.75)                                  # the way the flower faces


def flower(k, F, FN, petals, r0, R, span, cols, bone="body"):
    """Five broad petals round the neck in the plane facing FN, each a pink plate cupped a little toward the front,
    over a white one a little larger with a ruffled rim; yellow points between them at the heart."""
    pink, white, yellow = cols
    e1 = np.cross(FN, (0, 0, 1.0)); e1 /= np.linalg.norm(e1)
    e2 = np.cross(FN, e1)
    for i in range(petals):
        a = 2 * math.pi * i / petals + math.pi / 2          # the first straight up the plane
        lift = 0.006 * (i % 2)

        def plate(scale, ruffle, back, thick, name, col):
            def at(u, v):
                phi = a + span * (2 * (0.015 + 0.97 * v) - 1)
                w = (phi - a) / span
                Rp = R * scale * (0.78 + 0.22 * np.sqrt(np.clip(1 - w * w, 0, 1)))
                Rp = Rp + ruffle * np.sin(13 * phi) * (u > 0.6)
                t = 0.015 + 0.97 * u
                r = r0 + (Rp - r0) * t
                narrow = 0.55 + 0.45 * np.sqrt(t)                # each petal narrows toward the heart
                ph = a + (phi - a) * narrow
                cup = 0.06 * t ** 1.5 - back + lift
                return (F[None, None] + (r * np.cos(ph))[..., None] * e1[None, None]
                        + (r * np.sin(ph))[..., None] * e2[None, None] + cup[..., None] * FN[None, None])
            k.add(name, outward(sheet(at, 28, 22, thick)), col, bone, fuse=False, surface="leaf")
        plate(1.0, 0.0, 0.038, 0.016, f"back{i}", pink)         # the back, pink, inside the white rim
        plate(1.1, 0.013, 0.016, 0.02, f"rim{i}", white)
        plate(1.0, 0.0, 0.0, 0.022, f"petal{i}", pink)
    for i in range(petals):
        a = 2 * math.pi * (i + 0.5) / petals + math.pi / 2
        o = math.cos(a) * e1 + math.sin(a) * e2
        t = -math.sin(a) * e1 + math.cos(a) * e2
        base = F + o * (r0 + 0.01) + FN * 0.035
        pts = [base + t * 0.024, base - t * 0.024, base + o * 0.075 + FN * 0.006,
               base + FN * 0.016, base - FN * 0.01, base + o * 0.035 + FN * 0.02]
        k.add(f"point{i}", hull(pts), yellow, bone, fuse=False, surface="leaf")


def feelers(k, H, HR, col, length=0.2):
    """Two feelers from the crown, curving up and back, a round knob at each tip."""
    top = c.hit([(H, HR)], H, (0, 0.05, 1))
    for s in (1, -1):
        b = top + (0.026 * s, 0.0, -0.012)
        pts = [b, b + (0.02 * s, 0.03, 0.07), b + (0.04 * s, 0.09, 0.11), b + (0.05 * s, 0.15, length * 0.8)]
        k.add(f"feeler{'R' if s > 0 else 'L'}", outward(tube(pts, [0.013, 0.012, 0.011, 0.011], seg=12, per=6)),
              col, "head", fuse=False, surface="leaf")
        k.add(f"knob{'R' if s > 0 else 'L'}", ell(pts[-1] + (0.008 * s, 0.012, 0.012), (0.022, 0.022, 0.028),
                                                     rot=(-35, 0, 0), sub=3), col, "head", fuse=False, surface="leaf")


def meganium(src, out):
    k = Kit(src, coat="fur")
    c.trunk(k, TRUNK, GREEN)
    c.legs(k, LEGS, GREEN, CLAW)
    k.add("neck", tube(NECK, NECK_R), GREEN, "head")
    k.add("head", ell(H, HR), GREEN, "head")
    k.add("snout", ell(*SNOUT), GREEN, "head")
    k.add("tail", tube([(0, 0.27, 0.31), (0, 0.36, 0.33), (0, 0.45, 0.36)], [0.08, 0.045, 0.014]), GREEN, "tail")
    # a small point on the back before the tail
    k.add("ridge", hull([(0.03, 0.2, 0.42), (-0.03, 0.2, 0.42), (0, 0.13, 0.43), (0, 0.27, 0.41),
                         (0, 0.24, 0.5)]), GREEN, "body", fuse=False, surface="fur")

    flower(k, F, FN, 5, 0.07, 0.24, math.radians(40), (PINK, PETALWHITE, YELLOW))
    feelers(k, H, HR, YELLOW, length=0.15)

    k.decal(c.hit([SNOUT], SNOUT[0], (0, -1, -0.45)), unit(0, -1, -0.35), (0.026, 0.015),
            c.smile(GREEN, SMILE, width=0.32), eps=0.003, rings=16, segs=72)

    k.build(voxel=0.0078, band=0.008)
    centre = c.hit([(H, HR)], H, EYE_D)
    look = c.eye_colours(WHITE, IRIS, PUPIL, INK, at=(-0.08, -0.04), size=(0.7, 0.8), mid=(0.3, 0.55),
                         light=((0.16, 0.34), (0.2, 0.18)))
    k.eye_states(lambda state: c.lay_eyes(k, state, centre, EYE_D, EYE_HALF, look, GREEN, INK))
    k.export(out)


if __name__ == "__main__":
    meganium(*args())
