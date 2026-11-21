# Tarountula sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tarountula.py -- <the export's Tarountula.glb> <out.glb>
#
# The design, from the references: a small yellow spider carrying a great white ball of thread on its back, the
# ball the largest mass (some 0.62 of the height across), wound with grey threads in crossing bundles, a few loose
# strands standing off it (a curl over the top, one out at each side). A round yellow head low in front of the ball,
# half buried in it, a small white loop of thread on its crown. Two big white eyes close together, a navy pupil in
# each one's lower half (a sleepy look); under them a white scalloped band across the face (its fangs); a yellow lobe
# flaring out and down at each side of the lower face. Eight legs, four a side, splaying out from under the head:
# yellow above, green below the knee's fall, darker green at the tips.
#
# Written to be grown from (spidops.py imports it): the colours, on_ell, a banded leg and the eye are functions and
# constants; the build is under __main__.
# Skeleton: body (0, 0.03, 0.18), head (0, -0.06, 0.12), legs (+-0.04, -0.09 / -0.05, 0.11) on leg{s} and leg{s}.001,
# hind{s} (+-0.04, -0.01, 0.11).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args

YELLOW, YELLOW_D, GREEN, GREEN_D = "#F2DE38", "#D8C22A", "#5FA83A", "#3C7A26"
WHITE, THREAD, THREAD_D, NAVY, INK = "#F4F4F2", "#C9CBD2", "#A9ACB6", "#24304E", "#18181C"

BALL_C, BALL_R = np.array([0.0, 0.05, 0.235]), np.array([0.136, 0.134, 0.13])
HEAD_C, HEAD_R = np.array([0.0, -0.1, 0.122]), np.array([0.076, 0.06, 0.062])
VOXEL = 0.0038


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r) in direction d from its centre, and its outward normal."""
    c, r, d = np.array(c, float), np.array(r, float), unit(*d)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def banded_leg(k, name, pts, radii, bone, low, tip, top=YELLOW, mid=GREEN, end=GREEN_D, mirror=True):
    """A leg as one fused tube, yellow above, green below the height low, darker green below tip (paints)."""
    k.add(name, tube(pts, radii), top, bone, mirror=mirror)
    k.paint(lambda P, low=low: inside(P[:, 2] - low, 0.002), mid, on=[name])
    k.paint(lambda P, tip=tip: inside(P[:, 2] - tip, 0.002), end, on=[name])


def eye_open(white=WHITE, pupil=NAVY, px=-0.1, py=-0.3, pr=(0.62, 0.5)):
    """A white oval with a dark oval pupil low and toward the middle (a drowsy look), a white glint on the pupil."""
    W, K = srgb(white), srgb(pupil)
    def eye(u, v):
        c = np.tile(W, (len(u), 1))
        c[((u - px) / pr[0]) ** 2 + ((v - py) / pr[1]) ** 2 < 1] = K
        c[((u - px + 0.2) / 0.14) ** 2 + ((v - py - 0.16) / 0.14) ** 2 < 1] = W
        return c
    return eye


def lay_eyes(k, state, c, r, d, half, white=WHITE, pupil=NAVY, lid=YELLOW, ink=INK, px=-0.12, py=-0.28,
             pr=(0.56, 0.5), eps=0.0015):
    """The eyes for a state: a white oval patch (eye_look on plain white, so the lid's lines and Fierce's cut are
    eye_look's), and in the Open and Fierce states a pupil as a patch of its own over it (crisp at any ring count),
    with a white glint as a third patch."""
    plain = lambda u, v: np.tile(srgb(white), (len(u), 1))
    p, n = on_ell(c, r, d)
    k.decal(p, n, half, eye_look(state, plain, srgb(lid), srgb(ink), line=0.16), up=(0, 0, 1), eps=eps,
            rings=20, segs=56, mirror=True)
    if state not in ("Open", "Fierce"):
        return
    a, b = half
    q = p + (np.cross((0, 0, 1.0), n) / np.linalg.norm(np.cross((0, 0, 1.0), n))) * px * a + np.array([0, 0, py * b])
    # the pupil's centre toward the middle on both sides: lay each side by hand
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        k.decal(q * m, n * m, (pr[0] * a, pr[1] * b), pupil, eps=eps * 2, rings=10, segs=48)
        g = q * m + np.array([-0.2 * a * s, 0, 0.18 * b])
        k.decal(g, n * m, (0.15 * a, 0.15 * a), white, eps=eps * 3, rings=6, segs=32)


# --- Tarountula ----------------------------------------------------------------------------------------------------
LEGS = [  # bone, hip (under the head), knee, ankle, foot (on +X)
    ("leg{s}", (0.04, -0.13, 0.095), (0.098, -0.165, 0.13), (0.125, -0.19, 0.06), (0.13, -0.2, 0.009)),
    ("leg{s}.001", (0.048, -0.1, 0.09), (0.112, -0.115, 0.135), (0.142, -0.125, 0.06), (0.148, -0.13, 0.009)),
    ("hind{s}", (0.05, -0.065, 0.09), (0.115, -0.06, 0.135), (0.145, -0.055, 0.06), (0.15, -0.05, 0.009)),
    ("hind{s}", (0.045, -0.035, 0.09), (0.105, -0.01, 0.13), (0.132, 0.015, 0.06), (0.136, 0.025, 0.009)),
]
EYE_D, EYE_HALF = (0.56, -1, 0.3), (0.022, 0.021)


def tarountula(src, out):
    k = Kit(src, coat="shell")
    # ---- the ball of thread, its threads in crossing bundles
    k.add("ball", ell(BALL_C, BALL_R, sub=5), WHITE, "body", surface="cloth")
    bundles = [((0.3, -0.9, 0.3), (0.9, 0.3, 0.2)), ((-0.7, 0.2, 0.7), (0.2, -0.5, 0.8)),
               ((0.8, 0.5, -0.3), (-0.3, 0.9, 0.3)), ((0.1, 0.8, 0.6), (0.0, 0.5, -0.8)),
               ((-0.6, -0.7, -0.2), (-0.8, 0.3, -0.5)), ((0.6, 0.1, 0.8), (0.3, -0.9, 0.2)), ((0.9, -0.3, 0.3), (-0.7, -0.5, 0.4))]
    def threads(P):
        q = P - BALL_C
        m = np.zeros(len(P))
        for n, keep in bundles:
            n, keep = unit(*n), unit(*keep)
            d = q @ n
            region = inside(-(q @ keep) + 0.05, 0.012)
            for off in (-0.03, -0.015, 0.0, 0.015, 0.03):
                m = np.maximum(m, inside(np.abs(d - off) - 0.0026, 0.0012) * region)
        return m
    k.paint(threads, THREAD_D, on=["ball"])
    # ---- loose strands: a curl over the top, one out at each side, sinking down
    curl = [(0.0, 0.06, 0.355), (0.012, 0.08, 0.385), (0.03, 0.1, 0.405), (0.052, 0.1, 0.41), (0.064, 0.085, 0.398),
            (0.058, 0.068, 0.385), (0.042, 0.07, 0.382)]
    k.add("curl", tube(curl, [0.0072] * len(curl)), WHITE, "body", fuse=False, surface="cloth")
    for name, pts in (("strandR", [(0.12, 0.1, 0.2), (0.15, 0.115, 0.19), (0.172, 0.125, 0.165), (0.178, 0.13, 0.14)]),
                      ("strandL", [(-0.115, 0.02, 0.17), (-0.145, 0.0, 0.16), (-0.168, -0.01, 0.142),
                                   (-0.172, -0.012, 0.122)])):
        k.add(name, tube(pts, [0.0072] * len(pts)), WHITE, "body", fuse=False, surface="cloth")
    # ---- the head, low in front, half buried in the ball; its side lobes; a small loop of thread on its crown
    k.add("head", ell(HEAD_C, HEAD_R, sub=5), YELLOW, "head")
    k.add("face", ell(HEAD_C + np.array([0, -0.022, -0.004]), (0.064, 0.044, 0.05), sub=4), YELLOW, "head")
    k.add("lobe", ell((0.058, -0.138, 0.084), (0.027, 0.022, 0.022), rot=(0, 30, 25)), YELLOW, "head", mirror=True)
    k.add("underneath", ell((0, -0.07, 0.1), (0.055, 0.06, 0.035)), YELLOW, "head")
    loop = [(-0.01, -0.105, 0.172)] + [(0.019 * math.sin(t), -0.108, 0.203 + 0.019 * math.cos(t))
                                      for t in np.linspace(-2.5, 2.5, 11)] + [(0.01, -0.105, 0.172)]
    k.add("loop", tube(loop, [0.0068] * len(loop)), WHITE, "head", fuse=False, surface="cloth")
    # ---- eight legs, four a side, splaying out from under the head
    for i, (bone, hip, knee, ankle, ft) in enumerate(LEGS):
        banded_leg(k, f"leg{i}", [hip, knee, ankle, ft], [0.0105, 0.0098, 0.0094, 0.0088], bone, 0.07, 0.03)
    # ---- the white scalloped band under the eyes (in every state)
    p, n = on_ell(HEAD_C + np.array([0, -0.022, -0.004]), (0.064, 0.044, 0.05), (0, -1, -0.38))
    def band(th):
        if math.sin(th) >= 0:
            return 1.0
        return 0.78 + 0.22 * abs(math.cos(2.5 * (th + math.pi / 2)))
    k.decal(p, n, (0.05, 0.016), WHITE, eps=0.0015, outline=band, rings=12, segs=64)
    k.build(voxel=VOXEL, smooth=3, band=0.005, wband=0.025)

    k.eye_states(lambda state: lay_eyes(k, state, HEAD_C + np.array([0, -0.022, -0.004]), (0.064, 0.044, 0.05),
                                        EYE_D, EYE_HALF))
    k.export(out)


if __name__ == "__main__":
    tarountula(*args())
