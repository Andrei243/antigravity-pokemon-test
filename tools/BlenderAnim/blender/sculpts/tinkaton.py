# Tinkaton sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Tinkatink's script (tinkatink.py, imported: its colours, on_ell, the prisms, fists, cheeks and lidded eyes) and
# Tinkatuff's (tinkatuff.py: its zigzag hairline):
#   blender --background --factory-startup --python sculpts/tinkaton.py -- <the export's Tinkaton.glb> <out.glb>
# From the references: a small magenta figure under a great deal of pale pink hair: a cap over its round head whose
# hairline zigzags down over the forehead with a pink zigzag line inside the pale, two pointed lobes of hair standing up
# from the crown like a rabbit's ears with a pink line up each front, and two great tails hanging from the sides of the
# head to the ground, broad and rounded below, a pink zigzag band across each. Smug half-lidded eyes, a wide open grin
# with a pink tongue, small pale cheek spots. Short magenta legs on round pale feet. The +X arm held forward with an
# open pale hand of three fingers, a pink spot on its back; the -X fist, pale, grips the end of the handle at the hip.
# The colossal hammer rests behind it: a long purple handle running up and back behind the head to a great barrel of
# pale lavender stone, its big flat face at the +X end (the side the game's front camera sees), a purple end cap at the
# -X end, dark purple bands round it, and on its top a pale yellow forked crest with teeth.
# Reference ratios (3D picture): the figure, hair lobes to feet / everything 0.5; the hammer's barrel length / the
# figure 1.2, its diameter / the figure 0.6; the head with its cap / the figure 0.42; the tails reach the ground.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, srgb, inside, unit, axes, outward, tri, args, sibling

t = sibling("tinkatink")

HAIR, HAIRPINK, PALE = "#F4D2D4", "#EE8FAE", "#F6D6D8"
MAGENTA = "#E0407E"
BARREL, BARRELFACE, BAND, CAPPURPLE, CREST = "#B8ACDE", "#CDC4EC", "#5E44A4", "#4E3890", "#F2E2A8"
TONGUE = "#F07AA0"

HC = np.array([0.0, -0.012, 0.29])            # the head (bone 0.23; the ears' bones at (+-0.05, 0.025, 0.33))
HR = np.array([0.1, 0.088, 0.09])
G = np.array([-0.11, -0.065, 0.14])           # the -X fist on the handle's end
TOUCH = np.array([0.0, 0.255, 0.42])          # where the handle meets the barrel


def hairline(P):
    """How far a point stands above the zigzag hairline (above: hair); the nape is covered low behind."""
    phi = np.arctan2(P[:, 0] - HC[0], -(P[:, 1] - HC[1]))
    a = np.abs(phi)
    front = HC[2] + 0.025 + 0.028 * tri(a / math.radians(62)) * (a < math.radians(98))
    back = HC[2] - 0.06
    w = np.clip((a - math.radians(98)) / math.radians(25), 0, 1)
    return P[:, 2] - (front * (1 - w) + back * w)


def tinkaton(src, output):
    k = Kit(src, coat="fur")

    # --- the head and its cap of hair, a pink zigzag line inside the hairline
    k.add("head", ell(HC, HR), MAGENTA, "head")
    def capdeform(V):
        V = V.copy()
        low = np.clip((0.15 - V[:, 2]) / 0.35, 0, 1) * (V[:, 1] < 0.2)
        V[:, 0] *= 1 - 0.45 * low; V[:, 1] *= 1 - 0.45 * low
        return V
    k.add("hair", ell(HC + (0, 0.008, 0.006), HR + 0.006, deform=capdeform), HAIR, "head")
    near = lambda P: inside(np.linalg.norm((P - HC) / (HR * 1.4), axis=1) - 1, 0.01)
    k.paint(lambda P: inside(-hairline(P), 0.0025) * near(P), HAIR)
    k.paint(lambda P: inside(-(hairline(P) - 0.016), 0.002) * inside(hairline(P) - 0.03, 0.002) * near(P)
            * (P[:, 1] < HC[1] + 0.03), HAIRPINK)

    # the two lobes of hair standing up from the crown, a little out, a pink line up each front
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        pts = np.array([(0.045, 0.02, 0.34), (0.06, 0.022, 0.38), (0.075, 0.026, 0.415), (0.088, 0.03, 0.44)]) * m
        k.add(f"lobe{side}", tube(pts, [0.036, 0.032, 0.022, 0.009], seg=20), HAIR, f"ear{side}")
        k.add(f"lobeline{side}", outward(tube(np.array([(0.05, -0.012, 0.35), (0.064, -0.008, 0.385),
                                                         (0.076, -0.004, 0.41)]) * m, [0.009, 0.008, 0.006],
                                              seg=12)), HAIRPINK, f"ear{side}", fuse=False, surface="hair")

    # the two great tails hanging from the sides of the head to the ground, a pink zigzag band across each
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"tailtop{side}", ell(np.array([0.09, 0.03, 0.27]) * m, (0.05, 0.055, 0.06)), HAIR, f"ear{side}")
        k.add(f"tail{side}", tube(np.array([(0.1, 0.03, 0.25), (0.125, 0.035, 0.17), (0.138, 0.035, 0.09),
                                            (0.138, 0.03, 0.045)]) * m, [0.05, 0.058, 0.066, 0.05], seg=24),
              HAIR, f"ear{side}")
    def band(P):
        z0 = 0.13 + 0.014 * tri((P[:, 1] + 0.02) / 0.05)
        return inside(z0 - P[:, 2], 0.002) * inside(P[:, 2] - z0 - 0.022, 0.002) * (np.abs(P[:, 0]) > 0.075)
    k.paint(band, HAIRPINK, on=["tail"])

    # --- the body: small, magenta, short legs on round pale feet
    k.add("body", ell((0, -0.004, 0.165), (0.052, 0.046, 0.062)), MAGENTA, "body")
    k.add("hips", ell((0, 0.0, 0.115), (0.056, 0.048, 0.04)), MAGENTA, "body")
    k.add("leg", tube([(0.036, -0.004, 0.1), (0.04, -0.008, 0.035)], [0.026, 0.024]), MAGENTA, "leg{s}", mirror=True)
    k.add("foot", ell((0.042, -0.02, 0.016), (0.034, 0.042, 0.017)), PALE, "leg{s}", mirror=True)
    k.paint(lambda P: inside(P[:, 2] - 0.03, 0.002), PALE, on=["foot", "leg"])

    # --- the arms: +X forward with an open pale hand, -X fist on the handle at the hip
    H = np.array([0.112, -0.105, 0.17])
    k.add("armR", tube([(0.048, 0.0, 0.2), (0.08, -0.05, 0.185), H + (-0.012, 0.012, 0.0)], [0.026, 0.024, 0.022]),
          MAGENTA, "armR")
    k.add("palm", ell(H, (0.03, 0.026, 0.02), rot=(0, 0, -25)), PALE, "armR")
    for i, a in enumerate((-35, 0, 35)):
        d = unit(math.sin(math.radians(a)) + 0.4, -1, 0.15)
        k.add(f"finger{i}", tube([H + d * 0.018, H + d * 0.042], [0.0115, 0.0105]), PALE, "armR")
    k.paint(lambda P: inside(np.linalg.norm(P - (H + (0, 0.018, -0.006)), axis=1) - 0.04, 0.002), PALE, on=["armR"])
    k.add("armL", tube([(-0.048, 0.0, 0.2), (-0.08, -0.035, 0.17), G + (0.01, 0.01, 0.006)], [0.026, 0.024, 0.022]),
          MAGENTA, "armL")
    D = unit(*(TOUCH - G))
    t.fist(k, G, D, 0.026, PALE, "armL", "fistL")

    # --- the hammer: a long purple handle behind the head, a barrel of pale stone, bands, an end cap, a crest
    k.add("pommel", t.prism(G - D * 0.055, G - D * 0.03, 0.026, 0.024, n=8, turn=0.2), CAPPURPLE, "armL",
          fuse=False, surface="metal")
    k.add("handle", outward(tube([G - D * 0.035, TOUCH + D * 0.01], [0.021, 0.021], seg=18)), t.HANDLE, "armL",
          fuse=False, surface="metal")
    k.add("guard", t.prism(TOUCH - D * 0.1, TOUCH - D * 0.075, 0.032, 0.032, n=8, turn=0.3), CAPPURPLE, "armL",
          fuse=False, surface="metal")
    R = 0.108
    C = TOUCH + D * R
    A = np.array([1.0, 0, 0]) - D * D[0]
    A = A / np.linalg.norm(A)                     # the barrel's axis, square to the handle, toward +X
    L0, L1 = -0.17, 0.17
    k.add("barrel", t.prism(C + A * L0, C + A * (L1 - 0.03), R, R * 1.02, n=10, turn=0.15, jitter=0.06, seed=11),
          BARREL, "armL", fuse=False, surface="metal")
    k.add("face", t.prism(C + A * (L1 - 0.032), C + A * L1, R * 1.07, R * 1.04, n=10, turn=0.15, jitter=0.05,
                          seed=12), BARRELFACE, "armL", fuse=False, surface="metal")
    k.add("endcap", t.prism(C + A * (L0 - 0.05), C + A * (L0 + 0.004), R * 0.72, R * 0.9, n=10, turn=0.15),
          CAPPURPLE, "armL", fuse=False, surface="metal")
    for i, (a, b) in enumerate(((-0.1, -0.075), (0.05, 0.075))):
        k.add(f"band{i}", t.prism(C + A * a, C + A * b, R * 1.045, R * 1.045, n=10, turn=0.15), BAND, "armL",
              fuse=False, surface="metal")
    # the crest: a pale yellow fork standing on the barrel's top, toothed
    up = np.array([0, 0, 1.0]) - A * A[2]; up /= np.linalg.norm(up)
    base = C + up * (R * 0.92)
    crest = [(-0.11, 0.0), (-0.085, 0.0), (-0.02, 0.065), (0.0, 0.06), (0.02, 0.065), (0.085, 0.0), (0.11, 0.0),
             (0.13, 0.02), (0.112, 0.03), (0.118, 0.048), (0.096, 0.042), (0.05, 0.088), (0.06, 0.105),
             (0.036, 0.1), (0.0, 0.112), (-0.036, 0.1), (-0.06, 0.105), (-0.05, 0.088), (-0.096, 0.042),
             (-0.118, 0.048), (-0.112, 0.03), (-0.13, 0.02)]
    crest = [(a * 1.25, b * 0.85) for a, b in crest]
    k.add("crest", slab(crest, base, A, up, 0.026), CREST, "armL", fuse=False, surface="metal")

    # --- the face: cheek spots, the open grin with its tongue
    t.cheek(k, HC, HR, (0.78, -0.56, -0.3), (0.014, 0.012), colour=PALE)
    Mo, Tg = srgb(t.MOUTH), srgb(TONGUE)
    def grin(u, v):
        c = np.tile(Mo, (len(u), 1))
        c[(u / 0.6) ** 2 + ((v + 0.85) / 0.45) ** 2 < 1] = Tg
        return c
    mp, mn = t.on_ell(HC, HR, (0.0, -1.0, -0.42))
    k.decal(mp, mn, (0.03, 0.022), grin, eps=0.0018, rings=16, segs=64,
            outline=lambda th: 0.62 if math.sin(th) > 0 else 1.0)

    k.build(voxel=0.0045, smooth=4, band=0.004, wband=0.03)

    # --- the eyes: smug, half-lidded, the lid nearly level
    look, _ = t.lid_eye(slope=-0.15, top=0.1)
    k.eye_states(t.lay_eyes(k, HC, HR, (0.42, -0.9, -0.08), (0.025, 0.02), look, srgb(MAGENTA), srgb(t.INK)))
    k.export(output)


if __name__ == "__main__":
    tinkaton(*args())
