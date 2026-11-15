# Lotad sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/lotad.py -- <the export's Lotad.glb> <out.glb>
#
# The design, from the references: a small blue creature, its head and body one rounded mass, half hidden under a
# broad green lily pad worn on its back like a dish: a thick plate with a raised rim all round, a wedge cut out of it
# to its middle, and dark green triangles on its floor pointing in from the rim. A wide pale yellow bill pokes out
# from under the pad's front; a small sleepy eye (white, a black pupil, the upper lid low over it) on each side above
# the bill; three round stub legs down each side.
#
# Written to be grown from (lombre.py and ludicolo.py import it with sibling("lotad")): the line's colours, the way
# a pad is swept (pad_sweep) and the sleepy eye (sleepy_eye) are named here; the build is under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, slab, srgb, inside, unit, eye_look, outward, earclip, args

# --- the line's colours ----------------------------------------------------------------------------------------------
BLUE, BLUE_DARK = "#3E9AD8", "#2C78B4"
PAD, PAD_DARK, PAD_UNDER = "#5FB85A", "#2F7A36", "#58AE52"
BILL = "#F4D57A"
INK, WHITE = "#16181E", "#FFFFFF"

# --- Lotad's frame ---------------------------------------------------------------------------------------------------
BODY_C, BODY_R = np.array([0.0, 0.01, 0.104]), np.array([0.108, 0.118, 0.084])
FACE_C, FACE_R = np.array([0.0, -0.07, 0.11]), np.array([0.09, 0.072, 0.074])
PAD_C = np.array([0.0, 0.03, 0.198])         # the pad's middle, just over the back (the pad bone is at 0, 0, 0.16)
PAD_TILT = (16, 0, 0)                         # its back raised, so its floor shows to the game's cameras
PAD_RADIUS, PAD_FLOOR, PAD_RIM = 0.178, 0.014, 0.042
NOTCH_AT, NOTCH_HALF = -45.0, 24.0            # the wedge's middle, degrees (round from +X toward +Y), toward the front-right
EYE_DIR = unit(0.58, -0.72, 0.26)
EYE_HALF = (0.026, 0.024)
VOXEL = 0.0055


def rotation(rot):
    return np.array(Euler([math.radians(a) for a in rot]).to_matrix())


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r) in direction d from its centre, and the surface's normal there."""
    d = np.array(d, float)
    q = d / np.array(r, float)
    p = np.array(c, float) + d / np.linalg.norm(q)
    n = (p - np.array(c, float)) / np.array(r, float) ** 2
    return p, n / np.linalg.norm(n)


def pad_sweep(profile, a0, a1, n, c, R, closed=False):
    """
    A solid swept round an axis: profile(a) gives a closed outline of (r, h) points (out from the axis, up along it) at
    the angle a in radians, the same number of points at every angle. From a0 to a1 with flat ends (a wedge left out
    between them), or all the way round when closed. c and R place it (R a turn).
    """
    angles = np.linspace(a0, a0 + 2 * math.pi, n, endpoint=False) if closed else np.linspace(a0, a1, n + 1)
    rows = []
    for a in angles:
        p = np.array(profile(a), float)
        area = (p[:, 0] * np.roll(p[:, 1], -1) - np.roll(p[:, 0], -1) * p[:, 1]).sum()
        if area < 0:
            p = p[::-1]
        rows.append(np.stack([p[:, 0] * math.cos(a), p[:, 0] * math.sin(a), p[:, 1]], axis=1))
    m = len(rows[0])
    V = np.vstack(rows)
    F = []
    steps = len(angles) if closed else len(angles) - 1
    for j in range(steps):
        j1 = (j + 1) % len(angles)
        for i in range(m):
            i1 = (i + 1) % m
            F.append([j * m + i, j * m + i1, j1 * m + i1, j1 * m + i])
    if not closed:
        last = len(angles) - 1
        p0 = np.array(profile(a0), float)
        if (p0[:, 0] * np.roll(p0[:, 1], -1) - np.roll(p0[:, 0], -1) * p0[:, 1]).sum() < 0:
            p0 = p0[::-1]
        for a, b, cc in earclip(p0):
            F.append([cc, b, a])
            F.append([last * m + a, last * m + b, last * m + cc])
    return outward((V @ np.array(R).T + np.array(c, float), F))


def sleepy_eye(lid_at=0.22, pupil=(-0.18, -0.2), pupil_r=0.42, lid=BLUE):
    """A sleepy eye: white, a big black pupil low toward the nose, the upper lid down to lid_at with a dark edge."""
    W, K, L = srgb(WHITE), srgb(INK), srgb(lid)
    def look(u, v):
        c = np.tile(W, (len(u), 1))
        r = np.hypot(u - pupil[0], v - pupil[1])
        c[r < pupil_r] = K
        c[np.hypot(u, v) > 0.86] = K
        c[v > lid_at + 0.16] = L
        c[np.abs(v - lid_at - 0.07) < 0.11] = K
        return c
    return look


def lay_eye(k, state, p, n, half, look, lid, eps=0.005):
    """One eye in a state, mirrored to the other side."""
    k.decal(p, n, half, eye_look(state, look, srgb(lid), srgb(INK), line=0.15), eps=eps, rings=20, segs=56,
            mirror=True)


def lily_pad(k):
    """The pad: a thick dish with a raised rim, a wedge cut to its middle, dark triangles on its floor."""
    R = rotation(PAD_TILT)
    Rr, t, H = PAD_RADIUS, PAD_FLOOR, PAD_RIM
    profile = [(0.006, -0.004), (Rr - 0.01, -0.004), (Rr + 0.004, 0.012), (Rr + 0.006, H), (Rr - 0.014, H),
               (Rr - 0.02, t + 0.006), (Rr - 0.04, t), (0.006, t)]
    a0, a1 = math.radians(NOTCH_AT + NOTCH_HALF), math.radians(NOTCH_AT + 360 - NOTCH_HALF)
    k.add("pad", pad_sweep(lambda a: profile, a0, a1, 64, PAD_C, R), PAD, "pad", fuse=False, surface="leaf")
    # the dark triangles: bases along the rim, points toward the middle
    ex, ey = R @ np.array([1.0, 0, 0]), R @ np.array([0, 1.0, 0])
    origin = PAD_C + R @ np.array([0, 0, t + 0.0012])
    span = 360 - 2 * NOTCH_HALF
    for i in range(4):
        a = math.radians(NOTCH_AT + NOTCH_HALF + span * (i + 0.5) / 4)
        rb, w, tip = Rr - 0.042, 0.29, 0.07
        outline = [(rb * math.cos(a - w), rb * math.sin(a - w)), (rb * math.cos(a + w), rb * math.sin(a + w)),
                   (tip * math.cos(a), tip * math.sin(a))]
        k.add(f"vein{i}", slab(outline, origin, ex, ey, 0.004), PAD_DARK, "pad", fuse=False, surface="leaf")


def lotad(src, out):
    k = Kit(src, coat="fur")
    # one rounded mass: the body, and the face fused into its front
    k.add("body", ell(BODY_C, BODY_R), BLUE, "body")
    k.add("face", ell(FACE_C, FACE_R), BLUE, "head")
    # six round stub legs, three down each side, splayed out and down
    for i, (y, bone) in enumerate([(-0.068, "leg{s}"), (0.008, "body"), (0.082, "hind{s}")]):
        k.add(f"stub{i}", ell((0.098, y, 0.034), (0.034, 0.03, 0.034), rot=(0, -25, 0)), BLUE, bone, mirror=True)
    # the pad's short stalk, out of the middle of the back
    k.add("stalk", tube([(0, 0.02, 0.14), (0, 0.03, 0.166)], [0.03, 0.026]), BLUE, "pad")
    # the wide pale bill, flat and drooping a little at its tip, out from under the pad
    def bill(V):
        V = V.copy()
        t = np.clip(-V[:, 1], -1, 1)
        V[:, 0] *= 0.82 + 0.22 * np.clip(t, 0, 1)
        V[:, 2] -= 0.35 * np.clip(t, 0, 1) ** 2
        return V
    k.add("bill", ell((0, -0.138, 0.078), (0.074, 0.062, 0.026), rot=(8, 0, 0), deform=bill), BILL, "head",
          fuse=False, surface="shell")
    lily_pad(k)
    k.build(voxel=VOXEL, band=0.006)

    p, n = on_ell(FACE_C, FACE_R, EYE_DIR)
    look = sleepy_eye()
    k.eye_states(lambda state: lay_eye(k, state, p, n, EYE_HALF, look, BLUE))
    k.export(out)


if __name__ == "__main__":
    lotad(*args())
