# Gothorita sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gothorita.py -- <the export's Gothorita.glb> <out.glb>
# Grown from gothita.py: the same lilac, black and white, the same bows, the same blue eyes and red pout.
#
# The design, from the references: a slim girl in black. A round black head of hair, the lilac face showing at its
# front, framed by the hair at its sides and cut across the top by a fringe coming down to a point between the eyes;
# the eyes half shut under heavy lids, the blue irises glancing aside; a small red pout. Two great round black buns at
# the head's upper sides, as big as the head, each tied on top with a white bow and hanging a black teardrop under it.
# A white bow at the collar; a slim black body; a big white bow at the waist over a short flared black skirt; thin
# black arms held out and down, pointed at their ends; long black legs drawn together, tapering to small feet.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, srgb, inside, smoothstep, unit, eye_look, args, sibling

g = sibling("gothita")
LILAC, BLACK, WHITE = g.LILAC, g.BLACK, g.WHITE

# --- the frame (the skeleton's joints: body (0, 0, 0.28), head (0, -0.01, 0.36), arms (+-0.035, 0, 0.34),
#     legs (+-0.025, 0, 0.21)) ---------------------------------------------------------------------------------------
HC = np.array([0.0, -0.006, 0.432])      # the head's middle
HR = np.array([0.1, 0.09, 0.1])
BUN = np.array([0.152, 0.022, 0.472])    # the +X bun's middle
BR = 0.076


def heavy_eye():
    """The line's eye grown up: the same white, blue iris, pupil and glint, half shut under a heavy straight lid with
    a dark lash line, the iris glancing outward; u outward, v up."""
    W, I, D, P, K = srgb(g.EYEWHITE), srgb(g.IRIS), srgb(g.IRISDEEP), srgb(g.PUPIL), srgb(g.INKH)
    def eye(u, v, lid):
        c = np.tile(lid, (len(u), 1))
        o = ((u + 0.02) / 0.9) ** 2 + ((v + 0.05) / 0.92) ** 2
        top = 0.16 - 0.06 * u                                # the lid: straight, drooping a little outward
        c[o < 1] = K
        c[o < 0.72] = W
        ir = ((u - 0.12) / 0.52) ** 2 + ((v + 0.14) / 0.66) ** 2
        c[(o < 0.72) & (ir < 1)] = D
        c[(o < 0.72) & (ir < 0.62)] = I
        c[((u - 0.12) / 0.24) ** 2 + ((v + 0.14) / 0.32) ** 2 < 1] = P
        c[((u + 0.0) / 0.13) ** 2 + ((v + 0.0) / 0.13) ** 2 < 1] = W
        c[(v > top) & (o < 1.15)] = lid
        c[(np.abs(v - top) < 0.15) & (o < 1.12)] = K
        # a short flick of lash at the outer corner
        c[(u > 0.7) & (u < 0.98) & (np.abs(v - (top + 0.5 * (u - 0.7))) < 0.13)] = K
        return c
    return eye


def gothorita(src, out):
    k = Kit(src, coat="fur")

    # --- legs: long, drawn together, the thighs full under the skirt and tapering to the ankles; small feet
    k.add("hips", ell((0, 0.002, 0.215), (0.05, 0.04, 0.04)), BLACK, "body")
    k.add("leg", tube([(0.024, 0.0, 0.215), (0.02, -0.002, 0.14), (0.014, -0.004, 0.07), (0.011, -0.006, 0.03)],
                      [0.028, 0.02, 0.014, 0.011]), BLACK, "leg{s}", mirror=True)
    k.add("foot", ell((0.012, -0.014, 0.013), (0.016, 0.026, 0.013)), BLACK, "leg{s}", mirror=True)

    # --- a slim black body and a short flared skirt (loose, its hem crisp)
    k.add("waist", ell((0, 0.002, 0.255), (0.03, 0.027, 0.04)), BLACK, "body")
    k.add("chest", ell((0, 0.002, 0.305), (0.036, 0.03, 0.045)), BLACK, "body")
    k.add("neck", ell((0, -0.004, 0.345), (0.024, 0.022, 0.03)), BLACK, "head")
    k.add("skirt", lathe((0, 0.002, 0), (0, 0, 1), [(0, 0.188), (0.072, 0.186), (0.088, 0.196), (0.084, 0.206),
                                                    (0.058, 0.228), (0.032, 0.25), (0, 0.255)], seg=56),
          BLACK, "body", fuse=False)

    # --- arms: thin, held out to the sides and down, a little forward, pointed at their ends
    k.add("arm", tube([(0.03, 0.0, 0.33), (0.066, -0.02, 0.305), (0.098, -0.048, 0.272), (0.118, -0.068, 0.25)],
                      [0.012, 0.011, 0.01, 0.0095]), BLACK, "arm{s}", mirror=True)
    k.add("hand", tube([(0.115, -0.066, 0.252), (0.132, -0.084, 0.238), (0.144, -0.098, 0.226)],
                       [0.014, 0.012, 0.005]), BLACK, "arm{s}", mirror=True)

    # --- the head: a round black head of hair, the lilac face at its front
    k.add("head", ell(HC, HR, sub=5), BLACK, "head")
    k.add("face", ell(HC + np.array([0, -0.012, -0.012]), (0.088, 0.08, 0.088)), BLACK, "head")
    def face(P):
        Q = P - HC
        th = np.arctan2(Q[:, 0], -Q[:, 1])
        a = th * 0.095                                    # round the head, in metres
        z = Q[:, 2]
        oval = (a / 0.072) ** 2 + ((z + 0.024) / 0.074) ** 2 - 1
        top = 0.004 + 0.75 * np.abs(a)                     # the fringe: a point down between the eyes
        top = np.minimum(top, 0.05 - 0.6 * np.clip(np.abs(a) - 0.05, 0, 1))
        return inside(oval * 0.07, 0.003) * inside(z - top, 0.003) * (Q[:, 1] < 0)
    k.paint(face, LILAC)

    # the buns: great round balls at the head's upper sides, a little back, each with a teardrop hanging under it
    k.add("bunroot", tube([HC + np.array([0.06, 0.02, 0.03]), BUN - np.array([0.03, 0, 0.005])], [0.035, 0.04]),
          BLACK, "head", mirror=True)
    k.add("bun", ell(BUN, (BR, BR * 0.95, BR)), BLACK, "head", mirror=True)
    k.add("pendstalk", tube([BUN + np.array([0.004, 0, -BR + 0.01]), BUN + np.array([0.006, -0.004, -BR - 0.038])],
                            [0.012, 0.011]), BLACK, "head", mirror=True)
    def drop(V):
        V = V.copy()
        t = (1 - V[:, 2]) / 2                             # narrow at the top
        V[:, 0] *= 0.5 + 0.5 * np.sqrt(t); V[:, 1] *= 0.5 + 0.5 * np.sqrt(t)
        return V
    k.add("pendant", ell(BUN + np.array([0.006, -0.004, -BR - 0.062]), (0.026, 0.024, 0.034), deform=drop), BLACK,
          "head", mirror=True)

    # the pout
    lp, ln = g.on_ell(HC, HR, (0, -1, -0.62))
    k.add("lipbump", ell(lp - ln * 0.003, (0.014, 0.009, 0.011)), g.LIPS, "head")
    g.lips(k, lp + ln * 0.005, ln, (0.014, 0.012))

    # --- the bows: one atop each bun, one at the collar, a big one at the waist
    bp, bn = g.on_ell(BUN, (BR, BR, BR), (0.25, -0.15, 1))
    g.bow(k, "bunbow", bp + bn * 0.012, (1, -0.1, -0.25), (0.15, -1, 0.5), 0.07, 0.042, 0.02, "head", mirror=True)
    g.bow(k, "neckbow", (0, -0.034, 0.332), (1, 0, 0.06), (0, -1, 0.25), 0.058, 0.032, 0.017, "body")
    g.bow(k, "waistbow", (0, -0.04, 0.238), (1, 0, -0.05), (0, -1, 0.1), 0.066, 0.038, 0.02, "body")

    k.build(voxel=0.005, smooth=4, band=0.006)

    # --- the eyes: half shut under heavy lids, the irises glancing aside
    LID, INK = srgb(LILAC), srgb(g.INKH)
    eye = heavy_eye()
    open_look = lambda u, v: eye(u, v, LID)
    ep, en = g.on_ell(HC, HR, (0.4, -0.92, -0.05))
    def lay(state):
        k.decal(ep, en, (0.029, 0.03), eye_look(state, open_look, LID, INK, line=0.15), eps=0.003, rings=20,
                segs=56, mirror=True)

    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    gothorita(*args())
