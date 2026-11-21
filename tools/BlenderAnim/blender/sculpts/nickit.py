# Nickit sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/nickit.py -- <the export's Nickit.glb> <out.glb>
# A small russet fox: a round head with a short pointed muzzle and a black nose, a black bandit's mask across the eyes
# (half-lidded yellow eyes with slit pupils), a white stroke over the brow and white flecks on the cheek; huge pointed
# black ears lined russet; a white bib on the chest; slim legs with black socks; a thin tail up from the rump into a
# great round brush that rests on the ground behind, dark below, on a black star of splayed fur.
#
# Written to be grown from (thievul.py keeps its colours, mask, eyes, ears, socks and the way of its tail's brush).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, polygon, tri, args

# --- colours ----------------------------------------------------------------------------------------------------------
RUSSET = "#C9502A"          # the coat
BLACK = "#25201F"           # the mask, the ears, the socks, the brush's underside
WHITE = "#F4F2EE"           # the bib, the brow stroke, the flecks
YELLOW, PUPIL = "#F2C21C", "#1A1410"
NOSE, MOUTH = "#1E1816", "#5A2420"
SHUT_LINE = "#C9B8A8"       # a light line for the closed eyes, which lie on the black mask

# --- the frame (skeleton: body (0, 0, 0.13), head (0, -0.07, 0.18), legs (+-0.035, -0.05, 0.12),
#     hind legs (+-0.035, 0.06, 0.12), tail (0, 0.08, 0.14)) ---------------------------------------------------------
H = np.array([0, -0.098, 0.208])
HR = np.array([0.07, 0.063, 0.063])
LEG_X = 0.034
FORE_Y, HIND_Y = -0.05, 0.062


def in_poly(x, y, pts):
    pts = np.array(pts, float)
    res = np.zeros(len(x), bool)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        res ^= ((y1 > y) != (y2 > y)) & (x < (x2 - x1) * (y - y1) / (y2 - y1 + 1e-12) + x1)
    return res


def poly_mask(x, y, pts, edge=0.002):
    pts = np.array(pts, float)
    d = np.full(len(x), 9.0)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        ex, ey = x2 - x1, y2 - y1
        t = np.clip(((x - x1) * ex + (y - y1) * ey) / (ex * ex + ey * ey + 1e-12), 0, 1)
        d = np.minimum(d, np.hypot(x - x1 - t * ex, y - y1 - t * ey))
    return inside(np.where(in_poly(x, y, pts), -d, d), edge)


def on_head(x, z, h=None, hr=None, lift=0.0):
    h = H if h is None else h
    hr = HR if hr is None else hr
    zn = (z - h[2]) / hr[2]
    q = max(0.0, 1 - zn * zn - (x / hr[0]) ** 2)
    p = np.array([x, h[1] - hr[1] * math.sqrt(q), z])
    n = (p - h) / hr ** 2; n /= np.linalg.norm(n)
    return p + n * lift, n


def taper(V):
    V = V.copy()
    t = np.clip(-V[:, 1], 0, 1)
    V[:, 0] *= 1 - 0.55 * t
    V[:, 2] *= 1 - 0.4 * t
    return V


# --- the head and the mask ----------------------------------------------------------------------------------------------
def add_head(k, h=None, hr=None, muzzle=(0.034, 0.05, 0.03), colour=RUSSET):
    h = H if h is None else h
    hr = HR if hr is None else hr
    k.add("head", ell(h, hr), colour, "head")
    k.add("muzzle", ell(h + np.array([0, -hr[1] * 0.95, -hr[2] * 0.42]), muzzle, deform=taper), colour, "head")


# The mask's outline in (arc round the head's upright axis, height over the head's centre), as fractions of HR[0]
MASK = [(0.0, -0.08), (0.45, -0.26), (0.95, -0.2), (1.3, 0.12), (1.52, 0.55), (1.12, 0.46), (0.72, 0.42),
        (0.3, 0.3), (0.0, 0.24)]


def add_mask(k, h=None, hr=None, outline=MASK, colour=BLACK):
    h = H if h is None else h
    hr = HR if hr is None else hr
    half = [(a * hr[0], z * hr[0]) for a, z in outline]
    pts = half + [(-a, z) for a, z in half[::-1][1:-1]]
    def mask(P):
        d = P - h
        a = np.arctan2(d[:, 0], -d[:, 1]) * hr[0]
        near = np.linalg.norm(d / (hr * 1.6), axis=1) < 1
        return poly_mask(a, d[:, 2], pts) * near * (d[:, 1] < hr[1] * 0.3)
    k.paint(mask, colour, on=["head", "muzzle"])


def add_marks(k, h=None, hr=None, scale=1.0):
    h = H if h is None else h
    hr = HR if hr is None else hr
    for s in (1, -1):
        # the brow stroke: a short white sweep over the mask, near the middle
        p, n = on_head(0.022 * s * scale, h[2] + hr[2] * 0.62, h, hr)
        k.decal(p, n, (0.013 * scale, 0.006 * scale), WHITE, eps=0.003, up=(0.35 * s, 0, 1),
                outline=polygon([(-1, -0.6), (0.2, 0.2), (1, 0.9), (0.6, -0.4), (-0.4, -1)]), rings=8, segs=36)
        # white flecks on the cheek under the mask's outer end
        for (x, dz, r) in ((0.05, -0.2, 0.0055), (0.06, -0.36, 0.0045)):
            p, n = on_head(x * s * scale, h[2] + hr[2] * dz, h, hr)
            k.decal(p, n, (r * scale, r * scale * 0.8), WHITE, eps=0.003, rings=5, segs=20)


def add_nose_mouth(k, h=None, hr=None, scale=1.0):
    h = H if h is None else h
    hr = HR if hr is None else hr
    tip = h + np.array([0, -hr[1] * 0.95 - 0.046 * scale, -hr[2] * 0.42 + 0.002])
    k.add("nose", ell(tip, np.array([0.011, 0.008, 0.008]) * scale), NOSE, "head", fuse=False, surface="skin")
    CR, CM = srgb(RUSSET), srgb(MOUTH)
    def smirk(u, v):
        c = np.tile(CR, (len(u), 1))
        c[(np.abs(v - (-0.1 + 0.5 * u * u)) < 0.22) & (np.abs(u) < 0.9)] = CM
        return c
    k.decal(tip + np.array([0, 0.022, -0.017]) * scale, unit(0, -1, -0.6), (0.016 * scale, 0.008 * scale), smirk,
            eps=0.0025, rings=10, segs=40)


# --- the ears: huge, pointed, black, up and a little out, lined russet ------------------------------------------------
def along(P, pts, n=200):
    pts = np.array(pts, float)
    seg = np.linalg.norm(np.diff(pts, axis=0), axis=1)
    cum = np.concatenate([[0], np.cumsum(seg)]); L = cum[-1]
    s = np.linspace(0, L, n)
    line = np.stack([np.interp(s, cum, pts[:, i]) for i in range(3)], axis=1)
    Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
    j = np.concatenate([((Q[a:a + 4000, None, :] - line[None]) ** 2).sum(2).argmin(1) for a in range(0, len(Q), 4000)])
    return s[j] / L, Q - line[j]


EAR = [(0.036, -0.09, 0.245), (0.05, -0.09, 0.3), (0.066, -0.086, 0.352), (0.084, -0.08, 0.398)]
EAR_R = [0.042, 0.036, 0.02, 0.004]


def add_ears(k, ear=EAR, radii=EAR_R, width=0.024, flat=0.34):
    k.add("ear", blade(ear, radii, (0, 1, 0), flat), BLACK, "head", mirror=True)
    d = unit(*(np.array(ear[-1]) - np.array(ear[0])))
    across = np.cross(d, (0, 1.0, 0)); across /= np.linalg.norm(across)
    def lining(P):
        t, off = along(P, ear)
        half = width * np.sin(np.pi * np.clip((t + 0.1) / 1.0, 0, 1)) ** 0.7
        return (inside(np.abs(off @ across) - half, 0.0015) * inside(off[:, 1] + 0.001, 0.0015)
                * inside(t - 0.7, 0.01) * (t > 0.04))
    k.paint(lining, RUSSET, on=["ear"])


# --- the body, the bib, the legs ----------------------------------------------------------------------------------------
def add_body(k):
    k.add("chest", ell((0, -0.035, 0.135), (0.042, 0.05, 0.046)), RUSSET, "body")
    k.add("rump", ell((0, 0.05, 0.135), (0.042, 0.05, 0.044)), RUSSET, "body")
    k.add("waist", ell((0, 0.008, 0.132), (0.038, 0.05, 0.04)), RUSSET, "body")
    k.add("neck", ell((0, -0.07, 0.17), (0.036, 0.036, 0.045), rot=(30, 0, 0)), RUSSET, "body")
    # the white bib down the front of the neck and chest
    def bib(P):
        d = P - np.array([0, -0.08, 0.15])
        return inside(np.linalg.norm(d / np.array([0.03, 0.04, 0.05]), axis=1) - 1, 0.003) * (P[:, 1] < -0.055)
    k.paint(bib, WHITE, on=["neck", "chest"])


def add_legs(k, sock=0.045):
    k.add("shoulder", ell((LEG_X - 0.004, FORE_Y, 0.11), (0.024, 0.03, 0.04)), RUSSET, "leg{s}", mirror=True)
    k.add("leg", tube([(LEG_X, FORE_Y, 0.1), (LEG_X + 0.002, FORE_Y - 0.004, 0.05), (LEG_X + 0.002, FORE_Y - 0.008, 0.016)],
                      [0.017, 0.015, 0.015]), RUSSET, "leg{s}", mirror=True)
    k.add("paw", ell((LEG_X + 0.002, FORE_Y - 0.016, 0.011), (0.017, 0.022, 0.011)), BLACK, "leg{s}", mirror=True)
    k.add("thigh", ell((LEG_X, HIND_Y + 0.004, 0.105), (0.03, 0.04, 0.048)), RUSSET, "hind{s}", mirror=True)
    k.add("hind", tube([(LEG_X + 0.002, HIND_Y + 0.012, 0.085), (LEG_X + 0.003, HIND_Y + 0.016, 0.045),
                        (LEG_X + 0.003, HIND_Y + 0.004, 0.016)], [0.018, 0.016, 0.015]), RUSSET, "hind{s}", mirror=True)
    k.add("hpaw", ell((LEG_X + 0.003, HIND_Y - 0.006, 0.011), (0.017, 0.022, 0.011)), BLACK, "hind{s}", mirror=True)
    k.paint(lambda P: inside(P[:, 2] - sock, 0.0015), BLACK, on=["leg", "hind", "paw", "hpaw"])
    k.paint(lambda P: inside(sock - P[:, 2], 0.0015), RUSSET, on=["leg", "hind"])


# --- the tail: a thin stalk up from the rump into a great round brush resting behind, dark below, on a black star ------
def add_tail(k, ball=(0.018, 0.21, 0.08), r=(0.074, 0.078, 0.076), stalk=None, star=0.1, petals=8):
    b = np.array(ball, float)
    stalk = stalk or [(0, 0.08, 0.145), (0.004, 0.12, 0.17), (0.012, 0.16, 0.16), b + (0, -0.02, 0.03)]
    k.add("tail", tube(stalk, [0.02, 0.022, 0.026, 0.04]), RUSSET, "tail")
    k.add("brush", ell(b, r), RUSSET, "tail")
    def under(P):
        d = P - b
        ang = np.arctan2(d[:, 1], d[:, 0])
        edge = -0.2 * r[2] + 0.18 * r[2] * tri(ang * 7 / math.pi) + 0.1 * r[2] * np.sin(ang * 3 + 1)
        return inside(d[:, 2] - edge, 0.002) * (np.linalg.norm(d / np.array(r), axis=1) < 1.3)
    k.paint(under, BLACK, on=["brush"])
    # the black star of splayed fur under it, lying on the ground
    for i in range(petals):
        a = 2 * math.pi * (i + 0.5) / petals
        d = np.array([math.cos(a), math.sin(a), 0.0])
        root = b + np.array([0, 0, -r[2] * 0.72]) + d * r[0] * 0.4
        tip = root + d * star * 0.62
        tip[2] = 0.012
        k.add(f"star{i}", blade([root, (root + tip) / 2 + (0, 0, 0.004), tip], [0.03, 0.034, 0.022],
                                (0, 0, 1), 0.45), BLACK, "tail")


# --- the eyes: half-lidded almonds on the mask, yellow with a slit pupil ------------------------------------------------
EYE = [(-1, -0.15), (-0.55, 0.32), (0.3, 0.42), (1.0, 0.3), (0.92, -0.25), (0.35, -0.75), (-0.45, -0.7)]


def eye_colour():
    CY, CP = srgb(YELLOW), srgb(PUPIL)
    def eye(u, v):
        c = np.tile(CY, (len(u), 1))
        c[np.hypot((u + 0.05) / 0.2, (v + 0.05) / 0.62) < 1] = CP
        c[np.hypot((u - 0.32) / 0.14, (v - 0.12) / 0.16) < 1] = (1, 1, 1)
        return c
    return eye


def lay_eyes(k, state, x=0.034, dz=0.006, half=(0.019, 0.015), h=None, hr=None, turn=1.2, outline=EYE):
    h = H if h is None else h
    hr = HR if hr is None else hr
    p, n = on_head(x, h[2] + dz, h, hr)
    n = unit(n[0] * turn, n[1], n[2])
    look = eye_look(state, eye_colour(), srgb(BLACK), srgb(SHUT_LINE), line=0.18)
    q = np.array([-1, 1, 1.0])
    for s in (1, -1):
        m = q if s < 0 else np.ones(3)
        k.decal(p * m, n * m, half, look if s > 0 else (lambda u, v: look(-u, v)), eps=0.0042,
                outline=polygon([(a * s, b) for a, b in outline]), rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    add_body(k)
    add_legs(k)
    add_head(k)
    add_ears(k)
    add_tail(k)
    add_mask(k)
    add_marks(k)
    add_nose_mouth(k)
    k.build(voxel=0.0036, band=0.004)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)
