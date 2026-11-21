# Dragapult sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dragapult.py -- <the export's Dragapult.glb> <out.glb>
# From the references: a ghost dragon like a stealth flier, hovering, its head the largest mass: a long dark-navy delta
# held level, pointed in front, a ridge down its middle; narrow slanted yellow eyes edged red low on its sides; out of
# the back of the head on each side a horn like a launch tube: a pink triangular tube open in front, a dark window in
# its face and a Dreepy hanging out of it, its blade sweeping out and back to a point, pink above and dark below; a
# dark neck and chest, a cream throat with red chevrons; thin dark arms held before the chest, three long pink claws
# each; the body darkening into teal below, small teal legs with three toes; a great thick tail sweeping down, back and
# round to the side, fading to white toward its end and finishing in a thin curl.
# The two Dreepy are dreepy.py's (sibling("dreepy")), their eyes open in every state.
# Bones: root 0; body (0, 0, 0.501); tail (0, 0.02, 0.381); armR (0.06, -0.02, 0.561); head (0, -0.02, 0.641);
# legR (0.04, 0, 0.391).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, eye_look, polygon, args, sibling

dr = sibling("dreepy")

NAVY, TEAL, CREAM = "#2D4459", "#5FCFCB", "#EEE2A2"
PINK, RED, WINDOW = "#EA5274", "#C2344E", "#1A1E28"
EYE, PUPIL = "#F2D21E", "#14161A"
PALE, WHITE = "#BDEDEA", "#F1FAF9"

HEAD_C, HEAD_R = np.array([0.0, -0.06, 0.758]), np.array([0.12, 0.195, 0.08])
GROW_FROM, GROW = np.array([0.0, -0.05, 0.735]), 1.3      # the crown was drawn on a head a third smaller
TAIL = [(0.0, 0.05, 0.4), (0.02, 0.14, 0.27), (0.09, 0.21, 0.17), (0.19, 0.22, 0.12), (0.28, 0.15, 0.12),
        (0.33, 0.05, 0.17), (0.34, -0.02, 0.25), (0.32, -0.03, 0.32), (0.295, 0.0, 0.36)]
TAIL_R = [0.08, 0.072, 0.064, 0.056, 0.048, 0.04, 0.032, 0.021, 0.0145]
TUBE_X = 0.17                                   # where each launch tube stands out from the head


def on_ell(c, r, d):
    d = unit(*d)
    s = 1 / math.sqrt(((d / np.array(r)) ** 2).sum())
    p = np.array(c) + d * s
    n = (p - c) / np.array(r) ** 2
    return p, n / np.linalg.norm(n)


def snout(V):
    V = V.copy()
    t = np.clip(-V[:, 1], 0, 1)
    s = 1 - 0.55 * t ** 1.6
    V[:, 0] *= s
    V[:, 2] = V[:, 2] * s - 0.15 * t ** 2
    return V


def dragapult(src, out):
    k = Kit(src, coat="scales")

    # --- the body: a dark chest darkening down into a teal belly ----------------------------------------------------
    k.add("chest", ell((0, -0.015, 0.565), (0.075, 0.07, 0.075)), NAVY, "body")
    k.add("belly", ell((0, 0.01, 0.45), (0.1, 0.092, 0.1)), TEAL, "body")
    k.add("neck", tube([(0, -0.03, 0.58), (0, -0.03, 0.65), (0, -0.01, 0.705)], [0.052, 0.048, 0.046]), NAVY, "head")

    # the cream throat and its red chevrons
    k.paint(lambda Q: inside(np.abs(Q[:, 0]) - 0.04 * (1 - 0.35 * np.clip((Q[:, 2] - 0.53) / 0.15, 0, 1)), 0.004)
            * inside(Q[:, 2] - 0.685, 0.004) * inside(0.515 - Q[:, 2], 0.01) * (Q[:, 1] < -0.04), CREAM,
            on=["neck", "chest"])
    VEE = polygon([(0, -0.3), (1, 0.75), (0.72, 0.95), (0, 0.18), (-0.72, 0.95), (-1, 0.75)])
    for z in (0.63, 0.585):
        p, n = on_ell((0, -0.03, z), (0.05, 0.052, 0.2), (0, -1, 0.1))
        k.decal(p, n, (0.036, 0.022), RED, eps=0.003, outline=VEE, rings=10, segs=96)

    # the tail: thick to near its end, down, back and round to the side, fading to white
    k.add("tail", tube(TAIL, TAIL_R), TEAL, "tail")
    end = np.array(TAIL[-1])
    k.paint(lambda Q: 1 - smoothstep(0.06, 0.3, np.linalg.norm(Q - end, axis=1)), PALE, on=["tail"])
    k.paint(lambda Q: 1 - smoothstep(0.03, 0.09, np.linalg.norm(Q - end, axis=1)), WHITE, on=["tail"])

    # --- the legs: small teal thighs in the belly, three toes forward and down ---------------------------------------
    k.add("thigh", ell((0.068, -0.02, 0.405), (0.042, 0.045, 0.045)), TEAL, "leg{s}", mirror=True)
    k.add("shin", tube([(0.07, -0.03, 0.39), (0.088, -0.07, 0.345)], [0.03, 0.022]), TEAL, "leg{s}", mirror=True)
    for i, dx in enumerate((-0.016, 0.0, 0.016)):
        b = np.array([0.088 + dx, -0.085, 0.34])
        k.add(f"toe{i}", hull([b + (-0.008, 0, 0.008), b + (0.008, 0, 0.008), b + (0, 0.01, -0.006),
                               b + (dx * 0.6, -0.035, -0.02)]), TEAL, "leg{s}", fuse=False, mirror=True)

    # --- the arms: thin, dark, held before the chest; three long pink claws ------------------------------------------
    S, M, HD = np.array([0.06, -0.02, 0.56]), np.array([0.1, -0.07, 0.52]), np.array([0.08, -0.12, 0.48])
    k.add("arm", tube([S, M, HD], [0.019, 0.017, 0.016]), NAVY, "arm{s}", mirror=True)
    k.add("hand", ell(HD + (0, -0.006, -0.006), (0.02, 0.018, 0.018)), NAVY, "arm{s}", mirror=True)
    for i, dx in enumerate((-0.013, 0.0, 0.013)):
        b = HD + np.array([dx, -0.012, -0.014])
        k.add(f"claw{i}", hull([b + (-0.007, 0, 0.004), b + (0.007, 0, 0.004), b + (0, 0.008, 0.002),
                                b + (0, -0.006, 0.002), b + (dx * 0.8, -0.03, -0.042)]), PINK, "arm{s}",
              fuse=False, mirror=True)

    # --- the head: a long dark wedge, level, under a crown of flat faces ---------------------------------------------
    k.add("head", ell(HEAD_C, HEAD_R, deform=snout, sub=5), NAVY, "head")
    crown = [(0, -0.24, 0.733), (0, -0.23, 0.712), (0, -0.08, 0.818), (0, 0.08, 0.803), (0, 0.13, 0.76),
             (0, 0.05, 0.72)]
    for sd in (1, -1):
        crown += [(0.082 * sd, -0.1, 0.766), (0.082 * sd, -0.1, 0.744), (0.095 * sd, 0.06, 0.782),
                  (0.095 * sd, 0.06, 0.748), (0.045 * sd, 0.125, 0.752)]
    crown = [HEAD_C + GROW * (np.array(p) - GROW_FROM) for p in crown]
    k.add("crown", hull(crown), NAVY, "head", fuse=False)

    # --- the launch tubes and their blades, a Dreepy hanging out of each (laid on +X, mirrored) ---------------------
    X = TUBE_X
    Z = 0.025                                   # the tubes stand higher on the bigger head
    # the horn's root from the head's side out to the tube
    k.add("root", hull([(0.06, -0.04, 0.8), (0.06, 0.08, 0.8), (0.06, 0.0, 0.77), (X, -0.05, 0.8 + Z),
                        (X, 0.03, 0.8 + Z), (X, 0.0, 0.85 + Z)]), NAVY, "head", fuse=False, mirror=True)
    tri = [(X, 0.0, 0.905 + Z), (X - 0.055, 0.0, 0.79 + Z), (X + 0.055, 0.0, 0.79 + Z)]
    tube_pts = [np.array(p) + (0, y, 0) for p in tri for y in (-0.06, 0.03)]
    k.add("tube", hull(tube_pts), PINK, "head", fuse=False, mirror=True)
    # its dark window, open in front
    win = [(X, -0.062, 0.878 + Z), (X - 0.034, -0.062, 0.806 + Z), (X + 0.034, -0.062, 0.806 + Z)]
    k.add("window", hull([np.array(p) + (0, dy, 0) for p in win for dy in (0.0, 0.012)]) , WINDOW, "head",
          fuse=False, mirror=True)
    # the blade: dark, swept out and back to a point, its top pink
    tip = np.array([0.39, 0.1, 0.89])
    rf, rb, rt, rd = (np.array(p) for p in ((X + 0.04, -0.05, 0.825 + Z), (X + 0.04, 0.035, 0.83 + Z),
                                             (X + 0.03, -0.0, 0.875 + Z), (X + 0.03, 0.0, 0.795 + Z)))
    k.add("blade", hull([rf, rb, rt, rd, tip, tip + (0, 0, -0.012)]), NAVY, "head", fuse=False, mirror=True)
    up = np.array([0, 0, 1.0])
    k.add("bladetop", hull([rf + up * 0.004 + (0, -0.003, 0), rb + up * 0.004, rt + up * 0.005,
                            rf - up * 0.014, rb - up * 0.014, rt - up * 0.02, tip + up * 0.003 + (0.004, 0, 0)]),
          PINK, "head", fuse=False, mirror=True)
    spots = dr.dreepy(k, O=(X, -0.08, 0.815 + Z), F=(0.12, -1, -0.38), U=(0, 0, 1), s=0.42,
                      bones=dict(head="head", body="head", tail="head"), mirror=True, legs=False, floor=0.0095,
                      name="rider")
    dr.lay_eyes(k, spots, "Open", mirror=True, eps=0.0018, lid=dr.MINT)

    k.build(voxel=0.0072, smooth=4, band=0.016, wband=0.06)

    # --- the eyes: narrow slanted slits, yellow edged red, low on the head's sides ---------------------------------
    Y, R, P = srgb(EYE), srgb(RED), srgb(PUPIL)

    def eye(u, v):
        c = np.tile(Y, (len(u), 1))
        c[v < -0.35] = R
        c[((u + 0.1) / 0.2) ** 2 + (v / 0.55) ** 2 < 1] = P
        return c
    SLIT = [(-1, -0.15), (-0.5, -0.65), (0.4, -0.6), (1, 0.25), (0.65, 0.6), (-0.2, 0.5), (-0.7, 0.3)]
    places = []
    for sd in (1, -1):
        p, n = on_ell(HEAD_C, HEAD_R * (0.82, 0.82, 1), (0.75 * sd, -0.6, -0.2))      # the ray finds the wedge
        places.append((p, n, sd))

    def eyes(state):
        f = eye_look(state, eye, srgb(NAVY), srgb("#C9A818"), line=0.2)
        for p, n, sd in places:
            col = f if sd > 0 else (lambda u, v, f=f: f(-u, v))
            k.decal(p, n, (0.048, 0.024), col, eps=0.0018, outline=polygon([(a * sd, b) for a, b in SLIT]),
                    rings=20, segs=56, up=(0, 0, 1))

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    dragapult(*args())
