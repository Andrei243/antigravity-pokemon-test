# Pawmot sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pawmot.py -- <the export's Pawmot.glb> <out.glb>
#
# The design, from the references, grown from Pawmi's and Pawmo's scripts (the ears with their teal hollows, the brown
# eyes with two glints, yellow cheeks, cream muzzle, the palms' yellow pad in a white star, the cream tail paddle): a
# deep orange mouse standing on short legs, a big pear of a body under a large round head; a tall mop of spiky fur
# swept up and over to +X from the crown, the ears rising out of it; a ruff of pointed tufts of the same orange round
# the neck, hanging over the top of the chest; small round brown eyes with glints; an open smile, dark red with a pink
# tongue, in a broad cream muzzle; big yellow cheeks; short orange upper arms and great cream forearms and fists, an
# orange chevron at each elbow, the palms turned forward; feet of a darker orange; a cream feathery tail behind to +X.
# The pictures overrule the brief: the ruff is the coat's own orange, not cream; the cream is the forearms and fists.
# The art holds the arms out wide: carried here forward and down before the belly.
# The export's joints: body (0, 0, 0.28), head (0, -0.01, 0.39), ears (+-0.06, -0.01, 0.52), arms (+-0.09, -0.01,
# 0.33), legs (+-0.06, 0, 0.18), tail (0, 0.07, 0.22).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, args, outward, sibling, axes, tri

pm = sibling("pawmi")

FUR, CREAM, FEET = "#EE961E", "#F5E1B4", "#C26A1A"

HC, HR = np.array([0.0, -0.022, 0.445]), np.array([0.128, 0.11, 0.11])       # the head
BC, BR = np.array([0.0, 0.012, 0.205]), np.array([0.162, 0.142, 0.2])         # the pear of a body
ELBOW, WRIST = np.array([0.172, -0.062, 0.29]), np.array([0.156, -0.135, 0.24])


def on_head(x, z, lift=0.0):
    q = 1 - (x / HR[0]) ** 2 - ((z - HC[2]) / HR[2]) ** 2
    y = HC[1] - HR[1] * math.sqrt(max(q, 0.0))
    p = np.array([x, y, z])
    g = (p - HC) / HR ** 2
    g /= np.linalg.norm(g)
    return p + g * lift, g


def pear(V):
    V = V.copy()
    s = 1 - 0.16 * V[:, 2]
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.where(V[:, 2] < -0.75, -0.75 + (V[:, 2] + 0.75) * 0.5, V[:, 2])
    return V


def chevrons(P):
    """Orange points into the cream at each elbow, one over the forearm and one under it."""
    Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
    a = unit(*(WRIST - ELBOW))
    e1, e2 = axes(a)
    d = Q - ELBOW
    s = d @ a
    th = np.arctan2(d @ e2, d @ e1)
    edge = 0.012 + 0.04 * (0.5 + 0.5 * tri(th / math.pi + 0.25))
    return inside(s - edge, 0.002)


def pawmot(src, dest):
    k = Kit(src, coat="fur")

    # --- a large round head on a big pear of a body -------------------------------------------------------------------
    k.add("head", ell(HC, HR), FUR, "head")
    k.add("body", ell(BC, BR, deform=pear), FUR, "body")
    k.add("chest", ell((0, -0.01, 0.35), (0.12, 0.1, 0.075)), FUR, "body")

    # the ruff: pointed tufts of the coat's orange round the neck, hanging over the top of the chest
    for i in range(11):
        th = math.radians(-150 + 30 * i)                # 0 straight ahead (-Y)
        o = np.array([math.sin(th), -math.cos(th), 0])
        root = np.array([0, -0.012, 0.338]) + o * 0.108
        tip = np.array([0, -0.012, 0.27 + 0.012 * (i % 2)]) + o * (0.152 + 0.01 * (i % 2))
        k.add("ruff", tube([root, (root + tip) / 2 + np.array([0, 0, 0.006]), tip], [0.036, 0.024, 0.009]), FUR, "body")

    # the mop: spiky locks swept up and over to +X from the crown
    LOCKS = [
        ([(-0.05, -0.02, 0.53), (-0.075, -0.035, 0.585), (-0.07, -0.045, 0.625)], [0.045, 0.03, 0.01]),
        ([(-0.01, -0.03, 0.545), (0.0, -0.045, 0.6), (0.025, -0.05, 0.64)], [0.05, 0.034, 0.01]),
        ([(0.025, -0.02, 0.55), (0.07, -0.03, 0.6), (0.125, -0.03, 0.625)], [0.05, 0.034, 0.01]),
        ([(0.045, -0.04, 0.53), (0.11, -0.055, 0.56), (0.165, -0.05, 0.555)], [0.045, 0.03, 0.01]),
        ([(0.03, 0.03, 0.535), (0.08, 0.05, 0.575), (0.13, 0.06, 0.59)], [0.045, 0.03, 0.01]),
        ([(0.0, -0.08, 0.525), (0.035, -0.112, 0.522), (0.07, -0.118, 0.505)], [0.034, 0.024, 0.008]),
    ]
    for pts, rad in LOCKS:
        k.add("lock", tube(pts, rad), FUR, "head")

    # --- ears rising out of the mop, up and out -------------------------------------------------------------------------
    pm.ear(k, (0.085, -0.01, 0.5), (0.42, 0.08, 0.9), 0.19, 0.052, 0.024, "ear{s}", lobe=False, color=FUR)

    # --- arms: short orange upper arms, great cream forearms and fists forward and down ---------------------------------
    k.add("upperarm", tube([(0.115, -0.01, 0.345), (0.15, -0.035, 0.315), ELBOW], [0.044, 0.042, 0.04]), FUR, "arm{s}",
          mirror=True)
    k.add("forearm", tube([ELBOW, (ELBOW + WRIST) / 2, WRIST], [0.044, 0.052, 0.058]), CREAM, "arm{s}", mirror=True)
    k.add("fist", ell((0.146, -0.165, 0.228), (0.064, 0.058, 0.064)), CREAM, "arm{s}", mirror=True)
    k.paint(chevrons, FUR, on=["forearm"])

    # --- short legs, darker orange feet ---------------------------------------------------------------------------------
    k.add("thigh", ell((0.072, 0.0, 0.1), (0.07, 0.07, 0.08)), FUR, "leg{s}", mirror=True)
    k.add("foot", ell((0.078, -0.042, 0.022), (0.046, 0.062, 0.024)), FEET, "leg{s}", mirror=True)

    # --- the tail: a cream feathery paddle behind, run back and out to +X --------------------------------------------
    pm.tail_leaf(k, (0.04, 0.14, 0.24), unit(0.35, 0.55, 0.76), 0.17, 0.046, 0.02, "tail", color=CREAM)
    pm.tail_leaf(k, (0.05, 0.14, 0.23), unit(0.55, 0.65, 0.5), 0.11, 0.032, 0.018, "tail", color=CREAM)

    # --- the face ---------------------------------------------------------------------------------------------------------
    for s in (1, -1):
        p, n = on_head(0.102 * s, 0.415)
        k.decal(p, n, (0.034, 0.038), pm.YELLOW, eps=0.0025, rings=10, segs=40)
    p, n = on_head(0.0, 0.398)
    k.decal(p, unit(0, -1, -0.12), (0.064, 0.05), CREAM, eps=0.0025, rings=12, segs=56)
    p, n = on_head(0.0, 0.432)
    k.decal(p, unit(0, -1, 0.0), (0.008, 0.0055), pm.NOSE, eps=0.0045, rings=5, segs=24)
    CM, CT = srgb(pm.MOUTH), srgb(pm.TONGUE)
    def smile(u, v):
        c = np.tile(CM, (len(u), 1))
        c[(u / 0.62) ** 2 + ((v + 0.75) / 0.6) ** 2 < 1] = CT
        return c
    def smile_shape(th):
        s_ = math.sin(th)
        return min(1.0, 0.3 / s_) if s_ > 0 else 1.0          # flat along the top, round below
    p, n = on_head(0.0, 0.392)
    k.decal(p, unit(0, -1, -0.2), (0.032, 0.03), smile, eps=0.0045, outline=smile_shape, rings=16, segs=64)

    # --- the fists' palms turned forward: a yellow pad in a white star ---------------------------------------------------
    for s in (1, -1):
        pm.pad(k, (0.14 * s, -0.222, 0.232), unit(0.1 * s, -1, 0.05), 0.032)

    k.build(voxel=0.006, band=0.006, wband=0.05)

    E, n = on_head(0.046, 0.462)
    k.eye_states(lambda state: pm.lay_eyes(k, state, E, n, (0.02, 0.022), FUR))
    k.export(dest)


if __name__ == "__main__":
    pawmot(*args())
