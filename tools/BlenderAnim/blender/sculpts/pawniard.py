# Pawniard sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pawniard.py -- <the export's Pawniard.glb> <out.glb>
# The base of its line: bisharp.py imports this frame with sibling("pawniard") (its colours, the helmet's paint, the
# eyes peering from under the helmet's rim, the knife blades and the bands of blade round the body).
#
# From the references: a small soldier with a head as big as its body. The head is a round helmet: grey, with a red
# band over the crown from the brow to the nape and the grey sides hanging low past the cheeks; under the red rim a
# dark visor where two yellow-orange eyes with black pupils peer out, half shut and stern. One white crescent blade
# stands on the helmet's right side (the viewer's right), curving up past the crown. A grey segmented body, two white
# blades wrapped round it (one at the chest, one at the belly), their points flaring out. Thin red arms whose hands
# ARE long white blades. Thin red bowed legs, round red feet with two white claws in front.
# Skeleton: body (0, 0, 0.13), head (0, -0.01, 0.18), arms (+-0.05, -0.01, 0.17), legs (+-0.03, 0, 0.08).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, sheet, slab, hull, outward, srgb, inside, unit, eye_look, polygon, args)

RED, GREY, DARK, SEAM = "#C8343C", "#9A9AA2", "#3E3E46", "#5E5E68"
BODY, BLADE, AMBER, INK = "#7E7E88", "#EEEFF3", "#F2A41C", "#141418"


# --- shapes the line shares ---------------------------------------------------------------------------------------
def knife(origin, d, face, length, width, thick, hook=0.15):
    """A flat blade from origin along d, its broad face turned to face (a hard-edged slab, one solid piece)."""
    d = unit(*d)
    n = np.array(face, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    w = np.cross(n, d)                       # the blade's width lies across d in its own plane
    L, W = length, width
    outline = [(0.0, -0.5 * W), (0.35 * L, -0.55 * W), (0.72 * L, -0.32 * W), (L, hook * W),
               (0.7 * L, 0.36 * W), (0.3 * L, 0.5 * W), (0.0, 0.48 * W)]
    return slab(outline, origin, d, w, thick)


def band(c, rx, ry, z, width, thick, span=300, start=-150, flare=0.3, rise=0.0, nu=64):
    """A curved blade wrapped round a body: from angle start (0 the front, +X at 90) through span degrees, broad at its
    start and tapering to a point that flares out from the body."""
    c = np.array(c, float)
    def at(u, v):
        th = np.radians(start + span * u)
        r = 1 + flare * u ** 2
        w = width * np.clip(1 - u, 0.06, 1) ** 0.6
        x = c[0] + rx * r * np.sin(th)
        y = c[1] - ry * r * np.cos(th)
        zz = z + rise * u + (v - 0.5) * w
        return np.stack([x, y, zz], axis=-1)
    return outward(sheet(at, nu, 4, thick))


def eye_shape(top_out=0.55, top_in=-0.05):
    """A half-shut eye, u outward: its top cut flat by the helmet's rim, sloping down to the nose (stern)."""
    return [(-1.0, top_in), (-0.85, -0.45), (-0.4, -0.8), (0.25, -0.82), (0.8, -0.5), (1.0, -0.05), (1.0, top_out)]


def eye_open(iris=AMBER, pupil=INK):
    CI, CK = srgb(iris), srgb(pupil)
    def eye(u, v):
        c = np.tile(CI, (len(u), 1))
        c[((u + 0.2) / 0.42) ** 2 + ((v + 0.25) / 0.58) ** 2 < 1] = CK
        c[((u + 0.0) / 0.13) ** 2 + ((v + 0.05) / 0.13) ** 2 < 1] = (0.95, 0.95, 0.95)
        return c
    return eye


def lay_eyes(k, state, centre, normal, half, lid=DARK, shape=None, iris=AMBER):
    """Both eyes laid by hand, u outward on each: the left one's outline and colour flipped."""
    shape = shape or eye_shape()
    # on the dark visor a closed eye is a light line (a black one would not be seen)
    line_ink = INK if state in ("Open", "Fierce") else "#C4C4CC"
    look = eye_look(state, eye_open(iris), srgb(lid), srgb(line_ink), line=0.2)
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        col = look if s == 1 else (lambda a, b, f=look: f(-a, b))
        out = shape if s == 1 else [(-a, b) for a, b in shape]
        k.decal(np.array(centre) * m, np.array(normal) * m, half, col, eps=0.002, rings=20, segs=56,
                outline=polygon(out))


def on_ell(c, r, d):
    """The point of an ellipsoid along a direction from its centre, and its outward normal there."""
    c, r = np.array(c, float), np.array(r, float)
    d = unit(*d)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


# --- Pawniard's frame ---------------------------------------------------------------------------------------------
HC, HR = np.array([0.0, -0.008, 0.236]), np.array([0.09, 0.082, 0.074])    # the helmet-head
TC, TR = np.array([0.0, 0.0, 0.124]), np.array([0.05, 0.046, 0.06])         # the segmented body
SHOULDER = np.array([0.048, -0.01, 0.168])
ELBOW = np.array([0.085, -0.05, 0.146])
WRIST = np.array([0.104, -0.088, 0.13])
HIP = np.array([0.03, 0.0, 0.082])
KNEE = np.array([0.058, -0.008, 0.046])
ANKLE = np.array([0.05, -0.012, 0.02])


def pawniard(src, out):
    k = Kit(src, coat="metal")

    # the head: one round helmet, a little flatter in front where the visor is
    def helmet(V):
        V = V.copy()
        V[:, 1] = np.where(V[:, 1] < 0, V[:, 1] * (0.9 + 0.1 * np.clip(V[:, 2] + 0.3, 0, 1)), V[:, 1])
        V[:, 2] = np.where(V[:, 2] < -0.4, -0.4 + (V[:, 2] + 0.4) * 0.6, V[:, 2])   # a flatter underside
        return V
    k.add("head", ell(HC, HR, deform=helmet), GREY, "head")
    k.add("neck", ell((0, -0.004, 0.172), (0.03, 0.028, 0.024)), DARK, "head")
    # the helmet's grey sides hang low past the cheeks
    k.add("cheekguard", ell(HC + (0.06, -0.006, -0.04), (0.024, 0.05, 0.046), rot=(0, -10, 0)), GREY, "head",
          mirror=True)

    # the red band over the crown, from the brow to the nape
    def crown(P):
        rel = P - HC
        half = 0.046 + 0.012 * np.clip(-rel[:, 1] / 0.08, 0, 1)
        return inside(np.abs(rel[:, 0]) - half, 0.002) * inside(-(rel[:, 2] + 0.004 + 0.012 * np.clip(rel[:, 1] / 0.08, 0, 1)), 0.002)
    k.paint(crown, RED, on=["head"])
    # the dark visor under the rim, across the front where the eyes peer out
    def visor(P):
        rel = P - HC
        return (inside(rel[:, 2] + 0.004, 0.002) * inside(-(rel[:, 2] + 0.05), 0.002)
                * inside(np.abs(rel[:, 0]) - 0.066, 0.003) * inside(rel[:, 1] + 0.035, 0.004))
    k.paint(visor, DARK, on=["head"])

    # the crescent blade on the helmet's right side, its face to the front, curving up past the crown
    ex, ez = unit(1, 0.32, 0), np.array([0, 0, 1.0])
    crescent = [(0.06, -0.035), (0.098, -0.005), (0.112, 0.035), (0.104, 0.074), (0.08, 0.105), (0.045, 0.128),
                (0.058, 0.098), (0.07, 0.066), (0.072, 0.03), (0.066, 0.0), (0.05, -0.022)]
    k.add("crescent", slab(crescent, HC + (0, -0.012, 0), ex, ez, 0.013), BLADE, "head", fuse=False)

    # the body: an egg, its segments dark seams
    k.add("torso", ell(TC, TR), BODY, "body")
    k.add("hips", ell((0, 0.0, 0.085), (0.042, 0.038, 0.03)), BODY, "body")
    for z in (0.096, 0.128, 0.158):
        k.paint(lambda P, z=z: inside(np.abs(P[:, 2] - z) - 0.0022, 0.0015) * inside(P[:, 2] - 0.172, 0.002), SEAM,
                on=["torso", "hips"])
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.0022, 0.0015) * (P[:, 1] < -0.02) * inside(P[:, 2] - 0.172, 0.002)
            * inside(0.08 - P[:, 2], 0.002), SEAM, on=["torso"])
    # two blades wrapped round it, the chest's from the left round to a point on the right, the belly's the other way
    for name, z, start, span, rise in (("chestblade", 0.15, -160, 290, -0.012), ("bellyblade", 0.104, 165, -290, 0.01)):
        f = math.sqrt(max(1 - ((z - TC[2]) / TR[2]) ** 2, 0.2))
        k.add(name, band(TC, TR[0] * f + 0.006, TR[1] * f + 0.006, z, 0.02, 0.011, span=span, start=start,
                         flare=0.35, rise=rise), BLADE, "body", fuse=False)

    # the arms: thin red, held forward and a little down; the hands are long white blades
    k.add("shoulder", ell(SHOULDER, (0.018, 0.018, 0.018)), RED, "arm{s}", mirror=True)
    k.add("arm", tube([SHOULDER, ELBOW, WRIST], [0.0125, 0.0115, 0.013]), RED, "arm{s}", mirror=True)
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"hand{side}", knife(WRIST * m + np.array([0.004 * s, -0.004, 0.002]), (s * 0.55, -0.55, 0.62),
                                    (0, -1, 0.2), 0.105, 0.03, 0.012, hook=0.2 * s), BLADE, f"arm{side}", fuse=False)

    # the legs: thin red and bowed, round red feet with two white claws
    k.add("leg", tube([HIP, KNEE, ANKLE], [0.0165, 0.0145, 0.014]), RED, "leg{s}", mirror=True)
    k.add("foot", ell(ANKLE + (0.002, -0.012, -0.006), (0.023, 0.03, 0.015)), RED, "leg{s}", mirror=True)
    for s, side in ((1, "R"), (-1, "L")):
        for dx in (-0.009, 0.009):
            t = (ANKLE + (0.002 + dx, -0.04, -0.012)) * (s, 1, 1)
            pts = [t + (-0.006, 0.01, -0.002), t + (0.006, 0.01, -0.002), t + (-0.006, 0.012, 0.01),
                   t + (0.006, 0.012, 0.01), t + (0, -0.008, -0.004), t + (0, -0.004, 0.005)]
            k.add(f"toe{side}{'ab'[dx > 0]}", hull(pts), BLADE, f"leg{side}", fuse=False)

    k.build(voxel=0.0036, smooth=3, band=0.006)

    # the eyes: amber, a black pupil low toward the nose, half shut under the rim
    EC, EN = on_ell(HC, HR, (0.42, -0.86, -0.3))
    def lay(state):
        lay_eyes(k, state, EC, EN, (0.021, 0.017))
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    pawniard(*args())
