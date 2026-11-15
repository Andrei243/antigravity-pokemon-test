# Surskit sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/surskit.py -- <the export's Surskit.glb> <out.glb>
# A pond skater: a round blue body wider than it is tall, held high; a yellow cap over its top with a curved yellow
# horn rising from it and bending back; small black eyes with a glint and pink cheeks under them; four long thin
# blue legs arching out and down to points. (No mouth in either picture.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, outward, args

BLUE, YELLOW, PINK = "#6CBDEB", "#F2D67A", "#F08A9C"
INK, WHITE = srgb("#141420"), np.array([1.0, 1.0, 1.0])

B = np.array([0.0, 0.0, 0.2])                       # the body's centre
BR = np.array([0.13, 0.115, 0.086])
CAP = 0.243                                         # the cap's edge, a little lower at the back


def surskit(src, out):
    k = Kit(src, coat="shell")
    k.add("body", ell(B, BR), BLUE, "body")
    # the cap: its lower edge level round the front, dipping a little behind
    k.paint(lambda P: inside(CAP - 0.012 * np.clip(P[:, 1] / BR[1], 0, 1) - P[:, 2], 0.0025), YELLOW)
    # the horn: from the cap's top up and bent back
    k.add("horn", outward(tube([(0, 0.0, 0.275), (0, 0.008, 0.32), (0, 0.03, 0.365), (0, 0.065, 0.392)],
                               [0.03, 0.022, 0.013, 0.005])), YELLOW, "head", fuse=False, surface="shell")
    # four legs: up and out from the body's sides, then arching down to points
    for s, side in ((1, "R"), (-1, "L")):
        for bone, y0, y1, y2, xf in (("leg", -0.03, -0.06, -0.1, 0.265), ("hind", 0.03, 0.06, 0.11, 0.235)):
            pts = [(0.09 * s, y0, 0.2), (0.165 * s, y1, 0.208), (0.215 * s, y1 * 1.15, 0.165),
                   ((xf - 0.012) * s, (y1 + y2) / 2, 0.08), (xf * s, y2, 0.008)]
            k.add(f"{bone}{side}", outward(tube(pts, [0.018, 0.0175, 0.0165, 0.0155, 0.015])), BLUE, f"{bone}{side}",
                  fuse=False, surface="shell")
    def on_body(d):
        return B + d / np.linalg.norm(d / BR)

    # the pink cheeks under the eyes
    CD = unit(0.6, -0.78, -0.05)
    k.decal(on_body(CD), CD, (0.03, 0.019), PINK, eps=0.003, rings=10, segs=40, mirror=True)
    k.build(voxel=0.0045)

    def eye(u, v):
        c = np.tile(INK, (len(u), 1))
        c[((u + 0.25) / 0.32) ** 2 + ((v - 0.3) / 0.32) ** 2 < 1] = WHITE
        return c

    ED = unit(0.36, -0.9, 0.28)

    def eyes(state):
        lid = srgb(BLUE)
        look = eye_look(state, eye, lid, INK, line=0.2)
        k.decal(on_body(ED), ED, (0.025, 0.029), look, eps=0.004, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    surskit(*args())
