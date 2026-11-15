# Cherrim (Sunshine Form) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cherrimsunshine.py -- <the export's Cherrim-Sunshine.glb> <out.glb>
# Grown from cherrim.py (the Overcast Form): the bud opened in the sun. Its pink body grown into a round yellow face
# (the flower's middle) with Cherrim's eyes made pink-red and a wide open smiling mouth; Cherubi's two cherries sitting
# on top of the face at its sides; round the face from behind a great blossom of five pale pink petals, each notched at
# its tip and cupped a little forward, a deeper pink on their backs; under the face a small pink body with two little
# pointed petals for arms, a green leaf under the chin and two small yellow feet.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, slab, hull, srgb, unit, eye_look, args, axes

src, out = args()
k = Kit(src, coat="leaf")

PETAL, PETALBACK, YELLOW, PINK, GREEN = "#F6BED0", "#E890AE", "#F6DE4A", "#F3A6BE", "#3DA84A"
CHERRY, CHERRYDARK = "#E8485E", "#B02A44"
EYERED, EYEDARK, MOUTH, TONGUE = "#E04870", "#8A1A3A", "#8E1E36", "#F07890"

H = np.array([0.0, -0.03, 0.3])            # the face's middle: the flower's heart
HR = np.array([0.175, 0.125, 0.158])
P0 = np.array([0.0, 0.085, 0.355])         # the blossom's middle, behind the face


def on_face(d):
    d = unit(*d)
    p = H + d / math.sqrt(((d / HR) ** 2).sum())
    n = (p - H) / HR ** 2
    return p, n / np.linalg.norm(n)


def pointed(c, d, r):
    """A small petal: an ellipsoid along d that tapers to a point at its far end."""
    a, b = axes(d)
    d = unit(*d)
    def taper(V):
        V = V.copy()
        t = np.clip((V[:, 2] + 1) / 2, 0, 1)
        V[:, 0] *= 1 - 0.85 * t ** 1.5; V[:, 1] *= 1 - 0.85 * t ** 1.5
        return V
    V, F = ell((0, 0, 0), r, deform=taper)
    return V @ np.stack([a, b, d]) + np.array(c, float), F


# --- the yellow face and the small pink body under it ---------------------------------------------------------------
k.add("face", ell(H, HR), YELLOW, "body")
k.add("torso", ell((0, -0.03, 0.115), (0.075, 0.062, 0.06)), PINK, "body")
k.add("arm", pointed((0.06, -0.06, 0.115), (1.0, -0.45, -0.35), (0.028, 0.02, 0.055)), PINK, "body", mirror=True)
k.add("foot", ell((0.05, -0.04, 0.032), (0.034, 0.045, 0.032)), YELLOW, "leg{s}", mirror=True)
# the green leaf under the chin
k.add("leaf", hull([(0.024, -0.112, 0.155), (-0.024, -0.112, 0.155), (0.0, -0.118, 0.112), (0.0, -0.096, 0.15),
                    (0.0, -0.1, 0.12)]), GREEN, "body", fuse=False)

# --- the two cherries on top of the face -----------------------------------------------------------------------------
k.add("cherry", ell((0.135, -0.035, 0.415), (0.064, 0.055, 0.062)), CHERRY, "body", mirror=True, fuse=False,
      surface="shell")

# --- the blossom: five pale pink petals behind the face, notched at their tips, cupped a little forward ---------------
OUTLINE = [(0.0, -0.04), (0.07, -0.066), (0.16, -0.09), (0.228, -0.072), (0.262, -0.036), (0.222, 0.0),
           (0.262, 0.036), (0.228, 0.072), (0.16, 0.09), (0.07, 0.066), (0.0, 0.04)]
CUP = math.radians(16)
for i in range(5):
    a = 2 * math.pi * i / 5
    d = np.array([math.sin(a) * math.cos(CUP), -math.sin(CUP), math.cos(a) * math.cos(CUP)])
    across = np.array([math.cos(a), 0.0, -math.sin(a)])
    n = np.cross(d, across); n = n if n[1] < 0 else -n          # toward the front
    base = P0 + d * 0.035
    k.add(f"petal{i}", slab(OUTLINE, base + n * 0.004, d, across, 0.012), PETAL, "petals", fuse=False,
          surface="leaf")
    k.add(f"petalback{i}", slab([(x * 0.985, y * 0.97) for x, y in OUTLINE], base - n * 0.005, d, across, 0.01),
          PETALBACK, "petals", fuse=False, surface="leaf")
# the blossom's heart seen from behind, where the petals meet
k.add("heart", ell(P0 + (0, 0.004, 0), (0.1, 0.016, 0.1)), PETALBACK, "petals", fuse=False, surface="leaf")

# --- the mouth: wide open, flat along the top, the tongue in its lower half ------------------------------------------
CM, CT = srgb(MOUTH), srgb(TONGUE)
def mouth(u, v):
    c = np.tile(CM, (len(u), 1))
    c[(u / 0.62) ** 2 + ((v + 0.72) / 0.48) ** 2 < 1] = CT
    return c
def mouth_shape(th):
    s = math.sin(th)
    return min(1.0, 0.45 / s) if s > 0 else 1.0
MP, MN = on_face((0.0, -1.0, -0.42))
k.decal(MP, MN, (0.052, 0.05), mouth, eps=0.004, outline=mouth_shape, rings=14, segs=56)

k.build(voxel=0.006, band=0.004)

# --- Cherrim's eyes made pink-red: a dark rim, a glint high on the outer side ------------------------------------------
CY, CE, CD = srgb(YELLOW), srgb(EYERED), srgb(EYEDARK)
def eye(u, v):
    c = np.tile(CE, (len(u), 1))
    c[((u + 0.0) / 0.5) ** 2 + ((v + 0.3) / 0.5) ** 2 < 1] = srgb("#C8305A")
    c[((u - 0.15) / 0.32) ** 2 + ((v - 0.35) / 0.3) ** 2 < 1] = (1, 1, 1)
    c[np.sqrt(u * u + v * v) > 0.8] = CD
    return c
EP, EN = on_face((0.4, -0.9, 0.12))
def eyes(state):
    k.decal(EP, EN, (0.022, 0.03), eye_look(state, eye, CY, CD), eps=0.004, rings=20, segs=56, mirror=True)

k.eye_states(eyes)
k.export(out)
