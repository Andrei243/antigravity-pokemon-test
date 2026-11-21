# Mega Kangaskhan sculpted on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/kangaskhanmega.py -- <the export's Kangaskhan-Mega.glb> <out.glb>
#
# Grown from Kangaskhan's script (its helpers by sibling; the mother's body copied here, since its function builds the
# baby in the pouch with no argument to leave it out): the mother as she was, her pouch now empty, and the baby grown to
# half her height, out of the pouch and standing on the ground before her to fight. From the references the baby is
# a small Kangaskhan: lilac, a pear of a body on short legs with white claws, a pale yellow belly with lilac hexagonal
# plates, a big round head with a short snout and an open pink mouth with a white fang, the mother's dark grey helmet
# over its crown and her yellow ears standing up and out of its sides, red eyes cut flat by the helmet, pink cheeks,
# short arms bent forward with white claws, lilac spikes down its back and a short spiked tail.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, leaf, outward, polygon, srgb, inside, unit, boxy, eye_look, sibling,
                       args)

kg = sibling("kangaskhan")
claw, plate = kg.claw, kg.plate
BROWN, CREAM, SHADE, HELM, EAR = kg.BROWN, kg.CREAM, kg.SHADE, kg.HELM, kg.EAR
BABY, MOUTH, CLAW, INK = kg.BABY, kg.MOUTH, kg.CLAW, kg.INK
H, HR = kg.H, kg.HR
BELLY, PLATE, BLUSH, SPIKE = "#ECDC84", "#9790C8", "#EE9CB0", "#9A93CC"

# The baby, standing on the ground before its mother (its bone is at 0, -0.28, 0.1)
BB = np.array([0.0, -0.335, 0.175])        # its body's middle
BH = np.array([0.0, -0.395, 0.36])          # its head's middle
BHR = np.array([0.108, 0.1, 0.1])


def mother(k):
    def pear(V):
        V = V.copy()
        low = np.clip(-V[:, 2], 0, 1)
        V[:, :2] *= (1 + 0.12 * low)[:, None]
        return V
    k.add("torso", ell((0, 0.03, 0.5), (0.235, 0.2, 0.29), rot=(5, 0, 0), deform=pear), BROWN, "body")
    k.add("chest", ell((0, -0.03, 0.64), (0.2, 0.16, 0.13)), BROWN, "body")
    k.add("hips", ell((0, 0.05, 0.31), (0.22, 0.18, 0.13)), BROWN, "body")
    k.add("neck", tube([(0, -0.01, 0.66), (0, -0.06, 0.76), (0, -0.09, 0.82)], [0.13, 0.11, 0.09]), BROWN, "head")

    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array((0.165, 0.0, 0.25)) * m, (0.12, 0.15, 0.15)), BROWN, f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.18, -0.02, 0.16), (0.185, -0.03, 0.08)]) * m, [0.09, 0.078]), BROWN,
              f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.185, -0.07, 0.04)) * m, (0.09, 0.12, 0.042)), BROWN, f"leg{side}")
        for j, dx in enumerate((-0.04, 0.0, 0.04)):
            claw(k, f"toe{side}{j}", np.array((0.185 + dx, -0.175, 0.03)) * m, (0.15 * s * np.sign(dx), -1, -0.15),
                 0.045, 0.017, f"leg{side}")
        plate(k, f"knee{side}", np.array((0.2, -0.13, 0.25)) * m, (0.4 * s, -1, 0.1), (0, 0, 1), 0.055, 0.05, 0.03,
              f"leg{side}")

    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"arm{side}", tube(np.array([(0.18, -0.02, 0.67), (0.28, -0.07, 0.6), (0.29, -0.17, 0.55)]) * m,
                                 [0.085, 0.072, 0.065]), BROWN, f"arm{side}")
        k.add(f"paw{side}", ell(np.array((0.285, -0.215, 0.545)) * m, (0.065, 0.06, 0.058)), BROWN, f"arm{side}")
        for j, (dx, dz) in enumerate(((-0.035, 0.02), (0.0, 0.03), (0.035, 0.02))):
            claw(k, f"handclaw{side}{j}", np.array((0.285 + dx, -0.265, 0.545 + dz)) * m,
                 (0.12 * s * np.sign(dx), -1, -0.45), 0.05, 0.016, f"arm{side}")
        plate(k, f"shoulder{side}", np.array((0.27, -0.02, 0.69)) * m, (1 * s, -0.25, 0.5), (0, 0.3, 1), 0.06, 0.05,
              0.03, f"arm{side}")

    TAIL = [(0, 0.16, 0.36), (0.03, 0.31, 0.21), (0.11, 0.44, 0.1), (0.24, 0.51, 0.055), (0.36, 0.5, 0.04)]
    k.add("tail", tube(TAIL, [0.12, 0.1, 0.075, 0.05, 0.018]), BROWN, "tail")
    for j, (y, z) in enumerate(((0.18, 0.66), (0.22, 0.55), (0.23, 0.44))):
        plate(k, f"back{j}", (0, y, z), (0, 1, 0.1), (0, 0, 1), 0.035, 0.035, 0.03, "body")

    k.add("cranium", ell(H, HR), BROWN, "head")
    def wedge(V):
        V = boxy(2.4)(V)
        V[:, 0] *= 1 + 0.15 * V[:, 1]
        return V
    k.add("snout", ell(H + (0, -0.11, -0.02), (0.075, 0.105, 0.055), rot=(-6, 0, 0), deform=wedge), BROWN, "head")
    k.add("jaw", ell(H + (0, -0.08, -0.095), (0.07, 0.095, 0.034), rot=(16, 0, 0), deform=wedge), BROWN, "head")
    k.add("cheek", ell(H + (0, -0.02, -0.055), (0.095, 0.075, 0.06)), BROWN, "head")
    k.add("brow", ell(H + (0.05, -0.075, 0.03), (0.045, 0.05, 0.022), rot=(-12, 0, -12)), BROWN, "head", mirror=True)
    def gape(V):
        V = V.copy()
        V[:, 2] += 0.3 * V[:, 1] ** 2
        return V
    k.add("mouth", ell(H + (0, -0.14, -0.065), (0.058, 0.1, 0.026), rot=(-4, 0, 0), deform=gape), MOUTH, "head",
          carve=True)
    for s in (1, -1):
        claw(k, f"fang{'R' if s > 0 else 'L'}", H + (0.04 * s, -0.19, -0.04), (0.05 * s, -0.1, -1), 0.028, 0.01,
             "head")

    def helmet(P):
        r = (P - H) / np.array([0.13, 0.14, 0.13])
        near = inside((r ** 2).sum(1) - 1, 0.02)
        return near * inside(H[2] + 0.03 - 0.12 * np.clip(P[:, 1] - H[1], 0, 1) - P[:, 2], 0.003)
    k.paint(helmet, HELM, on=["cranium", "brow", "neck"])

    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"ear{side}", outward(leaf(H + (0.08 * s, 0.03, 0.045), (0.55 * s, 0.85, 0.2), 0.165, 0.072,
                                        thick=0.03)), EAR, "head", fuse=False, surface="fur")

    # The belly, cream from the chest down; its lower half the pouch, empty now, its opening shaded
    def belly(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        return inside((x / 0.175) ** 2 + ((z - 0.43) / 0.25) ** 2 - 1, 0.008) * (y < -0.04)
    k.paint(belly, CREAM, on=["torso", "chest", "hips"])
    def pouch_shape(V):
        V = V.copy()
        V[:, 2] = np.minimum(V[:, 2], 0.7)
        return V
    k.add("lip", ell((0, -0.14, 0.4), (0.165, 0.115, 0.165), deform=pouch_shape), CREAM, "body")
    k.paint(lambda P: inside(np.abs(P[:, 2] - (0.535 + 1.6 * P[:, 0] ** 2)) - 0.009, 0.003)
            * inside(np.abs(P[:, 0]) - 0.16, 0.004) * (P[:, 1] < -0.08), SHADE, on=["torso", "chest", "lip"])


def spike(k, name, base, d, length, w, bone="baby"):
    """A lilac spike of the baby's back: a four-sided point, its root buried in the body."""
    base, d = np.array(base, float), unit(*d)
    a = np.cross(d, (1.0, 0, 0)); a /= np.linalg.norm(a)
    b = np.cross(d, a)
    k.add(name, hull([base + a * w, base - a * w, base + b * w * 0.7, base - b * w * 0.7, base - d * w * 0.5,
                      base + d * length]), SPIKE, bone, fuse=False, surface="fur")


def baby(k):
    # A pear of a body on short legs, broad feet with two white claws each
    k.add("bbody", ell(BB, (0.1, 0.078, 0.115)), BABY, "baby")
    k.add("bneck", tube([BB + (0, -0.01, 0.08), BH + (0, 0.02, -0.06)], [0.07, 0.065]), BABY, "baby")
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        k.add(f"bthigh{'R' if s > 0 else 'L'}", ell(np.array((0.068, -0.325, 0.085)) * m, (0.052, 0.058, 0.06)),
              BABY, "baby")
        k.add(f"bfoot{'R' if s > 0 else 'L'}", ell(np.array((0.072, -0.36, 0.025)) * m, (0.046, 0.06, 0.026)),
              BABY, "baby")
        for j, dx in enumerate((-0.02, 0.02)):
            claw(k, f"btoe{'R' if s > 0 else 'L'}{j}", np.array((0.072 + dx, -0.41, 0.02)) * m,
                 (0.2 * s * np.sign(dx), -1, -0.2), 0.028, 0.011, "baby")
    # Short arms bent forward, one raised a little higher (the art's fighting stance), white claws
    for s, paw in ((1, (0.135, -0.43, 0.265)), (-1, (-0.13, -0.43, 0.205))):
        side = "R" if s > 0 else "L"
        sh = np.array((0.08 * s, -0.34, 0.235))
        el = np.array((0.125 * s, -0.375, 0.215))
        k.add(f"barm{side}", tube([sh, el, paw], [0.035, 0.03, 0.03]), BABY, "baby")
        k.add(f"bpaw{side}", ell(paw, (0.033, 0.032, 0.032)), BABY, "baby")
        for j, dx in enumerate((-0.017, 0.017)):
            claw(k, f"bclaw{side}{j}", np.array(paw) + (dx, -0.028, 0.012), (0.25 * np.sign(dx), -1, 0.3), 0.026,
                 0.01, "baby")
    # A short tail out behind to the +X side; lilac spikes down the back and the tail
    k.add("btail", tube([BB + (0, 0.06, -0.06), (0.06, -0.235, 0.06), (0.1, -0.235, 0.035)], [0.04, 0.028, 0.012]),
          BABY, "baby")
    for j, (p, d) in enumerate((((0.0, -0.27, 0.26), (0, 0.7, 0.55)), ((0.0, -0.26, 0.19), (0, 1, 0.2)),
                                ((0.03, -0.245, 0.09), (0.3, 0.9, 0.25)), ((0.07, -0.225, 0.065), (0.4, 0.7, 0.6)))):
        spike(k, f"bspike{j}", p, d, 0.045 if j < 2 else 0.035, 0.016)

    # The head: big and round, a short snout, the helmet's grey over its crown
    k.add("bhead", ell(BH, BHR), BABY, "baby")
    k.add("bsnout", ell(BH + (0, -0.07, -0.03), (0.06, 0.055, 0.045), deform=boxy(2.2)), BABY, "baby")
    k.add("bcheek", ell(BH + (0.045, -0.04, -0.035), (0.045, 0.05, 0.04)), BABY, "baby", mirror=True)

    def cap(P):
        r = (P - BH) / (BHR + 0.018)
        near = inside((r ** 2).sum(1) - 1, 0.02)
        return near * inside(BH[2] + 0.025 - 0.15 * np.clip(P[:, 1] - BH[1], 0, 1) - P[:, 2], 0.003)
    k.paint(cap, HELM, on=["bhead"])
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"bear{side}", outward(leaf(BH + (0.065 * s, 0.015, 0.045), (0.6 * s, 0.25, 0.75), 0.1, 0.046,
                                         thick=0.026)), EAR, "baby", fuse=False, surface="fur")

    # The belly: pale yellow, three lilac hexagonal plates on it
    def bbelly(P):
        x, z = P[:, 0], P[:, 2]
        return inside((x / 0.075) ** 2 + ((z - BB[2] + 0.01) / 0.095) ** 2 - 1, 0.004) * (P[:, 1] < BB[1] - 0.02)
    k.paint(bbelly, BELLY, on=["bbody"])
    hexa = polygon([(np.cos(np.radians(30 + 60 * i)), np.sin(np.radians(30 + 60 * i))) for i in range(6)])
    for j, (dx, dz, r) in enumerate(((-0.018, 0.04, 0.026), (0.038, 0.0, 0.019), (-0.03, -0.035, 0.019))):
        n = unit(dx * 4, -1, dz * 3)
        k.decal(BB + np.array((dx, -0.076, dz)), n, (r, r), PLATE, eps=0.004, outline=hexa, rings=6, segs=36)

    # The mouth: one open pink shape under the snout with a white fang; pink cheeks
    k.decal(BH + np.array((0, -0.118, -0.058)), unit(0, -1, -0.45), (0.04, 0.022), MOUTH, eps=0.004,
            outline=polygon([(-1, 0.6), (1, 0.6), (0.6, -0.5), (0, -1), (-0.6, -0.5)]), rings=10, segs=44)
    k.decal(BH + np.array((0.018, -0.124, -0.05)), unit(0, -1, -0.45), (0.009, 0.011), CLAW, eps=0.0065,
            outline=polygon([(-1, 1), (1, 1), (0, -1)]), rings=4, segs=24)
    k.decal(BH + np.array((0.083, -0.07, -0.035)), unit(0.75, -0.65, -0.1), (0.016, 0.012), BLUSH, eps=0.004,
            rings=8, segs=36, mirror=True)


CB, CW, CR, CK = srgb(BROWN), srgb("#F6F2EE"), srgb("#D8343A"), srgb(INK)
OUT = [(-1.0, -0.15), (-0.6, 0.55), (0.6, 0.62), (1.0, 0.3), (0.85, -0.3), (0.2, -0.75), (-0.5, -0.65)]
BOUT = [(-1.0, -0.25), (-0.72, 0.62), (0.72, 0.62), (1.0, -0.25), (0.55, -0.8), (-0.55, -0.8)]   # symmetric


def open_eye(u, v):
    c = np.tile(CW, (len(u), 1))
    c[(u / 0.72) ** 2 + (v / 0.8) ** 2 < 1] = CR
    c[((u + 0.08) / 0.24) ** 2 + ((v + 0.05) / 0.36) ** 2 < 1] = CK
    c[((u - 0.12) / 0.14) ** 2 + ((v - 0.2) / 0.16) ** 2 < 1] = (1, 1, 1)
    return c


def baby_eye(u, v):
    c = np.tile(CW, (len(u), 1))
    c[((u + 0.05) / 0.66) ** 2 + (v / 0.8) ** 2 < 1] = CR
    c[((u + 0.12) / 0.3) ** 2 + ((v + 0.05) / 0.42) ** 2 < 1] = CK
    c[((u - 0.1) / 0.17) ** 2 + ((v - 0.25) / 0.19) ** 2 < 1] = (1, 1, 1)
    return c


def eyes(k, state):
    en = unit(0.58, -0.78, 0.12)
    E = H + en * 0.099 + np.array([0, 0, 0.004])
    bn = unit(0.5, -0.85, 0.1)
    BE = BH + bn * 0.097 + np.array([0, 0, 0.004])
    look = eye_look(state, open_eye, CB, CK, line=0.18)
    blook = eye_look(state, baby_eye, srgb(BABY), CK, line=0.18)
    for s in (1, -1):
        col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
        k.decal(E * np.array([s, 1, 1]), en * np.array([s, 1, 1]), (0.034, 0.027), col, up=(0, 0.1, 1), eps=0.005,
                outline=polygon([(u * s, v) for u, v in OUT]), rings=20, segs=56)
    k.decal(BE, bn, (0.029, 0.024), blook, up=(0, 0.1, 1), eps=0.005, outline=polygon(BOUT), rings=20, segs=56,
            mirror=True)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    mother(k)
    baby(k)
    k.build(voxel=0.0078, smooth=4)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
