# Tropius sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tropius.py -- <the export's Tropius.glb> <out.glb>
#
# From the references: a heavy tan sauropod, a deep round body on four short thick legs, two yellow nails on each
# foot; a long thick neck rising from the chest and arching over, the small head hanging forward under the arch; a
# green leaf over the head like a hood, its point over the snout and its edge just over the eyes; a long tan snout,
# the mouth open and pink; a bunch of yellow fruit hanging under the chin; a ruff of broad green leaves round the
# neck's root, lying down over the chest and the shoulders; four great fronds on the back, two to a side, each with a
# dark midrib, notched edges and a fringed end; a short tail.
# The export's joints: body (0, 0.04, 0.36), head (0, -0.27, 0.81), wings (+-0.09, 0.02, 0.5), tail (0, 0.3, 0.33),
# legs (+-0.13, -0.12, 0.22), hinds (+-0.13, 0.2, 0.22).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, srgb, inside, smoothstep, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="scales")

TAN = "#D9A06C"            # the body: the 3D model's warm tan
GREEN, GREEN2, VEIN = "#52B23E", "#4AA238", "#2E7A2A"
NAIL = "#F4D23A"
FRUIT, FRUITTIP = "#F6D23C", "#8A6234"
PINK, MOUTH, INK = "#E8787A", "#9A2E3A", "#2A1A14"

# --- the body: deep and round, a chest in front, a rump behind ----------------------------------------------------
BODY = [((0, 0.04, 0.33), (0.225, 0.29, 0.225)),
        ((0, -0.13, 0.355), (0.2, 0.15, 0.21)),
        ((0, 0.18, 0.325), (0.21, 0.15, 0.21))]
for i, (c, r) in enumerate(BODY):
    k.add(f"body{i}", ell(c, r), TAN, "body")

# --- the legs: short thick stumps under broad shoulders and thighs, two yellow nails each ---------------------------
LX = 0.135
for name, y, bone in (("fore", -0.12, "leg{s}"), ("hind", 0.2, "hind{s}")):
    k.add(name + "top", ell((LX + 0.005, y, 0.26), (0.095, 0.105, 0.13)), TAN, bone, mirror=True)
    k.add(name, tube([(LX, y, 0.24), (LX + 0.004, y - 0.004, 0.16), (LX + 0.006, y - 0.01, 0.1)],
                     [0.09, 0.088, 0.092]), TAN, bone, mirror=True)
    k.add(name + "foot", ell((LX + 0.006, y - 0.018, 0.045), (0.098, 0.105, 0.045)), TAN, bone, mirror=True)
    for j, dx in enumerate((-0.034, 0.034)):
        k.add(f"{name}nail{j}", ell((LX + 0.006 + dx, y - 0.112, 0.024), (0.026, 0.02, 0.022), rot=(-20, 0, 0), sub=3),
              NAIL, bone, fuse=False, mirror=True, surface="shell")

# --- a short tail ---------------------------------------------------------------------------------------------------
k.add("tail", tube([(0, 0.27, 0.32), (0, 0.34, 0.28), (0, 0.38, 0.24)], [0.08, 0.05, 0.022]), TAN, "tail")
k.add("neckroot", ell((0, -0.11, 0.54), (0.17, 0.15, 0.16)), TAN, "body")

# --- the neck: thick at its root in the chest, rising and arching over to the head --------------------------------
# one arch: leaning back from the chest, then over and down to the head, which hangs under it
k.add("necklo", tube([(0, -0.13, 0.42), (0, -0.135, 0.56), (0, -0.12, 0.7)], [0.15, 0.125, 0.095]), TAN, "body")
k.add("neckhi", tube([(0, -0.125, 0.64), (0, -0.12, 0.76), (0, -0.165, 0.87), (0, -0.235, 0.88), (0, -0.28, 0.84)],
                     [0.087, 0.08, 0.075, 0.072, 0.07]), TAN, "head")

# --- the head: a skull hanging forward under the arch, a long snout down and forward; everything of the head is
#     made at a first size and then grown about the head's joint by HS, to the reference's share of the height -------
J, HS = np.array([0, -0.27, 0.81]), 1.45
HEAD_FIRST = len(k.parts)
H = np.array([0, -0.315, 0.81])
HR = np.array((0.072, 0.088, 0.07))
k.add("cranium", ell(H, HR), TAN, "head")
k.add("snout", ell(H + (0, -0.095, -0.03), (0.046, 0.085, 0.042), rot=(14, 0, 0)), TAN, "head")
k.add("jaw", ell(H + (0, -0.06, -0.05), (0.05, 0.07, 0.035), rot=(10, 0, 0)), TAN, "head")


# --- the leaf over the head: a hood on the skull, pointed over the snout, wrapping down to just over the eyes ------
HOOD_R = HR + 0.012


def hood_at(u, v):
    uu = 0.015 + 0.97 * u
    phi = np.radians(-8 + (150 + 8) * uu)                     # front, slightly down, over the top to back and up
    ext = 1 + 0.6 * np.clip((0.35 - uu) / 0.35, 0, 1) ** 1.3    # the point reaches forward over the snout
    half = np.radians(64) * np.sin(np.pi * np.clip(uu / 0.5, 0, 1) / 2) ** 0.8 * (1 - 0.55 * np.clip((uu - 0.6) / 0.4, 0, 1))
    th = (v - 0.5) * 2 * half
    my = -np.cos(phi) * HOOD_R[1] * ext
    mz = np.sin(phi) * HOOD_R[2] - 0.012 * np.clip((0.3 - uu) / 0.3, 0, 1)
    x = np.sin(th) * HOOD_R[0]
    y = H[1] + np.cos(th) * my
    z = H[2] + np.cos(th) * mz
    return np.stack([x, y, z], axis=2)


k.add("hood", sheet(hood_at, 60, 21, 0.012), GREEN, "head", fuse=False, surface="leaf")
crease = [hood_at(np.array([[t]]), np.array([[0.5]]))[0, 0] for t in np.linspace(0.3, 0.92, 9)]
crease = [p + unit(*(p - H)) * 0.007 for p in crease]
k.add("hoodvein", tube(crease, [0.0045] * 9, seg=8, per=4), VEIN, "head", fuse=False, surface="leaf")


# Paint the skull green under the hood, so no tan shows between them
def under_hood(P):
    d = (P - H) / HS                      # H is the grown head's middle by the time this is asked
    up = d[:, 2] - (0.045 + 0.25 * (d[:, 1] + 0.02))
    return inside(-up, 0.002) * inside(np.linalg.norm(d, axis=1) - 0.12, 0.003) * inside(d[:, 1] - 0.03, 0.003)


k.paint(under_hood, GREEN)

# --- the fruit: a bunch of yellow fruit hanging under the chin ----------------------------------------------------
S = H + np.array([0, 0.025, -0.07])
k.add("stem", ell(S + (0, 0, 0.008), (0.022, 0.022, 0.018), sub=3), GREEN2, "head", fuse=False, surface="leaf")
for i, a in enumerate((-0.75, -0.25, 0.25, 0.75)):
    d0 = unit(0.55 * math.sin(a), -0.25, -1)
    outw = np.array([math.sin(a), -0.5, 0.0])
    p0 = S + d0 * 0.01
    p1 = S + d0 * 0.055 + outw * 0.012
    p2 = S + d0 * 0.095 + outw * 0.035 + (0, -0.012, 0.012)
    k.add(f"fruit{i}", tube([p0, p1, p2], [0.016, 0.021, 0.012], seg=12, per=5), FRUIT, "head", fuse=False,
          surface="leaf")
    k.add(f"fruittip{i}", ell(p2 + unit(*(p2 - p1)) * 0.012, (0.008, 0.008, 0.008), sub=2), FRUITTIP, "head",
          fuse=False, surface="leaf")


def grown(p):
    return J + (np.array(p, float) - J) * HS


for p in k.parts[HEAD_FIRST:]:
    p["V"] = grown(p["V"])
H = grown(H)


# --- leaves: a pointed blade with a dark midrib, laid along a curve ----------------------------------------------
def push_out(P, scale=1.07):
    """Points inside the body's ellipsoids pushed out to just over their surface, so a leaf lies on the body."""
    shp = P.shape
    Q = P.reshape(-1, 3).copy()
    for c, r in BODY:
        c, r = np.array(c), np.array(r)
        d = (Q - c) / r
        s = np.linalg.norm(d, axis=1)
        m = s < scale
        Q[m] = c + (Q[m] - c) * (scale / s[m])[:, None]
    return Q.reshape(shp)


def ruff_leaf(name, a, z0, length, width, drop, bone="body"):
    """A leaf of the ruff: from the neck's root at angle a (0 to the front) down and out over the body."""
    c = np.array([0, -0.16, z0])
    radial = np.array([math.sin(a), -math.cos(a), 0.0])
    base = c + radial * 0.1
    d = unit(*(radial * 0.75 + np.array([0, 0, -drop])))
    side = np.cross(d, radial); side /= np.linalg.norm(side)

    def mid(uu):
        return base[None, None] + length * (uu[..., None] * d[None, None]
                                            - 0.35 * uu[..., None] ** 2 * np.array([0, 0, 1.0])[None, None])

    def at(u, v):
        uu = 0.015 + 0.97 * u
        w = width * np.sin(np.pi * np.clip(uu, 0, 1) ** 0.8) ** 0.7 + 0.002
        P = mid(uu) + ((v - 0.5) * 2 * w)[..., None] * side[None, None]
        return push_out(P, 1.05)

    k.add(name, sheet(at, 30, 7, 0.01), GREEN, bone, fuse=False, surface="leaf")
    vein = [push_out(mid(np.array([[t]])), 1.064)[0, 0] for t in np.linspace(0.05, 0.85, 6)]
    k.add(name + "vein", tube(vein, [0.005] * 5 + [0.003], seg=8, per=4), VEIN, bone, fuse=False, surface="leaf")


RUFF = [(0.0, 0.63, 0.22, 0.08, 1.5), (0.8, 0.63, 0.22, 0.08, 1.2), (-0.8, 0.63, 0.22, 0.08, 1.2),
        (1.7, 0.62, 0.2, 0.075, 0.9), (-1.7, 0.62, 0.2, 0.075, 0.9), (2.7, 0.6, 0.17, 0.07, 0.7),
        (-2.7, 0.6, 0.17, 0.07, 0.7)]
for i, (a, z0, L, w, drop) in enumerate(RUFF):
    ruff_leaf(f"ruff{i}", a, z0, L, w, drop)


# --- the fronds on the back: two a side, raised in a V and spread, the front pair forward and out, the hind pair
#     back and out, each turned so its face shows from the front three-quarter and from behind -----------------------
def frond(name, base, d, face, length, width, bone):
    base, d = np.array(base, float), unit(*d)
    nt = unit(*face)
    w_dir = np.cross(nt, d); w_dir /= np.linalg.norm(w_dir)
    g = np.array([0, 0, -1.0]) - d * d[2]; g /= np.linalg.norm(g)
    def mid(uu):
        return base[None, None] + length * (uu[..., None] * d[None, None] + 0.14 * uu[..., None] ** 2 * g[None, None])

    def width_at(uu):
        # a short stalk, then a broad blade widest past its middle, its far end blunt
        grow = smoothstep(0.02, 0.6, uu) ** 0.7
        blunt = 1 - 0.55 * np.clip((uu - 0.66) / 0.34, 0, 1) ** 1.6
        return width * grow * blunt + 0.008

    def at(u, v):
        # the far end cut into four shallow notches: where the blade ends depends on the place across it
        across = np.abs(2 * v - 1)
        notch = 0.045 * (1 - np.abs(np.cos(np.pi * 2 * v)))      # four bays between five points, across the end
        uu = 0.012 + (0.988 - notch - 0.02 * across) * u
        w = width_at(uu)
        s = (v - 0.5) * 2 * w
        cup = 0.016 * (1 - (2 * v - 1) ** 2)                    # a little cupped, so it is no flat card
        return mid(uu) + s[..., None] * w_dir[None, None] + cup[..., None] * nt[None, None]

    k.add(name, sheet(at, 80, 13, 0.009), GREEN, bone, fuse=False, surface="leaf")
    vein = [mid(np.array([[t]]))[0, 0] + nt * 0.016 for t in np.linspace(0.0, 0.93, 11)]   # on the cupped midline
    k.add(name + "vein", tube(vein, [0.011] * 7 + [0.01, 0.009, 0.008, 0.006], seg=8, per=4), VEIN, bone, fuse=False,
          surface="leaf")


for sx, side in ((1, "R"), (-1, "L")):
    frond(f"frontfrond{side}", (0.09 * sx, -0.05, 0.56), (0.72 * sx, -0.42, 0.55), (0.35 * sx, -0.75, 0.5),
          0.8, 0.135, "wing" + side)
    frond(f"hindfrond{side}", (0.08 * sx, 0.0, 0.57), (0.6 * sx, 0.42, 0.68), (0.2 * sx, 0.6, 0.78),
          0.75, 0.13, "wing" + side)

# --- the face: the open mouth on the snout's underside, the nostrils ---------------------------------------------
CPK, CMO = srgb(PINK), srgb(MOUTH)


def mouth(u, v):
    c = np.tile(CPK, (len(u), 1))
    c[(u / 0.78) ** 2 + ((v + 0.05) / 0.62) ** 2 < 1] = CMO
    return c


k.decal(H + np.array((0, -0.14, -0.058)) * HS, unit(0, -0.45, -0.9), (0.034 * HS, 0.03 * HS), mouth, eps=0.004,
        rings=14, segs=48, up=(0, -1, 0))
k.decal(H + np.array((0.014, -0.175, -0.02)) * HS, unit(0.3, -0.8, 0.5), (0.006 * HS, 0.005 * HS), INK, eps=0.004,
        rings=4, segs=20, mirror=True)

k.build(voxel=0.0082)

# --- the eyes: dark almonds with a white glint, their top cut by the hood's edge -----------------------------------
CT, CK, CW, CB = srgb(TAN), srgb(INK), srgb("#FFFFFF"), srgb("#1A1210")
EYE_DIR = unit(0.78, -0.58, 0.18)
E = H + (np.array([0.0, -0.045, -0.004]) + EYE_DIR * 0.062) * HS


def eye(u, v):
    c = np.tile(CB, (len(u), 1))
    c[((u + 0.15) / 0.3) ** 2 + ((v - 0.25) / 0.3) ** 2 < 1] = CW
    return c


EYE_OUT = polygon([(-1, 0.1), (-0.9, -0.4), (-0.55, -0.78), (0, -0.88), (0.55, -0.78), (0.9, -0.4), (1, 0.1),
                   (0.85, 0.5), (0.5, 0.62), (-0.5, 0.62), (-0.85, 0.5)])   # round below, flat under the hood


def eyes(state):
    k.decal(E, EYE_DIR, (0.047, 0.037), eye_look(state, eye, CT, CK, line=0.16), eps=0.004, rings=20, segs=56,
            outline=EYE_OUT, mirror=True)


k.eye_states(eyes)
k.export(out)
