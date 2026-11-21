# Pansage sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pansage.py -- <the export's Pansage.glb> <out.glb>
#
# The small monkey of the elemental three, written to be grown from: small(src, out, P) takes the colours and the head
# tuft (and the tail's tip, the face's edge, the eyes and the mouth), so Pansear is this with its own dictionary
# (sibling("pansage")). From the references: a big round head, wider than tall, green above and cream below, the green
# coming down in a V between the eyes; big round eyes ringed dark with large black pupils, set on that edge; a small
# open smiling mouth with a pink tongue; big round green ears out at the sides with pale green linings; on the crown a
# short stem and a broccoli cloud of green lumps with five yellow seeds fanned on its front; a narrow cream torso and
# cream arms with big mitten hands (held up in the art, carried forward and a little down here); green shorts, short
# green legs and feet; a thin green tail out to the +X side, curling up, tipped with a three-lobed tuft of leaf.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, slab, srgb, inside, smoothstep, unit, boxy, polygon, eye_look, args

CREAM, INK = "#F2D98E", "#1C1A1E"
MOUTH, TONGUE = "#7A2430", "#E87D8C"
WHITE = np.array([1.0, 1.0, 1.0])

H = np.array([0, -0.005, 0.325])          # the head's centre
HR = np.array([0.124, 0.104, 0.096])      # its half sizes: wider than tall


def on_ell(C, R, d, lift=0.0):
    """The point of an ellipsoid's surface in a direction from its centre."""
    d = unit(*d)
    s = 1 / math.sqrt(((d / np.array(R)) ** 2).sum())
    return np.array(C) + d * (s + lift)


def turned(rot, v):
    return np.array(Euler([math.radians(a) for a in rot]).to_matrix()) @ np.array(v, float)


# --- the ears: a flat round ear fused to the head's side, its lining a loose plate on its front -----------------------
def ear(k, c, r, rot, main, lining, outline=None):
    k.add("ear", ell(c, r, rot=rot, deform=boxy(2.4)), main, "ear{s}", mirror=True)
    n = turned(rot, (0, -1, 0))
    if outline is None:
        piece = ell(np.array(c) + n * r[1] * 0.74, (r[0] * 0.74, 0.006, r[2] * 0.76), rot=rot, sub=3)
    else:
        piece = slab([(a * r[0], b * r[2]) for a, b in outline], np.array(c) + n * r[1] * 0.86,
                     turned(rot, (1, 0, 0)), turned(rot, (0, 0, 1)), 0.008)
    k.add("lining", piece, lining, "ear{s}", fuse=False, mirror=True)


# --- the face's colours: cream below an edge, given as a height over the head's centre by |x| ------------------------
def face_paint(k, C, R, top, cream):
    C, R = np.array(C), np.array(R)
    def mask(P):
        q = P - C
        ax = np.abs(q[:, 0])
        near = inside(np.sqrt(((q / (R * 1.3)) ** 2).sum(1)) - 1, 0.01) * inside(ax - R[0] * 0.93, 0.004)
        front = inside(q[:, 2] - top(ax), 0.0025) * inside(q[:, 1] + 0.3 * R[1], 0.004)
        chin = inside(q[:, 2] + 0.62 * R[2], 0.004) * inside(q[:, 1] - 0.02, 0.004)
        return np.maximum(front, chin) * near
    k.paint(mask, cream)


def sage_top(ax):
    """Pansage: the cream rises over each eye; the green comes down in a V between them."""
    return np.minimum(-0.02 + 2.3 * ax, 0.052 - 4.0 * (ax - 0.052) ** 2)


# --- the waist: the colour below an edge round the body, cream above it, both crisp ------------------------------------
def waist_paint(k, waist, main, cream, reach, band=0.05):
    def at(P):
        th = np.arctan2(P[:, 0], -P[:, 1])
        return P[:, 2] - waist(th), inside(np.hypot(P[:, 0], P[:, 1]) - reach, 0.004)
    k.paint(lambda P: (lambda d, r: inside(d, 0.0025) * inside(-d - band, 0.004) * r)(*at(P)), main)
    k.paint(lambda P: (lambda d, r: inside(-d, 0.0025) * inside(d - band, 0.004) * r)(*at(P)), cream)


def flat_waist(th):
    return np.full_like(th, 0.128)


# --- the eyes ----------------------------------------------------------------------------------------------------------
def round_eye(u, v):
    """A white ringed dark with a large black pupil."""
    c = np.tile(srgb(INK), (len(u), 1))
    c[u * u + v * v < 0.775 ** 2] = WHITE
    c[((u + 0.06) / 0.5) ** 2 + ((v + 0.08) / 0.6) ** 2 < 1] = srgb(INK)
    return c


# --- the mouths --------------------------------------------------------------------------------------------------------
def open_mouth():
    CM, CT = srgb(MOUTH), srgb(TONGUE)
    def look(u, v):
        c = np.tile(CM, (len(u), 1))
        c[(u / 0.62) ** 2 + ((v + 0.8) / 0.62) ** 2 < 1] = CT
        return c
    def shape(th):                       # a D: a flat top, a round bottom
        s = math.sin(th)
        return min(1.0, 0.3 / s) if s > 0 else 1.0
    return look, shape, (0.022, 0.024), (0, -1, -0.4)


# --- the head's tufts ----------------------------------------------------------------------------------------------------
def broccoli(k, P, top):
    """A short stem and a cloud of green lumps, five yellow seeds fanned on its front."""
    main = P["main"]
    k.add("stem", tube([top + (0, 0, -0.03), top + (0, 0.004, 0.035)], [0.036, 0.03]), main, "head")
    C = top + np.array([0, 0.008, 0.1])
    lumps = [((0, -0.012, -0.004), (0.084, 0.064, 0.062)),            # the big front lump the seeds lie on
             ((0, 0.03, 0.03), (0.07, 0.06, 0.055)),
             ((0.085, 0.008, -0.004), (0.058, 0.055, 0.052)),
             ((-0.085, 0.008, -0.004), (0.058, 0.055, 0.052)),
             ((0.135, 0.012, -0.022), (0.04, 0.042, 0.04)),
             ((-0.135, 0.012, -0.022), (0.04, 0.042, 0.04)),
             ((0.045, 0.01, 0.045), (0.05, 0.05, 0.046)),
             ((-0.045, 0.01, 0.045), (0.05, 0.05, 0.046)),
             ((0, 0.045, -0.03), (0.07, 0.05, 0.045))]
    for i, (o, r) in enumerate(lumps):
        k.add(f"cloud{i}", ell(C + np.array(o), r), main, "head")
    F, FR = C + np.array(lumps[0][0]), np.array(lumps[0][1])
    O = np.array([0, 0, -0.036])                     # where the seeds fan from, low on the front lump
    for i, a in enumerate((-64, -32, 0, 32, 64)):
        d = np.array([math.sin(math.radians(a)), 0, math.cos(math.radians(a))])
        q = O + d * 0.04
        at = on_ell(F, FR, (q[0] / FR[0], -0.9, q[2] / FR[2]))
        n = unit(*((at - F) / FR ** 2))
        k.decal(at, n, (0.009, 0.019), P["seed"], up=tuple(d), eps=0.004, rings=8, segs=28)
    return C[2] + 0.1


def leaf_tip(k, P, end, plane):
    """The tail's tuft: three lobes of leaf, in a plane facing back and out."""
    side = np.cross(plane, (0, 0, 1.0)); side /= np.linalg.norm(side)
    E = np.array(end)
    k.add("tiplobe0", ell(E + (0, 0, 0.042), (0.03, 0.022, 0.036)), P["main"], "tail")
    for i, s in enumerate((1, -1)):
        k.add(f"tiplobe{i + 1}", ell(E + side * s * 0.03 + (0, 0, 0.018), (0.027, 0.02, 0.03)), P["main"], "tail")


PANSAGE = dict(
    main="#3FAE5A", lining="#9ADDA6", seed="#F0DC6A",
    ear_c=(0.163, 0.012, 0.338), ear_r=(0.066, 0.022, 0.094), ear_rot=(0, 0, 26), ear_outline=None,
    tuft=broccoli, tip=leaf_tip, top=sage_top, waist=flat_waist,
    eye=round_eye, eye_half=(0.029, 0.037), eye_dir=(0.42, -0.88, 0.06), lid=CREAM,
    mouth=open_mouth,
)


def small(src, out, P):
    k = Kit(src, coat="fur")
    main = P["main"]

    # --- the head and a narrow pear of a body ----------------------------------------------------------------------
    k.add("head", ell(H, HR), main, "head")
    k.add("cheeks", ell(H + (0, -0.035, -0.035), (0.11, 0.07, 0.06)), main, "head")
    def pear(V):
        V = V.copy(); s = 1 - 0.16 * V[:, 2]
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("torso", ell((0, 0.0, 0.168), (0.058, 0.05, 0.078), deform=pear), CREAM, "body")
    k.add("neck", ell((0, -0.002, 0.232), (0.046, 0.042, 0.03)), CREAM, "body")
    k.add("hips", ell((0, 0.003, 0.112), (0.07, 0.058, 0.046)), main, "body")

    # --- short legs and small feet, in the colour --------------------------------------------------------------------
    k.add("leg", tube([(0.042, 0.0, 0.105), (0.052, -0.004, 0.062), (0.058, -0.008, 0.034)], [0.03, 0.026, 0.024]),
          main, "leg{s}", mirror=True)
    k.add("foot", ell((0.061, -0.022, 0.018), (0.032, 0.046, 0.019)), main, "leg{s}", mirror=True)
    waist_paint(k, P["waist"], main, CREAM, 0.095)

    # --- cream arms carried forward and a little down, big mitten hands, palms up -------------------------------------
    k.add("arm", tube([(0.034, -0.008, 0.204), (0.072, -0.046, 0.164), (0.094, -0.086, 0.122)], [0.025, 0.021, 0.02]),
          CREAM, "arm{s}", mirror=True)
    k.add("hand", ell((0.104, -0.104, 0.104), (0.032, 0.036, 0.022), rot=(28, 0, -14)), CREAM, "arm{s}", mirror=True)
    k.add("thumb", ell((0.084, -0.122, 0.118), (0.013, 0.017, 0.015), rot=(30, 0, -20)), CREAM, "arm{s}", mirror=True)

    # --- the ears ------------------------------------------------------------------------------------------------------
    ear(k, P["ear_c"], P["ear_r"], P["ear_rot"], main, P["lining"], P["ear_outline"])

    # --- the head's tuft -------------------------------------------------------------------------------------------------
    P["tuft"](k, P, H + np.array([0, 0.0, HR[2] - 0.004]))

    # --- the thin tail, out of the back and round to the +X side, curling up -------------------------------------------
    TP = [(0, 0.04, 0.095), (0.04, 0.11, 0.065), (0.11, 0.15, 0.072), (0.17, 0.14, 0.118), (0.2, 0.11, 0.186),
          (0.2, 0.1, 0.228)]
    k.add("tail", tube(TP, [0.016, 0.0155, 0.015, 0.015, 0.015, 0.016]), main, "tail")
    P["tip"](k, P, TP[-1], unit(0.6, 0.8, 0))

    # --- the face: cream below its edge, the mouth -------------------------------------------------------------------
    face_paint(k, H, HR, P["top"], CREAM)
    look, shape, half, dm = P["mouth"]()
    dm = unit(*dm)
    k.decal(on_ell(H + (0, -0.035, -0.035), (0.11, 0.07, 0.06), dm), dm, half, look, eps=0.003, outline=shape,
            rings=20, segs=64)

    # --- the eyes ----------------------------------------------------------------------------------------------------------
    de = unit(*P["eye_dir"])
    E = on_ell(H, HR, de)
    def eyes(state):
        k.decal(E, de, P["eye_half"], eye_look(state, P["eye"], srgb(P["lid"]), srgb(INK), line=0.16), eps=0.005,
                outline=P.get("eye_outline"), rings=20, segs=56, mirror=True)

    k.build(voxel=0.0052, band=0.005)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    small(*args(), PANSAGE)
