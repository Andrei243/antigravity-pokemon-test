# Capsakid sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/capsakid.py -- <the export's Capsakid.glb> <out.glb>
#
# From the references (they overrule the brief's "toothy jaw and a stem tuft"): a little green pepper whose largest
# mass is a great round green head; a pale calyx hugging the back of the head with triangular points round its edge,
# a small green knob on top of it behind; sly half-moon eyes, black, cut flat at the top by a slant down to the middle,
# a round dark green pupil low in each; a pointed orange beak in the middle of the face with an orange flush round its
# root; under it a smirking open mouth, red, with a white fang; a collar of darker green leaf points round the neck;
# a pear of a body, short flat pointed arms, two short legs tapering to points, a small pointed tail.
#
# Written to be grown from: Scovillain keeps its colours and its eyes (angry_eye, lay_eye) and its leaf points.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, unit, eye_look, outward, polygon, args

GREEN, DARK, PALE, ORANGE, FLUSH = "#66BF74", "#459C58", "#E3ECD5", "#F08C34", "#EE9E5E"
INK, PUPIL, MOUTH, TONGUE, WHITE = "#191B16", "#4F6E3F", "#7C1E27", "#C7404C", "#F6F4EE"

H = np.array([0.0, -0.012, 0.272])          # the head's centre
HR = np.array([0.122, 0.11, 0.114])
VOXEL = 0.004
CAP_N = unit(0, 0.72, 0.69)                 # the calyx covers the head behind this plane
CAP_D = 0.022


def on_ell(C, R, d, lift=0.0):
    d = unit(*d)
    s = 1 / math.sqrt(((d / np.array(R)) ** 2).sum())
    return np.array(C) + d * (s + lift)


def point(k, name, root, d, width, length, thick, colour, bone, up=None, mirror=False, surface="leaf"):
    """A solid flat point (a leaf's tip, a calyx's spike): a kite of flat faces from root along d."""
    d = unit(*d)
    side = np.cross(d, up if up is not None else (0, 0, 1.0))
    if np.linalg.norm(side) < 1e-3:
        side = np.cross(d, (1.0, 0, 0))
    side /= np.linalg.norm(side)
    n = np.cross(side, d)
    root = np.array(root, float)
    pts = []
    for s in (1, -1):
        pts.append(root + side * width * s)
        pts.append(root + d * length * 0.35 + side * width * 0.8 * s)
    pts.append(root + d * length)
    pts.append(root - d * width * 0.5)
    P = [p + n * thick / 2 for p in pts] + [p - n * thick / 2 for p in pts[:-2]]
    k.add(name, hull(P), colour, bone, fuse=False, mirror=mirror, surface=surface)


# --- the eyes: sly half-moons, cut flat at the top by a slant down to the middle -------------------------------------
def cut_outline(a=0.28, b=0.42):
    """An oval cut by the line v = a + b u (u outward): the slant falls toward the inner corner."""
    def r(th):
        c, s = math.cos(th), math.sin(th)
        den = s - b * c
        return min(1.0, a / den) if den > 1e-6 else 1.0
    return r


def angry_eye(pupil=PUPIL):
    ck, cp, cw = srgb(INK), srgb(pupil), np.array([1.0, 1.0, 1.0])
    def look(u, v):
        c = np.tile(ck, (len(u), 1))
        c[((u + 0.08) / 0.46) ** 2 + ((v + 0.32) / 0.46) ** 2 < 1] = cp
        c[((u - 0.18) / 0.13) ** 2 + ((v + 0.12) / 0.13) ** 2 < 1] = cw
        return c
    return look


def lay_eye(k, E, n, half, state, lid, pupil=PUPIL, line=0.16, a=0.28, b=0.42):
    """Both eyes laid by hand (the outline isn't symmetric): E and n are the +X eye's; the -X one mirrors them."""
    lk = eye_look(state, angry_eye(pupil), srgb(lid), srgb(INK), line=line)
    r = cut_outline(a, b)
    m = np.array([-1, 1, 1.0])
    k.decal(E, n, half, lk, eps=0.0018, outline=r, rings=20, segs=56)
    k.decal(np.array(E) * m, np.array(n) * m, half, lambda u, v: lk(-u, v), eps=0.0018,
            outline=lambda th: r(math.pi - th), rings=20, segs=56)


EYE_DIR = (0.48, -0.86, 0.06)
EYE_HALF = (0.036, 0.031)


# --- the parts -----------------------------------------------------------------------------------------------------
def head(k):
    k.add("head", ell(H, HR), GREEN, "head")
    k.add("cap", ell(H + CAP_N * 0.01, HR * 0.985), GREEN, "head")
    k.paint(lambda P: inside(CAP_D - (P - H) @ CAP_N, 0.0018)
            * inside(np.sqrt((((P - H) / HR) ** 2).sum(1)) - 1.2, 0.004), PALE, on=["head", "cap"])
    # the calyx's points round its edge, laid back and out
    b = unit(0, -0.69, 0.72)
    for i, phi in enumerate((-82, -52, -26, 0, 26, 52, 82)):
        p = math.radians(phi)
        d = unit(*(CAP_N * 0.3 + b * math.cos(p) + np.array([1.0, 0, 0]) * math.sin(p)))
        root = on_ell(H, HR, d) - d * 0.012
        out_ = unit(*(d + CAP_N * 0.55))
        point(k, f"calyx{i}", root, out_, 0.024, 0.06, 0.013, PALE, "head", up=CAP_N)
    k.add("knob", outward(ell(on_ell(H, HR, (0, 0.62, 0.78)) - unit(0, 0.62, 0.78) * 0.004, (0.019, 0.019, 0.017))),
          GREEN, "head", fuse=False, surface="leaf")
    # the beak, its flush and the mouth
    d = unit(0, -0.78, -0.62)
    B0 = on_ell(H, HR, (0, -1, -0.22)) + unit(0, 1, 0.2) * 0.012
    k.add("beak", outward(tube([B0, B0 + d * 0.035, B0 + d * 0.06 + np.array([0, 0, -0.008])],
                               [0.03, 0.017, 0.003])), ORANGE, "head", fuse=False, surface="shell")


def face(k):
    k.decal(on_ell(H, HR, (0, -1, -0.22)), unit(0, -1, -0.2), (0.048, 0.036), FLUSH, eps=0.0012, rings=12, segs=44)
    # the smirk: wider to +X, red with a darker red inside, the tongue low, one white fang under the beak
    cm, ct = srgb(MOUTH), srgb(TONGUE)
    def mouth(u, v):
        c = np.tile(cm, (len(u), 1))
        c[((u - 0.1) / 0.6) ** 2 + ((v + 0.85) / 0.55) ** 2 < 1] = ct
        return c
    smirk = polygon([(-0.75, 0.55), (0.0, 0.7), (1.0, 0.95), (0.85, -0.2), (0.3, -1.0), (-0.45, -0.85), (-1.0, 0.1)])
    dm = unit(0.22, -0.92, -0.42)
    k.decal(on_ell(H, HR, dm), dm, (0.04, 0.024), mouth, eps=0.0022, outline=smirk, rings=16, segs=56)
    df = unit(-0.03, -0.9, -0.44)
    k.decal(on_ell(H, HR, df), dm, (0.009, 0.012), WHITE, eps=0.0032, outline=polygon([(-1, 1), (1, 1), (0.1, -1)]),
            rings=8, segs=36)


def body(k):
    def pear(V):
        V = V.copy(); s = 1 - 0.18 * V[:, 2]
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("torso", ell((0, 0.0, 0.112), (0.068, 0.06, 0.074), deform=pear), GREEN, "body")
    k.add("neck", ell((0, -0.004, 0.172), (0.042, 0.04, 0.03)), GREEN, "body")
    # short legs tapering forward to points
    k.add("leg", tube([(0.034, 0.0, 0.075), (0.045, -0.006, 0.035), (0.05, -0.012, 0.024)], [0.031, 0.026, 0.021]),
          GREEN, "leg{s}", mirror=True)
    k.add("toe", tube([(0.05, -0.01, 0.021), (0.053, -0.04, 0.016), (0.055, -0.062, 0.012)], [0.02, 0.014, 0.004]),
          GREEN, "leg{s}", mirror=True)
    # short flat pointed arms, out, a little forward and down
    k.add("arm", blade([(0.045, -0.004, 0.148), (0.075, -0.016, 0.126), (0.1, -0.03, 0.104)], [0.024, 0.016, 0.004],
                       unit(0.4, 0.1, 1), 0.5), GREEN, "arm{s}", mirror=True)
    # a small pointed tail
    k.add("tail", tube([(0, 0.045, 0.09), (0, 0.075, 0.098), (0, 0.098, 0.11)], [0.022, 0.012, 0.003]), GREEN, "tail")


def collar(k):
    """Darker leaf points round the neck, falling over the chest and shoulders."""
    for i, a in enumerate((0, 60, -60, 120, -120, 180)):
        t = math.radians(a)
        d = np.array([math.sin(t), -math.cos(t), 0.0])
        root = np.array([0, -0.004, 0.18]) + d * 0.032
        point(k, f"collar{i}", root, unit(*(d * 0.9 + np.array([0, 0, -0.75]))), 0.024, 0.046, 0.011, DARK, "body")


def capsakid(src, out):
    k = Kit(src, coat="leaf")
    head(k)
    body(k)
    collar(k)
    face(k)
    de = unit(*EYE_DIR)
    E = on_ell(H, HR, de)
    k.build(voxel=VOXEL, smooth=3, band=0.005, wband=0.028)
    k.eye_states(lambda state: lay_eye(k, E, de, EYE_HALF, state, GREEN))
    k.export(out)


if __name__ == "__main__":
    capsakid(*args())
