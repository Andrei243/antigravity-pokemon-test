# Tatsugiri (Curly Form) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tatsugiri.py -- <the export's Tatsugiri.glb> <out.glb>
#
# From the references: a small fish like a slice of fish on rice. Its body lies along the rice with its head forward,
# a round snout with two big eyes at its front (black, a white ring, a black rim), and its rear bends up behind into
# a tail that curls up and over forward, ending in a fin of two rounded lobes. A peach body with a darker orange-red
# cloak laid over its back and head whose edge drips down the sides, paler patches on the rear, a white belly, a low
# wavy orange fin on the back. It sits on a white cushion of rice. (The pictures show it lying, not upright as the
# game's own sculpt; no mouth, no side fins.)
#
# The skeleton stacks head over body with the crest above; here the head reaches forward about the head joint, the
# body lies on the body joint and the curled rear and fin go on the tail. Colours are arguments, for the other forms.
# Joints: body (0, 0, 0.14), head (0, -0.005, 0.185), crest (0, 0, 0.26), tail (0, 0.01, 0.095), finR/L.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, unit, eye_look, outward, lumpy, args

CURLY = dict(body="#F6A27C", cloak="#EE5A2A", pale="#F9C3A2", belly="#F4F1EC", fin="#EE5A2A")
RICE, INK, WHITE = "#EDECE8", "#141418", "#FAFAF8"

# The centre line (y, z) with its radius, head to tail tip; the first four are head and body, the rest the tail
LINE = [(-0.15, 0.134, 0.043), (-0.11, 0.13, 0.046), (-0.055, 0.126, 0.047), (0.01, 0.122, 0.051),
        (0.08, 0.114, 0.054), (0.135, 0.132, 0.051), (0.15, 0.186, 0.041), (0.133, 0.238, 0.03), (0.1, 0.27, 0.02)]
HC, HR = np.array([0.0, -0.135, 0.136]), np.array([0.054, 0.062, 0.053])
CURL = np.array([0.07, 0.19])            # the middle of the curl


def dense(n=300):
    p = np.array(LINE)
    t = np.linspace(0, len(p) - 1, n)
    y = np.interp(t, range(len(p)), p[:, 0]); z = np.interp(t, range(len(p)), p[:, 1])
    r = np.interp(t, range(len(p)), p[:, 2])
    s = np.concatenate([[0], np.cumsum(np.hypot(np.diff(y), np.diff(z)))])
    return np.stack([y, z], 1), r, s


DL, DR, DS = dense()


def frame(P):
    """For points: the up share (away from below, toward +Z), the outward share (away from the curl's middle), the
    length along the line and the side share (x over radius)."""
    yz = P[:, 1:3]
    i = ((yz[:, None, :] - DL[None]) ** 2).sum(2).argmin(1)
    q = DL[i]; r = DR[i]
    rel = yz - q
    out = q - CURL; out /= np.linalg.norm(out, axis=1, keepdims=True)
    return rel[:, 1] / r, (rel * out).sum(1) / r, DS[i], P[:, 0] / r


def body(k, C):
    pts = [(0, y, z) for y, z, _ in LINE]
    rad = [r for _, _, r in LINE]
    k.add("neck", tube(pts[:3], rad[:3], seg=28), C["body"], "head")
    k.add("trunk", tube(pts[2:6], rad[2:6], seg=28), C["body"], "body")
    k.add("tail", tube(pts[4:], rad[4:], seg=28), C["body"], "tail")
    k.add("head", ell(HC, HR, sub=5), C["body"], "head")


def markings(k, C):
    def cloak(P):
        up, out, s, side = frame(P)
        drip = np.clip(np.sin(2 * math.pi * s / 0.05), 0, 1) ** 2
        front = inside((0.15 - 0.55 * drip) - up, 0.03)
        front = front * inside(P[:, 1] - 0.1, 0.01)
        rear = inside((0.0 - 0.5 * drip) - out, 0.03) * inside(0.1 - P[:, 1], 0.01) * inside(0.25 - P[:, 2], 0.01)
        return np.maximum(front, rear)
    k.paint(cloak, C["cloak"], on=["neck", "trunk", "tail"])

    def hood(P):
        d = (P - HC) / HR
        drip = np.clip(np.sin(math.atan2(1, 1) + 9 * np.arctan2(d[:, 0], -d[:, 1])), 0, 1) ** 2
        return inside((0.1 - 0.45 * drip) - d[:, 2], 0.04) * inside(-0.55 - d[:, 1], 0.06)
    k.paint(hood, C["cloak"], on=["head", "neck"])

    # paler patches on the rear's cloak
    def patches(P):
        m = np.zeros(len(P))
        for c, r in (((0.15, 0.21), 0.022), ((0.135, 0.13), 0.02)):
            m = np.maximum(m, inside(np.hypot(P[:, 1] - c[0], P[:, 2] - c[1]) - r, 0.004))
        return m
    k.paint(patches, C["pale"], on=["tail", "trunk"])

    # the white belly under the lying part
    def belly(P):
        up, out, s, side = frame(P)
        return inside(up + 0.55, 0.06) * inside(P[:, 1] - 0.13, 0.01) * inside(P[:, 2] - 0.16, 0.01)
    k.paint(belly, C["belly"], on=["neck", "trunk", "tail", "head"])

    # the eyes' ground: peach round each eye so that a shut eye matches it
    def ground(P):
        m = np.zeros(len(P))
        for s in (1, -1):
            c = EYE_AT * np.array([s, 1, 1])
            m = np.maximum(m, inside(np.linalg.norm((P - c) / np.array([1.3, 1.3, 1.0]), axis=1) - 0.024, 0.003))
        return m
    k.paint(ground, C["body"], on=["head"])


def fins(k, C):
    # the low wavy fin on the back, turned to +X
    ex = unit(math.sin(math.radians(40)), math.cos(math.radians(40)), 0)
    back = [(-0.035, 0.0), (-0.03, 0.022), (-0.015, 0.034), (0.0, 0.026), (0.012, 0.036), (0.026, 0.03),
            (0.036, 0.034), (0.045, 0.018), (0.042, 0.0)]
    k.add("dorsal", slab(back, (0, 0.03, 0.162), ex, (0, 0, 1), 0.012), C["fin"], "body", fuse=False)
    # the tail's fin: two rounded lobes off the tip, one up and one out, its face to the front
    fw = unit(0.94, -0.34, 0)
    tail = [(0.0, -0.01)]
    tail += [(0.04 + 0.032 * math.cos(a), 0.009 + 0.016 * math.sin(a)) for a in np.radians(np.linspace(-100, 130, 14))]
    tail += [(0.006 + 0.017 * math.cos(a), 0.04 + 0.028 * math.sin(a)) for a in np.radians(np.linspace(-20, 210, 14))]
    tail += [(-0.014, 0.0)]
    k.add("tailfin", slab(tail, (0, 0.096, 0.268), fw, (0, 0, 1), 0.013), C["fin"], "tail", fuse=False)


def rice(k):
    k.add("rice", outward(ell((0, 0.035, 0.038), (0.1, 0.14, 0.038), deform=lumpy(7, 3, 0.08), sub=4)), RICE,
          "root", fuse=False, surface="cloth")


# --- the eyes: big ovals at the snout's front, black, a white ring, a black rim -----------------------------------
EYE_N = unit(0.62, -0.68, 0.28)
EYE_AT = HC + EYE_N * HR * 0.97
EYE_HALF = (0.019, 0.024)
CW, CK = srgb(WHITE), srgb(INK)


def eye_open(u, v):
    r = np.sqrt(u * u + v * v)
    c = np.tile(CW, (len(u), 1))
    c[r > 0.86] = CK
    c[(u + 0.05) ** 2 + (v + 0.02) ** 2 < 0.62 ** 2] = CK
    return c


def eyes(k, state, C):
    look = eye_look(state, eye_open, srgb(C["body"]), CK, line=0.16)
    k.decal(EYE_AT, EYE_N, EYE_HALF, look, eps=0.0018, rings=20, segs=56, mirror=True)


def tatsugiri(src, out, C=CURLY):
    k = Kit(src, coat="scales")
    body(k, C)
    markings(k, C)
    fins(k, C)
    rice(k)
    k.build(voxel=0.0032, smooth=4, band=0.003, wband=0.02)
    k.eye_states(lambda state: eyes(k, state, C))
    k.export(out)


if __name__ == "__main__":
    tatsugiri(*args())
