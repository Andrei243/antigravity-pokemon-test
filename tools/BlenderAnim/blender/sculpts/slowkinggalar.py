# Galarian Slowking sculpted from nothing on the skeleton and clips of the game's own export of the form
# (sculptkit.py), grown from slowking.py (its pear of a body, thick legs and broad feet, the ruff of wedges, the
# claws) and slowpoke.py (the line's pink and claws).
#   blender --background --factory-startup --python sculpts/slowkinggalar.py -- <the export's Slowking-Galar.glb> <out.glb>
#
# From the references: the Shellder has swallowed the head. A great grey-violet hood, the largest mass after the
# body, comes down over the whole upper head to the mouth: darker spots, a knobbed crest up its top, two thick horns
# out of its sides, a green gem in a dark socket on its front, and across its front a dark purple band in which the
# eyes look out, narrow, half-lidded and sly under a heavy black lash line; white shell teeth along the hood's lower
# rim bite over a wide-open red mouth; the lower jaw pale violet. A cape of dark purple panels lies over the
# shoulders, wider than the body. The chest and arms are violet, the hips and legs pink, the belly khaki ringed with
# brown lines; white claws; a violet tail out to the +X side with a few small spikes on its top.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, sheet, srgb, inside, unit, outward, eye_look, args, sibling

sp = sibling("slowpoke")
PINK = sp.PINK
VIOLET, LILAC = "#8D74C8", "#B7A6DE"
HOOD, HOOD_SPOT, BAND, LASH = "#9B8CBE", "#6F5E9E", "#4A3A76", "#141018"
MOUTH, TONGUE, TEETH = "#B8343E", "#E07884", "#F6F4F0"
SOCKET, GEM = "#2E2448", "#46C24E"
CAPE_A, CAPE_B = "#43336E", "#5A4890"
BELLY, RIB = "#E0DCAE", "#B9AE80"

# --- the frame ------------------------------------------------------------------------------------------------------
HOOD_C, HOOD_R = np.array([0.0, -0.02, 0.695]), np.array([0.245, 0.218, 0.168])
HOOD_LOW = 0.85                          # the hood's lower half squashed: a mushroom, flat under
FACE_C, FACE_R = np.array([0.0, -0.05, 0.54]), np.array([0.155, 0.14, 0.095])
EYE_Z, EYE_X = 0.703, 0.088
EYE_HALF = (0.057, 0.037)
CAPE_C = np.array([0.0, -0.02, 0.5])
CAPE_IN, CAPE_OUT, CAPE_DROP, CAPE_N = 0.12, 0.28, 0.12, 14


FLARE = 0.16                             # the lower half widening toward its rim


def flare(uz):
    return 1 + FLARE * np.clip(-uz, 0, 0.6)


def low_squash(V):
    V = V.copy()
    f = flare(V[:, 2])
    V[:, 0] *= f; V[:, 1] *= f
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * HOOD_LOW, V[:, 2])
    return V


def on_hood(x, z, front=True):
    """The point of the hood's front (or back) surface at x and height z, with its outward normal."""
    zr = HOOD_R[2] * (HOOD_LOW if z < HOOD_C[2] else 1)
    uz = (z - HOOD_C[2]) / zr
    fl = float(flare(np.array(uz)))
    a, b = HOOD_R[0] * fl, HOOD_R[1] * fl
    q = 1 - (x / a) ** 2 - uz ** 2
    y = HOOD_C[1] + (-1 if front else 1) * b * math.sqrt(max(q, 0.0))
    p = np.array([x, y, z])
    n = unit(x / a ** 2, (y - HOOD_C[1]) / b ** 2, (z - HOOD_C[2]) / zr ** 2)
    return p, n


def hood(k):
    """The Shellder over the head: the hood with its crest, spots and band, horns, gem and teeth."""
    k.add("hood", ell(HOOD_C, HOOD_R, deform=low_squash), HOOD, "head", surface="shell")
    k.add("dome", ell(HOOD_C + (0, 0.02, 0.08), (0.18, 0.17, 0.11)), HOOD, "head", surface="shell")
    # the knobbed crest up the top, front to back
    for i, (y, z, r) in enumerate([(-0.07, 0.855, 0.045), (0.0, 0.885, 0.05), (0.07, 0.86, 0.042), (0.12, 0.815, 0.035)]):
        k.add(f"knob{i}", ell((0.0, y, z), (r * 0.9, r, r * 1.05)), HOOD, "head", surface="shell")
    for sx in (1, -1):
        k.add(f"knobside{'RL'[sx < 0]}", ell((0.07 * sx, -0.02, 0.845), (0.034, 0.036, 0.034)), HOOD, "head",
              surface="shell")
    # the dark band across the front, in which the eyes look out
    k.paint(lambda P: inside(np.abs(P[:, 2] - EYE_Z) - 0.045, 0.0025) * inside(P[:, 1] + 0.05, 0.004)
            * inside(np.abs(P[:, 0]) - 0.235, 0.004), BAND, on=["hood"])
    # darker spots over the top and sides, kept off the band and the front
    rs = np.random.RandomState(172)
    for i in range(30):
        q = rs.normal(size=3); q /= np.linalg.norm(q)
        if q[2] < 0.1 or (q[1] < -0.35 and q[2] < 0.55):
            continue
        p = HOOD_C + q * HOOD_R * np.array([1, 1, 1.0]) + np.array([0, 0.0, 0.03])
        s = rs.uniform(0.016, 0.026)
        k.paint(lambda P, p=p, s=s: inside(np.linalg.norm(P - p, axis=1) - s, 0.003), HOOD_SPOT, on=["hood", "dome"])
    # two thick horns out of the sides, curving out and up
    for sx in (1, -1):
        P = [(0.2 * sx, -0.06, 0.705), (0.275 * sx, -0.08, 0.74), (0.32 * sx, -0.09, 0.79), (0.335 * sx, -0.085, 0.84)]
        k.add(f"horn{'RL'[sx < 0]}", outward(tube(P, [0.058, 0.046, 0.03, 0.016], seg=18, per=6)), HOOD, "head",
              fuse=False, surface="shell")
    # the green gem in its dark socket over the band
    gp, gn = on_hood(0.0, 0.79)
    k.add("socket", outward(ell(gp - gn * 0.006, (0.04, 0.02, 0.036), rot=(-25, 0, 0))), SOCKET, "head", fuse=False,
          surface="shell")
    k.add("gem", outward(ell(gp + gn * 0.006, (0.027, 0.02, 0.025), rot=(-25, 0, 0))), GEM, "head", fuse=False,
          surface="shell", glow=GEM)
    # white shell teeth along the hood's lower front rim, biting down over the mouth
    for i, x in enumerate((-0.062, 0.0, 0.062)):
        z = 0.59
        p, n = on_hood(x, z)
        c = p + n * 0.004 + np.array([0, 0, -0.018])
        k.add(f"tooth{i}", outward(ell(c, (0.025, 0.015, 0.03), rot=(0, 0, math.degrees(math.atan2(n[0], -n[1]))))),
              TEETH, "head", fuse=False, surface="shell")


def face(k):
    """The pale violet face under the hood: the head's lower part and the jaw, the wide open mouth."""
    k.add("face", ell(FACE_C, FACE_R), LILAC, "head")
    k.add("jaw", ell((0, -0.075, 0.49), (0.13, 0.115, 0.06)), LILAC, "head")
    # behind the jaw the neck is violet, as the chest is
    k.paint(lambda P: inside(-0.01 - P[:, 1], 0.004) * inside(P[:, 2] - 0.66, 0.004), VIOLET, on=["face", "jaw"])
    c = FACE_C + np.array([0, -FACE_R[1] * 0.98, 0.0])
    def mouth(u, v):
        col = np.tile(srgb(MOUTH), (len(u), 1))
        col[(u / 0.62) ** 2 + ((v + 0.62) / 0.42) ** 2 < 1] = srgb(TONGUE)
        return col
    k.decal(c + np.array([0, 0, 0.012]), unit(0, -1, -0.12), (0.1, 0.048), mouth, eps=0.003, rings=14, segs=48)


def sly_eye(u, v):
    """A narrow sly eye (u outward, v up): white under a heavy black lash that droops to the outer corner, a black
    pupil looking in; the band's purple round it."""
    c = np.tile(srgb(BAND), (len(u), 1))
    lid = 0.3 - 0.28 * u
    white = (u * u / 0.9 + (v + 0.1) ** 2 / 0.72 < 1) & (v < lid)
    c[white] = srgb("#F7F5F0")
    c[white & ((u + 0.2) ** 2 + (v + 0.12) ** 2 < 0.16)] = srgb(LASH)
    lash = (np.abs(v - lid) < 0.2) & (u * u / 0.95 + (v + 0.05) ** 2 / 0.9 < 1.12)
    c[lash] = srgb(LASH)
    c[(u > 0.62) & (np.abs(v - (lid + 0.12 * (u - 0.62) / 0.4)) < 0.17) & (u < 0.98)] = srgb(LASH)
    return c


def eyes(k, state):
    look = eye_look(state, sly_eye, srgb(BAND), srgb(LASH), line=0.18)
    for sx in (1, -1):
        p, n = on_hood(EYE_X * sx, EYE_Z)
        col = look if sx > 0 else (lambda u, v, look=look: look(-u, v))
        k.decal(p, n, EYE_HALF, col, eps=0.0025, rings=20, segs=56)


def cape(k):
    """The cape of dark panels over the shoulders: wedges edge to edge, flaring out and hanging down, open in front."""
    step = 2 * math.pi / CAPE_N
    for i in range(CAPE_N):
        a0 = -math.pi / 2 + (i + 0.5) * step
        if abs(((a0 + step / 2) - (-math.pi / 2) + math.pi) % (2 * math.pi) - math.pi) < 0.35:
            continue                               # the front, where the violet chest shows
        dark = i % 2 == 0
        grow = 1.02 if dark else 1.0
        def at(u, v, a0=a0, grow=grow):
            uu = 0.015 + 0.97 * u
            th = a0 + step * uu
            r = CAPE_IN + (CAPE_OUT * grow - CAPE_IN) * v
            x, y = r * np.cos(th), r * np.sin(th) * 0.9
            z = CAPE_C[2] - CAPE_DROP * v ** 1.3 * (1 + 0.6 * (-np.sin(th)).clip(0, 1)) + 0.02 * (1 - v)
            return np.stack([x + CAPE_C[0], y + CAPE_C[1], z], axis=-1)
        k.add(f"cape{i}", outward(sheet(at, 9, 9, 0.022 if dark else 0.019)), CAPE_A if dark else CAPE_B, "body",
              fuse=False, surface="cloth")


def slowkinggalar(src, out):
    k = Kit(src, coat="scales")
    hood(k)
    face(k)
    # The body: a heavy pear, violet above and pink below, the khaki belly ringed with brown
    def pear(V):
        V = V.copy()
        s = 1 + 0.12 * np.clip(-V[:, 2], 0, 1) - 0.08 * np.clip(V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell((0, 0.0, 0.29), (0.19, 0.165, 0.22), deform=pear), PINK, "body")
    k.add("shoulders", ell((0, -0.01, 0.44), (0.15, 0.13, 0.1)), VIOLET, "body")
    k.add("neck", ell((0, -0.03, 0.5), (0.12, 0.11, 0.07)), VIOLET, "body")
    k.paint(lambda P: inside(0.34 + 0.02 * np.cos(P[:, 0] * 30) - P[:, 2], 0.003), VIOLET, on=["body"])
    k.paint(lambda P: inside(((P[:, 0] / 0.13) ** 2 + ((P[:, 2] - 0.2) / 0.13) ** 2 - 1) * 0.06, 0.003)
            * (P[:, 1] < -0.05) * inside(P[:, 2] - 0.33, 0.003), BELLY, on=["body"])
    for z in (0.12, 0.17, 0.22, 0.27):
        k.paint(lambda P, z=z: inside(np.abs(P[:, 2] - z + 0.02 * (P[:, 0] / 0.1) ** 2) - 0.0045, 0.002)
                * inside(((P[:, 0] / 0.13) ** 2 + ((P[:, 2] - 0.2) / 0.13) ** 2 - 1) * 0.06 + 0.004, 0.002)
                * (P[:, 1] < -0.05), RIB, on=["body"])
    # Legs: heavy thighs fused into the body, short shins, broad feet with white claws
    k.add("thigh", ell((0.12, 0.0, 0.16), (0.1, 0.105, 0.11)), PINK, "leg{s}", mirror=True)
    k.add("leg", tube([(0.115, 0.0, 0.16), (0.13, -0.02, 0.06)], [0.082, 0.072]), PINK, "leg{s}", mirror=True)
    k.add("foot", ell((0.135, -0.05, 0.032), (0.074, 0.094, 0.036)), PINK, "leg{s}", mirror=True)
    sp.claws(k, "claw", (0.135, -0.138, 0.026), (0.15, -1, 0), 3, "leg{s}", size=1.4)
    # Arms: violet, short, from under the cape forward and down, white claws
    k.add("arm", tube([(0.14, -0.03, 0.44), (0.2, -0.11, 0.38), (0.17, -0.19, 0.33)], [0.056, 0.05, 0.046]),
          VIOLET, "arm{s}", mirror=True)
    k.add("hand", ell((0.16, -0.21, 0.32), (0.048, 0.046, 0.044)), VIOLET, "arm{s}", mirror=True)
    sp.claws(k, "finger", (0.15, -0.248, 0.315), (-0.3, -1, 0), 2, "arm{s}", size=1.4)
    # The tail: violet, along the ground out to the +X side, its tip lifting, small spikes along its top
    TAIL = [(0, 0.12, 0.22), (0.07, 0.24, 0.12), (0.19, 0.3, 0.08), (0.3, 0.29, 0.1), (0.36, 0.24, 0.17)]
    k.add("tail", tube(TAIL, [0.08, 0.072, 0.058, 0.042, 0.026]), VIOLET, "tail")
    for i, (t, h) in enumerate([(1.3, 0.05), (2.0, 0.042), (2.7, 0.034)]):
        j = int(t); f = t - j
        c = np.array(TAIL[j]) * (1 - f) + np.array(TAIL[j + 1]) * f
        r = 0.072 - 0.016 * t
        base = c + np.array([0, 0, r * 0.7])
        d = unit(*(np.array(TAIL[j + 1]) - np.array(TAIL[j])))
        side = np.cross(d, (0, 0, 1.0)); side /= np.linalg.norm(side)
        w = 0.022
        pts = [base + w * d, base - w * d, base + 0.6 * w * side, base - 0.6 * w * side, base - np.array([0, 0, 0.02]),
               base + np.array([0, 0, h]) - d * 0.012]
        k.add(f"tspike{i}", hull(pts), VIOLET, "tail", fuse=False, surface="scales")
    cape(k)
    k.build(voxel=0.0068, smooth=3, band=0.006)
    k.eye_states(lambda st: eyes(k, st))
    k.export(out)


if __name__ == "__main__":
    slowkinggalar(*args())
