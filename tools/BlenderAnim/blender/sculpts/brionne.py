# Brionne sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Popplio's script (sibling("popplio")): the same eye, flipper, collar, nose and mouth.
#   blender --background --factory-startup --python sculpts/brionne.py -- <the export's Brionne.glb> <out.glb>
#
# From the references: a slimmer, lighter blue sea lion, the darker blue low on its belly; a skirt of white and pale
# cyan frills round its lower body; a pink nose in a white puff of a muzzle; dark oval eyes with a magenta iris and a
# glint; an open mouth with one small white fang; two rounded pale ear flaps at the back of the head standing up; two
# stalks of pale cyan beads rising from the crown, each ending in a big white bubble; front flippers blue at the root
# and broad and white at the end with grey stripes; hind flippers blue with white tips, spread behind.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, srgb, inside, smoothstep, unit, lumpy, outward, args, sibling

p = sibling("popplio")

BLUE, DEEP, WHITE, PINK, FRILL, EAR = "#58A6E2", "#2F6FB8", "#F6F8FC", "#EE8FAA", "#C4E8EE", "#BDD0E2"
BEAD, IRIS, STRIPE = "#B6E2EA", "#B23A78", "#9AA8BC"

H = np.array([0, -0.045, 0.238])
HR = np.array([0.063, 0.059, 0.058])
MZ = H + np.array([0, -0.054, -0.019])
MR = np.array([0.026, 0.022, 0.021])


def white_end(k, frame, root, length, at, on, mirror=True):
    """Paints a flipper white from a share at of its length on (mirrored by |x|)."""
    s, dd, n = frame
    root = np.array(root, float)
    def m(P):
        Q = P.copy()
        if mirror:
            Q[:, 0] = np.abs(Q[:, 0])
        t = (Q - root) @ dd
        return inside(length * at - t, 0.0015)
    k.paint(m, WHITE, on=on)


def brionne(src, out):
    k = Kit(src, coat="scales")
    k.add("head", ell(H, HR), BLUE, "head")
    k.add("muzzle", ell(MZ, MR, deform=lumpy(5, 3, 0.12)), WHITE, "head")
    k.add("neck", ell((0, -0.022, 0.19), (0.046, 0.044, 0.042)), BLUE, "body")
    def pear(V):
        V = V.copy()
        s = 1 + 0.12 * np.clip(-V[:, 2], 0, 1) - 0.06 * np.clip(V[:, 2], 0, 1) ** 2
        V[:, 0] *= s; V[:, 1] *= s
        V[:, 2] = np.where(V[:, 2] < -0.8, -0.8 + (V[:, 2] + 0.8) * 0.4, V[:, 2])
        return V
    k.add("body", ell((0, 0.004, 0.11), (0.05, 0.05, 0.098), deform=pear), BLUE, "body")
    k.paint(lambda P: inside(P[:, 2] - 0.09, 0.004) * inside(P[:, 1] + 0.015, 0.02), DEEP, on=["body"])
    # Front flippers, longer and broader than Popplio's, white at the end
    fr = p.flipper(k, "arm", (0.036, -0.032, 0.13), (0.34, -0.66, -0.67), (0.064, 0.03, 0.0105), BLUE, "leg{s}",
                   roll=-18, stripes=STRIPE)
    white_end(k, fr, (0.036, -0.032, 0.13), 0.064 * 1.8, 0.5, ["arm"])
    # Hind flippers spread behind, rising at their white tips
    hr = p.flipper(k, "hind", (0.022, 0.04, 0.02), (0.7, 0.66, 0.2), (0.05, 0.025, 0.0105), BLUE, "body", roll=10,
                   stripes=STRIPE)
    white_end(k, hr, (0.022, 0.04, 0.02), 0.05 * 1.8, 0.62, ["hind"])
    # The skirt: an outer white frill and an inner pale cyan one, round the lower body
    p.collar(k, "skirtout", 0.1, 0.044, 0.05, 0.08, 0.1, 8, WHITE, "body", 0.006, wave=0.18)
    p.collar(k, "skirtin", 0.075, 0.048, 0.02, 0.085, 0.05, 8, FRILL, "body", 0.006, wave=0.18)
    # Ear flaps at the back of the head, standing up and out
    for sx in (1, -1):
        ec = p.at_surface(H, HR, unit(0.55 * sx, 0.42, 0.72)) + np.array([0.004 * sx, 0.004, 0.006])
        k.add(f"ear{'RL'[sx < 0]}", ell(ec, (0.012, 0.022, 0.022), rot=(-35, 30 * sx, 0)), EAR, "head", fuse=False,
              surface="skin")
    # Two stalks of beads from the crown, each ending in a big white bubble
    for sx in (1, -1):
        d = unit(0.5 * sx, 0.55, 0.67)
        base = p.at_surface(H, HR, unit(0.2 * sx, 0.05, 0.98))
        for i, (t, r) in enumerate(((0.006, 0.011), (0.025, 0.01))):
            k.add(f"bead{i}{'RL'[sx < 0]}", ell(base + d * t, (r, r, r), sub=3), BEAD, "head", fuse=False,
                  surface="skin")
        k.add(f"bubble{'RL'[sx < 0]}", ell(base + d * 0.053, (0.024, 0.024, 0.024), sub=3), WHITE, "head",
              fuse=False, surface="skin")
    p.nose(k, p.at_surface(MZ, MR, unit(0, -0.95, 0.3)), unit(0, -0.93, 0.36), 0.014, PINK, "head")
    md = unit(0, -0.6, -0.8)
    p.open_mouth(k, p.at_surface(H, HR, md) + np.array([0, -0.004, 0.004]), md, (0.019, 0.012), tooth=True)
    k.build(voxel=0.0028, smooth=4, band=0.003, wband=0.02)
    E = [(p.at_surface(H, HR, d), d, sx) for sx in (1, -1) for d in [unit(0.5 * sx, -0.83, 0.1)]]
    k.eye_states(lambda st: p.eyes(k, E, (0.0145, 0.022), st, BLUE, p.popplio_eye(pupil=p.INK, iris=IRIS)))
    k.export(out)


if __name__ == "__main__":
    brionne(*args())
