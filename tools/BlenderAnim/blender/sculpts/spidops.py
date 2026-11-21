# Spidops sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/spidops.py -- <the export's Spidops.glb> <out.glb>
#
# Tarountula grown up (tarountula.py's on_ell and way of laying eyes as patches over a white). From the references: a
# guard of a spider, khaki tan and grass green, made of thick round-ended cylinders. A big rounded-box head: khaki on
# its sides and back, a green hood over its top coming down over the face; two black eyes under heavy green lids that
# slope down toward the middle (a sleepy scowl), a white crescent under each; a round black eye like an ear-cup on each
# side of the head; a green mouth hanging under the eyes like a three-lobed moustache. A short khaki neck, a fat green
# thorax, khaki balls at the shoulders, and under it a pale segmented abdomen. Eight thick limbs, tan above and green
# below: the front pair as arms (tan upper arm out to the elbow, a thick green forearm down to the ground before it),
# a long pair at the sides and a pair behind, each a thin tan thigh out to a knee and a thick green shin to the ground,
# and two short tan legs under the abdomen. A triangle of white thread is held between the forearms, the arm on +X
# keeping its top corner at the elbow, as the pictures hold it.
# The pictures raise one arm high: here both forearms come forward and down (the rule), so the web hangs before the
# abdomen and below the face.
# Skeleton: body (0, 0.02, 0.44), head (0, 0, 0.54), leg{s} (+-0.06, -0.02, 0.44), hind{s} (+-0.06, 0.08, 0.44).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, boxy, srgb, inside, unit, eye_look, polygon, args, sibling

tr = sibling("tarountula")
TAN, TAN_D, GREEN, GREEN_D = "#C8A25C", "#A9874A", "#78A83A", "#56842A"
PALE, PALE_D, WEB, INK, WHITE = "#DCD8D2", "#A49C96", "#F2F2F4", "#141416", "#F6F6F4"

HC, HR = np.array([0.0, -0.02, 0.68]), np.array([0.152, 0.13, 0.135])       # the head
THC, THR = np.array([0.0, 0.02, 0.45]), np.array([0.13, 0.118, 0.098])      # the thorax
ABC, ABR = np.array([0.0, 0.03, 0.31]), np.array([0.098, 0.095, 0.1])       # the abdomen
VOXEL = 0.0072

SHOULDER, ELBOW, HAND = np.array([0.13, -0.035, 0.475]), np.array([0.255, -0.12, 0.44]), np.array([0.17, -0.29, 0.07])
LIMBS = [  # bone, hip, knee, foot (on +X): a thin tan thigh and a thick green shin
    ("leg{s}", (0.11, 0.03, 0.43), (0.3, 0.0, 0.26), (0.33, 0.03, 0.035)),
    ("hind{s}", (0.1, 0.09, 0.42), (0.22, 0.22, 0.26), (0.25, 0.29, 0.035)),
]


def head_eye(k, state, side):
    """A black eye under a heavy green lid sloping down toward the middle, a white crescent at its foot: an oval cut
    by the lid's line (the patch's own outline, so the line is crisp), laid by hand on each side."""
    def open_look(u, v):
        c = np.tile(srgb(INK), (len(u), 1))
        c[(u ** 2 + ((v + 0.28) / 0.78) ** 2 > 1) & (v < -0.1)] = srgb(WHITE)
        c[((u + 0.3) / 0.16) ** 2 + ((v - 0.05) / 0.16) ** 2 < 1] = srgb(WHITE)
        return c
    look = eye_look(state, open_look, srgb(GREEN), srgb(INK), line=0.16)
    cut = [(-1.4, -1.4), (1.4, -1.4), (1.4, 0.2 + 0.42 * 1.4), (-1.4, 0.2 - 0.42 * 1.4)]
    if side < 0:
        cut = [(-a, b) for a, b in cut][::-1]
        col = lambda u, v: look(-u, v)
    else:
        col = look
    poly = polygon(cut)
    p, n = tr.on_ell(HC, HR, (0.4 * side, -1, 0.04))
    k.decal(p, n, (0.042, 0.036), col, eps=0.0022, rings=24, segs=64, outline=lambda th: min(1.0, poly(th)))


def spidops(src, out):
    k = Kit(src, coat="shell")
    # ---- the head: a rounded box, khaki, with a green hood over its top and face
    k.add("head", ell(HC, HR, deform=boxy(3.2), sub=5), TAN, "head")
    def hood(P):
        q = P - HC
        front = inside(q[:, 1] + 0.055, 0.004)
        top = inside(-(q[:, 2] - 0.075), 0.004) * inside(np.abs(q[:, 0]) - 0.118, 0.004)
        return np.clip(front + top, 0, 1)
    k.paint(hood, GREEN, on=["head"])
    k.add("neck", ell((0, 0.0, 0.55), (0.07, 0.065, 0.06)), TAN, "head")
    # the mouth: a three-lobed green moustache hanging under the eyes
    k.add("mouthmid", ell((0, -0.15, 0.585), (0.026, 0.022, 0.042)), GREEN_D, "head")
    k.add("mouthlobe", tube([(0.018, -0.148, 0.612), (0.052, -0.15, 0.592), (0.062, -0.146, 0.558)],
                            [0.018, 0.017, 0.016]), GREEN_D, "head", mirror=True)
    # the side eyes: black balls in a khaki rim on each side of the head
    k.add("sideeye", ell((0.152, -0.013, 0.70), (0.03, 0.036, 0.036)), INK, "head", fuse=False, mirror=True)
    k.add("sidering", ell((0.142, -0.013, 0.70), (0.022, 0.048, 0.048)), TAN_D, "head", mirror=True)
    # ---- the thorax, the shoulders, the abdomen with its segments
    k.add("thorax", ell(THC, THR, deform=boxy(2.6), sub=5), GREEN, "body")
    k.add("shoulder", ell(SHOULDER, (0.045, 0.045, 0.045)), TAN, "leg{s}", mirror=True)
    k.add("abdomen", ell(ABC, ABR, sub=5), PALE, "body", surface="skin")
    def segments(P):
        z = P[:, 2] - ABC[2]
        m = np.zeros(len(P))
        for off in (-0.06, -0.025, 0.01):
            m = np.maximum(m, inside(np.abs(z - off - 0.01 * np.abs(P[:, 0]) / 0.1) - 0.003, 0.0015))
        return m * (P[:, 1] < 0.06)
    k.paint(segments, PALE_D, on=["abdomen"])
    # ---- the arms: a tan upper arm out to the elbow, a thick green forearm down to the ground before the body
    k.add("upperarm", tube([SHOULDER, (SHOULDER + ELBOW) / 2 + (0, 0, 0.01), ELBOW], [0.034, 0.033, 0.036]), TAN,
          "leg{s}", mirror=True)
    k.add("forearm", tube([ELBOW, ELBOW + (HAND - ELBOW) * 0.5, HAND + (0, 0, 0.04)], [0.048, 0.05, 0.047]), GREEN,
          "leg{s}", mirror=True)
    # ---- the long legs: thin tan thighs, thick green shins
    for i, (bone, hip, knee, ft) in enumerate(LIMBS):
        hip, knee, ft = np.array(hip), np.array(knee), np.array(ft)
        k.add(f"thigh{i}", tube([hip, (hip + knee) / 2 + (0, 0, 0.02), knee], [0.027, 0.026, 0.03]), TAN, bone,
              mirror=True)
        k.add(f"shin{i}", tube([knee, knee + (ft - knee) * 0.5, ft + (0, 0, 0.04)], [0.036, 0.044, 0.042]), GREEN,
              bone, mirror=True)
    # ---- two short tan legs under the abdomen
    k.add("stub", tube([(0.045, 0.04, 0.24), (0.08, -0.0, 0.13), (0.085, -0.02, 0.045)], [0.036, 0.034, 0.034]), TAN,
          "hind{s}", mirror=True)
    k.add("stubfoot", ell((0.087, -0.035, 0.035), (0.038, 0.05, 0.034)), TAN_D, "hind{s}", mirror=True)
    # ---- the web: a triangle of white thread from the +X elbow to both hands, a strut across it
    eR, hR, hL = ELBOW + (0.0, -0.03, 0.0), HAND + (0.0, -0.04, 0.035), HAND * (-1, 1, 1) + (0.0, -0.04, 0.035)
    for i, (a, b) in enumerate(((eR, hL), (hL, hR), (hR, eR), ((eR + hL) / 2, (hL + hR) / 2 + (0.03, 0, 0)))):
        k.add(f"web{i}", tube([a, (a + b) / 2 + (0, -0.012, 0), b], [0.0125, 0.012, 0.0125]), WEB, "body", fuse=False,
              surface="cloth")
    k.build(voxel=VOXEL, smooth=4, band=0.008, wband=0.05)
    k.eye_states(lambda state: (head_eye(k, state, 1), head_eye(k, state, -1)))
    k.export(out)


if __name__ == "__main__":
    spidops(*args())
