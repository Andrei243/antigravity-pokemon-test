# Mareanie sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/mareanie.py -- <the export's Mareanie.glb> <out.glb>
#
# From the references: a little purple starfish of poison. A purple dome of a head with a crown of purple petals and
# a yellow spike at its top; ten thick pale cyan tentacles falling from round the dome like hair to the ground, the
# ends curling out with a pale lilac claw, set with pale lilac spikes standing out sideways; one short tentacle hangs
# over the forehead to just above the eyes with a spike pointing forward; under it a small purple face with cross
# yellow eyes (teal irises) and two white fangs; below the face a purple skirt of scalloped frills to the ground.
# Built to be grown from: Toxapex (toxapex.py) calls these functions. Joints: body (0, 0, 0.08), head (0, -0.01,
# 0.09), legs (+-0.02, -0.01, 0.03).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, unit, eye_look, polygon, lumpy, args

PURPLE, CYAN, SPIKE, TIP, YELLOW, EYE, IRIS, INK, FANG = ("#A57BD0", "#84E2E8", "#DCCBF0", "#BFA2E4", "#F6D445",
                                                          "#F4E25C", "#3B9DB0", "#25183A", "#FBFAF6")


def on(c, r, d):
    """The point of an ellipsoid along a direction of its unit sphere, and its outward normal."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    n = d / np.asarray(r, float)
    return np.asarray(c, float) + d * r, n / np.linalg.norm(n)


def radial(az):
    """The level direction at an azimuth in degrees from the front (-Y) toward +X."""
    a = math.radians(az)
    return np.array([math.sin(a), -math.cos(a), 0.0])


def spike(k, name, base, d, length, root, colour, bone):
    """A spike: a solid cone of six faces from base along d, root its radius at the base; a loose piece."""
    d = unit(*d)
    a = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a); b = np.cross(d, a)
    base = np.asarray(base, float)
    pts = [base + d * length]
    for i in range(6):
        t = 2 * math.pi * i / 6
        pts.append(base + root * (math.cos(t) * a + math.sin(t) * b))
        pts.append(base + d * length * 0.35 + root * 0.7 * (math.cos(t) * a + math.sin(t) * b))
    k.add(name, hull(pts), colour, bone, fuse=False, surface="shell")


def tentacle_path(centre, az, prof):
    """The path of a tentacle at an azimuth: prof is (distance out from the axis, height) points."""
    d = radial(az)
    return [np.asarray(centre, float) * np.array([1, 1, 0]) + d * r + np.array([0, 0, z]) for r, z in prof]


def tentacle(k, name, centre, az, prof, radii, colour, bone, flat=0.55):
    """A thick tentacle, flattened across its radial direction so it is broad seen from outside; a loose piece."""
    pts = tentacle_path(centre, az, prof)
    k.add(name, blade(pts, radii, radial(az), flat), colour, bone, fuse=False)
    return pts


# --- Mareanie's own frame --------------------------------------------------------------------------------------
DOME_C, DOME_R = np.array([0.0, 0.006, 0.135]), np.array([0.07, 0.066, 0.056])
FACE_C, FACE_R = np.array([0.0, -0.018, 0.086]), np.array([0.037, 0.034, 0.033])
PROF = [(0.052, 0.152), (0.078, 0.136), (0.09, 0.1), (0.094, 0.06), (0.099, 0.022), (0.114, 0.008)]
RADII = [0.022, 0.026, 0.027, 0.026, 0.022, 0.014]
FRINGE = [(0.036, 0.188), (0.066, 0.165), (0.076, 0.135), (0.074, 0.114)]
FRINGE_R = [0.016, 0.018, 0.016, 0.012]


def head(k):
    k.add("dome", ell(DOME_C, DOME_R, sub=5), PURPLE, "head")
    k.add("face", ell(FACE_C, FACE_R, sub=5), PURPLE, "head")
    k.add("neck", ell((0.0, -0.004, 0.07), (0.03, 0.03, 0.03), sub=4), PURPLE, "body")
    # The crown: a ring of petals round the top and a yellow spike out of it
    for i in range(7):
        a = 2 * math.pi * i / 7
        c = DOME_C + np.array([0.026 * math.sin(a), 0.026 * math.cos(a), 0.05])
        k.add(f"petal{i}", ell(c, (0.013, 0.013, 0.016), rot=(25 * math.cos(a), -25 * math.sin(a), 0), sub=3),
              PURPLE, "head")
    spike(k, "crownspike", DOME_C + np.array([0, 0, 0.05]), (0, 0, 1), 0.038, 0.012, YELLOW, "head")


def skirt(k):
    """The purple skirt of frills under the face: three scalloped tiers widening to the ground."""
    for z, r, h, bone in ((0.058, 0.027, 0.016, "body"), (0.038, 0.035, 0.016, "body"), (0.016, 0.043, 0.016, None)):
        sh = ell((0.0, -0.008, z), (r, r * 0.85, h), deform=lumpy(5, 1, 0.12), sub=4)
        if bone:
            k.add(f"tier{z}", sh, PURPLE, bone)
        else:
            V, F = sh
            for s, nm in ((1, "R"), (-1, "L")):
                keep = [f for f in F if (V[f, 0].mean() * s) >= -0.004]
                k.add(f"tierlow{nm}", (V, keep), PURPLE, f"leg{nm}")


def tentacles(k):
    """Ten tentacles: one short fringe over the forehead, nine to the ground round the rest of the head."""
    pts = tentacle(k, "fringe", DOME_C, 0, FRINGE, FRINGE_R, CYAN, "head")
    spike(k, "fringespike", pts[2] + radial(0) * 0.012, unit(0, -1, 0.15), 0.024, 0.008, SPIKE, "head")
    for i, az in enumerate((48, 82, 116, 150, 180, -150, -116, -82, -48)):
        pts = tentacle(k, f"tent{i}", DOME_C, az, PROF, RADII, CYAN, "body")
        out = radial(az)
        # a spike at the shoulder standing out and up, one lower on the side ones, a claw at the curled tip
        spike(k, f"spike{i}a", pts[1] + out * 0.014, out + np.array([0, 0, 0.35]), 0.034, 0.009, SPIKE, "body")
        if abs(az) > 60:
            spike(k, f"spike{i}b", pts[3] + out * 0.013, out + np.array([0, 0, 0.1]), 0.03, 0.008, SPIKE, "body")
        spike(k, f"claw{i}", pts[-1] + out * 0.004 + np.array([0, 0, 0.002]), out + np.array([0, 0, -0.35]), 0.024,
              0.008, TIP, "body")


def fangs(k, c, s=1.0, bone="head"):
    """Two small white fangs hanging from under the face's middle."""
    for x in (-0.009, 0.009):
        base = c + np.array([x * s, 0.0, 0.0])
        pts = [base + np.array([-0.0045 * s, 0, 0]), base + np.array([0.0045 * s, 0, 0]), base + np.array([0, 0.005, 0.001]),
               base + np.array([0, -0.002, 0.001]), base + np.array([0, -0.002, -0.009 * s])]
        k.add(f"fang{'R' if x > 0 else 'L'}", hull(pts), FANG, bone, fuse=False, surface="shell")


# --- the eyes: yellow, a teal iris and dark pupil, a purple lid slanting down to the nose: cross ---------------------
CE, CI, CK, CP = srgb(EYE), srgb(IRIS), srgb(INK), srgb(PURPLE)


def eye_open(u, v):
    c = np.tile(CE, (len(u), 1))
    c[(u + 0.05) ** 2 + (v + 0.12) ** 2 < 0.5 ** 2] = CI
    c[(u + 0.05) ** 2 + (v + 0.12) ** 2 < 0.24 ** 2] = CK
    lid = 0.38 + 0.3 * u                      # lower at the inner corner: a cross look
    c[v > lid] = CP
    c[np.abs(v - lid) < 0.12] = CK
    c[(u * u + v * v > 0.84 ** 2) & (v <= lid)] = CK
    return c


def eyes(k, state, face_c, face_r, x, z, half, tilt=0.55):
    for s in (1, -1):
        p, n = on(face_c, face_r, (x * s / face_r[0], -1.0, (z - face_c[2]) / face_r[2]))
        n = unit(n[0] * tilt, n[1], n[2] + 0.05)     # a little more to the front than the face's own curve
        look = eye_look(state, eye_open, CP, CK, line=0.16)
        k.decal(p, n, half, look if s > 0 else (lambda u, v, f=look: f(-u, v)), eps=0.0009, rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="scales")
    head(k)
    skirt(k)
    tentacles(k)
    fangs(k, FACE_C + np.array([0.0, -FACE_R[1] * 0.86, -0.019]))
    k.build(voxel=0.0021, smooth=4, band=0.003, wband=0.015)
    k.eye_states(lambda state: eyes(k, state, FACE_C, FACE_R, 0.016, 0.093, (0.0145, 0.0145)))
    k.export(out)
