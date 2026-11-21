# Cinccino sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cinccino.py -- <the export's Cinccino.glb> <out.glb>
# Minccino grown (minccino.py, whose ears, eyes and smile it borrows). From the references: the same grey chinchilla,
# taller and slimmer in the body, its ears smaller against the head and less spread (some 35 degrees), grey to the
# tips, pink inside; little fronds of fur at the sides of the head; a white forelock up from the crown; and the long
# white fur that wraps it: a soft roll round the neck held at the chest by both little arms, two great lobes rising
# behind the head to the ears' tips like a heart, and two long streamers sweeping out from the shoulders and down to
# either side, their ends curled up off the ground.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, args, sibling

m_ = sibling("minccino")

GREY, WHITE, PINK, SCARF = m_.GREY, m_.WHITE, m_.PINK, "#F6F6FA"
EYE, EYELOW, INK = m_.EYE, m_.EYELOW, m_.INK

HC = np.array([0.0, -0.01, 0.262])
HR = np.array([0.088, 0.077, 0.076])


def cinccino(src, out):
    k = Kit(src, coat="fur")

    # The round head on a slim small body
    k.add("head", ell(HC, HR), GREY, "head")
    k.add("cheek", ell(HC + (0.034, -0.022, -0.03), (0.048, 0.048, 0.043)), GREY, "head", mirror=True)
    k.add("body", ell((0, 0.002, 0.11), (0.05, 0.046, 0.078)), GREY, "body")
    k.add("neck", ell((0, 0.0, 0.19), (0.045, 0.042, 0.04)), GREY, "body")

    # The ears, smaller and less spread, grey to the tips
    m_.ear(k, (0.045, 0.002, 0.305), 34, 0.16, 0.054, 0.018, "ear{s}", tip=GREY)

    # Little fronds of fur at the sides of the head, under the ears
    for i, (dz, a) in enumerate(((0.012, 25), (-0.008, 0), (-0.026, -25))):
        r = np.array([0.08, -0.004, HC[2] + dz])
        d = np.array([math.cos(math.radians(a)), 0.15, math.sin(math.radians(a))])
        k.add(f"frond{i}", blade([r - d * 0.006, r + d * 0.018, r + d * 0.03], [0.011, 0.008, 0.003], (0, 1, 0), 0.55),
              GREY, "head", mirror=True)

    # The white forelock up from the crown
    k.add("forelock", blade([(0.0, -0.03, 0.315), (0.0, -0.022, 0.345), (0.004, -0.005, 0.37), (0.012, 0.012, 0.382)],
                            [0.026, 0.022, 0.015, 0.007], (0, 1, 0), 0.55), SCARF, "head")

    # The soft white roll round the neck, low in front, and the little arms holding it at the chest
    ring = []
    for i in range(13):
        t = 2 * math.pi * i / 12 + math.pi / 2           # start and end behind
        x, y = 0.058 * math.cos(t), -0.052 * math.sin(t)
        z = 0.19 - 0.022 * max(0.0, math.sin(t)) + 0.01 * math.cos(t)
        ring.append((x, y, z))
    k.add("roll", tube(ring, [0.024] * 13, seg=20), SCARF, "body")
    k.add("arm", tube([(0.04, -0.02, 0.2), (0.05, -0.05, 0.172), (0.03, -0.072, 0.16)], [0.017, 0.016, 0.015]),
          GREY, "arm{s}", mirror=True)
    k.add("paw", ell((0.026, -0.078, 0.158), (0.017, 0.015, 0.016)), GREY, "arm{s}", mirror=True)

    # Two great lobes rising behind the head to the ears' tips, a heart from behind
    lobe = [(0.02, 0.05, 0.2), (0.07, 0.07, 0.29), (0.115, 0.078, 0.37), (0.108, 0.072, 0.425),
            (0.062, 0.062, 0.418), (0.024, 0.052, 0.38)]
    k.add("lobe", blade(lobe, [0.03, 0.04, 0.044, 0.038, 0.032, 0.024], (0, 1, 0), 0.68), SCARF, "body", mirror=True)

    # Two long streamers from the shoulders, out and down to either side, the ends curled up
    stream = [(0.05, 0.04, 0.21), (0.12, 0.05, 0.19), (0.172, 0.03, 0.12), (0.19, 0.0, 0.052), (0.205, -0.03, 0.025),
              (0.228, -0.045, 0.048)]
    k.add("stream", blade(stream, [0.03, 0.034, 0.03, 0.025, 0.016, 0.007], unit(0.3, 1, 0), 0.62), SCARF, "body",
          mirror=True)

    # Short legs and little feet, a little white tail behind
    k.add("thigh", ell((0.028, -0.006, 0.05), (0.027, 0.027, 0.03)), GREY, "leg{s}", mirror=True)
    k.add("foot", ell((0.03, -0.026, 0.011), (0.02, 0.03, 0.011)), GREY, "leg{s}", mirror=True)
    k.add("tail", ell((0, 0.06, 0.07), (0.035, 0.035, 0.04)), SCARF, "tail")

    # The nose and the smile
    m_.face(k, HC, HR, GREY, (0, -0.077, -0.022), (0, -0.075, -0.035), size=0.95)

    # Big oval dark eyes
    de = unit(0.47, -0.87, -0.07)
    E = HC + de / np.linalg.norm(de / HR)
    look = m_.oval_eye(EYE, EYELOW, WHITE)
    def eyes(state):
        m_.lay_eyes(k, state, E, de, (0.02, 0.027), look, GREY, INK)

    k.build(voxel=0.0036, band=0.005)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    cinccino(*args())
