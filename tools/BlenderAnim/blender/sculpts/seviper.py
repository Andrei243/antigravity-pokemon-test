# Seviper sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/seviper.py -- <the export's Seviper.glb> <out.glb>
# Laid as ekans.py's line (imported: its Line, a tube a segment on its own bone through the chain of joints).
# From the references: a long, thick, dark navy serpent lying in a coil on the ground, the inner coil stacked on the
# outer, big yellow hexagonal marks on the tops of the coils; its neck rises out of the coil at the front to a big
# wedge of a head carried forward: the crown yellow, a yellow crescent behind each eye, the snout and the jaw pale
# purple, a purple spike over each eye; a round red eye with a dark pupil; two long red fangs hanging from the upper
# jaw's front. Jagged purple scars down the front of the neck. Behind, the tail rises to a purple band and ends in a
# great blade, dark with a red edge along its back and a notch out of its front.
# (The pictures' coil runs sideways along the ground; the game's skeleton lays it round in a ring, which decides.)
# The export's chain: body and tail at (0.14, 0.34, 0.3) behind, seg1 (0.2, 0.2, 0.07) round the coil on the ground to
# seg11 (-0.084, -0.093, 0.146), seg12 (0, -0.05, 0.24) up the neck, head (0, -0.06, 0.44).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args, outward, hull, slab, \
    ring_band, polygon, tri, sibling

e = sibling("ekans")

NAVY, YELLOW, PURPLE, RED, BLADE_RED = "#283652", "#F0C83C", "#A898D8", "#E0545E", "#E8687A"
EYE_RED, PUPIL, INK = "#E2404E", "#1A1018", "#16121C"

LINE = [(0.17, 0.385, 0.37), (0.15, 0.345, 0.27), (0.18, 0.28, 0.15), (0.205, 0.17, 0.078), (0.175, -0.03, 0.076),
        (0.06, -0.21, 0.076), (-0.11, -0.195, 0.076), (-0.21, -0.05, 0.076), (-0.175, 0.105, 0.078),
        (-0.03, 0.193, 0.08), (0.1, 0.135, 0.15), (0.152, 0.01, 0.175), (0.1, -0.09, 0.185), (0.0, -0.128, 0.2),
        (-0.06, -0.1, 0.26), (-0.035, -0.075, 0.355), (0.0, -0.075, 0.435)]
RADS = [0.028, 0.038, 0.052, 0.066, 0.073, 0.075, 0.076, 0.077, 0.077, 0.077, 0.076, 0.075, 0.074, 0.072, 0.069,
        0.066, 0.064]
BONES = ["tail", "tail", "body", "seg1", "seg2", "seg3", "seg4", "seg5", "seg6", "seg7", "seg8", "seg9", "seg10",
         "seg11", "seg12", "seg12"]
HC = np.array([0.0, -0.145, 0.5])            # the head's middle
HS = 1.25                                     # its size: the pictures' head is a third of the length
EYE = np.array([0.062, -0.04, 0.012])        # the eye's place about it


def head(k):
    def wedge(V):
        V = V.copy()
        t = np.clip(-V[:, 1], 0, 1)
        V[:, 0] *= 1 - 0.35 * t
        V[:, 2] *= 1 - 0.25 * t
        return V
    def hp(p):
        return HC + np.array(p, float) * HS
    k.add("skull", ell(HC, np.array((0.074, 0.112, 0.056)) * HS, rot=(-6, 0, 0), deform=wedge), NAVY, "head")
    k.add("cheek", ell(hp((0, 0.03, -0.02)), np.array((0.07, 0.07, 0.048)) * HS), NAVY, "head")
    k.add("jaw", ell(hp((0, -0.05, -0.04)), np.array((0.05, 0.075, 0.026)) * HS, rot=(12, 0, 0)), PURPLE, "head")
    k.add("nape", tube([LINE[-2], LINE[-1], hp((0, 0.05, -0.025))], [0.066, 0.064, 0.064]), NAVY, "head")

    def q(P):
        return (P - HC) / HS
    E = EYE.copy()
    def eye_d(P):
        Q = q(P); Q[:, 0] = np.abs(Q[:, 0])
        return np.linalg.norm(Q - E, axis=1)
    # the crown yellow from the snout's top back over the head
    k.paint(lambda P: inside(0.006 + 0.13 * (q(P)[:, 1] + 0.1) - q(P)[:, 2], 0.003)
            * (q(P)[:, 1] < 0.075) * (q(P)[:, 1] > -0.115), YELLOW, on=["skull", "cheek"])
    # the snout and the jaw pale purple, and round the eye's front
    k.paint(lambda P: (inside(q(P)[:, 2] + 0.002 + 0.12 * (q(P)[:, 1] + 0.03), 0.003) * (q(P)[:, 1] < 0.0)
                       + inside(eye_d(P) - 0.03, 0.003) * (q(P)[:, 1] < E[1] + 0.008)).clip(0, 1)
            * (q(P)[:, 2] < 0.035), PURPLE, on=["skull", "cheek", "jaw"])
    # a yellow crescent behind each eye
    k.paint(lambda P: inside(eye_d(P) - 0.058, 0.003) * inside(0.03 - eye_d(P), 0.003)
            * (q(P)[:, 1] > E[1] + 0.004) * (q(P)[:, 2] < 0.03), YELLOW, on=["skull", "cheek"])

    # a purple spike over each eye, rising up and back
    for s in (1, -1):
        b = hp((0.05 * s, -0.035, 0.035))
        pts = [b + np.array(d) * HS for d in ((0.012 * s, -0.012, 0.0), (0.012 * s, 0.022, 0.0),
                                              (-0.012 * s, 0.005, -0.006), (0.016 * s, 0.006, 0.006),
                                              (0.022 * s, 0.04, 0.05))]
        k.add("brow" + ("R" if s > 0 else "L"), hull(pts), PURPLE, "head", fuse=False, surface="shell")
    # two long red fangs from the upper jaw's front corners, hanging down
    for s in (1, -1):
        a = hp((0.026 * s, -0.098, -0.022))
        pts = [a, a + (-0.002 * s, -0.008, -0.045), a + (-0.004 * s, -0.004, -0.09), a + (-0.006 * s, 0.006, -0.125)]
        k.add("fang" + ("R" if s > 0 else "L"), outward(blade(pts, [0.013, 0.011, 0.007, 0.002], (0, 1, 0), 0.7)),
              RED, "head", fuse=False, surface="shell")


def tail_blade(k, line):
    """The tail's blade: dark, its back edge red, a notch out of its front, turned out to the right of straight back."""
    c, t, r = line.at(0.0)
    base = c - t * 0.01
    ex = unit(0.8, 0.6, 0.0)                       # back and well out to +X
    ez = np.array([0, 0, 1.0])
    dark = [(-0.03, -0.02), (0.035, -0.02), (0.08, 0.07), (0.115, 0.16), (0.14, 0.24), (0.07, 0.19),
            (0.02, 0.14), (0.034, 0.105), (-0.02, 0.098), (-0.034, 0.05)]
    k.add("blade", slab(dark, base, ex, ez, 0.026), NAVY, "tail", fuse=False, surface="shell")
    redp = [(0.028, -0.005), (0.083, 0.07), (0.117, 0.158), (0.144, 0.247), (0.088, 0.17), (0.055, 0.07)]
    k.add("bladeedge", slab(redp, base, ex, ez, 0.032), BLADE_RED, "tail", fuse=False, surface="shell")
    # the purple band round the tail under the blade
    c1, t1, r1 = line.at(1.0)
    k.add("band", ring_band(c1, t1, r1 + 0.003, 0.03, thick=0.007), PURPLE, "tail", fuse=False)


def hexes(k, line):
    """Big yellow hexagons on the tops and outsides of the coils."""
    hexa = polygon([(math.cos(a), math.sin(a)) for a in np.radians(np.arange(0, 360, 60) + 30)])
    for s, up in ((3.4, 0.5), (4.8, 0.8), (6.2, 0.8), (7.6, 0.8), (9.6, 1.6), (11.0, 1.6), (12.3, 1.4)):
        c, t, r = line.at(s)
        out = np.array([c[0], c[1], 0.0]); out /= np.linalg.norm(out) + 1e-9
        n = out + np.array([0, 0, up])
        n = n - t * (n @ t); n /= np.linalg.norm(n)
        k.decal(c + n * r, n, (0.036, 0.036), YELLOW, eps=0.004, outline=hexa, rings=8, segs=36, up=t)


def scars(k, line):
    """Jagged purple scars down the neck's front."""
    def mask(Q):
        j, s, d = line.place(Q)
        par = line.S[j]
        width = 0.74 + 0.2 * tri(s / 0.05)
        return inside(width - d, 0.003) * (par > 13.7) * (par < 15.6)
    k.paint(mask, PURPLE, on=["seg"])


def lay_eyes(k):
    CR, CP, CI, CW, CN = srgb(EYE_RED), srgb(PUPIL), srgb(INK), srgb("#FFFFFF"), srgb(PURPLE)
    def eye(u, v):
        c = np.tile(CR, (len(u), 1))
        c[u * u + v * v > 0.84 ** 2] = CI
        c[((u + 0.08) / 0.26) ** 2 + (v / 0.42) ** 2 < 1] = CP
        c[((u + 0.3) / 0.14) ** 2 + ((v - 0.38) / 0.14) ** 2 < 1] = CW
        return c
    E = HC + EYE * HS
    de = unit(0.78, -0.42, 0.42)
    def eyes(state):
        look = eye_look(state, eye, CN, CI, line=0.2)
        k.decal(E, de, (0.024 * HS, 0.024 * HS), look, eps=0.005, rings=20, segs=56, mirror=True)
    return eyes


def seviper(src, out):
    k = Kit(src, coat="scales")
    line = e.Line(LINE, RADS, rise=(0.2, 0.3))
    line.tubes(k, BONES, NAVY)
    head(k)
    tail_blade(k, line)
    scars(k, line)
    hexes(k, line)
    k.build(voxel=0.0062, smooth=4, band=0.005)
    k.eye_states(lay_eyes(k))
    k.export(out)


if __name__ == "__main__":
    seviper(*args())
