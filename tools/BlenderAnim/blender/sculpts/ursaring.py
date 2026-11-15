# Ursaring sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Teddiursa's script (teddiursa.py, imported as sibling("teddiursa")): the same round ears with a lining laid on their
# front, the same yellow lower face painted round the head's axis, the same black nose, mouth and two small fangs, the
# same hull claws on round paws and broad feet.
#   blender --background --factory-startup --python sculpts/ursaring.py -- <the export's Ursaring.glb> <out.glb>
#
# The design, from the references: a big brown bear standing upright, heavy and broad; a smallish head on a thick
# short neck, the lower face yellow from cheek to cheek with a black nose on its snout and a frowning mouth; small
# black eyes under a heavy scowling brow; small round ears, tan inside; a fringe of flat shaggy tufts of a paler brown
# round the shoulders; a yellow ring on the belly; thick arms hanging a little forward, each paw with four long pale
# claws; thick legs set wide, short shins, broad feet with three claws; a small round tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, polygon, srgb, inside, unit, boxy, eye_look, args, sibling

t = sibling("teddiursa")
BROWN, FRINGE, YELLOW, LINING = "#8E6550", "#A57B62", "#EBCB6A", "#D6B47A"
CLAW, INK, MOUTH, WHITE = "#DDEDEC", t.INK, "#4A2626", t.WHITE


def tuft(k, name, root, out, down, length, width, thick, bone, color):
    """A flat shaggy tuft: a thick tapering plate from root, out and down, its broad face outward."""
    root, out, down = np.array(root, float), unit(*out), unit(*down)
    d = unit(*(out * 0.8 + down * 0.6))
    side = np.cross(d, out); side /= np.linalg.norm(side)
    n = np.cross(side, d)
    P = []
    for s_, w in ((-1, width), (1, width)):
        P.append(root + side * s_ * w + n * thick * 0.5)
        P.append(root + side * s_ * w - n * thick * 0.5)
        P.append(root + d * length * 0.75 + side * s_ * w * 0.8 + n * thick * 0.3)
        P.append(root + d * length * 0.75 + side * s_ * w * 0.8 - n * thick * 0.3)
    P.append(root + d * length + n * thick * 0.1)
    k.add(name, hull(P), color, bone, fuse=False, surface="fur")


def ursaring(src, out):
    k = Kit(src, coat="fur")
    H = np.array([0, -0.065, 0.765])             # the head's middle (its bone is at 0, -0.02, 0.62)
    HR = np.array([0.122, 0.112, 0.102])
    B = np.array([0, 0.01, 0.47])                # the trunk's middle

    # The trunk: a heavy pear, a broad chest, the hips over thick thighs
    def pear(V):
        V = V.copy()
        s = 1 + 0.08 * np.clip(-V[:, 2], 0, 1) - 0.05 * np.clip(V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("trunk", ell(B, (0.205, 0.17, 0.27), deform=pear), BROWN, "body")
    k.add("chest", ell((0, -0.005, 0.63), (0.205, 0.155, 0.12)), BROWN, "body")
    k.add("hips", ell((0, 0.02, 0.285), (0.195, 0.155, 0.12)), BROWN, "body")
    k.add("neck", tube([(0, -0.01, 0.62), (0, -0.04, 0.71)], [0.12, 0.1]), BROWN, "head")
    k.add("tail", ell((0, 0.175, 0.3), (0.05, 0.045, 0.048)), BROWN, "body")

    # The head: the cub's grown smaller beside the body, a snout standing forward for the yellow lower face
    def head_shape(V):
        V = boxy(2.3)(V)
        V[:, 0] *= 1 + 0.06 * np.clip(-V[:, 2], 0, 1)
        return V
    HEAD = ell(H, HR, deform=head_shape)
    k.add("head", HEAD, BROWN, "head")
    SNOUT = ell(H + (0, -0.085, -0.04), (0.08, 0.065, 0.055))
    k.add("snout", SNOUT, BROWN, "head")
    # A heavy brow ridge over each eye, lower toward the nose
    k.add("brow", ell(H + (0.047, -0.098, 0.03), (0.042, 0.024, 0.016), rot=(0, 18, -10)), BROWN, "head",
          mirror=True)

    # Small round ears, tan inside
    t.round_ear(k, (0.092, -0.03, 0.855), (0.038, 0.022, 0.036), (0.15, -1, 0.1), "ear{s}", BROWN, LINING,
                (0.021, 0.022))

    # Thick arms hanging down and a little forward, round paws, four long pale claws each
    k.add("arm", tube([(0.17, -0.02, 0.64), (0.255, -0.04, 0.53), (0.275, -0.085, 0.42)], [0.08, 0.068, 0.058]),
          BROWN, "arm{s}", mirror=True)
    for s, bone in ((1, "armR"), (-1, "armL")):
        t.paw(k, "paw" + bone[-1], (0.28 * s, -0.1, 0.37), (0.06, 0.058, 0.06), (0.1 * s, -0.55, -1), bone,
              BROWN, claws=4, length=0.07, width=0.013, spread=0.42, claw_color=CLAW)

    # Thick legs set wide, the thighs fused into the hips, short shins, broad feet with three claws
    for s, bone in ((1, "legR"), (-1, "legL")):
        m = np.array([s, 1, 1.0])
        k.add("thigh" + bone[-1], ell(np.array((0.13, 0.0, 0.25)) * m, (0.11, 0.12, 0.13)), BROWN, bone)
        k.add("shin" + bone[-1], tube([np.array(p) * m for p in [(0.15, -0.01, 0.17), (0.165, -0.02, 0.065)]],
                                      [0.082, 0.072]), BROWN, bone)
        k.add("foot" + bone[-1], ell(np.array((0.17, -0.055, 0.035)) * m, (0.078, 0.105, 0.038)), BROWN, bone)
        for i, dx in enumerate((-0.035, 0.0, 0.035)):
            t.claw(k, f"toe{bone[-1]}{i}", np.array((0.17 + dx, -0.15, 0.025)) * m, (0.2 * s * np.sign(dx), -1, 0.0),
                   0.038, 0.013, bone, CLAW)

    # The fringe of flat tufts round the shoulders, open over the throat
    ring = []
    for a in np.radians(np.linspace(40, 320, 15)):
        o = np.array([math.sin(a), -math.cos(a), 0])
        ring.append((a, o))
    for i, (a, o) in enumerate(ring):
        root = np.array([0, 0.0, 0.665]) + o * np.array([0.19, 0.15, 0]) + (0, 0, 0.012 * (i % 2))
        tuft(k, f"tuft{i}", root, o, (0, 0, -1), 0.1 + 0.015 * (i % 2), 0.034, 0.03, "body", FRINGE)

    # The yellow lower face from cheek to cheek, the same paint as the cub's
    FRONT = lambda P: P[:, 1] < H[1] + 0.03
    z0 = H[2]
    FACE = t.mirrored([(0.0, z0 - 0.018), (0.04, z0 - 0.016), (0.075, z0 - 0.022), (0.105, z0 - 0.04),
                       (0.125, z0 - 0.075), (0.12, z0 - 0.12), (0.09, z0 - 0.15), (0.0, z0 - 0.16)])
    k.paint(lambda P: t.poly_mask(*t.round_about(P, H, HR[0]), FACE, edge=0.0025) * FRONT(P), YELLOW,
            on=["head", "snout"])

    # The yellow ring on the belly: crisp edges, an oval a little taller than wide
    RZ = 0.43
    def belly_ring(P):
        s, z = t.round_about(P, B, 0.2)
        e = np.hypot(s / 0.155, (z - RZ) / 0.158)
        return inside(np.abs(e - 1) * 0.155 - 0.016, 0.0022) * (P[:, 1] < B[1] - 0.05)
    k.paint(belly_ring, YELLOW)

    # The black nose on the snout's top, the frowning mouth under it, two small fangs at its corners
    NC = t.hit(SNOUT, (0, -1, z0 - 0.02), (0, 1, 0))
    k.decal(NC, unit(0, -1, 0.35), (0.026, 0.017), srgb(INK), eps=0.004, rings=10, segs=48)
    CM, CY = srgb(MOUTH), srgb(YELLOW)
    def mouth(u, v):
        c = np.tile(CY, (len(u), 1))
        c[np.abs(v - 0.7 * (1 - u * u) + 0.3) < 0.4] = CM          # a frown: arched up in its middle
        return c
    MC = t.hit(SNOUT, (0, -1, z0 - 0.078), (0, 1, 0))
    k.decal(MC, unit(0, -1, -0.2), (0.045, 0.018), mouth, eps=0.004, rings=8, segs=56)
    for s in (1, -1):
        p = t.hit(SNOUT, (0.036 * s, -1, z0 - 0.08), (0, 1, 0))
        t.claw(k, f"fang{'R' if s > 0 else 'L'}", p + (0, 0.004, 0), (0.12 * s, -0.25, 1), 0.024, 0.0085, "head",
               WHITE)

    # Small black eyes under the scowling brow: the top line slopes down toward the nose
    CB, CK = srgb(BROWN), srgb(INK)
    OUT = [(-1.0, -0.15), (-0.7, 0.25), (0.2, 0.62), (1.0, 0.45), (0.85, -0.3), (0.1, -0.7), (-0.6, -0.55)]
    open_eye = t.bear_eye(CK, at=(0.3, 0.2), size=0.26)
    def fierce(u, v):
        c = open_eye(u, v)
        c[v > 0.25 + 0.45 * u] = CB
        return c
    de = unit(0.45, -0.88, 0.12)
    E = t.hit(HEAD, (0.052, -1, z0 + 0.008), (0, 1, 0))
    def lay(state):
        look = fierce if state == "Fierce" else eye_look(state, open_eye, CB, CK, line=0.2)
        for s in (1, -1):
            shape = polygon([(u * s, v) for u, v in OUT])
            col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
            k.decal(E * np.array([s, 1, 1]), de * np.array([s, 1, 1]), (0.025, 0.02), col, eps=0.005,
                    outline=shape, rings=20, segs=56)

    k.build(voxel=0.0078, band=0.006)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    ursaring(*args())
