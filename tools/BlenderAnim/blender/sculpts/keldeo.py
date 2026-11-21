# Keldeo sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/keldeo.py -- <the export's Keldeo.glb> <out.glb>
#
# From the references (Ordinary Form): a cream colt, the face and the muzzle cream too, a short pale horn pointing
# forward and up from the brow. Over the head a navy band of thick feathered plumes like a brim, flaring out at the
# sides and back over the nape; on it a great red-orange mane, a puffed cloud swept back with flame-like points at
# its top, and a red lock curling down behind the head. A light blue tuft hangs from the throat over the chest, and
# a great light blue feathered tail rises and curls back from the rump. Slim legs on navy hooves (cuffs, dark
# soles). Big round dark eyes under the brim, two small pink nostrils.
# The frame is in named constants and functions (the Resolute Form grows from it): build under __main__.
# Bones: body (0, 0.01, 0.26); head (0, -0.1, 0.32); legs (+-0.05, -0.11, 0.22); hind legs (+-0.05, 0.13, 0.22);
# tail (0, 0.14, 0.28). The export stands 0.52 tall.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, lathe, outward, srgb, inside, unit, eye_look, polygon, lumpy, args)

CREAM = "#F6ECBE"
RED = "#EE5A2A"
RED_DEEP = "#D8441E"
NAVY = "#2E4C92"
BLUE = "#7CC2EE"
HOOF = "#2C4E8E"
SOLE = "#141C30"
HORN = "#F2E2A2"
NOSE = "#E0707A"
EYE_W, EYE_I, EYE_P = "#F6F4EE", "#2A2A36", "#121218"
INK = "#2A2430"

LEG_X = 0.05
FORE_Y, HIND_Y = -0.11, 0.13
LEG_TOP = 0.22
BODY_Z = 0.255
H = np.array([0.0, -0.15, 0.345])      # the head's centre
HR = np.array([0.055, 0.061, 0.06])
EYE_DIR = unit(0.6, -0.72, 0.22)
EYE_R = 0.021


def frame(d, orient):
    """A right-handed frame (side, face, d): side square to d in the plane of orient."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    s = np.array(orient, float); s = s - d * (s @ d); s /= np.linalg.norm(s)
    return s, np.cross(d, s), d


def plume(base, d, length, width, thick, orient=(1, 0, 0), bend=(0, 0, 0), curl=0.0, sub=3):
    """A thick feathered plume tapering to a point from base along d, its tip bent toward bend by curl."""
    s, f, dd = frame(d, orient)
    def shape(V):
        V = V.copy()
        t = np.clip((V[:, 2] + 1) / 2, 0, 1)
        w = np.sqrt(t + 0.02) * (1 - t ** 1.4) * 1.55 + 0.04
        V[:, 0] *= w; V[:, 1] *= np.maximum(w, 0.35)
        return V
    V, F = ell((0, 0, 0), (width, thick, length / 2), deform=shape, sub=sub)
    t = np.clip((V[:, 2] / (length / 2) + 1) / 2, 0, 1)
    W = (np.array(base, float) + np.outer(V[:, 0], s) + np.outer(V[:, 1], f)
         + np.outer(V[:, 2] + length / 2, dd) + np.outer(curl * t ** 2, np.array(bend, float)))
    return outward((W, F))


def on_ell(c, r, d):
    d = unit(*d)
    s = 1 / math.sqrt(((d / np.array(r)) ** 2).sum())
    p = np.array(c) + d * s
    n = (p - np.array(c)) / np.array(r) ** 2
    return p, n / np.linalg.norm(n)


# --- the trunk and the legs ----------------------------------------------------------------------------------------------
def add_trunk(k):
    k.add("chest", ell((0, -0.07, BODY_Z + 0.008), (0.058, 0.07, 0.066)), CREAM, "body")
    k.add("middle", ell((0, 0.02, BODY_Z + 0.004), (0.062, 0.115, 0.062)), CREAM, "body")
    k.add("rump", ell((0, 0.105, BODY_Z + 0.006), (0.06, 0.07, 0.064)), CREAM, "body")
    k.add("neck", tube([(0, -0.07, 0.285), (0, -0.105, 0.315), (0, -0.13, 0.34)], [0.046, 0.04, 0.038]), CREAM, "head")


def add_legs(k):
    """Slim legs that are no stilts: shoulders and thighs fused over the upper legs, cannons tapering to the hooves."""
    k.add("shoulder", ell((LEG_X - 0.004, FORE_Y + 0.004, 0.215), (0.044, 0.052, 0.06)), CREAM, "leg{s}", mirror=True)
    k.add("leg", tube([(LEG_X, FORE_Y, 0.21), (LEG_X, FORE_Y + 0.008, 0.13), (LEG_X, FORE_Y - 0.002, 0.07),
                       (LEG_X, FORE_Y - 0.004, 0.036)], [0.028, 0.02, 0.017, 0.018]), CREAM, "leg{s}", mirror=True)
    k.add("thigh", ell((LEG_X - 0.003, HIND_Y + 0.01, 0.225), (0.05, 0.068, 0.074)), CREAM, "hind{s}", mirror=True)
    k.add("hind", tube([(LEG_X, HIND_Y + 0.01, 0.2), (LEG_X, HIND_Y - 0.01, 0.15), (LEG_X, HIND_Y + 0.018, 0.09),
                        (LEG_X, HIND_Y + 0.006, 0.036)], [0.03, 0.022, 0.018, 0.018]), CREAM, "hind{s}", mirror=True)
    for name, y, bone in (("hoof", FORE_Y - 0.004, "leg{s}"), ("hhoof", HIND_Y + 0.006, "hind{s}")):
        k.add(name, lathe((LEG_X, y, 0.0), (0, 0, 1), [(0, 0.003), (0.019, 0.003), (0.025, 0.006), (0.024, 0.026),
                                                      (0.021, 0.042), (0, 0.042)], seg=32),
              HOOF, bone, fuse=False, mirror=True, surface="shell")
        k.add(name + "sole", lathe((LEG_X, y, 0.0), (0, 0, 1), [(0, 0.0), (0.018, 0.0), (0.018, 0.004), (0, 0.004)],
                                   seg=32), SOLE, bone, fuse=False, mirror=True, surface="shell")


# --- the head -------------------------------------------------------------------------------------------------------------
def add_head(k):
    k.add("head", ell(H, HR), CREAM, "head")
    k.add("muzzle", ell(H + (0, -0.056, -0.028), (0.037, 0.052, 0.035), rot=(18, 0, 0)), CREAM, "head")
    k.add("cheek", ell(H + (0, -0.01, -0.022), (0.05, 0.05, 0.044)), CREAM, "head")


def add_horn(k, color=HORN):
    k.add("horn", tube([(0, -0.186, 0.37), (0, -0.212, 0.402), (0, -0.236, 0.44)], [0.013, 0.009, 0.0028]),
          color, "head", fuse=False, surface="shell")


def add_brim(k):
    """The navy band over the head: a fused cap and thick plumes radiating out and back from its rim."""
    k.add("cap", ell(H + (0, 0.012, 0.046), (0.054, 0.06, 0.03)), NAVY, "head")
    for a, L, w, droop in ((-60, 0.07, 0.03, 0.3), (-25, 0.09, 0.032, 0.3), (15, 0.105, 0.032, 0.2),
                           (50, 0.11, 0.03, 0.1), (80, 0.11, 0.028, 0.05)):
        r = math.radians(a)
        for sx in (1, -1):
            out = np.array([sx * math.cos(r), math.sin(r), 0.0])
            base = H + (0, 0.012, 0.05) + out * np.array([0.036, 0.04, 0])
            d = out + np.array([0, 0.6, -droop])
            k.add(f"brim{a}{'R' if sx > 0 else 'L'}", plume(base, d, L, w, 0.013, orient=(0, 0, 1),
                                                           bend=(0, 0.3, 1), curl=0.02),
                  NAVY, "head", fuse=False, surface="fur")
    # The front of the brim, over the brow (the horn comes out under it)
    for sx in (1, -1):
        k.add(f"brow{'R' if sx > 0 else 'L'}", plume(H + (sx * 0.014, -0.035, 0.05), (sx * 0.8, -0.8, -0.3), 0.055,
                                                     0.026, 0.013, orient=(0, 0, 1), bend=(0, 0.5, 0.6), curl=0.015),
              NAVY, "head", fuse=False, surface="fur")


def add_mane(k):
    """The red mane: a puffed cloud on the brim swept back, flame-like points at its top and back, and a curl."""
    k.add("mane", ell(H + (0, 0.045, 0.105), (0.074, 0.085, 0.056), deform=lumpy(4, 3, 0.03)), RED, "head")
    k.add("manefront", ell(H + (0, -0.005, 0.085), (0.056, 0.045, 0.042), deform=lumpy(4, 3, 0.03)), RED, "head")
    k.add("maneback", ell(H + (0, 0.115, 0.095), (0.055, 0.06, 0.048), deform=lumpy(4, 3, 0.03)), RED, "head")
    for base, d, L, w in (((0, 0.0, 0.145), (0, 0.75, 1), 0.08, 0.034),
                          ((0.03, 0.06, 0.14), (0.35, 1, 0.55), 0.075, 0.03), ((-0.03, 0.06, 0.14), (-0.35, 1, 0.55), 0.075, 0.03),
                          ((0, 0.11, 0.13), (0, 1, 0.35), 0.085, 0.032), ((0, 0.15, 0.09), (0, 1, 0.05), 0.065, 0.03)):
        k.add(f"tuft{len(k.parts)}", plume(H + base, d, L, w, 0.018, orient=(1, 0, 0), bend=(0, 0, 1), curl=0.012),
              RED_DEEP if d[2] > 0.8 else RED, "head", fuse=False, surface="fur")
    # A crisp line between the navy band and the red over it (the fused colours would blend to purple)
    def near(P):
        return ((np.abs(P[:, 0]) < 0.1) & (P[:, 1] > H[1] - 0.09) & (P[:, 1] < H[1] + 0.07)
                & (P[:, 2] > H[2] + 0.03) & (P[:, 2] < H[2] + 0.12)).astype(float)
    line = H[2] + 0.066
    k.paint(lambda P: near(P) * inside(P[:, 2] - line, 0.0025) * (P[:, 2] > H[2] + 0.035), NAVY, on=["cap", "mane"])
    k.paint(lambda P: near(P) * inside(line - P[:, 2], 0.0025), RED, on=["cap", "mane"])
    # The lock curling down behind the head
    k.add("lock", tube([H + (0.02, 0.07, 0.07), H + (0.04, 0.1, 0.03), H + (0.04, 0.095, -0.015),
                        H + (0.032, 0.07, -0.03)], [0.028, 0.026, 0.022, 0.017]), RED, "head")


def add_bib(k):
    """The light blue tuft hanging from the throat over the chest."""
    k.add("bibroot", ell((0, -0.125, 0.29), (0.035, 0.03, 0.03)), BLUE, "head")
    for dx, L, w in ((0.0, 0.085, 0.026), (0.022, 0.07, 0.022), (-0.022, 0.07, 0.022)):
        k.add(f"bib{dx}", plume((dx, -0.13, 0.3), (dx * 8, -0.35, -1), L, w, 0.016, orient=(1, 0, 0),
                                bend=(0, 0.3, 0.6), curl=0.014), BLUE, "head", fuse=False, surface="fur")


def add_tail(k):
    """The light blue tail: a fused root and broad plumes rising back and out to +X, their tips curling up."""
    k.add("tailroot", tube([(0, 0.15, 0.285), (0.012, 0.19, 0.31), (0.025, 0.22, 0.33)], [0.03, 0.03, 0.026]),
          BLUE, "tail")
    o = (0.012, 0.19, 0.31)
    for d, L, w, t, bend in (((0.5, 0.8, 0.35), 0.21, 0.06, 0.036, (0, 0.2, 1)),
                             ((0.35, 0.7, 0.75), 0.17, 0.05, 0.03, (0, 1, 0.2)),
                             ((0.55, 0.85, 0.0), 0.17, 0.046, 0.028, (0, 0.2, 1)),
                             ((0.15, 0.85, 0.45), 0.16, 0.044, 0.028, (0, 0.3, 1))):
        k.add(f"tail{len(k.parts)}", plume(o, d, L, w, t, orient=(0, 0, 1), bend=bend, curl=0.05, sub=4),
              BLUE, "tail", fuse=False, surface="fur")


# --- the face -------------------------------------------------------------------------------------------------------------
def add_face(k):
    tip = H + (0, -0.107, -0.037)
    for sx in (1, -1):
        k.decal(tip + (sx * 0.012, 0.003, 0.004), unit(sx * 0.3, -1, 0.15), (0.0055, 0.0045), NOSE, eps=0.002,
                rings=4, segs=20)
    k.decal(tip + (0, 0.005, -0.019), unit(0, -0.8, -0.6), (0.008, 0.005), INK, eps=0.002, rings=4, segs=20)


def eye_open(u, v):
    c = np.tile(srgb(EYE_W), (len(u), 1))
    r = np.sqrt(((u - 0.02) / 0.8) ** 2 + ((v + 0.02) / 0.86) ** 2)
    c[r < 1] = srgb(EYE_I)
    c[np.sqrt(((u - 0.02) / 0.45) ** 2 + ((v + 0.06) / 0.52) ** 2) < 1] = srgb(EYE_P)
    c[((u + 0.25) / 0.26) ** 2 + ((v - 0.32) / 0.26) ** 2 < 1] = (1, 1, 1)
    return c


def eyes(k, state):
    p, n = on_ell(H, HR, EYE_DIR)
    f = eye_look(state, eye_open, srgb(CREAM), srgb(INK), line=0.15)
    for side in (1, -1):
        col = f if side == 1 else (lambda a, b, g=f: g(-a, b))
        k.decal(p * np.array([side, 1, 1.0]), n * np.array([side, 1, 1.0]), (EYE_R, EYE_R * 1.1), col, eps=0.003,
                rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    add_trunk(k)
    add_legs(k)
    add_head(k)
    add_horn(k)
    add_brim(k)
    add_mane(k)
    add_bib(k)
    add_tail(k)
    add_face(k)
    k.build(voxel=0.005, band=0.005)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)
