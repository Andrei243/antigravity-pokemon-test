# Tirtouga sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tirtouga.py -- <the export's Tirtouga.glb> <out.glb>
#
# From the references: a small sea turtle lying low on the ground. A broad dark navy shell, a low dome with a lighter
# rim, shallow round hollows in it (six: one on the crown, two pairs down its sides, one behind), each a dark ring
# round a lighter floor; light blue skin under it. A big head pushed forward out of the shell's front: navy over the
# crown and down the front into a hooked navy beak, light blue cheeks and jaw; white eyes with a black pupil toward
# the beak, cut along their tops by the navy mask; the mouth open under the beak, red inside. Two big light blue front
# flippers swept out and back, a navy spot at each tip; two small hind flippers raised behind like fins; a stub of a
# tail.
#
# Written to be grown from (Carracosta: sibling("tirtouga")): the line's colours, its flippers, mask, beak, mouth and
# eyes are the functions below; the calls that build Tirtouga are under __main__.
# Reference ratios (3D picture): head width / shell width 0.36, the shell's top 1.0 of the height (flippers below it),
# flipper span / shell width 1.65, the belly on the ground.
# The export's joints: body (0, 0, 0.14), head (0, -0.14, 0.13), tail (0, 0.17, 0.13), legs (+-0.1, -0.08, 0.1),
# hind (+-0.1, 0.12, 0.1); the export stands 0.27 tall.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, eye_look, polygon, args

# --- the line's colours ---------------------------------------------------------------------------------------------
LIGHT, NAVY, RIM, DIMPLE, FLOOR = "#78B8E6", "#34518C", "#4C6CA8", "#22355F", "#4A6AA6"
MOUTH, INK, WHITE, PUPIL = "#D85A64", "#141826", "#F8F8F6", "#16161C"


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre."""
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r, float)) ** 2).sum())


def normal_on(c, r, d):
    p = on_ell(c, r, d) - np.array(c, float)
    n = p / np.array(r, float) ** 2
    return n / np.linalg.norm(n)


def rot(deg):
    return np.array(Euler([math.radians(a) for a in deg]).to_matrix())


def flip_frame(yaw, bank, pitch=0.0):
    """A flipper's turn: banked by bank about its length (+ turns its top face toward the front), its tip raised by
    pitch, then turned by yaw about z (+ sweeps the tip back on the +X side)."""
    return rot((0, 0, yaw)) @ rot((0, -pitch, 0)) @ rot((bank, 0, 0))


def flipper(c, r, yaw, bank, droop=0.0, taper=0.45, pitch=0.0):
    """A flat flipper: an ellipsoid of half-length r[0] along its own x, its outer end narrowed by taper and drooping
    by droop (a share of its length), turned by flip_frame."""
    def deform(V):
        V = V.copy()
        t = np.clip((V[:, 0] + 1) / 2, 0, 1)
        V[:, 1] *= 1 - taper * t ** 1.5
        V[:, 2] += -droop * (t ** 2) * r[0] / r[2]
        return V
    V, F = ell((0, 0, 0), r, deform=deform)
    return V @ flip_frame(yaw, bank, pitch).T + np.array(c, float), F


def flipper_point(c, r, yaw, bank, t, pitch=0.0):
    """Where along a flipper's length (t from -1 at its root to 1 at its tip) its middle lies (no droop)."""
    return np.array(c, float) + flip_frame(yaw, bank, pitch) @ np.array([t * r[0], 0, 0])


def head_frame(H):
    """A point's place round the head: its angle from straight ahead (radians, + toward +X) and its height."""
    def f(P):
        p = P - H
        return np.arctan2(p[:, 0], -p[:, 1]), p[:, 2], p
    return f


def head_mask(H, HR, high=0.62, drop=0.85, back=0.06, edge=0.0018):
    """The mask over the crown and the front of the face, down to the beak: its edge at high (a share of the head's
    height) on the sides, falling by drop toward the front, where the eyes' top line is."""
    hf = head_frame(H)
    def mask(P):
        a, z, p = hf(P)
        e = HR[2] * (high - drop * smoothstep(0.3, 1.05, np.cos(a) * (1 - np.abs(a) / 2.2)))
        near = (np.linalg.norm((p / (HR * 1.35)), axis=1) < 1) * (p[:, 1] < back)
        return inside(e - z, edge) * near
    return mask


def eye_open(look_in=0.25):
    """A white almond, a big black pupil toward the beak, a white glint."""
    W, K = srgb(WHITE), srgb(PUPIL)
    def eye(u, v):
        c = np.tile(W, (len(u), 1))
        c[((u + look_in) / 0.5) ** 2 + ((v + 0.05) / 0.68) ** 2 < 1] = K
        c[((u + look_in - 0.12) / 0.17) ** 2 + ((v - 0.22) / 0.19) ** 2 < 1] = W
        return c
    return eye


def cut_outline(cut, n=40):
    """An eye's outline (u, v points, u outward): its ellipse with the top taken off along the mask's cut line, so the
    patch's own edge is the crisp line where the mask comes down over the eye."""
    pts = []
    for t in np.linspace(0, 2 * math.pi, n, endpoint=False):
        u, v = math.cos(t), math.sin(t)
        pts.append((u, min(v, cut(u))))
    return pts


def masked(look, cut, top, ink, line=0.11):
    """An eye's colour in any state with the mask's colour over the cut line (v > cut(u)) and a dark edge along it, so
    the mask runs over the eye whatever the eye does."""
    def f(u, v):
        c = look(u, v)
        e = cut(u)
        c[v > e] = top
        c[np.abs(v - e) < line] = ink
        return c
    return f


def lay_eyes(k, state, E, de, half, open_look, lid, top, cut, ink=INK, eps=0.003, outline=None, line=0.11):
    """Both eyes for a state: a patch a side at E (and its mirror) facing de; u outward on both."""
    for side in (1, -1):
        m = np.array([side, 1, 1.0])
        look = masked(eye_look(state, open_look, srgb(lid), srgb(ink), line=0.16), cut, srgb(top), srgb(ink), line)
        f = look if side == 1 else (lambda u, v, look=look: look(-u, v))
        ol = None
        if outline is not None:
            ol = polygon([(side * u, v) for u, v in (outline if side == 1 else outline[::-1])])
        k.decal(np.array(E) * m, np.array(de) * m, half, f, eps=eps, rings=20, segs=56, outline=ol)


def dimple(k, c, n, half, up=(0, 0, 1), eps=0.003):
    """A shallow round hollow in the shell: a dark ring round a lighter floor."""
    k.decal(c, n, (half, half), DIMPLE, eps=eps, rings=6, segs=48, up=up)
    k.decal(c, n, (half * 0.72, half * 0.72), FLOOR, eps=eps + 0.0012, rings=5, segs=48, up=up)


# --- Tirtouga's frame -----------------------------------------------------------------------------------------------
SC = np.array([0.0, 0.035, 0.125])           # the shell's middle
SR = np.array([0.178, 0.205, 0.124])
H = np.array([0.0, -0.24, 0.126])            # the head's centre: a fifth over the measure, it is the read
HS = 1.2                                      # the head's parts are placed about a head of HR / HS at H0
H0 = np.array([0.0, -0.215, 0.118])
HR = np.array([0.07, 0.078, 0.064]) * HS
DE = unit(0.62, -0.68, 0.32)                  # where the eyes look out
EYE_HALF = (0.03, 0.028)
VOXEL = 0.004

FRONT = dict(c=(0.212, -0.045, 0.074), r=(0.118, 0.06, 0.013), yaw=18, bank=26, droop=0.16, pitch=0)
HIND = dict(c=(0.15, 0.205, 0.12), r=(0.064, 0.05, 0.012), yaw=40, bank=-15, droop=0.0, pitch=52)


def tirtouga(src, out):
    k = Kit(src, coat="shell")

    # The skin under the shell, light blue, the belly on the ground
    k.add("under", ell((0, 0.02, 0.07), (0.15, 0.18, 0.062)), LIGHT, "body", surface="fur")

    # The shell: a low dome, its lower half drawn in under a lighter rim
    def dome(V):
        V = V.copy()
        V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.3, V[:, 2])
        return V
    k.add("shell", ell(SC, SR, deform=dome), NAVY, "body")
    k.add("rim", ell((SC[0], SC[1], SC[2] - 0.004), (SR[0] + 0.014, SR[1] + 0.014, 0.024)), RIM, "body")

    # The head pushed forward out of the shell's front on a short thick neck
    def hp(x, y, z):
        """A point placed about the first head (H0), moved and grown with the head."""
        return H + (np.array([x, y, z], float) - H0) * HS
    k.add("neck", tube([(0, -0.1, 0.1), (0, -0.17, 0.112)], [0.06, 0.064]), LIGHT, "head", surface="fur")
    k.add("head", ell(H, HR), LIGHT, "head", surface="fur")
    # The mouth open under the beak: the jaw dropped forward, red set back between
    k.add("jaw", ell(hp(0, -0.245, 0.073), np.array((0.052, 0.054, 0.02)) * HS, rot=(14, 0, 0)), LIGHT, "head",
          surface="fur")
    k.add("gape", ell(hp(0, -0.258, 0.09), np.array((0.042, 0.042, 0.017)) * HS, rot=(8, 0, 0)), MOUTH, "head",
          surface="fur")

    # The navy mask: the crown and the front of the face, down to the beak; its edge the eyes' top line
    k.paint(head_mask(H, HR), NAVY, on=["head", "neck"])
    # The hooked beak: a navy point bent down at the front
    k.add("beak", hull([hp(*p) for p in [(0.03, -0.262, 0.128), (-0.03, -0.262, 0.128), (0, -0.27, 0.15),
                                         (0.027, -0.29, 0.1), (-0.027, -0.29, 0.1), (0, -0.312, 0.106),
                                         (0, -0.305, 0.07), (0.022, -0.27, 0.088), (-0.022, -0.27, 0.088)]]),
          NAVY, "head", fuse=False, surface="shell")

    # The flippers: big front ones swept out and back with a navy spot at the tip, small hind ones raised like fins
    for name, f, bone in (("flip", FRONT, "leg{s}"), ("fin", HIND, "hind{s}")):
        k.add(name, flipper(f["c"], f["r"], f["yaw"], f["bank"], f["droop"], pitch=f["pitch"]), LIGHT, bone, mirror=True,
              surface="fur")
    tip = flipper_point(FRONT["c"], FRONT["r"], FRONT["yaw"], FRONT["bank"], 0.78, FRONT["pitch"])
    tip[2] -= 0.018
    for s in (1, -1):
        t = tip * np.array([s, 1, 1.0])
        k.paint(lambda P, t=t: inside(np.linalg.norm((P - t) / np.array([0.034, 0.03, 0.03]), axis=1) - 1, 0.08),
                NAVY, on=["flip"])
    tip = flipper_point(HIND["c"], HIND["r"], HIND["yaw"], HIND["bank"], 0.8, HIND["pitch"])
    for s in (1, -1):
        t = tip * np.array([s, 1, 1.0])
        k.paint(lambda P, t=t: inside(np.linalg.norm((P - t) / np.array([0.022, 0.022, 0.022]), axis=1) - 1, 0.1),
                NAVY, on=["fin"])

    # A stub of a tail behind
    k.add("tail", tube([(0, 0.2, 0.06), (0, 0.255, 0.045)], [0.03, 0.012]), LIGHT, "tail", surface="fur")

    # The hollows in the shell
    for d, h, mir, up in (((0, -0.15, 1), 0.044, False, (0, -1, 0)), ((0.55, -0.42, 0.72), 0.036, True, (0, 0, 1)),
                          ((0.6, 0.38, 0.7), 0.036, True, (0, 0, 1)), ((0, 0.75, 0.66), 0.034, False, (0, 0, 1))):
        for s in ((1, -1) if mir else (1,)):
            dd = (d[0] * s, d[1], d[2])
            dimple(k, on_ell(SC, SR, dd), normal_on(SC, SR, dd), h, up=up)

    k.build(voxel=VOXEL, smooth=3, band=0.004)
    V = np.array([v.co[:] for v in k.ob.data.vertices])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    print("KIT: board: shows %.3f in a square of %.3f" % (np.ptp(proj), 1.3 * max(h, 0.85 * max(w, l))))

    E = on_ell(H, HR, DE)
    cut = lambda u: 0.42 + 0.38 * u
    outline = cut_outline(cut)
    k.eye_states(lambda state: lay_eyes(k, state, E, DE, EYE_HALF, eye_open(), LIGHT, NAVY, cut, line=0.0,
                                        outline=outline))
    k.export(out)


if __name__ == "__main__":
    tirtouga(*args())
