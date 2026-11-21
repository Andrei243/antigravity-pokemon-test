# Watchog sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/watchog.py -- <the export's Watchog.glb> <out.glb>
# Patrat grown (patrat.py, whose eyes, band, teeth, toes and tuft it borrows). From the references: a tall dark
# red-brown meerkat standing up, a column of a body heavy at the hips; a yellow vest down its front with a cream throat
# in its V, two yellow bands round the lower body; arms folded in front; white feet; a head like Patrat's, its eyes
# ringed red, yellow and orange in a dark band, puffed cream cheeks, a dark nose and big white teeth; a long thin tail
# up behind, banded yellow near its end, to a white jagged tuft.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, polygon, args, sibling

p_ = sibling("patrat")
s_ = sibling("sentret")

BROWN, CREAM, YELLOW, WHITE = "#8A3A2C", "#E8D6BC", "#F4C82E", "#F4F2EE"
BAND, NOSE, PUPIL = p_.BAND, "#2A1A18", p_.PUPIL
RED, ORANGE, EYEYELLOW = p_.RED, "#F0782A", p_.YELLOW
SHUTLINE = "#C0705A"

HC = np.array([0.0, -0.016, 0.53])
HR = np.array([0.075, 0.07, 0.068])
BODY = [(0, 0.008, 0.1), (0, 0.004, 0.15), (0, 0.0, 0.25), (0, -0.002, 0.35), (0, -0.006, 0.43)]
BRAD = [0.082, 0.096, 0.088, 0.074, 0.058]
TAIL = [(0, 0.04, 0.18), (0, 0.12, 0.17), (0.055, 0.17, 0.27), (0.085, 0.18, 0.42), (0.1, 0.17, 0.54)]
TRAD = [0.021, 0.02, 0.018, 0.016, 0.015]
EYE_RINGS = [(1.0, RED), (0.86, EYEYELLOW), (0.7, ORANGE), (0.54, RED)]


def watchog(src, out):
    k = Kit(src, coat="fur")

    # The column of a body, heavy at the hips, and the neck up into the head
    k.add("body", tube(BODY, BRAD, seg=28), BROWN, "body")
    k.add("hips", ell((0, 0.01, 0.12), (0.1, 0.092, 0.09)), BROWN, "body")
    k.add("neck", tube([(0, -0.006, 0.42), (0, -0.012, 0.5)], [0.052, 0.05], seg=24), BROWN, "head")

    # The head: Patrat's, longer and less capped; cream cheeks and muzzle
    k.add("head", ell(HC, HR), BROWN, "head")
    k.add("brow", ell(HC + (0, -0.026, 0.032), (0.064, 0.048, 0.026)), BROWN, "head")
    k.add("cheek", ell(HC + (0.052, -0.03, -0.038), (0.046, 0.043, 0.04)), CREAM, "head", mirror=True)
    k.add("muzzle", ell(HC + (0, -0.055, -0.036), (0.04, 0.032, 0.032)), CREAM, "head")
    def face(P):
        d = P - HC
        near = np.linalg.norm(d / (HR * 1.5), axis=1) < 1
        return inside(P[:, 2] - (HC[2] - 0.018), 0.002) * (d[:, 1] < -0.012) * near * (P[:, 2] > HC[2] - 0.08)
    k.paint(face, CREAM)
    for s, nm in ((1, "R"), (-1, "L")):
        pts = [(0.044 * s, 0.006, 0.585), (0.054 * s, 0.01, 0.61), (0.06 * s, 0.014, 0.627)]
        k.add("ear" + nm, blade(pts, [0.021, 0.014, 0.004], (0, 1, 0), 0.55), BROWN, "head")
    k.paint(p_.band(HC, HR, HC[2] - 0.012, HC[2] + 0.034, reach=1.75), BAND)

    # The cream throat down the neck into the vest's V, the yellow vest, two yellow bands round the lower body
    def throat(P):
        z = P[:, 2]
        w = np.where(z > 0.4, 0.034, 0.034 * np.clip((z - 0.33) / 0.07, 0, 1))
        return inside(np.abs(P[:, 0]) - w, 0.002) * (P[:, 1] < -0.02) * (z > 0.33) * (z < HC[2] - 0.04)
    def vest(P):
        z = P[:, 2]
        ang = np.abs(np.arctan2(P[:, 0], -(P[:, 1] - 0.0)))
        return inside(z - 0.415, 0.002) * inside(0.29 - z, 0.002) * inside(ang - 1.2, 0.03)
    k.paint(vest, YELLOW, on=["body", "neck"])
    k.paint(throat, CREAM, on=["body", "neck", "head"])
    def rings(P):
        z = P[:, 2]
        m = inside(z - 0.278, 0.002) * inside(0.252 - z, 0.002)
        return np.maximum(m, inside(z - 0.232, 0.002) * inside(0.204 - z, 0.002)) * (np.abs(P[:, 0]) < 0.11)
    k.paint(rings, YELLOW, on=["body", "hips"])

    # Arms folded in front: down and forward from the shoulders, the paws meeting over the vest
    k.add("arm", tube([(0.058, -0.01, 0.41), (0.084, -0.055, 0.375), (0.05, -0.098, 0.352), (0.012, -0.106, 0.35)],
                      [0.026, 0.024, 0.022, 0.021]), BROWN, "arm{s}", mirror=True)
    k.add("paw", ell((0.012, -0.108, 0.35), (0.024, 0.022, 0.022)), BROWN, "arm{s}", mirror=True)

    # Short legs under the hips, white feet with white toes
    k.add("thigh", ell((0.048, -0.012, 0.07), (0.05, 0.05, 0.05)), BROWN, "leg{s}", mirror=True)
    k.add("shin", tube([(0.05, -0.018, 0.05), (0.052, -0.024, 0.026)], [0.03, 0.028]), WHITE, "leg{s}", mirror=True)
    k.add("foot", ell((0.052, -0.032, 0.015), (0.032, 0.044, 0.015)), WHITE, "leg{s}", mirror=True)
    p_.claws(k, (0.052, -0.064, 0.012), (0.015, 0.008, 0.006, 0.022), WHITE, "leg{s}")
    k.paint(lambda P: inside(P[:, 2] - 0.045, 0.002) * (np.abs(P[:, 0]) > 0.015), WHITE, on=["thigh", "shin", "foot"])

    # The long tail up behind, out to the right, banded yellow near its end, a white tuft at the tip
    k.add("tail", tube(TAIL, TRAD, seg=18), BROWN, "tail")
    k.paint(s_.bands(s_.spline(TAIL), [(0.72, 0.8), (0.86, 0.94)]), YELLOW, on=["tail"])
    p_.tuft(k, (0.102, 0.168, 0.552), unit(0.55, 0.83, 0), 0.072, 0.018, WHITE, "tail", count=6, spread=150)

    # The dark nose and the big teeth
    NOSE_T = polygon([(-1, 0.7), (1, 0.7), (0, -1)])
    k.decal(HC + (0, -0.083, -0.018), unit(0, -1, 0.3), (0.014, 0.01), NOSE, eps=0.003, outline=NOSE_T, rings=6,
            segs=24)
    p_.teeth(k, HC + (0, -0.083, -0.043), 0.013, 0.023, 0.007, "head")

    # The ringed eyes in the band, cut slanting by the cap
    de = unit(0.5, -0.85, 0.16)
    E = HC + de / np.linalg.norm(de / HR)
    look = p_.ring_eye(EYE_RINGS, PUPIL, "#FFFFFF", pupil_half=(0.18, 0.3))
    cut = p_.cut_eye(0.62, 0.3)
    def eyes(state):
        p_.lay_eyes(k, state, E, de, (0.026, 0.026), look, BAND, SHUTLINE, cut)

    k.build(voxel=0.0045)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    watchog(*args())
