# Galarian Mr. Mime sculpted on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/mrmimegalar.py -- "<the export's Mr. Mime-Galar.glb>" <out.glb>
# Mr. Mime's frame (mrmime.py builds as it is read, so its way is copied here: the round head with two wide tufts of
# hair from its top, the round body with big balls at the shoulders, thin limbs, big white gloves), dressed as Mr.
# Rime's line is (mrrime.py: solid slab tufts with a notched lower edge, loose gloves and shoes, its ice and navy).
#
# From the references: a tap dancer. A round pale pink-white face, the head navy behind it and in two round lumps at
# the jaw's corners; two wide navy tufts of hair sweeping up and out from the head's upper sides, three teeth along
# their lower edge; round white eyes ringed in black with small dark pupils, a small navy triangle of a nose, a flat
# rose line of a mouth. A pale ice-blue collar round the neck and a faceted ice crystal on the chest under it; a white
# vest over the round body, its hem four scallops, navy below it and down the legs; big mid-blue balls at the
# shoulders; thin pale pink arms and white mitten gloves; white puffs at the ankles; blue tap shoes with pale cyan
# soles and curled toes. (No red cheeks and no dots on the belly: the pictures show none.)
# The skeleton: body (0, 0, 0.6), arms (+-0.15, 0, 0.73), head (0, 0, 0.78), legs (+-0.09, 0, 0.46).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, hull, srgb, inside, unit, eye_look, polygon, outward, args

NAVY, FACE, WHITE, LIMB = "#2F4775", "#F4E4E6", "#F5F5F7", "#F2CDD2"
ICE, ICEDEEP, BALL, SHOE, SOLE = "#A6E2EE", "#7FCCE4", "#6E99E2", "#7BA2EA", "#A4E8F2"
INK, PUPIL, MOUTH = "#1A1D2C", "#22304E", "#9C4752"
H = np.array([0, -0.01, 0.835])            # the head's centre
HR = np.array([0.162, 0.14, 0.14])
TC, TR = np.array([0, 0.0, 0.55]), np.array([0.2, 0.165, 0.18])     # the round body


def on(c, r, d):
    """The point of an ellipsoid (centre c, radii r) in direction d, and the outward normal there."""
    d = unit(*d)
    p = np.array(c) + d / np.linalg.norm(d / np.array(r))
    n = (p - np.array(c)) / np.array(r) ** 2
    return p, n / np.linalg.norm(n)


def mr_mime_galar(src, out_path):
    k = Kit(src, coat="fur")

    # --- the body: navy, under a white vest whose hem is four scallops ------------------------------------------------
    k.add("torso", ell(TC, TR), NAVY, "body", surface="cloth")
    W = 0.78                                     # a scallop's width, radians round the body

    def vest(P):
        q = P - TC
        phi = np.arctan2(q[:, 0], -q[:, 1])      # 0 at the front
        s = 2 * np.mod(phi / W, 1.0) - 1
        hem = TC[2] - 0.075 + 0.055 * (1 - np.sqrt(np.clip(1 - s * s, 0, 1)))
        body = inside(np.linalg.norm(q / (TR * 1.04), axis=1) - 1, 0.003)
        return inside(hem - P[:, 2], 0.0025) * body * inside(P[:, 2] - 0.7, 0.003)
    k.paint(vest, WHITE)
    # the neck and the pale ice collar round it, the crystal on the chest under it
    k.add("neck", ell((0, 0.0, 0.71), (0.085, 0.075, 0.06)), FACE, "body", surface="skin")
    k.add("collar", outward(ell((0, -0.005, 0.708), (0.135, 0.118, 0.032))), ICE, "body", fuse=False,
          surface="cloth")
    p, n = on(TC, TR, (0, -1, 0.42))
    c = p + n * 0.012
    rr, dd = 0.066, 0.026
    gem = [c + (0, 0, rr * 1.15), c + (rr * 0.85, 0, 0.0), c + (0, 0, -rr * 1.3), c + (-rr * 0.85, 0, 0.0),
           c + (0.012, -dd, 0.006), c + (-0.014, -dd * 0.8, -0.012), c + (0, dd * 1.2, 0)]
    k.add("crystal", hull(gem), ICE, "body", fuse=False, surface="shell")
    k.add("crystalb", hull([c + (0.004, -dd - 0.002, 0.004), c + (rr * 0.8, -0.002, -0.004), c + (0.002, -0.003, -rr * 1.25),
                            c + (0.0, 0.004, 0.0)]), ICEDEEP, "body", fuse=False, surface="shell")

    # --- the head: navy behind the face and in two lumps at the jaw's corners -----------------------------------------
    k.add("head", ell(H, HR), FACE, "head", surface="skin")
    k.add("jaw", ell((0.105, -0.035, 0.75), (0.058, 0.055, 0.055)), NAVY, "head", mirror=True)
    k.add("nape", ell((0, 0.04, 0.8), (0.12, 0.09, 0.09)), NAVY, "head")

    def hair(P):
        q = (P - H) / HR
        # the face's edge: the hair behind the head and coming further forward low on its sides, by the jaw's lumps
        edge = -0.05 - 0.2 * np.clip(q[:, 2], -1, 1)
        back = inside(-q[:, 1] - edge, 0.02)
        return back * inside(np.linalg.norm(q, axis=1) - 1.15, 0.01)
    k.paint(hair, NAVY)

    # two wide tufts of navy hair sweeping up and out from the head's upper sides, three teeth below: solid plates
    TUFT = [(0.0, -0.05), (0.0, 0.075), (0.1, 0.1), (0.2, 0.13), (0.315, 0.165), (0.285, 0.085), (0.255, 0.035),
            (0.22, 0.07), (0.185, 0.0), (0.15, 0.045), (0.11, -0.02), (0.065, 0.025)]
    k.add("tuft", slab(TUFT, (0.1, 0.02, 0.875), unit(1.0, 0.22, 0.05), (0, 0.25, 1.0), 0.042), NAVY, "head",
          fuse=False, mirror=True, surface="fur")

    # the nose, a small navy triangle point up; the mouth a flat rose line
    p, n = on(H, HR, (0, -1, -0.12))
    k.decal(p, n, (0.024, 0.02), NAVY, eps=0.004, rings=8, segs=40, outline=polygon([(-1, -0.7), (1, -0.7), (0, 1)]))
    p, n = on(H, HR, (0, -1, -0.5))
    k.decal(p, n, (0.062, 0.011), MOUTH, eps=0.004, rings=8, segs=48,
            outline=polygon([(-1, 0.6), (-0.9, 1), (0.9, 1), (1, 0.6), (1, -0.6), (0.9, -1), (-0.9, -1), (-1, -0.6)]))

    # --- shoulders, arms, gloves: big blue balls, thin pale arms bent forward and down, white mittens ----------------
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        k.add("shoulder" + side, outward(ell((0.2 * s, -0.005, 0.69), (0.08, 0.078, 0.075))), BALL, "arm" + side,
              fuse=False, surface="cloth")
        sh, el, wr = np.array([0.24 * s, -0.03, 0.66]), np.array([0.275 * s, -0.12, 0.58]), \
            np.array([0.215 * s, -0.215, 0.55])
        k.add("arm" + side, outward(tube([sh, el, wr], [0.02, 0.019, 0.018])), LIMB, "arm" + side, fuse=False,
              surface="skin")
        g = wr + np.array([-0.005 * s, -0.03, -0.015])
        k.add("glove" + side, outward(ell(g, (0.05, 0.044, 0.058), rot=(20, 0, 10 * s))), WHITE, "arm" + side,
              fuse=False, surface="cloth")
        k.add("thumb" + side, outward(ell(g + np.array([-0.04 * s, -0.012, 0.022]), (0.022, 0.022, 0.028))), WHITE,
              "arm" + side, fuse=False, surface="cloth")
        k.add("cuff" + side, outward(ell(wr + np.array([0, -0.006, 0.01]), (0.026, 0.026, 0.02))), WHITE,
              "arm" + side, fuse=False, surface="cloth")

    # --- legs: navy, broad where they leave the body and tapering, white puffs at the ankles, tap shoes ---------------
    k.add("leg", tube([(0.09, 0.0, 0.45), (0.115, -0.015, 0.3), (0.125, -0.02, 0.17), (0.13, -0.02, 0.1)],
                      [0.065, 0.045, 0.032, 0.028]), NAVY, "leg{s}", mirror=True, surface="cloth")
    k.add("puff", outward(ell((0.13, -0.02, 0.09), (0.04, 0.04, 0.032))), WHITE, "leg{s}", fuse=False, mirror=True,
          surface="cloth")
    k.add("shoe", outward(ell((0.135, -0.07, 0.05), (0.055, 0.095, 0.042), rot=(0, 0, -6))), SHOE, "leg{s}",
          fuse=False, mirror=True, surface="shell")
    k.add("sole", outward(ell((0.135, -0.075, 0.018), (0.06, 0.1, 0.018), rot=(0, 0, -6))), SOLE, "leg{s}",
          fuse=False, mirror=True, surface="shell")
    k.add("toe", outward(tube([(0.142, -0.15, 0.03), (0.15, -0.185, 0.048), (0.155, -0.2, 0.078),
                               (0.152, -0.19, 0.098)], [0.03, 0.024, 0.018, 0.015])), SOLE, "leg{s}", fuse=False,
          mirror=True, surface="shell")

    k.build(voxel=0.0072, smooth=5, band=0.006)

    # --- the eyes: round, white, ringed in black, a small dark pupil ------------------------------------------------
    CW, CI, CP, LID = srgb(WHITE), srgb(INK), srgb(PUPIL), srgb(FACE)
    def eye(u, v):
        r = u * u + v * v
        c = np.tile(CI, (len(u), 1))
        c[r < 0.72] = CW
        c[((u + 0.05) / 0.26) ** 2 + ((v + 0.05) / 0.4) ** 2 < 1] = CP
        c[(u - 0.02) ** 2 + (v - 0.14) ** 2 < 0.008] = (1, 1, 1)
        return c
    def eyes(state):
        look = eye_look(state, eye, LID, CI, line=0.14)
        for s in (1, -1):
            p, nrm = on(H, HR, (0.4 * s, -1, 0.3))
            col = look if s == 1 else (lambda u, v, f=look: f(-u, v))
            k.decal(p, nrm, (0.036, 0.044), col, eps=0.003, rings=20, segs=56)
    k.eye_states(eyes)
    k.export(out_path)


if __name__ == "__main__":
    mr_mime_galar(*args())
