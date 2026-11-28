# Stupara sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/stupara.py -- <the export's Stupara.glb> <out.glb>
#
# Grown from albinut.py (sibling("albinut")): the line's golden down, amber, dark brown and pale wax, its eye made the
# same way (dark brown, darker pupil, the large white glint high and outward), its gauzy wings (a warm rim plate under
# a paler panel) and its tiny workers, which are Albinut in small. From the picture (10036-Stupara.jpg) and its brief
# (CP-dex-1b-plateau-more.md, 36), the masses largest first: the abdomen, an old straw skep of coiled rings, wider a
# third of the way up, a small dark door low on its front and golden honey running round its foot and dripping from
# it; two pairs of broad gauzy wings out of her back; a slim golden thorax with an amber band, in a ruff of pale cream
# fur under the chin; a round golden head with large dark almond eyes lined on top and a small smile; a little wax
# crown of five points; two dark antennae curling outward. Six thin dark limbs: one hand raised to her chin, one folded
# at her waist, two resting on the skep's shoulders and two long legs lying down its sides. Two workers on the skep.
# Skeleton: body (0, 0, 0.448), arms (+-0.07, -0.04, 0.748), wings (+-0.06, 0.05, 0.778), head (0, -0.03, 0.848).
# Height about 1.15, hovering (the honey's lowest drop at about 0.12).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, lathe, srgb, unit, inside, eye_look, polygon, lumpy, args, outward,
                       sibling)

al = sibling("albinut")
GOLD, AMBER, BAND, WAX = al.GOLD, al.AMBER, al.BAND, al.WAX
LIMB = "#4C2E1C"
STRAW, STRAW_DARK, STRAW_CORE = "#D9A853", "#BE8B3C", "#A9782F"
HONEY = "#F6A21C"
RUFF = "#F3E9CE"
DOOR = "#3A2416"
LINER = srgb("#1A100A")

# --- Stupara's frame ------------------------------------------------------------------------------------------------
SKC = np.array([0.0, 0.0, 0.445])        # the skep's middle
SKR = np.array([0.238, 0.238, 0.245])
TC = np.array([0.0, -0.01, 0.725])       # the thorax
TR = np.array([0.088, 0.078, 0.105])
HC = np.array([0.0, -0.035, 0.948])      # the head
HR = np.array([0.114, 0.102, 0.1])
EYE_D = (0.43, -0.9, 0.02)
EYE_HALF = (0.034, 0.03)


def skep_r(z):
    """The skep's radius at a height."""
    t = (z - SKC[2]) / SKR[2]
    return SKR[0] * float(skep_profile(t))


def skep_profile(t):
    """The skep's radius on the unit sphere's height t: a dome above, fuller and flatter below (a beehive's foot)."""
    t = np.clip(t, -1, 1)
    return np.where(t > 0, np.sqrt(np.clip(1 - t * t, 0, 1)), np.clip(1 - np.abs(t) ** 3.2, 0, 1) ** (1 / 3.2))


def skep_shape(V):
    """The core's unit sphere drawn to the skep's profile."""
    V = V.copy()
    r = np.hypot(V[:, 0], V[:, 1])
    want = skep_profile(V[:, 2])
    have = np.sqrt(np.clip(1 - V[:, 2] ** 2, 1e-6, 1))
    k = np.where(have > 1e-3, want / have, 1.0)
    V[:, 0] *= k; V[:, 1] *= k
    return V


def torus(c, R, r, seg=48, n=14):
    """A ring about the upright axis through c: major radius R, its tube's radius r (a lathe of a closed circle)."""
    prof = [(R + r * math.cos(2 * math.pi * i / n), r * math.sin(2 * math.pi * i / n)) for i in range(n)]
    prof.append(prof[0])
    return lathe(c, (0, 0, 1), prof, seg=seg)


def skep(k):
    """The straw skep: a fused core and loose coils of straw round it, alternately lighter and darker."""
    k.add("skep", ell(SKC, SKR, deform=skep_shape), STRAW_CORE, "body", surface="leaf")
    zs = np.linspace(0.235, 0.672, 10)
    for i, z in enumerate(zs):
        R = skep_r(z)
        r = 0.031 if i < 8 else 0.026
        k.add(f"coil{i}", torus((0, 0, z), max(R - 0.01, 0.04), r), STRAW if i % 2 == 0 else STRAW_DARK, "body",
              fuse=False, surface="leaf")
    # the door, low on the front three-quarter (+X): a dark oval set into the coils, a darker straw lip round it
    a = math.radians(24)
    z = 0.36
    R = skep_r(z) + 0.022
    p = np.array([R * math.sin(a), -R * math.cos(a), z])
    n = unit(math.sin(a), -math.cos(a), 0.0)
    rot = (0, 0, math.degrees(a))
    k.add("doorlip", outward(ell(p - n * 0.006, (0.062, 0.02, 0.07), rot=rot)), STRAW_DARK, "body", fuse=False,
          surface="leaf")
    k.add("door", outward(ell(p + n * 0.002, (0.046, 0.016, 0.056), rot=rot)), DOOR, "body", fuse=False)
    # honey running round its foot and dripping from it
    k.add("honeyrim", torus((0, 0, 0.232), skep_r(0.232) + 0.004, 0.03), HONEY, "body", fuse=False, surface="shell")
    # drops of honey hanging from the rim, of uneven lengths, their tops in it
    R = skep_r(0.232) + 0.006
    for i, (a, ln) in enumerate(((-40, 0.05), (10, 0.034), (60, 0.056), (115, 0.03), (165, 0.048), (220, 0.036),
                                 (285, 0.052))):
        a = math.radians(a)
        d = np.array([math.sin(a), -math.cos(a), 0.0])
        k.add(f"drip{i}", outward(ell(d * R + (0, 0, 0.228 - ln * 0.62), (0.024, 0.024, ln), sub=3)), HONEY, "body",
              fuse=False, surface="shell")


def limb(k, name, pts, radii, bone, hand=None):
    """A thin dark limb (loose), and a small hand of a ball and two short fingers when hand gives its forward way."""
    k.add(name, outward(tube(pts, radii)), LIMB, bone, fuse=False)
    if hand is not None:
        p = np.array(pts[-1], float)
        k.add(name + "hand", outward(ell(p, (0.026, 0.024, 0.026), sub=3)), LIMB, bone, fuse=False)
        f = unit(*hand)
        side = np.cross(f, (0, 0, 1.0)); side /= np.linalg.norm(side)
        for j, s in enumerate((-1, 1)):
            q = p + f * 0.016 + side * s * 0.014
            k.add(f"{name}finger{j}", outward(tube([q, q + f * 0.022 - (0, 0, 0.008)], [0.0178, 0.0175])), LIMB, bone,
                  fuse=False)


def stupara(src, out):
    k = Kit(src, coat="fur")
    skep(k)
    # the waist: golden fuzz over the skep's crown where the thorax sits
    k.add("waist", ell((0, -0.005, 0.66), (0.085, 0.08, 0.05), deform=lumpy(9, 3, 0.18)), GOLD, "body")
    # the slim thorax, golden with an amber band
    k.add("thorax", ell(TC, TR), GOLD, "body")
    k.paint(lambda P: inside(0.69 - P[:, 2], 0.004) * inside(P[:, 2] - 0.735, 0.004) * inside(np.hypot(P[:, 0], P[:, 1]) - 0.12, 0.004),
            "#D88E30")
    # the ruff of pale cream fur round the neck, under the chin, wider than the head
    k.add("ruffcore", ell((0, -0.005, 0.81), (0.1, 0.092, 0.05)), RUFF, "body")
    rs = np.random.RandomState(4)
    for i in range(11):
        a = 2 * math.pi * (i + 0.3 * rs.rand()) / 11
        c = (0.098 * math.sin(a), -0.09 * math.cos(a) - 0.005, 0.805 + 0.012 * math.cos(3 * a))
        k.add(f"ruff{i}", ell(c, (0.044, 0.042, 0.04), deform=lumpy(3, 2, 0.15)), RUFF, "body")
    # the head, golden and round, a little fuzz at the cheeks
    k.add("head", ell(HC, HR), GOLD, "head")
    for sx in (1, -1):
        for j, (dz, ln) in enumerate(((-0.25, 0.026), (0.05, 0.022))):
            p, n = al.on_ell(HC, HR, (0.98 * sx, 0.05, dz))
            al.tuft(k, f"cheek{j}" + ("R" if sx > 0 else "L"), p - n * 0.006, n + np.array([0, 0.2, -0.3]), ln, 0.016,
                    GOLD, "head")
    # a small closed smile
    CI = srgb("#7A3A28")
    CB = srgb(GOLD)
    def smile(u, v):
        c = np.tile(CB, (len(u), 1))
        c[(np.abs(v - (-0.3 + 0.75 * u * u)) < 0.24) & (np.abs(u) < 0.85)] = CI
        return c
    mp, mn = al.on_ell(HC, HR, (0, -0.94, -0.42))
    k.decal(mp, mn, (0.03, 0.016), smile, eps=0.003, rings=8, segs=40)

    # the wax crown: a band and five points, the middle tallest, a pale gem before it (loose, crisp)
    top, _ = al.on_ell(HC, HR, (0, -0.05, 1))
    cb = top + np.array([0, 0, -0.016])
    k.add("crown", lathe(cb, (0, 0, 1), [(0, 0), (0.05, 0), (0.054, 0.006), (0.052, 0.03), (0.046, 0.032),
                                          (0, 0.032)]), WAX, "head", fuse=False, surface="shell")
    for i in range(5):
        a = math.radians(-60 + 30 * i)                   # across the front, the middle at the front
        rr = 0.046
        b = cb + np.array([rr * math.sin(a), -rr * math.cos(a), 0.028])
        h = 0.058 if i == 2 else (0.044 if i in (1, 3) else 0.034)
        o = unit(math.sin(a), -math.cos(a), 0) * 0.016
        t = unit(math.cos(a), math.sin(a), 0) * 0.017
        k.add(f"crownpt{i}", hull([b + t + o * 0.3, b - t + o * 0.3, b + t - o, b - t - o, b + (0, 0, h) - o * 0.2]),
              WAX, "head", fuse=False, surface="shell")
    for i in range(2):
        a = math.radians(150 + 60 * i)                   # two low points behind, so the crown is round from behind
        b = cb + np.array([0.046 * math.sin(a), -0.046 * math.cos(a), 0.028])
        o = unit(math.sin(a), -math.cos(a), 0) * 0.016
        t = unit(math.cos(a), math.sin(a), 0) * 0.017
        k.add(f"crownbk{i}", hull([b + t + o * 0.3, b - t + o * 0.3, b + t - o, b - t - o, b + (0, 0, 0.03) - o * 0.2]),
              WAX, "head", fuse=False, surface="shell")
    g = cb + np.array([0, -0.054, 0.018])
    k.add("gem", hull([g + (0.013, 0, 0), g + (-0.013, 0, 0), g + (0, 0, 0.016), g + (0, 0, -0.012), g + (0, -0.01, 0.002),
                       g + (0, 0.008, 0.002)]), "#F7D9D2", "head", fuse=False, surface="shell")
    # two dark antennae out of the crown's sides, curling outward
    for sx in (1, -1):
        p0, _ = al.on_ell(HC, HR, (0.42 * sx, -0.1, 0.9))
        pts = [p0 - (0, 0, 0.01), p0 + (0.02 * sx, -0.012, 0.06), p0 + (0.045 * sx, -0.012, 0.115),
               p0 + (0.085 * sx, 0.0, 0.14), p0 + (0.112 * sx, 0.014, 0.12), p0 + (0.112 * sx, 0.012, 0.094)]
        k.add("antenna" + ("R" if sx > 0 else "L"), outward(tube(pts, [0.018, 0.0175, 0.017, 0.017, 0.0175, 0.0185])),
              LIMB, "head", fuse=False)

    # the wings: two broad gauzy pairs out of the back, raised in a V and swept back
    al.wing(k, "wingU", (0.06, 0.06, 0.82), (0.72, 0.42, 0.55), (0.35, -0.9, 0.2), 0.41, 0.13, thick=0.008)
    al.wing(k, "wingD", (0.065, 0.065, 0.775), (0.8, 0.5, 0.05), (0.25, -0.6, 0.75), 0.31, 0.09, thick=0.008)

    # the arms: her -X hand raised to her chin, her +X hand folded at her waist
    limb(k, "armL", [(-0.07, -0.04, 0.748), (-0.13, -0.08, 0.72), (-0.16, -0.12, 0.7), (-0.1, -0.15, 0.78),
                     (-0.058, -0.13, 0.83)], [0.021, 0.019, 0.019, 0.018, 0.018], "armL", hand=(0.4, -0.2, 0.8))
    limb(k, "armR", [(0.07, -0.04, 0.748), (0.14, -0.08, 0.69), (0.165, -0.11, 0.645), (0.1, -0.15, 0.64),
                     (0.04, -0.13, 0.655)], [0.021, 0.019, 0.019, 0.018, 0.018], "armR", hand=(-0.9, -0.3, 0.1))
    # the middle pair resting on the skep's shoulders, and the long legs lying down its sides
    for sx, s in ((1, "R"), (-1, "L")):
        m = np.array([sx, 1, 1.0])
        limb(k, "mid" + s, [np.array(p) * m for p in ((0.06, -0.02, 0.69), (0.15, -0.09, 0.66), (0.2, -0.13, 0.6),
                                                       (0.19, -0.16, 0.555))],
             [0.02, 0.019, 0.018, 0.018], "body", hand=(0.2 * sx, -0.6, -0.8))
        limb(k, "leg" + s, [np.array(p) * m for p in ((0.06, 0.0, 0.645), (0.17, -0.07, 0.6), (0.255, -0.12, 0.47),
                                                       (0.272, -0.12, 0.34), (0.245, -0.14, 0.24))],
             [0.021, 0.02, 0.019, 0.018, 0.0175], "body")
    # two workers on the skep, one on its front three-quarter, one high on its back
    for i, (a, z, fwd) in enumerate(((-38, 0.4, (0.9, -0.2, 0.3)), (130, 0.52, (-0.5, -0.8, 0.3)))):
        aa = math.radians(a)
        R = skep_r(z) + 0.05
        c = np.array([R * math.sin(aa), -R * math.cos(aa), z])
        al.worker(k, f"worker{i}", c, fwd, 0.038)
    k.build(voxel=0.0075, smooth=4, band=0.008, wband=0.06)
    al.board(k.ob)

    # the eyes: large dark almonds, their outer corners lifted, lined on top
    # (the outline isn't symmetric, so each eye is its own patch: a mirrored patch keeps its outline unmirrored)
    PTS = [(math.cos(t), 0.84 * math.sin(t) + 0.2 * math.cos(t)) for t in np.linspace(0, 2 * math.pi, 28, endpoint=False)]
    ALMOND_R = polygon(PTS)
    ALMOND_L = polygon([(-a, b) for a, b in PTS])
    base = al.eye_open()
    def open_look(u, v):
        c = base(u, v)
        c[v > 0.56 + 0.12 * u] = LINER
        return c
    def eyes(state):
        look = eye_look(state, open_look, srgb(GOLD), al.INK, line=0.16)
        p, nrm = al.on_ell(HC, HR, EYE_D)
        k.decal(p, nrm, EYE_HALF, look, eps=0.003, outline=ALMOND_R, rings=20, segs=56)
        m = np.array([-1, 1, 1.0])
        k.decal(p * m, nrm * m, EYE_HALF, lambda u, v: look(-u, v), eps=0.003, outline=ALMOND_L, rings=20, segs=56,
                up=(0, 0, 1))
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    stupara(*args())
