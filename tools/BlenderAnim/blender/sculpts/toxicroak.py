# Toxicroak sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/toxicroak.py -- <the export's Toxicroak.glb> <out.glb>
#
# Croagunk grown (croagunk.py: the same blue, yellow eyes in a black rim under a heavy lid, black bands, a grin of
# white teeth). From the references: a tall, lean, hunched blue frog fighter. A round head thrust forward with a
# thick spike sweeping up and back off its crown; yellow eyes under a heavy black lid slanting to the nose, a black
# streak running back from each; a red lip over a dark mouth line with a white grin of teeth in its middle. A big red
# throat sac under the chin. A black line like a singlet's edge running from the shoulders down beside the sac to a
# white patch at the groin. Arms bent out and forward, two black bands round each forearm, three blue fingers and one
# long red claw out of the knuckles. Long legs bent at the knee, a black band at each ankle, three toes, the middle
# one red, and a spike behind the heel.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

BLUE, BLACK, WHITE, RED, YELLOW, MOUTH = ("#4680CF", "#24262F", "#EEF0F2", "#E0505E", "#F6C42E",
                                                "#1E2030")

# --- the trunk: a lean chest over a narrow waist and hips ---------------------------------------------------------
k.add("pelvis", ell((0, 0.01, 0.44), (0.115, 0.09, 0.075)), BLUE, "body")
k.add("belly", ell((0, 0.0, 0.53), (0.11, 0.085, 0.09)), BLUE, "body")
k.add("chest", ell((0, -0.005, 0.63), (0.145, 0.1, 0.085)), BLUE, "body")
k.add("shoulder", ell((0.11, 0.0, 0.66), (0.055, 0.06, 0.05)), BLUE, "arm{s}", mirror=True)

# --- the head: thrust forward and a little low, the spike sweeping up and back off its crown --------------------------
HC = np.array([0.0, -0.09, 0.77])
k.add("head", ell(HC, (0.115, 0.11, 0.085), deform=boxy(2.3)), BLUE, "head")
k.add("snout", ell((0, -0.15, 0.745), (0.095, 0.065, 0.055)), BLUE, "head")
k.add("nape", ell((0, -0.03, 0.72), (0.08, 0.07, 0.06)), BLUE, "head")
k.add("crest", blade([(0, -0.08, 0.8), (0, -0.065, 0.88), (0, -0.035, 0.94), (0, 0.0, 0.985)],
                     [0.075, 0.048, 0.024, 0.004], (1, 0, 0), 0.8), BLUE, "head")

# the red throat sac under the chin, round and crisp
k.add("sac", ell((0, -0.1, 0.6), (0.115, 0.08, 0.098)), RED, "body", fuse=False, surface="shell")

# --- arms: bent out at the elbow, forearms forward, hands low in front ------------------------------------------------
SH, EL, WR = np.array((0.13, 0.0, 0.655)), np.array((0.235, 0.03, 0.6)), np.array((0.255, -0.09, 0.52))
k.add("upper", tube([SH, (0.19, 0.02, 0.63), EL], [0.042, 0.038, 0.034]), BLUE, "arm{s}", mirror=True)
k.add("fore", tube([EL, (0.248, -0.03, 0.56), WR], [0.034, 0.033, 0.031]), BLUE, "arm{s}", mirror=True)
HAND = np.array((0.262, -0.13, 0.5))
k.add("hand", ell(HAND, (0.042, 0.045, 0.034), rot=(-30, 0, 0)), BLUE, "arm{s}", mirror=True)
for i, a in enumerate((-0.025, 0.0, 0.025)):
    b0 = HAND + np.array((a, -0.02, -0.01))
    k.add(f"finger{i}", tube([b0, b0 + (a * 0.4, -0.035, -0.015), b0 + (a * 0.6, -0.05, -0.045)],
                             [0.015, 0.014, 0.015]), BLUE, "arm{s}", mirror=True)
# the long red claw out of the knuckles, curving out and forward
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    b0 = HAND + np.array((0.02, -0.01, 0.025))
    pts = np.array([b0, b0 + (0.035, -0.035, 0.025), b0 + (0.06, -0.07, 0.03), b0 + (0.075, -0.11, 0.01)]) * m
    k.add(f"claw{side}", blade(pts, [0.026, 0.021, 0.012, 0.002], (0, 0, 1), 0.5), RED, f"arm{side}", fuse=False,
          surface="shell")

# two black bands round each forearm, by place along the forearm's own line
def bands(P):
    out = np.zeros(len(P))
    for s in (1, -1):
        e, w = EL * (s, 1, 1), WR * (s, 1, 1)
        d = w - e; L = np.linalg.norm(d); d /= L
        t = (P - e) @ d / L
        r = np.linalg.norm((P - e) - np.outer(t * L, d), axis=1)
        near = inside(r - 0.06, 0.003)
        for a, b in ((0.18, 0.32), (0.45, 0.58)):
            out = np.maximum(out, near * inside(a - t, 0.035) * inside(t - b, 0.035))
    return out
k.paint(bands, BLACK)

# --- legs: long, bent forward at the knee, a black band at the ankle, three toes and a heel spike --------------------
HIP, KN, AN = np.array((0.085, 0.02, 0.42)), np.array((0.19, -0.08, 0.25)), np.array((0.155, 0.03, 0.085))
k.add("thigh", tube([HIP, (0.15, -0.05, 0.34), KN], [0.072, 0.066, 0.05]), BLUE, "leg{s}", mirror=True)
k.add("shin", tube([KN, (0.175, -0.02, 0.16), AN], [0.05, 0.04, 0.034]), BLUE, "leg{s}", mirror=True)
k.add("foot", ell((0.15, -0.0, 0.03), (0.04, 0.055, 0.028)), BLUE, "leg{s}", mirror=True)
for i, a in enumerate((-0.03, 0.0, 0.03)):
    k.add(f"toe{i}", ell((0.15 + a, -0.065, 0.02), (0.019, 0.026, 0.02)), RED if i == 1 else BLUE, "leg{s}",
          mirror=True)
k.paint(lambda P: inside(np.abs(P[:, 2] - 0.075) - 0.016, 0.003) * inside(np.abs(np.abs(P[:, 0]) - 0.15) - 0.06,
                                                                         0.003), BLACK, on=["shin", "foot"])
k.add("heel", blade([(0.155, 0.05, 0.07), (0.158, 0.075, 0.11), (0.162, 0.09, 0.15)], [0.02, 0.011, 0.002],
                    (1, 0, 0), 0.6), BLUE, "leg{s}", mirror=True)

# --- the singlet's line and the white groin patch -------------------------------------------------------------------
def strap(P):
    z = P[:, 2]
    f = np.interp(z, [0.43, 0.48, 0.53, 0.58, 0.64, 0.7], [0.045, 0.07, 0.085, 0.11, 0.125, 0.1])
    return inside(np.abs(np.abs(P[:, 0]) - f) - 0.009, 0.0045) * inside(P[:, 1] - 0.04, 0.01) \
        * inside(z - 0.675, 0.01) * inside(0.43 - z, 0.005)
k.paint(strap, BLACK)
def groin(P):
    z = P[:, 2]
    half = np.clip((z - 0.39) / 0.08, 0, 1) * 0.045
    return inside(np.abs(P[:, 0]) - half, 0.0045) * inside(z - 0.475, 0.0045) * inside(P[:, 1] + 0.03, 0.01) \
        * inside(0.37 - z, 0.005)
k.paint(groin, WHITE)

# --- the mouth: a red lip over a dark line from cheek to cheek, corners rising (a sneer) -----------------------------
HA = np.array([0, -0.09])
def head_angle(P):
    return np.arctan2(P[:, 0], -(P[:, 1] - HA[1]))
def lip_line(a):
    return 0.733 + 0.016 * a * a
def lip(P):
    a = head_angle(P)
    l = lip_line(a)
    return inside(np.abs(P[:, 2] - (l + 0.011)) - 0.0075, 0.0045) * inside(np.abs(a) - 1.15, 0.03) \
        * (P[:, 1] < -0.06) * (P[:, 2] > 0.69)
def mouth(P):
    a = head_angle(P)
    return inside(np.abs(P[:, 2] - lip_line(a)) - 0.005, 0.004) * inside(np.abs(a) - 1.2, 0.03) \
        * (P[:, 1] < -0.06) * (P[:, 2] > 0.69)
k.paint(lip, RED)
k.paint(mouth, MOUTH)
# a black streak back from each eye along the side of the head
def streak(P):
    a = np.abs(head_angle(P))
    z = 0.79 - 0.03 * (a - 0.7)
    return inside(np.abs(P[:, 2] - z) - 0.007, 0.0025) * inside(0.7 - a, 0.03) * inside(a - 1.55, 0.03)
k.paint(streak, BLACK, on=["head"])
# the grin: a white bar under the mouth's middle cut by thick dividers
CWT, CDV = srgb(WHITE), srgb("#6E7480")
def teeth(u, v):
    c = np.tile(CWT, (len(u), 1))
    for x in (-0.5, 0.0, 0.5):
        c[np.abs(u - x) < 0.08] = CDV
    return c
k.decal((0, -0.21, 0.72), unit(0, -1, 0.1), (0.04, 0.012), teeth, eps=0.004, rings=24, segs=120,
        outline=lambda th: 1.0 / max(abs(math.cos(th)), abs(math.sin(th))) ** 0.85)

k.build(voxel=0.006)

# --- the eyes: yellow in a black rim, a small black pupil, the lid heavy and slanting down to the nose ---------------
CB, CY, CBL = srgb(BLACK), srgb(YELLOW), srgb(BLUE)
def lid(u):
    return 0.25 + 0.35 * u
def eye(u, v):
    c = np.tile(CY, (len(u), 1))
    c[v > lid(u)] = CB
    return c
def ringed(f):
    def g(u, v):
        c = f(u, v)
        c[u * u + v * v > 0.72 ** 2] = CB
        return c
    return g
de = unit(0.45, -0.85, 0.2)
E = np.array([0.062, -0.17, 0.79])
def eyes(state):
    k.decal(E, de, (0.042, 0.03), ringed(eye_look(state, eye, CBL, CB, line=0.13)), eps=0.005, rings=20, segs=56,
            mirror=True)
    if state in ("Open", "Fierce"):
        k.decal(E + np.array([-0.006, 0, -0.004]), de, (0.0075, 0.011), CB, eps=0.008, rings=6, segs=40,
                mirror=True)

k.eye_states(eyes)
k.export(out)
