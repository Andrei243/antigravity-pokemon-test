# Talonflame sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/talonflame.py -- <the export's Talonflame.glb> <out.glb>
# Fletchinder grown (its crest, its yellow-edged mask, its way of making the eyes and its tail of chevrons, imported
# from fletchinder.py and fletchling.py): from the references, a falcon in the air; broad wings raised in a V as a
# mirror pair, red-orange over their inner half with black flame marks, black flight feathers fingered at their tips;
# a red-orange head with its crest swept back from the crown, a small fierce pale eye in a dark band edged with
# yellow, a dark hooked beak and a dark violet patch under it; a red-orange back with a jagged grey patch between the
# wings; a grey belly streaked with red; a fan of black tail feathers, the middle three barred with yellow chevrons;
# grey thighs barred with red; yellow legs and feet hanging forward and gripping, with black talons.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, tri, unit, eye_look, polygon, args, sibling

fi = sibling("fletchinder")
f_ = fi.f_
g = fi.g
RED, GREY, BLACK, YELLOW, BEAK = "#E44A26", "#9C9CA8", "#26262C", "#F4C21E", "#3E3E48"
VIOLET, MASK, PALEEYE = "#5E4E72", "#24222A", "#F6EDB4"


def frame():
    F = SimpleNamespace()
    F.H = np.array([0, -0.102, 0.296])             # the head's centre, before its joint (0, -0.07, 0.287)
    F.HR = np.array([0.055, 0.063, 0.053])
    F.NECK = ((0, -0.055, 0.282), (0.06, 0.062, 0.056))
    F.BODY = ((0, 0.025, 0.24), (0.072, 0.125, 0.064), (4, 0, 0))
    F.CHEST = ((0, -0.045, 0.247), (0.065, 0.064, 0.07))
    F.SH = np.array([0.058, -0.02, 0.284])           # the wings' joint (0.06, -0.02, 0.277)
    F.EYEDIR = unit(0.62, -0.7, 0.26)
    F.EYE = (0.016, 0.012)
    F.MASKDIR = unit(0.7, -0.62, 0.26)
    F.MASK = (0.032, 0.022)
    F.TAILBASE = np.array([0, 0.135, 0.248])
    F.VOXEL = 0.0045
    return F


on_head = f_.on_head


def body(k, F):
    k.add("neck", ell(*F.NECK), RED, "head")
    c, r, rot = F.BODY
    k.add("body", ell(c, r, rot=rot), RED, "body")
    k.add("chest", ell(*F.CHEST), RED, "body")
    k.add("head", ell(F.H, F.HR), RED, "head")
    for s, b in ((1, "wingR"), (-1, "wingL")):
        k.add("shoulder", ell(F.SH * np.array([s, 1, 1]) + (-0.01 * s, 0.03, -0.012), (0.04, 0.055, 0.036)), RED, b)


def belly(k, F):
    """Grey under the body, rising a little at the front; red flame streaks across it; a jagged grey patch on the back."""
    grey = lambda P: inside(P[:, 2] - (0.226 + 0.3 * np.clip(-P[:, 1] - 0.02, 0, 0.06)
                                       + 0.006 * tri(P[:, 0] / 0.03)), 0.002)
    k.paint(grey, GREY, on=["body", "chest"])
    def streaks(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        t = (z + 0.45 * np.abs(x) + 0.15 * y) / 0.036
        band = inside(np.abs(t - np.floor(t) - 0.5) - 0.14, 0.003)
        return band * grey(P) * inside(np.abs(x) - 0.05, 0.004) * inside(y - 0.07, 0.004) * inside(0.2 - z, 0.004)
    k.paint(streaks, RED)
    def patch(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        return (inside(np.abs(x) - (0.05 + 0.012 * tri(y / 0.024)), 0.002) * inside(np.abs(y - 0.01) - 0.045, 0.003)
                * inside(0.27 - z, 0.003))
    k.paint(patch, GREY, on=["body", "shoulder"])


def head_marks(k, F):
    """The dark violet patch under the beak: in every state."""
    d = unit(0, -0.55, -0.83)
    k.decal(on_head(F, d), d, (0.022, 0.016), VIOLET, eps=0.004, rings=10, segs=40, up=(0, -1, 0))


def beak(k, F):
    k.add("beak", ell((0, -0.168, 0.29), (0.019, 0.038, 0.02), rot=(12, 0, 0), deform=g.hooked), BEAK, "head",
          fuse=False, surface="shell")
    k.add("beaklow", ell((0, -0.158, 0.272), (0.014, 0.026, 0.008), rot=(-10, 0, 0), deform=g.pointed), BEAK, "head",
          fuse=False, surface="shell")


# --- the wings, the design's largest mass -----------------------------------------------------------------------------
ELEV, SWEEP = 34, 16
LEAD = [(0.0, -0.028), (0.12, -0.038), (0.25, -0.033), (0.34, -0.014), (0.395, 0.018)]
FINGERS = [(0.39, 0.058), (0.355, 0.098), (0.31, 0.128), (0.255, 0.148), (0.195, 0.157), (0.13, 0.152),
           (0.065, 0.138)]
REDPART = [(0.0, -0.022), (0.12, -0.032), (0.25, -0.027), (0.3, -0.018), (0.295, 0.004), (0.262, 0.02),
           (0.245, 0.008), (0.21, 0.05), (0.185, 0.032), (0.145, 0.078), (0.118, 0.056), (0.075, 0.098),
           (0.048, 0.078), (0.012, 0.104), (0.0, 0.1)]
FLAMES = [[(0.05, 0.028), (0.11, 0.004), (0.17, -0.004), (0.13, 0.016), (0.165, 0.026), (0.1, 0.04)],
          [(0.16, 0.018), (0.215, -0.008), (0.255, -0.012), (0.23, 0.006), (0.25, 0.012), (0.2, 0.026)]]


def wing_axes():
    e, s = math.radians(ELEV), math.radians(SWEEP)
    d = np.array([math.cos(e) * math.cos(s), math.cos(e) * math.sin(s), math.sin(e)])
    c0 = np.array([0.3, 0.35, -0.9])
    c = c0 - d * (d @ c0); c /= np.linalg.norm(c)
    return d, c


def wing_outline():
    out_ = list(LEAD)
    for i, (a, b) in enumerate(FINGERS):
        out_.append((a, b))
        nxt = FINGERS[i + 1] if i + 1 < len(FINGERS) else (0.0, 0.118)
        ma, mb = (a + nxt[0]) / 2, (b + nxt[1]) / 2
        q = 0.4 if ma > 0.2 else 0.18         # deep notches between the primaries, shallow ones inside
        out_.append((ma + q * (0.15 - ma), mb + q * (0.02 - mb)))
    out_.append((0.0, 0.118))
    return out_


def wings(k, F):
    """Stacked plates a wing: the black of the whole wing, the red over its inner half thicker than it, the black
    flames on the red thicker still, so each shows on both faces."""
    d, c = wing_axes()
    root = F.SH - d * 0.025
    t = 0.012
    k.add("wing", slab(wing_outline(), root, d, c, t), BLACK, "wing{s}", fuse=False, mirror=True)
    k.add("wingred", slab(REDPART, root, d, c, t * 1.5), RED, "wing{s}", fuse=False, mirror=True)
    for j, fl in enumerate(FLAMES):
        k.add(f"wingflame{j}", slab(fl, root, d, c, t * 2.0), BLACK, "wing{s}", fuse=False, mirror=True)


# --- the legs, hanging forward and gripping -----------------------------------------------------------------------------
def legs(k, F):
    for s, side in ((1, "R"), (-1, "L")):
        x = 0.046 * s
        k.add(f"thigh{side}", ell((x, -0.058, 0.205), (0.034, 0.042, 0.046), rot=(-25, 0, 0)), GREY, f"leg{side}")
        top = np.array([x, -0.078, 0.18])
        ank = np.array([x * 1.04, -0.142, 0.138])
        k.add(f"shin{side}", tube([top, (top + ank) / 2, ank], [0.016, 0.015, 0.0135]), YELLOW, f"leg{side}",
              fuse=False, surface="scales")
        for j, ang in enumerate((-30, 0, 30, 180)):
            rr = math.radians(ang)
            dd = np.array([math.sin(rr) * s, -math.cos(rr), 0])
            ln = 0.05 if ang != 180 else 0.03
            mid = ank + dd * ln * 0.55 + np.array([0, 0, 0.002])
            tip = ank + dd * ln + np.array([0, 0, -0.024])
            k.add(f"toe{side}", tube([ank, mid, tip], [0.0105, 0.0095, 0.0085]), YELLOW, f"leg{side}", fuse=False,
                  surface="scales")
            cd = unit(*(dd * 0.35 + np.array([0, 0, -1.0])))
            k.add(f"claw{side}", g.spike(tip, tip + cd * 0.024 - dd * 0.003, 0.014, 0.012), BLACK, f"leg{side}",
                  fuse=False, surface="shell")
    # red bars across the grey thighs
    for z0 in (0.186, 0.214):
        k.paint(lambda P, z0=z0: inside(np.abs(P[:, 2] + 0.25 * (P[:, 1] + 0.06) - z0) - 0.0055, 0.0015), RED,
                on=["thigh"])


# --- the eyes: small and pale in the dark band ---------------------------------------------------------------------------
def eye_open(u, v):
    c = np.tile(srgb(PALEEYE), (len(u), 1))
    c[((u + 0.12) / 0.42) ** 2 + ((v + 0.05) / 0.55) ** 2 < 1] = fi.PUP
    return c


def talonflame(src, out_path):
    F = frame()
    k = Kit(src, coat="fur")
    body(k, F)
    belly(k, F)
    legs(k, F)
    fi.crest(k, F, colour=RED, rise=(0.0, 0.085, 0.05), w=0.06, t=0.028)
    beak(k, F)
    wings(k, F)
    f_.tail(k, F, [(0, 0.205, 0.056, BLACK, [(0.3, 0.07), (0.46, 0.07), (0.62, 0.07)], YELLOW),
                   (15, 0.195, 0.054, BLACK, [(0.3, 0.07), (0.46, 0.07), (0.62, 0.07)], YELLOW),
                   (-15, 0.195, 0.054, BLACK, [(0.3, 0.07), (0.46, 0.07), (0.62, 0.07)], YELLOW),
                   (30, 0.17, 0.05, BLACK, [], None),
                   (-30, 0.17, 0.05, BLACK, [], None)], pitch=24, thick=0.012, roll=1.9, lean=0)
    head_marks(k, F)
    fi.mask(k, F, outer=YELLOW, inner=MASK, place=(0.05, -0.04, 0.86, 0.74))
    k.build(voxel=F.VOXEL, band=0.006, wband=0.05)
    k.eye_states(fi.eyes(k, F, lid=MASK, open_look=eye_open))
    k.export(out_path)


if __name__ == "__main__":
    talonflame(*args())
