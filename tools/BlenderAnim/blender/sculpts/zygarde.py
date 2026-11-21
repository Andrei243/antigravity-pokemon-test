# Zygarde (the 50% Forme) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/zygarde.py -- <the export's Zygarde.glb> <out.glb>
#
# From the references: a black-and-green serpent reared up, its lower body coiled on the ground (the skeleton's coil,
# body and seg1 to seg9, where the art draws an S) and its thick chest rising to a small dark wedge of a head. The
# design's largest mass is the hood: a fan of six long dark plates spread behind the head, each widening and then
# tapering to a point, each with two green hexagons side by side. The chest and the front of the neck green, edged in
# hexagons where they meet the black back; white hexagons on the chest. Branched green cilia on each side of the chest;
# a row of short dark spikes under the jaw on each side. The eye a white hexagon in a patch of green hexagons on the
# side of the head, a white streak along the top of the head above it. The coil green below, a black stripe along its
# top with a few green hexagons on it; the tail's end a green hand of five tendrils.
# Bones: body and tail (0.08, 0.24, 0.14); seg1 to seg9 a coil on the ground round (0, 0, 0.07); seg10 (0, 0, 0.28);
# head (0, -0.1, 0.5).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, outward, srgb, inside, tri, unit, eye_look, polygon, args

BLACK, BLACK2, GREEN, GREEN2, WHITE = "#3A322E", "#2A2422", "#8CD82C", "#A8EE3A", "#F2F6EE"

# --- the coil: a tube from the tail's root round the skeleton's coil, its top black ------------------------------
CP = np.array([(0.12, 0.38, 0.075), (0.09, 0.27, 0.075), (0.19, 0.1, 0.065), (0.12, -0.12, 0.062),
               (-0.045, -0.155, 0.065), (-0.145, -0.055, 0.07), (-0.13, 0.07, 0.075), (-0.03, 0.135, 0.085),
               (0.075, 0.105, 0.1), (0.12, 0.01, 0.115), (0.075, -0.075, 0.13), (0.0, -0.1, 0.165)])
CR = np.array([0.036, 0.046, 0.054, 0.058, 0.06, 0.062, 0.062, 0.062, 0.062, 0.064, 0.068, 0.072])
CB = ["tail", "body", "body", "seg1", "seg2", "seg3", "seg4", "seg5", "seg6", "seg7", "seg8", "seg9"]


def spline(pts, per):
    pts = np.array(pts, float)
    ext = np.vstack([2 * pts[0] - pts[1], pts, 2 * pts[-1] - pts[-2]])
    C, S = [], []
    for i in range(len(pts) - 1):
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        for j in range(per):
            t = j / per
            C.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                            + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
            S.append(i + t)
    C.append(pts[-1]); S.append(len(pts) - 1)
    return np.array(C), np.array(S)


FC, FS = spline(CP, 30)
FR = np.interp(FS, np.arange(len(CP)), CR)


def nearest(Q):
    return np.concatenate([((Q[a:a + 3000, None, :] - FC[None]) ** 2).sum(2).argmin(1) for a in range(0, len(Q), 3000)])


def coil(k):
    C, S = spline(CP, 10)
    R = np.interp(S, np.arange(len(CP)), CR)
    for i, bone in enumerate(CB):
        sel = (S >= i - 0.6) & (S <= i + 0.6)
        if sel.sum() < 2:
            continue
        k.add(f"coil{i:02d}", tube(C[sel], R[sel], seg=24, per=4), GREEN, bone)

    # the black stripe along its top, its edges stepped in hexagons; green hexagons on it
    def top(Q):
        j = nearest(Q)
        rel = Q - FC[j]
        h = rel[:, 2] / FR[j]
        along = FS[j] * 3.0
        edge = 0.38 + 0.1 * tri(along)
        m = inside(edge - h, 0.03) * (FS[j] < 10.6) * (np.linalg.norm(rel, axis=1) < FR[j] * 1.6)
        return m
    k.paint(top, BLACK, on=["coil"])

    # green hexagons along the stripe, painted in the coil's own frame (patches cast on a coil slip onto the next turn)
    def hexes(Q):
        j = nearest(Q)
        rel = Q - FC[j]
        m = np.zeros(len(Q))
        for s in (2.6, 4.4, 6.2, 8.0, 9.6):
            i = int(np.argmin(np.abs(FS - s)))
            t = FC[min(i + 1, len(FC) - 1)] - FC[max(i - 1, 0)]; t /= np.linalg.norm(t)
            sd = np.cross(t, (0, 0, 1.0)); sd /= np.linalg.norm(sd)
            q = Q - (FC[i] + np.array([0, 0, FR[i]]))
            a, b = q @ t, q @ sd
            # a hexagon: the largest of three projections
            hx = np.max(np.abs(np.stack([b, 0.5 * b + 0.866 * a, 0.5 * b - 0.866 * a])), axis=0)
            m = np.maximum(m, inside(hx - 0.016, 0.002) * (np.abs(q[:, 2]) < 0.03))
        return m * (rel[:, 2] > 0)
    k.paint(hexes, GREEN2, on=["coil"])


HEX = polygon([(math.cos(math.radians(60 * i)), math.sin(math.radians(60 * i))) for i in range(6)])

# --- the chest and neck: fused masses rising from the coil, green in front, black behind ---------------------------
NECK = [((0.0, -0.085, 0.24), (0.09, 0.076, 0.11), (8, 0, 0), "seg9"),
        ((0.0, -0.055, 0.35), (0.072, 0.064, 0.1), (-6, 0, 0), "seg10"),
        ((0.0, -0.07, 0.44), (0.052, 0.052, 0.07), (-20, 0, 0), "seg10")]


def axis_y(z):
    return np.interp(z, [0.14, 0.24, 0.35, 0.44, 0.52], [-0.09, -0.085, -0.055, -0.075, -0.13])


def neck(k):
    for i, (c, r, rot, bone) in enumerate(NECK):
        k.add(f"neck{i}", ell(c, r, rot=rot), BLACK, bone)

    # green over the front, edged in a row of hexagons along each side
    def front(Q):
        z = Q[:, 2]
        d = Q[:, 1] - axis_y(z) + 0.012 + 0.012 * tri(z / 0.05)
        return inside(d, 0.003) * (z > 0.14) * (z < 0.47)
    k.paint(front, GREEN, on=["neck", "coil11"])
    # white hexagons over the chest
    for x, z in ((0.032, 0.25), (-0.045, 0.2)):
        c = np.array([x, -0.152, z])
        k.decal(c, unit(x * 4, -1, 0.0), (0.017, 0.017), WHITE, outline=HEX, eps=0.003, rings=8, segs=48)


def cilia(k):
    """Branched green cilia out of each side of the chest: a stem and two forks."""
    for s, sd in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        root = np.array((0.06, -0.1, 0.27)) * m
        tip = np.array((0.13, -0.08, 0.24)) * m
        pts = [[root, tip], [np.array((0.095, -0.095, 0.26)) * m, np.array((0.12, -0.11, 0.3)) * m],
               [np.array((0.1, -0.09, 0.255)) * m, np.array((0.11, -0.075, 0.205)) * m],
               [np.array((0.062, -0.085, 0.32)) * m, np.array((0.105, -0.09, 0.335)) * m]]
        for j, (a, b) in enumerate(pts):
            k.add(f"cilium{sd}{j}", outward(tube([a, b], [0.012, 0.0105], seg=12, per=4)), GREEN, "seg10",
                  fuse=False, surface="scales")


def spikes(k):
    """Short dark spikes under the jaw, three a side, pointing down and out."""
    for s, sd in ((1, "R"), (-1, "L")):
        for j, (y, z) in enumerate(((-0.13, 0.47), (-0.1, 0.43), (-0.075, 0.39))):
            b = np.array((0.035 * s, y, z))
            d = unit(0.9 * s, -0.25, -0.55)
            a = np.array((0, 1.0, 0.3)); a = a - d * (a @ d); a /= np.linalg.norm(a)
            n = np.cross(d, a)
            ln = 0.055 - 0.008 * j
            k.add(f"spike{sd}{j}", hull([b + a * 0.016, b - a * 0.016, b + n * 0.012, b - n * 0.012, b + d * ln]),
                  BLACK2, "head", fuse=False, surface="scales")


# --- the head: a small dark wedge, a brow over each eye -----------------------------------------------------------
H, HR = np.array([0.0, -0.158, 0.522]), np.array([0.061, 0.104, 0.055])


def snout(V):
    V = V.copy()
    f = np.clip(-V[:, 1], 0, 1)
    V[:, 0] *= 1 - 0.38 * f
    V[:, 2] *= 1 - 0.3 * f
    V[:, 2] -= 0.12 * f
    return V


def head(k):
    k.add("head", ell(H, HR, deform=snout), BLACK, "head")
    k.add("brow", ell((0.034, -0.16, 0.548), (0.026, 0.05, 0.02), rot=(-8, 0, -18)), BLACK, "head", mirror=True)
    k.add("jaw", ell((0.0, -0.17, 0.49), (0.04, 0.075, 0.028), rot=(8, 0, 0)), BLACK, "head")
    # the hood's root: a black mass at the nape
    k.add("nape", ell((0.0, -0.04, 0.48), (0.07, 0.05, 0.065)), BLACK, "head")


# --- the hood: six long plates fanned behind the head, a mirror pair of three, each with two green hexagons ---------
FAN = np.array([0.0, -0.02, 0.48])
LEAN = math.radians(18)
PLATE = [(0.0, -0.02), (0.13, -0.043), (0.19, -0.037), (0.205, -0.022), (0.25, 0.0), (0.205, 0.022), (0.19, 0.037),
         (0.13, 0.043), (0.0, 0.02)]
R0 = 0.05


def hood(k):
    up = np.array([0.0, math.sin(LEAN), math.cos(LEAN)])
    face = np.cross(np.array([1.0, 0, 0]), up)        # the fan's normal, toward the front
    for s, sd in ((1, "R"), (-1, "L")):
        for j, a in enumerate((20, 48, 76)):
            ar = math.radians(a)
            d = math.cos(ar) * np.array([s, 0, 0.0]) + math.sin(ar) * up
            w = np.cross(face, d); w /= np.linalg.norm(w)
            depth = (0.012, -0.004, 0.006)[j]
            o = FAN + d * R0 + face * depth * -1
            k.add(f"plate{sd}{j}", slab(PLATE, o, d, w, 0.024), BLACK, "head", fuse=False, surface="scales")
            for q in (1, -1):
                hx = [(0.15 + 0.013 * math.cos(math.radians(60 * i + 30)),
                       q * 0.017 + 0.013 * math.sin(math.radians(60 * i + 30))) for i in range(6)]
                k.add(f"platehex{sd}{j}{'a' if q > 0 else 'b'}", slab(hx, o, d, w, 0.032), GREEN, "head",
                      fuse=False, surface="scales")


# --- the tail: a green hand of five tendrils, its plane turned out to +X ------------------------------------------
def tail(k):
    base = CP[0]
    k.add("palm", ell(base + (0.01, 0.03, 0.005), (0.04, 0.045, 0.028), rot=(0, 30, 25)), GREEN, "tail")
    fwd = unit(0.6, 0.8, 0.05)
    side = unit(0.8, -0.6, 0.0)
    upv = np.cross(side, fwd)
    for j, f in enumerate((-2, -1, 0, 1, 2)):
        d = unit(*(fwd + side * 0.32 * f))
        a = base + fwd * 0.04 + side * 0.016 * f
        ln = 0.085 - 0.012 * abs(f)
        k.add(f"tendril{j}", tube([a, a + d * ln * 0.55 + upv * 0.012, a + d * ln + upv * 0.03],
                                  [0.017, 0.015, 0.012], seg=16, per=5), GREEN, "tail")


# --- the face: a white hexagon eye in a patch of green hexagons; a white streak above -------------------------------
EYE_N = unit(0.66, -0.66, 0.36)


def on_head(d):
    d = unit(*d)
    t = 1.0 / math.sqrt(((d / HR) ** 2).sum())
    return H + d * t


def marks(k):
    e = on_head(EYE_N) + np.array([0.0, -0.012, 0.0])
    m = np.array([-1, 1, 1.0])
    for sgn in (1, -1):
        mm = m if sgn < 0 else np.ones(3)
        # the green patch round the eye, and a second green hexagon behind it
        k.decal(e * mm, EYE_N * mm, (0.03, 0.03), GREEN, outline=HEX, eps=0.003, rings=10, segs=60)
        b = on_head(unit(0.85, -0.1, 0.5)) * mm
        k.decal(b, unit(0.85, -0.1, 0.5) * mm, (0.02, 0.02), GREEN, outline=HEX, eps=0.003, rings=8, segs=48)
        # the white streak along the top of the head
        st = on_head(unit(0.35, -0.3, 0.88)) * mm
        k.decal(st, unit(0.35, -0.3, 0.88) * mm, (0.009, 0.05), WHITE, up=unit(0, -1, 0.3) * mm, eps=0.004,
                rings=8, segs=48)


CW, CK = srgb(WHITE), srgb("#1A1614")


def eye_open(u, v):
    return np.tile(CW, (len(u), 1))


def eyes(k, state):
    e = on_head(EYE_N) + np.array([0.0, -0.012, 0.0])
    look = eye_look(state, eye_open, CW, CK, line=0.18)
    k.decal(e, EYE_N, (0.019, 0.019), look, eps=0.005, rings=20, segs=56, outline=HEX, mirror=True)


def zygarde(src, dst):
    k = Kit(src, coat="scales")
    coil(k)
    neck(k)
    head(k)
    tail(k)
    marks(k)
    cilia(k)
    spikes(k)
    hood(k)
    k.build(voxel=0.0062, smooth=4, band=0.004, wband=0.05)
    k.eye_states(lambda state: eyes(k, state))
    k.export(dst)


if __name__ == "__main__":
    zygarde(*args())
