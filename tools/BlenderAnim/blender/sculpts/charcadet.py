# Charcadet sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/charcadet.py -- <the export's Charcadet.glb> <out.glb>
# The base of its line: Armarouge and Ceruledge grow from it through sibling("charcadet") (its colours, its flames,
# its way of making the eye that peeps out where the shell opens).
#
# The design, from the references: a small dark child under a great black egg of a head, the head some two fifths of
# its height. The black shell covers the face and the left of the head, with three darker hollows on it like a mask's
# eye holes and mouth; to the model's +X it opens on red skin running to orange behind, and there one large yellow eye
# with a red rim and a red pupil peeps out, a red flame mark over it. A dark red band runs over the crown. On top a
# round red flame with a tail curling to +X and a violet-blue tip; an orange flame behind the right shoulder. A thin
# black body, a red bib with a yellow middle and red flame frills at its sides; short black arms ending in red fists;
# thin black legs with red boots, a dark red band round each. (The brief's "red helmet" is a black shell in both
# pictures: the red is the skin showing at its right side and the band over its crown.)
# Skeleton: body (0, 0, 0.27), head (0, 0, 0.34), flame (0, -0.01, 0.53), arms (+-0.04, 0, 0.31), legs (+-0.035, 0,
# 0.21), flameBack (0.02, 0.03, 0.25).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, polygon, outward, args

# --- the line's colours ------------------------------------------------------------------------------------------
SHELL, HOLE, SKIN, EMBER, AMBER, CROWN = "#3A383E", "#18171B", "#D8321E", "#F07A26", "#F6B03A", "#86221C"
COAL, RED, BOOTBAND, YELLOW, INK = "#2A272C", "#E2342A", "#7A1C18", "#F8D232", "#1E1012"
FLAME_RED, FLAME_VIOLET, FLAME_BLUE, FLAME_ORANGE, FLAME_HEART = "#F23A26", "#C04CD2", "#4A5CE8", "#F89A2C", "#FFD860"
EYE_YELLOW, EYE_RIM, EYE_PUPIL = "#FAD838", "#C42416", "#C42416"

HC, HR = np.array([0.0, -0.004, 0.452]), np.array([0.128, 0.122, 0.142])
EYE_AZ, EYE_ZR = 48.0, -0.18          # where the eye peeps out: degrees round from the front toward +X, height in radii


# --- helpers the line shares ---------------------------------------------------------------------------------------
def on_ell(c, r, d):
    """The point of an ellipsoid along a direction from its centre, and its outward normal there."""
    c, r = np.array(c, float), np.array(r, float)
    d = unit(*d)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def az_dir(az, zr, r=None):
    """A direction from a head's centre: az degrees from the front (-Y) toward +X, zr its height in radii."""
    a = math.radians(az)
    return (math.sin(a) * math.sqrt(max(1 - zr * zr, 0.05)), -math.cos(a) * math.sqrt(max(1 - zr * zr, 0.05)), zr)


def azimuth(P, c):
    """Degrees round an upright axis through c, 0 at the front (-Y), +90 at +X, +-180 behind."""
    return np.degrees(np.arctan2(P[:, 0] - c[0], -(P[:, 1] - c[1])))


def fframe(d, side):
    d = np.array(d, float); d /= np.linalg.norm(d)
    s = np.array(side, float); s -= d * np.dot(s, d); s /= np.linalg.norm(s)
    return np.stack([s, np.cross(d, s), d], axis=1)


_count = [0]


def tongue(base, d, length, width, side=(1, 0, 0), flick=0.3, thick=0.42, rag=0.2, waves=1.4, root=0.55, sub=3):
    """A ragged tongue of flame from base along d, flicking toward side near its tip (Ponyta's way)."""
    _count[0] += 1
    ph = 2.3 * _count[0]
    def shape(V):
        V = V.copy()
        t = np.clip((V[:, 2] + 1) / 2, 0, 1)
        w = (1 - t ** 1.1) ** 1.15 * (root + (1.55 - root) * np.sqrt(t))
        s = np.where(V[:, 0] >= 0, 1.0, -1.0)
        edge = 1 + rag * np.sin(2 * math.pi * waves * t + ph + s * 2.1) * smoothstep(0.1, 0.35, t)
        V[:, 0] *= w * edge; V[:, 1] *= w
        V[:, 0] += flick * (2.1 * t ** 2.4 - 0.55 * np.sin(math.pi * t))
        return V
    V, F = ell((0, 0, 0), (width, width * thick, length / 2), deform=shape, sub=sub)
    m = fframe(d, side)
    return outward((V @ m.T + np.array(base, float) + m[:, 2] * length / 2, F))


class Flames:
    """Tongues gathered a bone, a colour and a light to one loose piece, as a glowing piece wants."""
    def __init__(self):
        self.groups = {}

    def add(self, shape, color, bone, glow):
        self.groups.setdefault((bone, color, glow), []).append(shape)

    def put(self, k, name="flame"):
        for i, ((bone, color, glow), shapes) in enumerate(self.groups.items()):
            V, F, off = [], [], 0
            for sv, sf in shapes:
                V.append(sv); F.extend([[j + off for j in f] for f in sf]); off += len(sv)
            k.add(f"{name}{i}", (np.vstack(V), F), color, bone, fuse=False, glow=glow)


def ring_eye(iris=EYE_YELLOW, rim=EYE_RIM, pupil=EYE_PUPIL, pupil_u=-0.12):
    """The line's eye that peeps out where the shell opens: a bright iris, a rim a fifth of it, a solid pupil."""
    CI, CR, CP = srgb(iris), srgb(rim), srgb(pupil)
    def eye(u, v):
        c = np.tile(CI, (len(u), 1))
        o = u * u + v * v
        c[o > 0.62] = CR
        c[((u - pupil_u) / 0.3) ** 2 + ((v + 0.04) / 0.42) ** 2 < 1] = CP
        c[((u - 0.2) / 0.16) ** 2 + ((v - 0.3) / 0.16) ** 2 < 1] = (0.97, 0.95, 0.9)
        return c
    return eye


# --- Charcadet -----------------------------------------------------------------------------------------------------
def charcadet(src, out):
    k = Kit(src, coat="fur")
    flames = Flames()

    # The head: a great egg of shell, a short dark neck under it
    k.add("neck", tube([(0, 0.0, 0.3), (0, -0.002, 0.35)], [0.024, 0.026]), COAL, "head")
    k.add("head", ell(HC, HR), SKIN, "head")

    def shell(P):
        rel = P - HC
        zr = np.clip(rel[:, 2] / HR[2], -1, 1)
        a = azimuth(P, HC)
        # the shell's edge runs down the front of the +X side, drawn back round the eye
        edge = 42 - 22 * np.exp(-((zr - EYE_ZR) / 0.28) ** 2) + 14 * smoothstep(-0.2, -0.9, zr)
        # and closes again behind: the back of the head is shell, the skin a crescent down its +X side
        back = 150 - 25 * smoothstep(0.2, 0.9, zr)
        return np.maximum(inside(a - edge, 2.0), 1 - inside(a - back, 3.0)) * (np.linalg.norm(rel / HR, axis=1) < 1.3)
    k.paint(lambda P: shell(P), SHELL, on=["head"])
    # the skin runs from red at the shell's edge to orange and amber behind
    k.paint(lambda P: (1 - shell(P)) * smoothstep(70, 105, azimuth(P, HC)), EMBER, on=["head"])
    k.paint(lambda P: (1 - shell(P)) * smoothstep(105, 140, azimuth(P, HC)) * inside(P[:, 2] - HC[2] - 0.03, 0.02),
            AMBER, on=["head"])
    # the dark red band over the crown, from the brow to the back
    def crown(P):
        rel = (P - HC) / HR
        half = 0.2 + 0.08 * rel[:, 1]
        return inside(np.abs(rel[:, 0] - 0.1) - half, 0.02) * inside(0.62 - rel[:, 2] + 0.25 * np.clip(-rel[:, 1], 0, 1), 0.03)
    k.paint(crown, CROWN, on=["head"])

    # Hollows in the shell like a mask's eye holes and mouth, on the left of the face
    def hollow(az, zr, half, rot_up, shape=None):
        p, n = on_ell(HC, HR, az_dir(az, zr))
        k.decal(p, n, half, HOLE, up=rot_up, eps=0.0025, outline=shape, rings=10, segs=40)
    hollow(-32, 0.12, (0.026, 0.02), (0.35, 0, 1))
    hollow(4, 0.06, (0.016, 0.022), (-0.3, 0, 1))
    hollow(-14, -0.46, (0.02, 0.016), (0, 0, 1), polygon([(-1, 0.6), (1, 0.6), (0.0, -1)]))
    # the red flame mark over the eye, licking up into the shell
    pf, nf = on_ell(HC, HR, az_dir(EYE_AZ - 4, EYE_ZR + 0.42))
    k.decal(pf, nf, (0.016, 0.03), FLAME_RED, eps=0.003, rings=12, segs=44,
            outline=polygon([(0, -1), (0.8, -0.55), (0.75, 0.1), (0.3, 0.45), (0.25, 1.0), (-0.25, 0.35),
                             (-0.75, 0.0), (-0.7, -0.6)]))

    # The flame on top: a round red flame with a tail curling to +X and a violet-blue tip
    top = np.array([0.0, 0.016, 0.57])
    flames.add(tongue(top, unit(-0.12, 0.05, 1), 0.15, 0.05, side=(-1, 0, 0), flick=0.22, thick=0.8, root=1.0,
                      rag=0.12, sub=4), FLAME_RED, "flame", FLAME_RED)
    flames.add(tongue(top + (0.035, 0.0, 0.035), unit(1, 0.1, -0.25), 0.075, 0.026, side=(0, 0, -1), flick=0.35,
                      thick=0.7), FLAME_RED, "flame", FLAME_RED)
    flames.add(tongue(top + (-0.012, 0.004, 0.1), unit(-0.35, 0.05, 1), 0.07, 0.026, side=(-1, 0, 0), flick=0.4,
                      thick=0.7), FLAME_VIOLET, "flame", FLAME_VIOLET)
    flames.add(tongue(top + (-0.032, 0.006, 0.15), unit(-0.6, 0.05, 1), 0.04, 0.016, side=(-1, 0, 0), flick=0.4,
                      thick=0.7), FLAME_BLUE, "flame", FLAME_BLUE)

    # The body: thin and black, a red bib with a yellow middle, red frills of flame at its sides
    BC = np.array([0.0, 0.0, 0.272])
    k.add("chest", ell(BC, (0.044, 0.034, 0.058)), COAL, "body")
    k.add("pelvis", ell((0, 0.0, 0.214), (0.042, 0.032, 0.03)), COAL, "body")
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.012 - 0.5 * np.clip(P[:, 2] - 0.24, 0, 0.1), 0.002)
            * inside(0.238 - P[:, 2], 0.002) * inside(P[:, 1] + 0.005, 0.003), RED, on=["chest"])
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.0035 - 0.32 * np.clip(P[:, 2] - 0.25, 0, 0.1), 0.0015)
            * inside(0.255 - P[:, 2], 0.0015) * inside(P[:, 2] - 0.318, 0.002) * inside(P[:, 1] + 0.02, 0.003),
            YELLOW, on=["chest"])
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        n = 2 if s > 0 else 1
        for i in range(n):
            b = np.array([0.036 * s, -0.012, 0.3 - 0.03 * i])
            k.add(f"frill{side}{i}", tongue(b, unit(0.7 * s, -0.2, 0.7), 0.05 - 0.01 * i, 0.017, side=(0, 0, 1),
                                           flick=0.35 * s, thick=0.6), RED, "body", fuse=False)

    # The arms: short and black, red forearms and fists held forward and a little down before the belly
    SHO, ELB, HAND = np.array([0.04, 0.0, 0.31]), np.array([0.062, -0.03, 0.272]), np.array([0.05, -0.074, 0.25])
    k.add("upperarm", tube([SHO, ELB], [0.018, 0.016]), COAL, "arm{s}", mirror=True)
    k.add("forearm", tube([ELB, HAND], [0.017, 0.019]), RED, "arm{s}", mirror=True)
    k.add("fist", ell(HAND + (0.002, -0.008, -0.002), (0.024, 0.025, 0.023)), RED, "arm{s}", mirror=True)

    # The legs: thin and black, red boots with a dark red band round each
    HIP, KNEE, ANKLE = np.array([0.035, 0.0, 0.205]), np.array([0.046, -0.012, 0.12]), np.array([0.05, 0.0, 0.05])
    k.add("thigh", tube([HIP, KNEE], [0.025, 0.021]), COAL, "leg{s}", mirror=True)
    k.add("shin", tube([KNEE, ANKLE], [0.021, 0.022]), COAL, "leg{s}", mirror=True)
    k.add("boot", ell((0.05, -0.012, 0.028), (0.028, 0.04, 0.028)), RED, "leg{s}", mirror=True)
    k.paint(lambda P: inside(P[:, 2] - 0.075, 0.002) * (np.abs(P[:, 0]) > 0.02), RED, on=["shin", "boot"])
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.064) - 0.008, 0.0015) * (np.abs(P[:, 0]) > 0.02), BOOTBAND,
            on=["shin", "boot"])

    # The flame behind the right shoulder, orange with a yellow heart
    fb = np.array([0.03, 0.03, 0.29])
    flames.add(tongue(fb, unit(0.55, 0.55, 0.62), 0.15, 0.046, side=(0, 1, 0), flick=0.35), FLAME_ORANGE,
               "flameBack", FLAME_ORANGE)
    flames.add(tongue(fb + (0.02, 0.02, -0.02), unit(0.85, 0.45, 0.1), 0.08, 0.022, side=(0, 0, 1), flick=0.4),
               FLAME_ORANGE, "flameBack", FLAME_ORANGE)
    flames.add(tongue(fb + (0.004, -0.004, 0.004), unit(0.55, 0.55, 0.62), 0.08, 0.022, side=(0, 1, 0), flick=0.2,
                      thick=0.7), FLAME_HEART, "flameBack", FLAME_HEART)
    flames.put(k)

    k.build(voxel=0.0045, smooth=4, band=0.006, wband=0.04)

    # The eye: one, where the shell opens toward +X; on the red skin its lid is red
    EP, EN = on_ell(HC, HR, az_dir(EYE_AZ, EYE_ZR))
    look_open = ring_eye()
    def lay(state):
        k.decal(EP, EN, (0.032, 0.034), eye_look(state, look_open, srgb(SKIN), srgb(INK), line=0.17), eps=0.0022,
                rings=20, segs=56)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    charcadet(*args())
