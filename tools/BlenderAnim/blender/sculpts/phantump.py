# Phantump sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/phantump.py -- <the export's Phantump.glb> <out.glb>
#
# The design, from the references: a stump of brown bark is its head, a short round-edged drum a little wider at its
# foot, its underside open (a dark hollow inside an orange rim of fresh wood). On its front two great tall dark
# sockets, a pink eye in each, and a small round dark mouth low between them. A black wisp rises from the stump's top
# and curls over to the left in a point. From the stump's sides two thick branches run out, sag a little and rise
# into horns as high as the wisp, three green leaves on each. Under the stump hangs a little black ghost: a round
# blob with two nub arms and a wavy tail tapering to a point. Written to be grown from: Trevenant keeps its bark, its
# dark sockets with their pink-red eyes, its leaves.
# The skeleton: body and head both at (0, 0, 0.227) (the stump's foot), armR/armL at (+-0.07, 0, 0.277).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, leaf, outward, srgb, inside, unit, eye_look, args

# The line's colours (Trevenant takes its bark, sockets, eyes and leaves from these)
BARK, BARKDARK, RIM, HOLLOW = "#9C7A57", "#7A5C40", "#D08A3E", "#2E2520"
GHOST, SOCKET, PINK, PINKLIGHT = "#373B48", "#2A2C36", "#F2A2B4", "#FBD4DD"
LEAF, LEAFDARK = "#2DAF6A", "#1F8A52"


def cylish(n=4.0):
    """A deform that makes a sphere a drum: round about its upright axis, squared off top and bottom by n."""
    def f(V):
        rho = np.hypot(V[:, 0], V[:, 1])
        k = (rho ** n + np.abs(V[:, 2]) ** n) ** (1 / n)
        return V / np.maximum(k, 1e-6)[:, None]
    return f


def socket_eye(u, v):
    """A pink oval eye with a paler glint up and to the outer side; the rest is the socket's dark."""
    r = np.hypot(u, v)
    c = np.tile(srgb(SOCKET), (len(u), 1))
    c[r < 0.86] = srgb(PINK)
    c[((u - 0.28) / 0.3) ** 2 + ((v - 0.38) / 0.24) ** 2 < 1] = srgb(PINKLIGHT)
    return c


def leaves(k, at, dirs, bone, size=1.0, tag=""):
    """Green leaves from a twig's point at along each direction, each a loose plate."""
    for i, d in enumerate(dirs):
        k.add(f"leaf{bone}{tag}{i}", outward(leaf(at, unit(*d), 0.05 * size, 0.013 * size, thick=0.008 * size)),
              LEAF, bone, fuse=False, surface="leaf")


# --- the frame -------------------------------------------------------------------------------------------------------
STUMP = np.array([0.0, 0.0, 0.305]); STUMPR = np.array([0.104, 0.094, 0.085])
FOOT = STUMP[2] - STUMPR[2]                      # the stump's open underside
EYE_X, EYE_Z = 0.044, 0.338
SOCKET_HALF, EYE_HALF = (0.027, 0.033), (0.0145, 0.021)


def stump(k):
    def taper(V):
        V = cylish(4.2)(V)
        V[:, :2] *= (1.0 - 0.07 * V[:, 2])[:, None]     # a little wider at its foot
        return V
    k.add("stump", ell(STUMP, STUMPR, deform=taper), BARK, "head")
    # the open underside: a dark hollow in a rim of fresh wood
    rho = lambda P: np.hypot(P[:, 0] / STUMPR[0], P[:, 1] / STUMPR[1])
    low = lambda P: (P[:, 2] < FOOT + 0.012) * (P[:, 2] > FOOT - 0.02)
    k.paint(lambda P: low(P) * inside(0.9 - rho(P), 0.01), RIM, on=["stump"])
    k.paint(lambda P: inside(P[:, 2] - (FOOT + 0.004), 0.002) * inside(rho(P) - 0.82, 0.01), HOLLOW, on=["stump"])
    # a few dark grooves of bark round the sides and back
    for a in (1.2, 2.1, 2.8, -2.5, -1.6):
        def groove(P, a=a):
            th = np.arctan2(P[:, 0], -P[:, 1])
            d = np.abs((th - a + math.pi) % (2 * math.pi) - math.pi) * 0.1
            return inside(d - 0.0035, 0.002) * (P[:, 2] > FOOT + 0.02) * (P[:, 2] < STUMP[2] + 0.05)
        k.paint(groove, BARKDARK, on=["stump"])


def wisp(k):
    top = STUMP[2] + STUMPR[2]
    k.add("wisp", tube([(0.035, 0.03, top - 0.02), (0.02, 0.026, top + 0.025), (-0.002, 0.022, top + 0.06),
                        (-0.03, 0.016, top + 0.09), (-0.062, 0.01, top + 0.1), (-0.082, 0.006, top + 0.088)],
                       [0.032, 0.024, 0.017, 0.012, 0.009, 0.007]), GHOST, "head")


def branches(k):
    root = np.array([0.07, 0.0, 0.29])
    pts = [root, (0.12, -0.004, 0.29), (0.165, -0.01, 0.272), (0.198, -0.014, 0.3), (0.207, -0.016, 0.352),
           (0.196, -0.018, 0.41), (0.178, -0.02, 0.44)]
    k.add("branch", tube(pts, [0.029, 0.027, 0.024, 0.022, 0.019, 0.016, 0.014]), BARK, "arm{s}", mirror=True)
    for side, bone in ((1, "armR"), (-1, "armL")):
        m = np.array([side, 1, 1.0])
        tip = np.array([0.178, -0.02, 0.44]) * m
        leaves(k, tip - (0, 0, 0.006), [(0.55 * side, -0.15, 1), (-0.5 * side, -0.2, 0.9)], bone, size=1.5)
        leaves(k, np.array([0.208, -0.016, 0.33]) * m, [(1 * side, -0.25, 0.35)], bone, size=1.5, tag="m")


def ghost(k):
    k.add("blob", ell((0, 0.004, 0.2), (0.038, 0.034, 0.036)), GHOST, "body")
    k.add("neck", tube([(0, 0.004, 0.24), (0, 0.004, 0.21)], [0.026, 0.03]), GHOST, "body")
    k.add("nub", tube([(0.03, 0.0, 0.208), (0.052, -0.004, 0.198), (0.062, -0.006, 0.19)], [0.015, 0.013, 0.011]),
          GHOST, "body", mirror=True)
    k.add("tail", tube([(0, 0.006, 0.19), (0.012, 0.012, 0.15), (-0.018, 0.016, 0.115), (0.008, 0.02, 0.083),
                        (0.03, 0.022, 0.063), (0.014, 0.024, 0.052)],
                       [0.032, 0.026, 0.02, 0.014, 0.009, 0.0065]), GHOST, "body")


def face(k):
    # the two great dark sockets, in every state
    for side in (1, -1):
        n = unit(0.42 * side, -1, 0.06)
        k.decal((EYE_X * side, -0.085, EYE_Z), n, SOCKET_HALF, SOCKET, eps=0.003, rings=16, segs=56)
    # the small round mouth
    k.decal((0, -0.094, 0.29), (0, -1, -0.05), (0.0095, 0.0115), SOCKET, eps=0.003, rings=10, segs=40)


def eyes(k, state, at=None, half=EYE_HALF, look=socket_eye, ink=PINK):
    x, z = at if at else (EYE_X, EYE_Z)
    k.decal((x, -0.086, z), unit(0.42, -1, 0.06), half,
            eye_look(state, look, srgb(SOCKET), srgb(ink), line=0.16), eps=0.0055, rings=20, segs=56, mirror=True)


def phantump(src, out_path):
    k = Kit(src, coat="fur")
    stump(k)
    wisp(k)
    branches(k)
    ghost(k)
    face(k)
    k.build(voxel=0.0042, smooth=3, band=0.005, wband=0.03)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out_path)


if __name__ == "__main__":
    phantump(*args())
