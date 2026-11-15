# Seel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/seel.py -- <the export's Seel.glb> <out.glb>
#
# Written to be grown from (Dewgong: sibling("seel")). From the references: a white seal pup, faintly blue in its
# shade, lying low at the front with its big round head down and its body rising behind it into a tail held high, a
# fluke of two round lobes at its top spread to the sides and curled; a short solid cone of a horn on the forehead;
# small dark eyes with a white glint; a cream muzzle of two puffed cheeks, a round black nose over them, two white
# tusks pointing down at their outer corners and a pink tongue hanging out between them; two broad flippers spread
# flat on the ground, their ends lobed into three round toes.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, srgb, inside, smoothstep, unit, eye_look, outward, args

WHITE, SHADE, CREAM, NOSE, TONGUE, TUSK, INK = ("#EBEFF6", "#BCC7DD", "#E8D6A8", "#1C1A22", "#E68AA6", "#F8F6F0",
                                                "#141620")


def under_shade(k, zc, depth, on):
    """The cool shade of the white's underside: everything below a level eased in, so the white forms read."""
    k.paint(lambda P: smoothstep(zc, zc - depth, P[:, 2]) * 0.8, SHADE, on=on)


def horn(k, base, d, length, r, bone):
    """A short solid cone of a horn from base along d."""
    k.add("horn", lathe(base - unit(*d) * r * 0.5, d, [(0, 0), (r, 0), (r * 0.82, length * 0.3), (r * 0.45, length * 0.7),
                                                          (0, length + r * 0.5)], seg=28),
          WHITE, bone, fuse=False, surface="shell")


def tusk(k, name, root, tip, r, bone):
    """A white tusk from root to tip, round and pointed."""
    root, tip = np.array(root, float), np.array(tip, float)
    pts = [root + (tip - root) * t for t in np.linspace(0, 1, 5)]
    k.add(name, outward(tube(pts, [r * (1 - t) + 0.002 * t for t in np.linspace(0, 1, 5)], seg=14, per=3)), TUSK,
          bone, fuse=False, mirror=True, surface="shell")


def paddle(c, half, rot, toes=3):
    """A flipper: a thick flat oval whose far end is lobed into round toes (local +x is the far end)."""
    def f(V):
        V = V.copy()
        th = np.arctan2(V[:, 1], V[:, 0])
        far = np.clip(V[:, 0], 0, 1) ** 2
        V[:, :2] *= (1 + 0.1 * far * np.abs(np.cos(toes * th * 1.0)))[:, None]
        return V
    return ell(c, half, rot=rot, deform=f)


def eye(u, v):
    """A small dark eye with a white glint up and out (u outward, v up)."""
    c = np.tile(srgb(INK), (len(u), 1))
    c[((u - 0.12) / 0.32) ** 2 + ((v - 0.36) / 0.3) ** 2 < 1] = (1.0, 1.0, 1.0)
    return c


def eyes(k, spots, half, state):
    look = eye_look(state, eye, srgb(WHITE), srgb(INK), line=0.17)
    for at, d, sx in spots:
        col = look if sx > 0 else (lambda u, v, look=look: look(-u, v))
        k.decal(at, d, half, col, eps=0.004, rings=20, segs=56)


def at_surface(C, R, d):
    d = np.array(d, float)
    return C + d / np.linalg.norm(d / R)


def seel(src, out):
    k = Kit(src, coat="fur")
    HC = np.array([0, -0.185, 0.21])
    HR = np.array([0.138, 0.126, 0.128])
    k.add("head", ell(HC, HR), WHITE, "head")
    # The body, lying low at the front and rising behind into the tail
    k.add("body", ell((0, 0.0, 0.16), (0.13, 0.17, 0.14), rot=(-22, 0, 0)), WHITE, "body")
    k.add("chest", ell((0, -0.1, 0.13), (0.125, 0.1, 0.11)), WHITE, "body")
    TAIL = [(0, 0.1, 0.22), (0, 0.19, 0.29), (0, 0.235, 0.37), (0, 0.245, 0.44), (0, 0.23, 0.49)]
    k.add("tail", tube(TAIL, [0.115, 0.09, 0.068, 0.05, 0.04]), WHITE, "tail")
    # The fluke: two round lobes spread to the sides at the top, faces turned to the front, tips curled up
    k.add("fluke", paddle((0.1, 0.225, 0.5), (0.1, 0.024, 0.058), (-25, -18, 0)), WHITE, "tail", mirror=True)
    # The front flippers spread flat on the ground, pointing out and a little forward
    k.add("arm", tube([(0.09, -0.08, 0.1), (0.15, -0.11, 0.05)], [0.05, 0.035]), WHITE, "leg{s}", mirror=True)
    k.add("flipper", paddle((0.19, -0.13, 0.03), (0.095, 0.052, 0.022), (0, 0, -32)), WHITE, "leg{s}", mirror=True)
    # The muzzle: two puffed cream cheeks, the nose over them, the tongue between, the tusks at the outer corners
    for sx in (1, -1):
        k.add(f"cheek{'RL'[sx < 0]}", ell(HC + (0.05 * sx, -0.105, -0.05), (0.064, 0.05, 0.048)), CREAM, "head")
    k.add("tongue", ell(HC + (0, -0.135, -0.108), (0.033, 0.015, 0.036), rot=(-25, 0, 0)), TONGUE, "head", fuse=False,
          surface="skin")
    k.add("nose", ell(HC + (0, -0.148, -0.01), (0.022, 0.014, 0.015)), NOSE, "head", fuse=False, surface="skin")
    tusk(k, "tusk", HC + (0.082, -0.125, -0.075), HC + (0.092, -0.135, -0.12), 0.011, "head")
    horn(k, HC + (0, -0.055, 0.112), (0, -0.25, 1), 0.06, 0.022, "head")
    under_shade(k, 0.12, 0.12, ["body", "chest", "head", "arm", "flipper", "tail"])
    k.build(voxel=0.006, smooth=3, band=0.006)
    spots = [(at_surface(HC, HR, d), d, sx) for sx in (1, -1) for d in [unit(0.36 * sx, -0.88, 0.32)]]
    k.eye_states(lambda st: eyes(k, spots, (0.019, 0.025), st))
    k.export(out)


if __name__ == "__main__":
    seel(*args())
