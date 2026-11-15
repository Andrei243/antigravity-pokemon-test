# Ninjask sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ninjask.py -- <the export's Ninjask.glb> <out.glb>
#
# The design, from the references: a hovering cicada. A great round head whose front and top are a yellow mask with
# two pointed yellow horns at its upper corners; a black keyhole on it (a disc on the brow, a bar across at the eyes'
# height, a short stem between); round red eyes with a pink glow and a white glint at the bar's ends; a grey plate
# under the head; a round black abdomen behind and below with a yellow tip; a pair of thick grey hooked forelegs with
# yellow claws curling under the head; two pairs of white wings rimmed and veined in grey, the larger rising up and
# out with three red triangles along the upper edge near the tip, the smaller out to the sides below them.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, hull, srgb, inside, unit, eye_look, args

YELLOW, BLACK, GREY, CLAWY = "#EAC456", "#2C2C34", "#9C9EA6", "#E8B840"
WING, RIM, VEIN, RED = "#F2F3F7", "#9AA0AC", "#A6ACB8", "#E0404E"
EYERED, EYEPINK, INK = "#D8243E", "#FF8FA4", "#141218"

H = np.array([0.0, -0.075, 0.272])          # the head's centre
HR = np.array([0.088, 0.074, 0.08])


def wing_outline(L, W, t0=0.0, t1=1.0, inset=1.0, n=26):
    t = np.linspace(t0, t1, n)
    w = W * np.sqrt(np.clip(1 - ((t - 0.55) / 0.55) ** 2, 0, 1)) * (0.5 + 0.5 * t) * inset
    w = np.maximum(w, 0.001)
    a = 0.004 + t * L
    return np.vstack([np.stack([a, w], 1), np.stack([a, -w], 1)[::-1]]), t, w


def frame(d, nd):
    d = np.array(d, float); d /= np.linalg.norm(d)
    ez = np.cross(nd, d); ez /= np.linalg.norm(ez)
    if ez[2] < 0:                  # +b is the wing's upper edge, where the red triangles go
        ez = -ez
    return d, ez


def wing(k, name, bone, root, d, nd, L, W, red):
    for s, side in ((1, "R"), (-1, "L")):
        o = np.array(root, float) * (s, 1, 1)
        ex, ez = frame(np.array(d) * (s, 1, 1), np.array(nd) * (s, 1, 1))
        b = bone.format(s=side)
        out, _, _ = wing_outline(L, W)
        k.add(f"{name}{side}", slab(out, o, ex, ez, 0.004), RIM, b, fuse=False)
        inner, _, _ = wing_outline(L, W, 0.03, 0.985, 0.86)
        k.add(f"{name}in{side}", slab(inner, o, ex, ez, 0.0065), WING, b, fuse=False)
        # two veins along it
        for i, f in enumerate((-0.05,)):
            t = np.linspace(0.08, 0.9, 10)
            _, _, wv = wing_outline(L, W, 0.08, 0.9, 1.0, n=10)
            mid = np.stack([0.004 + t * L, wv * f], 1)
            top = mid + (0, 0.003); bot = mid - (0, 0.003)
            k.add(f"{name}v{i}{side}", slab(np.vstack([top, bot[::-1]]), o, ex, ez, 0.009), VEIN, b, fuse=False)
        if red:
            # three red triangles along the upper edge near the tip, points inward
            for i, (t0, t1) in enumerate(((0.56, 0.68), (0.7, 0.82), (0.84, 0.95))):
                _, ts, ws = wing_outline(L, W, t0, t1, 0.9, n=6)
                edge = [(0.004 + tt * L, ww) for tt, ww in zip(ts, ws)]
                tip = (0.004 + t0 * L - 0.012, ws[0] * 0.3)
                k.add(f"{name}red{i}{side}", slab(np.array(edge[::-1] + [tip]), o, ex, ez, 0.0095), RED, b, fuse=False)


def ninjask(src, out):
    k = Kit(src, coat="shell")

    # --- the head: a yellow mask over the front and top, black behind -------------------------------------------
    k.add("head", ell(H, HR), YELLOW, "head")
    k.paint(lambda P: inside(-(P[:, 1] - H[1] - 0.025), 0.004) * inside(np.linalg.norm((P - H) / (HR + 0.03), axis=1) - 1, 0.01),
            BLACK, on=["head"])
    # the keyhole: a bar across at the eyes' height and a stem up to the disc on the brow
    def keyhole(P):
        q = P - H
        front = q[:, 1] < -0.02
        bar = inside(np.abs(q[:, 2] + 0.004) - 0.011, 0.0015) * inside(np.abs(q[:, 0]) - 0.07, 0.003)
        stem = inside(np.abs(q[:, 0]) - 0.008, 0.0015) * inside(q[:, 2] - 0.045, 0.002) * inside(-q[:, 2] - 0.0, 0.002)
        return np.maximum(bar, stem) * front
    k.paint(keyhole, BLACK, on=["head"])

    # the horns: pointed yellow solids at the head's upper corners
    for s, side in ((1, "R"), (-1, "L")):
        base = H + np.array([0.05 * s, -0.012, 0.058])
        tip = H + np.array([0.074 * s, -0.026, 0.118])
        pts = [base + (0.024 * s, 0.0, -0.006), base + (-0.02 * s, 0.0, 0.006), base + (0, -0.02, -0.004),
               base + (0, 0.022, -0.004), tip]
        k.add(f"horn{side}", hull(pts), YELLOW, "head", fuse=False)

    # --- the body: a round black abdomen behind and below, a grey plate under the head, a yellow tip ---------------
    k.add("abdomen", ell((0, 0.03, 0.2), (0.078, 0.088, 0.088), rot=(-22, 0, 0)), BLACK, "body")
    k.add("plate", ell((0, -0.055, 0.203), (0.068, 0.052, 0.016)), GREY, "body")
    k.paint(lambda P: inside(P[:, 2] - 0.14, 0.004) * (P[:, 1] > 0.0), YELLOW, on=["abdomen"])

    # --- the forelegs: thick grey hooks curling under the head, yellow claws ------------------------------------
    k.add("leg", tube([(0.05, -0.065, 0.192), (0.09, -0.11, 0.17), (0.1, -0.138, 0.13), (0.08, -0.145, 0.1)],
                      [0.016, 0.016, 0.014, 0.012]), GREY, "leg{s}", fuse=False, mirror=True)
    k.add("claw", tube([(0.08, -0.145, 0.1), (0.062, -0.148, 0.088), (0.048, -0.15, 0.084)], [0.012, 0.009, 0.005]),
          CLAWY, "leg{s}", fuse=False, mirror=True)
    k.add("hind", tube([(0.045, 0.03, 0.15), (0.068, 0.02, 0.11), (0.058, 0.0, 0.085)], [0.011, 0.01, 0.008]),
          GREY, "leg{s}", fuse=False, mirror=True)
    k.add("hindclaw", tube([(0.058, 0.0, 0.085), (0.048, -0.012, 0.08), (0.04, -0.018, 0.08)], [0.008, 0.006, 0.003]),
          CLAWY, "leg{s}", fuse=False, mirror=True)

    # --- the wings -----------------------------------------------------------------------------------------------
    wing(k, "fore", "wing{s}", (0.05, 0.035, 0.27), (0.72, 0.3, 0.62), (0, -1, 0.35), 0.21, 0.072, True)
    wing(k, "back", "wing{s}.001", (0.05, 0.045, 0.22), (0.88, 0.35, 0.05), (0, -1, 0.45), 0.15, 0.055, False)

    # the keyhole's disc on the brow, over the stem's top
    dd = unit(0, -0.6, 0.8)
    pd = H + dd / math.sqrt(((dd / HR) ** 2).sum())
    k.decal(pd, (pd - H) / HR ** 2, (0.025, 0.025), BLACK, eps=0.003, rings=12, segs=48)

    k.build(voxel=0.0036, smooth=4, band=0.006)

    # --- the eyes: round red, a pink glow, a white glint, at the bar's ends ------------------------------------
    CR, CP, CW, CY = srgb(EYERED), srgb(EYEPINK), np.array([1.0, 1.0, 1.0]), srgb(YELLOW)
    def eye(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(CR, (len(u), 1))
        c[r < 0.62] = CP
        c[((u + 0.15) / 0.3) ** 2 + ((v - 0.25) / 0.3) ** 2 < 1] = CW
        return c
    de = unit(0.8, -0.58, -0.04)
    p = H + de / math.sqrt(((de / HR) ** 2).sum())
    n = (p - H) / HR ** 2; n /= np.linalg.norm(n)

    def eyes(state):
        k.decal(p, n, (0.023, 0.023), eye_look(state, eye, CY, srgb(INK), line=0.16), eps=0.003, rings=20, segs=56,
                mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    ninjask(*args())
