# Decidueye-Hisui sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/decidueyehisui.py -- <the export's Decidueye-Hisui.glb> <out.glb>
#
# Grown from decidueye.py (sibling("decidueye")): its frame, its jagged hood, fringe, ruff and tail feathers, its beak,
# its eyes under the fringe and its folded wings at the sides, with what the pictures change. From the references: the
# hood red, a ragged red fringe over the brow and a broad red ruff of jagged feathers round the shoulders, the hood's
# crown orange like the top of a hat; on top a tall white tuft of five points like a crown of feathers, an orange star
# of three points at its root; a pale grey-white face with a black mask over the eyes and the beak, the eyes orange with
# a dark pupil; two orange petals in a V at the top of the chest; the body and the legs white, the legs stout with a
# ruff of white feathers over each ankle, great dark grey talons; the wings folded at the sides as two thick rounded
# masses of russet brown, darker olive brown at their feather tips, a row of orange triangles along their tops; red
# feathers hanging behind between the legs. (The task spoke of a flat hat of leaves like a straw hat: the pictures have
# a red feathered hood with an orange crown and a white tuft, the hood's ragged rim spreading wide over the face.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, outward, srgb, inside, unit, args, sibling

d = sibling("decidueye")
r, p = d.r, d.p

RED, REDDARK, CAP, FACE, WHITE = "#D2352C", "#A4241E", "#F39A2C", "#E6E6E8", "#F3F3F4"
MASK, BROWN, BROWNTIP, ORANGE = d.MASK, "#9C7348", "#5D5434", "#F2A12E"
TALON, CLAW, BEAKC = "#3A3B44", "#18191E", "#1E1F26"
# the base's functions read these as they run: the hood, its feathers and the tail are red here
d.HOOD, d.HOODDARK = RED, REDDARK


def frame():
    F = d.frame()
    F.LEGR = (0.03, 0.026)
    F.TOE, F.TOER = 0.058, 0.0125
    return F


def body(k, F):
    k.add("head", ell(F.H, F.HR), RED, "head")
    k.add("face", ell(F.FC, F.FR), FACE, "head")
    k.add("cape", ell((0.0, 0.012, 0.46), (0.135, 0.1, 0.07)), RED, "body")
    k.add("chest", ell(*F.CHEST), WHITE, "body")
    k.add("torso", ell((0.0, 0.004, 0.335), (0.07, 0.062, 0.105)), WHITE, "body")
    k.add("thigh", ell((0.05, 0.0, 0.225), (0.06, 0.062, 0.09)), WHITE, "leg{s}", mirror=True)
    k.add("shin", tube([(0.05, 0.0, 0.18), (0.051, -0.004, 0.12), (0.05, -0.006, 0.075)], [0.04, 0.035, 0.03]),
          WHITE, "leg{s}", mirror=True)
    # the hood's crown orange, like the top of a hat
    k.paint(lambda P: inside(F.H[2] + 0.72 * F.HR[2] - P[:, 2], 0.003) * (P[:, 2] < F.H[2] + 1.2 * F.HR[2]), CAP,
            on=["head"])
    # the face pale, the black mask over the eyes and the beak, round the head's axis
    rc = r.round_coords(F.H[1], 0.078 * F.HS)
    poly = d.head_poly([(0.0, 0.548), (0.02, 0.548), (0.04, 0.551), (0.058, 0.542), (0.062, 0.518), (0.048, 0.502),
                        (0.024, 0.5), (0.008, 0.486), (0.0, 0.48)])
    k.paint(lambda P: p.poly_mask(*rc(P), poly, edge=0.002) * (P[:, 1] < F.H[1] - 0.02), MASK, on=["face", "head"])
    # the ruff open at the front on the white throat
    k.paint(lambda P: inside(np.abs(P[:, 0]) - (0.014 + 0.55 * (0.47 - P[:, 2])), 0.002) * (P[:, 2] < 0.47)
            * (P[:, 1] < -0.02), WHITE, on=["cape"])


def crest(k, F):
    """The white tuft: five broad points fanned upward like a crown of feathers, an orange star at its root."""
    root = d.S(F, (0.0, -0.01, 0.598))
    for ang, ln in ((-50, 0.1), (-25, 0.13), (0, 0.145), (25, 0.13), (50, 0.1)):
        t = math.radians(ang)
        dirn = np.array([math.sin(t), 0.1, math.cos(t)])
        k.add("tuft", p.spike(root + dirn * 0.012, root + dirn * ln, 0.062, 0.032, side=(math.cos(t), 0, -math.sin(t)),
                              at=0.4), WHITE, "head", fuse=False, surface="fur")
    b = d.S(F, (0.0, -0.05, 0.588))
    for ang in (-60, 0, 60):
        t = math.radians(ang)
        dirn = np.array([math.sin(t), -0.3, math.cos(t)])
        k.add("star", hull([b + (0.011, 0.006, -0.008), b + (-0.011, 0.006, -0.008), b + (0, -0.008, -0.008),
                            b + (0, 0.004, 0.01), b + dirn * 0.058]), CAP, "head", fuse=False)


def chest_v(k, F):
    """Two orange petals in a V at the top of the chest, on the ruff's open front."""
    C, n = r.on_ell((0.0, 0.012, 0.46), (0.135, 0.1, 0.07), 0.0, 0.42)
    for s in (1, -1):
        dd = unit(-0.55 * s, 0, -1)
        dd = dd - n * (dd @ n); dd /= np.linalg.norm(dd)
        k.add("petal", r.ell_frame(C + n * 0.008 + np.array([0.028 * s, 0, 0.014]), (0.04, 0.011, 0.019), dd, n), ORANGE,
              "body", fuse=False, surface="leaf")


def cloak(k, F):
    """The wings folded at the sides as two thick rounded masses of russet brown, dark tips at the hem, orange triangles."""
    for s, side in ((1, "R"), (-1, "L")):
        b = f"wing{side}"
        C, R = np.array([s * 0.112, 0.024, 0.3]), np.array([0.066, 0.11, 0.19])
        k.add(f"cloak{side}", ell(C, R, deform=d.taper), BROWN, b, fuse=False, surface="fur")
        # the folded edge toward the front, a narrower mass reaching forward of the side
        FC_, FR_ = np.array([s * 0.096, -0.05, 0.32]), np.array([0.045, 0.05, 0.15])
        k.add(f"cloakfront{side}", ell(FC_, FR_, deform=d.taper), BROWN, b, fuse=False, surface="fur")
        # the dark olive flight feathers hanging from under the masses
        for phi in (10, 50, 90, 130, 170):
            t = math.radians(phi)
            o = np.array([s * math.sin(t), -math.cos(t), 0])
            base = C + o * np.array([0.04, 0.065, 0]) + np.array([0, 0, -0.13])
            tip = base + o * 0.012 + np.array([0, 0, -0.085])
            k.add(f"cloaktip{side}", p.spike(base, tip, 0.05, 0.02, side=np.cross((0, 0, 1.0), o), at=0.4), BROWNTIP,
                  b, fuse=False, surface="fur")
        # orange triangles in a row round the side mass's upper part, pointing down
        dz = 0.11
        for phi in (40, 75, 110, 145):
            t = math.radians(phi)
            q = math.sqrt(1 - (dz / R[2]) ** 2)
            c = C + np.array([s * R[0] * math.sin(t) * q, -R[1] * math.cos(t) * q, dz])
            n = (c - C) / R ** 2; n /= np.linalg.norm(n)
            tang = np.cross((0, 0, 1.0), n); tang /= np.linalg.norm(tang)
            c = c + n * 0.002
            k.add(f"tri{side}", hull([c + tang * 0.02 + (0, 0, 0.013), c - tang * 0.02 + (0, 0, 0.013),
                                      c + (0, 0, -0.027), c + n * 0.008 + (0, 0, 0.004)]), ORANGE, b, fuse=False)


def cuffs(k, F):
    """A ruff of white feathers over each ankle, pointing down and out."""
    for s, side in ((1, "R"), (-1, "L")):
        c = np.array([0.05 * s, -0.006, 0.1])
        for a in range(0, 360, 45):
            t = math.radians(a)
            o = np.array([math.sin(t), -math.cos(t), 0])
            base = c + o * 0.03
            tip = base + o * 0.022 + np.array([0, 0, -0.045])
            k.add(f"cuff{side}", p.spike(base, tip, 0.032, 0.014, side=np.cross((0, 0, 1.0), o), at=0.4), WHITE,
                  f"leg{side}", fuse=False, surface="fur")


def decidueye_hisui(src, out):
    F = frame()
    k = Kit(src, coat="fur")
    body(k, F)
    d.jagged(k, F)
    crest(k, F)
    d.beak(k, F)
    cloak(k, F)
    chest_v(k, F)
    d.tail(k)
    cuffs(k, F)
    r.feet(k, F, colour=TALON, claw=CLAW)
    k.build(voxel=F.VOXEL, band=0.006, wband=0.05)
    k.eye_states(d.eyes(k, F))
    k.export(out)


if __name__ == "__main__":
    decidueye_hisui(*args())
