# Slaking sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Slakoth's script (slakoth.py, imported as sibling("slakoth")): the same pink pig's nose with two nostrils, the same
# heavy-lidded eye in a dark patch, the flat tufts of fur, the stripes of the back come round as grooves on the head.
#   blender --background --factory-startup --python sculpts/slaking.py -- <the export's Slaking.glb> <out.glb>
#
# The design, from the references: a huge brown gorilla lolling on its belly (the skeleton is a quadruped's, so it
# lies; the art lies it on its side); a great round tan belly and chest under a brown back; a tan head with brown
# grooves arching over the crown, a white knot of a tuft at its back; a shaggy white ruff framing the face from the
# cheeks to under the chin; a big pink pig's nose, heavy-lidded eyes under a brown brow, a wide pink mouth with a small
# fang standing up at each corner; brown arms with shaggy cuffs and tan hands, the right fist propped under the chin,
# the left arm lying back along its side; the hind legs sprawled behind, shaggy brown cuffs over tan feet.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, boxy, args, sibling

s_ = sibling("slakoth")
hit, claw, tuft = s_.hit, s_.claw, s_.tuft

BROWN, TAN, WHITE_FUR, PATCH = "#8C6849", "#DCC6A2", "#F3F1EC", "#76523A"
INK, MOUTH, FANG = s_.INK, "#C9606C", "#FFFFFF"

H = np.array([0.0, -0.285, 0.39])            # the head's middle (its bone is at 0, -0.2, 0.28)
HR = np.array([0.14, 0.128, 0.13])


def cuff(k, name, c, d, r, n, length, bone, color=BROWN):
    """A shaggy cuff: n flat tufts round a limb at c, leaning along d."""
    c, d = np.array(c, float), unit(*d)
    a = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a)
    b = np.cross(d, a)
    for i in range(n):
        th = 2 * math.pi * i / n
        o = math.cos(th) * a + math.sin(th) * b
        tuft(k, f"{name}{i}", c + o * r * 0.8, d * 0.75 + o * 0.6, length, 0.026, 0.026, bone, color)


def slaking(src, out):
    k = Kit(src, coat="fur")

    # The great body lying on its belly: a broad round mass, the rump one round mass behind
    def settle(V):
        V = V.copy()
        V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.85, V[:, 2])
        return V
    k.add("torso", ell((0, 0.03, 0.215), (0.25, 0.27, 0.215), deform=settle), BROWN, "body")
    k.add("chest", ell((0, -0.17, 0.27), (0.2, 0.13, 0.17)), TAN, "body")
    k.add("rump", ell((0, 0.22, 0.17), (0.2, 0.14, 0.15)), BROWN, "body")

    # The head held up, a little forward, tan
    def head_shape(V):
        V = boxy(2.3)(V)
        V[:, 0] *= 1 + 0.06 * np.clip(-V[:, 2], 0, 1)
        return V
    HEAD = ell(H, HR, deform=head_shape)
    k.add("head", HEAD, TAN, "head")
    k.add("muzzle", ell(H + (0, -0.075, -0.05), (0.1, 0.07, 0.065)), TAN, "head")
    # The heavy brown brow over the eyes
    k.add("brow", ell(H + (0.05, -0.1, 0.052), (0.055, 0.03, 0.022), rot=(0, 12, -12)), PATCH, "head", mirror=True)

    # The tan belly under the brown back: the lower half of the body's flanks and front
    k.paint(lambda P: inside(P[:, 2] - 0.16 - 0.25 * np.clip(-(P[:, 1] + 0.05), 0, 1), 0.004), TAN,
            on=["torso", "rump"])

    # Grooves over the crown: two brown arcs from brow to brow over the top
    for r0 in (0.6, 0.85):
        def groove(P, r0=r0):
            d = P - (H + np.array([0, -0.11, -0.02]))
            rr = np.sqrt(d[:, 0] ** 2 + (d[:, 1] * 0.8) ** 2 + (d[:, 2] * 1.3) ** 2) / HR[0]
            return inside(np.abs(rr - r0 * 1.2) - 0.035, 0.004) * (P[:, 2] > H[2] + 0.03)
        k.paint(groove, PATCH, on=["head"])

    # The white knot of a tuft at the back of the head
    for i, (lean, L) in enumerate([(0.6, 0.075), (-0.6, 0.075), (0.0, 0.06)]):
        root = H + np.array([0, HR[1] * 0.7, HR[2] * 0.62])
        tuft(k, f"knot{i}", root, (lean, 0.8, 0.45), L, 0.03, 0.026, "head", WHITE_FUR)
    k.add("knotroot", ell(H + (0, HR[1] * 0.72, HR[2] * 0.6), (0.04, 0.03, 0.035)), WHITE_FUR, "head")

    # The shaggy white ruff framing the face: the jaw and cheeks white, flat tufts radiating from them
    def jaw(P):
        d = P - H
        side = np.sqrt(d[:, 0] ** 2 + (d[:, 2] + 0.02) ** 2)
        return inside(0.11 - side, 0.004) * (d[:, 2] < 0.035) * (d[:, 1] > -0.13)
    k.paint(jaw, WHITE_FUR, on=["head"])
    for i, ang in enumerate(np.linspace(-110, 110, 11)):
        a = math.radians(ang)
        o = np.array([math.sin(a), 0.1, -math.cos(a)])
        root = H + np.array([o[0] * HR[0] * 0.9, -0.02, -0.02 + o[2] * HR[2] * 0.85])
        tuft(k, f"ruff{i}", root, o + np.array([0, 0.1, -0.35]), 0.07 + 0.015 * (i % 2), 0.03, 0.03, "head",
             WHITE_FUR)
    # The ruff down the chest
    for i, x in enumerate((-0.12, -0.06, 0.0, 0.06, 0.12)):
        root = np.array([x, -0.27, 0.26 - 0.03 * abs(x) / 0.12])
        tuft(k, f"chestruff{i}", root, (x * 2, -0.45, -0.9), 0.08, 0.032, 0.03, "body", WHITE_FUR)
    k.paint(lambda P: inside(np.hypot(P[:, 0] / 0.14, (P[:, 2] - 0.29) / 0.11) - 1, 0.004)
            * (P[:, 1] < -0.2), WHITE_FUR, on=["chest"])

    # The pig's nose, a wide pink mouth under it with a small fang standing up at each corner
    NOSE_C = H + np.array([0, -0.14, -0.035])
    s_.nose(k, NOSE_C, (0.05, 0.03, 0.036), "head")
    CM, CT = srgb(MOUTH), srgb(TAN)
    def mouth(u, v):
        c = np.tile(CT, (len(u), 1))
        lower = -0.1 - 0.3 * (1 - u * u)
        c[(v < 0.35 + 0.15 * u * u) & (v > lower) & (np.abs(u) < 0.95)] = CM
        return c
    MUZ = ell(H + (0, -0.075, -0.05), (0.1, 0.07, 0.065))
    MC = hit(MUZ, (0, -1, H[2] - 0.08), (0, 1, 0))
    k.decal(MC, unit(0, -1, -0.3), (0.05, 0.02), mouth, eps=0.004, rings=10, segs=56)
    for side in (1, -1):
        p = hit(MUZ, (side * 0.042, -1, H[2] - 0.093), (0, 1, 0))
        claw(k, f"fang{'R' if side > 0 else 'L'}", p + np.array([0, 0.004, 0]), (side * 0.15, -0.2, 1), 0.026,
             0.01, "head", FANG)

    # The right arm (+X) bent, the elbow on the ground forward, the fist propped under the chin
    k.add("shoulderR", ell((0.19, -0.13, 0.27), (0.1, 0.1, 0.1)), BROWN, "legR")
    k.add("armR", tube([(0.2, -0.15, 0.24), (0.22, -0.3, 0.1), (0.17, -0.38, 0.08)], [0.085, 0.075, 0.07]),
          BROWN, "legR")
    k.add("foreR", tube([(0.17, -0.38, 0.08), (0.11, -0.41, 0.15), (0.07, -0.42, 0.21)], [0.068, 0.062, 0.058]),
          BROWN, "legR")
    cuff(k, "cuffR", (0.08, -0.42, 0.2), (-0.35, -0.1, 0.9), 0.06, 7, 0.06, "legR")
    k.add("fistR", ell((0.055, -0.42, 0.255), (0.06, 0.058, 0.055), deform=boxy(2.5)), TAN, "legR")

    # The left arm (-X) lying back along its side, the hand resting on the ground by the hip
    k.add("shoulderL", ell((-0.19, -0.13, 0.27), (0.1, 0.1, 0.1)), BROWN, "legL")
    k.add("armL", tube([(-0.2, -0.13, 0.25), (-0.28, -0.05, 0.13), (-0.29, 0.07, 0.075), (-0.27, 0.17, 0.07)],
                       [0.085, 0.075, 0.068, 0.062]), BROWN, "legL")
    cuff(k, "cuffL", (-0.27, 0.17, 0.085), (0.05, 1, 0.0), 0.06, 7, 0.06, "legL")
    k.add("handL", ell((-0.26, 0.255, 0.05), (0.06, 0.065, 0.05), deform=boxy(2.5)), TAN, "legL")

    # The hind legs sprawled behind, shaggy cuffs over tan feet
    for s, bone in ((1, "hindR"), (-1, "hindL")):
        m = np.array([s, 1, 1.0])
        k.add("thigh" + bone[-1], ell(np.array((0.13, 0.25, 0.13)) * m, (0.12, 0.13, 0.11)), BROWN, bone)
        k.add("shin" + bone[-1], tube([np.array(p) * m for p in [(0.15, 0.3, 0.1), (0.18, 0.42, 0.07)]],
                                      [0.085, 0.075]), BROWN, bone)
        cuff(k, "cuffh" + bone[-1], np.array((0.18, 0.43, 0.085)) * m, (0.1 * s, 1, 0), 0.06, 7, 0.06, bone)
        k.add("foot" + bone[-1], ell(np.array((0.185, 0.5, 0.062)) * m, (0.065, 0.055, 0.062)), TAN, bone)

    # Heavy-lidded eyes in a brown patch, a tan lid, under the brow
    sleepy = s_.sleepy_eye()
    open_look = lambda u, v: sleepy(u, v, lid=srgb(TAN))
    de = unit(0.42, -0.9, 0.1)
    E = hit(HEAD, (0.05, -1, H[2] + 0.012), (0, 1, 0))
    def lay(state):
        k.decal(E, de, (0.036, 0.03), s_.patched(state, open_look, PATCH, ring=0.7, lid=TAN), eps=0.0025, rings=20,
                segs=56, mirror=True)

    k.build(voxel=0.008, band=0.005)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    slaking(*args())
