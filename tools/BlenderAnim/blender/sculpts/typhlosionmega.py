# Mega Typhlosion (a Mega of our own, plan 06 · R20b) on the skeleton and clips of the game's own export of the form:
#   blender --background --factory-startup --python sculpts/typhlosionmega.py -- <Typhlosion-Mega.glb> <out.glb>
# No game has it, so the game's own sculpt (PokemonModels.Ours.Megas.cs, TyphlosionMega) is the design: Typhlosion's
# Blender sculpt (typhlosion.py, copied here: its function takes no arguments that reach what changes) hunched lower
# and broader (x and depth by 1.1, height by 0.94, the form's joints), its cream turned to a mid grey with the cream
# kept on the belly and under the jaw, the dark of its back turned ash grey and cracked with lava; the ruff of fire
# gone for a crater's rim of dark rock blocks round its shoulders with fire welling inside and three thick tongues of
# flame standing out of it, and bracers of the same rock on its forearms.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, smoothstep, unit, polygon, args, sibling, axes

cq = sibling("cyndaquil")
ql = sibling("quilava")

GREY, ASH, CREAM = "#929096", "#5E5E68", "#F5E09A"
ROCK, ROCK2 = "#3A363C", "#4E484E"
LAVA, FIRE, HEART = "#F6802C", "#FA9A3A", "#FFDE78"
MOUTH, TONGUE = srgb("#7E2228"), srgb("#E58C98")

# The form's stretch of Typhlosion's frame (Blender's axes: across, depth, height)
K = np.array([1.1, 1.1, 0.94])
H, HR = np.array([0.0, -0.08, 0.75]), np.array([0.112, 0.114, 0.1])
SNOUT = [(0, -0.13, 0.73), (0, -0.2, 0.712), (0, -0.25, 0.7)]


class Stretched(Kit):
    """A kit that lays what it is given in Typhlosion's frame stretched to the form's while `stretch` is on."""
    stretch = True

    def add(self, name, shape, color, bone, **kw):
        V, F = shape
        if self.stretch:
            V = V * K
        super().add(name, (V, F), color, bone, **kw)

    def paint(self, mask, color, on=None):
        if self.stretch:
            m0 = mask
            mask = lambda P, m0=m0: m0(P / K)
        super().paint(mask, color, on)

    def decal(self, center, normal, half, color, **kw):
        if self.stretch:
            center = np.array(center, float) * K
            n = np.array(normal, float) / K
            normal = n / np.linalg.norm(n)
            half = tuple(h * 1.05 for h in half)
        super().decal(center, normal, half, color, **kw)


def box(c, d, a_half, b_half, d_half, twist=0.0, taper=0.85):
    """A block of rock: a box about c along d, its top (+d) a little narrower, turned twist radians about d."""
    a, b = axes(d)
    ca, sa = math.cos(twist), math.sin(twist)
    a, b = a * ca + b * sa, b * ca - a * sa
    d = unit(*d)
    pts = []
    for sd, t in ((-1, 1.0), (1, taper)):
        for sa_ in (-1, 1):
            for sb in (-1, 1):
                pts.append(np.array(c, float) + d * sd * d_half + a * sa_ * a_half * t + b * sb * b_half * t)
    return hull(pts)


def typhlosion_mega(src, out_path):
    k = Stretched(src, coat="fur")

    # --- Typhlosion's body, as typhlosion.py lays it, in its own frame ---------------------------------------------
    k.add("hips", ell((0, 0.02, 0.27), (0.215, 0.19, 0.19)), GREY, "body")
    k.add("belly", ell((0, -0.015, 0.42), (0.19, 0.175, 0.23), rot=(-8, 0, 0)), GREY, "body")
    k.add("chest", ell((0, -0.035, 0.54), (0.145, 0.13, 0.12), rot=(-10, 0, 0)), GREY, "body")
    k.add("neck", tube([(0, -0.03, 0.56), (0, -0.05, 0.64), (0, -0.07, 0.7)], [0.11, 0.095, 0.085]), GREY, "head")
    k.add("head", ell(H, HR * 1.04), GREY, "head")
    k.add("snout", tube(SNOUT, [0.078, 0.065, 0.052]), GREY, "head")
    k.add("jaw", ell((0, -0.13, 0.695), (0.075, 0.09, 0.05)), GREY, "head")

    k.add("arm", tube([(0.13, -0.04, 0.53), (0.19, -0.09, 0.49), (0.205, -0.16, 0.45)], [0.058, 0.05, 0.045]),
          GREY, "arm{s}", mirror=True)
    HC = np.array([0.208, -0.19, 0.44])
    k.add("hand", ell(HC, (0.045, 0.04, 0.04)), GREY, "arm{s}", mirror=True)
    for j, (dx, dz) in enumerate([(0.022, 0.006), (0.0, -0.004), (-0.022, 0.006)]):
        b = HC + np.array((dx, -0.032, -0.006 + dz))
        k.add(f"claw{j}", cq.claw(b, (dx * 1.2, -1, -0.6), 0.032, 0.02, 0.016, up=(0, 1, 0)), cq.CLAW, "arm{s}",
              fuse=False, mirror=True, surface="shell")

    k.add("thigh", ell((0.13, -0.02, 0.17), (0.11, 0.13, 0.13)), GREY, "leg{s}", mirror=True)
    k.add("shin", tube([(0.135, -0.04, 0.1), (0.14, -0.05, 0.068)], [0.072, 0.065]), GREY, "leg{s}", mirror=True)
    k.add("foot", ell((0.142, -0.085, 0.028), (0.07, 0.1, 0.03)), GREY, "leg{s}", mirror=True)
    for j, dx in enumerate((-0.03, 0.0, 0.03)):
        k.add(f"toe{j}", cq.claw((0.142 + dx, -0.175, 0.026), (dx * 0.6, -1, -0.15), 0.034, 0.024, 0.018), cq.CLAW,
              "leg{s}", fuse=False, mirror=True, surface="shell")

    k.add("tail", blade([(0, 0.17, 0.25), (0.04, 0.27, 0.17), (0.08, 0.35, 0.1)], [0.06, 0.035, 0.008],
                        (0.6, 0, 0.8), 0.6), ASH, "tail")

    # The ash: Typhlosion's stripe from the tail up the spine and its hood over the crown, grey everywhere else
    stripe_ys = ([0.1, 0.2, 0.3, 0.4, 0.5, 0.58, 0.66, 0.76, 0.86], [0.14, 0.16, 0.165, 0.15, 0.11, 0.06, 0.02, -0.05, -0.08])
    back = ql.back_mask(*stripe_ys, 0.86)
    narrow = lambda P: inside(np.abs(P[:, 0]) - (0.075 + 0.04 * smoothstep(0.7, 0.8, P[:, 2])), 0.0035)
    crown = cq.hood([-0.4, -0.2, -0.17, -0.15, -0.08, 0.0, 0.05, 0.1, 0.4],
                    [0.758, 0.758, 0.765, 0.77, 0.79, 0.75, 0.69, 0.65, 0.65], below=0.64)
    point = lambda P: inside(np.abs(P[:, 0]) - np.interp(P[:, 1], [-0.235, -0.2, -0.17, -0.12, 0.2],
                                                        [0.0, 0.045, 0.12, 0.2, 0.3]), 0.0035)
    dark = lambda P: np.maximum(back(P) * narrow(P), crown(P) * point(P))
    k.paint(dark, ASH)
    k.paint(lambda P: 1 - dark(P), GREY, on=["hips", "belly", "chest", "neck", "head", "snout", "jaw", "arm",
                                             "hand", "thigh", "shin", "foot"])

    for s in (1, -1):
        b = np.array([0.075 * s, -0.035, 0.825])
        tip = b + np.array([0.035 * s, 0.05, 0.06])
        k.add(f"ear{s}", hull([b + (0.025, 0, 0), b - (0.025, 0, 0), b + (0, 0.025, 0), b - (0, 0.025, 0),
                               b - (0, 0, 0.02), tip]), ASH, "head", fuse=False, surface="fur")

    # --- What the Mega adds, in the form's own frame -------------------------------------------------------------
    k.stretch = False

    # Cream kept on the belly and under the jaw only
    def belly(P):
        e = (P[:, 0] / 0.15) ** 2 + ((P[:, 2] - 0.35) / 0.165) ** 2 - 1
        return inside(e, 0.035) * inside(P[:, 1] + 0.06, 0.01)
    k.paint(belly, CREAM, on=["hips", "belly", "chest"])

    def chin(P):
        e = (P[:, 0] / 0.07) ** 2 + ((P[:, 1] + 0.17) / 0.07) ** 2 + ((P[:, 2] - 0.62) / 0.04) ** 2 - 1
        return inside(e, 0.05) * inside(P[:, 2] - 0.645, 0.004)
    k.paint(chin, CREAM, on=["jaw", "neck", "snout"])

    # Lava in thin cracks zigzagging down the back to the tail, three of them
    def cracks(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        m = np.zeros(len(P))
        for x0, ph in ((-0.085, 0.0), (0.0, 1.3), (0.085, 2.6)):
            line = x0 + 0.018 * np.sin(z * 42 + ph)
            m = np.maximum(m, inside(np.abs(x - line) - 0.0075, 0.003))
        return m * inside(z - 0.47, 0.01) * inside(0.2 - z, 0.01) * inside(0.08 - y, 0.004)
    k.paint(cracks, LAVA)

    # The crater on its shoulders: a ring of blocks of rock round the back of the neck, fire welling inside it
    CR = np.array([0.0, 0.11, 0.555])
    for i in range(16):
        a = i * 2 * math.pi / 16
        o = np.array([math.sin(a), math.cos(a), 0.0])           # a = 0 behind, a = pi in front
        at = CR + np.array([math.sin(a) * 0.175, math.cos(a) * 0.145, 0.0])
        hh = 0.06 + 0.012 * ((i * 7) % 3)
        lift = 0.012 * (i % 3)
        tilt = unit(o[0] * 0.18, o[1] * 0.18, 1.0)
        k.add(f"rock{i}", box(at + (0, 0, lift), tilt, 0.04, 0.032, hh, twist=-a), ROCK2 if i % 4 == 1 else ROCK,
              "body", fuse=False, surface="shell")
    # The rim's floor (shuts the ring's far wall from the cameras) and the fire welling in it
    k.add("crater_floor", ell(CR + (0, 0, 0.005), (0.17, 0.14, 0.04)), ROCK, "body", fuse=False, surface="shell")
    k.add("crater_fire", ell(CR + (0, 0, 0.04), (0.15, 0.12, 0.03)), FIRE, "flame", fuse=False, glow=FIRE)
    k.add("crater_heart", ell(CR + (0, -0.005, 0.058), (0.09, 0.07, 0.02)), HEART, "flame", fuse=False, glow=HEART)

    # Three thick tongues of flame standing out of the crater, the middle one tallest, leaning back a little
    f = cq.Flames()
    for x, z, h in ((0.0, -0.03, 0.32), (-0.075, 0.01, 0.25), (0.075, 0.0, 0.27)):
        root = CR + np.array([x, -z, 0.03])
        tip = CR + np.array([x * 1.6, -(z - 0.05), 0.04 + h])
        d = tip - root
        side = np.cross(unit(*d), (0, 0, 1.0))
        if np.linalg.norm(side) < 1e-3:
            side = np.array([1.0, 0, 0])
        f.flame(root, d, np.linalg.norm(d) * 1.12, 0.1, "flame", unit(1.0, 0.0, 0.0), flick=0.18)
    f.add(k)

    # Bracers of the same rock on the forearms
    for s, bone in ((1, "armR"), (-1, "armL")):
        sh = np.array([0.13 * s, -0.04, 0.53]) * K
        hd = np.array([0.208 * s, -0.19, 0.44]) * K
        d = hd - sh
        c1 = sh + d * 0.62
        c2 = sh + d * 0.84 + np.array([0, 0, 0.008])
        k.add(f"bracer{bone}", box(c1, d, 0.062, 0.058, 0.04, taper=1.0), ROCK, bone, fuse=False, surface="shell")
        k.add(f"cuff{bone}", box(c2, d, 0.054, 0.05, 0.026, twist=0.35 * s, taper=1.0), ROCK2, bone, fuse=False,
              surface="shell")

    # --- Typhlosion's face, in its own frame again -----------------------------------------------------------------
    k.stretch = True

    def mouth(u, v):
        c = np.tile(MOUTH, (len(u), 1))
        c[(u / 0.6) ** 2 + ((v + 0.65) / 0.5) ** 2 < 1] = TONGUE
        return c
    k.decal((0, -0.248, 0.668), unit(0, -0.8, -0.55), (0.048, 0.024), mouth, eps=0.004,
            outline=polygon([(-1, 0.6), (-0.5, 0.25), (0.5, 0.25), (1, 0.6), (0.7, -0.4), (0, -0.85), (-0.7, -0.4)]),
            rings=12, segs=56)
    k.decal((0, -0.29, 0.71), (0, -1, 0.2), (0.013, 0.01), cq.NOSE, eps=0.003, rings=4, segs=20)

    cut = ql.brow_cut(0.22, 0.42)
    de = unit(0.6, -0.74, 0.18)
    E = cq.on_ell(H + (0, -0.01, 0.0), HR * 1.04, de)
    eyes = ql.eyes_under_brow(k, E, de, (0.048, 0.048), ql.fierce_eye(ql.RED, cut, iris_c=(-0.15, -0.18),
                                                                      iris_r=0.56, pupil_r=0.28, rim=0.8),
                              cut, srgb(GREY), line=0.22)

    k.build(voxel=0.0075, smooth=4, band=0.006)
    k.eye_states(eyes)
    k.export(out_path)


if __name__ == "__main__":
    typhlosion_mega(*args())
