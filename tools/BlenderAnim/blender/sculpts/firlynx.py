# Firlynx sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/firlynx.py -- <the export's Firlynx.glb> <out.glb>
# Carpathia No. 10002, Lynkit's second stage (docs/plans/decisions/CP-dex-1-plateau.md, "2. Firlynx"). Grown from
# lynkit.py: its frame, ears, fir brushes, ruff, nose, mouth and way of making eyes. From its picture
# (docs/art/reference/gemini/carpathia/10002-Firlynx.jpg): a lean young lynx on long legs in a lighter moss green, its
# flanks, back, shoulders and haunches barred like fir bark in brown with darker green between; the cream bib grown
# long down the chest and the cream belly; a fuller, darker green ruff of shaggy points hanging from the cheeks; tall
# ears lined cream-tan, each with a fir sprig; smaller almond eyes of orange amber under a dark lid line; three dark
# green spots on the forehead and brown bars over the brows and from each eye back over the cheek; a cuff of dark moss
# round each ankle over a cream paw; a medium tail held low, its tip dark brown.
# Reference ratios (picture, to the sprigs' tips): head height / total 0.22 (here 0.25: a tenth larger), leg showing
# under the belly / total 0.38 (here 0.30: the trunk deepened against stilts), head width / body width 1.1 (here 1.15),
# ear with its sprig / head height 1.0 (here 1.0).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import sibling, inside, smoothstep, unit, polygon, srgb, args

L = sibling("lynkit")

# Skeleton: body (0, 0.04, 0.38), head (0, -0.208, 0.493), ears (+-0.055, -0.245, 0.607), forelegs (+-0.075, -0.17, 0.3),
# hind legs (+-0.075, 0.2, 0.3), tail (0, 0.265, 0.424).
FIRLYNX = dict(L.LYNKIT,
    name="Firlynx",
    height=0.86,
    voxel=0.0072,
    coat="#A3C788", spot="#3F6E3A", bark="#7B5E45", cream="#F1ECCF", ruff="#5E9448", ruff2="#77A85A",
    fir="#3C6B38", fir2="#4E8044", earin="#D9CCAC", tailtip="#4A4234", pawc="#F1ECCF", cuff="#4E8A3C",
    trunk=[((0, -0.15, 0.4), (0.104, 0.12, 0.152)), ((0, 0.03, 0.405), (0.09, 0.15, 0.118)),
           ((0, 0.2, 0.41), (0.098, 0.11, 0.125))],
    neck=((0, -0.235, 0.5), (0.078, 0.085, 0.1)),
    H=(0, -0.285, 0.6), HR=(0.11, 0.108, 0.105),
    muzzle=((0, -0.085, -0.042), (0.046, 0.036, 0.034)),
    leg_x=0.075, fore_y=-0.17, hind_y=0.2,
    shoulder=((0.006, 0.0, 0.33), (0.064, 0.08, 0.14)),
    thigh=((0.006, 0.014, 0.335), (0.072, 0.098, 0.145)),
    leg=[(0.0, 0.0, 0.31), (0.002, -0.004, 0.17), (0.003, -0.008, 0.05)], leg_r=[0.05, 0.042, 0.039],
    hind=[(0.0, 0.02, 0.31), (0.002, 0.05, 0.17), (0.002, 0.02, 0.085), (0.002, 0.012, 0.05)],
    hind_r=[0.054, 0.044, 0.04, 0.039],
    paw=((0.002, -0.02, 0.026), (0.049, 0.058, 0.03)),
    paw_line=0.05, toes=True,
    tail=[(0, 0.27, 0.44), (0, 0.34, 0.43), (0, 0.41, 0.4), (0, 0.46, 0.37)], tail_r=[0.036, 0.036, 0.037, 0.034],
    tail_out=unit(0.5, 0.87, 0), tail_tip=0.06,
    ear_at=(0.55, 0.12, 0.72), ear_tip=(0.03, 0.01, 0.105), ear_base=0.042, ear_depth=0.32,
    brush=0.1, brush_w=0.013,
    ruff_pts=[(0.4, 0.06), (0.65, 0.075), (0.9, 0.085), (1.15, 0.085), (1.4, 0.075), (1.65, 0.062), (1.9, 0.05)],
    ruff_w=0.03, ruff_ring=(0.92, -0.02, 0.86, -0.03), puff=((0.088, -0.005, -0.055), (0.036, 0.045, 0.06)),
    eye_x=0.045, eye_dz=0.01, eye_half=(0.03, 0.024), eye_tilt=1.25,
    eye_outline=L.almond(top=0.88, bottom=0.8, outer=0.3),
    iris_top="#A3501A", iris_bottom="#E8902E", pupil_r=0.42, rim=0.82, lid_line=True,
    nose_half=(0.012, 0.009), mouth_half=(0.02, 0.012),
    face=((0, -0.075, -0.058), (0.078, 0.07, 0.056)),
    bib=dict(y=-0.21, top=0.56, bottom=0.3, w0=0.045, w1=0.075),
    belly=0.33,
    spots=False,
)


def stripes(k, P, period=0.088, top=0.56, bottom=0.29, w0=0.027, slant=0.55, on=None, seed=4, front=-0.21):
    """Bars of bark down the flanks, the back, the shoulders and the haunches: wavy, wide over the back and
    narrowing to points toward the belly, brown and dark green in turn."""
    on = on or ["chest", "middle", "rump", "foretop", "hindtop", "neck"]
    def phase(Q):
        y = Q[:, 1] + slant * (top - Q[:, 2])          # each bar runs down and forward from the back
        return (y + 0.012 * np.sin(Q[:, 2] * 60 + np.abs(Q[:, 0]) * 40)) / period
    def band(Q, which):
        t = phase(Q)
        d = np.abs(t - np.floor(t) - 0.5) * period
        f = np.clip((Q[:, 2] - bottom) / (top - bottom), 0, 1)
        w = w0 * (0.25 + 0.75 * f) * (0.75 + 0.25 * np.sin(np.floor(t) * 2.3))
        pick = (np.floor(t) % 2 == which)
        return inside(d - w, 0.0025) * pick * inside(bottom - Q[:, 2], 0.004) * inside(front - Q[:, 1], 0.015)
    k.paint(lambda Q: band(Q, 0), P["bark"], on=on)
    k.paint(lambda Q: band(Q, 1), P["spot"], on=on)


def face_marks(k, P):
    """Three dark green spots on the forehead; a brown bar over each brow and one from the eye back over the cheek."""
    H, HR = np.array(P["H"]), np.array(P["HR"])
    eps = P["height"] * 0.004
    for x, dz, hs in ((0.0, 0.06, (0.012, 0.008)), (0.026, 0.07, (0.008, 0.007)), (-0.026, 0.07, (0.008, 0.007))):
        p, n = L.on_head(x, H[2] + dz, H, HR)
        k.decal(p, n, hs, P["spot"], eps=eps, rings=5, segs=22)
    for s in (1, -1):
        for x, dz, ang, hs in ((0.055, 0.058, 35, (0.022, 0.006)), (0.09, -0.005, -20, (0.024, 0.006))):
            p, n = L.on_head(x * s, H[2] + dz, H, HR)
            a = math.radians(ang) * s
            k.decal(p, n, hs, P["bark"], up=unit(-math.sin(a), 0, math.cos(a)), eps=eps, rings=5, segs=24)


def firlynx(src, out_path, P=FIRLYNX, extra=None, decals=None):
    L.lynx(src, out_path, P, extra=extra, decals=decals or face_marks)


FIRLYNX["stripes"] = stripes

if __name__ == "__main__":
    firlynx(*args())
