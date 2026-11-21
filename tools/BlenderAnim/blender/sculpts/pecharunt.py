# Pecharunt sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pecharunt.py -- <the export's Pecharunt.glb> <out.glb>
#
# The design, from the references: a small round ghost of dark violet with a little curled tip on its crown, its
# lower body darker spotted and running down into a wisp of a tail that turns magenta-pink; a sly face: white eyes half
# under heavy lids, a magenta mouth with two white fangs, and its hands two pink hearts held up at the sides of the
# mouth. At its sides hang the two halves of its peach shell, the largest masses: thick round discs, near black, each
# with a glowing pink face shaped like a speech bubble (its point to the top, toward the middle), magenta cracks from
# that face to the rim, a pointed ear on the rim's top outer side and a magenta lobe hanging under it. (The task's text
# spoke of a shell worn like a hood or cape round the body: the pictures show the two halves held apart at its sides,
# faces to the front; they are set a little back and turned in so that they frame the body.)
# The skeleton: body (0, 0, 0.217), armR/armL (+-0.05, -0.03, 0.207); the halves and the hearts are the arms'.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, lathe, outward, polygon, slab, hull, \
    axes, args

VIOLET, VIOLET_DARK, MAGENTA, PINK_TAIL = "#4A2A72", "#33204F", "#B52E8A", "#E07AB8"
SHELL, SHELL_DARK, BUBBLE, CRACK = "#2C2830", "#1E1B22", "#F4A8DC", "#C8358F"
HEART, FANG, INK, LID = "#E85BA6", "#F6F2F7", "#160E20", "#7A2C78"
WHITE = np.array([1.0, 1.0, 1.0])

BODY_C, BODY_R = np.array([0.0, 0.0, 0.225]), np.array([0.07, 0.062, 0.066])
EYE_D, EYE_HALF = (0.42, -1.0, 0.3), (0.019, 0.016)
DISC_C = np.array([0.152, 0.03, 0.27])
DISC_N = unit(-0.14, -0.99, 0.0)            # the face's normal: to the front, turned in toward the middle
DISC_R = 0.104
VOXEL = 0.0036


def on_ell(c, r, d):
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def heart_outline(n=40):
    pts = []
    for t in np.linspace(0, 2 * math.pi, n, endpoint=False):
        x = 16 * math.sin(t) ** 3
        y = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
        pts.append((x / 17, y / 17))
    return pts


def bubble_outline(n=48):
    """A round speech bubble whose point is at the top, toward the middle (-a in the plate's frame)."""
    pts = []
    tip_at = math.radians(112)               # just past straight up, toward the middle
    for t in np.linspace(0, 2 * math.pi, n, endpoint=False):
        d = abs((t - tip_at + math.pi) % (2 * math.pi) - math.pi)
        r = 1.0 + 0.42 * max(0.0, 1 - d / 0.42)
        pts.append((r * math.cos(t), r * math.sin(t)))
    return pts


# --- the parts ----------------------------------------------------------------------------------------------------
def body(k):
    k.add("body", ell(BODY_C, BODY_R), VIOLET, "body")
    k.add("belly", ell((0.0, 0.004, 0.17), (0.052, 0.048, 0.05)), VIOLET, "body")
    k.add("tail", tube([(0.0, 0.008, 0.14), (0.014, 0.018, 0.105), (-0.01, 0.028, 0.075), (0.008, 0.04, 0.052),
                        (0.03, 0.05, 0.05)], [0.036, 0.022, 0.015, 0.011, 0.009]), PINK_TAIL, "body")
    k.add("tip", tube([(0.0, 0.006, 0.28), (0.008, 0.01, 0.302), (0.024, 0.014, 0.312), (0.036, 0.012, 0.306)],
                      [0.026, 0.015, 0.01, 0.008]), VIOLET, "body")
    # the tail's magenta, deeper toward its end
    k.paint(lambda P: smoothstep(0.155, 0.11, P[:, 2]) * 0.85, MAGENTA, on=["tail", "belly"])
    k.paint(lambda P: smoothstep(0.1, 0.06, P[:, 2]), PINK_TAIL, on=["tail"])
    # the lower body darker
    k.paint(lambda P: smoothstep(0.2, 0.175, P[:, 2]) * inside(0.15 - P[:, 2], 0.01) * 0.7, VIOLET_DARK,
            on=["belly", "body"])
    # spots on the belly
    for x, z in ((-0.024, 0.168), (0.006, 0.158), (0.03, 0.17), (-0.008, 0.18)):
        p, n = on_ell((0.0, 0.004, 0.17), (0.052, 0.048, 0.05), (x / 0.052, -1.0, (z - 0.17) / 0.05))
        k.decal(p, n, (0.0075, 0.0062), INK, eps=0.0024, rings=6, segs=28)


def face(k):
    # the mouth: a wide magenta band, its top edge straight, its bottom curved, with two white fangs
    def mouth(u, v):
        c = np.tile(srgb(MAGENTA), (len(u), 1))
        return c
    p, n = on_ell(BODY_C, BODY_R, (0.0, -1.0, -0.38))
    k.decal(p, n, (0.03, 0.014), mouth, eps=0.0024, rings=12, segs=60,
            outline=polygon([(-1, 0.6), (1, 0.6), (0.8, -0.2), (0.35, -0.85), (-0.35, -0.85), (-0.8, -0.2)]))
    for s in (1, -1):
        q, m = on_ell(BODY_C, BODY_R, (0.14 * s, -1.0, -0.3))
        k.decal(q, m, (0.0062, 0.0085), FANG, eps=0.004, rings=6, segs=36,
                outline=polygon([(-1, 1), (1, 1), (0.1, -1)]))


def hearts(k):
    pts = heart_outline()
    k.add("heart", slab([(x * 0.024, y * 0.024) for x, y in pts], (0.054, -0.062, 0.186), unit(1, -0.25, 0),
                        (0, 0, 1), 0.014), HEART, "arm{s}", fuse=False, mirror=True, surface="skin")
    k.add("wrist", outward(tube([(0.046, -0.03, 0.2), (0.054, -0.052, 0.19)], [0.0095, 0.0085])), VIOLET,
          "arm{s}", fuse=False, mirror=True, surface="skin")


def halves(k):
    """The two halves of the shell, at its sides: a domed back, the glowing bubble, cracks, an ear and a lobe."""
    n = DISC_N
    prof = [(0.0, -0.05), (0.05, -0.046), (0.083, -0.034), (DISC_R, -0.012), (DISC_R + 0.002, 0.008),
            (DISC_R - 0.006, 0.024), (0.0, 0.026)]
    k.add("half", lathe(DISC_C, n, prof, seg=72), SHELL, "arm{s}", fuse=False, mirror=True)
    up = np.array([0, 0, 1.0])
    ex = np.cross(up, n)                     # in the plate, toward +X's side
    face = DISC_C + n * 0.027
    k.add("bubble", slab([(x * 0.068, y * 0.068) for x, y in bubble_outline()], face, ex, up, 0.006), BUBBLE,
          "arm{s}", fuse=False, mirror=True, glow="#A8407E")
    # cracks from the bubble's edge out to the rim
    for i, a in enumerate((-20, 40, 160, 215, 280)):
        t = math.radians(a)
        d = math.cos(t) * ex + math.sin(t) * up
        side = -math.sin(t) * ex + math.cos(t) * up
        r0, r1, w = 0.072, DISC_R - 0.006, 0.0055
        p0, p1 = face + d * r0, face + d * r1
        kink = face + d * (r0 + r1) / 2 + side * 0.006
        k.add(f"crack{i}", hull([p0 + side * w, p0 - side * w, kink + side * w, kink - side * w,
                                 p0 + side * w + n * 0.006, p0 - side * w + n * 0.006,
                                 kink + side * w + n * 0.006, kink - side * w + n * 0.006]), CRACK, "arm{s}",
              fuse=False, mirror=True)
        k.add(f"crackb{i}", hull([kink + side * w, kink - side * w, p1 + side * w, p1 - side * w,
                                  kink + side * w + n * 0.006, kink - side * w + n * 0.006,
                                  p1 + side * w + n * 0.006, p1 - side * w + n * 0.006]), CRACK, "arm{s}",
              fuse=False, mirror=True)
    # the ear on the rim's top outer side
    t = math.radians(58)
    root = DISC_C + (math.cos(t) * ex + math.sin(t) * up) * (DISC_R - 0.022) - n * 0.012
    tip = root + (math.cos(t) * ex + math.sin(t) * up) * 0.07 + up * 0.012
    a, b = axes(tip - root)
    k.add("ear", hull([root + a * 0.028, root - a * 0.028, root + b * 0.024, root - b * 0.024, tip]), SHELL,
          "arm{s}", fuse=False, mirror=True)
    # the magenta lobe hanging under it
    def drop(V):
        V = V.copy()
        t = np.clip(V[:, 2], -1, 1)
        w = 0.55 + 0.45 * (t + 1) / 2
        V[:, 0] *= w; V[:, 1] *= w
        return V
    k.add("lobe", outward(ell(DISC_C + (0.018, 0.01, -0.118), (0.036, 0.03, 0.05), rot=(0, -14, 0), deform=drop)),
          MAGENTA, "arm{s}", fuse=False, mirror=True)


def eye(u, v):
    """Half under a heavy lid: the white below a straight lid line, a small dark pupil low in it."""
    c = np.tile(WHITE, (len(u), 1))
    lid_line = 0.12 - 0.12 * u
    c[v > lid_line] = srgb(LID)
    c[np.abs(v - lid_line) < 0.14] = srgb(INK)
    c[np.hypot(u - 0.05, v + 0.38) < 0.3] = srgb(INK)
    c[np.hypot(u, v) > 0.84] = srgb(INK)
    return c


def lay_eyes(k, state):
    for s in (1, -1):
        p, n = on_ell(BODY_C, BODY_R, (EYE_D[0] * s, EYE_D[1], EYE_D[2]))
        look = eye_look(state, eye, srgb(VIOLET), srgb(FANG), line=0.17)
        k.decal(p, n, EYE_HALF, look if s > 0 else (lambda u, v, f=look: f(-u, v)), eps=0.0024, rings=20, segs=56)


def pecharunt(src, dst):
    k = Kit(src, coat="shell")
    body(k)
    face(k)
    hearts(k)
    halves(k)
    k.build(voxel=VOXEL, smooth=4, band=0.005, wband=0.03)
    k.eye_states(lambda st: lay_eyes(k, st))
    k.export(dst)


if __name__ == "__main__":
    pecharunt(*args())
