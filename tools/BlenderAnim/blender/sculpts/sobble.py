# Sobble sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sobble.py -- <the export's Sobble.glb> <out.glb>
#
# From the references: a small pale blue water lizard sat on its folded legs, its head the greatest part of it, round
# and much wider than tall. A paler face whose top edge dips in a point between the eyes; great round eyes, white with
# a thick navy rim, a blue iris and a navy pupil with a glint; a dark blue spot low on each cheek; a wide open mouth,
# dark red above and a pink tongue below. On the crown a yellow fin on a short stalk, leaning to the +X side, notched
# on that side into a big upper lobe and a small hook, a blue stripe up its other edge. A small body with a paler belly,
# thin arms coming down to hold the knees, frog legs folded with the knees out, splayed toes; a thick tail curled into a
# ring behind, out to the +X side, with dark blue spots.
#
# Written to be grown from (Drizzile and Inteleon import it with sibling("sobble")): the line's colours, on_ell,
# the round eye and its states, the curled tail and the spots are the functions below.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, smoothstep, unit, eye_look, args

# --- the line's colours ---------------------------------------------------------------------------------------------
BLUE = "#86C6EC"            # Sobble's coat
PALE = "#C8E8F4"            # its face and belly
SPOT = "#2C66B4"            # cheek and tail spots, the fin's stripe
YELLOW = "#F6DC44"
MOUTH, TONGUE = "#9C2C3C", "#E88892"
NAVY, IRIS = srgb("#1E3068"), srgb("#3C7CCC")
INK, WHITE = srgb("#151A2A"), np.array([1.0, 1.0, 1.0])
VOXEL = 0.0042


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre."""
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r, float)) ** 2).sum())


def ell_normal(c, r, p):
    """The outward normal of an unturned ellipsoid at its point p."""
    n = (np.array(p, float) - np.array(c, float)) / np.array(r, float) ** 2
    return n / np.linalg.norm(n)


def round_eye(iris_r=0.62, iris_c=(-0.06, -0.02), pupil_r=None, rim=0.825, glint=None,
              white=WHITE, iris=IRIS, rimc=NAVY):
    """The open eye (u outward, v up): white, a thick dark rim, a round iris toward the inner side, a dark pupil and
    one glint on the iris."""
    def eye(u, v):
        rho = np.hypot(u, v)
        c = np.tile(white, (len(u), 1))
        ri = np.hypot(u - iris_c[0], v - iris_c[1])
        c[ri < iris_r] = iris
        if pupil_r is not None:
            c[ri < pupil_r] = rimc
        if glint is not None:
            c[np.hypot(u - glint[0], v - glint[1]) < glint[2]] = white
        c[rho >= rim] = rimc
        return c
    return eye


def curl(root, d, centre_back, centre_up, r0, r1, turns, tube_r0, tube_r1, n=40):
    """A tail curled into a ring in the plane of d (back) and up: straight back from root to the ring's foot, then
    round over the top and in. Its points and radii."""
    d = unit(*d)
    up = np.array([0, 0, 1.0])
    root = np.array(root, float)
    C = root + d * centre_back + up * centre_up
    pts, rad = [root], [tube_r0]
    for i in range(n):
        t = i / (n - 1)
        th = -math.pi / 2 + turns * 2 * math.pi * t
        R = r0 + (r1 - r0) * t
        pts.append(C + R * (math.cos(th) * d + math.sin(th) * up))
        rad.append(tube_r0 + (tube_r1 - tube_r0) * t)
    return np.array(pts), np.array(rad), C


def spots(k, centres, r, color, on):
    """Round spots painted on a part (each a ball's reach)."""
    for c in centres:
        c = np.array(c, float)
        k.paint(lambda P, c=c: inside(np.linalg.norm(P - c, axis=1) - r, 0.002), color, on=on)


def digits(k, name, base, d, spread, length, r, col, bone, n=3):
    """Short fingers or toes from base along d, fanned across spread, with round ends; on +X, mirrored."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    sp = np.array(spread, float)
    for i, f in enumerate(np.linspace(-1, 1, n)):
        a = np.array(base, float) + sp * f * 0.5
        e = a + (d + sp * f * 6) * length
        k.add(f"{name}{i}", tube([a, (a + e) / 2, e], [r, r * 0.95, r * 0.9]), col, bone, mirror=True)


# --- Sobble's frame -------------------------------------------------------------------------------------------------
H = np.array([0.0, -0.01, 0.222])            # the head: wide and round
HR = np.array([0.148, 0.118, 0.098])


def fin(k, base, lean, scale, wide=1.0, yellow=YELLOW, stripe=SPOT, bone="head"):
    """The fin on the crown: a yellow plate on a short stalk leaning to +X, notched on that side into a big upper lobe
    and a small hook, a blue stripe up its other edge. Faces the front, tilted back by lean."""
    ex = np.array([1.0, 0, 0])
    ez = unit(0, math.sin(lean), math.cos(lean))
    plate = [(-0.011, -0.02), (0.011, -0.02), (0.016, 0.01), (0.03, 0.022), (0.05, 0.026), (0.062, 0.034),
           (0.058, 0.044), (0.042, 0.05), (0.036, 0.058), (0.05, 0.07), (0.064, 0.09), (0.07, 0.11), (0.064, 0.13),
           (0.048, 0.144), (0.028, 0.15), (0.012, 0.146), (0.0, 0.132), (-0.008, 0.11), (-0.012, 0.08),
           (-0.013, 0.05), (-0.013, 0.02)]
    plate = [(a * scale * wide, b * scale) for a, b in plate]
    k.add("fin", slab(plate, base, ex, ez, 0.0135), yellow, bone, fuse=False)
    edge = [(-0.016, -0.02), (-0.004, -0.02), (-0.002, 0.02), (-0.001, 0.05), (0.002, 0.08), (0.008, 0.108),
            (0.016, 0.13), (0.026, 0.144), (0.014, 0.148), (0.0, 0.136), (-0.011, 0.112), (-0.016, 0.082),
            (-0.018, 0.05), (-0.018, 0.02)]
    edge = [(a * scale * wide, b * scale) for a, b in edge]
    k.add("finstripe", slab(edge, np.array(base, float) + np.array([-0.0005, 0, 0]), ex, ez, 0.0175), stripe, bone,
          fuse=False)


def pale_face(P):
    """The paler face: from the cheeks in, its top edge dipping in a point between the eyes, down to the mouth."""
    x, y, z = P[:, 0], P[:, 1], P[:, 2]
    a = np.arctan2(np.abs(x), -(y - H[1]))
    line = H[2] - 0.006 + 0.052 * np.clip(a / 0.85, 0, 1) ** 0.85
    head = inside(np.sqrt((x / (HR[0] + 0.02)) ** 2 + ((y - H[1]) / (HR[1] + 0.02)) ** 2
                          + ((z - H[2]) / (HR[2] + 0.02)) ** 2) - 1, 0.004)
    return (inside(z - line, 0.0025) * inside(a - 1.02, 0.03) * inside(H[2] - 0.07 - z, 0.004) * head
            * inside(y - H[1], 0.01))


def sobble(src, out):
    k = Kit(src, coat="scales")

    # --- the head: wide, round, the face a little flattened; the fin on its crown ----------------------------------
    def face(V):
        V = V.copy()
        front = np.clip(-V[:, 1], 0, 1)
        V[:, 1] *= 1 - 0.1 * front ** 2
        low = np.clip(-V[:, 2], 0, 1)
        V[:, 0] *= 1 + 0.04 * low
        return V
    k.add("head", ell(H, HR, deform=face), BLUE, "head")
    k.add("jowl", ell(H + (0, -0.02, -0.05), (0.12, 0.09, 0.05)), BLUE, "head")
    fin(k, H + (-0.008, 0.02, HR[2] - 0.012), math.radians(24), 1.15, 1.3)

    # --- the body: small, sat low under the head, a paler belly -----------------------------------------------------
    k.add("torso", ell((0, 0.0, 0.085), (0.058, 0.052, 0.062)), BLUE, "body")
    k.paint(lambda P: inside(np.sqrt((P[:, 0] / 0.04) ** 2 + ((P[:, 2] - 0.085) / 0.05) ** 2) - 1, 0.003)
            * inside(P[:, 1] + 0.02, 0.004) * inside(P[:, 2] - 0.13, 0.004), PALE, on=["torso"])

    # --- legs: frog legs folded with the knees out and up, short shins, splayed toes ------------------------------
    k.add("thigh", tube([(0.036, 0.0, 0.055), (0.062, -0.028, 0.07), (0.078, -0.05, 0.068)], [0.026, 0.02, 0.017]),
          BLUE, "leg{s}", mirror=True)
    k.add("shin", tube([(0.078, -0.05, 0.068), (0.08, -0.056, 0.04), (0.076, -0.058, 0.015)], [0.016, 0.0145, 0.014]),
          BLUE, "leg{s}", mirror=True)
    k.add("foot", ell((0.078, -0.07, 0.009), (0.02, 0.022, 0.009)), BLUE, "leg{s}", mirror=True)
    digits(k, "toe", (0.08, -0.084, 0.008), (0.25, -1, 0), (0.006, 0, 0), 0.016, 0.0072, BLUE, "leg{s}")

    # --- arms: thin, down from the shoulders to hold the knees in front ------------------------------------------
    k.add("arm", tube([(0.045, -0.01, 0.125), (0.062, -0.035, 0.1), (0.066, -0.058, 0.082)], [0.0135, 0.012, 0.0115]),
          BLUE, "arm{s}", mirror=True)
    k.add("hand", ell((0.066, -0.064, 0.077), (0.015, 0.014, 0.012)), BLUE, "arm{s}", mirror=True)

    # --- the tail: thick, back and out to +X, curled up into a ring, dark spots on it ------------------------------
    pts, rad, C = curl((0.0, 0.04, 0.07), (0.55, 0.83, 0), 0.1, 0.062, 0.062, 0.02, 1.3, 0.041, 0.026)
    k.add("tail", tube(pts, rad, seg=20, per=4), BLUE, "tail")
    d = unit(0.55, 0.83, 0)
    side = np.cross(d, (0, 0, 1.0))                 # the ring's face, toward +X and the front
    spots(k, [C + d * 0.058 + side * 0.022, C - d * 0.01 + (0, 0, 0.058) + side * 0.02,
              C + d * 0.03 - (0, 0, 0.05) + side * 0.024, C + d * 0.06 - side * 0.02], 0.02, SPOT, ["tail"])

    # --- the face: paler middle, cheek spots, the open mouth -----------------------------------------------------
    k.paint(pale_face, PALE)
    for s in (1, -1):
        dc = (0.93 * s, -0.36, -0.33)
        p = on_ell(H, HR, dc)
        k.decal(p, ell_normal(H, HR, p), (0.024, 0.024), SPOT, eps=0.0018, rings=10, segs=40)
    mc = on_ell(H, HR, (0, -1, -0.52))
    cm, ct = srgb(MOUTH), srgb(TONGUE)

    def mouth(u, v):
        c = np.tile(cm, (len(u), 1))
        c[v < -0.1 + 0.25 * u * u] = ct
        return c

    def lips(th):
        return 1.0 if math.sin(th) < 0 else 1.0 - 0.45 * math.sin(th) ** 2
    k.decal(mc + (0, 0, 0.004), unit(0, -1, -0.35), (0.078, 0.032), mouth, eps=0.002, rings=22, segs=72,
            outline=lips)

    # --- the eyes: great round ones, white with a navy rim, a blue iris -------------------------------------------
    de = unit(0.66, -1, 0.1)
    E = on_ell(H, HR, de)
    En = ell_normal(H, HR, E)
    look = round_eye()
    lid = srgb(PALE)

    half = (0.037, 0.044)
    right = np.cross((0, 0, 1.0), En); right /= np.linalg.norm(right)       # u: outward on the +X eye
    upv = np.cross(En, right)

    def at(u, v):
        return E + right * u * half[0] + upv * v * half[1]

    def lay(state):
        k.decal(E, En, half, eye_look(state, look, lid, NAVY, line=0.15), eps=0.0018, rings=20, segs=56,
                mirror=True)
        if state in ("Open", "Fierce"):
            # the pupil and its glint, patches of their own so their edges are round
            pv, pr = (-0.04, 0.3) if state == "Open" else (-0.12, 0.26)
            k.decal(at(-0.06, pv), En, (pr * half[0], pr * half[1]), NAVY, eps=0.0026, rings=8, segs=40, mirror=True)
            k.decal(at(-0.2, 0.22 if state == "Open" else 0.02), En, (0.13 * half[0], 0.13 * half[1]), WHITE,
                    eps=0.0032, rings=5, segs=28, mirror=True)

    k.build(voxel=VOXEL, smooth=4, band=0.005, wband=0.03)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    sobble(*args())
