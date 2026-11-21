# Liepard sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Purrloin's script (purrloin.py, imported: its head, ears and lining, pink mask, eyes, mouth and the tail's sickle):
#   blender --background --factory-startup --python sculpts/liepard.py -- <the export's Liepard.glb> <out.glb>
# A sleek purple leopard on four long legs: a deep chest and broad shoulders and thighs over tapering lower legs, a
# thick neck carrying a small head forward and up; a long pointed muzzle, yellow below, pointed ears lined pink, a pink
# mask over each half-shut green eye, thin yellow streaks on the brow, two yellow whiskers a side; the throat, chest and
# belly yellow, the lower legs yellow below a ragged edge; yellow rings over the back and flanks; a long thin tail
# rising high behind to a great sickle sweeping back.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, tri, polygon, args, sibling

p = sibling("purrloin")

# --- colours: the line's, the cream grown into Liepard's yellow --------------------------------------------------------
PURPLE = p.PURPLE
YELLOW = "#F2CC3A"
PINK = p.PINK

# --- the frame (the skeleton's joints: body (0, 0, 0.28), head (0, -0.17, 0.34), ears (+-0.03, -0.21, 0.425),
#     forelegs (+-0.05, -0.13, 0.24), hind legs (+-0.05, 0.15, 0.24), tail (0, 0.19, 0.3)) ------------------------------
DZ = 0.02                                # the head carried up from where it was first laid out
H = np.array([0, -0.235, 0.405 + DZ])
HR = np.array([0.064, 0.07, 0.059])


def lifted(poly):
    return [(a, z + DZ) for a, z in poly]
LEG_X = 0.054
FORE_Y, HIND_Y = -0.12, 0.15
OUT = p.OUT


def add_head(k):
    p.add_head(k, H, HR, PURPLE, neck=([(0, -0.115, 0.3), (0, -0.17, 0.36), (0, -0.205, 0.405)], [0.052, 0.046, 0.042]),
               tufts=False)
    # The long pointed muzzle, a little down
    k.add("snout", ell(H + (0, -0.075, -0.022), (0.03, 0.04, 0.025), rot=(-6, 0, 0)), PURPLE, "head")
    # a short point of fur back from each cheek
    for s in (1, -1):
        b = H + np.array([0.045 * s, 0.004, -0.02])
        k.add(f"cheek{'R' if s > 0 else 'L'}", blade([b, b + (0.014 * s, 0.018, -0.004), b + (0.024 * s, 0.034, -0.012)],
                                                     [0.014, 0.009, 0.002], (0, 0, 1), 0.5), PURPLE, "head")


def add_body(k):
    # A long deep trunk: the chest deep and forward, the waist drawn up a little, a round rump
    k.add("chest", ell((0, -0.095, 0.27), (0.072, 0.095, 0.08)), PURPLE, "body")
    k.add("middle", ell((0, 0.025, 0.272), (0.064, 0.13, 0.068)), PURPLE, "body")
    k.add("rump", ell((0, 0.14, 0.278), (0.07, 0.08, 0.076)), PURPLE, "body")


def add_legs(k):
    # Broad shoulders and thighs fused over the top of each leg; the lower leg tapering to a round paw
    k.add("shoulder", ell((LEG_X - 0.006, FORE_Y, 0.235), (0.042, 0.054, 0.075)), PURPLE, "leg{s}", mirror=True)
    k.add("leg", tube([(LEG_X, FORE_Y, 0.22), (LEG_X + 0.002, FORE_Y - 0.008, 0.115), (LEG_X + 0.004, FORE_Y - 0.016, 0.024)],
                      [0.034, 0.025, 0.021]), PURPLE, "leg{s}", mirror=True)
    k.add("paw", ell((LEG_X + 0.004, FORE_Y - 0.03, 0.015), (0.024, 0.032, 0.015)), PURPLE, "leg{s}", mirror=True)
    k.add("thigh", ell((LEG_X - 0.004, HIND_Y + 0.006, 0.24), (0.048, 0.075, 0.088)), PURPLE, "hind{s}", mirror=True)
    k.add("hind", tube([(LEG_X, HIND_Y + 0.02, 0.19), (LEG_X + 0.003, HIND_Y + 0.045, 0.1),
                        (LEG_X + 0.004, HIND_Y + 0.02, 0.024)], [0.034, 0.024, 0.021]), PURPLE, "hind{s}", mirror=True)
    k.add("hpaw", ell((LEG_X + 0.004, HIND_Y + 0.002, 0.015), (0.024, 0.032, 0.015)), PURPLE, "hind{s}", mirror=True)


def add_tail(k):
    b = np.array([0, 0.205, 0.305])
    z = np.array([0, 0, 1.0])
    pts = [b, b + (0, 0.035, 0.03), b + OUT * 0.04 + z * 0.08, b + OUT * 0.035 + z * 0.13, b + OUT * 0.055 + z * 0.165]
    k.add("tail", tube(pts, [0.016, 0.013, 0.012, 0.011, 0.011]), PURPLE, "tail")
    # the sickle: Purrloin's grown into a great blade sweeping back from the tail's top and down to a point, jags on its
    # upper edge, in the plane of the tail's sweep (back and out to +X)
    k.add("sickle", p.slab(PLUME, pts[-1] - z * 0.006, OUT, (0, 0, 1.0), 0.017), PURPLE, "tail", fuse=False)


PLUME = [(-0.008, -0.01), (0.0, 0.012), (0.03, 0.042), (0.062, 0.058), (0.072, 0.048), (0.094, 0.062), (0.106, 0.05),
         (0.135, 0.054), (0.165, 0.036), (0.192, 0.0), (0.204, -0.042), (0.165, -0.006), (0.125, 0.012),
         (0.085, 0.013), (0.045, 0.002), (0.016, -0.016)]


def add_yellow(k):
    LEGS = ["leg", "paw", "hind", "hpaw"]

    # The lower legs, below a ragged edge; the purple painted back above it so the edge is crisp
    def edge(P):
        hind = P[:, 1] > 0.02
        top = np.where(hind, 0.12, 0.135)
        a = np.arctan2(P[:, 1] - np.where(hind, HIND_Y + 0.03, FORE_Y - 0.01), np.abs(P[:, 0]) - LEG_X)
        return top + 0.012 * tri(a * 3 / math.pi)
    k.paint(lambda P: inside(P[:, 2] - edge(P), 0.002), YELLOW, on=LEGS)

    # The throat, the chest and a band along the belly, with a ragged edge up the chest's sides
    def under(P):
        y, z = P[:, 1], P[:, 2]
        line = 0.315 + (-0.1 - y) * 0.75 + 0.008 * tri((y + z) / 0.03)
        front = inside(z - line, 0.002) * (y < -0.04) * (y > -0.27)
        belly = inside(z - (0.222 + 0.006 * tri(y / 0.04)), 0.002) * (y > -0.08) * (y < 0.11)
        return np.maximum(front, belly) * (z > 0.17) * (np.abs(P[:, 0]) < 0.06)
    k.paint(under, YELLOW, on=["chest", "middle", "neck", "shoulder", "head", "snout"])


def add_face(k):
    # The yellow of the muzzle and the jaw, under a line from below the eye to the chin
    FACE = [(0.0, 0.397), (0.016, 0.394), (0.032, 0.389), (0.05, 0.384), (0.064, 0.376), (0.06, 0.36),
            (0.03, 0.35), (0.0, 0.348), (-0.001, 0.37)]
    p.add_face(k, H, HR, lifted(FACE), YELLOW)
    # The pink mask round each eye, running up and back toward the ear
    MASK = [(0.008, 0.404), (0.016, 0.414), (0.028, 0.426), (0.044, 0.44), (0.06, 0.436), (0.07, 0.418),
            (0.064, 0.404), (0.048, 0.396), (0.03, 0.396), (0.014, 0.398)]
    p.add_mask(k, H, HR, lifted(MASK), PINK)
    p.add_streaks(k, H, HR, YELLOW, size=0.8)
    p.add_mouth(k, H + np.array([0, -0.106, -0.04]), (0.013, 0.007), ground=YELLOW)


def add_whiskers(k):
    # Two short yellow whiskers a side, from the muzzle's side out and back
    for i, (mid, end) in enumerate((((0.042, -0.092, -0.008), (0.066, -0.082, -0.002)),
                                    ((0.042, -0.09, -0.02), (0.064, -0.08, -0.03)))):
        k.add(f"whisker{i}", tube([H + (0.024, -0.096, -0.014 - 0.006 * i), H + mid, H + end], [0.0045, 0.004, 0.003],
                                  seg=8, per=6), YELLOW, "head", fuse=False, mirror=True, surface="hair")


# --- the rings: yellow rosettes over the back and the flanks, each a yellow patch with a purple one inside -------------
RINGS = [(-0.07, 0.31, 0.017), (-0.01, 0.315, 0.015), (0.05, 0.305, 0.017), (0.13, 0.31, 0.018),
         (0.165, 0.26, 0.017), (0.02, 0.26, 0.014), (-0.1, 0.255, 0.014), (0.1, 0.255, 0.015)]
RING = [(1, 0), (0.7, 0.75), (0, 1), (-0.75, 0.65), (-1, -0.05), (-0.6, -0.8), (0.1, -1), (0.8, -0.65)]


def body_x(y, z):
    for c, r in (((0, -0.095, 0.27), (0.072, 0.095, 0.08)), ((0, 0.025, 0.272), (0.064, 0.13, 0.068)),
                 ((0, 0.14, 0.278), (0.07, 0.08, 0.076))):
        q = 1 - ((y - c[1]) / r[1]) ** 2 - ((z - c[2]) / r[2]) ** 2
        if q > 0.05:
            return r[0] * math.sqrt(q), c[2], r[2]
    return 0.06, 0.27, 0.07


def add_rings(k):
    for j, (y, z, s) in enumerate(RINGS):
        x, zc, rz = body_x(y, z)
        for side in (1, -1):
            c = np.array([side * x, y, z])
            n = unit(side * 1.0, 0, (z - zc) / rz * 1.2)
            up = unit(0, -1, 0.3)
            k.decal(c, n, (s, s * 0.8), YELLOW, up=up, eps=0.0035, outline=polygon(RING), rings=8, segs=36)
            k.decal(c, n, (s * 0.52, s * 0.42), PURPLE, up=up, eps=0.006, outline=polygon(RING), rings=6, segs=36)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    add_body(k)
    add_legs(k)
    add_head(k)
    p.add_ears(k, ear=[(0.026, -0.235, 0.458), (0.035, -0.231, 0.485), (0.045, -0.225, 0.515), (0.054, -0.217, 0.542)],
               radii=[0.028, 0.025, 0.014, 0.002], color=PURPLE, lining=PINK, width=0.014)
    add_tail(k)
    add_yellow(k)
    add_face(k)
    add_whiskers(k)
    add_rings(k)
    k.build(voxel=0.0045, band=0.004)
    k.eye_states(lambda s: p.lay_eyes(k, s, H, HR, x=0.026, dz=0.004, half=(0.017, 0.012), lid=PINK))
    k.export(out)
