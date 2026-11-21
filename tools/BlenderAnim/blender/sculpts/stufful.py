# Stufful sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/stufful.py -- <the export's Stufful.glb> <out.glb>
# From the references: a plush bear cub that is one big pink ball of a head and body, like a hood; the face a paler
# pink disc framed by a broad white ring; a white oval muzzle with a small dark grey nose, a line down from it and a
# wide thin smile; small round black eyes with a white glint, close together above the muzzle; big white ears like
# fans standing up and out from the ball's upper sides, pink at their roots; four short thick dark brown limbs, round
# at their ends, the front ones reaching forward, a pink pad on each front sole; a short pink tail at the back with a
# white tag at its end.
#
# Written to be grown from (bewear.py imports it with sibling("stufful")): the line's pink, white and brown, the eye,
# the white muzzle with its nose and mouth, the fan of an ear and the paw's pink pad are the functions below; the
# calls that build Stufful are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, ring_band, srgb, inside, unit, polygon, eye_look, args, sibling

td = sibling("teddiursa")             # hit, bear_eye

# --- the line's colours -------------------------------------------------------------------------------------------
PINK, FACE, WHITE = "#EF9DB0", "#F6BDC9", "#F8F5F3"
BROWN, PAD, NOSE, MOUTH = "#56352B", "#E9718E", "#3D3A3E", "#4A3A3C"
INK = srgb("#151316")


def on_ell(c, r, d):
    d = unit(*d)
    t = 1.0 / math.sqrt(((d / np.array(r)) ** 2).sum())
    return np.array(c, float) + d * t


def muzzle_face(k, MC, MR, bone, nose_half, mouth_half, mouth_line=0.16, nose_up=0.32):
    """A white oval muzzle fused on the face, a dark nose high on its front, a line down from it to a wide smile."""
    k.add("muzzle", ell(MC, MR), WHITE, bone, surface="skin")
    front = on_ell(MC, MR, (0, -1, 0.25))
    k.decal(front + np.array([0, 0, MR[2] * 0.08]), unit(0, -1, 0.35), nose_half, NOSE, eps=0.0025, rings=10,
            segs=48)
    CW, CM = srgb(WHITE), srgb(MOUTH)
    def mouth(u, v):
        c = np.tile(CW, (len(u), 1))
        c[(np.abs(u) < mouth_line * 0.6) & (v > -0.05)] = CM                     # the line down from the nose
        c[np.abs(v + 0.15 - 0.75 * u * u) < mouth_line] = CM                   # the smile
        return c
    low = on_ell(MC, MR, (0, -1, -0.35))
    k.decal(low, unit(0, -1, -0.2), mouth_half, mouth, eps=0.002, rings=12, segs=56,
            outline=polygon([(-1, 0.2), (-0.95, 0.75), (0, 1), (0.95, 0.75), (1, 0.2), (0.5, -1), (-0.5, -1)]))


def fan_ear(k, root, out, up_tilt, length, width, thick, bone, color=WHITE, root_color=PINK, root_r=None):
    """A fan of an ear: a thick white plate spreading from its root up and out, a pink knob at its root (mirrored)."""
    root, out = np.array(root, float), unit(*out)
    face = unit(*up_tilt)
    side = np.cross(face, out); side /= np.linalg.norm(side)
    L, W = length, width
    # a broad fan, its lower edge rounded, its upper edge running straight out to a point at the top
    outline = [(0.0, -W * 0.22)] + [(L * (0.5 + 0.48 * math.cos(t)), W * 0.55 * math.sin(t))
                                    for t in np.linspace(-math.pi * 0.85, -0.05, 10)] + \
              [(L * 1.04, W * 0.5), (L * 0.45, W * 0.42), (0.0, W * 0.22)]
    k.add("ear", slab(outline, root, out, side, thick), color, bone, fuse=False, mirror=True, surface="fur")
    k.add("earroot", ell(root + out * L * 0.08, root_r or (W * 0.42, thick * 1.6, W * 0.42)), root_color, bone,
          mirror=True)


def pad(k, at, n, half, bone, color=PAD):
    """A round pink pad on a sole (mirrored)."""
    k.decal(at, n, half, color, eps=0.003, rings=10, segs=48, mirror=True, up=unit(0, 0.3, 1))


def stufful(src, out):
    k = Kit(src, coat="fur")
    C, CR = np.array([0, -0.012, 0.16]), np.array([0.09, 0.086, 0.086])

    # --- the ball: head and body as one, the hind half on the body's bone -----------------------------------------
    k.add("ball", ell(C, CR), PINK, "head")
    k.add("rear", ell((0, 0.035, 0.14), (0.078, 0.07, 0.07)), PINK, "body")

    # --- the face: a paler disc framed by a broad white ring, cut round an axis looking forward and a little down --
    A = unit(0, -1, -0.12)
    def ang(P):
        Q = P - C
        return np.degrees(np.arccos(np.clip((Q @ A) / np.linalg.norm(Q, axis=1), -1, 1)))
    front = lambda P: (P[:, 1] < C[1]) * inside(np.linalg.norm((P - C) / CR, axis=1) - 1.08, 0.01)
    # the ball pink to a crisp line where the limbs come out under it
    k.paint(lambda P: inside(0.098 - P[:, 2], 0.0025) * inside(np.linalg.norm((P - C) / CR, axis=1) - 1.03, 0.004),
            PINK)
    k.paint(lambda P: inside(ang(P) - 37, 0.9) * front(P), FACE)
    k.paint(lambda P: inside(ang(P) - 55, 0.9) * inside(37 - ang(P), 0.9) * front(P), WHITE)

    # --- the white muzzle, its nose and smile -----------------------------------------------------------------
    MC, MR = np.array([0, -0.094, 0.138]), np.array([0.036, 0.022, 0.026])
    muzzle_face(k, MC, MR, "head", (0.011, 0.0075), (0.022, 0.01), mouth_line=0.13)

    # --- big white fans of ears up and out from the ball's upper sides -------------------------------------------
    fan_ear(k, (0.058, 0.0, 0.212), (0.75, 0.05, 0.66), (0.45, -1, 0.1), 0.085, 0.06, 0.018, "ear{s}",
            root_r=(0.018, 0.016, 0.018))

    # --- four short thick brown limbs, the front ones reaching forward, a pink pad on each front sole -----------
    k.add("fore", tube([(0.05, -0.035, 0.105), (0.056, -0.07, 0.05), (0.058, -0.09, 0.034)], [0.04, 0.038, 0.035]),
          BROWN, "leg{s}", mirror=True)
    k.add("hind", tube([(0.05, 0.05, 0.1), (0.054, 0.058, 0.05), (0.054, 0.06, 0.032)], [0.04, 0.038, 0.034]),
          BROWN, "hind{s}", mirror=True)
    pn = unit(0.1, -0.75, -0.65)
    pad(k, on_ell((0.058, -0.09, 0.034), (0.035, 0.035, 0.035), pn), pn, (0.016, 0.016), "leg{s}")

    # --- a short pink tail at the back, a white tag round its end ----------------------------------------------
    TA, TB = np.array((0, 0.1, 0.13)), np.array((0, 0.128, 0.128))
    k.add("tail", tube([TA, TB], [0.024, 0.022]), PINK, "body")
    k.add("tag", ring_band((TA + TB) / 2 + (0, 0.008, 0), (0, 1, 0), 0.0205, 0.012, thick=0.005), WHITE, "body",
          fuse=False, surface="cloth")

    # --- the eyes: small, round and black with a glint, close above the muzzle -------------------------------------
    de = unit(0.32, -0.92, 0.18)
    E = on_ell(C, CR, unit(0.3, -0.93, 0.06))
    lay = td.eyes(k, E, de, (0.012, 0.0145), srgb(FACE), INK, td.bear_eye(INK, at=(0.25, 0.3), size=0.3))

    k.build(voxel=0.0028, smooth=4, band=0.004, wband=0.03)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    stufful(*args())
