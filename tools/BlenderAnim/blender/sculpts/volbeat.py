# Volbeat sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/volbeat.py -- <the export's Volbeat.glb> <out.glb>
#
# From the references: an upright firefly, its head turned a little to one side. A round grey-blue face in a thick red
# hood that rolls round it like a ring and under the chin; half-lidded yellow eyes with black slit pupils set low and
# wide, a small round nose and a little frown; two yellow antennae, each rising into one curl and ending in a black tip.
# A dark navy body: the chest and the back black, the lower body's front grey-blue with two yellow bands, a glowing
# yellow bulb at the tail's end low behind; short black arms ending in points, carried to the chest, a red cuff round
# each; short black legs with pointed feet and a red cuff round each ankle; pale blue wings behind, two lobes a side.
# (The brief's "red collar and belly": the pictures' red is the hood and the cuffs; the belly is banded grey-blue.)
# Illumise is built with this script's makes (sibling("volbeat")): the wings, the arms, the legs and feet, the roll.
# Skeleton: body (0, 0, 0.28), head (0, -0.02, 0.36), ears (+-0.03, -0.03, 0.49), arms (+-0.05, -0.02, 0.31),
# wings (+-0.02, 0.03, 0.31), legs (+-0.05, 0, 0.12).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, slab, hull, ring_band, srgb, inside, unit, eye_look, args

BLACK, NAVY = "#2C2E3A", "#1C1E2A"
FACE = "#98ABD9"                   # the grey-blue face and lower body
BELLY = "#8E9FCC"
RED = "#E0384A"
YELLOW = "#F4C430"
BULB, GLOW = "#F8E070", "#FFE47A"
WING = "#C4D4F0"
E = 0.003                          # a crisp paint's edge


# --- the makes Illumise shares ---------------------------------------------------------------------------------------
def roll(c, d, R, r, seg=48, n=16):
    """A torus: a thick roll round the axis d through c, R from the axis to the roll's middle, r its thickness."""
    phi = np.linspace(0, 2 * math.pi, n, endpoint=False)
    prof = [(R + r * math.cos(p), r * math.sin(p)) for p in phi]
    prof.append(prof[0])
    return lathe(c, d, prof, seg=seg)


def wing_outline(L, W, n=24):
    t = np.linspace(0, 1, n)
    w = W * np.sqrt(np.clip(1 - ((t - 0.5) / 0.5) ** 2, 0, 1)) * (0.55 + 0.45 * t)
    a = 0.004 + t * L
    return np.vstack([np.stack([a, np.maximum(w, 0.003)], 1), np.stack([a, -np.maximum(w, 0.003)], 1)[::-1]])


def wing_axes(sweep, lift, twist):
    """On +X: outward raised by lift and swept back by sweep (degrees); the width along the body tilted by twist."""
    sw, li = math.radians(sweep), math.radians(lift)
    ex = np.array([math.cos(li) * math.cos(sw), math.cos(li) * math.sin(sw), math.sin(li)])
    ez = np.array([0.0, 1.0, math.tan(math.radians(twist))])
    ez = ez - ex * np.dot(ez, ex)
    return ex, ez / np.linalg.norm(ez)


def wings(k, root, L, colour=WING):
    """Two pale lobes a side behind the shoulders, raised in a V and swept apart."""
    root = np.array(root, float)
    for name, dz, l, w, sweep, lift, twist in (("wingU", 0.0, L, 0.32 * L, 40, 32, 35),
                                               ("wingD", -0.025, 0.78 * L, 0.27 * L, 55, 2, -20)):
        ex, ez = wing_axes(sweep, lift, twist)
        k.add(name, slab(wing_outline(l, w), root + (0, 0, dz), ex, ez, 0.012), colour, "wing{s}", fuse=False,
              mirror=True)


def arm(k, pts, radii, colour=BLACK, cuff=RED, at=0.55, cuff_r=None):
    """A short black arm carried forward to the chest, ending in a point, a cuff round its forearm (on +X; mirrored)."""
    pts = np.array(pts, float)
    k.add("arm", tube(pts, radii), colour, "arm{s}", mirror=True)
    if cuff:
        a, b = pts[1], pts[2]
        c = a + (b - a) * at
        rr = cuff_r or (radii[1] * 0.85 + radii[2] * 0.15)
        k.add("cuff", ring_band(c, b - a, rr + 0.003, 0.02, thick=0.012), cuff, "arm{s}", fuse=False, mirror=True)


def leg(k, hip, ankle, r, colour=BLACK, cuff=RED, toe=(0.012, -0.085, 0.006)):
    """A short thick black leg to a pointed foot (a hard cone forward), a cuff round the ankle (on +X; mirrored)."""
    hip, ankle = np.array(hip, float), np.array(ankle, float)
    k.add("leg", tube([hip, ankle], [r * 1.08, r]), colour, "leg{s}", mirror=True)
    pts = []
    for t in np.linspace(0, 2 * math.pi, 14, endpoint=False):
        pts.append(ankle + (r * 0.95 * math.cos(t), r * 0.95 * math.sin(t), 0.012))
        pts.append(ankle + (r * 1.0 * math.cos(t), r * 0.9 * math.sin(t) - 0.012, -0.025))
    for t in np.linspace(0, 2 * math.pi, 10, endpoint=False):
        pts.append((ankle[0] + 0.8 * r * math.cos(t), ankle[1] - 0.02 + 0.8 * r * math.sin(t), 0.0))
    pts.append((ankle[0] + toe[0], ankle[1] + toe[1], toe[2]))
    k.add("foot", hull(pts), colour, "leg{s}", fuse=False, mirror=True)
    if cuff:
        c = hip + (ankle - hip) * 0.88
        k.add("anklet", ring_band(c, ankle - hip, r * 1.03 + 0.001, 0.022, thick=0.012), cuff, "leg{s}", fuse=False,
              mirror=True)


# --- Volbeat's frame -------------------------------------------------------------------------------------------------
H, HR = np.array([0.0, -0.038, 0.418]), np.array([0.078, 0.074, 0.076])      # the face
CAP, CAPR = np.array([0.0, 0.0, 0.424]), np.array([0.113, 0.098, 0.104])   # the hood's back
RING_C, RING_D = np.array([0.0, -0.06, 0.416]), unit(0, -1, 0.3)             # the hood's roll round the face
BODY, BODYR = np.array([0.0, 0.025, 0.178]), np.array([0.102, 0.104, 0.122])  # the lower body
CHEST, CHESTR = np.array([0.0, -0.002, 0.3]), np.array([0.072, 0.066, 0.066])


def antenna(k):
    """Yellow, rising from the hood's top into one curl like a written l, the end black."""
    stem = [(0.03, -0.028, 0.49), (0.04, -0.03, 0.53), (0.058, -0.032, 0.565), (0.09, -0.034, 0.593), (0.118, -0.034, 0.583), (0.12, -0.033, 0.552), (0.097, -0.032, 0.536), (0.07, -0.031, 0.556), (0.062, -0.03, 0.595)]
    k.add("antenna", tube(stem, [0.016] * len(stem)), YELLOW, "ear{s}", fuse=False, mirror=True)
    k.add("antennatip", tube([(0.062, -0.03, 0.59), (0.066, -0.03, 0.61), (0.076, -0.03, 0.626)],
                             [0.0165, 0.0165, 0.0155]), NAVY, "ear{s}", fuse=False, mirror=True)


def eye_open(u, v):
    """Yellow under a flat heavy lid, a black slit pupil toward the inner side, a dark rim."""
    c = np.tile(srgb(YELLOW), (len(u), 1))
    c[((u + 0.12) / 0.24) ** 2 + ((v + 0.12) / 0.62) ** 2 < 1] = srgb(NAVY)
    c[u * u + v * v > 0.8 ** 2] = srgb(NAVY)
    c[v > 0.32 - 0.12 * u] = srgb(FACE)
    c[np.abs(v - (0.32 - 0.12 * u)) < 0.13] = srgb(NAVY)
    return c


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")

    # The head: the grey-blue face standing out of the red hood, the hood's roll round it and under the chin
    k.add("face", ell(H, HR), FACE, "head")
    k.add("hood", ell(CAP, CAPR), RED, "head")
    k.add("roll", roll(RING_C, RING_D, 0.084, 0.03), RED, "head", fuse=False)
    k.add("nose", ell(H + (0, -HR[1] * 0.95, -0.014), (0.017, 0.013, 0.014)), FACE, "head")
    # A little frown under the nose
    def frown(P):
        zc, R = H[2] - 0.066, 0.022
        d = np.sqrt(P[:, 0] ** 2 + (P[:, 2] - zc) ** 2)
        return (inside(np.abs(d - R) - 0.0035, E) * (P[:, 2] > zc + 0.011) * (P[:, 1] < H[1] - 0.04)
                * (np.abs(P[:, 0]) < 0.03))
    k.paint(frown, NAVY, on=["face"])
    antenna(k)

    # The body: a black chest, the lower body black behind and grey-blue in front with two yellow bands
    k.add("chest", ell(CHEST, CHESTR), BLACK, "body")
    k.add("belly", ell(BODY, BODYR), BLACK, "body")
    def front(P):
        a = np.arctan2(P[:, 0], -(P[:, 1] - BODY[1]))
        return inside(np.abs(a) - 1.55, 0.02) * inside(P[:, 2] - 0.262, E) * inside(0.07 - P[:, 2], E)
    k.paint(front, BELLY, on=["belly"])
    for lo, hi in ((0.118, 0.146), (0.19, 0.218)):
        k.paint(lambda P, lo=lo, hi=hi: front(P) * inside(P[:, 2] - hi, E) * inside(lo - P[:, 2], E), YELLOW,
                on=["belly"])
    # The tail's light, low behind
    k.add("bulb", ell((0.0, 0.118, 0.098), (0.046, 0.042, 0.042)), BULB, "body", fuse=False, glow=GLOW)

    # Arms carried forward to the chest, ending in points, a red cuff each
    arm(k, [(0.05, -0.02, 0.312), (0.085, -0.058, 0.285), (0.074, -0.105, 0.272)], [0.025, 0.022, 0.011])
    # Short thick legs, pointed feet, a red cuff round each ankle
    leg(k, (0.05, 0.0, 0.13), (0.058, -0.008, 0.058), 0.034)
    wings(k, (0.025, 0.04, 0.318), 0.17)

    k.build(voxel=0.0045, smooth=4, band=0.006)

    LID, INK = srgb(FACE), srgb(NAVY)
    EN = unit(0.5, -0.84, -0.06)

    def eyes(state):
        for s in (1, -1):
            n = EN * (s, 1, 1)
            p = H + n / math.sqrt(((n / HR) ** 2).sum())
            look = eye_look(state, eye_open, LID, INK, line=0.15)
            lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
            k.decal(p, n, (0.026, 0.025), lk, eps=0.004, rings=20, segs=56)

    k.eye_states(eyes)
    k.export(out)
