# Doublade sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/doublade.py -- <the export's Doublade.glb> <out.glb>
# Grown from honedge.py (sibling): its sword frame, scabbard, rings with the eye on the top one, mouth and teeth,
# grip and cloth; what the pictures change.
#
# From the references: two of Honedge's swords crossed in an X, hilts up and out, points down, hovering; the sword
# whose hilt is on the left (the viewer's left) lies in front where they cross. Paler than Honedge: the scabbards light
# tan, each with a darker brown lozenge panel down its middle, and below the crossing a dark lozenge between the two
# points. Each hilt pale khaki: the same trefoil of rings (the top one on the sword's line the eye, pink with a dark
# slit; the two lower white with a screw slot), white teeth below them, and a curved sickle of a crossguard behind
# the rings, its two horns bent up toward the pommel. From each pommel a cloth rises, turns outward and hangs down at
# the side: deep magenta at its root, pink below with a darker pink curl, ending in five pink tassels. The brief's
# "pink-purple cloths" are magenta to pink in both pictures.
# Skeleton: root (0, 0, 0) and body (0, 0, 0.231) alone.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, srgb, inside, args, sibling

hd = sibling("honedge")
SCAB, PANEL, HILT = "#BCA680", "#8E7054", "#D8CEA4"
LOZENGE = "#5E5250"
IRIS = "#EC86B8"
CLOTH = ("#9A1E7E", "#C8489E", "#F08CC0")
CURL = "#C8489E"
TASSEL = "#F49ACA"

ANGLE = 45.0          # each sword's lean from upright, its hilt outward
F = 0.85              # the swords' size against Honedge's
CROSS = 0.14          # where along each scabbard (Honedge's lengths) the two cross
CLUSTER = (0.0, 0.318)   # the middle of the three rings, where the sickle curves round


def sickle(side, r=0.112, thick=0.03, a0=125.0, a1=-82.0):
    """The crossguard, a sickle hugging the rings' outer side: one horn up by the sword's line toward the other
    sword, the other down beside the scabbard. Its outline in (t, s), t outward by side."""
    outer, inner = [], []
    for a in np.linspace(a0, a1, 15):
        f = math.sin(math.pi * (a - a1) / (a0 - a1))
        rr = r - thick * max(f, 0.08) ** 0.7
        c, sn = math.cos(math.radians(a)), math.sin(math.radians(a))
        outer.append((side * r * c + CLUSTER[0], CLUSTER[1] + r * sn))
        inner.append((side * rr * c + CLUSTER[0], CLUSTER[1] + rr * sn))
    return outer + inner[::-1]


def frames():
    """The two swords: (frame, side) with side +1 for the hilt on +X; the left-hilted one in front."""
    a = math.radians(ANGLE)
    out = []
    for side, y in ((1, 0.022), (-1, -0.022)):
        d = (side * math.sin(a), 0, math.cos(a))
        s = CROSS * F
        point = (-side * s * math.sin(a), y, 0.165 - s * math.cos(a))
        out.append((hd.Frame(point, d, F), side))
    return out


def doublade(src, out_path):
    k = Kit(src, coat="metal")
    eyes = []
    for i, (fr, side) in enumerate(frames()):
        name = f"scab{i}"
        k.add(name, fr.put(hd.scabbard_shape(bow=0.0, point=0.6)), SCAB, "body")
        def panel(P, fr=fr):
            t, y, s = fr.local(P)
            return inside(np.abs(t) / 0.03 + np.abs(s - hd.SC) / 0.105 - 1, 0.02) * inside(np.abs(y) - 0.024, 0.002)
        k.paint(panel, PANEL, on=[name])
        fronts = hd.rings(k, fr, HILT, name=f"ring{i}_")
        top = hd.grip(k, fr, cap=HILT)
        hd.guard(k, fr, sickle(side), HILT, name=f"guard{i}")
        p = top
        o = np.array([side, 1, 1.0])
        w = lambda u: 0.028 + 0.064 * np.clip(u, 0, 1) ** 1.3
        cl = hd.Cloth([p + (0, 0.012, -0.01), p + o * (0.01, 0.014, 0.03), p + o * (0.026, 0.016, 0.035),
                       p + o * (0.034, 0.02, 0.0), p + o * (0.03, 0.022, -0.09), p + o * (0.026, 0.024, -0.18),
                       p + o * (0.022, 0.024, -0.24)], w)
        # the cloth's own pieces, named for their sword
        edges = [0.0, 0.56, 0.84, 1.0]
        for j, col in enumerate(CLOTH):
            k.add(f"cloth{i}_{j}", cl.piece(edges[j], edges[j + 1]), col, "body", fuse=False, surface="cloth")
        for back in (False, True):
            k.add(f"curl{i}{'b' if back else ''}", cl.curl(0.72, 0.025, back=back), CURL, "body", fuse=False,
                  surface="cloth")
        for j, sl in enumerate(cl.tassels(5, 0.09, spread=0.3)):
            k.add(f"tassel{i}_{j}", sl, TASSEL, "body", fuse=False, surface="cloth")
        for c, turn in zip(fronts[1:], (20 * side, -15 * side)):
            hd.bolt(k, fr, c, turn)
        hd.mouth(k, fr)
        eyes.append((fr, fronts[0]))

    # the dark lozenge between the two points, under the crossing
    z0 = 0.165 - CROSS * F * math.cos(math.radians(ANGLE))
    loz = []
    for y in (-0.008, 0.008):
        loz += [(0, y, z0 - 0.04), (0.03, y, z0 + 0.02), (0, y, z0 + 0.07), (-0.03, y, z0 + 0.02)]
    k.add("lozenge", hull(loz), LOZENGE, "body", fuse=False)

    k.build(voxel=0.0036, smooth=3, band=0.005)

    def lay(state):
        for fr, c in eyes:
            hd.lay_eye(k, fr, c, state, iris=IRIS, lid=HILT)
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    doublade(*args())
