# Omanyte sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/omanyte.py -- <the export's Omanyte.glb> <out.glb>
# Written to be grown from: omastar.py imports it (sibling("omanyte")) and keeps its frame, the shell swept along a
# spiral, its rib lines, the blue body and its stubs, the way of making eyes.
#
# From the references: a pale blue soft body under a tall cream spiral shell. The shell is coiled side to side: its
# last whorl comes over the top and down the front and opens downward over the face, the earlier whorls coil in at the
# back, so each side shows a flat disc with a spiral groove; thin brown lines run across the whorls as growth lines,
# horizontal bands over the top and front, spokes on the sides. Under the shell's front lip the dark inside of the shell
# shows. Below it a blue face: two big round eyes close together, white in a black rim with a large round black pupil
# (the pictures show round pupils, not slits); two thick blue tentacles rise at the sides of the face like horns, their
# tips turned in; a beard of short thick tentacles fans out under the face, their tips curled out.
# Skeleton: body (0, 0, 0.22), head (0, -0.06, 0.18): the shell on the body, the soft parts on the head.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, srgb, inside, smoothstep, unit, eye_look, outward, args

SHELL, LINE, DARK, BLUE, DEEP = "#E6D8A8", "#9A8458", "#2A2420", "#5CB8DC", "#3E92BC"
WHITE, INK = "#F8F8F6", "#121216"

# --- the frame -------------------------------------------------------------------------------------------------------
CC = np.array([0.0, 0.05, 0.245])     # the coil's middle (its axis runs side to side)
R_END = 0.19                          # the last whorl's distance from the middle at the opening
TH_END = 110.0                        # where the opening is, degrees round from straight up toward the front
GROW = 2.3                            # how much the whorl grows in a turn
HC = 0.42                             # a whorl's half height across, a share of its distance from the middle
WX = (0.11, 0.35)                     # a whorl's half width: a + b times its distance
R_MIN = 0.018                         # where the coil starts
SQ = 2.2                              # how square a whorl's section is (2 round)


def r_of(th, r_end=R_END, th_end=TH_END, grow=GROW):
    return r_end * grow ** ((th - th_end) / 360.0)


def th_start(r_end=R_END, th_end=TH_END, grow=GROW, r_min=R_MIN):
    return th_end - 360.0 * math.log(r_end / r_min) / math.log(grow)


def spow(a, p):
    return np.sign(a) * np.abs(a) ** p


def whorl_at(cc, th, phi, r_end=R_END, th_end=TH_END, grow=GROW, hc=HC, wx=WX, sq=SQ, off=0.0, scale=1.0):
    """A point of the shell's surface: th (degrees) along the coil, phi (radians) round the section, 0 outward."""
    t = np.radians(th)
    r = r_of(th, r_end, th_end, grow)
    D = np.stack([np.zeros_like(t), -np.sin(t), np.cos(t)], axis=-1)
    h = hc * r * scale + off
    w = (wx[0] + wx[1] * r) * scale + off
    rad = h * spow(np.cos(phi), 2 / sq)
    x = w * spow(np.sin(phi), 2 / sq)
    return np.asarray(cc) + (r + rad)[..., None] * D + x[..., None] * np.array([1.0, 0, 0])


def shell(k, cc, colour=SHELL, lining=DARK, line=LINE, bone="body", r_end=R_END, th_end=TH_END, grow=GROW,
          hc=HC, wx=WX, rib_step=32.0, rib_w=0.008):
    """The spiral shell: its wall swept along the coil, a dark lining in its mouth, the growth lines across it."""
    t0 = th_start(r_end, th_end, grow)
    turns = (th_end - t0) / 360.0
    kw = dict(r_end=r_end, th_end=th_end, grow=grow, hc=hc, wx=wx)

    def wall(u, v):
        return whorl_at(cc, t0 + u * (th_end - t0), math.pi + 2 * math.pi * v, **kw)
    k.add("shell", outward(sheet(wall, int(70 * turns), 56, 0.012)), colour, bone, fuse=False, surface="shell")

    def lin(u, v):
        return whorl_at(cc, th_end - 70 + u * 69.5, math.pi + 2 * math.pi * v, scale=0.9, **kw)
    k.add("lining", outward(sheet(lin, 24, 48, 0.008)), lining, bone, fuse=False, surface="shell")

    # growth lines: thin bands round the outer side of the whorl, over the last turn and a half
    th = th_end - 4.0
    i = 0
    while th > th_end - 560:
        r = r_of(th, r_end, th_end, grow)
        dth = math.degrees(rib_w / max(r, 0.03))

        def rib(u, v, th=th, dth=dth):
            return whorl_at(cc, th + (v - 0.5) * dth, (u - 0.5) * math.radians(320), off=0.0065, **kw)
        k.add(f"rib{i}", outward(sheet(rib, 48, 3, 0.005)), line, bone, fuse=False, surface="shell")
        th -= rib_step
        i += 1


# --- the soft body ---------------------------------------------------------------------------------------------------
FACE_C, FACE_R = np.array([0.0, -0.15, 0.105]), (0.125, 0.115, 0.1)


def body(k, c=FACE_C, r=FACE_R, colour=BLUE):
    k.add("face", ell(c, r), colour, "head")
    # the face's front flattened a little, for the eyes
    k.add("front", ell(c + (0, -0.045, 0.01), (r[0] * 0.92, r[1] * 0.55, r[2] * 0.85)), colour, "head")
    # the body runs back under the shell, so nothing is seen through under it from behind
    k.add("foot", ell(c + (0, 0.16, -0.01), (r[0] * 0.95, 0.15, r[2] * 0.75)), colour, "head")
    # its top pushed up into the shell's mouth
    k.add("neck", ell(c + (0, 0.02, 0.08), (r[0] * 0.95, r[1] * 0.8, 0.07)), colour, "head")


def horns(k, root, pts, radii, colour=BLUE):
    """The two tentacles at the sides of the face, rising like horns."""
    k.add("horn", tube([np.array(root)] + [np.array(p) for p in pts], radii), colour, "head", mirror=True)


def stubs(k, c, n, spread, reach, drop, r0, r1, colour=BLUE, curl=0.03, z0=0.045, name="stub", out0=0.07):
    """The beard: short thick tentacles fanned out under the face, their tips curled out and up."""
    for j, a in enumerate(np.linspace(-spread, spread, n)):
        d = np.array([math.sin(math.radians(a)), -math.cos(math.radians(a)), 0.0])
        base = np.array([c[0], c[1], z0]) + d * out0
        mid = base + d * reach * 0.6 + (0, 0, -drop)
        tip = base + d * reach + (0, 0, -drop + curl)
        k.add(f"{name}{j}", tube([base, mid, tip], [r0, (r0 + r1) / 2, r1]), colour, "head")


# --- the eyes: big and round, white in a black rim, a large round black pupil ----------------------------------------
EYE_X, EYE_Y, EYE_Z, EYE_HALF = 0.062, -0.255, 0.122, 0.044


def eye_open(sclera=WHITE, pupil=INK, rim=0.82, pr=0.56, px=-0.06, slit=None):
    CW, CK = srgb(sclera), srgb(pupil)

    def eye(u, v):
        rho = np.sqrt(u * u + v * v)
        c = np.tile(CK, (len(u), 1))
        c[rho < rim] = CW
        if slit is None:
            c[((u - px) ** 2 + (v + 0.02) ** 2) < pr * pr] = CK
        else:
            c[(np.abs(u - px) < slit * (1 - (v / 0.75) ** 2) ** 0.5) & (np.abs(v) < 0.75)] = CK
        return c
    return eye


def eyes(k, state, at=(EYE_X, EYE_Y, EYE_Z), half=EYE_HALF, lid=BLUE, d=(0.3, -1.0, 0.08), look=None):
    lk = eye_look(state, look or eye_open(), srgb(lid), srgb(INK), line=0.17)
    for s in (1, -1):
        col = lk if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(lk)
        k.decal(np.array(at) * (s, 1, 1), unit(s * d[0], d[1], d[2]), (half, half * 1.05), col, eps=0.004,
                rings=20, segs=56)


def omanyte(src, out):
    k = Kit(src, coat="scales")
    shell(k, CC)
    body(k)
    horns(k, (0.095, -0.2, 0.135), [(0.14, -0.215, 0.15), (0.172, -0.22, 0.185), (0.172, -0.215, 0.225),
                                   (0.152, -0.205, 0.252)], [0.034, 0.033, 0.03, 0.025, 0.017])
    stubs(k, FACE_C, 9, 80, 0.095, 0.02, 0.03, 0.017)
    stubs(k, FACE_C + (0, 0.05, 0), 4, 125, 0.09, 0.02, 0.028, 0.016, name="side")
    k.build(voxel=0.006, smooth=4, band=0.008)
    k.eye_states(lambda st: eyes(k, st))
    k.export(out)


if __name__ == "__main__":
    omanyte(*args())
