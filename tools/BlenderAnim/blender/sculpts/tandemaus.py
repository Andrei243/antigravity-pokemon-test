# Tandemaus sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tandemaus.py -- <the export's Tandemaus.glb> <out.glb>
#
# The design, from the references: two little white mice standing side by side, always together. Each is a great
# round head (half of its height) with two big round ears up and out at its top corners, small black bead eyes with a
# glint set low and wide, a tiny dark nose between them; a small pear of a body on two stubby legs; the inner arms
# raised so that the two hold paws between their heads, the outer arms short stubs hanging forward; thin white tails
# that meet behind them in a curl. The one on the left wears pale blue-grey below (shorts), the one on the right pale
# blue-grey above (a vest). No tuft on the head: the pictures show only a few fine strokes of fuzz.
# Written to be grown from (maushold.py): a mouse is one function placed by a frame (foot, size, turn), each head on
# its own bone, the bodies on "body".
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, outward, polygon, args

WHITE, LINING, BLUE = "#F6F4F0", "#EAE8EE", "#C0CAE2"
EYE, GLINT, NOSE = "#141418", "#FFFFFF", "#38303A"

# A Tandemaus-sized mouse in its own axes (x right, y back, z up, its foot at nought): measured on the references and
# the head made a tenth larger (its height half the mouse's to the ears' top)
HC = np.array([0.0, -0.004, 0.19])            # the head's centre
HR = np.array([0.086, 0.075, 0.081])          # its radii
EAR_AT = np.array([0.074, 0.014, 0.068])      # an ear's middle from the head's centre (on +X)
EAR_R = (0.058, 0.017, 0.066)                 # its radii: wide, thick, tall
EAR_TILT = 34                                 # degrees out from upright
EYE_AZ, EYE_EL = 27, -9                       # where the eyes sit on the head (degrees from the front, and up)
EYE_HALF = (0.0125, 0.0155)
SHOULDER = np.array([0.036, -0.004, 0.1])
HANG = np.array([0.04, -0.034, 0.064])        # an outer arm's paw, hanging forward inside the body's width


def rotz(deg):
    a = math.radians(deg)
    return np.array([[math.cos(a), -math.sin(a), 0], [math.sin(a), math.cos(a), 0], [0, 0, 1.0]])


class Mouse:
    """One mouse of the line: foot its place on the ground, s its size against a Tandemaus, turn degrees about the
    upright (positive turns its face toward +X)."""

    def __init__(self, foot, s, turn, name, head_bone, body_bone="body", head_k=1.0, eye_k=1.0):
        self.foot = np.array(foot, float)
        self.s = s
        self.R = rotz(turn)
        self.name = name
        self.head_bone = head_bone
        self.body_bone = body_bone
        self.hk = head_k                           # a little one's head is larger for its size
        self.ek = eye_k                            # and its eyes larger again
        self.hc = self.world(HC * np.array([1, 1, 1]))
        self.hr = HR * s * head_k

    def world(self, p):
        return self.foot + self.R @ (np.array(p, float) * self.s)

    def way(self, d):
        d = self.R @ np.array(d, float)
        return d / np.linalg.norm(d)

    def local(self, P):
        return ((P - self.foot) @ self.R) / self.s

    def on_head(self, az, el):
        """The head's surface at azimuth az (degrees from the face toward its +X) and elevation el, and its outward
        way."""
        A, E = math.radians(az), math.radians(el)
        dl = np.array([math.sin(A) * math.cos(E), -math.cos(A) * math.cos(E), math.sin(E)])
        R = self.hr
        p = dl / math.sqrt(((dl / R) ** 2).sum())
        n = p / R ** 2
        n /= np.linalg.norm(n)
        return self.hc + self.R @ p, self.R @ n


def mouse(k, m, clothes=None, hands=(None, None), tail=None):
    """
    A mouse's parts. clothes: None, "shorts" or "vest". hands: where its paw on +X and on -X go (world points), or
    None for the outer arm hanging forward. tail: a list of world points from its rump, or None.
    """
    s, n = m.s, m.name
    hb, bb = m.head_bone, m.body_bone
    # The body: a small pear, its legs stubby haunches fused into it, little feet
    def pear(V):
        V = V.copy()
        V[:, :2] *= (1 + 0.14 * np.clip(-V[:, 2], 0, 1))[:, None]
        return V
    turn = math.degrees(math.atan2(m.R[1, 0], m.R[0, 0]))
    k.add(n + "torso", ell(m.world((0, 0.0, 0.07)), np.array((0.047, 0.041, 0.056)) * s, rot=(0, 0, turn),
                           deform=pear), WHITE, bb)
    for sx in (1, -1):
        sd = "R" if sx > 0 else "L"
        k.add(n + "leg" + sd, ell(m.world((0.023 * sx, -0.004, 0.036)), np.array((0.022, 0.023, 0.03)) * s,
                                  rot=(0, 0, turn)), WHITE, bb)
        k.add(n + "foot" + sd, ell(m.world((0.025 * sx, -0.014, 0.015)), np.array((0.02, 0.026, 0.016)) * s,
                                   rot=(0, 0, turn)), WHITE, bb)
    # The great round head, its big round ears up and out at its top corners
    k.add(n + "head", ell(m.hc, m.hr, rot=(0, 0, turn)), WHITE, hb)
    for sx in (1, -1):
        e = EAR_AT * np.array([sx, 1, 1]) * s * m.hk
        ec = m.hc + m.R @ e
        k.add(n + "ear" + ("R" if sx > 0 else "L"),
              ell(ec, np.array(EAR_R) * s * m.hk, rot=(0, EAR_TILT * sx, turn)), WHITE, hb)
        # a faint lining on its face
        tilt = math.radians(EAR_TILT * sx)
        up = m.R @ np.array([math.sin(tilt), 0, math.cos(tilt)])
        fn = m.way((0, -1, 0))
        # painted on the ear's own front (a patch there was caught by the other mouse's ear in front of it)
        side = np.cross(up, fn)
        def lining(P, ec=ec, fn=fn, up=up, side=side, sc=s * m.hk):
            d = P - ec
            r = np.hypot((d @ side) / (EAR_R[0] * 0.7 * sc), (d @ up - EAR_R[2] * 0.06 * sc) / (EAR_R[2] * 0.72 * sc))
            return inside(r - 1, 0.08) * inside(-(d @ fn), 0.002)
        k.paint(lining, LINING, on=[n + "ear" + ("R" if sx > 0 else "L")])
    # The arms: an outer one hanging forward, an inner one raised to hold another's paw
    for sx, hand in zip((1, -1), hands):
        sd = "R" if sx > 0 else "L"
        sh = m.world(SHOULDER * np.array([sx, 1, 1]))
        paw = np.array(hand, float) if hand is not None else m.world(HANG * np.array([sx, 1, 1]))
        mid = sh + (paw - sh) * 0.5 + m.way((0, -0.4, -0.2)) * 0.006 * s
        k.add(n + "arm" + sd, tube([sh, mid, paw], [0.014 * s, 0.0135 * s, 0.013 * s]), WHITE, bb)
        k.add(n + "paw" + sd, ell(paw, np.array((0.016, 0.015, 0.017)) * s), WHITE, bb)
    # Clothes: pale blue-grey shorts below, or a vest above
    if clothes:
        lo, hi = (0.027, 0.064) if clothes == "shorts" else (0.06, 0.118)
        def wear(P, m=m, lo=lo, hi=hi):
            L = m.local(P)
            return inside(lo - L[:, 2], 0.0015) * inside(L[:, 2] - hi, 0.0015) * inside(np.hypot(L[:, 0], L[:, 1]) - 0.07, 0.002)
        k.paint(wear, BLUE, on=[n + "torso", n + "leg"])
    # The tail, thin, a loose piece so that it keeps its line
    if tail is not None:
        pts = np.array(tail, float)
        k.add(n + "tail", outward(tube(pts, np.linspace(0.0062, 0.0056, len(pts)) * max(s, 0.9), seg=12)), WHITE, bb,
              fuse=False)
    # The tiny dark nose
    p, nn = m.on_head(0, -17)
    k.decal(p, nn, (0.0068 * s * m.hk, 0.0054 * s * m.hk), NOSE, up=m.way((0, 0, 1)), eps=0.0015 * s,
            outline=polygon([(-1, 0.55), (1, 0.55), (0, -0.95)]), rings=6, segs=36)


def eye_open(u, v):
    c = np.tile(srgb(EYE), (len(u), 1))
    c[((u - 0.28) / 0.3) ** 2 + ((v - 0.36) / 0.28) ** 2 < 1] = srgb(GLINT)
    return c


def lay_eyes(k, mice, state):
    look = eye_look(state, eye_open, srgb(WHITE), srgb(EYE), line=0.17)
    for m in mice:
        half = (EYE_HALF[0] * m.s * m.hk ** 1.5 * m.ek, EYE_HALF[1] * m.s * m.hk ** 1.5 * m.ek)
        for sx in (1, -1):
            p, n = m.on_head(sx * EYE_AZ, EYE_EL)
            f = look if sx > 0 else (lambda u, v, g=look: g(-u, v))
            k.decal(p, n, half, f, up=m.way((0, 0, 1)), eps=0.0018 * m.s, rings=20, segs=56)


def board(k):
    V = np.array([v.co[:] for v in k.ob.data.vertices])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    sq = 1.3 * max(h, 0.85 * max(w, l))
    print("KIT: board: shows %.3f in a square of %.3f (%.2f)" % (np.ptp(proj), sq, np.ptp(proj) / sq))


# --- Tandemaus ----------------------------------------------------------------------------------------------------------
GAP = 0.12           # each mouse's middle from the midline: the heads a little apart, as in the pictures


def tandemaus(src, out):
    k = Kit(src, coat="fur")
    left = Mouse((-GAP, -0.03, 0), 1.0, 8, "a", "head")
    right = Mouse((GAP, 0.03, 0), 1.0, -8, "b", "head2")
    held = np.array([0.0, -0.038, 0.124])          # the two paws held between their heads
    mouse(k, left, "shorts", hands=(held + (-0.012, 0, 0), None),
          tail=[left.world((0.0, 0.038, 0.05)), (-0.075, 0.06, 0.044), (-0.03, 0.08, 0.048), (-0.006, 0.076, 0.066),
                (0.0, 0.07, 0.084), (0.014, 0.068, 0.078), (0.016, 0.07, 0.064), (0.006, 0.072, 0.058)])
    mouse(k, right, "vest", hands=(None, held + (0.012, 0, 0)),
          tail=[right.world((0.0, 0.038, 0.05)), (0.075, 0.086, 0.044), (0.034, 0.082, 0.046), (0.01, 0.084, 0.058),
                (-0.006, 0.082, 0.076), (-0.022, 0.08, 0.08), (-0.026, 0.078, 0.064), (-0.012, 0.078, 0.054)])
    k.build(voxel=0.0033, smooth=4, band=0.006, wband=0.03)
    board(k)
    k.eye_states(lambda st: lay_eyes(k, (left, right), st))
    k.export(out)


if __name__ == "__main__":
    tandemaus(*args())
