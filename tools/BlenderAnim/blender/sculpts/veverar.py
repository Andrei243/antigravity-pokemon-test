# Veverar sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/veverar.py -- <the export's Veverar.glb> <out.glb>
# Carpathia No. 10092, the Normal/Grass hoarder squirrel (docs/plans/decisions/CP-dex-2b-mountains-more.md,
# "92. Veverar"). Grown from veverel.py's frame (its head, cheeks, eyes, ears and brushes, belly, legs and tail line),
# on the export's skeleton, which is Veverel's at 1.9 times. From its picture
# (docs/art/reference/gemini/carpathia/10092-Veverar.jpg): a big russet squirrel standing tall on its hind legs, the
# kit's colours, its cream cheeks less puffed and its head smaller against a long pear of a body; long ears with bigger
# dark brushes; its forepaws held up before its belly; in its chest the cream fur parts round a hollow like a tree's
# knot, crammed with nuts and acorns; its tail a great plume of green fir needles round a russet core swirled darker,
# with golden beech leaves caught in it.
# Reference ratios (picture, the ear brushes to the feet): head height / total 0.21 (here 0.23), ears with brushes /
# total 0.32 (here 0.31), the hollow's height / body height 0.3 (here 0.3), tail height / total 0.8 (here 0.8).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import ell, leaf, outward, unit, axes, sibling, args

vv = sibling("veverel")

VEVERAR = dict(vv.VEVEREL)
VEVERAR.update(
    name="Veverar",
    height=1.035,
    voxel=0.0084,
    s=1.9,
    tailc="#2C5232", swirl="#8C3A22", swirl_cover=0.4, fir="#2E5A34", fir2="#3F7444", beech="#D8A838", beech2="#B88624",
    hole="#3A2414", acorn="#9A6A36", hazel="#7A4E2A", cup="#5A3E22",
    H=(0, -0.03, 0.322), HR=(0.073, 0.067, 0.068),
    cheek=((0.044, -0.03, -0.03), (0.035, 0.034, 0.032)),
    muzzle=((0, -0.06, -0.02), (0.026, 0.024, 0.022)),
    chest=((0, -0.016, 0.235), (0.07, 0.064, 0.09)),
    belly=((0, -0.004, 0.14), (0.085, 0.076, 0.1)),
    belly_top=0.29,
    haunch=((0.06, 0.018, 0.082), (0.05, 0.066, 0.072)),
    foot=((0.066, -0.05, 0.012), (0.026, 0.058, 0.013)),
    arm=[(0.05, -0.032, 0.24), (0.068, -0.07, 0.205), (0.05, -0.094, 0.178)], arm_r=[0.019, 0.016, 0.014],
    hand=((0.046, -0.098, 0.172), (0.016, 0.015, 0.016)),
    hold_nut=False,
    ear_at=(0.42, 0.1, 0.86), ear_tip=(0.018, 0.006, 0.128), ear_w=0.02, ear_d=0.008, brush=0.046,
    tail=[(0, 0.055, 0.075), (0, 0.13, 0.09), (0, 0.18, 0.18), (0, 0.17, 0.3), (0, 0.19, 0.4), (0, 0.26, 0.445),
          (0, 0.315, 0.4), (0, 0.29, 0.34)],
    tail_r=[0.03, 0.05, 0.06, 0.062, 0.058, 0.05, 0.04, 0.028],
    tail_kind="bough",
    eye_x=0.036, eye_dz=0.012, eye_half=(0.02, 0.023), ring=1.3,
)


def hoard(k, P):
    """The hollow in the chest and its nuts; the plume of fir needles and beech leaves round the tail's core."""
    s = P["s"]
    eps = P["height"] * 0.004
    # the hollow: a dark knot-hole on the chest's front, the cream fur parted round it in points, nuts crammed in
    c, r = P["chest"]
    hc = np.array([0.0, (c[1] - r[1]) * s + 0.006 * s, 0.235 * s])
    k.decal(hc, unit(0, -1, 0.05), (0.031 * s, 0.047 * s), P["hole"], eps=eps, rings=10, segs=40,
            outline=vv.polygon([(0, 1), (0.6, 0.55), (0.9, 0), (0.6, -0.6), (0, -1), (-0.6, -0.6), (-0.9, 0), (-0.6, 0.55)]))
    rng = np.random.RandomState(8)
    for i, (x, z) in enumerate(((-0.012, 0.012), (0.011, 0.016), (0.0, -0.005), (-0.013, -0.02), (0.013, -0.018),
                                (0.0, 0.03), (0.002, -0.034))):
        p = hc + np.array([x * 0.9 * s, 0.002 * s, z * 0.85 * s])
        rr = (0.0095 + 0.002 * rng.rand()) * s
        kind = i % 3
        col = (P["acorn"], P["hazel"], P["acorn"])[kind]
        k.add(f"nut{i}", outward(ell(p, (rr, rr * 0.85, rr * (1.25 if kind == 0 else 1.0)), rot=(0, 0, rng.rand() * 90),
                                     sub=3)), col, "body", fuse=False, surface="shell")
        if kind == 0:
            k.add(f"cup{i}", outward(ell(p + np.array([0, 0, rr * 0.7]), (rr * 0.95, rr * 0.82, rr * 0.5), sub=3)), P["cup"],
                  "body", fuse=False, surface="shell")
    for i in range(10):
        th = 2 * math.pi * i / 10 + 0.15
        o = np.array([math.sin(th) * 0.03, 0, math.cos(th) * 0.046]) * s
        way = unit(math.sin(th), -0.6, math.cos(th))
        k.add(f"rim{i}", vv.spike(hc + o + np.array([0, 0.008 * s, 0]), way, 0.022 * s, 0.011 * s, 0.008 * s,
                                  across=(0, 1, 0)), P["cream"], "body", fuse=False, surface="fur")

    # the plume: fir needles radiating from the core, more and longer on its outer side, with beech leaves among them
    pts = vv.tail_pts(P)
    mid = np.mean(pts, axis=0)
    rng = np.random.RandomState(12)
    n_st = 17
    for i in range(n_st):
        t = 0.7 + (len(pts) - 1.8) * i / (n_st - 1)
        j = min(int(t), len(pts) - 2); f = t - j
        c = pts[j] * (1 - f) + pts[j + 1] * f
        rr = (P["tail_r"][j] * (1 - f) + P["tail_r"][j + 1] * f) * s
        d = unit(*(pts[j + 1] - pts[j]))
        a, b = axes(d)
        o = c - mid; o -= d * (o @ d)
        o = unit(*o) if np.linalg.norm(o) > 1e-6 else a
        side = np.cross(d, o)
        for m in range(11):
            th = -2.3 + 4.6 * m / 10 + rng.normal(0, 0.12)
            out_ = unit(*(o * math.cos(th) + side * math.sin(th)))
            ln = (0.085 + 0.035 * math.cos(th) + 0.02 * rng.rand()) * s
            way = unit(*(out_ + d * 0.4))
            root = c + out_ * rr * 0.35
            tip = root + way * ln
            if tip[2] < 0.03:
                continue
            if tip[2] > P["height"] * 0.985 and way[2] > 0.05:
                ln = (P["height"] * 0.985 - root[2]) / way[2]
            k.add(f"needle{i}_{m}", vv.spike(root, way, ln, 0.019 * s, 0.012 * s,
                                             across=np.cross(way, d)),
                  P["fir"] if (i + m) % 2 else P["fir2"], "tail", fuse=False, surface="leaf")
        if i % 1 == 0 and i < n_st - 1:
            th = (0.9 if i % 2 else -0.9) + rng.normal(0, 0.3)
            out_ = unit(*(o * math.cos(th) + side * math.sin(th)))
            base = c + out_ * rr * 1.1
            way = unit(*(out_ * 0.8 + d * 0.5 + np.array([0.3, -0.3, 0.2])))
            k.add(f"beech{i}", outward(leaf(base, way, 0.052 * s, 0.022 * s, thick=0.007 * s)),
                  P["beech"] if i % 3 else P["beech2"], "tail", fuse=False, surface="leaf")


if __name__ == "__main__":
    vv.squirrel(*args(), VEVERAR, extra=hoard)
