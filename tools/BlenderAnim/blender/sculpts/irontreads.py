# Iron Treads sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Great Tusk's script (greattusk.py, imported: its band of blocks, its spikes and its way of making an eye):
#   blender --background --factory-startup --python sculpts/irontreads.py -- <the export's Iron Treads.glb> <out.glb>
#
# Donphan's future, from the references: a round silver-grey ball of a metal body with a darker panel line round its
# face; one great tread over it from low behind, high over the top and down in front of the face to the ground,
# curling back under, a black band of blocks each carrying a glowing red panel, black spikes off it in front and at
# its rear end; angry red eyes with dark slit pupils on a black face low on the ball's front, half hidden behind the
# tread; a short black hook of a tusk under each eye, curving down; a long black horn out of each side of the head
# pointing back; four short legs, black joints under curved silver plates, two silver claws to a foot.
# Bones: body (0, 0, 0.24), head (0, -0.12, 0.22), ears (+-0.15, -0.06, 0.27), legs (+-0.11, -0.09, 0.12), hind legs
# (+-0.11, 0.1, 0.12).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, eye_look, args, sibling

g = sibling("greattusk")
on_ell = g.on_ell

SILVER, SEAM = "#B6BAC4", "#767A86"
BLACK, GROOVE = "#2A2A32", "#18181E"
RED, PUPIL = "#F0383C", "#5A0A14"
CLAW = "#A8ACB8"

C = np.array([0.0, 0.01, 0.235])               # the ball's middle
CR = np.array([0.175, 0.19, 0.175])
F = np.array([0.0, -0.1, 0.2])                 # the face's front part (head bone)
FR = np.array([0.135, 0.1, 0.125])

# the tread's line in the midline, from low behind, over the top, down in front of the face and curling under
_LINE = [(0.2, 0.12), (0.235, 0.25), (0.19, 0.37), (0.08, 0.445), (-0.06, 0.455), (-0.18, 0.4), (-0.265, 0.3),
         (-0.3, 0.18), (-0.29, 0.08), (-0.24, 0.035)]
LINE = [(C[1] + (y - C[1]) * 1.1, max(0.055, C[2] + (z - C[2]) * 1.1)) for y, z in _LINE]
LINE[-1] = (LINE[-1][0], 0.06); LINE[-2] = (LINE[-2][0], 0.075)


def leg(k, x, y, bone):
    """A short leg: a black joint and shin, a curved silver plate over its front and side, two silver claws."""
    k.add("joint" + bone[:-3], ell((x, y, 0.115), (0.05, 0.05, 0.045)), BLACK, bone, mirror=True)
    k.add("shin" + bone[:-3], tube([(x, y, 0.11), (x + 0.004, y - 0.006, 0.065), (x + 0.006, y - 0.01, 0.04)],
                                   [0.038, 0.034, 0.034]), BLACK, bone, mirror=True)
    pts = []
    for a in np.linspace(-math.pi, math.pi, 14)[:-1]:
        d = np.array([math.cos(a), math.sin(a) * 0.9 - 0.15, 0.0])
        for z, r in ((0.125, 0.052), (0.045, 0.047)):
            for rr in (r, r - 0.012):
                pts.append(np.array([x + 0.005, y - 0.012, z]) + d * rr)
    k.add("plate" + bone[:-3], hull(pts), SILVER, bone, fuse=False, mirror=True, surface="metal")
    for dx in (-0.018, 0.018):
        a = np.array([x + 0.006 + dx, y - 0.055, 0.0])
        cl = [a + (-0.013, 0.03, 0.0), a + (0.013, 0.03, 0.0), a + (-0.011, 0.03, 0.03), a + (0.011, 0.03, 0.03),
              a + (0, -0.012, 0.0), a + (0, -0.008, 0.012)]
        k.add(f"claw{bone[:-3]}{dx:+.2f}", hull(cl), CLAW, bone, fuse=False, mirror=True, surface="metal")


def iron_treads(src, out_path):
    k = Kit(src, coat="metal")
    k.add("body", ell(C, CR), SILVER, "body")
    k.add("face", ell(F, FR), SILVER, "head")
    # the panel line round the face: a seam where the hood of the ball meets its front
    def seam(P):
        d = P - C
        ang = np.degrees(np.arccos(np.clip(-d[:, 1] / np.linalg.norm(d, axis=1), -1, 1)))
        return inside(np.abs(ang - 62) - 2.2, 0.004) * (P[:, 2] > 0.09)
    k.paint(seam, SEAM)
    # the tread: black blocks, a glowing red panel on each (one piece a bone), spikes at its ends
    panels = {"body": ([], []), "head": ([], [])}
    def red_panel(k, i, c, t, N, X, w, d, hl, bone):
        pts = []
        for b in (-w * 0.74, w * 0.74):
            for a in (-hl * 0.72, hl * 0.72):
                drop = -0.25 * d * (b / w) ** 2
                for h in (d - 0.012, d + 0.006):
                    pts.append(c + t * a + X * b + N * (drop + h))
        V, Fc = hull(pts)
        Vs, Fs = panels[bone]
        o = sum(len(v) for v in Vs)
        Vs.append(V); Fs.extend([[q + o for q in f] for f in Fc])
        if i == 0:
            g.spike(k, "rearspike", c + N * d * 0.5 + t * hl * 0.2, N, -t * 0.7 + N * 0.5 + np.array([0, 0, -0.2]),
                    0.08, 0.03, BLACK, bone, surface="metal")
        if i == 9:
            g.spike(k, "frontspike", c + N * d * 0.7, N, N * 0.75 - t * 0.6, 0.085, 0.032, BLACK, bone,
                    surface="metal")
    g.band(k, LINE, 13, lambda f: np.interp(f, [0, 0.2, 0.6, 1], [0.085, 0.108, 0.104, 0.088]),
           lambda f: np.full_like(f, 0.05), BLACK, GROOVE, lambda f: "body" if f < 0.45 else "head",
           strips=3, core=0.7, extra=red_panel, surface="metal")
    for bone, (Vs, Fs) in panels.items():
        k.add("redpanel" + bone, (np.vstack(Vs), Fs), RED, bone, fuse=False, glow=RED, surface="plastic")
    # the black face on the ball's front, behind the tread's foot
    for s in (1, -1):
        d = unit(0.58 * s, -0.8, 0.12)
        k.decal(on_ell(F, FR, d), d, (0.072, 0.06), BLACK, eps=0.002, rings=12, segs=48, up=(0, 0.2, 1))
    # the hooked tusks under the eyes and the long horns off the head's sides
    k.add("tusk", tube([(0.098, -0.17, 0.165), (0.128, -0.215, 0.12), (0.128, -0.245, 0.07), (0.112, -0.245, 0.04)],
                       [0.03, 0.025, 0.017, 0.008]), BLACK, "head", fuse=False, mirror=True, surface="metal")
    k.add("horn", tube([(0.135, -0.05, 0.31), (0.185, 0.06, 0.325), (0.215, 0.17, 0.332), (0.228, 0.27, 0.335)],
                       [0.032, 0.026, 0.016, 0.005]), BLACK, "ear{s}", fuse=False, mirror=True, surface="metal")
    # legs
    for y, bone in ((-0.09, "leg{s}"), (0.1, "hind{s}")):
        leg(k, 0.11, y, bone)
    k.build(voxel=0.005, smooth=5, band=0.005, wband=0.04)
    # red eyes with a dark slit, slanted angrily, on the black face; shut they are a red line on the black
    Rd, K, Bk = srgb(RED), srgb(PUPIL), srgb(BLACK)
    def look(u, v):
        c = np.tile(Bk, (len(u), 1))
        r = np.hypot(u / 0.85, (v + 0.1) / 0.75)
        c[(r < 1) & (v < 0.5 - 0.55 * u)] = Rd
        c[(np.abs(u + 0.02) < 0.14) & (np.abs(v + 0.1) < 0.5) & (v < 0.5 - 0.55 * u)] = K
        return c
    ed = unit(0.62, -0.77, 0.14)
    def lay(state):
        if state == "Fierce":
            def f(u, v):
                c = look(u, v)
                c[v > 0.12 + 0.4 * u] = Bk
                return c
        else:
            f = eye_look(state, look, Bk, Rd, line=0.22)
        k.decal(on_ell(F, FR, ed), ed, (0.038, 0.03), f, eps=0.004, rings=20, segs=56, mirror=True, up=(0, 0.2, 1))
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    iron_treads(*args())
