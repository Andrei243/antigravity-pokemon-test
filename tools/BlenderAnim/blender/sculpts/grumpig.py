# Grumpig sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/grumpig.py -- <the export's Grumpig.glb> <out.glb>
# Spoink grown (spoink.py, imported: its snout and nostrils, its pearl, its spring of a tail turned into a curl).
# From the references: a stout pig standing upright, lavender in the face and down the front of its round belly,
# dark grey over its back, its shoulders and great sleeve-like arms and its big round haunches; lavender hands with
# three short fingers held forward and lavender feet with two toes. A round head with no neck, a dark tuft over its
# crown with two black pearls on its brow; a pink pig's snout; sly half-lidded eyes, white with a big dark pupil under
# a heavy dark lid; a toothy grin under the snout; great pointed ears, dark with pink linings, out at the head's upper
# sides; a third black pearl on the belly to the left; a long pink tail curling in loops out behind to the right.
# (The task's "frilled ears": the pictures show smooth pointed ears with pink linings.)
# The export's joints: body (0, 0, 0.32), arms (+-0.08, 0, 0.4), head (0, -0.01, 0.44), ears (+-0.06, 0, 0.54),
# legs (+-0.07, 0, 0.2), tail (0, 0.07, 0.24).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, outward, slab, polygon, smoothstep

from sculptkit import sibling
sp = sibling("spoink")

LAV, DARK, PINK, LINING = "#D8C4E6", "#4E4B56", "#F2A2B4", "#EE9AAE"
BLACK, WHITE, INK, PUPIL = "#18161C", "#FFFFFF", "#1E1A22", "#2A2232"
NOSTRIL = "#7A3A4C"

# --- the frame -------------------------------------------------------------------------------------------------------
BELLY = np.array([0, -0.012, 0.3])
BELLY_R = np.array([0.15, 0.13, 0.17])
H = np.array([0, -0.035, 0.585])           # the head's middle
HR = np.array([0.105, 0.098, 0.1])
SH = np.array([0.115, 0.0, 0.445])         # shoulder, elbow, wrist, hand
EL = np.array([0.205, -0.035, 0.405])
WR = np.array([0.225, -0.115, 0.37])
HAND = np.array([0.228, -0.158, 0.36])


def add_body(k):
    def pear(V):
        V = V.copy()
        s = 1 + 0.08 * np.clip(-V[:, 2], 0, 1) - 0.12 * np.clip(V[:, 2], 0, 1) ** 1.5
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("belly", ell(BELLY, BELLY_R, deform=pear), LAV, "body")
    k.add("back", ell((0, 0.03, 0.36), (0.13, 0.1, 0.13)), DARK, "body")
    k.add("shoulders", ell((0, 0.01, 0.44), (0.135, 0.095, 0.06)), DARK, "body")


def add_legs(k):
    """Big round dark haunches fused into the hips, a stub of shin, lavender feet with two toes."""
    k.add("thigh", ell((0.095, 0.005, 0.15), (0.088, 0.1, 0.1)), DARK, "leg{s}", mirror=True)
    k.add("shin", tube([(0.1, -0.01, 0.1), (0.105, -0.025, 0.05)], [0.05, 0.044]), DARK, "leg{s}", mirror=True)
    k.add("foot", ell((0.105, -0.05, 0.026), (0.05, 0.07, 0.027)), LAV, "leg{s}", mirror=True)
    for i, dx in enumerate((-0.022, 0.022)):
        k.add(f"toe{i}", ell((0.105 + dx, -0.108, 0.022), (0.022, 0.026, 0.021)), LAV, "leg{s}", mirror=True)
    # the dark shin above a crisp line over the foot
    k.paint(lambda P: inside(0.045 - P[:, 2], 0.0025) * (np.abs(np.abs(P[:, 0]) - 0.105) < 0.075)
            * (P[:, 1] > -0.16), LAV, on=["foot", "toe", "shin"])


def add_arms(k):
    """Great dark sleeves out from the shoulders, carried forward and down, lavender hands with three fingers."""
    k.add("arm", tube([SH - (0.04, 0, 0), SH, EL, WR], [0.062, 0.062, 0.058, 0.05]), DARK, "arm{s}", mirror=True)
    k.add("hand", ell(HAND, (0.04, 0.036, 0.036)), LAV, "arm{s}", mirror=True)
    d = unit(*(WR - EL))
    for i, dx in enumerate((-0.022, 0.0, 0.022)):
        a = HAND + np.array([dx, -0.022, -0.012])
        k.add(f"finger{i}", tube([a, a + d * 0.03 + (0, 0, -0.012)], [0.014, 0.012]), LAV, "arm{s}", mirror=True)
    # the sleeve's end a crisp line round the wrist
    def lav_hand(P):
        Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
        t = (Q - (WR + d * 0.012)) @ d
        return inside(-t, 0.003) * (np.linalg.norm(Q - HAND, axis=1) < 0.09)
    k.paint(lav_hand, LAV, on=["hand", "finger", "arm"])


def add_head(k):
    k.add("head", ell(H, HR), LAV, "head")
    k.add("jowl", ell(H + (0, -0.02, -0.045), (0.09, 0.08, 0.06)), LAV, "head")
    k.add("neck", ell((0, -0.02, 0.495), (0.09, 0.078, 0.045)), LAV, "head")
    # the dark tuft over the crown, rising to a point over the brow
    k.add("tuft", tube([(0, 0.04, 0.665), (0, -0.01, 0.69), (0, -0.045, 0.715), (0, -0.06, 0.735)],
                       [0.065, 0.055, 0.035, 0.014]), DARK, "head")
    def cap(P):
        rel = P - H
        return inside(0.045 - rel[:, 2] - 0.25 * rel[:, 1] - 0.4 * np.abs(rel[:, 0]), 0.003) * (rel[:, 2] > -0.02)
    k.paint(cap, DARK, on=["head", "tuft", "jowl"])
    k.paint(lambda P: inside(0.06 - (P[:, 2] - H[2] - 0.25 * (P[:, 1] - H[1])), 0.003) * (np.abs(P[:, 0]) < 0.2)
            * (P[:, 2] > 0.6), DARK, on=["tuft"])
    # two black pearls on the brow, at the tuft's foot
    for s in (1, -1):
        sp.pearl(k, (0.032 * s, -0.098, 0.672), 0.021, BLACK, "head", glow=False, name="pearl" + ("R" if s > 0 else "L"))
    # the pink snout
    sp.snout(k, H + (0, -0.09, -0.012), (0.046, 0.03, 0.032), PINK, "head", nostril=NOSTRIL, tilt=8)


def add_ears(k):
    """Great pointed ears out from the head's upper sides, dark, their fronts lined pink: loose plates."""
    d = unit(0.86, 0.12, 0.5)
    base = np.array([0.075, 0.02, 0.625])
    outline = [(0, -0.045), (0.06, -0.052), (0.125, -0.028), (0.165, 0.006), (0.105, 0.042), (0.035, 0.05), (0, 0.04)]
    k.add("ear", slab(outline, base, d, (0, 0, 1), 0.024), DARK, "ear{s}", fuse=False, mirror=True)
    n = np.cross(d, (0, 0, 1.0)); n /= np.linalg.norm(n)
    lin = [(0.05 + (a - 0.06) * 0.72, b * 0.62) for a, b in outline]
    k.add("lining", slab(lin, base + n * 0.0135, d, (0, 0, 1), 0.006), LINING, "ear{s}", fuse=False, mirror=True)
    k.add("earroot", ell(base + d * 0.01, (0.035, 0.03, 0.04)), DARK, "ear{s}", mirror=True)


def add_tail(k):
    """A long pink tail out behind to the right, curling into two loops that face front and back."""
    root = np.array([0.03, 0.125, 0.25])
    c = np.array([0.24, 0.17, 0.22])                 # the loops' middle
    ex, ez = unit(0.82, 0.57, 0), np.array([0, 0, 1.0])
    pts = [root, root + (0.05, 0.03, -0.01), c + ex * -0.04 + ez * -0.06]
    th = np.linspace(0, 2 * math.pi * 1.6, 60)
    rr = 0.07 - 0.018 * th / th[-1]
    ang = -math.pi / 2 - th                          # from the bottom, round to the right and up
    loop = c[None] + (np.cos(ang) * rr)[:, None] * ex[None] + (np.sin(ang) * rr)[:, None] * ez[None] \
        + (th / th[-1] * 0.03)[:, None] * ex[None]
    pts = np.vstack([pts, loop[1:]])
    r = np.concatenate([[0.02, 0.018, 0.017], np.linspace(0.017, 0.014, len(loop) - 1)])
    k.add("tail", outward(tube(pts, r, seg=14, per=3)), PINK, "tail", fuse=False)


def add_face(k):
    # the toothy grin under the snout: a white crescent of teeth in a dark line, rising at its corners
    CW, CI, CL = srgb(WHITE), srgb(INK), srgb(LAV)
    def grin(u, v):
        c = np.tile(CL, (len(u), 1))
        top = 0.25 + 0.55 * u * u
        low = -0.55 + 0.75 * u * u
        m = (v < top) & (v > low) & (np.abs(u) < 0.95)
        c[m] = CW
        edge = m & ((v > top - 0.2) | (v < low + 0.2) | (np.abs(u) > 0.82))
        c[edge] = CI
        c[m & (np.abs(np.abs(u) - 0.35) < 0.06)] = CI
        return c
    k.decal(H + (0, -0.08, -0.068), unit(0, -1, -0.25), (0.05, 0.019), grin, eps=0.004, rings=14, segs=56)
    # the third pearl on the belly, to the left
    p = BELLY + np.array([-0.05, -BELLY_R[1] * 0.9, 0.055])
    sp.pearl(k, p, 0.022, BLACK, "body", glow=False, name="bellypearl")


def lay_eyes(k):
    CW, CP, CI, CL = srgb(WHITE), srgb(PUPIL), srgb(INK), srgb(LAV)
    def lid(u):
        return 0.32 - 0.12 * u        # the heavy lid, a little lower at the outer corner: sly
    def eye(u, v):
        c = np.tile(CW, (len(u), 1))
        c[u * u + v * v > 0.86 ** 2] = CI
        c[((u + 0.12) / 0.42) ** 2 + ((v + 0.05) / 0.62) ** 2 < 1] = CP
        c[v > lid(u) - 0.16] = CI
        c[v > lid(u) + 0.12] = CL
        return c
    de = unit(0.5, -0.84, 0.18)
    E = H + np.array([0.048, -0.087, 0.03])
    def eyes(state):
        ink = CI
        if state == "Fierce":
            look = lambda u, v: (lambda c: c)(eye(u, v - 0.35 * u))
        else:
            look = eye_look(state, eye, CL, ink, line=0.18)
        k.decal(E, de, (0.03, 0.03), look, eps=0.005, rings=20, segs=56, mirror=True)
    return eyes


def grumpig(src, out):
    k = Kit(src, coat="fur")
    add_body(k)
    add_legs(k)
    add_arms(k)
    add_head(k)
    add_ears(k)
    add_tail(k)
    # dark over the back and the shoulders, lavender down the front: a crisp edge down the sides
    def torso(P, lo, hi):
        return (P[:, 2] > lo) * (P[:, 2] < hi) * (np.abs(P[:, 0]) < 0.165)
    k.paint(lambda P: inside(0.012 - P[:, 1], 0.003) * torso(P, 0.2 - 0.12 * smoothstep(0.02, 0.08, P[:, 1]), 0.5), DARK, on=["belly", "back", "shoulders"])
    k.paint(lambda P: inside(P[:, 1] - 0.012, 0.003) * torso(P, 0.12, 0.465), LAV, on=["belly", "back", "shoulders"])
    add_face(k)
    k.build(voxel=0.0058, band=0.005)
    k.eye_states(lay_eyes(k))
    k.export(out)


if __name__ == "__main__":
    grumpig(*args())
