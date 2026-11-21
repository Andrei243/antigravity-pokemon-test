# Mega Crabominable sculpted on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/crabominablemega.py -- <the export's Crabominable-Mega.glb> <out.glb>
#
# Crabominable grown (crabominable.py's body and the Crabrawler line's legs by sibling). From the references: the two fists
# are the design's largest masses, raised up and out over the shoulders in a V: each a ball of dark navy fur hanging in
# jagged points round a white fist, crowned on top with great cyan crystals of ice. The body a round ball, its lower half
# dark navy ringed with cyan stripes, its upper half and face buried in shaggy white fur: a mop of white hair over the
# brow, a white beard hanging over the chest in locks. Under the fringe two small cyan eyes in dark red sockets; a wide
# open mouth of dark navy. One yellow feeler curling up from the crown to its right. A ruff of yellow-olive fur spikes
# fanned out behind the waist. Small cyan crystals on the chest's sides. White arms with navy patches at the elbows. Four
# white legs ending in long cyan crystals. (The pictures overrule the game's sculptor: no icicles hang under the body,
# the fists are navy fur with no paw pad, and the ice is a crown on the fists' tops.)
#
# Reference ratios (3D picture, the figure 480 px tall): a fist's fur ball across / body across 1.07; the fist's centre
# over the body's centre by 0.33 of the height and out by 0.29; the ice crown's height / the fist's 0.45; the legs'
# crystals 0.2 of the height. Bones: root, body, arm{s}, leg{s}, hind{s} (as Crabominable's).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, outward, hull, polygon, sibling, axes

cb = sibling("crabominable")
cr = cb.cr

WHITE, NAVY, CYAN, ICE, ICE_DK = "#F4F4F8", "#3E3A6E", "#3CC4EA", "#7FD8F4", "#3AA8DC"
YELLOW, OLIVE, SOCKET = "#F0D84A", "#C9BE4A", "#7A1E2A"
NAVY_DK = "#2C2852"
CW, CK = srgb("#FAFAF6"), srgb("#1C1830")

BODY_C, BODY_R = cb.BODY_C, cb.BODY_R          # (0, 0, 0.188), (0.11, 0.1, 0.128)


# ---- the body: Crabominable's egg; the lower half navy with cyan stripes --------------------------------------------
def body(k):
    cb.body(k)
    c = BODY_C
    def low(P):
        x, y, z = P[:, 0] - c[0], P[:, 1] - c[1], P[:, 2]
        return inside(z - 0.2, 0.002) * inside(np.hypot(x, y) - 0.13, 0.004)
    k.paint(low, NAVY)
    for z0, z1 in ((0.164, 0.176), (0.128, 0.14), (0.094, 0.105)):
        cr.stripe(k, z0=z0, z1=z1, c=c, reach=0.13, color=CYAN)


def lock(k, name, root, d, length, w, color, bone="body", surface="fur"):
    """A lock of fur: a tapering four-sided hull from root along d, wide w at its root."""
    root = np.array(root, float)
    d = unit(*d)
    a, b = axes(d)
    tip = root + d * length
    mid = root + d * length * 0.45
    pts = [root + a * w, root - a * w, root + b * w * 0.6, root - b * w * 0.6,
           mid + a * w * 0.7, mid - a * w * 0.7, mid + b * w * 0.4, mid - b * w * 0.4, tip]
    k.add(name, hull(pts), color, bone, fuse=False, surface=surface)


# ---- the hair: a white mop over the brow, a beard down the chest, fur round the sides --------------------------------
def fur(k):
    k.add("mop", ell((0.0, -0.012, 0.318), (0.1, 0.088, 0.058)), WHITE, "body")
    # the fringe: locks hanging over the brow, between and beside the eyes
    for i, (x, l) in enumerate(((0.0, 0.05), (0.045, 0.045), (-0.045, 0.045), (0.08, 0.05), (-0.08, 0.05),
                                (0.024, 0.032), (-0.024, 0.032))):
        lock(k, f"fringe{i}", (x, -0.07 + 0.25 * abs(x) ** 2, 0.352), (x * 2.5, -0.55, -1), l, 0.022, WHITE)
    # spiky crown of hair over the top and back
    for i in range(9):
        t = (i / 8 - 0.5) * math.pi * 1.2
        root = (0.08 * math.sin(t), 0.02 + 0.05 * math.cos(t) * 0.4, 0.36)
        lock(k, f"crown{i}", root, (math.sin(t) * 1.2, 0.5, 0.6), 0.05, 0.02, WHITE)
    # the beard: locks hanging over the upper chest, under the mouth
    for i, (x, z, l) in enumerate(((0.0, 0.18, 0.07), (0.037, 0.18, 0.07), (-0.037, 0.18, 0.07),
                                   (0.07, 0.19, 0.065), (-0.07, 0.19, 0.065), (0.095, 0.205, 0.06),
                                   (-0.095, 0.205, 0.06), (0.018, 0.172, 0.05), (-0.018, 0.172, 0.05))):
        y = -math.sqrt(max(0.0, 1 - (x / 0.115) ** 2)) * 0.1 - 0.006
        lock(k, f"beard{i}", (x, y, z), (x * 1.2, -0.25, -1), l, 0.024, WHITE)
    # fur round the shoulders and flanks, pointing out
    for i in range(10):
        t = (i / 9) * math.pi * 0.9 + 0.05 * math.pi
        for s in (1, -1):
            d = np.array((s * math.sin(t), -math.cos(t) * 0.7, 0.1 - 0.4 * (i % 2)))
            root = BODY_C + np.array((s * 0.1 * math.sin(t), -0.09 * math.cos(t), 0.04 + 0.02 * (i % 3)))
            lock(k, f"side{'R' if s > 0 else 'L'}{i}", root, d, 0.05 + 0.01 * (i % 2), 0.02, WHITE)
    # the yellow-olive ruff fanned out behind the waist
    for i in range(7):
        for s in (1, -1):
            t = 0.1 + 0.2 * i
            d = np.array((s * math.cos(t), 0.35 + 0.5 * math.sin(t), -0.25 + 0.12 * (i % 2)))
            root = BODY_C + np.array((s * 0.085 * math.cos(t), 0.04 + 0.03 * math.sin(t), -0.02))
            lock(k, f"ruff{'R' if s > 0 else 'L'}{i}", root, d, 0.16 - 0.012 * i, 0.026,
                 YELLOW if i % 2 else OLIVE)


def feeler(k):
    pts = [(0.012, 0.0, 0.37), (0.03, 0.01, 0.41), (0.065, 0.02, 0.435), (0.1, 0.02, 0.43), (0.115, 0.012, 0.41)]
    k.add("feeler", outward(tube(pts, [0.012, 0.011, 0.01, 0.009, 0.008], seg=12, per=6)), YELLOW, "body",
          fuse=False, surface="hair")


def gem(c, d, length, r):
    """A small cut crystal: a bipyramid along d."""
    c = np.array(c, float); d = unit(*d)
    a, b = axes(d)
    pts = [c + d * length, c - d * length * 0.6]
    for i in range(6):
        t = 2 * math.pi * i / 6
        pts.append(c + r * (math.cos(t) * a + math.sin(t) * b))
    return hull(pts)


def chest_gems(k):
    for s, sn in ((1, "R"), (-1, "L")):
        k.add("chestgem" + sn, gem((0.105 * s, -0.06, 0.2), (0.25 * s, -0.3, -1), 0.03, 0.014), CYAN, "body",
              fuse=False, surface="shell")
        k.add("armgem" + sn, gem((0.13 * s, -0.05, 0.27), (0.6 * s, -0.4, 0.5), 0.026, 0.012), CYAN, "arm" + sn,
              fuse=False, surface="shell")


# ---- the face: cyan eyes in dark red sockets under the fringe, a wide navy mouth --------------------------------------
EYE_C = np.array([0.026, -0.1, 0.298])
EYE_N = unit(0.3, -1, 0.15)
EYE_HALF = (0.017, 0.016)
MOUTH_C, MOUTH_N = np.array([0.0, -0.106, 0.256]), unit(0, -1, 0.1)


def face(k):
    mo = polygon([(-1, 0.5), (-0.8, 0.95), (-0.35, 0.8), (0.35, 0.8), (0.8, 0.95), (1, 0.5), (0.95, -0.2),
                  (0.7, -0.8), (0.3, -1), (-0.3, -1), (-0.7, -0.8), (-0.95, -0.2)])
    k.decal(MOUTH_C, MOUTH_N, (0.072, 0.038), NAVY, eps=0.0036, outline=mo, rings=12, segs=96)


def open_eye(u, v):
    c = np.tile(srgb(SOCKET), (len(u), 1))
    c[((u + 0.08) / 0.56) ** 2 + ((v - 0.02) / 0.62) ** 2 < 1] = srgb(CYAN)
    c[((u + 0.08) / 0.24) ** 2 + ((v - 0.02) / 0.3) ** 2 < 1] = CK
    c[((u - 0.12) / 0.17) ** 2 + ((v - 0.3) / 0.17) ** 2 < 1] = CW
    return c


def eyes(k):
    lid = srgb(WHITE)
    def lay(state):
        k.decal(EYE_C, EYE_N, EYE_HALF, eye_look(state, open_eye, lid, CK, line=0.18), eps=0.0045, rings=20,
                segs=56, mirror=True)
    return lay


# ---- the arms raised, and the fists of navy fur crowned with ice ------------------------------------------------------
ARM = [(0.098, -0.02, 0.278), (0.165, -0.04, 0.3), (0.2, -0.055, 0.372)]
ARM_R = [0.034, 0.031, 0.032]
FIST_C, FIST_R = np.array([0.232, -0.055, 0.46]), np.array([0.1, 0.085, 0.088])


def arms(k):
    for side, sn in ((1, "R"), (-1, "L")):
        m = np.array([side, 1, 1.0])
        P = [np.array(p) * m for p in ARM]
        k.add("arm" + sn, tube(P, ARM_R, seg=16, per=6), WHITE, "arm" + sn)
        el = P[1]
        k.paint(lambda Q, el=el: inside(np.linalg.norm(Q - el, axis=1) - 0.03, 0.0015), NAVY, on=["arm" + sn])
        fist(k, side)


def fist(k, side):
    sn = "R" if side > 0 else "L"
    m = np.array([side, 1, 1.0])
    c = FIST_C * m
    b = "arm" + sn
    # the white fist under the fur, and the fur ball over it
    k.add("fistcore" + sn, ell(c + (0.0, -0.005, -0.03), FIST_R * (0.8, 0.8, 0.72)), WHITE, b)
    k.add("fist" + sn, ell(c, FIST_R), NAVY, b)
    k.paint(lambda Q, c=c: inside(Q[:, 2] - (c[2] - 0.052), 0.002)
            * inside(np.linalg.norm((Q - c) / FIST_R, axis=1) - 1.1, 0.01), WHITE, on=["fistcore" + sn])
    # the fur hanging in jagged points round the ball's lower edge
    for i in range(16):
        t = 2 * math.pi * i / 16 + 0.1
        d = np.array((math.cos(t), math.sin(t), 0.0))
        root = c + d * FIST_R * (0.92, 0.92, 0) + np.array((0, 0, -0.01 + 0.012 * (i % 2)))
        ln = 0.055 + 0.022 * (i % 3 == 0) + 0.01 * (i % 2)
        lock(k, f"furpt{sn}{i}", root, d * 0.45 + np.array((0, 0, -1.0)), ln, 0.026, NAVY, bone=b)
    # the crown of ice: a faceted base on the top, five great crystals rising out of it
    top = c + np.array((0, 0, FIST_R[2] * 0.72))
    base = []
    for i in range(8):
        t = 2 * math.pi * i / 8
        base.append(top + np.array((0.08 * math.cos(t), 0.07 * math.sin(t), -0.03)))
        base.append(top + np.array((0.066 * math.cos(t + 0.39), 0.058 * math.sin(t + 0.39), 0.02)))
    k.add("icebase" + sn, hull(base), ICE_DK, b, fuse=False, surface="shell")
    spikes = [((0, 0, 0.0), (0, 0, 1), 0.1, 0.034, ICE)]
    for i in range(6):
        t = 2 * math.pi * i / 6 + 0.3
        spikes.append(((0.055 * math.cos(t), 0.048 * math.sin(t), 0.0),
                       (0.55 * math.cos(t), 0.5 * math.sin(t), 1), 0.08 - 0.008 * (i % 2), 0.026,
                       ICE if i % 2 else CYAN))
    for j, (off, d, ln, r, col) in enumerate(spikes):
        root = top + np.array(off) * m
        d = unit(*(np.array(d) * m))
        a, bb = axes(d)
        pts = [root + d * ln]
        for q in range(5):
            th = 2 * math.pi * q / 5
            pts.append(root + r * (math.cos(th) * a + math.sin(th) * bb) - d * 0.015)
            pts.append(root + d * ln * 0.45 + r * 0.8 * (math.cos(th + 0.6) * a + math.sin(th + 0.6) * bb))
        k.add(f"ice{sn}{j}", hull(pts), col, b, fuse=False, surface="shell")


# ---- the legs: white, ending in long cyan crystals --------------------------------------------------------------------
LEGS = [
    ("leg{s}", (0.08, -0.04, 0.13), (0.145, -0.075, 0.15), (0.17, -0.1, 0.1), (0.188, -0.118, 0.0)),
    ("hind{s}", (0.08, 0.04, 0.13), (0.145, 0.075, 0.15), (0.17, 0.1, 0.1), (0.188, 0.118, 0.0)),
]


def crabominablemega(src, out_path):
    k = Kit(src, coat="fur")
    body(k)
    fur(k)
    feeler(k)
    chest_gems(k)
    cr.legs(k, specs=LEGS, r=(0.026, 0.024, 0.022), tip_r=0.031, color=WHITE, tip_color=CYAN, surface="fur")
    arms(k)
    face(k)
    k.build(voxel=0.0048, smooth=3, band=0.005, wband=0.04)
    k.eye_states(eyes(k))
    k.export(out_path)


if __name__ == "__main__":
    src, dst = args()
    crabominablemega(src, dst)
