# Acvilet sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/acvilet.py -- <the export's Acvilet.glb> <out.glb>
# From its picture (docs/art/reference/gemini/carpathia/10063-Acvilet.jpg): a fluffy golden eaglet, a big round head
# for its size with ragged gold down standing up and back over the crown; a white mask: a white brow over each eye
# running back to the side of the head, white cheeks and throat, the gold of the forehead coming down in a point
# between the eyes to a big hooked yellow beak; fierce yellow eyes with black pupils, their tops cut by the white brow
# sloping to the beak; a white bib with a ragged edge on a gold chest; dark-brown feathers coming through over the
# shoulders; wings spread out for balance, gold coverts, a white band, dark-brown flight feathers; a short fan of a
# tail, dark brown under gold; shaggy white thighs; short yellow legs, big yellow talons with three toes forward, one
# back. Written to be grown from: acviltor.py imports it (sibling("acvilet")) and keeps its frame, its way of making
# the mask, the eyes, the beak, the wings and the talons.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, tri, unit, eye_look, polygon, args, sibling

g = sibling("pidgey")            # its helpers: spike, poly_mask, mirrored, hooked, pointed, frame_of
r_ = sibling("rufflet")          # tufts: a deform that raises pointed tufts on a ball of down

# --- the line's colours -------------------------------------------------------------------------------------------
GOLD, GOLDDK, WHITE, BROWN = "#EDB94A", "#D49A34", "#F7F2E6", "#4E3D2E"
BEAK, BEAKLOW, FOOT, CLAW = "#F4C73C", "#E2AE2E", "#EFC44A", "#B88426"
IRIS, INKH = "#F2C230", "#17120E"


# --- the frame: every place and size from the skeleton's joints -----------------------------------------------------
def frame():
    F = SimpleNamespace()
    F.H = np.array([0, -0.045, 0.452])              # the head's centre, over its joint (0, -0.03, 0.4)
    F.HR = np.array([0.112, 0.104, 0.098])
    F.R = 0.105                                     # the head's radius for its round coordinates
    F.BODY = ((0, 0.01, 0.255), (0.128, 0.122, 0.135), (-6, 0, 0))
    F.CHEST = ((0, -0.05, 0.3), (0.1, 0.08, 0.09))
    F.NECK = ((0, -0.03, 0.37), (0.085, 0.08, 0.06))
    F.DOWN = (70, 0.32, 0.09)                       # the head's and body's tufts: how many, how wide, how high
    F.THIGH = ((0.064, -0.035, 0.14), (0.056, 0.058, 0.058))
    F.SHOULDER = np.array([0.118, 0.0, 0.335])
    F.WINGLEN = 1.0                                 # the wing's outline scaled by this
    F.ELEV, F.SWEEP = 42, 26                       # the wing raised and swept back, degrees
    F.TAILBASE = np.array([0, 0.115, 0.215])
    F.TAIL = [(0, 0.13, 0.06), (24, 0.12, 0.055), (-24, 0.12, 0.055)]       # (yaw, length, width)
    F.LEGX, F.LEGY, F.LEGTOP, F.ANKLE = 0.064, -0.04, 0.12, 0.03
    F.LEGR = (0.021, 0.019)
    F.TOE, F.HEEL, F.TOER, F.CLAWLEN = 0.056, 0.034, 0.0135, 0.034
    F.BEAK = ((0, -0.158, 0.43), (0.034, 0.062, 0.034), 16)
    F.BEAKLOW = ((0, -0.128, 0.398), (0.026, 0.034, 0.011), -8)
    F.EYEDIR = unit(0.5, -0.84, 0.1)
    F.EYE = (0.03, 0.025)
    F.VOXEL = 0.0058
    # the white mask in (angle round the head x R, z - the eye's height), its right half from the top middle round
    # to the bottom middle: the gold point between the eyes, the brow over the eye back to the side, the cheek, throat
    F.MASK = [(0.0, -0.034), (0.022, -0.006), (0.036, 0.034), (0.07, 0.044), (0.11, 0.04), (0.15, 0.026),
              (0.19, 0.0), (0.17, -0.04), (0.18, -0.08), (0.14, -0.11), (0.08, -0.13), (0.0, -0.14)]
    # the bib on the chest, right half in (x, z), its lower edge ragged
    F.BIB = [(0.0, 0.37), (0.075, 0.355), (0.085, 0.31), (0.06, 0.285), (0.045, 0.26), (0.025, 0.272), (0.0, 0.25)]
    return F


def eye_centre(F):
    return F.H + F.EYEDIR / np.linalg.norm(F.EYEDIR / F.HR)


def round_head(F, P):
    P = np.atleast_2d(P)
    return np.arctan2(P[:, 0], -(P[:, 1] - F.H[1])) * F.R, P[:, 2]


# --- the parts ----------------------------------------------------------------------------------------------------
def body(k, F, colour=GOLD, head=GOLD):
    n, size, amount = F.DOWN
    c, r, rot = F.BODY
    k.add("body", ell(c, r, rot=rot, deform=r_.tufts(n, size, amount * 0.7, seed=5), sub=5), colour, "body")
    k.add("chest", ell(*F.CHEST), colour, "body")
    k.add("neck", ell(*F.NECK), colour, "head")
    front = lambda D: (D[:, 1] < -0.55) & (D[:, 2] < 0.5)
    k.add("head", ell(F.H, F.HR, deform=r_.tufts(n, size, amount, skip=front, seed=7), sub=5), head, "head")


def thighs(k, F, colour=WHITE):
    """Shaggy white thighs, fused into the belly and hanging to just above the ankles."""
    c, r = F.THIGH
    for s, b in ((1, "legR"), (-1, "legL")):
        k.add("thigh", ell((c[0] * s, c[1], c[2]), r, deform=r_.tufts(30, 0.4, 0.14, seed=11), sub=4), colour, b)


def mask(k, F, colour=WHITE, on=("head", "neck")):
    """The white mask painted round the head's axis, and the white bib on the chest."""
    E = eye_centre(F)
    poly = g.mirrored(F.MASK)
    k.paint(lambda P: g.poly_mask(round_head(F, P)[0], P[:, 2] - E[2], poly, edge=0.003)
            * (P[:, 1] < F.H[1] + 0.04), colour, on=list(on))
    bib = g.mirrored(F.BIB)
    k.paint(lambda P: g.poly_mask(P[:, 0], P[:, 2], bib, edge=0.003) * inside(P[:, 1] + 0.02, 0.006), colour,
            on=["body", "chest", "neck"])


def shoulders(k, F, colour=BROWN):
    """Dark-brown feathers coming through over the shoulders and the top of the back, in a ragged patch."""
    def m(P):
        a = np.arctan2(P[:, 0], P[:, 1] - 0.0)            # round the body's axis from the back
        top = 0.365 - 0.03 * np.abs(a) + 0.01 * tri(a * 3.2)
        low = 0.3 + 0.05 * np.abs(a) + 0.014 * tri(a * 4)        # a ragged V from the shoulders down the back
        return inside(P[:, 2] - top, 0.0025) * inside(low - P[:, 2], 0.0025) * inside(np.abs(a) - 1.15, 0.01)
    k.paint(lambda P: m(P) * inside(-P[:, 1] + 0.02, 0.004), colour)


def crown(k, F, colour=GOLD, n=6, scale=1.0, bone="head"):
    """Ragged down standing up and back over the crown, and two tufts behind the cheeks."""
    top = F.H + np.array([0, 0.01, F.HR[2] * 0.86])
    for j in range(n):
        x = (j - (n - 1) / 2) / max(n - 1, 1) * 0.1
        base = top + np.array([x, -0.03 + 0.02 * abs(x) / 0.05, -0.012 - 0.03 * (abs(x) / 0.05) ** 2])
        tip = base + unit(x * 4.0, 0.75, 0.9 - abs(x) * 3) * (0.075 - 0.2 * abs(x)) * scale
        k.add("crown", g.spike(base, tip, 0.042 * scale, 0.022 * scale, side=(1, 0, -x * 3), at=0.3), colour, bone,
              fuse=False)
    for s in (1, -1):
        base = F.H + np.array([0.08 * s, 0.05, -0.02])
        k.add("cheektuft", g.spike(base, base + unit(0.75 * s, 0.6, -0.15) * 0.06 * scale, 0.04 * scale,
                                   0.02 * scale, side=(0, 0, 1)), colour, bone, fuse=False)


def beak(k, F, upper=BEAK, lower=BEAKLOW):
    c, r, rot = F.BEAK
    k.add("beak", ell(c, r, rot=(rot, 0, 0), deform=g.hooked), upper, "head", fuse=False, surface="shell")
    c, r, rot = F.BEAKLOW
    k.add("beaklow", ell(c, r, rot=(rot, 0, 0), deform=g.pointed), lower, "head", fuse=False, surface="shell")


# --- the wings ------------------------------------------------------------------------------------------------------
# (along the span, across the chord: down and back), the feathers fingered at the trailing edge
LEAD = [(0.0, -0.02), (0.07, -0.03), (0.15, -0.032), (0.215, -0.02), (0.26, 0.005)]
TIPS = [(0.27, 0.075), (0.245, 0.135), (0.2, 0.18), (0.15, 0.195), (0.1, 0.185), (0.055, 0.155)]
BANDS = (0.032, 0.062)          # the white band across the chord, between the gold and the dark


def wing_axes(F):
    e, s = math.radians(F.ELEV), math.radians(F.SWEEP)
    d = np.array([math.cos(e) * math.cos(s), math.cos(e) * math.sin(s), math.sin(e)])
    c0 = np.array([0.12, 0.4, -0.9])
    c = c0 - d * (d @ c0); c /= np.linalg.norm(c)
    return d, c


def wing_outline(F, b0=-1.0, b1=1.0):
    """The wing's outline, cut to the part of the chord between b0 and b1."""
    L = F.WINGLEN
    o = list(LEAD)
    for i, (a, b) in enumerate(TIPS):
        o.append((a, b))
        nxt = TIPS[i + 1] if i + 1 < len(TIPS) else (0.0, 0.11)
        o.append(((a + nxt[0]) / 2 + 0.006, (b + nxt[1]) / 2 - 0.04))
    o.append((0.0, 0.11))
    o = np.array([(a * L, b * L) for a, b in o])
    if b0 > -1 or b1 < 1:
        o[:, 1] = np.clip(o[:, 1], b0 * L, b1 * L)
        # drop points that came together
        keep = [0] + [i for i in range(1, len(o)) if np.linalg.norm(o[i] - o[i - 1]) > 1e-4]
        o = o[keep]
    return [tuple(p) for p in o]


def wings(k, F, base=GOLD, band=WHITE, dark=BROWN, thick=0.012):
    """A plate a wing in gold, a white band and dark flight feathers on it as thicker plates, so both faces show them."""
    d, c = wing_axes(F)
    root = F.SHOULDER - d * 0.02
    k.add("wing", slab(wing_outline(F), root, d, c, thick), base, "wing{s}", fuse=False, mirror=True)
    k.add("wingband", slab(wing_outline(F, BANDS[0], BANDS[1]), root, d, c, thick * 1.5), band, "wing{s}", fuse=False,
          mirror=True)
    k.add("wingdark", slab(wing_outline(F, BANDS[1], 1.0), root, d, c, thick * 1.5), dark, "wing{s}", fuse=False,
          mirror=True)
    # the wing's root, a fused ball of down at the shoulder so the plate grows out of the body
    k.add("wingroot", ell(F.SHOULDER + np.array([0.0, 0.01, 0.0]), (0.05, 0.05, 0.05)), base, "wing{s}", mirror=True)


# --- the tail -------------------------------------------------------------------------------------------------------
def tail(k, F, colour=BROWN, edge=GOLD, pitch=-24, thick=0.016):
    base = F.TAILBASE
    p = math.radians(pitch)
    for i, (yaw, ln, w) in enumerate(F.TAIL):
        y = math.radians(yaw)
        dd = np.array([math.sin(y) * math.cos(p), math.cos(y) * math.cos(p), math.sin(p)])
        side = np.array([math.cos(y), -math.sin(y), 0])
        r = math.radians(1.5 * yaw)
        side = side * math.cos(r) - np.array([0, 0, 1.0]) * math.sin(r)
        b = base + np.array([math.sin(y) * 0.015, 0, -0.01 * abs(yaw) / 20])
        half = [(a * ln, h * w / 2) for a, h in g.FEATHER]
        out_ = half + [(a, -h) for a, h in half[::-1][1:-1]]
        k.add(f"tail{i}", slab(out_, b, dd, side, thick), colour, "tail", fuse=False)
        half = [(a * ln * 0.55, h * w / 2 * 1.08) for a, h in g.FEATHER]
        k.add(f"tailedge{i}", slab(half + [(a, -h) for a, h in half[::-1][1:-1]], b, dd, side, thick * 1.4), edge,
              "tail", fuse=False)


# --- the talons -----------------------------------------------------------------------------------------------------
def talons(k, F, foot=FOOT, claw=CLAW):
    """Short thick shins under the thighs; three toes forward and one back, each with a big curved talon."""
    for s, side in ((1, "R"), (-1, "L")):
        x = F.LEGX * s
        top = np.array([x, F.LEGY, F.LEGTOP])
        ank = np.array([x * 1.06, F.LEGY - 0.01, F.ANKLE])
        k.add(f"shin{side}", tube([top, (top + ank) / 2, ank], [F.LEGR[0], F.LEGR[0] * 0.96, F.LEGR[1]]), foot,
              f"leg{side}", fuse=False, surface="scales")
        for j, ang in enumerate((-34, 0, 34, 180)):
            rr = math.radians(ang + 6 * s)
            dd = np.array([math.sin(rr), -math.cos(rr), 0])
            ln = F.TOE if ang != 180 else F.HEEL
            tip = ank + dd * ln + np.array([0, 0, -F.ANKLE + F.TOER * 0.95])
            k.add(f"toe{side}{j}", tube([ank, (ank + tip) / 2 + np.array([0, 0, 0.004]), tip],
                                        [F.TOER * 1.25, F.TOER * 1.05, F.TOER * 0.9]), foot, f"leg{side}",
                  fuse=False, surface="scales")
            cd = unit(*(dd * 0.55 + np.array([0, 0, -1.0])))
            k.add(f"claw{side}{j}", g.spike(tip - dd * F.TOER * 0.4 + np.array([0, 0, F.TOER * 0.3]),
                                            tip + dd * F.CLAWLEN * 0.55 + cd * F.CLAWLEN * 0.6,
                                            F.TOER * 1.7, F.TOER * 1.5, side=np.cross(dd, (0, 0, 1.0))), claw,
                  f"leg{side}", fuse=False, surface="shell")


# --- the eyes: fierce, yellow, their tops cut by the white brow sloping to the beak -----------------------------------
INK, IRISC, WHITEC = srgb(INKH), srgb(IRIS), np.array([1.0, 1.0, 1.0])
EYEOUT = [(-1.0, -0.25), (-0.75, 0.1), (-0.3, 0.42), (0.2, 0.68), (0.65, 0.8), (0.95, 0.55), (1.0, 0.0), (0.75, -0.55),
          (0.2, -0.85), (-0.45, -0.78), (-0.85, -0.45)]       # u toward the back: the top sloping down to the beak


def eye_open(u, v, iris=None):
    """A yellow eye ringed in ink, a big black pupil a little to the front, a white glint."""
    iris = IRISC if iris is None else iris
    c = np.tile(iris, (len(u), 1))
    rr = np.hypot(u, v * 1.1)
    c[rr > 0.8] = INK
    c[np.hypot(u + 0.12, v + 0.04) < 0.42] = INK
    c[np.hypot(u + 0.32, v - 0.26) < 0.15] = WHITEC
    return c


def eyes(k, F, lid=WHITE, open_look=eye_open, outline=EYEOUT, line=0.18):
    LID = srgb(lid) if isinstance(lid, str) else lid
    E = eye_centre(F)
    m = np.array([-1, 1, 1.0])
    def lay(state):
        look = eye_look(state, open_look, LID, INK, line=line)
        k.decal(E, F.EYEDIR, F.EYE, look, eps=0.004, outline=polygon(outline), rings=20, segs=56)
        k.decal(E * m, F.EYEDIR * m, F.EYE, lambda u, v: look(-u, v), eps=0.004,
                outline=polygon([(-a, b) for a, b in outline]), rings=20, segs=56)
    return lay


def board(ob):
    V = np.array([v.co[:] for v in ob.data.vertices])
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    print("KIT: board: corners %.3f (0.853 w + 0.522 l), shows %.3f, in a square of %.3f"
          % (0.853 * w + 0.522 * l, np.ptp(proj), 1.3 * max(h, 0.85 * max(w, l))))


def acvilet(src, out_path):
    F = frame()
    k = Kit(src, coat="fur")
    body(k, F)
    thighs(k, F)
    mask(k, F)
    shoulders(k, F)
    crown(k, F)
    beak(k, F)
    wings(k, F)
    tail(k, F)
    talons(k, F)
    ob = k.build(voxel=F.VOXEL, band=0.006)
    board(ob)
    k.eye_states(eyes(k, F))
    k.export(out_path)


if __name__ == "__main__":
    acvilet(*args())
