# Rapanel (Carpathia No. 10160) sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py):
#   blender --background --factory-startup --python sculpts/rapanel.py -- <the export's Rapanel.glb> <out.glb>
# Written to be grown from: rapanor.py imports it (sibling("rapanel")) and keeps its frame: the coiled shell swept along
# a spiral that drifts toward +X and back as it shrinks, its ridges and brown bands, the soft grey body, the eye on its
# stalk and the way of laying it.
#
# The design, from the project's picture (docs/art/reference/gemini/carpathia/10160-Rapanel.jpg): a small sea snail
# creeping. A big round cream shell, some four fifths of the whole height and three quarters of its length, its
# outline lumpy with ridges; a spiral on its side (the eye of the coil a fifth from the back, a knob of small whorls
# standing out of it), the last whorl huge over the top and the front; wavy brown bands across the whorls along their
# growth lines. Under it a soft blue-grey body: a long flat foot with a wavy skirt running back past the shell as a
# thin tail, rising at the front into a thick neck and a round head; two stalks on the head's top, the eyes white balls
# with dark pupils on their tips, splayed and leaning forward; a little pair of tentacles low on the face pointing
# forward and down; a small wavy mouth; slime drips on the head; darker speckles on the body.
# Joints: body (0, 0, 0.12), head (0, -0.17, 0.08): the shell and the foot on the body, the neck and head on the head.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, srgb, inside, unit, eye_look, outward, args

SHELL, BAND, LINING = "#EDE2C6", "#9A6A40", "#C8B48E"
BODY, SPECK, SKIRT, SLIME = "#7B91A4", "#566B7E", "#9FB2C2", "#93A9BB"
WHITE, INK, IRIS, MOUTHC = "#F7F5EF", "#15171B", "#2B2F36", "#3A4652"


# --- the shell -------------------------------------------------------------------------------------------------------
class Shell:
    """A coiled shell: a section swept along a logarithmic spiral in the body's side plane (th in degrees from straight
    up toward the front), its middle drifting toward +X (the eye of the coil stands out on the front camera's side) and
    toward the back as the whorls shrink."""

    def __init__(self, cc, r_end=0.16, th_end=160.0, grow=3.0, hc=0.52, wx=(0.03, 0.62), drift=(0.22, 0.0),
                 r_min=0.01, ridge=0.07, ridges=11, sq=2.2, stretch=1.15):
        self.cc, self.r_end, self.th_end, self.grow, self.hc = np.array(cc, float), r_end, th_end, grow, hc
        self.wx, self.drift, self.r_min, self.ridge, self.ridges, self.sq, self.stretch = wx, drift, r_min, ridge, ridges, sq, stretch
        self.t0 = th_end - 360.0 * math.log(r_end / r_min) / math.log(grow)

    def r(self, th):
        return self.r_end * self.grow ** ((th - self.th_end) / 360.0)

    def at(self, th, phi, off=0.0, scale=1.0):
        t = np.radians(th)
        r = self.r(th)
        D = np.stack([np.zeros_like(t), -np.sin(t), np.cos(t)], axis=-1)
        cp = np.cos(phi)
        # the ridges: the outer side of the whorl bulges at intervals along it
        bump = 1 + self.ridge * (0.5 + 0.5 * np.cos(t * self.ridges)) ** 3 * np.clip(cp, 0, 1)
        h = self.hc * r * scale * bump + off
        w = (self.wx[0] + self.wx[1] * r) * scale + off
        rad = h * np.sign(cp) * np.abs(cp) ** (2 / self.sq)
        sp = np.sin(phi)
        x = w * np.sign(sp) * np.abs(sp) ** (2 / self.sq)
        k = self.r_end - r
        mid = self.cc + np.stack([k * self.drift[0], k * self.drift[1], np.zeros_like(k)], axis=-1)
        D = D * np.array([1.0, self.stretch, 1.0])
        return mid + (r + rad)[..., None] * D + x[..., None] * np.array([1.0, 0, 0])

    def add(self, k, colour=SHELL, band=BAND, lining=LINING, bone="body", thick=0.01, band_w=0.026, step=34.0,
            wave=11.0, lift=0.0055, turns_banded=1.25):
        turns = (self.th_end - self.t0) / 360.0

        def wall(u, v):
            return self.at(self.t0 + u * (self.th_end - self.t0), math.pi + 2 * math.pi * v)
        k.add("shell", outward(sheet(wall, int(105 * turns), 46, thick)), colour, bone, fuse=False, surface="shell")

        def lin(u, v):
            return self.at(self.th_end - 60 + u * 59.5, math.pi + 2 * math.pi * v, scale=0.9)
        k.add("lining", outward(sheet(lin, 24, 48, thick * 0.8)), lining, bone, fuse=False, surface="shell")

        # the brown bands across the whorls, each bending as a wave round the section
        th = self.th_end - 10.0
        i = 0
        while th > self.th_end - 360 * turns_banded:
            r = self.r(th)
            dth = math.degrees(band_w / max(r, 0.02)) * (0.8 + 0.4 * ((i * 7) % 3) / 2)

            def rib(u, v, th=th, dth=dth, i=i):
                phi = (u - 0.5) * math.radians(330)
                return self.at(th + (v - 0.5) * dth + wave * np.sin(2.2 * phi + i * 1.3), phi, off=lift)
            k.add(f"band{i}", outward(sheet(rib, 52, 3, lift * 1.2)), band, bone, fuse=False, surface="shell")
            th -= step * (0.85 + 0.3 * ((i * 5) % 4) / 3)
            i += 1


# --- the eye on its stalk --------------------------------------------------------------------------------------------
def eye_open(u, v, px=-0.05, py=-0.02, pr=0.58):
    """A white ball's front: a big dark pupil looking forward, a glint."""
    c = np.tile(srgb(WHITE), (len(u), 1))
    c[(u - px) ** 2 + (v - py) ** 2 < pr * pr] = srgb(IRIS)
    c[(u - px) ** 2 + (v - py) ** 2 < (pr * 0.72) ** 2] = srgb(INK)
    c[((u - px - 0.2) / 0.2) ** 2 + ((v - py - 0.24) / 0.2) ** 2 < 1] = srgb(WHITE)
    return c


def stalk(k, name, pts, radii, ball, colour=BODY, eye=WHITE, bone="head"):
    """A stalk through pts with a white ball fused on its tip (mirrored)."""
    k.add(name, tube(pts, radii), colour, bone, mirror=True)
    k.add(name + "ball", ell(ball[0], ball[1]), eye, bone, mirror=True)


def lay_eyes(k, state, ball_c, d, half, lid=BODY, look=eye_open, ink=INK):
    lk = eye_look(state, look, srgb(lid), srgb(ink), line=0.2)
    for s in (1, -1):
        col = lk if s > 0 else (lambda uu, v, f=lk: f(-uu, v))
        dd = unit(d[0] * s, d[1], d[2])
        k.decal(np.array(ball_c) * (s, 1, 1) + dd * 0.001, dd, (half, half), col, eps=0.0025, rings=20, segs=56)


def speckles(k, colour, region, n, seed, rmin, rmax, on):
    """Darker speckles over the body: n little round spots at places drawn once from a fixed seed inside a box."""
    rs = np.random.RandomState(seed)
    lo, hi = np.array(region[0]), np.array(region[1])
    C = lo + (hi - lo) * rs.rand(n, 3)
    R = rmin + (rmax - rmin) * rs.rand(n)
    def mask(P):
        m = np.zeros(len(P))
        for c, r in zip(C, R):
            m = np.maximum(m, inside(np.linalg.norm(P - c, axis=1) - r, 0.002))
        return m
    k.paint(mask, colour, on=on)


# --- Rapanel ---------------------------------------------------------------------------------------------------------
HEAD_C, HEAD_R = np.array([0.0, -0.19, 0.258]), np.array([0.074, 0.077, 0.074])
BALL_C, BALL_R = np.array([0.058, -0.232, 0.41]), 0.027
EYE_D = (0.32, -0.92, 0.18)


def rapanel(src, out):
    k = Kit(src, coat="skin")

    # The foot: long and flat, a wavy skirt, a thin tail running back past the shell
    def foot(V):
        V = V.copy()
        t = np.clip((V[:, 1] + 1) / 2, 0, 1)                    # 0 at the front, 1 at the tail
        V[:, 2] *= 1 - 0.55 * t ** 1.5
        V[:, 0] *= (1 - 0.45 * t ** 2) * (1 + 0.06 * np.sin(V[:, 1] * 14))
        return V
    k.add("foot", ell((0, 0.0, 0.038), (0.112, 0.29, 0.046), deform=foot), BODY, "body")
    k.add("under", ell((0, 0.075, 0.125), (0.112, 0.17, 0.095)), BODY, "body")

    # The thick neck rising out of the shell's mouth, the round head on it
    k.add("neck", tube([(0, -0.11, 0.105), (0, -0.16, 0.13), (0, -0.185, 0.2)], [0.095, 0.088, 0.075]), BODY, "head")
    k.add("head", ell(HEAD_C, HEAD_R), BODY, "head")
    k.add("snout", ell(HEAD_C + (0, -0.048, -0.03), (0.058, 0.05, 0.048)), BODY, "head")

    # The eye stalks on the head's top, splayed and leaning forward, the white balls on their tips
    stalk(k, "stalk", [(0.03, -0.185, 0.3), (0.042, -0.205, 0.355), BALL_C - (0.004, -0.004, 0.022)],
          [0.0125, 0.0108, 0.0102], (BALL_C, (BALL_R, BALL_R * 0.9, BALL_R)))
    # The little feelers low on the face, pointing forward and down, with a knob at each tip
    k.add("feeler", tube([(0.042, -0.25, 0.212), (0.058, -0.272, 0.19), (0.066, -0.282, 0.172)],
                         [0.012, 0.011, 0.0102]), BODY, "head", mirror=True)
    k.add("feelerknob", ell((0.067, -0.284, 0.168), (0.0135, 0.0135, 0.0135)), BODY, "head", mirror=True)

    # (The picture's slime drips on the head are left out: as lobes they read as little arms beside the face.)

    # A small smiling mouth low on the face
    def mouth(u, v):
        c = np.tile(srgb(BODY), (len(u), 1))
        c[(np.abs(v - (-0.35 + 0.8 * u * u)) < 0.3) & (np.abs(u) < 0.88)] = srgb(MOUTHC)
        return c
    k.decal((0, -0.285, 0.222), unit(0, -1, -0.15), (0.03, 0.012), mouth, eps=0.0025, rings=10, segs=56)

    # The paler skirt round the foot's edge, the speckles
    k.paint(lambda P: inside(P[:, 2] - 0.014, 0.004) * (P[:, 1] > -0.27), SKIRT, on=["foot"])
    speckles(k, SPECK, ((-0.11, -0.25, 0.0), (0.11, 0.3, 0.32)), 38, 7, 0.006, 0.011,
             on=["foot", "neck", "head", "snout", "under"])

    # The shell on the back
    sh = Shell((0, 0.083, 0.3))
    sh.add(k)

    k.build(voxel=0.0042, smooth=4, band=0.005, wband=0.04)
    k.eye_states(lambda st: lay_eyes(k, st, BALL_C, EYE_D, 0.02))
    k.export(out)


if __name__ == "__main__":
    rapanel(*args())
