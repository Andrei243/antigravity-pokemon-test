# Rattata-Alola sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/rattataalola.py -- <the export's Rattata-Alola.glb> <out.glb>
# Rattata's Alolan form, grown from rattata.py (its frame: the head, muzzle, jaw, ears, legs, teeth, cut eyes and
# tail are placed as Rattata's): a charcoal rat crouched low with a tan jaw and lower face, a tan belly patch with a
# ragged edge, tan paws with claws, big round ears lined blue-grey with a spike on top and a smaller one on the outer
# rim, a thick dark moustache curling down from either side of the nose, red eyes under the scowling brow, the two
# big front teeth over the open pink mouth, and the tail rising to a curl with a spiky tuft on top.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, tri, args, sibling

r = sibling("rattata")

FUR, CREAM, LINING, NOSE = "#4C4F4A", "#DCC9A0", "#8D94AA", "#2E302C"
CLAW = "#F0E6D0"
IRIS = "#D83228"
H, HR, EC, ER, EA = r.H, r.HR, r.EC, r.ER, r.EA


def spike(base, d, length, w, th):
    """A short solid spike from base along d: a flat-faced hull, w wide and th thick at its root."""
    base = np.array(base, float); d = unit(*d)
    a = np.cross(d, (0, 1.0, 0))
    if np.linalg.norm(a) < 1e-3:
        a = np.cross(d, (1.0, 0, 0))
    a /= np.linalg.norm(a); b = np.cross(d, a)
    pts = [base + a * w / 2, base - a * w / 2, base + b * th / 2, base - b * th / 2, base - d * th * 0.5,
           base + d * length * 0.55 + a * w * 0.2, base + d * length * 0.55 - a * w * 0.2, base + d * length]
    return hull(pts)


def rattata_alola(src, out):
    k = Kit(src, coat="fur")

    # The long low body, the head carried forward, the muzzle, the dropped jaw and the cheeks: Rattata's
    k.add("body", ell((0, 0.06, 0.185), (0.115, 0.185, 0.1), rot=(-6, 0, 0)), FUR, "body")
    k.add("chest", ell((0, -0.07, 0.17), (0.097, 0.09, 0.087)), FUR, "body")
    k.add("head", ell(H, HR * 1.04), FUR, "head")
    def snout(V):
        V = V.copy(); t = np.clip(-V[:, 1], 0, 1)
        V[:, 0] *= 1 - 0.25 * t; V[:, 2] *= 1 - 0.2 * t
        return V
    k.add("snout", ell((0, -0.29, 0.222), (0.062, 0.085, 0.056), rot=(-6, 0, 0), deform=snout), FUR, "head")
    k.add("jaw", ell((0, -0.268, 0.163), (0.055, 0.062, 0.03), rot=(14, 0, 0)), CREAM, "head")
    k.add("cheek", ell((0.065, -0.22, 0.19), (0.054, 0.052, 0.049)), FUR, "head", mirror=True)

    # Big round ears, blue-grey inside, a spike standing on each one's top and a smaller one on its outer rim
    k.add("ear", ell(EC, ER, rot=EA), FUR, "ear{s}", mirror=True)
    k.add("earroot", ell((0.07, -0.135, 0.31), (0.035, 0.025, 0.04)), FUR, "ear{s}", mirror=True)
    r.ear_lining(k, EC, EA, (0.05, 0.055), -0.008, LINING)
    M = r.rot(EA)
    top = EC + M @ np.array([0.004, 0, 0.07])
    k.add("earspike", spike(top, M @ unit(0.18, 0, 1), 0.06, 0.034, 0.024), FUR, "ear{s}", fuse=False, mirror=True)
    rim = EC + M @ np.array([0.064, 0, 0.022])
    k.add("earspur", spike(rim, M @ unit(1, 0, 0.45), 0.036, 0.026, 0.02), FUR, "ear{s}", fuse=False, mirror=True)

    # Legs as Rattata's, tan paws with three toes and a claw at each
    def toes(V):
        ph = np.arctan2(V[:, 0], -V[:, 1])
        s = 1 + 0.18 * np.cos(3 * ph) * np.clip(-V[:, 1], 0, 1)
        V = V.copy(); V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("leg", tube([(0.07, -0.095, 0.17), (0.085, -0.11, 0.08), (0.088, -0.125, 0.03)], [0.037, 0.031, 0.027]), FUR,
          "leg{s}", mirror=True)
    k.add("paw", ell((0.09, -0.15, 0.018), (0.033, 0.045, 0.019), deform=toes), CREAM, "leg{s}", mirror=True)
    k.add("thigh", ell((0.08, 0.15, 0.15), (0.062, 0.082, 0.077)), FUR, "hind{s}", mirror=True)
    k.add("hind", tube([(0.088, 0.17, 0.12), (0.092, 0.19, 0.06), (0.092, 0.17, 0.03)], [0.035, 0.029, 0.027]), FUR,
          "hind{s}", mirror=True)
    k.add("hpaw", ell((0.094, 0.135, 0.018), (0.034, 0.056, 0.019), deform=toes), CREAM, "hind{s}", mirror=True)
    for i, ang in enumerate((-28, 0, 28)):
        d = np.array([math.sin(math.radians(ang)), -math.cos(math.radians(ang)), -0.35])
        k.add(f"claw{i}", spike((0.09 + 0.022 * math.sin(math.radians(ang)), -0.188, 0.016), d, 0.022, 0.012, 0.009),
              CLAW, "leg{s}", fuse=False, mirror=True, surface="shell")
        k.add(f"hclaw{i}", spike((0.094 + 0.022 * math.sin(math.radians(ang)), 0.083, 0.016), d, 0.02, 0.012, 0.009),
              CLAW, "hind{s}", fuse=False, mirror=True, surface="shell")
    k.paint(lambda P: inside(P[:, 2] - 0.042, 0.003) * (P[:, 2] < 0.1), CREAM, on=["leg", "hind", "paw", "hpaw"])
    k.paint(lambda P: inside(0.042 - P[:, 2], 0.003) * (P[:, 2] < 0.1), FUR, on=["leg", "hind"])

    # A tan patch under the belly between the legs, its top edge ragged as a tuft of fur
    def belly(P):
        edge = 0.132 + 0.02 * tri(P[:, 1] / 0.045)
        return (inside(P[:, 2] - edge, 0.003) * inside(np.abs(P[:, 0]) - 0.1, 0.004)
                * inside(P[:, 1] - 0.16, 0.004) * inside(-0.04 - P[:, 1], 0.004))
    k.paint(belly, CREAM, on=["body", "chest"])
    # The tan lower face from the cheeks to the chin
    def face(P):
        line = 0.205 - 0.25 * (P[:, 1] + 0.2) - 0.25 * np.clip(np.abs(P[:, 0]) - 0.04, 0, 1)
        return inside(P[:, 2] - line, 0.0025) * (P[:, 1] < -0.15) * (P[:, 2] > 0.11)
    k.paint(face, CREAM)

    # The tail: Rattata's, up from the rump and round into a curl, with a tuft of spikes on the curl's top
    rise = [(0, 0.21, 0.2), (0.015, 0.29, 0.25), (0.035, 0.35, 0.33), (0.05, 0.365, 0.4)]
    C = np.array([0.055, 0.3, 0.42])
    curl = []
    for th in np.linspace(0.25, 2 * math.pi, 14):
        rr = 0.065 - 0.04 * th / (2 * math.pi)
        curl.append((C[0], C[1] + rr * math.cos(th), C[2] + rr * math.sin(th)))
    pts = rise + curl
    k.add("tail", tube(pts, np.linspace(0.021, 0.014, len(pts)), seg=14), FUR, "tail")
    for th, ln in ((1.35, 0.05), (1.85, 0.042), (0.85, 0.034)):
        rr = 0.065 - 0.04 * th / (2 * math.pi)
        p = C + np.array([0, rr * math.cos(th), rr * math.sin(th)])
        d = unit(0, math.cos(th), math.sin(th))
        k.add(f"tuft{th}", spike(p + d * 0.008, d, ln, 0.03, 0.022), FUR, "tail", fuse=False)

    # The nose's tip, the teeth, the moustache curling down from either side of the nose
    k.add("nose", ell((0, -0.373, 0.232), (0.021, 0.015, 0.017)), NOSE, "head", fuse=False, surface="skin")
    r.teeth(k, (0, -0.352, 0.192), 0.026, 0.05)
    tache = [(0.014, -0.366, 0.218), (0.05, -0.37, 0.212), (0.085, -0.366, 0.196), (0.104, -0.358, 0.168),
             (0.098, -0.35, 0.145), (0.084, -0.348, 0.142)]
    k.add("tache", tube(tache, [0.012, 0.012, 0.011, 0.0095, 0.008, 0.0075], seg=12), FUR, "head", fuse=False,
          mirror=True)

    # The open mouth under the teeth
    CM, CT = srgb(r.MOUTH), srgb(r.TONGUE)
    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        c[(u / 0.7) ** 2 + ((v + 0.7) / 0.5) ** 2 < 1] = CT
        return c
    k.decal((0, -0.33, 0.178), unit(0, -1, -0.25), (0.036, 0.024), mouth, eps=0.003, rings=12, segs=48)

    # Red eyes under the dark brow slanting down toward the nose, a little larger than Rattata's
    CW, CF = srgb(r.WHITE), srgb(FUR)
    look = r.red_eye(CW, srgb(IRIS), srgb(r.PUPIL))
    de = unit(0.6, -0.76, 0.22)
    E = H + de / np.linalg.norm(de / (HR * 1.04))
    top = lambda u: 0.28 + 0.42 * u
    ftop = lambda u: 0.02 + 0.5 * u
    def eyes(state):
        r.lay_eyes(k, state, E, de, (0.038, 0.038), look, top, ftop, CF, srgb("#E8E0CC"))

    k.build(voxel=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    rattata_alola(*args())
