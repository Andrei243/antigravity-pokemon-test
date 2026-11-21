# Gimmighoul (Chest Form) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gimmighoul.py -- <the export's Gimmighoul.glb> <out.glb>
#
# From the references: a treasure chest whose lid floats open above it, tipped back on its hinge. The box is a frame of
# square gold bars along every edge with deep red panels set back between them, heaped with gold coins over its rim; a
# black strap runs out from under its front and lies on the ground like a tongue. The lid is arched front to back (its
# ends D-shaped), red with gold rims at its ends, a red D panel on each end, gold rims along its lower edges, dark under
# it; a black strap runs from its back over its top down its front to a great silver disc engraved with a ring. Out of
# the coins peeks the ghost: a round pale grey-lavender head with two yellow eyes ringed in black and black pupils, two
# short black feelers ending in yellow balls, and two stubby arms raised up and out of the heap.
# The skeleton: body (0, 0, 0.08), jaw (the lid's hinge, at the back) (0, 0.085, 0.145), head (0, -0.02, 0.16),
# arms (+-0.03, -0.04, 0.155).
# Written to be grown from (gholdengo.py takes its small chest, its strap and its disc with sibling("gimmighoul")).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, slab, hull, lathe, srgb, inside, unit, eye_look, args, outward, lumpy

# --- colours ----------------------------------------------------------------------------------------------------------
GOLD, RED, BLACK = "#DDB653", "#B8303A", "#2A2A2F"
SILVER, SILVER_DK = "#CDD1DD", "#8E93AC"
COIN, COIN_DK = "#F2C431", "#C89A1E"
GHOST, YELLOW, INK = "#C5C9DB", "#F6D43C", "#1D1D27"
UNDER = "#3A2A2C"

# --- the frame --------------------------------------------------------------------------------------------------------
BOX_W, BOX_D, BOX_H = 0.12, 0.085, 0.145      # half width, half depth, height of the box's edges (bars on them)
BAR = 0.012                                   # half the bars' square section
HEAD_C, HEAD_R = np.array([0.0, -0.03, 0.19]), np.array([0.046, 0.042, 0.044])
HINGE = np.array([0.0, 0.105, 0.19])          # where the lid's back lower edge floats
LID_OPEN = -35.0                              # degrees about X: the front rises
LID_W, LID_D, LID_WALL, LID_ARCH = 0.142, 0.1, 0.028, 0.1
VOXEL = 0.0034


def box_hull(x0, x1, y0, y1, z0, z1, ch=0.0):
    """A box from its two corners, its edges chamfered by ch."""
    pts = []
    for x in (x0, x1):
        for y in (y0, y1):
            for z in (z0, z1):
                sx, sy, sz = (1 if x == x0 else -1), (1 if y == y0 else -1), (1 if z == z0 else -1)
                pts += [(x + sx * ch, y, z), (x, y + sy * ch, z), (x, y, z + sz * ch)] if ch else [(x, y, z)]
    return hull(pts)


def chest(k, c, w, d, h, bar, bone, gold=GOLD, red=RED, surface="metal"):
    """A chest's box: a red core set back between square gold bars along its twelve edges (the posts a little thicker,
    so no two faces of the frame lie in one plane), c the middle of its floor."""
    c = np.array(c, float)
    k.add("chestcore", box_hull(c[0] - w + bar * 0.6, c[0] + w - bar * 0.6, c[1] - d + bar * 0.6, c[1] + d - bar * 0.6,
                                c[2] + bar * 0.4, c[2] + h - bar * 0.2), red, bone, fuse=False, surface="shell")
    p = bar * 1.08
    for sx in (-1, 1):
        for sy in (-1, 1):
            x, y = c[0] + sx * w, c[1] + sy * d
            k.add(f"chestpost{sx}{sy}", box_hull(x - p, x + p, y - p, y + p, c[2], c[2] + h + p, ch=p * 0.3), gold, bone,
                  fuse=False, surface=surface)
    for z in (c[2] + bar, c[2] + h):
        for sy in (-1, 1):
            y = c[1] + sy * d
            k.add(f"chestbarx{z:.3f}{sy}", box_hull(c[0] - w, c[0] + w, y - bar, y + bar, z - bar, z + bar), gold, bone,
                  fuse=False, surface=surface)
        for sx in (-1, 1):
            x = c[0] + sx * w
            k.add(f"chestbary{z:.3f}{sx}", box_hull(x - bar, x + bar, c[1] - d, c[1] + d, z - bar, z + bar), gold, bone,
                  fuse=False, surface=surface)


def disc(k, name, c, n, r, bone, thick=0.012, face=SILVER, mark=SILVER_DK):
    """The silver disc of the lid (and the belt's buckle): a flat drum along n with a darker ring engraved on its face."""
    c = np.array(c, float); n = unit(*n)
    k.add(name, lathe(c, n, [(0, -thick / 2), (r, -thick / 2), (r * 1.0, thick * 0.3), (r * 0.9, thick / 2),
                             (0, thick / 2)], seg=48), face, bone, fuse=False, surface="metal")
    k.add(name + "ring", lathe(c + n * thick * 0.5, n, [(r * 0.42, -0.002), (r * 0.66, -0.002), (r * 0.66, 0.0022),
                                                         (r * 0.42, 0.0022), (r * 0.42, -0.002)], seg=48),
          mark, bone, fuse=False, surface="metal")


def ribbon(path, width, thick, up=(0, 0, 1.0)):
    """A flat strap along a path of points, width across it (square to the path and to up)."""
    P = np.array(path, float)
    L = np.r_[0, np.cumsum(np.linalg.norm(np.diff(P, axis=0), axis=1))]
    L /= L[-1]
    T = np.gradient(P, axis=0); T /= np.linalg.norm(T, axis=1, keepdims=True)
    S = np.cross(T, np.array(up, float)); S /= np.linalg.norm(S, axis=1, keepdims=True)
    def at(u, v):
        x = np.stack([np.interp(u, L, P[:, i]) for i in range(3)], axis=-1)
        s = np.stack([np.interp(u, L, S[:, i]) for i in range(3)], axis=-1)
        return x + s * ((v - 0.5) * width)[..., None]
    return outward(sheet(at, 4 * len(P), 3, thick))


def turn_x(deg):
    t = math.radians(deg)
    return np.array([[1, 0, 0], [0, math.cos(t), -math.sin(t)], [0, math.sin(t), math.cos(t)]])


def d_profile(scale=1.0, steps=14):
    """The lid's end: a flat bottom, straight walls, an arch over them (y, z), the hinge edge at (LID_D, 0)."""
    pts = [(-LID_D, 0.0), (LID_D, 0.0), (LID_D, LID_WALL)]
    for i in range(1, steps):
        t = math.pi * i / steps
        pts.append((LID_D * math.cos(t), LID_WALL + LID_ARCH * math.sin(t)))
    pts.append((-LID_D, LID_WALL))
    mid = np.array([0.0, (LID_WALL + LID_ARCH) * 0.42])
    return [tuple(mid + (np.array(p) - mid) * scale) for p in pts]


def lid(k, bone="jaw"):
    """The lid, built closed in its own frame (y back to the hinge at LID_D, z up from its bottom), then tipped open
    about its hinge and set floating at HINGE."""
    m = turn_x(LID_OPEN)
    def place(p):
        p = np.array(p, float)
        return HINGE + m @ (p - np.array([0, LID_D, 0]))
    def prism(prof, x0, x1):
        return hull([place((x, y, z)) for x in (x0, x1) for y, z in prof])
    k.add("lidcore", prism(d_profile(1.0), -LID_W + 0.022, LID_W - 0.022), RED, bone, fuse=False, surface="shell")
    for s in (-1, 1):
        x0, x1 = sorted((s * (LID_W - 0.026), s * LID_W))
        k.add(f"lidend{s}", prism(d_profile(1.07), x0, x1), GOLD, bone, fuse=False, surface="metal")
        # the red D panel on the end, standing just proud of the gold
        prof = d_profile(0.66)
        origin = place((s * (LID_W + 0.002), 0, 0))
        ey, ez = m @ np.array([0, 1.0, 0]), m @ np.array([0, 0, 1.0])
        k.add(f"lidpanel{s}", slab([(y, z) for y, z in prof], origin, ey, ez, 0.006), RED, bone, fuse=False,
              surface="shell")
    # gold rims along the lower edges, front and back
    for y in (-LID_D, LID_D):
        k.add(f"lidrim{y:.2f}", prism([(y - 0.014, -0.004), (y + 0.014, -0.004), (y + 0.014, 0.024), (y - 0.014, 0.024)],
                                      -LID_W + 0.002, LID_W - 0.002), GOLD, bone, fuse=False, surface="metal")
    # dark under it
    k.add("lidunder", prism([(-LID_D + 0.012, -0.006), (LID_D - 0.012, -0.006), (LID_D - 0.012, -0.001),
                             (-LID_D + 0.012, -0.001)], -LID_W + 0.024, LID_W - 0.024), UNDER, bone, fuse=False,
          surface="shell")
    # the black strap from the back over the top and down the front
    k.add("lidstrap", prism(d_profile(1.055), -0.032, 0.032), BLACK, bone, fuse=False, surface="leather")
    # the silver disc on the front, over the strap
    front = place((0, -LID_D * 1.055 - 0.006, LID_WALL * 0.9 + 0.024))
    disc(k, "liddisc", front, m @ np.array([0, -1.0, 0.0]), 0.05, bone)


def coins(k, c, r, n, seed, bone="body", size=0.018):
    """Loose coins lying tilted over a heap (centre c, radii r): thick flat drums, a few of them."""
    rs = np.random.RandomState(seed)
    for i in range(n):
        a = 2 * math.pi * (i + rs.rand() * 0.6) / n
        k_ = 0.35 + 0.5 * rs.rand()
        d = np.array([math.cos(a) * k_, math.sin(a) * k_, 0.0])
        d[2] = math.sqrt(max(1 - d[0] ** 2 - d[1] ** 2, 0.05))
        p = c + d * r * 0.97
        nrm = unit(*(d / r ** 2)) + unit(rs.randn(), rs.randn(), rs.randn()) * 0.5
        nrm = unit(*nrm)
        k.add(f"coin{seed}_{i}", lathe(p, nrm, [(0, -0.0035), (size, -0.0035), (size, 0.0035), (size * 0.72, 0.0045),
                                                 (0, 0.0045)], seg=24), COIN, bone, fuse=False, surface="metal")


def on_ell(c, r, d):
    d = unit(*d)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    nn = (p - c) / r ** 2
    return p, nn / np.linalg.norm(nn)


def ghost_eye(u, v):
    """A yellow disc ringed in black, a black pupil in its middle."""
    c = np.tile(srgb(INK), (len(u), 1))
    rr = u * u + v * v
    c[rr < 0.74 ** 2] = srgb(YELLOW)
    c[rr < 0.34 ** 2] = srgb(INK)
    return c


EYE_D, EYE_HALF = (0.42, -1.0, 0.12), (0.0165, 0.0165)


def lay_eyes(k, state):
    p, n = on_ell(HEAD_C, HEAD_R, EYE_D)
    k.decal(p, n, EYE_HALF, eye_look(state, ghost_eye, srgb(GHOST), srgb(INK), line=0.17), up=(0, 0, 1), eps=0.0015,
            rings=20, segs=56, mirror=True)


def gimmighoul(src, out_path):
    k = Kit(src, coat="shell")
    # --- the box, heaped with coins, its strap out from under its front ---------------------------------------------
    chest(k, (0, 0, 0.0), BOX_W, BOX_D, BOX_H, BAR, "body")
    heap_c, heap_r = np.array([0.0, -0.002, 0.142]), np.array([0.112, 0.08, 0.034])
    k.add("heap", ell(heap_c, heap_r, deform=lumpy(7, 5, 0.12)), COIN, "body", surface="metal")
    k.paint(lambda P: inside(np.abs(np.sin(P[:, 0] * 160) * np.sin(P[:, 1] * 150)) - 0.25, 0.08)
            * (P[:, 2] > 0.13) * inside(np.abs(P[:, 0]) - 0.13), COIN_DK, on=["heap"])
    coins(k, heap_c, heap_r, 9, 3)
    k.add("strap", ribbon([(0, -0.05, 0.012), (0, -0.1, 0.007), (0, -0.15, 0.0045), (0.01, -0.2, 0.0045)], 0.05, 0.009),
          BLACK, "body", fuse=False, surface="leather")

    # --- the ghost peeking out ----------------------------------------------------------------------------------------
    k.add("ghost", ell(HEAD_C, HEAD_R), GHOST, "head", surface="skin")
    k.add("ghostneck", ell(HEAD_C - (0, -0.004, 0.034), (0.03, 0.028, 0.026)), GHOST, "head", surface="skin")
    k.add("arm", tube([(0.028, -0.035, 0.158), (0.062, -0.055, 0.19), (0.082, -0.066, 0.214)], [0.013, 0.0125, 0.0125]),
          GHOST, "arm{s}", mirror=True, surface="skin")
    k.add("hand", ell((0.088, -0.068, 0.221), (0.017, 0.016, 0.017)), GHOST, "arm{s}", mirror=True, surface="skin")
    # the feelers, black, ending in yellow balls
    for name, pts, ball in (("feelerL", [(-0.012, -0.03, 0.228), (-0.026, -0.028, 0.252), (-0.044, -0.024, 0.27)],
                             (-0.049, -0.023, 0.276)),
                            ("feelerR", [(0.014, -0.028, 0.226), (0.034, -0.026, 0.244), (0.056, -0.022, 0.254)],
                             (0.062, -0.021, 0.257))):
        k.add(name, tube(pts, [0.006, 0.006, 0.006]), INK, "head", fuse=False)
        k.add(name + "ball", outward(ell(ball, (0.011, 0.011, 0.011), sub=3)), YELLOW, "head", fuse=False)

    # --- the lid floating open over it ----------------------------------------------------------------------------
    lid(k)

    k.build(voxel=VOXEL, smooth=4, band=0.004, wband=0.03)
    k.eye_states(lambda state: lay_eyes(k, state))
    k.export(out_path)


if __name__ == "__main__":
    src, out_path = args()
    gimmighoul(src, out_path)
