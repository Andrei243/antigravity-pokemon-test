# Furret sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/furret.py -- <the export's Furret.glb> <out.glb>
# Sentret grown (sentret.py, whose bands, eyes and mouth it borrows). From the references: a long cream ferret standing
# up, its body heavy below and narrowing up a long neck to a small head carried a little forward; a brown cap over the
# head between small pointed ears running down the back of the neck as a stripe; three brown rings round the body
# below the short arms, which end in brown paws; big round black eyes, a small open mouth, three brown strokes on each
# cheek; little cream feet; a great bushy tail out behind and up, ringed brown, with a spiky cream tuft at its end.
# The pictures overrule the brief here: Furret stands upright on two feet (as the skeleton does), not on four.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, args, sibling

s_ = sibling("sentret")

CREAM, BROWN, PAW, FOOT = "#EEDCBC", "#946C58", "#8A624E", "#F4E6CC"
WHITE, PUPIL, INK, MOUTH, TONGUE, NOSE = s_.WHITE, s_.PUPIL, s_.INK, "#8E2E3C", "#EC8090", "#4A3028"

HC = np.array([0.0, -0.05, 0.69])       # the head's middle
HR = np.array([0.108, 0.1, 0.097])
BODY = [(0, -0.045, 0.14), (0, -0.06, 0.22), (0, -0.04, 0.32), (0, 0.0, 0.42), (0, 0.025, 0.52), (0, 0.01, 0.61)]
BRAD = [0.125, 0.13, 0.118, 0.098, 0.08, 0.074]
TAIL = [(0, 0.07, 0.12), (0.05, 0.19, 0.12), (0.11, 0.29, 0.19), (0.15, 0.335, 0.3), (0.165, 0.34, 0.41)]
TRAD = [0.07, 0.088, 0.09, 0.078, 0.05]


def furret(src, out):
    k = Kit(src, coat="fur")

    # The long body standing up: heavy below, up a long neck into the head
    k.add("body", tube(BODY[:5], BRAD[:5], seg=24), CREAM, "body")
    k.add("belly", ell((0, -0.05, 0.13), (0.135, 0.13, 0.125)), CREAM, "body")
    k.add("neck", tube(BODY[3:], BRAD[3:], seg=24), CREAM, "head")
    k.add("head", ell(HC, HR), CREAM, "head")
    k.add("snout", ell(HC + (0, -0.07, -0.03), (0.06, 0.05, 0.05)), CREAM, "head")

    # Small pointed ears at the head's top corners
    for s, nm in ((1, "R"), (-1, "L")):
        pts = [(0.055 * s, -0.03, 0.735), (0.078 * s, -0.025, 0.79), (0.092 * s, -0.02, 0.83)]
        k.add("ear" + nm, blade(pts, [0.034, 0.024, 0.006], (0, 1, 0), 0.55), BROWN, "head")

    # Short arms held in front, brown paws
    k.add("arm", tube([(0.07, -0.04, 0.43), (0.09, -0.1, 0.405), (0.096, -0.135, 0.38)], [0.033, 0.031, 0.029]),
          CREAM, "arm{s}", mirror=True)
    k.add("paw", ell((0.098, -0.15, 0.37), (0.031, 0.031, 0.033)), PAW, "arm{s}", mirror=True)

    # Little cream feet under the heavy lower body
    k.add("foot", ell((0.065, -0.085, 0.026), (0.04, 0.052, 0.027)), FOOT, "leg{s}", mirror=True)

    # The bushy tail, out behind and up to the model's right, a spiky tuft at its end
    k.add("tail", tube(TAIL, TRAD, seg=24), CREAM, "tail")
    tip = np.array(TAIL[-1])
    for i, d in enumerate([(0.0, 0.1, 1.0), (0.5, -0.1, 0.8), (-0.45, 0.25, 0.85), (0.15, 0.6, 0.6)]):
        d = unit(*d)
        k.add(f"tailtuft{i}", blade([tip - d * 0.02, tip + d * 0.04, tip + d * 0.085], [0.034, 0.022, 0.005],
                                    (1, 0, 0), 0.6), CREAM, "tail")
    k.paint(s_.bands(s_.spline(TAIL), [(0.06, 0.2), (0.33, 0.47), (0.6, 0.74), (0.86, 0.97)]), BROWN, on=["tail"])

    # The brown cap over the head, down the back of the neck as a stripe
    def cap(P):
        line = -0.03 - 1.4 * (P[:, 2] - 0.705)
        return inside(line - P[:, 1], 0.003) * (P[:, 2] > 0.6)
    k.paint(cap, BROWN, on=["head", "neck", "snout"])
    def stripe(P):
        a = np.abs(np.arctan2(P[:, 0], P[:, 1] - 0.0))
        return inside(a - 0.5, 0.003) * (P[:, 2] > 0.38) * (P[:, 2] < 0.7)
    k.paint(stripe, BROWN, on=["body", "neck"])

    # Three brown rings round the body below the arms, the lower two leaving the front cream
    def rings(P):
        z = P[:, 2]
        front = np.abs(np.arctan2(P[:, 0], -(P[:, 1] + 0.03)))
        m = inside(z - 0.36, 0.003) * inside(0.315 - z, 0.003)
        for a, b in ((0.205, 0.245), (0.09, 0.13)):
            m = np.maximum(m, inside(z - b, 0.003) * inside(a - z, 0.003) * inside(0.8 - front, 0.004))
        return m
    k.paint(rings, BROWN, on=["body", "belly"])

    # Three brown strokes on each cheek
    cheek = unit(0.86, -0.42, -0.25)
    C = HC + cheek / np.linalg.norm(cheek / HR)
    for j, dz in enumerate((0.017, 0.0, -0.017)):
        k.decal(C + (0, 0, dz), cheek, (0.017, 0.0055), BROWN, up=unit(0, 0.25, 1), eps=0.004, rings=8, segs=36,
                mirror=True)

    # The nose's tip and the small open mouth
    k.add("nose", ell(HC + (0, -0.118, -0.005), (0.013, 0.009, 0.01)), NOSE, "head", fuse=False, surface="skin")
    CM, CT = srgb(MOUTH), srgb(TONGUE)
    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        c[(u / 0.72) ** 2 + ((v + 0.6) / 0.55) ** 2 < 1] = CT
        return c
    k.decal(HC + (0, -0.11, -0.055), unit(0, -1, -0.35), (0.021, 0.019), mouth, eps=0.004, rings=12, segs=48)

    # Big round black eyes with a glint, high on the face
    de = unit(0.44, -0.8, 0.4)
    E = HC + de / np.linalg.norm(de / HR)
    look = s_.black_eye(PUPIL, WHITE)
    def eyes(state):
        s_.lay_eyes(k, state, E, de, (0.025, 0.029), look, CREAM, INK)

    k.build(voxel=0.0065)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    furret(*args())
