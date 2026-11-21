# Ribombee sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ribombee.py -- <the export's Ribombee.glb> <out.glb>
#
# Grown from Cutiefly's script (sibling("cutiefly")): the same yellow, the same rounded-square eye with its square
# glint and brown square, the same pale wing with tan marks stacked on it, the same thin black legs. From the
# references: a small upright bee fly; a round yellow head, the largest mass, its lower face white in a mask that rises
# in a point between the eyes; big rounded-square eyes, black-rimmed, brown inside with a white square glint; a small
# smile; two black leaf-shaped antennae standing up in a V; a brown fluffy poncho over the shoulders hanging in broad
# points, a yellow bib under the chin in front, a small white belly; short black pointed arms out from under the
# poncho, carried forward and down; long thin black legs with pointed feet; one pair of broad pale wings raised behind,
# each with a tan four-lobed mark like a paw print.
# (The task's "scarf-like collar" is the pictures' poncho of broad points; no proboscis, as the pictures show. The
# yellow curls by the eyes are left out: they are wire at the game's size.)
# Skeleton: body (0, 0, 0.179), head (0, -0.005, 0.219), arms (+-0.02, -0.01, 0.189), wings (+-0.025, 0.02, 0.219),
# legs (+-0.012, 0, 0.129).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, unit, inside, args, outward, sibling

cf = sibling("cutiefly")

YELLOW, DARK, WING, LOOP = cf.YELLOW, cf.DARK, cf.WING, cf.LOOP
FACE = "#F6F4EC"
CAPE, CAPE_DARK = "#8E6644", "#7A5638"
BELLY = "#F4F2EA"
MOUTH = srgb("#4A2A22")
EYE_TAN = srgb("#B08560")

# --- Ribombee's frame ------------------------------------------------------------------------------------------------
HC = np.array([0.0, -0.012, 0.27])
HR = np.array([0.064, 0.058, 0.058])
EYE_D = (0.5, -0.85, -0.12)
EYE_HALF = (0.024, 0.026)
CAPE_C = np.array([0.0, 0.004, 0.205])
WING_ROOT = np.array([0.025, 0.024, 0.222])


def face_mask(P):
    """The white lower face, rising in a point between the eyes, kept to the head's front."""
    rel = P - HC
    ang = np.arctan2(rel[:, 0], -rel[:, 1])
    top = -0.006 + 0.03 * np.exp(-(rel[:, 0] / 0.012) ** 2) - 0.012 * np.clip(np.abs(rel[:, 0]) - 0.03, 0, None) / 0.03
    return inside(rel[:, 2] - top, 0.002) * inside(np.abs(ang) - 1.25, 0.03) * (P[:, 2] > 0.214)


def smile(k):
    p, nrm = cf.on_ell(HC, HR, (0, -1, -0.5))
    f = srgb(FACE)
    def look(u, v):
        col = np.tile(f, (len(u), 1))
        curve = -0.25 + 0.75 * (1 - u * u)
        col[(np.abs(u) < 0.86) & (np.abs(v + curve - 0.25) < 0.34)] = MOUTH
        return col
    k.decal(p, nrm, (0.009, 0.005), look, eps=0.0015, rings=10, segs=40)


def antenna(k):
    """A black leaf standing up from the crown, out a little: a solid with a kite section."""
    for sx in (1, -1):
        root, _ = cf.on_ell(HC, HR, (0.3 * sx, 0.05, 0.95))
        root = root - (0, 0, 0.006)
        d = unit(0.28 * sx, 0.12, 1.0)
        side = unit(1, 0, -0.28 * sx)
        face = np.cross(d, side)
        pts = []
        for t, w in ((0.0, 0.006), (0.25, 0.009), (0.55, 0.0085), (0.85, 0.005)):
            c = root + d * 0.07 * t
            pts += [c + side * w, c - side * w, c + face * 0.005, c - face * 0.005]
        pts.append(root + d * 0.075)
        k.add("antenna" + ("R" if sx > 0 else "L"), hull(pts), DARK, "head", fuse=False)


def cape(k):
    """The brown poncho: a fused round of shoulders and broad loose points hanging down and out."""
    k.add("shoulders", ell(CAPE_C, (0.05, 0.046, 0.026)), CAPE, "body")
    for i, ang in enumerate((-60, -25, 25, 60, 100, 140, 180, 220, 260)):
        a = math.radians(ang)
        rad = np.array([math.sin(a), -math.cos(a), 0.0])      # 0 is straight ahead
        tang = np.array([math.cos(a), math.sin(a), 0.0])
        top = CAPE_C + rad * 0.03 + (0, 0, 0.012)
        tip = CAPE_C + rad * 0.074 + (0, 0, -0.05)
        mid = CAPE_C + rad * 0.052 + (0, 0, -0.012)
        pts = [top + tang * 0.03, top - tang * 0.03, mid + tang * 0.032, mid - tang * 0.032,
               top - rad * 0.008, mid - rad * 0.01, tip, tip - rad * 0.006 + (0, 0, 0.006)]
        k.add(f"capept{i}", hull(pts), CAPE if i % 2 else CAPE_DARK, "body", fuse=False)


def ribombee(src, out):
    k = Kit(src, coat="fur")
    # The head and its white lower face
    k.add("head", ell(HC, HR), YELLOW, "head")
    k.paint(face_mask, FACE, on=["head"])
    smile(k)
    antenna(k)
    # The body: a yellow bib under the chin, a small white belly, the brown poncho over the shoulders
    k.add("bib", ell((0.0, -0.03, 0.2), (0.026, 0.016, 0.024)), YELLOW, "body")
    k.add("bibpt", hull([(-0.018, -0.03, 0.2), (0.018, -0.03, 0.2), (0, -0.044, 0.198), (0, -0.02, 0.2),
                         (0.0, -0.04, 0.168), (-0.01, -0.03, 0.18), (0.01, -0.03, 0.18)]), YELLOW, "body", fuse=False)
    k.add("belly", ell((0.0, -0.004, 0.158), (0.026, 0.024, 0.03)), BELLY, "body")
    cape(k)
    # Short black pointed arms out from under the poncho, forward and down
    for sx in (1, -1):
        k.add("arm" + ("R" if sx > 0 else "L"),
              outward(tube([(0.026 * sx, -0.022, 0.182), (0.04 * sx, -0.04, 0.162), (0.042 * sx, -0.052, 0.146)],
                           [0.0075, 0.0068, 0.004])),
              DARK, "armR" if sx > 0 else "armL", fuse=False)
    # Long thin black legs, pointed feet
    for sx in (1, -1):
        s = "R" if sx > 0 else "L"
        cf.leg(k, "leg" + s, [(0.013 * sx, 0.0, 0.14), (0.017 * sx, -0.002, 0.1), (0.02 * sx, 0.006, 0.064)], 0.0068,
               bone="leg" + s, mirror=False)
        k.add("foot" + s, outward(ell((0.021 * sx, -0.004, 0.058), (0.008, 0.02, 0.008), rot=(15, 0, 0), sub=3)),
              DARK, "leg" + s, fuse=False)
    # One pair of broad pale wings, a tan four-lobed mark on each
    L, W = 0.15, 0.062
    def lobes(r, centres, n=60):
        pts = []
        cx = np.mean([c[0] for c in centres]); cy = np.mean([c[1] for c in centres])
        for i in range(n):
            t = 2 * math.pi * i / n
            d = np.array([math.cos(t), math.sin(t)])
            best = 0.0
            for c in centres:
                o = np.array(c) - (cx, cy)
                b = o @ d; q = b * b - (o @ o - r * r)
                if q > 0:
                    best = max(best, b + math.sqrt(q))
            pts.append((cx + d[0] * best, cy + d[1] * best))
        return pts
    centres = [(0.055, 0.0), (0.085, 0.022), (0.092, -0.012), (0.075, -0.03)]
    marks = [(lobes(0.019, centres), LOOP)]
    for j, c in enumerate(centres):
        marks.append(([(a, b + c[1]) for a, b in cf.oval(c[0] - 0.0095, c[0] + 0.0095, 0.0095)], WING))
    cf.wing(k, WING_ROOT, (0.55, 0.38, 0.74), (0.42, -0.9, -0.1), L, W, marks)
    k.build(voxel=0.0034, smooth=4, band=0.004, wband=0.03)

    def eyes(state):
        cf.lay_eyes(k, HC, HR, EYE_D, EYE_HALF, cf.eye_open(low=EYE_TAN, rim=cf.EYE_DARK, rim_w=0.0), FACE, state)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    ribombee(*args())
