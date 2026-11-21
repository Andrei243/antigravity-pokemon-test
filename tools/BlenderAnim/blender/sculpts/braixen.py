# Braixen sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Fennekin's script (fennekin.py, imported: its colours, ear lining, cheek tufts, nose and mouth):
#   blender --background --factory-startup --python sculpts/braixen.py -- <the export's Braixen.glb> <out.glb>
# A fox standing upright: a yellow head with a long pointed snout, white below the eyes and on the cheeks, a small red
# nose, narrow sly red eyes under a dark lid line; tall yellow ears lined red, from each a great plume of red-orange fur
# streaming out to the side; a fluffy white ruff on the chest; thin white arms hanging forward in front of a flared
# yellow skirt of fur with a ragged hem; long black legs with broad thighs under the skirt, bent back at the hock; a
# huge bushy yellow tail hanging behind to the model's +X, red-orange at its tip, a forked twig standing up out of it.
#
# Written to be grown from (delphox.py keeps its head, ears and plumes, eyes and legs, lifted to its own joints).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, tri, eye_look, polygon, args, sibling

f = sibling("fennekin")
YELLOW, FUR, WHITE, INK = f.YELLOW, f.FUR, f.WHITE, f.INK
BLACK = "#3C3A40"
WOOD = "#8A5A36"
NOSE = "#E0442C"
IRIS, PUPIL = "#D8402A", "#2A0C08"

# --- the frame (the skeleton's joints: body (0, 0, 0.256), arms (+-0.05, -0.01, 0.326), head (0, -0.01, 0.356), ears
#     (+-0.03, -0.01, 0.461), legs (+-0.035, 0, 0.16), tail (0, 0.05, 0.196)) ----------------------------------------
# Ratios on the 3D reference / this sculpt: the head of the height 0.26 / 0.22 (the plumes above it), the legs showing
# under the skirt 0.32 / 0.32, the skirt's width 0.4 / 0.43, the tail's height 0.36 / 0.48.
H = np.array([0, -0.028, 0.412])
HR = np.array([0.07, 0.064, 0.066])


def taper(V):
    V = V.copy()
    t = np.clip(-V[:, 1], 0, 1)
    V[:, 0] *= 1 - 0.55 * t
    V[:, 2] *= 1 - 0.4 * t
    return V


def add_head(k, h=H, hr=HR, snout=1.0, color=YELLOW, white=WHITE, neck=None):
    k.add("head", ell(h, hr, deform=f.cheeky), color, "head")
    k.add("muzzle", ell(h + np.array([0, -0.06, -0.024]) * (1, snout, 1), (0.03, 0.05 * snout, 0.025), rot=(-8, 0, 0),
                        deform=taper), color, "head")
    k.add("neck", tube(neck or [h + (0, 0.02, -0.105), h + (0, 0.01, -0.06)], [0.03, 0.03]), color, "head")
    # white below the eyes: the snout's lower half and the cheeks, a crisp line rising from the nose to the cheeks
    def m(P):
        line = h[2] - 0.012 + 0.014 * np.clip((P[:, 1] - (h[1] - 0.1 * snout)) / (0.09 * snout), 0, 1) \
            + 0.012 * np.clip((np.abs(P[:, 0]) - 0.03) / 0.03, 0, 1)
        return inside(P[:, 2] - line, 0.0018) * (P[:, 1] < h[1] + 0.03) * (P[:, 2] > h[2] - 0.08) * (np.abs(P[:, 0]) < 0.075)
    k.paint(m, white, on=["head", "muzzle"])


# --- the ears: tall and pointed, a little out, lined red; from the base of each a plume of fur streaming out ----------
def ears(h):
    return [h + q for q in ((0.032, 0.004, 0.044), (0.048, 0.008, 0.095), (0.062, 0.014, 0.14), (0.07, 0.02, 0.18))]


EAR_R = [0.036, 0.033, 0.019, 0.003]
# each plume: points from the ear's root, and their radii; the main one out and a little up, a lower one out and down,
# a short one up past the ear's back
PLUMES = [([(0.035, -0.012, 0.062), (0.085, -0.012, 0.07), (0.135, 0.0, 0.078), (0.178, 0.016, 0.1), (0.2, 0.026, 0.126)],
           [0.026, 0.034, 0.032, 0.02, 0.004]),
          ([(0.04, -0.01, 0.05), (0.09, -0.004, 0.04), (0.135, 0.008, 0.022), (0.158, 0.016, 0.0)],
           [0.022, 0.024, 0.016, 0.003]),
          ([(0.04, 0.0, 0.07), (0.07, 0.012, 0.11), (0.1, 0.022, 0.135)], [0.022, 0.017, 0.003])]


def add_ears(k, h=H, plumes=PLUMES, scale=1.0, color=YELLOW, fur=FUR):
    f.add_ears(k, ear=ears(h), radii=EAR_R, color=color, fur=fur, width=0.022, flat=0.32, points=[])
    for i, (pts, rr) in enumerate(plumes):
        P = [h + np.array(p) * (1, 1, 1) * scale for p in pts]
        k.add(f"plume{i}", blade(P, [r * scale for r in rr], (0, 1, 0.2), 0.55), fur, "ear{s}", mirror=True)


# --- the eyes: almonds slanting up to the outside, a red iris with a dark pupil and a glint, a dark lid line along the
#     top cut straight (no ring); the white of the cheek below, the yellow of the brow above ------------------------------
SLANT = 0.1


def eye_top(u):
    return 0.45 + SLANT * u


def eye_bottom(u):
    return eye_top(u) - 1.35 * np.clip(1 - u * u, 0, 1) ** 0.42


ALMOND = [(u, eye_top(u)) for u in np.linspace(1, -1, 9)] + [(u, eye_bottom(u)) for u in np.linspace(-0.92, 0.92, 13)]


def eye_open(state, iris=IRIS, pupil=PUPIL, lid=YELLOW):
    CI, CP, CL, CK = srgb(iris), srgb(pupil), srgb(lid), srgb(INK)
    def look(u, v):
        c = np.tile(CI, (len(u), 1))
        top = np.minimum(eye_top(u), -0.05 + 0.75 * u) if state == "Fierce" else eye_top(u)
        c[((u + 0.06) / 0.32) ** 2 + ((v + 0.1) / 0.5) ** 2 < 1] = CP
        c[((u - 0.2) / 0.2) ** 2 + ((v - 0.1) / 0.2) ** 2 < 1] = (1, 1, 1)
        c[(v > top - 0.17) & (v <= top)] = CK
        c[v > top] = CL
        return c
    return look


def lowered(look, lid, low, line=-0.3):
    """A closed eye's lid: the brow's yellow above, the cheek's white below, as the face is painted round the eye."""
    def g(u, v):
        c = look(u, v)
        keep = np.all(np.isclose(c, lid[None]), axis=1) & (v < line)
        c[keep] = low
        return c
    return g


def lay_eyes(k, state, h=H, hr=HR, d=unit(0.44, -0.88, 0.12), half=(0.027, 0.027), iris=IRIS, lid=YELLOW, low=WHITE):
    E = h + d * hr[0] * 0.9 + np.array([0, 0, 0.004])
    for side in (1, -1):
        m = np.array([side, 1, 1.0])
        flip = (lambda g: g) if side == 1 else (lambda g: (lambda u, v: g(-u, v)))
        outline = polygon([(side * u, v) for u, v in (ALMOND if side == 1 else ALMOND[::-1])])
        if state in ("Open", "Fierce"):
            look = eye_open(state, iris=iris, lid=lid)
        else:
            shut = eye_look(state, eye_open("Open", iris=iris, lid=lid), srgb(lid), srgb(INK), line=0.2)
            look = lowered(shut, srgb(lid), srgb(low))
        k.decal(E * m, d * m, half, flip(look), eps=0.004, outline=outline, rings=20, segs=56)


def add_face(k, h=H, snout=1.0, nose=NOSE):
    tip = h + np.array([0, -0.108 * snout, -0.022])
    k.decal(tip, unit(0, -1, 0.3), (0.0095, 0.008), nose, eps=0.0025,
            outline=polygon([(-1, 0.6), (1, 0.6), (0.3, -0.9), (-0.3, -0.9)]), rings=6, segs=28)
    f.add_mouth(k, h + np.array([0, -0.088 * snout, -0.041]), n=unit(0, -1, -0.3), half=(0.02, 0.009),
                look=f.smile(WHITE, INK))


# --- legs: black, broad thighs under the skirt, bent back at the hock, small feet ------------------------------------
def add_legs(k, z=0.0, color=BLACK):
    HIP, KN, AN, TO = (np.array(p) + (0, 0, z) for p in ((0.044, 0.004, 0.215), (0.07, -0.03, 0.12),
                                                        (0.078, 0.022, 0.05), (0.08, -0.022, 0.014)))
    k.add("thigh", tube([HIP, (HIP + KN) / 2 + (0.006, 0, 0), KN], [0.04, 0.036, 0.024]), color, "leg{s}", mirror=True)
    k.add("shin", tube([KN, AN], [0.022, 0.0155]), color, "leg{s}", mirror=True)
    k.add("foot", tube([AN, TO], [0.016, 0.016]), color, "leg{s}", mirror=True)
    k.add("toes", ell(TO + (0, -0.008, -0.002), (0.019, 0.024, 0.013)), color, "leg{s}", mirror=True)


# --- the body: a slim yellow trunk, the white ruff on the chest, the skirt of fur --------------------------------------
def flare(V):
    """A skirt: widest at the hem, narrowing up to the waist."""
    V = V.copy()
    s = 0.62 + 0.38 * np.clip((1 - V[:, 2]) / 2, 0, 1) ** 0.7
    V[:, 0] *= s; V[:, 1] *= s
    return V


def add_body(k):
    k.add("chest", ell((0, -0.004, 0.33), (0.046, 0.04, 0.05)), YELLOW, "body")
    k.add("waist", ell((0, 0.0, 0.285), (0.05, 0.045, 0.04)), YELLOW, "body")
    k.add("skirt", ell((0, 0.006, 0.245), (0.126, 0.104, 0.064), deform=flare), YELLOW, "body")
    # the hem's points: a few big ones round the edge, pointing down and out
    for i, a in enumerate(np.linspace(0, 2 * math.pi, 11, endpoint=False)):
        c, s = math.sin(a), -math.cos(a)
        b = np.array([0.104 * c, 0.006 + 0.086 * s, 0.2])
        d = unit(0.35 * c, 0.35 * s, -1)
        ln = 0.04 + 0.012 * (i % 2)
        k.add(f"hem{i}", blade([b, b + d * ln * 0.5, b + d * ln], [0.026, 0.018, 0.003], (c, s, 0), 0.5), YELLOW, "body")
    # the ruff: a soft white mass over the chest, its lower edge in points
    k.add("ruff", ell((0, -0.032, 0.338), (0.05, 0.034, 0.042)), WHITE, "body")
    for i, (x, ln) in enumerate(((-0.026, 0.03), (0.0, 0.04), (0.026, 0.03))):
        b = np.array([x, -0.05, 0.315])
        d = unit(x * 6, -0.3, -1)
        k.add(f"ruffpt{i}", blade([b, b + d * ln * 0.5, b + d * ln], [0.02, 0.014, 0.003], (0, 1, 0), 0.55), WHITE, "body")
    for s in (1, -1):
        b = np.array([0.04 * s, -0.035, 0.345])
        k.add(f"ruffside{'R' if s > 0 else 'L'}", blade([b, b + (0.02 * s, 0.0, -0.01), b + (0.035 * s, 0.006, -0.022)],
                                                         [0.02, 0.014, 0.003], (0, 1, 0), 0.55), WHITE, "body")


def add_arms(k, color=WHITE):
    # thin white arms hanging forward and down in front of the skirt, the elbows bent, the hands at its front
    SH, EL, WR = np.array([0.05, -0.012, 0.33]), np.array([0.076, -0.038, 0.276]), np.array([0.05, -0.112, 0.252])
    k.add("upper", tube([SH, EL], [0.0145, 0.0135]), color, "arm{s}", mirror=True)
    k.add("fore", tube([EL, WR], [0.0135, 0.013]), color, "arm{s}", mirror=True)
    k.add("hand", ell(WR + (0, -0.006, -0.01), (0.016, 0.016, 0.018)), color, "arm{s}", mirror=True)


# --- the tail: a huge bushy plume hanging behind and out to +X, red-orange at its end; the twig standing up out of it --
OUT = unit(0.57, 0.82, 0)
TAIL = [np.array([0, 0.05, 0.27]), np.array([0, 0.06, 0.27]) + OUT * 0.05 + (0, 0, 0.03),
        np.array([0, 0.06, 0.27]) + OUT * 0.11 + (0, 0, -0.01), np.array([0, 0.06, 0.27]) + OUT * 0.14 + (0, 0, -0.1),
        np.array([0, 0.06, 0.27]) + OUT * 0.15 + (0, 0, -0.18), np.array([0, 0.06, 0.27]) + OUT * 0.2 + (0, 0, -0.225)]
TAIL_R = [0.03, 0.058, 0.074, 0.07, 0.048, 0.006]


def add_tail(k):
    f.add_tail(k, tail=TAIL, radii=TAIL_R, color=YELLOW, tip=FUR, start=0.8)


def add_twig(k):
    base = np.array([0, 0.06, 0.27]) + OUT * 0.09 + (0, 0, 0.02)
    top = base + np.array([0.018, 0.02, 0.2])
    fork = base + (top - base) * 0.68
    k.add("twig", tube([base, fork, top], [0.011, 0.0102, 0.0095], seg=12), WOOD, "tail", fuse=False, surface="leather")
    k.add("twig2", tube([fork - (top - base) * 0.04, fork + np.array([-0.03, -0.004, 0.05])], [0.0098, 0.0095], seg=12),
          WOOD, "tail", fuse=False, surface="leather")


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    add_legs(k)
    add_body(k)
    add_arms(k)
    add_head(k)
    add_ears(k)
    f.add_cheeks(k, h=H, hr=HR, out=0.05, back=0.03, up=0.0, r=0.022, at=(0.82, 0.0, -0.35))
    add_tail(k)
    add_twig(k)
    add_face(k)
    k.build(voxel=0.005, band=0.004, wband=0.03)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)
