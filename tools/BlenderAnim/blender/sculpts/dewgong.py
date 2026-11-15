# Dewgong sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dewgong.py -- <the export's Dewgong.glb> <out.glb>
#
# Grown from seel.py: its white with a cool shade underneath, its horn, tusks, nose, flippers and way of making eyes.
# From the references: a long sleek white sea lion, its chest reared up and its head held high, the body sweeping down
# to the belly and back up into a tail that ends in one broad fan of a fin with a ruffled edge; a round head with a
# short pointed horn on top, a rounded muzzle, a small black nose, two white tusks hanging from the upper lip; large
# dark brown eyes with a white glint; two broad flippers with ruffled ends held out at the sides. No cream muzzle and
# no tongue (the pictures show neither).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, eye_look, args, sibling

se = sibling("seel")
WHITE, NOSE = se.WHITE, se.NOSE
BROWN, INK = "#3A2A24", "#18120E"


def eye(u, v):
    """A large dark brown eye, darker at its rim, with a white glint up and out."""
    c = np.tile(srgb(BROWN), (len(u), 1))
    c[u * u + v * v > 0.7] = srgb(INK)
    c[((u - 0.15) / 0.3) ** 2 + ((v - 0.35) / 0.28) ** 2 < 1] = (1.0, 1.0, 1.0)
    return c


def eyes(k, spots, half, state):
    look = eye_look(state, eye, srgb(WHITE), srgb(INK), line=0.17)
    for at, d, sx in spots:
        col = look if sx > 0 else (lambda u, v, look=look: look(-u, v))
        k.decal(at, d, half, col, eps=0.004, rings=20, segs=56)


def dewgong(src, out):
    k = Kit(src, coat="fur")
    HC = np.array([0, -0.135, 0.5])
    HR = np.array([0.112, 0.108, 0.104])
    k.add("head", ell(HC, HR), WHITE, "head")
    # the rounded muzzle standing forward low on the face
    k.add("muzzle", ell(HC + (0, -0.08, -0.04), (0.075, 0.06, 0.05)), WHITE, "head")
    # the body: a sleek sweep from the reared chest down to the belly and up into the tail
    k.add("chest", ell((0, -0.07, 0.32), (0.15, 0.14, 0.175), rot=(25, 0, 0)), WHITE, "body")
    k.add("neck", ell((0, -0.115, 0.42), (0.11, 0.1, 0.1)), WHITE, "body")
    k.add("belly", ell((0, 0.08, 0.22), (0.155, 0.18, 0.15)), WHITE, "body")
    k.add("front", ell((0, -0.1, 0.25), (0.135, 0.11, 0.12)), WHITE, "body")
    TAIL = [(0, 0.15, 0.2), (0, 0.27, 0.21), (0, 0.36, 0.27), (0, 0.4, 0.36)]
    k.add("body", tube(TAIL, [0.13, 0.1, 0.075, 0.05], seg=36, per=14), WHITE, "tail")
    # the flippers, out at the sides and back a little, their faces turned up and forward, the ends ruffled
    k.add("flipper", se.paddle((0.22, 0.0, 0.16), (0.12, 0.068, 0.024), (-20, 25, -35), toes=4), WHITE, "leg{s}",
          mirror=True)
    # the tail's fan: one broad fin rising up and back from the tail's end, turned out to +X so its face shows
    root = np.array([0.0, 0.41, 0.37])
    along = np.array(unit(0.57, 0.82, 0.0))
    up = np.array(unit(0.25, 0.55, 1))
    far = np.array(unit(0.3, 0.25, 1.0))
    n = np.array(unit(0.8, -0.55, 0.0)); n = n - far * (n @ far); n /= np.linalg.norm(n)
    M = np.stack([far, n, np.cross(far, n)], axis=1)
    def flare(V):
        V = V.copy()
        t = (V[:, 0] + 1) / 2
        V[:, 2] *= 0.3 + 0.9 * t
        th = np.arctan2(V[:, 2], V[:, 0])
        V[:, [0, 2]] *= (1 + 0.07 * np.clip(V[:, 0], 0, 1) * np.abs(np.cos(5 * th)))[:, None]
        return V
    V, F = ell((0, 0, 0), (0.12, 0.025, 0.165), deform=flare)
    k.add("fin", (V @ M.T + root + far * 0.1, F), WHITE, "tail")
    k.add("finroot", tube([root - far * 0.03, root + far * 0.05], [0.052, 0.04]), WHITE, "tail")
    # nose, tusks, horn
    k.add("nose", ell(HC + (0, -0.142, -0.02), (0.018, 0.011, 0.012)), NOSE, "head", fuse=False, surface="skin")
    se.tusk(k, "tusk", HC + (0.035, -0.12, -0.075), HC + (0.04, -0.13, -0.13), 0.012, "head")
    se.horn(k, HC + (0, -0.02, 0.095), (0, 0.1, 1), 0.05, 0.022, "head")
    se.under_shade(k, 0.3, 0.16, ["body", "chest", "neck", "belly", "front", "head", "muzzle", "flipper", "finroot"])
    k.build(voxel=0.0062, smooth=7, band=0.006)
    spots = [(se.at_surface(HC, HR, d), d, sx) for sx in (1, -1) for d in [unit(0.42 * sx, -0.84, 0.3)]]
    k.eye_states(lambda st: eyes(k, spots, (0.021, 0.027), st))
    k.export(out)


if __name__ == "__main__":
    dewgong(*args())
