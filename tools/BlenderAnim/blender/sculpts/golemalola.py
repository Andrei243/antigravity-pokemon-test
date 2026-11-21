# Alolan Golem sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/golemalola.py -- <the export's Golem-Alola.glb> <out.glb>
# Golem's frame (golem.py, which builds as it is read, so its way is copied here; the skeleton is the same): the great
# shell of flat plates parted by grooves, a reptile's head low at its front with a gaping mouth, short arms and thick
# legs with white claws. Grown from Geodude-Alola (geodudealola.py, through sibling): the plates grey-green, the head
# and limbs pale, a black mask of iron sand over the brow and down the nose, a shaggy black beard of iron sand under
# the jaw, and on its back the railgun: two great black bars of rock rising from the shell, the left one set with
# three amber crystals, and a round black rock between their feet.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, boxy, eye_look, args, sibling

g = sibling("geodudealola")

PLATE, GROOVE = "#97A392", "#4A524C"
SKIN, SKINDARK = "#C9BEB1", "#A99C8E"
MASK = g.BLACK
CLAW, MOUTH = "#F4F4F2", "#E0788A"
C = np.array([0.0, 0.06, 0.54])          # the shell's middle, on the body bone
R = 0.36                                 # its radius


def shade(h, f):
    c = np.clip(np.array([int(h[i:i + 2], 16) for i in (1, 3, 5)]) * f, 0, 255).astype(int)
    return "#%02X%02X%02X" % tuple(c)


def cone(base, d, length, r, sides=6, bend=None):
    """A claw or a spike: a flat-faced cone from a ring of radius r at base to a point length along d."""
    a, b, d = g.frame(d)
    base = np.asarray(base, float)
    tip = base + d * length + (np.asarray(bend, float) if bend is not None else 0)
    pts = [tip]
    for i in range(sides):
        t = 2 * math.pi * i / sides
        pts.append(base + r * (math.cos(t) * a + math.sin(t) * b))
        pts.append(base + d * length * 0.35 + 0.8 * r * (math.cos(t) * a + math.sin(t) * b))
    return hull(np.array(pts))


def golem_alola(src, dest):
    k = Kit(src, coat="fur")

    # The shell: a dark sphere under the plates, showing only in the grooves between them
    k.add("shell", ell(C, (R, R * 0.97, R)), GROOVE, "body", surface="shell")

    # The plates (golem.py's spherical Voronoi), each a flat-topped slab of cut stone
    N = 46
    i = np.arange(N) + 0.5
    ph = np.arccos(1 - 2 * i / N); th = math.pi * (1 + 5 ** 0.5) * i
    P = np.stack([np.cos(th) * np.sin(ph), np.sin(th) * np.sin(ph), np.cos(ph)], 1)
    rs = np.random.RandomState(76)
    P += rs.normal(size=P.shape) * 0.06
    P /= np.linalg.norm(P, axis=1, keepdims=True)
    HEADDIR = unit(0, -1, -0.32)
    GAP = 0.011
    plates = []
    for n_, c in enumerate(P):
        if c @ HEADDIR > math.cos(math.radians(19)) or c[2] < -0.9:
            continue
        t1 = np.cross(c, (0, 0, 1.0))
        if np.linalg.norm(t1) < 1e-3:
            t1 = np.cross(c, (1.0, 0, 0))
        t1 /= np.linalg.norm(t1); t2 = np.cross(c, t1)
        poly = [np.array(p) * 0.3 for p in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
        for m in P:
            if np.allclose(m, c):
                continue
            dmn = m - c
            a, b = t1 @ dmn, t2 @ dmn
            lim = R * (1 - c @ m) - GAP * math.hypot(a, b)
            new = []
            for j in range(len(poly)):
                p, q = poly[j], poly[(j + 1) % len(poly)]
                fp, fq = a * p[0] + b * p[1] - lim, a * q[0] + b * q[1] - lim
                if fp <= 0:
                    new.append(p)
                if fp * fq < 0:
                    new.append(p + (q - p) * (fp / (fp - fq)))
            poly = new
            if len(poly) < 3:
                break
        if len(poly) < 3:
            continue
        poly = np.array(poly)
        top = [C + c * (R + 0.026) + t1 * x * 0.86 + t2 * y * 0.86 for x, y in poly]
        mid = [C + c * (R + 0.012) + t1 * x + t2 * y for x, y in poly]
        low = []
        for x, y in poly:
            p = c * R + t1 * x + t2 * y
            low.append(C + p / np.linalg.norm(p) * (R - 0.03))
        tone = 0.92 + 0.14 * rs.rand()
        k.add(f"plate{n_}", hull(np.vstack([top, mid, low])), shade(PLATE, tone), "body", fuse=False, surface="shell")

    # The railgun on its back: two great black bars of rock rising out of the shell's top, leaning apart, and a round
    # black rock between their feet. The left bar (-X) is the taller and narrower and carries three amber crystals.
    def bar(foot, top, wide, thick, slant):
        foot, top = np.asarray(foot, float), np.asarray(top, float)
        up = top - foot; up /= np.linalg.norm(up)
        side = np.cross(up, (0, 1.0, 0)); side /= np.linalg.norm(side)
        depth = np.cross(side, up)
        pts = []
        for p, w, h in ((foot, wide, 0.0), (top, wide * 0.82, slant)):
            for sx in (-1, 1):
                for sd in (-1, 1):
                    pts.append(p + side * sx * w / 2 + depth * sd * thick / 2 + up * h * sx)
        return hull(np.array(pts))
    LFOOT, LTOP = np.array([-0.13, 0.09, 0.8]), np.array([-0.19, 0.13, 1.1])
    RFOOT, RTOP = np.array([0.14, 0.1, 0.78]), np.array([0.24, 0.15, 1.07])
    k.add("gunL", bar(LFOOT, LTOP, 0.17, 0.12, -0.035), MASK, "body", fuse=False, surface="shell")
    k.add("gunR", bar(RFOOT, RTOP, 0.28, 0.13, 0.06), MASK, "body", fuse=False, surface="shell")
    def stone(V):
        return g.facets(9, 1.0, 31, depth=(0.8, 0.92))(V)
    k.add("gunrock", hull(ell((0.0, -0.02, 0.95), (0.1, 0.09, 0.085), deform=stone, sub=3)[0]), shade(MASK, 1.15),
          "body", fuse=False, surface="shell")
    # three amber crystals down the left bar's front face
    up = (LTOP - LFOOT) / np.linalg.norm(LTOP - LFOOT)
    face = unit(0, -1, 0.0); face = face - up * (face @ up); face /= np.linalg.norm(face)
    gems = []
    for t in (0.42, 0.63, 0.84):
        p = LFOOT + (LTOP - LFOOT) * t + face * 0.062
        gems.append((p, face, 0.026, 0.012, 0.0))
    g.crystals(k, "gem", gems, "body")

    # The head: out of the front of the shell, a squared reptile's head, a quarter larger than Golem's and carried a
    # little higher (the pictures: the head is the face's whole front, its brow against the shell's lower plates)
    A0 = np.array([0.0, -0.3, 0.42]); HS = 1.25
    def H(p):
        return A0 + (np.asarray(p, float) - A0) * HS + np.array([0.0, -0.01, 0.03])
    def Hr(r):
        return tuple(np.asarray(r, float) * HS)
    k.add("neck", tube([H((0, -0.18, 0.44)), H((0, -0.33, 0.46))], [0.095 * HS, 0.088 * HS]), SKIN, "head")
    k.add("skull", ell(H((0, -0.39, 0.485)), Hr((0.104, 0.11, 0.084)), deform=boxy(2.5)), SKIN, "head")
    def snoutf(V):
        V = boxy(2.8)(V)
        V[:, 0] *= 1 + 0.2 * V[:, 1]
        return V
    k.add("snout", ell(H((0, -0.515, 0.465)), Hr((0.074, 0.11, 0.045)), rot=(-6, 0, 0), deform=snoutf), SKIN, "head")
    k.add("brow", ell(H((0.05, -0.445, 0.53)), Hr((0.052, 0.05, 0.026)), rot=(-10, 0, -18), deform=boxy(3.0)), MASK,
          "head", mirror=True)
    k.add("lowjaw", ell(H((0, -0.48, 0.38)), Hr((0.068, 0.1, 0.032)), rot=(18, 0, 0), deform=boxy(2.6)), SKIN, "jaw")
    k.add("chin", ell(H((0, -0.4, 0.4)), Hr((0.076, 0.066, 0.044))), SKIN, "jaw")

    # The black mask of iron sand: over the top of the head and down a ridge to the end of the nose
    def mask(Pt):
        x, y, z = Pt[:, 0], Pt[:, 1], Pt[:, 2]
        top = inside(0.578 - z, 0.003) * inside(y + 0.2) * inside(-0.72 - y)
        ridge = (inside(np.abs(x) - 0.038, 0.003) * inside(0.54 - z, 0.003) * inside(y + 0.475) * inside(-0.74 - y)
                 * inside(z - 0.63))
        return np.maximum(top, ridge)
    k.paint(mask, MASK, on=["skull", "snout", "neck", "brow"])

    # The mouth, wide open between the jaws, pink within
    def gape(V):
        V = V.copy()
        V[:, 2] += 0.35 * V[:, 1] ** 2
        return V
    k.add("mouth", ell(H((0, -0.515, 0.425)), Hr((0.06, 0.1, 0.032)), rot=(-10, 0, 0), deform=gape), MOUTH, "head",
          carve=True)
    for s in (1, -1):
        k.add(f"fang{s}", cone(H((0.04 * s, -0.575, 0.448)), (0, 0, -1), 0.036, 0.014), CLAW, "head", fuse=False,
              surface="shell")
        k.add(f"tusk{s}", cone(H((0.037 * s, -0.545, 0.398)), (0, -0.2, 1), 0.029, 0.012), CLAW, "jaw", fuse=False,
              surface="shell")

    # The beard of black iron sand under the jaw: a shaggy mass with jagged points spreading out and down onto the
    # chest, half as wide again as the head
    BC = H((0.0, -0.43, 0.325))
    k.add("beard", ell(BC, (0.13, 0.08, 0.06)), MASK, "jaw")
    spikes = []
    for j, ang in enumerate(np.linspace(-172, -8, 9)):
        a = math.radians(ang)
        d = unit(math.cos(a), -0.35, math.sin(a) * 0.9)
        L = 0.08 + 0.03 * ((j * 5) % 3) + (0.02 if j == 4 else 0)
        spikes.append(cone(BC + d * np.array([0.09, 0.06, 0.04]), d, L, 0.036, sides=5))
    for j, x in enumerate((-0.06, 0.0, 0.06)):
        d = unit(x * 2, -1, -0.25)
        spikes.append(cone(BC + np.array([x, -0.05, 0.0]), d, 0.06, 0.03, sides=5))
    k.add("beardspikes", g.join(spikes), MASK, "jaw", fuse=False, surface="shell")

    # The arms: short pale stubs out of the shell's sides, each with two great white claws pointing out and forward
    SH = np.array([0.26, -0.1, 0.52]); HC = np.array([0.42, -0.17, 0.47])
    k.add("arm", tube([SH, (SH + HC) / 2, HC], [0.08, 0.074, 0.068]), SKIN, "arm{s}", mirror=True)
    k.add("palm", ell(HC, (0.07, 0.065, 0.06)), SKIN, "arm{s}", mirror=True)
    k.add("claws", g.join([cone(HC + [0.04, -0.04, 0.02], unit(1, -0.85, -0.1), 0.11, 0.03),
                          cone(HC + [0.02, -0.05, -0.035], unit(0.7, -0.8, -0.2), 0.08, 0.024)]),
          CLAW, "arm{s}", fuse=False, mirror=True, surface="shell")

    # The legs: thick, splayed out from under the shell, on wide feet with three white claws
    HP = np.array([0.17, -0.06, 0.28]); KN = np.array([0.24, -0.12, 0.17]); AN = np.array([0.25, -0.15, 0.1])
    k.add("thigh", tube([HP, KN, AN], [0.14, 0.125, 0.095]), SKIN, "leg{s}", mirror=True)
    k.add("foot", ell((0.255, -0.19, 0.05), (0.105, 0.115, 0.05), deform=boxy(2.6)), SKIN, "leg{s}", mirror=True)
    toes = []
    for dx in (-0.06, 0.0, 0.06):
        b0 = np.array([0.255 + dx, -0.275, 0.045])
        toes.append(cone(b0, unit(dx * 1.5, -1, -0.15), 0.06, 0.022, sides=5))
    k.add("toes", g.join(toes), CLAW, "leg{s}", fuse=False, mirror=True, surface="shell")

    # A darker tone under the jaw
    k.paint(lambda Pt: smoothstep(0.415, 0.385, Pt[:, 2]) * smoothstep(-0.37, -0.43, Pt[:, 1]) *
            inside(Pt[:, 2] - 0.44), SKINDARK, on=["lowjaw"])

    # The eyes: red under the black brow, a dark pupil to the front (golem.py's way), larger
    SCLERA, IRIS, PUPIL, LID = srgb("#F6F2EC"), srgb("#D42A2A"), srgb("#3A0E0E"), srgb(SKIN)
    def eye(u, v):
        col = np.tile(SCLERA, (len(u), 1))
        col[((u + 0.25) / 0.5) ** 2 + ((v + 0.08) / 0.82) ** 2 < 1] = IRIS
        col[((u + 0.3) / 0.22) ** 2 + ((v + 0.1) / 0.45) ** 2 < 1] = PUPIL
        col[(u * u + v * v > 0.74) & (v < 0)] = PUPIL           # a rim under the eye only: the mask cuts its top
        edge = 0.3 + 0.4 * u
        col[v > edge] = CUT
        return col
    CUT = srgb(MASK)
    de = unit(0.72, -0.62, 0.3)
    E = H((0.07, -0.445, 0.5))
    # The mask's brow band over each eye and its stripe down the nose: loose black blocks, crisp where paint is not
    for s, nm in ((1, "browbandR"), (-1, "browbandL")):
        g.brow_block(k, nm, E * [s, 1, 1] + np.array([-0.06 * s, -0.045, 0.028]),
                     E * [s, 1, 1] + np.array([0.035 * s, 0.03, 0.04]), 0.036, 0.05, (0.6 * s, -0.7, 0.4), "head",
                     droop=0.5)
    nose = []
    for y, z in ((-0.53, 0.583), (-0.7, 0.545)):
        for sx in (-1, 1):
            for dz in (-0.014, 0.01):
                nose.append((0.033 * sx * (1 if y > -0.6 else 0.8), y, z + dz))
    k.add("nosestripe", hull(np.array(nose)), MASK, "head", fuse=False, surface="shell")
    def eyes(state):
        k.decal(E, de, (0.044, 0.034), eye_look(state, eye, LID, srgb("#3A2A24")), up=(0, 0.25, 1), eps=0.004,
                rings=20, segs=56, mirror=True)

    k.build(voxel=0.008, smooth=2, band=0.006)
    k.eye_states(eyes)
    k.export(dest)


if __name__ == "__main__":
    src_, dest_ = args()
    golem_alola(src_, dest_)
