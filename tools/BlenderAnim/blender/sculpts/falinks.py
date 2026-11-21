# Falinks sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/falinks.py -- <the export's Falinks.glb> <out.glb>
#
# The design, from the references: a squad of six round black troopers marching in a file, each in yellow armour: a
# yellow helmet over its top, sides and back whose front edge comes down to a point between the eyes, leaving a
# black face round two tall oval eyes (a dark outline, a cyan ring, a white middle); a round yellow shield on each
# side with a white face and a red cone for a boss; two black stubs of feet with white toes; red spikes along the
# helmet's crest. The brass, at the front, is the largest and carries a tall red horn leaning back with a red ridge
# behind it; the five troopers are smaller, two red spikes each.
# Each trooper is one function placed by a frame (trooper()), on its own bone. The five lean their faces a little to
# the front, the file standing in one line across the front (see FILE below).
# Skeleton: body (0, -0.2, 0.09), the brass; trooper1..5 from (-0.115, -0.12, 0.077) to (0.245, 0.12, 0.077).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, lathe, srgb, inside, unit, eye_look, args

BLACK, YELLOW, RED, WHITE, CYAN, INK = "#26262C", "#F2C230", "#D8302C", "#F4F4F0", "#46C8EC", "#121216"
TOE = "#E8E8E4"

# The file. The game's board looks at a model turned 31.5 degrees and frames a square of 1.3 x max(height, 0.85 x
# max(width, length)); the file on the joints' own diagonal (the brass at the front left, the troopers stepping back to
# the right) is seen broadside and was cut at both ends. Stood in one line across the front instead, the brass at the
# left, each ball drawn some 15% closer to the next than the joints stand (they touch and overlap a little), the board
# sees the line at 31.5 degrees and the four-corner sum 0.853 w + 0.522 l comes within 0.9 of the square. Each ball
# stays on its own bone; the parts are drawn off their joints, as the skeleton's file is longer and on a slant.
BRASS_X, FILE_Y, FIRST_GAP, GAP = -0.236, -0.03, 0.12, 0.095
FACING = (0.0, 0.0, 0.0, 0.0, 0.0)
EYE_AZ = (25, 25, 25, 25, 25, 25)
# (bone, centre, radius, facing, the brass?)
SQUAD = [("body", (BRASS_X, FILE_Y, 0.092), 0.078, 0.0, True)] + [
    (f"trooper{i}", (BRASS_X + FIRST_GAP + GAP * (i - 1), FILE_Y, 0.077), 0.053, FACING[i - 1], False)
    for i in range(1, 6)]
VOXEL = 0.0028


class Frame:
    """A trooper's own axes: f its front, s its right (+X when it faces the front), z up."""

    def __init__(self, c, r, face):
        a = math.radians(face)
        self.c = np.array(c, float)
        self.r = r
        self.R = np.array([r * 1.03, r, r * 0.96])          # a little wider than deep, a little flattened
        self.a = a
        self.f = np.array([math.sin(a), -math.cos(a), 0.0])
        self.s = np.array([math.cos(a), math.sin(a), 0.0])
        self.z = np.array([0, 0, 1.0])

    def world(self, x, y, z):
        """A point given in the trooper's own axes (x right, y forward, z up), in shares of its radius."""
        return self.c + self.r * (x * self.s + y * self.f + z * self.z)

    def way(self, x, y, z):
        d = x * self.s + y * self.f + z * self.z
        return d / np.linalg.norm(d)

    def on(self, az, el):
        """The ball's surface at azimuth az (degrees from its front toward its right) and elevation el, and its
        outward way."""
        A, E = math.radians(az), math.radians(el)
        dl = np.array([math.sin(A) * math.cos(E), math.cos(A) * math.cos(E), math.sin(E)])      # local x, y, z
        R = self.R
        Rl = np.array([R[0], R[1], R[2]])
        k = 1 / math.sqrt(((dl / Rl) ** 2).sum())
        pl = dl * k
        nl = pl / Rl ** 2
        nl /= np.linalg.norm(nl)
        p = self.c + pl[0] * self.s + pl[1] * self.f + pl[2] * self.z
        n = nl[0] * self.s + nl[1] * self.f + nl[2] * self.z
        return p, n

    def local(self, P):
        d = P - self.c
        return d @ self.s, d @ self.f, d[:, 2]


def cone(k, name, root, d, length, radius, colour, bone, seg=10):
    """A solid red spike: a cone from a base sunk a little behind root out along d."""
    d = unit(*d)
    k.add(name, lathe(root, d, [(0, -radius * 0.7), (radius, -radius * 0.2), (radius * 0.75, length * 0.35),
                                (radius * 0.3, length * 0.75), (0, length)], seg=seg), colour, bone, fuse=False,
          surface="shell")


def trooper(k, i, bone, c, r, face, brass):
    fr = Frame(c, r, face)
    name = f"ball{i}"
    k.add(name, ell(c, fr.R, rot=(0, 0, math.degrees(fr.a))), BLACK, bone)

    # the helmet: yellow over all but the face and the underside; its front edge comes down to a point between the eyes
    def helmet(P, fr=fr):
        x, y, z = fr.local(P)
        az = np.degrees(np.arctan2(x, y))
        el = np.degrees(np.arctan2(z, np.hypot(x, y)))
        edge = 13.0 + 0.9 * np.abs(az)                      # the face's top edge, low in the middle
        face_ = inside(np.abs(az) - 64, 2.5) * inside(el - edge, 2.5)
        under = inside(el + 34, 2.5)
        return (1 - face_) * (1 - under)
    k.paint(helmet, YELLOW, on=[name])

    # the feet: two black stubs under its front, their toes white
    for sx in (1, -1):
        fc = fr.world(0.42 * sx, 0.28, 0) * np.array([1, 1, 0]) + np.array([0, 0, 0.021])
        fn = f"foot{i}{'R' if sx > 0 else 'L'}"
        k.add(fn, ell(fc, (r * 0.24, r * 0.32, 0.021), rot=(0, 0, math.degrees(fr.a))), BLACK, bone)

        def toe(P, fc=fc, fr=fr):
            d = P - fc
            return inside(-(d @ fr.f - r * 0.2), 0.002) * inside(np.linalg.norm(d, axis=1) - r * 0.45, 0.002)
        k.paint(toe, TOE, on=[fn])

    # the shields: a yellow dish a side, a white face on it and a red cone for a boss
    for sx in (1, -1):
        axis = fr.way(sx, -0.05, -0.08)
        centre = fr.c + axis * fr.R[0] * 0.92
        rad = r * 0.52
        # a shield between two balls that touch would stand inside the next one: only the outer ones of the line
        if any(np.linalg.norm(centre - np.array(o[1])) < o[2] * 1.25 for o in SQUAD if o[0] != bone):
            continue
        k.add(f"shield{i}{sx}", lathe(centre, axis, [(0, -rad * 0.25), (rad * 0.9, -rad * 0.25), (rad, -rad * 0.05),
                                                      (rad * 0.98, rad * 0.12), (0, rad * 0.12)], seg=40),
              YELLOW, bone, fuse=False, surface="metal")
        k.add(f"shieldface{i}{sx}", lathe(centre, axis, [(0, 0.0), (rad * 0.72, 0.0), (rad * 0.7, rad * 0.2),
                                                          (0, rad * 0.2)], seg=40), WHITE, bone, fuse=False,
              surface="metal")
        cone(k, f"boss{i}{sx}", centre + axis * rad * 0.18, axis, rad * 0.58, rad * 0.32, RED, bone)

    # the crest: the brass's tall horn leaning back and a ridge behind it; two spikes on each trooper
    if brass:
        p, n = fr.on(0, 66)
        cone(k, f"horn{i}", p - n * r * 0.05, fr.way(0, -0.62, 1), r * 1.25, r * 0.36, RED, bone, seg=12)
        for el, ln, rd in ((32, 0.62, 0.24), (2, 0.42, 0.18)):
            p, n = fr.on(180, el)
            cone(k, f"ridge{i}{el}", p - n * r * 0.05, (n + fr.way(0, 0, 0.5)), r * ln, r * rd, RED, bone)
    else:
        for el, ln, rd, tilt in ((70, 0.62, 0.24, -0.5), (30, 0.5, 0.21, -0.2)):
            p, n = fr.on(180 if el < 50 else 0, el)
            cone(k, f"spike{i}{el}", p - n * r * 0.05, n + fr.way(0, tilt, 0.6), r * ln, r * rd, RED, bone)
    return fr


def eye_open(u, v):
    c = np.tile(srgb(WHITE), (len(u), 1))
    rr = np.hypot(u, v)
    c[rr > 0.5] = srgb(CYAN)
    c[rr > 0.83] = srgb(INK)
    return c


def clear_cast(fr, p, n, frames, reach=0.35):
    """
    A way to cast an eye's patch along that no other trooper stands in: a patch is cast from 0.35 out along its normal
    and lands on whatever it meets first, so a trooper standing behind another would wear its eyes on the other's
    back. Turned round the upright as little as will clear every other ball (each taken a little larger than it is).
    """
    ways = sorted(((turn, tilt) for turn in range(-60, 61, 5) for tilt in range(0, 46, 5)),
                  key=lambda w: w[0] ** 2 + 1.5 * w[1] ** 2)
    for turn, tilt in ways:
        t = math.radians(turn)
        m = np.array([[math.cos(t), -math.sin(t), 0], [math.sin(t), math.cos(t), 0], [0, 0, 1.0]])
        d = m @ n
        d = unit(*(d + np.array([0, 0, math.tan(math.radians(tilt))])))
        clear = True
        for o in frames:
            if o is fr:
                continue
            q = o.c - p
            s = np.clip(q @ d, 0, reach)
            if np.linalg.norm(q - d * s) < o.r * 1.02 + 0.012:
                clear = False
                break
        if clear:
            if turn or tilt:
                print(f"KIT: an eye of the trooper at {fr.c.round(3).tolist()} cast turned {turn}, tilted {tilt}")
            return d
    print("KIT: !! no clear way to cast an eye of the trooper at", fr.c.round(3).tolist())
    return n


def falinks(src, out):
    k = Kit(src, coat="metal")
    frames = [trooper(k, i, *t) for i, t in enumerate(SQUAD)]
    k.build(voxel=VOXEL, smooth=3, band=0.004, wband=0.012)

    V = np.array([v.co[:] for v in k.ob.data.vertices])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    sq = 1.3 * max(h, 0.85 * max(w, l))
    print("KIT: board: shows %.3f in a square of %.3f (%.2f); the four-corner sum %.3f (%.2f)"
          % (np.ptp(proj), sq, np.ptp(proj) / sq, 0.853 * w + 0.522 * l, (0.853 * w + 0.522 * l) / sq))

    def lay(state):
        look = eye_look(state, eye_open, srgb(BLACK), srgb(CYAN), line=0.16)
        for fr, (_, _, r, _, brass) in zip(frames, SQUAD):
            half = (r * 0.2, r * 0.31) if brass else (r * 0.22, r * 0.33)
            for sx in (1, -1):
                p, n = fr.on(sx * EYE_AZ[frames.index(fr)], -2)
                n = clear_cast(fr, p, n, frames)
                f = look if sx > 0 else (lambda u, v, g=look: g(-u, v))
                k.decal(p, n, half, f, up=(0, 0, 1), eps=0.0018, rings=20, segs=56)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    falinks(*args())
