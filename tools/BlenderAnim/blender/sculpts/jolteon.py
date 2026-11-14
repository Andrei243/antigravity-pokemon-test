# Jolteon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/jolteon.py -- <the export's Jolteon.glb> <out.glb>
# Eevee grown sharp and lean: the same frame of four legs and a trunk, the same round head and way of making the eyes,
# nose and mouth, but bright yellow fur that stands out in spikes; a collar of white spikes round the neck; long
# pointed ears swept back, lined dark; spikes of fur on the cheeks, along the back and in a burst over the haunches,
# where Eevee's tail was; longer legs; dark almond eyes with a white glint and a small black nose on a longer muzzle.
# Grown from eevee.py (copied, not imported), as the other evolutions are.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, tri, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

# --- colours ----------------------------------------------------------------------------------------------------------
YELLOW = "#F5CD38"         # the coat: the drawing's warm yellow, a little deeper than the 3D model's lemon
WHITE = "#F0F0EC"          # the collar
LINING = "#2C2236"         # inside the ears: the drawing's dark purple-black
EYE, PUPIL = "#2A1E2E", "#0F0A12"
NOSE, INK = "#1C1418", "#3A2A1A"

# --- the frame (the skeleton's joints: body (0, 0.03, 0.26), head joint (0, -0.13, 0.41), ears (+-0.05, -0.13, 0.52),
#     forelegs (+-0.05, -0.08, 0.24), hind legs (+-0.05, 0.13, 0.24), tail (0, 0.15, 0.28)) ---------------------------
H = np.array([0, -0.155, 0.485])         # the head's centre, over and in front of its joint (as Eevee's)
HR = np.array([0.103, 0.096, 0.097])     # as large against the body as Eevee's: the family's big head
HS = HR[0] / 0.084                       # the face's offsets below were laid on a head of 0.084: scaled to this one
LEG_X = 0.05
FORE_Y, HIND_Y = -0.08, 0.13
BODY_Z = 0.275
LEG_TOP = 0.27


def head_shape(V):
    V = V.copy()
    V[:, 0] *= 1 + 0.06 * np.clip(-V[:, 2], 0, 1) * (1 - np.clip(-V[:, 2], 0, 1))
    return V


def add_head():
    k.add("head", ell(H, HR, deform=head_shape), YELLOW, "head")
    # A longer muzzle than Eevee's, tapering to the nose
    k.add("muzzle", ell(H + HS * np.array((0, -0.066, -0.032)), HS * np.array((0.04, 0.036, 0.028))), YELLOW, "head")
    k.add("snout", ell(H + HS * np.array((0, -0.094, -0.036)), HS * np.array((0.024, 0.022, 0.019))), YELLOW, "head")
    # A short thick neck: the head sits on the shoulders, the collar round it
    k.add("neck", tube([(0, -0.06, 0.3), (0, -0.1, 0.37), (0, -0.14, 0.43)], [0.062, 0.058, 0.06]), YELLOW, "head")


def add_body():
    # A deep, sturdy trunk (the drawing's, not the skeleton's thin line), a little longer than Eevee's
    k.add("chest", ell((0, -0.06, BODY_Z + 0.005), (0.078, 0.085, 0.1)), YELLOW, "body")
    k.add("middle", ell((0, 0.035, BODY_Z - 0.008), (0.07, 0.11, 0.088)), YELLOW, "body")
    k.add("rump", ell((0, 0.12, BODY_Z - 0.008), (0.076, 0.075, 0.094)), YELLOW, "body")


def add_legs(color=YELLOW):
    # Eevee's legs and paws, longer: a broad shoulder and a broad thigh cover the upper half of each leg, the lower leg
    # tapers but stays sturdy, and the paws are clear
    k.add("shoulder", ell((LEG_X, FORE_Y + 0.005, 0.215), (0.045, 0.054, 0.075)), color, "leg{s}", mirror=True)
    k.add("leg", tube([(LEG_X - 0.004, FORE_Y + 0.005, LEG_TOP), (LEG_X, FORE_Y, 0.13), (LEG_X + 0.002, FORE_Y - 0.004, 0.03)],
                      [0.042, 0.035, 0.031]), color, "leg{s}", mirror=True)
    k.add("paw", ell((LEG_X + 0.003, FORE_Y - 0.017, 0.021), (0.036, 0.045, 0.023)), color, "leg{s}", mirror=True)
    k.add("thigh", ell((LEG_X - 0.002, HIND_Y + 0.008, 0.21), (0.05, 0.066, 0.085)), color, "hind{s}", mirror=True)
    k.add("hind", tube([(LEG_X, HIND_Y + 0.014, 0.2), (LEG_X + 0.002, HIND_Y + 0.012, 0.11),
                        (LEG_X + 0.002, HIND_Y - 0.002, 0.03)], [0.04, 0.034, 0.031]), color, "hind{s}", mirror=True)
    k.add("hpaw", ell((LEG_X + 0.003, HIND_Y - 0.015, 0.021), (0.036, 0.045, 0.023)), color, "hind{s}", mirror=True)


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


def spike(name, base, d, ln, r, flat, bone, color=YELLOW, mirror=False):
    """A spike of fur: a short blade from base along d, fused at its foot, flattened across flat."""
    base = np.array(base, float); d = unit(*d)
    k.add(name, blade([base, base + d * ln * 0.5, base + d * ln], [r, r * 0.55, 0.003], flat, 0.5), color, bone,
          mirror=mirror)


# --- the ears: long and pointed, swept up and back from the top of the head, flat with their faces forward and up;
#     the dark lining on their front faces with a yellow rim round it -------------------------------------------------
EAR = [(0.058, -0.152, 0.555), (0.084, -0.11, 0.615), (0.106, -0.064, 0.67), (0.124, -0.018, 0.72)]
EAR_R = [0.052, 0.05, 0.034, 0.004]
ED = unit(*(np.array(EAR[-1]) - np.array(EAR[0])))
EN = np.array([0, -1.0, 0]) - ED * ED[1]; EN /= np.linalg.norm(EN)     # the ear's front face
EA = np.cross(ED, EN)


def add_ears():
    k.add("ear", blade(EAR, EAR_R, EN, 0.3), YELLOW, "ear{s}", mirror=True)

    def lining(P):
        t, off = along(P, EAR)
        half = 0.028 * np.sin(np.pi * np.clip((t - 0.02) / 0.94, 0, 1)) ** 0.7
        w = off @ EA
        front = off @ EN
        return (inside(np.abs(w) - half, 0.0025) * inside(-front + 0.002, 0.002) * inside(t - 0.9, 0.01)
                * (P[:, 2] > 0.565))
    k.paint(lining, LINING, on=["ear"])


# --- the collar: a mass of white fur round the neck with spikes all round it, longest down the chest -------------------
RUFF = np.array([0, -0.12, 0.34])


def add_collar():
    k.add("ruff", ell(RUFF, (0.088, 0.078, 0.074)), WHITE, "body")
    for i in range(5):                                   # round the back of the neck
        a = math.pi / 2 + math.pi * i / 4
        c = (0.078 * math.sin(a), -0.09 - 0.05 * math.cos(a), 0.37)
        k.add(f"ruffback{i}", ell(c, (0.034, 0.034, 0.032)), WHITE, "body")
    # Spikes along the lower edge, pointing down and out, the front ones longest
    for i, a in enumerate(np.linspace(-1.4, 1.4, 9)):
        base = np.array((0.074 * math.sin(a), RUFF[1] - 0.058 * math.cos(a), 0.3))
        d = unit(0.7 * math.sin(a), -0.55 * math.cos(a), -1.0)
        ln = (0.12 if i % 2 == 0 else 0.09) - 0.02 * abs(a)
        spike(f"rufftuft{i}", base, d, ln, 0.03, (math.sin(a), -math.cos(a), 0.0), "body", WHITE)
    # A higher row in front, shorter, pointing forward and down
    for i, a in enumerate(np.linspace(-0.9, 0.9, 4)):
        base = np.array((0.07 * math.sin(a), RUFF[1] - 0.064 * math.cos(a), 0.345))
        d = unit(0.6 * math.sin(a), -1.0 * math.cos(a), -0.7)
        spike(f"rufffront{i}", base, d, 0.065, 0.028, (math.sin(a), -math.cos(a), 0.0), "body", WHITE)
    # At each side, spikes out, back and down over the shoulder
    for i, (z, d) in enumerate(((0.37, (0.8, 0.6, 0.2)), (0.325, (0.8, 0.7, -0.45)))):
        spike(f"ruffside{i}", (0.07, -0.1, z), d, 0.085, 0.03, (0, 0, 1), "body", WHITE, mirror=True)


# --- spikes of yellow fur: on the cheeks, on the crown, along the back, and a burst over each haunch -------------------
def add_spikes():
    spike("cheek0", H + HS * np.array((0.068, 0.0, -0.025)), (0.75, 0.6, -0.25), 0.05, 0.022, (0, 0, 1), "head", mirror=True)
    spike("cheek1", H + HS * np.array((0.06, 0.03, 0.01)), (0.6, 0.8, 0.1), 0.045, 0.02, (0, 0, 1), "head", mirror=True)
    spike("crown", H + np.array((0, 0.0, HR[2] - 0.014)), (0, 0.5, 1), 0.045, 0.024, unit(0.77, -0.64, 0), "head")
    # Along the back: two on the midline, their faces turned 40 degrees toward +X, and a pair to each side
    spike("back0", (0, 0.0, 0.345), (0, 0.8, 1), 0.075, 0.034, unit(0.77, -0.64, 0), "body")
    spike("back1", (0.03, 0.06, 0.34), (0.4, 0.9, 0.9), 0.08, 0.034, (0, 0, 1), "body", mirror=True)
    # The burst over the haunches, where Eevee's tail was: a fan of spikes back and out, up and down
    for i, (ang, ln, out_) in enumerate(((70, 0.11, 0.4), (35, 0.135, 0.6), (5, 0.135, 0.8), (-28, 0.1, 0.9))):
        a = math.radians(ang)
        d = (out_, math.cos(a), math.sin(a))
        spike(f"burst{i}", (0.04, 0.165, 0.3 + 0.01 * math.sin(a)), d, ln, 0.038, unit(-math.sin(a) * 0.3, 0, 1)
              if abs(ang) < 50 else (1, 0, 0), "tail", mirror=True)
    spike("burstmid", (0, 0.18, 0.31), (0, 1, 0.45), 0.12, 0.038, unit(0.77, 0, 0.64), "tail")
    # Spikes up and out over each hip, so the burst shows from behind and not only end-on
    spike("hipup", (0.05, 0.11, 0.33), (0.85, 0.35, 0.6), 0.085, 0.034, (0, 1, 0), "tail", mirror=True)
    spike("hipout", (0.065, 0.14, 0.28), (1, 0.45, 0.05), 0.08, 0.032, (0, 0, 1), "tail", mirror=True)
    # A spike low on each haunch, back and down
    spike("haunch", (0.07, 0.16, 0.21), (0.5, 0.9, -0.5), 0.065, 0.03, (0, 0, 1), "hind{s}", mirror=True)


# --- the face: patches on the fused head, laid before the body's build ------------------------------------------------
CY, CI = srgb(YELLOW), srgb(INK)


def add_nose(at=H + HS * np.array((0, -0.116, -0.027)), half=(0.012, 0.009), color=NOSE):
    k.decal(at, unit(0, -1, 0.15), half, color, eps=0.003, outline=polygon([(-1, 0.7), (1, 0.7), (0, -1)]),
            rings=6, segs=24)


def mouth(u, v):
    """A small smile: one clean line, dipping in the middle and turning up at the corners."""
    c = np.tile(CY, (len(u), 1))
    c[(np.abs(v - (-0.2 + 1.4 * (np.abs(u) - 0.45) ** 2)) < 0.26) & (np.abs(u) < 0.88)] = CI
    return c


def add_mouth(at=H + HS * np.array((0, -0.107, -0.05)), half=(0.024, 0.014), look=mouth):
    k.decal(at, unit(0, -1, -0.25), half, look, eps=0.003, rings=16, segs=72)


# The eyes: Eevee's construction, but almond: wider than tall and slanting up to the outer corner, dark with a darker
# middle and a white glint; no rim, the yellow of the face is their lid
EYE_DIR = unit(0.47, -0.88, 0.02)
EYE_HALF = (0.034, 0.028)
EYE_UP = unit(-0.4, 0, 1)                 # turns the almond so its outer corner rises
CE, CP = srgb(EYE), srgb(PUPIL)


def almond(th):
    """An almond: a top arc flatter than the bottom, pointed at both corners."""
    c, s = math.cos(th), math.sin(th)
    return 1.0 / max((abs(c) ** 1.6 + abs(s) ** 1.6) ** (1 / 1.6), 1e-6) * (0.92 if s > 0 else 1.0)


def eye(u, v):
    c = np.tile(CE, (len(u), 1))
    c[((u + 0.05) / 0.5) ** 2 + ((v + 0.05) / 0.75) ** 2 < 1] = CP
    c[((u - 0.12) / 0.24) ** 2 + ((v - 0.32) / 0.3) ** 2 < 1] = (1, 1, 1)
    return c
E = H + EYE_DIR * 0.082 * HS + np.array([0, 0, 0.004])


def eyes(state):
    k.decal(E, EYE_DIR, EYE_HALF, eye_look(state, eye, CY, CI), up=EYE_UP, eps=0.004, outline=almond, rings=20,
            segs=56, mirror=True)


add_body()
add_legs()
add_head()
add_ears()
add_collar()
add_spikes()
add_nose()
add_mouth()

k.build(voxel=0.0052)
k.eye_states(eyes)
k.export(out)
