# Ponyta-Galar sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ponytagalar.py -- <the export's Ponyta-Galar.glb> <out.glb>
# Kin of ponyta.py (the same pony: barrel, neck, head and muzzle, pointed ears, eyes on the head's sides), its flames
# turned into cotton candy. From the pictures: a white pony with a lavender shade under the chest; a short dark violet
# horn with a white spiral band; pointed ears lined dark violet; big violet eyes under a dark lid with a white glint;
# a fluffy mane of fused puffs over the crown and down the nape, lilac above and turquoise below with a periwinkle band
# between, and turquoise hooked tufts curling out of it; a tail as large as the trunk, a cloud of the same colours;
# lilac fluffy cuffs over dark violet hooves. Built to be grown from (rapidashgalar.py takes its frame through sibling).
# The skeleton: body (0, 0.02, 0.5), head (0, -0.18, 0.6), ears (+-0.04, -0.27, 0.86), mane (0, -0.24, 0.82),
# legs (+-0.07, -0.16, 0.42), hinds (+-0.07, 0.2, 0.42), tail (0, 0.24, 0.56).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, lathe, ring_band, lumpy, srgb, inside, smoothstep, unit, eye_look,
                       outward, args)

WHITE, SHADE, NOSTRIL = "#F7F4FA", "#DCD4E6", "#B8A8C4"
LILAC, PERI, TURQ = "#EBCBF3", "#B7B2EC", "#97E7D0"
HOOF, HORN, BAND, EARIN = "#4A2864", "#4B2A6C", "#F4EEF8", "#56306F"
IRIS, IRISD, PUPIL, LIDC = "#9A72E2", "#5E3AA8", "#2A1340", "#3A1E4E"

H = np.array([0, -0.312, 0.808])            # the head's centre
HR = np.array([0.084, 0.106, 0.082])
LEG_X, FORE_Y, HIND_Y = 0.071, -0.16, 0.205


# --- the trunk, neck and head (ponyta.py's, on this skeleton) -------------------------------------------------------
def trunk(k, Z=0.52, chest=-0.135, rump=0.17, rx=0.122, rz=0.145, col=WHITE):
    k.add("barrel", ell((0, 0.02, Z), (rx, 0.22, rz)), col, "body")
    k.add("chest", ell((0, chest, Z + 0.012), (rx - 0.008, 0.1, rz + 0.004)), col, "body")
    k.add("rump", ell((0, rump, Z + 0.01), (rx - 0.004, 0.105, rz - 0.008)), col, "body")


def neck(k, pts, radii, col=WHITE):
    k.add("neck", tube(pts, radii), col, "head")


def head(k, h=H, hr=HR, muzzle=((0, -0.398, 0.772), (0.06, 0.074, 0.057)), rot=(0, 0, 0), col=WHITE):
    k.add("head", ell(h, hr), col, "head")
    k.add("muzzle", ell(muzzle[0], muzzle[1], rot=rot), col, "head")


def ears(k, root, tip, radii=(0.034, 0.026, 0.004), col=WHITE):
    root, tip = np.array(root, float), np.array(tip, float)
    mid = root + (tip - root) * 0.5 + np.array([0.008, 0.004, 0])
    k.add("ear", blade([root, mid, tip], list(radii), (0, 1, 0), 0.5), col, "ear{s}", mirror=True)


def ear_linings(k, c, n, half, up, col=EARIN):
    k.decal(c, n, half, col, up=up, eps=0.003, rings=6, segs=28, mirror=True)


def horn(k, base, d, length, r0, bands=((0.42, 0.012),), col=HORN, band=BAND):
    """A solid cone of a horn from the forehead (a lathe) with white spiral bands, each (place along it, width)."""
    d = unit(*d)
    prof = [(0, -0.01), (r0, -0.01), (r0 * 0.93, length * 0.15), (r0 * 0.7, length * 0.45), (r0 * 0.42, length * 0.75),
            (r0 * 0.14, length * 0.95), (0, length)]
    k.add("horn", lathe(base, d, prof, seg=36), col, "head", fuse=False, surface="shell")
    for i, (t, w) in enumerate(bands):
        r = np.interp(t * length, [p[1] for p in prof[1:-1]], [p[0] for p in prof[1:-1]])
        c = np.array(base, float) + d * t * length
        k.add(f"hornband{i}", ring_band(c, d, r + 0.003, w, thick=0.005), band, "head",
              fuse=False, surface="shell")


# --- legs that are no stilts: shoulders and thighs fused over the upper legs, cuffs of cotton over the hooves --------
def legs(k, x=LEG_X, fore=FORE_Y, hind=HIND_Y, top=0.43, col=WHITE, r=(0.06, 0.044, 0.037, 0.036),
         shoulder=(0.062, 0.075, 0.1), thigh=(0.066, 0.1, 0.115)):
    k.add("shoulder", ell((x - 0.004, fore + 0.008, top + 0.01), shoulder), col, "leg{s}", mirror=True)
    k.add("fore", tube([(x, fore, top), (x + 0.002, fore - 0.006, top * 0.6), (x + 0.002, fore, top * 0.3),
                        (x + 0.002, fore - 0.003, 0.05)], list(r)), col, "leg{s}", mirror=True)
    k.add("thigh", ell((x - 0.004, hind + 0.012, top + 0.02), thigh), col, "hind{s}", mirror=True)
    k.add("hind", tube([(x, hind + 0.01, top), (x + 0.002, hind + 0.02, top * 0.6), (x + 0.002, hind + 0.004, top * 0.3),
                        (x + 0.002, hind, 0.05)], list(r)), col, "hind{s}", mirror=True)


def hooves(k, x=LEG_X, fore=FORE_Y, hind=HIND_Y, r=0.037, h=0.05, col=HOOF):
    for name, y, bone in (("hoof", fore - 0.003, "leg{s}"), ("hhoof", hind, "hind{s}")):
        k.add(name, lathe((x + 0.002, y - 0.006, 0.0), (0, 0, 1), [(0, 0.0), (r * 0.95, 0.0), (r, 0.006),
                                                                   (r * 0.96, h * 0.7), (r * 0.85, h), (0, h)],
                          seg=32), col, bone, fuse=False, mirror=True, surface="shell")


def cuffs(k, x=LEG_X, fore=FORE_Y, hind=HIND_Y, z=0.077, ring=0.036, ball=0.036, n=6, col=LILAC, seed=3):
    """A cuff of cotton round each leg over the hoof: balls fused round the leg, jittered so they don't read as petals."""
    rs = np.random.RandomState(seed)
    for name, y, bone in (("cuff", fore - 0.003, "leg{s}"), ("cuffh", hind, "hind{s}")):
        for i in range(n):
            a = 2 * math.pi * (i + rs.rand() * 0.5) / n
            c = (x + 0.002 + math.cos(a) * ring, y + math.sin(a) * ring, z + rs.uniform(-0.012, 0.018))
            s = ball * rs.uniform(0.85, 1.12)
            k.add(f"{name}{i}", ell(c, (s, s, s * 0.9), sub=3), col, bone, mirror=True)


# --- cotton: a cloud of fused puffs; the lower part painted turquoise -----------------------------------------------
def cloud(k, name, c, r, bone, seed, lumps=10, small=0.45, spread=0.75, zr=(-0.5, 0.75), col=LILAC):
    c, r = np.array(c, float), np.array(r, float)
    k.add(name, ell(c, r, deform=lumpy(5, 3, 0.06)), col, bone)
    rs = np.random.RandomState(seed)
    m = min(r)
    for i in range(lumps):
        a = 2 * math.pi * (i + rs.rand() * 0.6) / lumps
        zz = rs.uniform(*zr)
        d = np.array([math.cos(a) * math.sqrt(1 - zz * zz), math.sin(a) * math.sqrt(1 - zz * zz), zz])
        s = m * small * rs.uniform(0.85, 1.2)
        k.add(f"{name}{i}", ell(c + d * r * spread, (s, s, s * 0.95), sub=3), col, bone)


def wisp(k, name, base, d, length, r0, bone, curl=(0, 0, 1), bend=0.35, col=TURQ):
    """A hooked tuft of cotton curling out of a cloud: a thick tapering horn of fluff, its tip bent toward curl."""
    d = unit(*d); cu = np.array(curl, float); cu = cu - d * (cu @ d); cu /= np.linalg.norm(cu)
    b = np.array(base, float)
    pts = [b, b + d * length * 0.45, b + d * length * 0.8 + cu * length * bend * 0.4, b + d * length * 0.95 + cu * length * bend]
    k.add(name, outward(blade(pts, [r0, r0 * 0.78, r0 * 0.45, r0 * 0.12], np.cross(d, cu), 0.62)), col, bone,
          fuse=False, surface="fur")


def band_paint(k, on, lower, upper, wave=0.016, period=0.06, soft=0.008):
    """Turquoise below the line z = lower, periwinkle between lower and upper; each line waves a little."""
    def w(P):
        return wave * np.sin(P[:, 0] / period * 2.1 + P[:, 1] / period * 1.3)
    k.paint(lambda P: inside(P[:, 2] - upper - w(P), soft), PERI, on=on)
    k.paint(lambda P: inside(P[:, 2] - lower - w(P), soft), TURQ, on=on)


# --- the eyes: big and violet under a dark lid, a white glint high and to the front (u runs toward the back) --------
def pony_eye(iris=IRIS, irisd=IRISD, pupil=PUPIL, lid=LIDC, white="#FBF8FF", lidw=0.17):
    IR, ID, PU, LI, WH = srgb(iris), srgb(irisd), srgb(pupil), srgb(lid), srgb(white)
    def look(u, v):
        c = np.tile(WH, (len(u), 1))
        ir = ((u + 0.14) / 0.68) ** 2 + ((v + 0.02) / 0.8) ** 2
        c[ir < 1] = ID
        c[(ir < 0.62) & (v < 0.25)] = IR
        c[((u + 0.12) / 0.3) ** 2 + ((v + 0.02) / 0.38) ** 2 < 1] = PU
        c[((u + 0.36) / 0.2) ** 2 + ((v - 0.34) / 0.22) ** 2 < 1] = (1, 1, 1)
        c[((u + 0.0) / 0.12) ** 2 + ((v + 0.48) / 0.1) ** 2 < 1] = (0.85, 0.8, 1.0)
        top = 0.74 + 0.08 * u - 0.12 * u * u
        c[v > top - lidw] = LI
        c[np.sqrt(u * u + v * v) > 0.93] = LI
        return c
    return look


def nostrils(k, c, n, mouth_c, mouth_n, col=NOSTRIL):
    k.decal(c, n, (0.009, 0.007), col, eps=0.003, rings=4, segs=20, mirror=True)
    k.decal(mouth_c, mouth_n, (0.024, 0.0045), col, eps=0.003, rings=3, segs=20, mirror=True)


def ponyta_galar(src, out):
    k = Kit(src, coat="fur")
    trunk(k)
    neck(k, [(0, -0.15, 0.56), (0, -0.225, 0.665), (0, -0.275, 0.765)], [0.105, 0.09, 0.078])
    head(k)
    ears(k, (0.04, -0.29, 0.85), (0.112, -0.29, 0.955))
    horn(k, (0, -0.372, 0.862), (0, -0.32, 1.0), 0.105, 0.025, bands=((0.4, 0.013),))
    legs(k)
    hooves(k)
    cuffs(k)
    # a lavender shade under the chest and belly
    k.paint(lambda P: inside(P[:, 2] - 0.43, 0.03) * inside(np.abs(P[:, 1] - 0.02) - 0.3, 0.02), SHADE,
            on=["barrel", "chest", "rump"])

    # the mane: a cloud over the crown behind the horn, swelling down the nape to the withers
    cloud(k, "mane", (0, -0.215, 0.875), (0.095, 0.11, 0.085), "mane", 11, lumps=9)
    for i, (c, r) in enumerate((((0, -0.2, 0.79), (0.085, 0.085, 0.08)), ((0, -0.15, 0.7), (0.088, 0.08, 0.08)),
                                ((0, -0.1, 0.62), (0.08, 0.075, 0.07)))):
        cloud(k, f"manen{i}", c, r, "mane" if i == 0 else "head", 20 + i, lumps=7, spread=0.8)
    # turquoise locks hanging down each side of the neck, toward the chest
    for i, (c, r) in enumerate((((0.075, -0.21, 0.69), (0.045, 0.05, 0.07)), ((0.08, -0.17, 0.6), (0.04, 0.05, 0.06)))):
        k.add(f"manelock{i}", ell(c, r, deform=lumpy(4, 3, 0.08)), LILAC, "head", mirror=True)
    band_paint(k, ["mane"], 0.725, 0.75)

    # the tail: as large as the trunk, a cloud streaming back and a little up from the rump
    k.add("tailroot", tube([(0, 0.25, 0.57), (0, 0.32, 0.6), (0, 0.38, 0.63)], [0.055, 0.07, 0.09]), LILAC, "tail")
    cloud(k, "tail", (0, 0.47, 0.67), (0.14, 0.17, 0.17), "tail", 31, lumps=12, small=0.5)
    cloud(k, "tailb", (0, 0.5, 0.5), (0.11, 0.13, 0.1), "tail", 32, lumps=8, small=0.5, zr=(-0.7, 0.3))
    band_paint(k, ["tail"], 0.585, 0.61)
    # the cuffs periwinkle below
    k.paint(lambda P: inside(P[:, 2] - 0.062, 0.005), PERI, on=["cuff"])

    # turquoise hooked tufts curling out of the mane and the tail
    for i, (b, d, L, cu) in enumerate((((0.0, -0.27, 0.94), (-0.1, -0.2, 1), 0.08, (0, -1, 0)),
                                       ((0.05, -0.23, 0.92), (0.8, 0.1, 0.6), 0.07, (0, 0, 1)),
                                       ((-0.05, -0.23, 0.92), (-0.8, 0.1, 0.6), 0.07, (0, 0, 1)),
                                       ((0.05, -0.12, 0.74), (0.9, 0.3, 0.2), 0.065, (0, 0, 1)),
                                       ((-0.05, -0.12, 0.74), (-0.9, 0.3, 0.2), 0.065, (0, 0, 1)))):
        wisp(k, f"wispm{i}", b, d, L, 0.022, "mane" if i < 3 else "head", cu)
    for i, (b, d, L, cu) in enumerate((((0.03, 0.43, 0.81), (0.15, -0.25, 1), 0.075, (0, -1, 0)),
                                       ((-0.03, 0.52, 0.81), (-0.2, 0.3, 1), 0.07, (0, 1, 0)),
                                       ((0.11, 0.52, 0.5), (1, 0.3, -0.3), 0.075, (0, 0, 1)),
                                       ((-0.11, 0.52, 0.5), (-1, 0.3, -0.3), 0.075, (0, 0, 1)),
                                       ((0.0, 0.62, 0.52), (0, 1, -0.2), 0.08, (0, 0, 1)))):
        wisp(k, f"wispt{i}", b, d, L, 0.024, "tail", cu)

    # the face: nostrils and a mouth, the ears' dark linings
    nostrils(k, (0.022, -0.465, 0.785), unit(0.25, -1, 0.1), (0.032, -0.448, 0.745), unit(0.6, -1, -0.3))
    ear_linings(k, (0.074, -0.305, 0.905), unit(0.35, -1, 0.1), (0.014, 0.034), unit(0.5, 0, 0.85))

    k.build(voxel=0.0072, band=0.004)

    de = unit(0.76, -0.56, 0.2)
    E = H + de / np.linalg.norm(de / HR) + np.array([0, -0.008, 0.004])
    look = pony_eye()
    def eyes(state):
        k.decal(E, de, (0.036, 0.042), eye_look(state, look, srgb(WHITE), srgb(LIDC)), eps=0.005, rings=20, segs=56,
                mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    ponyta_galar(*args())
