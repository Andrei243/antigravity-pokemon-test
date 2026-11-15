# Lileep sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/lileep.py -- <the export's Lileep.glb> <out.glb>
# Written to be grown from: cradily.py imports it (sibling("lileep")) and keeps its frame: the tentacles swept along a
# dense curve with their deeper pink tips as pieces of their own, the yellow spiral marks, the yellow eye spots on black.
#
# From the references: a lavender sea lily. A low lobed foot like a sucker (five rounded toes), a short yellow stalk
# ringed with fine lines, a round goblet of a body narrowing to the stalk, a yellow band round it under its top lip, and
# on its sides yellow spiral marks (a C of a ring round a dot), four round it. In the goblet's mouth a black dome of a
# head with two yellow oval eye spots. Eight thick pink tentacles from the lip: the four behind arch up and out and droop
# at their ends, the four in front come out and hang down over the body's upper front; their tips a deeper pink (the
# art's rose, a little deeper so that it reads; the 3D picture's are magenta).
# Skeleton: body (0, 0, 0.2), head (0, 0, 0.28).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, outward, ring_band, args

PURPLE, YELLOW, YLINE, BLACK = "#A290D8", "#EED27A", "#B89646", "#2A2632"
PINK, TIP = "#F4B2B0", "#E0708E"            # TIP: shared with cradily.py

# --- the frame -------------------------------------------------------------------------------------------------------
BULB_C, BULB_R = np.array([0.0, 0.0, 0.19]), (0.09, 0.09, 0.082)
LIP_Z, LIP_R = 0.252, 0.066
DOME_C, DOME_R = np.array([0.0, -0.006, 0.286]), (0.071, 0.069, 0.06)


def curve(pts, n=28):
    """A dense Catmull-Rom curve through pts (so a tentacle and its tip can share it exactly)."""
    p = np.array(pts, float)
    ext = np.vstack([2 * p[0] - p[1], p, 2 * p[-1] - p[-2]])
    out = []
    per = max(2, n // (len(p) - 1))
    for i in range(len(p) - 1):
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        for j in range(per):
            t = j / per
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                              + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    out.append(p[-1])
    return np.array(out)


def tentacle(k, name, pts, r0, r1, bone="head", tip_share=0.27, colour=PINK, tip=TIP):
    """A thick tentacle swept along pts, tapering r0 to r1, its last share a deeper pink piece a little wider."""
    c = curve(pts)
    n = len(c)
    r = np.linspace(r0, r1, n)
    cut = int(round(n * (1 - tip_share)))
    k.add(name, outward(tube(c[:cut + 1], r[:cut + 1], seg=18, per=2)), colour, bone, fuse=False, surface="skin")
    k.add(name + "tip", outward(tube(c[cut - 1:], r[cut - 1:] + 0.0022, seg=18, per=2)), tip, bone, fuse=False,
          surface="skin")


ANGLES = (32, 74, 116, 160, -160, -116, -74, -32)     # degrees round from the front: a gap left for the face


def crown(k, centre, axis_up, front, roots, back_path, front_path, r0=0.022, r1=0.019, bone="head", angles=ANGLES):
    """
    Eight tentacles round a head: roots[0] out from the axis and roots[1] up, then each path a list of (out, up)
    from its root; the four nearest the front take front_path, the rest back_path. front and axis_up name the frame.
    """
    up = unit(*axis_up)
    fw = np.array(front, float); fw = fw - up * (fw @ up); fw /= np.linalg.norm(fw)
    side = np.cross(fw, up)
    for i, deg in enumerate(angles):
        a = math.radians(deg)
        d = math.cos(a) * fw + math.sin(a) * side
        path = front_path if math.cos(a) > 0 else back_path
        root = np.asarray(centre) + d * roots[0] + up * roots[1]
        pts = [root] + [root + d * o + up * z for o, z in path]
        tentacle(k, f"tentacle{i}", pts, r0, r1, bone)


def spiral_mark(ring=YELLOW, ground=PURPLE, gap_at=1.9, gap=0.75):
    """A yellow C of a ring (open toward gap_at, radians from +u) round a yellow dot."""
    CY, CG = srgb(ring), srgb(ground)

    def mark(u, v):
        rho = np.sqrt(u * u + v * v)
        th = np.arctan2(v, u)
        c = np.tile(CG, (len(u), 1))
        d = np.abs((th - gap_at + math.pi) % (2 * math.pi) - math.pi)
        c[(rho > 0.64) & (rho < 0.98) & (d > gap)] = CY
        c[rho < 0.36] = CY
        return c
    return mark


def eye_spot(colour=YELLOW):
    CY = srgb(colour)

    def eye(u, v):
        return np.tile(CY, (len(u), 1))
    return eye


def spot_eyes(k, state, c, n, half, lid=BLACK, colour=YELLOW):
    """The two yellow spots that are the eyes, on black: shut states draw a yellow line on the black."""
    lk = eye_look(state, eye_spot(colour), srgb(lid), srgb(colour), line=0.2)
    for s in (1, -1):
        col = lk if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(lk)
        k.decal(np.array(c) * (s, 1, 1), unit(*(np.array(n) * (s, 1, 1))), half, col, eps=0.004, rings=20, segs=56)


# --- the parts -------------------------------------------------------------------------------------------------------
def foot(k):
    k.add("foot", ell((0, 0.0, 0.03), (0.06, 0.06, 0.032)), PURPLE, "root")
    for j, a in enumerate((36, -36, 108, -108, 180)):
        d = np.array([math.sin(math.radians(a)), -math.cos(math.radians(a)), 0.0])
        k.add(f"toe{j}", ell(d * 0.06 + (0, 0, 0.025), (0.033, 0.031, 0.025), rot=(0, 0, -a)), PURPLE, "root")
    k.add("footcone", ell((0, 0, 0.062), (0.036, 0.036, 0.034)), PURPLE, "root")


def stalk(k):
    k.add("stalk", tube([(0, 0, 0.07), (0, 0, 0.1), (0, 0, 0.135)], [0.021, 0.02, 0.021]), YELLOW, "body")
    k.paint(lambda P: inside(P[:, 2] - 0.088, 0.003) * inside(np.hypot(P[:, 0], P[:, 1]) - 0.03, 0.004), PURPLE)
    for j, z in enumerate((0.098, 0.111, 0.124)):
        k.add(f"stalkring{j}", ring_band((0, 0, z), (0, 0, 1), 0.0205, 0.0045, thick=0.004), YLINE, "body",
              fuse=False, surface="shell")


def bulb(k):
    def goblet(V):
        V = V.copy()
        low = np.clip(-V[:, 2], 0, 1)
        f = 1 - 0.55 * low ** 1.6
        V[:, 0] *= f; V[:, 1] *= f
        return V
    k.add("bulb", ell(BULB_C, BULB_R, deform=goblet), PURPLE, "body")
    k.add("lip", ell((0, 0, LIP_Z), (LIP_R, LIP_R, 0.024)), PURPLE, "body")
    # the yellow band under the lip
    k.paint(lambda P: inside(P[:, 2] - 0.246, 0.0025) * inside(0.232 - P[:, 2], 0.0025)
            * inside(0.05 - np.hypot(P[:, 0], P[:, 1]), 0.004), YELLOW)
    # four spiral marks round the sides
    for a in (52, -52, 130, -130):
        d = np.array([math.sin(math.radians(a)), -math.cos(math.radians(a)), -0.05])
        p = BULB_C + np.array([d[0] * 0.088, d[1] * 0.088, -0.006])
        gap = 1.9 if a > 0 else math.pi - 1.9
        k.decal(p, unit(*d), (0.031, 0.031), spiral_mark(gap_at=gap), eps=0.003, rings=22, segs=72)


def head(k):
    k.add("dome", ell(DOME_C, DOME_R), BLACK, "head")


EYE_C, EYE_N, EYE_HALF = (0.03, -0.064, 0.312), (0.47, -0.88, 0.45), (0.02, 0.025)


def lileep(src, out):
    k = Kit(src, coat="shell")
    foot(k)
    stalk(k)
    bulb(k)
    head(k)
    crown(k, (0, 0, LIP_Z), (0, 0, 1), (0, -1, 0), (0.066, 0.012),
          back_path=[(0.03, 0.06), (0.075, 0.105), (0.115, 0.1), (0.13, 0.06)],
          front_path=[(0.045, 0.03), (0.085, 0.025), (0.1, -0.02), (0.1, -0.065)])
    k.build(voxel=0.005, smooth=4, band=0.005)
    k.eye_states(lambda st: spot_eyes(k, st, EYE_C, EYE_N, EYE_HALF))
    k.export(out)


if __name__ == "__main__":
    lileep(*args())
