# Chewtle sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/chewtle.py -- <the export's Chewtle.glb> <out.glb>
#
# From the references: a small snapping turtle that is mostly head. A huge mint-teal head like a drop, wide at the top
# and narrowing down into a short neck; its whole front a great open mouth, pink inside with a dark throat to the right,
# one white tooth hanging from the middle of the upper lip and a broad orange lower jaw thrust forward under it like a
# shelf; a round orange knob on each side at the mouth's corners; small white eyes with a black pupil cut flat along
# their tops by a heavy straight brow, two nostrils over the lip; an orange horn on the crown leaning a little back and
# to the right, its base an orange cap running down the skull in drips. Under the head a blue shell dome on the back
# rimmed in brown, a cream plastron in front, four short thick teal legs with white claws, a stub of a tail.
#
# Written to be grown from (Drednaw: sibling("chewtle")): the line's way of making an eye cut by its brow, a claw, a
# horn and a point on an ellipsoid is one function each.
# Reference ratios (3D picture): head width / body width 1.4, the head's ball 0.37 of the height and the horn 0.24,
# the body under the head 0.29 of the height; horn tip to ground 0.45 (the export's skeleton: head joint at 0.16).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, srgb, inside, unit, eye_look, polygon, args

TEAL, ORANGE, MOUTH, THROAT = "#8FD8D0", "#F39A2C", "#D45A6A", "#5E2030"
SHELL, RIM, PLASTRON, CLAW = "#3C8CCB", "#9A4E2A", "#F2D27A", "#F6F4EE"
WHITE, PUPIL, INK, NOSTRIL = "#FAFAF6", "#14161A", "#1C2228", "#2F6A66"

# The frame: the head's ball and the bowl under it, the body
HC = np.array([0.0, -0.025, 0.274])
HR = np.array([0.132, 0.108, 0.098])
VOXEL = 0.0042


def on_ell(c, r, x, z, front=True):
    """The point of an ellipsoid's front (or back) at a given x and z."""
    c, r = np.array(c, float), np.array(r, float)
    s = 1 - ((x - c[0]) / r[0]) ** 2 - ((z - c[2]) / r[2]) ** 2
    y = c[1] + (-1 if front else 1) * r[1] * math.sqrt(max(s, 0.0))
    return np.array((x, y, z))


def normal_at(c, r, p):
    n = (np.array(p, float) - np.array(c, float)) / np.array(r, float) ** 2
    return n / np.linalg.norm(n)


def spike(base, d, length, w, t, side=None):
    """A solid tapering point (a claw, a tooth, a spike): a flat-faced hull from base along d."""
    base = np.array(base, float)
    d = unit(*d)
    if side is None:
        side = np.cross(d, (0, 0, 1.0))
        if np.linalg.norm(side) < 1e-3:
            side = np.array([1.0, 0, 0])
    side = np.array(side, float); side = side - d * (side @ d); side /= np.linalg.norm(side)
    face = np.cross(d, side)
    pts = [base + side * w, base - side * w, base + face * t, base - face * t,
           base + d * length * 0.45 + side * w * 0.55, base + d * length * 0.45 - side * w * 0.55,
           base + d * length]
    return hull(pts)


def claws(k, foot, half, n, col, bone, size=1.0, spread=0.55):
    """n white claws along the front of a foot (centre foot, radii half), pointing forward and down."""
    pc, pr = np.array(foot, float), np.array(half, float)
    for i in range(n):
        dx = 0 if n == 1 else (-1 + 2 * i / (n - 1)) * spread * pr[0]
        base = pc + (dx, -pr[1] * 0.78, -pr[2] * 0.25)
        k.add(f"claw{i}", spike(base, (dx * 2, -1, -0.45), 0.42 * pr[1] * size, 0.28 * pr[0] * size,
                                0.24 * pr[2] * size + 0.003), col, bone, fuse=False, mirror=True, surface="shell")


def horn(c, d, base_r, length):
    """A smooth cone standing from c along d: a lathe with a slightly bulging flank."""
    return lathe(c, d, [(0, -0.012), (base_r, -0.012), (base_r * 0.78, length * 0.3), (base_r * 0.45, length * 0.68),
                        (base_r * 0.12, length * 0.94), (0, length)], seg=36)


def eye_open(look_in=0.08):
    """A white oval with a big black pupil and a glint."""
    W, K = srgb(WHITE), srgb(PUPIL)
    def eye(u, v):
        c = np.tile(W, (len(u), 1))
        c[((u + look_in) / 0.58) ** 2 + ((v + 0.12) / 0.66) ** 2 < 1] = K
        c[((u + look_in - 0.18) / 0.16) ** 2 + ((v + 0.02) / 0.16) ** 2 < 1] = W
        return c
    return eye


def cut_outline(cut, n=48):
    """An eye's oval with its top taken off along the brow's line."""
    pts = []
    for t in np.linspace(0, 2 * math.pi, n, endpoint=False):
        u, v = math.cos(t), math.sin(t)
        pts.append((u, min(v, cut(u))))
    return pts


def browed(look, cut, top, ink, line):
    """An eye in any state with the brow's dark band along its cut line and the head's colour over it."""
    def f(u, v):
        c = look(u, v)
        e = cut(u)
        c[v > e] = top
        c[np.abs(v - e) < line] = ink
        return c
    return f


def lay_eyes(k, state, E, de, half, open_look, lid, cut, ink=INK, line=0.2, eps=0.0025, up=(0, 0, 1)):
    """Both eyes for a state, a patch a side, u outward on both; the top cut flat by the brow in every state."""
    outline = cut_outline(cut)
    for side in (1, -1):
        m = np.array([side, 1, 1.0])
        look = browed(eye_look(state, open_look, srgb(lid), srgb(ink), line=0.17), cut, srgb(lid), srgb(ink), line)
        f = look if side == 1 else (lambda u, v, look=look: look(-u, v))
        ol = polygon([(side * u, v) for u, v in (outline if side == 1 else outline[::-1])])
        k.decal(np.array(E) * m, np.array(de) * m, half, f, eps=eps, rings=20, segs=56, outline=ol,
                up=np.array(up, float) * m)


def chewtle(src, out):
    k = Kit(src, coat="shell")

    # --- the body under the head: a cream plastron, the blue shell on the back with its brown rim ----------------
    k.add("belly", ell((0, -0.008, 0.088), (0.072, 0.066, 0.062)), PLASTRON, "body", surface="skin")
    def dome(V):
        V = V.copy()
        V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.45, V[:, 2])
        return V
    k.add("shell", ell((0, 0.032, 0.098), (0.09, 0.08, 0.064), deform=dome), SHELL, "body")
    k.paint(lambda P: inside(np.abs(P[:, 2] - (0.088 + 0.35 * np.clip(-(P[:, 1] - 0.0), 0, 0.2))) - 0.0075, 0.002)
            * (P[:, 1] > -0.05), RIM, on=["shell"])
    k.add("tail", spike((0.004, 0.09, 0.075), (0.25, 0.7, 0.75), 0.05, 0.014, 0.012), TEAL, "body", fuse=False,
          surface="skin")

    # --- four short thick legs, white claws --------------------------------------------------------------------
    FOOT_R = (0.031, 0.036, 0.019)
    k.add("arm", tube([(0.05, -0.012, 0.11), (0.056, -0.035, 0.065), (0.061, -0.048, 0.034)], [0.03, 0.028, 0.027]),
          TEAL, "arm{s}", mirror=True, surface="skin")
    ff = (0.062, -0.058, 0.019)
    k.add("hand", ell(ff, FOOT_R), TEAL, "arm{s}", mirror=True, surface="skin")
    claws(k, ff, FOOT_R, 2, CLAW, "arm{s}")
    k.add("leg", tube([(0.046, 0.012, 0.075), (0.054, 0.035, 0.045), (0.058, 0.045, 0.032)], [0.033, 0.03, 0.028]),
          TEAL, "leg{s}", mirror=True, surface="skin")
    hf = (0.059, 0.04, 0.019)
    k.add("foot", ell(hf, FOOT_R), TEAL, "leg{s}", mirror=True, surface="skin")
    claws(k, hf, FOOT_R, 2, CLAW, "leg{s}")

    # --- the head: a great drop on a short neck ----------------------------------------------------------------
    k.add("neck", tube([(0, 0.0, 0.11), (0, -0.018, 0.17), (0, -0.026, 0.21)], [0.05, 0.062, 0.078]), TEAL, "head",
          surface="skin")
    k.add("bowl", ell((0, -0.03, 0.205), (0.098, 0.084, 0.075)), TEAL, "head", surface="skin")
    k.add("head", ell(HC, HR), TEAL, "head", surface="skin")

    # The orange cap over the crown, running down the skull in drips
    def cap(P):
        p = P - HC
        th = np.arctan2(p[:, 0], -p[:, 1])
        drip = np.clip(np.cos(3 * th + 0.5), 0, 1) ** 8 + 0.8 * np.clip(np.cos(2 * th - 2.2), 0, 1) ** 10
        edge = HC[2] + 0.079 - 0.03 * drip
        near = np.linalg.norm(p / (HR * 1.25), axis=1) < 1
        return inside(edge - P[:, 2], 0.0016) * near
    k.paint(cap, ORANGE, on=["head"])
    k.add("horn", horn((0.004, 0.0, HC[2] + 0.07), unit(0.16, 0.22, 1), 0.044, 0.105), ORANGE, "head", fuse=False)

    # The open mouth: pink across the front with the throat dark to the right, a tooth from the upper lip, the broad
    # orange jaw thrust forward under it, an orange knob at each corner
    MZ = 0.25
    M = on_ell(HC, HR, 0.0, MZ)
    P_, T_ = srgb(MOUTH), srgb(THROAT)
    def mouth(u, v):
        c = np.tile(P_, (len(u), 1))
        c[((u - 0.36) / 0.42) ** 2 + ((v + 0.05) / 0.55) ** 2 < 1] = T_
        return c
    mo = [(math.cos(t), (math.sin(t) * (0.82 if math.sin(t) > 0 else 1.0)) + 0.18 * (1 - math.cos(t) ** 2)
           * (math.sin(t) > 0)) for t in np.linspace(0, 2 * math.pi, 64, endpoint=False)]
    k.decal(M, unit(0, -1, 0.18), (0.094, 0.058), mouth, eps=0.0025, rings=24, segs=96, outline=polygon(mo))
    top = on_ell(HC, HR, 0.0, MZ + 0.058)
    k.add("tooth", hull([top + (-0.017, -0.007, 0.004), top + (0.017, -0.007, 0.004), top + (-0.014, 0.008, 0.004),
                         top + (0.014, 0.008, 0.004), top + (0, -0.007, -0.028), top + (0, 0.004, -0.026)]),
          WHITE, "head", fuse=False)
    k.add("jaw", ell((0, -0.114, 0.214), (0.09, 0.052, 0.031), rot=(-14, 0, 0)), ORANGE, "head", fuse=False,
          surface="skin")
    k.add("knob", ell((0.124, -0.062, 0.248), (0.034, 0.038, 0.042)), ORANGE, "head", fuse=False, mirror=True,
          surface="skin")

    # Two nostrils over the lip
    for s in (1, -1):
        p = on_ell(HC, HR, 0.018 * s, 0.318)
        k.decal(p, normal_at(HC, HR, p), (0.0052, 0.0042), NOSTRIL, eps=0.002, rings=5, segs=24)

    k.build(voxel=VOXEL, smooth=3, band=0.004)

    # Small white eyes cut flat by a heavy brow, set high on the face
    E = on_ell(HC, HR, 0.05, 0.33)
    de = unit(*(normal_at(HC, HR, E) + np.array((0, -0.25, 0))))
    cut = lambda u: 0.36 + 0.12 * u
    k.eye_states(lambda s: lay_eyes(k, s, E, de, (0.021, 0.024), eye_open(), TEAL, cut))
    k.export(out)


if __name__ == "__main__":
    chewtle(*args())
