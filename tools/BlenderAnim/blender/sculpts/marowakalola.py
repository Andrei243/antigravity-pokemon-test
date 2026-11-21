# Marowak-Alola sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/marowakalola.py -- <the export's Marowak-Alola.glb> <out.glb>
#
# The design, from the references: Marowak's build and skull, its body gone a deep violet-black, the belly a pale
# grey-violet crossed by dark lines; the skull pale grey with a dark violet mark like a crack or a star on its brow;
# the bone pale grey, held behind the shoulders in the raised hand on -X, a green-teal flame burning at each end (pale
# at the root, green toward the wisping tips). The other arm reaches forward. The tail with its spike, dark.
# Built as marowak.py is (its frame copied: marowak.py's function takes no argument that reaches the colours or the
# bone's place), by Cubone's functions (sibling("cubone")).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, polygon, hull, outward, sibling, args

cb = sibling("cubone")
mw = sibling("marowak")

SKIN, BELLY, LINE, SOCKET = "#3E2C44", "#B9B0C2", "#7C7088", "#5A4A60"
SKULL, BONE, MARK = "#D2D2D8", "#D6D6DC", "#3A2742"
IRIS = "#7A6A80"
FLAME, FLAME_PALE = "#3FCF9F", "#BEF2CF"
INK, WHITE = cb.INK, cb.WHITE

ALOLA = dict(mw.MAROWAK, skin=SKIN, belly=BELLY)
CROWN_C, CROWN_R = mw.CROWN_C, mw.CROWN_R
SNOUT_C, SNOUT_R = mw.SNOUT_C, mw.SNOUT_R
EYE_AZ, EYE_EL = mw.EYE_AZ, mw.EYE_EL
EYE_SQ = mw.EYE_SQ

# The bone: through the raised -X hand, its lower end behind the body on +X, its upper end over the -X shoulder
HAND_L = np.array([-0.19, 0.05, 0.47])
BONE_D = unit(-0.72, -0.36, 0.6)
BONE_LO = HAND_L - BONE_D * 0.34
BONE_HI = HAND_L + BONE_D * 0.2


def flame(k, name, base, size, lean, bone, seed):
    """A green flame rising from a point: a pale round root and three tongues wisping up, each tapering and curling;
    the tongues one glowing piece, the root another."""
    rs = np.random.RandomState(seed)
    base = np.array(base, float)
    lean = np.array(lean, float)
    root = outward(ell(base + (0, 0, size * 0.12), (size * 0.47, size * 0.43, size * 0.3), sub=3))
    k.add(name + "root", root, FLAME_PALE, bone, fuse=False, glow=FLAME_PALE)
    Vs, Fs, off = [], [], 0
    for j, (dx, h, r0) in enumerate(((0.0, 1.25, 0.3), (-0.24, 0.85, 0.24), (0.26, 0.7, 0.22))):
        side = np.array([dx, 0.1 * (j - 1), 0.0]) * size
        pts, rad = [], []
        for t in np.linspace(0, 1, 6):
            wig = math.sin(t * 5.0 + seed + j) * 0.12 * t
            p = base + side * (1 - 0.4 * t) + lean * size * t * h * 0.5 \
                + np.array([wig, 0.04 * math.cos(t * 4 + j), 0]) * size + np.array([0, 0, 1.0]) * size * (0.15 + h * t)
            pts.append(p)
            rad.append(size * r0 * (1 - t) ** 0.8 + size * 0.045)
        V, F = outward(tube(pts, rad, seg=14, per=6))
        Vs.append(V); Fs.extend([[i + off for i in f] for f in F]); off += len(V)
    k.add(name + "tongues", (np.vstack(Vs), Fs), FLAME, bone, fuse=False, glow=FLAME)


def glare(lid):
    """Marowak's glare, the iris a dusky grey-violet."""
    I = srgb(IRIS)
    def look(u, v):
        c = np.tile(I, (len(u), 1))
        c[((u + 0.12) / 0.26) ** 2 + ((v + 0.05) / 0.62) ** 2 < 1] = INK
        top = 0.3 + 0.3 * u
        c[v > top] = lid
        c[np.abs(v - top) < 0.16] = INK
        share = np.hypot(u, v) / np.array([EYE_SQ(t) for t in np.arctan2(v, u)])
        c[share > 0.84] = INK
        return c
    return look


# The brow's mark: a ragged star of dark violet, on the forehead's -X side
STAR = [(0.0, 1.0), (0.18, 0.32), (0.85, 0.55), (0.32, 0.02), (0.7, -0.62), (0.05, -0.28), (-0.35, -0.95),
        (-0.22, -0.12), (-0.95, 0.05), (-0.22, 0.25)]


def marowak_alola(src, out):
    k = Kit(src, coat="fur")
    F = ALOLA
    cb.body(k, F)
    bc, br = np.array(F["belly_c"]), np.array(F["belly_r"])
    def lines(P):
        m = np.zeros(len(P))
        for z in (0.15, 0.19, 0.23, 0.27):
            m = np.maximum(m, inside(np.abs(P[:, 2] - z) - 0.0032, 0.0015))
        inb = inside(np.sqrt((((P - bc) / br)[:, [0, 2]] ** 2).sum(1)) - 0.93, 0.002) * (P[:, 1] < bc[1] + 0.03)
        return m * inb
    k.paint(lines, LINE, on=["torso"])
    cb.legs(k, F)

    k.add("neck", ell((0.0, 0.0, 0.37), (0.1, 0.095, 0.07)), SKIN, "head")
    k.add("crown", ell(CROWN_C, CROWN_R), SKULL, "head", surface="shell")
    k.add("snout", ell(SNOUT_C, SNOUT_R, rot=(-16, 0, 0), deform=mw.taper), SKULL, "head", surface="shell")
    k.add("jaw", ell((0.0, -0.12, 0.405), (0.085, 0.1, 0.045)), SKULL, "head", surface="shell")
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        b0 = np.array((0.07, 0.05, 0.56)) * m
        pts = [b0 + np.array((-0.03 * s, -0.03, 0)), b0 + np.array((0.03 * s, -0.03, 0)),
               b0 + np.array((-0.03 * s, 0.04, 0)), b0 + np.array((0.03 * s, 0.04, -0.01)),
               b0 + np.array((0.0, 0.0, -0.04)), b0 + np.array((0.04 * s, 0.1, 0.085))]
        k.add(f"horn{side}", hull(pts), SKULL, "head", fuse=False, surface="shell")

    # Arms: the right (+X) reaching forward, two white claws; the left (-X) raised beside the head, holding the bone
    cb.arm(k, F, "R", [(0.11, -0.02, 0.319), (0.15, -0.09, 0.3), (0.175, -0.16, 0.28)], [0.042, 0.037, 0.034],
           (0.178, -0.18, 0.278), (0.036, 0.038, 0.034))
    for dz in (-0.014, 0.014):
        b = np.array((0.18, -0.21, 0.278 + dz))
        k.add(f"fclaw{dz > 0}", hull([b + (-0.008, 0.01, -0.007), b + (0.008, 0.01, -0.007), b + (-0.008, 0.01, 0.007),
                                      b + (0.008, 0.01, 0.007), b + (0.0, -0.022, 0.0)]),
              cb.CLAW, "armR", fuse=False, surface="shell")
    cb.arm(k, F, "L", [(-0.11, -0.02, 0.319), (-0.16, 0.0, 0.38), (-0.185, 0.04, 0.44)], [0.042, 0.037, 0.034],
           HAND_L, (0.038, 0.036, 0.038))
    cb.SKULL = BONE
    cb.club(k, BONE_LO, BONE_HI, 0.024, "armL")
    flame(k, "flamehi", BONE_HI + BONE_D * 0.03, 0.235, (-0.25, 0.1, 0), "armL", 1)
    flame(k, "flamelo", BONE_LO - BONE_D * 0.03 + (0, 0, -0.01), 0.22, (0.35, 0.15, 0), "armL", 4)

    # The eye holes, the creases along the snout and the brow's dark mark
    holes = []
    for s in (1, -1):
        p, n = cb.on_ell(CROWN_C, CROWN_R, s * EYE_AZ, EYE_EL)
        holes.append((p, n, s))
        cb.socket(k, p, n, (0.048, 0.03), s, tilt=14, rim=0.22, color=SOCKET)
        mw.stroke(k, p + np.array([-0.012 * s, -0.04, -0.03]), p + np.array([-0.04 * s, -0.13, -0.05]))
    p, n = cb.on_ell(CROWN_C, CROWN_R, -22, 30)
    k.decal(p, n, (0.052, 0.05), MARK, up=cb.tilted_up(n, -12), eps=0.003, outline=polygon(STAR), rings=16, segs=90)

    k.build(voxel=0.006, smooth=4, band=0.006)

    look_open = glare(srgb(SOCKET))

    def eyes(state):
        f = eye_look(state, look_open, srgb(SOCKET), INK, line=0.18)
        for p, n, s in holes:
            cb.mirrored_decal(k, p, n, (0.034, 0.022), f, s, up=cb.tilted_up(n, 14 * s), eps=0.0065,
                              outline=EYE_SQ, rings=20, segs=56)

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    marowak_alola(*args())
