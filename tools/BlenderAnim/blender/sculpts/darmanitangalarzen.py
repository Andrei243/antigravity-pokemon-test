# Darmanitan-Galar-Zen sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py), kin of darmanitangalarstandard.py (its white and its beard of lobes) and of darmanitan.py (its
# flame's outline, imported as sibling("darmanitan")).
#   blender --background --factory-startup --python sculpts/darmanitangalarzen.py -- <the export's .glb> <out.glb>
#
# The design, from the references: a snowman of two white balls, the upper nearly as wide as the lower. On the upper
# ball a face of its own drawn in every state: two yellow discs ringed in red-orange and a grey wavy mouth like a
# moustache; from its brow a long flame leaning forward, red at its tips, orange, a pale yellow heart. On the upper
# part of the lower ball the true face: a red mask round two round white eyes with a dark rim and a black pupil, a
# puffy white roll of fur under it, and below that a wide grin drawn as a grey line with a zigzag of teeth; low on the
# lower ball two yellow ovals ringed in red, one over the other; at its sides two short orange hands, thumbs up. No
# legs. (No nose of fire and no flame brow: the pictures show the flame rising from the forehead.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, unit, boxy, eye_look, polygon, args, sibling

dm = sibling("darmanitan")
g = sibling("darumakagalar")
d = g.d
SNOW, SHADE = g.SNOW, g.SHADE
RED, ORANGE, YELLOW, PALE, GREY, WHITE, INK = "#D2483A", "#E8834A", "#F7D867", "#FBF0A0", "#7E8290", "#FFFFFF", "#1E1618"
FLAME, FLAME_MID = "#E4522E", "#F59A35"

LOW_C, LOW_R = np.array([0.0, 0.0, 0.225]), np.array([0.235, 0.215, 0.225])
UP_C, UP_R = np.array([0.0, 0.005, 0.59]), np.array([0.2, 0.185, 0.18])


def ringed(k, c, r, az, z, half, inner, outer, rim=0.3, bone=None):
    """A disc of one colour ringed in another: a patch and a smaller one over it."""
    p, n = d.on_ball(c, r, az, z)
    k.decal(p, n, half, outer, eps=0.004, rings=12, segs=56)
    k.decal(p, n, (half[0] * (1 - rim), half[1] * (1 - rim * half[0] / half[1])), inner, eps=0.0055, rings=12,
            segs=56)


def stroke(k, name, pts_uv, c, r, rad, colour, bone, lift=0.002):
    """A line drawn on a ball as a loose tube lying in its surface: (az, z) points."""
    pts = []
    for az, z in pts_uv:
        p, n = d.on_ball(c, r, az, z)
        pts.append(p + n * lift)
    k.add(name, tube(pts, [rad] * len(pts), seg=12, per=4), colour, bone, fuse=False, surface="skin")


def snow_face(k):
    """The upper ball's own face: ringed yellow discs, a grey wavy mouth."""
    for side in (1, -1):
        ringed(k, UP_C, UP_R, side * 0.45, 0.618, (0.036, 0.04), YELLOW, ORANGE, rim=0.28)
    W = []
    for t in np.linspace(-1, 1, 17):
        az = t * 0.42
        z = 0.548 + 0.016 * math.cos(t * 2 * math.pi) - 0.01 * abs(t) + (0.014 if abs(t) > 0.9 else 0) * (abs(t) - 0.9) / 0.1
        W.append((az, z))
    stroke(k, "mouth", W, UP_C, UP_R, 0.0105, GREY, "head")


def flame(k):
    """A long flame from the brow, leaning forward and up: red, orange, a pale heart."""
    root = np.array([0.0, -0.12, 0.7])
    dirn = unit(0.0, -0.75, 1.0)
    across = unit(1.0, 0.0, 0.0)
    for name, col, L, w, t, back in (("flame", FLAME, 0.26, 0.06, 0.04, 0.0),
                                     ("flamemid", FLAME_MID, 0.17, 0.045, 0.052, 0.006),
                                     ("flameheart", PALE, 0.1, 0.034, 0.062, 0.01)):
        k.add(name, slab(dm.flame_outline(L, w), root + dirn * back, dirn, across, t), col, "head", fuse=False,
              surface="fur", glow=col)
    k.add("flameroot", ell(root + (0, 0.02, -0.01), (0.04, 0.03, 0.03)), PALE, "head", fuse=False, surface="fur")


def true_face(k):
    """The red mask on the lower ball, the roll of fur under it, the grin's line."""
    MASK = [(-0.15, 0.4), (-0.1, 0.42), (-0.05, 0.405), (0.0, 0.392), (0.05, 0.405), (0.1, 0.42), (0.15, 0.4),
            (0.14, 0.352), (0.1, 0.335), (0.05, 0.345), (0.0, 0.332), (-0.05, 0.345), (-0.1, 0.335), (-0.14, 0.352)]
    k.paint(lambda P: d.poly_mask(*d.round_about(P, LOW_C, LOW_R[0]), MASK) * (P[:, 1] < LOW_C[1] - 0.08)
            * inside(P[:, 2] - 0.43), RED, on=["low"])
    # the roll of fur under the mask, in lobes
    k.add("roll", ell((0.0, -0.17, 0.3), (0.15, 0.065, 0.05)), SNOW, "body")
    for i, x in enumerate((-0.12, -0.06, 0.0, 0.06, 0.12)):
        k.add(f"lobe{i}", ell((x, -0.185 + 0.03 * (x / 0.12) ** 2, 0.29 - 0.008 * (i % 2)), (0.05, 0.048, 0.042),
                              sub=3), SNOW, "body")
    # the grin: a grey arc with a zigzag of teeth inside it
    arc = [(t * 0.62, 0.215 - 0.045 * (1 - t * t) + 0.012) for t in np.linspace(-1, 1, 15)]
    stroke(k, "grin", arc, LOW_C, LOW_R, 0.0095, GREY, "body")
    zig = []
    for i in range(11):
        t = -1 + 2 * i / 10
        zig.append((t * 0.5, 0.228 - (0.03 if i % 2 else 0.0) - 0.03 * (1 - t * t) * 0.6))
    stroke(k, "teeth", zig, LOW_C, LOW_R, 0.0085, GREY, "body")


def belly(k):
    for z, w in ((0.125, 0.082), (0.062, 0.07)):
        ringed(k, LOW_C, LOW_R, 0.0, z, (w, 0.024), YELLOW, RED, rim=0.22)


def hands(k):
    """Short orange paddle hands at the sides, thumbs up."""
    HC = np.array([0.255, -0.02, 0.29])
    k.add("hand", ell(HC, (0.04, 0.055, 0.07), deform=boxy(3)), ORANGE, "arm{s}", fuse=False, mirror=True,
          surface="skin")
    k.add("thumb", ell(HC + (-0.004, -0.032, 0.085), (0.024, 0.022, 0.036), deform=boxy(2.6)), ORANGE, "arm{s}",
          fuse=False, mirror=True, surface="skin")
    for i, z in enumerate((-0.025, 0.0, 0.025)):
        k.add(f"groove{i}", ell(HC + (0.034, -0.005, z), (0.012, 0.05, 0.0045)), "#B85A2E", "arm{s}", fuse=False,
              mirror=True, surface="skin")
    k.add("wrist", tube([(0.2, -0.01, 0.29), (0.235, -0.02, 0.29)], [0.04, 0.036]), SNOW, "arm{s}", mirror=True)


def eye_open(u, v):
    r = np.hypot(u, v)
    ring = 1 - inside(r - 0.78, 0.02)
    dot = inside(np.hypot(u + 0.05, v + 0.02) - 0.3, 0.025)
    c = srgb(WHITE)[None] * np.ones((len(u), 1))
    c = c * (1 - ring[:, None]) + srgb(INK)[None] * ring[:, None]
    return c * (1 - dot[:, None]) + srgb(INK)[None] * dot[:, None]


def eyes(k, state):
    look = eye_look(state, eye_open, srgb(RED), srgb(INK), line=0.16)
    for side in (1, -1):
        p, n = d.on_ball(LOW_C, LOW_R, side * 0.33, 0.373)
        f = look if side > 0 else (lambda u, v, L=look: L(-u, v))
        k.decal(p, n, (0.032, 0.032), f, eps=0.0035, rings=20, segs=56)


def darmanitan_galar_zen(src, out):
    k = Kit(src, coat="fur")
    k.add("low", ell(LOW_C, LOW_R), SNOW, "body")
    k.add("up", ell(UP_C, UP_R), SNOW, "head")
    k.paint(lambda P: 0.6 * inside(P[:, 2] - 0.06, 0.04), SHADE, on=["low"])
    true_face(k)
    hands(k)
    snow_face(k)
    belly(k)
    flame(k)
    k.build(voxel=0.0062, smooth=3, band=0.006)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    darmanitan_galar_zen(*args())
