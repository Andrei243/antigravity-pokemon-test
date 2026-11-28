# Tambalon (No. 10086, Carpathia, Sound/Normal), sculpted from nothing on the skeleton and clips of the game's export:
#   blender --background --factory-startup --python sculpts/tambalon.py -- <the export's Tambalon.glb> <out.glb>
# Grown from tambalet.py (sibling("tambalet")): the same tortoise, legs, toes, neck, head, swirls, teal and gold marks,
# the same box of strings, bridges and pegs, the same two feelers with felt hammers, all half as large again. From the
# picture (10086-Tambalon.jpg) and the brief (CP-dex-2-mountains.md, 86): the box is a concert cimbalom of dark carved
# wood with more strings, its sides inlaid with a band of folk ornament (gold and teal diamonds on dark wood, the
# picture's gold scrolls simplified to diamonds at the game's size) over a gold moulding; under the box a carved shell
# of wooden scutes hanging as a skirt of pointed flaps, each with a gold inlay; a carved wooden crest standing on the
# head (a tall lobe between two scrolls, a lighter inset and a gold diamond); the eyes half shut as if lost in music;
# the claws larger.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, hull, slab, srgb, unit, args, sibling)

tb = sibling("tambalet")

TAMBALON = dict(tb.TAMBALET)
TAMBALON.update(k=1.5, side="#6A3820", deck="#8C5A34", frame="#A06A3E", band="#C99A46", toe="#E8D4A6",
                strings=12, eyes="lidded", voxel=0.0078, toe_size=1.3, eye_half=(0.028, 0.026))
DARKWOOD, CARVE, INLAY, GOLD, TEAL = "#4A2A18", "#7C4A2A", "#9C6A3C", "#D6A646", "#3E9E94"


def diamond(h):
    return [(0, -h), (h * 0.72, 0), (0, h), (-h * 0.72, 0)]


def ornament(k, P):
    """The inlaid band round the box's sides: dark wood, gold and teal diamonds along it."""
    K = P["k"]
    y0, y1 = -0.19 * K, 0.25 * K
    z0 = 0.172 * K
    zb, zt = z0 + 0.052 * K, z0 + 0.088 * K
    up = np.array([0, 0, 1.0])
    faces = []
    for s in (1, -1):                                     # the two long sides
        a = np.array([s * tb.halfwidth(P, y0), y0, 0]); b = np.array([s * tb.halfwidth(P, y1), y1, 0])
        n = unit(s, -abs(b[0] - a[0]) / (y1 - y0), 0)
        faces.append((a, b, n))
    for y in (y1,):                                       # the back (the neck comes out of the front)
        a = np.array([tb.halfwidth(P, y), y, 0]); b = np.array([-tb.halfwidth(P, y), y, 0])
        faces.append((a, b, np.array([0, 1.0, 0])))
    a = np.array([tb.halfwidth(P, y0), y0, 0]); b = np.array([-tb.halfwidth(P, y0), y0, 0])
    faces.append((a, b, np.array([0, -1.0, 0])))
    for fi, (a, b, n) in enumerate(faces):
        L = np.linalg.norm(b - a)
        ex = (b - a) / L
        o = a + ex * 0.02 * K + n * 0.003 * K + np.array([0, 0, zb])
        span = L - 0.04 * K
        k.add(f"orn{fi}", slab([(0, 0), (span, 0), (span, zt - zb), (0, zt - zb)], o, ex, up, 0.004 * K),
              DARKWOOD, "body", fuse=False, surface="shell")
        m = max(3, int(span / (0.06 * K)))
        for j in range(m):
            c = o + ex * span * (j + 0.5) / m + up * (zt - zb) / 2 + n * 0.003 * K
            col = TEAL if (j % 2) else GOLD
            k.add(f"orn{fi}d{j}", slab(diamond(0.014 * K), c, ex, up, 0.004 * K), col, "body", fuse=False,
                  surface="shell")


def skirt(k, P):
    """The carved shell under the box: pointed scute flaps hanging round its sides and back, each with gold inlay."""
    K = P["k"]
    y0, y1 = -0.19 * K, 0.25 * K
    z0 = 0.172 * K
    spots = []
    for s in (1, -1):
        for t in (0.12, 0.38, 0.64, 0.9):
            y = y0 + (y1 - y0) * t
            spots.append((np.array([s * (tb.halfwidth(P, y) - 0.01 * K), y, z0]), np.array([s, 0, 0.0])))
    for x in (-0.12, 0.0, 0.12):
        spots.append((np.array([x * K, y1 - 0.01 * K, z0]), np.array([0, 1.0, 0])))
    for i, (p, n) in enumerate(spots):
        side = np.cross((0, 0, 1.0), n)
        w = 0.05 * K
        pts = []
        for d in (0.0, 0.016 * K):
            q = p - n * d
            pts += [q + side * w + np.array([0, 0, 0.006 * K]), q - side * w + np.array([0, 0, 0.006 * K]),
                    q + side * w * 0.8 - np.array([0, 0, 0.04 * K]) - n * 0.012 * K,
                    q - side * w * 0.8 - np.array([0, 0, 0.04 * K]) - n * 0.012 * K,
                    q - np.array([0, 0, 0.075 * K]) - n * 0.03 * K]
        k.add(f"scute{i}", hull(pts), CARVE, "body", fuse=False, surface="shell")
        c = p - np.array([0, 0, 0.03 * K]) + n * 0.006 * K - n * 0.008 * K
        k.add(f"scute{i}gold", slab(diamond(0.013 * K), c, side, unit(*(np.array([0, 0, 1.0]) - n * 0.25)), 0.004 * K),
              GOLD, "body", fuse=False, surface="shell")


def crest(k, P, H, HR):
    """The carved wooden crest on the head: a tall lobe between two scrolls, a lighter inset, a gold diamond."""
    o = np.array([0, H[1] - 0.055, H[2] + HR[2] * 0.55])
    ex = np.array([1.0, 0, 0]); ez = unit(0, 0.25, 1)
    g = 1.45
    out_ = [(a * g, b * g) for a, b in
            [(-0.055, 0), (0.055, 0), (0.07, 0.028), (0.085, 0.052), (0.074, 0.074), (0.052, 0.066), (0.046, 0.088),
             (0.026, 0.108), (0, 0.125), (-0.026, 0.108), (-0.046, 0.088), (-0.052, 0.066), (-0.074, 0.074),
             (-0.085, 0.052), (-0.07, 0.028)]]
    k.add("crest", slab(out_, o, ex, ez, 0.034), CARVE, "head", fuse=False, surface="shell")
    n = -np.cross(ex, ez); n = n / np.linalg.norm(n)
    if n[1] > 0:
        n = -n
    inner = [(a * 0.72, 0.012 + b * 0.74) for a, b in out_]
    k.add("crestinset", slab(inner, o + n * 0.019, ex, ez, 0.006), INLAY, "head", fuse=False, surface="shell")
    k.add("crestgold", slab(diamond(0.026), o + n * 0.024 + ez * 0.09, ex, ez, 0.005), GOLD, "head", fuse=False,
          surface="shell")


def extra(k, P, H, HR):
    ornament(k, P)
    skirt(k, P)
    crest(k, P, H, HR)


if __name__ == "__main__":
    src, out = args()
    tb.cimbalom(src, out, TAMBALON, extra)
