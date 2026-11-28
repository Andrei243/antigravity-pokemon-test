# Vidrut sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/vidrut.py -- <the export's Vidrut.glb> <out.glb>
#
# Carpathia No. 10147, an otter pup (plan 46, CP-dex-3b-river-and-caves-more.md, 147; picture 10147-Vidrut.jpg).
# The brief makes the pose its identity: floating on its back, a grey pebble clutched to its belly in both paws. So
# the trunk lies along Y on its back (belly up), the chest raised toward the head end (-Y) so that the game's front
# camera sees the belly, the pebble and the face; the hind paws stand up behind the pebble, soles turned to the head,
# and the thick tail runs back, swept toward +X, curling up into a blue two-lobed fin. Everything heavy stays on
# `body`; the head on `head`, the arms on the arms, the hind legs on `hind{s}`, the tail and its fin on `tail`.
# The masses, largest first (picture): the long curved trunk; the big round head (0.34 of the length) with a cream
# lower face and puffed cheeks; the tail with its fin (fin 0.7 of the head's width); the pebble (0.55 of the head's
# width); the raised hind paws with blue pads; the small arms; the fin-like blue ears.
# The face: eyes shut in contented arcs (the Open state is the arc), a small dark nose, a w-shaped smile, a few short
# whiskers (the picture's are long and fine; kept short and thick for the game's size).
# Skeleton: body (0, 0, 0.11), head (0, -0.16, 0.15), arms (+-0.09, -0.1, 0.16), hind (+-0.08, 0.12, 0.14),
# tail (0, 0.2, 0.08). Export 0.33 tall and 0.93 long; this is about 0.42 tall (the head raised as drawn).
#
# Vidroar (vidroar.py) grows from this file: its palette, on_ell, the fin ears, the fin, the nose and the whiskers.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, hull, srgb, inside, smoothstep, unit, eye_look, polygon, outward, args

# --- the line's palette ----------------------------------------------------------------------------------------------
BROWN = "#5C4A40"
DARK = "#46362E"
CREAM = "#F1E8CC"
FIN = "#8FA9D8"
FIN_DARK = "#6F88BC"
NOSE = "#4A322C"
INK = srgb("#2A1C18")
PEBBLE = "#8C8C86"
PEBBLE_DARK = "#6E6E6A"
WHISKER = "#3A2C26"


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r) along the direction d from its centre, and its normal there."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def board(ob):
    """The game board's check: what the model's corners show turned 31.5 degrees, against the square it is framed in."""
    V = np.array([v.co[:] for v in ob.data.vertices])
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    print("KIT: board: corners %.3f (0.853 w + 0.522 l), shows %.3f, in a square of %.3f"
          % (0.853 * w + 0.522 * l, np.ptp(proj), 1.3 * max(h, 0.85 * max(w, l))))


def fin_ear(k, base, out_d, front, size, thick, bone, colour=FIN, stub=BROWN, stub_r=None):
    """
    The line's ear: a short brown stub with a blue fin standing out of it, its edge scalloped, its face turned toward
    `front`. Laid on +X and mirrored.
    """
    out_d = unit(*out_d)
    front = np.array(front, float); front -= out_d * (front @ out_d); front /= np.linalg.norm(front)
    across = np.cross(front, out_d)            # in the fin's plane, square to out_d
    s = size
    outline = [(0.0, -0.42 * s), (0.45 * s, -0.36 * s), (0.62 * s, -0.22 * s), (0.78 * s, -0.12 * s), (1.0 * s, 0.02 * s),
               (0.74 * s, 0.1 * s), (0.6 * s, 0.24 * s), (0.36 * s, 0.34 * s), (0.0, 0.42 * s)]
    k.add("ear", slab(outline, base - out_d * 0.15 * s, out_d, across, thick), colour, bone, fuse=False, mirror=True,
          surface="skin")
    r = stub_r or 0.3 * s
    k.add("earstub", tube([base - out_d * 0.4 * r, base + out_d * 0.5 * r], [r, r * 0.8]), stub, bone, mirror=True)


def fin_rib(k, name, a, b, r, colour, bone):
    """A darker rib laid across a fin (a thin loose hull from a to b)."""
    a, b = np.array(a, float), np.array(b, float)
    d = b - a; d /= np.linalg.norm(d)
    s1 = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(s1) < 1e-3:
        s1 = np.array([1.0, 0, 0])
    s1 /= np.linalg.norm(s1); s2 = np.cross(d, s1)
    pts = [a + s1 * r, a - s1 * r, a + s2 * r, a - s2 * r, b + s1 * r * 0.5, b - s1 * r * 0.5, b + s2 * r * 0.5, b - s2 * r * 0.5]
    k.add(name, hull(pts), colour, bone, fuse=False, surface="scales")


def whiskers(k, roots, d_list, length, r, bone):
    """Short thick whiskers from the muzzle's sides (+X, mirrored)."""
    for i, (p, d) in enumerate(zip(roots, d_list)):
        p = np.array(p, float); d = unit(*d)
        k.add(f"whisker{i}", tube([p - d * 0.004, p + d * length * 0.6, p + d * length], [r, r * 0.85, r * 0.6]),
              WHISKER, bone, fuse=False, mirror=True, surface="hair")


def mouth_line(k, c, n, up, half_w, depth, r, shape=None, dots=11):
    """A mouth drawn as a row of small overlapping dark patches along a curve (a w by default): crisp at any size."""
    n = np.array(n, float); n /= np.linalg.norm(n)
    up = np.array(up, float); up = up - n * (up @ n); up /= np.linalg.norm(up)
    side = np.cross(up, n)
    shape = shape or (lambda t: 0.5 * np.cos(math.pi * t * 1.05) + 0.1)
    for t in np.linspace(-1, 1, dots):
        p = np.array(c, float) + side * t * half_w + up * shape(t) * depth
        k.decal(p, n, (r, r * 0.9), INK, up=up, eps=0.0032, rings=3, segs=14)


# --- Vidrut's frame --------------------------------------------------------------------------------------------------
FACE = unit(0, -0.84, 0.54)          # where the face looks: forward and up, the head propped up
HUP = unit(0, 0.54, 0.84)            # the head's own up
X = np.array([1.0, 0, 0])
HC = np.array([0.0, -0.212, 0.276])
HR = np.array([0.128, 0.118, 0.118])


def hd(a, f, u):
    """A direction in the head's frame: a toward +X, f along the face, u the head's up."""
    return a * X + f * FACE + u * HUP


MC = HC + FACE * 0.088 - HUP * 0.04
MR = np.array([0.072, 0.056, 0.052])


def vidrut(src, out):
    k = Kit(src, coat="fur")
    # The trunk on its back: the hips low, the chest raised toward the head
    k.add("hips", ell((0, 0.11, 0.105), (0.13, 0.15, 0.1)), BROWN, "body")
    k.add("chest", ell((0, -0.06, 0.162), (0.132, 0.15, 0.112), rot=(-30, 0, 0)), BROWN, "body")
    k.add("neck", ell((0, -0.16, 0.222), (0.105, 0.085, 0.095), rot=(-35, 0, 0)), BROWN, "head")
    k.add("head", ell(HC, HR), BROWN, "head")
    k.add("muzzle", ell(MC, MR), CREAM, "head")
    for i, (a, f, u, r) in enumerate(((0.62, 0.55, -0.35, 0.062), (0.5, 0.35, -0.7, 0.055))):
        p, n = on_ell(HC, HR, hd(a, f, u))
        k.add(f"cheek{i}", ell(p - n * 0.03, (r, r * 0.9, r * 0.85)), CREAM, "head", mirror=True)
    # The fin ears on the head's upper sides, faces turned to the front
    ep, en = on_ell(HC, HR, hd(0.78, -0.05, 0.62))
    fin_ear(k, ep - en * 0.006, unit(*(en * 0.7 + HUP * 0.45 + X * 0.2)), FACE + X * 0.2, 0.082, 0.013, "head")

    # The arms clutching the pebble to the chest, the paws on its sides
    OX = 0.05                       # the pebble and the paws a little toward +X, clear of the head
    for s, bone in ((1, "armR"), (-1, "armL")):
        k.add(f"arm{s}", tube([(0.105 * s, -0.1, 0.17), (0.125 * s + OX * 0.4, -0.085, 0.245),
                               (0.092 * s + OX, -0.066, 0.305)], [0.036, 0.034, 0.031]), BROWN, bone)
        k.add(f"paw{s}", ell((0.08 * s + OX, -0.06, 0.322), (0.036, 0.032, 0.03)), DARK, bone)
    # The pebble on the belly, a little tilted, standing up off the chest
    k.add("pebble", ell((OX, -0.04, 0.318), (0.078, 0.062, 0.048), rot=(-28, 0, 12)), PEBBLE, "body", fuse=False,
          surface="shell")
    k.add("pebble2", ell((OX + 0.012, -0.048, 0.35), (0.045, 0.034, 0.012), rot=(-28, 0, 12)), "#A2A29C", "body",
          fuse=False, surface="shell")

    # The hind legs raised, soles turned toward the head
    k.add("haunch", ell((0.085, 0.115, 0.15), (0.068, 0.08, 0.075)), BROWN, "hind{s}", mirror=True)
    k.add("shin", tube([(0.09, 0.125, 0.18), (0.1, 0.14, 0.235)], [0.04, 0.036]), BROWN, "hind{s}", mirror=True)
    k.add("foot", ell((0.1, 0.135, 0.275), (0.042, 0.026, 0.05), rot=(-40, 0, 0)), BROWN, "hind{s}", mirror=True)

    # The thick tail back and out to +X, curling up into its fin
    H = unit(0.42, 0.9, 0)
    T0 = np.array([0, 0.2, 0.085])
    tail_pts = [T0, T0 + H * 0.1 + np.array([0, 0, -0.02]), T0 + H * 0.2 + np.array([0, 0, -0.012]),
                T0 + H * 0.27 + np.array([0, 0, 0.04]), T0 + H * 0.31 + np.array([0, 0, 0.1])]
    k.add("tail", tube(tail_pts, [0.078, 0.062, 0.046, 0.034, 0.024]), BROWN, "tail")
    TIP = tail_pts[-1]
    # The fin: two lobes in the plane of the tail's run and the vertical, its faces to the front-three-quarter
    # camera and to the back one
    fin = [(-0.03, -0.03), (0.03, -0.045), (0.09, -0.03), (0.15, 0.01), (0.175, 0.05), (0.13, 0.065), (0.105, 0.075),
           (0.125, 0.12), (0.105, 0.165), (0.06, 0.15), (0.02, 0.1), (-0.02, 0.04)]
    k.add("fin", slab(fin, TIP + np.array([0, 0, -0.01]), H, (0, 0, 1), 0.016), FIN, "tail", fuse=False,
          surface="scales")
    nrm = np.cross(H, (0, 0, 1.0))
    for i, (a, b) in enumerate((((0.02, 0.0), (0.13, 0.04)), ((0.02, 0.03), (0.09, 0.13)))):
        pa = TIP + np.array([0, 0, -0.01]) + H * a[0] + np.array([0, 0, a[1]])
        pb = TIP + np.array([0, 0, -0.01]) + H * b[0] + np.array([0, 0, b[1]])
        for side in (1, -1):
            fin_rib(k, f"rib{i}{side}", pa + nrm * side * 0.008, pb + nrm * side * 0.008, 0.005, FIN_DARK, "tail")

    # Whiskers, short and few
    wp, wn = on_ell(MC, MR, hd(0.75, 0.55, 0.1))
    whiskers(k, [wp - wn * 0.004, wp - wn * 0.004 - HUP * 0.016],
             [wn + X * 0.5 + HUP * 0.15, wn + X * 0.5 - HUP * 0.12], 0.06, 0.0055, "head")
    np_, nn = on_ell(MC, MR, hd(0, 0.8, 0.5))
    k.add("nose", ell(np_ + nn * 0.004, (0.022, 0.016, 0.016), rot=(-33, 0, 0)), NOSE, "head", fuse=False,
          surface="skin")

    # Paint: the cream throat, chest and belly on the upper face of the trunk, and the lower face
    def belly(P):
        y, z = P[:, 1], P[:, 2]
        axis_z = 0.105 + np.clip(0.11 - y, 0, 0.4) * 0.42
        up = inside(axis_z + 0.05 - z, 0.006)
        return up * inside(np.abs(P[:, 0]) - 0.098, 0.006) * inside(y - 0.13, 0.008) * inside(-0.2 - y, 0.01)
    k.paint(lambda P: belly(P) * inside(np.abs(P[:, 0]) - 0.1), CREAM, on=["hips", "chest", "neck"])
    def lower_face(P):
        q = P - HC
        return inside(q @ HUP + 0.012, 0.005) * inside(-0.02 - q @ FACE, 0.006)
    k.paint(lower_face, CREAM, on=["head", "neck", "muzzle", "cheek"])
    k.paint(lambda P: inside(P[:, 2] - 0.255, 0.004) * inside(0.2 - P[:, 1]), DARK, on=["paw"])

    # The pads on the soles: a palm and three toes, blue
    sole = unit(0, -0.64, 0.77)
    for s in (1, -1):
        fc = np.array([0.1 * s, 0.135, 0.275])
        k.decal(fc - sole * 0.02, sole, (0.02, 0.017), FIN, eps=0.003, rings=6, segs=24)
        for j, du in enumerate((-1, 0, 1)):
            k.decal(fc + np.array([0.017 * du * s, 0, 0]) + unit(0, 0.6, 0.8) * 0.03 - sole * 0.02 * 0.0,
                    sole, (0.0085, 0.008), FIN, eps=0.003, rings=4, segs=16)
    # The smile: a small w under the nose
    mp, mn = on_ell(MC, MR, hd(0, 0.74, -0.62))
    mouth_line(k, mp, mn, HUP, 0.036, 0.02, 0.0058, dots=19)
    k.build(voxel=0.0046, smooth=4, band=0.006, wband=0.035)
    board(k.ob)

    # The eyes, shut in contented arcs above the cream: the arc is the Open state
    LID = srgb(BROWN)
    def arc(state):
        if state == "Fierce":
            def look(u, v):
                c = np.tile(LID, (len(u), 1))
                c[(np.abs(u) < 0.86) & (np.abs(v - (0.05 + 0.34 * u)) < 0.2)] = INK
                return c
            return look
        return eye_look("Smile" if state == "Open" else state, None, LID, INK, line=0.24)
    ep, en = on_ell(HC, HR, hd(0.42, 0.78, 0.4))
    def eyes(state):
        k.decal(ep, en, (0.036, 0.025), arc(state), up=HUP, eps=0.003, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    vidrut(*args())
