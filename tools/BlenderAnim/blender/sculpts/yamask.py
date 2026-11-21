# Yamask sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/yamask.py -- <the export's Yamask.glb> <out.glb>
#
# The design, from the references: a small black ghost. Its head is a hood, round in front and rising behind into a
# pointed crest that leans up, back and a little to its left (the viewer's left); two great red eyes on the hood's
# front sides, each a red disc ringed darker with a small black pupil and a tear-like tail at its lower outer edge (the
# "Q"). Under the hood two thin black arms reach out and down to round fists of curled fingers. Its tail hangs from
# under the hood down behind a golden mask, which it holds, and curls round the mask's chin. The mask: a tall oval of
# dull gold, a face drawn on it in brown strokes (two lidded eyes, a long nose from the brows, a straight mouth).
# The art holds the arms flung out wide: here they are carried forward and a little down so the model is not wider
# than it is tall. The references show one eye from the three-quarter: the eyes are a pair, one each side.
# The skeleton: body (0, 0, 0.315), head (0, 0, 0.345), arms (+-0.03, 0, 0.315), tail (0, 0, 0.285).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, smoothstep, eye_look, args

# --- colours (Cofagrigus grows from these) ----------------------------------------------------------------------------
BLACK, BLACKSOFT = "#38363E", "#46444C"
GOLD, GOLDDEEP, GOLDLINE = "#DDB468", "#B98A42", "#6A4824"
RED, REDRIM, PUPIL = "#E2483E", "#9C2228", "#16121A"

# --- the frame --------------------------------------------------------------------------------------------------------
HOOD_C, HOOD_R = np.array([0.0, 0.012, 0.362]), np.array([0.112, 0.102, 0.098])
MASK_C, MASK_R = np.array([0.0, -0.045, 0.142]), np.array([0.07, 0.03, 0.104])
EYE_X, EYE_Z, EYE_HALF = 0.06, 0.372, 0.034
FIST = np.array([0.175, -0.108, 0.25])
VOXEL = 0.0045


def on_ell(c, r, x, z, front=-1):
    """A point of an ellipsoid's front (or back) at width x and height z, and its outward normal."""
    lx, lz = x / r[0], (z - c[2]) / r[2]
    ly = front * math.sqrt(max(1 - lx * lx - lz * lz, 0.01))
    p = c + np.array([lx, ly, lz]) * r
    return p, unit(lx / r[0], ly / r[1], lz / r[2])


def red_eye(tail=True, pupil=0.3):
    """The open eye: a red disc ringed darker, a small black pupil a little in and up, a tail at its lower outer edge."""
    red, rim, pup = srgb(RED), srgb(REDRIM), srgb(PUPIL)
    tail_on = tail
    hi = np.array([1.0, 0.86, 0.82])
    def look(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(red, (len(u), 1))
        c[(r > 0.8) & (r <= 1.02)] = rim
        tail = (u > 0.25) & (v < -0.25) & (r > 0.78) & tail_on
        c[tail] = red
        c[tail & (r > 1.18)] = rim
        c[(u + 0.06) ** 2 + (v - 0.04) ** 2 < pupil ** 2] = pup
        c[(u + 0.17) ** 2 + (v - 0.17) ** 2 < 0.09 ** 2] = hi
        return c
    return look


def eye_outline(th):
    """A disc with a short tear-like tail toward its lower outer side (u outward, v up)."""
    d = math.atan2(math.sin(th + 0.85), math.cos(th + 0.85))
    return 1.0 + 0.6 * math.exp(-d * d / 0.06)


def hood(k):
    k.add("hood", ell(HOOD_C, HOOD_R), BLACK, "head")
    # the crest: the hood rising behind into a point that leans up, back and to its left
    k.add("crest", tube([HOOD_C + (0.0, 0.03, 0.03), HOOD_C + (-0.01, 0.05, 0.085), HOOD_C + (-0.03, 0.06, 0.135),
                         HOOD_C + (-0.052, 0.05, 0.162)], [0.078, 0.052, 0.028, 0.0095]), BLACK, "head")
    # the hood's rim drawn a little down at the sides, and a small body under it where arms and tail leave it
    k.add("rim", ell(HOOD_C + (0.0, 0.0, -0.045), (0.105, 0.09, 0.06)), BLACK, "head")
    k.add("core", ell((0, 0.015, 0.29), (0.05, 0.05, 0.045)), BLACK, "body")


def arms(k):
    """Thin arms out from under the hood, forward and a little down, to round fists of curled fingers."""
    k.add("arm", tube([(0.045, -0.005, 0.3), (0.095, -0.035, 0.31), (0.14, -0.075, 0.29), FIST + (-0.022, 0.012, 0.006)],
                      [0.017, 0.0135, 0.0135, 0.017]), BLACK, "arm{s}", mirror=True)
    k.add("fist", ell(FIST, (0.04, 0.038, 0.036)), BLACK, "arm{s}", mirror=True)
    # three curled fingers across the top and front of the fist, and a thumb under
    for j, dz in enumerate((0.022, 0.0, -0.022)):
        k.add(f"finger{j}", ell(FIST + (0.012, -0.03, dz), (0.03, 0.02, 0.0135), rot=(0, 0, -20)), BLACKSOFT,
              "arm{s}", mirror=True)
    k.add("thumb", ell(FIST + (-0.018, -0.028, -0.03), (0.018, 0.017, 0.015)), BLACK, "arm{s}", mirror=True)


def tail(k):
    """From under the hood down behind the mask, round under its chin and up its left side in a curl."""
    k.add("tail", tube([(0.0, 0.02, 0.28), (0.004, 0.03, 0.22), (0.0, 0.03, 0.12), (-0.012, 0.012, 0.042),
                        (-0.038, -0.035, 0.03), (-0.05, -0.068, 0.052)], [0.015, 0.014, 0.013, 0.016, 0.02, 0.022]),
          BLACK, "tail")
    k.add("curl", ell((-0.05, -0.072, 0.058), (0.026, 0.024, 0.024)), BLACK, "tail")


def mask(k):
    k.add("mask", ell(MASK_C, MASK_R, rot=(-6, 0, 0)), GOLD, "tail", surface="metal")
    # its rim a little darker, as the art's shading at the edge
    def rim(P):
        q = (P - MASK_C) / MASK_R
        r = np.sqrt(q[:, 0] ** 2 + q[:, 2] ** 2)
        return smoothstep(0.82, 0.98, r) * (np.abs(P[:, 0]) < 0.09) * (P[:, 2] > 0.02) * (P[:, 2] < 0.27) * (P[:, 1] < 0.0)
    k.paint(rim, GOLDDEEP, on=["mask"])
    p, n = on_ell(MASK_C, MASK_R, 0.0, 0.158)
    k.decal(p, (0, -1.0, 0.08), (0.06, 0.074), mask_face(), eps=0.002, rings=64, segs=200, surface="metal")


def mask_face(gold=GOLD, line=GOLDLINE, W=0.065):
    """The face drawn on the mask: two lidded eyes, a long nose from the brows, a straight mouth (bold strokes); W is a
    stroke's half width in the patch's units."""
    gold, line = srgb(gold), srgb(line)
    def face(u, v):
        c = np.tile(gold, (len(u), 1))
        for s in (1, -1):
            # the eye: an arched upper edge meeting a straight lid at its corners (a half-shut eye)
            ex, ev = 0.4 * s, 0.34
            du, dv = (u - ex) / 0.3, (v - ev)
            arch = (np.abs(du) < 1.02) & (np.abs(dv - 0.17 * (1 - du * du)) < W)
            lid = (np.abs(du) < 1.02) & (np.abs(dv) < W)
            c[arch | lid] = line
        # a long nose: two lines down from between the brows, closed by a base
        nose = (((np.abs(np.abs(u) - 0.09) < W) & (v < 0.5) & (v > -0.32))
                | ((np.abs(v + 0.32) < W) & (np.abs(u) < 0.09 + W)))
        mouth = (np.abs(v + 0.66) < W) & (np.abs(u) < 0.34)
        c[nose | mouth] = line
        return c
    return face


def lay_eyes(k, state):
    look = eye_look(state, red_eye(), srgb(BLACK), srgb(RED), line=0.15)
    for s in (1, -1):
        p, n = on_ell(HOOD_C, HOOD_R, EYE_X * s, EYE_Z)
        d = unit(0.55 * s, -1, 0.08)
        col = look if s == 1 else (lambda u, v, f=look: f(-u, v))
        out = eye_outline if s == 1 else (lambda th: eye_outline(math.pi - th))
        k.decal(p, d, (EYE_HALF, EYE_HALF), col, eps=0.003, outline=out, rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    hood(k)
    arms(k)
    tail(k)
    mask(k)
    k.build(voxel=VOXEL, smooth=4, band=0.005)
    k.eye_states(lambda state: lay_eyes(k, state))
    k.export(out)
