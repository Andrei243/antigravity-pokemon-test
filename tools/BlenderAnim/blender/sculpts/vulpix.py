# Vulpix sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/vulpix.py -- <the export's Vulpix.glb> <out.glb>
# A small fox in a warm red-brown coat: a big head with a short pointed muzzle, large pointed ears lined dark brown,
# big brown eyes with a dark middle and a white glint, a small black nose and a little open mouth; a crown of curled
# orange hair on the head; a cream patch on the chest; dark-brown socks on all four feet with a ragged top; six orange
# tails fanned out behind, each curling over at its end.
#
# Written to be grown from (Ninetales keeps this frame: the trunk, the legs, the head, the way the eyes, nose, mouth
# and tails are made). Each part of the frame is a constant or a small function below, the calls at the end.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, tri, unit, eye_look, polygon, lumpy, args

src, out = args()
k = Kit(src, coat="fur")

# --- colours ----------------------------------------------------------------------------------------------------------
COAT = "#D4825A"           # between the drawing's red-brown and the 3D model's orange-tan
TAILS = "#F2873A"          # the tails and the crown of curls: a brighter orange than the coat
CREAM = "#F4E6BC"          # the chest
SOCK = "#7E4A2E"           # the feet
LINING = "#6E3E24"         # inside the ears
EYE, PUPIL = "#7A4C34", "#2C1810"   # brown eyes with a near-black middle
NOSE, INK = "#1E1412", "#2A1610"
MOUTH = "#9A3E44"

# --- the frame (the skeleton's joints: body (0, 0.04, 0.2), head (0, -0.09, 0.26), ears (+-0.05, -0.1, 0.36),
#     forelegs (+-0.07, -0.09, 0.15), hind legs (+-0.07, 0.15, 0.15), tail (0, 0.17, 0.22)) ------------------------------
H = np.array([0, -0.14, 0.35])          # the head's centre, over and in front of its joint
HR = np.array([0.104, 0.096, 0.094])     # its radii
LEG_X = 0.06                             # the legs' distance from the midline
FORE_Y, HIND_Y = -0.085, 0.145           # where the fore and hind legs stand
BODY_Z = 0.205                           # the trunk's middle height
LEG_TOP = 0.2                            # where a leg's tube begins under the trunk


def head_shape(V):
    """The head: round, a little fuller at the cheeks below its middle."""
    V = V.copy()
    V[:, 0] *= 1 + 0.08 * np.clip(-V[:, 2], 0, 1) * (1 - np.clip(-V[:, 2], 0, 1))
    return V


def add_head(color=COAT):
    k.add("head", ell(H, HR, deform=head_shape), color, "head")
    # A short pointed fox's muzzle standing forward under the eyes
    k.add("muzzle", ell(H + (0, -0.088, -0.04), (0.046, 0.048, 0.034), rot=(-8, 0, 0)), color, "head")
    # A short thick neck from the shoulders up into the head
    k.add("neck", tube([(0, -0.06, 0.24), (0, -0.095, 0.29), (0, -0.12, 0.33)], [0.066, 0.062, 0.06]), color, "head")


def add_body(color=COAT):
    # A short deep trunk: chest, middle, rump
    k.add("chest", ell((0, -0.07, BODY_Z), (0.082, 0.086, 0.095)), color, "body")
    k.add("middle", ell((0, 0.03, BODY_Z - 0.003), (0.076, 0.11, 0.09)), color, "body")
    k.add("rump", ell((0, 0.135, BODY_Z), (0.08, 0.076, 0.092)), color, "body")


def add_legs(color=COAT):
    # Short legs with small round paws; a shoulder and a thigh over the top of each
    k.add("shoulder", ell((LEG_X - 0.004, FORE_Y + 0.004, 0.175), (0.038, 0.045, 0.06)), color, "leg{s}", mirror=True)
    k.add("leg", tube([(LEG_X - 0.003, FORE_Y + 0.004, LEG_TOP), (LEG_X, FORE_Y, 0.1), (LEG_X + 0.002, FORE_Y - 0.004, 0.03)],
                      [0.039, 0.035, 0.033]), color, "leg{s}", mirror=True)
    k.add("paw", ell((LEG_X + 0.002, FORE_Y - 0.016, 0.02), (0.037, 0.045, 0.023)), color, "leg{s}", mirror=True)
    k.add("thigh", ell((LEG_X - 0.004, HIND_Y + 0.004, 0.175), (0.044, 0.056, 0.066)), color, "hind{s}", mirror=True)
    k.add("hind", tube([(LEG_X, HIND_Y + 0.01, LEG_TOP - 0.02), (LEG_X + 0.002, HIND_Y + 0.008, 0.1),
                        (LEG_X + 0.002, HIND_Y - 0.002, 0.03)], [0.039, 0.035, 0.033]), color, "hind{s}", mirror=True)
    k.add("hpaw", ell((LEG_X + 0.002, HIND_Y - 0.014, 0.02), (0.037, 0.045, 0.023)), color, "hind{s}", mirror=True)


def add_socks(top=0.062, color=SOCK, coat=COAT):
    """Dark socks on all four feet, their top edge ragged; the coat painted back above it, so the edge is crisp."""
    def edge(P):
        a = np.arctan2(P[:, 1] - np.where(P[:, 1] > 0.03, HIND_Y, FORE_Y), np.abs(P[:, 0]) - LEG_X)
        return top + 0.007 * tri(a * 4 / math.pi)
    LEGS = ["leg", "paw", "hind", "hpaw"]
    k.paint(lambda P: inside(edge(P) - P[:, 2], 0.0025) * (P[:, 2] < top + 0.05), coat, on=LEGS)
    k.paint(lambda P: inside(P[:, 2] - edge(P), 0.0025), color, on=LEGS)


def add_chest(color=CREAM):
    """A cream patch on the front of the chest, between the forelegs, from under the neck down to the belly."""
    c = np.array([0, -0.14, 0.19])
    k.paint(lambda P: inside(((P[:, 0] / 0.05) ** 2 + ((P[:, 2] - c[2]) / 0.06) ** 2) ** 0.5 - 1, 0.04)
            * (P[:, 1] < -0.1) * (np.abs(P[:, 0]) < 0.05), color, on=["chest"])


def along(P, pts, n=200):
    """Nearest place on a polyline (|x| taken, so a mirrored part's both sides): its share of the length and the offset."""
    pts = np.array(pts, float)
    seg = np.linalg.norm(np.diff(pts, axis=0), axis=1)
    cum = np.concatenate([[0], np.cumsum(seg)]); L = cum[-1]
    s = np.linspace(0, L, n)
    line = np.stack([np.interp(s, cum, pts[:, i]) for i in range(3)], axis=1)
    Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
    j = np.concatenate([((Q[a:a + 4000, None, :] - line[None]) ** 2).sum(2).argmin(1) for a in range(0, len(Q), 4000)])
    return s[j] / L, Q - line[j]


# --- the ears: large and pointed, broad at the root, leaning out from the top corners of the head; flat front to back,
#     the dark lining on their front faces -----------------------------------------------------------------------------
EAR = [(0.055, -0.15, 0.41), (0.08, -0.145, 0.46), (0.105, -0.138, 0.51), (0.125, -0.132, 0.55)]
EAR_R = [0.05, 0.044, 0.026, 0.004]


def add_ears(color=COAT, lining=LINING):
    k.add("ear", blade(EAR, EAR_R, (0, 1, 0), 0.3), color, "ear{s}", mirror=True)
    d = unit(*(np.array(EAR[-1]) - np.array(EAR[0])))
    across = np.cross(d, (0, 1.0, 0)); across /= np.linalg.norm(across)

    def lin(P):
        t, off = along(P, EAR)
        half = 0.036 * np.sin(np.pi * np.clip((t + 0.12) / 1.0, 0, 1)) ** 0.7
        w = off @ across
        front = -off[:, 1]
        return (inside(np.abs(w) - half, 0.0025) * inside(-front + 0.002, 0.002) * inside(t - 0.88, 0.01)
                * (P[:, 2] > 0.415))
    k.paint(lin, lining, on=["ear"])


# --- curls: a lobe of fur that runs out from its root along d and curls over toward e at its end, a spiral of a turn
#     and a bit; the tails and the crown are made of them -------------------------------------------------------------
def curl(base, d, e, straight, R, r_root, r_mid, turns=1.15, n=26):
    """Points and radii of a curling lobe: straight along d, then a shrinking spiral turning toward e."""
    d = unit(*d); e = np.array(e, float); e = e - d * (e @ d); e /= np.linalg.norm(e)
    pts, rad = [], []
    for i in range(n):
        t = i / (n - 1)
        if t < 0.35:
            s = t / 0.35
            a, b = straight * s, 0.0
            r = r_root + (r_mid - r_root) * smoothstep(0, 1, s)
        else:
            s = (t - 0.35) / 0.65
            phi = s * turns * 2 * math.pi
            rr = R * (1 - 0.62 * s)
            a, b = straight + rr * math.sin(phi), -R + rr * math.cos(phi)
            r = r_mid * (1 - 0.82 * s ** 1.1)
        pts.append(np.array(base, float) + a * d - b * e)
        rad.append(r)
    return pts, rad


# --- the tails: six orange lobes from the rump, fanned out behind in a half ring so all six show from behind and the
#     outer ones past the body from the front three-quarter, each rising back and up and curling over outward -----------
TAIL_ROOT = np.array([0, 0.19, 0.235])
# (angle round the back's line from straight up, toward +X; lean back; length before the curl; curl radius; thickness)
TAIL_FAN = [(-72, 0.4, 0.12, 0.04, 0.038), (-44, 0.3, 0.14, 0.042, 0.04), (-15, 0.25, 0.16, 0.044, 0.042),
            (15, 0.25, 0.16, 0.044, 0.042), (44, 0.3, 0.14, 0.042, 0.04), (72, 0.4, 0.12, 0.04, 0.038)]


def add_tails(color=TAILS):
    back, up, side = np.array([0, 1.0, 0]), np.array([0, 0, 1.0]), np.array([1.0, 0, 0])
    for i, (ang, lean, ln, R, th) in enumerate(TAIL_FAN):
        a = math.radians(ang)
        d = unit(*(back * lean + up * math.cos(a) + side * math.sin(a) * 0.85))
        e = back * 0.35 + side * math.copysign(1.0, ang) - up * 0.25      # the tips hook apart along the fan's rim
        base = TAIL_ROOT + d * 0.03
        pts, rad = curl(base, d, e, ln, R, 0.019, th)
        k.add(f"tail{i}", tube(pts, rad), color, "tail")
    k.add("tailroot", ell(TAIL_ROOT + (0, 0.01, 0.01), (0.042, 0.04, 0.04)), color, "tail")


# --- the crown: three curled locks of orange hair on top of the head, the middle one tallest, curling back --------------
def add_crown(color=TAILS):
    top = H + (0, 0.01, HR[2] * 0.75)
    k.add("crownbase", ell(top, (0.06, 0.06, 0.035), deform=lumpy(3, 2, 0.08)), color, "head")
    for i, (x, y, d, e, ln, R, th) in enumerate((
            (0.0, -0.01, (0, -0.25, 1), (0, 1, 0), 0.05, 0.04, 0.036),       # the middle one up and curling back
            (0.03, -0.045, (0.35, -0.6, 0.75), (0.2, -1, 0), 0.03, 0.03, 0.028),  # a side lock forward, curling out
            (0.0, 0.05, (0, 0.55, 0.85), (0, 1, -0.3), 0.03, 0.032, 0.03))):    # one behind, back and down
        base = top + (x, y, 0)
        pts, rad = curl(base, d, e, ln, R, 0.03, th)
        k.add(f"crown{i}", tube(pts, rad), color, "head")


# --- the face: patches on the fused head, laid before the body's build -------------------------------------------------
CB, CI = srgb(COAT), srgb(INK)


def add_nose(at=H + (0, -0.135, -0.025), half=(0.013, 0.01), color=NOSE):
    """A small black nose at the tip of the muzzle, a rounded triangle point down."""
    k.decal(at, unit(0, -1, 0.25), half, color, eps=0.003, outline=polygon([(-1, 0.6), (1, 0.6), (0.3, -0.9), (-0.3, -0.9)]),
            rings=6, segs=28)


def mouth(u, v):
    """A small open mouth: a dark-red oval with an ink edge."""
    c = np.tile(CI, (len(u), 1))
    c[(u / 0.72) ** 2 + ((v + 0.05) / 0.66) ** 2 < 1] = srgb(MOUTH)
    return c


def add_mouth(at=H + (0, -0.124, -0.062), half=(0.015, 0.012), look=mouth):
    k.decal(at, unit(0, -1, -0.15), half, look, eps=0.003, rings=12, segs=48)


# The eyes: big ovals, a little taller than wide, brown with a near-black middle and a white glint up and out; no rim,
# the coat round them their lid
EYE_DIR = unit(0.5, -0.86, 0.04)
EYE_HALF = (0.027, 0.032)
CE, CP = srgb(EYE), srgb(PUPIL)


def eye(u, v):
    c = np.tile(CE, (len(u), 1))
    c[((u + 0.02) / 0.74) ** 2 + ((v - 0.02) / 0.8) ** 2 < 1] = CP
    c[((u - 0.24) / 0.3) ** 2 + ((v - 0.4) / 0.27) ** 2 < 1] = (1, 1, 1)
    return c


E = H + EYE_DIR * HR[0] * 0.9 + np.array([0, 0, 0.004])


def eyes(state):
    k.decal(E, EYE_DIR, EYE_HALF, eye_look(state, eye, CB, CI), eps=0.004, rings=20, segs=56, mirror=True)


if __name__ == "__main__":
    add_body()
    add_legs()
    add_head()
    add_ears()
    add_crown()
    add_tails()
    add_chest()
    add_socks()
    add_nose()
    add_mouth()

    k.build(voxel=0.0055, band=0.004)
    k.eye_states(eyes)
    k.export(out)
