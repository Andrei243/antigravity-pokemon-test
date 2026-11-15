# Mudkip sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/mudkip.py -- <the export's Mudkip.glb> <out.glb>
#
# The design, from the references: a small blue amphibian that is mostly head, a big round head wider than tall on a
# short body with four stub legs; a tall thick blue paddle of a fin standing up from the top of the head, rounded and
# a little pointed at its top, two darker lines along it; orange cheek gills on the sides of the face, each a rounded
# plate with three points; the lower third of the face pale (a pale chin from cheek to cheek, its top edge the mouth's
# line, nearly straight and turned up a little at the corners); small black oval eyes with a white glint, wide apart;
# a big pale fan of a tail rising behind, two lines along it.
#
# Written to be grown from (marshtomp.py, swampert.py: sibling("mudkip")): the line's colours, its way of making a fin
# (a fused thick plate along a frame), a gill (a hard-rimmed slab with points), an eye and the pale chin are functions.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, boxy, eye_look, slab, args

# --- the line's colours --------------------------------------------------------------------------------------------
BLUE, PALE, ORANGE, FINLINE, TAILC, TAILLINE = "#4CA6E4", "#DCE8F4", "#F49A30", "#2F78BC", "#D2E4F4", "#86AED0"
INK, GLINT = "#101418", "#FFFFFF"
EDGE = 0.0035


# --- small helpers ---------------------------------------------------------------------------------------------------
def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r) in direction d from its centre."""
    d = np.array(d, float)
    q = d / np.array(r, float)
    return np.array(c, float) + d / np.linalg.norm(q)


def plate(c, wdir, ndir, ldir, w, t, l, deform=None, sub=4):
    """A fused thick plate: an ellipsoid of half width w along wdir, half thickness t along ndir and half length l along
    ldir, centred on c (a fin, a paddle). deform works on the unit sphere with x across, y through, z along."""
    V, F = ell((0, 0, 0), (1, 1, 1), deform=deform, sub=sub)
    M = np.stack([np.array(wdir, float) * w, np.array(ndir, float) * t, np.array(ldir, float) * l], axis=1)
    return V @ M.T + np.array(c, float), F


def frame3(ldir, across):
    """A right-handed frame (w, n, l): l along ldir, w the part of across square to it, n through the plate."""
    l = unit(*ldir)
    w = np.array(across, float) - l * (np.dot(across, l)); w /= np.linalg.norm(w)
    n = np.cross(l, w)
    return w, n, l


def paddle(top=0.55, bottom=0.55, point=0.0):
    """A deform for a fin: narrow at its root (z -1), fuller toward its top (z 1), a little pointed there."""
    def f(V):
        V = V.copy()
        t = (V[:, 2] + 1) / 2
        V[:, 0] *= bottom + (top - bottom) * t + 0.45 * np.sin(np.pi * t) * 0.6
        if point:
            V[:, 0] *= 1 - point * np.clip(t - 0.75, 0, 1) * 2
        return V
    return f


def lines_on(c, wdir, ldir, offsets, l0, l1, w=0.0038, edge=0.003):
    """A paint mask: lines along a plate (c, wdir, ldir), each at an offset across it, from l0 to l1 along it."""
    c = np.array(c, float)
    def mask(P):
        q = P - c
        a, b = q @ np.array(wdir), q @ np.array(ldir)
        m = np.zeros(len(P))
        for o, bend in offsets:
            m = np.maximum(m, inside(np.abs(a - o - bend * (b - l0) ** 2) - w, edge))
        return m * inside(l0 - b, edge) * inside(b - l1, edge)
    return mask


def gill(k, anchor, out, outline, thick, col, bone="head", name="gill"):
    """A cheek gill: a hard-rimmed orange slab with points, its root in the cheek, its face turned to the front-out
    (out: its outward direction, level); mirrored on -X."""
    k.add(name, slab(outline, anchor, unit(*out), (0, 0, 1.0), thick), col, bone, fuse=False, mirror=True,
          surface="skin")


def dark_eye(u, v):
    """Mudkip's eye: black, a round white glint high and outward."""
    c = np.tile(srgb(INK), (len(u), 1))
    c[((u - 0.12) / 0.36) ** 2 + ((v - 0.34) / 0.34) ** 2 < 1] = srgb(GLINT)
    return c


def lay_eyes(k, state, h, hr, d, half, look, lid, ink, line=0.2):
    k.decal(on_ell(h, hr, d), d, half, eye_look(state, look, srgb(lid), srgb(ink), line=line), eps=0.004, rings=20,
            segs=56, mirror=True)


def chin(k, h, z0, rise, reach, col, back, on=None, low=-9.0):
    """The pale lower face: below a line round the head's upright axis (z0 at the front, rising by rise at the
    angle reach), as far round as reach, and ahead of the plane y = back."""
    def mask(P):
        q = P - np.array(h)
        a = np.arctan2(q[:, 0], -q[:, 1])
        top = z0 + rise * (a / reach) ** 2
        return inside(P[:, 2] - top, EDGE) * inside(np.abs(a) - reach, 0.02) * inside(P[:, 1] - back, 0.01) * inside(low - P[:, 2], 0.006)
    k.paint(mask, col, on=on)


# --- Mudkip's frame --------------------------------------------------------------------------------------------------
H = np.array([0, -0.05, 0.208])            # the head's middle
HR = np.array([0.14, 0.124, 0.118])
EYE_D = unit(0.4, -0.87, 0.24)
EYE_HALF = (0.0165, 0.0215)
VOXEL = 0.0055


def head(k):
    def dome(V):
        V = V.copy()
        V[:, 0] *= 1 + 0.05 * np.clip(-V[:, 2], 0, 1)           # fuller at the cheeks
        V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.92, V[:, 2])  # a flatter underside
        return V
    k.add("head", ell(H, HR, deform=dome), BLUE, "head")
    # the pale jaw stands a little forward under the face
    k.add("jaw", ell((0, -0.1, 0.165), (0.1, 0.075, 0.055)), BLUE, "head")


def fin(k):
    """The tall blue paddle on the head: its face to the front, leaning a little back."""
    base = np.array([0, -0.045, 0.305])
    ld = unit(0, 0.12, 1)
    w, n, l = frame3(ld, (1, 0, 0))
    c = base + l * 0.108
    k.add("fin", plate(c, w, n, l, 0.058, 0.022, 0.118, deform=paddle(0.95, 0.6, 0.25)), BLUE, "head")
    k.paint(lines_on(c, w, l, [(0.016, 0.0), (-0.016, 0.0)], -0.06, 0.09), FINLINE, on=["fin"])


def tail(k):
    """A big pale fan rising behind, run back and out to +X so its face shows to both cameras."""
    root = np.array([0.0, 0.1, 0.135])
    out = unit(0.57, 0.82, 0)
    ld = unit(*(out * 0.62 + np.array([0, 0, 0.78])))
    w, n, l = frame3(ld, out)
    k.add("tailroot", tube([root, root + out * 0.04 + np.array([0, 0, 0.02])], [0.045, 0.034]), BLUE, "tail")
    c = root + out * 0.05 + l * 0.115
    def fan(V):
        V = V.copy()
        t = (V[:, 2] + 1) / 2
        V[:, 0] *= 0.45 + 0.65 * t
        return V
    k.add("tailfin", plate(c, w, n, l, 0.068, 0.02, 0.115, deform=fan), TAILC, "tail")
    k.paint(lines_on(c, w, l, [(0.022, 0.0), (-0.022, 0.0)], -0.05, 0.085), TAILLINE, on=["tailfin"])


def body_legs(k):
    k.add("body", ell((0, 0.04, 0.12), (0.092, 0.105, 0.07)), BLUE, "body")
    for nm, y, bone in (("leg", -0.06, "leg{s}"), ("hind", 0.07, "hind{s}")):
        k.add(nm + "top", ell((0.066, y + 0.005, 0.085), (0.052, 0.056, 0.05)), BLUE, bone, mirror=True)
        k.add(nm, tube([(0.07, y, 0.08), (0.074, y - 0.004, 0.032)], [0.04, 0.038]), BLUE, bone, mirror=True)
        k.add(nm + "foot", ell((0.075, y - 0.014, 0.02), (0.043, 0.048, 0.022)), BLUE, bone, mirror=True)


GILL = [(-0.03, 0.035), (0.0, 0.05), (0.03, 0.052), (0.072, 0.074), (0.062, 0.025), (0.098, 0.0), (0.062, -0.025),
        (0.07, -0.074), (0.03, -0.052), (0.0, -0.05), (-0.03, -0.035)]


def mudkip(src, out):
    k = Kit(src, coat="scales")
    head(k)
    fin(k)
    body_legs(k)
    tail(k)
    chin(k, H, H[2] - 0.022, 0.016, 1.45, PALE, H[1] + 0.03, on=["head", "jaw"], low=0.1)
    gill(k, (0.108, H[1] - 0.04, H[2] - 0.022), (1, -0.32, 0), GILL, 0.04, ORANGE)
    k.build(voxel=VOXEL, band=0.006)
    k.eye_states(lambda state: lay_eyes(k, state, H, HR, EYE_D, EYE_HALF, dark_eye, BLUE, INK))
    k.export(out)


if __name__ == "__main__":
    mudkip(*args())
