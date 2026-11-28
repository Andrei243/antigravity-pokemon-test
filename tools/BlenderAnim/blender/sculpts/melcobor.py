# Melcobor sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/melcobor.py -- <the export's Melcobor.glb> <out.glb>
# Carpathia No. 10044 (plan 46), the limestone snail, Bug/Rock. Grown from melcut.py (sibling("melcut")): its foot,
# its stalks, its eyes and its coil kept. From its picture: a heavy snail whose shell is the design, some four fifths
# of its height and of its length, a spiral of grey limestone with raised whorls ribbed across at its middle, growth
# lines and cracks over the outer whorl, and ammonite fossils pressed into it (a large one high at the front, smaller
# ones about the rest); a thick grey-brown body, wrinkled, its front a tan throat folded in horizontal rolls; a tan rim
# round the foot; two short thick eye stalks with small dark eyes; three thick feelers hanging over its mouth.
# The skeleton: body (0, 0, 0.18), head (0, -0.25, 0.3), eye stalks earL/earR (-+0.035, -0.28, 0.41),
# shell (0, 0.1, 0.4).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args, axes, sibling

mc = sibling("melcut")

BODY, BODY_D = "#8C857A", "#77706A"
TAN, TAN_D = "#C2A07C", "#A88A68"
STONE, RIDGE, DARK = "#A6A59E", "#84837C", "#5E5C57"
MOUTHC = "#4E3E34"

SHELL_C, SHELL_R = np.array([0.0, 0.095, 0.40]), np.array([0.2, 0.285, 0.268])
COIL_C = SHELL_C + np.array([0.0, 0.04, -0.03])
COIL_R = 0.27
HEAD_C, HEAD_R = np.array([0.0, -0.238, 0.345]), np.array([0.105, 0.088, 0.08])


def shell_x(y, z):
    """The shell's surface x at (y, z) on its +X side."""
    q = 1 - ((y - SHELL_C[1]) / SHELL_R[1]) ** 2 - ((z - SHELL_C[2]) / SHELL_R[2]) ** 2
    return SHELL_R[0] * math.sqrt(max(q, 0.0))


def whorl_ridge(k, turn=0.56, r0=0.08, r1=0.66, radius=0.022):
    """The suture of the coil raised as a fused ridge on each side, from the middle out to r1 of the coil."""
    pts, rr = [], []
    th = 0.0
    while True:
        r = turn ** (-th / (2 * math.pi)) * r0
        if r > r1:
            break
        y = COIL_C[1] + COIL_R * r * math.cos(th)
        z = COIL_C[2] + COIL_R * r * math.sin(th)
        x = shell_x(y, z) - radius * 0.2
        pts.append((x, y, z)); rr.append(radius * (0.7 + 0.5 * r))
        th += 0.35
    k.add("whorl", tube(pts, rr, per=4), STONE, "shell", mirror=True, surface="shell")


def fossil_at(k, name, c, n, radius):
    a, b = axes(n)
    V, F = ell((0, 0, 0), (radius, radius, radius * 0.28))
    V = V @ np.stack([a, b, n], axis=1).T + (c - n * radius * 0.12)
    k.add(name, (V, F), STONE, "shell", surface="shell")
    def frame(P):
        d = P - c
        h = d @ n
        q1, q2 = d @ a, d @ b
        r = np.sqrt(q1 * q1 + q2 * q2) / radius + 1e-4
        th = np.arctan2(q2, q1)
        near = inside(np.abs(h) - radius * 0.6, 0.004)
        return r, th, near
    def ribs(P):
        r, th, near = frame(P)
        rib = inside(np.abs(mc.frac(th * 16 / (2 * math.pi)) - 0.5) - 0.17, 0.05)
        return rib * (r > 0.55) * inside(r - 0.98, 0.03) * near
    def coilline(P):
        r, th, near = frame(P)
        ph = np.log(r) / math.log(0.5) + th / (2 * math.pi)
        line = inside(np.abs(mc.frac(ph + 0.5) - 0.5) - 0.12, 0.03) * inside(r - 0.6, 0.03)
        ring = inside(np.abs(r - 1.0) - 0.08, 0.03)
        return (line + ring) * near
    k.paint(ribs, DARK, on=[name])
    k.paint(coilline, DARK, on=[name, "shell"])


def build(src, out):
    k = Kit(src, coat="skin")
    # the foot, the column into the head, the hump under the shell
    mc.foot(k, "foot", -0.33, 0.39, 0.075, 0.15, 0.075, BODY)
    k.add("column", tube([(0, -0.2, 0.11), (0, -0.218, 0.22), (0, -0.232, 0.31)], [0.112, 0.108, 0.098]), BODY, "head")
    k.add("hump", ell((0, 0.06, 0.16), (0.15, 0.24, 0.09)), BODY, "body")
    k.add("head", ell(HEAD_C, HEAD_R), BODY, "head")
    k.add("snout", ell((0, -0.29, 0.325), (0.078, 0.05, 0.058)), BODY, "head")
    # three thick feelers hanging over the mouth
    k.add("feeler", tube([(0.058, -0.318, 0.33), (0.072, -0.34, 0.3), (0.074, -0.342, 0.255)], [0.02, 0.018, 0.016]),
          BODY, "head", mirror=True)
    k.add("lobe", tube([(0.0, -0.33, 0.33), (0.0, -0.345, 0.298), (0.0, -0.343, 0.272)], [0.02, 0.019, 0.017]), BODY, "head")
    # the eye stalks: short and thick
    ball = mc.stalks(k, (0.04, -0.25, 0.41), (0.055, -0.268, 0.455), (0.07, -0.282, 0.48), 0.02, 0.016, 0.03, BODY)

    # the great shell of limestone
    k.add("shell", ell(SHELL_C, SHELL_R), STONE, "shell", surface="shell")
    whorl_ridge(k)
    phases = mc.coil(COIL_C, COIL_R, bands=18, curve=0.0)
    def suture(P):
        r, ph, band = phases(P)
        return inside(np.abs(mc.frac(ph + 0.5) - 0.5) - 0.05, 0.02) * (r < 0.7) * (r > 0.07)
    def ribs(P):
        r, ph, band = phases(P)
        return inside(np.abs(mc.frac(band) - 0.5) - 0.08, 0.05) * inside(r - 0.6, 0.02) * (r > 0.14)
    def growth(P):
        r, ph, band = phases(P)
        g = inside(np.abs(mc.frac(band * 0.5 + 0.13 * np.sin(r * 9)) - 0.5) - 0.035, 0.05)
        return g * smoothstep(0.7, 0.76, r) * 0.8
    k.paint(ribs, RIDGE, on=["shell", "whorl"])
    k.paint(growth, RIDGE, on=["shell"])
    k.paint(suture, DARK, on=["shell", "whorl"])
    # a few cracks over the outer whorl, plain lines in the stone
    def cracks(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        m = np.zeros(len(P))
        for (y0, z0, ang, ln) in ((-0.05, 0.56, 0.3, 0.09), (0.25, 0.52, -0.6, 0.08), (0.0, 0.3, 0.1, 0.07)):
            dy, dz = y - y0, z - z0
            along = dy * math.cos(ang) + dz * math.sin(ang)
            across = -dy * math.sin(ang) + dz * math.cos(ang) - 0.012 * np.sin(along * 70)
            m = np.maximum(m, inside(np.abs(across) - 0.0035, 0.002) * inside(np.abs(along) - ln, 0.004))
        return m
    k.paint(cracks, DARK, on=["shell"])
    # the fossils pressed in: unlike on the two sides
    places = [(+1, (-0.07, 0.56), 0.075), (+1, (0.27, 0.53), 0.035), (+1, (0.06, 0.24), 0.04), (+1, (0.3, 0.3), 0.034),
              (-1, (-0.02, 0.6), 0.055), (-1, (0.26, 0.42), 0.045), (-1, (-0.06, 0.26), 0.036)]
    for i, (side, (y, z), r) in enumerate(places):
        x = shell_x(y, z) * side
        c = np.array([x, y, z])
        n = np.array([x / SHELL_R[0] ** 2, (y - SHELL_C[1]) / SHELL_R[1] ** 2, (z - SHELL_C[2]) / SHELL_R[2] ** 2])
        n /= np.linalg.norm(n)
        fossil_at(k, f"fossil{i}", c, n, r)
    # one more on the shell's back, for the camera behind
    d = unit(0.25, 0.75, 0.55)
    p = SHELL_C + d / math.sqrt(((d / SHELL_R) ** 2).sum())
    n = (p - SHELL_C) / SHELL_R ** 2; n /= np.linalg.norm(n)
    fossil_at(k, "fossilback", p, n, 0.045)

    # the tan throat folded in rolls, the tan rim of the foot
    def throat(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        front = inside(y + 0.285 - 0.3 * (x / 0.085) ** 2, 0.006)
        return front * inside(z - 0.3, 0.008) * inside(np.abs(x) - 0.08, 0.01) * (z > 0.035)
    k.paint(throat, TAN, on=["column", "foot", "snout"])
    def rolls(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        front = inside(y + 0.285 - 0.3 * (x / 0.085) ** 2, 0.006)
        roll = inside(np.abs(mc.frac(z / 0.042 + 0.08 * np.sin(x * 40)) - 0.5) - 0.07, 0.04)
        return front * roll * inside(z - 0.29, 0.01) * (z > 0.05) * inside(np.abs(x) - 0.075, 0.01)
    k.paint(rolls, TAN_D, on=["column"])
    k.paint(lambda P: inside(P[:, 2] - 0.03, 0.006), TAN, on=["foot", "column"])
    k.paint(lambda P: inside(P[:, 2] - 0.012, 0.004), TAN_D, on=["foot", "column"])
    # wrinkles over the grey body
    def wrinkles(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        w = inside(np.abs(mc.frac(y / 0.06 + 0.4 * np.sin(z * 30) + 0.3 * x / 0.15) - 0.5) - 0.06, 0.03)
        return w * (z > 0.06) * 0.85
    k.paint(wrinkles, BODY_D, on=["foot", "hump"])

    # the mouth: a short wavy line between the feelers
    M, B = srgb(MOUTHC), srgb(TAN)
    def mouth(u, v):
        c = np.tile(B, (len(u), 1))
        c[(np.abs(v - 0.18 * np.sin(u * 5.5)) < 0.3) & (np.abs(u) < 0.88)] = M
        return c
    k.decal((0, -0.335, 0.286), unit(0, -1, -0.2), (0.05, 0.014), mouth, eps=0.0025, rings=8, segs=48)

    k.build(voxel=0.006, smooth=4, band=0.008)

    n = unit(0.3, -0.92, 0.25)
    k.eye_states(lambda st: mc.lay_eyes(k, st, ball, 0.03, n, (0.022, 0.022), lid=BODY, pupil=0.55))
    k.export(out)


if __name__ == "__main__":
    build(*args())
