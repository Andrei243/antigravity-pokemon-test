# Motanor sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/motanor.py -- <the export's Motanor.glb> <out.glb>
# Pisicut grown old (built with pisicut.py's own functions: its stripes, its thread, its tail and its nose and mouth):
# from the picture (10032-Motanor.jpg), the big old tomcat of the village tales standing four-square, heavy in the
# shoulders and the chest, on thick legs with broad paws; a big head with shaggy silver ruffs at the cheeks and a pale
# muzzle and chin; narrow yellow eyes under a scowl, a pink-brown nose, a frown; upright ears pink inside, the right
# one notched; dark grey tabby stripes over the back, down the flanks and round the legs, three on the forehead; a
# pale shaggy chest; a pink cross of an old scar on the shoulder and a scar over the brow; the red thread of its
# kittenhood frayed and knotted round its neck, its ends hanging; a long striped tail hanging down and curling back
# along the ground behind it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, slab, outward, srgb, inside, unit, polygon, axes, args, sibling)

P = sibling("pisicut")
L = P.L

GREY = "#9A98A0"
STRIPE = "#5C5A64"
SILVER = "#D9D8DD"
PALEGREY = "#C4C3C9"
EARIN = "#D9A0A6"
NOSE = "#B98288"
SCAR = "#C98E92"
THREAD = "#A8342F"
YELLOW, AMBER, PUPIL = "#F0CE3A", "#C89424", "#262014"
INK = "#1E1C20"

# --- the frame (joints: body (0, 0.02, 0.42), head (0, -0.22, 0.56), ears (+-0.07, -0.25, 0.74), legs
#     (+-0.1, -0.17, 0.32), hind (+-0.1, 0.2, 0.32), tail (0, 0.28, 0.47)) --------------------------------------------
H = np.array([0.0, -0.275, 0.635])
HR = np.array([0.142, 0.125, 0.123])
LEG_X = 0.1


def ear(k, side, notch=False):
    """A flat upright ear, broad at the root, its face turned forward and a little out, pink inside; notched."""
    s = side
    root = np.array([0.083 * s, -0.262, 0.705])
    d = unit(0.28 * s, 0.06, 1.0)
    n = unit(0.25 * s, -1.0, 0.12)
    ex = np.cross(d, n)
    Ln, w = 0.14, 0.055
    if notch:
        o = [(-w, 0), (w, 0), (0.62 * w, 0.38 * Ln), (0.2 * w, 0.49 * Ln), (0.42 * w, 0.6 * Ln), (0, Ln)]
    else:
        o = [(-w, 0), (w, 0), (0, Ln)]
    nm = "ear" + ("R" if s > 0 else "L")
    k.add(nm, slab(o, root, ex, d, 0.026), GREY, nm, fuse=False, surface="fur")
    lin = [(-w * 0.58, Ln * 0.22), (w * 0.58, Ln * 0.22), (0, Ln * 0.8)]
    V, F = slab(lin, root, ex, d, 0.006)
    k.add(nm + "in", (V + n * 0.0155, F), EARIN, nm, fuse=False, surface="fur")


def lay_eyes(k, state):
    look = L.eye_colour(yellow=YELLOW, amber=AMBER, pupil=PUPIL, ink=INK, pupil_w=0.15, pupil_h=0.6)
    L.lay_eyes(k, state, h=H, hr=HR, x=0.06, dz=0.0, half=(0.032, 0.023), lid=GREY, line=INK, look=look, tilt=1.1,
               eps=0.0022)


def motanor(src, out_path):
    k = Kit(src, coat="fur")
    # The body: a deep chest and heavy shoulders, a leaner middle, the rump
    k.add("chest", ell((0, -0.12, 0.43), (0.152, 0.17, 0.18)), GREY, "body")
    k.add("middle", ell((0, 0.06, 0.405), (0.135, 0.2, 0.152)), GREY, "body")
    k.add("rump", ell((0, 0.21, 0.44), (0.138, 0.13, 0.15)), GREY, "body")
    k.add("neck", ell((0, -0.225, 0.555), (0.112, 0.105, 0.12)), GREY, "head")
    # Fat shoulders and thighs fused over the upper legs, short thick legs, broad paws
    k.add("shoulder", ell((LEG_X, -0.15, 0.37), (0.068, 0.085, 0.13)), GREY, "leg{s}", mirror=True)
    k.add("fore", tube([(LEG_X, -0.165, 0.33), (LEG_X, -0.18, 0.16), (LEG_X, -0.19, 0.06)], [0.062, 0.054, 0.051]),
          GREY, "leg{s}", mirror=True)
    k.add("paw", ell((LEG_X, -0.215, 0.032), (0.058, 0.07, 0.034)), GREY, "leg{s}", mirror=True)
    k.add("thigh", ell((LEG_X + 0.005, 0.2, 0.37), (0.075, 0.115, 0.14)), GREY, "hind{s}", mirror=True)
    k.add("hind", tube([(LEG_X, 0.225, 0.3), (LEG_X, 0.26, 0.16), (LEG_X, 0.22, 0.06)], [0.058, 0.05, 0.049]),
          GREY, "hind{s}", mirror=True)
    k.add("hpaw", ell((LEG_X, 0.2, 0.032), (0.056, 0.068, 0.034)), GREY, "hind{s}", mirror=True)

    # The head: big, a pale muzzle and chin, shaggy silver ruffs at the cheeks
    k.add("head", ell(H, HR), GREY, "head")
    k.add("muzzle", ell((0, -0.372, 0.6), (0.066, 0.04, 0.046)), SILVER, "head")
    k.add("chin", ell((0, -0.35, 0.56), (0.05, 0.04, 0.035)), SILVER, "head")
    k.add("jowl", ell((0.085, -0.33, 0.585), (0.06, 0.055, 0.055)), SILVER, "head", mirror=True)
    for i, (z, ln, dd) in enumerate(((0.63, 0.07, (1.0, -0.15, -0.25)), (0.59, 0.08, (1.0, -0.2, -0.55)),
                                     (0.55, 0.065, (0.75, -0.3, -0.85)))):
        for s in (1, -1):
            k.add(f"ruff{i}{'R' if s > 0 else 'L'}", L.spike((0.115 * s, -0.31, z), (dd[0] * s, dd[1], dd[2]), ln * 0.85, 0.045,
                                                             0.02), PALEGREY, "head", fuse=False, surface="fur")
    # a shaggy chest: points of pale fur hanging at its bottom
    for i, (x, z, ln) in enumerate(((0.0, 0.31, 0.07), (0.055, 0.32, 0.06), (-0.055, 0.32, 0.06), (0.1, 0.35, 0.05),
                                    (-0.1, 0.35, 0.05))):
        k.add(f"shag{i}", L.spike((x, -0.255 + abs(x) * 0.5, z + 0.03), (x * 3, -0.35, -1.0), ln * 0.75, 0.034, 0.017),
              PALEGREY, "body", fuse=False, surface="fur")

    # The pale chest and belly, the silver lower face
    k.paint(lambda Q: inside(np.hypot(Q[:, 0] / 0.11, (Q[:, 2] - 0.42) / 0.16) - 1, 0.004)
            * inside(Q[:, 1] + 0.2, 0.006) * (Q[:, 2] < 0.6), PALEGREY, on=["chest", "neck"])
    k.paint(lambda Q: inside(Q[:, 2] - 0.29, 0.01) * (np.abs(Q[:, 0]) < 0.1), PALEGREY, on=["chest", "middle"])
    k.paint(lambda Q: inside(Q[:, 2] - (H[2] - 0.035 + 0.25 * np.abs(Q[:, 0])), 0.003)
            * inside(Q[:, 1] - (H[1] - 0.06), 0.004) * (Q[:, 2] > 0.5), SILVER, on=["head", "jowl", "muzzle", "chin"])

    # Stripes over the back and the flanks, carried round the rump, banded legs, the forehead's three, the cheeks
    P.stripes(k, ["chest", "middle", "rump", "shoulder", "thigh"], -0.2, 0.28, 0.08, 0.6, 0.28, slant=0.18,
              width=0.16, front=lambda Q: 1 - (np.abs(Q[:, 0]) < 0.1) * (Q[:, 1] < -0.2), color=STRIPE)
    P.rump_stripes(k, ["rump", "thigh", "middle"], 0.27, 0.6, 0.26, 0.075, color=STRIPE)
    def legbands(Q):
        x, z = np.abs(Q[:, 0]), Q[:, 2]
        f = np.abs((z - 0.04) / 0.07 - np.round((z - 0.04) / 0.07))
        return inside(f - 0.15, 0.03) * (z > 0.07) * (z < 0.33) * (x > 0.05)
    k.paint(legbands, STRIPE, on=["fore", "hind"])
    P.forehead(k, h=H, hr=HR, color=STRIPE, xs=(-0.035, 0.0, 0.035), w=0.009)
    P.cheek_stripes(k, h=H, hr=HR, color=STRIPE)

    # The ears, the right one notched
    ear(k, 1, notch=True)
    ear(k, -1)

    # Scars: a cross on the right shoulder, a stroke over the right brow
    rect = polygon([(-1, -1), (1, -1), (1, 1), (-1, 1)])
    sc, sn = np.array([0.152, -0.09, 0.47]), unit(1, -0.3, 0.2)
    for a in (35, -35):
        r = math.radians(a)
        k.decal(sc, sn, (0.035, 0.0055), SCAR, up=unit(0, math.sin(r), math.cos(r)), eps=0.0025, outline=rect,
                rings=6, segs=32)
    p, n = L.on_head(0.075, H[2] + 0.05, H, HR)
    k.decal(p, n, (0.006, 0.028), SCAR, up=unit(0.45, 0, 1), eps=0.002, outline=rect, rings=6, segs=32)

    P.add_nose_mouth(k, h=H, hr=HR, dz=-0.042, nose=NOSE, ink=INK, ground=SILVER, size=1.6, eps=0.002)
    P.add_thread(k, cy=-0.233, rx=0.11, ry=0.103, z0=0.567, dz=-0.033, r=0.0105, bow=False, scale=1.7, frayed=True,
                 color=THREAD)
    tail = [(0.0, 0.31, 0.5), (0.03, 0.4, 0.45), (0.075, 0.46, 0.32), (0.11, 0.48, 0.17), (0.14, 0.44, 0.07),
            (0.17, 0.36, 0.045)]
    # The long tail, fused (it hangs clear of the legs), its dark rings painted by the share along its line
    k.add("tail", tube(tail, [0.04, 0.041, 0.04, 0.039, 0.037, 0.034]), GREY, "tail")
    T = tube(tail, [0.0] * len(tail), seg=3, per=12)[0][:-2].reshape(-1, 3, 3)[:, 0]     # its centre line, densely
    cum = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(T, axis=0), axis=1))])
    cum /= cum[-1]
    def tailbands(Q):
        d = ((Q[:, None, :] - T[None]) ** 2).sum(2)
        t = cum[d.argmin(1)]
        f = np.abs(t / 0.12 - np.round(t / 0.12))
        return inside(f - 0.17, 0.02) * (t > 0.2) + inside(0.93 - t, 0.01)
    k.paint(tailbands, STRIPE, on=["tail"])

    k.build(voxel=0.0085, smooth=4, band=0.006, wband=0.06)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out_path)


if __name__ == "__main__":
    motanor(*args())
