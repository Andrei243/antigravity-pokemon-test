# Clobbopus sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/clobbopus.py -- <the export's Clobbopus.glb> <out.glb>
#
# From the references: a little octopus that is nearly all head: a great round cream head, a little wider than tall,
# sitting low on a short fringe of cream tentacle tips (navy under their ends). Across its face an orange mask like a
# pair of goggles, a little peak at the top of its middle; in it light blue eyes with a dark rim and a dark bar of a
# pupil across, half asleep. A crown of orange spots round the top of the head; a small orange ring with a dark middle
# on each side (an ear). Two cream arms from the head's lower sides ending in great navy fists like boxing gloves, half
# the head's width, each with a round orange pad on its front, a mauve cuff at the wrist and an orange spot under it.
# (The art raises one fist; in the rest pose both are held forward and low before the body.) No head bone: the head
# is the body.
# Skeleton: body (0, 0, 0.12); arms (+-0.08, -0.02, 0.12); legs (+-0.04, -0.04, 0.05), hinds (+-0.04, 0.04, 0.05).
#
# Written to be grown from (grapploct.py): the mask is painted in coordinates round the head's axis, the eyes, the
# suckers and pads, the fringe of tentacles and the arms are functions.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, polygon, outward, ring_band, args

CREAM, ORANGE, GLOWO, NAVY, MAUVE = "#F2E8DC", "#EC6E3A", "#F4A27E", "#36345E", "#9A6A8A"
EYEB, EYERIM, PUPIL, INK = "#9CDDEE", "#2A4C86", "#1C2244", "#1A1A2E"

HC, HR = np.array([0.0, 0.0, 0.156]), np.array([0.122, 0.108, 0.104])


def oriented(c, r, n, sub=3):
    """An ellipsoid of radii r whose local z is the direction n (a pad, a sucker)."""
    n = np.array(n, float); n /= np.linalg.norm(n)
    a = np.cross(n, (0, 0, 1.0)) if abs(n[2]) < 0.95 else np.cross(n, (1.0, 0, 0))
    a /= np.linalg.norm(a); b = np.cross(n, a)
    V, F = ell((0, 0, 0), r, sub=sub)
    return outward((V @ np.stack([a, b, n], axis=0) + np.array(c, float), F))


def on_ell(c, r, d):
    c, r, d = np.array(c, float), np.array(r, float), unit(*d)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def az_el(az, el):
    """A way out of the head: az degrees round from the front toward +X, el degrees up."""
    a, e = math.radians(az), math.radians(el)
    return (math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e))


# --- a polygon painted in coordinates round the head's upright axis (as luxio.py's) --------------------------------
def in_poly(x, y, pts):
    pts = np.array(pts, float)
    res = np.zeros(len(x), bool)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        res ^= ((y1 > y) != (y2 > y)) & (x < (x2 - x1) * (y - y1) / (y2 - y1 + 1e-12) + x1)
    return res


def poly_mask(x, y, pts, edge=0.0025):
    pts = np.array(pts, float)
    d = np.full(len(x), 9.0)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        ex, ey = x2 - x1, y2 - y1
        t = np.clip(((x - x1) * ex + (y - y1) * ey) / (ex * ex + ey * ey + 1e-12), 0, 1)
        d = np.minimum(d, np.hypot(x - x1 - t * ex, y - y1 - t * ey))
    return inside(np.where(in_poly(x, y, pts), -d, d), edge)


def mirrored(half):
    """A half outline from the middle's top out and round to the middle's bottom (s >= 0), made whole."""
    return half + [(-a, z) for a, z in half[::-1][1:-1]]


def round_head(P, hc, radius):
    """Points as (arc along the head's front at the given radius, height)."""
    return np.arctan2(P[:, 0], -(P[:, 1] - hc[1])) * radius, P[:, 2]


def mask_paint(k, hc, radius, half, color, edge=0.0022, front=1.4, grow=0.0):
    pts = mirrored([(a * (1 + grow) if a else 0.0, hc[2] + (z - hc[2]) * (1 + grow)) for a, z in half])
    def m(P):
        s, z = round_head(P, hc, radius)
        ang = np.abs(np.arctan2(P[:, 0], -(P[:, 1] - hc[1])))
        return poly_mask(s, z, pts, edge=edge) * (ang < front) * (P[:, 1] < hc[1] + 0.02)
    k.paint(m, color)


# The mask as (arc, height): a peak at the top of the middle, rising at the outer corners, curving under the eyes
MASK = [(0.0, 0.212), (0.011, 0.212), (0.02, 0.196), (0.046, 0.194), (0.074, 0.2), (0.092, 0.188), (0.1, 0.16),
        (0.093, 0.128), (0.072, 0.116), (0.044, 0.12), (0.018, 0.13), (0.0, 0.134)]
EYE_S, EYE_Z, EYE_HALF = 0.047, 0.158, (0.025, 0.024)


def eye_open(u, v):
    c = np.tile(srgb(EYEB), (len(u), 1))
    r = np.sqrt(u * u + v * v)
    c[r > 0.8] = srgb(EYERIM)
    return c


def eyes(k, hc, hr, s_, z_, half, lid, look_open=eye_open, eps=0.0022, bar=(0.56, 0.2), bar_color=PUPIL):
    """The eyes, and the pupil a bar across each (a patch of its own, so its edge is clean)."""
    def lay(state):
        look = eye_look(state, look_open, srgb(lid), srgb(INK), line=0.16)
        az = math.degrees(s_ / hr[0])
        el = math.degrees(math.asin(np.clip((z_ - hc[2]) / hr[2], -1, 1)))
        p, n = on_ell(hc, hr, az_el(az, el))
        k.decal(p, n, half, look, eps=eps, rings=20, segs=56, mirror=True)
        if state in ("Open", "Fierce") and bar:
            low = 0.04 if state == "Open" else 0.32
            k.decal(p - np.array((0, 0, half[1] * low)), n, (half[0] * bar[0], half[1] * bar[1]), bar_color,
                    eps=eps * 1.8, rings=6, segs=36, mirror=True)
    return lay


def ring_look(ring, middle, ground, inner=0.42, outer=0.92):
    R, M, G = srgb(ring), srgb(middle), srgb(ground)
    def look(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(G, (len(u), 1))
        c[r < outer] = R
        c[r < inner] = M
        return c
    return look


def fringe(k, n, c, r0, r1, z0, z1, rad, color, tip_color, tip_z, bones):
    """A skirt of n short tentacles round the base, their ends darker underneath."""
    k.add("skirt", ell(c, (r0 * 1.35, r0 * 1.35, (z0 - z1) * 0.9)), color, "body")
    for i in range(n):
        a = 2 * math.pi * (i + 0.5) / n
        d = np.array([math.sin(a), -math.cos(a), 0.0])
        bone = bones(d)
        pts = [c + d * r0 * 0.6 + np.array((0, 0, z0 - c[2])), c + d * r0 + np.array((0, 0, (z0 + z1) / 2 - c[2])),
               c + d * r1 + np.array((0, 0, z1 - c[2]))]
        pts = [np.array([p[0], p[1], p[2]]) for p in pts]
        k.add(f"lobe{i}", tube(pts, [rad, rad * 0.95, rad * 0.75], seg=16, per=6), color, bone)
    k.paint(lambda P: inside(P[:, 2] - tip_z, 0.0015) * (np.hypot(P[:, 0] - c[0], P[:, 1] - c[1]) > r0 * 0.7),
            tip_color, on=["lobe"])


def quadrant(d):
    return ("legR" if d[1] < 0 else "hindR") if d[0] > 0 else ("legL" if d[1] < 0 else "hindL")


def arms(k):
    """Cream arms down and forward to great navy gloves held low before the body."""
    sh = np.array([0.082, -0.02, 0.112])
    wrist = np.array([0.122, -0.09, 0.072])
    gc = np.array([0.13, -0.122, 0.058])
    k.add("arm", tube([sh, (0.11, -0.055, 0.088), wrist], [0.03, 0.026, 0.024], seg=20, per=6), CREAM, "arm{s}",
          mirror=True)
    k.add("glove", ell(gc, (0.054, 0.052, 0.052), sub=4), NAVY, "arm{s}", mirror=True)
    # the mauve cuff at the wrist, a loose band
    d = gc - wrist; d /= np.linalg.norm(d)
    k.add("cuff", ring_band(wrist + d * 0.004, d, 0.0255, 0.014, thick=0.006), MAUVE, "arm{s}", fuse=False,
          mirror=True)
    # the round orange pad on the glove's front, and an orange spot under the wrist
    nd = unit(0.3, -0.9, 0.2)
    p = gc + nd * 0.05
    k.add("pad", oriented(p, (0.031, 0.031, 0.011), nd), ORANGE, "arm{s}", fuse=False, mirror=True)
    nu = unit(0.25, -0.35, -0.9)
    q = np.array([0.112, -0.068, 0.08]) + nu * 0.02
    k.add("spot", oriented(q, (0.012, 0.012, 0.004), nu), ORANGE, "arm{s}", fuse=False, mirror=True)


def marks(k):
    # the orange mask, with a paler glow round it
    mask_paint(k, HC, HR[0], MASK, GLOWO, edge=0.004, grow=0.05)
    mask_paint(k, HC, HR[0], MASK, ORANGE, edge=0.0016)
    # a crown of orange spots round the top of the head
    for az, el, h in ((22, 63, 0.016), (-22, 63, 0.016), (68, 50, 0.015), (-68, 50, 0.015), (128, 56, 0.015),
                      (-128, 56, 0.015), (0, 84, 0.017)):
        p, n = on_ell(HC, HR, az_el(az, el))
        up = (0, 1.0, 0) if el > 75 else (0, 0, 1.0)
        k.decal(p, n, (h, h), GLOWO if False else ORANGE, up=up, eps=0.0018, rings=8, segs=36)
    # the ears: a small orange ring with a dark middle on each side
    p, n = on_ell(HC, HR, az_el(96, -4))
    k.decal(p, n, (0.016, 0.016), ring_look(ORANGE, PUPIL, CREAM), eps=0.0018, rings=12, segs=44, mirror=True)


def clobbopus(src, out_path):
    k = Kit(src, coat="fur")
    k.add("head", ell(HC, HR), CREAM, "body")
    fringe(k, 8, np.array([0.0, 0.0, 0.06]), 0.06, 0.1, 0.055, 0.014, 0.021, CREAM, NAVY, 0.012, quadrant)
    arms(k)
    marks(k)
    k.build(voxel=0.0028, smooth=4, band=0.004, wband=0.03)
    k.eye_states(eyes(k, HC, HR, EYE_S, EYE_Z, EYE_HALF, ORANGE))
    k.export(out_path)


if __name__ == "__main__":
    clobbopus(*args())
