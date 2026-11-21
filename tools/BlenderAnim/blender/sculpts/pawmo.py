# Pawmo sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pawmo.py -- <the export's Pawmo.glb> <out.glb>
#
# The design, from the references, grown from Pawmi's script (its ears with their teal hollows and front lobe, its
# brown eyes with two glints, yellow cheeks, cream muzzle, tail paddle): a paler golden-orange mouse standing up, head
# and body one bean wider at the bottom with no neck; long ears, the -X one upright with its lobe, the +X one flopped
# out to the side; a big lock of fur swept from the crown over to +X; the eyes cut flat by a scowling brow; a small open
# mouth, dark red with a pink tongue, in a cream muzzle; orange upper arms and cream forearms ending in big round cream
# fists, each palm a yellow pad in a white star; short legs with brown feet; a small forked cream tail behind to +X.
# The pictures overrule the brief: Pawmo's forearms are cream, not long fur; it holds its fists up in the art, carried
# here forward and down before the belly.
# The export's joints: body (0, 0, 0.16), head (0, -0.01, 0.23), ears (+-0.06, -0.01, 0.35), arms (+-0.06, -0.01,
# 0.2), legs (+-0.04, 0, 0.1), tail (0, 0.05, 0.12).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, args, outward, sibling

pm = sibling("pawmi")

FUR, CREAM, FEET = "#F0B452", "#F6E3B8", "#9A6436"

HC, HR = np.array([0.0, -0.018, 0.3]), np.array([0.106, 0.096, 0.096])       # the head
BC, BR = np.array([0.0, 0.006, 0.148]), np.array([0.118, 0.1, 0.14])          # the bean of a body


def on_head(x, z, lift=0.0):
    q = 1 - (x / HR[0]) ** 2 - ((z - HC[2]) / HR[2]) ** 2
    y = HC[1] - HR[1] * math.sqrt(max(q, 0.0))
    p = np.array([x, y, z])
    g = (p - HC) / HR ** 2
    g /= np.linalg.norm(g)
    return p + g * lift, g


def bean(V):
    V = V.copy()
    s = 1 - 0.12 * V[:, 2]
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.where(V[:, 2] < -0.7, -0.7 + (V[:, 2] + 0.7) * 0.6, V[:, 2])
    return V


def pawmo(src, dest):
    k = Kit(src, coat="fur")

    # --- head and body one bean, no neck --------------------------------------------------------------------------
    k.add("head", ell(HC, HR), FUR, "head")
    k.add("body", ell(BC, BR, deform=bean), FUR, "body")
    k.add("chest", ell((0, -0.004, 0.235), (0.1, 0.082, 0.07)), FUR, "body")

    # the lock of fur swept from the crown over to +X
    k.add("lock", tube([(-0.035, -0.02, 0.37), (0.01, -0.03, 0.402), (0.06, -0.03, 0.408), (0.105, -0.025, 0.388),
                        (0.128, -0.03, 0.36)], [0.04, 0.05, 0.048, 0.032, 0.014]), FUR, "head")

    # --- ears: the -X one upright with its lobe, the +X one flopped out to the side ---------------------------------
    pm.ear(k, (-0.06, -0.01, 0.34), (-0.14, 0.06, 1), 0.19, 0.046, 0.022, "earL", mirror=False, color=FUR)
    pm.ear(k, (0.07, -0.0, 0.345), (0.82, 0.12, 0.58), 0.18, 0.046, 0.022, "earR", mirror=False, lobe=False,
           color=FUR, front=(0, -1, 0.3))

    # --- arms: orange above the elbow, cream forearms and big round fists forward and down --------------------------
    k.add("upperarm", tube([(0.08, -0.01, 0.215), (0.112, -0.035, 0.19), (0.122, -0.06, 0.165)], [0.034, 0.031, 0.03]),
          FUR, "arm{s}", mirror=True)
    k.add("forearm", tube([(0.122, -0.062, 0.163), (0.112, -0.095, 0.135), (0.098, -0.118, 0.12)],
                          [0.032, 0.036, 0.04]), CREAM, "arm{s}", mirror=True)
    k.add("fist", ell((0.092, -0.13, 0.115), (0.046, 0.042, 0.046)), CREAM, "arm{s}", mirror=True)

    # --- short legs, brown feet --------------------------------------------------------------------------------------
    k.add("thigh", ell((0.055, -0.005, 0.07), (0.052, 0.054, 0.062)), FUR, "leg{s}", mirror=True)
    k.add("foot", ell((0.056, -0.036, 0.02), (0.036, 0.048, 0.021)), FEET, "leg{s}", mirror=True)

    # --- a small forked cream tail behind, run back and out to +X ---------------------------------------------------
    for d in (unit(0.45, 0.65, 0.62), unit(0.62, 0.72, 0.3)):
        pm.tail_leaf(k, (0.03, 0.085, 0.12), d, 0.1, 0.026, 0.014, "tail", color=CREAM)

    # --- the face ------------------------------------------------------------------------------------------------------
    for s in (1, -1):
        p, n = on_head(0.078 * s, 0.272)
        k.decal(p, n, (0.024, 0.026), pm.YELLOW, eps=0.0025, rings=10, segs=40)
    p, n = on_head(0.0, 0.262)
    k.decal(p, unit(0, -1, -0.1), (0.045, 0.036), CREAM, eps=0.0025, rings=12, segs=56)
    p, n = on_head(0.0, 0.289)
    k.decal(p, unit(0, -1, 0.05), (0.0065, 0.0045), pm.NOSE, eps=0.0045, rings=5, segs=24)
    CM, CT = srgb(pm.MOUTH), srgb(pm.TONGUE)
    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        c[(u / 0.7) ** 2 + ((v + 0.55) / 0.5) ** 2 < 1] = CT
        return c
    p, n = on_head(0.0, 0.257)
    k.decal(p, unit(0, -1, -0.15), (0.015, 0.017), mouth, eps=0.0045, rings=12, segs=56)

    # --- the fists' palms: a yellow pad in a white star, turned to the front -----------------------------------------
    for s in (1, -1):
        pm.pad(k, (0.086 * s, -0.17, 0.124), unit(0.15 * s, -1, 0.05), 0.026)

    k.build(voxel=0.0048, band=0.006, wband=0.04)

    E, n = on_head(0.042, 0.31)
    cut = pm.cut_eye(0.36, 0.38)           # the scowl: the brow lower toward the nose
    k.eye_states(lambda state: pm.lay_eyes(k, state, E, n, (0.022, 0.024), FUR, cut=cut))
    k.export(dest)


if __name__ == "__main__":
    pawmo(*args())
