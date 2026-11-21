# Musharna sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Munna's script (munna.py, imported as sibling("munna")): the same pale pink and the same flat-bottomed dome.
#   blender --background --factory-startup --python sculpts/musharna.py -- <the export's Musharna.glb> <out.glb>
#
# The design, from the references: a larger round body curled up asleep, its front half pale pink (the face) and its
# back lavender, the two meeting in a line of three round lobes over the crown; eyes always shut, a curved line with
# short lashes on each side of the face; a small pink mark over them at the front; two short lavender legs folded
# under the front with pink feet; a plume of pink dream smoke out of the forehead, a lumpy wavy column turning to the
# +X side as it rises and rolling into a curl at its end (kept small: in the art it is twice the body's height).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, sibling

m = sibling("munna")
PALE, LAVENDER, FEET, SMOKE, LINE = "#F8DCE2", "#A898DC", "#E2909E", "#F09AAA", "#3A2230"
MARK = "#E58E9E"

# The skeleton's joints: body (0, 0, 0.186), head (0, -0.14, 0.226), tail (0, -0.05, 0.296), legs (+-0.07, -0.05, 0.086)
MUSHARNA = dict(
    C=(0.0, 0.02, 0.19), R=(0.178, 0.2, 0.172),
    eye_dir=(0.56, -0.8, 0.0), eye_half=(0.034, 0.026),
    smoke=[(0.0, -0.06, 0.33), (0.03, -0.045, 0.38), (0.08, -0.015, 0.405), (0.1, 0.025, 0.445),
           (0.075, 0.06, 0.475), (0.11, 0.08, 0.495), (0.145, 0.085, 0.485)],
    smoke_r=[0.042, 0.04, 0.036, 0.04, 0.034, 0.03, 0.026],
)


def face_mask(Pr):
    """Pale in front of a line of three round lobes over the crown, lavender behind it."""
    C, R = np.array(Pr["C"]), np.array(Pr["R"])
    def f(P):
        phi = np.arctan2(P[:, 0] - C[0], P[:, 2] - C[2])         # round the body's long axis, nought on top
        lobes = 0.06 * np.abs(np.cos(3 * phi)) ** 0.7     # round lobes, three over each side from the top
        yb = C[1] - 0.035 + lobes - 0.05 * np.clip(np.abs(phi) - 1.4, 0, None)
        return inside(P[:, 1] - yb, 0.0025) * (P[:, 2] < 0.375)
    return f


def shut_eye(u, v):
    """The open state is the slit: a line curving down, three short lashes from its outer half."""
    c = np.tile(srgb(PALE), (len(u), 1))
    line = -0.05 - 0.45 * (1 - u * u)
    c[(np.abs(v - line) < 0.17) & (np.abs(u) < 0.85)] = srgb(LINE)
    for t in (0.25, 0.55, 0.82):
        x0, y0 = t, -0.05 - 0.45 * (1 - t * t)
        dx, dy = 0.3, -0.45
        s = np.clip(((u - x0) * dx + (v - y0) * dy) / (dx * dx + dy * dy), 0, 1)
        d = np.hypot(u - x0 - s * dx, v - y0 - s * dy)
        c[d < 0.12] = srgb(LINE)
    return c


def fierce_eye(u, v):
    c = np.tile(srgb(PALE), (len(u), 1))
    line = -0.1 - 0.25 * (1 - u * u) + 0.35 * u
    c[(np.abs(v - line) < 0.18) & (np.abs(u) < 0.85)] = srgb(LINE)
    return c


def lay_eyes(k, Pr, state):
    look = fierce_eye if state == "Fierce" else (shut_eye if state == "Open" else
                                                 eye_look(state, shut_eye, srgb(PALE), srgb(LINE), line=0.17))
    p, n = m.on_body(Pr, Pr["eye_dir"])
    k.decal(p, n, Pr["eye_half"], look, eps=0.004, rings=20, segs=56, mirror=True)


def musharna(src, out, Pr=MUSHARNA):
    k = Kit(src, coat="fur")
    C, R = np.array(Pr["C"]), np.array(Pr["R"])
    k.add("body", ell(C, R, deform=m.flat_bottom), LAVENDER, "body")
    k.add("face", ell((0, -0.06, 0.2), (0.14, 0.1, 0.14)), PALE, "head")
    k.paint(face_mask(Pr), PALE, on=["body", "face"])
    # The legs folded under the front, pink feet
    for s, b in ((1, "legR"), (-1, "legL")):
        k.add(f"leg{b[-1]}", ell((0.072 * s, -0.08, 0.075), (0.042, 0.06, 0.045)), LAVENDER, b)
        k.add(f"foot{b[-1]}", ell((0.078 * s, -0.135, 0.055), (0.036, 0.03, 0.032)), FEET, b)
    # The dream smoke: a lumpy, wavy plume from the forehead
    pts, rr = Pr["smoke"], Pr["smoke_r"]
    k.add("smoke", tube(pts, rr), SMOKE, "tail")
    for i in range(1, len(pts) - 1):
        q = np.array(pts[i]); r = rr[i]
        k.add(f"puff{i}", ell(q + np.array([0.012 * (-1) ** i, 0.006, 0.0]), (r * 1.32, r * 1.12, r * 1.08)), SMOKE, "tail")
    end = np.array(pts[-1])
    k.add("curl", ell(end + np.array([0.01, 0.0, -0.012]), (0.03, 0.026, 0.028)), SMOKE, "tail")
    # The pink mark over the eyes at the front
    p, n = m.on_body(Pr, (0, -0.9, 0.42))
    k.decal(p, n, (0.016, 0.012), MARK, eps=0.004, rings=10, segs=40)
    k.build(voxel=0.005, smooth=3, band=0.006)
    k.eye_states(lambda s: lay_eyes(k, Pr, s))
    k.export(out)


if __name__ == "__main__":
    musharna(*args())
