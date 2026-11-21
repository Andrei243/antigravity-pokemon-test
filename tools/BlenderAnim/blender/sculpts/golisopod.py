# Golisopod sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/golisopod.py -- <the export's Golisopod.glb> <out.glb>
# Grown from wimpod.py (sibling("wimpod")): the same pale mint-grey plates (their seam triangles now the design's dark
# green triangles on each plate), the same purple, the shell's way of overlapping plates; Wimpod stood up as a knight.
#
# From the references: a great hunched armoured woodlouse standing on two short legs. Its largest mass is the back:
# three broad pale plates arching up and forward over the head like a hood, each with dark green triangles, and pale
# curved horns rising from them up and back. The head sits low in front under the hood: a pale plated helmet over a
# dark face crossed by a purple X whose lower arms splay out as mandibles, two tiny white glints of eyes in the dark
# above the crossing. Under the arch a mauve segmented body curving down to the hips. Two great arms from pale
# shoulder plates: a mauve upper arm and a forearm cased in pale gauntlet rings with green triangles, three long
# black claws at the end, held forward and down. Two small pale arms at the chest with black claws. Short legs: a
# pale plate over each thigh, a pale shin, a foot with two black claws. A short mauve tail with pale plates behind.
# Skeleton: body (0, 0, 0.42), arms (+-0.17, -0.02, 0.52), head (0, -0.07, 0.57), tail (0, 0.08, 0.32),
# legs (+-0.08, 0.02, 0.24).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, lathe, boxy, srgb, inside, unit, eye_look, outward, polygon, \
    sibling, args

W = sibling("wimpod")
PLATE, PURPLE = W.SHELL, "#A250CC"
MARK, MAUVE, MAUVE_DK, FACE, CLAW, GLINT = "#3A7464", "#A88F9E", "#8A7282", "#2A2630", "#2A2E32", "#F6F8FA"
on_ell = W.on_ell

# --- the frame -------------------------------------------------------------------------------------------------------
HD_C, HD_R = np.array([0.0, -0.245, 0.49]), np.array([0.09, 0.086, 0.096])
BACK = [  # centre, radii, turn about x: the three plates of the arch, back to front
    ((0.0, 0.08, 0.6), (0.2, 0.13, 0.11), -25),
    ((0.0, -0.02, 0.66), (0.215, 0.12, 0.105), 0),
    ((0.0, -0.1, 0.635), (0.17, 0.1, 0.09), 28),
]
SHOULDER = np.array([0.18, -0.03, 0.56])
ELBOW, WRIST = np.array([0.255, -0.07, 0.43]), np.array([0.215, -0.26, 0.28])


def frame(d):
    d = unit(*d)
    a = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a)
    return d, a, np.cross(a, d)


def horn(k, name, pts, widths, colour=PLATE, bone="body", flat=0.6):
    """A curved horn or claw as a chain of hulls, a kite section at each point (width w, depth w*flat), to a point."""
    pts = [np.asarray(p, float) for p in pts]
    secs = []
    for i, (p, w) in enumerate(zip(pts, widths)):
        d = pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]
        _, a, b = frame(d)
        secs.append([p + a * w, p - a * w, p + b * w * flat, p - b * w * flat] if w > 0 else [p])
    for i in range(len(secs) - 1):
        k.add(f"{name}{i}", hull(secs[i] + secs[i + 1]), colour, bone, fuse=False, surface="shell")


def triangle_mark(k, c, n, size, up=(0, 0, 1)):
    k.decal(c, n, (size, size), MARK, up=up, eps=0.0025, rings=8, segs=36, outline=W.TRI)


def back(k):
    for i, (c, r, rx) in enumerate(BACK):
        k.add(f"plate{i}", ell(c, r, rot=(rx, 0, 0), deform=boxy(2.6)), PLATE, "body")
        # two green triangles on each plate's top, pointing forward
        for s in (1, -1):
            p, n = on_ell(c, r, (0.45 * s, -0.25, 1))
            triangle_mark(k, p, n, 0.03, up=(0, -1, 0))
    for s in (1, -1):
        k.add("pauldron", ell(SHOULDER * (s, 1, 1), (0.1, 0.1, 0.085), deform=boxy(2.4)), PLATE, "body")
        p, n = on_ell(SHOULDER * (s, 1, 1), (0.1, 0.1, 0.085), (0.6 * s, -0.5, 0.6))
        triangle_mark(k, p, n, 0.028)
    # horns up and back off the arch, and out at its sides
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1])
        horn(k, f"hornA{side}", [np.array(p) * m for p in ((0.08, 0.0, 0.7), (0.1, 0.07, 0.78), (0.12, 0.16, 0.83),
                                                          (0.13, 0.23, 0.84))], [0.035, 0.028, 0.018, 0.0])
        horn(k, f"hornB{side}", [np.array(p) * m for p in ((0.18, 0.06, 0.6), (0.25, 0.1, 0.66), (0.29, 0.17, 0.7),
                                                          (0.3, 0.23, 0.7))], [0.032, 0.025, 0.016, 0.0])
        horn(k, f"hornC{side}", [np.array(p) * m for p in ((0.12, 0.16, 0.52), (0.16, 0.25, 0.5), (0.18, 0.31, 0.46))],
             [0.03, 0.02, 0.0])


def body(k):
    k.add("torso", ell((0, -0.03, 0.44), (0.135, 0.13, 0.15)), MAUVE, "body")
    k.add("belly", ell((0, -0.02, 0.3), (0.125, 0.115, 0.1)), MAUVE, "body")
    k.add("chest", ell((0, -0.12, 0.47), (0.1, 0.06, 0.1)), MAUVE, "body")
    # segments down the front
    k.paint(lambda P: inside(P[:, 1] + 0.1, 0.01) * sum(inside(np.abs(P[:, 2] - z) - 0.005, 0.003)
                                                       for z in (0.34, 0.4, 0.46)), MAUVE_DK, on=["torso", "belly", "chest"])


def head(k):
    C, R = HD_C, HD_R
    k.add("head", ell(C, R), FACE, "head")
    k.add("helmet", ell(C + (0, 0.015, 0.035), R * (1.06, 1.0, 0.82), deform=boxy(2.4)), PLATE, "head")
    k.add("jaw", ell(C + (0, -0.025, -0.075), (0.055, 0.05, 0.035)), FACE, "head")
    # the dark face: the front of the head below the helmet's brow, cut as a V by the helmet
    def face(P):
        x, y, z = P[:, 0] - C[0], P[:, 1] - C[1], P[:, 2] - C[2]
        return inside(y + 0.035, 0.003) * inside(z - 0.03 + 0.35 * np.abs(x), 0.003) * inside(-0.06 - z, 0.003) * \
            inside(np.abs(x) - 0.072, 0.003)
    k.paint(face, FACE, on=["helmet", "head", "jaw"])


def purple_x(k):
    """The purple X over the face, its lower arms splaying out as mandibles: two loose tubes laid on the face."""
    C, R = HD_C, HD_R
    for s in (1, -1):
        dirs = [(0.85 * s, -0.35, 1.1), (0.62 * s, -1, 0.75), (0.3 * s, -1, 0.36), (0.0, -1, 0.0), (-0.3 * s, -1, -0.36), (-0.6 * s, -1, -0.72),
                (-0.9 * s, -0.6, -0.95)]
        pts = []
        for d in dirs:
            p, n = on_ell(C, R * (1.0, 1.0, 1.0), d)
            pts.append(p + n * 0.006)
        pts.append(pts[-1] + np.array([-0.05 * s, -0.02, -0.03]))
        k.add(f"xline{'R' if s > 0 else 'L'}", outward(tube(pts, [0.009, 0.012, 0.013, 0.013, 0.013, 0.012, 0.011, 0.007])), PURPLE,
              "head", fuse=False, surface="shell")


def arms(k):
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1])
        sh, el, wr = SHOULDER * m, ELBOW * m, WRIST * m
        k.add(f"upperarm{side}", tube([sh, (sh + el) / 2 + (0.015 * s, 0, 0.01), el], [0.058, 0.055, 0.052]), MAUVE,
              f"arm{side}")
        k.add(f"forearm{side}", tube([el, wr], [0.05, 0.045]), MAUVE, f"arm{side}")
        # the gauntlet: four pale rings flaring toward the hand, a green triangle on each
        d = wr - el
        L = np.linalg.norm(d); d = d / L
        _, a, b = frame(d)
        for j in range(4):
            t0 = 0.05 + j * 0.24
            c = el + d * L * t0
            r0, r1 = 0.072 + 0.006 * j, 0.082 + 0.006 * j
            k.add(f"ring{side}{j}", lathe(c, d, [(0, 0), (r0, 0), (r1, L * 0.2), (r1 - 0.012, L * 0.255),
                                                  (0, L * 0.255)], seg=40), PLATE, f"arm{side}", fuse=False,
                  surface="shell")
            # a dark green triangle on the ring's outer top
            o = unit(*(b * 0.75 + a * 0.6 * s))
            tc = c + d * L * 0.11 + o * (r0 + r1) / 2 * 1.03
            nn = o
            e1 = d
            k.add(f"mark{side}{j}", slab([(-0.024, -0.02), (-0.024, 0.02), (0.026, 0.0)], tc, e1,
                                         np.cross(nn, e1), 0.006), MARK, f"arm{side}", fuse=False, surface="shell")
        # three black claws, forward and down, curving in
        for j, off in enumerate((-0.04, 0.0, 0.04)):
            base = wr + d * 0.03 + a * off * s
            tip1 = base + d * 0.07 + np.array([0, -0.03, -0.03]) + a * off * s * 0.5
            tip2 = base + d * 0.12 + np.array([0, -0.03, -0.09]) + a * off * s * 0.2
            horn(k, f"claw{side}{j}", [base, tip1, tip2], [0.022, 0.016, 0.0], colour=CLAW, bone=f"arm{side}")


def small_arms(k):
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1])
        pts = [np.array(p) * m for p in ((0.07, -0.13, 0.47), (0.11, -0.19, 0.4), (0.1, -0.23, 0.33))]
        k.add(f"smallarm{side}", outward(tube(pts, [0.02, 0.018, 0.016])), PLATE, "body", fuse=False, surface="shell")
        horn(k, f"smallclaw{side}", [pts[-1], pts[-1] + np.array([-0.01 * s, -0.03, -0.035]),
                                     pts[-1] + np.array([-0.02 * s, -0.035, -0.075])], [0.017, 0.012, 0.0], colour=CLAW)


def legs(k):
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1])
        k.add(f"hip{side}", ell((0.1, 0.02, 0.25) * m, (0.09, 0.09, 0.08)), MAUVE, f"leg{side}")
        k.add(f"thigh{side}", ell((0.14, -0.02, 0.18) * m, (0.085, 0.1, 0.09), deform=boxy(2.6)), PLATE, f"leg{side}")
        triangle_mark_pt = on_ell((0.14, -0.02, 0.18) * m, (0.085, 0.1, 0.09), (0.5 * s, -1, 0.2))
        triangle_mark(k, *triangle_mark_pt, 0.026)
        k.add(f"shin{side}", tube([(0.15, -0.04, 0.12) * m, (0.155, -0.04, 0.07) * m, (0.155, -0.035, 0.04) * m],
                                  [0.052, 0.046, 0.044]), PLATE, f"leg{side}")
        k.add(f"foot{side}", ell((0.155, -0.06, 0.03) * m, (0.055, 0.075, 0.03)), PLATE, f"leg{side}")
        for j, dx in enumerate((-0.025, 0.025)):
            b = np.array([0.155 + dx, -0.11, 0.025]) * m
            horn(k, f"toe{side}{j}", [b, b + (0.0, -0.035, -0.008), b + (0.0, -0.06, -0.022)], [0.018, 0.012, 0.0],
                 colour=CLAW, bone=f"leg{side}")


def tail(k):
    k.add("tail", tube([(0, 0.08, 0.3), (0, 0.16, 0.25), (0, 0.22, 0.2)], [0.08, 0.06, 0.04]), MAUVE, "tail")
    for i, (c, r) in enumerate((((0, 0.12, 0.31), (0.1, 0.06, 0.06)), ((0, 0.19, 0.26), (0.075, 0.05, 0.045)))):
        k.add(f"tailplate{i}", ell(c, r, rot=(-30, 0, 0), deform=boxy(2.6)), PLATE, "tail")


def glint_eye():
    CW = srgb(GLINT)
    CF = srgb(FACE)
    def eye(u, v):
        c = np.tile(CF, (len(u), 1))
        star = (np.abs(u) ** 0.6 + np.abs(v) ** 0.6) < 0.85
        c[star] = CW
        return c
    return eye


def golisopod(src, out):
    k = Kit(src, coat="shell")
    back(k)
    body(k)
    head(k)
    purple_x(k)
    arms(k)
    small_arms(k)
    legs(k)
    tail(k)
    k.build(voxel=0.006, smooth=4, band=0.005, wband=0.05)

    def lay(state):
        lk = eye_look(state, glint_eye(), srgb(FACE), srgb(GLINT), line=0.2)
        p, n = on_ell(HD_C, HD_R, (0.5, -1, 0.06))
        k.decal(p, n, (0.019, 0.019), lk, eps=0.002, rings=20, segs=56, mirror=True)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    golisopod(*args())
