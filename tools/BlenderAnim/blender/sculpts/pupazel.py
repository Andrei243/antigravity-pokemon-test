# Pupazel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pupazel.py -- <the export's Pupazel.glb> <out.glb>
# From its picture (docs/art/reference/gemini/carpathia/10047-Pupazel.jpg) and its brief (CP-dex-1b, No. 47): a
# round hoopoe chick in cinnamon brown, head and body one plump mass with no neck; a pale cream throat and breast
# under the beak ending in a soft scalloped edge on the chest; a fan of six short crest feathers raked back over the
# crown, each cinnamon with a white band and a black tip; a short dark grey beak, slightly curved, held open with the
# pink of the mouth showing; big round dark brown eyes with a white glint in a pale tan ring; folded wings along the
# sides barred black and white behind a cinnamon shoulder; a short black tail with a white band; short grey legs,
# three toes forward and one back.
# Written to be grown from: Pupazoi imports it (sibling("pupazel")) and keeps its palette, its eye, its crest feather,
# its barred feathers, its beak and its feet.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, slab, sheet, outward, srgb, inside, tri, unit, eye_look, polygon, args,
                       sibling)

pg = sibling("pidgey")              # its feet

# --- the line's colours -------------------------------------------------------------------------------------------
CINNAMON, CREAM, BLACK, WHITE = "#C9804A", "#F0C99A", "#2A2628", "#F4F0E8"
BEAK, MOUTH, LEG, CLAW, TAN = "#4A4246", "#D9737A", "#6E676C", "#2E2A2C", "#EBCB9E"


# --- the frame ----------------------------------------------------------------------------------------------------
def frame():
    F = SimpleNamespace()
    F.BODY = ((0, 0.014, 0.183), (0.142, 0.146, 0.136))
    F.H = np.array([0, -0.046, 0.33])              # the head's middle, over its joint (0, -0.05, 0.29)
    F.HR = np.array([0.094, 0.09, 0.09])
    F.EYEDIR = unit(0.6, -0.72, 0.26)
    F.EYE = (0.027, 0.027)
    # the beak: root inside the face, along -Y, curving a little down
    F.BEAK_UP = ([(0, -0.112, 0.338), (0, -0.152, 0.338), (0, -0.186, 0.330), (0, -0.207, 0.317)],
                 [0.019, 0.014, 0.0085, 0.0052])
    F.BEAK_LO = ([(0, -0.112, 0.314), (0, -0.142, 0.303), (0, -0.168, 0.29)], [0.016, 0.011, 0.0058])
    F.MOUTH = ((0, -0.12, 0.326), (0.013, 0.024, 0.011))
    # the crest: roots along the crown from the brow round to the back of the head (angles in the y-z plane from
    # straight up, forward negative), and each feather's length
    F.CREST = [(-42, 0.066), (-28, 0.08), (-14, 0.092), (0, 0.1), (14, 0.102), (28, 0.098), (42, 0.09), (56, 0.078)]
    F.CREST_RAKE = 40                              # degrees each feather leans further back than its root
    F.CREST_W, F.CREST_T = 0.05, 0.013
    # the folded wing laid over the body's side: azimuth from the front round to the back, the upper and lower
    # edge's elevation at the shoulder and at the tip
    F.WING_A = (68, 166)
    F.WING_TOP = (36, -4)
    F.WING_BOT = (-34, -15)
    F.WING_OFF, F.WING_T = 0.001, 0.012
    # cinnamon shoulder, then white bars across the black (shares of the wing's length)
    F.WING_BANDS = [(0.41, 0.49, WHITE), (0.59, 0.67, WHITE), (0.77, 0.84, WHITE)]
    F.TAILBASE = np.array([0, 0.135, 0.15])
    F.TAIL = [(0, 0.11, 0.046), (22, 0.098, 0.042), (-22, 0.098, 0.042)]     # yaw, length, width
    F.LEGX, F.LEGTOP, F.ANKLE, F.LEGY = 0.045, 0.1, 0.014, 0.004
    F.LEGR = (0.0098, 0.0088)
    F.TOE, F.HEEL, F.TOER = 0.04, 0.022, 0.0066
    F.CHEST = 0.175                                # the cream breast's lower edge at the front
    F.VOXEL = 0.0042
    return F


def on_head(F, d):
    return F.H + d / np.linalg.norm(d / F.HR)


# --- feathers -----------------------------------------------------------------------------------------------------
FEATHER = [(0.0, 0.34), (0.25, 0.82), (0.6, 1.0), (0.85, 0.82), (0.96, 0.45), (1.0, 0.0)]     # (along, half width)


def half_at(ln, w, s):
    xs = [a * ln for a, _ in FEATHER]; hs = [h * w / 2 for _, h in FEATHER]
    return float(np.interp(s, xs, hs))


def outline_between(ln, w, a0, a1, n=8, grow=1.0):
    """The feather's outline cut to the stretch a0..a1 of its length (shares), a little wider by grow."""
    s = np.linspace(a0 * ln, a1 * ln, n)
    h = np.array([half_at(ln, w, x) for x in s]) * grow
    h = np.maximum(h, 0.0015)
    return [(x, y) for x, y in zip(s, h)] + [(x, -y) for x, y in zip(s[::-1], h[::-1])]


def feather(k, name, root, d, across, ln, w, thick, colour, bone, bands=(), mirror=False, surface=None):
    """
    A flat feather from root along d, its width along across (its face square to both): one slab of its colour, and
    each band (share from, share to, colour) a thicker slab over that stretch, so it shows on both faces.
    """
    k.add(name, slab(outline_between(ln, w, 0.0, 1.0, 14), root, d, across, thick), colour, bone, fuse=False,
          mirror=mirror, surface=surface)
    for j, (a0, a1, col) in enumerate(bands):
        k.add(f"{name}_b{j}", slab(outline_between(ln, w, a0, a1, 8, 1.04), root, d, across, thick * 1.3), col, bone,
              fuse=False, mirror=mirror, surface=surface)


def crest(k, F, bone="head", tip_white=(0.62, 0.78), tip_black=(0.78, 1.0), spread=0.008, turn=0, splay=6, yaw=-38):
    """
    The fan on the crown: roots round the top of the head in its middle plane, each feather leaning back, its roots
    spread a little apart and the feathers leaning `splay` degrees out to either side by turns; the whole fan then
    turned `yaw` degrees about the head's upright axis, its plane opened toward the front three-quarter camera (+X)
    as a flat tail is, so that camera sees the fan's face and not its edge, and the back camera still sees it.
    """
    t = math.radians(turn)
    y = math.radians(yaw)
    R = np.array([[math.cos(y), -math.sin(y), 0], [math.sin(y), math.cos(y), 0], [0, 0, 1.0]])
    for i, (ang, ln) in enumerate(F.CREST):
        a = math.radians(ang)
        side = (i % 2) * 2 - 1
        r = unit(0, math.sin(a), math.cos(a))
        root = on_head(F, r) - r * 0.012 + np.array([spread * side * 0.5, 0, 0])
        b = math.radians(ang + F.CREST_RAKE)
        sp = math.radians(splay) * side
        d = unit(math.sin(sp), math.sin(b) * math.cos(sp), math.cos(b) * math.cos(sp))
        across = np.cross(np.array([1.0, 0, 0]), d)                 # in the middle plane, square to d
        across = across * math.cos(t) + np.array([1.0, 0, 0]) * math.sin(t)
        root = F.H + R @ (root - F.H); d = R @ d; across = R @ across
        feather(k, f"crest{i}", root, d, across, ln, F.CREST_W, F.CREST_T, CINNAMON, bone,
                bands=[(tip_white[0], tip_white[1], WHITE), (tip_black[0], tip_black[1], BLACK)])


def tail(k, F, bone="tail", pitch=-14, lean=34, roll=1.2, band=(0.42, 0.6), thick=0.012):
    """A short fan from the rump pointing back and a little down, its face leaned toward +X (both cameras see it)."""
    p = math.radians(pitch)
    for i, (yaw, ln, w) in enumerate(F.TAIL):
        y = math.radians(yaw)
        d = np.array([math.sin(y) * math.cos(p), math.cos(y) * math.cos(p), math.sin(p)])
        side = np.array([math.cos(y), -math.sin(y), 0.0])
        up = np.cross(side, d)
        r = math.radians(roll * yaw + lean)
        side = side * math.cos(r) + up * math.sin(r)
        b = F.TAILBASE + np.array([math.sin(y) * 0.012, 0, -0.003 * abs(yaw) / 20])
        feather(k, f"tail{i}", b, d, side, ln, w, thick, BLACK, bone, bands=[(band[0], band[1], WHITE)])


# --- the folded wing ----------------------------------------------------------------------------------------------
def body_dir(a, el):
    return np.stack([np.sin(a) * np.cos(el), -np.cos(a) * np.cos(el), np.sin(el)], axis=-1)


def wing_sheet(F, u0, u1, off, thick, wave=0.0):
    c, r = F.BODY
    c, r = np.array(c, float), np.array(r, float)
    A0, A1 = [math.radians(x) for x in F.WING_A]
    T0, T1 = [math.radians(x) for x in F.WING_TOP]
    B0, B1 = [math.radians(x) for x in F.WING_BOT]

    def at(u, v):
        s = u0 + (u1 - u0) * u + wave * np.sin(3 * math.pi * v)
        a = A0 + (A1 - A0) * s
        top = T0 + (T1 - T0) * s
        bot = B0 + (B1 - B0) * s
        el = bot + (top - bot) * (0.015 + 0.97 * v)
        return c + body_dir(a, el) * (r + off)
    return outward(sheet(at, 22, 12, thick))


def wings_folded(k, F):
    k.add("wing", wing_sheet(F, 0.3, 1.0, F.WING_OFF + F.WING_T / 2, F.WING_T), BLACK, "wing{s}", fuse=False,
          mirror=True)
    for j, (u0, u1, col) in enumerate(F.WING_BANDS):
        k.add(f"wingband{j}", wing_sheet(F, u0, u1, F.WING_OFF + F.WING_T + 0.002, 0.008, wave=0.025), col,
              "wing{s}", fuse=False, mirror=True)


# --- the body -----------------------------------------------------------------------------------------------------
def body(k, F):
    c, r = F.BODY
    k.add("body", ell(c, r), CINNAMON, "body")
    k.add("head", ell(F.H, F.HR), CINNAMON, "head")
    # the cheeks filling the hollow between head and breast
    k.add("cheek", ell((0, -0.07, 0.27), (0.09, 0.07, 0.06)), CINNAMON, "head")
    k.add("nape", ell((0, -0.01, 0.27), (0.118, 0.11, 0.08)), CINNAMON, "body")


def breast(k, F):
    """Cream from under the beak down the front to a scalloped edge on the chest."""
    def mask(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        az = np.arctan2(np.abs(x), -(y - 0.0))
        edge = F.CHEST + 0.012 * tri(x / 0.034 + 0.5)
        side = 0.62 + 0.18 * np.clip((z - 0.2) / 0.1, 0, 1)
        return inside(edge - z, 0.003) * inside(az - side, 0.04) * inside(z - 0.318 + 6 * x * x, 0.004) * inside(y + 0.02, 0.01)
    k.paint(mask, CREAM)


def beak(k, F, up=None, lo=None, mouth=None):
    up = up or F.BEAK_UP; lo = lo or F.BEAK_LO; mouth = mouth or F.MOUTH
    k.add("beakup", outward(tube(*up, seg=20)), BEAK, "head", fuse=False, surface="shell")
    k.add("beaklo", outward(tube(*lo, seg=20)), BEAK, "head", fuse=False, surface="shell")
    k.add("mouth", ell(*mouth, sub=3), MOUTH, "head", fuse=False)


def legs(k, F):
    pg.legs(k, F, foot=LEG, claw=CLAW)


# --- the eyes -----------------------------------------------------------------------------------------------------
INK, IRIS, PUPIL, GLINT = srgb("#1E1612"), srgb("#5C3622"), srgb("#140E0C"), np.array([1.0, 1.0, 1.0])


def eye_open(u, v):
    """A big round dark brown eye, a black middle, a white glint high toward the front (u toward the back)."""
    c = np.tile(IRIS, (len(u), 1))
    c[np.hypot(u + 0.04, v + 0.02) < 0.44] = PUPIL
    c[np.hypot(u + 0.26, v - 0.28) < 0.24] = GLINT
    c[np.hypot(u - 0.3, v + 0.36) < 0.1] = GLINT
    return c


def eyes(k, F, lid=CINNAMON, ring=TAN, outline=None, centre=None, look_of=None):
    """Two patches laid by hand, u toward the back on both; a pale ring round each that stays in every state."""
    LID, RING = srgb(lid), srgb(ring)
    E = on_head(F, F.EYEDIR) if centre is None else centre
    m = np.array([-1, 1, 1.0])
    rim = 0.8

    def lay(state):
        base = eye_look(state, eye_open if look_of is None else look_of, LID, INK, line=0.17)

        def look(u, v):
            c = base(u, v)
            c[np.hypot(u, v) > rim] = RING
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


def pupazel(src, out_path):
    F = frame()
    k = Kit(src, coat="fur")
    body(k, F)
    breast(k, F)
    crest(k, F)
    wings_folded(k, F)
    tail(k, F)
    beak(k, F)
    legs(k, F)
    k.build(voxel=F.VOXEL, band=0.006, wband=0.04)
    k.eye_states(eyes(k, F))
    k.export(out_path)


if __name__ == "__main__":
    pupazel(*args())
