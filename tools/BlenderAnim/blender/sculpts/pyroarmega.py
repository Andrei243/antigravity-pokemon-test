# Mega Pyroar sculpted on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pyroarmega.py -- <the export's Pyroar-Mega.glb> <out.glb>
# Grown from pyroar.py (sibling: its body, legs, dark back, tail, face, nose and eyes; the two skeletons are the same).
# From the pictures: the lion of Pyroar crouched under a mane grown into a blaze that is most of the figure. The mane
# is now gold with red edges (the base's is red with gold): a burst of broad flame tongues round a small face, three a
# side rising and curling out, a skirt of tongues under the chin reaching nearly to the ground, and from the crown a
# great plume of flame rising as tall again as the lion and sweeping back over its body (here back and out to +X so
# both of the game's cameras see its face). The face is lowered and roaring: the jaw dropped, a dark red mouth with a
# pink tongue and two white fangs, and a red slash running down through each eye from the brow to the cheek. Tan
# tufts of flame rise from the haunches and a dark one from the back.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, polygon, args, sibling

py = sibling("pyroar")
RED, GOLD = "#DC2A24", "#F8CF40"
TAN, DARK = py.TAN, py.DARK
MOUTH, TONGUE, FANG = "#7A1C22", "#E87A86", "#FAF6EE"

# The face lowered and carried forward of the base's (the pictures roar with the head low in the mane)
py.F = np.array([0, -0.315, 0.47])
py.E = py.F + py.EYE_DIR / np.linalg.norm(py.EYE_DIR / py.FR) * 0.995
F = py.F


def tongue(k, name, pts, radii, shift, bone, mirror=True, f=0.5, axis=(0, 1, 0), front=(0, -0.012, 0)):
    """A flame tongue: a red blade, and a gold one a little smaller laid over its front, shifted toward one edge,
    so that a red edge shows along the other."""
    pts = np.array(pts, float)
    k.add(name, blade(pts, radii, axis, f), RED, bone, mirror=mirror)
    w = np.array(radii, float)
    off = np.array(shift, float)
    for j, sgn in enumerate((1, -1)):          # on both faces, so the back camera sees gold tongues edged red too
        gp = [p + off * w[i] + np.array(front) * sgn * (1 if i else 0.6) for i, p in enumerate(pts)]
        k.add(name + f"g{j}", blade(gp, list(w * 0.78), axis, f * 1.05), GOLD, bone, mirror=mirror)


# Tongues on +X (mirrored): points, radii, which way the gold is shifted (as a share of the radius), bone
UPPER = [
    ([(0.05, -0.24, 0.6), (0.09, -0.24, 0.7), (0.15, -0.23, 0.78), (0.22, -0.21, 0.81)], [0.06, 0.052, 0.034, 0.004],
     (-0.45, 0, 0.1), "head"),
    ([(0.1, -0.25, 0.55), (0.18, -0.25, 0.62), (0.25, -0.24, 0.67), (0.31, -0.22, 0.66)], [0.062, 0.052, 0.032, 0.004],
     (-0.1, 0, 0.45), "head"),
    ([(0.12, -0.26, 0.46), (0.21, -0.27, 0.47), (0.28, -0.27, 0.51), (0.33, -0.25, 0.56)], [0.062, 0.05, 0.03, 0.004],
     (0, 0, 0.45), "head"),
]
LOWER = [
    ([(0.1, -0.29, 0.37), (0.19, -0.31, 0.32), (0.26, -0.31, 0.27), (0.32, -0.3, 0.27)], [0.06, 0.05, 0.03, 0.004],
     (0, 0, 0.45), "body"),
    ([(0.07, -0.3, 0.3), (0.12, -0.32, 0.2), (0.16, -0.33, 0.11), (0.21, -0.32, 0.07)], [0.06, 0.05, 0.03, 0.004],
     (-0.45, 0, 0.1), "body"),
]
MIDDLE = ([(0, -0.31, 0.32), (0, -0.34, 0.21), (0.01, -0.36, 0.1), (0.035, -0.35, 0.04)], [0.064, 0.052, 0.032, 0.004])
PLUME = [(0, -0.22, 0.6), (0.01, -0.21, 0.72), (0.035, -0.15, 0.83), (0.075, -0.06, 0.9), (0.125, 0.05, 0.92),
         (0.18, 0.15, 0.89), (0.235, 0.22, 0.9), (0.27, 0.27, 0.95)]
PLUME_R = [0.085, 0.1, 0.095, 0.08, 0.062, 0.044, 0.026, 0.004]


def add_mane(k):
    # the core behind the face, gold, framed by the tongues' roots
    k.add("manecore", ell((0, -0.17, 0.48), (0.15, 0.11, 0.16)), GOLD, "head")
    k.add("maneback", ell((0, -0.1, 0.47), (0.145, 0.11, 0.155)), GOLD, "head")
    k.add("bib", ell((0, -0.25, 0.36), (0.13, 0.08, 0.11)), GOLD, "body")
    for i, (pts, rr, sh, bone) in enumerate(UPPER + LOWER):
        tongue(k, f"flame{i}", pts, rr, sh, bone)
    tongue(k, "flamemid", MIDDLE[0], MIDDLE[1], (-0.35, 0, 0.2), "body", mirror=False)
    # the plume: gold with a red edge along its underside and back, thick enough to read from every side
    P = np.array(PLUME)
    k.add("plume", blade(P, PLUME_R, (0.85, -0.3, 0.2), 0.6), GOLD, "head")
    red = [p + np.array([0.012, 0.025, -0.03]) * min(1, PLUME_R[i] / 0.05) for i, p in enumerate(P)]
    k.add("plumered", blade(red, [r * 0.85 for r in PLUME_R], (0.85, -0.3, 0.2), 0.6), RED, "head")


def add_tufts(k):
    """Tan flames rising from the haunches, a dark one from the back."""
    k.add("hiptuft", blade([(0.06, 0.14, 0.37), (0.075, 0.17, 0.44), (0.085, 0.21, 0.5), (0.08, 0.25, 0.53)],
                           [0.042, 0.032, 0.018, 0.003], (0.3, 1, 0), 0.5), TAN, "hind{s}", mirror=True)
    k.add("hiptuft2", blade([(0.045, 0.21, 0.37), (0.055, 0.25, 0.43), (0.06, 0.29, 0.46)], [0.034, 0.022, 0.003],
                            (0.3, 1, 0), 0.5), TAN, "hind{s}", mirror=True)
    k.add("backtuft", blade([(0, -0.02, 0.39), (0.01, 0.02, 0.46), (0.02, 0.07, 0.5)], [0.045, 0.03, 0.003],
                            (1, 0, 0), 0.55), DARK, "body")


def add_jaw(k):
    k.add("jaw", ell(F + (0, -0.038, -0.088), (0.03, 0.034, 0.02)), TAN, "head")


def add_mouth(k):
    """A roaring mouth: a dark red opening over the gap between muzzle and jaw, a pink tongue, two white fangs."""
    cm, ct = srgb(MOUTH), srgb(TONGUE)
    def look(u, v):
        c = np.tile(cm, (len(u), 1))
        c[(v < -0.2) & (np.hypot(u / 0.7, (v + 0.75) / 0.6) < 1)] = ct
        return c
    c = F + np.array([0, -0.074, -0.074])
    n = unit(0, -1, -0.35)
    k.decal(c, n, (0.026, 0.02), look, eps=0.003, rings=12, segs=48)
    for s in (1, -1):
        k.decal(c + np.array([0.015 * s, 0, 0.012]), n, (0.006, 0.009), FANG, eps=0.0045,
                outline=polygon([(-1, 0.9), (1, 0.9), (0, -1)]), rings=6, segs=30)


def add_slash(k):
    """A red slash down through each eye: from the brow up into the mane, and from under the eye down the cheek."""
    ex, ez = py.E[0], py.E[2]
    def mask(P):
        ax = np.abs(P[:, 0])
        line = inside(np.abs(ax - (ex + 0.18 * (P[:, 2] - ez))) - 0.007, 0.002)
        gap = 1 - inside(np.abs(P[:, 2] - ez) - 0.026, 0.002)
        return line * gap * (P[:, 2] > ez - 0.075) * (P[:, 1] < F[1] + 0.02)
    k.paint(mask, RED, on=["face", "muzzle", "jaw"])


def pyroarmega(src, dest):
    k = Kit(src, coat="fur")
    py.k = k
    py.add_body()
    py.add_legs()
    py.add_back()
    py.add_tail()
    add_tufts(k)
    add_mane(k)
    py.add_face()
    add_jaw(k)
    py.add_face_marks()
    add_slash(k)
    py.add_nose()
    add_mouth(k)
    k.build(voxel=0.008, smooth=4, band=0.006, wband=0.06)
    k.eye_states(py.eyes)
    k.export(dest)


if __name__ == "__main__":
    pyroarmega(*args())
