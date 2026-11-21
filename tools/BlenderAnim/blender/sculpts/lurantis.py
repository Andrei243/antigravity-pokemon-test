# Lurantis sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/lurantis.py -- <the export's Lurantis.glb> <out.glb>
# Grown from fomantis.py (sibling("fomantis")): its colours' way, its red eye laid the same way, its loose puffy lobes.
#
# From the references: an orchid mantis standing upright. A small pink head with a broad white flap drooping out at
# each side at eye level, two pink petals rising from its crown out and back, red at their tips, two short teal leaves
# up from the brow and a teal mark over each eye; big red eyes with a dark pupil, set on the head's sides; a white
# collar. A slim pale torso. The largest masses are the two great scythe petals of the arms: fat red petals with a white
# rim along the outer edge, from the shoulders hanging forward and down in front of the body to green claw points.
# Behind them two pale petals splay out and down from the back like a cape, white with a red stripe, pointed. Two thick
# legs like striped trousers, red and pale lengthwise, full from the hip, ending in green claw toes.
# (The task's "red striped trousers-like legs" hold; the pictures add the back petals, the head's white flaps and the
# teal leaves on the brow.)
# Skeleton: body (0, 0, 0.32), head (0, -0.01, 0.44), arms (+-0.05, -0.02, 0.42), legs (+-0.03, 0, 0.18).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, args, outward, sibling

f = sibling("fomantis")

PINK, PALE, WHITE_ = "#F2B4C2", "#F8DDE3", "#F8F2F3"
RED, RED_DEEP = "#E65A6C", "#C9475A"
STRIPE_PALE = "#F7D2DA"
TEAL = f.TEAL
CLAW = "#52CDA2"
EYE_RED, EYE_DARK = srgb("#E44C60"), srgb("#4A0E1C")

# --- Lurantis's frame (about 0.65 tall) -----------------------------------------------------------------------------
HS = 1.25                                  # the head grown about its joint, to read in the game
HJ = np.array([0.0, -0.01, 0.44])
HC = HJ + (np.array([0.0, -0.018, 0.486]) - HJ) * HS       # the head's middle
HR = np.array([0.064, 0.054, 0.052]) * HS
EYE_D = (0.66, -0.7, 0.02)
EYE_HALF = (0.027 * HS, 0.027 * HS)
VOXEL = 0.0055


def frame(a):
    """A right-handed frame (e1, e2, a) about the direction a, e1 near the model's +X."""
    a = np.array(a, float); a /= np.linalg.norm(a)
    e1 = np.array([1.0, 0, 0]) - a * a[0]
    if np.linalg.norm(e1) < 1e-3:
        e1 = np.array([0, 1.0, 0]) - a * a[1]
    e1 /= np.linalg.norm(e1)
    return e1, np.cross(a, e1), a


def petal(c, r, a, roll=0.0, taper=0.35, point=0.0):
    """An ellipsoid laid along a (its length r[2]), e1 rolled by roll degrees about a; narrower toward its +a end by
    taper, drawn to a point at its -a end by point. A loose piece wound outward."""
    def shape(V):
        V = V.copy()
        t = np.clip(V[:, 2], 0, 1)
        b = np.clip(-V[:, 2], 0, 1)
        s = (1 - taper * t ** 2) * (1 - point * b ** 1.8)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    V, F = ell((0, 0, 0), r, deform=shape, sub=3)
    e1, e2, ax = frame(a)
    t = math.radians(roll)
    e1, e2 = e1 * math.cos(t) + e2 * math.sin(t), -e1 * math.sin(t) + e2 * math.cos(t)
    M = np.stack([e1, e2, ax], axis=1)
    return outward((V @ M.T + np.array(c, float), F))


def claw(base, d, length, w):
    """A green claw point: a flat-faced hull from base along d."""
    d = unit(*d)
    e1, e2, _ = frame(d)
    b = np.array(base, float)
    pts = [b + e1 * w + e2 * w * 0.6, b - e1 * w + e2 * w * 0.6, b + e1 * w - e2 * w * 0.6,
           b - e1 * w - e2 * w * 0.6, b + d * length * 0.55 + e1 * w * 0.5, b + d * length * 0.55 - e1 * w * 0.5,
           b + d * length + e2 * w * 0.1]
    return hull(pts)


def lurantis(src, out_path):
    k = Kit(src, coat="leaf")

    # ---- the torso: slim and pale, a white collar, a pelvis the legs grow from
    k.add("chest", ell((0, -0.012, 0.37), (0.04, 0.034, 0.064)), PALE, "body")
    k.add("waist", ell((0, -0.004, 0.31), (0.034, 0.03, 0.04)), PALE, "body")
    k.add("pelvis", ell((0, 0.0, 0.27), (0.062, 0.046, 0.04)), RED, "body")
    k.add("collar", ell((0, -0.016, 0.428), (0.046, 0.04, 0.022)), WHITE_, "head")
    k.add("neck", tube([(0, -0.012, 0.42), (0, -0.016, 0.46)], [0.022, 0.022]), PALE, "head")
    k.add("waistleaf", outward(ell((0, -0.04, 0.3), (0.016, 0.01, 0.02), sub=3)), TEAL, "body", fuse=False,
          surface="leaf")

    # ---- the legs: thick tubes striped lengthwise, full from the hip
    LEG_TOP, LEG_BOT = np.array([0.036, 0.004, 0.27]), np.array([0.042, -0.008, 0.026])
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        top, bot = LEG_TOP * (s, 1, 1), LEG_BOT * (s, 1, 1)
        mid = (top + bot) / 2 + np.array([0.004 * s, -0.004, 0])
        k.add("leg" + side, tube([top, mid, bot], [0.046, 0.041, 0.03]), RED, "leg" + side)
        ax = unit(*(bot - top))
        def stripes(P, top=top, ax=ax, s=s):
            q = P - top
            along = q @ ax
            rad = q - np.outer(along, ax)
            th = np.arctan2(rad[:, 1], rad[:, 0] * s)
            near = inside(np.linalg.norm(rad, axis=1) - 0.06, 0.004) * inside(-along - 0.0, 0.004) \
                * inside(along - 0.26, 0.004)
            return inside(np.sin(5 * th) - 0.1, 0.05) * near
        k.paint(stripes, STRIPE_PALE, on=["leg" + side])
        for j, dx in enumerate((-0.016, 0.016)):
            k.add(f"toe{j}{side}", claw(bot + np.array([dx, -0.018, -0.02]), (dx * 8, -1.0, -0.35), 0.036, 0.011),
                  CLAW, "leg" + side, fuse=False, surface="leaf")

    # ---- the arms: great red petals with a white rim along their outer edge, forward and down to green claws
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        root = np.array([0.064 * s, -0.046, 0.405])
        tip = np.array([0.058 * s, -0.112, 0.105])
        a = unit(*(root - tip))
        c = (root + tip) / 2
        L = np.linalg.norm(root - tip) / 2
        k.add("arm" + side, petal(c, (0.05, 0.032, L), a, roll=-12 * s, taper=0.42, point=0.25), RED, "arm" + side,
              fuse=False, surface="leaf")
        k.add("armrim" + side, petal(c + np.array([0.012 * s, 0.006, 0.006]), (0.05, 0.026, L * 1.01), a,
                                     roll=-12 * s, taper=0.42, point=0.25), WHITE_, "arm" + side, fuse=False,
              surface="leaf")
        k.add("shoulder" + side, outward(ell(root + np.array([-0.012 * s, 0.01, 0.008]), (0.03, 0.026, 0.026), sub=3)),
              WHITE_, "arm" + side, fuse=False, surface="leaf")
        k.add("armclaw" + side, claw(tip + a * 0.006, (0.15 * s, -0.45, -1.0), 0.05, 0.014), CLAW, "arm" + side,
              fuse=False, surface="leaf")

    # ---- the back petals: red with a white rim and point, splayed out and down behind like a cape
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        root = np.array([0.04 * s, 0.03, 0.4])
        tip = np.array([0.15 * s, 0.1, 0.14])
        a = unit(*(root - tip))
        c = (root + tip) / 2
        L = np.linalg.norm(root - tip) / 2
        k.add("cape" + side, petal(c, (0.05, 0.018, L * 1.02), a, roll=40 * s, taper=0.3, point=0.85), WHITE_,
              "body", fuse=False, surface="leaf")
        k.add("capered" + side, petal(c + a * 0.03 - np.array([0.006 * s, 0, 0]), (0.036, 0.025, L * 0.86), a,
                                      roll=40 * s, taper=0.3, point=0.7), RED, "body", fuse=False, surface="leaf")

    # ---- the head (grown by HS about its joint): pink, white flaps drooping at its sides, the crown petals, the
    # brow's teal leaves and marks
    k.add("head", ell(HC, HR), PINK, "head")
    def at(o):
        return HC + np.array(o, float) * HS
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        k.add("flap" + side, petal(at((0.082 * s, 0.006, -0.016)), np.array((0.028, 0.016, 0.048)) * HS,
                                   (-s, -0.1, 0.42), roll=70 * s, taper=0.1, point=0.5), WHITE_, "head", fuse=False,
              surface="leaf")
        k.add("flaptip" + side, outward(ell(at((0.116 * s, 0.004, -0.03)), np.array((0.016, 0.014, 0.012)) * HS,
                                            sub=3)), PINK, "head", fuse=False, surface="leaf")
        # the crown petals: leaves pointed at the top, out and back, their upper third red
        base = at((0.03 * s, 0.014, 0.03))
        up = unit(0.6 * s, 0.35, 1.0)
        k.add("crest" + side, petal(base + up * 0.064, (0.034, 0.013, 0.07), -up, roll=0, taper=0.35, point=0.55),
              PINK, "head", fuse=False, surface="leaf")
        k.add("cresttip" + side, petal(base + up * 0.11, (0.03, 0.016, 0.03), -up, roll=0, taper=0.1, point=0.7),
              RED, "head", fuse=False, surface="leaf")
        lb = at((0.022 * s, -0.04, 0.03))
        lu = unit(0.14 * s, 0.1, 1.0)
        k.add("feeler" + side, petal(lb + lu * 0.036, (0.013, 0.01, 0.04), -lu, taper=0.2, point=0.6), TEAL,
              "head", fuse=False, surface="leaf")
        k.add("browmark" + side, outward(ell(at((0.03 * s, -0.05, 0.036)), np.array((0.009, 0.006, 0.013)) * HS,
                                             rot=(0, 25 * s, 0), sub=3)), TEAL, "head", fuse=False, surface="leaf")

    k.build(voxel=VOXEL, smooth=4, band=0.005, wband=0.04)

    look = f.red_eye(fill=EYE_RED, dark=EYE_DARK)
    def eyes(state):
        f.lay_eyes(k, HC, HR, EYE_D, EYE_HALF, look, PINK, state, eps=0.003)
    k.eye_states(eyes)
    k.export(out_path)


if __name__ == "__main__":
    lurantis(*args())
