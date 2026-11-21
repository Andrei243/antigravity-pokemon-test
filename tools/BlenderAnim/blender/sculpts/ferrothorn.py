# Ferrothorn sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ferrothorn.py -- <the export's Ferrothorn.glb> <out.glb>
#
# Ferroseed grown up (ferroseed.py's spike, chips and eyes in their green patches). From the references: a wide,
# low lens of pale steel, two dark bands round it below its middle and small black chips over it, steel-grey spikes in
# two rows round its rim and a few on its top. High on its front a dark grey visor runs between the eyes and dips to a
# point under them; the eyes are Ferroseed's (yellow, an upright black pupil, a flat top edge) in green patches, with a
# green point rising over the inner corner of each. From the top's middle three thick green vines run out low and
# curve up, each to a green pod: a squat ball with a few steel spikes over it and green points hanging under it.
# Skeleton: body (0, 0, 0.12), head (0, 0, 0.22), vine0, vine1, vine2 (0, 0, 0.24), each a vine.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, args, sibling

fs = sibling("ferroseed")
STEEL, BAND, CHIP, VISOR = "#B2B5BB", fs.BAND, "#26272B", "#5E6168"
SPIKE, GREEN, VINE, POD = "#C8CBD0", fs.GREEN, "#3E9E64", "#44A86C"

C = np.array([0.0, 0.0, 0.135])          # the lens's middle
RXY, RZ = 0.165, 0.09                    # its radii
VOXEL = 0.004
EYE_AZ, EYE_EL = 17.0, 36.0


def surf(az, el):
    a, e = math.radians(az), math.radians(el)
    d = np.array([math.sin(a) * math.cos(e) * RXY, -math.cos(a) * math.cos(e) * RXY, math.sin(e) * RZ])
    n = np.array([math.sin(a) * math.cos(e) / RXY, -math.cos(a) * math.cos(e) / RXY, math.sin(e) / RZ])
    return C + d, n / np.linalg.norm(n)


def angles(P):
    D = (P - C) / np.array([RXY, RXY, RZ])
    az = np.degrees(np.arctan2(D[:, 0], -D[:, 1]))
    el = np.degrees(np.arcsin(np.clip(D[:, 2] / np.maximum(np.linalg.norm(D, axis=1), 1e-9), -1, 1)))
    return az, el


SPIKES = [(az, 12) for az in range(0, 360, 36)] + [(az + 18, -34) for az in range(0, 360, 45)] \
    + [(100, 58), (-100, 58), (180, 60), (140, 40), (-140, 40), (60, 50), (-60, 50)]
SPIKES = [(az, el) for az, el in SPIKES if not (abs((az + 180) % 360 - 180) < 30 and el > 0)]  # leave the face clear
BANDS = (-0.08, -0.5)                    # in shares of RZ above C
BAND_W = 0.0085
CHIPS = [(az, el) for el, n, off in ((22, 14, 0), (-14, 14, 12), (-60, 9, 20), (52, 8, 10)) for az in
         np.linspace(-180, 180, n, endpoint=False) + off]
CHIPS = [(az, el) for az, el in CHIPS if not (abs(az) < 45 and el > 10)]
VINES = [  # bone, azimuth, the pod's distance out and its height
    ("vine0", -68, 0.2, 0.33),
    ("vine1", 95, 0.205, 0.37),
    ("vine2", 188, 0.19, 0.35),
]


def vine(k, bone, az, reach, height):
    a = math.radians(az)
    d = np.array([math.sin(a), -math.cos(a), 0.0])
    top = C[2] + RZ
    pts = [np.array([0, 0, top - 0.01]), d * 0.06 + (0, 0, top + 0.03), d * 0.13 + (0, 0, top + 0.045),
           d * reach * 0.94 + (0, 0, height - 0.06), d * reach + (0, 0, height - 0.02)]
    k.add(bone + "stem", tube(pts, [0.016, 0.0135, 0.0125, 0.0125, 0.014]), VINE, bone, surface="leaf")
    pod = d * reach + np.array([0, 0, height])
    k.add(bone + "pod", ell(pod, (0.05, 0.05, 0.04), sub=4), POD, bone, surface="leaf")
    # steel spikes over it, green points hanging under it
    for j in range(4):
        t = 2 * math.pi * j / 4 + a + 0.4
        out = np.array([math.cos(t), math.sin(t), 0.0])
        p = pod + out * 0.04 + np.array([0, 0, 0.018])
        fs.spike(k, f"{bone}spike{j}", p, out + np.array([0, 0, 0.9]), 0.032, 0.012, bone, SPIKE, surface="metal",
                 seg=8)
    for j in range(3):
        t = 2 * math.pi * j / 3 + a + 1.0
        out = np.array([math.cos(t), math.sin(t), 0.0])
        p = pod + out * 0.036 + np.array([0, 0, -0.02])
        tip = p + out * 0.02 + np.array([0, 0, -0.032])
        side = np.cross(out, (0, 0, 1.0)) * 0.016
        k.add(f"{bone}sepal{j}", hull([p + side, p - side, p + out * 0.01 + (0, 0, 0.012), p - out * 0.012, tip]),
              POD, bone, fuse=False, surface="leaf")
    return pod, d


def ferrothorn(src, out):
    k = Kit(src, coat="metal")
    def lens(V):
        V = V.copy()
        V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.92, V[:, 2])
        return V
    k.add("lens", ell(C, (RXY, RXY, RZ), deform=lens, sub=5), STEEL, "body")
    k.add("cap", ell((0, 0, C[2] + RZ - 0.008), (0.04, 0.04, 0.02)), VINE, "head", surface="leaf")
    # the dark bands round its lower half
    for zb in BANDS:
        k.paint(lambda P, zb=zb: inside(np.abs(P[:, 2] - (C[2] + zb * RZ)) - BAND_W, 0.0018) * (P[:, 2] < C[2] + 0.02),
                BAND, on=["lens"])
    # the visor between the eyes, dipping to a point under them
    def visor(P):
        az, el = angles(P)
        lower = EYE_EL - 13 - 14 * np.clip(1 - np.abs(az) / 12, 0, 1)
        return (inside(np.abs(az) - 46, 1.2) * inside(lower - el, 1.0) * inside(el - (EYE_EL + 12), 1.0)
                * (P[:, 1] < 0))
    k.paint(visor, VISOR, on=["lens"])
    # spikes round the rim and on the top
    for i, (az, el) in enumerate(SPIKES):
        p, n = surf(az, el)
        fs.spike(k, f"spike{i}", p, n + np.array([0, 0, -0.15 if el < 0 else 0.0]), 0.06, 0.021, "body", SPIKE,
                 surface="metal")
    for i, (az, el) in enumerate(CHIPS):
        p, n = surf(az, el)
        fs.chip(k, p, n, 0.0065, i, CHIP)
    # the green points rising over the eyes' inner corners
    for s in (1, -1):
        p, n = surf(s * (EYE_AZ - 5), EYE_EL + 15)
        up = unit(*(n * 0.5 + np.array([0, 0, 1.0])))
        side = np.array([math.cos(math.radians(s * EYE_AZ)), math.sin(math.radians(s * EYE_AZ)), 0.0]) * 0.02
        k.add("lid" + ("R" if s > 0 else "L"), hull([p - n * 0.012 + side, p - n * 0.012 - side, p + n * 0.008 + side * 0.7,
                                                     p + n * 0.008 - side * 0.7, p + up * 0.045 - n * 0.002]),
              GREEN, "body", fuse=False, surface="shell")
    for bone, az, reach, height in VINES:
        vine(k, bone, az, reach, height)
    places = [surf(s * EYE_AZ, EYE_EL) for s in (1, -1)]
    places = [(p, unit(*(n + np.array([0, -0.5, -0.25])))) for p, n in places]
    fs.patches(k, places, (0.04, 0.029), eps=0.0025)
    k.build(voxel=VOXEL, smooth=3, band=0.0035, wband=0.03)
    k.eye_states(lambda state: fs.eyes(k, state, places, (0.029, 0.018), eps=0.004))
    k.export(out)


if __name__ == "__main__":
    ferrothorn(*args())
