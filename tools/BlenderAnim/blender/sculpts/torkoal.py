# Torkoal sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/torkoal.py -- <the export's Torkoal.glb> <out.glb>
#
# From the references: an orange tortoise under a great dark grey shell of faceted plates, with a broad rim; red
# hexagonal holes in the shell, each a dark frame round a red ring and a hot pink heart; a long thick neck rising out of
# the shell's front to a small rounded head tilted up, its eyes always shut (a curved lid line), two nostrils at the
# snout's tip and a small mouth; a grey band round the neck's foot and round each leg near the foot; short thick legs,
# three white claws on each foot; a stub of a tail; a cloud of beige smoke rising from the top of its shell (kept
# small: in the pictures it is as large as the tortoise, which would shrink the body in the game). The smoke streaming
# from the nostrils is left out (thin streams are wire in the game).
# Reference ratios (3D picture, without the smoke): head 0.23 of the height, the shell's top 0.7 of the height, the
# shell's length 0.8 of the height, the belly 0.12 of the height off the ground, the neck 0.18 of the height across.
# The export's joints: body (0, 0, 0.22), head (0, -0.18, 0.24), tail (0, 0.2, 0.18), legs (+-0.12, -0.1, 0.13),
# hind (+-0.12, 0.12, 0.13).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, ring_band, srgb, inside, unit, eye_look, polygon, facets, args, outward, \
    sibling

nm = sibling("numel")
on_ell, normal_on, spike = nm.on_ell, nm.normal_on, nm.spike

ORANGE, SHELL, RIM, BAND = "#EE8A3C", "#4C4E58", "#3C3D46", "#8C8E98"
FRAME, HOLE, HEART, CLAW = "#24242A", "#D8384A", "#F47C8A", "#F2F0EA"
SMOKE, SMOKE2, INK = "#D8CCB6", "#C4B49A", "#3A2418"

SC = np.array([0.0, 0.05, 0.215])            # the shell's middle
SR = np.array([0.2, 0.235, 0.195])
H = np.array([0.0, -0.245, 0.515])
HR = np.array([0.081, 0.098, 0.077])
HEAD_TILT = -16                               # the head's front turned up


def head_point(d):
    """A point of the tilted head's surface in direction d (in the head's own frame) and its normal."""
    m = np.array([[1, 0, 0], [0, math.cos(math.radians(HEAD_TILT)), -math.sin(math.radians(HEAD_TILT))],
                  [0, math.sin(math.radians(HEAD_TILT)), math.cos(math.radians(HEAD_TILT))]])
    p = on_ell((0, 0, 0), HR, d)
    n = p / HR ** 2; n /= np.linalg.norm(n)
    return H + m @ p, m @ n


def torkoal(src, out):
    k = Kit(src, coat="shell")

    # The body under the shell, orange, its belly low
    k.add("body", ell((0, 0.05, 0.175), (0.185, 0.225, 0.105)), ORANGE, "body")
    # The shell: a faceted dome, its lower half drawn in under the rim so the orange shows below it, and the rim
    def dome(V):
        V = facets(46, 0.55, 3, depth=(0.9, 1.0))(V)
        V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.25, V[:, 2])
        return V
    k.add("shell", ell(SC, SR, deform=dome), SHELL, "body")
    k.add("rim", ell((0, 0.05, 0.215), (0.218, 0.255, 0.035)), RIM, "body")

    # The neck rising out of the shell's front, the head tilted up
    k.add("neck", tube([(0, -0.1, 0.25), (0, -0.175, 0.31), (0, -0.215, 0.4), (0, -0.235, 0.47)],
                       [0.08, 0.068, 0.06, 0.058]), ORANGE, "head", surface="scales")
    k.add("head", ell(H, HR, rot=(HEAD_TILT, 0, 0)), ORANGE, "head", surface="scales")

    # The legs: short and thick, wide feet, white claws
    for name, y, bone in (("leg", -0.105, "leg{s}"), ("hind", 0.135, "hind{s}")):
        k.add(name + "top", ell((0.13, y, 0.15), (0.075, 0.08, 0.08)), ORANGE, bone, mirror=True, surface="scales")
        k.add(name, tube([(0.135, y, 0.15), (0.145, y - 0.01, 0.06)], [0.066, 0.062]), ORANGE, bone, mirror=True,
              surface="scales")
        k.add(name + "foot", ell((0.147, y - 0.015, 0.032), (0.068, 0.075, 0.032)), ORANGE, bone, mirror=True,
              surface="scales")
        for j, dx in enumerate((-0.035, 0.0, 0.035)):
            k.add(f"{name}claw{j}", spike((0.147 + dx, y - 0.075, 0.02), (dx * 4, -1, -0.35), 0.04, 0.018, sink=0.015),
                  CLAW, bone, fuse=False, mirror=True)
        k.add(name + "band", ring_band((0.146, y - 0.012, 0.075), (0.02, -0.02, 1), 0.06, 0.024, thick=0.008), BAND,
              bone, fuse=False, mirror=True)
    # the band round the neck's foot
    nd = unit(0, -0.075, 0.09)
    k.add("neckband", ring_band((0, -0.17, 0.305), nd, 0.066, 0.026, thick=0.008), BAND, "head", fuse=False)

    # A stub of a tail behind
    k.add("tail", spike((0, 0.24, 0.15), (0, 1, -0.45), 0.065, 0.032, sink=0.02), ORANGE, "tail", fuse=False,
          surface="scales")

    # The smoke rising from the top of the shell toward its back: a lumpy cloud of fused puffs, two beiges
    rs = np.random.RandomState(4)
    k.add("smoke0", ell((0.0, 0.12, 0.425), (0.045, 0.045, 0.05)), SMOKE2, "body", surface="cloth")
    k.add("smoke1", ell((0.01, 0.14, 0.47), (0.05, 0.05, 0.045)), SMOKE2, "body", surface="cloth")
    k.add("smokeC", ell((0.0, 0.16, 0.535), (0.1, 0.07, 0.05)), SMOKE, "body", surface="cloth")
    for i in range(16):
        a = 2 * math.pi * i / 16 + 0.3 * rs.rand()
        rr = 0.06 + 0.035 * rs.rand()
        c = np.array([rr * math.cos(a) * 1.25, 0.16 + rr * math.sin(a) * 0.75, 0.53 + 0.025 * rs.randn()])
        r = 0.04 + 0.018 * rs.rand()
        k.add(f"smokeP{i}", ell(c, (r, r, r * 0.9), sub=3), SMOKE if i % 3 else SMOKE2, "body", surface="cloth")

    # The holes in the shell: a dark frame, a red ring, a pink heart, all six-sided, each its own patch
    HEX = polygon([(math.cos(math.radians(60 * j + 30)), math.sin(math.radians(60 * j + 30))) for j in range(6)])
    for d, h, mir in (((0, -0.45, 0.9), 0.055, False), ((0.72, -0.3, 0.6), 0.05, True), ((0.72, 0.4, 0.55), 0.05, True),
                      ((0, 0.85, 0.42), 0.05, False), ((0.5, 0.05, 0.86), 0.042, True)):
        for j, (f, col) in enumerate(((1.0, FRAME), (0.78, HOLE), (0.48, HEART))):
            k.decal(on_ell(SC, SR, d), normal_on(SC, SR, d), (h * f, h * f), col, eps=0.004 + 0.0012 * j, rings=4,
                    segs=60, outline=HEX, mirror=mir, up=(0, -1, 0) if abs(d[0]) < 0.6 else (0, 0, 1))

    # Two nostrils at the snout's tip, a small mouth under it
    c, n = head_point(unit(0.17, -0.93, -0.32))
    k.decal(c, n, (0.0075, 0.0068), INK, eps=0.004, rings=5, segs=20, mirror=True)
    c, n = head_point(unit(0.0, -0.75, -0.66))
    def mouth(u, v):
        col = np.tile(srgb(ORANGE), (len(u), 1))
        col[np.abs(v - (0.25 - 0.55 * (1 - u * u))) < 0.28] = srgb(INK)
        return col
    k.decal(c, n, (0.024, 0.011), mouth, eps=0.004, rings=8, segs=40)

    k.build(voxel=0.0068, smooth=3, band=0.004)

    # The eyes, always shut: the open state is the lid's curved line; Fierce is the line tilted, written by hand
    L, K = srgb(ORANGE), srgb(INK)
    def slit(u, v):
        c = np.tile(L, (len(u), 1))
        c[(np.abs(u) < 0.85) & (np.abs(v - (-0.05 - 0.32 * (1 - u * u))) < 0.22)] = K
        return c
    def fierce(u, v):
        c = np.tile(L, (len(u), 1))
        c[(np.abs(u) < 0.85) & (np.abs(v - (-0.15 + 0.35 * u)) < 0.22)] = K
        return c
    ec, en = head_point(unit(0.55, -0.5, 0.66))
    def eyes(state):
        if state in ("Open", "Shut"):
            f = slit
        elif state == "Fierce":
            f = fierce
        else:
            f = eye_look(state, slit, L, K, line=0.22)
        k.decal(ec, en, (0.045, 0.034), f, eps=0.004, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    torkoal(*args())
