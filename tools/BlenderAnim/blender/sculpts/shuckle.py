# Shuckle sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/shuckle.py -- <the export's Shuckle.glb> <out.glb>
#
# The design, from the references: a round red shell, a dome a little flattened under, with large raised cream rings
# round dark holes and a few flat cream spots. Out of the holes come the soft yellow creature's parts: a long neck
# from the hole at the top of the front, rising and bending forward into a bean of a head (its snout bulging down in
# front, two short upright dashes of eyes high on its face), and four stubby limbs splayed from the holes low on the
# sides, out and down to the ground, two forward and two back, their ends rounded.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args

RED, CREAM, HOLE, YELLOW, INK = "#C83A3C", "#F0DFB2", "#2C1A16", "#F2CB3C", "#1A1612"

CS = np.array([0.0, 0.02, 0.12])           # the shell's middle (the body bone is at z 0.17)
RS = np.array([0.19, 0.19, 0.13])          # its radii; under the middle the height is squashed
UNDER = 0.75
HC = np.array([0.0, -0.155, 0.452])        # the head's middle
HR = (0.064, 0.082, 0.056)


def radii(d):
    return np.array([RS[0], RS[1], RS[2] * (UNDER if d[2] < 0 else 1.0)])


def on_shell(d):
    """The shell's surface along d from its middle, and the outward normal there."""
    d = np.array(d, float)
    r = radii(d)
    s = 1 / math.sqrt(((d / r) ** 2).sum())
    p = CS + d * s
    n = (p - CS) / r ** 2
    return p, n / np.linalg.norm(n)


def squash(V):
    V = V.copy()
    V[V[:, 2] < 0, 2] *= UNDER
    return V


def rim(k, name, d, rad):
    """A raised cream ring round a hole, fused, lying on the shell; the hole's inside painted dark."""
    p, n = on_shell(d)
    a = np.cross(n, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a); b = np.cross(n, a)
    th = np.linspace(0, 2 * math.pi, 49)
    th = np.concatenate([th, th[1:4]])
    pts = []
    for t in th:
        q = p + rad * (math.cos(t) * a + math.sin(t) * b)
        sp, sn = on_shell(q - CS)
        pts.append(sp + sn * 0.003)
    k.add(name, tube(pts, [0.0125] * len(pts), seg=12, per=3), CREAM, "body")
    def mask(P, p=p, n=n):
        D = P - p
        along = D @ n
        off = np.linalg.norm(D - np.outer(along, n), axis=1)
        return inside(off - (rad - 0.004), 0.002) * (along > -0.05)
    k.paint(mask, HOLE, on=["shell"])
    return p, n


def spot(k, d, r):
    p, _ = on_shell(d)
    k.paint(lambda P, p=p: inside(np.linalg.norm(P - p, axis=1) - r, 0.0025), CREAM, on=["shell"])


def limb(k, name, joint, out, foot, bone):
    """A stubby limb from its hole round the joint, out and down to the ground (authored on +X)."""
    M = np.array([-1, 1, 1.0])
    p, n = rim(k, name + "rimR", np.array(joint) - CS, 0.042)
    rim(k, name + "rimL", (np.array(joint) - CS) * M, 0.042)
    pts = [p - n * 0.035, p + n * 0.03, np.array(out), np.array(foot)]
    rr = [0.03, 0.03, 0.029, 0.031]
    k.add(name + "R", tube(pts, rr, seg=20, per=6), YELLOW, bone + "R", surface="skin")
    k.add(name + "L", tube([q * M for q in pts], rr, seg=20, per=6), YELLOW, bone + "L", surface="skin")


def eye_open(u, v):
    return np.tile(srgb(INK), (len(u), 1))


def eyes(k, state):
    for s in (1, -1):
        c = HC + np.array([s * 0.024, -0.07, 0.024])
        n = unit(s * 0.38, -1, 0.32)
        look = eye_look(state, eye_open, srgb(YELLOW), srgb(INK), line=0.2)
        k.decal(c, n, (0.0085, 0.017), look, eps=0.003, rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    k.add("shell", ell(CS, RS, deform=squash, sub=5), RED, "body")
    # the neck's hole at the top of the front, and the neck rising out of it into the head
    p, n = rim(k, "neckrim", np.array([0.0, -0.12, 0.2]) - CS, 0.046)
    neck = [p - n * 0.04, p + n * 0.03, (0, -0.138, 0.3), (0, -0.118, 0.37), (0, -0.118, 0.425)]
    k.add("neck", tube(neck, [0.033, 0.033, 0.032, 0.031, 0.031], seg=20, per=6), YELLOW, "head", surface="skin")
    k.add("head", ell(HC, HR, rot=(12, 0, 0)), YELLOW, "head", surface="skin")
    # the four limbs from the holes low on the sides
    limb(k, "arm", (0.13, -0.06, 0.1), (0.2, -0.138, 0.074), (0.208, -0.198, 0.03), "leg")
    limb(k, "hind", (0.13, 0.1, 0.1), (0.2, 0.174, 0.074), (0.208, 0.232, 0.03), "hind")
    # the empty holes and the flat spots
    for i, (d, r) in enumerate((((-0.55, -0.25, 0.8), 0.046), ((0.5, 0.45, 0.75), 0.043), ((0.0, 0.9, 0.42), 0.04),
                                ((-0.9, 0.12, 0.45), 0.036), ((0.86, -0.45, 0.38), 0.034))):
        rim(k, f"hole{i}rim", d, r)
    for d, r in (((0.3, -0.75, 0.55), 0.021), ((-0.2, 0.55, 0.85), 0.02), ((-0.6, 0.75, 0.25), 0.024),
                 ((0.75, 0.6, 0.15), 0.022), ((0.0, -1.0, 0.08), 0.022), ((-0.5, -0.65, 0.3), 0.018)):
        spot(k, d, r)
    k.build(voxel=0.0045, smooth=3)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
