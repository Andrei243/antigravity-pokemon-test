# Baxcalibur-Mega sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/baxcaliburmega.py -- <the export's Baxcalibur-Mega.glb> <out.glb>
#
# Grown from Baxcalibur (sibling("baxcalibur"), and through it frigibax's shards, claws, hexagons and the yellow eye cut
# by a brow): its navy, its masked head, its claws and red quills. From the references (they overrule the C#
# sculptor's sword with a hilt and gem, which it says was a guess): the dragon no longer stands like a film monster
# but leans far forward over its haunches, trunk near level, head carried low in front, a long heavy tail lying out
# behind on the ground and curling at its end, a pale wavy ridge of ice along its top. The axe's blade on its back has
# grown into the design's largest mass: a great plate of ice rising up and back over the body, as long as the body is
# high, a pale frame with lavender spikes at its corners and edges round a black plate crossed by a pale bar. A pale
# mantle of ice over the shoulders and back, its lower edge spiked; the mask's crest and beard tipped in lavender; a
# lavender star of ice shards at each knee and a pale hexagon on each thigh; arms forward and down, red quills and pale
# claws at the wrists.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, srgb, inside, unit, args, sibling

bx = sibling("baxcalibur")
fb, bg = bx.fb, bx.bg

NAVY, ICE, BLACK, RED = "#2D5584", bx.ICE, bx.BLACK, bx.RED
LAV, FRAME = "#A898E6", "#C4E5F1"
CLAW, SOLE = bx.CLAW, bx.SOLE

D = np.array([0.0, -0.26, -0.25])                                           # the head moved forward and down
HC, HR = bx.HC + D, bx.HR * 1.12                                             # the skull, a tenth larger
VOXEL = 0.0085


def head(k):
    S = 1.12
    def at(p):                       # the base's head points, moved and grown about its skull
        return tuple(HC + (np.array(p, float) - bx.HC) * S)
    k.add("neck", tube([(0, -0.17, 0.56), (0, -0.28, 0.6), at((0, -0.08, 0.88))], [0.13, 0.11, 0.09]), NAVY, "head")
    k.add("skull", ell(HC, HR), NAVY, "head")
    k.add("snout", ell(at((0, -0.215, 0.89)), np.array((0.078, 0.1, 0.064)) * S, rot=(10, 0, 0)), ICE, "head",
          surface="shell")
    k.add("jaw", ell(at((0, -0.19, 0.845)), np.array((0.07, 0.085, 0.035)) * S, rot=(14, 0, 0)), ICE, "head",
          surface="shell")
    k.add("crest", hull([at(p) for p in [(0, -0.33, 0.925), (0, -0.31, 0.9), (0.075, -0.2, 0.955), (-0.075, -0.2, 0.955),
                                         (0.08, -0.22, 0.92), (-0.08, -0.22, 0.92), (0.06, -0.08, 1.0),
                                         (-0.06, -0.08, 1.0), (0, -0.03, 1.03), (0, -0.14, 1.02), (0.035, -0.12, 0.93),
                                         (-0.035, -0.12, 0.93)]]), ICE, "head", fuse=False, surface="shell")
    # the mask grown jagged: lavender spikes standing on the snout and swept back over the crown
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        k.add(f"nosespike{sd}", fb.shard(at((s * 0.035, -0.25, 0.93)), (s * 0.25, -0.1, 1), 0.08, 0.042, 0.022,
                                         across=(0, 1, 0)), LAV, "head", fuse=False, surface="shell")
        for j, (p, d, ln) in enumerate((((0.055, -0.1, 0.99), (0.45, 0.6, 0.5), 0.11), ((0.07, -0.02, 0.95), (0.5, 0.8, 0.1), 0.1))):
            k.add(f"crown{sd}{j}", fb.shard(at((s * p[0], p[1], p[2])), (s * d[0], d[1], d[2]), ln, 0.045, 0.024,
                                            across=(1, 0, 0)), LAV, "head", fuse=False, surface="shell")
    beard = [(0.0, -0.25, 0.835, 0.11), (0.045, -0.23, 0.835, 0.1), (-0.045, -0.23, 0.835, 0.1),
             (0.065, -0.17, 0.84, 0.085), (-0.065, -0.17, 0.84, 0.085), (0.0, -0.17, 0.83, 0.09)]
    for i, (x, y, z, ln) in enumerate(beard):
        k.add(f"beard{i}", fb.shard(at((x, y, z)), (x * 2, 0.15, -1), ln, 0.052, 0.022, across=(0, 1, 0)),
              LAV if i in (0, 3, 4) else ICE, "head", fuse=False, surface="shell")


def body(k):
    # leaning far forward: a near-level trunk from the haunches to a chest carried low in front
    k.add("trunk", ell((0, 0.05, 0.42), (0.22, 0.27, 0.21), rot=(-15, 0, 0)), NAVY, "body")
    k.add("chest", ell((0, -0.18, 0.52), (0.17, 0.2, 0.15), rot=(-32, 0, 0)), NAVY, "body")
    # the pale mantle of ice over the shoulders and back, its edge spiked
    k.add("mantle", ell((0, -0.04, 0.57), (0.185, 0.25, 0.1), rot=(-25, 0, 0)), ICE, "body", surface="shell")
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        for j, (y, z, ln) in enumerate(((-0.15, 0.54, 0.085), (-0.02, 0.52, 0.1), (0.12, 0.5, 0.085))):
            k.add(f"mspike{sd}{j}", fb.shard((s * 0.17, y, z), (s * 0.8, 0.15, -0.55), ln, 0.06, 0.024,
                                              across=(0, 0, 1)), ICE, "body", fuse=False, surface="shell")
    # arms forward and down, claws and red quills at the wrists
    k.add("arm", tube([(0.15, -0.16, 0.56), (0.21, -0.26, 0.46), (0.2, -0.38, 0.37)], [0.062, 0.052, 0.046]), NAVY,
          "arm{s}", mirror=True)
    k.add("hand", ell((0.195, -0.42, 0.345), (0.05, 0.052, 0.05)), NAVY, "arm{s}", mirror=True)
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        for j, dx in enumerate((-0.028, 0.0, 0.028)):
            k.add(f"hclaw{sd}{j}", fb.claw((s * (0.195 + dx), -0.45, 0.31), (s * dx * 3, -0.5, -0.85), 0.07, 0.034,
                                           up=(0, 1, 0)), CLAW, f"arm{sd}", fuse=False, surface="shell")
        for j, (d, ln) in enumerate([((0.35, 0.35, -0.85), 0.13), ((0.45, 0.6, -0.55), 0.12), ((0.4, 0.8, -0.2), 0.1)]):
            k.add(f"quill{sd}{j}", fb.shard((s * 0.225, -0.35, 0.4), (d[0] * s, d[1], d[2]), ln, 0.055, 0.026,
                                            across=(0, 0, 1) if j == 2 else (0, 1, 0)), RED, f"arm{sd}", fuse=False,
                  surface="shell")
    # thick legs: haunches fused into the hips, a short shin, wide feet of pale claws on pale soles
    k.add("thigh", ell((0.15, 0.05, 0.27), (0.13, 0.16, 0.17)), NAVY, "leg{s}", mirror=True)
    k.add("shin", tube([(0.16, 0.0, 0.15), (0.165, -0.02, 0.09)], [0.085, 0.075]), NAVY, "leg{s}", mirror=True)
    k.add("foot", ell((0.165, -0.07, 0.038), (0.09, 0.125, 0.042)), NAVY, "leg{s}", mirror=True)
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        for j, dx in enumerate((-0.05, -0.017, 0.017, 0.05)):
            k.add(f"toe{sd}{j}", fb.claw((s * (0.165 + dx), -0.18, 0.042), (s * dx * 4, -1, -0.25), 0.06, 0.038),
                  CLAW, f"leg{sd}", fuse=False, surface="shell")
        # the lavender star of ice at the knee
        knee = bg.on_ell((0.15 * s, 0.05, 0.27), (0.13, 0.16, 0.17), (0.35 * s, -1, -0.35))
        for j, (a, ln) in enumerate(((90, 0.1), (160, 0.075), (230, 0.085), (300, 0.08), (20, 0.07))):
            t = math.radians(a)
            d = unit(math.cos(t) * s * 0.9 + 0.2 * s, -0.55, math.sin(t))
            k.add(f"star{sd}{j}", fb.shard(knee + d * 0.01, d, ln, 0.045, 0.024, across=(0, 1, 0)), LAV, f"leg{sd}",
                  fuse=False, surface="shell")
    k.paint(lambda P: inside(P[:, 2] - 0.012, 0.004), SOLE, on=["foot"])
    # the long heavy tail lying out behind, curling to +X at its end, a pale wavy ridge along its top
    tp = np.array([(0, 0.22, 0.36), (0, 0.42, 0.2), (0.05, 0.62, 0.1), (0.15, 0.78, 0.075), (0.27, 0.86, 0.1)])
    tr = np.array([0.15, 0.11, 0.08, 0.055, 0.03])
    k.add("tail", tube(tp, tr), NAVY, "tail")
    line = np.vstack([tp[i] + (tp[i + 1] - tp[i]) * t for i in range(len(tp) - 1) for t in np.linspace(0, 1, 25)])
    rad = np.concatenate([tr[i] + (tr[i + 1] - tr[i]) * np.linspace(0, 1, 25) for i in range(len(tp) - 1)])
    along = np.concatenate([i + np.linspace(0, 1, 25) for i in range(len(tp) - 1)])
    def ridge(P):
        j = np.argmin(((P[:, None, :] - line[None]) ** 2).sum(2), axis=1)
        top = line[j, 2] + rad[j] * 0.6
        return inside(top - P[:, 2], 0.004) * (along[j] > 0.35)
    k.paint(ridge, ICE, on=["tail"])
    # a pale hexagon on each thigh's outer side
    for s in (1, -1):
        d = unit(1.0 * s, -0.25, 0.1)
        k.decal(bg.on_ell((0.15 * s, 0.05, 0.27), (0.13, 0.16, 0.17), d), d, (0.055, 0.05), ICE, eps=0.004,
                outline=fb.hexagon(1.0), rings=10, segs=60)


def blade(k):
    """The great blade on the back: rising up and back at 55 degrees, its plane yawed 35 degrees out to +X so its face
    shows to both of the game's cameras; a pale frame, lavender spikes, a black plate crossed by a pale bar."""
    ex = unit(math.sin(math.radians(35)), math.cos(math.radians(35)), 0)       # back, and out to +X
    ez = np.array([0, 0, 1.0])
    root = np.array([0.0, 0.03, 0.64])
    up = math.radians(57)
    dirv, perp = np.array([math.cos(up), math.sin(up)]), np.array([-math.sin(up), math.cos(up)])
    L = 0.68
    def ab(u, w):                    # along the blade (0..1 of L), across it (+ toward the front)
        p = dirv * (u * L) + perp * w
        return (p[0], p[1])
    FR = [ab(0.0, 0.08), ab(0.12, 0.13), ab(0.55, 0.17), ab(0.93, 0.2), ab(1.0, 0.185), ab(1.02, 0.0),
          ab(1.0, -0.17), ab(0.93, -0.19), ab(0.5, -0.155), ab(0.1, -0.11), ab(0.0, -0.07)]
    k.add("blade", slab(FR, root, ex, ez, 0.036), FRAME, "body", fuse=False, surface="shell")
    BK = [ab(0.2, 0.1), ab(0.55, 0.125), ab(0.88, 0.155), ab(0.88, -0.145), ab(0.5, -0.12), ab(0.2, -0.075)]
    k.add("blackplate", slab(BK, root, ex, ez, 0.048), BLACK, "body", fuse=False, surface="shell")
    BAR = [ab(0.22, 0.018), ab(0.66, 0.02), ab(0.66, 0.135), ab(0.72, 0.145), ab(0.74, 0.02), ab(0.78, 0.0),
           ab(0.74, -0.02), ab(0.72, -0.13), ab(0.66, -0.125), ab(0.66, -0.02), ab(0.22, -0.016)]
    k.add("bar", slab(BAR, root, ex, ez, 0.06), FRAME, "body", fuse=False, surface="shell")
    # lavender spikes at the corners and along the edges, in the blade's plane
    n = np.cross(ex, ez)
    def world(a, b):
        return root + ex * a + ez * b
    for i, (u, w, side, ln) in enumerate(((1.0, 0.185, 1, 0.12), (1.0, -0.17, -1, 0.11), (0.8, 0.19, 1, 0.09),
                                          (0.75, -0.17, -1, 0.09), (0.45, 0.16, 1, 0.08), (1.02, 0.0, 0, 0.1))):
        p = world(*ab(u, w))
        out2 = perp * side + dirv * (0.6 if side else 1.0)
        d = ex * out2[0] + ez * out2[1]
        k.add(f"bspike{i}", fb.shard(p - d * 0.02, d, ln, 0.055, 0.034, across=n), LAV, "body", fuse=False,
              surface="shell")
    # the pale spike at the blade's root, pointing forward and up
    p = world(*ab(0.12, 0.11))
    d = ex * (-0.7) + ez * 0.7
    k.add("rootspike", fb.shard(p, d, 0.13, 0.06, 0.03, across=n), FRAME, "body", fuse=False, surface="shell")
    k.add("bladeroot", tube([(0, -0.04, 0.68), (0.03, 0.06, 0.7), (0.06, 0.14, 0.62)], [0.07, 0.075, 0.06]), NAVY, "body")


def baxcaliburmega(src, out_path):
    k = Kit(src, coat="scales")
    body(k)
    head(k)
    blade(k)
    k.build(voxel=VOXEL, smooth=3, band=0.007, wband=0.07)
    cut, look = fb.yellow_eye(BA=0.3, BB=0.45, pupil_c=(-0.2, -0.08), pupil_r=0.48)
    de = unit(0.8, -0.5, 0.25)
    E = bg.on_ell(HC, HR, de)
    k.eye_states(fb.eyes_under_brow(k, E, de, (0.036, 0.032), look, cut, srgb(NAVY), eps=0.004))
    k.export(out_path)


if __name__ == "__main__":
    baxcaliburmega(*args())
