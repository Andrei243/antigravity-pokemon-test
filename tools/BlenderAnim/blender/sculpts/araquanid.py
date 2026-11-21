# Araquanid sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/araquanid.py -- <the export's Araquanid.glb> <out.glb>
# Grown from dewpider.py (sibling("dewpider")): the same opaque pale bubble with a window at its front through which
# the blue head shows, the same dark band round the head, the same lime legs and pale glints; Dewpider grown up.
#
# From the references: a big round dome of water, wider than tall, worn over the front like a diving bell, the head
# inside it: blue, lighter lobes over its crown (the front pair are its eyes, pale blue with a dark slit), a dark navy
# band round its middle with pale blue swirls on it, a grey point at the front of the band and two small dark fangs
# under it. Behind the dome a dark navy round abdomen with a brown star on its top. Six long thick legs: each rises up
# and out from the body to a knee held in a ball of water above the dome, then falls to the ground, lime down to a
# lime knuckle and dark navy below it to a point.
# Skeleton: body (0, 0, 0.18), head (0, 0, 0.216), legs at (+-0.036, 0.018 / 0.072 / 0.126, 0.162) on leg, leg.001,
# hind.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, hull, srgb, inside, unit, eye_look, outward, polygon, sibling, args

D = sibling("dewpider")
LIME, WATER, GLINT = D.LIME, D.WATER, D.GLINT
BLUE, BLUE_LO, BAND, SWIRL = "#5AB4DA", "#3E86B2", "#26445E", "#9ED8EE"
NAVY, STAR, POINT, FANG = "#22303E", "#8E6C4C", "#8C949C", "#1E2A36"

# --- the frame -------------------------------------------------------------------------------------------------------
DOME_C, DOME_R, DOME_S = np.array([0.0, -0.035, 0.2]), 0.1, np.array([1.28, 1.05, 0.98])
WINDOW = unit(0, -1, 0.18)
HD_C, HD_R = np.array([0.0, -0.045, 0.19]), np.array([0.104, 0.074, 0.066])
AB_C, AB_R = np.array([0.0, 0.085, 0.255]), np.array([0.07, 0.075, 0.06])
LEGS = [  # bone, root, knee, foot (on +X)
    ("leg{s}", (0.04, 0.0, 0.25), (0.15, -0.058, 0.34), (0.172, -0.09, 0.004)),
    ("leg{s}.001", (0.045, 0.04, 0.255), (0.175, 0.035, 0.355), (0.195, 0.03, 0.004)),
    ("hind{s}", (0.04, 0.08, 0.25), (0.15, 0.12, 0.34), (0.168, 0.155, 0.004)),
]
BALL_R = 0.042


def dome(k, c=DOME_C, R=DOME_R, scale=DOME_S, axis=WINDOW, open_deg=56, thick=0.006, colour=WATER):
    """Dewpider's bubble, stretched wide: a shell of turning with its window, scaled about its centre."""
    th = math.radians(open_deg)
    outer = [(R * math.sin(p), R * math.cos(p)) for p in np.linspace(math.pi, th, 40)]
    ri = R - thick
    inner = [(ri * math.sin(p), ri * math.cos(p)) for p in np.linspace(th, math.pi, 40)]
    prof = [(0.0, -R)] + outer[1:] + inner[:-1] + [(0.0, -ri)]
    V, F = lathe((0, 0, 0), axis, prof, seg=80)
    k.add("bubble", outward((V * scale + c, F)), colour, "head", fuse=False, surface="shell")
    for el, az, a in ((58, -66, 0.22), (44, -80, 0.18), (30, -90, 0.13)):
        d = np.array([math.sin(math.radians(az)) * math.cos(math.radians(el)),
                      -math.cos(math.radians(az)) * math.cos(math.radians(el)), math.sin(math.radians(el))])
        p = c + d * R * scale
        n = d / scale; n /= np.linalg.norm(n)
        k.add("glint", D.lens(p + n * 0.001, n, R * a, R * 0.05, R * 0.018), GLINT, "head", fuse=False,
              surface="shell")


def head(k):
    C, RR = HD_C, HD_R
    k.add("head", ell(C, RR), BLUE, "head")
    # lobes over the crown: the front pair (the eyes) and a pair behind
    for s in (1, -1):
        for x, y, z, r in ((0.4, -0.42, 0.62, 0.36), (0.62, 0.18, 0.6, 0.3)):
            k.add("lobe", ell(C + RR * np.array([x * s, y, z]), RR * r * np.array([1, 0.9, 0.8])), BLUE, "head")
    zc = C[2] - 0.08 * RR[2]
    h = 0.3 * RR[2]
    k.paint(lambda P: inside(P[:, 2] - (zc - h) + 0.002, 0.0015), BLUE_LO, on=["head"])
    k.paint(lambda P: inside(np.abs(P[:, 2] - zc) - h, 0.0015), BAND, on=["head"])
    # pale swirls on the band's sides: a curl of patches either side
    for s in (1, -1):
        for az, dz, w in ((0.9, 0.0, 0.034), (0.55, -0.1, 0.026)):
            p, n = D.on_ell(C, RR, (az * s, -0.55, (zc - C[2]) / RR[2] + dz))
            k.decal(p, n, (w, 0.0085), SWIRL, eps=0.0016, rings=8, segs=40)
    # the grey point at the band's front, and two fangs under it
    p, n = D.on_ell(C, RR, (0, -1, (zc - C[2]) / RR[2] + 0.1))
    k.decal(p, n, (0.018, 0.026), POINT, eps=0.0018, rings=10, segs=40,
            outline=polygon([(-1, 0.6), (1, 0.6), (0, -1)]))
    for s in (1, -1):
        b = C + np.array([0.035 * s, -RR[1] * 0.78, -RR[2] * 0.62])
        pts = [b + (0.009, 0, 0.004), b + (-0.009, 0, 0.004), b + (0, -0.008, 0.004), b + (0, 0.008, 0.004),
               b + (-0.004 * s, -0.006, -0.03)]
        k.add("fang", hull(pts), FANG, "head", fuse=False, surface="shell")


def abdomen(k):
    k.add("abdomen", ell(AB_C, AB_R), NAVY, "body")
    k.add("waist", ell((0, 0.035, 0.235), (0.05, 0.05, 0.045)), NAVY, "body")
    star = [(math.cos(a) * r, math.sin(a) * r) for i in range(10)
            for a, r in ((math.pi / 2 + i * math.pi / 5, 1.0 if i % 2 == 0 else 0.45),)]
    p, n = D.on_ell(AB_C, AB_R, (0, 0.25, 1))
    k.decal(p, n, (0.042, 0.042), STAR, up=(0, -1, 0), eps=0.002, rings=12, segs=60, outline=polygon(star))


def legs(k):
    for bone, root, knee, foot in LEGS:
        root, knee, foot = np.array(root), np.array(knee), np.array(foot)
        nm = "mid" if "001" in bone else bone[:4].strip("{")
        k.add(f"{nm}up", outward(tube([root, (root + knee) / 2 + (0, 0, 0.03), knee], [0.012, 0.012, 0.011])), LIME,
              bone, fuse=False, mirror=True, surface="shell")
        k.add(f"{nm}ball", outward(ell(knee, (BALL_R, BALL_R, BALL_R), sub=4)), WATER, bone, fuse=False, mirror=True,
              surface="shell")
        g = knee + unit(-0.4, -0.6, 0.7) * (BALL_R + 0.0006)
        k.add(f"{nm}glint", D.lens(g, unit(-0.4, -0.6, 0.7), 0.012, 0.007, 0.0025), GLINT, bone, fuse=False,
              mirror=True, surface="shell")
        lim = knee + (foot - knee) * 0.5
        k.add(f"{nm}shin", outward(tube([knee, (knee + lim) / 2 + (0.006, 0, 0), lim], [0.012, 0.0125, 0.012])), LIME,
              bone, fuse=False, mirror=True, surface="shell")
        # the lime knuckle, a diamond, and the navy point below it
        d = unit(*(foot - knee))
        a, b = np.cross(d, (0, 0, 1.0)), None
        a /= np.linalg.norm(a); b = np.cross(a, d)
        kn = [lim + a * 0.021, lim - a * 0.021, lim + b * 0.017, lim - b * 0.017, lim - d * 0.035, lim + d * 0.03]
        k.add(f"{nm}knuckle", hull(kn), LIME, bone, fuse=False, mirror=True, surface="shell")
        L = np.linalg.norm(foot - lim)
        k.add(f"{nm}point", lathe(lim + d * 0.02, d, [(0, -0.004), (0.0125, 0.0), (0.011, L * 0.45), (0.006, L * 0.85),
                                                       (0, L - 0.02)]), NAVY, bone, fuse=False, mirror=True,
              surface="shell")


def araquanid(src, out):
    k = Kit(src, coat="shell")
    head(k)
    abdomen(k)
    dome(k)
    legs(k)
    k.build(voxel=0.0036, smooth=4, band=0.003, wband=0.03)

    lid = srgb(BLUE)
    def lay(state):
        lk = eye_look(state, D.almond_eye(white="#B4E6F6", pupil=BAND, rim=BAND, rim_w=0.12), lid, srgb(BAND), line=0.2)
        p, n = D.on_ell(HD_C + HD_R * np.array([0.4, -0.42, 0.62]), HD_R * 0.36 * np.array([1, 0.9, 0.8]),
                        (0.15, -1.0, 0.35))
        k.decal(p, n, (0.021, 0.017), lk, eps=0.0018, rings=20, segs=56, mirror=True)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    araquanid(*args())
