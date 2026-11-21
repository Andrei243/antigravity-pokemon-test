# Thievul sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/thievul.py -- <the export's Thievul.glb> <out.glb>
# Nickit grown into a tall sly fox: a long head with a pointed muzzle, Nickit's black mask grown out at each side into
# black points, a white mark on the brow, narrow half-lidded yellow eyes, a white curl of moustache down from each
# corner of the mouth; big pointed black ears; a great white ruff hanging from the throat over the chest; a russet body
# with a black saddle down the back and a black patch on the hip; long legs in black boots; a great bushy tail curving
# back and down to the ground, ending in a black spiky brush.
#
# Grown from nickit.py (its colours, mask, eyes, ears, marks and socks).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, polygon, tri, lumpy, args, sibling

nk = sibling("nickit")
RUSSET, BLACK, WHITE = nk.RUSSET, nk.BLACK, nk.WHITE

# --- the frame (skeleton: body (0, 0, 0.28), head (0, -0.15, 0.44), legs (+-0.05, -0.1, 0.25),
#     hind legs (+-0.05, 0.12, 0.25), tail (0, 0.15, 0.3)) ---------------------------------------------------------------
H = np.array([0, -0.2, 0.505])
HR = np.array([0.085, 0.082, 0.075])
LEG_X = 0.052
FORE_Y, HIND_Y = -0.1, 0.125


def add_body(k):
    k.add("chest", ell((0, -0.08, 0.31), (0.075, 0.09, 0.085)), RUSSET, "body")
    k.add("waist", ell((0, 0.03, 0.31), (0.062, 0.08, 0.065)), RUSSET, "body")
    k.add("rump", ell((0, 0.12, 0.32), (0.07, 0.075, 0.075)), RUSSET, "body")
    k.add("neck", ell((0, -0.15, 0.41), (0.055, 0.055, 0.08), rot=(35, 0, 0)), RUSSET, "body")
    # the white ruff from the throat over the chest, its lower edge in soft points
    k.add("ruff", ell((0, -0.16, 0.33), (0.068, 0.06, 0.1), deform=lumpy(6, 2, 0.06)), WHITE, "body")
    for i, x in enumerate((-0.04, 0.0, 0.04)):
        b = np.array([x, -0.17, 0.25])
        k.add(f"ruffpt{i}", blade([b, b + (x * 0.2, -0.004, -0.03), b + (x * 0.4, 0.0, -0.06)], [0.03, 0.02, 0.006],
                                  (0, 1, 0), 0.6), WHITE, "body")
    # the black saddle down the back, pointed at both ends, and a black patch on the hip
    def saddle(P):
        t = np.clip((P[:, 1] + 0.1) / 0.24, 0, 1)
        w = 0.05 * np.sin(np.pi * t) ** 0.6 + 0.006
        return inside(np.abs(P[:, 0]) - w, 0.002) * (P[:, 2] > 0.33) * (P[:, 1] > -0.1) * (P[:, 1] < 0.14)
    k.paint(saddle, BLACK, on=["chest", "waist", "rump"])
    def hip(P):
        d = (P - np.array([0, 0.13, 0.35])) / np.array([1.0, 0.045, 0.035])
        return inside(np.hypot(d[:, 1], d[:, 2]) - 1, 0.003) * (np.abs(P[:, 0]) > 0.04)
    k.paint(hip, BLACK, on=["rump"])


def add_legs(k, boot=0.17, hboot=0.13):
    # fused shoulders and thighs over the legs' upper halves; the lower legs taper and are never sticks
    k.add("shoulder", ell((LEG_X - 0.008, FORE_Y, 0.25), (0.04, 0.05, 0.075)), RUSSET, "leg{s}", mirror=True)
    k.add("leg", tube([(LEG_X, FORE_Y, 0.22), (LEG_X + 0.002, FORE_Y - 0.004, 0.12), (LEG_X + 0.002, FORE_Y - 0.01, 0.022)],
                      [0.032, 0.026, 0.024]), RUSSET, "leg{s}", mirror=True)
    k.add("paw", ell((LEG_X + 0.002, FORE_Y - 0.026, 0.015), (0.024, 0.034, 0.015)), BLACK, "leg{s}", mirror=True)
    k.add("thigh", ell((LEG_X, HIND_Y, 0.25), (0.048, 0.065, 0.085)), RUSSET, "hind{s}", mirror=True)
    k.add("hind", tube([(LEG_X + 0.002, HIND_Y + 0.025, 0.2), (LEG_X + 0.003, HIND_Y + 0.035, 0.11),
                        (LEG_X + 0.003, HIND_Y + 0.01, 0.022)], [0.032, 0.026, 0.024]), RUSSET, "hind{s}", mirror=True)
    k.add("hpaw", ell((LEG_X + 0.003, HIND_Y - 0.008, 0.015), (0.024, 0.034, 0.015)), BLACK, "hind{s}", mirror=True)
    k.paint(lambda P: inside(P[:, 2] - boot, 0.002) * (P[:, 1] < 0.0), BLACK, on=["leg", "paw"])
    k.paint(lambda P: inside(P[:, 2] - hboot, 0.002) * (P[:, 1] > 0.0), BLACK, on=["hind", "hpaw"])
    k.paint(lambda P: inside(boot - P[:, 2], 0.002), RUSSET, on=["leg"])
    k.paint(lambda P: inside(hboot - P[:, 2], 0.002), RUSSET, on=["hind"])


def add_head_parts(k):
    nk.add_head(k, H, HR, muzzle=(0.046, 0.078, 0.036))
    # the mask's ends grown out into black points at the sides of the head, swept back
    for s in (1, -1):
        b = H + np.array([0.072 * s, 0.0, 0.014])
        t = b + np.array([0.05 * s, 0.03, 0.008])
        k.add(f"maskpt{'R' if s > 0 else 'L'}", blade([b, (b + t) / 2, t], [0.026, 0.018, 0.006], (0, 0, 1), 0.5),
              BLACK, "head")
    # big pointed black ears, up and a little out and back
    ear = [(0.046, -0.18, 0.555), (0.06, -0.175, 0.6), (0.076, -0.165, 0.64), (0.094, -0.15, 0.675)]
    nk.add_ears(k, ear=ear, radii=[0.044, 0.036, 0.02, 0.008], width=0.015)


MASK = [(0.0, -0.05), (0.42, -0.2), (0.85, -0.18), (1.2, 0.0), (1.35, 0.3), (0.95, 0.38), (0.6, 0.36),
        (0.3, 0.28), (0.0, 0.22)]


def add_face(k):
    nk.add_mask(k, H, HR, outline=MASK)
    # the white mark on the brow
    p, n = nk.on_head(0.0, H[2] + HR[2] * 0.62, H, HR)
    k.decal(p, n, (0.016, 0.008), WHITE, eps=0.003,
            outline=polygon([(-1, 0.2), (-0.3, 0.8), (0.3, 0.8), (1, 0.2), (0.4, -0.3), (0, -1), (-0.4, -0.3)]),
            rings=8, segs=36)
    # nose and mouth at the long muzzle's tip
    tip = H + np.array([0, -HR[1] * 0.95 - 0.066, -HR[2] * 0.42 + 0.002])
    k.add("nose", ell(tip, (0.013, 0.01, 0.01)), nk.NOSE, "head", fuse=False, surface="skin")
    CR, CM = srgb(RUSSET), srgb(nk.MOUTH)
    def grin(u, v):
        c = np.tile(CR, (len(u), 1))
        c[(np.abs(v - (-0.1 + 0.5 * u * u)) < 0.2) & (np.abs(u) < 0.9)] = CM
        return c
    k.decal(tip + np.array([0, 0.03, -0.02]), unit(0, -1, -0.6), (0.022, 0.009), grin, eps=0.0025, rings=10, segs=40)
    # the white moustache: a curl down and out from each corner of the mouth
    for s in (1, -1):
        b = tip + np.array([0.018 * s, 0.04, -0.02])
        k.add(f"tash{'R' if s > 0 else 'L'}", tube([b, b + (0.012 * s, -0.004, -0.018), b + (0.016 * s, -0.012, -0.032),
                                                     b + (0.01 * s, -0.02, -0.038)], [0.015, 0.014, 0.012, 0.009]),
              WHITE, "head", fuse=False)


def add_tail(k):
    # the great bushy tail: back from the rump, down in a curve, forward along the ground to its black brush
    pts = [(0.0, 0.17, 0.33), (0.02, 0.26, 0.32), (0.05, 0.33, 0.25), (0.07, 0.35, 0.16), (0.08, 0.31, 0.09)]
    k.add("tail", tube(pts, [0.045, 0.07, 0.08, 0.075, 0.065]), RUSSET, "tail")
    c = np.array([0.08, 0.25, 0.085])
    k.add("brush", ell(c, (0.075, 0.08, 0.075)), BLACK, "tail")
    rng = np.random.default_rng(828)
    for i in range(14):
        a = 2 * math.pi * i / 14 + rng.uniform(-0.15, 0.15)
        e = rng.uniform(-0.6, 0.8)
        d = np.array([math.cos(a) * math.cos(e), math.sin(a) * math.cos(e) * 0.9, math.sin(e)])
        if d[2] < -0.3:
            continue
        r0 = c + d * 0.055
        k.add(f"brushpt{i}", blade([r0, r0 + d * 0.035, r0 + d * 0.065], [0.03, 0.02, 0.006], (0, 0, 1), 0.6),
              BLACK, "tail")
    # the dark end shading up the tail toward the brush
    def dark(P):
        return inside(np.linalg.norm(P - c, axis=1) - 0.11, 0.004)
    k.paint(dark, BLACK, on=["tail"])


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    add_body(k)
    add_legs(k)
    add_head_parts(k)
    add_tail(k)
    add_face(k)
    k.build(voxel=0.006, band=0.004)
    k.eye_states(lambda s: nk.lay_eyes(k, s, x=0.043, dz=0.008, half=(0.026, 0.017), h=H, hr=HR, turn=1.15))
    k.export(out)
