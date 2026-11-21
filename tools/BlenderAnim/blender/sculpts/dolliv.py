# Dolliv sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dolliv.py -- <the export's Dolliv.glb> <out.glb>
#
# Grown from Smoliv's script (sibling("smoliv")): the same olive with its socket, the same fleshy leaves, the same
# black eye with a big glint. From the references: a little olive doll. A big round pale yellow-green face; a cap of
# green leaves over its top whose rounded lobes hang to just above the eyes, two leaves standing up on it like a bow;
# a great pale yellow olive lying out to each side behind the cap like a bun of hair, its socket at the outer end;
# small black eyes rimmed white, a tiny mouth; a collar of pointed leaves round the neck; a dress of green leaves from
# the collar flaring to the ground, their tips turned out white at the hem; two leaf arms (out and down in the art,
# carried forward and down here) with split tips; a stub of pale leg under the hem.
# (The task's "small girl of a plant with two olives like buns" holds; the pictures add the cap of lobes and the white
# hem, and show the olives yellow, lying level at the cap's sides.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, srgb, inside, smoothstep, unit, eye_look, args, outward, sibling

sm = sibling("smoliv")

FACE = "#ECF0BC"
CAP, CAP_DARK = "#62B240", "#4C9C36"
DRESS, DRESS_IN = "#5AAE3A", "#4A9A32"
HEM = "#F2F2EA"
LEG = "#E2E8BE"
OLIVE_Y = sm.OLIVE_Y
MOUTH = srgb("#3E5A2A")
INK = sm.INK
WHITE = sm.WHITE

# --- Dolliv's frame (joints: body (0, 0, 0.2), arms (+-0.035, 0, 0.27), head (0, 0, 0.3), legs (+-0.025, 0, 0.14);
#     about 0.6 tall) ---------------------------------------------------------------------------------------------
HC = np.array([0.0, -0.004, 0.388])      # the face's middle
HR = np.array([0.1, 0.092, 0.09])
EYE_D = (0.52, -0.8, -0.4)
EYE_HALF = (0.0205, 0.0215)
OLIVE_X, OLIVE_Z = 0.14, 0.44
OLIVE_R = (0.054, 0.05, 0.074)          # across, across, along
HEM_Z, HEM_R = 0.036, 0.118


def rimmed_eye(u, v):
    """Dolliv's eye: Smoliv's black eye grown, a white ring round its black middle under a dark rim, and a glint."""
    r = np.sqrt(u * u + v * v)
    c = np.tile(INK, (len(u), 1))
    c[(r > 0.5) & (r < 0.8)] = WHITE
    c[((u + 0.14) / 0.2) ** 2 + ((v - 0.18) / 0.2) ** 2 < 1] = WHITE
    return c


def cap(k, c, r, lobes=(-100, -30, 30, 100), low=0.06, back=-0.35, bone="head", colour=CAP):
    """
    A cap of leaf lobes over the head: a loose shell a little over the head, everything under its scalloped edge drawn
    in under the skin (so the edge is the shell's own crisp rim). lobes: their middles in degrees from the front; low:
    how far down (unit height) a lobe's middle hangs; back: the edge's height behind.
    """
    def f(V):
        V = V.copy()
        th = np.degrees(np.arctan2(V[:, 0], -V[:, 1]))
        edge = np.full(len(V), 0.75)
        for m in lobes:
            edge = np.minimum(edge, low + 0.5 * ((th - m) / 38.0) ** 2)
        edge = np.where(np.abs(th) > 150, back, edge)
        edge = np.where((np.abs(th) > 120) & (np.abs(th) <= 150),
                        np.minimum(edge, back + (150 - np.abs(th)) / 30 * (low - back)), edge)
        V *= (1 - 0.16 * smoothstep(edge, edge - 0.14, V[:, 2]))[:, None]
        return V
    k.add("cap", outward(ell(c, r, deform=f, sub=5)), colour, bone, fuse=False, surface="leaf")


def dolliv(src, out_path):
    k = Kit(src, coat="leaf")
    # --- the head: a big round face, the olives at its sides, the cap of lobes and the bow on top
    k.add("head", ell(HC, HR, sub=5), FACE, "head")
    k.add("chin", ell(HC + (0, 0.004, -0.05), (0.06, 0.058, 0.04)), FACE, "head")
    for sx in (1, -1):
        sm.olive(k, "olive" + ("R" if sx > 0 else "L"), (OLIVE_X * sx, 0.018, OLIVE_Z), OLIVE_R,
                 unit(sx, 0.12, 0.18), OLIVE_Y, "head", surface="leaf")
    cap(k, HC + (0, 0.004, 0.006), HR * np.array([1.07, 1.08, 1.1]))
    top = HC + np.array([0, 0.006, 0.084])
    for sx in (1, -1):
        k.add("bow" + ("R" if sx > 0 else "L"),
              sm.fleshy_leaf(top + (0.004 * sx, 0, -0.008), (0.55 * sx, -0.1, 1.0), (0, -1.0, 0.25), 0.085, 0.034,
                             0.022, bend=0.04, cup=0.25), CAP, "head", fuse=False, surface="leaf")
    # --- the dress: a bell from the collar to the hem, leaves round it turned out white at the hem
    prof = [(0.0, 0.312), (0.03, 0.305), (0.033, 0.25), (0.043, 0.19), (0.066, 0.12), (0.098, 0.062),
            (HEM_R - 0.012, HEM_Z + 0.004), (0.0, HEM_Z + 0.004)]
    k.add("dress", lathe((0, 0, 0), (0, 0, 1), prof, seg=48), DRESS_IN, "body")
    for i in range(8):
        a = 2 * math.pi * i / 8
        rad = np.array([math.sin(a), -math.cos(a), 0.0])
        root = rad * 0.03 + np.array([0, 0, 0.255])
        tip = rad * HEM_R + np.array([0, 0, HEM_Z + 0.01])
        d = tip - root
        L = float(np.linalg.norm(d))
        k.add(f"skirt{i}", sm.fleshy_leaf(root, d, rad + (0, 0, 0.3), L, 0.046, 0.02, bend=0.14, cup=-0.15),
              DRESS, "body", fuse=False, surface="leaf")
        k.add(f"hem{i}", sm.fleshy_leaf(root + d * 0.86 + rad * 0.012, rad + (0, 0, -0.3), (0, 0, 1.0), 0.04, 0.03,
                                        0.016, bend=-0.1, cup=0.2), HEM, "body", fuse=False, surface="leaf")
    # --- the collar: pointed leaves round the neck, up and out to the chin
    for i in range(6):
        a = 2 * math.pi * (i + 0.5) / 6
        rad = np.array([math.sin(a), -math.cos(a), 0.0])
        k.add(f"collar{i}", sm.fleshy_leaf(rad * 0.024 + (0, 0, 0.286), rad + (0, 0, 0.75), rad + (0, 0, -0.5), 0.052,
                                           0.024, 0.016, bend=0.1, cup=0.2), DRESS, "body", fuse=False, surface="leaf")
    # --- the arms: leaves out of the shoulders, forward and down, the tip split in three
    for sx in (1, -1):
        side = "R" if sx > 0 else "L"
        sh = np.array([0.03 * sx, -0.004, 0.272])
        d = unit(0.62 * sx, -0.5, -0.6)
        L = 0.15
        k.add("arm" + side, sm.fleshy_leaf(sh, d, (0, -0.3, 1.0), L, 0.038, 0.026, bend=0.12, cup=0.2), DRESS,
              "arm" + side, fuse=False, surface="leaf")
        tip = sh + d * L * 0.9 + np.array([0, 0, 0.012])
        for j, turn in enumerate((-0.5, 0.0, 0.5)):
            dj = unit(*(d + np.array([0, 0, turn * 0.9]) + np.array([0, -turn * 0.2, 0])))
            k.add(f"finger{side}{j}", sm.fleshy_leaf(tip, dj, (0, -0.3, 1.0), 0.04, 0.014, 0.014, bend=0.05),
                  DRESS, "arm" + side, fuse=False, surface="leaf")
    # --- a stub of pale leg under the hem
    k.add("leg", tube([(0.014, 0.0, 0.14), (0.014, -0.004, 0.06), (0.012, -0.006, 0.016)], [0.016, 0.015, 0.012]),
          LEG, "leg{s}", mirror=True)
    # --- a tiny mouth (in every state, so before the build)
    mp, mn = sm.on_ell(HC, HR, (0, -1, -0.62))
    k.decal(mp, mn, (0.012, 0.0055), MOUTH, eps=0.002, rings=10, segs=40)
    k.build(voxel=0.005, smooth=4, band=0.005)

    ep, en = sm.on_ell(HC, HR, EYE_D)
    lid = srgb(FACE)
    def eyes(state):
        k.decal(ep, en, EYE_HALF, eye_look(state, rimmed_eye, lid, INK, line=0.16), eps=0.002, rings=20, segs=56,
                mirror=True)
    k.eye_states(eyes)
    k.export(out_path)


if __name__ == "__main__":
    dolliv(*args())
