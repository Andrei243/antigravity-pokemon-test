# Deino sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/deino.py -- <the export's Deino.glb> <out.glb>
# From the references: a small blue dragon on four short thick legs. Its head is a bowl of shaggy grey-brown hair
# with one horn standing up and back off its crown; the hair hangs over the eyes in two locks a side, the blue face
# running up in a point between them; a blue snout forward and an open mouth, pink inside. A long grey-brown neck
# rises from a shaggy mantle over the shoulders, the mantle spotted pink; a short blue tail with a few small spikes.
# (The task's text spoke of a pink-ringed neck: the pictures have the pink as spots on the mantle.)
#
# Written to be grown from: zweilous.py and hydreigon.py import it (sibling("deino")). One class makes every head of
# the line (Head: a mop of hair or none, horns, a crest of spikes, a mouth open or shut, eyes hidden or shown).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, polygon, eye_look, outward, tri, sibling, args

# --- the line's colours ---------------------------------------------------------------------------------------------
BLUE = "#4A84CE"
MOP = "#655A55"
SPOT = "#D2206A"
MOUTH = "#E27A7A"
TOOTH = "#F4F3EE"
INK = srgb("#17141A")

_doduo = sibling("doduo")
spiky = _doduo.spiky


def frame(look, up=(0, 0, 1.0)):
    """A right-handed frame for a head: columns side, back (-look), up; local -Y is where the head looks."""
    L = np.array(look, float); L /= np.linalg.norm(L)
    side = np.cross(-L, up); side /= np.linalg.norm(side)
    up2 = np.cross(side, -L)
    return np.stack([side, -L, up2], axis=1)


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre."""
    d = unit(*d)
    t = 1.0 / math.sqrt(((d / np.array(r)) ** 2).sum())
    return np.array(c, float) + d * t


def spike(base, d, length, width, up):
    """A spike, horn tip or fang: a flat-faced point from base along d, its section a kite wide across up."""
    base, d = np.array(base, float), unit(*d)
    up = np.array(up, float); up = up - d * (up @ d); up /= np.linalg.norm(up)
    side = np.cross(d, up)
    b = base - d * width * 0.4
    return hull([b + side * width / 2, b - side * width / 2, b + up * width * 0.35, b - up * width * 0.35,
                 base + d * length * 0.5 + side * width * 0.27, base + d * length * 0.5 - side * width * 0.27,
                 base + d * length * 0.5 + up * width * 0.17, base + d * length * 0.5 - up * width * 0.17,
                 base + d * length])


# The face core and the lock over each eye, in the head's own units (r)
CORE_C, CORE_R = (0.0, -0.28, -0.18), (0.8, 0.8, 0.72)
LOCK = polygon([(-1, 1), (1, 1), (1, -0.15), (0.45, -1), (0, -0.3), (-0.45, -1), (-1, -0.15)])


class Head:
    """
    A head of the line at c looking along look, r its size. mop: the hair's colour (None: no hair, the crown blue);
    horns: (side, back, length) in r, standing off the crown; crest: (base, direction, length, width) of spikes in the
    head's own units; gape: how far the jaw drops (0 shut); eyes: "hidden" under the locks, or a colour for open eyes.
    """

    def __init__(self, k, tag, bone, c, look, r, mop=MOP, horns=(), crest=(), crest_colour=SPOT, gape=0.6,
                 fangs=False, teeth=False, eyes="hidden", snout=1.0, ink=None):
        self.k, self.tag, self.bone, self.c, self.r = k, tag, bone, np.array(c, float), r
        self.M = frame(look)
        self.mop, self.eyes_kind = mop, eyes
        M, C = self.M, self.c

        def put(shape):
            V, F = shape
            return (V * r) @ M.T + C, F
        self.put = put
        self.at = lambda p: C + M @ (np.array(p, float) * r)

        # the skull: a blue core forward and low, the hair (or a blue crown) over the top and the back
        k.add(f"face{tag}", put(ell(CORE_C, CORE_R)), BLUE, bone)
        if mop:
            k.add(f"mop{tag}", put(ell((0, 0.12, 0.14), (1.0, 1.0, 0.9))), mop, bone)
            # the hair hangs low at the back and the sides, ragged at its lower edge
            k.add(f"mopback{tag}", put(ell((0, 0.4, -0.22), (0.92, 0.72, 0.72),
                                           deform=spiky(34, 0.28, 0.24, seed=len(tag) + 9,
                                                        keep=lambda D: (D[:, 2] > -0.1) | (D[:, 1] < -0.3)))),
                  mop, bone)
        else:
            k.add(f"crown{tag}", put(ell((0, 0.1, 0.08), (0.88, 0.95, 0.8))), BLUE, bone)
        # the snout forward, its lower jaw dropped by gape, the mouth pink between them
        k.add(f"snout{tag}", put(ell((0, -0.86 * snout, -0.24), (0.56, 0.55 * snout, 0.25), rot=(-6, 0, 0))), BLUE,
              bone)
        k.add(f"jaw{tag}", put(ell((0, -0.68 * snout, -0.6 - 0.22 * gape), (0.44, 0.52 * snout, 0.2),
                                   rot=(30 * gape, 0, 0))), BLUE, bone)
        if gape > 0:
            k.add(f"mouth{tag}", put(ell((0, -0.66 * snout, -0.52 - 0.12 * gape), (0.4, 0.46 * snout,
                                                                                    0.08 + 0.2 * gape))),
                  MOUTH, bone)
        jaw_front = (-0.68 * snout - 0.48 * snout * math.cos(math.radians(30 * gape)),
                     -0.6 - 0.22 * gape - 0.48 * snout * math.sin(math.radians(30 * gape)) + 0.16)
        if fangs:
            # two white fangs standing up from the front of the lower jaw, two down from the snout
            for s in (1, -1):
                b = self.at((0.24 * s, jaw_front[0] + 0.12, jaw_front[1]))
                k.add(f"fang{tag}{s}", spike(b, M @ unit(0.1 * s, -0.15, 1), 0.36 * r, 0.2 * r, M[:, 1]), TOOTH,
                      bone, fuse=False, surface="shell")
        if teeth:
            for s in (1, -1):
                for j, x in enumerate((0.3, 0.12)):
                    b = self.at((x * s, -0.86 * snout - 0.28 + 0.05 * j, -0.5))
                    k.add(f"tooth{tag}{s}{j}", spike(b, M @ unit(0, -0.1, -1), 0.24 * r, 0.14 * r, M[:, 1]),
                          TOOTH, bone, fuse=False, surface="shell")
        # horns of hair off the crown
        for j, (sx, back, length) in enumerate(horns):
            root = np.array((0.28 * sx, 0.15, 0.55))
            d = unit(sx, back, 1.0)
            tip = root + d * (0.55 + length)
            k.add(f"horn{tag}{j}", put(tube([root, root + d * 0.55 + d * length * 0.45, tip],
                                            [0.3, 0.18, 0.035])), mop, bone)
        # a crest of spikes
        for j, (base, d, length, width) in enumerate(crest):
            b = self.at(base)
            k.add(f"crest{tag}{j}", spike(b, M @ unit(*d), length * r, width * r, M @ unit(-d[1], d[0], 0.3)
                                          if abs(d[2]) > 0.9 else M[:, 2]),
                  crest_colour, bone, fuse=False, surface="hair")
        if mop and mop != BLUE:
            # the hair over the face above the fringe, which rises to a point in the middle (the blue face's point)
            def hair(P, C=C, M=M, r=r):
                L = (P - C) @ M / r
                fringe = -0.06 + 0.6 * np.clip(1 - np.abs(L[:, 0]) / 0.3, 0, 1)
                return inside(fringe - L[:, 2], 0.004 / r) * (np.linalg.norm(L, axis=1) < 1.7)
            k.paint(hair, mop)
        # where the eyes are: under the fringe at each side of the face
        self.specs = []
        for s in (1, -1):
            d = unit(0.58 * s, -0.78, -0.02 if eyes == "hidden" else 0.1)
            p_local = on_ell(CORE_C, CORE_R, d - np.array(CORE_C) * 0)
            p_local = on_ell(CORE_C, CORE_R, d)
            # the surface normal of the core there
            n_local = (p_local - np.array(CORE_C)) / np.array(CORE_R) ** 2
            self.specs.append((self.at(p_local), M @ unit(*n_local), s))
        self.lid = srgb(mop) if mop else srgb(BLUE)
        self.ink = ink

    def mouth_line(self, colour="#20263A"):
        """A shut mouth: a zigzag line painted across the front where the snout meets the jaw, in the head's frame."""
        C, M, r = self.c, self.M, self.r
        def line(P):
            L = (P - C) @ M / r
            zz = -0.4 + 0.1 * tri(L[:, 0] * 2.2 + 0.25)
            return (inside(np.abs(L[:, 2] - zz) - 0.075, 0.012) * inside(L[:, 1] + 0.55, 0.05)
                    * inside(np.abs(L[:, 0]) - 0.5, 0.03) * (np.linalg.norm(L, axis=1) < 1.6))
        self.k.paint(line, colour)

    def lay(self, state):
        """The eyes for a state: the locks of hair over them (the same in every state), or open eyes."""
        k, M = self.k, self.M
        for p, n, s in self.specs:
            up = M[:, 2]
            right = np.cross(up, n); right /= np.linalg.norm(right)
            flip = -1.0 if np.dot(right, -M[:, 1]) > 0 else 1.0       # +u away from the snout
            if self.eyes_kind == "hidden":
                lid = self.lid
                look = eye_look(state, lambda u, v: np.tile(lid, (len(u), 1)), lid, lid)
                k.decal(p + up * 0.02 * self.r, n, (0.32 * self.r, 0.26 * self.r), look, up=up, eps=0.0035,
                        outline=LOCK, rings=12, segs=60)
            else:
                open_look = self.eyes_kind
                lk = eye_look(state, open_look, self.lid, self.ink, line=0.17)
                k.decal(p, n, (0.26 * self.r, 0.2 * self.r), (lambda u, v, lk=lk, f=flip: lk(f * u, v)), up=up,
                        eps=0.004, rings=20, segs=56)


def mantle(k, c, r, seed=4, spots=(), name="mantle", tufts=60):
    """The shaggy mantle over the shoulders: a ball of fur whose top is smooth, ragged round its sides and below; pink
    spots painted on it (directions from its centre, each with its size)."""
    k.add(name, ell(c, r, deform=spiky(tufts, 0.4, 0.2, seed=seed, keep=lambda D: D[:, 2] > 0.4)), MOP, "body")
    for d, size, stretch in spots:
        p = on_ell(c, r, d)
        q = np.array(stretch, float)
        k.paint(lambda P, p=p, q=q, size=size: inside(np.linalg.norm((P - p) / q, axis=1) - size, 0.002), SPOT,
                on=[name])


def deino(src, out):
    k = Kit(src, coat="fur")

    # the body: blue, long and low, the belly near the ground
    k.add("body", ell((0, 0.02, 0.105), (0.072, 0.115, 0.062)), BLUE, "body")
    k.add("chest", ell((0, -0.055, 0.12), (0.066, 0.06, 0.06)), BLUE, "body")
    # four short thick legs: thighs fused into the flanks, a short shin, a broad foot
    for y, bone, big in ((-0.065, "leg{s}", 1.0), (0.06, "hind{s}", 1.12)):
        k.add(f"thigh{bone[0]}", ell((0.05, y, 0.085), (0.036 * big, 0.04 * big, 0.046)), BLUE, bone, mirror=True)
        k.add(f"shin{bone[0]}", tube([(0.052, y - 0.003, 0.07), (0.054, y - 0.006, 0.03)], [0.03, 0.028]), BLUE,
              bone, mirror=True)
        k.add(f"foot{bone[0]}", ell((0.054, y - 0.016, 0.017), (0.032, 0.043, 0.019)), BLUE, bone, mirror=True)
    # the tail: short, back and a little up, a few small blue spikes on it
    k.add("tail", tube([(0, 0.1, 0.12), (0, 0.15, 0.125), (0, 0.18, 0.13)], [0.036, 0.022, 0.011]), BLUE, "tail")
    for j, (y, z, dx, lean) in enumerate(((0.15, 0.14, 0.012, 1.0), (0.15, 0.14, -0.012, 1.0),
                                          (0.172, 0.138, 0.0, 1.6))):
        k.add(f"tailspike{j}", spike((dx, y, z), unit(dx * 20, lean, 1.0), 0.022, 0.018, (1, 0, 0)), BLUE, "tail",
              fuse=False, surface="scales")

    # the mantle over the shoulders, spotted pink
    mantle(k, (0, -0.035, 0.155), (0.07, 0.08, 0.046), seed=4,
           spots=[((0.8, -0.3, 0.3), 0.013, (1, 1, 0.7)), ((-0.75, -0.45, 0.25), 0.011, (1, 1, 0.7)),
                  ((0.55, 0.6, 0.5), 0.012, (1, 1, 0.7)), ((-0.5, 0.55, 0.6), 0.01, (1, 1, 0.7)),
                  ((0.2, -0.9, 0.1), 0.009, (1, 1, 0.8))])
    # the neck: long, up and forward from the mantle into the hair
    k.add("neck", tube([(0, -0.05, 0.16), (0, -0.07, 0.215), (0, -0.085, 0.255)], [0.036, 0.032, 0.042]), MOP,
          "head")

    # the head
    head = Head(k, "", "head", (0, -0.11, 0.29), unit(0, -1, -0.15), 0.078, horns=[(0.0, 0.55, 0.55)], gape=1.3)

    k.build(voxel=0.0045, smooth=4, band=0.004)
    k.eye_states(head.lay)
    k.export(out)


if __name__ == "__main__":
    deino(*args())
