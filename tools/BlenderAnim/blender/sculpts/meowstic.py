# Meowstic (the male) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/meowstic.py -- <the export's Meowstic.glb> <out.glb>
# Grown from espurr.py (sibling: its legs with white toes, its collar of pointed tufts, its way of making eyes, nose and
# mouth, its pear of a body flaring at the hem). From the references: an upright navy-blue cat, slim, with a wide head
# whose cheeks end in points; tall ears standing up and a little out (no longer folded), rolled at their edges, each
# with a white point and two white stripes across it low down; a broad white forelock sweeping from the crown down
# over the face to between the eyes on the viewer's left; slanted green eyes with a dark pupil and a pale glint; a
# white ruff under the chin; short arms tipped white; a body flaring into a point at each hip; slender legs with white
# feet; and behind it two great thick tails, as big together as the body, curling out to one side, each crossed by
# two white stripes and ending in a long white curl.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, polygon, args, sibling

es = sibling("espurr")
NAVY = "#3258A8"           # between the drawing's navy and the 3D model's brighter blue
WHITE = es.WHITE
IRIS, IRIS_D, GLINT, PUPIL = "#35B38A", "#1E8466", "#B4F0D8", "#10283A"
NOSE, INK = "#1A2440", "#18203A"


def meowstic(src, dest):
    k = Kit(src, coat="fur")

    # --- the head: wide, a tenth over the drawing, its cheeks drawn out into points ---------------------------------
    H = np.array([0.0, -0.02, 0.298])
    HR = np.array([0.098, 0.08, 0.082])
    def skull(V):
        V = V.copy()
        V[:, 1] = np.where(V[:, 1] < 0, V[:, 1] * 0.9, V[:, 1])
        V[:, 0] *= 1 + 0.08 * np.clip(-V[:, 2], 0, 1)
        return V
    k.add("head", ell(H, HR, deform=skull), NAVY, "head")
    k.add("neck", tube([(0, -0.005, 0.22), (0, -0.012, 0.26)], [0.036, 0.04]), NAVY, "head")
    for i, (z, ln, dz) in enumerate(((0.275, 0.05, -0.35), (0.255, 0.036, -0.75))):
        base = np.array((0.085, -0.03, z))
        d = unit(1, 0.1, dz)
        k.add(f"cheek{i}", blade([base, base + d * ln * 0.5, base + d * ln], [0.022, 0.014, 0.004], (0, 1, 0), 0.5),
              NAVY, "head", mirror=True)

    # The ears: tall, broad at the root, standing up and some 22 degrees out, flattened front to back and rolled
    # forward at the edges; a white point and two white stripes low across them
    EAR = [(0.05, -0.02, 0.35), (0.075, -0.022, 0.41), (0.1, -0.02, 0.47), (0.122, -0.012, 0.525)]
    k.add("ear", blade(EAR, [0.046, 0.044, 0.032, 0.006], (0, 1, 0), 0.42), NAVY, "ear{s}", mirror=True)
    k.add("rollear", blade([(0.072, -0.034, 0.37), (0.098, -0.036, 0.43), (0.116, -0.03, 0.49)], [0.014, 0.012, 0.006],
                           (0, 1, 0), 0.7), NAVY, "ear{s}", mirror=True)
    def ear_marks(P):
        t, off = es.along(P, EAR)
        near = (np.linalg.norm(off, axis=1) < 0.06) * (P[:, 2] > 0.345)
        bands = (inside(np.abs(t - 0.3) - 0.04, 0.0025) + inside(np.abs(t - 0.48) - 0.035, 0.0025)) * (off[:, 1] < 0.004)
        return np.clip(inside(0.76 - t, 0.003) + bands, 0, 1) * near
    k.paint(ear_marks, WHITE, on=["ear"])

    # The forelock: a broad white sweep from the crown down over the face toward the viewer's left (-X), between the eyes
    LOCK = [(0.012, -0.03, 0.39), (0.0, -0.06, 0.375), (-0.016, -0.083, 0.345), (-0.022, -0.093, 0.315),
            (-0.018, -0.096, 0.29)]
    k.add("lock", blade(LOCK, [0.024, 0.03, 0.026, 0.017, 0.006], (0, 1, 0.25), 0.45), WHITE, "head")
    k.add("lockroot", ell((0.006, -0.025, 0.378), (0.04, 0.035, 0.022)), WHITE, "head")

    # --- the body: slim, a deep chest narrowing to the waist and flaring at its hem into a point at each hip ---------
    def pear(V):
        V = V.copy()
        V[:, :2] *= (1 + 0.15 * np.clip(-V[:, 2], 0, 1))[:, None]
        return V
    k.add("torso", ell((0, 0.0, 0.185), (0.046, 0.04, 0.078), deform=pear), NAVY, "body")
    for i, (z, d, ln) in enumerate(((0.135, unit(1, -0.25, -0.6), 0.04), (0.128, unit(0.3, -1, -0.7), 0.03))):
        base = np.array((0.035, -0.012 * i, z))
        k.add(f"hem{i}", blade([base, base + d * ln * 0.5, base + d * ln], [0.016, 0.01, 0.003], (0, 0, 1), 0.5),
              NAVY, "body", mirror=True)
    # The white ruff under the chin: soft puffs and a few points hanging over the chest
    for i, (x, y, z, r) in enumerate(((0.0, -0.05, 0.245, 0.03), (0.032, -0.042, 0.248, 0.026),
                                      (0.05, -0.02, 0.25, 0.022))):
        k.add(f"ruff{i}", ell((x, y, z), (r, r * 0.85, r * 0.8)), WHITE, "body", mirror=x > 0)
    es.collar(k, (0, -0.012), 0.04, 0.036, 0.238, 9, 0.03, 0.013, WHITE, front_only=True, droop=0.8)

    # Short arms carried forward and down, the hands in front of the belly and inside the body's width, tipped white
    ARM = [(0.036, -0.012, 0.268), (0.05, -0.04, 0.235), (0.052, -0.066, 0.21)]
    k.add("arm", tube(ARM, [0.016, 0.014, 0.013]), NAVY, "arm{s}", mirror=True)
    k.add("hand", blade([(0.052, -0.066, 0.212), (0.053, -0.079, 0.2), (0.053, -0.086, 0.192)], [0.013, 0.01, 0.004],
                        (1, 0, 0), 0.6), WHITE, "arm{s}", mirror=True)

    # Slender legs with full thighs fused under the hem, white feet
    k.add("thigh", ell((0.031, 0.002, 0.125), (0.034, 0.036, 0.055)), NAVY, "leg{s}", mirror=True)
    es.legs(k, 0.029, 0.12, -0.006, NAVY, r=0.021)
    def feet(P):
        return inside(P[:, 2] - 0.02, 0.0015) * (P[:, 1] < 0.0) * inside(np.abs(P[:, 0]) - 0.06, 0.002)
    k.paint(feet, WHITE, on=["foot", "leg"])

    # --- the two tails: great and thick, from the tail's root back and out to the +X side (some 35 degrees off straight
    #     back, so both of the game's cameras see them), one curving up and over, one sweeping low; each crossed by two
    #     white stripes and ending in a long white curl ----------------------------------------------------------------
    TAILS = [
        ([(0.0, 0.03, 0.19), (0.04, 0.085, 0.24), (0.09, 0.14, 0.3), (0.14, 0.18, 0.315), (0.18, 0.2, 0.28),
          (0.19, 0.19, 0.235), (0.175, 0.165, 0.215)], [0.022, 0.044, 0.054, 0.048, 0.036, 0.02, 0.008]),
        ([(0.0, 0.03, 0.17), (0.045, 0.09, 0.16), (0.095, 0.14, 0.145), (0.14, 0.17, 0.11), (0.16, 0.17, 0.06),
          (0.15, 0.14, 0.032), (0.125, 0.12, 0.035)], [0.022, 0.044, 0.052, 0.046, 0.034, 0.018, 0.008]),
    ]
    for j, (pts, radii) in enumerate(TAILS):
        k.add(f"tail{j}", tube(pts, radii, seg=18), NAVY, "tail")
        def marks(P, pts=pts):
            t, off = es.along(P, pts, absx=False)
            near = np.linalg.norm(off, axis=1) < 0.065
            bands = inside(np.abs(t - 0.38) - 0.03, 0.003) + inside(np.abs(t - 0.53) - 0.026, 0.003)
            return np.clip(inside(0.68 - t, 0.003) + bands, 0, 1) * near * (P[:, 1] > 0.05)
        k.paint(marks, WHITE, on=[f"tail{j}"])

    # --- the face: a small dark nose and a short mouth under the eyes ------------------------------------------------
    k.decal((0, -0.092, 0.268), unit(0, -1, 0.05), (0.006, 0.0048), NOSE, eps=0.002,
            outline=polygon([(-1, 0.7), (1, 0.7), (0, -1)]), rings=5, segs=20)
    CN, CI = srgb(NAVY), srgb(INK)
    def mouth(u, v):
        c = np.tile(CN, (len(u), 1))
        c[(np.abs(v - (-0.1 + 1.2 * (np.abs(u) - 0.4) ** 2)) < 0.28) & (np.abs(u) < 0.85)] = CI
        return c
    k.decal((0, -0.09, 0.255), unit(0, -1, -0.05), (0.012, 0.007), mouth, eps=0.002, rings=8, segs=40)

    k.build(voxel=0.0042, band=0.005, wband=0.035)

    # The eyes: slanted almonds, the outer corner up; green with a darker ring, a dark upright pupil looking in and a
    # pale glint; their top edge a straight line sloping up outward, cut by the fur over them
    ci, cd, cg, cp = srgb(IRIS), srgb(IRIS_D), srgb(GLINT), srgb(PUPIL)
    def eye(u, v):
        r = np.hypot(u, v)
        c = np.tile(ci, (len(u), 1))
        c[r > 0.8] = cd
        c[np.hypot((u + 0.12) / 0.3, (v + 0.05) / 0.52) < 1] = cp
        c[np.hypot((u - 0.25) / 0.24, (v - 0.3) / 0.22) < 1] = cg
        c[np.hypot((u + 0.18) / 0.12, (v + 0.4) / 0.1) < 1] = cg
        return c
    # The outline is not mirrored by the kit (only the colour's u): each eye is laid by hand, u outward on both
    CUT = [(-1.2, 0.45), (1.2, 0.95), (1.2, -1.2), (-1.2, -1.2)]
    out_r = lambda th, p=polygon(CUT): min(1.0, p(th))
    out_l = lambda th, p=polygon([(-a, b) for a, b in CUT]): min(1.0, p(th))
    de = unit(0.5, -0.84, 0.0)
    E = np.array((0.052, -0.078, 0.302))
    m = np.array([-1, 1, 1.0])
    def eyes(state):
        look = eye_look(state, eye, srgb(NAVY), srgb(INK))
        k.decal(E, de, (0.026, 0.026), look, eps=0.002, rings=20, segs=56, outline=out_r)
        k.decal(E * m, de * m, (0.026, 0.026), lambda u, v: look(-u, v), eps=0.002, rings=20, segs=56,
                outline=out_l)
    k.eye_states(eyes)
    k.export(dest)


if __name__ == "__main__":
    src, dest = args()
    meowstic(src, dest)
