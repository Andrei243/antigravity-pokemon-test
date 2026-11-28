# Stalaghet (No. 10138, Carpathia) sculpted from nothing on the skeleton and clips of the game's own export of it:
#   blender --background --factory-startup --python sculpts/stalaghet.py -- <the export's Stalaghet.glb> <out.glb>
#
# Grown from ghetel.py: its ice's colours, its dome of a head with the same eyes, frosty glowering brows and small
# open mouth, its hexagonal spikes (now stalagmites, a crown and the arms' icicles).
# From its picture (docs/art/reference/gemini/carpathia/10138-Stalaghet.jpg) and its brief (CP-dex-3-river-and-caves.md,
# 138): the ice cave pillar. Largest first: a pillar of a body rising from a base of dark limestone blocks, a cluster
# of ice stalagmites standing up its front, sides and back to under the chin, its ice in bands of blue and white;
# two long icicle arms hanging from the shoulders nearly to the base, each a cluster of spikes; the round dome of a
# head with a crown of small ice spikes. The cold mist it breathes is a drawn effect, dropped.
# The export has no sculptor (a small biped by rule): body 0.39, arms (+-0.17, 0, 0.481), head 0.536, legs 0.273.
# The base, the trunk and the stalagmites are on body (never the legs), the head and its crown on head, the arms on
# armR and armL: the shoulders stand at 0.64, so the clips swing them about a point a little under the shoulder.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, outward, srgb, inside, smoothstep, unit, sibling, args

gh = sibling("ghetel")

ICE, ICEDOME, ICEDEEP, ICEPALE, FROST = gh.ICE, gh.ICEDOME, gh.ICEDEEP, gh.ICEPALE, gh.FROST
ICEBAND = "#86B6E2"
ROCK, ROCKD, ROCKL = "#4E555E", "#3B4149", "#646C76"

HC = np.array([0.0, -0.012, 0.75])
HR = np.array([0.135, 0.122, 0.13])
VOXEL = 0.0068


def band_mask(P):
    """Layered ice: wavy bands of deeper blue round the trunk and arms."""
    w = P[:, 2] * 21 + np.sin(np.arctan2(P[:, 0], -P[:, 1]) * 3) * 0.8 + np.sin(P[:, 0] * 20) * 0.5
    return smoothstep(0.6, 0.85, np.sin(w)) * inside(P[:, 2] - 0.6) * inside(np.abs(P[:, 0]) - 0.19, 0.02)


def pale_mask(P):
    w = P[:, 2] * 21 + np.sin(np.arctan2(P[:, 0], -P[:, 1]) * 3) * 0.8 + np.sin(P[:, 0] * 20) * 0.5
    return smoothstep(-0.75, -0.92, np.sin(w)) * inside(P[:, 2] - 0.6) * inside(np.abs(P[:, 0]) - 0.19, 0.02)


def rock(c, size, rs, top_tilt=(0.0, 0.0)):
    """A block of limestone: a box with its corners jittered and its top tilted a little."""
    c = np.array(c, float); sx, sy, sz = size
    pts = []
    # a jagged frustum: an outline of seven corners at the foot, the same drawn in and broken at the top, and a
    # ledge part way up, so the block reads as broken rock rather than a box
    for h, inset, jit in ((0.0, 1.0, 0.12), (0.55, 0.93, 0.16), (1.0, 0.72, 0.2)):
        for j in range(7):
            t = 2 * math.pi * j / 7 + rs.uniform(-0.2, 0.2)
            q = np.array([math.cos(t), math.sin(t)])
            q = q / np.abs(q).max()                                     # squarish
            q = q * inset * (1 + rs.uniform(-jit, jit))
            z = h * (sz + top_tilt[0] * q[0] * sx + top_tilt[1] * q[1] * sy) * (1 + rs.uniform(-0.12, 0.1) * h)
            pts.append(c + np.array([q[0] * sx, q[1] * sy, z]))
    p = np.array(pts); p[:, 2] = np.maximum(p[:, 2], 0.0)
    return hull(p)


def banded(k, name, base, d, length, width, cols, bone, sides=6, twist=0.3, depth=0.8):
    """A stalagmite or icicle in layers of ice: Ghetel's hexagonal spike cut across into stacked hulls, a colour each
    (the bands of thousand-year-old ice, crisp where paint on a loose piece can't be)."""
    d = unit(*d)
    a = np.cross(d, (0, 0, 1.0)) if abs(d[2]) < 0.95 else np.cross(d, (1.0, 0, 0))
    a /= np.linalg.norm(a); b = np.cross(d, a)
    base = np.array(base, float)
    n = len(cols)
    cuts = [0.0] + [(i + 1) / (n + 0.5) for i in range(n - 1)] + [1.0]

    def ring(f):
        w = width * (1 - 0.9 * f)
        return [base + d * f * length + w * (math.cos(twist + 2 * math.pi * j / sides) * a
                                             + depth * math.sin(twist + 2 * math.pi * j / sides) * b)
                for j in range(sides)]
    for i, col in enumerate(cols):
        f0, f1 = cuts[i], cuts[i + 1]
        pts = ring(f0) + (ring(f1) if i < n - 1 else [base + d * length])
        k.add(f"{name}_{i}", hull(pts), col, bone, fuse=False, surface="shell")


def stalaghet(src, out_path):
    k = Kit(src, coat="shell")
    rs = np.random.RandomState(138)

    # --- the base of dark limestone ------------------------------------------------------------------------------
    blocks = [((0.0, 0.01, 0.0), (0.25, 0.18, 0.2), ROCK, (0.0, 0.0)),
              ((0.17, -0.07, 0.0), (0.15, 0.12, 0.235), ROCKL, (-0.1, 0.0)),
              ((-0.18, -0.06, 0.0), (0.15, 0.12, 0.15), ROCKD, (0.15, 0.0)),
              ((-0.16, 0.1, 0.0), (0.14, 0.09, 0.21), ROCK, (0.0, 0.1)),
              ((0.16, 0.1, 0.0), (0.15, 0.09, 0.17), ROCKD, (0.0, -0.1)),
              ((0.0, -0.17, 0.0), (0.11, 0.06, 0.1), ROCKD, (0.0, -0.3)),
              ((0.3, 0.0, 0.0), (0.06, 0.1, 0.09), ROCK, (-0.3, 0.0)),
              ((-0.3, 0.02, 0.0), (0.06, 0.1, 0.08), ROCKL, (0.3, 0.0))]
    for i, (c, size, col, tilt) in enumerate(blocks):
        k.add(f"rock{i}", rock(c, size, rs, tilt), col, "body", fuse=False, surface="shell")
    # loose stones at the base's foot, and two rock points standing up at its front corners
    for i, (c, r_) in enumerate((((-0.27, -0.17, 0.0), 0.04), ((0.28, -0.16, 0.0), 0.035), ((0.3, 0.14, 0.0), 0.04),
                                 ((-0.31, 0.1, 0.0), 0.035))):
        pts = np.array(c) + rs.uniform(-1, 1, (9, 3)) * r_ * np.array([1, 1, 0.8])
        pts[:, 2] = np.clip(pts[:, 2] + r_ * 0.6, 0, None)
        k.add(f"stone{i}", hull(pts), ROCKD, "body", fuse=False, surface="shell")
    for i, (c, h) in enumerate((((-0.2, -0.13, 0.08), 0.17), ((0.26, -0.06, 0.12), 0.14))):
        k.add(f"rockpoint{i}", gh.spike(c, (0.1 * (1 if c[0] > 0 else -1), -0.1, 1), h, 0.05, sides=5, twist=0.6),
              ROCKD, "body", fuse=False, surface="shell")

    # --- the pillar of a body, fused, banded --------------------------------------------------------------------
    k.add("trunk", ell((0, 0.01, 0.41), (0.17, 0.14, 0.27)), ICE, "body")
    k.add("shoulders", ell((0, 0.01, 0.59), (0.205, 0.14, 0.085)), ICE, "body")
    # --- the head ------------------------------------------------------------------------------------------------
    k.add("head", ell(HC, HR), ICEDOME, "head")

    # --- the arms: a banded core hanging from each shoulder, a cluster of icicles along it -----------------------
    # The parts above the arm bones' joints (0.48) are on body, the rest on the arm, or the clips would swing the
    # shoulders backwards about a point under them
    for s, side in ((1, "R"), (-1, "L")):
        bone = "arm" + side
        k.add("upperarm" + side, tube([(0.18 * s, -0.005, 0.61), (0.235 * s, -0.035, 0.5)], [0.06, 0.05], seg=20),
              ICE, "body")
        k.add("arm" + side, tube([(0.235 * s, -0.035, 0.5), (0.258 * s, -0.047, 0.42), (0.268 * s, -0.05, 0.39)],
                                 [0.05, 0.042, 0.036], seg=20), ICE, bone)
        X = np.array([s, 1, 1.0])
        for name, base, d, ln, w, cols, b in (
                ("main", (0.262, -0.05, 0.48), (0.12, -0.03, -1), 0.3, 0.065, (ICE, ICEBAND, ICEPALE), bone),
                ("out", (0.29, -0.02, 0.42), (0.35, 0.1, -1), 0.2, 0.042, (ICEPALE, ICE), bone),
                ("in", (0.255, -0.085, 0.38), (-0.06, -0.12, -1), 0.17, 0.038, (ICEDEEP, ICE), bone),
                ("elbow", (0.24, -0.02, 0.52), (1, 0.2, 0.35), 0.11, 0.034, (ICEPALE, FROST), "body"),
                ("back", (0.25, 0.03, 0.47), (0.3, 1, -0.6), 0.11, 0.032, (ICEDEEP, ICE), bone),
                ("upper", (0.2, -0.02, 0.6), (0.6, -0.1, -1), 0.17, 0.05, (ICE, ICEBAND, ICEPALE), "body"),
                ("shoulder", (0.2, 0.0, 0.64), (0.35, 0.05, 1), 0.1, 0.038, (ICEPALE, FROST), "body")):
            banded(k, name + side, np.array(base) * X, np.array(d) * X, ln, w, cols, b)

    # --- the stalagmites: a few fat ones up the front with their tops under the chin, taller ones at the sides
    # rising to the mouth so the head sits in a collar of them, and more up the back ---------------------------------
    mites = [((0.0, -0.13, 0.19), (0, -0.1, 1), 0.41, 0.08, (ICEPALE, ICE, ICEPALE)),
             ((-0.1, -0.12, 0.19), (-0.1, -0.12, 1), 0.36, 0.075, (ICE, ICEBAND, ICE)),
             ((0.105, -0.12, 0.19), (0.1, -0.12, 1), 0.39, 0.075, (ICEPALE, ICE, FROST)),
             ((0.045, -0.17, 0.12), (0.05, -0.15, 1), 0.2, 0.05, (ICE, ICEPALE)),
             ((-0.055, -0.16, 0.13), (-0.05, -0.15, 1), 0.17, 0.045, (ICEBAND, ICE)),
             # the collar round the head: at its sides, rising to the mouth
             ((-0.15, -0.07, 0.34), (-0.12, -0.08, 1), 0.36, 0.07, (ICE, ICEBAND, ICEPALE)),
             ((0.15, -0.07, 0.34), (0.12, -0.08, 1), 0.37, 0.07, (ICEPALE, ICE, ICEPALE)),
             ((-0.085, -0.1, 0.4), (-0.1, -0.15, 1), 0.2, 0.06, (ICEPALE, FROST)),
             ((0.09, -0.1, 0.41), (0.1, -0.15, 1), 0.2, 0.06, (ICE, ICEPALE)),
             ((-0.17, 0.02, 0.36), (-0.2, 0.05, 1), 0.34, 0.065, (ICEBAND, ICE, ICEPALE)),
             ((0.17, 0.02, 0.36), (0.2, 0.05, 1), 0.33, 0.065, (ICE, ICEBAND, ICE)),
             # the back, for the back camera
             ((0.0, 0.12, 0.2), (0, 0.12, 1), 0.48, 0.08, (ICE, ICEBAND, ICEPALE)),
             ((-0.11, 0.1, 0.2), (-0.1, 0.12, 1), 0.42, 0.075, (ICEPALE, ICE, ICEPALE)),
             ((0.11, 0.1, 0.2), (0.1, 0.12, 1), 0.38, 0.075, (ICEBAND, ICE, FROST)),
             ((-0.08, 0.13, 0.42), (-0.12, 0.2, 1), 0.3, 0.06, (ICE, ICEPALE)),
             ((0.08, 0.13, 0.42), (0.12, 0.2, 1), 0.32, 0.06, (ICEPALE, ICE)),
             ((-0.17, 0.05, 0.2), (-0.3, 0.1, 1), 0.22, 0.055, (ICE, ICEPALE)),
             ((0.17, 0.05, 0.2), (0.3, 0.1, 1), 0.22, 0.055, (ICEPALE, ICE))]
    for i, (base, d, ln, w, cols) in enumerate(mites):
        banded(k, f"mite{i}", base, d, ln, w, cols, "body", twist=0.3 + 0.7 * i)

    # --- the crown of small ice spikes round the dome's top -------------------------------------------------------
    for i in range(12):
        a = 2 * math.pi * i / 12
        front = math.cos(a) > 0.6
        e = math.radians(48 if not front else 58)
        q = np.array([math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)])
        base = HC + q * HR * 0.9
        d = q + np.array([0, 0, 0.7])
        ln = (0.055 if front else 0.08) + 0.015 * (i % 2)
        k.add(f"crown{i}", gh.spike(base, d, ln, 0.022, twist=i), ICEPALE if i % 2 else ICE, "head", fuse=False,
              surface="shell")

    # --- colours on the fused body: the layered bands, glints, cheeks, the mouth ----------------------------------
    k.paint(band_mask, ICEBAND)
    k.paint(pale_mask, FROST)
    p, n = gh.on_ell(-0.55, 0.83, HC, HR)
    k.decal(p, n, (0.03, 0.013), FROST, up=unit(0.5, 0, 1), eps=0.0025, rings=8, segs=40)
    for s in (1, -1):
        p, n = gh.on_ell(0.68 * s, 0.725, HC, HR)
        k.decal(p, n, (0.019, 0.011), gh.CHEEK, eps=0.0022, rings=8, segs=36)
    p, n = gh.on_ell(0.0, 0.69, HC, HR)
    gh.mouth(k, p, n, (0.02, 0.019))

    body = k.build(voxel=VOXEL, smooth=4, band=0.008, wband=0.05)
    gh.board(body)
    k.eye_states(gh.lay_face(k, HC, HR, 0.38, 0.758, (0.031, 0.029), ICEDOME))
    k.export(out_path)


if __name__ == "__main__":
    stalaghet(*args())
