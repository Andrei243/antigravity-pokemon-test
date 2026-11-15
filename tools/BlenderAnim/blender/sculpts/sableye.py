# Sableye sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sableye.py -- <the export's Sableye.glb> <out.glb>
#
# From the references: a purple imp crouched on bent legs, most of it a broad rounded head wider than tall with a flat
# front, its eyes two great pale blue cut gems in a hexagon (loose faceted hulls that glow), a dark setting round each;
# two long pointed ears rising up and out from the head's upper corners, each with a second prong off its lower edge;
# a small hunched body with a red oval gem on the chest and two small blue gems on the back; long thin arms held
# forward and down to three-clawed hands with a spike at the wrist; crouched legs on clawed feet with a spike at the
# heel. The pictures show no mouth (the closed face of both references), so none is drawn.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, boxy, polygon, eye_look, args

PURPLE, INK = "#6A4CBC", "#24183F"
GEM, GEM_RIM, GEM_GLOW = "#CDEBF7", "#33427E", "#B8E6FA"
RED, RED_GLOW = "#E2333F", "#F04450"
GLOW = os.environ.get("SABLEYE_NOGLOW") is None       # judge the gems' colour on the turntable with the glow off

H = np.array([0.0, -0.055, 0.385])                    # the head's centre and radii (a rounded box)
HR = np.array([0.15, 0.118, 0.114])
BOX = 2.6


def on_head(x, z):
    """The point of the head's front with that x and z, and the surface's normal there."""
    a = abs(x - H[0]) / HR[0]
    b = abs(z - H[2]) / HR[2]
    dy = HR[1] * max(1 - a ** BOX - b ** BOX, 0.0) ** (1 / BOX)
    p = np.array([x, H[1] - dy, z])
    q = (p - H) / HR
    n = np.sign(q) * np.abs(q) ** (BOX - 1) / HR
    return p, n / np.linalg.norm(n)


def frame(n, up=(0, 0, 1.0)):
    n = np.array(n, float)
    right = np.cross(np.array(up, float), n); right /= np.linalg.norm(right)
    return right, np.cross(n, right)


def gem(c, n, rx, rz, h, sides=6, table=0.55, sink=0.012, phase=0.0, cut=None, up=(0, 0, 1.0)):
    """
    A cut gem standing out of the surface at c along n: a ring sunk into it, a girdle and a smaller flat table, each a
    polygon of `sides` corners (the first on +right). cut(u, v) -> v limit clips the outline from above (a narrowed eye).
    """
    n = unit(*n)
    right, up2 = frame(n, up)
    pts = []
    for i in range(sides):
        a = phase + 2 * math.pi * i / sides
        u, v = math.cos(a), math.sin(a)
        if cut is not None:
            v = min(v, cut(u))
        e = right * u * rx + up2 * v * rz
        pts.append(c - n * sink + e * 0.9)
        pts.append(c + n * h * 0.4 + e)
        pts.append(c + n * h + e * table)
    return hull(pts)


def claw(base, d, length, width, thick, up=(0, 0, 1)):
    """A claw or spike: a flat-faced solid from a kite at its root to its point along d."""
    d = unit(*d)
    a = np.cross(d, np.array(up, float)); a /= np.linalg.norm(a)
    b = np.cross(a, d)
    base = np.array(base, float)
    mid = base + d * length * 0.45
    pts = [base + a * width / 2, base - a * width / 2, base + b * thick / 2, base - b * thick / 2 - d * 0.004,
           mid + a * width * 0.28, mid - a * width * 0.28, mid + b * thick * 0.28, mid - b * thick * 0.28,
           base + d * length]
    return hull(pts)


def blade_hull(base, tip, width, thick, across, bend=(0, 0, 0)):
    """An ear or prong: a flat solid from a root of `width` (along `across`) and `thick` to a point, a little bent."""
    base, tip = np.array(base, float), np.array(tip, float)
    d = tip - base
    a = unit(*across)
    t = unit(*np.cross(a, d))
    mid = base + d * 0.5 + np.array(bend)
    pts = [base + a * width / 2, base - a * width / 2, base + t * thick / 2, base - t * thick / 2,
           mid + a * width * 0.3, mid - a * width * 0.3, mid + t * thick * 0.32, mid - t * thick * 0.32, tip]
    return hull(pts)


def sableye(src, out):
    k = Kit(src, coat="fur")
    gl = (lambda c: c) if GLOW else (lambda c: None)

    # --- the head: a broad rounded box, its front flat ------------------------------------------------------------
    k.add("head", ell(H, HR, deform=boxy(BOX)), PURPLE, "head")
    k.add("jowl", ell((0, -0.02, 0.31), (0.11, 0.09, 0.06)), PURPLE, "head")

    # the ears: long points up and out from the head's upper corners, each with a prong off its lower edge
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        B = np.array([0.11, -0.01, 0.44]) * m
        T = np.array([0.27, 0.03, 0.6]) * m
        k.add(f"ear{side}", blade_hull(B, T, 0.085, 0.036, (s * -0.7, 0, 0.7), bend=(0, 0.0, 0.01)), PURPLE,
              f"ear{side}", fuse=False)
        P = B + (T - B) * 0.42 + np.array([s * 0.02, 0, -0.02])
        k.add(f"prong{side}", blade_hull(P, P + np.array([s * 0.085, 0.01, -0.005]), 0.045, 0.03, (0, 0, 1)),
              PURPLE, f"ear{side}", fuse=False)

    # --- the body: small, hunched forward under the head ------------------------------------------------------
    k.add("torso", ell((0, 0.0, 0.235), (0.07, 0.064, 0.085), rot=(15, 0, 0)), PURPLE, "body")
    k.add("hips", ell((0, 0.012, 0.165), (0.066, 0.058, 0.05)), PURPLE, "body")
    # the red gem on the chest, the two blue gems on the back
    k.add("chestgem", gem(np.array([0, -0.062, 0.215]), (0, -1, 0.22), 0.022, 0.029, 0.014, sides=8, sink=0.01,
                          phase=math.pi / 8), RED, "body", fuse=False, glow=gl(RED_GLOW), surface="shell")
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"backgem{side}", gem(np.array([s * 0.036, 0.06, 0.262]), (s * 0.45, 1, 0.25), 0.015, 0.018, 0.011,
                                    sides=6, sink=0.008), GEM, "body", fuse=False, glow=gl(GEM_GLOW), surface="shell")

    # --- the arms: long and thin, forward and down, three claws a hand and a spike at the wrist ------------------
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"arm{side}", tube(np.array([(0.06, -0.02, 0.29), (0.14, -0.07, 0.245), (0.175, -0.13, 0.18)]) * m,
                                 [0.024, 0.02, 0.019]), PURPLE, f"arm{side}")
        HC = np.array([0.183, -0.15, 0.158])
        k.add(f"hand{side}", ell(HC * m, (0.03, 0.032, 0.026)), PURPLE, f"arm{side}")
        for j, d in enumerate([(0.55, -0.75, -0.35), (0.1, -0.85, -0.5), (-0.4, -0.75, -0.45)]):
            d = np.array(d)
            b = HC + unit(*d) * 0.018
            k.add(f"finger{side}{j}", claw(b * m, d * m, 0.052, 0.032, 0.026), PURPLE, f"arm{side}", fuse=False)
        W = np.array([0.18, -0.115, 0.19])
        k.add(f"wrist{side}", claw(W * m, np.array([0.7, 0.4, 0.5]) * m, 0.05, 0.03, 0.026), PURPLE, f"arm{side}",
              fuse=False)

    # --- the legs: crouched, the knees out and forward, clawed feet with a spike at the heel ---------------------
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array([0.072, -0.005, 0.15]) * m, (0.045, 0.05, 0.045)), PURPLE, f"leg{side}")
        k.add(f"leg{side}", tube(np.array([(0.065, 0.0, 0.155), (0.125, -0.055, 0.115), (0.12, -0.02, 0.06),
                                           (0.115, -0.02, 0.03)]) * m, [0.034, 0.028, 0.024, 0.024]), PURPLE,
              f"leg{side}")
        F = np.array([0.115, -0.055, 0.022])
        k.add(f"foot{side}", ell(F * m, (0.032, 0.05, 0.022)), PURPLE, f"leg{side}")
        for j, dx in enumerate((-0.022, 0.0, 0.022)):
            b = np.array([0.115 + dx, -0.09, 0.018])
            k.add(f"toe{side}{j}", claw(b * m, np.array([dx * 1.5, -1, -0.2]) * m, 0.04, 0.03, 0.022), PURPLE,
                  f"leg{side}", fuse=False)
        k.add(f"heel{side}", claw(np.array([0.12, 0.0, 0.05]) * m, np.array([0.5, 0.8, 0.2]) * m, 0.05, 0.03, 0.024),
              PURPLE, f"leg{side}", fuse=False)

    # --- the eyes: a hexagonal cut gem in a dark setting, open; shut, the lid of the head's own purple ----------
    EYE = {}
    for s in (1, -1):
        p, n = on_head(s * 0.07, H[2] - 0.005)
        EYE[s] = (p, n)
    R = 0.05                             # the gem's corner radius (a corner each side, flat top and bottom)
    HEX = [(math.cos(a), math.sin(a)) for a in np.linspace(0, 2 * math.pi, 7)[:-1]]
    CL, CI = srgb(PURPLE), srgb("#160E2C")

    def fierce_top(u_out):
        return 0.35 + 0.45 * u_out          # the top cut slanting down to the inner corner

    def eyes(state):
        for s in (1, -1):
            p, n = EYE[s]
            if state in ("Open", "Fierce"):
                cut = (lambda u: fierce_top(s * u)) if state == "Fierce" else None
                ring = [(u, min(v, cut(u)) if cut else v) for u, v in HEX]
                if cut:
                    ring = [(u, v) for u, v in ring]
                k.decal(p, n, (R * 1.18, R * 1.18), GEM_RIM, eps=0.004, outline=polygon(ring), rings=20, segs=56)
                k.add(f"eye{'R' if s > 0 else 'L'}", gem(p, n, R, R, 0.024, sides=6, table=0.52, sink=0.012, cut=cut),
                      GEM, "head", fuse=False, glow=gl(GEM_GLOW), surface="shell")
            else:
                look = eye_look(state, None, CL, CI, line=0.15)
                f = look if s > 0 else (lambda u, v, look=look: look(-u, v))
                k.decal(p, n, (R * 1.1, R * 1.1), f, eps=0.005, outline=polygon(HEX), rings=20, segs=56)

    k.build(voxel=0.006, smooth=4)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    sableye(*args())
