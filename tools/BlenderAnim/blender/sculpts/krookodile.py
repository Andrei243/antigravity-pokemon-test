# Krookodile sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/krookodile.py -- <the export's Krookodile.glb> <out.glb>
# Sandile and Krokorok grown into a big red crocodile standing upright (built by sandile.py's functions: the line's
# skull with its brows, the snout with its band, bulb and nostrils, the mouth's line, the painted mask and the eyes
# on it): a great pear of a body with a pale blue-white belly from the chest to the legs; a broad head whose top
# corners rise into two short points, a black bandit's mask across the eyes sweeping up over the fronts of the points,
# small pale eyes with dark pupils in it; a wide jaw flaring under the mask with white teeth hanging at its corners; a
# long thick snout hanging forward and down, a black band round its root, a bulb with nostrils at its end; a black
# wedge on each side of the chest pointing in toward the belly; arms carried forward and down, black round the upper
# arm and the wrist, three white claws a hand; thick thighs fused down to the ankle, black cuffs, white claws; a
# thick tail rising behind out to +X, banded black.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from types import SimpleNamespace
import numpy as np
from sculptkit import Kit, ell, tube, ring_band, srgb, inside, unit, args, sibling

D = sibling("sandile")
RED = "#C8404E"
BELLY = "#DCE3EE"
DARK = "#3A3B42"
WHITE = "#F4F2EE"
VOXEL = 0.0065

H = SimpleNamespace(**vars(D.H))
H.O = (0.0, -0.06, 0.635)
H.skull_r = (0.112, 0.1, 0.082)
H.dome_c, H.dome_r = (0.046, -0.025, 0.036), (0.052, 0.05, 0.04)
H.saddle_c, H.saddle_r = (0.0, -0.02, 0.035), (0.045, 0.04, 0.035)
H.cheek_c, H.cheek_r = (0.0, 0.025, -0.045), (0.105, 0.07, 0.05)
H.snout_root, H.snout_pitch = (0.0, 0.06, -0.04), 30.0
H.snout_c, H.snout_r = (0.0, 0.1, 0.0), (0.058, 0.14, 0.04)
H.nose_c, H.nose_r = (0.0, 0.235, 0.006), (0.064, 0.05, 0.044)
H.bump_c, H.bump_r = (0.024, 0.25, 0.034), (0.022, 0.02, 0.018)
H.jaw_c, H.jaw_r = (0.0, 0.1, -0.05), (0.053, 0.13, 0.025)
H.mouth_u, H.mouth_rise, H.mouth_corner, H.mouth_curl, H.mouth_w = -0.034, 0.02, 0.0, 0.04, 0.0034
H.band_f, H.band_col = (0.03, 0.09), "#1C1C22"
H.nostril, H.nostril_r = (0.024, 0.274, 0.024), 0.011
H.eye_dir = (0.32, 1.0, 0.1)
H.mask_ab, H.mask_out, H.mask_tilt, H.mask_reach = (0.001, 0.001), 0.0, 0.0, 0.01
H.bridge = (0.0, 0.0, 0.0, 9.0)          # the band below is the whole mask
H.eye_h = (0.026, 0.02)
H.eye_look = D.eye_open(eye=srgb("#C9CCD4"), r=0.7, pupil=(-0.08, -0.05, 0.36, 0.5))
H.glint = (-0.35, 0.35, 0.26)
HORN = [(0.064, 0.015, 0.042), (0.1, 0.026, 0.076), (0.126, 0.05, 0.096)]
HORN_R = [0.031, 0.02, 0.008]

# The head a tenth larger than first measured (the game's shots want it so): everything in its frames scaled
HS = 1.12
for key in ("skull_r", "dome_c", "dome_r", "saddle_c", "saddle_r", "cheek_c", "cheek_r", "snout_root", "snout_c",
            "snout_r", "nose_c", "nose_r", "bump_c", "bump_r", "jaw_c", "jaw_r", "nostril", "eye_h", "band_f"):
    setattr(H, key, tuple(np.array(getattr(H, key)) * HS))
for key in ("mouth_u", "mouth_rise", "mouth_curl", "nostril_r"):
    setattr(H, key, getattr(H, key) * HS)
H.O = (0.0, -0.06, 0.645)
HORN = [tuple(np.array(p) * HS) for p in HORN]
HORN_R = [r * HS for r in HORN_R]


def horns_front(P, F):
    """The fronts of the head's two points, black as the mask runs up over them."""
    x, f, u = F.local(P)
    out = np.zeros(len(P))
    for s in (1, -1):
        a, b = np.array(HORN[0]) * (s, 1, 1), np.array(HORN[-1]) * (s, 1, 1)
        q = np.stack([x, f, u], 1) - a
        d = b - a
        t = np.clip(q @ d / (d @ d), 0, 1)
        near = np.linalg.norm(q - np.outer(t, d), axis=1)
        front = f > (a[1] + d[1] * t) - 0.004
        out = np.maximum(out, (near < 0.05) * front * inside(t - 0.85, 0.05) * (u > 0.02))
    return out


def mask_band(P, F):
    """The bandit's mask across the face: a band dipping at the middle, rising and widening outward, and running up
    over the fronts of the head's two points."""
    x, f, u = F.local(P)
    ax = np.abs(x)
    uc = 0.026 * HS + 0.22 * ax
    h = 0.016 * HS + 0.11 * ax
    band = inside(np.abs(u - uc) - h, 0.0025) * inside(ax - 0.112 * HS, 0.004) * (f > -0.04) * (f < 0.085)
    return np.maximum(band, horns_front(P, F))


H.mask_more = mask_band


def krookodile(src, out):
    k = Kit(src, coat="scales")

    # The body: a great pear widest at the belly, wide low hips, a broad chest, a thick neck
    def pear(V):
        V = V.copy()
        V[:, :2] *= (1 + 0.12 * np.clip(-V[:, 2], 0, 1))[:, None]
        return V
    k.add("torso", ell((0, 0.0, 0.34), (0.165, 0.145, 0.19), deform=pear), RED, "body")
    k.add("hips", ell((0, 0.015, 0.24), (0.16, 0.13, 0.1)), RED, "body")
    k.add("chest", ell((0, -0.01, 0.48), (0.14, 0.11, 0.09)), RED, "body")
    k.add("neck", tube([(0, 0.0, 0.49), (0, -0.03, 0.55), (0, -0.05, 0.6)], [0.1, 0.095, 0.09]), RED, "head")

    # The pale belly down the front, and a black wedge on each side of the chest pointing in toward it
    def belly(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        e = np.sqrt((x / 0.128) ** 2 + ((z - 0.32) / 0.2) ** 2)
        return inside(e - 1, 0.02) * inside(y + 0.03, 0.004) * inside(-0.21 - y, 0.004)
    k.paint(belly, BELLY)
    def wedges(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        a, b = np.array((0.15, 0.48)), np.array((0.075, 0.37))
        q = np.stack([np.abs(x), z], 1) - a
        d = b - a
        t = np.clip(q @ d / (d @ d), 0, 1)
        dist = np.linalg.norm(q - np.outer(t, d), axis=1)
        w = 0.022 * (1 - t) + 0.002
        return inside(dist - w, 0.003) * inside(y + 0.02, 0.004) * inside(-0.2 - y, 0.004)
    k.paint(wedges, DARK)

    # The legs: thick thighs fused into the hips down to just above the ankle, a black cuff, a broad foot with three
    # white claws
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        b, m = f"leg{side}", np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array((0.118, 0.0, 0.18)) * m, (0.092, 0.1, 0.12)), RED, b)
        k.add(f"shin{side}", tube(np.array([(0.13, -0.008, 0.11), (0.135, -0.018, 0.05)]) * m, [0.064, 0.06]), RED, b)
        foot = np.array((0.137, -0.06, 0.026))
        k.add(f"foot{side}", ell(foot * m, (0.066, 0.088, 0.028)), RED, b)
        k.add(f"cuff{side}", ring_band(np.array((0.135, -0.016, 0.065)) * m, (0, -0.1, 1), 0.063, 0.036), DARK, b,
              fuse=False, surface="scales")
        for j, dx in enumerate((-0.04, 0.0, 0.04)):
            base = foot + np.array((dx, -0.075, 0.006))
            D.claw(k, f"claw{side}{j}", base * m, np.array((dx * 3, -1.0, -0.3)) * m, 0.05, 0.03, b, color=WHITE)

    # The arms: forward and a little down, black round the upper arm and the wrist, three white claws a hand
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        b, m = f"arm{side}", np.array([s, 1, 1.0])
        sh, el, wr = np.array((0.125, -0.01, 0.47)), np.array((0.205, -0.075, 0.41)), np.array((0.215, -0.17, 0.355))
        k.add(f"arm{side}", tube(np.array([sh, el, wr]) * m, [0.048, 0.041, 0.036]), RED, b)
        k.add(f"palm{side}", ell((wr + np.array((0, -0.025, -0.008))) * m, (0.038, 0.036, 0.032)), RED, b)
        for j, d in enumerate([(0.45, -1, -0.35), (0.0, -1, -0.45), (-0.45, -1, -0.35)]):
            d = unit(*d)
            p0 = wr + np.array((0, -0.045, -0.01))
            tip = p0 + d * 0.03
            k.add(f"finger{side}{j}", tube(np.array([p0, tip]) * m, [0.016, 0.015]), RED, b)
            D.claw(k, f"hclaw{side}{j}", (tip + d * 0.01) * m, d * m, 0.035, 0.022, b, color=WHITE)
        ua, fa = el - sh, wr - el
        k.add(f"upperband{side}", ring_band((sh + ua * 0.5) * m, ua * m, 0.046, 0.05), DARK, b, fuse=False,
              surface="scales")
        k.add(f"wristband{side}", ring_band((el + fa * 0.72) * m, fa * m, 0.037, 0.026), DARK, b, fuse=False,
              surface="scales")

    # The tail: thick, rising behind and out to +X, banded black to its point
    D.banded_tail(k, "tail", [(0, 0.1, 0.25), (0.05, 0.25, 0.18), (0.13, 0.36, 0.23), (0.18, 0.41, 0.37),
                              (0.2, 0.4, 0.52)], [0.095, 0.078, 0.06, 0.04, 0.012], RED,
                  bands=[(0.27, 0.04), (0.44, 0.04), (0.61, 0.038), (0.77, 0.035)], tip_from=0.92, band_col=DARK)

    # The head, its two points, the snout's band, the mouth's line, the nostrils, the teeth, the mask and the eyes
    F, S = D.head(k, H, RED)
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        k.add(f"horn{side}", tube([F.p(*(np.array(p) * (s, 1, 1))) for p in HORN], HORN_R), RED, "head")
    D.nostrils(k, H, S)
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        for j, (f, x, ln) in enumerate(((-0.01, 0.075, 0.034), (0.035, 0.062, 0.03))):
            base = S.p(x * s, f, H.mouth_u + 0.006)
            k.add(f"tooth{side}{j}", D.fang(base, (0.15 * s, -0.25, -1.0), ln, 0.024, 0.016, up=(0, 1, 0)), WHITE,
                  "head", fuse=False, surface="shell")
    D.mask(k, H, F)
    lay = D.eyes(k, H, F)

    k.build(voxel=VOXEL, smooth=4, band=0.006)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    krookodile(*args())
