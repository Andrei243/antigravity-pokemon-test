# Aggron-Mega sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/aggronmega.py -- <the export's Aggron-Mega.glb> <out.glb>
# Grown from aggron.py (copied: it takes no arguments that reach what changes): its head, open jaws, fangs, angry blue
# eyes in black sockets, its fists and toes; and from aron.py and lairon.py through sibling.
# From the references (the pictures overrule the game's old sculpt, which only adds a crest and tail rings): Aggron
# shut into white armour. Largest masses first: the trunk a white barrel of plates over a rounded white shell behind,
# its belly light grey; great white gauntlets on the forearms, each with a white spike up from the elbow; tall dark
# grey blades rising behind the shoulders, the shoulders themselves dark grey balls; the helmet a white wedge rising to
# a point over the crown, three black holes on each side, a short thick horn forward from each cheek; white thighs,
# dark grey shins and feet with white claws; a short tail of stacked white rings ending in a dark spike.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, ring_band, srgb, inside, unit, eye_look, boxy, args, sibling

ar = sibling("aron")
la = sibling("lairon")
STEEL, HOLE, SOCKET = ar.STEEL, ar.HOLE, ar.SOCKET
GREY, SEAM, BELLY = "#5E636B", "#7C828B", "#A9AEB7"
MOUTH, LINING = la.MOUTH, la.LINING


def aggronmega(src, out):
    k = Kit(src, coat="metal")

    # --- the trunk: a broad white barrel, leaning a little forward, a rounded shell of plates behind ----------------
    k.add("torso", ell((0, 0.0, 0.5), (0.225, 0.185, 0.23)), STEEL, "body")
    k.add("chest", ell((0, -0.04, 0.62), (0.215, 0.16, 0.12)), STEEL, "body")
    k.add("hips", ell((0, 0.02, 0.37), (0.225, 0.18, 0.135)), STEEL, "body")
    k.add("shell", ell((0, 0.09, 0.56), (0.215, 0.155, 0.2)), STEEL, "body")
    k.add("neck", tube([(0, 0.0, 0.66), (0, -0.04, 0.72), (0, -0.07, 0.76)], [0.12, 0.1, 0.09]), GREY, "head")
    k.add("nape", ell((0, 0.0, 0.73), (0.14, 0.11, 0.07)), GREY, "head")

    # the light grey belly plate, the chest's seams, the back's plates
    def belly(P):
        return (inside(np.hypot(P[:, 0] / 0.155, (P[:, 2] - 0.45) / 0.155) - 1, 0.004) * (P[:, 1] < -0.06)
                * (P[:, 2] < 0.6))
    k.paint(belly, BELLY, on=["torso", "chest", "hips"])
    def seams(P):
        front = P[:, 1] < -0.05
        across = sum(inside(np.abs(P[:, 2] - z) - 0.005, 0.0025) for z in (0.565,))
        down = inside(np.abs(P[:, 0]) - 0.005, 0.0025) * (P[:, 2] > 0.57) * (P[:, 2] < 0.72)
        rim = 0 * P[:, 0]
        return np.clip(across + down + rim, 0, 1) * front * (P[:, 2] > 0.25) * (P[:, 2] < 0.72)
    k.paint(seams, SEAM, on=["torso", "chest", "hips"])
    def backseams(P):
        back = P[:, 1] > 0.08
        rows = sum(inside(np.abs(P[:, 2] - z) - 0.005, 0.0025) for z in (0.46, 0.6))
        mid = inside(np.abs(P[:, 0]) - 0.005, 0.0025)
        return np.clip(rows + mid, 0, 1) * back * (P[:, 2] > 0.4) * (P[:, 2] < 0.74)
    k.paint(backseams, SEAM, on=["torso", "shell"])

    # --- the head: aggron.py's, scaled about the neck, under a white wedge of a helmet -----------------------------
    PV, HS, DROP = np.array([0.0, -0.06, 0.70]), 1.22, np.array([0.0, -0.01, -0.03])
    def hp(p):
        return PV + (np.array(p, float) - PV) * HS + DROP
    def hx(shape):
        V, F = shape
        return PV + (np.asarray(V) - PV) * HS + DROP, F
    def hinv(P):
        return PV + (P - DROP - PV) / HS

    HC, HR = np.array([0.0, -0.1, 0.81]), np.array([0.145, 0.18, 0.112])
    def cap(V):
        V = boxy(2.4)(V)
        front = np.clip(-V[:, 1], 0, 1)
        V[:, 2] -= 0.25 * front * np.clip(-V[:, 2] + 0.3, 0, 1.3)
        V[:, 0] *= 1 - 0.18 * np.clip(V[:, 2], 0, 1)
        return V
    k.add("helmet", hx(ell(HC, HR, rot=(-8, 0, 0), deform=cap)), STEEL, "head")
    k.add("muzzle", hx(ell((0, -0.205, 0.69), (0.098, 0.108, 0.07), deform=boxy(2.4))), STEEL, "head")
    k.add("jaw", hx(ell((0, -0.18, 0.635), (0.08, 0.1, 0.04), rot=(12, 0, 0))), STEEL, "head")
    k.add("mouth", hx(ell((0, -0.3, 0.67), (0.062, 0.08, 0.035), rot=(-8, 0, 0))), MOUTH, "head", carve=True)
    def mouthmask(P):
        Q = hinv(P)
        return inside(np.hypot(Q[:, 0] / 0.062, (Q[:, 2] - 0.668) / 0.036) - 1, 0.003) * (Q[:, 1] < -0.24)
    def liningmask(P):
        Q = hinv(P)
        return (inside(np.hypot(Q[:, 0] / 0.04, (Q[:, 2] - 0.662) / 0.018) - 1, 0.003) * (Q[:, 1] < -0.24)
                * (Q[:, 1] > -0.28))
    k.paint(mouthmask, MOUTH)
    k.paint(liningmask, LINING)
    for s in (1, -1):
        la.spike(k, f"fang{'R' if s > 0 else 'L'}", hp((0.045 * s, -0.275, 0.645)), (0.15 * s, -0.25, 1), 0.048,
                 0.031, 0.024, (0, 1, 0), STEEL, "head", sink=0.012)
        # a white tooth hanging from the middle of the upper jaw on each side
        la.spike(k, f"tooth{'R' if s > 0 else 'L'}", hp((0.018 * s, -0.29, 0.69)), (0.05 * s, -0.3, -1), 0.04,
                 0.026, 0.02, (0, 1, 0), STEEL, "head", sink=0.01)

    # the wedge: a roof of white steel over the helmet, rising to a point over the crown and a little behind
    crest = []
    for s in (1, -1):
        crest += [(0.168 * s, -0.27, 0.8), (0.175 * s, 0.04, 0.8), (0.17 * s, -0.12, 0.79),
                  (0.06 * s, -0.17, 1.0), (0.045 * s, -0.02, 1.02)]
    crest += [(0, -0.33, 0.86), (0, -0.07, 1.09), (0, 0.1, 0.82)]
    k.add("crest", hull(crest), STEEL, "head", fuse=False, surface="metal")
    # three black holes on each side of the wedge, in a triangle near its top
    def side_x(y, z):
        # the side face through (0.168, -0.27, 0.8), (0.175, 0.04, 0.8) and (0.06, -0.17, 1.0)
        return 0.17 - 0.11 * (z - 0.8) / 0.2
    nrm = unit(0.88, 0, 0.47)
    for j, (y, z, r) in enumerate(((-0.07, 0.93, 0.022), (-0.0, 0.88, 0.02), (-0.12, 0.87, 0.018))):
        for s in (1, -1):
            ar.disc(k, f"hole{j}{'R' if s > 0 else 'L'}", (side_x(y, z) * s, y, z), (nrm[0] * s, 0, nrm[2]), r,
                    "head", thick=0.006, proud=0.0015)
    # a short thick horn forward and out from each cheek of the helmet
    for s, side in ((1, "R"), (-1, "L")):
        la.spike(k, f"horn{side}", (0.15 * s, -0.24, 0.81), (0.45 * s, -1, 0.1), 0.25, 0.08, 0.075,
                 (0, 0, 1), STEEL, "head", sink=0.05)
    # a peak of the helmet pointing down between the eyes, over the muzzle
    la.spike(k, "visor", hp((0, -0.27, 0.77)), (0, -0.35, -1), 0.06, 0.05, 0.03, (0, 1, 0), STEEL, "head", sink=0.02)

    # --- the shoulders: dark grey balls with tall dark blades rising behind them -----------------------------------
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"shoulder{side}", ell((0.185 * s, -0.01, 0.64), (0.1, 0.1, 0.095)), GREY, f"arm{side}")
        la.spike(k, f"blade{side}", (0.15 * s, 0.07, 0.66), (0.22 * s, 0.3, 1), 0.34, 0.17, 0.085,
                 (0.3 * s, 1, 0), GREY, "body", sink=0.08)

    # --- the arms: dark upper arms, great white gauntlets on the forearms with a spike up from each elbow, dark fists
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        SH, EL, HA = np.array((0.19, -0.01, 0.62)), np.array((0.255, -0.1, 0.55)), np.array((0.27, -0.21, 0.49))
        k.add(f"arm{side}", tube([SH * m, EL * m, HA * m], [0.085, 0.075, 0.066]), GREY, f"arm{side}")
        d = unit(*(HA - EL))
        mid = (EL * 0.3 + HA * 0.7)
        ang = math.degrees(math.atan2(d[2], -d[1]))
        k.add(f"gauntlet{side}", ell(mid * m, (0.1, 0.115, 0.092), rot=(-ang, 0, 0), deform=boxy(2.6)), STEEL,
              f"arm{side}", fuse=False, surface="metal")
        la.spike(k, f"elbow{side}", np.array((0.27, -0.06, 0.6)) * m, np.array((0.25, 0.45, 1)) * m, 0.25, 0.075,
                 0.06, np.array((1, 0, 0)) * m, STEEL, f"arm{side}", sink=0.04)
        k.add(f"fist{side}", ell(np.array((0.275, -0.245, 0.475)) * m, (0.07, 0.066, 0.066)), GREY, f"arm{side}")
        for j, (dx, dz) in enumerate(((-0.034, 0.02), (0.0, 0.03), (0.034, 0.02))):
            la.spike(k, f"claw{side}{j}", np.array((0.275 + dx, -0.29, 0.49 + dz)) * m,
                     np.array((dx * 0.5, -0.7, 0.75)) * m, 0.055, 0.036, 0.03, (0, 1, 0), STEEL, f"arm{side}",
                     sink=0.015)

    # --- the legs: great white thighs, short dark shins, broad dark feet with three white claws ------------------
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array((0.175, 0.0, 0.28)) * m, (0.165, 0.17, 0.155)), STEEL, f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.18, -0.01, 0.17), (0.185, -0.02, 0.11)]) * m, [0.11, 0.105]), GREY,
              f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.185, -0.06, 0.047)) * m, (0.115, 0.145, 0.05)), GREY, f"leg{side}")
        for j, dx in enumerate((-0.05, 0.0, 0.05)):
            la.spike(k, f"toe{side}{j}", np.array((0.185 + dx, -0.185, 0.045)) * m,
                     np.array((dx * 0.6, -1, -0.35)) * m, 0.06, 0.042, 0.034, (0, 0, 1), STEEL, f"leg{side}",
                     sink=0.015)
    # the thighs' armour ends in a crisp line over the shins
    k.paint(lambda P: inside(P[:, 2] - 0.155, 0.003) * (np.abs(P[:, 0]) > 0.06) * (P[:, 2] > 0.09), GREY,
            on=["thigh"])

    # --- the tail: short and thick, back, down and out to +X, in stacked white rings, a dark spike at its end -----
    TAIL = [(0, 0.15, 0.38), (0.04, 0.29, 0.27), (0.1, 0.39, 0.18), (0.15, 0.46, 0.13)]
    TR = [0.15, 0.115, 0.08, 0.05]
    k.add("tail", tube(TAIL, TR), GREY, "tail")
    pts = np.array(TAIL)
    for j, t in enumerate((0.2, 0.42, 0.63, 0.82)):
        i = t * (len(pts) - 1); a = int(i); f = i - a
        c = pts[a] * (1 - f) + pts[a + 1] * f
        r = np.interp(t, np.linspace(0, 1, len(TR)), TR)
        dirn = unit(*(pts[a + 1] - pts[a]))
        k.add(f"ring{j}", ring_band(c, dirn, r + 0.012, 0.062 - 0.008 * j, thick=0.03), STEEL, "tail", fuse=False,
              surface="metal")
    end = pts[-1]
    la.spike(k, "tailspike", end, unit(0.45, 0.6, -0.4), 0.11, 0.07, 0.06, (0, 0, 1), GREY, "tail", sink=0.03)

    # --- the eyes: aggron.py's, angry and blue in black sockets under the helmet's brow ---------------------------
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

    k.build(voxel=0.0078, smooth=4, band=0.004)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    aggronmega(*args())
