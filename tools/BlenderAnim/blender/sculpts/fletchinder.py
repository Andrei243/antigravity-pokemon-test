# Fletchinder sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/fletchinder.py -- <the export's Fletchinder.glb> <out.glb>
# Fletchling grown (its frame, bill, tail of chevrons, feet and way of making eyes, imported from fletchling.py): from
# the references, a leaner bird leaning forward, its head lower and forward; an orange-red head with a pointed crest
# swept up and back from the crown; a fierce white eye with a black pupil in a black band edged with yellow that runs
# back and down from it; a long straight dark bill; a pale blue-grey breast framed by the orange running down its
# sides to a point, an orange notch at its top; an orange back with a pointed tuft before the tail; black folded wings
# with gold flame-shaped feathers at their rear; a white fluff under the belly; long black tail feathers raised up
# and back, the longest with three white chevrons; long thin black legs. Written to be grown from: Talonflame imports
# it (sibling("fletchinder")) for its crest, its mask and its eyes.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, blade, srgb, inside, unit, eye_look, polygon, args, sibling

f_ = sibling("fletchling")
g = f_.g
ORANGE, PALE, BLACK, GOLD, WHITE = "#E8502A", "#CAD3EE", "#26262C", "#F0B020", "#F4F6FB"
YELLOW, BILL, MASK = "#F4C626", "#36363E", "#1E1E24"


def frame():
    F = SimpleNamespace()
    F.H = np.array([0, -0.09, 0.218])               # the head's centre, before its joint (0, -0.07, 0.21)
    F.HR = np.array([0.06, 0.064, 0.057])
    F.BODY = ((0, 0.02, 0.165), (0.066, 0.1, 0.066), (12, 0, 0))
    F.BREAST = ((0, -0.048, 0.15), (0.058, 0.058, 0.062))
    F.NECK = ((0, -0.05, 0.2), (0.054, 0.052, 0.05))
    F.WING = ([(0.056, -0.035, 0.196), (0.064, 0.02, 0.18), (0.061, 0.083, 0.162), (0.04, 0.132, 0.154)],
              [0.026, 0.044, 0.04, 0.011], 0.3)
    F.FLAMES = np.array([0.05, 0.105, 0.165])       # where the gold feathers leave the wing's rear
    F.LEGX, F.LEGTOP, F.ANKLE, F.LEGY = 0.036, 0.112, 0.012, 0.0
    F.LEGR = (0.0078, 0.0068)
    F.TOE, F.HEEL, F.TOER = 0.046, 0.024, 0.0056
    F.BILL = (np.array([0, -0.142, 0.207]), unit(0, -1, -0.2), 0.072, 0.0155)
    F.EYEDIR = unit(0.6, -0.72, 0.26)
    F.EYE = (0.018, 0.0135)
    F.MASKDIR = unit(0.68, -0.64, 0.24)
    F.MASK = (0.033, 0.024)
    F.VOXEL = 0.0035
    F.TAILBASE = np.array([0, 0.105, 0.19])
    return F


on_head = f_.on_head


def body(k, F):
    c, r, rot = F.BODY
    k.add("body", ell(c, r, rot=rot), ORANGE, "body")
    k.add("breast", ell(*F.BREAST), ORANGE, "body")
    k.add("neck", ell(*F.NECK), ORANGE, "head")
    k.add("head", ell(F.H, F.HR), ORANGE, "head")


def breast(k, F):
    """The pale breast, an oval framed by the orange down its sides, with an orange notch at its top; pale under."""
    def m(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        oval = inside(np.hypot(x / 0.044, (z - 0.125) / 0.072) - 1, 0.03) * inside(y + 0.02, 0.006)
        notch = inside(np.abs(x) - 0.016 * np.clip((z - 0.158) / 0.035, 0, 1), 0.0015) * inside(0.158 - z, 0.002)
        under = inside(z - (0.118 - 0.2 * y), 0.004)
        return np.maximum(oval * (1 - notch), under)
    k.paint(m, PALE)


def wings_folded(k, F):
    k.add("wing", blade(*F.WING[:2], (1, 0, 0), F.WING[2]), BLACK, "wing{s}", mirror=True)
    for j, (dy, dz, ln) in enumerate(((1.0, 0.45, 0.075), (1.0, 0.12, 0.068), (0.85, 0.8, 0.055), (1.0, -0.15, 0.05))):
        d = unit(0.12, dy, dz)
        base = F.FLAMES + np.array([0, -0.01 * j, 0.004 * j])
        k.add(f"flame{j}", g.spike(base, base + d * ln, 0.03, 0.011, side=(0, -dz, dy), at=0.3), GOLD, "wing{s}",
              fuse=False, mirror=True)


def crest(k, F, colour=ORANGE, rise=(0.0, 0.06, 0.062), w=0.056, t=0.026, bone="head"):
    """A pointed crest swept up and back from the crown: a solid with width, not a plate in the midline."""
    top = F.H + np.array([0, -0.004, F.HR[2] * 0.72])
    k.add("crest", g.spike(top, top + np.array(rise), w, t, side=(0, 1, -0.6), at=0.25), colour, bone, fuse=False)


def back_tuft(k, F):
    b = np.array([0, 0.035, 0.212])
    k.add("tuft", g.spike(b, b + np.array([0, 0.06, 0.03]), 0.034, 0.016, side=(0, 0.4, -1), at=0.3), ORANGE, "body",
          fuse=False)


def fluff(k, F):
    for j, x in enumerate((-0.016, 0.0, 0.016)):
        b = np.array([x, 0.035, 0.11])
        k.add(f"fluff{j}", g.spike(b, b + np.array([x * 0.8, 0.02, -0.035]), 0.022, 0.009, side=(1, 0, 0)), WHITE,
              "body", fuse=False)


def mask(k, F, outer=YELLOW, inner=MASK, place=(0.15, 0.0, 0.75, 0.62)):
    """The black band round the eye edged with yellow, the yellow running back and down: laid before build()."""
    E = on_head(F, F.MASKDIR)
    du, dv, ru, rv = place
    Y, Bk = srgb(outer), srgb(inner)
    def look(u, v):
        c = np.tile(Y, (len(u), 1))
        c[((u + du) / ru) ** 2 + ((v - dv) / rv) ** 2 < 1] = Bk
        return c
    outline = [(-1.0, 0.15), (-0.6, 0.62), (0.2, 0.75), (0.85, 0.45), (1.0, 0.0), (0.75, -0.6), (0.1, -0.95),
               (-0.6, -0.7)]
    m = np.array([-1, 1, 1.0])
    k.decal(E, F.MASKDIR, F.MASK, look, eps=0.004, outline=polygon(outline), rings=16, segs=56)
    k.decal(E * m, F.MASKDIR * m, F.MASK, lambda u, v: look(-u, v), eps=0.004,
            outline=polygon([(-a, b) for a, b in outline]), rings=16, segs=56)


EYEWHITE, PUP = np.array([0.97, 0.97, 0.99]), srgb("#141418")
FIERCE = [(-1.0, -0.15), (-0.65, 0.2), (0.0, 0.5), (0.7, 0.82), (1.0, 0.35), (0.75, -0.6), (0.0, -0.85),
          (-0.65, -0.65)]                       # u toward the back: its top slopes down to the bill


def eye_open(u, v):
    c = np.tile(EYEWHITE, (len(u), 1))
    c[((u + 0.12) / 0.48) ** 2 + ((v + 0.05) / 0.62) ** 2 < 1] = PUP
    return c


def eyes(k, F, lid=MASK, ink="#9A9AA4", open_look=eye_open, outline=FIERCE):
    LID, INK = srgb(lid), srgb(ink)
    E = on_head(F, F.EYEDIR)
    m = np.array([-1, 1, 1.0])
    def lay(state):
        look = eye_look(state, open_look, LID, INK, line=0.19)
        kw = dict(eps=0.0075, rings=20, segs=56)
        k.decal(E, F.EYEDIR, F.EYE, look, outline=polygon(outline), **kw)
        k.decal(E * m, F.EYEDIR * m, F.EYE, lambda u, v: look(-u, v),
                outline=polygon([(-a, b) for a, b in outline]), **kw)
    return lay


def fletchinder(src, out_path):
    F = frame()
    k = Kit(src, coat="fur")
    body(k, F)
    breast(k, F)
    wings_folded(k, F)
    crest(k, F)
    back_tuft(k, F)
    fluff(k, F)
    f_.bill(k, F, colour=BILL)
    f_.tail(k, F, [(0, 0.19, 0.052, BLACK, [(0.36, 0.075), (0.53, 0.075), (0.7, 0.075)], WHITE),
                   (16, 0.165, 0.046, BLACK, [], None),
                   (-16, 0.15, 0.044, BLACK, [], None)], pitch=38, thick=0.009)
    f_.legs(k, F, foot=BLACK, claw=MASK)
    mask(k, F)
    k.build(voxel=F.VOXEL, band=0.005, wband=0.04)
    k.eye_states(eyes(k, F))
    k.export(out_path)


if __name__ == "__main__":
    fletchinder(*args())
