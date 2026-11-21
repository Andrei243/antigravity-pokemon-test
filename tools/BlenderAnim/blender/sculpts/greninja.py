# Greninja sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/greninja.py -- <the export's Greninja.glb> <out.glb>
#
# Froakie and Frogadier grown (froakie.py, frogadier.py: the loose foam-white spots, digits with round pads, a lid that
# cuts the eye's top straight with the patch's own outline). From the references: a dark navy ninja frog crouched low
# on great folded thighs, chest forward, the head carried high. Its long pink tongue is wound round its neck and over
# its mouth like a scarf, one end trailing out and back to the side as a broad thick ribbon. Two navy horns rise from
# the crown in a V with a pale cyan plate between them, and a tall cream blade sweeps up from the side of the head on
# the scarf's side. Narrow fierce eyes, red in white. A cream bib down the chest under the scarf. Big white oval spots
# on the thighs and the upper arms. Long hands and feet of three digits with round navy pads, cream webs between.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, polygon, srgb, inside, unit, eye_look, args, sibling

fk = sibling("froakie")
NAVY, CREAM, CYAN, PINK, WHITE, RED, INK = ("#2E3C94", "#EDE2A6", "#8ED6F2", "#EE8CA0", "#E8ECF4", "#D8303C",
                                            "#1A1C26")


def web(k, name, base, tips, normal, col, bone, mirror=True, th=0.003):
    """A cream web between digits: a flat hull from their roots to most of the way to their tips."""
    base = np.array(base, float); n = np.array(normal, float); n /= np.linalg.norm(n)
    pts = [base]
    for t in tips:
        t = np.array(t, float)
        pts.append(base + (t - base) * 0.72)
    pts = np.array(pts)
    k.add(name, hull(np.vstack([pts + n * th, pts - n * th])), col, bone, fuse=False, mirror=mirror,
          surface="skin")


def splay(base, d, spread, length, n=3):
    """Where n digits from base along d, fanned across spread, end."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    sp = np.array(spread, float)
    return [np.array(base, float) + sp * f * 0.5 + (d + sp * f * 0.6) * length for f in np.linspace(-1, 1, n)]


def greninja(src, out):
    k = Kit(src, coat="scales")
    V = 0.0058

    # The whole figure is crouched: knees bent deep and spread, the trunk leaning forward some 25 degrees, the head
    # carried forward, the hands near the knees.
    LEAN = -0.53                     # the trunk's axis: forward (-y) by this much for each unit up

    # --- the trunk: chest forward over a pinched waist, the hips low between the thighs -----------------------------
    k.add("pelvis", ell((0, 0.04, 0.2), (0.08, 0.07, 0.062)), NAVY, "body")
    k.add("waist", ell((0, 0.0, 0.272), (0.058, 0.052, 0.062), rot=(-25, 0, 0)), NAVY, "body")
    k.add("chest", ell((0, -0.048, 0.358), (0.088, 0.068, 0.078), rot=(-25, 0, 0)), NAVY, "body")
    k.add("neck", ell((0, -0.088, 0.425), (0.046, 0.044, 0.042), rot=(-25, 0, 0)), NAVY, "body")
    k.add("shoulder", ell((0.075, -0.058, 0.4), (0.042, 0.044, 0.042)), NAVY, "arm{s}", mirror=True)
    # the cream bib down the chest's front, a V under the scarf
    def bib(P):
        z = P[:, 2]
        front = P[:, 1] - LEAN * (z - 0.272)
        return (inside(np.abs(P[:, 0]) - 0.056 * np.clip((z - 0.22) / 0.19, 0, 1), 0.003)
                * inside(front + 0.035, 0.006) * inside(z - 0.43, 0.004))
    k.paint(bib, CREAM, on=["chest", "waist", "neck"])

    # --- the head: navy, large, carried forward, a short snout above the scarf --------------------------------------
    HC = np.array([0.0, -0.11, 0.475])
    k.add("head", ell(HC, (0.084, 0.08, 0.07)), NAVY, "head")
    k.add("snout", ell((0, -0.163, 0.452), (0.06, 0.05, 0.038)), NAVY, "head")
    # the horns: two navy points rising from the crown in a V, laid back, a pale cyan plate filling the V
    T = np.array([0.0, -0.075, -0.075])           # the crest moved with the head
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"horn{side}", hull(np.array([(0.008, -0.05, 0.6), (0.046, -0.05, 0.598), (0.026, -0.076, 0.59),
                                            (0.026, -0.01, 0.612), (0.026, -0.04, 0.625),
                                            (0.092, 0.06, 0.705)]) * m + T), NAVY, "head", fuse=False)
    k.add("plate", hull(np.array([(-0.026, -0.05, 0.606), (0.026, -0.05, 0.606), (0.07, 0.026, 0.675),
                                  (-0.07, 0.026, 0.675), (-0.026, -0.036, 0.606), (0.026, -0.036, 0.606),
                                  (0.07, 0.04, 0.675), (-0.07, 0.04, 0.675)]) + T), CYAN, "head", fuse=False)
    # the tall cream blade sweeping up and out from the head's +X side (the scarf's side): a broad plate
    bl = np.array([(0.05, -0.045, 0.585), (0.088, -0.012, 0.57), (0.085, -0.05, 0.62), (0.168, 0.045, 0.7)]) + T
    off = 0.011 * unit(0.35, -1, 0.1)
    k.add("blade", hull(np.vstack([bl + off, bl - off])), CREAM, "head", fuse=False)

    # --- the tongue: wound once round the neck under the chin; one end trails back and out to +X, drooping -----------
    NC = np.array([0.0, -0.082, 0.418])
    ring = []
    for a in np.linspace(0, 2 * math.pi, 19)[:-1]:
        th = math.radians(50) + a                  # th = 0 at the back; starts at the back of the +X side
        x, y = 0.07 * math.sin(th), NC[1] + 0.066 * math.cos(th)
        z = NC[2] + 0.02 * math.cos(th)            # tilted with the lean: higher at the back, under the chin in front
        ring.append((x, y, z))
    ring.append(ring[0]); ring.append(ring[1])
    k.add("scarf", tube(ring, [0.022] * len(ring), seg=20), PINK, "head", fuse=False, surface="skin")
    r0 = np.array(ring[0])
    trail = [r0, r0 + (0.045, 0.03, -0.01), r0 + (0.09, 0.055, -0.035), r0 + (0.125, 0.07, -0.07),
             r0 + (0.15, 0.075, -0.105)]
    k.add("trail", blade(trail, [0.022, 0.026, 0.024, 0.018, 0.011], unit(-0.5, 1, 0), 0.55), PINK, "body",
          fuse=False, surface="skin")

    # --- arms: forward and down, the hands near the knees, three-fingered with round pads and cream webs ------------
    SH, EL, WR = np.array((0.078, -0.06, 0.405)), np.array((0.14, -0.075, 0.335)), np.array((0.12, -0.155, 0.255))
    k.add("upper", tube([SH, (0.115, -0.065, 0.38), EL], [0.03, 0.027, 0.024]), NAVY, "arm{s}", mirror=True)
    k.add("fore", tube([EL, (0.138, -0.12, 0.29), WR], [0.024, 0.021, 0.019]), NAVY, "arm{s}", mirror=True)
    HB = np.array((0.118, -0.172, 0.24))
    k.add("hand", ell(HB, (0.022, 0.024, 0.016)), NAVY, "arm{s}", mirror=True)
    fd, fs = (0.15, -0.75, -0.6), (0.06, 0, -0.01)
    fk.digits(k, "finger", HB, fd, fs, 0.06, 0.009, NAVY, "arm{s}", tip=NAVY, tip_r=0.0155)
    web(k, "palmweb", HB, splay(HB, fd, fs, 0.06), np.cross(unit(*fd), unit(*fs)), CREAM, "arm{s}")

    # --- legs: great thighs folded deep and spread, knees out and forward, short shins, long webbed feet -----------
    HIP, KN, AN = np.array((0.055, 0.03, 0.2)), np.array((0.19, -0.09, 0.15)), np.array((0.17, 0.0, 0.035))
    k.add("thigh", tube([HIP, (0.13, -0.02, 0.19), KN], [0.074, 0.068, 0.052]), NAVY, "leg{s}", mirror=True)
    k.add("shin", tube([KN, (0.192, -0.06, 0.085), AN], [0.045, 0.035, 0.027]), NAVY, "leg{s}", mirror=True)
    FB = np.array((0.172, -0.03, 0.022))
    k.add("foot", ell(FB + (0, 0.02, 0.004), (0.026, 0.04, 0.02)), NAVY, "leg{s}", mirror=True)
    td, ts = (0.3, -1.0, -0.08), (0.075, 0, 0)
    fk.digits(k, "toe", FB, td, ts, 0.085, 0.01, NAVY, "leg{s}", tip=NAVY, tip_r=0.017)
    web(k, "footweb", FB, splay(FB, td, ts, 0.085), (0, 0, 1), CREAM, "leg{s}")

    # the big white oval spots: on the front of each knee and on each upper arm
    k.decal((0.2, -0.135, 0.17), unit(0.4, -1, 0.4), (0.032, 0.026), WHITE, eps=0.002, rings=12, segs=48,
            mirror=True)
    k.decal((0.118, -0.088, 0.385), unit(0.5, -1, 0.35), (0.018, 0.015), WHITE, eps=0.002, rings=12, segs=48,
            mirror=True)

    k.build(voxel=V, smooth=4, band=0.008, wband=0.04)

    # --- the eyes: narrow and fierce, red in white, the top cut slanting down to the nose ---------------------------
    CW, CR, CK, CN = srgb(WHITE), srgb(RED), srgb(INK), srgb(NAVY)
    cut = polygon([(-1.3, -1.3), (1.3, -1.3), (1.3, 0.7), (-1.3, 0.0)])
    def almond(th):
        return min(1.0, cut(th))
    def eye(u, v):
        c = np.tile(CW, (len(u), 1))
        c[((u + 0.1) / 0.62) ** 2 + ((v + 0.05) / 0.85) ** 2 < 1] = CR
        c[((u + 0.1) / 0.26) ** 2 + ((v + 0.05) / 0.4) ** 2 < 1] = CK
        return c
    E = np.array([0.038, -0.18, 0.49])
    de = unit(0.4, -1, 0.15)
    def eyes(state):
        k.decal(E, de, (0.027, 0.02), eye_look(state, eye, CN, CK, line=0.15), eps=0.002, rings=20, segs=56,
                mirror=True, outline=almond)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    greninja(*args())
