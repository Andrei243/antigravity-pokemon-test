# Mega Chimecho sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/chimechomega.py -- <the export's Chimecho-Mega.glb> <out.glb>
#
# Grown from chimecho.py (copied: that script builds as it is read). From the references: a great wind chime. Its
# largest mass a golden ring standing in the front plane (it floats, as the game's own sculpt does), open at the
# bottom, with a golden bar across it a little above its middle that runs through two small golden rings, and a
# flared golden horn out of each side; on top a golden knob, and from it two great red clubs of arms rising up and
# out in a V, banded white twice, each ending in three fingers (the middle one white). Chimecho itself, small, hangs
# from the top of the ring in its middle: its pale blue head wider than tall with red marks on its sides, small yellow
# eyes and a shallow smile, a little nub of an arm each side, and below it a long pale blue ribbon whose lower part is
# red with a ragged edge. Six little chimes hang below the bar's rings and the horns: pale blue domes banded with a
# zigzag (red on the outer two, teal on the inner four) over short ribbons, red on the outer two, teal and green on
# the inner. The skeleton: body (ring's middle) (0, 0, 0.615), head (0, 0, 0.875), bows (+-0.03, 0, 0.935), tail
# (0, -0.02, 0.655).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, ring_band, outward, srgb, inside, unit, eye_look, tri, args

HEAD, RED, INK = "#B9DDEF", "#DD4458", "#2C2A36"
GOLD, BOW, WHITE = "#EBC659", "#E5394F", "#F1F1F4"
TEAL, GREEN, ZIGIN = "#2E78A8", "#5BA548", "#3A9AA0"

C = np.array([0.0, 0.0, 0.615])                      # the ring's middle
RING_R = 0.3
GAP = 13                                             # half the opening at the bottom, degrees


def ring_pt(deg, r=RING_R, y=0.0):
    """A point of the ring at an angle from its top, clockwise seen from the front (+X first)."""
    a = math.radians(deg)
    return C + np.array([r * math.sin(a), y, r * math.cos(a)])


def gold_ring(k):
    degs = np.linspace(-(180 - GAP), 180 - GAP, 73)
    pts = [ring_pt(d) for d in degs]
    radii = [0.014 + 0.012 * min(1.0, (180 - abs(d)) / 70) for d in degs]
    k.add("ring", outward(tube(pts, radii, seg=20)), GOLD, "body", fuse=False, surface="metal")
    # the bar across, a little above the middle, and the two small rings it runs through
    zb = C[2] + 0.03
    k.add("bar", outward(tube([(-0.29, 0.012, zb), (0.29, 0.012, zb)], [0.017, 0.017], seg=16)), GOLD, "body",
          fuse=False, surface="metal")
    for s in (1, -1):
        cc = np.array([s * 0.16, 0.0, zb])
        pts = [cc + 0.065 * np.array([math.cos(a), 0, math.sin(a)]) for a in np.linspace(0, 2 * math.pi, 41)]
        k.add(f"loop{'R' if s > 0 else 'L'}", outward(tube(pts, [0.017] * len(pts), seg=16)), GOLD, "body",
              fuse=False, surface="metal")
        # a flared horn out of the ring's side, its end a rounded lobe hanging a little
        h0 = ring_pt(s * 78, RING_R - 0.01)
        k.add(f"horn{'R' if s > 0 else 'L'}",
              outward(tube([h0, h0 + np.array([s * 0.05, 0, 0.0]), h0 + np.array([s * 0.09, 0, -0.005]),
                            h0 + np.array([s * 0.115, 0, -0.012])], [0.024, 0.022, 0.032, 0.04], seg=20)),
              GOLD, "body", fuse=False, surface="metal")
        k.add(f"lobe{'R' if s > 0 else 'L'}", ell(h0 + np.array([s * 0.13, 0, -0.016]), (0.042, 0.034, 0.04),
                                                 rot=(0, s * -15, 0)), GOLD, "body", fuse=False, surface="metal")
    k.add("knob", ell(C + np.array([0, 0, RING_R + 0.018]), (0.05, 0.04, 0.034)), GOLD, "body", fuse=False,
          surface="metal")


def bows(k):
    """The two great red clubs rising up and out in a V from the knob, banded white, three fingers at each end."""
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        bone = f"bow{side}"
        d = unit(s * 0.82, 0.0, 0.57)
        p = unit(-s * 0.57, 0.0, 0.82)                    # across the club, toward its top side
        r0 = C + np.array([s * 0.03, 0, RING_R + 0.025])
        L = 0.33
        def rad(t):
            return 0.03 + 0.055 * t ** 0.7
        ts = np.linspace(0, 1, 7)
        k.add(f"club{side}", outward(tube([r0 + d * L * t for t in ts], [rad(t) for t in ts], seg=24)), BOW, bone,
              fuse=False, surface="shell")
        for t0 in (0.38, 0.72):
            k.add(f"band{side}{t0}", ring_band(r0 + d * L * t0, d, lambda v, t0=t0: rad(t0 - 0.06 + 0.12 * v) + 0.004,
                                              L * 0.12, thick=0.007), WHITE, bone, fuse=False, surface="shell")
        E = r0 + d * L
        k.add(f"fist{side}", ell(E + d * 0.012, (0.086, 0.074, 0.08)), BOW, bone, fuse=False, surface="shell")
        for j, (along, across, ln, r, col) in enumerate(((0.035, 0.035, 0.075, 0.032, BOW),
                                                         (-0.005, 0.06, 0.07, 0.028, WHITE),
                                                         (-0.045, 0.065, 0.055, 0.024, BOW))):
            b = E + d * along + p * across
            w = unit(*(p * 0.85 + d * 0.5))
            k.add(f"finger{side}{j}", outward(tube([b, b + w * ln * 0.6, b + w * ln], [r, r * 0.95, r * 0.75],
                                                   seg=16)), col, bone, fuse=False, surface="shell")


def ribbon(k, name, top, bottom, half_top, half_bottom, lower, lower_from, bone, y=0.0, thick=0.014, rag=6):
    """A flat ribbon facing the front, pale blue over a coloured lower part with a ragged top edge."""
    x0 = top[0]
    blue = [(x0 - half_top, top[2]), (x0 + half_top, top[2]), (x0 + half_bottom, bottom[2] - 0.006),
            (x0 - half_bottom * 1.1, bottom[2] + 0.006)]
    k.add(name, slab(blue, (0, y, 0), (1, 0, 0), (0, 0, 1), thick), HEAD, bone, fuse=False, surface="cloth")
    def hw(z):
        t = (top[2] - z) / (top[2] - bottom[2])
        return half_top + (half_bottom - half_top) * t + 0.003
    zf = lower_from
    edge = []
    for i in range(rag + 1):
        x = -1 + 2 * i / rag
        z = zf + (0.018 if i % 2 else 0.0) * (top[2] - bottom[2]) / 0.28
        edge.append((x0 + x * hw(z), z))
    red = [(x0 - half_bottom * 1.1 - 0.003, bottom[2] + 0.004), (x0 + half_bottom + 0.003, bottom[2] - 0.008)]
    red += edge[::-1]
    k.add(name + "low", slab(red, (0, y, 0), (1, 0, 0), (0, 0, 1), thick + 0.007), lower, bone, fuse=False,
          surface="cloth")


def chimes(k):
    """Six little chimes: a pale blue dome banded with a zigzag over a short ribbon."""
    zb = C[2] + 0.03
    places = []
    for s in (1, -1):
        for dx, col in ((0.115, TEAL), (0.2, GREEN)):
            x = s * (0.16 + (dx - 0.16))
            z = zb - math.sqrt(max(0.065 ** 2 - (x - s * 0.16) ** 2, 0)) - 0.03
            places.append((x, z, col, ZIGIN))
        h0 = ring_pt(s * 78, RING_R - 0.01)
        places.append((h0[0] + s * 0.13, h0[2] - 0.075, RED, RED))
    for j, (x, z, col, zig) in enumerate(places):
        c = np.array([x, 0.0, z])
        k.add(f"dome{j}", ell(c, (0.03, 0.026, 0.028)), HEAD, "body")
        def zag(P, c=c):
            q = P - c
            a = np.arctan2(q[:, 0], -q[:, 1])
            return inside(np.abs(q[:, 2] - 0.002 - 0.006 * tri(a * 3 / math.pi)) - 0.0055, 0.0015) \
                * (np.linalg.norm(q, axis=1) < 0.045)
        k.paint(zag, zig, on=[f"dome{j}"])
        ribbon(k, f"strip{j}", (x, 0, z - 0.02), (x, 0, z - 0.15), 0.019, 0.022, col, z - 0.095, "body",
               thick=0.012, rag=4)


def chimecho_mega(src, out):
    k = Kit(src, coat="metal")
    gold_ring(k)
    bows(k)

    # --- Chimecho itself, hanging from the top of the ring -----------------------------------------------------
    HC = np.array([0.0, -0.01, 0.8])
    HR = np.array([0.068, 0.058, 0.048])
    k.add("hanger", outward(tube([C + np.array([0, 0, RING_R - 0.01]), HC + np.array([0, 0, 0.03])], [0.014, 0.014],
                                 seg=12)), GOLD, "head", fuse=False, surface="metal")

    def flatbottom(V):
        V = V.copy()
        V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.8, V[:, 2])
        return V
    k.add("head", ell(HC, HR, deform=flatbottom), HEAD, "head")
    k.add("nub", ell((0.066, -0.025, 0.775), (0.022, 0.016, 0.015), rot=(0, 34, 0)), HEAD, "head", mirror=True)
    for s in (1, -1):
        k.paint(lambda P, s=s: inside(np.linalg.norm((P - (HC + np.array([s * 0.06, 0.0, 0.006])))
                                                     / np.array([0.018, 0.03, 0.016]), axis=1) - 1, 0.003),
                RED, on=["head"])
    k.add("neck", tube([(0, -0.01, 0.765), (0, -0.01, 0.72)], [0.012, 0.016]), HEAD, "tail")
    ribbon(k, "tail", (0, 0, 0.725), (0, 0, 0.44), 0.03, 0.047, RED, 0.56, "tail", y=-0.01, thick=0.016, rag=6)

    chimes(k)

    # --- the face: small round yellow eyes in a dark rim, a shallow smile ------------------------------------
    CY, CI, CH = srgb("#F2D24A"), srgb(INK), srgb(HEAD)

    def eye(u, v):
        c = np.tile(CY, (len(u), 1))
        c[u * u + v * v > 0.6] = CI
        c[((u - 0.2) / 0.22) ** 2 + ((v - 0.25) / 0.22) ** 2 < 1] = srgb("#FFF4C0")
        return c

    def smile(u, v):
        c = np.tile(CH, (len(u), 1))
        line = -0.15 + 0.35 * u * u
        c[(np.abs(v - line) < 0.2) & (np.abs(u) < 0.92)] = CI
        return c
    md = unit(0, -1, -0.15)
    k.decal(HC + md * HR, md, (0.036, 0.013), smile, eps=0.003, rings=10, segs=48)

    def eyes(state):
        for s in (1, -1):
            dn = unit(s * 0.42, -0.86, 0.3)
            e = HC + dn * HR
            look = eye_look(state, eye, CH, CI, line=0.18)
            lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
            k.decal(e, dn, (0.016, 0.016), lk, eps=0.003, rings=20, segs=56)

    k.build(voxel=0.0035, smooth=3)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    chimecho_mega(*args())
