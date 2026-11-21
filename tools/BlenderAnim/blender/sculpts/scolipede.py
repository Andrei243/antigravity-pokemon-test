# Scolipede sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/scolipede.py -- <the export's Scolipede.glb> <out.glb>
#
# Venipede grown (venipede.py's magenta, its cones with dark bands, its flat-topped eye and dark spikes): from the
# references, a huge magenta centipede reared up. Its rear runs level behind on three pairs of short legs; its front
# half rises in a column of bulging magenta segments arching up the back to a small helmeted head, while the whole
# front under them is one great dark grey belly, a pear swelling low and narrowing up the throat. A purple teardrop
# with a lilac rim on the side of every segment. A short purple spike pointing forward out of each side of the reared
# segments. The head: a magenta helmet with a beak hooking down, a dark grey jaw under it, a yellow-orange eye with a
# slit pupil under a heavy lid; two long horns sweeping forward and up from its crown, magenta with purple stripes. Two
# striped prongs up and back from the tail. Legs magenta, a dark grey band and a dark grey point at the foot.
# Skeleton: body (0, 0.1, 0.32); rear2, rear1, rear0 (0, 0.25 / 0.38 / 0.5, 0.28); front4 (0, -0.06, 0.45), front5
# (0, -0.11, 0.6), front6 (0, -0.1, 0.73), head (0, -0.12, 0.77); legs (+-0.06, -0.02 / 0.23 / 0.38, 0.22-0.24).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, ring_band, outward, srgb, inside, unit, eye_look, polygon, args, sibling

vp = sibling("venipede")
MAG, BELLY, PURPLE, LILAC = vp.RED, "#4C5254", "#8A4CA4", "#CE9CDC"
GREY, INK, YELLOW = vp.GREY, vp.INK, vp.YELLOW

# the segments: (centre, radii, bone); the reared ones bulge behind the spine, the rear ones run level
REARED = [((0, 0.075, 0.36), (0.142, 0.118, 0.1), "body"),
          ((0, -0.005, 0.475), (0.132, 0.106, 0.086), "front4"),
          ((0, -0.04, 0.6), (0.122, 0.1, 0.08), "front5"),
          ((0, -0.05, 0.715), (0.112, 0.094, 0.074), "front6")]
REAR = [((0, 0.19, 0.3), (0.128, 0.075, 0.1), "body"),
        ((0, 0.28, 0.29), (0.118, 0.07, 0.094), "rear2"),
        ((0, 0.37, 0.285), (0.108, 0.066, 0.088), "rear1"),
        ((0, 0.455, 0.285), (0.096, 0.062, 0.08), "rear0"),
        ((0, 0.53, 0.29), (0.078, 0.055, 0.066), "tail")]
HEAD_C, HEAD_R = np.array([0.0, -0.13, 0.8]), np.array([0.104, 0.115, 0.086])


def body(k):
    for i, (c, r, b) in enumerate(REARED):
        k.add(f"back{i}", ell(c, r), MAG, b)
    for i, (c, r, b) in enumerate(REAR):
        k.add(f"rear{i}", ell(c, r, deform=vp.flat_bottom(-0.85)), MAG, b)
    # the belly: a great dark pear low in front, narrowing up the throat to the jaw
    k.add("belly", ell((0, -0.04, 0.255), (0.165, 0.175, 0.215)), BELLY, "body")
    k.add("throat", tube([(0, -0.07, 0.36), (0, -0.11, 0.49), (0, -0.135, 0.61), (0, -0.145, 0.72)],
                         [0.13, 0.1, 0.08, 0.062], seg=24), BELLY, "front5")
    # the head: a helmet, a beak hooking down, a dark jaw under it
    k.add("head", ell(HEAD_C, HEAD_R), MAG, "head")

    def hook(V):
        V = V.copy()
        V[:, 2] -= 0.55 * np.clip(-V[:, 1], 0, 1) ** 2
        V[:, 0] *= 0.6 + 0.4 * np.clip(1 + V[:, 1], 0, 1)
        return V
    k.add("beak", ell((0, -0.215, 0.79), (0.058, 0.08, 0.042), deform=hook), MAG, "head")
    k.add("jaw", ell((0, -0.19, 0.735), (0.055, 0.065, 0.03), rot=(12, 0, 0)), BELLY, "head")


def marks(k):
    # purple teardrops with a lilac rim, point down, on each segment's side
    drop = []
    for th in np.linspace(0, 2 * math.pi, 64, endpoint=False):
        u, v = math.cos(th), math.sin(th)
        if v < 0:
            w = 1 + v * 0.9                            # narrowing to the point below
            u *= w
            v *= 1.0
        drop.append((u * 0.8, v * 1.0 + 0.1 * (v > 0)))
    DROP = polygon(drop)
    CP, CL, CM = srgb(PURPLE), srgb(LILAC), srgb(MAG)
    def tear(u, v):
        reach = np.array([DROP(t) for t in np.arctan2(v, u)])
        rho = np.sqrt(u * u + v * v) / reach
        c = np.tile(CP, (len(u), 1))
        c[rho > 0.62] = CL
        return c
    for c, r, b in REARED[1:] + REAR[:4]:
        for s in (1, -1):
            d = (0.9 * s, 0.42, 0.05) if c[1] > 0.1 else (0.86 * s, 0.5, 0.08)
            p, n = vp.on_ell(np.array(c) * np.array([1, 1, 1]), r, d)
            k.decal(p, n, (0.034, 0.046), tear, eps=0.004, rings=16, segs=56, outline=DROP)
    # the first reared segment's drops a little larger, lower on its side
    for s in (1, -1):
        c, r, b = REARED[0]
        p, n = vp.on_ell(c, r, (0.92 * s, 0.38, 0.0))
        k.decal(p, n, (0.04, 0.054), tear, eps=0.004, rings=16, segs=56, outline=DROP)


def pieces(k):
    # short purple spikes pointing forward out of the reared segments' sides
    for i, (c, r, b) in enumerate(REARED):
        for s in (1, -1):
            p, n = vp.on_ell(c, r, (0.62 * s, -0.75, -0.05))
            base = p - n * 0.008
            vp.spike(k, f"fspike{i}{'R' if s > 0 else 'L'}", base, base + unit(0.45 * s, -0.85, -0.15) * 0.06, 0.02, b,
                     color=LILAC, mirror=False, along=(0, 0, 1))
    # horns: two long, from the crown sweeping forward and up, magenta with purple stripes
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        P = np.array([(0.045, -0.12, 0.85), (0.068, -0.2, 0.895), (0.09, -0.3, 0.94), (0.108, -0.4, 0.985),
                      (0.118, -0.5, 1.03)]) * m
        R = [0.036, 0.03, 0.022, 0.013, 0.003]
        k.add(f"horn{side}", outward(tube(P, R, seg=18)), MAG, "head", fuse=False, surface="shell")
        for t in (0.3, 0.52, 0.72):
            seg = t * (len(P) - 1); i = int(seg); f = seg - i
            c = P[i] + (P[i + 1] - P[i]) * f
            d = P[i + 1] - P[i]
            rad = R[i] + (R[i + 1] - R[i]) * f
            k.add(f"hband{side}{t}", ring_band(c, d, rad + 0.0035, 0.03, thick=0.006), PURPLE, "head", fuse=False,
                  surface="shell")
    # prongs: two up and back from the tail
    for s in (1, -1):
        vp.antenna(k, f"prong{'R' if s > 0 else 'L'}", (0.04 * s, 0.55, 0.31), unit(0.3 * s, 0.62, 0.72), 0.2, 0.026,
                   "tail", color=MAG, band=PURPLE, bands=((0.28, 0.4), (0.55, 0.66)))
    # legs: short and bent outward, a dark band at the knee and a dark point for a foot
    for bone, y in (("leg{s}", -0.03), ("hind{s}", 0.23), ("hind{s}.001", 0.38)):
        for s, side in ((1, "R"), (-1, "L")):
            m = np.array([s, 1, 1.0])
            hip, knee, ankle = np.array((0.08, y, 0.24)), np.array((0.2, y - 0.01, 0.2)), np.array((0.215, y - 0.03, 0.055))
            if y < 0:
                hip, knee, ankle = (np.array((0.1, y, 0.16)), np.array((0.2, y - 0.02, 0.14)),
                                    np.array((0.215, y - 0.04, 0.05)))
            P = np.array([hip, knee, ankle]) * m
            bn = bone.format(s=side)
            k.add(f"leg{side}{y}", outward(tube(P, [0.044, 0.04, 0.032], seg=16)), MAG, bn, fuse=False,
                  surface="shell")
            mid = P[1] + (P[2] - P[1]) * 0.55
            k.add(f"knee{side}{y}", ring_band(mid, (P[2] - P[1]), 0.04, 0.034, thick=0.007), GREY, bn,
                  fuse=False, surface="shell")
            vp.spike(k, f"foot{side}{y}", ankle * m + np.array((0, 0, 0.015)), np.array((0.225 * s, y - 0.07, 0.0)), 0.03,
                     bn, mirror=False, along=(1, 0, 0))


# ---- the eye: yellow-orange with a slit pupil, its top cut by a heavy lid sloping down to the beak (u outward)
EYE = []
for th in np.linspace(0, 2 * math.pi, 96, endpoint=False):
    u, v = math.cos(th), math.sin(th)
    lim = 0.18 + 0.32 * u
    if v > lim:
        t = lim / (v - 0.32 * u) if (v - 0.32 * u) > 1e-6 else 1
        u, v = u * t, v * t
    EYE.append((u, v))
_ER, _EL = polygon(EYE), polygon([(-a, b) for a, b in EYE[::-1]])
# a ray that slips between two of the outline's points answers far off: cap it at the circle
EYER = lambda th: min(_ER(th), 1.0)
EYEL = lambda th: min(_EL(th), 1.0)
CY, CK, CM = srgb(YELLOW), srgb(INK), srgb(MAG)


def eye_open(u, v):
    reach = np.array([EYER(t) for t in np.arctan2(v, u)])
    rho = np.sqrt(u * u + v * v) / reach
    c = np.tile(CY, (len(u), 1))
    c[rho > 0.8] = CK
    return c


def eyes(k, state):
    look = eye_look(state, eye_open, CM, CK, line=0.17)
    for s in (1, -1):
        p, n = vp.on_ell(HEAD_C * np.array([s, 1, 1]), HEAD_R, (0.86 * s, -0.36, 0.2))
        col = look if s == 1 else (lambda a, b, f=look: f(-a, b))
        k.decal(p, n, (0.05, 0.044), col, eps=0.004, rings=20, segs=56, outline=EYER if s == 1 else EYEL)
        if state in ("Open", "Fierce"):
            r_ = np.cross((0, 0, 1.0), n); r_ /= np.linalg.norm(r_)
            pc = p - r_ * 0.006 * s - np.array((0, 0, 0.008 if state == "Open" else 0.013))
            k.decal(pc, n, (0.0075, 0.019 if state == "Open" else 0.012), INK, eps=0.007, rings=6, segs=28)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    body(k)
    marks(k)
    pieces(k)
    k.build(voxel=0.0065, smooth=3, band=0.004)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
