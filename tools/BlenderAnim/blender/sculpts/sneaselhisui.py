# Sneasel-Hisui sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sneaselhisui.py -- <the export's Sneasel-Hisui.glb> <out.glb>
# Sneasel's Hisuian form, from the pictures: the same cat-headed weasel standing upright, its coat pale grey with a
# lilac cast and its markings a deep violet: a wavy band over the shoulders and upper chest with the yellow gem in
# it, a mask round the muzzle and a streak up the brow with the other gem, and the forearms violet from a wavy cuff
# at the elbow to big violet hands with three long hooked claws, carried forward and down. One ear is a tall lilac
# blade standing up on the +X side (the viewer's right from the front), the other a short grey point; three lilac
# blades fan up and back from the tail. Red eyes under a dark lid with a flick of lash, a sly mouth with a fang.
# sneasel.py builds as it is read, so its head, eyes and mouth are copied here and changed where the pictures change
# them; its thin arms held out in a T and its thin legs are not followed.
# The export's joints: body (0, 0, 0.4), tail (0, 0.05, 0.36), arms (+-0.07, -0.02, 0.5), head (0, -0.02, 0.52),
# ears (+-0.06, 0, 0.67), legs (+-0.05, 0.01, 0.32).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, smoothstep, unit, eye_look, args

GREY, VIOLET, LILAC, CLAW, PALE = "#BDBACF", "#5E4A9E", "#9A86DE", "#4A3A84", "#E2E0EA"
GOLD, INK = "#F2C232", "#12141E"
H = np.array([0, -0.035, 0.655])           # the head's centre
HR = np.array([0.142, 0.12, 0.13])


def jowl(V):
    V = V.copy()
    low = np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.22 * low ** 1.4
    V[:, 1] *= 1 - 0.08 * low ** 1.4
    return V


def sneasel_hisui(src, out):
    k = Kit(src, coat="fur")

    # The head: round above, the cheeks narrowing to a small chin, a short muzzle, points of fur behind the cheeks
    k.add("head", ell(H, HR, deform=jowl), GREY, "head")
    k.add("face", ell((0, -0.088, 0.61), (0.094, 0.072, 0.078)), GREY, "head")
    k.add("muzzle", ell((0, -0.138, 0.58), (0.05, 0.042, 0.035)), GREY, "head")
    k.add("cheek", blade([(0.11, 0.0, 0.595), (0.14, 0.018, 0.58), (0.165, 0.034, 0.57)], [0.028, 0.018, 0.006],
                         (0, 0, 1), 0.5), GREY, "head", mirror=True)
    k.add("neck", tube([(0, 0.0, 0.47), (0, -0.015, 0.52), (0, -0.025, 0.56)], [0.056, 0.054, 0.055]), GREY, "head")

    # The body: a slim chest swelling to a round belly
    k.add("chest", ell((0, 0.0, 0.45), (0.08, 0.07, 0.08)), GREY, "body")
    k.add("belly", ell((0, -0.006, 0.375), (0.09, 0.08, 0.085)), GREY, "body")

    # The legs: thighs fused into the hips, bent forward at the knee, shins tapering but thick, grey feet
    k.add("thigh", ell((0.052, -0.005, 0.3), (0.054, 0.06, 0.075)), GREY, "leg{s}", mirror=True)
    k.add("leg", tube([(0.054, 0.0, 0.29), (0.07, -0.055, 0.2), (0.078, 0.02, 0.09), (0.08, -0.005, 0.045)],
                      [0.05, 0.04, 0.034, 0.032]), GREY, "leg{s}", mirror=True)
    k.add("foot", ell((0.08, -0.04, 0.03), (0.038, 0.062, 0.03)), GREY, "leg{s}", mirror=True)
    for j, dx in enumerate((-0.017, 0.017)):
        b = np.array((0.08 + dx, -0.088, 0.022))
        k.add(f"toe{j}", hull([b + (0.012, 0, 0.01), b + (-0.012, 0, 0.01), b + (0.01, 0.01, -0.016),
                               b + (-0.01, 0.01, -0.016), b + (dx * 0.3, -0.04, -0.012)]), PALE, "leg{s}",
              fuse=False, mirror=True, surface="shell")

    # The arms: forward and a little down, the elbows bent; violet from a wavy cuff at the elbow to big hands
    SH, EL, WR = np.array([0.07, -0.02, 0.5]), np.array([0.112, -0.07, 0.445]), np.array([0.1, -0.15, 0.41])
    k.add("arm", tube([SH, EL, WR], [0.036, 0.032, 0.034]), GREY, "arm{s}", mirror=True)
    HAND = np.array([0.096, -0.19, 0.398])
    k.add("hand", ell(HAND, (0.05, 0.046, 0.044), rot=(-15, 0, 0)), VIOLET, "arm{s}", mirror=True)
    # the forearm violet beyond the cuff: a point is past the cuff when it is forward of a wavy plane across the arm
    def cuff(P):
        x = np.abs(P[:, 0])
        d = unit(*(WR - EL))
        rel = np.stack([x - EL[0], P[:, 1] - EL[1], P[:, 2] - EL[2]], axis=1)
        along = rel @ d
        ang = np.arctan2(rel[:, 2], rel[:, 0])
        near = inside(np.linalg.norm(rel - along[:, None] * d[None], axis=1) - 0.05, 0.003)
        return inside(0.012 + 0.008 * np.sin(4 * ang) - along, 0.0025) * near * inside(P[:, 2] - 0.48, 0.003)
    k.paint(cuff, VIOLET, on=["arm", "hand"])
    for j, (dx, dz) in enumerate([(0.024, 0.012), (0.0, 0.016), (-0.024, 0.01)]):
        b = HAND + np.array((dx, -0.036, dz - 0.01))
        tip = b + np.array((dx * 0.4, -0.05, -0.055))
        mid = b + np.array((dx * 0.2, -0.045, -0.012))
        k.add(f"claw{j}", hull([b + (0.01, 0.008, 0), b + (-0.01, 0.008, 0), b + (0, 0.008, 0.012),
                                b + (0, -0.006, -0.01), mid + (0.006, 0, 0.004), mid + (-0.006, 0, 0.004),
                                mid + (0, 0, 0.012), tip]), CLAW, "arm{s}", fuse=False, mirror=True,
              surface="shell")

    # The ears: a short grey point on -X; on +X a tall lilac blade standing up and a little out, its face forward
    k.add("ear", blade([(-0.068, 0.0, 0.715), (-0.092, 0.01, 0.77), (-0.11, 0.02, 0.81)], [0.044, 0.028, 0.004],
                       (0, 1, 0), 0.45), GREY, "earL")
    k.add("stub", blade([(0.07, 0.0, 0.72), (0.085, 0.006, 0.76)], [0.044, 0.034], (0, 1, 0), 0.5), GREY, "earR")
    EAR = [(0.08, 0.004, 0.74), (0.098, 0.01, 0.8), (0.115, 0.016, 0.87), (0.13, 0.022, 0.93), (0.14, 0.026, 0.975)]
    k.add("feather", blade(EAR, [0.03, 0.05, 0.052, 0.038, 0.006], (0, 1, 0.05), 0.38), LILAC, "earR", fuse=False,
          surface="fur")

    # The tail's fan of three lilac blades rising up and back from the rump, turned some 35 degrees out to +X so
    # that both of the game's cameras see their faces
    b = np.array([0.015, 0.06, 0.37])
    for j, tip in enumerate([(0.07, 0.17, 0.6), (0.12, 0.21, 0.5), (0.15, 0.22, 0.39)]):
        t = np.array(tip)
        d = unit(*(t - b))
        across = np.cross(d, unit(0.6, -0.8, 0))       # flatten across the plane turned out to +X
        across /= np.linalg.norm(across)
        k.add(f"plume{j}", blade([b, b + (t - b) * 0.45, t], [0.02, 0.042, 0.006], across, 0.42), LILAC, "tail",
              fuse=False, surface="fur")
    k.add("rump", ell((0, 0.055, 0.36), (0.042, 0.038, 0.038)), GREY, "tail")

    # The violet markings: a wavy band over the shoulders and upper chest, a mask round the muzzle, a streak up
    # the brow to the forehead's gem
    def band(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        bottom = 0.445 + 0.012 * np.sin(x * 70 + 1.0)
        reach = 0.085 + 0.01 * np.sin(z * 120)
        return inside(bottom - z, 0.003) * inside(z - 0.53, 0.003) * inside(np.abs(x) - reach, 0.003)
    k.paint(band, VIOLET, on=["chest", "neck", "belly"])
    def mask(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        e = ((x / 0.068) ** 2 + ((y + 0.15) / 0.08) ** 2 + ((z - 0.575) / (0.036 + 0.008 * np.sin(x * 60))) ** 2)
        return inside(e - 1, 0.03) * inside(y + 0.07, 0.003) * inside(z - 0.62, 0.003)
    k.paint(mask, VIOLET)
    def streak(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        t = np.clip((z - 0.665) / 0.11, 0, 1)
        w = 0.012 + 0.03 * np.sin(math.pi * t) ** 0.7
        return inside(np.abs(x) - w, 0.003) * inside(0.665 - z, 0.003) * inside(z - 0.775, 0.003) * inside(y + 0.07, 0.003)
    k.paint(streak, VIOLET)

    # The yellow gems: an upright oval on the brow and one on the chest
    k.decal((0, -0.148, 0.715), unit(0, -1, 0.5), (0.015, 0.028), GOLD, eps=0.004, rings=8, segs=32, surface="shell")
    k.decal((0, -0.08, 0.465), unit(0, -1, 0.05), (0.013, 0.024), GOLD, eps=0.004, rings=8, segs=32, surface="shell")

    # The mouth: a small sly grin low on the mask, a little white fang hanging from its top
    CM, CF, CV = srgb("#24101A"), srgb("#F4F4F8"), srgb(VIOLET)
    def grin(u, v):
        c = np.tile(CV, (len(u), 1))
        top = 0.05 + 0.32 * u + 0.18 * u * u
        lens = np.clip(1 - ((u - 0.3) / 0.62) ** 2, 0, 1)
        low = top - 0.2 - 0.6 * lens
        dark = (v < top) & (v > low) & (np.abs(u) < 0.9)
        c[dark] = CM
        fang = (v < top - 0.02) & (v > top - 0.45) & (np.abs(u - 0.5) < 0.18 * (v - (top - 0.45)) / 0.4)
        c[dark & fang] = CF
        return c
    k.decal((0.004, -0.173, 0.571), unit(0, -1, -0.15), (0.034, 0.021), grin, eps=0.004, rings=12, segs=48)

    # The eyes: almond, slanting up to the outer corner where a dark lash flicks out; a thick dark top lid; a red
    # iris looking in toward the nose, a dark pupil
    CG, CW, CR, CP, CI = srgb(GREY), srgb("#F8F8FA"), srgb("#D8283E"), srgb("#1A0A10"), srgb(INK)
    def top(u):
        return 0.18 + 0.42 * u - 0.12 * u * u
    def bottom(u):
        return -0.5 + 0.2 * u + 0.22 * u * u
    def eye(u, v):
        c = np.tile(CG, (len(u), 1))
        white = (v < top(u)) & (v > bottom(u)) & (np.abs(u) < 0.96)
        c[white] = CW
        iris = white & (((u + 0.2) / 0.38) ** 2 + ((v + 0.08) / 0.52) ** 2 < 1)
        c[iris] = CR
        c[iris & (((u + 0.2) / 0.14) ** 2 + ((v + 0.08) / 0.32) ** 2 < 1)] = CP
        c[(np.abs(v - top(u) - 0.03) < 0.14) & (np.abs(u) < 0.98)] = CI
        c[(u > 0.55) & (v > top(u) - 0.05) & (v < top(u) + 0.32 * (u - 0.4)) & (u * u + v * v < 0.97)] = CI
        return c
    de = unit(0.48, -0.87, 0.08)
    E = H + de * 0.122 + np.array([0, 0, -0.008])
    def eyes(state):
        k.decal(E, de, (0.046, 0.038), eye_look(state, eye, CG, CI, line=0.17), eps=0.005, rings=20, segs=56,
                mirror=True)

    k.build(voxel=0.0055)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    sneasel_hisui(*args())
