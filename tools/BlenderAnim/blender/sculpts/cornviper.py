# Cornviper sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cornviper.py -- <the export's Cornviper.glb> <out.glb>
# Carpathia No. 10071, the horned viper (CP-dex-2-mountains.md, 71), grown from Viperin (viperin.py: the same line laid
# through the chain of joints as ekans.py lays it, its palette, its zigzag, its belly of broad scales, its slit eyes
# under a brow). From its picture: a long, heavy ash-grey viper coiled in two great loops, the neck rising in an S to a
# raised head; the bold black zigzag down its back; craggy plates of limestone along the sides of the coils and a ridge
# of rocky spikes down the back of the neck; a stone horn jutting up from the tip of the snout; rocky spikes behind the
# eyes, a fierce eye under a heavy brow; the jaws open on two long fangs and a violet tongue, violet venom dripping; the
# tail rising behind at the right to a club of rocky scales.
# The export's chain: body and tail (0.36, 0.44, 0.3) where the tail ends, seg1 (0.243, -0.177, 0.095) round the outer
# coil to seg5, seg6 to seg8 the inner coil, seg9 (0, -0.1, 0.46) up the neck, head (0, -0.12, 0.7).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, unit, args, sibling

v = sibling("viperin")
ek = v.ek

GREY = "#5E5C5C"           # Viperin's grey grown paler: ash
STONE = "#86837A"          # the limestone plates, the horn and the club
STONE_DARK = "#646058"
VENOM = "#9A4ED0"          # the tongue, the venom
MAW = "#5A2A6E"            # inside the mouth
FANG = "#F0ECE2"

LINE = [(0.335, 0.425, 0.45), (0.35, 0.445, 0.35), (0.36, 0.44, 0.25), (0.335, 0.26, 0.13), (0.243, -0.177, 0.1),
        (-0.06, -0.274, 0.104), (-0.257, -0.052, 0.108), (-0.14, 0.199, 0.115), (0.116, 0.191, 0.13),
        (0.203, -0.026, 0.18), (0.052, -0.178, 0.215), (-0.127, -0.107, 0.24), (-0.125, -0.06, 0.36),
        (-0.035, -0.075, 0.5), (0.0, -0.11, 0.61), (0.0, -0.16, 0.655)]
RADS = [0.032, 0.045, 0.062, 0.086, 0.1, 0.102, 0.102, 0.102, 0.1, 0.098, 0.096, 0.094, 0.088, 0.082, 0.076, 0.072]
BONES = ["tail", "tail", "body", "body", "seg1", "seg2", "seg3", "seg4", "seg5", "seg6", "seg7", "seg8", "seg8",
         "seg9", "seg9"]
HC = np.array([0.0, -0.26, 0.69])          # the head's middle
HS = 1.95


def rock(c, size, seed, frame=None, n=11):
    """A craggy limestone plate: the hull of points scattered round c (sizes along the frame's axes)."""
    rs = np.random.RandomState(seed)
    P = rs.normal(size=(n, 3))
    P /= np.linalg.norm(P, axis=1, keepdims=True)
    P *= (0.75 + 0.35 * rs.rand(n))[:, None]
    P = P * np.array(size)
    if frame is not None:
        P = P @ np.array(frame)
    return hull(np.array(c) + P)


def frame_at(line, s):
    p, T, r = line.at(s)
    side = np.cross(T, (0, 0, 1.0))
    if np.linalg.norm(side) < 1e-3:
        side = np.array([1.0, 0, 0])
    side /= np.linalg.norm(side)
    up = np.cross(side, T)
    if up[2] < 0 and abs(T[2]) < 0.9:
        up = -up
    return p, T, r, side, up


def head(k):
    """Viperin's arrow-shaped head grown long and heavy, its jaws open: the upper jaw and its brows, the lower jaw
    dropped, the maw carved between, the horn on the snout's tip, the fangs, the tongue and the venom."""
    def hp(p):
        return HC + np.array(p, float) * HS
    def arrow(V):
        V = V.copy()
        t = np.clip(-V[:, 1], 0, 1)
        V[:, 0] *= 1 - 0.4 * t
        V[:, 2] *= 1 - 0.15 * t
        return V
    k.add("skull", ell(hp((0, -0.01, 0.006)), np.array((0.08, 0.105, 0.038)) * HS, rot=(-4, 0, 0), deform=arrow),
          GREY, "head")
    k.add("jaws", ell(hp((0, 0.045, -0.006)), np.array((0.09, 0.062, 0.046)) * HS), GREY, "head")
    k.add("chin", ell(hp((0, -0.03, -0.045)), np.array((0.06, 0.08, 0.02)) * HS, rot=(-20, 0, 0), deform=arrow),
          v.BELLY, "head")
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"brow{side}", ell(hp((0.046 * s, -0.03, 0.034)), np.array((0.028, 0.036, 0.016)) * HS,
                                 rot=(0, 0, -16 * s)), GREY, "head")
    k.add("maw", ell(hp((0, -0.06, -0.024)), np.array((0.052, 0.07, 0.02)) * HS, rot=(-8, 0, 0)), MAW, "head",
          carve=True)
    # the pale lip of the upper jaw, the stripe behind the eye, the dark wedge on the crown
    lip = hp((0, 0, -0.012))[2]
    k.paint(lambda P: inside(P[:, 2] - lip, 0.002) * inside(np.linalg.norm(P - HC, axis=1) - 0.15 * HS, 0.004)
            * (P[:, 1] < HC[1] + 0.06 * HS), v.BELLY, on=["skull", "jaws"])
    def stripe(P):
        q = (P - HC) / HS
        zl = 0.008 - 0.12 * (q[:, 1] + 0.03)
        return inside(np.abs(q[:, 2] - zl) - 0.0085, 0.0015) * (np.abs(q[:, 0]) > 0.046) * (q[:, 1] > -0.02) \
            * (q[:, 1] < 0.085)
    k.paint(stripe, v.BLACK, on=["skull", "jaws", "brow"])
    def crown(P):
        q = (P - HC) / HS
        return inside(np.abs(q[:, 0]) - 0.012 - 0.22 * np.clip(q[:, 1] + 0.02, 0, 1), 0.0015) * (q[:, 1] > -0.02) \
            * (q[:, 2] > 0.02)
    k.paint(crown, v.BLACK, on=["skull", "jaws", "nape"])

    # the stone horn: up and a little forward from the tip of the snout, craggy
    base = hp((0, -0.088, 0.012))
    tip = base + unit(0, -0.22, 1) * 0.27
    pts = [base + (0.056, 0.036, 0), base + (-0.056, 0.036, 0), base + (0.05, -0.036, -0.006),
           base + (-0.05, -0.036, -0.006), base + (0, 0.058, 0.0), base + (0, -0.055, 0)]
    for f, w in ((0.3, 0.044), (0.6, 0.029), (0.82, 0.016)):
        m = base + (tip - base) * f
        pts += [m + (w, 0.006, 0), m + (-w * 0.9, 0.004, 0.004), m + (0, w * 1.05, -0.004), m + (0.006, -w, 0.008)]
    pts.append(tip)
    k.add("horn", hull(pts), STONE, "head", fuse=False, surface="shell")
    # rocky spikes behind the eyes, laid back
    for s, side in ((1, "R"), (-1, "L")):
        for j, (y, z, ln) in enumerate(((0.02, 0.03, 0.07), (0.055, 0.022, 0.06))):
            b = hp((0.062 * s, y, z))
            d = unit(0.45 * s, 1.0, 0.55)
            a1 = np.cross(d, (0, 0, 1.0)); a1 /= np.linalg.norm(a1)
            a2 = np.cross(a1, d)
            k.add(f"spur{side}{j}", hull([b + a1 * 0.022, b - a1 * 0.022, b + a2 * 0.018, b - a2 * 0.018, b + d * ln]),
                  STONE, "head", fuse=False, surface="shell")
    # two long fangs from the upper jaw's front corners, a short drip of venom hanging from each, the tongue in the
    # open mouth
    for s, side in ((1, "R"), (-1, "L")):
        a = hp((0.026 * s, -0.088, -0.012))
        b = hp((0.024 * s, -0.082, -0.05))
        k.add(f"fang{side}", blade([a, (a + b) / 2, b], [0.016, 0.012, 0.005], (0, 1, 0), 0.8), FANG, "head",
              fuse=False, surface="shell")
        d0 = b + np.array((0, 0, -0.002))
        k.add(f"drip{side}", tube([d0, d0 + (0.001 * s, -0.002, -0.025), d0 + (0.002 * s, -0.001, -0.042)],
                                  [0.014, 0.015, 0.019]), VENOM, "head", fuse=False)
    t0, t1, t2 = hp((0, -0.04, -0.026)), hp((0, -0.085, -0.03)), hp((0, -0.106, -0.04))
    k.add("tongue", tube([t0, t1, t2], [0.018, 0.017, 0.016], seg=12), VENOM, "head", fuse=False)
    return hp


def cornviper(src, out):
    k = Kit(src, coat="scales")
    line = ek.Line(LINE, RADS, rise=(0.25, 0.36))
    line.tubes(k, BONES, GREY)
    k.add("nape", tube([LINE[-2], LINE[-1], HC + (0, 0.07, -0.01)], [0.076, 0.072, 0.07]), GREY, "head")
    hp = head(k)

    # the belly of broad scales, the zigzag down the back
    k.paint(v.front_only(line, ek.belly(line, 3.0, 14.5, width=0.5, edge=0.004), 9.5, 13.0), v.BELLY, on=["seg"])
    k.paint(v.front_only(line, v.scutes(line, 3.0, 14.5, 0.05), 9.5, 13.0), v.SCUTE, on=["seg"])
    band, rim = v.zigzag(line, 0.6, 14.6, 0.12, 0.8, 0.36, 0.15, below=0.63)
    k.paint(band, v.ZIG, on=["seg", "nape"])
    k.paint(rim, v.BLACK, on=["seg", "nape"])

    # craggy limestone plates along the sides of the coils, both sides, set in the flank
    i = 0
    for s in np.arange(3.6, 10.3, 0.5):
        p, T, r, side, up = frame_at(line, s)
        for w in (1, -1):
            for a in (0.55,):
                c = p + r * 0.93 * (math.cos(a) * side * w + math.sin(a) * up)
                fr = [T, side * w, up]
                k.add(f"crag{i}", rock(c, (0.07, 0.034, 0.046), 7100 + i, frame=fr), STONE if i % 3 else STONE_DARK,
                      "seg1" if s < 4 else ek_bone(s), fuse=False, surface="shell")
                i += 1
    # the ridge of rocky spikes down the back of the neck and over the inner coil
    j = 0
    for s in np.arange(11.4, 14.4, 0.34):
        p, T, r, side, up = frame_at(line, s)
        back = up if s < 11.7 else unit(0, 1, 0.25)
        c = p + back * r * 0.82
        d = unit(*(back + 0.6 * -T * 0 + np.array((0, 0.35, 0.3))))
        a1 = side
        a2 = np.cross(a1, d); a2 /= np.linalg.norm(a2)
        ln = 0.075 if s < 13.5 else 0.05
        k.add(f"ridge{j}", hull([c + a1 * 0.026, c - a1 * 0.026, c + a2 * 0.03, c - a2 * 0.03, c + d * ln,
                                 c + d * ln * 0.55 + a1 * 0.012]), STONE, ek_bone(s), fuse=False, surface="shell")
        j += 1
    # the tail's club of rocky scales
    tip = np.array(LINE[0])
    for n, (dx, dy, dz, sz) in enumerate(((0.0, 0.0, 0.03, 0.05), (0.03, 0.02, -0.03, 0.045),
                                          (-0.03, -0.015, -0.035, 0.045), (0.0, 0.03, -0.08, 0.04),
                                          (0.03, -0.03, -0.09, 0.038), (-0.03, 0.02, -0.12, 0.035))):
        k.add(f"club{n}", rock(tip + (dx, dy, dz), (sz * 0.8, sz * 0.8, sz), 7300 + n), STONE, "tail", fuse=False,
              surface="shell")
    for n, d in enumerate(((0.3, 0.2, 1), (-0.35, 0.1, 0.9), (0.1, -0.4, 0.9))):
        d = unit(*d)
        b = tip + (0, 0, 0.04)
        a1 = np.cross(d, (1.0, 0, 0)); a1 /= np.linalg.norm(a1)
        a2 = np.cross(a1, d)
        k.add(f"clubspike{n}", hull([b + a1 * 0.022, b - a1 * 0.022, b + a2 * 0.02, b - a2 * 0.02, b + d * 0.08]),
              STONE, "tail", fuse=False, surface="shell")

    # the eyes: fierce, under heavy brows sloping down to the snout
    E = hp((0.05, -0.036, 0.014))
    de = unit(0.78, -0.48, 0.36)
    lay = v.lay_eyes(k, E, de, (0.022 * HS, 0.022 * HS), GREY, cut=v.brow_cut(0.3, 0.38), fierce_slope=0.6)

    k.build(voxel=0.0085, smooth=4, band=0.01, wband=0.08)
    k.eye_states(lay)
    k.export(out)


def ek_bone(s):
    """The bone of the line's segment at parameter s."""
    return BONES[min(int(s), len(BONES) - 1)]


if __name__ == "__main__":
    cornviper(*args())
