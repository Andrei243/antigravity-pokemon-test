# Aron sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/aron.py -- <the export's Aron.glb> <out.glb>
# The first of its line (lairon.py and aggron.py are grown from this file). From the references: a small quadruped
# whose head is all helmet, a great white egg of steel leaning back, as tall as the whole Pokemon; on each side of it
# a big round blue eye looking out of a black socket, with a black hole above and behind it and another below and in
# front of it; a round dark slate body under a smooth white plate over its back, the plate's lip standing off the
# body, one black hole in each of its sides and a tall pointed fin of steel standing up from its middle; four short
# white pegs of legs, pointed at the foot. The armour is smooth and rounded in both pictures, so the helmet is the
# fused head and the back plate a smooth loose shell; only the fin and the pegs are hard pieces.
#
# Written to be grown from: the line's colours, its eye in a socket, its holes in the steel and its pieces are the
# functions below; the calls that build Aron are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, srgb, inside, smoothstep, unit, eye_look, args

# --- the line's colours ---------------------------------------------------------------------------------------------
STEEL = "#E6E8EC"
DARK = "#4C5662"
HOLE = "#16181C"
SOCKET = "#1C1F25"
BLUE = srgb("#56BDEB")
DEEP = srgb("#2C86C4")
INK, WHITE = srgb("#121418"), np.array([1.0, 1.0, 1.0])
LIGHTLINE = srgb("#9AA4B0")          # the shut eye's line, on the dark socket


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre."""
    d = unit(*d)
    t = 1.0 / math.sqrt(((d / np.array(r)) ** 2).sum())
    return np.array(c, float) + d * t


def ell_normal(c, r, p):
    q = (np.array(p, float) - np.array(c, float)) / np.array(r, float) ** 2
    return unit(*q)


def disc(k, name, p, n, radius, bone, color=HOLE, thick=0.012, proud=0.003, surface="metal"):
    """A black hole in a loose plate: a flat disc set into its face, a little proud of it (a patch can't land on a loose
    piece)."""
    n = unit(*n)
    c = np.array(p, float) + n * (proud - thick / 2)
    prof = [(0, 0), (radius, 0), (radius, thick), (0, thick)]
    k.add(name, lathe(c, n, prof, seg=28), color, bone, fuse=False, surface=surface)


def peg(k, name, top, foot, radii, bone, color=STEEL, mirror=False):
    """A short white peg of a leg, thick at the body and pointed at the foot."""
    top, foot = np.array(top, float), np.array(foot, float)
    k.add(name, tube([top, top * 0.45 + foot * 0.55, foot], radii, seg=18, per=6), color, bone, fuse=False,
          mirror=mirror, surface="metal")


def socket_eye(iris_c=(-0.12, -0.02)):
    """A big blue eye in its socket: a deeper blue edge, a black upright slit toward the front, a white glint."""
    def eye(u, v):
        c = np.tile(BLUE, (len(u), 1))
        rho = np.sqrt(u * u + v * v)
        c[rho > 0.82] = DEEP
        return c
    return eye


def socket_eyes(k, E, de, half, look_open=None, lid=srgb(SOCKET), ink=LIGHTLINE, up=(0, 0, 1), pupil=(-0.12, -0.02)):
    """The eyes for a state, over sockets laid before the build: shut, the socket's dark with a light line. The slit
    pupil and its glint are patches of their own (in the colour function their edges were ragged)."""
    look_open = look_open or socket_eye()
    def eyes(state):
        k.decal(E, de, half, eye_look(state, look_open, lid, ink, line=0.16), up=up, eps=0.007, rings=20, segs=56,
                mirror=True)
        if state in ("Open", "Fierce"):
            for s in (1, -1):
                m = np.array([s, 1, 1.0])
                n = de * m
                right = np.cross(np.array(up, float), n); right /= np.linalg.norm(right)   # +u, toward the back
                upv = np.cross(n, right)
                c = E * m + right * pupil[0] * half[0] + upv * pupil[1] * half[1]
                low = 0.0 if state == "Open" else -0.25
                tall = 0.42 if state == "Open" else 0.24
                k.decal(c + upv * low * half[1], n, (0.2 * half[0], tall * half[1]), INK, up=up, eps=0.0095,
                        rings=8, segs=36)
                if state == "Open":
                    k.decal(c + right * 0.32 * half[0] + upv * 0.36 * half[1], n, (0.15 * half[0], 0.15 * half[1]),
                            WHITE, up=up, eps=0.011, rings=4, segs=20)
    return eyes


# --- Aron ----------------------------------------------------------------------------------------------------------
H = np.array([0.0, -0.075, 0.172])           # the helmet's middle (the head bone is at 0, -0.06, 0.16)
HR = np.array([0.118, 0.118, 0.132])
TILT = 22


def egg(V):
    """The helmet: an egg, fuller low in front, narrowing toward its top."""
    V = V.copy()
    z = V[:, 2]
    V[:, 0] *= 1 - 0.13 * np.clip(z, 0, 1)
    V[:, 1] *= 1 - 0.08 * np.clip(z, 0, 1)
    return V


def aron(src, out):
    k = Kit(src, coat="metal")

    # The body: a round dark slate barrel, its belly low over the stubby pegs
    BODY_C, BODY_R = np.array([0.0, 0.055, 0.108]), np.array([0.09, 0.105, 0.08])
    k.add("body", ell(BODY_C, BODY_R), DARK, "body")
    # the helmet: the head itself, a white egg leaning back; the dark nape fills it from behind
    k.add("head", ell(H, HR, rot=(-TILT, 0, 0), deform=egg), STEEL, "head")
    k.add("nape", ell((0, 0.005, 0.13), (0.08, 0.06, 0.07)), DARK, "body")

    # The plate over the back: a smooth white shell whose lip stands off the body, a black hole in each side
    PC, PR = np.array([0.0, 0.068, 0.125]), np.array([0.102, 0.114, 0.085])
    def dome(V):
        V = V.copy()
        low = V[:, 2] < 0
        V[low, 2] *= 0.38
        return V
    k.add("plate", ell(PC, PR, deform=dome), STEEL, "body", fuse=False, surface="metal")
    for s, side in ((1, "R"), (-1, "L")):
        d = np.array([s * 1.0, 0.25, 0.42])
        p = on_ell(PC, PR, d)
        disc(k, f"platehole{side}", p, ell_normal(PC, PR, p), 0.024, "body")

    # The fin standing up from the plate's middle: a hard kite of steel, its front edge steep, its back sloping
    fin = [(0, -0.01, 0.17), (0, 0.16, 0.16), (0.03, 0.06, 0.19), (-0.03, 0.06, 0.19),
           (0.016, 0.055, 0.245), (-0.016, 0.055, 0.245), (0, 0.035, 0.282), (0, 0.075, 0.278)]
    k.add("fin", hull(fin), STEEL, "body", fuse=False, surface="metal")

    # The legs: four short white pegs, pointed at the foot
    for name, y, bone in (("leg", -0.03, "leg{s}"), ("hind", 0.12, "hind{s}")):
        peg(k, name, (0.06, y, 0.085), (0.074, y - 0.012, 0.012), [0.034, 0.028, 0.01], bone, mirror=True)

    # The holes in the helmet: one above and behind each eye, one below and in front of it
    for d, half in (((0.36, 0.18, 0.92), (0.022, 0.024)), ((0.4, -0.66, -0.64), (0.02, 0.022))):
        p = on_ell(H, HR, d)
        k.decal(p, unit(*d), half, HOLE, eps=0.004, rings=10, segs=40, mirror=True)

    # The sockets the eyes look out of, black, round
    de = unit(0.66, -0.72, 0.06)
    E = on_ell(H, HR, de)
    k.decal(E, de, (0.05, 0.053), SOCKET, eps=0.004, rings=14, segs=56, mirror=True)

    k.build(voxel=0.0048, smooth=3, band=0.004)
    k.eye_states(socket_eyes(k, E, de, (0.034, 0.036)))
    k.export(out)


if __name__ == "__main__":
    aron(*args())
