# Terrakion sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/terrakion.py -- <the export's Terrakion.glb> <out.glb>
#
# Built on raikou.py's frame (trunk, legs, eyes) and cobalion.py's (hooves), through sibling. From the references: a
# heavy grey bull of rock, the trunk deep and low, the forequarters hunched high, short thick legs with tan cuffs
# (two bands with grey between) over black hooves. A big head carried low and forward: a tan face set in a grimace of
# gritted white teeth, orange eyes under a scowl, the chin dark. Over the head a dark brown helmet that dips to a point
# between the eyes, three dark spikes standing on its crown, and two great broad brown horns sweeping out from its
# sides, back and down like plates. A tan band runs from the throat down the chest; a dark stripe along the spine; two
# orange bars stacked on each upper flank; a short grey spike for a tail.
# Bones: body (0, 0, 0.42); head (0, -0.22, 0.48); legs (+-0.11, -0.17, 0.36); hind legs (+-0.11, 0.18, 0.36);
# tail (0, 0.24, 0.44).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, lathe, outward, srgb, inside, unit, polygon, args, sibling)

r = sibling("raikou")
c = sibling("cobalion")
sui = sibling("suicune")

GREY = "#9EA2A8"
DARK = "#5C5E66"
TAN = "#D8C390"
HELM = "#6A5242"
SPIKE = "#4E4A4A"
ORANGE = "#F07A28"
HOOF = "#2C2E34"
TEETH, GUM = "#F6F6F4", "#2A2228"
EYE, IRIS, PUPIL = "#F6F4EE", "#EE7A22", "#1A1012"

LEG_X = 0.11
FORE_Y, HIND_Y = -0.17, 0.18
LEG_TOP = 0.36
LEG_R = (0.086, 0.07, 0.062)
PAW_R = (0.074, 0.084, 0.034)
BODY_Z = 0.405
H = np.array([0.0, -0.345, 0.5])
HR = np.array([0.1, 0.092, 0.1])
EYE_DIR = unit(0.5, -0.8, 0.12)
EYE_R = 0.026
MUZ = H + np.array((0, -0.065, -0.045))
MUZ_R = np.array((0.08, 0.062, 0.062))


def add_head(k):
    k.add("neck", ell((0, -0.23, 0.47), (0.13, 0.1, 0.13)), GREY, "head")
    k.add("head", ell(H, HR), TAN, "head")
    k.add("muzzle", ell(MUZ, MUZ_R), TAN, "head")
    # The chin and the jaw's underside dark
    near = lambda P: np.linalg.norm((P - H) / (0.16, 0.16, 0.16), axis=1) < 1
    k.paint(lambda P: inside(P[:, 2] - (H[2] - 0.085 - 0.25 * (P[:, 1] - H[1])), 0.003) * near(P)
            * (P[:, 1] < H[1] + 0.02), DARK)


def helmet_keep(D):
    """The helmet over the crown and the back of the head, dipping to a point between the eyes."""
    dip = 0.32 * np.clip(-D[:, 1], 0, 1) * np.clip(1 - 3.2 * np.abs(D[:, 0]), 0, 1)
    return D[:, 2] > 0.42 - dip


def add_helmet(k):
    k.add("helmet", r.plate(H, HR, helmet_keep, lift=0.024, under=0.01), HELM, "head", fuse=False, surface="shell")


def add_spikes(k):
    """Three dark spikes on the crown, the middle one tallest, the others leaning out."""
    top, _ = r.on_ell(H, HR, (0, -0.25, 1))
    for j, (dx, tip) in enumerate(((0.0, (0.0, 0.02, 0.12)), (0.055, (0.085, 0.035, 0.09)),
                                   (-0.055, (-0.085, 0.035, 0.09)))):
        b = top + np.array((dx, 0.0, 0.0))
        t = top + np.array(tip)
        pts = [b + (0.042, -0.02, 0), b + (-0.042, -0.02, 0), b + (0.04, 0.035, 0), b + (-0.04, 0.035, 0),
               b + (0, -0.026, -0.01), b + (0, 0.034, -0.01), t]
        k.add(f"spike{j}", hull(pts), SPIKE, "head", fuse=False, surface="shell")


def add_horns(k):
    """The two great horns: broad brown plates from the helmet's sides, out, back and down, faces to the front."""
    path = sui.smooth_path([H + (0.06, 0.0, 0.06), H + (0.16, 0.03, 0.1), H + (0.25, 0.09, 0.07),
                            H + (0.27, 0.15, -0.03), H + (0.252, 0.18, -0.13)])
    k.add("horn", sui.ribbon(path, lambda u: 0.15 * (1 - 0.6 * u), 0.032,
                             [unit(0, 0.35, 1), unit(-0.35, 0.35, 0.9), unit(-0.85, 0.3, 0.4), unit(-1, 0.25, 0.05)]),
          HELM, "head", fuse=False, mirror=True, surface="shell")


def add_teeth(k):
    """The grimace: gritted white teeth in a dark rim, one dark line between the rows."""
    m, n = r.on_ell(MUZ, MUZ_R, (0, -1, -0.25))
    k.decal(m, n, (0.05, 0.026), GUM, eps=0.003, outline=polygon([(-1, -1), (1, -1), (1, 1), (-1, 1)]), rings=10, segs=48)
    k.decal(m, n, (0.042, 0.019), TEETH, eps=0.0045, outline=polygon([(-1, -1), (1, -1), (1, 1), (-1, 1)]), rings=10,
            segs=48)
    k.decal(m, n, (0.042, 0.0038), GUM, eps=0.006, outline=polygon([(-1, -1), (1, -1), (1, 1), (-1, 1)]), rings=4,
            segs=40)


def add_chest(k):
    """The tan band from the throat down the chest, widening up into the throat."""
    def band(P):
        w = 0.05 + 0.3 * np.clip(P[:, 2] - 0.42, 0, 1)
        return inside(np.abs(P[:, 0]) - w, 0.003) * (P[:, 1] < -0.12) * (P[:, 2] > 0.24) * (P[:, 2] < 0.5)
    k.paint(band, TAN, on=["chest", "middle", "neck", "bib"])


def add_back(k):
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.055, 0.003) * inside(0.54 - P[:, 2], 0.003) * (P[:, 1] > -0.2) * (P[:, 1] < 0.1), DARK)


def box(cen, half, rot_z=0.0, rot_x=0.0, bevel=0.35):
    """A bevelled box as a hull (its corners cut by bevel of the smallest half)."""
    hx, hy, hz = half
    b = min(half) * bevel
    pts = []
    for sx in (-1, 1):
        for sy in (-1, 1):
            for sz in (-1, 1):
                pts += [(sx * hx, sy * (hy - b), sz * (hz - b)), (sx * (hx - b), sy * hy, sz * (hz - b)),
                        (sx * (hx - b), sy * (hy - b), sz * hz)]
    P = np.array(pts)
    a, x = math.radians(rot_z), math.radians(rot_x)
    Rz = np.array([[math.cos(a), -math.sin(a), 0], [math.sin(a), math.cos(a), 0], [0, 0, 1]])
    Rx = np.array([[1, 0, 0], [0, math.cos(x), -math.sin(x)], [0, math.sin(x), math.cos(x)]])
    return hull(P @ (Rz @ Rx).T + np.array(cen))


def add_bars(k):
    """Two orange bars stacked on each upper flank behind the shoulder."""
    k.add("bar0", box((0.158, 0.02, 0.5), (0.022, 0.075, 0.03), rot_x=-12), ORANGE, "body", fuse=False, mirror=True,
          surface="shell")
    k.add("bar1", box((0.168, 0.05, 0.43), (0.022, 0.07, 0.028), rot_x=-12), ORANGE, "body", fuse=False, mirror=True,
          surface="shell")


def add_tail(k):
    b = np.array((0.0, 0.2, 0.5))
    pts = [b + (0.028, 0, -0.03), b + (-0.028, 0, -0.03), b + (0.028, 0, 0.03), b + (-0.028, 0, 0.03),
           b + (0.01, 0.06, 0.04), b + (-0.01, 0.06, 0.04), b + (0, 0.12, 0.1)]
    k.add("tail", hull(pts), GREY, "tail", fuse=False, surface="shell")


def add_cuffs(k):
    """Two tan bands round each lower leg with grey between, over the hoof."""
    for bone, y in (("leg{s}", FORE_Y - 0.008), ("hind{s}", HIND_Y + 0.008)):
        for j, (z0, z1) in enumerate(((0.055, 0.17),)):
            rr = r.leg_radius((z0 + z1) / 2, LEG_TOP, LEG_R)
            ri, ro = rr - 0.02, rr + 0.006
            k.add(f"cuff{bone[0]}{j}", lathe((LEG_X, y, 0), (0, 0, 1), [(ri, z0), (ro, z0), (ro, z1), (ri, z1)], seg=48),
                  TAN, bone, fuse=False, mirror=True, surface="shell")


SHUT_LINE = "#3A2A22"


def eye_top(u):
    return 0.12 + 0.62 * u


def eyes(k, state):
    p, n = r.on_ell(H, HR, EYE_DIR)
    r.lay_eyes(k, state, p, n, EYE_R, r.eye_open(EYE, IRIS, PUPIL, iris_r=(0.66, 0.82)), TAN, SHUT_LINE, eye_top)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    r.add_trunk(k, GREY, z=BODY_Z, w=0.17, deep=0.165, front=-0.15, back=0.17)
    r.add_legs(k, GREY, LEG_X, FORE_Y, HIND_Y, LEG_TOP, r=LEG_R, shoulder=(0.1, 0.11, 0.15), thigh=(0.1, 0.12, 0.15),
               paw=PAW_R, paw_color=HOOF)
    c.add_hooves(k, HOOF, top=0.04)
    add_head(k)
    add_chest(k)
    add_teeth(k)
    add_cuffs(k)
    add_helmet(k)
    add_spikes(k)
    add_horns(k)
    add_bars(k)
    add_tail(k)
    k.build(voxel=0.0065, band=0.004)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)
