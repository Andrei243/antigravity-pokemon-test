# Mienfoo sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/mienfoo.py -- <the export's Mienfoo.glb> <out.glb>
# From the references: a small pale-yellow weasel of the martial arts in a fighting stance: a round head with small
# round ears, two grey ovals on the brow set in a V, fierce slanted eyes with dark red irises, a small pointed muzzle
# with a pink nose and a pointed tuft at each cheek; a slim cream neck; great puffs of pink-red fur on the forearms
# (each nearly as big as the head), the upper arm a narrow red band, small cream paws with three fingers; a cream
# body flaring like a bell to its hem; thick red legs and feet; a red tail with a cream band, forked at its tip.
# Written to be grown from (mienshao.py): the frame in named constants, the paw, nose, eye and tail as functions.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, ring_band, outward, polygon, srgb, inside, smoothstep, unit, boxy,
                       eye_look, args)

CREAM, RED, GREY, NOSE, INK = "#F2E7A4", "#D9566E", "#8E8C90", "#E57A90", "#22161C"
IRIS, WHITE = "#9A1E36", "#FBF6EE"

HC, HR = np.array([0.0, -0.012, 0.36]), np.array([0.079, 0.072, 0.074])     # the head, a tenth over the measure


def frame_ell(c, r, ex, ey, ez):
    V, F = ell((0, 0, 0), r)
    M = np.stack([unit(*ex), unit(*ey), unit(*ez)], axis=1)
    return outward((V @ M.T + np.array(c, float), F))


def on_head(x, z, c=HC, r=HR):
    """The head's front surface at (x, z): the point to lay a patch on."""
    q = 1 - ((x - c[0]) / r[0]) ** 2 - ((z - c[2]) / r[2]) ** 2
    return np.array([x, c[1] - r[1] * math.sqrt(max(q, 0.0)), z])


def paw(k, name, bone, wrist, fdir, colour, size=1.0):
    """A small paw: a round palm and three short fingers along fdir, loose so it keeps its edge on the fur."""
    f = unit(*fdir)
    w = unit(*np.cross(f, (0, 0, 1)))
    c = np.array(wrist, float) + f * 0.012 * size
    k.add(f"{name}palm", frame_ell(c, (0.027 * size, 0.024 * size, 0.022 * size), w, f, (0, 0, 1)), colour, bone,
          fuse=False, surface="fur")
    for i, o in enumerate((-1, 0, 1)):
        b = c + f * 0.016 * size + w * o * 0.014 * size
        k.add(f"{name}f{i}", outward(tube([b, b + f * 0.02 * size - np.array([0, 0, 0.004 * size])],
                                          [0.0105 * size, 0.0095 * size])), colour, bone, fuse=False, surface="fur")


def nose(k, tip, size=1.0):
    """The pink nose: a small rounded triangle on the muzzle's tip."""
    k.decal(tip, unit(0, -1, 0.1), (0.011 * size, 0.009 * size), NOSE, eps=0.003,
            outline=polygon([(-1, 0.6), (-0.6, 1), (0.6, 1), (1, 0.6), (0, -1)]), rings=8, segs=32)


def forked_tip(k, end, d, colour, bone="tail", size=1.0):
    """Two short solid points splaying from a tail's end."""
    d = unit(*d)
    side = unit(*np.cross(d, (0, 0, 1)))
    for i, a in enumerate((-0.5, 0.5)):
        g = unit(*(d + side * a + np.array([0, 0, 0.25 * a])))
        k.add(f"fork{i}", blade([end - d * 0.01 * size, end + g * 0.03 * size, end + g * 0.052 * size],
                                [0.017 * size, 0.015 * size, 0.004 * size], (0, 0, 1), 0.75), colour, bone)


def mienfoo(src, out):
    k = Kit(src, coat="fur")

    # The head: round, a little broad at the cheeks; a small pointed muzzle; round ears
    def cheeks(V):
        V = V.copy(); V[:, 0] *= 1 + 0.08 * np.clip(-V[:, 2], 0, 1); return V
    k.add("head", ell(HC, HR, deform=cheeks), CREAM, "head")
    k.add("muzzle", ell((0, -0.07, 0.338), (0.03, 0.034, 0.025), rot=(10, 0, 0)), CREAM, "head")
    k.add("ear", ell((0.054, 0.004, 0.425), (0.03, 0.02, 0.029), rot=(0, -25, 0)), CREAM, "ear{s}", mirror=True)
    # a pointed tuft at each cheek, out and down
    k.add("tuft", blade([(0.07, -0.01, 0.33), (0.098, -0.012, 0.316), (0.118, -0.012, 0.302)],
                        [0.018, 0.012, 0.003], (0, 1, 0), 0.6), CREAM, "head", mirror=True)

    # A slim neck, a small chest, the body flaring like a bell to its hem
    k.add("neck", tube([(0, 0.0, 0.205), (0, -0.006, 0.25), (0, -0.01, 0.3)], [0.034, 0.03, 0.032]), CREAM, "body")
    def bell(V):
        V = V.copy()
        t = np.clip((V[:, 2] + 1) / 2, 0, 1)
        g = 0.42 + 0.58 * (1 - t) ** 0.7
        V[:, 0] *= g; V[:, 1] *= g
        V[:, 2] = np.maximum(V[:, 2], -0.62)
        return V
    k.add("bell", ell((0, 0.006, 0.16), (0.104, 0.088, 0.092), deform=bell), CREAM, "body")
    HEM = 0.16 - 0.092 * 0.62

    # Thick red legs under the hem, a little apart and bent; red feet
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"leg{side}", tube(np.array([(0.045, 0.0, 0.11), (0.06, -0.012, 0.068), (0.064, 0.0, 0.03)]) * m,
                                 [0.036, 0.032, 0.027]), RED, f"leg{side}")
        k.add(f"foot{side}", frame_ell((0.066 * s, -0.026, 0.016), (0.03, 0.043, 0.018), (1, 0, 0), (0, 1, 0),
                                       (0, 0, 1)), RED, f"leg{side}", fuse=False)

    # the hem's edge crisp: red under it on the legs, cream over it
    k.paint(lambda P: inside(P[:, 2] - (HEM - 0.004), 0.002), RED, on=["leg"])
    k.paint(lambda P: inside((HEM - 0.002) - P[:, 2], 0.002) * (P[:, 2] < 0.22), CREAM, on=["bell", "leg"])

    # The arms: a narrow red band of an upper arm, a great puff of red fur on the forearm, a cream paw out of it
    SH, EL, WR = np.array([0.058, -0.01, 0.25]), np.array([0.1, -0.045, 0.225]), np.array([0.112, -0.135, 0.205])
    k.add("upper", tube([SH, EL], [0.026, 0.026]), RED, "arm{s}", mirror=True)
    d = unit(*(WR - EL))
    k.add("puff", frame_ell(EL + d * 0.05, (0.06, 0.068, 0.058), unit(*np.cross(d, (0, 0, 1))), d, (0, 0, 1)), RED,
          "arm{s}", mirror=True)
    for s, side in ((1, "R"), (-1, "L")):
        paw(k, f"paw{side}", f"arm{side}", WR * np.array([s, 1, 1]), d * np.array([s, 1, 1]), CREAM)

    # The tail: red, back and out to the +X side, rising, a cream band near its root, forked at the tip
    TP = [(0.0, 0.05, 0.15), (0.03, 0.105, 0.135), (0.075, 0.15, 0.15), (0.105, 0.18, 0.185)]
    k.add("tail", tube(TP, [0.024, 0.021, 0.019, 0.017]), RED, "tail")
    forked_tip(k, np.array(TP[-1]), np.array(TP[-1]) - np.array(TP[-2]), RED)
    BC = (np.array(TP[1]) + np.array(TP[2])) / 2
    k.add("tailband", ring_band(BC, unit(*(np.array(TP[2]) - np.array(TP[1]))), 0.0182, 0.026, thick=0.006), CREAM,
          "tail", fuse=False)

    # The grey ovals on the brow, in a V; the pink nose (in every state)
    for s in (1, -1):
        k.decal(on_head(0.028 * s, 0.408), unit(0.3 * s, -0.8, 0.55), (0.0125, 0.021), GREY, up=(-0.45 * s, 0, 1),
                eps=0.003, rings=10, segs=40)
    nose(k, (0, -0.103, 0.343))

    # The eyes: fierce, slanted down to the muzzle, white with a dark red iris and a pupil
    CC, CW, CI, CK = srgb(CREAM), srgb(WHITE), srgb(IRIS), srgb(INK)
    def top(u):
        return 0.05 + 0.55 * (u + 1) / 2 * 1.4                      # low by the nose (u -1), high outside
    def open_eye(u, v):
        c = np.tile(CW, (len(u), 1))
        c[((u + 0.15) / 0.55) ** 2 + ((v + 0.15) / 0.62) ** 2 < 1] = CI
        c[((u + 0.18) / 0.26) ** 2 + ((v + 0.2) / 0.32) ** 2 < 1] = CK
        c[((u - 0.05) / 0.14) ** 2 + ((v - 0.0) / 0.14) ** 2 < 1] = (1, 1, 1)
        c[np.abs(v - top(u)) < 0.16] = CK                           # the angry lid line along the cut
        return c
    EYE_OUT = [(math.cos(a), min(math.sin(a), top(math.cos(a)) + 0.12))
               for a in np.linspace(0, 2 * math.pi, 48, endpoint=False)]
    E = on_head(0.036, 0.36)
    def lay(state):
        look = eye_look(state, open_eye, CC, CK, line=0.18)
        for s in (1, -1):
            shape = polygon([(u * s, v) for u, v in EYE_OUT]) if state in ("Open", "Fierce") else None
            col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
            k.decal(E * np.array([s, 1, 1]), unit(0.45 * s, -0.88, 0.05), (0.02, 0.017), col, up=(0, 0.1, 1),
                    eps=0.004, outline=shape, rings=20, segs=56)

    k.build(voxel=0.0042, smooth=4, band=0.006)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    mienfoo(*args())
