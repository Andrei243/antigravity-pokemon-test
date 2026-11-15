# Poochyena sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/poochyena.py -- <the export's Poochyena.glb> <out.glb>
# From the references: a stocky grey hyena pup on four short legs with a big head; a charcoal muzzle and lower face
# that runs up the middle of the brow in a point, a red nose, a white fang at each corner of the mouth; yellow eyes
# with a red iris toward the nose and a black pupil, cut flat by a scowling brow; big pointed ears, grey outside and
# black inside; charcoal down the throat onto the chest with a jagged edge; charcoal socks with zigzag tops; a spiky
# grey crest along the back and a bushy pointed grey tail.
# Written to be grown from (mightyena.py keeps this frame: the eye, the fang, the socks, the spikes and the claws).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, srgb, inside, smoothstep, tri, unit, eye_look, polygon, args)

GREY, DARK, NOSE, FANG = "#A9AEB6", "#3A3E48", "#E0404E", "#F6F4EE"
SCLERA, IRIS, PUPIL, MOUTH = "#F4C230", "#D8303E", "#141016", "#5A1E26"
INK, LIGHT = "#141418", "#9EA2AA"

# The frame (the skeleton's joints: body (0, 0, 0.22), head (0, -0.1, 0.26), ears (+-0.05, -0.12, 0.34),
# legs (+-0.06, -0.08, 0.14), hinds (+-0.06, 0.1, 0.14), tail (0, 0.12, 0.22))
HC = np.array([0.0, -0.15, 0.305])        # the head's middle: big, carried forward of the chest
HR = np.array([0.082, 0.08, 0.074])
LX = 0.064                                 # the legs' x
FY, HY = -0.085, 0.1                       # forelegs' and hind legs' y
BZ = 0.198                                 # the trunk's middle


# --- shared by the line ----------------------------------------------------------------------------------------------
def spike(base, d, length, w, t, side=None):
    """A solid tapering spike (a tuft of fur, a fang, a claw): a flat-faced hull, w wide and t thick at its root."""
    base = np.array(base, float)
    d = unit(*d)
    if side is None:
        side = np.cross(d, (0, 0, 1.0))
        if np.linalg.norm(side) < 1e-3:
            side = np.array([1.0, 0, 0])
    side = np.array(side, float); side = side - d * (side @ d); side /= np.linalg.norm(side)
    face = np.cross(d, side)
    pts = [base + side * w, base - side * w, base + face * t, base - face * t,
           base + d * length * 0.45 + side * w * 0.55, base + d * length * 0.45 - side * w * 0.55,
           base + d * length]
    return hull(pts)


def claws(k, paw, front, spread, size, col, bone):
    """Small white claws on a paw's front edge (Mightyena's are long and show; Poochyena's are dark)."""
    for i, dx in enumerate((-spread, 0.0, spread)):
        a = np.array([paw[0] + dx, front, 0.012])
        k.add(f"claw{bone[:4]}{i}", spike(a + (0, 0.012, 0), (dx * 2, -1, -0.35), size, size * 0.42, size * 0.3), col,
              bone, fuse=False, mirror=True, surface="shell")


def socks(k, legs, top, depth, teeth, col, on):
    """Dark socks on the legs: below a zigzag line round each leg (legs: (x, y) of each leg's axis on +X)."""
    def mask(P):
        m = np.zeros(len(P))
        for x, y, h in legs:
            q = P.copy(); q[:, 0] = np.abs(q[:, 0])
            near = np.hypot(q[:, 0] - x, q[:, 1] - y) < 0.075
            ang = np.arctan2(q[:, 0] - x, q[:, 1] - y) / (2 * math.pi)
            line = h + depth * tri(ang * teeth)
            m = np.maximum(m, inside(P[:, 2] - line, 0.003) * near)
        return m
    k.paint(mask, col, on=on)


def ear_lining(k, base, tip, width, col):
    """The black inside of an ear: a narrower point painted on its front face, crisp (no on)."""
    base, tip = np.array(base, float), np.array(tip, float)
    d = tip - base
    L = np.linalg.norm(d); d /= L
    def mask(P, sx):
        b = base * np.array([sx, 1, 1]); dd = d * np.array([sx, 1, 1])
        q = P - b
        t = q @ dd
        across = q - np.outer(t, dd)
        rad = np.hypot(across[:, 0], across[:, 2])
        w = width * (1 - np.clip(t / (L * 0.94), 0, 1))
        front = across[:, 1] < -0.003
        return inside(rad - w, 0.003) * (t > L * 0.08) * front * (np.abs(across[:, 1]) < 0.03) * (np.sign(P[:, 0]) == sx)
    k.paint(lambda P: np.clip(mask(P, 1) + mask(P, -1), 0, 1), col)


def clipped(top, n=96):
    """The unit circle cut off above v = top(u): an eye's outline under a brow."""
    pts = []
    for th in np.linspace(0, 2 * math.pi, n, endpoint=False):
        du, dv = math.cos(th), math.sin(th)
        t = 1.0
        if dv > top(du):
            lo, hi = 0.0, 1.0
            for _ in range(40):
                t = (lo + hi) / 2
                if t * dv > top(t * du):
                    hi = t
                else:
                    lo = t
            t = lo
        pts.append((t * math.cos(th), t * math.sin(th)))
    return pts


def eye_open(sclera, iris, pupil, top, iris_c=(-0.3, -0.08), iris_r=(0.5, 0.6), pupil_r=0.42):
    """Yellow eye, a red iris toward the nose (u below nought), a black pupil, the brow's dark edge on top."""
    S, I, P, K = srgb(sclera), srgb(iris), srgb(pupil), srgb(INK)
    def look(u, v):
        c = np.tile(S, (len(u), 1))
        r = np.sqrt(((u - iris_c[0]) / iris_r[0]) ** 2 + ((v - iris_c[1]) / iris_r[1]) ** 2)
        c[r < 1] = I
        c[r < pupil_r] = P
        c[((u - iris_c[0] - 0.14) / 0.14) ** 2 + ((v - iris_c[1] - 0.22) / 0.14) ** 2 < 1] = (1, 1, 1)
        c[v > top(u) - 0.16] = K
        return c
    return look


def lay_eyes(k, state, E, n, half, look, outline, lid, line_col):
    """Both eyes laid by hand, a side each, u outward on both."""
    f = eye_look(state, look, srgb(lid), srgb(line_col), line=0.14)
    for side in (1, -1):
        nn = np.array(n) * np.array([side, 1, 1.0])
        out_ = polygon([(side * a, b) for a, b in outline]) if state in ("Open", "Fierce") else None
        col = f if side == 1 else (lambda a, b, g=f: g(-a, b))
        k.decal(np.array(E) * np.array([side, 1, 1.0]), nn, half, col, eps=0.004, outline=out_, rings=20, segs=56)


# --- Poochyena --------------------------------------------------------------------------------------------------------
BROW = (0.22, 0.42)                 # the eye's top: v = BROW[0] + BROW[1] u (higher outward: a scowl)


def eye_top(u):
    return BROW[0] + BROW[1] * u


def poochyena(src, out):
    k = Kit(src, coat="fur")

    # A stout trunk low between the legs, the haunches fused into it
    k.add("chest", ell((0, -0.065, BZ + 0.014), (0.088, 0.085, 0.1)), GREY, "body")
    k.add("middle", ell((0, 0.02, BZ), (0.088, 0.15, 0.1)), GREY, "body")
    k.add("rump", ell((0, 0.09, BZ + 0.006), (0.088, 0.075, 0.098)), GREY, "body")
    k.add("neck", ell((0, -0.11, 0.27), (0.066, 0.06, 0.07)), GREY, "head")

    # Short thick legs: a shoulder and a thigh fused over each, a stub of shin, a wide paw
    k.add("shoulder", ell((LX - 0.006, FY + 0.008, 0.14), (0.046, 0.052, 0.065)), GREY, "leg{s}", mirror=True)
    k.add("leg", tube([(LX + 0.002, FY, 0.14), (LX + 0.006, FY - 0.004, 0.08), (LX + 0.008, FY - 0.006, 0.03)],
                      [0.038, 0.034, 0.033]), GREY, "leg{s}", mirror=True)
    k.add("paw", ell((LX + 0.008, FY - 0.02, 0.022), (0.038, 0.048, 0.023)), GREY, "leg{s}", mirror=True)
    k.add("thigh", ell((LX - 0.006, HY + 0.005, 0.15), (0.05, 0.066, 0.075)), GREY, "hind{s}", mirror=True)
    k.add("hind", tube([(LX + 0.002, HY + 0.008, 0.13), (LX + 0.006, HY + 0.012, 0.08), (LX + 0.008, HY + 0.004, 0.03)],
                       [0.038, 0.034, 0.033]), GREY, "hind{s}", mirror=True)
    k.add("hpaw", ell((LX + 0.008, HY - 0.012, 0.022), (0.038, 0.048, 0.023)), GREY, "hind{s}", mirror=True)

    # The big head, a muzzle standing forward and a little down, a jaw under it
    k.add("head", ell(HC, HR), GREY, "head")
    k.add("snout", ell(HC + (0, -0.085, -0.024), (0.046, 0.068, 0.04)), DARK, "head")
    k.add("jaw", ell(HC + (0, -0.07, -0.052), (0.044, 0.056, 0.03)), DARK, "head")

    # Big pointed ears up and a little out; grey outside, black inside
    EB, ET = np.array([0.048, -0.135, 0.36]), np.array([0.074, -0.122, 0.475])
    k.add("ear", blade([EB, (EB + ET) / 2, ET], [0.04, 0.026, 0.004], (0, 1, 0), 0.4), GREY, "ear{s}", mirror=True)
    ear_lining(k, EB + (0, 0, 0.01), ET, 0.024, INK)

    # The charcoal of the face: the muzzle and the lower head under a line just below the eyes, and a point up the
    # middle of the brow; the grey of the crown and round the eyes above
    def face(P):
        d = (P - HC) / HR
        low = inside(d[:, 2] - (-0.05 + 0.45 * (-d[:, 1] - 0.2)), 0.04) * (d[:, 1] < 0.35)
        wedge = inside(np.abs(d[:, 0]) - (0.36 - 0.36 * d[:, 2]), 0.04) * (d[:, 1] < 0.1) * (d[:, 2] < 1.0)
        return np.maximum(low, wedge) * (P[:, 2] > 0.2) * (P[:, 1] < -0.095)
    k.paint(face, DARK, on=["head", "snout", "jaw", "neck"])
    # The throat and the chest charcoal down between the forelegs, its edge jagged
    def chest(P):
        edge = -0.075 - 0.018 * tri(P[:, 2] * 18) - 0.012 * tri(P[:, 0] * 14)
        return inside(P[:, 1] - edge, 0.004) * (P[:, 2] > 0.1) * inside(np.abs(P[:, 0]) - 0.07, 0.01)
    k.paint(chest, DARK, on=["chest", "neck", "middle"])

    # Charcoal socks with zigzag tops
    socks(k, [(LX + 0.006, FY - 0.004, 0.075), (LX + 0.006, HY + 0.01, 0.075)], 0.075, 0.016, 5, DARK,
          on=["leg", "paw", "hind", "hpaw", "shoulder", "thigh"])

    # The spiky grey crest along the back, laid back, and the bushy pointed tail out behind to the model's right
    for i, (y, z, ln, lean) in enumerate(((-0.055, 0.31, 0.06, 0.5), (-0.01, 0.31, 0.075, 0.6), (0.035, 0.305, 0.07, 0.7),
                                          (0.075, 0.3, 0.06, 0.8))):
        for s in (-1, 0, 1):
            if s and i in (0, 3):
                continue
            k.add(f"crest{i}{s + 1}", spike((0.026 * s, y, z - 0.015), (0.35 * s, lean, 1.0 - 0.3 * abs(s)), ln - 0.012 * abs(s),
                                            0.02, 0.016, side=(1, 0, 0)), GREY, "body", fuse=False, surface="fur")
    TAIL = [(0, 0.15, 0.25), (0.035, 0.2, 0.245), (0.075, 0.25, 0.23), (0.1, 0.285, 0.215)]
    k.add("tail", tube(TAIL, [0.035, 0.052, 0.046, 0.02], seg=20), GREY, "tail")
    tip = np.array(TAIL[-1])
    for i, (d, ln) in enumerate((((0.45, 0.85, -0.2), 0.07), ((0.65, 0.7, 0.15), 0.055), ((0.2, 0.8, -0.55), 0.05),
                                  ((0.3, 0.85, 0.35), 0.05))):
        k.add(f"tailtip{i}", spike(tip - unit(*d) * 0.02, d, ln + 0.02, 0.022, 0.016), GREY, "tail", fuse=False,
              surface="fur")

    # The red nose on the muzzle's tip; a dark open mouth under it and a white fang at each corner
    k.add("nose", ell(HC + (0, -0.15, -0.014), (0.022, 0.017, 0.017)), NOSE, "head", fuse=False, surface="skin")
    CD, CM = srgb(DARK), srgb(MOUTH)
    def mouth(u, v):
        c = np.tile(CD, (len(u), 1))
        c[(np.abs(u) < 0.9) & (v < 0.25 - 0.2 * u * u) & (v > -0.55 + 0.35 * u * u)] = CM
        return c
    k.decal(HC + (0, -0.13, -0.06), unit(0, -1, -0.25), (0.036, 0.018), mouth, eps=0.003, rings=12, segs=48)
    for s in (1, -1):
        a = HC + np.array((0.024 * s, -0.125, -0.05))
        k.add(f"fang{'R' if s > 0 else 'L'}", spike(a, (0.15 * s, -0.25, -1), 0.026, 0.008, 0.006, side=(1, 0, 0)), FANG,
              "head", fuse=False, surface="shell")

    k.build(voxel=0.0058, smooth=3, band=0.006)

    # The eyes: yellow with a red iris, cut flat by a scowling brow, on the grey just over the charcoal
    de = unit(0.56, -0.8, 0.2)
    E = HC + de / np.linalg.norm(de / HR) + (0, 0, 0.006)
    out_ = clipped(eye_top)
    look = eye_open(SCLERA, IRIS, PUPIL, eye_top)
    def eyes(state):
        lay_eyes(k, state, E, de, (0.029, 0.025), look, out_, DARK, LIGHT)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    poochyena(*args())
