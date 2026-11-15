# Sharpedo sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sharpedo.py -- <the export's Sharpedo.glb> <out.glb>
#
# From the references: a torpedo of a shark, dark blue over its top and white under it, no tail (the body ends blunt
# behind its fins). A huge open maw at the front, rimmed in white, dark red inside with a pink tongue, big white
# triangular teeth along both jaws. A yellow three-pointed star on top of the snout. A small red eye ringed in black on
# each side, three black gill arcs behind it. A great dorsal fin leaning back with two notches in its trailing edge,
# long pectoral fins swept back and down, and a white fin hanging under the rear, notched behind.
#
# Grown from Carvanha (sibling("carvanha")): its way of placing a patch on a side, its fang, its fins as slabs.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, slab, hull, srgb, inside, unit, eye_look, polygon, sibling, args

cv = sibling("carvanha")
BLUE, WHITE, YELLOW, MAW, TONGUE, INK, IRIS, LIGHT = ("#2C5CA4", "#ECEEF4", "#F4CC3A", "#9C2834", "#E8828C",
                                                      "#14131A", "#D8283C", "#8EA6DE")

HC = np.array([0.0, -0.12, 0.252])        # the head's middle
HR = np.array([0.118, 0.15, 0.13])
BC = np.array([0.0, 0.03, 0.262])
BR = np.array([0.1, 0.17, 0.11])
BROT = (10, 0, 0)                          # the back end rises toward the tail's joint

# The maw: a cutter opening forward, inside the head's width
MC = np.array([0.0, -0.262, 0.236])
MR = np.array([0.076, 0.11, 0.087])


def q_ell(P, c, r, rot=(0, 0, 0)):
    m = np.array(Euler([math.radians(a) for a in rot]).to_matrix())
    return np.linalg.norm(((P - c) @ m) / r, axis=1)


def white_line(P):
    """Above nought blue, below white: along the side about the middle, rising toward the back with the body."""
    return P[:, 2] - (0.236 + 0.16 * P[:, 1])


def body(k):
    k.add("head", ell(HC, HR, sub=5), BLUE, "body")
    # The trunk: one tube tapering smoothly from the head's back to the blunt end at the tail's joint, a little
    # narrower than it is deep
    V, F = tube([(0, -0.1, 0.254), (0, 0.0, 0.26), (0, 0.1, 0.274), (0, 0.18, 0.29), (0, 0.222, 0.302)],
                [0.124, 0.116, 0.094, 0.062, 0.036], seg=32)
    V = V.copy(); V[:, 0] *= 0.86
    k.add("trunk", (V, F), BLUE, "body")
    k.add("rear", tube([(0, 0.15, 0.284), (0, 0.21, 0.298)], [0.07, 0.04]), BLUE, "tail")
    k.add("chin", ell((0.0, -0.18, 0.195), (0.1, 0.11, 0.075)), WHITE, "body")
    k.paint(lambda P: inside(white_line(P), 0.0012), WHITE)
    # The white rim round the maw (on the outer surface), the dark red inside, the pink tongue low in it
    def lips(P):
        qm = q_ell(P, MC, MR)
        qh = q_ell(P, HC, HR)
        return inside(qm - 1.32, 0.004) * (qh > 0.9) * (P[:, 1] < -0.12)
    k.paint(lips, WHITE)
    def hollow(P):
        return inside(q_ell(P, MC, MR) - 1.12, 0.004) * (q_ell(P, HC, HR) < 0.92)
    k.paint(hollow, MAW)
    k.paint(lambda P: hollow(P) * inside(P[:, 2] - (MC[2] - 0.032), 0.004), TONGUE)
    k.add("maw", ell(MC, MR), MAW, "body", carve=True)


def rim_point(psi):
    """Where the maw's cutter meets the head's surface at angle psi round the opening (0 the top, toward +X positive)."""
    best, bp = 9.0, None
    for s in np.linspace(0, math.pi / 2, 200):
        p = MC + np.array([MR[0] * math.sin(psi) * math.cos(s), -MR[1] * math.sin(s), MR[2] * math.cos(psi) * math.cos(s)])
        d = abs(q_ell(p[None], HC, HR)[0] - 1)
        if d < best:
            best, bp = d, p
    return bp


def teeth(k):
    for n, (deg, ln, w) in enumerate(((22, 0.036, 0.03), (55, 0.034, 0.03), (85, 0.03, 0.026),
                                      (150, 0.03, 0.026), (172, 0.03, 0.028))):
        for s in (1, -1):
            psi = math.radians(deg) * s
            p = rim_point(psi) + np.array([0, 0.012, 0])
            axis = np.array([MC[0], p[1], MC[2]])
            toward = unit(*(axis - p))
            out = unit(0, -1, 0)
            side = np.cross(toward, out); side /= np.linalg.norm(side)
            pts = [p + side * w / 2, p - side * w / 2, p - toward * 0.012, p + out * 0.01 - toward * 0.004,
                   p - out * 0.008, p + toward * ln + out * 0.004]
            k.add(f"tooth{n}{'R' if s > 0 else 'L'}", hull(pts), WHITE, "body", fuse=False, surface="shell")


def star(k):
    c = np.array([0.0, -0.17, 0.374])
    n = unit(0, -0.4, 1)
    pts = cv.star(3, 0.36, 90)
    k.decal(c, n, (0.046, 0.046), YELLOW, up=(0, -1, 0), outline=polygon(pts), eps=0.004, rings=12, segs=90)


DORSAL = [(-0.065, 0.0), (0.0, 0.07), (0.075, 0.15), (0.115, 0.185), (0.1, 0.13), (0.08, 0.125), (0.09, 0.085),
          (0.072, 0.08), (0.085, 0.035), (0.11, 0.0)]
PEC = [(0.0, 0.032), (0.1, 0.012), (0.215, -0.025), (0.11, -0.03), (0.0, -0.03)]
VENTRAL = [(-0.035, 0.0), (0.0, -0.06), (0.03, -0.12), (0.035, -0.08), (0.055, -0.075), (0.05, -0.035),
           (0.045, 0.0)]


def fins(k):
    cv.jagged(k, "dorsal", DORSAL, np.array([0.0, 0.0, 0.352]), unit(0.6, 0.8, 0.12), (0.0, 0.0, 1.0), 0.026, BLUE,
              "body")
    for s in (1, -1):
        cv.jagged(k, f"pec{'R' if s > 0 else 'L'}", PEC, np.array([0.092 * s, -0.06, 0.212]),
                  unit(0.72 * s, 0.6, -0.34), (0.0, -0.15, 1.0), 0.026, BLUE, f"fin{'R' if s > 0 else 'L'}")
    cv.jagged(k, "ventral", VENTRAL, np.array([0.0, 0.11, 0.2]), unit(0.6, 0.8, 0.0), (0.0, 0.0, 1.0), 0.026, WHITE,
              "tail")


# --- the eyes: small, red, ringed in black, a black pupil; three black gill arcs behind --------------------------------
EYE_A, EYE_Z = math.radians(46), HC[2] + 0.048
EYE_HALF = (0.025, 0.025)
CI, CK, CB, CL = srgb(IRIS), srgb(INK), srgb(BLUE), srgb(LIGHT)


def eye_open(u, v):
    c = np.tile(CI, (len(u), 1))
    c[u * u + v * v > 0.7 ** 2] = CK
    return c


def gills(k):
    def look(u, v):
        c = np.tile(CB, (len(u), 1))
        x = u + 0.45 * v * v
        for x0 in (-0.5, 0.05, 0.6):
            c[(np.abs(x - x0) < 0.15) & (np.abs(v) < 0.82)] = CK
        return c
    for s in (1, -1):
        cv.side_patch(k, math.radians(68), HC[2] + 0.045, (0.04, 0.036), look, side=s, c=HC, r=HR, rings=24, segs=120)


def eyes(k, state):
    ink = CK if state in ("Open", "Fierce") else CL
    for s in (1, -1):
        cv.side_patch(k, EYE_A, EYE_Z, EYE_HALF, eye_look(state, eye_open, CB, ink, line=0.16), side=s, c=HC, r=HR)
        if state in ("Open", "Fierce"):
            cv.side_patch(k, EYE_A, EYE_Z, (0.009, 0.009), INK, side=s, eps=0.0055, rings=8, segs=32, c=HC, r=HR)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="scales")
    body(k)
    star(k)
    gills(k)
    teeth(k)
    fins(k)
    k.build(voxel=0.0045, smooth=3, band=0.004)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
