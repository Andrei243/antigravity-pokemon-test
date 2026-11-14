# Flareon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/flareon.py -- <the export's Flareon.glb> <out.glb>
# Grown from eevee.py (its frame of legs, trunk, head, eyes, nose and mouth), refitted to Flareon's taller skeleton:
# an orange-red fox, stockier than Eevee, with a great cream-yellow mane over the chest and round the neck, a cream
# pompadour curling forward over the brow, long ears lined dark slate, dark eyes with a white glint, a small dark nose
# and a little cat's mouth, and a huge bushy cream tail swung out to the model's +X side.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, tri, unit, eye_look, polygon, lumpy, args

src, out = args()
k = Kit(src, coat="fur")

# --- colours ----------------------------------------------------------------------------------------------------------
ORANGE = "#EC7430"         # the coat: the 3D model's orange-red
CREAM = "#F7E6A2"          # the mane, the crown and the tail: a warmer, yellower cream than Eevee's
LINING = "#38404E"         # inside the ears: dark slate
EYE, PUPIL = "#24202A", "#0E0B10"   # near-black eyes, a darker middle
NOSE, INK = "#2A1612", "#2A1410"

# --- the frame (the skeleton's joints: body 0.22 up, head joint (0, -0.13, 0.37), ears (+-0.05, -0.13, 0.48),
#     forelegs (+-0.05, -0.08, 0.20), hind legs (+-0.05, 0.13, 0.20), tail (0, 0.16, 0.24)) ------------------------------
# Eevee's frame moved up 0.1 and forward 0.05 at the head, the legs half as long again, the trunk longer and deeper.
H = np.array([0, -0.152, 0.45])           # the head's centre, over and in front of its joint
HR = np.array([0.122, 0.106, 0.11])       # larger than Eevee's (0.112, 0.099, 0.103), as the drawing's
LEG_X = 0.056
FORE_Y, HIND_Y = -0.082, 0.13
BODY_Z = 0.24


def head_shape(V):
    V = V.copy()
    V[:, 0] *= 1 + 0.06 * np.clip(-V[:, 2], 0, 1) * (1 - np.clip(-V[:, 2], 0, 1))
    return V


def add_head():
    k.add("head", ell(H, HR, deform=head_shape), ORANGE, "head")
    k.add("muzzle", ell(H + (0, -0.08, -0.036), (0.046, 0.033, 0.03)), ORANGE, "head")
    k.add("neck", tube([(0, -0.06, 0.27), (0, -0.1, 0.34), (0, -0.135, 0.4)], [0.068, 0.064, 0.064]), ORANGE, "head")


def add_body():
    # A stocky trunk: deeper and wider than Eevee's, its belly covering the tops of the legs
    k.add("chest", ell((0, -0.07, BODY_Z - 0.005), (0.084, 0.088, 0.1)), ORANGE, "body")
    k.add("middle", ell((0, 0.03, BODY_Z - 0.01), (0.078, 0.115, 0.095)), ORANGE, "body")
    k.add("rump", ell((0, 0.12, BODY_Z - 0.012), (0.082, 0.074, 0.096)), ORANGE, "body")


def add_legs(color=ORANGE):
    # Eevee's legs, thicker: a broad shoulder over the top half, the lower leg tapering to a clear paw
    k.add("shoulder", ell((LEG_X - 0.004, FORE_Y + 0.004, 0.175), (0.045, 0.052, 0.07)), color, "leg{s}", mirror=True)
    k.add("leg", tube([(LEG_X - 0.004, FORE_Y + 0.006, 0.21), (LEG_X, FORE_Y, 0.11), (LEG_X + 0.002, FORE_Y - 0.004, 0.03)],
                      [0.042, 0.036, 0.033]), color, "leg{s}", mirror=True)
    k.add("paw", ell((LEG_X + 0.002, FORE_Y - 0.018, 0.02), (0.033, 0.041, 0.022)), color, "leg{s}", mirror=True)
    k.add("thigh", ell((LEG_X - 0.004, HIND_Y + 0.006, 0.19), (0.05, 0.064, 0.08)), color, "hind{s}", mirror=True)
    k.add("hind", tube([(LEG_X, HIND_Y + 0.012, 0.17), (LEG_X + 0.002, HIND_Y + 0.008, 0.1),
                        (LEG_X + 0.002, HIND_Y - 0.002, 0.03)], [0.037, 0.032, 0.031]), color, "hind{s}", mirror=True)
    k.add("hpaw", ell((LEG_X + 0.002, HIND_Y - 0.016, 0.02), (0.033, 0.041, 0.022)), color, "hind{s}", mirror=True)


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


# --- the ears: Eevee's long pointed ears, wider and a little longer, lined dark slate -------------------------------
EAR = [(0.046, -0.135, 0.5), (0.085, -0.13, 0.575), (0.125, -0.124, 0.65), (0.158, -0.118, 0.715)]
EAR_R = [0.05, 0.049, 0.033, 0.004]


def add_ears():
    k.add("ear", blade(EAR, EAR_R, (0, 1, 0), 0.32), ORANGE, "ear{s}", mirror=True)
    d = unit(*(np.array(EAR[-1]) - np.array(EAR[0])))
    across = np.cross(d, (0, 1.0, 0)); across /= np.linalg.norm(across)

    def lining(P):
        t, off = along(P, EAR)
        half = 0.038 * np.sin(np.pi * np.clip((t + 0.12) / 1.0, 0, 1)) ** 0.7
        w = off @ across
        front = -off[:, 1]
        return (inside(np.abs(w) - half, 0.0025) * inside(-front + 0.002, 0.002) * inside(t - 0.9, 0.01)
                * (P[:, 2] > 0.5))
    k.paint(lining, LINING, on=["ear"])


# --- the mane: one deep mass of cream fur on the chest under the chin and round the neck, flaring out at the sides,
#     with broad pointed tufts along its lower edge (a ring of balls reads as a rolled collar: none here) --------------
RUFF = np.array([0, -0.17, 0.29])


def add_mane():
    k.add("mane", ell(RUFF, (0.116, 0.09, 0.112), deform=lumpy(5, 3, 0.05)), CREAM, "body")
    # Round the back of the neck: one collar mass, thinner, under the head
    k.add("manecollar", ell((0, -0.1, 0.345), (0.1, 0.075, 0.07), deform=lumpy(4, 2, 0.05)), CREAM, "body")
    # Broad pointed tufts along the lower edge, pointing down and forward, the middle ones longest
    for i, a in enumerate(np.linspace(-1.2, 1.2, 5)):
        base = np.array((0.085 * math.sin(a), RUFF[1] + 0.01 - 0.06 * math.cos(a), 0.215))
        d = unit(0.45 * math.sin(a), -0.45 * math.cos(a), -1.0)
        ln = 0.08 - 0.015 * abs(a)
        k.add(f"manetuft{i}", blade([base, base + d * ln * 0.55, base + d * ln], [0.038, 0.022, 0.003],
                                    (math.sin(a), -math.cos(a), 0.0), 0.5), CREAM, "body")
    # The mane flares out at each side of the neck in a row of broad short points, so its edge reads as fur
    for j, (z, d) in enumerate(((0.36, unit(1, -0.1, 0.35)), (0.315, unit(1, -0.15, -0.05)),
                                (0.265, unit(1, -0.2, -0.5)))):
        base = np.array((0.1, -0.15, z))
        k.add(f"maneside{j}", blade([base, base + d * 0.025, base + d * 0.045], [0.032, 0.02, 0.003], (0, 1, 0), 0.5),
              CREAM, "body", mirror=True)


# --- the crown: a cream pompadour from the top of the head curling forward over the brow ----------------------------
def add_crown():
    # A loose piece seated in the skull, so its edge stays crisp and no cream bleeds over the back of the head
    # It rises about a third of the head's height over the skull, then curls forward and down to the brow
    T = HR[2]
    pts = [H + (0, 0.025, 0.5 * T), H + (0, -0.01, T + 0.012), H + (0, -0.06, T + 0.03), H + (0, -0.1, T + 0.005),
           H + (0, -0.112, 0.62 * T)]
    V, F = tube(pts, [0.044, 0.058, 0.054, 0.038, 0.016])
    # A roll wider than it is thick, on the midline, narrowing toward its root at the back so it reads as a ridge there
    V = V.copy(); V[:, 0] *= 1.1 + 0.4 * np.clip((H[1] + 0.02 - V[:, 1]) / 0.06, 0, 1)
    k.add("crown", (V, F), CREAM, "head", fuse=False)


# --- the tail: a huge bushy cream plume from the rump, back and up, swung about 40 degrees out to the model's +X side,
#     flattened across its swing, with broad points of fur along its outer edge -----------------------------------
TAIL = [(0.0, 0.15, 0.25), (0.04, 0.21, 0.3), (0.1, 0.255, 0.38), (0.145, 0.27, 0.465), (0.17, 0.265, 0.54)]
TAIL_R = [0.04, 0.095, 0.115, 0.092, 0.01]


def add_tail():
    k.add("tail", blade(TAIL, TAIL_R, unit(1, -0.6, 0), 0.68), CREAM, "tail")
    k.add("tailroot", ell((0, 0.155, 0.255), (0.042, 0.04, 0.04)), ORANGE, "tail")
    side = unit(1, -0.6, 0)
    back = unit(0.6, 1, 0)
    for i, (t, ang, ln) in enumerate(((0.35, -0.3, 0.05), (0.5, 0.2, 0.06), (0.65, 0.7, 0.055), (0.78, 1.1, 0.05),
                                      (0.55, -0.9, 0.045))):
        n = len(TAIL) - 1
        f = t * n; a = int(f); s = f - a
        c = np.array(TAIL[a]) * (1 - s) + np.array(TAIL[a + 1]) * s
        r = TAIL_R[a] * (1 - s) + TAIL_R[a + 1] * s
        d = unit(*(math.cos(ang) * back + math.sin(ang) * np.array((0, 0, 1.0))))
        base = c + d * r * 0.7
        k.add(f"tailtuft{i}", blade([base, base + d * ln * 0.55, base + d * ln], [0.032, 0.02, 0.003], side, 0.45),
              CREAM, "tail")


# --- the face: patches on the fused head, laid before the body's build (Eevee's) ---------------------------------------
CB, CI = srgb(ORANGE), srgb(INK)


def add_nose(at=H + (0, -0.112, -0.028), half=(0.011, 0.008), color=NOSE):
    k.decal(at, unit(0, -1, 0.1), half, color, eps=0.003, outline=polygon([(-1, 0.7), (1, 0.7), (0, -1)]),
            rings=6, segs=24)


def mouth(u, v):
    c = np.tile(CB, (len(u), 1))
    c[(np.abs(v - (-0.2 + 1.4 * (np.abs(u) - 0.45) ** 2)) < 0.26) & (np.abs(u) < 0.88)] = CI
    return c


def add_mouth(at=H + (0, -0.107, -0.056), half=(0.022, 0.013), look=mouth):
    k.decal(at, unit(0, -1, -0.2), half, look, eps=0.003, rings=16, segs=72)


# The eyes: Eevee's ovals, a little smaller against the head and near black, with the white glint up and out
EYE_DIR = unit(0.47, -0.88, -0.02)
EYE_HALF = (0.024, 0.032)
CE, CP = srgb(EYE), srgb(PUPIL)


def eye(u, v):
    c = np.tile(CE, (len(u), 1))
    c[((u + 0.05) / 0.62) ** 2 + ((v + 0.08) / 0.7) ** 2 < 1] = CP
    c[((u - 0.2) / 0.32) ** 2 + ((v - 0.38) / 0.28) ** 2 < 1] = (1, 1, 1)
    return c
E = H + EYE_DIR * HR[0] * 0.86 + np.array([0, 0, 0.0])


def eyes(state):
    k.decal(E, EYE_DIR, EYE_HALF, eye_look(state, eye, CB, CI), eps=0.004, rings=20, segs=56, mirror=True)


add_body()
add_legs()
add_head()
add_ears()
add_mane()
add_crown()
add_tail()
add_nose()
add_mouth()

k.build(voxel=0.006, band=0.004)
k.eye_states(eyes)
k.export(out)
