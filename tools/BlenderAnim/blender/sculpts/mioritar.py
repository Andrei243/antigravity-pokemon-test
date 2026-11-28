# Mioritar sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/mioritar.py -- <the export's Mioritar.glb> <out.glb>
# Carpathia No. 10022 (plan 46), the project's own design: docs/art/reference/gemini/carpathia/10022-Mioritar.jpg and
# CP-dex-1-plateau.md, "22. Mioritar: the flock guardian". Ciobipup grown (ciobipup.py's cord, tassel, eye, tufts and
# curled plume): from the picture, a huge shaggy mountain sheepdog standing four-square like a sentinel; its coat in
# long pointed cords of white with grey among them, hanging from the back and flanks to a third of its height and
# feathering the legs; massive legs as wide as their paws, big paws with dark claws; a big square head (a third of its
# height) carried forward, a fringe of fur hanging over the brow, grey patches round calm brown eyes cut by heavy brows,
# a black nose on a long muzzle, a beard and jowls hanging under it; grey ears hanging beside the head; a broad dark
# leather collar studded with iron spikes, and under it the twisted red and white cord with its tassel (the picture's,
# not in the brief's Look line); a great plumed tail curled up over the rump.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, ring_band, srgb, inside, unit, eye_look, polygon, args, outward, \
    sibling

C = sibling("ciobipup")

WHITE, GREY, PALE = "#EEEAE0", "#C6C2BA", "#DCD6CA"
PATCH, EAR, NOSE, CLAW = "#6E6C70", "#A29E98", "#1C1A1E", "#3C3A3C"
LEATHER, IRON = "#4E3628", "#9EA2AA"
IRIS, PUPIL, INK = "#6E4A30", "#141012", "#ECE6DA"

# The frame (joints: body (0, 0.02, 0.45), head (0, -0.26, 0.58), ears (+-0.11, -0.3, 0.74), legs (+-0.12, -0.2,
# 0.32), hinds (+-0.12, 0.22, 0.32), tail (0, 0.32, 0.54))
HC = np.array([0.0, -0.375, 0.69])
HR = np.array([0.165, 0.152, 0.152])
MC = np.array([0.0, -0.505, 0.638])
MR = np.array([0.088, 0.085, 0.072])
BROW = (0.42, 0.22)            # the eye's top under the brow: v = BROW[0] + BROW[1] u (u outward)


def clipped(top, n=72):
    pts = []
    for th in np.linspace(0, 2 * math.pi, n, endpoint=False):
        du, dv = math.cos(th), math.sin(th)
        t = 1.0
        if dv > top(du):
            lo, hi = 0.0, 1.0
            for _ in range(40):
                t = (lo + hi) / 2
                if t * dv > top(t * du):
                    hi = t
                else:
                    lo = t
            t = lo
        pts.append((t * du, t * dv))
    return pts


def lock(k, name, base, d, length, r, col, bone, flat):
    """A long pointed cord of fleece."""
    C.tuft(k, name, base, d, length, r, col, bone, flat=flat, f=0.5, tip=0.18)


def mioritar(src, out):
    k = Kit(src, coat="fur")
    rs = np.random.RandomState(1022)

    # The great trunk, the fleece's mass over it
    k.add("chest", ell((0, -0.2, 0.5), (0.19, 0.14, 0.19)), WHITE, "body")
    k.add("trunk", ell((0, 0.02, 0.5), (0.195, 0.32, 0.175)), WHITE, "body")
    k.add("fleece", ell((0, 0.02, 0.54), (0.215, 0.34, 0.155)), WHITE, "body")
    k.add("neck", tube([(0, -0.17, 0.55), (0, -0.3, 0.66)], [0.13, 0.12]), WHITE, "head")

    # Massive legs as wide as their paws, big paws with dark claws
    for s, nm in ((1, "R"), (-1, "L")):
        for leg, y, top in (("leg", -0.2, 0.42), ("hind", 0.22, 0.42)):
            bone = f"{leg}{nm}"
            if leg == "hind":
                k.add(f"thigh{nm}", ell((0.12 * s, 0.22, 0.42), (0.1, 0.13, 0.14)), WHITE, bone)
            else:
                k.add(f"shoulder{nm}", ell((0.12 * s, -0.2, 0.44), (0.095, 0.11, 0.13)), WHITE, bone)
            k.add(f"{leg}{nm}", tube([(0.122 * s, y, top), (0.125 * s, y - 0.005, 0.2), (0.126 * s, y - 0.012, 0.06)],
                                     [0.085, 0.08, 0.076]), WHITE, bone)
            pc = np.array((0.127 * s, y - 0.035, 0.045))
            k.add(f"{leg}paw{nm}", ell(pc, (0.088, 0.1, 0.042)), PALE, bone)
            for j, dx in enumerate((-0.045, -0.015, 0.015, 0.045)):
                k.add(f"{leg}toe{nm}{j}", ell(pc + (dx, -0.075, -0.006), (0.024, 0.026, 0.03)), PALE, bone)
                a = pc + np.array((dx, -0.1, -0.012))
                k.add(f"{leg}claw{nm}{j}", hull([a + (0.01, 0, 0), a - (0.01, 0, 0), a + (0, 0, 0.012),
                                                  a + (0, 0.008, -0.008), a + (0, -0.028, -0.014)]),
                      CLAW, bone, fuse=False, surface="shell")
            # Feathering round the leg, hanging to the paw
            for j, ang in enumerate((-0.6, 0.3, 1.2, 2.2)):
                o = np.array((math.cos(ang) * s, -math.sin(ang), 0.0))
                base = np.array((0.124 * s, y, 0.27)) + o * 0.07
                lock(k, f"{leg}feather{nm}{j}", base, o * 0.3 + np.array((0, 0, -1)), rs.uniform(0.12, 0.17),
                     0.04, WHITE if j % 3 else GREY, bone, flat=o)

    # The cords of fleece hanging from the back and flanks, uneven, white with grey among them
    n = 0
    for row, (z, count, reach) in enumerate(((0.46, 15, (0.15, 0.22)), (0.58, 15, (0.12, 0.18)),
                                             (0.66, 11, (0.09, 0.13)))):
        for j in range(count):
            t = (j + 0.5 * (row % 2)) / count
            y = -0.25 + 0.55 * t + rs.uniform(-0.012, 0.012)
            f = math.sqrt(max(0.05, 1 - ((y - 0.02) / 0.36) ** 2))
            for s, nm in ((1, "R"), (-1, "L")):
                x = 0.2 * f * (1.02 - 0.25 * row) * s
                base = np.array((x, y, z))
                o = np.array((s * 1.0, 0.1, 0.0))
                d = o * (0.35 + 0.25 * row) + np.array((0, 0.12, -1.0))
                col = GREY if rs.rand() < 0.2 else WHITE
                lock(k, f"cord{n}{nm}", base, d, rs.uniform(*reach), 0.048 - 0.006 * row, col, "body",
                     flat=(s, 0.1, 0.25 * row))
            n += 1
    # The back's ridge in short locks laid back, and the rump's fall
    for i, y in enumerate((-0.15, -0.04, 0.07, 0.18)):
        for s, nm in ((1, "R"), (-1, "L")):
            lock(k, f"ridge{i}{nm}", (0.05 * s, y, 0.68), (0.6 * s, 0.7, -0.15), 0.1, 0.04,
                 GREY if (i + (s > 0)) % 3 == 0 else WHITE, "body", flat=(0, 0, 1))
    for i, x in enumerate((-0.12, -0.04, 0.04, 0.12)):
        lock(k, f"rump{i}", (x, 0.33, 0.47), (x * 2, 0.35, -1), rs.uniform(0.14, 0.19), 0.045,
             GREY if i == 2 else WHITE, "body", flat=(0, 1, 0))
    # The shaggy chest under the beard
    for i, x in enumerate((-0.14, -0.09, 0.09, 0.14)):
        lock(k, f"bib{i}", (x, -0.3, 0.48), (x * 1.5, -0.25, -1), rs.uniform(0.14, 0.19), 0.045,
             GREY if i == 2 else WHITE, "body", flat=(0, 1, 0))

    # The big head carried forward, a long muzzle, a black nose
    k.add("head", ell(HC, HR), WHITE, "head")
    k.add("muzzle", ell(MC, MR), WHITE, "head")
    k.add("nose", ell(MC + (0, -0.078, 0.028), (0.036, 0.024, 0.026), rot=(10, 0, 0)), NOSE, "head", fuse=False,
          surface="shell")
    # A beard and jowls hanging under the muzzle
    for i, (x, ln) in enumerate(((-0.07, 0.11), (-0.035, 0.14), (0.0, 0.15), (0.035, 0.14), (0.07, 0.11))):
        lock(k, f"beard{i}", MC + (x, -0.035 + 0.02 * abs(x) / 0.07, -0.045), (x * 1.2, -0.15, -1), ln * 0.8, 0.034,
             WHITE if i != 1 else PALE, "head", flat=(0, 1, 0))
    for s, nm in ((1, "R"), (-1, "L")):
        for j, (dz, ln) in enumerate(((0.0, 0.12), (-0.06, 0.13))):
            lock(k, f"jowl{nm}{j}", HC + (s * 0.13, -0.05, dz - 0.03), (s * 0.45, 0.0, -1), ln, 0.04, WHITE,
                 "head", flat=(1, 0, 0))
        # Grey ears hanging beside the head
        k.add(f"ear{nm}", blade([(0.14 * s, -0.33, 0.77), (0.185 * s, -0.34, 0.69), (0.19 * s, -0.345, 0.58)],
                                 [0.05, 0.062, 0.042], (1, 0, 0.2 * s), 0.42), EAR, f"ear{nm}")
        # Heavy brows over the eyes
        k.add(f"brow{nm}", ell(HC + (s * 0.07, -0.112, 0.074), (0.05, 0.028, 0.02), rot=(10, 0, -14 * s)), PALE,
              "head")
    # The fringe hanging over the brow, its ends above the eyes
    for i, x in enumerate((-0.1, -0.05, 0.0, 0.05, 0.1)):
        lock(k, f"fringe{i}", HC + (x, -0.085, 0.14), (x * 1.5, -0.55, -0.3), 0.065 if i % 2 == 0 else 0.055,
             0.036, WHITE if i != 3 else GREY, "head", flat=(0, 0.6, 0.8))
    for i, (x, d) in enumerate(((-0.08, (-0.5, 0.3, 1)), (0.0, (0, 0.5, 1)), (0.08, (0.5, 0.3, 1)))):
        lock(k, f"crown{i}", HC + (x, 0.0, 0.125), d, 0.05, 0.04, WHITE, "head", flat=(0, 1, 0))

    # The great plumed tail curled over the rump, its plane turned 35 degrees out to +X
    C.plume(k, (0, 0.33, 0.0), (0.57, 0.82, 0), [(0.0, 0.53), (0.06, 0.63), (0.05, 0.74), (-0.03, 0.8),
                                                (-0.12, 0.775), (-0.15, 0.7), (-0.11, 0.65), (-0.07, 0.69)],
            [0.07, 0.085, 0.09, 0.088, 0.08, 0.068, 0.055, 0.042], WHITE, "tail",
            (0.014, 0.055, 0.009, GREY), (-0.06, 0.71), n_tufts=8, lift=0.0)
    for i, (s_, z, d) in enumerate(((0.07, 0.6, (0.7, 0.6, -0.6)), (0.07, 0.72, (0.8, 0.4, -0.2)),
                                    (-0.16, 0.7, (-0.4, 0.2, -0.6)))):
        h = np.array((0.57, 0.82, 0))
        base = np.array((0, 0.33, z)) + h * s_
        lock(k, f"plumelock{i}", base, d, 0.08, 0.045, WHITE if i != 1 else PALE, "tail", flat=(1, 0, 0))

    # The broad leather collar studded with iron spikes, the shepherd's cord under it and its tassel
    cc = np.array((0, -0.25, 0.605))
    axis = unit(0, -0.74, 0.67)
    k.add("collar", ring_band(cc, axis, 0.137, 0.075, thick=0.022), LEATHER, "head", fuse=False, surface="leather")
    a, e1, e2 = C.ring_frame(axis)
    for i in range(11):
        th = 2 * math.pi * i / 11
        rad = math.cos(th) * e1 + math.sin(th) * e2
        if (rad @ np.array((0, 1.0, 0))) > 0.75:
            continue                        # none hidden behind the neck
        p = cc + rad * 0.146
        w = np.cross(rad, a)
        k.add(f"spike{i}", hull([p + w * 0.018 + a * 0.018, p - w * 0.018 + a * 0.018, p + w * 0.018 - a * 0.018,
                                 p - w * 0.018 - a * 0.018, p + rad * 0.055]), IRON, "head", fuse=False,
              surface="metal")
    cc2 = cc - axis * 0.052
    C.cord(k, cc2, axis, 0.128, 0.013, 30, "head")
    C.tassel(k, cc2 - e2 * 0.142 + (0, -0.04, -0.005), 0.05, "head")

    # Grey patches round the eyes
    E = C.on_ell(HC, HR, 0.067, 0.698) + (0, -0.002, 0)
    EN = unit(0.5, -1, 0.1)
    def blob(th):
        return 1.0 + 0.08 * math.cos(2 * th - 0.6) - 0.06 * math.sin(th) - 0.04 * math.cos(3 * th)
    k.decal(E + (0.006, 0, -0.008), EN, (0.052, 0.045), PATCH, eps=0.003, outline=blob, rings=12, segs=72,
            mirror=True)

    ob = k.build(voxel=0.008, smooth=3, band=0.006)
    sibling("arichip").board(ob)

    # Calm brown eyes cut flat by the brows
    top = lambda u: BROW[0] + BROW[1] * u
    base_look = C.dog_eye(IRIS, PUPIL)
    Kc = srgb(PATCH)
    def look(u, v):
        c = base_look(u, v)
        c[v > top(u) - 0.14] = srgb("#3A3436")
        c[v > top(u)] = srgb(PATCH)
        return c
    lid, ink = srgb(PATCH), srgb(INK)
    def lay(state):
        k.decal(E, EN, (0.032, 0.032), eye_look(state, look, lid, ink), eps=0.005, rings=20, segs=56, mirror=True)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    mioritar(*args())
