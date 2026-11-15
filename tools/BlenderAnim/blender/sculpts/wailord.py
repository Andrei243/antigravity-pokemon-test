# Wailord sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/wailord.py -- <the export's Wailord.glb> <out.glb>
#
# From the references: a very long whale, a blunt high dome at the front tapering to a low tail; light blue over its
# back, pale grey under it, the grey reaching high up the front and its edge running down along the side toward the
# tail; long grooves down the grey underside. A tiny dark eye high on the front's side. At the back two short blue fins
# on the upper sides, a blue fluke of two lobes and a small pale fin under the rear. No flippers at the front, so the
# skeleton's fin bones (at the front, low) carry nothing and the fins at the back go on the body and the tail.
#
# Grown from Wailmer (sibling("wailmer")): its eye, the way its flipper and grooves are made, its colours' kin.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, unit, eye_look, sibling, args

wm = sibling("wailmer")
BLUE, GREY, GROOVE, LIGHT = "#3A94D8", "#D8DADE", "#9A9EA8", "#B4D6F2"

AXIS_Z = 0.222                                   # the body's long axis' height


def line_z(P):
    """The grey's upper edge: high over the front, running down the side toward the tail."""
    t = np.clip((P[:, 1] + 0.43) / 0.78, 0, 1)
    return 0.35 - 0.12 * t ** 0.8


def body(k):
    # One long body: a tube through the length, round and blunt at the front, highest a third of the way back and
    # tapering to the tail, its belly a little flattened
    t = np.linspace(0, 1, 18)
    radius = 0.05 + 0.178 * (1 - t ** 2.2) ** 0.7          # a smooth taper: kinks in it show as ridges round the body
    V, F = tube([(0.0, -0.28 + 0.68 * u, 0.226 - 0.022 * u + 0.01 * u * u) for u in t], radius, seg=64, per=6)
    V = V.copy()
    V[:, 0] *= 1.05
    low = V[:, 2] < AXIS_Z
    V[low, 2] = AXIS_Z + (V[low, 2] - AXIS_Z) * 0.88
    k.add("main", (V, F), BLUE, "body")
    k.add("nose", ell((0.0, -0.3, 0.228), (0.236, 0.205, 0.224), sub=5), BLUE, "body")   # rounds the tube's blunt end
    k.add("rear", tube([(0.0, 0.26, 0.21), (0.0, 0.4, 0.214)], [0.1, 0.045]), BLUE, "tail")
    k.paint(lambda P: inside(P[:, 2] - line_z(P), 0.0022), GREY)
    k.paint(lambda P: inside(line_z(P) - P[:, 2], 0.0022), BLUE, on=["main", "rear", "nose"])

    def grooves(P):
        th = np.degrees(np.arctan2(P[:, 0], -(P[:, 2] - AXIS_Z)))
        r = np.hypot(P[:, 0], P[:, 2] - AXIS_Z)
        m = np.zeros(len(P))
        for a0 in (-84, -62, -40, -18, 0, 18, 40, 62, 84):
            m = np.maximum(m, inside(np.abs(np.radians(th - a0)) * r - 0.0036, 0.002))
        front = np.clip((P[:, 1] + 0.43) / 0.1, 0, 1)            # they fade out over the front's dome
        return m * (P[:, 2] < line_z(P) - 0.025) * (front > 0.5) * (P[:, 1] < 0.3)
    k.paint(grooves, GROOVE)


FIN = [(0.0, 0.04), (0.08, 0.046), (0.16, 0.042), (0.185, 0.02), (0.16, 0.002), (0.18, -0.018), (0.14, -0.032),
       (0.0, -0.03)]
FLUKE = [(0.0, 0.03), (0.06, 0.05), (0.15, 0.068), (0.19, 0.05), (0.165, 0.025), (0.18, -0.002), (0.09, -0.026),
         (0.0, -0.026)]
LOW = [(-0.03, 0.0), (0.04, -0.035), (0.09, -0.05), (0.07, -0.02), (0.045, 0.0)]


def fins(k):
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        # The fins on the upper sides at the back: back and out, raised some 25 degrees
        k.add(f"fin{sd}", slab([(a * 1.3, b * 1.3) for a, b in FIN], (0.13 * s, 0.17, 0.29), unit(0.55 * s, 0.84, 0.0), (0.9 * s, 0.0, 0.42), 0.026),
              BLUE, "body", fuse=False, surface="scales")
        # The fluke: two lobes out of the tail's end, back and out, raised in a V
        k.add(f"fluke{sd}", slab([(a * 1.3, b * 1.3) for a, b in FLUKE], (0.012 * s, 0.4, 0.216), unit(0.55 * s, 0.84, 0.12), (0.88 * s, 0.0, 0.47),
                                 0.026), BLUE, "tail", fuse=False, surface="scales")
    # The small pale fin under the rear, turned to +X
    k.add("lowfin", slab(LOW, (0.0, 0.3, 0.135), unit(0.6, 0.8, 0.0), (0.0, 0.0, 1.0), 0.026), GREY, "tail",
          fuse=False, surface="scales")


CB, CK, CL = srgb(BLUE), srgb(wm.INK), srgb(LIGHT)


def eyes(k, state):
    ink = CK if state in ("Open", "Fierce") else srgb("#1E5C98")
    look = eye_look(state, wm.eye_open, CB, ink, line=0.18)
    a = math.radians(38)
    c, r = np.array([0.0, -0.28, 0.226]), np.array([0.235, 0.228, 0.228])
    z = 0.37
    sz = (z - c[2]) / r[2]
    ca = math.sqrt(1 - sz * sz)
    d = np.array([math.sin(a) * ca, -math.cos(a) * ca, sz])
    n = d / r; n /= np.linalg.norm(n)
    k.decal(c + d * r, n, (0.017, 0.021), look, eps=0.0045, rings=20, segs=56, mirror=True)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="scales")
    body(k)
    fins(k)
    k.build(voxel=0.0055, smooth=5, band=0.004)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
