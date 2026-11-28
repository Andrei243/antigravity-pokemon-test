# Namolel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/namolel.py -- <the export's Namolel.glb> <out.glb>
#
# The design, from the project's picture (docs/art/reference/gemini/carpathia/10145-Namolel.jpg) and its brief
# (CP-dex-3-river-and-caves.md, 145): a small round blob of grey clay mud slumped into its own puddle, the puddle's rim
# in soft round lobes; a big glassy bubble swelling on its head, about to pop, pale grey-green with a gloss; a dry
# crust cracked into brown-grey plates over its +X side from the crown down to the puddle, a strip of it round the back
# and a few flakes on the -X edge; two small round black eyes with a glint, wide apart, low on its front; a small wavy
# mouth under them; a few pale wet gleams on the mud.
# Reference ratios (picture): the bubble 0.49 of the width and 0.39 of the height (built 0.46, 0.51), the width 1.17 of
# the height (built 1.26: the bubble's top stands where the export's wisps did), the eyes 0.22 of the width apart
# (built 0.22), 0.05 of the width across (built 0.077, bold).
# The brief's wisps of gas are left out: drawn effects in the picture, and a wisp built as a tube reads as a handle.
# The export's joints: body (0, 0, 0.14), bubble (0, 0, 0.25).
#
# Namolcan grows from this script (sibling("namolel")): its colours, its crust plates, its bubbles, its eyes.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, eye_look, outward, args

MUD, MUD2, CRUST, CRUST2, CRACK = "#7E7D72", "#8E8D82", "#6E6556", "#7C7262", "#3E3A33"
BUBBLE, GLOSS, WET, SALT = "#A4AD9C", "#E4EADF", "#5E5F56", "#E2DA9C"
INK, GLINT, MOUTH = "#141210", "#FFFFFF", "#3A3832"


# --- the line's helpers ------------------------------------------------------------------------------------------------
def hit(shape, o, d):
    V, F = shape
    h = BVHTree.FromPolygons(V.tolist(), F).ray_cast(Vector(o), Vector(unit(*d)))
    return None if h[0] is None else np.array(h[0])


def board(ob):
    V = np.array([v.co[:] for v in ob.data.vertices])
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    sq = 1.3 * max(h, 0.85 * max(w, l))
    s = 0.853 * w + 0.522 * l
    print(f"BOARD: height {h:.3f} width {w:.3f} length {l:.3f}; corners {s:.3f} of square {sq:.3f} = {s / sq:.2f}")


def view_span(ob, turn=31.5):
    """The true width the board sees: the model turned `turn` degrees, its span across the view, against the square."""
    V = np.array([v.co[:] for v in ob.data.vertices])
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    sq = 1.3 * max(h, 0.85 * max(w, l))
    a = math.radians(turn)
    s = np.ptp(V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a))
    print(f"SPAN: across the view at {turn} degrees {s:.3f} of square {sq:.3f} = {s / sq:.2f}")


def on_ell(c, r, d):
    d = np.asarray(d, float)
    return np.asarray(c, float) + d / np.linalg.norm(d / np.asarray(r, float))


def normal_on(c, r, d):
    p = on_ell(c, r, d) - c
    return unit(*(p / (np.asarray(r, float) ** 2)))


def cracked(k, seeds, region, colours, crack, width, on=None, salt=None, salt_every=4):
    """
    Dry ground cracked into plates, as paint over the fused surface inside region(P): the plates are the cells of the
    nearest seed (alternate colours), the cracks the points nearly as near another seed as their own (width), every
    salt_every-th crack filled with salt instead of dark.
    """
    def near(P):
        D = np.linalg.norm(P[:, None, :] - seeds[None], axis=2)
        o = np.argsort(D, axis=1)[:, :2]
        d1 = np.take_along_axis(D, o[:, :1], 1)[:, 0]; d2 = np.take_along_axis(D, o[:, 1:], 1)[:, 0]
        # the true distance to the plane halfway between the two seeds, so a crack keeps its width wherever the
        # surface stands off the seeds
        gap = np.linalg.norm(seeds[o[:, 1]] - seeds[o[:, 0]], axis=1)
        return o[:, 0], o[:, 1], (d2 ** 2 - d1 ** 2) / (2 * np.maximum(gap, 1e-6))
    k.paint(lambda P: region(P), colours[0], on=on)
    k.paint(lambda P: region(P) * (near(P)[0] % 2 == 1), colours[1], on=on)
    if salt is None:
        k.paint(lambda P: region(P) * inside(near(P)[2] - width, width * 0.3), crack, on=on)
        return
    def lines(P, want):
        a, b, g = near(P)
        is_salt = ((a + b) % salt_every == 0)
        return region(P) * inside(g - width, width * 0.3) * (is_salt == want)
    k.paint(lambda P: lines(P, False), crack, on=on)
    k.paint(lambda P: lines(P, True), salt, on=on)


def bubble(k, name, c, r, bone, gloss_dir=(0.45, -0.6, 0.65)):
    """A glassy bubble: a loose ball, a white gloss of its own on its upper front and a smaller one low behind it."""
    c, r = np.asarray(c, float), np.asarray(r, float)
    k.add(name, ell(c, r, sub=4), BUBBLE, bone, fuse=False, surface="plastic")
    g = unit(*gloss_dir)
    p, n = on_ell(c, r, g), normal_on(c, r, g)
    side = unit(*np.cross(n, (0, 0, 1.0))); up = np.cross(side, n)
    V, F = ell((0, 0, 0), (r[0] * 0.34, r[0] * 0.2, r[0] * 0.05), rot=(0, 0, 0), sub=3)
    V = V @ np.stack([side, up, n], axis=0) + p - n * r[0] * 0.02
    k.add(name + "gloss", outward((V, F)), GLOSS, bone, fuse=False, surface="plastic")
    g2 = unit(-g[0] * 0.6, g[1], -0.2)
    p2, n2 = on_ell(c, r, g2), normal_on(c, r, g2)
    side = unit(*np.cross(n2, (0, 0, 1.0))); up = np.cross(side, n2)
    V, F = ell((0, 0, 0), (r[0] * 0.16, r[0] * 0.09, r[0] * 0.04), sub=3)
    V = V @ np.stack([side, up, n2], axis=0) + p2 - n2 * r[0] * 0.015
    k.add(name + "gloss2", outward((V, F)), GLOSS, bone, fuse=False, surface="plastic")


def mud_eye(iris=None):
    """The line's eye: round and black, a white glint high outward, a small one low inward; an iris ring if asked."""
    K, G = srgb(INK), srgb(GLINT)
    I = srgb(iris) if iris else None

    def look(u, v):
        c = np.tile(K, (len(u), 1))
        if I is not None:
            c[(u * u + v * v > 0.42 ** 2) & (u * u + v * v < 0.82 ** 2)] = I
        c[((u - 0.28) / 0.28) ** 2 + ((v - 0.32) / 0.28) ** 2 < 1] = G
        c[((u + 0.3) / 0.12) ** 2 + ((v + 0.34) / 0.12) ** 2 < 1] = G
        return c
    return look


# --- Namolel ----------------------------------------------------------------------------------------------------------
BC = np.array([0.0, 0.0, 0.128])               # the blob's middle
BR = np.array([0.19, 0.172, 0.13])


def namolel(src, out):
    k = Kit(src, coat="scales")
    rs = np.random.RandomState(145)

    # --- the blob slumped into its puddle: a flat-bottomed dome, a low puddle with round lobes at its rim -------------
    def slump(V):
        V = V.copy()
        V[:, 2] = np.maximum(V[:, 2], -0.8)
        s = 1 + 0.08 * np.clip(-V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("blob", ell(BC, BR, deform=slump), MUD, "body")
    k.add("crown", ell((-0.01, 0.0, 0.2), (0.13, 0.12, 0.07)), MUD, "body")

    def rim(V):
        V = V.copy()
        th = np.arctan2(V[:, 1], V[:, 0])
        s = 1 + 0.07 * np.sin(7 * th + 0.6) * smoothstep(0.3, -0.6, V[:, 2])
        V[:, 0] *= s; V[:, 1] *= s
        V[:, 2] = np.maximum(V[:, 2], -0.6)
        return V
    k.add("puddle", ell((0, -0.005, 0.026), (0.2, 0.185, 0.034), deform=rim), MUD, "body")
    for j, (x, y, r) in enumerate(((-0.16, -0.08, 0.03), (0.104, -0.136, 0.028), (-0.064, -0.152, 0.026),
                                   (0.176, 0.016, 0.028), (-0.16, 0.08, 0.028), (0.04, 0.16, 0.028))):
        k.add(f"lobe{j}", ell((x, y, 0.02), (r * 1.2, r, r * 0.8)), MUD, "body")
    # --- the crust: a thicker shell over the +X side, round the back, cracked into plates ------------------------------
    k.add("crustbody", ell(BC + (0.026, 0.008, 0.004), BR * (0.97, 0.98, 1.0), deform=slump), CRUST, "body")
    k.add("crustback", ell(BC + (0.0, 0.03, 0.0), BR * (0.95, 0.95, 0.96), deform=slump), MUD, "body")

    def region(P):
        """The crust's ground: the +X side from front to back, ragged at its edge, and a band round the back."""
        d = P - BC
        az = np.arctan2(d[:, 1], d[:, 0])
        edge = 1.0 + 0.12 * np.sin(P[:, 2] * 60) + 0.06 * np.sin(P[:, 2] * 140 + 1)
        side = inside(np.abs(az + 0.1) - edge, 0.004)
        back = inside(np.abs(az - 1.7) - 0.75, 0.004) * inside(P[:, 2] - (0.2 + 0.025 * np.sin(az * 7)), 0.004)
        return np.clip(side + back, 0, 1) * inside(0.05 - P[:, 2], 0.004) * inside(np.linalg.norm(P[:, :2], axis=1) - 0.215, 0.004)
    seeds = []
    for i in range(44):
        az = -1.1 + 3.6 * rs.rand(); el = -0.15 + 1.5 * rs.rand()
        seeds.append(on_ell(BC + (0.026, 0.008, 0.004), BR * (0.97, 0.98, 1.0), (math.cos(el) * math.cos(az), math.cos(el) * math.sin(az), math.sin(el))))
    cracked(k, np.array(seeds), region, (CRUST, CRUST2), CRACK, 0.003, on=["blob", "crown", "crust"])
    # a few loose flakes broken off on the puddle at the crust's foot
    for j, (x, y) in enumerate(((0.165, -0.095), (0.12, 0.14), (0.185, 0.045))):
        p = np.array([x, y, 0.045])
        pts = p + rs.normal(size=(10, 3)) * np.array([0.016, 0.014, 0.006])
        k.add(f"flake{j}", hull(pts), CRUST, "body", fuse=False, surface="shell")

    # --- the bubble swelling on its head, a little toward -X as the picture has it -------------------------------------
    bubble(k, "bubble", (-0.02, 0.005, 0.292), (0.113, 0.108, 0.1), "bubble")

    # --- wet gleams on the mud, the wavy mouth ----------------------------------------------------------------------
    for j, (d, half) in enumerate((((-0.55, -0.7, 0.45), (0.022, 0.012)), ((0.15, -0.9, 0.35), (0.014, 0.009)),
                                   ((-0.85, -0.35, 0.2), (0.016, 0.01)), ((0.05, -0.75, 0.62), (0.012, 0.008)))):
        dd = unit(*d)
        k.decal(on_ell(BC, BR, dd), dd, half, MUD2, eps=0.002, rings=6, segs=28)
    cm, ck = srgb(MUD), srgb(MOUTH)

    def mouth(u, v):
        c = np.tile(cm, (len(u), 1))
        c[(np.abs(v - 0.3 * np.sin(u * 4.6)) < 0.38) & (np.abs(u) < 0.86)] = ck
        return c
    md = unit(-0.05, -1, -0.12)
    mc = hit(ell(BC, BR, deform=slump), BC + md * 0.4 + (0, 0, -0.042), -md)
    k.decal(mc, md, (0.03, 0.0095), mouth, eps=0.0022, rings=8, segs=48)

    k.build(voxel=0.0042, smooth=4, band=0.004, wband=0.04)
    board(k.ob)
    view_span(k.ob)

    # --- the eyes: small, round and black, wide apart, low on the front ----------------------------------------------
    de = unit(0.36, -0.9, 0.12)
    ec = hit(ell(BC, BR, deform=slump), BC + (0.054, -0.4, 0.008), (0, 1, 0))
    look = mud_eye()
    k.eye_states(lambda st: k.decal(ec, de, (0.019, 0.021), eye_look(st, look, srgb(MUD), srgb(INK), line=0.17),
                                    eps=0.0018, rings=20, segs=56, mirror=True))
    k.export(out)


if __name__ == "__main__":
    namolel(*args())
