# Clopotitan sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/clopotitan.py -- <the export's Clopotitan.glb> <out.glb>
#
# Clopotel grown (its bronze, mottling and strap's bow, imported from clopotel.py). The design, from its picture
# (docs/art/reference/gemini/carpathia/10084-Clopotitan.jpg) and brief (CP-dex-2-mountains.md, "84. Clopotitan: the
# bell tower"): a living wooden bell tower. Its masses, largest first: the steep roof, a hipped pyramid of dark shingles
# in courses with broad eaves and a needle of a spire, Clopotel's leather bow tied round the spire; the open belfry under
# it, four corner posts bound in iron with curved braces, arched openings in its side walls and a dark back wall where
# two yellow eyes glow, slanted angrily, beside the crown of the great bronze church bell that hangs inside like a
# heart, its clapper under its lip; the shingled lower storey flaring to its foot, iron straps at its corners; two
# timber arms, a beam to a great iron-banded forearm block patched with moss and a blocky hand; two stout timber legs
# bound in iron on broad feet. The back: the back wall's own arched window and the roof's courses all round.
# Where the picture parted from the rules: the hands hang to just under the skirt's foot (knee height), not near the
# ground as drawn; both arms are built alike (the picture's +X arm is raised on a slant).
#
# Skeleton: root, body (0, 0, 0.62), clapper (0, 0, 1.0), bow (0, -0.05, 1.42), armL/armR (+-0.3, 0, 1.0), legL/legR
# (+-0.15, 0, 0.42). The tower, its roof and its bell on body (as the game's sculptor has it), the clapper on clapper,
# the bow on bow, the arms and legs on theirs.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, srgb, inside, unit, eye_look, args, sibling

c_ = sibling("clopotel")
BRONZE, BRONZE_DK, BRONZE_LT, INSIDE = c_.BRONZE, c_.BRONZE_DK, c_.BRONZE_LT, c_.INSIDE
IRON = "#4A4846"
TIMBER, TIMBER_DK = "#5E4836", "#46362A"
SHINGLE, SHINGLE_DK = "#54463A", "#3E3329"
MOSS = "#57702F"
DARK = "#16110D"
GLOW, GLOW_LT = "#F2B428", "#FFE88A"

SILL_Z, BEAM_Z = 0.7, 1.19          # the belfry's floor and the underside of its top beam
POST = (0.32, 0.215)                # the corner posts' x and y
BACK_Y = 0.2                        # the back wall's front face
EYE_X, EYE_Z, EYE_HALF = 0.082, 1.09, (0.06, 0.036)
FACE_Y = 0.03                       # the front of the shadow the eyes are laid on


def box(lo, hi):
    return hull([(x, y, z) for x in (lo[0], hi[0]) for y in (lo[1], hi[1]) for z in (lo[2], hi[2])])


def frustum(z0, z1, b0, b1, dy=0.0):
    """A course of shingles: a four-sided frustum from half sizes b0 at z0 to b1 at z1."""
    pts = []
    for z, (hx, hy) in ((z0, b0), (z1, b1)):
        pts += [(sx * hx, dy + sy * hy, z) for sx in (-1, 1) for sy in (-1, 1)]
    return hull(pts)


def beam(a, b, hw, hd):
    """A squared timber from a to b, half width hw across and hd deep."""
    a, b = np.array(a, float), np.array(b, float)
    d = unit(*(b - a))
    e1 = np.cross(d, (0, 1.0, 0)); e1 /= np.linalg.norm(e1)
    e2 = np.cross(e1, d)
    return hull([p + e1 * sx * hw + e2 * sy * hd for p in (a, b) for sx in (-1, 1) for sy in (-1, 1)])


def legs(k):
    k.add("leg", box((0.09, -0.1, 0.06), (0.31, 0.1, 0.45)), TIMBER, "leg{s}", fuse=False, mirror=True)
    k.add("foot", box((0.075, -0.17, 0.0), (0.325, 0.12, 0.085)), TIMBER_DK, "leg{s}", fuse=False, mirror=True)
    for z in (0.2, 0.33):
        k.add(f"legband{int(z * 100)}", box((0.082, -0.108, z), (0.318, 0.108, z + 0.035)), IRON, "leg{s}", fuse=False,
              mirror=True, surface="metal")


def storey(k):
    """The shingled lower storey, flaring to its foot, in courses; the sill over it; iron straps at its corners."""
    courses = [(0.38, 0.47, (0.45, 0.32), (0.415, 0.3)), (0.45, 0.54, (0.42, 0.302), (0.385, 0.282)),
               (0.52, 0.61, (0.4, 0.284), (0.375, 0.265)), (0.59, 0.68, (0.38, 0.267), (0.36, 0.25))]
    for i, (z0, z1, b0, b1) in enumerate(courses):
        k.add(f"skirt{i}", frustum(z0, z1, b0, b1), SHINGLE if i % 2 else SHINGLE_DK, "body", fuse=False)
    k.add("sill", box((-0.37, -0.26, 0.665), (0.37, 0.26, SILL_Z)), TIMBER, "body", fuse=False)
    for sx in (1, -1):
        for sy in (1, -1):
            a = (sx * 0.452, sy * 0.322, 0.38); b = (sx * 0.365, sy * 0.258, 0.69)
            k.add(f"strap{'R' if sx > 0 else 'L'}{'B' if sy > 0 else 'F'}", beam(a, b, 0.03, 0.03), IRON, "body",
                  fuse=False, surface="metal")


def belfry(k):
    for sx in (1, -1):
        for sy in (1, -1):
            nm = f"{'R' if sx > 0 else 'L'}{'B' if sy > 0 else 'F'}"
            x, y = sx * POST[0], sy * POST[1]
            k.add(f"post{nm}", box((x - 0.04, y - 0.04, SILL_Z), (x + 0.04, y + 0.04, BEAM_Z + 0.02)), TIMBER, "body",
                  fuse=False)
            for z in (SILL_Z + 0.02, BEAM_Z - 0.06):
                k.add(f"bracket{nm}{int(z * 100)}", box((x - 0.047, y - 0.047, z), (x + 0.047, y + 0.047, z + 0.04)),
                      IRON, "body", fuse=False, surface="metal")
    # the curved braces in the front corners
    for sx in (1, -1):
        th = np.linspace(0, math.pi / 2, 7)
        pts = [(sx * (POST[0] - 0.16 + 0.16 * math.cos(t)), -POST[1] - 0.005, BEAM_Z - 0.16 + 0.16 * math.sin(t))
               for t in th]
        pts = [(sx * POST[0] - sx * 0.005, -POST[1] - 0.005, BEAM_Z - 0.17)] + pts[1:]
        k.add(f"brace{'R' if sx > 0 else 'L'}", tube(pts, [0.028] * len(pts), seg=8), TIMBER_DK, "body", fuse=False)
    # the top beam, a ceiling under the eaves
    k.add("ceiling", box((-0.375, -0.27, BEAM_Z), (0.375, 0.27, BEAM_Z + 0.04)), TIMBER, "body", fuse=False)
    # the side walls and the back wall's outside, each with an arched opening
    arch = [(-0.21, SILL_Z), (-0.1, SILL_Z), (-0.1, 0.94)]
    arch += [(-0.1 * math.cos(t), 0.94 + 0.1 * math.sin(t)) for t in np.linspace(0.15, math.pi - 0.15, 9)]
    arch += [(0.1, 0.94), (0.1, SILL_Z), (0.21, SILL_Z), (0.21, BEAM_Z), (-0.21, BEAM_Z)]
    k.add("wall", slab(arch, (POST[0] + 0.002, 0, 0), (0, 1, 0), (0, 0, 1), 0.035), TIMBER_DK, "body", fuse=False,
          mirror=True)
    back = [(x * 1.2, z) for x, z in arch]
    k.add("backwall", slab(back, (0, BACK_Y + 0.055, 0), (1, 0, 0), (0, 0, 1), 0.03), TIMBER_DK, "body", fuse=False)


def roof(k):
    courses = [(1.21, 1.29, (0.47, 0.38), (0.39, 0.315)), (1.27, 1.355, (0.395, 0.32), (0.305, 0.25)),
               (1.335, 1.42, (0.31, 0.255), (0.22, 0.185)), (1.4, 1.485, (0.225, 0.19), (0.135, 0.118)),
               (1.465, 1.54, (0.14, 0.122), (0.08, 0.072))]
    for i, (z0, z1, b0, b1) in enumerate(courses):
        k.add(f"roof{i}", frustum(z0, z1, b0, b1), SHINGLE_DK if i % 2 else SHINGLE, "body", fuse=False)
    k.add("spire", hull([(sx * 0.08, sy * 0.072, 1.52) for sx in (-1, 1) for sy in (-1, 1)] + [(0, 0, 1.763)]),
          SHINGLE, "body", fuse=False)
    k.add("band", frustum(1.565, 1.6, (0.062, 0.056), (0.055, 0.05)), c_.STRAP, "bow", fuse=False,
          surface="leather")
    c_.bow(k, (0, -0.08, 1.585), 1.4, (0.3, -0.92, 0.25), "bow")


def arms(k):
    SH, EL, WR = np.array([0.385, 0.0, 1.03]), np.array([0.47, -0.04, 0.76]), np.array([0.5, -0.09, 0.4])
    k.add("upper", beam(SH, EL, 0.045, 0.05), TIMBER, "arm{s}", fuse=False, mirror=True)
    k.add("fore", beam(EL + [0, 0, 0.03], WR, 0.1, 0.09), TIMBER, "arm{s}", fuse=False, mirror=True)
    for f in (0.25, 0.72):
        p = EL + [0, 0, 0.03] + (WR - EL - [0, 0, 0.03]) * f
        d = unit(*(WR - EL - [0, 0, 0.03]))
        k.add(f"band{int(f * 100)}", beam(p - d * 0.025, p + d * 0.025, 0.108, 0.098), IRON, "arm{s}", fuse=False,
              mirror=True, surface="metal")
    # moss on the forearm's outer face and its front
    k.add("moss", beam(EL + [0.1, -0.02, -0.08], EL + [0.115, -0.05, -0.2], 0.006, 0.05), MOSS, "arm{s}", fuse=False,
          mirror=True, surface="leaf")
    k.add("moss2", beam(EL + [0.01, -0.13, -0.12], EL + [0.03, -0.155, -0.3], 0.055, 0.006), MOSS, "arm{s}",
          fuse=False, mirror=True, surface="leaf")
    H = WR + [0, -0.02, -0.07]
    k.add("palm", box(H - [0.085, 0.075, 0.06], H + [0.085, 0.075, 0.06]), TIMBER_DK, "arm{s}", fuse=False,
          mirror=True)
    for j, dx in enumerate((-0.055, 0.0, 0.055)):
        a = H + [dx, -0.06, -0.04]
        k.add(f"finger{j}", beam(a, a + [0, -0.03, -0.09], 0.024, 0.026), TIMBER_DK, "arm{s}", fuse=False,
              mirror=True)
    a = H + [-0.09, -0.05, 0.0]
    k.add("thumb", beam(a, a + [-0.01, -0.05, -0.06], 0.022, 0.024), TIMBER_DK, "arm{s}", fuse=False, mirror=True)


def bell(k, rs):
    """The great bronze church bell hanging in the belfry: a solid of turning, its mouth flat, fused for its patina."""
    zs = [0.73, 0.75, 0.79, 0.85, 0.9, 0.935, 0.955]
    rr = [0.17, 0.158, 0.126, 0.106, 0.098, 0.088, 0.066]
    V, F = tube([(0, 0, z) for z in zs], rr, seg=32)
    V[:, 2] = np.maximum(V[:, 2], 0.73)
    k.add("bell", (V, F), BRONZE, "body", surface="metal")
    k.paint(lambda P: inside(P[:, 2] - 0.736, 0.003) * inside(np.hypot(P[:, 0], P[:, 1]) - 0.155, 0.004), INSIDE,
            on=["bell"])
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.765) - 0.006, 0.002), BRONZE_DK, on=["bell"])
    c_.mottle(k, rs, ["bell"], (0, 0), (0.125, 0.125), 10, 0.76, 0.86, 1.3)
    # the crown's loop and the yoke it hangs from, the clapper under the lip
    th = np.linspace(math.pi, 0, 9)
    k.add("loop", tube([(0.036 * math.cos(t), 0.0, 1.0 + 0.05 * math.sin(t)) for t in th], [0.014] * 9), IRON,
          "body", fuse=False, surface="metal")
    k.add("yoke", box((-0.03, -0.03, 1.04), (0.03, 0.03, BEAM_Z)), TIMBER_DK, "body", fuse=False)
    k.add("stem", tube([(0, 0, 0.95), (0, 0, 0.8), (0, -0.01, 0.71)], [0.013] * 3), IRON, "clapper", fuse=False,
          surface="metal")
    k.add("ball", ell((0, -0.012, 0.69), (0.045, 0.045, 0.045)), BRONZE_LT, "clapper", fuse=False, surface="metal")


def clopotitan(src, dest):
    k = Kit(src, coat="leather")
    rs = np.random.RandomState(84)
    legs(k)
    storey(k)
    belfry(k)
    roof(k)
    arms(k)
    bell(k, rs)
    # the back wall in shadow, seen through the arches
    k.add("dark", ell((0, BACK_Y + 0.03, 0.95), (0.3, 0.03, 0.24), deform=lambda V: V / (np.abs(V) ** 6).sum(1, keepdims=True) ** (1 / 6)),
          DARK, "body")
    # the shadow over the bell's crown, brought forward to just behind it, where the eyes glow
    k.add("face", ell((0, FACE_Y + 0.03, 1.11), (0.27, 0.03, 0.08), deform=lambda V: V / (np.abs(V) ** 6).sum(1, keepdims=True) ** (1 / 6)),
          DARK, "body")
    k.build(voxel=0.0075, smooth=2)
    c_.board(k.ob)

    cd, cg, cl = srgb(DARK), srgb(GLOW), srgb(GLOW_LT)
    def glow_eye(u, v):
        c = np.tile(cg, (len(u), 1))
        c[((u + 0.05) / 0.45) ** 2 + ((v + 0.1) / 0.45) ** 2 < 1] = cl
        return c
    def outline_for(slant):
        th = np.linspace(0, 2 * math.pi, 64, endpoint=False)
        u, v = np.cos(th), np.sin(th) * 0.9
        v = np.minimum(v, 0.25 + slant * u)
        from sculptkit import polygon
        return polygon(np.stack([u, v], 1))
    angry, fierce = outline_for(0.55), outline_for(0.9)
    def eyes(state):
        for s in (1, -1):
            if state == "Open":
                f, ol = glow_eye, angry
            elif state == "Fierce":
                f, ol = glow_eye, fierce
            else:
                f, ol = eye_look(state, glow_eye, cd, cg, line=0.22), angry
            lk = f if s > 0 else (lambda g: (lambda u, v: g(-u, v)))(f)
            o = ol if s > 0 else (lambda q: (lambda th: q(math.pi - th)))(ol)
            k.decal((s * EYE_X, FACE_Y, EYE_Z), (0, -1, 0), EYE_HALF, lk, eps=0.004, rings=20, segs=56, outline=o)
    k.eye_states(eyes)
    k.export(dest)


if __name__ == "__main__":
    clopotitan(*args())
