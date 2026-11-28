# Mega Dynamax Starmie (a form of our own) on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/starmiemegammax.py -- <the export's Starmie-Mega-Mmax.glb> <out.glb>
#
# Mega Starmie as starmiemega.py builds it, whole and unchanged, with what the form lays over it: a second, larger star
# of rainbow crystal standing behind its own (red point up behind the head, violet and cyan to the upper and lower left,
# blue and gold to the right, each point cut in a light and a dark facet), the Key Stone's emblem as a ring of cyan light
# round the gem with a four-pointed cyan star about it (the gem, which takes the eyes' states, left as it is), and the
# clouds of Max energy: a broken ring of crimson and violet mounds about the feet with cyan curls, two wisps rising at
# the sides and three bolts of lightning in them.
#
# starmiemega.py builds under __main__ with no function to call: its five lines are replayed here, and the clouds are a
# build of their own (a mesh "mmclouds" made before the body's), so that the body's fused surface, its colours, weights
# and the gem's patches are the Mega's exactly.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, ring_band, outward, unit, sibling, args

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
EMBLEM = "#58DCEC"

m = sibling("starmiemega")
y = m.y
C, F = m.C, m.F            # the gem's middle (0, 0, 0.68) and the star's frame (across, up its face, out of it)

# The rainbow star: (angle in the star's plane from across, degrees; length from the middle; light, dark facet)
RAINBOW = [(90, 0.43, "#FF8C92", "#E8505E"), (130, 0.5, "#C894F4", "#9A62E0"), (205, 0.46, "#9AF0F6", "#4CCCE0"),
           (55, 0.5, "#9CB8FF", "#6684EA"), (18, 0.49, "#FFE48A", "#F0B840")]
RD = -0.11                 # behind the back star (whose back face is at -0.09)


def rainbow(k):
    for i, (ang, L, light, dark) in enumerate(RAINBOW):
        a = math.radians(ang)
        d = np.array([math.cos(a), math.sin(a)])
        s = np.array([-d[1], d[0]])
        def p(r, w, dd):
            q = d * r + s * w
            return y.at(C, F, q[0], q[1], RD + dd)
        for side, col in ((1, light), (-1, dark)):
            pts = [p(0.1, 0, 0.026), p(0.1, 0, -0.026), p(L, 0, 0.004), p(L, 0, -0.004),
                   p(0.1, side * 0.12, 0.016), p(0.1, side * 0.12, -0.016),
                   p(0.55 * L, side * 0.08, 0.012), p(0.55 * L, side * 0.08, -0.012)]
            k.add(f"mmray{i}{'ab'[side < 0]}", hull(pts), col, "body", fuse=False, glow=col, surface="shell")


def emblem(k):
    # A ring of cyan light on the gold ring's lip about the gem's girdle, and a four-pointed star of light about it
    k.add("mmemblemring", ring_band(y.at(C, F, 0, 0, 0.118), F[2], 0.09, 0.014, thick=0.014), EMBLEM, "body",
          fuse=False, glow=EMBLEM)
    for j in range(4):
        a = math.radians(45 + 90 * j)
        d = np.array([math.cos(a), math.sin(a)]); s = np.array([-d[1], d[0]])
        def p(r, w, dd):
            q = d * r + s * w
            return y.at(C, F, q[0], q[1], dd)
        pts = [p(0.088, 0.024, 0.122), p(0.088, -0.024, 0.122), p(0.088, 0.024, 0.104), p(0.088, -0.024, 0.104),
               p(0.165, 0, 0.112), p(0.165, 0, 0.102)]
        k.add(f"mmstar{j}", hull(pts), EMBLEM, "body", fuse=False, glow=EMBLEM)


def clump(k, name, c, tangent, radial, s0, col, other, rnd):
    """A mound of cloud: three round puffs of mixed sizes on the ground and a small one riding on top."""
    for j, (sz, t, r, top) in enumerate([(1.0, 0.0, 0.0, 0), (0.78, 0.62, 0.25, 0), (0.66, -0.58, 0.18, 0),
                                         (0.5, 0.1, -0.2, 1)]):
        s = s0 * sz * (1 + 0.12 * rnd.uniform(-1, 1))
        p = c + tangent * t * s0 + radial * r * s0
        z = s0 * 1.45 if top else s * 0.9
        k.add(f"{name}_{j}", ell((p[0], p[1], z), (s, s, s * 0.9)), other if j == 2 else col, "root")


def ring(k, cx, cy, rx, ry, angles, s0, seed):
    rnd = np.random.default_rng(seed)
    cols = (CRIMSON, VIOLET, DEEP, DEEPV)
    centres = []
    for i, ang in enumerate(angles):
        a = math.radians(ang)
        c = np.array([cx + rx * math.cos(a), cy + ry * math.sin(a)])
        tg = np.array([-rx * math.sin(a), ry * math.cos(a)]); tg /= np.linalg.norm(tg)
        rd = np.array([tg[1], -tg[0]])
        clump(k, f"mmcloud{i}", c, tg, rd, s0, cols[i % 4], cols[(i + 1) % 4], rnd)
        centres.append(c)
    return centres


def bolt(k, name, pts, col):
    k.add(name, outward(tube(pts, [0.015] * len(pts), seg=8)), col, "root", fuse=False, glow=col)


def clouds(k):
    # A broken ring about the feet, open at the front, within about 1.2 times the star's width
    centres = ring(k, 0.05, 0.0, 0.38, 0.27, [-45, -5, 35, 75, 115, 155, 195, 232], 0.08, 7)
    # Two wisps rising at the sides as the picture curls them up past the lower points
    for side, x0 in ((1, 0.44), (-1, -0.34)):
        for j, (dx, dz, s) in enumerate([(0.0, 0.16, 0.066), (-0.045, 0.26, 0.052)]):
            k.add(f"mmwisp{side}_{j}", ell((x0 + side * dx, 0.02, dz), (s, s, s * 0.9)), CRIMSON if j != 1 else DEEP,
                  "root")
    return centres


def curls(k, centres):
    for j, i in enumerate((1, 4, 6)):
        c = centres[i]
        k.add(f"mmcurl{j}", ell((c[0], c[1], 0.15), (0.024, 0.024, 0.022)), CYAN, "root", fuse=False, glow=CYAN)
    bolt(k, "mmbolt0", [(0.43, -0.12, 0.08), (0.47, -0.1, 0.17), (0.41, -0.09, 0.22), (0.46, -0.06, 0.32)], CYAN)
    bolt(k, "mmbolt1", [(-0.34, -0.12, 0.06), (-0.38, -0.1, 0.15), (-0.32, -0.1, 0.2), (-0.37, -0.08, 0.28)], "#B98CF4")
    bolt(k, "mmbolt2", [(0.2, 0.22, 0.08), (0.24, 0.24, 0.16), (0.18, 0.25, 0.2), (0.22, 0.27, 0.27)], CYAN)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    # The clouds' build: a mesh of their own, fused at a voxel a hundredth of the height
    centres = clouds(k)
    curls(k, centres)
    clo = k.build(voxel=0.01, smooth=4, name="mmclouds")
    # Starmie-Mega's own lines (starmiemega.py, __main__), the rainbow star and the emblem laid with it as loose pieces
    m.starmie_mega(k)
    rainbow(k)
    emblem(k)
    body = k.build(voxel=0.004, smooth=3)
    k.eye_states(lambda state: m.eyes(k, state))
    V = np.vstack([np.array([v.co[:] for v in o.data.vertices]) for o in (clo, body)])
    w, l, h = np.ptp(V[:, 0]), np.ptp(V[:, 1]), V[:, 2].max()
    sq = 1.3 * max(h, 0.85 * max(w, l))
    print(f"KIT: board: width {w:.3f} length {l:.3f} height {h:.3f}; sum {0.853 * w + 0.522 * l:.3f} "
          f"of square {sq:.3f} ({(0.853 * w + 0.522 * l) / sq:.2f}); lowest {V[:, 2].min():.3f}")
    k.export(out)
