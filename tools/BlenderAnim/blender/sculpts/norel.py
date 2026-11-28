# Norel (No. 10110, Carpathia) sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py):
#   blender --background --factory-startup --python sculpts/norel.py -- <the export's Norel.glb> <out.glb>
#
# The design, from its picture (docs/art/reference/gemini/carpathia/10110-Norel.jpg) and its brief (CP-dex-2b, "110.
# Norel: a little storm cloud"): a small round storm cloud, slate grey, its outline a ring of big round billows (three
# on top, one each side, two below) with a front mass of its own (the face) whose edge is scalloped in small bumps;
# the lower billows a shade darker, the tops lighter. Bristles of static stand out of its edges in clusters of
# dark pointed spikes, a big one up and out on the right. Two glowing yellow eyes, wide and cut across their tops by
# heavy dark brows that slope down to a furrow over the nose (a glower); a small pouting frown under them. Small
# yellow crackles run on its lower front (the brief's sparks from its underside, fixed to it: the picture's loose
# sparks in the air are left out). It drifts above the ground (the export hovers: lowest 0.044).
#
# The skeleton is root and body (0, 0, 0.207) alone: everything is on body.
#
# Written to be grown from (tunetor.py imports it with sibling("norel")): the billows, the static spikes, the crackles,
# the glowering eyes and the board check are functions taking what changes; Norel's calls are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, inside, unit, eye_look, polygon, args, hull, slab, axes

# --- the line's colours ---------------------------------------------------------------------------------------------
CLOUD, CLOUD_TOP, CLOUD_LOW = "#6E7687", "#848DA1", "#575E6E"
FACE = "#79828F"
SPIKE = "#3C4250"
BROW = "#444A58"
MOUTH = "#33384A"
SPARK = "#FFD43A"
EYE_CORE, EYE_MID, EYE_EDGE = srgb("#FFF6B8"), srgb("#FFD23C"), srgb("#D9961C")
EYE_RIM = srgb("#2A2E3A")
INK = srgb("#262A36")
WHITE = np.array([1.0, 1.0, 1.0])

# --- Norel's frame --------------------------------------------------------------------------------------------------
C = np.array([0.0, 0.0, 0.225])              # the cloud's middle (body joint 0.207)
R = np.array([0.15, 0.125, 0.112])
FC = np.array([0.0, -0.045, 0.224])          # the face's own mass, forward of the middle
FR = np.array([0.125, 0.08, 0.1])
EYE_D = (0.43, -0.9, 0.16)
EYE_HALF = (0.034, 0.029)
CUT = (0.3, 0.45)                            # the brow's cut across the eye's top: v = a + b u (u outward)
MOUTH_D = (0.0, -1.0, -0.32)
VOXEL = 0.004

# billows round the edge: centre, radius, colour (x right, y back, z up)
BILLOWS = [
    ((-0.085, 0.015, 0.315), 0.072, CLOUD_TOP), ((0.0, 0.03, 0.33), 0.078, CLOUD_TOP),
    ((0.09, 0.02, 0.312), 0.072, CLOUD_TOP),
    ((-0.152, 0.0, 0.235), 0.072, CLOUD), ((0.155, 0.0, 0.24), 0.074, CLOUD),
    ((-0.118, -0.01, 0.135), 0.07, CLOUD_LOW), ((0.118, -0.01, 0.132), 0.07, CLOUD_LOW),
    ((0.0, -0.02, 0.118), 0.072, CLOUD_LOW),
    ((0.0, 0.085, 0.27), 0.09, CLOUD), ((-0.09, 0.075, 0.17), 0.075, CLOUD_LOW), ((0.09, 0.075, 0.175), 0.075, CLOUD_LOW),
    ((-0.07, 0.09, 0.3), 0.065, CLOUD_TOP), ((0.08, 0.09, 0.29), 0.065, CLOUD_TOP),
]
# the static's clusters: direction out from the middle, length (the picture's big one up and out on +X)
SPIKES = [
    ((0.55, 0.05, 0.83), 0.085), ((0.92, 0.06, 0.32), 0.065), ((0.86, 0.1, -0.45), 0.055),
    ((-0.42, 0.08, 0.9), 0.06), ((-0.93, 0.05, 0.36), 0.075), ((-0.85, 0.1, -0.42), 0.055),
    ((0.45, 0.2, -0.85), 0.035), ((0.2, 0.92, 0.38), 0.085), ((-0.35, 0.88, 0.1), 0.08),
]


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre, and its normal."""
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def billows(k, name, items, bone, sub=3):
    """Round billows of cloud fused round a core: (centre, radius, colour) each."""
    for i, (c, r, col) in enumerate(items):
        k.add(f"{name}{i}", ell(c, (r, r * 0.95, r * 0.92), sub=sub), col, bone)


def scallops(k, name, c, r, z, n, size, color, bone, span=0.85, depth=-1.0):
    """A row of small bumps along the top or bottom edge of a front mass (the scalloped rim the picture draws)."""
    for i in range(n):
        t = -span + 2 * span * i / (n - 1)
        d = (math.sin(t), -math.cos(t) * 0.75, z)
        p, nrm = on_ell(c, r, d)
        p = p - nrm * size * 0.45
        k.add(f"{name}{i}", ell(p, (size, size * 0.85, size * 0.85), sub=3), color, bone)


def spike_cluster(k, name, root, d, length, width, color, bone):
    """A bristle of static: one solid pointed spike out along d and two smaller beside it, flat-faced, sunk at the root."""
    d = unit(*d)
    a, b = axes(d)
    root = np.array(root, float)
    for j, (turn, share, off) in enumerate(((0.0, 1.0, 0.0), (0.5, 0.58, 0.8), (-0.46, 0.5, -0.8))):
        dd = unit(*(d * math.cos(turn) + a * math.sin(turn)))
        aa, bb = axes(dd)
        base = root + a * width * off - d * 0.6 * width
        w = width * (0.72 if j else 1.0)
        pts = [base + aa * w, base - aa * w, base + bb * w * 0.75, base - bb * w * 0.75,
               base + dd * length * share * 0.35 + aa * w * 0.45, base + dd * length * share * 0.35 - aa * w * 0.45,
               base + dd * length * share]
        k.add(f"{name}{j}", hull(pts), color, bone, fuse=False)


BOLT = [(-0.2, 1.0), (0.5, 1.0), (0.12, 0.25), (0.55, 0.25), (-0.35, -1.0), (0.0, -0.1), (-0.4, -0.1)]


def bolt(k, name, at, n, size, color, bone, thick, lean=0.0, mirror_x=False):
    """A crackle of lightning laid on the surface: a zigzag plate in the tangent plane, half sunk, glowing."""
    n = unit(*n)
    up = np.array([math.sin(lean), 0.0, math.cos(lean)])
    ex = np.cross(up, n); ex /= np.linalg.norm(ex)
    ez = np.cross(n, ex)
    pts = [((-a if mirror_x else a) * size * 0.5, b * size) for a, b in BOLT]
    k.add(name, slab(pts, np.array(at, float) + n * thick * 0.15, ex, ez, thick), color, bone, fuse=False, glow=color)


def glow_eye(core=EYE_CORE, mid=EYE_MID, edge=EYE_EDGE, rim=EYE_RIM):
    """The line's glowing eye: a pale heart, a gold body, an amber edge, a dark rim, a white glint toward the top inside."""
    def look(u, v):
        r = np.sqrt(u * u + (v * 1.1) ** 2)
        c = np.tile(mid, (len(u), 1))
        c[r > 0.62] = edge
        c[r < 0.42] = core
        c[((u + 0.12) / 0.2) ** 2 + ((v + 0.05) / 0.2) ** 2 < 1] = WHITE
        c[r > 0.85] = rim
        return c
    return look


def eye_frame(p, n):
    """The decal's own frame (as sculptkit lays it): right = up x n, then up."""
    right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right)
    return right, np.cross(n, right)


def cut_outline(side, cut):
    """The eye's oval with its top cut by the brow's line, in the decal's own (right, up) frame: u outward is side * right."""
    a, b = cut
    line = [(-1.6, a - b * 1.6), (1.6, a + b * 1.6)]
    if side < 0:
        line = [(-x, y) for x, y in line]
    pts = [line[0], line[1], (1.6 if side > 0 else -1.6, -1.6), (-1.6 if side > 0 else 1.6, -1.6)]
    if side < 0:
        pts = [pts[1], pts[0], pts[3], pts[2]]
    poly = polygon(pts)
    return lambda th: min(1.0, poly(th))


def brow(k, p, n, half, side, cut, colour, reach=(1.55, 1.2), height=0.62, depth=0.05):
    """The heavy brow over an eye: paint above the cut line, in the eye's own frame, from past the inner corner to the
    outer, so the eye's own cut edge is the crisp line under it."""
    right, up = eye_frame(p, n)
    a, b = cut
    p = np.array(p, float)
    def mask(P):
        q = P - p
        u = side * (q @ right) / half[0]
        v = (q @ up) / half[1]
        h = q @ n
        line = a + b * u - 0.12
        m = inside(line - v, 0.04) * inside(v - (line + height), 0.04)
        m *= inside(-reach[0] - u, 0.04) * inside(u - reach[1], 0.04)
        return m * inside(np.abs(h) - depth, 0.01)
    k.paint(mask, colour)


def mouth_frown(k, c, r, d, half, colour, eps=0.003):
    """A small pouting frown: an arch, its ends turned down."""
    pts = [(u, 0.32 - 0.6 * u * u) for u in np.linspace(-1, 1, 9)] + \
          [(u, -0.38 - 0.6 * u * u) for u in np.linspace(1, -1, 9)]
    p, nrm = on_ell(c, r, d)
    k.decal(p, nrm, half, colour, eps=eps, outline=polygon(pts), rings=10, segs=72)


def lay_glower(k, c, r, d, half, cut, lid, state, look=None, ink=INK, eps=0.003):
    """Both eyes, each cut by its brow, laid by hand (the cut is not symmetric about the eye's middle)."""
    look = look or glow_eye()
    for side in (1, -1):
        dd = (d[0] * side, d[1], d[2])
        p, nrm = on_ell(c, r, dd)
        f = eye_look(state, look, srgb(lid) if isinstance(lid, str) else lid, ink, line=0.16)
        col = f if side > 0 else (lambda u, v, f=f: f(-u, v))
        k.decal(p, nrm, half, col, eps=eps, outline=cut_outline(side, cut), rings=20, segs=56)


def brows(k, c, r, d, half, cut, colour, reach=(1.55, 1.18), height=0.85, eps=0.0022):
    """The heavy brows: a patch over each eye whose lower edge runs a little under the eye's cut, so the eye's own
    crisp cut is the line between them; from past the inner corner (where the two nearly meet over the nose) to the
    outer corner. In every eye state: laid before the body's build."""
    a, b = cut
    u0 = (reach[1] - reach[0]) / 2
    v0 = a + b * u0 + height / 2 - 0.1
    for side in (1, -1):
        p, nrm = on_ell(c, r, (d[0] * side, d[1], d[2]))
        right, up = eye_frame(p, nrm)
        q = p + right * side * u0 * half[0] + up * v0 * half[1]
        lo = lambda u: a + b * u - 0.1
        pts = [(-reach[0], lo(-reach[0])), (reach[1], lo(reach[1])), (reach[1], lo(reach[1]) + height),
               (-reach[0], lo(-reach[0]) + height)]
        pts = [(side * (u - u0), v - v0) for u, v in pts]
        k.decal(q, nrm, half, colour, eps=eps, outline=polygon(pts), rings=8, segs=64)


def board(k):
    """The board check on the built body (PokemonStudio.Strip): the span at 31.5 degrees against its square."""
    V = np.array([v.co[:] for v in k.ob.data.vertices])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    sq = 1.3 * max(h, 0.85 * max(w, l))
    print("KIT: board: shows %.3f in a square of %.3f (%.2f); corner sum %.3f" %
          (np.ptp(proj), sq, np.ptp(proj) / sq, 0.853 * w + 0.522 * l))


def norel(src, out):
    k = Kit(src, coat="fur")
    k.add("core", ell(C, R), CLOUD, "body")
    billows(k, "billow", BILLOWS, "body")
    # the face: its own mass forward, scalloped along its top and its bottom
    k.add("face", ell(FC, FR), FACE, "body")
    scallops(k, "scalltop", FC, FR, 0.75, 4, 0.04, FACE, "body", span=0.75)
    scallops(k, "scallow", FC, FR, -0.72, 4, 0.04, CLOUD, "body", span=0.68)
    # tops light, undersides dark (the billows' own colours do most; these keep the face's middle its own grey)
    k.paint(lambda P: inside(P[:, 2] - 0.15, 0.02), CLOUD_LOW, on=["core", "billow", "scallow"])
    brows(k, FC, FR, EYE_D, EYE_HALF, CUT, BROW)
    mouth_frown(k, FC, FR, MOUTH_D, (0.018, 0.01), MOUTH)
    # the static standing its edges on end
    out_r = R + 0.06
    for i, (d, length) in enumerate(SPIKES):
        root, _ = on_ell(C, out_r, d)
        spike_cluster(k, f"spike{i}", root, d, length * 1.1, 0.02, SPIKE, "body")
    # crackles on the lower front, on both sides
    for side in (1, -1):
        b6 = BILLOWS[6 if side > 0 else 5]
        p, n = on_ell(b6[0], (b6[1], b6[1] * 0.95, b6[1] * 0.92), (0.8 * side, -0.45, -0.35))
        bolt(k, "crackle" + ("R" if side > 0 else "L"), p, n, 0.04, SPARK, "body", 0.007, lean=-0.3 * side,
             mirror_x=side < 0)
    k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.04)
    board(k)

    def eyes(state):
        lay_glower(k, FC, FR, EYE_D, EYE_HALF, CUT, FACE, state)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    norel(*args())
