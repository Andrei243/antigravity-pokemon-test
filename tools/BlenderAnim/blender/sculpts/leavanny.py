# Leavanny sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/leavanny.py -- <the export's Leavanny.glb> <out.glb>
# Grown from sewaddle.py (its yellow, its greens, its ink and white).
#
# The design, from the references: a slim upright mantis that sews leaves. A big yellow head, wider than tall, with
# small red eyes low on its sides (a dark rim, a white glint) and a long thin smile between them; two yellow feelers
# from its crown that broaden into curved yellow blades; a ruff of round green leaf lobes behind the head, rising above
# it on both sides; a short dark green neck, yellow points at the shoulders, a narrow dark green thorax; arms whose
# forearms are long light green leaf blades, each with a hooked notch near its end; a dark green abdomen of stacked
# plates hanging below the waist, light green leaves hanging down its sides from the waist; short yellow pointed legs
# showing under it.
# Skeleton: body (0, 0, 0.32), arms (+-0.035, 0, 0.4), head (0, -0.01, 0.45), legs (+-0.03, 0, 0.22).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, leaf, slab, srgb, inside, smoothstep, unit, eye_look, args, outward,
                       sibling, polygon)

sw = sibling("sewaddle")
YELLOW, INK, WHITE = sw.YELLOW, sw.INK, sw.WHITE
DARK, PLATE, RUFF, RUFF2, LIGHT = "#2F7C36", "#3E9140", "#7DBE40", "#6CB23A", "#A6D64C"
RED, REDRIM = "#E23C46", "#4A1418"

HC, HR = np.array([0.0, -0.03, 0.555]), np.array([0.112, 0.088, 0.088])     # the head
TH_C, TH_R = np.array([0.0, -0.005, 0.41]), np.array([0.046, 0.04, 0.05])   # the thorax
AB_C, AB_R = np.array([0.0, 0.01, 0.24]), np.array([0.07, 0.066, 0.145])   # the abdomen
SHOULDER = np.array([0.045, -0.005, 0.425])
ELBOW = np.array([0.1, -0.06, 0.375])


def on_head(d):
    d = np.array(d, float); d /= np.linalg.norm(d)
    p = HC + d / math.sqrt(((d / HR) ** 2).sum())
    n = (p - HC) / HR ** 2
    return p, n / np.linalg.norm(n)


def taper(V):
    """The abdomen: broadest a third of the way down, narrowing to a blunt point below."""
    V = V.copy()
    s = 1 - 0.45 * np.clip(-V[:, 2], 0, 1) ** 1.5 - 0.15 * np.clip(V[:, 2], 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    return V


def add_body(k):
    k.add("neck", tube([(0, -0.01, 0.43), (0, -0.02, 0.49)], [0.024, 0.024]), DARK, "head", surface="leaf")
    k.add("thorax", ell(TH_C, TH_R), DARK, "body", surface="leaf")
    k.add("waist", ell((0, 0.0, 0.355), (0.036, 0.034, 0.03)), DARK, "body", surface="leaf")
    k.add("abdomen", ell(AB_C, AB_R, deform=taper), DARK, "body", surface="leaf")
    # the legs' yellow kept off the abdomen where they go in under it
    def above_legs(P):
        q = (P - AB_C) / (AB_R * 1.15)
        return inside(0.098 - P[:, 2], 0.003) * inside((q ** 2).sum(1) - 1.0, 0.01)
    k.paint(above_legs, DARK)
    # the stacked plates: lighter bands down the abdomen, each plate's lower edge darker
    def plates(P):
        z = P[:, 2]
        t = ((0.345 - z) / 0.048) % 1.0
        return inside(t - 0.62, 0.03) * inside(z - 0.345, 0.003) * inside(0.1 - z, 0.003)
    k.paint(plates, PLATE, on=["abdomen"])
    # yellow points over the shoulders, where the arms meet the thorax
    k.add("shoulder", ell(SHOULDER + (0.006, 0, 0.004), (0.03, 0.026, 0.022), rot=(0, -20, 0)), YELLOW, "arm{s}",
          mirror=True)


def add_legs(k):
    # short yellow legs out under the abdomen, each to a point
    k.add("leg", tube([(0.03, 0.0, 0.2), (0.042, -0.006, 0.11), (0.046, -0.014, 0.05), (0.044, -0.02, 0.008)],
                      [0.026, 0.022, 0.016, 0.006]), YELLOW, "leg{s}", mirror=True)


def add_arms(k):
    k.add("upper", tube([SHOULDER, (SHOULDER + ELBOW) / 2, ELBOW], [0.02, 0.018, 0.018]), RUFF2, "arm{s}",
          mirror=True, surface="leaf")
    k.add("elbow", ell(ELBOW, (0.022, 0.022, 0.022)), RUFF2, "arm{s}", mirror=True, surface="leaf")
    # the forearm: a long flat leaf blade forward and down from the elbow, its plane turned out so its face shows,
    # a hooked notch in its lower edge near the end
    outline = [(-0.01, 0.014), (0.05, 0.03), (0.11, 0.036), (0.16, 0.026), (0.19, 0.006), (0.192, -0.008),
               (0.178, -0.024), (0.162, -0.03), (0.156, -0.016), (0.146, -0.008), (0.134, -0.014), (0.128, -0.03),
               (0.08, -0.03), (0.03, -0.022), (-0.01, -0.012)]
    for s, side in ((1, "R"), (-1, "L")):
        e = ELBOW * np.array([s, 1, 1])
        ex = unit(0.2 * s, -0.88, -0.42)                  # along the blade: forward and down
        ez = np.cross(ex, unit(0.55 * s, 0.35, 0.0))       # across it, the face turned out to the side and front
        k.add(f"blade{side}", slab(outline, e, ex, ez, 0.022), LIGHT, f"arm{side}", fuse=False, surface="leaf")


def add_skirt(k):
    # light green leaves hanging from the waist down the abdomen's sides and back
    for s, side in ((1, "R"), (-1, "L")):
        for j, (root, d, length, width) in enumerate((((0.052, 0.012, 0.345), (0.32, 0.18, -1.0), 0.25, 0.055),
                                                     ((0.03, 0.05, 0.35), (0.16, 0.42, -1.0), 0.23, 0.05))):
            r = np.array(root) * (s, 1, 1)
            dd = np.array(d) * (s, 1, 1)
            k.add(f"skirt{j}{side}", outward(leaf(r, dd, length, width, thick=0.018)), LIGHT if j == 0 else RUFF,
                  "body", fuse=False, surface="leaf")


def add_head(k):
    k.add("head", ell(HC, HR), YELLOW, "head")
    k.add("jaw", ell(HC + (0, -0.012, -0.03), (0.09, 0.07, 0.06)), YELLOW, "head")
    # the feelers: from the crown up and out, broadening into curved yellow blades with their faces to the front
    k.add("feeler", outward(blade([(0.03, -0.05, 0.625), (0.045, -0.045, 0.67), (0.062, -0.035, 0.72),
                                   (0.074, -0.022, 0.77), (0.078, -0.012, 0.8)],
                                  [0.012, 0.016, 0.03, 0.027, 0.006], (0, 1, 0), 0.4)), YELLOW, "head", fuse=False,
          mirror=True)


def add_ruff(k):
    """Round leaf lobes behind the head, standing out round it and rising above it on both sides."""
    # the hood's back: a broad cupped leaf over the back of the head, so from behind the head is green
    def back_cup(V):
        V = V.copy()
        V[:, 1] -= 2.0 * (V[:, 0] ** 2 + V[:, 2] ** 2)
        return V
    k.add("ruffback", outward(ell(HC + np.array([0, 0.098, 0.012]), (0.122, 0.02, 0.112), deform=back_cup, sub=3)),
          RUFF2, "head", fuse=False, surface="leaf")
    lobes = [(55, 0.13, 0.07, RUFF), (88, 0.1, 0.06, RUFF2), (20, 0.12, 0.065, RUFF2), (-18, 0.11, 0.062, RUFF),
             (-50, 0.085, 0.05, RUFF2)]
    for s, side in ((1, "R"), (-1, "L")):
        for j, (ang, reach, rad, col) in enumerate(lobes):
            if ang > 80 and s < 0:
                continue
            a = math.radians(ang)
            d = np.array([s * math.cos(a), 0.0, math.sin(a)]) if ang < 80 else np.array([0, 0, 1.0])
            c = HC + np.array([0, 0.045, 0]) + d * reach
            # flat across the y axis, cupped a little forward
            def cup(V):
                V = V.copy()
                V[:, 1] -= 2.2 * (V[:, 0] ** 2 + V[:, 2] ** 2)
                return V
            k.add(f"ruff{j}{side}", outward(ell(c, (rad * 1.05, 0.016, rad), rot=(0, -ang * s if ang < 80 else 0, 0),
                                                deform=cup, sub=3)), col, "head", fuse=False, surface="leaf")


def add_mouth(k):
    CY, CI = srgb(YELLOW), srgb("#5A3A20")
    def smile(u, v):
        c = np.tile(CY, (len(u), 1))
        c[(np.abs(v - (0.3 - 0.75 * (1 - u * u))) < 0.3) & (np.abs(u) < 0.95)] = CI
        return c
    p, n = on_head((0, -1, -0.42))
    k.decal(p, n, (0.06, 0.016), smile, eps=0.004, rings=12, segs=64)


EYE_D = (0.66, -0.72, -0.12)
EYE_HALF = (0.019, 0.023)


def eye_open(u, v, red=srgb(RED), rim=srgb(REDRIM), white=srgb(WHITE)):
    c = np.tile(red, (len(u), 1))
    c[u * u + v * v > 0.7] = rim
    c[((u + 0.12) / 0.3) ** 2 + ((v - 0.3) / 0.3) ** 2 < 1] = white
    return c


def eyes(k, state):
    look = eye_look(state, eye_open, srgb(YELLOW), srgb(REDRIM), line=0.18)
    p, n = on_head(EYE_D)
    k.decal(p, n, EYE_HALF, look, eps=0.005, rings=20, segs=56, mirror=True)


def leavanny(src, out):
    k = Kit(src, coat="leaf")
    add_body(k)
    add_legs(k)
    add_arms(k)
    add_skirt(k)
    add_head(k)
    add_ruff(k)
    add_mouth(k)
    k.build(voxel=0.005, smooth=3)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    leavanny(*args())
