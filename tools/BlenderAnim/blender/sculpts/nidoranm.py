# Nidoran (male) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/nidoranm.py -- <the export's Nidoran♂.glb> <out.glb>
# From the references: a small lilac quadruped crouched low, a round rump and a head carried low and forward on a
# short snout; very large pointed ears, teal inside, their outer edges notched near the tip; a short horn on the
# forehead; red eyes under a scowling brow; two small white fangs at the front of the upper jaw; a ragged row of
# spines down the back; dark purple spots on the side and the haunch; three white claws a foot.
#
# Written to be grown from (Nidorino, Nidoking): the look is a namespace of numbers (LOOK), and each feature is a
# function of the kit and that namespace (ear, horn, spine, claws, fangs, spot, eye), called at the end.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from types import SimpleNamespace
import numpy as np
from sculptkit import (Kit, ell, tube, hull, srgb, inside, tri, unit, eye_look, polygon, outward, args)

# --- the line's colours ------------------------------------------------------------------------------------------
LILAC, SPOT, TEAL, CLAW = "#C893D2", "#86569A", "#2A9A92", "#F2EFEC"
IRIS, PUPIL, SCLERA, INK = "#CC2230", "#2E0608", "#F6F0EE", "#2A1C30"


def frame(d, face):
    """A part's own axes: d its length, n the way its face is turned (made square to d), s across (s, n, d)."""
    d = unit(*d)
    n = np.array(face, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    return np.cross(n, d), n, d


def place(V, base, s, n, d):
    """Points made in a part's own axes (x across, y through its face, z along it) set at base."""
    return V @ np.stack([s, n, d]) + np.array(base, float)


def ear(k, L, side_bone="ear{s}"):
    """
    A great ear: a thick pointed leaf from its base on the head along L.ear_d, its face turned to L.ear_n, its outer
    edge notched toward the tip (L.notches teeth); the teal lining a crisp patch on its face.
    """
    s, n, d = frame(L.ear_d, L.ear_n)
    length, half, thick = L.ear_len, L.ear_w / 2, L.ear_t / 2
    def shape(V):
        V = V.copy()
        t = (V[:, 2] + 1) / 2                                   # 0 at the base, 1 at the point
        w = np.sqrt(np.clip(t, 0, 1)) * (1 - np.clip(t, 0, 1) ** L.ear_point) * 1.55 + 0.25 * (1 - t) ** 3
        # the outer edge (local x < 0) notched toward the tip: teeth pointing back toward the base
        teeth = np.clip(tri(t * L.notches * 2.2 + 0.5), 0, 1) ** 2 * smooth_band(t, 0.45, 0.92)
        out = V[:, 0] < 0
        w = np.where(out, w * (1 + L.notch * teeth), w)
        V[:, 0] *= w
        V[:, 1] *= 0.55 + 0.45 * np.clip(w, 0, 1)
        return V
    V, F = ell((0, 0, length / 2), (half, thick, length / 2), deform=shape, sub=5)
    k.add("ear", (place(V, L.ear_base, s, n, d), F), LILAC, side_bone, mirror=True)
    # the lining: a pointed patch on the ear's face, inside its rim
    c = np.array(L.ear_base, float) + d * length * L.lining_at + n * thick
    LINING = polygon([(0, 1), (0.5, 0.5), (0.72, -0.05), (0.62, -0.6), (0.2, -1), (-0.2, -1), (-0.62, -0.6),
                      (-0.72, -0.05), (-0.5, 0.5)])
    k.decal(c, n, (half * L.lining_w, length * L.lining_h), TEAL, up=d, eps=0.003, outline=LINING, rings=12, segs=72,
            mirror=True)


def smooth_band(t, a, b, e=0.06):
    return inside(a - t, e) * inside(t - b, e)


def horn(k, L, color=LILAC):
    """The horn on the forehead: a solid cone, forward and up."""
    base, d = np.array(L.horn_base, float), unit(*L.horn_d)
    k.add("horn", outward(tube([base - d * 0.02, base + d * L.horn_len * 0.45, base + d * L.horn_len],
                               [L.horn_r, L.horn_r * 0.62, 0.003], seg=20, per=6)), color, "head", fuse=False,
          surface="shell")


def spine(k, name, root, d, length, width, depth, bone="body", color=LILAC, lean=(1.0, 0, 0)):
    """
    A spine of the back: a solid kite-sectioned blade (a hull) from root along d, width across its back edge (lean),
    depth through it, sunk a little into the body at its root.
    """
    root, d = np.array(root, float), unit(*d)
    a = unit(*lean); a = a - d * (a @ d); a /= np.linalg.norm(a)
    b = np.cross(d, a)
    pts = [root - d * 0.02 + a * width * 0.5, root - d * 0.02 - a * width * 0.5,
           root - d * 0.02 + b * depth * 0.5, root - d * 0.02 - b * depth * 0.5,
           root + d * length * 0.4 + a * width * 0.32, root + d * length * 0.4 - a * width * 0.22,
           root + d * length * 0.4 + b * depth * 0.3, root + d * length * 0.4 - b * depth * 0.3,
           root + d * length]
    k.add(name, hull(pts), color, bone, fuse=False, surface="fur")


def claws(k, tag, foot, bone, n=3, spread=0.022, size=0.012, down=-0.4, toward=-1.0):
    """White claws along a foot's front edge, short stout cones forward and down (rhydon.py's)."""
    foot = np.array(foot, float)
    for j in range(n):
        dx = (j - (n - 1) / 2) * spread
        b = foot + np.array([dx, 0, 0])
        d = unit(dx * 1.4, toward, down)
        k.add(f"claw{tag}{j}", outward(tube([b - d * 0.008, b + d * size * 0.8, b + d * size * 1.5],
                                            [size * 0.6, size * 0.38, 0.0015], seg=10, per=4)), CLAW, bone,
              fuse=False, surface="shell")


def fang(k, name, base, up, length, width, bone="head"):
    """A small white fang, a cone from base along up."""
    base, up = np.array(base, float), unit(*up)
    k.add(name, outward(tube([base - up * 0.006, base + up * length * 0.5, base + up * length],
                             [width, width * 0.6, 0.0015], seg=10, per=4)), CLAW, bone, fuse=False, surface="shell")


def spot(k, c, n, half, up=(0, 0, 1), color=SPOT, shape=None, mirror=True):
    """A dark spot on the coat: a crisp patch."""
    k.decal(c, n, half, color, up=up, eps=0.003, outline=shape, rings=8, segs=40, mirror=mirror)


def eye_open(L):
    """The line's eye: iris, a dark pupil, a white glint, a sliver of white behind, under a brow sloping to the snout."""
    CI, CP, CS, CK, LID = srgb(L.iris), srgb(L.pupil), srgb(SCLERA), srgb(INK), srgb(LILAC)
    def eye(u, v):
        c = np.tile(CI, (len(u), 1))
        c[(u > 0.4) & (u * u + v * v > 0.52 ** 2)] = CS
        c[((u + 0.12) / L.pupil_w) ** 2 + ((v + 0.1) / 0.42) ** 2 < 1] = CP
        c[((u + 0.0) / 0.15) ** 2 + ((v - 0.1) / 0.15) ** 2 < 1] = (1, 1, 1)
        c[u * u + v * v > 0.82 ** 2] = CK
        edge = L.brow + L.brow_slope * u                  # the brow slopes down toward the snout (u inward is -)
        c[v > edge + 0.14] = LID
        c[np.abs(v - edge) <= 0.14] = CK
        return c
    return eye


def eyes_of(k, L):
    eye = eye_open(L)
    CK, LID = srgb(INK), srgb(LILAC)
    def lay(state):
        k.decal(L.eye_c, L.eye_n, L.eye_h, eye_look(state, eye, LID, CK, line=0.16), up=L.eye_up, eps=0.005,
                rings=20, segs=56, mirror=True)
    return lay


# --- Nidoran's own frame ------------------------------------------------------------------------------------------
LOOK = SimpleNamespace(
    H=np.array([0.0, -0.18, 0.225]),                   # the head's centre (the head bone is at 0, -0.1, 0.27)
    HR=np.array([0.104, 0.1, 0.094]),
    ear_base=(0.055, -0.13, 0.285), ear_d=(0.5, 0.42, 0.78), ear_n=(0.55, -1.0, 0.1),
    ear_len=0.3, ear_w=0.19, ear_t=0.032, ear_point=1.5, notches=3, notch=0.4,
    lining_at=0.47, lining_w=0.76, lining_h=0.37,
    horn_base=(0, -0.235, 0.305), horn_d=(0, -0.72, 0.7), horn_len=0.085, horn_r=0.028,
    iris=IRIS, pupil=PUPIL, pupil_w=0.3, brow=0.3, brow_slope=0.38,
    eye_c=None, eye_n=None, eye_h=(0.034, 0.036), eye_up=(0, 0.15, 1),
)
LOOK.eye_n = unit(0.62, -0.76, 0.12)
LOOK.eye_c = LOOK.H + np.array([0.0, -0.01, 0.0]) + LOOK.eye_n * 0.098


def nidoran(src, out, L=LOOK):
    k = Kit(src, coat="fur")
    H, HR = L.H, L.HR

    # The body: crouched low, a round rump behind and a chest before
    def pear(V):                                          # fuller behind, toward the round rump
        V = V.copy()
        V[:, 0] *= 1 + 0.12 * V[:, 1]; V[:, 2] *= 1 + 0.1 * V[:, 1]
        return V
    k.add("torso", ell((0, 0.06, 0.168), (0.13, 0.215, 0.122), deform=pear), LILAC, "body")
    k.add("tail", ell((0, 0.27, 0.19), (0.022, 0.03, 0.022)), LILAC, "tail")

    # The head, round, a short snout before it and the brow over the eyes
    k.add("head", ell(H, HR), LILAC, "head")
    k.add("snout", ell(H + (0, -0.085, -0.035), (0.068, 0.07, 0.056)), LILAC, "head")
    k.add("brow", ell(H + (0.048, -0.055, 0.035), (0.038, 0.048, 0.02), rot=(-15, 0, -12)), LILAC, "head", mirror=True)
    k.add("neck", ell((0, -0.1, 0.2), (0.095, 0.09, 0.095)), LILAC, "head")
    k.add("cheek", ell(H + (0.065, -0.02, -0.03), (0.05, 0.05, 0.05)), LILAC, "head", mirror=True)

    # The legs, short and stout: forelegs reaching forward, the hind ones folded under the rump
    k.add("leg", tube([(0.088, -0.09, 0.15), (0.092, -0.13, 0.07), (0.09, -0.16, 0.035)], [0.045, 0.04, 0.036]),
          LILAC, "leg{s}", mirror=True)
    k.add("paw", ell((0.09, -0.185, 0.026), (0.044, 0.055, 0.027)), LILAC, "leg{s}", mirror=True)
    k.add("thigh", ell((0.092, 0.16, 0.13), (0.072, 0.105, 0.095)), LILAC, "hind{s}", mirror=True)
    k.add("hind", tube([(0.105, 0.15, 0.1), (0.108, 0.1, 0.04)], [0.045, 0.038]), LILAC, "hind{s}", mirror=True)
    k.add("hpaw", ell((0.108, 0.07, 0.026), (0.044, 0.058, 0.027)), LILAC, "hind{s}", mirror=True)
    for side, m in (("R", 1), ("L", -1)):
        claws(k, "f" + side, (0.09 * m, -0.232, 0.024), f"leg{side}")
        claws(k, "h" + side, (0.108 * m, 0.017, 0.024), f"hind{side}")

    # The ears, the horn, the fangs
    ear(k, L)
    horn(k, L)
    for m, side in ((1, "R"), (-1, "L")):
        fang(k, "fang" + side, (0.022 * m, -0.318, 0.168), (0.1 * m, -0.25, -1), 0.024, 0.008)

    # The ragged spines of the back, running back and up, their tips a little to each side
    for j, (y, z, ln, lean) in enumerate([(-0.02, 0.275, 0.1, 1), (0.05, 0.28, 0.15, -1), (0.12, 0.27, 0.14, 1),
                                          (0.19, 0.245, 0.1, -1)]):
        spine(k, f"spine{j}", (0.012 * lean, y, z), (0.12 * lean, 0.7, 0.7), ln, 0.1, 0.04,
              lean=(0, 1, -0.3))

    # Dark spots on the side and the haunch
    spot(k, (0.12, 0.2, 0.18), unit(0.85, 0.35, 0.1), (0.05, 0.045))
    spot(k, (0.115, 0.03, 0.24), unit(0.75, 0.0, 0.55), (0.028, 0.022))
    spot(k, (0.115, -0.05, 0.2), unit(0.9, -0.2, 0.25), (0.018, 0.018))

    k.build(voxel=0.006, smooth=4)
    k.eye_states(eyes_of(k, L))
    k.export(out)


if __name__ == "__main__":
    nidoran(*args())
