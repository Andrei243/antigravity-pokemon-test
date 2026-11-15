# Jigglypuff sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/jigglypuff.py -- <the export's Jigglypuff.glb> <out.glb>
# A pink balloon that is all head: pointed ears lined dark navy, a fused curl of hair wound flat on the brow from the
# crown, very large eyes (a thick white ring with a dark rim round a teal iris and a glint), a small closed smile,
# little pointed arms and two oval feet. Written to be grown from (wigglytuff.py keeps this frame).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, boxy, eye_look, args

# --- colours ----------------------------------------------------------------------------------------------------------
PINK = "#F7C3D2"
LINING = "#2C3446"          # inside the ears: dark navy
INK = "#1E2430"             # the eyes' rim and the mouth's line
IRIS, PUPIL = "#2696A6", "#156674"
WHITE = "#FFFFFF"
MOUTH = "#6E3646"

# --- the frame (the skeleton's joints: body (0, 0, 0.2), ears (+-0.1, 0, 0.34), arms (+-0.16, -0.04, 0.2),
#     legs (+-0.08, -0.02, 0.06)); no head bone, so the face and the curl go on the body ---------------------------------
C = np.array([0, 0, 0.215])              # the ball's middle
R = np.array([0.19, 0.18, 0.185])        # its radii
EAR_BASE = np.array([0.105, 0.0, 0.335])
EAR_TIP = np.array([0.215, 0.012, 0.455])
EAR_R = [0.078, 0.06, 0.03, 0.005]
CURL_AT = (0.0, 0.5)                     # the spiral's middle on the brow: x, and z as a share of R up from C
CURL_R = 0.078                           # the spiral's outer radius
CURL_TUBE = 0.04                         # the lock's thickness where it starts
EYE_X, EYE_Z = 0.098, 0.008              # an eye's middle: out from the face's middle, and up from C
EYE_HALF = (0.054, 0.057)
MOUTH_Z = -0.07


def front(x, z, lift=0.0):
    """The ball's front point at (x, z), lifted off it along -y."""
    zn = (z - C[2]) / R[2]
    q = max(0.0, 1 - zn * zn - (x / R[0]) ** 2)
    return np.array([x, -R[1] * math.sqrt(q) - lift, z])


def normal_at(p):
    n = (p - C) / R ** 2
    return n / np.linalg.norm(n)


def on_ball(q, lift):
    """A point taken onto the ball's surface along its own ray from the middle, then lifted off it."""
    q = np.array(q, float)
    p = C + (q - C) / np.linalg.norm((q - C) / R)
    return p + normal_at(p) * lift


def roundish(V):
    """Round, a hair fuller below its middle."""
    V = boxy(2.15)(V)
    s = 1 + 0.03 * np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= s; V[:, 1] *= s
    return V


k = None


def add_body(k, color=PINK, deform=None):
    k.add("body", ell(C, R, deform=deform or roundish), color, "body")


def add_ears(k, base=EAR_BASE, tip=EAR_TIP, radii=EAR_R, lining=LINING, inset=0.62, color=PINK):
    """Pointed ears up and out from the crown's shoulders, flattened front to back, their front faces lined."""
    pts = [base, base + (tip - base) * 0.4, base + (tip - base) * 0.8, tip]
    k.add("ear", blade(pts, radii, (0, 1, 0), 0.45), color, "ear{s}", mirror=True)
    d = unit(*(tip - base))
    L = np.linalg.norm(tip - base)
    across = np.cross(d, (0, 1.0, 0)); across /= np.linalg.norm(across)
    rr = np.array(radii, float)

    def mask(P):
        Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
        rel = Q - base
        t = rel @ d / L
        w = rel @ across
        half = inset * np.interp(t, [0, 0.4, 0.8, 1.0], rr) - 0.004
        fr = rel[:, 1] - base[1] - (tip[1] - base[1]) * t
        return (inside(np.abs(w) - half, 0.0025) * inside(fr + 0.006, 0.002) * inside(0.14 - t, 0.004)
                * inside(t - 0.93, 0.004) * (P[:, 2] > base[2] - 0.02))
    k.paint(mask, lining)


def add_curl(k, at=CURL_AT, radius=CURL_R, thick=CURL_TUBE, turns=1.25, color=PINK, lead=None, lift=(0.018, 0.012)):
    """The lock of hair: from the crown over the top and down onto the brow, wound flat into a spiral (counter-
    clockwise as seen from the front: over to the viewer's left, down, round and up into its middle), proud of the
    face so it fuses into one raised lobe."""
    cz = C[2] + at[1] * R[2]
    cs = front(at[0], cz)
    n = normal_at(cs)
    e1 = np.array([1.0, 0, 0]); e2 = np.cross(n, e1)
    if e2[2] < 0:
        e2 = -e2
    pts = list(lead) if lead is not None else [on_ball((0.0, 0.07, C[2] + R[2]), 0.012),
                                                 on_ball((0.0, -0.02, C[2] + R[2]), 0.016)]
    rad = [thick * 0.9] * len(pts)
    for t in np.linspace(0, 1, 26):
        a = math.pi * 0.5 + t * 2 * math.pi * turns
        r = radius * (1 - 0.82 * t)
        # Wound over the ball's own curve, so the lock lies on the brow and doesn't stand off it like a visor
        pts.append(on_ball(cs + r * (math.cos(a) * e1 + math.sin(a) * e2), lift[0] + lift[1] * t))
        rad.append(thick * (1 - 0.45 * t))
    k.add("curl", tube(pts, rad), color, "body")


def add_arms(k, root=(0.15, -0.045, 0.195), tip=(0.235, -0.09, 0.17), radii=(0.048, 0.036, 0.022), color=PINK):
    """Little pointed arms out from the sides, carried a little forward and down."""
    root, tip = np.array(root), np.array(tip)
    k.add("arm", blade([root, root + (tip - root) * 0.55, tip], list(radii), (0, 0.4, 1), 0.65), color, "arm{s}",
          mirror=True)


def add_feet(k, at=(0.088, -0.06, 0.026), r=(0.05, 0.07, 0.03), turn=18, color=PINK,
              leg=((0.08, -0.02, 0.07), (0.085, -0.04, 0.04)), leg_r=(0.05, 0.045)):
    """Two oval feet under the ball, their toes turned a little out."""
    k.add("leg", tube(list(leg), list(leg_r)), color, "leg{s}", mirror=True)
    k.add("foot", ell(at, r, rot=(0, 0, -turn)), color, "leg{s}", mirror=True)


# --- the face: patches on the fused ball, laid before the body's build ----------------------------------------------
CP, CI, CW, CIR, CPU = srgb(PINK), srgb(INK), srgb(WHITE), srgb(IRIS), srgb(PUPIL)


def eye(u, v):
    """A thick white ring with a dark rim round a large teal iris, its darker middle and one glint up and out."""
    c = np.tile(CI, (len(u), 1))
    r = np.hypot(u, v)
    c[r < 0.8] = CW
    ir = np.hypot((u + 0.02) / 0.6, (v + 0.04) / 0.62)
    c[ir < 1] = CIR
    c[np.hypot((u + 0.02) / 0.32, (v + 0.06) / 0.34) < 1] = CPU
    c[np.hypot((u - 0.2) / 0.17, (v - 0.24) / 0.17) < 1] = CW
    return c


def mouth(u, v):
    """A small closed smile: one dark curve."""
    c = np.tile(CP, (len(u), 1))
    c[(np.abs(v - (-0.25 + 0.75 * u * u)) < 0.26) & (np.abs(u) < 0.9)] = srgb(MOUTH)
    return c


def add_mouth(k, z=MOUTH_Z, half=(0.03, 0.014), look=mouth):
    at = front(0.0, C[2] + z)
    k.decal(at, normal_at(at), half, look, eps=0.003, rings=12, segs=56)


def eye_place(x=EYE_X, z=EYE_Z):
    p = front(x, C[2] + z)
    return p, normal_at(p)


def lay_eyes(k, state, look=eye, half=EYE_HALF, x=EYE_X, z=EYE_Z, lid=PINK):
    p, n = eye_place(x, z)
    k.decal(p, n, half, eye_look(state, look, srgb(lid), CI), eps=0.004, rings=20, segs=56, mirror=True)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    add_body(k)
    add_ears(k)
    add_curl(k)
    add_arms(k)
    add_feet(k)
    add_mouth(k)
    k.build(voxel=0.0055, band=0.008)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)
