# Pumpkaboo (the average size) sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pumpkaboo.py -- <the export's Pumpkaboo.glb> <out.glb>
#
# The design, from the references: a squat salmon-orange pumpkin of eight lobes with darker grooves, two round
# yellow lights shining from its front sides and two little nubs of feet under it. On it sits the spirit, dark brown:
# a small round head with two yellow slanting slit eyes and two small white fangs, two little pointed ears curling
# out at its top corners and a great stalk rising from its crown to curl over to its right in a spiral; round the
# head's foot a wide ragged collar of thick tongues sweeps out and down over the pumpkin, one pointing down over its
# front, the side ones curling up at their tips. Written to be grown from: Gourgeist keeps the pumpkin's lobes, the
# curl of the stalk and the way its eyes are made.
# The skeleton: body (0, 0, 0.122) in the pumpkin's upper part, head (0, 0, 0.19) at the collar.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, polygon, srgb, inside, unit, eye_look, args

# The line's colours
PUMP, PUMPRIB = "#E98A72", "#C26450"
LIGHTRIM, LIGHT, LIGHTCORE = "#D9921E", "#FFD23E", "#FFF1A6"
SPIRIT, SPIRITDARK = "#5C4632", "#45331F"
EYE, EYEINK, FANG = "#FFD23E", "#2A1C10", "#F6F2EA"

PUMPC = np.array([0.0, 0.0, 0.098]); PUMPR = np.array([0.114, 0.102, 0.078])
HEADC = np.array([0.0, 0.004, 0.232]); HEADR = np.array([0.063, 0.057, 0.058])
COLLAR = 0.196


def lobes(n=8, depth=0.05):
    """A deform that grooves a sphere into n lobes round its upright axis."""
    def f(V):
        th = np.arctan2(V[:, 1], V[:, 0])
        g = (1 - np.abs(np.sin(n / 2 * th))) ** 5          # 1 in a groove, 0 across a lobe
        V = V.copy()
        V[:, :2] *= (1 - depth * g)[:, None]
        return V
    return f


def pumpkin(k, centre, radii, colour, rib, bone, n=8, extra=None):
    """The pumpkin: lobed, with a darker line down each groove (centre and radii in the model's units)."""
    centre = np.array(centre, float); radii = np.array(radii, float)
    k.add("pumpkin", ell(centre, radii, deform=extra or lobes(n), sub=5), colour, bone)
    def ribs(P):
        q = (P - centre) / radii
        th = np.arctan2(q[:, 1], q[:, 0])
        g = np.abs(np.sin(n / 2 * th)) * np.hypot(P[:, 0] - centre[0], P[:, 1] - centre[1])
        return inside(g - 0.0055, 0.003) * (np.abs(q[:, 2]) < 0.9)
    k.paint(ribs, rib, on=["pumpkin"])


def curl(k, base, scale, colour, bone, turn=0.0, lean=0.0, name="curl"):
    """The stalk: up from base, over to the +X side and round into a spiral; scale 1 is Pumpkaboo's."""
    pts = np.array([(0, 0, 0), (-0.012, 0, 0.035), (-0.004, 0, 0.075), (0.025, 0, 0.097), (0.052, 0, 0.086),
                    (0.058, 0, 0.06), (0.042, 0, 0.044), (0.026, 0, 0.054), (0.03, 0, 0.068)]) * scale
    c, s = math.cos(math.radians(turn)), math.sin(math.radians(turn))
    pts = pts @ np.array([[c, s, 0], [-s, c, 0], [0, 0, 1]])
    pts[:, 1] += pts[:, 2] * lean
    rad = np.array([0.019, 0.017, 0.015, 0.013, 0.012, 0.011, 0.0095, 0.0085, 0.0075]) * scale
    k.add(name, tube(pts + np.array(base), np.maximum(rad, 0.0058)), colour, bone)


def light(u, v):
    r = np.hypot(u, v)
    c = np.tile(srgb(LIGHTRIM), (len(u), 1))
    c[r < 0.8] = srgb(LIGHT)
    c[np.hypot(u + 0.12, v - 0.12) < 0.42] = srgb(LIGHTCORE)
    return c


def collar(k):
    """Thick tongues out of the head's foot, flattened, the front one hanging over the pumpkin, the sides curling up."""
    # (azimuth from the front, +X positive; reach from the axis; how far the middle drops; how far the tip rises;
    # half width): broad, overlapping, a cape rather than tentacles
    for i, (az, reach, drop, rise, w) in enumerate(((0, 0.088, 0.088, -0.02, 0.05), (30, 0.11, 0.062, 0.0, 0.05),
                                                    (60, 0.138, 0.042, 0.008, 0.052), (90, 0.162, 0.032, 0.034, 0.052),
                                                    (120, 0.138, 0.032, 0.014, 0.05), (150, 0.112, 0.04, 0.0, 0.048),
                                                    (180, 0.098, 0.045, 0.0, 0.048))):
        for side in ((1,) if az in (0, 180) else (1, -1)):
            a = math.radians(az) * side
            d = np.array([math.sin(a), -math.cos(a), 0.0])
            pts = [d * 0.03 + (0, 0, COLLAR + 0.012), d * reach * 0.45 + (0, 0, COLLAR - drop * 0.55),
                   d * reach * 0.8 + (0, 0, COLLAR - drop), d * reach + (0, 0, COLLAR - drop + rise)]
            k.add(f"tongue{i}{side}", blade(pts, [w, w * 0.95, w * 0.66, w * 0.26], (0, 0, 1), 0.42), SPIRIT, "head")


def spirit(k):
    k.add("head", ell(HEADC, HEADR), SPIRIT, "head")
    collar(k)
    for side in (1, -1):
        e = [np.array(p) * (side, 1, 1) for p in ((0.036, 0.004, 0.258), (0.052, 0.004, 0.282), (0.066, 0.0, 0.296),
                                                  (0.08, -0.004, 0.292))]
        k.add(f"ear{side}", tube(e, [0.016, 0.012, 0.008, 0.0058]), SPIRIT, "head")
    curl(k, (0.0, 0.008, 0.268), 1.0, SPIRIT, "head")


def body(k):
    pumpkin(k, PUMPC, PUMPR, PUMP, PUMPRIB, "body")
    k.add("feet", ell((0.04, -0.008, 0.024), (0.022, 0.026, 0.021)), PUMP, "body", mirror=True)
    for side in (1, -1):
        a = math.radians(38) * side
        n = unit(math.sin(a), -math.cos(a), 0.05)
        c = PUMPC + np.array([math.sin(a) * PUMPR[0], -math.cos(a) * PUMPR[1], 0.002])
        look = light if side == 1 else (lambda u, v: light(-u, v))
        k.decal(c, n, (0.025, 0.025), look, eps=0.003, rings=14, segs=56)


def mouth(k):
    k.decal((0, -0.057, 0.222), (0, -1, -0.1), (0.012, 0.0026), EYEINK, eps=0.003, rings=6, segs=40)
    fang = polygon([(-1, 0.6), (1, 0.6), (0, -1.0)])
    for side in (1, -1):
        k.decal((0.0055 * side, -0.057, 0.2175), (0, -1, -0.1), (0.0042, 0.0058), FANG, eps=0.0036, outline=fang,
                rings=6, segs=48)


def slit(u, v):
    r = np.hypot(u, v)
    c = np.tile(srgb(EYEINK), (len(u), 1))
    c[r < 0.82] = srgb(EYE)
    return c


def eyes(k, state, at=(0.021, -0.054, 0.246), half=(0.0085, 0.0135), lid=SPIRIT, tilt=12):
    x, y, z = at
    t = math.radians(tilt)
    up = (-math.sin(t), 0, math.cos(t))                      # the eye's top leans in toward the middle
    k.decal((x, y, z), unit(0.35, -1, 0.05), half, eye_look(state, slit, srgb(lid), srgb(EYEINK), line=0.17),
            up=up, eps=0.0045, rings=20, segs=56, mirror=True)


def pumpkaboo(src, out_path):
    k = Kit(src, coat="leaf")
    body(k)
    spirit(k)
    mouth(k)
    k.build(voxel=0.0034, smooth=3, band=0.004, wband=0.025)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out_path)


if __name__ == "__main__":
    pumpkaboo(*args())
