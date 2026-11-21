# Stunfisk-Galar sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/stunfiskgalar.py -- <the export's Stunfisk-Galar.glb> <out.glb>
#
# Stunfisk's frame (stunfisk.py: the flat front, the hump, the waist, the side fins, the eyes on the front's slope) made
# a steel trap. From the references: the same flat body lying on the ground, dark grey-green all round and under, its top
# a dull taupe plate sunk inside a raised dark rim, as a trap's jaw; a row of dark triangular teeth standing up along the
# front of the rim, and a tall crown of dark spikes along the back of the hump where Stunfisk's yellow fan was; blotches
# of lime moss on the top (one round the model's left eye, a clover beside the right eye, one on the hump); two round
# white eyes with small black pupils turned in toward the middle; between them a small red mouth with white teeth, its
# lips pursed up; broad dark ribbed fins out of the sides. The joints: body (0, 0, 0.05), tail (0, 0.12, 0.05).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, srgb, inside, unit, eye_look, outward, args, sibling

s_ = sibling("stunfisk")

TOP, RIM, FIN, RIB, MOSS, LIP, TEETH, WHITE, INK = ("#8C8279", "#53564F", "#5B5E57", "#41443E", "#A9D35C",
                                                   "#D8392F", "#F4F2EE", "#F8F8F4", "#16161C")

FRONT_C, FRONT_R = s_.FRONT_C, s_.FRONT_R
HUMP_C, HUMP_R = s_.HUMP_C, s_.HUMP_R
WAIST_C, WAIST_R = np.array([0.0, 0.03, 0.04]), np.array([0.14, 0.08, 0.085])
LEVELS = ((FRONT_C, FRONT_R, 0.3), (HUMP_C, HUMP_R, 0.36), (WAIST_C, WAIST_R, 0.4))
MID = np.array([0.0, 0.01])          # the rim's loop is laid out by angle round this point


def rim_loop(n=96):
    """The top plate's edge: for each angle round MID the outermost of the three parts' level rings, at its height."""
    pts = []
    for th in np.linspace(0, 2 * math.pi, n, endpoint=False):
        d = np.array([math.cos(th), math.sin(th)])
        best, bz = 0.0, 0.0
        for c, r, lev in LEVELS:
            k = math.sqrt(max(1 - lev * lev, 0.0))
            a, b = r[0] * k, r[1] * k
            o = MID - c[:2]
            # |o + t d| on the ellipse (a, b): a quadratic in t
            A = (d[0] / a) ** 2 + (d[1] / b) ** 2
            B = 2 * (o[0] * d[0] / a ** 2 + o[1] * d[1] / b ** 2)
            C = (o[0] / a) ** 2 + (o[1] / b) ** 2 - 1
            disc = B * B - 4 * A * C
            if disc <= 0:
                continue
            t = (-B + math.sqrt(disc)) / (2 * A)
            if t > best:
                best, bz = t, c[2] + lev * r[2]
        pts.append((MID[0] + best * d[0], MID[1] + best * d[1], bz, th))
    return pts


def body(k):
    k.add("front", ell(FRONT_C, FRONT_R, deform=s_.flat_bottom, sub=5), RIM, "body")
    k.add("hump", ell(HUMP_C, HUMP_R, deform=s_.flat_bottom, sub=5), RIM, "tail")
    k.add("waist", ell(WAIST_C, WAIST_R, deform=s_.flat_bottom, sub=4), RIM, "body")
    k.paint(lambda P: np.maximum(np.maximum(s_.upper(FRONT_C, FRONT_R, 0.3)(P), s_.upper(HUMP_C, HUMP_R, 0.36)(P)),
                                 s_.upper(WAIST_C, WAIST_R, 0.4)(P)), TOP, on=["front", "hump", "waist"])


def rim(k):
    """The trap's raised jaw: a dark lip round the top's edge, teeth along its front, a crown of spikes behind."""
    loop = rim_loop()
    # the heights of the three parts' rings eased into one another round the loop, so the lip has no steps
    zs = np.array([z for _, _, z, _ in loop])
    for _ in range(12):
        zs = (np.roll(zs, 1) + 2 * zs + np.roll(zs, -1)) / 4
    loop = [(x, y, z, th) for (x, y, _, th), z in zip(loop, zs)]
    P = np.array([(x, y, z + 0.007) for x, y, z, _ in loop])
    # a closed tube: the loop walked once round and a little past, its seam at the back under the crown
    start = int(np.argmax(P[:, 1]))
    order = [(start + i) % len(P) for i in range(len(P) + 2)]
    k.add("lip", outward(tube(P[order], [0.013] * len(order), seg=12, per=2)), RIM, "body", fuse=False,
          surface="metal")
    # teeth: broad triangles standing up along the front of the lip, leaning a little out, clear of the eyes
    for i, th in enumerate(np.linspace(math.radians(-158), math.radians(-22), 7)):
        j = int(round((th % (2 * math.pi)) / (2 * math.pi) * len(loop))) % len(loop)
        x, y, z, _ = loop[j]
        inward = np.array([MID[0] - x, MID[1] - y, 0.0]); inward /= np.linalg.norm(inward)
        p = np.array([x, y, z + 0.008]) - inward * 0.004
        tdir = P[(j + 1) % len(P)] - P[j - 1]; tdir[2] = 0; tdir /= np.linalg.norm(tdir)
        h = 0.036 + 0.004 * (i in (2, 3, 4))
        w = 0.024
        apex = p + np.array([0, 0, h]) - inward * 0.014
        k.add(f"tooth{i}", hull([p + tdir * w, p - tdir * w, p + inward * 0.01, p - inward * 0.01, apex]), RIM,
              "body", fuse=False, surface="metal")
    # the crown: a low dark wall along the back of the lip, leaning out, and tall spikes along it, the middle ones
    # tallest (the references draw it as one jagged plate)
    ths = np.linspace(math.radians(26), math.radians(154), 13)
    for i in range(len(ths) - 1):
        q = []
        for th in (ths[i], ths[i + 1]):
            j = int(round(th / (2 * math.pi) * len(loop))) % len(loop)
            x, y, z, _ = loop[j]
            outw = np.array([x - MID[0], y - MID[1], 0.0]); outw /= np.linalg.norm(outw)
            p = np.array([x, y, z + 0.004])
            q += [p + outw * 0.009, p - outw * 0.009, p + (0, 0, 0.034) + outw * 0.018, p + (0, 0, 0.034) + outw * 0.004]
        k.add(f"wall{i}", hull(q), RIM, "tail", fuse=False, surface="metal")
    for i, th in enumerate(np.linspace(math.radians(32), math.radians(148), 7)):
        j = int(round(th / (2 * math.pi) * len(loop))) % len(loop)
        x, y, z, _ = loop[j]
        p = np.array([x, y, z + 0.006])
        tdir = P[(j + 1) % len(P)] - P[j - 1]; tdir[2] = 0; tdir /= np.linalg.norm(tdir)
        outw = np.array([x - MID[0], y - MID[1], 0.0]); outw /= np.linalg.norm(outw)
        mid = 1 - abs(i - 3) / 3
        h = 0.05 + 0.035 * mid
        apex = p + np.array([0, 0, h]) + outw * (0.022 + 0.01 * mid)
        k.add(f"spike{i}", hull([p + tdir * 0.026, p - tdir * 0.026, p + outw * 0.014, p - outw * 0.012, apex]), RIM,
              "tail", fuse=False, surface="metal")


FIN_OUT = [(0.0, -0.066), (0.08, -0.05), (0.168, -0.004), (0.11, 0.03), (0.124, 0.098), (0.055, 0.074), (0.0, 0.078)]


def fins(k):
    for s in (1, -1):
        o = np.array([0.148 * s, -0.035, 0.034])
        ex = unit(0.92 * s, 0.1, 0.38)
        ez = unit(0.0, 0.86, 0.5)
        side = "R" if s > 0 else "L"
        k.add(f"fin{side}", slab(FIN_OUT, o, ex, ez, 0.022), FIN, "body", fuse=False, surface="metal")
        # two ribs, darker and a little thicker so they show on both faces
        for j, (b0, b1) in enumerate((((0.03, -0.02), (0.14, -0.005)), ((0.03, 0.03), (0.105, 0.07)))):
            dx, dz = b1[0] - b0[0], b1[1] - b0[1]
            L = math.hypot(dx, dz); nx, nz = -dz / L * 0.006, dx / L * 0.006
            rib = [(b0[0] + nx, b0[1] + nz), (b1[0], b1[1]), (b0[0] - nx, b0[1] - nz)]
            k.add(f"rib{side}{j}", slab(rib, o, ex, ez, 0.028), RIB, "body", fuse=False, surface="metal")


EYE_X, EYE_DY = 0.074, -0.6          # further back up the top than Stunfisk's, clear of the trap's front teeth


def eye_point(s):
    dx = EYE_X / FRONT_R[0]
    dz = math.sqrt(max(1 - dx * dx - EYE_DY * EYE_DY, 0.02))
    return s_.on(FRONT_C, FRONT_R, (dx * s, EYE_DY, dz))


def cloud(lobes=3, depth=0.22, turn=0.0):
    return lambda th: 1.0 - depth + depth * abs(math.cos(lobes * (th + turn) / 2))


def moss(k):
    """Lime blotches on the top: round the model's left eye, a clover beside the right eye, one on the hump."""
    pl, nl = eye_point(-1)
    k.decal(pl + np.array([-0.012, 0.004, 0.0]), nl, (0.052, 0.046), MOSS, eps=0.003, outline=cloud(4, 0.18, 0.4),
            rings=14, segs=64)
    for (x, y), half, c, src in (((0.112, -0.0), (0.06, 0.05), cloud(3, 0.26, 0.6), (FRONT_C, FRONT_R)),
                                 ((-0.035, 0.095), (0.05, 0.042), cloud(3, 0.24, 0.2), (HUMP_C, HUMP_R))):
        cc, rr = src
        dx, dy = (x - cc[0]) / rr[0], (y - cc[1]) / rr[1]
        p, n = s_.on(cc, rr, (dx, dy, math.sqrt(max(1 - dx * dx - dy * dy, 0.05))))
        k.decal(p, n, half, MOSS, up=(0, 1, 0), eps=0.003, outline=c, rings=14, segs=64)


LIPS_C = np.array([0.0, -0.15, 0.084])


def mouth(k):
    """A small mouth pursed up between the eyes: red lips over and round a row of white teeth (loose, so crisp)."""
    k.add("lips", outward(ell(LIPS_C + (0, 0.004, 0.008), (0.028, 0.02, 0.016))), LIP, "body", fuse=False,
          surface="skin")
    k.add("teeth", outward(ell(LIPS_C + (0, -0.004, -0.004), (0.024, 0.018, 0.012), deform=s_.flat_bottom)), TEETH,
          "body", fuse=False, surface="skin")
    k.add("chin", outward(ell(LIPS_C + (0, 0.0, -0.012), (0.026, 0.019, 0.009))), LIP, "body", fuse=False,
          surface="skin")


CW, CK, CT = srgb(WHITE), srgb(INK), srgb(TOP)


def eyes(k, state):
    for s in (1, -1):
        p, n = eye_point(s)
        look = eye_look(state, s_.eye_open, CT, CK, line=0.15)
        k.decal(p, n, s_.EYE_HALF, look if s > 0 else (lambda u, v, f=look: f(-u, v)), eps=0.0045, rings=20,
                segs=56)
        if state in ("Open", "Fierce"):
            q = p + np.array([-0.008 * s, -0.003, -0.001])
            k.decal(q, n, (0.0085, 0.0085), INK, eps=0.006, rings=8, segs=32)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="scales")
    body(k)
    rim(k)
    fins(k)
    moss(k)
    mouth(k)
    k.build(voxel=0.0045, smooth=4, band=0.006)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
