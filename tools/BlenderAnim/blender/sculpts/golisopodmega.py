# Golisopod-Mega sculpted on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/golisopodmega.py -- <the export's Golisopod-Mega.glb> <out.glb>
# Grown from golisopod.py (sibling("golisopod")): its horn chains, triangle marks and the way its gauntlets are rings of
# plate round a forearm; the plates gone gunmetal, the purple X of the face become lightning.
#
# From the references (10316): a crab-like knight in gunmetal armour, crouched low on FOUR grey legs (two in front, two
# spread behind: the pictures count four boots). Its masses, largest first: (1) two great gauntlet arms held out to the
# sides and a little forward, stacks of grey rings over a dark maroon forearm, each with a row of purple spikes along
# its top and three long black claws at the hand, each some 0.33 of the height long and 0.2 thick; (2) the head, a
# great rounded dome of three overlapping grey bands with dark green triangles, enclosing a dark face low at its front,
# some 0.36 of the width and 0.42 of the height; (3) two thin arms raised behind the shoulders, each ending in a round
# grey pod of a hand with a maroon palm and black claws hanging down, setting the height; (4) the purple: zigzag
# lightning up the dome from the eyes, lines down to the cheeks, and a long purple beard down the chest; yellow eyes
# with a dark X. Under the dome a dark maroon belly; a short tail behind with purple spikes.
# The game's sculptor (PokemonModels.Alola2.cs) speaks of a crest of purple fins down the head and back: the pictures
# show none on the dome, only purple shards along the gauntlets and behind; they overrule it.
# Bones (the form's own): body (0, 0, 0.42), arms (+-0.195, -0.02, 0.52), head (0, -0.07, 0.57), tail (0, 0.08, 0.32),
# legs (+-0.08, 0.02, 0.24). The second pair of legs is on the same leg bones; the raised pod arms on the body.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, lathe, boxy, srgb, inside, unit, eye_look, outward, polygon, \
    sibling, args

G = sibling("golisopod")
on_ell, frame = G.on_ell, G.frame

PLATE, PLATE_DK = "#7E8188", "#62656C"
MARK = "#3F8A72"
PURPLE = "#9B3CCB"
MAROON, MAROON_DK = "#5E3C44", "#45292F"
FACE, CLAW = "#1E1C24", "#26302C"
EYE_Y = "#F6DF3C"

# --- the frame --------------------------------------------------------------------------------------------------------
DOME_C, DOME_R = np.array([0.0, -0.01, 0.5]), np.array([0.16, 0.15, 0.17])
BANDS = [  # centre, radii, turn about x: the dome's three bands, back to front
    ((0.0, 0.06, 0.53), (0.165, 0.12, 0.15), -30),
    ((0.0, -0.015, 0.585), (0.16, 0.12, 0.11), 0),
    ((0.0, -0.075, 0.57), (0.145, 0.09, 0.09), 32),
]
FACE_C, FACE_R = np.array([0.0, -0.125, 0.445]), np.array([0.105, 0.07, 0.09])
SHOULDER = np.array([0.195, -0.02, 0.5])
ELBOW, WRIST = np.array([0.25, -0.06, 0.45]), np.array([0.33, -0.175, 0.41])


def triangle_mark(k, c, n, size, up=(0, 0, 1)):
    k.decal(c, n, (size, size), MARK, up=up, eps=0.0025, rings=8, segs=36, outline=G.W.TRI)


def spikes(k, name, root, d, n, length, w, bone, step):
    """A row of n purple shards from root along step, each pointing along d."""
    for i in range(n):
        b = np.asarray(root, float) + np.asarray(step, float) * i
        dd = unit(*d)
        _, a, c = frame(dd)
        L = length * (1 - 0.15 * abs(i - (n - 1) / 2))
        k.add(f"{name}{i}", hull([b + a * w, b - a * w, b + c * w * 0.6, b - c * w * 0.6, b + dd * L]), PURPLE, bone,
              fuse=False, surface="shell")


def dome(k):
    for i, (c, r, rx) in enumerate(BANDS):
        k.add(f"band{i}", ell(c, r, rot=(rx, 0, 0), deform=boxy(2.5)), PLATE, "head")
        for s in (1, -1):
            p, n = on_ell(c, r, (0.42 * s, -0.3, 1))
            triangle_mark(k, p, n, 0.026, up=(0, -1, 0))
    # the dome comes down the cheeks to the jaw, round the face
    k.add("cheek", ell((0.11, -0.05, 0.43), (0.07, 0.1, 0.11), deform=boxy(2.4)), PLATE, "head", mirror=True)
    k.add("face", ell(FACE_C, FACE_R), FACE, "head")
    k.add("jaw", ell(FACE_C + (0, 0.01, -0.06), (0.07, 0.06, 0.04)), FACE, "head")
    # the dark face: the front of the head under the dome's brow
    def face(P):
        x, y, z = P[:, 0] - FACE_C[0], P[:, 1] - FACE_C[1], P[:, 2] - FACE_C[2]
        return inside(y + 0.01, 0.003) * inside(z - 0.05 + 0.2 * np.abs(x), 0.003) * inside(-0.095 - z, 0.003) * \
            inside(np.abs(x) - 0.092, 0.003) * inside(P[:, 2] - 0.52, 0.003)
    k.paint(face, FACE, on=["face", "jaw", "band", "cheek"])
    # triangles on the cheeks
    for s in (1, -1):
        p, n = on_ell((0.11 * s, -0.05, 0.43), (0.07, 0.1, 0.11), (0.9 * s, -0.5, 0.1))
        triangle_mark(k, p, n, 0.022)


def lightning(k):
    """The purple lightning: a zigzag up the dome from each eye, a line down to each cheek, the beard down the chest."""
    # painted in the front's (x, z): zigzags from the eyes' inner corners up over the dome, and a line from under each
    # eye out and down to the cheek's edge, as the pictures draw them
    lines = []
    for s in (1, -1):
        lines.append([(0.012 * s, 0.455), (0.045 * s, 0.5), (0.03 * s, 0.53), (0.075 * s, 0.585), (0.055 * s, 0.62),
                      (0.1 * s, 0.67)])
        lines.append([(0.03 * s, 0.415), (0.07 * s, 0.395), (0.085 * s, 0.37), (0.115 * s, 0.35)])
    def zig(P):
        x, z = P[:, 0], P[:, 2]
        best = np.full(len(P), 9.0)
        for line in lines:
            for (x0, z0), (x1, z1) in zip(line, line[1:]):
                ex, ez = x1 - x0, z1 - z0
                t = np.clip(((x - x0) * ex + (z - z0) * ez) / (ex * ex + ez * ez), 0, 1)
                best = np.minimum(best, np.hypot(x - x0 - t * ex, z - z0 - t * ez))
        return inside(best - 0.0105, 0.002) * inside(P[:, 1] + 0.03, 0.01) * inside(P[:, 2] - 0.7, 0.003)
    k.paint(zig, PURPLE, on=["band", "face", "cheek", "jaw"])
    # the beard: from between the eyes, broad, down over the chest to a point
    top, bot = FACE_C + (0, -0.072, -0.03), np.array([0.0, -0.175, 0.24])
    pts = []
    for t, w in ((0.0, 0.016), (0.25, 0.03), (0.5, 0.03), (0.78, 0.02), (1.0, 0.0)):
        c = top + (bot - top) * t + np.array([0, -0.012 * math.sin(math.pi * t), 0])
        pts += [c + (w, 0, 0), c - (w, 0, 0), c + np.array([0, -0.01, 0]) * (w > 0), c + (0, 0.01, 0)]
    for i in range(0, len(pts) - 4, 4):
        k.add(f"beard{i}", hull(pts[i:i + 8]), PURPLE, "head", fuse=False, surface="shell")


def torso(k):
    k.add("torso", ell((0, 0.0, 0.33), (0.13, 0.12, 0.12)), MAROON, "body")
    k.add("chest", ell((0, -0.1, 0.33), (0.085, 0.06, 0.085)), MAROON, "body")
    k.add("belly", ell((0, 0.0, 0.23), (0.11, 0.1, 0.07)), MAROON, "body")
    # the dark round plate on the chest under the beard
    k.paint(lambda P: inside(np.hypot(P[:, 0], P[:, 2] - 0.31) - 0.065, 0.003) * inside(P[:, 1] + 0.12, 0.004),
            MAROON_DK, on=["chest", "torso"])
    # a grey plate over the shoulders and back, under the dome
    k.add("mantle", ell((0, 0.06, 0.43), (0.16, 0.1, 0.08), deform=boxy(2.4)), PLATE, "body")
    for s in (1, -1):
        p, n = on_ell((0, 0.06, 0.43), (0.16, 0.1, 0.08), (0.8 * s, 0.4, 0.4))
        triangle_mark(k, p, n, 0.024)


def gauntlets(k):
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1])
        sh, el, wr = SHOULDER * m, ELBOW * m, WRIST * m
        k.add(f"pauldron{side}", ell(sh + (0.0, 0.0, 0.02), (0.06, 0.06, 0.055), deform=boxy(2.4)), PLATE, f"arm{side}")
        k.add(f"upperarm{side}", tube([sh, (sh + el) / 2, el], [0.05, 0.05, 0.05]), MAROON, f"arm{side}")
        d = wr - el
        L = np.linalg.norm(d); d = d / L
        k.add(f"forearm{side}", tube([el, wr + d * 0.03], [0.06, 0.065]), MAROON_DK, f"arm{side}")
        _, a, b = frame(d)
        for j in range(4):
            t0 = 0.0 + j * 0.27
            c = el + d * L * t0
            r0, r1 = 0.085 + 0.006 * j, 0.096 + 0.006 * j
            k.add(f"ring{side}{j}", lathe(c, d, [(0, 0), (r0, 0), (r1, L * 0.2), (r1 - 0.012, L * 0.25),
                                                  (0, L * 0.25)], seg=40), PLATE, f"arm{side}", fuse=False,
                  surface="shell")
            o = unit(*(b * 0.8 + a * 0.5 * s))
            tc = c + d * L * 0.11 + o * (r0 + r1) / 2 * 1.04
            k.add(f"mark{side}{j}", slab([(-0.022, -0.019), (-0.022, 0.019), (0.024, 0.0)], tc, d,
                                         np.cross(o, d), 0.006), MARK, f"arm{side}", fuse=False, surface="shell")
        # purple shards along the gauntlet's top, leaning back toward the shoulder
        top = unit(*(b * 0.9 + a * 0.2 * s))
        spikes(k, f"gspike{side}", el + d * L * 0.15 + top * 0.095, top * 1.0 - d * 0.45, 4, 0.06, 0.016,
               f"arm{side}", d * L * 0.24)
        # three long black claws at the hand, forward and down, curving in
        for j, off in enumerate((-0.045, 0.0, 0.045)):
            base = wr + d * 0.04 + a * off
            tip1 = base + d * 0.06 + np.array([0, -0.035, -0.04])
            tip2 = base + d * 0.08 + np.array([-0.02 * s, -0.06, -0.11])
            G.horn(k, f"claw{side}{j}", [base, tip1, tip2], [0.026, 0.019, 0.0], colour=CLAW, bone=f"arm{side}")


def pod_arms(k):
    """Two thin arms raised behind the shoulders, each a round grey pod of a hand with a maroon palm and claws."""
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1])
        P0, P1, P2 = np.array((0.12, 0.08, 0.58)) * m, np.array((0.2, 0.07, 0.68)) * m, np.array((0.225, 0.03, 0.75)) * m
        k.add(f"podarm{side}", tube([P0, P1, P2], [0.022, 0.02, 0.019]), PLATE, "body")
        pc = np.array((0.225, -0.005, 0.78)) * m
        k.add(f"pod{side}", ell(pc, (0.07, 0.062, 0.05), rot=(-20, 0, 0), deform=boxy(2.3)), PLATE, "body")
        k.add(f"palm{side}", ell(pc + (0, -0.012, -0.03), (0.06, 0.052, 0.03), rot=(-20, 0, 0)), MAROON, "body")
        for j, off in enumerate((-0.035, 0.0, 0.035)):
            base = pc + np.array([off, -0.04, -0.03])
            G.horn(k, f"podclaw{side}{j}", [base, base + (0.005 * np.sign(off), -0.03, -0.035),
                                            base + (0.01 * np.sign(off), -0.025, -0.08)],
                   [0.017, 0.012, 0.0], colour=CLAW, bone="body")


def legs(k):
    """Four short grey legs: a pair in front under the face, a pair spread behind; each pair on the leg bones."""
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1])
        for j, (hip, knee, ankle) in enumerate((
                ((0.09, -0.06, 0.22), (0.15, -0.12, 0.14), (0.15, -0.15, 0.05)),
                ((0.11, 0.06, 0.22), (0.21, 0.1, 0.15), (0.24, 0.13, 0.05)))):
            hip, knee, ankle = np.array(hip) * m, np.array(knee) * m, np.array(ankle) * m
            k.add(f"thigh{side}{j}", tube([hip, knee], [0.046, 0.042]), PLATE, f"leg{side}")
            k.add(f"knee{side}{j}", ell(knee, (0.044, 0.044, 0.04), deform=boxy(2.4)), PLATE, f"leg{side}")
            k.add(f"boot{side}{j}", tube([knee, (knee + ankle) / 2, ankle + (0, 0, 0.01)], [0.038, 0.04, 0.045]),
                  PLATE_DK, f"leg{side}")
            fwd = unit(*((ankle - hip) * (1, 1, 0) + (0, -0.12, 0)))
            k.add(f"foot{side}{j}", ell(ankle + (0, 0, -0.015) + fwd * 0.015, (0.05, 0.06, 0.03)), PLATE_DK,
                  f"leg{side}")
            for t, dx in enumerate((-0.025, 0.025)):
                _, a, _ = frame(fwd)
                bb = ankle + fwd * 0.055 + a * dx + (0, 0, -0.02)
                G.horn(k, f"toe{side}{j}{t}", [bb, bb + fwd * 0.03 + (0, 0, -0.006), bb + fwd * 0.05 + (0, 0, -0.02)],
                       [0.017, 0.012, 0.0], colour=CLAW, bone=f"leg{side}")
    k.add("hip", ell((0, 0.02, 0.22), (0.12, 0.11, 0.06)), MAROON, "body")


def tail(k):
    k.add("tail", tube([(0, 0.1, 0.28), (0, 0.15, 0.25), (0, 0.18, 0.21)], [0.065, 0.05, 0.035]), MAROON, "tail")
    k.add("tailplate", ell((0, 0.14, 0.27), (0.085, 0.06, 0.05), rot=(-30, 0, 0), deform=boxy(2.6)), PLATE, "tail")
    for s, side in ((1, "R"), (-1, "L")):
        spikes(k, f"tspike{side}", (0.03 * s, 0.14, 0.26), (0.7 * s, 0.6, -0.2), 3, 0.06, 0.016, "tail",
               (0.0, 0.03, -0.03))


def x_eye():
    CY, CI = srgb(EYE_Y), srgb(FACE)
    def eye(u, v):
        c = np.tile(CY, (len(u), 1))
        x = (np.abs(np.abs(u) - np.abs(v)) < 0.15) & (np.abs(u) < 0.42) & (np.abs(v) < 0.42)
        c[x] = CI
        return c
    return eye


EYE = polygon([(1.0, 0.25), (0.35, 0.75), (-0.75, 0.55), (-1.0, -0.05), (-0.45, -0.7), (0.6, -0.65)])


def golisopodmega(src, out):
    k = Kit(src, coat="shell")
    dome(k)
    lightning(k)
    torso(k)
    gauntlets(k)
    pod_arms(k)
    legs(k)
    tail(k)
    k.build(voxel=0.0062, smooth=4, band=0.005, wband=0.05)

    def lay(state):
        lk = eye_look(state, x_eye(), srgb(FACE), srgb(EYE_Y), line=0.2)
        for s in (1, -1):
            d = (0.62 * s, -1, 0.2)
            p, n = on_ell(FACE_C, FACE_R, d)
            col = lk if s > 0 else (lambda u, v, g=lk: g(-u, v))
            outl = EYE if s > 0 else polygon([(-u, v) for u, v in [(1.0, 0.25), (0.35, 0.75), (-0.75, 0.55),
                                                                    (-1.0, -0.05), (-0.45, -0.7), (0.6, -0.65)]])
            k.decal(p, n, (0.032, 0.027), col, eps=0.0018, rings=20, segs=56, outline=outl)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    golisopodmega(*args())
