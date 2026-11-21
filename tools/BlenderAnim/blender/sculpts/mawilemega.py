# Mega Mawile sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/mawilemega.py -- <the export's Mawile-Mega.glb> <out.glb>
#
# Grown from mawile.py (copied: that script's build takes no arguments that reach what changes). From the references:
# two great black jaws in place of one, the design's largest mass, growing from the back of the crown and running far
# back, one above the other. Each is one mass, an open crocodile's head: a deep wedge at the hinge (its yellow oval
# spot on each flank, a few short spines on top) joining an upper and a lower mandible, deep and flattened, whose
# inner faces are magenta with white teeth between; the mouth opens toward the tips, the upper tip curling up, the
# lower down. After the game's shot (cut by its cell): both jaws a fifth shorter, the upper no higher than a fifth of
# the height over the head's top, both swung round to 74 and 78 degrees out to +X (across the game's cameras) and
# parted in height rather than in yaw, the lower pitched down toward its tip, so that they cost the cell one width.
# A jaw run back toward -X was end-on (a pillar over the head) from the game's front three-quarter camera.
# The body a little yellow figure in a long pink dress flaring to the ground in straight pleats, pink sleeves on its little arms and black ragged hands; a round yellow head with magenta-red eyes, its crown
# black, and two long black strands hanging from its sides down the dress.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, lathe, srgb, inside, unit, eye_look, args

YELLOW, BLACK, PINK, TEETH, SPOT = "#F3DE92", "#45454D", "#C2477E", "#F6F4EE", "#F1DA86"
DRESS = "#BC4A8C"
RED, INK = srgb("#D02A58"), srgb("#1A1418")

HC = np.array([0.0, -0.012, 0.37])                  # the head's centre and radii (the base's)
HR = np.array([0.092, 0.086, 0.088])


def claw(base, d, length, width, thick, up=(0, 0, 1)):
    d = unit(*d)
    a = np.cross(d, np.array(up, float)); a /= np.linalg.norm(a)
    b = np.cross(a, d)
    base = np.array(base, float)
    mid = base + d * length * 0.45
    pts = [base + a * width / 2, base - a * width / 2, base + b * thick / 2, base - b * thick / 2 - d * 0.004,
           mid + a * width * 0.28, mid - a * width * 0.28, mid + b * thick * 0.28, mid - b * thick * 0.28,
           base + d * length]
    return hull(pts)


def skirt():
    """The dress: a closed solid of turning flaring from the waist to the ground in straight pleats."""
    prof = [(0.0, 0.29), (0.05, 0.29), (0.068, 0.245), (0.09, 0.17), (0.118, 0.09), (0.142, 0.012), (0.0, 0.012)]
    V, F = lathe((0, 0, 0), (0, 0, 1), prof, seg=96)
    V = V.copy()
    r = np.hypot(V[:, 0], V[:, 1])
    th = np.arctan2(V[:, 1], V[:, 0])
    low = np.clip((0.24 - V[:, 2]) / 0.23, 0, 1)
    pleat = 1 + 0.05 * low * np.abs(np.cos(5 * th))
    k = np.where(r > 1e-6, pleat, 1.0)
    V[:, 0] *= k; V[:, 1] *= k
    hem = (V[:, 2] < 0.02) & (r > 1e-6)
    V[hem, 2] += 0.014 * (1 - np.abs(np.cos(5 * th[hem])))
    return V, F


class Jaw:
    """One jaw: a frame (its root and its way back), the mandibles' rows (s along the way, z, radius)."""

    def __init__(self, root, angle, bone, up, low):
        self.root = np.array(root, float)
        self.D = unit(math.sin(math.radians(angle)), math.cos(math.radians(angle)), 0)
        self.LAT = unit(*np.cross(self.D, (0, 0, 1.0)))
        self.bone, self.up, self.low = bone, up, low

    def pt(self, s, z, lat=0.0):
        return self.root + self.D * s + np.array([0, 0, z - self.root[2]]) + self.LAT * lat

    def along(self, rows, s):
        S = [q[0] for q in rows]
        return float(np.interp(s, S, [q[1] for q in rows])), float(np.interp(s, S, [q[2] for q in rows]))

    def add(self, k, name):
        UP, LOW, HINGE = self.up, self.low, self.hinge
        # the mandibles: deep and flattened across the jaw, so that each reads as a jaw and not a tentacle
        k.add(f"{name}top", blade([self.pt(s, z) for s, z, r in UP], [r for s, z, r in UP], self.LAT, 0.72), BLACK,
              self.bone)
        k.add(f"{name}low", blade([self.pt(s, z) for s, z, r in LOW], [r for s, z, r in LOW], self.LAT, 0.72), BLACK,
              self.bone)
        # the head of the jaw at the hinge: a deep wedge joining the two mandibles from the root to where the mouth
        # opens, as deep there as half the jaw is long
        mid = []
        for s, f in zip(np.linspace(0.0, HINGE * 0.65, 5), (1, 1, 1, 0.85, 0.7)):
            zu, ru = self.along(UP, s)
            zl, rl = self.along(LOW, s)
            mid.append((s, (zu + zl) / 2, (zu + ru * 0.8 - zl + rl * 0.8) / 2 * f))
        k.add(f"{name}hinge", blade([self.pt(s, z) for s, z, r in mid], [r for s, z, r in mid], self.LAT, 0.62),
              BLACK, self.bone)
        # the mouth lined magenta: the mandibles' inner faces and the throat at the hinge's front
        root, D = self.root, self.D

        def lining(P, rows, below):
            s = (P - root) @ D
            S = [q[0] for q in rows]
            zc = np.interp(s, S, [q[1] for q in rows])
            rc = np.interp(s, S, [q[2] for q in rows])
            d = (zc - 0.3 * rc - P[:, 2]) if below else (P[:, 2] - zc - 0.3 * rc)
            return inside(-d, 0.003) * (s > HINGE * 0.75)

        def throat(P):
            s = (P - root) @ D
            zu = np.interp(s, [q[0] for q in UP], [q[1] - 0.3 * q[2] for q in UP])
            zl = np.interp(s, [q[0] for q in LOW], [q[1] + 0.3 * q[2] for q in LOW])
            lat = np.abs((P - root) @ self.LAT)
            return inside(P[:, 2] - zu, 0.003) * inside(zl - P[:, 2], 0.003) * inside(HINGE * 0.55 - s, 0.004) \
                * inside(lat - 0.022, 0.003)
        k.paint(lambda P: lining(P, UP, True), PINK, on=[f"{name}top"])
        k.paint(lambda P: lining(P, LOW, False), PINK, on=[f"{name}low"])
        k.paint(throat, PINK, on=[f"{name}hinge"])
        # spines laid back off the top of the head
        for j, (s, ln) in enumerate(((0.02, 0.045), (0.07, 0.042), (0.12, 0.035))):
            z, r = self.along(UP, s)
            b = self.pt(s, z + r * 0.6)
            d = self.D * 0.6 + np.array([0, 0, 0.8])
            k.add(f"{name}spine{j}", claw(b, d, ln, 0.04, 0.026, up=self.LAT), BLACK, self.bone, fuse=False)
        # teeth: white points down from the upper mandible and up from the lower, on both flanks
        for side in (1, -1):
            for j, s in enumerate((0.22, 0.28, 0.34, 0.395)):
                z, r = self.along(UP, s)
                b = self.pt(s, z - r * 0.85, side * 0.014)
                dn = unit(*(self.pt(s + 0.012, z - r * 0.85 - 0.06, side * 0.018) - b))
                k.add(f"{name}tooth{side}{j}", claw(b, dn, 0.04, 0.03, 0.024, up=self.LAT), TEETH, self.bone,
                      fuse=False, surface="shell")
            for j, s in enumerate((0.25, 0.31, 0.37)):
                z, r = self.along(LOW, s)
                b = self.pt(s, z + r * 0.8, side * 0.012)
                upd = unit(*(self.pt(s - 0.01, z + r * 0.8 + 0.05, side * 0.014) - b))
                k.add(f"{name}fang{side}{j}", claw(b, upd, 0.032, 0.028, 0.022, up=self.LAT), TEETH, self.bone,
                      fuse=False, surface="shell")

    def spots(self, k):
        z, r = self.along(self.up, 0.07)
        zl, rl = self.along(self.low, 0.07)
        for side in (1, -1):
            n = self.LAT * side + np.array([0, 0, 0.25])
            k.decal(self.pt(0.07, (z + zl) / 2 + 0.02, side * 0.04), n, (0.055, 0.045), SPOT,
                    up=tuple(self.D + np.array([0, 0, 0.4])), eps=0.004, rings=16, segs=56)


# The jaws a fifth shorter than first built, the upper one lowered, both swung round toward +X at nearly one yaw
# (across the game's cameras, and so that they cost the cell one width, not a width and a length) and parted in
# height instead, the lower pitched down toward its tip: (s along the jaw, z, radius) for each mandible
HIGH = Jaw((-0.02, 0.035, 0.43), 78, "maw",
           [(-0.01, 0.465, 0.05), (0.06, 0.505, 0.054), (0.14, 0.515, 0.05), (0.23, 0.505, 0.045), (0.31, 0.495, 0.04),
            (0.38, 0.5, 0.032), (0.43, 0.525, 0.024)],
           [(0.0, 0.405, 0.036), (0.1, 0.39, 0.038), (0.19, 0.383, 0.035), (0.28, 0.377, 0.031), (0.36, 0.372, 0.026),
            (0.42, 0.352, 0.02)])
LOWJ = Jaw((0.02, 0.03, 0.31), 74, "maw2",
           [(-0.01, 0.325, 0.048), (0.06, 0.31, 0.05), (0.14, 0.285, 0.046), (0.23, 0.26, 0.042), (0.31, 0.242, 0.038),
            (0.38, 0.238, 0.03), (0.43, 0.255, 0.022)],
           [(0.0, 0.225, 0.034), (0.1, 0.19, 0.035), (0.19, 0.16, 0.032), (0.28, 0.14, 0.029), (0.36, 0.125, 0.025),
            (0.42, 0.105, 0.02)])
HIGH.hinge = LOWJ.hinge = 0.17


def mawile_mega(src, out):
    k = Kit(src, coat="fur")

    # --- the head: round, the crown and the back black where the jaws grow ----------------------------------------
    k.add("head", ell(HC, HR), YELLOW, "head")
    k.add("cheek", ell(HC + (0, -0.01, -0.03), (0.084, 0.076, 0.06)), YELLOW, "head")
    k.paint(lambda P: inside(0.03 - 0.75 * (P[:, 2] - HC[2]) - (P[:, 1] - HC[1]), 0.003)
            * inside(np.linalg.norm((P - HC) / (HR * 1.25), axis=1) - 1, 0.01) * (P[:, 2] > HC[2] - 0.05), BLACK)
    # the long strands hanging from the sides of the head down the dress
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"strand{side}", tube(np.array([(0.06, 0.01, 0.425), (0.11, 0.015, 0.415), (0.135, 0.012, 0.35),
                                              (0.14, 0.0, 0.26), (0.148, -0.005, 0.17)]) * m,
                                    [0.02, 0.02, 0.019, 0.019, 0.02]), BLACK, "head")
        k.add(f"strandtip{side}", ell(np.array([0.15, -0.007, 0.15]) * m, (0.024, 0.016, 0.036)), BLACK, "head")

    # --- the two jaws ------------------------------------------------------------------------------------------------
    HIGH.add(k, "jawa")
    LOWJ.add(k, "jawb")
    HIGH.spots(k)
    LOWJ.spots(k)

    # --- the body: a slim yellow body in a long pink dress -------------------------------------------------------
    k.add("torso", ell((0, 0.0, 0.25), (0.05, 0.045, 0.065)), YELLOW, "body")
    k.add("neck", ell((0, -0.005, 0.3), (0.042, 0.04, 0.04)), YELLOW, "body")
    k.add("skirt", skirt(), DRESS, "body", fuse=False, surface="cloth")
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"foot{side}", ell(np.array([0.05, -0.03, 0.018]) * m, (0.03, 0.04, 0.02)), BLACK, f"leg{side}")
        k.add(f"leg{side}", tube(np.array([(0.04, 0.0, 0.1), (0.05, -0.015, 0.03)]) * m, [0.025, 0.024]), YELLOW,
              f"leg{side}")
        # the arms: little, forward and a little down, pink sleeves, black ragged hands
        k.add(f"arm{side}", tube(np.array([(0.035, 0.0, 0.26), (0.072, -0.03, 0.237)]) * m, [0.02, 0.019]), YELLOW,
              f"arm{side}")
        k.add(f"sleeve{side}", tube(np.array([(0.07, -0.028, 0.238), (0.1, -0.06, 0.216)]) * m, [0.022, 0.031]),
              DRESS, f"arm{side}")
        Hd = np.array([0.116, -0.078, 0.204])
        k.add(f"hand{side}", ell(Hd * m, (0.024, 0.026, 0.022)), BLACK, f"arm{side}")
        for j, d in enumerate([(0.6, -0.6, 0.2), (0.3, -0.85, -0.3), (-0.1, -0.8, -0.55)]):
            d = np.array(d)
            b = Hd + unit(*d) * 0.014
            k.add(f"finger{side}{j}", claw(b * m, d * m, 0.034, 0.03, 0.022), BLACK, f"arm{side}", fuse=False)

    # --- the eyes: the base's, magenta-red ----------------------------------------------------------------------
    def eye(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(RED, (len(u), 1))
        c[((u + 0.05) / 0.42) ** 2 + ((v + 0.02) / 0.5) ** 2 < 1] = INK
        c[r > 0.82] = INK
        return c
    de = unit(0.42, -0.9, 0.08)
    E = HC + de / math.sqrt(((de / HR) ** 2).sum())
    half = (0.029, 0.034)
    LID = srgb(YELLOW)
    right = np.cross((0, 0, 1.0), de); right /= np.linalg.norm(right)
    upv = np.cross(de, right)

    def eyes(state):
        k.decal(E, de, half, eye_look(state, eye, LID, INK, line=0.15), eps=0.006, rings=20, segs=56, mirror=True)
        if state == "Open":
            g = E + right * (-0.25 * half[0]) + upv * (0.42 * half[1])
            k.decal(g, de, (0.0075, 0.008), "#FFFFFF", eps=0.010, rings=5, segs=20, mirror=True)

    k.build(voxel=0.0055, smooth=4, band=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    mawile_mega(*args())
