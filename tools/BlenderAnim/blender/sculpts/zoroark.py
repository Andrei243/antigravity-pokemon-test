# Zoroark sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Zorua's script (zorua.py, imported: its colours, ears and lining, eyes under a red mark, nose):
#   blender --background --factory-startup --python sculpts/zoroark.py -- <the export's Zoroark.glb> <out.glb>
# A tall dark grey fox standing on two legs, crouched a little: a long pointed muzzle with a red line of a mouth,
# tall ears lined dark, a cyan eye under a red mark; a great red mane from the crown over the back of the head and the
# shoulders, its points tipped black, falling behind into a thick tail of mane bound by a teal bead, below it a black
# tuft with a red tip; a black ruff on the chest; arms held forward and down, broad forearms, three red claws to each
# hand; great haunches over short thick shins, three red claws to each foot.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, lathe, srgb, inside, smoothstep, unit, tri, polygon, args, sibling

z_ = sibling("zorua")

GREY, RUFF, RED = "#4A4858", z_.RUFF, z_.RED
MANE = "#B52436"
TIP = "#1E1E30"
TEAL = "#2EB4B8"
CLAW = "#D63046"

# --- the frame (the skeleton's joints: body (0, 0, 0.44), arms (+-0.08, -0.02, 0.56), tail (0, 0.06, 0.64), head
#     (0, -0.04, 0.62), ears (+-0.035, -0.05, 0.72), legs (+-0.06, 0.02, 0.36)) ------------------------------------------
# The figure hunches: the trunk leans forward some 20 degrees, the head is carried forward and a little down in front
# of the mane, a sixth larger than first laid out (G grows a place laid out about the first head, H0, onto it)
H0 = np.array([0, -0.07, 0.668])
G = 1.15
H = np.array([0, -0.125, 0.638])
HR = np.array([0.064, 0.068, 0.062]) * G
OUT = unit(0.57, 0.82, 0)                    # the tail of mane swings back and out to +X


def g(p):
    """A place laid out on the first head, on the grown head."""
    return H + G * (np.array(p, float) - H0)


def taper(V):
    V = V.copy()
    t = np.clip(-V[:, 1], 0, 1)
    V[:, 0] *= 1 - 0.5 * t
    V[:, 2] *= 1 - 0.35 * t
    return V


def add_head(k):
    k.add("head", ell(H, HR, deform=z_.cheeky), GREY, "head")
    k.add("muzzle", ell(H + G * np.array([0, -0.08, -0.026]), np.array([0.036, 0.07, 0.032]) * G, rot=(-14, 0, 0),
                        deform=taper), GREY, "head")
    k.add("neck", tube([(0, -0.05, 0.56), (0, -0.08, 0.59), (0, -0.11, 0.615)], [0.056, 0.052, 0.05]), GREY, "head")
    # a point of fur back from each cheek
    for s in (1, -1):
        b = H + G * np.array([0.05 * s, 0.01, -0.03])
        k.add(f"cheek{'R' if s > 0 else 'L'}", blade([b, b + (0.02 * s, 0.025, -0.007), b + (0.034 * s, 0.05, -0.018)],
                                                     [0.02, 0.014, 0.002], (0, 0, 1), 0.5), GREY, "head")


def add_body(k):
    # The trunk leaning forward some 20 degrees over the hips
    k.add("chest", ell((0, -0.05, 0.52), (0.082, 0.07, 0.08), rot=(-20, 0, 0)), GREY, "body")
    k.add("waist", ell((0, -0.022, 0.445), (0.062, 0.056, 0.07), rot=(-20, 0, 0)), GREY, "body")
    k.add("hips", ell((0, 0.01, 0.39), (0.068, 0.06, 0.058)), GREY, "body")
    # the black ruff over the chest, from under the jaw to a point in the middle of the chest
    k.add("ruff", ell((0, -0.09, 0.56), (0.074, 0.048, 0.062), rot=(-20, 0, 0)), RUFF, "body")
    k.add("ruffpt", blade([(0, -0.105, 0.53), (0, -0.1, 0.49), (0, -0.085, 0.455)], [0.04, 0.025, 0.004], (0, 1, 0), 0.45),
          RUFF, "body")


def add_arms(k):
    # Brought forward and down, bent at the elbow, the claws in front of the thighs; the forearm broad
    SH, EL, WR = np.array([0.088, -0.05, 0.545]), np.array([0.135, -0.1, 0.455]), np.array([0.11, -0.18, 0.37])
    k.add("upper", tube([SH, EL], [0.042, 0.038]), GREY, "arm{s}", mirror=True)
    k.add("fore", tube([EL, (EL + WR) / 2 + (0.01, 0, 0), WR], [0.042, 0.05, 0.04]), GREY, "arm{s}", mirror=True)
    k.add("shtuft", blade([SH + (0.02, 0.0, 0.01), SH + (0.05, 0.02, 0.02), SH + (0.075, 0.035, 0.02)],
                          [0.024, 0.015, 0.002], (0, 0, 1), 0.5), GREY, "arm{s}", mirror=True)
    k.add("eltuft", blade([EL + (0.02, 0.01, 0), EL + (0.04, 0.03, -0.004), EL + (0.055, 0.05, -0.01)],
                          [0.02, 0.013, 0.002], (0, 0, 1), 0.5), GREY, "arm{s}", mirror=True)
    HAND = WR + np.array([-0.004, -0.018, -0.024])
    k.add("hand", ell(HAND, (0.038, 0.04, 0.032)), GREY, "arm{s}", mirror=True)
    for j, dx in enumerate((-0.02, 0.0, 0.02)):
        a = HAND + np.array([dx, -0.026, -0.014])
        tip = a + np.array([dx * 0.4, -0.03, -0.048])
        mid = a + np.array([dx * 0.2, -0.026, -0.014])
        k.add(f"claw{j}", hull([a + (0.007, 0.006, 0.004), a + (-0.007, 0.006, 0.004), a + (0, -0.006, 0.008),
                                a + (0, 0.004, -0.008), mid + (0.004, 0, 0), mid + (-0.004, 0, 0), tip]),
              CLAW, "arm{s}", fuse=False, mirror=True, surface="shell")


def add_legs(k):
    # Great haunches fused into the hips and reaching down near the ankle, a short thick shin, a long foot
    k.add("thigh", ell((0.072, -0.02, 0.31), (0.054, 0.072, 0.092), rot=(18, 0, 0)), GREY, "leg{s}", mirror=True)
    KN, AN, TO = np.array([0.082, -0.075, 0.215]), np.array([0.088, 0.0, 0.1]), np.array([0.092, -0.07, 0.022])
    k.add("shin", tube([KN, AN], [0.042, 0.032]), GREY, "leg{s}", mirror=True)
    k.add("foot", tube([AN, (AN + TO) / 2 + (0, 0, -0.01), TO], [0.03, 0.028, 0.026]), GREY, "leg{s}", mirror=True)
    k.add("toes", ell(TO + (0, -0.01, -0.004), (0.036, 0.04, 0.02)), GREY, "leg{s}", mirror=True)
    for j, dx in enumerate((-0.02, 0.0, 0.02)):
        a = TO + np.array([dx, -0.045, -0.004])
        tip = a + np.array([dx * 0.3, -0.03, -0.016])
        k.add(f"tclaw{j}", hull([a + (0.007, 0.008, 0.006), a + (-0.007, 0.008, 0.006), a + (0, 0.006, -0.008),
                                 a + (0, -0.004, 0.006), tip]), CLAW, "leg{s}", fuse=False, mirror=True, surface="shell")


# --- the mane: a great red mass from the crown over the back of the head and the shoulders, its points tipped black;
#     a tail of it behind, bound by a teal bead, a black tuft with a red tip below -------------------------------------
MC = np.array([0, 0.06, 0.64])
# (root on the mane's surface, tip, thickness): the points out over the top, the sides and the back, mirrored off the midline
SPIKES = [((0, 0.02, 0.8), (0, 0.1, 0.89), 0.05), ((0.1, 0.04, 0.77), (0.17, 0.08, 0.85), 0.046),
          ((0.17, 0.08, 0.67), (0.26, 0.12, 0.71), 0.046), ((0.18, 0.13, 0.54), (0.27, 0.19, 0.51), 0.046),
          ((0.14, 0.18, 0.41), (0.22, 0.25, 0.35), 0.04), ((0.06, 0.2, 0.75), (0.09, 0.3, 0.8), 0.046),
          ((0, 0.25, 0.6), (0, 0.36, 0.62), 0.046), ((0.08, 0.26, 0.47), (0.12, 0.36, 0.43), 0.04)]
# drawn out from their roots past the grown mane's surface (the top one less, for the height), and thicker
SPIKES = [(b, tuple(np.array(b) + (np.array(t) - np.array(b)) * (1.3 if i == 0 else 1.8)), r * 1.15)
          for i, (b, t, r) in enumerate(SPIKES)]


def add_mane(k):
    # The largest mass of the design: from over the brow, over the shoulders wider than them, and down the back to the
    # knees
    k.add("manecrown", ell(H + (0, 0.075, 0.075), (0.082, 0.08, 0.072)), MANE, "head")
    k.add("mane", ell(MC + (0, 0.02, 0.04), (0.19, 0.14, 0.15)), MANE, "tail")
    k.add("maneback", ell(MC + (0, 0.1, -0.15), (0.165, 0.12, 0.15)), MANE, "tail")
    k.add("manelow", ell(MC + (0, 0.14, -0.31), (0.12, 0.09, 0.12)), MANE, "tail")
    # one swept mass: a thick tube down through the three, so they read as one fall of mane and not as balls
    k.add("manefall", tube([MC + (0, 0.0, 0.07), MC + (0, 0.07, -0.06), MC + (0, 0.12, -0.2), MC + (0, 0.15, -0.31)],
                           [0.165, 0.16, 0.14, 0.105]), MANE, "tail")
    for i, (b, t, r) in enumerate(SPIKES):
        b, t = np.array(b), np.array(t)
        d = unit(*(t - b))
        ax = np.cross(d, (0, 0, 1.0)) if abs(d[2]) < 0.9 else np.array([1.0, 0, 0])
        k.add(f"spike{i}", blade([b, b + (t - b) * 0.5, t], [r, r * 0.6, 0.003], ax, 0.5), MANE, "tail",
              mirror=b[0] != 0)
    # the black tips of the points
    def tips(P):
        q = P.copy(); q[:, 0] = np.abs(q[:, 0])
        m = np.zeros(len(P))
        for b, t, r in SPIKES:
            b, t = np.array(b), np.array(t)
            d = t - b
            s = ((q - b) @ d) / (d @ d)
            m = np.maximum(m, inside(0.55 - s, 0.02))
        return m
    k.paint(tips, TIP, on=["spike"])
    # the tail of mane: from behind the shoulders back and down to the bead
    root = MC + np.array([0, 0.18, -0.3])
    bead = root + OUT * 0.1 + np.array([0, 0, -0.12])
    k.add("ponytail", tube([root, root + OUT * 0.06 + (0, 0, -0.05), bead], [0.08, 0.066, 0.044]), MANE, "tail")
    d = unit(*(bead - (root + OUT * 0.07 + np.array([0, 0, -0.05]))))
    k.add("bead", lathe(bead, d, [(0, -0.026), (0.03, -0.024), (0.05, -0.012), (0.052, 0.0), (0.05, 0.012),
                                  (0.03, 0.024), (0, 0.026)]), TEAL, "tail", fuse=False, surface="shell")
    # the tuft below the bead: black, swept back and up to a red tip, flat in the tail's own plane
    a = bead + d * 0.02
    pts = [a, a + OUT * 0.05 + (0, 0, -0.05), a + OUT * 0.13 + (0, 0, -0.06), a + OUT * 0.2 + (0, 0, -0.02)]
    side = np.cross(OUT, (0, 0, 1.0))
    k.add("tuft", blade(pts, [0.044, 0.05, 0.034, 0.004], side, 0.55), TIP, "tail")
    k.paint(lambda P: inside(0.13 - ((P - a) @ OUT), 0.006) * ((P - a) @ OUT > 0), RED, on=["tuft"])


def add_face(k):
    tip = g(H0 + (0, -0.15, -0.032))
    k.decal(tip, unit(0, -1, 0.2), (0.014, 0.0105), z_.NOSE, eps=0.003,
            outline=polygon([(-1, 0.6), (1, 0.6), (0.3, -0.9), (-0.3, -0.9)]), rings=6, segs=28)
    # a red line of a mouth along each side of the muzzle, curling up at its end
    CG, CR = srgb(GREY), srgb(RED)
    def line(u, v):
        c = np.tile(CG, (len(u), 1))
        c[(np.abs(v - (-0.25 + 0.5 * np.clip(u - 0.3, 0, 1) ** 2 * 3)) < 0.22) & (np.abs(u) < 0.95)] = CR
        return c
    for s in (1, -1):
        at = g(H0 + (0.026 * s, -0.1, -0.05))
        k.decal(at, unit(s * 0.9, -0.3, -0.3), (0.039, 0.014), line if s > 0 else (lambda u, v: line(-u, v)),
                up=unit(0, -0.3, 1), eps=0.003, rings=8, segs=40)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    add_body(k)
    add_arms(k)
    add_legs(k)
    add_head(k)
    z_.add_ears(k, ear=[g(p) for p in ((0.034, -0.068, 0.712), (0.044, -0.06, 0.75), (0.053, -0.05, 0.788), (0.06, -0.04, 0.822))],
                radii=[0.034, 0.03, 0.016, 0.003], color=GREY, lining=RUFF, width=0.02)
    add_mane(k)
    add_face(k)
    k.build(voxel=0.0068, band=0.004)
    k.eye_states(lambda s: z_.lay_eyes(k, s, H, HR, x=0.034, dz=0.007, half=(0.023, 0.016), lid=GREY, iris="#2AA8D8",
                                       turn=1.35))
    k.export(out)
