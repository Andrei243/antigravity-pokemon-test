# Kakuna sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/kakuna.py -- <the export's Kakuna.glb> <out.glb>
#
# From the references: a golden chrysalis standing on its point. A smooth dome of a head with a hard brim over a
# flat underside; the eyes on the dome's lower front, just over the brim, black slanting triangles with a small white
# glint, their outer corners high (a scowl). Under the brim a beak of a point in the middle, a shoulder plate at each
# side and a folded arm plate below it running in and down, and a long pointed plate down the front's middle. The
# lower body tapers to a point, ringed by curved seams. All one gold, the plates shaded flat.
# Skeleton: body (0, 0, 0.3) alone.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, srgb, inside, smoothstep, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="shell")

GOLD, SEAM, INK = "#E8C64A", "#A8862E", "#16120E"

# --- the frame -------------------------------------------------------------------------------------------------------
DOME_C, DOME_R = np.array([0.0, -0.005, 0.395]), np.array([0.158, 0.152, 0.165])
BODY_C, BODY_R = np.array([0.0, 0.005, 0.235]), np.array([0.132, 0.122, 0.21])


def flat_under(V):
    """The dome's lower half pressed up flat: a brim over a flat underside."""
    V = V.copy()
    lo = V[:, 2] < 0
    V[lo, 2] *= 0.32
    return V


def taper(V):
    """The body narrowing below its middle to a blunt point."""
    V = V.copy()
    t = np.clip(-V[:, 2], 0, 1)
    f = 1 - 0.62 * t ** 1.4
    V[:, 0] *= f; V[:, 1] *= f
    return V


def surf(angle, z, lift):
    """A point of the trunk's surface at an angle round from the front (degrees, + toward +X) and a height, lifted."""
    q = np.clip((z - BODY_C[2]) / BODY_R[2], -0.999, 0.999)
    s = math.sqrt(1 - q * q) * (1 - 0.62 * max(-q, 0) ** 1.4)
    a = math.radians(angle)
    p = BODY_C + np.array([BODY_R[0] * s * math.sin(a), -BODY_R[1] * s * math.cos(a), z - BODY_C[2]])
    n = unit(math.sin(a) / BODY_R[0], -math.cos(a) / BODY_R[1], 0)
    return p + n * lift


def add_body():
    k.add("dome", ell(DOME_C, DOME_R, deform=flat_under, sub=5), GOLD, "body")
    k.add("trunk", ell(BODY_C, BODY_R, deform=taper, sub=5), GOLD, "body")
    # the body widest right under the brim, behind and at the sides, so no neck shows under the dome
    k.add("shoulders", ell((0, 0.03, 0.315), (0.145, 0.115, 0.06)), GOLD, "body")


def plates():
    """The hard plates under the brim: a beak, the shoulders, the folded arms and the long middle plate."""
    def plate(name, outline, lift, mirror=False, tip=None):
        # a low cut stone: the outline and its edges' middles lifted, and its middle lifted a little more, so it is a few
        # large facets meeting at a ridge, standing clear of the body's curve
        o = np.array(outline, float)
        mids = (o + np.roll(o, -1, axis=0)) / 2
        cen = o.mean(axis=0)
        pts = ([surf(a, z, lift) for a, z in o] + [surf(a, z, lift) for a, z in mids] + [surf(cen[0], cen[1], lift * 1.35)]
               + [surf(a, z, -0.012) for a, z in o])
        if tip is not None:
            pts.append(surf(tip[0], tip[1], tip[2]))
        k.add(name, hull(pts, merge=2.0), GOLD, "body", fuse=False, mirror=mirror, surface="shell")
    # lapels hugging the body, layered: the arms under the shoulders, the middle plate over both
    plate("arm", [(10, 0.282), (70, 0.284), (58, 0.232), (12, 0.152)], 0.02, mirror=True)
    plate("shoulder", [(14, 0.338), (86, 0.334), (88, 0.29), (60, 0.27), (20, 0.29)], 0.03, mirror=True)
    plate("middle", [(-11, 0.33), (11, 0.33), (10, 0.23), (0, 0.105), (-10, 0.23)], 0.034)
    plate("beak", [(-14, 0.348), (14, 0.348), (0, 0.282)], 0.042, tip=(0, 0.335, 0.066))


def seams():
    """Curved seams round the lower body, bowed up at the front middle."""
    def m(P):
        q = P - BODY_C
        ring = np.zeros(len(P))
        for z0 in (0.135, 0.098, 0.064):
            z = z0 + 0.018 * smoothstep(0.0, -0.1, q[:, 1]) - 1.0 * q[:, 0] ** 2
            ring = np.maximum(ring, inside(np.abs(P[:, 2] - z) - 0.0028, 0.0018))
        return ring * (P[:, 2] < 0.17)
    k.paint(m, SEAM, on=["trunk"])


# --- the eyes: black slanting triangles with a white glint, outer corners high --------------------------------------
CK, CW, CL = srgb(INK), np.array([1.0, 1.0, 1.0]), srgb(GOLD)
EYE_SHAPE = [(1.0, 0.62), (0.4, 0.35), (-0.3, 0.08), (-0.95, -0.12), (-0.55, -0.55), (0.0, -0.72), (0.55, -0.55),
             (0.9, -0.05)]


def eye_open(u, v):
    c = np.tile(CK, (len(u), 1))
    c[((u + 0.38) / 0.2) ** 2 + ((v + 0.25) / 0.24) ** 2 < 1] = CW
    return c


def eye_frame(side, d=(0.5, -0.84, -0.1)):
    de = unit(side * d[0], d[1], d[2])
    p = DOME_C + de / math.sqrt(((de / DOME_R) ** 2).sum())
    n = (p - DOME_C) / DOME_R ** 2
    return p + np.array([0, 0, 0.04]), n / np.linalg.norm(n)


EYE_HALF = (0.045, 0.034)
def eyes(state):
    look = eye_look(state, eye_open, CL, CK, line=0.17)
    for side in (1, -1):
        c, n = eye_frame(side)
        col = look if side == 1 else (lambda a, b, f=look: f(-a, b))
        shape = EYE_SHAPE if side == 1 else [(-a, b) for a, b in EYE_SHAPE]
        k.decal(c, n, EYE_HALF, col, eps=0.003, rings=20, segs=56, outline=polygon(shape))


if __name__ == "__main__":
    add_body()
    seams()
    plates()
    k.build(voxel=0.006, smooth=4)
    k.eye_states(eyes)
    k.export(out)
