# Mega Charizard Y sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/charizardmegay.py -- <the export's Charizard-Mega-Y.glb> <out.glb>
# Charizard's orange and cream on Mega Charizard X's frame (charizardmegax.py): blue eyes, the cream running up the
# throat to the jaw, the mouth shut with a white fang each side. Its largest change is the wings: tall, raised high
# above the head, a thick orange spar rising from the shoulder to a peak with a claw, a second sweeping from the peak
# out and down to a long low point, a third between, the membrane orange with a teal panel near the root. Small
# orange wings have grown on its forearms (two blades each, teal between them), one long horn sweeps up and back off
# its crown with a small spike each side behind it, and a fringe of dark spikes runs along the top of its tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import slab, tube, hull, outward, srgb, unit, args, sibling

mx = sibling("charizardmegax")
cm = mx.cm

ORANGE, CREAM, TEAL, SPIKE = "#F08A2C", cm.CREAM, "#1F6F86", "#8A4632"


def blade(base, d, length, width, thick, across):
    """A flat-faced blade from base along d, broad across `across` at its root, thick across its face."""
    d = unit(*d)
    a = np.array(across, float); a = a - d * (a @ d); a /= np.linalg.norm(a)
    n = np.cross(d, a)
    b = np.array(base, float)
    pts = [b + a * width / 2, b - a * width / 2, b + n * thick / 2, b - n * thick / 2,
           b + d * length * 0.5 + a * width * 0.3 + n * thick * 0.25, b + d * length * 0.5 + a * width * 0.3 - n * thick * 0.25,
           b + d * length * 0.45 - a * width * 0.15, b + d * length]
    return hull(pts)


class LookY(mx.Look):
    BODY, BELLY = ORANGE, CREAM
    IRIS = srgb("#2E7FB8")
    OPEN_MOUTH = False

    WING_SCALE = 1.28
    WING = [(0.0, 0.03), (0.07, 0.26), (0.15, 0.47), (0.21, 0.4), (0.31, 0.15), (0.4, -0.08), (0.43, -0.27),
            (0.35, -0.14), (0.28, -0.19), (0.2, -0.08), (0.12, -0.12), (0.05, -0.03), (0.0, -0.07)]
    APEX, LOW, MID = (0.15, 0.47), (0.43, -0.27), (0.28, -0.19)
    PANEL = [(0.03, 0.0), (0.075, 0.19), (0.13, 0.3), (0.21, 0.08), (0.27, -0.14), (0.2, -0.06), (0.12, -0.09),
             (0.05, -0.02)]

    def wings(self, k):
        ex, ez, n = self.wing_frame()
        S = self.WING_SCALE
        P = [tuple(np.array(p) * S) for p in self.WING]
        k.add("wingback", slab(P, self.WING_ROOT, ex, ez, 0.018), ORANGE, "wing{s}", fuse=False, mirror=True)
        panel = [tuple(np.array(p) * S) for p in self.PANEL]
        for side, lift in (("f", 0.0105), ("b", -0.0105)):
            k.add(f"wingpanel{side}", slab(panel, self.WING_ROOT + n * lift, ex, ez, 0.006), TEAL, "wing{s}",
                  fuse=False, mirror=True)
        wp = self.wing_point
        k.add("wingedge", outward(tube([wp(0.0, 0.02), wp(0.07, 0.25), wp(*self.APEX)], [0.042, 0.034, 0.026])),
              ORANGE, "wing{s}", fuse=False, mirror=True)
        k.add("wingouter", outward(tube([wp(*self.APEX), wp(0.31, 0.15), wp(0.4, -0.08), wp(*self.LOW)],
                                        [0.026, 0.024, 0.019, 0.011])), ORANGE, "wing{s}", fuse=False, mirror=True)
        k.add("wingmid", outward(tube([wp(*self.APEX), wp(0.22, 0.12), wp(*self.MID)], [0.022, 0.018, 0.011])),
              ORANGE, "wing{s}", fuse=False, mirror=True)
        k.add("wingclaw", cm.claw(wp(*self.APEX), unit(0.25, 0.3, 1), 0.06, 0.036, 0.03, up=(0, 1, 0)), ORANGE,
              "wing{s}", fuse=False, mirror=True)

    def horns(self, k, at):
        """One long horn swept up and back off the crown, a small spike each side of the back of the head."""
        k.add("horn", cm.claw(at(0, 0.0, 0.055), unit(0, 0.86, 0.5), 0.3, 0.06, 0.052, up=(0, -0.5, 1)), ORANGE,
              "head", fuse=False, surface="scales")
        for s in (1, -1):
            k.add(f"hornside{'R' if s > 0 else 'L'}", cm.claw(at(0.05 * s, 0.06, 0.015), unit(0.45 * s, 1, 0.25),
                                                              0.075, 0.034, 0.03), ORANGE, "head", fuse=False,
                  surface="scales")

    def head_extras(self, k, at):
        pass

    def body_extras(self, k):
        pass

    def wrist_extras(self, k):
        """Small wings on the forearms: two orange blades up and out from behind the elbow, teal between them."""
        for s in (1, -1):
            side = "R" if s > 0 else "L"
            m = np.array([s, 1, 1.0])
            e = np.array((0.235, -0.13, 0.6))
            for j, (d, L, w) in enumerate([((0.55, 0.45, 0.75), 0.21, 0.075), ((0.85, 0.35, 0.15), 0.15, 0.06)]):
                k.add(f"wristwing{side}{j}", blade(e * m, np.array(unit(*d)) * m, L, w, 0.03, (0, -1, 0)), ORANGE,
                      f"arm{side}", fuse=False, surface="scales")
            # The teal between the two blades
            p0, p1, p2 = e, e + unit(0.55, 0.45, 0.75) * 0.15, e + unit(0.85, 0.35, 0.15) * 0.12
            mid = (p0 + p1 + p2) / 3
            nn = np.cross(p1 - p0, p2 - p0); nn /= np.linalg.norm(nn)
            pts = [p0, p1, p2, mid + nn * 0.008, mid - nn * 0.008]
            k.add(f"wristpanel{side}", hull([np.array(p) * m for p in pts]), TEAL, f"arm{side}", fuse=False,
                  surface="scales")

    def tail_flame(self, k, tip):
        cm.flame(k, tip, 1.35)

    def tail_extras(self, k, tail):
        """A fringe of dark spikes along the top of the tail, pointing up and back."""
        T = np.array(tail, float)
        fine = np.vstack([T[i] + (T[i + 1] - T[i]) * t for i in range(len(T) - 1) for t in np.linspace(0, 1, 20,
                                                                                                    endpoint=False)])
        R = np.interp(np.linspace(0, 1, len(fine)), np.linspace(0, 1, 5), [0.125, 0.088, 0.06, 0.043, 0.034])
        for j, i in enumerate([18, 25, 32, 39, 46]):
            p, r = fine[i], R[i]
            tang = fine[i + 1] - fine[i - 1]; tang /= np.linalg.norm(tang)
            base = p + np.array((0, 0, 1.0)) * r * 0.75
            k.add(f"tailspike{j}", cm.claw(base, unit(*(np.array((0, 0, 1.0)) + tang * 0.9)), 0.075 - j * 0.006,
                                           0.036, 0.026, up=(1, 0, 0)), SPIKE, "tail", fuse=False, surface="shell")


if __name__ == "__main__":
    mx.charizard_mega(*args(), LookY())
