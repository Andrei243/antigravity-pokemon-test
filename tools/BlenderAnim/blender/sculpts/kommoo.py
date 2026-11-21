# Kommo-o sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/kommoo.py -- <the export's Kommo-o.glb> <out.glb>
# Hakamo-o grown up (built on jangmoo.py's and hakamoo.py's frame through sibling): from the references a big grey
# dragon standing on two legs, its chest out, the head carried forward on a thick neck. The head, large with its mane,
# is the top mass: a pale grey hooked muzzle with a pair of small white tusks, the line's red eye under a black brow, a
# red-middled yellow scale on its crown and one on each cheek, and a mane of grey scales edged yellow hanging from the
# back of the skull down the neck, a column of them down the front of the neck. A spiky white ruff at the base of the
# neck. Thick grey arms held forward, a little out and down, elbows bent, the claws at knee height, edged with
# yellow-rimmed scales, a great red-middled shield on each forearm and yellow claws. Grey haunches with scales over long
# black shins, black feet with yellow claws, a pale patch under the belly. A row of grey bumps down the back and a long
# grey tail swinging back and out to its right, ending behind the rump at hip-to-shoulder height in a rattle of loose
# yellow-edged scales like the petals of a flower.
# (The game's shot of the first round read as a gorilla: the arms reached the ground, the head was small and the
# rattle stood beside the head like a second one. This round answers it.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, polygon, eye_look, axes, sibling, args

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

    # --- the trunk: standing, the chest out and high in front, the hips under it ----------------------------------
    k.add("chest", ell((0, -0.045, 0.53), (0.11, 0.1, 0.1), rot=(-12, 0, 0)), GREY, "body")
    k.add("belly", ell((0, 0.01, 0.44), (0.094, 0.1, 0.09), rot=(-8, 0, 0)), GREY, "body")
    k.add("hips", ell((0, 0.05, 0.35), (0.1, 0.09, 0.08)), GREY, "body")
    # the pale patch under the belly
    k.paint(lambda P: inside(P[:, 2] - 0.37, 0.004) * inside(np.abs(P[:, 0]) - 0.06, 0.004)
            * inside(P[:, 1] - 0.04, 0.004) * inside(-0.06 - P[:, 1], 0.004), PALE, on=["belly", "hips"])
    # the row of bumps down the back
    for j, (y, z) in enumerate(((0.05, 0.62), (0.085, 0.56), (0.105, 0.49), (0.12, 0.42), (0.14, 0.35))):
        k.add(f"bump{j}", ell((0, y, z), (0.03, 0.022, 0.035), rot=(-10, 0, 0)), GREY, "body")
    # the flanks' scales
    for j, (y, z) in enumerate(((-0.03, 0.49), (0.04, 0.45), (0.06, 0.38), (0.0, 0.56))):
        for s, side in ((1, "R"), (-1, "L")):
            m = np.array([s, 1, 1.0])
            kscale(k, f"flank{j}{side}", np.array((0.1, y, z)) * m, unit(s, 0, 0.3), (0, 0.4, -1), 0.04, "body")

    # --- the neck: thick, up and forward to the head, a column of scales down its front ---------------------------
    k.add("neck", tube([(0, -0.06, 0.58), (0, -0.1, 0.65), (0, -0.13, 0.71)], [0.075, 0.068, 0.064]), GREY, "head")
    for j, (y, z) in enumerate(((-0.165, 0.665), (-0.14, 0.61), (-0.115, 0.555))):
        kscale(k, f"throat{j}", (0, y, z), unit(0, -1, 0.25), (0, 0.2, -1), 0.042, "head" if j == 0 else "body")
    # the white ruff at the base of the neck: points out and down, round the front and sides
    for j, a in enumerate(np.linspace(-110, 110, 9)):
        r = math.radians(a)
        d = unit(math.sin(r), -math.cos(r) * 0.9, -0.45)
        b = np.array((0.08 * math.sin(r), -0.06 - 0.07 * math.cos(r), 0.56))
        k.add(f"ruff{j}", claw(b, d, 0.07 if j % 2 else 0.055, 0.045, up=(0, 0, 1), flat=0.35), WHITE_FUR, "body",
              fuse=False, surface="fur")

    # --- the legs: grey haunches with a scale, long black shins, black feet, yellow claws -------------------------
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array((0.1, 0.04, 0.28)) * m, (0.075, 0.095, 0.11), rot=(-20, 0, 0)), GREY,
              f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.11, 0.09, 0.2), (0.115, 0.105, 0.12), (0.115, 0.06, 0.04)]) * m,
                                  [0.045, 0.04, 0.036]), DARK, f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.115, 0.01, 0.024)) * m, (0.045, 0.075, 0.025)), DARK, f"leg{side}")
        for j, dx in enumerate((-0.024, 0.0, 0.024)):
            k.add(f"toe{side}{j}", claw(np.array((0.115 + dx, -0.06, 0.02)) * m, unit(dx * 6 * s, -1, -0.2), 0.04,
                                        0.024), YELLOW, f"leg{side}", fuse=False, surface="shell")
        kscale(k, f"thighscale{side}", np.array((0.17, 0.02, 0.3)) * m, unit(s, -0.2, 0.2), (0, 0.4, -1), 0.045,
               f"leg{side}")
    k.paint(lambda P: inside(P[:, 2] - 0.19, 0.004), DARK, on=["thigh"])

    # --- the arms: thick, forward, a little out and down, elbows bent, the claws at knee height --------------------
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        sh, el, wr = np.array((0.11, -0.04, 0.55)), np.array((0.165, -0.1, 0.43)), np.array((0.15, -0.215, 0.33))
        k.add(f"shoulder{side}", ell(sh * m, (0.06, 0.06, 0.06)), GREY, f"arm{side}")
        k.add(f"upper{side}", tube([sh * m, el * m], [0.05, 0.047]), GREY, f"arm{side}")
        k.add(f"fore{side}", tube([el * m, ((el + wr) / 2) * m, wr * m], [0.048, 0.052, 0.048]), GREY, f"arm{side}")
        ha = wr + np.array((0.0, -0.03, -0.035))
        k.add(f"hand{side}", ell(ha * m, (0.048, 0.048, 0.044)), GREY, f"arm{side}")
        for j, (dx, dz) in enumerate(((-0.025, 0.008), (0.0, -0.008), (0.025, 0.006))):
            b = ha + np.array((dx, -0.045, dz - 0.012))
            k.add(f"claw{side}{j}", claw(b * m, unit(dx * 5 * s, -0.7, -0.7), 0.045, 0.026, up=(0, 0.7, 1)), YELLOW,
                  f"arm{side}", fuse=False, surface="shell")
        # scales down the outside of the arm, a great shield on the forearm
        kscale(k, f"pauldron{side}", (sh + (0.045, 0.0, 0.03)) * m, unit(0.75 * s, -0.1, 0.65), (s * 0.6, 0, -0.8),
               0.05, f"arm{side}")
        kscale(k, f"upperscale{side}", ((sh + el) / 2 + (0.045, 0.0, 0.0)) * m, unit(s, -0.1, 0.1), (0, 0.3, -1), 0.04,
               f"arm{side}")
        shield(k, f"shield{side}", ((el + wr) / 2 + (0.05, -0.01, 0.0)) * m, unit(0.8 * s, -0.6, 0.0),
               (0, -0.8, -0.6), 0.08, f"arm{side}")
        kscale(k, f"wristscale{side}", (wr + (0.035, -0.01, 0.01)) * m, unit(0.8 * s, -0.5, 0.2), (0, -0.7, -0.7),
               0.035, f"arm{side}")

    # --- the tail: back and out to the right, rising to a rattle behind the rump at hip-to-shoulder height ---------
    tp = [(0, 0.1, 0.35), (0.03, 0.22, 0.3), (0.09, 0.33, 0.31), (0.15, 0.4, 0.36), (0.19, 0.43, 0.42),
          (0.205, 0.44, 0.45)]
    k.add("tail", tube(tp, [0.062, 0.05, 0.042, 0.035, 0.03, 0.026]), GREY, "tail")
    for j, p in enumerate(((0.035, 0.24, 0.345), (0.1, 0.34, 0.35))):
        k.add(f"tailbump{j}", ell(p, (0.022, 0.03, 0.02)), GREY, "tail")
    RC = np.array([0.215, 0.45, 0.48])
    A = unit(0.55, 0.75, 0.35)           # the flower's axis: back, out to the right and up, so both cameras see faces
    a1, a2 = axes(A)
    k.add("rattlecore", ell(RC, (0.04, 0.04, 0.04)), GREY, "tail")
    for j in range(7):
        t = 2 * math.pi * (j + 0.25) / 7
        radial = math.cos(t) * a1 + math.sin(t) * a2
        n = unit(*(A * 0.75 + radial * 0.65))
        kscale(k, f"petal{j}", RC + radial * 0.075 + A * 0.01, n, radial, 0.07, "tail")
    kscale(k, "petalmid", RC + A * 0.04, A, a2, 0.045, "tail")

    # --- the head: large with its mane, the top mass, a hooked pale muzzle forward ---------------------------------
    # Laid out at the first round's size and place, then moved up and back onto the upright neck (T) and grown a
    # third about the neck's top (J, G)
    J, G, T = np.array([0.0, -0.13, 0.72]), 1.33, np.array([0.0, 0.07, 0.04])
    def hp(p):
        return J + (np.array(p, float) + T - J) * G
    first, firstd = len(k.parts), len(k.decals)
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
    # the mane: scales hanging from the back of the skull down the neck, three rows
    n = 0
    for el_, azs, sz in ((25, (45, 100, 155), 0.06), (-20, (70, 120, 170), 0.062), (-55, (95, 150), 0.058)):
        for az in azs:
            for s in (1, -1):
                a, e = math.radians(az * s), math.radians(el_)
                d = np.array([math.cos(e) * math.sin(a), math.cos(e) * math.cos(a), math.sin(e)])
                c = H + np.array((0, 0.035, -0.02)) + d * np.array((HR[0] * 1.08, HR[1] * 1.02, HR[2] * 1.08))
                kscale(k, f"mane{n}", c, d, (0, 0.4, -1), sz, "head")
                n += 1
    for p in k.parts[first:]:
        p["V"] = J + (p["V"] + T - J) * G
    for dd in k.decals[firstd:]:
        dd["c"] = hp(dd["c"])
        dd["half"] = tuple(h * G for h in dd["half"])

    # --- the eyes: the line's red eye under the black brow -----------------------------------------------------
    de = unit(0.6, -0.76, 0.15)
    E = hp(on_ell(H, HR, de))
    cut = jm.brow_cut(0.45, -0.2)
    eyes = jm.eye_pair(k, E, de, (0.038, 0.033), jm.line_eye(0.45, -0.2), srgb(GREY), outline=cut, line=0.16,
                       eps=0.005)

    k.build(voxel=0.0075, smooth=4, band=0.004)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    kommoo(*args())
