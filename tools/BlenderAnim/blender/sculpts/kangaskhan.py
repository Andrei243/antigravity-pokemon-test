# Kangaskhan sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/kangaskhan.py -- <the export's Kangaskhan.glb> <out.glb>
# From the references: a heavy brown beast standing like a biped reptile (thighs one with a leaning pear of a body,
# short shins, broad feet with white claws), its head carried forward: a long snout, the mouth open and pink with
# two white fangs, red eyes under a dark grey plate that covers the head like a helmet, great cream ears swept back
# from the plate's sides; a cream belly whose lower half is a pouch, a small lilac baby looking out of it with its own
# grey cap and dark eyes; thick arms forward with three white claws a paw and angular plates on the shoulders and
# knees; small plates down the back; a thick tail lying on the ground and curving out to one side.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, leaf, outward, polygon, srgb, inside, smoothstep, unit, boxy, eye_look, args

BROWN, CREAM, SHADE, HELM, EAR = "#A87C5E", "#E8DA9C", "#9C8A52", "#5A5652", "#ECD98A"
BABY, MOUTH, CLAW, INK = "#ABA5D4", "#D87884", "#F2F0EA", "#1C1416"
H = np.array([0.0, -0.11, 0.86])                   # the head's centre (the head bone is at 0, -0.06, 0.78)
HR = np.array([0.115, 0.112, 0.102])
BC = np.array([0.0, -0.215, 0.6])                  # the baby's head (its bone is at 0, -0.18, 0.5)


def claw(k, name, base, d, length, width, bone):
    """A white claw: a flat-faced wedge from base along d."""
    base, d = np.array(base, float), unit(*d)
    a = np.cross(d, (0, 0, 1.0)); a /= np.linalg.norm(a)
    b = np.cross(a, d)
    k.add(name, hull([base + a * width + b * width * 0.8, base - a * width + b * width * 0.8,
                      base + a * width - b * width * 0.6, base - a * width - b * width * 0.6,
                      base - d * width, base + d * length - b * width * 0.2]), CLAW, bone, fuse=False, surface="shell")


def plate(k, name, c, out, up, w, h, t, bone):
    """An angular plate of the hide, a flat-faced slab standing a little proud of the body."""
    c, out, up = np.array(c, float), unit(*out), unit(*up)
    side = np.cross(up, out); side /= np.linalg.norm(side)
    up = np.cross(out, side)
    P = []
    for sx, sz, depth in ((-1, -1, 0), (1, -1, 0), (1, 0.6, 0), (0.3, 1, 0), (-1, 0.7, 0),
                          (-0.8, -0.8, 1), (0.8, -0.8, 1), (0.8, 0.5, 1), (0.25, 0.8, 1), (-0.8, 0.55, 1)):
        P.append(c + side * sx * w + up * sz * h + out * (depth * t - t * 0.6))
    k.add(name, hull(P), BROWN, bone, fuse=False, surface="fur")


def kangaskhan(src, out):
    k = Kit(src, coat="fur")

    # The body: a heavy pear, leaning a little forward, the thighs one with it
    def pear(V):
        V = V.copy()
        low = np.clip(-V[:, 2], 0, 1)
        V[:, :2] *= (1 + 0.12 * low)[:, None]
        return V
    k.add("torso", ell((0, 0.03, 0.5), (0.235, 0.2, 0.29), rot=(5, 0, 0), deform=pear), BROWN, "body")
    k.add("chest", ell((0, -0.03, 0.64), (0.2, 0.16, 0.13)), BROWN, "body")
    k.add("hips", ell((0, 0.05, 0.31), (0.22, 0.18, 0.13)), BROWN, "body")
    k.add("neck", tube([(0, -0.01, 0.66), (0, -0.06, 0.76), (0, -0.09, 0.82)], [0.13, 0.11, 0.09]), BROWN, "head")

    # The legs: thighs fused into the hips, short shins, broad feet
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

    # The arms: thick, forward and a little down, a paw with three white claws, a plate on each shoulder
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

    # The tail: thick at the hips, lying back along the ground and curving out to the model's +X side
    TAIL = [(0, 0.16, 0.36), (0.03, 0.31, 0.21), (0.11, 0.44, 0.1), (0.24, 0.51, 0.055), (0.36, 0.5, 0.04)]
    k.add("tail", tube(TAIL, [0.12, 0.1, 0.075, 0.05, 0.018]), BROWN, "tail")

    # Small plates down the back
    for j, (y, z) in enumerate(((0.18, 0.66), (0.22, 0.55), (0.23, 0.44))):
        plate(k, f"back{j}", (0, y, z), (0, 1, 0.1), (0, 0, 1), 0.035, 0.035, 0.03, "body")

    # The head: a broad crown, a long snout over a dropped jaw, the mouth open and pink, two white fangs
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

    # The helmet: dark grey over the crown from just above the eyes to the back of the head
    def helmet(P):
        r = (P - H) / np.array([0.13, 0.14, 0.13])
        near = inside((r ** 2).sum(1) - 1, 0.02)
        return near * inside(H[2] + 0.03 - 0.12 * np.clip(P[:, 1] - H[1], 0, 1) - P[:, 2], 0.003)
    k.paint(helmet, HELM, on=["cranium", "brow", "neck"])

    # The ears: great cream blades swept back and out from the helmet's sides
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"ear{side}", outward(leaf(H + (0.08 * s, 0.03, 0.045), (0.55 * s, 0.85, 0.2), 0.165, 0.072,
                                        thick=0.03)), EAR, "head", fuse=False, surface="fur")

    # The belly, cream from the chest down; its lower half a pouch with a lip, the opening shaded
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

    # The baby: a small lilac head looking out over the lip, a grey cap, a small ear, two tiny paws on the lip
    k.add("babyhead", ell(BC, (0.066, 0.06, 0.06)), BABY, "baby")
    k.add("babysnout", ell(BC + (0, -0.04, -0.01), (0.035, 0.03, 0.028)), BABY, "baby")
    k.add("babyear", ell(BC + (0.045, 0.01, 0.035), (0.014, 0.02, 0.026), rot=(-20, 30, 0)), EAR, "baby", mirror=True)
    k.add("babypaw", ell((0.05, -0.25, 0.555), (0.022, 0.02, 0.016)), BABY, "baby", mirror=True)
    k.paint(lambda P: inside(((P - BC) ** 2).sum(1) - 0.075 ** 2, 0.004) * inside(BC[2] + 0.025 - P[:, 2], 0.003),
            HELM, on=["babyhead"])

    # The faces: the mother's red eyes cut flat across the top by the helmet's edge; the baby's two dark eyes
    CB, CW, CR, CK = srgb(BROWN), srgb("#F6F2EE"), srgb("#D8343A"), srgb(INK)
    OUT = [(-1.0, -0.15), (-0.6, 0.55), (0.6, 0.62), (1.0, 0.3), (0.85, -0.3), (0.2, -0.75), (-0.5, -0.65)]
    def open_eye(u, v):
        c = np.tile(CW, (len(u), 1))
        c[((u + 0.0) / 0.72) ** 2 + ((v + 0.0) / 0.8) ** 2 < 1] = CR
        c[((u + 0.08) / 0.24) ** 2 + ((v + 0.05) / 0.36) ** 2 < 1] = CK
        c[((u - 0.12) / 0.14) ** 2 + ((v - 0.2) / 0.16) ** 2 < 1] = (1, 1, 1)
        return c
    en = unit(0.58, -0.78, 0.12)
    E = H + en * 0.099 + np.array([0, 0, 0.004])
    BB, BK = srgb(BABY), srgb("#2A2238")
    bn = unit(0.45, -0.88, 0.1)
    def baby_eye(u, v):
        c = np.tile(BK, (len(u), 1))
        c[((u - 0.25) / 0.3) ** 2 + ((v - 0.3) / 0.3) ** 2 < 1] = (1, 1, 1)
        return c
    def lay(state):
        look = eye_look(state, open_eye, CB, CK, line=0.18)
        for s in (1, -1):
            shape = polygon([(u * s, v) for u, v in OUT])
            col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
            k.decal(E * np.array([s, 1, 1]), en * np.array([s, 1, 1]), (0.034, 0.027), col, up=(0, 0.1, 1),
                    eps=0.005, outline=shape, rings=20, segs=56)
        k.decal(BC + bn * 0.06 + np.array([0.0, 0, 0.006]), bn, (0.012, 0.014),
                eye_look(state, baby_eye, BB, BK, line=0.2), eps=0.004, rings=20, segs=56, mirror=True)

    k.build(voxel=0.0078, smooth=4)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    kangaskhan(*args())
