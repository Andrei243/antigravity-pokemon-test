# Huhurez sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/huhurez.py -- <the export's Huhurez.glb> <out.glb>
# From its picture (docs/art/reference/gemini/carpathia/10065-Huhurez.jpg): a large pale-grey owl standing upright,
# a big round head as wide as its body with two small ear tufts; a round flat face disc, paler than the head and
# ringed in a bold dark line, two small dark almond eyes and a little dark hooked beak; the grey streaked with dark
# dashes all over; on its chest a dark crescent moon; long folded wings barred in dark bands; a long tail barred in
# bands, held back and out; short feathered legs, grey toes and black talons.
# Its feet, its board check and the frame's names are Acvilet's (sibling("acvilet")).
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, slab, srgb, inside, unit, eye_look, polygon, args, sibling

a_ = sibling("acvilet")
g = a_.g

GREY, WINGC, PALE, DARK, RIM = "#C3C7D1", "#B2B6C3", "#E8EBF1", "#4D5062", "#3E4152"
BEAK, TOE, CLAW, EYEC = "#4A4C58", "#8E909E", "#1E1F26", "#23242E"


def frame():
    F = SimpleNamespace()
    F.H = np.array([0, -0.01, 0.8])                 # the head's centre, over its joint (0, 0, 0.66)
    F.HR = np.array([0.185, 0.168, 0.168])
    F.BODY = ((0, 0.015, 0.43), (0.19, 0.18, 0.28), (-6, 0, 0))
    F.DISC = ((0, -0.095, 0.79), (0.152, 0.085, 0.142))
    F.DISCR = (0.138, 0.13)                         # the rim's middle, as half sizes about the disc's centre
    F.SHOULDER = np.array([0.168, -0.02, 0.62])
    F.LEGX, F.LEGY, F.LEGTOP, F.ANKLE = 0.068, -0.05, 0.13, 0.03
    F.LEGR = (0.03, 0.026)
    F.TOE, F.HEEL, F.TOER, F.CLAWLEN = 0.06, 0.036, 0.0155, 0.042
    F.EYE = (0.034, 0.024)
    F.EYEAT = np.array([0.058, -0.17, 0.81])
    F.EYEDIR = unit(0.34, -0.94, 0.04)
    F.VOXEL = 0.0078
    F.TAILBASE = np.array([0.02, 0.15, 0.27])
    F.TAILDIR = unit(0.4, 0.72, -0.56)
    return F


def body(k, F):
    c, r, rot = F.BODY
    k.add("body", ell(c, r, rot=rot), GREY, "body")
    k.add("belly", ell((0, -0.03, 0.3), (0.16, 0.15, 0.17)), GREY, "body")
    k.add("head", ell(F.H, F.HR), GREY, "head")
    k.add("disc", ell(*F.DISC), PALE, "head")
    # feathered legs fused into the belly
    for s, b in ((1, "legR"), (-1, "legL")):
        k.add("thigh", ell((0.07 * s, -0.04, 0.16), (0.06, 0.06, 0.08)), GREY, b)


def face(k, F):
    """The disc pale, its bold dark rim; the head's grey kept round it."""
    c, _ = F.DISC
    ax, az = F.DISCR
    def rn(P):
        return np.hypot(P[:, 0] / ax, (P[:, 2] - c[2]) / az)
    k.paint(lambda P: inside(rn(P) - 1.0, 0.004) * inside(P[:, 1] + 0.1, 0.006), PALE)
    k.paint(lambda P: inside(np.abs(rn(P) - 1.03) - 0.065, 0.004) * inside(P[:, 1] + 0.035, 0.006), RIM)


def ear_tufts(k, F):
    for s in (1, -1):
        base = F.H + np.array([0.105 * s, -0.03, 0.13])
        tip = base + unit(0.45 * s, 0.25, 1.0) * 0.085
        k.add("tuft", g.spike(base, tip, 0.055, 0.03, side=(1, 0, 0), at=0.3), GREY, "head", fuse=False)
        k.add("tufttip", g.spike(base + (tip - base) * 0.55, tip + unit(0.45 * s, 0.25, 1.0) * 0.006, 0.035, 0.034,
                                 side=(1, 0, 0), at=0.25), DARK, "head", fuse=False)


def beak(k, F):
    k.add("beak", ell((0, -0.185, 0.762), (0.022, 0.034, 0.026), rot=(48, 0, 0), deform=g.hooked), BEAK, "head",
          fuse=False, surface="shell")


def wings(k, F):
    """Long folded wings down the sides, barred in bold dark bands sloping back."""
    J = F.SHOULDER
    pts = [J, J + (0.03, 0.05, -0.15), J + (0.005, 0.14, -0.33), J + (-0.07, 0.24, -0.47)]
    k.add("wing", blade(pts, [0.07, 0.095, 0.075, 0.02], (1, 0, 0), 0.45), WINGC, "wing{s}", mirror=True)
    def bars(P):
        t = (P[:, 2] + 0.45 * P[:, 1]) / 0.085
        return inside(np.abs(t - np.round(t)) - 0.16, 0.03) * inside(P[:, 2] - 0.56, 0.01) * (np.abs(P[:, 0]) > 0.15)
    k.paint(bars, DARK, on=["wing"])


def marks(k, F):
    """Bold dark streaks over the head and the front, and the crescent moon on the chest toward +X."""
    front = [(-0.07, 0.6, -0.15), (-0.13, 0.5, -0.5), (-0.06, 0.4, -0.15), (0.03, 0.3, 0.05), (0.14, 0.33, 0.6),
             (-0.14, 0.3, -0.6), (0.02, 0.62, 0.0)]
    for x, z, lean in front:
        k.decal((x, -0.15, z), unit(x / 0.19 * 1.1, -1.0, 0.0), (0.015, 0.058), DARK, up=(lean * 0.3, 0, 1),
                eps=0.004, rings=8, segs=32)
    # the crown, cast from above and behind the disc; the nape and the back, cast from behind
    for x, y in ((0.0, 0.02), (0.07, 0.04), (-0.07, 0.04), (0.035, 0.1), (-0.035, 0.1)):
        k.decal((x, y, 0.96), unit(x * 3, 0.3, 1.0), (0.015, 0.05), DARK, up=(0, -1, 0), eps=0.004, rings=8, segs=32)
    for x, z in ((0.0, 0.8), (0.09, 0.76), (-0.09, 0.76), (0.0, 0.62), (0.06, 0.5), (-0.06, 0.5), (0.0, 0.38),
                 (0.08, 0.3), (-0.08, 0.3)):
        k.decal((x, 0.17, z), unit(x * 4, 1.0, 0.1), (0.015, 0.056), DARK, eps=0.004, rings=8, segs=32)
    # the crescent: a dark disc, and the grey over most of it at a higher lift, offset up and out
    cn = unit(0.45, -1.0, 0.05)
    C = np.array([0.08, -0.16, 0.5])
    k.decal(C, cn, (0.07, 0.074), DARK, eps=0.004, rings=12, segs=48)
    k.decal(C + np.array([0.028, -0.004, 0.022]), cn, (0.058, 0.062), GREY, eps=0.0065, rings=12, segs=48)


def tail(k, F):
    """A long tail of three feathers back, down and out to +X, barred in three dark bands, its face to the back."""
    d0 = F.TAILDIR
    for i, (yaw, ln, w) in enumerate(((0, 0.34, 0.11), (14, 0.31, 0.1), (-14, 0.31, 0.1))):
        y = math.radians(yaw)
        d = np.array([d0[0] * math.cos(y) - d0[1] * math.sin(y), d0[0] * math.sin(y) + d0[1] * math.cos(y), d0[2]])
        d = d / np.linalg.norm(d)
        side = np.cross((0, 0, 1.0), d); side /= np.linalg.norm(side)
        b = F.TAILBASE + side * 0.02 * np.sign(yaw) + np.array([0, 0, -0.004 * abs(yaw)])
        def part(a0, a1, scale=1.0):
            prof = np.array(g.FEATHER)
            half = [(a * ln, float(np.interp(a, prof[:, 0], prof[:, 1])) * w / 2 * scale) for a in np.linspace(a0, a1, 7)]
            return half + [(a, -h) for a, h in half[::-1]]
        k.add(f"tail{i}", slab(part(0.0, 1.0), b, d, side, 0.018), GREY, "tail", fuse=False)
        for j, (a0, a1) in enumerate(((0.3, 0.42), (0.55, 0.67), (0.8, 0.92))):
            k.add(f"tailbar{i}{j}", slab(part(a0, a1, 1.04), b, d, side, 0.026), DARK, "tail", fuse=False)


# --- the eyes: small dark almonds on the pale disc ------------------------------------------------------------------
INK, EYEK, WHITEC, LIDC = srgb("#15161C"), srgb(EYEC), np.array([1.0, 1.0, 1.0]), srgb(PALE)
ALMOND = [(-1.0, -0.1), (-0.6, 0.55), (0.0, 0.85), (0.6, 0.7), (1.0, 0.15), (0.7, -0.55), (0.0, -0.8),
          (-0.6, -0.6)]


def eye_open(u, v):
    c = np.tile(EYEK, (len(u), 1))
    c[np.hypot(u + 0.28, v - 0.28) < 0.2] = WHITEC
    return c


def eyes(k, F):
    m = np.array([-1, 1, 1.0])
    def lay(state):
        look = eye_look(state, eye_open, LIDC, INK, line=0.2)
        k.decal(F.EYEAT, F.EYEDIR, F.EYE, look, eps=0.005, outline=polygon(ALMOND), rings=20, segs=56)
        k.decal(F.EYEAT * m, F.EYEDIR * m, F.EYE, lambda u, v: look(-u, v), eps=0.005,
                outline=polygon([(-a, b) for a, b in ALMOND]), rings=20, segs=56)
    return lay


def huhurez(src, out_path):
    F = frame()
    k = Kit(src, coat="fur")
    body(k, F)
    face(k, F)
    wings(k, F)
    marks(k, F)
    ear_tufts(k, F)
    beak(k, F)
    tail(k, F)
    a_.talons(k, F, foot=TOE, claw=CLAW)
    ob = k.build(voxel=F.VOXEL, band=0.008)
    a_.board(ob)
    k.eye_states(eyes(k, F))
    k.export(out_path)


if __name__ == "__main__":
    huhurez(*args())
