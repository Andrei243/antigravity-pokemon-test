# Slowbro sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/slowbro.py -- <the export's Slowbro.glb> <out.glb>
#
# Grown from slowpoke.py: its head (pink, the cream bill over the open mouth, the fangs, the nostrils, the round eyes
# with their tiny pupils, the ears with their curls), its claws and colours. From the references: Slowpoke stood up on
# two thick short legs, a heavy pear of a body with a cream belly ringed by brown lines, short arms resting forward on
# the belly with three white claws, white claws on the feet; its tail curls round behind to the +X side and ends in a
# Shellder clamped on it: a pale grey-blue spiral shell of whorls narrowing to a point, rising up and back, solid
# spikes standing out of the whorls, darker blue-grey spots, and a cross face of its own (white eyes with black
# pupils under a dark brow) over the dark mouth where the tail goes in.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, axes, args, sibling

sp = sibling("slowpoke")
PINK, CREAM = sp.PINK, "#EEE0B2"
RIB = "#C7B285"
SHELL, SPOT, SHELL_MOUTH, SHELL_INK = "#A8C4D8", "#7690B2", "#40303E", "#1A1820"


def shellder(k, m, a, bone):
    """A Shellder clamped on the tail's end at m, its spire rising along a: whorls, spikes, spots, a face."""
    a = unit(*a)
    e1, e2 = axes(a)
    # the whorls: each smaller and set off to one side round the axis, so the shell spirals up to its point
    WH = [(0.0, 0.13, 0.0), (0.085, 0.112, 0.6), (0.16, 0.088, 1.9), (0.225, 0.064, 3.2), (0.275, 0.042, 4.5),
          (0.31, 0.024, 5.8)]
    rot = np.stack([e1, e2, a], axis=1)            # local (x, y, along) to world
    whorls = []
    for i, (t, r, ph) in enumerate(WH):
        off = 0.022 * (1 - t / 0.33) * (math.cos(ph) * e1 + math.sin(ph) * e2)
        c = m + a * (t + 0.05) + off
        V, F = ell((0, 0, 0), (r, r, r * 0.72), sub=4)
        k.add(f"shell{i}", (V @ rot.T + c, F), SHELL, bone, surface="shell")
        whorls.append((c, r, ph))
    # the spikes: solid cones out of the whorls, leaning up the spire, never thin plates
    for i, (wi, ang) in enumerate([(0, 0.4), (0, 2.6), (1, 1.5), (1, 4.0), (2, 0.2), (2, 2.9), (3, 1.8), (0, 4.9)]):
        c, r, _ = whorls[wi]
        out = math.cos(ang) * e1 + math.sin(ang) * e2
        root = c + out * r * 0.8
        tip = c + out * r * 1.45 + a * r * 0.5
        side = np.cross(out, a); side /= np.linalg.norm(side)
        w = 0.036 + 0.012 * (wi == 0)
        pts = [root + w * (math.cos(q) * side + math.sin(q) * a) for q in np.linspace(0, 2 * math.pi, 7)[:-1]]
        pts = pts + [root - out * w * 0.5, tip]
        k.add(f"spike{i}", hull(pts), SHELL, bone, fuse=False, surface="shell")
    # darker spots scattered over the whorls
    rs = np.random.RandomState(80)
    for i in range(9):
        wi = i % 4
        c, r, _ = whorls[wi]
        ang = rs.uniform(0, 2 * math.pi)
        p = c + r * (math.cos(ang) * e1 + math.sin(ang) * e2) * 0.98 + a * rs.uniform(-0.3, 0.4) * r
        s = rs.uniform(0.016, 0.026)
        k.paint(lambda P, p=p, s=s: inside(np.linalg.norm(P - p, axis=1) - s, 0.004), SPOT, on=["shell"])
    # the dark mouth round the tail where it goes in, on the base whorl's underside
    k.paint(lambda P: inside(np.linalg.norm(P - (m + a * 0.01), axis=1) - 0.085, 0.004)
            * inside((P - m) @ a - 0.035, 0.004), SHELL_MOUTH, on=["shell0"])
    return whorls


def shell_eyes(k, c, n):
    """The Shellder's cross face: two white eyes with black pupils under a dark brow sloping to the middle."""
    n = unit(*n)
    right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right)
    up = np.cross(n, right)
    for sx in (1, -1):
        def look(u, v, sx=sx):
            uu = u * sx                                   # + toward the shell's outer side for each eye
            col = np.tile(srgb("#F6F6F2"), (len(u), 1))
            brow = 0.12 + 0.5 * uu                        # high at the outer side, low at the inner: a scowl
            col[v > brow] = srgb(SHELL)
            col[np.abs(v - brow) < 0.16] = srgb(SHELL_INK)
            col[((uu + 0.1) / 0.3) ** 2 + ((v + 0.2) / 0.36) ** 2 < 1] = srgb(SHELL_INK)
            col[u * u + v * v > 0.8] = srgb(SHELL_INK)
            col[(v > brow + 0.2) & (u * u + v * v > 0.8)] = srgb(SHELL)
            return col
        k.decal(c + right * 0.046 * sx, n, (0.04, 0.03), look, eps=0.004, rings=20, segs=56)


def slowbro(src, out):
    k = Kit(src, coat="scales")
    HC = np.array([0, -0.05, 0.635])
    spec = sp.slow_head(k, HC, 1.22)
    # The body: a heavy pear, widest low, the head set on it with no neck to speak of
    def pear(V):
        V = V.copy()
        s = 1 + 0.12 * np.clip(-V[:, 2], 0, 1) - 0.08 * np.clip(V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell((0, 0.0, 0.33), (0.19, 0.16, 0.23), deform=pear), PINK, "body")
    k.add("shoulders", ell((0, -0.01, 0.48), (0.16, 0.13, 0.11)), PINK, "body")
    # the cream belly, ringed by brown lines
    def belly(P):
        d = (P[:, 0] / 0.15) ** 2 + ((P[:, 2] - 0.29) / 0.2) ** 2 - 1
        return inside(d * 0.06, 0.003) * (P[:, 1] < -0.06)
    k.paint(belly, CREAM, on=["body"])
    for z in (0.19, 0.245, 0.3, 0.355):
        k.paint(lambda P, z=z: inside(np.abs(P[:, 2] - z + 0.02 * (P[:, 0] / 0.12) ** 2) - 0.0045, 0.002)
                * inside(((P[:, 0] / 0.15) ** 2 + ((P[:, 2] - 0.29) / 0.2) ** 2 - 1) * 0.06 + 0.004, 0.002)
                * (P[:, 1] < -0.06), RIB, on=["body"])
    # Legs: thick short columns with heavy thighs fused into the body, broad feet with three white claws
    k.add("thigh", ell((0.12, 0.0, 0.17), (0.095, 0.1, 0.11)), PINK, "leg{s}", mirror=True)
    k.add("leg", tube([(0.115, 0.0, 0.17), (0.13, -0.02, 0.06)], [0.08, 0.07]), PINK, "leg{s}", mirror=True)
    k.add("foot", ell((0.135, -0.05, 0.032), (0.072, 0.092, 0.036)), PINK, "leg{s}", mirror=True)
    sp.claws(k, "claw", (0.135, -0.136, 0.024), (0.15, -1, 0), 3, "leg{s}")
    # Arms: short, from the shoulders down and forward onto the belly, three white claws at each hand
    k.add("arm", tube([(0.16, -0.03, 0.45), (0.19, -0.1, 0.38), (0.17, -0.155, 0.32)], [0.05, 0.045, 0.04]),
          PINK, "arm{s}", mirror=True)
    k.add("hand", ell((0.165, -0.17, 0.3), (0.042, 0.04, 0.04)), PINK, "arm{s}", mirror=True)
    for i, dx in enumerate((-0.022, 0.0, 0.022)):
        root = np.array([0.165 + dx, -0.195, 0.28])
        k.add(f"finger{i}", sp.outward(tube([root, root + np.array([dx * 0.3, -0.012, -0.024])], [0.009, 0.002],
                                            seg=12, per=3)), sp.CLAW, "arm{s}", fuse=False, mirror=True,
              surface="shell")
    # The tail: out of the back, curling round to the +X side, its end clamped in the Shellder
    TAIL = [(0, 0.13, 0.22), (0.05, 0.22, 0.17), (0.14, 0.27, 0.15), (0.23, 0.25, 0.16)]
    k.add("tail", tube(TAIL, [0.07, 0.065, 0.06, 0.058]), PINK, "tail")
    m = np.array([0.255, 0.22, 0.155])
    A = unit(0.4, 0.45, 0.8)
    whorls = shellder(k, m, A, "tail")
    # its face on the base whorl, turned to the front and out to +X
    c0, r0, _ = whorls[0]
    n = unit(0.55, -0.82, 0.05)
    shell_eyes(k, c0 + n * r0 * 0.98 + A * 0.045, n)
    k.build(voxel=0.0068, smooth=3, band=0.006)
    k.eye_states(lambda st: sp.eyes(k, spec, st))
    k.export(out)


if __name__ == "__main__":
    slowbro(*args())
