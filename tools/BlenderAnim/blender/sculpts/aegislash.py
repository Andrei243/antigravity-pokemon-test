# Aegislash (Shield Forme) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/aegislash.py -- <the export's Aegislash.glb> <out.glb>
# Grown from honedge.py (sibling): the line's rings as pucks with their faces as patches, the slit eye, the way of
# making hard pieces; what the pictures change.
#
# From the references: a great sword standing point down behind a round shield, hovering. Above the shield the sword's
# gold hilt: a tall faceted pommel spike, a gold crossguard band with a horn rising at each end, and under the band a
# black almond socket with the one eye in it, purple with a pale rim and a black slit. From the guard's ends two black
# cloth arms loop out and down round the shield's upper sides and hold its rim. The shield: a beige rim round a gold
# face, on the face a dark triangle with hollow curved sides, a beige ring at each of its corners (black at their
# middles) and beige spokes from its sides to the rim. Behind the shield, at each side, a fan of five lavender strips
# like feathers, darker at their roots, longest at the top. Below the shield the sword's silver blade, its edges cut in
# steps, a gold strip down its middle. The brief's "golden-hilted sword behind a round ornate shield, with two cloth
# arms" holds; the lavender fans and the stepped silver blade are the pictures'.
# Skeleton: root (0, 0, 0) and body (0, 0, 0.446) alone.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, hull, lathe, srgb, inside, unit, eye_look, args, sibling

hd = sibling("honedge")
GOLD, GOLD_DARK, BEIGE, DARK = "#E4CF5E", "#C8AE3C", "#CBBE9C", "#2E2A28"
ARM = "#3A3430"
SILVER, IRIS, RIM, INK = "#DCE2EA", "#9A7ACC", "#EEE8F4", "#141214"
FAN_ROOT, FAN = "#8E6CC4", "#B696E0"

C = np.array([0.0, -0.085, 0.43])       # the shield's middle
R, TH = 0.176, 0.028                    # its radius and half its thickness
RING_R, RING_H, RING_AT = 0.038, 0.011, 0.086
SOCKET = np.array([0.0, -0.052, 0.643])


def shield(k):
    k.add("shield", hd.puck(C, R * 0.97, TH, n=4), GOLD, "body")
    # its beige rim, a loose band standing a little proud of the face
    rim = [(0.152, -0.032), (0.182, -0.032), (0.182, 0.032), (0.152, 0.032), (0.152, -0.032)]
    k.add("rim", lathe(C, (0, -1, 0), rim, seg=72), BEIGE, "body", fuse=False)

    def polar(P):
        q = P - C
        return q[:, 0], q[:, 2], q[:, 1]
    corners = [90, 210, 330]
    h = 0.07

    def tri(P):
        x, z, y = polar(P)
        m = np.full(len(P), -1.0)
        for a in corners:
            n = np.array([-math.cos(math.radians(a)), -math.sin(math.radians(a))])
            tx = np.array([-n[1], n[0]])
            along = x * n[0] + z * n[1]
            tang = x * tx[0] + z * tx[1]
            m = np.maximum(m, along - h + 0.016 * np.clip(1 - (tang / 0.1) ** 2, 0, 1))
        return inside(m, 0.0015) * (y < -0.005)
    k.paint(tri, DARK, on=["shield"])

    def spokes(P):
        x, z, y = polar(P)
        m = np.zeros(len(P))
        for a in (270, 30, 150):
            d = np.array([math.cos(math.radians(a)), math.sin(math.radians(a))])
            along = x * d[0] + z * d[1]
            across = -x * d[1] + z * d[0]
            m = np.maximum(m, inside(np.abs(across) - 0.007, 0.0015) * inside(h - 0.035 - along, 0.002))
        return m * (y < -0.005)
    k.paint(spokes, BEIGE, on=["shield"])

    fronts = []
    for a in corners:
        c = C + RING_AT * np.array([math.cos(math.radians(a)), 0, math.sin(math.radians(a))])
        c = c + (0, -TH - 0.004, 0)
        k.add(f"ring{a}", hd.puck(c, RING_R, RING_H), BEIGE, "body")
        fronts.append(c + (0, -RING_H, 0))
    return fronts


def hilt(k):
    # the faceted pommel spike, gold, flat across its depth
    pts = []
    for z, r in ((0.71, 0.04), (0.79, 0.03)):
        for i in range(8):
            a = 2 * math.pi * (i + 0.5) / 8
            pts.append((r * math.cos(a), -0.02 + r * 0.55 * math.sin(a), z))
    pts.append((0, -0.02, 0.9))
    k.add("spike", hull(pts), GOLD, "body", fuse=False)
    # a pale facet down its front
    k.add("facet", hull([(-0.011, -0.0435, 0.712), (0.011, -0.0435, 0.712), (0, -0.026, 0.875), (0, -0.034, 0.73)]),
          "#F4E890", "body", fuse=False)
    # the guard band and its two horns
    band = [(x, y, z) for x in (-0.112, 0.112) for y in (-0.05, 0.01) for z in (0.674, 0.712)]
    band += [(x, -0.056, 0.693) for x in (-0.1, 0.1)]
    k.add("guard", hull(band), GOLD, "body", fuse=False)
    k.add("guardline", hull([(x, y, z) for x in (-0.104, 0.104) for y in (-0.058, -0.05) for z in (0.67, 0.678)]),
          GOLD_DARK, "body", fuse=False)
    for s in (1, -1):
        base = np.array([0.096 * s, -0.02, 0.702])
        horn = [base + (dx, dy, dz) for dx in (-0.016, 0.016) for dy in (-0.022, 0.022) for dz in (-0.02, 0.0)]
        horn.append(base + (0.022 * s, 0, 0.07))
        k.add(f"horn{'R' if s > 0 else 'L'}", hull(horn), GOLD, "body", fuse=False)

    # the black almond socket that holds the eye
    def almond(V):
        V = V.copy()
        V[:, 2] *= np.clip(1 - np.abs(V[:, 0]), 0, 1) ** 0.55
        return V
    k.add("socket", ell(SOCKET, (0.064, 0.022, 0.034), deform=almond), DARK, "body")
    k.add("neck", ell((0, -0.03, 0.655), (0.03, 0.022, 0.03)), DARK, "body")

    # the black cloth arms, from the guard's ends round to the shield's rim
    arm = [(0.08, -0.02, 0.682), (0.155, -0.025, 0.665), (0.222, -0.035, 0.585), (0.226, -0.055, 0.5),
           (0.188, -0.075, 0.45)]
    k.add("arm", tube(arm, [0.018, 0.018, 0.018, 0.017, 0.016]), ARM, "body", mirror=True)


def blade(k):
    out = [(0.046, 0.6), (0.05, 0.3), (0.056, 0.27), (0.044, 0.255), (0.054, 0.235), (0.036, 0.215), (0.044, 0.195),
           (0.024, 0.175), (0.028, 0.162), (0.0, 0.148)]
    pts = out + [(-x, z) for x, z in reversed(out[:-1])]
    k.add("blade", slab(pts, (0, -0.03, 0), (1, 0, 0), (0, 0, 1), 0.022), SILVER, "body", fuse=False)
    k.add("edge", slab([(0.02, 0.29), (0.0, 0.2), (-0.02, 0.29)], (0, -0.044, 0), (1, 0, 0), (0, 0, 1), 0.01), GOLD,
          "body", fuse=False)


def fans(k):
    """Five lavender strips behind each side of the shield, darker at their roots, longest at the top."""
    for i, (z, tip) in enumerate(((0.462, 0.29), (0.42, 0.282), (0.378, 0.27), (0.338, 0.248), (0.3, 0.214))):
        d = unit(1, 0.28, -0.16 - 0.04 * i)
        up = np.array([0, 0, 1.0]) - d * d[2]; up /= np.linalg.norm(up)
        root = np.array([0.09, 0.006 + 0.003 * i, z])
        L = (tip - 0.08) / d[0]
        w = 0.0185
        cut = 0.55 * L
        k.add(f"fanroot{i}", slab([(0, -w), (cut, -w), (cut, w), (0, w)], root, d, up, 0.016), FAN_ROOT, "body",
              fuse=False, mirror=True)
        k.add(f"fan{i}", slab([(cut, -w), (L - 0.012, -w), (L, w), (cut, w)], root, d, up, 0.016), FAN, "body",
              fuse=False, mirror=True)


def aegislash(src, out_path):
    k = Kit(src, coat="metal")
    rings = shield(k)
    hilt(k)
    blade(k)
    fans(k)
    CB, CK = srgb(BEIGE), srgb(DARK)
    def hole(u, v):
        c = np.tile(CK, (len(u), 1))
        c[np.hypot(u, v) > 0.58] = CB
        return c
    for c in rings:
        k.decal(c, (0, -1, 0), (0.032, 0.032), hole, eps=0.003, rings=14, segs=56)

    k.build(voxel=0.005, smooth=3, band=0.006)

    CI, CR, CKK, CD = srgb(IRIS), srgb(RIM), srgb(INK), srgb(DARK)
    def eye(u, v):
        r = np.hypot(u, v)
        c = np.tile(CI, (len(u), 1))
        c[r > 0.74] = CR
        c[r > 0.94] = CD
        c[(np.abs(u) < 0.13 * np.sqrt(np.clip(1 - (v / 0.66) ** 2, 0, 1)) + 0.03) & (np.abs(v) < 0.64)] = CKK
        c[((u - 0.3) / 0.14) ** 2 + ((v - 0.32) / 0.14) ** 2 < 1] = (1, 1, 1)
        return c
    def lay(state):
        ink = INK if state in ("Open", "Fierce") else "#D8D0E4"
        look = eye_look(state, eye, CD, srgb(ink), line=0.16)
        k.decal(SOCKET + (0, -0.022, 0.0), (0, -1, 0), (0.027, 0.027), look, eps=0.003, rings=20, segs=56)
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    aegislash(*args())
