# Dunsparce sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dunsparce.py -- <the export's Dunsparce.glb> <out.glb>
# From the references: a fat pale yellow grub lying on the ground; a great round head in front, its eyes two big blue
# rings filled cream with a dark slit slanting across (always shut), its lower front blue with three blue spikes under
# the chin; a thick body behind, cream and blue bands across its back, its underside blue; two small white wings, each
# of two lobes, standing up from the back; the body narrows to a thin stalk that rises behind into a yellow drill
# ringed with dark lines.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, hull, slab, lathe, ring_band

YELLOW, BLUE, PALE, WING, GROOVE, SLIT = "#F2DE72", "#3AA6C2", "#F6EBBE", "#F4F4F0", "#8E7E36", "#3A3026"

HC = np.array([0.0, -0.13, 0.158])        # the head's middle
HR = np.array([0.138, 0.13, 0.126])
BODY = [(0, -0.08, 0.15), (0, 0.02, 0.14), (0, 0.11, 0.135), (0, 0.18, 0.135)]
BRAD = [0.1, 0.1, 0.092, 0.068]
DRILL = np.array([0.0, 0.28, 0.245])      # the drill's base
DD = unit(0, 0.35, 1)
DROP = -0.022                             # how far the whole is set down from where the parts are written


def wing(k, side):
    """A small white wing of two thick lobes, raised out and back, its faces turned for both of the game's views."""
    root = np.array([0.055 * side, 0.0, 0.215])
    for i, (d, L, w) in enumerate([(unit(0.42 * side, 0.18, 0.89), 0.115, 0.04), (unit(0.62 * side, 0.55, 0.56), 0.085, 0.032)]):
        n = unit(0.72 * side, 0.6, -0.1)
        ex = np.cross(d, n)
        t = np.linspace(0, 1, 18)
        half = w * np.sqrt(t) * (1 - t) ** 0.55 * 1.55
        outline = [(half[j], t[j] * L) for j in range(len(t))] + [(-half[j], t[j] * L) for j in range(len(t) - 2, 0, -1)]
        k.add(f"wing{i}{'R' if side > 0 else 'L'}", slab(outline, root + (0, 0.012 * i, -0.008 * i), ex, d, 0.02),
              WING, "wingR" if side > 0 else "wingL", fuse=False)


def drill(k):
    """The drill: a cone of four tiers, each standing a little proud of the next, a dark groove under each lip."""
    L, R0, lip = 0.17, 0.052, 0.006
    r = lambda s: R0 * (1 - s / L)
    cuts = [0.0, 0.05, 0.09, 0.125, L]
    prof = [(0.0, 0.0), (R0, 0.0)]
    for a, b in zip(cuts, cuts[1:]):
        if a > 0:
            prof.append((r(a) + lip, a))
        prof.append((max(r(b), 0.0) if b < L else 0.0, b))
    k.add("drill", lathe(DRILL, DD, prof, seg=44), YELLOW, "tail", fuse=False, surface="shell")
    for a in cuts[1:-1]:
        k.add(f"groove{int(a * 1000)}", ring_band(DRILL + DD * (a - 0.004), DD, r(a) + 0.0015, 0.007, thick=0.004, nu=49),
              GROOVE, "tail", fuse=False, surface="shell")


def dunsparce(src, out):
    k = Kit(src, coat="scales")

    # The great round head and the thick body behind it, lying on the ground
    k.add("head", ell(HC, HR), YELLOW, "head")
    k.add("body", tube(BODY, BRAD, seg=24), YELLOW, "body")
    # the stalk, narrowing and rising behind into the drill's round base
    k.add("tail", tube([(0, 0.17, 0.135), (0, 0.235, 0.165), (0, 0.262, 0.21), (0, 0.272, 0.235)],
                       [0.06, 0.034, 0.034, 0.042], seg=20), YELLOW, "tail")
    k.add("tailbase", ell(DRILL + (0, -0.004, -0.008), (0.05, 0.05, 0.04), rot=(-19, 0, 0)), YELLOW, "tail")
    drill(k)

    # The blue underside: the lower front of the head and the belly, crisp
    k.paint(lambda P: inside(P[:, 2] - (0.05 + 0.08 * np.clip(-(P[:, 1] + 0.16), 0, 1)), 0.003) * (P[:, 1] < 0.16),
            BLUE, on=["head", "body"])
    # cream and blue bands across the back
    def back(P):
        up = np.abs(np.arctan2(P[:, 0], P[:, 2] - 0.108)) < 1.05
        return up * inside(-0.045 - P[:, 1], 0.003) * inside(P[:, 1] - 0.155, 0.003) * (P[:, 2] > 0.078)
    k.paint(back, PALE, on=["body"])
    def stripes(P):
        up = np.abs(np.arctan2(P[:, 0], P[:, 2] - 0.108)) < 1.05
        m = np.zeros(len(P))
        for a, b in ((-0.012, 0.028), (0.07, 0.11)):
            m = np.maximum(m, inside(a - P[:, 1], 0.003) * inside(P[:, 1] - b, 0.003))
        return up * m * (P[:, 2] > 0.078)
    k.paint(stripes, BLUE, on=["body"])

    # Three blue spikes under the chin
    for i, (base, d) in enumerate([((0, -0.235, 0.075), unit(0, -0.45, -0.9)),
                                   ((0.072, -0.205, 0.075), unit(0.45, -0.35, -0.82)),
                                   ((-0.072, -0.205, 0.075), unit(-0.45, -0.35, -0.82))]):
        base = np.array(base); a, b = np.cross(d, (0, 0, 1.0)), None
        a = a / np.linalg.norm(a) if np.linalg.norm(a) > 1e-3 else np.array([1.0, 0, 0])
        b = np.cross(d, a)
        pts = [base - d * 0.02 + 0.022 * (math.cos(t) * a + math.sin(t) * b) for t in np.linspace(0, 2 * math.pi, 7)[:-1]]
        pts.append(base + d * 0.05)
        k.add(f"spike{i}", hull(pts), BLUE, "head", fuse=False, surface="shell")

    # Two small white wings
    wing(k, 1); wing(k, -1)

    # The eyes: big blue rings filled cream, a dark slit slanting across, always shut
    CB, CP, CS = srgb(BLUE), srgb(PALE), srgb(SLIT)
    def slit(tilt):
        def look(u, v):
            c = np.tile(CP, (len(u), 1))
            c[np.abs(v - tilt * u) / math.sqrt(1 + tilt * tilt) < 0.13] = CS
            return c
        return look
    def ringed(f):
        def look(u, v):
            c = f(u, v)
            c[u * u + v * v > 0.74 ** 2] = CB
            return c
        return look
    de = unit(0.68, -0.68, 0.25)
    E = HC + de / np.linalg.norm(de / HR) + (0, 0, DROP)
    # Everything set down DROP, so the head and belly rest on the ground (the joints stay where they are)
    for p in k.parts:
        p["V"] = p["V"] + np.array([0, 0, DROP])
    def eyes(state):
        if state == "Open":
            f = slit(-0.75)
        elif state == "Fierce":
            f = slit(0.75)
        else:
            f = eye_look(state, slit(-0.75), CP, CS, line=0.15)
        k.decal(E, de, (0.056, 0.056), ringed(f), eps=0.005, rings=20, segs=56, mirror=True)

    k.build(voxel=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    dunsparce(*args())
