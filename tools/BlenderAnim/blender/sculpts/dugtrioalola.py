# Dugtrio-Alola sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dugtrioalola.py -- <the export's Dugtrio-Alola.glb> <out.glb>
#
# Copied from dugtrio.py (which builds as it is read), grown from diglett.py and diglettalola.py's colours: three of the
# Alolan Digletts in one patch of dark broken earth, each wearing a head of pale golden hair, the hair the largest mass.
# From the references: the tallest behind in the middle wears long straight hair parted in the middle, falling to
# either side behind the other two and trailing out in a long wavy lock on the viewer's right; the one before it on
# the viewer's left (-X, "left") wears a big wavy bob, wide and curly, down to the nose; the smallest on the right (+X,
# "right") wears a blunt fringe cut straight across over the eyes, with the sides falling to its chin. Each head of
# hair is one fused mass over the dome, its face side drawn in under the skin so the face shows through.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sibling, lumpy, smoothstep, args

d = sibling("diglett")
da = sibling("diglettalola")

HAIR = "#F2DC82"
HAIR_DEEP = "#E4C866"


def hairdo(k, tag, foot, R, top, bone, yaw, style):
    """A head of hair over one Diglett's dome: an ellipsoid shell, curled or straight, with its face window drawn in
    under the skin. style: 'part' (straight, parted in the middle), 'curl' (a wavy bob), 'fringe' (blunt bangs)."""
    zc = top - R
    D = d.DEPTH
    base = np.array([foot[0], foot[1], 0.0])
    C = base + np.array([0, 0, zc])
    if style == "curl":
        rad = np.array([1.32, 1.2 * D, 1.2])
        off = np.array([0.0, 0.14, 0.12])
        line = lambda x: 0.56 - 0.35 * x * x
        width = 0.66
    elif style == "fringe":
        rad = np.array([1.2, 1.14 * D, 1.16])
        off = np.array([0.0, 0.12, 0.1])
        line = lambda x: 0.47 + 0.0 * x
        width = 0.7
    else:
        rad = np.array([1.2, 1.14 * D, 1.15])
        off = np.array([0.0, 0.12, 0.1])
        line = lambda x: 0.66 - 0.42 * np.abs(x)
        width = 0.62

    def shape(V):
        V = V.copy()
        if style == "curl":
            V = lumpy(7, 5, 0.07)(V)
            flare = 1 + 0.22 * np.clip(-V[:, 2] + 0.2, 0, 1.2)
            V[:, 0] *= flare; V[:, 1] *= 1 + 0.08 * np.clip(-V[:, 2], 0, 1)
        q = V * rad + off                          # in R units round the dome's middle (y forward is negative)
        hz = np.where(q[:, 2] > 0, np.sqrt(q[:, 0] ** 2 + (q[:, 1] / D) ** 2 + q[:, 2] ** 2),
                      np.sqrt(q[:, 0] ** 2 + (q[:, 1] / D) ** 2))
        kk = 0.88 / np.maximum(hz, 0.3)
        inner = q * kk[:, None]
        inner[:, 2] = np.where(q[:, 2] > 0, q[:, 2] * kk, q[:, 2])     # below the dome the wall is upright
        w = (smoothstep(0.02, -0.25, q[:, 1]) * smoothstep(width + 0.06, width - 0.06, np.abs(q[:, 0]))
             * smoothstep(0.05, -0.05, q[:, 2] - line(q[:, 0])))
        q = q * (1 - w[:, None]) + inner * w[:, None]
        return (q - off) / rad

    centre = C + d.turn(off * R, yaw)
    k.add(f"hair{tag}", ell(centre, rad * R, rot=(0, 0, yaw), deform=shape, sub=5), HAIR, bone, surface="hair")
    if style == "fringe":
        # The sides fall straight to the chin, behind the cheeks
        for s in (1, -1):
            c = base + d.turn((s * 0.93 * R, 0.18 * R, zc - 0.25 * R), yaw)
            k.add(f"side{tag}{'R' if s > 0 else 'L'}", ell(c, (0.3 * R, 0.62 * R, 0.62 * R), rot=(0, 0, yaw)),
                  HAIR, bone, surface="hair")
    if style == "curl":
        # Curls round the bob's lower edge
        for i, a in enumerate(np.linspace(20, 160, 6)):
            t = math.radians(a)
            c = base + d.turn((math.cos(t) * 1.28 * R, math.sin(t) * 0.75 * R, zc - 0.32 * R), yaw)
            k.add(f"curl{tag}{i}", ell(c, (0.3 * R, 0.3 * R, 0.3 * R)), HAIR, bone, surface="hair")


def long_locks(k, R, top, foot):
    """The middle one's long hair: a lock down each side behind the others, the right one trailing out in waves."""
    zc = top - R
    x0, y0 = foot
    k.add("lockL", tube([(x0 - 0.98 * R, y0 + 0.2 * R, zc + 0.2 * R), (x0 - 1.15 * R, y0 + 0.35 * R, zc - 0.6 * R),
                         (x0 - 1.35 * R, y0 + 0.45 * R, zc - 1.4 * R), (x0 - 1.6 * R, y0 + 0.5 * R, zc - 2.3 * R)],
                        [0.42 * R, 0.4 * R, 0.36 * R, 0.28 * R]), HAIR, "body", surface="hair")
    k.add("lockR", tube([(x0 + 0.98 * R, y0 + 0.2 * R, zc + 0.2 * R), (x0 + 1.12 * R, y0 + 0.45 * R, zc - 0.6 * R),
                         (x0 + 1.32 * R, y0 + 0.55 * R, zc - 1.5 * R), (x0 + 1.6 * R, y0 + 0.6 * R, zc - 2.35 * R),
                         (x0 + 2.0 * R, y0 + 0.55 * R, zc - 2.85 * R), (x0 + 2.4 * R, y0 + 0.5 * R, zc - 2.7 * R)],
                        [0.42 * R, 0.42 * R, 0.4 * R, 0.36 * R, 0.3 * R, 0.22 * R]), HAIR, "body", surface="hair")


if __name__ == "__main__":
    src, out = args()
    da.colour(d)
    k = Kit(src, coat="scales")
    d.burrow(k, 0.32, 0.26, 17, 84, centre=(0.0, -0.012))
    specs = [
        d.diglett(k, "M", (0.0, 0.06), 0.122, 0.62, "body"),
        d.diglett(k, "L", (-0.15, -0.05), 0.117, 0.47, "left", yaw=-10),
        d.diglett(k, "R", (0.15, -0.07), 0.107, 0.40, "right", yaw=10),
    ]
    hairdo(k, "M", (0.0, 0.06), 0.122, 0.62, "body", 0, "part")
    long_locks(k, 0.122, 0.62, (0.0, 0.06))
    hairdo(k, "L", (-0.15, -0.05), 0.117, 0.47, "left", -10, "curl")
    hairdo(k, "R", (0.15, -0.07), 0.107, 0.40, "right", 10, "fringe")
    k.build(voxel=0.006, smooth=3, band=0.004)
    k.eye_states(lambda s: d.eyes(k, specs, s))
    k.export(out)
