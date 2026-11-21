# Accelgor sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/accelgor.py -- <the export's Accelgor.glb> <out.glb>
#
# From the references: a ninja slug out of Shelmet's shell. A great pink head (half the height) like a tall helmet
# leaning back, a raised spiral curl on each side at the back, a green band running over the crown from side to side
# behind the face, a black four-pointed star on the forehead; under the helmet's front a black mask band, sharp at its
# outer ends, with white eyes and dark pupils glaring out, a round pink nose under it. The body is wrapped in thick
# blue-grey cloth, a cocoon tapering to a ragged point at the ground, its arms folded across the chest, the wrapping's
# seams dark zigzag lines; two thick ribbons of the same cloth stream back from the neck with ragged ends.
#
# Grown from shelmet.py (its pink, green and eye; sibling). Bones: root, body, head, arm{s}, leg{s}.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, outward, hull, sheet, polygon, tri, sibling

sh = sibling("shelmet")
PINK, GREEN = sh.PINK, sh.GREEN
WRAP, WRAPDARK, SEAM = "#6E8AB6", "#5C76A0", "#2C3654"
MASK, WHITE, PUPIL, STAR = "#1C1A22", "#F2F2EA", "#3A3640", "#18161C"

HEAD_C, HEAD_R, HEAD_ROT = np.array([0.0, 0.005, 0.418]), np.array([0.082, 0.088, 0.112]), (-12, 0, 0)


# ---- the body: a wrapped cocoon tapering to a ragged point, arms folded ----------------------------------------------
def taper(V):
    V = V.copy()
    t = np.clip((V[:, 2] + 1) / 2, 0, 1)             # 0 at the bottom, 1 at the top
    s = 0.22 + 0.78 * np.sqrt(t)
    V[:, 0] *= s; V[:, 1] *= s
    return V


def body(k):
    k.add("body", ell((0.0, 0.01, 0.19), (0.09, 0.078, 0.17), rot=(-6, 0, 0), deform=taper), WRAP, "body")
    k.add("chest", ell((0.0, -0.01, 0.29), (0.08, 0.07, 0.05)), WRAP, "body")
    k.add("hips", ell((0.0, 0.01, 0.13), (0.055, 0.05, 0.05)), WRAP, "body")
    # the folded arms: each from its shoulder forward and across under the other
    k.add("armR", tube([(0.06, -0.01, 0.31), (0.075, -0.06, 0.28), (0.02, -0.085, 0.262), (-0.045, -0.07, 0.258)],
                       [0.026, 0.027, 0.026, 0.024], seg=18, per=6), WRAP, "armR")
    k.add("armL", tube([(-0.06, -0.01, 0.31), (-0.075, -0.06, 0.29), (-0.02, -0.088, 0.29), (0.045, -0.074, 0.288)],
                       [0.026, 0.027, 0.026, 0.024], seg=18, per=6), WRAP, "armL")
    # the wrapping's seams: zigzag lines round the body, tilted
    def seams(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        a = np.arctan2(x, -y)
        out = np.zeros(len(P))
        for z0, tilt in ((0.07, 0.25), (0.13, -0.2), (0.19, 0.3), (0.235, -0.15)):
            line = z0 + tilt * y + 0.006 * tri(a * 4 / math.pi)
            out = np.maximum(out, inside(np.abs(z - line) - 0.0022, 0.0012))
        return out * inside(z - 0.25, 0.002)
    k.paint(seams, SEAM)
    def armseams(P):
        x, z = P[:, 0], P[:, 2]
        line = 0.277 + 0.25 * x + 0.004 * tri(x * 40)
        return inside(np.abs(z - line) - 0.002, 0.0012) * inside(P[:, 1] + 0.07, 0.003)
    k.paint(armseams, SEAM, on=["arm"])
    # the ragged end at the bottom: a few pointed tatters
    for i, a in enumerate(np.radians([0, 70, 150, 215, 290])):
        d = np.array([math.sin(a), -math.cos(a), 0.0])
        root = np.array([0.0, 0.03, 0.05]) + d * 0.022
        tip = root + d * (0.04 + 0.01 * (i % 2)) + np.array([0, 0.02, -0.045])
        b = np.cross(d, (0, 0, 1.0))
        pts = [root + b * 0.014 + (0, 0, 0.012), root - b * 0.014 + (0, 0, 0.012), root + b * 0.012 - (0, 0, 0.01),
               root - b * 0.012 - (0, 0, 0.01), root - d * 0.012, tip]
        k.add(f"tatter{i}", hull(pts, merge=0), WRAP, "body", fuse=False, surface="cloth")


# ---- the scarf: two thick ribbons streaming back from the neck ------------------------------------------------------
def ribbon(k, name, root, way, length, width, twist, droop, side):
    root = np.array(root, float); way = unit(*way)
    across0 = unit(0, 0, 1) - way * way[2]; across0 /= np.linalg.norm(across0)
    def at(u, v):
        u = 0.015 + 0.97 * u
        vv = v - 0.5
        # a ragged end: the length at each strip across falls in a zigzag
        L = length * (1 - 0.13 * (1 - np.abs(tri(vv * 2.0))) * 0.5 - 0.06 * np.abs(np.sin(vv * 9)))
        s = u * L
        ang = twist * u
        bend = np.array([0, 0, -1.0])
        pos = root[None, None, :] + way[None, None, :] * s[..., None] + bend * (droop * u ** 2)[..., None] \
            + np.array([side, 0, 0]) * (0.02 * np.sin(u * 3.0))[..., None]
        side_dir = np.cross(way, across0)
        cross = np.cos(ang)[..., None] * across0 + np.sin(ang)[..., None] * side_dir
        w = width * (1 - 0.25 * u)
        return pos + cross * (vv * w)[..., None]
    k.add(name, outward(sheet(at, 30, 7, 0.012)), WRAPDARK if side < 0 else WRAP, "body", fuse=False,
          surface="cloth")


def scarf(k):
    ribbon(k, "scarfR", (0.035, 0.05, 0.32), (0.5, 0.85, 0.05), 0.27, 0.075, 1.3, 0.04, 1)
    ribbon(k, "scarfL", (-0.03, 0.055, 0.3), (0.3, 0.92, -0.14), 0.24, 0.068, -1.1, 0.05, -1)


# ---- the head: a tall pink helmet leaning back ------------------------------------------------------------------------
def head(k):
    k.add("head", ell(HEAD_C, HEAD_R, rot=HEAD_ROT), PINK, "head", surface="skin")
    k.add("nose", ell((0.0, -0.092, 0.334), (0.021, 0.017, 0.018)), PINK, "head", surface="skin")
    # the curls: a raised spiral on each side at the back
    for s in (1, -1):
        t = np.linspace(0, 1, 36)
        th = 1.2 + t * 2.4 * math.pi
        rr = 0.042 * (1 - 0.78 * t)
        cy, cz = HEAD_C[1] + 0.035, HEAD_C[2] + 0.02
        y, z = cy + rr * np.cos(th), cz + rr * np.sin(th)
        q = 1 - ((y - HEAD_C[1]) / HEAD_R[1]) ** 2 - ((z - HEAD_C[2]) / HEAD_R[2]) ** 2
        x = s * (HEAD_R[0] * np.sqrt(np.clip(q, 0.02, 1)) + 0.002)
        k.add(f"curl{'R' if s > 0 else 'L'}", tube(np.stack([x, y, z], 1), np.full(36, 0.0075), seg=12, per=3),
              PINK, "head", surface="skin")
    # the green band over the crown, side to side, behind the face
    def band(P):
        q = P - HEAD_C
        line = 0.0 - 0.15 * q[:, 2]
        return inside(np.abs(q[:, 1] - line) - 0.016, 0.0018) * inside(-(q[:, 2] + 0.02), 0.003) \
            * inside(np.linalg.norm(q / (HEAD_R + 0.02), axis=1) - 1, 0.01)
    k.paint(band, GREEN, on=["head", "curl"])
    # the mask: a black band under the helmet's front, its ends sharp and rising toward the back
    def mask(P):
        q = P - HEAD_C
        a = np.abs(np.arctan2(q[:, 0], -q[:, 1]))
        top = -0.036 + 0.034 * np.clip((a - 0.4) / 1.0, 0, 1)
        bottom = -0.088 + 0.06 * np.clip((a - 0.3) / 1.1, 0, 1) ** 1.3
        return inside(q[:, 2] - top, 0.0015) * inside(bottom - q[:, 2], 0.0015) * inside(a - 1.45, 0.003) \
            * inside(np.linalg.norm(q / (HEAD_R + 0.02), axis=1) - 1, 0.01)
    k.paint(mask, MASK, on=["head"])


STAR_PTS = []
for i in range(8):
    a = math.pi / 2 + i * math.pi / 4
    r = 1.0 if i % 2 == 0 else 0.26
    STAR_PTS.append((r * math.cos(a), r * math.sin(a)))


def star(k):
    d = unit(0, -1, 0.55)
    p = HEAD_C + d / math.sqrt(((d / HEAD_R) ** 2).sum())
    k.decal(p, d, (0.034, 0.034), STAR, outline=polygon(STAR_PTS), up=(0.25, 0, 1), eps=0.003, rings=12, segs=120)


EYE_N = unit(0.6, -1, 0.0)
EYE_C = HEAD_C + np.array([0.0, 0.0, -0.052]) + EYE_N * np.array([0.068, 0.074, 0.0])


def open_eye(u, v):
    c = np.tile(srgb(WHITE), (len(u), 1))
    c[((u + 0.18) / 0.34) ** 2 + ((v + 0.02) / 0.56) ** 2 < 1] = srgb(PUPIL)
    return c


EYE_OUTLINE = [(-1.0, 0.12), (0.9, 0.62), (0.98, -0.25), (0.45, -0.72), (-0.75, -0.5)]   # u outward: inner corner low


def eyes(k):
    """Two patches laid by hand (the outline is not symmetric), u outward on both."""
    def lay(state):
        look = eye_look(state, open_eye, srgb(MASK), srgb(WHITE), line=0.17)
        if state == "Fierce":
            def look(u, v):
                c = open_eye(u, v)
                c[v > 0.3 + 0.35 * u] = srgb(MASK)
                return c
        for s in (1, -1):
            m = np.array([s, 1, 1.0])
            outline = polygon([(a * s, b) for a, b in EYE_OUTLINE])
            colour = look if s > 0 else (lambda u, v, f=look: f(-u, v))
            k.decal(EYE_C * m, EYE_N * m, (0.03, 0.021), colour, outline=outline, eps=0.003, rings=20, segs=56)
    return lay


def accelgor(src, out):
    k = Kit(src, coat="fur")
    body(k)
    head(k)
    scarf(k)
    star(k)
    k.build(voxel=0.0045, smooth=3, band=0.005)
    k.eye_states(eyes(k))
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    accelgor(src, out)
