# Durant sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/durant.py -- <the export's Durant.glb> <out.glb>
#
# From the references: a steel ant, long and low. A round silver head with three round studs on its brow, two short
# elbowed feelers ending in balls, red eyes with a black pupil under a heavy dark brow, angled down to the middle; under
# the face two great flat mandibles curving round in front of it to meet in a ring. A round silver thorax with a dark
# zigzag band round its lower half, a narrow dark waist, a big round silver abdomen with studs and a dark zigzag band
# below. Six dark grey legs, short and thick, bent at a raised knee, ending in small points.
# Skeleton: body (0, 0, 0.09), head (0, -0.04, 0.1), legs/mid/hind (+-0.03, -0.04/0/0.04, 0.08).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, outward, srgb, inside, unit, tri, eye_look, polygon, args

SILVER, DARK, LEG, RED, INK = "#B9BAC2", "#55565F", "#4A4B54", "#E0283A", "#141418"

HC, HR = np.array([0.0, -0.1, 0.086]), np.array([0.052, 0.05, 0.047])
THC, THR = np.array([0.0, -0.025, 0.098]), np.array([0.046, 0.044, 0.05])
ABC, ABR = np.array([0.0, 0.1, 0.092]), np.array([0.066, 0.078, 0.06])


def on_ell(c, r, d):
    d = unit(*d)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def zigzag(c, r, top, teeth, depth, on):
    """A dark band round a round segment's lower part, its top edge a zigzag."""
    def m(P):
        rel = P - c
        th = np.arctan2(rel[:, 0], -rel[:, 1])
        edge = c[2] + top + depth * tri(th * teeth / (2 * math.pi))
        return inside(P[:, 2] - edge, 0.0015)
    return m


def durant(src, out):
    k = Kit(src, coat="metal")

    # --- the body: head, thorax, waist, abdomen ----------------------------------------------------------------
    k.add("head", ell(HC, HR), SILVER, "head")
    k.add("thorax", ell(THC, THR), SILVER, "body")
    k.add("waist", ell((0, 0.032, 0.095), (0.024, 0.026, 0.026)), DARK, "body")
    k.add("abdomen", ell(ABC, ABR, rot=(-8, 0, 0)), SILVER, "body")
    k.paint(zigzag(THC, THR, -0.006, 9, 0.008, None), DARK, on=["thorax"])
    k.paint(zigzag(ABC, ABR, -0.012, 11, 0.01, None), DARK, on=["abdomen"])
    # the head's underside and the chin dark, where the mandibles sit
    k.paint(lambda P: inside(P[:, 2] - (HC[2] - 0.03), 0.002) * inside(np.abs(P[:, 0]) - 0.05, 0.002), DARK,
            on=["head"])

    # studs: three on the brow, two on the thorax, four on the abdomen
    studs = [(HC, HR, (0, -0.55, 0.85), 0.0085), (HC, HR, (0.45, -0.45, 0.78), 0.0075),
             (HC, HR, (-0.45, -0.45, 0.78), 0.0075),
             (THC, THR, (0.5, 0.0, 0.85), 0.0075), (THC, THR, (-0.5, 0.0, 0.85), 0.0075),
             (ABC, ABR, (0.45, -0.2, 0.85), 0.008), (ABC, ABR, (-0.45, -0.2, 0.85), 0.008),
             (ABC, ABR, (0.42, 0.45, 0.75), 0.008), (ABC, ABR, (-0.42, 0.45, 0.75), 0.008)]
    for i, (c, r, d, s) in enumerate(studs):
        p, n = on_ell(c, r, d)
        k.add(f"stud{i}", ell(p + n * 0.002, (s, s, s * 0.8), sub=3), SILVER, "head" if c is HC else "body",
              fuse=False)

    # --- the feelers: short, elbowed, a ball at each end -------------------------------------------------------
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        base, elbow, tip = (np.array(p) * m for p in ((0.018, -0.11, 0.127), (0.028, -0.122, 0.148), (0.048, -0.138, 0.16)))
        k.add(f"feeler{side}", outward(tube([base, elbow, tip], [0.0045, 0.004, 0.0038], seg=12, per=6)), DARK,
              "head", fuse=False)
        k.add(f"knob{side}", ell(tip + np.array([0.004 * s, -0.002, 0.003]), (0.011, 0.011, 0.009), sub=3), DARK,
              "head", fuse=False)

    # --- the mandibles: two great flat jaws curving round in front of the face to meet in a ring -----------------
    for s, side in ((1, "R"), (-1, "L")):
        pts = []
        for t in np.linspace(0, 1, 7):
            a = math.radians(95 - 90 * t)                      # angle from the front, toward the side
            pts.append((s * 0.046 * math.sin(a), -0.13 - 0.046 * math.cos(a), 0.038 + 0.026 * math.cos(a)))
        k.add(f"mandible{side}", outward(blade(pts, [0.018, 0.019, 0.019, 0.018, 0.016, 0.014, 0.011], (0, 0.5, 1), 0.5)),
              SILVER, "head", fuse=False)

    # --- six short thick legs, bent at a raised knee -----------------------------------------------------------
    for bone, y, kx, ky, fx, fy in (("leg{s}", -0.045, 0.078, -0.072, 0.092, -0.092),
                                    ("mid{s}", 0.005, 0.086, 0.005, 0.104, 0.008),
                                    ("hind{s}", 0.05, 0.082, 0.08, 0.098, 0.115)):
        hip = np.array((0.03, y, 0.08))
        knee = np.array((kx, ky, 0.088))
        foot = np.array((fx, fy, 0.008))
        mid = knee * 0.45 + foot * 0.55 + np.array((0.004, 0, 0.004))
        k.add(f"leg{bone[0]}", outward(tube([hip, knee, mid, foot], [0.016, 0.0145, 0.012, 0.008])), LEG, bone,
              fuse=False, mirror=True)
        # a small point at the foot, turned forward
        c = foot + np.array((0.002, -0.004, -0.004))
        k.add(f"claw{bone[0]}", hull([c + (0.006, 0.006, 0), c + (-0.006, 0.006, 0), c + (0, 0.006, 0.008),
                                       c + (0, -0.016, -0.003)]), LEG, bone, fuse=False, mirror=True)

    k.build(voxel=0.003, smooth=3, band=0.005)

    # --- the eyes: red, a black pupil, under a heavy dark brow slanting down to the middle -----------------------
    EC, EN = on_ell(HC, HR, (0.58, -0.76, 0.08))
    shape = [(-1.0, -0.1), (-0.7, -0.6), (0.0, -0.8), (0.7, -0.55), (1.0, -0.05), (0.9, 0.55), (-0.9, -0.05)]
    CR, CK, CS, CD = srgb(RED), srgb(INK), srgb(SILVER), srgb(DARK)
    def eye_open(u, v):
        c = np.tile(CR, (len(u), 1))
        c[((u + 0.1) / 0.3) ** 2 + ((v + 0.2) / 0.5) ** 2 < 1] = CK
        c[((u + 0.15) / 0.11) ** 2 + ((v + 0.0) / 0.12) ** 2 < 1] = (0.9, 0.9, 0.9)
        return c
    def lay(state):
        look = eye_look(state, eye_open, CD, CK, line=0.2)
        for s in (1, -1):
            mm = np.array([s, 1, 1.0])
            col = look if s == 1 else (lambda a, b, f=look: f(-a, b))
            sh = shape if s == 1 else [(-a, b) for a, b in shape]
            # the dark brow and socket under the eye, then the eye on it
            k.decal(EC * mm + np.array([0, 0, 0.002]), EN * mm, (0.024, 0.019), CD, eps=0.0015, rings=8, segs=40,
                    outline=polygon([(-1, 0.0), (-0.6, -0.7), (0.5, -0.75), (1, -0.1), (0.95, 0.85), (-0.95, 0.15)]
                                    if s == 1 else [(1, 0.0), (0.6, -0.7), (-0.5, -0.75), (-1, -0.1), (-0.95, 0.85),
                                                    (0.95, 0.15)]))
            k.decal(EC * mm, EN * mm, (0.019, 0.014), col, eps=0.003, rings=20, segs=56, outline=polygon(sh))
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    durant(*args())
