# Wimpod sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/wimpod.py -- <the export's Wimpod.glb> <out.glb>
#
# From the references: a small woodlouse lying low, nose down. An oval shell of pale mint-grey plates over its back,
# dark brown-grey triangles showing at the seams between them. A purple face at the front: two big yellow eyes with a
# small dark pupil and a glint, each in a purple socket with a raised rim; under the face a purple jaw that sweeps back
# along each side to a hook, a row of small jagged teeth at its front. Two long thin purple feelers rise from the face
# and sweep back over the shell. Behind, a fan of three sharp purple spikes. Small purple legs under it.
# Written to be grown from (golisopod.py imports it): the shell's plates and seam triangles, the eye and the purple.
# Skeleton: body (0, 0, 0.08), head (0, -0.07, 0.075).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, unit, eye_look, outward, polygon, args

SHELL, SEAM, PURPLE, PURPLE_DK = "#D3E3DE", "#6E625E", "#9672AA", "#5C3E70"
YELLOW, PUPIL, WHITE = "#EED64A", "#3C3446", "#FAFAF6"


def on_ell(c, r, d):
    c, r, d = np.array(c, float), np.array(r, float), unit(*d)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def spike(k, name, root, d, length, w0, t0, colour=PURPLE, bone="body"):
    """A tapering blade with a kite section: a tail spike, a claw."""
    d = unit(*d)
    a = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a)
    b = np.cross(a, d)
    root = np.asarray(root, float)
    pts = []
    for s, f in ((0.0, 1.0), (0.45, 0.8), (0.8, 0.4)):
        c = root + d * length * s
        pts += [c + a * w0 * f, c - a * w0 * f, c + b * t0 * f, c - b * t0 * f]
    pts.append(root + d * length)
    k.add(name, hull(pts), colour, bone, fuse=False, surface="shell")


TRI = polygon([(-1, -0.8), (1, -0.8), (0, 1)])


def seam_triangles(k, c, r, spots, size, colour=SEAM):
    """Dark triangles at the seams of the shell's plates: (azimuth from the top toward +X, place along y) each."""
    for az, y in spots:
        a = math.radians(az)
        d = (math.sin(a), y, math.cos(a))
        p, n = on_ell(c, r, d)
        k.decal(p, n, (size, size), colour, up=(0, -1, 0), eps=0.0012, rings=8, segs=36, outline=TRI)


def ringed_yellow(white=YELLOW, rim=PURPLE_DK, pupil=PUPIL, glint=WHITE, rim_w=0.16, pr=0.26, px=0.0, pz=-0.05):
    CW, CR, CP, CG = srgb(white), srgb(rim), srgb(pupil), srgb(glint)
    def eye(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(CW, (len(u), 1))
        c[r > 1 - rim_w] = CR
        m = (u - px) ** 2 + (v - pz) ** 2
        c[m < pr * pr] = CP
        c[(u - px - 0.08) ** 2 + (v - pz - 0.1) ** 2 < (pr * 0.32) ** 2] = CG
        return c
    return eye


# --- the frame -------------------------------------------------------------------------------------------------------
BODY_C, BODY_R = np.array([0.0, 0.025, 0.066]), np.array([0.076, 0.108, 0.06])
FACE_C, FACE_R = np.array([0.0, -0.068, 0.058]), np.array([0.064, 0.042, 0.05])


def shell(k):
    def nose_down(V):
        V = V.copy()
        front = np.clip(-V[:, 1], 0, 1)
        V[:, 2] -= 0.12 * front ** 2
        return V
    k.add("body", ell(BODY_C, BODY_R, deform=nose_down), SHELL, "body")
    # overlapping plates down the back, each a little proud of the next
    for i, y in enumerate((-0.04, 0.0, 0.04, 0.08)):
        f = 1 - abs(i - 1.3) * 0.08
        k.add("plate", ell((0, y, 0.071 + 0.004 * (1 - abs(i - 1.5) / 1.5)), (0.077 * f, 0.03, 0.058 * f)), SHELL, "body")
    k.add("underside", ell((0, 0.02, 0.025), (0.06, 0.095, 0.022)), PURPLE_DK, "body")
    seam_triangles(k, BODY_C, BODY_R * 1.02, [(0, -0.2), (45, -0.18), (-45, -0.18), (70, 0.22), (-70, 0.22),
                                              (20, 0.25), (-20, 0.25), (85, -0.2), (-85, -0.2)], 0.012)


def face(k):
    k.add("face", ell(FACE_C, FACE_R), PURPLE, "head")
    for s in (1, -1):
        # the raised purple rim of each eye's socket
        p, n = on_ell(FACE_C, FACE_R, (0.55 * s, -0.8, 0.22))
        k.add("socket", ell(p - n * 0.006, (0.026, 0.026, 0.026)), PURPLE, "head")


def jaw(k):
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1])
        pts = [np.array(p) * m for p in ((0.0, -0.106, 0.024), (0.045, -0.1, 0.02), (0.072, -0.065, 0.024),
                                          (0.082, -0.02, 0.04), (0.08, 0.02, 0.072))]
        k.add(f"jaw{side}", outward(blade(pts, [0.018, 0.02, 0.019, 0.014, 0.004], (1, 0, 0), 0.42)), PURPLE, "head",
              fuse=False, surface="shell")
        for j, x in enumerate((0.012, 0.034)):
            b = np.array([x * s, -0.112, 0.014])
            spike(k, f"tooth{side}{j}", b, (0.1 * s, -0.4, -1), 0.016, 0.006, 0.004, colour=PURPLE, bone="head")


def feelers(k, top=0.18):
    for s, side in ((1, "R"), (-1, "L")):
        pts = [(0.02 * s, -0.098, 0.088), (0.035 * s, -0.1, 0.13), (0.05 * s, -0.07, top - 0.025),
               (0.062 * s, -0.02, top), (0.07 * s, 0.035, top - 0.01)]
        k.add(f"feeler{side}", outward(tube(pts, [0.007, 0.0062, 0.0055, 0.005, 0.0045])), PURPLE, "head", fuse=False,
              surface="shell")


def tail(k):
    k.add("tailbase", ell((0, 0.125, 0.07), (0.03, 0.025, 0.025)), PURPLE, "body")
    for name, d, L in (("spikeC", (0, 1, 0.55), 0.085), ("spikeR", (0.55, 1, 0.65), 0.07), ("spikeL", (-0.55, 1, 0.65), 0.07),
                       ("spikeR2", (0.75, 1, 0.15), 0.05), ("spikeL2", (-0.75, 1, 0.15), 0.05)):
        spike(k, name, (0, 0.125, 0.075), d, L, 0.012, 0.007)


def legs(k):
    for s, side in ((1, "R"), (-1, "L")):
        for j, y in enumerate((-0.03, 0.01, 0.05, 0.09)):
            k.add(f"leg{side}{j}", outward(tube([(0.04 * s, y, 0.03), (0.06 * s, y - 0.005, 0.016), (0.064 * s, y - 0.01, 0.004)],
                                                [0.007, 0.0065, 0.005])), PURPLE, "body", fuse=False, surface="shell")


def eyes(k, state, c=FACE_C, r=FACE_R, half=0.022):
    lk = eye_look(state, ringed_yellow(), srgb(PURPLE), srgb(PUPIL), line=0.2)
    p, n = on_ell(c, r, (0.55, -0.8, 0.22))
    p = p + n * 0.012
    k.decal(p, n, (half, half), lk, eps=0.0016, rings=20, segs=56, mirror=True)


def wimpod(src, out):
    k = Kit(src, coat="shell")
    shell(k)
    face(k)
    jaw(k)
    feelers(k)
    tail(k)
    legs(k)
    k.build(voxel=0.0022, smooth=4, band=0.002, wband=0.02)
    k.eye_states(lambda st: eyes(k, st))
    k.export(out)


if __name__ == "__main__":
    wimpod(*args())
