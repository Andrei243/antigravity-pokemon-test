# Mega Raichu Y sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py),
# grown from Raichu's script (raichu.py: its head, pear of a body, belly, back stripes and arms, copied, since it builds
# as it is read) and from Mega Raichu X's frame (raichumegax.py, through sibling: on_head, bolt, curl, brow_eyes).
#   blender --background --factory-startup --python sculpts/raichumegay.py -- <the export's Raichu-Mega-Y.glb> <out.glb>
#
# The design, from the two pictures: Raichu's orange body and white belly, hovering; the largest masses are two great
# flat yellow bolts of lightning rising from behind the ears, up and out to a point and down again outward in a zigzag
# to a small curl, and the tail: a long thin brown wire that hangs in a loop under the feet and comes up at the right
# into a broad yellow bolt that ends in a great flag; tall pointed ears black over their upper half; jagged tufts of
# fur on the cheeks; blue eyes under brows that slope down to the nose, a small wavy mouth, yellow cheeks.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, slab, hull, srgb, inside, unit, eye_look, polygon, args, sibling

X = sibling("raichumegax")

ORANGE, WHITE, BROWN, BLACK, CHEEK = "#F2A030", "#FBF8F2", "#6A4224", "#22202A", "#F6D63E"
BOLTY, INK, NOSE, BLUE = "#FFDA26", "#1E1E26", "#2A2026", "#3E9AE6"


def raichu_mega_y(src, out_path):
    k = Kit(src, coat="fur")
    DZ = 0.128                                 # the form's joints stand Raichu's 0.128 higher: it hovers
    H = np.array([0, -0.012, 0.525 + DZ + 0.004])
    HR = np.array([0.156, 0.136, 0.127])
    oh = lambda d, lift=0.0: X.on_head(H, HR, d, lift)

    # Raichu's head and pear of a body, no neck
    k.add("head", ell(H, HR), ORANGE, "head")
    k.add("body", ell((0, 0.01, 0.255 + DZ), (0.142, 0.128, 0.18), deform=X.pear), ORANGE, "body")
    k.add("chest", ell((0, 0.012, 0.405 + DZ), (0.112, 0.085, 0.07)), ORANGE, "body")

    # Short thick legs, the feet hanging with their toes a little down
    k.add("thigh", ell((0.078, -0.014, 0.115 + DZ), (0.062, 0.066, 0.078)), ORANGE, "leg{s}", mirror=True)
    k.add("foot", ell((0.086, -0.056, 0.03 + DZ), (0.044, 0.072, 0.028), rot=(-14, 0, 0)), ORANGE, "leg{s}",
          mirror=True)

    # Short arms, forward and a little down, small orange paws
    k.add("arm", tube([(0.09, -0.03, 0.385 + DZ), (0.138, -0.068, 0.355 + DZ), (0.162, -0.1, 0.33 + DZ)],
                      [0.04, 0.034, 0.03]), ORANGE, "arm{s}", mirror=True)
    k.add("paw", ell((0.172, -0.112, 0.322 + DZ), (0.034, 0.034, 0.03)), ORANGE, "arm{s}", mirror=True)

    # The big white belly
    def belly(P):
        e = (P[:, 0] / 0.102) ** 2 + ((P[:, 2] - 0.245 - DZ) / 0.15) ** 2 - 1
        return inside(e * 0.05, 0.0025) * inside(P[:, 1] + 0.03, 0.01) * inside(P[:, 2] - 0.46 - DZ, 0.004)
    k.paint(belly, WHITE, on=["body", "chest"])

    # Tall pointed ears out of the crown, up and a little out: orange at the root, black over the upper half
    EAR = np.array([(0.07, 0.012, H[2] + 0.07), (0.092, 0.014, H[2] + 0.12), (0.118, 0.016, H[2] + 0.19),
                    (0.142, 0.018, H[2] + 0.25), (0.16, 0.02, H[2] + 0.3)])
    k.add("ear", blade(EAR, [0.042, 0.05, 0.038, 0.02, 0.005], (0, 1, 0), 0.5), ORANGE, "ear{s}", mirror=True)
    P0 = EAR[1] + (EAR[2] - EAR[1]) * 0.35
    EA = unit(*(EAR[3] - EAR[1]))
    def ear_tip(P):
        Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
        return inside(((P0 - Q) @ EA), 0.0025) * (P[:, 2] > H[2] + 0.1) * (Q[:, 0] > 0.04)
    k.paint(ear_tip, BLACK)

    # Jagged tufts of fur on the cheeks: three short orange points out of each side of the face
    for i, (dx, dz, ln) in enumerate([(0.0, 0.03, 0.05), (0.0, -0.025, 0.06), (0.02, -0.075, 0.045)]):
        d = unit(1, -0.15 + dx, dz * 6)
        base = oh(unit(1, -0.25, dz * 4), -0.02)
        tip = base + d * (ln * 0.75 + 0.02) + np.array([0, 0, -0.012])
        a, b = np.array([0, 0, 1.0]) * 0.032, np.array([0, 1.0, 0]) * 0.026
        k.add(f"tuft{i}", hull([base + a, base - a, base + b, base - b, tip]), ORANGE, "head", fuse=False,
              mirror=True)

    # The two great bolts of lightning from behind the ears, in a plane that leans back and turns a little out: up
    # and out to a point, then down outward in a zigzag to a small curl. Outline: a outward, b up, from the root
    HEADB = [(0, -0.02), (0.05, 0.12), (0.16, 0.33), (0.27, 0.14), (0.2, 0.13), (0.28, -0.07), (0.26, -0.115),
             (0.15, 0.02), (0.1, 0.04), (0.07, -0.035)]
    RO, BX, BZ = np.array([0.09, 0.07, H[2] + 0.05]), unit(1, 0.3, 0), unit(0, 0.3, 1)
    S = 1.1
    X.bolt(k, "hbolt", HEADB, S, RO, BX, BZ, "ear{s}", [(BOLTY, -1, 1, 0.026)])
    BXn, BZn = np.array(BX), np.array(BZ) - np.array(BX) * (np.array(BZ) @ np.array(BX))
    BZn /= np.linalg.norm(BZn)
    tipc = RO + BXn * (0.245 * S) + BZn * (-0.13 * S)
    cp = X.curl(tipc, BXn, BZn, 0.032, 0.009, 1.15, start=math.pi * 0.2)
    k.add("hcurl", tube([RO + BXn * 0.268 * S + BZn * -0.105 * S] + cp, [0.014] + list(np.linspace(0.014, 0.01, len(cp))),
                        seg=12), BOLTY, "ear{s}", fuse=False, mirror=True)

    # The tail: a thin brown wire out of the back, down in a loop under the feet and up again at the right, where it
    # becomes a broad yellow bolt that runs back and out to the +X side and ends in a great flag
    wire = [(0, 0.1, 0.33), (0.0, 0.17, 0.25), (-0.06, 0.205, 0.15), (-0.05, 0.215, 0.07), (0.03, 0.22, 0.035),
            (0.11, 0.225, 0.08), (0.145, 0.235, 0.2), (0.158, 0.25, 0.31)]
    k.add("tailwire", tube(wire, [0.016] * len(wire), seg=12), BROWN, "tail", fuse=False)
    TAILB = [(0, 0), (0.04, 0.0), (0.085, 0.13), (0.062, 0.135), (0.105, 0.20), (0.40, 0.26), (0.47, 0.42),
             (0.10, 0.37), (0.06, 0.29), (0.04, 0.20), (0.02, 0.19), (-0.01, 0.08)]
    X.bolt(k, "tbolt", TAILB, 1.0, (0.152, 0.245, 0.3), unit(0.6, 0.8, 0), (0, 0, 1.0), "tail",
           [(BOLTY, -1, 1, 0.026)], mirror=False)

    # Brown stripes across the back, as Raichu's
    LENS = polygon([(-1, 0), (-0.8, 0.45), (-0.4, 0.82), (0, 1), (0.4, 0.82), (0.8, 0.45), (1, 0),
                    (0.8, -0.45), (0.4, -0.82), (0, -1), (-0.4, -0.82), (-0.8, -0.45)])
    for zc, hw in ((0.35 + DZ, 0.085), (0.285 + DZ, 0.098)):
        k.decal((0, 0.12, zc), unit(0, 1, 0.06), (hw, 0.017), BROWN, eps=0.003, outline=LENS, rings=10, segs=64)

    # The face: yellow cheeks, a dot of a nose, a small wavy mouth
    dc = unit(0.74, -0.6, -0.3)
    k.decal(oh(dc), dc, (0.042, 0.038), CHEEK, eps=0.003, rings=10, segs=40, mirror=True)
    k.add("nose", ell(oh((0, -1, -0.03), -0.002), (0.009, 0.0055, 0.0055)), NOSE, "head", fuse=False,
          surface="skin")
    CO, CK = srgb(ORANGE), srgb(INK)
    def mouth(u, v):
        c = np.tile(CO, (len(u), 1))
        c[(np.abs(v - (0.35 - 0.7 * np.abs(np.sin(math.pi * u)))) < 0.24) & (np.abs(u) < 0.95)] = CK
        return c
    dm = unit(0, -1, -0.36)
    k.decal(oh(dm), dm, (0.036, 0.018), mouth, eps=0.003, rings=18, segs=72)

    # Blue eyes, a dark pupil and a white glint, under brows that slope down to the nose
    CB = srgb(BLUE)
    def eye(u, v):
        c = np.tile(CK, (len(u), 1))
        c[((u - 0.04) / 0.66) ** 2 + ((v + 0.1) / 0.7) ** 2 < 1] = CB
        c[((u - 0.06) / 0.3) ** 2 + ((v + 0.08) / 0.34) ** 2 < 1] = CK
        c[((u - 0.28) / 0.2) ** 2 + ((v - 0.18) / 0.2) ** 2 < 1] = (1, 1, 1)
        return c
    de = unit(0.42, -0.88, 0.14)
    E = oh(de)
    def eyes(state):
        X.brow_eyes(k, E, de, (0.028, 0.031), eye_look(state, eye, CO, CK), 0.35, 0.45)

    k.build(voxel=0.0058)
    k.eye_states(eyes)
    k.export(out_path)


if __name__ == "__main__":
    src, out = args()
    raichu_mega_y(src, out)
