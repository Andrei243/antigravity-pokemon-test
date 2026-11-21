# Bergmite sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/bergmite.py -- <the export's Bergmite.glb> <out.glb>
#
# The design, from the references (they overrule the task's "four short legs, a flat top and a dark face low in
# front": Bergmite has no legs at all): a lump of pale blue ice cut in flat facets, its broad flat front low down, a
# tall crystal spire standing up from its crown and leaning a little back, a flat triangular crystal like an ear on
# its left in front of the spire, two small points on its right flank. Under it a white lip of ice that juts forward
# under the face like a jaw, its top edge a zigzag of blue points, a row of small icicles under it. On the front two
# big round eyes rimmed lilac, yellow-orange with a small dark pupil, and between them a thin dark line of a mouth.
# Every crystal is a loose hull with flat faces; the face's patches lie on a thin fused plate just proud of the flat
# front (the Regis' way). The skeleton: body (0, 0, 0.1) alone; the export is 0.275 tall.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, unit, eye_look, boxy, hull, axes, args

# --- colours (the line's: Avalugg takes them) -------------------------------------------------------------------------
ICE, ICE2, ICE3, ICEDEEP = "#B2D8F2", "#C6E3F7", "#9CC8EC", "#86B4E4"
WHITE, ICICLE = "#EAF4FB", "#D6ECF8"
LILAC, IRIS, IRIS2, PUPIL = "#8E6CC6", "#F6B62A", "#FFD45A", "#2A2036"
INK = "#2E3046"

# --- the frame --------------------------------------------------------------------------------------------------------
FRONT_Y = -0.088                  # the flat front's plane
FRONT_X, FRONT_Z0, FRONT_Z1 = 0.098, 0.034, 0.116   # its half width and its foot and top
FACE_Y = FRONT_Y - 0.0009        # where the face's patches are cast: the plate's front
EYE_X, EYE_Z, EYE_R = 0.045, 0.075, 0.0255
MOUTH_Z = 0.056
VOXEL = 0.0028


def crystal(p0, p1, prof, n=6, turn=0.0, flat=1.0):
    """
    A crystal along p0 -> p1: rings of n sides at shares t of the way, (t, radius) each (a radius of nought a point),
    made a hull so its faces are flat; flat squashes it across (1 round, less a blade).
    """
    p0, p1 = np.array(p0, float), np.array(p1, float)
    d = p1 - p0
    a, b = axes(d)
    pts = []
    for t, r in prof:
        c = p0 + d * t
        if r <= 0:
            pts.append(c)
            continue
        for j in range(n):
            th = 2 * math.pi * j / n + math.radians(turn)
            pts.append(c + r * (math.cos(th) * a + flat * math.sin(th) * b))
    return hull(np.array(pts))


def jitter(pts, amount, seed, keep=None):
    """Points nudged a little each (the ice's facets uneven), those keep() picks left where they are."""
    rs = np.random.RandomState(seed)
    pts = np.array(pts, float)
    d = rs.uniform(-amount, amount, pts.shape)
    if keep is not None:
        d[np.array([keep(p) for p in pts])] = 0
    return pts + d


def body(k):
    fx, fy = FRONT_X, FRONT_Y
    pts = [
        # the flat front, low down
        (-fx, fy, FRONT_Z0), (fx, fy, FRONT_Z0), (-fx, fy, FRONT_Z0 + 0.03), (fx, fy, FRONT_Z0 + 0.03),
        (-0.07, fy, FRONT_Z1), (0.07, fy, FRONT_Z1), (0, fy, FRONT_Z0),
        # the front's upper corners cut back
        (-0.108, -0.06, 0.11), (0.106, -0.058, 0.108),
        # the foot, over the white lip
        (-0.1, -0.045, 0.034), (0.1, -0.045, 0.034), (-0.096, 0.035, 0.036), (0.098, 0.032, 0.034),
        (-0.05, 0.08, 0.035), (0.055, 0.078, 0.036), (0, 0.086, 0.034),
        # the widest round, a little over the front's middle
        (-0.12, -0.035, 0.082), (0.118, -0.03, 0.078), (-0.114, 0.035, 0.09), (0.112, 0.045, 0.086),
        (-0.07, 0.088, 0.085), (0.075, 0.086, 0.08), (0, 0.1, 0.088),
        # the brow over the front, sloping back
        (-0.075, -0.062, 0.14), (0.08, -0.058, 0.135), (0, -0.068, 0.146),
        # the shoulders round to the crown
        (-0.09, 0.025, 0.142), (0.092, 0.03, 0.138), (-0.05, 0.075, 0.135), (0.056, 0.07, 0.13),
        (-0.04, -0.018, 0.178), (0.045, -0.01, 0.172), (0.0, 0.035, 0.186),
    ]
    pts = jitter(pts, 0.006, 3, keep=lambda p: p[1] <= FRONT_Y + 1e-6)
    k.add("body", hull(pts), ICE, "body", fuse=False, surface="shell")
    # the thin plate the face's patches lie on, just proud of the flat front
    k.add("face", ell((0, FACE_Y + 0.0075, 0.074), (0.08, 0.0075, 0.031), deform=boxy(6.0)), ICE,
          "body", surface="shell")


def crown(k):
    # the spire: tall, a little back and leaning back, broken at a third of its height into a narrower point
    k.add("spire", crystal((0.006, 0.012, 0.12), (0.016, 0.03, 0.275), [(0, 0.064), (0.35, 0.05), (1.0, 0)],
                           n=5, turn=18, flat=0.85), ICE2, "body", fuse=False, surface="shell")
    # the ear: a broad flat triangle of crystal on the left, in front of the spire, its face to the front and out
    ear = [(-0.012, -0.05, 0.13), (-0.092, -0.036, 0.118), (-0.07, 0.0, 0.13), (-0.03, -0.012, 0.13),
           (-0.104, -0.056, 0.218), (-0.096, -0.044, 0.222)]
    k.add("ear", hull(ear), ICE3, "body", fuse=False, surface="shell")
    # two small points on the right flank
    k.add("flank1", crystal((0.1, 0.015, 0.098), (0.148, 0.03, 0.112), [(0, 0.017), (1, 0)], n=4, turn=45),
          ICE3, "body", fuse=False, surface="shell")
    k.add("flank2", crystal((0.085, 0.05, 0.128), (0.122, 0.07, 0.16), [(0, 0.015), (1, 0)], n=4, turn=45),
          ICE3, "body", fuse=False, surface="shell")


def lip(k):
    """The white lip of ice under the body, jutting forward under the face, its top a zigzag of blue points."""
    ring = []
    for i in range(14):
        th = 2 * math.pi * i / 14
        ring.append((0.106 * math.sin(th), -0.106 * math.cos(th) if math.cos(th) > 0 else -0.088 * math.cos(th),
                     0.044))
        ring.append((0.084 * math.sin(th), -0.08 * math.cos(th) if math.cos(th) > 0 else -0.07 * math.cos(th),
                     0.011))
    k.add("lip", hull(ring + [(0, 0, 0.008)]), WHITE, "body", fuse=False, surface="shell")
    # the blue points hanging over the lip's top at the front
    for i, x in enumerate((-0.072, -0.024, 0.024, 0.072)):
        y = FRONT_Y - 0.016 + 0.008 * (abs(x) > 0.05)
        k.add(f"point{i}", hull([(x - 0.019, y + 0.012, 0.047), (x + 0.019, y + 0.012, 0.047),
                                 (x - 0.019, y - 0.002, 0.044), (x + 0.019, y - 0.002, 0.044),
                                 (x, y - 0.004, 0.034), (x, y + 0.012, 0.034)]),
              ICE3, "body", fuse=False, surface="shell")
    # icicles under the lip, pointing down and out
    for i in range(7):
        a = math.radians(-75 + 150 * i / 6)
        root = np.array([0.07 * math.sin(a), -0.066 * math.cos(a), 0.02])
        tip = root + np.array([0.012 * math.sin(a), -0.012 * math.cos(a), -0.02])
        k.add(f"icicle{i}", crystal(root, tip, [(0, 0.011), (1, 0)], n=4, turn=45), ICICLE, "body", fuse=False,
              surface="shell")


# --- the face -----------------------------------------------------------------------------------------------------------
PUP_V, PUP_HALF = 0.06, (0.21, 0.31)      # the pupil, in the eye's own u, v


def eye_open(pupil=True):
    lil, iris, iris2, pup = srgb(LILAC), srgb(IRIS), srgb(IRIS2), srgb(PUPIL)
    def look(u, v):
        r2 = u * u + v * v
        c = np.tile(iris, (len(u), 1))
        c[(v < -0.25) & (r2 < 0.75 ** 2)] = iris2                              # the iris lighter low down
        if pupil:
            c[(u / PUP_HALF[0]) ** 2 + ((v - PUP_V) / PUP_HALF[1]) ** 2 < 1] = pup
        c[r2 > 0.73 ** 2] = lil
        return c
    return look


def eyes(k, state):
    lid, lil = srgb(ICE), srgb(LILAC)
    inner = eye_look(state, eye_open(pupil=state != "Open"), lid, srgb(INK), line=0.16)
    def look(u, v):
        c = inner(u, v)
        c[u * u + v * v > 0.73 ** 2] = lil                 # the lilac ring is the design's: it stays when the eye shuts
        return c
    k.decal((EYE_X, FACE_Y, EYE_Z), (0, -1.0, 0), (EYE_R, EYE_R), look, eps=0.0022, rings=20, segs=56, mirror=True)
    if state == "Open":
        # the pupil and its glint, patches of their own so their edges are round
        k.decal((EYE_X, FACE_Y, EYE_Z + PUP_V * EYE_R), (0, -1.0, 0), (PUP_HALF[0] * EYE_R, PUP_HALF[1] * EYE_R),
                PUPIL, eps=0.0028, rings=6, segs=32, mirror=True)
        k.decal((EYE_X - 0.0055, FACE_Y, EYE_Z + 0.0085), (0, -1.0, 0), (0.0038, 0.0038), "#FFFFFF", eps=0.0034,
                rings=4, segs=24, mirror=True)


def mouth(k):
    k.decal((0, FACE_Y, MOUTH_Z), (0, -1.0, 0), (0.027, 0.0032), INK, eps=0.0014, rings=4, segs=40)


def bergmite(src, out):
    k = Kit(src, coat="shell")
    body(k)
    crown(k)
    lip(k)
    mouth(k)
    k.build(voxel=VOXEL, band=0.003, wband=0.02)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    bergmite(*args())
