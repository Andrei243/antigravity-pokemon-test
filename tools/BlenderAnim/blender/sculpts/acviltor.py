# Acviltor sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/acviltor.py -- <the export's Acviltor.glb> <out.glb>
# Acvilet grown (its frame, mask, eyes, hooked beak, wing plates and talons, imported from acvilet.py): from its
# picture (docs/art/reference/gemini/carpathia/10064-Acviltor.jpg), a powerful golden eagle, its body dark brown;
# a golden cape of feathers over the crown, the nape and the shoulders, laid back in points; the face dark brown round
# a fierce orange eye whose top is cut by the brow sloping to the beak; a great hooked yellow beak; a white throat
# and bib coming to a point on the chest; wings raised mid-beat, golden coverts over long dark-brown flight feathers
# that hang down, their ends bronze; a fan of a tail, dark brown edged with gold; shaggy white thighs; short thick
# yellow legs and yellow feet like a fighter's fists, with dark talons.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, tri, unit, eye_look, polygon, args, sibling

a_ = sibling("acvilet")
g = a_.g
GOLD, WHITE, FOOT, BEAK, BEAKLOW = a_.GOLD, a_.WHITE, a_.FOOT, a_.BEAK, a_.BEAKLOW
BROWN, BRONZE, CLAW, IRIS = "#4A3829", "#B9803F", "#3A2F28", "#EE7428"
GOLD = "#E3AE42"


def frame():
    F = a_.frame()
    F.H = np.array([0, -0.14, 0.91])                # the head's centre, forward of its joint (0, -0.1, 0.86)
    F.HR = np.array([0.178, 0.17, 0.156])
    F.R = 0.175
    F.BODY = ((0, 0.02, 0.525), (0.205, 0.19, 0.275), (-12, 0, 0))
    F.CHEST = ((0, -0.095, 0.62), (0.165, 0.13, 0.16))
    F.NECK = ((0, -0.085, 0.78), (0.13, 0.12, 0.1))
    F.DOWN = (64, 0.3, 0.035)
    F.THIGH = ((0.108, -0.055, 0.285), (0.1, 0.1, 0.105))
    F.SHOULDER = np.array([0.18, 0.02, 0.705])
    F.WINGLEN = 2.0
    F.ELEV, F.SWEEP = 58, 24
    F.TAILBASE = np.array([0, 0.19, 0.37])
    F.TAIL = [(0, 0.27, 0.095), (18, 0.255, 0.09), (-18, 0.255, 0.09), (36, 0.23, 0.085), (-36, 0.23, 0.085)]
    F.LEGX, F.LEGY, F.LEGTOP, F.ANKLE = 0.108, -0.065, 0.23, 0.05
    F.LEGR = (0.036, 0.033)
    F.TOE, F.HEEL, F.TOER, F.CLAWLEN = 0.128, 0.072, 0.028, 0.08
    F.BEAK = ((0, -0.338, 0.89), (0.062, 0.112, 0.062), 16)
    F.BEAKLOW = ((0, -0.298, 0.832), (0.046, 0.07, 0.019), -8)
    F.EYEDIR = unit(0.56, -0.8, 0.12)
    F.EYE = (0.043, 0.035)
    F.VOXEL = 0.0085
    # the dark face round the eye in (angle round the head x R, z - the eye's height), its right half from the top
    # middle round to the bottom middle; the gold of the crown above it
    F.FACE = [(0.0, 0.04), (0.05, 0.05), (0.1, 0.05), (0.16, 0.035), (0.215, 0.0), (0.24, -0.05), (0.2, -0.09),
              (0.12, -0.1), (0.0, -0.1)]
    # the white throat and bib in (x, z), right half: from under the beak to a point on the chest
    F.BIB = [(0.0, 0.84), (0.07, 0.83), (0.1, 0.78), (0.085, 0.72), (0.07, 0.66), (0.04, 0.62), (0.03, 0.635),
             (0.0, 0.58)]
    return F


def colours(k, F):
    """The gold crown and nape painted over the dark body's top; the dark face; the white throat and bib."""
    # gold over the head, the nape and the top of the back and shoulders (a cape), brown below
    def cape(P):
        a = np.arctan2(P[:, 0], P[:, 1] - 0.0)
        low = 0.7 + 0.11 * np.clip(np.abs(a) / 2.2, 0, 1) ** 1.5 + 0.02 * tri(a * 3.5)
        return inside(low - P[:, 2], 0.004)
    k.paint(cape, GOLD, on=["head", "neck", "body", "chest", "wingroot"])
    E = a_.eye_centre(F)
    face = g.mirrored(F.FACE)
    k.paint(lambda P: g.poly_mask(a_.round_head(F, P)[0], P[:, 2] - E[2], face, edge=0.003)
            * (P[:, 1] < F.H[1] + 0.03), BROWN)
    bib = g.mirrored(F.BIB)
    k.paint(lambda P: g.poly_mask(P[:, 0], P[:, 2], bib, edge=0.003) * inside(P[:, 1] + 0.06, 0.006), WHITE)


def cape_feathers(k, F):
    """The cape's edge: gold feathers laid back and down over the nape and shoulders, and the crown's laid back."""
    for j, a in enumerate((-70, -45, -22, 0, 22, 45, 70)):
        t = math.radians(a)
        root = np.array([math.sin(t) * 0.16, 0.06 + math.cos(t) * 0.08, 0.83 - 0.03 * abs(a) / 70])
        tip = root + unit(math.sin(t) * 0.5, 0.55 + 0.3 * math.cos(t), -1.0) * (0.13 - 0.02 * abs(a) / 70)
        k.add("cape", g.spike(root, tip, 0.075, 0.03, side=(math.cos(t), -math.sin(t), 0), at=0.35), GOLD, "head",
              fuse=False)
    top = F.H + np.array([0, 0.03, F.HR[2] * 0.82])
    for x in (-0.07, -0.035, 0.0, 0.035, 0.07):
        base = top + np.array([x, -0.06 + 0.3 * abs(x), -0.01 - 2 * x * x])
        tip = base + unit(x * 2.5, 1.0, 0.42 - abs(x) * 2) * (0.15 - abs(x) * 0.5)
        k.add("crown", g.spike(base, tip, 0.06, 0.028, side=(1, 0, -x * 4), at=0.3), GOLD, "head", fuse=False)


# the golden coverts' plate in (along the span, across the chord: down and back)
COVERTS = [(0.0, -0.04), (0.15, -0.06), (0.3, -0.056), (0.42, -0.03), (0.5, 0.02), (0.48, 0.1), (0.38, 0.16),
           (0.24, 0.19), (0.1, 0.19), (0.0, 0.15)]
# the flight feathers: (along the span, across the chord at the root, turn from the chord toward the span in
# degrees, length)
PRIMARIES = [(0.06, 0.13, -4, 0.3), (0.13, 0.13, 0, 0.33), (0.2, 0.125, 4, 0.36), (0.27, 0.115, 8, 0.39),
             (0.34, 0.1, 12, 0.42), (0.4, 0.08, 17, 0.43), (0.45, 0.05, 23, 0.4)]


def feather(L, w, a0=0.0):
    """A long flight feather's outline from a0 of its length to its point."""
    half = [(0.0, w * 0.36), (0.25 * L, w * 0.5), (0.72 * L, w * 0.48), (0.9 * L, w * 0.32), (L, 0.0)]
    if a0 > 0:
        half = [(a0 * L, float(np.interp(a0 * L, [p for p, _ in half], [q for _, q in half])) * 1.06)] + \
               [(p, q * 1.06) for p, q in half if p > a0 * L]
    return half + [(p, -q) for p, q in half[::-1][1:-1]] + ([(a0 * L, -half[0][1])] if a0 > 0 else [])


def wings(k, F, thick=0.02):
    """Raised mid-beat: a plate of golden coverts, long dark flight feathers hanging from it, their ends bronze."""
    d, c = a_.wing_axes(F)
    n = np.cross(d, c)
    root = F.SHOULDER - d * 0.04
    k.add("wing", slab(COVERTS, root + n * 0.004, d, c, thick * 1.3), GOLD, "wing{s}", fuse=False, mirror=True)
    face = unit(0.3, -1.0, 0.1)                       # the feathers' faces turned to the front three-quarter
    for i, (a, b, turn, L) in enumerate(PRIMARIES):
        # each hangs from the coverts' trailing edge, down and a little out and back, the outer ones further out
        f = unit(0.06 + 0.035 * i, 0.3, -1.0)
        ez = np.cross(face, f)
        o = root + d * a + c * b * 0.6 + face * (0.004 + 0.004 * i)
        k.add(f"primary{i}", slab(feather(L, 0.085), o, f, ez, thick), BROWN, "wing{s}", fuse=False, mirror=True)
        k.add(f"primarytip{i}", slab(feather(L, 0.085, 0.7), o, f, ez, thick * 1.35), BRONZE, "wing{s}", fuse=False,
              mirror=True, surface="metal")
    k.add("wingroot", ell(F.SHOULDER + np.array([0.0, 0.02, -0.01]), (0.085, 0.09, 0.09)), BROWN, "wing{s}",
          mirror=True)


INK, IRISC, WHITEC, LIGHT = srgb(a_.INKH), srgb(IRIS), np.array([1.0, 1.0, 1.0]), srgb(GOLD)


def eye_open(u, v):
    return a_.eye_open(u, v, iris=IRISC)


def eyes(k, F):
    """Acvilet's eyes in orange on the dark face; the lid dark, its line in the gold, so a closed eye shows."""
    LID = srgb(BROWN)
    E = a_.eye_centre(F)
    m = np.array([-1, 1, 1.0])
    outline = a_.EYEOUT
    def lay(state):
        look = eye_look(state, eye_open, LID, LIGHT, line=0.18)
        k.decal(E, F.EYEDIR, F.EYE, look, eps=0.005, outline=polygon(outline), rings=20, segs=56)
        k.decal(E * m, F.EYEDIR * m, F.EYE, lambda u, v: look(-u, v), eps=0.005,
                outline=polygon([(-a, b) for a, b in outline]), rings=20, segs=56)
    return lay


def acviltor(src, out_path):
    F = frame()
    k = Kit(src, coat="fur")
    a_.body(k, F, colour=BROWN, head=GOLD)
    a_.thighs(k, F)
    wings(k, F)
    colours(k, F)
    cape_feathers(k, F)
    a_.beak(k, F)
    a_.tail(k, F, colour=BROWN, edge=GOLD, pitch=-30, thick=0.022)
    a_.talons(k, F, claw=CLAW)
    ob = k.build(voxel=F.VOXEL, band=0.008)
    a_.board(ob)
    k.eye_states(eyes(k, F))
    k.export(out_path)


if __name__ == "__main__":
    acviltor(*args())
