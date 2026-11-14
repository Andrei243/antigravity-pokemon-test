# Magnezone sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/magnezone.py -- <the export's Magnezone.glb> <out.glb>
# Grown from magnemite.py: its frame (constants, the ball, the eye, the screws and the magnets) kept as it is.
#
# The design, from the references: a flying saucer of steel. A wide domed top, grey over its crown and blue-grey round
# its lower half, a thin flat flange standing out all round at its widest, and a rounder blue belly under it. In the
# middle of the front one great eye, wider than tall, its top cut flat by the grey crown: white, a dark outline, a red
# pupil with a lighter ring in it. A yellow aerial on the crown, thicker at its foot, a yellow ball on top; a round
# flat plate on the crown behind it. At each side, sunk into the saucer on the flange, a little Magnemite's ball with
# its own small eye looking out to its side and a screw standing on its top, leaning outward. Three horseshoe magnets,
# darker than Magnemite's: one under each side unit, hanging down and opening forward and out (blue end up, red end
# down), and one under the back. (No screws on the lower rim: the pictures show none.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, unit, eye_look, args

STEEL, SCREW, CROSS = "#B4C8D4", "#A3AAB3", "#4A5059"
MAGNET, RED, BLUE = "#9A9EA6", "#D9464C", "#3F8FDB"
WHITE, INK, PUPIL = "#F6F7F8", "#2A2E35", "#16181C"

# A ball's parts as shares of its radius
EYE_HALF = (0.47, 0.53)          # the eye's half width and height
PUPIL_R = 0.19                   # the pupil, a share of the eye's half size
RIM = 0.91                       # the outline's inner edge, a share of the eye's half size (0.82 was a heavy ring at the game's size)
SCREW_HEAD = 0.24                # a small screw's head radius
TOP_HEAD, TOP_SHANK, TOP_RISE = 0.44, 0.14, 0.34      # the top screw: head radius, shank radius, the shank's height
MAG_R, MAG_BAR, MAG_PRONG, MAG_TIP = 0.36, 0.42, 0.72, 0.36   # a magnet: bend radius, bar side, prong, coloured end


# --- hard-edged pieces --------------------------------------------------------------------------------------------
def frame(axis):
    """Two directions square to an axis and to each other (e1 x e2 = axis)."""
    a = np.array(axis, float); a /= np.linalg.norm(a)
    ref = np.array([0, 0, 1.0]) if abs(a[2]) < 0.9 else np.array([1.0, 0, 0])
    e1 = np.cross(ref, a); e1 /= np.linalg.norm(e1)
    e2 = np.cross(a, e1)
    return a, e1, e2


def lathe(profile, base, axis, seg=32):
    """A turned piece: profile (radius, height) from the bottom up the outside; each step of it a band of its own
    points, so the edges between steps stay hard."""
    a, e1, e2 = frame(axis)
    base = np.array(base, float)
    ang = np.linspace(0, 2 * math.pi, seg, endpoint=False)
    V, F = [], []
    for (r0, h0), (r1, h1) in zip(profile[:-1], profile[1:]):
        o = len(V)
        for r, h in ((r0, h0), (r1, h1)):
            V.extend((base + a * h + r * (math.cos(t) * e1 + math.sin(t) * e2)).tolist() for t in ang)
        for j in range(seg):
            jj = (j + 1) % seg
            F.append([o + j, o + jj, o + seg + jj, o + seg + j])
    return np.array(V), F


def box(c, axes, half):
    """A box with hard edges: middle, three directions, half sizes; each face its own points."""
    c = np.array(c, float)
    ax = [np.array(x, float) / np.linalg.norm(x) for x in axes]
    V, F = [], []
    for i in range(3):
        n, p, q = ax[i], ax[(i + 1) % 3], ax[(i + 2) % 3]
        hn, hp, hq = half[i], half[(i + 1) % 3], half[(i + 2) % 3]
        for s in (1, -1):
            o = len(V)
            m = c + n * hn * s
            pts = [m - p * hp - q * hq, m + p * hp - q * hq, m + p * hp + q * hq, m - p * hp + q * hq]
            V.extend(x.tolist() for x in pts)
            F.append([o, o + 1, o + 2, o + 3] if s > 0 else [o, o + 3, o + 2, o + 1])
    return np.array(V), F


def bar(path, side, normal):
    """A bar of square section swept along a path lying in a plane (normal), its four sides strips of their own
    points so the corners stay hard, its ends open (a coloured end is a box of its own)."""
    P = np.array(path, float)
    n = np.array(normal, float); n /= np.linalg.norm(n)
    T = np.gradient(P, axis=0); T /= np.linalg.norm(T, axis=1, keepdims=True)
    W = np.cross(n, T); W /= np.linalg.norm(W, axis=1, keepdims=True)
    h = side / 2
    corners = [(-1, -1), (1, -1), (1, 1), (-1, 1)]          # (along W, along n)
    V, F = [], []
    k = len(P)
    for c0, c1 in zip(corners, np.roll(corners, -1, axis=0)):
        o = len(V)
        for c in (c0, c1):
            V.extend((P + W * (c[0] * h) + n * (c[1] * h)).tolist())
        for i in range(k - 1):
            F.append([o + i, o + i + 1, o + k + i + 1, o + k + i])
    V = np.array(V)
    # wind each strip outward
    out = []
    for f in F:
        a, b, c = V[f[0]], V[f[1]], V[f[2]]
        mid = V[f].mean(0)
        i = np.argmin(np.linalg.norm(P - mid, axis=1))
        out.append(f if np.dot(np.cross(b - a, c - a), mid - P[i]) > 0 else f[::-1])
    return V, out


# --- a Magnemite's parts ------------------------------------------------------------------------------------------
def magnet(k, name, bone, bend, open_dir, normal, r, first, second, colour=MAGNET, prong=MAG_PRONG):
    """A horseshoe magnet whose curve's outer edge lies at bend, its prongs running along open_dir, in the plane
    whose normal is given. first colours the prong on the side of cross(normal, open_dir), second the other."""
    u = unit(*open_dir)
    n = np.array(normal, float); n -= u * np.dot(n, u); n /= np.linalg.norm(n)
    x = np.cross(n, u)
    R, s, pr, tip = MAG_R * r, MAG_BAR * r, prong * r, MAG_TIP * r
    O = np.array(bend, float) + u * (R + s / 2)               # the bend's middle
    arc = [O + R * (math.cos(t) * x - math.sin(t) * u) for t in np.linspace(0, math.pi, 17)]
    a0 = O + x * R
    z0 = O - x * R
    path = [a0 + u * pr * t for t in np.linspace(1, 0, 6)][:-1] + arc + [z0 + u * pr * t for t in np.linspace(0, 1, 6)][1:]
    k.add(name, bar(path, s, n), colour, bone, fuse=False, surface="metal")
    for side, col in ((x, first), (-x, second)):
        c = O + side * R + u * (pr + tip / 2 - 0.0005 * r)
        k.add(name + "end", box(c, (u, x, n), (tip / 2, s / 2 * 1.02, s / 2 * 1.02)), col, bone, fuse=False,
              surface="metal")


def screw(k, name, bone, centre, r, outward, size=SCREW_HEAD, shank=0.0):
    """A cross-headed screw in the ball: a flat round head on the surface, a short shank under it if shank > 0."""
    d = unit(*outward)
    hr = size * r
    base = np.array(centre, float) + d * (r - 0.02 * r + shank * r)
    if shank > 0:
        k.add(name + "shank", lathe([(0, -shank * r - 0.1 * r), (0.4 * hr, -shank * r - 0.1 * r), (0.4 * hr, 0.01 * r),
                                     (0, 0.01 * r)], base, d, 16), SCREW, bone, fuse=False, surface="metal")
    top = 0.36 * hr
    k.add(name, lathe([(0, 0), (hr, 0), (hr, 0.2 * hr), (0.86 * hr, top), (0, top)], base, d), SCREW, bone,
          fuse=False, surface="metal")
    a, e1, e2 = frame(d)
    # the cross, turned a little off square, two dark bars standing just out of the head's flat top
    t = math.radians(30)
    p, q = math.cos(t) * e1 + math.sin(t) * e2, -math.sin(t) * e1 + math.cos(t) * e2
    c = base + a * top
    for ax0, ax1 in ((p, q), (q, p)):
        k.add(name + "cross", box(c, (a, ax0, ax1), (0.06 * hr, 0.62 * hr, 0.16 * hr)), CROSS, bone, fuse=False,
              surface="metal")


def top_screw(k, name, bone, centre, r, up=(0, 0, 1)):
    """The screw standing on top: a threaded shank rising out of the ball under a broad flat head."""
    d = unit(*up)
    base = np.array(centre, float) + d * (r * 0.9)
    sr, rise, hr = TOP_SHANK * r, TOP_RISE * r, TOP_HEAD * r
    prof = [(0, 0), (sr, 0)]
    turns = 4
    for i in range(turns):
        h0 = 0.08 * r + (rise - 0.08 * r) * i / turns
        h1 = 0.08 * r + (rise - 0.08 * r) * (i + 0.5) / turns
        prof += [(sr, h0), (sr * 1.45, h1)]
    prof += [(sr, rise), (0, rise)]
    k.add(name + "shank", lathe(prof, base, d, 20), SCREW, bone, fuse=False, surface="metal")
    th = 0.24 * hr
    k.add(name, lathe([(0, rise), (hr * 0.94, rise), (hr, rise + 0.3 * th), (hr, rise + th), (0.9 * hr, rise + 1.25 * th),
                       (0, rise + 1.3 * th)], base, d, 40), SCREW, bone, fuse=False, surface="metal")
    a, e1, e2 = frame(d)
    c = base + a * (rise + 1.3 * th)
    for ax0, ax1 in ((e1, e2), (e2, e1)):
        k.add(name + "cross", box(c, (a, ax0, ax1), (0.03 * hr, 0.55 * hr, 0.09 * hr)), CROSS, bone, fuse=False,
              surface="metal")


def ball(k, name, bone, centre, r):
    """The ball of steel, its front a little flattened so the eye lies on it without wrapping."""
    c = np.array(centre, float)
    k.add(name, ell(c, (r, r, r)), STEEL, bone)
    k.add(name + "face", ell(c + (0, -0.55 * r, 0), (0.62 * r, 0.47 * r, 0.66 * r)), STEEL, bone)


# The eye: white, a dark outline, a solid round black pupil
CW, CI, CP = srgb(WHITE), srgb(INK), srgb(PUPIL)


def eye_open(u, v):
    c = np.tile(CW, (len(u), 1))
    rr = u * u + v * v
    c[rr > RIM ** 2] = CI
    c[rr < PUPIL_R ** 2] = CP
    return c


def eye(k, state, centre, r, look=(0, -1, 0), lid=STEEL, scale=1.0):
    """A ball's eye in one of the five states: laid on the ball's surface where look points from its middle."""
    n = unit(*look)
    at = np.array(centre, float) + n * r
    k.decal(at, n, (EYE_HALF[0] * r * scale, EYE_HALF[1] * r * scale), eye_look(state, eye_open, srgb(lid), CI, line=0.21),
            eps=0.003 * r / 0.1, rings=20, segs=56)


def magnemite(k, at, r, body="body", magnets=(), screws=(), top=True, name="m"):
    """
    One Magnemite: the ball at `at` of radius r on bone body, the top screw if top, and
    magnets: (bone, side angle in degrees about the front axis from +X, lift of the bend's middle as a share of r,
    first colour, second colour, open direction or None for straight out, plane normal or None for the front);
    screws: (outward direction, shank as a share of r) each.
    """
    at = np.array(at, float)
    ball(k, name, body, at, r)
    if top:
        top_screw(k, name + "top", body, at, r)
    for i, (d, sh) in enumerate(screws):
        screw(k, f"{name}screw{i}", body, at, r, d, shank=sh)
    for i, (bone, ang, lift, first, second, open_dir, normal) in enumerate(magnets):
        t = math.radians(ang)
        side = np.array([math.cos(t), 0, math.sin(t)])
        od = side if open_dir is None else unit(*open_dir)
        bend = at + side * (0.9 * r) + (0, 0, lift * r)
        magnet(k, f"{name}magnet{i}", bone, bend, od, normal if normal is not None else (0, -1, 0), r, first, second)


# --- Magnezone ------------------------------------------------------------------------------------------------------
from sculptkit import inside, polygon

CAP, BLUEGREY, BELLY, FLANGE = "#A7ABB2", "#A3B8D2", "#8CA3C5", "#B0C3DB"
YELLOW, KNOB, DARK_MAGNET = "#E3C23A", "#F3D24C", "#60656E"
RED_EYE, PINK_EYE = "#D7343C", "#F2858A"

RIM_Z = 0.40                                         # the flange's level, just under the body bone
DOME = np.array([0.0, 0.0, RIM_Z])
DOME_R = (0.255, 0.215, 0.16)                        # the domed top (its lower half squashed into a lip)
BELLY_R = (0.205, 0.175, 0.15)
FLANGE_R, FLANGE_T = 0.315, 0.012
CAP_Z = 0.481                                        # where the grey crown begins: it cuts the eye's top
UNIT, UNIT_R = np.array([0.25, -0.01, 0.438]), 0.072  # the +X side unit (the -X one mirrored)
EYE_C, EYE_H = np.array([0.0, 0.0, 0.452]), (0.056, 0.042)
EYE_TOP = 0.7                                        # the eye's outline cut flat at this share of its half height
AERIAL_FOOT, AERIAL_TOP, KNOB_R = 0.548, 0.632, 0.021
MAG_SIZE, MAG_LONG = 0.1, 1.15


def on_dome(d):
    """The point of the domed top's surface along a direction from its middle."""
    d = unit(*d)
    return DOME + d / math.sqrt(sum((d[i] / DOME_R[i]) ** 2 for i in range(3)))


def hanging(open_dir):
    """A magnet's plane normal such that its prongs lie one over the other (first colour on the upper)."""
    u = unit(*open_dir)
    xd = np.array([0, 0, 1.0]) - u * u[2]; xd /= np.linalg.norm(xd)
    return np.cross(u, xd)


def off_units(P):
    """0 on the side units, so the crown and belly paints leave them blue."""
    m = np.ones(len(P))
    for s in (1, -1):
        m *= 1 - inside(np.linalg.norm(P - UNIT * (s, 1, 1), axis=1) - UNIT_R - 0.004, 0.003)
    return m


def saucer(k):
    def squash(V):
        V = V.copy(); V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.3, V[:, 2]); return V
    k.add("dome", ell(DOME, DOME_R, deform=squash), BLUEGREY, "body")
    k.add("belly", ell(DOME, BELLY_R), BELLY, "body")
    k.paint(lambda P: inside(CAP_Z - P[:, 2], 0.0025) * off_units(P), CAP)
    k.paint(lambda P: inside(P[:, 2] - (RIM_Z - 0.006), 0.0025) * off_units(P), BELLY)
    k.add("flange", lathe([(0.2, -FLANGE_T / 2), (FLANGE_R, -FLANGE_T / 2), (FLANGE_R + 0.004, 0), (FLANGE_R, FLANGE_T / 2),
                           (0.2, FLANGE_T / 2)], DOME, (0, 0, 1), 72), FLANGE, "body", fuse=False, surface="metal")
    # the yellow aerial, thicker at its foot, a ball on top
    k.add("aerial", lathe([(0, AERIAL_FOOT - 0.02), (0.02, AERIAL_FOOT - 0.02), (0.014, AERIAL_FOOT + 0.012),
                           (0.0065, AERIAL_FOOT + 0.035), (0.006, AERIAL_TOP), (0, AERIAL_TOP)], (0, 0, 0), (0, 0, 1), 24),
          YELLOW, "body", fuse=False, surface="metal")
    k.add("knob", ell((0, 0, AERIAL_TOP + KNOB_R * 0.8), (KNOB_R,) * 3), KNOB, "body", fuse=False, surface="metal")
    # a round flat plate on the crown, behind the aerial and to the +X side
    d = unit(0.5, 0.35, 0.8)
    k.add("plate", lathe([(0, -0.006), (0.028, -0.006), (0.028, 0.004), (0.025, 0.006), (0, 0.006)], on_dome(d) + d * 0.003,
                         d, 40), CAP, "body", fuse=False, surface="metal")


def units(k):
    for s in (1, -1):
        c = UNIT * (s, 1, 1)
        nm = "unit" + ("R" if s > 0 else "L")
        ball(k, nm, "body", c, UNIT_R)
        top_screw(k, nm + "top", "body", c, UNIT_R, up=(s * 0.3, 0.3, 1))


def magnets(k):
    for s in (1, -1):
        od = (s * 0.72, -0.5, -0.5)
        magnet(k, "magnet" + ("R" if s > 0 else "L"), "arm" + ("R" if s > 0 else "L"),
               UNIT * (s, 1, 1) + (s * -0.02, -0.01, -UNIT_R + 0.004), od, hanging(od), MAG_SIZE, BLUE, RED,
               colour=DARK_MAGNET, prong=MAG_LONG * MAG_PRONG)
    od = (0.12, 0.6, -0.79)
    magnet(k, "magnetBack", "body", (0.03, 0.135, 0.3), od, hanging(od), MAG_SIZE, BLUE, RED, colour=DARK_MAGNET,
           prong=MAG_LONG * MAG_PRONG)


# The great eye: white, a dark outline, its top cut flat; a red pupil with a lighter ring in it
CRED, CPINK = srgb(RED_EYE), srgb(PINK_EYE)
pts = [(math.cos(t), min(math.sin(t), EYE_TOP)) for t in np.linspace(0, 2 * math.pi, 48, endpoint=False)]
BIG_OUTLINE = polygon(pts)


def big_open(u, v):
    c = np.tile(CW, (len(u), 1))
    c[(u * u + v * v > 0.84 ** 2) | (v > EYE_TOP - 0.16)] = CI
    x, y = u * EYE_H[0], (v + 0.04) * EYE_H[1]
    d = np.sqrt(x * x + y * y)
    c[d < 0.021] = CRED
    c[(d < 0.0145) & (d > 0.0095)] = CPINK
    return c


def lay(state):
    n = unit(0, -1, 0.12)
    k.decal(EYE_C + n * 0.25, n, EYE_H, eye_look(state, big_open, srgb(BLUEGREY), CI, line=0.21),
            eps=0.003, rings=20, segs=56, outline=BIG_OUTLINE)
    if state == "Open":
        # the pupil again as round patches of their own, so its rings keep round edges
        c = EYE_C - np.array([0, 0, 0.0017])
        for r_, col, e in ((0.021, RED_EYE, 0.0045), (0.0145, PINK_EYE, 0.0058), (0.0095, RED_EYE, 0.0071)):
            k.decal(c + n * 0.25, n, (r_, r_), col, eps=e, rings=8, segs=48)
    for s in (1, -1):
        eye(k, state, UNIT * (s, 1, 1), UNIT_R, look=(s * 0.55, -0.83, 0.06), lid=BLUEGREY)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="metal")
    saucer(k)
    units(k)
    magnets(k)
    k.build(voxel=0.005, smooth=3)
    k.eye_states(lay)
    k.export(out)
