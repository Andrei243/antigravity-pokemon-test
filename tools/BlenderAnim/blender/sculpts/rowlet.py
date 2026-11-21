# Rowlet sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/rowlet.py -- <the export's Rowlet.glb> <out.glb>
# From the references: a round egg of a tan owlet, all head and body in one; on its front a white face of two discs
# joined over the beak, arched at the top with a tan notch between them; tall black oval eyes; a white beak standing
# down the middle of the face with an orange tip; a teal-green bow tie of two leaves under the face; a pale grey belly
# below it; the wings folded into the sides, their lower feathers white; short orange feet of three toes.
# Written to be grown from: dartrix.py and decidueye.py import it (sibling("rowlet")) for its colours, its frame
# helpers, the bow tie's leaves, the beak, the feet and the way of making the eyes.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, outward, srgb, inside, smoothstep, unit, eye_look, polygon, args, sibling

p = sibling("pidgey")           # poly_mask, mirrored, spike

# --- the line's colours -------------------------------------------------------------------------------------------
TAN, WINGTAN, WHITE, GREY = "#DDB592", "#CBA07C", "#F6F4EE", "#D5D5D3"
GREEN, GREENDARK = "#2FA27C", "#1F7E5E"
BEAKC, ORANGE, FOOT, INKH = "#F4F0E8", "#EC7A38", "#EE8A3A", "#2A2427"


# --- helpers every owl of the line uses -----------------------------------------------------------------------------
def on_ell(c, r, x, z, side=-1.0):
    """The point of an ellipsoid (centre c, radii r) at x and z on its front (side -1) or back, with its normal."""
    c, r = np.asarray(c, float), np.asarray(r, float)
    q = 1 - ((x - c[0]) / r[0]) ** 2 - ((z - c[2]) / r[2]) ** 2
    y = c[1] + side * r[1] * math.sqrt(max(q, 0.0))
    P = np.array([x, y, z])
    n = (P - c) / r ** 2
    return P, n / np.linalg.norm(n)


def ell_frame(c, r, ex, ey, deform=None, sub=3):
    """An ellipsoid laid along a frame: radii r along ex, ey and their cross, centred on c."""
    ex = np.array(ex, float); ex /= np.linalg.norm(ex)
    ey = np.array(ey, float); ey = ey - ex * (ey @ ex); ey /= np.linalg.norm(ey)
    ez = np.cross(ex, ey)
    V, F = ell((0, 0, 0), r, deform=deform, sub=sub)
    return outward((V @ np.stack([ex, ey, ez]) + np.asarray(c, float), F))


def round_coords(cy, R):
    """(angle round the upright axis through y = cy, times R) and z: a face's mask is drawn in these."""
    def f(P):
        return np.arctan2(P[:, 0], -(P[:, 1] - cy)) * R, P[:, 2]
    return f


def lobe(V):
    """A leaf's lobe: rounded at its root (x -1), fuller outward and pointed at its tip (x +1)."""
    V = V.copy()
    t = (V[:, 0] + 1) / 2
    w = 0.55 + 0.75 * np.sin(np.clip(t, 0, 1) * math.pi * 0.82) - 0.35 * np.clip(t - 0.85, 0, 1) * 4
    V[:, 1] *= np.clip(w, 0.15, 1.3); V[:, 2] *= np.clip(w, 0.15, 1.3)
    return V


def bow_tie(k, c, r, z, half_w, half_h, thick, colour, knot, bone="body", lift=0.6):
    """
    Two leaves meeting at a knot on the front of an ellipsoid (c, r), each laid along the surface outward and a
    little up, as loose pieces so their edges keep; the knot a small darker ball between them.
    """
    for s in (1, -1):
        x = s * half_w * 0.95
        P, n = on_ell(c, r, x, z + half_h * 0.25)
        P0, _ = on_ell(c, r, 0.0, z)
        d = P - P0; d -= n * (d @ n); d /= np.linalg.norm(d)
        k.add("bowtie", ell_frame(P0 + d * half_w + n * thick * lift, (half_w, thick, half_h), d, n,
                                  deform=lobe), colour, bone, fuse=False, surface="leaf")
    P0, n0 = on_ell(c, r, 0.0, z)
    k.add("knot", ell_frame(P0 + n0 * thick * 0.8, (half_w * 0.22, thick * 1.1, half_h * 0.55), (1, 0, 0), n0),
          knot, bone, fuse=False, surface="leaf")


def feet(k, F, colour=FOOT, claw=None):
    """A short thick stub of leg and three toes forward, one back, each of its leg's bone."""
    for s, side in ((1, "R"), (-1, "L")):
        top = np.array([F.LEGX * s, F.LEGY, F.LEGTOP])
        ank = np.array([F.LEGX * s * 1.05, F.LEGY - F.TOER * 0.6, F.TOER * 1.25])
        k.add(f"leg{side}", tube([top, (top + ank) / 2, ank], [F.LEGR[0], F.LEGR[0] * 0.92, F.LEGR[1]]), colour,
              f"leg{side}", fuse=False, surface="scales")
        for j, ang in enumerate((-34, 0, 34, 180)):
            rr = math.radians(ang + 6 * s)
            d = np.array([math.sin(rr) * s, -math.cos(rr), 0])
            ln = F.TOE if ang != 180 else F.TOE * 0.5
            tip = ank + d * ln + np.array([0, 0, -F.TOER * 0.35])
            k.add(f"toe{side}{j}", tube([ank, ank + (tip - ank) * 0.55 + np.array([0, 0, F.TOER * 0.2]), tip],
                                        [F.TOER * 1.15, F.TOER, F.TOER * 0.8]), colour, f"leg{side}", fuse=False,
                  surface="scales")
            if claw is not None:
                cd = d + np.array([0, 0, -0.6])
                k.add(f"claw{side}{j}", p.spike(tip, tip + cd * F.TOER * 1.9, F.TOER * 1.3, F.TOER * 1.2), claw,
                      f"leg{side}", fuse=False, surface="shell")


# --- the frame: every place and size from the skeleton's joints (body 0.12, wings at 0.14, legs at 0.05) ------------
def frame():
    F = SimpleNamespace()
    F.C = np.array([0.0, 0.004, 0.11])               # the egg's centre
    F.R = np.array([0.087, 0.085, 0.12])
    F.FC = np.array([0.0, -0.022, 0.142])            # a flatter face fused into the front for the face's patches
    F.FR = np.array([0.07, 0.064, 0.07])
    F.LEGX, F.LEGY, F.LEGTOP = 0.04, -0.018, 0.045
    F.LEGR = (0.013, 0.012)
    F.TOE, F.TOER = 0.026, 0.0062
    F.EYE = (0.0115, 0.0205)                          # the eye's half sizes
    F.EYEX, F.EYEZ = 0.036, 0.142
    F.VOXEL = 0.0026
    return F


def egg(V):
    """Fuller below the middle and a little flattened under, so the belly sits low over the feet."""
    V = V.copy()
    low = np.clip(-V[:, 2], 0, 1)
    V[:, :2] *= (1 + 0.1 * np.sin(low * math.pi * 0.9))[:, None]
    V[:, 2] = np.where(V[:, 2] < 0, np.maximum(V[:, 2] * 0.8, -0.68), V[:, 2])
    return V


def body(k, F):
    k.add("body", ell(F.C, F.R, deform=egg), TAN, "body")
    k.add("face", ell(F.FC, F.FR), TAN, "body")
    # the folded wings, a low bulge down each side inside the egg's outline, their lower feathers white
    k.add("wing", ell((0.07, 0.02, 0.112), (0.02, 0.054, 0.06), rot=(10, -8, 0)), WINGTAN, "wing{s}", mirror=True)
    k.paint(lambda P: inside(P[:, 2] - (0.064 + 0.25 * (P[:, 1] - 0.0)), 0.002) * (np.abs(P[:, 0]) > 0.062), WHITE,
            on=["wing"])
    # the pale grey belly under the bow tie
    def belly(P):
        d = np.sqrt((P[:, 0] / 0.064) ** 2 + ((P[:, 2] - 0.04) / 0.05) ** 2) - 1
        return inside(d * 0.06, 0.0015) * (P[:, 1] < -0.01) * (P[:, 2] < 0.075)
    k.paint(belly, GREY, on=["body", "face"])


FACE = [(0.0, 0.18), (0.012, 0.196), (0.033, 0.206), (0.054, 0.2), (0.07, 0.182), (0.079, 0.153), (0.078, 0.122),
        (0.068, 0.1), (0.05, 0.089), (0.03, 0.09), (0.013, 0.094), (0.0, 0.091)]


def face(k, F):
    """The white face: two discs round the eyes joined over the beak, the tan notch between their tops."""
    rc = round_coords(F.C[1], 0.09)
    poly = p.mirrored(FACE)
    k.paint(lambda P: p.poly_mask(*rc(P), poly, edge=0.0015) * (P[:, 1] < 0.0), WHITE, on=["face", "body"])


def beak(k, F):
    """A white beak standing down the face's middle, an orange point at its lower end."""
    P, n = on_ell(F.FC, F.FR, 0.0, 0.146)
    k.add("beak", ell_frame(P + n * 0.002, (0.016, 0.0095, 0.024), (1, 0, 0), n),
          BEAKC, "body", fuse=False, surface="shell")
    # the orange point: a wedge under the white, its lower edge cut level
    Q, m = on_ell(F.FC, F.FR, 0.0, 0.128)
    k.add("beaktip", ell_frame(Q + m * 0.004, (0.013, 0.0085, 0.0105), (1, 0, 0), m,
                               deform=lambda V: np.where(V[:, 2:3] > 0.35, V * [1, 1, 0] + [0, 0, 0.35], V)),
          ORANGE, "body", fuse=False, surface="shell")


# --- the eyes -----------------------------------------------------------------------------------------------------
INK = srgb(INKH)
GLINT = srgb("#FFFFFF")
SHEEN = srgb("#5A5052")


def eye_open(u, v):
    """A tall black oval, a grey sheen low on it and a small white glint high on its outer side."""
    c = np.tile(INK, (len(u), 1))
    c[(u / 0.62) ** 2 + ((v + 0.5) / 0.32) ** 2 < 1] = SHEEN
    c[((u - 0.3) / 0.26) ** 2 + ((v - 0.45) / 0.18) ** 2 < 1] = GLINT
    return c


def eyes(k, F, lid=WHITE, centre=None, normal=None, half=None, look=eye_open):
    LID = srgb(lid)
    if centre is None:
        centre, normal = on_ell(F.FC, F.FR, F.EYEX, F.EYEZ)
        normal = unit(*(normal + np.array([0, 0, 0.0])))
    half = half or F.EYE
    def lay(state):
        k.decal(centre, normal, half, eye_look(state, look, LID, INK, line=0.16), eps=0.0018, rings=20, segs=56,
                mirror=True)
    return lay


def rowlet(src, out):
    F = frame()
    k = Kit(src, coat="fur")
    body(k, F)
    face(k, F)
    beak(k, F)
    # the egg is a tenth fuller here than its ellipsoid (egg())
    bow_tie(k, F.C, F.R * [1.08, 1.08, 0.92], 0.074, 0.029, 0.018, 0.0085, GREEN, GREENDARK)
    feet(k, F)
    k.build(voxel=F.VOXEL, band=0.004, wband=0.03)
    k.eye_states(eyes(k, F))
    k.export(out)


if __name__ == "__main__":
    rowlet(*args())
