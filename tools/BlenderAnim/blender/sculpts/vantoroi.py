# Vantoroi (No. 10114, Carpathia; Vântoroi) sculpted from nothing on the skeleton and clips of the game's own export
# of it (sculptkit.py), grown from Vantulet's script (sibling("vantulet")); its glowering brows are Norel's way
# (sibling("norel")'s helpers, which build nothing on import):
#   blender --background --factory-startup --python sculpts/vantoroi.py -- <the export's Vantoroi.glb> <out.glb>
#
# The design, from its picture (10114-Vantoroi.jpg) and its brief (CP-dex-2b, "114. Vantoroi: the howling
# whirlwind"): Vantulet's whirl grown into a tall tornado. Its crown is a wide disc of grey air whose top is wound into
# Vantulet's spiral (its eye high on the front) and whose edge rolls over in a pale lip, flaring like the bell of a
# horn (the Sound type's instrument: the brief names none, so the howling funnel's flared mouth is it). Under the
# crown the funnel, about half its height across at the top, narrows and bends to its right (+X) down to a point; it
# is wound in six broad bands of air of uneven width, each tilted its own way and overlapping the one below like wound
# cloth, their lower edges wavy, pale and grey in turn. Two narrow glowing cyan eyes under heavy brows look out from the
# crown's front. Long streamers of cloud sweep out and back from its sides, two a side and none like another: flat
# ribbons tapering to a point, each ending in Vantulet's curl flattened, an open spiral of a turn and a half, their
# faces to the cameras. Leaves are caught on the bands and the ribbons (the picture's leaves and snow flying free, and
# its pale rings of howl, are left out: seven leaves are fixed to it).
#
# The skeleton: body (0, 0, 0.672), tail (0, 0, 0.542), head (0, 0, 0.922), arms (+-0.17, 0, 0.822).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, srgb, inside, unit, args, sibling, outward

v = sibling("vantulet")
nrl = sibling("norel")

GREY, PALE, DARK = "#B3C1D0", "#E3EBF3", "#8494AA"
MID, ASH = "#CDD8E4", "#B8C6D5"
GROOVE = "#9EAFC3"
BROW = "#6E7E96"
EYE = nrl.glow_eye(core=srgb("#F4FDFF"), mid=srgb("#7FE8FF"), edge=srgb("#2CB4E6"), rim=srgb("#2B3A52"))

CC = np.array([0.0, -0.01, 0.965])       # the crown
CR = np.array([0.255, 0.235, 0.115])
SPIRAL_AXIS = (0.1, -0.42, 0.9)
EYE_D = (0.74, -0.66, 0.06)
EYE_HALF = (0.055, 0.041)
CUT = (0.12, 0.5)
FACE_C = np.array([0.0, -0.15, 0.86])     # the face: its own mass forward under the lip, so the eyes clear it
FACE_R = np.array([0.17, 0.1, 0.085])
TIP = np.array([0.1, 0.0, 0.13])
NECK_Z = 0.85
VOXEL = 0.0078
CAMERA = unit(0.52, -0.85, 0.17)         # the game's front three-quarter camera, as seen from the model

# the bands, top to bottom: middle height, width, tilt (degrees), the way it tilts (radians), colour
BANDS = [(0.76, 0.15, 9, 0.3, PALE), (0.645, 0.17, 21, 3.6, ASH), (0.53, 0.14, 14, 1.6, PALE),
         (0.42, 0.155, 20, 2.6, MID), (0.33, 0.12, 17, 5.0, PALE), (0.245, 0.09, 24, 1.2, ASH)]


def funnel_at(z):
    """The funnel's middle and radius at a height: wide under the crown, bending to +X as it narrows to its point."""
    s = np.clip((z - TIP[2]) / (NECK_Z - TIP[2]), 0, 1)
    x = TIP[0] * (1 - s) ** 1.6
    r = 0.014 + 0.225 * s ** 1.25
    return np.array([x, 0.0, z]), r


def turn(p, c, axis, deg):
    """Points p turned about an axis through c (Rodrigues)."""
    k_ = unit(*axis); a = math.radians(deg)
    q = p - c
    return c + q * math.cos(a) + np.cross(k_, q) * math.sin(a) + np.outer(q @ k_, k_) * (1 - math.cos(a))


def band(z, w, tilt, way, seed):
    """A broad band of air wound round the funnel: a ring its own width, flaring out at its lower edge over the band
    below, its edges wavy, tilted its own way. A closed loose sheet, its seam behind."""
    def at(u, vv):
        th = math.pi / 2 + 2 * math.pi * u
        wave = 1 + 0.2 * np.sin(3 * th + seed) + 0.08 * np.sin(5 * th + 2 * seed)
        top, bot = z + w * 0.5 * (1 + 0.1 * np.sin(2 * th + seed)), z - w * 0.5 * wave
        zz = top + (bot - top) * vv
        c, r = funnel_at(z)
        _, rt = funnel_at(z + w * 0.5)
        rr = rt * 0.98 + (r * 1.0 + 0.03 - rt * 0.98) * vv ** 0.8
        P = np.stack([c[0] + rr * np.cos(th), c[1] + rr * np.sin(th), zz], axis=-1)
        sh = P.shape
        return turn(P.reshape(-1, 3), np.array([c[0], 0, z]), (math.cos(way), math.sin(way), 0), tilt).reshape(sh)
    return outward(sheet(at, 97, 6, 0.02))


def funnel(k):
    zs = np.linspace(TIP[2] + 0.01, NECK_Z + 0.02, 16)
    pts = [funnel_at(z)[0] for z in zs]
    rad = [funnel_at(z)[1] * 0.93 for z in zs]
    lo = [i for i, z in enumerate(zs) if z < 0.56]
    hi = [i for i, z in enumerate(zs) if z >= 0.52]
    k.add("core_low", tube([pts[i] for i in lo], [rad[i] for i in lo]), GROOVE, "tail")
    k.add("core", tube([pts[i] for i in hi], [rad[i] for i in hi]), GROOVE, "body")
    for i, (z, w, tilt, way, col) in enumerate(BANDS):
        k.add(f"band{i}", band(z, w, tilt, way, 1.3 * i + 0.4), col, "body" if z > 0.55 else "tail", fuse=False)


def crown(k):
    k.add("crown", ell(CC, CR), GREY, "head")
    k.add("crownridge", v.spiral_ridge(CC, CR, SPIRAL_AXIS, 0.055, 2.4, 0.0, 0.02), GREY, "head")
    k.paint(v.spiral_mask(CC, SPIRAL_AXIS, 0.055, 0.14, 0.2, 0.025), DARK, on=["crown", "crownridge"])
    # the lip rolling over the rim, pale, flaring like a horn's bell
    lip = [CC + np.array([math.cos(a) * CR[0] * 0.98, math.sin(a) * CR[1] * 0.98, -0.045 + 0.012 * math.sin(2 * a)])
           for a in np.linspace(0, 2 * math.pi, 49)]
    k.add("lip", tube(lip, [0.034] * len(lip), seg=14, per=2), PALE, "head")
    # the face's own front, so the eyes lie on plain ground
    k.add("brow", ell(FACE_C, FACE_R), GREY, "head")


def catmull(pts, per=12):
    p = np.array(pts, float)
    ext = np.vstack([2 * p[0] - p[1], p, 2 * p[-1] - p[-2]])
    out_ = []
    for i in range(len(p) - 1):
        p0, p1, p2, p3 = ext[i:i + 4]
        for t in np.linspace(0, 1, per, endpoint=False):
            out_.append(0.5 * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                               + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    out_.append(p[-1])
    return np.array(out_)


def ribbon(pts, face, w0, w1, curl_r, curl_side=1.0, thick=0.024):
    """A streamer: a flat ribbon along a smooth line, its face turned toward `face`, tapering from w0 to a point of w1,
    and ending in Vantulet's curl flattened, an open spiral of a turn and a half in the ribbon's own plane."""
    line = catmull(pts)
    n = unit(*face)
    t_end = line[-1] - line[-2]; t_end /= np.linalg.norm(t_end)
    nf = n - t_end * (n @ t_end); nf /= np.linalg.norm(nf)
    e2 = np.cross(nf, t_end) * curl_side
    centre = line[-1] + e2 * curl_r
    sp = []
    for ph in np.linspace(0, 3 * math.pi, 40)[1:]:
        r = curl_r * (1 - 0.62 * ph / (3 * math.pi))
        sp.append(centre + r * (math.sin(ph) * t_end - math.cos(ph) * e2))
    C = np.vstack([line, np.array(sp)])
    ref = np.vstack([np.tile(n, (len(line), 1)), np.tile(nf, (len(sp), 1))])
    T = np.gradient(C, axis=0); T /= np.linalg.norm(T, axis=1, keepdims=True)
    Wd = np.cross(ref, T); Wd /= np.linalg.norm(Wd, axis=1, keepdims=True)
    L = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(C, axis=0), axis=1))]); L /= L[-1]
    width = w0 + (w1 - w0) * L ** 0.8
    N = len(C)
    def at(u, vv):
        i = np.clip(np.round(u * (N - 1)).astype(int), 0, N - 1)
        return C[i] + Wd[i] * ((vv - 0.5) * width[i])[..., None]
    return outward(sheet(at, N, 4, thick))


def vantoroi(src, out):
    k = Kit(src, coat="fur")
    funnel(k)
    crown(k)
    nrl.brows(k, FACE_C, FACE_R, EYE_D, EYE_HALF, CUT, BROW, height=0.75)
    # four streamers sweeping out and back, two a side, none like another
    k.add("streamR1", ribbon([(0.14, -0.04, 0.8), (0.3, 0.0, 0.79), (0.45, 0.08, 0.73), (0.53, 0.17, 0.63)],
                             (0.5, -0.85, 0.15), 0.11, 0.014, 0.065, curl_side=1.0), PALE, "armR", fuse=False)
    k.add("streamR2", ribbon([(0.14, -0.02, 0.56), (0.26, 0.04, 0.52), (0.36, 0.12, 0.43)],
                             (0.5, -0.85, 0.1), 0.09, 0.012, 0.045, curl_side=-1.0), MID, "body", fuse=False)
    k.add("streamL1", ribbon([(-0.14, -0.04, 0.77), (-0.3, 0.0, 0.73), (-0.42, 0.07, 0.62), (-0.45, 0.14, 0.5)],
                             (0.3, -0.9, 0.3), 0.105, 0.013, 0.06, curl_side=-1.0), PALE, "armL", fuse=False)
    k.add("streamL2", ribbon([(-0.11, 0.0, 0.44), (-0.22, 0.06, 0.41), (-0.3, 0.13, 0.35), (-0.33, 0.2, 0.3)],
                             (0.3, -0.9, 0.35), 0.085, 0.012, 0.04, curl_side=1.0), ASH, "tail", fuse=False)
    # leaves caught on the bands and the ribbons, their faces to the cameras
    face = (0.52, -0.85, 0.05)
    for i, (stem, tip, bone) in enumerate((
            ((0.36, 0.0, 0.75), (0.46, -0.03, 0.85), "armR"),
            ((0.47, 0.1, 0.71), (0.56, 0.06, 0.8), "armR"),
            ((-0.3, -0.03, 0.72), (-0.4, -0.08, 0.81), "armL"),
            ((-0.22, 0.02, 0.43), (-0.32, -0.04, 0.39), "tail"),
            ((0.14, -0.2, 0.66), (0.24, -0.19, 0.6), "body"),
            ((-0.1, -0.17, 0.46), (-0.2, -0.16, 0.52), "tail"),
            ((0.17, -0.1, 0.3), (0.26, -0.1, 0.24), "tail"))):
        v.leaf(k, f"leaf{i}", stem, tip, face, 0.04, bone, thick=0.018)
    k.build(voxel=VOXEL, smooth=4, band=0.008, wband=0.06)
    v.board(k)

    def eyes(state):
        nrl.lay_glower(k, FACE_C, FACE_R, EYE_D, EYE_HALF, CUT, GREY, state, look=EYE)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    vantoroi(*args())
