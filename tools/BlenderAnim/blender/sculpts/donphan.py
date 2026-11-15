# Donphan sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/donphan.py -- <the export's Donphan.glb> <out.glb>
#
# Phanpy grown (phanpy.py's frame, by sibling: the stub legs with their white nails, the ear plate's frame, the dark
# oval eye with its glint). From the references: a stocky pale blue-grey elephant, low and broad; a dark grey band of
# tyre tread, block by block with dark grooves between, running from over the rump along the back, over the brow and
# down in front of the face as the trunk, which curls back under at the ground (the wheel it rolls on); great flat
# ears out of the sides of the head under the band, sweeping out and back to points, dark grey above and dark red
# beneath; two white tusks out of the face beside the trunk, forward, out and curving up; small dark eyes beside the
# trunk's root under a heavy lid; four short thick legs, each with a dark grey cuff over a pale foot with three white
# nails; a short tail under the band's end.
# Bones: body (0, 0.03, 0.3), head (0, -0.2, 0.3), ears (+-0.12, -0.2, 0.34), legs (+-0.12, -0.12, 0.2), hind legs
# (+-0.12, 0.15, 0.2), tail (0, 0.27, 0.3).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, slab, ring_band, srgb, inside, unit, eye_look, args, sibling

p = sibling("phanpy")
GREY, ARMOUR, GROOVE, RED, TUSK = "#B6C6DC", "#626A78", "#353A44", "#9C2438", "#F4F2EC"
NAIL = p.NAIL

H = np.array([0.0, -0.2, 0.33])               # the head's middle
HR = np.array([0.15, 0.135, 0.135])

# the tread's line, in the midline: from over the rump along the back, over the brow, down the face, curling back
TREAD = [(0.3, 0.36), (0.22, 0.48), (0.08, 0.54), (-0.08, 0.535), (-0.22, 0.49), (-0.32, 0.4), (-0.37, 0.27),
         (-0.375, 0.15), (-0.35, 0.075), (-0.29, 0.05)]


def path(pts, n):
    """n points evenly along a smooth line through pts (y, z), with their tangents."""
    P = np.array([(0.0, y, z) for y, z in pts])
    ext = np.vstack([2 * P[0] - P[1], P, 2 * P[-1] - P[-2]])
    fine = []
    for i in range(len(P) - 1):
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        for t in np.linspace(0, 1, 40, endpoint=False):
            fine.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                               + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    fine.append(P[-1])
    fine = np.array(fine)
    s = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(fine, axis=0), axis=1))])
    at = np.linspace(0, s[-1], n)
    Q = np.stack([np.interp(at, s, fine[:, j]) for j in range(3)], axis=1)
    T = np.gradient(Q, axis=0); T /= np.linalg.norm(T, axis=1, keepdims=True)
    return Q, T, s[-1]


def width_at(f):
    """The tread's half width and depth along it (f 0 at the rump, 1 at the trunk's tip): broad on the back, a
    narrower thick trunk down the face."""
    w = np.interp(f, [0, 0.12, 0.45, 0.62, 1.0], [0.11, 0.155, 0.15, 0.085, 0.07])
    t = np.interp(f, [0, 0.45, 0.62, 1.0], [0.05, 0.055, 0.075, 0.065])
    return w, t


def tread(k, n=15):
    """The band: a dark fused core along the line (the grooves, and the trunk joined to the face), and over it a block
    of tread a step, each a flat-faced solid with its outer edges cut back."""
    Q, T, L = path(TREAD, 120)
    f = np.linspace(0, 1, len(Q))
    w, t = width_at(f)
    X = np.array([1.0, 0, 0])
    core_r = t * 0.75
    # the back's blocks lie on the body itself; the trunk has a fused core, from the brow down, joined to the face
    sel = f > 0.44
    k.add("trunkcore", tube(Q[sel][:-2:5], core_r[sel][:-2:5] * 1.05, seg=20, per=4), GROOVE, "head")
    for i in range(n):
        g = (i + 0.5) / n
        j = int(round(g * (len(Q) - 1)))
        c, tt = Q[j], T[j]
        N = np.cross(tt, X); N /= np.linalg.norm(N)
        hl = L / n * 0.44
        wi, ti = width_at(np.array([g]))
        wi, ti = float(wi[0]), float(ti[0])
        droop = float(np.interp(g, [0, 0.45, 0.6, 1], [0.07, 0.075, 0.02, 0.012]))   # the back's plates curve
        ch = min(0.016, hl * 0.35)                                                     # down over the flanks

        def lift(b):
            return -droop * (b / wi) ** 2
        cuts = np.linspace(-wi, wi, 6)
        bone = "head" if g > 0.5 else "body"
        for q in range(5):                       # five convex strips across, so the block can curve
            b0, b1 = cuts[q] - 0.002, cuts[q + 1] + 0.002
            pts = []
            for b in (b0, b1):
                z = lift(b)
                edge_in = ch if abs(b) > wi - 0.01 else 0.0
                for a in (-hl, hl):
                    pts.append(c + tt * a + X * b + N * (z - ti * 0.55))
                    pts.append(c + tt * a + X * b + N * (z + ti - ch - edge_in * 0.5))
                for a in (-hl + ch, hl - ch):
                    pts.append(c + tt * a + X * b + N * (z + ti - edge_in))
            k.add(f"tread{i}_{q}", hull(pts), ARMOUR, bone, fuse=False, surface="shell")


def ears(k):
    """Great flat ears out of the head's sides under the band, out and back to a point, tilted down outward; dark grey
    above, dark red under (a red plate a little under the grey and short of its edge, showing at the front)."""
    out = [(0.0, -0.12), (0.06, -0.135), (0.16, -0.1), (0.26, -0.02), (0.31, 0.06), (0.27, 0.07), (0.17, 0.08),
           (0.07, 0.1), (0.0, 0.1)]
    for s in (1,):
        o = np.array([0.1, -0.16, 0.43])
        ex = unit(1.0, 0.18, -0.36)                  # out, a little back, sloping down
        ez = unit(0.0, 1.0, 0.0)                     # across it: back
        k.add("ear", slab(out, o, ex, ez, 0.026), ARMOUR, "ear{s}", fuse=False, mirror=True, surface="shell")
        n = np.cross(ex, ez - ex * (ez @ ex)); n /= np.linalg.norm(n)
        if n[2] > 0:
            n = -n                                    # down
        red = [(0.02, -0.125), (0.06, -0.14), (0.16, -0.106), (0.25, -0.03), (0.27, 0.02), (0.17, 0.05),
               (0.04, 0.06)]
        k.add("earred", slab(red, o + n * 0.016, ex, ez, 0.018), RED, "ear{s}", fuse=False, mirror=True,
              surface="skin")


def eyes(k):
    """Small dark eyes beside the trunk's root, a heavy lid sloping down toward the nose, a glint."""
    K, Wt, G = srgb(p.INK), srgb(p.WHITE), srgb(GREY)
    def look(u, v):
        c = np.tile(K, (len(u), 1))
        c[((u - 0.12) / 0.26) ** 2 + ((v - 0.05) / 0.24) ** 2 < 1] = Wt
        c[v > 0.32 + 0.32 * u] = G
        return c
    d = unit(0.72, -0.62, 0.28)
    centre = p.on_ell(H, HR, d)
    def lay(state):
        if state == "Fierce":
            f = lambda u, v: (lambda c: (c.__setitem__(v > 0.12 + 0.45 * u, G), c)[1])(look(u, v))
        else:
            f = eye_look(state, look, G, K, line=0.2)
        k.decal(centre, d, (0.026, 0.026), f, eps=0.005, rings=20, segs=56, mirror=True)
    return lay


def donphan(src, out):
    k = Kit(src, coat="shell")
    def barrel(V):
        V = V.copy()
        V[:, :2] *= (1 + 0.1 * np.clip(-V[:, 2], 0, 1))[:, None]
        return V
    k.add("body", ell((0, 0.04, 0.31), (0.19, 0.25, 0.2), deform=barrel), GREY, "body")
    k.add("rump", ell((0, 0.17, 0.25), (0.19, 0.14, 0.15)), GREY, "body")
    k.add("chest", ell((0, -0.13, 0.27), (0.17, 0.14, 0.15)), GREY, "body")
    k.add("head", ell(H, HR), GREY, "head")
    tread(k)
    ears(k)
    # the tusks: white, out of the face beside the trunk, forward, out and curving up
    k.add("tusk", tube([(0.075, -0.3, 0.3), (0.12, -0.39, 0.3), (0.165, -0.47, 0.335), (0.19, -0.52, 0.39)],
                       [0.034, 0.029, 0.02, 0.008]), TUSK, "head", fuse=False, mirror=True, surface="shell")
    # stub legs, a dark cuff over each pale foot, three white nails
    p.legs(k, 0.12, -0.12, 0.15, 0.17, (0.1, 0.1, 0.1), 0.07, (0.08, 0.088, 0.032), GREY, NAIL, nails=3,
           nail_size=0.026)
    for y in (-0.12, 0.15):
        k.add("cuff", ring_band((0.124, y - 0.004, 0.08), (0, 0, 1), 0.07, 0.046, thick=0.016), ARMOUR,
              "leg{s}" if y < 0 else "hind{s}", fuse=False, mirror=True, surface="shell")
    # a short tail under the band's end
    k.add("tail", tube([(0, 0.27, 0.31), (0, 0.31, 0.26), (0, 0.32, 0.22)], [0.02, 0.017, 0.016]), GREY, "tail")
    k.add("tailtip", blade([(0, 0.318, 0.225), (0, 0.325, 0.19), (0, 0.33, 0.165)], [0.026, 0.02, 0.004], (1, 0, 0),
                           0.6), ARMOUR, "tail")
    k.build(voxel=0.0065, smooth=5, band=0.006)
    k.eye_states(eyes(k))
    k.export(out)


if __name__ == "__main__":
    donphan(*args())
