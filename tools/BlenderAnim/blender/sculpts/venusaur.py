# Venusaur sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/venusaur.py -- <the export's Venusaur.glb> <out.glb>
#
# The design, from the references: Ivysaur grown heavy, wide and low, teal, on four thick legs with big white claws;
# a wide head carried low between the shoulders, the mouth wide open (pink inside, white fangs at its corners), the
# eyes red and scowling, the ears pointed and pink inside; one fern leaf lying forward over the brow; a skirt of great
# fern leaves spread round the back; out of them a thick brown trunk scored with dark zigzags, crowned in gold, and on
# it a great flower of five broad pink petals, drooping at their round tips, spotted white.
#
# Grown from bulbasaur.py and ivysaur.py (sibling): their body, legs, claws, head, ears, patches, mouth, eyes and fern.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, tri, unit, args, outward, sheet, sibling

b = sibling("bulbasaur")
b.EDGE = 0.0055                                         # paint edges at this voxel: stepped under it
iv = sibling("ivysaur")

TEAL, SPOT, INK, CLAW = "#5EBDBA", "#357E80", "#1C2A2C", "#F2F0EA"
LINING = "#E06A7A"
PETAL, PSPOT = "#F06C86", "#FBE6EC"
TRUNK, ZIG, CROWN = "#9A7A34", "#5C4420", "#F2C63A"
LEAF, RIB = "#4FA344", iv.RIB


def petal(k, name, base, d, length, width, droop, thick, bone, col, spots, spot_col):
    """A broad petal from base along d, its face up, rounded at its tip and drooping; white spots lying on it."""
    base, d = np.array(base, float), unit(*d)
    side = np.cross(d, (0, 0, 1.0)); side /= np.linalg.norm(side)
    up = np.cross(side, d)

    def mid(uu):
        return base[None, None] + length * (uu[..., None] * d[None, None]
                                            - droop * uu[..., None] ** 2.2 * np.array([0, 0, 1.0])[None, None])

    def at(u, v):
        uu = 0.015 + 0.97 * u
        w = width * np.clip(np.sin(np.pi * np.clip(uu, 0, 1) ** 0.62), 0, 1) ** 0.55 * (0.35 + 0.65 * smoothstep(0, 0.4, uu))
        s = (v - 0.5) * 2 * w
        cup = -0.03 * (2 * v - 1) ** 2 * uu                  # the edges a little down: a cupped back
        return mid(uu) + s[..., None] * side[None, None] + cup[..., None] * up[None, None]

    k.add(name, outward(sheet(at, 40, 15, thick)), col, bone, fuse=False, surface="leaf")
    # Spots: flat discs lying on the petal's upper face
    for j, (su, sv, r) in enumerate(spots):
        p = at(np.array([[su]]), np.array([[sv]]))[0, 0]
        pu = at(np.array([[su + 0.02]]), np.array([[sv]]))[0, 0] - at(np.array([[su - 0.02]]), np.array([[sv]]))[0, 0]
        pv = at(np.array([[su]]), np.array([[sv + 0.02]]))[0, 0] - at(np.array([[su]]), np.array([[sv - 0.02]]))[0, 0]
        n = np.cross(pv, pu); n /= np.linalg.norm(n)
        if n[2] < 0:
            n = -n
        eu = pu / np.linalg.norm(pu); ev = np.cross(n, eu)
        V, F = ell((0, 0, 0), (r, r * 0.8, 0.006), sub=2)
        V = V @ np.stack([eu, ev, n], axis=1).T + p + n * (thick / 2 + 0.002)
        k.add(f"{name}spot{j}", outward((V, F)), spot_col, bone, fuse=False, surface="leaf")


H = np.array([0, -0.43, 0.42])
HR = np.array([0.275, 0.2, 0.17])
JAW = ((0, -0.47, 0.31), (0.25, 0.18, 0.11))
EYE_D = unit(0.6, -0.78, 0.18)
EYE_HALF = (0.058, 0.05)
EYE = iv.EYE
LEGS = dict(x=0.24, front=-0.22, hind=0.27, top=0.3, ankle=0.07, r=0.12, thigh=(0.19, 0.21, 0.2), splay=0.055,
            foot=(0.11, 0.125, 0.058), toe=0.03, spread=0.052, claw=0.048)
TOP = np.array([0, 0.08, 0.92])                          # the trunk's top, where the petals start


def venusaur(src, out):
    k = Kit(src, coat="scales")
    BC, BR = (0, 0.05, 0.37), (0.36, 0.42, 0.23)
    b.body(k, BC, BR, TEAL, chest=((0, -0.22, 0.38), (0.32, 0.2, 0.19)))
    b.legs(k, LEGS, TEAL, CLAW)
    b.underside(k, ((0, 0.05, 0.25), (0.34, 0.42, 0.18)), ((0, 0.28, 0.29), (0.34, 0.24, 0.22)), TEAL)
    b.head(k, H, HR, JAW, TEAL)
    ear0, ear1 = (0.17, -0.39, 0.55), (0.25, -0.41, 0.67)
    b.ears(k, ear0, ear1, 0.068, TEAL)
    b.ear_lining(k, ear0, ear1, 0.068, LINING)

    # The trunk, fused so its zigzags are paint, and its golden crown
    k.add("trunk", tube([(0, 0.08, 0.5), (0, 0.08, 0.7), (0, 0.08, 0.88)], [0.15, 0.13, 0.1]), TRUNK, "flower",
          surface="shell")
    def zigzags(P):
        a = np.arctan2(P[:, 0], -(P[:, 1] - 0.08))
        m = np.zeros(len(P))
        for z0 in (0.66, 0.73, 0.8):
            m = np.maximum(m, inside(np.abs(P[:, 2] - (z0 + 0.014 * tri(a * 7 / (2 * math.pi)))) - 0.005, b.EDGE))
        return m * (P[:, 2] > 0.6) * (np.hypot(P[:, 0], P[:, 1] - 0.08) < 0.17)
    k.paint(zigzags, ZIG)
    for i in range(8):
        a = 2 * math.pi * (i + 0.5) / 8
        o = np.array([math.sin(a), -math.cos(a), 0.0])
        t = np.cross(o, (0, 0, 1.0))
        c = TOP + o * 0.075
        pts = [c - t * 0.026, c + t * 0.026, c - o * 0.02, c + o * 0.012 - np.array([0, 0, 0.01]),
               c + o * 0.035 + np.array([0, 0, 0.075])]
        k.add(f"crown{i}", hull(pts), CROWN, "flower", fuse=False, surface="shell")
    k.add("heart", ell(TOP + (0, 0, 0.005), (0.08, 0.08, 0.03)), CROWN, "flower", fuse=False, surface="shell")

    # The petals: five, one to the back and two to each side forward and behind; the white spots on each
    spots = [(0.5, 0.32, 0.034), (0.62, 0.7, 0.03), (0.82, 0.45, 0.036)]
    for i in range(5):
        a = 2 * math.pi * (i + 0.5) / 5
        d = (math.sin(a), -math.cos(a), 0.12)
        base = TOP + np.array([math.sin(a), -math.cos(a), 0]) * 0.05 - (0, 0, 0.02)
        petal(k, f"petal{i}", base, d, 0.42, 0.18, 0.36, 0.03, "flower", PETAL, spots, PSPOT)

    # The skirt of fern leaves round the trunk's foot, and one lying forward over the brow
    for name, a, length in (("fernf", 52, 0.44), ("ferns", 105, 0.46), ("fernb", 152, 0.42)):
        r = math.radians(a)
        d = (math.sin(r), -math.cos(r), 0.22)
        iv.fern(k, name, (0.06 * math.sin(r), 0.08 - 0.06 * math.cos(r), 0.6), d, (0.1 * math.sin(r), -0.1 * math.cos(r), 1),
                length, 0.15, "flower", col=LEAF, rib=RIB, droop=0.6, teeth=9, cut=0.5)
    iv.fern(k, "fernhead", (0, -0.12, 0.74), (0, -1, 0.1), (0, -0.1, 1), 0.46, 0.15, "flower", col=LEAF, rib=RIB,
            droop=0.55, teeth=8, cut=0.5, mirror=False)

    # The patches: a dark one on the brow under the leaf, the flanks and the legs, small warts
    for d, h in (((0.85, 0.0, 0.35), (0.06, 0.05)), ((0.7, 0.62, 0.3), (0.05, 0.045)), ((0.8, -0.45, 0.3),
                                                                                         (0.04, 0.035))):
        b.patch(k, b.on_ell(BC, BR, d), d, h, SPOT, b.PATCH, mirror=True)
    b.patch(k, (0.42, -0.2, 0.28), (1, -0.2, 0.2), (0.04, 0.048), SPOT, b.PATCH2, mirror=True, depth=0.05)
    b.patch(k, (0.42, 0.28, 0.29), (1, 0.1, 0.2), (0.04, 0.046), SPOT, b.PATCH, mirror=True, depth=0.05)

    # The mouth wide open, white fangs at its corners
    b.mouth(k, H, 0.24, 0.37, 0.2, 0.1, (b.PINK, b.MOUTH, INK, b.WHITE), fangs=[(0.15, 0.02, 0.042)], rise=0.03,
            line_w=0.006)

    k.build(voxel=0.011, band=0.012)
    centre = b.on_ell(H, HR, EYE_D)
    cols = dict(iris=b.RED, pupil=b.PUPIL, light=b.WHITE)
    k.eye_states(lambda state: b.lay_eyes(k, state, centre, EYE_D, EYE_HALF, b.eye_colours(b.WHITE, INK, EYE), TEAL,
                                          INK, EYE, cols))
    k.export(out)


if __name__ == "__main__":
    venusaur(*args())
