# Pineco sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pineco.py -- <the export's Pineco.glb> <out.glb>
#
# The design, from the references: a pine cone a little wider at the top than at its rounded bottom, made of thick
# flat scales with square ends standing out in staggered rows: blue-green over the upper half, a duller blue below,
# short stubs pointing down under it. A cone of a spike stands straight up from the top. Two long scales like brows
# rise from the middle of the top up and out over a dark gap; under them two eyes peer out of the dark (white, a red
# iris and a black pupil, their top edge cut by the brow so they glare toward the middle). Between the eyes and below
# them a big scale juts forward like a nose: a flat bar on top narrowing down to a point.
# The skeleton is one bone, the body, at z 0.17: everything is on it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, inside, smoothstep, unit, eye_look, polygon, hull, args

TEAL, TEAL_D, BLUE, BLUE_D = "#4E958F", "#46838A", "#3C6A7C", "#33596C"
SLOT, WHITE, RED, PUPIL, LIGHT = "#141830", "#F4F2EE", "#D8363A", "#141416", "#9CCFC8"

C = np.array([0.0, 0.0, 0.18])           # the cone's middle, on the body bone
RX, RY, RZ = 0.145, 0.14, 0.15           # its radii
FLARE = 0.12                             # how much wider the top is than the bottom


def cone(V):
    V = V.copy()
    V[:, :2] *= (1 + FLARE * V[:, 2:3])
    return V


def surf(az, z):
    """The body's surface at azimuth az (degrees from the front, toward +X) and height z, and the way out there."""
    t = np.clip((z - C[2]) / RZ, -0.99, 0.99)
    w = math.sqrt(1 - t * t) * (1 + FLARE * t)
    a = math.radians(az)
    p = np.array([RX * w * math.sin(a), -RY * w * math.cos(a), z]) + C * np.array([1, 1, 0])
    return p, np.array([math.sin(a), -math.cos(a), 0.0])


def plate(k, name, az, z, length, w0, w1, th, tilt, color, sink=0.035):
    """A thick scale with a bevelled square end, from inside the body out along its way tilted by tilt degrees."""
    p, dh = surf(az, z)
    a = math.radians(az)
    side = np.array([math.cos(a), math.sin(a), 0.0])
    tl = math.radians(tilt)
    d = dh * math.cos(tl) + np.array([0, 0, 1.0]) * math.sin(tl)
    n = np.cross(side, d); n /= np.linalg.norm(n)
    root = p - d * sink
    L = length + sink
    pts = []
    for s, w, t in ((0, w0, th * 1.15), (L - 0.008, w1, th), (L, w1 * 0.82, th * 0.6)):
        for sw in (1, -1):
            for st in (1, -1):
                pts.append(root + d * s + side * w * sw + n * t * st)
    k.add(name, hull(pts), color, "body", fuse=False, surface="shell")


def brow(k, s):
    """A long scale rising from the middle of the top up and out over the eye, with a dark underside."""
    a = np.array([s * 0.012, -0.112, 0.286])
    b = np.array([s * 0.172, -0.058, 0.372])
    pts = []
    for q in (a, b):
        for dz in (0.022, -0.02):
            for dy in (0.0, 0.085):
                pts.append(q + (0, dy, dz))
    k.add(f"brow{'R' if s > 0 else 'L'}", hull(pts), TEAL, "body", fuse=False, surface="shell")
    pts = []
    for q in (a, b):
        for dz in (-0.018, -0.046):
            for dy in (0.012, 0.075):
                pts.append(q + (s * 0.004, dy, dz))
    k.add(f"browdark{'R' if s > 0 else 'L'}", hull(pts), SLOT, "body", fuse=False, surface="shell")


def nose(k):
    """The scale that juts forward between the eyes: a flat bar on top narrowing down to a point."""
    pts = [(0.05, -0.112, 0.222), (-0.05, -0.112, 0.222), (0.046, -0.215, 0.214), (-0.046, -0.215, 0.214),
           (0.048, -0.112, 0.195), (-0.048, -0.112, 0.195), (0.044, -0.212, 0.19), (-0.044, -0.212, 0.19),
           (0.012, -0.12, 0.128), (-0.012, -0.12, 0.128), (0.006, -0.19, 0.13), (-0.006, -0.19, 0.13)]
    k.add("nose", hull(pts), TEAL, "body", fuse=False, surface="shell")


def spike(k):
    pts = [(0.038, 0.038, 0.29), (-0.038, 0.038, 0.29), (0.038, -0.038, 0.29), (-0.038, -0.038, 0.29),
           (0.024, 0.024, 0.34), (-0.024, 0.024, 0.34), (0.024, -0.024, 0.34), (-0.024, -0.024, 0.34),
           (0.0, -0.004, 0.405)]
    k.add("spike", hull(pts), TEAL, "body", fuse=False, surface="shell")


def mirrored(az_list):
    out = []
    for a in az_list:
        out.extend([a] if a in (0, 180) else [a, -a])
    return out


# (height, azimuths, length, root half width, end half width, half thickness, tilt, colour)
ROWS = [
    (0.318, mirrored([56, 100, 140, 180]), 0.05, 0.036, 0.03, 0.014, 52, TEAL),
    (0.292, mirrored([72, 112, 148, 180]), 0.068, 0.046, 0.04, 0.016, 28, TEAL),
    (0.218, mirrored([70, 108, 144, 180]), 0.078, 0.048, 0.042, 0.017, -4, TEAL),
    (0.15, mirrored([34, 72, 110, 148]), 0.072, 0.046, 0.04, 0.016, -18, TEAL_D),
    (0.09, mirrored([0, 52, 96, 138, 180]), 0.058, 0.042, 0.036, 0.015, -34, BLUE),
    (0.045, mirrored([26, 100, 180]), 0.042, 0.03, 0.026, 0.014, -68, BLUE_D),
]


IRIS_UV, IRIS_HALF = (-0.16, -0.36), 0.0155     # the iris: its place in the eye's u, v and its radius


def eye_white(u, v):
    return np.tile(srgb(WHITE), (len(u), 1))


def eye_open(u, v):
    c = eye_white(u, v)
    c[((u - IRIS_UV[0]) * EYE_HALF[0]) ** 2 + ((v - IRIS_UV[1]) * EYE_HALF[1]) ** 2 < IRIS_HALF ** 2] = srgb(RED)
    return c


# The eye's outline (u outward, v up): its top edge slopes from the outer corner down to the inner, as the brow cuts it
EYE = [(1.0, 0.62), (1.0, -0.45), (0.62, -0.95), (-0.35, -0.97), (-0.92, -0.62), (-1.0, -0.12)]
EYE_HALF = (0.038, 0.029)
EYE_X, EYE_Z = 0.058, 0.247


def eyes(k, state):
    for s in (1, -1):
        p, _ = surf(s * math.degrees(math.asin(EYE_X / 0.137)), EYE_Z)
        n = unit(s * 0.42, -1, 0.3)
        # open, the iris is a round patch of its own (inside the eye's colour function its edge came out ragged)
        look = eye_look(state, eye_white if state == "Open" else eye_open, srgb(SLOT), srgb(LIGHT), line=0.16)
        if s > 0:
            lk, o = look, polygon(EYE)
        else:
            lk, o = (lambda f: (lambda u, v: f(-u, v)))(look), polygon([(-a, b) for a, b in EYE])
        k.decal(p, n, EYE_HALF, lk, outline=o, eps=0.003, rings=20, segs=56)
        q = p + np.array([s * IRIS_UV[0] * EYE_HALF[0], 0.0, IRIS_UV[1] * EYE_HALF[1]])
        if state == "Open":
            k.decal(q, n, (IRIS_HALF, IRIS_HALF), srgb(RED), eps=0.0037, rings=12, segs=40)
        if state in ("Open", "Fierce"):
            k.decal(q, n, (0.0085, 0.0095), srgb(PUPIL), eps=0.0045, rings=8, segs=32)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    k.add("body", ell(C, (RX, RY, RZ), deform=cone, sub=5), TEAL, "body")
    # the lower half duller and bluer
    k.paint(lambda P: 1 - smoothstep(0.13, 0.2, P[:, 2]), BLUE)
    # the dark gap under the brows, where the eyes are
    k.paint(lambda P: (P[:, 1] < -0.03) * inside(np.abs(P[:, 0]) - 0.17, 0.012) * inside(0.198 - P[:, 2], 0.003)
            * inside(P[:, 2] - (0.296 + 0.5 * np.abs(P[:, 0])), 0.003), SLOT)
    spike(k)
    for sd in (1, -1):
        brow(k, sd)
    nose(k)
    for ri, (z, azs, ln, w0, w1, th, tilt, col) in enumerate(ROWS):
        for j, az in enumerate(azs):
            plate(k, f"scale{ri}_{j}", az, z, ln, w0, w1, th, tilt, col)
    k.build(voxel=0.004, smooth=3)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
