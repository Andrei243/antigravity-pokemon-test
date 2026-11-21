# Ursaluna sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Teddiursa's line (Ursaring's kin): Teddiursa's paws, claws, round ears and the way a place is found on a part
# (sibling("teddiursa")); its own flat planks of bark (plank) where Ursaring had pointed tufts.
#   blender --background --factory-startup --python sculpts/ursaluna.py -- <the export's Ursaluna.glb> <out.glb>
#
# The design, from the references: a huge brown bear on four legs, heavy and low, its head held low and forward; over
# its back a great mound of grey peat, the largest mass, ringed by a fringe of flat brown planks standing out and up
# round its rim; a big brown head, a full yellow moon on the brow with two grey brows lying across its lower edge,
# over half-shut green eyes; small round ears; a tan muzzle with a grey bridge and a dark nose, the mouth open wide and
# pink with two white fangs; thick forelegs grey from the elbow down, broad paws with five long white claws; brown
# haunches fused into the flanks, grey hind feet with white claws; a grey ball of a tail.
# The export's joints: body (0, 0.02, 0.38), head (0, -0.25, 0.4), ears (+-0.09, -0.31, 0.48), legs (+-0.13, -0.18,
# 0.3), hinds (+-0.13, 0.2, 0.3), tail (0, 0.32, 0.42).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, polygon, srgb, inside, unit, boxy, tri, lumpy, eye_look, args, sibling

t = sibling("teddiursa")
BROWN, PEAT, GREY, TAN, YELLOW = "#6B3D2A", "#8E8580", "#A39C97", "#E6C07A", "#F3CF45"
PLANK, PINK, THROAT, CLAW, INK, NOSE = "#7A4530", "#E2738C", "#9C3550", "#EEF0EE", "#1A1414", "#3E3634"
IRIS, WHITE, LINING = "#6FA850", "#F6F6F2", "#C9A15E"

H = np.array([0.0, -0.335, 0.29])            # the head's middle
HR = np.array([0.145, 0.128, 0.134])
M = np.array([0.0, 0.05, 0.4])               # the mound's middle
MR = np.array([0.255, 0.31, 0.175])


def trunk(k):
    # The brown trunk under the mound, low: its belly within a few hundredths of the ground
    k.add("trunk", ell((0, 0.04, 0.27), (0.215, 0.32, 0.19)), BROWN, "body")
    k.add("chest", ell((0, -0.2, 0.27), (0.2, 0.16, 0.17)), BROWN, "body")
    k.add("rump", ell((0, 0.27, 0.3), (0.19, 0.12, 0.16)), BROWN, "body")
    # The mound of peat over the back: a lumpy grey dome, the largest mass
    k.add("mound", ell(M, MR, deform=lumpy(5, 3, 0.05)), PEAT, "body")
    k.add("moundfront", ell((0, -0.17, 0.41), (0.2, 0.13, 0.12)), PEAT, "body")
    k.add("tail", ell((0, 0.35, 0.43), (0.075, 0.07, 0.07)), GREY, "tail")


def plank(k, name, root, d, out, length, w, th, bone, short=0.0):
    """A flat plank of bark: a hard-edged board from root along d, its broad face turned out, its end cut square
    (one corner short by `short`, as the pictures' planks end ragged)."""
    root, d, out = np.array(root, float), unit(*d), unit(*out)
    side = np.cross(d, out); side /= np.linalg.norm(side)
    n = np.cross(side, d)
    P = []
    for s_ in (-1, 1):
        for q in (-1, 1):
            P.append(root + side * s_ * w + n * q * th / 2)
            end = length - (short if s_ > 0 else 0.0)
            P.append(root + d * end + side * s_ * w * 0.85 + n * q * th * 0.4)
    k.add(name, hull(P), PLANK, bone, fuse=False, surface="fur")


def fringe(k):
    """The flat brown planks round the mound's rim: a crown standing up and out, a lower ring over the flanks."""
    n = 28
    for i in range(n):
        a = 2 * math.pi * i / n
        o = np.array([math.sin(a), -math.cos(a), 0.0])
        root = M + o * MR * np.array([0.9, 0.97, 0]) + (0, 0, -0.07 + 0.012 * (i % 2))
        if o[1] < -0.6:                       # over the head the rim comes forward and down onto the brow
            root = root + (0, 0.03, -0.01)
        d = o * 0.5 + np.array([0, 0, 0.87])
        plank(k, f"plank{i}", root, d, o, 0.15 + 0.03 * (i % 2), 0.032, 0.024, "body", short=0.02 * (i % 3 == 0))
    for i in range(18):
        a = math.radians(-150 + 300 * i / 17)    # round the sides and back; the front is the head's
        o = np.array([math.sin(a), -math.cos(a), 0.0])
        if abs(o[0]) < 0.35 and o[1] < 0:
            continue
        root = np.array([0, 0.05, 0.31]) + o * np.array([0.2, 0.3, 0]) + (0, 0, 0.005 * (i % 2))
        plank(k, f"low{i}", root, o * 0.75 + np.array([0, 0, -0.66]), o, 0.085 + 0.015 * (i % 2), 0.028, 0.022,
              "body", short=0.015 * (i % 2))
    # Planks round the head's sides, as the pictures frame the face
    for s in (1, -1):
        for j, (z, dz) in enumerate(((0.35, 0.3), (0.27, -0.1), (0.19, -0.5))):
            root = np.array([0.125 * s, -0.32, z])
            plank(k, f"cheek{'R' if s > 0 else 'L'}{j}", root, (s, -0.25, dz), (0, -1, 0.2), 0.06, 0.022, 0.02,
                  "head")


def head(k):
    def shape(V):
        V = boxy(2.4)(V)
        V[:, 0] *= 1 + 0.05 * np.clip(-V[:, 2], 0, 1)
        return V
    HEAD = ell(H, HR, deform=shape)
    k.add("head", HEAD, BROWN, "head")
    k.add("neck", ell((0, -0.24, 0.29), (0.15, 0.12, 0.13)), BROWN, "head")
    # The muzzle: tan, standing forward low on the face, the mouth open wide under it
    k.add("muzzle", ell(H + (0, -0.115, -0.045), (0.078, 0.07, 0.052)), TAN, "head")
    k.add("jaw", ell(H + (0, -0.085, -0.15), (0.072, 0.07, 0.038)), TAN, "head")
    # the open mouth: a pink mass standing between the muzzle and the dropped jaw, darker deep in it
    k.add("mouth", ell(H + (0, -0.118, -0.105), (0.058, 0.04, 0.045)), PINK, "head")
    k.add("throat", ell(H + (0, -0.135, -0.1), (0.032, 0.025, 0.025)), THROAT, "head")
    # the grey bridge of the nose, from the brows down the muzzle's top
    k.paint(lambda P: inside(np.abs(P[:, 0]) - (0.026 + 0.18 * np.clip(H[2] + 0.04 - P[:, 2], 0, 1)), 0.003)
            * inside(P[:, 2] - (H[2] + 0.04), 0.003) * inside((H[2] - 0.055) - P[:, 2], 0.003)
            * (P[:, 1] < H[1] - 0.09), GREY, on=["muzzle", "head"])
    k.decal(t.hit(ell(H + (0, -0.115, -0.045), (0.078, 0.07, 0.052)), (0, -1, H[2] - 0.022), (0, 1, 0)),
            unit(0, -1, 0.6), (0.028, 0.018), NOSE, eps=0.004, rings=10, segs=48)
    # Small round ears high on the head's sides, tan inside
    t.round_ear(k, (0.105, -0.3, 0.41), (0.042, 0.026, 0.04), (0.3, -1, 0.2), "ear{s}", BROWN, LINING, (0.022, 0.024))
    # The full moon on the brow
    MOON = t.hit(HEAD, (0, -1, H[2] + 0.075), (0, 1, 0))
    k.decal(MOON, unit(0, -1, 0.35), (0.05, 0.05), YELLOW, eps=0.004, rings=14, segs=56)
    # Two grey brows lying across the moon's lower edge, curling up and out over the eyes
    for s in (1, -1):
        p = [MOON + (0.008 * s, -0.012, -0.03), MOON + (0.045 * s, -0.008, -0.03), MOON + (0.08 * s, 0.01, -0.018)]
        k.add(f"brow{'R' if s > 0 else 'L'}", tube(p, [0.018, 0.019, 0.013]), GREY, "head", fuse=False,
              surface="fur")
    # Two white fangs down from the upper lip, small teeth up from the jaw
    for s in (1, -1):
        p = H + (0.038 * s, -0.172, -0.075)
        t.claw(k, f"fang{'R' if s > 0 else 'L'}", p, (0.1 * s, -0.25, -1), 0.03, 0.011, "head", WHITE)
        q = H + (0.03 * s, -0.15, -0.135)
        t.claw(k, f"tooth{'R' if s > 0 else 'L'}", q, (0.05 * s, -0.3, 1), 0.018, 0.008, "head", WHITE)
    return HEAD


def legs(k):
    for s, bone in ((1, "legR"), (-1, "legL")):
        m = np.array([s, 1, 1.0])
        # the shoulder a broad mass fused into the chest, the forearm thick, grey from the elbow
        k.add("shoulder" + bone[-1], ell(np.array((0.17, -0.2, 0.26)) * m, (0.1, 0.12, 0.13)), BROWN, bone)
        k.add("fore" + bone[-1], tube([np.array(p) * m for p in [(0.19, -0.24, 0.2), (0.215, -0.31, 0.11),
                                                                 (0.225, -0.36, 0.06)]], [0.085, 0.078, 0.072]),
              GREY, bone)
        t.paw(k, "paw" + bone[-1], np.array((0.23, -0.4, 0.045)) * m, (0.075, 0.08, 0.045), (0.12 * s, -1, -0.25),
              bone, GREY, claws=5, length=0.095, width=0.017, spread=0.36, claw_color=CLAW)
    for s, bone in ((1, "hindR"), (-1, "hindL")):
        m = np.array([s, 1, 1.0])
        k.add("haunch" + bone[-1], ell(np.array((0.165, 0.2, 0.22)) * m, (0.105, 0.135, 0.14)), BROWN, bone)
        k.add("shin" + bone[-1], tube([np.array(p) * m for p in [(0.18, 0.2, 0.13), (0.19, 0.19, 0.06)]],
                                      [0.075, 0.07]), GREY, bone)
        t.paw(k, "foot" + bone[-1], np.array((0.19, 0.15, 0.035)) * m, (0.068, 0.085, 0.035), (0.1 * s, -1, -0.2),
              bone, GREY, claws=4, length=0.06, width=0.015, spread=0.4, claw_color=CLAW)
    # Grey from the elbow and the hock down: a ragged line of fur round each leg, by distance from the leg's axis
    def leg_grey(P):
        out = np.zeros(len(P))
        for (x, y), z0 in (((0.215, -0.3), 0.17), ((0.185, 0.19), 0.125)):
            d = np.hypot(np.abs(P[:, 0]) - x, P[:, 1] - y)
            ang = np.arctan2(P[:, 1] - y, np.abs(P[:, 0]) - x)
            edge = z0 + 0.012 * tri(ang * 7 / (2 * math.pi))
            out = np.maximum(out, inside(P[:, 2] - edge, 0.003) * inside(d - 0.13, 0.01))
        return out
    k.paint(lambda P: 1 - leg_grey(P), BROWN, on=["fore", "shin", "shoulder", "haunch"])
    k.paint(leg_grey, GREY, on=["fore", "shin", "shoulder", "haunch", "paw", "foot"])


# --- the eyes: half shut, green, the brown lid's edge straight across over them ---------------------------------------
CB, CW, CI, CP, CK = srgb(BROWN), srgb(WHITE), srgb(IRIS), srgb("#141A12"), srgb(INK)
OUT = [(-1.0, 0.18), (1.0, 0.3), (0.95, -0.3), (0.55, -0.85), (0.0, -1.0), (-0.55, -0.85), (-0.95, -0.35)]


def open_eye(u, v):
    c = np.tile(CW, (len(u), 1))
    iris = ((u + 0.05) / 0.56) ** 2 + ((v + 0.2) / 0.62) ** 2 < 1
    c[iris] = CI
    c[((u + 0.05) / 0.26) ** 2 + ((v + 0.22) / 0.32) ** 2 < 1] = CP
    c[((u + 0.18) / 0.11) ** 2 + ((v + 0.02) / 0.11) ** 2 < 1] = CW
    c[v > 0.04 + 0.06 * u] = CK                    # the heavy lid's edge
    return c


def eyes(k, HEAD):
    de = unit(0.42, -0.9, 0.05)
    E = t.hit(HEAD, (0.062, -1, H[2] + 0.012), (0, 1, 0))
    def lay(state):
        if state == "Fierce":
            def look(u, v):
                c = open_eye(u, v)
                c[v > -0.05 + 0.4 * u] = CK
                c[v > 0.12 + 0.4 * u] = CB
                return c
        else:
            look = eye_look(state, open_eye, CB, CK, line=0.18)
        for s in (1, -1):
            shape = polygon([(a * s, b) for a, b in OUT])
            col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
            k.decal(E * np.array([s, 1, 1]), de * np.array([s, 1, 1]), (0.029, 0.024), col, eps=0.004,
                    outline=shape, rings=20, segs=56)
    return lay


def ursaluna(src, out):
    k = Kit(src, coat="fur")
    trunk(k)
    fringe(k)
    HEAD = head(k)
    legs(k)
    k.build(voxel=0.007, band=0.005)
    k.eye_states(eyes(k, HEAD))
    k.export(out)


if __name__ == "__main__":
    ursaluna(*args())
