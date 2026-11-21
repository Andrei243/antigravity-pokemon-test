# Primarina sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Popplio's script (sibling("popplio")): the same eye, flipper with its stripes, frilled collar and nose.
#   blender --background --factory-startup --python sculpts/primarina.py -- <the export's Primarina.glb> <out.glb>
#
# From the references: a white sea lion soloist sitting up; a small head with a pointed muzzle and a little pink nose
# at its tip, blue eyes; a frilled pale blue cap on the crown and from it a great mass of sea-green hair trailing back
# (here back and out to +X), gathered by a string of pearls and ending in curled locks; a pink starfish on the head's
# side (-X, the side the front three-quarter camera sees); a pale lavender-blue ruff at the neck and a frilled skirt
# at the waist; long white front flippers reaching down to the ground before her, broad at their ends with grey
# stripes; a thick tail blue above and white below, pink spines along its top, curling round her -X side to the front
# where its white fluke lies on the ground.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, slab, srgb, inside, smoothstep, unit, lumpy, outward, args, sibling

p = sibling("popplio")

WHITE, BLUE, HAIR, FRILL, PINK, PEARL = "#F2F4FA", "#2F78D8", "#86D6DC", "#C2D0F2", "#EE8FA4", "#FBFBFF"
STRIPE, EYEBLUE, EYEDEEP, STAR = "#A6AEBE", "#3C86E2", "#1C3A86", "#F08A9C"

H = np.array([0, -0.018, 0.335])
HR = np.array([0.047, 0.048, 0.044])
MZ = H + np.array([0, -0.05, -0.014])
TAIL = [(0, 0.055, 0.07), (-0.045, 0.12, 0.052), (-0.112, 0.11, 0.042), (-0.142, 0.035, 0.036), (-0.122, -0.045, 0.03),
        (-0.085, -0.088, 0.027)]
TRAD = [0.054, 0.046, 0.039, 0.033, 0.027, 0.021]


def primarina(src, out):
    k = Kit(src, coat="scales")
    k.add("head", ell(H, HR), WHITE, "head")
    def taper(V):
        V = V.copy(); f = 1 - 0.55 * np.clip(-V[:, 1], 0, 1)
        V[:, 0] *= f; V[:, 2] *= f
        return V
    k.add("muzzle", ell(MZ, (0.022, 0.036, 0.019), rot=(6, 0, 0), deform=taper), WHITE, "head")
    k.add("neck", ell((0, -0.008, 0.272), (0.03, 0.03, 0.045)), WHITE, "body")
    k.add("torso", ell((0, 0.005, 0.18), (0.046, 0.042, 0.09), rot=(-8, 0, 0)), WHITE, "body")
    k.add("hips", ell((0, 0.025, 0.07), (0.058, 0.064, 0.064)), WHITE, "body")
    # The long front flippers: an arm down from the shoulder to a broad paddle on the ground before her
    k.add("arm", tube([(0.034, -0.02, 0.225), (0.056, -0.05, 0.14), (0.064, -0.075, 0.05)], [0.02, 0.019, 0.019]),
          WHITE, "leg{s}", mirror=True)
    p.flipper(k, "paddle", (0.064, -0.075, 0.048), (0.16, -0.95, -0.26), (0.056, 0.029, 0.0105), WHITE, "leg{s}",
              roll=-8, stripes=STRIPE)
    # The tail, curling round her -X side to the front, blue above and white below
    k.add("tail", tube(TAIL, TRAD, seg=20, per=6), WHITE, "tail")
    def blue_top(P):
        return inside(0.046 - P[:, 2], 0.004) * inside(0.05 - np.hypot(P[:, 0], P[:, 1] - 0.02), 0.01)
    k.paint(blue_top, BLUE, on=["tail"])
    # The fluke: two white lobes lying on the ground at the tail's end
    end = np.array(TAIL[-1], float)
    for i, d in enumerate((unit(0.1, -0.9, 0.18), unit(0.78, -0.55, 0.18))):
        k.add(f"fluke{i}", p.flat(end + d * 0.05, d, (0.054, 0.03, 0.01), roll=0), WHITE, "tail")
    # Pink spines along the tail's top, leaning back along it
    for t in (0.5, 1.4, 2.3, 3.2):
        i = int(t); f = t - i
        c = np.array(TAIL[i]) * (1 - f) + np.array(TAIL[i + 1]) * f
        r = TRAD[i] * (1 - f) + TRAD[i + 1] * f
        nxt = np.array(TAIL[i + 1]) - np.array(TAIL[i])
        d = unit(*(np.array([0, 0, 1.0]) * 1.0 - unit(*nxt) * 0.6 + unit(c[0], c[1] - 0.02, 0) * 0.4))
        k.add(f"spine{i}", lathe(c + np.array([0, 0, r * 0.75]), d,
                                 [(0, 0), (0.011, 0), (0.007, 0.016), (0, 0.032)], seg=16),
              PINK, "tail", fuse=False, surface="shell")
    # The ruff at the neck and the frilled skirt at the waist
    p.collar(k, "ruff", 0.272, 0.024, 0.236, 0.066, 0.22, 7, FRILL, "body", 0.007, wave=0.16)
    p.collar(k, "skirt", 0.13, 0.04, 0.085, 0.088, 0.1, 8, FRILL, "body", 0.007, wave=0.18)
    # The frilled cap on the crown and the mass of hair trailing back and out to +X, gathered by pearls
    k.add("cap", ell(H + (0, 0.018, 0.034), (0.046, 0.04, 0.024), deform=lumpy(6, 2, 0.1)), FRILL, "head",
          fuse=False, surface="hair")
    HP = [H + (0.0, 0.03, 0.022), np.array([0.025, 0.08, 0.375]), np.array([0.055, 0.12, 0.35]),
          np.array([0.08, 0.15, 0.29]), np.array([0.09, 0.158, 0.23]), np.array([0.085, 0.14, 0.18])]
    k.add("hair", tube(HP, [0.038, 0.04, 0.026, 0.048, 0.054, 0.04], seg=20, per=6), HAIR, "earR")
    band = HP[2]; ax = unit(*(HP[3] - HP[1]))
    a, b = np.cross(ax, (0, 0, 1.0)), None
    a /= np.linalg.norm(a); b = np.cross(ax, a)
    for j in range(9):
        th = 2 * math.pi * j / 9
        k.add(f"pearl{j}", ell(band + 0.028 * (math.cos(th) * a + math.sin(th) * b), (0.0095,) * 3, sub=3), PEARL,
              "earR", fuse=False, surface="shell")
    # Curled locks at the hair's end
    for j, (dx, dy) in enumerate(((0.035, 0.0), (-0.012, 0.035), (0.022, -0.035))):
        e = HP[-1]
        k.add(f"lock{j}", tube([e, e + (dx, dy, -0.04), e + (dx * 1.8, dy * 1.8, -0.06), e + (dx * 1.4, dy * 1.4 - 0.012,
                                                                                            -0.074)],
                                [0.022, 0.016, 0.012, 0.01], seg=14, per=5), HAIR, "earR")
    k.paint(lambda P: np.ones(len(P)), HAIR, on=["hair", "lock"])
    # The pink starfish on the head's -X side
    sd = unit(-0.72, -0.2, 0.66)
    sc = p.at_surface(H, HR, sd) + sd * 0.004
    ex = unit(*np.cross(sd, (0, 0, 1.0))); ez = np.cross(ex, sd)
    pts = [(0.016 * (1 if i % 2 == 0 else 0.45) * math.sin(2 * math.pi * i / 10),
            0.016 * (1 if i % 2 == 0 else 0.45) * math.cos(2 * math.pi * i / 10)) for i in range(10)]
    k.add("star", slab(pts, sc, ex, ez, 0.008), STAR, "head", fuse=False, surface="shell")
    p.nose(k, MZ + np.array([0, -0.033, 0.003]), unit(0, -1, 0.15), 0.0085, PINK, "head")
    k.build(voxel=0.0036, smooth=4, band=0.003, wband=0.025)
    E = [(p.at_surface(H, HR, d), d, sx) for sx in (1, -1) for d in [unit(0.52 * sx, -0.82, 0.18)]]
    k.eye_states(lambda st: p.eyes(k, E, (0.0125, 0.0155), st, WHITE,
                                   p.popplio_eye(pupil=EYEBLUE, iris=EYEDEEP), eps=0.0016))
    k.export(out)


if __name__ == "__main__":
    primarina(*args())
