# Manectric sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/manectric.py -- <the export's Manectric.glb> <out.glb>
#
# Electrike grown (its frame through sibling("electrike"): the solid spikes, the almond eye with its heavy top line,
# the painted lightning): a tall blue wolf whose head is a pale yellow helmet rising to a tall point over the crown,
# with a lower point behind on each side; the muzzle long, yellow on top, blue underneath with a blue zag up the
# cheek toward the eye; fierce red eyes; a black mark behind each eye; a shaggy blue ruff down the chest; two yellow
# spikes on the back; a big yellow puff on each haunch with points behind; yellow cuffs above the forepaws, white
# claws; a blue tail like a bolt rising up and back with a spur.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, unit, eye_look, polygon, boxy, sibling, args

e = sibling("electrike")
spike, almond, poly_mask = e.spike, e.almond, e.poly_mask

BLUE, YELLOW, NAVY = "#5AAEE0", "#F0DD88", "#2C3E5C"
EYEWHITE, IRIS, PUPIL, INK, CLAW, MARK = "#F6F6F0", "#E2303C", "#3A0C12", "#1C1E26", "#F2F2EE", "#1A1A20"


HC, HS = np.array((0, -0.19, 0.515)), 1.25      # the head's centre and how much larger than first laid out


def S(p):
    """A point laid out on the first round's head, on the head as it is now."""
    return HC + (np.array(p, float) - HC) * HS


def manectric(src, out):
    k = Kit(src, coat="fur")

    # --- trunk: deep chest, the belly well clear of the ground but not on stilts ---------------------------------
    k.add("chest", ell((0, -0.1, 0.32), (0.095, 0.115, 0.125)), BLUE, "body")
    k.add("middle", ell((0, 0.02, 0.32), (0.085, 0.12, 0.105)), BLUE, "body")
    k.add("rump", ell((0, 0.13, 0.33), (0.088, 0.085, 0.1)), BLUE, "body")

    # --- legs: broad shoulders and thighs over the upper legs, tapering shins, paws ------------------------------
    k.add("shoulder", ell((0.068, -0.115, 0.255), (0.058, 0.07, 0.11)), BLUE, "leg{s}", mirror=True)
    k.add("leg", tube([(0.068, -0.12, 0.26), (0.07, -0.122, 0.15), (0.072, -0.128, 0.04)], [0.056, 0.043, 0.04]),
          BLUE, "leg{s}", mirror=True)
    k.add("paw", ell((0.073, -0.15, 0.023), (0.042, 0.054, 0.025)), BLUE, "leg{s}", mirror=True)
    k.add("cuff", ell((0.072, -0.13, 0.07), (0.054, 0.056, 0.036)), YELLOW, "leg{s}", mirror=True)
    for i, a in enumerate((-2.4, -1.2, 0.0, 1.2, 2.4)):
        base = np.array((0.072 + 0.046 * math.sin(a), -0.13 - 0.046 * math.cos(a), 0.062))
        tip = base + np.array((0.03 * math.sin(a), -0.03 * math.cos(a), -0.028))
        k.add(f"cufftuft{i}", spike(base, tip, 0.016, 0.012), YELLOW, "leg{s}", fuse=False, mirror=True)
    for i, dx in enumerate((-0.018, 0.0, 0.018)):
        base = np.array((0.073 + dx * 1.2, -0.198, 0.015))
        k.add(f"claw{i}", spike(base, base + np.array((0, -0.024, -0.008)), 0.008, 0.006), CLAW, "leg{s}",
              fuse=False, mirror=True, surface="shell")

    k.add("thigh", ell((0.068, 0.145, 0.27), (0.064, 0.085, 0.115)), BLUE, "hind{s}", mirror=True)
    k.add("hind", tube([(0.07, 0.15, 0.24), (0.072, 0.175, 0.12), (0.072, 0.155, 0.04)], [0.053, 0.04, 0.039]), BLUE,
          "hind{s}", mirror=True)
    k.add("hpaw", ell((0.073, 0.132, 0.023), (0.042, 0.054, 0.025)), BLUE, "hind{s}", mirror=True)
    k.add("hock", spike((0.072, 0.2, 0.12), (0.074, 0.25, 0.09), 0.02, 0.018), BLUE, "hind{s}", fuse=False,
          mirror=True)
    for i, dx in enumerate((-0.016, 0.016)):
        base = np.array((0.073 + dx * 1.2, 0.082, 0.015))
        k.add(f"hclaw{i}", spike(base, base + np.array((0, -0.022, -0.008)), 0.008, 0.006), CLAW, "hind{s}",
              fuse=False, mirror=True, surface="shell")
    # the yellow puff over each haunch, with points sweeping back
    PC, PR = np.array((0.088, 0.15, 0.32)), np.array((0.066, 0.09, 0.094))
    k.add("puff", ell(PC, PR), YELLOW, "hind{s}", mirror=True)
    def puff(P):
        Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
        return inside(np.linalg.norm((Q - PC) / PR, axis=1) - 1.06, 0.01) * (Q[:, 0] > 0.045)
    k.paint(puff, YELLOW)
    k.paint(lambda P: (1 - puff(P)) * (np.abs(P[:, 0]) > 0.03) * (P[:, 1] > 0.08) * (P[:, 1] < 0.24)
            * (P[:, 2] > 0.2) * (P[:, 2] < 0.42), BLUE, on=["puff"])
    for i, (a, b) in enumerate((((0.092, 0.21, 0.38), (0.11, 0.29, 0.45)), ((0.104, 0.225, 0.32), (0.13, 0.32, 0.31)),
                                ((0.095, 0.21, 0.26), (0.11, 0.28, 0.2)))):
        k.add(f"puffpt{i}", spike(a, b, 0.027, 0.032), YELLOW, "hind{s}", fuse=False, mirror=True)

    # --- neck and head: the yellow helmet on a thick neck, a long muzzle --------------------------------------------
    # The head with its crest is laid out on a head of the first round's size and scaled up by HS about its centre
    k.add("neck", tube([(0, -0.11, 0.37), (0, -0.155, 0.44), (0, -0.18, 0.5)], [0.085, 0.084, 0.082]), YELLOW, "head")
    k.add("head", ell(S((0, -0.19, 0.515)), HS * np.array((0.088, 0.092, 0.087)), deform=boxy(2.3)), YELLOW, "head")
    def taper(V):
        V = V.copy(); t = np.clip((-V[:, 1] + 1) / 2, 0, 1)
        V[:, 0] *= 1 - 0.3 * t; V[:, 2] *= 1 - 0.2 * t
        return V
    k.add("muzzle", ell(S((0, -0.29, 0.468)), HS * np.array((0.047, 0.08, 0.042)), deform=taper), YELLOW, "head")
    k.add("jaw", ell(S((0, -0.26, 0.43)), HS * np.array((0.045, 0.07, 0.028))), BLUE, "head")
    k.add("nose", ell(S((0, -0.368, 0.474)), HS * np.array((0.017, 0.012, 0.013))), NAVY, "head", fuse=False,
          surface="skin")
    # the tall point over the crown, fused so the head flows up into it, and a lower point behind on each side
    k.add("crest", blade([S(p) for p in ((0, -0.185, 0.53), (0, -0.175, 0.6), (0, -0.16, 0.66), (0, -0.145, 0.71))],
                         [HS * r for r in (0.072, 0.05, 0.026, 0.004)], (1, 0, 0), 0.72), YELLOW, "head")
    k.add("horn", blade([S(p) for p in ((0.04, -0.13, 0.54), (0.06, -0.095, 0.6), (0.078, -0.065, 0.65))],
                        [HS * r for r in (0.04, 0.024, 0.004)], (1, 0, 0), 0.6), YELLOW, "head", mirror=True)

    # --- the shaggy blue ruff down the chest and throat ---------------------------------------------------------
    RUFF = (((0.0, -0.225, 0.36, (0, -0.45, -1), 0.11), (0.05, -0.215, 0.37, (0.5, -0.45, -1), 0.105),
             (0.088, -0.18, 0.39, (0.95, -0.25, -0.6), 0.095), (0.03, -0.235, 0.3, (0.3, -0.55, -1), 0.1),
             (0.075, -0.205, 0.31, (0.75, -0.4, -1), 0.09), (0.095, -0.14, 0.43, (1, 0.1, -0.35), 0.08),
             (0.0, -0.21, 0.41, (0, -1, -0.5), 0.07), (0.06, -0.22, 0.42, (0.6, -0.9, -0.5), 0.07)))
    for i, (x, y, z, d, ln) in enumerate(RUFF):
        base = np.array((x, y, z))
        k.add(f"ruff{i}", blade([base, base + unit(*d) * ln * 0.5, base + unit(*d) * ln], [0.045, 0.03, 0.003],
                                (0, 1, 0) if x < 0.04 else (1, 0, 0), 0.5), BLUE, "body", mirror=x > 0)
    k.add("ruffmass", ell((0, -0.19, 0.37), (0.095, 0.07, 0.085)), BLUE, "body")

    # --- two yellow spikes on the back --------------------------------------------------------------------------
    k.add("back0", spike((0.02, -0.03, 0.39), (0.035, 0.03, 0.5), 0.022, 0.028), YELLOW, "body", fuse=False)
    k.add("back1", spike((-0.015, 0.05, 0.39), (-0.025, 0.11, 0.47), 0.02, 0.026), YELLOW, "body", fuse=False)

    # --- the tail: a blue bolt rising up and back with a spur pointing back from its middle --------------------
    k.add("tailroot", ell((0, 0.19, 0.35), (0.035, 0.04, 0.04)), BLUE, "tail")
    k.add("tail", spike((0, 0.19, 0.355), (0.01, 0.31, 0.57), 0.03, 0.036), BLUE, "tail", fuse=False)
    k.add("tailspur", spike((0.004, 0.24, 0.43), (0.01, 0.33, 0.4), 0.018, 0.022), BLUE, "tail", fuse=False)

    # --- the blue under the muzzle with a zag up the cheek toward the eye ---------------------------------------
    ZAG = [(-0.4, 0.4), (-0.39, 0.462), (-0.28, 0.462), (-0.236, 0.505), (-0.236, 0.47), (-0.175, 0.485),
           (-0.19, 0.43), (-0.21, 0.4)]
    def zag(P):
        Q = HC + (P - HC) / HS
        return poly_mask(Q[:, 1], Q[:, 2], ZAG, 0.0025 / HS) * (Q[:, 2] < 0.5) * (Q[:, 1] < -0.15)
    k.paint(zag, BLUE)
    # the black mark behind each eye: a drop with a point up
    k.decal(S((0.085, -0.16, 0.52)), unit(1, -0.1, 0.05), (0.018, 0.03), MARK, up=unit(0, 0.35, 1), eps=0.003,
            outline=polygon([(0, 1), (0.5, 0.2), (0.85, -0.45), (0.5, -0.9), (0, -1), (-0.5, -0.9), (-0.85, -0.45),
                             (-0.5, 0.2)]), rings=12, segs=48, mirror=True)

    # --- fierce red eyes under the helmet's brow -----------------------------------------------------------------
    W, R, P_, I, Y = srgb(EYEWHITE), srgb(IRIS), srgb(PUPIL), srgb(INK), srgb(YELLOW)
    top = lambda u: 0.36 + 0.36 * u
    def look(u, v):
        c = np.tile(W, (len(u), 1))
        c[((u + 0.12) / 0.55) ** 2 + ((v + 0.05) / 0.7) ** 2 < 1] = R
        c[((u + 0.14) / 0.2) ** 2 + ((v + 0.05) / 0.36) ** 2 < 1] = P_
        c[((u + 0.02) / 0.12) ** 2 + ((v - 0.22) / 0.13) ** 2 < 1] = (1, 1, 1)
        c[v > top(u)] = I
        return c
    de = unit(0.64, -0.74, 0.12)
    E = S((0.062, -0.262, 0.528))
    def eyes(state):
        f = e.fierce(look, Y, I, top) if state == "Fierce" else eye_look(state, look, Y, I)
        k.decal(E, de, (0.03, 0.021), f, up=unit(0.3, 0, 1), eps=0.004, outline=almond, rings=20, segs=56,
                mirror=True)

    k.build(voxel=0.0055, band=0.005)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    manectric(*args())
