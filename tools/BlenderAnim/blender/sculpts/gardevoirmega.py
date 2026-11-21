# Mega Gardevoir sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gardevoirmega.py -- <the export's Gardevoir-Mega.glb> <out.glb>
# Grown from gardevoir.py (which builds as it is read, so its way is copied here): the same white, green, horn red and
# eye red, the same face, mouth and large gentle eyes, the same bob of hair opened on the -X eye with dark grooves.
#
# What the Mega changes, from the references, largest mass first:
#   1. the gown becomes a great ball gown: a dome from just under the chest out to the ground, about 0.6 of the height
#      across, cut by deep creases into eight rounded panels whose ends touch the ground as rounded points, closed all
#      round (no slit, no green lining), the panels behind and toward +X running out into a long train on the ground;
#   2. the arms turn white (long gloves), slender, with a small flared cuff at each hand;
#   3. the white point beside the face becomes a large four-pointed white star on the +X side, its long point out;
#   4. the red fin through the chest grows into a bigger bow of two lobes, front and back;
#   5. the bob is a little rounder and shorter.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, slab, lathe, srgb, inside, smoothstep, unit, eye_look, args

src, dst = args()
k = Kit(src, coat="fur")

WHITE, SHADE, GREEN, GROOVE, HORN, RED, INKH = "#F4F5FA", "#E4E6F2", "#7FCB72", "#3F7A3A", "#E26878", "#D84A62", "#2A1418"
H = np.array([0, -0.03, 0.905])           # the face's middle
HC = np.array([0, 0.006, 0.942])         # the bob of hair's middle
HCR = np.array([0.15, 0.138, 0.124])


# The ball gown: a loose sheet closed all round (its seam behind, toward -X, away from the train), a dome from the
# waist to the ground, cut into eight rounded panels by deep creases, one crease at the very front; the hem rises a
# little at each crease so that the panels end in rounded points on the ground; the panels behind toward +X trail out
# into a long train lying on the ground.
N = 8
SEAM = math.pi + 0.55                     # the seam's angle, from the front toward +X
TRAIN = math.pi - 0.95                    # the train's way: behind and toward +X
TOP, R0, RW = 0.722, 0.04, 0.262         # the waist's height and radius, how far the dome swells out
def gown_at(u, v):
    t = v                                                  # 0 at the waist, 1 at the hem
    phi = SEAM + 0.004 + u * (2 * math.pi - 0.008)
    R = R0 + RW * np.clip(1 - (1 - t) ** 3, 0, 1) ** 0.62 - 0.012 * smoothstep(0.85, 1.0, t)
    lobe = np.abs(np.sin(N * phi / 2)) ** 0.6              # nought at a crease, one in a panel's middle
    crease = smoothstep(0.03, 0.25, t)
    rr = R * (1 - 0.15 * crease * (1 - lobe))
    d = (phi - TRAIN + math.pi) % (2 * math.pi) - math.pi
    w = np.exp(-(d / 0.85) ** 2)                           # how much of the train a place is
    rr = rr * (1 + 1.45 * w * smoothstep(0.25, 1.0, t) ** 1.5)
    z = TOP - (TOP - 0.006) * t
    z = z - w * 0.2 * smoothstep(0.4, 1.0, t)            # the train comes down to the ground sooner
    lift = 0.055 * (1 - lobe) ** 1.5 * smoothstep(0.82, 1.0, t)
    z = np.maximum(z, 0.006) + lift
    x = rr * np.sin(phi)
    y = -rr * np.cos(phi)
    return np.stack([x, y, z], axis=-1)
k.add("gown", sheet(gown_at, 200, 48, 0.012), WHITE, "body", fuse=False)
# a darker floor just inside the gown's top, so that nothing is seen through the gap round the waist
k.add("gownfloor", lathe((0, 0, TOP - 0.02), (0, 0, 1), [(0, -0.005), (0.06, -0.005), (0.06, 0.004), (0, 0.004)]),
      SHADE, "body", fuse=False)

# Two slim white legs inside the gown, their points seen under the crease at the front
k.add("leg", tube([(0.028, 0.0, 0.66), (0.032, -0.006, 0.42), (0.03, -0.012, 0.16), (0.024, -0.02, 0.012)],
                  [0.024, 0.02, 0.016, 0.009]), WHITE, "body", mirror=True)

# A slim white body and a thin neck, all white now
k.add("waist", ell((0, 0.0, 0.71), (0.04, 0.035, 0.06)), WHITE, "body")
k.add("chest", ell((0, 0.0, 0.766), (0.048, 0.04, 0.05)), WHITE, "body")
k.add("neck", tube([(0, 0.0, 0.79), (0, -0.008, 0.88)], [0.017, 0.014]), WHITE, "head")


# The red bow through the chest: the base's heart lobes, larger and thicker, front and back (loose, crisp)
def lobe(V):
    V = V.copy()
    V[:, 2] *= 1 - 0.55 * smoothstep(-0.3, 1.0, V[:, 1])
    V[:, 2] += 0.25 * smoothstep(0.2, 1.0, V[:, 1])
    return V
k.add("hornF", ell((0, -0.08, 0.772), (0.016, 0.075, 0.05), rot=(0, 0, 180), deform=lobe), HORN, "body",
      fuse=False)
k.add("hornB", ell((0, 0.08, 0.772), (0.016, 0.075, 0.05), deform=lobe), HORN, "body", fuse=False)

# Long slender white arms held forward and down, elbows bent, a small flared cuff and three fingers at each hand
SH, EL, WR = (0.042, 0.0, 0.782), (0.118, -0.06, 0.768), (0.185, -0.185, 0.752)
k.add("arm", tube([SH, (0.08, -0.022, 0.778), EL, (0.152, -0.122, 0.76), WR], [0.023, 0.019, 0.016, 0.015, 0.014]),
      WHITE, "arm{s}", mirror=True)
cd = unit(0.4, -0.85, -0.3)
k.add("cuff", lathe(np.array(WR) - cd * 0.012, cd, [(0, 0), (0.016, 0.0), (0.026, 0.03), (0.02, 0.034), (0, 0.02)],
                    seg=24), WHITE, "arm{s}", fuse=False, mirror=True)
HA = np.array(WR) + cd * 0.04
k.add("hand", ell(HA, (0.015, 0.017, 0.012), rot=(0, -20, 15)), WHITE, "arm{s}", fuse=False, mirror=True)
for i, (dx, dz) in enumerate(((-0.012, 0.0), (0.002, -0.004), (0.014, 0.002))):
    p0 = HA + np.array([dx * 0.5, -0.006, dz * 0.5])
    p1 = HA + np.array([dx * 1.3, -0.04, dz - 0.018])
    k.add(f"finger{i}", tube([p0, p1], [0.0065, 0.004]), WHITE, "arm{s}", fuse=False, mirror=True)

# The face: a white head standing forward into the hair's opening
k.add("head", ell((0, -0.026, 0.905), (0.072, 0.064, 0.072)), WHITE, "head")
k.add("face", ell((0, -0.046, 0.897), (0.062, 0.05, 0.062)), WHITE, "head")


# The bob, as Gardevoir's: a round mass, opened at the front on the face (more of it on -X), a lock down over the +X
# side of the face; a little shorter at its sides
def bob(U):
    x, y, z = U[:, 0].copy(), U[:, 1].copy(), U[:, 2].copy()
    rh = np.sqrt(x * x + y * y) + 1e-9
    zc = np.clip(-z, 0, 1)
    want = np.where(z < 0, (1 - zc ** 4) ** 0.25, rh) * (1 + 0.16 * smoothstep(0.5, 0.85, zc))
    s = np.where(z < 0, want / rh, 1)
    s = np.where(rh < 0.05, 1, s)
    x, y = x * s, y * s
    z = np.maximum(z, -0.82)
    top = 0.18 - 0.6 * ((U[:, 0] + 0.18) / 0.6) ** 2
    m = (smoothstep(-0.86, -0.6, U[:, 0]) * (1 - smoothstep(0.02, 0.18, U[:, 0]))
         * (1 - smoothstep(top - 0.12, top + 0.02, U[:, 2])) * smoothstep(0.05, 0.4, -U[:, 1]))
    y = y + m * (0.5 * (y - 0.45 + np.sqrt((y + 0.45) ** 2 + 0.03)) - y)
    return np.stack([x, y, z], axis=1)
k.add("hair", ell(HC, HCR, deform=bob, sub=5), GREEN, "head", fuse=False)


def on_bob(th, e, lift=1.01):
    U = np.stack([np.sin(th) * np.cos(e), -np.cos(th) * np.cos(e), np.sin(e)], axis=1)
    return HC + bob(U) * HCR * lift
def groove(name, th, e):
    pts = on_bob(np.array(th, float), np.array(e, float))
    k.add(name, tube(pts, [0.0022] * len(pts), seg=8, per=6), GROOVE, "head", fuse=False)
groove("part", [0.0, 0.2, 0.28, 0.3], [1.25, 0.85, 0.45, 0.1])
groove("lineR", [0.75, 0.95, 1.12, 1.25], [1.1, 0.7, 0.3, -0.1])
groove("lineL", [-0.75, -0.95, -1.12, -1.25], [1.1, 0.7, 0.3, -0.1])

# The white star on the +X side of the face, over the lock: four points, the long one out and a little up, a short
# one up, one down and one in toward the face; a thick plate turned toward the front and out
SC = np.array([0.112, -0.1, 0.896])
sn = unit(0.55, -1, 0.05)
sx = unit(1, 0.55, 0.0)                    # in the plate's plane, outward
sz = np.cross(sn, sx); sz = -sz if sz[2] < 0 else sz
STAR = [(0.105, 0.016), (0.022, 0.016), (0.0, 0.058), (-0.018, 0.012), (-0.05, -0.004), (-0.014, -0.014),
        (-0.004, -0.06), (0.02, -0.014)]
k.add("star", slab(STAR, SC, sx, sz, 0.02), WHITE, "head", fuse=False)

# A small calm mouth low on the face
INK, LID, IRIS = srgb(INKH), srgb(WHITE), srgb(RED)
MOUTH = srgb("#B8506A")
def mouth(u, v):
    c = np.tile(LID, (len(u), 1))
    c[(np.abs(v - (-0.1 + 0.4 * u * u)) < 0.28) & (np.abs(u) < 0.75)] = MOUTH
    return c
fm = unit(-0.12, -1, -0.6)
k.decal(H + fm * 0.07, fm, (0.01, 0.006), mouth, eps=0.003, rings=8, segs=32)

# The eyes: Gardevoir's own
DEEP, OUTL = srgb("#9A2238"), srgb("#3A1A22")
def eye(u, v):
    c = np.tile(LID, (len(u), 1))
    o = (u / 0.95) ** 2 + ((v + 0.02) / 0.92) ** 2
    ir = ((u + 0.04) / 0.62) ** 2 + ((v + 0.06) / 0.78) ** 2
    c[(o < 1) & (ir < 1)] = DEEP
    c[(o < 1) & (ir < 0.72)] = IRIS
    c[((u + 0.04) / 0.24) ** 2 + ((v + 0.04) / 0.36) ** 2 < 1] = INK
    c[((u - 0.14) / 0.17) ** 2 + ((v - 0.32) / 0.19) ** 2 < 1] = (1, 1, 1)
    c[((u + 0.24) / 0.08) ** 2 + ((v + 0.36) / 0.08) ** 2 < 1] = (1, 1, 1)
    c[(o < 1) & (o > 0.8 - 0.1 * np.clip(u, 0, 1)) & (v > 0.25 - 0.15 * u)] = OUTL
    return c
de = unit(0.42, -0.9, 0.08)
def eyes(state):
    k.decal(H + de * 0.07, de, (0.026, 0.031), eye_look(state, eye, LID, INK, line=0.15), eps=0.003, rings=20,
            segs=56, mirror=True)

k.build(voxel=0.005)
k.eye_states(eyes)
k.export(dst)
