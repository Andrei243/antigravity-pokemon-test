# Lycanroc (Midday Form) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/lycanroc.py -- <the export's Lycanroc.glb> <out.glb>
# Rockruff grown (rockruff.py, whose eye, spikes, rocks, ruff and claws it borrows). From the references: a tan wolf
# crouched low, ready to spring, its great white mane the largest mass, over the neck, shoulders and chest, ragged in
# points to the sides and down the chest, set with big dark rock spikes standing up and back and one forward off each
# shoulder; a large head low and forward in front of it with a long snout, white on the jaw, a black nose; fierce blue
# eyes cut by a brow; tall pointed ears, pink inside; a ragged tan mane round the face, two rocks behind the ears;
# short bent legs (elbows and hocks back) under fused shoulder and thigh masses, white below, black claws; a big white
# bushy tail swept up behind.
# The skeleton's leg joints stand at 0.2: the masses are drawn down over them, so the trunk hangs low (the joints are
# inside the shoulders and thighs).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, lumpy, args, sibling

r_ = sibling("rockruff")

TAN, WHITE, ROCK, PINK = "#D6B4A2", "#F2F0EE", "#4E423C", "#F0A6B4"
NOSE, CLAW, BLUE, PUPIL, INK = "#141414", "#2A2624", "#4C9EE0", "#121216", "#2A2630"

# The frame (joints: body (0, 0.02, 0.24), head (0, -0.1, 0.32), legs (+-0.045, -0.1, 0.2), hinds (+-0.045, 0.1, 0.2),
# tail (0, 0.14, 0.27))
HC = np.array([0.0, -0.21, 0.24])        # the head's middle: large, low and forward of the mane
HR = np.array([0.085, 0.088, 0.074])
SC = HC + np.array([0, -0.095, -0.028])  # the long snout
SR = np.array([0.042, 0.075, 0.036])
LX = 0.06
MANE = np.array([0, -0.1, 0.22])         # the white mane's middle


def eye(u, v):
    """A fierce eye: white round a blue iris, a black pupil, a glint."""
    W, B, P = srgb(WHITE), srgb(BLUE), srgb(PUPIL)
    c = np.tile(W, (len(u), 1))
    c[np.hypot((u + 0.18) / 0.72, (v + 0.02) / 0.9) < 1] = B
    c[np.hypot((u + 0.18) / 0.36, (v + 0.02) / 0.5) < 1] = P
    c[np.hypot(u - 0.05, v - 0.3) < 0.16] = (1, 1, 1)
    return c


def lycanroc(src, out):
    k = Kit(src, coat="fur")

    # The trunk hanging low between the legs, the chest deepest, rump a little higher
    k.add("chest", ell((0, -0.06, 0.16), (0.08, 0.09, 0.085)), TAN, "body")
    k.add("middle", ell((0, 0.03, 0.17), (0.066, 0.085, 0.072)), TAN, "body")
    k.add("rump", ell((0, 0.11, 0.19), (0.07, 0.07, 0.075)), TAN, "body")
    k.add("neck", tube([(0, -0.08, 0.18), (0, -0.15, 0.22)], [0.062, 0.06]), TAN, "head")

    # Short bent legs under fused shoulder and thigh masses: elbows and hocks back; white below, black claws
    k.add("shoulder", ell((LX, -0.095, 0.13), (0.05, 0.056, 0.07)), TAN, "leg{s}", mirror=True)
    k.add("leg", tube([(LX - 0.002, -0.095, 0.12), (LX, -0.07, 0.075), (LX, -0.11, 0.028)], [0.042, 0.036, 0.032]),
          TAN, "leg{s}", mirror=True)
    k.add("paw", ell((LX, -0.13, 0.016), (0.032, 0.042, 0.018)), WHITE, "leg{s}", mirror=True)
    k.add("thigh", ell((LX, 0.11, 0.155), (0.055, 0.07, 0.085)), TAN, "hind{s}", mirror=True)
    k.add("hind", tube([(LX, 0.12, 0.13), (LX + 0.002, 0.165, 0.07), (LX, 0.13, 0.028)], [0.045, 0.036, 0.032]),
          TAN, "hind{s}", mirror=True)
    k.add("hpaw", ell((LX, 0.115, 0.016), (0.032, 0.042, 0.018)), WHITE, "hind{s}", mirror=True)
    k.paint(lambda P: inside(P[:, 2] - (0.08 - 0.006 * (P[:, 1] > 0)), 0.003), WHITE,
            on=["leg", "paw", "hind", "hpaw"])
    for bone, y in (("leg{s}", -0.168), ("hind{s}", 0.077)):
        for j, dx in enumerate((-1, 0, 1)):
            a = np.array([LX + dx * 0.016, y, 0.014])
            k.add(f"claw{bone[0]}{j}", r_.spike(a, (0.3 * dx, -1, -0.5), 0.03, 0.01, 0.008), CLAW, bone, fuse=False,
                  mirror=True, surface="shell")

    # The large head low and forward, a long snout, white on the jaw, a black nose
    k.add("head", ell(HC, HR), TAN, "head")
    k.add("snout", ell(SC, SR, rot=(-8, 0, 0)), TAN, "head")
    k.add("jaw", ell(SC + (0, 0.01, -0.022), (0.032, 0.058, 0.018)), WHITE, "head")
    k.paint(lambda P: inside(P[:, 2] - (SC[2] - 0.005), 0.002) * (P[:, 1] < HC[1] - 0.03), WHITE,
            on=["snout", "jaw", "head"])
    k.add("nose", ell(SC + (0, -SR[1] * 0.92, SR[2] * 0.45), (0.016, 0.013, 0.012)), NOSE, "head", fuse=False,
          surface="skin")

    # Tall pointed ears up and back, pink inside
    zb = HC[2] + 0.058
    for s, nm in ((1, "R"), (-1, "L")):
        b0 = HC + np.array([0.05 * s, 0.016, 0.058])
        tip = b0 + np.array([0.034 * s, 0.04, 0.09])
        k.add("ear" + nm, blade([b0, (b0 + tip) / 2, tip], [0.032, 0.021, 0.005], unit(0, 1, -0.3), 0.45), TAN, "head")
    k.paint(lambda P: inside(np.abs(np.abs(P[:, 0]) - 0.052 - 0.378 * (P[:, 2] - zb)) - 0.011, 0.0015)
            * (P[:, 2] > zb + 0.012) * (P[:, 2] < zb + 0.07) * (P[:, 1] < HC[1] + 0.04), PINK, on=["ear"])

    # The ragged tan mane round the face: points out and back from the cheeks and the brow
    n = 0
    for s in (1, -1):
        for dz, d, ln in ((0.026, (1, 0.5, 0.45), 0.065), (-0.006, (1, 0.45, 0.0), 0.075), (-0.04, (1, 0.35, -0.45), 0.07),
                          (-0.065, (0.6, 0.1, -1), 0.058)):
            base = HC + np.array([s * HR[0] * 0.85, 0.005, dz])
            k.add(f"facefur{n}", r_.spike(base, (s * d[0], d[1], d[2]), ln, 0.02, 0.013), TAN, "head", fuse=False,
                  surface="fur"); n += 1
        k.add(f"facefur{n}", r_.spike(HC + (s * 0.026, -0.02, 0.058), (s * 0.4, 0.8, 0.5), 0.065, 0.02, 0.013), TAN,
              "head", fuse=False, surface="fur"); n += 1
    # Two dark rocks up behind the ears
    for s in (1, -1):
        k.add(f"headrock{'R' if s > 0 else 'L'}", r_.rock(HC + (s * 0.03, 0.06, 0.058), (s * 0.3, 0.6, 1), 0.08, 0.019,
                                                          seed=7 + s), ROCK, "head", fuse=False, surface="shell")

    # The great white mane, the largest mass: over the neck and shoulders and down the chest, ragged in points to
    # the sides and down the chest, the dark rocks large in it
    k.add("mane", ell(MANE, (0.115, 0.1, 0.105), deform=lumpy(7, 3, 0.09)), WHITE, "body")
    k.add("maneback", ell((0, 0.0, 0.24), (0.09, 0.1, 0.07), deform=lumpy(5, 2, 0.07)), WHITE, "body")
    k.add("manebib", ell((0, -0.165, 0.14), (0.08, 0.06, 0.075), deform=lumpy(6, 2, 0.08)), WHITE, "body")
    r_.ruff(k, MANE + (0, 0.01, -0.02), 0.105, 0.095, 14, 0.075, 0.028, WHITE, "body", down=0.45, name="maneruff")
    for i, x in enumerate((-0.05, -0.025, 0.0, 0.025, 0.05)):
        k.add(f"bibtuft{i}", r_.spike((x, -0.195, 0.1), (x * 3, -0.35, -1), 0.06 - 0.01 * abs(x) / 0.025, 0.022, 0.014),
              WHITE, "body", fuse=False, surface="fur")
    for i, (x, y, z, d, ln, w) in enumerate(((0.0, -0.12, 0.31, (0.0, 0.3, 1), 0.12, 0.03),
                                             (0.06, -0.07, 0.29, (0.5, 0.5, 1), 0.11, 0.028),
                                             (-0.06, -0.07, 0.29, (-0.5, 0.5, 1), 0.11, 0.028),
                                             (0.0, 0.0, 0.29, (0.0, 0.8, 0.8), 0.1, 0.026))):
        k.add(f"manerock{i}", r_.rock((x, y, z), d, ln, w, seed=i), ROCK, "body", fuse=False, surface="shell")
    for s in (1, -1):
        k.add(f"shoulderrock{'R' if s > 0 else 'L'}", r_.rock((s * 0.1, -0.16, 0.16), (s * 0.35, -1, -0.15), 0.08,
                                                             0.022, seed=11 + s), ROCK, "leg" + ("R" if s > 0 else "L"),
              fuse=False, surface="shell")

    # The big white bushy tail swept up behind, swung out to +X
    TB = np.array([0.0, 0.16, 0.2])
    k.add("tailroot", tube([TB, TB + (0.012, 0.04, 0.03)], [0.03, 0.04]), WHITE, "tail")
    k.add("tail", ell(TB + (0.045, 0.07, 0.1), (0.058, 0.062, 0.095), rot=(-25, 0, -30), deform=lumpy(5, 3, 0.06)),
          WHITE, "tail")
    k.add("tailtip", ell(TB + (0.06, 0.085, 0.19), (0.04, 0.044, 0.054), rot=(-30, 0, -30)), WHITE, "tail")
    for i, (dz, ang) in enumerate(((0.06, 0.2), (0.115, 0.5), (0.16, 0.85), (0.21, 1.2))):
        base = TB + np.array([0.058, 0.125 - 0.017 * i, dz])
        k.add(f"tailfur{i}", r_.spike(base, (0.45, math.cos(ang), math.sin(ang)), 0.052, 0.023, 0.014), WHITE, "tail",
              fuse=False, surface="fur")

    # A mouth line along the jaw
    CW, CK = srgb(WHITE), srgb(INK)
    def mouth(u, v):
        c = np.tile(CW, (len(u), 1))
        c[(np.abs(v - 0.25 * u) < 0.25) & (np.abs(u) < 0.95)] = CK
        return c
    for s in (1, -1):
        k.decal(SC + (s * SR[0] * 0.9, -0.016, -0.016), (s, -0.3, -0.2), (0.03, 0.008),
                mouth if s > 0 else (lambda a, b: mouth(-a, b)), eps=0.002, rings=8, segs=40)

    k.build(voxel=0.0048, smooth=4, band=0.004, wband=0.04)

    # Fierce blue eyes, their tops cut by a brow sloping down to the snout
    de = unit(0.55, -0.8, 0.12)
    E = HC + de / np.linalg.norm(de / HR) + np.array([0, -0.004, 0])
    cut = [(math.cos(t), min(math.sin(t), 0.25 + 0.45 * math.cos(t))) for t in np.linspace(0, 2 * math.pi, 72, endpoint=False)]
    k.eye_states(lambda s: r_.lay_eyes(k, s, E, de, (0.024, 0.021), eye, TAN, INK, outline=cut))
    k.export(out)


if __name__ == "__main__":
    lycanroc(*args())
