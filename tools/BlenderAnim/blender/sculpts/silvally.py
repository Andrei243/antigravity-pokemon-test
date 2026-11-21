# Silvally sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/silvally.py -- <the export's Silvally.glb> <out.glb>
#
# Type: Null freed of its helmet, grown from typenull.py's frame (its trunk, its bird's forelegs and beast's hind legs,
# the ruff's square-ended strips and the fish's tail, through sibling("typenull")). From the references: the same
# charcoal trunk and hind legs with purple scales, the same green taloned forelegs; the ruff now a great white mane of
# square-cut strips over the neck and the whole chest, the design's largest mass. The head white: a domed crown and a
# long pale muzzle held forward and down, its mouth open on red with white fangs, a grey lower jaw; a red mask round
# each eye, pointed up and back, the eye black with a red iris; a grey disc with a slot on each side of the head
# behind the eye. A crest of broad white feathers swept up and back from the crown. The tail's fin white with its spine
# and lower edge blue, grey spikes on the spine.
# Bones: body (0, 0, 0.38); head (0, -0.12, 0.5); tail (0, 0.21, 0.39); legs (+-0.055, -0.12, 0.36); hind legs
# (+-0.06, 0.16, 0.34).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, slab, lathe, outward, srgb, inside, unit, axes, eye_look, polygon, lumpy,
                       args, sibling)

t = sibling("typenull")
r = sibling("raikou")

WHITE = "#ECEEF2"
WHITE2 = "#D6D9E0"
HEAD = "#E8EAEE"
MUZZLE = "#D2D6DD"
JAW = "#9EA3AC"
MOUTH = "#C8303C"
FANG = "#FAFAFA"
MASK = "#D8262E"
DISC = "#9A9FA9"
SLOTC = "#3A3E46"
FEATHER = "#F2F4F7"
SPIKE = "#A9ADB6"
MEMBRANE = "#E6EAF0"
EYE_BLACK, IRIS = "#16121A", "#E0303C"

F = dict(x=0.058, fy=-0.12, hy=0.16, s=1.18, body_z=0.38)

G = 1.33                                     # the head a third larger after the game's shot
HD = np.array([0.0, -0.235, 0.675])          # the crown's middle
HR = np.array([0.078, 0.092, 0.082]) * G
EYE_DIR = unit(0.62, -0.74, 0.25)
EYE_R = 0.03 * G
ML = 1.12                                     # the muzzle longer still


def add_neck(k):
    k.add("neck", tube([(0, -0.12, 0.46), (0, -0.17, 0.55), (0, -0.21, 0.6)], [0.075, 0.07, 0.064]), WHITE, "head")
    # The mane's hood behind the head, falling into the ruff, its tufts hanging from it
    k.add("hood", ell(HD + (0, 0.075, -0.06), (0.1, 0.09, 0.1), deform=lumpy(5, 3, 0.05)), WHITE, "head")
    for j, (x, y, ln) in enumerate(((0.085, -0.16, 0.08), (0.08, -0.11, 0.1), (0.045, -0.07, 0.11), (0.0, -0.06, 0.1))):
        for sd in ((1,) if x == 0 else (1, -1)):
            out = unit(x * sd, -0.2 if y < -0.15 else 0.6, 0)
            k.add(f"hoodstrip{j}{'R' if sd > 0 else 'L'}",
                  t.strip((x * sd, y, 0.63), out, ln * t.jitter(3 * j + (sd < 0)), 0.03, 0.026, 0.3),
                  WHITE if j % 2 else WHITE2, "head", fuse=False, surface="fur")


def add_head(k):
    k.add("crown", ell(HD, HR), HEAD, "head")
    k.add("muzzle", ell(HD + np.array((0, -0.105 * ML, -0.035)) * G, np.array((0.05, 0.085 * ML, 0.043)) * G,
                        rot=(-14, 0, 0)), MUZZLE, "head")
    # The open mouth: red set back between the muzzle and the dropped grey jaw
    k.add("mouth", ell(HD + np.array((0, -0.105 * ML, -0.078)) * G, np.array((0.036, 0.06 * ML, 0.022)) * G,
                       rot=(-14, 0, 0)), MOUTH, "head")
    k.add("jaw", ell(HD + np.array((0, -0.085 * ML, -0.108)) * G, np.array((0.04, 0.068 * ML, 0.022)) * G,
                     rot=(-22, 0, 0)), JAW, "head")
    for s in (1, -1):
        a = HD + np.array((0.028 * s, -0.17 * ML, -0.07)) * G
        k.add(f"fang{'R' if s > 0 else 'L'}", hull([a + (0.012, 0.007, 0), a + (-0.012, 0.007, 0), a + (0, -0.01, 0),
                                                     a + (0, 0.014, 0.002), a + (0.001 * s, -0.001, -0.032)]),
              FANG, "head", fuse=False, surface="shell")
    # The grey disc with its slot on each side behind the eye
    c = HD + (HR[0] - 0.008, 0.026, -0.016)
    k.add("disc", lathe(c, (1, 0, 0), [(0, 0), (0.042, 0), (0.042, 0.014), (0.035, 0.024), (0, 0.024)]), DISC, "head",
          fuse=False, mirror=True, surface="metal")
    k.add("slot", slab([(-0.028, -0.006), (0.028, -0.006), (0.028, 0.006), (-0.028, 0.006)], c + (0.024, 0, 0),
                       unit(0, -0.7, 0.7), (0, 0.7, 0.7), 0.01), SLOTC, "head", fuse=False, mirror=True, surface="metal")


def add_crest(k):
    """Broad white feathers swept up and back from the crown, fanned apart: solid hulls, thick at the root."""
    for j, (sx, d, ln, w) in enumerate(((0.0, (0, 0.78, 0.62), 0.28, 0.055), (0.04, (0.28, 0.85, 0.45), 0.25, 0.05),
                                         (-0.04, (-0.28, 0.85, 0.45), 0.25, 0.05), (0.05, (0.36, 0.93, 0.05), 0.2, 0.045),
                                         (-0.05, (-0.36, 0.93, 0.05), 0.2, 0.045))):
        d = unit(*d)
        root = HD + (sx, 0.02, HR[2] * 0.6)
        a = np.cross(d, (0, 0, 1.0)) if abs(d[2]) < 0.95 else np.array((1.0, 0, 0))
        a = a / np.linalg.norm(a)
        n = np.cross(a, d); n /= np.linalg.norm(n)
        # the plate's face turned mostly up-forward (its width across a), a ridge down its middle
        pts = [root + a * w * 0.6 + n * 0.016, root - a * w * 0.6 + n * 0.016, root + a * w * 0.6 - n * 0.016,
               root - a * w * 0.6 - n * 0.016, root + d * ln * 0.55 + a * w, root + d * ln * 0.55 - a * w,
               root + d * ln * 0.55 + n * 0.012, root + d * ln * 0.55 - n * 0.012, root + d * ln]
        k.add(f"feather{j}", hull(pts), FEATHER, "head", fuse=False, surface="fur")


def eye_point():
    return r.on_ell(HD, HR, EYE_DIR)


def add_masks(k):
    """The red mask round each eye, pointed up and back (a patch before the build, in every state)."""
    p, n = eye_point()
    out = polygon([(-1.0, -0.55), (-0.2, -1.0), (0.7, -0.75), (1.25, 0.2), (1.35, 1.25), (0.3, 0.85), (-0.85, 0.35)])
    for side in (1, -1):
        o = polygon([(-a, b) for a, b in [(-1.0, -0.55), (-0.2, -1.0), (0.7, -0.75), (1.25, 0.2), (1.35, 1.25),
                                          (0.3, 0.85), (-0.85, 0.35)]]) if side < 0 else out
        k.decal(p * np.array([side, 1, 1.0]), n * np.array([side, 1, 1.0]), (EYE_R * 1.45, EYE_R * 1.35), MASK,
                eps=0.003, outline=o, rings=14, segs=72)


def eye_open(u, v):
    c = np.tile(srgb(EYE_BLACK), (len(u), 1))
    c[((u + 0.03) / 0.74) ** 2 + ((v + 0.02) / 0.8) ** 2 < 1] = srgb(IRIS)
    c[((u + 0.03) / 0.3) ** 2 + ((v + 0.02) / 0.4) ** 2 < 1] = srgb(EYE_BLACK)
    c[((u + 0.3) / 0.17) ** 2 + ((v - 0.35) / 0.17) ** 2 < 1] = (1, 1, 1)
    return c


def eyes(k, state):
    p, n = eye_point()
    look = eye_look(state, eye_open, srgb(MASK), srgb("#3A0E14"), line=0.16)
    for side in (1, -1):
        col = look if side == 1 else (lambda a, b, f=look: f(-a, b))
        k.decal(p * np.array([side, 1, 1.0]), n * np.array([side, 1, 1.0]), (EYE_R, EYE_R * 0.85), col, eps=0.005,
                rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    t.add_trunk(k, F)
    t.add_forelegs(k, F)
    t.add_hindlegs(k, F)
    add_neck(k)
    t.add_ruff(k, (0, -0.15, 0.5), (0.15, 0.135, 0.12), 0.58, lambda a: 0.25 - 0.13 * (a / 145) ** 1.4,
               color=WHITE, color2=WHITE2, w=0.032, t=0.028, tilt=0.25,
               angles=(0, 20, 40, 60, 80, 100, 122, 145))
    t.add_tail(k, (0, 0.285, 0.47), F, membrane=MEMBRANE, size=1.15, spikes=SPIKE)
    add_head(k)
    add_crest(k)
    add_masks(k)
    k.build(voxel=0.0075, band=0.004)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)
