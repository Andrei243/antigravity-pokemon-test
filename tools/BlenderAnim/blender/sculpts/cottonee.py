# Cottonee sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cottonee.py -- <the export's Cottonee.glb> <out.glb>
#
# From the references: a smooth white ball of a face, wider than tall, held between two pale green-white clouds of
# cotton: a cap over its top with two little knobs on its crown, and a bigger lumpy cloud over its lower half and
# front, wider than the cap, reaching up to the eyes; a narrow band of the white face between them; two upright orange
# eyes with a darker rim and a white glint, set apart; no mouth to be seen; out of its sides under the cap two green
# leaves like flags, broad at their outer end, their top edge rising to a point and their lower edge torn in teeth.
#
# Written to be grown from (whimsicott.py imports it with sibling("cottonee")): the cotton, the eyes and how they are
# laid are the functions below; the calls that build Cottonee are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, slab, srgb, inside, unit, lumpy, eye_look, args

# --- the line's colours -------------------------------------------------------------------------------------------
FACE = "#F8F8F4"
COTTON, COTTON_LOW = "#CCDEBC", "#B2C8A2"
LEAF = "#4C9E50"
ORANGE, ORANGE_DARK = srgb("#F2782A"), srgb("#C4501A")
RIM = srgb("#7A3410")
INK = srgb("#4A3020")
WHITE = np.array([1.0, 1.0, 1.0])

# --- Cottonee's frame (joints: body (0, 0, 0.085), ears (+-0.06, 0, 0.095)) ---------------------------------------
C = np.array([0.0, 0.0, 0.106])          # the white ball's middle
R = np.array([0.09, 0.08, 0.08])
CAP = np.array([0.0, 0.01, 0.166])       # the cap of cotton
CAP_R = np.array([0.078, 0.07, 0.034])
LOW = np.array([0.0, -0.012, 0.05])      # the cloud over the lower half
LOW_R = np.array([0.104, 0.084, 0.048])
EYE_D = (0.44, -0.9, 0.08)
EYE_HALF = (0.014, 0.02)


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre, and its normal."""
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def cloud(k, name, c, r, bone, seed, lumps=9, color=COTTON, small=0.42, spread=0.7, zr=(-0.4, 0.7), flat=1.0,
          upright=False):
    """
    A cloud of cotton: a lumpy ellipsoid with smaller balls fused round its rim, jittered so they don't read as petals.
    The rim goes round the level (x, y); upright, round the front's plane (x, z), its third measure along y.
    """
    c, r = np.array(c, float), np.array(r, float)
    k.add(name, ell(c, r, deform=lumpy(5, 3, 0.06)), color, bone)
    rs = np.random.RandomState(seed)
    m = min(r[0], r[2]) if upright else min(r[0], r[1])
    for i in range(lumps):
        a = 2 * math.pi * (i + rs.rand() * 0.6) / lumps
        z = rs.uniform(*zr)
        d = np.array([math.cos(a) * math.sqrt(1 - z * z), math.sin(a) * math.sqrt(1 - z * z), z * flat])
        if upright:
            d = d[[0, 2, 1]]
        s = m * small * rs.uniform(0.85, 1.15)
        k.add(f"{name}{i}", ell(c + d * r * spread, (s, s, s * 0.95), sub=3), color, bone)


def cloud_shade(k, c, r, color=COTTON_LOW, share=0.35, on=("cap", "low"), soft=0.01):
    """The lower part of a cloud a shade darker, its edge soft."""
    c, r = np.array(c, float), np.array(r, float)
    def mask(P):
        q = (P - c) / (r * 1.5)
        return inside(np.linalg.norm(q, axis=1) - 1, 0.004) * inside((P[:, 2] - c[2]) + r[2] * share, soft)
    k.paint(mask, color, on=list(on))


def orange_eye(fill=ORANGE, dark=ORANGE_DARK, rim=RIM):
    """The line's upright eye: orange, a deeper orange below, a dark rim and a long white glint high toward the inside."""
    def look(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(fill, (len(u), 1))
        c[(v < -0.25) & (r < 0.8)] = dark
        c[((u + 0.18) / 0.22) ** 2 + ((v - 0.28) / 0.36) ** 2 < 1] = WHITE
        c[r > 0.78] = rim
        return c
    return look


def lay_eyes(k, c, r, d, half, look, lid, state, ink=INK, eps=0.004):
    """The pair of eyes at the direction d (+X side) from the ellipsoid (c, r), mirrored."""
    p, nrm = on_ell(c, r, d)
    k.decal(p, nrm, half, eye_look(state, look, srgb(lid) if isinstance(lid, str) else lid, ink, line=0.16),
            eps=eps, rings=20, segs=56, mirror=True)


# the leaf's outline in its own plane: a out from the root, b up; the top edge rises to a point, the lower is torn
FLAG = [(0.0, -0.014), (0.0, 0.016), (0.04, 0.022), (0.075, 0.03), (0.104, 0.042), (0.1, 0.012),
        (0.098, -0.026), (0.088, -0.012), (0.08, -0.034), (0.07, -0.016), (0.06, -0.034), (0.049, -0.016),
        (0.038, -0.03), (0.026, -0.013), (0.014, -0.024)]


def flags(k, root=(0.078, 0.006, 0.112), out=(1.0, 0.32, 0.08), size=1.0, color=LEAF, bone="ear{s}", thick=0.018):
    """The two leaves out of the sides like flags, their faces to the front, swept a little back."""
    ex = unit(*out)
    ez = np.array([0, 0, 1.0])
    k.add("flag", slab([(a * size, b * size) for a, b in FLAG], root, ex, ez, thick), color, bone, fuse=False,
          mirror=True, surface="leaf")


def cottonee(src, out):
    k = Kit(src, coat="fur")
    k.add("face", ell(C, R), FACE, "body")
    # the cap: a flat cloud over the top, two little knobs on its crown
    cloud(k, "cap", CAP, CAP_R, "body", 4, lumps=8, small=0.5, spread=0.78, zr=(-0.3, 0.2), flat=0.5)
    for x, y, z, s in ((0.03, 0.01, 0.198, 0.02), (-0.024, 0.016, 0.196, 0.018), (0.004, 0.034, 0.198, 0.019)):
        k.add("capknob", ell((x, y, z), (s, s, s * 0.9), sub=3), COTTON, "body")
    # the lower cloud, wider, over the lower half and the front, up to just under the eyes
    cloud(k, "low", LOW, LOW_R, "body", 11, lumps=11, small=0.5, spread=0.78, zr=(-0.5, 0.35))
    for i, (x, z) in enumerate(((0.0, 0.07), (0.046, 0.066), (-0.046, 0.066))):
        k.add(f"lowfront{i}", ell((x, -0.072, z), (0.034, 0.026, 0.024), sub=3), COTTON, "body")
    cloud_shade(k, LOW, LOW_R, share=0.25)
    flags(k)
    k.build(voxel=0.0045, smooth=4, band=0.005)

    def eyes(state):
        lay_eyes(k, C, R, EYE_D, EYE_HALF, orange_eye(), FACE, state)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    cottonee(*args())
