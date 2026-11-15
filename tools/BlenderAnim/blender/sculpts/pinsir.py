# Pinsir sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pinsir.py -- <the export's Pinsir.glb> <out.glb>
#
# From the references: a taupe-brown stag beetle standing upright, its head and body one tall egg, a little broader at
# the top. Two great light-grey horns (grey in both pictures, not cream) rise from the egg's upper sides, curving out
# and up and their tips turning in, each studded on both edges with short spikes. Small angry eyes, white with a black
# pupil at the inner corner under a black rim slanting down to the middle; under them a tall dark mouth with white teeth
# set sideways like ribs either side of a dark spine. Dark lines round the lower body. Thin jointed arms (round
# shoulders, elbows and fists) with three grey claws a hand, carried forward and a little down here (raised in the art);
# short thick legs, a round thigh and a round shin, grey claws at the foot and a spur behind.
# Reference ratios (3D picture): horns above the egg 0.26 of the height, the egg 0.63 tall from 0.12 of the height up,
# the egg's width 0.43 of the height, the mouth 0.23 tall and 0.17 wide, an eye 0.08 wide.
# The export's joints: body (0, 0, 0.44), head (0, 0, 0.6), arms (+-0.15, -0.02, 0.5), legs (+-0.09, 0, 0.25).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, polygon, hull, outward, args

src, out = args()
k = Kit(src, coat="shell")

BROWN, LINE, HORN, CLAW = "#B09484", "#6A5448", "#D4D3DB", "#C6C5CE"
WHITE, INK, MOUTH, TOOTH = "#F4F4F0", "#1A1614", "#201814", "#F2F0EC"

BC = np.array([0.0, -0.005, 0.47])        # the egg's middle
BR = np.array([0.225, 0.2, 0.335])        # and its radii


def broad_top(V):
    V = V.copy()
    V[:, 0] *= 1 + 0.1 * V[:, 2]
    V[:, 1] *= 1 + 0.05 * V[:, 2]
    return V


# --- the egg of head and body ---------------------------------------------------------------------------------------
k.add("body", ell(BC, BR, deform=broad_top), BROWN, "body")
k.add("crown", ell((0, 0.0, 0.66), (0.2, 0.17, 0.15)), BROWN, "head")

# --- the horns: thick flat crescents, loose so their edges and tips keep, studded on both edges ---------------------
HPATH = np.array([(0.085, 0.0, 0.66), (0.17, -0.01, 0.78), (0.235, -0.03, 0.88), (0.25, -0.06, 0.965),
                  (0.205, -0.09, 1.03), (0.125, -0.11, 1.055)])
HRAD = [0.06, 0.066, 0.06, 0.047, 0.032, 0.012]


def path_at(t):
    """A point and the direction along the horn's path at t in 0..1 (straight between its points)."""
    n = len(HPATH) - 1
    i = min(int(t * n), n - 1)
    f = t * n - i
    p = HPATH[i] * (1 - f) + HPATH[i + 1] * f
    d = HPATH[i + 1] - HPATH[i]
    r = HRAD[i] * (1 - f) + HRAD[i + 1] * f
    return p, d / np.linalg.norm(d), r


def stud(p, out_dir, d, length, w):
    """A short spike: a four-sided point, its root buried in the horn."""
    out_dir = out_dir / np.linalg.norm(out_dir)
    face = np.cross(out_dir, d); face /= np.linalg.norm(face)
    base = p - out_dir * 0.012
    tip = p + out_dir * length + d * length * 0.25
    return hull([base + d * w, base - d * w, base + face * w * 0.8, base - face * w * 0.8, tip])


for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    k.add(f"horn{side}", outward(blade(HPATH * m, HRAD, (0, 1, 0.15), 0.62)), HORN, "head", fuse=False,
          surface="shell")
    for t, outer in ((0.3, True), (0.55, True), (0.75, True), (0.38, False), (0.62, False), (0.82, False)):
        p, d, r = path_at(t)
        p = p * m; d = d * m
        across = np.cross(d, (0, 1.0, 0)); across /= np.linalg.norm(across)
        if across[0] * s < 0:
            across = -across                    # toward the horn's outer edge
        o = across if outer else -across
        k.add(f"stud{side}{t}", stud(p + o * r * 0.9, o, d, 0.045, 0.02), HORN, "head", fuse=False,
              surface="shell")

# --- arms: round shoulder, a thin upper arm, a round elbow, a forearm and a fist, carried forward and a little down --
SH, EL, WR = np.array([0.215, -0.03, 0.52]), np.array([0.285, -0.12, 0.47]), np.array([0.3, -0.24, 0.47])
k.add("shoulder", ell(SH, (0.05, 0.05, 0.05)), BROWN, "arm{s}", mirror=True)
k.add("upper", tube([SH, EL], [0.028, 0.026]), BROWN, "arm{s}", mirror=True)
k.add("elbow", ell(EL, (0.036, 0.036, 0.036)), BROWN, "arm{s}", mirror=True)
k.add("fore", tube([EL, WR], [0.026, 0.03]), BROWN, "arm{s}", mirror=True)
FIST = WR + np.array([0.0, -0.035, 0.0])
k.add("fist", ell(FIST, (0.048, 0.05, 0.05)), BROWN, "arm{s}", mirror=True)


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
    for j, (off, d) in enumerate((((0.0, -0.035, 0.03), (0.0, -0.6, 0.8)),
                                  ((0.03, -0.04, -0.015), (0.55, -0.75, -0.2)),
                                  ((-0.03, -0.04, -0.015), (-0.5, -0.75, -0.25)))):
        base = (FIST + np.array(off)) * m
        k.add(f"hclaw{j}{side}", claw(base, np.array(d) * m, 0.07, 0.022), CLAW, f"arm{side}", fuse=False,
              surface="shell")

# --- legs: a round thigh, a round shin, grey claws at the foot and a spur behind -----------------------------------
k.add("thigh", ell((0.18, -0.01, 0.2), (0.095, 0.105, 0.1)), BROWN, "leg{s}", mirror=True)
k.add("shin", ell((0.2, -0.045, 0.075), (0.075, 0.095, 0.075)), BROWN, "leg{s}", mirror=True)
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    for j, dx in enumerate((-0.03, 0.03)):
        k.add(f"fclaw{j}{side}", claw(np.array([0.2 + dx, -0.12, 0.045]) * m, np.array([dx * 4, -0.7, -0.55]) * m,
                                      0.07, 0.024), CLAW, f"leg{side}", fuse=False, surface="shell")
    k.add(f"spur{side}", claw(np.array([0.21, 0.04, 0.06]) * m, np.array([0.3, 0.7, -0.6]) * m, 0.065, 0.022), CLAW,
          f"leg{side}", fuse=False, surface="shell")

# --- the lower body's lines: three dark rings round the egg under the mouth ---------------------------------------
for z in (0.3, 0.235, 0.175):
    k.paint(lambda P, z=z: inside(np.abs(P[:, 2] - z) - 0.005, 0.003) * inside(np.abs(P[:, 0]) - 0.21, 0.01),
            LINE, on=["body"])

# --- the mouth: a tall dark oval, four bold white teeth a side set sideways either side of a dark spine -------------
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

k.build(voxel=0.0075, band=0.006)

# --- the eyes: small angry whites, a black pupil at the inner corner, a black rim slanting down to the middle -------
CB, CW, CK = srgb(BROWN), srgb(WHITE), srgb(INK)
# u outward, v up: the inner corner low, the outer top high
SHAPE = [(-1.0, -0.05), (-0.35, 0.3), (0.95, 0.85), (1.0, -0.15), (0.55, -0.62), (-0.35, -0.6)]
RIM = 0.74


def make_eye(flip):
    shape = [(-u, v) for u, v in SHAPE] if flip else SHAPE
    out_r = polygon(shape)

    def look(uo, v):                        # uo outward on either side
        u = -uo if flip else uo             # the patch's own u, in which its outline is drawn
        th = np.arctan2(v, u)
        rr = np.sqrt(u * u + v * v) / np.array([out_r(t) for t in th])
        c = np.tile(CW, (len(u), 1))
        c[((uo + 0.3) / 0.36) ** 2 + ((v + 0.12) / 0.45) ** 2 < 1] = CK
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
        k.decal(E * m, de * m, (0.056, 0.045), fn, eps=0.005, rings=20, segs=56, outline=outline)


k.eye_states(eyes)
k.export(out)
