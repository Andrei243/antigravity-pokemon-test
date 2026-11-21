# Kingambit sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/kingambit.py -- <the export's Kingambit.glb> <out.glb>
# Grown from pawniard.py (sibling): its red, dark and blade white, its eyes peering from a dark face (lay_eyes), its
# blades wrapped round the waist (band); what the pictures change.
#
# From the references (they overrule the task's "crescent blade on its helmet": the crest is one tall straight gold
# sword blade with a silver edge, rising from the brow; the "blade whiskers" are a wide black brim edged red, out to
# each side at the eyes' height, drooping and turning up at the tips): a great warlord sitting on its own black mantle,
# which falls behind its shoulders to the ground and is the seat under it. A red helmet with a ridge down the middle of
# a black face, narrow yellow eyes; a red chest armour with gold lapels in a V over a dark middle, square red
# pauldrons; a black belt with white blades whirling round it; wide red breeches, knees out, short red shins, white
# blade feet; thin dark grey arms, the hands great gold gauntlets of blades resting before the knees.
# Ratios (the art, the blade left out): head height / height 0.24, chest width / height 0.38, knees' span / height
# 0.68; the blade is as long as the body is tall in the art: here it is cut to half that and laid back a little, so
# the body is not drawn small (helmet height / KIT height about 0.2); the brim cut from 1.7 to 1.0 of the height.
# Skeleton: body (0, 0, 0.5), tail (0, 0.06, 0.42), arms (+-0.15, 0, 0.65), head (0, -0.01, 0.71), legs (+-0.07, 0, 0.36).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, args, sibling

pw = sibling("pawniard")
RED, DARK, BLADE = pw.RED, "#2A2830", pw.BLADE
BLACK, GOLD, GOLDDK, SILVER, GREY, YELLOW = "#232128", "#E8B83A", "#B98A22", "#D8DCE4", "#55555F", "#F4D020"

# The squat (the coordinator's round 2): set low and wide, hips at 0.22, thighs level out to the knees, short shins,
# the chest and pauldrons a quarter larger just above the knees (z below is the parts', not the joints')
HC, HR = np.array([0.0, -0.03, 0.7]), np.array([0.126, 0.12, 0.124])
CHEST, CR = np.array([0.0, 0.0, 0.47]), np.array([0.19, 0.13, 0.155])
SHOULDER = np.array([0.19, 0.0, 0.53])
ELBOW = np.array([0.29, -0.05, 0.4])
HAND = np.array([0.29, -0.13, 0.29])
HIP = np.array([0.08, -0.02, 0.22])
KNEE = np.array([0.3, -0.1, 0.205])
ANKLE = np.array([0.32, -0.12, 0.08])
ZD = -0.13          # how far the old chest's parts are set down


def chain(sections):
    """A solid along a run of cross-sections: a flat-faced hull between each two."""
    return [hull(np.vstack([a, b])) for a, b in zip(sections, sections[1:])]


def blade_crest(k):
    """The tall gold sword blade from the brow, broad face to the front, a silver edge on its left (-X) side."""
    base = HC + np.array([0.0, -0.045, 0.075])
    up = unit(0, 0.2, 1)
    side = np.array([1.0, 0, 0])
    fwd = np.cross(up, side)
    secs, edge = [], []
    for t, w, th in ((0.0, 0.05, 0.036), (0.12, 0.062, 0.034), (0.26, 0.056, 0.03), (0.36, 0.034, 0.024), (0.43, 0.0, 0.012)):
        c = base + up * t
        secs.append(np.array([c + side * w + fwd * th * 0.2, c - side * w * 0.8 + fwd * th * 0.2,
                              c + fwd * th, c - fwd * th, c + side * w * 0.4 - fwd * th * 0.6]))
        if w > 0:
            e = c - side * (w * 0.8 + 0.012)
            edge.append(np.array([e + fwd * 0.01, e - fwd * 0.01, e + side * 0.016 + fwd * 0.012,
                                  e + side * 0.016 - fwd * 0.012]))
        else:
            edge.append(np.array([c]))
    for i, h in enumerate(chain(secs)):
        k.add(f"crest{i}", h, GOLD, "head", fuse=False)
    for i, h in enumerate(chain(edge)):
        k.add(f"crestedge{i}", h, SILVER, "head", fuse=False)
    # the gold band at its root on the brow
    k.add("crestroot", hull([base + (dx, dy, dz) for dx in (-0.045, 0.045) for dy in (-0.012, 0.03) for dz in (-0.03, 0.02)]),
          GOLDDK, "head", fuse=False)


def brim(k):
    """The wide black brim out to each side at the eyes' height, drooping and turning up at its tips, edged red below;
    its face banked up toward the front so the game's cameras see it."""
    for s, sname in ((1, "R"), (-1, "L")):
        # (x out, z, half width front to back, thickness)
        run = [(0.075, 0.82, 0.05, 0.04), (0.16, 0.805, 0.055, 0.036), (0.28, 0.78, 0.05, 0.032),
               (0.4, 0.785, 0.038, 0.03), (0.47, 0.815, 0.022, 0.026), (0.51, 0.85, 0.004, 0.02)]
        n = unit(0, -0.45, 0.89)          # the brim's face, banked up toward the front
        a = unit(0, 0.89, 0.45)          # front to back in its plane
        secs, edge = [], []
        for x, z, w, t in run:
            c = np.array([s * (x + 0.015), HC[1] + 0.005 + 0.02 * x, HC[2] + (z - 0.805)])
            secs.append(np.array([c - a * w + n * t / 2, c - a * w - n * t / 2, c + a * w + n * t / 2, c + a * w - n * t / 2]))
            e = c - a * w - n * t * 0.35
            edge.append(np.array([e, e - a * 0.016, e - n * 0.018, e - a * 0.012 - n * 0.016]))
        for i, h in enumerate(chain(secs)):
            k.add(f"brim{i}{sname}", h, BLACK, "head", fuse=False)
        for i, h in enumerate(chain(edge[:-1])):
            k.add(f"brimedge{i}{sname}", h, RED, "head", fuse=False)


def gauntlet(k, s, sname):
    """A great gold gauntlet: a fist of hull with blades fanned forward from it."""
    h = HAND * np.array([s, 1, 1])
    fist = [h + (dx, dy, dz) for dx in (-0.04, 0.04) for dy in (-0.045, 0.04) for dz in (-0.035, 0.035)]
    k.add(f"fist{sname}", hull(fist), GOLD, f"arm{sname}", fuse=False)
    for j, (ang, L) in enumerate(((-25, 0.065), (0, 0.08), (25, 0.065))):
        d = unit(math.sin(math.radians(ang)) * s * 0.8, -0.8, -0.6)
        root = h + np.array([s * math.sin(math.radians(ang)) * 0.03, -0.035, 0.0])
        pts = [root + (0.018, 0, 0.022), root + (-0.018, 0, 0.022), root + (0.018, 0, -0.022), root + (-0.018, 0, -0.022),
               root + d * L]
        k.add(f"claw{j}{sname}", hull(pts), GOLD, f"arm{sname}", fuse=False)
    # the forearm's gold cuff
    k.add(f"cuff{sname}", tube([h + (0, 0.07, 0.02), h + (0, 0.03, 0.005)], [0.036, 0.042]), GOLDDK, f"arm{sname}", fuse=False)


def kingambit(src, out):
    k = Kit(src, coat="metal")

    # --- the head: a red helmet over a black face -----------------------------------------------------------------
    k.add("head", ell(HC, HR), RED, "head")
    k.add("jowl", ell(HC + (0, -0.022, -0.068), (0.086, 0.08, 0.068)), BLACK, "head")
    k.add("neck", tube([(0, 0.0, 0.56), HC + (0, 0.01, -0.06)], [0.06, 0.06]), DARK, "head")
    def face(P):
        rel = P - HC
        m = inside(rel[:, 1] + 0.035, 0.003) * inside(rel[:, 2] - 0.018, 0.003)
        ridge = inside(np.abs(rel[:, 0]) - (0.006 + 0.25 * np.clip(rel[:, 2] + 0.06, 0, 1)), 0.002) * inside(-0.055 - rel[:, 2], 0.003)
        return m * (1 - ridge)
    k.paint(face, BLACK, on=["head"])
    blade_crest(k)
    brim(k)

    # --- the body: red armour, gold lapels in a V, a black belt ---------------------------------------------------
    k.add("chest", ell(CHEST, CR), RED, "body")
    k.add("pauldron", ell(SHOULDER + (0.015, 0, 0.0), (0.09, 0.09, 0.075)), RED, "arm{s}", mirror=True)
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.038 - 0.25 * np.clip(P[:, 2] - 0.42, 0, 1), 0.003)
            * (P[:, 1] < 0) * inside(P[:, 2] - 0.6, 0.003), GREY, on=["chest"])
    for s, sname in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        lap = [(np.array(p) * (1.25, 1.22, 1) + (0, 0, ZD)) * m for p in ((0.04, -0.1, 0.7), (0.1, -0.08, 0.71), (0.03, -0.125, 0.54),
                                          (0.065, -0.11, 0.53), (0.04, -0.07, 0.6), (0.11, -0.05, 0.68))]
        k.add(f"lapel{sname}", hull(lap), GOLD, "body", fuse=False)
    k.add("belt", ell((0, 0.0, 0.325), (0.165, 0.13, 0.06)), BLACK, "body")
    k.add("pelvis", ell((0, -0.01, 0.24), (0.17, 0.13, 0.085)), RED, "body")
    for name, z, start, span, rise in (("waistblade", 0.34, -160, 300, -0.014), ("hipblade", 0.305, 160, -300, 0.012)):
        k.add(name, pw.band((0, 0, 0.325), 0.172, 0.138, z, 0.042, 0.018, span=span, start=start, flare=0.75, rise=rise),
              BLADE, "body", fuse=False)

    # --- the black mantle, falling behind the shoulders to the ground, and the seat it folds into -----------------
    # a wide low mass behind the hips and shoulders, no taller than the shoulders, spreading sideways; it is the seat
    k.add("mantle", ell((0, 0.12, 0.34), (0.3, 0.12, 0.2)), BLACK, "tail")
    k.add("mantlelow", ell((0, 0.09, 0.13), (0.24, 0.14, 0.13)), BLACK, "tail")
    k.paint(lambda P: inside(P[:, 2] - 0.035, 0.004) * inside(-P[:, 1] + 0.12, 0.01), RED, on=["mantle"])

    # --- the legs: wide red breeches, knees out, short thick shins, white blade feet ------------------------------
    k.add("thigh", tube([HIP, KNEE], [0.088, 0.08]), RED, "leg{s}", mirror=True)
    k.add("shin", tube([KNEE + (0, 0, -0.02), ANKLE + (0, 0, 0.03)], [0.064, 0.056]), RED, "leg{s}", mirror=True)
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.1) - 0.018, 0.003) * inside(0.2 - np.abs(P[:, 0]), 0.003)
            * inside(P[:, 2] - 0.15, 0.003), GREY, on=["shin"])
    for s, sname in ((1, "R"), (-1, "L")):
        a = ANKLE * np.array([s, 1, 1])
        o = 0.035 * s        # the toe turned out
        boot = [a + (0.04, 0.03, 0.03), a + (-0.04, 0.03, 0.03), a + (0.04, -0.03, 0.035), a + (-0.04, -0.03, 0.035),
                a + (0.035, 0.03, -0.08), a + (-0.035, 0.03, -0.08), a + (0.025 + o, -0.105, -0.08),
                a + (-0.025 + o, -0.105, -0.08), a + (o * 1.6, -0.135, -0.075)]
        k.add(f"boot{sname}", hull(boot), BLADE, f"leg{sname}", fuse=False)

    # --- the arms: dark grey, elbows bent, gold gauntlets resting before the knees --------------------------------
    k.add("arm", tube([SHOULDER, ELBOW, HAND + (0, 0.06, 0.015)], [0.034, 0.03, 0.03]), GREY, "arm{s}", mirror=True)
    for s, sname in ((1, "R"), (-1, "L")):
        gauntlet(k, s, sname)

    k.build(voxel=0.0075, smooth=4, band=0.008)

    EC, EN = pw.on_ell(HC, HR, (0.4, -0.9, -0.08))
    def lay(state):
        pw.lay_eyes(k, state, EC, EN, (0.026, 0.017), lid=BLACK, iris=YELLOW)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    kingambit(*args())
