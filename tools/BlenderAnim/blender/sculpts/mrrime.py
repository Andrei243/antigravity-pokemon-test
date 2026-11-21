# Mr. Rime sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/mrrime.py -- "<the export's Mr. Rime.glb>" <out.glb>
# Mr. Mime's Galarian line grown old (mrmime.py's way with the tufts and the gloves): a tap-dancing gentleman. Its head
# is a slate-navy hat whose sides come down as a hood round a pale pink face, two great wing-like tufts sweeping up and
# out from the hat's sides with a jagged lower edge; round yellow eyes set close, a big curled navy moustache; a ruff
# of navy knobs at the neck with one knob on its right. The body is a round slate-navy coat, the largest mass, and a
# face of its own: two yellow ovals, a big domed red nose and a white bib below for a mouth, a little white fang at the
# top. Thin pale blue arms, big white gloves held forward and down, the right hand on a zigzag cane of ice reaching the
# ground; puffy blue trousers, navy cuffs, big rounded pale blue tap shoes.
# The skeleton: body (0, 0, 0.38), arms (+-0.12, 0, 0.44), head (0, -0.01, 0.51), legs (+-0.06, 0, 0.24).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, slab, hull, srgb, inside, smoothstep, unit, eye_look, polygon, outward, boxy,
                       args)

NAVY, NAVYDARK, FACE, WHITE = "#4A4775", "#3A3762", "#F1D9DE", "#F4F3F1"
RED, YELLOW, PANTS, SHOE, ARM, ICE = "#B8395C", "#E3B030", "#3E6FC6", "#8EC7EE", "#6E9EE6", "#A9DAF4"
EYEPALE, EYERING, INK = "#F8F0C4", "#D7A126", "#1F1C2C"
H = np.array([0, -0.05, 0.615])          # the face's centre
HF = (0.105, 0.085, 0.1)                  # the face's radii
TORSO_C, TORSO_R = np.array([0, 0.0, 0.385]), np.array([0.235, 0.16, 0.145])


def on(c, r, d):
    """The point of an ellipsoid (centre c, radii r) in direction d, and the outward normal there."""
    d = unit(*d)
    p = np.array(c) + d / np.linalg.norm(d / np.array(r))
    n = (p - np.array(c)) / np.array(r) ** 2
    return p, n / np.linalg.norm(n)


def flat_on(p, n, a, b, t, up=(0, 0, 1.0)):
    """A flat oval lying on a surface at p facing n: half sizes a across, b up, t thick."""
    n = np.array(n, float); n /= np.linalg.norm(n)
    up = np.array(up, float); up = up - n * (up @ n); up /= np.linalg.norm(up)
    side = np.cross(up, n)
    V, F = ell((0, 0, 0), (a, t, b), sub=3)
    return outward((V @ np.stack([side, n, up], axis=1).T + p + n * t * 0.4, F))


def mrrime(src, out):
    k = Kit(src, coat="fur")

    # --- the coat: round, flaring a little at its hem, the body's largest mass --------------------------------------
    def coat(V):
        V = V.copy()
        s = 1 + 0.08 * np.clip(-V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("torso", ell(TORSO_C, TORSO_R, deform=coat), NAVY, "body", surface="cloth")
    # the white bib below the nose, wider at the bottom: the belly-face's mouth
    def bib(P):
        z = P[:, 2]
        half = 0.075 + 0.6 * np.clip(0.345 - z, 0, 1)
        return inside(np.abs(P[:, 0]) - half, 0.003) * inside(z - 0.345, 0.003) * inside(P[:, 1] + 0.05, 0.003)
    k.paint(bib, WHITE, on=["torso"])

    # --- puffy trousers, navy cuffs, rounded tap shoes ---------------------------------------------------------------
    k.add("seat", ell((0, 0.01, 0.255), (0.17, 0.12, 0.07)), PANTS, "body", surface="cloth")
    k.add("trouser", ell((0.085, -0.005, 0.17), (0.078, 0.075, 0.09)), PANTS, "leg{s}", mirror=True, surface="cloth")
    k.add("cuff", ell((0.08, -0.005, 0.085), (0.05, 0.05, 0.032)), NAVYDARK, "leg{s}", mirror=True, surface="cloth")
    k.add("shoe", outward(ell((0.085, -0.045, 0.042), (0.066, 0.1, 0.045), rot=(0, 0, -6))), SHOE, "leg{s}",
          fuse=False, mirror=True, surface="shell")

    # --- the belly's face: a big domed red nose, two yellow ovals, a little white fang under the ruff ---------------
    p, nrm = on(TORSO_C, TORSO_R, (0, -1, -0.08))
    k.add("nose", outward(ell(p + nrm * 0.012, (0.072, 0.048, 0.052))), RED, "body", fuse=False, surface="shell")
    for s in (1, -1):
        p, nrm = on(TORSO_C, TORSO_R, (0.95 * s, -1, -0.02))
        k.add("button" + ("R" if s > 0 else "L"), flat_on(p, nrm, 0.02, 0.032, 0.009), YELLOW, "body", fuse=False,
              surface="shell")
    p, nrm = on(TORSO_C, TORSO_R, (0, -1, 0.55))
    k.add("fang", hull([p + np.array([-0.022, -0.004, 0.005]), p + np.array([0.022, -0.004, 0.005]),
                        p + np.array([0, -0.012, -0.04]), p + np.array([-0.022, 0.012, 0.0]),
                        p + np.array([0.022, 0.012, 0.0]), p + np.array([0, 0.006, -0.04])]), WHITE, "body",
          fuse=False)

    # --- the ruff: navy knobs round the neck, one more on its right ---------------------------------------------------
    for i in range(12):
        a = 2 * math.pi * i / 12
        k.add(f"ruff{i}", ell((0.115 * math.sin(a), 0.095 * -math.cos(a) + 0.015, 0.515), (0.046, 0.046, 0.036)),
              NAVY, "head", surface="cloth")
    k.add("knob", outward(ell((0.135, -0.09, 0.52), (0.03, 0.03, 0.03))), NAVYDARK, "head", fuse=False,
          surface="cloth")

    # --- the head: a hat that is the head, its sides a hood round the pink face --------------------------------------
    k.add("face", ell(H, HF), FACE, "head", surface="skin")
    k.add("hood", ell((0, 0.045, 0.625), (0.145, 0.105, 0.11)), NAVY, "head", surface="cloth")
    k.add("hoodside", ell((0.11, 0.03, 0.6), (0.05, 0.07, 0.085)), NAVY, "head", mirror=True, surface="cloth")
    k.add("hat", ell((0, 0.012, 0.745), (0.155, 0.14, 0.1), deform=boxy(2.4)), NAVY, "head", surface="cloth")
    k.add("brim", ell((0, -0.012, 0.695), (0.178, 0.165, 0.034)), NAVYDARK, "head", surface="cloth")

    # two great tufts from the hat's sides, swept up, out and back, a notched lower edge: solid plates
    TUFT = [(0.0, -0.035), (0.0, 0.075), (0.09, 0.1), (0.18, 0.12), (0.27, 0.15), (0.235, 0.075), (0.215, 0.035),
            (0.175, 0.055), (0.14, -0.02), (0.1, 0.02), (0.04, -0.02)]
    ex = unit(1.0, 0.18, 0.12)
    k.add("tuft", slab(TUFT, (0.12, 0.035, 0.7), ex, (0, 0.18, 1.0), 0.042), NAVY, "head", fuse=False, mirror=True,
          surface="cloth")

    # --- arms: thin pale blue, bent at the elbow, big white gloves forward and down ---------------------------------
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        sh, el, wr = np.array([0.19 * s, -0.01, 0.46]), np.array([0.262 * s, -0.08, 0.4]), \
            np.array([0.225 * s, -0.175, 0.355])
        k.add("arm" + side, outward(tube([sh, el, wr], [0.019, 0.018, 0.018])), ARM, "arm" + side, fuse=False,
              surface="skin")
        g = wr + np.array([-0.005 * s, -0.025, -0.02])
        k.add("glove" + side, outward(ell(g, (0.055, 0.048, 0.058), rot=(15, 0, 0))), WHITE, "arm" + side,
              fuse=False, surface="cloth")
        k.add("thumb" + side, outward(ell(g + np.array([-0.045 * s, -0.014, 0.026]), (0.024, 0.024, 0.03))), WHITE,
              "arm" + side, fuse=False, surface="cloth")
        k.add("wristband" + side, outward(ell(wr + np.array([0, -0.004, 0.006]), (0.028, 0.028, 0.02))), WHITE,
              "arm" + side, fuse=False, surface="cloth")
    # the cane of ice in the right glove, zigzagging down to the ground in front
    pts = [(0.205, -0.2, 0.42), (0.215, -0.205, 0.345), (0.26, -0.22, 0.28), (0.225, -0.24, 0.21), (0.27, -0.26, 0.145),
           (0.235, -0.28, 0.08), (0.27, -0.295, 0.02)]
    k.add("cane", outward(tube(pts, [0.017] * 7, per=3)), ICE, "armR", fuse=False, surface="shell")
    k.add("hook", outward(tube([(0.205, -0.2, 0.42), (0.2, -0.205, 0.455), (0.17, -0.21, 0.465),
                                (0.155, -0.21, 0.445)], [0.017, 0.017, 0.016, 0.015])), ICE, "armR", fuse=False,
          surface="shell")

    # --- the moustache: one big curled navy patch under the eyes -----------------------------------------------------
    MOU = [(-1, 0.25), (-0.92, 0.95), (-0.7, 0.55), (-0.35, 0.35), (0, 0.6), (0.35, 0.35), (0.7, 0.55), (0.92, 0.95),
           (1, 0.25), (0.75, -0.45), (0.35, -0.6), (0, -0.25), (-0.35, -0.6), (-0.75, -0.45)]
    p, nrm = on(H, HF, (0, -1, -0.34))
    k.decal(p, nrm, (0.08, 0.036), NAVYDARK, eps=0.004, outline=polygon(MOU), rings=12, segs=96)

    k.build(voxel=0.0072, smooth=6, band=0.006)

    # --- the eyes: round, close together, a bold yellow ring round a pale disc, a dark pupil -------------------------
    CP, CR, CI, LID = srgb(EYEPALE), srgb(EYERING), srgb(INK), srgb(FACE)
    def eye(u, v):
        r = u * u + v * v
        c = np.tile(CR, (len(u), 1))
        c[r < 0.5] = CP
        c[r < 0.11] = CI
        c[(u - 0.12) ** 2 + (v - 0.14) ** 2 < 0.012] = (1, 1, 1)
        return c
    de = {s: unit(0.42 * s, -1, 0.12) for s in (1, -1)}
    def eyes(state):
        look = eye_look(state, eye, LID, CI, line=0.14)
        for s in (1, -1):
            p, nrm = on(H, HF, (0.42 * s, -1, 0.14))
            col = look if s == 1 else (lambda u, v, f=look: f(-u, v))
            k.decal(p, nrm, (0.035, 0.035), col, eps=0.003, rings=20, segs=56)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    mrrime(*args())
