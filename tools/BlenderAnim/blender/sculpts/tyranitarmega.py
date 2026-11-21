# Mega Tyranitar sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tyranitarmega.py -- <the export's Tyranitar-Mega.glb> <out.glb>
# Copied from tyranitar.py (whose build function takes no arguments that reach what must change) and changed where the
# pictures change it; the trunk, legs, arms, head and eyes are the base's own. The Mega, from the references: the great
# change is the spikes, long curving horns of rock radiating out, back and up from the shoulders and down the back on
# each side (four a side, each nearly half the height long), the tail's spikes grown long too; the plate down the front
# is red now (Larvitar's colour), a shield pointing down with a notch at its top; above it a face of black holes on the
# chest, two large slanted ovals with red hearts, small holes between and beside them and on each side of the neck; the
# horn on the crown taller, more points back off the head. (The base's own description follows.)
# Grown from larvitar.py (its green, its eyes under a brow, its spikes, nails and holes of flat-faced rock): a huge
# armoured biped. From the references: a massive trunk leaning a little forward, wide at the hips, the thighs great
# masses fused into it over short thick shins and broad feet with three white claws; a blue diamond plate down the
# front crossed by darker ribs; black slanted holes in the armour, one high on each side of the chest and two on each
# thigh; short thick arms held forward and down, three white claws a hand and a white spike at each wrist; a head
# carried forward on a thick neck, the jaws open (red within, two white fangs down), a horn swept back off the crown
# and a point back off each cheek, small red eyes under a scowling brow; big spikes over the shoulders and down the
# back; a long thick tail sweeping back and out to +X, spikes along its top.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, polygon, eye_look, args, sibling

lv = sibling("larvitar")

GREEN, HOLE, CLAW = lv.GREEN, lv.HOLE, lv.CLAW
PLATE, PLATERIB = "#D23440", "#A8262F"
MOUTH = "#B23A46"

TRI_IN = [(1, 1), (1, -1), (-1, 0.15)]           # a slanted hole pointing toward the middle (u outward)


def pear(V):
    V = V.copy()
    low = np.clip(-V[:, 2], 0, 1)
    V[:, :2] *= (1 + 0.16 * low)[:, None]
    return V


def hole(k, c, n, half, stripe=True):
    """A slanted black hole in the armour, pointing toward the middle, a green bar across it splitting it in two."""
    c, n = np.array(c, float), unit(*n)
    m = np.array([-1, 1, 1.0])
    for s in (1, -1):
        cc, nn = (c, n) if s > 0 else (c * m, n * m)
        # on a patch facing out to the side +u runs toward the back on both sides: the point is to the front (-u)
        k.decal(cc, nn, half, HOLE, eps=0.004, outline=polygon(TRI_IN), rings=10, segs=48)
        if stripe:
            k.decal(cc + np.array([0, 0, -0.004]), nn, (half[0] * 0.95, half[1] * 0.16), GREEN, eps=0.006,
                    outline=polygon([(-1, -1), (1, -1), (1, 1), (-1, 1)]), rings=4, segs=40,
                    up=unit(0, 0, 1))


def tyranitarmega(src, out):
    k = Kit(src, coat="shell")

    # The trunk: a great pear, the chest broad under the shoulders, the hips wide
    k.add("torso", ell((0, 0.0, 0.5), (0.2, 0.175, 0.24), deform=pear), GREEN, "body")
    k.add("chest", ell((0, -0.02, 0.645), (0.185, 0.15, 0.115)), GREEN, "body")
    k.add("hips", ell((0, 0.02, 0.37), (0.205, 0.17, 0.13)), GREEN, "body")
    k.add("neck", tube([(0, 0.0, 0.66), (0, -0.03, 0.74), (0, -0.07, 0.8)], [0.13, 0.11, 0.095]), GREEN, "head")

    # The head: carried forward, a crown over a broad snout, the lower jaw dropped open under it
    H, HR = np.array([0.0, -0.085, 0.84]), np.array([0.105, 0.11, 0.092])
    k.add("head", ell(H, HR), GREEN, "head")
    k.add("snout", ell((0, -0.175, 0.825), (0.08, 0.085, 0.058), rot=(-6, 0, 0)), GREEN, "head")
    k.add("brow", ell((0.05, -0.15, 0.875), (0.045, 0.06, 0.022), rot=(-14, 0, -10)), GREEN, "head", mirror=True)
    k.add("jaw", ell((0, -0.15, 0.758), (0.075, 0.09, 0.036), rot=(14, 0, 0)), GREEN, "head")
    k.add("mouth", ell((0, -0.215, 0.79), (0.058, 0.085, 0.028), rot=(2, 0, 0)), MOUTH, "head", carve=True)
    for s in (1, -1):
        lv.spike(k, f"fang{'R' if s > 0 else 'L'}", (0.043 * s, -0.24, 0.81), (0, -0.1, -1), 0.04, 0.026, 0.02,
                 (0, 1, 0), CLAW, "head", sink=0.012)
    # The horn swept back off the crown, and a point back off each cheek
    lv.spike(k, "horn", (0, -0.08, 0.905), (0, 0.4, 0.92), 0.22, 0.075, 0.055, (1, 0, 0), GREEN, "head", sink=0.04)
    # the Mega's points back off the head, behind the horn
    for j, (b, d, ln) in enumerate((((0, 0.0, 0.9), (0, 0.9, 0.45), 0.13), ((0, 0.04, 0.84), (0, 0.95, 0.15), 0.12))):
        lv.spike(k, f"nape{j}", b, d, ln, 0.065, 0.045, (1, 0, 0), GREEN, "head", sink=0.035)
    lv.spike(k, "cheek", (0.085, -0.06, 0.83), (0.55, 0.8, 0.2), 0.1, 0.06, 0.045, (0, 0, 1), GREEN, "head",
             sink=0.035, mirror=True)

    # The arms: short and thick, forward and a little down, three white claws, a white spike at the wrist
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"arm{side}", tube(np.array([(0.17, -0.02, 0.6), (0.25, -0.08, 0.55), (0.29, -0.15, 0.51)]) * m,
                                 [0.08, 0.07, 0.064]), GREEN, f"arm{side}")
        HC = np.array((0.3, -0.19, 0.5))
        k.add(f"hand{side}", ell(HC * m, (0.058, 0.055, 0.055)), GREEN, f"arm{side}")
        for j, (dx, dz) in enumerate(((-0.028, 0.02), (0.0, -0.005), (0.026, -0.025))):
            lv.claw(k, f"claw{side}{j}", np.array((0.3 + dx, -0.235, 0.5 + dz)) * m, np.array((dx, -1, -0.25)) * m,
                    0.045, 0.034, f"arm{side}")
        lv.spike(k, f"wrist{side}", np.array((0.33, -0.12, 0.54)) * m, np.array((0.7, 0.6, 0.35)) * m, 0.06, 0.04,
                 0.034, (0, 0, 1), CLAW, f"arm{side}", sink=0.025)

    # The legs: great thighs fused into the hips, short thick shins, broad feet with three white claws
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array((0.16, 0.0, 0.29)) * m, (0.13, 0.155, 0.16)), GREEN, f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.17, -0.015, 0.19), (0.172, -0.03, 0.07)]) * m, [0.095, 0.088]), GREEN,
              f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.172, -0.07, 0.045)) * m, (0.1, 0.135, 0.048)), GREEN, f"leg{side}")
        for j, dx in enumerate((-0.05, 0.0, 0.05)):
            lv.claw(k, f"toe{side}{j}", np.array((0.172 + dx, -0.19, 0.035)) * m, np.array((dx * 0.6, -1, -0.3)) * m,
                    0.05, 0.04, f"leg{side}")

    # The tail: thick at the hips, long, sweeping back, down and out to +X, spikes along its top
    TAIL = [(0, 0.14, 0.38), (0.04, 0.33, 0.25), (0.11, 0.48, 0.15), (0.19, 0.58, 0.105), (0.25, 0.66, 0.095)]
    k.add("tail", tube(TAIL, [0.145, 0.11, 0.078, 0.05, 0.018]), GREEN, "tail")
    for j, (t, ln, w) in enumerate(((0.12, 0.2, 0.095), (0.38, 0.22, 0.085), (0.62, 0.17, 0.075), (0.85, 0.12, 0.06))):
        pts = np.array(TAIL)
        i = t * (len(pts) - 1); a = int(i); f = i - a
        c = pts[a] * (1 - f) + pts[a + 1] * f
        r = np.interp(t, np.linspace(0, 1, 5), [0.145, 0.11, 0.078, 0.05, 0.018])
        dirn = unit(*(pts[a + 1] - pts[a]))
        lv.spike(k, f"tailspike{j}", c + np.array([0, 0, r * 0.7]), unit(*(dirn * 0.8 + np.array([0, 0, 1.0]))), ln,
                 w, 0.048, np.cross(dirn, (0, 0, 1.0)), GREEN, "tail", sink=0.04)

    # The spikes over the shoulders and down the back, few and bold, pointing out, up and back
    # The Mega's great spikes, the largest change: long horns of rock radiating out, back and up from the shoulders
    # and down the back, each made of two stones (a root and a tip bending upward), the lowest pointing out and down
    for j, (b, d, ln, w) in enumerate((((0.15, 0.03, 0.72), (0.72, 0.35, 0.6), 0.34, 0.085),
                                       ((0.17, 0.07, 0.62), (0.85, 0.45, 0.28), 0.42, 0.09),
                                       ((0.16, 0.11, 0.5), (0.82, 0.55, 0.05), 0.38, 0.085),
                                       ((0.15, 0.13, 0.4), (0.75, 0.6, -0.18), 0.28, 0.075),
                                       ((0.07, 0.12, 0.72), (0.3, 0.75, 0.6), 0.24, 0.08))):
        b, d = np.array(b), unit(*d)
        side = np.cross(d, (0, 0, 1.0)) if abs(d[2]) < 0.9 else (1, 0, 0)
        ln, w = ln * 1.25, w * 1.1
        L1 = ln * 0.55
        lv.spike(k, f"back{j}", b, d, L1 * 1.25, w, 0.055, side, GREEN, "body", sink=0.045, mirror=True)
        d2 = unit(*(d + np.array([0, 0, 0.45])))
        lv.spike(k, f"backtip{j}", b + d * L1 * 0.85, d2, ln - L1 * 0.85, w * 0.62, 0.04, side, GREEN, "body",
                 sink=0.03, mirror=True)
    lv.spike(k, "nape", (0, 0.1, 0.78), (0, 0.85, 0.55), 0.16, 0.085, 0.05, (1, 0, 0), GREEN, "body", sink=0.045)

    # The blue plate down the front: a diamond by the angle round the trunk, its points at the chest and the crotch,
    # crossed by darker ribs
    # The Mega's red shield down the front: widening from its point at the crotch to its top under the chest's holes,
    # a notch in the middle of its top edge; a darker stripe down its middle
    def plate(P):
        a = np.abs(np.arctan2(P[:, 0], -(P[:, 1] - 0.0)))
        z = P[:, 2]
        w = 0.6 * np.clip((z - 0.29) / 0.16, 0, 1)
        top = 0.525 + 0.06 * np.clip(a / 0.35, 0, 1)
        return inside(a - w, 0.006) * inside(z - top, 0.003) * (P[:, 1] < -0.05) * inside(np.abs(P[:, 0]) - 0.17, 0.004)
    def stripe(P):
        return plate(P) * inside(np.abs(P[:, 0]) - 0.012, 0.003)
    k.paint(plate, PLATE)
    k.paint(stripe, PLATERIB)

    # The chest's face of black holes: two large slanted ovals with red hearts, small holes between them and beside the
    # neck
    HOLE_RED = "#D8283A"
    for c, n, half, up in (((0.08, -0.155, 0.62), (0.35, -1, 0.1), (0.05, 0.03), (-0.55, 0, 1)),):
        k.decal(c, unit(*n), half, HOLE, up=unit(*up), eps=0.004, rings=12, segs=48, mirror=True)
        k.decal(c, unit(*n), (half[0] * 0.55, half[1] * 0.45), HOLE_RED, up=unit(*up), eps=0.006, rings=8, segs=40,
                mirror=True)
    k.decal((0.028, -0.165, 0.695), unit(0.15, -1, 0.2), (0.016, 0.026), HOLE, eps=0.004,
            outline=polygon([(0, 1), (1, -1), (-1, -1)]), rings=8, segs=40, mirror=True)
    k.decal((0.0, -0.17, 0.555), unit(0, -1, 0.05), (0.018, 0.022), HOLE, eps=0.004,
            outline=polygon([(-1, 1), (1, 1), (0, -1)]), rings=8, segs=40)
    k.decal((0.09, -0.115, 0.72), unit(0.4, -1, 0.35), (0.018, 0.032), HOLE, up=unit(-0.4, 0, 1), eps=0.004, rings=8,
            segs=40, mirror=True)

    # The black holes: high on each side of the chest and two on each thigh
    hole(k, (0.165, -0.08, 0.66), (0.75, -0.6, 0.15), (0.05, 0.026))
    hole(k, (0.27, -0.04, 0.33), (1, -0.25, 0.05), (0.06, 0.03))
    hole(k, (0.26, -0.03, 0.22), (1, -0.3, -0.15), (0.05, 0.025), stripe=False)

    # The eyes: small and red under the scowling brow (Larvitar's, with no wedge under them)
    de = unit(0.62, -0.74, 0.22)
    E = lv.on_ell(H, HR, de) + np.array([0, -0.01, -0.005])
    cut = lv.brow_cut(0.22, 0.45)
    eyes = lv.eyes_under_brow(k, E, de, (0.032, 0.029), cut, srgb(GREEN))

    k.build(voxel=0.008, smooth=4)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    tyranitarmega(*args())
