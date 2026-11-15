# Flaaffy sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Mareep's script (mareep.py, imported as sibling("mareep")): the same wool of fused puffs, the same banded horn ears
# made of lathes, the same banded loose tail with a glowing ball, the same dark round eyes with a white glint.
#   blender --background --factory-startup --python sculpts/flaaffy.py -- <the export's Flaaffy.glb> <out.glb>
#
# The design, from the references: a pink sheep standing upright; a pale grey-white cap of wool on its head with two
# spiral curls on its front, and a thick ruff of the same round its neck and shoulders, the chin sunk in it; a pink
# face standing forward and down, long in the muzzle, a small open smile; ears like short horns out to the sides,
# black at the root and the point; short stubby arms out of the ruff; a round pear of a pink belly on short thick legs
# with a white point at each foot's front; a long pink tail banded black, curling out behind and to one side, a
# glowing blue ball at its end.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, args, sibling

m = sibling("mareep")
PINK, WOOL, WOOL_SHADE = "#F4A3BE", "#E6ECF0", "#CBD5DC"
BLACK, BLUE_BALL, WHITE = m.BLACK, "#4FB2F2", "#FAFAFA"
MOUTH, RED = "#3A1A26", "#B23A4A"


def pear(V):
    V = V.copy()
    s = 1 + 0.08 * np.clip(-V[:, 2], 0, 1) - 0.12 * np.clip(V[:, 2], 0, 1)
    V[:, 0] *= s; V[:, 1] *= s
    return V


def curl(k, name, c, d, rad, bone, col):
    """A spiral curl of wool standing on the surface at c, facing d: a fused ridge winding in to the middle."""
    d = unit(*d)
    a = np.cross(d, (0, 0, 1.0)); a /= np.linalg.norm(a)
    b = np.cross(d, a)
    pts = []
    for t in np.linspace(0, 1, 16):
        ang = 0.4 + 3.8 * math.pi * t
        r = rad * (1 - 0.8 * t)
        pts.append(np.array(c) + a * r * math.cos(ang) + b * r * math.sin(ang) + d * (0.006 - 0.004 * t))
    k.add(name, tube(pts, [0.012] * 15 + [0.01]), col, bone)
    return np.array(pts)


def flaaffy(src, out):
    k = Kit(src, coat="fur")

    # The pear of a body, a narrower chest under the ruff
    k.add("belly", ell((0, 0.008, 0.225), (0.13, 0.112, 0.135), deform=pear), PINK, "body")
    k.add("chest", ell((0, -0.005, 0.355), (0.085, 0.075, 0.07)), PINK, "body")

    # Short thick legs, the thighs fused into the belly, a white point at each foot's front
    for s, bone in ((1, "legR"), (-1, "legL")):
        x = np.array([s, 1, 1.0])
        k.add("thigh" + bone[-1], ell(np.array((0.07, -0.01, 0.14)) * x, (0.068, 0.076, 0.08)), PINK, bone)
        k.add("shin" + bone[-1], tube([np.array(p) * x for p in [(0.068, -0.01, 0.12), (0.072, -0.02, 0.04)]],
                                      [0.044, 0.038]), PINK, bone)
        k.add("foot" + bone[-1], ell(np.array((0.073, -0.034, 0.025)) * x, (0.041, 0.056, 0.026)), PINK, bone)
        t = np.array((0.073 * s, -0.088, 0.016))
        k.add("toe" + bone[-1], hull([t + (0, -0.012, -0.004), t + (0.022, 0.012, -0.012), t + (-0.022, 0.012, -0.012),
                                      t + (0.02, 0.012, 0.014), t + (-0.02, 0.012, 0.014), t + (0, 0.0, 0.016),
                                      t + (0.016, 0.03, 0.0), t + (-0.016, 0.03, 0.0)]), WHITE, bone, fuse=False,
              surface="shell")

    # Stubby arms out of the ruff, forward and a little down
    k.add("arm", tube([(0.09, -0.03, 0.345), (0.14, -0.065, 0.32), (0.178, -0.098, 0.295)], [0.04, 0.035, 0.03]),
          PINK, "arm{s}", mirror=True)

    # The ruff: a thick collar of wool round the neck and shoulders, the chin sunk in it
    m.wool(k, "ruff", (0, -0.012, 0.39), (0.115, 0.098, 0.058), "body", col=WOOL, n=30, puff=0.05, sink=0.016, seed=5)
    k.paint(lambda P: inside(P[:, 2] - 0.36, 0.02) * 0.5, WOOL_SHADE, on=["ruff"])

    # The head: a pink face forward and down, a long muzzle, the back of the skull under the cap
    FACE_C, FACE_R = np.array([0, -0.065, 0.5]), np.array([0.06, 0.085, 0.064])
    FACE = ell(FACE_C, FACE_R, rot=(22, 0, 0))
    k.add("face", FACE, PINK, "head")
    SN_C, SN_R = np.array([0, -0.125, 0.468]), np.array([0.043, 0.046, 0.04])
    SNOUT = ell(SN_C, SN_R, rot=(30, 0, 0))
    k.add("snout", SNOUT, PINK, "head")
    k.add("skull", ell((0, -0.02, 0.52), (0.066, 0.062, 0.064)), PINK, "head")
    # The cap of wool and its two curls on the front
    CAP_C, CAP_R = np.array([0, -0.028, 0.582]), np.array([0.088, 0.08, 0.055])
    m.wool(k, "cap", CAP_C, CAP_R, "head", col=WOOL, n=18, puff=0.04, sink=0.015, seed=9,
           skip=lambda d: d[1] < -0.6 and d[2] < 0.3)
    curls = []
    for s in (1, -1):
        dd = unit(0.35 * s, -0.75, 0.6)
        c = m.hit(ell(CAP_C, CAP_R), CAP_C + np.array([0.038 * s, 0, 0.01]) + dd * 0.4, -dd)
        curls.append(curl(k, "curl" + "RL"[s < 0], c + dd * 0.014, dd, 0.03, "head", WOOL))
    def groove(P):
        mm = np.zeros(len(P))
        for Q in curls:
            dist = np.min(np.linalg.norm(P[:, None, :] - Q[None], axis=2), axis=1)
            mm = np.maximum(mm, inside(np.abs(dist - 0.018) - 0.003, 0.003) * inside(dist - 0.04))
        return mm * (P[:, 2] > 0.55) * 0.8
    k.paint(groove, WOOL_SHADE)

    # The ears: short pink horns out to the sides, black at the root and the point
    m.ear_cone(k, "ear", np.array([0.058, -0.035, 0.508]), unit(1.0, 0.15, -0.12), 0.098, 0.031, PINK,
               [(0.0, 0.26, BLACK), (0.64, 1.0, BLACK)], "ear{s}")

    # The tail: long, out behind and curling to +X, banded black, the glowing blue ball at its end
    m.banded_tail(k, "tail", [(0, 0.075, 0.2), (0.03, 0.165, 0.14), (0.1, 0.245, 0.115), (0.18, 0.265, 0.135),
                              (0.222, 0.245, 0.195)], 0.028, 0.02, PINK,
                  [(0.28, 0.4), (0.52, 0.64), (0.78, 0.9)], BLACK, 0.037, BLUE_BALL)

    # The mouth: a small open smile under the muzzle's end
    dm = unit(0, -0.72, -0.68)
    MC = m.hit(SNOUT, SN_C + np.array([0, -0.03, -0.018]) - dm * 0.3, dm)
    CP, CM, CR = srgb(PINK), srgb(MOUTH), srgb(RED)
    def mouth(u, v):
        c = np.tile(CP, (len(u), 1))
        line = v - (0.5 * u * u - 0.05)
        c[(np.abs(line) < 0.24) & (np.abs(u) < 0.92)] = CM
        c[(line < 0) & (np.abs(u) < 0.5) & (v > -0.75) & ((u / 0.5) ** 2 + ((v + 0.1) / 0.62) ** 2 < 1)] = CR
        return c
    k.decal(MC, dm, (0.03, 0.016), mouth, eps=0.004, rings=12, segs=48, up=(0, -0.6, 0.8))

    k.build(voxel=0.0068, smooth=4, band=0.005)

    de = unit(0.6, -0.73, 0.28)
    E = m.hit(FACE, FACE_C + np.array([0, 0, 0.026]) + de * 0.4, -de)
    k.eye_states(m.lay_dark_eyes(k, E, de, (0.018, 0.023), PINK))
    k.export(out)


if __name__ == "__main__":
    flaaffy(*args())
