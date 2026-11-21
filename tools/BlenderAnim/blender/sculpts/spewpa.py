# Spewpa sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/spewpa.py -- <the export's Spewpa.glb> <out.glb>
#
# Grown from scatterbug.py (sibling("scatterbug")): its grey head with the seam down the middle, its upright tan eyes
# and the way they are laid; Larvesta's jagged fluff deform (sibling("larvesta")) for the coat.
#
# From the references: the grey round head of Scatterbug, sunk to its eyes in a great ball of white fluff; a tan cap on
# the crown narrowing to a point on the brow, split by the dark seam; two small tan dots on the brow over the eyes; the
# eyes big upright tan ovals, each with a dark brown rounded oblong of a pupil and a white glint. The fluff is jagged
# all round in white points, lavender grey toward the back underneath, and runs back into one long point; red, black
# and orange square flakes stick to it. (The task's "zigzag marks" are, in the pictures, these square flakes.)
# Bones: root, body (0, 0, 0.08), head (0, -0.06, 0.09).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, srgb, inside, smoothstep, unit, args, polygon, sibling

sb = sibling("scatterbug")
lv = sibling("larvesta")

GREY, SEAM, TAN = sb.GREY, sb.SEAM, sb.TAN
WHITE, SHADE, PUPIL, GLINT = "#F4F4F6", "#A39DBE", "#3A2A26", "#FFFFFF"
RED, BLACK, ORANGE = "#E0382C", "#1E1C22", "#F2A24A"

HC = np.array([0.0, -0.046, 0.12])           # the head's middle
HR = np.array([0.066, 0.056, 0.058])
FC = np.array([0.0, 0.022, 0.07])            # the ball of fluff's middle
FR = np.array([0.094, 0.094, 0.07])


def head(k):
    k.add("skull", ell(HC, HR, sub=5), GREY, "head")
    def cap(P):
        z0 = HC[2] + 0.018
        w = 0.006 + 0.62 * (P[:, 2] - z0)
        return (inside(np.abs(P[:, 0]) - w, 0.0015) * inside(z0 - P[:, 2], 0.002)
                * inside(P[:, 1] - (HC[1] + 0.03), 0.004))
    k.paint(cap, TAN, on=["skull"])
    def seam(P):
        q = np.sqrt((((P - HC) / HR) ** 2).sum(1))
        return (inside(np.abs(P[:, 0]) - 0.0022, 0.0012) * inside(np.abs(q - 1) - 0.12, 0.01)
                * inside((HC[2] + 0.008) - P[:, 2], 0.003))
    k.paint(seam, SEAM, on=["skull"])
    # two small tan dots on the brow over the eyes
    p, n = sb.on_head((0.36, -0.78, 0.5), HC, HR)
    k.decal(p, n, (0.0085, 0.0085), srgb(TAN), eps=0.0015, rings=10, segs=36, mirror=True)


def fluff(k):
    k.add("fluff", ell(FC, FR, deform=lv.fluff(teeth=11, rows=2.2, amp=0.28, keep_front=False), sub=5), WHITE, "body")
    # lavender grey toward the back underneath
    def shade(P):
        return smoothstep(0.0, 0.06, P[:, 1] - 0.004) * smoothstep(0.075, 0.03, P[:, 2])
    k.paint(shade, SHADE, on=["fluff"])
    # jagged white points all round the fluff's rim, and one long point running back and out to +X
    rs = np.random.RandomState(7)
    for i in range(18):
        a = 2 * math.pi * i / 18 + rs.uniform(-0.12, 0.12)
        z = FC[2] + (0.01 if i % 2 else -0.02) + rs.uniform(-0.006, 0.006)
        d = np.array([math.sin(a), -math.cos(a), 0.0])
        s = np.array([math.cos(a), math.sin(a), 0.0])
        rim = math.sqrt(1 - ((z - FC[2]) / FR[2]) ** 2)
        base = FC + d * FR[:2].mean() * rim * 0.82 + np.array([0, 0, z - FC[2]])
        tip = base + d * (0.04 + rs.uniform(0, 0.014)) + np.array([0, 0, rs.uniform(-0.012, 0.006)])
        w, h = 0.016, 0.013
        k.add(f"point{i}", hull([base + s * w + (0, 0, h), base - s * w + (0, 0, h), base + s * w - (0, 0, h),
                                 base - s * w - (0, 0, h), tip]), WHITE, "body", fuse=False, surface="fur")
    for i, (dz, top) in enumerate(((0.06, 0.012), (0.028, -0.01))):
        d = unit(0.55, 0.83, 0.12)
        s = unit(-0.83, 0.55, 0)
        base = FC + np.array([0.03, 0.05, dz - FC[2]])
        tip = base + d * (0.085 - 0.02 * i) + np.array([0, 0, top])
        k.add(f"tail{i}", hull([base + s * 0.022 + (0, 0, 0.016), base - s * 0.022 + (0, 0, 0.016),
                                base + s * 0.022 - (0, 0, 0.014), base - s * 0.022 - (0, 0, 0.014), tip]),
              WHITE if i == 0 else SHADE, "body", fuse=False, surface="fur")


def flakes(k):
    """Square flakes stuck to the fluff, turned on a corner like diamonds."""
    for i, (d, size, col) in enumerate((((-0.7, -0.58, 0.3), 0.011, RED), ((0.66, -0.66, 0.15), 0.01, RED),
                                         ((0.18, -0.9, -0.42), 0.009, BLACK), ((0.85, 0.2, 0.25), 0.009, ORANGE),
                                         ((-0.8, 0.35, 0.2), 0.009, RED), ((0.3, 0.7, 0.55), 0.008, BLACK))):
        d = unit(*d)
        p = FC + d / math.sqrt(((d / FR) ** 2).sum())
        n = (p - FC) / FR ** 2; n /= np.linalg.norm(n)
        a = np.cross(n, (0, 0, 1.0)); a /= np.linalg.norm(a)
        b = np.cross(n, a)
        p = p + n * 0.003
        corners = [p + a * size, p + b * size, p - a * size, p - b * size]
        k.add(f"flake{i}", hull([c + n * t for c in corners for t in (-0.003, 0.003)]), col, "body", fuse=False,
              surface="shell")


def pupil_look(u, v, dark=srgb(PUPIL), glint=srgb(GLINT)):
    c = np.tile(dark, (len(u), 1))
    c[((u + 0.25) / 0.32) ** 2 + ((v - 0.45) / 0.3) ** 2 < 1] = glint
    return c


ROUNDED = polygon([(math.copysign(1, math.cos(t)) * min(1, abs(math.cos(t)) * 1.6),
                    math.copysign(1, math.sin(t)) * min(1, abs(math.sin(t)) * 1.6))
                   for t in np.linspace(0, 2 * math.pi, 48, endpoint=False)])
ROUNDED_FIERCE = polygon([(math.cos(t), max(-1.0, min(0.15, math.sin(t) * 1.4)))
                          for t in np.linspace(0, 2 * math.pi, 48, endpoint=False)])


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    head(k)
    fluff(k)
    flakes(k)
    k.build(voxel=0.0024, smooth=3, band=0.004, wband=0.02)
    k.eye_states(sb.eyes(k, centre=HC, radii=HR, d=(0.55, -0.8, -0.08), half=(0.021, 0.027),
                         pupil=(0.0, -0.04, 0.44, 0.6), pupil_look=pupil_look, shapes=(ROUNDED, ROUNDED_FIERCE)))
    k.export(out)
