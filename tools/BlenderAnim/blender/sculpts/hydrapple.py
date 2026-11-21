# Hydrapple sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/hydrapple.py -- <the export's Hydrapple.glb> <out.glb>
#
# Grown from Dipplin's script (sibling("dipplin")): its candy apple (the syrup's coat hanging in drips, the puddle), its
# worm's tail with two leaves, the line's reds and greens.
# From the references: a great candy apple standing in its puddle, the syrup's bright coat over its top third hanging
# in round drips, one long drip down its right (+X) side; round its darker lower half two rows of lighter wavy lines;
# on its shoulders pale green leaf-shaped marks. Out of its top a thick dragon's neck winds up in an S, green above with
# a cream belly down its front, scalloped where the two meet; at its top a dragon's head looking ahead, its jaws open,
# a red forked tongue out, big round yellow eyes on its sides with a red ring and a dark red pupil, two small green
# fins behind them; across its mouth it carries a stick with a little red candy apple and a leaf at its far end. Out of
# the apple's foot on the right (+X) its green tail curls, cream under it, two dark green leaves at its tip.
# (The task gave it "seven small worm heads on syrup necks": the pictures, and the skeleton's one neck and one head,
# show one serpent with one head; the leaf marks on the shoulders are kept, as marks.)
# The skeleton: body (0, 0, 0.22), neck (0, 0, 0.39), head (0, -0.1, 0.92), tail (0.18, 0.06, 0.08).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, eye_look, args, outward, sibling

dp = sibling("dipplin")
a = dp.a

GLAZE, RED_DEEP, PUDDLE, WAVE = dp.GLAZE, dp.RED_DEEP, dp.PUDDLE, "#C93038"
MARK = "#A8D06C"
GREEN, BELLY, FIN = "#7CA84A", "#E6DCA2", "#6A9A3E"
MOUTH, TONGUE = "#6E1A24", "#D8404C"
EYE_Y, EYE_RIM, PUPIL = "#F4E68A", "#D84A2E", "#7A1418"
STICK, CANDY, CANDY_LEAF = "#6A4630", "#D8302C", "#4E8A34"

# --- the frame (about 1.0 tall) -----------------------------------------------------------------------------------
APPLE_C = np.array([0.0, 0.0, 0.216])
APPLE_R = np.array([0.215, 0.205, 0.19])
PUDDLE_C, PUDDLE_R = np.array([0.0, -0.006, 0.02]), np.array([0.29, 0.272, 0.022])
GLAZE_BASE = 0.36
DRIPS = [(-2.6, 0.22, 0.3), (-1.8, 0.24, 0.42), (-1.0, 0.22, 0.3), (-0.3, 0.24, 0.4), (0.45, 0.22, 0.28),
         (1.25, 0.16, 0.95), (1.9, 0.22, 0.34), (2.55, 0.24, 0.42)]
NECK = [(0.0, 0.01, 0.34), (0.0, 0.02, 0.44), (-0.066, 0.02, 0.52), (-0.08, 0.012, 0.6), (-0.012, 0.0, 0.676),
        (0.066, -0.012, 0.75), (0.07, -0.032, 0.822), (0.026, -0.06, 0.87), (0.0, -0.08, 0.89)]
NECK_R = [0.056, 0.054, 0.052, 0.05, 0.049, 0.047, 0.046, 0.046, 0.046]
HC, HR = np.array([0.0, -0.12, 0.905]), np.array([0.086, 0.1, 0.072])
TAIL_PTS = [(0.15, 0.05, 0.1), (0.25, 0.08, 0.07), (0.33, 0.06, 0.075), (0.37, 0.02, 0.115)]
TAIL_R = [0.042, 0.034, 0.027, 0.022]
VOXEL = 0.0065


def waves(k):
    """Two rows of lighter wavy lines round the apple's lower half."""
    def m(P):
        th, h, nr = a.skin_coords(P, APPLE_C, APPLE_R)
        out_ = np.zeros(len(P))
        for row, ph in ((-0.28, 0.0), (-0.56, 0.5)):
            wv = a.scallop(th, 9, row - 0.07, row + 0.05, ph)
            out_ = np.maximum(out_, inside(np.abs(h - wv) - 0.03, 0.025))
        # only on the bare apple, never over the syrup's drips
        return out_ * (nr > 0.6) * (nr < 1.35) * (1 - inside(dp.glaze_edge(th, GLAZE_BASE, DRIPS) - h, 0.03))
    k.paint(m, WAVE, on=["apple"])


def shoulder_marks(k):
    """Pale green leaf-shaped marks lying on the apple's shoulders, each along the apple's own surface."""
    for j, ang in enumerate((-70, 8, 82, 155, -145)):
        t = math.radians(ang)
        h = 0.58
        d = np.array([math.sin(t) * math.sqrt(1 - h * h), -math.cos(t) * math.sqrt(1 - h * h), h])
        p, n = a.on_ell(APPLE_C, APPLE_R, d)
        q = (p[2] - APPLE_C[2]) / APPLE_R[2]
        s = 1 + 0.035 * max(1 - (q - 0.25) ** 2 * 1.6, 0)
        p = np.array([p[0] * s, p[1] * s, p[2]])
        down = np.array([0, 0, -1.0]) - n * (-n[2])
        down /= np.linalg.norm(down)
        side = np.cross(n, down)
        def leafy(V):
            V = V.copy()
            tt = (V[:, 2] + 1) / 2
            w = np.sqrt(np.clip(1 - tt, 0, 1)) * (0.55 + 0.9 * tt) * (1 - tt ** 2.2)
            V[:, 0] *= np.clip(w * 1.3, 0.05, 1.2)
            return V
        V, F = ell((0, 0, 0), (0.05, 0.011, 0.05), deform=leafy, sub=3)
        V = V @ np.stack([side, n, down], axis=1).T + p + n * 0.003 + down * 0.012
        k.add(f"mark{j}", outward((V, F)), MARK, "body", fuse=False, surface="shell")


def neck(k):
    k.add("neck", tube(NECK, NECK_R), GREEN, "neck", surface="scales")
    P = np.array(NECK, float)
    t = np.linspace(0, len(P) - 1, 160)
    CL = np.stack([np.interp(t, np.arange(len(P)), P[:, j]) for j in range(3)], axis=1)
    S = np.r_[0, np.cumsum(np.linalg.norm(np.diff(CL, axis=0), axis=1))]
    T = np.gradient(CL, axis=0); T /= np.linalg.norm(T, axis=1, keepdims=True)
    fwd = np.array([0, -1.0, 0]) - T * (-T[:, 1:2])
    fwd /= np.linalg.norm(fwd, axis=1, keepdims=True)
    def belly(Q):
        d = ((Q[:, None, :] - CL[None]) ** 2).sum(2)
        i = d.argmin(1)
        o = Q - CL[i]
        o /= np.maximum(np.linalg.norm(o, axis=1, keepdims=True), 1e-9)
        c = (o * fwd[i]).sum(1)
        edge = 0.32 + 0.34 * np.abs(np.sin(math.pi * S[i] / 0.075))
        return inside(edge - c, 0.06) * (Q[:, 2] > 0.36)
    k.paint(belly, BELLY, on=["neck"])


def head(k):
    k.add("skull", ell(HC, HR), GREEN, "head", surface="scales")
    k.add("snout", ell(HC + (0, -0.092, 0.006), (0.064, 0.08, 0.041), rot=(-8, 0, 0)), GREEN, "head", surface="scales")
    k.add("jaw", ell(HC + (0, -0.08, -0.067), (0.057, 0.077, 0.025), rot=(14, 0, 0)), BELLY, "head", surface="scales")
    k.add("maw", ell(HC + (0, -0.085, -0.035), (0.048, 0.068, 0.03)), MOUTH, "head", surface="skin")
    k.paint(lambda P: inside(P[:, 2] - (HC[2] - 0.018), 0.004) * (P[:, 1] < HC[1] - 0.05)
            * inside(np.abs(P[:, 0]) - 0.075), BELLY, on=["snout"])
    # the tongue, red and forked, out and down
    root, tip = HC + (0, -0.135, -0.04), HC + (0.0, -0.215, -0.078)
    k.add("tongue", hull([root + (0.018, 0, 0.008), root - (0.018, 0, 0.008), root + (-0.018, 0, 0.008),
                          root + (0.018, 0, -0.008), tip + (0.016, 0, 0.008), tip + (-0.016, 0, 0.008),
                          tip + (0.016, 0, -0.008), tip + (-0.016, 0, -0.008)]), TONGUE, "head", fuse=False,
          surface="skin")
    for s in (1, -1):
        e = tip + (s * 0.034, -0.042, -0.02)
        k.add(f"fork{s}", hull([tip + (s * 0.002, 0.01, 0.008), tip + (s * 0.018, 0.01, 0.008),
                               tip + (s * 0.002, 0.01, -0.008), tip + (s * 0.018, 0.01, -0.008),
                               e + (0, 0, 0.004), e - (0, 0, 0.004)]), TONGUE, "head", fuse=False, surface="skin")
    # the fins behind the eyes
    for s in (1, -1):
        b = HC + (s * 0.058, 0.035, 0.035)
        k.add(f"fin{s}", hull([b + (0, -0.02, 0.0), b + (0, 0.02, 0.0), b + (s * 0.012, 0, 0.012),
                              b + (-s * 0.008, 0, -0.008), b + (s * 0.045, 0.05, 0.035)]), FIN, "head", fuse=False,
              surface="scales")


def stick(k):
    """The stick carried across its mouth, the little candy apple and its leaf at the far end."""
    p0, p1 = np.array([-0.08, -0.215, 0.868]), np.array([0.22, -0.06, 0.93])
    k.add("stick", tube([p0, (p0 + p1) / 2, p1], [0.013, 0.013, 0.013]), STICK, "head", fuse=False, surface="leather")
    c = p1 + unit(*(p1 - p0)) * 0.005
    k.add("candy", outward(ell(c, (0.042, 0.042, 0.039), sub=3)), CANDY, "head", fuse=False, surface="shell")
    lb = c + (0.0, 0.0, 0.036)
    k.add("candyleaf", hull([lb + (0, -0.006, 0), lb + (0, 0.006, 0), lb + (0.012, 0, 0.004),
                             lb + (0.05, 0.004, 0.022), lb + (0.03, -0.004, 0.026)]), CANDY_LEAF, "head",
          fuse=False, surface="leaf")


def eye_open(u, v):
    c = np.tile(srgb(EYE_RIM), (len(u), 1))
    rr = u * u + v * v
    c[rr < 0.78 ** 2] = srgb(EYE_Y)
    c[(u / 0.26) ** 2 + (v / 0.42) ** 2 < 1] = srgb(PUPIL)
    return c


EYE_D, EYE_HALF = (0.62, -0.68, 0.42), (0.034, 0.034)


def lay_eyes(k, state):
    p, n = a.on_ell(HC, HR, EYE_D)
    k.decal(p, n, EYE_HALF, eye_look(state, eye_open, srgb(GREEN), srgb(PUPIL), line=0.17), up=(0, 0, 1), eps=0.002,
            rings=20, segs=56, mirror=True)


def hydrapple(src, out_path):
    k = Kit(src, coat="scales")
    dp.candy_apple(k, APPLE_C, APPLE_R, PUDDLE_C, PUDDLE_R, GLAZE_BASE, DRIPS)
    waves(k)
    shoulder_marks(k)
    neck(k)
    head(k)
    stick(k)
    dp.worm_tail(k, TAIL_PTS, TAIL_R, leaf_len=0.085, leaf_w=0.026, green=GREEN, belly=BELLY)
    k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.05)
    k.eye_states(lambda state: lay_eyes(k, state))
    k.export(out_path)


if __name__ == "__main__":
    src, out_path = args()
    hydrapple(src, out_path)
