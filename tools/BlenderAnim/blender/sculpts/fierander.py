# Fierander sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/fierander.py -- <the export's Fierander.glb> <out.glb>
# Carpathia's Fire starter's final stage (No. 10006), from its picture (10006-Fierander.jpg) and its brief
# (CP-dex-1-plateau.md, "6. Fierander"): the village smith (fierar) as a great upright salamander, Furnander's build at
# 1.25 (furnander.py's hearth, its bones exactly 1.25 times Furnander's) with the smith's iron laid over it: the crown
# of its broad flat head an anvil's face of iron, its horns standing out to both sides (across the head: in the
# picture the horn drawn larger is on the near side, so they are left and right, not fore and aft), riveted; glowing
# orange eyes under it, a long mouth whose seam glows orange with dark teeth along it, an iron boss at the jaw's hinge;
# riveted iron pauldrons, bracers on the forearms, plates on the thighs and knees, a gorget at the throat, a row of
# iron plates down the spine; the apron hung from two straps over the shoulders; the tail's end a heavy hammer-head of
# riveted iron bands, its striking face glowing, angled forward and out to +X. The picture's sparks and smoke are left
# out (specks and pipes at the game's size).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, sheet, srgb, inside, unit, tri, args, sibling, outward

fu = sibling("furnander")
sm = fu.sm
IRON = "#80848C"
IRON_D = "#5E6168"
RIVET = "#AEB2BA"
GLOW = "#FF8A2A"
EYE = srgb("#F27A22")
EYE_HEART = srgb("#FFD060")
MOUTH = "#F0781E"           # the mouth's glowing seam (Furnander's mouth line in the smith's colour)
K = 1.25
HELM_BROW, HELM_TOP = 0.83, 0.915        # the helmet's brow and its anvil face (Furnander's units)
APRON = (0.655, 0.2, 0.11, 0.02)         # the apron shortened to the knees and narrowed to the chest's width
EYE_DIR = (0.75, -0.62, 0.12)            # the eyes lower on the head, under the iron brow
BROW = (0.3, 0.3)


def S(*v):
    return np.array(v, float) * K


def plate(c, n, along, hw, hl, t, taper=0.82):
    """A plate of iron: a flat-faced block, its outer face a little smaller, along a direction on the body."""
    c = np.array(c, float); n = unit(*n)
    a = np.array(along, float); a = a - n * (a @ n); a /= np.linalg.norm(a)
    side = np.cross(n, a)
    pts = []
    for sx in (-1, 1):
        for sa in (-1, 1):
            pts.append(c - n * t + side * sx * hw + a * sa * hl)
            pts.append(c + n * t + side * sx * hw * taper + a * sa * hl * taper)
    return hull(pts)


def rivets(k, name, places, normals, r, bone):
    """A few large rivet heads, one loose piece."""
    k.add(name, sm.join(*[fu.disc(np.array(p) + unit(*n) * r * 0.2, n, r, r * 0.55) for p, n in zip(places, normals)]),
          RIVET, bone, fuse=False, surface="metal")


def band(c, d, r, length, rim=0.012):
    """An iron band round a limb: a short drum with a raised rim at each end (a lathe)."""
    L = length / 2
    return lathe(c, d, [(0, -L), (r + rim, -L), (r + rim, -L + rim * 1.4), (r, -L + rim * 1.6), (r, L - rim * 1.6),
                        (r + rim, L - rim * 1.4), (r + rim, L), (0, L)])


def dome(c, d, r, h):
    """A pauldron or a knee plate: a low dome of iron with a lip (a lathe), its axis d."""
    return lathe(c, d, [(0, -0.01 * K), (r, -0.01 * K), (r, 0.012 * K), (r * 0.82, h * 0.6), (r * 0.45, h * 0.92),
                        (0, h)])


def tail_end(k, K_, TAIL, TR):
    """The hammer-head of iron across the tail's tip: tapering to a peen in front, its back face glowing."""
    c = S(0.3, 0.39, 0.625)
    d = unit(0.5, -0.86, 0.0)                        # forward and out to +X: neither camera sees it end-on
    up = np.array([0, 0, 1.0])
    side = np.cross(d, up)
    hw, hh = 0.075 * K, 0.075 * K
    back_, front = c - d * 0.15 * K, c + d * 0.15 * K
    pts = []
    for sx in (-1, 1):
        for sz in (-1, 1):
            pts.append(back_ + side * sx * hw + up * sz * hh)
            pts.append(front + side * sx * hw * 0.55 + up * sz * hh * 0.9)
    k.add("hammer", hull(pts), IRON, "tail", fuse=False, surface="metal")
    # the bands: two raised rings of darker iron round the head
    for i, t in enumerate((-0.07, 0.04)):
        m = c + d * t * K
        s = 1.04 + 0.0 * i
        w = (1 - 0.45 * (t + 0.15) / 0.3) * hw
        q = []
        for sx in (-1, 1):
            for sz in (-1, 1):
                for e in (-0.022, 0.022):
                    q.append(m + d * e * K + side * sx * w * s * 1.035 + up * sz * hh * 1.035)
        k.add(f"hammerband{i}", hull(q), IRON_D, "tail", fuse=False, surface="metal")
        rivets(k, f"hammerrivet{i}", [m + side * w * 1.13, m + up * hh * 1.1], [side, up], 0.016 * K, "tail")
    # the striking face, glowing
    q = []
    for sx in (-1, 1):
        for sz in (-1, 1):
            for e in (0.0, 0.012):
                q.append(back_ - d * e * K + side * sx * hw * 0.7 + up * sz * hh * 0.7)
    k.add("hammerface", hull(q), sm.CHIP, "tail", fuse=False, glow=GLOW)


def back(k, K_, rows, TAIL, TR):
    """A row of iron plates down the spine and courses of brick scales beside it, as Furnander's."""
    for i, (z, y, ny) in enumerate(rows):
        c = S(0, y + 0.012, z)
        k.add(f"spine{i}", plate(c, (0, 1, ny), (0, -ny, 1), 0.05 * K, 0.04 * K, 0.016 * K), IRON, "body",
              fuse=False, surface="metal")
    rivets(k, "spinerivets", [S(0, y + 0.03, z) for z, y, ny in rows[:4]], [(0, 1, ny) for z, y, ny in rows[:4]],
           0.012 * K, "body")
    for i, (z, y, ny) in enumerate(rows[:-1]):
        for s in (-1, 1):
            p = S(0.1 * s, y - 0.03 - 0.004 * i, z - 0.04)
            k.add(f"brick{i}{'R' if s > 0 else 'L'}", fu.scale_block(p, (0.6 * s, 1, ny), S(0.04, 0.03, 0.03)),
                  fu.BRICK, "body")
    for i, t in enumerate((0.3, 0.5, 0.68)):
        j = int(t * (len(TAIL) - 1)); f = t * (len(TAIL) - 1) - j
        p = TAIL[j] * (1 - f) + TAIL[j + 1] * f
        r = TR[j] * (1 - f) + TR[j + 1] * f
        k.add(f"tailbrick{i}", fu.scale_block(p + np.array([0, 0, r * 0.82]), (0, 0, 1), S(0.034, 0.032, 0.026)),
              fu.BRICK, "tail")


def mouth_z(y):
    t = np.clip((y - (-0.33 * K)) / (0.27 * K), 0, 1)
    return (0.748 + 0.03 * t * t) * K


def armour(k, K_, F):
    H, HR = F["H"], F["HR"]

    # --- the anvil: a helmet of iron that is the upper half of the head, not a plate on it ------------------------
    # Its lower edge a heavy brow just over the eyes, its top flat as an anvil's face, the skull filling it; a
    # thick horn out of each side, round at the root, tapering to a blunt point and rising a little outward.
    zb, zt = HELM_BROW * K, HELM_TOP * K
    ring = []
    for i in range(16):
        a = 2 * math.pi * i / 16
        c, s = math.cos(a), math.sin(a)
        # the brow's ring: round the skull a little outside it, lower behind than in front
        ring.append((0.148 * K * c, H[1] + 0.155 * K * s, zb - (0.035 * K if s > 0.3 else 0.0)))
        # the anvil's face: a flat top a little smaller, squared off
        sq = 1.0 / max(abs(c), abs(s)) ** 0.35
        ring.append((0.13 * K * c * sq * 0.92, H[1] + 0.008 * K + 0.142 * K * s * sq * 0.92, zt))
    # the brow stands forward over the eyes and snout
    ring += [(x, H[1] - 0.168 * K, zb + 0.004 * K) for x in (-0.1 * K, 0.0, 0.1 * K)]
    k.add("helm", hull(ring), IRON, "head", fuse=False, surface="metal")
    # the skull filling it, so no black shows between helmet and head from any side
    k.add("crown", ell((0, H[1] - 0.005 * K, zb + 0.012 * K), (0.125 * K, 0.13 * K, 0.05 * K)), fu.BLACK, "head")
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        root = np.array((0.11, H[1] / K + 0.005, (HELM_BROW + HELM_TOP) / 2 + 0.006)) * K
        pts = [root, root + S(0.06, -0.004, 0.012), root + S(0.105, -0.006, 0.03), root + S(0.135, -0.006, 0.05)]
        k.add(f"horn{side}", tube(np.array(pts) * m, [0.04 * K, 0.034 * K, 0.024 * K, 0.014 * K]), IRON, "head",
              fuse=False, surface="metal")
    # rivets: a row along the brow's front and one on each side
    rivets(k, "helmrivets", [(x * K, H[1] - 0.163 * K + abs(x) * 0.25 * K, zb + 0.024 * K) for x in (-0.075, 0.0, 0.075)]
           + [(0.132 * K * s, H[1] - 0.06 * K, zb + 0.03 * K) for s in (-1, 1)],
           [(0, -1, 0.1)] * 3 + [(s, -0.3, 0.1) for s in (-1, 1)], 0.016 * K, "head")
    # the face: glowing cracks round the eyes and down the snout, thick and branching
    for d, seed in (((0.55, -0.75, -0.15), 41), ((-0.55, -0.75, -0.15), 42)):
        fu.glow_crack(k, H, d, 0.55, seed, 0.07, 0.3 * K, 0.08 * K, n=3)
    fu.glow_crack(k, H + S(0, -0.08, 0), (0.0, -0.85, 0.35), 0.45, 43, 0.07, 0.25 * K, 0.06 * K, n=2)

    # --- the belly's iron beside the shortened apron ---------------------------------------------------------
    for s, side in ((1, "R"), (-1, "L")):
        c = S(0.148 * s, -0.135, 0.42)
        k.add(f"fauld{side}", plate(c, (0.75 * s, -0.66, 0.05), (0, 0.1, 1), 0.045 * K, 0.07 * K, 0.014 * K), IRON,
              "body", fuse=False, surface="metal")
        rivets(k, f"fauldrivet{side}", [c + unit(0.75 * s, -0.66, 0.05) * 0.016 * K], [(0.75 * s, -0.66, 0.05)],
               0.013 * K, "body")
    # the boss at the jaw's hinge, each side
    for s, side in ((1, "R"), (-1, "L")):
        c = S(0.122 * s, -0.08, 0.745)
        n = unit(1.0 * s, -0.25, 0.1)
        k.add(f"boss{side}", fu.disc(c, n, 0.036 * K, 0.013 * K), IRON, "head", fuse=False, surface="metal")
        k.add(f"bossrivet{side}", fu.disc(c + n * 0.012 * K, n, 0.014 * K, 0.008 * K), RIVET, "head", fuse=False,
              surface="metal")
    # the mouth: a glowing seam with dark teeth along it (Furnander's line made wide)
    def seam(P):
        tooth = 0.005 * K * (1 + tri(P[:, 1] / (0.028 * K)))
        z0 = mouth_z(P[:, 1])
        return (inside(P[:, 2] - z0 - 0.004 * K, 0.0012 * K) * inside(z0 - 0.007 * K - P[:, 2], 0.0012 * K)
                * (P[:, 1] < -0.08 * K) * (P[:, 1] > -0.4 * K) * (P[:, 2] > 0.65 * K))
    def teeth(P):
        tooth = 0.0045 * K * (1 + tri(P[:, 1] / (0.03 * K)))
        z0 = mouth_z(P[:, 1])
        return (inside(P[:, 2] - z0 - 0.0045 * K, 0.001 * K) * inside(z0 + 0.0045 * K - tooth - P[:, 2], 0.001 * K)
                * (P[:, 1] < -0.1 * K) * (P[:, 1] > -0.36 * K) * (P[:, 2] > 0.65 * K))
    k.paint(seam, MOUTH)
    k.paint(teeth, "#2A2427")

    # --- the gorget at the throat, over the apron's fold, and the straps over the shoulders --------------------
    def gorget(u, v):
        a = (u - 0.5) * 2.3
        r = (0.125 + 0.012 * v) * K
        cx, cy = 0.0, -0.1 * K
        z = (0.705 + 0.05 * v) * K
        return np.stack([cx + r * np.sin(a), cy - r * np.cos(a) * np.ones_like(v), z * np.ones_like(u)], axis=-1)
    k.add("gorget", outward(sheet(gorget, 24, 6, 0.016 * K)), IRON, "body", fuse=False, surface="metal")
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"strap{side}", tube([S(0.115 * s, -0.215, 0.64), S(0.15 * s, -0.16, 0.71), S(0.15 * s, -0.02, 0.76),
                                    S(0.12 * s, 0.1, 0.68)], [0.017 * K] * 4), fu.LEATHER_D, "body", fuse=False,
              surface="leather")

    # --- the arms: riveted pauldrons, a bracer on each forearm -----------------------------------------------
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        d = unit(0.55 * s, 0.05, 0.85)
        c = S(0.17, -0.05, 0.62) * m + d * 0.052 * K
        k.add(f"pauldron{side}", dome(c, d, 0.098 * K, 0.06 * K), IRON, f"arm{side}", fuse=False, surface="metal")
        k.add(f"lame{side}", dome(c - d * 0.035 * K + S(0.018 * s, -0.005, -0.03), unit(0.85 * s, 0.0, 0.5),
                                  0.08 * K, 0.035 * K), IRON_D, f"arm{side}", fuse=False, surface="metal")
        rivets(k, f"pauldronrivets{side}", [c + d * 0.05 * K + S(0.03 * s, -0.04, 0.0)],
               [unit(0.5 * s, -0.6, 0.6)], 0.013 * K, f"arm{side}")
        e, w = F[f"elbow{side}"], F[f"wrist{side}"]
        ax = w - e
        mid = e + ax * 0.5
        k.add(f"bracer{side}", band(mid, ax, 0.053 * K, np.linalg.norm(ax) * 0.68, rim=0.01 * K), IRON,
              f"arm{side}", fuse=False, surface="metal")
        out_ = np.cross(unit(*ax), (0, 0, 1.0)) * s
        rivets(k, f"bracerrivet{side}", [mid + unit(*(out_ * 0.6 + np.array([0, -0.5, 0.6]))) * 0.064 * K],
               [out_ * 0.6 + np.array([0, -0.5, 0.6])], 0.012 * K, f"arm{side}")

    # --- the legs: a plate over each thigh, a knee plate, a band round the shin ---------------------------------
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        d = unit(0.8 * s, -0.45, 0.25)
        c = S(0.15, 0.0, 0.29) * m + d * 0.088 * K
        k.add(f"thighplate{side}", dome(c, d, 0.085 * K, 0.035 * K), IRON, f"leg{side}", fuse=False, surface="metal")
        rivets(k, f"thighrivet{side}", [c + d * 0.04 * K], [d], 0.015 * K, f"leg{side}")
        kd = unit(0.25 * s, -1, 0.15)
        kc = S(0.163, -0.115, 0.165) * m
        k.add(f"knee{side}", dome(kc, kd, 0.058 * K, 0.03 * K), IRON_D, f"leg{side}", fuse=False, surface="metal")
        k.add(f"greave{side}", band(S(0.168, -0.048, 0.1) * m, (0.0, 0.25, 1.0), 0.066 * K, 0.06 * K, rim=0.01 * K),
              IRON, f"leg{side}", fuse=False, surface="metal")


def eye(cut):
    """The smith's eye: Salamint's make, glowing orange with a hot heart, the pupil narrowed."""
    return sm.eye_open(EYE, cut=cut, pupil=0.27, glint=(-0.32, 0.2, 0.2), glow=EYE_HEART), srgb(fu.BLACK)


def fierce(cut):
    return None


if __name__ == "__main__":
    src, out = args()
    fu.hearth(src, out, K=K, smith=sys.modules[__name__])
