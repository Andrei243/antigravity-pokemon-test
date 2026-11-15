# Vigoroth sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Slakoth's script (slakoth.py, imported as sibling("slakoth")): the same hull claws two to a hand, the same flat tufts
# of fur, the eye set in a brown mask, the fur painted crisp by place.
#   blender --background --factory-startup --python sculpts/vigoroth.py -- <the export's Vigoroth.glb> <out.glb>
#
# The design, from the references: a white ape, hunched, its big round head (as wide as its chest) sunk onto the body
# with no neck; a red flame-shaped tuft standing up and forward from the brow; a brown mask round both eyes, joined
# over a round red nose; round white eyes with small black pupils, wild; a great open mouth under the nose, brown lips,
# pink inside, two white fangs hanging from the upper jaw; spiky fur at the cheeks and a white ruff on the chest; a
# brown patch on each flank; long thick arms (carried forward and down here, where the art flings one up), each hand
# with a round pink pad on the palm and two long dark-grey claws; short thick legs, broad feet with two dark-grey
# claws; a white tufted tail behind.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, boxy, eye_look, args, sibling

s_ = sibling("slakoth")
hit, claw, poly_mask, round_about, mirrored, tuft = s_.hit, s_.claw, s_.poly_mask, s_.round_about, s_.mirrored, s_.tuft

WHITE_FUR, MASK, RED, PINK, PAD = "#EEEDEA", "#8C6248", "#E03A48", "#E58A98", "#EE5868"
CLAW, INK, FANG = "#56545C", s_.INK, "#FFFFFF"
WHITE = np.array([1.0, 1.0, 1.0])

H = np.array([0.0, -0.065, 0.565])           # the head's middle (its bone is at 0, -0.02, 0.48)
HR = np.array([0.135, 0.122, 0.118])


def dark_claws(k, name, tip, d, bone, length=0.06, width=0.016, spread=0.02):
    """Two long dark claws out of a hand or foot along d."""
    d = unit(*d)
    a = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a)
    for i, o in enumerate((-1, 1)):
        claw(k, f"{name}{i}", np.array(tip) + a * o * spread, d + a * o * 0.1, length, width, bone, CLAW)


def vigoroth(src, out):
    k = Kit(src, coat="fur")

    # The torso: a round heavy pear, the belly low; hips fused over the thighs
    def pear(V):
        V = V.copy()
        s = 1 + 0.08 * np.clip(-V[:, 2], 0, 1) - 0.06 * np.clip(V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    B = np.array([0.0, 0.01, 0.345])
    k.add("torso", ell(B, (0.152, 0.128, 0.17), deform=pear), WHITE_FUR, "body")
    k.add("chest", ell((0, -0.03, 0.45), (0.155, 0.115, 0.1)), WHITE_FUR, "body")

    # The big round head sunk forward onto the chest
    def head_shape(V):
        V = boxy(2.2)(V)
        V[:, 0] *= 1 + 0.05 * np.clip(-V[:, 2], 0, 1)
        return V
    HEAD = ell(H, HR, deform=head_shape)
    k.add("head", HEAD, WHITE_FUR, "head")

    # Spiky fur at the cheeks, and the ruff on the chest
    for side in (1, -1):
        for i, (dz, ang) in enumerate([(0.03, 20), (-0.03, -10)]):
            root = H + np.array([side * HR[0] * 0.86, 0.01, dz])
            tuft(k, f"cheek{i}{'R' if side > 0 else 'L'}", root,
                 (side * 0.9, 0.2, math.sin(math.radians(ang))), 0.05, 0.022, 0.026, "head", WHITE_FUR)
    for i, x in enumerate((-0.07, -0.025, 0.025, 0.07)):
        root = np.array([x, -0.135, 0.42 - 0.012 * (i % 2)])
        tuft(k, f"ruff{i}", root, (x * 2.5, -0.55, -0.85), 0.055, 0.022, 0.026, "body", WHITE_FUR)

    # The red flame of a tuft, up and forward from the brow: thick, flat across, its point leaning forward
    from sculptkit import blade
    k.add("crest", blade([H + (0, -0.06, HR[2] * 0.7), H + (0, -0.11, HR[2] + 0.045), H + (0, -0.19, HR[2] + 0.075)],
                         [0.058, 0.042, 0.006], (1, 0, 0), 0.75), RED, "head", fuse=False, surface="fur")
    k.add("crestroot", ell(H + (0, -0.055, HR[2] * 0.72), (0.04, 0.035, 0.03)), RED, "head")

    # The brown mask round both eyes, joined over the nose
    FRONT = lambda P: P[:, 1] < H[1] - 0.02
    z0 = H[2]
    MASKP = mirrored([(0.0, z0 + 0.02), (0.035, z0 + 0.05), (0.075, z0 + 0.062), (0.11, z0 + 0.05),
                      (0.135, z0 + 0.015), (0.13, z0 - 0.025), (0.1, z0 - 0.048), (0.06, z0 - 0.045),
                      (0.03, z0 - 0.035), (0.0, z0 - 0.04)])
    k.paint(lambda P: poly_mask(*round_about(P, H, HR[0]), MASKP, edge=0.0025) * FRONT(P), MASK, on=["head"])

    # The round red nose in the middle of the mask
    k.add("nose", ell(H + (0, -HR[1] * 0.98, -0.022), (0.033, 0.026, 0.028)), RED, "head")

    # The great open mouth: carved, pink inside, brown lips round it, two white fangs hanging from the upper jaw
    MC = H + np.array([0, -HR[1] * 0.92, -0.085])
    MR = np.array([0.07, 0.05, 0.042])
    k.add("mouth", ell(MC, MR), PINK, "head", carve=True)
    def emouth(P, s):
        return np.sqrt(((P[:, 0] - MC[0]) / (MR[0] * s)) ** 2 + ((P[:, 2] - MC[2]) / (MR[2] * s)) ** 2)
    k.paint(lambda P: inside(emouth(P, 1.22) - 1, 0.003) * (P[:, 1] < MC[1] + 0.05), MASK, on=["head", "chest"])
    k.paint(lambda P: inside(emouth(P, 0.93) - 1, 0.003) * (P[:, 1] > MC[1] - 0.03) * (P[:, 1] < MC[1] + MR[1] * 1.3), PINK)
    for side in (1, -1):
        base = MC + np.array([side * 0.038, 0.008, MR[2] * 0.82])
        claw(k, f"fang{'R' if side > 0 else 'L'}", base, (side * 0.1, -0.2, -1), 0.03, 0.011, "head", FANG)

    # A brown patch on each flank, round the waist behind
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.3) - 0.03, 0.003) * inside(-P[:, 1] - 0.06, 0.004)
            * (np.abs(P[:, 0]) > 0.085), MASK, on=["torso"])

    # Long thick arms carried forward and down, round hands with a pink pad on the palm and two dark claws
    for s, bone in ((1, "armR"), (-1, "armL")):
        m = np.array([s, 1, 1.0])
        pts = [np.array(p) * m for p in [(0.12, 0.0, 0.47), (0.2, -0.03, 0.39), (0.235, -0.09, 0.28),
                                        (0.235, -0.15, 0.2)]]
        k.add("arm" + bone[-1], tube(pts, [0.065, 0.058, 0.054, 0.05]), WHITE_FUR, bone)
        HC = np.array((0.235, -0.165, 0.175)) * m
        k.add("hand" + bone[-1], ell(HC, (0.055, 0.05, 0.055)), WHITE_FUR, bone)
        k.decal(HC + np.array([0, -0.05, 0.0]), unit(-0.15 * s, -1, 0.1), (0.026, 0.026), PAD, eps=0.003, rings=10,
                segs=44)
        dark_claws(k, "clawh" + bone[-1], HC + np.array([0.0, -0.025, -0.04]), (0.12 * s, -0.55, -1), bone,
                   length=0.065, width=0.017, spread=0.022)

    # Short thick legs, the thighs fused into the hips, broad feet with two dark claws
    for s, bone in ((1, "legR"), (-1, "legL")):
        m = np.array([s, 1, 1.0])
        k.add("thigh" + bone[-1], ell(np.array((0.09, 0.0, 0.2)) * m, (0.085, 0.09, 0.1)), WHITE_FUR, bone)
        k.add("shin" + bone[-1], tube([np.array(p) * m for p in [(0.1, -0.01, 0.13), (0.11, -0.025, 0.05)]],
                                      [0.062, 0.055]), WHITE_FUR, bone)
        k.add("foot" + bone[-1], ell(np.array((0.11, -0.05, 0.03)) * m, (0.06, 0.08, 0.032)), WHITE_FUR, bone)
        dark_claws(k, "clawf" + bone[-1], np.array((0.11, -0.115, 0.03)) * m, (0.12 * s, -1, -0.25), bone,
                   length=0.045, width=0.016, spread=0.022)

    # The white tufted tail behind, run back and out to +X so the cameras see its face
    for i, (lean, L) in enumerate([(0.0, 0.15), (0.35, 0.125), (-0.35, 0.125)]):
        root = np.array([0.03, 0.13, 0.25])
        d = unit(0.55 + lean * 0.3, 0.8, 0.25 + lean)
        tuft(k, f"tail{i}", root + d * 0.05, d, L, 0.04, 0.04, "body", WHITE_FUR)
    k.add("tailroot", ell((0.06, 0.16, 0.26), (0.065, 0.075, 0.058), rot=(0, 0, 35)), WHITE_FUR, "body")

    # Round white eyes with small black pupils, set in the mask
    CW, CK, CM = WHITE, srgb(INK), srgb(MASK)
    def eye(u, v):
        c = np.tile(CK, (len(u), 1))
        c[u * u + v * v < 0.84 ** 2] = CW
        c[((u + 0.12) / 0.3) ** 2 + ((v - 0.0) / 0.34) ** 2 < 1] = CK
        return c
    de = unit(0.42, -0.9, 0.08)
    E = hit(HEAD, (0.06, -1, H[2] + 0.012), (0, 1, 0))
    def lay(state):
        k.decal(E, de, (0.03, 0.032), eye_look(state, eye, CM, CK, line=0.17), eps=0.005, rings=20, segs=56,
                mirror=True)

    k.build(voxel=0.0075, band=0.004)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    vigoroth(*args())
