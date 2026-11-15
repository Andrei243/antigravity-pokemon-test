# Dodrio sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dodrio.py -- <the export's Dodrio.glb> <out.glb>
# Grown from doduo.py (its fur, heads, beaks, necks and legs, by import): an egg of a body, fuzzy brown above and black
# below with a ragged line between; three black necks fanned apart, each with a fuzzy head and its own temper (the left
# and middle screaming with their beaks open and pink inside, the right smug with its beak shut and its eyes half
# lidded), black feathers swept back off the crowns; a fan of three pink-red tail feathers; longer, stronger legs.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, slab, srgb, inside, tri, unit, eye_look, outward, sibling, args

d = sibling("doduo")
FUR, NECK, BLACK = d.FUR, d.NECK, "#2B2629"
PINK, RED = "#EE9AA0", "#D5505C"


class Head(d.Head):
    """Doduo's head with a temper: an eye of white and a black pupil cut along its top by the lid, slanting down to the
    beak (angry) or level and low (smug); and black feathers swept back off its crown."""

    def __init__(self, k, tag, bone, base, centre, look, temper, crest, **kw):
        super().__init__(k, tag, bone, base, centre, look, **kw)
        self.temper = temper
        M, c, r = self.M, self.c, self.r
        for j, (side, lift, length) in enumerate(crest):
            root = c + M @ unit(side * 0.25, 0.45, 0.86) * r * 0.85
            dd = M @ unit(side, 0.75, 0.6 + lift)
            k.add(f"crest{tag}{j}", outward(blade([root, root + dd * length * 0.5, root + dd * length],
                                                  [0.024, 0.018, 0.003], M @ unit(-0.6 * side, 0, 1.0), 0.72)),
                  BLACK, bone, fuse=False)

    def top(self, u):
        return (0.1 + 0.5 * u) if self.temper == "angry" else np.full_like(u, -0.02)

    def eyes(self, state, half=(0.025, 0.023)):
        k = self.k
        white, black, lid = srgb("#FFFFFF"), srgb("#16131A"), self.lid
        for p, de in self.specs:
            up = self.M[:, 2]
            right = np.cross(up, de); right /= np.linalg.norm(right)
            s = -1.0 if np.dot(right, self.L) > 0 else 1.0            # +u away from the beak
            def look(u, v):
                c = np.tile(white, (len(u), 1))
                c[(u + 0.25) ** 2 / 0.3 + (v + 0.05) ** 2 / 0.55 < 1] = black
                c[v > self.top(u)] = lid
                return c
            lk = eye_look(state, look, lid, black, line=0.18)
            k.decal(p, de, half, (lambda u, v, lk=lk, s=s: lk(s * u, v)), up=up, eps=0.006, rings=20, segs=56)


def dodrio(src, out):
    k = Kit(src, coat="fur")
    # the body: an egg of fur, the lower part smooth and black
    k.add("body", ell((0, 0.0, 0.465), (0.13, 0.13, 0.155), deform=d.spiky(110, 0.24, 0.15, seed=11,
                                                                          keep=lambda D: D[:, 2] < -0.35)),
          FUR, "body")
    def lower(P):
        th = np.arctan2(P[:, 0], -P[:, 1])
        return inside(P[:, 2] - (0.415 + 0.022 * tri(th * 9 / (2 * math.pi))), 0.0035)
    k.paint(lower, BLACK, on=["body"])
    d.legs(k, (0.06, 0.0, 0.37), (0.076, 0.06, 0.19), (0.07, -0.005, 0.05), radii=(0.05, 0.03, 0.021), toe=0.1,
           toe_r=0.016)

    # three heads, fanned apart
    heads = [
        Head(k, "L", "headL", (-0.06, 0.0, 0.57), (-0.21, -0.04, 0.8), unit(-0.75, -0.5, 0.43), "angry",
             [(-1, 0.1, 0.12), (1, 0.0, 0.11)], gape=0.35, neck=0.02, bend=(-0.03, 0.0, 0.0)),
        Head(k, "M", "headM", (0.0, 0.02, 0.58), (-0.02, 0.05, 0.93), unit(-0.42, -0.6, 0.68), "angry",
             [(-1, 0.25, 0.15), (1, 0.15, 0.16), (0.2, 0.4, 0.13)], gape=0.4, neck=0.02, bend=(0.01, 0.01, 0.0)),
        Head(k, "R", "headR", (0.06, 0.0, 0.57), (0.18, -0.03, 0.83), unit(0.85, -0.5, 0.05), "smug",
             [(1, 0.1, 0.13), (-1, 0.05, 0.11)], neck=0.02, bend=(0.03, 0.0, 0.0)),
    ]

    # the tail: three rounded feathers fanned up and down, back and out to +X, pink with a red root
    back = unit(0.5, 0.8, -0.32)
    across = np.cross(back, (0, 0, 1.0)); across /= np.linalg.norm(across)
    upin = np.cross(across, back)
    ROOT = np.array([0.02, 0.12, 0.43])
    FEATHER = [(0.0, -0.022), (0.06, -0.036), (0.14, -0.042), (0.2, -0.034), (0.235, -0.016), (0.245, 0.0),
               (0.235, 0.016), (0.2, 0.034), (0.14, 0.042), (0.06, 0.036), (0.0, 0.022)]
    BASE = [(0.0, -0.024), (0.06, -0.039), (0.1, -0.044), (0.1, 0.044), (0.06, 0.039), (0.0, 0.024)]
    for j, ang in enumerate((-20, 0, 20)):
        a = math.radians(ang)
        ex = back * math.cos(a) + upin * math.sin(a)
        ez = upin * math.cos(a) - back * math.sin(a)
        o = ROOT + across * 0.006 * (j - 1)
        k.add(f"tail{j}", slab(FEATHER, o, ex, ez, 0.02), PINK, "tail", fuse=False)
        k.add(f"tailroot{j}", slab(BASE, o, ex, ez, 0.026), RED, "tail", fuse=False)

    k.build(voxel=0.0064, smooth=2)
    k.eye_states(lambda state: [h.eyes(state) for h in heads])
    k.export(out)


if __name__ == "__main__":
    dodrio(*args())
