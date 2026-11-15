# Baltoy sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/baltoy.py -- <the export's Baltoy.glb> <out.glb>
#
# From the references: a small floating clay figure of tan clay (not brown) in two balls. The head is a squat onion,
# wider than tall, drawn up at the top into a cone of a spike; two eyes as raised domes side by side low on its front,
# each crossed by the line of a half-shut lid; a red band round the head at the eyes' level that arches up over each
# eye, so the two read as goggles. Under it a smaller ball with a round raised boss on its front, ringed in red, the red
# going round the ball from the ring; a short cone of a point under it, which it spins on. Two long flat clay arms out
# of the lower ball's sides, out and a little down, their ends turned down.
#
# Written to be grown from (claydol.py imports it with sibling). Bones: root, body, head, arm{s}.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, args, hull

CLAY, RED, LINE = "#D2C38E", "#DE5F5E", "#3A2E22"
X = np.array([1.0, 0, 0]); Z = np.array([0, 0, 1.0])


def cone(root, tip, r, n=8, back=0.5):
    """A solid cone from a ring of radius r round root to tip (a loose piece)."""
    root, tip = np.array(root, float), np.array(tip, float)
    d = tip - root; d /= np.linalg.norm(d)
    a = np.cross(d, Z if abs(d[2]) < 0.9 else X); a /= np.linalg.norm(a)
    b = np.cross(d, a)
    pts = [root + r * (math.cos(t) * a + math.sin(t) * b) for t in np.linspace(0, 2 * math.pi, n, endpoint=False)]
    pts.append(root - d * r * back)
    pts.append(tip)
    return hull(pts, merge=0)


HEAD_C, HEAD_R = np.array([0.0, 0.0, 0.352]), np.array([0.1, 0.09, 0.07])
LOW_C, LOW_R = np.array([0.0, 0.0, 0.229]), np.array([0.058, 0.054, 0.06])
EYE_C = np.array([0.036, -0.088, 0.338])          # the +X eye dome's middle
EYE_R = np.array([0.032, 0.013, 0.028])


def onion(V):
    """The head: squat, its top drawn up toward the spike."""
    V = V.copy()
    top = np.clip(V[:, 2], 0, 1)
    V[:, 2] += 0.18 * top ** 3
    s = 1 - 0.25 * top ** 2
    V[:, 0] *= s; V[:, 1] *= s
    return V


def head(k):
    k.add("head", ell(HEAD_C, HEAD_R, deform=onion), CLAY, "head")
    k.add("spike", cone(HEAD_C + (0, 0, 0.07), HEAD_C + (0, 0, 0.165), 0.03), CLAY, "head", fuse=False,
          surface="shell")
    k.add("eyedome", ell(EYE_C, EYE_R, rot=(0, 0, 12)), CLAY, "head", mirror=True)


def goggles(k):
    """Red: a band round the head at the eyes' level, arching up over each eye."""
    ez = EYE_C[2]
    def mask(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        band = inside(np.abs(z - (ez + 0.002)) - 0.009, 0.002) * (y > -0.06)
        arch = np.zeros(len(P))
        for s in (1, -1):
            dx, dz = x - EYE_C[0] * s, z - ez
            r = np.sqrt(dx * dx + dz * dz)
            arch = np.maximum(arch, inside(np.abs(r - 0.04) - 0.0085, 0.002) * (dz > -0.004) * (y < -0.02))
        # keep the arches off the eyes' domes themselves
        return np.maximum(band, arch)
    k.paint(mask, RED)


def lower(k):
    k.add("low", ell(LOW_C, LOW_R), CLAY, "body")
    k.add("boss", ell(LOW_C + (0, -0.05, 0.004), (0.028, 0.012, 0.028)), CLAY, "body")
    k.add("point", cone(LOW_C + (0, 0, -0.035), LOW_C + (0, 0, -0.11), 0.026), CLAY, "body", fuse=False,
          surface="shell")
    c = LOW_C + (0, 0, 0.004)
    def mask(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        r = np.sqrt((x - c[0]) ** 2 + (z - c[2]) ** 2)
        ring = inside(np.abs(r - 0.036) - 0.0075, 0.002) * (y < -0.02)
        band = inside(np.abs(z - (c[2] - 0.012)) - 0.0075, 0.002) * (r > 0.03) * (np.hypot(x, y) < 0.068)
        return np.maximum(ring, band)
    k.paint(mask, RED)


ARM = [(0.035, -0.004, 0.235), (0.085, -0.012, 0.228), (0.125, -0.02, 0.212), (0.15, -0.024, 0.186),
       (0.158, -0.026, 0.158)]


def arms(k, pts=ARM, r=(0.02, 0.02, 0.0195, 0.019, 0.017)):
    k.add("arm", blade(pts, r, (0, 1, 0), 0.75), CLAY, "arm{s}", mirror=True)


# ---- the eyes: raised domes with the line of a half-shut lid; their open state is that line -------------------------
CL, CK = srgb(CLAY), srgb(LINE)
SHADE = srgb("#C2B27E")


def slit(u, v, tilt=0.0, lift=0.08):
    c = np.tile(CL, (len(u), 1))
    curve = lift + 0.16 * (1 - u * u) + tilt * u
    c[v < curve - 0.12] = SHADE
    c[(np.abs(v - curve) < 0.16) & (np.abs(u) < 0.9)] = CK
    return c


def eye_lay(k, c=EYE_C, half=(0.03, 0.026), n=None):
    n = unit(0.35, -1, 0.05) if n is None else n
    def lay(state):
        if state == "Open":
            look = slit
        elif state == "Fierce":
            look = lambda u, v: slit(u, v, tilt=0.45, lift=0.02)         # the lid slanting down to the middle
        else:
            look = eye_look(state, slit, CL, CK, line=0.13)
        k.decal(c - n * 0.004, n, half, look, eps=0.003, rings=20, segs=56, mirror=True)
    return lay


def baltoy(src, out):
    k = Kit(src, coat="shell")
    head(k)
    goggles(k)
    lower(k)
    arms(k)
    k.build(voxel=0.004, smooth=3, band=0.006)
    k.eye_states(eye_lay(k))
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    baltoy(src, out)
