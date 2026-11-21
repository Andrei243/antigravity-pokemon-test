# Cryogonal sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cryogonal.py -- <the export's Cryogonal.glb> <out.glb>
#
# The design, from the references: a thick plate of blue ice cut as a hexagon, its corners to the left and right, its
# flat front turned to the cameras. The front steps in as a hexagonal funnel: frames of pale and deep blue, each a
# little further back, to a pale hexagon in the middle. Across it at the corners' height (a little above the middle)
# runs a black band, notched at the left and right corners, and on it the eyes: two glowing cyan arcs, each rising from
# the band and bending over outward. Below the middle the mouth: a black chevron like an upturned V with four round
# cyan beads on it. Round the hexagon, out from the middle of each of its six sides (the set turned a little
# clockwise in the pictures), a crystal like a thick X, two flat bars of blue ice crossing. Every crystal is a loose
# hull with flat faces; the band and the mouth are thin dark plates, fused, standing just proud of the funnel, which
# carry the eyes' patches (the Regis' way: a body of loose hulls with its face on a dark plate of the design).
# The skeleton: body (0, 0, 0.336) alone.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, unit, eye_look, boxy, hull, args

# --- colours ----------------------------------------------------------------------------------------------------------
PALE, PALE2, PALE3 = "#D4E6F8", "#B6D2F2", "#E6F1FB"
DEEP, ARM = "#5A8AD4", "#6C9ADC"
BLACK, CYAN, CYANHI = "#1C2230", "#62D2F8", "#C6F2FF"

# --- the frame --------------------------------------------------------------------------------------------------------
C = np.array([0.0, 0.0, 0.336])
BACK = 0.05                                   # the plate's back face
RINGS = [(0.215, 0.172, -0.046, PALE), (0.172, 0.14, -0.04, DEEP), (0.14, 0.108, -0.033, PALE2),
         (0.108, 0.074, -0.026, DEEP)]        # (outer radius, inner radius, front, colour), corner radii
MIDDLE = (0.074, -0.02, PALE3)
TURN = -10.0                                  # the crystals round the hexagon, turned a little clockwise
ARM_R, ARM_HALF_L, ARM_HALF_W, ARM_HALF_D = 0.238, 0.072, 0.022, 0.028
BAND_DZ, BAND_HALF = 0.016, (0.205, 0.036)
PLATE_Y = -0.0505                             # the dark plates' middle, just proud of the outer frame
EYE_X, EYE_HALF = 0.085, (0.06, 0.034)
BEADS = [(-0.03, -0.1), (0.008, -0.09), (-0.06, -0.136), (0.052, -0.124)]
VOXEL = 0.004


def corner(r, i, y):
    """The hexagon's corner i (0 on the right, going round counter-clockwise seen from the front) at radius r, depth y."""
    a = math.radians(60 * i)
    return np.array([C[0] + r * math.cos(a), y, C[2] + r * math.sin(a)])


def frames(k):
    """The stepped funnel: each frame six flat-faced trapezoids from its front back to the plate's back face."""
    for j, (ro, ri, front, col) in enumerate(RINGS):
        for i in range(6):
            pts = [corner(r, q, y) for r in (ro, ri) for q in (i, i + 1) for y in (front, BACK)]
            k.add(f"frame{j}_{i}", hull(pts), col, "body", fuse=False, surface="shell")
    r, front, col = MIDDLE
    k.add("middle", hull([corner(r, q, y) for q in range(6) for y in (front, BACK)]), col, "body", fuse=False,
          surface="shell")


def bar(c, d, half_l, half_w, half_d):
    """A flat bar of ice: a box along d in the face's plane, with its corners clipped so it reads as a crystal."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    s = np.array([-d[2], 0.0, d[0]])
    y = np.array([0.0, 1.0, 0.0])
    pts = []
    for a in (-1, 1):
        for b in (-1, 1):
            for e in (-1, 1):
                pts.append(c + d * a * half_l + s * b * half_w + y * e * half_d)
                pts.append(c + d * a * (half_l + half_w * 0.6) + y * e * half_d * 0.6)
    return hull(pts)


def crystals(k):
    for i in range(6):
        a = math.radians(90 + 60 * i + TURN)
        out = np.array([math.cos(a), 0.0, math.sin(a)])
        c = C + out * ARM_R + np.array([0, 0.004, 0])
        for t, f in ((45, 1.0), (-45, 0.82)):
            b = math.radians(t)
            d = out * math.cos(b) + np.array([-out[2], 0, out[0]]) * math.sin(b)
            k.add(f"arm{i}_{'a' if t > 0 else 'b'}", bar(c, d, ARM_HALF_L * f, ARM_HALF_W, ARM_HALF_D), ARM, "body",
                  fuse=False, surface="shell")


def dark_plates(k):
    """The black band across the corners and the mouth's chevron, thin plates that carry the face."""
    k.add("band", ell((0, PLATE_Y, C[2] + BAND_DZ), (BAND_HALF[0], 0.004, BAND_HALF[1]), deform=boxy(5)), BLACK, "body")
    # the notches at the left and right corners, where the band runs out over the rim
    for s in (1, -1):
        k.add("notch" + ("R" if s > 0 else "L"),
              ell((0.212 * s, PLATE_Y + 0.004, C[2] + BAND_DZ), (0.022, 0.006, 0.04), deform=boxy(3)), BLACK, "body")
    # the chevron: two legs from its peak down and out
    peak = np.array([0.0, PLATE_Y, C[2] - 0.072])
    for s in (1, -1):
        foot = np.array([0.078 * s, PLATE_Y, C[2] - 0.17])
        mid = (peak + foot) / 2
        ang = math.degrees(math.atan2(foot[0] - peak[0], peak[2] - foot[2]))
        L = np.linalg.norm(foot - peak) / 2 + 0.012
        k.add("mouth" + ("R" if s > 0 else "L"), ell(mid, (0.022, 0.004, L), rot=(0, -ang, 0), deform=boxy(4)), BLACK,
              "body")
    # the beads, round and glowing, on the chevron
    for i, (x, z) in enumerate(BEADS):
        k.add(f"bead{i}", ell((x, PLATE_Y - 0.008, C[2] + z), (0.015, 0.011, 0.015), sub=3), CYAN, "body", fuse=False,
              glow=CYAN, surface="shell")


def eye_open():
    """A glowing arc: low and flat at the inner end, rising and bending over outward and down to a short tail."""
    cy, hi, bl = srgb(CYAN), srgb(CYANHI), srgb(BLACK)
    def look(u, v):
        c = np.tile(bl, (len(u), 1))
        mid = -0.35 + 1.05 * np.exp(-((u - 0.12) / 0.42) ** 2)
        d = np.abs(v - mid)
        c[d < 0.44] = cy
        c[d < 0.16] = hi
        return c
    return look


def eyes(k, state):
    look = eye_look(state, eye_open(), srgb(BLACK), srgb(CYAN), line=0.2)
    for s in (1, -1):
        col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
        k.decal((EYE_X * s, PLATE_Y - 0.004, C[2] + BAND_DZ + 0.002), (0, -1.0, 0), EYE_HALF, col, eps=0.0015,
                rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    frames(k)
    crystals(k)
    dark_plates(k)
    k.build(voxel=VOXEL, smooth=2, band=0.004)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
