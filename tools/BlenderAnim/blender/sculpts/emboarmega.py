# Emboar-Mega sculpted on the skeleton and clips of the game's own export of the form (sculptkit.py), grown from
# emboar.py (imported: its frame, its legs, arms, head, snout, tusks, tail, mouth and eyes, and its way of making a
# flame's tongue):
#   blender --background --factory-startup --python sculpts/emboarmega.py -- <the export's Emboar-Mega.glb> <out.glb>
# From the references, what the Mega changes, largest mass first: (1) a mantle of flame grown over the whole upper
# body: a tall sheet of flame up beside each side of the head to the crest's height, flames over the shoulders and a
# great beard of red flame with a yellow heart in a V hanging over the chest down to the belly; (2) a staff of fire
# held upright in the right hand (the viewer's right in the art), a zigzag of red with a yellow core, taller than
# Emboar, forked into two curls at its top; (3) the belly a broad yellow panel with black swirls, a skirt of red
# flame-fur hanging round the hips over the thighs; (4) the black crest grown into a great crown of two horns
# swept up and out over the head. Arms, hands, legs, face and tail are Emboar's.
# Reference ratios (3D picture, feet to the crest's top = 1): the side flames' top 1.03; the staff's top 1.25 (held
# to 1.17 here, so that the body stays large in the game's cell); the crest above the crown 0.12; the beard's foot
# 0.38; the yellow panel 0.22 to 0.52; the skirt's hem 0.14; the width over the mantle / height 0.75.
# The export's joints: body (0, 0, 0.42), arms (+-0.16, 0, 0.54), head (0, -0.04, 0.62), ears (+-0.07, -0.06, 0.73),
# legs (+-0.09, 0, 0.24), tail (0, 0.11, 0.3).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, boxy, args, outward, hull, sibling

em = sibling("emboar")

BLACK, ORANGE, YELLOW, RED = em.BLACK, em.ORANGE, em.YELLOW, em.RED
FLAME_RED, FLAME, FLAME_CORE = "#EC4A2C", "#F6782C", "#FFC83A"
PANEL = "#F4C23A"
H, BEL, BELR = em.H, em.BEL, em.BELR


def add_body(k):
    k.add("belly", ell(BEL, BELR, deform=boxy(2.3)), BLACK, "body")
    k.add("chest", ell((0, -0.015, 0.535), (0.205, 0.15, 0.075)), ORANGE, "body")
    k.add("yoke", ell((0, 0.01, 0.585), (0.15, 0.12, 0.06)), ORANGE, "body")
    k.add("hips", ell((0, 0.01, 0.215), (0.205, 0.165, 0.085)), RED, "body", surface="cloth")
    k.paint(lambda P: inside(0.497 - P[:, 2], 0.003) * (P[:, 2] < 0.64), ORANGE, on=["belly", "chest", "yoke"])
    k.paint(lambda P: inside(P[:, 2] - 0.497, 0.003) * (P[:, 2] > 0.3), BLACK, on=["belly", "chest"])
    # The yellow panel over the belly's front, black at its sides, and its swirls
    LO, HI = 0.255, 0.49

    def panel(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        a = np.arctan2(x, -(y - BEL[1]))
        return inside(LO - z, 0.003) * inside(z - HI, 0.003) * inside(np.abs(a) - 1.45, 0.01)
    k.paint(panel, PANEL, on=["belly"])

    def swirls(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        a = np.arctan2(x, -(y - BEL[1])) * 0.24          # along the belly, in metres
        zc = 0.5 * (LO + HI) - 0.005
        m = np.zeros(len(P))
        # a great coil at each side and a smaller pair low in the middle, curling inward
        for cx, cz, R, turn in ((0.2, zc + 0.02, 0.07, 1), (-0.2, zc + 0.02, 0.07, -1), (0.07, zc - 0.05, 0.045, -1),
                                (-0.07, zc - 0.05, 0.045, 1)):
            r = np.hypot(a - cx, z - cz)
            ang = np.arctan2(z - cz, (a - cx) * turn)
            pitch = R * 0.42
            sp = np.abs(((r - pitch * ((ang + math.pi) / (2 * math.pi))) + pitch / 2) % pitch - pitch / 2) - 0.007
            m = np.maximum(m, inside(sp, 0.0025) * inside(r - R, 0.003))
        # a black point rising up the middle from the hem
        m = np.maximum(m, inside(np.abs(a) - 0.03 * np.clip((zc - z) / 0.09, 0, 1), 0.003) * (z < zc - 0.01))
        return m * inside(LO + 0.004 - z, 0.002) * inside(z - HI + 0.004, 0.002)
    k.paint(swirls, BLACK, on=["belly"])


def add_skirt(k):
    """Red flame-fur hanging round the hips over the thighs: rows of solid points."""
    for row, (z, n, ln, w) in enumerate(((0.275, 16, 0.12, 0.04), (0.23, 13, 0.1, 0.036))):
        for i in range(n):
            a = 2 * math.pi * (i + 0.5 * row) / n
            c, s_ = math.cos(a), math.sin(a)
            p = np.array([(BELR[0] + 0.005) * s_ * 0.93, BEL[1] - (BELR[1] + 0.005) * c * 0.93, z])
            out_ = unit(s_ / BELR[0], -c / BELR[1], 0)
            jit = 0.02 * math.sin(5.3 * i + row)
            tip = p + out_ * (0.04 + jit) + np.array([0, 0, -(ln + jit)])
            side = np.cross(out_, (0, 0, 1.0))
            k.add(f"skirt{row}_{i}", hull([p - out_ * 0.025 + side * w, p - out_ * 0.025 - side * w,
                                           p + out_ * 0.012 + side * w * 0.7, p + out_ * 0.012 - side * w * 0.7,
                                           p + (0, 0, 0.02), tip]),
                  RED if row == 0 else FLAME_RED, "body", fuse=False, surface="fur")


def add_crown(k):
    """The black crest grown into a crown: two great horns swept up and out from the crown, a notch between."""
    for side, s in (("R", 1), ("L", -1)):
        b = H + np.array((0.03 * s, -0.04, 0.07))
        pts = [b, b + (0.03 * s, 0.0, 0.06), b + (0.075 * s, 0.01, 0.12), b + (0.125 * s, 0.02, 0.16),
               b + (0.16 * s, 0.025, 0.17)]
        k.add("horn" + side, blade(pts, [0.04, 0.038, 0.03, 0.02, 0.008], (0, 1, 0), 0.5), BLACK, "ear" + side)
        q = b + np.array((0.05 * s, 0.0, 0.06))
        k.add("hornb" + side, blade([q, q + (0.05 * s, 0.01, 0.0), q + (0.09 * s, 0.015, -0.025)],
                                    [0.026, 0.018, 0.007], (0, 1, 0), 0.5), BLACK, "ear" + side)
    k.add("crownbase", ell(H + (0, -0.03, 0.085), (0.06, 0.05, 0.04)), BLACK, "head")


def add_mantle(k):
    """The mantle of flame: sheets up beside the head, flames over the shoulders and the beard over the chest."""
    red, mid, core = em.Pieces(), em.Pieces(), em.Pieces()
    tongue = em.tongue
    for s in (1, -1):
        # the tall sheets beside the head, each of three tongues fanned, the tallest outermost
        b = np.array((0.15 * s, 0.0, 0.58))
        red.put(tongue(b, (0.32 * s, 0.1, 1.0), 0.33, 0.075, depth=0.05, bend=0.05))
        red.put(tongue(b + (-0.04 * s, -0.02, 0.03), (0.08 * s, 0.05, 1.0), 0.26, 0.06, depth=0.045))
        mid.put(tongue(b + (0.04 * s, -0.01, 0.0), (0.7 * s, 0.05, 1.0), 0.2, 0.055, depth=0.04, bend=0.25))
        core.put(tongue(b + (0.0, -0.045, 0.02), (0.3 * s, 0.0, 1.0), 0.2, 0.04, depth=0.03))
        core.put(tongue(b + (-0.03 * s, -0.05, 0.02), (0.05 * s, 0.0, 1.0), 0.14, 0.03, depth=0.025))
        # flames over the shoulder, out and down over the arm's top
        for j, (dx, dz) in enumerate(((0.06, 0.0), (0.1, -0.03))):
            sb = np.array(((0.16 + dx) * s, -0.02, 0.6 + dz))
            (red if j == 0 else mid).put(tongue(sb, (0.9 * s, -0.2, 0.1), 0.13, 0.05, bend=0.3))
    # The beard: red tongues hanging from a ring round the neck down over the chest, a yellow V inside
    n = 8
    for i in range(n):
        f = i / (n - 1) - 0.5
        x = 0.38 * f
        base = np.array([x, -0.13 + 0.12 * abs(f) ** 1.5, 0.635])
        ln = 0.27 - 0.17 * abs(f)
        red.put(tongue(base, (x * 1.6, -0.42, -1.0), ln, 0.07, depth=0.035))
    for i in range(5):
        f = i / 4 - 0.5
        x = 0.24 * f
        base = np.array([x, -0.165 + 0.05 * abs(f), 0.62])
        mid.put(tongue(base, (x * 1.4, -0.4, -1.0), 0.21 - 0.08 * abs(f), 0.06, depth=0.03))
    # the yellow heart: three broad tongues meeting low in a V at the middle
    for i, (x, ln) in enumerate(((-0.06, 0.15), (0.0, 0.2), (0.06, 0.15))):
        core.put(tongue((x, -0.19, 0.605), (-x * 1.5, -0.42, -1.0), ln, 0.042, depth=0.024))
    # the mantle seen from behind: tongues up over the shoulders' backs and the nape
    for i, x in enumerate((-0.17, -0.09, 0.0, 0.09, 0.17)):
        b = np.array((x, 0.07 + 0.03 * (abs(x) < 0.1), 0.6))
        (red if i % 2 == 0 else mid).put(tongue(b, (x * 1.2, 0.55, 1.0), 0.2 - 0.25 * abs(x), 0.06, depth=0.035,
                                                 bend=0.1))
    k.add("flamered", red.shape(), FLAME_RED, "head", fuse=False, glow=FLAME_RED)
    k.add("flamemid", mid.shape(), FLAME, "head", fuse=False, glow=FLAME)
    k.add("flamecore", core.shape(), FLAME_CORE, "head", fuse=False)


def add_staff(k):
    """The staff of fire upright through the right hand: a red zigzag, a yellow core on its two faces, forked at its top."""
    x0, y0 = 0.3, -0.1
    zs = np.linspace(0.035, 0.84, 13)
    xs = [x0 + (0.03 if i % 2 else -0.03) * (0.4 if i == 0 else 1.0) for i in range(len(zs))]
    pts = [(x, y0, z) for x, z in zip(xs, zs)]
    radii = [0.026] + [0.032] * (len(zs) - 1)
    k.add("staff", outward(blade(pts, radii, (0, 1, 0), 0.6)), FLAME_RED, "arm{s}".format(s="R"), fuse=False,
          glow=FLAME_RED)
    for dy in (-0.0165, 0.0165):
        cp = [(x, y0 + dy, z) for x, z in zip(xs, zs)]
        k.add(f"staffcore{'F' if dy < 0 else 'B'}", outward(blade(cp[1:], [0.016] * (len(cp) - 1), (0, 1, 0), 0.35)),
              FLAME_CORE, "armR", fuse=False)
    # the fork: two prongs out and up from the top, each curling over at its end
    top = np.array(pts[-1])
    for s in (1, -1):
        fp = [top, top + (0.035 * s, 0.0, 0.05), top + (0.06 * s, 0.0, 0.1), top + (0.085 * s, 0.0, 0.12),
              top + (0.1 * s, 0.0, 0.1)]
        k.add(f"fork{'R' if s > 0 else 'L'}", outward(blade(fp, [0.03, 0.027, 0.023, 0.019, 0.015], (0, 1, 0), 0.6)),
              FLAME_RED, "armR", fuse=False, glow=FLAME_RED)
        k.add(f"forkcore{'R' if s > 0 else 'L'}",
              outward(blade([np.array(p) + (0, -0.0155, 0) for p in fp[:4]], [0.015, 0.013, 0.011, 0.008], (0, 1, 0),
                            0.35)), FLAME_CORE, "armR", fuse=False)


def emboarmega(src, out):
    k = Kit(src, coat="fur")
    add_body(k)
    add_skirt(k)
    em.add_legs(k)
    em.add_arms(k)
    em.add_head(k)
    # Emboar's two prongs give way to the crown's horns
    k.parts = [p for p in k.parts if not p["name"].startswith("prong")]
    em.paint_head(k)
    add_crown(k)
    add_mantle(k)
    add_staff(k)
    em.add_tail(k)
    em.add_mouth(k)
    k.build(voxel=0.0075, band=0.005)
    k.eye_states(lambda s: em.lay_eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    emboarmega(*args())
