# Munna sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/munna.py -- <the export's Munna.glb> <out.glb>
#
# Written to be grown from: musharna.py imports its frame (sibling("munna")): the colours, the flowers, the eye.
#
# The design, from the references: a round pale-pink body that is head and all, a dome a little longer than it is
# wide with a flat underside whose rim overhangs four short pointed legs; at its front a pink oval (the snout's tip);
# small red upright oval eyes with a white glint and a dark rim, set on the sides behind the front (two tiny lashes over
# each in the art, left out: specks at the game's size); purple five-petalled flowers with pink middles on its back,
# a large one high behind and a smaller one low behind on each side. It floats: the legs need not take its weight.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args

PINK, SNOUT, PURPLE, CENTRE = "#F4D2DC", "#E9909E", "#A88AC8", "#EC8EA4"
RED, RIM, GLINT = "#E2384E", "#4A1A26", np.array([1.0, 1.0, 1.0])

# The skeleton's joints: body (0, 0, 0.16), head (0, -0.1, 0.16), legs (+-0.06, -0.03, 0.06)
MUNNA = dict(
    C=(0.0, 0.01, 0.152), R=(0.16, 0.18, 0.14),
    eye_dir=(0.74, -0.64, 0.12), eye_half=(0.024, 0.037),
    snout=(0.0, -0.108, 0.14), snout_r=(0.11, 0.08, 0.1), nose_half=(0.034, 0.048), nose_z=-0.08,
    flowers=[((0.62, 0.45, 0.6), 0.066, 0), ((0.78, 0.5, -0.2), 0.05, 18)],
    legs=[((0.062, -0.07), 0.03), ((0.075, 0.085), 0.03)],
)


def flat_bottom(V):
    """A dome: below its lower part the sphere is pressed into a flat underside, a rim standing a little proud."""
    V = V.copy()
    z = V[:, 2]
    V[:, 2] = np.where(z < -0.55, -0.55 + (z + 0.55) * 0.35, z)
    s = 1 + 0.04 * np.exp(-((z + 0.5) / 0.15) ** 2)          # the rim
    V[:, 0] *= s; V[:, 1] *= s
    return V


def on_body(Pr, d):
    """The body's ellipsoid point along a direction from its middle, and its normal."""
    C, R = np.array(Pr["C"]), np.array(Pr["R"])
    d = unit(*d)
    p = C + d / np.linalg.norm(d / R)
    n = (p - C) / R ** 2
    return p, n / np.linalg.norm(n)


def flower_look(petal=PURPLE, centre=CENTRE):
    def look(u, v):
        c = np.tile(srgb(petal), (len(u), 1))
        c[np.hypot(u, v) < 0.38] = srgb(centre)
        return c
    return look


def petals(n=5, turn=0.0, out=0.52, r=0.5):
    """A flower's outline: n round petals (discs) round its middle."""
    discs = [(out * math.cos(math.radians(turn) + 2 * math.pi * i / n),
              out * math.sin(math.radians(turn) + 2 * math.pi * i / n), r) for i in range(n)]
    discs.append((0, 0, out))
    def f(th):
        d = np.array([math.cos(th), math.sin(th)])
        best = 0.0
        for u, v, rad in discs:
            c = np.array([u, v]); b = d @ c; q = b * b - (c @ c - rad * rad)
            if q >= 0:
                best = max(best, b + math.sqrt(q))
        return best
    return f


def flowers(k, Pr, look=None, dist=None):
    for d, half, turn in Pr["flowers"]:
        p, n = on_body(Pr, d)
        k.decal(p, n, (half, half), look or flower_look(), outline=petals(turn=turn), eps=0.002, rings=24, segs=90,
                mirror=True, up=(0, -0.3, 1))


def eye_open(u, v):
    """A small upright red oval with a dark rim and a white glint up toward the front."""
    c = np.tile(srgb(RIM), (len(u), 1))
    c[(u / 0.78) ** 2 + (v / 0.84) ** 2 < 1] = srgb(RED)
    c[((u + 0.12) / 0.28) ** 2 + ((v - 0.28) / 0.36) ** 2 < 1] = GLINT
    return c


def lay_eyes(k, Pr, state, look=eye_open, lid=PINK):
    p, n = on_body(Pr, Pr["eye_dir"])
    k.decal(p, n, Pr["eye_half"], eye_look(state, look, srgb(lid), srgb(RIM), line=0.18), eps=0.004, rings=20,
            segs=56, mirror=True)


def munna(src, out, Pr=MUNNA):
    k = Kit(src, coat="fur")
    C, R = np.array(Pr["C"]), np.array(Pr["R"])
    k.add("body", ell(C, R, deform=flat_bottom), PINK, "body")
    k.add("snout", ell(Pr["snout"], Pr["snout_r"]), PINK, "head")
    # Four short pointed legs under the rim
    for (x, y), r in Pr["legs"]:
        top = np.array([x, y, C[2] - R[2] * 0.45])
        k.add(f"leg{y:+.2f}", tube([top, top + np.array([0.006, 0, -0.04]), np.array([x + 0.012, y, 0.012])],
                                   [r, r * 0.75, r * 0.4]), PINK, "leg{s}", mirror=True)
    # The snout's pink oval at the front
    p, n = on_body(Pr, (0, -1, Pr["nose_z"]))
    p = p + np.array([0, -0.03, 0])
    k.decal(p, (0, -1, Pr["nose_z"] * 0.8), Pr["nose_half"], SNOUT, eps=0.004, rings=12, segs=56)
    flowers(k, Pr)
    k.build(voxel=0.0045, smooth=3, band=0.006)
    k.eye_states(lambda s: lay_eyes(k, Pr, s))
    k.export(out)


if __name__ == "__main__":
    munna(*args())
