# Comfey sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/comfey.py -- <the export's Comfey.glb> <out.glb>
#
# From the references: an upright oval lei standing in the plane of the face, its largest mass: eight round five-petalled
# flowers strung on a thin pale blue vine, each with a white star in its middle; going round from the top right they
# are white, magenta, yellow, red, white (at the bottom), pink, yellow and red (on the left). At the lei's top left the
# little green sprite: its face a round golden-orange flower's heart with small dark half-shut eyes and a smile, a
# frill of pale cream petals round its sides and chin, a cap of lobed green leaves over its crown that streams back
# into a long green tail laid along the lei's top to the right; its green body curls down the lei's left side from
# under the face to the red flower, holding the vine.
# (The task's "ring of flowers it floats inside": the pictures' sprite is part of the ring, at its top left, and its
# body runs down the ring's left side; the flowers are thick blobs of five round petals, fused.)
# Skeleton: body (0, 0, 0.203), head (-0.061, 0, 0.318): the ring's middle is the body, of radius 0.13.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, srgb, unit, eye_look, polygon, lumpy, args, outward

GREEN, GREEN_DARK = "#A8D86E", "#8CC458"
CREAM = "#F4E6A4"
FACE = "#F2A43A"
VINE = "#9CD0E8"
STAR = "#FAF6F2"
INK = srgb("#3A2418")
EYE = srgb("#2A1E1A")
GLINT = np.array([1.0, 0.92, 0.55])

C = np.array([0.0, 0.0, 0.203])       # the ring's middle
R = 0.13                              # its radius
FLOWERS = [(52, "#E6E4E8"), (18, "#D8508E"), (-14, "#F2D23E"), (-48, "#EC4A3C"), (-92, "#EEECEE"),
           (-128, "#EC7AB2"), (-160, "#F2D23E"), (166, "#EC4A3C")]
PETAL = 0.023                         # a flower's reach from its middle to a petal's middle
FC = np.array([-0.066, -0.016, 0.33])   # the face
FR = np.array([0.033, 0.03, 0.032])


def ring_at(deg, r=R, y=0.0):
    a = math.radians(deg)
    return C + np.array([math.cos(a) * r, y, math.sin(a) * r])


def flower(k, i, deg, colour):
    """Five round fused petals about a round middle, facing the front, on the ring."""
    c = ring_at(deg)
    t0 = math.radians(deg * 1.7 + 18)
    k.add(f"fl{i}mid", ell(c, (0.016, 0.012, 0.016), sub=3), colour, "body")
    for j in range(5):
        t = t0 + 2 * math.pi * j / 5
        p = c + np.array([math.cos(t), 0, math.sin(t)]) * PETAL
        k.add(f"fl{i}p{j}", ell(p, (0.02, 0.011, 0.02), rot=(0, -math.degrees(t), 0), sub=3), colour, "body")
    return c, t0


def star(t0):
    """A white five-armed star, its arms between the petals' notches... along the petals."""
    pts = []
    for j in range(10):
        t = t0 + math.pi * j / 5
        r = 1.0 if j % 2 == 0 else 0.3
        pts.append((r * math.cos(t), r * math.sin(t)))
    return pts


def eye_open(u, v):
    """Small dark half-shut eyes: dark below a flat lid, a warm glint."""
    c = np.tile(srgb(FACE), (len(u), 1))
    edge = 0.15 + 0.3 * (1 - u * u)
    body = (u * u + v * v < 1) & (v < edge)
    c[body] = EYE
    c[body & ((u - 0.25) ** 2 / 0.06 + (v - 0.05) ** 2 / 0.06 < 1)] = GLINT
    c[(np.abs(v - edge) < 0.14) & (u * u + v * v < 1.05)] = INK
    return c


def comfey(src, out):
    k = Kit(src, coat="leaf")
    # The vine under it all, pale blue, seen between the flowers
    pts = [ring_at(d, y=0.004) for d in range(60, -201, -10)]
    k.add("vine", tube(pts, [0.0058] * len(pts)), VINE, "body")
    stars = []
    for i, (deg, col) in enumerate(FLOWERS):
        stars.append((*flower(k, i, deg, col), "#B8B4C0" if col.startswith("#E") and col[1:3] in ("E6", "EE") else STAR))

    # The sprite: the body curling down the ring's left side from under the face to the red flower
    body = [FC + (0.004, 0.012, -0.03), ring_at(140, y=0.004), ring_at(156, y=0.0), ring_at(170, y=-0.004)]
    k.add("body", tube(body, [0.026, 0.02, 0.016, 0.012]), GREEN, "body")
    k.add("bulb", ell(FC + (0.002, 0.012, -0.034), (0.03, 0.026, 0.03)), GREEN, "head")
    # The face, a frill of cream petals round its sides and chin
    k.add("face", ell(FC, FR), FACE, "head")
    for j, t in enumerate((-60, -90, -120, -25, -155, 10, 170)):
        a = math.radians(t)
        p = FC + np.array([math.cos(a) * 0.034, 0.008, math.sin(a) * 0.032])
        k.add(f"frill{j}", ell(p, (0.017, 0.011, 0.014), rot=(0, -t, 0), sub=3), CREAM, "head")
    # The cap of lobed leaves over the crown, streaming into a long tail along the ring's top to the right
    k.add("cap", ell(FC + (0.004, 0.012, 0.03), (0.05, 0.036, 0.026), deform=lumpy(5, 1, 0.12)), GREEN, "head")
    for sx in (1, -1):
        k.add("lobe" + ("R" if sx > 0 else "L"), ell(FC + (0.043 * sx, 0.006, 0.014), (0.02, 0.022, 0.018), sub=3),
              GREEN, "head")
    tail = [FC + (0.02, 0.016, 0.034), FC + (0.06, 0.016, 0.03), (0.02, 0.012, 0.35), (0.06, 0.008, 0.335),
            (0.088, 0.004, 0.31)]
    k.add("tail", tube(tail, [0.024, 0.02, 0.016, 0.012, 0.0075]), GREEN, "head")
    k.paint(lambda P: (P[:, 2] > FC[2] + 0.03) * np.clip((P[:, 1] - 0.015) / 0.01, 0, 1), GREEN_DARK,
            on=["cap", "tail"])

    # A smile on the face, and the stars on the flowers, front and back
    sm = FC + np.array([0, -FR[1], -0.012])
    f = srgb(FACE)
    def smile(u, v):
        col = np.tile(f, (len(u), 1))
        curve = -0.25 + 0.75 * (1 - u * u)
        col[(np.abs(u) < 0.86) & (np.abs(v + curve - 0.25) < 0.34)] = INK
        return col
    k.decal(sm + (0, 0.004, 0), unit(0, -1, -0.35), (0.009, 0.0055), smile, eps=0.0015, rings=10, segs=40)
    for c, t0, sc in stars:
        for ny in (-1, 1):
            k.decal(c, (0, ny, 0), (0.024, 0.024), sc, up=(0, 0, 1), eps=0.0015,
                    outline=polygon(star(t0 if ny < 0 else math.pi - t0)), rings=12, segs=120)
    k.build(voxel=0.0034, smooth=4, band=0.004, wband=0.03)

    def eyes(state):
        for sx in (1, -1):
            d = unit(0.42 * sx, -0.88, 0.12)
            p = FC + d / math.sqrt(((d / FR) ** 2).sum())
            n = (p - FC) / FR ** 2
            look = eye_look(state, eye_open, srgb(FACE), INK, line=0.16)
            lk = look if sx > 0 else (lambda g: (lambda u, v: g(-u, v)))(look)
            k.decal(p, n, (0.0075, 0.009), lk, eps=0.0015, rings=20, segs=56)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    comfey(*args())
