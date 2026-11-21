# Wishiwashi (Solo Form) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/wishiwashi.py -- <the export's Wishiwashi.glb> <out.glb>
#
# From the references: a tiny sardine, some 0.1 tall. A big round white head with a round open mouth at its front
# (dark red, a pink tongue in its floor); one great teary eye on each side of the head: a white disc ringed grey, its
# upper part blue with drips hanging down and a pale cyan splash of a pupil; a blue back from behind the eye to the
# tail, its edge along the flank a row of white scallops (the largest behind the eye, smaller toward the tail); a
# pale grey belly; a low blue hump of a dorsal fin; a forked white tail; two round grey paddle fins hanging under the
# head like little feet. Joints: body (0, 0, 0.052), tail (0, 0.05, 0.052).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, boxy, srgb, inside, smoothstep, unit, eye_look, args

H = 0.1                         # the model's height: every size below is a share of it
BLUE, WHITE, BELLY, FIN, RIM, CYAN, MOUTH, TONGUE, INK = ("#3B7DD2", "#F4F6F8", "#C6CBD2", "#AFB5BE", "#7E8A99",
                                                          "#6FD2F4", "#6E1C29", "#D9677A", "#2A3240")

HEAD_C, HEAD_R = np.array([0.0, -0.036, 0.056]), np.array([0.038, 0.046, 0.038])
BACK_C, BACK_R = np.array([0.0, 0.012, 0.056]), np.array([0.033, 0.05, 0.034])


def on(c, r, d):
    """The point of an ellipsoid along a direction of its unit sphere, and its outward normal."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    n = d / r
    return c + d * r, n / np.linalg.norm(n)


def blue_edge(y):
    """The height of the edge between the blue back and the white flank: scallops of white bulging up into the blue."""
    y = np.asarray(y, float)
    base = 0.058 - (y + 0.01) * 0.06
    # four scallops behind the eye, each smaller toward the tail: (start, width, height)
    bumps = np.zeros_like(y)
    for y0, w, a in ((-0.012, 0.024, 0.012), (0.012, 0.018, 0.009), (0.03, 0.015, 0.007), (0.045, 0.013, 0.005)):
        t = (y - y0) / w
        bumps = np.maximum(bumps, a * np.sqrt(np.clip(1 - (2 * t - 1) ** 2, 0, 1)))
    return base + bumps


def blue_front(z):
    """The blue's front edge over the head: just behind the eye's top, sloping back down to the eye."""
    return -0.042 + (0.094 - z) * 0.3


def body(k):
    k.add("head", ell(HEAD_C, HEAD_R, deform=boxy(2.3), sub=5), WHITE, "body")
    k.add("back", ell(BACK_C, BACK_R, sub=5), WHITE, "body")
    # The tail's stalk, flatter from side to side than it is deep
    V, F = tube([(0, 0.035, 0.055), (0, 0.06, 0.054), (0, 0.078, 0.054)], [0.024, 0.014, 0.008])
    V[:, 0] *= 0.7
    k.add("stalk", (V, F), WHITE, "tail")
    edge = 0.0007
    k.paint(lambda P: inside(P[:, 2] - 0.038, 0.004), BELLY)
    k.paint(lambda P: inside(blue_edge(P[:, 1]) - P[:, 2], edge) * inside(blue_front(P[:, 2]) - P[:, 1], edge), BLUE)


def mouth(k):
    """The round open mouth at the head's front: dark red, a pink tongue on its floor."""
    c = HEAD_C + np.array([0.0, -HEAD_R[1] * 0.97, -0.009])
    def look(u, v):
        col = np.tile(srgb(MOUTH), (len(u), 1))
        col[(u / 0.8) ** 2 + ((v + 0.75) / 0.5) ** 2 < 1] = srgb(TONGUE)
        return col
    k.decal(c, unit(0, -1, -0.15), (0.0125, 0.0135), look, eps=0.0005, rings=16, segs=48)


def fins(k):
    # The dorsal: a low blue hump, turned 40 degrees to +X so the game's cameras see its face
    yaw = math.radians(40)
    ex = (math.sin(yaw), math.cos(yaw), 0.0)
    dorsal = [(-0.024, -0.004), (-0.014, 0.008), (0.002, 0.014), (0.02, 0.012), (0.034, 0.003), (0.036, -0.004)]
    k.add("dorsal", slab(dorsal, (0.0, 0.0, 0.087), ex, (0, 0, 1), 0.004), BLUE, "body", fuse=False)
    # The tail: forked, white, turned 35 degrees to +X
    yaw = math.radians(50)
    ex = (math.sin(yaw), math.cos(yaw), 0.0)
    tail = [(-0.004, 0.01), (0.014, 0.022), (0.034, 0.033), (0.03, 0.012), (0.022, 0.0), (0.03, -0.014),
            (0.034, -0.03), (0.014, -0.02), (-0.004, -0.01)]
    k.add("tailfin", slab(tail, (0.0, 0.074, 0.055), ex, (0, 0, 1), 0.004), WHITE, "tail", fuse=False)
    # The two paddles under the head, faces to the front, hanging and splayed a little
    k.add("paddle", ell((0.024, -0.03, 0.016), (0.013, 0.0045, 0.016), rot=(15, 0, -25)), FIN, "body", fuse=False,
          mirror=True)


# --- the eyes: a white disc ringed grey, a blue cap with drips, a cyan splash for a pupil ------------------------------
EYE_D = unit(0.74, -0.6, 0.14)
EYE_HALF = (0.0215, 0.0215)
CW, CB, CC, CR, CK = srgb(WHITE), srgb(BLUE), srgb(CYAN), srgb(RIM), srgb(INK)


def eye_open(u, v):
    c = np.tile(CW, (len(u), 1))
    rr = u * u + v * v
    # the blue cap, its lower edge hanging in three drips
    drip = 0.05 - 0.22 * np.maximum(0, np.cos(3.2 * u)) ** 3
    c[(rr < 0.74 ** 2) & (v > drip)] = CB
    c[rr > 0.82 ** 2] = CR
    return c


def splash(th):
    """The cyan splash's outline: a round middle with three lobes (up-front, up-back, down)."""
    return 0.78 + 0.32 * max(0.0, math.cos(3 * th - math.pi / 2)) ** 2


def eyes(k, state):
    p, n = on(HEAD_C, HEAD_R, EYE_D)
    n = unit(*(n + np.array([0, -0.25, 0])))         # brought forward, so it is seen from the front three-quarter
    k.decal(p, n, EYE_HALF, eye_look(state, eye_open, CW, CK, line=0.16), eps=0.0005, rings=20, segs=56, mirror=True)
    if state in ("Open", "Fierce"):
        right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right)
        up = np.cross(n, right)
        c = p - up * 0.003 - right * 0.0006
        k.decal(c, n, (0.0095, 0.0095), CYAN, eps=0.0009, outline=splash, rings=10, segs=90, mirror=True)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="scales")
    body(k)
    mouth(k)
    fins(k)
    k.build(voxel=0.0011, smooth=4, band=0.0015, wband=0.012)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
