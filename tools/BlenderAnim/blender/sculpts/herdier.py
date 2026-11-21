# Herdier sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/herdier.py -- <the export's Herdier.glb> <out.glb>
# Lillipup grown (lillipup.py, whose tufts, ears and lining, legs and paws, nose and eye laying it borrows). From the
# references: a tan terrier; a big head with a short snout and a black nose; a cream brow of pointed tufts spread like
# an X over the eyes (two up, two out to the sides, one up the middle); white eyes with a dark iris, cut flat by the
# brow; a great cream moustache hanging from the snout past the chin in ragged points; tall pointed ears, a darker
# lining; brown tufts at the back of the cheeks; a shaggy navy cape over the back, shoulders and chest, its lower edge
# in long points; tan legs below it; a navy tail held up, jagged along its back.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, tri, unit, eye_look, polygon, args, sibling

L = sibling("lillipup")

TAN, CREAM, NAVY = "#C9814F", "#EFD9A6", "#2F4278"
LINING, NOSE = "#9A5844", "#18161A"
WHITE, IRIS, PUPIL, INK = "#F6F4EE", "#4A4E5A", "#141418", "#3A2418"

# The frame (joints: body (0, 0, 0.18), head (0, -0.1, 0.26), ears (+-0.04, -0.13, 0.34), legs (+-0.055, -0.08, 0.14),
# hinds (+-0.055, 0.1, 0.14), tail (0, 0.14, 0.23))
HC = np.array([0.0, -0.158, 0.296])       # the skull
HR = np.array([0.078, 0.07, 0.072])
SC = HC + np.array((0, -0.06, -0.022))    # the snout
SR = np.array((0.042, 0.04, 0.034))
LX, FY, HY = 0.055, -0.085, 0.1
BZ = 0.17
BROW = (0.3, 0.28)                        # the eye's top under the brow: v = BROW[0] + BROW[1] u


def eye_top(u):
    return BROW[0] + BROW[1] * u


def clipped(top, n=96):
    """The unit circle cut off above v = top(u): an eye's outline under a brow."""
    pts = []
    for th in np.linspace(0, 2 * math.pi, n, endpoint=False):
        du, dv = math.cos(th), math.sin(th)
        t = 1.0
        if dv > top(du):
            lo, hi = 0.0, 1.0
            for _ in range(40):
                t = (lo + hi) / 2
                if t * dv > top(t * du):
                    hi = t
                else:
                    lo = t
            t = lo
        pts.append((t * math.cos(th), t * math.sin(th)))
    return pts


def terrier_eye(white, iris, pupil, top):
    """A white eye, a big dark iris toward the nose, a black pupil, a glint; a dark edge under the brow's cut."""
    W, I, P, K = srgb(white), srgb(iris), srgb(pupil), srgb(INK)
    def look(u, v):
        c = np.tile(W, (len(u), 1))
        r = np.sqrt(((u + 0.12) / 0.62) ** 2 + ((v + 0.02) / 0.72) ** 2)
        c[r < 1] = I
        c[r < 0.6] = P
        c[((u - 0.06) / 0.2) ** 2 + ((v - 0.2) / 0.2) ** 2 < 1] = W
        c[v > top(u) - 0.18] = K
        return c
    return look


def brow(k, at, spikes, col):
    """A cream brow of pointed tufts from a point between the eyes: (direction, length, root radius) each."""
    k.add("browbase", ell(at, (0.05, 0.022, 0.022)), col, "head")
    for i, (d, ln, r) in enumerate(spikes):
        L.tuft(k, f"brow{i}", np.array(at) + unit(*d) * 0.01, d, ln, r, col, "head", flat=(0, 1, 0), f=0.45)


def moustache(k, top, col, width=0.05, drop=0.1, tufts=4, r=0.026):
    """A great cream moustache: a lobe each side of the snout hanging down, its lower edge in ragged points."""
    top = np.array(top, float)
    for s in (1, -1):
        c = top + np.array((s * width * 0.6, -0.004, -drop * 0.35))
        k.add(f"stache{'R' if s > 0 else 'L'}", ell(c, (width * 0.72, 0.022, drop * 0.56), rot=(0, s * 22, 0)), col, "head")
        for j in range(tufts):
            t = (j + 0.5) / tufts
            base = c + np.array((s * width * (t - 0.15) * 1.25, -0.002, -drop * 0.32))
            d = (s * (0.15 + 0.55 * t), -0.12, -1)
            L.tuft(k, f"stachetuft{'R' if s > 0 else 'L'}{j}", base, d, drop * (0.7 - 0.2 * abs(t - 0.45)), r, col,
                   "head", flat=(0, 1, 0), f=0.5)


def cape(k, c, r, col, n=18, drop=0.06, z_edge=None, name="cape"):
    """A shaggy cape: a mass over the back and points hanging round its lower edge, faces turned outward."""
    c, r = np.array(c, float), np.array(r, float)
    k.add(name, ell(c, r), col, "body")
    ze = c[2] - r[2] * 0.35 if z_edge is None else z_edge
    for i in range(n):
        th = 2 * math.pi * (i + 0.5) / n
        sx, cy = math.sin(th), -math.cos(th)            # 0 = straight ahead
        s = math.sqrt(max(1 - ((ze - c[2]) / r[2]) ** 2, 0.05))
        base = c + np.array((r[0] * s * sx * 0.92, r[1] * s * cy * 0.92, ze - c[2]))
        out = np.array((sx, cy * 0.5, 0.0))
        d = out * 0.38 + np.array((0, 0, -1.0))
        ln = drop * (1.15 if i % 2 else 0.85)
        L.tuft(k, f"{name}tuft{i}", base, d, ln, 0.036, col, "body", flat=(sx, cy, 0), f=0.45)


def herdier(src, out):
    k = Kit(src, coat="fur")

    # The trunk, tan under the cape, the haunches fused into it
    k.add("chest", ell((0, -0.07, BZ + 0.01), (0.07, 0.07, 0.072)), TAN, "body")
    k.add("middle", ell((0, 0.02, BZ), (0.066, 0.11, 0.064)), TAN, "body")
    k.add("rump", ell((0, 0.1, BZ + 0.008), (0.066, 0.06, 0.066)), TAN, "body")
    k.add("neck", tube([(0, -0.08, 0.2), (0, -0.12, 0.25), (0, -0.14, 0.28)], [0.056, 0.052, 0.05]), TAN, "head")

    # Legs: a shoulder and thigh under the cape, a short shin, a round paw with toes
    PR = (0.03, 0.036, 0.02)
    pf = L.leg(k, LX, FY, 0.13, 0.029, PR, TAN, "leg{s}", "leg", shoulder=(0.04, 0.048, 0.06))
    ph = L.leg(k, LX, HY, 0.13, 0.029, PR, TAN, "hind{s}", "hind", shoulder=(0.044, 0.056, 0.065))
    L.toe_lines(k, [pf, ph], PR, "#8A4E30")

    # The head: a round skull, a short snout, tufts of brown at the back of the cheeks
    k.add("head", ell(HC, HR), TAN, "head")
    k.add("snout", ell(SC, SR), TAN, "head")
    for s in (1, -1):
        for j, (dz, ln) in enumerate(((0.0, 0.05), (-0.03, 0.042))):
            base = HC + np.array((s * 0.058, 0.012, dz))
            L.tuft(k, f"cheek{'R' if s > 0 else 'L'}{j}", base, (s, 0.45, 0.1 - 0.5 * j), ln, 0.022, TAN, "head",
                   flat=(0, 0, 1), f=0.5)

    # Tall pointed ears, up and a little out and back, a dark lining
    EB, ET = np.array([0.04, -0.14, 0.335]), np.array([0.072, -0.118, 0.475])
    L.ear(k, EB, ET, 0.038, TAN)
    L.ear_lining(k, EB + (0.002, 0, 0.012), ET, 0.024, LINING)

    # The cream brow in an X over the eyes, and the moustache from the snout
    brow(k, HC + (0, -0.058, 0.026), [((0.3, 0.2, 1), 0.11, 0.03), ((-0.3, 0.2, 1), 0.11, 0.03),
                                       ((0, 0.25, 1), 0.085, 0.026), ((1, 0.1, 0.25), 0.1, 0.028),
                                       ((-1, 0.1, 0.25), 0.1, 0.028)], CREAM)
    moustache(k, SC + (0, -0.03, -0.012), CREAM, width=0.058, drop=0.12)

    # The shaggy navy cape over the back, shoulders and chest
    cape(k, (0, 0.012, 0.198), (0.085, 0.172, 0.076), NAVY, n=20, drop=0.095)
    # The cape over the tops of the shoulders and thighs too, where they bulge through it
    k.paint(lambda P: inside(0.125 + 0.012 * tri(P[:, 1] * 30) - P[:, 2], 0.003) * (P[:, 1] > -0.11) * (P[:, 1] < 0.19)
            * (np.abs(P[:, 0]) > 0.03) * (P[:, 2] < 0.26), NAVY)
    k.add("capefront", ell((0, -0.1, 0.2), (0.07, 0.04, 0.06)), NAVY, "body")
    for i, x in enumerate((-0.045, -0.015, 0.015, 0.045)):
        L.tuft(k, f"bib{i}", (x, -0.135, 0.18), (x * 3, -0.35, -1), 0.06 if i in (1, 2) else 0.05, 0.026, NAVY, "body",
               flat=(0, 1, 0), f=0.45)

    # The navy tail held up and a little back, out to +X, jagged along its back
    T = [(0, 0.15, 0.23), (0.012, 0.18, 0.3), (0.025, 0.2, 0.37), (0.034, 0.206, 0.43)]
    k.add("tail", blade(T, [0.032, 0.03, 0.022, 0.006], unit(1, -0.5, 0), 0.6), NAVY, "tail")
    for i, (t, ln) in enumerate(((0.3, 0.045), (0.55, 0.04), (0.78, 0.035))):
        a = int(t * 3); f = t * 3 - a
        p = np.array(T[a]) * (1 - f) + np.array(T[a + 1]) * f
        L.tuft(k, f"tailtuft{i}", p + (0, 0.012, 0), (0.25, 1, 0.55), ln, 0.016, NAVY, "tail", flat=(1, -0.4, 0), f=0.5)

    # The black nose on the snout's tip
    L.nose(k, SC + (0, -0.038, 0.012), (0.017, 0.012, 0.012), NOSE)

    k.build(voxel=0.005, smooth=3, band=0.005)

    # The eyes: white with a dark iris, cut flat by the brow
    ex, ez = 0.036, HC[2] + 0.004
    E = L.on_ell(HC, HR, ex, ez) + (0, -0.001, 0)
    n = unit(0.38, -1, 0.06)
    look = terrier_eye(WHITE, IRIS, PUPIL, eye_top)
    out_ = clipped(eye_top)
    k.eye_states(lambda s: L.lay_eyes(k, s, E, n, (0.02, 0.019), look, TAN, INK, outline=out_))
    k.export(out)


if __name__ == "__main__":
    herdier(*args())
