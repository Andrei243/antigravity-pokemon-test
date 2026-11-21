# Joltik sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/joltik.py -- <the export's Joltik.glb> <out.glb>
#
# The design, from the references: a tiny yellow tick that is head and body in one round ball, its top a crown of
# big jagged points of fur, a short fringe of points hanging over the lower face. Four eyes: two big blue ovals low on
# the face (a black pupil, a white glint, a darker blue rim) and two small blue ovals above them, nearer the middle.
# Four short, thick, fuzzy legs, two a side, splayed forward and back, each ending in a cuff of points with two blue
# claws under it. No mouth shows.
#
# Written to be grown from (galvantula.py imports it): the colours, a point of fur, a foot's cuff and claws, and the
# eyes are functions and constants; the build is under __main__.
# Skeleton: body (0, 0, 0.07), head (0, -0.04, 0.08), legs (+-0.05, -0.03 / 0.03, 0.06) on leg{s} and hind{s}.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, unit, axes, eye_look, args

YELLOW, YELLOW_D, BLUE, BLUE_D = "#F4D23E", "#D8B23A", "#3A64C8", "#24387C"
INK, WHITE, CLAW = "#141418", "#FFFFFF", "#4A62B0"

C = np.array([0.0, -0.002, 0.07])        # the ball's middle
R = np.array([0.074, 0.07, 0.064])       # its radii
VOXEL = 0.0018


# --- the line's pieces ---------------------------------------------------------------------------------------------
def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r) in direction d from its centre, and its outward normal."""
    c, r, d = np.array(c, float), np.array(r, float), unit(*d)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def point(k, name, root, d, length, radius, bone, colour=YELLOW, sides=4, flat=0.75, surface=None):
    """A point of fur (or a claw): a cone of flat faces from a base buried behind root out along d, a little
    flattened so it shows a lit face and a shaded one."""
    d = unit(*d)
    a, b = axes(d)
    root = np.array(root, float)
    base = root - d * radius * 1.2
    pts = [base + radius * (math.cos(t) * a + flat * math.sin(t) * b)
           for t in np.linspace(0, 2 * math.pi, sides, endpoint=False) + 0.4]
    pts += [root + d * length * 0.45 + radius * 0.45 * (math.cos(t) * a + flat * math.sin(t) * b)
            for t in np.linspace(0, 2 * math.pi, sides, endpoint=False) + 0.4]
    pts.append(root + d * length)
    k.add(name, hull(pts), colour, bone, fuse=False, surface=surface)


def crown(k, c, r, bone, n=34, length=(0.024, 0.036), radius=0.021, seed=595, colour=YELLOW, lowest=-0.15,
          face=(-0.5, 0.5)):
    """Broad points of fur spread over the top and sides of a ball (centre c, radii r), leaning out and a little
    back, longest on the crown; the face (directions with y below face[0] and z below face[1]) left clear."""
    rs = np.random.RandomState(seed)
    golden = math.pi * (3 - math.sqrt(5))
    for i in range(n):
        z = 1 - (1 - lowest) * (i + 0.5) / n
        ring = math.sqrt(max(1 - z * z, 0))
        th = golden * i
        d = np.array([ring * math.cos(th), ring * math.sin(th), z])
        if d[1] < face[0] and z < face[1]:         # leave the face clear
            continue
        p, nrm = on_ell(c, r, d)
        lean = unit(*(nrm + np.array([0, 0.25, 0.1]) + rs.normal(size=3) * 0.12))
        ln = length[0] + (length[1] - length[0]) * (0.4 + 0.6 * max(z, 0)) * (0.85 + 0.3 * rs.rand())
        point(k, f"tuft{i}", p, lean, ln, radius * (0.85 + 0.3 * rs.rand()), bone, colour, flat=0.55, surface="fur")


def fringe(k, c, r, bone, xs, z, length, radius, colour=YELLOW):
    """A row of points hanging from the lower face, pointing down and forward."""
    for i, x in enumerate(xs):
        p, nrm = on_ell(c, r, (x, -1, z))
        point(k, f"fringe{i}", p, nrm * 0.6 + np.array([0, -0.2, -1.0]), length * (1 - 0.25 * abs(x)), radius, bone,
              colour, surface="fur")


def foot(k, name, at, d, bone, radius, cuff=YELLOW, claw=CLAW, n=6, scale=1.0):
    """A foot's cuff: points of fur round a leg's end, splaying out and down, and two blue claws under it."""
    at = np.array(at, float)
    d = unit(*d)
    a, b = axes(d)
    for i in range(n):
        t = 2 * math.pi * i / n + 0.3
        out = math.cos(t) * a + math.sin(t) * b
        root = at + out * radius * 0.7
        point(k, f"{name}cuff{i}", root, out * 0.9 + d * 0.6 + np.array([0, 0, -0.35]),
              0.024 * scale, 0.008 * scale, bone, cuff, surface="fur")
    side = np.cross(d, (0, 0, 1.0)); side /= np.linalg.norm(side)
    for s in (1, -1):
        root = at + d * radius * 0.4 + side * s * radius * 0.42 + np.array([0, 0, -radius * 0.4])
        point(k, f"{name}claw{'a' if s > 0 else 'b'}", root, d * 0.45 + side * s * 0.15 + np.array([0, 0, -1.0]),
              0.016 * scale, 0.0085 * scale, bone, claw, sides=6, flat=1.0, surface="shell")


def eye_big(u, v):
    """Blue with a darker blue rim, a black oval pupil toward the middle, a white glint on its upper inner edge."""
    r = np.sqrt(u * u + v * v)
    c = np.tile(srgb(BLUE), (len(u), 1))
    c[r > 0.82] = srgb(BLUE_D)
    c[((u + 0.12) / 0.36) ** 2 + ((v + 0.04) / 0.5) ** 2 < 1] = srgb(INK)
    c[((u + 0.3) / 0.17) ** 2 + ((v - 0.32) / 0.17) ** 2 < 1] = srgb(WHITE)
    return c


def eye_small(u, v):
    """A small blue oval with a pale glint."""
    c = np.tile(srgb(BLUE), (len(u), 1))
    c[((u + 0.15) / 0.35) ** 2 + ((v - 0.3) / 0.3) ** 2 < 1] = srgb("#8FB0F0")
    return c


# --- Joltik --------------------------------------------------------------------------------------------------------
LEGS = [  # bone, hip (inside the ball), knee, foot, the foot's way (on +X)
    ("leg{s}", (0.045, -0.018, 0.038), (0.068, -0.046, 0.04), (0.08, -0.058, 0.03), (0.45, -0.55, -0.7)),
    ("hind{s}", (0.045, 0.024, 0.038), (0.068, 0.046, 0.04), (0.08, 0.055, 0.03), (0.45, 0.55, -0.7)),
]
EYE_D, EYE_HALF = (0.41, -1, -0.28), (0.016, 0.0215)
SMALL_D, SMALL_HALF = (0.2, -1, 0.14), (0.0058, 0.0095)


def joltik(src, out):
    k = Kit(src, coat="fur")
    # the ball, a little flattened under; its front on the head bone
    def under(V):
        V = V.copy()
        V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.85, V[:, 2])
        return V
    k.add("ball", ell(C, R, deform=under, sub=5), YELLOW, "body")
    k.add("face", ell(C + np.array([0, -0.026, -0.006]), (0.066, 0.047, 0.054), sub=5), YELLOW, "head")
    # four short thick legs, fused into the ball's sides
    for bone, hip, knee, ft, d in LEGS:
        k.add("leg" + bone[:3], tube([hip, knee, ft], [0.027, 0.025, 0.022]), YELLOW, bone, mirror=True)
        k.add("ankle" + bone[:3], ell(ft, (0.025, 0.025, 0.02)), YELLOW, bone, mirror=True)
    # the crown of points, the fringe over the lower face
    crown(k, C, R, "body")
    fringe(k, C, R, "head", (-0.36, -0.12, 0.12, 0.36), -0.82, 0.026, 0.013)
    # the feet's cuffs and claws (written for both sides: the claws' sideways lean must mirror)
    for bone, hip, knee, ft, d in LEGS:
        for s in (1, -1):
            m = np.array([s, 1, 1.0])
            foot(k, ("R" if s > 0 else "L") + bone[:3], np.array(ft) * m, np.array(d) * m, bone.format(s="R" if s > 0 else "L"),
                 0.022, scale=1.15)
    # the two small eyes never blink: in every state
    for s in (1, -1):
        p, n = on_ell(C, R, (SMALL_D[0] * s, SMALL_D[1], SMALL_D[2]))
        k.decal(p, n, SMALL_HALF, eye_small if s > 0 else (lambda u, v: eye_small(-u, v)), up=(0, 0, 1),
                eps=0.0011, rings=12, segs=40)
    k.build(voxel=VOXEL, smooth=3, band=0.004, wband=0.02)

    V = np.array([v.co[:] for v in k.ob.data.vertices])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    print("KIT: board: shows %.3f in a square of %.3f" % (np.ptp(proj), 1.3 * max(h, 0.85 * max(w, l))))

    def lay(state):
        p, n = on_ell(C, R, EYE_D)
        k.decal(p, n, EYE_HALF, eye_look(state, eye_big, srgb(YELLOW), srgb(INK), line=0.16), up=(0, 0, 1),
                eps=0.0011, rings=20, segs=56, mirror=True)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    joltik(*args())
