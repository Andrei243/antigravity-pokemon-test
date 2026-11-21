# Chien-Pao sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/chienpao.py -- <the export's Chien-Pao.glb> <out.glb>
#
# From the references: a snow leopard of ruin, white, long and lithe, its chest carried high and its head up; small
# rounded ears, narrow fierce blue eyes under a hard brow, a short muzzle with a dark nose; white fur hanging in
# points round its jaw like a beard; a long crystal of ice standing out sideways-back from each cheek; from the
# corners of its mouth two great broken sword blades hang straight down, dark teal bars with a green zigzag inlay, a
# dark ring at the top, a black crossguard across the left one, each ending in a jagged point of pale ice; pale blue
# diamond spots edged darker over its back, flanks and tail; a few shards of ice along its shoulders; blue stripes
# on its toes; a long thick tail curling up at its end.
# The pictures overrule the task's text: the blades hang straight down from the jaws (not level, not forward).
# The skeleton: body (0, 0, 0.42), head (0, -0.22, 0.54), tail (0, 0.26, 0.43), legR/L (+-0.095, -0.17, 0.34),
# hindR/L (+-0.095, 0.19, 0.34).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, outward, srgb, inside, unit, polygon, eye_look, args

WHITE, SHADE, SPOT, SPOTDK = "#EEF2F8", "#D8E2EE", "#A6D2EE", "#5E9ED2"
ICE, ICE2 = "#B6E2F6", "#86C6EA"
BLADE, ZIG, HILT, NOSE = "#24474C", "#4FB4A2", "#1C1E24", "#3A4250"
EYE_W, IRIS, PUPIL, INK = "#FFFFFF", "#2F86D6", "#12223A", "#1C2836"

H = np.array([0.0, -0.255, 0.605])
HR = np.array([0.112, 0.1, 0.094])


def body(k):
    k.add("chest", ell((0, -0.12, 0.41), (0.12, 0.13, 0.17)), WHITE, "body")
    k.add("middle", ell((0, 0.03, 0.38), (0.108, 0.17, 0.13)), WHITE, "body")
    k.add("rump", ell((0, 0.17, 0.37), (0.115, 0.1, 0.125)), WHITE, "body")
    k.add("neck", tube([(0, -0.14, 0.47), (0, -0.19, 0.53), (0, -0.22, 0.58)], [0.095, 0.088, 0.08]), WHITE, "head")


def legs(k):
    k.add("shoulder", ell((0.088, -0.15, 0.32), (0.074, 0.09, 0.14)), WHITE, "leg{s}", mirror=True)
    k.add("fore", tube([(0.095, -0.17, 0.29), (0.1, -0.19, 0.15), (0.1, -0.2, 0.05)], [0.064, 0.054, 0.05]),
          WHITE, "leg{s}", mirror=True)
    k.add("paw", ell((0.1, -0.225, 0.026), (0.05, 0.066, 0.03)), WHITE, "leg{s}", mirror=True)
    k.add("thigh", ell((0.09, 0.18, 0.3), (0.08, 0.105, 0.14)), WHITE, "hind{s}", mirror=True)
    k.add("hind", tube([(0.095, 0.22, 0.22), (0.1, 0.24, 0.11), (0.1, 0.215, 0.05)], [0.062, 0.052, 0.048]),
          WHITE, "hind{s}", mirror=True)
    k.add("hpaw", ell((0.1, 0.19, 0.026), (0.05, 0.066, 0.03)), WHITE, "hind{s}", mirror=True)
    # blue stripes along the toes, and one up the back of each foot
    for py in (-0.225, 0.19):
        for o in (-0.019, 0.0, 0.019):
            k.paint(lambda P, py=py, o=o: inside(np.abs(np.abs(P[:, 0]) - 0.1 - o) - 0.0045, 0.002)
                    * inside(P[:, 1] - (py - 0.03), 0.004) * inside(P[:, 2] - 0.055, 0.004), SPOTDK)


def head(k):
    k.add("head", ell(H, HR), WHITE, "head")
    k.add("muzzle", ell(H + (0, -0.09, -0.03), (0.062, 0.055, 0.05)), WHITE, "head")
    k.add("brow", ell(H + (0, -0.055, 0.03), (0.085, 0.045, 0.04)), WHITE, "head")
    for s in (1, -1):
        k.add("ear" + ("R" if s > 0 else "L"), ell(H + (0.068 * s, 0.02, 0.07), (0.034, 0.024, 0.036),
                                                   rot=(0, -25 * s, 0)), WHITE, "head")
        # the cheeks' fur, swept back and out
        k.add("cheekfur" + ("R" if s > 0 else "L"), tube([H + (0.07 * s, -0.04, -0.03), H + (0.105 * s, 0.0, -0.04),
                                                          H + (0.125 * s, 0.04, -0.06)], [0.04, 0.026, 0.01]),
              WHITE, "head")
    # the white beard: points hanging round the jaw
    for i, (x, y, L) in enumerate([(0.0, -0.33, 0.07), (0.035, -0.325, 0.085), (0.065, -0.305, 0.08),
                                   (0.085, -0.27, 0.065), (-0.035, -0.325, 0.085), (-0.065, -0.305, 0.08),
                                   (-0.085, -0.27, 0.065)]):
        top = np.array([x, y + 0.01, 0.57])
        k.add(f"beard{i}", tube([top, top + (x * 0.15, -0.006, -L * 0.5), top + (x * 0.25, -0.01, -L)],
                                [0.026, 0.018, 0.006], seg=12, per=4), WHITE, "head")
    # the dark nose on the muzzle's tip
    tip = H + np.array([0, -0.128, -0.005])
    k.decal(tip, unit(0, -0.8, 0.6), (0.016, 0.011), NOSE, eps=0.0025,
            outline=polygon([(-1, 0.8), (1, 0.8), (0.3, -1), (-0.3, -1)]), rings=6, segs=28)
    # the mouth's line under the muzzle
    k.decal(H + np.array([0, -0.123, -0.05]), unit(0, -1, -0.2), (0.022, 0.004), INK, eps=0.0025, rings=4, segs=28)


def spots(k):
    """Pale blue diamonds edged darker over the back and flanks."""
    cb, cd = srgb(SPOT), srgb(SPOTDK)
    def diamond(u, v):
        c = np.tile(cb, (len(u), 1))
        c[np.abs(u) + np.abs(v) > 0.7] = cd
        return c
    places = [((0, 0.03, 0.38), (0.108, 0.17, 0.13), (0.55, -0.5, 0.65)), ((0, 0.03, 0.38), (0.108, 0.17, 0.13), (0.8, 0.1, 0.55)),
              ((0, 0.03, 0.38), (0.108, 0.17, 0.13), (0.4, 0.45, 0.8)), ((0, 0.17, 0.37), (0.115, 0.1, 0.125), (0.85, 0.3, 0.4)),
              ((0, 0.17, 0.37), (0.115, 0.1, 0.125), (0.35, 0.6, 0.75)), ((0, -0.12, 0.41), (0.12, 0.13, 0.17), (0.75, 0.25, 0.6)),
              ((0, 0.03, 0.38), (0.108, 0.17, 0.13), (0.95, -0.25, 0.1)), ((0, 0.03, 0.38), (0.108, 0.17, 0.13), (0.2, 0.0, 1.0))]
    for c, r, d in places:
        c, r, d = np.array(c), np.array(r), unit(*d)
        p = c + d * r
        n = unit(*(d / r))
        k.decal(p, n, (0.021, 0.034), diamond, up=unit(0, 1, 0.3), eps=0.003,
                outline=polygon([(0, 1), (1, 0), (0, -1), (-1, 0)]), rings=8, segs=40, mirror=d[0] > 0.05)


def tail(k):
    pts = [(0, 0.24, 0.43), (0, 0.32, 0.41), (0.01, 0.41, 0.39), (0.03, 0.49, 0.41), (0.055, 0.54, 0.47),
           (0.065, 0.52, 0.545), (0.055, 0.46, 0.565), (0.045, 0.43, 0.525)]
    radii = [0.058, 0.054, 0.05, 0.048, 0.046, 0.044, 0.042, 0.038]
    k.add("tail", tube(pts, radii, seg=20), WHITE, "tail")
    cb, cd = srgb(SPOT), srgb(SPOTDK)
    def diamond(u, v):
        c = np.tile(cb, (len(u), 1))
        c[np.abs(u) + np.abs(v) > 0.7] = cd
        return c
    P = np.array(pts)
    for i, side in ((2, 1), (3, -1), (4, 1), (2, -1), (5, 1)):
        d = unit(*(P[i + 1] - P[i - 1]))
        n = np.cross(d, (0, 0, 1.0)) * side + np.array([0, 0, 0.6])
        n = unit(*n)
        k.decal(P[i] + n * radii[i], n, (0.012, 0.02), diamond, up=d, eps=0.003,
                outline=polygon([(0, 1), (1, 0), (0, -1), (-1, 0)]), rings=8, segs=40)


def ice(k):
    # a long crystal out of each cheek, sideways and back
    for s in (1, -1):
        root = H + np.array([0.08 * s, -0.01, -0.01])
        d = unit(0.85 * s, 0.4, 0.2)
        a = unit(*np.cross(d, (0, 0, 1.0))); b = np.cross(d, a)
        pts = [root + a * 0.016 * math.cos(t) + b * 0.016 * math.sin(t) for t in np.linspace(0, 2 * math.pi, 6, endpoint=False)]
        pts += [root + d * 0.06 + a * 0.012 * math.cos(t + 0.5) + b * 0.012 * math.sin(t + 0.5)
                for t in np.linspace(0, 2 * math.pi, 6, endpoint=False)]
        pts.append(root + d * 0.15)
        k.add("cheekice" + ("R" if s > 0 else "L"), hull(pts), ICE, "head", fuse=False, surface="shell")
    # shards along the shoulders and the back
    for i, (c, d, L) in enumerate([((0.05, -0.12, 0.56), (0.5, 0.4, 0.8), 0.07), ((-0.05, -0.1, 0.56), (-0.5, 0.4, 0.8), 0.065),
                                    ((0.04, 0.02, 0.5), (0.4, 0.5, 0.75), 0.055), ((-0.04, 0.08, 0.49), (-0.4, 0.5, 0.75), 0.05)]):
        c = np.array(c); d = unit(*d)
        a = unit(*np.cross(d, (0, 1.0, 0))); b = np.cross(d, a)
        pts = [c - d * 0.02 + a * 0.016 * math.cos(t) + b * 0.011 * math.sin(t) for t in np.linspace(0, 2 * math.pi, 5, endpoint=False)]
        pts.append(c + d * L)
        k.add(f"shard{i}", hull(pts), ICE if i % 2 else ICE2, "body", fuse=False, surface="shell")


def blades(k):
    """Two broken sword blades hanging straight down from the corners of the mouth."""
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        x, y = 0.058 * s, -0.315
        top, bot = 0.54, 0.22
        hw, hd = 0.019, 0.012
        k.add("blade" + sd, hull([(x + a * hw, y + b * hd, z) for a in (-1, 1) for b in (-1, 1) for z in (top, bot)]),
              BLADE, "head", fuse=False, surface="metal")
        # the dark ring at its top, at the mouth's corner
        k.add("ring" + sd, hull([(x + 0.025 * math.cos(t), y + 0.018 * math.sin(t), z) for t in np.linspace(0, 2 * math.pi, 10, endpoint=False)
                                 for z in (top - 0.005, top - 0.03)]), HILT, "head", fuse=False, surface="metal")
        # the green zigzag inlaid down its front and its back
        zz = np.linspace(top - 0.045, bot + 0.03, 7)
        xs = [0.007 * (1 if i % 2 else -1) for i in range(len(zz))]
        outline = [(xs[i] + 0.0055, -(zz[i] - zz[0])) for i in range(len(zz))] + \
                  [(xs[i] - 0.0055, -(zz[i] - zz[0])) for i in range(len(zz) - 1, -1, -1)]
        for face, yy in (("f", y - hd), ("b", y + hd)):
            k.add("zig" + face + sd, slab(outline, (x, yy, zz[0]), (1, 0, 0), (0, 0, -1), 0.006), ZIG, "head",
                  fuse=False, surface="metal")
        # the jagged point of ice
        pts = [(x + a * hw * 1.15, y + b * hd * 1.2, bot + 0.012) for a in (-1, 1) for b in (-1, 1)]
        pts += [(x + a * hw * 1.5, y + b * hd * 1.4, bot - 0.025) for a in (-1, 1) for b in (-1, 1)]
        pts += [(x + 0.004 * s, y, bot - 0.1)]
        k.add("point" + sd, hull(pts), ICE, "head", fuse=False, surface="shell")
        side = [(x + 1.3 * hw * s, y, bot - 0.005), (x + 2.3 * hw * s, y - 0.004, bot - 0.035),
                (x + 1.2 * hw * s, y + 0.008, bot - 0.06), (x + 1.2 * hw * s, y - 0.008, bot - 0.05)]
        k.add("chip" + sd, hull(side), ICE2, "head", fuse=False, surface="shell")
    # the black crossguard across the left blade
    x, y, z = -0.058, -0.315, 0.44
    k.add("guard", hull([(x + a, y + b, z + c) for a in (-0.045, 0.045) for b in (-0.01, 0.01) for c in (-0.009, 0.009)]),
          HILT, "head", fuse=False, surface="metal")


# --- the eyes: narrow blue eyes under a hard brow sloping down to the nose ------------------------------------------
CW, CI, CP, CK, CL = srgb(EYE_W), srgb(IRIS), srgb(PUPIL), srgb(INK), srgb(WHITE)
OUT = [(-1, -0.15), (-0.55, 0.25), (0.3, 0.55), (1.0, 0.6), (0.85, -0.1), (0.35, -0.6), (-0.3, -0.62), (-0.8, -0.45)]
_RIM = polygon(OUT)


def eye_open(u, v):
    c = np.tile(CW, (len(u), 1))
    c[np.hypot((u + 0.05) / 0.5, (v + 0.05) / 0.62) < 1] = CI
    c[np.hypot((u + 0.05) / 0.22, (v + 0.05) / 0.38) < 1] = CP
    c[np.hypot(u + 0.2, v - 0.15) < 0.12] = CW
    rr = np.hypot(u, v) / np.array([_RIM(t) for t in np.arctan2(v, u)])
    c[(rr > 0.7) & (v > -0.05 + 0.3 * u)] = CK
    c[rr > 0.86] = CK
    return c


def eyes(k, state):
    look = eye_look(state, eye_open, CL, CK, line=0.16)
    for s in (1, -1):
        n = unit(0.5 * s, -1, 0.3)
        c = H + np.array([0.042 * s, -0.08, 0.035])
        f = look if s > 0 else (lambda u, v, look=look: look(-u, v))
        k.decal(c, n, (0.028, 0.02), f, eps=0.003, outline=polygon([(x * s, y) for x, y in OUT]), rings=20, segs=56)


def chienpao(src, out):
    k = Kit(src, coat="fur")
    body(k)
    legs(k)
    head(k)
    tail(k)
    spots(k)
    ice(k)
    blades(k)
    k.build(voxel=0.0065, smooth=4, band=0.006)
    k.eye_states(lambda st: eyes(k, st))
    k.export(out)


if __name__ == "__main__":
    chienpao(*args())
