# Yungoos sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/yungoos.py -- <the export's Yungoos.glb> <out.glb>
# From the references: a long low brown mongoose on short legs; a big round head, half the height, under a broad
# yellow cap of hair swept down over the forehead to a point between the eyes; small fierce black eyes under thick
# black brows; small round ears on the head's sides, pink inside; a small pink nose over a huge wide grin of white
# teeth meeting in a zigzag; a yellow chin, throat and underside along the whole body and tail; a thin yellow line
# along the back; three black claws on each foot; a long tail out behind ending in a yellow tuft of flame-like points.
# Written to be grown from (gumshoos.py keeps this frame: the grin, the eyes and brows, the claws, the spikes, the tuft).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, tri, unit, eye_look, polygon, args

BROWN, YELLOW, PINK, NOSE = "#B4705A", "#F2D060", "#E89AA0", "#E8707A"
BLACK, WHITE, MOUTH, CLAW = "#1C1616", "#F8F6F2", "#3A1A1C", "#24201E"
INK = "#3A2420"


# --- the line's own parts, shared with Gumshoos ----------------------------------------------------------------------
def spike(base, d, length, w, t, side=None):
    """A solid tapering point (a claw, a tuft of fur, a flame of the tail): a flat-faced hull, w wide, t thick."""
    base = np.array(base, float)
    d = unit(*d)
    if side is None:
        side = np.cross(d, (0, 0, 1.0))
        if np.linalg.norm(side) < 1e-3:
            side = np.array([1.0, 0, 0])
    side = np.array(side, float); side = side - d * (side @ d); side /= np.linalg.norm(side)
    face = np.cross(d, side)
    return hull([base + side * w, base - side * w, base + face * t, base - face * t,
                 base + d * length * 0.45 + side * w * 0.6, base + d * length * 0.45 - side * w * 0.6,
                 base + d * length * 0.45 + face * t * 0.6, base + d * length * 0.45 - face * t * 0.6, base + d * length])


def claws(k, name, front, spread, size, colour, bone):
    """Three black claws forward and down off a foot (front: the foot's front middle on +X), mirrored."""
    front = np.array(front, float)
    for j, dx in enumerate((-1, 0, 1)):
        a = front + np.array([dx * spread, 0, 0])
        k.add(f"{name}{j}", spike(a, (0.35 * dx, -1, -0.55), size, size * 0.38, size * 0.3), colour, bone,
              fuse=False, mirror=True, surface="shell")


def grin_look(teeth=3.5, amp=0.42, line=0.2, rim=0.16):
    """A wide grin: white teeth meeting in one zigzag across the middle, a dark rim round them (u, v in -1..1)."""
    W, D = srgb(WHITE), srgb(MOUTH)
    def look(u, v):
        c = np.tile(W, (len(u), 1))
        zig = amp * tri(u * teeth / 2 + 0.25)
        c[np.abs(v - zig) < line] = D
        r = (np.abs(u) ** 4 + np.abs(v) ** 4) ** 0.25
        c[r > 1 - rim] = D
        return c
    return look


def lay_grin(k, at, n, half, look, rings=24, segs=120):
    k.decal(at, n, half, look, eps=0.002, outline=polygon([(-1, -0.8), (-0.8, -1), (0.8, -1), (1, -0.8),
                                                           (1, 0.8), (0.8, 1), (-0.8, 1), (-1, 0.8)]),
            rings=rings, segs=segs)


def black_eye(pupil=BLACK, white=WHITE, glint=(0.18, 0.3, 0.26)):
    """A small fierce eye: black all through with a white glint up and out."""
    P, W = srgb(pupil), srgb(white)
    def look(u, v):
        c = np.tile(P, (len(u), 1))
        c[((u - glint[0]) / glint[2]) ** 2 + ((v - glint[1]) / glint[2]) ** 2 < 1] = W
        return c
    return look


def cut_oval(a, b, n=72):
    """The unit circle with its top cut by the line v = a + b u (u outward): the brow's slant."""
    pts = []
    for i in range(n):
        t = 2 * math.pi * i / n
        u, v = math.cos(t), math.sin(t)
        pts.append((u, min(v, a + b * u)))
    return pts


def lay_eyes(k, state, E, n, half, look, lid, ink, cut):
    """Both eyes by hand, u outward on each; the open and fierce eyes cut by the brow's line."""
    f = eye_look(state, look, srgb(lid), srgb(ink), line=0.17)
    for side in (1, -1):
        out_ = polygon([(side * a, b) for a, b in cut]) if state in ("Open", "Fierce") else None
        col = f if side == 1 else (lambda a, b, g=f: g(-a, b))
        k.decal(np.array(E) * (side, 1, 1), np.array(n) * (side, 1, 1), half, col, eps=0.002, outline=out_,
                rings=20, segs=56)


def brows(k, E, n, half, colour, lift=0.65, slope=0.5, width=0.32):
    """A thick black brow over each eye, sloping down toward the nose: a slanted bar laid above the eye."""
    for side in (1, -1):
        pts = [(-1, -width - slope), (1, -width + slope), (1, width + slope), (-1, width - slope)]
        if side < 0:
            pts = [(-a, b) for a, b in pts]
        nn = np.array(n) * (side, 1, 1)
        c = np.array(E) * (side, 1, 1) + np.array([0, 0, half[1] * lift])
        k.decal(c, nn, (half[0] * 1.25, half[1] * 0.5), colour, eps=0.0025,
                outline=polygon(pts), rings=8, segs=40)


def tuft(k, name, T, up, back, length, width, colour, bone, count=4):
    """A tail's end of flame-like points: a fan of thick tapering spikes from T, up and back."""
    T = np.array(T, float)
    up, back = unit(*up), unit(*back)
    for i in range(count):
        a = -0.35 + 1.1 * i / (count - 1)
        d = unit(*(math.cos(a) * back + math.sin(a) * up + up * 0.5))
        L = length * (1.0, 0.8, 0.95, 0.72)[i % 4]
        k.add(f"{name}{i}", spike(T - d * width * 0.6, d, L, width, width * 0.6, side=np.cross(d, unit(1, -0.3, 0))),
              colour, bone, fuse=False, surface="fur")


def ear(k, c, r, rot, colour, lining, bone="head"):
    """A small round ear, flat front to back, its pink lining painted on the front face (mirrored)."""
    k.add("ear", ell(c, r, rot=rot), colour, bone, mirror=True)
    c = np.array(c, float)
    k.paint(lambda P: inside(np.hypot(np.abs(P[:, 0]) - c[0], P[:, 2] - c[2] + r[2] * 0.1) - r[0] * 0.62, 0.0015)
            * (P[:, 1] < c[1] - r[1] * 0.3), lining, on=["ear"])


def along_y(P, pts):
    """A tail's centre height at each point's y (the tail runs back along y)."""
    pts = np.array(pts, float)
    return np.interp(P[:, 1], pts[:, 1], pts[:, 2]), np.interp(P[:, 1], pts[:, 1], pts[:, 0])


# --- Yungoos ---------------------------------------------------------------------------------------------------------
# The frame (joints: body (0, 0, 0.07), head (0, -0.1, 0.09), legs (+-0.03, -0.06, 0.05), hinds (+-0.03, 0.07, 0.05),
# tail (0, 0.1, 0.08))
HC = np.array([0.0, -0.122, 0.122])       # the head's middle: half the height, a tenth larger than measured
HR = np.array([0.05, 0.046, 0.05])
MC = HC + np.array([0, -0.032, -0.022])   # the muzzle that carries the grin
MR = np.array([0.04, 0.024, 0.026])
TAIL = [(0, 0.09, 0.07), (0.008, 0.16, 0.058), (0.026, 0.23, 0.052), (0.055, 0.29, 0.06), (0.078, 0.33, 0.082)]
TRAD = [0.03, 0.023, 0.019, 0.018, 0.02]


def hair_mask(HC, HR, base=0.32, rise=0.55, dip=0.42, dip_w=0.22, back=0.45, width=0.8):
    """The yellow cap of hair over the head: its lower edge rises to the sides, dips to a point between the eyes."""
    def mask(P):
        d = (P - HC) / HR
        ax = np.abs(d[:, 0])
        line = base + rise * ax ** 1.4 - dip * np.clip(1 - ax / dip_w, 0, 1) * (d[:, 1] < 0) + back * np.clip(d[:, 1], 0, None)
        near = np.linalg.norm(d / 1.5, axis=1) < 1
        return inside(line - d[:, 2], 0.025) * inside(ax - width, 0.04) * near
    return mask


def yungoos(src, out):
    k = Kit(src, coat="fur")

    # The long low body: chest, middle and rump, the belly low over short legs
    k.add("chest", ell((0, -0.07, 0.072), (0.04, 0.046, 0.04)), BROWN, "body")
    k.add("middle", ell((0, 0.0, 0.066), (0.037, 0.07, 0.034)), BROWN, "body")
    k.add("rump", ell((0, 0.072, 0.066), (0.038, 0.044, 0.036)), BROWN, "body")
    k.add("neck", tube([(0, -0.07, 0.085), (0, -0.098, 0.1), (0, -0.115, 0.108)], [0.032, 0.033, 0.034]), BROWN, "head")

    # Short legs: a shoulder or haunch fused over each, a stub of shin, a small foot
    for nm, y, bone, top in (("fore", -0.062, "leg{s}", (0.02, 0.024, 0.026)), ("hind", 0.07, "hind{s}", (0.022, 0.028, 0.03))):
        k.add(nm + "top", ell((0.03, y, 0.052), top), BROWN, bone, mirror=True)
        fy = y - 0.012 if nm == "fore" else y - 0.004
        k.add(nm + "shin", tube([(0.03, y, 0.046), (0.031, fy, 0.014)], [0.016, 0.014]), BROWN, bone, mirror=True)
        k.add(nm + "foot", ell((0.031, fy - 0.006, 0.008), (0.016, 0.02, 0.009)), BROWN, bone, mirror=True)
        claws(k, nm + "claw", (0.031, fy - 0.024, 0.007), 0.008, 0.016, CLAW, bone)

    # The big round head, the muzzle forward and low, small round ears on the sides
    k.add("head", ell(HC, HR), BROWN, "head")
    k.add("muzzle", ell(MC, MR), BROWN, "head")
    k.add("hair", ell(HC + (0, -0.008, 0.024), (0.046, 0.04, 0.032)), YELLOW, "head")
    ear(k, HC + (0.046, 0.004, 0.026), (0.017, 0.008, 0.018), (0, 0, -20), BROWN, PINK)

    # The long tail out behind, swung out to +X, its underside yellow
    k.add("tail", tube(TAIL, TRAD, seg=20), BROWN, "tail")

    # The yellow: the cap of hair, the chin, throat and underside, the line along the back, the tail's underside
    hair = hair_mask(HC, HR)
    k.paint(hair, YELLOW)
    k.paint(lambda P: 1 - hair(P), BROWN, on=["hair"])
    def under(P):
        z = P[:, 2]
        chin = inside(z - (MC[2] - 0.004), 0.002) * (P[:, 1] < HC[1] + 0.02) * inside(np.abs(P[:, 0]) - 0.034, 0.003)
        throat = (P[:, 1] < -0.06) * inside(np.abs(P[:, 0]) - 0.022, 0.002) * (z < 0.1) * (P[:, 1] < -0.04 - 0.4 * (z - 0.06))
        belly = inside(z - (0.05 - 0.05 * np.clip(-P[:, 1] - 0.05, 0, None)), 0.002)
        return np.clip(chin + throat + belly, 0, 1)
    k.paint(under, YELLOW, on=["chest", "middle", "rump", "neck", "head", "muzzle"])
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.006, 0.0012) * (P[:, 1] > -0.035) * (P[:, 1] < 0.12)
            * (P[:, 2] > 0.09), YELLOW, on=["chest", "middle", "rump"])
    def tail_under(P):
        zc, xc = along_y(P, TAIL)
        return np.maximum(inside(P[:, 2] - (zc - 0.003), 0.0015) * (P[:, 1] > 0.1),
                          inside(0.302 - P[:, 1], 0.003))
    k.paint(tail_under, YELLOW, on=["tail"])
    tuft(k, "flame", TAIL[-1], (0, 0, 1), (0.45, 0.89, 0), 0.075, 0.019, YELLOW, "tail", count=5)

    # The face: a small pink nose over a huge grin of white teeth, a thick black brow over each eye
    k.add("nose", ell(MC + (0, -MR[1] * 0.82, MR[2] * 0.62), (0.009, 0.007, 0.007), rot=(15, 0, 0)), NOSE, "head",
          fuse=False, surface="skin")
    lay_grin(k, MC + (0, -MR[1] * 0.97, -0.004), unit(0, -1, -0.1), (0.033, 0.0145), grin_look())
    de = unit(0.42, -0.86, 0.22)
    E = HC + de / np.linalg.norm(de / HR) + np.array([0, -0.001, 0.0])
    half = (0.0095, 0.0105)
    brows(k, E, de, half, BLACK, lift=0.75, slope=0.42, width=0.34)

    k.build(voxel=0.0021, smooth=4, band=0.003, wband=0.025)

    look = black_eye()
    cut = cut_oval(0.42, 0.35)
    k.eye_states(lambda s: lay_eyes(k, s, E, de, half, look, BROWN, INK, cut))
    k.export(out)


if __name__ == "__main__":
    yungoos(*args())
