# Mega Charizard X sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/charizardmegax.py -- <the export's Charizard-Mega-X.glb> <out.glb>
# Charizard's frame (charizard.py, copied here, since its function takes no arguments) turned black: charcoal scales,
# a pale blue belly from the jaw down the throat to between the legs and under the tail, red eyes. Its largest change
# is the wings: larger, black-edged, their membrane blue on both faces, the trailing edge cut into three long sharp
# points. Its jaws stand open (a dark mouth, a pink tongue, a white fang each side) and blue fire streams back from
# each corner of them; its two horns are longer and stand up and back, tipped in blue; two blue spikes jut from each
# shoulder, a row of small black spikes runs down the back of the neck, and the tail's flame is blue.
#
# Written to be grown from (Mega Charizard Y imports it with sibling("charizardmegax")): the frame is `charizard_mega`,
# and what a form changes is a `Look` (colours and the functions that add its wings, horns, mouth and other parts).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, outward, axes, srgb, inside, smoothstep, unit, eye_look, args, sibling

cm = sibling("charmander")
cl = sibling("charmeleon")

# The head's place and size: Charizard's, a tenth larger
H0, HR0 = np.array([0.0, -0.15, 1.02]), np.array([0.088, 0.09, 0.078])
HS = 1.1


def along(shape, base, d):
    """A shape made along +z (its root at the origin) laid along d from base, its frame right-handed."""
    V, F = shape
    a, b = axes(d)
    d = unit(*d)
    return outward((np.array(base, float) + V[:, :1] * a + V[:, 1:2] * b + V[:, 2:3] * d, F))


def tongue(w, length, th=None):
    """A flame's tongue along +z from the origin: round at its root, pointed at its end (Charmander's teardrop)."""
    th = w * 0.85 if th is None else th
    V, F = ell((0, 0, length / 2), (w, th, length / 2), deform=cm.teardrop)
    return V, F


def flame_along(k, name, base, d, length, w, outer, heart, bone, licks=((0.35, 0.55, 30), (-0.25, 0.45, -35))):
    """A flame streaming from base along d: a main tongue, two smaller licking out, a pale heart through both faces."""
    d = unit(*d)
    a, b = axes(d)
    shapes = [along(tongue(w, length), base, d)]
    for t, s, ang in licks:
        dd = unit(*(d * math.cos(math.radians(ang)) + a * math.sin(math.radians(ang))))
        shapes.append(along(tongue(w * 0.5, length * s), np.array(base) + d * length * 0.18 + a * w * t, dd))
    k.add(name, cm.join(*shapes), outer, bone, fuse=False, glow=outer)
    k.add(name + "heart", along(tongue(w * 0.62, length * 0.72, th=w * 0.95), np.array(base) + d * length * 0.04, d),
          heart, bone, fuse=False, glow=heart)


class Look:
    """Mega Charizard X: what the frame asks of a form."""
    BODY, BELLY = "#3A3D49", "#7CC8F0"
    IRIS = srgb("#D8303A")
    CLAW = cm.CLAW
    BLUE, PALE = "#3F8CF2", "#CDEBFF"
    OPEN_MOUTH = True
    MOUTH, TONGUE = "#5A1420", "#E07888"

    # --- the wings: larger than Charizard's, three long sharp points on the trailing edge ------------------------
    WING_ROOT = np.array([0.1, 0.15, 0.655])
    WING_EX = unit(0.82, 0.48, 0.2)
    WING_EZ = unit(0.3, 0.22, 1.0)
    WING_SCALE = 1.38
    WING = [(0.0, 0.03), (0.2, 0.3), (0.5, -0.07), (0.4, -0.05), (0.43, -0.22), (0.31, -0.08), (0.29, -0.27),
            (0.2, -0.1), (0.15, -0.23), (0.08, -0.07), (0.03, -0.12), (0.0, -0.07)]
    TOP, TIP = (0.2, 0.3), (0.5, -0.07)
    STRUTS = [(0.43, -0.22), (0.29, -0.27), (0.15, -0.23)]
    MEMBRANE_FRONT, MEMBRANE_BACK = "#2B7FD0", "#22609E"

    def wing_frame(self):
        ex = self.WING_EX
        ez = self.WING_EZ - ex * (self.WING_EZ @ ex); ez /= np.linalg.norm(ez)
        return ex, ez, np.cross(ex, ez)

    def wing_point(self, a, b, lift=0.0):
        ex, ez, n = self.wing_frame()
        return self.WING_ROOT + self.WING_SCALE * (a * ex + b * ez) + n * lift

    def wings(self, k):
        ex, ez, n = self.wing_frame()
        P = np.array(self.WING) * self.WING_SCALE
        mid = P.mean(axis=0)
        inner = [tuple(mid + (p - mid) * 0.9 + (0.006, 0.0)) for p in P]
        # The back plate is the membrane's back face; a thinner plate on the front is its front face
        k.add("wingback", slab([tuple(p) for p in P], self.WING_ROOT, ex, ez, 0.018), self.MEMBRANE_BACK, "wing{s}",
              fuse=False, mirror=True)
        k.add("wingface", slab(inner, self.WING_ROOT + n * 0.0105, ex, ez, 0.006), self.MEMBRANE_FRONT, "wing{s}",
              fuse=False, mirror=True)
        wp = self.wing_point
        lead = outward(tube([wp(0.0, 0.02), wp(0.1, 0.17), wp(*self.TOP), wp(0.36, 0.12), wp(*self.TIP)],
                            [0.04, 0.032, 0.026, 0.019, 0.012]))
        k.add("wingedge", lead, self.BODY, "wing{s}", fuse=False, mirror=True)
        # Black struts from the top to each point, on both faces
        for j, e in enumerate(self.STRUTS):
            e = np.array(self.TOP) + (np.array(e) - np.array(self.TOP)) * 0.93
            for side, lift in (("f", 0.011), ("b", -0.0105)):
                st = outward(tube([wp(*self.TOP, lift=lift), wp(*((np.array(self.TOP) + e) / 2), lift=lift),
                                   wp(*e, lift=lift)], [0.016, 0.013, 0.009]))
                k.add(f"wingstrut{side}{j}", st, self.BODY, "wing{s}", fuse=False, mirror=True)
        t = wp(*self.TOP)
        k.add("wingclaw", cm.claw(t, unit(0.3, 0.25, 1), 0.06, 0.038, 0.03, up=(0, 1, 0)), self.BODY, "wing{s}",
              fuse=False, mirror=True)

    # --- the head's own parts -------------------------------------------------------------------------------------
    def horns(self, k, at):
        """Two horns standing up and back off the back of the skull, black, their tips blue."""
        for s in (1, -1):
            side = "R" if s > 0 else "L"
            b, d, L = at(0.045 * s, 0.06, 0.035), unit(0.28 * s, 0.75, 0.75), 0.19
            k.add(f"horn{side}", cm.claw(b, d, L, 0.046, 0.04, up=(0, -1, 1)), self.BODY, "head", fuse=False,
                  surface="scales")
            k.add(f"horntip{side}", cm.claw(b + d * L * 0.52, d, L * 0.5 + 0.004, 0.034, 0.03, up=(0, -1, 1)),
                  self.BLUE, "head", fuse=False, surface="scales")

    def head_extras(self, k, at):
        """Blue fire streaming back from each corner of the jaws, a row of small black spikes down the neck."""
        for s in (1, -1):
            side = "R" if s > 0 else "L"
            flame_along(k, f"jawfire{side}", at(0.06 * s, -0.07, -0.045), unit(0.36 * s, 0.78, 0.72), 0.31, 0.05,
                        self.BLUE, self.PALE, "head")
        for j, (y, z, L) in enumerate([(0.07, 0.94, 0.05), (0.075, 0.86, 0.055), (0.08, 0.78, 0.055)]):
            b = (0, y, z)
            k.add(f"neckspike{j}", cm.claw(b, unit(0, 1, 0.55), L, 0.034, 0.03, up=(1, 0, 0)), self.BODY,
                  "head" if z > 0.83 else "body", fuse=False, surface="scales")

    def body_extras(self, k):
        """Two blue spikes jutting up and out from each shoulder."""
        for s in (1, -1):
            side = "R" if s > 0 else "L"
            m = np.array([s, 1, 1.0])
            for j, (b, d, L, w) in enumerate([((0.165, -0.03, 0.7), (0.35, -0.25, 1), 0.11, 0.052),
                                              ((0.215, -0.08, 0.64), (0.75, -0.35, 0.6), 0.085, 0.042)]):
                k.add(f"shoulderspike{side}{j}", cm.claw(np.array(b) * m, np.array(unit(*d)) * m, L, w, w * 0.9,
                                                         up=(0, -1, 0)), self.BLUE, f"arm{side}", fuse=False,
                      surface="shell")

    def tail_flame(self, k, tip):
        flame_along(k, "flame", np.array(tip) - (0, 0, 0.01), unit(0.15, 0.25, 1), 0.33, 0.07, self.BLUE, self.PALE,
                    "flame", licks=((0.6, 0.6, 32), (-0.6, 0.5, -30)))

    def wrist_extras(self, k):
        pass

    def tail_extras(self, k, tail):
        pass


def throat(lo=0.56, hi=0.97):
    """The belly's colour up the front of the neck to the jaw: in front of the neck's line, narrow at the top."""
    Z = np.array([0.5, 0.62, 0.73, 0.83, 0.91, 0.975, 1.0])
    Y = np.array([-0.02, 0.03, 0.035, 0.0, -0.05, -0.11, -0.13])
    W = np.array([0.11, 0.1, 0.075, 0.06, 0.052, 0.05, 0.05])
    def mask(P):
        yc = np.interp(P[:, 2], Z, Y)
        w = np.interp(P[:, 2], Z, W)
        return (inside(P[:, 1] - (yc - 0.04), 0.005) * inside(np.abs(P[:, 0]) - w, 0.006)
                * inside(P[:, 2] - hi, 0.005) * smoothstep(lo - 0.01, lo + 0.01, P[:, 2]))
    return mask


def charizard_mega(src, out, L):
    k = Kit(src, coat="scales")
    BODY = L.BODY

    # The head: Charizard's, a tenth larger, carried forward and up on the long neck
    H, HR = H0, HR0 * HS
    def at(x, y, z):
        return H + np.array([x, y, z]) * HS
    k.add("head", ell(H, HR), BODY, "head")
    k.add("snout", ell(at(0, -0.095, -0.02), np.array((0.062, 0.1, 0.052)) * HS, rot=(-6, 0, 0)), BODY, "head")
    if L.OPEN_MOUTH:
        # The jaw dropped open, the mouth's dark and a tongue set back between the jaws
        k.add("jaw", ell(at(0, -0.06, -0.075), np.array((0.066, 0.095, 0.036)) * HS, rot=(30, 0, 0)), BODY, "head")
        k.add("mouthin", ell(at(0, -0.075, -0.052), np.array((0.05, 0.08, 0.03)) * HS, rot=(10, 0, 0)), L.MOUTH, "head")
        k.add("tongue", ell(at(0, -0.1, -0.06), np.array((0.038, 0.05, 0.016)) * HS, rot=(18, 0, 0)), L.TONGUE, "head")
    else:
        k.add("jaw", ell(at(0, -0.045, -0.055), np.array((0.07, 0.1, 0.042)) * HS, rot=(10, 0, 0)), BODY, "head")
    k.add("brow", ell(at(0, -0.045, 0.025), np.array((0.075, 0.07, 0.035)) * HS, rot=(12, 0, 0)), BODY, "head")
    L.horns(k, at)
    k.add("neckroot", tube([(0, 0.03, 0.62), (0, 0.035, 0.73), (0, 0.0, 0.83)], [0.12, 0.09, 0.072]), BODY, "body")
    k.add("neck", tube([(0, 0.0, 0.83), (0, -0.05, 0.91), (0, -0.11, 0.975)], [0.072, 0.064, 0.06]), BODY, "head")

    # The body: Charizard's great pear, a little heavier
    k.add("chest", ell((0, 0.0, 0.6), (0.175, 0.145, 0.125)), BODY, "body")
    k.add("torso", ell((0, 0.0, 0.47), (0.24, 0.2, 0.215)), BODY, "body")
    k.add("hips", ell((0, 0.03, 0.35), (0.25, 0.19, 0.145)), BODY, "body")
    k.decal((0, -0.2, 0.47), (0, -1, 0), (0.172, 0.225), L.BELLY, eps=0.003, rings=18, segs=60)
    k.paint(throat(), L.BELLY)

    # The arms: Charizard's, the forearm forward, three fingers with white claws in front of the belly's edge
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"arm{side}", tube(np.array([(0.15, -0.04, 0.65), (0.228, -0.145, 0.6), (0.205, -0.24, 0.55)]) * m,
                                 [0.054, 0.047, 0.04]), BODY, f"arm{side}")
        HC = np.array((0.198, -0.272, 0.536))
        k.add(f"hand{side}", ell(HC * m, (0.04, 0.038, 0.034)), BODY, f"arm{side}")
        for j, d in enumerate([(0.35, -0.9, -0.15), (0.0, -1, -0.3), (-0.35, -0.9, -0.15)]):
            d = unit(*d)
            b = HC + d * 0.03
            k.add(f"finger{side}{j}", tube(np.array([b, b + d * 0.03]) * m, [0.016, 0.013]), BODY, f"arm{side}")
            tip = b + d * 0.038
            k.add(f"claw{side}{j}", cm.claw(tip * m, np.array(d) * m, 0.028, 0.018, 0.015), L.CLAW, f"arm{side}",
                  fuse=False, surface="shell")
    L.wrist_extras(k)
    L.body_extras(k)

    # The legs: heavy thighs fused into the hips, short shins, broad feet with three white claws
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array((0.195, -0.01, 0.3)) * m, (0.14, 0.155, 0.165), rot=(-12, 0, 0)), BODY,
              f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.2, -0.02, 0.18), (0.2, -0.015, 0.07)]) * m, [0.08, 0.072]), BODY,
              f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.2, -0.06, 0.038)) * m, (0.08, 0.115, 0.04)), BODY, f"leg{side}")
        for j, dx in enumerate((-0.036, 0.0, 0.036)):
            b = np.array((0.2 + dx, -0.155, 0.026))
            k.add(f"toe{side}{j}", cm.claw(b * m, np.array((dx * 0.6, -1, -0.35)) * m, 0.045, 0.03, 0.023),
                  L.CLAW, f"leg{side}", fuse=False, surface="shell")

    # The tail: thick at the hips, back low and curling up on the +X side to its flame, the belly's colour beneath
    TAIL = [(0, 0.1, 0.36), (0.02, 0.27, 0.2), (0.09, 0.42, 0.14), (0.18, 0.5, 0.19), (0.22, 0.53, 0.28)]
    k.add("tail", tube(TAIL, [0.125, 0.088, 0.06, 0.043, 0.034]), BODY, "tail")
    k.paint(cl.tail_underside(TAIL, below=0.016, start=0.22), L.BELLY)
    L.tail_flame(k, (0.22, 0.53, 0.29))
    L.tail_extras(k, TAIL)

    L.wings(k)
    L.head_extras(k, at)

    # The mouth: open (a white fang down from each side of the upper jaw), or shut on a dark line with the fangs
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        if not L.OPEN_MOUTH:
            k.decal(at(0.048 * s, -0.115, -0.043), unit(0.85 * s, -0.4, -0.2), (0.058, 0.005), "#5A1A16", up=(0, 0, 1),
                    eps=0.004, rings=4, segs=32)
        k.add(f"fang{side}", cm.claw(at(0.04 * s, -0.15, -0.045), (0, -0.2, -1), 0.03, 0.018, 0.013, up=(0, 1, 0)),
              L.CLAW, "head", fuse=False, surface="shell")
    k.decal(at(0.02, -0.185, 0.003), unit(0.2, -0.6, 0.8), (0.0075, 0.006), "#2A1C1C", eps=0.004, rings=4, segs=16,
            mirror=True)

    # The eyes: small and fierce under the brow, sloping down toward the snout
    cut = cl.brow_cut(0.32, 0.45)
    de = unit(0.68, -0.68, 0.26)
    E = cm.on_ell(H, HR, de)
    eyes = cl.eyes_under_brow(k, E, de, (0.03, 0.035), cl.fierce_eye(L.IRIS, cut), cut, srgb(BODY))

    k.build(voxel=0.008, smooth=4)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    charizard_mega(*args(), Look())
