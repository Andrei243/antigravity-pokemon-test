# Scream Tail sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Jigglypuff's script (jigglypuff.py, imported: its ball, ears, arms and feet), as Wigglytuff is:
#   blender --background --factory-startup --python sculpts/screamtail.py -- <the export's Scream Tail.glb> <out.glb>
#
# Jigglypuff's ancient past, from the references: a pale pink ball that is all of it; short pointed ears lined black;
# a great lock of hair rising from the brow in a rounded pompadour over the crown and falling behind into one long
# thick tail of hair that hangs to the ground and curls forward at its end, pale pink above turning coral below, a few
# hooked tufts down its outer edge; very large round yellow eyes, a thin dark rim and a white ring, a big glint and a
# small one, a pink flick at each eye's outer top corner; pink blush under the eyes; a small wavy mouth with two little
# white fangs; little pointed arms carried forward and down; oval feet with three toes. (The pictures' two fine wisps
# over the ears are left out: thinner than the game can draw.) The tail of hair is turned some 45 degrees out to the
# model's +X as it falls, so that the front three-quarter camera sees it beside the ball.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, polygon, srgb, inside, smoothstep, unit, eye_look, args, sibling

j = sibling("jigglypuff")

PINK = "#F7D0DC"
LINING = "#2E2A34"
CORAL, CORAL_DEEP = "#EC7A8E", "#D9506A"
INK, WHITE, GOLD, GOLD_DEEP = "#2A2030", "#FFFFFF", "#F0C426", "#C89416"
BLUSH, LINER = "#F2869E", "#E8607E"
MOUTH = "#5E2A3C"

# the frame (the skeleton's joints: body (0, 0, 0.2), ears (+-0.09, 0, 0.32), arms (+-0.15, -0.04, 0.2), mane
# (0, -0.02, 0.35), tail (0, 0.14, 0.1), legs (+-0.075, -0.02, 0.06))
j.C = np.array([0, 0, 0.205])
j.R = np.array([0.185, 0.175, 0.18])
C, R = j.C, j.R
EYE_X, EYE_Z = 0.088, -0.004
EYE_HALF = (0.057, 0.059)
MOUTH_Z = -0.07

# the lock: from the brow over the crown (mane), then falling behind and out to +X and curling forward (tail)
LOCK = [(0.0, -0.07, 0.395), (0.0, -0.035, 0.43), (0.0, 0.03, 0.44), (0.03, 0.12, 0.42), (0.08, 0.195, 0.365)]
LOCK_R = [0.07, 0.08, 0.08, 0.072, 0.06]
TRAIL = [(0.03, 0.12, 0.42), (0.08, 0.195, 0.365), (0.155, 0.235, 0.275), (0.215, 0.245, 0.175), (0.245, 0.235, 0.09),
         (0.258, 0.19, 0.045), (0.258, 0.135, 0.062), (0.254, 0.125, 0.108), (0.258, 0.16, 0.125)]
TRAIL_R = [0.072, 0.06, 0.052, 0.046, 0.041, 0.036, 0.031, 0.027, 0.02]


def tuft(k, name, at, d, length, base, col, bone):
    """A hooked tuft of hair: a flat-faced spike from a base on the tail of hair."""
    d = unit(*d)
    side = np.cross(d, (0, 0, 1.0)); side /= np.linalg.norm(side)
    up = np.cross(side, d)
    a = np.array(at, float)
    tip = a + d * length + up * length * 0.35
    pts = [a + side * base * 0.6, a - side * base * 0.6, a + up * base, a - up * base, a - d * base * 0.5, tip,
           a + d * length * 0.55 + up * length * 0.05]
    k.add(name, hull(pts), col, bone, fuse=False, surface="fur")


def screamtail(src, out_path):
    k = Kit(src, coat="fur")
    j.add_body(k, color=PINK)
    j.add_ears(k, base=np.array([0.1, 0.0, 0.325]), tip=np.array([0.175, 0.012, 0.43]),
               radii=[0.062, 0.048, 0.024, 0.005], lining=LINING, inset=0.62, color=PINK)
    # the lock of hair, fused into one mass with the ball
    k.add("lock", tube(LOCK, LOCK_R, seg=20), PINK, "mane")
    k.add("pomp", ell((0.0, -0.035, 0.42), (0.1, 0.098, 0.062)), PINK, "mane")
    k.add("trail", tube(TRAIL, TRAIL_R, seg=20), PINK, "tail")
    # coral below, deeper at the curl
    k.paint(lambda P: smoothstep(0.31, 0.2, P[:, 2]) , CORAL, on=["trail"])
    k.paint(lambda P: smoothstep(0.14, 0.06, P[:, 2]), CORAL_DEEP, on=["trail"])
    # hooked tufts down the tail's outer edge
    TP = np.array(TRAIL)
    for i, z in enumerate((0.27, 0.205, 0.145)):
        y = np.interp(-z, -TP[1:5, 2], TP[1:5, 1])
        x = np.interp(-z, -TP[1:5, 2], TP[1:5, 0])
        r = np.interp(-z, -TP[1:5, 2], TRAIL_R[1:5])
        out = unit(0.75, 0.66, 0.0)
        tuft(k, f"tuft{i}", np.array([x, y, z]) + out * r * 0.7, out + np.array([0, 0, 0.25]), 0.055, 0.02,
             CORAL if i else CORAL, "tail")
    j.add_arms(k, root=(0.155, -0.05, 0.2), tip=(0.205, -0.15, 0.16), radii=(0.046, 0.036, 0.022), color=PINK)
    j.add_feet(k, at=(0.08, -0.06, 0.025), r=(0.05, 0.068, 0.028), turn=18, color=PINK,
               leg=((0.075, -0.02, 0.07), (0.08, -0.04, 0.04)), leg_r=(0.048, 0.043))
    for dx in (-0.024, 0.0, 0.024):
        k.add(f"toe{dx:+.3f}", ell((0.08 + dx * 1.1 + 0.022 * 0.3, -0.12 - 0.003 * abs(dx) / 0.024, 0.018),
                                  (0.016, 0.02, 0.016)), PINK, "leg{s}", mirror=True)
    # blush, the liner's flick, the mouth and its fangs: in every state
    p_e, n_e = j.eye_place(EYE_X, EYE_Z)
    b = j.front(0.112, C[2] - 0.075)
    k.decal(b, j.normal_at(b), (0.028, 0.017), BLUSH, eps=0.003, rings=10, segs=40, mirror=True)
    fl = j.on_ball(p_e + np.array([0.066, 0.0, 0.046]), 0.0)
    k.decal(fl, j.normal_at(fl), (0.025, 0.011), LINER, eps=0.002, rings=8, segs=40, mirror=True,
            up=unit(-0.55, 0, 1))
    CP, CM = srgb(PINK), srgb(MOUTH)
    def mouth(u, v):
        c = np.tile(CP, (len(u), 1))
        line = -0.05 + 0.42 * np.abs(np.sin(math.pi * u))
        c[(np.abs(v - line) < 0.3) & (np.abs(u) < 0.95)] = CM
        return c
    m = j.front(0.0, C[2] + MOUTH_Z)
    k.decal(m, j.normal_at(m), (0.032, 0.014), mouth, eps=0.003, rings=12, segs=56)
    fang = polygon([(-1, 0.9), (1, 0.9), (0, -1)])
    for x in (-0.014, 0.014):
        f_ = j.front(x, C[2] + MOUTH_Z - 0.011)
        k.decal(f_, j.normal_at(f_), (0.0075, 0.0095), WHITE, eps=0.0045, outline=fang, rings=8, segs=40)
    k.build(voxel=0.0045, smooth=5, band=0.006, wband=0.04)
    # the eyes: a thin dark rim, a white ring, a great yellow iris deeper below, a big glint and a small one
    CI, CW, CG, CD = srgb(INK), srgb(WHITE), srgb(GOLD), srgb(GOLD_DEEP)
    def eye(u, v):
        c = np.tile(CI, (len(u), 1))
        r = np.hypot(u, v)
        c[r < 0.86] = CW
        ir = np.hypot(u + 0.02, v + 0.02)
        c[ir < 0.72] = CG
        c[(ir < 0.72) & (v < -0.2 + 0.15 * u * u)] = CD
        c[(ir < 0.72) & (np.hypot(u + 0.02, v + 0.02) > 0.64)] = CD
        c[np.hypot((u - 0.2) / 0.24, (v - 0.24) / 0.24) < 1] = CW
        c[np.hypot((u + 0.28) / 0.11, (v + 0.32) / 0.11) < 1] = CW
        return c
    def lay(state):
        j.lay_eyes(k, state, look=eye, half=EYE_HALF, x=EYE_X, z=EYE_Z, lid=PINK)
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    screamtail(*args())
