# Tadbulb sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tadbulb.py -- <the export's Tadbulb.glb> <out.glb>
#
# The design, from the references: a light bulb of a tadpole. The head IS the bulb: a round yellow dome (white spots
# on it, a big one high on the +X side, a small one on the -X side) over a grey-brown face that comes up to about the
# dome's middle at the front and down to the cheeks at the sides, and narrows below into the bulb's neck; under the
# neck an orange band like a lamp's screw cap and a short grey stub; from it a teardrop tail, a thin grey stem
# swelling into a fat drop with an orange tip, hanging down and back. Two tall oval eyes, orange-rimmed and white,
# set close; a yellow half-disc on each cheek at the yellow's edge; a wide orange smile between them.
#
# Written to be grown from (Bellibolt: sibling("tadbulb")): the line's colours, its yellow-dot marks and its smile.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, lathe, srgb, inside, smoothstep, unit, eye_look, outward, polygon, args

# --- the line's colours ---------------------------------------------------------------------------------------------
YELLOW, FACE, ORANGE, BAND = "#F4E01C", "#665A52", "#F2A23C", "#C9702E"
SPOT, WHITE, INK = "#FFFFF4", "#FFFFFF", "#1E1A18"

# --- the frame (export: 0.29 tall, from 0.034 to 0.325) -------------------------------------------------------------
C = np.array([0.0, 0.0, 0.243])            # the bulb's middle (the body bone is at 0, 0, 0.225)
R = 0.082                                  # the dome's radius
DOWN = 0.098                               # how far the bulb reaches down from its middle, into its neck
NECK = 0.5                                 # the neck's radius at its foot, a share of R
EYE_X, EYE_Z = 0.3, -0.06                  # the eyes' place on the bulb, shares of R
EYE_HALF = (0.0115, 0.0195)
VOXEL = 0.0032


def bulb(V):
    """The unit sphere made a bulb: the upper half a dome, the lower half narrowing to a neck."""
    V = V.copy()
    t = np.clip(-V[:, 2], 0, 1)
    s = 1 - (1 - NECK) * t ** 1.5
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * DOWN / R, V[:, 2])
    return V


def on_sphere(c, r, d):
    d = unit(*d)
    return np.array(c, float) + d * r, d


def smile_look(ground, ink, w=0.5, curve=0.55):
    """A wide smile, one bold line curving up at its ends (u, v in -1..1)."""
    G, K = srgb(ground), srgb(ink)
    def look(u, v):
        line = -0.3 + curve * u * u
        d = w / 2 - np.abs(v - line)
        d = np.where(np.abs(u) > 0.9, -1.0, d)
        m = np.clip(0.5 + d / 0.1, 0, 1)[:, None]
        return G[None] * (1 - m) + K[None] * m
    return look


def dot_look(color):
    """A round dot of one colour (a cheek, an eye of Bellibolt's): the patch is the dot."""
    return color


def bulb_eye(rim=0.34):
    """A tall oval eye: an orange rim round a white middle."""
    W, O = np.ones(3), srgb(ORANGE)
    def eye(u, v):
        r = np.hypot(u, v)
        m = np.clip(0.5 + (r - (1 - rim)) / 0.1, 0, 1)[:, None]
        return W[None] * (1 - m) + O[None] * m
    return eye


def tadbulb(src, out):
    k = Kit(src, coat="shell")

    # The bulb, yellow; its face painted grey under an edge that is high at the front and low at the sides and back
    k.add("bulb", ell(C, (R, R * 0.98, R), deform=bulb), YELLOW, "body")
    def face(P):
        q = P - C
        az = np.abs(np.arctan2(q[:, 0], -q[:, 1]))
        edge = -0.01 - 0.045 * smoothstep(0.9, 2.0, az) - 0.022 * smoothstep(1.7, 3.0, az)
        return inside(q[:, 2] - edge, 0.0022)
    k.paint(face, FACE)
    # over its front a patch whose top is the edge (paint frays there on the remeshed bulb)
    fp, fn = on_sphere(C, R, unit(0, -0.97, -0.25))
    k.decal(fp, fn, (0.083, 0.07), FACE, eps=0.0018, rings=30, segs=120,
            outline=polygon([(-0.8, -0.5), (-0.74, 0.22), (-0.45, 0.53), (0, 0.6), (0.45, 0.53), (0.74, 0.22),
                             (0.8, -0.5), (0.6, -0.9), (0, -1), (-0.6, -0.9)]))

    # The screw cap under the neck, and a short stub under it
    zf = C[2] - DOWN
    k.add("cap", lathe((0, 0, zf + 0.012), (0, 0, -1),
                       [(0, -0.002), (NECK * R * 1.02, -0.002), (NECK * R * 1.08, 0.008), (NECK * R * 0.92, 0.02),
                        (NECK * R * 0.55, 0.026), (0, 0.026)]), BAND, "body", fuse=False, surface="shell")
    k.add("stub", tube([(0, 0.0, zf + 0.004), (0, 0.006, zf - 0.016)], [0.022, 0.016]), FACE, "body")

    # The teardrop tail: a thin stem swelling into a fat drop, back, down and out to +X, flat with its face to the
    # cameras; the drop's tip orange
    root = np.array([0.0, 0.008, zf - 0.016])
    pts = [root, root + (0.012, 0.028, -0.022), root + (0.032, 0.058, -0.048), root + (0.048, 0.072, -0.064)]
    rad = [0.012, 0.014, 0.027, 0.033]
    d = unit(*(pts[-1] - pts[0]))
    across = unit(0.5, 0.86, 0.0); across = across - d * (across @ d); across /= np.linalg.norm(across)
    k.add("tail", blade(pts, rad, across, 0.62), FACE, "tail")
    L = np.linalg.norm(pts[-1] - pts[0]) + rad[-1]
    def tip(P):
        q = P - root
        t = q @ d
        side = q @ np.cross(d, across)
        return inside(0.6 * L + 0.008 * np.sin(side * 160) - t, 0.002) * (P[:, 2] < zf - 0.02)
    k.paint(tip, ORANGE, on=["tail"])

    # White spots on the dome: a big one high on +X, a small one on -X, one behind
    for at, half in ((unit(0.45, -0.32, 0.83), (0.03, 0.022)), (unit(-0.88, -0.22, 0.42), (0.014, 0.017)),
                     (unit(-0.2, 0.75, 0.63), (0.018, 0.014))):
        p, n = on_sphere(C, R, at)
        k.decal(p, n, half, SPOT, eps=0.0022, rings=10, segs=40)

    # The cheeks: yellow half-discs at the yellow's edge
    p, n = on_sphere(C, R * 0.97, unit(0.78, -0.56, -0.28))
    k.decal(p, n, (0.0125, 0.0135), YELLOW, eps=0.0024, rings=10, segs=40, mirror=True)
    # The smile between them
    p, n = on_sphere(C, R * 0.93, unit(0, -0.93, -0.36))
    k.decal(p, unit(0, -1, -0.32), (0.042, 0.016), smile_look(FACE, ORANGE), eps=0.0028, rings=18, segs=64)

    k.build(voxel=VOXEL, band=0.003)

    eye = bulb_eye()
    def eyes(state):
        f = eye_look(state, eye, srgb(FACE), srgb(ORANGE), line=0.22)
        for side in (1, -1):
            dd = unit(EYE_X * side, -math.sqrt(1 - EYE_X ** 2 - EYE_Z ** 2), EYE_Z)
            p, n = on_sphere(C, R, dd)
            col = f if side > 0 else (lambda u, v, f=f: f(-u, v))
            k.decal(p, unit(*(n * np.array([0.8, 1, 0.6]))), EYE_HALF, col, eps=0.003, rings=20, segs=56)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    tadbulb(*args())
