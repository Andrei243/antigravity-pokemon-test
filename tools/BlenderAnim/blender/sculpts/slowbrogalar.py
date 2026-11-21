# Galarian Slowbro sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py),
# grown from slowbro.py (its pear of a body, ringed cream belly, thick legs, claws) and slowpokegalar.py (the line's
# head with its mouth shut and its cap and curls of another colour).
#   blender --background --factory-startup --python sculpts/slowbrogalar.py -- <the export's Slowbro-Galar.glb> <out.glb>
#
# From the references: Slowbro standing, a heavy pear with a khaki belly ringed by brown lines; its mouth SHUT under a
# pale lilac bill, two small fangs; the top of its head and its ears violet down to the eyes' tops, with wrinkles;
# its right arm (+X, the viewer's right) swallowed to the elbow by a Shellder held forward at the hip: a round pale
# blue-grey shell, as big as the head, spiked all round with thick cones, darker spots, a sleepy face of its own on
# its upper inner side and a violet cone at its far end; the other arm hangs at the side, violet from the elbow to the
# hand; the tail rises behind out to the -X side, pink with a violet band and a violet tip.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, srgb, inside, unit, axes, outward, args, sibling

sp = sibling("slowpoke")
pg = sibling("slowpokegalar")
PINK = sp.PINK
VIOLET, WRINKLE_V, CURL_V = "#8C74CC", "#6A55A8", "#6E5AAE"
MUZZLE = "#E6DEEE"
BELLY, RIB = "#E0DCAE", "#B9AE80"
SHELL, SPOT, TIP, SHELL_INK = "#BCCDD9", "#8C9FBA", "#8664C2", "#2A2638"

HC = np.array([0, -0.05, 0.635])
HS = 1.22
SC = np.array([0.265, -0.27, 0.285])   # the Shellder's middle, before the right hip
SR = 0.145                              # its radius


def shellder_arm(k, c, r, bone):
    """The Shellder that has swallowed the +X arm: a round shell fused to the forearm, thick spikes, spots, its far
    cone and a sleepy face turned up and in toward the viewer."""
    k.add("shell", ell(c, (r, r * 0.95, r * 0.92)), SHELL, bone, surface="shell")
    # thick cones out of it, spread round, never thin plates
    dirs = [unit(0.9, -0.2, 0.45), unit(0.3, -0.75, 0.6), unit(0.2, 0.55, -0.7), unit(0.55, 0.3, 0.75),
            unit(0.95, 0.1, -0.25), unit(0.45, -0.85, -0.15), unit(-0.2, -0.95, 0.1), unit(0.35, -0.45, -0.85),
            unit(0.75, 0.55, 0.0), unit(0.0, 0.0, 1.0)]
    for i, d in enumerate(dirs):
        e1, e2 = axes(d)
        w = r * 0.33
        root = c + d * r * 0.78
        pts = [root + w * (math.cos(q) * e1 + math.sin(q) * e2) for q in np.linspace(0, 2 * math.pi, 9)[:-1]]
        pts += [c + d * r * 1.58]
        k.add(f"spike{i}", hull(pts), SHELL, bone, fuse=False, surface="shell")
    # the violet cone at its far end, pointing down and out
    d = unit(0.55, -0.35, -0.75)
    V, F = lathe(c + d * r * 0.7, d, [(0.0, 0.0), (0.05, 0.0), (0.047, 0.03), (0.022, 0.12), (0.0, 0.122)], seg=28)
    k.add("cone", outward((V, F)), TIP, bone, fuse=False, surface="shell")
    # darker spots, kept off the face
    rs = np.random.RandomState(165)
    for i in range(9):
        q = rs.normal(size=3); q /= np.linalg.norm(q)
        if q @ unit(-0.45, -0.6, 0.65) > 0.55:
            continue
        p = c + q * r
        s = rs.uniform(0.014, 0.022)
        k.paint(lambda P, p=p, s=s: inside(np.linalg.norm(P - p, axis=1) - s, 0.003), SPOT, on=["shell"])


def shellder_face(k, c, r):
    """Two shut, sleepy eyes on the Shellder's upper inner side: dark arcs curving down."""
    n = unit(-0.45, -0.6, 0.65)
    right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right)
    up = np.cross(n, right)
    for sx in (1, -1):
        def look(u, v):
            col = np.tile(srgb(SHELL), (len(u), 1))
            col[(np.abs(v - (0.15 - 0.5 * (1 - u * u))) < 0.3) & (np.abs(u) < 0.85)] = srgb(SHELL_INK)
            return col
        p = c + n * r * 0.99 + right * 0.046 * sx + up * 0.005
        k.decal(p, n, (0.032, 0.02), look, eps=0.003, rings=12, segs=40)


def slowbrogalar(src, out):
    k = Kit(src, coat="scales")
    spec = pg.galar_head(k, HC, HS, col=VIOLET, curl_col=CURL_V)
    for p in k.parts:
        if p["name"] == "muzzle":
            p["color"] = srgb(MUZZLE)
    pg.yellow_cap(k, HC, HS, col=VIOLET, wrinkle=WRINKLE_V)
    # The body: a heavy pear, widest low, the head set on it with no neck to speak of
    def pear(V):
        V = V.copy()
        s = 1 + 0.12 * np.clip(-V[:, 2], 0, 1) - 0.08 * np.clip(V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell((0, 0.0, 0.33), (0.19, 0.16, 0.23), deform=pear), PINK, "body")
    k.add("shoulders", ell((0, -0.01, 0.48), (0.16, 0.13, 0.11)), PINK, "body")
    # the khaki belly, ringed by brown lines
    k.paint(lambda P: inside(((P[:, 0] / 0.15) ** 2 + ((P[:, 2] - 0.29) / 0.2) ** 2 - 1) * 0.06, 0.003)
            * (P[:, 1] < -0.06) * inside(P[:, 2] - 0.5, 0.004), BELLY, on=["body"])
    for z in (0.19, 0.245, 0.3, 0.355):
        k.paint(lambda P, z=z: inside(np.abs(P[:, 2] - z + 0.02 * (P[:, 0] / 0.12) ** 2) - 0.0045, 0.002)
                * inside(((P[:, 0] / 0.15) ** 2 + ((P[:, 2] - 0.29) / 0.2) ** 2 - 1) * 0.06 + 0.004, 0.002)
                * (P[:, 1] < -0.06), RIB, on=["body"])
    # Legs: heavy thighs fused into the body, short shins, broad feet with three white claws
    k.add("thigh", ell((0.12, 0.0, 0.17), (0.095, 0.1, 0.11)), PINK, "leg{s}", mirror=True)
    k.add("leg", tube([(0.115, 0.0, 0.17), (0.13, -0.02, 0.06)], [0.08, 0.07]), PINK, "leg{s}", mirror=True)
    k.add("foot", ell((0.135, -0.05, 0.032), (0.072, 0.092, 0.036)), PINK, "leg{s}", mirror=True)
    sp.claws(k, "claw", (0.135, -0.136, 0.024), (0.15, -1, 0), 3, "leg{s}")
    # The -X arm hangs at the side, forward a little, violet from the elbow to the hand
    k.add("armL", tube([(-0.16, -0.03, 0.45), (-0.2, -0.08, 0.38), (-0.2, -0.13, 0.31)], [0.055, 0.05, 0.046]),
          PINK, "armL")
    k.add("handL", ell((-0.195, -0.15, 0.29), (0.048, 0.046, 0.046)), VIOLET, "armL")
    k.paint(lambda P: inside(P[:, 2] - 0.365, 0.003) * inside(P[:, 0] + 0.15, 0.003), VIOLET, on=["armL", "handL"])
    for i, dx in enumerate((-0.02, 0.0, 0.02)):
        root = np.array([-0.195 + dx, -0.188, 0.272])
        k.add(f"finger{i}", outward(tube([root, root + np.array([dx * 0.3, -0.01, -0.026])], [0.01, 0.003],
                                         seg=12, per=3)), sp.CLAW, "armL", fuse=False, surface="shell")
    # The +X arm: from the shoulder forward and down into the Shellder at the hip
    k.add("armR", tube([(0.16, -0.03, 0.45), (0.22, -0.1, 0.39), (0.255, -0.2, 0.32)], [0.055, 0.052, 0.05]),
          PINK, "armR")
    shellder_arm(k, SC, SR, "armR")
    shellder_face(k, SC, SR)
    # The tail: out of the back, rising behind out to the -X side, a violet band and a violet tip
    TAIL = [(0, 0.12, 0.24), (-0.05, 0.22, 0.2), (-0.12, 0.3, 0.25), (-0.19, 0.35, 0.34), (-0.24, 0.37, 0.43)]
    k.add("tail", tube(TAIL, [0.072, 0.064, 0.054, 0.042, 0.03]), PINK, "tail")
    tip, band = np.array(TAIL[-1]), np.array(TAIL[2])
    k.paint(lambda P: inside(np.linalg.norm(P - tip, axis=1) - 0.075, 0.003) * (P[:, 1] > 0.18), VIOLET, on=["tail"])
    k.paint(lambda P: inside(np.linalg.norm(P - band, axis=1) - 0.065, 0.003) * (P[:, 1] > 0.24),
            VIOLET, on=["tail"])
    k.build(voxel=0.0068, smooth=3, band=0.006)
    k.eye_states(lambda st: sp.eyes(k, spec, st))
    k.export(out)


if __name__ == "__main__":
    slowbrogalar(*args())
