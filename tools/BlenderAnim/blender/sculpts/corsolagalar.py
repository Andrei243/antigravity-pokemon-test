# Galarian Corsola sculpted on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/corsolagalar.py -- "<the export's Corsola-Galar.glb>" <out.glb>
# Grown from corsola.py (sibling("corsola")): its round body, the ragged line its underside's colour keeps and the
# flecks over it, its four stub legs; the branches made as cursola.py makes its ghostly coral (lumpy, with stubs),
# since Cursola is this form grown.
#
# From the references: a ghost of Corsola, bleached. The round body a pale grey-white, its lower part a soft grey with
# the same ragged edge and a few grey dots; four short grey stubs of legs. Out of its back and top long lumpy branches
# of white coral spreading up and out to both sides and back, each with a stub or two (much longer than the "short
# stubs" the task expected: the pictures show long branches, as long as the body is wide); two branches broken off
# short, their cut ends flat ovals, one on top toward the front and one on its side by the face; a short broken stub
# low on the other side. The face low on the front and sad: two dark eyes under a heavy flat lid drooping to their
# outer corners, pink low in each with a small glint, and a little frown of a chevron between and below.
# The skeleton: body (0, 0, 0.17), coral (0, 0, 0.31), legs (+-0.07, -0.06, 0.1), hind (+-0.07, 0.06, 0.1).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, srgb, inside, unit, eye_look, polygon, outward, sibling, args

co = sibling("corsola")
C, RB = co.C, np.array(co.RB)
BODY, UNDER, LEG, CORAL, CUT = "#DADAE0", "#B7B7BE", "#A6A6AD", "#F5F5F8", "#CFCFD6"
INK, IRIS, GLINT, FROWN = "#1C1A24", "#E46AA6", "#FFFFFF", "#4A4854"


def on(c, r, d):
    """The point of an ellipsoid (centre c, radii r) in direction d, and the outward normal there."""
    d = unit(*d)
    p = np.array(c) + d / np.linalg.norm(d / np.array(r))
    n = (p - np.array(c)) / np.array(r) ** 2
    return p, n / np.linalg.norm(n)


def branch(k, name, pts, r0, r1, stubs=()):
    """A thick lumpy branch of white coral through pts, with stubs (a share along it, a direction, a length)."""
    pts = np.array(pts, float)
    radii = np.linspace(r0, r1, len(pts))
    k.add(name, tube(pts, radii), CORAL, "coral")
    for j in range(1, len(pts)):
        p = (pts[j - 1] + pts[j]) / 2
        k.add(f"{name}k{j}", ell(p + (0, 0.003 * (-1) ** j, 0.005), [radii[j] * 1.08] * 3), CORAL, "coral")
    k.add(f"{name}end", ell(pts[-1], [r1 * 1.05] * 3), CORAL, "coral")
    for i, (t, d, ln) in enumerate(stubs):
        j = t * (len(pts) - 1); a = int(min(j, len(pts) - 2)); f = j - a
        at = pts[a] + (pts[a + 1] - pts[a]) * f
        d = unit(*d)
        k.add(f"{name}s{i}", tube([at, at + d * ln * 0.6, at + d * ln], [r1 * 0.9, r1 * 0.8, r1 * 0.85]), CORAL,
              "coral")


def stump(k, name, d, r, length, bone="body"):
    """A branch broken off short: a loose drum out of the body along d, its cut end a flat grey oval."""
    p, n = on(C, RB, d)
    base = p - n * 0.03
    k.add(name, lathe(base, n, [(0, 0), (r, 0), (r * 1.04, length * 0.6), (r * 0.98, length), (0, length)], seg=40),
          CORAL, bone, fuse=False, surface="shell")
    k.add(name + "cut", lathe(base, n, [(0, length - 0.002), (r * 0.8, length - 0.002), (r * 0.8, length + 0.003),
                                        (0, length + 0.003)], seg=40), CUT, bone, fuse=False, surface="shell")


def corsola_galar(src, out_path):
    k = Kit(src, coat="shell")
    k.add("body", ell(C, RB), BODY, "body")
    for s, (x, y, bone) in enumerate(((0.07, -0.06, "leg{s}"), (0.07, 0.06, "hind{s}"))):
        k.add(f"leg{s}", ell((x, y, 0.04), (0.036, 0.036, 0.038)), LEG, bone, mirror=True)
    k.paint(lambda P: np.maximum(inside(P[:, 2] - co.edge_z(P), 0.0025), co.flecks(P)), UNDER, on=["body"])

    # the long branches of ghostly coral, up and out to both sides and back
    branch(k, "b1", [(-0.05, 0.03, 0.29), (-0.11, 0.06, 0.37), (-0.17, 0.09, 0.44), (-0.22, 0.11, 0.5)], 0.042, 0.03,
           [(0.45, (-0.2, -0.3, 1), 0.06), (0.8, (-1, 0.1, 0.1), 0.05)])
    branch(k, "b2", [(0.05, 0.02, 0.29), (0.11, 0.04, 0.37), (0.165, 0.06, 0.445), (0.205, 0.07, 0.505)], 0.042,
           0.03, [(0.5, (1, -0.2, 0.3), 0.055), (0.85, (0.2, -0.3, 1), 0.045)])
    branch(k, "b3", [(-0.1, 0.06, 0.24), (-0.17, 0.09, 0.28), (-0.24, 0.12, 0.31), (-0.3, 0.14, 0.33)], 0.04, 0.028,
           [(0.55, (-0.1, 0.2, 1), 0.05)])
    branch(k, "b4", [(0.1, 0.07, 0.24), (0.17, 0.11, 0.27), (0.23, 0.15, 0.3), (0.28, 0.17, 0.33)], 0.039, 0.028,
           [(0.6, (0.2, -0.2, 1), 0.05)])
    branch(k, "b5", [(0.0, 0.08, 0.28), (0.01, 0.15, 0.36), (0.02, 0.2, 0.43)], 0.04, 0.03,
           [(0.6, (-1, 0.2, 0.5), 0.05)])
    # the branches broken off short: on top toward the front, by the face, and low on the other side
    stump(k, "stumpA", (-0.38, -0.55, 0.8), 0.04, 0.04)
    stump(k, "stumpB", (1, -0.42, 0.1), 0.032, 0.035)
    stump(k, "stumpC", (-1, -0.35, -0.3), 0.026, 0.028)

    # the little frown
    p, n = on(C, RB, (0, -1, -0.33))
    k.decal(p, n, (0.03, 0.012), FROWN, eps=0.004, rings=8, segs=40,
            outline=polygon([(-1, -1), (0, 1), (1, -1), (0, -0.25)]))
    k.build(voxel=0.0055, smooth=4, band=0.006, wband=0.05)

    # the sad eyes: dark, a heavy flat lid drooping to the outer corner, pink low in each with a glint
    CK, CI, CG, LID = srgb(INK), srgb(IRIS), srgb(GLINT), srgb(BODY)
    def eye(u, v):
        c = np.tile(CK, (len(u), 1))
        c[((u + 0.0) / 0.62) ** 2 + ((v + 0.38) / 0.42) ** 2 < 1] = CI
        c[((u - 0.05) / 0.16) ** 2 + ((v + 0.2) / 0.16) ** 2 < 1] = CG
        return c
    OUT = [(1.0, 0.3), (0.5, 0.48), (0.0, 0.62), (-0.5, 0.7), (-1.0, 0.72)] + \
          [(math.cos(a), 1.05 * math.sin(a)) for a in np.linspace(math.pi, 2 * math.pi, 13)[1:-1]]
    def eyes(state):
        look = eye_look(state, eye, LID, srgb("#5A5866"), line=0.18)
        for s in (1, -1):
            p, n = on(C, RB, (0.34 * s, -1, -0.04))
            col = look if s == 1 else (lambda u, v, f=look: f(-u, v))
            out = polygon([(s * u, v) for u, v in OUT])
            k.decal(p, n, (0.026, 0.034), col, eps=0.003, outline=out, rings=20, segs=56)
    k.eye_states(eyes)
    k.export(out_path)


if __name__ == "__main__":
    corsola_galar(*args())
