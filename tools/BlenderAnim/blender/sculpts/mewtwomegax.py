# Mega Mewtwo X sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/mewtwomegax.py -- <the export's Mewtwo-Mega-X.glb> <out.glb>
#
# Grown from mewtwo.py (which builds as it is read, so its frame is copied here as a class that Mega Y grows from
# through sibling): the head drawn in its own measure about the top of the neck and grown, the almond eyes under a
# brow laid by hand on each side, the teardrop thighs, the knobbed feet.
#
# What Mega X changes, from the pictures, largest mass first:
#   1. heavier all over: a broad deep chest, thick arms ending in great clenched fists (a fist some 0.1 of the height
#      across against the base's open hand of 0.05);
#   2. the tube from the back of the skull grown into a thick purple collar lying over BOTH shoulders, a horseshoe
#      open at the front (the sculptor's band over one shoulder is the pictures' horseshoe seen at three-quarter);
#   3. a larger purple belly bulging forward, from the navel down between the thighs;
#   4. a long horn swept back from the crest (beside the base's two short ears);
#   5. the tail thinner, rising behind to the shoulders and hooking over at its end;
#   6. bigger feet, three toes spread with round knobs, and a heel;
#   7. blue eyes.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, polygon, eye_look, boxy, args

SKIN, PURPLE_X, INKH = "#E2DAE8", "#A86C96", "#2A1C30"
WHITE = "#FFFFFF"


def wider_top(V):
    """A ribcage: broad at the shoulders, narrowing down to the waist."""
    V = V.copy()
    V[:, 0] *= 1 + 0.2 * V[:, 2]
    return V


def taper_down(V):
    """A thigh: a teardrop, full at the hip, narrowing to the knee."""
    V = V.copy()
    s = 1 - 0.45 * np.clip(-V[:, 2], 0, 1) ** 1.2
    V[:, 0] *= s; V[:, 1] *= s
    return V


class Mewtwo:
    """mewtwo.py's frame: a head of its own measure, almond eyes, thighs, feet. A form changes its numbers."""

    def __init__(self, k, grow=1.2, forward=-0.022, purple=PURPLE_X, iris="#3C8CE6", skin=SKIN):
        self.k = k
        self.pivot, self.grow, self.forward = np.array([0.0, 0.0, 0.86]), grow, np.array([0.0, forward, 0.0])
        self.purple, self.iris, self.skin = purple, iris, skin
        self.H = np.array([0.0, 0.0, 0.94])
        self.HR = np.array([0.082, 0.082, 0.08])

    def hd(self, p):
        """A point of the head's own measure where the model has it."""
        return self.pivot + self.grow * (np.asarray(p, float) - self.pivot) + self.forward

    def head_part(self, name, shape, color=None, mirror=False, bone="head"):
        V, F = shape
        self.k.add(name, (self.hd(V), F), color or self.skin, bone, mirror=mirror)

    # --- the head: mewtwo.py's cat-like skull, short muzzle, brow ----------------------------------------------------
    def head(self, horns=True):
        self.head_part("head", ell(self.H, self.HR, deform=boxy(2.3)))
        self.head_part("jaw", ell((0, -0.03, 0.895), (0.062, 0.06, 0.045)))
        self.head_part("muzzle", ell((0, -0.07, 0.902), (0.044, 0.04, 0.036)))
        self.head_part("brow", tube([(0.018, -0.078, 0.966), (0.048, -0.072, 0.975), (0.072, -0.052, 0.976)],
                                    [0.014, 0.015, 0.012]), mirror=True)
        if horns:
            self.head_part("horn", tube([(0.046, 0.012, 0.985), (0.058, 0.022, 1.008), (0.066, 0.03, 1.026)],
                                        [0.032, 0.029, 0.026]), mirror=True)

    def mouth(self):
        INK, CS = srgb(INKH), srgb(self.skin)
        def look(u, v):
            c = np.tile(CS, (len(u), 1))
            c[(np.abs(v + 0.25 * u * u) < 0.38) & (np.abs(u) < 0.9)] = INK
            return c
        self.k.decal(self.hd((0, -0.104, 0.885)), unit(0, -1, -0.25), (0.019, 0.0072), look, eps=0.003, rings=10, segs=48)

    # --- the eyes: mewtwo.py's narrow fierce almonds, the top line sloping to the nose under the brow -----------------
    def eyes(self, state):
        INK, CS = srgb(INKH), srgb(self.skin)
        CW, CI, CP = srgb(WHITE), srgb(self.iris), srgb("#1A1020")

        def eye_top(u):
            return 0.12 + 0.42 * u

        def eye_bottom(u):
            return eye_top(u) - 0.85 * (1 - u * u) ** 0.7

        almond = [(u, eye_top(u)) for u in np.linspace(1, -1, 9)] + \
                 [(u, eye_bottom(u)) for u in np.linspace(-0.92, 0.92, 13)]

        def eye_open(st):
            def look(u, v):
                c = np.tile(CW, (len(u), 1))
                top = np.minimum(eye_top(u), -0.05 + 0.65 * u) if st == "Fierce" else eye_top(u)
                c[((u + 0.06) / 0.56) ** 2 + ((v + 0.14) / 0.66) ** 2 < 1] = CI
                c[((u + 0.06) / 0.2) ** 2 + ((v + 0.14) / 0.3) ** 2 < 1] = CP
                c[(v > top - 0.24) & (v <= top)] = INK
                c[v > top] = CS
                return c
            return look

        de = unit(0.44, -0.87, 0.12)
        E = self.hd((0.044, -0.074, 0.948))
        for side in (1, -1):
            m = np.array([side, 1, 1.0])
            flip = (lambda f: f) if side == 1 else (lambda f: (lambda u, v: f(-u, v)))
            outline = polygon([(side * u, v) for u, v in (almond if side == 1 else almond[::-1])])
            look = eye_open(state) if state in ("Open", "Fierce") else eye_look(state, eye_open("Open"), CS, INK, line=0.18)
            self.k.decal(E * m, de * m, (0.04, 0.04), flip(look), eps=0.004, outline=outline, rings=20, segs=56)

    # --- the legs: teardrop thighs fused into the hips, tapering shins, feet with knobbed toes ------------------------
    def legs(self, thigh=(0.086, 0.094, 0.15), shin=(0.048, 0.034, 0.027), toes=None, toe_r=0.024, toe_col=None,
             heel=True):
        k, S = self.k, self.skin
        k.add("thigh", ell((0.098, -0.012, 0.405), thigh, rot=(-8, 0, -7), deform=taper_down), S, "leg{s}", mirror=True)
        k.add("shin", tube([(0.11, -0.022, 0.3), (0.12, 0.0, 0.18), (0.122, 0.022, 0.085)], list(shin)), S, "leg{s}",
              mirror=True)
        k.add("ankle", ell((0.122, 0.024, 0.06), (shin[2] + 0.005, shin[2] + 0.009, shin[2] + 0.005)), S, "leg{s}",
              mirror=True)
        A = np.array([0.122, 0.02, 0.034])
        for i, (x, y) in enumerate(toes or ((0.1, -0.128), (0.148, -0.118))):
            tip = np.array([x, y, toe_r])
            k.add(f"toebone{i}", tube([A, (A + tip) / 2 + (0, 0, 0.004), tip], [0.026, 0.022, toe_r * 0.8]), S,
                  "leg{s}", mirror=True)
            k.add(f"toe{i}", ell(tip + (0, -0.006, 0), (toe_r, toe_r * 1.1, toe_r)), toe_col or S, "leg{s}",
                  mirror=True)
        if heel:
            k.add("heel", ell((0.122, 0.07, toe_r), (toe_r, toe_r * 1.1, toe_r)), toe_col or S, "leg{s}", mirror=True)


# --- Mega X ---------------------------------------------------------------------------------------------------------
def mega_x(src, dst):
    k = Kit(src, coat="fur")
    m = Mewtwo(k, grow=1.2)
    S, P = m.skin, m.purple

    # The trunk, a step heavier all round: a broad deep chest, round shoulders, the waist thicker
    k.add("chest", ell((0, 0.012, 0.735), (0.142, 0.085, 0.105), deform=wider_top), S, "body")
    k.add("pec", ell((0.06, -0.026, 0.745), (0.084, 0.058, 0.066)), S, "body", mirror=True)
    k.add("waist", tube([(0, 0.004, 0.56), (0, 0.01, 0.63), (0, 0.01, 0.68)], [0.088, 0.07, 0.092]), S, "body")
    k.add("belly", ell((0, -0.016, 0.51), (0.112, 0.118, 0.11)), S, "body")
    k.add("neck", tube([(0, 0.008, 0.79), (0, -0.008, 0.88)], [0.052, 0.046]), S, "body")
    k.add("shoulder", ell((0.15, 0.004, 0.79), (0.058, 0.058, 0.056)), S, "arm{s}", mirror=True)

    # The purple belly: larger than the base's and bulging forward (the front alone, crisp against the grey)
    k.add("paunch", ell((0, -0.07, 0.5), (0.088, 0.07, 0.1)), S, "body")
    k.paint(lambda P_: inside((np.sqrt((P_[:, 0] / 0.094) ** 2 + ((P_[:, 2] - 0.5) / 0.118) ** 2) - 1) * 0.09, 0.0025)
            * inside(P_[:, 1] + 0.02, 0.003) * inside(P_[:, 2] - 0.62, 0.003), P)
    # A faint seam down the chest's middle, from the collar to the belly (the pictures' line)
    k.paint(lambda P_: inside(np.abs(P_[:, 0]) - 0.004, 0.0025) * inside(P_[:, 1] + 0.05, 0.004)
            * inside(P_[:, 2] - 0.77, 0.004) * inside(0.62 - P_[:, 2], 0.004), "#C9BCD4")

    # The legs: the base's thighs a little fuller, thicker shins, bigger feet of three spread toes
    m.legs(thigh=(0.102, 0.11, 0.165), shin=(0.062, 0.047, 0.034),
           toes=((0.076, -0.13), (0.126, -0.146), (0.176, -0.122)), toe_r=0.027)

    # The arms: thick, bent at the elbows, great fists held in front of the belly
    ELBOW, FIST = np.array([0.205, -0.03, 0.645]), np.array([0.118, -0.172, 0.625])
    k.add("upper", tube([(0.15, 0.0, 0.785), (0.19, -0.012, 0.715), ELBOW], [0.044, 0.04, 0.034]), S, "arm{s}",
          mirror=True)
    k.add("fore", tube([ELBOW, (0.168, -0.11, 0.628), FIST + (0.012, 0.03, 0)], [0.036, 0.036, 0.034]), S, "arm{s}",
          mirror=True)
    k.add("fist", ell(FIST, (0.056, 0.058, 0.054), deform=boxy(2.4)), S, "arm{s}", mirror=True)
    for i, (dx, dz) in enumerate(((-0.026, 0.022), (-0.03, -0.004), (-0.022, -0.03))):
        # three round knuckles of the curled fingers, standing out on the fist's front and inner side
        k.add(f"knuckle{i}", ell(FIST + (dx * 1.15, -0.048, dz * 1.15), (0.026, 0.026, 0.025)), S, "arm{s}", mirror=True)
    k.add("thumb", ell(FIST + (-0.036, -0.014, 0.048), (0.024, 0.03, 0.021)), S, "arm{s}", mirror=True)

    # The head, its short ears, and the long horn swept back from the crest
    m.head()
    k.add("crest", blade([m.hd(p) for p in [(0, -0.005, 1.0), (0, 0.04, 1.04), (0, 0.1, 1.06), (0, 0.16, 1.05)]],
                         [0.034 * 1.2, 0.03 * 1.2, 0.022 * 1.2, 0.012], (1, 0, 0), 0.62), S, "head")

    # The tube from the back of the skull into a thick purple collar over both shoulders, open at the front
    k.add("pipe", tube([m.hd((0, 0.05, 0.955)), (0, 0.1, 0.9), (0, 0.112, 0.84)], [0.03, 0.032, 0.034]), P, "head")
    # a roll lying on the shoulders' tops, dipping behind the neck: a curve from behind, not a bar
    ring = [(0.07, -0.088, 0.755), (0.135, -0.05, 0.8), (0.155, 0.025, 0.835), (0.11, 0.09, 0.83), (0, 0.11, 0.8)]
    k.add("collar", tube(ring + [(-x, y, z) for x, y, z in ring[-2::-1]],
                         [0.03, 0.036, 0.04, 0.04, 0.04, 0.04, 0.04, 0.036, 0.03]), P, "body")

    # The tail: thinner than the base's, up behind the model's right, its end hooking back at shoulder height
    k.add("tail", tube([(0, 0.08, 0.5), (0.03, 0.18, 0.49), (0.08, 0.25, 0.56), (0.12, 0.27, 0.67),
                        (0.14, 0.27, 0.77), (0.15, 0.31, 0.83), (0.15, 0.35, 0.8)],
                       [0.046, 0.042, 0.036, 0.033, 0.032, 0.03, 0.028]), P, "tail")

    m.mouth()
    k.build(voxel=0.0065, band=0.004)
    k.eye_states(m.eyes)
    k.export(dst)


if __name__ == "__main__":
    mega_x(*args())
