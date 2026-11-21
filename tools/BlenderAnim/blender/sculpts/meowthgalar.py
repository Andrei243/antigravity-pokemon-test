# Meowth-Galar sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py),
# grown from meowth.py (imported: its ears, coin, curled tail and way of laying a patch on the head):
#   blender --background --factory-startup --python sculpts/meowthgalar.py -- <the export's Meowth-Galar.glb> <out.glb>
#
# From the references: head and body are one great shaggy ball of taupe fur, two thirds of the figure's height and as
# wide, ragged all round its edge with pointed tufts (out at the cheeks, down at the belly's fringe, short ones on the
# chest); short legs under it with dark feet and a dark band at the knee; arms coming out of the fur's sides, each paw
# with three thick curved black claws; a dark iron coin with two grooves upright on the brow, and a dark curved spike
# rising out to either side of it; pointed ears, dark outside and at the tip, pale inside; big round yellow eyes with a
# black slit pupil and a dark rim, close together; a wide grimace of big white teeth meeting in a zigzag; a dark tail
# curling behind. The arms are carried forward and down (the pictures fling them out).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, unit, inside, tri, eye_look, args, sibling

m = sibling("meowth")

FUR = "#A6967F"
DARK = "#2E3432"
LINING = "#D9CEBE"
COIN, GROOVE = "#454A48", "#1E2222"
IRIS, INK = "#F6C42E", "#1C1C20"
TOOTH, LIP = "#F4F2EC", "#3A3234"

# --- the frame (the skeleton's joints: body (0, 0, 0.33), head (0, -0.01, 0.44), ears (+-0.1, 0, 0.66), arms
#     (+-0.08, -0.01, 0.41), legs (+-0.05, 0, 0.26), tail (0, 0.06, 0.27)) --------------------------------------------
HF = np.array([0, -0.02, 0.6])           # the head's half of the ball of fur
HFR = np.array([0.27, 0.21, 0.205])
BF = np.array([0, 0.0, 0.39])            # the body's half
BFR = np.array([0.235, 0.195, 0.19])


def on_head(x, z, lift=0.0):
    return m.on_head(x, z, lift, h=HF, hr=HFR)


def shag(V):
    """Ragged bumps round the ball, the face left smooth."""
    th = np.arctan2(V[:, 1], V[:, 0]); ph = np.arccos(np.clip(V[:, 2], -1, 1))
    bump = 0.05 * np.abs(np.sin(9 * th) * np.sin(6 * ph))
    face = np.clip((-V[:, 1] - 0.55) / 0.3, 0, 1) * np.clip((V[:, 2] + 0.55) / 0.3, 0, 1)
    return V * (1 + bump * (1 - face))[:, None]


def add_fur(k):
    k.add("headfur", ell(HF, HFR, deform=shag, sub=5), FUR, "head")
    k.add("bodyfur", ell(BF, BFR, deform=shag, sub=5), FUR, "body")
    k.add("waist", ell((0, -0.012, 0.5), (0.255, 0.2, 0.12), deform=shag), FUR, "body")


def tuft(c, d, length, r):
    """A pointed tuft of fur: a five-sided cone, its root sunk in the ball."""
    d = unit(*d)
    a = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a); b = np.cross(d, a)
    root = np.array(c, float) - d * 0.025
    pts = [root + r * (math.cos(t) * a * 1.25 + math.sin(t) * b * 0.8) for t in np.linspace(0, 2 * math.pi, 6)[:-1]]
    pts.append(root + d * (length + 0.025))
    return hull(pts)


def on_ball(C, R, a, y, lift=0.0):
    """A point of a ball of fur seen from the front at angle a (0 to the side, 90 up), at depth share y."""
    x, z = math.cos(math.radians(a)), math.sin(math.radians(a))
    s = math.sqrt(max(0.0, 1 - y * y))
    p = C + R * np.array([x * s, y, z * s])
    n = (p - C) / R ** 2; n /= np.linalg.norm(n)
    return p + n * lift, n


def add_tufts(k):
    rs = np.random.RandomState(52)
    # Cheeks: out and a little down, on the head's sides
    for side in (1, -1):
        for i, a in enumerate((25, 5, -15, -35)):
            ang = a if side > 0 else 180 - a
            p, n = on_ball(HF, HFR, ang, -0.15)
            d = n + np.array([0, 0.1, -0.35])
            k.add(f"cheek{side}{i}", tuft(p, d, rs.uniform(0.055, 0.075), 0.044), FUR, "head", fuse=False, surface="fur")
        for i, a in enumerate((60, 45)):
            ang = a if side > 0 else 180 - a
            p, n = on_ball(HF, HFR, ang, 0.1)
            k.add(f"crown{side}{i}", tuft(p, n + np.array([0, 0.2, 0.1]), rs.uniform(0.042, 0.055), 0.038), FUR,
                  "head", fuse=False, surface="fur")
    # The belly's fringe, down and out
    for i, a in enumerate(np.linspace(-160, -20, 7)):
        p, n = on_ball(BF, BFR, a, -0.35)
        k.add(f"fringe{i}", tuft(p, n + np.array([0, 0, -0.6]), rs.uniform(0.05, 0.07), 0.044), FUR, "body",
              fuse=False, surface="fur")
    for i, a in enumerate(np.linspace(-150, -30, 5)):
        p, n = on_ball(BF, BFR, a, 0.45)
        k.add(f"back{i}", tuft(p, n + np.array([0, 0.3, -0.5]), rs.uniform(0.045, 0.062), 0.04), FUR, "body",
              fuse=False, surface="fur")
    # Short tufts down the chest, pointing down
    for i, (x, z) in enumerate([(-0.09, 0.36), (0.0, 0.33), (0.09, 0.36), (-0.05, 0.27), (0.05, 0.27)]):
        q = BF + np.array([x, 0, z - BF[2]])
        yy = -BFR[1] * math.sqrt(max(0.0, 1 - (x / BFR[0]) ** 2 - ((z - BF[2]) / BFR[2]) ** 2))
        p = np.array([x, BF[1] + yy, z])
        k.add(f"chest{i}", tuft(p, (0, -0.55, -1), rs.uniform(0.036, 0.048), 0.034), FUR, "body", fuse=False,
              surface="fur")


def add_legs(k):
    k.add("leg", tube([(0.05, 0.0, 0.26), (0.066, 0.0, 0.15), (0.072, -0.008, 0.06)], [0.05, 0.044, 0.04]),
          FUR, "leg{s}", mirror=True)
    k.add("foot", ell((0.074, -0.04, 0.032), (0.056, 0.078, 0.034)), DARK, "leg{s}", mirror=True)
    # Dark socks and a dark band at the knee, crisp lines across the leg
    k.paint(lambda P: inside(P[:, 2] - 0.1, 0.0025) * (np.abs(P[:, 0]) > 0.02) * (P[:, 1] < 0.06), DARK)
    k.paint(lambda P: inside(P[:, 2] - 0.185, 0.0025) * inside(0.155 - P[:, 2], 0.0025) * (np.abs(P[:, 0]) > 0.025)
            * (P[:, 1] < 0.06), DARK)


def add_arms(k):
    """Out of the fur's sides, the elbows bent so the paws come forward before the belly, clawed."""
    k.add("arm", tube([(0.08, -0.01, 0.41), (0.2, -0.04, 0.39), (0.225, -0.13, 0.34), (0.205, -0.2, 0.32)],
                      [0.042, 0.04, 0.038, 0.036]), FUR, "arm{s}", mirror=True)
    k.add("paw", ell((0.2, -0.215, 0.318), (0.048, 0.044, 0.042)), FUR, "arm{s}", mirror=True)
    for i, (dx, dz) in enumerate(((0.03, 0.022), (0.006, 0.006), (-0.02, -0.012))):
        root = np.array([0.2 + dx, -0.248, 0.318 + dz])
        d = unit(0.35 + dx * 6, -1.0, -0.15 + dz * 5)
        a = np.cross(d, (0, 0, 1.0)); a /= np.linalg.norm(a); b = np.cross(d, a)
        bend = np.array([0, 0, -0.025])
        pts = [root + 0.016 * (math.cos(t) * a + math.sin(t) * b) - d * 0.01 for t in np.linspace(0, 2 * math.pi, 7)[:-1]]
        pts += [root + d * 0.035 + bend * 0.3 + 0.01 * (math.cos(t) * a + math.sin(t) * b) for t in (0, 2.1, 4.2)]
        pts.append(root + d * 0.07 + bend)
        k.add(f"claw{i}", hull(pts), DARK, "arm{s}", fuse=False, mirror=True, surface="shell")


def add_spikes(k):
    """A dark curved spike rising out to either side of the coin."""
    for s in (1, -1):
        p0, n0 = on_head(0.075 * s, HF[2] + 0.155, -0.012)
        pts = [p0, p0 + np.array([0.025 * s, 0.01, 0.06]), p0 + np.array([0.065 * s, 0.03, 0.1]),
               p0 + np.array([0.11 * s, 0.045, 0.115])]
        k.add(f"spike{'R' if s > 0 else 'L'}", tube(pts, [0.026, 0.021, 0.015, 0.009]), DARK, "head", fuse=False,
              surface="shell")


def add_coin(k, half=(0.042, 0.062), thick=0.026):
    c, n = on_head(0.0, HF[2] + 0.165, 0.01)
    up = np.array([0, 0, 1.0]) - n * n[2]; up /= np.linalg.norm(up)
    side = np.cross(up, n)
    k.add("coin", m.coin(c, n, up, half=half, thick=thick), COIN, "head", fuse=False, surface="metal")
    for i, s in enumerate((-0.05, -0.45)):
        w = half[0] * 0.86 * math.sqrt(max(0.0, 1 - s * s)) * 0.78
        p = c + n * (thick * 0.5 + 0.003) + up * s * half[1]
        corners = [p + side * sx * w + up * sz * 0.0075 + n * sn * 0.004
                   for sx in (-1, 1) for sz in (-1, 1) for sn in (-1, 1)]
        k.add(f"groove{i}", hull(corners), GROOVE, "head", fuse=False, surface="metal")


EAR_BASE = np.array([0.16, 0.0, 0.73])
EAR_TIP = np.array([0.235, 0.025, 0.875])
EAR_R = [0.07, 0.058, 0.032, 0.008]


def add_tail(k):
    m.CREAM, m.BROWN = FUR, DARK
    m.add_tail(k)


# --- the face ---------------------------------------------------------------------------------------------------------
CF, CI, CY, CT, CL = srgb(FUR), srgb(INK), srgb(IRIS), srgb(TOOTH), srgb(LIP)


def grimace(u, v):
    """Big white teeth meeting in a zigzag, a dark lip round them."""
    c = np.tile(CT, (len(u), 1))
    zig = 0.3 * tri(u * 1.25 + 0.25) - 0.02
    c[np.abs(v - zig) < 0.2] = CL
    c[np.hypot(u, v) > 0.83] = CL
    return c


def add_mouth(k):
    p, n = on_head(0.0, HF[2] - 0.055)
    k.decal(p, unit(0, -1, -0.12), (0.105, 0.05), grimace, eps=0.003, rings=24, segs=96)


EYE_X, EYE_Z = 0.088, 0.05
EYE_HALF = (0.05, 0.054)


def eye(u, v):
    """A round yellow eye with a dark rim and a black slit pupil."""
    c = np.tile(CI, (len(u), 1))
    c[np.hypot(u, v) < 0.8] = CY
    c[np.abs(u + 0.04) < 0.16 * np.sqrt(np.clip(1 - (v / 0.66) ** 2, 0, 1))] = CI
    return c


def ring(u, v):
    c = np.tile(CI, (len(u), 1))
    c[np.hypot(u, v) < 0.8] = CY
    return c


def lay_eyes(k, state):
    p, n = on_head(EYE_X, HF[2] + EYE_Z)
    up = unit(0.14, 0, 1)
    if state == "Open":
        k.decal(p, n, EYE_HALF, ring, eps=0.004, rings=20, segs=56, mirror=True, up=up)
        q, nq = on_head(EYE_X - 0.002, HF[2] + EYE_Z)
        k.decal(q, nq, (0.0085, 0.036), CI, eps=0.0065, rings=8, segs=40, mirror=True, up=up)
        return
    k.decal(p, n, EYE_HALF, eye_look(state, eye, CF, CI), eps=0.004, rings=20, segs=56, mirror=True, up=up)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    add_fur(k)
    add_tufts(k)
    add_legs(k)
    add_arms(k)
    m.add_ears(k, base=EAR_BASE, tip=EAR_TIP, radii=EAR_R, outer=DARK, lining=LINING, inset=0.6, coat=FUR, root=0.2)
    add_spikes(k)
    add_coin(k)
    add_tail(k)
    add_mouth(k)
    k.build(voxel=0.0062, band=0.006)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)
