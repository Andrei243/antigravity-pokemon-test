# Grookey sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/grookey.py -- <the export's Grookey.glb> <out.glb>
#
# The base of the Grookey line, written to be grown from (thwackey.py and rillaboom.py take its pieces through
# sibling("grookey")): the eye, the muzzle with its nostrils, the ear with its lining, the stick with its cut ends and
# the leaf are functions here.
#
# From the references: a small lime-green monkey with a big round head (some 0.43 of its height), wider than tall; a
# pale yellow mask round the eyes, two round lobes joined below with the green coming down in a V between them; big
# eyes, a large black iris in a white with a highlight; a big round orange muzzle standing forward out of the lower
# face, two nostrils and a smile; big round brown ears out at the sides; a short tuft on the crown with a brown stick
# laid across it, cut ends showing, and a sprout of two leaves in a V out of the tuft. A small pear of a body, short
# legs with orange socks and orange feet; thin green arms with orange hands (one waved in the art, carried forward
# and down here); a long brown tail curling up into a loop behind, out to the +X side. Height about 0.46.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, slab, lathe, outward, srgb, inside, unit, boxy, eye_look, args, sibling

ted = sibling("teddiursa")
hit, poly_mask, round_about, mirrored = ted.hit, ted.poly_mask, ted.round_about, ted.mirrored

GREEN, GREEN_D, MASK = "#8CC64A", "#6FA83A", "#F5EE9C"
ORANGE, ORANGE_D, BROWN, BROWN_D = "#EE7A30", "#C45A22", "#8E5A32", "#6A3F22"
WOOD, WOOD_END, LEAF, LEAF_D = "#9A6638", "#D9B07A", "#9AD45C", "#78B848"
INK, WHITE = "#16161A", np.array([1.0, 1.0, 1.0])

H = np.array([0.0, -0.012, 0.296])          # the head's middle (its bone is at 0, 0, 0.2)
HR = np.array([0.11, 0.098, 0.094])


def on_ell(C, R, d, lift=0.0):
    """The point of an ellipsoid's surface in a direction from its centre."""
    d = unit(*d)
    s = 1 / math.sqrt(((d / np.array(R)) ** 2).sum())
    return np.array(C) + d * (s + lift)


def turned(rot, v):
    return np.array(Euler([math.radians(a) for a in rot]).to_matrix()) @ np.array(v, float)


# --- pieces the line shares ------------------------------------------------------------------------------------------
def ear(k, c, r, rot, main, lining, bone="head"):
    """A round ear fused to the head's side, a darker lining as a loose plate on its front (mirrored)."""
    k.add("ear", ell(c, r, rot=rot, deform=boxy(2.3)), main, bone, mirror=True)
    n = turned(rot, (0, -1, 0))
    piece = ell(np.array(c) + n * r[1] * 0.92, (r[0] * 0.72, max(r[1] * 0.22, 0.004), r[2] * 0.72), rot=rot, sub=3)
    k.add("lining", outward(piece), lining, bone, fuse=False, mirror=True)


def stick(k, a, b, r, name="stick", bone="head", wood=WOOD, end=WOOD_END):
    """A cut stick from a to b: a brown rod with flat ends, a pale ring of cut wood at each."""
    a, b = np.array(a, float), np.array(b, float)
    d = b - a
    L = np.linalg.norm(d)
    d = d / L
    k.add(name, lathe(a, d, [(0, 0.0), (r * 0.98, 0.0), (r, 0.004), (r, L - 0.004), (r * 0.98, L), (0, L)], seg=28),
          wood, bone, fuse=False, surface="shell")
    for i, (p, s) in enumerate(((a, -1), (b, 1))):
        k.add(f"{name}end{i}", lathe(p + d * s * 0.0015, d, [(0, -0.001), (r * 0.8, -0.001), (r * 0.8, 0.001),
                                                              (0, 0.001)], seg=24), end, bone, fuse=False,
              surface="shell")


def leaf_outline(length, width):
    """A pointed leaf, its stalk at a = 0, broadest a third of the way along."""
    t = np.linspace(0, 1, 15)
    w = width * np.sin(np.pi * np.clip(t * 0.94 + 0.03, 0, 1)) ** 0.75 * (1 - 0.3 * t)
    top = [(length * a, b) for a, b in zip(t, w)]
    bot = [(length * a, -b) for a, b in zip(t[::-1], w[::-1])]
    return top + [(length * 1.03, 0.0)] + bot[1:-1]


def leaf_piece(k, name, base, d, length, width, thick, colour, bone, face=(0, -1, 0)):
    """A flat leaf from base along d, its face turned toward face (a slab: a loose piece)."""
    d = unit(*d)
    f = np.array(face, float)
    side = np.cross(f, d); side /= np.linalg.norm(side)
    k.add(name, slab(leaf_outline(length, width), base, d, side, thick), colour, bone, fuse=False, surface="leaf")


def big_eye(u, v):
    """A white with a large black iris set a little inward, a white highlight high in it, a dark rim."""
    c = np.tile(srgb(INK), (len(u), 1))
    c[u * u + v * v < 0.84 ** 2] = WHITE
    c[((u + 0.12) / 0.66) ** 2 + ((v + 0.02) / 0.74) ** 2 < 1] = srgb(INK)
    c[((u + 0.3) / 0.22) ** 2 + ((v - 0.3) / 0.24) ** 2 < 1] = WHITE
    return c


def muzzle(k, C, R, rot, colour, bone="head", name="muzzle"):
    """An orange muzzle standing forward out of the face (fused; its colour crisp by band)."""
    S = ell(C, R, rot=rot)
    k.add(name, S, colour, bone, surface="shell")
    return S


def nostrils(k, S, C, R, dz, dx, half, colour):
    for s in (1, -1):
        p = hit(S, (dx * s, C[1] - 0.3, C[2] + dz), (0, 1, 0))
        k.decal(p, unit(0.2 * s, -1, 0.3), half, colour, eps=0.0015, rings=8, segs=32)


# --- Grookey ---------------------------------------------------------------------------------------------------------
def body(k):
    def pear(V):
        V = V.copy(); s = 1 + 0.12 * np.clip(-V[:, 2], 0, 1) - 0.1 * np.clip(V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("torso", ell((0, 0.004, 0.135), (0.07, 0.064, 0.082), deform=pear), GREEN, "body")
    k.add("neck", ell((0, 0.0, 0.2), (0.056, 0.052, 0.04)), GREEN, "body")
    k.add("belly", ell((0, -0.03, 0.12), (0.052, 0.04, 0.055)), GREEN, "body")


def legs(k):
    """Short legs: a haunch fused into the hips, a stub of shin, an orange sock and an orange foot with three toes."""
    k.add("haunch", ell((0.044, -0.005, 0.082), (0.042, 0.048, 0.042)), GREEN, "leg{s}", mirror=True)
    k.add("shin", tube([(0.05, -0.012, 0.07), (0.056, -0.018, 0.03)], [0.028, 0.025]), GREEN, "leg{s}", mirror=True)
    F = np.array((0.058, -0.03, 0.015))
    k.add("foot", ell(F, (0.03, 0.046, 0.016)), ORANGE, "leg{s}", mirror=True)
    for i, dx in enumerate((-0.017, 0.0, 0.017)):
        k.add(f"toe{i}", ell(F + np.array([dx, -0.044, -0.003]), (0.011, 0.014, 0.01)), ORANGE, "leg{s}",
              mirror=True)
    # The orange sock over the ankle, its edge crisp
    k.paint(lambda P: inside(P[:, 2] - 0.036, 0.002) * inside(0.02 - np.abs(P[:, 0]), 0.002), ORANGE)


def arms(k):
    """Thin green arms carried forward and a little down, orange hands with three fingers."""
    S, E, W = np.array((0.052, -0.004, 0.174)), np.array((0.085, -0.04, 0.14)), np.array((0.072, -0.088, 0.112))
    k.add("arm", tube([S, E, W], [0.024, 0.021, 0.019]), GREEN, "arm{s}", mirror=True)
    d = unit(*(W - E))
    k.add("wrist", tube([W - d * 0.012, W + d * 0.01], [0.0195, 0.021]), ORANGE, "arm{s}", mirror=True)
    Q = W - d * 0.01
    def cuff(P_, Q=Q, d=d):
        A = P_.copy(); A[:, 0] = np.abs(A[:, 0])
        return inside(-((A - Q) @ d), 0.0015) * inside(np.linalg.norm(A - Q, axis=1) - 0.06, 0.002)
    k.paint(cuff, ORANGE, on=["arm", "wrist", "hand"])
    P = W + d * 0.025
    k.add("hand", ell(P, (0.024, 0.024, 0.02), rot=(20, 0, 0)), ORANGE, "arm{s}", mirror=True)
    for i, dx in enumerate((-0.014, 0.0, 0.014)):
        p0 = P + np.array([dx, -0.014, -0.004])
        k.add(f"finger{i}", tube([p0, p0 + np.array([dx * 0.3, -0.016, -0.012])], [0.0085, 0.0075]), ORANGE,
              "arm{s}", mirror=True)
    k.add("thumb", tube([P + np.array([-0.02, -0.004, 0.006]), P + np.array([-0.03, -0.016, 0.0])], [0.0085, 0.0075]),
          ORANGE, "arm{s}", mirror=True)


def tail(k):
    """A long brown tail from the back, out to +X and curling up into a loop."""
    T = [(0.0, 0.05, 0.085), (0.03, 0.1, 0.065), (0.075, 0.14, 0.07), (0.115, 0.165, 0.105), (0.13, 0.17, 0.155),
         (0.115, 0.16, 0.2), (0.085, 0.145, 0.21), (0.065, 0.135, 0.19), (0.07, 0.137, 0.162)]
    k.add("tail", tube(T, [0.015, 0.0145, 0.014, 0.014, 0.0135, 0.013, 0.013, 0.0125, 0.012]), BROWN, "tail")


def head(k):
    HEAD = ell(H, HR)
    k.add("head", HEAD, GREEN, "head")
    k.add("cheeks", ell(H + (0, -0.03, -0.03), (0.1, 0.066, 0.06)), GREEN, "head")
    # The tuft on the crown the sprout grows from
    k.add("tuft", tube([H + (0, 0.004, HR[2] - 0.03), H + (0, 0.006, HR[2] + 0.012)], [0.036, 0.016]), GREEN, "head")
    ear(k, (0.118, 0.012, 0.302), (0.05, 0.02, 0.047), (0, 0, 22), BROWN, BROWN_D)
    S = muzzle(k, H + (0, -0.094, -0.04), (0.056, 0.047, 0.042), (-12, 0, 0), ORANGE)
    return HEAD, S


def mask(k):
    """The pale yellow mask: a round lobe round each eye, joined below over the muzzle, the green coming down in a V
    between them."""
    EX, EZ = 0.047, H[2] + 0.012
    def m(P):
        x, z = round_about(P, H, HR[0])
        front = inside(P[:, 1] - (H[1] - 0.035), 0.003) * inside(np.sqrt((((P - H) / (HR * 1.35)) ** 2).sum(1)) - 1)
        lobe = np.minimum(np.hypot(np.abs(x) - EX, (z - EZ) * 1.02), 9) - 0.064
        bridge = np.maximum(np.abs(x) - 0.072, np.maximum(z - EZ, H[2] - 0.06 - z))
        d = np.minimum(lobe, bridge)
        vee = (z - (EZ + 0.008)) - 1.5 * np.abs(x)          # above this line between the lobes: the green's V
        d = np.maximum(d, vee)
        return inside(d, 0.0025) * front
    k.paint(m, MASK)


def grookey(src, out):
    k = Kit(src, coat="fur")
    body(k)
    legs(k)
    arms(k)
    tail(k)
    HEAD, S = head(k)
    mask(k)

    # The stick across the crown, the sprout's stalk and its two leaves in a V
    top = H + np.array([0, 0.006, HR[2] + 0.004])
    stick(k, top + (-0.092, 0.012, -0.004), top + (0.088, 0.008, 0.006), 0.0115)
    k.add("stalk", outward(tube([top + (0, 0, -0.01), top + (0, -0.002, 0.026)], [0.0085, 0.008])), LEAF_D, "head",
          fuse=False, surface="leaf")
    leaf_piece(k, "leafR", top + (0.002, -0.004, 0.024), (0.7, -0.1, 0.72), 0.082, 0.028, 0.012, LEAF, "head",
               face=(0.25, -1, 0.1))
    leaf_piece(k, "leafL", top + (-0.002, 0.002, 0.024), (-0.62, 0.1, 0.78), 0.07, 0.024, 0.012, LEAF, "head",
               face=(-0.25, -1, -0.1))

    # Nostrils and a smile on the muzzle
    MC, MR = H + (0, -0.094, -0.04), (0.056, 0.047, 0.042)
    nostrils(k, S, MC, MR, 0.012, 0.014, (0.0055, 0.0045), srgb(ORANGE_D))
    CO, CD = srgb(ORANGE), srgb(ORANGE_D)
    def smile(u, v):
        c = np.tile(CO, (len(u), 1))
        c[np.abs(v - (-0.1 + 0.6 * u * u)) < 0.28] = CD
        return c
    p = hit(S, (0, MC[1] - 0.3, MC[2] - 0.016), (0, 1, 0))
    k.decal(p, unit(0, -1, -0.35), (0.03, 0.009), smile, eps=0.0015, rings=8, segs=48)

    # The eyes: big round ones on the mask's lobes
    de = unit(0.44, -0.89, 0.12)
    E = on_ell(H, HR, de)
    def lay(state):
        look = eye_look(state, big_eye, srgb(MASK), srgb(INK), line=0.16)
        k.decal(E, de, (0.032, 0.039), look, eps=0.0022, rings=20, segs=56, mirror=True)

    k.build(voxel=0.0046, smooth=4, band=0.003, wband=0.035)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    grookey(*args())
