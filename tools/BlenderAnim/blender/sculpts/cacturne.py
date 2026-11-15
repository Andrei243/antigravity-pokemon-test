# Cacturne sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cacturne.py -- <the export's Cacturne.glb> <out.glb>
#
# Grown from Cacnea's script (its thorns, diamonds and black dots, its dark green): from the references, a tall pale
# green cactus scarecrow: a body like a gingerbread figure, narrow at the shoulders and widening to thick short legs
# on rounded feet; thick arms ending in round mitts (carried forward and down here, not out like the art's scarecrow);
# dark green pyramid thorns along the tops of the arms and down the outer sides of body and legs; four dark diamonds
# down its front; a round head under a great dark green pointed hat of four faces with a ragged brim and a small star
# on its tip, its front flap hanging over the head's left eye, so one yellow eye with a black pupil in a dark socket
# shows; a row of five black stitches for a mouth.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, unit, eye_look, sibling, args

c = sibling("cacnea")
GREEN, DARK, INK, LIGHTLINE = "#A6D27E", c.DARK, c.INK, c.LIGHTLINE
HAT = "#4A8A3C"
EYE, SOCKET = "#F2C43A", "#26321F"

HEAD = np.array([0.0, -0.01, 0.55])
HR = np.array([0.084, 0.078, 0.084])
TORSO = np.array([0.0, 0.0, 0.32])
TR = np.array([0.15, 0.1, 0.175])
SH = np.array([0.07, 0.0, 0.455]); EL = np.array([0.15, -0.065, 0.38]); HAND = np.array([0.212, -0.122, 0.302])
HIP = np.array([0.08, 0.0, 0.25]); KNEE = np.array([0.092, -0.006, 0.13]); ANKLE = np.array([0.1, -0.01, 0.076])
LEG_R = (0.072, 0.07, 0.072)


def taper(V):
    """The torso narrows toward the shoulders: a gingerbread figure's body."""
    V = V.copy()
    t = (np.clip(V[:, 2], -1, 1) + 1) / 2
    V[:, 0] *= 1 - 0.45 * t
    V[:, 1] *= 1 - 0.2 * t
    return V


def torso_x(z):
    q = np.clip((z - TORSO[2]) / TR[2], -1, 1)
    t = (q + 1) / 2
    return TR[0] * math.sqrt(max(1 - q * q, 0)) * (1 - 0.45 * t)


def body(k):
    k.add("torso", ell(TORSO, TR, deform=taper), GREEN, "body")
    k.add("neck", tube([(0, 0, 0.44), (0, -0.006, 0.5)], [0.042, 0.04]), GREEN, "body")
    k.add("head", ell(HEAD, HR), GREEN, "head")
    k.add("leg", tube([HIP, KNEE, ANKLE], LEG_R), GREEN, "leg{s}", mirror=True)
    k.add("foot", ell((0.105, -0.022, 0.032), (0.08, 0.09, 0.034)), GREEN, "leg{s}", mirror=True)
    k.add("arm", tube([SH, EL, HAND], [0.042, 0.045, 0.05]), GREEN, "arm{s}", mirror=True)
    k.add("mitt", ell(HAND + (0.008, -0.01, -0.012), (0.057, 0.056, 0.062)), GREEN, "arm{s}", mirror=True)


def thorns(k):
    # along the tops of the arms
    for j, t in enumerate((0.15, 0.42, 0.68, 0.92)):
        p = SH + (EL - SH) * min(t * 2, 1) if t < 0.5 else EL + (HAND - EL) * (t - 0.5) * 2
        n = unit(0.35, 0.15, 0.92)
        c.thorn(k, f"armthorn{j}", p + n * 0.044, n, 0.046, 0.022, "arm{s}", sides=4, mirror=True)
    # down the outer sides of the body and the legs
    for j, z in enumerate((0.39, 0.32, 0.25)):
        x = torso_x(z)
        n = unit(1, 0.05, -0.15)
        c.thorn(k, f"sidethorn{j}", np.array([x - 0.004, 0.0, z]), n, 0.046, 0.022, "body", sides=4, mirror=True)
    for j, t in enumerate((0.45, 0.85)):
        p = KNEE + (ANKLE - KNEE) * t if t > 0.5 else HIP + (KNEE - HIP) * (t * 2)
        n = unit(1, 0.05, -0.2)
        c.thorn(k, f"legthorn{j}", p + n * 0.068, n, 0.046, 0.022, "leg{s}", sides=4, mirror=True)


def hat(k):
    """A great four-faced pointed hat, its front corner hanging down over the head's left eye, a ragged brim."""
    tip = np.array([0.012, 0.025, 0.83])
    L = np.array([-0.19, 0.012, 0.6]); R = np.array([0.19, 0.022, 0.66])
    F = np.array([-0.055, -0.155, 0.548]); B = np.array([0.02, 0.17, 0.61])
    k.add("hat", hull([tip, L, R, F, B, (L + F) / 2 + (0, 0, 0.012), (R + B) / 2 + (0, 0, 0.012)]), HAT, "head",
          fuse=False, surface="leaf")
    # ragged points of the brim hanging off its corners
    for i, (p, out, down) in enumerate([(L, (-1, 0, -0.3), (0.015, -0.03, -0.045)),
                                        (R, (1, 0, -0.2), (-0.02, -0.02, -0.04)),
                                        (F, (-0.2, -1, -0.5), (0.035, 0.0, -0.04)),
                                        ((L + F) / 2, (-0.6, -0.8, -0.3), (0.0, -0.01, -0.045))]):
        o = unit(*out)
        pts = [p + (0, 0, 0.012), p - o * 0.05 + (0, 0, 0.012), p - o * 0.03 + (0, 0, -0.012), p + np.array(down),
               p - o * 0.045 + np.array(down) * 0.55]
        k.add(f"brim{i}", hull(pts), HAT, "head", fuse=False, surface="leaf")
    # a small four-pointed star on its tip, two crossed plates
    # each point a hull of its own, or the hull of the whole is a diamond
    s = tip + (0, 0, 0.014)
    face = np.array([0.0, -0.6, 0.0]); across = np.array([1.0, 0, 0]); up = np.array([0, 0, 1.0])
    for i, ang in enumerate(range(0, 360, 90)):
        d = math.cos(math.radians(ang)) * across + math.sin(math.radians(ang)) * up
        w = np.cross(d, unit(*face))
        pts = [s + d * 0.05]
        for q in (-1, 1):
            for f in (-1, 1):
                pts.append(s - d * 0.006 + w * 0.014 * q + unit(*face) * 0.015 * f)
        k.add(f"star{i}", hull(pts), HAT, "head", fuse=False, surface="leaf")


def marks(k):
    for z in (0.43, 0.365, 0.3, 0.235):
        c.diamond(k, (0, -0.1, z), (0, -1, 0.08), (0.021, 0.025))
    # the stitched mouth: five black dots in a shallow smile
    for x in (-0.036, -0.018, 0.0, 0.018, 0.036):
        z = 0.512 + 0.012 * (x / 0.036) ** 2
        c.dot(k, (x + 0.004, -0.085, z), unit(x * 6, -1, 0.1), 0.0098)


EYE_P = np.array([0.037, -0.074, 0.566])
EYE_N = unit(0.42, -0.9, 0.08)


def socket(k):
    k.decal(EYE_P, EYE_N, (0.029, 0.026), SOCKET, eps=0.0028, rings=12, segs=48)


def eye_open(u, v):
    c_ = np.tile(srgb(EYE), (len(u), 1))
    r = u * u + v * v
    c_[r > 0.78] = srgb(INK)
    c_[((u + 0.06) / 0.42) ** 2 + (v / 0.44) ** 2 < 1] = srgb(INK)
    c_[((u + 0.22) / 0.16) ** 2 + ((v - 0.2) / 0.16) ** 2 < 1] = srgb("#FFFFFF")
    return c_


def eyes(k, state):
    k.decal(EYE_P, EYE_N, (0.019, 0.019), eye_look(state, eye_open, srgb(SOCKET), srgb(LIGHTLINE), line=0.2),
            eps=0.005, rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="leaf")
    body(k)
    thorns(k)
    hat(k)
    marks(k)
    socket(k)
    k.build(voxel=0.005, smooth=3)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
