# Mega Delphox sculpted on the skeleton and clips of the game's own export of the form (sculptkit.py), grown from
# Delphox's script (delphox.py, imported: its sleeves with their ragged cuffs, the robe's way of being made, and
# through it Braixen's head, white face, ears, plumes, eyes, nose and mouth and Fennekin's cheek tufts):
#   blender --background --factory-startup --python sculpts/delphoxmega.py -- <the export's Delphox-Mega.glb> <out.glb>
# The form hovers: its joints stand 0.076 over Delphox's (the export is lifted so), and everything here is Delphox's
# frame raised by LIFT.
# From the two pictures, the masses by size:
#   1. the robe, wider than Delphox's, flaring into a great ragged skirt of dark purple tatters; red-orange from the
#      neck to the waist, dark purple below;
#   2. the mane of the ears' plumes, grown into a great sweep of red-orange fur back and out from the head, its ends
#      dark purple;
#   3. the branch it rides like a witch's broom, under the skirt, its front end raised, its back end down and out to
#      +X in a great ball of flame;
#   4. two floating sticks, each burning at its lower end, beside the hands;
#   5. the wide purple sleeves with ragged cuffs, black hands with grey claws; the long white ruff down the chest.
# (The pictures overrule the game's sculptor: the sticks float beside the hands rather than being held, and it rides a
# burning branch. They float close to the hands here, not out where the drawing throws them, which would make the model
# twice as wide and its body small in the game. No tail shows under the robe in the pictures; none is built.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, inside, smoothstep, unit, args, sibling

d_ = sibling("delphox")
b_ = d_.b_
f = d_.f
YELLOW, FUR, WHITE, WOOD = b_.YELLOW, b_.FUR, b_.WHITE, b_.WOOD
ROBE = "#3C2846"            # the dark purple
TOP = "#CC3E22"             # the red-orange of the chest and shoulders
HAND = d_.HAND
CLAW = d_.CLAW
FLAME, FLAME_IN = d_.FLAME, d_.FLAME_IN
PLUME = "#E4502A"
BRANCH = "#6E4A36"

d_.ROBE = ROBE              # the sleeves read it as they are made

LIFT = 0.076
H = d_.H + np.array([0, 0, LIFT])
HR = d_.HR * 1.06
SNOUT = d_.SNOUT
ROBE_Y = 0.012
HEM_Z = 0.115
PROFILE = [(0.47, 0.06), (0.41, 0.084), (0.34, 0.118), (0.25, 0.165), (0.165, 0.2)]
DEPTH = 0.9


def robe_radius(z):
    zz = [p[0] for p in PROFILE][::-1]; rr = [p[1] for p in PROFILE][::-1]
    return np.interp(z, zz, rr)


def add_robe(k):
    V, F = tube([(0, ROBE_Y, z) for z, _ in PROFILE], [r for _, r in PROFILE], seg=40)
    V = V - np.array([0, ROBE_Y, 0]); V[:, 1] *= DEPTH; V = V + np.array([0, ROBE_Y, 0])
    V[:, 2] = np.clip(V[:, 2], HEM_Z, 0.5)
    k.add("robe", (V, F), ROBE, "body")
    # the skirt's tatters: many, long and uneven, hanging down and out
    rs = np.random.RandomState(5)
    for i, a in enumerate(np.linspace(0, 2 * math.pi, 22, endpoint=False)):
        c, s = math.sin(a), -math.cos(a)
        b = np.array([0.19 * c, ROBE_Y + 0.19 * DEPTH * s, HEM_Z + 0.035])
        dd = unit(0.38 * c, 0.38 * s, -1)
        ln = 0.06 + 0.035 * rs.rand() + 0.02 * (i % 2)
        k.add(f"hem{i}", blade([b, b + dd * ln * 0.5, b + dd * ln], [0.032, 0.022, 0.004], (c, s, 0), 0.5), ROBE,
              "body")
    # red-orange above the waist, the purple rising a little higher at the back
    k.paint(lambda P: smoothstep(0.31, 0.35, P[:, 2] - 0.03 * np.clip(P[:, 1] / 0.1, 0, 1)), TOP,
            on=["robe", "chest"])
    # the robe opens over the chest in a V, the yellow showing
    def v(P):
        return (inside(np.abs(P[:, 0]) - 0.05 * np.clip((P[:, 2] - 0.38) / 0.1, 0, 1), 0.002) * (P[:, 1] < -0.02)
                * (P[:, 2] > 0.38))
    k.paint(v, YELLOW, on=["robe", "chest"])


def add_chest(k):
    z = LIFT
    k.add("chest", ell((0, -0.012, 0.41 + z), (0.058, 0.048, 0.06)), YELLOW, "body")
    # the long white ruff down the front, from under the jaw to a point at the waist
    k.add("ruff", blade([(0, -0.054, 0.435 + z), (0, -0.068, 0.37 + z), (0, -0.08, 0.3 + z), (0, -0.092, 0.22 + z)],
                        [0.04, 0.036, 0.026, 0.004], (0, 1, 0), 0.45), WHITE, "body")
    for s in (1, -1):
        b = np.array([0.03 * s, -0.064, 0.32 + z])
        k.add(f"ruffpt{'R' if s > 0 else 'L'}",
              blade([b, b + (0.01 * s, -0.004, -0.035), b + (0.014 * s, -0.008, -0.06)], [0.018, 0.012, 0.003],
                    (0, 1, 0), 0.5), WHITE, "body")


def stick(k, name, flame_at, d, bone):
    """A floating stick: its shaft from the flame up and out along d, a side twig, the flame at its lower end."""
    d = unit(*d)
    top = flame_at + d * 0.17
    k.add(name, tube([flame_at + d * 0.01, (flame_at + top) / 2 + np.array([0, 0, 0.006]), top], [0.012, 0.012, 0.011],
                     seg=12), WOOD, bone, fuse=False, surface="leather")
    tw = flame_at + d * 0.11
    k.add(name + "twig", tube([tw, tw + unit(*(d + np.array([0, 0, 0.9]))) * 0.04], [0.01, 0.009], seg=10), WOOD, bone,
          fuse=False, surface="leather")
    # the flame: a ball of fire at the end, its tongues licking back along the shaft's other way and up
    back = -d
    k.add(name + "fire", ell(flame_at - d * 0.012, (0.032, 0.032, 0.03)), FLAME, bone, fuse=False, glow=FLAME)
    fl = [flame_at - d * 0.02, flame_at + back * 0.045 + np.array([0, 0, 0.02]),
          flame_at + back * 0.075 + np.array([0, 0, 0.012])]
    k.add(name + "tongue", blade(fl, [0.026, 0.018, 0.004], (0, 1, 0), 0.55), FLAME, bone, fuse=False, glow=FLAME)
    k.add(name + "core", ell(flame_at - d * 0.016 + np.array([0, -0.016, 0]), (0.019, 0.019, 0.018)), FLAME_IN, bone,
          fuse=False, glow=FLAME_IN)


def add_arms(k):
    sh = np.array([0.06, -0.01, 0.466])
    hand, bone = d_.sleeve(k, 1, sh, np.array([0.115, -0.06, 0.415]), np.array([0.11, -0.145, 0.425]))
    handl, bonel = d_.sleeve(k, -1, sh, np.array([0.115, -0.06, 0.405]), np.array([0.105, -0.14, 0.39]))
    # grey claws on the black hands
    for h_, bn, s in ((hand, bone, 1), (handl, bonel, -1)):
        for j, dx in enumerate((-0.012, 0.0, 0.012)):
            a = h_ + np.array([dx * s, -0.016, 0.004 - abs(dx) * 0.3])
            tip = a + np.array([dx * 0.5 * s, -0.016, -0.012])
            k.add(f"hclaw{j}{bn[-1]}", hull([a + (0.005, 0.004, 0.005), a + (-0.005, 0.004, 0.005), a + (0, 0.004, -0.005),
                                              a + (0, -0.004, 0.004), tip]), CLAW, bn, fuse=False, surface="shell")
    # the floating sticks, burning at their lower ends just beyond each hand
    stick(k, "stickR", hand + np.array([0.045, -0.03, 0.05]), (0.55, 0.15, 0.82), bone)
    stick(k, "stickL", handl + np.array([-0.045, -0.03, 0.035]), (-0.6, 0.2, 0.78), bonel)


def add_feet(k):
    # seated on the branch: the shins hang forward under the skirt, the black feet with grey claws peeping out in front
    HIP, KN, AN = np.array([0.04, 0.0, 0.27]), np.array([0.06, -0.1, 0.24]), np.array([0.064, -0.13, 0.16])
    k.add("thigh", tube([HIP, KN], [0.036, 0.03]), d_.BLACK, "leg{s}", mirror=True)
    k.add("shin", tube([KN, AN], [0.028, 0.022]), d_.BLACK, "leg{s}", mirror=True)
    TO = AN + np.array([0.004, -0.04, -0.015])
    k.add("toes", ell(TO, (0.024, 0.03, 0.015)), d_.BLACK, "leg{s}", mirror=True)
    for j, dx in enumerate((-0.014, 0.0, 0.014)):
        a = TO + np.array([dx, -0.026, -0.002])
        tip = a + np.array([dx * 0.4, -0.018, -0.008])
        k.add(f"claw{j}", hull([a + (0.006, 0.006, 0.006), a + (-0.006, 0.006, 0.006), a + (0, 0.006, -0.006),
                                a + (0, -0.004, 0.006), tip]), CLAW, "leg{s}", fuse=False, mirror=True, surface="shell")


def add_branch(k):
    # under the skirt from front to back, the front end raised; the back end down and out to +X, burning
    FR, BK = np.array([-0.06, -0.33, 0.3]), np.array([0.13, 0.25, 0.13])
    mid = (FR + BK) / 2 + np.array([0, 0, -0.01])
    k.add("branch", tube([FR, mid, BK], [0.019, 0.02, 0.018], seg=14), BRANCH, "body", fuse=False, surface="leather")
    for i, (t, dd, ln) in enumerate(((0.12, (-0.5, -0.6, 0.6), 0.07), (0.82, (0.6, 0.3, -0.5), 0.06),
                                     (0.62, (-0.6, 0.4, -0.6), 0.06))):
        p = FR + (BK - FR) * t
        k.add(f"twig{i}", tube([p, p + unit(*dd) * ln], [0.012, 0.009], seg=10), BRANCH, "body", fuse=False,
              surface="leather")
    # the great ball of flame at its back end, trailing back and up
    back = unit(*(BK - FR))
    c = BK + back * 0.05
    k.add("fireball", ell(c, (0.09, 0.095, 0.08)), FLAME, "body", fuse=False, glow=FLAME)
    for i, (dd, ln, r) in enumerate((((0.3, 1, 0.5), 0.12, 0.045), ((0.8, 0.6, 0.35), 0.1, 0.04),
                                     ((-0.3, 1, 0.2), 0.1, 0.038))):
        q = c + unit(*dd) * 0.04
        e = unit(*dd)
        k.add(f"firetongue{i}", blade([q, q + e * ln * 0.5 + np.array([0, 0, 0.015]), q + e * ln],
                                      [r, r * 0.7, 0.005], (0, 0, 1), 0.6), FLAME, "body", fuse=False, glow=FLAME)
    k.add("firecore", ell(c + np.array([0.012, -0.04, 0.0]), (0.06, 0.058, 0.052)), FLAME_IN, "body", fuse=False,
          glow=FLAME_IN)


def add_mane(k):
    # Braixen's plumes grown into a great sweep back and out, a mass of the same fur behind the head
    # (the yellow ears stand up clear of it, as in both pictures: the fur sweeps out, back and down from their roots)
    s = 1.4
    plumes = [([(0.035, 0.0, 0.06), (0.09, 0.02, 0.05), (0.14, 0.05, 0.02), (0.17, 0.075, -0.03), (0.18, 0.09, -0.07)],
               [0.03, 0.04, 0.036, 0.026, 0.005]),
              ([(0.045, 0.02, 0.04), (0.07, 0.05, 0.015), (0.08, 0.08, -0.03), (0.085, 0.1, -0.08)],
               [0.022, 0.032, 0.026, 0.004])]
    b_.add_ears(k, h=H, plumes=plumes, scale=s, fur=PLUME)
    k.add("backhair", ell(H + np.array([0, 0.07, 0.0]), (0.085, 0.062, 0.075)), PLUME, "head")
    k.add("backhair2", ell(H + np.array([0, 0.1, -0.065]), (0.08, 0.055, 0.07)), PLUME, "head")
    # the ends dark purple
    k.paint(lambda P: smoothstep(0.16, 0.2, np.hypot(P[:, 0], P[:, 1] - H[1] - 0.02)) * (P[:, 2] > H[2] - 0.2),
            ROBE, on=["plume", "backhair"])


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    add_feet(k)
    add_robe(k)
    add_chest(k)
    add_arms(k)
    add_branch(k)
    b_.add_head(k, h=H, hr=HR, snout=SNOUT, neck=[H + (0, 0.025, -0.1), H + (0, 0.012, -0.055)])
    add_mane(k)
    f.add_cheeks(k, h=H, hr=HR, out=0.047, back=0.06, up=-0.012, r=0.025, at=(0.8, 0.1, -0.4))
    b_.add_face(k, h=H, snout=SNOUT)
    k.build(voxel=0.0058, band=0.004, wband=0.035)
    k.eye_states(lambda s: b_.lay_eyes(k, s, h=H, hr=HR, half=(0.028, 0.028)))
    k.export(out)
