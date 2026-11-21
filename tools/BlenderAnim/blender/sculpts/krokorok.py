# Krokorok sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/krokorok.py -- <the export's Krokorok.glb> <out.glb>
# Sandile grown upright (built by sandile.py's functions: the line's tan, grey and black, its skull and domes, the
# snout with its band, bulb and nostrils, the mouth's line, the painted mask and the eyes on it): a darker tan
# crocodile standing on two legs, the head carried forward, a black visor across both domes like dark glasses with a
# dark eye and a glint behind each lens, a long snout banded grey at its root, white fangs hanging at the mouth's
# corner and near its tip; a pink throat and belly crossed by two grey bands; thighs fused down to just above the
# ankle with a grey band across each, a grey cuff at the ankle, three white claws a foot; arms carried forward and in
# (the folded arms of the art), grey at the wrist; dark spikes down the back and along a long tail banded in grey.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from types import SimpleNamespace
import numpy as np
from sculptkit import Kit, ell, tube, ring_band, srgb, inside, unit, args, sibling

D = sibling("sandile")
TAN = "#C99E68"
PINK = "#E597A6"
BAND = D.BAND
WHITE = "#F4F2EE"
VOXEL = 0.005

H = SimpleNamespace(**vars(D.H))
H.O = (0.0, -0.04, 0.455)
H.skull_r = (0.08, 0.078, 0.064)
H.dome_c, H.dome_r = (0.036, -0.012, 0.044), (0.038, 0.04, 0.033)
H.saddle_c, H.saddle_r = (0.0, -0.014, 0.04), (0.034, 0.032, 0.028)
H.cheek_c, H.cheek_r = (0.0, 0.025, -0.03), (0.064, 0.05, 0.042)
H.snout_root, H.snout_pitch = (0.0, 0.05, -0.022), 6.0
H.snout_c, H.snout_r = (0.0, 0.085, 0.0), (0.045, 0.115, 0.03)
H.nose_c, H.nose_r = (0.0, 0.19, 0.004), (0.047, 0.036, 0.032)
H.bump_c, H.bump_r = (0.018, 0.2, 0.024), (0.016, 0.015, 0.013)
H.jaw_c, H.jaw_r = (0.0, 0.08, -0.04), (0.041, 0.115, 0.02)
H.mouth_u, H.mouth_rise, H.mouth_corner, H.mouth_curl = -0.027, 0.018, 0.0, 0.035
H.band_f = (0.03, 0.085)
H.nostril, H.nostril_r = (0.017, 0.215, 0.018), 0.0078
H.eye_dir = (0.32, 1.0, 0.12)
H.mask_ab, H.mask_out, H.mask_tilt, H.mask_reach = (0.036, 0.024), 0.45, 0.0, 0.075
H.bridge = (0.04, 0.044, 0.0135, 0.005)
H.eye_h, H.eye_look = (0.026, 0.02), D.eye_open(r=0.7)
H.glint = (-0.15, 0.3, 0.36)


def spike(k, name, base, d, length, width, bone, across=(1, 0, 0)):
    """A dark spike of the back or the tail."""
    k.add(name, D.fang(base, d, length, width, width * 0.55, up=across), BAND, bone, fuse=False, surface="scales")


def krokorok(src, out):
    k = Kit(src, coat="scales")

    # The body: a pear widest at the belly, low hips, a thick neck carrying the head forward
    def pear(V):
        V = V.copy()
        V[:, :2] *= (1 + 0.12 * np.clip(-V[:, 2], 0, 1))[:, None]
        return V
    k.add("torso", ell((0, 0.0, 0.245), (0.098, 0.088, 0.125), deform=pear), TAN, "body")
    k.add("hips", ell((0, 0.012, 0.17), (0.102, 0.088, 0.07)), TAN, "body")
    k.add("chest", ell((0, -0.005, 0.33), (0.083, 0.072, 0.065)), TAN, "body")
    k.add("neck", tube([(0, 0.0, 0.34), (0, -0.02, 0.39), (0, -0.035, 0.42)], [0.07, 0.065, 0.06]), TAN, "head")

    # The pink throat and belly down the front, crossed by two grey bands round the body
    def belly(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        w = 0.068 - 0.025 * np.clip((z - 0.33) / 0.08, 0, 1)
        return inside(np.abs(x) - w, 0.003) * inside(z - 0.43, 0.003) * inside(0.115 - z, 0.003) \
            * inside(y + 0.02, 0.004) * inside(-0.115 - y, 0.004) * inside(np.abs(x) - 0.09, 0.003)
    k.paint(belly, PINK)
    def bands(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        m = np.maximum(inside(np.abs(z - 0.345) - 0.014, 0.0025), inside(np.abs(z - 0.165) - 0.013, 0.0025))
        return m * inside(np.sqrt((x / 0.126) ** 2 + ((y - 0.01) / 0.116) ** 2) - 1, 0.03)
    k.paint(bands, BAND)

    # The legs: great thighs fused into the hips down to just above the ankle, a grey band across each, a short
    # shin with a grey cuff, a broad foot with three white claws
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        b, m = f"leg{side}", np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array((0.078, 0.0, 0.135)) * m, (0.06, 0.066, 0.085)), TAN, b)
        k.add(f"shin{side}", tube(np.array([(0.086, -0.004, 0.085), (0.09, -0.01, 0.04)]) * m, [0.04, 0.037]), TAN, b)
        foot = np.array((0.09, -0.04, 0.018))
        k.add(f"foot{side}", ell(foot * m, (0.043, 0.062, 0.02)), TAN, b)
        k.add(f"cuff{side}", ring_band(np.array((0.089, -0.008, 0.05)) * m, (0, -0.08, 1), 0.04, 0.016), BAND, b,
              fuse=False, surface="scales")
        for j, dx in enumerate((-0.026, 0.0, 0.026)):
            base = foot + np.array((dx, -0.052, 0.004))
            D.claw(k, f"claw{side}{j}", base * m, np.array((dx * 3, -1.0, -0.3)) * m, 0.034, 0.02, b, color=WHITE)
    def thighband(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        return inside(np.abs(z - 0.115 - 0.12 * (np.abs(x) - 0.1)) - 0.012, 0.0025) * inside(0.088 - np.abs(x), 0.003) \
            * inside(y - 0.03, 0.003)
    k.paint(thighband, BAND)

    # The arms: forward and in toward one another (the art's folded arms), a grey band at the wrist
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        b, m = f"arm{side}", np.array([s, 1, 1.0])
        sh, el, wr = np.array((0.072, -0.01, 0.345)), np.array((0.108, -0.07, 0.3)), np.array((0.065, -0.132, 0.288))
        k.add(f"arm{side}", tube(np.array([sh, el, wr]) * m, [0.033, 0.029, 0.027]), TAN, b)
        k.add(f"palm{side}", ell(wr * m + np.array((0, -0.018, 0)), (0.027, 0.026, 0.024)), TAN, b)
        for j, d in enumerate([(-0.5, -1, 0.2), (-0.25, -1, -0.1), (0.1, -1, -0.3)]):
            d = unit(*d)
            p0 = wr + np.array((0, -0.03, 0))
            k.add(f"finger{side}{j}", tube(np.array([p0, p0 + d * 0.022]) * m, [0.012, 0.011]), TAN, b)
        ax = wr - el
        k.add(f"wrist{side}", ring_band((el + ax * 0.68) * m, ax * m, 0.0275, 0.018), BAND, b, fuse=False,
              surface="scales")

    # The tail: long, back and out to +X, banded in grey, a grey point; dark spikes on it and down the back
    pts = [(0, 0.07, 0.2), (0.03, 0.18, 0.13), (0.08, 0.3, 0.09), (0.14, 0.4, 0.085), (0.2, 0.47, 0.11)]
    line = D.banded_tail(k, "tail", pts, [0.062, 0.05, 0.038, 0.026, 0.013], TAN,
                         bands=[(0.3, 0.035), (0.5, 0.035), (0.68, 0.032)], tip_from=0.86)
    seg = np.linalg.norm(np.diff(line, axis=0), axis=1)
    sl = np.concatenate([[0], np.cumsum(seg)]); sl /= sl[-1]
    for j, (t, ln, w) in enumerate(((0.38, 0.05, 0.04), (0.58, 0.045, 0.034), (0.76, 0.035, 0.028))):
        i = int(np.searchsorted(sl, t))
        a = line[min(i + 1, len(line) - 1)] - line[max(i - 1, 0)]
        r = 0.062 + (0.013 - 0.062) * t
        spike(k, f"tailspike{j}", line[i] + np.array((0, 0, r * 0.7)),
              unit(*(a / np.linalg.norm(a) * 0.6 + np.array((0, 0, 1.0)))), ln, w, "tail",
              across=tuple(np.cross(a, (0, 0, 1))))
    for j, (y, z, ln) in enumerate(((0.075, 0.37, 0.05), (0.09, 0.3, 0.05), (0.09, 0.23, 0.045))):
        spike(k, f"backspike{j}", (0.0, y - 0.01, z), (0.0, 0.85, 0.5), ln, 0.045, "body")

    # The head, the snout's band, the mouth's line, the nostrils, the fangs, the visor and the eyes
    F, S = D.head(k, H, TAN)
    D.nostrils(k, H, S)
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        for j, (f, x, ln) in enumerate(((0.02, 0.036, 0.03), (0.175, 0.03, 0.022))):
            base = S.p(x * s, f, H.mouth_u + 0.004)
            k.add(f"fang{side}{j}", D.fang(base, S.d(0.1 * s, 0.1, -1.0), ln, 0.016, 0.012, up=(0, 1, 0)), WHITE,
                  "head", fuse=False, surface="shell")
    D.mask(k, H, F)
    lay = D.eyes(k, H, F)

    k.build(voxel=VOXEL, smooth=4, band=0.006)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    krokorok(*args())
