# Decidueye sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/decidueye.py -- <the export's Decidueye.glb> <out.glb>
# Rowlet and Dartrix grown (rowlet.py's frame helpers and feet): from the references, a tall archer owl. Its head is a
# hood of jagged teal-green feathers, a fringe hanging over the brow, its face a dark navy mask with two orange-red
# eyes joined by an orange band like goggles, a small dark beak; a curled white tuft on its crown with an orange-red
# notch at its root; the hood falls into a jagged green capelet over the shoulders, open at the front on a cream
# throat; at the top of the chest an orange-red cross of four petals, a green vine running from it over the left
# shoulder and down the right side, a leaf at each end; a slim cream body on stout cream thighs and thinner shins,
# cream toes with dark talons; the wings folded round it as a tan-brown cloak down to the ankles, orange-red triangles
# along its top, cream dots at its front edge and cream feather tips at the hem; long green tail feathers behind.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, leaf, sheet, hull, outward, srgb, inside, tri, unit, eye_look, args, \
    sibling

r = sibling("rowlet")
p = r.p

HOOD, HOODDARK, MASK, CREAM = "#21A275", "#14775A", "#1F2A3D", "#EFE6CB"
CLOAK, CLOAKTIP, CORAL, CREST = "#C58D59", "#EBDDBE", "#EE5A30", "#F1ECDC"
VINE, VLEAF, CLAW, BEAKC = "#2C7A4C", "#3C9B5E", "#22304A", "#2A3346"


def frame():
    F = SimpleNamespace()
    F.H = np.array([0.0, -0.016, 0.53])             # the head's centre, over the head's joint (0, -0.01, 0.49)
    F.HS = 1.22                                     # the head a fifth larger than measured, as on a tall figure
    F.HR = np.array([0.078, 0.074, 0.075]) * F.HS
    F.FC = F.H + (np.array([0.0, -0.05, 0.518]) - F.H) * F.HS     # the flatter face of the mask, for its patches
    F.FR = np.array([0.06, 0.042, 0.05]) * F.HS
    F.CHEST = (np.array([0.0, -0.006, 0.415]), np.array([0.066, 0.06, 0.055]))
    F.LEGX, F.LEGY, F.LEGTOP = 0.047, -0.004, 0.07
    F.LEGR = (0.017, 0.015)
    F.TOE, F.TOER = 0.046, 0.0085
    F.EYEX, F.EYEZ = 0.027 * F.HS, F.H[2] - 0.006 * F.HS
    F.EYE = (0.02 * F.HS, 0.0175 * F.HS)
    F.VOXEL = 0.0055
    return F


def S(F, q):
    """A point measured on the head as first drawn, moved out from the head's centre as the head was made larger."""
    return F.H + (np.array(q, float) - F.H) * F.HS


def head_poly(half):
    """A mask's outline (round the head's axis, z) as first drawn, grown with the head; mirrored."""
    F = frame()
    return [(a * F.HS, F.H[2] + (z - F.H[2]) * F.HS) for a, z in p.mirrored(half)]


def body(k, F):
    k.add("head", ell(F.H, F.HR), HOOD, "head")
    k.add("face", ell(F.FC, F.FR), HOOD, "head")
    k.add("cape", ell((0.0, 0.01, 0.448), (0.1, 0.082, 0.06)), HOOD, "body")
    k.add("chest", ell(*F.CHEST), CREAM, "body")
    k.add("torso", ell((0.0, 0.004, 0.345), (0.066, 0.058, 0.095)), CREAM, "body")
    # stout thighs fused into the hips, the shins tapering to the ankles
    k.add("thigh", ell((0.044, 0.002, 0.245), (0.047, 0.052, 0.078)), CREAM, "leg{s}", mirror=True)
    k.add("shin", tube([(0.046, 0.0, 0.2), (0.047, -0.002, 0.12), (0.048, -0.004, 0.065)], [0.026, 0.02, 0.017]),
          CREAM, "leg{s}", mirror=True)
    # the dark mask, round the head's axis, its top cut by the fringe
    rc = r.round_coords(F.H[1], 0.078 * F.HS)
    poly = head_poly([(0.0, 0.56), (0.016, 0.552), (0.03, 0.561), (0.046, 0.553), (0.06, 0.538), (0.064, 0.508),
                       (0.05, 0.476), (0.028, 0.46), (0.0, 0.452)])
    k.paint(lambda P: p.poly_mask(*rc(P), poly, edge=0.002) * (P[:, 1] < F.H[1] - 0.02), MASK, on=["face", "head"])
    # the capelet open at the front on the cream throat, a V widening down
    k.paint(lambda P: inside(np.abs(P[:, 0]) - (0.012 + 0.55 * (0.47 - P[:, 2])), 0.002) * (P[:, 2] < 0.47)
            * (P[:, 1] < -0.02), CREAM, on=["cape"])


def jagged(k, F):
    """The hood's feathers: a fringe over the brow, a ring round the head's rim and the capelet's hem, all hanging."""
    H, HR = F.H, F.HR
    for x, ln in ((-0.05, 0.034), (-0.026, 0.04), (0.0, 0.032), (0.026, 0.042), (0.05, 0.034)):
        base = S(F, (x, -0.066, 0.584))
        tip = base + np.array([x * 0.25, -0.036, -ln]) * F.HS
        k.add("fringe", p.spike(base, tip, 0.03, 0.018, side=(1, 0, 0), at=0.3), HOOD, "head", fuse=False,
              surface="fur")
    for a in range(55, 310, 25):
        t = math.radians(a)
        rad = np.array([math.sin(t), -math.cos(t), 0])
        base = H + (rad * np.array([0.074, 0.07, 0]) + np.array([0, 0, -0.035])) * F.HS
        tip = base + (rad * 0.024 + np.array([0, 0, -0.068])) * F.HS
        k.add("rim", p.spike(base, tip, 0.04, 0.02, side=(math.cos(t), math.sin(t), 0), at=0.3),
              HOOD if a % 50 else HOODDARK, "head", fuse=False, surface="fur")
    # higher up the back of the hood, a second row laid down over the first
    for a in range(105, 260, 30):
        t = math.radians(a)
        rad = np.array([math.sin(t), -math.cos(t), 0])
        base = H + (rad * np.array([0.07, 0.068, 0]) + np.array([0, 0, 0.03])) * F.HS
        tip = base + (rad * 0.028 + np.array([0, 0, -0.07])) * F.HS
        k.add("rim", p.spike(base, tip, 0.042, 0.02, side=(math.cos(t), math.sin(t), 0), at=0.3),
              HOODDARK if a % 60 else HOOD, "head", fuse=False, surface="fur")
    for a in range(40, 330, 24):
        t = math.radians(a)
        rad = np.array([math.sin(t), -math.cos(t), 0])
        base = np.array([0, 0.01, 0.43]) + rad * np.array([0.096, 0.078, 0])
        tip = base + rad * 0.022 + np.array([0, 0, -0.07])
        k.add("hem", p.spike(base, tip, 0.045, 0.02, side=(math.cos(t), math.sin(t), 0), at=0.3),
              HOOD if a % 48 else HOODDARK, "body", fuse=False, surface="fur")


def crest(k, F):
    """The curled white tuft on the crown, an orange-red notch at its root."""
    for pts, rad in (([(0.0, -0.01, 0.59), (0.004, -0.004, 0.632), (-0.012, 0.006, 0.658), (-0.034, 0.016, 0.648)],
                      [0.013, 0.013, 0.01, 0.003]),
                     ([(0.006, 0.004, 0.592), (0.016, 0.012, 0.614), (0.028, 0.022, 0.618)], [0.01, 0.008, 0.003])):
        k.add("crest", blade([S(F, q) for q in pts], np.array(rad) * F.HS, (0, 1, 0), 0.55), CREST, "head", fuse=False, surface="fur")
    b = S(F, (0.0, -0.05, 0.594))
    k.add("notch", hull([b + (-0.012, 0, 0.008), b + (0.012, 0, 0.008), b + (0, -0.004, -0.014), b + (0, 0.014, 0.0)]),
          CORAL, "head", fuse=False)


def beak(k, F):
    P, n = r.on_ell(F.FC, F.FR, 0.0, F.H[2] - 0.022 * F.HS)
    k.add("beak", p.spike(P - n * 0.002 + np.array([0, 0, 0.006]), P + n * 0.012 + np.array([0, 0, -0.016]), 0.02,
                          0.014, side=(1, 0, 0), at=0.3), BEAKC, "head", fuse=False, surface="shell")


def cloak(k, F):
    """The wings folded round as a cloak from the shoulders to the ankles: a curved plate a side, cream at the hem."""
    th0, th1 = math.radians(42), math.radians(170)
    for s, side in ((1, "R"), (-1, "L")):
        b = f"wing{side}"
        def at(u, v, grow=0.0, s=s):
            th = th0 + (th1 - th0) * v
            R = 0.074 + 0.038 * u + 0.012 * np.sin(math.pi * v) + grow
            z = 0.47 - 0.42 * u - u * 0.018 * (1 + tri(v * 4.5)) / 2
            return np.stack([s * R * np.sin(th), 0.006 - R * np.cos(th), z], -1)
        k.add(f"cloak{side}", outward(sheet(at, 40, 24, 0.016)), CLOAK, b, fuse=False, surface="fur")
        k.add(f"cloaktip{side}", outward(sheet(lambda u, v: at(0.86 + 0.14 * u, v, 0.006), 8, 24, 0.014)), CLOAKTIP,
              b, fuse=False, surface="fur")
        # orange-red triangles in a row under the capelet's hem, pointing down
        for v in (0.12, 0.3, 0.48, 0.66):
            c = at(np.array(0.24), np.array(v), 0.009)
            n = c - np.array([0, 0.006, c[2]]); n /= np.linalg.norm(n)
            tang = np.cross((0, 0, 1.0), n) * s
            k.add(f"tri{side}", hull([c + tang * 0.016 + (0, 0, 0.01), c - tang * 0.016 + (0, 0, 0.01),
                                      c + (0, 0, -0.022), c + n * 0.006 + (0, 0, 0.004)]), CORAL, b, fuse=False)
        # cream dots down the front edge
        for u in (0.34, 0.46, 0.58):
            c = at(np.array(u), np.array(0.06), 0.009)
            n = c - np.array([0, 0.006, c[2]]); n /= np.linalg.norm(n)
            k.add(f"dot{side}", r.ell_frame(c, (0.008, 0.004, 0.009), np.cross((0, 0, 1.0), n), n), CLOAKTIP, b,
                  fuse=False)


def cross(k, F):
    """The orange-red cross of four petals at the top of the chest, and the vine from it with a leaf at each end."""
    c, rr = F.CHEST
    C, n = r.on_ell(c, rr, 0.0, 0.43)
    for dx, dz in ((1, 1), (-1, 1), (1, -1), (-1, -1)):
        d = unit(dx, 0, dz)
        d = d - n * (d @ n); d /= np.linalg.norm(d)
        k.add("petal", r.ell_frame(C + n * 0.007 + d * 0.019, (0.021, 0.0075, 0.011), d, n), CORAL, "body",
              fuse=False, surface="leaf")
    k.add("knot", r.ell_frame(C + n * 0.01, (0.008, 0.006, 0.008), (1, 0, 0), n), "#C9431F", "body", fuse=False)
    # over the left shoulder (the viewer's left), and down the right side over the cloak
    for pts, d in (([C + n * 0.008, (-0.045, -0.078, 0.434), (-0.08, -0.08, 0.42), (-0.098, -0.08, 0.392)],
                    (-0.35, -0.3, -1)),
                   ([C + n * 0.008, (0.05, -0.082, 0.4), (0.1, -0.1, 0.33), (0.118, -0.104, 0.26)], (0.15, -0.3, -1))):
        k.add("vine", tube(pts, [0.009, 0.009, 0.0085, 0.008]), VINE, "body", fuse=False, surface="leaf")
        k.add("vleaf", outward(leaf(pts[-1], d, 0.07, 0.026, thick=0.013)), VLEAF, "body", fuse=False, surface="leaf")


def tail(k):
    for s in (1, 0, -1):
        base = np.array([0.016 * s, 0.07, 0.32])
        d = unit(0.08 * s, 0.32, -1)
        k.add("tail", outward(leaf(base, d, 0.3 - 0.03 * abs(s), 0.032, thick=0.014)),
              HOOD if s == 0 else HOODDARK, "body", fuse=False, surface="leaf")


# --- the eyes: orange-red under the fringe, a dark pupil, joined by an orange band -----------------------------------
INK, ORANGE, AMBER, LIDC = srgb("#1A1E2A"), srgb("#EE5A30"), srgb("#F59A4A"), srgb(MASK)


def eye_open(u, v):
    rr = np.sqrt(u * u + v * v)
    c = np.tile(ORANGE, (len(u), 1))
    c[rr < 0.62] = AMBER
    c[(u + 0.08) ** 2 + (v - 0.05) ** 2 < 0.3 ** 2] = INK
    c[rr > 0.86] = INK
    c[(u - 0.06) ** 2 + (v - 0.28) ** 2 < 0.1 ** 2] = srgb("#FFFFFF")
    return c


def stern(look):
    """The fringe cuts the eye's top, lower toward the beak: a stern look in every open state."""
    def f(u, v):
        c = look(u, v)
        c[v > 0.42 + 0.3 * u] = LIDC
        return c
    return f


def eyes(k, F):
    centre, normal = r.on_ell(F.FC, F.FR, F.EYEX, F.EYEZ)
    normal = unit(*(normal + np.array([0.1, 0, 0])))
    def lay(state):
        look = eye_look(state, eye_open, LIDC, srgb("#E8854A"), line=0.16)
        if state in ("Open", "Fierce"):
            look = stern(look)
        k.decal(centre, normal, F.EYE, look, eps=0.0025, rings=20, segs=56, mirror=True)
    return lay


def decidueye(src, out):
    F = frame()
    k = Kit(src, coat="fur")
    body(k, F)
    jagged(k, F)
    crest(k, F)
    beak(k, F)
    cloak(k, F)
    cross(k, F)
    tail(k)
    r.feet(k, F, colour=CREAM, claw=CLAW)
    # the orange band between the eyes, over the beak's root (in every state)
    P, n = r.on_ell(F.FC, F.FR, 0.0, 0.53)
    k.decal(P, n, (0.016, 0.0055), "#EE5A30", eps=0.002, rings=6, segs=32)
    k.build(voxel=F.VOXEL, band=0.006, wband=0.05)
    k.eye_states(eyes(k, F))
    k.export(out)


if __name__ == "__main__":
    decidueye(*args())
