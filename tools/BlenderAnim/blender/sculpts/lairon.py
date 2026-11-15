# Lairon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/lairon.py -- <the export's Lairon.glb> <out.glb>
# Grown from aron.py (its steel and slate, its holes in the steel, its blue eyes, its helmet as the fused head). From
# the references: a long low armoured quadruped; the head a great white helmet running forward to a peak over the
# face, holes in its sides, under its rim a slate face with an angry blue eye on each side and the jaws open (pink
# within, white fangs at the corners); three white saddle plates across the back, each with a hard triangular spike
# leaning back and a black hole in each side, a small one behind them over the rump; the slate body between the
# plates banded in lighter grey; four thick slate legs, a white cuff round each and three white spikes at each foot.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, ring_band, srgb, inside, smoothstep, unit, polygon, eye_look, \
    boxy, args, sibling

ar = sibling("aron")
STEEL, DARK, HOLE = ar.STEEL, ar.DARK, ar.HOLE
BAND = "#7E8792"
MOUTH, LINING = "#C2454E", "#E58A98"
INK, WHITE, BLUE, DEEP = ar.INK, ar.WHITE, ar.BLUE, ar.DEEP


def spike(k, name, base, d, length, width, thick, up, color, bone, sink=0.02, mirror=False, surface="metal"):
    """A hard spike or fang of steel: a flat-faced pyramid from base along d, its section a kite wide across `up`."""
    base, d = np.array(base, float), unit(*d)
    up = np.array(up, float); up = up - d * (up @ d); up /= np.linalg.norm(up)
    side = np.cross(d, up)
    b = base - d * sink
    pts = [b + side * width / 2, b - side * width / 2, b + up * thick / 2, b - up * thick / 2,
           base + d * length * 0.45 + side * width * 0.3, base + d * length * 0.45 - side * width * 0.3,
           base + d * length * 0.45 + up * thick * 0.3, base + d * length * 0.45 - up * thick * 0.3,
           base + d * length]
    k.add(name, hull(np.array(pts)), color, bone, fuse=False, mirror=mirror, surface=surface)


def saddle(k, name, cy, y0, y1, rx, rz, a0, bone, color=STEEL):
    """A plate across the back: a hard saddle of steel from y0 to y1, its section half an ellipse about (0, cy) round
    the body (rx wide, rz tall), down each side to the angle a0 from the top."""
    pts = []
    for a in np.linspace(-a0, a0, 11):
        r = math.radians(a)
        for y in (y0, y1):
            w = 1.0 if y == y0 else 0.96
            pts.append((rx * w * math.sin(r), y, cy[1] + rz * w * math.cos(r)))
    k.add(name, hull(pts), color, bone, fuse=False, surface="metal")


def angry_eye(slope=0.45, top=0.42):
    """An angry eye's outline and look: its top a straight line sloping down toward the snout (u forward is -), the
    rest round; blue, a black rim along the outline, a black pupil toward the front."""
    def cut(th):
        s = math.sin(th) - slope * math.cos(th)
        return min(1.0, top / s) if s > 1e-6 else 1.0
    def look(u, v):
        reach = np.array([cut(t) for t in np.arctan2(v, u)])
        rho = np.sqrt(u * u + v * v) / reach
        c = np.tile(INK, (len(u), 1))
        c[rho < 0.76] = BLUE
        c[((u + 0.18) / 0.26) ** 2 + ((v + 0.05) / 0.36) ** 2 < 1] = INK
        c[((u - 0.18) / 0.13) ** 2 + ((v + 0.1) / 0.13) ** 2 < 1] = WHITE
        c[rho >= 0.76] = INK
        return c
    return cut, look


def lairon(src, out):
    k = Kit(src, coat="metal")

    # The body: a long low slate barrel, its belly slung low between the legs
    k.add("body", ell((0, 0.05, 0.18), (0.155, 0.22, 0.135)), DARK, "body")
    k.add("chest", ell((0, -0.1, 0.17), (0.14, 0.11, 0.12)), DARK, "body")
    # The lighter bands round the body between the plates
    for y in (-0.005, 0.135, 0.255):
        k.paint(lambda P, y=y: inside(np.abs(P[:, 1] - y) - 0.012, 0.003) * (P[:, 2] > 0.07), BAND, on=["body"])

    # The legs: thighs fused into the flanks, thick short shins, broad feet; a white cuff, three white spikes a foot
    for name, y, bone in (("leg", -0.1, "leg{s}"), ("hind", 0.14, "hind{s}")):
        k.add(name + "thigh", ell((0.12, y, 0.13), (0.075, 0.09, 0.09)), DARK, bone, mirror=True)
        k.add(name, tube([(0.125, y, 0.13), (0.135, y - 0.005, 0.07)], [0.07, 0.068]), DARK, bone, mirror=True)
        k.add(name + "foot", ell((0.135, y - 0.015, 0.033), (0.075, 0.085, 0.034)), DARK, bone, mirror=True)
        for s, side in ((1, "R"), (-1, "L")):
            m = np.array([s, 1, 1.0])
            k.add(f"{name}cuff{side}", ring_band(np.array((0.133, y - 0.004, 0.07)) * m, (0, 0, 1), 0.071, 0.034,
                                                 thick=0.012), STEEL, bone.format(s=side), fuse=False, surface="metal")
            for j, (dx, dy) in enumerate(((-0.045, -0.06), (0.0, -0.08), (0.045, -0.06))):
                b = np.array((0.135 + dx, y - 0.015 + dy, 0.045)) * m
                spike(k, f"{name}toe{side}{j}", b, np.array((dx * 0.8, dy, -0.18)) * m, 0.042, 0.034, 0.028,
                      (0, 0, 1), STEEL, bone.format(s=side), sink=0.015)

    # The helmet: the head itself, a white mass running forward to a peak over the face, its sides flat
    HC, HR = np.array([0.0, -0.2, 0.265]), np.array([0.145, 0.175, 0.155])
    def hood(V):
        V = boxy(2.6)(V)
        front = np.clip(-V[:, 1], 0, 1)
        top = np.clip(V[:, 2], 0, 1)
        V[:, 1] -= 0.3 * top ** 2                                  # the top drawn forward over the face
        V[:, 2] += 0.3 * top ** 3 + 0.12 * top * front             # rising to a peak
        V[:, 0] *= 1 - 0.32 * np.clip(V[:, 2], 0, 1.4)            # narrowing to a ridge
        V[:, 2] -= 0.18 * front * np.clip(-V[:, 2], 0, 1)          # its front coming down over the face
        return V
    k.add("helmet", ell(HC, HR, rot=(-6, 0, 0), deform=hood), STEEL, "head")
    # The face under its rim: slate, the jaws open
    k.add("face", ell((0, -0.27, 0.155), (0.105, 0.115, 0.08), deform=boxy(2.4)), DARK, "head")
    k.add("jaw", ell((0, -0.29, 0.1), (0.09, 0.1, 0.05), rot=(10, 0, 0)), DARK, "head")
    k.add("mouth", ell((0, -0.39, 0.135), (0.068, 0.09, 0.032), rot=(-6, 0, 0)), MOUTH, "head", carve=True)
    k.paint(lambda P: inside(np.hypot(P[:, 0] / 0.058, (P[:, 2] - 0.135) / 0.026) - 1, 0.003) * (P[:, 1] < -0.33),
            MOUTH)
    k.paint(lambda P: inside(np.hypot(P[:, 0] / 0.045, (P[:, 2] - 0.128) / 0.018) - 1, 0.003) * (P[:, 1] < -0.33)
            * (P[:, 1] > -0.37), LINING)
    # the fangs: two up from the lower jaw's corners, two small down from the upper
    for s in (1, -1):
        spike(k, f"fang{'R' if s > 0 else 'L'}", (0.05 * s, -0.36, 0.105), (0.15 * s, -0.25, 1), 0.035, 0.022, 0.018,
              (0, 1, 0), STEEL, "head", sink=0.01)
        spike(k, f"tooth{'R' if s > 0 else 'L'}", (0.035 * s, -0.375, 0.165), (0, -0.2, -1), 0.022, 0.016, 0.014,
              (0, 1, 0), STEEL, "head", sink=0.008)

    # Holes in the helmet's sides: a big one in the middle, a small one above and behind it, one low in front
    for d, half in (((1, -0.1, 0.12), (0.034, 0.034)), ((0.6, 0.55, 0.7), (0.024, 0.024)),
                    ((0.75, -0.75, -0.2), (0.022, 0.022)), ((0.4, -0.55, 0.85), (0.02, 0.02))):
        p = ar.on_ell(HC, HR, d)
        k.decal(p, unit(*d), half, HOLE, eps=0.004, rings=10, segs=40, mirror=True)

    # The plates across the back, each with a spike leaning back and a black hole in each side
    PLATES = [(-0.04, 0.07, 0.17, 0.14, 0.42), (0.085, 0.19, 0.165, 0.12, 0.38), (0.205, 0.29, 0.155, 0.1, 0.33)]
    for j, (y0, y1, rx, ln, tip) in enumerate(PLATES):
        saddle(k, f"plate{j}", (0, 0.19), y0, y1, rx, rx, 100, "body")
        ym = (y0 + y1) / 2
        top = 0.19 + rx
        spike(k, f"spike{j}", (0, ym, top - 0.01), (0, 0.5, 1), ln, (y1 - y0) * 1.0, 0.05, (1, 0, 0), STEEL, "body",
              sink=0.02)
        if j < 2:
            for s, side in ((1, "R"), (-1, "L")):
                n = unit(s * 0.85, 0, 0.55)
                p = np.array((0, ym, 0.19)) + np.array([s * rx * 0.85, 0, rx * 0.53])
                ar.disc(k, f"platehole{j}{side}", p, n, 0.024, "body")
    # the small one over the rump
    saddle(k, "rump", (0, 0.17), 0.29, 0.34, 0.12, 0.12, 80, "body")
    spike(k, "rumpspike", (0, 0.315, 0.28), (0, 0.8, 0.6), 0.07, 0.05, 0.035, (1, 0, 0), STEEL, "body", sink=0.02)

    # The eyes: angry and blue, on the face's sides just under the helmet's rim
    cut, look = angry_eye()
    de = unit(0.72, -0.62, 0.22)
    E = ar.on_ell((0, -0.27, 0.155), (0.105, 0.115, 0.08), de) + np.array([0, 0, 0.012])
    m = np.array([-1, 1, 1.0])
    def eyes(state):
        lk = eye_look(state, look, srgb(DARK), INK, line=0.16)
        # on a sideways patch +u runs back on both sides: the snout is at -u, as the outline's slope wants
        k.decal(E, de, (0.037, 0.032), lk, eps=0.006, rings=20, segs=56, outline=cut)
        k.decal(E * m, de * m, (0.037, 0.032), lambda u, v: lk(-u, v), eps=0.006, rings=20, segs=56,
                outline=lambda th: cut(math.pi - th))

    k.build(voxel=0.0055, smooth=3, band=0.004)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    lairon(*args())
