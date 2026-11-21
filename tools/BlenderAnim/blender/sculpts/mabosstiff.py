# Mabosstiff sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/mabosstiff.py -- <the export's Mabosstiff.glb> <out.glb>
#
# Maschiff grown (maschiff.py: its legs and paws, floppy ear, scowling eye, locks of hair, nose). From the references:
# a great level mastiff, taupe grey-brown, leaning forward; a head as wide as the body carried low in front, a great
# black beard hanging from the muzzle to the chest in ragged points with a taupe lip down its middle; an orange nose;
# sleepy orange eyes under heavy lids and thick silver brows curling out and down; floppy taupe ears; a silver crest
# of waves from the forehead back along the spine, ending in a long silver tail hanging back; a black saddle over the
# back, its lower edge ragged down the flanks; thick legs, big toed paws.
# The pictures overrule the brief: the coat is taupe, not grey, and the dark mass round the face is a beard hanging
# from the muzzle, with the silver crest and brows the head's signature.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, tri, unit, polygon, boxy, lumpy, args, sibling

M = sibling("maschiff")

TAUPE, BLACK, SILVER = "#7E7064", "#2E292C", "#B4B5BC"
NOSE, IRIS, PUPIL, WHITE, INK = "#F0A040", "#E99B2E", "#18141A", "#F4F0E4", "#1E1A1C"
SHADE = "#5E5248"

# --- the frame (joints: body (0, 0.05, 0.3), head (0, -0.15, 0.38), legs (+-0.1, -0.12, 0.24),
#     hinds (+-0.1, 0.2, 0.24), tail (0, 0.28, 0.35)) ---------------------------------------------------------------------
HC = np.array([0.0, -0.275, 0.45])        # the skull, carried low in front
HR = np.array([0.164, 0.14, 0.138])
MC = np.array([0.0, -0.37, 0.37])         # the muzzle under the beard
MR = np.array([0.11, 0.08, 0.075])
LX, FY, HY = 0.105, -0.13, 0.2
BROW = (0.18, 0.12)                       # heavy lids, nearly level


def beard(k, col):
    """The great black beard: a mass from the muzzle down to the chest, wider than the jaw, ragged points below."""
    k.add("beard", ell((0, -0.35, 0.31), (0.135, 0.095, 0.115)), col, "head")
    k.add("beardside", ell((0.085, -0.31, 0.33), (0.07, 0.085, 0.11)), col, "head", mirror=True)
    for i, x in enumerate((-0.11, -0.065, -0.022, 0.022, 0.065, 0.11)):
        ln = 0.085 if i in (1, 4) else 0.07 if i in (2, 3) else 0.06
        M.tuft(k, f"beardtuft{i}", (x, -0.36 + abs(x) * 0.45, 0.23), (x * 1.6, -0.15, -1), ln, 0.034, col, "head",
               flat=(0, 1, 0), f=0.45)


def crest(k, col):
    """The silver crest: waves from the forehead back over the crown, a ridge down the spine, the tail hanging back
    from its end, flat, its face turned up and out to +X."""
    # one swept mass: a roll rising over the forehead, back over the crown, narrowing into the ridge
    k.add("crest", blade([(0, -0.4, 0.525), (0, -0.36, 0.585), (0, -0.28, 0.595), (0, -0.19, 0.57), (0, -0.12, 0.545)],
                         [0.07, 0.1, 0.1, 0.08, 0.062], (0, 0, 1), 0.7), col, "head")
    for i, x in enumerate((-0.05, 0.05)):             # two soft waves along its sides
        k.add(f"wave{i}", tube([(x, -0.39, 0.55), (x * 1.2, -0.34, 0.595), (x * 1.15, -0.26, 0.6),
                                (x, -0.18, 0.575)], [0.03, 0.04, 0.04, 0.032]), col, "head")
    k.add("ridge", blade([(0, -0.17, 0.555), (0, -0.04, 0.51), (0, 0.1, 0.49), (0, 0.22, 0.47)],
                         [0.06, 0.055, 0.05, 0.045], (0, 0, 1), 0.45), col, "body")
    T = [(0.0, 0.26, 0.47), (0.03, 0.33, 0.43), (0.065, 0.385, 0.35), (0.09, 0.41, 0.27), (0.1, 0.415, 0.2)]
    k.add("tail", blade(T, [0.055, 0.055, 0.048, 0.036, 0.014], unit(0.7, -0.3, 0.6), 0.42), col, "tail")


def brows(k, col):
    """Thick silver brows over each eye, curling out and down at the side (mirrored)."""
    k.add("brow", tube([(0.03, -0.405, 0.505), (0.075, -0.4, 0.52), (0.12, -0.37, 0.515), (0.15, -0.34, 0.49),
                        (0.15, -0.35, 0.46)], [0.02, 0.026, 0.028, 0.026, 0.02]), col, "head", mirror=True)


def mabosstiff(src, out):
    k = Kit(src, coat="fur")

    # --- the trunk: deep and level, the haunches fused into it -------------------------------------------------------
    k.add("chest", ell((0, -0.11, 0.335), (0.152, 0.135, 0.14)), TAUPE, "body")
    k.add("middle", ell((0, 0.06, 0.335), (0.142, 0.15, 0.125)), TAUPE, "body")
    k.add("rump", ell((0, 0.2, 0.345), (0.142, 0.1, 0.13)), TAUPE, "body")

    # --- thick legs, big toed paws ---------------------------------------------------------------------------------
    PR = (0.066, 0.078, 0.038)
    pf = M.leg(k, LX, FY, 0.24, 0.066, PR, TAUPE, "leg{s}", "leg", (0.085, 0.095, 0.12))
    ph = M.leg(k, LX, HY, 0.24, 0.066, PR, TAUPE, "hind{s}", "hind", (0.09, 0.105, 0.125))
    M.toe_lines(k, [pf, ph], PR, SHADE)

    # the black saddle over the back, its lower edge ragged down the flanks
    k.paint(lambda P: inside((0.4 - 0.05 * tri(P[:, 1] * 9) - 0.015 * tri(P[:, 1] * 23)) - P[:, 2], 0.004)
            * (P[:, 1] > -0.2) * (P[:, 1] < 0.29), BLACK, on=["chest", "middle", "rump"])

    # --- the head: a great skull carried low, the muzzle, the beard; floppy ears -----------------------------------
    k.add("neck", ell((0, -0.19, 0.42), (0.13, 0.11, 0.12)), TAUPE, "head")
    k.add("head", ell(HC, HR, deform=boxy(2.3)), TAUPE, "head")
    k.add("muzzle", ell(MC, MR, deform=boxy(2.4)), BLACK, "head")
    beard(k, BLACK)
    # the taupe lip down the middle of the beard, under the nose
    k.decal(M.on_ell(MC, MR, 0.0, MC[2] - 0.035), unit(0, -1, -0.1), (0.04, 0.07),
            TAUPE, eps=0.003, outline=polygon([(0, 1), (0.75, 0.55), (0.9, -0.2), (0.45, -0.85), (0, -1),
                                               (-0.45, -0.85), (-0.9, -0.2), (-0.75, 0.55)]), rings=12, segs=48)
    M.floppy_ear(k, (0.138, -0.3, 0.525), 0.15, (0.032, 0.072), TAUPE, bone="head", out=0.45)

    crest(k, SILVER)
    brows(k, SILVER)
    # ragged taupe hair on the chest's front and the shoulders, under the beard
    for i, (x, z) in enumerate(((0.11, 0.26), (-0.11, 0.26), (0.13, 0.33), (-0.13, 0.33))):
        M.tuft(k, f"shag{i}", (x, -0.22, z), (np.sign(x) * 0.5, -0.4, -1), 0.07, 0.03, TAUPE, "body", flat=(0, 1, 0))

    k.add("nose", ell(MC + (0, -MR[1] - 0.006, 0.042), (0.034, 0.024, 0.024), rot=(14, 0, 0)), NOSE, "head",
          fuse=False, surface="skin")

    k.build(voxel=0.0068, smooth=3, band=0.008, wband=0.07)

    # --- sleepy orange eyes under heavy lids ------------------------------------------------------------------------
    E = M.on_ell(HC, HR, 0.07, HC[2] + 0.012)
    n = unit(0.32, -1, 0.05)
    look = M.scowl(white=WHITE, pupil=PUPIL, ink=INK, brow=BROW, band=0.28, p=(-0.08, -0.12, 0.62), iris=IRIS)
    k.eye_states(lambda s: M.lay_eyes(k, s, E, n, (0.036, 0.03), look, TAUPE, brow=BROW))
    k.export(out)


if __name__ == "__main__":
    mabosstiff(*args())
