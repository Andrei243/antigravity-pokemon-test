# Rattata sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/rattata.py -- <the export's Rattata.glb> <out.glb>
# A small purple rat crouched low: a big head carried forward on a long body, big round ears with cream insides, a
# cream muzzle, cheeks, belly and paws, red eyes under a scowling brow, two big square front teeth over an open pink
# mouth, a whisker a side and a thin tail rising behind to a curl. Raticate (raticate.py) grows from its functions.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, srgb, inside, unit, boxy, eye_look, args

FUR, CREAM, LINING, NOSE = "#9C6FC4", "#F1DEA8", "#E8C796", "#6E4386"
WHITE, MOUTH, TONGUE = "#FAFAF4", "#9E2E44", "#EE8296"
IRIS, PUPIL, INK = "#D8283A", "#1A1016", "#2A1A2E"


def rot(angles):
    return np.array(Euler([math.radians(a) for a in angles]).to_matrix())


# --- the rat's own parts, shared with Raticate ------------------------------------------------------------------------
def teeth(k, at, w, h, bone="head"):
    """The two big square front teeth hanging side by side from the muzzle's front, loose so they keep their edges."""
    at = np.array(at, float)
    for s, n in ((1, "R"), (-1, "L")):
        k.add(f"tooth{n}", ell(at + (s * w * 0.55, 0, -h * 0.5), (w * 0.5, w * 0.32, h * 0.5), rot=(-6, 0, 0),
                               deform=boxy(5)), WHITE, bone, fuse=False, surface="shell")


def whisker(k, root, d, length, r, color, name="whisker", bone="head"):
    """One short thick whisker a side, tapering out from the muzzle (mirrored)."""
    root = np.array(root, float); d = unit(*d)
    k.add(name, tube([root, root + d * length * 0.5 + (0, 0, -0.004), root + d * length], [r, r * 0.7, r * 0.3], seg=10),
          color, bone, fuse=False, mirror=True)


def ear_lining(k, centre, angles, half, drop, color):
    """An ear's lining: a patch on the ear's front face, cast along the ear's own normal, smaller than the ear."""
    M = rot(angles)
    n = M @ np.array([0, -1.0, 0]); up = M @ np.array([0, 0, 1.0])
    k.decal(np.array(centre, float) + up * drop + n * 0.02, n, half, color, up=up, eps=0.003, rings=12, segs=48,
            mirror=True)


def eye_outline(top, side, n=72):
    """The eye's outline in the patch's own u, v (u outward): its unit circle cut along the top by the brow's line."""
    pts = []
    for th in np.linspace(0, 2 * math.pi, n, endpoint=False):
        u, v = math.cos(th), math.sin(th)
        if v > top(u):
            lo, hi = 0.0, 1.0
            for _ in range(30):
                t = (lo + hi) / 2
                if t * v > top(t * u):
                    hi = t
                else:
                    lo = t
            u, v = lo * u, lo * v
        pts.append((side * u, v))
    pts = np.array(pts)
    def r(th):
        # the outline's distance from the middle along th, read off the sampled ring
        a = np.arctan2(pts[:, 1], pts[:, 0])
        d = np.hypot(pts[:, 0], pts[:, 1])
        o = np.argsort(a)
        return float(np.interp((th + math.pi) % (2 * math.pi) - math.pi, a[o], d[o], period=2 * math.pi))
    return r


def lay_eyes(k, state, E, de, half, open_look, top, fierce_top, lid, ink, line=0.16):
    """
    The two eyes laid by hand (a mirrored patch's outline isn't mirrored), each cut along its top by the brow, which
    slants down toward the nose; the Fierce brow comes lower. u is outward on both sides as the colour function sees it.
    """
    for side in (1, -1):
        m = np.array([side, 1, 1.0])
        n = de * m
        if state in ("Open", "Fierce"):
            t = top if state == "Open" else fierce_top
            look = open_look
        else:
            t = lambda u: top(u) - 0.04
            look = eye_look(state, open_look, lid, ink, line=line)
        # this patch's own u is outward on +X and inward on -X
        col = (lambda f: (lambda u, v: f(u, v)))(look) if side > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
        k.decal(E * m, n, half, col, eps=0.006, outline=eye_outline(t, side), rings=20, segs=56)


def red_eye(white, iris, pupil):
    def look(u, v):
        c = np.tile(white, (len(u), 1))
        c[((u + 0.14) / 0.74) ** 2 + ((v + 0.1) / 0.8) ** 2 < 1] = iris
        c[((u + 0.24) / 0.3) ** 2 + ((v + 0.12) / 0.34) ** 2 < 1] = pupil
        c[((u - 0.08) / 0.15) ** 2 + ((v - 0.12) / 0.15) ** 2 < 1] = white
        return c
    return look


# --- Rattata ---------------------------------------------------------------------------------------------------------
H = np.array([0, -0.2, 0.245])            # the head's centre, forward of its joint
HR = np.array([0.115, 0.108, 0.104])
EC = np.array([0.1, -0.13, 0.375])        # the ear's centre (+X)
ER = np.array([0.072, 0.022, 0.078])
EA = (0, 24, 22)


def rattata(src, out):
    k = Kit(src, coat="fur")

    # A long low body, the rump a little higher than the shoulders
    k.add("body", ell((0, 0.06, 0.185), (0.112, 0.185, 0.1), rot=(-6, 0, 0)), FUR, "body")
    k.add("chest", ell((0, -0.07, 0.17), (0.095, 0.09, 0.085)), FUR, "body")
    k.add("head", ell(H, HR), FUR, "head")
    # The muzzle running forward, and the lower jaw dropped open under it
    def snout(V):
        V = V.copy(); t = np.clip(-V[:, 1], 0, 1)
        V[:, 0] *= 1 - 0.25 * t; V[:, 2] *= 1 - 0.2 * t
        return V
    k.add("snout", ell((0, -0.29, 0.222), (0.062, 0.085, 0.056), rot=(-6, 0, 0), deform=snout), FUR, "head")
    k.add("jaw", ell((0, -0.268, 0.163), (0.055, 0.062, 0.03), rot=(14, 0, 0)), CREAM, "head")
    k.add("cheek", ell((0.065, -0.22, 0.19), (0.052, 0.05, 0.047)), FUR, "head", mirror=True)

    # Big round ears, tilted out and turned a little forward, cream inside
    k.add("ear", ell(EC, ER, rot=EA), FUR, "ear{s}", mirror=True)
    k.add("earroot", ell((0.07, -0.135, 0.31), (0.035, 0.025, 0.04)), FUR, "ear{s}", mirror=True)
    ear_lining(k, EC, EA, (0.05, 0.055), -0.008, LINING)

    # Legs: slim in front, heavy thighs behind, cream paws with three toes
    def toes(V):
        ph = np.arctan2(V[:, 0], -V[:, 1])
        s = 1 + 0.18 * np.cos(3 * ph) * np.clip(-V[:, 1], 0, 1)
        V = V.copy(); V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("leg", tube([(0.07, -0.095, 0.17), (0.085, -0.11, 0.08), (0.088, -0.125, 0.03)], [0.036, 0.03, 0.026]), FUR,
          "leg{s}", mirror=True)
    k.add("paw", ell((0.09, -0.15, 0.018), (0.032, 0.044, 0.019), deform=toes), CREAM, "leg{s}", mirror=True)
    k.add("thigh", ell((0.08, 0.15, 0.15), (0.06, 0.08, 0.075)), FUR, "hind{s}", mirror=True)
    k.add("hind", tube([(0.088, 0.17, 0.12), (0.092, 0.19, 0.06), (0.092, 0.17, 0.03)], [0.034, 0.028, 0.026]), FUR,
          "hind{s}", mirror=True)
    k.add("hpaw", ell((0.094, 0.135, 0.018), (0.033, 0.055, 0.019), deform=toes), CREAM, "hind{s}", mirror=True)
    # the cream comes up over the wrists and ankles in a crisp line
    k.paint(lambda P: inside(P[:, 2] - 0.042, 0.003) * (P[:, 2] < 0.1), CREAM, on=["leg", "hind", "paw", "hpaw"])
    k.paint(lambda P: inside(0.042 - P[:, 2], 0.003) * (P[:, 2] < 0.1), FUR, on=["leg", "hind"])

    # The cream underneath: belly and chest, and the lower face from the cheeks to the chin
    k.paint(lambda P: inside(P[:, 2] - (0.125 + 0.15 * np.clip(-0.04 - P[:, 1], 0, 1)), 0.003) * inside(np.abs(P[:, 0]) - 0.085, 0.004)
            * (P[:, 1] > -0.16) * (P[:, 1] < 0.2) * (P[:, 2] > 0.05), CREAM, on=["body", "chest"])
    def face(P):
        line = 0.205 - 0.25 * (P[:, 1] + 0.2) - 0.25 * np.clip(np.abs(P[:, 0]) - 0.04, 0, 1)
        return inside(P[:, 2] - line, 0.0025) * (P[:, 1] < -0.15) * (P[:, 2] > 0.11)
    k.paint(face, CREAM)

    # The tail: up from the rump, out a little to the right, curling forward over the top and in
    rise = [(0, 0.21, 0.2), (0.015, 0.29, 0.25), (0.035, 0.35, 0.33), (0.05, 0.365, 0.4)]
    C = np.array([0.055, 0.3, 0.42])
    curl = []
    for i, th in enumerate(np.linspace(0.25, 2 * math.pi, 14)):
        r = 0.065 - 0.04 * th / (2 * math.pi)
        curl.append((C[0], C[1] + r * math.cos(th), C[2] + r * math.sin(th)))
    pts = rise + curl
    k.add("tail", tube(pts, np.linspace(0.02, 0.013, len(pts)), seg=14), FUR, "tail")

    # The nose's tip, the teeth, the whiskers
    k.add("nose", ell((0, -0.373, 0.232), (0.02, 0.014, 0.016)), NOSE, "head", fuse=False, surface="skin")
    teeth(k, (0, -0.352, 0.192), 0.026, 0.05)
    whisker(k, (0.045, -0.3, 0.215), (1, 0.15, 0.3), 0.085, 0.007, CREAM)

    # The open mouth under the teeth: dark red with the tongue at its bottom
    CM, CT = srgb(MOUTH), srgb(TONGUE)
    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        c[(u / 0.7) ** 2 + ((v + 0.7) / 0.5) ** 2 < 1] = CT
        return c
    k.decal((0, -0.33, 0.178), unit(0, -1, -0.25), (0.036, 0.024), mouth, eps=0.003, rings=12, segs=48)

    # Red eyes, the purple brow slanting down over them toward the nose
    CW, CF = srgb(WHITE), srgb(FUR)
    look = red_eye(CW, srgb(IRIS), srgb(PUPIL))
    de = unit(0.6, -0.76, 0.22)
    E = H + de / np.linalg.norm(de / HR)
    top = lambda u: 0.28 + 0.42 * u
    ftop = lambda u: 0.02 + 0.5 * u
    def eyes(state):
        lay_eyes(k, state, E, de, (0.035, 0.035), look, top, ftop, CF, srgb(INK))

    k.build(voxel=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    rattata(*args())
