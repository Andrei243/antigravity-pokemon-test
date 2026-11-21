# Mega Banette sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/banettemega.py -- <the export's Banette-Mega.glb> <out.glb>
#
# Grown from banette.py (its head, eyes and zipper pull are that script's). From the references: the doll's arms and
# its lower body are now three great cloth bags with two little pointed ears each, the design's largest masses: one
# hanging from each shoulder out to the side and a little forward and down, and one under the head for a body, each
# opening under a gold zipper rim onto a pink inside from which long pink claws of cloth hang down (three from each
# arm, one broad one from each side of the body for legs). The head the base's broad grey head with its pointed corners; the
# horn and the streamer gone into one long twisted ribbon rising from the crown, a gold zipper down its edge and a gold
# tab at its tip; a gold zipper down the face from the crown to the grin; the grin a thin gold zipper rising to the
# right, its pull at the -X end; the pink eyes with their slit. No star on a tail (neither picture shows one).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, unit, eye_look, polygon, args, sibling

base = sibling("banette")
H, HR = base.H, base.HR
EYE_ARC, EYE_Z, EYE_HALF, EYE_OUT = base.EYE_ARC, base.EYE_Z, base.EYE_HALF, base.EYE_OUT
head_dir, on_ell, head_shape, rho_of, eye_open = base.head_dir, base.on_ell, base.head_shape, base.rho_of, base.eye_open

GREY, GOLD, TICK, INK = base.GREY, base.GOLD, base.ZIPLINE, base.INK
PINK_IN, CLAW = "#E07A9C", "#EE8AA8"
VOXEL = 0.005


class Bag:
    """A cloth bag: an ellipsoid opening on the side `opening` looks to, past `depth` of its radius there."""

    def __init__(self, name, c, r, opening, depth, bone, ears, claws, claw_bone=None):
        self.name, self.c, self.r = name, np.array(c, float), np.array(r, float)
        self.n = unit(*opening)
        self.reach = 1 / math.sqrt(((self.n / self.r) ** 2).sum())          # the radius along the opening's way
        self.d = depth * self.reach
        self.bone, self.ears, self.claws = bone, ears, claws
        self.claw_bone = claw_bone or bone
        a = np.cross(self.n, (0, 0, 1.0))
        if np.linalg.norm(a) < 1e-3:
            a = np.array([1.0, 0, 0])
        self.e1 = a / np.linalg.norm(a)
        self.e2 = np.cross(self.n, self.e1)

    def rim(self, phi, push=1.0):
        """The point of the opening's rim at the angle phi round the opening's middle."""
        o = self.n * self.d
        dirn = math.cos(phi) * self.e1 + math.sin(phi) * self.e2
        q0, q1 = o / self.r, dirn / self.r
        A, B, C = q1 @ q1, 2 * q0 @ q1, q0 @ q0 - 1
        t = (-B + math.sqrt(B * B - 4 * A * C)) / (2 * A)
        return self.c + o + dirn * t * push

    def add(self, k):
        k.add(self.name, ell(self.c, self.r), GREY, self.bone)
        c, n, d = self.c, self.n, self.d
        k.paint(lambda P: inside(d - (P - c) @ n, 0.003), PINK_IN, on=[self.name])
        # the gold zipper round the opening, its seam where nobody looks
        ring = [self.rim(p, 1.035) for p in np.linspace(0, 2 * math.pi, 49)]
        k.add(self.name + "zip", tube(ring, [0.0105] * len(ring)), GOLD, self.bone, fuse=False)
        # little pointed ears
        for j, (at, way) in enumerate(self.ears):
            b = self.c + np.array(at) * self.r
            w = unit(*way)
            k.add(f"{self.name}ear{j}", tube([b - w * 0.01, b + w * 0.035, b + w * 0.065], [0.03, 0.017, 0.006]),
                  GREY, self.bone)
        # long pink claws of cloth hanging out of the opening
        for j, (phi, frac, ln, curl) in enumerate(self.claws):
            s = self.c + self.n * self.d + (self.rim(phi) - self.c - self.n * self.d) * frac
            down = np.array([0, 0, -1.0])
            p1 = s + self.n * 0.03 + down * 0.015
            p2 = p1 + unit(*(self.n * 0.5 + down)) * ln * 0.5
            p3 = p2 + unit(*(self.n * curl + down)) * ln * 0.5
            k.add(f"{self.name}claw{j}", blade([s - self.n * 0.01, p1, p2, p3], [0.03, 0.028, 0.019, 0.007],
                                                np.cross(self.n, down) if np.linalg.norm(np.cross(self.n, down)) > 1e-3
                                                else (1, 0, 0), 0.6), CLAW, self.claw_bone, fuse=False)


def arm_bag(s):
    side = "R" if s > 0 else "L"
    return Bag(f"bag{side}", (s * 0.25, -0.035, 0.235), (0.1, 0.088, 0.085), (s * 0.55, -0.35, -0.8), 0.32,
               f"arm{side}",
               ears=[((s * 0.55, 0.05, 0.75), (s * 0.35, 0, 1)), ((s * -0.25, 0.1, 0.9), (s * -0.2, 0.1, 1))],
               claws=[(math.pi * 0.5, 0.55, 0.125, 0.35), (math.pi * 0.85, 0.5, 0.115, 0.3),
                      (math.pi * 0.15, 0.5, 0.11, 0.4)])


def body_bag():
    return Bag("bagbody", (0, 0.0, 0.142), (0.118, 0.1, 0.118), (0, -0.85, -0.52), 0.5, "body",
               ears=[((0.82, 0.05, 0.45), (1, 0, 0.6)), ((-0.82, 0.05, 0.45), (-1, 0, 0.6))],
               claws=[], claw_bone="body")


def antenna(k):
    """The horn and the streamer as one long twisted ribbon from the crown, a gold zipper down its edge."""
    P = np.array([(0.0, 0.02, 0.5), (0.0, 0.04, 0.57), (0.045, 0.055, 0.615), (0.02, 0.07, 0.655),
                  (-0.035, 0.07, 0.685), (-0.02, 0.055, 0.72)])
    R = [0.045, 0.034, 0.028, 0.024, 0.02, 0.017]
    k.add("antenna", blade(P, R, (0.3, -1, 0), 0.55), GREY, "head")
    zip_pts = P[1:] + np.array([0.0, -1.0, 0.0])[None] * np.array(R[1:])[:, None] * 0.62
    k.add("antennazip", tube(zip_pts, [0.0105] * len(zip_pts)), GOLD, "head", fuse=False)
    t = P[-1] + np.array([-0.005, -0.004, 0.012])
    pts = []
    for dx in (-0.024, 0.024):
        for dy in (-0.007, 0.007):
            for dz in (0.0, 0.03):
                pts.append(t + np.array([dx, dy, dz]))
    k.add("antennatab", hull(pts), GOLD, "head", fuse=False)


def zipper(u, v, rho=None, ticks_along_u=True, n=9):
    """A zipper's colour: gold with dark teeth across it, a dark edge."""
    c = np.tile(srgb(GOLD), (len(u), 1))
    t = (u if ticks_along_u else v) * n
    c[np.abs(t - np.round(t)) < 0.16] = srgb(TICK)
    if rho is not None:
        c[rho > 0.84] = srgb(INK)
    return c


GRIN_OUT = ([(u, 0.32 + 0.45 * u * u + 0.22 * u) for u in np.linspace(1, -1, 15)]
            + [(u, -0.32 + 0.45 * u * u + 0.22 * u) for u in np.linspace(-1, 1, 15)[1:-1]])


def face(k):
    rho = rho_of(GRIN_OUT)
    d = head_dir(0.0, 0.335)
    k.decal(on_ell(H, HR, d) + d * 0.004, d, (0.112, 0.04),
            lambda u, v: zipper(u, v, rho(u, v), True, 8), eps=0.003, outline=polygon(GRIN_OUT), rings=16, segs=120)
    # the zipper down the face, from the crown to the grin, a little to -X of the middle
    for z0, z1 in ((0.47, 0.41), (0.41, 0.35)):
        zm = (z0 + z1) / 2
        d = head_dir(-0.012, zm)
        k.decal(on_ell(H, HR, d), d, (0.011, (z0 - z1) / 2 + 0.004),
                lambda u, v: zipper(u, v, None, False, 3), eps=0.0032, rings=10, segs=40)
    d = unit(0, -0.45, 1)
    k.decal(on_ell(H, HR, d), d, (0.011, 0.045), lambda u, v: zipper(u, v, None, False, 3), eps=0.0032,
            up=(0, 1, 0.45), rings=10, segs=40)


def banette_mega(src, out):
    k = Kit(src, coat="fur")
    # --- the head: the base's, its horn and streamer gone into the ribbon ---------------------------------------
    k.add("head", ell(H, HR, deform=head_shape), GREY, "head")
    k.add("ear", tube([(0.095, 0.0, 0.45), (0.135, 0.0, 0.5), (0.165, 0.0, 0.53)], [0.048, 0.027, 0.013]),
          GREY, "head", mirror=True)
    antenna(k)
    base.pull(k)
    face(k)

    # --- the body: a short neck, the three bags --------------------------------------------------------------------
    k.add("neck", ell((0, 0.0, 0.27), (0.07, 0.06, 0.05)), GREY, "body")
    body_bag().add(k)
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        k.add(f"sleeve{side}", tube([(s * 0.06, 0.0, 0.29), (s * 0.13, -0.015, 0.275), (s * 0.19, -0.03, 0.26)],
                                    [0.034, 0.034, 0.04]), GREY, f"arm{side}")
        arm_bag(s).add(k)
        # the legs: a broad pink claw out of the lower front of the body's bag on each side, down to the ground
        leg = [(s * 0.04, -0.04, 0.08), (s * 0.055, -0.085, 0.055), (s * 0.068, -0.105, 0.028),
               (s * 0.078, -0.128, 0.018)]
        k.add(f"leg{side}", blade(leg, [0.036, 0.032, 0.022, 0.009], (1, 0, 0), 0.6), CLAW, f"leg{side}", fuse=False)

    k.build(voxel=VOXEL, smooth=3, band=0.005)

    out_r = polygon(EYE_OUT)
    out_l = polygon([(-a, b) for a, b in EYE_OUT])

    def fierce(u, v):
        c = eye_open(u, v)
        c[v > 0.2 + 0.5 * u] = srgb(GREY)
        c[np.abs(v - (0.2 + 0.5 * u)) < 0.14] = srgb(INK)
        return c

    def eyes(state):
        for side in (1, -1):
            d = head_dir(EYE_ARC * side, EYE_Z)
            look = fierce if state == "Fierce" else eye_look(state, eye_open, srgb(GREY), srgb(INK), line=0.16)
            col = look if side == 1 else (lambda u, v, f=look: f(-u, v))
            k.decal(on_ell(H, HR, d), d, EYE_HALF, col, eps=0.0035, outline=out_r if side == 1 else out_l,
                    rings=20, segs=56)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    banette_mega(*args())
