# Mega Sableye sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sableyemega.py -- <the export's Sableye-Mega.glb> <out.glb>
#
# Grown from sableye.py (its head, ears, gems and claws are that script's functions). From the references: the red
# gem of its chest grown into a great cut shield, nearly as tall as the whole imp, an elongated hexagon with a broad
# table, held up before its left side (-X, as the game's own sculpt holds it) by the left hand behind it and gripped at
# its inner edge by the right hand reaching across the chest; the eyes red cut gems now; a wide grin of big yellow
# teeth meeting in a zigzag under them; the chest's own gem gone (it is the shield); a second small prong on each ear.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, slab, srgb, unit, boxy, polygon, eye_look, tri, args, sibling)

base = sibling("sableye")
H, HR, BOX = base.H, base.HR, base.BOX
on_head, gem, claw, blade_hull = base.on_head, base.gem, base.claw, base.blade_hull

PURPLE, INK = base.PURPLE, base.INK
GEM, GEM_GLOW = base.GEM, base.GEM_GLOW
EYE_RED, EYE_RIM, EYE_GLOW = "#E8283E", "#2A0E1C", "#F04456"
SHIELD, SHIELD_TABLE, SHIELD_HEART = "#D62A4A", "#F48CA0", "#E2506C"
MOUTH, TEETH = "#2A0E1C", "#E8CF48"

# The shield: its middle, the way its face looks, its long axis (the top leaning a little in toward the head)
SC = np.array([-0.25, -0.12, 0.30])
SN = unit(-0.2, -1, 0)
_a = unit(0.17, 0, 1)
SA = unit(*(_a - SN * np.dot(_a, SN)))
SR = np.cross(SA, SN)                                 # across the face, toward +X
SW, SH = 0.155, 0.27                                  # half its width and half its height
HEXAGON = [(0, 1), (0.95, 0.5), (0.95, -0.5), (0, -1), (-0.95, -0.5), (-0.95, 0.5)]


def shield(k):
    pts = []
    for a, b in HEXAGON:
        e = SR * a * SW + SA * b * SH
        pts.append(SC + e)                            # the girdle
        pts.append(SC + SN * 0.045 + e * 0.6)         # the table's corners
        pts.append(SC - SN * 0.035 + e * 0.82)        # the back
    from sculptkit import hull
    k.add("shield", hull(pts), SHIELD, "armL", fuse=False, surface="shell")
    table = [(a * SW * 0.6, b * SH * 0.6) for a, b in HEXAGON]
    k.add("shieldtable", slab(table, SC + SN * 0.0475, SR, SA, 0.004), SHIELD_TABLE, "armL", fuse=False,
          surface="shell")
    heart = [(a * SW * 0.33, b * SH * 0.33) for a, b in HEXAGON]
    k.add("shieldheart", slab(heart, SC + SN * 0.0505, SR, SA, 0.004), SHIELD_HEART, "armL", fuse=False,
          surface="shell")


def sableye_mega(src, out):
    k = Kit(src, coat="fur")

    # --- the head as the base's ---------------------------------------------------------------------------------
    k.add("head", ell(H, HR, deform=boxy(BOX)), PURPLE, "head")
    k.add("jowl", ell((0, -0.02, 0.31), (0.11, 0.09, 0.06)), PURPLE, "head")
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        B = np.array([0.11, -0.01, 0.44]) * m
        T = np.array([0.27, 0.03, 0.6]) * m
        k.add(f"ear{side}", blade_hull(B, T, 0.085, 0.036, (s * -0.7, 0, 0.7), bend=(0, 0.0, 0.01)), PURPLE,
              f"ear{side}", fuse=False)
        P = B + (T - B) * 0.42 + np.array([s * 0.02, 0, -0.02])
        k.add(f"prong{side}", blade_hull(P, P + np.array([s * 0.085, 0.01, -0.005]), 0.045, 0.03, (0, 0, 1)),
              PURPLE, f"ear{side}", fuse=False)
        Q = B + (T - B) * 0.68 + np.array([s * 0.012, 0, -0.016])
        k.add(f"prongb{side}", blade_hull(Q, Q + np.array([s * 0.06, 0.01, -0.012]), 0.036, 0.028, (0, 0, 1)),
              PURPLE, f"ear{side}", fuse=False)

    # --- the body: the base's, the chest's gem gone into the shield -------------------------------------------------
    k.add("torso", ell((0, 0.0, 0.235), (0.07, 0.064, 0.085), rot=(15, 0, 0)), PURPLE, "body")
    k.add("hips", ell((0, 0.012, 0.165), (0.066, 0.058, 0.05)), PURPLE, "body")
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"backgem{side}", gem(np.array([s * 0.036, 0.06, 0.262]), (s * 0.45, 1, 0.25), 0.015, 0.018, 0.011,
                                    sides=6, sink=0.008), GEM, "body", fuse=False, glow=GEM_GLOW, surface="shell")

    # --- the shield, and the arms that hold it ---------------------------------------------------------------------
    shield(k)
    # the left arm (-X) bent up behind the shield, its hand on the shield's back
    k.add("armL", tube([(-0.06, -0.02, 0.29), (-0.12, -0.04, 0.24), (-0.17, -0.06, 0.255)], [0.024, 0.021, 0.02]),
          PURPLE, "armL")
    k.add("handL", ell((-0.185, -0.07, 0.26), (0.03, 0.026, 0.03)), PURPLE, "armL")
    k.add("wristL", claw((-0.14, -0.035, 0.22), (0.4, 0.7, -0.5), 0.05, 0.03, 0.026), PURPLE, "armL", fuse=False)
    # the right arm across the chest, its claws gripping the shield's inner edge
    k.add("armR", tube([(0.06, -0.02, 0.29), (0.1, -0.1, 0.235), (0.02, -0.17, 0.215), (-0.05, -0.185, 0.22)],
                       [0.024, 0.021, 0.02, 0.019]), PURPLE, "armR")
    HC = np.array([-0.07, -0.19, 0.225])
    k.add("handR", ell(HC, (0.03, 0.03, 0.03)), PURPLE, "armR")
    for j, d in enumerate([(-0.85, -0.2, 0.45), (-0.95, -0.25, 0.0), (-0.85, -0.2, -0.45)]):
        d = np.array(d)
        k.add(f"fingerR{j}", claw(HC + unit(*d) * 0.018, d, 0.05, 0.03, 0.026), PURPLE, "armR", fuse=False)
    k.add("wristR", claw((0.08, -0.13, 0.235), (0.7, 0.2, 0.6), 0.05, 0.03, 0.026), PURPLE, "armR", fuse=False)

    # --- the legs as the base's ---------------------------------------------------------------------------------
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array([0.072, -0.005, 0.15]) * m, (0.045, 0.05, 0.045)), PURPLE, f"leg{side}")
        k.add(f"leg{side}", tube(np.array([(0.065, 0.0, 0.155), (0.125, -0.055, 0.115), (0.12, -0.02, 0.06),
                                           (0.115, -0.02, 0.03)]) * m, [0.034, 0.028, 0.024, 0.024]), PURPLE,
              f"leg{side}")
        F = np.array([0.115, -0.055, 0.022])
        k.add(f"foot{side}", ell(F * m, (0.032, 0.05, 0.022)), PURPLE, f"leg{side}")
        for j, dx in enumerate((-0.022, 0.0, 0.022)):
            b = np.array([0.115 + dx, -0.09, 0.018])
            k.add(f"toe{side}{j}", claw(b * m, np.array([dx * 1.5, -1, -0.2]) * m, 0.04, 0.03, 0.022), PURPLE,
                  f"leg{side}", fuse=False)
        k.add(f"heel{side}", claw(np.array([0.12, 0.0, 0.05]) * m, np.array([0.5, 0.8, 0.2]) * m, 0.05, 0.03, 0.024),
              PURPLE, f"leg{side}", fuse=False)

    # --- the grin: big yellow teeth meeting in a zigzag, a dark rim, its corners rising -------------------------
    top = [(-1, 0.75), (-0.5, 0.55), (0, 0.5), (0.5, 0.55), (1, 0.75)]
    bottom = [(-1, 0.75), (-0.7, 0.0), (-0.35, -0.4), (0, -0.5), (0.35, -0.4), (0.7, 0.0), (1, 0.75)]
    outline = [(1, 0.75), (0.5, 0.55), (0, 0.5), (-0.5, 0.55), (-1, 0.75), (-0.7, 0.0), (-0.35, -0.4), (0, -0.5),
               (0.35, -0.4), (0.7, 0.0)]
    cd, ct = srgb(MOUTH), srgb(TEETH)

    def grin(u, v):
        hi = np.interp(u, [p[0] for p in top], [p[1] for p in top])
        lo = np.interp(u, [p[0] for p in bottom], [p[1] for p in bottom])
        mid = (hi + lo) / 2 + 0.22 * (hi - lo) * tri(u * 3.5)
        teeth = (v < hi - 0.17) & (v > lo + 0.17) & (np.abs(v - mid) > 0.11) & (np.abs(u) < 0.88)
        c = np.tile(cd, (len(u), 1))
        c[teeth] = ct
        return c

    k.decal((0, -0.155, 0.305), (0, -1, -0.2), (0.118, 0.052), grin, eps=0.004, outline=polygon(outline),
            rings=24, segs=72)

    # --- the eyes: red hexagonal cut gems in a dark setting -------------------------------------------------------
    EYE = {s: on_head(s * 0.07, H[2] - 0.005) for s in (1, -1)}
    R = 0.052
    HEX = [(math.cos(a), math.sin(a)) for a in np.linspace(0, 2 * math.pi, 7)[:-1]]
    CL, CI = srgb(PURPLE), srgb("#160E2C")

    def eyes(state):
        for s in (1, -1):
            p, n = EYE[s]
            if state in ("Open", "Fierce"):
                cut = (lambda u, s=s: 0.35 + 0.45 * s * u) if state == "Fierce" else None
                ring = [(u, min(v, cut(u)) if cut else v) for u, v in HEX]
                k.decal(p, n, (R * 1.2, R * 1.2), EYE_RIM, eps=0.004, outline=polygon(ring), rings=20, segs=56)
                k.add(f"eye{'R' if s > 0 else 'L'}", gem(p, n, R, R, 0.024, sides=6, table=0.52, sink=0.012, cut=cut),
                      EYE_RED, "head", fuse=False, glow=EYE_GLOW, surface="shell")
            else:
                look = eye_look(state, None, CL, CI, line=0.15)
                f = look if s > 0 else (lambda u, v, look=look: look(-u, v))
                k.decal(p, n, (R * 1.1, R * 1.1), f, eps=0.005, outline=polygon(HEX), rings=20, segs=56)

    k.build(voxel=0.006, smooth=4)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    sableye_mega(*args())
