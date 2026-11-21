# Eelektross sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/eelektross.py -- <the export's Eelektross.glb> <out.glb>
#
# From the references (both lie it down in a long S; the skeleton stands it up on its coiled tail, which decides the
# pose): a great dark blue eel, its body rising from a coil on the ground; a big head carried forward, drawn out
# behind into a long tapering point; on the head's front and underside a huge sucker of a mouth, a thick red rim with
# big white fangs round it; the eyes red with a black pupil, each in a yellow patch that runs back along the head;
# two small yellow spots on the head's top behind them; two short thick arms held forward, each ending in a round
# sucker of a hand, a red rim round a yellow middle, with white claws on its rim, a yellow spot on the arm's outside;
# a cream belly up the front of the body; long cream fins hanging back from under the head's point, cream fins along
# the coil and a dark spiky fin at the tail's end, with a yellow spot on each side of the tail.
# Shapes named from the pictures before building: the red band with four fangs under the eye is the mouth seen from
# its side; the round red-and-yellow end with claws at the lower left is a hand (the other hand is the clawed lobe at
# the lower right, its yellow spot on its back); the long point at the upper right is the back of the head, the cream
# fin under it hangs from it.
# Grown from eelektrik.py (by sibling): its colours, the body's Line, the spots and the ribbed paddles.
# The skeleton: body (0, 0, 0.27), head (0, -0.08, 0.43), armL/R (+-0.07, -0.05, 0.33), tail (-0.04, 0.36, 0.23).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, lathe, axes, srgb, inside, unit, eye_look, args, outward, sibling

ek = sibling("eelektrik")
dr = ek.dr
NAVY, CREAM, CREAM2, RED, RED2, YELLOW, WHITE, INK = (ek.NAVY, ek.CREAM, ek.CREAM2, ek.RED, ek.RED2, ek.YELLOW,
                                                      ek.WHITE, ek.INK)
MAW, GREY, IRIS = "#5A2232", "#5C7290", "#D83A3E"

# The body's line, the neck first: down the front, round the coil on the ground, up behind to the tail's end
LINE = [(0.0, -0.075, 0.45), (0.0, -0.04, 0.35), (0.0, -0.005, 0.25), (0.0, 0.035, 0.155), (0.0, 0.11, 0.095),
        (-0.005, 0.2, 0.09), (-0.02, 0.285, 0.13), (-0.035, 0.335, 0.2), (-0.045, 0.36, 0.27)]
RADS = [0.084, 0.092, 0.095, 0.09, 0.08, 0.068, 0.056, 0.045, 0.034]
BONES = ["head", "body", "body", "body", "tail", "tail", "tail", "tail"]

HC = np.array([0.0, -0.115, 0.485])          # the head
HR = np.array([0.104, 0.12, 0.094])


def on(c, r, d):
    d = unit(*d)
    return c + d * r, d


def fang(root, tip, w, across):
    """A fang or claw: a solid kite from a broad root to a point."""
    root, tip = np.array(root, float), np.array(tip, float)
    d = tip - root; d /= np.linalg.norm(d)
    a = np.array(across, float); a = a - d * (a @ d); a /= np.linalg.norm(a)
    b = np.cross(d, a)
    return hull([root + w * a, root - w * a, root + 0.65 * w * b, root - 0.65 * w * b, tip])


def sucker(k, name, c, d, r_in, r_out, depth, bone, inner, fangs, fang_len, fang_w, roll=0.0):
    """A sucker: a thick red rim (a loose ring of turning) standing on the surface at c facing d, its middle a patch
    of `inner`, and white fangs on the rim leaning in."""
    d = unit(*d)
    prof = [(r_in, -depth), (r_in, depth * 0.6), (r_in + 0.006, depth), (r_out - 0.008, depth), (r_out, depth * 0.4),
            (r_out, -depth), (r_in, -depth)]
    k.add(name, lathe(c, d, prof, seg=48), RED, bone, fuse=False, surface="scales")
    k.decal(c, d, (r_in * 1.05, r_in * 1.05), inner, eps=0.004, rings=12, segs=48)
    a, b = axes(d)
    for i in range(fangs):
        t = roll + 2 * math.pi * i / fangs
        rad = math.cos(t) * a + math.sin(t) * b
        root = c + rad * (r_in + 0.6 * (r_out - r_in)) + d * depth * 0.6
        tip = c + rad * (r_in * 0.55) + d * (depth + fang_len)
        k.add(f"{name}fang{i}", fang(root, tip, fang_w, np.cross(d, rad)), WHITE, bone, fuse=False, surface="shell")


def eelektross(src, out):
    k = Kit(src, coat="scales")
    line = dr.Line(LINE, RADS, rise=(0.13, 0.2))
    line.tubes(k, BONES, NAVY)
    k.paint(dr.belly(line, 0.3, 5.5, ground=0.2, risen=-0.05, rise=(0.13, 0.2), edge=0.004), CREAM, on=["seg"])

    # --- the head: a big skull carried forward, drawn out behind into a long point -----------------------------------
    k.add("skull", ell(HC, HR, rot=(-8, 0, 0)), NAVY, "head")
    k.add("crown", ell(HC + (0, 0.07, 0.02), (0.088, 0.1, 0.08)), NAVY, "head")
    k.add("point", tube([(0, -0.02, 0.52), (0, 0.07, 0.535), (0, 0.145, 0.545), (0, 0.205, 0.552)],
                        [0.072, 0.05, 0.03, 0.018]), NAVY, "head")
    k.add("neck", tube([LINE[0], (0, -0.09, 0.47)], [0.084, 0.09]), NAVY, "head")

    # the yellow patch round each eye, drawn back along the head, and the two spots on its top (in every state)
    for s in (1, -1):
        c, n = on(HC, HR, (s * 0.62, -0.62, 0.48))
        k.decal(c, n, (0.04, 0.036), YELLOW, eps=0.003, rings=12, segs=48)
        c2, n2 = on(HC + (0, 0.035, 0.005), HR, (s * 0.8, -0.2, 0.56))
        k.decal(c2, n2, (0.034, 0.026), YELLOW, eps=0.003, rings=10, segs=44)
        for dy, rr in ((0.06, 0.017), (0.115, 0.014)):
            c3, n3 = on(HC + (0, dy, 0.03), HR * (1, 0.9, 1), (s * 0.3, 0.0, 0.95))
            k.decal(c3, n3, (rr, rr), YELLOW, eps=0.003, rings=8, segs=36)

    # --- the mouth: a huge sucker on the head's front and underside, fangs round its rim -------------------------------
    mc, md = on(HC, HR, (0, -0.72, -0.62))
    sucker(k, "mouth", mc + md * 0.002, md, 0.048, 0.076, 0.014, "head", MAW, 4, 0.03, 0.012, roll=math.pi / 4)

    # --- the arms: short and thick, forward and a little down, each a sucker of a hand with claws ------------------
    for s, side in ((1, "R"), (-1, "L")):
        sh = np.array([s * 0.07, -0.05, 0.33])
        el = np.array([s * 0.13, -0.12, 0.285])
        hd = np.array([s * 0.145, -0.18, 0.25])
        k.add("arm" + side, tube([sh, el, hd], [0.048, 0.042, 0.044]), NAVY, "arm" + side)
        hand_d = unit(s * 0.25, -0.85, -0.45)
        k.add("hand" + side, ell(hd + hand_d * 0.012, (0.05, 0.05, 0.05)), NAVY, "arm" + side)
        hc = hd + hand_d * 0.06
        palm = np.array(srgb(YELLOW))

        def ring(u, v, Y=palm, R=srgb(RED)):
            c = np.tile(R, (len(u), 1))
            c[u * u + v * v < 0.55 ** 2] = Y
            return c
        k.decal(hc, hand_d, (0.043, 0.043), ring, eps=0.004, rings=14, segs=48)
        a, b = axes(hand_d)
        for i, t in enumerate((math.radians(60), math.radians(180), math.radians(300))):
            rad = math.cos(t) * a + math.sin(t) * b
            root = hc + rad * 0.036 - hand_d * 0.01
            tip = root + rad * 0.022 + hand_d * 0.034
            k.add(f"claw{side}{i}", fang(root, tip, 0.012, np.cross(hand_d, rad)), WHITE, "arm" + side,
                  fuse=False, surface="shell")
        # the yellow spot on the arm's outside
        el_out = el + np.array([s * 0.042, 0.004, 0.006])
        k.decal(el_out, unit(s, -0.1, 0.25), (0.022, 0.022), YELLOW, eps=0.003, rings=10, segs=40)

    # --- yellow spots on the tail's sides ------------------------------------------------------------------------------
    for s in (1, -1):
        for sv, r in ((5.4, 0.024), (6.4, 0.017)):
            p, t, rad = line.at(sv)
            n = unit(s, 0.0, 0.45)
            k.decal(p + n * rad, n, (r, r), YELLOW, eps=0.003, rings=10, segs=40)

    # --- fins: long cream fins hanging back from under the head's point, cream fins on the coil, a dark spiky fin at
    # the tail's end --------------------------------------------------------------------------------------------------
    for s, side in ((1, "R"), (-1, "L")):
        ek.paddle(k, "headfin" + side, (s * 0.06, 0.02, 0.48), (s * 0.75, 0.75, -0.35), (0, 0.2, 1.0), 0.18, 0.08,
                  [CREAM, CREAM2], "head", thick=0.018, ribs=4)
        ek.paddle(k, "coilfin" + side, (s * 0.06, 0.16, 0.1), (s * 0.85, 0.5, 0.1), (0, -0.3, 1.0), 0.12, 0.07,
                  [CREAM, CREAM2], "tail", thick=0.016, ribs=3)
    p, t, rad = line.at(len(LINE) - 1.0)
    spikes = [(0.0, -0.03), (0.06, -0.035), (0.1, -0.06), (0.11, -0.02), (0.15, -0.025), (0.13, 0.01), (0.17, 0.03),
              (0.1, 0.035), (0.04, 0.03), (0.0, 0.025)]
    k.add("tailfin", slab(spikes, p - t * 0.02, unit(0.35, 0.55, 0.75), unit(0.85, -0.3, -0.4),
                          0.02), GREY, "tail", fuse=False, surface="scales")

    k.build(voxel=0.005, smooth=5, band=0.006)

    # --- the eyes: red, a black pupil and a dark rim, a glint, in the yellow patch -----------------------------------
    R, K, W, Y = srgb(IRIS), srgb(INK), srgb("#FFFFFF"), srgb(YELLOW)

    def eye(u, v):
        c = np.tile(R, (len(u), 1))
        r2 = u * u + v * v
        c[r2 > 0.84 ** 2] = K
        c[(u + 0.04) ** 2 + v * v < 0.38 ** 2] = K
        c[(u - 0.3) ** 2 + (v - 0.3) ** 2 < 0.17 ** 2] = W
        return c

    def eyes(state):
        look = eye_look(state, eye, Y, K, line=0.17)
        for s in (1, -1):
            lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
            c, n = on(HC, HR, (s * 0.62, -0.62, 0.48))
            k.decal(c, n, (0.026, 0.026), lk, eps=0.0045, rings=20, segs=56)

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    eelektross(*args())
