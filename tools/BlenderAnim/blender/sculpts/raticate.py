# Raticate sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/raticate.py -- <the export's Raticate.glb> <out.glb>
# Rattata grown (rattata.py, whose teeth, whiskers, ear linings and cut eyes it borrows): a heavy tawny rat sitting
# up, its round head run into a round body with no neck, a cream front from the chin down, a ragged tuft between big
# round ears lined pink, small black eyes under a scowling brow, great white front teeth over a wide-open mouth, three
# short whiskers a side, small clawed paws held up in front, big flat hind feet and a long ringed tail behind.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, ring_band, outward, sibling, args

r = sibling("rattata")

FUR, CREAM, TAN, LINING, NOSE = "#C68A44", "#F2DEA0", "#DBA47E", "#D99A84", "#5A3424"
RING = "#B98060"
H = np.array([0, -0.085, 0.5])           # the head's centre
HR = np.array([0.15, 0.14, 0.13])
EC = np.array([0.135, -0.03, 0.665])     # the ear's centre (+X)
ER = np.array([0.075, 0.022, 0.078])
EA = (0, 30, 24)


def spline(pts, per=12):
    p = np.array(pts, float)
    e = np.vstack([2 * p[0] - p[1], p, 2 * p[-1] - p[-2]])
    out_ = []
    for i in range(len(p) - 1):
        p0, p1, p2, p3 = e[i], e[i + 1], e[i + 2], e[i + 3]
        for j in range(per):
            t = j / per
            out_.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                               + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    out_.append(p[-1])
    return np.array(out_)


def raticate(src, out):
    k = Kit(src, coat="fur")

    # A round body sitting up, the head run into it with no neck
    def pear(V):
        V = V.copy(); s = 1 - 0.1 * V[:, 2]
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell((0, 0.0, 0.29), (0.205, 0.19, 0.235), deform=pear), FUR, "body")
    k.add("head", ell(H, HR), FUR, "head")
    k.add("cheek", ell((0.09, -0.15, 0.45), (0.075, 0.07, 0.065)), FUR, "head", mirror=True)
    # ragged fur at the cheeks, pointing back and out
    for i, (a, b, rr) in enumerate([((0.15, -0.1, 0.46), (0.215, -0.04, 0.44), 0.035),
                                    ((0.14, -0.08, 0.4), (0.205, -0.02, 0.37), 0.03)]):
        k.add(f"cheekfur{i}", blade([a, b], [rr, 0.004], (0, 0, 1), 0.55), FUR, "head", mirror=True)
    # The muzzle, lifted as it snarls, and the dropped lower jaw under the open mouth
    def snout(V):
        V = V.copy(); t = np.clip(-V[:, 1], 0, 1)
        V[:, 0] *= 1 - 0.22 * t; V[:, 2] *= 1 - 0.15 * t
        return V
    k.add("snout", ell((0, -0.2, 0.495), (0.085, 0.09, 0.07), rot=(-12, 0, 0), deform=snout), FUR, "head")
    k.add("jaw", ell((0, -0.19, 0.385), (0.075, 0.07, 0.04), rot=(18, 0, 0)), CREAM, "head")

    # The ragged tuft between the ears, three points laid back
    for i, (x, h, y) in enumerate([(0.0, 0.09, 0.0), (-0.035, 0.07, 0.02), (0.035, 0.07, 0.02), (0.06, 0.05, 0.04)]):
        b0 = np.array([x, -0.07 + y, 0.6])
        k.add(f"tuft{i}", blade([b0, b0 + (x * 0.5, 0.035, h * 0.6), b0 + (x * 0.8, 0.08, h)], [0.03, 0.02, 0.004],
                                (1, 0, 0), 0.55), FUR, "head")

    # Big round ears tilted out, lined pink
    k.add("ear", ell(EC, ER, rot=EA), FUR, "ear{s}", mirror=True)
    k.add("earroot", ell((0.1, -0.03, 0.6), (0.04, 0.028, 0.045)), FUR, "ear{s}", mirror=True)
    r.ear_lining(k, EC, EA, (0.052, 0.054), -0.006, LINING)

    # Small arms with paws held up in front, three claws each
    k.add("arm", tube([(0.13, -0.08, 0.36), (0.155, -0.16, 0.33), (0.15, -0.22, 0.32)], [0.042, 0.036, 0.03]), FUR,
          "arm{s}", mirror=True)
    k.add("paw", ell((0.15, -0.24, 0.318), (0.034, 0.03, 0.03)), TAN, "arm{s}", mirror=True)
    for i, dx in enumerate((-0.02, 0.0, 0.02)):
        b0 = np.array([0.15 + dx, -0.255, 0.31])
        k.add(f"claw{i}", outward(blade([b0, b0 + (dx * 0.5, -0.02, -0.02), b0 + (dx * 0.8, -0.022, -0.045)],
                                        [0.011, 0.008, 0.002], (0, 1, 0), 0.6)), TAN, "arm{s}", fuse=False, mirror=True)
    # paint the paw's tan back crisply over the arm's end
    k.paint(lambda P: inside(-0.226 - P[:, 1], 0.003) * (P[:, 2] > 0.26) * (P[:, 2] < 0.37) * (np.abs(P[:, 0]) > 0.1),
            TAN, on=["arm", "paw"])

    # Heavy thighs and big flat feet, three long toes forward and one back
    k.add("thigh", ell((0.125, -0.02, 0.15), (0.09, 0.105, 0.1)), FUR, "leg{s}", mirror=True)
    k.add("foot", ell((0.14, -0.1, 0.028), (0.06, 0.1, 0.028)), TAN, "leg{s}", mirror=True)
    for i, ang in enumerate((-22, 0, 22)):
        d = np.array([math.sin(math.radians(ang)), -math.cos(math.radians(ang)), 0])
        b0 = np.array([0.14, -0.14, 0.024])
        k.add(f"toe{i}", tube([b0, b0 + d * 0.06 + (0, 0, 0.004), b0 + d * 0.1], [0.018, 0.016, 0.012]), TAN, "leg{s}",
              mirror=True)
        tip = b0 + d * 0.1
        k.add(f"toeclaw{i}", outward(blade([tip, tip + d * 0.02 + (0, 0, -0.006), tip + d * 0.032 + (0, 0, -0.014)],
                                           [0.009, 0.006, 0.0015], (0, 0, 1), 0.6)), "#F2E6D8", "leg{s}", fuse=False,
              mirror=True, surface="shell")
    k.paint(lambda P: inside(P[:, 2] - 0.05, 0.003) * (P[:, 2] < 0.1), TAN, on=["foot", "toe", "thigh"])

    # The cream front: from the chin down the chest and belly, broad, and the lower face and muzzle
    def front(P):
        a = np.abs(np.arctan2(P[:, 0], -(P[:, 1] - 0.02)))
        return inside(a - (0.95 - 0.5 * np.clip((P[:, 2] - 0.33) / 0.12, 0, 1)), 0.003) * (P[:, 2] > 0.06) * (P[:, 2] < 0.46)
    k.paint(front, CREAM, on=["body", "jaw", "head", "cheek"])
    def face(P):
        line = 0.47 - 0.3 * (P[:, 1] + 0.18) - 0.35 * np.clip(np.abs(P[:, 0]) - 0.05, 0, 1)
        return inside(P[:, 2] - line, 0.0025) * (P[:, 1] < -0.12) * (P[:, 2] > 0.33)
    k.paint(face, CREAM)

    # The long tail lying out behind and curving up to the right, tan with darker rings
    TP = [(0, 0.17, 0.13), (0.02, 0.3, 0.06), (0.1, 0.44, 0.05), (0.22, 0.52, 0.1), (0.3, 0.54, 0.22),
          (0.33, 0.52, 0.36)]
    TR = [0.034, 0.028, 0.024, 0.02, 0.017, 0.013]
    k.add("tail", tube(TP, TR, seg=16), TAN, "tail")
    L = spline(TP)
    R = np.interp(np.linspace(0, 1, len(L)), np.linspace(0, 1, len(TR)), TR)
    for i in range(10, len(L) - 4, 6):
        d = L[i + 1] - L[i - 1]
        k.add(f"ring{i}", ring_band(L[i], d, R[i] + 0.0015, 0.01, thick=0.004, nu=41), RING, "tail", fuse=False)

    # The nose, the great teeth, the whiskers
    k.add("nose", ell((0, -0.29, 0.52), (0.024, 0.016, 0.018)), NOSE, "head", fuse=False, surface="skin")
    r.teeth(k, (0, -0.268, 0.465), 0.036, 0.075)
    for i, d in enumerate([(1, 0.05, 0.4), (1, 0.15, 0.08), (1, 0.25, -0.25)]):
        r.whisker(k, (0.065, -0.24, 0.475), d, 0.1, 0.0075, CREAM, name=f"whisker{i}")

    # The wide-open mouth under the teeth
    CM, CT = srgb(r.MOUTH), srgb(r.TONGUE)
    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        c[(u / 0.72) ** 2 + ((v + 0.65) / 0.55) ** 2 < 1] = CT
        return c
    k.decal((0, -0.245, 0.41), unit(0, -1, -0.15), (0.05, 0.04), mouth, eps=0.003, rings=12, segs=48)

    # Small black eyes with a glint, cut along the top by the brow slanting down toward the nose
    CW, CF = srgb(r.WHITE), srgb(FUR)
    look = r.red_eye(CW, srgb("#1C1410"), srgb("#1C1410"))
    de = unit(0.56, -0.78, 0.25)
    E = H + de / np.linalg.norm(de / HR)
    top = lambda u: 0.2 + 0.4 * u
    ftop = lambda u: -0.02 + 0.5 * u
    def eyes(state):
        r.lay_eyes(k, state, E, de, (0.034, 0.03), look, top, ftop, CF, srgb(r.INK))

    k.build(voxel=0.0075)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    raticate(*args())
