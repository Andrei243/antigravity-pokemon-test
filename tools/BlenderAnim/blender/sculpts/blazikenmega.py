# Mega Blaziken sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/blazikenmega.py -- <the export's Blaziken-Mega.glb> <out.glb>
# Blaziken (blaziken.py: its red head and short beak, its sharp eyes, its red trunk and arms, grey hands, boots of fur
# and claws, made by torchic.py's spikes, ruffs and eyes) grown into its Mega, as the two pictures draw it: the legs
# turned dark from the hips down, the red of the hips running onto them in a jagged hem and red boots of fur from the
# knee down, jagged at the top and flared at the ground; two great cream plumes rising from the head in a V, a red point
# of the face's mask between them, cream locks framing the face down to a great cream bib over the chest, jagged at the
# waist; flames streaming back from the wrists. The long mane down the back, the horns, the grey bands, the yellow point
# of the breast and the plumes behind the waist are gone, as the pictures show.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, args, sibling

BZ = sibling("blaziken")
T = BZ.T
RED, CREAM, GREY, CLAW, INK = BZ.RED, BZ.CREAM, BZ.GREY, BZ.CLAW, BZ.INK
DARK = "#3A2A26"
BOOT = "#E2402E"
FLAME, FLAMEIN = "#F5962A", "#FFD24A"
SCLERA, IRIS = BZ.SCLERA, BZ.IRIS
VOXEL = 0.0075
H, HR, DE, EYE_HALF, EYE_SHAPE = BZ.H, BZ.HR, BZ.DE, BZ.EYE_HALF, BZ.EYE_SHAPE


def join(*shapes):
    """Several closed shapes as one piece (a flame's tongues share a material)."""
    Vs, Fs, n = [], [], 0
    for V, F in shapes:
        Vs.append(np.asarray(V)); Fs += [[int(i) + n for i in f] for f in F]; n += len(V)
    return np.vstack(Vs), Fs


def tongue(base, d, up, length, width, f, wave=0.035, n=9, reach=1.0):
    """A broad tongue of flame: a flattened tube along a gentle wave from base along d, tapering to a point, its faces
    across the wave (up), so the cameras that look along the wave's normal see its faces."""
    ts = np.linspace(0, reach, n)
    pts = [base + d * length * t + up * wave * math.sin(math.pi * 1.6 * t) for t in ts]
    radii = [max(0.006, width * (1 - t) ** 0.8 * (0.75 + 0.25 * math.sin(math.pi * min(1, t * 2.2)))) for t in ts]
    across = np.cross(d, up)
    return blade(pts, radii, across, f)


def flame(base, d, length, width):
    """A flame streaming from base along d: a long broad tongue and a shorter one branching up from it, orange, and a
    yellow heart along the first half, thicker than the orange so it shows on both faces."""
    d = unit(*d)
    base = np.array(base, float)
    # The tongues lie in the plane of d and the model's x, so the front and back cameras see their faces
    up = np.array([1.0, 0, 0]) - d * d[0]; up /= np.linalg.norm(up)
    outer = join(tongue(base, d, up, length, width, 0.34),
                 tongue(base + d * length * 0.12, unit(*(d + np.array([0, 0, 0.5]))), up, length * 0.55, width * 0.75, 0.34,
                        wave=0.02))
    inner = tongue(base + d * 0.01, d, up, length, width * 0.55, 0.95, reach=0.5, n=5)
    return outer, inner


def blazikenmega(src, out):
    k = Kit(src, coat="fur")

    # The trunk: red hips and waist, broad shoulders, a cream neck
    k.add("pelvis", ell((0, 0.01, 0.5), (0.115, 0.09, 0.088)), RED, "body")
    k.add("hem", T.ruff((0, 0.01, 0.47), (0.118, 0.092, 0.06), 9, 1.0, 0.12), RED, "body")
    k.add("torso", ell((0, 0.0, 0.625), (0.098, 0.078, 0.12)), RED, "body")
    k.add("shoulders", ell((0, 0.005, 0.715), (0.122, 0.085, 0.062)), RED, "body")
    k.add("neck", ell((0, -0.018, 0.775), (0.056, 0.055, 0.06)), CREAM, "head")
    # The great cream bib over the chest, jagged at the waist
    k.add("bib", T.ruff((0, -0.012, 0.705), (0.13, 0.096, 0.07), 9, 1.05, 0.22), CREAM, "body")

    # The head: red, a short pointed beak open a little, a red point of the mask rising between the plumes
    k.add("head", ell(H, HR), RED, "head")
    k.add("beak", ell((0, -0.135, 0.828), (0.034, 0.046, 0.02), rot=(12, 0, 0), deform=T.pointed), RED, "head",
          fuse=False)
    k.add("jaw", ell((0, -0.118, 0.8), (0.022, 0.032, 0.012), rot=(18, 0, 0), deform=T.pointed), "#E2B4A6", "head",
          fuse=False, surface="skin")
    k.add("maskpoint", T.spike((0, -0.06, 0.915), (0, 0.25, 1), 0.085, 0.03, 0.022, across=(1, 0, 0)), RED, "head",
          fuse=False)
    # The plumes: two great broad cream points rising in a V from the head
    k.add("plume", T.spike((0.05, -0.0, 0.89), (0.36, 0.2, 0.92), 0.34, 0.085, 0.034, across=(1, -0.25, -0.35),
                           belly=1.15), CREAM, "head", fuse=False, mirror=True, surface="hair")
    # Cream locks framing the face down to the bib
    k.add("lock", tube([(0.06, -0.01, 0.9), (0.092, -0.02, 0.83), (0.098, -0.035, 0.76), (0.085, -0.05, 0.71)],
                       [0.034, 0.042, 0.042, 0.03]), CREAM, "head", mirror=True)
    k.add("nape", ell((0, 0.055, 0.81), (0.085, 0.068, 0.1)), CREAM, "head")
    # The cream runs on down the back of the shoulders to the bib's points
    k.paint(lambda P: inside(0.715 - P[:, 2], 0.004) * inside(P[:, 2] - 0.9, 0.004) * inside(0.02 - P[:, 1], 0.006)
            * inside(np.abs(P[:, 0]) - 0.11, 0.006), CREAM, on=["shoulders", "torso", "neck", "nape", "head"])

    # Arms: red, bent forward, grey hands of three thick fingers
    sh, el, wr = np.array([0.09, -0.01, 0.72]), np.array([0.168, 0.015, 0.6]), np.array([0.155, -0.15, 0.565])
    k.add("arm", tube([sh, el, wr], [0.046, 0.041, 0.037]), RED, "arm{s}", mirror=True)
    along = (wr - el) / np.linalg.norm(wr - el)
    palm = wr + along * 0.035
    k.add("hand", ell(palm, (0.04, 0.044, 0.038)), GREY, "arm{s}", mirror=True)
    for i, dx in enumerate((-0.024, 0.0, 0.024)):
        d = along + np.array([dx * 6, 0, -0.35])
        d /= np.linalg.norm(d)
        b = palm + along * 0.02 + np.array([dx, 0, -0.01])
        k.add(f"finger{i}", tube([b, b + d * 0.035, b + d * 0.06 + np.array([0, 0, -0.012])], [0.019, 0.018, 0.016]),
              GREY, "arm{s}", mirror=True)
    # Flames streaming back, up and out from each wrist
    fo, fi = flame(wr + np.array([0.04, 0.02, 0.01]), (0.16, 0.8, 0.58), 0.62, 0.05)
    k.add("flame", fo, FLAME, "arm{s}", fuse=False, mirror=True, glow=FLAME)
    k.add("flamein", fi, FLAMEIN, "arm{s}", fuse=False, mirror=True, glow=FLAMEIN)

    # Legs: thick dark thighs fused into the hips, dark shins, red boots of fur from the knee down
    k.add("thigh", tube([(0.08, 0.01, 0.5), (0.1, -0.03, 0.38), (0.11, -0.04, 0.3)], [0.076, 0.068, 0.058]), DARK,
          "leg{s}", mirror=True)
    k.add("shin", tube([(0.11, -0.04, 0.3), (0.116, -0.01, 0.24)], [0.056, 0.054]), DARK, "leg{s}", mirror=True)
    k.add("boot", tube([(0.116, -0.01, 0.255), (0.118, -0.008, 0.15), (0.118, -0.012, 0.085)], [0.058, 0.072, 0.085]),
          BOOT, "leg{s}", mirror=True)
    k.add("bootcuff", T.ruff((0.116, -0.008, 0.265), (0.07, 0.07, 0.032), 7, 2.6, 0.12, up=True), BOOT, "leg{s}",
          mirror=True)
    k.add("boothem", T.ruff((0.118, -0.012, 0.066), (0.088, 0.094, 0.034), 8, 0.75, 0.45), BOOT, "leg{s}", mirror=True)
    for i, dx in enumerate((-0.03, 0.0, 0.03)):
        k.add(f"claw{i}", T.claw((0.118 + dx, -0.08, 0.03), (dx * 5, -1, -0.15), 0.045, 0.015), CLAW, "leg{s}",
              fuse=False, mirror=True, surface="shell")

    k.build(voxel=VOXEL, band=0.006)

    E = T.on_ell(H, HR, DE)
    look = T.eye_open(SCLERA, iris=IRIS, iris_c=(-0.18, -0.08), iris_r=0.52, pupil=INK, pupil_c=(-0.18, -0.08),
                      pupil_r=(0.24, 0.3), glint=(-0.32, 0.12, 0.13))
    k.eye_states(lambda state: T.lay_eyes(k, state, E, DE, EYE_HALF, look, srgb(RED), INK, outline=EYE_SHAPE,
                                          rim=INK, inset=0.78))
    k.export(out)


if __name__ == "__main__":
    blazikenmega(*args())
