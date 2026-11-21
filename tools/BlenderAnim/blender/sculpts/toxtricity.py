# Toxtricity (Amped Form) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/toxtricity.py -- <the export's Toxtricity.glb> <out.glb>
#
# Toxel grown (toxel.py: the same purple, its eye of a heavy lid with a dark line along it over a white with the iris
# low in it, its stubby round toes). From the references: a lanky purple punk standing upright and hunched, the head
# carried forward and low on a thick neck. The head is a wedge: its snout points forward and down, its crown sweeps
# back into a long pointed horn; a narrow scowling eye under a slanting lid with a small red iris; the lower jaw and
# throat yellow. Behind and over the head a great crest of pale yellow spikes like a flame, laid back. A yellow band
# from the throat down the chest, wide across the chest and narrowing to the groin, with two rows of lavender spikes
# hanging from the chest like strings; a slim waist ringed at the hips with lavender spikes. Long arms hanging forward
# and down, a ring of spikes at each wrist and big three-fingered hands at knee height. Legs with a yellow band across
# the thigh and another across the shin, broad feet with yellow toe-tips, a short tail. The joints: body (0, 0, 0.55),
# head (0, -0.01, 0.66), armR/L (+-0.055, 0, 0.64), legR/L (+-0.05, 0, 0.43), tail (0, 0.04, 0.44).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, axes, srgb, inside, unit, eye_look, args, sibling

tx = sibling("toxel")

PURPLE, YELLOW, CREST, CREST_LIGHT, SPIKE, IRIS, MOUTH = ("#6A3EAE", "#F4DE2C", "#F2E46E", "#FAF3B4", "#B48CDA",
                                                          "#D2304E", "#3A2450")


def spike(root, tip, width, thick=None):
    """A solid spike from root to tip: a kite of four points round the root and the tip, flat-faced."""
    root, tip = np.array(root, float), np.array(tip, float)
    d = tip - root
    a, b = axes(d)
    t = thick if thick is not None else width * 0.7
    pts = [root + a * width, root - a * width, root + b * t, root - b * t, root + d * 0.45 + a * width * 0.5,
           root + d * 0.45 - a * width * 0.5, tip]
    return hull(pts)


def trunk(k):
    # hunched: the hips low over the thighs, the chest broad and thrust forward, the shoulders up, the neck short
    k.add("pelvis", ell((0.0, 0.02, 0.4), (0.12, 0.088, 0.088)), PURPLE, "body")
    k.add("waist", ell((0.0, 0.0, 0.48), (0.088, 0.072, 0.07)), PURPLE, "body")
    k.add("chest", ell((0.0, -0.05, 0.58), (0.15, 0.1, 0.095)), PURPLE, "body")
    k.add("back", ell((0.0, 0.03, 0.625), (0.12, 0.08, 0.08)), PURPLE, "body")
    k.add("shoulder", ell((0.135, -0.03, 0.648), (0.068, 0.065, 0.062)), PURPLE, "arm{s}", mirror=True)
    k.add("neck", tube([(0.0, -0.02, 0.62), (0.0, -0.07, 0.665), (0.0, -0.11, 0.69)], [0.07, 0.066, 0.06]), PURPLE,
          "head")
    k.add("tail", tube([(0.0, 0.08, 0.41), (0.0, 0.14, 0.36), (0.0, 0.18, 0.32)], [0.045, 0.028, 0.012]), PURPLE,
          "tail")


def head(k):
    # the wedge: from the snout's point, forward and down, back and up into a long horn
    pts = [(0.0, -0.275, 0.666), (0.0, -0.19, 0.698), (0.0, -0.1, 0.738), (0.0, -0.01, 0.782), (0.0, 0.08, 0.815),
           (0.0, 0.15, 0.84)]
    V, F = tube(pts, [0.02, 0.08, 0.102, 0.085, 0.045, 0.01], seg=24)
    V[:, 0] *= np.interp(V[:, 1], [-0.2, -0.05, 0.15], [1.35, 1.2, 0.75])   # broader than tall from the front
    k.add("head", (V, F), PURPLE, "head")
    # the brows the eyes lie under
    k.add("cheek", ell((0.06, -0.165, 0.722), (0.044, 0.062, 0.046), rot=(25, 0, 0)), PURPLE, "head", mirror=True)
    # the yellow lower jaw
    k.add("jaw", ell((0.0, -0.18, 0.66), (0.068, 0.095, 0.036), rot=(-12, 0, 0)), YELLOW, "head")


def crest(k):
    """
    The great crest, the top mass with the head: a yellow core over the nape and spikes fanning out from it, as wide
    as the shoulders and as tall as the head again, leaning back.
    """
    core = np.array([0.0, 0.03, 0.835])
    k.add("crestcore", ell(core, (0.09, 0.08, 0.072)), CREST, "head", fuse=False, surface="skin")
    # (angle from straight up in the frontal plane, degrees, + to +X; length; lean back in degrees)
    rays = [(0, 0.27, 30), (-16, 0.25, 22), (16, 0.25, 22), (-32, 0.24, 30), (32, 0.24, 30), (-50, 0.21, 18),
            (50, 0.21, 18), (-68, 0.17, 25), (68, 0.17, 25), (-8, 0.2, 55), (8, 0.2, 55), (-26, 0.18, 62),
            (26, 0.18, 62), (0, 0.16, 75), (-42, 0.15, 48), (42, 0.15, 48)]
    for i, (phi, length, lean) in enumerate(rays):
        p, b = math.radians(phi), math.radians(lean)
        d = np.array([math.sin(p), math.sin(b) * math.cos(p), math.cos(b) * math.cos(p)])
        d = d / np.linalg.norm(d) * length
        root = core + np.array([math.sin(p) * 0.05, 0.0, 0.0])
        col = CREST_LIGHT if i % 3 == 1 else CREST
        k.add(f"crest{i}", spike(root, root + d, 0.058, 0.032), col, "head", fuse=False, surface="skin")


def arms(k):
    # thick arms hanging forward and down to the knees, the big hands turned in
    SH, EL, WR = np.array((0.16, -0.03, 0.64)), np.array((0.205, 0.0, 0.48)), np.array((0.185, -0.105, 0.335))
    k.add("upper", tube([SH, (0.19, -0.01, 0.56), EL], [0.062, 0.056, 0.05]), PURPLE, "arm{s}", mirror=True)
    k.add("fore", tube([EL, (0.198, -0.05, 0.4), WR], [0.05, 0.047, 0.044]), PURPLE, "arm{s}", mirror=True)
    H = np.array((0.172, -0.135, 0.272))
    k.add("hand", ell(H, (0.058, 0.05, 0.064), rot=(-15, 0, 12)), PURPLE, "arm{s}", mirror=True)
    for i, (a, l) in enumerate(((-0.032, 0.085), (0.0, 0.098), (0.032, 0.085))):
        b0 = H + np.array((a, -0.016, -0.03))
        k.add(f"finger{i}", tube([b0, b0 + (a * 0.3 - 0.01, -0.024, -l * 0.5), b0 + (a * 0.3 - 0.022, -0.014, -l)],
                                 [0.026, 0.025, 0.023]), PURPLE, "arm{s}", mirror=True)
    k.add("thumb", tube([H + (-0.04, -0.012, 0.0), H + (-0.062, -0.045, -0.035)], [0.023, 0.02]), PURPLE, "arm{s}",
          mirror=True)
    # a ring of spikes round each wrist
    for s, side in ((1, "R"), (-1, "L")):
        w = WR * np.array([s, 1, 1]) + np.array([0.0, -0.005, 0.0])
        for j, ang in enumerate((0, 90, 180, 270)):
            t = math.radians(ang)
            out = np.array([math.cos(t) * s, math.sin(t), 0.0]) * 0.8 + np.array([0, 0, 0.4])
            out /= np.linalg.norm(out)
            k.add(f"wrist{side}{j}", spike(w + out * 0.03, w + out * 0.1, 0.026), SPIKE, f"arm{side}", fuse=False,
                  surface="shell")


LEG_X = 0.105


def legs(k):
    # thick thighs under the low hips, knees bent forward, feet apart
    HIP, KN, AN = np.array((0.075, 0.02, 0.41)), np.array((0.1, -0.065, 0.21)), np.array((LEG_X, 0.0, 0.065))
    k.add("thigh", tube([HIP, (0.09, -0.03, 0.31), KN], [0.088, 0.078, 0.066]), PURPLE, "leg{s}", mirror=True)
    k.add("shin", tube([KN, (0.104, -0.035, 0.13), AN], [0.064, 0.056, 0.05]), PURPLE, "leg{s}", mirror=True)
    F = np.array((LEG_X, -0.03, 0.03))
    k.add("foot", ell(F, (0.062, 0.082, 0.032)), PURPLE, "leg{s}", mirror=True)
    for i, a in enumerate((-0.036, 0.0, 0.036)):
        k.add(f"toe{i}", ell(F + np.array([a, -0.078, -0.008]), (0.021, 0.022, 0.02)), YELLOW, "leg{s}", mirror=True)
    # a yellow band across the thigh and another across the shin, slanting
    def bands(P):
        ax = np.abs(P[:, 0])
        m = np.zeros(len(P))
        for z0, w in ((0.3, 0.032), (0.115, 0.027)):
            m = np.maximum(m, inside(np.abs(P[:, 2] - (z0 + 0.5 * (ax - LEG_X) - 0.15 * P[:, 1])) - w, 0.003))
        return m
    k.paint(bands, YELLOW, on=["thigh", "shin"])


def chest_mark(P):
    """The yellow down the front: wide across the chest, narrowing to the waist and the groin, up into the throat."""
    z = P[:, 2]
    half = np.interp(z, [0.38, 0.43, 0.5, 0.55, 0.6, 0.645, 0.7], [0.025, 0.045, 0.05, 0.1, 0.12, 0.075, 0.05])
    front = inside(P[:, 1] + 0.03, 0.004)
    return inside(np.abs(P[:, 0]) - half, 0.003) * front * inside(z - 0.71, 0.004) * inside(0.385 - z, 0.004)


def spikes(k):
    # two rows of lavender spikes hanging from the chest, large enough to read
    for x, z, l in ((0.045, 0.62, 0.1), (0.05, 0.565, 0.095), (0.042, 0.51, 0.08)):
        y = float(np.interp(z, [0.51, 0.565, 0.62], [-0.08, -0.13, -0.135]))
        for s, side in ((1, "R"), (-1, "L")):
            root = np.array([x * s, y, z])
            tip = root + np.array([0.012 * s, -0.035, -l])
            k.add(f"string{side}{z:.2f}", spike(root, tip, 0.028, 0.022), SPIKE, "body", fuse=False, surface="shell")
    # the ring of spikes at the hips: sides and back
    for j, ang in enumerate((-20, 20, 55, 90, 125, 160, 200)):
        t = math.radians(ang)
        out = np.array([math.cos(t), math.sin(t), -0.25])
        out /= np.linalg.norm(out)
        root = np.array([0.0, 0.02, 0.43]) + np.array([math.cos(t) * 0.11, math.sin(t) * 0.08, 0.0])
        k.add(f"hip{j}", spike(root, root + out * 0.12, 0.034), SPIKE, "body", fuse=False, surface="shell")
    # spikes down the back
    for j, z in enumerate((0.64, 0.57)):
        root = np.array([0.0, 0.09, z])
        k.add(f"spine{j}", spike(root, root + np.array([0.0, 0.065, -0.025]), 0.024), SPIKE, "body", fuse=False,
              surface="shell")


EYE_AT = np.array([0.078, -0.175, 0.738])
EYE_N = unit(0.62, -0.75, 0.2)
EYE_HALF = (0.04, 0.024)


def toxtricity(src, out):
    k = Kit(src, coat="scales")
    trunk(k)
    head(k)
    arms(k)
    legs(k)
    k.paint(chest_mark, YELLOW, on=["chest", "waist", "pelvis", "neck", "back"])
    # the mouth: a dark line along the jaw's top
    k.paint(lambda P: inside(np.abs(P[:, 2] - (0.676 + 0.2 * (P[:, 1] + 0.15))) - 0.004, 0.002)
            * inside(P[:, 1] + 0.1, 0.003), MOUTH, on=["head", "jaw"])
    crest(k)
    spikes(k)
    k.build(voxel=0.0065, smooth=4, band=0.006)
    look = tx.sullen_eye(PURPLE, lid_edge=lambda u: 0.02 + 0.3 * u, iris=(-0.05, -0.15), iris_r=0.42,
                         pupil_r=0.2, iris_col=IRIS)
    k.eye_states(lambda state: tx.lay_eyes(k, state, EYE_AT, EYE_N, EYE_HALF, PURPLE, look))
    k.export(out)


if __name__ == "__main__":
    toxtricity(*args())
