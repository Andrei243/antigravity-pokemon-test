# Ducklett sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ducklett.py -- <the export's Ducklett.glb> <out.glb>
# From the references: a pale sky-blue duckling, a round head on a bean of a body with hardly a neck; a tuft on the
# crown of two round lobes, a heart standing up; a broad flat yellow bill drooping a little over a pouting lower bill;
# upright oval white eyes far apart, a black rim and a black pupil toward the inner top; a heart of deeper blue on the
# chest running down into a fluffy cloud of deeper blue down round the bottom of the body; wings like hands of three
# round feather fingers, held a little out and forward at the sides; short legs and broad webbed orange-yellow feet.
# Written to be grown from: swanna.py imports it (sibling("ducklett")) for its frame helpers and its way of making eyes.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, slab, srgb, inside, smoothstep, lumpy, unit, eye_look, args

TEAL, BLUE, BILL, BILLLOW, FOOT, INK = "#9DD7E6", "#4FA4DB", "#F4C03E", "#E2A532", "#F2B436", "#1A1C22"

H = np.array([0, -0.012, 0.247])            # the head's centre, over the head's joint (0, -0.02, 0.19)
HR = np.array([0.071, 0.066, 0.064])
VOXEL = 0.0036


def frame_ell(c, r, ex, ey, deform=None, sub=4):
    """An ellipsoid laid along a frame: radii r along ex, ey (made square to ex) and their cross product."""
    ex = np.array(ex, float); ex /= np.linalg.norm(ex)
    ey = np.array(ey, float); ey = ey - ex * (ey @ ex); ey /= np.linalg.norm(ey)
    ez = np.cross(ex, ey)
    V, F = ell((0, 0, 0), r, deform=deform, sub=sub)
    M = np.stack([ex, ey, ez], axis=1)
    return V @ M.T + np.array(c, float), F


def droop(V):
    """The bill: broad and rounded at its tip, drawn in at its root, its tip turned down a little."""
    V = V.copy()
    t = np.clip(-V[:, 1], -1, 1)
    V[:, 0] *= 0.62 + 0.42 * smoothstep(-0.95, 0.3, t)
    V[:, 2] -= 0.35 * np.clip(t, 0, 1) ** 2
    return V


# --- the parts ----------------------------------------------------------------------------------------------------
def body(k):
    k.add("body", ell((0, 0.006, 0.128), (0.078, 0.074, 0.088)), TEAL, "body")
    k.add("neck", ell((0, -0.006, 0.19), (0.058, 0.056, 0.045)), TEAL, "head")
    k.add("head", ell(H, HR), TEAL, "head")
    # the cloud of down round the bottom, lumpy
    k.add("skirt", ell((0, 0.006, 0.072), (0.086, 0.082, 0.05), deform=lumpy(5, 2, 0.07)), BLUE, "body")
    # a short pointed tail behind
    k.add("tail", blade([(0, 0.06, 0.13), (0, 0.092, 0.142), (0, 0.112, 0.155)], [0.03, 0.02, 0.006], (1, 0, 0), 0.5),
          TEAL, "tail")


def down_line(P):
    """The deeper blue's top edge round the body, a little wavy."""
    th = np.arctan2(P[:, 0], P[:, 1])
    return 0.088 + 0.005 * np.sin(5 * th)


def heart(P):
    """The heart on the chest: two round lobes over a point that runs down into the down."""
    x, z = np.abs(P[:, 0]), P[:, 2]
    lobe = np.hypot(x - 0.0175, z - 0.123) - 0.02
    tri_ = np.maximum(z - 0.123, (z - 0.074) * 0.75 - (0.034 - x) * 1.0)       # under the lobes, narrowing to a point
    tri_ = np.maximum(tri_, x - 0.036)
    return inside(np.minimum(lobe, tri_), 0.0022) * (P[:, 1] < -0.02) * (z < 0.16)


def colours(k):
    k.paint(lambda P: inside(P[:, 2] - down_line(P), 0.0022) * (P[:, 2] < 0.13) * (np.hypot(P[:, 0], P[:, 1] - 0.006) < 0.098), BLUE)
    k.paint(lambda P: inside(down_line(P) - P[:, 2], 0.0022), TEAL, on=["skirt"])
    k.paint(heart, BLUE)


def tuft(k):
    """Two round lobes on a short stalk from the crown, a heart standing up, its faces to the front and the back."""
    k.add("stalk", tube([(0, 0.004, 0.296), (0, 0.006, 0.322)], [0.0125, 0.0105]), TEAL, "head")
    for s in (1, -1):
        k.add("lobe", frame_ell((0.0125 * s, 0.006, 0.337), (0.0105, 0.0225, 0.0175),
                                (0, 1, 0), (math.sin(math.radians(28)) * s, 0, math.cos(math.radians(28)))),
              TEAL, "head")


def bill(k):
    k.add("bill", ell((0, -0.083, 0.224), (0.05, 0.04, 0.0155), rot=(4, 0, 0), deform=droop), BILL, "head",
          fuse=False, surface="shell")
    k.add("billlow", ell((0, -0.072, 0.207), (0.036, 0.03, 0.011), rot=(-8, 0, 0), deform=droop), BILLLOW, "head",
          fuse=False, surface="shell")


def wings(k):
    """
    A hand of a wing each side: a broad flat mass from the shoulder, hanging down and a little out and forward, its end
    three round feather fingers fanned.
    """
    sh = np.array([0.064, 0.004, 0.165])                  # the wing's joint (0.06, 0.01, 0.16)
    d = unit(0.46, -0.45, -0.77)                          # down the wing, out and forward
    ch = unit(0, 1, -0.38)                                # across it, toward the back
    k.add("wing", frame_ell(sh + d * 0.048, (0.058, 0.046, 0.016), d, ch), TEAL, "wing{s}", mirror=True)
    for j, a in enumerate((-0.6, 0.0, 0.6)):
        fd = unit(*(d + ch * a * 0.75))
        root = sh + d * 0.083 + ch * a * 0.034
        k.add(f"finger{j}", frame_ell(root + fd * 0.022, (0.032, 0.0165, 0.0135), fd, ch), TEAL,
              "wing{s}", mirror=True)


def _foot():
    """A webbed foot: three toes with round tips fanned forward, the web drawn in between them."""
    pts = [(0.0, -0.011), (0.012, -0.006)]
    for i, a in enumerate((-28, 0, 28)):               # the toes from the +a side round to the -a side
        a = math.radians(-a)
        d = np.array([math.sin(a), math.cos(a)])
        side = np.array([d[1], -d[0]])
        tip = d * 0.05
        for t in np.linspace(-80, 80, 7):               # a round tip
            r = math.radians(t)
            pts.append(tuple(tip + 0.009 * (d * math.cos(r) - side * math.sin(r))))
        if i < 2:
            nd = np.array([math.sin(a - math.radians(28)), math.cos(a - math.radians(28))])
            pts.append(tuple((d + nd) / np.linalg.norm(d + nd) * 0.036))     # the web between two toes
    pts.append((-0.012, -0.006))
    return pts


FOOT_OUTLINE = _foot()


def feet(k):
    for s, side in ((1, "R"), (-1, "L")):
        x = 0.033 * s
        k.add(f"leg{side}", tube([(x, 0.0, 0.06), (x * 1.04, -0.006, 0.03), (x * 1.08, -0.01, 0.014)],
                                 [0.0125, 0.0115, 0.011]), FOOT, f"leg{side}", fuse=False, surface="scales")
        turn = math.radians(14 * s)
        ex = np.array([math.cos(turn), math.sin(turn), 0])
        ez = np.array([math.sin(turn), -math.cos(turn), 0])
        k.add(f"foot{side}", slab(FOOT_OUTLINE, (x * 1.12, -0.012, 0.0065), ex, ez, 0.012), FOOT, f"leg{side}",
              fuse=False, surface="scales")


# --- the eyes -----------------------------------------------------------------------------------------------------
WHITEC, INKC = np.array([1.0, 1.0, 1.0]), srgb(INK)
EYEDIR = unit(0.47, -0.84, 0.27)
EYE = (0.0155, 0.0225)


def eye_open(u, v):
    """A white upright oval in a black rim (the pupil is a patch of its own, toward the inner top)."""
    c = np.tile(WHITEC, (len(u), 1))
    c[u * u + v * v > 0.83 ** 2] = INKC
    return c


PUPIL = (-0.24, 0.06, 0.42, 0.53)          # its centre and half sizes in the eye's own units (u outward)


def eyes(k, centre=H, radii=HR, direction=EYEDIR, half=EYE, lid=TEAL, look=eye_open, pupil=PUPIL):
    E = centre + direction / np.linalg.norm(direction / radii)
    right = np.cross((0, 0, 1.0), direction); right /= np.linalg.norm(right)
    up = np.cross(direction, right)
    LID = srgb(lid)
    def lay(state):
        k.decal(E, direction, half, eye_look(state, look, LID, INKC, line=0.17), eps=0.0035, rings=20, segs=56,
                mirror=True)
        if state in ("Open", "Fierce") and pupil:
            # Fierce: the lid slants down toward the inner corner (eye_look), the pupil sits lower under it
            pu, pv, pa, pb = pupil if state == "Open" else (-0.1, -0.3, 0.34, 0.34)
            P = E + right * pu * half[0] + up * pv * half[1]
            k.decal(P, direction, (pa * half[0], pb * half[1]), INKC, eps=0.0045, rings=8, segs=40, mirror=True)
    return lay


def ducklett(src, out):
    k = Kit(src, coat="fur")
    body(k)
    tuft(k)
    wings(k)
    colours(k)
    bill(k)
    feet(k)
    k.build(voxel=VOXEL, band=0.006)
    k.eye_states(eyes(k))
    k.export(out)


if __name__ == "__main__":
    ducklett(*args())
