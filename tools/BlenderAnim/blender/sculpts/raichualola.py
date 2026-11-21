# Raichu-Alola sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/raichualola.py -- <the export's Raichu-Alola.glb> <out.glb>
#
# Raichu's Alolan form, copied and changed from raichu.py (which builds as it is read): a caramel mouse, its round
# head and plump body one tall smooth bean with no neck; a big cream belly; short stubby arms with white tips held a
# little forward; small white feet with brown toe lines; round yellow paddle ears with a darker spiral drawn on their
# fronts and a brown lobe at their outer foot; big blue eyes ringed dark with a white glint; yellow cheek ovals; a
# dot of a nose and a small smile; and its tail, a long thin brown wire looping out to its left and down to a broad
# yellow board of a bolt that it rides, standing on it, the board tilted so both game cameras see its face.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, smoothstep, unit, eye_look, args

CARAMEL, CREAM, WHITE, BROWN = "#C27A38", "#F6E6C2", "#F7F3EA", "#7A4422"
YELLOW, SPIRAL, CHEEK = "#F6DC3C", "#D49A1E", "#F4D83A"
NOSE, MOUTH, RIM, IRIS, WIRE = "#5A3218", "#5A2A1A", "#22284A", "#3A8CE0", "#6E3E1E"
LIFT = 0.183                               # the form rides its board: every joint is Raichu's this much higher
H = np.array([0, -0.012, 0.668])           # the head's centre
HR = np.array([0.138, 0.124, 0.122])


def on_head(d, lift=0.0):
    d = unit(*d)
    s = 1 / math.sqrt(((d / HR) ** 2).sum())
    return H + d * (s + lift)


def rot(angles):
    from mathutils import Euler
    return np.array(Euler([math.radians(a) for a in angles]).to_matrix())


def raichu_alola(src, out):
    k = Kit(src, coat="fur")

    # The head and the body one tall bean: round head, a filler where a neck would be, a plump pill of a body
    k.add("head", ell(H, HR), CARAMEL, "head")
    k.add("neck", ell((0, -0.004, 0.575), (0.122, 0.112, 0.085)), CARAMEL, "body")
    def pear(V):
        V = V.copy(); s = 1 - 0.06 * V[:, 2]
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell((0, 0.004, 0.43), (0.138, 0.126, 0.195), deform=pear), CARAMEL, "body")

    # Short legs and small white feet with brown toe lines
    k.add("thigh", ell((0.07, -0.008, 0.29), (0.062, 0.062, 0.07)), CARAMEL, "leg{s}", mirror=True)
    k.add("foot", ell((0.074, -0.05, 0.215), (0.046, 0.064, 0.032)), WHITE, "leg{s}", mirror=True)
    k.paint(lambda P: inside(P[:, 2] - 0.238, 0.003) * inside(0.02 - np.abs(P[:, 0]), 0.003) * (P[:, 2] > 0.15), WHITE)
    CW, CB = srgb(WHITE), srgb(BROWN)
    def toes(u, v):
        c = np.tile(CW, (len(u), 1))
        c[((np.abs(np.abs(u) - 0.36) < 0.1) & (v < 0.55))] = CB
        return c
    k.decal((0.074, -0.112, 0.215), unit(0, -1, 0.15), (0.034, 0.024), toes, eps=0.003, rings=12, segs=56,
            mirror=True)

    # Short stubby arms held forward and a little down, white over their tips
    k.add("arm", tube([(0.1, -0.03, 0.565), (0.135, -0.085, 0.535), (0.142, -0.125, 0.508)], [0.044, 0.04, 0.036]),
          CARAMEL, "arm{s}", mirror=True)
    k.add("hand", ell((0.143, -0.142, 0.502), (0.036, 0.034, 0.033)), CARAMEL, "arm{s}", mirror=True)
    k.paint(lambda P: inside(P[:, 1] + 0.128, 0.003) * inside(np.abs(P[:, 2] - 0.51) - 0.06, 0.003)
            * inside(0.09 - np.abs(P[:, 0]), 0.003), WHITE)

    # The big cream belly, an upright oval on the front
    def belly(P):
        e = (P[:, 0] / 0.096) ** 2 + ((P[:, 2] - 0.43) / 0.135) ** 2 - 1
        return inside(e * 0.05, 0.0025) * inside(P[:, 1] + 0.03, 0.01)
    k.paint(belly, CREAM, on=["body", "neck"])

    # Round paddle ears out of the crown, leaning out, yellow with a brown lobe at their outer foot
    EB = np.array([0.075, 0.012, 0.77])
    EA = (6, 24, 0)
    M = rot(EA)
    EC = EB + M @ np.array([0, 0, 0.11])
    ER = np.array([0.09, 0.03, 0.112])
    def drop(V):
        V = V.copy(); t = smoothstep(-1, 0.4, V[:, 2])
        V[:, 0] *= 0.42 + 0.58 * t; V[:, 1] *= 0.6 + 0.4 * t
        return V
    k.add("ear", ell(EC, ER, rot=EA, deform=drop), YELLOW, "ear{s}", mirror=True)
    Mi = M.T
    def lobe(P):
        Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
        L = (Q - EC) @ Mi.T                       # the ear's own frame
        d = ((L[:, 0] - 0.07) / 0.06) ** 2 + ((L[:, 2] + 0.025) / 0.06) ** 2 - 1
        return inside(d * 0.04, 0.002) * (P[:, 2] > 0.75)
    k.paint(lobe, BROWN, on=["ear"])
    # the spiral on the ear's front, its outer end running into the brown lobe
    CY, CS = srgb(YELLOW), srgb(SPIRAL)
    def spiral(u, v):
        c = np.tile(CY, (len(u), 1))
        rr = np.hypot(u, v); th = np.arctan2(v, u)
        turns = 1.7
        for kk in range(3):
            t = ((th + 0.6) % (2 * math.pi) + 2 * math.pi * kk) / (2 * math.pi * turns)
            rs = 0.1 + 0.72 * t
            c[(np.abs(rr - rs) < 0.085) & (t <= 1) & (t > 0.02)] = CS
        return c
    n = M @ np.array([0, -1.0, 0]); up = M @ np.array([0, 0, 1.0])
    k.decal(EC + up * 0.012 + n * 0.02, n, (0.066, 0.078), spiral, up=up, eps=0.003, rings=40, segs=160, mirror=True)

    # The face: yellow cheek ovals, a dot of a nose, a small smile
    dc = unit(0.8, -0.5, -0.22)
    k.decal(on_head(dc), dc, (0.036, 0.046), CHEEK, eps=0.003, rings=12, segs=48, mirror=True)
    k.add("nose", ell(on_head((0, -1, -0.02), -0.002), (0.01, 0.006, 0.006)), NOSE, "head", fuse=False,
          surface="skin")
    CO, CM = srgb(CARAMEL), srgb(MOUTH)
    def smile(u, v):
        c = np.tile(CO, (len(u), 1))
        c[(np.abs(v - (-0.35 + 0.9 * u * u)) < 0.24) & (np.abs(u) < 0.9)] = CM
        return c
    dm = unit(0, -1, -0.3)
    k.decal(on_head(dm), dm, (0.032, 0.016), smile, eps=0.003, rings=18, segs=72)

    # The board: a broad yellow surfboard of a rounded bolt, two fifths as wide as it is long, its front edge convex,
    # its back edge hollowed, a notch stepped into its front edge and a pointed tip at its right end. It lies under the
    # feet with its back edge raised 30 degrees off the ground and its right end raised a little, so both game cameras
    # see its face, not its edge
    O = np.array([0.03, -0.035, 0.148])
    ex = unit(1, 0, 0.2)
    ez = unit(0, math.cos(math.radians(30)), math.sin(math.radians(30)))
    def board(a, b):
        return O + a * ex + b * ez
    A = np.linspace(-0.31, 0.33, 48)
    t = (A + 0.31) / 0.64                                   # 0 at the round left end, 1 at the pointed tip
    mid = -0.035 + 0.075 * np.sin(np.pi * t)                # the middle line bows back
    half = 0.13 * np.clip(np.sin(np.pi * t ** 0.85), 0, 1) ** 0.7 * (1 - 0.35 * t) + 0.004
    front, back = [], []
    for a, tt, m, h in zip(A, t, mid, half):
        notch = 0.05 * max(0.0, 1 - abs(tt - 0.62) / 0.06)  # a step into the front edge, as the bolt's
        front.append((a, m - h + notch))
        back.append((a, m + h * 0.55))
    # Two lobes either side of the notch: the broad one under the feet, its back edge raised, for the front camera;
    # the pointed one sweeping up to the right with its front edge raised, so its face shows to the back camera too
    cut = int(np.argmin(np.abs(t - 0.62)))
    k.add("board", slab(front[:cut + 1] + back[:cut + 1][::-1], O, ex, ez, 0.034), YELLOW, "root", fuse=False)
    a0, m0 = A[cut], mid[cut]
    OB = board(a0, m0)
    exB = unit(1, 0, 0.75)
    ezB = unit(0, math.cos(math.radians(28)), -math.sin(math.radians(28)))
    lobe = [(a - a0 + 0.012, b - m0) for a, b in front[cut - 1:]] + [(a - a0 + 0.012, b - m0) for a, b in back[cut - 1:]][::-1]
    k.add("boardtip", slab(lobe, OB, exB, ezB, 0.034), YELLOW, "root", fuse=False)

    # The tail: one smooth brown wire out of the rump, a round loop out to the left and down, and in to the middle of
    # the board's back edge, yellow for its last stretch
    i0 = int(np.argmin(np.abs(A - 0.0)))
    end = board(A[i0], back[i0][1] - 0.012)
    k.add("tailroot", tube([(0, 0.08, 0.37), (-0.03, 0.16, 0.34)], [0.016, 0.015], seg=12), WIRE, "body", fuse=False)
    loop = [(0, 0.08, 0.37), (-0.03, 0.16, 0.34), (-0.12, 0.22, 0.29), (-0.24, 0.22, 0.23), (-0.31, 0.15, 0.17),
            (-0.28, 0.07, 0.12), (-0.18, 0.07, 0.11), (-0.08, 0.08, 0.15), end + (0, 0.03, 0.01)]
    k.add("tailwire", tube(loop[1:], [0.015, 0.0148, 0.0145, 0.014, 0.014, 0.014, 0.0145, 0.015], seg=12), WIRE,
          "root", fuse=False)
    k.add("tailtip", tube([end + (-0.035, 0.035, -0.005), end + (0, 0.03, 0.01), end], [0.0155, 0.017, 0.019],
                          seg=12), YELLOW, "root", fuse=False)

    # Big blue eyes with a dark rim and a white glint
    CR, CI = srgb(RIM), srgb(IRIS)
    def eye(u, v):
        c = np.tile(CR, (len(u), 1))
        c[(u / 0.78) ** 2 + ((v + 0.02) / 0.8) ** 2 < 1] = CI
        c[((u + 0.05) / 0.36) ** 2 + ((v + 0.12) / 0.4) ** 2 < 1] = CR
        c[((u - 0.18) / 0.3) ** 2 + ((v - 0.36) / 0.28) ** 2 < 1] = (1, 1, 1)
        return c
    de = unit(0.44, -0.88, 0.17)
    E = on_head(de)
    def eyes(state):
        k.decal(E, de, (0.026, 0.03), eye_look(state, eye, CO, CR), eps=0.003, rings=20, segs=56, mirror=True)

    k.build(voxel=0.0055)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    raichu_alola(*args())
