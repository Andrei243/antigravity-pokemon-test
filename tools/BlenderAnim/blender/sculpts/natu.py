# Natu sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/natu.py -- <the export's Natu.glb> <out.glb>
# A tiny round green bird, head and body one ball: a large blunt yellow beak, big half-lidded eyes (a white almond
# under a heavy black lid line, a black pupil pressed up against it), one long red crest feather laid back from the
# crown, stubby wings like rolled cuffs (a yellow front, a black band, red feathers with black between), a little
# fan of red tail feathers under them, thin red legs with white claws. Written to be grown from: Xatu imports it
# (sibling("natu")) and keeps its colours and its way of making the eyes, the beak and the feet.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, boxy, slab, lathe, outward, srgb, inside, unit, eye_look, polygon, args, sibling)

# --- the line's colours -------------------------------------------------------------------------------------------
GREEN, YELLOW, RED, BLACK, WHITE = "#8CCB3A", "#F4C62C", "#E0475E", "#1C1C22", "#F6F6F4"
REDDARK = "#B83248"
INK, WHITEC, PUP = srgb("#141418"), np.array([1.0, 1.0, 1.0]), srgb("#121216")


def oriented_ell(c, radii, d, up=(0, 0, 1.0), deform=None):
    """An ellipsoid whose y radius lies along d, its z radius toward up: right-handed, so wound outward."""
    d = unit(*d)
    ex = np.cross(d, up); ex /= np.linalg.norm(ex)
    ez = np.cross(ex, d)
    V, F = ell((0, 0, 0), radii, deform=deform)
    V = V @ np.stack([ex, d, ez])
    return outward((V + np.array(c, float), F))


# --- the frame ----------------------------------------------------------------------------------------------------
def frame():
    F = SimpleNamespace()
    F.C = np.array([0, -0.005, 0.228])               # the ball's centre, over the body's joint (0, 0, 0.2)
    F.CR = np.array([0.142, 0.138, 0.137])
    F.BONE = "body"
    # the eye: big, half lidded, high on the ball's side and turned well toward the front
    F.EYEDIR = unit(0.7, -0.64, 0.26)
    F.EYE = (0.058, 0.032)
    # the beak: blunt, straight out of the face a little under the eyes
    F.BEAK = ((0, -0.168, 0.212), (0.05, 0.072, 0.043))
    F.BEAKTILT = 6
    # the wing: a short roll from just behind the cheek back along the side
    F.WINGFRONT = np.array([0.112, -0.055, 0.215])
    F.WINGDIR = unit(0.07, 1.0, -0.14)
    F.WINGR = 0.068
    # the legs (pidgey's feet)
    F.LEGX, F.LEGTOP, F.ANKLE, F.LEGY = 0.052, 0.13, 0.024, -0.005
    F.LEGR = (0.0165, 0.0155)
    F.TOE, F.HEEL, F.TOER = 0.05, 0.028, 0.012
    F.TAILBASE = np.array([0, 0.11, 0.155])
    F.VOXEL = 0.0055
    return F


def eye_centre(F, C=None, CR=None):
    C = F.C if C is None else C
    CR = F.CR if CR is None else CR
    de = F.EYEDIR
    return C + de / np.linalg.norm(de / CR)


# --- the parts ----------------------------------------------------------------------------------------------------
def ball(k, F):
    k.add("ball", ell(F.C, F.CR), GREEN, F.BONE)


def blunt(V):
    """A beak: narrowing to a rounded end, its upper line nearly straight and the lower one curving up to it."""
    t = np.clip(-V[:, 1], 0, 1)
    V = V.copy()
    V[:, 0] *= 1 - 0.5 * t
    V[:, 2] *= 1 - 0.45 * t
    V[:, 2] += 0.18 * t * t * (V[:, 2] < 0)
    return V


def beak(k, F, colour=YELLOW, deform=blunt, bone=None):
    c, r = F.BEAK
    k.add("beak", ell(c, r, rot=(F.BEAKTILT, 0, 0), deform=deform), colour, bone or F.BONE, fuse=False,
          surface="shell")


def wing(k, F):
    """
    A short roll along the side: a yellow domed front, a black band, then three red feathers stacked with black
    between them (a black core shows in the gaps).
    """
    d, f0, R = F.WINGDIR, F.WINGFRONT, F.WINGR
    k.add("wingfront", lathe(f0, d, [(0, 0), (0.035, 0.004), (0.055, 0.016), (R, 0.04), (R, 0.07), (0, 0.07)]),
          YELLOW, "wing{s}", fuse=False, mirror=True)
    k.add("wingband", lathe(f0, d, [(0, 0.064), (R + 0.002, 0.064), (R + 0.002, 0.098), (0, 0.098)]),
          BLACK, "wing{s}", fuse=False, mirror=True)
    k.add("wingred", lathe(f0, d, [(0, 0.094), (R - 0.001, 0.094), (R - 0.003, 0.19), (R - 0.014, 0.204),
                                   (0, 0.207)]), RED, "wing{s}", fuse=False, mirror=True)
    # the black lines between the red feathers: two level plates through the roll, standing just proud of it
    up = np.array([0, 0, 1.0]) - d * d[2]
    up /= np.linalg.norm(up)
    side = np.cross(d, up)
    for i, off in enumerate((0.024, -0.024)):
        w = math.sqrt(R * R - off * off) + 0.003
        out_ = [(0.096, w), (0.19, w), (0.21, w - 0.014), (0.21, -w + 0.014), (0.19, -w), (0.096, -w)]
        k.add(f"wingline{i}", slab(out_, f0 + up * off, d, side, 0.011), BLACK, "wing{s}", fuse=False, mirror=True)


def crest(k, F):
    """One long red feather from the crown, laid back and turned toward +X: narrow at its root, a round paddle."""
    base = F.C + np.array([0, 0.035, F.CR[2] * 0.93])
    yaw, pitch = math.radians(32), math.radians(26)
    d = np.array([math.sin(yaw) * math.cos(pitch), math.cos(yaw) * math.cos(pitch), math.sin(pitch)])
    side = np.cross((0, 0, 1.0), d); side /= np.linalg.norm(side)
    ez = np.cross(d, side)                          # the feather's width, upright-ish
    L = 0.2
    out_ = [(0.0, 0.014), (0.25 * L, 0.017), (0.5 * L, 0.024), (0.72 * L, 0.03), (0.88 * L, 0.028), (0.97 * L, 0.018),
            (L, 0.0), (0.97 * L, -0.018), (0.88 * L, -0.028), (0.72 * L, -0.03), (0.5 * L, -0.024),
            (0.25 * L, -0.017), (0.0, -0.014)]
    k.add("crest", slab(out_, base - d * 0.03, d, ez, 0.03), RED, F.BONE, fuse=False)


def tail(k, F):
    """A little fan of red feathers out behind and under the wings."""
    p = sibling("pidgey")
    T = SimpleNamespace(TAILBASE=F.TAILBASE)
    p.tail(k, T, [(0, 0.12, 0.06, RED, None), (24, 0.11, 0.055, RED, None), (-24, 0.11, 0.055, RED, None)],
           pitch=-22, thick=0.022, roll=1.2)


def legs(k, F, foot=RED, claw=WHITE):
    sibling("pidgey").legs(k, F, foot=foot, claw=claw)


# --- the eyes -----------------------------------------------------------------------------------------------------
ALMOND = [(-1.0, 0.18), (-0.78, 0.62), (-0.35, 0.86), (0.2, 0.88), (0.7, 0.72), (1.0, 0.3), (0.85, -0.3),
          (0.4, -0.72), (-0.15, -0.82), (-0.65, -0.55)]


def eye_open_of(outline, pupil=(-0.12, 0.2, 0.42, 0.66), rim=0.18, lidline=0.42, glint=True):
    """
    An open eye in an outline: white, a black rim, a thick black lid line across its top, a black pupil pressed up
    under it (u toward the back, v up), a white glint in the pupil.
    """
    edge = polygon(outline)
    def look(u, v):
        th = np.arctan2(v, u)
        rad = np.array([edge(t) for t in th])
        rel = np.hypot(u, v) / rad
        c = np.tile(WHITEC, (len(u), 1))
        pu, pv, pa, pb = pupil
        c[((u - pu) / pa) ** 2 + ((v - pv) / pb) ** 2 < 1] = PUP
        if glint:
            c[((u - pu + 0.12) / 0.15) ** 2 + ((v - pv - 0.08) / 0.17) ** 2 < 1] = WHITEC
        c[rel > 1 - rim] = INK
        c[v > lidline + 0.18 * u * u] = INK
        return c
    return look


eye_open = eye_open_of(ALMOND)


def eyes(k, F, lid=GREEN, open_look=eye_open, outline=ALMOND, C=None, CR=None, half=None):
    LID = srgb(lid)
    E = eye_centre(F, C, CR)
    half = half or F.EYE
    m = np.array([-1, 1, 1.0])
    def lay(state):
        look = eye_look(state, open_look, LID, INK, line=0.18)
        # two patches laid by hand (a mirrored patch's outline is not mirrored): u toward the back on both
        k.decal(E, F.EYEDIR, half, look, eps=0.006, outline=polygon(outline), rings=20, segs=56)
        k.decal(E * m, F.EYEDIR * m, half, lambda u, v: look(-u, v), eps=0.006,
                outline=polygon([(-a, b) for a, b in outline]), rings=20, segs=56)
    return lay


def natu(src, out):
    F = frame()
    k = Kit(src, coat="fur")
    ball(k, F)
    beak(k, F)
    wing(k, F)
    crest(k, F)
    tail(k, F)
    legs(k, F)
    k.build(voxel=F.VOXEL)
    k.eye_states(eyes(k, F))
    k.export(out)


if __name__ == "__main__":
    natu(*args())
