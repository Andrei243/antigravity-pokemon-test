# Belugrand sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# morunel.py's frame (its fish turned YAW about the vertical, both sides by hand, its eyes, plates and board check):
#   blender --background --factory-startup --python sculpts/belugrand.py -- <the export's Belugrand.glb> <out.glb>
#
# From its picture (No. 10116, the great river sturgeon): a colossal deep-bodied fish, slate-grey above and pale
# blue-grey below, a great blunt head almost half its length with thick pale lips round a wide mouth that runs back
# under the eye, a fringe of long wavy barbels hanging from its chin, a tiny old eye under a heavy lid; a crown of big
# rugged stone plates on its head, a row of stone plates down its back and a row of larger ones along each flank,
# shrinking toward the tail; rough rocky fins (a big pectoral pair, a pelvic pair, a small dorsal) and a shark's tail
# whose long upper lobe rises behind, its edges ragged like rock.
# Joints: body (0, 0.05, 0.345), head (0, -0.24, 0.345), tail (0, 0.4, 0.365), finR/L (+-0.17, -0.12, 0.215).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, tube, ell, hull, slab, srgb, inside, unit, eye_look, args, sibling

mo = sibling("morunel")
mo.LIFT = 0.0
W, L, turned, mirrored, add2, decal2, line_at, fish_tube = (mo.W, mo.L, mo.turned, mo.mirrored, mo.add2, mo.decal2,
                                                            mo.line_at, mo.fish_tube)

BACK, SLATE, PALE, ROCK, ROCKD, FIN, BARBEL, LIP, MOUTH, INK = ("#43566A", "#56697D", "#A8BAC7", "#8E8C7A",
                                                                "#6B6A5E", "#5A5C56", "#7E887C", "#B6C5D0",
                                                                "#4A3840", "#16181E")

# The head: a great blunt ellipsoid; the body a tube behind it (y, centre height, half depth), off the picture
HC, HR = np.array([0.0, -0.235, 0.33]), np.array([0.235, 0.24, 0.255])
LINE = [(-0.25, 0.335, 0.255), (-0.064, 0.342, 0.272), (0.121, 0.342, 0.268), (0.292, 0.345, 0.228),
        (0.438, 0.365, 0.155), (0.543, 0.378, 0.098), (0.6, 0.4, 0.08)]


def width(y):
    return np.interp(y, [-0.3, 0.1, 0.45, 0.7], [0.9, 0.84, 0.8, 0.78])


def on_head(th, z, out=0.0):
    """The head's surface at an angle th round its front (0 straight ahead, + toward +X) and a height z."""
    k = math.sqrt(max(1 - ((z - HC[2]) / HR[2]) ** 2, 0.0))
    p = np.array([(HR[0] + out) * math.sin(th) * k, HC[1] - (HR[1] + out) * math.cos(th) * k, z])
    n = unit(p[0] / HR[0] ** 2, (p[1] - HC[1]) / HR[1] ** 2, (p[2] - HC[2]) / HR[2] ** 2)
    return p, n


def rock(c, n, a, b, h, seed, lean=(0, 0.35, 0)):
    """A rugged stone plate on a surface at c facing n: a base of half sizes a, b, h high, its top leaning back."""
    rs = np.random.RandomState(seed)
    n = unit(*n)
    t1 = np.cross(n, (0, 1.0, 0))
    if np.linalg.norm(t1) < 1e-3:
        t1 = np.array([1.0, 0, 0])
    t1 /= np.linalg.norm(t1); t2 = np.cross(n, t1)
    c = np.asarray(c, float) - n * h * 0.25
    pts = []
    for i in range(7):
        t = 2 * math.pi * i / 7 + rs.uniform(-0.25, 0.25)
        k = rs.uniform(0.8, 1.1)
        pts.append(c + (t1 * a * math.cos(t) + t2 * b * math.sin(t)) * k)
    tip = c + n * h + np.asarray(lean, float) * h
    for i in range(3):
        t = 2 * math.pi * i / 3 + rs.uniform(0, 1)
        pts.append(c + n * h * rs.uniform(0.7, 0.85) + np.asarray(lean, float) * h * 0.6
                   + (t1 * a * math.cos(t) + t2 * b * math.sin(t)) * 0.62)
    pts.append(tip)
    return hull(pts)


def body(k):
    k.add("head", turned(ell(HC, HR)), SLATE, "head")
    k.add("trunk", turned(fish_tube([p for p in LINE if p[0] <= 0.44], width)), SLATE, "body")
    k.add("stalk", turned(fish_tube([p for p in LINE if p[0] >= 0.29], width)), SLATE, "tail")
    k.add("lobe", turned(tube([(0, 0.58, 0.41), (0, 0.64, 0.5), (0, 0.7, 0.6), (0, 0.735, 0.66)],
                              [0.06, 0.04, 0.025, 0.014], seg=14)), SLATE, "tail")


def at_line(P):
    """Centre height and half depth for any point, the head's included."""
    y = P[:, 1]
    zc, r = line_at(LINE, y)
    return zc, r


ZM = 0.272                      # the mouth's height at its middle


def markings(k):
    def dark(P):
        P = L(P)
        zc, r = at_line(P)
        return inside(zc + 0.5 * r - P[:, 2], 0.004)
    k.paint(dark, BACK)

    def belly(P):
        P = L(P)
        zc, r = at_line(P)
        wave = 0.07 * np.sin(P[:, 1] * 14 + 0.8)
        line = zc - (0.28 + wave) * r
        # the pale runs forward under the eye to the lips
        line = np.where(P[:, 1] < -0.12, np.maximum(line, ZM + 0.02 + 0.06 * np.clip((P[:, 1] + 0.12) / 0.2, 0, 1)),
                        line)
        return inside(P[:, 2] - line, 0.004)
    k.paint(belly, PALE)


def lips(k):
    arc = np.radians(np.linspace(-112, 112, 23))
    def line(dz, out, taper):
        pts, rad = [], []
        for th in arc:
            f = abs(th) / math.radians(112)
            p, _ = on_head(th, ZM + dz * (1 - 0.35 * f * f) - 0.012 * f * f, out)
            pts.append(p); rad.append(taper * (1 - 0.45 * f ** 3))
        return tube(pts, rad, seg=14, per=4)
    k.add("mouth", turned(line(0.0, -0.004, 0.017)), MOUTH, "head", fuse=False, surface="skin")
    k.add("liptop", turned(line(0.03, -0.008, 0.026)), LIP, "head", fuse=False, surface="skin")
    k.add("lipbot", turned(line(-0.032, -0.008, 0.027)), LIP, "head", fuse=False, surface="skin")


def barbels(k):
    # a fringe of long wavy barbels hanging from the chin, down and a little back
    for i, th in enumerate((10, 32, 56, 80)):
        for s in (1, -1):
            p, n = on_head(math.radians(th) * s, ZM - 0.075, -0.012)
            d = 0.095 + 0.025 * math.cos(i * 1.7)
            wob = 0.012 * s
            pts = [p, p + (wob, 0.012, -d * 0.35), p + (-wob * 0.5, 0.03, -d * 0.7), p + (wob, 0.05, -d)]
            k.add(f"barbel{i}{'R' if s > 0 else 'L'}", turned(tube(pts, [0.012, 0.011, 0.0095, 0.008], seg=10)),
                  BARBEL, "head", fuse=False, surface="skin")


def plates(k):
    m = dict(fuse=False, surface="shell")
    # the crown: big rugged plates standing up on the head, the tallest in the middle
    crown = [(0.0, -0.29, 0.12, 0.07), (0.085, -0.33, 0.095, 0.058), (-0.085, -0.33, 0.095, 0.058),
             (0.065, -0.22, 0.11, 0.065), (-0.065, -0.22, 0.11, 0.065), (0.0, -0.15, 0.09, 0.06),
             (0.14, -0.26, 0.075, 0.05), (-0.14, -0.26, 0.075, 0.05), (0.12, -0.16, 0.07, 0.048),
             (-0.12, -0.16, 0.07, 0.048), (0.0, -0.39, 0.075, 0.05)]
    for i, (x, y, h, a) in enumerate(crown):
        q = 1 - (x / HR[0]) ** 2 - ((y - HC[1]) / HR[1]) ** 2
        z = HC[2] + HR[2] * math.sqrt(max(q, 0))
        n = unit(x / HR[0] ** 2 * 0.6, (y - HC[1]) / HR[1] ** 2 * 0.4, (z - HC[2]) / HR[2] ** 2)
        k.add(f"crown{i}", turned(rock((x, y, z), n, a, a * 0.8, h, 10 + i)), ROCK if i % 3 else ROCKD, "head", **m)
    # down the back
    for i in range(8):
        y = -0.06 + 0.075 * i
        zc, r = line_at(LINE, y)
        s = 1 - 0.09 * i
        k.add(f"back{i}", turned(rock((0, y, zc + r), (0, 0.2, 1), 0.055 * s, 0.045 * s, 0.075 * s, 40 + i)),
              ROCK if i % 2 else ROCKD, "body" if y < 0.4 else "tail", **m)
    # along each flank, high, the largest behind the head
    for i in range(7):
        y = -0.02 + 0.075 * i
        zc, r = line_at(LINE, y)
        s = 1 - 0.1 * i
        ang = math.radians(48)
        x = r * width(np.array([y]))[0] * math.cos(ang)
        z = zc + r * math.sin(ang)
        n = unit(math.cos(ang) / width(np.array([y]))[0], 0.05, math.sin(ang))
        add2(k, f"side{i}", rock((x, y, z), n, 0.068 * s, 0.052 * s, 0.05 * s, 60 + i), ROCK if i % 2 else ROCKD,
             "body" if y < 0.4 else "tail", **m)
    # a lower row, smaller, along the middle of the flank
    for i in range(6):
        y = 0.03 + 0.08 * i
        zc, r = line_at(LINE, y)
        s = 1 - 0.11 * i
        x = r * width(np.array([y]))[0]
        add2(k, f"low{i}", rock((x, y, zc + 0.05 * r), (1, 0.05, 0.05), 0.045 * s, 0.035 * s, 0.035 * s, 80 + i),
             ROCKD, "body" if y < 0.4 else "tail", **m)


def jag(points, amp, seed):
    """An outline with a ragged edge: a small zigzag pushed out along each side."""
    rs = np.random.RandomState(seed)
    p = np.array(points, float)
    out = []
    for a, b in zip(p, np.roll(p, -1, axis=0)):
        out.append(a)
        e = b - a
        nrm = np.array([e[1], -e[0]]) / (np.linalg.norm(e) + 1e-9)
        for t in (0.33, 0.66):
            out.append(a + e * t + nrm * amp * rs.uniform(-0.5, 1.0))
    return out


def fins(k):
    m = dict(fuse=False, surface="shell")
    pec = jag([(0.0, -0.09), (0.12, -0.12), (0.3, -0.04), (0.32, 0.03), (0.16, 0.09), (0.0, 0.09)], 0.016, 1)
    add2(k, "pec", slab(pec, (0.17, -0.1, 0.25), unit(0.55, 0.75, -0.2), (0, -0.7, 0.7), 0.028), FIN, "fin{s}", **m)
    pel = jag([(0.0, -0.045), (0.06, -0.06), (0.13, 0.0), (0.06, 0.04), (0.0, 0.045)], 0.01, 2)
    add2(k, "pel", slab(pel, (0.1, 0.33, 0.2), unit(0.55, 0.65, -0.4), (0, -1, 0.15), 0.018), FIN, "tail", **m)
    zc, r = line_at(LINE, 0.46)
    dor = jag([(-0.04, 0.0), (0.05, 0.0), (0.07, 0.08), (0.03, 0.085)], 0.008, 3)
    k.add("dorsal", turned(slab(dor, (0, 0.47, zc + r - 0.02), (0, 1, 0), unit(0.35, 0, 1), 0.02)), FIN, "tail", **m)
    tail = [(0.56, 0.4), (0.6, 0.48), (0.68, 0.6), (0.74, 0.69), (0.735, 0.64), (0.745, 0.6), (0.72, 0.55),
            (0.7, 0.47), (0.69, 0.38), (0.71, 0.3), (0.68, 0.29), (0.62, 0.34)]
    tail = jag(tail, 0.01, 4)
    k.add("caudal", turned(slab(tail, (0, 0, 0), (0, 1, 0), (0, 0, 1), 0.024)), FIN, "tail", **m)


# --- the eyes: tiny and old, an amber-brown iris under a heavy lid that cuts its top third ----------------------------
CI, CS, CIR, CLID, CG = srgb(INK), srgb("#E6E2D2"), srgb("#6A5A38"), srgb(SLATE), srgb("#FFFFFF")


def eye_open(u, v):
    r = np.sqrt(u * u + v * v)
    c = np.tile(CS, (len(u), 1))
    c[r > 0.86] = CI
    pr = np.sqrt((u + 0.1) ** 2 + (v + 0.05) ** 2)
    c[pr < 0.6] = CIR
    c[pr < 0.34] = CI
    c[((u + 0.25) ** 2 + (v - 0.08) ** 2) < 0.12 ** 2] = CG
    lid = 0.3 - 0.12 * u                       # the heavy lid, a little lower toward the tail
    c[v > lid] = CLID
    c[np.abs(v - lid) < 0.12] = CI
    return c


EYE_TH, EYE_Z, EYE_HALF = math.radians(56), 0.395, (0.034, 0.031)


def eyes(k, state):
    look = eye_look(state, eye_open, CLID, CI, line=0.16)
    p, n = on_head(EYE_TH, EYE_Z)
    decal2(k, p, n, EYE_HALF, look, eps=0.002, rings=20, segs=56)


def nostrils(k):
    p, n = on_head(math.radians(22), 0.375)
    decal2(k, p, n, (0.011, 0.008), "#26303A", eps=0.0025, rings=6, segs=24)


def belugrand(src, out):
    k = Kit(src, coat="scales")
    body(k)
    markings(k)
    lips(k)
    barbels(k)
    plates(k)
    fins(k)
    nostrils(k)
    k.build(voxel=0.0072, smooth=4, band=0.006, wband=0.06)
    mo.board(k)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    belugrand(*args())
