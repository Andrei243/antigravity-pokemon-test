# Cormoran sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cormoran.py -- <the export's Cormoran.glb> <out.glb>
#
# Carpathia's delta cormorant (No. 10155, Water/Dark), from its picture
# (docs/art/reference/gemini/carpathia/10155-Cormoran.jpg) and its brief (CP-dex-3b-river-and-caves-more.md, 155): an
# upright black cormorant in glossy green-black plumage with teal sheen and navy under the wings; a long S neck to a
# small head carried forward, a long grey beak hooked at its tip, a bare yellow patch from the beak's root down the
# throat, a sharp turquoise eye under a low brow, a ragged crest of short spikes at the back of the crown; the wings
# held open like a dark cloak to dry, raised from the shoulders to wrists at the head's height and hanging their long
# feathers down beside the body to the feet, cupped forward; a long wedge of tail feathers hanging behind; short dark
# legs and long-toed clawed feet. The perch (a snag) is scenery and left out; it stands on the ground.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, hull, outward, srgb, inside, smoothstep, unit, eye_look, args, sibling

p_ = sibling("pidgey")
spike = p_.spike

BLACK, SHEEN, NAVY, NAVY2 = "#1C2A27", "#29483F", "#2A3A66", "#34477A"
YELLOW, YELLOWDK, BEAK, BEAKDK, FOOT, CLAW = "#F2C232", "#D69A1E", "#747A82", "#4C5158", "#26282C", "#9A9EA6"
COVERT = "#21352F"
IRIS, PUPIL, INK ="#30E0D0", "#0E1416", "#0C1012"
WHITEC, INKC = np.array([1.0, 1.0, 1.0]), srgb(INK)

VOXEL = 0.0066
HJ = np.array([0.0, -0.04, 0.578])            # the head's joint
HC = np.array([0.0, -0.075, 0.80])            # the head's centre (head-local, before its turn)
HR = np.array([0.069, 0.085, 0.069])
YAW = -16.0                                   # the head turned toward -X, so the front camera sees it near profile
SH = np.array([0.085, 0.02, 0.56])            # the right shoulder, on wingR's joint


def on_ell(c, r, d):
    d = np.array(d, float)
    return np.array(c, float) + d / np.linalg.norm(d / np.array(r, float))


def turn(V):
    """Head-local points turned about the head's upright axis through the neck's top."""
    a = math.radians(YAW)
    m = np.array([[math.cos(a), -math.sin(a), 0], [math.sin(a), math.cos(a), 0], [0, 0, 1.0]])
    pivot = np.array([0.0, -0.05, 0.0])
    return (np.asarray(V, float) - pivot) @ m.T + pivot


def turned(shape):
    V, F = shape
    return turn(V), F


def unturn(P):
    a = math.radians(-YAW)
    m = np.array([[math.cos(a), -math.sin(a), 0], [math.sin(a), math.cos(a), 0], [0, 0, 1.0]])
    pivot = np.array([0.0, -0.05, 0.0])
    return (P - pivot) @ m.T + pivot


def board(k):
    """The game's board check on the built body: the span it shows at the board's turn against its square."""
    V = np.array([v.co[:] for v in k.ob.data.vertices])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    sq = 1.3 * max(h, 0.85 * max(w, l))
    print("KIT: board: shows %.3f in a square of %.3f (%.2f); four corners %.3f" % (np.ptp(proj), sq, np.ptp(proj) / sq,
                                                                                  0.853 * w + 0.522 * l))


# --- the body -------------------------------------------------------------------------------------------------------
def body(k):
    k.add("body", ell((0, 0.0, 0.415), (0.112, 0.106, 0.205), rot=(8, 0, 0)), BLACK, "body")
    k.add("chest", ell((0, -0.045, 0.52), (0.092, 0.08, 0.095)), BLACK, "body")
    k.add("rump", ell((0, 0.05, 0.31), (0.085, 0.08, 0.08)), BLACK, "tail")
    k.add("lowbody", ell((0, -0.005, 0.235), (0.1, 0.092, 0.1)), BLACK, "body")
    k.add("neck", tube([(0, -0.02, 0.56), (0, -0.07, 0.63), (0, -0.045, 0.69), (0, -0.065, 0.76)],
                       [0.054, 0.042, 0.039, 0.044]), BLACK, "head")
    k.add("head", turned(ell(HC, HR, rot=(-6, 0, 0))), BLACK, "head")
    # The thighs fused into the flanks, short thick shins under them
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"thigh{side}", ell((0.048 * s, -0.005, 0.2), (0.046, 0.055, 0.075)), BLACK, f"leg{side}")
        k.add(f"shin{side}", tube([(0.06 * s, 0.0, 0.14), (0.064 * s, -0.008, 0.04)], [0.03, 0.025]), FOOT,
              f"leg{side}")


def plumage(k):
    """The teal sheen over the chest and the nape, navy low on the flanks, the shins dark."""
    k.paint(lambda P: inside(((P[:, 0] / 0.08) ** 2 + ((P[:, 2] - 0.53) / 0.1) ** 2) - 1, 0.25) * (P[:, 1] < -0.02) * 0.6,
            SHEEN, on=["body", "chest"])
    k.paint(lambda P: inside(((P[:, 0] / 0.06) ** 2 + ((P[:, 2] - 0.39) / 0.08) ** 2) - 1, 0.3) * (P[:, 1] < -0.04)
            * 0.6, SHEEN, on=["body"])
    k.paint(lambda P: inside(P[:, 2] - 0.36, 0.03) * inside(0.24 - P[:, 2], 0.03)
            * smoothstep(0.05, 0.1, np.abs(P[:, 0])) * 0.8, NAVY, on=["body"])
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.032, 0.01) * (P[:, 1] > -0.02) * inside(P[:, 2] - 0.72, 0.03)
            * inside(0.6 - P[:, 2], 0.03), SHEEN, on=["neck"])


def throat(k):
    """The bare yellow skin from the beak's root down the throat, in the head's own frame."""
    def m(P):
        Q = unturn(P)
        x, y, z = Q[:, 0], Q[:, 1], Q[:, 2]
        top = 0.806 - 0.25 * np.clip(x / 0.06, 0, 1) ** 2 * 0.1          # under the eye's line
        lower = inside(z - top, 0.004) * inside(0.675 - z, 0.006)
        front = inside(y - (-0.075 - 0.25 * (z - 0.70)), 0.006)         # the front of the throat and the face's base
        wide = inside(np.abs(x) - (0.04 + 0.25 * (z - 0.68)), 0.006)
        return lower * front * wide
    k.paint(m, YELLOW)
    # the scaly texture of the bare skin: two darker arcs across it
    def arcs(P):
        Q = unturn(P)
        x, y, z = Q[:, 0], Q[:, 1], Q[:, 2]
        band = sum(inside(np.abs(z - z0 + 0.25 * x * x / 0.002 * 0.004) - 0.0035, 0.002) for z0 in (0.725, 0.75))
        return np.clip(band, 0, 1) * inside(np.abs(x) - 0.03, 0.005) * inside(y + 0.09, 0.006) * 0.7
    k.paint(arcs, YELLOWDK)


# --- the head's loose parts -----------------------------------------------------------------------------------------
def beak(k):
    """A long grey beak from the face, hooked down at its tip; a darker lower mandible under it."""
    up = [(0, -0.128, 0.795), (0, -0.19, 0.79), (0, -0.245, 0.778), (0, -0.272, 0.758), (0, -0.266, 0.734)]
    k.add("beak", outward(turned(tube(up, [0.021, 0.0165, 0.0145, 0.0115, 0.0075]))), BEAK, "head", fuse=False,
          surface="shell")
    lo = [(0, -0.12, 0.772), (0, -0.18, 0.77), (0, -0.232, 0.763)]
    k.add("beaklow", outward(turned(tube(lo, [0.017, 0.0135, 0.009]))), BEAKDK, "head", fuse=False, surface="shell")


def crest(k):
    """A ragged crest of short thick spikes at the back of the crown, fanned up and back and toward +X."""
    base = HC + np.array([0, 0.035, HR[2] * 0.72])
    for j, (dx, dy, dz, ln) in enumerate(((0.35, 0.55, 0.85, 0.085), (0.0, 0.45, 0.9, 0.08), (-0.3, 0.6, 0.78, 0.065),
                                          (0.6, 0.75, 0.55, 0.07), (0.15, 0.95, 0.45, 0.07), (-0.5, 0.85, 0.45, 0.055))):
        d = unit(dx + 0.25, dy, dz)
        b = base + np.array([dx * 0.025, dy * 0.02 - 0.01, -0.01])
        sh = spike(b, b + d * ln, 0.034, 0.024, side=np.cross(d, (0, 1.0, 0.3)), at=0.3)
        k.add(f"crest{j}", turned(sh), BLACK if j % 2 else SHEEN, "head", fuse=False, surface="fur")


# --- the wings: a cloak held open ----------------------------------------------------------------------------------
def wing(k, s, side):
    """
    Raised from the shoulder up and out to a wrist at the head's height, cupped forward; the coverts along it; seven
    long feathers hanging from it to beside the feet, navy within, green-black without.
    """
    sh = SH * np.array([s, 1, 1])
    el = np.array([0.19 * s, -0.035, 0.69])
    wr = np.array([0.275 * s, -0.095, 0.765])
    k.add(f"wingarm{side}", tube([sh - np.array([0.02 * s, 0, 0.01]), el, wr], [0.04, 0.032, 0.026]), BLACK,
          f"wing{side}")
    arm = unit(*(wr - sh))
    # the wing's plane: along the arm and hanging down, its face turned some 30 degrees toward the front
    down = np.array([0.0, 0.0, -1.0])
    n = np.cross(arm, down); n /= np.linalg.norm(n)
    # coverts: a broad plate along the arm, hanging over the feathers' roots
    ex = unit(*(wr - sh))
    ez = unit(*(down - ex * (down @ ex)))
    L = np.linalg.norm(wr - sh)
    COV = [(-0.03, 0.03), (L * 0.5, 0.045), (L + 0.03, 0.03), (L + 0.02, -0.06), (L * 0.75, -0.13), (L * 0.4, -0.15),
           (0.0, -0.11)]
    k.add(f"coverts{side}", slab(COV, sh + np.array([0, -0.006, 0]), ex, ez, 0.032), COVERT,
          f"wing{side}", fuse=False, surface="fur")
    # the hanging flight feathers, innermost shortest; tips spread out and a little forward
    FEATHERS = [(0.08, 0.40, 0.10, NAVY), (0.24, 0.31, 0.11, NAVY2), (0.4, 0.22, 0.12, NAVY), (0.56, 0.14, 0.125, BLACK),
                (0.72, 0.08, 0.13, NAVY2), (0.87, 0.04, 0.13, BLACK), (1.0, 0.03, 0.12, BLACK)]
    for j, (t, ztip, w, col) in enumerate(FEATHERS):
        b = sh + (wr - sh) * t + np.array([0, 0, -0.02])
        tip = np.array([b[0] + (0.02 + 0.09 * t) * s, b[1] - 0.03 - 0.04 * t, ztip])
        d = unit(*(tip - b))
        sd = np.cross(n * (1 if s > 0 else -1), d) * 0 + arm
        k.add(f"feather{side}{j}", spike(b, tip, w, 0.03, side=sd, at=0.3), col, f"wing{side}", fuse=False,
              surface="fur")
    # ragged points on the wrist
    for j, (dx, dz) in enumerate(((0.5, 0.9), (0.9, 0.5), (0.1, 1.0))):
        d = unit(dx * s, -0.15, dz)
        k.add(f"wristpoint{side}{j}", spike(wr - d * 0.01, wr + d * 0.06, 0.034, 0.024, side=arm, at=0.3), BLACK,
              f"wing{side}", fuse=False, surface="fur")


def tail(k):
    """A long wedge of stiff tail feathers hanging behind, swept a little toward +X so both cameras see its face."""
    base = np.array([0.0, 0.09, 0.3])
    for j, a in enumerate((-0.28, -0.12, 0.04, 0.2, 0.36)):
        d = unit(0.28 + a, 0.72, -0.66 + abs(a - 0.04) * 0.2)
        ln = 0.27 - abs(a - 0.04) * 0.15
        b = base + np.array([a * 0.06, 0, 0])
        side = np.cross(d, (0, 0, 1.0))
        k.add(f"tail{j}", spike(b, b + d * ln, 0.075, 0.03, side=side, at=0.3), BLACK if j % 2 else NAVY, "tail",
              fuse=False, surface="fur")


def toe(base, d, length, w, t):
    """A long toe ending in a short pale claw: two flat-faced hulls."""
    base = np.array(base, float); d = unit(*d)
    s = np.cross(d, (0, 0, 1.0)); s /= np.linalg.norm(s)
    up = np.cross(s, d)
    tip = base + d * length
    T = hull([base + s * w / 2, base - s * w / 2, base + up * t, base + up * t * 0.2 + s * w * 0.3,
              base + up * t * 0.2 - s * w * 0.3, tip + s * w * 0.3, tip - s * w * 0.3, tip + up * t * 0.6])
    C = hull([tip + s * w * 0.28 + up * t * 0.55, tip - s * w * 0.28 + up * t * 0.55, tip + s * w * 0.28 - d * 0.004,
              tip - s * w * 0.28 - d * 0.004, tip + d * 0.026 + up * 0.002])
    return T, C


def feet(k):
    for s, side in ((1, "R"), (-1, "L")):
        heel = np.array([0.066 * s, -0.005, 0.002])
        for j, (dx, ln) in enumerate(((0.45, 0.07), (0.05, 0.085), (-0.35, 0.072))):
            T, C = toe(heel, (dx * s, -1.0, 0.0), ln, 0.024, 0.022)
            k.add(f"toe{side}{j}", T, FOOT, f"leg{side}", fuse=False, surface="scales")
            k.add(f"claw{side}{j}", C, CLAW, f"leg{side}", fuse=False, surface="shell")
        T, C = toe(heel, (0.2 * s, 1.0, 0.0), 0.04, 0.022, 0.02)
        k.add(f"toeback{side}", T, FOOT, f"leg{side}", fuse=False, surface="scales")
        k.add(f"clawback{side}", C, CLAW, f"leg{side}", fuse=False, surface="shell")


# --- the eyes ------------------------------------------------------------------------------------------------------
LID = srgb(BLACK)
EYEDIR = unit(0.72, -0.62, 0.3)
EYE = (0.022, 0.019)
BROW = (0.3, 0.42)                         # the brow's cut: v = a + b u, low toward the beak (u inward is -u)


def eye_open():
    I, P = srgb(IRIS), srgb(PUPIL)
    a, b = BROW
    def look(u, v):
        c = np.tile(I, (len(u), 1))
        c[u * u + v * v > 0.86 ** 2] = INKC
        c[(u - 0.05) ** 2 + (v + 0.02) ** 2 < 0.36 ** 2] = P
        c[(u + 0.28) ** 2 + (v - 0.22) ** 2 < 0.18 ** 2] = WHITEC
        edge = a + b * u
        c[v > edge] = LID
        c[np.abs(v - edge) < 0.12] = INKC
        return c
    return look


def eyes(k):
    def lay(state):
        for s in (1, -1):
            d = EYEDIR * np.array([s, 1, 1])
            c = turn(on_ell(HC, HR, d)[None])[0]
            n = turn(d[None] + np.array([0.0, -0.05, 0.0]))[0] - np.array([0.0, -0.05, 0.0])
            base = eye_look(state, eye_open(), LID, INKC, line=0.16)
            # a patch laid by hand has u toward +Y-ish (back) on +X and toward the front on -X: make +u back on both
            look = base if s > 0 else (lambda u, v, f=base: f(-u, v))
            k.decal(c, n, EYE, look, eps=0.003, rings=20, segs=56)
    return lay


def cormoran(src, out_path):
    k = Kit(src, coat="fur")
    body(k)
    wing(k, 1, "R")
    wing(k, -1, "L")
    plumage(k)
    throat(k)
    beak(k)
    crest(k)
    tail(k)
    feet(k)
    k.build(voxel=VOXEL, band=0.006)
    board(k)
    k.eye_states(eyes(k))
    k.export(out_path)


if __name__ == "__main__":
    cormoran(*args())
