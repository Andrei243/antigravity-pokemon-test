# Mega Falinks sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/falinksmega.py -- <the export's Falinks-Mega.glb> <out.glb>
#
# Grown from falinks.py (which builds under __main__: its Frame, cone, eye_open and clear_cast are imported; its
# trooper is copied here and given a say over which shields and spikes each trooper wears): the same round black
# troopers in yellow helmets coming down to a point between two tall cyan-rimmed eyes, the same white-faced shields
# with red bosses, the same red cones.
#
# What the Mega changes, from the pictures: the file of six stands up as one soldier, largest mass first:
#   1. a great yellow shield on the +X arm, a shallow cone like a broad hat, a white ring and a red cone in its middle
#      (some 0.74 of the height across in the picture; 0.49 here, its face turned out and to the front);
#   2. a long red lance out of the -X arm (some 0.59 of the height in the picture, 0.55 here), pointing down and to
#      the front where the art points it out to the side;
#   3. the brass is the head and chest, the largest trooper (0.39 of the height across, as in the picture), its tall
#      red horn over it, a red ridge behind and a small yellow point on its brow between the eyes, a shield at each
#      side for a shoulder;
#   4. two troopers for arms at its sides (0.22 of the height across), each with two red spikes;
#   5. one trooper under the brass for a waist, its eyes squeezed shut in strain (><), and two at the bottom for
#      feet, each with its red spikes, a shield on its outer side and its little black feet with white toes.
# The skeleton's body and head stand where the brass is; the waist is on body, the feet on the legs, the arms on the
# arms; the horn and the brow's point are on head. The model is wider than tall: 0.77 wide, 0.37 long and 0.67 tall;
# turned as the board turns it, it fills 0.94 of its square (the lance and the shield drawn in from the picture's
# reach so that it does).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, lathe, srgb, inside, unit, eye_look, args, sibling

base = sibling("falinks")
Frame, cone, eye_open, clear_cast = base.Frame, base.cone, base.eye_open, base.clear_cast
BLACK, YELLOW, RED, WHITE, CYAN, TOE = base.BLACK, base.YELLOW, base.RED, base.WHITE, base.CYAN, base.TOE

# (name, bone, centre, radius, the brass?, shield sides (+1 its right, -1 its left), spikes?)
BRASS = ("brass", "body", (0.0, -0.02, 0.4), 0.14, True, (1, -1))
WAIST = ("waist", "body", (0.0, -0.012, 0.198), 0.07, False, (1, -1))
ARM_R = ("armr", "armR", (0.232, -0.006, 0.405), 0.08, False, ())
ARM_L = ("arml", "armL", (-0.232, -0.006, 0.405), 0.08, False, (-1,))
FOOT_R = ("footr", "legR", (0.11, -0.02, 0.074), 0.072, False, (1,))
FOOT_L = ("footl", "legL", (-0.11, -0.02, 0.074), 0.072, False, (-1,))
SQUAD = [BRASS, ARM_R, ARM_L, WAIST, FOOT_R, FOOT_L]
VOXEL = 0.0055


def trooper(k, name, bone, c, r, brass, shields):
    fr = Frame(c, r, 0.0)
    k.add(name, ell(c, fr.R), BLACK, bone)

    def helmet(P, fr=fr):
        x, y, z = fr.local(P)
        az = np.degrees(np.arctan2(x, y))
        el = np.degrees(np.arctan2(z, np.hypot(x, y)))
        edge = 13.0 + 0.9 * np.abs(az)
        face_ = inside(np.abs(az) - 64, 2.5) * inside(el - edge, 2.5)
        under = inside(el + 34, 2.5)
        return (1 - face_) * (1 - under)
    k.paint(helmet, YELLOW, on=[name])

    if name.startswith("foot"):
        for sx in (1, -1):
            fc = fr.world(0.42 * sx, 0.28, 0) * np.array([1, 1, 0]) + np.array([0, 0, 0.024])
            fn = f"toes{name}{'R' if sx > 0 else 'L'}"
            k.add(fn, ell(fc, (r * 0.26, r * 0.34, 0.024)), BLACK, bone)

            def toe(P, fc=fc, fr=fr):
                d = P - fc
                return inside(-(d @ fr.f - r * 0.2), 0.002) * inside(np.linalg.norm(d, axis=1) - r * 0.48, 0.002)
            k.paint(toe, TOE, on=[fn])

    for sx in shields:
        axis = fr.way(sx, -0.05, -0.08)
        centre = fr.c + axis * fr.R[0] * 0.92
        rad = r * 0.52
        k.add(f"shield{name}{sx}", lathe(centre, axis, [(0, -rad * 0.25), (rad * 0.9, -rad * 0.25), (rad, -rad * 0.05),
                                                         (rad * 0.98, rad * 0.12), (0, rad * 0.12)], seg=40),
              YELLOW, bone, fuse=False, surface="metal")
        k.add(f"shieldface{name}{sx}", lathe(centre, axis, [(0, 0.0), (rad * 0.72, 0.0), (rad * 0.7, rad * 0.2),
                                                             (0, rad * 0.2)], seg=40), WHITE, bone, fuse=False,
              surface="metal")
        cone(k, f"boss{name}{sx}", centre + axis * rad * 0.18, axis, rad * 0.58, rad * 0.32, RED, bone)

    if brass:
        p, n = fr.on(0, 66)
        cone(k, f"horn{name}", p - n * r * 0.05, fr.way(0, -0.5, 1), r * 1.25, r * 0.3, RED, "head", seg=14)
        for el, ln, rd in ((44, 0.6, 0.22),):
            p, n = fr.on(180, el)
            cone(k, f"ridge{name}{el}", p - n * r * 0.05, (n + fr.way(0, 0, 0.5)), r * ln, r * rd, RED, "head")
        # the small yellow point on the brow, where the helmet comes down between the eyes
        p, n = fr.on(0, 36)
        cone(k, f"brow{name}", p - n * r * 0.04, n + fr.way(0, 0, 1.4), r * 0.42, r * 0.13, YELLOW, "head")
    else:
        for el, ln, rd, tilt in ((70, 0.62, 0.24, -0.5), (30, 0.5, 0.21, -0.2)):
            p, n = fr.on(180 if el < 50 else 0, el)
            cone(k, f"spike{name}{el}", p - n * r * 0.05, n + fr.way(0, tilt, 0.6), r * ln, r * rd, RED, bone)
    return fr


def arms(k):
    # the lance: a long red cone out of the -X arm's outer, lower side, down and to the front
    c, r = np.array(ARM_L[2]), ARM_L[3]
    d = unit(-0.32, -0.6, -0.74)
    root = c + unit(-0.55, -0.55, -0.5) * r * 0.55
    k.add("lancering", lathe(root + d * r * 0.25, d, [(0, -0.012), (0.056, -0.012), (0.058, 0.0), (0.054, 0.012),
                                                       (0, 0.012)], seg=32), WHITE, "armL", fuse=False,
          surface="metal")
    k.add("lance", lathe(root, d, [(0, -0.02), (0.048, -0.02), (0.05, 0.03), (0.036, 0.14), (0.018, 0.27),
                                   (0, 0.37)], seg=32), RED, "armL", fuse=False, surface="shell")
    # the great shield on the +X arm: a shallow cone like a broad hat, its face out and to the front
    c, r = np.array(ARM_R[2]), ARM_R[3]
    ax = unit(0.74, -0.65, 0.12)
    sc = c + ax * r * 0.78
    R = 0.166
    k.add("bigshield", lathe(sc, ax, [(0, -0.012), (R * 0.5, -0.016), (R * 0.96, -0.012), (R, 0.0), (R * 0.97, 0.012),
                                      (R * 0.82, 0.02), (R * 0.55, 0.04), (R * 0.42, 0.05), (0, 0.054)], seg=64),
          YELLOW, "armR", fuse=False, surface="metal")
    k.add("bigshieldring", lathe(sc, ax, [(R * 0.24, 0.05), (R * 0.4, 0.044), (R * 0.41, 0.054), (R * 0.25, 0.062),
                                          (R * 0.24, 0.05)], seg=48), WHITE, "armR", fuse=False, surface="metal")
    cone(k, "bigboss", sc + ax * 0.05, ax, 0.075, R * 0.26, RED, "armR", seg=24)


def falinks_mega(src, out):
    k = Kit(src, coat="metal")
    frames = [trooper(k, *t[:5], t[5]) for t in SQUAD]
    arms(k)
    k.build(voxel=VOXEL, smooth=3, band=0.004, wband=0.03)

    V = np.array([v.co[:] for v in k.ob.data.vertices])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    sq = 1.3 * max(h, 0.85 * max(w, l))
    print("KIT: board: shows %.3f in a square of %.3f (%.2f); the four-corner sum %.3f (%.2f)"
          % (np.ptp(proj), sq, np.ptp(proj) / sq, 0.853 * w + 0.522 * l, (0.853 * w + 0.522 * l) / sq))

    def lay(state):
        look = eye_look(state, eye_open, srgb(BLACK), srgb(CYAN), line=0.16)
        # the waist strains: its open eyes are squeezed shut
        strain = eye_look("Squeeze" if state in ("Open", "Fierce") else state, eye_open, srgb(BLACK), srgb(CYAN),
                          line=0.2)
        for fr, t in zip(frames, SQUAD):
            name, r, brass = t[0], t[3], t[4]
            half = (r * 0.2, r * 0.31) if brass else (r * 0.22, r * 0.33)
            g = strain if name == "waist" else look
            for sx in (1, -1):
                p, n = fr.on(sx * 25, -2)
                n = clear_cast(fr, p, n, frames)
                f = g if sx > 0 else (lambda u, v, g=g: g(-u, v))
                k.decal(p, n, half, f, up=(0, 0, 1), eps=0.0025, rings=20, segs=56)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    falinks_mega(*args())
