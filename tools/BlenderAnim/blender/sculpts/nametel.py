# Nametel (Carpathia No. 10103, the snow puff) sculpted from nothing on the skeleton and clips of the game's own export
# of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/nametel.py -- <the export's Nametel.glb> <out.glb>
#
# From its picture (docs/art/reference/gemini/carpathia/10103-Nametel.jpg) and its brief (CP-dex-2b-mountains-more.md,
# 103): a round puff of fresh powdery snow, a little wider than tall, its outline bumpy with clumps of snow all round
# (smooth over the face), blue-grey in the shade under it; two coal-dark lumpy dot eyes, a pink blush under each, a
# small soft smile; two thin brown twig arms coming out of its sides, out and down, forked at their ends. The crumbs of
# snow round it are left out.
# The line's frame for Nameton (nameton.py): the palette, on_ell, clumps(), the coal eye and the blush.
# Bones: root (0, 0, 0), body (0, 0, 0.204), armR/L (+-0.17, -0.02, 0.174).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, outward, srgb, inside, smoothstep, unit, eye_look, polygon, args

# --- the line's palette ----------------------------------------------------------------------------------------------
SNOW, SHADE, DEEPSHADE = "#EEF3FA", "#BCCDE2", "#9DB3CF"
COAL, COALGLINT, CHEEK, MOUTH = "#26262D", "#7C7C88", "#F2A8B6", "#8EA0BE"
TWIG, TWIGDARK = "#6E4C34", "#4E3524"

BC, BR = np.array([0.0, 0.0, 0.214]), np.array([0.2, 0.186, 0.206])
VOXEL = 0.0042


def on_ell(c, r, az, el, lift=0.0):
    """The point of an ellipsoid at an azimuth from the front (+ toward +X) and an elevation, in degrees, and its
    outward normal there."""
    a, e = math.radians(az), math.radians(el)
    d = np.array([math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)])
    p = np.array(c, float) + np.array(r, float) * d
    n = d / np.array(r, float); n /= np.linalg.norm(n)
    return p + n * lift, n


def clumps(k, name, c, r, n, size, bone, colour=SNOW, keep_clear=None, el_range=(-40, 75), seed=10103, sink=0.55):
    """Clumps of packed snow over a mass: fused balls spread round it on a golden spiral, jittered, each sunk so that
    a share of it stands proud; keep_clear(az, el) leaves the face smooth."""
    rs = np.random.RandomState(seed)
    lo, hi = [math.sin(math.radians(e)) for e in el_range]
    placed = 0
    for i in range(n):
        t = (i + 0.5) / n
        z = lo + (hi - lo) * t
        az = (i * 137.508 + rs.uniform(-10, 10)) % 360 - 180
        el = math.degrees(math.asin(z)) + rs.uniform(-4, 4)
        if keep_clear is not None and keep_clear(az, el):
            continue
        p, nn = on_ell(c, r, az, el)
        s = size * rs.uniform(0.75, 1.2)
        k.add(f"{name}{placed:02d}", ell(p - nn * s * sink, (s, s, s * 0.9), sub=3), colour, bone, surface="fur")
        placed += 1


def coal_eye(state, lid):
    """The line's coal eye: a dark lump with a ragged edge (its outline does that) and a dull grey glint."""
    Ck, Gl = srgb(COAL), srgb(COALGLINT)
    def open_look(u, v):
        c = np.tile(Ck, (len(u), 1))
        c[((u - 0.25) / 0.24) ** 2 + ((v - 0.32) / 0.22) ** 2 < 1] = Gl
        return c
    return eye_look(state, open_look, srgb(lid), Ck, line=0.2)


LUMP = polygon([(1.0, 0.1), (0.8, 0.62), (0.35, 0.95), (-0.2, 0.85), (-0.7, 0.75), (-1.0, 0.2), (-0.85, -0.45),
                (-0.4, -0.95), (0.2, -0.8), (0.75, -0.75)])


def twig_arm(k, s, nm, root, tip, fork, r0):
    """A thin twig, loose, out of the snow and down, forked toward its end, a smaller spur off its top."""
    mid = root * 0.45 + tip * 0.55 + np.array([0, 0, 0.012])
    k.add("twig" + nm, outward(tube([root, mid, tip], [r0, r0 * 0.85, r0 * 0.65])), TWIG, "arm" + nm, fuse=False,
          surface="leather")
    k.add("fork" + nm, outward(tube([mid * 0.4 + tip * 0.6, fork], [r0 * 0.72, r0 * 0.55])), TWIG, "arm" + nm,
          fuse=False, surface="leather")


def nametel(src, out):
    k = Kit(src, coat="fur")
    k.add("puff", ell(BC, BR), SNOW, "body")
    clumps(k, "clump", BC, BR, 46, 0.056, "body", keep_clear=lambda az, el: abs(az) < 44 and -30 < el < 46)
    # under the puff the snow lies in shade
    k.paint(lambda P: smoothstep(0.15, 0.04, P[:, 2]), SHADE)
    k.paint(lambda P: smoothstep(0.06, 0.0, P[:, 2]), DEEPSHADE)
    k.paint(lambda P: smoothstep(0.08, 0.2, P[:, 1]) * smoothstep(0.3, 0.12, P[:, 2]), SHADE)
    # twig arms, out and down and a little forward
    for s, nm in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        root = np.array((0.15, -0.03, 0.18)) * m
        tip = np.array((0.25, -0.1, 0.1)) * m
        fork = np.array((0.255, -0.12, 0.155)) * m
        twig_arm(k, s, nm, root, tip, fork, 0.0095)
    # blush and the small smile, in every state
    for s in (1, -1):
        p, n = on_ell(BC, BR, 33 * s, 5)
        k.decal(p, n, (0.024, 0.014), CHEEK, eps=0.0016, rings=10, segs=40)
    Sn, Mo = srgb(SNOW), srgb(MOUTH)
    def smile(u, v):
        c = np.tile(Sn, (len(u), 1))
        c[(np.abs(v - (-0.3 + 0.6 * u * u)) < 0.3) & (np.abs(u) < 0.85)] = Mo
        return c
    p, n = on_ell(BC, BR, 0, -3)
    k.decal(p, n, (0.018, 0.007), smile, eps=0.0016, rings=10, segs=48)
    k.build(voxel=VOXEL, smooth=4, band=0.005, wband=0.04)
    board(k.ob)
    def lay(state):
        look = coal_eye(state, SNOW)
        for s in (1, -1):
            p, n = on_ell(BC, BR, 21 * s, 21)
            col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
            k.decal(p, n, (0.021, 0.021), col, eps=0.0018, outline=LUMP if s > 0 else (lambda th: LUMP(math.pi - th)),
                    rings=20, segs=56)
    k.eye_states(lay)
    k.export(out)


def board(ob):
    V = np.array([v.co[:] for v in ob.data.vertices])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    sq = 1.3 * max(h, 0.85 * max(w, l))
    print("KIT: board: shows %.3f (corners %.3f) in a square of %.3f" % (np.ptp(proj), 0.853 * w + 0.522 * l, sq))


if __name__ == "__main__":
    nametel(*args())
