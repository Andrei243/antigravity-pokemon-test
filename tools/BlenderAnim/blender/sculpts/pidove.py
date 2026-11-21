# Pidove sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pidove.py -- <the export's Pidove.glb> <out.glb>
# A plump little pigeon: a round mid-grey head nearly as wide as the body, with a tuft of three short feathers at the
# back of its crown; a large round eye, a yellow iris in a black rim round a black pupil; a round pink cere over a short
# dark grey bill, open on a pink mouth; a pale grey body with a near-white chest patch whose top is a heart, its two
# lobes rising under the cheeks; folded wings at the sides, pale grey with two black bars across them; a short dark
# fan of tail feathers tilted up; short pink legs and black claws. Written to be grown from: Tranquill and Unfezant
# import it (sibling("pidove")) and keep its frame, its eyes, its bill, its wings and its feet.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, slab, srgb, inside, unit, eye_look, polygon, args, sibling

g = sibling("pidgey")           # its helpers: spike, poly_mask, mirrored, pointed, hooked

# --- the line's colours -------------------------------------------------------------------------------------------
HEAD, BODY, CHEST, WINGC, BAR = "#9A9BA4", "#C6C7CD", "#E8E6E2", "#B9BAC1", "#34353C"
TAILC, BILL, CERE, MOUTH = "#4E5058", "#3A3B42", "#EC98A4", "#E07A8C"
FOOT, CLAW = "#E7A2AD", "#2A2A30"


# --- the frame: every place and size from the skeleton's joints ----------------------------------------------------
def frame():
    F = SimpleNamespace()
    F.H = np.array([0, -0.048, 0.243])              # the head's centre, over its joint (0, -0.04, 0.22)
    F.HR = np.array([0.074, 0.07, 0.07])
    F.BODY = ((0, 0.012, 0.128), (0.1, 0.108, 0.1), (-6, 0, 0))
    F.BREAST = ((0, -0.038, 0.148), (0.088, 0.08, 0.088))
    F.NECK = ((0, -0.03, 0.2), (0.07, 0.064, 0.05))
    # the folded wing, shoulder to tip, fused along the side (the wing's joint is (0.07, 0, 0.19))
    F.WING = ([(0.084, -0.028, 0.188), (0.1, 0.02, 0.166), (0.097, 0.08, 0.128), (0.062, 0.14, 0.1)],
              [0.03, 0.05, 0.044, 0.01], 0.34)
    # the wing's two dark bars, by t = y + 0.45 (z - 0.15) along it, and its dark tip past TIPT
    F.BARS = [(0.018, 0.045), (0.072, 0.098)]
    F.TIPT = None
    F.LEGX, F.LEGTOP, F.ANKLE, F.LEGY = 0.034, 0.06, 0.014, -0.012
    F.LEGR = (0.014, 0.012)
    F.TOE, F.HEEL, F.TOER = 0.03, 0.017, 0.0068
    # the bill: upper (centre, radii, tilt), lower, the mouth between; the cere over its root
    F.BILL = ((0, -0.135, 0.236), (0.016, 0.03, 0.012), 8)
    F.BILLLOW = ((0, -0.128, 0.219), (0.012, 0.022, 0.0075), -14)
    F.MOUTH = ((0, -0.122, 0.227), (0.011, 0.018, 0.0075))
    F.CERE = ((0, -0.111, 0.251), (0.021, 0.015, 0.017))
    F.EYEDIR = unit(0.62, -0.72, 0.2)
    F.EYE = (0.025, 0.025)                           # the eye's half sizes
    F.VOXEL = 0.0035
    F.TAILBASE = np.array([0, 0.1, 0.13])
    # the chest's heart in (x, z) seen from the front, the right half from the top middle round to the bottom
    F.HEART = [(0.0, 0.188), (0.022, 0.21), (0.046, 0.214), (0.069, 0.198), (0.086, 0.165), (0.09, 0.11),
               (0.075, 0.06), (0.04, 0.036), (0.0, 0.03)]
    return F


def eye_centre(F):
    de = F.EYEDIR
    return F.H + de / np.linalg.norm(de / F.HR)


# --- the parts ----------------------------------------------------------------------------------------------------
def body(k, F, body=BODY, head=HEAD):
    c, r, rot = F.BODY
    k.add("body", ell(c, r, rot=rot), body, "body")
    k.add("breast", ell(*F.BREAST), body, "body")
    k.add("neck", ell(*F.NECK), head, "head")
    k.add("head", ell(F.H, F.HR), head, "head")


def chest(k, F, colour=CHEST):
    """The near-white chest, its top a heart: painted in (x, z) as seen from the front, on the front half alone."""
    HEART = g.mirrored(F.HEART)
    k.paint(lambda P: g.poly_mask(P[:, 0], P[:, 2], HEART, edge=0.002) * inside(P[:, 1] + 0.02, 0.01), colour)


def wings_folded(k, F, colour=WINGC, bar=BAR):
    """The folded wing along the side, its two dark bars painted across it (and a dark tip where TIPT says)."""
    pts, radii, f = F.WING
    k.add("wing", blade(pts, radii, (1, 0, 0), f), colour, "wing{s}", mirror=True)
    z0 = pts[0][2] - 0.04
    sc = getattr(F, "SCALLOP", 0.0)              # a scalloped edge to the bars (Unfezant's), its depth
    t = lambda P: P[:, 1] + 0.45 * (P[:, 2] - z0) + sc * np.abs(np.sin(P[:, 2] * math.pi / 0.035))
    for a, b in F.BARS:
        k.paint(lambda P, a=a, b=b: inside(a - t(P), 0.002) * inside(t(P) - b, 0.002), bar, on=["wing"])
    if F.TIPT is not None:
        k.paint(lambda P: inside(F.TIPT - t(P), 0.002), bar, on=["wing"])


def bill(k, F, upper=BILL, lower=BILL, mouth=MOUTH, cere=CERE):
    c, r, tilt = F.BILL
    k.add("bill", ell(c, r, rot=(tilt, 0, 0), deform=g.hooked), upper, "head", fuse=False, surface="shell")
    c, r, tilt = F.BILLLOW
    k.add("billlow", ell(c, r, rot=(tilt, 0, 0), deform=g.pointed), lower, "head", fuse=False, surface="shell")
    if mouth:
        k.add("mouth", ell(*F.MOUTH), mouth, "head", fuse=False, surface="skin")
    if cere:
        k.add("cere", ell(*F.CERE), cere, "head", fuse=False, surface="skin")


def tuft(k, F, colour=HEAD):
    """Three short feathers at the back of the crown, the middle one tallest, leaning back."""
    top = F.H + np.array([0, 0.018, F.HR[2] * 0.93])
    for x, tip, w in [(0.0, (0.0, 0.034, 0.03), 0.028), (0.016, (0.028, 0.03, 0.02), 0.024),
                      (-0.016, (-0.028, 0.03, 0.02), 0.024)]:
        k.add("tuft", g.spike(top + (x * 0.5, -0.012, -0.012), top + tip, w, 0.013, side=(1, 0, 0)), colour, "head",
              fuse=False)


def tail(k, F, feathers, pitch=24, thick=0.01, tip=None, tipshare=0.32, bars=(), barcol=None):
    """
    A fan of flat pointed feathers from the rump, tilted up (pitch) and the outer ones banked, so the camera behind
    sees their upper faces: (yaw in degrees, length, width, colour) each. With tip, a feather is a plate of the tip's
    colour under a shorter, thicker one of its own colour, so its end shows dark on both faces.
    """
    base = F.TAILBASE
    p = math.radians(pitch)
    for i, (yaw, ln, w, col) in enumerate(feathers):
        y = math.radians(yaw)
        d = np.array([math.sin(y) * math.cos(p), math.cos(y) * math.cos(p), math.sin(p)])
        side = np.array([math.cos(y), -math.sin(y), 0])
        r = math.radians(1.6 * yaw)
        side = side * math.cos(r) - np.array([0, 0, 1.0]) * math.sin(r)
        b = base + np.array([math.sin(y) * 0.01, 0, -0.006 * abs(yaw) / 20])
        def outline(length):
            half = [(0.0, w * 0.32), (length * 0.35, w * 0.5), (length * 0.75, w * 0.42), (length, 0.0)]
            return half + [(a, -h) for a, h in half[::-1][1:-1]]
        if tip is None:
            k.add(f"tail{i}", slab(outline(ln), b, d, side, thick), col, "tail", fuse=False)
        else:
            k.add(f"tail{i}", slab(outline(ln), b, d, side, thick), tip, "tail", fuse=False)
            # the feather's own colour up to a square end across it, where the dark tip begins
            cut = ln * (1 - tipshare)
            xs, hs = [0.0, ln * 0.35, ln * 0.75, ln], [w * 0.32, w * 0.5, w * 0.42, 0.0]
            hc = float(np.interp(cut, xs, hs)) * 0.97
            half = [(x, h * 0.97) for x, h in zip(xs, hs) if x < cut] + [(cut, hc)]
            inner = half + [(a, -h) for a, h in half[::-1]]
            k.add(f"tailin{i}", slab(inner, b, d, side, thick * 1.7), col, "tail", fuse=False)
        # pale chevron bars across the feather (Unfezant's): (share of the length, bar's width along it) each
        xs, hs = [0.0, ln * 0.35, ln * 0.75, ln], [w * 0.32, w * 0.5, w * 0.42, 0.0]
        for j, (at, bw) in enumerate(bars):
            s = ln * at
            h = min(float(np.interp(s, xs, hs)), float(np.interp(s + bw, xs, hs))) * 0.86
            ch = bw * 0.7                            # the chevron's point toward the feather's end
            k.add(f"tailbar{i}_{j}", slab([(s, -h), (s + ch, 0.0), (s, h), (s + bw, h), (s + bw + ch, 0.0),
                                            (s + bw, -h)], b, d, side, thick * 1.7), barcol, "tail", fuse=False)


def legs(k, F, foot=FOOT, claw=CLAW):
    """Pidgey's feet on this frame: a short leg, three toes forward and one back, a claw at each."""
    g.legs(k, F, foot=foot, claw=claw)


# --- the eyes -----------------------------------------------------------------------------------------------------
WHITEC, INK, PUP = np.array([1.0, 1.0, 1.0]), srgb("#16161A"), srgb("#121216")
IRIS = srgb("#F2AE28")


def eye_open(u, v, rim=0.8, pupil=0.4, iris=IRIS):
    """A round eye: a black rim, a yellow iris, a round black pupil a little toward the front, a white glint."""
    r = np.hypot(u, v)
    c = np.tile(iris, (len(u), 1))
    c[r > rim] = INK
    c[np.hypot(u + 0.1, v) < pupil] = PUP
    c[np.hypot(u - 0.12, v - 0.3) < 0.15] = WHITEC
    return c


def eyes(k, F, lid=HEAD, open_look=eye_open, cut=None, cutcol=None, outline=None):
    """
    Two round eyes laid as patches, u toward the back on both. cut(u, v) is True where a brow or a mask lies over the
    eye in every state (painted cutcol).
    """
    LID = srgb(lid)
    CUT = srgb(cutcol) if cutcol else LID
    E = eye_centre(F)
    m = np.array([-1, 1, 1.0])
    def lay(state):
        look = eye_look(state, open_look, LID, INK, line=0.17)
        if cut is not None:
            base = look
            def look(u, v, base=base):
                c = base(u, v)
                c[cut(u, v)] = CUT
                return c
        kw = dict(eps=0.004, rings=20, segs=56)
        if outline is not None:
            k.decal(E, F.EYEDIR, F.EYE, look, outline=polygon(outline), **kw)
            k.decal(E * m, F.EYEDIR * m, F.EYE, lambda u, v: look(-u, v),
                    outline=polygon([(-a, b) for a, b in outline]), **kw)
        else:
            k.decal(E, F.EYEDIR, F.EYE, look, **kw)
            k.decal(E * m, F.EYEDIR * m, F.EYE, lambda u, v: look(-u, v), **kw)
    return lay


def pidove(src, out):
    F = frame()
    k = Kit(src, coat="fur")
    body(k, F)
    chest(k, F)
    wings_folded(k, F)
    bill(k, F)
    tuft(k, F)
    tail(k, F, [(0, 0.13, 0.058, TAILC), (32, 0.115, 0.046, TAILC), (-32, 0.115, 0.046, TAILC)], pitch=36, thick=0.011)
    legs(k, F)
    k.build(voxel=F.VOXEL, band=0.006)
    k.eye_states(eyes(k, F))
    k.export(out)


if __name__ == "__main__":
    pidove(*args())
