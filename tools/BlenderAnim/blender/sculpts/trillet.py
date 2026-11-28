# Trillet (Carpathia No. 10010) sculpted from nothing on the skeleton and clips of the game's own export (sculptkit.py):
#   blender --background --factory-startup --python sculpts/trillet.py -- <the export's Trillet.glb> <out.glb>
# From its picture (docs/art/reference/gemini/carpathia/10010-Trillet.jpg): a tiny plain brown songbird round as a
# ball, its head one mass with the body; a cream breast speckled with dark brown dashes over a pinkish-tan belly; a
# big round dark eye with a white glint and a faint pale ring; a small grey beak open mid-song, pink inside; folded
# brown wings whose darker primaries point back past the rump; a short tail of four narrow feathers cocked up behind;
# thin dark legs, three toes forward and one back. The music curls of light the picture draws are an effect, not a
# part, and are left to the battle.
#
# Written to be grown from: nightrill.py and privigale.py import it (sibling("trillet")) and keep its colours' way,
# its eyes, its open beak, its feathers with their bands, its speckles and its feet.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, slab, srgb, inside, unit, eye_look, polygon, args, sibling

g = sibling("pidgey")            # spike, frame_of, legs

# --- the line's colours ---------------------------------------------------------------------------------------------
BROWN, DARK, CREAM, BELLY = "#B8854F", "#7E5532", "#F4E6BE", "#CFA88E"
SPECK, BEAK, MOUTH, LEG, CLAW = "#6A4426", "#8E8682", "#E08C92", "#4A3A36", "#2A2224"
TAIL_A, TAIL_B = "#9A673A", "#AE7A47"


# --- helpers shared by the line ----------------------------------------------------------------------------------------
def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r) in direction d from its centre."""
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    return c + d / np.linalg.norm(d / r)


def emask(c, r, edge=0.05):
    """1 inside an ellipsoid (in its own units), eased over edge."""
    c, r = np.array(c, float), np.array(r, float)
    return lambda P: inside(np.linalg.norm((P - c) / r, axis=1) - 1, edge)


def front_y(x, z, ells):
    """Where a ray from the front along +Y first meets one of the ellipsoids (c, r): the union's front."""
    best = 9.0
    for c, r in ells:
        q = 1 - ((x - c[0]) / r[0]) ** 2 - ((z - c[2]) / r[2]) ** 2
        if q > 0:
            best = min(best, c[1] - r[1] * math.sqrt(q))
    return best


def speckles(k, spots, ells, size, colour=SPECK, on=None):
    """Dark dashes on the breast, painted as small upright ellipsoids where the front of the body is."""
    for x, z in spots:
        y = front_y(x, z, ells)
        if y > 8:
            continue
        k.paint(emask((x, y + 0.001, z), (size, size * 1.6, size * 1.35), 0.15), colour, on=on)


FEATHER = [(0.0, 0.34), (0.22, 0.82), (0.55, 1.0), (0.82, 0.86), (0.95, 0.5), (1.0, 0.0)]     # (along, half width)


def _half(ln, w):
    return [(a * ln, h * w / 2) for a, h in FEATHER]


def _half_at(ln, w, s):
    xs = [a * ln for a, _ in FEATHER]; hs = [h * w / 2 for _, h in FEATHER]
    return float(np.interp(s, xs, hs))


def feather(k, name, base, d, side, ln, w, colour, bone, thick, bands=(), mirror=False):
    """
    One flat feather from base along d, its width across side: a hard-edged plate, with bands across it (each
    (share of the length, width as a share, colour)), V's pointing to the root, thicker than the feather so they show
    on both of its faces.
    """
    half = _half(ln, w)
    k.add(name, slab(half + [(a, -h) for a, h in half[::-1][1:-1]], base, d, side, thick), colour, bone,
          fuse=False, mirror=mirror)
    for j, (at, bw, bc) in enumerate(bands):
        s, bw = ln * at, ln * bw
        ch = bw * 0.7
        h = min(_half_at(ln, w, s + ch), _half_at(ln, w, s + ch + bw)) * 0.9
        k.add(f"{name}b{j}", slab([(s + ch, -h), (s, 0.0), (s + ch, h), (s + ch + bw, h), (s + bw, 0.0),
                                   (s + ch + bw, -h)], base, d, side, thick * 1.8), bc, bone, fuse=False, mirror=mirror)


def fan(k, name, base, feathers, pitch, roll=1.2, lean=16, thick=0.006, bone="tail"):
    """
    Feathers out of the rump raised up and back by pitch, spread by yaw, the outer ones banked and all leaned about
    their own length so the cameras before and behind see their faces: (yaw, length, width, colour, bands) each.
    """
    p = math.radians(pitch)
    for i, (yaw, ln, w, col, bands) in enumerate(feathers):
        y = math.radians(yaw)
        d = np.array([math.sin(y) * math.cos(p), math.cos(y) * math.cos(p), math.sin(p)])
        side = np.array([math.cos(y), -math.sin(y), 0])
        r = math.radians(roll * yaw + lean)
        up = np.cross(side, d)
        side = side * math.cos(r) + up * math.sin(r)
        b = np.array(base, float) + np.array([math.sin(y) * ln * 0.06, 0, 0])
        feather(k, f"{name}{i}", b, d, side, ln, w, col, bone, thick, bands)


def beak(k, root, d, up, ln, w, h, gape, colour=BEAK, mouth=MOUTH, bone="head"):
    """
    A songbird's beak open mid-song: an upper and a lower mandible from the face's front, hard-edged and tapering to
    points, held apart by gape (degrees), the pink of the mouth between them.
    """
    d = unit(*d); up = np.array(up, float); up = up - d * (up @ d); up /= np.linalg.norm(up)
    side = np.cross(d, up)
    root = np.array(root, float)
    a = math.radians(gape)
    for name, sgn, length, hh in (("beakup", 1, ln, h), ("beaklow", -1, ln * 0.82, h * 0.8)):
        dd = d * math.cos(a * (0.55 if sgn > 0 else 0.45)) + up * sgn * math.sin(a * (0.55 if sgn > 0 else 0.45))
        uu = np.cross(side, dd) if sgn > 0 else -np.cross(side, dd)
        r0 = root + up * sgn * hh * 0.35
        pts = []
        for t in np.linspace(0, 2 * math.pi, 10, endpoint=False):
            pts.append(r0 + side * math.cos(t) * w / 2 + uu * max(math.sin(t), -0.25) * hh)
        m = r0 + dd * length * 0.5
        for t in np.linspace(0, 2 * math.pi, 8, endpoint=False):
            pts.append(m + side * math.cos(t) * w * 0.28 + uu * max(math.sin(t), -0.2) * hh * 0.5)
        pts.append(r0 + dd * length)
        k.add(name, hull(pts), colour, bone, fuse=False, surface="shell")
    k.add("mouth", ell(root + d * ln * 0.08, (w * 0.32, ln * 0.16, h * 0.45)), mouth, bone, fuse=False)


def legs(k, legx, legy, top, ankle, r, toe, toer, foot=LEG, claw=CLAW):
    F = SimpleNamespace(LEGX=legx, LEGY=legy, LEGTOP=top, ANKLE=ankle, LEGR=r, TOE=toe, HEEL=toe * 0.55, TOER=toer)
    g.legs(k, F, foot=foot, claw=claw)


def eye_ringed(ink, iris, glint, ring=None, ring_at=0.84):
    """A big round dark eye: an iris round a large pupil, a white glint at its front top, a faint pale ring."""
    INK, IRIS, GLINT = srgb(ink), srgb(iris), srgb(glint)
    RING = srgb(ring) if ring else None
    def look(u, v):
        c = np.tile(INK, (len(u), 1))
        rr = np.hypot(u, v)
        c[rr < 0.78] = IRIS
        c[np.hypot(u + 0.04, v + 0.02) < 0.5] = INK
        c[np.hypot(u + 0.3, v - 0.3) < 0.26] = GLINT
        if RING is not None:
            c[rr > ring_at] = RING
        return c
    return look


def eyes(k, H, HR, d, half, open_look, lid, ink, line=0.16, eps=0.004, outline=None):
    """Two patches laid by hand, u toward the back on both (a mirrored patch's outline is not mirrored)."""
    LID = srgb(lid) if isinstance(lid, str) else lid
    INK = srgb(ink) if isinstance(ink, str) else ink
    D = unit(*d)
    E = on_ell(H, HR, D)
    m = np.array([-1, 1, 1.0])
    def lay(state):
        look = eye_look(state, open_look, LID, INK, line=line)
        kw = dict(eps=eps, rings=20, segs=56)
        if outline is not None:
            k.decal(E, D, half, look, outline=polygon(outline), **kw)
            k.decal(E * m, D * m, half, lambda u, v: look(-u, v), outline=polygon([(-a, b) for a, b in outline]), **kw)
        else:
            k.decal(E, D, half, look, **kw)
            k.decal(E * m, D * m, half, lambda u, v: look(-u, v), **kw)
    return lay


# --- Trillet's frame (skeleton: body (0, 0, 0.15), head (0, -0.04, 0.23), wings (+-0.1, -0.02, 0.21),
#     tail (0, 0.1, 0.18), legs (+-0.045, 0, 0.07); the export stands 0.33 tall) ------------------------------------
H = np.array([0, -0.036, 0.243]); HR = np.array([0.09, 0.084, 0.082])
BODY = ((0, 0.012, 0.172), (0.114, 0.12, 0.104))
CHEST = ((0, -0.045, 0.168), (0.1, 0.078, 0.094))
VOXEL = 0.0033


def trillet(src, out_path):
    k = Kit(src, coat="fur")
    k.add("body", ell(*BODY, rot=(8, 0, 0)), BROWN, "body")
    k.add("chest", ell(*CHEST), BROWN, "body")
    k.add("head", ell(H, HR), BROWN, "head")
    # folded wings along the sides, shoulder to a tip past the rump
    k.add("wing", blade([(0.088, 0.0, 0.2), (0.1, 0.05, 0.172), (0.096, 0.1, 0.138), (0.074, 0.145, 0.112)],
                        [0.036, 0.05, 0.04, 0.015], (1, 0, 0), 0.24), BROWN, "wing{s}", mirror=True)
    # darker primaries at the wing's end, pointing back and a little down
    for j, (dz, ln) in enumerate(((0.0, 0.07), (-0.25, 0.062), (0.22, 0.056))):
        base = np.array([0.09, 0.08, 0.15 + dz * 0.04])
        tip = base + unit(-0.18, 1.0, -0.38 + dz) * ln
        k.add(f"primary{j}", g.spike(base, tip, 0.03, 0.011, side=(0.25, 0, 1)), DARK, "wing{s}", fuse=False,
              mirror=True)
    # the cream breast and the pinkish-tan belly under it
    k.paint(lambda P: inside((P[:, 0] / 0.088) ** 2 + ((P[:, 2] - 0.168) / 0.088) ** 2 - 1, 0.06)
            * inside(P[:, 1] + 0.035, 0.006), CREAM, on=["body", "chest", "head"])
    k.paint(lambda P: inside(P[:, 2] - 0.1, 0.008) * inside(P[:, 1] - 0.03, 0.02), BELLY, on=["body", "chest"])
    speckles(k, [(-0.045, 0.2), (-0.015, 0.215), (0.02, 0.205), (0.05, 0.19), (-0.06, 0.165), (-0.03, 0.175),
                 (0.0, 0.18), (0.035, 0.17), (0.065, 0.155), (-0.045, 0.14), (-0.012, 0.148), (0.02, 0.14),
                 (0.05, 0.128), (-0.025, 0.118), (0.008, 0.112), (0.075, 0.185), (-0.075, 0.19)],
             [CHEST, BODY], 0.0044)
    # the wing's lower half a shade darker: the coverts over the flight feathers
    k.paint(lambda P: inside(P[:, 2] - (0.168 - 0.25 * P[:, 1]), 0.004), "#9C6B3F", on=["wing"])
    # a short tail of four narrow feathers cocked up behind
    fan(k, "tail", (0, 0.105, 0.172),
        [(-28, 0.13, 0.05, TAIL_A, ()), (-10, 0.155, 0.054, TAIL_B, ()), (10, 0.155, 0.054, TAIL_A, ()),
         (28, 0.13, 0.05, TAIL_B, ())], pitch=58, roll=0.35, lean=0, thick=0.0065)
    # the beak, open mid-song
    root = on_ell(H, HR, (0, -1, -0.02)) + np.array([0, 0.006, 0])
    beak(k, root, (0, -1, 0.32), (0, 0, 1), 0.058, 0.034, 0.016, 42)
    legs(k, 0.045, 0.0, 0.1, 0.011, (0.0068, 0.006), 0.032, 0.0052)
    k.build(voxel=VOXEL, band=0.005, wband=0.03)
    k.eye_states(eyes(k, H, HR, (0.6, -0.7, 0.34), (0.022, 0.022),
                      eye_ringed("#17110D", "#3B3122", "#FFFFFF", ring="#EADBB4"), BROWN, "#2A1C12"))
    k.export(out_path)


if __name__ == "__main__":
    trillet(*args())
