# Venusaur-Mega sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/venusaurmega.py -- <the export's Venusaur-Mega.glb> <out.glb>
#
# Grown from venusaur.py (sibling: its body, legs, head, ears, mouth, eyes, petal and the line's fern). From the two
# pictures, what the Mega changes, largest mass first: the fern leaves grown huge, two broad ones a side rising up and
# out from the trunk's foot past the body's width; the flower raised on a taller trunk, its five petals broader and
# flatter with a frilled rim and big white spots near their ends; a skirt of dark teal-green leaves hanging over the
# shoulders and hips; two dark green vines hanging from under the petals in arches down the trunk's front, curled at
# their ends; a pink five-petalled flower on the brow where Venusaur's leaf lay, a dark teal diamond under it on the
# brow; the body a paler, bluer teal.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, tri, unit, args, outward, sheet, sibling, polygon

v = sibling("venusaur")
b, iv = v.b, v.iv

TEAL, SPOT, INK, CLAW = "#64B6D2", "#3C84A4", "#1C2A2C", "#F2F0EA"
LINING = "#E06A7A"
PETAL, PSPOT = "#F2788E", "#FBEDF0"
TRUNK, ZIG, CROWN = "#9A7A34", "#5C4420", "#F2C63A"
LEAF, LEAF2, RIB = "#53A847", "#4A9A40", iv.RIB
DARKLEAF, DARKRIB = "#1F7A66", "#145446"
VINE = "#1E6A58"
BROWPETAL, BROWHEART = "#F27D92", "#E4607A"
DIAMOND = "#26707E"

H, HR, JAW = v.H, v.HR, v.JAW
TOP = np.array([0, 0.08, 0.98])                         # the trunk's top, a little higher than Venusaur's 0.92


def frilled_petal(k, name, base, d, length, width, droop, thick, bone, col, spots, spot_col):
    """Venusaur's petal made broader and flatter, its rounded end cut into a frill of three scallops."""
    base, d = np.array(base, float), unit(*d)
    side = np.cross(d, (0, 0, 1.0)); side /= np.linalg.norm(side)
    up = np.cross(side, d)

    def mid(uu):
        return base[None, None] + length * (uu[..., None] * d[None, None]
                                            - droop * uu[..., None] ** 2.2 * np.array([0, 0, 1.0])[None, None])

    def at(u, vv):
        reach = 1 - 0.075 * (0.5 + 0.5 * np.cos(6 * np.pi * vv)) * smoothstep(0.6, 1.0, u)
        uu = (0.015 + 0.97 * u) * reach
        w = width * np.clip(np.sin(np.pi * np.clip(0.015 + 0.97 * u, 0, 1) ** 0.55), 0, 1) ** 0.45 \
            * (0.35 + 0.65 * smoothstep(0, 0.35, uu))
        s = (vv - 0.5) * 2 * w
        cup = -0.02 * (2 * vv - 1) ** 2 * uu
        return mid(uu) + s[..., None] * side[None, None] + cup[..., None] * up[None, None]

    k.add(name, outward(sheet(at, 44, 25, thick)), col, bone, fuse=False, surface="leaf")
    for j, (su, sv, r) in enumerate(spots):
        p = at(np.array([[su]]), np.array([[sv]]))[0, 0]
        pu = at(np.array([[su + 0.02]]), np.array([[sv]]))[0, 0] - at(np.array([[su - 0.02]]), np.array([[sv]]))[0, 0]
        pv = at(np.array([[su]]), np.array([[sv + 0.02]]))[0, 0] - at(np.array([[su]]), np.array([[sv - 0.02]]))[0, 0]
        n = np.cross(pv, pu); n /= np.linalg.norm(n)
        if n[2] < 0:
            n = -n
        eu = pu / np.linalg.norm(pu); ev = np.cross(n, eu)
        V, F = ell((0, 0, 0), (r, r * 0.75, 0.006), sub=2)
        V = V @ np.stack([eu, ev, n], axis=1).T + p + n * (thick / 2 + 0.002)
        k.add(f"{name}spot{j}", outward((V, F)), spot_col, bone, fuse=False, surface="leaf")


def trunk(k):
    k.add("trunk", tube([(0, 0.08, 0.5), (0, 0.08, 0.72), (0, 0.08, 0.94)], [0.15, 0.125, 0.095]), TRUNK, "flower",
          surface="shell")
    def zigzags(P):
        a = np.arctan2(P[:, 0], -(P[:, 1] - 0.08))
        m = np.zeros(len(P))
        for z0 in (0.66, 0.735, 0.81, 0.885):
            m = np.maximum(m, inside(np.abs(P[:, 2] - (z0 + 0.014 * tri(a * 7 / (2 * math.pi)))) - 0.005, b.EDGE))
        return m * (P[:, 2] > 0.6) * (P[:, 2] < 0.95) * (np.hypot(P[:, 0], P[:, 1] - 0.08) < 0.17)
    k.paint(zigzags, ZIG)
    for i in range(8):
        a = 2 * math.pi * (i + 0.5) / 8
        o = np.array([math.sin(a), -math.cos(a), 0.0])
        t = np.cross(o, (0, 0, 1.0))
        c = TOP + o * 0.075
        pts = [c - t * 0.026, c + t * 0.026, c - o * 0.02, c + o * 0.012 - np.array([0, 0, 0.01]),
               c + o * 0.035 + np.array([0, 0, 0.07])]
        k.add(f"crown{i}", hull(pts), CROWN, "flower", fuse=False, surface="shell")
    k.add("heart", ell(TOP + (0, 0, 0.005), (0.08, 0.08, 0.03)), CROWN, "flower", fuse=False, surface="shell")


def flower(k):
    """Five broad flat petals with a frilled rim and three big white spots out toward their ends."""
    spots = [(0.62, 0.3, 0.042), (0.66, 0.72, 0.04), (0.84, 0.5, 0.046)]
    for i in range(5):
        a = 2 * math.pi * (i + 0.5) / 5
        d = (math.sin(a), -math.cos(a), 0.16)
        base = TOP + np.array([math.sin(a), -math.cos(a), 0]) * 0.05 - (0, 0, 0.02)
        frilled_petal(k, f"petal{i}", base, d, 0.44, 0.215, 0.22, 0.03, "flower", PETAL, spots, PSPOT)


def vines(k):
    """Two dark green vines from under the petals, arching down the trunk's front and curling at their ends."""
    for s in (1, -1):
        pts = [(0.05 * s, -0.0, 0.95), (0.11 * s, -0.09, 0.9), (0.1 * s, -0.135, 0.8), (0.04 * s, -0.14, 0.72),
               (0.0, -0.125, 0.66), (-0.03 * s, -0.12, 0.69), (-0.02 * s, -0.125, 0.725)]
        k.add(f"vine{'R' if s > 0 else 'L'}", tube(pts, [0.019, 0.019, 0.018, 0.017, 0.016, 0.015, 0.014]), VINE,
              "flower", fuse=False, surface="leaf")


def ferns(k):
    """The great leaves: two a side rising up and out from the trunk's foot, past the body's width."""
    for name, a, lift, length, width in (("fernf", 66, 0.55, 0.63, 0.23), ("fernb", 116, 0.5, 0.61, 0.22)):
        r = math.radians(a)
        d = (math.sin(r), -math.cos(r), lift)
        iv.fern(k, name, (0.07 * math.sin(r), 0.08 - 0.07 * math.cos(r), 0.62), d,
                (-0.25 * math.sin(r), 0.25 * math.cos(r), 1), length, width, "flower", col=LEAF, rib=RIB, droop=0.4,
                teeth=10, cut=0.5)
    iv.fern(k, "fernback", (0, 0.16, 0.62), (0, 1, 0.45), (0, -0.3, 1), 0.46, 0.16, "flower", col=LEAF2, rib=RIB,
            droop=0.5, teeth=9, cut=0.5, mirror=False)
    # The dark skirt: leaves hanging over the shoulders and the hips
    for name, y, dy, ln in (("darkfront", -0.2, -0.7, 0.36), ("darkhind", 0.24, 0.5, 0.34)):
        iv.fern(k, name, (0.14, y, 0.6), (1, dy, -0.55), (0.6, 0, 1), ln, 0.13, "body", col=DARKLEAF, rib=DARKRIB,
                droop=0.25, teeth=6, cut=0.55)


def brow_flower(k):
    """A pink five-petalled flower on the brow, where Venusaur's leaf lay."""
    n = unit(0, -0.5, 1)
    c = b.on_ell(H, HR, n)
    side = np.array([1.0, 0, 0]); fw = np.cross(n, side)
    for i in range(5):
        a = 2 * math.pi * i / 5
        o = math.sin(a) * side + math.cos(a) * fw
        p = c + o * 0.05 + n * 0.012
        e3 = n * 0.85 + o * 0.25; e3 /= np.linalg.norm(e3)
        e2 = np.cross(e3, o)
        V, F = ell((0, 0, 0), (0.058, 0.04, 0.014), sub=3)
        V = V @ np.stack([o, e2, e3], axis=1).T + p
        k.add(f"browpetal{i}", outward((V, F)), BROWPETAL, "head", fuse=False, surface="leaf")
    k.add("browheart", ell(c + n * 0.02, (0.03, 0.03, 0.02)), BROWHEART, "head", fuse=False, surface="leaf")


def venusaurmega(src, out):
    k = Kit(src, coat="scales")
    BC, BR = (0, 0.05, 0.37), (0.36, 0.42, 0.23)
    b.body(k, BC, BR, TEAL, chest=((0, -0.22, 0.38), (0.32, 0.2, 0.19)))
    b.legs(k, v.LEGS, TEAL, CLAW)
    b.underside(k, ((0, 0.05, 0.25), (0.34, 0.42, 0.18)), ((0, 0.28, 0.29), (0.34, 0.24, 0.22)), TEAL)
    b.head(k, H, HR, JAW, TEAL)
    ear0, ear1 = (0.17, -0.39, 0.55), (0.25, -0.41, 0.67)
    b.ears(k, ear0, ear1, 0.068, TEAL)
    b.ear_lining(k, ear0, ear1, 0.068, LINING)

    trunk(k)
    flower(k)
    vines(k)
    ferns(k)
    brow_flower(k)

    # The patches: the flanks, the legs, small warts
    for d, h in (((0.7, 0.62, 0.3), (0.05, 0.045)), ((0.8, -0.45, 0.3), (0.04, 0.035))):
        b.patch(k, b.on_ell(BC, BR, d), d, h, SPOT, b.PATCH, mirror=True)
    b.patch(k, (0.42, -0.2, 0.28), (1, -0.2, 0.2), (0.04, 0.048), SPOT, b.PATCH2, mirror=True, depth=0.05)
    b.patch(k, (0.42, 0.28, 0.29), (1, 0.1, 0.2), (0.04, 0.046), SPOT, b.PATCH, mirror=True, depth=0.05)

    # The dark teal diamond on the brow, under the flower and between the eyes
    dn = unit(0, -0.88, 0.47)
    k.decal(b.on_ell(H, HR, dn), dn, (0.062, 0.08), DIAMOND, eps=0.004,
            outline=polygon([(0, 1), (0.75, 0.15), (0, -1), (-0.75, 0.15)]), rings=16, segs=48)

    b.mouth(k, H, 0.24, 0.37, 0.2, 0.1, (b.PINK, b.MOUTH, INK, b.WHITE), fangs=[(0.15, 0.02, 0.042)], rise=0.03,
            line_w=0.006)

    k.build(voxel=0.011, band=0.012)
    centre = b.on_ell(H, HR, v.EYE_D)
    cols = dict(iris=b.RED, pupil=b.PUPIL, light=b.WHITE)
    k.eye_states(lambda state: b.lay_eyes(k, state, centre, v.EYE_D, v.EYE_HALF, b.eye_colours(b.WHITE, INK, v.EYE),
                                          TEAL, INK, v.EYE, cols))
    k.export(out)


if __name__ == "__main__":
    venusaurmega(*args())
