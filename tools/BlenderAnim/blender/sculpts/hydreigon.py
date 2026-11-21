# Hydreigon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/hydreigon.py -- <the export's Hydreigon.glb> <out.glb>
# Grown from deino.py (its heads, ragged fur and colours, by import). From the references: a three-headed dragon that
# hovers. The main head is blue with no hair, a crown of long magenta spikes round its back and top, red eyes in dark
# sockets, an open mouth pink inside with white teeth, on a short grey-brown neck. Grey-brown shaggy fur over the
# shoulders and the arms; each arm ends in a smaller head of its own: a blue dome over the face (no eyes), a few
# magenta spikes on top, its mouth open and pink. The belly blue with magenta chevrons; two short blue legs hanging,
# ending in ragged grey-brown fur; a long magenta tail with grey-brown bands; six long ragged wings, three a side.
# (The task's text spoke of a black collar of petals and a pink-ringed neck: the pictures have a magenta crown of
# spikes, a grey-brown neck and magenta stripes on the belly; the wings and fur are grey-brown, not black.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, hull, ring_band, srgb, inside, unit, args, sibling

d = sibling("deino")
BLUE, MOP, TOOTH = d.BLUE, d.MOP, d.TOOTH
MAGENTA = "#D21F65"
TAIL = "#B42460"
SOCKET, IRIS = srgb("#4A1426"), srgb("#E8283C")
INK = srgb("#17141A")


def red_eye(u, v):
    """The main head's eye (u away from the snout): a dark socket, a red iris toward the snout, a black pupil."""
    c = np.tile(SOCKET, (len(u), 1))
    c[(u + 0.15) ** 2 + (v + 0.02) ** 2 < 0.55 ** 2] = IRIS
    c[(u + 0.15) ** 2 / 0.16 ** 2 + (v + 0.02) ** 2 / 0.4 ** 2 < 1] = INK
    return c


# an arm head's crest
SMALL = [((0.0, 0.3, 0.75), (0, 0.6, 1.0), 1.05, 0.34)]
for s in (1, -1):
    SMALL += [((0.45 * s, 0.25, 0.6), (0.6 * s, 0.6, 0.9), 0.9, 0.3)]


def hydreigon(src, out):
    k = Kit(src, coat="fur")

    # the trunk: blue, a pear hanging under the shoulders, magenta chevrons down the belly
    # leaning forward a little: it hovers
    k.add("torso", ell((0, 0.0, 0.47), (0.12, 0.11, 0.13), rot=(16, 0, 0)), BLUE, "body")
    def chevrons(P):
        sV = P[:, 2] + 0.75 * np.abs(P[:, 0])
        band = inside(np.abs(np.mod(sV - 0.39, 0.065) - 0.032) - 0.012, 0.003)
        return band * inside(P[:, 2] - 0.52, 0.004) * inside(P[:, 1] - 0.03, 0.01)
    k.paint(chevrons, MAGENTA, on=["torso"])
    # the shaggy fur over the shoulders and chest, ragged round its lower edge
    k.add("ruff", ell((0, -0.01, 0.575), (0.165, 0.145, 0.1),
                      deform=d.spiky(90, 0.45, 0.2, seed=8, keep=lambda D: D[:, 2] > 0.45)), MOP, "body")
    k.add("ruffback", ell((0, 0.06, 0.6), (0.12, 0.1, 0.08)), MOP, "body")
    # two short legs tucked up under the belly, ending in ragged fur
    k.add("leg", tube([(0.055, 0.02, 0.4), (0.068, 0.04, 0.355), (0.074, 0.065, 0.33)], [0.05, 0.046, 0.042]),
          BLUE, "body", mirror=True)
    k.add("cuff", ell((0.075, 0.072, 0.322), (0.042, 0.04, 0.034),
                      deform=d.spiky(16, 0.45, 0.32, seed=3, keep=lambda D: D[:, 2] > 0.2)), MOP, "body", mirror=True)
    # the long tail, down and back, magenta with grey-brown bands
    tp = [(0, 0.05, 0.42), (0, 0.11, 0.33), (0.015, 0.16, 0.23), (0.025, 0.19, 0.13)]
    k.add("tail", tube(tp, [0.05, 0.04, 0.03, 0.016]), TAIL, "tail")
    for j, (a, b, r) in enumerate(((1, 2, 0.04), (2, 3, 0.031))):
        p, q = np.array(tp[a]), np.array(tp[b])
        m = (p + q) / 2
        k.add(f"tailband{j}", ring_band(m, q - p, r - 0.002, 0.03, thick=0.008), MOP, "tail", fuse=False,
              surface="fur")
    k.add("tailtuft", ell((0.025, 0.19, 0.12), (0.03, 0.03, 0.03),
                          deform=d.spiky(14, 0.5, 0.35, seed=5, keep=lambda D: D[:, 2] > 0.3)), MOP, "tail")

    # six long ragged wings, three a side: a mirror pair each, raised and swept back, faces to the front and back
    root = np.array((0.075, 0.09, 0.62))
    WING = [(0.0, -0.032), (0.12, -0.03), (0.2, -0.022), (0.17, -0.008), (0.27, -0.006), (0.22, 0.008),
            (0.3, 0.0), (0.24, 0.022), (0.13, 0.03), (0.0, 0.032)]
    for j, (dx, dy, dz, scale) in enumerate(((0.55, 0.55, 0.75, 1.0), (0.8, 0.5, 0.3, 1.0), (0.85, 0.45, -0.1, 0.85))):
        ex = unit(dx, dy, dz)
        ez = np.cross(ex, (0, 1.0, 0)); ez /= np.linalg.norm(ez)
        ez = ez if ez[2] >= 0 else -ez
        outline = [(a * scale, b) for a, b in WING]
        k.add(f"wing{j}", slab(outline, root + np.array((0, 0.006 * j, 0)), ex, ez, 0.026), MOP, "wing{s}",
              fuse=False, mirror=True, surface="fur")

    # the arms: thick fur, forward and down at some 45 degrees, a head at each end hanging before the body's sides
    heads = []
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"arm{side}", tube([(0.08 * s, -0.03, 0.57), (0.13 * s, -0.11, 0.5), (0.16 * s, -0.17, 0.43)],
                                 [0.052, 0.048, 0.05]), MOP, f"arm{side}")
        # a broad ragged collar of fur round the wrist
        k.add(f"wrist{side}", ell((0.165 * s, -0.18, 0.42), (0.075, 0.055, 0.075),
                                  deform=d.spiky(22, 0.45, 0.28, seed=2, keep=lambda D: D[:, 1] < -0.4)),
              MOP, f"arm{side}")
        heads.append(d.Head(k, side, f"arm{side}", (0.175 * s, -0.255, 0.39), unit(0.15 * s, -1, -0.15), 0.095,
                            mop=BLUE, crest=SMALL, crest_colour=MAGENTA, gape=1.25, snout=1.45))

    # the main head on its short neck
    k.add("neck", tube([(0, -0.02, 0.6), (0, -0.07, 0.66), (0, -0.11, 0.7)], [0.055, 0.05, 0.055]), MOP, "head")
    main = d.Head(k, "M", "head", (0, -0.15, 0.725), unit(0, -1, -0.2), 0.119, mop=None,
                  gape=1.1, teeth=True, eyes=red_eye, snout=1.4, ink=INK)
    heads.append(main)
    # its ruff: a few broad flattened magenta petals fanned back round the top and sides of the head
    M, r = main.M, main.r
    for j, th in enumerate(np.radians([-120, -85, -50, -17, 17, 50, 85, 120])):
        sn, cs = math.sin(th), math.cos(th)
        base = main.at((0.78 * sn, 0.3, 0.1 + 0.7 * cs))
        dd = M @ unit(sn, 0.85, 0.85 * cs)
        tan = M @ unit(cs, 0, -sn)
        tan = tan - dd * (tan @ dd); tan /= np.linalg.norm(tan)
        nrm = np.cross(dd, tan)
        L, w, t = (1.1 if abs(th) < 1.6 else 0.85) * r, 0.5 * r, 0.03
        b = base - dd * 0.02
        pts = [b + tan * w / 2 + nrm * t / 2, b + tan * w / 2 - nrm * t / 2,
               b - tan * w / 2 + nrm * t / 2, b - tan * w / 2 - nrm * t / 2]
        m = base + dd * L * 0.45
        pts += [m + tan * w * 0.42 + nrm * t * 0.4, m + tan * w * 0.42 - nrm * t * 0.4,
                m - tan * w * 0.42 + nrm * t * 0.4, m - tan * w * 0.42 - nrm * t * 0.4, base + dd * L]
        k.add(f"petal{j}", hull(pts), MAGENTA, "head", fuse=False, surface="hair")

    k.build(voxel=0.0075, smooth=4, band=0.006)
    k.eye_states(lambda state: [h.lay(state) for h in heads])
    k.export(out)


if __name__ == "__main__":
    hydreigon(*args())
