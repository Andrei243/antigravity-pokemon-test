# Shelgon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/shelgon.py -- <the export's Shelgon.glb> <out.glb>
# From the references: Bagon's dome of rock grown round the whole body into a white ball of a shell. Three rounded
# ridges arch over its top from the brow back, as Bagon's lobes did, a thick ridge runs along each side and curls out
# and up at its front end; the lower front is panelled, the seams running out from a dark grey hexagon of a slot low
# on the front, where two angry yellow eyes (a black pupil, the top cut by a lid sloping down to the middle) glare
# out. Four short grey legs splayed out under it, red flesh showing between them and the shell.
# Bagon's line (sibling("bagon")): its grooved rock and its eye cut by a brow.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, polygon, eye_look, args, sibling

bg = sibling("bagon")

SHELL, SEAM = "#E9EAEE", "#BCC0C8"
SLOT = "#6C7078"
LEG, FLESH = "#7A7E86", "#C8384A"
EYE_YELLOW = srgb("#F2D35A")
INK = bg.INK
PALE = srgb("#F6E6A0")

# The shell: a ball, a little lower than it is wide
C, R = np.array([0.0, 0.005, 0.207]), np.array([0.178, 0.185, 0.165])
HEX = polygon([(1, 0), (0.5, 0.87), (-0.5, 0.87), (-1, 0), (-0.5, -0.87), (0.5, -0.87)])
HEXC = np.array([0.0, -0.17, 0.165])                 # the slot's middle, low on the front
HEXD = unit(0, -1, -0.12)
HEXH = (0.064, 0.06)


def on_shell(d, lift=1.0):
    return bg.on_ell(C, R * lift, d)


def arc(x, a0, a1, lift, n=9):
    """Points over the shell in the plane at x, from angle a0 to a1 (0 straight ahead, 90 straight up, 180 back)."""
    pts = []
    for a in np.linspace(a0, a1, n):
        t = math.radians(a)
        yz = np.array([-math.cos(t), math.sin(t)])
        k = math.sqrt(max(1 - (x / R[0]) ** 2, 0.05))
        pts.append((x, C[1] + yz[0] * R[1] * k * lift, C[2] + yz[1] * R[2] * k * lift))
    return pts


def shelgon(src, out):
    k = Kit(src, coat="shell")

    k.add("shell", ell(C, R), SHELL, "body")
    # The three ridges arching over the top from the brow back, each ending in a round lobe over the front, the outer
    # two lower down the sides
    for name, x, a0, a1, r in (("ridge0", 0.0, 40, 168, 0.068), ("ridgeR", 0.097, 44, 160, 0.064),
                               ("ridgeL", -0.097, 44, 160, 0.064)):
        p = arc(x, a0, a1, 0.82)
        k.add(name, tube(p, [r * 1.1] + [r] * (len(p) - 2) + [r * 0.7]), SHELL, "body")
    # The thick ridge along each side, curling out and up at its front end
    k.add("flange", blade([(0.13, 0.12, 0.225), (0.16, 0.03, 0.23), (0.172, -0.05, 0.24), (0.19, -0.1, 0.26)],
                          [0.04, 0.05, 0.048, 0.034], (0, 0, 1), 0.55), SHELL, "body", mirror=True)
    # The red flesh under the shell, showing round the tops of the legs
    k.add("flesh", ell((0, 0.0, 0.07), (0.1, 0.11, 0.03)), FLESH, "body", surface="skin")

    # The legs: short grey stumps splayed out from under the shell, tapering to a blunt point
    for name, y, bone in (("leg", -0.085, "leg{s}"), ("hind", 0.085, "hind{s}")):
        sy = -1 if y < 0 else 1
        k.add(name, tube([(0.085, y, 0.09), (0.115, y + 0.012 * sy, 0.045), (0.135, y + 0.02 * sy, 0.008)],
                         [0.048, 0.04, 0.025]), LEG, bone, mirror=True, surface="skin")

    # The seams of the lower front: lines running out from the slot's corners
    ax = unit(*(HEXC - C))
    def seams(P):
        q = P - C
        dist = np.linalg.norm(q, axis=1)
        along = (q @ ax) / np.maximum(dist, 1e-6)
        m = np.zeros(len(P))
        for a in range(6):
            t = math.radians(a * 60)
            side = unit(math.cos(t), 0, math.sin(t))
            side = side - ax * (side @ ax)
            n = np.cross(ax, side / np.linalg.norm(side))
            pos = (q @ (side / np.linalg.norm(side))) > 0
            m = np.maximum(m, inside(np.abs(q @ n) - 0.004, 0.0025) * pos)
        return m * (along > 0.35) * (along < 0.93) * (P[:, 2] < 0.31)
    k.paint(seams, SEAM, on=["shell"])

    # The slot: a dark hexagon, the eyes in it
    k.decal(HEXC, HEXD, HEXH, SLOT, eps=0.004, outline=HEX, rings=12, segs=72)

    # The eyes: yellow, a black pupil toward the middle, the top cut by a lid sloping down to the middle; a pale line
    # for the closed states, since the slot round them is dark
    cut = bg.brow_cut(0.28, 0.5)
    def eye(u, v):
        reach = np.array([cut(t) for t in np.arctan2(v, u)])
        rho = np.sqrt(u * u + v * v) / reach
        c = np.tile(INK, (len(u), 1))
        c[rho < 0.8] = EYE_YELLOW
        c[(((u + 0.12) / 0.26) ** 2 + ((v + 0.12) / 0.42) ** 2 < 1) & (rho < 0.8)] = INK
        return c
    ex, ez = 0.031, 0.173
    E = np.array([ex, C[1] - R[1] * math.sqrt(1 - (ex / R[0]) ** 2 - ((ez - C[2]) / R[2]) ** 2), ez])
    de = unit(0.12, -1, -0.05)
    m = np.array([-1, 1, 1.0])
    lid = srgb(SLOT)
    def eyes(state):
        look = eye_look(state, eye, lid, PALE, line=0.17)
        k.decal(E, de, (0.026, 0.021), look, eps=0.007, rings=20, segs=56, outline=cut)
        k.decal(E * m, de * m, (0.026, 0.021), lambda u, v: look(-u, v), eps=0.007, rings=20, segs=56,
                outline=lambda th: cut(math.pi - th))

    k.build(voxel=0.006, smooth=4, band=0.004)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    shelgon(*args())
