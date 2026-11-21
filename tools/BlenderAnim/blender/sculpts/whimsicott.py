# Whimsicott sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/whimsicott.py -- <the export's Whimsicott.glb> <out.glb>
#
# Grown from Cottonee's script (sibling("cottonee")): the same clouds of cotton, the same upright orange eyes with
# their rim and glint. From the references: a little brown imp, its body a pear that widens to two stubby feet, short
# arms (raised in the art, carried forward and a little down here); a big round brown head under a cap of cream
# cotton with two little curls on its crown, the cap down to its brow; green ram's horns curled at its cheeks, their
# faces to the front; upright orange eyes set close; an open happy mouth, dark with a pink tongue; a cream beard of
# cotton under the chin; and behind it all a great round cloud of cream cotton, a shade darker below, reaching from
# its knees to over its head and wider than it is tall.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, args, outward, sibling

ct = sibling("cottonee")

BROWN = "#9A7052"
CREAM, CREAM_LOW = "#F2E6BC", "#D8C690"
HORN = "#56A656"
MOUTH, TONGUE = srgb("#6E2228"), srgb("#EE8C9A")
INK = srgb("#3A2418")

# --- Whimsicott's frame (joints: body (0, 0, 0.12), head (0, -0.02, 0.18), arms (+-0.04, -0.01, 0.15),
#     legs (+-0.03, 0, 0.06)) -----------------------------------------------------------------------------------------
HC = np.array([0.0, -0.024, 0.212])      # the head's middle
HR = np.array([0.064, 0.058, 0.058])
BODY = np.array([0.0, 0.0, 0.098])
BODY_R = np.array([0.046, 0.04, 0.07])
CLOUD = np.array([0.0, 0.088, 0.21])     # the great cloud behind
CLOUD_R = np.array([0.136, 0.054, 0.118])
EYE_D = (0.4, -0.9, 0.1)
EYE_HALF = (0.0115, 0.0165)


def pear(V):
    """The unit sphere made a pear: wider toward its foot."""
    V = V.copy()
    s = 1 + 0.28 * np.clip(-V[:, 2], 0, 1) - 0.1 * np.clip(V[:, 2], 0, 1)
    V[:, 0] *= s; V[:, 1] *= s
    return V


def horn(k, side):
    """A ram's horn curled at the cheek: a fat tube on a shrinking spiral whose face looks forward and out."""
    c = np.array([0.08 * side, -0.026, 0.206])
    a = unit(0.87 * side, 0.5, 0)            # out and back, in the curl's plane
    b = np.array([0, 0, 1.0])
    pts, rad = [], []
    for i in range(14):
        t = i / 13
        th = math.radians(110 - 390 * t)     # from the top, out, down, in and round again (a points outward)
        rho = 0.038 - 0.026 * t
        pts.append(c + rho * (math.cos(th) * a + math.sin(th) * b))
        rad.append(0.018 - 0.006 * t)
    k.add("horn" + ("R" if side > 0 else "L"), outward(tube(pts, rad)), HORN, "head", fuse=False)


def whimsicott(src, out):
    k = Kit(src, coat="fur")
    # stubby legs on wide feet
    k.add("leg", tube([(0.03, 0.0, 0.07), (0.034, -0.004, 0.026)], [0.022, 0.021]), BROWN, "leg{s}", mirror=True)
    k.add("foot", ell((0.036, -0.012, 0.014), (0.024, 0.03, 0.015)), BROWN, "leg{s}", mirror=True)
    k.add("body", ell(BODY, BODY_R, deform=pear), BROWN, "body")
    # short arms, forward and a little down
    k.add("arm", tube([(0.036, -0.01, 0.152), (0.066, -0.04, 0.138), (0.082, -0.058, 0.128)],
                      [0.019, 0.017, 0.016]), BROWN, "arm{s}", mirror=True)
    k.add("head", ell(HC, HR), BROWN, "head")
    # the cap of cotton over the crown, down to the brow, with two little curls on top
    ct.cloud(k, "cap", (0.0, -0.006, 0.27), (0.064, 0.058, 0.03), "head", 6, lumps=9, color=CREAM, small=0.52,
             spread=0.8, zr=(-0.4, 0.3), flat=0.6)
    for sx in (1, -1):
        k.add("curl", tube([(0.022 * sx, -0.002, 0.284), (0.03 * sx, -0.006, 0.3), (0.02 * sx, -0.016, 0.306)],
                           [0.016, 0.015, 0.014]), CREAM, "head")
    # the beard under the chin
    for i, (x, z, s) in enumerate(((0.0, 0.158, 0.02), (0.026, 0.164, 0.018), (-0.026, 0.164, 0.018),
                                   (0.044, 0.174, 0.015), (-0.044, 0.174, 0.015))):
        k.add(f"beard{i}", ell((x, -0.062 + abs(x) * 0.25, z), (s, s * 0.9, s * 0.95), sub=3), CREAM, "head")
    # the great cloud behind
    ct.cloud(k, "cloud", CLOUD, CLOUD_R, "body", 2, lumps=16, color=CREAM, small=0.5, spread=0.9,
             zr=(-0.15, 0.15), flat=0.4, upright=True)
    ct.cloud_shade(k, CLOUD, CLOUD_R, color=CREAM_LOW, share=0.3, on=("cloud",), soft=0.04)
    horn(k, 1)
    horn(k, -1)
    # the open, happy mouth: flat on top, round below, a tongue in it
    p, nrm = ct.on_ell(HC, HR, (0.0, -1.0, -0.32))
    def mouth(u, v):
        col = np.tile(MOUTH, (len(u), 1))
        col[(u / 0.62) ** 2 + ((v + 0.62) / 0.5) ** 2 < 1] = TONGUE
        return col
    def shape(th):
        s = math.sin(th)
        return min(1.0, 0.34 / s) if s > 0 else 1.0
    k.decal(p, nrm, (0.022, 0.02), mouth, eps=0.004, outline=shape, rings=12, segs=48)
    k.build(voxel=0.005, smooth=4, band=0.006)

    def eyes(state):
        ct.lay_eyes(k, HC, HR, EYE_D, EYE_HALF, ct.orange_eye(), BROWN, state, ink=INK)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    whimsicott(*args())
