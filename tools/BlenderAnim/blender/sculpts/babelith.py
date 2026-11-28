# Babelith sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/babelith.py -- <the export's Babelith.glb> <out.glb>
#
# Babut grown (its lichen, its cracks and its way of laying marks, imported from babut.py). The design, from its
# picture (docs/art/reference/gemini/carpathia/10081-Babelith.jpg) and brief (CP-dex-2-mountains.md, "81. Babelith: a
# weathered stone"): a mushroom-shaped pillar of sandstone. The cap is the first mass, as wide as the whole is tall,
# a low dome whose rim dips at the front where the face is worn into it: heavy brows sloping down to the nose over
# deep-set stern eyes, a broad bulb of a nose, folds beside it and a downturned mouth. The neck is narrow under the
# cap and flares to a broad foot that spreads into roots on the ground. Two thick stone arms hang from under the cap's
# sides, longer than the brief's "stubby" (the picture's: they reach the neck's foot), each ending in a hand of three
# hooked fingers and a thumb. Bands of rusty and pale rock run wavily round the neck, the arms and the cap's sides;
# lichen in yellow blots dotted orange on the cap, its rim, the arms and the foot, carried round the back.
#
# Skeleton: root, body (0, 0, 0.61), armL/armR (+-0.3, -0.04, 0.69). No head or leg bones: cap, face, neck and foot are
# all on body, the arms on their bones from their joints under the cap.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, polygon, eye_look, args, sibling

b_ = sibling("babut")

# --- palette ----------------------------------------------------------------------------------------------------------
SAND, SAND_DK, RUST, PALE = "#B3AC9C", "#958E80", "#C68A70", "#E7DDC8"
RIM, WHITE, IRIS, PUPIL = "#4A443E", "#CFCAC0", "#6A6258", "#2A2622"
CREASE = "#5C554C"

# --- the frame --------------------------------------------------------------------------------------------------------
CAP_C, CAP_R = np.array([0.0, 0.01, 0.69]), np.array([0.5, 0.43, 0.4])
FACE_C, FACE_R = np.array([0.0, -0.22, 0.69]), np.array([0.26, 0.17, 0.17])
FACE_Y = -0.39                     # the face's front, about
EYE_X, EYE_Z, EYE_HALF = 0.112, 0.77, (0.062, 0.042)
NOSE = np.array([0.0, -0.41, 0.705])
MOUTH_Z, MOUTH_HALF = 0.625, (0.135, 0.03)
STEM = [(0.0, 0.31), (0.06, 0.27), (0.16, 0.225), (0.3, 0.2), (0.44, 0.205), (0.56, 0.235)]
SHOULDER = np.array([0.31, -0.03, 0.6])
ELBOW = np.array([0.345, -0.06, 0.42])
WRIST = np.array([0.36, -0.1, 0.27])


def cap_shape(V):
    """A low dome: the top round, the underside nearly flat."""
    V = V.copy()
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.36, V[:, 2])
    return V


def eye_outline():
    """The eye's outline, cut along its top by the brow on a slope down toward the nose (u is outward)."""
    th = np.linspace(0, 2 * math.pi, 64, endpoint=False)
    u, v = np.cos(th), np.sin(th)
    v = np.minimum(v, 0.12 + 0.42 * u)
    return polygon(np.stack([u, v], 1))


def stern_eye():
    cr, cw, ci, cp = srgb(RIM), srgb(WHITE), srgb(IRIS), srgb(PUPIL)
    def look(u, v):
        c = np.tile(cw, (len(u), 1))
        c[((u + 0.12) / 0.5) ** 2 + ((v + 0.12) / 0.62) ** 2 < 1] = ci
        c[((u + 0.12) / 0.26) ** 2 + ((v + 0.12) / 0.34) ** 2 < 1] = cp
        top = 0.12 + 0.42 * u
        r = np.sqrt(u * u + v * v)
        c[(r > 0.8) | (v > top - 0.2)] = cr
        return c
    return look


def bands(P):
    """The banded rock: wavy rings of rust round neck, arms and the cap's sides, kept off the face and the cap's top."""
    th = np.arctan2(P[:, 1], P[:, 0])
    z = P[:, 2] + 0.014 * np.sin(3 * th + 0.7) + 0.008 * np.sin(7 * th)
    m = np.zeros(len(P))
    for c, w in [(0.1, 0.008), (0.2, 0.011), (0.32, 0.008), (0.43, 0.01), (0.53, 0.007),
                 (0.61, 0.009), (0.68, 0.011)]:
        m = np.maximum(m, inside(np.abs(z - c) - w, 0.004))
    face = inside(np.abs(P[:, 0]) - 0.27, 0.01) * smoothstep(-0.2, -0.27, P[:, 1]) * inside(P[:, 2] - 0.86, 0.01)
    return m * (1 - face)


def pale_bands(P):
    th = np.arctan2(P[:, 1], P[:, 0])
    z = P[:, 2] + 0.014 * np.sin(3 * th + 0.7) + 0.008 * np.sin(7 * th)
    m = np.zeros(len(P))
    for c, w in [(0.225, 0.007), (0.455, 0.007), (0.645, 0.007),
                 ]:
        m = np.maximum(m, inside(np.abs(z - c) - w, 0.004))
    face = inside(np.abs(P[:, 0]) - 0.27, 0.01) * smoothstep(-0.2, -0.27, P[:, 1]) * inside(P[:, 2] - 0.86, 0.01)
    return m * (1 - face)


def arm(k):
    k.add("arm", tube([SHOULDER, (SHOULDER + ELBOW) / 2 + [0.01, 0, 0], ELBOW, WRIST], [0.075, 0.08, 0.072, 0.064]),
          SAND, "arm{s}", mirror=True)
    k.add("forearm", ell((0.355, -0.08, 0.35), (0.075, 0.07, 0.1), rot=(-12, 4, 0)), SAND, "arm{s}", mirror=True)
    H = np.array([0.36, -0.12, 0.215])
    k.add("palm", ell(H, (0.07, 0.06, 0.065)), SAND, "arm{s}", mirror=True)
    # three hooked fingers curling in under the palm, and a thumb on the inner side
    for j, dx in enumerate((-0.035, 0.0, 0.035)):
        a = H + [dx, -0.035, -0.035]
        k.add(f"finger{j}", tube([a, a + [dx * 0.3, -0.03, -0.045], a + [dx * 0.3, -0.005, -0.085]],
                                 [0.024, 0.021, 0.016]), SAND, "arm{s}", mirror=True)
    a = H + [-0.055, -0.03, 0.0]
    k.add("thumb", tube([a, a + [-0.03, -0.03, -0.03], a + [-0.02, -0.04, -0.065]], [0.022, 0.019, 0.015]), SAND,
          "arm{s}", mirror=True)


def babelith(src, dest):
    k = Kit(src, coat="shell")
    rs = np.random.RandomState(81)

    # The cap and the face's swell at its front, dipping the rim there
    k.add("cap", ell(CAP_C, CAP_R, deform=cap_shape, sub=5), SAND, "body")
    k.add("face", ell(FACE_C, FACE_R), SAND, "body")
    # The neck, flaring to its foot, and the roots spreading on the ground
    V, F = tube([(0, 0.015, z) for z, _ in STEM], [r for _, r in STEM], seg=24)
    V[:, 2] = np.maximum(V[:, 2], 0.0)          # the end cap pressed flat on the ground
    k.add("stem", (V, F), SAND, "body")
    for i, a in enumerate((20, 100, 160, 230, 290, 340)):
        t = math.radians(a)
        d = np.array([math.cos(t), math.sin(t), 0])
        k.add(f"root{i}", ell(d * 0.25 + [0, 0, 0.045], (0.11, 0.075, 0.045), rot=(0, 0, a)), SAND, "body")
    arm(k)

    # The face: brows low and sloping down to the nose, the nose, the folds beside it, the cheeks
    for s in (1, -1):
        k.add(f"brow{'R' if s > 0 else 'L'}", ell((s * 0.11, -0.365, 0.835), (0.085, 0.045, 0.032), rot=(0, -s * 16, 0)),
              SAND, "body")
        k.add(f"fold{'R' if s > 0 else 'L'}", ell((s * 0.075, -0.38, 0.67), (0.035, 0.03, 0.05), rot=(0, s * -20, 0)),
              SAND, "body")
    k.add("nose", ell(NOSE, (0.058, 0.05, 0.058)), SAND, "body")
    k.add("bridge", ell((0, -0.385, 0.77), (0.032, 0.03, 0.05)), SAND, "body")

    k.paint(bands, RUST)
    k.paint(pale_bands, PALE)
    # the wrinkles of the brow, the creases under the eyes: dark worn lines
    k.paint(lambda P: b_.crack(P, [(-0.03, -0.375, 0.86), (0.03, -0.375, 0.875)], 0.004, 0.003), CREASE, on=["face", "brow", "bridge"])
    for s in (1, -1):
        k.paint(lambda P, s=s: b_.crack(P, [(s * 0.06, -0.385, 0.725), (s * 0.115, -0.38, 0.715), (s * 0.16, -0.365, 0.73)],
                                        0.0035, 0.003), CREASE, on=["face", "fold"])

    # The mouth: a downturned crease, heavy at its corners
    cs, ci = srgb(SAND), srgb(CREASE)
    def mouth(u, v):
        c = np.tile(cs, (len(u), 1))
        line = 0.22 - 0.5 * u * u
        c[(np.abs(v - line) < 0.26 * (1 - 0.3 * u * u)) & (np.abs(u) < 0.95)] = ci
        return c
    k.decal((0, FACE_Y + 0.02, MOUTH_Z), unit(0, -1, -0.1), MOUTH_HALF, mouth, eps=0.004, rings=24, segs=96)

    # Lichen on the cap's top and rim, the arms, the foot, and the back
    for d, size in [((0.25, -0.2, 0.95), 0.05), ((-0.35, 0.0, 0.93), 0.045), ((0.0, -0.4, 0.92), 0.035),
                    ((0.05, 0.45, 0.9), 0.05), ((0.75, -0.3, 0.3), 0.045), ((-0.8, -0.3, 0.25), 0.045),
                    ((0.85, 0.35, 0.2), 0.04), ((-0.6, 0.65, 0.3), 0.045), ((0.35, 0.85, 0.3), 0.04),
                    ((-0.25, -0.75, 0.55), 0.035)]:
        at, n = b_.on_stone(d, CAP_C, CAP_R * np.array([1, 1, 1.0]))
        b_.lichen(k, at, n, size * 1.6, rs, eps=0.005)
    for at, n, size in [((0.15, -0.24, 0.1), (0.4, -0.9, 0.2), 0.04), ((-0.18, -0.2, 0.1), (-0.5, -0.8, 0.2), 0.035),
                        ((0.05, 0.25, 0.1), (0, 1, 0.2), 0.04), ((0.42, -0.06, 0.4), (1, -0.3, 0), 0.03),
                        ((-0.42, -0.06, 0.45), (-1, -0.3, 0), 0.03)]:
        b_.lichen(k, np.array(at), n, size * 1.4, rs, eps=0.005, satellites=2)

    k.build(voxel=0.0105, smooth=3)
    b_.board(k.ob)

    look = stern_eye()
    out_line = eye_outline()
    def eyes(state):
        for s in (1, -1):
            dn = unit(s * 0.3, -0.95, 0.12)
            f = eye_look(state, look, srgb(SAND), srgb(RIM), line=0.17)
            lk = f if s > 0 else (lambda g: (lambda u, v: g(-u, v)))(f)
            if s > 0:
                ol = out_line
            else:
                ol = (lambda o: (lambda th: o(math.pi - th)))(out_line)
            k.decal((s * EYE_X, FACE_Y + 0.03, EYE_Z), dn, EYE_HALF, lk, eps=0.0045, rings=20, segs=56, outline=ol)
    k.eye_states(eyes)
    k.export(dest)


if __name__ == "__main__":
    babelith(*args())
