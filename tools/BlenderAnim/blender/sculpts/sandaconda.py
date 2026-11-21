# Sandaconda sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sandaconda.py -- <the export's Sandaconda.glb> <out.glb>
# Silicobra grown (silicobra.py: its face, nostrils, eye and lid; ekans.py's line through the chain of joints).
# From the references: a great snake wound in coils, the front coil a fat olive ring standing up like a wheel, its
# largest mass by far, the sack of sand, marked all over with thick black cracks that branch like thorns and with
# patches of tan; out of the ring's middle pokes the head: a pale skull of a snout with two big round nostrils
# (black, ringed pale grey), two small white fangs under its front, a brown cap over the crown, a green eye rimmed
# red under a heavy brown lid. Behind the ring the body lies coiled, cream banded with black zigzags and patched
# with tan, and ends in a pointed tail banded black.
# The pictures stand the coil up like a spring on its side; the game's skeleton lays the body round on the ground
# (body (0.3, 0.24, 0.04) behind at the right, seg1 (0.218, -0.1, 0.07) round two turns to seg11 (-0.054, -0.118,
# 0.15), head (0, -0.08, 0.44)), which decides: the body is a wide flat coil like a disc, and the ring of sand
# stands up on its front with the head in its middle.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, tri, eye_look, args, hull, sibling

e = sibling("ekans")
si = sibling("silicobra")

OLIVE, CRACK, TAN, CREAM, FACE, CAP = "#7E7A2E", "#1E1E1A", "#A8805C", "#E8E2CC", "#EEEADA", "#94705A"
NOSTRIL, NOSTRIL_RING, FANG, RED = "#1C1A1A", "#B8B8B4", "#F6F4EE", "#D23A2A"

VOXEL = 0.0062

LINE = [(0.385, 0.47, 0.03), (0.355, 0.38, 0.036), (0.3, 0.24, 0.046), (0.27, 0.07, 0.06), (0.218, -0.1, 0.07),
        (0.08, -0.215, 0.078), (-0.091, -0.198, 0.086), (-0.194, -0.072, 0.094), (-0.178, 0.082, 0.102),
        (-0.065, 0.173, 0.11), (0.072, 0.158, 0.118), (0.153, 0.057, 0.126), (0.138, -0.063, 0.134),
        (0.049, -0.132, 0.142), (-0.054, -0.118, 0.15), (-0.03, -0.13, 0.25), (0.0, -0.15, 0.34)]
RADS = [0.008, 0.03, 0.058, 0.07, 0.074, 0.08, 0.084, 0.085, 0.083, 0.08, 0.078, 0.076, 0.074, 0.072, 0.07,
        0.064, 0.06]
BONES = ["body", "body", "body", "body", "seg1", "seg2", "seg3", "seg4", "seg5", "seg6", "seg7", "seg8", "seg9",
         "seg10", "seg11", "head"]

# --- the ring of sand: a torus standing up across the front, its top leaning back a little --------------------
RING_C = np.array([0.0, -0.15, 0.355])
RING_R, RING_T = 0.178, 0.09                # the ring's radius and its tube's
TILT = math.radians(14)


def ring_frame(P):
    """Points in the ring's frame: the angle round the ring (0 at the right, up at 90), the angle round the tube
    (0 outward, 90 toward the front) and the distance from the tube's middle line."""
    Q = P - RING_C
    c, s = math.cos(TILT), math.sin(TILT)
    x = Q[:, 0]
    y = Q[:, 1] * c - Q[:, 2] * s              # across the ring, the front negative
    z = Q[:, 1] * s + Q[:, 2] * c              # up in the ring's plane
    rho = np.sqrt(x * x + z * z)
    th = np.arctan2(z, x)
    ph = np.arctan2(-y, rho - RING_R)
    dist = np.sqrt((rho - RING_R) ** 2 + y * y)
    return th, ph, dist


def torus(c, R, r, tilt, nu=96, nv=40):
    th = np.linspace(0, 2 * math.pi, nu, endpoint=False)
    ph = np.linspace(0, 2 * math.pi, nv, endpoint=False)
    T, Pp = np.meshgrid(th, ph, indexing="ij")
    rho = R + r * np.cos(Pp)
    x = rho * np.cos(T)
    z = rho * np.sin(T)
    y = -r * np.sin(Pp)
    cs, sn = math.cos(tilt), math.sin(tilt)
    Y = y * cs + z * sn
    Z = -y * sn + z * cs
    V = np.stack([x, Y, Z], axis=-1).reshape(-1, 3) + np.array(c)
    F = []
    for i in range(nu):
        for j in range(nv):
            a = i * nv + j
            b = ((i + 1) % nu) * nv + j
            F.append([a, b, ((i + 1) % nu) * nv + (j + 1) % nv, i * nv + (j + 1) % nv])
    return V, F


def ring(k):
    k.add("donut", torus(RING_C, RING_R, RING_T, TILT), OLIVE, "seg11", surface="fur")
    # the ring's middle closed behind the head, so that from behind it is one round mass of sand
    back = np.array([0.0, math.cos(TILT), -math.sin(TILT)]) * 0.035
    k.add("donutplug", ell(RING_C + back, (0.11, 0.05, 0.11), rot=(-math.degrees(TILT), 0, 0)), OLIVE, "seg11",
          surface="fur")


def cracks(k):
    """Thick black cracks across the ring's tube, each wavering and throwing short thorny spurs, and tan patches
    in the spaces between them on the ring's face."""
    LINES = np.radians([12, 62, 108, 158, 205, 250, 296, 338])
    def near_line(th, ph):
        best = np.full(len(th), 9.0)
        for i, a in enumerate(LINES):
            w = a + 0.12 * np.sin(2.3 * ph + i) + 0.05 * np.sin(5 * ph + 2 * i)
            dth = np.angle(np.exp(1j * (th - w)))
            best = np.minimum(best, np.abs(dth) * (RING_R + RING_T * np.cos(ph)))
            # a spur off each crack, toward the ring's outer side on the face
            for sp, side in ((0.6 + 0.3 * (i % 3), 1), (-0.5 - 0.2 * (i % 2), -1)):
                t = np.clip((ph - sp) * side / 0.55, 0, 1)
                along = np.abs(dth - side * 0.35 * t * (1 if i % 2 else -1)) * (RING_R + RING_T * np.cos(ph))
                on = (t > 0) & (t < 1)
                best = np.where(on, np.minimum(best, along + 0.006 * t), best)
        return best
    def crack_mask(P):
        th, ph, dist = ring_frame(P)
        return inside(near_line(th, ph) - 0.012, 0.002) * inside(dist - RING_T - 0.03, 0.004)
    def tan_mask(P):
        th, ph, dist = ring_frame(P)
        mids = (LINES + np.roll(LINES, -1) + np.where(np.arange(len(LINES)) == len(LINES) - 1, 2 * math.pi, 0)) / 2
        m = np.zeros(len(P))
        for i, a in enumerate(mids[::2]):
            dth = np.angle(np.exp(1j * (th - a)))
            ph0 = 0.9 if i % 2 else -0.4
            m = np.maximum(m, inside(np.hypot(dth / 0.32, (ph - ph0) / 0.75) - 1, 0.05))
        return m * inside(dist - RING_T - 0.03, 0.004)
    k.paint(tan_mask, TAN, on=["donut"])
    k.paint(crack_mask, CRACK, on=["donut"])


def body_marks(k, line):
    """Black zigzag bands across the cream coils and the tail, tan patches on their tops."""
    def bands(Q):
        j, s, d = line.place(Q)
        par = line.S[j]
        every = 0.12
        zig = 0.018 * tri(d * 1.6 + 0.25)
        f = ((s + zig) / every) % 1.0
        g = np.minimum(f, 1 - f) * every
        return inside(g - 0.011, 0.002) * (par < 14.4)
    def patches(Q):
        j, s, d = line.place(Q)
        par = line.S[j]
        every = 0.17
        f = ((s + 0.045) / every) % 1.0
        g = np.minimum(f, 1 - f) * every
        return inside(g - 0.03, 0.006) * inside(d + 0.2, 0.06) * (par > 1.5) * (par < 14.4)
    k.paint(patches, TAN, on=["seg"])
    k.paint(bands, CRACK, on=["seg"])
    # the neck rising into the ring is the sack's own olive, so the ring reads whole from behind
    def neck(Q):
        j, s, d = line.place(Q)
        return inside(14.5 - line.S[j], 0.002) * (Q[:, 2] > 0.16)
    k.paint(neck, OLIVE, on=["seg"])


# --- the head out of the ring's middle ----------------------------------------------------------------------------
FACE_F = si.Face(skull=(0.0, -0.215, 0.37), skull_r=(0.088, 0.094, 0.076),
                 snout=(0.0, -0.3, 0.35), snout_r=(0.066, 0.084, 0.054), size=0.083)
EYEDIR = unit(0.74, -0.42, 0.34)


def cap(k, f):
    """The brown cap over the crown, from the brow back."""
    def mask(P):
        Q = (P - f.skull) / f.skull_r
        return inside(0.5 - Q[:, 2] - 0.3 * np.clip(-Q[:, 1], 0, 1), 0.04) * (np.abs(Q[:, 0]) < 1.3) \
            * (Q[:, 1] < 1.0) * (Q[:, 1] > -1.6)
    k.paint(mask, CAP, on=["skull", "snout"])


def fangs(k, f):
    for s in (1, -1):
        a = f.snout + np.array([0.031 * s, -0.058, -0.035])
        k.add("fang" + ("R" if s > 0 else "L"),
              hull([a + (0.009, 0.006, 0.004), a + (-0.009, 0.006, 0.004), a + (0.0, -0.007, 0.004),
                    a + (0.0, 0.004, 0.012), a + (0.0, -0.001, -0.03)]),
              FANG, "head", fuse=False, surface="shell")


def eye_open():
    """Silicobra's eye: green with a small pupil, here ringed red and shut by a brown lid."""
    base = si.eye_open(lid_edge=0.34, droop=0.1)
    R, L, K = srgb(RED), srgb(CAP), srgb(si.INK)
    def look(u, v):
        c = base(u, v)
        r2 = u * u + v * v
        ring_ = (r2 > 0.7 ** 2) & (r2 < 0.86 ** 2) & (v < 0.34 - 0.1 * u - 0.1)
        c[ring_] = R
        lid = v > 0.34 - 0.1 * u
        c[lid & (np.abs(v - (0.34 - 0.1 * u)) >= 0.12)] = L
        return c
    return look


def sandaconda(src, out_path):
    k = Kit(src, coat="scales")
    line = e.Line(LINE, RADS, rise=(0.2, 0.3))
    line.tubes(k, BONES, CREAM)
    ring(k)
    si.face(k, FACE_F, FACE)
    cap(k, FACE_F)
    cracks(k)
    body_marks(k, line)
    fangs(k, FACE_F)
    si.nostrils(k, FACE_F, NOSTRIL, ring=NOSTRIL_RING, x=0.5, z=0.25, half=(0.26, 0.26), eps=0.003)
    k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.05)
    k.eye_states(si.lay_eyes(k, FACE_F, EYEDIR, (0.036, 0.036), CAP, eye_open(), eps=0.003))
    k.export(out_path)


if __name__ == "__main__":
    sandaconda(*args())
