# Orthworm sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/orthworm.py -- <the export's Orthworm.glb> <out.glb>
# From the references (they overrule the task's "rearing upright out of the ground with stubs like arms": both
# pictures and the skeleton show a whole worm lying on the ground, its front reared in an S, and what the task took for
# stubs are round blue ports): a coral-red worm of fat ring segments, its underside peach. From the tail: three red
# segments tapering to a round end, a big peach segment banded red at its ends, a small red ring, then a great silver
# steel segment behind the head; blue ports on the segments' sides (three on the silver one). The head is a red dome,
# as big as the silver segment, a seam or two across its crown, its lower front one wide open dark mouth under a peach
# lip and jaw, two small pale blue eyes with dark middles over the mouth.
# Ratios (the art): head height / total height 0.38, head / silver segment 1.05, tail end / head 0.55.
# Skeleton: body (0, 0, 0.2), seg2 (0.3, 0.05, 0.1) ... seg10 and head (-0.15, -0.1, 0.38); nothing past seg2, so the
# tail's end is on body (as the game's own sculpt has it).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args

RED, PEACH, SILVER, BLUE, RIM = "#E8604E", "#F6C2A8", "#D8D6D8", "#4FA8E0", "#8E95A6"
MOUTH, LIP, INK, SEAM = "#4A1C24", "#F8CCB6", "#1E3A6E", "#B44234"

# (centre, radius, bone, colour) of each ring segment from the tail's end to the neck: the tail curls back behind
SEGS = [((0.35, 0.15, 0.066), 0.062, "body", RED),
        ((0.295, 0.065, 0.074), 0.07, "seg2", RED),
        ((0.21, 0.03, 0.082), 0.078, "seg3", RED),
        ((0.12, 0.012, 0.09), 0.084, "seg4", RED),
        ((0.025, -0.008, 0.102), 0.096, "seg6", PEACH),
        ((-0.055, -0.03, 0.158), 0.082, "seg7", RED),
        ((-0.105, -0.06, 0.255), 0.108, "seg9", SILVER)]
HC = np.array([-0.15, -0.115, 0.43])
HR = np.array([0.118, 0.112, 0.105])
FACE = unit(-0.38, -0.9, -0.1)


def on_ell(c, r, d):
    c, r = np.array(c, float), np.array(r, float)
    d = unit(*d)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def along(i):
    """The body's direction at segment i (tail to head)."""
    a = np.array(SEGS[max(i - 1, 0)][0]); b = np.array(SEGS[min(i + 1, len(SEGS) - 1)][0]) if i + 1 < len(SEGS) else HC
    return unit(*(b - a))


def orthworm(src, out):
    k = Kit(src, coat="shell")

    # the ring segments, each an egg laid along the body, joined by a core
    pts = [np.array(c) for c, _, _, _ in SEGS] + [HC + (0.0, 0.03, -0.07)]
    k.add("core", tube(pts, [0.05, 0.055, 0.06, 0.066, 0.072, 0.07, 0.08, 0.085]), RED, "seg5")
    for i, (c, r, bone, col) in enumerate(SEGS):
        d = along(i)
        yaw = math.degrees(math.atan2(d[1], d[0]))
        pitch = math.degrees(math.asin(np.clip(d[2], -1, 1)))
        k.add(f"seg{i}", ell(c, (r * 1.05, r, r), rot=(0, -pitch, yaw)), col, bone,
              surface="metal" if col == SILVER else None)

    # the head: a red dome, its open mouth on the lower front
    k.add("head", ell(HC, HR), RED, "head")
    k.add("jaw", ell(HC + FACE * 0.02 + (0, 0, -0.05), (0.1, 0.09, 0.06)), PEACH, "head")
    MC = HC + FACE * 0.1 + np.array([0, 0, -0.03])
    k.add("mouth", ell(MC, (0.082, 0.05, 0.038), rot=(0, 0, math.degrees(math.atan2(FACE[0], -FACE[1])) * -1)),
          MOUTH, "head", carve=True)

    # the peach underside of the red segments, the belly turned down and a little to the front
    cs = np.array([c for c, _, _, _ in SEGS]); rs = np.array([r for _, r, _, _ in SEGS])
    def belly(P):
        dd = np.linalg.norm(P[:, None, :] - cs[None], axis=2) / rs[None]
        j = dd.argmin(1)
        rel = (P - cs[j]) / rs[j][:, None]
        down = rel @ unit(0, -0.35, -1)
        return inside(0.42 - down, 0.04) * (P[:, 2] < 0.32)
    k.paint(belly, PEACH, on=["seg0", "seg1", "seg2", "seg3", "seg5", "core"])
    # red bands at the big peach segment's two ends
    c4, r4 = np.array(SEGS[4][0]), SEGS[4][1]
    d4 = along(4)
    k.paint(lambda P: inside(0.62 - np.abs((P - c4) @ d4) / r4, 0.04) * 0 + inside(np.abs(np.abs((P - c4) @ d4) / r4 - 0.82) - 0.12, 0.04)
            * inside(np.linalg.norm(P - c4, axis=1) - r4 * 1.2, 0.004), RED, on=["seg4"])
    # the head's peach jaw and lip, and two seams across its crown
    k.paint(lambda P: inside(np.linalg.norm((P - HC) / HR, axis=1) - 1.25, 0.01) * inside(-((P - HC) @ unit(-0.35, -0.75, 0.56)) + 0.0, 0.01)
            * inside(P[:, 2] - HC[2] + 0.005, 0.006), LIP, on=["head", "jaw"])
    for off in (0.02, 0.07):
        k.paint(lambda P, off=off: inside(np.abs((P - HC) @ unit(-0.38, -0.9, 0.0) + off * 0 - (-0.005 + (off - 0.045) * 1.4)) - 0.004, 0.002)
                * inside(HC[2] + 0.04 - P[:, 2], 0.004) * inside(np.linalg.norm(P - HC, axis=1) - 0.13, 0.004), SEAM, on=["head"])
    # the mouth's dark lining
    k.paint(lambda P: inside(np.linalg.norm((P - MC) / np.array([0.084, 0.052, 0.042]), axis=1) - 1.0, 0.004), MOUTH,
            on=["head", "jaw"])

    # blue ports in grey rims on the segments' sides (both sides)
    CB, CR = srgb(BLUE), srgb(RIM)
    def port(u, v):
        c = np.tile(CB, (len(u), 1))
        c[u * u + v * v > 0.78 ** 2] = CR
        return c
    for i, h in ((1, 0.024), (3, 0.026), (4, 0.03), (6, 0.03)):
        c, r = np.array(SEGS[i][0]), SEGS[i][1]
        d = along(i)
        side = np.cross(d, (0, 0, 1.0)); side /= np.linalg.norm(side)
        for s in (1, -1):
            n = unit(*(side * s + np.array([0, 0, 0.25])))
            if i == 6:
                n = unit(*(np.array([-0.2, -1.0, 0.1]) * (1 if s > 0 else -1) + np.array([0.5 * s, 0, 0])))
            p, nn = on_ell(c, (r * 1.05, r, r), n)
            k.decal(p, nn, (h, h * 1.15), port, eps=0.003, rings=12, segs=40)
            if i == 6:
                for dz in (-0.055, 0.055):
                    p2, n2 = on_ell(c + (0, 0, dz), (r * 1.05, r, r), n)
                    k.decal(p2, n2, (0.02, 0.022), port, eps=0.003, rings=12, segs=40)

    k.build(voxel=0.0055, smooth=4, band=0.008)

    # The eyes: small pale blue ovals with dark middles, close above the mouth
    CB, CI, CR = srgb("#8ECBF0"), srgb(INK), srgb(RED)
    def open_eye(u, v):
        c = np.tile(CB, (len(u), 1))
        c[(u / 0.5) ** 2 + ((v + 0.05) / 0.55) ** 2 < 1] = CI
        c[((u + 0.15) / 0.2) ** 2 + ((v - 0.3) / 0.2) ** 2 < 1] = (0.95, 0.95, 0.95)
        return c
    EP, EN = on_ell(HC, HR, unit(*(FACE + np.array([0, 0, 0.38]))))
    side = np.cross(FACE, (0, 0, 1.0)); side /= np.linalg.norm(side)
    def lay(state):
        look = eye_look(state, open_eye, CR, CI, line=0.18)
        for s in (1, -1):
            p, n = on_ell(HC, HR, unit(*(FACE + np.array([0, 0, 0.42]) + side * 0.3 * s)))
            col = look if s == 1 else (lambda a, b, f=look: f(-a, b))
            k.decal(p, n, (0.019, 0.025), col, eps=0.0025, rings=20, segs=56)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    orthworm(*args())
