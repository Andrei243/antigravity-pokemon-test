# Terapagos (Normal Form) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/terapagos.py -- <the export's Terapagos.glb> <out.glb>
#
# The design, from the references: a turtle on four thick legs, its skin deep navy-violet; a long thick neck curving
# up and forward to a round head with a short snout, one great eye a side (a blue iris with a white four-pointed star
# in it, a dark rim and a pink-red ring round it), an open pink smiling mouth; a cheek spike of mint crystal on each
# side of the head and a smaller one on its crown, and from the back of its crown a short zigzag of crystals, mint and
# then lavender at the tip. Thick mint rings round the neck's root and round each ankle, mint four-pointed stars on the
# legs and the neck. The shell is a cut gem lying along the back, faceted, pale yellow-green in front and mint behind,
# glowing a little. A short tail ends in a mint four-pointed star. (The task's text spoke of a small blue turtle: the
# pictures show it navy-violet with a long neck, standing high on its legs; the shell is the gem.)
# The skeleton: body (0, 0, 0.12), neck (0, -0.1, 0.14), head (0, -0.16, 0.3), tail (0, 0.11, 0.12),
# legR/legL (+-0.07, -0.06, 0.1), hindR/hindL (+-0.07, 0.06, 0.1).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, lathe, outward, polygon, slab, hull, axes, args

NAVY, NAVY_DARK = "#2E2B80", "#1E1B57"
MINT, MINT_DARK, LAVENDER = "#6FE3CB", "#3FB9A3", "#B6A0EA"
GEM_FRONT, GEM_MID, GEM_BACK = "#E1EDB2", "#C6EBC2", "#A9E8D2"
PINK, MOUTH, IRIS, IRIS_DARK = "#E2406E", "#E9677F", "#45A8F0", "#1E5FB8"
INK = "#120F2E"
WHITE = np.array([1.0, 1.0, 1.0])

HEAD_C, HEAD_R = np.array([0.0, -0.168, 0.305]), np.array([0.057, 0.058, 0.061])
SNOUT_C, SNOUT_R = np.array([0.0, -0.214, 0.288]), np.array([0.04, 0.034, 0.034])
EYE_D, EYE_HALF = (0.78, -0.62, 0.18), (0.026, 0.027)
VOXEL = 0.0036


def on_ell(c, r, d):
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def torus(c, axis, radius, thick, n=12, seg=56):
    prof = [(radius + thick * math.cos(t), thick * math.sin(t)) for t in np.linspace(0, 2 * math.pi, n + 1)]
    return lathe(c, axis, prof, seg=seg)


def star_outline(k=0.34):
    """A four-pointed star."""
    pts = []
    for i in range(8):
        a = math.pi / 2 + i * math.pi / 4
        r = 1.0 if i % 2 == 0 else k
        pts.append((r * math.cos(a), r * math.sin(a)))
    return pts


STAR = polygon(star_outline())


def spike(root, tip, r):
    """A crystal spike: a four-sided pyramid from a square root to a point."""
    root, tip = np.array(root, float), np.array(tip, float)
    a, b = axes(tip - root)
    return hull([root + a * r, root - a * r, root + b * r, root - b * r, tip])


def crystal(p0, p1, r):
    """A crystal of the antenna: a double pyramid from p0 to p1, square in its middle."""
    p0, p1 = np.array(p0, float), np.array(p1, float)
    a, b = axes(p1 - p0)
    m = (p0 + p1) / 2
    return hull([p0, p1, m + a * r, m - a * r, m + b * r, m - b * r])


# --- the parts ----------------------------------------------------------------------------------------------------
def trunk_and_legs(k):
    k.add("trunk", ell((0.0, 0.01, 0.125), (0.078, 0.105, 0.05)), NAVY, "body")
    k.add("chest", ell((0.0, -0.07, 0.135), (0.05, 0.04, 0.045)), NAVY, "neck")
    for nm, y, foot_y in (("leg", -0.06, -0.078), ("hind", 0.06, 0.076)):
        hip = np.array([0.068, y, 0.108])
        foot = np.array([0.094, foot_y, 0.03])
        k.add(nm + "haunch", ell(hip, (0.042, 0.044, 0.04)), NAVY, nm + "{s}", mirror=True)
        k.add(nm, tube([hip, (hip + foot) / 2 + (0.004, 0, 0), foot], [0.035, 0.031, 0.032]), NAVY, nm + "{s}",
              mirror=True)
        k.add(nm + "foot", ell(foot + (0.004, -0.008 if y < 0 else 0.004, -0.012), (0.038, 0.043, 0.02)), NAVY,
              nm + "{s}", mirror=True)
        # the ring round the ankle
        d = unit(*(hip - foot))
        k.add(nm + "ring", torus(foot + d * 0.026, d, 0.034, 0.0105), MINT, nm + "{s}", fuse=False, mirror=True,
              glow="#2E6E62")
        # a star on the leg's outer side
        p = (hip + foot) / 2 + (0.034, 0.0, 0.004)
        k.decal(p, (1.0, 0.0, 0.0), (0.016, 0.022), MINT, up=(0, 0, 1), eps=0.0028, outline=STAR, rings=10, segs=64,
                mirror=True)
    # toes, darker, under each foot's front
    k.paint(lambda P: inside(P[:, 2] - 0.016, 0.003), NAVY_DARK, on=["legfoot", "hindfoot"])


def neck_and_head(k):
    pts = [(0.0, -0.075, 0.14), (0.0, -0.12, 0.17), (0.0, -0.148, 0.21), (0.0, -0.162, 0.25), (0.0, -0.168, 0.28)]
    k.add("neck", tube(pts[:3], [0.044, 0.041, 0.039]), NAVY, "neck")
    k.add("neckup", tube(pts[2:], [0.039, 0.039, 0.04]), NAVY, "head")
    k.add("ghead", ell(HEAD_C, HEAD_R), NAVY, "head")
    k.add("snout", ell(SNOUT_C, SNOUT_R), NAVY, "head")
    # the ring round the neck's root
    k.add("neckring", torus((0.0, -0.098, 0.15), unit(0, -0.55, 1), 0.046, 0.0125), MINT, "neck", fuse=False,
          glow="#2E6E62")
    # stars on the neck's sides
    for s in (1, -1):
        k.decal((0.033 * s, -0.137, 0.205), (s, -0.25, 0.0), (0.012, 0.017), MINT, up=(0, 0, 1), eps=0.0028,
                outline=STAR, rings=10, segs=64)
    # the mouth: open, pink, smiling, under the snout's front
    def mouth(u, v):
        c = np.tile(srgb(MOUTH), (len(u), 1))
        c[v > 0.55 - 0.5 * u * u] = srgb(NAVY)
        c[np.hypot(u, v + 0.35) < 0.4] = srgb("#F7A3B3")
        return c
    p, n = on_ell(SNOUT_C, SNOUT_R, (0.0, -1.0, -0.55))
    k.decal(p, n, (0.024, 0.014), mouth, eps=0.0025, rings=12, segs=56,
            outline=polygon([(-1, 0.55), (-0.6, -0.4), (0, -0.85), (0.6, -0.4), (1, 0.55), (0, 0.3)]))
    for s in (1, -1):
        p, n = on_ell(SNOUT_C, SNOUT_R, (0.35 * s, -1.0, 0.35))
        k.decal(p, n, (0.004, 0.004), INK, eps=0.0022, rings=4, segs=20)
    # crystal spikes: one on each cheek behind the eye, a small one on the crown
    for s in (1, -1):
        root = HEAD_C + (0.048 * s, 0.022, -0.012)
        k.add("cheek" + ("R" if s > 0 else "L"), spike(root, root + (0.042 * s, 0.03, 0.006), 0.014), MINT,
              "head", fuse=False, glow="#2E6E62")
    root = HEAD_C + (0.0, -0.022, 0.052)
    k.add("crown", spike(root, root + (0.008, -0.012, 0.03), 0.012), MINT, "head", fuse=False, glow="#2E6E62")
    # the antenna: a zigzag of crystals from the back of the crown, up and forward, its plane turned to +X
    a = HEAD_C + (0.0, 0.02, 0.054)
    b = a + (0.022, -0.006, 0.03)
    c = b + (-0.004, -0.03, 0.022)
    d = c + (0.024, -0.026, 0.016)
    k.add("ant0", crystal(a, b, 0.011), MINT, "head", fuse=False, glow="#2E6E62")
    k.add("ant1", crystal(b, c, 0.012), MINT, "head", fuse=False, glow="#2E6E62")
    k.add("ant2", crystal(c, d, 0.0115), LAVENDER, "head", fuse=False, glow="#4A3F70")


def shell(k):
    """The gem on the back: two hulls of one octagonal section, pale yellow-green in front, mint behind."""
    sec = [(-0.048, 0.205), (0.048, 0.205), (0.086, 0.176), (0.088, 0.13), (0.064, 0.105), (-0.064, 0.105),
           (-0.088, 0.13), (-0.086, 0.176)]
    zc = 0.152
    def ring(y, s):
        return [(x * s, y, zc + (z - zc) * s) for x, z in sec]
    k.add("gemfront", hull(ring(-0.098, 0.58) + ring(-0.066, 1.0) + ring(-0.014, 1.0)), GEM_FRONT, "body",
          fuse=False, surface="shell", glow="#5C6648")
    k.add("gemmid", hull(ring(-0.018, 0.997) + ring(0.046, 0.997)), GEM_MID, "body",
          fuse=False, surface="shell", glow="#516A55")
    k.add("gemback", hull(ring(0.042, 0.994) + ring(0.09, 0.994) + ring(0.126, 0.6)), GEM_BACK, "body",
          fuse=False, surface="shell", glow="#466A5E")


def tail(k):
    k.add("tail", tube([(0.0, 0.1, 0.125), (0.012, 0.135, 0.13), (0.026, 0.162, 0.138)], [0.02, 0.015, 0.012]), NAVY,
          "tail")
    tip = np.array([0.034, 0.178, 0.142])
    out = unit(0.55, 0.84, 0.0)
    pts = [(x * 0.026, y * 0.026) for x, y in star_outline(0.38)]
    k.add("tailstar", slab(pts, tip, out, (0, 0, 1), 0.014), MINT, "tail", fuse=False, glow="#2E6E62")


def eye(u, v):
    c = np.tile(srgb(IRIS), (len(u), 1))
    q = np.hypot(u, v)
    c[v > 0.35] = srgb(IRIS_DARK)
    # the white four-pointed star, large, a little up
    su, sv = u / 0.5, (v - 0.02) / 0.55
    star = (np.abs(su) ** 0.5 + np.abs(sv) ** 0.5) < 1.0
    c[star] = WHITE
    c[q > 0.74] = srgb(INK)
    c[q > 0.86] = srgb(PINK)
    return c


def lay_eyes(k, state):
    lid, ink = srgb(NAVY), srgb(MINT)
    for s in (1, -1):
        p, n = on_ell(HEAD_C, HEAD_R, (EYE_D[0] * s, EYE_D[1], EYE_D[2]))
        look = eye_look(state, eye, lid, ink, line=0.16)
        k.decal(p, n, EYE_HALF, look if s > 0 else (lambda u, v, f=look: f(-u, v)), eps=0.0024, rings=20, segs=56)


def terapagos(src, dst):
    k = Kit(src, coat="shell")
    trunk_and_legs(k)
    neck_and_head(k)
    shell(k)
    tail(k)
    k.build(voxel=VOXEL, smooth=4, band=0.005, wband=0.03)
    k.eye_states(lambda st: lay_eyes(k, st))
    k.export(dst)


if __name__ == "__main__":
    terapagos(*args())
