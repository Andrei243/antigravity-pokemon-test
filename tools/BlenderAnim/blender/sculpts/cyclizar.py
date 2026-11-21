# Cyclizar sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cyclizar.py -- <the export's Cyclizar.glb> <out.glb>
#
# From the references: a green lizard standing crouched on two strong legs, its trunk leaning forward. Its head is
# carried forward: a pale green snout, a white eye with a black pupil at its front, half covered by the dark green
# helmet that caps the head and sweeps back into a broad point; an orange round on each side at the helmet's edge.
# Round its neck a great black tyre, ridged in segments, tilted low at the front over a white throat. Long green arms
# hanging forward with three clawed fingers, dark claws; thick thighs, long sloped shins and broad feet of three toes.
# The tail is black and ridged, a dark green stripe along its top, and curls up at its end to a white tip; it runs
# back and out to the +X side.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, eye_look, args

GREEN, LIGHT, DARK, STRIPE = "#86B98F", "#9CCBA3", "#3D5847", "#4E7258"
TYRE, GROOVE = "#2B2F36", "#454B55"
THROAT, ORANGE, CLAW = "#E2EEDF", "#E8683A", "#2B2F36"
WHITE, PUPIL = "#F6F7F2", "#141617"
VOXEL = 0.0072

HC = np.array([0.0, -0.165, 0.775])          # the skull
HS = 1.3                                        # the head grown a third past the measure
EYE_AT = HC + HS * np.array([0.058, -0.06, 0.012])
EYE_N = unit(0.8, -0.52, 0.18)


def tyre(c, axis, R, r, grooves=12, nu=96, nv=18):
    """A ridged ring: a torus whose tube dips a little at each of `grooves` places round it (a loose piece)."""
    a, b = sculptkit.axes(axis)
    d = unit(*axis)
    c = np.array(c, float)
    V, F = [], []
    for i in range(nu):
        t = 2 * math.pi * i / nu
        dip = 1 - 0.16 * max(0.0, math.cos(grooves * t)) ** 8
        ctr = c + R * (math.cos(t) * a + math.sin(t) * b)
        out = math.cos(t) * a + math.sin(t) * b
        for j in range(nv):
            p = 2 * math.pi * j / nv
            V.append(ctr + r * dip * (math.cos(p) * out + math.sin(p) * d))
    for i in range(nu):
        for j in range(nv):
            i1, j1 = (i + 1) % nu, (j + 1) % nv
            F.append([i * nv + j, i1 * nv + j, i1 * nv + j1, i * nv + j1])
    return sculptkit.outward((np.array(V), F))


def claw(k, name, bone, base, d, length, width):
    d = unit(*d)
    a, b = sculptkit.axes(d)
    base = np.array(base, float)
    pts = [base + a * width, base - a * width, base + b * width, base - b * width, base + d * length]
    k.add(name, hull(pts), CLAW, bone, fuse=False, surface="shell")


def eye_open(u, v):
    """A white eye, a black pupil toward the front (-u), the helmet's dark lid along its top."""
    c = np.tile(srgb(WHITE), (len(u), 1))
    c[((u + 0.28) / 0.42) ** 2 + ((v + 0.05) / 0.6) ** 2 < 1] = srgb(PUPIL)
    c[v > 0.5 - 0.15 * u] = srgb(DARK)
    c[(u * u + v * v) > 0.9 ** 2] = srgb(DARK)
    return c


def cyclizar(src, out):
    k = Kit(src, coat="scales")


    # The trunk: leaning well forward, a deep chest, hips low over the thighs
    k.add("hips", ell((0, 0.02, 0.45), (0.118, 0.1, 0.095)), GREEN, "body")
    k.add("chest", ell((0, -0.06, 0.57), (0.096, 0.09, 0.105), rot=(-30, 0, 0)), GREEN, "body")
    k.add("neck", tube([(0, -0.075, 0.63), (0, -0.11, 0.69), (0, -0.14, 0.735)], [0.062, 0.058, 0.056]), GREEN, "head")
    # the white throat under the tyre
    k.paint(lambda P: inside(np.linalg.norm((P - (0, -0.13, 0.64)) / (0.065, 0.045, 0.075), axis=1) - 1, 0.05)
            * (P[:, 1] < -0.09), THROAT)

    # The head (a third larger than measured): a long skull, a pale snout, the dark helmet capping it and swept back
    # into a broad point
    def h(x, y, z):
        return HC + HS * np.array([x, y, z])
    k.add("skull", ell(HC, HS * np.array((0.07, 0.1, 0.068))), LIGHT, "head")
    k.add("snout", ell(h(0, -0.085, -0.022), HS * np.array((0.048, 0.07, 0.042))), LIGHT, "head")
    k.add("helmet", ell(h(0, 0.02, 0.03), HS * np.array((0.078, 0.125, 0.06)), rot=(8, 0, 0)), DARK, "head")
    k.add("crest", tube([h(0, 0.04, 0.045), h(0, 0.12, 0.035), h(0, 0.185, 0.01)], HS * np.array([0.055, 0.04, 0.016])),
          DARK, "head")
    k.add("hood", ell(h(0, 0.07, -0.025), HS * np.array((0.066, 0.06, 0.05))), DARK, "head")
    # the orange rounds at the helmet's edge
    k.decal(h(0.072, 0.03, -0.012), unit(1, 0.15, -0.1), (0.022, 0.022), srgb(ORANGE), eps=0.002, mirror=True)
    # the mouth: a short dark line along each side of the snout's tip
    k.decal(h(0.033, -0.135, -0.045), unit(0.55, -0.7, -0.35), (0.032, 0.0055), srgb(DARK), up=(0, 0, 1),
            eps=0.0018, mirror=True)

    # The tyre round the neck, low at the front
    k.add("tyre", tyre((0, -0.075, 0.65), (0, -0.5, 0.87), 0.1, 0.042), TYRE, "body", fuse=False, surface="shell")

    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        # Legs crouched: great thighs forward and out to the knee, thick shins sloped back, broad three-toed feet
        k.add(f"thigh{side}", ell(np.array((0.112, -0.07, 0.345)) * m, (0.095, 0.13, 0.1), rot=(45, 0, -10 * s)),
              GREEN, f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.14, -0.15, 0.265), (0.135, -0.085, 0.16), (0.128, -0.005, 0.075)]) * m,
                                  [0.064, 0.05, 0.04]), GREEN, f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.125, -0.045, 0.03)) * m, (0.05, 0.078, 0.03)), GREEN, f"leg{side}")
        for j, dx in enumerate((-0.45, 0.0, 0.45)):
            d = unit(dx * s, -1, -0.05)
            b0 = np.array((0.125 * s, -0.085, 0.022))
            tip = b0 + d * 0.06
            k.add(f"toe{side}{j}", tube([b0, tip], [0.02, 0.016]), GREEN, f"leg{side}")
            claw(k, f"toeclaw{side}{j}", f"leg{side}", tip + d * 0.008 - (0, 0, 0.004), d + np.array([0, 0, -0.2]),
                 0.026, 0.01)
        # Arms: long, hanging forward and down to the knees, bent at the elbow, three clawed fingers
        sh, el, wr = np.array((0.075, -0.07, 0.59)), np.array((0.13, -0.11, 0.48)), np.array((0.115, -0.21, 0.355))
        k.add(f"arm{side}", tube(np.array([sh, el, wr]) * m, [0.042, 0.036, 0.032]), GREEN, f"arm{side}")
        k.add(f"hand{side}", ell((wr + (0, -0.015, -0.02)) * m, (0.032, 0.036, 0.03)), GREEN, f"arm{side}")
        for j, dx in enumerate((-0.5, 0.0, 0.5)):
            d = unit(dx * s, -0.55, -1)
            b0 = (wr + (0, -0.02, -0.035)) * m
            tip = b0 + d * 0.045
            k.add(f"finger{side}{j}", tube([b0, tip], [0.014, 0.012]), GREEN, f"arm{side}")
            claw(k, f"claw{side}{j}", f"arm{side}", tip + d * 0.006, d, 0.024, 0.009)

    # The tail: black and ridged, a dark green stripe on top, curling up to a white tip
    C = [(0, 0.06, 0.45), (0.03, 0.15, 0.37), (0.09, 0.25, 0.32), (0.16, 0.33, 0.34), (0.205, 0.38, 0.42),
         (0.215, 0.37, 0.51)]
    R = [0.068, 0.058, 0.046, 0.034, 0.024, 0.014]
    k.add("tail", tube(C, R), TYRE, "tail")
    k.add("tailstripe", tube([np.array(p) + (0, 0, r * 0.62) for p, r in zip(C[:4], R[:4])],
                             [r * 0.62 for r in R[:4]]), STRIPE, "tail")
    line = np.array(C, float)
    dense = np.vstack([line[i] + (line[i + 1] - line[i]) * t for i in range(len(line) - 1)
                       for t in np.linspace(0, 1, 30, endpoint=False)] + [line[-1]])
    arc = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(dense, axis=0), axis=1))])
    def along(P):
        i = np.argmin(((P[:, None, :] - dense[None]) ** 2).sum(2), axis=1)
        return arc[i], np.linalg.norm(P - dense[i], axis=1)
    def grooves(P):
        s, r = along(P)
        near = inside(r - 0.08, 0.002) * (P[:, 1] > 0.09)
        ph = (s / 0.05) % 1.0
        return near * inside(np.abs(ph - 0.5) - 0.06, 0.004) * inside(-(s - 0.08), 0.004) * inside(s - arc[-1] + 0.12, 0.004)
    k.paint(grooves, GROOVE, on=["tail"])
    def tip(P):
        s, r = along(P)
        return inside(arc[-1] - 0.09 - s, 0.004) * inside(r - 0.05, 0.002)
    k.paint(tip, WHITE)

    k.build(voxel=VOXEL, smooth=4, band=0.008, wband=0.05)
    look = lambda state: eye_look(state, eye_open, srgb(LIGHT), srgb(DARK), line=0.2)
    k.eye_states(lambda state: k.decal(EYE_AT, EYE_N, (0.03, 0.024), look(state), up=(0, 0, 1), eps=0.0018,
                                       rings=20, segs=56, mirror=True))
    k.export(out)


if __name__ == "__main__":
    cyclizar(*args())
