# Galarian Weezing sculpted on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/weezinggalar.py -- "<the export's Weezing-Galar.glb>" <out.glb>
# Grown from weezing.py (sibling("weezing"), and through it koffing.py): its two balls and their studs, its sleepy
# lidded eyes, its skull and crossbones; the colours and the new parts are the form's own.
#
# From the references: Weezing's two balls, the larger on the left (-X), dark grey and studded with small round knobs;
# on each a top hat whose crown runs up into a tall smokestack with a flared lip, the brim curling up at its sides,
# a puff of white smoke at each lip; under the eyes a thick bushy lime-green moustache (on the larger, right across
# the face and curled up at its ends; on the smaller, a ring round its open mouth with a beard under it) and on the
# smaller two green brows; the larger's mouth wide open below its moustache, dark red with a pink tongue, two square
# teeth at its top and a fang rising at each corner; the smaller's mouth open in a sad O; a cream skull ring and
# crossbones on the larger's lower front, the smaller's cream ring peeping under its beard; the lumpy knob on its
# stalk under the smaller; puffs of lime gas round both.
# The skeleton: body (-0.06, 0, 0.323), small (0.13, 0.04, 0.403) (0.049 above Weezing's).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, srgb, smoothstep, unit, lumpy, polygon, outward, sibling, args

wz = sibling("weezing")
kf = wz.kf                                   # the one koffing module weezing's functions read their colours from
GREY, STACK, BORE = "#6E7075", "#7B7D82", "#2A2A2E"
MOSS, MOSSLIGHT, MOSSDARK = "#A9D66A", "#C2E58C", "#8CBF55"
SMOKE, SMOKELIGHT = "#E6E9EC", "#F4F6F7"
MARK = "#E0D29E"
MOUTH, TONGUE, TOOTH = "#6A1A22", "#C25A72", "#F6F4F0"
INK = "#1C1C20"
wz.PURPLE = GREY                             # the lid round the eyes (eyes_turned reads it as it runs)
kf.PURPLE = GREY
kf.INK = INK

DZ = 0.049                                   # this form's joints stand higher than Weezing's
BIG, RB = wz.BIG + np.array([0, 0, DZ]), wz.RB
SMALL, RS = wz.SMALL + np.array([0, 0, DZ]), wz.RS
YAW = wz.YAW_SMALL
rotz = wz.rotz


def studded(k, name, bone, C, R, avoid, n=18, bump=0.17, seed=7):
    """The ball and its small round studs spread evenly over it, none within any of avoid's (direction, clear)."""
    C = np.array(C, float)
    k.add(name, ell(C, (R, R, R), sub=5), GREY, bone)
    rs = np.random.RandomState(seed)
    golden = math.pi * (3 - math.sqrt(5))
    m = 0
    for i in range(n * 3):
        z = 1 - 2 * (i + 0.5) / (n * 3)
        r = math.sqrt(1 - z * z)
        d = np.array([math.cos(golden * i) * r, math.sin(golden * i) * r, z])
        if any(d @ unit(*a) > c for a, c in avoid) or (m % 2 and rs.rand() < 0.5):
            continue
        m += 1
        if m > n:
            break
        s = R * bump * (0.85 + 0.3 * rs.rand())
        a = math.degrees(math.atan2(d[1], d[0])); b = math.degrees(math.acos(np.clip(d[2], -1, 1)))
        k.add(f"{name}stud{m}", ell(C + d * R * 0.98, (s, s, s * 0.8), rot=(0, b, a)), GREY, bone)


def hat(k, name, bone, C, R, top):
    """A top hat whose crown runs up into a smokestack: the brim curling up at its sides, the crown a low round drum,
    the stack straight to a flared lip with a dark bore. Loose, hard-edged pieces."""
    x, y = C[0], C[1]
    zb = C[2] + R * 0.93                    # the brim's underside
    rb, t = R * 0.78, R * 0.13              # the brim's radius and thickness
    V, F = lathe((x, y, zb), (0, 0, 1), [(0, 0), (rb, 0), (rb + t * 0.5, t * 0.5), (rb, t), (0, t)], seg=64)
    rr = np.hypot(V[:, 0] - x, V[:, 1] - y)
    th = np.arctan2(V[:, 1] - y, V[:, 0] - x)
    V = V.copy()
    V[:, 2] += R * 0.12 * np.clip((rr - R * 0.45) / (rb - R * 0.45), 0, 1) ** 2 * np.cos(th) ** 2
    k.add(name + "brim", outward((V, F)), STACK, bone, fuse=False, surface="metal")
    rc, rs_, rl = R * 0.44, R * 0.26, R * 0.33  # crown, stack and lip radii
    z0, z1 = C[2] + R * 0.8, zb + t + R * 0.22
    k.add(name + "stack", lathe((x, y, 0), (0, 0, 1), [
        (0, z0), (rc, z0), (rc * 1.02, zb + t + R * 0.12), (rc * 0.9, z1), (rs_ * 1.15, z1 + R * 0.05),
        (rs_, z1 + R * 0.12), (rs_, top - R * 0.2), (rl, top - R * 0.02), (rl, top), (rl * 0.82, top),
        (rl * 0.78, top - R * 0.12), (0, top - R * 0.12)], seg=48), STACK, bone, fuse=False, surface="metal")
    k.add(name + "bore", lathe((x, y, 0), (0, 0, 1), [(0, top - R * 0.115), (rl * 0.79, top - R * 0.115),
                                                       (rl * 0.79, top - R * 0.085), (0, top - R * 0.085)], seg=40),
          BORE, bone, fuse=False)
    return np.array([x, y, top])


def cloud(k, name, bone, c, size, cols, d=(0.5, 0.0, 0.85), seed=0):
    """A puff of three soft balls, the lightest on top."""
    c = np.array(c, float); d = unit(*d)
    side = np.cross(d, (0, 1.0, 0)); side = side / (np.linalg.norm(side) + 1e-9)
    rs = np.random.RandomState(seed)
    k.add(name, outward(ell(c, (size, size * 0.88, size * 0.92), sub=4)), cols[0], bone, fuse=False)
    k.add(name + "a", outward(ell(c + side * size * 0.72 + d * size * 0.2, (size * 0.6,) * 3, sub=3)), cols[1], bone,
          fuse=False)
    k.add(name + "b", outward(ell(c - side * size * (0.6 + 0.2 * rs.rand()) - d * size * 0.05 + (0, 0.01, 0),
                                  (size * 0.52,) * 3, sub=3)), cols[2], bone, fuse=False)


def smoke(k, name, bone, lip, R, n=3):
    """A short puff of white smoke from the stack's lip, drifting out to +X."""
    for i in range(n):
        s = R * (0.13 + 0.03 * i)
        c = lip + np.array([R * (0.05 + 0.32 * i), 0.0, R * (0.1 + 0.02 * (i % 2))])
        cloud(k, f"{name}{i}", bone, c, s, (SMOKE, SMOKELIGHT, SMOKE), d=(0, 0, 1), seed=i + 5)


def bush(k, name, bone, C, R, path, rad, lift, lumps=2.2):
    """A thick lumpy rope of moss laid along the ball: path is (azimuth, elevation) in degrees about the ball's centre,
    rad its radius; lumps swell it every so often. A loose piece."""
    pts, radii = [], []
    path = np.array(path, float)
    m = len(path)
    fine = np.linspace(0, m - 1, (m - 1) * 3 + 1)
    for j, f in enumerate(fine):
        i = min(int(f), m - 2); t = f - i
        az, el, ex = path[i] + (path[i + 1] - path[i]) * t
        a, e = math.radians(az), math.radians(el)
        d = np.array([math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)])
        pts.append(np.array(C) + d * (R + lift + ex))
        radii.append(rad * (1 + 0.22 * math.cos(j * math.pi * 2 / lumps)))
    k.add(name, outward(tube(pts, radii, seg=18, per=4)), MOSS, bone, fuse=False, surface="fur")


def ring(k, name, bone, C, R, d, wx, wy, rad, lift, top=1.35, bottom=1.2):
    """A closed ring of moss round a point of the ball along d: wx, wy its half sizes in the tangent plane; the top
    (the moustache) and the bottom (the beard) bushier than the sides."""
    at, n, right, up = kf.frame(C, R, d)
    pts, radii = [], []
    N = 30
    for j in range(N + 3):
        a = 2 * math.pi * j / N - math.pi / 2
        q = n + right * math.cos(a) * wx + up * math.sin(a) * wy
        q /= np.linalg.norm(q)
        pts.append(np.array(C) + q * (R + lift))
        s = math.sin(a)
        f = 1 + (top - 1) * max(s, 0) ** 2 + (bottom - 1) * max(-s, 0) ** 2
        radii.append(rad * f * (1 + 0.15 * math.cos(j * 2.1)))
    k.add(name, outward(tube(pts, radii, seg=18, per=4)), MOSS, bone, fuse=False, surface="fur")


def mouth(k, C, R, d, half):
    """The larger's mouth, wide open under the moustache: a flat top hidden under the moss, a round bottom, dark red
    within, a pink tongue below, two square teeth at its top, a fang rising at each corner."""
    at, n, right, up = kf.frame(C, R, d)
    dark, tongue = srgb(MOUTH), srgb(TONGUE)
    def look(u, v):
        c = np.tile(dark, (len(u), 1))
        t = smoothstep(-0.2, -0.45, v + 0.12 * u * u)[:, None]
        return c * (1 - t) + tongue[None] * t
    out = [(1.0, 0.75), (0.0, 0.8), (-1.0, 0.75)] + \
          [(math.cos(a), 0.95 * math.sin(a)) for a in np.linspace(math.pi, 2 * math.pi, 15)[1:-1]]
    k.decal(at, n, half, look, eps=0.004, outline=polygon(out), rings=18, segs=64)
    box = polygon([(-1, 1), (1, 1), (1, -1), (-1, -1)])
    for s in (1, -1):
        p, nn = kf.onto(C, R, at + right * s * half[0] * 0.22 + up * half[1] * 0.42)
        k.decal(p, nn, (half[0] * 0.11, half[1] * 0.2), TOOTH, up=up, eps=0.0065, outline=box, rings=5, segs=32)
        p, nn = kf.onto(C, R, at + right * s * half[0] * 0.72 - up * half[1] * 0.08)
        k.decal(p, nn, (half[0] * 0.11, half[1] * 0.42), TOOTH, up=up, eps=0.0068,
                outline=polygon([(-1, -1), (1, -1), (0, 1)]), rings=6, segs=36)


def small_mouth(k, C, R, d, half):
    """The smaller's mouth open in a sad O: dark red, the tongue's pink low, a little fang at each top corner."""
    at, n, right, up = kf.frame(C, R, d)
    dark, tongue = srgb(MOUTH), srgb(TONGUE)
    def look(u, v):
        c = np.tile(dark, (len(u), 1))
        t = smoothstep(-0.25, -0.5, v)[:, None]
        return c * (1 - t) + tongue[None] * t
    k.decal(at, n, half, look, eps=0.004, rings=14, segs=56)
    for s in (1, -1):
        p, nn = kf.onto(C, R, at + right * s * half[0] * 0.55 + up * half[1] * 0.35)
        k.decal(p, nn, (half[0] * 0.16, half[1] * 0.32), TOOTH, up=up, eps=0.0066,
                outline=polygon([(-1, 1), (1, 1), (0, -1)]), rings=5, segs=30)


def weezing_galar(src, out_path):
    k = Kit(src, coat="shell")
    face_big = (0, -1, 0.15)
    face_small = rotz((0, -1, 0.05), YAW)
    studded(k, "big", "body", BIG, RB, [(face_big, 0.5), ((0, 0, 1), 0.72), (SMALL - BIG, 0.8)], n=22)
    studded(k, "small", "small", SMALL, RS, [(face_small, 0.5), ((0, 0, 1), 0.7), (BIG - SMALL, 0.75)], n=13, seed=3)
    # the brow ridges that make the larger's lids heavy
    for s in (1, -1):
        a, e = math.radians(27 * s), math.radians(48)
        dd = np.array([math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)])
        k.add("brow" + ("R" if s > 0 else "L"), ell(BIG + dd * RB * 0.97, (0.058, 0.022, 0.02),
                                                    rot=(-40, -12 * s, 27 * s)), GREY, "body")
    # the knob under the smaller ball on its short stalk
    k.add("stalk", tube([SMALL + (-0.03, -0.03, -0.1), (0.13, -0.05, 0.215 + DZ)], [0.026, 0.022], seg=14), GREY,
          "small")
    k.add("knob", ell((0.13, -0.055, 0.19 + DZ), (0.042, 0.04, 0.036), deform=lumpy(4, 3, 0.12)), GREY, "small")
    # the hats and their stacks, the smoke at their lips
    lip = hat(k, "big", "body", BIG, RB, 0.915)
    smoke(k, "smokebig", "body", lip, RB)
    lip = hat(k, "small", "small", SMALL, RS, 0.78)
    smoke(k, "smokesmall", "small", lip, RS * 1.3)
    # the larger's moustache, right across the face and curled up at its ends
    bush(k, "moustache", "body", BIG, RB,
         [(-98, 30, 0.014), (-86, 21, 0.004), (-62, 15, 0.0), (-32, 14, 0.0), (0, 15, 0.0), (32, 14, 0.0),
          (58, 15, 0.0), (76, 20, 0.004), (84, 28, 0.012)], 0.029, 0.012, lumps=2.6)
    # the smaller's ring of moss round its mouth and its two green brows
    ring(k, "beard", "small", SMALL, RS, rotz((0, -1, -0.14), YAW), 0.52, 0.3, 0.019, 0.008, top=1.3, bottom=1.25)
    for s in (1, -1):
        b = YAW + 30 * s
        bush(k, "sbrow" + ("R" if s > 0 else "L"), "small", SMALL, RS,
             [(b - 9 * s, 50, 0.0), (b, 48, 0.0), (b + 10 * s, 42, 0.0)], 0.016, 0.006, lumps=30)
    # puffs of lime gas round both
    for i, (c, s, bone) in enumerate([((-0.34, -0.02, 0.6), 0.042, "body"), ((-0.3, -0.04, 0.14), 0.045, "body"),
                                      ((0.04, -0.02, 0.64), 0.036, "body"), ((0.36, 0.02, 0.37), 0.04, "small"),
                                      ((0.1, -0.03, 0.12), 0.036, "small")]):
        cloud(k, f"gas{i}", bone, c, s, (MOSS, MOSSLIGHT, MOSSDARK), seed=i)
    # the larger's mouth and skull, the smaller's O and the cream ring under its beard
    mouth(k, BIG, RB, (0.0, -1, -0.05), (0.112, 0.042))
    kf.skull_mark(k, BIG, RB, (0.0, -0.9, -0.52), 0.15, colour=MARK, hole=GREY)
    small_mouth(k, SMALL, RS, rotz((0, -1, -0.14), YAW), (0.038, 0.024))
    at, n, right, up = kf.frame(SMALL, RS, rotz((0, -1, -0.72), YAW))
    k.decal(at, n, (0.036, 0.02), MARK, eps=0.0045, rings=10, segs=48,
            outline=polygon([(-1, 0.55), (-0.6, -0.15), (0, -0.45), (0.6, -0.15), (1, 0.55), (0.6, 0.4), (0, 0.2),
                             (-0.6, 0.4)]))
    k.build(voxel=0.005, smooth=3)

    def eyes(state):
        wz.eyes_turned(k, state, BIG, RB, (0.36, -0.7, 0.62), (0.058, 0.034), 0, top=0.18, droop=0.26)
        wz.eyes_turned(k, state, SMALL, RS, (0.37, -0.87, 0.5), (0.04, 0.026), YAW, top=0.2, droop=0.3)

    k.eye_states(eyes)
    k.export(out_path)


if __name__ == "__main__":
    weezing_galar(*args())
