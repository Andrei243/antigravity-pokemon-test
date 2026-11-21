# Wattrel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/wattrel.py -- <the export's Wattrel.glb> <out.glb>
# From the references: a small seabird standing on two orange legs with webbed feet; a big egg of a head under a
# charcoal hood that runs down the back of the neck; huge round eyes, white with a black rim and a black pupil with a
# teal heart; a small yellow point on the crown; a long orange beak pointing forward and down, the yellow of the throat
# under it running down the front of the neck; a pale blue-grey breast whose lower edge hangs in jagged points over a
# blue-grey belly; folded wings yellow over the shoulder, charcoal behind, their dark primaries reaching over a long
# forked charcoal tail. Written to be grown from: Kilowattrel imports it (sibling("wattrel")) and keeps its colours,
# its eyes, its jagged edge and its way of making beak, tail and feet.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, slab, srgb, inside, tri, unit, eye_look, args, sibling

r = sibling("rookidee")
spike, on_ell = r.spike, r.on_ell

# --- the line's colours -------------------------------------------------------------------------------------------
DARK, YELLOW, BREAST, BELLY = "#34363F", "#F2C23A", "#AEBCDD", "#8D9ABF"
ORANGE, WHITE, PUPIL, TEAL, GLINT = "#F2A04E", "#F6F7F4", "#121418", "#6FD3CB", "#FFFFFF"
LINE = "#B8BCC8"                                      # the shut eye's line on the dark hood


# --- the frame ----------------------------------------------------------------------------------------------------
def frame():
    F = SimpleNamespace()
    F.H = np.array([0, -0.05, 0.245])               # the head's centre (its joint (0, -0.03, 0.23))
    F.HR = np.array([0.062, 0.064, 0.07])
    F.NECK = ((0, -0.028, 0.19), (0.052, 0.054, 0.056))
    F.BODY = ((0, 0.016, 0.15), (0.072, 0.095, 0.067), (-12, 0, 0))
    F.WING = ([(0.05, -0.01, 0.19), (0.06, 0.04, 0.168), (0.056, 0.095, 0.155), (0.04, 0.13, 0.152)],
              [0.026, 0.035, 0.028, 0.012], 0.42)
    F.TIPS = np.array([0.043, 0.112, 0.155])           # where the dark primaries leave the folded wing
    F.LEGX, F.LEGTOP, F.ANKLE, F.LEGY, F.LEGR = 0.03, 0.1, 0.012, 0.004, (0.0098, 0.0086)
    F.FOOT = 0.042                                    # the webbed foot's length forward
    F.BEAK = (np.array([0, -0.106, 0.228]), unit(0, -1, -0.42), 0.072, 0.02)
    F.EYEDIR = unit(0.56, -0.76, 0.33)
    F.EYE = (0.027, 0.027)
    F.TAILBASE = np.array([0, 0.088, 0.158])
    F.VOXEL = 0.0032
    return F


# --- the parts ----------------------------------------------------------------------------------------------------
def body(k, F, colour=DARK):
    c, rr, rot = F.BODY
    k.add("body", ell(c, rr, rot=rot), colour, "body")
    k.add("neck", ell(*F.NECK), colour, "head")
    k.add("head", ell(F.H, F.HR), colour, "head")


def jag(x, spacing, depth):
    """The jagged edge's offset at x: points every `spacing`, `depth` deep."""
    return depth * (tri(x / spacing + 0.5) + 1) / 2


def coat(k, F):
    """The yellow of the throat under the beak, the pale breast with its jagged edge, the belly below it."""
    H = F.H
    # the belly and the breast: the front of the body below the neck
    k.paint(lambda P: inside(P[:, 1] - 0.05, 0.006) * inside(P[:, 2] - 0.2, 0.004)
            * inside(np.abs(P[:, 0]) - 0.075, 0.006), BELLY, on=["body", "neck"])
    k.paint(lambda P: inside(P[:, 1] - (-0.01 + 0.6 * (P[:, 2] - 0.15)), 0.004)
            * inside(0.122 + jag(P[:, 0], 0.03, 0.03) - P[:, 2], 0.0025)
            * inside(P[:, 2] - 0.205, 0.004), BREAST, on=["body", "neck"])
    # the yellow of the throat: under the beak, down the front of the neck, the hood's dark on either side
    k.paint(lambda P: inside(P[:, 1] - (H[1] + 0.0 + 0.5 * (P[:, 2] - 0.2)), 0.003)
            * inside(np.abs(P[:, 0]) - (0.03 + 0.3 * np.clip(0.235 - P[:, 2], 0, 0.1)), 0.003)
            * inside(P[:, 2] - (H[2] - 0.004), 0.003) * inside(0.165 - P[:, 2], 0.003), YELLOW)


def breast_points(k, F, colour=BREAST):
    """The breast's jagged edge stands out a little: loose points hanging over the belly."""
    for j, (x, y, z) in enumerate(((0.0, -0.074, 0.124), (0.03, -0.068, 0.122), (-0.03, -0.068, 0.122),
                                   (0.055, -0.048, 0.124), (-0.055, -0.048, 0.124))):
        b = np.array([x, y, z])
        d = unit(x * 1.5, -0.18, -1)
        k.add(f"point{j}", spike(b + d * -0.012, b + d * 0.02, 0.03, 0.008, side=(1, 0, 0)), colour, "body",
              fuse=False)


def wings_folded(k, F, colour=YELLOW, dark=DARK):
    """The folded wing yellow over the shoulder, its back half charcoal, the primaries dark over the tail."""
    pts, radii, f = F.WING
    k.add("wing", blade(pts, radii, (1, 0, 0), f), colour, "wing{s}", mirror=True)
    # the dark back of the wing: a clean oval line, painted without `on` (only the wings reach out so far)
    k.paint(lambda P: inside(0.058 - np.abs(P[:, 0]), 0.004) * inside(-P[:, 1] + 0.08 + 0.6 * (P[:, 2] - 0.17), 0.003)
            * inside(P[:, 2] - 0.205, 0.004), dark)
    for j, (dy, dz, ln) in enumerate(((1.0, -0.05, 0.075), (0.92, -0.25, 0.062), (0.85, 0.15, 0.055))):
        d = unit(-0.12, dy, dz)
        k.add(f"primary{j}", spike(F.TIPS - d * 0.01, F.TIPS + d * ln, 0.03, 0.011, side=(0.25, 0, 1)), dark,
              "wing{s}", fuse=False, mirror=True)


def beak(k, F, colour=ORANGE, hook=0.0):
    """A long wedge of a beak; hook bends its tip down (Kilowattrel)."""
    c, d, ln, rr = F.BEAK
    a, b = np.cross(d, (0, 0, 1.0)), None
    a /= np.linalg.norm(a)
    b = np.cross(a, d)                     # up across the beak
    pts, rad = [], []
    for t in np.linspace(0, 1, 7):
        p = c + d * ln * t - b * hook * ln * max(t - 0.55, 0) ** 2 * 4.5
        pts.append(p)
        rad.append(rr * (1 - 0.82 * t ** 1.3) + 0.002)
    k.add("beak", blade(pts, rad, b, 0.72), colour, "head", fuse=False, surface="shell")


def tail(k, F, feathers, bone="tail", thick=0.009, base=None):
    """Long pointed feathers from the rump, each (yaw toward +X, pitch, length, width, lean, colour)."""
    b0 = F.TAILBASE if base is None else np.array(base, float)
    for i, (yaw, pitch, ln, w, lean, col) in enumerate(feathers):
        y, p = math.radians(yaw), math.radians(pitch)
        d = np.array([math.sin(y) * math.cos(p), math.cos(y) * math.cos(p), math.sin(p)])
        side = np.array([math.cos(y), -math.sin(y), 0])
        up = np.cross(side, d)
        rl = math.radians(lean)
        side = side * math.cos(rl) + up * math.sin(rl)
        half = [(0.0, w * 0.3), (ln * 0.2, w * 0.5), (ln * 0.55, w * 0.42), (ln * 0.85, w * 0.22), (ln, w * 0.04)]
        outline = half + [(a, -h) for a, h in half[::-1]]
        k.add(f"tail{i}", slab(outline, b0 + np.array([0, 0, -0.002 * i]), d, side, thick), col, bone, fuse=False)


def legs(k, F, colour=ORANGE, thigh=None):
    """Orange legs and flat webbed feet of three toes, the web between them."""
    for s, side in ((1, "R"), (-1, "L")):
        x = F.LEGX * s
        ank = np.array([x * 1.1, F.LEGY - 0.004, F.ANKLE])
        k.add(f"leg{side}", tube([(x, F.LEGY, F.LEGTOP), (x * 1.06, F.LEGY + 0.006, (F.LEGTOP + F.ANKLE) * 0.55), ank],
                                 [F.LEGR[0], F.LEGR[0] * 0.94, F.LEGR[1]]), colour, f"leg{side}", fuse=False,
              surface="scales")
        if thigh is not None:
            k.add(f"thigh{side}", ell((x, F.LEGY, F.LEGTOP + 0.004), (0.022, 0.025, 0.026)), thigh, f"leg{side}")
        ln, t = F.FOOT, 0.0055
        toes = [ank + np.array([math.sin(math.radians(a)) * s, -math.cos(math.radians(a)), 0]) * ln
                for a in (-34, -4, 26)]
        heel = ank + np.array([0, 0.008, 0])
        pts = []
        for p in [heel, ank + np.array([0.008 * s, -0.004, 0]), ank + np.array([-0.008 * s, -0.004, 0])] + toes:
            pts += [p + np.array([0, 0, t * 0.5 - F.ANKLE + 0.003]), p + np.array([0, 0, -t * 0.5 - F.ANKLE + 0.003])]
        pts.append(ank + np.array([0, 0, 0.004]))
        k.add(f"foot{side}", hull(pts), colour, f"leg{side}", fuse=False, surface="scales")


def tuft(k, F, colour=YELLOW):
    """The small yellow point on the crown."""
    top = on_ell(F.H, F.HR, unit(0, -0.25, 1))
    k.add("tuft", spike(top + np.array([0, 0.004, -0.01]), top + np.array([0, -0.012, 0.034]), 0.026, 0.013,
                        side=(1, 0, 0)), colour, "head", fuse=False)


# --- the eyes -----------------------------------------------------------------------------------------------------
def round_eye(white=WHITE, rim=PUPIL, pupil=PUPIL, heart=TEAL, glint=GLINT, at=(-0.08, -0.02), pr=0.42, hr=0.2,
              rim_from=0.82):
    """A round white with a black rim, a black pupil a little toward the beak with a teal heart and a glint."""
    Wc, Rc, Pc, Tc, Gc = srgb(white), srgb(rim), srgb(pupil), srgb(heart), srgb(glint)
    def look(u, v):
        c = np.tile(Wc, (len(u), 1))
        c[np.hypot(u, v) > rim_from] = Rc
        if pr:
            q = np.hypot(u - at[0], v - at[1])
            c[q < pr] = Pc
            if hr:
                c[q < hr] = Tc
            c[np.hypot(u - at[0] + pr * 0.38, v - at[1] - pr * 0.45) < pr * 0.26] = Gc
        return c
    return look


def eyes(k, F, open_look, lid=DARK, ink=LINE, half=None, de=None, E=None, pupil=None):
    """pupil: (radius, (u, v), heart colour or None, heart radius), a patch of its own over the white in the open
    and fierce states, so that its edge is round and not stepped at the eye patch's rings."""
    de = F.EYEDIR if de is None else de
    E = on_ell(F.H, F.HR, de) if E is None else E
    half = F.EYE if half is None else half
    L, K = srgb(lid), srgb(ink)
    right = np.cross((0, 0, 1.0), de); right /= np.linalg.norm(right)
    up = np.cross(de, right)
    def lay(state):
        k.decal(E, de, half, eye_look(state, open_look, L, K, line=0.15), eps=0.0025, rings=20, segs=56,
                mirror=True)
        if pupil and state in ("Open", "Fierce"):
            pr, (pu, pv), heart, hr = pupil
            if state == "Fierce":
                pr, pv = pr * 0.75, pv - pr * 0.3
            c = E + right * pu * half[0] + up * pv * half[1]
            Pc, Tc, Gc = srgb(PUPIL), srgb(heart) if heart else None, srgb(GLINT)
            def look(u, v):
                col = np.tile(Pc, (len(u), 1))
                if Tc is not None:
                    col[np.hypot(u, v) < hr] = Tc
                col[np.hypot(u + 0.36, v - 0.42) < 0.26] = Gc
                return col
            k.decal(c, de, (pr * half[0], pr * half[1]), look, eps=0.0042, rings=12, segs=48, mirror=True)
    return lay


def wattrel(src, out_path):
    F = frame()
    k = Kit(src, coat="fur")
    body(k, F)
    coat(k, F)

    wings_folded(k, F)
    beak(k, F)
    tuft(k, F)
    tail(k, F, [(9, -14, 0.135, 0.034, 32, DARK), (-9, -14, 0.135, 0.034, -32, DARK),
                (0, -22, 0.09, 0.034, 0, DARK)])
    legs(k, F)
    k.build(voxel=F.VOXEL, band=0.004, wband=0.03)
    k.eye_states(eyes(k, F, round_eye(pr=0), pupil=(0.44, (-0.08, -0.03), TEAL, 0.45)))
    k.export(out_path)


if __name__ == "__main__":
    wattrel(*args())
