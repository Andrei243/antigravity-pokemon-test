# Fennekin sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/fennekin.py -- <the export's Fennekin.glb> <out.glb>
# A small yellow fox, mostly head and ears: huge broad ears leaning out, each full of red-orange fur that juts out past
# its outer edge in three points; big red-orange eyes with a dark middle and a white glint; a short pointed muzzle with
# a small dark nose, white about the mouth; a pointed white tuft out from each cheek; a small slim body on short legs;
# a big bushy tail up behind, swung to the model's +X, its tip red-orange in points.
#
# Written to be grown from (braixen.py and delphox.py take its colours and the way of its eyes, nose, ear fur and
# cheek tufts). Every function takes the kit and the head it works on.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, tri, eye_look, polygon, args

# --- colours ----------------------------------------------------------------------------------------------------------
YELLOW = "#F4D862"          # the coat, a warm lemon yellow
FUR = "#E8582C"             # the fur in the ears and the tail's tip: red-orange
WHITE = "#F6F2EA"           # the cheek tufts and about the mouth
EYE, PUPIL = "#E2522A", "#5E120C"
NOSE, INK = "#3A3036", "#3A1E16"

# --- the frame (the skeleton's joints: body (0, 0, 0.12), head (0, -0.06, 0.16), ears (+-0.04, -0.06, 0.26),
#     forelegs (+-0.035, -0.06, 0.1), hind legs (+-0.035, 0.06, 0.1), tail (0, 0.07, 0.13)) ----------------------------
# Ratios measured on the 3D reference (reference / this sculpt): the head, chin to skull top, of the whole height
# 0.34 / 0.33; the ears' rise over the skull against the head's height about 1.0; the legs showing under the
# belly of the height 0.12 / 0.11; the head's width against the trunk's 1.8 / 1.7.
H = np.array([0, -0.074, 0.216])          # the head's centre
HR = np.array([0.077, 0.069, 0.069])      # its radii: a little wider than tall
LEG_X = 0.034
FORE_Y, HIND_Y = -0.058, 0.06
BODY_Z = 0.092


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


def cheeky(V):
    """A fox's head: fullest at the cheeks, narrowing a little to the chin."""
    V = V.copy()
    low = np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= 1 + 0.07 * (1 - V[:, 2] ** 2) - 0.12 * low ** 2
    return V


def taper(V):
    """A muzzle narrowing to its tip at -Y."""
    V = V.copy()
    t = np.clip(-V[:, 1], 0, 1)
    V[:, 0] *= 1 - 0.5 * t
    V[:, 2] *= 1 - 0.32 * t
    return V


# --- head, body, legs --------------------------------------------------------------------------------------------------
def add_head(k):
    k.add("head", ell(H, HR, deform=cheeky), YELLOW, "head")
    k.add("muzzle", ell(H + (0, -0.059, -0.027), (0.032, 0.036, 0.025), rot=(-6, 0, 0), deform=taper), YELLOW, "head")
    k.add("neck", tube([(0, -0.032, 0.11), (0, -0.05, 0.145), (0, -0.065, 0.17)], [0.03, 0.03, 0.03]), YELLOW, "head")


def add_body(k):
    k.add("chest", ell((0, -0.034, BODY_Z), (0.04, 0.046, 0.046)), YELLOW, "body")
    k.add("middle", ell((0, 0.012, BODY_Z - 0.002), (0.037, 0.05, 0.042)), YELLOW, "body")
    k.add("rump", ell((0, 0.055, BODY_Z), (0.041, 0.042, 0.045)), YELLOW, "body")


def add_legs(k):
    # Short slim legs: a shoulder and a thigh fused over the top of each, a small rounded paw
    k.add("shoulder", ell((LEG_X - 0.002, FORE_Y + 0.004, 0.08), (0.022, 0.026, 0.034)), YELLOW, "leg{s}", mirror=True)
    k.add("leg", tube([(LEG_X - 0.002, FORE_Y + 0.003, 0.085), (LEG_X, FORE_Y, 0.045), (LEG_X + 0.001, FORE_Y - 0.002, 0.016)],
                      [0.018, 0.0155, 0.0145]), YELLOW, "leg{s}", mirror=True)
    k.add("paw", ell((LEG_X + 0.001, FORE_Y - 0.008, 0.01), (0.015, 0.019, 0.01)), YELLOW, "leg{s}", mirror=True)
    k.add("thigh", ell((LEG_X - 0.001, HIND_Y + 0.004, 0.082), (0.026, 0.032, 0.038)), YELLOW, "hind{s}", mirror=True)
    k.add("hind", tube([(LEG_X, HIND_Y + 0.008, 0.075), (LEG_X + 0.001, HIND_Y + 0.006, 0.042),
                        (LEG_X + 0.001, HIND_Y - 0.001, 0.016)], [0.018, 0.0155, 0.0145]), YELLOW, "hind{s}", mirror=True)
    k.add("hpaw", ell((LEG_X + 0.001, HIND_Y - 0.007, 0.01), (0.015, 0.019, 0.01)), YELLOW, "hind{s}", mirror=True)


# --- the ears: huge, broad, leaning out some 33 degrees from upright and a little back; flat front to back; the front
#     face red-orange inside a yellow rim, the fur jutting out past the outer edge in three thick points ----------------
EAR = [(0.045, -0.072, 0.262), (0.073, -0.068, 0.305), (0.102, -0.062, 0.348), (0.126, -0.056, 0.386), (0.14, -0.052, 0.41)]
EAR_R = [0.04, 0.046, 0.038, 0.02, 0.003]


def add_ears(k, ear=EAR, radii=EAR_R, color=YELLOW, fur=FUR, width=0.034, flat=0.3, points=None, bone="ear{s}"):
    """
    The ear as a blade flat front to back, its front lined with fur, and points of fur out past its outer edge:
    points (share along the ear, length past the edge, thickness, lean toward the tip) or the three below.
    """
    k.add("ear", blade(ear, radii, (0, 1, 0), flat), color, bone, mirror=True)
    e = np.array(ear, float)
    d = unit(*(e[-1] - e[0]))
    across = np.cross(d, (0, 1.0, 0)); across /= np.linalg.norm(across)      # toward the inner edge
    outer = -across

    def lining(P):
        t, off = along(P, ear)
        half = width * np.sin(np.pi * np.clip((t + 0.1) / 1.0, 0, 1)) ** 0.7
        return (inside(np.abs(off @ across) - half, 0.0018) * inside(off[:, 1] + 0.0005, 0.0012)
                * inside(t - 0.86, 0.008) * (t > 0.04))
    k.paint(lining, fur, on=["ear"])
    # the points: from inside the ear's front out past its outer edge, swept a little toward the tip
    seg = np.linalg.norm(np.diff(e, axis=0), axis=1); cum = np.concatenate([[0], np.cumsum(seg)])
    for i, (t, ln, th, lean) in enumerate(points if points is not None else ((0.2, 0.038, 0.021, 0.05), (0.44, 0.046, 0.022, 0.4),
                                                    (0.68, 0.032, 0.018, 0.75))):
        s = t * cum[-1]
        base = np.array([np.interp(s, cum, e[:, j]) for j in range(3)]) + np.array([0, -0.004, 0])
        dd = unit(*(outer + d * lean))
        reach = np.interp(t, np.linspace(0, 1, len(radii)), radii) * 0.9 + ln
        k.add(f"earfur{i}", blade([base, base + dd * reach * 0.55, base + dd * reach], [th, th * 0.7, 0.002],
                                  (0, 1, 0), 0.55), fur, bone, mirror=True)


# --- the cheek tufts: a white point out and back from each cheek, flat front to back --------------------------------
def add_cheeks(k, h=H, hr=HR, color=WHITE, out=0.062, back=0.034, up=0.006, r=0.025, at=(0.8, -0.1, -0.32), bone="head"):
    b = h + np.array([hr[0] * at[0], hr[1] * at[1], hr[2] * at[2]])
    t = b + np.array([out, back, up])
    k.add("cheek", blade([b, b + (t - b) * 0.5, t], [r, r * 0.65, 0.002], unit(0, 1, 0.25), 0.5), color, bone,
          mirror=True)


def add_mouthfur(k, h=H, color=WHITE):
    """White about the mouth: the muzzle's lower half and the chin, a crisp line from the cheeks to the nose."""
    def m(P):
        line = h[2] - 0.018 - 0.006 * np.clip((h[1] - 0.03 - P[:, 1]) / 0.05, 0, 1)
        return (inside(P[:, 2] - line, 0.0018) * (P[:, 1] < h[1] - 0.02) * (P[:, 2] > h[2] - 0.07)
                * (np.abs(P[:, 0]) < 0.052))
    k.paint(m, color, on=["head", "muzzle"])


# --- the tail: a big bushy plume from the rump, back, up and out to +X, flattened across its swing, its tip in points --
OUT = unit(0.57, 0.82, 0)
TAIL_ROOT = np.array([0, 0.08, 0.105])
TAIL = [TAIL_ROOT, TAIL_ROOT + OUT * 0.04 + (0, 0, 0.022), TAIL_ROOT + OUT * 0.08 + (0, 0, 0.066),
        TAIL_ROOT + OUT * 0.1 + (0, 0, 0.115), TAIL_ROOT + OUT * 0.104 + (0, 0, 0.158)]
TAIL_R = [0.02, 0.04, 0.05, 0.04, 0.006]


def add_tail(k, tail=TAIL, radii=TAIL_R, color=YELLOW, tip=FUR, start=0.7, bone="tail"):
    k.add("tail", blade(tail, radii, unit(OUT[1], -OUT[0], 0), 0.7), color, bone)
    def m(P):
        t, off = along(P, tail)
        jag = start + 0.06 * tri(np.arctan2(off[:, 2], off[:, 0] + 1e-9) * 3 / math.pi)
        return inside(jag - t, 0.003) * (np.linalg.norm(off, axis=1) < 0.08)
    k.paint(m, tip, on=["tail"])


# --- the face: patches on the fused head, laid before the body's build -------------------------------------------------
def add_nose(k, at, n=unit(0, -1, 0.15), half=(0.0095, 0.0075), color=NOSE):
    k.decal(at, n, half, color, eps=0.0025, outline=polygon([(-1, 0.6), (1, 0.6), (0.3, -0.9), (-0.3, -0.9)]),
            rings=6, segs=28)


def smile(ground, ink):
    CG, CI = srgb(ground), srgb(ink)
    def look(u, v):
        c = np.tile(CG, (len(u), 1))
        c[(np.abs(v - (-0.1 + 1.1 * (np.abs(u) - 0.35) ** 2)) < 0.24) & (np.abs(u) < 0.9)] = CI
        return c
    return look


def add_mouth(k, at, n=unit(0, -1, -0.25), half=(0.016, 0.009), look=None):
    k.decal(at, n, half, look or smile(WHITE, INK), eps=0.0025, rings=12, segs=56)


# The eyes: big ovals a little taller than wide, red-orange with a dark middle and a white glint, a dark line along the
# top; the yellow of the head their lid
EYE_DIR = unit(0.46, -0.885, 0.02)
EYE_HALF = (0.0205, 0.024)


def eye(u, v):
    c = np.tile(srgb(EYE), (len(u), 1))
    c[((u + 0.04) / 0.44) ** 2 + ((v + 0.04) / 0.52) ** 2 < 1] = srgb(PUPIL)
    c[((u - 0.22) / 0.3) ** 2 + ((v - 0.38) / 0.27) ** 2 < 1] = (1, 1, 1)
    c[(v > 0.62 - 0.1 * u) & (u * u + v * v > 0.5)] = srgb(INK)
    return c


def eyes(k, state, h=H, hr=HR, d=EYE_DIR, half=EYE_HALF, lid=YELLOW, look=eye):
    e = h + d * hr[0] * 0.92 + np.array([0, 0, 0.004])
    k.decal(e, d, half, eye_look(state, look, srgb(lid), srgb(INK)), eps=0.003, rings=20, segs=56, mirror=True)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    add_body(k)
    add_legs(k)
    add_head(k)
    add_ears(k)
    add_cheeks(k)
    add_tail(k)
    add_mouthfur(k)
    add_nose(k, H + (0, -0.093, -0.023))
    add_mouth(k, H + (0, -0.087, -0.044))
    k.build(voxel=0.0036, band=0.003, wband=0.025)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)
