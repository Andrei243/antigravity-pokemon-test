# Quaxwell sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/quaxwell.py -- <the export's Quaxwell.glb> <out.glb>
# Quaxly grown into a young dancer (built by quaxly.py's functions: its white, eyes and feathers): a great dome of blue
# hair swept back over the head and down behind it, a sky-blue visor of feathers along its front edge pointing down
# between the eyes; heavy-lidded blue eyes under it, a small pointed yellow beak; a white face and neck; a blue
# leotard over the chest with a white neckline and a scalloped sky-blue hem; great round white hips with a ruffle of
# blue feathers behind; white arms bent forward with cuffs of blue feathers for hands; long white legs on yellow feet
# with a yellow cuff and a blue band at the ankle.
# The pictures overrule the brief: the hair is a dome over the head, not a pompadour standing forward, and the hands
# are tufts of blue feathers, not cuffs on a hand.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, ring_band, srgb, inside, smoothstep, tri, unit, args, sibling

Q = sibling("quaxly")
WHITE, BEAK, INK = Q.WHITE, Q.BEAK, Q.INK
BLUE, DEEP, SKY, YELLOW = "#2F6FD8", "#2458B8", "#62CBE8", "#F7D53C"
VOXEL = 0.0065

H = np.array([0.0, -0.045, 0.54])            # the face's centre, under the dome
HR = np.array([0.064, 0.062, 0.064])
DOME = np.array([0.0, 0.022, 0.632])
DOMER = np.array([0.118, 0.135, 0.118])
DE = unit(0.46, -0.88, 0.02)
EYE_HALF = (0.021, 0.019)


def quaxwell(src, out):
    k = Kit(src, coat="fur")

    # The trunk: a slim chest under the leotard, great round white hips
    k.add("torso", ell((0, -0.006, 0.39), (0.07, 0.058, 0.075)), WHITE, "body")
    k.add("hips", ell((0, 0.012, 0.285), (0.112, 0.104, 0.092)), WHITE, "body")
    k.add("rump", ell((0, 0.05, 0.27), (0.096, 0.08, 0.08)), WHITE, "body")
    k.add("neck", tube([(0, -0.01, 0.43), (0, -0.03, 0.5)], [0.044, 0.04]), WHITE, "head")

    # The leotard: blue over the chest, a white neckline, a scalloped sky-blue hem
    def ang(P):
        return np.arctan2(P[:, 0], -P[:, 1])
    def hem(P):
        return 0.338 - 0.012 * (0.5 + 0.5 * tri(ang(P) * 5 / (2 * math.pi)))
    reach = lambda P: inside(np.sqrt(P[:, 0] ** 2 + (P[:, 1] + 0.006) ** 2) - 0.11)
    neckline = lambda P: inside(np.abs(P[:, 0]) - 0.028 - 0.5 * np.clip(P[:, 2] - 0.42, 0, 1)) * inside(P[:, 1] + 0.02)
    k.paint(lambda P: reach(P) * inside(hem(P) - P[:, 2], 0.0025) * inside(P[:, 2] - 0.452, 0.003)
            * (1 - neckline(P) * smoothstep(0.415, 0.42, P[:, 2])), SKY)
    k.paint(lambda P: reach(P) * inside(0.352 - P[:, 2], 0.0025) * inside(P[:, 2] - 0.452, 0.003)
            * (1 - neckline(P) * smoothstep(0.415, 0.42, P[:, 2])), BLUE)

    # The head: a small white face under a great dome of blue hair swept back and down behind
    k.add("face", ell(H, HR), WHITE, "head")
    k.add("dome", ell(DOME, DOMER, rot=(-8, 0, 0)), BLUE, "head", surface="hair")
    k.add("domeback", ell((0, 0.055, 0.575), (0.112, 0.1, 0.1)), BLUE, "head", surface="hair")
    # the sky-blue visor along the dome's front edge, coming to a point between the eyes
    def visor(P):
        x = np.abs(P[:, 0])
        low = np.minimum(0.515 + 1.3 * x, 0.553 + 0.1 * x)
        top = 0.618 - 0.3 * x
        return inside(low - P[:, 2], 0.002) * inside(P[:, 2] - top, 0.002) * inside(P[:, 1] + 0.035 - 0.5 * x, 0.004)
    k.paint(visor, SKY)

    # Arms bent forward and down, cuffs of blue feathers for hands
    sh, el, wr = np.array([0.07, 0.0, 0.415]), np.array([0.112, 0.0, 0.345]), np.array([0.1, -0.08, 0.31])
    k.add("arm", tube([sh, el, wr], [0.03, 0.026, 0.024]), WHITE, "wing{s}", mirror=True)
    k.add("cuff", ell(wr, (0.03, 0.032, 0.03)), BLUE, "wing{s}", mirror=True, surface="hair")
    for i, (dx, dy) in enumerate(((0.55, -0.35), (0.25, -0.6), (-0.1, -0.7), (0.6, 0.05))):
        d = unit(dx, dy - 0.2, -0.75)
        k.add(f"hand{i}", Q.plume(wr + d * 0.01, d, 0.085 - 0.012 * (i == 3), 0.026, 0.018,
                                  across=np.cross(d, unit(-0.3, -1, 0))),
              DEEP if i % 2 else BLUE, "wing{s}", fuse=False, mirror=True, surface="hair")

    # A ruffle of blue feathers behind the hips, sky-blue between
    for i, a in enumerate((-55, -25, 5, 35)):
        t = math.radians(a)
        d = unit(0.75 * math.sin(t) + 0.55, 0.65 * math.cos(t) + 0.3, -0.35)
        base = np.array([0.06, 0.07, 0.31]) + 0.02 * np.array([math.sin(t), 0, math.cos(t)])
        k.add(f"tail{i}", Q.plume(base, d, 0.1, 0.03, 0.02, across=np.cross(d, (0, 0, 1.0)) + np.array([0, 0, 0.6])),
              BLUE, "body", fuse=False, mirror=True, surface="hair")
        if i < 3:
            d2 = unit(d[0] - 0.15, d[1], d[2] + 0.25)
            k.add(f"tailsky{i}", Q.plume(base + np.array([0, -0.004, 0.01]), d2, 0.07, 0.022, 0.016,
                                         across=np.cross(d2, (0, 0, 1.0)) + np.array([0, 0, 0.6])),
                  SKY, "body", fuse=False, mirror=True, surface="hair")

    # Long white legs fused into the hips, yellow feet with a cuff and a blue band at the ankle
    k.add("thigh", tube([(0.055, 0.005, 0.27), (0.058, 0.0, 0.17)], [0.056, 0.038]), WHITE, "leg{s}", mirror=True)
    k.add("shin", tube([(0.058, 0.0, 0.18), (0.058, 0.0, 0.085)], [0.036, 0.029]), WHITE, "leg{s}", mirror=True)
    k.add("bootcuff", ell((0.058, -0.002, 0.08), (0.036, 0.036, 0.022)), YELLOW, "leg{s}", mirror=True,
          surface="skin")
    k.add("ankle", tube([(0.058, -0.002, 0.07), (0.058, -0.008, 0.035)], [0.027, 0.03]), YELLOW, "leg{s}",
          mirror=True, surface="skin")
    k.add("foot", ell((0.062, -0.045, 0.018), (0.042, 0.066, 0.018), rot=(0, 0, 10), deform=Q.webbed), YELLOW,
          "leg{s}", mirror=True, surface="skin")
    k.add("band", ring_band((0.058, -0.004, 0.052), (0, 0, 1), 0.0275, 0.014, thick=0.007), BLUE, "leg{s}",
          fuse=False, mirror=True)

    # The beak: small, pointed, a little down
    k.add("beak", ell((0, -0.12, 0.508), (0.026, 0.042, 0.02), rot=(22, 0, 0), deform=Q.pointed_beak), BEAK,
          "head", fuse=False, surface="shell")

    k.build(voxel=VOXEL, band=0.006, wband=0.05)

    E = Q.on_ell(H, HR, DE)
    look = Q.eye_open(lid=srgb(WHITE), lid_line=(0.28, 0.12), rim_w=0.2, pupil_c=(-0.1, -0.15), pupil_r=(0.4, 0.5),
                      glint=(-0.3, 0.05, 0.2))
    k.eye_states(lambda state: Q.lay_eyes(k, state, E, DE, EYE_HALF, look, srgb(WHITE), INK))
    k.export(out)


if __name__ == "__main__":
    quaxwell(*args())
