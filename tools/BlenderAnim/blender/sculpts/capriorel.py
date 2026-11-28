# Capriorel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/capriorel.py -- <the export's Capriorel.glb> <out.glb>
# From the picture (10094-Capriorel.jpg, the project's own): a roe deer fawn, reddish brown all over, cream spots
# scattered over its back and haunch; a big round head on a long neck, huge ears standing up and out with pink
# linings; big dark eyes with white glints in a pale tan patch, a tan muzzle with a small dark nose and a little
# smile, a pale chin; a tiny green sprout of two leaves on its crown between the ears; a short tuft of a tail with
# green leaves at its tip, a small sprig of leaves on each hock; long legs to small dark hooves.
# The picture's long thin legs and belly at 0.42 of the height are the brief's stilts: the trunk is deepened, the
# shoulders and thighs fused over the upper legs, the shins a third thicker (the report gives the numbers).
#
# Written to be grown from (caprioara.py imports it with sibling("capriorel")): roe() takes a scale (the doe's
# skeleton is the fawn's at 1.5), the shares of the masses that change, the colours and an extras hook.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, slab, lathe, outward, srgb, inside, smoothstep, unit, polygon, eye_look,
                       args, sibling)

T = sibling("torchic")          # feather (a pointed plate with a hard rim)

COAT = "#B35D3A"
COATDK = "#8E4428"
SPOT = "#F0D39A"
TAN = "#E3B78C"
CREAM = "#F1DDBF"
LINING = "#DE9C8C"
NOSE = "#33221E"
HOOF = "#38282A"
LEAF = "#5FA44A"
LEAFDK = "#3F7E36"
INK = srgb("#17100E")
IRIS = srgb("#4B2E22")
GLINT = np.array([1.0, 1.0, 1.0])


def on_ell(c, r, d):
    d = unit(*d)
    t = 1.0 / math.sqrt(((d / np.array(r)) ** 2).sum())
    return np.array(c, float) + d * t


def leaf(k, name, base, d, length, width, thick, colour, bone, mirror=False):
    """A leaf as a pointed flat plate with a hard rim, its face square to d and to the model's length (seen from the
    front and from behind; the kit's leaf turns its width along Y)."""
    k.add(name, T.feather(base, d, length, width, thick=thick, root=0.35), colour, bone, fuse=False, mirror=mirror,
          surface="leaf")


def ear(k, root, d, n, length, half, thick, outer, inner, bone="ear{s}"):
    """An ear: a broad flat plate from root along d, its face to n; a smaller plate of the lining laid on its face."""
    d = unit(*d); n = np.array(n, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    ez = np.cross(n, d)
    L, W = length, half
    ts = np.linspace(0, 1, 13)[:-1]
    f = np.sin(math.pi * (0.13 + 0.87 * ts)) ** 0.75
    right = [(L * t, -W * w) for t, w in zip(ts, f)]
    out = right + [(L, 0)] + [(a, -b) for a, b in right[::-1]]
    k.add("ear", slab(out, root, d, ez, thick), outer, bone, fuse=False, mirror=True, surface="fur")
    lin = [(0.1 * L + 0.78 * a, 0.66 * b) for a, b in out]
    k.add("earin", slab(lin, np.array(root) + n * (thick * 0.5 + 0.0025), d, ez, thick * 0.35), inner, bone,
          fuse=False, mirror=True, surface="skin")
    return d, ez, n


def eye_open(u, v):
    """Big and dark: a thin dark rim, a brown iris, a black pupil, a big glint up and out and a small one low."""
    r = np.hypot(u, v)
    c = np.tile(INK, (len(u), 1))
    c[r < 0.86] = IRIS
    c[np.hypot(u + 0.04, v + 0.04) < 0.5] = INK
    c[((u - 0.16) / 0.26) ** 2 + ((v - 0.32) / 0.24) ** 2 < 1] = GLINT
    c[np.hypot(u + 0.3, v + 0.42) < 0.11] = GLINT
    return c


def board(ob):
    """The game's board check: the four-corner sum against its square (PokemonStudio.Strip)."""
    V = np.array([v.co[:] for v in ob.data.vertices])
    h = V[:, 2].max(); w = V[:, 0].max() - V[:, 0].min(); l = V[:, 1].max() - V[:, 1].min()
    sq = 1.3 * max(h, 0.85 * max(w, l))
    s = 0.853 * w + 0.522 * l
    print(f"KIT: board height {h:.3f} width {w:.3f} length {l:.3f}: sum {s:.3f} of square {sq:.3f} ({s / sq:.2f})")


def roe(src, out_path, S=1.0, head=1.08, muzzle=1.0, ears=1.0, eye=1.08, leg=1.12, coat=COAT, extras=None,
        spots=True, sprout=True):
    """The roe line's body at scale S (the fawn's joints at 1). head, muzzle, ears, eye, leg: shares that change."""
    k = Kit(src, coat="fur")
    s = S

    def P(x, y, z):
        return (x * s, y * s, z * s)

    # --- the trunk: deep, chest and rump round, the belly brought down off the stilts -------------------------------
    k.add("barrel", ell(P(0, 0.005, 0.33), (0.072 * s, 0.16 * s, 0.112 * s)), coat, "body")
    k.add("chest", ell(P(0, -0.112, 0.34), (0.068 * s, 0.078 * s, 0.118 * s)), coat, "body")
    k.add("rump", ell(P(0, 0.118, 0.348), (0.075 * s, 0.082 * s, 0.112 * s)), coat, "body")

    # --- legs: shoulders and thighs fused over the upper legs, thick tapering shins, small dark hooves -------------
    k.add("shoulder", ell(P(0.042, -0.108, 0.27), (0.035 * s, 0.046 * s, 0.088 * s)), coat, "leg{s}", mirror=True)
    k.add("thigh", ell(P(0.046, 0.12, 0.275), (0.04 * s, 0.068 * s, 0.1 * s)), coat, "hind{s}", mirror=True)
    rl = leg * s
    k.add("fore", tube([P(0.043, -0.11, 0.24), P(0.045, -0.104, 0.135), P(0.046, -0.11, 0.055), P(0.046, -0.112, 0.03)],
                       [0.024 * rl, 0.0185 * rl, 0.0158 * rl, 0.0152 * rl]), coat, "leg{s}", mirror=True)
    k.add("hindleg", tube([P(0.046, 0.128, 0.22), P(0.047, 0.152, 0.135), P(0.046, 0.13, 0.06), P(0.046, 0.125, 0.03)],
                          [0.027 * rl, 0.019 * rl, 0.0158 * rl, 0.0152 * rl]), coat, "hind{s}", mirror=True)
    for nm, y, bone in (("forehoof", -0.112, "leg{s}"), ("hindhoof", 0.125, "hind{s}")):
        hr = 0.0172 * rl
        k.add(nm, lathe(P(0.046, y - 0.002, 0.0), (0, 0, 1),
                        [(0, 0), (hr, 0), (hr * 1.04, hr * 0.3), (hr * 0.86, hr * 1.3), (0, hr * 1.3)]),
              HOOF, bone, fuse=False, mirror=True, surface="shell")

    # --- a long neck tapering to a big round head; a tan muzzle, a small dark nose ----------------------------------
    k.add("neck", tube([P(0, -0.128, 0.37), P(0, -0.148, 0.46), P(0, -0.163, 0.53)],
                       [0.068 * s, 0.055 * s, 0.046 * s]), coat, "head")
    H = np.array(P(0, -0.175, 0.573)); HR = np.array([0.066, 0.069, 0.066]) * s * head
    k.add("head", ell(H, HR), coat, "head")
    m = muzzle
    MZ = H + np.array([0, -0.066 * m, -0.03]) * s
    k.add("muzzle", ell(MZ, (0.036 * s * head, 0.05 * s * m, 0.036 * s * head), rot=(10, 0, 0)), coat, "head")
    tip = MZ + np.array([0, -0.048 * m, 0.006]) * s
    k.add("nose", ell(tip, (0.017 * s, 0.011 * s, 0.013 * s)), NOSE, "head", fuse=False, surface="skin")

    # the muzzle tan, the chin and lips pale (crisp paint in coordinates round the muzzle)
    e = 0.003 * s
    def muzzle_tan(P_):
        d = (P_ - MZ) / (np.array([0.045, 0.06, 0.044]) * s * np.array([head, m, head]))
        front = inside(P_[:, 1] - (MZ[1] + 0.005 * s), e)
        return inside(np.linalg.norm(d, axis=1) - 1, e) * front
    k.paint(muzzle_tan, TAN)
    def chin(P_):
        d = (P_ - (MZ + np.array([0, -0.01, -0.03]) * s)) / (np.array([0.034, 0.05, 0.022]) * s)
        return inside(np.linalg.norm(d, axis=1) - 1, e)
    k.paint(chin, CREAM)

    # --- the ears: huge, up and out, pink within ----------------------------------------------------------------
    ear(k, P(0.04, -0.168, 0.618), (0.72, 0.12, 0.8), unit(0.18, -0.95, 0.22), 0.162 * s * ears, 0.05 * s * ears,
        0.022 * s, coat, LINING)

    # --- the sprout on the crown: a short stem and two leaves in a V --------------------------------------------
    top = H[2] + HR[2]
    if sprout:
        k.add("stem", tube([(0, H[1], top - 0.01 * s), (0, H[1] - 0.002 * s, top + 0.022 * s),
                            (0, H[1] - 0.004 * s, top + 0.042 * s)], [0.0115 * s, 0.0108 * s, 0.0102 * s]),
              LEAFDK, "head", fuse=False, surface="leaf")
    for sd in ((1, -1) if sprout else ()):
        leaf(k, f"sprout{sd}", (sd * 0.006 * s, H[1] - 0.004 * s, top + 0.04 * s), (sd * 0.95, -0.1, 0.7),
             0.052 * s, 0.023 * s, 0.012 * s, LEAF, "head")

    # --- the tail: a short tuft up and back, green leaves at its tip swept toward +X ----------------------------
    k.add("tail", tube([P(0, 0.17, 0.42), P(0, 0.195, 0.462), P(0, 0.208, 0.49)], [0.031 * s, 0.024 * s, 0.015 * s]),
          coat, "tail")
    TT = np.array(P(0, 0.205, 0.48))
    for i, (d, ln) in enumerate((((0.55, 0.55, 0.62), 0.058), ((-0.25, 0.7, 0.62), 0.05), ((0.85, 0.35, 0.25), 0.05),
                                 ((0.25, 0.3, 0.95), 0.052), ((0.4, 0.85, -0.1), 0.045))):
        leaf(k, f"tailleaf{i}", TT, d, ln * s, 0.021 * s, 0.012 * s, LEAF if i % 2 == 0 else LEAFDK, "tail")
    # a sprig of two leaves on each hock
    for i, (d, ln) in enumerate((((0.7, 0.55, 0.45), 0.045), ((0.6, 0.1, 0.8), 0.04))):
        leaf(k, f"hockleaf{i}", P(0.06, 0.158, 0.14), d, ln * s, 0.019 * s, 0.011 * s, LEAF if i == 0 else LEAFDK,
             "hind{s}", mirror=True)

    # --- cream spots over the back and the haunch (fixed places: the same bytes every build) -------------------------
    if spots:
        C0, R0 = np.array(P(0, 0.005, 0.33)), np.array([0.072, 0.16, 0.112]) * s
        RC, RR = np.array(P(0, 0.118, 0.348)), np.array([0.075, 0.082, 0.112]) * s
        for t, phi, r in ((-0.35, 30, 0.010), (-0.1, 55, 0.011), (0.05, 25, 0.011), (0.2, 62, 0.012), (0.3, 40, 0.012),
                          (0.42, 15, 0.010), (0.5, 70, 0.011), (0.6, 50, 0.013), (0.7, 28, 0.012), (0.82, 62, 0.012),
                          (0.9, 40, 0.011), (-0.22, 75, 0.009)):
            c_, r_ = (RC, RR) if t > 0.45 else (C0, R0)
            tt = (t - 0.45) / 0.45 * 0.9 if t > 0.45 else t
            ph = math.radians(phi)
            d = np.array([math.sin(ph) * math.sqrt(1 - tt * tt), tt, math.cos(ph) * math.sqrt(1 - tt * tt)])
            p = c_ + r_ * d
            n = d / r_; n /= np.linalg.norm(n)
            k.decal(p, n, (r * s, r * s * 0.85), SPOT, up=(0, 1, 0), eps=0.002 * s, rings=8, segs=32, mirror=True)

    # --- the face: a pale tan patch round each eye, a little smile ------------------------------------------------
    de = unit(0.63, -0.72, 0.16)
    E = on_ell(H, HR, de)
    half = (0.025 * s * eye, 0.028 * s * eye)
    pe = on_ell(H, HR, unit(0.6, -0.76, 0.08))
    k.decal(pe, unit(0.6, -0.76, 0.08), (half[0] * 1.38, half[1] * 1.22), TAN, up=unit(0, 0.25, 1), eps=0.0018 * s,
            rings=12, segs=48, mirror=True)
    k.decal(MZ + np.array([0.03, -0.028, -0.02]) * s * np.array([head, m, 1]), unit(0.55, -0.7, -0.35),
            (0.011 * s, 0.0032 * s), INK, up=unit(0, 0.4, 1), eps=0.003 * s, rings=3, segs=24, mirror=True)

    if extras is not None:
        extras(k, s, dict(H=H, HR=HR, MZ=MZ, E=E, de=de, half=half, ear=ear, leaf=leaf, P=P))

    k.build(voxel=0.0056 * s, smooth=4, band=0.006 * s, wband=0.05 * s)
    board(k.ob)
    lid = srgb(TAN)
    look = (extras.eye if extras is not None and hasattr(extras, "eye") else eye_open)

    def eyes(state):
        k.decal(E, de, half, eye_look(state, look, lid, INK, line=0.15), up=unit(0, 0.1, 1), eps=0.0036 * s,
                rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out_path)


if __name__ == "__main__":
    roe(*args())
