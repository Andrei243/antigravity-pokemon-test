# Fletchling sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/fletchling.py -- <the export's Fletchling.glb> <out.glb>
# From the references: a little round robin whose great orange-red head is as big as the rest of it, the orange
# running down the front of the chest to a scalloped edge of three small points over the pale grey belly; a long
# straight dark bill; a large round black eye with a white glint and a small yellow triangle at its back edge; a pale
# lavender-grey body; folded pale blue wings, each ending in a small fan of white feathers; two black tail feathers
# and a shorter third raised up and back, the middle one with a white chevron pointing to its root; thin dark grey
# legs, three toes forward and one back. Written to be grown from: Fletchinder imports it (sibling("fletchling")) and
# keeps its frame, its bill, its tail of chevrons, its feet and its way of making the eyes.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, slab, lathe, srgb, inside, tri, unit, eye_look, polygon, args, sibling

g = sibling("pidgey")           # its helpers: spike, poly_mask, mirrored, frame_of, legs

# --- the line's colours -------------------------------------------------------------------------------------------
ORANGE, GREY, WINGC, WHITE = "#EE6232", "#B3BCDA", "#C6D0EE", "#F4F6FB"
BLACK, BILL, LEGC, YELLOW = "#232328", "#38383F", "#3A3A42", "#F4C928"


# --- the frame: every place and size from the skeleton's joints ----------------------------------------------------
def frame():
    F = SimpleNamespace()
    F.H = np.array([0, -0.036, 0.156])             # the head's centre, before its joint (0, -0.03, 0.15)
    F.HR = np.array([0.064, 0.061, 0.061])
    F.BODY = ((0, 0.028, 0.104), (0.048, 0.068, 0.045), (10, 0, 0))
    F.BREAST = ((0, -0.03, 0.1), (0.046, 0.046, 0.042))
    # the folded wing along the side, shoulder to tip (the wing's joint is (0.045, 0, 0.12))
    F.WING = ([(0.05, -0.004, 0.128), (0.058, 0.03, 0.116), (0.054, 0.066, 0.104), (0.036, 0.094, 0.098)],
              [0.02, 0.032, 0.028, 0.008], 0.36)
    F.WINGTIP = np.array([0.04, 0.09, 0.1])         # where the white fan of the wing's end starts
    F.LEGX, F.LEGTOP, F.ANKLE, F.LEGY = 0.024, 0.075, 0.009, 0.006
    F.LEGR = (0.0058, 0.0052)
    F.TOE, F.HEEL, F.TOER = 0.03, 0.016, 0.0042
    # the bill, a cone from inside the face: its root, its direction, its length, its radius at the root
    F.BILL = (np.array([0, -0.082, 0.146]), unit(0, -1, -0.08), 0.066, 0.0125)
    F.EYEDIR = unit(0.6, -0.74, 0.2)
    F.EYE = (0.018, 0.018)
    F.MARKDIR = unit(0.86, -0.36, 0.3)              # the yellow triangle just behind the eye
    F.MARK = (0.011, 0.009)
    F.VOXEL = 0.0027
    F.TAILBASE = np.array([0, 0.078, 0.118])
    F.CHEST = 0.083                                 # the height of the orange chest's edge at the front
    return F


def on_head(F, d):
    """The point of the head's ellipsoid in a direction from its centre."""
    return F.H + d / np.linalg.norm(d / F.HR)


# --- the parts ----------------------------------------------------------------------------------------------------
def body(k, F, head=ORANGE, body=GREY):
    c, r, rot = F.BODY
    k.add("body", ell(c, r, rot=rot), body, "body")
    k.add("breast", ell(*F.BREAST), body, "body")
    k.add("head", ell(F.H, F.HR), head, "head")


def chest(k, F, colour=ORANGE):
    """The orange running down the front of the chest to three small points over the grey."""
    edge = lambda P: F.CHEST + 0.006 * tri(P[:, 0] / 0.026 + 0.5)
    k.paint(lambda P: inside(edge(P) - P[:, 2], 0.0015) * inside(P[:, 1] + 0.022, 0.004), colour)


def wings_folded(k, F, colour=WINGC, tip=WHITE):
    pts, radii, f = F.WING
    k.add("wing", blade(pts, radii, (1, 0, 0), f), colour, "wing{s}", mirror=True)
    # the small white fan at the wing's end, three short feathers pointing back
    for j, (dy, dz, ln) in enumerate(((1.0, 0.25, 0.042), (0.85, -0.1, 0.036), (0.75, 0.6, 0.032))):
        base = F.WINGTIP
        d = unit(0.15, dy, dz)
        k.add(f"wingtip{j}", g.spike(base, base + d * ln, 0.022, 0.008, side=(0.2, 0, 1)), tip, "wing{s}",
              fuse=False, mirror=True)


def bill(k, F, colour=BILL):
    c, d, ln, r = F.BILL
    k.add("bill", lathe(c, d, [(0, -0.008), (r, -0.006), (r, 0.004), (r * 0.78, ln * 0.35), (r * 0.42, ln * 0.75),
                               (r * 0.16, ln * 0.96), (0, ln)], seg=24), colour, "head", fuse=False, surface="shell")


FEATHER = [(0.0, 0.34), (0.22, 0.82), (0.55, 1.0), (0.82, 0.86), (0.95, 0.5), (1.0, 0.0)]     # (along, half width)


def feather_half(ln, w):
    return [(a * ln, h * w / 2) for a, h in FEATHER]


def half_at(ln, w, s):
    xs = [a * ln for a, _ in FEATHER]; hs = [h * w / 2 for _, h in FEATHER]
    return float(np.interp(s, xs, hs))


def tail(k, F, feathers, pitch=46, thick=0.007, roll=1.4, lean=18, base=None, bone="tail"):
    """
    Long feathers from the rump raised up and back (pitch), the outer ones banked (roll x yaw) and all leaned by
    lean degrees about their own length, so the cameras before and behind see their faces: (yaw, length, width,
    colour, chevrons, chevron colour) each, chevrons as (share of the length, width along it), each a V pointing to
    the feather's root, thicker than the feather so it shows on both faces.
    """
    base0 = F.TAILBASE if base is None else base
    p = math.radians(pitch)
    for i, (yaw, ln, w, col, chev, chevcol) in enumerate(feathers):
        y = math.radians(yaw)
        d = np.array([math.sin(y) * math.cos(p), math.cos(y) * math.cos(p), math.sin(p)])
        side = np.array([math.cos(y), -math.sin(y), 0])
        r = math.radians(roll * yaw + lean)
        up = np.cross(side, d)
        side = side * math.cos(r) + up * math.sin(r)
        b = base0 + np.array([math.sin(y) * 0.01, 0, -0.004 * abs(yaw) / 15])
        half = feather_half(ln, w)
        k.add(f"tail{i}", slab(half + [(a, -h) for a, h in half[::-1][1:-1]], b, d, side, thick), col, bone,
              fuse=False)
        for j, (at, bw) in enumerate(chev):
            s, bw = ln * at, ln * bw
            ch = bw * 0.9
            h = min(half_at(ln, w, s + ch), half_at(ln, w, s + ch + bw)) * 0.86
            k.add(f"tailbar{i}_{j}", slab([(s + ch, -h), (s, 0.0), (s + ch, h), (s + ch + bw, h), (s + bw, 0.0),
                                           (s + ch + bw, -h)], b, d, side, thick * 1.8), chevcol, bone, fuse=False)


def legs(k, F, foot=LEGC, claw=BLACK):
    g.legs(k, F, foot=foot, claw=claw)


def mark(k, F, colour=YELLOW):
    """The small yellow triangle at the eye's back edge, pointing back: in every state, so laid before build()."""
    E = on_head(F, F.MARKDIR)
    tri_ = [(-0.75, 0.8), (-0.75, -0.75), (1.0, 0.15)]
    m = np.array([-1, 1, 1.0])
    k.decal(E, F.MARKDIR, F.MARK, colour, eps=0.004, outline=polygon(tri_), rings=10, segs=48)
    k.decal(E * m, F.MARKDIR * m, F.MARK, colour, eps=0.004, outline=polygon([(-a, b) for a, b in tri_]),
            rings=10, segs=48)


# --- the eyes -----------------------------------------------------------------------------------------------------
INK, IRIS, GLINT = srgb("#141418"), srgb("#4A4A54"), np.array([1.0, 1.0, 1.0])


def eye_open(u, v):
    """A big round black eye, a dark grey middle a little toward the front, a white glint at its front top."""
    c = np.tile(INK, (len(u), 1))
    c[np.hypot(u + 0.08, v + 0.06) < 0.5] = IRIS
    c[np.hypot(u + 0.12, v + 0.04) < 0.28] = INK
    c[np.hypot(u + 0.3, v - 0.32) < 0.27] = GLINT
    return c


def eyes(k, F, lid=ORANGE, ink=INK, open_look=eye_open, outline=None, line=0.17):
    """Two patches laid by hand, u toward the back on both (a mirrored patch's outline is not mirrored)."""
    LID = srgb(lid) if isinstance(lid, str) else lid
    E = on_head(F, F.EYEDIR)
    m = np.array([-1, 1, 1.0])
    def lay(state):
        look = eye_look(state, open_look, LID, ink, line=line)
        kw = dict(eps=0.004, rings=20, segs=56)
        if outline is not None:
            k.decal(E, F.EYEDIR, F.EYE, look, outline=polygon(outline), **kw)
            k.decal(E * m, F.EYEDIR * m, F.EYE, lambda u, v: look(-u, v),
                    outline=polygon([(-a, b) for a, b in outline]), **kw)
        else:
            k.decal(E, F.EYEDIR, F.EYE, look, **kw)
            k.decal(E * m, F.EYEDIR * m, F.EYE, lambda u, v: look(-u, v), **kw)
    return lay


def fletchling(src, out_path):
    F = frame()
    k = Kit(src, coat="fur")
    body(k, F)
    chest(k, F)
    wings_folded(k, F)
    bill(k, F)
    tail(k, F, [(0, 0.138, 0.048, BLACK, [(0.52, 0.1)], WHITE),
                (18, 0.122, 0.042, BLACK, [], None),
                (-18, 0.108, 0.04, BLACK, [], None)])
    legs(k, F)
    mark(k, F)
    k.build(voxel=F.VOXEL, band=0.004, wband=0.03)
    k.eye_states(eyes(k, F))
    k.export(out_path)


if __name__ == "__main__":
    fletchling(*args())
