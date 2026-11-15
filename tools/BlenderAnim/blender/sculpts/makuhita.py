# Makuhita sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/makuhita.py -- <the export's Makuhita.glb> <out.glb>
# From the references: a round yellow sumo, one squat pear of a body with the head grown into its top (no neck), the
# head a wide flattened oval; a yellow topknot on the crown, a ball with three fat petal lobes (one up, one to each
# side); the eyes always shut, two short dark arcs; a wide smiling line of a mouth; a red ring on each cheek, the
# yellow showing inside it; a grey-blue band round the neck under the chin, running down the front into a tongue-shaped
# flap over the belly; short yellow arms ending in big round grey-blue mitts; short thick legs under the belly on wide
# flat feet with three toes. (The pictures overrule the old C# sculpt: the knot is yellow, not black; the mitts and band
# are grey-blue, not brown and black.)
# Reference ratios (3D picture): head chin to crown / height 0.35; the knot above the crown / height 0.2; head width /
# body width 0.65; body width / height 0.67; a mitt across / height 0.22; feet showing under the belly / height 0.08.
# Written to be grown from (hariyama.py keeps this frame: the body, the legs, the mitts' and the face's way).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, boxy, eye_look, polygon, outward, args

# --- colours ----------------------------------------------------------------------------------------------------------
YELLOW = "#F6D864"
DARK = "#4B6177"           # the mitts and the band: grey-blue
RING = "#EA5C68"
INK = "#2C2622"

# --- the frame (the skeleton's joints: body (0, 0, 0.28), head (0, -0.01, 0.38), the knot's lobes on the "ears"
#     (+-0.08, 0, 0.46), arms (+-0.12, 0, 0.32), legs (+-0.07, 0, 0.14)) ------------------------------------------------
H = np.array([0.0, -0.02, 0.405])            # the head's middle
HR = np.array([0.152, 0.128, 0.104])         # wide and flattened
BELLY = (np.array([0.0, 0.0, 0.205]), np.array([0.205, 0.17, 0.16]))
CHEST = (np.array([0.0, 0.005, 0.315]), np.array([0.165, 0.14, 0.1]))
KNOT = np.array([0.0, 0.01, 0.52])
LEG_X = 0.095


def on_head(x, z, lift=0.0, h=None, hr=None):
    """The head's front point at (x, z) (an ellipsoid's), lifted along its normal: where a patch goes."""
    h = H if h is None else h
    hr = HR if hr is None else hr
    zn = (z - h[2]) / hr[2]
    q = max(0.0, 1 - zn * zn - (x / hr[0]) ** 2)
    p = np.array([x, h[1] - hr[1] * math.sqrt(q), z])
    n = (p - h) / hr ** 2; n /= np.linalg.norm(n)
    return p + n * lift, n


def in_poly(x, y, pts):
    pts = np.array(pts, float)
    res = np.zeros(len(x), bool)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        res ^= ((y1 > y) != (y2 > y)) & (x < (x2 - x1) * (y - y1) / (y2 - y1 + 1e-12) + x1)
    return res


def poly_mask(x, y, pts, edge=0.003):
    """A polygon in two coordinates as a paint mask with a soft edge."""
    pts = np.array(pts, float)
    d = np.full(len(x), 9.0)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        ex, ey = x2 - x1, y2 - y1
        t = np.clip(((x - x1) * ex + (y - y1) * ey) / (ex * ex + ey * ey + 1e-12), 0, 1)
        d = np.minimum(d, np.hypot(x - x1 - t * ex, y - y1 - t * ey))
    return inside(np.where(in_poly(x, y, pts), -d, d), edge)


def add_body(k, belly=BELLY, chest=CHEST, color=YELLOW):
    """One squat pear: a round belly heaviest low down, a narrower chest the head grows out of."""
    k.add("belly", ell(*belly), color, "body")
    k.add("chest", ell(*chest), color, "body")
    # a broad round rump behind, so from behind the lower body is one mass
    k.add("rump", ell(belly[0] + (0, 0.04, 0.0), belly[1] * (0.92, 0.75, 0.9)), color, "body")


def add_head(k, h=H, hr=HR, color=YELLOW):
    def bean(V):
        V = boxy(2.3)(V)
        V[:, 0] *= 1 + 0.06 * np.clip(-V[:, 2], 0, 1)       # a little fuller low at the cheeks
        return V
    k.add("head", ell(h, hr, deform=bean), color, "head")


def add_knot(k, at=KNOT, color=YELLOW, scale=1.0):
    """The topknot: a ball on the crown, a fat petal standing up from it and one lying out to each side."""
    s = scale
    k.add("knot", ell(at, np.array((0.044, 0.04, 0.034)) * s), color, "head")
    up = at + np.array((0.0, 0.004, 0.015)) * s
    k.add("knotup", blade([up, up + np.array((-0.004, 0.004, 0.026)) * s, up + np.array((-0.008, 0.008, 0.05)) * s,
                           up + np.array((-0.012, 0.01, 0.066)) * s],
                          np.array([0.026, 0.033, 0.026, 0.018]) * s, (0, 1, 0), 0.7), color, "head")
    side = at + np.array((0.018, 0.004, 0.004)) * s
    k.add("knotside", blade([side, side + np.array((0.026, 0.006, 0.006)) * s, side + np.array((0.05, 0.01, 0.014)) * s,
                             side + np.array((0.066, 0.012, 0.024)) * s],
                            np.array([0.022, 0.028, 0.024, 0.016]) * s, (0, 0.4, 1), 0.7), color, "ear{s}", mirror=True)


def add_legs(k, color=YELLOW, x=LEG_X, top=0.15, r=0.06, foot=(0.064, 0.082, 0.03), toes=True, foot_color=None,
             ankle=0.05):
    """Short thick legs: a haunch fused into the belly, a stub of shin, a wide flat foot with three toes in front."""
    fcol = foot_color or color
    k.add("haunch", ell((x - 0.01, 0.0, top), (r * 1.25, r * 1.2, r * 1.2)), color, "leg{s}", mirror=True)
    k.add("leg", tube([(x, 0.0, top), (x + 0.005, -0.015, ankle)], [r, r * 0.9]), color, "leg{s}", mirror=True)
    fc = np.array((x + 0.008, -0.045 * foot[1] / 0.082, foot[2]))
    k.add("foot", ell(fc, foot), fcol, "leg{s}", mirror=True)
    if toes:
        for i, dx in enumerate((-0.03, 0.0, 0.03)):
            k.add(f"toe{i}", ell(fc + (dx * foot[0] / 0.064, -foot[1] * 0.82, -0.004),
                                 (foot[0] * 0.34, foot[0] * 0.36, foot[2] * 0.85)), fcol, "leg{s}", mirror=True)


def add_arms(k, color=YELLOW, mitt=DARK, shoulder=(0.145, 0.0, 0.305), elbow=(0.2, -0.03, 0.255),
             hand=(0.222, -0.105, 0.215), r=0.046, fist=(0.07, 0.068, 0.07)):
    """Short arms held forward and a little down, ending in big round mitts (loose, so their edge stays crisp)."""
    k.add("shoulder", ell(shoulder, (r * 1.15, r * 1.1, r * 1.1)), color, "arm{s}", mirror=True)
    k.add("arm", tube([shoulder, elbow, hand], [r, r * 0.95, r * 0.9]), color, "arm{s}", mirror=True)
    hand = np.array(hand)
    k.add("mitt", outward(ell(hand + (0.008, -0.01, -0.004), fist, deform=boxy(2.4))), mitt, "arm{s}", fuse=False,
          mirror=True, surface="leather")
    k.add("thumb", outward(ell(hand + (-0.052, -0.04, 0.02), (0.026, 0.028, 0.03))), mitt, "arm{s}", fuse=False,
          mirror=True, surface="leather")


def add_band(k, color=DARK, z0=0.262, z1=0.302, flap=None):
    """The grey-blue band round the neck under the chin, running down the front into a tongue-shaped flap."""
    flap = flap or [(-0.055, 0.29), (0.055, 0.29), (0.055, 0.2), (0.042, 0.168), (0.0, 0.155), (-0.042, 0.168),
                    (-0.055, 0.2)]
    def band(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        ring = inside(z - z1, 0.0025) * inside(z0 - z, 0.0025) * inside(np.abs(x) - 0.128, 0.004)          # round the neck, short of the shoulders
        front = poly_mask(x, z, flap, 0.0025) * (y < -0.05)
        return np.maximum(ring, front)
    k.paint(band, color)


# --- the face: patches on the fused head, laid before the body's build ----------------------------------------------
CY, CR, CI = srgb(YELLOW), srgb(RING), srgb(INK)


def add_rings(k, x=0.128, z=0.382, r=0.037, inner=0.021, h=None, hr=None, ground=YELLOW, color=RING):
    """A red ring on each cheek: a disc of red and a smaller disc of the face's colour on it."""
    p, n = on_head(x, z, 0.0, h, hr)
    n = unit(n[0] * 1.3, n[1], n[2])
    k.decal(p, n, (r, r), color, eps=0.004, rings=14, segs=56, mirror=True)
    k.decal(p, n, (inner, inner), ground, eps=0.0065, rings=10, segs=48, mirror=True)


def smile(u, v):
    """A wide smiling line, the corners up."""
    c = np.tile(CY, (len(u), 1))
    c[(np.abs(v - (-0.45 + 0.95 * u * u)) < 0.2) & (np.abs(u) < 0.9)] = CI
    return c


def add_mouth(k, at=None, half=(0.07, 0.03)):
    if at is None:
        at, _ = on_head(0.0, H[2] - 0.055)
    k.decal(at, unit(0, -1, -0.15), half, smile, eps=0.004, rings=16, segs=72)


def slit(u, v):
    """The shut eye: a short arc bowed up, its inner end a little lower."""
    c = np.tile(CY, (len(u), 1))
    c[(np.abs(u) < 0.88) & (np.abs(v - (0.42 * (1 - u * u) - 0.22 + 0.12 * u)) < 0.24)] = CI
    return c


def shut_eyes(state, lid=CY, ink=CI, look=slit):
    """eye_look's states on the slit, but Fierce is the same stroke straightened and slanted down to the nose."""
    if state != "Fierce":
        return eye_look(state, look, lid, ink, line=0.22)
    def fierce(u, v):
        c = np.tile(lid, (len(u), 1))
        c[(np.abs(u) < 0.88) & (np.abs(v - (0.5 * u - 0.05)) < 0.24)] = ink
        return c
    return fierce


EYE_X, EYE_Z, EYE_HALF = 0.06, 0.428, (0.036, 0.026)


def lay_eyes(k, state):
    p, n = on_head(EYE_X, EYE_Z)
    k.decal(p, n, EYE_HALF, shut_eyes(state), eps=0.004, rings=20, segs=56, mirror=True, up=unit(-0.12, 0, 1))


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    add_body(k)
    add_legs(k)
    add_arms(k)
    add_head(k)
    add_knot(k)
    add_band(k)
    add_rings(k)
    add_mouth(k)
    k.build(voxel=0.0068, band=0.006)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)
