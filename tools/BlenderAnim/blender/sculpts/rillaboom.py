# Rillaboom sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/rillaboom.py -- <the export's Rillaboom.glb> <out.glb>
#
# Grown from grookey.py (sibling): its ways of making a muzzle with nostrils and of placing on an ellipsoid.
# From the references (they overrule the task's text: neither picture nor the game's own sculptor has a drum, which
# belongs to its Gigantamax form, so none is built): a great dark-brown gorilla leaning forward, its head carried low
# in front; a huge mane of green pointed leaves round the head and over the shoulders, its largest mass; a dark face
# with a black band over stern orange-red eyes, a big round orange nose-muzzle, a curl of green vine falling from the
# brow to the nose; a broad grey-tan plate over the chest, notched at the top, and two tan marks on the belly; a belt
# of green leaves round the waist; long thick arms carried forward and down, a cuff of green leaves round each forearm
# with a jagged lower edge, great grey-tan fists at knee height (on the ground in the pictures); short bent dark legs,
# broad grey-tan feet. Height about 0.95.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, lumpy, outward, axes, srgb, inside, unit, eye_look, polygon, args, sibling

g = sibling("grookey")
ted = sibling("teddiursa")
hit, poly_mask, round_about, mirrored = ted.hit, ted.poly_mask, ted.round_about, ted.mirrored

BROWN, DARK, TAN, TAN_D = "#5B4741", "#3E302D", "#C6B59E", "#A8977F"
GREEN, GREEN_D, ORANGE, ORANGE_D = "#2FA352", "#1F8040", "#F05A26", "#B83C16"
BLACK, IRIS, INK = "#1C1919", "#F2762E", "#120F10"

H = np.array([0.0, -0.18, 0.705])          # the head's middle (its bone is at 0, -0.08, 0.64)
HR = np.array([0.11, 0.1, 0.102])
MANE = np.array([0.0, -0.02, 0.775])        # the mane's middle, behind the face


def spike(base, d, length, width, thick, up=(0, 0, 1)):
    """A pointed leaf tuft: a flat-faced hull from a broad base along d."""
    d = unit(*d)
    a = np.cross(d, up)
    if np.linalg.norm(a) < 1e-3:
        a = np.cross(d, (1.0, 0, 0))
    a /= np.linalg.norm(a)
    b = np.cross(a, d)
    base = np.array(base, float)
    pts = [base + a * width + b * thick * 0.3, base - a * width + b * thick * 0.3,
           base + a * width * 0.8 - b * thick, base - a * width * 0.8 - b * thick,
           base + d * length * 0.45 + a * width * 0.7, base + d * length * 0.45 - a * width * 0.7,
           base + d * length * 0.45 + b * thick * 0.4, base + d * length]
    return hull(pts)


def body(k):
    """A great torso leaning forward, shoulders massed high, the pelvis low behind."""
    k.add("chest", ell((0, -0.07, 0.53), (0.215, 0.17, 0.16), rot=(18, 0, 0)), BROWN, "body")
    k.add("pelvis", ell((0, 0.03, 0.37), (0.165, 0.14, 0.12)), BROWN, "body")
    k.add("belly", ell((0, -0.04, 0.43), (0.15, 0.13, 0.11)), BROWN, "body")
    k.add("neck", ell((0, -0.1, 0.64), (0.12, 0.1, 0.08)), BROWN, "body")
    # The grey-tan plate over the chest: broad, notched at the top in the middle, its lower edge in points
    C = np.array([0.0, -0.05, 0.52])
    z0 = C[2]
    PLATE = mirrored([(0.0, z0 + 0.03), (0.04, z0 + 0.075), (0.12, z0 + 0.07), (0.2, z0 + 0.04), (0.22, z0 + 0.0),
                      (0.17, z0 - 0.04), (0.12, z0 - 0.025), (0.08, z0 - 0.065), (0.03, z0 - 0.04),
                      (0.0, z0 - 0.07)])
    front = lambda P: inside(P[:, 1] - (C[1] - 0.05), 0.004) * inside(P[:, 2] - (z0 + 0.15), 0.004) \
        * inside((z0 - 0.15) - P[:, 2], 0.004)
    k.paint(lambda P: poly_mask(*round_about(P, C, 0.2), PLATE, edge=0.003) * front(P), TAN,
            on=["chest", "belly", "neck"])
    # Two tan marks on the belly, like leaves pointing out
    MK = [(0.02, 0.402), (0.05, 0.43), (0.12, 0.426), (0.148, 0.4), (0.11, 0.378), (0.04, 0.374)]
    for pts in (MK, [(-x, z) for x, z in MK]):
        k.paint(lambda P, pts=pts: poly_mask(*round_about(P, C, 0.2), pts, edge=0.003) * front(P), TAN,
                on=["belly", "chest", "pelvis"])


def legs(k):
    """Short bent dark legs, thighs fused into the hips, broad grey-tan feet with toes."""
    k.add("haunch", ell((0.115, 0.03, 0.3), (0.1, 0.11, 0.11)), DARK, "leg{s}", mirror=True)
    k.add("thigh", tube([(0.12, 0.03, 0.28), (0.15, -0.05, 0.19)], [0.1, 0.09]), DARK, "leg{s}", mirror=True)
    k.add("shin", tube([(0.15, -0.05, 0.18), (0.155, -0.02, 0.07)], [0.086, 0.075]), DARK, "leg{s}", mirror=True)
    F = np.array((0.16, -0.07, 0.04))
    k.add("foot", ell(F, (0.075, 0.105, 0.042)), TAN, "leg{s}", mirror=True)
    for i, dx in enumerate((-0.042, -0.014, 0.014, 0.042)):
        k.add(f"toe{i}", ell(F + np.array([dx, -0.095, -0.008]), (0.02, 0.026, 0.024)), TAN, "leg{s}", mirror=True)
    k.paint(lambda P: inside(P[:, 2] - 0.066, 0.003) * inside(0.06 - np.abs(P[:, 0]), 0.003), TAN,
            on=["shin", "foot", "toe"])


def arms(k):
    """Long thick arms carried forward and down, a cuff of green leaves over each forearm, a great grey-tan fist at
    knee height before the legs."""
    S, E, W = np.array((0.2, -0.05, 0.6)), np.array((0.29, -0.13, 0.4)), np.array((0.255, -0.24, 0.24))
    k.add("shoulder", ell(S + np.array([0.02, 0.0, -0.01]), (0.105, 0.11, 0.1)), BROWN, "arm{s}", mirror=True)
    k.add("upper", tube([S, (S + E) / 2 + np.array([0.02, 0, 0]), E], [0.088, 0.08, 0.072]), BROWN, "arm{s}",
          mirror=True)
    k.add("fore", tube([E, W], [0.072, 0.066]), BROWN, "arm{s}", mirror=True)
    d = unit(*(W - E))
    # The cuff: a green sleeve over the forearm, pointed leaves round its lower rim
    k.add("cuff", tube([E + d * 0.12, W - d * 0.005], [0.08, 0.083]), GREEN, "arm{s}", mirror=True)
    a, b = axes(d)
    for i in range(10):
        th = 2 * math.pi * (i + 0.5) / 10
        o = math.cos(th) * a + math.sin(th) * b
        base = W - d * (0.035 + 0.012 * (i % 3)) + o * 0.078
        k.add(f"cufftip{i}", spike(base, d + o * 0.35, 0.055 + 0.015 * ((i * 7) % 3), 0.026, 0.02, up=tuple(o)),
              GREEN if i % 2 else GREEN_D, "arm{s}", fuse=False, mirror=True, surface="leaf")
    # The fist: knuckles forward and down
    P = W + d * 0.06
    k.add("fist", ell(P, (0.078, 0.08, 0.066), rot=(-20, 0, 0)), TAN, "arm{s}", mirror=True)
    for i, dx in enumerate((-0.045, -0.015, 0.015, 0.045)):
        k.add(f"knuckle{i}", ell(P + np.array([dx, -0.065, -0.03]), (0.022, 0.026, 0.03)), TAN, "arm{s}",
              mirror=True)
    k.add("thumb", ell(P + np.array([-0.06, -0.04, 0.0]), (0.024, 0.032, 0.024), rot=(0, 0, 30)), TAN, "arm{s}",
          mirror=True)


def belt(k):
    """A belt of green leaves round the waist, pointing down and out, thickest at the front and sides."""
    for i in range(14):
        th = math.radians(-90 + (i - 6.5) * 22)            # round the waist, starting at the front
        o = np.array([math.cos(th), math.sin(th), 0])
        base = np.array([0, 0.0, 0.37]) + o * np.array([0.17, 0.15, 0]) + np.array([0, -0.01, 0.0])
        d = unit(*(o * 0.6 + np.array([0, 0, -0.8])))
        k.add(f"belt{i}", spike(base, d, 0.07 + 0.012 * (i % 3), 0.032, 0.02, up=tuple(o)),
              GREEN if i % 2 else GREEN_D, "body", fuse=False, surface="leaf")


def mane(k):
    """The mane: a great lumpy green mass round and behind the head and over the shoulders, ragged with pointed
    leaves of uneven length all round its rim."""
    k.add("mane", ell(MANE, (0.27, 0.17, 0.18), deform=lumpy(7, 4, 0.08)), GREEN, "head")
    k.add("manelow", ell(MANE + (0, 0.04, -0.12), (0.29, 0.16, 0.12), deform=lumpy(7, 3, 0.07)), GREEN, "head")
    k.add("maneback", ell(MANE + (0, 0.1, -0.03), (0.22, 0.13, 0.17), deform=lumpy(6, 4, 0.08)), GREEN, "body")
    k.add("brow", ell(H + (0, 0.015, 0.095), (0.12, 0.09, 0.055)), GREEN, "head")
    rs = np.random.RandomState(812)
    n = 0
    for ring, (dy, scale, count) in enumerate(((-0.06, 1.0, 18), (0.06, 0.95, 16), (0.14, 0.75, 12))):
        for i in range(count):
            th = 2 * math.pi * (i + 0.3 * ring) / count
            if math.sin(th) < -0.82:                      # none hanging over the face's lower half and the chest
                continue
            o = np.array([math.cos(th) * 0.27 * scale, 0, math.sin(th) * 0.18 * scale])
            base = MANE + np.array([o[0], dy, o[2] - (0.08 if math.sin(th) < 0 else 0.0)])
            out_ = unit(math.cos(th), 0.25 * (ring - 0.5), math.sin(th) * 1.1 + 0.15)
            L = (0.075 + 0.045 * rs.rand()) * scale * (0.65 if math.sin(th) > 0.6 else 1.0)
            k.add(f"manetip{n}", spike(base, out_, L, 0.04, 0.024, up=(0, 1, 0)),
                  GREEN if n % 2 else GREEN_D, "head", fuse=False, surface="leaf")
            n += 1


def face(k, HEAD):
    """The black band over the eyes, the orange nose-muzzle with nostrils, a curl of green vine to the nose."""
    MC, MR = H + (0, -0.098, -0.056), (0.053, 0.052, 0.058)
    S = ell(MC, MR, rot=(-15, 0, 0))
    k.add("muzzle", S, ORANGE, "head", surface="shell")
    g.nostrils(k, S, MC, MR, -0.02, 0.018, (0.008, 0.006), srgb(ORANGE_D))
    EZ = H[2] + 0.012
    def band(P):
        x, z = round_about(P, H, HR[0])
        front = inside(P[:, 1] - (H[1] - 0.04), 0.003) * inside(np.sqrt((((P - H) / (HR * 1.4)) ** 2).sum(1)) - 1)
        top = EZ + 0.034 - 0.12 * np.abs(x)               # the band's top slopes down to the sides
        bot = EZ - 0.03 + 0.06 * np.abs(x)
        return inside(z - top, 0.003) * inside(bot - z, 0.003) * inside(np.abs(x) - 0.095, 0.003) * front
    k.paint(band, BLACK, on=["head", "brow", "neck"])
    # The curl of vine from the brow down to the nose's top, a loose tube
    V = [H + np.array((0.01, 0.006, 0.0)) + p for p in [(0.0, -0.065, 0.115), (0.004, -0.1, 0.08), (0.0, -0.118, 0.035),
                         (-0.006, -0.128, 0.0), (0.0, -0.145, -0.02), (0.014, -0.143, -0.006),
                         (0.012, -0.134, 0.014)]]
    k.add("vine", outward(tube(V, [0.0145, 0.0145, 0.014, 0.014, 0.0135, 0.0135, 0.013])), GREEN, "head", fuse=False,
          surface="leaf")
    return EZ


def rillaboom(src, out):
    k = Kit(src, coat="fur")
    body(k)
    legs(k)
    arms(k)
    belt(k)
    HEAD = ell(H, HR)
    k.add("head", HEAD, BROWN, "head")
    mane(k)
    EZ = face(k, HEAD)

    # Stern orange-red eyes in the black band, the top line sloping down to the nose
    OUT = [(-1.0, -0.1), (-0.6, -0.75), (0.2, -0.85), (0.85, -0.4), (1.0, 0.35), (0.3, 0.45), (-0.6, 0.05)]
    def open_look(u, v):
        c = np.tile(srgb(INK), (len(u), 1))
        c[(u / 0.8) ** 2 + ((v + 0.15) / 0.6) ** 2 < 1] = srgb(IRIS)
        c[((u + 0.1) / 0.3) ** 2 + ((v + 0.15) / 0.4) ** 2 < 1] = srgb(INK)
        c[((u + 0.32) / 0.13) ** 2 + ((v - 0.05) / 0.13) ** 2 < 1] = np.array([1.0, 1.0, 1.0])
        return c
    de = unit(0.4, -0.91, 0.04)
    E = g.on_ell(H, HR, de)
    E[2] = EZ
    def lay(state):
        look = eye_look(state, open_look, srgb(BLACK), srgb("#B8A8A0"), line=0.2)
        if state in ("Open", "Fierce"):
            look = eye_look(state, open_look, srgb(BLACK), srgb(INK), line=0.2)
        for s in (1, -1):
            out_ = polygon(OUT if s > 0 else [(-a, b) for a, b in OUT])
            col = look if s > 0 else (lambda u, v, look=look: look(-u, v))
            k.decal(E * np.array([s, 1, 1]), de * np.array([s, 1, 1]), (0.04, 0.032), col, outline=out_,
                    eps=0.002, rings=20, segs=56)

    k.build(voxel=0.0088, smooth=4, band=0.006, wband=0.07)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    rillaboom(*args())
