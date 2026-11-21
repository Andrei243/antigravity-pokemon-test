# Elgyem sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/elgyem.py -- <the export's Elgyem.glb> <out.glb>
#
# From the references: a small teal alien that is mostly head. A great egg of a head, broad at its top and tipped
# back, with a dark brain mark over its front (a Y in the middle and a stroke each side, slanting in from the top and
# then straight down) and two thin lines round its lower part; two round green eyes (a dark rim, a glint) low on the
# head, close together; no mouth. Under it a little peg of a body on two short stubby legs, and two short thin arms
# reaching forward and down to broad round hands, each with three round lights at its fingertips: green, red, yellow.
# The skeleton: body (0, 0, 0.11), head (0, -0.01, 0.16), armR/L (+-0.035, 0, 0.12), legR/L (+-0.025, 0, 0.06).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args

TEAL, LINE, BRAIN = "#93D3CC", "#4F8F8A", "#363A40"
GREEN, RIM, WHITE = "#3BB36E", "#17603C", "#F4FFF8"
LIGHTS = {"green": "#2FA85A", "red": "#E8505A", "yellow": "#F2D23A"}

# --- the frame ------------------------------------------------------------------------------------------------------
HC, HRAD, HTILT = np.array([0.0, 0.014, 0.262]), np.array([0.09, 0.086, 0.14]), -14.0
VOXEL = 0.0042
EYE_ARC, EYE_Z, EYE_HALF = 0.041, -0.093, (0.022, 0.025)      # EYE_Z in the head's own frame


def rot(deg):
    return np.array(Euler((math.radians(deg), 0, 0)).to_matrix())


def egg(V):
    """Broad at the top, narrower toward the bottom where the eyes are."""
    V = V.copy()
    V[:, :2] *= (1 + 0.13 * V[:, 2])[:, None]
    return V


def local(P, c=HC, tilt=HTILT):
    """Points in the head's own frame (untilted)."""
    return (P - c) @ rot(tilt)


def on_head(arc, zl, c=HC, r=HRAD, tilt=HTILT, deform_k=0.13):
    """A point on the head's surface at an arc round it and a height in its own frame, and the way out there."""
    zz = zl / r[2]
    s = 1 + deform_k * zz
    th = arc / (r[0] * s)
    rho = math.sqrt(max(1 - zz * zz, 1e-4))
    p = np.array([math.sin(th) * rho * r[0] * s, -math.cos(th) * rho * r[1] * s, zl])
    n = unit(math.sin(th) / (r[0] * s), -math.cos(th) / (r[1] * s), zz / r[2] * 1.1)
    R = rot(tilt)
    return c + R @ p, R @ n


def strokes(segs, half, c=HC, r=HRAD, tilt=HTILT):
    """A paint mask of strokes on the head's front: segs of (arc, height) pairs in the head's own frame."""
    def mask(P):
        q = local(P, c, tilt)
        front = q[:, 1] < 0
        a = np.arctan2(q[:, 0], -q[:, 1]) * r[0]
        Q = np.stack([a, q[:, 2]], -1)
        d = np.full(len(P), 9.0)
        for p0, p1 in segs:
            p0, p1 = np.array(p0), np.array(p1)
            e = p1 - p0
            t = np.clip(((Q - p0) @ e) / (e @ e), 0, 1)
            d = np.minimum(d, np.linalg.norm(Q - (p0 + t[:, None] * e), axis=1))
        return inside(d - half, 0.0026) * front
    return mask


BRAIN_SEGS = [((0, -0.064), (0, 0.006)), ((0, 0.006), (0.03, 0.054)), ((0, 0.006), (-0.03, 0.054)),
              ((0.084, 0.05), (0.05, 0.0)), ((0.05, 0.0), (0.05, -0.056)),
              ((-0.084, 0.05), (-0.05, 0.0)), ((-0.05, 0.0), (-0.05, -0.056))]


def hand_frame(c, n, up=(0, 0, 1.0)):
    n = unit(*n)
    ex = np.cross(np.array(up), n); ex /= np.linalg.norm(ex)
    ez = np.cross(n, ex)
    return np.array(c, float), ex, n, ez


def hand(k, name, bone, frame, radii, colour, side):
    """A broad round flat hand: an ellipsoid laid in its frame (width ex, thin along n, height ez)."""
    c, ex, n, ez = frame
    V, F = ell((0, 0, 0), radii)
    k.add(name, (V @ np.stack([ex, n, ez], axis=0) + c, F), colour, bone)


def lights(k, bone, frame, radii, spots, r):
    """Round lights set into the hand's front: spots of (colour, u outward, v up)."""
    c, ex, n, ez = frame
    for colour, u, v in spots:
        w = math.sqrt(max(1 - u * u - v * v, 0.05))
        p = c + ex * u * radii[0] + ez * v * radii[2] + n * radii[1] * w * 0.85
        k.add("light" + colour, ell(p, (r, r, r), sub=3), LIGHTS[colour], bone, fuse=False, surface="skin")


def round_eye(rim, iris, glint):
    def look(u, v):
        rho = np.hypot(u, v)
        c = np.tile(srgb(rim), (len(u), 1))
        c[rho < 0.78] = srgb(iris)
        c[((u - 0.12) ** 2 + (v - 0.3) ** 2) < 0.2 ** 2] = srgb(glint)
        return c
    return look


def elgyem(src, out):
    k = Kit(src, coat="fur")
    k.add("head", ell(HC, HRAD, rot=(HTILT, 0, 0), deform=egg), TEAL, "head")
    k.add("body", ell((0, 0.004, 0.1), (0.036, 0.033, 0.05)), TEAL, "body")
    k.add("hips", ell((0, 0.004, 0.072), (0.042, 0.036, 0.03)), TEAL, "body")
    # short stubby legs on round little feet
    for s, bone in ((1, "legR"), (-1, "legL")):
        k.add("leg" + bone[-1], tube([(0.024 * s, 0.002, 0.066), (0.027 * s, -0.002, 0.02)], [0.02, 0.021]), TEAL, bone)
        k.add("foot" + bone[-1], ell((0.028 * s, -0.008, 0.013), (0.024, 0.028, 0.013)), TEAL, bone)
    # short arms reaching forward and down to broad round hands, the lights at their fingertips
    for s, bone in ((1, "armR"), (-1, "armL")):
        hc = np.array([0.094 * s, -0.052, 0.066])
        k.add("arm" + bone[-1], tube([(0.03 * s, 0.0, 0.125), (0.062 * s, -0.025, 0.1), (hc[0] * 0.92, hc[1] + 0.012, hc[2] + 0.012)],
                                     [0.012, 0.011, 0.012]), TEAL, bone)
        fr = hand_frame(hc, (0.4 * s, -0.75, 0.55))
        HR_ = (0.037, 0.015, 0.031)
        hand(k, "hand" + bone[-1], bone, fr, HR_, TEAL, s)
        c, ex, n, ez = fr
        ex = ex * (1 if (ex[0] * s) > 0 else -1)                 # outward
        lights(k, bone, (c, ex, n, ez), HR_, [("green", -0.3, 0.32), ("red", 0.62, -0.32), ("yellow", 0.08, -0.72)],
               0.0085)
    # two thin lines round the head's lower part, and the brain mark over its front
    for zl in (-0.067, -0.112):
        k.paint(lambda P, zl=zl: inside(np.abs(local(P)[:, 2] - zl) - 0.0022, 0.0012), LINE, on=["head"])
    k.paint(strokes(BRAIN_SEGS, 0.0062), BRAIN, on=["head"])

    k.build(voxel=VOXEL, smooth=3, band=0.005)

    look_open = round_eye(RIM, GREEN, WHITE)

    def eyes(state):
        for side in (1, -1):
            p, n = on_head(EYE_ARC * side, EYE_Z)
            look = eye_look(state, look_open, srgb(TEAL), srgb(RIM), line=0.17)
            col = look if side == 1 else (lambda u, v, f=look: f(-u, v))
            k.decal(p, n, EYE_HALF, col, eps=0.003, rings=20, segs=56)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    elgyem(*args())
