# Zoroark-Hisui sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown
# from Zoroark's script (zoroark.py, imported: its head and muzzle, claws, face and the way of its eyes, moved onto a
# crouch of this form's own) and Zorua-Hisui's (zoruahisui.py: its colours and its wisps):
#   blender --background --factory-startup --python sculpts/zoroarkhisui.py -- <the export's Zoroark-Hisui.glb> <out.glb>
# From the references: a pale grey fox crouched far forward, the chest leaning some 45 degrees, the head carried low in
# front of it at the shoulders' height, the knees bent in a wide stance, the arms hanging forward and down with dark
# violet claws by the knees; red oval marks on the arms, the thighs and the shins; ears lined dark violet, a gold eye
# under a dusky mark. The design's largest mass is the mane: a great white flowing mass over the head, the shoulders
# and the back, wider than the body, whose lower third turns rose-red and breaks into many long red wavy locks reaching
# down and out past the hips on both sides; one long wisp rises from its top and arches back, red at its end.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, smoothstep, unit, lumpy, args, sibling

zo = sibling("zoroark")
zh = sibling("zoruahisui")
z_ = zo.z_

COAT = "#CDD2D9"
WHITE = zh.WHITE
RED = "#E0485A"             # the mane's lower third, the locks, the limbs' marks
LINING = zh.LINING
CLAW = "#4E3A56"
SHADE = "#5C4466"           # the dusky mark over the eye
GOLD = "#F2BE1A"

# --- the crouch (the skeleton's joints: body (0, 0, 0.44), arms (+-0.08, -0.02, 0.56), tail (0, 0.06, 0.64), head
#     (0, -0.04, 0.62), ears (+-0.035, -0.05, 0.72), legs (+-0.06, 0.02, 0.36)): the masses lie forward and low of
#     their joints; the head carried at the shoulders' height in front of the chest --------------------------------
H = np.array([0, -0.255, 0.475])            # the head's centre (Zoroark's was (0, -0.125, 0.638))
HIP = np.array([0.075, 0.04, 0.315])
KNEE = np.array([0.11, -0.075, 0.2])
ANKLE = np.array([0.112, 0.035, 0.075])
TOE = np.array([0.118, -0.05, 0.022])
SH = np.array([0.085, -0.135, 0.455])
EL = np.array([0.13, -0.165, 0.33])
WR = np.array([0.115, -0.235, 0.225])


def add_body(k):
    k.add("hips", ell((0, 0.04, 0.33), (0.075, 0.07, 0.06)), COAT, "body")
    k.add("waist", ell((0, -0.03, 0.385), (0.066, 0.062, 0.07), rot=(-45, 0, 0)), COAT, "body")
    k.add("chest", ell((0, -0.11, 0.43), (0.085, 0.08, 0.075), rot=(-45, 0, 0)), COAT, "body")
    k.add("neck", tube([(0, -0.13, 0.46), (0, -0.19, 0.47), (0, -0.22, 0.47)], [0.055, 0.05, 0.048]), COAT, "head")
    # the mane's red front under the jaw
    k.add("ruff", ell((0, -0.19, 0.43), (0.07, 0.05, 0.05), rot=(-30, 0, 0)), RED, "body")


def add_head(k):
    zo.H = H                            # Zoroark's head, muzzle, ears and face laid round this form's head
    HR, G = zo.HR, zo.G
    k.add("head", ell(H, HR, deform=z_.cheeky), COAT, "head")
    k.add("muzzle", ell(H + G * np.array([0, -0.08, -0.026]), np.array([0.036, 0.07, 0.032]) * G, rot=(-14, 0, 0),
                        deform=zo.taper), COAT, "head")
    for s in (1, -1):
        b = H + G * np.array([0.05 * s, 0.01, -0.03])
        k.add(f"cheek{'R' if s > 0 else 'L'}", blade([b, b + (0.02 * s, 0.025, -0.007), b + (0.034 * s, 0.05, -0.018)],
                                                     [0.02, 0.014, 0.002], (0, 0, 1), 0.5), COAT, "head")
    z_.add_ears(k, ear=[zo.g(p) for p in ((0.034, -0.068, 0.712), (0.044, -0.06, 0.75), (0.053, -0.05, 0.788),
                                          (0.06, -0.04, 0.822))],
                radii=[0.034, 0.03, 0.016, 0.003], color=COAT, lining=LINING, width=0.02)


def claws(k, at, down, fwd, bone, n=3, ln=0.045):
    for j, dx in enumerate((-0.02, 0.0, 0.02)):
        a = at + np.array([dx, 0, 0])
        tip = a + fwd * ln * 0.6 + down * ln + np.array([dx * 0.4, 0, 0])
        mid = a + fwd * ln * 0.5 + down * ln * 0.3
        k.add(f"claw{bone}{j}", hull([a + (0.007, 0.006, 0.004), a + (-0.007, 0.006, 0.004), a + (0, -0.006, 0.008),
                                      a + (0, 0.004, -0.008), mid + (0.004, 0, 0), mid + (-0.004, 0, 0), tip]),
              CLAW, bone, fuse=False, mirror=True, surface="shell")


def add_arms(k):
    k.add("upper", tube([SH, EL], [0.042, 0.04]), COAT, "arm{s}", mirror=True)
    k.add("fore", tube([EL, (EL + WR) / 2 + (0.008, 0, 0), WR], [0.042, 0.048, 0.04]), COAT, "arm{s}", mirror=True)
    HAND = WR + np.array([-0.004, -0.022, -0.02])
    k.add("hand", ell(HAND, (0.036, 0.038, 0.03)), COAT, "arm{s}", mirror=True)
    claws(k, HAND + (0, -0.026, -0.012), np.array([0, 0, -1.0]), np.array([0, -1.0, 0]), "arm{s}", ln=0.042)


def add_legs(k):
    # a wide stance, knees bent forward, great haunches fused into the hips
    k.add("thigh", ell((HIP + KNEE) / 2 + (0, 0, 0.01), (0.056, 0.07, 0.085), rot=(40, 0, -8)), COAT, "leg{s}", mirror=True)
    k.add("shin", tube([KNEE, ANKLE], [0.042, 0.032]), COAT, "leg{s}", mirror=True)
    k.add("foot", tube([ANKLE, (ANKLE + TOE) / 2 + (0, 0, -0.008), TOE], [0.03, 0.028, 0.026]), COAT, "leg{s}", mirror=True)
    k.add("toes", ell(TOE + (0, -0.01, -0.004), (0.036, 0.04, 0.02)), COAT, "leg{s}", mirror=True)
    claws(k, TOE + (0, -0.045, -0.002), np.array([0, 0, -0.5]), np.array([0, -1.0, 0]), "leg{s}", ln=0.034)


# --- the mane: a great flowing mass over the head, the shoulders and the back; its lower third red ----------------
MC = np.array([0, 0.0, 0.6])


def add_mane(k):
    k.add("mane", ell(MC, (0.2, 0.22, 0.135), rot=(-12, 0, 0), deform=lumpy(4, 2, 0.05)), WHITE, "tail")
    k.add("maneback", ell(MC + (0, 0.2, -0.08), (0.15, 0.15, 0.11), rot=(-30, 0, 0)), WHITE, "tail")
    # flowing on back and down behind like a tail of hair
    k.add("manetail", tube([MC + (0, 0.2, -0.04), MC + (0, 0.33, -0.12), MC + (0.04, 0.41, -0.17)], [0.11, 0.08, 0.045]),
          WHITE, "tail")
    k.add("maneside", ell(MC + (0.14, -0.04, -0.07), (0.09, 0.13, 0.1)), WHITE, "tail", mirror=True)
    k.add("manefront", ell(H + (0, 0.12, 0.075), (0.095, 0.1, 0.07)), WHITE, "head")
    # a lock hanging down by each cheek, behind the face
    k.add("lock", tube([H + (0.08, 0.05, 0.06), H + (0.105, 0.02, -0.01), H + (0.115, 0.0, -0.075)],
                       [0.055, 0.05, 0.036]), WHITE, "head", mirror=True)
    # the lower third red all round, eased up over some 0.05
    def low(P):
        edge = 0.56 - 0.25 * np.clip(P[:, 1], 0, 0.42)        # the red reaching higher round the face
        return smoothstep(edge + 0.03, edge - 0.03, P[:, 2])
    k.paint(low, RED, on=["mane", "lock"])
    # the wisp: up from the top of the mane and arching back, red at its end
    zh.wisp(k, "topwisp", [MC + (0, 0.0, 0.12), MC + (0, 0.06, 0.2), MC + (0.02, 0.16, 0.23),
                           MC + (0.06, 0.25, 0.2), MC + (0.1, 0.3, 0.15)],
            [0.045, 0.034, 0.025, 0.018, 0.014], "tail", start=0.5, end=0.8, reach=0.05)


# (root on the mane's red edge, end, the wave's direction, its size): long locks down and out past the hips
LOCKS = [((0.12, -0.2, 0.47), (0.2, -0.3, 0.24), (0, 1, 0.3), 0.03),
         ((0.18, -0.12, 0.5), (0.3, -0.2, 0.2), (0, 1, 0), 0.035),
         ((0.21, -0.02, 0.5), (0.36, -0.05, 0.25), (0, 1, 0), 0.035),
         ((0.2, 0.1, 0.47), (0.33, 0.18, 0.28), (0, 1, 0), 0.03),
         ((0.12, 0.26, 0.43), (0.2, 0.4, 0.3), (1, 0, 0), 0.03)]


def add_locks(k):
    for i, (a, b, side, amp) in enumerate(LOCKS):
        for s in (1, -1):
            m = np.array([s, 1, 1.0])
            a_, b_ = np.array(a) * m, np.array(b) * m
            pts = zh.wavy(a_, b_, amp, 2.3, np.array(side) * m, n=9)
            d = unit(*(b_ - a_))
            out_ = np.array([s * 1.0, 0, 0]) if abs(a[0]) > 0.1 else np.array([0, 1.0, 0])
            flat = out_ - d * (out_ @ d)                  # thin outward, so its broad face is turned outward
            k.add(f"lock{i}{'R' if s > 0 else 'L'}",
                  blade(pts, np.linspace(0.032, 0.012, len(pts)), flat, 0.5), RED, "tail")


# --- the red ovals on the limbs: long patches on each limb's outer side --------------------------------------------
def add_spots(k):
    spots = []
    n = unit(1, -0.2, 0.2); spots.append(((SH + EL) / 2 + n * 0.041, n, unit(*(SH - EL)), (0.014, 0.024)))
    n = unit(1, -0.3, 0); spots.append(((EL + WR) / 2 + (0.008, 0, 0) + n * 0.047, n, unit(*(EL - WR)), (0.016, 0.026)))
    T = (HIP + KNEE) / 2 + (0, 0, 0.01)
    n = unit(0.85, -0.5, 0.15); spots.append((T + n * 0.058, n, unit(0, -0.6, 0.8), (0.02, 0.034)))
    n = unit(0.45, -1, 0.1); spots.append(((KNEE + ANKLE) / 2 + n * 0.036, n, unit(*(KNEE - ANKLE)), (0.013, 0.022)))
    # painted, not patched: the locks hanging beside the limbs caught the patches' rays
    def spot(c, n, up, half):
        a = np.cross(up, n); a /= np.linalg.norm(a)
        b = np.cross(n, a)
        def m(P):
            Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
            d = Q - c
            r = np.hypot((d @ a) / half[0], (d @ b) / half[1])
            return inside(r - 1, 0.08) * inside(np.abs(d @ n) - 0.03, 0.005)
        return m
    for c, n, up, half in spots:
        k.paint(spot(c, n, up, half), RED, on=["upper", "fore", "thigh", "shin"])


def zoroark_hisui(src, out):
    k = Kit(src, coat="fur")
    zo.GREY, zo.RED = COAT, RED
    add_body(k)
    add_arms(k)
    add_legs(k)
    add_head(k)
    add_mane(k)
    add_locks(k)
    add_spots(k)
    zo.add_face(k)
    k.build(voxel=0.0064, band=0.004)
    k.eye_states(lambda s: z_.lay_eyes(k, s, H, zo.HR, x=0.034, dz=0.007, half=(0.023, 0.016), lid=COAT, red=SHADE,
                                       iris=GOLD, turn=1.35))
    k.export(out)


if __name__ == "__main__":
    zoroark_hisui(*args())
