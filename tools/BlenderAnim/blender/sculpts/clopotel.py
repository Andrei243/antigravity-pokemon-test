# Clopotel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/clopotel.py -- <the export's Clopotel.glb> <out.glb>
#
# The design, from its picture (docs/art/reference/gemini/carpathia/10083-Clopotel.jpg) and brief (CP-dex-2-mountains.md,
# "83. Clopotel: a sheep bell"): a small dented bronze shepherd's bell (talanga), a tapered box with rounded corners,
# narrow at its flat-topped shoulder and flaring a little to its lip, open below with the dark inside showing and a
# chip out of the rim on its -X front. Its bronze is mottled darker and paler, scratched on the +X side. A little
# round clapper hangs from inside, half below the rim. Two round eyes look out from the bell's shoulder: a dark socket
# rim, white, a brown iris and a black pupil. On its crown a bronze loop, and through it a dark leather strap tied in a
# big bow like hair: the +X loop the larger (the picture's right), two tails hanging to the shoulder.
# Drawn swinging, tilted, with rings of sound round it: built hanging level at the export's height, the rings dropped.
# The first of its line (Clopotel, Clopotitan): the bronze, the mottling and the bow are here and Clopotitan takes them
# through sibling("clopotel").
#
# Skeleton: root, body (0, 0, 0.379), clapper (0, 0, 0.419), bow (0, 0, 0.579). The bell, its loop and the eyes on body,
# the clapper's stem and ball on clapper, the strap's knot, loops and tails on bow. It hovers: lowest 0.12.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, boxy, eye_look, args

# --- the line's palette -----------------------------------------------------------------------------------------------
BRONZE, BRONZE_DK, BRONZE_LT, INSIDE = "#A2763A", "#6C4A22", "#C69A54", "#1E1610"
STRAP, STRAP_DK = "#4A3426", "#33241A"
IRON = "#3E3A36"
SOCKET, WHITE, IRIS, PUPIL = "#3A2A1A", "#F2EEE4", "#7A5430", "#14100C"

# --- the frame --------------------------------------------------------------------------------------------------------
RIM_Z, TOP_Z = 0.195, 0.525
HALF_BOT = (0.19, 0.155)       # the bell's half width and depth at its lip
TAPER = 0.47                  # how much narrower its shoulder is
EYE_X, EYE_Z, EYE_HALF = 0.054, 0.39, (0.028, 0.028)


def width(t):
    """The bell's share of its lip's width at height t (0 the lip, 1 the shoulder): tapering, flared at the lip."""
    return 1 - TAPER * t + 0.1 * (1 - t) ** 6


def bell_shape(z0, z1, hx, hy, n=4.0):
    """A deform making the unit sphere into the bell's tapered rounded box between heights z0 and z1."""
    def f(V):
        V = boxy(n)(V)
        t = (V[:, 2] + 1) / 2
        w = width(t)
        return np.stack([V[:, 0] * hx * w, V[:, 1] * hy * w, z0 + t * (z1 - z0)], 1)
    return f


def norm_in_bell(P, hx=HALF_BOT[0], hy=HALF_BOT[1], z0=RIM_Z, z1=TOP_Z, n=4.0):
    """How far out a point lies within the bell's section at its height: 1 on the outer wall."""
    t = np.clip((P[:, 2] - z0) / (z1 - z0), 0, 1)
    w = width(t)
    return ((np.abs(P[:, 0] / (hx * w)) ** n + np.abs(P[:, 1] / (hy * w)) ** n) ** (1 / n))


def mottle(k, rs, on, centre, size, count, z0, z1, scale=1.0):
    """Patina and dents: blots of darker and paler bronze over the bell."""
    for i in range(count):
        a = rs.uniform(0, 2 * math.pi)
        z = rs.uniform(z0, z1)
        c = np.array([centre[0] + math.cos(a) * size[0], centre[1] + math.sin(a) * size[1], z])
        r = rs.uniform(0.018, 0.03) * scale
        col = BRONZE_DK if i % 3 else BRONZE_LT
        k.paint(lambda P, c=c, r=r: inside(np.linalg.norm(P - c, axis=1) - r, 0.006 * scale), col, on=on)


def ribbon(pts, r, normal, f=0.32):
    """A strap: a tube through points flattened across the plate's normal, broad in its plane."""
    return blade(pts, [r] * len(pts), normal, f)


def bow(k, knot, scale, normal, bone, name="bow"):
    """The leather strap tied in a bow: a knot, two loops (the +X one larger) and two tails."""
    knot = np.array(knot, float)
    n = unit(*normal)
    k.add(f"{name}knot", ell(knot, np.array([0.042, 0.034, 0.036]) * scale), STRAP_DK, bone, fuse=False, surface="leather")
    for side, (L, W, ang) in ((1, (0.16, 0.1, 28)), (-1, (0.125, 0.08, 34))):
        d = np.array([side * math.cos(math.radians(ang)), 0, math.sin(math.radians(ang))])
        d = d - n * (d @ n); d /= np.linalg.norm(d)
        p = np.cross(n, d); p /= np.linalg.norm(p)
        th = np.linspace(0, 2 * math.pi, 19)
        c = knot + d * L * scale / 2
        pts = [c - d * (L * scale / 2) * math.cos(t) * 0.96 + p * (W * scale / 2) * math.sin(t) for t in th]
        k.add(f"{name}loop{'R' if side > 0 else 'L'}", ribbon(pts, 0.034 * scale, n), STRAP, bone, fuse=False,
              surface="leather")
    for side, end in ((1, (0.15, -0.02, -0.085)), (-1, (-0.115, -0.02, -0.055))):
        e = knot + np.array(end) * scale
        mid = knot + (np.array(end) * 0.5 + [side * 0.01, 0, 0.015]) * scale
        k.add(f"{name}tail{'R' if side > 0 else 'L'}", ribbon([knot, mid, e], 0.03 * scale, n), STRAP, bone,
              fuse=False, surface="leather")


def round_eye():
    cs, cw, ci, cp = srgb(SOCKET), srgb(WHITE), srgb(IRIS), srgb(PUPIL)
    def look(u, v):
        c = np.tile(cw, (len(u), 1))
        c[((u + 0.05) / 0.56) ** 2 + ((v + 0.02) / 0.6) ** 2 < 1] = ci
        c[((u + 0.05) / 0.32) ** 2 + ((v + 0.02) / 0.34) ** 2 < 1] = cp
        c[((u - 0.12) / 0.15) ** 2 + ((v - 0.22) / 0.15) ** 2 < 1] = (1, 1, 1)
        c[u * u + v * v > 0.74] = cs
        return c
    return look


def clopotel(src, dest):
    k = Kit(src, coat="metal")
    rs = np.random.RandomState(83)

    # The bell: a solid tapered box, its shoulder domed a little, hollowed from below, a chip out of its rim
    k.add("bell", ell((0, 0, 0), (1, 1, 1), deform=bell_shape(RIM_Z, TOP_Z, *HALF_BOT), sub=5), BRONZE, "body")
    k.add("hollow", ell((0, 0, 0), (1, 1, 1), deform=bell_shape(RIM_Z - 0.06, TOP_Z - 0.075, HALF_BOT[0] - 0.03,
                                                                 HALF_BOT[1] - 0.03), sub=5),
          INSIDE, "body", carve=True)
    k.add("chip", ell((-0.1, -0.162, RIM_Z + 0.004), (0.026, 0.035, 0.034), rot=(0, 30, 0)), BRONZE_DK, "body", carve=True)

    # The inside dark; the lip's band; the patina; a scratch on the +X side
    k.paint(lambda P: inside(norm_in_bell(P) - 0.87, 0.006) * inside(P[:, 2] - (TOP_Z - 0.1), 0.005), INSIDE)
    k.paint(lambda P: inside(np.abs(P[:, 2] - (RIM_Z + 0.03)) - 0.006, 0.002) * inside(0.9 - norm_in_bell(P), 0.01),
            BRONZE_DK, on=["bell"])
    mottle(k, rs, ["bell"], (0, 0), (0.15, 0.12), 14, RIM_Z + 0.04, TOP_Z - 0.01)
    def scratch(P):
        a, b = np.array([0.135, -0.07, 0.27]), np.array([0.105, -0.1, 0.34])
        t = np.clip(((P - a) @ (b - a)) / ((b - a) @ (b - a)), 0, 1)
        return inside(np.linalg.norm(P - (a + t[:, None] * (b - a)), axis=1) - 0.0025, 0.0015)
    k.paint(scratch, BRONZE_DK, on=["bell"])

    # The loop on its crown, the clapper's stem and ball
    th = np.linspace(math.pi, 0, 9)
    k.add("loop", tube([(0.036 * math.cos(t), 0.0, TOP_Z - 0.01 + 0.058 * math.sin(t)) for t in th], [0.0115] * 9),
          BRONZE, "body", fuse=False, surface="metal")
    k.add("stem", tube([(0, 0, 0.43), (0, -0.005, 0.3), (0, -0.012, 0.2)], [0.01, 0.01, 0.01]), IRON, "clapper",
          fuse=False, surface="metal")
    k.add("ball", ell((0, -0.02, 0.164), (0.044, 0.044, 0.044), sub=3), BRONZE_LT, "clapper", fuse=False,
          surface="metal")

    # The strap tied in its bow over the loop
    bow(k, (0, -0.01, 0.576), 1.0, (0.3, -0.92, 0.25), "bow")

    k.build(voxel=0.0045, smooth=3)
    board(k.ob)

    look = round_eye()
    def eyes(state):
        for s in (1, -1):
            dn = unit(s * 0.38, -0.88, 0.22)
            f = eye_look(state, look, srgb(BRONZE), srgb(SOCKET), line=0.17)
            lk = f if s > 0 else (lambda g: (lambda u, v: g(-u, v)))(f)
            k.decal((s * EYE_X, -0.115, EYE_Z), dn, EYE_HALF, lk, eps=0.0032, rings=20, segs=56)
    k.eye_states(eyes)
    k.export(dest)


def board(ob):
    V = np.array([v.co[:] for v in ob.data.vertices])
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    print("KIT: board: corners %.3f (0.853 w + 0.522 l), shows %.3f, in a square of %.3f"
          % (0.853 * w + 0.522 * l, np.ptp(proj), 1.3 * max(h, 0.85 * max(w, l))))


if __name__ == "__main__":
    clopotel(*args())
