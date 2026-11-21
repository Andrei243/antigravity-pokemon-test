# Galvantula sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/galvantula.py -- <the export's Galvantula.glb> <out.glb>
#
# Joltik grown up (joltik.py's points of fur, feet and way of laying eyes). From the references: a big hairy spider,
# low. A large mauve abdomen behind, its top barred with yellow in jagged bands (a yellow cap at its front, a yellow
# zigzag band across its middle), and at each of its back corners a fan of dark blue spiky points rising up and out.
# A round yellow head in front, low, fuzzy points round its crown; its lower half mauve under a jagged yellow edge.
# Six eyes: two big ones low on the face's sides (black, a blue ring, a white glint), four small blue ones in a square
# between them. Under the face a short yellow palp with a fuzzy end. Four very thick yellow legs that rise from the body
# to a knee as high as the abdomen's flank and come down to a broad club of yellow points over blue claws.
# Skeleton: body (0, 0.04, 0.16), head (0, -0.04, 0.15), legs (+-0.06, -0.06 / 0.06, 0.14) on leg{s} and hind{s}.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, tri, unit, eye_look, args, sibling

jt = sibling("joltik")
YELLOW, BLUE, INK, WHITE = jt.YELLOW, jt.BLUE, jt.INK, jt.WHITE
MAUVE, MAUVE_D, NAVY, RING = "#C09CBA", "#A88AA6", "#34478C", "#5A86D0"

AB, ABR = np.array([0.0, 0.1, 0.19]), np.array([0.13, 0.142, 0.096])         # the abdomen
HD, HDR = np.array([0.0, -0.105, 0.158]), np.array([0.094, 0.076, 0.071])       # the head
VOXEL = 0.004
LEGS = [  # bone, hip, knee, foot (on +X), the foot's way
    ("leg{s}", (0.06, -0.05, 0.16), (0.13, -0.165, 0.235), (0.15, -0.255, 0.07), (0.3, -0.8, -0.55)),
    ("hind{s}", (0.07, 0.05, 0.16), (0.17, 0.11, 0.24), (0.225, 0.19, 0.07), (0.65, 0.5, -0.55)),
]


def eye_big(u, v):
    """Black with a blue ring round it and a white glint on its upper inner side."""
    r = np.sqrt(u * u + v * v)
    c = np.tile(srgb(INK), (len(u), 1))
    c[r > 0.72] = srgb(RING)
    c[r > 0.9] = srgb(NAVY)
    c[((u + 0.25) / 0.2) ** 2 + ((v - 0.28) / 0.2) ** 2 < 1] = srgb(WHITE)
    return c


def eye_small(u, v):
    c = np.tile(srgb(BLUE), (len(u), 1))
    c[((u + 0.15) / 0.34) ** 2 + ((v - 0.3) / 0.3) ** 2 < 1] = srgb("#9DBCF2")
    return c


def galvantula(src, out):
    k = Kit(src, coat="fur")
    # ---- the abdomen, the head and the waist between them
    k.add("abdomen", ell(AB, ABR, sub=5), MAUVE, "body")
    k.add("waist", ell((0, -0.02, 0.17), (0.07, 0.07, 0.055)), MAUVE, "body")
    k.add("head", ell(HD, HDR, sub=5), YELLOW, "head")
    # the abdomen's yellow bars: a cap at its front and a zigzag band across its middle, on its upper part
    def bars(P):
        y = P[:, 1] - AB[1]
        zig = 0.016 * tri(P[:, 0] * 9)
        up = inside(-(P[:, 2] - (AB[2] - 0.035) + 0.04 * np.abs(P[:, 0]) / ABR[0]), 0.004)
        cap = inside(y - (-0.06 + zig), 0.003)
        band = inside(y - (0.06 + zig), 0.003) * inside(-(y - (0.0 + zig)), 0.003)
        return up * np.clip(cap + band, 0, 1)
    k.paint(bars, YELLOW, on=["abdomen"])
    # the head's lower half mauve under a jagged edge
    def jaw(P):
        z = P[:, 2] - HD[2]
        zig = 0.008 * tri(P[:, 0] * 22)
        return inside(z - (-0.012 + zig), 0.0025) * (P[:, 1] < HD[1] + 0.03)
    k.paint(jaw, MAUVE, on=["head"])
    # ---- fur: points round the head's crown and the abdomen's flanks
    jt.crown(k, HD, HDR, "head", n=26, length=(0.03, 0.044), radius=0.022, seed=596, lowest=0.05, face=(-0.35, 0.95))
    for i, (d, ln) in enumerate((((0.95, 0.0, 0.0), 0.04), ((0.85, -0.45, -0.1), 0.036), ((0.92, 0.35, 0.05), 0.04))):
        for s in (1, -1):
            p, n = jt.on_ell(AB, ABR, (d[0] * s, d[1], d[2]))
            jt.point(k, f"flank{i}{'R' if s > 0 else 'L'}", p, n + np.array([0, 0.3, -0.2]), ln, 0.02, "body",
                     YELLOW, flat=0.55, surface="fur")
    # ---- the dark blue fans at the abdomen's back corners: a few thick tapering points rising up and out
    for s in (1, -1):
        base = np.array([0.075 * s, 0.17, 0.235])
        for j, (d, ln) in enumerate((((0.5, 0.35, 1.0), 0.1), ((0.85, 0.5, 0.75), 0.11), ((0.95, 0.75, 0.3), 0.085),
                                     ((0.25, 0.6, 0.9), 0.08), ((0.7, 0.9, 0.55), 0.075))):
            jt.point(k, f"fan{j}{'R' if s > 0 else 'L'}", base + np.array([0.012 * j * s, 0.01 * j, -0.008 * j]),
                     (d[0] * s, d[1], d[2]), ln, 0.046, "body", NAVY, flat=0.32, surface="fur")
    # ---- four very thick legs, rising to a high knee and down to a club of fur
    for bone, hip, knee, ft, d in LEGS:
        k.add("leg" + bone[:3], tube([hip, knee, np.array(knee) + (np.array(ft) - np.array(knee)) * 0.5, ft], [0.024, 0.028, 0.038, 0.045]), YELLOW, bone, mirror=True)
        k.add("club" + bone[:3], ell(ft, (0.05, 0.05, 0.038)), YELLOW, bone, mirror=True)
        for s in (1, -1):
            m = np.array([s, 1, 1.0])
            jt.foot(k, ("R" if s > 0 else "L") + bone[:3], np.array(ft) * m + np.array([0, 0, -0.005]),
                    np.array(d) * m, bone.format(s="R" if s > 0 else "L"), 0.04, claw=NAVY, n=7, scale=1.9)
            # a ring of points round the club's top, where the fur starts
            kn, f = np.array(knee) * m, np.array(ft) * m
            ax = unit(*(f - kn))
            for j in range(5):
                t = 2 * math.pi * j / 5 + 0.6
                a = np.cross(ax, (0, 0, 1.0)); a /= np.linalg.norm(a)
                b = np.cross(ax, a)
                out_ = math.cos(t) * a + math.sin(t) * b
                jt.point(k, f"ruff{j}{'R' if s > 0 else 'L'}{bone[:3]}", f - ax * 0.035 + out_ * 0.032,
                         out_ + ax * 0.9, 0.04, 0.016, bone.format(s="R" if s > 0 else "L"), YELLOW, flat=0.55,
                         surface="fur")
    # ---- the palp under the face: a short yellow stub with a fuzzy end
    k.add("palp", tube([(0, -0.14, 0.12), (0, -0.16, 0.095), (0, -0.165, 0.08)], [0.026, 0.026, 0.028]), YELLOW, "head")
    jt.foot(k, "palp", (0, -0.167, 0.072), (0, -0.4, -1), "head", 0.022, claw=NAVY, n=5, scale=1.1)
    # ---- the four small eyes never blink
    for s in (1, -1):
        for j, z in enumerate((0.08, 0.4)):
            p, n = jt.on_ell(HD, HDR, (0.17 * s, -1, z))
            k.decal(p, n, (0.0095, 0.0095), eye_small if s > 0 else (lambda u, v: eye_small(-u, v)),
                    eps=0.002, rings=12, segs=40)
    k.build(voxel=VOXEL, smooth=3, band=0.006, wband=0.04)

    V = np.array([v.co[:] for v in k.ob.data.vertices])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    print("KIT: board: shows %.3f in a square of %.3f" % (np.ptp(proj), 1.3 * max(h, 0.85 * max(w, l))))

    def lay(state):
        p, n = jt.on_ell(HD, HDR, (0.62, -1, -0.12))
        k.decal(p, n, (0.021, 0.024), eye_look(state, eye_big, srgb(YELLOW), srgb(INK), line=0.16), up=(0, 0, 1),
                eps=0.002, rings=20, segs=56, mirror=True)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    galvantula(*args())
