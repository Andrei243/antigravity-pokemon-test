# Scatterbug sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/scatterbug.py -- <the export's Scatterbug.glb> <out.glb>
#
# Written to be grown from (spewpa.py and vivillon.py import it with sibling("scatterbug")): the line's grey, its tan
# eye with the black upright pupil and the way eyes are laid are here; the calls that build Scatterbug are under
# __main__.
#
# From the references: a great round grey head, a little wider than tall, a dark seam running from the back over the
# crown and down the middle of the brow; two big upright tan ovals of eyes set wide on the face, each with a black
# upright oblong of a pupil toward the nose; one small white fang pointing down under the middle of the face; two thin
# white feelers springing together from the crown and curving up and out. Under the head a white ruff of points with
# black square flecks, and behind it a short stout grey caterpillar of four segments lying back along the ground, dark
# seams between them, tan spots on their sides, small tan spikes under them and a tan saddle on the last segment.
# Bones: root, body (0, 0, 0.05), seg1 (0, 0.02, 0.045), seg2 (0, 0.055, 0.04), seg3 (0, 0.08, 0.042), head
# (0, -0.04, 0.08).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, outward, srgb, inside, smoothstep, unit, eye_look, args, polygon

GREY, SEAM, TAN, PUPIL, WHITE, FLECK = "#5C5C63", "#2C2C31", "#D9C69C", "#121214", "#F4F4F2", "#1C1C20"

HC = np.array([0.0, -0.055, 0.12])           # the head's middle
HR = np.array([0.068, 0.06, 0.06])

# the body, lying back along the ground from under the head: (bone, middle, radius)
SEGS = [("body", (0, -0.012, 0.036), 0.031), ("seg1", (0, 0.024, 0.031), 0.03), ("seg2", (0, 0.057, 0.029), 0.028),
        ("seg3", (0, 0.087, 0.031), 0.026), ("seg3", (0, 0.111, 0.04), 0.021)]


def on_head(d, c=HC, r=HR):
    """The point of the head's ellipsoid in direction d, and its outward normal there."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def head(k):
    k.add("skull", ell(HC, HR, sub=5), GREY, "head")
    # the seam: from the back over the crown and down the middle of the brow to above the eyes
    def seam(P):
        q = np.sqrt((((P - HC) / HR) ** 2).sum(1))
        return (inside(np.abs(P[:, 0]) - 0.0022, 0.0012) * inside(np.abs(q - 1) - 0.12, 0.01)
                * inside(P[:, 1] - (HC[1] + 0.045), 0.003) * inside((HC[2] + 0.012) - P[:, 2], 0.003))
    k.paint(seam, SEAM, on=["skull"])
    # one small white fang pointing down under the middle of the face
    k.add("fang", hull([(-0.0068, -0.1, 0.093), (0.0068, -0.1, 0.093), (-0.0068, -0.108, 0.093),
                        (0.0068, -0.108, 0.093), (0, -0.1, 0.077), (0, -0.106, 0.077)]),
          WHITE, "head", fuse=False, surface="shell")


def feelers(k, length=1.0, color=WHITE):
    pts = np.array([(0.0, -0.05, 0.168), (0.01, -0.046, 0.192), (0.025, -0.04, 0.214), (0.044, -0.034, 0.229)])
    pts = pts[0] + (pts - pts[0]) * length
    k.add("feeler", outward(tube(pts, [0.0048, 0.0046, 0.0044, 0.0042])), color, "head", fuse=False, mirror=True)


def ruff(k):
    """The white ruff of points under the head, black square flecks on it."""
    c = np.array([0.0, -0.04, 0.058])
    k.add("ruff", ell(c, (0.046, 0.042, 0.017)), WHITE, "body")
    for i in range(9):
        a = math.radians(-90 + i * 40)           # round from straight ahead
        d = np.array([math.cos(a), math.sin(a), 0.0])
        s = np.array([-d[1], d[0], 0.0])
        base = c + d * 0.036
        tip = c + d * 0.067 + np.array([0, 0, -0.012])
        k.add(f"point{i}", hull([base + s * 0.011 + (0, 0, 0.007), base - s * 0.011 + (0, 0, 0.007),
                                 base + s * 0.011 - (0, 0, 0.006), base - s * 0.011 - (0, 0, 0.006), tip]),
              WHITE, "body", fuse=False, surface="fur")
    for i, a in enumerate((-55, 55, -140, 140)):
        a = math.radians(-90 + a)
        d = np.array([math.cos(a), math.sin(a), 0.0])
        s = np.array([-d[1], d[0], 0.0])
        m = c + d * 0.042 + np.array([0, 0, 0.012])
        h = 0.0065
        k.add(f"fleck{i}", hull([m + sx * s * h + sd * d * h + (0, 0, sz * h) for sx in (-1, 1) for sd in (-1, 1)
                                 for sz in (-1, 1)]), FLECK, "body", fuse=False, surface="shell")


def body(k):
    for i, (bone, c, r) in enumerate(SEGS):
        k.add(f"seg{i}", ell(c, (r * 1.03, r, r)), GREY, bone)
    cuts = [(SEGS[i][1][1] + SEGS[i + 1][1][1]) / 2 for i in range(len(SEGS) - 1)]
    def seams(P):
        m = np.zeros(len(P))
        for y in cuts[1:]:
            m = np.maximum(m, inside(np.abs(P[:, 1] - y) - 0.0018, 0.0012))
        return m * inside(P[:, 2] - 0.07, 0.003)
    k.paint(seams, SEAM, on=["seg"])
    # the tan saddle on the last segment
    end = np.array(SEGS[-1][1])
    k.paint(lambda P: inside(np.linalg.norm(P - (end + (0, 0.006, 0.016)), axis=1) - 0.017, 0.0015), TAN, on=["seg4"])
    # tan spots on the segments' sides
    for (bone, c, r), up in ((SEGS[1], 0.55), (SEGS[2], 0.2), (SEGS[3], 0.6)):
        n = unit(0.85, 0.0, up)
        k.decal(np.array(c) + n * r, n, (0.0085, 0.0085), srgb(TAN), eps=0.0015, rings=10, segs=36, mirror=True)
    # small tan spikes under the segments, pointing down and out
    for i, (bone, c, r) in enumerate(SEGS[1:4]):
        c = np.array(c)
        b = c + np.array([0.62 * r, 0, -0.55 * r])
        tip = b + np.array([0.011, 0.002, -0.008])
        k.add(f"spike{i}", hull([b + (0, sy, sz) for sy in (-0.005, 0.005) for sz in (-0.004, 0.004)] + [tip]),
              TAN, bone, fuse=False, mirror=True, surface="shell")


# --- the eyes: upright tan ovals, a black upright oblong of a pupil toward the nose -----------------------------------
EYE_DIR = (0.6, -0.76, -0.12)
EYE_HALF = (0.02, 0.026)


def eye_open(u, v, tan=srgb(TAN)):
    return np.tile(tan, (len(u), 1))


# the pupil, a patch of its own over the eye (crisp edges): its middle and half size in the eye's half sizes
PUPIL_AT = (-0.14, -0.02, 0.3, 0.52)
BOX = polygon([(-1, -1), (1, -1), (1, 1), (-1, 1)])
BOX_FIERCE = polygon([(-1, -1), (1, -1), (1, 0.15), (-1, 0.15)])


def eyes(k, centre=HC, radii=HR, d=EYE_DIR, half=EYE_HALF, look=eye_open, lid=GREY, ink=TAN, pupil=PUPIL_AT,
         pupil_look=None, shapes=(BOX, BOX_FIERCE)):
    p, n = on_head(d, centre, radii)
    # the eye's own u (toward +X along the surface) and v (up)
    eu = np.array([1.0, 0, 0]) - n * n[0]; eu /= np.linalg.norm(eu)
    ev = np.cross(n, eu)
    if ev[2] < 0:
        ev = -ev
    du, dv, hu, hv = pupil
    pp, pn = on_head(p + eu * du * half[0] + ev * dv * half[1] - centre, centre, radii)
    pcol = pupil_look or srgb(PUPIL)
    def lay(state):
        k.decal(p, n, half, eye_look(state, look, srgb(lid), srgb(ink), line=0.16), eps=0.002, rings=20, segs=56,
                mirror=True)
        if state in ("Open", "Fierce"):
            k.decal(pp, pn, (hu * half[0], hv * half[1]), pcol, eps=0.0032, rings=16, segs=64, mirror=True,
                    outline=shapes[0] if state == "Open" else shapes[1])
    return lay


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    head(k)
    feelers(k)
    ruff(k)
    body(k)
    k.build(voxel=0.0024, smooth=3, band=0.004, wband=0.02)
    k.eye_states(eyes(k))
    k.export(out)
