# Mega Decidueye (a Mega of our own, plan 06 · R20b) sculpted on the skeleton and clips of the game's own export of
# the form (sculptkit.py), grown from decidueye.py (sibling("decidueye"): its hood, fringe, crest, beak, cross, vine,
# tail feathers and feet as they are; its cloak copied here and lengthened; its eyes recoloured):
#   blender --background --factory-startup --python sculpts/decidueyemega.py -- <the export's Decidueye-Mega.glb> <out.glb>
# No pictures: the game's own sculptor (PokemonModels.Ours.Megas.cs, DecidueyeMega) is the design. Its hood deepened
# into a cowl whose peak shades its face to the eyes, two leaves for ears on its crown; its eyes and the band between
# them lit a ghostly teal; its cloak lengthened into a cape to the ground, the hem behind a fan of quills tipped with a
# teal glow lying on the ground; and a bow of vine held across its chest, its ends toward the viewer, strung with a
# thread of teal light.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, outward, srgb, unit, eye_look, args, sibling

d = sibling("decidueye")
r, p = d.r, d.p
HOOD, HOODDARK, CLOAK, CLOAKTIP, CORAL = d.HOOD, d.HOODDARK, d.CLOAK, d.CLOAKTIP, d.CORAL
VINE, QUILL = "#60543A", "#7A5A3A"
TEAL, TEALPALE = "#3FE3CC", "#B4FAF0"


def join(shapes):
    """Several closed shapes as one loose piece (one glowing material)."""
    V, F, o = [], [], 0
    for sv, sf in shapes:
        V.append(np.asarray(sv, float)); F.extend([[i + o for i in f] for f in sf]); o += len(sv)
    return np.vstack(V), F


def cloak(k, F):
    """Decidueye's cloak lengthened into a cape to the ground: each side's masses longer and lower, tips at the hem."""
    taper = d.taper
    for s, side in ((1, "R"), (-1, "L")):
        b = f"wing{side}"
        C, R = np.array([s * 0.104, 0.024, 0.265]), np.array([0.064, 0.108, 0.25])
        k.add(f"cloak{side}", ell(C, R, deform=taper), CLOAK, b, fuse=False, surface="fur")
        FC_, FR_ = np.array([s * 0.062, -0.058, 0.27]), np.array([0.04, 0.042, 0.2])
        k.add(f"cloakfront{side}", ell(FC_, FR_, deform=taper), CLOAK, b, fuse=False, surface="fur")
        for phi in (35, 85, 135, 185):
            t = math.radians(phi)
            o = np.array([s * math.sin(t), -math.cos(t), 0])
            base = C + o * np.array([0.034, 0.055, 0]) + np.array([0, 0, -0.19])
            tip = base + o * 0.012 + np.array([0, 0, -0.055])
            k.add(f"cloaktip{side}", p.spike(base, tip, 0.04, 0.016, side=np.cross((0, 0, 1.0), o), at=0.4), CLOAKTIP,
                  b, fuse=False, surface="fur")
        dz = 0.17
        for phi in (50, 80, 110, 140):
            t = math.radians(phi)
            q = math.sqrt(1 - (dz / R[2]) ** 2)
            c = C + np.array([s * R[0] * math.sin(t) * q, -R[1] * math.cos(t) * q, dz])
            n = (c - C) / R ** 2; n /= np.linalg.norm(n)
            tang = np.cross((0, 0, 1.0), n); tang /= np.linalg.norm(tang)
            c = c + n * 0.002
            k.add(f"tri{side}", hull([c + tang * 0.018 + (0, 0, 0.012), c - tang * 0.018 + (0, 0, 0.012),
                                      c + (0, 0, -0.024), c + n * 0.007 + (0, 0, 0.004)]), CORAL, b, fuse=False)
        for z in (0.34, 0.27, 0.2):
            dx = s * 0.012
            q = 1 - ((z - FC_[2]) / FR_[2]) ** 2 - (dx / FR_[0]) ** 2
            c = np.array([FC_[0] + dx, FC_[1] - FR_[1] * math.sqrt(max(q, 0)), z])
            n = (c - FC_) / FR_ ** 2; n /= np.linalg.norm(n)
            k.add(f"dot{side}", r.ell_frame(c + n * 0.002, (0.009, 0.004, 0.01), np.cross((0, 0, 1.0), n), n),
                  CLOAKTIP, b, fuse=False)
    # the back of the cape between the wings, dark green, to the hem
    k.add("capeback", ell((0.0, 0.07, 0.24), (0.07, 0.04, 0.2), deform=taper), HOODDARK, "body", fuse=False,
          surface="fur")


def cowl(k, F):
    """The cowl's peak over the brow, shading the face to the eyes, and two leaves for ears on the crown."""
    top = d.S(F, (0.0, -0.03, 0.6))
    k.add("peak", hull([top + (0.06, 0.03, 0.0), top + (-0.06, 0.03, 0.0), top + (0.05, -0.02, 0.02),
                        top + (-0.05, -0.02, 0.02), top + (0, -0.075, -0.045), top + (0.03, -0.06, -0.04),
                        top + (-0.03, -0.06, -0.04)]), HOOD, "head", fuse=False, surface="fur")
    for s, side in ((1, "R"), (-1, "L")):
        root = F.H + np.array([0.06 * s, 0.01, 0.085])
        dd = unit(0.6 * s, 0.25, 1.0)
        L, w = 0.1, 0.03
        out = [(0.0, -0.4 * w), (0.2 * L, -0.95 * w), (0.55 * L, -w), (0.85 * L, -0.5 * w), (L, 0.0),
               (0.85 * L, 0.5 * w), (0.55 * L, w), (0.2 * L, 0.95 * w), (0.0, 0.4 * w)]
        face = unit(0.3 * s, -1, 0.0)
        k.add(f"leafear{side}", slab(out, root, dd, np.cross(face, dd), 0.02), HOODDARK, "head", fuse=False,
              surface="leaf")


def bow(k):
    """A bow of vine across the chest, its ends toward the viewer, a teal thread of light from end to end."""
    t = np.linspace(-1, 1, 9)
    pts = [np.array([0.2 * a, -(0.074 + 0.11 * a * a), 0.37 + 0.06 * a * a]) for a in t]
    k.add("bow", outward(tube(pts, [0.012, 0.013, 0.014, 0.015, 0.015, 0.015, 0.014, 0.013, 0.012])), VINE, "body",
          fuse=False, surface="shell")
    k.add("bowstring", outward(tube([pts[0], pts[-1]], [0.0065, 0.0065])), TEAL, "body", fuse=False, glow=TEAL)
    for s, end in ((-1, pts[0]), (1, pts[-1])):
        dd = unit(0.5 * s, -0.3, 0.8)
        out = [(0.0, -0.01), (0.03, -0.016), (0.05, 0.0), (0.03, 0.016), (0.0, 0.01)]
        k.add(f"bowleaf{'RL'[s < 0]}", slab(out, end, dd, np.cross(unit(0, -1, 0.2), dd), 0.012), HOOD, "body",
              fuse=False, surface="leaf")


def quills(k):
    """The cape's hem behind: a fan of seven quills lying back on the ground, faces up, tipped with teal light."""
    tips = []
    for i in range(7):
        x = -0.15 + 0.05 * i
        root = np.array([x * 0.8, 0.12 + 0.06 * abs(x), 0.1])
        tip = np.array([x * 1.7, 0.33 - 0.1 * abs(x), 0.025])
        dd = tip - root; L = float(np.linalg.norm(dd)); dd /= L
        face = unit(0, 0.45, 1)
        out = [(0.0, -0.012), (0.35 * L, -0.02), (0.8 * L, -0.016), (L, 0.0), (0.8 * L, 0.016), (0.35 * L, 0.02),
               (0.0, 0.012)]
        k.add(f"quill{i}", slab(out, root, dd, np.cross(face, dd), 0.016), QUILL, "body", fuse=False, surface="fur")
        a, b = np.cross(dd, (0, 0, 1.0)), np.array([0, 0, 1.0])
        a /= np.linalg.norm(a)
        tips.append(hull([tip - dd * 0.008 + a * 0.016, tip - dd * 0.008 - a * 0.016, tip - dd * 0.008 + b * 0.014,
                          tip - dd * 0.008 - b * 0.006, tip + dd * 0.04]))
    k.add("quilltips", join(tips), TEAL, "body", fuse=False, glow=TEAL)


INK, TEALC, PALEC, LIDC = srgb("#10202A"), srgb(TEAL), srgb(TEALPALE), srgb(d.MASK)


def eye_open(u, v):
    rr = np.sqrt(u * u + v * v)
    c = np.tile(TEALC, (len(u), 1))
    c[rr < 0.6] = PALEC
    c[(u + 0.08) ** 2 + (v - 0.05) ** 2 < 0.28 ** 2] = INK
    c[rr > 0.86] = INK
    c[(u - 0.06) ** 2 + (v - 0.28) ** 2 < 0.1 ** 2] = srgb("#FFFFFF")
    return c


def cowled(look):
    """The cowl's peak cuts the eye's top lower than Decidueye's fringe, lower toward the beak."""
    def f(u, v):
        c = look(u, v)
        c[v > 0.3 + 0.3 * u] = LIDC
        return c
    return f


def eyes(k, F):
    centre, normal = r.on_ell(F.FC, F.FR, F.EYEX, F.EYEZ)
    normal = unit(*(normal + np.array([0.1, 0, 0])))
    def lay(state):
        look = eye_look(state, eye_open, LIDC, TEALC, line=0.16)
        if state in ("Open", "Fierce"):
            look = cowled(look)
        k.decal(centre, normal, F.EYE, look, eps=0.0025, rings=20, segs=56, mirror=True)
    return lay


def decidueyemega(src, out):
    F = d.frame()
    k = Kit(src, coat="fur")
    d.body(k, F)
    d.jagged(k, F)
    d.crest(k, F)
    d.beak(k, F)
    cowl(k, F)
    cloak(k, F)
    d.cross(k, F)
    d.tail(k)
    bow(k)
    quills(k)
    r.feet(k, F, colour=d.CREAM, claw=d.CLAW)
    P, n = r.on_ell(F.FC, F.FR, 0.0, 0.53)
    k.decal(P, n, (0.016, 0.0055), TEAL, eps=0.002, rings=6, segs=32)
    k.build(voxel=F.VOXEL, band=0.006, wband=0.05)
    k.eye_states(eyes(k, F))
    k.export(out)


if __name__ == "__main__":
    decidueyemega(*args())
