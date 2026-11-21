# Delphox sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Braixen's script (braixen.py, imported: its head, white face, ears and plumes, eyes, nose and mouth; and through it
# Fennekin's colours, ear lining, cheek tufts and tail):
#   blender --background --factory-startup --python sculpts/delphox.py -- <the export's Delphox.glb> <out.glb>
# A tall fox mage: a yellow head with a long snout, white below the eyes and in long tufts back from the cheeks, narrow
# red eyes; tall yellow ears and great plumes of red-orange fur streaming out from them like flame; a yellow chest
# with a long white ruff hanging down its front; a long robe of dark red fur from the shoulders to the ground, flaring,
# its hem ragged, orange heart-shaped marks round its lower half; wide red sleeves with ragged cuffs and black hands,
# the right hand forward holding a twig whose tip burns, the left at the chest; black feet with grey claws under the
# hem; a yellow tail out behind to +X, curling up, its tip pale.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, inside, unit, args, sibling

b_ = sibling("braixen")
f = b_.f
YELLOW, FUR, WHITE, BLACK, WOOD = b_.YELLOW, b_.FUR, b_.WHITE, b_.BLACK, b_.WOOD
ROBE = "#B4342A"
MARK = "#F06A34"
HAND = "#2E2A30"
CLAW = "#8E8C94"
PALE = "#FBEFB0"
FLAME, FLAME_IN = "#F2502A", "#FFD24A"

# --- the frame (the skeleton's joints: body (0, 0, 0.32), arms (+-0.05, -0.01, 0.39), head (0, -0.01, 0.42), ears
#     (+-0.03, -0.01, 0.525), legs (+-0.035, 0, 0.2), tail (0, 0.05, 0.26)) -----------------------------------------
# The masses by size, from the pictures: the robe, the ear plumes, the tail, the head.
H = np.array([0, -0.036, 0.482])
HR = np.array([0.07, 0.064, 0.066])
SNOUT = 1.22
ROBE_C = np.array([0, 0.012, 0.215])           # only its y is used: the robe's middle line
HEM_Z = 0.052


# The robe's profile (height, radius): a bell from the shoulders, flaring all the way down to the hem
PROFILE = [(0.395, 0.06), (0.33, 0.082), (0.25, 0.11), (0.16, 0.136), (0.075, 0.154)]
DEPTH = 0.88                                   # front to back against side to side


def robe_radius(z):
    zz = [p[0] for p in PROFILE][::-1]; rr = [p[1] for p in PROFILE][::-1]
    return np.interp(z, zz, rr)


def add_robe(k):
    V, F = tube([(0, ROBE_C[1], z) for z, _ in PROFILE], [r for _, r in PROFILE], seg=40)
    V = V - np.array([0, ROBE_C[1], 0]); V[:, 1] *= DEPTH; V = V + np.array([0, ROBE_C[1], 0])
    V[:, 2] = np.clip(V[:, 2], HEM_Z, 0.43)
    k.add("robe", (V, F), ROBE, "body")
    # the hem's points: many, hanging down close to the ground, a little out
    for i, a in enumerate(np.linspace(0, 2 * math.pi, 18, endpoint=False)):
        c, s = math.sin(a), -math.cos(a)
        b = np.array([0.146 * c, ROBE_C[1] + 0.146 * DEPTH * s, HEM_Z + 0.03])
        d = unit(0.16 * c, 0.16 * s, -1)
        ln = 0.05 + 0.014 * (i % 2)
        k.add(f"hem{i}", blade([b, b + d * ln * 0.5, b + d * ln], [0.028, 0.02, 0.004], (c, s, 0), 0.5), ROBE, "body")
    # the robe opens over the chest in a V, the yellow showing
    def v(P):
        return (inside(np.abs(P[:, 0]) - 0.055 * np.clip((P[:, 2] - 0.29) / 0.1, 0, 1), 0.002) * (P[:, 1] < -0.02)
                * (P[:, 2] > 0.29))
    k.paint(v, YELLOW, on=["robe"])
    # orange heart-shaped marks round the lower robe, two lobes up, a round foot: in the robe's own frame
    MARKS = [(-0.62, 0.13), (0.62, 0.13), (1.75, 0.15), (-1.75, 0.15), (math.pi, 0.12), (0.0, 0.085)]
    def marks(P):
        a = np.arctan2(P[:, 0], -(P[:, 1] - ROBE_C[1]))
        r = np.hypot(P[:, 0], P[:, 1] - ROBE_C[1])
        m = np.zeros(len(P))
        for a0, z0 in MARKS:
            da = (a - a0 + math.pi) % (2 * math.pi) - math.pi
            sx = da * robe_radius(z0)
            t = P[:, 2] - z0
            top = 0.036 - 1.3 * np.abs(np.abs(sx) - 0.014)
            heart = np.maximum(inside(np.hypot(sx, t) - 0.026, 0.002) * (t < 0.004),
                               inside(t - top, 0.002) * inside(np.abs(sx) - 0.027, 0.002) * (t >= 0))
            m = np.maximum(m, heart * (r > robe_radius(z0) * 0.75))
        return m
    k.paint(marks, MARK, on=["robe", "hem"])


def add_chest(k):
    k.add("chest", ell((0, -0.012, 0.41), (0.056, 0.046, 0.06)), YELLOW, "body")
    # the long white ruff down the front, from under the jaw to a point over the robe
    k.add("ruff", blade([(0, -0.052, 0.43), (0, -0.064, 0.37), (0, -0.074, 0.31), (0, -0.084, 0.25)],
                        [0.036, 0.033, 0.024, 0.004], (0, 1, 0), 0.45), WHITE, "body")
    for s in (1, -1):
        b = np.array([0.028 * s, -0.06, 0.3])
        k.add(f"ruffpt{'R' if s > 0 else 'L'}", blade([b, b + (0.008 * s, -0.004, -0.03), b + (0.012 * s, -0.008, -0.05)],
                                                       [0.016, 0.011, 0.003], (0, 1, 0), 0.5), WHITE, "body")


def sleeve(k, side, SH, EL, WR):
    m = np.array([side, 1, 1.0])
    bone = "armR" if side > 0 else "armL"
    tag = bone[-1]
    SH, EL, WR = SH * m, EL * m, WR * m
    k.add("upper" + tag, tube([SH, EL], [0.032, 0.032]), ROBE, bone)
    # the sleeve flaring from the elbow to a wide open cuff
    d = unit(*(WR - EL))
    k.add("fore" + tag, tube([EL, (EL + WR) / 2, WR - d * 0.012], [0.032, 0.042, 0.054]), ROBE, bone)
    # the cuff's rim hanging in ragged points below and behind the wrist
    a1, a2 = axes_of(d)
    for j, ang in enumerate((-2.2, -1.57, -0.95, 0.0)):
        rim = WR - d * 0.01 + 0.048 * (math.cos(ang) * a1 + math.sin(ang) * a2)
        down = unit(*(0.35 * d + np.array([0, 0.15, -1.0])))
        ln = 0.05 if j in (1, 2) else 0.035
        k.add(f"cuff{j}" + tag, blade([rim, rim + down * ln * 0.5, rim + down * ln], [0.022, 0.015, 0.003],
                                      unit(*np.cross(d, down)), 0.5), ROBE, bone)
    hand = WR + d * 0.012
    k.add("hand" + tag, ell(hand, (0.017, 0.019, 0.018)), HAND, bone)
    return hand, bone


def axes_of(d):
    """Two directions square to d: the first level (across), the second as near up as can be."""
    a = np.cross(d, (0, 0, 1.0)); a /= np.linalg.norm(a)
    b = np.cross(a, d); b /= np.linalg.norm(b)
    return a, b


def add_arms(k):
    # the right arm forward at the chest with the twig; the left bent with its hand on the chest
    hand, bone = sleeve(k, 1, np.array([0.06, -0.01, 0.39]), np.array([0.105, -0.065, 0.335]),
                        np.array([0.082, -0.14, 0.33]))
    # the twig: from under the fist up and forward to its burning tip
    lo = hand + np.array([0.0, 0.03, -0.05])
    tip = hand + np.array([0.125, -0.005, 0.135])
    k.add("twig", tube([lo, (lo + tip) / 2 + (0.004, 0, 0), tip], [0.0115, 0.0108, 0.0102], seg=12), WOOD, bone,
          fuse=False, surface="leather")
    k.add("twig2", tube([lo + (tip - lo) * 0.62, lo + (tip - lo) * 0.62 + np.array([0.035, -0.004, 0.035])],
                        [0.0102, 0.01], seg=12), WOOD, bone, fuse=False, surface="leather")
    up = unit(0.25, -0.15, 1)
    fl = [tip - up * 0.01, tip + up * 0.03, tip + up * 0.06 + (0.008, 0, 0), tip + up * 0.085 + (-0.006, 0, 0)]
    k.add("flame", blade(fl, [0.018, 0.024, 0.014, 0.003], (0, 1, 0), 0.6), FLAME, bone, fuse=False, glow=FLAME)
    fi = [tip + up * 0.004 + (0, -0.002, 0), tip + up * 0.026 + (0, -0.002, 0), tip + up * 0.05 + (0, -0.002, 0)]
    k.add("flamein", blade(fi, [0.011, 0.013, 0.003], (0, 1, 0), 0.75), FLAME_IN, bone, fuse=False, glow=FLAME_IN)
    sleeve(k, -1, np.array([0.06, -0.01, 0.39]), np.array([0.105, -0.055, 0.32]), np.array([0.042, -0.112, 0.33]))


def add_feet(k):
    AN, TO = np.array([0.064, 0.0, 0.06]), np.array([0.07, -0.13, 0.014])
    k.add("shin", tube([np.array([0.052, 0.01, 0.2]), AN], [0.03, 0.022]), BLACK, "leg{s}", mirror=True)
    k.add("foot", tube([AN, TO], [0.02, 0.018]), BLACK, "leg{s}", mirror=True)
    k.add("toes", ell(TO + (0, -0.008, -0.002), (0.024, 0.028, 0.014)), BLACK, "leg{s}", mirror=True)
    for j, dx in enumerate((-0.014, 0.0, 0.014)):
        a = TO + np.array([dx, -0.03, -0.004])
        tip = a + np.array([dx * 0.4, -0.018, -0.006])
        k.add(f"claw{j}", hull([a + (0.006, 0.006, 0.006), a + (-0.006, 0.006, 0.006), a + (0, 0.006, -0.006),
                                a + (0, -0.004, 0.006), tip]), CLAW, "leg{s}", fuse=False, mirror=True, surface="shell")


OUT = unit(0.57, 0.82, 0)
ROOT = np.array([0, 0.11, 0.2])
TAIL = [ROOT, ROOT + OUT * 0.07 + (0, 0, -0.02), ROOT + OUT * 0.15 + (0, 0, -0.005), ROOT + OUT * 0.21 + (0, 0, 0.04),
        ROOT + OUT * 0.235 + (0, 0, 0.11)]
TAIL_R = [0.03, 0.046, 0.05, 0.04, 0.005]


def add_plumes(k):
    # Braixen's plumes, grown: longer and fuller, and a fourth lobe up, so they stream out like flame
    plumes = b_.PLUMES + [([(0.03, 0.004, 0.08), (0.06, 0.02, 0.13), (0.075, 0.032, 0.17)], [0.022, 0.018, 0.003])]
    b_.add_ears(k, h=H, plumes=plumes, scale=1.18)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    add_feet(k)
    add_robe(k)
    add_chest(k)
    add_arms(k)
    b_.add_head(k, h=H, hr=HR, snout=SNOUT, neck=[H + (0, 0.025, -0.1), H + (0, 0.012, -0.055)])
    add_plumes(k)
    f.add_cheeks(k, h=H, hr=HR, out=0.045, back=0.06, up=-0.012, r=0.024, at=(0.8, 0.1, -0.4))
    f.add_tail(k, tail=TAIL, radii=TAIL_R, color=YELLOW, tip=PALE, start=0.78)
    b_.add_face(k, h=H, snout=SNOUT)
    k.build(voxel=0.0058, band=0.004, wband=0.035)
    k.eye_states(lambda s: b_.lay_eyes(k, s, h=H, hr=HR, half=(0.026, 0.026)))
    k.export(out)
