# Ciocbit sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ciocbit.py -- <the export's Ciocbit.glb> <out.glb>
# From its picture (docs/art/reference/gemini/carpathia/10066-Ciocbit.jpg): a small woodpecker chick leaning forward,
# a red cap over the crown rising into a crest of flame-like red feathers swept up and back; a white face and throat,
# a black band from behind the eye round the nape and down the side of the neck; a round dark-brown eye; a long
# straight grey chisel of a beak banded dark; a pair of dark headphones over the cap, a round cup over each ear (the
# Sound type's mark, which its brief doesn't name); on its white breast a round grey speaker with a dark rim and a
# sound wave on it; a black back and wings spotted with grey and barred white at their ends; a long stiff black tail
# it props itself on, back and down; thin grey legs and toes with dark claws.
# Written to be grown from: ciocanor.py imports it (sibling("ciocbit")) and keeps its frame, its colours and its way
# of making the cap, the crest, the headphones, the speaker, the eyes and the feet.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, slab, lathe, srgb, inside, unit, eye_look, polygon, args, sibling

a_ = sibling("acvilet")          # its talons and its board check
g = a_.g                         # Pidgey's helpers: spike, poly_mask, mirrored

# --- the line's colours -------------------------------------------------------------------------------------------
BLACK, WHITE, RED, GREYW = "#2E2F37", "#F2F2EE", "#EC4A1F", "#A9ACB5"
BEAK, BAND, PHONES, CUP, SPEAKER = "#9C9891", "#3B3C43", "#3E4048", "#6C6F78", "#B4B6BC"
LEG, CLAW, IRIS = "#8C8C93", "#33343B", "#4A2E22"


# --- the frame: every place and size from the skeleton's joints ----------------------------------------------------
def frame():
    F = SimpleNamespace()
    F.H = np.array([0, -0.03, 0.42])                # the head's centre, over its joint (0, 0, 0.36)
    F.HR = np.array([0.084, 0.082, 0.08])
    F.BODY = ((0, 0.012, 0.245), (0.098, 0.096, 0.138), (-16, 0, 0))
    F.NECK = ((0, -0.02, 0.35), (0.07, 0.068, 0.06))
    F.BELLY = ((0, -0.012, 0.155), (0.08, 0.08, 0.072))
    F.BACKSPOTS = [(0.0, 0.33), (0.04, 0.3), (-0.04, 0.3), (0.0, 0.26), (0.045, 0.24), (-0.045, 0.24), (0.0, 0.2)]
    F.CAPZ = 0.012                                  # the cap's edge over the head's centre, rising backward
    F.CREST = [((0.0, 0.0), 0.12, 52), ((0.035, 0.02), 0.1, 44), ((-0.035, 0.02), 0.1, 44), ((0.0, 0.045), 0.11, 34),
               ((0.05, 0.05), 0.08, 26), ((-0.05, 0.05), 0.08, 26)]
    F.CRESTW = (0.06, 0.028)
    F.BILL = (np.array([0, -0.09, 0.412]), unit(0, -1, -0.06), 0.15, 0.021)
    F.BANDS = ((0.34, 0.44), (0.6, 0.7))
    F.EYEDIR = unit(0.52, -0.84, 0.08)
    F.EYE = (0.023, 0.023)
    F.PHONE = 0.011                                 # the headphones' band's radius
    F.CUP = (0.034, 0.024)                          # a cup's radius and depth
    F.SPEAK = (np.array([0.025, -0.085, 0.27]), unit(0.28, -1.0, 0.12), 0.048)
    F.SHOULDER = np.array([0.088, 0.0, 0.32])
    F.TAILBASE = np.array([0.0, 0.085, 0.16])
    F.TAIL = (unit(0.32, 0.78, -0.54), 0.2, 0.07)
    F.LEGX, F.LEGY, F.LEGTOP, F.ANKLE = 0.048, 0.0, 0.12, 0.018
    F.LEGR = (0.0135, 0.012)
    F.TOE, F.HEEL, F.TOER, F.CLAWLEN = 0.04, 0.026, 0.0085, 0.022
    F.VOXEL = 0.0046
    F.WS = 1.0                                      # the line's scale: the evolution is this much larger
    F.FRONT = (0.4, 0.3)                            # the white front's top, and where it begins to widen
    return F


def on_head(F, d):
    return F.H + d / np.linalg.norm(d / F.HR)


# --- the parts ----------------------------------------------------------------------------------------------------
def body(k, F, back=BLACK, front=WHITE):
    c, r, rot = F.BODY
    k.add("body", ell(c, r, rot=rot), back, "body")
    k.add("neck", ell(*F.NECK), back, "head")
    k.add("head", ell(F.H, F.HR), front, "head")
    # the belly hangs low, over the tops of the legs
    k.add("belly", ell(*F.BELLY), back, "body")
    # the white breast and belly down the front, widening lower down; the black back and flanks
    top, wide = F.FRONT
    k.paint(lambda P: inside(np.arctan2(np.abs(P[:, 0]), -(P[:, 1] - 0.01 * F.WS))
                             - (0.9 + 0.6 * np.clip((wide - P[:, 2]) / F.WS, 0, 1)), 0.03)
            * inside(P[:, 2] - top, 0.004), front, on=["body", "neck", "belly"])


def cap_line(F, P):
    """The cap's lower edge over the head's centre: lower toward the bill, rising toward the back."""
    dy = P[:, 1] - F.H[1]
    return F.H[2] + F.CAPZ - 0.25 * np.minimum(dy, 0) + 0.45 * np.maximum(dy, 0)


def head_marks(k, F, cap=RED, band=BLACK):
    """The red cap over the crown; the black band from behind the eye round the nape and down the neck's side."""
    H, w = F.H, F.WS
    def nape(P):
        a = np.arctan2(np.abs(P[:, 0]), -(P[:, 1] - H[1]))      # 0 at the front, pi at the back
        z = P[:, 2] - H[2]
        band_ = inside(np.abs(z - w * (-0.012 - 0.06 * (a - 1.0))) - 0.018 * w, 0.003) * inside(1.05 - a, 0.02)
        back = inside(1.5 - a, 0.04)
        return np.maximum(band_, back) * inside(P[:, 2] - cap_line(F, P) - 0.01 * w, 0.003)
    k.paint(nape, band, on=["head", "neck"])
    k.paint(lambda P: inside(cap_line(F, P) - P[:, 2], 0.003) * (P[:, 1] < H[1] + F.HR[1] * 1.3), cap, on=["head"])


def crest(k, F, colour=RED, scale=1.0):
    """Flame-like red feathers swept up and back from the cap."""
    top = F.H + np.array([0, 0.0, F.HR[2] * 0.82])
    w, t = F.CRESTW
    for (x, y), ln, rise in F.CREST:
        base = top + np.array([x * 0.6, y - 0.03 * F.WS, -0.006 * F.WS])
        r = math.radians(rise)
        tip = base + np.array([x * 1.2, math.cos(r), math.sin(r)]) * ln * scale
        k.add("crest", g.spike(base, tip, w * scale, t * scale, side=(1, 0, 0), at=0.3), colour, "head", fuse=False)


def bill(k, F, colour=BEAK, band=BAND, tip=BAND):
    """A long straight chisel of a bill, banded dark, its tip dark."""
    c, d, ln, r = F.BILL
    prof = [(0, -0.006), (r, -0.004), (r, 0.004), (r * 0.82, ln * 0.35), (r * 0.52, ln * 0.7), (r * 0.18, ln * 0.96),
            (0, ln)]
    k.add("bill", lathe(c, d, prof, seg=28), colour, "head", fuse=False, surface="shell")
    rs = lambda s: float(np.interp(s, [a for _, a in prof[2:]], [b for b, _ in prof[2:]]))
    for i, (a0, a1) in enumerate(F.BANDS):
        s0, s1 = ln * a0, ln * a1
        k.add(f"billband{i}", lathe(c, d, [(0, s0), (rs(s0) * 1.07, s0), (rs(s1) * 1.07, s1), (0, s1)], seg=28), band,
              "head", fuse=False, surface="shell")
    s0 = ln * 0.86
    k.add("billtip", lathe(c, d, [(0, s0), (rs(s0) * 1.08, s0), (rs(ln * 0.96) * 1.1, ln * 0.96), (0, ln * 1.008)],
                           seg=28), tip, "head", fuse=False, surface="shell")


def headphones(k, F, band=PHONES, cup=CUP, scale=1.0):
    """A band over the cap from ear to ear and a round cup over each ear: the Sound type's instrument."""
    H, HR = F.H, F.HR
    y = H[1] + 0.012
    R = np.array([HR[0] + F.PHONE * 0.6, HR[2] + F.PHONE * 0.7])
    pts = [(R[0] * math.sin(a), y, H[2] + R[1] * math.cos(a)) for a in np.radians(np.linspace(-88, 88, 9))]
    k.add("phoneband", tube(pts, [F.PHONE * scale] * len(pts), seg=14), band, "head", fuse=False, surface="plastic")
    cr, cd = F.CUP[0] * scale, F.CUP[1] * scale
    c0 = np.array([HR[0] - 0.006, y, H[2] - 0.004])
    prof = [(0, 0.0), (cr * 0.9, 0.0), (cr, cd * 0.25), (cr * 0.98, cd * 0.7), (cr * 0.8, cd), (0, cd * 1.02)]
    k.add("cup", lathe(c0, (1, 0, 0), prof, seg=32), band, "head", fuse=False, mirror=True, surface="plastic")
    k.add("cupface", lathe(c0 + np.array([cd * 0.55, 0, 0]), (1, 0, 0),
                           [(0, 0), (cr * 0.6, 0.0), (cr * 0.6, cd * 0.5), (0, cd * 0.56)], seg=32), cup, "head",
          fuse=False, mirror=True, surface="metal")


def speaker(k, F, rim=PHONES, face=SPEAKER, wave=BAND, scale=1.0, surface="plastic"):
    """A round speaker on the breast: a dark rim, a grey face proud of it, a sound wave of bars across its middle."""
    c, d, r = F.SPEAK
    r = r * scale
    k.add("speaker", lathe(c - d * 0.008, d, [(0, 0), (r, 0.0), (r * 1.02, 0.012 * scale), (r * 0.94, 0.02 * scale),
                                              (0, 0.021 * scale)], seg=40), rim, "body", fuse=False, surface=surface)
    k.add("speakerface", lathe(c - d * 0.008, d, [(0, 0), (r * 0.76, 0.0), (r * 0.76, 0.026 * scale),
                                                  (0, 0.028 * scale)], seg=40), face, "body", fuse=False, surface="metal")
    side = np.cross(d, (0, 0, 1.0)); side /= np.linalg.norm(side)
    up = np.cross(side, d)
    base = c - d * 0.008 + d * 0.028 * scale
    for i, hgt in enumerate((0.35, 0.6, 0.35)):
        x = (i - 1) * 0.36 * r
        hh = hgt * r * 0.95
        P = [base + side * (x + sx * 0.1 * r) + up * sz * hh + d * dz for sx in (-1, 1) for sz in (-1, 1)
             for dz in (0.0, 0.006 * scale)]
        k.add(f"wave{i}", hull(P), wave, "body", fuse=False, surface="plastic")


def wings(k, F, colour=BLACK, spots=GREYW, bars=WHITE):
    """Folded down the sides from the shoulder to past the rump, spotted grey above, barred white at their ends."""
    J, w = F.SHOULDER, F.WS
    pts = [J, J + np.array((0.02, 0.04, -0.07)) * w, J + np.array((0.012, 0.1, -0.15)) * w,
           J + np.array((-0.03, 0.17, -0.22)) * w]
    k.add("wing", blade(pts, list(np.array([0.045, 0.06, 0.05, 0.012]) * w), (1, 0, 0), 0.45), colour, "wing{s}",
          mirror=True)
    def barred(P):
        t = (P[:, 2] + 0.55 * P[:, 1]) / (0.04 * w)
        lo = J[2] - 0.08 * w
        return (inside(np.abs(t - np.round(t)) - 0.17, 0.04) * inside(P[:, 2] - lo, 0.006)
                * (np.abs(P[:, 0]) > J[0] - 0.013 * w))
    k.paint(barred, bars, on=["wing"])
    for (dy, dz) in ((0.02, -0.03), (0.05, -0.045), (0.0, -0.06), (0.035, -0.075), (0.07, -0.07)):
        p = J + np.array([0.06, dy, dz]) * w
        k.decal(p, (1, 0.1, 0.05), (0.012 * w, 0.012 * w), spots, eps=0.003, rings=6, segs=24, mirror=True)
    # the back between the wings, spotted too
    for x, z in F.BACKSPOTS:
        k.decal((x, 0.09 * w, z), unit(x / w * 6, 1.0, 0.2), (0.012 * w, 0.012 * w), spots, eps=0.003, rings=6,
                segs=24)


def tail(k, F, colour=BLACK, thick=0.012, tip=None, tipcol=None):
    """One long stiff tail back, down and out to +X, rolled so the camera behind sees its face."""
    d, ln, w = F.TAIL
    side = np.cross((0, 0, 1.0), d); side /= np.linalg.norm(side)
    up = np.cross(d, side)
    r = math.radians(8)
    ez = side * math.cos(r) + up * math.sin(r)
    half = [(0.0, w * 0.34), (ln * 0.3, w * 0.5), (ln * 0.75, w * 0.46), (ln * 0.92, w * 0.3), (ln, 0.0)]
    k.add("tail", slab(half + [(a, -h) for a, h in half[::-1][1:-1]], F.TAILBASE, d, ez, thick), colour, "tail",
          fuse=False)
    if tip:
        a0 = tip * ln
        h0 = float(np.interp(a0, [a for a, _ in half], [h for _, h in half])) * 1.08
        th = [(a0, h0)] + [(a, h * 1.08) for a, h in half if a > a0]
        k.add("tailtip", slab(th + [(a, -h) for a, h in th[::-1][1:-1]] + [(a0, -h0)], F.TAILBASE, d, ez,
                              thick * 1.5), tipcol, "tail", fuse=False, surface="metal")


# --- the eyes: round, dark brown, under the cap's edge ------------------------------------------------------------
INK, IRISC, WHITEC = srgb("#141418"), srgb(IRIS), np.array([1.0, 1.0, 1.0])


def eye_open(u, v):
    r = np.hypot(u, v)
    c = np.tile(IRISC, (len(u), 1))
    c[r > 0.8] = INK
    c[np.hypot(u + 0.05, v) < 0.44] = INK
    c[np.hypot(u + 0.3, v - 0.3) < 0.2] = WHITEC
    return c


def eyes(k, F, lid=WHITE, open_look=eye_open, outline=None, line=0.18, cut=None, cutcol=None):
    """Two patches laid by hand, u toward the back on both. cut(u, v) is True where a brow lies over the eye always."""
    LID = srgb(lid) if isinstance(lid, str) else lid
    CUT = srgb(cutcol) if isinstance(cutcol, str) else (cutcol if cutcol is not None else LID)
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
            kw["outline"] = polygon(outline)
            k.decal(E, F.EYEDIR, F.EYE, look, **kw)
            kw["outline"] = polygon([(-a, b) for a, b in outline])
        else:
            k.decal(E, F.EYEDIR, F.EYE, look, **kw)
        k.decal(E * m, F.EYEDIR * m, F.EYE, lambda u, v: look(-u, v), **kw)
    return lay


def ciocbit(src, out_path):
    F = frame()
    k = Kit(src, coat="fur")
    body(k, F)
    head_marks(k, F)
    wings(k, F)
    crest(k, F)
    bill(k, F)
    headphones(k, F)
    speaker(k, F)
    tail(k, F)
    a_.talons(k, F, foot=LEG, claw=CLAW)
    ob = k.build(voxel=F.VOXEL, band=0.005, wband=0.04)
    a_.board(ob)
    k.eye_states(eyes(k, F))
    k.export(out_path)


if __name__ == "__main__":
    ciocbit(*args())
