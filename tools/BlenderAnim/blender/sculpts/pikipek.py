# Pikipek sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pikipek.py -- <the export's Pikipek.glb> <out.glb>
# From the references: a little woodpecker whose round white head is bigger than its body; a red cap from over the
# bill back over the crown, rising into a red crest swept up and back, the back of the head black, the black curling
# down behind the eye to a hook under it; two black points at the back of the crown; a big round eye, a blue iris round
# a black pupil, a white glint; a long grey cone of a bill with a black tip; a small black body with a white bib in a
# star under the throat; wings raised up and back behind it, a black leading feather over large white ones and a
# lavender-grey one under them; one long black tail feather like a spoon; grey legs with long thin toes.
# Written to be grown from: Trumbeak and Toucannon import it (sibling("pikipek")) and keep its frame's names, its
# bill, its wings, its feet, its tail and its way of making the eyes.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, slab, lathe, srgb, inside, unit, eye_look, polygon, args, sibling

g = sibling("pidgey")           # its helpers: spike, poly_mask, mirrored, frame_of, legs

# --- the line's colours -------------------------------------------------------------------------------------------
BLACK, WHITE, RED, GREYF = "#2E2F36", "#F4F4F1", "#E2432C", "#CBC5D6"
BILL, BILLTIP, LEGC, CLAW = "#A2A4AE", "#26262B", "#A4A7B2", "#5A5C66"


# --- the frame: every place and size from the skeleton's joints ----------------------------------------------------
def frame():
    F = SimpleNamespace()
    F.H = np.array([0, -0.036, 0.168])              # the head's centre, over its joint (0, -0.03, 0.16)
    F.HR = np.array([0.06, 0.062, 0.059])
    F.BODY = ((0, 0.018, 0.074), (0.044, 0.054, 0.045), (14, 0, 0))
    F.NECK = ((0, -0.02, 0.116), (0.037, 0.037, 0.03))
    F.LEGX, F.LEGTOP, F.ANKLE, F.LEGY = 0.022, 0.055, 0.012, -0.004
    F.LEGR = (0.0056, 0.0048)
    F.TOE, F.HEEL, F.TOER = 0.036, 0.022, 0.0042
    # the bill, a cone from inside the face: its root, its direction, its length, its radius at the root
    F.BILL = (np.array([0, -0.086, 0.156]), unit(0, -1, -0.16), 0.09, 0.0145)
    F.TIPAT = 0.64                                  # where along the bill its black tip begins
    F.EYEDIR = unit(0.64, -0.72, -0.06)
    F.EYE = (0.0185, 0.0205)
    F.VOXEL = 0.0027
    F.BAND, F.WBAND = 0.004, 0.03
    # the crest: a red blade up and back from the crown (points, radii, its squash across)
    F.CREST = ([(0, -0.03, 0.214), (0, -0.01, 0.243), (0, 0.018, 0.259), (0, 0.048, 0.262), (0, 0.076, 0.25)],
               [0.02, 0.022, 0.018, 0.011, 0.003], 0.5)
    F.TUFTS = [((0.013, 0.036, 0.205), (0.02, 0.07, 0.244)), ((-0.013, 0.036, 0.205), (-0.02, 0.07, 0.244))]
    # the red cap and the black nape in (y, z) about the head's centre, seen from the side
    F.CAP = [(-0.078, -0.003), (-0.05, 0.008), (-0.02, 0.022), (0.0, 0.03), (0.01, 0.046), (0.02, 0.028),
             (0.036, 0.036), (0.06, 0.08), (0.0, 0.12), (-0.1, 0.08)]
    F.NAPE = [(0.2, 0.042), (0.05, 0.042), (0.032, 0.036), (0.034, 0.01), (0.028, -0.014), (0.01, -0.028), (-0.006, -0.032),
              (0.006, -0.043), (0.03, -0.042), (0.045, -0.06), (0.05, -0.2), (0.2, -0.2), (0.2, 0.14)]
    # the white bib on the chest, its right half in (x, z) from the top middle round to the bottom middle
    F.BIB = [(0.0, 0.124), (0.02, 0.116), (0.025, 0.098), (0.036, 0.084), (0.016, 0.08), (0.0, 0.054)]
    F.BIBFRONT = -0.012                             # only where the body's y is below this
    # the wings: shoulder, arm's direction, the direction its last feather goes, feathers (t, length, width, colour)
    F.SHOULDER = np.array([0.038, -0.004, 0.142])
    F.ARM = unit(0.55, 0.45, 0.72)
    F.BACK = unit(0.72, 0.68, 0.0)
    F.FEATHERS = [(0.0, 0.1, 0.03, BLACK), (0.25, 0.11, 0.036, WHITE), (0.5, 0.1, 0.034, WHITE),
                  (0.75, 0.086, 0.03, WHITE), (1.0, 0.062, 0.026, GREYF)]
    F.REACH = 0.03                                  # how far along the arm the first feather starts
    F.FTHICK = 0.008
    # the tail: base, direction, length, width, roll toward the sky (degrees)
    F.TAIL = (np.array([0, 0.06, 0.084]), unit(0.32, 0.94, 0.1), 0.112, 0.042, 35)
    return F


def on_head(F, d):
    """The point of the head's ellipsoid in a direction from its centre."""
    return F.H + d / np.linalg.norm(d / F.HR)


def side_mask(F, P, poly, edge=0.0016):
    """A polygon in (y, z) about the head's centre, painted through the head from side to side."""
    return g.poly_mask(P[:, 1] - F.H[1], P[:, 2] - F.H[2], poly, edge=edge)


# --- the parts ----------------------------------------------------------------------------------------------------
def body(k, F, body=BLACK, head=WHITE, neck=WHITE):
    c, r, rot = F.BODY
    k.add("body", ell(c, r, rot=rot), body, "body")
    k.add("neck", ell(*F.NECK), neck, "head")
    k.add("head", ell(F.H, F.HR), head, "head")


def crest(k, F, colour=RED):
    pts, radii, f = F.CREST
    k.add("crest", blade(pts, radii, (1, 0, 0), f), colour, "head")


def tufts(k, F, colour=BLACK, w=0.02, t=0.012):
    for i, (b, tip) in enumerate(F.TUFTS):
        k.add(f"tuft{i}", g.spike(b, tip, w, t, side=(0, 1, 0)), colour, "head", fuse=False)


def head_marks(k, F, cap=RED, nape=BLACK):
    """The red cap over the face and crown, then the black of the nape over it, curling to a hook under the eye."""
    k.paint(lambda P: side_mask(F, P, F.CAP), cap)
    k.paint(lambda P: side_mask(F, P, F.NAPE), nape)


def bib(k, F, colour=WHITE):
    BIB = g.mirrored(F.BIB)
    k.paint(lambda P: g.poly_mask(P[:, 0], P[:, 2], BIB, edge=0.0016) * inside(P[:, 1] - F.BIBFRONT, 0.004), colour)


def bill(k, F, colour=BILL, tip=BILLTIP, bands=(), prof=None):
    """
    A cone from inside the face, its black tip a cone of its own a hair over it; bands: (from, to as shares of the
    length, colour) each, a ring a hair over the cone.
    """
    c, d, ln, r = F.BILL
    if prof is None:
        prof = [(0, -0.008), (r, -0.006), (r, 0.004), (r * 0.8, ln * 0.35), (r * 0.46, ln * 0.72),
                (r * 0.18, ln * 0.95), (0, ln)]
    k.add("bill", lathe(c, d, prof, seg=28), colour, "head", fuse=False, surface="shell")
    rs = lambda s: float(np.interp(s, [a for _, a in prof[2:]], [b for b, _ in prof[2:]]))
    for i, (a0, a1, col) in enumerate(bands):
        s0, s1 = ln * a0, ln * a1
        k.add(f"billband{i}", lathe(c, d, [(0, s0), (rs(s0) * 1.05, s0), (rs(s1) * 1.05, s1), (0, s1)], seg=28), col, "head", fuse=False, surface="shell")
    if tip:
        s0 = ln * F.TIPAT
        tp = [(0, s0), (rs(s0) * 1.06, s0), (rs(ln * 0.8) * 1.06, ln * 0.8), (rs(ln * 0.95) * 1.08, ln * 0.95),
              (0, ln * 1.006)]
        k.add("billtip", lathe(c, d, tp, seg=28), tip, "head", fuse=False, surface="shell")


ROUND = [(0.0, 0.4), (0.3, 0.85), (0.6, 1.0), (0.85, 0.85), (0.97, 0.45), (1.0, 0.0)]       # (along, half width)


def feather_outline(ln, w, shape=ROUND):
    half = [(a * ln, h * w / 2) for a, h in shape]
    return half + [(a, -h) for a, h in half[::-1][1:-1]]


def wings(k, F, root=BLACK, shape=ROUND):
    """Each wing a fan of flat feathers from its shoulder, raised up and back, the pair a mirror (mirror=True)."""
    J, a, b = F.SHOULDER, F.ARM, F.BACK
    n = np.cross(a, b); n /= np.linalg.norm(n)
    k.add("wingroot", ell(J + a * 0.006, getattr(F, "WROOT", (0.016, 0.02, 0.022))), root, "wing{s}", mirror=True)
    for i, (t, ln, w, col) in enumerate(F.FEATHERS):
        f = a * (1 - t) + b * t; f /= np.linalg.norm(f)
        ez = np.cross(n, f)
        base = J + a * F.REACH * (1 - t) + n * 0.0016 * i
        k.add(f"wingf{i}", slab(feather_outline(ln, w, shape), base, f, ez, F.FTHICK), col, "wing{s}", fuse=False,
              mirror=True)


def tail(k, F, colour=BLACK, thick=0.008):
    """One long feather like a spoon, back and out to +X, rolled so the camera behind sees its face."""
    base, d, ln, w, roll = F.TAIL
    side = np.cross((0, 0, 1.0), d); side /= np.linalg.norm(side)
    up = np.cross(d, side)
    r = math.radians(roll)
    ez = side * math.cos(r) + up * math.sin(r)
    half = [(0.0, w * 0.2), (ln * 0.4, w * 0.28), (ln * 0.7, w * 0.5), (ln * 0.9, w * 0.42), (ln, 0.0)]
    k.add("tail", slab(half + [(a, -h) for a, h in half[::-1][1:-1]], base, d, ez, thick), colour, "tail",
          fuse=False)


def legs(k, F, foot=LEGC, claw=CLAW):
    g.legs(k, F, foot=foot, claw=claw)


# --- the eyes -----------------------------------------------------------------------------------------------------
INK, IRIS, GLINT = srgb("#141418"), srgb("#3AA4E4"), np.array([1.0, 1.0, 1.0])


def eye_open(u, v, iris=IRIS):
    """A big round eye: a dark rim, a blue iris, a big black pupil a little to the front, a white glint at its front."""
    r = np.hypot(u, v)
    c = np.tile(iris, (len(u), 1))
    c[r > 0.83] = INK
    c[np.hypot(u + 0.08, v + 0.02) < 0.5] = INK
    c[np.hypot(u + 0.3, v - 0.3) < 0.21] = GLINT
    return c


def eyes(k, F, lid=WHITE, open_look=eye_open, outline=None, line=0.17, cut=None, cutcol=None):
    """
    Two patches laid by hand, u toward the back on both (a mirrored patch's outline is not mirrored). cut(u, v) is
    True where a brow or a lid lies over the eye in every state (painted cutcol).
    """
    LID = srgb(lid) if isinstance(lid, str) else lid
    CUT = (srgb(cutcol) if isinstance(cutcol, str) else cutcol) if cutcol is not None else LID
    E = on_head(F, F.EYEDIR)
    m = np.array([-1, 1, 1.0])
    def lay(state):
        look = eye_look(state, open_look, LID, INK, line=line)
        if cut is not None:
            base = look
            def look(u, v, base=base):
                c = base(u, v)
                c[cut(u, v)] = CUT
                return c
        kw = dict(eps=0.003, rings=20, segs=56)
        if outline is not None:
            k.decal(E, F.EYEDIR, F.EYE, look, outline=polygon(outline), **kw)
            k.decal(E * m, F.EYEDIR * m, F.EYE, lambda u, v: look(-u, v),
                    outline=polygon([(-a, b) for a, b in outline]), **kw)
        else:
            k.decal(E, F.EYEDIR, F.EYE, look, **kw)
            k.decal(E * m, F.EYEDIR * m, F.EYE, lambda u, v: look(-u, v), **kw)
    return lay


def pikipek(src, out_path):
    F = frame()
    k = Kit(src, coat="fur")
    body(k, F)
    crest(k, F)
    head_marks(k, F)
    bib(k, F)
    tufts(k, F)
    bill(k, F)
    wings(k, F)
    tail(k, F)
    legs(k, F)
    k.build(voxel=F.VOXEL, band=F.BAND, wband=F.WBAND)
    k.eye_states(eyes(k, F))
    k.export(out_path)


if __name__ == "__main__":
    pikipek(*args())
