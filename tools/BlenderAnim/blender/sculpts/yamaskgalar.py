# Yamask-Galar sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/yamaskgalar.py -- <the export's Yamask-Galar.glb> <out.glb>
#
# Yamask's frame (yamask.py: the hood and its crest, the arms and fists, the Q-shaped eye) with Galar's changes. From
# the references: the same small black ghost, but its eyes lilac (a lilac disc ringed darker purple, a small black
# pupil, the Q's tail at the lower outer edge), its arms heavier and its fists larger, and in place of the mask its tail
# runs straight down as a thin stalk into a chunk of a carved clay slab: a pale grey block, wider than tall, its edges
# bevelled, red runes drawn on its front (a spiral, a long upright stroke, a fork with a curl); the tail's tip shows as
# a small black knob under the block. The slab is a hard-edged loose piece; its runes lie on a thin fused plate in its
# front face. Arms carried forward and a little down. The skeleton: body (0, 0, 0.306), head (0, 0, 0.336), arms
# (+-0.03, 0, 0.306), tail (0, 0, 0.276).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, unit, boxy, eye_look, args, sibling

y_ = sibling("yamask")

BLACK, BLACKSOFT = y_.BLACK, y_.BLACKSOFT
LILAC, LILACRIM, PUPIL = "#B678D8", "#6E3290", y_.PUPIL
STONE, RUNE = "#CEC6C2", "#D42E2C"
PLATE = "#BCB4B0"          # the runes' plate a little darker than the block: smooth-shaded, it catches more light

FIST = np.array([0.185, -0.11, 0.245])
SLAB_C = np.array([0.0, -0.01, 0.092])          # the block's middle
SX, SZ, SY = 0.1, 0.062, 0.04                  # its half width, half height, half depth
BEVEL = 0.024
TURN = math.radians(-8)                            # turned a little about the upright


def arms(k):
    """Heavier arms than Yamask's, out from under the hood, forward and a little down, to large curled fists."""
    k.add("arm", tube([(0.045, -0.005, 0.296), (0.1, -0.035, 0.302), (0.148, -0.075, 0.282),
                       FIST + (-0.026, 0.014, 0.006)], [0.024, 0.02, 0.02, 0.023]), BLACK, "arm{s}", mirror=True)
    k.add("fist", ell(FIST, (0.048, 0.045, 0.043)), BLACK, "arm{s}", mirror=True)
    for j, dz in enumerate((0.026, 0.0, -0.026)):
        k.add(f"finger{j}", ell(FIST + (0.014, -0.036, dz), (0.036, 0.024, 0.016), rot=(0, 0, -20)), BLACKSOFT,
              "arm{s}", mirror=True)
    k.add("thumb", ell(FIST + (-0.022, -0.034, -0.036), (0.022, 0.02, 0.018)), BLACK, "arm{s}", mirror=True)


def slab_frame():
    c, s = math.cos(TURN), math.sin(TURN)
    ex = np.array([c, s, 0.0]); ey = np.array([-s, c, 0.0]); ez = np.array([0, 0, 1.0])
    return ex, ey, ez


def stone(k):
    """The chunk of slab: an octagon of a face, wider than tall, its front and back edges bevelled."""
    ex, ey, ez = slab_frame()
    def octagon(sx, sz):
        b = BEVEL * sx / SX
        return [(sx, sz - b), (sx - b, sz), (-sx + b, sz), (-sx, sz - b), (-sx, -sz + b), (-sx + b, -sz),
                (sx - b, -sz), (sx, -sz + b)]
    pts = []
    for y, f in ((-SY, 0.86), (-SY + 0.009, 1.0), (SY - 0.009, 1.0), (SY, 0.86)):
        for a, b in octagon(SX * f, SZ * f):
            pts.append(SLAB_C + ex * a + ey * y + ez * b * (1 if f == 1 else SZ * 0.88 / (SZ * 0.86)))
    k.add("slab", hull(pts), STONE, "tail", fuse=False, surface="shell")


def tail(k):
    """Straight down from under the hood as a thin stalk into the block, its tip a small knob under it."""
    top = SLAB_C[2] + SZ
    k.add("tail", tube([(0.0, 0.015, 0.282), (0.0, 0.006, 0.22), (0.0, -0.004, top - 0.01)], [0.016, 0.013, 0.014]),
          BLACK, "tail")
    k.add("knob", ell((0.0, -0.01, SLAB_C[2] - SZ - 0.006), (0.02, 0.02, 0.016)), BLACK, "tail")
    # the plate in the block's front face that carries the runes, a hair proud of it, the stone's colour
    ex, ey, ez = slab_frame()
    c = SLAB_C + ey * (-SY + 0.0113)
    k.add("face", ell(c, (SX * 0.78, 0.0125, SZ * 0.74), rot=(0, 0, math.degrees(TURN)), deform=boxy(6)), PLATE,
          "tail", surface="shell")


def runes(width=0.13):
    """The red runes in the plate's units (u across, v up): a spiral on the left, a long upright stroke, a fork with
    a curl on the right; width is a stroke's half width in v."""
    st, rd = srgb(PLATE), srgb(RUNE)
    def seg(u, v, a, b):
        p = np.stack([u * 1.7, v], axis=1)
        a = np.array([a[0] * 1.7, a[1]]); b = np.array([b[0] * 1.7, b[1]])
        e = b - a
        t = np.clip(((p - a) @ e) / (e @ e), 0, 1)
        return np.linalg.norm(p - a - t[:, None] * e, axis=1)
    def arc(u, v, c, r, t0, t1):
        x, y = (u - c[0]) * 1.7, v - c[1]
        th = np.arctan2(y, x)
        inside_ = ((th - t0) % (2 * math.pi)) <= ((t1 - t0) % (2 * math.pi))
        d = np.abs(np.hypot(x, y) - r)
        return np.where(inside_, d, 9.0)
    def look(u, v):
        c = np.tile(st, (len(u), 1))
        d = np.minimum.reduce([
            arc(u, v, (-0.5, -0.05), 0.62, -2.4, 2.6),          # the spiral's outer turn
            arc(u, v, (-0.42, -0.0), 0.3, 0.6, 4.6),             # its inner curl
            seg(u, v, (0.02, -0.85), (0.02, 0.85)),              # the long upright stroke
            seg(u, v, (0.04, -0.1), (0.62, 0.7)),                # the fork
            seg(u, v, (0.3, 0.3), (0.74, -0.3)),
            arc(u, v, (0.56, -0.5), 0.26, -1.0, 2.2),            # the curl under the fork
            seg(u, v, (-0.9, -0.8), (-0.2, -0.8)),               # a stroke along the bottom
        ])
        c[d < width] = rd
        return c
    return look


def lilac_eye(pupil=0.3):
    li, rim, pup = srgb(LILAC), srgb(LILACRIM), srgb(PUPIL)
    hi = np.array([1.0, 0.92, 1.0])
    def look(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(li, (len(u), 1))
        c[(r > 0.8) & (r <= 1.02)] = rim
        tl = (u > 0.25) & (v < -0.25) & (r > 0.78)
        c[tl] = li
        c[tl & (r > 1.18)] = rim
        c[(u + 0.06) ** 2 + (v - 0.04) ** 2 < pupil ** 2] = pup
        c[(u + 0.17) ** 2 + (v - 0.17) ** 2 < 0.09 ** 2] = hi
        return c
    return look


def lay_eyes(k, state):
    look = eye_look(state, lilac_eye(), srgb(BLACK), srgb(LILAC), line=0.15)
    for s in (1, -1):
        p, n = y_.on_ell(y_.HOOD_C, y_.HOOD_R, y_.EYE_X * s, y_.EYE_Z)
        d = unit(0.55 * s, -1, 0.08)
        col = look if s == 1 else (lambda u, v, f=look: f(-u, v))
        out = y_.eye_outline if s == 1 else (lambda th: y_.eye_outline(math.pi - th))
        k.decal(p, d, (y_.EYE_HALF, y_.EYE_HALF), col, eps=0.003, outline=out, rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    y_.hood(k)
    arms(k)
    tail(k)
    stone(k)
    ex, ey, ez = slab_frame()
    front = SLAB_C + ey * (-SY - 0.0012)
    k.decal(front, -ey, (SX * 0.74, SZ * 0.7), runes(0.12), eps=0.0015, rings=48, segs=180, surface="shell")
    k.build(voxel=y_.VOXEL, smooth=4, band=0.005)
    k.eye_states(lambda state: lay_eyes(k, state))
    k.export(out)
