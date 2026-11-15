# Aggron sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/aggron.py -- <the export's Aggron.glb> <out.glb>
# Grown from aron.py and lairon.py (their steel, their holes in the steel, the eye in a black socket, Lairon's angry
# blue eye, its hard spikes and fangs, its white cuffs). From the references: a massive upright beast in grey armour,
# leaning a little forward; the head a white steel helmet with two thick horns running forward and up from its brow,
# two black holes on its crown and a flap down each side at the back; under its brow angry blue eyes in black sockets,
# and below a white muzzle with the jaws open, pink within, a fang at each corner; a broad grey chest of plates parted
# by darker seams; a great grey plate rising behind each shoulder; short thick arms held forward and down, a white cuff
# on each forearm and three white claws a fist; great thighs fused into the hips, each in a broad white band, short
# thick shins and broad feet with three white claws; a long thick tail of plates sweeping back and out to +X.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, ring_band, srgb, inside, smoothstep, unit, eye_look, boxy, args, sibling

ar = sibling("aron")
la = sibling("lairon")
STEEL, HOLE, SOCKET = ar.STEEL, ar.HOLE, ar.SOCKET
GREY, SEAM = "#5E636B", "#3C4046"
MOUTH, LINING = la.MOUTH, la.LINING
INK = ar.INK


def aggron(src, out):
    k = Kit(src, coat="metal")

    # The trunk: a great barrel leaning a little forward, broad chest, wide hips
    k.add("torso", ell((0, 0.0, 0.5), (0.2, 0.165, 0.22)), GREY, "body")
    k.add("chest", ell((0, -0.035, 0.62), (0.195, 0.15, 0.115)), GREY, "body")
    k.add("hips", ell((0, 0.02, 0.37), (0.21, 0.165, 0.13)), GREY, "body")
    k.add("neck", tube([(0, 0.0, 0.66), (0, -0.04, 0.72), (0, -0.07, 0.76)], [0.12, 0.1, 0.09]), GREY, "head")
    k.add("nape", ell((0, 0.0, 0.73), (0.14, 0.11, 0.07)), GREY, "head")          # fills the crease under the helmet

    # The chest's plates parted by darker seams: two across, one down the middle
    def seams(P):
        front = (P[:, 1] < -0.04)
        across = sum(inside(np.abs(P[:, 2] - z) - 0.006, 0.0025) for z in (0.46, 0.585))
        down = inside(np.abs(P[:, 0]) - 0.006, 0.0025) * (P[:, 2] > 0.4) * (P[:, 2] < 0.7)
        return np.clip(across + down, 0, 1) * front * (P[:, 2] > 0.35) * (P[:, 2] < 0.72)
    k.paint(seams, SEAM, on=["torso", "chest", "hips"])

    # The head is built at its first size and scaled up about the neck by HS, sunk a little into the shoulders
    PV, HS, DROP = np.array([0.0, -0.06, 0.70]), 1.2, np.array([0.0, 0.0, -0.03])
    def hp(p):
        return PV + (np.array(p, float) - PV) * HS + DROP
    def hx(shape):
        V, F = shape
        return PV + (np.asarray(V) - PV) * HS + DROP, F
    def hinv(P):
        return PV + (P - DROP - PV) / HS

    # The helmet: the head itself, white, its front coming down over the brow; a dark nape fills it from behind
    HC, HR = np.array([0.0, -0.1, 0.81]), np.array([0.145, 0.18, 0.112])
    def cap(V):
        V = boxy(2.4)(V)
        front = np.clip(-V[:, 1], 0, 1)
        V[:, 2] -= 0.25 * front * np.clip(-V[:, 2] + 0.3, 0, 1.3)     # the brow down over the eyes
        V[:, 0] *= 1 - 0.18 * np.clip(V[:, 2], 0, 1)
        return V
    k.add("helmet", hx(ell(HC, HR, rot=(-8, 0, 0), deform=cap)), STEEL, "head")
    # the flaps down each side at the back
    FLAP = np.array([(0.1, -0.09, 0.8), (0.1, 0.07, 0.83), (0.135, 0.06, 0.68), (0.16, -0.05, 0.67), (0.075, -0.06, 0.75),
                     (0.075, 0.05, 0.76)])
    FLAP = FLAP.mean(axis=0) + (FLAP - FLAP.mean(axis=0)) * 1.35        # larger flaps
    k.add("flap", hx(hull(FLAP)), STEEL, "head", fuse=False, mirror=True, surface="metal")
    # the white muzzle under it, the jaws open
    k.add("muzzle", hx(ell((0, -0.205, 0.69), (0.098, 0.108, 0.07), deform=boxy(2.4))), STEEL, "head")
    k.add("jaw", hx(ell((0, -0.18, 0.635), (0.08, 0.1, 0.04), rot=(12, 0, 0))), STEEL, "head")
    k.add("mouth", hx(ell((0, -0.3, 0.67), (0.062, 0.08, 0.035), rot=(-8, 0, 0))), MOUTH, "head", carve=True)
    def mouthmask(P):
        Q = hinv(P)
        return inside(np.hypot(Q[:, 0] / 0.062, (Q[:, 2] - 0.668) / 0.036) - 1, 0.003) * (Q[:, 1] < -0.24)
    def liningmask(P):
        Q = hinv(P)
        return inside(np.hypot(Q[:, 0] / 0.04, (Q[:, 2] - 0.662) / 0.018) - 1, 0.003) * (Q[:, 1] < -0.24)             * (Q[:, 1] > -0.28)
    k.paint(mouthmask, MOUTH)
    k.paint(liningmask, LINING)
    for s in (1, -1):
        la.spike(k, f"fang{'R' if s > 0 else 'L'}", hp((0.045 * s, -0.275, 0.645)), (0.15 * s, -0.25, 1), 0.048,
                 0.031, 0.024, (0, 1, 0), STEEL, "head", sink=0.012)

    # The horns: a thick pair from the brow, forward and up and a little apart
    for s, side in ((1, "R"), (-1, "L")):
        la.spike(k, f"horn{side}", hp((0.055 * s, -0.2, 0.83)), (0.18 * s, -1, 0.4), 0.44, 0.175, 0.135,
                 (0, 0, 1), STEEL, "head", sink=0.05)
    # the two black holes on its crown
    for d in ((0.38, 0.35, 0.86),):
        p = hp(ar.on_ell(HC, HR, d))
        k.decal(p, unit(*d), (0.031, 0.029), HOLE, up=(0, 1, 0), eps=0.004, rings=10, segs=40, mirror=True)

    # The plates rising behind the shoulders, grey, broad and few
    for s, side in ((1, "R"), (-1, "L")):
        la.spike(k, f"shoulderplate{side}", (0.13 * s, 0.07, 0.62), (0.25 * s, 0.45, 1), 0.21, 0.14, 0.06,
                 (0.3 * s, 1, 0), GREY, "body", sink=0.06)

    # The arms: short and thick, forward and down; a white cuff, a fist with three white claws
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        SH, EL, HA = np.array((0.17, -0.01, 0.63)), np.array((0.25, -0.09, 0.54)), np.array((0.27, -0.2, 0.49))
        k.add(f"arm{side}", tube([SH * m, EL * m, HA * m], [0.085, 0.072, 0.066]), GREY, f"arm{side}")
        k.add(f"fist{side}", ell(np.array((0.275, -0.235, 0.48)) * m, (0.068, 0.065, 0.065)), GREY, f"arm{side}")
        d = unit(*(HA - EL))
        k.add(f"cuff{side}", ring_band((EL * 0.35 + HA * 0.65) * m, d * m, 0.078, 0.06, thick=0.016), STEEL,
              f"arm{side}", fuse=False, surface="metal")
        for j, (dx, dz) in enumerate(((-0.032, 0.02), (0.0, 0.03), (0.032, 0.02))):
            la.spike(k, f"claw{side}{j}", np.array((0.275 + dx, -0.28, 0.5 + dz)) * m,
                     np.array((dx * 0.5, -0.7, 0.75)) * m, 0.05, 0.034, 0.028, (0, 1, 0), STEEL, f"arm{side}",
                     sink=0.015)

    # The legs: great round thighs in a broad white band, short thick shins, broad feet with three white claws
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array((0.17, 0.02, 0.28)) * m, (0.155, 0.155, 0.15)), GREY, f"leg{side}")
        k.add(f"band{side}", ring_band(np.array((0.17, 0.02, 0.28)) * m, (0, 0, 1), 0.16, 0.09, thick=0.016), STEEL,
              f"leg{side}", fuse=False, surface="metal")
        k.add(f"shin{side}", tube(np.array([(0.18, 0.0, 0.17), (0.185, -0.01, 0.115)]) * m, [0.112, 0.106]), GREY,
              f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.185, -0.055, 0.047)) * m, (0.112, 0.14, 0.05)), GREY, f"leg{side}")
        for j, dx in enumerate((-0.05, 0.0, 0.05)):
            la.spike(k, f"toe{side}{j}", np.array((0.185 + dx, -0.175, 0.045)) * m,
                     np.array((dx * 0.6, -1, -0.35)) * m, 0.055, 0.04, 0.032, (0, 0, 1), STEEL, f"leg{side}",
                     sink=0.015)

    # The tail: thick at the hips, long, sweeping back, down and out to +X, plated along its top
    TAIL = [(0, 0.14, 0.4), (0.04, 0.3, 0.27), (0.1, 0.42, 0.17), (0.16, 0.51, 0.115), (0.21, 0.57, 0.1)]
    TR = [0.17, 0.125, 0.08, 0.048, 0.015]
    k.add("tail", tube(TAIL, TR), GREY, "tail")
    pts = np.array(TAIL)
    for j, t in enumerate((0.2, 0.45, 0.7)):
        i = t * (len(pts) - 1); a = int(i); f = i - a
        c = pts[a] * (1 - f) + pts[a + 1] * f
        r = np.interp(t, np.linspace(0, 1, 5), TR)
        dirn = unit(*(pts[a + 1] - pts[a]))
        la.spike(k, f"tailplate{j}", c + np.array([0, 0, r * 0.75]), unit(*(dirn + np.array([0, 0, 0.5]))),
                 0.09 - 0.015 * j, 0.11 - 0.02 * j, 0.045, np.cross(dirn, (0, 0, 1.0)), GREY, "tail", sink=0.04)

    # The eyes: angry and blue, each in a black socket under the helmet's brow
    cut, look = la.angry_eye(slope=0.45, top=0.4)
    de = unit(0.6, -0.78, 0.05)
    E = hp((0.07, -0.245, 0.765))
    k.decal(E, de, (0.056, 0.05), SOCKET, eps=0.004, rings=12, segs=56, mirror=True)
    mm = np.array([-1, 1, 1.0])
    def eyes(state):
        lk = eye_look(state, look, srgb(SOCKET), ar.LIGHTLINE, line=0.16)
        k.decal(E, de, (0.043, 0.037), lk, eps=0.007, rings=20, segs=56, outline=cut)
        k.decal(E * mm, de * mm, (0.043, 0.037), lambda u, v: lk(-u, v), eps=0.007, rings=20, segs=56,
                outline=lambda th: cut(math.pi - th))

    k.build(voxel=0.0075, smooth=4, band=0.004)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    aggron(*args())
