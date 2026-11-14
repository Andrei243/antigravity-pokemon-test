# Gible sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gible.py -- <the export's Gible.glb> <out.glb>
# A little land shark that is mostly head: a great wedge of a head over a maw that opens across most of its front,
# pink inside, the lower jaw and the belly one red front, a zigzag of big white teeth along both lips, a notched fin
# standing back off the crown as a loose plate, a flat fin swept back from each side of the head with a pale band
# across it, round eyes under a sloping brow, tiny arms, stubby legs, a short finned tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

BLUE, STRIPE, RED, MOUTH, TEETH = "#5A8DAE", "#62C2E0", "#D5513F", "#D9737C", "#F4F5F7"
H = np.array([0.0, -0.06, 0.55])            # the head's middle: large about its bone, its underside low
HR = np.array([0.27, 0.3, 0.25])
HROT = (10, 0, 0)
SN, SNR, SNROT = np.array([0.0, -0.33, 0.605]), np.array([0.115, 0.105, 0.09]), (12, 0, 0)
CUT, CUTR, CUTROT = np.array([0.0, -0.29, 0.415]), np.array([0.185, 0.2, 0.148]), (24, 0, 0)


def inside_ell(P, c, r, rot):
    m = np.array(Euler([math.radians(a) for a in rot]).to_matrix())
    q = (np.atleast_2d(P) - c) @ m / r
    return (q ** 2).sum(1) < 1.0


def flat(outline, origin, ex, ez, thick):
    """A flat plate of a polygon (a, b in the plane of ex, ez about origin), thick about its middle, sharp edged."""
    ex, ez = unit(*ex), unit(*ez)
    nrm = np.cross(ex, ez); nrm /= np.linalg.norm(nrm)
    p = np.array(outline, float)
    if 0.5 * np.sum(p[:, 0] * np.roll(p[:, 1], -1) - np.roll(p[:, 0], -1) * p[:, 1]) < 0:
        p = p[::-1]
    W = np.array(origin, float) + p[:, :1] * ex + p[:, 1:] * ez
    m = len(p)
    front, back = W + nrm * thick / 2, W - nrm * thick / 2
    idx, tris = list(range(m)), []
    def cross(o, a, b):
        return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])
    while len(idx) > 3:
        for i in range(len(idx)):
            a, b, c = idx[i - 1], idx[i], idx[(i + 1) % len(idx)]
            if cross(p[a], p[b], p[c]) <= 1e-12:
                continue
            if any(cross(p[a], p[b], p[j]) >= 0 and cross(p[b], p[c], p[j]) >= 0 and cross(p[c], p[a], p[j]) >= 0
                   for j in idx if j not in (a, b, c)):
                continue
            tris.append((a, b, c)); idx.pop(i)
            break
        else:
            break
    tris.append(tuple(idx))
    F = []
    for a, b, c in tris:
        F.append([a, b, c]); F.append([m + c, m + b, m + a])
    rim = []
    for i in range(m):
        j = (i + 1) % m
        rim.extend([front[i], back[i], back[j], front[j]])
        F.append([2 * m + 4 * i, 2 * m + 4 * i + 1, 2 * m + 4 * i + 2, 2 * m + 4 * i + 3])
    return np.vstack([front, back, np.array(rim)]), F


# The body: small and short under the head, its front the red belly
k.add("body", ell((0, 0.06, 0.21), (0.16, 0.155, 0.19)), BLUE, "body")
k.add("belly", ell((0, -0.05, 0.24), (0.135, 0.12, 0.15)), RED, "body")
# The head: a wide wedge, its snout a round knob forward over the maw
k.add("head", ell(H, HR, rot=HROT), BLUE, "head")
k.add("snout", ell(SN, SNR, rot=SNROT), BLUE, "head")
k.add("nape", ell((0, 0.12, 0.5), (0.18, 0.14, 0.17)), BLUE, "head")

# The fins at the sides of the head: flat, swept back and a little out and down from behind the eyes, a pale band
# across each
k.add("sidefin", blade([(0.19, -0.02, 0.585), (0.255, 0.13, 0.575), (0.3, 0.27, 0.55), (0.315, 0.34, 0.535)],
                        [0.07, 0.062, 0.045, 0.02], (0.25, 0, 1), 0.6), BLUE, "head", mirror=True)
k.paint(lambda P: inside(np.abs(P[:, 1] - 0.2) - 0.032, 0.004) * inside(0.2 - np.abs(P[:, 0]), 0.004),
        STRIPE, on=["sidefin"])

# A pale stripe down each side of the back behind the fins
def side_stripe(P):
    d = np.abs((P[:, 1] - 0.1) - 0.45 * (0.46 - P[:, 2])) - 0.024
    return inside(d, 0.004) * inside(0.07 - np.abs(P[:, 0]), 0.004) * inside(P[:, 2] - 0.46, 0.004) * \
        inside(0.2 - P[:, 2], 0.004) * smoothstep(0.02, 0.04, P[:, 1])

# The red front: the lower jaw and the belly, a crisp line round it, blue outside
def red_front(P):
    e = np.sqrt((P[:, 0] / 0.15) ** 2 + ((P[:, 2] - 0.24) / 0.2) ** 2) - 1
    return inside(e * 0.15, 0.004) * inside(P[:, 1] + 0.0, 0.002) * inside(P[:, 2] - 0.43, 0.004) * \
        inside(0.07 - P[:, 2], 0.004)
k.paint(lambda P: (1 - red_front(P)) * inside(P[:, 2] - 0.37, 0.004), BLUE)
k.paint(red_front, RED)
k.paint(side_stripe, STRIPE, on=["body", "nape"])

# The maw: a great hollow carved across the head's front under the snout, opening forward and a little down
k.add("maw", ell(CUT, CUTR, rot=CUTROT), MOUTH, "head", carve=True)
# Its corners run back along the sides of the head, so the gape shows from the side
k.add("gape", ell((0.13, -0.2, 0.43), (0.06, 0.11, 0.045), rot=(24, 0, 10)), MOUTH, "head", carve=True, mirror=True)


# The teeth: a zigzag of big white points along both lips, each seated just inside the rim. The rims are found
# where the cutter meets the front of the head (the head, the snout and the red front under it).
def front_y(x, z):
    """The front surface's y at (x, z): the first point from the front inside the head, snout or belly."""
    ys = np.linspace(-0.6, 0.1, 701)
    P = np.stack([np.full_like(ys, x), ys, np.full_like(ys, z)], 1)
    hit = inside_ell(P, H, HR, HROT) | inside_ell(P, SN, SNR, SNROT) | \
        inside_ell(P, np.array([0, -0.05, 0.24]), np.array([0.135, 0.12, 0.15]), (0, 0, 0))
    return ys[np.argmax(hit)] if hit.any() else None


def rim(x, upper):
    zs = np.linspace(0.62, 0.3, 321) if upper else np.linspace(0.3, 0.62, 321)
    for z in zs:
        y = front_y(x, z)
        if y is not None and inside_ell(np.array([x, y + 0.004, z]), CUT, CUTR, CUTROT)[0]:
            return np.array([x, y, z])
    return None


for i, (x, l, w) in enumerate([(0.035, 0.075, 0.03), (0.095, 0.07, 0.03), (0.145, 0.055, 0.026)]):
    p = rim(x, True)
    if p is None:
        continue
    p = p + (0, 0.022, 0.012)
    k.add(f"tooth{i}", blade([p, p + (0, 0.002, -l * 0.5), p + (0, 0.004, -l)], [w, w * 0.6, 0.004],
                             (0, 1, 0), 0.5), TEETH, "head", fuse=False, mirror=True, surface="shell")
for i, (x, l, w) in enumerate([(0.065, 0.06, 0.028), (0.125, 0.05, 0.025)]):
    p = rim(x, False)
    if p is None:
        continue
    p = p + (0, 0.03, 0.006)
    k.add(f"fang{i}", blade([p, p + (0, 0.002, l * 0.5), p + (0, 0.004, l)], [w, w * 0.6, 0.004],
                            (0, 1, 0), 0.5), TEETH, "body", fuse=False, mirror=True, surface="shell")

# The arms: tiny, held forward and a little down, three small claws
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    k.add(f"arm{side}", tube(np.array([(0.15, -0.04, 0.3), (0.19, -0.09, 0.28), (0.21, -0.13, 0.27)]) * m,
                             [0.045, 0.04, 0.036]), BLUE, f"arm{side}")
    Hn = np.array([0.215, -0.15, 0.27])
    for j, d in enumerate([(0.3, -1, 0.6), (0.5, -1, 0.0), (0.3, -1, -0.6)]):
        d = unit(*d)
        k.add(f"claw{side}{j}", blade(np.array([Hn + d * 0.01, Hn + d * 0.03, Hn + d * 0.05]) * m,
                                      [0.016, 0.01, 0.002], (0, 0, 1), 0.6), BLUE, f"arm{side}", fuse=False,
              surface="scales")

# The legs: stubby, the feet broad with three short claws
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    k.add(f"leg{side}", tube(np.array([(0.1, -0.02, 0.15), (0.11, -0.03, 0.07)]) * m, [0.075, 0.068]), BLUE,
          f"leg{side}")
    k.add(f"foot{side}", ell(np.array((0.11, -0.07, 0.035)) * m, (0.068, 0.095, 0.037)), BLUE, f"leg{side}")
    for j, dx in enumerate((-0.035, 0.0, 0.035)):
        b = np.array((0.11 + dx, -0.15, 0.025))
        k.add(f"toe{side}{j}", blade(np.array([b, b + (dx * 0.3, -0.025, -0.004), b + (dx * 0.4, -0.045, -0.01)]) * m,
                                     [0.018, 0.011, 0.003], (0, 0, 1), 0.55), BLUE, f"leg{side}", fuse=False,
              surface="scales")

# The tail: short and low behind, a fin standing up and back off its end
k.add("tail", tube([(0, 0.17, 0.18), (0, 0.27, 0.13), (0, 0.34, 0.1)], [0.08, 0.055, 0.035]), BLUE, "tail")
k.add("tailfin", blade([(0, 0.28, 0.12), (0, 0.36, 0.16), (0, 0.43, 0.21)], [0.045, 0.025, 0.004], (1, 0, 0), 0.4),
      BLUE, "tail")

# The fin on the crown: a loose plate swept up and back, its root sunk in the head, a notch in its trailing edge
# near the tip (y back, z up)
FIN = [(-0.17, 0.66), (-0.08, 0.76), (0.04, 0.85), (0.16, 0.93), (0.27, 0.985), (0.235, 0.925), (0.255, 0.9),
       (0.2, 0.86), (0.17, 0.79), (0.15, 0.7), (0.05, 0.62)]
FV, FF = flat(FIN, (0, 0, 0), (0, 1, 0), (0, 0, 1), 0.075)
FV[:, 0] *= np.clip(1 - 0.75 * (FV[:, 2] - 0.72) / 0.26, 0.25, 1)      # thick at the root, thin at the tip
k.add("fin", (FV, FF), BLUE, "head", fuse=False, surface="scales")
# Its root widened into the crown, so from the front it rises out of the head as a wedge, not a pole
k.add("finroot", blade([(0, -0.12, 0.72), (0, 0.0, 0.79), (0, 0.12, 0.84)], [0.1, 0.075, 0.04], (1, 0, 0), 0.5),
      BLUE, "head")

# The eyes: round, a black rim, white, a large black pupil with a glint, the top cut straight by the brow sloping
# down toward the snout. The cut is the patch's own outline, so the rim follows it crisply: the rim is where a point
# is near the outline, measured as its share of the outline's reach at its angle. On a sideways patch +u runs toward
# the back; the left eye's patch runs the other way, so it is laid by itself with its picture and outline turned over.
INK, WHITE, LID = srgb("#141820"), np.array([1.0, 1.0, 1.0]), srgb(BLUE)
BA, BB = 0.62, 0.38                                  # the brow's line: v = BA + BB u
def cut(th):
    s = math.sin(th) - BB * math.cos(th)
    return min(1.0, BA / s) if s > 1e-6 else 1.0
def eye(u, v):
    reach = np.array([cut(t) for t in np.arctan2(v, u)])
    rho = np.sqrt(u * u + v * v) / reach
    c = np.tile(INK, (len(u), 1))
    c[rho < 0.74] = WHITE
    c[((u + 0.02) / 0.46) ** 2 + ((v + 0.14) / 0.46) ** 2 < 1] = INK
    return c
de = unit(0.74, -0.6, 0.16)
EC = np.array([0.19, -0.27, 0.62])
def eyes(state):
    look = eye_look(state, eye, LID, INK, line=0.16)
    k.decal(EC, de, (0.05, 0.06), look, eps=0.006, rings=20, segs=56, outline=cut)
    m = np.array([-1, 1, 1.0])
    k.decal(EC * m, de * m, (0.05, 0.06), lambda u, v: look(-u, v), eps=0.006, rings=20, segs=56,
            outline=lambda th: cut(math.pi - th))
    if state in ("Open", "Fierce"):
        # The glint, a patch of its own so that its edge is round, toward the back and up on both eyes
        right = np.cross((0, 0, 1.0), de); right /= np.linalg.norm(right)
        up = np.cross(de, right)
        g = EC + right * 0.011 + up * 0.004
        k.decal(g, de, (0.009, 0.011), "#FFFFFF", eps=0.009, rings=5, segs=20)
        k.decal(g * m, de * m, (0.009, 0.011), "#FFFFFF", eps=0.009, rings=5, segs=20)

k.build(voxel=0.007, smooth=3)
k.eye_states(eyes)
k.export(out)
