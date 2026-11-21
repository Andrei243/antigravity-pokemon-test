# Gurdurr sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Timburr's script (timburr.py, imported: its feet, hands, the way an eye is cut along its top line):
#   blender --background --factory-startup --python sculpts/gurdurr.py -- <the export's Gurdurr.glb> <out.glb>
# From the references: Timburr's grey-beige grown into an hourglass of muscle. A round head whose crown is a cloud of
# round lumps; a big red bulb of a nose; small fierce eyes, white with a black pupil under a heavy dark line sloping
# down to the nose; a frown off to one side. A broad chest with pink lines on it, enormous pink muscle hanging like a
# teardrop on each upper arm, a narrow waist, wide hips, short thick thighs with pink across them, stubby legs on flat
# feet with pointed toes, three-fingered hands. A red steel I-beam, its channel to the front. The pictures hold the
# beam overhead at arms' length; the model carries it across its shoulders behind the head, both hands up on it,
# elbows down, as the task says (overhead it would set the model's height and shrink the body).
# Reference ratios (3D picture, crown to feet, beam not counted): head / height 0.35, head width / height 0.34, chest
# and arms across / height 0.9, waist width / height 0.3, hip width / height 0.55, beam length / height 1.3 (0.89 here).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, eye_look, outward, args, sibling

t = sibling("timburr")

GREY, PINK, RED, INK = "#C4BBAD", "#E886C0", "#C8343F", "#2A2426"
BEAM, MOUTH = "#B83C4C", "#3A2A2C"

H = np.array([0.0, -0.035, 0.47])            # the head's middle (the head bone is at 0, -0.03, 0.42)
HR = np.array([0.09, 0.076, 0.076])


def gurdurr(src, out):
    k = Kit(src, coat="fur")

    # The head: round, its crown a cloud of round lumps
    k.add("head", ell(H, HR), GREY, "head")
    k.add("jowl", ell(H + (0, -0.02, -0.045), (0.078, 0.06, 0.042)), GREY, "head")
    for i, (p, r) in enumerate([((0, -0.045, 0.535), 0.038), ((0.048, -0.035, 0.528), 0.035),
                                ((-0.048, -0.035, 0.528), 0.035), ((0.034, 0.018, 0.538), 0.036),
                                ((-0.034, 0.018, 0.538), 0.036), ((0.0, 0.035, 0.528), 0.034),
                                ((0.078, -0.01, 0.5), 0.03), ((-0.078, -0.01, 0.5), 0.03)]):
        k.add(f"lump{i}", ell(p, (r, r, r * 0.9)), GREY, "head")
    k.add("nose", outward(ell((0.0, -0.118, 0.455), (0.037, 0.032, 0.034))), RED, "head", fuse=False, surface="skin")

    # The body: a broad chest and back over a narrow waist, wide hips
    k.add("chest", ell((0, -0.005, 0.36), (0.118, 0.085, 0.082)), GREY, "body")
    k.add("yoke", ell((0, 0.03, 0.418), (0.1, 0.062, 0.04)), GREY, "body")
    k.add("waist", ell((0, 0.006, 0.265), (0.07, 0.058, 0.062)), GREY, "body")
    k.add("hips", ell((0, 0.01, 0.185), (0.112, 0.078, 0.066)), GREY, "body")
    # pink lines across the chest's sides
    def lines(P):
        x, y, z = np.abs(P[:, 0]), P[:, 1], P[:, 2]
        band = inside(np.abs(z - (0.385 - 0.55 * (x - 0.045))) - 0.008, 0.003)
        return band * inside(0.045 - x, 0.003) * inside(x - 0.105, 0.003) * inside(y + 0.035, 0.004)
    k.paint(lines, PINK, on=["chest"])

    # The arms: up to the beam behind the head, elbows down; a great pink teardrop on each upper arm
    SH, EL, WR = np.array([0.105, 0.0, 0.385]), np.array([0.19, -0.04, 0.32]), np.array([0.178, 0.018, 0.448])
    k.add("delt", ell((0.125, 0.0, 0.395), (0.052, 0.05, 0.05)), GREY, "arm{s}", mirror=True)
    k.add("upper", tube([SH, (SH + EL) / 2, EL], [0.045, 0.045, 0.04]), GREY, "arm{s}", mirror=True)
    k.add("bicep", ell((0.178, -0.045, 0.34), (0.06, 0.055, 0.072), rot=(0, -35, 0)), PINK, "arm{s}", mirror=True)
    k.add("fore", tube([EL, (EL + WR) / 2 + (0.006, 0, 0), WR], [0.038, 0.036, 0.03]), GREY, "arm{s}", mirror=True)
    t.hand(k, (0.17, 0.036, 0.468), (0.03, 0.026, 0.028),
           [(0.45, 0.55, 0.7), (0.0, 0.6, 0.8), (-0.45, 0.55, 0.7)], 0.036, 0.012, GREY, "arm{s}")

    # The legs: short thick thighs with pink across them, stubby shins, flat feet with pointed toes
    k.add("thigh", ell((0.086, 0.0, 0.118), (0.068, 0.064, 0.074)), GREY, "leg{s}", mirror=True)
    k.add("veinthigh", ell((0.096, -0.05, 0.13), (0.05, 0.022, 0.026), rot=(0, 25, 0)), PINK, "leg{s}", mirror=True)
    k.add("shin", tube([(0.096, 0.0, 0.07), (0.104, -0.01, 0.034)], [0.044, 0.038]), GREY, "leg{s}", mirror=True)
    t.foot(k, (0.108, -0.032, 0.016), (0.046, 0.062, 0.016), GREY, "leg{s}")

    # The beam: a red steel I-beam lying across the shoulders, its channel to the front
    c = np.array([0.0, 0.088, 0.47]); L = 0.25
    def box(x0, x1, y0, y1, z0, z1):
        return hull([(x, y, z) for x in (x0, x1) for y in (y0, y1) for z in (z0, z1)])
    k.add("flangetop", box(-L, L, c[1] - 0.034, c[1] + 0.034, c[2] + 0.016, c[2] + 0.034), BEAM, "body", fuse=False,
          surface="metal")
    k.add("flangebot", box(-L, L, c[1] - 0.034, c[1] + 0.034, c[2] - 0.034, c[2] - 0.016), BEAM, "body", fuse=False,
          surface="metal")
    k.add("web", box(-L + 0.001, L - 0.001, c[1] - 0.01, c[1] + 0.01, c[2] - 0.0165, c[2] + 0.0165), "#8E2C3A", "body",
          fuse=False, surface="metal")

    # The frown, off toward +X as the pictures show it
    CS, CM = srgb(GREY), srgb(MOUTH)
    def frown(u, v):
        c_ = np.tile(CS, (len(u), 1))
        c_[(np.abs(v - (0.35 - 0.8 * u * u)) < 0.3) & (np.abs(u) < 0.9)] = CM
        return c_
    MP, _ = t.front_of(H + (0, -0.02, -0.045), (0.078, 0.06, 0.042), 0.032, 0.418)
    k.decal(MP, unit(0.35, -1, -0.3), (0.026, 0.011), frown, eps=0.003, rings=10, segs=56)

    k.build(voxel=0.0048, smooth=4, band=0.004)

    # The eyes: small, white, a black pupil low toward the nose, a heavy dark line along the top sloping down to it
    CK = srgb(INK)
    def top(u):
        return 0.3 + 0.45 * u
    def open_eye(u, v):
        c_ = np.tile(np.array([1.0, 1.0, 1.0]), (len(u), 1))
        c_[u * u + v * v > 0.86 ** 2] = CK
        c_[((u + 0.18) / 0.36) ** 2 + ((v + 0.12) / 0.42) ** 2 < 1] = CK
        c_[((u + 0.08) / 0.11) ** 2 + ((v - 0.02) / 0.11) ** 2 < 1] = (1, 1, 1)
        c_[v > top(u) - 0.3] = CK
        return c_
    E, EN = t.front_of(H, HR, 0.04, 0.483)
    EN = unit(EN[0], EN[1], EN[2] + 0.05)
    def lay(state):
        look = eye_look(state, open_eye, CS, CK, line=0.16)
        for s in (1, -1):
            t.eye_patch(k, E, EN, (0.022, 0.022), look, CS, CK, top, s)

    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    gurdurr(*args())
