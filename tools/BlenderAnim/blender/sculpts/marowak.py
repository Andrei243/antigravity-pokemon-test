# Marowak sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/marowak.py -- <the export's Marowak.glb> <out.glb>
#
# The design, from the references: Cubone grown, taller and fiercer, a deeper brown, the cream of its belly crossed by
# fine dark lines; a longer tail with a spike on it. The skull is its own head now: long and narrow, the crown running
# back to two short pointed horns swept up and back, the snout long and tapering forward and down to a blunt point,
# a dark crease running from under the eye along the snout. The eyes are narrow slits under a heavy brow, a brown eye
# with a dark pupil in each, glaring. The bone is raised in its hand on -X (the viewer's left, as the art has it),
# its lower end forward, its upper end up and back past the head; the other arm reaches forward, two white claws.
# Built by Cubone's functions (sibling("cubone")): its colours' way, body, legs, arms, club and eye holes.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, polygon, hull, sibling, args

cb = sibling("cubone")

SKIN, BELLY, LINE, SOCKET = "#C8955A", "#EBD6A3", "#9A7448", "#9C6E40"
IRIS = "#B07636"
SKULL = cb.SKULL
INK, WHITE = cb.INK, cb.WHITE

MAROWAK = dict(
    skin=SKIN, belly=BELLY,
    torso_c=(0.0, 0.0, 0.225), torso_r=(0.135, 0.12, 0.145), lean=-6,
    belly_c=(0.0, -0.105, 0.215), belly_r=(0.088, 1, 0.12),
    tail=[(0.0, 0.08, 0.15), (0.03, 0.18, 0.1), (0.07, 0.28, 0.06), (0.1, 0.34, 0.06)], tail_r=[0.055, 0.04, 0.026, 0.012],
    spike=[(0.06, 0.26, 0.08), (0.08, 0.28, 0.13)],
    thigh_c=(0.08, -0.005, 0.125), thigh_r=(0.072, 0.08, 0.08),
    shin=[(0.085, -0.01, 0.08), (0.088, -0.015, 0.035)], shin_r=[0.052, 0.046],
    foot_c=(0.088, -0.035, 0.022), foot_r=(0.05, 0.075, 0.024),
)
CROWN_C, CROWN_R = np.array([0.0, -0.03, 0.5]), np.array([0.124, 0.175, 0.118])
SNOUT_C, SNOUT_R = np.array([0.0, -0.19, 0.448]), np.array([0.082, 0.165, 0.074])
EYE_AZ, EYE_EL = 46, 6
EYE_SQ = cb.squircle(2.4)


def taper(V):
    """The snout narrowing to its blunt tip at the front (-Y)."""
    V = V.copy()
    t = np.clip(-V[:, 1], 0, 1)
    s = 1 - 0.55 * t ** 1.2
    V[:, 0] *= s; V[:, 2] *= s
    return V


def stroke(k, a, b, width=0.005):
    """A crease: a thin straight dark patch from a to b over the surface."""
    a, b = np.array(a, float), np.array(b, float)
    m = (a + b) / 2
    d = b - a
    n = np.array([np.sign(m[0]) * 1.0, -0.6, 0.1]); n /= np.linalg.norm(n)
    d = d - n * (d @ n)
    L = np.linalg.norm(d) / 2
    t = width / 2 / L
    k.decal(m, n, (L, L), "#4A3A3A", up=d / np.linalg.norm(d), eps=0.0035,
            outline=polygon([(-t, -1), (t, -1), (t, 1), (-t, 1)]), rings=6, segs=40)


def glare(lid):
    """A brown eye with a dark pupil, ringed in ink, its top cut by the heavy brow sloping down to the snout."""
    I = srgb(IRIS)
    def look(u, v):
        c = np.tile(I, (len(u), 1))
        c[((u + 0.12) / 0.26) ** 2 + ((v + 0.05) / 0.62) ** 2 < 1] = INK

        top = 0.3 + 0.3 * u
        c[v > top] = lid
        c[np.abs(v - top) < 0.16] = INK
        share = np.hypot(u, v) / np.array([EYE_SQ(t) for t in np.arctan2(v, u)])
        c[share > 0.84] = INK
        return c
    return look


def marowak(src, out):
    k = Kit(src, coat="fur")
    F = MAROWAK
    cb.body(k, F)
    # fine dark lines across the belly's cream
    bc, br = np.array(F["belly_c"]), np.array(F["belly_r"])
    def lines(P):
        m = np.zeros(len(P))
        for z in (0.15, 0.19, 0.23, 0.27):
            m = np.maximum(m, inside(np.abs(P[:, 2] - z) - 0.0028, 0.0015))
        inb = inside(np.sqrt((((P - bc) / br)[:, [0, 2]] ** 2).sum(1)) - 0.93, 0.002) * (P[:, 1] < bc[1] + 0.03)
        return m * inb
    k.paint(lines, LINE, on=["torso"])
    cb.legs(k, F)

    # The skull that is its head: the crown, the long tapering snout, the brow, two short pointed horns back
    k.add("neck", ell((0.0, 0.0, 0.37), (0.1, 0.095, 0.07)), SKIN, "head")
    k.add("crown", ell(CROWN_C, CROWN_R), SKULL, "head", surface="shell")
    k.add("snout", ell(SNOUT_C, SNOUT_R, rot=(-16, 0, 0), deform=taper), SKULL, "head", surface="shell")

    k.add("jaw", ell((0.0, -0.12, 0.405), (0.085, 0.1, 0.045)), SKULL, "head", surface="shell")
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        b0 = np.array((0.07, 0.05, 0.56)) * m
        pts = [b0 + np.array((-0.03 * s, -0.03, 0)), b0 + np.array((0.03 * s, -0.03, 0)),
               b0 + np.array((-0.03 * s, 0.04, 0)), b0 + np.array((0.03 * s, 0.04, -0.01)),
               b0 + np.array((0.0, 0.0, -0.04)), b0 + np.array((0.04 * s, 0.1, 0.085))]
        k.add(f"horn{side}", hull(pts), SKULL, "head", fuse=False, surface="shell")

    # Arms: the right (+X) reaching forward, two white claws; the left (-X) raised with the bone
    cb.arm(k, F, "R", [(0.11, -0.02, 0.319), (0.15, -0.09, 0.3), (0.175, -0.16, 0.28)], [0.04, 0.035, 0.032],
           (0.178, -0.18, 0.278), (0.034, 0.036, 0.032))
    for dz in (-0.014, 0.014):
        b = np.array((0.18, -0.21, 0.278 + dz))
        k.add(f"fclaw{dz > 0}", hull([b + (-0.008, 0.01, -0.007), b + (0.008, 0.01, -0.007), b + (-0.008, 0.01, 0.007),
                                      b + (0.008, 0.01, 0.007), b + (0.0, -0.022, 0.0)]),
              cb.CLAW, "armR", fuse=False, surface="shell")
    cb.arm(k, F, "L", [(-0.11, -0.02, 0.319), (-0.16, -0.08, 0.33), (-0.19, -0.13, 0.37)], [0.04, 0.035, 0.033],
           (-0.192, -0.14, 0.375), (0.036, 0.036, 0.036))
    cb.club(k, (-0.17, -0.27, 0.29), (-0.225, 0.03, 0.6), 0.026, "armL")

    # The creases from under each eye forward along the snout, and the eye slits under the brow
    holes = []
    for s in (1, -1):
        p, n = cb.on_ell(CROWN_C, CROWN_R, s * EYE_AZ, EYE_EL)
        holes.append((p, n, s))
        cb.socket(k, p, n, (0.048, 0.03), s, tilt=14, rim=0.22, color=SOCKET)
        stroke(k, p + np.array([-0.012 * s, -0.04, -0.03]), p + np.array([-0.04 * s, -0.13, -0.05]))

    k.build(voxel=0.006, smooth=4, band=0.006)

    lid = srgb(SKULL)
    look_open = glare(srgb(SOCKET))

    def eyes(state):
        f = eye_look(state, look_open, srgb(SOCKET), INK, line=0.18)
        for p, n, s in holes:
            cb.mirrored_decal(k, p, n, (0.034, 0.022), f, s, up=cb.tilted_up(n, 14 * s), eps=0.0065,
                              outline=EYE_SQ, rings=20, segs=56)

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    marowak(*args())
