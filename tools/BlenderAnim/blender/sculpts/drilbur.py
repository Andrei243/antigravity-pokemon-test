# Drilbur sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/drilbur.py -- <the export's Drilbur.glb> <out.glb>
# From the references: a grey-brown peg of a body, its head the domed top with no neck, wider toward its foot; slate
# blue bands round it slanting across (the lower body blue with a brown stripe through it, a blue band over the chest);
# a long flat white snout jutting forward and a little up out of the face, a round pink nose on its tip, a white mask
# round the snout's root; round white eyes with a small black pupil and a dark rim on the sides of the snout's root; an
# open mouth under the snout, red with a pink tongue; thick short arms held forward ending in three great white blades
# that close into a drill's cone; small dark stubby feet.
# Written to be grown from (excadrill.py): the drill, the eye and the colours are functions and constants.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args, hull, axes, boxy

BROWN, FEET, BLUE, WHITE, PINK = "#6A5E5A", "#4E4440", "#5664AC", "#F4F4F2", "#EE8A9A"
STEEL, STEEL_DARK, INK = "#ECECEE", "#BCBCC6", "#1A1618"
MOUTH, TONGUE = "#7A1E2E", "#E8707E"


# --- pieces of the line --------------------------------------------------------------------------------------------
def drill(k, name, base, d, length, radius, bone, colours=(STEEL, STEEL, STEEL_DARK), spin=0.0, teeth=0.0, smooth=False):
    """
    Three solid blades closing into a cone from base along d (a hand's claws): each a flat-faced hull of a third of
    the cone, its root radius wide. teeth: a sawtooth on each blade's back edge, that share of the radius out
    (Excadrill's serrated claws).
    """
    d = unit(*d)
    a, b = axes(d)
    base = np.array(base, float)
    for i in range(3):
        th = math.radians(spin + 120 * i)
        def rad(off, th=th):
            t = th + math.radians(off)
            return math.cos(t) * a + math.sin(t) * b
        r = rad(0)
        tip = base + d * length + r * 0.006
        pts = [base + r * radius * 0.12 - d * radius * 0.2, base + r * radius * 1.0,
               base + rad(52) * radius * 0.86, base + rad(-52) * radius * 0.86,
               base + d * length * 0.45 + r * radius * 0.8,
               base + d * length * 0.45 + rad(40) * radius * 0.55, base + d * length * 0.45 + rad(-40) * radius * 0.55,
               base + d * length * 0.45 + r * radius * 0.1,
               base + d * length * 0.8 + r * radius * 0.35,
               tip]
        if smooth:
            # more sides: a blade's back rounded by rings of points along its length, so its facets are small
            pts = [base + r * radius * 0.12 - d * radius * 0.2, tip]
            for s_, w_ in ((0.0, 1.0), (0.2, 0.97), (0.4, 0.86), (0.6, 0.68), (0.78, 0.47), (0.92, 0.24)):
                at = base + d * length * s_
                for off in (-56, -40, -22, 0, 22, 40, 56):
                    rr = w_ * radius * math.cos(math.radians(off) * 0.9)
                    pts.append(at + rad(off) * rr)
                pts.append(at + r * radius * 0.08)
        k.add(f"{name}{i}", hull(pts, merge=1.0) if smooth else hull(pts),colours[i % len(colours)], bone, fuse=False, surface="metal")
        if teeth:
            for j, at in enumerate((0.22, 0.55)):
                q = base + d * length * at + r * radius * (0.92 - 0.55 * at)
                pts = [q - d * length * 0.14 + rad(90) * 0.016, q - d * length * 0.14 + rad(-90) * 0.016,
                       q + d * length * 0.08 + rad(90) * 0.012, q + d * length * 0.08 + rad(-90) * 0.012,
                       q - r * radius * 0.25, q - d * length * 0.1 + r * radius * teeth]
                k.add(f"{name}{i}t{j}", hull(pts), colours[i % len(colours)], bone, fuse=False, surface="metal")


def round_eye(white=WHITE, ink=INK, rim=0.83):
    """A round white eye with a dark rim (its pupil is a patch of its own: pupil())."""
    W, K = srgb(white), srgb(ink)
    def look(u, v):
        c = np.tile(W, (len(u), 1))
        c[u * u + v * v > rim * rim] = K
        return c
    return look


def pupil(k, centre, normal, half, state, at=(-0.12, 0.0), size=0.4, ink=INK, up=(0, 0, 1)):
    """
    A solid round pupil laid over the eye's patch (a small dot inside a patch comes out a star), in the open and
    fierce states only; in the fierce one it sits lower, under the lid's slant. at and size in the eye's u, v.
    """
    if state not in ("Open", "Fierce"):
        return
    n = unit(*normal)
    right = np.cross(up, n); right /= np.linalg.norm(right)
    upv = np.cross(n, right)
    u, v = at if state == "Open" else (at[0], at[1] - 0.26)
    s = size if state == "Open" else size * 0.9
    c = np.array(centre, float) + right * u * half[0] + upv * v * half[1]
    k.decal(c, n, (s * half[0], s * half[1] * 1.08), ink, eps=0.0068, rings=10, segs=48, mirror=True, up=up)


def mouth(red=MOUTH, tongue=TONGUE, edge="#4A1018", ground=WHITE, top=0.25):
    """An open grin: a half moon flat along its top, red, a pink tongue in its lower part, a thin dark red edge."""
    R, T, K, G = srgb(red), srgb(tongue), srgb(edge), srgb(ground)
    def look(u, v):
        c = np.tile(R, (len(u), 1))
        c[(u / 0.6) ** 2 + ((v + 0.62) / 0.42) ** 2 < 1] = T
        r = np.sqrt(u * u + v * v)
        c[(r > 0.86) | (np.abs(v - top) < 0.09)] = K
        c[v > top + 0.09] = G
        return c
    return look


# --- Drilbur -------------------------------------------------------------------------------------------------------
BODY_C, BODY_R = np.array([0.0, 0.0, 0.17]), np.array([0.105, 0.095, 0.15])
HEAD_C, HEAD_R = np.array([0.0, -0.005, 0.255]), np.array([0.098, 0.092, 0.085])


def wider_below(V):
    V = V.copy()
    s = 1 + 0.1 * np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= s; V[:, 1] *= s
    return V


def spade(V):
    """A flat snout, squared off and a little wider toward its tip (-Y)."""
    V = boxy(2.4)(V)
    t = np.clip(-V[:, 1], 0, 1)
    V[:, 0] *= 1.12 - 0.42 * t
    return V


def build(src, out):
    k = Kit(src, coat="fur")
    k.add("body", ell(BODY_C, BODY_R, deform=wider_below), BROWN, "body")
    k.add("head", ell(HEAD_C, HEAD_R), BROWN, "head")
    # the snout: long, flat, white, forward and a little up; the pink nose on its tip
    k.add("snout", ell((0, -0.135, 0.312), (0.054, 0.084, 0.03), rot=(-14, 0, 0), deform=spade), WHITE, "head")
    k.add("nose", ell((0, -0.212, 0.338), (0.036, 0.026, 0.028)), PINK, "head", fuse=False, surface="skin")

    # arms: short and thick, held forward and a little out, a drill of three blades on each
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"arm{side}", tube(np.array([(0.07, -0.02, 0.21), (0.115, -0.07, 0.195), (0.13, -0.12, 0.18)]) * m,
                                 [0.046, 0.043, 0.041]), BROWN, f"arm{side}")
        drill(k, f"claw{side}", np.array((0.132, -0.14, 0.178)) * m, (-0.04 * s, -1, 0.32), 0.2, 0.066,
              f"arm{side}", spin=90, smooth=True)
    # feet: small dark stubs under the body's front
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"leg{side}", tube(np.array([(0.05, 0.0, 0.08), (0.05, -0.02, 0.035)]) * m, [0.045, 0.04]), BROWN,
              f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.055, -0.04, 0.022)) * m, (0.044, 0.058, 0.024)), FEET, f"leg{side}")

    # --- the blue bands, slanting across the body --------------------------------------------------------------
    def lower(Q):
        x, z = Q[:, 0], Q[:, 2]
        blue = inside(z - (0.115 + 0.12 * x), 0.002)
        stripe = inside(np.abs(z - (0.07 + 0.18 * x)) - 0.017, 0.002)
        return blue * (1 - stripe)
    k.paint(lower, BLUE, on=["body"])
    k.paint(lambda Q: inside(np.abs(Q[:, 2] - (0.165 - 0.3 * Q[:, 0])) - 0.026, 0.002), BLUE, on=["body"])
    # the right arm blue toward the drill, a blue spot on the left one's outer side
    k.paint(lambda Q: inside(Q[:, 1] + 0.095, 0.002) * (Q[:, 0] > 0), BLUE, on=["armR"])
    k.paint(lambda Q: inside(np.linalg.norm(Q - np.array([-0.157, -0.07, 0.198]), axis=1) - 0.026, 0.002), BLUE,
            on=["armL"])

    # --- the white mask round the snout's root ------------------------------------------------------------------
    def mask(Q):
        x, y, z = Q[:, 0], Q[:, 1], Q[:, 2]
        e = (x / 0.098) ** 2 + ((z - 0.27) / 0.072) ** 2
        return inside(np.sqrt(e) - 1, 0.06) * inside(y + 0.04, 0.004)
    k.paint(mask, WHITE, on=["head", "body"])

    # the open mouth under the snout
    k.decal((0, -0.096, 0.242), unit(0, -1, -0.1), (0.046, 0.034), mouth(), eps=0.004, rings=14, segs=48)

    # --- the eyes: round, white, a black pupil, on the sides of the snout's root ---------------------------------
    EC = np.array([0.066, -0.054, 0.305])
    de = unit(0.68, -0.68, 0.2)
    lid = srgb(WHITE)
    def eyes(state):
        k.decal(EC, de, (0.029, 0.029), eye_look(state, round_eye(), lid, srgb(INK), line=0.16), eps=0.005,
                rings=20, segs=56, mirror=True)
        pupil(k, EC, de, (0.029, 0.029), state)

    k.build(voxel=0.0055, smooth=4, band=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    build(*args())
