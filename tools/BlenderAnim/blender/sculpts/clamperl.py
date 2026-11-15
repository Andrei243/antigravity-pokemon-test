# Clamperl sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/clamperl.py -- <the export's Clamperl.glb> <out.glb>
#
# From the references: a blue clam shell held wide open, its upper half standing up behind like a great dish facing
# the front, its lower half a bowl; both halves' edges cut into square teeth like a gear's, the outside blue, the cut
# edge and a band inside it white, the inside black. In the bowl a cushion of pale blue lobes, two rising at each side
# like hands, holds a round pink pearl with a sleeping face: shut eyes curving down, a small round mouth, white cheeks.
# Each half of the shell is three loose layers of one dish (dish(): an ellipsoidal cap whose rim is a toothed outline):
# the blue outside stopping just short of the rim, a white layer through the shell's whole thickness at the rim, and a
# black lining short of the rim by the band's width, so the white band follows every tooth.
# The skeleton: body (0, 0, 0.12), lid (0, 0.12, 0.08) at the hinge behind, head (0, -0.03, 0.14) in the pearl.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, inside, smoothstep, unit, eye_look, args, outward

BLUE, WHITE, BLACK = "#3E6EB8", "#EEF2F8", "#141620"
CUSHION, PINK, INK, MOUTH, CHEEK = "#9ACCF0", "#F4A6B2", "#5A2836", "#A8334E", "#FFF0F2"

PC = np.array([0.0, -0.035, 0.19])          # the pearl's middle
PR = 0.08                                   # its radius


def frame(a):
    a = np.array(a, float); a /= np.linalg.norm(a)
    e1 = np.array([1.0, 0, 0]) - a * a[0]; e1 /= np.linalg.norm(e1)
    return e1, np.cross(a, e1), a


def dish(O, a, R, phimax, thick, offset, nu=240, nv=12):
    """
    A closed thick cap of an ellipsoid (radii R along e1, e2, a about O), its opening facing +a: the point at angle th
    round the axis and phi from the pole (behind, at -a) is on the cap while phi < phimax(th). offset moves the layer
    along the surface's normal (below nought toward the hollow side).
    """
    e1, e2, a = frame(a)
    O = np.array(O, float)
    th = np.linspace(0, 2 * math.pi, nu, endpoint=False)
    pm = phimax(th)
    def surf(phi, t):
        s = np.stack([np.sin(phi) * np.cos(t), np.sin(phi) * np.sin(t), -np.cos(phi)], axis=-1)
        P = O + R[0] * s[..., :1] * e1 + R[1] * s[..., 1:2] * e2 + R[2] * s[..., 2:] * a
        n = (s[..., :1] / R[0]) * e1 + (s[..., 1:2] / R[1]) * e2 + (s[..., 2:] / R[2]) * a
        n = n / np.linalg.norm(n, axis=-1, keepdims=True)
        return P, n
    rows = []
    for j in range(1, nv + 1):
        rows.append(surf(pm * j / nv, th))
    pole, pn = surf(np.zeros(1), np.zeros(1))
    out_, in_ = offset + thick / 2, offset - thick / 2
    V = [pole[0] + pn[0] * out_, pole[0] + pn[0] * in_]
    for P, n in rows:
        V.extend(P + n * out_)
    for P, n in rows:
        V.extend(P + n * in_)
    V = np.array(V)
    A0, B0 = 2, 2 + nv * nu
    def ia(j, i):
        return A0 + j * nu + i % nu
    def ib(j, i):
        return B0 + j * nu + i % nu
    F = []
    for i in range(nu):
        F.append([0, ia(0, i), ia(0, i + 1)])
        F.append([1, ib(0, i + 1), ib(0, i)])
    for j in range(nv - 1):
        for i in range(nu):
            F.append([ia(j, i), ia(j + 1, i), ia(j + 1, i + 1), ia(j, i + 1)])
            F.append([ib(j, i), ib(j, i + 1), ib(j + 1, i + 1), ib(j + 1, i)])
    for i in range(nu):
        F.append([ia(nv - 1, i), ib(nv - 1, i), ib(nv - 1, i + 1), ia(nv - 1, i + 1)])
    return outward((V, F))


def teeth(base, depth, n, phase, soft=0.3):
    """A gear's outline: the rim at base, cut back by depth in n square notches round it."""
    def f(th):
        return base - depth * smoothstep(-soft, soft, np.sin(n * th + phase))
    return f


def shell(k, name, O, a, R, outline, bone):
    """One half of the shell: the blue outside, the white rim and band, the black lining."""
    k.add(name + "out", dish(O, a, R, lambda t: outline(t) - 0.03, 0.012, 0.003), BLUE, bone, fuse=False,
          surface="shell")
    k.add(name + "rim", dish(O, a, R, outline, 0.016, -0.004), WHITE, bone, fuse=False, surface="shell")
    k.add(name + "lin", dish(O, a, R, lambda t: outline(t) - 0.17, 0.006, -0.013), BLACK, bone, fuse=False,
          surface="shell")


def clamperl(src, out):
    k = Kit(src, coat="shell")

    # The upper half: a dish standing up behind, its hollow facing the front and a little up
    shell(k, "upper", (0, 0.125, 0.305), unit(0, -1, 0.16), (0.225, 0.205, 0.075),
          teeth(1.58, 0.3, 7, 0.4, soft=0.14), "lid")
    # The lower half: a bowl, its hollow facing up and a little forward
    shell(k, "lower", (0, 0.0, 0.128), unit(0, -0.22, 1), (0.215, 0.185, 0.118),
          teeth(1.52, 0.26, 8, 1.2, soft=0.14), "body")
    # Where the halves meet behind
    k.add("hinge", outward(ell((0, 0.165, 0.125), (0.07, 0.035, 0.045))), BLUE, "body", fuse=False, surface="shell")

    # --- the cushion of pale blue lobes in the bowl ------------------------------------------------------------------
    k.add("cushion", ell((0, 0.0, 0.098), (0.145, 0.115, 0.05)), CUSHION, "body")
    k.add("cushback", ell((0, 0.055, 0.12), (0.12, 0.06, 0.055)), CUSHION, "body")
    for s in (1, -1):
        m = np.array([s, 1, 1])
        # a hand at each side rising beside the pearl, and a second lobe outside and behind it
        k.add("hand" + "RL"[s < 0], ell(np.array([0.094, -0.07, 0.142]) * m, (0.046, 0.045, 0.066),
                                         rot=(-12, 18 * s, 0)), CUSHION, "body")
        k.add("lobe" + "RL"[s < 0], ell(np.array([0.135, 0.0, 0.158]) * m, (0.05, 0.048, 0.064),
                                         rot=(0, 14 * s, 0)), CUSHION, "body")
        k.add("toe" + "RL"[s < 0], ell(np.array([0.045, -0.11, 0.098]) * m, (0.04, 0.036, 0.034)), CUSHION, "body")

    # --- the pearl -----------------------------------------------------------------------------------------------------
    k.add("pearl", ell(PC, (PR, PR * 0.97, PR)), PINK, "head")

    def on(d):
        d = unit(*d)
        return PC + d * PR, d

    # the mouth: a small round dark rose O under the eyes; the cheeks white
    c, n = on((0, -1, -0.12))
    k.decal(c, n, (0.0095, 0.011), srgb(MOUTH), eps=0.003, rings=8, segs=36)
    for s in (1, -1):
        c, n = on((s * 0.56, -0.8, -0.02))
        k.decal(c, n, (0.0085, 0.006), srgb(CHEEK), eps=0.003, rings=8, segs=36)

    k.build(voxel=0.004, smooth=4, band=0.005)

    # --- the eyes: always shut, two lines curving down; the open state is the slit itself ----------------------------
    lid, ink = srgb(PINK), srgb(INK)

    def slit(u, v):
        c = np.tile(lid, (len(u), 1))
        c[(np.abs(u) < 0.86) & (np.abs(v - (-0.05 - 0.3 * (1 - u * u))) < 0.2)] = ink
        return c

    def fierce(u, v):
        c = np.tile(lid, (len(u), 1))
        c[(np.abs(u) < 0.86) & (np.abs(v - (-0.25 + 0.45 * u)) < 0.2)] = ink
        return c

    def eyes(state):
        look = fierce if state == "Fierce" else (slit if state == "Open" else eye_look(state, slit, lid, ink, line=0.2))
        for s in (1, -1):
            lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
            c, n = on((s * 0.44, -0.86, 0.24))
            k.decal(c, n, (0.027, 0.022), lk, eps=0.003, rings=20, segs=56)

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    clamperl(*args())
