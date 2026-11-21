# Mega Meowstic (the male) sculpted on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/meowsticmalemega.py -- <the export's Meowstic-Male-Mega.glb> <out.glb>
# Grown from meowstic.py (copied: its function takes nothing that reaches the parts; its navy, its ruff of pointed
# tufts, its slim pear of a body and its tails' sweep). Mega Meowstic floats (its joints stand 0.074 over Meowstic's):
# its largest mass is the two great white tails hanging from its hips and sweeping back and down into a broad swirl
# under it, together some half its height and four times the body's width; then its ears, grown long and rising over
# the head in an arch, each ending in a round paddle with a yellow ring like an eye on its front; then a hood of white
# locks swept back from the head. The design is split down the middle: its left ear (-X) and the left of its face are
# navy, its right ear and the right of its face white (both pictures show the white ear and the white cheek on the
# side nearer the viewer, which is the creature's right as it faces the picture's right). Its eyes are shut in both
# pictures: a teal line on the navy side, a yellow one on the white. A slim navy body with a white ruff, short navy
# arms tipped white carried forward and down, short navy legs hanging to points.
#
# Written to be grown from: meowsticfemalemega.py calls meowsticmega(src, dest, female=True), the female branch of
# the game's own sculptor (white where the male is navy, navy marks, red eyes).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, polygon, args

NAVY = "#3258A8"
WHITE = "#F3F2F9"
RING, RING_IN = "#F2DC3A", "#7C8A2A"
NOSE = "#1A2440"


def on_ell(c, r, d):
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


H = np.array([0.0, -0.02, 0.408])        # the head, over its joint (0, -0.01, 0.374)
HR = np.array([0.074, 0.066, 0.068])
EAR_PTS = [(0.04, -0.012, 0.448), (0.06, -0.008, 0.51), (0.085, -0.006, 0.575), (0.098, -0.01, 0.635),
           (0.09, -0.018, 0.682)]
EAR_R = [0.032, 0.031, 0.03, 0.031, 0.033]
PADDLE = np.array([0.084, -0.022, 0.705])
PADDLE_R = (0.044, 0.026, 0.05)
TAILS = [
    ([(0.0, 0.03, 0.255), (0.04, 0.09, 0.245), (0.09, 0.15, 0.19), (0.115, 0.165, 0.115), (0.1, 0.125, 0.05),
      (0.065, 0.07, 0.035), (0.045, 0.04, 0.06), (0.055, 0.03, 0.09)],
     [0.03, 0.058, 0.074, 0.072, 0.062, 0.046, 0.03, 0.016]),
    ([(0.0, 0.03, 0.24), (-0.035, 0.1, 0.2), (-0.055, 0.16, 0.13), (-0.05, 0.2, 0.065), (-0.03, 0.225, 0.03),
      (-0.01, 0.235, 0.022)],
     [0.03, 0.056, 0.066, 0.05, 0.026, 0.006]),
]


def meowsticmega(src, dest, female=False):
    main, mark = (WHITE, NAVY) if female else (NAVY, WHITE)
    k = Kit(src, coat="fur")

    # --- the head: small and round; on the male its right half white -----------------------------------------------
    k.add("head", ell(H, HR), main, "head")
    k.add("neck", tube([(0, -0.006, 0.34), (0, -0.014, 0.38)], [0.03, 0.034]), main, "head")
    k.add("muzzle", ell(H + (0, -0.05, -0.022), (0.04, 0.03, 0.03)), main, "head")
    if not female:
        k.paint(lambda P: inside(-P[:, 0], 0.002) * (P[:, 2] > 0.35) * (P[:, 2] < 0.49) * (P[:, 1] < 0.08), WHITE,
                on=["head", "muzzle", "neck"])
    else:
        # the female's navy mark: a forelock painted down the brow from the crown
        k.paint(lambda P: inside(np.hypot(P[:, 0] / 0.018, (P[:, 2] - (H[2] + 0.04)) / 0.03) - 1, 0.002)
                * (P[:, 1] < H[1] - 0.02), NAVY, on=["head"])

    # --- the ears: long, rising in an arch over the head, a round paddle at each end ---------------------------------
    for sx in (1, -1):
        m = np.array([sx, 1, 1.0])
        col = (NAVY if sx < 0 else WHITE) if not female else NAVY
        nm = "earR" if sx > 0 else "earL"
        k.add(nm, tube([np.array(p) * m for p in EAR_PTS], EAR_R), col, nm)
        k.add("paddle" + nm[-1], ell(PADDLE * m, PADDLE_R, rot=(-8, 0, -12 * sx)), col, nm)
        if female:
            # a white band below the paddle (the sculptor's)
            k.paint(lambda P, sx=sx: inside(np.abs(P[:, 2] - 0.6) - 0.012, 0.002) * (P[:, 0] * sx > 0.05),
                    WHITE, on=["ear" + ("R" if sx > 0 else "L")])
        # the eye-ring on the paddle's front, in every state
        ci, cr = srgb(RING_IN), srgb(RING)
        def ring(u, v):
            r = np.hypot(u, v)
            c = np.tile(cr, (len(u), 1))
            c[(r > 0.3) & (r < 0.62)] = ci
            return c
        p, n = on_ell(PADDLE * m, PADDLE_R, (0.1 * sx, -1, 0.05))
        k.decal(p, n, (0.026, 0.034), ring, eps=0.002, rings=16, segs=48)

    # --- the hood of locks: white, swept back and out from the crown and the back of the head ------------------------
    lock_col = NAVY if female else WHITE
    k.add("hood", ell(H + (0, 0.03, 0.015), (0.08, 0.07, 0.07)), lock_col, "head")
    for sx in (1, -1):
        for i, (z, out, back, down, r) in enumerate(((0.45, 0.045, 0.12, 0.01, 0.042), (0.41, 0.075, 0.12, -0.035, 0.04),
                                                     (0.37, 0.06, 0.1, -0.06, 0.034))):
            base = np.array((0.04 * sx, 0.03, z))
            tip = base + np.array((out * sx, back, down))
            mid = (base + tip) / 2 + np.array((0, 0, 0.018))
            k.add(f"lock{'R' if sx > 0 else 'L'}{i}", blade([base, mid, tip], [r, r * 0.65, 0.004], (0, 0, 1), 0.55),
                  lock_col, "head")
    # a fringe sweeping across the brow from the crown toward the white side
    k.add("fringe", blade([(-0.04, -0.03, 0.468), (0.0, -0.06, 0.462), (0.04, -0.07, 0.446), (0.075, -0.06, 0.428)],
                          [0.018, 0.022, 0.018, 0.004], (0, 0.6, 1), 0.45), lock_col, "head")

    # --- the body: slim, a white ruff at the neck, the hem flared into points -----------------------------------------
    def pear(V):
        V = V.copy()
        V[:, :2] *= (1 + 0.18 * np.clip(-V[:, 2], 0, 1))[:, None]
        return V
    k.add("torso", ell((0, 0.0, 0.275), (0.042, 0.036, 0.07), deform=pear), main, "body")
    for i, (x, y, z, r) in enumerate(((0.0, -0.04, 0.338, 0.026), (0.03, -0.032, 0.34, 0.024),
                                      (0.042, -0.008, 0.342, 0.02), (0.03, 0.02, 0.344, 0.02))):
        k.add(f"ruff{i}", ell((x, y, z), (r, r * 0.85, r * 0.8)), WHITE, "body", mirror=x > 0)
    for j in range(7):
        a = -math.pi + 2 * math.pi * (j + 0.5) / 7
        if math.cos(a) < -0.3:
            continue
        b0 = np.array((0.032 * math.sin(a), -0.03 * math.cos(a), 0.33))
        d = unit(0.9 * math.sin(a), -0.9 * math.cos(a), -0.8)
        k.add(f"ruffpt{j}", blade([b0, b0 + d * 0.016, b0 + d * 0.032], [0.012, 0.008, 0.002], (0, 0, 1), 0.5), WHITE,
              "body")
    for i, (z, d, ln) in enumerate(((0.23, unit(1, -0.2, -0.55), 0.045), (0.225, unit(0.35, -1, -0.6), 0.035))):
        b0 = np.array((0.034, -0.012 * i, z))
        k.add(f"hem{i}", blade([b0, b0 + d * ln * 0.5, b0 + d * ln], [0.017, 0.011, 0.003], (0, 0, 1), 0.5), main,
              "body", mirror=True)

    # --- short arms carried forward and down, hands before the belly inside the body's width, tipped white ----------
    ARM = [(0.036, -0.012, 0.338), (0.048, -0.04, 0.312), (0.05, -0.062, 0.29)]
    k.add("arm", tube(ARM, [0.016, 0.014, 0.013]), main, "arm{s}", mirror=True)
    k.add("hand", blade([(0.05, -0.062, 0.292), (0.051, -0.075, 0.282), (0.051, -0.082, 0.275)], [0.013, 0.01, 0.004],
                        (1, 0, 0), 0.6), mark, "arm{s}", mirror=True)

    # --- short legs hanging to points (it floats) --------------------------------------------------------------------
    k.add("thigh", ell((0.028, 0.002, 0.215), (0.03, 0.032, 0.045)), main, "leg{s}", mirror=True)
    k.add("leg", tube([(0.027, 0.0, 0.2), (0.029, -0.004, 0.15), (0.03, -0.01, 0.118)], [0.02, 0.018, 0.012]), main,
          "leg{s}", mirror=True)
    k.add("foot", blade([(0.03, -0.01, 0.125), (0.031, -0.022, 0.1), (0.031, -0.03, 0.088)], [0.015, 0.01, 0.003],
                        (1, 0, 0), 0.6), mark if female else main, "leg{s}", mirror=True)

    # --- the two great tails: white, from the hips back and down into a swirl under the body ------------------------
    for j, (pts, radii) in enumerate(TAILS):
        pts = [(x, y, z + 0.035 * min(1, i / 2)) for i, (x, y, z) in enumerate(pts)]     # the swirl kept off the ground
        k.add(f"tail{j}", tube(pts, radii, seg=20), WHITE, "tail")
        if female:
            end = np.array(pts[-2]) if j == 0 else np.array(pts[-1])
            k.paint(lambda P, e=end: inside(np.linalg.norm(P - e, axis=1) - 0.05, 0.003), NAVY, on=[f"tail{j}"])

    # --- a small dark nose ---------------------------------------------------------------------------------------------
    p, n = on_ell(H + (0, -0.05, -0.022), (0.04, 0.03, 0.03), (0, -1, 0.2))
    k.decal(p, n, (0.007, 0.0055), NOSE, eps=0.002, outline=polygon([(-1, 0.7), (1, 0.7), (0, -1)]), rings=5, segs=20)

    k.build(voxel=0.0044, band=0.004, wband=0.035)

    # --- the eyes: shut in both pictures, so the Open state is the shut line; Fierce that line tilted ----------------
    if female:
        sides = {1: (WHITE, "#D8443A"), -1: (WHITE, "#D8443A")}
    else:
        sides = {1: (WHITE, "#E8C430"), -1: (NAVY, "#36B8B0")}
    def eyes(state):
        for sx in (1, -1):
            lid, ink = srgb(sides[sx][0]), srgb(sides[sx][1])
            if state == "Open":
                look = eye_look("Shut", None, lid, ink, line=0.2)
            elif state == "Fierce":
                def look(u, v, lid=lid, ink=ink):
                    c = np.tile(lid, (len(u), 1))
                    c[(np.abs(u) < 0.85) & (np.abs(v - (-0.1 + 0.35 * u)) < 0.2)] = ink
                    return c
            else:
                look = eye_look(state, None, lid, ink, line=0.2)
            p, n = on_ell(H, HR, (0.42 * sx, -0.88, 0.06))
            f = look if sx > 0 else (lambda u, v, g=look: g(-u, v))
            k.decal(p, n, (0.026, 0.018), f, eps=0.002, rings=20, segs=56)
    k.eye_states(eyes)
    k.export(dest)


if __name__ == "__main__":
    src, dest = args()
    meowsticmega(src, dest)
