# Larvitar sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/larvitar.py -- <the export's Larvitar.glb> <out.glb>
# From the references: a small green reptile standing upright, head and body one egg with no neck, the face carried
# forward over a short snout; a tall pointed crest rising from the crown and leaning a little back; angry red eyes
# under a brow sloping down to the snout, a black wedge pointing down under each; a short mouth line; a red diamond
# on the belly crossed by darker ribs; a dark diamond hole on each flank; stubby pointed arms forward and down; short
# legs, white nails; a fan of flat prongs stacked down the back for a tail.
#
# Written to be grown from (tyranitar.py imports it with sibling("larvitar")): the line's colours, its eye under a
# brow, its spikes and claws of flat-faced rock and its dark holes are the functions below; the calls that build
# Larvitar are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, smoothstep, unit, polygon, eye_look, args

# --- the line's colours ---------------------------------------------------------------------------------------------
GREEN = "#A6C27E"
RED, RIB = "#D0403A", "#962A28"
HOLE = "#2A2C32"
CLAW = "#F2F2EE"
EYE_RED = srgb("#D82A2A")
INK, WHITE = srgb("#15161C"), np.array([1.0, 1.0, 1.0])

DIAMOND = polygon([(0, 1), (1, 0), (0, -1), (-1, 0)])


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre."""
    d = unit(*d)
    t = 1.0 / math.sqrt(((d / np.array(r)) ** 2).sum())
    return np.array(c, float) + d * t


def spike(k, name, base, d, length, width, thick, up, color, bone, sink=0.02, mirror=False, surface="shell"):
    """
    A spike, prong or plate of rock: a flat-faced pyramid from base along d, its section a kite wide across `up` and
    thick along it, sunk into what it stands on by sink.
    """
    base, d = np.array(base, float), unit(*d)
    up = np.array(up, float); up = up - d * (up @ d); up /= np.linalg.norm(up)
    side = np.cross(d, up)
    b = base - d * sink
    pts = [b + side * width / 2, b - side * width / 2, b + up * thick / 2, b - up * thick / 2,
           base + d * length * 0.45 + side * width * 0.3, base + d * length * 0.45 - side * width * 0.3,
           base + d * length * 0.45 + up * thick * 0.3, base + d * length * 0.45 - up * thick * 0.3,
           base + d * length]
    k.add(name, hull(np.array(pts)), color, bone, fuse=False, mirror=mirror, surface=surface)


def claw(k, name, base, d, length, width, bone, color=CLAW, mirror=False):
    """A small white nail: a short stout spike, forward and down."""
    spike(k, name, base, d, length, width, width * 0.75, (0, 0, 1), color, bone, sink=0.012, mirror=mirror)


def brow_cut(BA, BB):
    """An eye's outline cut straight along the brow v = BA + BB u (u toward the back): its reach at each angle."""
    def cut(th):
        s = math.sin(th) - BB * math.cos(th)
        return min(1.0, BA / s) if s > 1e-6 else 1.0
    return cut


def angry_eye(cut, iris=EYE_RED, rim=0.78, iris_c=(-0.18, -0.08), iris_r=0.58, pupil_r=0.26):
    """An eye under a brow: a black rim along its outline, white, a red iris toward the snout, a black pupil."""
    def eye(u, v):
        reach = np.array([cut(t) for t in np.arctan2(v, u)])
        rho = np.sqrt(u * u + v * v) / reach
        c = np.tile(INK, (len(u), 1))
        c[rho < rim] = WHITE
        d = np.sqrt((u - iris_c[0]) ** 2 + (v - iris_c[1]) ** 2)
        c[(d < iris_r) & (rho < rim)] = iris
        c[(d < pupil_r) & (rho < rim)] = INK
        return c
    return eye


def eyes_under_brow(k, E, de, half, cut, lid, line=0.17, mark=None):
    """
    Lays the eyes for a state: the right as it is, the left with its picture and outline turned over. mark: (offset
    down, half sizes) of the black wedge pointing down under each eye, kept in every state.
    """
    m = np.array([-1, 1, 1.0])
    look_open = angry_eye(cut)
    def eyes(state):
        look = eye_look(state, look_open, lid, INK, line=line)
        k.decal(E, de, half, look, eps=0.006, rings=20, segs=56, outline=cut)
        k.decal(E * m, de * m, half, lambda u, v: look(-u, v), eps=0.006, rings=20, segs=56,
                outline=lambda th: cut(math.pi - th))
        if mark is not None:
            down, mh, ahead = mark
            right = np.cross((0, 0, 1.0), de); right /= np.linalg.norm(right)
            c = E + np.array([0, 0, -down]) - right * ahead
            k.decal(c, de, mh, INK, eps=0.005, outline=polygon([(-1, 1), (1, 1), (0.25, -1)]), rings=8, segs=48)
            k.decal(c * m, de * m, mh, INK, eps=0.005, outline=polygon([(-1, 1), (1, 1), (-0.25, -1)]), rings=8,
                    segs=48)
    return eyes


def ribbed(color, rib, ribs, width=0.075):
    """A plate's colour function: its own colour crossed by darker ribs at the heights v of `ribs`."""
    c0, c1 = srgb(color), srgb(rib)
    def look(u, v):
        c = np.tile(c0, (len(u), 1))
        for r in ribs:
            c[np.abs(v - r) < width] = c1
        return c
    return look


def ribbed_plate(k, center, normal, half, color, rib, ribs, width=0.08, outline=DIAMOND, up=(0, 0, 1), eps=0.004):
    """
    A diamond plate crossed by darker ribs: the plate a patch, each rib a patch of its own over it (a band of the
    diamond between v - width and v + width), so that its edges are crisp where a colour function steps at the rings.
    """
    center, n = np.array(center, float), unit(*normal)
    k.decal(center, n, half, color, up=up, eps=eps, outline=outline, rings=16, segs=64)
    right = np.cross(up, n); right /= np.linalg.norm(right)
    upv = np.cross(n, right)
    for r in ribs:
        lo, hi = r - width, r + width
        xl, xh = 0.95 * (1 - abs(lo)), 0.95 * (1 - abs(hi))
        sx, sy = half[0], half[1] * width
        pts = [(-xl / 1.0, -1), (xl, -1), (xh, 1), (-xh, 1)]
        k.decal(center + upv * r * half[1], n, (sx, sy), rib, up=up, eps=eps + 0.002, outline=polygon(pts), rings=6,
                segs=48)


def larvitar(src, out):
    k = Kit(src, coat="scales")

    # The body: one egg with the head, broad at the belly, the head forward at its top with no neck
    k.add("body", ell((0, 0.005, 0.17), (0.152, 0.138, 0.168)), GREEN, "body")
    k.add("belly", ell((0, -0.04, 0.135), (0.135, 0.125, 0.12)), GREEN, "body")
    H, HR = np.array([0.0, -0.04, 0.322]), np.array([0.122, 0.122, 0.115])
    k.add("head", ell(H, HR), GREEN, "head")
    k.add("snout", ell((0, -0.118, 0.292), (0.088, 0.072, 0.066), rot=(-8, 0, 0)), GREEN, "head")
    k.add("nape", ell((0, 0.04, 0.3), (0.12, 0.1, 0.12)), GREEN, "head")

    # The crest: one tall point rising from the crown, flattened a little from side to side, leaning back
    k.add("crest", blade([(0, -0.005, 0.39), (0, 0.008, 0.46), (0, 0.025, 0.53), (0, 0.045, 0.595)],
                         [0.072, 0.05, 0.03, 0.01], (1, 0, 0), 0.72), GREEN, "head")

    # The arms: stubby points out and forward and a little down, flattened top to bottom
    k.add("arm", blade([(0.1, -0.03, 0.232), (0.15, -0.075, 0.212), (0.195, -0.11, 0.195)], [0.05, 0.034, 0.012],
                       (0, 0, 1), 0.6), GREEN, "arm{s}", mirror=True)

    # The legs: thighs fused into the body, short, broad feet with two white nails each
    k.add("thigh", ell((0.085, 0.0, 0.1), (0.075, 0.08, 0.075)), GREEN, "leg{s}", mirror=True)
    k.add("foot", ell((0.092, -0.035, 0.034), (0.062, 0.08, 0.036)), GREEN, "leg{s}", mirror=True)
    for j, dx in enumerate((-0.02, 0.02)):
        claw(k, f"nail{j}", (0.092 + dx, -0.105, 0.026), (dx, -1, -0.3), 0.03, 0.03, "leg{s}", mirror=True)

    # The tail: a fan of flat prongs stacked down the lower back, pointing back and a little out to +X so that their
    # faces show from behind and from the three-quarter
    k.add("tailroot", ell((0, 0.1, 0.13), (0.09, 0.07, 0.09)), GREEN, "tail")
    for j, (z, ln, w) in enumerate(((0.238, 0.1, 0.095), (0.178, 0.13, 0.11), (0.118, 0.125, 0.105),
                                    (0.062, 0.1, 0.09))):
        x = 0.015 * (j - 1.5)
        spike(k, f"prong{j}", (x + 0.02, 0.145 - 0.01 * abs(j - 1.5), z), (0.45, 1, 0.05 - 0.08 * j), ln, w, 0.055,
              unit(-0.75, 0.3, 1), GREEN, "tail", sink=0.03)

    # The red diamond on the belly with its darker ribs, and the dark diamond hole on each flank
    ribbed_plate(k, (0, -0.15, 0.135), (0, -1, 0.05), (0.068, 0.092), RED, RIB, (-0.5, -0.12, 0.26))
    k.decal((0.128, -0.06, 0.15), unit(1, -0.5, 0.0), (0.019, 0.027), HOLE, eps=0.004, outline=DIAMOND, rings=8,
            segs=48, mirror=True)

    # The mouth: a short dark line across the foot of the snout, its corners a little down
    k.decal((0, -0.16, 0.258), unit(0, -1, -0.3), (0.042, 0.011),
            lambda u, v: np.where((np.abs(v - (-0.25 + 0.5 * (1 - u * u))) < 0.36)[:, None], INK, srgb(GREEN)),
            eps=0.004, rings=8, segs=48)

    # The eyes: on the face's sides, cut by the brow that slopes down to the snout, a black wedge under each
    de = unit(0.6, -0.76, 0.16)
    E = on_ell(H, HR, de)
    cut = brow_cut(0.26, 0.42)
    eyes = eyes_under_brow(k, E, de, (0.04, 0.037), cut, srgb(GREEN), mark=(0.054, (0.014, 0.025), 0.008))

    k.build(voxel=0.006, smooth=4)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    larvitar(*args())
