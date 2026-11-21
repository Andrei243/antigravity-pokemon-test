# Alolan Geodude sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/geodudealola.py -- <the export's Geodude-Alola.glb> <out.glb>
# Geodude's frame (geodude.py, which builds as it is read, so its way is copied here and scaled to this skeleton:
# the body bone at 0.197 where Geodude's is at 0.218): a pale blue-grey boulder with two great black brows of iron
# sand slanting to a scowl, black rods of iron sand bristling off its crown, square arms raised to blocky fists.
# The line's base: Graveler-Alola and Golem-Alola take the colours and the helpers below through sibling().
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, boxy, facets, eye_look, args

# --- the line's colours -------------------------------------------------------------------------------------------
GREY, DARK, CREASE = "#BABEC8", "#9A9EAA", "#6C707C"      # the Alolan stone: paler and bluer than Geodude's
BLACK = "#2C2E36"                                           # iron sand
AMBER = "#F6A12A"                                           # the crystals (Graveler, Golem)
SCLERA, PUPIL = srgb("#F6F6F2"), srgb("#14141A")


# --- the line's helpers (each takes the kit and its bone: no bone's name is written in) ----------------------------
def frame(d):
    """A right-handed frame (a, b, d) round a direction."""
    d = np.asarray(d, float); d = d / np.linalg.norm(d)
    a = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a)
    return a, np.cross(d, a), d


def rod_points(root, d, length, width, tip=0.7, twist=0.0):
    """The corners of one rod of iron sand: a square prism from root along d, tapering to tip of its width."""
    a, b, d = frame(d)
    c, s = math.cos(twist), math.sin(twist)
    a, b = a * c + b * s, b * c - a * s
    root = np.asarray(root, float)
    pts = []
    for t, w in ((0.0, width), (length, width * tip)):
        for sa, sb in ((1, 1), (-1, 1), (-1, -1), (1, -1)):
            pts.append(root + d * t + (a * sa + b * sb) * w / 2)
    return np.array(pts)


def join(shapes):
    """Several closed shapes as one piece (one material and one winding check for all of them)."""
    V, F, off = [], [], 0
    for v, f in shapes:
        V.append(np.asarray(v, float)); F.extend([[i + off for i in g] for g in f]); off += len(v)
    return np.vstack(V), F


def iron_sand(k, name, rods, bone, color=BLACK, mirror=False):
    """A tuft of black rods (root, direction, length, width, twist) as one loose piece of hulls."""
    k.add(name, join([hull(rod_points(r, d, l, w, twist=tw)) for r, d, l, w, tw in rods]), color, bone, fuse=False,
          mirror=mirror, surface="shell")


def brow_block(k, name, inner, outer, height, depth, normal, bone, color=BLACK, mirror=False, droop=0.35):
    """
    A heavy black brow of iron sand: a block from its inner end to its outer, height tall and depth thick along
    normal (out of the face), its inner end lower and blunter (the scowl), its front face bevelled down.
    """
    inner, outer, n = np.asarray(inner, float), np.asarray(outer, float), unit(*normal)
    along = outer - inner; along /= np.linalg.norm(along)
    up = np.cross(n, along); up = up if up[2] > 0 else -up
    pts = []
    for p, h in ((inner, height * (1 + droop)), (outer, height)):
        for t in (-0.45, 0.55):                       # the back half sunk into the stone, the front half standing out
            pts.append(p + n * t * depth + up * h / 2)
            pts.append(p + n * t * depth - up * h / 2 * (1.0 if t < 0 else 0.7))
    k.add(name, hull(np.array(pts)), color, bone, fuse=False, mirror=mirror, surface="shell")


def crystal_points(at, normal, r, h, turn=0.0):
    """A six-sided amber crystal set into the rock at at, standing h out along normal: a flat hexagonal gem."""
    a, b, n = frame(normal)
    pts = []
    for z, rr in ((-h, r), (h * 0.55, r), (h, r * 0.62)):
        for i in range(6):
            t = turn + i * math.pi / 3
            pts.append(np.asarray(at, float) + n * z + rr * (math.cos(t) * a + math.sin(t) * b))
    return np.array(pts)


def crystals(k, name, gems, bone, mirror=False):
    """Amber crystals (at, normal, radius, height) as one loose glowing piece."""
    k.add(name, join([hull(crystal_points(*g)) for g in gems]), AMBER, bone, fuse=False, mirror=mirror,
          glow=AMBER, surface="shell")


def eye_open(lid, cut=0.38, slope=0.42, pupil_x=-0.28):
    """
    The line's eye: white, a solid black pupil toward the inner corner, a rim of ink a sixth of its half size, the top
    cut by a straight line slanting down to the middle (the brow's lower edge) in the lid's colour.
    """
    def look(u, v):
        c = np.tile(SCLERA, (len(u), 1))
        c[((u - pupil_x) / 0.32) ** 2 + ((v + 0.05) / 0.56) ** 2 < 1] = PUPIL
        c[u * u + v * v > 0.7] = PUPIL
        edge = cut + slope * u
        c[v > edge] = lid
        return c
    return look


def stone_body(k, name, c, r, seed, color, bone, n=15, keep_front=0.55, depth=(0.82, 0.94)):
    k.add(name, ell(c, r, deform=facets(n, 1.0, seed, keep_front=keep_front, depth=depth)), color, bone)


# --- Geodude-Alola ---------------------------------------------------------------------------------------------------
S = 0.197 / 0.218                       # this skeleton against Geodude's
C = np.array([0.0, 0.0, 0.197])         # the boulder's middle, on the body bone
R = np.array([0.198, 0.162, 0.176])     # its radii: rounder than Geodude's (the pictures), a tenth larger for the face


def geodude_alola(src, dest):
    k = Kit(src, coat="shell")
    stone_body(k, "body", C, R, 74, GREY, "body")
    # a few lumps of stone standing off its back and top, as on Geodude
    for i, (d, r) in enumerate([((0.45, 0.55, 0.7), 0.05), ((-0.55, 0.5, 0.6), 0.048), ((0.0, 0.9, 0.3), 0.055),
                                ((0.75, 0.55, -0.2), 0.045), ((-0.7, 0.6, -0.25), 0.045)]):
        dd = unit(*d)
        k.add(f"lump{i}", ell(C + dd * R * 0.86, (r, r * 0.8, r * 0.85), rot=(20 * i, 35 * i, 0),
                              deform=facets(7, 1.0, 100 + i, depth=(0.75, 0.9))), GREY, "body")
    # The cheeks and jaw jutting forward under the eyes: the mouth's crease between them
    k.add("jaw", ell(np.array([0, -0.122, 0.148]), (0.115, 0.058, 0.052), deform=facets(10, 0.5, 9)), GREY, "body")
    # a ridge under each brow, the stone the brow sits on
    k.add("ridge", ell(np.array([0.064, -0.138, 0.246]), (0.07, 0.036, 0.026), rot=(-14, -16, 8)), GREY, "body",
          mirror=True)

    # The arms: square stone, a thick upper arm out to a low elbow, a forearm rising to a blocky fist held up beside
    # the head (the pictures' pose; the fists ahead of the shoulders, not out in a T)
    SH = np.array([0.135, -0.005, 0.197]); EL = np.array([0.272, -0.04, 0.15]); WR = np.array([0.305, -0.07, 0.27])
    k.add("upper", tube([SH, (SH + EL) / 2 + [0, 0, 0.016], EL], [0.034, 0.05, 0.046]), GREY, "arm{s}", mirror=True)
    k.add("biceps", ell((0.21, -0.028, 0.196), (0.058, 0.046, 0.044), rot=(0, 18, 0), deform=boxy(3)), GREY, "arm{s}",
          mirror=True)
    k.add("fore", tube([EL, (EL + WR) / 2, WR], [0.046, 0.05, 0.048]), GREY, "arm{s}", mirror=True)
    k.add("elbow", ell(EL + [0.01, 0.004, -0.01], (0.042, 0.042, 0.038), deform=boxy(3)), GREY, "arm{s}", mirror=True)
    FC = np.array([0.316, -0.08, 0.345])
    k.add("fist", ell(FC, (0.066, 0.066, 0.068), deform=boxy(3.6)), GREY, "arm{s}", mirror=True)
    # the fingers a squared block across the front of the fist, the thumb a block over them
    k.add("fingers", ell(FC + [-0.004, -0.05, 0.012], (0.06, 0.03, 0.05), deform=boxy(3.6)), GREY, "arm{s}",
          mirror=True)
    k.add("thumb", ell(FC + [-0.03, -0.074, -0.022], (0.04, 0.022, 0.02), rot=(0, 0, 10), deform=boxy(3.6)), GREY,
          "arm{s}", mirror=True)
    # a crease of shade between the fingers and the palm
    k.paint(lambda P: inside(np.abs(P[:, 1] - (FC[1] - 0.03)) - 0.005, 0.003) * inside(np.abs(np.abs(P[:, 0]) - FC[0]) - 0.07)
            * inside(np.abs(P[:, 2] - FC[2]) - 0.07), DARK, on=["fingers", "fist"])

    # The shade under the brows
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.232) - 0.012, 0.01) * smoothstep(-0.1, -0.14, P[:, 1]) *
            inside(np.abs(P[:, 0]) - 0.12), DARK, on=["body", "ridge"])

    # The two great brows of iron sand, slanting down to a notch over the nose: the scowl
    for s, nm in ((1, "browR"), (-1, "browL")):
        brow_block(k, nm, (0.012 * s, -0.16, 0.238), (0.128 * s, -0.12, 0.28), 0.048, 0.058,
                   (0.3 * s, -0.95, 0.1), "body")
    # The iron sand standing up off its crown and brow: short black rods, thick enough to read
    rods = []
    for root, d, l in [((0.0, -0.13, 0.31), (0.0, -0.55, 0.85), 0.05), ((-0.045, -0.115, 0.33), (-0.25, -0.5, 0.85), 0.056),
                       ((0.045, -0.118, 0.33), (0.25, -0.5, 0.85), 0.05), ((0.0, -0.085, 0.36), (0.02, -0.35, 0.95), 0.06),
                       ((-0.06, -0.065, 0.355), (-0.35, -0.25, 0.9), 0.052), ((0.06, -0.06, 0.36), (0.35, -0.25, 0.9), 0.058),
                       ((-0.02, -0.03, 0.375), (-0.1, -0.15, 1.0), 0.05), ((0.025, -0.02, 0.375), (0.12, -0.1, 1.0), 0.046)]:
        rods.append((root, unit(*d), l, 0.021, 0.3 * len(rods)))
    iron_sand(k, "sand", rods, "body")

    # The mouth: a hard frowning crease across the jaw
    CC, G = srgb(CREASE), srgb(GREY)
    def frown(u, v):
        col = np.tile(G, (len(u), 1))
        line = 0.3 - 0.75 * u * u
        w = 0.22 * (1 - 0.5 * u * u)
        col[(np.abs(v - line) < w) & (np.abs(u) < 0.9)] = CC
        return col
    k.decal((0, -0.165, 0.163), unit(0, -1, 0.15), (0.07, 0.03), frown, eps=0.0025, rings=28, segs=120)

    # Eyes under the brows: white, a black pupil to the inside, the top cut by the brow's slant
    look = eye_open(G)
    de = unit(0.36, -0.92, 0.1)
    def eyes(state):
        k.decal(C + np.array([0, 0, 0.018]) + de * 0.165 * np.array([1, 1, 1]), de, (0.036, 0.028),
                eye_look(state, look, G, srgb("#2A2C34")), eps=0.0035, rings=20, segs=56, mirror=True)

    k.build(voxel=0.0055, smooth=1, band=0.008, wband=0.05)
    k.eye_states(eyes)
    k.export(dest)


if __name__ == "__main__":
    src_, dest_ = args()
    geodude_alola(src_, dest_)
