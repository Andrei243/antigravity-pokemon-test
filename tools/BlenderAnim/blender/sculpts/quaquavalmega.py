# Mega Quaquaval (a Mega of our own, plan 06 · R20b) sculpted on the skeleton and clips of the game's own export of
# the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/quaquavalmega.py -- <the export's Quaquaval-Mega.glb> <out.glb>
# Grown from quaquaval.py through sibling: the base's own function builds the whole dancer (its white face and neck,
# red-orange mask and its flared ends, beak, eyes, collar of points, wings held forward and down, bell-bottomed legs and
# feet), and a Kit of this script's own changes the parts it was given just before the body is fused. What
# PokemonModels.Ours.Megas.cs's QuaquavalMega says the Mega changes, largest mass first:
#   1. its crest of plumes fanned into a crescent headdress: nine broad plumes standing round the back of the head in
#      a half circle facing the front, blue and teal by turns, an orange spot near each one's end on both faces (the
#      base's teardrop crest and its swept-back hair plumes go into it);
#   2. two long streamers of water beads trailing from its wrists out and back to the ground (the base's boa of drops
#      at its right side goes: the streamers are the Mega's water now, and two would cross);
#   3. a skirt of twelve blue and teal feathers fanned all round its hips over the thighs.
# The export raises its right wing in a T to start the right streamer at the shoulder; here both wings stay the
# base's, forward and down, and each streamer starts at its wrist (the sculptor's words: "from its wrists").
# No pictures of a Mega of our own exist: the export's own sheet is the design.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import ell, outward, unit, args, sibling

QV = sibling("quaquaval")
Q = QV.Q
BLUE, TEAL = "#3454BE", "#46AAC8"
SPOT = "#F27034"
BEAD, GLOW_BEAD = "#96D4F4", "#96D4F4"

FAN_ROOT = np.array([0.0, 0.035, 0.82])        # behind the crown


def headdress(k):
    for i in range(9):
        a = math.radians(-84 + 21 * i)
        d = unit(math.sin(a), 0.4, math.cos(a))
        ln = 0.235 - 0.04 * abs(math.sin(a))
        across = np.cross(d, (0, 1.0, 0))
        k.add(f"fan{i}", Q.plume(FAN_ROOT + d * 0.01, d, ln, 0.062, 0.028, across=across, root=0.55),
              BLUE if i % 2 == 0 else TEAL, "head", fuse=False, surface="hair")
        n = np.cross(across / np.linalg.norm(across), d)
        n /= np.linalg.norm(n)
        a_ = across / np.linalg.norm(across)
        V, F_ = ell((0, 0, 0), (0.026, 0.021, 0.025), sub=2)
        spot = outward((V @ np.stack([a_, n, d]) + FAN_ROOT + d * ln * 0.74, F_))
        k.add(f"fanspot{i}", spot, SPOT, "head", fuse=False, surface="hair")


def streamers(k):
    path = np.array([(0.128, -0.105, 0.425), (0.19, -0.095, 0.33), (0.255, -0.02, 0.2), (0.29, 0.07, 0.09),
                     (0.3, 0.12, 0.02)])
    # walk the path's polyline at a bead's spacing
    seg = np.linalg.norm(np.diff(path, axis=0), axis=1)
    total = seg.sum()
    n = int(total / 0.026)
    for s, side in ((1, "R"), (-1, "L")):
        beads = []
        for j in range(n + 1):
            t = j * total / n
            i = min(np.searchsorted(np.cumsum(seg), t), len(seg) - 1)
            f = (t - (np.cumsum(seg)[i] - seg[i])) / seg[i]
            p = path[i] + (path[i + 1] - path[i]) * f
            r = 0.026 - 0.006 * j / n
            p = p * np.array([s, 1, 1.0])
            p[2] = max(p[2], r)
            beads.append(outward(ell(p, (r, r, r), sub=2)))
        k.add(f"streamer{side}", Q_join(*beads), BEAD, f"wing{side}", fuse=False, glow=GLOW_BEAD, surface="hair")


def Q_join(*shapes):
    V, F_, off = [], [], 0
    for v, f in shapes:
        V.append(v); F_.extend([[i + off for i in q] for q in f]); off += len(v)
    return np.vstack(V), F_


def skirt(k):
    for i in range(12):
        a = (i + 0.5) * 2 * math.pi / 12
        out = np.array([math.sin(a), -math.cos(a) * 0.85, 0.0])
        hip = np.array([0.07 * math.sin(a), 0.02 - 0.062 * math.cos(a), 0.47])
        d = unit(out[0] * 0.75, out[1] * 0.75, -1)
        across = np.array([math.cos(a), math.sin(a), 0.0])
        k.add(f"skirt{i}", Q.plume(hip, d, 0.17 if i % 2 else 0.15, 0.058, 0.024, across=across, root=0.7),
              BLUE if i % 2 == 0 else TEAL, "body", fuse=False, surface="hair")


class MegaKit(sculptkit.Kit):
    """The base's Kit, changed into the Mega's just before the body is fused (the base's own build call)."""

    def build(self, *a, **kw):
        if not getattr(self, "_mega", False) and kw.get("name") is None:
            self._mega = True
            gone = ("crest", "plume", "boastem", "rosette", "bead")
            self.parts = [p for p in self.parts if not p["name"].startswith(gone)]
            headdress(self)
            streamers(self)
            skirt(self)
        return super().build(*a, **kw)


QV.Kit = MegaKit


if __name__ == "__main__":
    QV.quaquaval(*args())
