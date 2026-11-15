# Banette sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/banette.py -- <the export's Banette.glb> <out.glb>
#
# The design, from the references: a dark grey cloth doll; a big wide head, a short horn in the middle of its top and
# a pointed ear at each upper corner; a long darker streamer of cloth from the back of the head on its right (+X),
# out and down in a zigzag to a point; red-pink eyes with a black slit, the grey lid slanting down over them toward
# the nose; a wide gold zipper of a grin across the lower head, its teeth dark lines, the pull hanging from its left
# (-X) end; a small pear of a body, short pointed stubs of legs; flat cloth arms held out and down, each ending in
# three ragged points; a little gold star on a short stalk behind, to its left (-X) and low.
# The skeleton: body (0, 0, 0.28), arms (+-0.07, 0, 0.29), tail (0, 0.06, 0.18), head (0, -0.01, 0.33),
# legs (+-0.05, 0, 0.14).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, slab, hull, srgb, inside, unit, eye_look, polygon, boxy, args

GREY, DARK, GOLD, ZIPLINE = "#5E5E68", "#4A4A58", "#F0C850", "#5A4418"
PINK, SLIT, RIM, INK = "#EE86A2", "#1A0C12", "#7A1E36", "#1A1418"

# --- the frame ------------------------------------------------------------------------------------------------------
H, HR = np.array([0.0, -0.005, 0.39]), np.array([0.138, 0.107, 0.102])
EYE_ARC, EYE_Z, EYE_HALF = 0.064, 0.39, (0.04, 0.03)
MOUTH_Z, MOUTH_HALF = 0.333, (0.112, 0.04)
VOXEL = 0.0045
EYE_OUT = [(-1.0, -0.05), (-0.5, 0.35), (0.1, 0.72), (0.6, 0.92), (0.92, 0.6), (1.0, 0.1), (0.8, -0.45),
           (0.35, -0.75), (-0.2, -0.72), (-0.7, -0.45)]


def on_ell(c, r, d):
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r)) ** 2).sum())


def head_dir(arc, z):
    th = arc / HR[0]
    return unit(math.sin(th), -math.cos(th), (z - H[2]) / HR[2] * 1.05)


def head_shape(V):
    """A little squared, wide at the top, narrowing to the chin."""
    V = boxy(2.5)(V)
    V[:, 0] *= 1 - 0.18 * np.clip(-V[:, 2], 0, 1)
    return V


def rho_of(outline):
    r = polygon(outline)
    return lambda u, v: np.array([math.hypot(a, b) / r(math.atan2(b, a)) for a, b in zip(u, v)])


def eye_open(u, v):
    rho = rho_of(EYE_OUT)(u, v)
    c = np.tile(srgb(PINK), (len(u), 1))
    c[((u + 0.08) / 0.17) ** 2 + ((v + 0.02) / 0.58) ** 2 < 1] = srgb(SLIT)
    c[rho > 0.825] = srgb(RIM)
    # the heavy lid line along its top edge, slanting down to the nose
    top = (v > 0.0) & (v > 0.44 + 0.4 * u)
    c[top & (rho > 0.6)] = srgb(INK)
    return c


MOUTH_OUT = ([(u, 0.36 + 0.56 * u * u) for u in np.linspace(1, -1, 13)]
             + [(u, -0.52 + 1.02 * u * u) for u in np.linspace(-1, 1, 13)[1:-1]])


def mouth(k):
    """The zipper grin, gold, its teeth five dark lines across it, a dark edge round it; its pull at the -X end."""
    rho = rho_of(MOUTH_OUT)
    def zip_(u, v):
        c = np.tile(srgb(GOLD), (len(u), 1))
        for x in (-0.66, -0.33, 0.0, 0.33, 0.66):
            c[np.abs(u - x) < 0.06] = srgb(ZIPLINE)
        c[rho(u, v) > 0.86] = srgb(INK)
        return c
    d = head_dir(0.0, MOUTH_Z)
    k.decal(on_ell(H, HR, d) + d * 0.004, d, MOUTH_HALF, zip_, eps=0.003, outline=polygon(MOUTH_OUT), rings=16,
            segs=120)


def body(k):
    def pear(V):
        V = V.copy()
        s = 1 - 0.32 * np.clip(V[:, 2], 0, 1) + 0.04 * np.clip(-V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell((0, 0.005, 0.175), (0.088, 0.078, 0.13), deform=pear), GREY, "body")
    k.add("neck", ell((0, 0.0, 0.29), (0.055, 0.05, 0.04)), GREY, "body")
    k.add("leg", tube([(0.05, 0.0, 0.1), (0.055, -0.005, 0.045), (0.06, -0.015, 0.024)], [0.04, 0.034, 0.022]),
          GREY, "leg{s}", mirror=True)
    # the arms: flat cloth held out and down, a little forward, three ragged points at the end
    k.add("arm", blade([(0.07, 0.0, 0.285), (0.12, -0.02, 0.25), (0.17, -0.035, 0.205)], [0.036, 0.038, 0.042],
                       unit(0.15, 1, 0.35), 0.5), GREY, "arm{s}", mirror=True)
    for j, (dx, dz) in enumerate([(0.055, 0.0), (0.04, -0.035), (0.01, -0.055)]):
        a = np.array([0.168, -0.035, 0.205])
        k.add(f"fringe{j}", blade([a, a + np.array([dx, -0.008, dz - 0.012])], [0.022, 0.012],
                                  unit(0.15, 1, 0.35), 0.6), GREY, "arm{s}", mirror=True)


def head(k):
    k.add("head", ell(H, HR, deform=head_shape), GREY, "head")
    k.add("horn", tube([(0, 0.01, 0.46), (0.0, 0.015, 0.515), (-0.008, 0.02, 0.56)], [0.045, 0.026, 0.013]),
          GREY, "head")
    k.add("ear", tube([(0.095, 0.0, 0.45), (0.135, 0.0, 0.495), (0.16, 0.0, 0.522)], [0.045, 0.025, 0.013]),
          GREY, "head", mirror=True)
    # the streamer: from the back of the head on its right, out and down in a zigzag, run back and out to +X
    k.add("streamer", blade([(0.04, 0.07, 0.45), (0.11, 0.1, 0.44), (0.165, 0.12, 0.39), (0.185, 0.13, 0.31),
                             (0.155, 0.135, 0.25), (0.19, 0.14, 0.18), (0.17, 0.145, 0.12)],
                            [0.068, 0.062, 0.054, 0.046, 0.038, 0.03, 0.016], unit(-0.55, 1, 0), 0.4), DARK, "head")


def star(k):
    k.add("stalk", tube([(0, 0.065, 0.17), (-0.06, 0.075, 0.125), (-0.115, 0.05, 0.1)], [0.021, 0.02, 0.019]),
          GREY, "tail")
    pts = []
    for i in range(12):
        a = math.pi / 2 + i * math.pi / 6
        r = 0.05 if i % 2 == 0 else 0.027
        pts.append((r * math.cos(a), r * math.sin(a)))
    n = unit(-0.55, -0.83, 0.1)
    ex = unit(0.83, -0.55, 0)
    ez = np.cross(n, ex) * -1
    k.add("star", slab(pts, (-0.15, 0.03, 0.095), ex, ez, 0.03), GOLD, "tail", fuse=False)


def pull(k):
    """The zipper's pull hanging from the grin's -X end."""
    d = head_dir(-0.092, MOUTH_Z + 0.02)
    top = on_ell(H, HR, d) - d * 0.006
    side = unit(-d[1], d[0], 0)
    pts = []
    for z in (0.0, -0.065):
        for s in (-0.016, 0.016):
            for t in (0.0, 0.016):
                pts.append(top + np.array([0, 0, z]) + side * s + d * t)
    k.add("pull", hull(pts), GOLD, "head", fuse=False)


def banette(src, out):
    k = Kit(src, coat="fur")
    body(k)
    head(k)
    star(k)
    pull(k)
    mouth(k)
    k.build(voxel=VOXEL, smooth=3, band=0.005)

    out_r = polygon(EYE_OUT)
    out_l = polygon([(-a, b) for a, b in EYE_OUT])
    def fierce(u, v):
        c = eye_open(u, v)
        rho = rho_of(EYE_OUT)(u, v)
        c[(v > 0.2 + 0.5 * u) & (rho > 0.0)] = srgb(GREY)
        c[np.abs(v - (0.2 + 0.5 * u)) < 0.14] = srgb(INK)
        return c
    def eyes(state):
        for side in (1, -1):
            d = head_dir(EYE_ARC * side, EYE_Z)
            look = fierce if state == "Fierce" else eye_look(state, eye_open, srgb(GREY), srgb(INK), line=0.16)
            col = look if side == 1 else (lambda u, v, f=look: f(-u, v))
            k.decal(on_ell(H, HR, d), d, EYE_HALF, col, eps=0.0035, outline=out_r if side == 1 else out_l,
                    rings=20, segs=56)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    banette(*args())
