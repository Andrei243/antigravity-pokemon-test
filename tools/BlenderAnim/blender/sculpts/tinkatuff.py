# Tinkatuff sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Tinkatink's script (tinkatink.py, imported: its colours, on_ell, the prisms, fists, feet, cheeks and lidded eyes):
#   blender --background --factory-startup --python sculpts/tinkatuff.py -- <the export's Tinkatuff.glb> <out.glb>
# From the references: the same hot pink, the head round and still the largest part of the body, its top now under a
# cap of pale pink hair whose hairline zigzags over the forehead (a point down in the middle, a peak over each eye, a
# point down at each temple), and from the back of the crown a great pale pink ponytail in two lobes, as big as the
# head, swept up, back and out to +X and falling down. Angry eyes, the lid slanting down to the inner corner; a
# wide open mouth with two small fangs; blush on the cheeks. A small round body on short legs with round feet, a
# silver point at the hip. Both fists grip a long purple handle held across the front, its -X end a rounded cap, and
# at its +X end, lower, a big hammer's head of grey lavender metal: a thick curved plate bent round like a C (the
# shell of a short upright cylinder, open on its far side), the handle joined to its back with a purple collar.
# Reference ratios (3D picture, hair to feet, ponytail and hammer left out): head / height 0.55, head width / height
# 0.6, ponytail / head about 1, the hammer's head / the head 1.1.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, sheet, srgb, inside, unit, axes, outward, tri, args, sibling

t = sibling("tinkatink")

HAIR, HAIRDEEP, METAL, METALDEEP, SILVER = "#F4D0D0", "#E8B4B8", "#B9B2D2", "#9A92BC", "#D6D8E2"
FANG = "#FBF6F8"

HC = np.array([0.0, -0.008, 0.29])           # the head (bone at 0.22; the ears' bones carry the hair at 0.33)
HR = np.array([0.104, 0.092, 0.094])
P0, P1 = np.array([-0.14, -0.088, 0.228]), np.array([0.19, -0.135, 0.125])   # the handle, -X end to the hammer


def hairline(P):
    """How far a point stands above the zigzag hairline (above: hair); the nape is covered low behind."""
    phi = np.arctan2(P[:, 0] - HC[0], -(P[:, 1] - HC[1]))           # 0 at the front, +-pi behind
    a = np.abs(phi)
    front = HC[2] + 0.03 + 0.03 * tri(a / math.radians(65)) * (a < math.radians(98))
    back = HC[2] - 0.05
    w = np.clip((a - math.radians(98)) / math.radians(25), 0, 1)
    return P[:, 2] - (front * (1 - w) + back * w)


def tinkatuff(src, output):
    k = Kit(src, coat="fur")

    # --- the head, its cap of hair and the ponytail
    k.add("head", ell(HC, HR), t.PINK, "head")
    def capdeform(V):
        V = V.copy()
        low = np.clip((0.15 - V[:, 2]) / 0.35, 0, 1) * (V[:, 1] < 0.2)
        V[:, 0] *= 1 - 0.45 * low; V[:, 1] *= 1 - 0.45 * low
        return V
    k.add("hair", ell(HC + (0, 0.008, 0.006), HR + (0.006, 0.006, 0.006), deform=capdeform), HAIR, "head")
    k.paint(lambda P: inside(-hairline(P), 0.0025) * inside(np.linalg.norm((P - HC) / (HR * 1.4), axis=1) - 1, 0.01),
            HAIR)
    # the ponytail: two lobes swept up, back and out to +X, the lower one falling
    k.add("tailroot", tube([(0.02, 0.07, 0.33), (0.06, 0.11, 0.35), (0.1, 0.135, 0.36)], [0.04, 0.048, 0.055]),
          HAIR, "earR")
    k.add("lobeup", ell((0.115, 0.145, 0.36), (0.09, 0.072, 0.07), rot=(0, -15, 35)), HAIR, "earR")
    k.add("lobedown", ell((0.17, 0.165, 0.3), (0.058, 0.052, 0.07), rot=(0, 20, 35)), HAIR, "earR")
    k.add("lobetip", ell((0.195, 0.175, 0.24), (0.036, 0.034, 0.038), rot=(0, 30, 35)), HAIRDEEP, "earR")
    # --- the body: small and round on short legs, a silver point at the hip
    k.add("body", ell((0, 0.0, 0.16), (0.058, 0.052, 0.066)), t.PINK, "body")
    k.add("hips", ell((0, 0.004, 0.11), (0.064, 0.054, 0.045)), t.PINK, "body")
    k.add("thigh", ell((0.042, -0.004, 0.09), (0.036, 0.036, 0.04)), t.PINK, "leg{s}", mirror=True)
    k.add("shin", tube([(0.045, -0.006, 0.07), (0.048, -0.01, 0.03)], [0.028, 0.026]), t.PINK, "leg{s}", mirror=True)
    t.foot(k, (0.05, -0.016, 0.02), (0.032, 0.04, 0.02), t.PINK, "leg{s}")
    sp = np.array([-0.05, -0.04, 0.115])
    k.add("spike", hull([sp + (0.012, 0, 0.01), sp + (-0.012, 0, 0.01), sp + (0, 0.012, 0.012), sp + (0, -0.012, 0.012),
                         sp + (-0.008, -0.02, -0.04)]), SILVER, "body", fuse=False, surface="metal")

    # --- the arms: forward, both fists on the handle in front of the chest
    D = unit(*(P1 - P0))
    GL, GR = P0 + (P1 - P0) * 0.3, P0 + (P1 - P0) * 0.6
    k.add("armL", tube([(-0.05, 0.0, 0.18), (-0.07, -0.04, 0.19), GL + (0.0, 0.01, -0.004)], [0.03, 0.027, 0.025]),
          t.PINK, "armL")
    t.fist(k, GL, D, 0.028, t.PINK, "armL", "fistL")
    k.add("armR", tube([(0.05, 0.0, 0.18), (0.07, -0.05, 0.17), GR + (-0.004, 0.012, 0.0)], [0.03, 0.027, 0.025]),
          t.PINK, "armR")
    t.fist(k, GR, D, 0.028, t.PINK, "armR", "fistR")

    # --- the hammer: a long purple handle, a rounded cap at -X, a collar, and the C of metal
    k.add("handle", outward(tube([P0, P1], [0.017, 0.017], seg=18)), t.HANDLE, "armR", fuse=False, surface="metal")
    k.add("cap", outward(ell(P0 - D * 0.012, (0.024, 0.024, 0.024))), t.HANDLE, "armR", fuse=False, surface="metal")
    k.add("collar", t.prism(P1 - D * 0.02, P1 + D * 0.012, 0.022, 0.022, n=8), t.NECK, "armR", fuse=False,
          surface="metal")
    A = unit(*np.cross(D, (0, 1.0, 0)))           # the C's upright axis, square to the handle
    if A[2] < 0:
        A = -A
    e1 = D
    e2 = np.cross(A, e1)
    R, L = 0.078, 0.17
    C = P1 + D * (R + 0.006)
    def shell(u, v):
        th = math.radians(50) + u * math.radians(260)              # open over the far side, toward +D
        return (C + (v[..., None] - 0.5) * L * A
                + R * (np.cos(th)[..., None] * e1 + np.sin(th)[..., None] * e2))
    k.add("head_metal", outward(sheet(shell, 40, 12, 0.022)), METAL, "armR", fuse=False, surface="metal")
    k.add("bolt", t.prism(C - e1 * (R + 0.004), C - e1 * (R - 0.02), 0.016, 0.014, n=6), METALDEEP, "armR",
          fuse=False, surface="metal")

    # --- the face: cheeks, the open mouth with two fangs
    t.cheek(k, HC, HR, (0.8, -0.55, -0.3), (0.024, 0.02))
    M, F, Tg = srgb(t.MOUTH), srgb(FANG), srgb(t.LIP)
    def mouth(u, v):
        c = np.tile(M, (len(u), 1))
        c[((u) / 0.7) ** 2 + ((v + 0.75) / 0.4) ** 2 < 1] = Tg
        for x in (-0.45, 0.45):
            c[(v > 0.35) & (np.abs(u - x) < 0.2 * (v - 0.35) / 0.65 + 0.0) ] = F
        return c
    mp, mn = t.on_ell(HC, HR, (0.0, -1.0, -0.4))
    k.decal(mp, mn, (0.026, 0.022), mouth, eps=0.0018, rings=16, segs=64,
            outline=lambda th: 1.0 if math.sin(th) < 0 else 0.7 + 0.3 * abs(math.cos(th)))

    k.build(voxel=0.0042, smooth=4, band=0.004, wband=0.03)

    # --- the eyes: angry, the lid slanting down to the inner corner
    look, _ = t.lid_eye(slope=-0.5, top=0.12)
    k.eye_states(t.lay_eyes(k, HC, HR, (0.42, -0.9, -0.06), (0.026, 0.021), look, srgb(t.PINK), srgb(t.INK)))
    k.export(output)


if __name__ == "__main__":
    tinkatuff(*args())
