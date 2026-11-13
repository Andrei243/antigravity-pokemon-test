# Roselia sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/roselia.py -- <the export's Roselia.glb> <out.glb>
#
# The design, from the references: Budew grown and opened; a round pale face under a green hood whose lower edge comes
# down in points, three green thorns rising from the hood (one up the middle, one out each side); eyes shut in arcs
# with lashes, a small smile; a collar of two pointed green leaves out to the sides and a yellow wedge down the chest;
# a body like a green leaf bud tapering down to a pale stem and two little pale legs; thin pale arms, a red rose in the
# hand on -X and a blue rose in the hand on +X.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, leaf, srgb, inside, smoothstep, tri, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="leaf")

GREEN, PALE, LIMB, YELLOW, INKH = "#5FB34A", "#DDEFB0", "#C6E59C", "#F0D458", "#24301C"
H = np.array([0, 0.0, 0.445])            # the head's centre
E = 0.0035                               # a crisp paint's edge, under a voxel or so


# Little pale legs and feet under a pale stem
k.add("leg", tube([(0.024, 0.0, 0.14), (0.028, -0.005, 0.07), (0.03, -0.01, 0.03)], [0.019, 0.016, 0.014]), LIMB,
      "leg{s}", mirror=True)
k.add("foot", ell((0.03, -0.02, 0.016), (0.019, 0.03, 0.016)), LIMB, "leg{s}", mirror=True)
k.add("stem", ell((0, 0.0, 0.15), (0.042, 0.04, 0.05)), LIMB, "body")

# The body: a green leaf bud, round above and drawn to a point below
def bud(V):
    V = V.copy()
    t = np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.55 * t ** 1.5; V[:, 1] *= 1 - 0.55 * t ** 1.5
    return V
k.add("body", ell((0, 0.0, 0.252), (0.078, 0.07, 0.12), deform=bud), GREEN, "body")
# a pale leaf wrapped round its foot, rising to the +X side
k.paint(lambda P: inside(P[:, 2] - (0.175 + 0.9 * np.clip(P[:, 0], 0, 0.1) - 0.25 * np.clip(-P[:, 0], 0, 0.1)), E),
        LIMB, on=["body"])
# a light zigzag down the bud's front, as the drawing's leaf has
def zig(P):
    x = 0.012 * tri((P[:, 2] - 0.2) / 0.05)
    return inside(np.abs(P[:, 0] - x) - 0.004, E) * (P[:, 1] < -0.03) * (P[:, 2] > 0.19) * (P[:, 2] < 0.3)
k.paint(zig, "#8ED06A", on=["body"])
k.add("neck", ell((0, 0.0, 0.355), (0.04, 0.038, 0.035)), GREEN, "head")

# The collar: two pointed leaves out to the sides, a small one behind
for s in (1, -1):
    k.add(f"collar{'R' if s > 0 else 'L'}", leaf((0.02 * s, 0.0, 0.352), (s, -0.25, -0.05), 0.145, 0.066, 0.012),
          GREEN, "body", fuse=False)
k.add("collarB", leaf((0, 0.0, 0.352), (0, 1, -0.1), 0.09, 0.05, 0.012), GREEN, "body", fuse=False)
# the yellow wedge down the chest
k.decal((0, -0.075, 0.29), unit(0, -1, 0.1), (0.026, 0.055), YELLOW, eps=0.003, rings=10, segs=40,
        outline=polygon([(-1, 1), (1, 1), (0.0, -1)]))

# Thin pale arms out to little hands
for s in (1, -1):
    side = "R" if s > 0 else "L"
    hand = (0.155 * s, -0.025, 0.3 + (0.015 if s > 0 else -0.01))
    k.add(f"arm{side}", tube([(0.055 * s, 0.0, 0.31), (0.11 * s, -0.015, 0.3), hand], [0.014, 0.012, 0.012]), LIMB,
          f"arm{side}", fuse=False)
    k.add(f"hand{side}", ell(hand, (0.018, 0.016, 0.018)), LIMB, f"arm{side}", fuse=False)


def rose(name, c, axis, R, col, dark, bone):
    """A rose of loose cupped petals in three rings round a dark heart, its face along axis."""
    z = unit(*axis)
    x = np.cross(z, (0, 0, 1.0)); x /= np.linalg.norm(x)
    y = np.cross(z, x)
    c = np.array(c, float)
    k.add(f"{name}heart", ell(c + z * 0.18 * R, (0.34 * R, 0.34 * R, 0.36 * R), rot=(0, 0, 0)), dark, bone, fuse=False)
    # the bloom's round back, so it is a ball of petals from behind as well
    k.add(f"{name}back", ell(c - z * 0.25 * R, (0.7 * R, 0.7 * R, 0.7 * R)), col * 0.85, bone,
          fuse=False)
    k.add(f"{name}calyx", ell(c - z * 0.6 * R, (0.3 * R, 0.3 * R, 0.25 * R)), srgb(GREEN), bone, fuse=False)
    for ring, (n, rho, h, w, hh, tilt, off, colr) in enumerate([
            (3, 0.26, 0.2, 0.3, 0.38, 12, 0.3, dark),
            (5, 0.52, 0.02, 0.42, 0.46, 34, 0.1, col),
            (7, 0.78, -0.2, 0.44, 0.5, 58, 0.55, col),
            (8, 0.95, -0.42, 0.4, 0.42, 82, 0.2, col)]):
        for i in range(n):
            a = 2 * math.pi * (i + off) / n
            r = math.cos(a) * x + math.sin(a) * y
            up = math.cos(math.radians(tilt)) * z + math.sin(math.radians(tilt)) * r
            rad = math.cos(math.radians(tilt)) * r - math.sin(math.radians(tilt)) * z
            tan = np.cross(up, rad)
            def cup(V):
                V = V.copy()
                V[:, 1] -= 0.9 * V[:, 0] ** 2          # the petal's edges curl in toward the heart
                V[:, 0] *= 1 + 0.08 * np.sin(7 * V[:, 2] + i)   # a ruffled rim
                return V
            V, F = ell((0, 0, 0), (w * R, 0.11 * R, hh * R), deform=cup, sub=3)
            V = V @ np.stack([tan, rad, up], axis=1).T + c + r * rho * R + z * h * R
            k.add(f"{name}p{ring}{i}", (V, F), colr, bone, fuse=False)

# The roses: red in the hand on -X, blue in the hand on +X, each bigger than the hand by far
rose("red", (-0.235, -0.03, 0.305), (-0.45, -1, 0.15), 0.085, srgb("#E2505E"), srgb("#A82A3C"), "armL")
rose("blue", (0.235, -0.03, 0.34), (0.4, -1, 0.2), 0.085, srgb("#3B88E2"), srgb("#1E56A8"), "armR")

# The head: round, pale in front, the green hood over it coming down in points over the brow and to the cheeks
k.add("head", ell(H, (0.093, 0.086, 0.088)), PALE, "head")
def hood(P):
    q = P - H
    th = np.abs(np.arctan2(q[:, 0], -q[:, 1]))          # 0 at the front
    pt = (1 - tri(th / 0.75)) / 2                        # 1 at a point: the middle, and over each eye's outer corner
    edge = 0.05 - 0.052 * pt - 0.085 * smoothstep(1.05, 1.75, th)
    return inside(edge - q[:, 2], E)
k.paint(hood, GREEN, on=["head"])

# The thorns: green, rising from the hood, one up the middle and one out each side
k.add("thorn", tube([(0, 0.0, 0.5), (0, 0.004, 0.6), (0, 0.012, 0.69)], [0.04, 0.022, 0.003]), GREEN, "head",
      fuse=False)
k.add("thornS", tube([(0.05, 0.0, 0.485), (0.115, 0.004, 0.54), (0.18, 0.01, 0.595)], [0.034, 0.019, 0.003]), GREEN,
      "head", fuse=False, mirror=True)

# The smile
INK = srgb(INKH)
def mouth(u, v):
    c = np.tile(srgb(PALE), (len(u), 1))
    c[(np.abs(v - (-0.2 + 0.7 * u * u)) < 0.22) & (np.abs(u) < 0.85)] = INK
    return c
k.decal(H + unit(0, -1, -0.5) * 0.09, unit(0, -1, -0.5), (0.02, 0.012), mouth, eps=0.004, rings=8, segs=36)

# The eyes: shut in arcs bowed up, three lashes hanging from their outer half (u outward, v up)
LID = srgb(PALE)
def arc(u):
    return 0.15 + 0.4 * (1 - u * u)
def lashed(u, v):
    c = np.tile(LID, (len(u), 1))
    c[(np.abs(v - arc(u)) < 0.12) & (np.abs(u) < 0.88)] = INK
    for u0 in (0.4, 0.68):
        v0 = arc(u0)
        t = (v0 - v) / 0.6                                   # 0 at the arc, 1 at the lash's end
        uu = u0 + 0.18 * t                                   # slanting outward as it hangs
        c[(t > 0) & (t < 1) & (np.abs(u - uu) < 0.11 * (1.2 - t))] = INK
    return c
de = unit(0.42, -0.9, 0.05)
def eyes(state):
    k.decal(H + np.array([0, 0, -0.008]) + de * 0.09, de, (0.033, 0.033), eye_look(state, lashed, LID, INK, line=0.15),
            eps=0.004, rings=20, segs=56, mirror=True)

k.build(voxel=0.005)
k.eye_states(eyes)
k.export(out)
