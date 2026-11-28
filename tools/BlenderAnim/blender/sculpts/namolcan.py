# Namolcan sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/namolcan.py -- <the export's Namolcan.glb> <out.glb>
#
# Grown from namolel.py (sibling("namolel")): its colours, its cracked crust painted as plates, its glassy bubbles and
# its round black eyes.
# The design, from the project's picture (docs/art/reference/gemini/carpathia/10146-Namolcan.jpg) and its brief
# (CP-dex-3-river-and-caves.md, 146): a living mud volcano. A wide cone of grey clay, flat-topped, its whole surface
# cracked into plates, lighter grey above and darker low down, pale yellow salt in some of the cracks; a crater at its
# top full of wet mud with a bubble swelling in it and a smaller one beside it, mud spilling over the lip and down the
# front in drips; a heavy base on four stumpy splayed legs with three dark toes each; a heavy glowering face low on the
# front: brows sloping down to a broad nose, small dark eyes cut by them, a wide down-turned mouth with a thick lower
# lip, mud dripping from its corners.
# Reference ratios (picture): the width 1.10 of the crater rim's height (built 0.96, held in by the board), the crater 0.42
# of the base's width (built 0.40), the face 0.42 of the width and 0.22 of the height (built 0.45, 0.24).
# The picture's floating bubbles and purple gas are left out (drawn effects); the bubble sits in the crater as a dome.
# The export's joints: body (0, 0, 0.46), crater (0, 0.06, 0.9), head (0, -0.2, 0.38), legs (+-0.24, -0.16, 0.24),
# hinds (+-0.24, 0.16, 0.24).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, srgb, inside, smoothstep, unit, eye_look, polygon, sibling, args

nl = sibling("namolel")
hit, board, on_ell, normal_on, cracked, bubble = nl.hit, nl.board, nl.on_ell, nl.normal_on, nl.cracked, nl.bubble
CLAY, CLAY_LOW, CRACK, SALT, WET, INK = "#868579", "#6E6D63", nl.CRACK, nl.SALT, nl.WET, nl.INK
TOE, LIP = "#5A5850", "#3A3730"

CC = np.array([0.0, 0.03, 0.56])                # the cone: centre and radii of the ellipsoid it is cut from
CR = np.array([0.4, 0.36, 0.36])
TOP = 0.89                                      # the flat top, the crater's rim
F = np.array([0.0, -0.235, 0.32])               # the face's middle
FR = np.array([0.22, 0.14, 0.15])


def cone(V):
    """The unit sphere made a cone: narrowing upward, flat at the top."""
    V = V.copy()
    t = (V[:, 2] + 1) / 2
    q = np.maximum(np.linalg.norm(V[:, :2], axis=1), 1e-6)
    s = (1.18 - 0.72 * t) * q ** 0.15 / q                 # the sides drawn straight, a frustum's
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.minimum(V[:, 2], (TOP - CC[2]) / CR[2])
    return V


def cone_radius(z):
    """The cone's radius across X at height z, as the deform makes it."""
    zu = float(np.clip((z - CC[2]) / CR[2], -1, 1))
    return (1.18 - 0.72 * (zu + 1) / 2) * (1 - zu * zu) ** 0.075 * CR[0]


def namolcan(src, out):
    k = Kit(src, coat="scales")
    rs = np.random.RandomState(146)

    # --- the cone on its heavy base -----------------------------------------------------------------------------
    k.add("cone", ell(CC, CR, deform=cone), CLAY, "body")
    k.add("base", ell((0, 0.0, 0.27), (0.39, 0.35, 0.2)), CLAY, "body")
    k.add("face", ell(F, FR), CLAY, "head")
    # the brows: two heavy ridges sloping down toward the nose
    for s in (1, -1):
        k.add("brow" + ("R" if s > 0 else "L"), ell(F + (0.085 * s, -0.115, 0.075), (0.085, 0.045, 0.038),
                                                    rot=(0, -14 * s, 0)), CLAY, "head")
    k.add("nose", ell(F + (0, -0.14, 0.02), (0.05, 0.04, 0.034)), CLAY, "head")
    # the thick lower lip, pushed forward under the mouth's line
    k.add("lip", ell(F + (0, -0.1, -0.085), (0.11, 0.042, 0.03)), CLAY, "head")
    k.add("jowl", ell(F + (0, -0.06, -0.11), (0.17, 0.08, 0.05)), CLAY, "head")
    # mud dripping from the mouth's corners: short round lobes
    for j, (x, L) in enumerate(((0.1, 0.035), (-0.085, 0.025))):
        top = F + (x, -0.1, -0.08)
        k.add(f"lipdrip{j}", tube([top, top + (0, -0.004, -L)], [0.018, 0.02]), WET, "head", surface="plastic")

    # --- four stumpy splayed legs, three dark toes each -------------------------------------------------------------
    for nm, y, bone in (("leg", -0.16, "leg{s}"), ("hind", 0.16, "hind{s}")):
        k.add(nm + "top", ell((0.27, y, 0.2), (0.125, 0.125, 0.13)), CLAY, bone, mirror=True)
        k.add(nm + "foot", ell((0.315, y * 1.15, 0.058), (0.105, 0.115, 0.058)), CLAY, bone, mirror=True)
        ahead = unit(0.45, 1.0 if y > 0 else -1.0, 0)
        for j, a in enumerate((-0.5, 0.0, 0.5)):
            d = unit(ahead[0] * math.cos(a) - ahead[1] * math.sin(a), ahead[0] * math.sin(a) + ahead[1] * math.cos(a), 0)
            p = np.array([0.315, y * 1.15, 0.032]) + d * 0.095
            side = unit(-d[1], d[0], 0)
            k.add(f"{nm}toe{j}", hull([p + side * 0.024, p - side * 0.024, p + (0, 0, 0.034), p - (0, 0, 0.024),
                                       p + d * 0.026, p - d * 0.03 + (0, 0, 0.01)]),
                  TOE, bone, fuse=False, mirror=True, surface="shell")

    # --- the crater: carved into the flat top, a pool of wet mud in it, the bubbles on the pool ---------------------
    k.add("crater", ell((0, 0.045, TOP + 0.02), (0.135, 0.13, 0.065)), WET, "crater", carve=True)
    k.add("pool", lathe((0, 0.045, TOP - 0.04), (0, 0, 1), [(0, 0), (0.13, 0.0), (0.13, 0.012), (0, 0.016)]),
          WET, "crater", fuse=False, surface="plastic")
    bubble(k, "bubble", (-0.02, 0.06, TOP + 0.01), (0.075, 0.072, 0.065), "crater")
    bubble(k, "bubbletwo", (0.07, 0.0, TOP - 0.005), (0.036, 0.035, 0.03), "crater")
    # mud spilling over the lip and down the front in drips (fused, the wet colour), one over the +X front
    for j, (az, L) in enumerate(((-1.25, 0.24), (-1.85, 0.12))):
        pts = [(0.118 * math.cos(az), CC[1] + 0.118 * math.sin(az), TOP - 0.012)]
        for z in np.linspace(TOP - 0.02, TOP - L, 3):
            r = cone_radius(z) - 0.034
            pts.append((r * math.cos(az), CC[1] + r * CR[1] / CR[0] * math.sin(az), z))
        k.add(f"spill{j}", tube(pts, [0.03, 0.036, 0.036, 0.042]), WET, "body", surface="plastic")

    # --- the cracks: plates painted over the cone, the base and the legs, salt in some, the face left smoother -----
    seeds = []
    for i in range(110):
        az = 2 * math.pi * rs.rand(); z = 0.12 + 0.78 * rs.rand()
        rad = max(cone_radius(z), 0.36 if z < 0.4 else 0.0)
        seeds.append((rad * math.cos(az), CC[1] + rad * CR[1] / CR[0] * math.sin(az), z))
    seeds = np.array(seeds)

    def region(P):
        off_face = 1 - inside(np.linalg.norm((P - F) / (FR + (0.03, 0.08, 0.03)), axis=1) - 1, 0.01)
        return off_face * inside(0.06 - P[:, 2], 0.01) * inside(P[:, 2] - (TOP - 0.008), 0.004)
    k.paint(lambda P: inside(P[:, 2] - 0.42, 0.06) * inside(0.06 - P[:, 2], 0.01), CLAY_LOW,
            on=["cone", "base", "legtop", "hindtop", "legfoot", "hindfoot"])
    cracked(k, seeds, lambda P: region(P) * smoothstep(0.32, 0.48, P[:, 2]), (CLAY, "#7C7B70"), CRACK, 0.0042,
            on=["cone", "base"], salt=SALT, salt_every=3)
    cracked(k, seeds, lambda P: region(P) * (1 - smoothstep(0.32, 0.48, P[:, 2])), (CLAY_LOW, "#64635A"), CRACK,
            0.0042, on=["cone", "base", "legtop", "hindtop"])

    # --- the mouth: a wide frown, the line dipping at its corners, under the nose and over the lip ------------------
    cl, ck = srgb(CLAY), srgb(LIP)

    def frown(u, v):
        c = np.tile(cl, (len(u), 1))
        line = 0.25 - 0.7 * u * u
        c[(np.abs(v - line) < 0.24) & (np.abs(u) < 0.9)] = ck
        return c
    md = unit(0, -1, -0.05)
    mc = hit(ell(F + (0, -0.1, -0.085), (0.11, 0.042, 0.03)), F + (0, -0.6, -0.055), (0, 1, 0))
    if mc is None:
        mc = F + (0, -0.15, -0.055)
    k.decal(mc + (0, 0, 0.002), md, (0.11, 0.024), frown, eps=0.003, rings=10, segs=56)
    # two nostrils
    nd = unit(0.35, -0.85, -0.3)
    k.decal(hit(ell(F + (0, -0.14, 0.02), (0.05, 0.04, 0.034)), F + (0, -0.14, 0.02) + nd * 0.3, -nd), nd,
            (0.012, 0.009), LIP, eps=0.002, rings=6, segs=28, mirror=True)

    k.build(voxel=0.0078, smooth=4, band=0.006, wband=0.07)
    board(k.ob)

    # --- the eyes: small, dark, cut across the top by the brows sloping down toward the nose ----------------------
    look0 = nl.mud_eye(iris="#5A4E40")
    lid, ink = srgb(CLAY), srgb(INK)

    def glower(st):
        base = eye_look(st, look0, lid, ink, line=0.16)
        if st not in ("Open", "Fierce"):
            return base
        slope = 0.45 if st == "Fierce" else 0.3

        def look(u, v):
            c = base(u, v)
            edge = (0.12 if st == "Fierce" else 0.3) + slope * u      # u outward: the brow falls toward the nose
            c[v > edge] = lid
            c[np.abs(v - edge) < 0.12] = ink
            return c
        return look
    de = unit(0.32, -0.9, 0.18)
    ec = hit(ell(F, FR), F + (0.085, -0.6, 0.03), (0, 1, 0))
    k.eye_states(lambda st: k.decal(ec, de, (0.034, 0.028), glower(st), eps=0.0022, rings=20, segs=56, mirror=True))
    k.export(out)


if __name__ == "__main__":
    namolcan(*args())
