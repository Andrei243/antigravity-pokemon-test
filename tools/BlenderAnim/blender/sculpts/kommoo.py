# Kommo-o sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/kommoo.py -- <the export's Kommo-o.glb> <out.glb>
# Hakamo-o grown up (built on jangmoo.py's and hakamoo.py's frame through sibling): from the references a big grey
# dragon leaning forward, its chest out, the head carried low in front on a thick neck. The head has a pale grey
# hooked muzzle with a pair of small white tusks, the line's red eye under a black brow, a red-middled yellow scale on
# its crown and one on each cheek, and a mane of grey scales edged yellow hanging from the back of the skull down the
# neck, a column of them down the front of the neck. A spiky white ruff at the base of the neck. Grey arms, thick and
# long, reaching forward and down nearly to the ground, edged with yellow-rimmed scales, a great red-middled shield on
# each forearm and yellow claws. Grey haunches with scales over long black shins, black feet with yellow claws, a pale
# patch under the belly. A row of grey bumps down the back and a long grey tail curving up behind to a great rattle of
# yellow-edged scales, as large as the head.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, polygon, eye_look, sibling, args

jm = sibling("jangmoo")
hk = sibling("hakamoo")
GREY, DARK, YELLOW, PALEYELLOW, RED = "#A9AAB0", "#2E2C32", jm.YELLOW, jm.PALEYELLOW, jm.RED
WHITE_FUR, INK = jm.WHITE_FUR, jm.INK
PALE = "#DCDCE0"
claw, oriented, scale, on_ell, shield = jm.claw, jm.oriented, jm.scale, jm.on_ell, hk.shield


def kscale(k, name, c, n, d, size, bone):
    """Kommo-o's scale: grey with a yellow rim all round, the rim widest at its free end."""
    scale(k, name, c, n, d, size, bone, inner=GREY, rim=YELLOW, rim_share=0.08, thick=0.3, inner_share=0.66)


def kommoo(src, out):
    k = Kit(src, coat="scales")

    # --- the trunk: leaning forward, the chest out and high in front, the hips behind ------------------------------
    k.add("chest", ell((0, -0.07, 0.5), (0.11, 0.105, 0.1), rot=(-25, 0, 0)), GREY, "body")
    k.add("belly", ell((0, 0.03, 0.42), (0.095, 0.12, 0.085), rot=(-15, 0, 0)), GREY, "body")
    k.add("hips", ell((0, 0.1, 0.36), (0.1, 0.09, 0.08)), GREY, "body")
    # the pale patch under the belly
    k.paint(lambda P: inside(P[:, 2] - 0.345, 0.004) * inside(np.abs(P[:, 0]) - 0.06, 0.004)
            * inside(P[:, 1] - 0.08, 0.004) * inside(-0.02 - P[:, 1], 0.004), PALE, on=["belly"])
    # the row of bumps down the back
    for j, (y, z) in enumerate(((0.0, 0.6), (0.06, 0.55), (0.12, 0.49), (0.18, 0.44), (0.25, 0.41))):
        k.add(f"bump{j}", ell((0, y, z), (0.03, 0.035, 0.022), rot=(-30, 0, 0)), GREY, "body")
    # the flanks' scales
    for j, (y, z) in enumerate(((-0.05, 0.47), (0.03, 0.44), (0.1, 0.41), (0.0, 0.52))):
        for s, side in ((1, "R"), (-1, "L")):
            m = np.array([s, 1, 1.0])
            kscale(k, f"flank{j}{side}", np.array((0.1, y, z)) * m, unit(s, 0, 0.35), (0, 0.6, -1), 0.04, "body")

    # --- the neck: thick, forward and up to the head, a column of scales down its front ---------------------------
    k.add("neck", tube([(0, -0.08, 0.53), (0, -0.13, 0.6), (0, -0.16, 0.66)], [0.075, 0.065, 0.06]), GREY, "head")
    for j, (y, z) in enumerate(((-0.205, 0.6), (-0.175, 0.545), (-0.14, 0.49))):
        kscale(k, f"throat{j}", (0, y, z), unit(0, -1, 0.3), (0, 0.2, -1), 0.04, "head" if j == 0 else "body")
    # the white ruff at the base of the neck: points out and down, round the front and sides
    for j, a in enumerate(np.linspace(-110, 110, 9)):
        r = math.radians(a)
        d = unit(math.sin(r), -math.cos(r) * 0.9, -0.45)
        b = np.array((0.08 * math.sin(r), -0.09 - 0.07 * math.cos(r), 0.52))
        k.add(f"ruff{j}", claw(b, d, 0.07 if j % 2 else 0.055, 0.045, up=(0, 0, 1), flat=0.35), WHITE_FUR, "body",
              fuse=False, surface="fur")

    # --- the legs: grey haunches with a scale, long black shins, black feet, yellow claws -------------------------
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array((0.1, 0.06, 0.28)) * m, (0.075, 0.095, 0.11), rot=(-20, 0, 0)), GREY,
              f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.11, 0.11, 0.2), (0.115, 0.125, 0.12), (0.115, 0.08, 0.04)]) * m,
                                  [0.045, 0.04, 0.036]), DARK, f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.115, 0.03, 0.024)) * m, (0.045, 0.075, 0.025)), DARK, f"leg{side}")
        for j, dx in enumerate((-0.024, 0.0, 0.024)):
            k.add(f"toe{side}{j}", claw(np.array((0.115 + dx, -0.04, 0.02)) * m, unit(dx * 6 * s, -1, -0.2), 0.04,
                                        0.024), YELLOW, f"leg{side}", fuse=False, surface="shell")
        kscale(k, f"thighscale{side}", np.array((0.17, 0.04, 0.3)) * m, unit(s, -0.2, 0.2), (0, 0.4, -1), 0.045,
               f"leg{side}")
    k.paint(lambda P: inside(P[:, 2] - 0.19, 0.004), DARK, on=["thigh"])

    # --- the arms: thick and long, forward and down nearly to the ground ------------------------------------------
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        sh, el, wr = np.array((0.11, -0.06, 0.53)), np.array((0.16, -0.13, 0.37)), np.array((0.135, -0.25, 0.19))
        k.add(f"shoulder{side}", ell(sh * m, (0.06, 0.06, 0.06)), GREY, f"arm{side}")
        k.add(f"upper{side}", tube([sh * m, el * m], [0.05, 0.047]), GREY, f"arm{side}")
        k.add(f"fore{side}", tube([el * m, ((el + wr) / 2) * m, wr * m], [0.048, 0.052, 0.048]), GREY, f"arm{side}")
        ha = wr + np.array((0.0, -0.02, -0.045))
        k.add(f"hand{side}", ell(ha * m, (0.05, 0.05, 0.045)), GREY, f"arm{side}")
        for j, (dx, dz) in enumerate(((-0.025, 0.008), (0.0, -0.008), (0.025, 0.006))):
            b = ha + np.array((dx, -0.045, dz - 0.01))
            k.add(f"claw{side}{j}", claw(b * m, unit(dx * 5 * s, -0.7, -0.7), 0.045, 0.026, up=(0, 0.7, 1)), YELLOW,
                  f"arm{side}", fuse=False, surface="shell")
        # scales down the outside of the arm, a great shield on the forearm
        kscale(k, f"pauldron{side}", (sh + (0.045, 0.0, 0.03)) * m, unit(0.75 * s, -0.1, 0.65), (s * 0.6, 0, -0.8),
               0.05, f"arm{side}")
        kscale(k, f"upperscale{side}", ((sh + el) / 2 + (0.045, 0.01, 0.0)) * m, unit(s, 0.1, 0.1), (0, 0.3, -1), 0.04,
               f"arm{side}")
        shield(k, f"shield{side}", ((el + wr) / 2 + (0.05, -0.01, 0.0)) * m, unit(0.8 * s, -0.6, 0.0),
               (0, -0.6, -1), 0.085, f"arm{side}")
        kscale(k, f"wristscale{side}", (wr + (0.035, -0.01, 0.01)) * m, unit(0.8 * s, -0.5, 0.2), (0, -0.5, -1),
               0.035, f"arm{side}")

    # --- the tail: long, curving up behind to a great rattle of scales ---------------------------------------------
    tp = [(0, 0.15, 0.35), (0, 0.27, 0.31), (0, 0.37, 0.35), (0, 0.42, 0.45), (0, 0.41, 0.55), (0, 0.39, 0.6)]
    k.add("tail", tube(tp, [0.062, 0.05, 0.042, 0.035, 0.03, 0.026]), GREY, "tail")
    for j, (y, z) in enumerate(((0.32, 0.36), (0.39, 0.4))):
        k.add(f"tailbump{j}", ell((0, y, z), (0.022, 0.03, 0.02), rot=(-40 - 30 * j, 0, 0)), GREY, "tail")
    RC = np.array([0.0, 0.39, 0.655])
    k.add("rattlecore", ell(RC, (0.05, 0.05, 0.05)), GREY, "tail")
    rs = np.random.RandomState(784)
    pts = [(0, 1, 0.0)]
    for el_, n in ((45, 4), (-10, 5)):
        for j in range(n):
            az = math.radians(360 * (j + 0.5 * (el_ == 0)) / n + rs.uniform(-10, 10))
            e = math.radians(el_)
            pts.append((math.cos(e) * math.sin(az), math.sin(e), math.cos(e) * math.cos(az)))
    for j, (a, b_, c_) in enumerate(pts):
        d = unit(a, -c_, b_)          # x, y, z: round the tip
        if d[2] < -0.75:
            continue
        kscale(k, f"rattle{j}", RC + d * 0.088, d, (0, 0, -1) if abs(d[2]) < 0.9 else (0, 1, 0), 0.08, "tail")

    # --- the head: carried low in front, a hooked pale muzzle forward -------------------------------------------
    H, HR = np.array([0.0, -0.2, 0.7]), np.array([0.078, 0.088, 0.075])
    k.add("head", ell(H, HR), GREY, "head")
    k.add("muzzle", ell((0, -0.275, 0.69), (0.05, 0.065, 0.045), rot=(-8, 0, 0)), PALE, "head")
    k.add("hook", tube([(0, -0.3, 0.715), (0, -0.335, 0.7), (0, -0.345, 0.675)], [0.03, 0.022, 0.014]), PALE, "head")
    k.add("jaw", ell((0, -0.26, 0.655), (0.045, 0.06, 0.025), rot=(-6, 0, 0)), PALE, "head")
    k.decal((0, -0.318, 0.672), unit(0, -1, -0.1), (0.028, 0.004), INK, eps=0.003, rings=4, segs=40)
    for s in (1, -1):
        k.decal(np.array((0.04 * s, -0.29, 0.668)), unit(0.8 * s, -0.6, -0.05), (0.02, 0.0035), INK, eps=0.003,
                rings=4, segs=32)
        k.add("tusk" + ("R" if s > 0 else "L"), claw((0.03 * s, -0.305, 0.66), unit(0.15 * s, -0.3, 1), 0.024, 0.016,
                                                      up=(0, -1, 0)), "#F6F6F2", "head", fuse=False, surface="shell")

    # the crown's red-middled scale and one on each cheek
    shield(k, "crown", (0, -0.235, 0.77), unit(0, -0.55, 0.85), (0, 1, 0.3), 0.06, "head")
    for s, side in ((1, "R"), (-1, "L")):
        shield(k, f"cheek{side}", (0.07 * s, -0.16, 0.675), unit(s, -0.25, -0.1), (0, 0.3, -1), 0.045, "head")
    # the mane: scales hanging from the back of the skull down the neck
    n = 0
    for el_, azs in ((25, (45, 100, 155)), (-25, (75, 130))):
        for az in azs:
            for s in ((1,) if az == 0 else (1, -1)):
                a, e = math.radians(az * s), math.radians(el_)
                d = np.array([math.cos(e) * math.sin(a), math.cos(e) * math.cos(a), math.sin(e)])
                c = H + np.array((0, 0.035, -0.02)) + d * np.array((HR[0] * 1.05, HR[1] * 1.0, HR[2] * 1.05))
                kscale(k, f"mane{n}", c, d, (0, 0.4, -1), 0.058, "head")
                n += 1

    # --- the eyes: the line's red eye under the black brow -----------------------------------------------------
    de = unit(0.6, -0.76, 0.15)
    E = on_ell(H, HR, de)
    cut = jm.brow_cut(0.45, -0.2)
    eyes = jm.eye_pair(k, E, de, (0.03, 0.026), jm.line_eye(0.45, -0.2), srgb(GREY), outline=cut, line=0.16,
                       eps=0.005)

    k.build(voxel=0.0072, smooth=4, band=0.004)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    kommoo(*args())
