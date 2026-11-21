# Mega Pinsir sculpted on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pinsirmega.py -- <the export's Pinsir-Mega.glb> <out.glb>
#
# Grown from Pinsir's script, which builds as it is read: its egg, mouth, legs, arms, claws and eyes are copied here and
# lifted by the export's hover (its body bone stands 0.132 higher than Pinsir's). From the references, what the Mega
# changes, largest first: (1) great pale yellow wings spread to either side behind the arms, an orange vein along each
# top edge branching over the face; (2) the two halves of its shell opened up behind its head and splayed out, black,
# striped orange along their length, three orange spikes along each top edge; (3) the horns much taller and nearly
# straight, rising in a V, branched like antlers with big spikes on the outer edge and smaller on the inner;
# (4) the eyes yellow under a black rim, no white and no pupil; (5) the arms carried out and down, the legs tucked
# under it as it hovers. The mouth's grille, the egg's lines and the body's colour are Pinsir's.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, polygon, hull, outward, slab, args

DZ = 0.132                                  # the hover: everything of Pinsir's lifted by it

BROWN, LINE, HORN, CLAW = "#B09484", "#6A5448", "#D4D3DB", "#C6C5CE"
YELLOW, INK, MOUTH, TOOTH = "#F2D43A", "#1A1614", "#201814", "#F2F0EC"
SHELL, ORANGE, WING = "#2A2326", "#EE8228", "#F2E486"


class Lifted:
    """The kit with everything given to it in Pinsir's own places lifted by DZ."""
    def __init__(self, k):
        self.k = k

    def add(self, name, shape, *a, **kw):
        V, F = shape
        self.k.add(name, (np.array(V) + (0, 0, DZ), F), *a, **kw)

    def paint(self, mask, *a, **kw):
        self.k.paint(lambda P: mask(P - np.array((0, 0, DZ))), *a, **kw)

    def decal(self, c, *a, **kw):
        self.k.decal(np.array(c, float) + (0, 0, DZ), *a, **kw)


src, out = args()
kit = Kit(src, coat="shell")
k = Lifted(kit)

BC = np.array([0.0, -0.005, 0.47])
BR = np.array([0.225, 0.2, 0.335])


def broad_top(V):
    V = V.copy()
    V[:, 0] *= 1 + 0.1 * V[:, 2]
    V[:, 1] *= 1 + 0.05 * V[:, 2]
    return V


# --- the egg of head and body (Pinsir's) -----------------------------------------------------------------------------
k.add("body", ell(BC, BR, deform=broad_top), BROWN, "body")
k.add("crown", ell((0, 0.0, 0.66), (0.2, 0.17, 0.15)), BROWN, "head")

# --- the horns: taller and nearly straight, rising in a V, antlered with spikes on both edges -------------------------
HPATH = np.array([(0.085, 0.0, 0.66), (0.145, -0.01, 0.79), (0.185, -0.025, 0.95), (0.2, -0.04, 1.1),
                  (0.19, -0.05, 1.21), (0.165, -0.055, 1.29)])
HRAD = [0.062, 0.064, 0.056, 0.046, 0.033, 0.013]


def path_at(t):
    n = len(HPATH) - 1
    i = min(int(t * n), n - 1)
    f = t * n - i
    p = HPATH[i] * (1 - f) + HPATH[i + 1] * f
    d = HPATH[i + 1] - HPATH[i]
    r = HRAD[i] * (1 - f) + HRAD[i + 1] * f
    return p, d / np.linalg.norm(d), r


def stud(p, out_dir, d, length, w):
    """A spike of the antler: a four-sided point leaning up the horn, its root buried in it."""
    out_dir = out_dir / np.linalg.norm(out_dir)
    face = np.cross(out_dir, d); face /= np.linalg.norm(face)
    base = p - out_dir * 0.014
    tip = p + out_dir * length + d * length * 0.55
    return hull([base + d * w * 1.2, base - d * w, base + face * w * 0.8, base - face * w * 0.8, tip])


for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    k.add(f"horn{side}", outward(blade(HPATH * m, HRAD, (0, 1, 0.15), 0.62)), HORN, "head", fuse=False,
          surface="shell")
    for t, outer, length, w in ((0.3, True, 0.085, 0.028), (0.5, True, 0.09, 0.027), (0.68, True, 0.075, 0.024),
                                (0.83, True, 0.055, 0.02), (0.4, False, 0.05, 0.02), (0.6, False, 0.05, 0.019),
                                (0.77, False, 0.04, 0.017)):
        p, d, r = path_at(t)
        p = p * m; d = d * m
        across = np.cross(d, (0, 1.0, 0)); across /= np.linalg.norm(across)
        if across[0] * s < 0:
            across = -across
        o = across if outer else -across
        k.add(f"stud{side}{t}", stud(p + o * r * 0.85, o, d, length, w), HORN, "head", fuse=False, surface="shell")

# --- arms: carried out and down, a long forearm to a fist with three grey claws ---------------------------------------
SH, EL, WR = np.array([0.215, -0.03, 0.52]), np.array([0.3, -0.1, 0.48]), np.array([0.32, -0.2, 0.37])
k.add("shoulder", ell(SH, (0.052, 0.052, 0.052)), BROWN, "arm{s}", mirror=True)
k.add("upper", tube([SH, EL], [0.031, 0.029]), BROWN, "arm{s}", mirror=True)
k.add("elbow", ell(EL, (0.039, 0.039, 0.039)), BROWN, "arm{s}", mirror=True)
k.add("fore", tube([EL, WR], [0.029, 0.033]), BROWN, "arm{s}", mirror=True)
FIST = WR + np.array([0.0, -0.03, -0.02])
k.add("fist", ell(FIST, (0.05, 0.052, 0.05)), BROWN, "arm{s}", mirror=True)


def claw(base, d, length, w):
    d = unit(*d)
    a = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a)
    b = np.cross(d, a)
    base = np.array(base, float)
    return hull([base + a * w, base - a * w, base + b * w * 0.75, base - b * w * 0.75, base - d * w,
                 base + d * length * 0.55 + b * w * 0.35, base + d * length])


for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    for j, (off, d) in enumerate((((0.0, -0.04, 0.0), (0.0, -0.85, -0.5)),
                                  ((0.03, -0.035, -0.03), (0.5, -0.6, -0.6)),
                                  ((-0.03, -0.035, -0.03), (-0.45, -0.6, -0.65)))):
        base = (FIST + np.array(off)) * m
        k.add(f"hclaw{j}{side}", claw(base, np.array(d) * m, 0.075, 0.024), CLAW, f"arm{side}", fuse=False,
              surface="shell")

# --- legs: Pinsir's round thigh and shin, tucked under as it hovers, grey claws and a spur --------------------------
k.add("thigh", ell((0.17, -0.02, 0.21), (0.095, 0.105, 0.1)), BROWN, "leg{s}", mirror=True)
k.add("shin", ell((0.18, -0.07, 0.1), (0.075, 0.09, 0.07)), BROWN, "leg{s}", mirror=True)
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    for j, dx in enumerate((-0.03, 0.03)):
        k.add(f"fclaw{j}{side}", claw(np.array([0.18 + dx, -0.14, 0.07]) * m, np.array([dx * 4, -0.6, -0.65]) * m,
                                      0.07, 0.024), CLAW, f"leg{side}", fuse=False, surface="shell")
    k.add(f"spur{side}", claw(np.array([0.19, 0.02, 0.07]) * m, np.array([0.3, 0.7, -0.6]) * m, 0.06, 0.022), CLAW,
          f"leg{side}", fuse=False, surface="shell")

# --- the shell's two halves, opened up behind the head and splayed out: black, striped orange, spikes on top -------
SC = np.array([0.235, 0.21, 0.71])        # a half's middle (Pinsir's frame; lifted with the rest)
SR = (0.13, 0.055, 0.22)                   # across, thick, long


def shell_frame(s):
    """A half's axes: across (outward), out of its back, along its length (up and out)."""
    lean = math.radians(40)
    along = np.array([math.sin(lean) * s, 0.25, math.cos(lean)]); along /= np.linalg.norm(along)
    back = np.array([0.15 * s, 1.0, -0.3]); back -= along * (back @ along); back /= np.linalg.norm(back)
    across = np.cross(back, along)
    if across[0] * s < 0:
        across = -across
    return across, back, along


def placed(shape, c, R):
    V, F = shape
    return outward((np.array(V) @ R.T + c, F))


for s, side in ((1, "R"), (-1, "L")):
    ax, bk, al = shell_frame(s)
    R = np.stack([ax, bk, al], axis=1)
    c = SC * np.array([s, 1, 1.0])
    def dome(V):
        V = V.copy()
        V[:, 1] = np.where(V[:, 1] < 0, V[:, 1] * 0.35, V[:, 1])    # a flatter face toward the body
        return V
    k.add(f"shell{side}", placed(ell((0, 0, 0), SR, deform=dome, sub=4), c, R), SHELL, f"wing{side}", fuse=False,
          surface="shell")
    for j, a in enumerate((-0.055, 0.0, 0.055)):
        q = math.sqrt(1 - (a / SR[0]) ** 2)
        k.add(f"stripe{side}{j}", placed(ell((a, 0.004, -0.01), (0.011, SR[1] * q * 1.06, SR[2] * q * 0.9),
                                             deform=lambda V: dome(V) * (1, 1.45, 1), sub=3), c, R), ORANGE, f"wing{side}", fuse=False,
              surface="shell")
    for j, phi in enumerate((-8, 30, 62)):
        f = math.radians(phi)
        d = ax * math.sin(f) + al * math.cos(f)
        base = c + ax * SR[0] * math.sin(f) * 0.9 + al * SR[2] * math.cos(f) * 0.9
        side_w = np.cross(d, bk); side_w /= np.linalg.norm(side_w)
        k.add(f"sspike{side}{j}", hull([base + side_w * 0.026, base - side_w * 0.026, base + bk * 0.02,
                                        base - bk * 0.02, base - d * 0.02, base + d * 0.1 + bk * 0.01]), ORANGE,
              f"wing{side}", fuse=False, surface="shell")

# --- the wings: broad pale yellow plates spread out to either side, raised a little and swept back, veined orange --
WO = np.array([0.13, 0.16, 0.6])
WING_OUT = [(0.0, 0.1), (0.14, 0.13), (0.3, 0.12), (0.44, 0.08), (0.53, 0.03), (0.52, -0.04), (0.44, -0.11),
            (0.33, -0.19), (0.2, -0.25), (0.08, -0.22), (-0.01, -0.1)]


def vein(pts, w):
    """A strip along a line of (a, b) points, w either side: an outline for slab."""
    p = np.array(pts, float)
    left, right = [], []
    for i in range(len(p)):
        d = p[min(i + 1, len(p) - 1)] - p[max(i - 1, 0)]
        d /= np.linalg.norm(d)
        nrm = np.array([-d[1], d[0]])
        ww = w * (1 - 0.5 * i / (len(p) - 1))
        left.append(p[i] + nrm * ww); right.append(p[i] - nrm * ww)
    return left + right[::-1]


ex, ez = unit(1, 0.3, 0.12), unit(0, 0.25, 1)
k.add("wing", slab(WING_OUT, WO, ex, ez, 0.012), WING, "wing{s}", fuse=False, mirror=True, surface="leaf")
for j, line in enumerate(([(0.02, 0.1), (0.15, 0.125), (0.3, 0.115), (0.44, 0.075), (0.52, 0.03)],
                          [(0.06, 0.08), (0.2, 0.02), (0.34, -0.04), (0.45, -0.08)],
                          [(0.2, 0.02), (0.24, -0.1), (0.27, -0.18)])):
    k.add(f"vein{j}", slab(vein(line, 0.011 if j == 0 else 0.008), WO + unit(*np.cross(ex, ez)) * 0.0, ex, ez,
                           0.022), ORANGE, "wing{s}", fuse=False, mirror=True, surface="leaf")

# --- the lower body's lines (Pinsir's) -------------------------------------------------------------------------------
for z in (0.3, 0.235, 0.175):
    k.paint(lambda P, z=z: inside(np.abs(P[:, 2] - z) - 0.005, 0.003) * inside(np.abs(P[:, 0]) - 0.21, 0.01),
            LINE, on=["body"])

# --- the mouth (Pinsir's): a tall dark oval, four bold white teeth a side set sideways --------------------------------
MC = np.array([0.0, -0.2, 0.47])
MN = unit(0, -1, 0.05)
MH = (0.088, 0.12)
k.decal(MC, MN, MH, MOUTH, eps=0.004, rings=12, segs=60)
right = np.array([1.0, 0, 0])
upv = np.cross(MN, right)
for vc in (-0.6, -0.2, 0.2, 0.6):
    reach = 0.93 * math.sqrt(1 - vc * vc) - 0.03
    uc = 0.13 + (reach - 0.13) / 2
    half_u = (reach - 0.13) / 2
    for s in (1, -1):
        c = MC + right * s * uc * MH[0] + upv * vc * MH[1]
        k.decal(c, MN, (half_u * MH[0], 0.11 * MH[1]), TOOTH, eps=0.0065, rings=5, segs=36,
                outline=polygon([(-1, -0.8), (1, -0.8), (1, 0.8), (-1, 0.8)]))

kit.build(voxel=0.0075, band=0.006)

# --- the eyes: Pinsir's angry shape, yellow under a black rim, no pupil ---------------------------------------------
CB, CY, CK = srgb(BROWN), srgb(YELLOW), srgb(INK)
SHAPE = [(-1.0, -0.05), (-0.35, 0.3), (0.95, 0.85), (1.0, -0.15), (0.55, -0.62), (-0.35, -0.6)]
RIM = 0.72


def make_eye(flip):
    shape = [(-u, v) for u, v in SHAPE] if flip else SHAPE
    out_r = polygon(shape)

    def look(uo, v):
        u = -uo if flip else uo
        th = np.arctan2(v, u)
        rr = np.sqrt(u * u + v * v) / np.array([out_r(t) for t in th])
        c = np.tile(CY, (len(u), 1))
        c[rr > RIM] = CK
        return c
    return out_r, look, flip


ER = make_eye(False)
EL = make_eye(True)
de = unit(0.35, -0.93, 0.1)
E = np.array([0.08, -0.17, 0.665])


def eyes(state):
    for s, (outline, look, flip) in ((1, ER), (-1, EL)):
        m = np.array([s, 1, 1.0])
        col = eye_look(state, look, CB, CK, line=0.15)
        fn = (lambda u, v, col=col: col(-u, v)) if flip else col
        k.decal(E * m, de * m, (0.058, 0.047), fn, eps=0.005, rings=20, segs=56, outline=outline)


kit.eye_states(eyes)
kit.export(out)
