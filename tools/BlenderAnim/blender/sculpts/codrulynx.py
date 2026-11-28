# Codrulynx sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/codrulynx.py -- <the export's Codrulynx.glb> <out.glb>
# Carpathia No. 10003, Lynkit's final stage (docs/plans/decisions/CP-dex-1-plateau.md, "3. Codrulynx"). Grown from
# lynkit.py and firlynx.py: the same frame, ears, fir brushes, ruff, nose, mouth, eyes and bars of bark. From its
# picture (docs/art/reference/gemini/carpathia/10003-Codrulynx.jpg): a great heavy lynx carried low, its head forward,
# in deep forest green barred near-black over the flanks, the haunches and the legs; a mane of dark fir boughs laid
# back over its shoulders, its neck and its back; a beard of pale lichen hanging from the chin and the jowls and a
# shaggy lichen ruff round the cheeks; tall ears lined grey with tall dark fir tops; gold eyes glowing under a heavy
# dark brow, slit pupils; a grey-green muzzle and underside; cuffs of moss round the ankles over broad paws with pale
# claws; a tail rising behind, tipped with a dark spray of fir.
# Reference ratios (picture, to the fir tops): head height / total 0.23 (here 0.25: a tenth larger), leg showing under
# the belly / total 0.38 (here 0.30: the trunk deepened against stilts), head width / body width 0.95 (here 0.9), the
# mane's span over the shoulders / body length 0.45 (here 0.45), the beard / head height 0.45 (here 0.45).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import sibling, ell, tube, slab, outward, inside, unit, lumpy, tri, args

L = sibling("lynkit")
F = sibling("firlynx")

# Skeleton: body (0, 0.05, 0.54), head (0, -0.28, 0.692), ears (+-0.082, -0.332, 0.848), forelegs (+-0.12, -0.24, 0.42),
# hind legs (+-0.12, 0.3, 0.42), tail (0, 0.365, 0.608).
CODRULYNX = dict(F.FIRLYNX,
    name="Codrulynx",
    height=1.17,
    voxel=0.0108,
    coat="#426446", spot="#24382A", bark="#18221C", cream="#8F9680", ruff="#94AE80", ruff2="#AEC496",
    fir="#1F3D29", fir2="#2C5233", earin="#7A7A68", tailtip="#1C2A20", pawc="#426446", cuff="#3E5E2C",
    claws="#D2CCB4", nose="#2A2622", ink="#141412", mane="#22422C", mane2="#2E5836", lichen="#A9C290",
    lichen2="#8FAE78",
    # A heavy trunk: the chest a third deeper and wider than a lean cat's, its belly some 0.3 off the ground
    trunk=[((0, -0.2, 0.6), (0.228, 0.2, 0.3)), ((0, 0.06, 0.59), (0.205, 0.22, 0.255)),
           ((0, 0.3, 0.6), (0.21, 0.17, 0.25))],
    neck=((0, -0.33, 0.72), (0.16, 0.15, 0.17)),       # short and thick, lost in the mane
    H=(0, -0.415, 0.8), HR=(0.165, 0.152, 0.156),
    muzzle=((0, -0.112, -0.058), (0.066, 0.05, 0.048)),
    leg_x=0.12, fore_y=-0.24, hind_y=0.3,
    shoulder=((0.04, 0.0, 0.46), (0.135, 0.155, 0.22)),
    thigh=((0.04, 0.02, 0.47), (0.142, 0.17, 0.23)),
    leg=[(0.028, 0.0, 0.42), (0.03, -0.008, 0.24), (0.03, -0.014, 0.08)], leg_r=[0.104, 0.09, 0.082],
    hind=[(0.028, 0.03, 0.42), (0.03, 0.07, 0.25), (0.03, 0.03, 0.13), (0.03, 0.02, 0.08)],
    hind_r=[0.112, 0.092, 0.085, 0.082],
    paw=((0.03, -0.035, 0.048), (0.1, 0.115, 0.052)),
    paw_line=0.0, toes=True,
    tail=[(0, 0.42, 0.66), (0, 0.5, 0.7), (0, 0.565, 0.76), (0, 0.6, 0.82)], tail_r=[0.052, 0.05, 0.05, 0.046],
    tail_out=unit(0.5, 0.87, 0), tail_tip=0.06,
    ear_at=(0.55, 0.16, 0.72), ear_tip=(0.04, 0.02, 0.13), ear_base=0.056, ear_depth=0.32,
    brush=0.15, brush_w=0.02,
    ruff_pts=[(0.4, 0.08), (0.65, 0.1), (0.9, 0.11), (1.15, 0.108), (1.4, 0.095), (1.65, 0.08), (1.9, 0.064)],
    ruff_w=0.04, ruff_ring=(0.92, -0.02, 0.86, -0.035), puff=((0.12, -0.005, -0.07), (0.05, 0.06, 0.075)),
    eye_x=0.07, eye_dz=0.015, eye_half=(0.043, 0.03), eye_tilt=1.25,
    eye_outline=L.almond(top=0.8, bottom=0.72, outer=0.5),
    iris_top="#E2B02A", iris_bottom="#F8E260", pupil="#221A0C", slit=True, rim=0.8, lid_line=True, eye_ink="#0E100E",
    eye_lid="#2E4632", shut_ink="#C8B060",
    nose_half=(0.017, 0.012), mouth_half=(0.028, 0.016),
    face=((0, -0.1, -0.075), (0.075, 0.075, 0.06)),
    bib=dict(y=-0.36, top=0.74, bottom=0.36, w0=0.07, w1=0.12),
    belly=0.36,
    spots=False,
)


def stripes(k, P):
    F.stripes(k, P, period=0.1, top=0.88, bottom=0.36, w0=0.036, slant=0.5, front=-0.3)
    # and bars round the legs
    def legbars(Q):
        t = Q[:, 2] / 0.085
        d = np.abs(t - np.floor(t) - 0.5) * 0.085
        return inside(d - 0.013, 0.0025) * inside(Q[:, 2] - 0.4, 0.01) * inside(0.14 - Q[:, 2], 0.01) * (np.abs(Q[:, 0]) > 0.16)
    k.paint(legbars, P["bark"], on=["foreleg", "hindleg"])


def bough_outline(length, width, teeth=6):
    """A fir bough's outline: a midrib with needles in points along both edges, narrowing to the tip."""
    top, bottom = [], []
    n = teeth * 2
    for i in range(n + 1):
        a = length * i / n
        w = width * (1 - 0.75 * i / n) * (1.0 if i % 2 else 0.6)
        top.append((a, w)); bottom.append((a, -w))
    return [(0.0, 0.0)] + top[1:] + [(length * 1.06, 0.0)] + bottom[1:][::-1]


def add_bough(k, name, root, n, toward, length, width, thick, color, bone):
    n = unit(*n)
    ex = np.array(toward, float); ex = ex - n * (ex @ n); ex = unit(*ex)
    ex = unit(*(ex + n * 0.12))                      # lifted a little off the body, so its face shows
    ez = np.cross(n, ex)
    k.add(name, slab(bough_outline(length, width), np.array(root) + n * thick * 0.3 - ex * length * 0.1, ex, ez, thick),
          color, bone, fuse=False, surface="leaf")


# The mane is the design's largest mass: it covers the neck, the shoulders and the back to the middle of the trunk,
# stands above the head's top between the ears and hangs down the chest's sides and the flanks to the elbows; from the
# front it is twice the head's width at the shoulders. A broad fused shaggy mass first, the boughs laid over it.
MANE_LUMPS = [  # (name, centre, radii, mirrored)
    ("manetop", (0, -0.24, 0.9), (0.2, 0.16, 0.12), False),
    ("maneneck", (0, -0.3, 0.76), (0.23, 0.17, 0.21), False),
    ("manesh", (0.14, -0.17, 0.72), (0.15, 0.23, 0.22), True),
    ("maneback", (0, 0.0, 0.8), (0.24, 0.23, 0.12), False),
    ("manechest", (0.14, -0.33, 0.56), (0.1, 0.11, 0.16), True),
]
MANE_C, MANE_R = np.array([0, -0.16, 0.74]), np.array([0.255, 0.34, 0.27])     # the envelope the boughs root on


def add_mane(k, P):
    for name, c, r, mirror in MANE_LUMPS:
        k.add(name, ell(c, r, deform=lumpy(5, 4, 0.1)), P["mane"], "body", mirror=mirror)
    rng = np.random.RandomState(9)
    i = 0
    for y0, angs, ln in ((-0.85, (0.95, 1.3, 1.65), 0.24), (-0.6, (0.0, 0.4, 0.8, 1.15, 1.5, 1.8), 0.3),
                         (-0.3, (0.2, 0.6, 1.0, 1.35, 1.7), 0.3), (0.0, (0.0, 0.4, 0.8, 1.2, 1.55), 0.29),
                         (0.3, (0.2, 0.6, 1.0, 1.4, 1.75), 0.26), (0.55, (0.0, 0.45, 0.9, 1.3), 0.22)):
        for ang in angs:
            for s in ((1, -1) if ang > 0.02 else (1,)):
                a = ang + 0.08 * (rng.rand() - 0.5)
                d = (s * math.sin(a), y0, math.cos(a))
                p, nrm = L.on_ell(MANE_C, MANE_R * 0.97, d)
                toward = (-s * math.sin(a) * 0.3, 0.9, -math.sin(a) * 2.0 - 0.15)
                add_bough(k, f"bough{i}", p, nrm, toward, ln * (0.88 + 0.24 * rng.rand()), 0.075, 0.034,
                          P["mane"] if i % 2 else P["mane2"], "body")
                i += 1
    # three boughs at the back of the head, laid back into the mane
    H, HR = np.array(P["H"]), np.array(P["HR"])
    for j, (x, z) in enumerate(((0.0, 0.55), (0.06, 0.4), (-0.06, 0.4))):
        p, nrm = L.on_ell(H, HR * 0.98, (x / 0.1, 0.75, z))
        add_bough(k, f"headbough{j}", p, nrm, (x, 1.0, -0.2), 0.16, 0.05, 0.03, P["mane2"] if j else P["mane"], "head")


def beard_shape(V):
    """A wedge hanging from the chin: narrowing downward, its lower edge in ragged points."""
    V = V.copy()
    t = np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.45 * t
    V[:, 1] *= 1 - 0.35 * t
    th = np.arctan2(V[:, 0], -V[:, 1])
    V[:, 2] -= 0.55 * t ** 2 * (0.5 + 0.5 * np.abs(tri(th * 1.1)))
    return V


def add_beard(k, P):
    """Hanging lichen at the chin and the jowls: one fused wedge, pale green-grey, its lower edge ragged."""
    H = np.array(P["H"])
    o, r = P["muzzle"]
    mc = H + np.array(o)
    c = mc + np.array([0, r[1] * 0.45, -r[2] - 0.045])
    k.add("beard", ell(c, (0.1, 0.06, 0.085), deform=beard_shape), P["lichen"], "head", surface="leaf")
    k.paint(lambda Q: inside(Q[:, 2] - (c[2] - 0.06), 0.01) * inside(np.abs(Q[:, 0]) - 0.13, 0.01), P["lichen2"],
            on=["beard"])


def add_shag(k, P):
    """Shaggy points of the coat hanging at the elbows and the hocks."""
    x = P["leg_x"] + 0.03
    for nm, bone, y, z, way in (("elbow", "leg{s}", P["fore_y"] + 0.07, 0.36, (0.15, 0.6, -1.0)),
                                ("hock", "hind{s}", P["hind_y"] + 0.11, 0.2, (0.15, 0.7, -0.9))):
        for j, (dx, dz, ln) in enumerate(((0.05, 0.0, 0.1), (0.0, -0.03, 0.12), (-0.05, 0.0, 0.09))):
            root = np.array([x + dx, y, z + dz])
            k.add(f"{nm}{j}", L.spike(root, way, ln, 0.04, 0.03, across=(1, 0, 0)), P["coat"], bone, fuse=False,
                  mirror=True, surface="fur")


def add_tail_spray(k, P):
    """The tail's tip: a dark spray of fir, solid points out of its end."""
    pts = L.tail_pts(P)
    d = unit(*(pts[-1] - pts[-2]))
    end = pts[-1] + d * P["tail_r"][-1] * 0.5
    a, b = L.axes(d)
    for i in range(7):
        ang = 2 * math.pi * i / 7
        out = unit(*(d + (a * math.cos(ang) + b * math.sin(ang)) * (0.0 if i == 0 else 0.75)))
        k.add(f"tailfir{i}", L.spike(end - d * 0.02, out, 0.1 if i == 0 else 0.075, 0.022, 0.016),
              P["fir"] if i % 2 else P["fir2"], "tail", fuse=False, surface="leaf")


def brows(k, P):
    """A heavy dark brow over each eye and the forehead's dark bars."""
    H, HR = np.array(P["H"]), np.array(P["HR"])
    eps = P["height"] * 0.004
    for s in (1, -1):
        for x, dz, ang, hs in ((0.068, 0.055, -12, (0.04, 0.009)), (0.03, 0.09, 70, (0.03, 0.008)),
                               (0.11, 0.0, -25, (0.03, 0.008))):
            p, n = L.on_head(x * s, H[2] + dz, H, HR)
            a = math.radians(ang) * s
            k.decal(p, n, hs, P["bark"], up=unit(-math.sin(a), 0, math.cos(a)), eps=eps, rings=5, segs=24)
    for x, dz, hs in ((0.0, 0.085, (0.012, 0.024)),):
        p, n = L.on_head(x, H[2] + dz, H, HR)
        k.decal(p, n, hs, P["bark"], eps=eps, rings=5, segs=24)


def extra(k, P):
    add_mane(k, P)
    add_beard(k, P)
    add_shag(k, P)
    add_tail_spray(k, P)


CODRULYNX["stripes"] = stripes

if __name__ == "__main__":
    src, out_path = args()
    L.lynx(src, out_path, CODRULYNX, extra=extra, decals=brows)
