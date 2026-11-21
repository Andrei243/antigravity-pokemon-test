# Quaquaval sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/quaquaval.py -- <the export's Quaquaval.glb> <out.glb>
# Quaxwell grown into a tall dancing bird (built by quaxly.py's functions: its white, eyes, plumes and drops): a white
# face and long white neck, an orange beak pointing down, heavy-lidded pale blue eyes under a bold red-orange mask
# whose ends flare up and out like a headband's; dark blue hair swept back from the crown with a great pale-blue
# teardrop crest rising over it; a collar of pale-blue points round the neck's foot over a white bib; a dark blue
# trunk and haunches; wings held forward and down, pale-blue coverts at the shoulder and five long blue finger
# feathers; long legs flaring into bell-bottoms banded with pale-blue waves, orange feet with red-tipped toes; and its
# largest mass, a boa of pale-blue water drops rimmed in blue, rising from the lower back to a rosette out to its right
# side and hanging in a chain to the knee.
# The pictures overrule the brief: what the brief calls a fan of tail feathers is this chain of drops, which both
# pictures show beside the body on one side; it is built as one piece behind and out to +X.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, ring_band, srgb, inside, smoothstep, tri, unit, args, sibling

Q = sibling("quaxly")
WHITE, INK = Q.WHITE, Q.INK
BLUE, DEEP, SKY, RED, ORANGE, TOE = "#2C40C8", "#22309E", "#8FDBF2", "#F0502C", "#F7A63C", "#E5452C"
VOXEL = 0.008

H = np.array([0.0, -0.062, 0.785])           # the face's centre
HR = np.array([0.066, 0.068, 0.066])
DE = unit(0.5, -0.85, -0.05)
EYE_HALF = (0.021, 0.018)


def quaquaval(src, out):
    k = Kit(src, coat="fur")

    # The trunk: dark blue, a little forward; a white bib under the collar
    k.add("pelvis", ell((0, 0.02, 0.45), (0.098, 0.088, 0.08)), BLUE, "body")
    k.add("torso", ell((0, -0.012, 0.545), (0.086, 0.072, 0.098), rot=(-8, 0, 0)), BLUE, "body")
    k.add("neck", tube([(0, -0.015, 0.6), (0, -0.035, 0.68), (0, -0.05, 0.75)], [0.046, 0.042, 0.042]), WHITE,
          "head")
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.012 - 0.35 * np.clip(P[:, 2] - 0.53, 0, 1), 0.003)
            * inside(0.53 - P[:, 2], 0.003) * inside(P[:, 2] - 0.66) * inside(P[:, 1] + 0.03), WHITE)

    # The collar: pale-blue points round the neck's foot
    for i, a in enumerate((-150, -110, -70, -35, 35, 70, 110, 150)):
        t = math.radians(a)
        d = unit(math.sin(t) * 0.9, -math.cos(t) * 0.7, -0.55)
        base = np.array([0.0, -0.02, 0.63]) + 0.04 * np.array([math.sin(t), -math.cos(t), 0])
        k.add(f"collar{i}", Q.plume(base, d, 0.09, 0.032, 0.026, across=np.cross(d, (0, 0, 1.0)) + np.array([0, 0, 0.5])),
              SKY, "body", fuse=False, surface="hair")

    # The head: a white face, the red-orange mask over the eyes, hair swept back, the teardrop crest
    k.add("face", ell(H, HR), WHITE, "head")
    k.add("hair", ell((0, 0.01, 0.825), (0.068, 0.085, 0.055), rot=(-18, 0, 0)), BLUE, "head", surface="hair")
    k.add("mask", ell((0, -0.048, 0.808), (0.075, 0.058, 0.025), rot=(-6, 0, 0)), RED, "head")
    # the mask's band across the brow, crisp over the white face
    k.paint(lambda P: inside(0.8 - P[:, 2] + 0.08 * np.abs(P[:, 0]), 0.0025)
            * inside(P[:, 2] - 0.846 - 0.1 * np.abs(P[:, 0]), 0.0025) * inside(P[:, 1] + 0.01)
            * inside(np.linalg.norm(P - H, axis=1) - 0.1), RED)
    k.paint(lambda P: inside(0.846 + 0.1 * np.abs(P[:, 0]) - P[:, 2], 0.0025) * inside(P[:, 1] + 0.01)
            * inside(np.linalg.norm(P - H, axis=1) - 0.1), BLUE)
    for side in (1, -1):
        for j, (up, ln) in enumerate(((0.55, 0.11), (0.25, 0.09))):
            d = unit(0.85 * side, 0.45, up)
            base = np.array([0.055 * side, -0.035, 0.808 - 0.01 * j])
            k.add(f"maskwing{j}{'R' if side > 0 else 'L'}", Q.plume(base, d, ln, 0.026, 0.026, across=(0, 0, 1.0)),
                  RED, "head", fuse=False, surface="hair")
    for j, (x, up, ln) in enumerate(((0.0, 0.25, 0.17), (0.04, 0.12, 0.15), (-0.04, 0.12, 0.15), (0.065, -0.1, 0.12),
                                     (-0.065, -0.1, 0.12))):
        d = unit(x * 4, 1.0, up)
        base = np.array([x * 0.5, 0.02, 0.84 - abs(x) * 0.5])
        k.add(f"plume{j}", Q.plume(base, d, ln, 0.035, 0.028, across=np.cross(d, (0, 0, 1.0)) * 0.3 + np.array([0, 0, 1.0])),
              DEEP if j % 2 else BLUE, "head", fuse=False, surface="hair")
    k.add("crest", Q.drop(np.array([0, 0.03, 0.955]), unit(0, -0.18, -1), 0.14, 0.045, 0.03, across=(1, 0, 0)),
          SKY, "head", fuse=False, surface="hair")
    k.add("creststripe", Q.drop(np.array([0, 0.03, 0.96]), unit(0, -0.18, -1), 0.1, 0.018, 0.036, across=(1, 0, 0)),
          WHITE, "head", fuse=False, surface="hair")
    k.add("crestroot", ell((0, 0.02, 0.86), (0.02, 0.025, 0.03)), BLUE, "head", surface="hair")

    # The beak: orange, pointed, a little down
    k.add("beak", ell((0, -0.13, 0.755), (0.026, 0.05, 0.022), rot=(28, 0, 0), deform=Q.pointed_beak), ORANGE,
          "head", fuse=False, surface="shell")

    # Wings held forward and down: pale-blue coverts at the shoulder, five long blue fingers
    sh, el, wr = np.array([0.075, 0.0, 0.6]), np.array([0.125, -0.02, 0.51]), np.array([0.112, -0.1, 0.44])
    k.add("arm", tube([sh, el, wr], [0.036, 0.032, 0.028]), BLUE, "wing{s}", mirror=True)
    for i, (dx, dy) in enumerate(((0.4, 0.5), (0.65, 0.15), (0.45, -0.25))):
        d = unit(dx, dy, -0.75)
        k.add(f"covert{i}", Q.plume(np.array([0.1, -0.01, 0.57]) + np.array([0, -0.02 * i, -0.02 * i]), d, 0.08, 0.03,
                                    0.026, across=np.cross(d, unit(-0.2, -1, 0))), SKY, "wing{s}", fuse=False,
              mirror=True, surface="hair")
    for i, a in enumerate((-40, -15, 10, 35, 60)):
        t = math.radians(a)
        d = unit(0.35 + 0.55 * math.sin(t), -0.55 * math.cos(t), -0.75)
        k.add(f"finger{i}", Q.plume(wr + d * 0.01, d, 0.16 - 0.025 * abs(i - 1.5) / 1.5, 0.032, 0.03,
                                    across=np.cross(d, unit(-0.4, -1, 0)), root=0.7),
              BLUE if i % 2 == 0 else DEEP, "wing{s}", fuse=False, mirror=True, surface="hair")
    k.add("wrist", ell(wr, (0.032, 0.034, 0.03)), BLUE, "wing{s}", mirror=True)

    # Legs: thick haunches fused into the hips, long shins flaring into bell-bottoms
    k.add("thigh", tube([(0.055, 0.015, 0.44), (0.068, -0.02, 0.33), (0.072, -0.025, 0.28)], [0.07, 0.056, 0.046]),
          BLUE, "leg{s}", mirror=True)
    k.add("shin", tube([(0.072, -0.022, 0.29), (0.074, -0.008, 0.16), (0.075, 0.0, 0.07)], [0.043, 0.045, 0.058]),
          BLUE, "leg{s}", mirror=True)
    for z0 in (0.12, 0.2):
        def wave(P, z0=z0):
            ang = np.arctan2(P[:, 0] - np.sign(P[:, 0]) * 0.075, -P[:, 1])
            mid = z0 + 0.012 * (0.5 + 0.5 * tri(ang * 4 / (2 * math.pi)))
            return (inside(np.abs(P[:, 2] - mid) - 0.011, 0.0025) * inside(0.03 - np.abs(P[:, 0]))
                    * inside(np.abs(np.abs(P[:, 0]) - 0.075) - 0.075))
        k.paint(wave, SKY)
    # feet: three orange toes forward with red tips, one back
    for side in (1,):
        a = np.array([0.075, -0.01, 0.035])
        for i, ang in enumerate((-28, 0, 28)):
            t = math.radians(ang)
            d = np.array([math.sin(t), -math.cos(t), -0.25])
            k.add(f"toe{i}", tube([a, a + d * 0.06, a + d * 0.1], [0.022, 0.02, 0.018]), ORANGE, "leg{s}",
                  mirror=True, surface="skin")
            tip = a + d * 0.1
            k.add(f"nail{i}", ell(tip + d * 0.015, (0.019, 0.022, 0.016), rot=(0, 0, -ang)), TOE, "leg{s}",
                  fuse=False, mirror=True, surface="shell")
        k.add("toeback", tube([a, a + np.array([0.0, 0.055, -0.015])], [0.02, 0.016]), ORANGE, "leg{s}",
              mirror=True, surface="skin")
        k.add("anklecap", ell((0.075, -0.004, 0.05), (0.05, 0.05, 0.026)), ORANGE, "leg{s}", mirror=True,
              surface="skin")

    # The boa of water drops: from the lower back up to a rosette out to the right side, then a chain to the knee
    D = unit(0.86, 0.5, 0)                   # out to +X and a little back
    Z = np.array([0, 0, 1.0])
    N = np.cross(D, Z)                       # the drops' faces look along this
    base = np.array([0.02, 0.07, 0.47])
    def at(s, z):
        return base + D * s + Z * (z - base[2])
    k.add("boastem", tube([base, at(0.07, 0.53), at(0.14, 0.6)], [0.032, 0.028, 0.024]), BLUE, "body")
    def water_drop(name, c, d, ln, w):
        k.add(name + "rim", Q.drop(c, d, ln, w, 0.026, across=np.cross(d, N)), BLUE, "body", fuse=False, surface="hair")
        k.add(name, Q.drop(c + d * 0.006, d, ln * 0.78, w * 0.66, 0.034, across=np.cross(d, N)), SKY, "body",
              fuse=False, surface="hair")
    ros = at(0.15, 0.62)
    for i, a in enumerate((90, 30, 150, -20, 200)):
        t = math.radians(a)
        d = D * math.cos(t) + Z * math.sin(t)
        water_drop(f"rosette{i}", ros + d * 0.08, -d, 0.09, 0.038)
    chain = [(0.15, 0.5), (0.16, 0.415), (0.165, 0.33), (0.165, 0.245), (0.16, 0.16)]
    for i, (s, z) in enumerate(chain):
        d = unit(*(Z - D * 0.12))
        water_drop(f"bead{i}", at(s, z - 0.04), d, 0.1, 0.042)

    k.build(voxel=VOXEL, band=0.008, wband=0.06)

    E = Q.on_ell(H, HR, DE)
    look = Q.eye_open(iris=srgb("#5EC6EA"), pupil=srgb("#173A7A"), lid=srgb(WHITE), lid_line=(0.22, 0.15),
                      rim_w=0.2, pupil_c=(-0.15, -0.2), pupil_r=(0.38, 0.48), glint=(-0.35, 0.0, 0.18))
    k.eye_states(lambda state: Q.lay_eyes(k, state, E, DE, EYE_HALF, look, srgb(WHITE), INK))
    k.export(out)


if __name__ == "__main__":
    quaquaval(*args())
