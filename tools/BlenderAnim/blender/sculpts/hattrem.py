# Hattrem sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/hattrem.py -- <the export's Hattrem.glb> <out.glb>
# Grown from hatenna.py: its colours, its on_ell and white spots, its pink hat with a blue tip grown into a broken one.
#
# The design, from the references: a great ball of pale blue hair, white spots on it, with an arched hollow at its front
# and bottom out of which a small pink face looks: big glossy black eyes with white glints, a little smile; two small
# pink arms raised beside the cheeks. Under the face a little pink tiered dress with a white point at its collar, and
# two thin blue legs. A blue curl spirals on the brow above the hollow. From the ball's lower sides two short braids
# of hair, each through a pink cuff, end in great blue mitts with three fingers, resting on the ground. On the crown's
# +X side a pink hat band under a white cone whose tip is bent in a zigzag. The skeleton is a body, a head, two arms
# (the braids) and two legs.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, outward, sibling, args

h = sibling("hatenna")
PINK, PINKDEEP, HATPINK, BLUE, BLUEDEEP, BLUELIGHT, WHITE, INKH = (h.PINK, h.PINKDEEP, h.HATPINK, h.BLUE, h.BLUEDEEP,
                                                                    h.BLUELIGHT, h.WHITE, h.INKH)
on_ell, spot = h.on_ell, h.spot
SHADE = "#86A9D0"                          # the hollow's inside

# --- the frame (joints: body (0, 0, 0.22), head (0, 0, 0.2), arms (+-0.14, -0.04, 0.18), legs (+-0.03, -0.03, 0.05))
HC = np.array([0.0, 0.02, 0.335])           # the ball of hair
HR = np.array([0.26, 0.215, 0.225])
FC = np.array([0.0, -0.122, 0.278])         # the face in its hollow
FR = np.array([0.075, 0.055, 0.082])


def hollow(V):
    """The ball's front pushed in where the face looks out: an arch from just over the face down to the bottom."""
    V = V.copy()
    m = inside((V[:, 0] / 0.44) ** 2 + (np.clip(V[:, 2] - (-0.2), 0, None) / 0.42) ** 2 - 1, 0.35)
    m = m * smoothstep(0.0, -0.35, V[:, 1])
    return V * (1 - 0.32 * m)[:, None]


def hattrem(src, out):
    k = Kit(src, coat="fur")

    # --- the ball of hair with its hollow, the hollow's inside a shade deeper
    k.add("hair", ell(HC, HR, deform=hollow, sub=5), BLUE, "head")
    def deep(P):
        Q = (P - HC) / HR
        r = np.linalg.norm(Q, axis=1)
        return inside(r - 0.9, 0.03) * inside(Q[:, 1] + 0.2, 0.05)
    k.paint(deep, SHADE, on=["hair"])

    # --- the face, the dress under it and the little legs
    k.add("face", ell(FC, FR), PINK, "head")
    k.add("collar", ell((0, -0.112, 0.2), (0.02, 0.017, 0.02)), WHITE, "body")
    k.add("tier1", ell((0, -0.106, 0.168), (0.042, 0.038, 0.03)), PINK, "body")
    k.add("tier2", ell((0, -0.104, 0.128), (0.052, 0.045, 0.034)), PINK, "body")
    k.add("tier3", ell((0, -0.102, 0.092), (0.03, 0.03, 0.026)), PINK, "body")
    k.add("leg", tube([(0.016, -0.1, 0.085), (0.018, -0.108, 0.04), (0.017, -0.112, 0.013)], [0.011, 0.0105, 0.009]),
          BLUE, "leg{s}", mirror=True)
    # the little arms raised beside the cheeks
    k.add("arm", tube([(0.03, -0.12, 0.2), (0.078, -0.155, 0.215), (0.092, -0.172, 0.252)], [0.012, 0.011, 0.011]),
          PINK, "body", mirror=True)
    k.add("hand", ell((0.094, -0.176, 0.268), (0.017, 0.015, 0.02)), PINK, "body", mirror=True)

    # --- the braids: down from the ball's lower sides through a pink cuff to a great three-fingered mitt
    k.add("braid", tube([(0.17, -0.03, 0.17), (0.195, -0.055, 0.12), (0.212, -0.08, 0.088)], [0.046, 0.04, 0.034]),
          BLUE, "arm{s}", mirror=True)
    k.add("cuff", ell((0.215, -0.085, 0.08), (0.044, 0.044, 0.02), rot=(-20, 10, 0)), PINK, "arm{s}", mirror=True)
    d = unit(0.55, -0.83, 0)
    mc = np.array([0.24, -0.15, 0.052])
    a = math.degrees(math.atan2(d[1], d[0]))
    k.add("mitt", ell(mc, (0.084, 0.066, 0.051), rot=(0, 0, a)), BLUE, "arm{s}", mirror=True)
    side = np.array([-d[1], d[0], 0])
    for i, t in enumerate((-1, 0, 1)):
        fd = unit(*(d + side * 0.45 * t))
        fa = math.degrees(math.atan2(fd[1], fd[0]))
        k.add(f"finger{i}", ell(mc + d * 0.072 + side * 0.04 * t + (0, 0, -0.012 + 0.004 * (1 - abs(t))),
                                (0.044, 0.022, 0.024), rot=(0, 0, fa)), BLUE, "arm{s}", mirror=True)

    # --- the hat on the crown's +X side: a pink band, a white cone, its tip bent in a zigzag
    hp, hn = on_ell(HC, HR, (0.42, 0.12, 0.9))
    hd = unit(0.4, 0.08, 0.91)
    b0 = hp - hd * 0.03
    k.add("hatband", tube([b0, b0 + hd * 0.06, b0 + hd * 0.085], [0.054, 0.05, 0.048]), HATPINK, "head")
    c0 = b0 + hd * 0.085
    k.add("hatcone", tube([c0, c0 + hd * 0.05, c0 + hd * 0.095], [0.046, 0.03, 0.014]), WHITE, "head")
    q0 = c0 + hd * 0.09
    q1 = q0 + unit(0.85, 0.0, 0.5) * 0.035
    q2 = q1 + unit(0.0, 0.0, 1) * 0.03
    q3 = q2 + unit(0.9, 0.0, -0.4) * 0.034
    k.add("hattip", blade([q0, q1, q2, q3], [0.015, 0.013, 0.012, 0.005], (0, 1, 0), 0.75), WHITE, "head")
    def ring(P):                              # a deeper pink line round the band's top
        t = (P - b0) @ hd
        return inside(np.abs(t - 0.07) - 0.006, 0.002) * inside(np.linalg.norm(P - b0, axis=1) - 0.12)
    k.paint(ring, PINKDEEP, on=["hatband"])

    # --- the curl on the brow: a blue spiral lying on the ball over the hollow
    cp, cn = on_ell(HC, HR, (0.06, -0.62, 0.78))
    e1 = np.cross(cn, (0, 0, 1.0)); e1 /= np.linalg.norm(e1)
    e2 = np.cross(cn, e1)
    pts, rad = [], []
    for t in np.linspace(0, 1, 26):
        ang = 0.3 + t * 2.3 * math.pi
        rr = 0.052 * (1 - 0.78 * t)
        q = cp + rr * (math.cos(ang) * e1 + math.sin(ang) * e2)
        q, qn = on_ell(HC, HR, q - HC)
        pts.append(q + qn * 0.01)
        rad.append(0.0125 - 0.004 * t)
    k.add("curl", outward(tube(pts, rad, seg=14, per=4)), BLUELIGHT, "head", fuse=False)
    # the curl's tail coming down to the hollow's top edge
    k.add("curltail", ell(cp + e2 * -0.05 + cn * -0.006, (0.03, 0.02, 0.03)), BLUE, "head")

    # --- white spots over the hair
    for d_, s in (((0.62, -0.45, 0.64), 0.04), ((-0.68, -0.35, 0.6), 0.034), ((0.9, -0.25, 0.15), 0.03),
                  ((-0.2, 0.35, 0.92), 0.036), ((-0.7, 0.45, 0.45), 0.04), ((0.75, 0.55, -0.05), 0.032),
                  ((-0.9, 0.1, -0.15), 0.028)):
        spot(k, HC, HR, d_, s)

    # --- a small smile
    I = srgb(INKH); L = srgb(PINK)
    def smile(u, v):
        c = np.tile(L, (len(u), 1))
        c[(np.abs(v - (-0.35 + 0.75 * u * u)) < 0.3) & (np.abs(u) < 0.85)] = I
        return c
    mp, mn = on_ell(FC, FR, (0, -1, -0.42))
    k.decal(mp, mn, (0.012, 0.007), smile, eps=0.002, rings=12, segs=48)

    k.build(voxel=0.006, smooth=4, band=0.007, wband=0.04)

    # --- the eyes: big glossy black ovals with a large glint high and outward and a small one low
    K, W = srgb("#141018"), srgb("#FFFFFF")
    def eye(u, v):
        c = np.tile(L, (len(u), 1))
        c[u * u + v * v < 0.92] = K
        c[((u - 0.28) / 0.3) ** 2 + ((v - 0.34) / 0.3) ** 2 < 1] = W
        c[((u + 0.3) / 0.14) ** 2 + ((v + 0.38) / 0.14) ** 2 < 1] = W
        return c
    ep, en = on_ell(FC, FR, (0.4, -0.88, 0.14))
    def lay(state):
        k.decal(ep, en, (0.02, 0.025), eye_look(state, eye, L, I, line=0.15), eps=0.0018, rings=20, segs=56,
                mirror=True)

    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    hattrem(*args())
