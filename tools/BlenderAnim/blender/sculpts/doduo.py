# Doduo sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/doduo.py -- <the export's Doduo.glb> <out.glb>
# A fuzzy brown ball of a body on two long pale legs with big three-toed feet; two black necks fanned apart, each
# carrying a fuzzy round head with a long pale beak and small black eyes. No wings. Written to be grown from: Dodrio
# imports it (sibling("doduo")) and builds its three heads, its legs and its fur with these functions.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, outward, args

FUR, NECK, BEAK, LEG, CLAW = "#C98552", "#2A272C", "#EAD6B8", "#E8CBA6", "#E9EBF0"
EYE, GLINT = "#16131A", "#FFFFFF"


# --- fur ---------------------------------------------------------------------------------------------------------------
def spiky(n=70, amp=0.24, width=0.16, seed=3, keep=None):
    """A deform of the unit sphere into a ball of fur: n tufts standing out, evenly spread (a Fibonacci sphere, jittered),
    amp their length in radii, width their angular size; keep(D) is True for tuft directions left smooth (a face)."""
    rs = np.random.RandomState(seed)
    i = np.arange(n) + 0.5
    z = 1 - 2 * i / n
    th = math.pi * (1 + 5 ** 0.5) * i + rs.uniform(-0.3, 0.3, n)
    D = np.stack([np.sqrt(1 - z * z) * np.cos(th), np.sqrt(1 - z * z) * np.sin(th), z], 1)
    if keep is not None:
        D = D[~keep(D)]
    L = amp * (0.8 + 0.4 * rs.rand(len(D)))
    def f(V):
        c = np.clip(V @ D.T, -1, 1)                       # cosine to each tuft
        a = np.arccos(c) / width
        bump = (np.clip(1 - a, 0, 1) ** 1.15 * L[None]).max(1)
        return V * (1 + bump)[:, None]
    return f


def frame(look, up=(0, 0, 1.0)):
    """A right-handed frame for a head: columns side, back (-look), up; local -Y is where the head looks."""
    L = np.array(look, float); L /= np.linalg.norm(L)
    side = np.cross(-L, up); side /= np.linalg.norm(side)
    up2 = np.cross(side, -L)
    return np.stack([side, -L, up2], axis=1)


def placed(shape, c, M):
    V, F = shape
    return V @ M.T + np.array(c, float), F


# --- a head on its neck ----------------------------------------------------------------------------------------------
class Head:
    """A fuzzy round head at centre on a black neck from base, looking along look, its beak open by gape (radians);
    its eyes are laid by eyes(state)."""

    def __init__(self, k, tag, bone, base, centre, look, r=0.08, neck=0.019, beak=0.14, gape=0.0, fur=FUR,
                 bend=(0, 0, 0)):
        self.k, self.tag, self.c, self.r = k, tag, np.array(centre, float), r
        self.M = frame(look)
        self.L = -self.M[:, 1]
        self.lid = srgb(fur)
        b = np.array(base, float)
        mid = (b + self.c) / 2 + np.array(bend, float)
        k.add(f"neck{tag}", outward(tube([b, mid, self.c - self.L * r * 0.5], [neck, neck * 0.95, neck * 1.05])),
              NECK, bone, fuse=False, surface="skin")
        # the fur's face is left smooth so the eyes and the beak's root lie on it
        k.add(f"head{tag}", placed(ell((0, 0, 0), (r, r, r), deform=spiky(60, 0.32, 0.18, seed=len(tag) + 7,
                                                                           keep=lambda D: D[:, 1] < -0.25)),
                                   self.c, self.M), fur, bone)
        self.beak(beak, gape, bone)
        self.specs = []
        for s in (1, -1):
            de = self.M @ unit(0.8 * s, -0.55, 0.32)
            self.specs.append((self.c + de * r, de))

    def beak(self, length, gape, bone):
        """A long pale cone from the face, curving a little down; a gaping beak is two, the inside pink."""
        k, c, M, r = self.k, self.c, self.M, self.r
        def cone(tilt, thick, name, colour):
            d = M @ unit(0, -math.cos(tilt), math.sin(tilt))
            b0 = c + d * r * 0.65
            p = [b0, b0 + d * length * 0.5 + M[:, 2] * -0.004, b0 + d * length + M[:, 2] * -0.012]
            k.add(name, outward(tube(p, [thick, thick * 0.55, 0.003])), colour, bone, fuse=False, surface="shell")
        if gape <= 0:
            cone(-0.05, 0.024, f"beak{self.tag}", BEAK)
        else:
            cone(gape * 0.6, 0.02, f"beak{self.tag}", BEAK)
            cone(-gape * 0.5, 0.017, f"jaw{self.tag}", BEAK)
            # the pink inside between the two halves
            d = M @ unit(0, -1, 0.05 * gape)
            b0 = c + d * r * 0.7
            k.add(f"mouth{self.tag}", outward(tube([b0, b0 + d * length * 0.6], [0.012 + 0.055 * gape, 0.006])),
                  "#D9727E", bone, fuse=False, surface="skin")

    def eyes(self, state, half=(0.02, 0.026)):
        """Small black eyes with a white glint, one on each side of the face."""
        k = self.k
        black, white = srgb(EYE), srgb(GLINT)
        for p, de in self.specs:
            up = self.M[:, 2]
            right = np.cross(up, de); right /= np.linalg.norm(right)
            s = -1.0 if np.dot(right, self.L) > 0 else 1.0            # +u away from the beak
            def look(u, v, s=s):
                c = np.tile(black, (len(u), 1))
                c[(s * u - 0.2) ** 2 + (v - 0.35) ** 2 < 0.09] = white
                return c
            lk = eye_look(state, look, self.lid, black, line=0.22)
            k.decal(p, de, half, (lambda u, v, lk=lk, s=s: lk(s * u, v)), up=up, eps=0.006, rings=20, segs=56)


# --- legs ------------------------------------------------------------------------------------------------------------
def legs(k, hip, knee, ankle, radii=(0.042, 0.026, 0.018), toe=0.09, toe_r=0.014):
    """Long pale legs: the thigh in the body, the shin bending back at the knee and tapering, three long toes forward
    and one back, each with a pale claw. Given for the +X leg; mirrored."""
    k.add("leg", outward(tube([hip, knee, ankle], list(radii))), LEG, "leg{s}", fuse=False, mirror=True,
          surface="skin")
    foot = np.array(ankle, float) + np.array([0, 0, -ankle[2] + toe_r])
    k.add("heel", outward(ell(foot + np.array([0, 0.004, 0.006]), (0.02, 0.02, 0.018))), LEG, "leg{s}",
          fuse=False, mirror=True, surface="skin")
    for j, ang in enumerate((-32, 0, 32, 180)):
        a = math.radians(ang)
        d = np.array([math.sin(a), -math.cos(a), 0.0])
        n = toe * (0.55 if ang == 180 else 1.0)
        tip = foot + d * n
        k.add(f"toe{j}", outward(tube([foot, foot + d * n * 0.5 + np.array([0, 0, 0.004]), tip],
                                      [toe_r, toe_r * 0.9, toe_r * 0.75])), LEG, "leg{s}", fuse=False, mirror=True,
              surface="skin")
        k.add(f"claw{j}", outward(tube([tip, tip + (d * 1.0 + np.array([0, 0, -0.7])) * 0.022],
                                       [toe_r * 0.8, 0.002])), CLAW, "leg{s}", fuse=False, mirror=True,
              surface="shell")


def doduo(src, out):
    k = Kit(src, coat="fur")
    # the body: a ball of fur over the hips
    k.add("body", ell((0, 0, 0.375), (0.13, 0.13, 0.13), deform=spiky(96, 0.3, 0.16, seed=5)), FUR, "body")
    legs(k, (0.05, 0.0, 0.3), (0.064, 0.05, 0.155), (0.06, -0.005, 0.045))
    # two heads fanned apart: the left looking out to its side and forward, the right up and forward
    heads = [Head(k, "L", "headL", (-0.04, 0.0, 0.47), (-0.15, -0.03, 0.71), unit(-0.8, -0.55, 0.08),
                  bend=(-0.02, 0.0, 0.0)),
             Head(k, "R", "headR", (0.04, 0.0, 0.47), (0.08, 0.01, 0.79), unit(0.3, -0.5, 0.81),
                  bend=(0.015, 0.0, 0.0))]
    k.build(voxel=0.0062, smooth=2)
    k.eye_states(lambda state: [h.eyes(state) for h in heads])
    k.export(out)


if __name__ == "__main__":
    doduo(*args())
