# Noibat sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/noibat.py -- <the export's Noibat.glb> <out.glb>
# From the references: a small hovering bat, its round pale lilac head the most of it after its ears; two huge ears
# spread up and out from the top of the head like loudspeaker dishes, each a thick teardrop pointing outward, dark
# purple round its rim, a pale ring inside and a dark grey-brown core; a dark purple mark over the forehead coming down
# between the eyes; big yellow eyes with a black slit and a black rim; a small dark mouth with one white fang; pale
# jags of fur hanging under the cheeks; a dark grey-brown shaggy ruff round the neck over a small body, dark tufts at
# the hips; small pale lilac wings out from the shoulders, their trailing edges scalloped, two dark claws at each
# wing's lower inner corner; two short pale legs hanging under.
# Written to be grown from: Noivern imports it (sibling("noibat")) for its ear dishes, its eye and its wing plates.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, srgb, inside, smoothstep, unit, eye_look, polygon, args, sibling

# --- the line's colours -------------------------------------------------------------------------------------------
LILAC, LILACLOW = "#CDB2E0", "#B89BCF"
EARRIM, EARRING, EARCORE = "#874CAE", "#BCA5D0", "#5B4F58"
MARK = "#8C55B2"
RUFF = "#4D4148"
EYE, EYEINK, WHITE = "#F4C51E", "#141018", "#F8F6F4"
MOUTH = "#3A1C2C"

# --- the frame ----------------------------------------------------------------------------------------------------
H = np.array([0.0, -0.006, 0.222])            # the head over its joint (0, -0.01, 0.167)
HR = np.array([0.058, 0.052, 0.053])
EAR_ROOT = np.array([0.036, 0.004, 0.252])    # where an ear leaves the head (its joint (0.03, -0.02, 0.232))
EAR_D = unit(1.0, 0.16, 0.5)                 # up and out, a little back
EAR_FACE = unit(0.42, -1.0, 0.15)             # the dish's face: forward and some 30 degrees out
EAR = (0.074, 0.043, 0.016)                   # half length, half width, half thickness
VOXEL = 0.0034


def ear_frame(d, face):
    """A right-handed frame (d, w, n) for a dish along d whose face looks along about `face`."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    n = np.array(face, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    return d, np.cross(n, d), n


def side(v, s):
    return np.array(v, float) * np.array([s, 1, 1.0])


def teardrop(point=0.75):
    """A deform of the unit sphere into a teardrop pointing along +x."""
    def f(V):
        V = V.copy()
        x = V[:, 0]
        k = 1 - point * np.clip(x, 0, 1) ** 2
        V[:, 1] *= k; V[:, 2] *= k
        return V
    return f


def dished(front=0.35, shape=None):
    """Flattens the face (+z, the dish's front) of the unit sphere: a thick dish with a rounded rim."""
    def f(V):
        if shape is not None:
            V = shape(V)
        V = V.copy()
        V[:, 2] = np.where(V[:, 2] > 0, V[:, 2] * front, V[:, 2])
        return V
    return f


def tear_outline(point=0.6, n=72):
    """A teardrop's outline in u, v pointing toward +u, its middle the patch's middle."""
    t = np.linspace(0, 2 * math.pi, n, endpoint=False)
    x, y = np.cos(t), np.sin(t)
    y = y * (1 - point * np.clip(x, 0, 1) ** 2)
    x = np.where(x > 0, x * 1.08, x)
    return polygon(list(zip(x, y)))


def ear_dish(k, name, bone, centre, d, face, radii, colour, rings, shape=None, front=0.35, sides=(1, -1)):
    """
    A thick dish fused to the head, one on each side (mirrored by hand so that the patches' +u runs outward on both):
    centre, d (its length's way), face (its front's way) given for the +X side. rings: (scale of the dish's face,
    colour, outline or None, lift) laid as stacked patches on its flat face.
    """
    for s in sides:
        c = side(centre, s)
        dd, w, n = ear_frame(side(d, s), side(face, s))
        V, F = ell((0, 0, 0), radii, deform=dished(front, shape))
        V = V @ np.stack([dd, w, n], axis=1).T + c
        sn = "R" if s > 0 else "L"
        k.add(f"{name}{sn}", (V, F), colour, bone.format(s=sn))
        on = c + n * radii[2] * front * 0.9
        for scale, col, outline, lift in rings:
            k.decal(on, n, (radii[0] * scale, radii[1] * scale), col, up=np.cross(n, dd), eps=lift,
                    outline=outline, rings=16, segs=72)


def slit_eye(rim=0.84, slit=0.2, iris=EYE, ink=EYEINK):
    """A yellow eye with a black rim and a black upright slit."""
    I, K = srgb(iris), srgb(ink)
    def look(u, v):
        c = np.tile(I, (len(u), 1))
        c[u * u + v * v > rim * rim] = K
        c[np.abs(u + 0.04) < slit * np.sqrt(np.clip(1 - (v / 0.82) ** 2, 0, 1))] = K
        return c
    return look


def wing_plate(k, name, bone, origin, ex, ez, outline, thick, colour, surface="leather"):
    """A bat wing as a plate with a hard rim, mirrored to the model's left (slab is wound outward under any frame)."""
    k.add(name, slab(outline, origin, ex, ez, thick), colour, bone, fuse=False, mirror=True, surface=surface)


def claw(base, d, length, r, up=(0, 1, 0)):
    """A solid claw: a diamond of points round base and a tip along d."""
    d = unit(*d); up = np.array(up, float)
    a = np.cross(d, up); a /= np.linalg.norm(a); b = np.cross(a, d)
    base = np.array(base, float)
    return hull([base + a * r, base - a * r, base + b * r, base - b * r, base + d * length])


def tufts(k, name, colour, bone, places, mirror=True, surface=None):
    """Sharp jags of fur: (base, direction, length, root radius) each, solid hulls."""
    for i, (b, d, L, r) in enumerate(places):
        k.add(f"{name}{i}", claw(b, d, L, r, up=(0, 0, 1) if abs(unit(*d)[2]) < 0.9 else (0, 1, 0)), colour, bone,
              fuse=False, mirror=mirror, surface=surface)


def noibat(src, out):
    k = Kit(src, coat="fur")
    # the head: round, a little broader at the cheeks, its lower front drawn a touch forward
    k.add("head", ell(H, HR), LILAC, "head")
    k.add("cheeks", ell((0, -0.016, 0.196), (0.05, 0.036, 0.03)), LILAC, "head")

    # the ears: great thick dishes up and out from the top of the head, pointed at their outer ends
    tear = tear_outline(0.62)
    centre = EAR_ROOT + EAR_D * (EAR[0] * 0.92)
    ear_dish(k, "ear", "ear{s}", centre, EAR_D, EAR_FACE, EAR, EARRIM,
             [(0.8, EARRING, tear, 0.0025), (0.47, EARCORE, tear_outline(0.55), 0.005)],
             shape=teardrop(0.7), front=0.4)

    # the ruff: a dark shaggy collar round the neck, jags of fur standing out and down from it
    k.add("ruff", ell((0, 0.004, 0.164), (0.046, 0.04, 0.026)), RUFF, "body")
    places = []
    for a in (-60, -25, 10, 45, 80, 115, 150):
        t = math.radians(a)
        dx, dy = math.cos(t), math.sin(t)
        places.append(((0.04 * dx, 0.004 + 0.034 * dy, 0.16), (dx, dy, -0.55), 0.022, 0.009))
    tufts(k, "ruffjag", RUFF, "body", places)
    k.add("ruffjagF", claw((0, -0.034, 0.162), (0, -0.6, -1), 0.026, 0.01), RUFF, "body", fuse=False)
    # the body: small and dark under the ruff, dark tufts round the hips
    k.add("waist", ell((0, 0.004, 0.135), (0.032, 0.028, 0.032)), RUFF, "body")
    k.add("hips", ell((0, 0.006, 0.108), (0.038, 0.032, 0.026)), RUFF, "body")
    hp = []
    for a in (-75, -35, 5, 45, 90, 135):
        t = math.radians(a)
        dx, dy = math.cos(t), math.sin(t)
        hp.append(((0.032 * dx, 0.006 + 0.028 * dy, 0.1), (dx, dy, -0.9), 0.02, 0.008))
    tufts(k, "hipjag", RUFF, "body", hp)

    # the jags of pale fur hanging under the cheeks
    jags = [((0.03, -0.034, 0.184), (0.3, -0.35, -1), 0.022, 0.01),
            ((0.046, -0.012, 0.186), (0.6, 0.0, -1), 0.02, 0.009),
            ((0.012, -0.044, 0.18), (0.05, -0.4, -1), 0.016, 0.008)]
    tufts(k, "jag", LILAC, "head", jags, surface="fur")

    # the legs: short, pale, hanging under the hips with small pointed feet
    k.add("leg", tube([(0.019, 0.004, 0.1), (0.021, 0.002, 0.065), (0.022, -0.002, 0.04)], [0.011, 0.009, 0.008]),
          LILAC, "leg{s}", mirror=True)
    k.add("foot", ell((0.022, -0.008, 0.034), (0.009, 0.014, 0.007), rot=(15, 0, 0)), LILAC, "leg{s}", mirror=True)

    # the wings: pale plates raised in a shallow V from the shoulders, the trailing edge scalloped
    w = sibling("woobat")
    root = np.array((0.0, 0.0)); top = np.array((0.05, 0.022)); tip = np.array((0.152, 0.008))
    pts = [np.array((0.125, -0.042)), np.array((0.085, -0.062)), np.array((0.044, -0.088))]
    outline = w.wing_outline(root, top, tip, pts + [np.array((0.014, -0.085))], np.array((0.06, -0.01)),
                             lead=0.15, depth=0.35)
    WEX, WEZ = unit(1, 0.28, 0.5), unit(-0.42, 0.15, 1)
    origin = np.array((0.04, 0.0, 0.156))
    wing_plate(k, "wing", "wing{s}", origin, WEX, WEZ, outline, 0.011, LILAC)
    ex = WEX; ez = WEZ - ex * (WEZ @ ex); ez /= np.linalg.norm(ez)
    corner = origin + ex * 0.024 + ez * -0.088
    for j, dx in enumerate((-0.006, 0.006)):
        k.add(f"wclaw{j}", claw(corner + ex * dx, (dx * 20, -0.3, -1), 0.016, 0.0055), RUFF, "wing{s}",
              fuse=False, mirror=True, surface="shell")

    # a little darker lilac under the head and on the waist's lower edge
    k.paint(lambda P: smoothstep(H[2] - 0.03, H[2] - 0.05, P[:, 2]) * inside(P[:, 2] - 0.21, 0.004)
            * inside(0.15 - P[:, 2], 0.004), LILACLOW, on=["head", "cheeks"])

    # the mark: a dark purple shape over the forehead, coming down to a point between the eyes, its lobes over them
    MK = polygon([(-1.0, 0.25), (-0.72, 0.62), (-0.3, 0.5), (0.0, 0.7), (0.3, 0.5), (0.72, 0.62), (1.0, 0.25),
                  (0.62, -0.08), (0.18, -0.3), (0.0, -0.95), (-0.18, -0.3), (-0.62, -0.08)])
    mn = unit(0, -1, 0.55)
    k.decal(H + mn / np.linalg.norm(mn / HR), mn, (0.04, 0.032), MARK, outline=MK, rings=16, segs=72, eps=0.003)

    # the mouth: small and dark, a white fang hanging from its top middle
    CM, CF = srgb(MOUTH), srgb(WHITE)
    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        fang = (v > -0.3) & (np.abs(u) < 0.3 * np.clip((v + 0.3) / 1.0, 0, 1))
        c[fang] = CF
        return c
    MO = polygon([(-1, 0.55), (0, 0.7), (1, 0.55), (0.6, -0.4), (0, -0.8), (-0.6, -0.4)])
    md = unit(0, -1, -0.15)
    k.decal((0, -0.06, 0.196), md, (0.016, 0.012), mouth, outline=MO, rings=14, segs=56, eps=0.003)

    k.build(voxel=VOXEL, smooth=3, band=0.005, wband=0.03)

    # the eyes: big, yellow, a black rim and slit, low and wide on the face under the mark's lobes
    LID, INK = srgb(LILAC), srgb(EYEINK)
    ED = unit(0.56, -0.8, -0.06)
    E = H + ED / np.linalg.norm(ED / HR)
    def eyes(state):
        k.decal(E, ED, (0.021, 0.023), eye_look(state, slit_eye(), LID, INK, line=0.17), rings=20, segs=56,
                eps=0.004, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    noibat(*args())
