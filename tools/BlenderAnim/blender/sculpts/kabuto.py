# Kabuto sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/kabuto.py -- <the export's Kabuto.glb> <out.glb>
# Written to be grown from: kabutops.py imports its colours and its way of making the shell's hard edge and the eyes.
#
# From the references: a round brown dome of a shell, nearly as tall as it is wide, coming down to the ground at its
# sides and back and arching up over a wide opening at the front. Two small black dots on its top, toward the front
# (the pictures show small dots, not large eyes: they are drawn a little larger here so that they read). In the
# opening a black underside, darker blue toward its lower lip, with two big glowing pink eyes, pale in the middle.
# Pale yellow legs curl out from under the front edge, each a hooked claw; the hind pair barely shows at the sides.
# Skeleton: body (0, 0, 0.12), head (0, -0.04, 0.08), legs (+-0.1, -0.08, 0.06), hind (+-0.1, 0.04, 0.06).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args

BROWN, UNDER, NAVY, CLAW, DOT = "#B0783A", "#141418", "#1C2A56", "#EAC85E", "#141416"
PINK, PINKHI = "#F06C88", "#FFD8E0"

DC, DR = np.array([0.0, 0.01, 0.055]), np.array([0.178, 0.185, 0.205])


def dome(c, r, rim, na=160, nt=40):
    """
    A hard shell (cranidos.py's): the ellipsoid from its top down to a rim whose unit height on each meridian is
    rim(a) (a the azimuth: x = sin a, y = cos a, so a = 0 is the back and a = pi the front), then a lip turned in.
    """
    rings = []
    for t in np.linspace(0, 1, nt):
        ring = []
        for a in np.linspace(0, 2 * math.pi, na, endpoint=False):
            th = math.acos(float(np.clip(rim(a), -0.99, 0.99))) * t
            ring.append((math.sin(th) * math.sin(a), math.sin(th) * math.cos(a), math.cos(th)))
        rings.append(np.array(ring))
    last = rings[-1]
    for sc, dz in ((0.94, 0.0), (0.84, 0.04), (0.7, 0.1)):
        rings.append(last * np.array([sc, sc, 1]) + np.array([0, 0, dz]))
    V = [np.array([0, 0, 1.0])]
    for ring in rings[1:]:
        V.extend(ring)
    V = np.array(V)
    F = [[0, 1 + j, 1 + (j + 1) % na][::-1] for j in range(na)]
    for i in range(len(rings) - 2):
        a0, b0 = 1 + i * na, 1 + (i + 1) * na
        for j in range(na):
            F.append([a0 + j, b0 + j, b0 + (j + 1) % na, a0 + (j + 1) % na][::-1])
    return V * np.array(r) + np.array(c), F


def rim(a):
    """Down near the ground at the back and the sides, arched up over the face at the front."""
    d = abs((a + math.pi) % (2 * math.pi) - math.pi)         # 0 at the back, pi at the front
    return -0.2 + 0.72 * smoothstep(1.85, 2.75, d)


def on_dome(u, lift=0.0):
    u = unit(*u)
    p = DC + DR * u
    n = unit(*((p - DC) / DR ** 2))
    return p + n * lift, n


def dot(k, u, half=0.016):
    """A black dot on the dome: a lens set on its surface along the normal."""
    p, n = on_dome(u)
    a = np.cross(n, (0, 0, 1.0)); a /= np.linalg.norm(a)
    b = np.cross(n, a)
    V, F = ell((0, 0, 0), (half, half, 0.006), sub=3)
    V = V @ np.stack([a, b, n]) + p + n * 0.002
    k.add("dot", (V, F), DOT, "body", fuse=False, mirror=True, surface="shell")


def claw(k, pts, radii, bone, name):
    k.add(name, tube(pts, radii, seg=18), CLAW, bone, fuse=False, mirror=True, surface="shell")


EYE_C, EYE_HALF = np.array([0.056, -0.17, 0.1]), 0.037


def eye_open(u, v):
    rho = np.sqrt(u * u + v * v)
    c = np.tile(srgb(UNDER), (len(u), 1))
    t = np.clip(rho / 0.9, 0, 1)[:, None]
    c[rho < 0.9] = (srgb(PINKHI) * (1 - t) + srgb(PINK) * t)[rho < 0.9]
    return c


def eyes(k, state, c=EYE_C, half=EYE_HALF, d=(0.28, -1.0, 0.1)):
    lk = eye_look(state, eye_open, srgb(UNDER), srgb(PINK), line=0.18)
    for s in (1, -1):
        col = lk if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(lk)
        k.decal(c * (s, 1, 1), unit(s * d[0], d[1], d[2]), (half, half), col, eps=0.004, rings=20, segs=56)


def kabuto(src, out):
    k = Kit(src, coat="shell")
    k.add("dome", dome(DC, DR, rim), BROWN, "body", fuse=False, surface="shell")
    dot(k, (0.42, -0.5, 0.76))
    # the underside: black, its front flattened for the eyes, navy toward its lower lip
    k.add("under", ell((0, 0.0, 0.085), (0.155, 0.16, 0.085)), UNDER, "body")
    k.add("face", ell((0, -0.1, 0.09), (0.13, 0.075, 0.07)), UNDER, "head")
    k.add("facefront", ell((0, -0.125, 0.098), (0.12, 0.052, 0.062)), UNDER, "head")
    k.paint(lambda P: inside(P[:, 2] - 0.06, 0.004) * inside(P[:, 1] + 0.08, 0.01), NAVY)
    # the fore legs: out from under the front edge, down and hooked inward
    claw(k, [(0.085, -0.09, 0.07), (0.12, -0.15, 0.06), (0.135, -0.19, 0.035), (0.115, -0.215, 0.012),
             (0.085, -0.215, 0.012)], [0.024, 0.022, 0.019, 0.014, 0.006], "leg{s}", "fore")
    claw(k, [(0.06, -0.1, 0.05), (0.075, -0.15, 0.04), (0.07, -0.18, 0.016), (0.05, -0.185, 0.012)],
         [0.017, 0.016, 0.012, 0.005], "leg{s}", "foretoe")
    # the hind legs at the sides, barely out
    claw(k, [(0.1, 0.03, 0.06), (0.16, 0.0, 0.04), (0.185, -0.03, 0.014), (0.17, -0.06, 0.01)],
         [0.02, 0.018, 0.013, 0.005], "hind{s}", "hind")
    k.build(voxel=0.005, smooth=4, band=0.006)
    k.eye_states(lambda st: eyes(k, st))
    k.export(out)


if __name__ == "__main__":
    kabuto(*args())
