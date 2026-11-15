# Arbok sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/arbok.py -- <the export's Arbok.glb> <out.glb>
# Ekans grown (ekans.py: the same line through the chain of joints, its frame and its seams). From the references: a
# great grey-lavender cobra reared up from a loose coil on the ground, its thin tail curling up at the end, darker
# seams across the body all along. A broad hood spread behind the neck, its front a fearsome face in bold patches: a
# black brow peaked at the middle with a small red mark, two red eyes rimmed in black with yellow hearts, and a wide
# black grin below with a wavy lower edge; behind, the hood is plain. Over the hood's top a flat, wedge-shaped head with
# small fierce eyes, white with a black pupil under a scowl, its jaws open on two white fangs and a pink forked tongue.
# No rattle, no yellow band (the pictures show neither).
# The export's chain: body (0.34, 0.36, 0.05) at the tail's end, seg1 (0.139, -0.236, 0.075) round the coil to seg7
# (-0.08, -0.135, 0.165), seg8 (0, -0.1, 0.44) up the neck, hood (0, -0.06, 0.6), head (0, -0.06, 0.72).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, polygon, args, outward, sibling

e = sibling("ekans")

PURPLE, SEAM, MOUTH, FANG, TONGUE = "#9C8EC0", "#6C5E90", "#D8707E", "#F6F4F2", "#E8848E"
BLACK, RED, GOLD = "#241E2C", "#D8404A", "#F2C834"

LINE = [(0.3, 0.425, 0.155), (0.35, 0.425, 0.095), (0.365, 0.395, 0.045), (0.34, 0.36, 0.05), (0.29, 0.1, 0.07),
        (0.139, -0.236, 0.075), (-0.162, -0.196, 0.09), (-0.221, 0.08, 0.105), (0.0, 0.215, 0.12),
        (0.184, 0.066, 0.135), (0.112, -0.136, 0.15), (-0.08, -0.135, 0.165), (-0.06, -0.13, 0.3),
        (0.0, -0.1, 0.44), (0.0, -0.075, 0.6), (0.0, -0.07, 0.7)]
RADS = [0.014, 0.022, 0.03, 0.036, 0.05, 0.06, 0.064, 0.066, 0.067, 0.068, 0.069, 0.07, 0.072, 0.07, 0.064, 0.06]
BONES = ["body", "body", "body", "body", "body", "seg1", "seg2", "seg3", "seg4", "seg5", "seg6", "seg7", "seg7",
         "seg8", "seg8"]
HC = np.array([0.0, -0.115, 0.755])          # the head's middle
HS = 1.25                                      # its size: big enough to read in the game

# --- the hood: a broad shield behind the neck, widest at two thirds up, its edges curved forward ----------------------
HOOD = np.array([0.0, -0.045, 0.6])
HOOD_R = np.array([0.26, 0.032, 0.205])
WIDE = HOOD_R[0] / 0.23                      # the face below is drawn for a hood 0.23 wide: stretched to this one
BEND = 0.06                                    # how far forward the hood's edges come


def hood_shape(V):
    V = V.copy()
    z = V[:, 2]
    V[:, 0] *= 0.62 + 0.38 * np.clip(1 - ((z - 0.25) / 1.0) ** 2, 0, 1) + 0.12 * np.clip(z, 0, 1)
    V[:, 0] *= np.where(z < -0.4, 1 - 0.55 * (-0.4 - z) / 0.6, 1)    # narrowing to the neck below
    return V


def hood_bend(V):
    V = V.copy()
    V[:, 1] -= (BEND / HOOD_R[1]) * V[:, 0] ** 2               # the edges curve forward round the neck
    return V


def front_mask(Q):
    """Points on the hood's front, in the hood's plane: (x, z) and whether in front of its middle sheet."""
    mid = HOOD[1] - BEND * (Q[:, 0] / HOOD_R[0]) ** 2
    return (Q[:, 1] < mid - 0.006).astype(float)


def poly_sd(x, z, pts):
    """Signed distance from points (x, z) to a polygon, below nought inside."""
    p = np.array(pts, float)
    q = np.stack([x, z], 1)
    d = np.full(len(x), 9.0)
    ins = np.zeros(len(x), bool)
    for a, b in zip(p, np.roll(p, -1, axis=0)):
        ab = b - a
        t = np.clip(((q - a) @ ab) / (ab @ ab), 0, 1)
        d = np.minimum(d, np.linalg.norm(q - (a + t[:, None] * ab), axis=1))
        cond = ((a[1] > q[:, 1]) != (b[1] > q[:, 1]))
        xs = a[0] + (q[:, 1] - a[1]) * ab[0] / (ab[1] + 1e-12)
        ins ^= cond & (q[:, 0] < xs)
    return np.where(ins, -d, d)


def almond(cx, cz, w, h, tilt, n=24):
    """An almond (pointed at both ends) about (cx, cz), turned by tilt degrees."""
    t = np.linspace(0, 2 * math.pi, n, endpoint=False)
    x = w * np.cos(t)
    z = h * np.sin(t) * np.abs(np.sin(t)) ** 0.15 * (1 - 0.25 * np.cos(t))
    c, s = math.cos(math.radians(tilt)), math.sin(math.radians(tilt))
    return np.stack([cx + c * x - s * z, cz + s * x + c * z], 1)


def mirror_x(pts):
    p = np.array(pts, float) * np.array([-1, 1.0])
    return p[::-1]


# The face on the hood, in its plane (x across, z up): the brow peaked at the middle, the eyes, the grin
BROW = [(-0.205, 0.735), (-0.12, 0.745), (-0.03, 0.715), (0.0, 0.69), (0.03, 0.715), (0.12, 0.745), (0.205, 0.735),
        (0.2, 0.705), (0.12, 0.712), (0.035, 0.676), (0.0, 0.645), (-0.035, 0.676), (-0.12, 0.712), (-0.2, 0.705)]
DIAMOND = [(0.0, 0.708), (0.014, 0.69), (0.0, 0.672), (-0.014, 0.69)]
GRIN = [(-0.2, 0.585), (-0.13, 0.555), (-0.04, 0.545), (0.04, 0.545), (0.13, 0.555), (0.2, 0.585),
        (0.19, 0.53), (0.15, 0.49), (0.1, 0.505), (0.05, 0.47), (0.0, 0.49), (-0.05, 0.47), (-0.1, 0.505),
        (-0.15, 0.49), (-0.19, 0.53)]
EYE_C, EYE_W, EYE_H, EYE_T = (0.115, 0.63), 0.07, 0.043, 16


def hood_paint(k):
    def on_front(fn):
        return lambda Q: front_mask(Q) * fn(Q[:, 0], Q[:, 2])
    # the brow and the grin are paint (the brow runs in under the head, where no patch could be cast)
    k.paint(on_front(lambda x, z: inside(poly_sd(x / WIDE, z, BROW), 0.0025)), BLACK)
    k.paint(on_front(lambda x, z: inside(poly_sd(x / WIDE, z, DIAMOND), 0.0025)), RED)
    k.paint(on_front(lambda x, z: inside(poly_sd(x / WIDE, z, GRIN), 0.0025)), BLACK)
    # the eyes are patches cast straight onto the hood's front, so that their edges are crisp: a black almond, a red
    # one over it, a gold heart over that
    for s in (1, -1):
        cx, cz = EYE_C[0] * s * WIDE, EYE_C[1]
        slope = 2 * BEND * cx / HOOD_R[0] ** 2
        n = unit(slope, -1, 0)
        c = np.array([cx, HOOD[1] - BEND * (cx / HOOD_R[0]) ** 2 - HOOD_R[1], cz])
        for shape, colour, lift in ((almond(0, 0, EYE_W * WIDE, EYE_H, EYE_T * s), BLACK, 0.004),
                                    (almond(0, 0, EYE_W * WIDE - 0.014, EYE_H - 0.013, EYE_T * s), RED, 0.006),
                                    (almond(0.014 * s, 0.006, EYE_W * WIDE - 0.046, EYE_H - 0.027, EYE_T * s), GOLD,
                                     0.008)):
            k.decal(c, n, (1.0, 1.0), colour, eps=lift, outline=polygon(shape), rings=10, segs=64)


# --- the head: flat and wedge-shaped, jaws open on two fangs ---------------------------------------------------------
def head(k):
    def hp(p):
        return HC + np.array(p, float) * HS
    def wedge(V):
        V = V.copy()
        t = np.clip(-V[:, 1], 0, 1)
        V[:, 0] *= 1 - 0.3 * t
        V[:, 2] *= 1 - 0.25 * t
        return V
    k.add("skull", ell(hp((0, 0, 0)), np.array((0.075, 0.1, 0.045)) * HS, rot=(-10, 0, 0), deform=wedge), PURPLE, "head")
    k.add("cheek", ell(hp((0, 0.03, -0.03)), np.array((0.068, 0.07, 0.045)) * HS), PURPLE, "head")
    k.add("chin", ell(hp((0, -0.04, -0.07)), np.array((0.052, 0.07, 0.025)) * HS, rot=(24, 0, 0)), PURPLE, "head")
    k.add("maw", ell(hp((0, -0.085, -0.04)), np.array((0.047, 0.07, 0.028)) * HS, rot=(14, 0, 0)), MOUTH, "head",
          carve=True)
    # two fangs from the upper jaw's front corners, down and a little back
    for s in (1, -1):
        a = hp((0.021 * s, -0.098, -0.03))
        b = hp((0.023 * s, -0.095, -0.062))
        k.add(f"fang{'R' if s > 0 else 'L'}", blade([a, (a + b) / 2, b], [0.009, 0.006, 0.0015], (0, 1, 0), 0.6),
              FANG, "head", fuse=False, surface="shell")
    # the forked tongue: out of the mouth, forward and down, split at its end
    t0, t1, t2 = hp((0, -0.07, -0.045)), hp((0, -0.135, -0.06)), hp((0, -0.165, -0.095))
    k.add("tongue", tube([t0, t1, t2], [0.008, 0.007, 0.006], seg=12), TONGUE, "head", fuse=False)
    for s in (1, -1):
        k.add(f"fork{'R' if s > 0 else 'L'}", tube([t2, t2 + np.array((0.012 * s, -0.012, -0.02)) * HS],
                                                  [0.006, 0.0035], seg=10), TONGUE, "head", fuse=False)
    return hp


def build(src, out):
    k = Kit(src, coat="scales")
    line = e.Line(LINE, RADS, rise=(0.2, 0.3))
    line.tubes(k, BONES, PURPLE)
    k.add("nape", tube([LINE[-2], LINE[-1], HC + (0, 0.04, -0.02)], [0.064, 0.06, 0.055]), PURPLE, "head")
    k.add("hood", ell(HOOD, HOOD_R, deform=lambda V: hood_bend(hood_shape(V)), sub=5),
          PURPLE, "hood")
    hp = head(k)

    k.paint(e.seams(line, 1.0, 13.4, 0.075, half=0.003, start=0.02), SEAM, on=["seg"])
    hood_paint(k)

    # the eyes: small, white with a black pupil, cut across the top by a scowl sloping to the snout
    INK, WHITE, LID = srgb("#141018"), srgb("#FFFFFF"), srgb(PURPLE)
    BA, BB = 0.3, 0.5                                    # the scowl: v = BA + BB u (u outward, toward the back)
    def cut(th):
        s = math.sin(th) - BB * math.cos(th)
        return min(1.0, BA / s) if s > 1e-6 else 1.0
    def eye(u, v):
        reach = np.array([cut(t) for t in np.arctan2(v, u)])
        rho = np.sqrt(u * u + v * v) / reach
        c = np.tile(WHITE, (len(u), 1))
        c[rho > 0.8] = INK
        c[((u + 0.25) / 0.34) ** 2 + ((v + 0.05) / 0.5) ** 2 < 1] = INK
        return c
    EC = hp((0.052, -0.035, 0.022))
    de = unit(0.7, -0.55, 0.45)
    half = (0.024 * HS, 0.02 * HS)
    def eyes(state):
        look = eye_look(state, eye, LID, INK, line=0.2)
        k.decal(EC, de, half, look, eps=0.005, rings=20, segs=56, outline=cut)
        m = np.array([-1, 1, 1.0])
        k.decal(EC * m, de * m, half, lambda u, v: look(-u, v), eps=0.005, rings=20, segs=56,
                outline=lambda th: cut(math.pi - th))

    k.build(voxel=0.0065, smooth=4)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    build(*args())
