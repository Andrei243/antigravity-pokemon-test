# Woobat sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/woobat.py -- <the export's Woobat.glb> <out.glb>
# A round ball of pale blue fur bristling with soft spikes, darker below; a round pink nose high on the face with a
# black heart for its nostrils, a wide open mouth under it (dark, a pink tongue, one white fang hanging from its top
# middle), its eyes no more than two dark W's of fur at the nose's sides; two small charcoal bat wings raised up and
# out from the top of the ball, their trailing edges scalloped. Written to be grown from: Swoobat imports it
# (sibling("woobat")) and keeps its fur spikes, its heart nose and its wings.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, smoothstep, unit, eye_look, polygon, args

# --- the line's colours ---------------------------------------------------------------------------------------------
BLUE, BLUELOW = "#A8CDE6", "#86A9D0"
PINK, HEART = "#EE9AAA", "#141216"
WING = "#3E3B44"
MOUTH, TONGUE, FANG = "#5C1E28", "#EA8494", "#F8F6F4"
WMARK = "#2C3858"

# --- the frame ------------------------------------------------------------------------------------------------------
C = np.array([0.0, -0.01, 0.15])                  # the ball, over the body's joint (0, 0, 0.146)
CR = np.array([0.104, 0.1, 0.1])
FACE = ((0.0, -0.05, 0.158), (0.084, 0.06, 0.08))  # a flattened front on the head's joint (0, -0.07, 0.156)
NOSE = np.array([0.0, -0.104, 0.182])
NOSER = (0.037, 0.017, 0.034)
SPIKE = (0.032, 0.0045, 0.034)                    # a spike's root radius, tip radius and length past the ball
VOXEL = 0.0034


def heart_outline(n=72):
    """A heart's outline in u, v (about -1..1), its middle the patch's middle: star-shaped about it."""
    t = np.linspace(0, 2 * math.pi, n, endpoint=False)
    x = 16 * np.sin(t) ** 3
    y = 13 * np.cos(t) - 5 * np.cos(2 * t) - 2 * np.cos(3 * t) - np.cos(4 * t)
    y = y + 2.5                                   # its middle where the lobes and the point balance
    s = max(np.abs(x).max(), np.abs(y).max())
    return polygon(list(zip(x / s, y / s)))


def fibonacci(n):
    i = np.arange(n) + 0.5
    phi = np.arccos(1 - 2 * i / n)
    th = math.pi * (1 + 5 ** 0.5) * i
    return np.stack([np.cos(th) * np.sin(phi), np.sin(th) * np.sin(phi), np.cos(phi)], axis=1)


def spikes(k, name, c, r, n, colour, bone, keep, spike=SPIKE, droop=0.0, seed=0):
    """Soft cones of fur standing out of a ball of radii r about c, where keep(direction) holds."""
    rs = np.random.RandomState(seed)
    c = np.array(c, float); r = np.array(r, float)
    for i, d in enumerate(fibonacci(n)):
        d = d + rs.normal(0, 0.09, 3)
        d /= np.linalg.norm(d)
        if not keep(d):
            continue
        out = d + np.array([0, 0, -droop])
        out /= np.linalg.norm(out)
        root = c + d * r * 0.78
        surf = c + d / np.linalg.norm(d / r)
        L = spike[2] * (0.8 + 0.4 * rs.rand())
        tip = surf + out * L
        k.add(f"{name}{i}", tube([root, surf + out * L * 0.45, tip], [spike[0], spike[0] * 0.55, spike[1]]),
              colour, bone)


def scallop(p0, p1, toward, depth, m=8):
    """Points from p0 to p1 (not p1), the edge bowed toward a point by depth: a bat wing's trailing edge."""
    p0, p1, toward = np.array(p0, float), np.array(p1, float), np.array(toward, float)
    mid = (p0 + p1) / 2
    ctl = mid + (toward - mid) * depth
    t = np.linspace(0, 1, m, endpoint=False)[:, None]
    return list((1 - t) ** 2 * p0 + 2 * (1 - t) * t * ctl + t * t * p1)


def wing_outline(root, top, tip, points, wrist, lead=0.15, depth=0.45):
    """A bat wing's outline: from the root up the leading edge to its top, out to the tip, then scalloped back down
    between the trailing points to the root."""
    out = []
    out += scallop(root, top, wrist, -lead)            # bowed outward: the arm
    out += scallop(top, tip, wrist, -0.1)
    rim = [tip] + list(points) + [root]
    for a, b in zip(rim, rim[1:]):
        out += scallop(a, b, wrist, depth)
    return [tuple(p) for p in out]


def wing(k, name, bone, origin, ex, ez, outline, thick, colour=WING):
    """The wing as a plate with a hard rim, mirrored to the model's left; its frame is right-handed by slab."""
    k.add(name, slab(outline, origin, ex, ez, thick), colour, bone, fuse=False, mirror=True, surface="leather")


def woobat(src, out):
    k = Kit(src, coat="fur")
    # the ball and its flattened face
    k.add("ball", ell(C, CR), BLUE, "body")
    k.add("face", ell(*FACE), BLUE, "head")
    front = unit(0, -1, 0.12)
    spikes(k, "spike", C, CR, 74, BLUE, "body", lambda d: d @ front < 0.5)
    # the nose: a round pink knob high on the face
    k.add("nose", ell(NOSE, NOSER), PINK, "head")

    # the wings: charcoal plates raised up and out from the ball's upper sides
    root = np.array((0.0, 0.0)); top = np.array((0.03, 0.125)); tip = np.array((0.148, 0.172))
    pts = [np.array((0.13, 0.085)), np.array((0.085, 0.04))]
    outline = wing_outline(root, top, tip, pts, np.array((0.06, 0.11)), lead=0.25)
    wing(k, "wing", "wing{s}", (0.058, 0.035, 0.192), unit(1, 0.32, 0), unit(0, 0.22, 1), outline, 0.011)

    # darker fur below
    k.paint(lambda P: smoothstep(C[2] - 0.02, C[2] - 0.085, P[:, 2]), BLUELOW, on=["ball", "spike"])

    # the black heart on the nose
    k.decal(NOSE + np.array([0, -NOSER[1] + 0.001, 0.0]), unit(0, -1, 0.12), (0.027, 0.026), HEART,
            outline=heart_outline(), rings=16, segs=72, eps=0.003)

    # the mouth: wide and open, its top edge lifting at the corners, a pink tongue in its lower part and one white
    # fang hanging from the middle of its top
    CM, CT, CF = srgb(MOUTH), srgb(TONGUE), srgb(FANG)
    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        c[(u / 0.62) ** 2 + ((v + 0.95) / 0.62) ** 2 < 1] = CT
        top = 0.55 + 0.2 * u * u
        fang = (v > 0.05) & (np.abs(u) < 0.2 * np.clip((v - 0.05) / (top - 0.05), 0, 1))
        c[fang] = CF
        return c
    MOUTHO = polygon([(-1, 0.78), (-0.5, 0.6), (0, 0.55), (0.5, 0.6), (1, 0.78), (0.88, 0.2), (0.62, -0.45),
                      (0.3, -0.85), (0, -0.95), (-0.3, -0.85), (-0.62, -0.45), (-0.88, 0.2)])
    k.decal((0, -0.101, 0.11), unit(0, -1, -0.3), (0.05, 0.034), mouth, outline=MOUTHO, rings=18, segs=72, eps=0.003)

    k.build(voxel=VOXEL, smooth=3)

    # the eyes: a dark W of fur on each side of the nose; the open eye is the W (they are always shut)
    LID, INK = srgb(BLUE), srgb(WMARK)
    def wline(u, tilt=0.0):
        return 0.3 * (2 * np.abs(2 * ((u / 0.8 + 0.5) - np.floor(u / 0.8 + 1.0))) - 1) + tilt * u
    def w_look(tilt):
        def look(u, v):
            c = np.tile(LID, (len(u), 1))
            c[(np.abs(v - wline(u, tilt)) < 0.37) & (np.abs(u) < 0.88)] = INK
            return c
        return look
    ED = unit(0.66, -0.72, 0.22)
    E = C + ED / np.linalg.norm(ED / CR)
    def eyes(state):
        if state == "Open":
            look = w_look(0.0)
        elif state == "Fierce":
            look = w_look(0.45)                     # the W tilted, high at the outer end, low toward the nose
        else:
            look = eye_look(state, w_look(0.0), LID, INK, line=0.2)
        k.decal(E, ED, (0.026, 0.018), look, rings=20, segs=56, eps=0.004, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    woobat(*args())
