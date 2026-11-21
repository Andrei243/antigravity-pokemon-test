# Dartrix sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dartrix.py -- <the export's Dartrix.glb> <out.glb>
# Rowlet grown (rowlet.py's frame helpers, bow tie, feet and way of making the eyes): from the references, a taller
# owl, its head and chest dark brown, a white heart of a face pointed at the chin with its eyes shut in two calm arcs,
# a beak of white over orange; two lime-green crescents of leaves falling from the crown down each side of the face,
# the one on its right (the viewer's left) the larger; a light green bow tie of two drooping leaves at the neck; the
# brown chest ending in a jagged edge over a white lower body like breeches; white wings folded at the sides, two tan
# spots on each, a green leaf with an orange tip at each wrist; green leaf feathers hanging behind the legs; orange feet.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, leaf, outward, srgb, inside, tri, unit, eye_look, args, sibling

r = sibling("rowlet")
p = r.p

BROWN, WHITE, LIME, LIMEDARK = "#7A4E31", "#F5F3EF", "#9CD452", "#6FAE34"
TIE, TIEDARK, SPOT, ORANGE = "#8CCB4E", "#5E9E36", "#D8A26C", "#EC6A2C"
BEAKC, FOOT = "#ECEAE6", "#EE7A34"


def frame():
    F = SimpleNamespace()
    F.H = np.array([0.0, -0.012, 0.302])             # the head's centre, over the head's joint (0, 0, 0.24)
    F.HS = 1.08                                     # the head a little larger than measured
    F.HR = np.array([0.074, 0.066, 0.07]) * F.HS
    F.FC = np.array([0.0, -0.036, 0.298])           # the flatter face for its patches
    F.FR = np.array([0.062, 0.046, 0.054]) * F.HS
    F.BODY = (np.array([0.0, 0.006, 0.172]), np.array([0.082, 0.07, 0.112]))
    F.NECK = (np.array([0.0, -0.004, 0.228]), np.array([0.068, 0.06, 0.05]))
    F.LEGX, F.LEGY, F.LEGTOP = 0.036, -0.012, 0.05
    F.LEGR = (0.014, 0.012)
    F.TOE, F.TOER = 0.036, 0.0075
    F.EYEX, F.EYEZ = 0.032, 0.306
    F.EYE = (0.021, 0.014)
    F.VOXEL = 0.0038
    F.HEM = 0.128                                    # where the brown chest ends over the white
    return F


def body(k, F):
    c, rr = F.BODY
    k.add("body", ell(c, rr), BROWN, "body")
    k.add("neck", ell(*F.NECK), BROWN, "body")
    k.add("head", ell(F.H, F.HR), BROWN, "head")
    k.add("face", ell(F.FC, F.FR), BROWN, "head")
    # the white breeches: great round haunches fused into the lower body, down to the ankles
    k.add("haunch", ell((0.038, -0.004, 0.088), (0.046, 0.05, 0.062)), WHITE, "leg{s}", mirror=True)
    k.add("seat", ell((0.0, 0.012, 0.1), (0.06, 0.05, 0.05)), WHITE, "body")
    # the brown ends in a jagged hem over the white
    k.paint(lambda P: inside(P[:, 2] - (F.HEM + 0.012 * tri(np.arctan2(P[:, 0], -P[:, 1]) / 0.55 + 0.5)), 0.0018),
            WHITE, on=["body", "haunch", "seat"])
    k.paint(lambda P: inside(P[:, 2] - (F.HEM - 0.012), 0.002), WHITE, on=["body"])


HEART = [(0.0, 0.33), (0.014, 0.352), (0.034, 0.362), (0.054, 0.352), (0.066, 0.328), (0.07, 0.3), (0.064, 0.274),
         (0.048, 0.256), (0.026, 0.243), (0.0, 0.235)]


def face(k, F):
    rc = r.round_coords(F.H[1], 0.074 * F.HS)
    poly = [(a * F.HS, 0.3 + (z - 0.3) * F.HS) for a, z in p.mirrored(HEART)]
    k.paint(lambda P: p.poly_mask(*rc(P), poly, edge=0.0018) * (P[:, 1] < F.H[1] - 0.01) * (P[:, 2] > 0.22), WHITE,
            on=["face", "head", "neck"])


def hood(k, F):
    """Two lime crescents from the crown down the face's sides, each a tapering blade laid on the head."""
    for s, pts, rad in ((-1, [(-0.004, -0.012, 0.376), (-0.042, -0.03, 0.378), (-0.074, -0.04, 0.338),
                              (-0.078, -0.046, 0.292), (-0.062, -0.05, 0.252)], [0.017, 0.025, 0.025, 0.017, 0.006]),
                        (1, [(0.004, -0.012, 0.376), (0.04, -0.028, 0.374), (0.07, -0.036, 0.338),
                             (0.068, -0.044, 0.298)], [0.015, 0.021, 0.016, 0.005])):
        pts = F.H + (np.array(pts) - F.H) * F.HS
        k.add("hood", blade(pts, np.array(rad) * F.HS, unit(s * 0.8, -0.35, 0.5), 0.55), LIME, "head", fuse=False, surface="leaf")


def beak(k, F):
    P, n = r.on_ell(F.FC, F.FR, 0.0, 0.29)
    k.add("beak", r.ell_frame(P + n * 0.004, (0.015, 0.011, 0.016), (1, 0, 0), n), BEAKC, "head", fuse=False,
          surface="shell")
    Q, m = r.on_ell(F.FC, F.FR, 0.0, 0.27)
    down = unit(0, -0.45, -1)
    k.add("beaktip", p.spike(Q + m * 0.004 + np.array([0, 0, 0.012]), Q + m * 0.012 + down * 0.026, 0.03, 0.02,
                             side=(1, 0, 0), at=0.25), ORANGE, "head", fuse=False, surface="shell")


def wings(k, F):
    """The folded wings: white, hanging from the shoulders down the sides, two tan spots and a leaf at the wrist."""
    for s, side in ((1, "R"), (-1, "L")):
        b = f"wing{side}"
        # its top tucked in under the brown shoulder, its lower end flaring a little out and pointed below
        k.add(f"wing{side}", ell((0.084 * s, 0.006, 0.142), (0.024, 0.054, 0.092), rot=(8, -9 * s, 0),
                                 deform=lambda V: np.column_stack([V[:, 0], V[:, 1] * (1 - 0.3 * np.clip(-V[:, 2], 0, 1)),
                                                                   V[:, 2]])),
              WHITE, b, fuse=False, surface="fur")
        for y, z in ((-0.022, 0.162), (0.006, 0.152)):
            P = np.array([0.106 * s, y, z])
            k.add(f"spot{side}", r.ell_frame(P, (0.011, 0.004, 0.014), (0, 1, 0), (s, 0, 0)), SPOT, b, fuse=False)


def tail(k):
    for s in (1, 0, -1):
        base = np.array([0.012 * s, 0.052, 0.11])
        d = unit(0.25 * s, 0.55, -1)
        k.add("tail", outward(leaf(base, d, 0.075 - 0.01 * abs(s), 0.024, thick=0.012)),
              LIME if s == 0 else LIMEDARK, "body", fuse=False, surface="leaf")


# --- the eyes: shut in calm arcs, as both pictures show them ---------------------------------------------------------
INK, LIDC = srgb("#1E1A1C"), srgb(WHITE)


def arc(lift, bend, line=0.2):
    def f(u, v):
        c = np.tile(LIDC, (len(u), 1))
        c[(np.abs(u) < 0.9) & (np.abs(v - (lift + bend * (1 - u * u))) < line)] = INK
        return c
    return f


def fierce(u, v):
    c = np.tile(LIDC, (len(u), 1))
    c[(np.abs(u) < 0.9) & (np.abs(v - (0.05 + 0.45 * u)) < 0.2)] = INK
    return c


def eyes(k, F):
    centre, normal = r.on_ell(F.FC, F.FR, F.EYEX, F.EYEZ)
    normal = unit(*(normal + np.array([0.15, 0, 0])))
    def lay(state):
        if state == "Open":
            look = arc(-0.35, 0.6)
        elif state == "Shut":
            look = arc(-0.5, 0.45)
        elif state == "Fierce":
            look = fierce
        else:
            look = eye_look(state, arc(-0.35, 0.6), LIDC, INK, line=0.2)
        k.decal(centre, normal, F.EYE, look, eps=0.0022, rings=20, segs=56, mirror=True)
    return lay


def dartrix(src, out):
    F = frame()
    k = Kit(src, coat="fur")
    body(k, F)
    face(k, F)
    hood(k, F)
    beak(k, F)
    nc, nr = F.NECK
    r.bow_tie(k, nc, nr * [1.0, 1.04, 1.0], 0.218, 0.034, 0.02, 0.01, TIE, TIEDARK)
    wings(k, F)
    tail(k)
    r.feet(k, F, colour=FOOT)
    k.build(voxel=F.VOXEL, band=0.005, wband=0.04)
    k.eye_states(eyes(k, F))
    k.export(out)


if __name__ == "__main__":
    dartrix(*args())
