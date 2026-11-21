# Grafaiai sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/grafaiai.py -- <the export's Grafaiai.glb> <out.glb>
#
# Grown from Shroodle (sibling("shroodle")): its glassy mint lens of an eye. From the references (they overrule the
# brief's "round yellow eyes"): a lean dark navy monkey whose largest masses are its two great flat ears, broad at the
# root and pointed at the tip, and its big head; a crown of olive-grey spikes standing round the top of the head; a white
# mask over the face with two purple stripes up the forehead, framing two big mint lens eyes, each ringed purple and cut
# by a purple slant falling toward the middle; a dark snout down the middle of the mask, cyan paint drooling from its
# mouth; a narrow dark body; one arm raised to its chin and one hanging, fingers dipped pink; bowed legs, dark above and
# teal below with a white band, on pointed white feet; a ringed teal and white tail curling up behind on the +X side,
# ending in a spiky teal brush.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, ring_band, srgb, inside, unit, axes, eye_look, outward, args, sibling

shr = sibling("shroodle")
cap = sibling("capsakid")

NAVY, EAR, SNOUT, OLIVE = "#383851", "#302E46", "#25232F", "#ACA98E"
WHITE, PURPLE, TEAL, PINK, CYAN = "#F2F1F5", "#6E44B8", "#41606A", "#E43E8E", "#3CC4DC"
H = np.array([0.0, -0.012, 0.333])
HR = np.array([0.108, 0.092, 0.088])
VOXEL = 0.0045


def on_ell(C, R, d, lift=0.0):
    return shr.on_ell(C, R, d, lift)


def cone(k, name, root, d, length, radius, colour, bone, sides=6):
    """A solid spike of flat faces from a base buried behind root out along d."""
    d = unit(*d)
    a, b = axes(d)
    base = np.array(root, float) - d * radius * 0.8
    pts = [base + radius * (math.cos(t) * a + math.sin(t) * b) for t in np.linspace(0, 2 * math.pi, sides, endpoint=False)]
    pts.append(np.array(root, float) + d * length)
    k.add(name, hull(pts), colour, bone, fuse=False, surface="fur")


def head(k):
    k.add("head", ell(H, HR), NAVY, "head")
    k.add("snout", tube([(0, -0.092, 0.335), (0, -0.104, 0.305), (0, -0.108, 0.282)], [0.019, 0.023, 0.025]), SNOUT,
          "head")
    k.add("chin", ell((0, -0.088, 0.278), (0.027, 0.026, 0.022)), SNOUT, "head")
    # the white mask over the face, then the olive of the crown behind its top
    def mask(P):
        q = P - H
        return (inside(q[:, 1] + 0.03, 0.003) * inside((q[:, 0] / 0.096) ** 2 + ((q[:, 2] + 0.004) / 0.082) ** 2 - 1, 0.008))
    k.paint(mask, WHITE, on=["head"])
    k.paint(lambda P: inside(0.058 - (P - H) @ unit(0, 0.45, 1), 0.0025)
            * inside(np.sqrt((((P - H) / HR) ** 2).sum(1)) - 1.3, 0.004), OLIVE, on=["head"])
    # the crown of spikes, leaning back a little
    for i, phi in enumerate((-78, -52, -26, 0, 26, 52, 78)):
        p = math.radians(phi)
        d = unit(math.sin(p), 0.2, math.cos(p))
        root = on_ell(H, HR, d) - d * 0.01
        L = 0.078 - 0.02 * abs(phi) / 78
        cone(k, f"crown{i}", root, unit(*(d + np.array([0, 0.35, 0]))), L, 0.024, OLIVE, "head")
    # two great flat ears, broad at the root, pointed and a little raised at the tip, swept back
    def ear_shape(V):
        V = V.copy(); x = np.clip((V[:, 0] + 1) / 2, 0, 1)
        V[:, 2] = V[:, 2] * (1 - 0.75 * x ** 1.6) + 0.35 * x ** 2
        return V
    k.add("ear", ell((0.162, 0.045, 0.34), (0.092, 0.014, 0.076), rot=(0, -12, 34), deform=ear_shape), EAR, "ear{s}",
          mirror=True)
    # the purple stripes up the forehead
    for s in (1, -1):
        dd = unit(0.13 * s, -0.86, 0.5)
        k.decal(on_ell(H, HR, dd), dd, (0.009, 0.026), PURPLE, eps=0.0016, rings=8, segs=36)


def ring_paint(k, c, axis, half, reach, on, mirror=False):
    """A crisp white band round a fused tube, painted between two planes across it (a loose ring stood off it)."""
    c, a = np.array(c, float), unit(*axis)
    def mask(P):
        Q = P.copy()
        if mirror:
            Q[:, 0] = np.abs(Q[:, 0])
        d = (Q - c) @ a
        r = np.linalg.norm((Q - c) - np.outer(d, a), axis=1)
        return inside(np.abs(d) - half, 0.0018) * inside(r - reach, 0.003)
    k.paint(mask, WHITE, on=on)


def body(k):
    def pear(V):
        V = V.copy(); s = 1 - 0.14 * V[:, 2]
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("torso", ell((0, 0.002, 0.232), (0.06, 0.052, 0.07), deform=pear), NAVY, "body")
    k.add("neck", ell((0, -0.004, 0.282), (0.042, 0.04, 0.03)), NAVY, "body")
    k.add("hips", ell((0, 0.006, 0.182), (0.062, 0.052, 0.042)), NAVY, "body")
    # bowed legs: dark thighs, teal shins with a white band, pointed white feet
    k.add("leg", tube([(0.036, 0.0, 0.185), (0.072, -0.024, 0.108), (0.062, -0.004, 0.034)], [0.032, 0.023, 0.021]),
          NAVY, "leg{s}", mirror=True)
    k.paint(lambda P: inside(P[:, 2] - 0.112, 0.003), TEAL, on=["leg"])
    ring_paint(k, (0.067, -0.014, 0.072), (0.1, -0.24, 0.96), 0.008, 0.036, ["leg"], mirror=True)
    k.add("foot", ell((0.064, -0.028, 0.016), (0.026, 0.046, 0.016)), WHITE, "leg{s}", mirror=True)
    # the hanging arm (+X): down by the side, the hand pink, a smear of cyan on it
    k.add("armR", tube([(0.05, 0.0, 0.27), (0.084, -0.028, 0.212), (0.064, -0.062, 0.158)], [0.02, 0.018, 0.017]), NAVY,
          "armR")
    k.add("handR", ell((0.062, -0.068, 0.146), (0.021, 0.022, 0.024)), PINK, "armR")
    for j, (dx, dy) in enumerate(((-0.008, -0.012), (0.01, -0.006))):
        k.add(f"fingerR{j}", tube([(0.062 + dx, -0.07 + dy, 0.14), (0.064 + dx, -0.074 + dy, 0.118)], [0.0095, 0.0085]),
              PINK, "armR")
    k.add("smear", outward(ell((0.08, -0.07, 0.148), (0.009, 0.014, 0.012))), CYAN, "armR", fuse=False, surface="skin")
    # the raised arm (-X): up to the chin, the fingers pink
    k.add("armL", tube([(-0.05, 0.0, 0.27), (-0.094, -0.04, 0.218), (-0.058, -0.098, 0.228)], [0.02, 0.018, 0.017]),
          NAVY, "armL")
    k.add("handL", ell((-0.056, -0.104, 0.232), (0.02, 0.02, 0.019)), PINK, "armL")
    for j, dx in enumerate((-0.008, 0.008)):
        k.add(f"fingerL{j}", tube([(-0.054 + dx, -0.108, 0.236), (-0.05 + dx, -0.112, 0.256)], [0.009, 0.008]), PINK,
              "armL")


def tail(k):
    TP = [(0, 0.05, 0.185), (0.05, 0.12, 0.175), (0.12, 0.15, 0.215), (0.155, 0.14, 0.282), (0.162, 0.122, 0.33)]
    k.add("tail", tube(TP, [0.022, 0.021, 0.021, 0.021, 0.022]), TEAL, "tail")
    for i in (1, 2, 3):
        ring_paint(k, TP[i], np.array(TP[i + 1]) - np.array(TP[i - 1]), 0.0075, 0.034, ["tail"])
    tip = np.array(TP[-1])
    k.add("brushcore", ell(tip + (0, 0, 0.012), (0.03, 0.026, 0.03)), TEAL, "tail")
    side = unit(0.72, 0.69, 0)
    for i, a in enumerate((-70, -38, -8, 22, 52, 82)):
        t = math.radians(a)
        d = unit(*(math.cos(t) * np.array([0, 0, 1.0]) + math.sin(t) * side))
        cap.point(k, f"brush{i}", tip + (0, 0, 0.012) + d * 0.018, d, 0.02, 0.064 - 0.01 * (i % 2), 0.015, TEAL, "tail",
                  up=unit(0.7, -0.7, 0), surface="fur")


def drool(k):
    k.add("drool", outward(tube([(0.004, -0.112, 0.266), (0.014, -0.112, 0.252), (0.018, -0.108, 0.236)],
                                [0.011, 0.008, 0.0085])), CYAN, "head", fuse=False, surface="skin")
    k.add("drop", outward(ell((0.019, -0.106, 0.228), (0.011, 0.01, 0.012))), CYAN, "head", fuse=False, surface="skin")
    k.add("drool2", outward(tube([(-0.012, -0.113, 0.268), (-0.016, -0.11, 0.255)], [0.009, 0.0075])), CYAN, "head",
          fuse=False, surface="skin")


# --- the eyes: a mint lens ringed purple, cut by a purple slant falling toward the middle -----------------------------
A, B = 0.36, 0.38


def lens_ringed():
    lens = shr.lens_eye()
    cp = srgb(PURPLE)
    def look(u, v):
        c = lens(u, v)
        c[(u * u + v * v > 0.78 ** 2) | (v > A + B * u - 0.26)] = cp
        return c
    return look


EYE_DIR = (0.5, -0.85, 0.12)
EYE_HALF = (0.042, 0.037)


def eyes(k, state):
    de = unit(*EYE_DIR)
    E = on_ell(H, HR, de)
    lk = eye_look(state, lens_ringed(), srgb(WHITE), srgb(PURPLE), line=0.18)
    r = cap.cut_outline(A, B)
    m = np.array([-1, 1, 1.0])
    k.decal(E, de, EYE_HALF, lk, eps=0.0022, outline=r, rings=20, segs=56)
    k.decal(E * m, de * m, EYE_HALF, lambda u, v: lk(-u, v), eps=0.0022, outline=lambda th: r(math.pi - th), rings=20,
            segs=56)


def grafaiai(src, out):
    k = Kit(src, coat="fur")
    head(k)
    body(k)
    tail(k)
    drool(k)
    k.build(voxel=VOXEL, smooth=3, band=0.0055, wband=0.032)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    grafaiai(*args())
