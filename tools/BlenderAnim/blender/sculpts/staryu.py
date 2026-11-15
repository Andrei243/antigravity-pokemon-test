# Staryu sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/staryu.py -- <the export's Staryu.glb> <out.glb>
#
# The design, from the references: a five-pointed star standing upright, one point up, sandy brown; each arm a solid
# tapering body with a ridge down its middle (a lit face and a shaded one), the star a quarter of an arm's width thick;
# in its middle a yellow dome with seven gold ribs running out from the gem over it (the one to the right longer), and
# a round red gem in a gold collar at the dome's middle. No eyes: the gem takes the eyes' five states, bright when
# open and dimmed when shut.
#
# Written to be grown from (Starmie is two such stars, violet, one behind the other and turned, with a faceted gem in
# a gold ring): the star, its frame, the ribs and the gem's plate and states are the functions below.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, lathe, srgb, unit, eye_look, args

BROWN, DOME, RIB, COLLAR = "#B07A44", "#E6C24C", "#F4DA66", "#D9AE3A"
GEM, GEM_LIGHT, GEM_DIM, GEM_INK, GLINT = "#D8304A", "#F0687A", "#8C2232", "#561420", "#FFF4F4"


# --- the star's frame: upright, facing the front, leaning back a little ---------------------------------------------
def frame(lean):
    """(across, up the star, out of its face): the star's plane leans back by lean degrees."""
    t = math.radians(lean)
    return np.array([1.0, 0, 0]), np.array([0, math.sin(t), math.cos(t)]), np.array([0, -math.cos(t), math.sin(t)])


def at(c, F, a, b, d=0.0):
    """A point in the star's frame: a across, b up its face, d out of its face (toward the front)."""
    return np.array(c, float) + a * F[0] + b * F[1] + d * F[2]


def star(k, name, c, F, R, ri, tf, tb, te=0.011, turn=0.0, colour=BROWN, tip=0.007):
    """Five arms, one point up turned by turn degrees: each a hull from the star's middle (tf in front, tb behind) out
    to its tip, its two sides ending in the valleys at ri on either side (te thick there)."""
    for i in range(5):
        p = math.radians(90 + turn + 72 * i)
        pts = [at(c, F, 0, 0, tf), at(c, F, 0, 0, -tb)]
        for q in (p - math.radians(36), p + math.radians(36)):
            for d in (te, -te):
                pts.append(at(c, F, ri * math.cos(q), ri * math.sin(q), d))
        for d in (tip, -tip):
            pts.append(at(c, F, R * math.cos(p), R * math.sin(p), d))
        k.add(f"{name}{i}", hull(pts), colour, "body", fuse=False, surface="shell")


def rib(k, name, c, F, angle, r0, r1, depth, width, thick, colour=RIB, segs=3):
    """A flat bar lying over the middle from r0 out to r1 along angle (degrees from across), following depth(r), the
    height of what it lies on out of the star's face: hulls a stretch at a time, so it bends with it."""
    a = math.radians(angle)
    d = np.array([math.cos(a), math.sin(a)])
    s = np.array([-d[1], d[0]])
    rs = np.linspace(r0, r1, segs + 1)
    for j in range(segs):
        pts = []
        for r in (rs[j], rs[j + 1]):
            top = depth(r) + thick * 0.5
            for w in (width, -width):
                for h in (top, top - thick * 1.6):
                    pts.append(at(c, F, *(d * r + s * w), h))
        k.add(f"{name}{j}", hull(pts), colour, "body", fuse=False, surface="metal")


# --- the gem: a loose glowing stone, and a thin fused plate over its face for the eyes' states ------------------------
def gem_plate(k, c, F, front, radius, thick):
    """The plate the eyes' patches lie on: a flat lens whose face is at front out of the star's face."""
    t = math.degrees(math.atan2(F[1][1], F[1][2]))
    k.add("gemplate", ell(at(c, F, 0, 0, front - thick), (radius, thick, radius), rot=(-t, 0, 0), sub=5), GEM, "body",
          surface="shell")


def gem_look(open_look, state, lit=GEM, dim=GEM_DIM, ink=GEM_INK):
    """The gem's face in the eyes' five states: open as it is, fierce hotter, the shut ones dimmed with a faint line."""
    if state == "Open":
        return open_look
    if state == "Fierce":
        def hot(u, v):
            c = open_look(u, v)
            core = (u * u + v * v) < 0.3
            c[core] = c[core] * 0.6 + srgb("#FF8A5A") * 0.4
            return c
        return hot
    return eye_look(state, open_look, srgb(dim), srgb(ink), line=0.1)


def round_gem_open(u, v):
    """A round red stone: lighter toward its lower right, a glint high on its left."""
    c = np.tile(srgb(GEM), (len(u), 1))
    c[((u - 0.35) ** 2 + (v + 0.35) ** 2) < 0.18] = srgb(GEM_LIGHT)
    c[((u + 0.38) / 0.24) ** 2 + ((v - 0.38) / 0.18) ** 2 < 1] = srgb(GLINT)
    c[((u - 0.42) / 0.09) ** 2 + ((v + 0.46) / 0.09) ** 2 < 1] = srgb(GLINT)
    return c


# --- Staryu ------------------------------------------------------------------------------------------------------------
C = np.array([0.0, 0.0, 0.408])          # the star's middle, on the body bone
LEAN = 12
F = frame(LEAN)
R, RI = 0.39, 0.175                      # the points' reach and the valleys'
TF, TB = 0.07, 0.045                     # the middle's thickness in front of and behind the star's plane
DOME_C, DOME_R = 0.04, (0.16, 0.065, 0.16)      # the dome: its middle out of the face, its radii (across, out, up)
GEM_C, GEM_R = 0.075, 0.085             # the gem: its middle out of the face, its radius
PLATE_R, PLATE_T = 0.05, 0.01
RIBS = [(0, 0.215), (55, 0.175), (125, 0.175), (180, 0.175), (235, 0.175), (270, 0.15), (305, 0.175)]


def dome_depth(r):
    """How far out of the star's face the dome's surface stands at r from the middle."""
    q = min(r / DOME_R[0], 0.995)
    return max(DOME_C + DOME_R[1] * math.sqrt(1 - q * q), TF * (1 - r / R) * 0.85 + 0.012)


def staryu(k):
    star(k, "arm", C, F, R, RI, TF, TB)
    t = math.degrees(math.atan2(F[1][1], F[1][2]))
    k.add("dome", ell(at(C, F, 0, 0, DOME_C), DOME_R, rot=(-t, 0, 0), sub=5), DOME, "body")
    for i, (ang, reach) in enumerate(RIBS):
        rib(k, f"rib{i}", C, F, ang, 0.085, reach, dome_depth, 0.015, 0.016)
    k.add("gem", ell(at(C, F, 0, 0, GEM_C), (GEM_R,) * 3, sub=4), GEM, "body", fuse=False, glow=GEM, surface="shell")
    k.add("collar", lathe(at(C, F, 0, 0, 0), F[2], [(0, 0.06), (0.097, 0.06), (0.103, 0.093), (0.095, 0.108),
                                                     (0.08, 0.106), (0, 0.106)], seg=48), COLLAR, "body", fuse=False,
          surface="metal")
    gem_plate(k, C, F, GEM_C + GEM_R + 0.006, PLATE_R, PLATE_T)


def eyes(k, state):
    front = GEM_C + GEM_R + 0.006
    k.decal(at(C, F, 0, 0, front), F[2], (0.044, 0.044), gem_look(round_gem_open, state), up=F[1], eps=0.0025,
            rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    staryu(k)
    k.build(voxel=0.004, smooth=3)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
