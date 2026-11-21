# Bruxish sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/bruxish.py -- <the export's Bruxish.glb> <out.glb>
#
# From the references: a gaudy fish, flat from side to side, its profile a wedge: a tall forehead behind a blunt snout,
# the back sloping down to a short tail stalk, the belly nearly level. Pink above, a yellow band along the lower body,
# a light blue band behind the mouth; big pink lips over a grin of white teeth; a large sleepy eye on each side in a
# yellow ring, white with a magenta iris and a heavy pink lid, a light blue brow over it; light blue spots on the pink;
# a pink stalk rising from the forehead to a purple bulb; a wavy light blue fin along the back; purple fins (a pectoral
# each side behind the eye with light blue tips, one under the belly) and a purple two-lobed tail with light blue
# middles. Joints: body (0, 0, 0.162), tail (0, 0.15, 0.162).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, unit, eye_look, polygon, tri, args

H = 0.4                         # the model's height: sizes below are shares of it
PINK, LIP, YELLOW, BLUE, PURPLE, WHITE, INK, IRIS, MOUTH = ("#F27DC4", "#E8549E", "#F7E86A", "#8BD8EE", "#9A5FC6",
                                                            "#FBFBF8", "#2A1830", "#D8409A", "#5A1838")
E = 0.0016                      # an edge a little under the voxel


def on(c, r, d):
    d = np.array(d, float); d /= np.linalg.norm(d)
    n = d / r
    return c + d * r, n / np.linalg.norm(n)


BODY_C, BODY_R = np.array([0.0, -0.03, 0.16]), np.array([0.054, 0.145, 0.11])
BROW_C, BROW_R = np.array([0.0, -0.075, 0.205]), np.array([0.05, 0.088, 0.092])
EYE_C = np.array([0.0, -0.07, 0.212])           # the eye's middle on the midline; its socket stands out at the side


def body(k):
    k.add("body", ell(BODY_C, BODY_R, sub=5), PINK, "body")
    k.add("brow", ell(BROW_C, BROW_R, sub=5), PINK, "body")
    k.add("snout", ell((0.0, -0.15, 0.13), (0.043, 0.07, 0.062), sub=5), PINK, "body")
    k.add("chin", ell((0.0, -0.1, 0.085), (0.046, 0.09, 0.05), sub=5), PINK, "body")
    # Sockets: the side bulges a little round each eye, so the eye faces forward of straight out
    k.add("socket", ell(EYE_C + np.array([0.03, 0.0, 0.0]), (0.03, 0.04, 0.04), sub=4), PINK, "body", mirror=True)
    V, F = tube([(0, 0.08, 0.175), (0, 0.13, 0.172), (0, 0.175, 0.17), (0, 0.2, 0.17)], [0.07, 0.05, 0.03, 0.022])
    V[:, 0] *= 0.7
    k.add("stalk", (V, F), PINK, "tail")
    # The lips, fused, a darker pink
    k.add("lipup", ell((0.0, -0.198, 0.142), (0.044, 0.026, 0.02), rot=(-8, 0, 0), sub=4), LIP, "body")
    k.add("liplow", ell((0.0, -0.192, 0.097), (0.042, 0.026, 0.02), rot=(8, 0, 0), sub=4), LIP, "body")
    for s in (1, -1):
        k.add(f"lipside{'R' if s > 0 else 'L'}", ell((0.036 * s, -0.17, 0.12), (0.016, 0.03, 0.03), sub=4), LIP, "body")
    # The colours, by place along the side: yellow below, a light blue band behind the mouth, a yellow ring round the
    # eye joined to the belly's yellow by a band in front of the pectoral
    ex = np.abs  # noqa: just a short name
    lips = ["body", "brow", "snout", "chin", "socket", "stalk"]
    def yellow(P):
        y, z = P[:, 1], P[:, 2]
        low = inside(z - (0.125 - 0.12 * np.clip(y - 0.02, 0, 1)), E)
        dy, dz = y - EYE_C[1], z - EYE_C[2]
        ring = inside(np.hypot(dy / 0.046, dz / 0.046) - 1, E * 25)
        band = inside(np.abs((y + 0.04) - (z - 0.17) * 0.45) - 0.02, E) * inside(z - EYE_C[2], E) * inside(0.11 - z, E)
        return np.maximum(np.maximum(low, ring), band)
    k.paint(yellow, YELLOW, on=lips)
    k.paint(lambda P: inside(P[:, 1] - (-0.128 + (P[:, 2] - 0.12) * 0.25), E) * inside(P[:, 2] - 0.178, E)
            * inside(0.07 - P[:, 2], E), BLUE, on=lips)


def spots(k):
    for y, z, r in ((-0.095, 0.265, 0.016), (-0.02, 0.245, 0.017), (0.045, 0.225, 0.013), (0.0, 0.165, 0.011),
                    (0.06, 0.16, 0.012), (-0.03, 0.15, 0.009), (0.1, 0.2, 0.01)):
        p = np.array([0.06, y, z])
        k.decal(p, unit(1, -0.1, 0.1), (r, r), BLUE, eps=0.0016, rings=8, segs=36, mirror=True)


def teeth(k):
    """The grin between the lips: one white patch with a zigzag of the mouth's dark between the teeth."""
    cw, cm = srgb(WHITE), srgb(MOUTH)
    def look(u, v):
        c = np.tile(cw, (len(u), 1))
        zig = 0.6 * tri(u * 1.5 + 0.25)
        c[np.abs(v - zig * 0.6) < 0.22] = cm
        return c
    k.decal((0.0, -0.21, 0.119), unit(0, -1, 0.05), (0.034, 0.013), look, eps=0.0014, rings=14, segs=56)


def fin(k, name, outline, origin, ex, ez, bone, colour, thick=0.013):
    k.add(name, slab(outline, origin, ex, ez, thick), colour, bone, fuse=False)


def fins(k):
    # The bulb: a pink stalk out of the forehead, a purple bulb leaning back on it
    k.add("bulbstalk", tube([(0, -0.085, 0.27), (0, -0.095, 0.31), (0, -0.09, 0.345)], [0.016, 0.012, 0.012]),
          PINK, "body", fuse=False)
    k.add("bulb", ell((0.0, -0.08, 0.372), (0.024, 0.026, 0.034), rot=(-18, 0, 0)), PURPLE, "body", fuse=False)
    # The wavy light blue fin along the back, turned 40 degrees to +X
    yaw = math.radians(40)
    ex = (math.sin(yaw), math.cos(yaw), 0.0)
    back = [(-0.05, -0.012), (-0.045, 0.022), (-0.02, 0.03), (0.005, 0.02), (0.03, 0.034), (0.06, 0.026),
            (0.085, 0.034), (0.11, 0.014), (0.12, -0.012)]
    fin(k, "backfin", back, (0.0, -0.02, 0.262), ex, (0.0, -0.33, 1.0), "body", BLUE)
    # The light blue brow over each eye
    for s in (1, -1):
        o = EYE_C + np.array([0.046 * s, 0.004, 0.03])
        brow = [(-0.032, -0.004), (-0.012, 0.012), (0.012, 0.016), (0.034, 0.008), (0.03, -0.006), (-0.03, -0.012)]
        k.add(f"brow{'R' if s > 0 else 'L'}", slab(brow, o, (0.0, 1.0, 0.0), (0.45 * s, 0.0, 1.0), 0.012), BLUE, "body",
              fuse=False)
    # The pectorals: purple, two lobes with light blue tips, faces to the front
    pec = [(0.0, 0.016), (0.03, 0.03), (0.055, 0.026), (0.07, 0.006), (0.058, -0.006), (0.07, -0.022), (0.05, -0.032),
           (0.02, -0.02), (0.0, -0.014)]
    tips = [(0.045, 0.03), (0.058, 0.03), (0.072, 0.006), (0.058, -0.006), (0.072, -0.024), (0.054, -0.034),
            (0.044, -0.018), (0.05, 0.004)]
    for s in (1, -1):
        o = np.array([0.045 * s, 0.0, 0.135])
        exs, ezs = (0.7 * s, 0.55, -0.35), (0.0, -0.3, 1.0)
        fin(k, f"pec{'R' if s > 0 else 'L'}", pec, o, exs, ezs, "body", PURPLE, 0.012)
        fin(k, f"pectip{'R' if s > 0 else 'L'}", tips, o, exs, ezs, "body", BLUE, 0.016)
    # The fin under the belly, purple and wavy, turned 40 degrees to +X
    belly = [(-0.06, 0.012), (-0.05, -0.018), (-0.02, -0.012), (0.01, -0.026), (0.04, -0.016), (0.07, -0.028),
             (0.085, 0.012)]
    fin(k, "bellyfin", belly, (0.0, 0.02, 0.06), ex, (0, 0, 1), "body", PURPLE)
    # The tail: purple, two lobes, light blue middles; turned 50 degrees to +X so both cameras see a face
    yaw = math.radians(50)
    tex = (math.sin(yaw), math.cos(yaw), 0.0)
    tail = [(-0.01, 0.018), (0.03, 0.05), (0.07, 0.07), (0.088, 0.05), (0.075, 0.018), (0.06, 0.0), (0.075, -0.02),
            (0.088, -0.05), (0.068, -0.068), (0.03, -0.046), (-0.01, -0.018)]
    mids = [[(0.035, 0.03), (0.065, 0.05), (0.072, 0.03), (0.055, 0.014)],
            [(0.035, -0.03), (0.055, -0.014), (0.072, -0.03), (0.065, -0.05)]]
    o = np.array([0.0, 0.195, 0.17])
    fin(k, "tailfin", tail, o, tex, (0, 0, 1), "tail", PURPLE, 0.013)
    for i, m in enumerate(mids):
        fin(k, f"tailmid{i}", m, o, tex, (0, 0, 1), "tail", BLUE, 0.018)


# --- the eyes: white in the yellow ring, a magenta iris and dark pupil, the heavy pink lid ---------------------------
CW, CI, CK, CP, CY = srgb(WHITE), srgb(IRIS), srgb(INK), srgb(PINK), srgb(YELLOW)
EYE_N = unit(0.74, -0.6, 0.18)
EYE_HALF = (0.028, 0.03)


def eye_open(u, v):
    c = np.tile(CW, (len(u), 1))
    c[(u + 0.05) ** 2 + (v + 0.12) ** 2 < 0.52 ** 2] = CI
    c[(u + 0.05) ** 2 + (v + 0.12) ** 2 < 0.24 ** 2] = CK
    lid = 0.3 - 0.12 * u
    c[v > lid] = CP
    c[np.abs(v - lid) < 0.11] = CK
    return c


def eye_point(s):
    p, n = on(EYE_C + np.array([0.03, 0, 0]), np.array([0.03, 0.04, 0.04]), (1, -0.35, 0.05))
    return p * np.array([s, 1, 1])


def eyes(k, state):
    for s in (1, -1):
        n = EYE_N * np.array([s, 1, 1])
        look = eye_look(state, eye_open, CP, CK, line=0.15)
        k.decal(eye_point(s), n, EYE_HALF, look if s > 0 else (lambda u, v, f=look: f(-u, v)), eps=0.0018,
                rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="scales")
    body(k)
    spots(k)
    teeth(k)
    fins(k)
    k.build(voxel=0.0038, smooth=4, band=0.005, wband=0.03)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
