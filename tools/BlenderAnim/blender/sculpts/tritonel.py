# Tritonel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tritonel.py -- <the export's Tritonel.glb> <out.glb>
# Carpathia No. 10068, the pond newt (CP-dex-2-mountains.md, 68). From its picture: a small olive newt standing with its
# front raised, so that the round head sits high over a deep chest; a bright orange throat, chest and belly with dark
# brown spots, the orange reaching the lower jaw under a smiling mouth line; a low wavy tan crest from the back of the
# head down the back into the tail; a long flat tail, leaf-shaped, with a wavy fin above and below edged tan-orange;
# short legs, the front pair under the chest, four small fingers and five toes; round brown eyes with a large dark
# pupil and a white glint, two small nostrils; pale droplets on the skin.
#
# Written to be grown from (tritanor.py imports it with sibling("tritonel")): the palette, the eye, the spots, the
# crest and fin sheets and the ellipsoid helpers are functions below; the calls that build Tritonel are under __main__.
# The export's joints: body (0, 0.02, 0.14), head (0, -0.14, 0.18), tail (0, 0.15, 0.14), legs (+-0.1, -0.1, 0.1),
# hinds (+-0.1, 0.1, 0.1).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, outward, srgb, inside, smoothstep, unit, eye_look, polygon, args

# --- the line's palette ---------------------------------------------------------------------------------------------
OLIVE = "#7A7440"          # the coat
BELLY = "#F28628"          # the orange underside
SPOT = "#3E3020"           # its spots
CREST = "#C2884E"          # the crest's and the fin's tan
EDGE = "#E2914A"           # the fin's orange edge
FIN = "#8C8248"            # the fin's olive, lighter than the coat
DROP = "#DCEEF4"           # droplets
IRIS, PUPIL, RIM = srgb("#6E4630"), srgb("#1A120C"), srgb("#241A10")
WHITE = np.array([1.0, 1.0, 1.0])


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre."""
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r, float)) ** 2).sum())


def normal_on(c, r, d):
    p = on_ell(c, r, d) - np.array(c, float)
    n = p / np.array(r, float) ** 2
    return n / np.linalg.norm(n)


# --- the eye: round, a dark rim, the iris, a large pupil and a glint -------------------------------------------------
def round_eye(iris, pupil=PUPIL, rim=RIM, rim_at=0.84, pupil_r=0.5, slit=None, glint=True):
    """u outward, v up. slit: a vertical slit pupil (its half width as a share) in place of the round one."""
    def look(u, v):
        c = np.tile(iris, (len(u), 1))
        rr = u * u + v * v
        if slit:
            c[(u / slit) ** 2 + (v / 0.78) ** 2 < 1] = pupil
        else:
            c[((u + 0.06) ** 2 + (v + 0.02) ** 2) < pupil_r * pupil_r] = pupil
        c[rr > rim_at * rim_at] = rim
        if glint:
            c[((u + 0.3) / 0.17) ** 2 + ((v - 0.3) / 0.19) ** 2 < 1] = WHITE
        return c
    return look


def lay_eyes(k, E, de, half, look, lid, ink, line=0.17):
    """The eyes' layer for each state, mirrored: lay(state) as eye_states wants it."""
    def lay(state):
        fn = look if state == "Open" else eye_look(state, look, srgb(lid), ink, line=line)
        k.decal(E, de, half, fn, eps=0.0025 * half[0] / 0.026, rings=20, segs=56, mirror=True)
    return lay


# --- a wavy fin or crest: a thick sheet along a line, standing up from it ---------------------------------------------
def curve(pts, n=200):
    """A smooth line through points (the kit's tube's Catmull-Rom), sampled evenly by arc length: n x 3."""
    p = np.array(pts, float)
    ext = np.vstack([2 * p[0] - p[1], p, 2 * p[-1] - p[-2]])
    C = []
    for i in range(len(p) - 1):
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        for t in np.linspace(0, 1, 40, endpoint=False):
            C.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                            + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    C.append(p[-1])
    C = np.array(C)
    cum = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(C, axis=0), axis=1))])
    tt = np.linspace(0, cum[-1], n)
    return np.stack([np.interp(tt, cum, C[:, i]) for i in range(3)], 1)


def _along(C, u):
    t = u * (len(C) - 1)
    return np.stack([np.interp(t, np.arange(len(C)), C[:, i]) for i in range(3)], -1)


def frill(line, height, waves, amp, thick, n=120, lean=0.0, nv=6, way=None):
    """A sheet whose foot follows line (points) and whose edge stands height(t) over it, straight up (t 0..1 along the
    line), scalloped by waves of amp: a crest along a back. lean: how far back (+Y) the edge stands for its height.
    way: a function of the sampled line giving, for each sample, the way the edge stands from its foot instead."""
    C = curve(line)
    D = way(C) if way is not None else None
    def at(u, v):
        hgt = height(u) * (1 + amp * np.sin(u * waves * math.pi) ** 2)
        off = v * hgt
        if D is not None:
            return _along(C, u) + _along(D, u) * off[..., None]
        return _along(C, u) + np.stack([np.zeros_like(off), off * lean, off], -1)
    return outward(sheet(at, n, nv, thick))


def stand_out(C, up=0.6, tilt=30.0):
    """For a crest's foot sampled along a line: the way out of the back, square to the line in the midline plane,
    turned toward the sky by up and leaned toward +X by tilt degrees, so that the front camera sees its face."""
    T = np.gradient(C, axis=0)
    N = np.stack([np.zeros(len(C)), -T[:, 2], T[:, 1]], 1)
    N = N * np.where((N @ np.array([0, 1.0, 1.0])) < 0, -1, 1)[:, None]
    N /= np.linalg.norm(N, axis=1, keepdims=True) + 1e-9
    N = N + np.array([0, 0, up])
    N /= np.linalg.norm(N, axis=1, keepdims=True)
    t = math.radians(tilt)
    N = N * math.cos(t) + np.array([1.0, 0, 0]) * math.sin(t)
    return N / np.linalg.norm(N, axis=1, keepdims=True)


def fin_sheet(line, top, bottom, waves, amp, thick, n=120):
    """A fin through a tail's line, in the vertical plane along it: top(t) over the line and bottom(t) under it."""
    C = curve(line)
    def at(u, v):
        wav = 1 + amp * np.abs(np.sin(u * waves * math.pi))
        z = np.where(v > 0.5, (v - 0.5) * 2 * top(u) * wav, (v - 0.5) * 2 * bottom(u) * wav)
        return _along(C, u) + np.stack([np.zeros_like(z), np.zeros_like(z), z], -1)
    return outward(sheet(at, n, 9, thick))
def spots(k, places, colour, eps):
    """Round spots on ellipsoids: (centre, radii, direction, half size) each."""
    for c, r, d, h in places:
        k.decal(on_ell(c, r, d), normal_on(c, r, d), (h, h), colour, eps=eps, rings=6, segs=28)


# --- Tritonel ---------------------------------------------------------------------------------------------------------
HEAD_C, HEAD_R = np.array([0.0, -0.175, 0.252]), np.array([0.1, 0.09, 0.063])
SNOUT_C, SNOUT_R = np.array([0.0, -0.215, 0.24]), np.array([0.078, 0.052, 0.048])
CHEST_C, CHEST_R = np.array([0.0, -0.118, 0.158]), np.array([0.096, 0.088, 0.108])
TRUNK_C, TRUNK_R = np.array([0.0, -0.01, 0.13]), np.array([0.096, 0.125, 0.088])
HIPS_C, HIPS_R = np.array([0.0, 0.085, 0.1]), np.array([0.084, 0.075, 0.068])
TAIL = [(0.0, 0.12, 0.098), (0.012, 0.21, 0.086), (0.048, 0.3, 0.068), (0.105, 0.37, 0.05), (0.185, 0.415, 0.036)]
TAIL_R = [0.058, 0.044, 0.032, 0.02, 0.008]
MOUTH_C, MOUTH = np.array([0.0, -0.262, 0.222]), "#2E2A18"
BACK = [(0.0, -0.135, 0.29), (0.0, -0.11, 0.268), (0.0, -0.07, 0.24), (0.0, -0.01, 0.207), (0.0, 0.06, 0.168),
        (0.0, 0.12, 0.145)]


def tritonel(src, out):
    k = Kit(src, coat="scales")

    # The body: the hips low, the trunk rising to a deep chest under the head
    k.add("hips", ell(HIPS_C, HIPS_R), OLIVE, "body")
    k.add("trunk", ell(TRUNK_C, TRUNK_R, rot=(-22, 0, 0)), OLIVE, "body")
    k.add("chest", ell(CHEST_C, CHEST_R), OLIVE, "body")
    # one smooth back over the three, so that the body is no row of balls from behind
    k.add("spine", tube([(0, 0.11, 0.1), (0, 0.02, 0.125), (0, -0.07, 0.16), (0, -0.14, 0.2)],
                        [0.072, 0.086, 0.094, 0.085]), OLIVE, "body")
    k.add("throat", ell((0, -0.19, 0.205), (0.084, 0.066, 0.05)), OLIVE, "head")

    # The head: round and broad, a blunt rounded snout
    k.add("head", ell(HEAD_C, HEAD_R), OLIVE, "head")
    k.add("snout", ell(SNOUT_C, SNOUT_R), OLIVE, "head")

    # The front legs under the chest, short and a little apart; four small fingers
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"shoulder{side}", ell(np.array((0.07, -0.105, 0.125)) * m, (0.038, 0.04, 0.045)), OLIVE, f"leg{side}")
        k.add(f"arm{side}", tube(np.array([(0.075, -0.105, 0.12), (0.098, -0.122, 0.065), (0.104, -0.135, 0.018)]) * m,
                                 [0.027, 0.022, 0.019]), OLIVE, f"leg{side}")
        k.add(f"hand{side}", ell(np.array((0.106, -0.142, 0.011)) * m, (0.024, 0.024, 0.01)), OLIVE, f"leg{side}")
        for j, d in enumerate(((0.75, -0.65), (0.2, -1.0), (-0.3, -0.95), (-0.85, -0.5))):
            b = np.array((0.106, -0.142, 0.011))
            tip = b + np.array((d[0], d[1], 0)) * 0.032 + np.array((0, 0, -0.004))
            k.add(f"finger{side}{j}", tube(np.array([b, (b + tip) / 2 + (0, 0, 0.003), tip]) * m,
                                           [0.0075, 0.0068, 0.006], seg=12), OLIVE, f"leg{side}")
        # the hind legs: haunches fused into the hips, short shins, flat feet with five toes
        k.add(f"haunch{side}", ell(np.array((0.078, 0.095, 0.09)) * m, (0.045, 0.052, 0.05)), OLIVE, f"hind{side}")
        k.add(f"shin{side}", tube(np.array([(0.09, 0.098, 0.075), (0.112, 0.092, 0.035), (0.118, 0.085, 0.016)]) * m,
                                  [0.03, 0.025, 0.021]), OLIVE, f"hind{side}")
        k.add(f"foot{side}", ell(np.array((0.122, 0.075, 0.011)) * m, (0.028, 0.03, 0.011)), OLIVE, f"hind{side}")
        for j, d in enumerate(((0.95, -0.3), (0.6, -0.8), (0.05, -1.0), (-0.5, -0.85), (1.0, 0.35))):
            b = np.array((0.122, 0.075, 0.011))
            tip = b + np.array((d[0], d[1], 0)) * 0.036 + np.array((0, 0, -0.004))
            k.add(f"toe{side}{j}", tube(np.array([b, (b + tip) / 2 + (0, 0, 0.003), tip]) * m,
                                        [0.008, 0.007, 0.0062], seg=12), OLIVE, f"hind{side}")

    # The tail: a tube curving back and out to +X, its flat fin above and below, edged orange
    k.add("tail", tube(TAIL, TAIL_R), OLIVE, "tail")
    top = lambda t: 0.022 + 0.064 * np.sin(np.clip(t, 0, 1) * math.pi) ** 0.8 * (1 - 0.55 * t)
    bot = lambda t: 0.018 + 0.042 * np.sin(np.clip(t, 0, 1) * math.pi) ** 0.8 * (1 - 0.6 * t)
    k.add("finedge", fin_sheet(TAIL, lambda t: top(t) + 0.009, lambda t: bot(t) + 0.009, 7, 0.08, 0.009),
          CREST, "tail", fuse=False)
    k.add("fin", fin_sheet(TAIL, top, bot, 7, 0.08, 0.013), FIN, "tail", fuse=False)
    # the crest along the back: low and wavy
    k.add("crest", frill(BACK, lambda t: 0.032 + 0.016 * np.sin(t * math.pi), 5, 0.25, 0.014),
          CREST, "body", fuse=False)

    # The orange underside: throat, chest and belly, and the lower jaw under the mouth line
    def under(P):
        y, z, x = P[:, 1], P[:, 2], P[:, 0]
        # under a line slanting from the jaw (y -0.24, z 0.215) back to the hips (y 0.12, z 0.07)
        zl = 0.215 + (y + 0.24) * (0.07 - 0.215) / 0.36
        ventral = inside(z - zl, 0.004) * (y < 0.14)
        sides = inside(np.abs(x) - (0.072 + 0.5 * np.clip(zl - z, 0, 0.12)), 0.004)
        return ventral * sides
    k.paint(under, BELLY, on=["chest", "trunk", "throat", "hips", "head", "snout"])
    # the mouth line: a dark smile where the orange meets the olive on the snout's front, a crisp patch
    smile = [(u, 0.1 + 0.55 * u * u) for u in np.linspace(1, -1, 13)] + \
            [(u, -0.16 + 0.55 * u * u) for u in np.linspace(-1, 1, 13)]
    k.decal(MOUTH_C, unit(0, -1, -0.12), (0.066, 0.03), MOUTH, eps=0.0018, outline=polygon(smile), rings=8,
            segs=72)

    # Spots on the orange
    places = []
    for d, h in (((0.0, -1, -0.15), 0.013), ((0.45, -0.85, -0.25), 0.012), ((-0.4, -0.85, -0.05), 0.012),
                 ((0.25, -0.9, 0.25), 0.011), ((-0.2, -0.95, -0.45), 0.013), ((0.55, -0.7, -0.55), 0.011),
                 ((-0.6, -0.65, -0.4), 0.011), ((0.15, -0.8, -0.65), 0.012), ((-0.05, -0.7, -0.8), 0.012)):
        places.append((CHEST_C, CHEST_R, d, h))
    for d, h in (((0.3, -0.3, -0.9), 0.012), ((-0.35, -0.1, -0.9), 0.012), ((0.1, 0.25, -0.95), 0.011),
                 ((0.55, 0.0, -0.75), 0.01), ((-0.5, 0.35, -0.75), 0.01)):
        places.append((TRUNK_C + (0, 0, 0), TRUNK_R, d, h))
    for d, h in (((0.35, -0.75, -0.55), 0.009), ((-0.4, -0.75, -0.5), 0.009), ((0.0, -0.65, -0.75), 0.009)):
        places.append((np.array((0, -0.19, 0.205)), np.array((0.084, 0.066, 0.05)), d, h))
    spots(k, places, SPOT, 0.0018)
    # Droplets on the back, the head and the tail's root
    for c, r, d, h in ((HEAD_C, HEAD_R, (0.15, 0.1, 1), 0.008), (HEAD_C, HEAD_R, (0.6, 0.35, 0.75), 0.007),
                       (TRUNK_C, TRUNK_R, (0.55, 0.1, 0.8), 0.009), (TRUNK_C, TRUNK_R, (-0.6, 0.3, 0.7), 0.008),
                       (HIPS_C, HIPS_R, (0.5, 0.4, 0.75), 0.008), (CHEST_C, CHEST_R, (0.75, -0.1, 0.55), 0.008)):
        k.decal(on_ell(c, r, d), normal_on(c, r, d), (h * 0.8, h), DROP, eps=0.0018, rings=5, segs=24)
    # Nostrils
    for s in (1, -1):
        d = (0.28 * s, -0.85, 0.42)
        k.decal(on_ell(SNOUT_C, SNOUT_R, d), normal_on(SNOUT_C, SNOUT_R, d), (0.0065, 0.0055), "#2A2416",
                eps=0.0018, rings=4, segs=20)

    # The eyes: round, on the upper sides of the head, looking forward and out
    de = unit(0.62, -0.62, 0.46)
    E = on_ell(HEAD_C, HEAD_R, de)
    lay = lay_eyes(k, E, normal_on(HEAD_C, HEAD_R, de), (0.027, 0.027), round_eye(IRIS), OLIVE, RIM)

    k.build(voxel=0.0035, smooth=4, band=0.004, wband=0.03)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    tritonel(*args())
