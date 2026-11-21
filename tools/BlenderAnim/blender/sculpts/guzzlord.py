# Guzzlord sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/guzzlord.py -- <the export's Guzzlord.glb> <out.glb>
#
# The design, from the references: a black glutton wider than it is tall whose whole front is one great open mouth: a
# bulging blue hollow (paler in its middle, a dark ridge across its lower part) ringed by yellow lips cut into jagged
# teeth, the upper row pointing down, the lower pointing up. On top a small black head like a crowned block with two
# slanting cyan eyes and yellow marks under them, a black horn edged in yellow at each side of its crown. Black spikes
# on the shoulders; on the left shoulder a small stalk with a pincer holding a yellow ball (one, not a pair). Two long
# thick black arms grow from the lower sides, curve out and up and end above the shoulders in great jaws of their own,
# open forward, black teeth round a pale gullet. Short fat legs with yellow zigzags and black claws, and on the right
# behind a short tail ending in a spiked ball.
# The brief expected a dark mouth: both pictures show it blue inside, as built (a fused blue dome set into the black
# body, not a carve). The arms' jaws are drawn in over the shoulders and forward from the art's width.
# Skeleton: root, body (0.3), head (0.48), armL/armR (+-0.2, 0, 0.4), tail (0, 0.15, 0.2), legL/legR (+-0.12, 0, 0.12).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, boxy, eye_look, args, hull, inside, tri, axes, outward

# --- colours ----------------------------------------------------------------------------------------------------------
BLACK, BLACK_HI, YELLOW = "#2C2F3A", "#3C4050", "#F2C632"
BLUE, BLUE_PALE, BLUE_DEEP, RIDGE = "#3A86AC", "#79C4DC", "#2A6488", "#2F4656"
GULLET, CYAN, INK = "#B9C0CA", "#5FE4F2", "#10131A"

# --- the frame --------------------------------------------------------------------------------------------------------
BODY_C, BODY_R = np.array([0.0, 0.03, 0.31]), np.array([0.29, 0.24, 0.27])
MOUTH_C, MOUTH_R = np.array([0.0, -0.09, 0.325]), np.array([0.205, 0.165, 0.19])
HEAD_C, HEAD_R = np.array([0.0, -0.035, 0.585]), np.array([0.092, 0.072, 0.07])
EYE_X, EYE_Z = 0.042, 0.6
JAW = np.array([0.32, -0.1, 0.6])          # the middle of the right arm's jaw (mirrored for the left)


def mouth_point(x, z, scale=1.0):
    """A point of the mouth dome's front at offsets x, z (as shares of its half sizes), and its normal."""
    lx, lz = x * scale, z * scale
    ly = -math.sqrt(max(1 - lx * lx - lz * lz, 0.02))
    p = MOUTH_C + np.array([lx, ly, lz]) * MOUTH_R
    n = unit(lx / MOUTH_R[0], ly / MOUTH_R[1], lz / MOUTH_R[2])
    return p, n


def body(k):
    def pear(V):
        V = V.copy()
        V[:, 0] *= 1 + 0.12 * np.clip(-V[:, 2], 0, 1)     # wider low, where the lower lip hangs
        return V
    k.add("torso", ell(BODY_C, BODY_R, deform=pear), BLACK, "body")
    k.add("mouth", ell(MOUTH_C, MOUTH_R), BLUE, "body")
    # the yellow lips round the mouth, on the black body close to the dome's rim
    def lips(P):
        q = (P - MOUTH_C) / MOUTH_R
        d = np.linalg.norm(q, axis=1)
        return inside(np.abs(d - 1.0) * 0.2 - 0.03, 0.003) * (P[:, 1] < -0.02)
    k.paint(lips, YELLOW, on=["torso"])
    # the dome paler in its middle and on the right, a dark ridge across its lower part
    def pale(P):
        q = (P - MOUTH_C - np.array([0.05, 0, 0.06])) / (MOUTH_R * np.array([0.38, 1, 0.34]))
        return inside(np.linalg.norm(q[:, [0, 2]], axis=1) - 1, 0.12)
    k.paint(pale, BLUE_PALE, on=["mouth"])
    def deep(P):
        q = (P - MOUTH_C) / MOUTH_R
        return inside(0.62 - np.hypot(q[:, 0], q[:, 2]), 0.05)
    k.paint(deep, BLUE_DEEP, on=["mouth"])
    def ridge(P):
        z = P[:, 2] - (MOUTH_C[2] - 0.11) - 0.012 * tri(P[:, 0] * 14)
        return inside(np.abs(z) - 0.022, 0.003)
    k.paint(ridge, RIDGE, on=["mouth"])
    # the jagged teeth of the lips: the upper row points down, the lower up, lying on the dome
    for j, phi in enumerate(np.linspace(25, 155, 7)):
        tooth(k, f"toothu{j}", phi, 0.065 if j % 2 else 0.05)
    for j, phi in enumerate(np.linspace(205, 335, 7)):
        tooth(k, f"toothl{j}", phi, 0.06 if j % 2 == 0 else 0.045)


def tooth(k, name, phi, length):
    a = math.radians(phi)
    root, n = mouth_point(math.cos(a), math.sin(a), 0.95)
    inward = -np.array([math.cos(a), 0, math.sin(a)])
    inward = inward - n * np.dot(inward, n); inward /= np.linalg.norm(inward)
    side = np.cross(n, inward)
    tipp = root + inward * length + n * 0.004
    pts = [root + side * 0.022 * s + n * dn - inward * 0.012 for s in (-1, 1) for dn in (-0.004, 0.02)] + [tipp, tipp + n * 0.01]
    k.add(name, hull(pts), YELLOW, "body", fuse=False, surface="shell")


def head(k):
    k.add("head", ell(HEAD_C, HEAD_R, deform=boxy(3.0), sub=4), BLACK, "head")
    k.add("neck", ell((0, 0.0, 0.53), (0.1, 0.08, 0.06)), BLACK, "head")
    # yellow marks under the eyes: a band along the head's lower front
    def cheek(P):
        z = P[:, 2] - (HEAD_C[2] - 0.035) - 0.01 * tri(P[:, 0] * 22)
        return inside(np.abs(z) - 0.011, 0.002) * (P[:, 1] < HEAD_C[1] - 0.03) * inside(np.abs(P[:, 0]) - 0.075, 0.003)
    k.paint(cheek, YELLOW, on=["head"])
    # a horn at each side of the crown, black edged yellow, and a short middle point
    for s in (1, -1):
        base = HEAD_C + np.array([s * 0.07, 0.0, 0.045])
        d = unit(s * 0.45, 0.1, 1)
        a, b = axes(d)
        tipp = base + d * 0.11
        k.add(f"horn{'R' if s > 0 else 'L'}", hull([base + a * 0.022 * p + b * 0.018 * q for p in (-1, 1) for q in (-1, 1)]
                                                   + [tipp]), BLACK, "head", fuse=False, surface="shell")
        k.add(f"hornedge{'R' if s > 0 else 'L'}", hull([base + a * 0.023 * p + b * 0.006 * q - np.array([0, 0.016, 0])
                                                        for p in (-1, 1) for q in (-1, 1)] + [tipp - np.array([0, 0.004, 0])]),
              YELLOW, "head", fuse=False, surface="shell")
    k.add("crown", hull([HEAD_C + np.array([x, y, 0.05]) for x in (-0.02, 0.02) for y in (-0.02, 0.02)]
                        + [HEAD_C + np.array([0, -0.005, 0.11])]), BLACK, "head", fuse=False, surface="shell")


def spikes(k):
    """Black spikes on the shoulders and back."""
    for s, side in ((1, "R"), (-1, "L")):
        for (x, y, z), d, L in (((0.2, 0.06, 0.5), (0.6, 0.1, 1), 0.1), ((0.27, 0.1, 0.4), (1, 0.2, 0.5), 0.09),
                                ((0.1, 0.18, 0.5), (0.3, 0.8, 0.8), 0.08)):
            base = np.array([x * s, y, z])
            dd = unit(d[0] * s, d[1], d[2])
            a, b = axes(dd)
            k.add(f"spike{x}{side}", hull([base + a * 0.024 * p + b * 0.024 * q for p in (-1, 1) for q in (-1, 1)]
                                          + [base + dd * L]), BLACK, "body", fuse=False, surface="shell")
    # the small pincer on a stalk on the left shoulder, holding a yellow ball
    root = np.array([-0.16, 0.0, 0.5])
    top = np.array([-0.2, -0.02, 0.66])
    k.add("stalk", tube([root, (-0.2, -0.01, 0.58), top], [0.018, 0.016, 0.016]), BLACK, "armL")
    k.add("ball", ell(top + (0, -0.005, 0.035), (0.022, 0.022, 0.022)), YELLOW, "armL", fuse=False, surface="shell")
    for s in (1, -1):
        base = top + np.array([0.025 * s, 0, 0.005])
        k.add(f"pincer{s}", outward(tube([base, base + (0.03 * s, -0.005, 0.03), base + (0.012 * s, -0.008, 0.065)],
                                         [0.013, 0.012, 0.008], seg=12)), BLACK, "armL", fuse=False, surface="shell")
        k.add(f"pincertip{s}", ell(base + (0.012 * s, -0.008, 0.065), (0.011, 0.011, 0.011)), YELLOW, "armL",
              fuse=False, surface="shell")


def arms(k):
    for s, side in ((1, "R"), (-1, "L")):
        bone = f"arm{side}"
        m = np.array([s, 1, 1])
        J = JAW * m
        path = [np.array(p) * m for p in ((0.22, 0.0, 0.24), (0.33, -0.02, 0.22), (0.38, -0.05, 0.32), (0.36, -0.08, 0.45))]
        k.add(f"arm{side}", tube(path + [J + (0, 0.0, -0.08)], [0.05, 0.052, 0.05, 0.048, 0.05]), BLACK, bone)
        # the jaw: two lobes parted forward, a pale gullet set back between them
        f = unit(-0.35 * s, -1, 0.15)                # the way it opens
        k.add(f"jawup{side}", ell(J + (0, 0.015, 0.064), (0.085, 0.08, 0.05), rot=(-30, 0, 0)), BLACK, bone)
        k.add(f"jawlo{side}", ell(J + (0, 0.015, -0.062), (0.08, 0.075, 0.046), rot=(28, 0, 0)), BLACK, bone)
        k.add(f"jawback{side}", ell(J + (0, 0.04, 0), (0.07, 0.05, 0.075)), BLACK, bone)
        k.add(f"gullet{side}", ell(J + (0, 0.0, 0), (0.062, 0.05, 0.05)), GULLET, bone)
        a = np.array([1.0, 0, 0])
        for j, dx in enumerate((-0.05, -0.017, 0.017, 0.05)):
            root = J + np.array([dx, -0.062, 0.05])
            k.add(f"fangu{j}{side}", hull([root + a * 0.013 * p + np.array([0, q * 0.012, 0]) for p in (-1, 1) for q in (-1, 1)]
                                          + [root + np.array([0, -0.004, -0.045])]), BLACK_HI, bone, fuse=False, surface="shell")
        for j, dx in enumerate((-0.035, 0.0, 0.035)):
            root = J + np.array([dx, -0.058, -0.05])
            k.add(f"fangl{j}{side}", hull([root + a * 0.013 * p + np.array([0, q * 0.012, 0]) for p in (-1, 1) for q in (-1, 1)]
                                          + [root + np.array([0, -0.004, 0.04])]), BLACK_HI, bone, fuse=False, surface="shell")


def legs(k):
    for s, side in ((1, "R"), (-1, "L")):
        bone = f"leg{side}"
        c = np.array([0.185 * s, -0.01, 0.1])
        k.add(f"leg{side}", ell(c, (0.11, 0.13, 0.105)), BLACK, bone)
        for j, dx in enumerate((-0.05, 0.0, 0.05)):
            root = c + np.array([dx, -0.11, -0.07])
            k.add(f"claw{j}{side}", hull([root + np.array([p * 0.016, q * 0.016, r * 0.012]) for p in (-1, 1) for q in (-1, 1)
                                          for r in (-1, 1)] + [root + np.array([0, -0.045, -0.02])]), BLACK_HI, bone,
                  fuse=False, surface="shell")
    # yellow zigzags across the legs' fronts
    def zig(P):
        z = P[:, 2] - 0.115 - 0.016 * tri(P[:, 0] * 12)
        return inside(np.abs(z) - 0.013, 0.002) * (P[:, 1] < -0.04) * inside(0.1 - np.abs(P[:, 0]), 0.003)
    k.paint(zig, YELLOW, on=["leg"])


def tail(k):
    a, b = np.array([0.1, 0.2, 0.2]), np.array([0.3, 0.27, 0.2])
    k.add("tail", tube([a, (0.2, 0.25, 0.19), b], [0.04, 0.035, 0.035]), BLACK, "tail")
    k.add("tailball", ell(b + (0.03, 0.01, 0.01), (0.05, 0.05, 0.05)), BLACK, "tail")
    for i in range(8):
        th = 2 * math.pi * i / 8
        d = unit(math.cos(th) * 0.9 + 0.3, 0.3 * math.sin(th), math.sin(th))
        base = b + (0.03, 0.01, 0.01) + d * 0.04
        a1, b1 = axes(d)
        k.add(f"tailspike{i}", hull([base + a1 * 0.014 * p + b1 * 0.014 * q for p in (-1, 1) for q in (-1, 1)] + [base + d * 0.05]),
              BLACK, "tail", fuse=False, surface="shell")


def eye_open():
    cc, ck, cw = srgb(CYAN), srgb(INK), np.array([0.85, 1.0, 1.0])
    def look(u, v):
        c = np.tile(cc, (len(u), 1))
        r = np.hypot(u, v)
        c[r > 0.84] = ck
        c[(u - 0.05) ** 2 + (v + 0.05) ** 2 < 0.3 ** 2] = cw * 0.9
        return c
    return look


ALMOND = None


def guzzlord(src, out_path):
    k = Kit(src, coat="scales")
    body(k)
    head(k)
    spikes(k)
    arms(k)
    legs(k)
    tail(k)
    k.build(voxel=0.0072, smooth=4, band=0.004)

    from sculptkit import polygon
    # an almond slanting down toward the nose: an angry glutton's eye
    outline = polygon([(-1.0, -0.15), (-0.35, 0.65), (0.6, 0.9), (1.0, 0.2), (0.4, -0.75), (-0.5, -0.65)])
    def lay(state):
        look = eye_look(state, eye_open(), srgb(BLACK), srgb(CYAN), line=0.2)
        for s in (1, -1):
            n = unit(0.3 * s, -1, 0.05)
            c = HEAD_C + np.array([EYE_X * s, -HEAD_R[1] * 0.98, EYE_Z - HEAD_C[2]])
            if s > 0:
                k.decal(c, n, (0.026, 0.017), look, eps=0.002, outline=outline, rings=20, segs=56)
            else:
                k.decal(c, n, (0.026, 0.017), lambda u, v: look(-u, v), eps=0.002,
                        outline=lambda th: outline(math.pi - th), rings=20, segs=56)
    k.eye_states(lay)

    V = np.array([v.co[:] for o in __import__("bpy").data.objects if o.type == "MESH" and o.name == k.name
                  for v in o.data.vertices])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    print("KIT: board: shows %.3f in a square of %.3f" % (np.ptp(proj), 1.3 * max(h, 0.85 * max(w, l))))
    k.export(out_path)


if __name__ == "__main__":
    guzzlord(*args())
