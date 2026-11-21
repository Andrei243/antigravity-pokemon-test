# Zoroark-Hisui sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown
# from Zoroark's script (zoroark.py, imported: its hunched frame, head and muzzle, arms with their claws, haunches and
# feet, face and the way of its eyes) and Zorua-Hisui's (zoruahisui.py: its colours and its wisps):
#   blender --background --factory-startup --python sculpts/zoroarkhisui.py -- <the export's Zoroark-Hisui.glb> <out.glb>
# From the references: Zoroark's crouched build in a pale grey coat, red oval marks on the arms, the thighs and the
# shins, dark violet claws; ears lined dark violet, a gold eye under a dusky mark; the design's largest mass a great
# rounded mane of white hair over the head, the shoulders and the back, rose-red along its lower edge, from which red
# tendrils hang and writhe like flames, a lock hanging down by each cheek; one long wisp arching up and back from the
# top of the mane, red at its end.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, lumpy, args, sibling

zo = sibling("zoroark")
zh = sibling("zoruahisui")
z_ = zo.z_
H, HR, G, g = zo.H, zo.HR, zo.G, zo.g

COAT = "#CDD2D9"
WHITE = zh.WHITE
RED = "#E0485A"             # the mane's lower edge, the tendrils, the limbs' marks
LINING = zh.LINING
CLAW = "#4E3A56"
SHADE = "#5C4466"           # the dusky mark over the eye
GOLD = "#F2BE1A"


# --- the mane: the largest mass, a rounded cloud of white hair over the head, the shoulders and the back ------------
MC = np.array([0, 0.07, 0.74])


def add_mane(k):
    # swept up and back from the brow, a lobe falling behind over the back
    k.add("mane", ell(MC + (0, 0.06, 0.02), (0.185, 0.2, 0.15), rot=(-22, 0, 0), deform=lumpy(4, 2, 0.05)), WHITE, "tail")
    k.add("manecrown", ell(H + (0, 0.13, 0.06), (0.09, 0.08, 0.07)), WHITE, "head")
    k.add("maneback", ell(MC + (0, 0.19, -0.12), (0.14, 0.14, 0.12), rot=(-30, 0, 0)), WHITE, "tail")
    k.add("maneshoulder", ell(MC + (0.12, -0.01, -0.12), (0.085, 0.1, 0.09)), WHITE, "tail", mirror=True)
    # a lock hanging down by each cheek, behind the face, to the shoulder
    k.add("lock", tube([H + (0.085, 0.04, 0.07), H + (0.11, 0.02, -0.0), H + (0.12, 0.01, -0.07)],
                       [0.06, 0.055, 0.04]), WHITE, "head", mirror=True)
    # rose-red along the lower edge round the face and the shoulders (the front half), eased up over some 0.1
    def low(P):
        return smoothstep(0.69, 0.58, P[:, 2]) * smoothstep(0.16, 0.05, P[:, 1])
    k.paint(low, RED, on=["mane", "lock"])
    # the wisp: up from the top of the mane and arching back, red at its end
    zh.wisp(k, "topwisp", [MC + (0, -0.02, 0.13), MC + (0, 0.05, 0.19), MC + (0.02, 0.15, 0.21),
                           MC + (0.06, 0.24, 0.17), MC + (0.1, 0.28, 0.12)],
            [0.045, 0.033, 0.024, 0.018, 0.014], "tail", start=0.5, end=0.8, reach=0.05)


# (root on the mane's lower edge, end, the sideways wave's direction): the tendrils hanging and writhing out
TENDRILS = [((0.13, -0.03, 0.57), (0.27, -0.08, 0.42), (0, 0.3, 1), 0.024, 2.5),
            ((0.19, 0.05, 0.6), (0.3, 0.02, 0.45), (0, 0.5, 1), 0.026, 1.6),
            ((0.1, 0.27, 0.6), (0.21, 0.42, 0.6), (0, 0, 1), 0.03, 2.0)]


def add_tendrils(k):
    for i, (a, b, side, amp, waves) in enumerate(TENDRILS):
        for s in (1, -1):
            m = np.array([s, 1, 1.0])
            pts = zh.wavy(np.array(a) * m, np.array(b) * m, amp, waves, np.array(side) * m, n=8)
            k.add(f"tendril{i}{'R' if s > 0 else 'L'}", tube(pts, np.linspace(0.024, 0.014, len(pts))), RED, "tail")


# --- the red ovals on the limbs: long patches along each limb on its outer side -------------------------------------
def add_spots(k):
    SH, EL, WR = np.array([0.088, -0.05, 0.545]), np.array([0.135, -0.1, 0.455]), np.array([0.11, -0.18, 0.37])
    KN, AN = np.array([0.082, -0.075, 0.215]), np.array([0.088, 0.0, 0.1])
    spots = []
    n = unit(1, -0.3, 0.15); spots.append(((SH + EL) / 2 + n * 0.04, n, unit(*(EL - SH)), (0.014, 0.024)))
    n = unit(1, -0.25, 0); spots.append(((EL + WR) / 2 + (0.01, 0, 0) + n * 0.048, n, unit(*(EL - WR)), (0.017, 0.028)))
    n = unit(1, -0.15, 0); spots.append((np.array([0.124, -0.035, 0.33]), n, unit(0, -0.3, 1), (0.02, 0.036)))
    n = unit(0.35, -1, 0.25); spots.append(((KN + AN) / 2 + (0, 0, 0.01) + n * 0.035, n, unit(*(KN - AN)), (0.013, 0.024)))
    for c, n, up, half in spots:
        for s in (1, -1):
            m = np.array([s, 1, 1.0])
            k.decal(c * m, n * m, half, RED, up=up * m, eps=0.0035, rings=10, segs=40)


def zoroark_hisui(src, out):
    k = Kit(src, coat="fur")
    zo.GREY, zo.RUFF, zo.CLAW, zo.RED = COAT, RED, CLAW, RED
    zo.add_body(k)                       # Zoroark's ruff under the jaw is the mane's red front here
    zo.add_arms(k)
    zo.add_legs(k)
    zo.add_head(k)
    z_.add_ears(k, ear=[g(p) for p in ((0.034, -0.068, 0.712), (0.044, -0.06, 0.75), (0.053, -0.05, 0.788),
                                       (0.06, -0.04, 0.822))],
                radii=[0.034, 0.03, 0.016, 0.003], color=COAT, lining=LINING, width=0.02)
    add_mane(k)
    add_tendrils(k)
    add_spots(k)
    zo.add_face(k)
    k.build(voxel=0.0068, band=0.004)
    k.eye_states(lambda s: z_.lay_eyes(k, s, H, HR, x=0.034, dz=0.007, half=(0.023, 0.016), lid=COAT, red=SHADE,
                                       iris=GOLD, turn=1.35))
    k.export(out)


if __name__ == "__main__":
    zoroark_hisui(*args())
