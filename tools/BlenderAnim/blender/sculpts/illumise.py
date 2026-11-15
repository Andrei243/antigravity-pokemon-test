# Illumise sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/illumise.py -- <the export's Illumise.glb> <out.glb>
#
# Volbeat's counterpart, built with volbeat.py's makes (sibling("volbeat")): its wings, arms, pointed legs and roll.
# From the references: a round light blue face in a lavender hood that runs over the top of the head from cheek to
# cheek and rolls up into a big curl at each cheek; two yellow antennae, long pointed spindles rising in a V; large
# eyes, white with a big blue iris and a dark pupil, a dark lid line along the top; a small smile. A dark grey dress
# from the neck to the feet, a light blue front panel with two darker lines down it, a yellow scalloped bib at the
# neck, lavender rolls over the shoulders and round the hem; short dark arms ending in points, held out and down; short
# dark legs with pointed feet; small pale blue wings behind. (The brief's "blue hair-like curls" are lavender in the
# pictures, the blue is the face and the panel, and the yellow band is the bib.)
# Skeleton: body (0, 0, 0.26), head (0, -0.01, 0.34), ears (+-0.03, -0.02, 0.46), arms (+-0.05, -0.01, 0.27),
# wings (+-0.04, 0.04, 0.3), legs (+-0.04, 0, 0.08).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, polygon, outward, args, sibling

v = sibling("volbeat")
DRESS, INK = "#3E3E4A", "#1E2030"
FACE = "#6CC0EE"
PANEL, PANEL_LINE = "#78C8F0", "#3A84BC"
LAV = "#AE96E0"
BIB = "#F2D66E"
YELLOW = v.YELLOW
IRIS, PUPIL, WHITE = "#2A84D0", "#14203A", "#FFFFFF"
E = 0.003

H, HR = np.array([0.0, -0.028, 0.39]), np.array([0.08, 0.074, 0.078])        # the face
CAP, CAPR = np.array([0.0, 0.014, 0.4]), np.array([0.098, 0.088, 0.092])     # the hood's back
DRESSC, DRESSR = np.array([0.0, 0.005, 0.198]), np.array([0.09, 0.08, 0.136])


def flare(V):
    V = V.copy()
    s = 1 + 0.16 * np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= s; V[:, 1] *= s
    return V


def hood(k):
    """A lavender roll over the head from cheek to cheek, each end rolled up into a curl."""
    c = np.array([0.0, -0.048, 0.392])
    R = 0.088
    a = np.array([1.0, 0, 0]); b = unit(0, 0.18, 1)          # the roll's plane, its top leaning back a little
    arc = [c + R * (math.cos(t) * a + math.sin(t) * b) for t in np.radians(np.linspace(-12, 192, 18))]
    k.add("hoodroll", tube(arc, [0.027] * len(arc)), LAV, "head", fuse=False)
    for s in (1, -1):
        cc = c + R * (math.cos(math.radians(-38)) * a * s + math.sin(math.radians(-38)) * b) + (0.006 * s, -0.004, 0)
        u = np.array([s * 1.0, 0, 0]); w = unit(0, 0.25, 1)
        pts, rad = [], []
        for i, t in enumerate(np.linspace(0, 1.25, 22)):
            ang = math.pi / 2 + (-s) * 0 + 2 * math.pi * t * (-1)
            r = 0.04 * (1 - 0.72 * t / 1.25)
            pts.append(cc + r * (math.cos(ang) * u + math.sin(ang) * w) - (0, 0.004 * t, 0))
            rad.append(0.022 - 0.008 * t / 1.25)
        k.add(f"curl{'R' if s > 0 else 'L'}", tube(pts, rad, seg=16), LAV, "head", fuse=False)


def antenna(k):
    """A yellow spindle rising in a V from the hood's top, its flat face to the front."""
    root = np.array([0.03, -0.02, 0.468])
    d = unit(0.4, 0.05, 0.92)
    L = 0.12
    w = np.cross((0, -1.0, 0), d); w /= np.linalg.norm(w)
    t = np.cross(d, w)
    V, F = ell((0, 0, 0), (0.022, 0.016, L / 2), deform=lambda P: P * np.array([1, 1, 1.0])[None]
               * np.c_[1 - 0.25 * np.clip(-P[:, 2], 0, 1), 1 - 0.25 * np.clip(-P[:, 2], 0, 1), np.ones(len(P))], sub=3)
    V = V @ np.stack([w, t, d]) + root + d * (L / 2 - 0.01)
    k.add("antenna", outward((V, F)), YELLOW, "ear{s}", fuse=False, mirror=True)


def eye_open(u, v_):
    """White, a big blue iris with a dark pupil and a glint, a dark lid line along the top."""
    c = np.tile(srgb(WHITE), (len(u), 1))
    c[((u + 0.12) / 0.66) ** 2 + ((v_ + 0.08) / 0.74) ** 2 < 1] = srgb(IRIS)
    c[((u + 0.12) / 0.32) ** 2 + ((v_ + 0.06) / 0.4) ** 2 < 1] = srgb(PUPIL)
    c[((u + 0.0) / 0.2) ** 2 + ((v_ - 0.28) / 0.17) ** 2 < 1] = srgb(WHITE)
    c[(u * u + v_ * v_ > 0.8 ** 2) & (v_ > -0.25)] = srgb(INK)
    return c


EYE_OUT = polygon([(1.0, 0.1), (0.75, 0.62), (0.3, 0.9), (-0.3, 0.88), (-0.8, 0.55), (-1.0, 0.0), (-0.8, -0.6),
                   (-0.3, -0.9), (0.3, -0.92), (0.8, -0.6)])


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")

    # The head: the face standing out of the hood, the roll and its curls
    k.add("face", ell(H, HR), FACE, "head")
    k.add("hood", ell(CAP, CAPR), LAV, "head")
    hood(k)
    def smile(P):
        zc, R = H[2] - 0.035, 0.018
        d = np.sqrt(P[:, 0] ** 2 + (P[:, 2] - zc) ** 2)
        return (inside(np.abs(d - R) - 0.0035, E) * (P[:, 2] < zc - 0.009) * (P[:, 1] < H[1] - 0.04)
                * (np.abs(P[:, 0]) < 0.025))
    k.paint(smile, INK, on=["face"])
    antenna(k)

    # The dress, flaring to the feet; the blue panel and its lines; the bib; the lavender rolls
    k.add("dress", ell(DRESSC, DRESSR, deform=flare), DRESS, "body")
    k.add("neck", ell((0, -0.01, 0.318), (0.06, 0.055, 0.04)), DRESS, "body")
    def panel(P):
        return inside(np.abs(P[:, 0]) - 0.046, E) * inside(P[:, 2] - 0.3, E) * inside(0.115 - P[:, 2], E) \
            * (P[:, 1] < DRESSC[1] - 0.03)
    k.paint(panel, PANEL, on=["dress", "neck"])
    k.paint(lambda P: panel(P) * inside(np.abs(np.abs(P[:, 0]) - 0.016) - 0.0028, 0.002) * inside(P[:, 2] - 0.27, E),
            PANEL_LINE, on=["dress"])
    def bib(P):
        x = P[:, 0]
        lo = 0.272 - 0.012 * np.abs(np.sin(x / 0.06 * 1.5 * math.pi))
        return inside(np.abs(x) - 0.06, E) * inside(lo - P[:, 2], E) * inside(P[:, 2] - 0.33, E) * (P[:, 1] < -0.02)
    k.paint(bib, BIB, on=["dress", "neck"])
    # Shoulder rolls over the arms' tops, and the hem's roll
    for s in (1, -1):
        k.add(f"shoulder{'R' if s > 0 else 'L'}", tube([(0.04 * s, -0.045, 0.312), (0.078 * s, -0.04, 0.293),
                                                         (0.1 * s, -0.03, 0.262), (0.106 * s, -0.012, 0.232)],
                                                        [0.019, 0.021, 0.021, 0.018]), LAV, "body", fuse=False)
    zc = 0.086
    rz = math.sqrt(max(0.0, 1 - ((zc - DRESSC[2]) / DRESSR[2]) ** 2)) * (1 + 0.16 * (DRESSC[2] - zc) / DRESSR[2])
    hem = [(DRESSR[0] * rz * math.sin(t) * 1.0, DRESSC[1] + DRESSR[1] * rz * math.cos(t), zc)
           for t in np.linspace(0.25, 2 * math.pi + 0.05, 40)]
    k.add("hem", tube(hem, [0.019] * len(hem)), LAV, "body", fuse=False)

    # Arms out and down to points, no cuffs; legs short with pointed feet; small wings
    v.arm(k, [(0.05, -0.01, 0.275), (0.105, -0.04, 0.235), (0.128, -0.07, 0.205)], [0.026, 0.022, 0.011],
          colour=DRESS, cuff=None)
    v.leg(k, (0.04, 0.0, 0.1), (0.046, -0.006, 0.05), 0.031, colour=DRESS, cuff=None)
    v.wings(k, (0.035, 0.045, 0.3), 0.13)

    k.build(voxel=0.0045, smooth=4, band=0.006)

    LID, CI = srgb(FACE), srgb(INK)
    EN = unit(0.46, -0.86, 0.08)

    def eyes(state):
        for s in (1, -1):
            n = EN * (s, 1, 1)
            p = H + n / math.sqrt(((n / HR) ** 2).sum())
            look = eye_look(state, eye_open, LID, CI, line=0.15)
            lk = look if s > 0 else (lambda f: (lambda a, b: f(-a, b)))(look)
            ol = EYE_OUT if s > 0 else (lambda th: EYE_OUT(math.pi - th))
            k.decal(p, n, (0.027, 0.031), lk, eps=0.004, outline=ol, rings=20, segs=56)

    k.eye_states(eyes)
    k.export(out)
