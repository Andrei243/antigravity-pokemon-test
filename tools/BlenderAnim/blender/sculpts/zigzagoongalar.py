# Zigzagoon-Galar sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/zigzagoongalar.py -- <the export's Zigzagoon-Galar.glb> <out.glb>
# Zigzagoon's frame (zigzagoon.py: its spikes, feet, zigzag bands and nose) in Galar's colours. From the references:
# the same low bristly raccoon, but black and white: a big white shaggy head and mane over the shoulders, a black band
# across the eyes that spreads into a black star round each eye, half-lidded pink eyes with dark pupils, a black nose
# on a short white muzzle, a great open mouth (dark inside, a zigzag row of white teeth along its top) with a long pink
# tongue hanging out over the chin; large white jagged ears; the coat in black and white zigzags with black legs and
# black paws with white claws; a bushy black tail with a pale zigzag band. Linoone-Galar grows from this.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, slab, srgb, inside, unit, eye_look, polygon, outward, args, sibling

z_ = sibling("zigzagoon")

WHITE, BLACK, MASK, GREYBAND = "#ECECEE", "#2E2C32", "#1C1A1F", "#BDBDC3"
CLAW, NOSE, MOUTH, TONGUE = "#F4F4F2", "#141216", "#3E1C26", "#E58BA6"
IRIS, PUPIL, GLINT, INK = "#E4306C", "#3A0A1C", "#FFFFFF", "#141216"

# Zigzagoon's frame, the head a tenth larger (the Galar head and mane are the design's first mass)
HC = np.array([0.0, -0.165, 0.185])
HR = np.array([0.104, 0.097, 0.097])
BC, BR = z_.BC, z_.BR
LX, FY, HY = z_.LX, z_.FY, z_.HY


def star(n=5, inner=0.5, turn=90.0):
    pts = []
    for i in range(2 * n):
        a = math.radians(turn + i * 180.0 / n)
        r = 1.0 if i % 2 == 0 else inner
        pts.append((r * math.cos(a), r * math.sin(a)))
    return polygon(pts)


def mouth_outline():
    """A wide D: round below, its top a zigzag of four white teeth biting down into the dark."""
    pts = []
    for th in np.linspace(math.pi, 2 * math.pi, 15):
        pts.append((0.95 * math.cos(th), 0.95 * math.sin(th)))
    top = [(0.95, 0.15), (0.78, 0.62), (0.58, 0.2), (0.38, 0.66), (0.19, 0.22), (0.0, 0.68),
           (-0.19, 0.22), (-0.38, 0.66), (-0.58, 0.2), (-0.78, 0.62), (-0.95, 0.15)]
    return polygon(pts + top)


def zigzagoon_galar(src, out_path):
    k = Kit(src, coat="fur")

    # The long low body, its belly near the ground, the haunches fused in (zigzagoon.py's)
    k.add("body", ell(BC, BR), BLACK, "body")
    k.add("chest", ell((0, -0.085, 0.15), (0.108, 0.085, 0.1)), WHITE, "body")
    k.add("rump", ell((0, 0.13, 0.145), (0.1, 0.08, 0.09)), BLACK, "body")
    k.add("belly", ell((0, 0.02, 0.085), (0.088, 0.15, 0.058)), BLACK, "body")
    for name, (x, y), bone in (("leg", (LX, FY), "leg{s}"), ("hind", (LX, HY), "hind{s}")):
        k.add(name, tube([(x - 0.008, y, 0.1), (x, y - 0.004, 0.035)], [0.042, 0.036]), BLACK, bone, mirror=True)
    z_.feet(k, [("leg", (LX, FY, 0), "leg{s}"), ("hind", (LX, HY, 0), "hind{s}")], (0.036, 0.044, 0.022), BLACK, CLAW,
            None)

    # The big round white head and its short muzzle, a chin under the open mouth
    k.add("head", ell(HC, HR), WHITE, "head")
    k.add("snout", ell(HC + (0, -0.09, -0.015), (0.048, 0.042, 0.036)), WHITE, "head")
    k.add("chin", ell(HC + (0, -0.06, -0.072), (0.064, 0.055, 0.04)), WHITE, "head")

    # The coat's zigzag bands round the body, white over black; the mane white over the shoulders
    bands = z_.zigzag((0, 1, 0.25), (0, -0.07, 0.14), 0.085, 0.02, 7)
    k.paint(lambda P: bands(P) * (P[:, 1] > -0.04) * (P[:, 2] > 0.085), WHITE, on=["body", "rump"])
    k.paint(lambda P: inside(P[:, 1] + 0.035 + 0.02 * np.abs(np.sin(P[:, 0] * 60)), 0.004) * (P[:, 2] > 0.1),
            WHITE, on=["body", "chest"])

    def col_at(p):
        if p[1] < -0.035:
            return WHITE
        return WHITE if bands(np.array([p]))[0] > 0.5 else BLACK
    for yy in np.linspace(-0.06, 0.17, 6):
        for az in np.linspace(-1.3, 1.3, 7):
            x, z = BR[0] * 0.97 * math.sin(az), 0.14 + BR[2] * 0.97 * math.cos(az)
            yl = yy + 0.012 * (abs(az) > 0.6)
            base = np.array([x, yl, z])
            d = unit(0.55 * math.sin(az), 1.0, 0.35 * math.cos(az) - 0.1)
            k.add(f"fur{len(k.parts)}", z_.spike(base - d * 0.01, d, 0.06, 0.024, 0.016), col_at(base), "body",
                  fuse=False, surface="fur")

    # A shaggy white mane: tufts round the back of the head and down the cheeks to the chest
    for i, az in enumerate(np.linspace(-1.4, 1.4, 7)):
        base = HC + np.array([HR[0] * 0.9 * math.sin(az), 0.035, HR[2] * 0.9 * math.cos(az)])
        d = unit(0.6 * math.sin(az), 1.0, 0.55 * math.cos(az))
        k.add(f"nape{i}", z_.spike(base - d * 0.01, d, 0.058, 0.026, 0.017), WHITE, "head", fuse=False,
              surface="fur")
    for s in (1, -1):
        for j, (dy, dz, ln) in enumerate(((-0.03, 0.0, 0.05), (-0.005, -0.045, 0.055), (0.01, -0.09, 0.05))):
            base = HC + np.array([0.09 * s, dy, dz])
            k.add(f"cheek{'R' if s > 0 else 'L'}{j}", z_.spike(base, (s, 0.45, -0.3 - j * 0.15), ln, 0.026, 0.017),
                  WHITE, "head", fuse=False, surface="fur")

    # Large white ears, jagged along their tops, standing up and tilted out
    EAR = [(-0.036, 0.0), (0.036, 0.0), (0.05, 0.042), (0.032, 0.04), (0.036, 0.074), (0.014, 0.056), (0.0, 0.09),
           (-0.014, 0.056), (-0.036, 0.074), (-0.032, 0.04), (-0.05, 0.042)]
    for s in (1, -1):
        o = np.array([0.058 * s, -0.152, 0.262])
        ex = unit(s, 0.0, -0.5)
        ez = unit(0.5 * s, 0.12, 1.0)
        k.add(f"ear{'R' if s > 0 else 'L'}", slab(EAR, o, ex, ez, 0.022), WHITE, "ear" + ("R" if s > 0 else "L"),
              fuse=False, surface="fur")

    # The bushy black tail, up and back, swung out to the model's right, one pale zigzag band
    TB, TD = np.array([0.0, 0.17, 0.18]), unit(0.35, 0.75, 0.6)
    k.add("tail", ell(TB + TD * 0.1, (0.072, 0.067, 0.115), rot=(-40, 0, -25)), BLACK, "tail")
    tailband = z_.zigzag(TD, TB + TD * 0.035, 0.12, 0.015, 6, share=0.3)
    k.paint(lambda P: tailband(P) * inside(np.linalg.norm(P - TB, axis=1) - 0.16), GREYBAND, on=["tail"])
    a = np.cross(TD, (0, 0, 1.0)); a /= np.linalg.norm(a); b = np.cross(TD, a)
    n = 0
    for t in (0.05, 0.1, 0.15):
        for th in np.linspace(0, 2 * math.pi, 7, endpoint=False):
            base = TB + TD * t + 0.06 * (math.cos(th) * a + math.sin(th) * b) * (1 - (t - 0.1) ** 2 * 30)
            d = unit(*(TD * 0.9 + 0.6 * (math.cos(th) * a + math.sin(th) * b)))
            col = GREYBAND if tailband(np.array([base]))[0] > 0.5 else BLACK
            k.add(f"tailfur{n}", z_.spike(base - d * 0.01, d, 0.055, 0.024, 0.016), col, "tail", fuse=False,
                  surface="fur"); n += 1
    k.add("tailtip", z_.spike(TB + TD * 0.2, TD, 0.05, 0.03, 0.02), BLACK, "tail", fuse=False, surface="fur")

    # The black band across the eyes, round the head
    def band(P):
        d = (P - HC) / HR
        return inside(np.abs(d[:, 2] - 0.2 - 0.12 * np.abs(d[:, 0])) - 0.2, 0.03) * (d[:, 1] < 0.15) * \
            (np.abs(d[:, 0]) > 0.0) * inside(np.linalg.norm(d, axis=1) - 1.4)
    k.paint(band, MASK, on=["head"])

    # A black star round each eye, its points up out of the band (in every eye state, so before build)
    de = unit(0.52, -0.82, 0.18)
    E = HC + de / np.linalg.norm(de / HR) + (0, 0, 0.004)
    for s in (1, -1):
        k.decal(E * np.array([s, 1, 1.0]), de * np.array([s, 1, 1.0]), (0.058, 0.058), MASK, eps=0.004,
                outline=star(turn=90 + 12 * s), rings=16, segs=60)

    # The black nose on the muzzle; the great open mouth under it, teeth along its top
    k.add("nose", ell(HC + (0, -0.133, -0.004), (0.024, 0.016, 0.017)), NOSE, "head", fuse=False, surface="skin")
    CM = srgb(MOUTH)
    k.decal(HC + (0, -0.106, -0.06), unit(0, -1, -0.35), (0.058, 0.042), CM, eps=0.0035, outline=mouth_outline(),
            rings=18, segs=96)

    # The long pink tongue, out of the mouth and down over the chin, flat across
    tg = [HC + (0, -0.098, -0.07), HC + (0, -0.122, -0.098), HC + (0.004, -0.126, -0.125), HC + (0.008, -0.118, -0.148)]
    k.add("tongue", outward(blade(tg, [0.022, 0.028, 0.028, 0.022], (0, -1, 0.15), 0.42)), TONGUE, "head", fuse=False,
          surface="skin")

    k.build(voxel=0.006, smooth=3, band=0.006)

    WH, IR, PU, KK, MK = srgb(WHITE), srgb(IRIS), srgb(PUPIL), srgb(INK), srgb(MASK)
    def look(u, v):
        """Half-lidded: a pink eye, its dark pupil upright, cut flat across its top by the black lid."""
        c = np.tile(IR, (len(u), 1))
        c[np.hypot((u + 0.05) / 0.3, (v + 0.12) / 0.52) < 1] = PU
        c[np.hypot(u - 0.3, v - 0.05) < 0.16] = (1, 1, 1)
        c[np.hypot(u, v) > 0.88] = KK
        c[v > 0.32 - 0.12 * u] = MK
        c[np.abs(v - (0.32 - 0.12 * u)) < 0.11] = KK
        return c

    def eyes(state):
        f = eye_look(state, look, MK, srgb("#8A8A90"), line=0.16)
        k.decal(E, de, (0.031, 0.031), f, eps=0.0065, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out_path)


if __name__ == "__main__":
    zigzagoon_galar(*args())
