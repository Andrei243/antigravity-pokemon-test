# Bounsweet sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/bounsweet.py -- <the export's Bounsweet.glb> <out.glb>
#
# From the references: a little fruit. A magenta dome, rounded on top and widest at its rim, over a white underside of
# soft puffs; two tiny magenta claws of feet under it; a green cap on the dome's top with a thick green stalk curling
# over to its left; two big green leaves out of the cap like the halves of an umbrella, domed and rounded at their
# ends, falling out to the sides; on the dome's front small upright yellow eyes set wide and a big open mouth, flat on
# top and round below, a pink tongue in it.
# (The task called the body pink and white with a calyx: the pictures have a deep magenta dome, a lime-green cap with
# a curling stalk, and two umbrella leaves.)
#
# Written to be grown from (steenee.py and tsareena.py import it with sibling("bounsweet")): the line's colours, the
# rounded leaf, the cap, the stalk, the puffs and the open mouth are the functions below; Bounsweet's own calls are
# under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, lathe, srgb, unit, eye_look, args, outward

# --- the line's colours -------------------------------------------------------------------------------------------
MAGENTA, MAGENTA_DARK = "#D8287A", "#9C1452"
WHITE = "#F6F4F2"
GREEN, GREEN_IN = "#A6D86A", "#86BE52"
YELLOW, YELLOW_RIM = "#F4E43A", "#A88A1C"
MOUTH_RIM, MOUTH, TONGUE = "#7E1030", "#E8606E", "#F7A0A4"
INK = "#4A0A24"

# --- Bounsweet's frame (joints: root (0, 0, 0), body (0, 0, 0.12); 0.42 tall) ---------------------------------------
C = np.array([0.0, 0.0, 0.134])          # the dome's middle (its rim 0.07 below, its top 0.145 above)
R = np.array([0.098, 0.093, 0.12])
CAP = np.array([0.0, -0.004, 0.252])     # where the cap sits on the dome's top
EYE = np.array([0.064, -0.084, 0.19])    # the right eye's place on the dome, and the way it looks
EYE_N = unit(0.56, -0.82, 0.12)
MOUTH_AT = np.array([0.0, -0.104, 0.152])


def dome(V):
    """The unit sphere as a fruit's dome like a bell: round on top, its sides falling nearly straight to a broad rim
    low down, and a flat underside inside the rim."""
    V = V.copy()
    z = V[:, 2]
    lo = np.clip(-z, 0, 1)
    s = (1 + 0.75 * lo ** 2) * (1 + 0.06 * (1 - z * z)) * (1 + 0.06 * np.clip(z, 0, 1))
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.where(z < 0, z * 0.55, z * 1.14)
    return V


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre, and its normal."""
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def round_leaf(base, tip, width, arch, facing, cup=0.35, thick=0.014, end=0.0, full=0.0):
    """
    A broad leaf rounded at its end like a spoon or half an umbrella, from base to tip: widest past its middle, arched
    up toward facing along its length and its edges let down, so it is a dome over its own underside. end lets the tip
    curl down; full widens it toward its end so the end is blunt. A flat ellipsoid bent so (a sheet's thin end folded into rags), wound outward: a loose piece.
    """
    base, tip = np.array(base, float), np.array(tip, float)
    L = np.linalg.norm(tip - base)
    d = (tip - base) / L
    n = np.array(facing, float); n = n - d * np.dot(n, d); n /= np.linalg.norm(n)
    s = np.cross(n, d)                                       # (d, s, n) right-handed
    V, F = ell((0, 0, 0), (1, 1, 1), sub=4)
    t = (V[:, 0] + 1) / 2                                    # 0 at the root, 1 at the end
    y = V[:, 1] * width * (0.78 + 0.32 * np.sin(math.pi * np.clip(t * 0.8 + 0.1, 0, 1)) + full * t ** 2)
    x = V[:, 1]
    z = V[:, 2] * thick / 2 + arch * np.sin(math.pi * t * 0.85) - cup * width * x * x - end * t ** 3
    P = base[None] + d[None] * (L * t)[:, None] + s[None] * y[:, None] + n[None] * z[:, None]
    return outward((P, F))


def cap(k, c, r, h, bone, colour=GREEN, name="cap"):
    """A green cap sitting on a fruit's top: a squat dome with a rounded rim a little wider than its foot."""
    prof = [(0, h), (r * 0.45, h * 0.94), (r * 0.8, h * 0.72), (r, h * 0.4), (r * 0.97, h * 0.12), (r * 0.82, 0),
            (0, -h * 0.05)]
    k.add(name, lathe(c, (0, 0, 1), prof, seg=48), colour, bone, fuse=False, surface="leaf")


def stalk(k, pts, radii, bone, colour=GREEN, name="stalk"):
    """The stalk out of the cap: a thick round tube curling over, a little swollen at its tip."""
    k.add(name, outward(tube(pts, radii)), colour, bone, fuse=False, surface="leaf")


def puffs(k, c, ring, n, r, z, bone, colour=WHITE, name="puff", middle=None, phase=0.0):
    """A ring of n soft white puffs (loose, so their edge with the fruit's dome is the dome's own) round c at radius
    ring, each of radii r, and a middle one under them."""
    for i in range(n):
        a = 2 * math.pi * (i + phase) / n
        p = np.array([c[0] + math.sin(a) * ring, c[1] - math.cos(a) * ring, z])
        k.add(f"{name}{i}", outward(ell(p, r, rot=(0, 0, -math.degrees(a)), sub=3)), colour, bone, fuse=False)
    if middle is not None:
        k.add(f"{name}mid", outward(ell((c[0], c[1], z), middle, sub=3)), colour, bone, fuse=False)


def open_mouth(k, c, n, half, up=(0, 0, 1), top=0.5):
    """A big open mouth: flat along its top, round below, a dark red rim, pink inside and a tongue low in it."""
    CR, CM, CT = srgb(MOUTH_RIM), srgb(MOUTH), srgb(TONGUE)
    def look(u, v):
        c_ = np.tile(CR, (len(u), 1))
        inner = (u / 0.84) ** 2 + ((v - 0.06) / 0.86) ** 2 < 1
        c_[inner & (v < top - 0.16)] = CM
        c_[inner & ((u / 0.56) ** 2 + ((v + 0.62) / 0.42) ** 2 < 1)] = CT
        return c_
    def shape(th):
        s_ = math.sin(th)
        return min(1.0, top / s_) if s_ > 0 else 1.0
    k.decal(c, n, half, look, up=up, eps=0.003, outline=shape, rings=14, segs=56)


def yellow_eye(u, v):
    """Bounsweet's small upright yellow eye, a darker gold rim round it."""
    c = np.tile(srgb(YELLOW), (len(u), 1))
    c[u * u + v * v > 0.8 ** 2] = srgb(YELLOW_RIM)
    return c


def bounsweet(src, out):
    k = Kit(src, coat="scales")

    # --- the dome, fused alone: the face's patches lie on it
    k.add("dome", ell(C, R, deform=dome, sub=5), MAGENTA, "body")

    # --- the white underside: a ring of puffs and one in the middle, loose under the dome's rim
    puffs(k, C, 0.058, 9, (0.042, 0.04, 0.034), 0.05, "body", middle=(0.08, 0.078, 0.036), phase=0.5)
    # two tiny magenta claws pointing down and a little out from under the puffs
    for sx in (1, -1):
        root = np.array([0.044 * sx, -0.02, 0.034])
        d = unit(0.35 * sx, -0.15, -1)
        k.add("claw" + ("R" if sx > 0 else "L"),
              lathe(root, d, [(0, -0.004), (0.016, 0.0), (0.013, 0.012), (0, 0.03)], seg=24), MAGENTA, "body",
              fuse=False, surface="shell")

    # --- the cap, the stalk curling over to the left (-X), and the two umbrella leaves
    cap(k, CAP, 0.068, 0.07, "body")
    stalk(k, [(0.0, 0.004, 0.31), (-0.004, 0.008, 0.35), (-0.018, 0.01, 0.384), (-0.042, 0.01, 0.4),
              (-0.062, 0.008, 0.391), (-0.07, 0.006, 0.371)], [0.0135, 0.013, 0.0125, 0.012, 0.0125, 0.0135], "body")
    for sx, tipz in ((1, 0.22), (-1, 0.2)):
        k.add("leaf" + ("R" if sx > 0 else "L"),
              round_leaf((0.03 * sx, 0.002, 0.298), (0.215 * sx, 0.006, tipz), 0.058, 0.034, (0.25 * sx, -0.3, 1.0),
                         cup=0.8, thick=0.03, full=0.6), GREEN, "body", fuse=False, surface="leaf")

    # --- the face: the open mouth (in every state, so before the body's build)
    open_mouth(k, MOUTH_AT, (0, -1, -0.04), (0.04, 0.032))

    k.build(voxel=0.004, smooth=4, band=0.005)

    # small upright yellow eyes set wide
    LID, INK_ = srgb(MAGENTA), srgb(INK)
    def eyes(state):
        k.decal(EYE, EYE_N, (0.0125, 0.018), eye_look(state, yellow_eye, LID, INK_, line=0.17), eps=0.003, rings=20,
                segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    bounsweet(*args())
