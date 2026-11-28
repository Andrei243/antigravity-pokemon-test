# Mega Dynamax Lucario (Lucario-Mega-Mmax), a form of this project's own, sculpted as Lucario-Mega's script with the
# Mega Dynamax form's additions laid on its kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/lucariomegammax.py -- <the export's Lucario-Mega-Mmax.glb> <out.glb>
# The Mega whole and unchanged, and over it, from the picture (Mega-Dynamax-Lucario.jpg), largest first: a great
# sphere of aura held before its belly toward its right hand, cyan with a white core and rings of light; ribbons of
# crystal streaming on from the two upper tendrils, blue turning red, back and up; clouds of Max energy about the
# feet with a cyan curl or two, a wisp of crimson by the left ear and one behind the right shoulder; the Key Stone's
# emblem, a glowing disc in a cyan ring, at the top of the cream chest.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, tube, lathe, ring_band, srgb, unit, sibling, args, outward

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
AURA, CORE, RING = "#5ACAFA", "#E6FAFF", "#9AF2FA"
RIBBLUE, RIBRED = "#4682E8", "#D63C54"
STONE = "#FFD890"

# From lucariomega.py: the head's centre (0, -0.005, 0.955); the upper tendrils end at (+-0.23, 0.44, 0.87) and the
# lower at (+-0.26, 0.44, 0.79), each with a red ball beyond; the neck (0, 0, 0.81) r 0.066 under the collar's spikes
# (+-0.07, -0.05, 0.83); the white chest spike at (0, -0.08, 0.72) pointing forward; the paws at (+-0.258, -0.1,
# 0.465); the feet at x +-0.117, toes to y -0.15; the tail back and out to +X from (0, 0.08, 0.47); the voxel 0.0075.


def ribbon(pts, width, thick, face, per=6, seg=18):
    """A flat band along a smooth curve through pts, its broad face turned as near to face as the curve lets it."""
    pts = np.array(pts, float)
    ext = np.vstack([2 * pts[0] - pts[1], pts, 2 * pts[-1] - pts[-2]])
    P = []
    for i in range(len(pts) - 1):
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        for j in range(per):
            t = j / per
            P.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                            + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    P.append(pts[-1])
    P = np.array(P)
    T = np.gradient(P, axis=0)
    T /= np.linalg.norm(T, axis=1, keepdims=True)
    ang = np.linspace(0, 2 * math.pi, seg, endpoint=False)
    cs, sn = np.cos(ang), np.sin(ang)
    sx = np.sign(cs) * np.abs(cs) ** 0.35
    sy = np.sign(sn) * np.abs(sn) ** 0.35
    face = np.array(face, float)
    V, rows = [], []
    for i, p in enumerate(P):
        n = face - T[i] * (face @ T[i]); n /= np.linalg.norm(n)
        b = np.cross(T[i], n)
        w = width(i / (len(P) - 1)) if callable(width) else width
        rows.append(len(V))
        V.extend(p + (w / 2) * sx[j] * b + (thick / 2) * sy[j] * n for j in range(seg))
    F = []
    for r0, r1 in zip(rows, rows[1:]):
        for j in range(seg):
            F.append([r0 + j, r0 + (j + 1) % seg, r1 + (j + 1) % seg, r1 + j])
    s, e = len(V), len(V) + 1
    V.append(P[0] - T[0] * thick * 0.3); V.append(P[-1] + T[-1] * thick * 0.3)
    for j in range(seg):
        F.append([s, rows[0] + (j + 1) % seg, rows[0] + j])
        F.append([e, rows[-1] + j, rows[-1] + (j + 1) % seg])
    return outward((np.array(V), F))


def maxed(k):
    """What the Mega Dynamax form lays over its Mega: called with the Mega's kit just before it is meshed."""
    # ---- the aura sphere before the belly, toward the right hand ---------------------------------------------------
    SC, SR = np.array([0.1, -0.25, 0.56]), 0.145
    k.add("aura", ell(SC, (SR, SR, SR), sub=4), AURA, "body", fuse=False, glow=AURA)
    k.add("auracore", ell(SC + (0.0, -SR * 0.62, 0.0), (SR * 0.42, SR * 0.42, SR * 0.42), sub=3), CORE, "body",
          fuse=False, glow=CORE)
    for j, (ax, r) in enumerate(((unit(0.3, -0.5, 1), 1.06), (unit(1, -0.3, -0.4), 1.08))):
        k.add(f"auraring{j}", ring_band(SC, ax, SR * r, 0.02, thick=0.014), RING, "body", fuse=False, glow=RING)

    # ---- ribbons of crystal streaming on from the upper tendrils: blue, turning red, back and up -------------------
    face = unit(0.52, -0.85, 0.25)
    for s in (1, -1):
        g = 1.0 if s > 0 else 0.72          # the one on +X (seen by the front camera) the larger
        b = np.array([0.23 * s, 0.44, 0.87])
        off = [(-0.03, -0.07, -0.01), (0, 0, 0), (0.04, 0.08, 0.08), (0.1, 0.1, 0.18), (0.17, 0.07, 0.25),
               (0.24, 0.09, 0.19), (0.27, 0.15, 0.08), (0.31, 0.23, 0.09), (0.35, 0.27, 0.19)]
        path = [b + np.array([o[0] * s, o[1], o[2]]) * (g if i > 1 else 1.0) for i, o in enumerate(off)]
        f = np.array([face[0] * s, face[1], face[2]])
        k.add(f"ribbonblue{'RL'[s < 0]}", ribbon(path[:6], lambda t: 0.06 + 0.05 * t, 0.038, f), RIBBLUE, "head",
              fuse=False, glow=RIBBLUE, surface="shell")
        k.add(f"ribbonred{'RL'[s < 0]}", ribbon(path[5:], lambda t: 0.11 - 0.05 * t * t, 0.04, f), RIBRED, "head",
              fuse=False, glow=RIBRED, surface="shell")

    # ---- the emblem at the top of the cream chest, above its white spike --------------------------------------------
    c = np.array([0.0, -0.066, 0.787])
    d = unit(0, -1, 0.2)
    k.add("emblem", lathe(c, d, [(0, -0.004), (0.024, -0.004), (0.024, 0.008), (0, 0.011)]), STONE, "body",
          fuse=False, glow=STONE)
    k.add("emblemring", ring_band(c + d * 0.002, d, 0.031, 0.011, thick=0.009), CYAN, "body", fuse=False, glow=CYAN)

    # ---- the clouds: fused mounds in a ring about the feet, clear of them ---------------------------------------------
    rnd = np.random.default_rng(9)
    nm = 9
    CX, CY, RX, RY = 0.0, -0.03, 0.31, 0.27
    for i in range(nm):
        a = 2 * math.pi * (i + 0.5) / nm
        col = (CRIMSON, VIOLET, DEEP, DVIOLET)[i % 4]
        other = (VIOLET, CRIMSON, DVIOLET, CRIMSON)[i % 4]
        for j in range(4):
            sz = (0.068, 0.055, 0.046, 0.034)[j] * (1 + 0.12 * rnd.uniform(-1, 1))
            t = a + (0.0, 0.22, -0.22, 0.06)[j]
            dr = (0.0, 0.02, 0.015, -0.02)[j]
            p = (CX + (RX + dr) * math.cos(t), CY + (RY + dr) * math.sin(t), sz * (0.85, 0.9, 0.85, 2.6)[j])
            k.add(f"mcloud{i}_{j}", ell(p, (sz, sz, sz * 0.88)), other if j == 2 else col, "root")
        if i % 3 == 0:
            q = np.array([CX + RX * math.cos(a), CY + RY * math.sin(a), 0.1])
            o = unit(math.cos(a), math.sin(a), 0)
            tg = np.cross((0, 0, 1.0), o)
            k.add(f"curl{i}", tube([q - tg * 0.045, q + o * 0.025 + (0, 0, 0.03), q + tg * 0.035 + (0, 0, 0.045),
                                    q + tg * 0.008 + o * 0.012 + (0, 0, 0.058)], [0.017, 0.019, 0.017, 0.015], seg=10),
                  CYAN, "root", fuse=False, glow=CYAN)

    # ---- wisps of crimson: by the left ear, and behind the right shoulder --------------------------------------------
    # (each a clump of flattened puffs overlapping heavily, one colour, so that it reads as a cloud and not as berries)
    for j, (c0, col) in enumerate((((-0.21, 0.06, 1.12), CRIMSON), ((0.29, 0.16, 0.8), DEEP))):
        c0 = np.array(c0)
        for q, (dx, dy, dz, r) in enumerate(((0, 0, 0, 0.042), (0.035, 0.01, -0.006, 0.034), (-0.034, 0.0, -0.008, 0.032),
                                             (0.012, -0.004, 0.026, 0.028), (-0.012, 0.02, 0.02, 0.026))):
            k.add(f"wisp{j}_{q}", ell(c0 + (dx, dy, dz), (r * 1.2, r * 1.1, r * 0.8), sub=3), col, "body", fuse=False)


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

m = sibling("lucariomega")
m.lucario_mega(*args())
