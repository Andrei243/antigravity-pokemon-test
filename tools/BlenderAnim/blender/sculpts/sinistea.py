# Sinistea sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sinistea.py -- <the export's Sinistea.glb> <out.glb>
#
# The design, from the references: a squat teacup, wider than it is tall, full to the brim of purple tea with an
# orange gleam curving over its surface; the cup pale aqua with a teal band under the gold rim whose lower edge hangs
# in scallops; a short lavender foot on a gold ring; a purple handle on its -X side (the viewer's left), a loop whose
# lower end grips a little white tab like a hand; a drop of the purple tea hanging over the rim on the +X front. The
# face is the cup's own: two big upright eyes, each a white oval in a teal rim with a second teal oval inside it, a
# small teal smile, and a teal curl on each cheek. (The task's text spoke of a swirl of tea rising from the cup as a
# ghost: the pictures show none; the tea lies in the cup, only the drop over the rim.)
# The skeleton: body (0, 0, 0.12) alone; everything is the body's.
# Written to be grown from: Polteageist imports its colours, its rings, its curls and its cup's way of being made.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, lathe, outward, args

# --- the line's colours -------------------------------------------------------------------------------------------
AQUA, TEAL, TEAL_DARK = "#A9E2DD", "#4DB5AD", "#2C8C88"
GOLD, FOOT = "#F2CF55", "#CFCBE6"
TEA, TEA_DEEP, GLEAM = "#9150BE", "#6E3496", "#F2A24A"
WHITE = np.array([1.0, 1.0, 1.0])

# --- the frame ----------------------------------------------------------------------------------------------------
# The cup's outer profile, (radius, height), from the foot up to the rim; the tea's surface is flat at TEA_Z.
CUP = [(0.034, 0.038), (0.062, 0.05), (0.088, 0.072), (0.108, 0.1), (0.118, 0.13), (0.118, 0.158), (0.114, 0.18),
       (0.11, 0.192)]
RIM_Z, RIM_R = 0.192, 0.106
TEA_Z = 0.188
EYE_X, EYE_Z, EYE_HALF = 0.04, 0.118, (0.022, 0.032)
MOUTH_Z = 0.09
VOXEL = 0.0024


def radius_at(z, profile=CUP):
    zs = [p[1] for p in profile]; rs = [p[0] for p in profile]
    return float(np.interp(z, zs, rs))


def on_cup(x, z, profile=CUP, inset=0.002):
    """A point on the cup's side at (x, z), its front, and the side's normal there."""
    r = radius_at(z, profile)
    y = -math.sqrt(max(r * r - x * x, 1e-6))
    dz = 0.004
    slope = (radius_at(z + dz, profile) - radius_at(z - dz, profile)) / (2 * dz)
    n = unit(x / r, y / r, -slope)
    return np.array([x, y, z]) - n * inset, n


def cup_solid(profile, top):
    """The cup and its tea as one solid of turning: the side up to the rim, then flat across the tea."""
    prof = [(0.0, profile[0][1])] + list(profile) + [(profile[-1][0] - 0.004, top), (0.0, top)]
    return lathe((0, 0, 0), (0, 0, 1), prof, seg=72)


def gold_ring(c, radius, thick, seg=72, n=12, axis=(0, 0, 1)):
    """A round ring of gold (a torus) lying level at c: a loose solid of turning, its section a circle."""
    prof = [(radius + thick * math.cos(t), thick * math.sin(t)) for t in np.linspace(0, 2 * math.pi, n + 1)]
    return lathe(c, axis, prof, seg=seg)


def scallops(az, top, depth, n=10, phase=0.0):
    """The band's lower edge round the cup: semicircles hanging down, n of them round (az from the front)."""
    t = (az * n / (2 * math.pi) + phase) % 1.0
    return top - depth * np.sqrt(np.clip(1 - (2 * t - 1) ** 2, 0, 1))


def curl_look(ground, ink, width=0.1):
    """A curl of one line, a spiral of a turn and a quarter from its middle: the cups' and pots' decoration."""
    g, k = srgb(ground), srgb(ink)
    def look(u, v):
        c = np.tile(g, (len(u), 1))
        rho = np.hypot(u, v)
        phi = (np.arctan2(v, u)) % (2 * math.pi)
        best = np.full(len(u), 9.0)
        for turn in (0, 1):
            t = (phi + 2 * math.pi * turn) / (2 * math.pi)
            r = 0.1 + 0.62 * t
            d = np.where(t < 1.3, np.abs(rho - r), 9.0)
            best = np.minimum(best, d)
        c[best < width] = k
        return c
    return look


def cup_eye(u, v):
    """The cup's eye: a white oval in a teal rim, with a second teal oval inside it, set outward and a little up."""
    c = np.tile(WHITE, (len(u), 1))
    q = np.hypot((u - 0.16) / 0.5, (v - 0.04) / 0.62)
    c[np.abs(q - 1) < 0.24] = srgb(TEAL)
    c[np.hypot(u, v) > 0.8] = srgb(TEAL_DARK)
    return c


def gleam_look(u, v):
    """The tea seen from above: purple, an orange gleam curving round its far half."""
    c = np.tile(srgb(TEA), (len(u), 1))
    rho = np.hypot(u, v)
    ang = np.arctan2(v, u)
    arc = (np.abs(rho - 0.52) < 0.09) & (ang > 0.35) & (ang < 2.4)
    c[arc] = srgb(GLEAM)
    return c


# --- the parts ----------------------------------------------------------------------------------------------------
def cup(k):
    k.add("cup", cup_solid(CUP, TEA_Z), AQUA, "body")
    az = lambda P: np.arctan2(P[:, 0], -P[:, 1])
    rad = lambda P: np.hypot(P[:, 0], P[:, 1])
    side = lambda P: (rad(P) > RIM_R - 0.004)
    # the teal band under the rim, its lower edge in scallops
    k.paint(lambda P: inside(scallops(az(P), 0.166, 0.013) - P[:, 2], 0.0018) * side(P), TEAL)
    # the tea, purple to the rim, deeper in the middle
    k.paint(lambda P: inside(TEA_Z - 0.004 - P[:, 2], 0.0015) * inside(rad(P) - (RIM_R - 0.002), 0.0015), TEA)
    k.decal((0, 0.006, TEA_Z), (0, 0, 1), (0.084, 0.084), gleam_look, up=(0, 1, 0), eps=0.0025, rings=16, segs=72)
    # the gold rim
    k.add("rim", gold_ring((0, 0, RIM_Z), RIM_R + 0.004, 0.0062), GOLD, "body", fuse=False, surface="metal")
    # the foot: a lavender stem flaring to a gold ring underneath
    k.add("foot", lathe((0, 0, 0), (0, 0, 1), [(0.0, 0.009), (0.05, 0.009), (0.052, 0.016), (0.042, 0.022),
                                                (0.03, 0.032), (0.03, 0.046), (0.0, 0.046)], seg=56),
          FOOT, "body", fuse=False)
    k.add("footring", lathe((0, 0, 0), (0, 0, 1), [(0.0, 0.0), (0.055, 0.0), (0.058, 0.005), (0.056, 0.011),
                                                    (0.0, 0.011)], seg=56), GOLD, "body", fuse=False, surface="metal")


def handle(k):
    """The purple handle on -X: a loop from under the rim out and down, its lower end curled round a white tab."""
    pts = [(-0.108, 0.004, 0.168), (-0.142, 0.004, 0.178), (-0.172, 0.004, 0.16), (-0.178, 0.004, 0.122),
           (-0.162, 0.004, 0.09), (-0.138, 0.004, 0.078)]
    k.add("handle", outward(tube(pts, [0.0095, 0.0095, 0.0095, 0.009, 0.0095, 0.011])), TEA, "body", fuse=False,
          surface="skin")
    # the lower end's little fist round the tab
    k.add("fist", outward(ell((-0.134, -0.002, 0.08), (0.016, 0.014, 0.014))), TEA, "body", fuse=False, surface="skin")
    k.add("tab", outward(ell((-0.114, -0.004, 0.072), (0.024, 0.011, 0.0075), rot=(0, 18, 0))), "#F4F6F8", "body",
          fuse=False)


def drop(k):
    """The drop of tea hanging over the rim on the +X front, rounded below, its top running back over the rim."""
    a = 1.12
    r = radius_at(0.15) + 0.016
    c = np.array([r * math.sin(a), -r * math.cos(a), 0.15])
    def teardrop(V):
        V = V.copy()
        t = np.clip(V[:, 2], -1, 1)
        w = np.sqrt(np.clip(1 + t, 0, None)) * np.clip(1 - t, 0, None) ** 0.7 / 1.25
        V[:, 0] *= w; V[:, 1] *= w
        return V
    k.add("drop", outward(ell(c, (0.027, 0.021, 0.042), rot=(0, 0, math.degrees(a)), deform=teardrop)), TEA, "body",
          fuse=False, surface="skin")
    top = np.array([0.098 * math.sin(a), -0.098 * math.cos(a), TEA_Z - 0.002])
    k.add("dropover", outward(tube([top, top + (0.012 * math.sin(a), -0.012 * math.cos(a), 0.008),
                                    c + (0, 0, 0.034)], [0.009, 0.011, 0.011])), TEA, "body", fuse=False,
          surface="skin")


def face(k):
    # the smile
    def smile(u, v):
        c = np.tile(srgb(AQUA), (len(u), 1))
        line = -0.25 + 0.6 * u * u
        c[(np.abs(v - line) < 0.26) & (np.abs(u) < 0.9)] = srgb(TEAL_DARK)
        return c
    p, n = on_cup(0.0, MOUTH_Z)
    k.decal(p, n, (0.013, 0.007), smile, eps=0.0022, rings=10, segs=56)
    # a curl on each cheek, outside the eyes
    for s in (1, -1):
        p, n = on_cup(0.088 * s, 0.112)
        look = curl_look(AQUA, TEAL)
        k.decal(p, n, (0.018, 0.018), look if s > 0 else (lambda u, v, f=look: f(-u, v)), eps=0.0022, rings=18, segs=64)


def lay_eyes(k, state):
    for s in (1, -1):
        p, n = on_cup(EYE_X * s, EYE_Z)
        look = eye_look(state, cup_eye, srgb(AQUA), srgb(TEAL_DARK), line=0.16)
        k.decal(p, n, EYE_HALF, look if s > 0 else (lambda u, v, f=look: f(-u, v)), eps=0.0022, rings=20, segs=56)


def sinistea(src, dst):
    k = Kit(src, coat="shell")
    cup(k)
    handle(k)
    drop(k)
    face(k)
    k.build(voxel=VOXEL, smooth=3, band=0.004, wband=0.03)
    k.eye_states(lambda st: lay_eyes(k, st))
    k.export(dst)


if __name__ == "__main__":
    sinistea(*args())
