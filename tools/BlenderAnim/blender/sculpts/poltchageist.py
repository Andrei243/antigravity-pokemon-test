# Poltchageist sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/poltchageist.py -- <the export's Poltchageist.glb> <out.glb>
#
# The design, from the references: a tea caddy shaped like an egg, a little wider above, cream below and glazed black
# over its shoulders and back, the black coming down the sides to the eyes' height and leaving a cream arch up the
# front to just under the lid; a dark brown neck under a flat cream lid with a round knob. Painted on the cream front
# is its face: two tall ovals each drawn as a bold line with a second loop inside its lower part, two drips under the
# one on +X, a small smile, and three black dots in a triangle on the arch above. A hexagon seal on its +X side (dark
# green, a yellow oval in it). Green matcha oozes from under the lid down its front -X shoulder, and a green hand of
# matcha comes out of its +X side holding up a bamboo tea scoop. (The task's text spoke of a swirl of matcha rising
# from the caddy as a ghost: the pictures show the matcha only as the ooze under the lid and the hand with the scoop.
# The loose splash and droplets the art scatters round it are left out: they would float.)
# The skeleton: body (0, 0, 0.121), head (0, 0, 0.201) for the lid, armR (0.06, 0, 0.161) for the hand and scoop.
# Written to be grown from: Sinistcha imports its colours, its loops, its seal and its matcha.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, lathe, outward, polygon, slab, hull, args, sibling

si = sibling("sinistea")

# --- the line's colours -------------------------------------------------------------------------------------------
CREAM, GLAZE, INK, BROWN = "#EFE8D2", "#2B2729", "#1C1A1C", "#6A4426"
MATCHA, MATCHA_DARK = "#78C063", "#4F9A45"
BAMBOO, BAMBOO_DARK = "#D9CF8C", "#B8A862"
SEAL_GREEN, SEAL_DARK, SEAL_YELLOW = "#4FA853", "#2E6B36", "#E9D84E"

# --- the frame ----------------------------------------------------------------------------------------------------
JAR = [(0.034, 0.03), (0.054, 0.038), (0.066, 0.06), (0.073, 0.095), (0.075, 0.135), (0.072, 0.172),
       (0.063, 0.198), (0.047, 0.212), (0.038, 0.216)]
JAR_TOP = 0.218
EYE_X, EYE_Z, EYE_HALF = 0.029, 0.1, (0.022, 0.03)
MOUTH_Z = 0.07
VOXEL = 0.0026


def on_jar(a, z, profile=JAR, inset=0.002):
    """A point on the side at azimuth a (from the front, +X positive) and height z, and the normal there."""
    r = si.radius_at(z, profile)
    dz = 0.004
    slope = (si.radius_at(z + dz, profile) - si.radius_at(z - dz, profile)) / (2 * dz)
    n = unit(math.sin(a), -math.cos(a), -slope)
    return np.array([r * math.sin(a), -r * math.cos(a), z]) - n * inset, n


def glaze_line(a):
    """The height of the black glaze's lower edge round the caddy: low at the sides and back, an arch up the front."""
    f = np.clip(1 - (np.abs(a) / 0.95) ** 2, 0, 1)
    return 0.094 + 0.088 * f ** 0.6 + 0.004 * np.sin(5 * a)


def loop_eye(u, v):
    """The painted eye: a bold oval line with a second loop inside its lower part, cream between."""
    c = np.tile(srgb(CREAM), (len(u), 1))
    q = np.hypot(u, v)
    c[q > 0.74] = srgb(INK)
    qi = np.hypot((u + 0.02) / 0.36, (v + 0.16) / 0.52)
    c[np.abs(qi - 1) < 0.32] = srgb(INK)
    return c


def seal_look(u, v):
    """The hexagon seal: a dark green rim, green, a yellow oval in the middle."""
    c = np.tile(srgb(SEAL_GREEN), (len(u), 1))
    c[np.hypot(u, v) > 0.78] = srgb(SEAL_DARK)
    c[np.hypot(u / 0.42, v / 0.58) < 1] = srgb(SEAL_YELLOW)
    return c


HEXAGON = polygon([(math.cos(math.pi / 6 + i * math.pi / 3), math.sin(math.pi / 6 + i * math.pi / 3))
                   for i in range(6)])


def matcha_strand(k, name, pts, radii, bone="body"):
    k.add(name, outward(tube(pts, radii)), MATCHA, bone, fuse=False, surface="skin")


# --- the parts ----------------------------------------------------------------------------------------------------
def jar(k):
    k.add("jar", si.cup_solid(JAR, JAR_TOP), CREAM, "body")
    az = lambda P: np.arctan2(P[:, 0], -P[:, 1])
    k.paint(lambda P: inside(glaze_line(az(P)) - P[:, 2], 0.0016), GLAZE)
    k.add("neck", lathe((0, 0, 0), (0, 0, 1), [(0.0, 0.208), (0.043, 0.208), (0.046, 0.216), (0.043, 0.226),
                                                (0.0, 0.226)], seg=56), BROWN, "body", fuse=False)


def lid(k):
    k.add("lid", lathe((0, 0, 0), (0, 0, 1), [(0.0, 0.222), (0.05, 0.222), (0.054, 0.228), (0.052, 0.236),
                                               (0.042, 0.241), (0.0, 0.242)], seg=56), CREAM, "head", fuse=False)
    k.add("knob", lathe((0, 0, 0), (0, 0, 1), [(0.0, 0.24), (0.013, 0.24), (0.012, 0.25), (0.025, 0.254),
                                                (0.028, 0.262), (0.021, 0.271), (0.0, 0.273)], seg=48), CREAM,
          "head", fuse=False)


def ooze(k):
    """Matcha oozing from under the lid down the front -X shoulder: a lip of it and two runs."""
    for a, z1, r in ((-0.75, 0.152, 0.0068), (-0.3, 0.186, 0.006)):
        top, _ = on_jar(a, 0.21, inset=-0.004)
        mid, _ = on_jar(a + 0.06, (0.21 + z1) / 2, inset=-0.004)
        end, _ = on_jar(a + 0.1, z1, inset=-0.003)
        matcha_strand(k, f"run{a}", [top, mid, end], [r, r * 0.9, r * 1.15])
        k.add(f"drip{a}", outward(ell(end, (r * 1.3, r * 1.1, r * 1.4))), MATCHA, "body", fuse=False, surface="skin")
    lip = [on_jar(a, 0.211, inset=-0.005)[0] for a in np.linspace(-1.05, -0.1, 5)]
    matcha_strand(k, "lip", lip, [0.0055, 0.0068, 0.0068, 0.0062, 0.005])


def hand_and_scoop(k):
    """A green hand of matcha out of the +X side, holding a bamboo scoop up and a little forward."""
    root, _ = on_jar(1.45, 0.15, inset=0.004)
    matcha_strand(k, "arm", [root, (0.09, -0.012, 0.162), (0.104, -0.024, 0.18)], [0.013, 0.012, 0.012], "armR")
    k.add("hand", outward(ell((0.108, -0.028, 0.185), (0.017, 0.015, 0.016))), MATCHA, "armR", fuse=False,
          surface="skin")
    a, b = np.array([0.086, -0.014, 0.12]), np.array([0.142, -0.046, 0.27])
    d = unit(*(b - a))
    side = unit(*np.cross(d, (0, -1.0, 0)))
    w = 0.008
    outline = [(-w, 0), (w, 0), (w, 0.15), (0.6 * w, 0.162), (-0.4 * w, 0.164), (-w, 0.156)]
    k.add("scoop", slab(outline, a, side, d, 0.009), BAMBOO, "armR", fuse=False)
    node = a + d * 0.05
    k.add("node", hull([node + side * s1 * 0.0095 + np.cross(side, d) * s2 * 0.0062 + d * s3 * 0.004
                        for s1 in (-1, 1) for s2 in (-1, 1) for s3 in (-1, 1)]), BAMBOO_DARK, "armR", fuse=False)
    # the fingers wound round the scoop, and a drip off them
    for i, t in enumerate((0.055, 0.075)):
        p = a + d * (0.03 + t)
        k.add(f"finger{i}", outward(ell(p, (0.013, 0.011, 0.009), rot=(0, -20, 0))), MATCHA, "armR", fuse=False,
              surface="skin")
    matcha_strand(k, "fingerdrip", [(0.118, -0.03, 0.18), (0.124, -0.034, 0.16)], [0.006, 0.0075], "armR")


def face(k):
    def smile(u, v):
        c = np.tile(srgb(CREAM), (len(u), 1))
        line = -0.3 + 0.62 * u * u
        c[(np.abs(v - line) < 0.27) & (np.abs(u) < 0.9)] = srgb(INK)
        return c
    p, n = on_jar(0.0, MOUTH_Z)
    k.decal(p, n, (0.013, 0.0065), smile, eps=0.0022, rings=10, segs=56)
    # three dots in a triangle on the arch
    for x, z in ((-0.008, 0.152), (0.008, 0.152), (0.0, 0.139)):
        p, n = on_jar(math.asin(x / 0.075), z)
        k.decal(p, n, (0.0062, 0.0062), INK, eps=0.0022, rings=6, segs=28)
    # two drips under the +X eye
    for x in (0.022, 0.034):
        p, n = on_jar(math.asin((EYE_X + x - 0.028) / 0.072), EYE_Z - 0.039)
        k.decal(p, n, (0.0034, 0.0075), INK, eps=0.0022, rings=6, segs=28)
    # the seal on the +X side
    p, n = on_jar(1.25, 0.152)
    k.decal(p, n, (0.019, 0.019), seal_look, eps=0.0024, outline=HEXAGON, rings=14, segs=60)


def lay_eyes(k, state):
    for s in (1, -1):
        p, n = on_jar(math.asin(EYE_X * s / 0.073), EYE_Z)
        look = eye_look(state, loop_eye, srgb(CREAM), srgb(INK), line=0.17)
        k.decal(p, n, EYE_HALF, look if s > 0 else (lambda u, v, f=look: f(-u, v)), eps=0.0022, rings=20, segs=56)


def poltchageist(src, dst):
    k = Kit(src, coat="shell")
    jar(k)
    lid(k)
    ooze(k)
    hand_and_scoop(k)
    face(k)
    k.build(voxel=VOXEL, smooth=3, band=0.004, wband=0.03)
    k.eye_states(lambda st: lay_eyes(k, st))
    k.export(dst)


if __name__ == "__main__":
    poltchageist(*args())
