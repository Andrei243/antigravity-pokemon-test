# Plusle sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/plusle.py -- <the export's Plusle.glb> <out.glb>
#
# The design, from the references: a small cream mouse, a big round head over a short pear of a body; at the top of
# the head two round cream ear-roots, from which long red ears rise up and out, widest past their middle and pointed;
# round red cheeks, each with a cream plus; black eyes with a white glint; a wide open mouth with a pink tongue;
# short arms held forward, the paws red; round thighs and small feet; a thin tail ending in a red plus.
# Minun is built by the same function (sibling("plusle")): blue for red, a minus for a plus, rounded ears and a smile.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, slab, srgb, inside, unit, eye_look, polygon, args

CREAM, INK = "#F8ECB6", "#1C1A24"
RED = "#EA4652"
MOUTH, TONGUE = "#8C2632", "#EE7A88"

H = np.array([0, -0.012, 0.3])            # the head's centre
HR = np.array([0.108, 0.096, 0.092])

# The ears, from the root along the line, with their half-widths: Plusle's pointed, widest past the middle
PLUS_EAR = [0.018, 0.032, 0.043, 0.05, 0.051, 0.044, 0.026, 0.004]
ROUND_EAR = [0.018, 0.031, 0.041, 0.047, 0.049, 0.047, 0.04, 0.024]

PLUS = [(-1, 0.32), (-0.32, 0.32), (-0.32, 1), (0.32, 1), (0.32, 0.32), (1, 0.32), (1, -0.32), (0.32, -0.32),
        (0.32, -1), (-0.32, -1), (-0.32, -0.32), (-1, -0.32)]
MINUS = [(-1, 0.34), (1, 0.34), (1, -0.34), (-1, -0.34)]
UPRIGHT_MINUS = [(-0.32, 1), (0.32, 1), (0.32, -1), (-0.32, -1)]          # Minun's tail stands its bar upright


def on_head(d, lift=0.0):
    d = unit(*d)
    s = 1 / math.sqrt(((d / HR) ** 2).sum())
    return H + d * (s + lift)


def open_mouth():
    CM, CT = srgb(MOUTH), srgb(TONGUE)
    def look(u, v):
        c = np.tile(CM, (len(u), 1))
        c[(u / 0.6) ** 2 + ((v + 0.75) / 0.6) ** 2 < 1] = CT
        return c
    def shape(th):                       # a D: a flat top, a round bottom
        s = math.sin(th)
        return min(1.0, 0.35 / s) if s > 0 else 1.0
    return look, shape, (0.034, 0.03), unit(0, -1, -0.38)


def smile():
    CC, CI = srgb(CREAM), srgb(INK)
    def look(u, v):
        c = np.tile(CC, (len(u), 1))
        c[(np.abs(v - (-0.1 + 0.55 * (u * u - 0.5))) < 0.2) & (np.abs(u) < 0.85)] = CI
        return c
    return look, None, (0.026, 0.016), unit(0, -1, -0.34)


def mouse(src, out, tint=RED, sign=PLUS, ear=PLUS_EAR, mouth=open_mouth, tail_sign=None):
    k = Kit(src, coat="fur")

    # --- the head and a short pear of a body under it -----------------------------------------------------------
    k.add("head", ell(H, HR), CREAM, "head")
    def pear(V):
        V = V.copy(); s = 1 - 0.14 * V[:, 2]
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell((0, 0.004, 0.13), (0.074, 0.07, 0.1), deform=pear), CREAM, "body")
    k.add("chest", ell((0, 0.0, 0.205), (0.066, 0.056, 0.05)), CREAM, "body")

    # --- legs: round thighs and small feet ----------------------------------------------------------------------
    k.add("thigh", ell((0.048, -0.008, 0.065), (0.042, 0.046, 0.05)), CREAM, "leg{s}", mirror=True)
    k.add("foot", ell((0.054, -0.036, 0.018), (0.031, 0.046, 0.019)), CREAM, "leg{s}", mirror=True)

    # --- short arms held forward and a little down, the paws in the colour ---------------------------------------
    k.add("arm", tube([(0.058, -0.008, 0.19), (0.088, -0.045, 0.17), (0.1, -0.072, 0.152)], [0.026, 0.024, 0.023]),
          CREAM, "arm{s}", mirror=True)
    k.add("paw", ell((0.104, -0.082, 0.146), (0.03, 0.032, 0.029)), tint, "arm{s}", mirror=True)

    # --- the ears: round cream roots on the head's top corners, long flat ears in the colour rising up and out ----
    root = np.array((0.068, 0.0, 0.368))
    k.add("earroot", ell(root, (0.036, 0.03, 0.034)), CREAM, "ear{s}", mirror=True)
    d = unit(0.5, 0.06, 0.87)
    n = len(ear)
    pts = [root + d * (0.02 + 0.21 * i / (n - 1)) for i in range(n)]
    k.add("ear", blade(pts, ear, (0, 1, 0), 0.42), tint, "ear{s}", mirror=True)

    # --- the tail: a thin cream cord back and out to the sign, a flat plate turned toward the front three-quarter --
    k.add("tail", tube([(0, 0.05, 0.1), (0.03, 0.1, 0.085), (0.062, 0.14, 0.1), (0.085, 0.165, 0.125)],
                       [0.016, 0.015, 0.015, 0.015]), CREAM, "tail")
    k.add("sign", slab([(0.058 * a, 0.058 * b) for a, b in (tail_sign or sign)], (0.1, 0.18, 0.15), unit(0.6, 0.8, 0), (0, 0, 1),
                       0.03), tint, "tail", fuse=False, surface="shell")

    # --- the face: round cheeks with the sign in cream, the mouth -----------------------------------------------
    dc = unit(0.8, -0.48, -0.26)
    k.decal(on_head(dc), dc, (0.034, 0.034), tint, eps=0.003, rings=12, segs=48, mirror=True)
    k.decal(on_head(dc), dc, (0.024, 0.024), CREAM, eps=0.006, outline=polygon(sign), rings=10, segs=120, mirror=True)
    look, shape, half, dm = mouth()
    k.decal(on_head(dm), dm, half, look, eps=0.003, outline=shape, rings=20, segs=64)

    # --- black eyes with a white glint --------------------------------------------------------------------------
    CC, CI = srgb(CREAM), srgb(INK)
    def eye(u, v):
        c = np.tile(CI, (len(u), 1))
        c[((u - 0.1) / 0.36) ** 2 + ((v - 0.36) / 0.32) ** 2 < 1] = (1, 1, 1)
        return c
    de = unit(0.44, -0.88, 0.16)
    E = on_head(de)
    def eyes(state):
        k.decal(E, de, (0.022, 0.028), eye_look(state, eye, CC, CI), eps=0.004, rings=20, segs=56, mirror=True)

    k.build(voxel=0.005, band=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    mouse(*args())
