# Toxapex sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Mareanie's script (mareanie.py: its spikes, tentacles and eyes):
#   blender --background --factory-startup --python sculpts/toxapex.py -- <the export's Toxapex.glb> <out.glb>
#
# From the references: Mareanie grown into a dome of twelve thick legs. Ten close round it from a pink crown down to
# the ground, pale blue outside and set with pale blue spikes, dark indigo inside; the two in front stand up from the
# crown in a V, their fronts indigo set with pink spikes, their backs and edges pale blue; every leg ends in an orange
# point. Between the front legs' feet the small face peers out of the dark: purple with a pink frill below, cross
# yellow eyes and two white fangs. Joints: body (0, 0, 0.12); leg1 to leg8 all at the crown (0, 0.02, 0.31), each
# moving the legs nearest its own direction (54, 90, ..., 306 degrees from the front toward +X).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, args, sibling

m = sibling("mareanie")

PALE, INNER, FRONT, PINK, ORANGE, PURPLE, FRILL, EYE = ("#9CD8EE", "#3B3586", "#5A4DB4", "#E0479F", "#F2A257",
                                                        "#73489F", "#DE5AA4", "#F4D44C")
CROWN = np.array([0.0, 0.02, 0.31])
BONE_AZ = {f"leg{i}": a for i, a in enumerate((54, 90, 126, 162, -162, -126, -90, -54), start=1)}


def bone_for(az):
    """The leg bone whose direction is nearest an azimuth."""
    return min(BONE_AZ, key=lambda b: abs((BONE_AZ[b] - az + 180) % 360 - 180))


DOME = [(0.03, 0.3), (0.1, 0.29), (0.18, 0.24), (0.235, 0.16), (0.26, 0.085), (0.275, 0.04)]
DOME_R = [0.04, 0.06, 0.07, 0.07, 0.062, 0.045]
RAISED = [(0.03, 0.29), (0.08, 0.33), (0.145, 0.375), (0.2, 0.41), (0.235, 0.435)]
RAISED_R = [0.04, 0.05, 0.052, 0.046, 0.034]


def dome_leg(k, i, az):
    out = m.radial(az)
    bone = bone_for(az)
    pts = m.tentacle_path(CROWN, az, DOME)
    axis = unit(*(out + np.array([0, 0, 0.6])))
    k.add(f"leg{i}", blade(pts, DOME_R, axis, 0.42), PALE, bone, fuse=False)
    # the orange point, carrying on the leg's line past its end
    end, before = pts[-1], pts[-2]
    d = unit(*(out - np.array([0, 0, 0.38])))      # out along the ground, ending at it
    m.spike(k, f"point{i}", end + d * 0.01, d, 0.07, 0.03, ORANGE, bone)
    # pale blue spikes on the outside
    for j, (p, up) in enumerate(((pts[2], 0.6), (pts[3], 0.25), (pts[4], 0.0))):
        side = np.cross(out, (0, 0, 1.0)) * (0.02 if j % 2 else -0.02)
        m.spike(k, f"legspike{i}_{j}", p + out * 0.02 + side, out + np.array([0, 0, up]), 0.055, 0.016, PALE, bone)


def raised_leg(k, i, az):
    """A front leg standing up from the crown: indigo in front with pink spikes, pale blue behind, an orange point."""
    out = m.radial(az)
    bone = bone_for(az)
    pts = m.tentacle_path(CROWN, az, RAISED)
    k.add(f"raised{i}", blade(pts, RAISED_R, out, 0.5), FRONT, bone, fuse=False)
    back = [p + np.array([0, 0.012, 0.0]) - out * 0.008 for p in pts]
    k.add(f"raisedback{i}", blade(back, [r * 1.04 for r in RAISED_R], out, 0.5), PALE, bone, fuse=False)
    end, before = pts[-1], pts[-2]
    d = unit(*(end - before))
    m.spike(k, f"raisedpoint{i}", end + d * 0.008, d, 0.075, 0.029, ORANGE, bone)
    # pink spikes standing out of the broad front face (the face square to the leg's line, out and down)
    for j, t in enumerate((1, 2, 3)):
        p = pts[t]
        dr = float((pts[t + 1] - pts[t - 1]) @ out); dz = float(pts[t + 1][2] - pts[t - 1][2])
        n = unit(*(out * dz - np.array([0, 0, 1.0]) * dr))
        m.spike(k, f"pinkspike{i}_{j}", p + n * 0.012, n + np.array([0, 0, 0.25]), 0.05, 0.016, PINK, bone)


def body(k):
    # The dark inside of the dome, fused with the face so the gaps between the legs show dark
    k.add("inside", ell((0.0, 0.03, 0.16), (0.21, 0.2, 0.17), sub=5), INNER, "body")
    k.add("face", ell((0.0, -0.18, 0.13), (0.052, 0.045, 0.05), sub=5), PURPLE, "body")
    k.add("frill", ell((0.0, -0.18, 0.075), (0.048, 0.04, 0.03), deform=m.lumpy(5, 1, 0.15), sub=4), FRILL, "body")
    k.add("chin", ell((0.0, -0.16, 0.05), (0.06, 0.045, 0.03), deform=m.lumpy(6, 1, 0.15), sub=4), FRILL, "body")
    # The pink crown the legs grow from, with a ring of orange points
    k.add("crown", ell(CROWN + np.array([0, 0, -0.015]), (0.065, 0.065, 0.04), sub=4), PINK, "body", fuse=False)
    for j in range(6):
        a = 360 * j / 6 + 30
        o = m.radial(a)
        m.spike(k, f"crownpoint{j}", CROWN + o * 0.045 + np.array([0, 0, 0.005]), o + np.array([0, 0, 1.4]), 0.035,
                0.01, ORANGE, bone_for(a))


def legs(k):
    for i, az in enumerate((42, 74, 106, 138, 168, -168, -138, -106, -74, -42)):
        dome_leg(k, i, az)
    raised_leg(k, 0, 30)
    raised_leg(k, 1, -30)


FACE_C, FACE_R = np.array([0.0, -0.18, 0.13]), np.array([0.052, 0.045, 0.05])


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="scales")
    body(k)
    legs(k)
    m.fangs(k, FACE_C + np.array([0.0, -FACE_R[1] * 0.86, -0.028]), s=1.6, bone="body")
    k.build(voxel=0.005, smooth=4, band=0.006, wband=0.03)
    m.CE = srgb(EYE)
    m.CI = srgb("#C0503A")
    m.CP = srgb(PURPLE)
    k.eye_states(lambda state: m.eyes(k, state, FACE_C, FACE_R, 0.022, 0.138, (0.02, 0.02)))
    k.export(out)
