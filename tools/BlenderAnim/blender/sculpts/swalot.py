# Swalot sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/swalot.py -- <the export's Swalot.glb> <out.glb>
#
# Gulpin grown up, from gulpin.py (sibling("gulpin")): its three-lump hands, its puckered lip, its black diamond.
# From the references (drawn from the front three-quarter, the face turned to the viewer's right): a great purple sack
# shaped like a bell, a dome on top widening to a foot that spreads into a wavy hem of soft lobes; big puckered lips
# standing out at the top of its front; a long yellow whisker hanging from each side of the lips and curling out at its
# end; a small pink dot on each side of the head behind the lips, its eyes; stubby three-lump hands on its front sides;
# big black diamonds on its flanks, two a side. (The brief's "wide mouth": the pictures' lips are closed and puckered.)
# Skeleton: body (0, 0, 0.22), head (0, 0, 0.42), arms (+-0.12, -0.1, 0.3).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args, sibling

g = sibling("gulpin")
PURPLE, MARK, WHISKER = "#A08CD2", g.MARK, "#F4D070"
PINK, PINK_RIM, LINE = "#F27894", "#B04462", "#4A3A6A"

BODY, BODYR = np.array([0.0, 0.0, 0.25]), np.array([0.2, 0.18, 0.25])


def bell(V):
    """A dome on top widening to a flat foot."""
    V = V.copy()
    z = V[:, 2]
    rr = np.maximum(np.hypot(V[:, 0], V[:, 1]), 1e-6)
    side = np.sqrt(np.clip(1 - z * z, 0, 1))
    low = 0.97 + 0.2 * smoothstep(-0.5, -0.97, z)          # below the middle the sides fall straight and flare
    target = np.where(z < 0, np.maximum(side, low * (1 - 0.0 * z)), side)
    s = np.where(rr > 1e-5, target / rr, 1.0)
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.maximum(V[:, 2], -0.93)
    return V


def surface(d, lift=0.0):
    """A point on the bell (as the ellipsoid gives it, before its foot widens) toward d, lifted along its normal."""
    p, n = g.on(BODY, BODYR, d)
    return p + n * lift


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")

    k.add("body", ell(BODY, BODYR, deform=bell), PURPLE, "body")
    # The wavy hem: soft lobes round the foot
    for i in range(12):
        t = 2 * math.pi * (i + 0.5) / 12
        k.add(f"hem{i}", ell((0.218 * math.sin(t), 0.197 * math.cos(t), 0.03), (0.06, 0.056, 0.032)), PURPLE, "body")
    # Big puckered lips at the top of the front, a dark crease between them
    k.add("lipU", ell((0.0, -0.17, 0.425), (0.06, 0.05, 0.034)), PURPLE, "head")
    k.add("lipD", ell((0.0, -0.165, 0.37), (0.054, 0.048, 0.034)), PURPLE, "head")
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.397) - 0.0035, 0.002) * inside(np.abs(P[:, 0]) - 0.035, 0.003)
            * (P[:, 1] < -0.19), LINE, on=["lip"])
    # Hands of three lumps on the front sides
    g.hand(k, BODY, BODYR, (0.75, -0.62, 0.05), 0.04, PURPLE, "arm{s}")
    # Two black diamonds a side
    for s in (1, -1):
        for d in ((0.86 * s, -0.32, -0.28), (0.8 * s, 0.6, -0.28)):
            g.diamond(k, BODY, BODYR, d, (0.058, 0.074), MARK)
    # The whiskers: from beside the lips down the face, curling out at their ends
    for s in (1, -1):
        pts = [surface((0.3 * s, -0.95, 0.5), 0.016), surface((0.42 * s, -0.9, 0.3), 0.016),
               surface((0.5 * s, -0.86, 0.1), 0.016), surface((0.6 * s, -0.79, -0.02), 0.018),
               surface((0.74 * s, -0.66, 0.0), 0.03)]
        k.add(f"whisker{'R' if s > 0 else 'L'}", tube(pts, [0.016, 0.016, 0.016, 0.015, 0.015]), WHISKER, "head",
              fuse=False)

    k.build(voxel=0.0055, smooth=4, band=0.006)

    CP, CR, CL, CI = srgb(PINK), srgb(PINK_RIM), srgb(PURPLE), srgb(LINE)

    def dot(u, v):
        c = np.tile(CP, (len(u), 1))
        c[u * u + v * v > 0.74 ** 2] = CR
        c[((u + 0.25) / 0.22) ** 2 + ((v - 0.3) / 0.2) ** 2 < 1] = np.array([1.0, 0.8, 0.85])
        return c

    def eyes(state):
        for s in (1, -1):
            p, n = g.on(BODY, BODYR, (0.4 * s, -0.62, 0.68))
            look = eye_look(state, dot, CL, CI, line=0.2)
            lk = look if s > 0 else (lambda f: (lambda a, b: f(-a, b)))(look)
            k.decal(p, n, (0.02, 0.02), lk, eps=0.004, rings=20, segs=56)

    k.eye_states(eyes)
    k.export(out)
