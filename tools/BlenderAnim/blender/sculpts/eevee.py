# Eevee sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/eevee.py -- <the export's Eevee.glb> <out.glb>
# A small fox-like quadruped in warm brown fur: a large round head with a little spiky crown, long pointed ears lined
# dark brown, big dark-brown eyes with a white glint, a small dark nose and a little cat's mouth; a full ruff of cream
# fur round the neck in pointed tufts; a short body on four slim legs; a big bushy tail rising behind, tipped cream.
#
# Written to be grown from: Eevee's seven evolutions take this frame (the body, the legs, the neck and head, the way
# the eyes, nose and mouth are made) and change what is theirs. Each part of the frame is a constant or a small
# function below, in the order a grown script would want to change them.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, tri, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

# --- colours ----------------------------------------------------------------------------------------------------------
BROWN = "#C8894C"          # the coat: between the drawing's warm brown and the 3D model's orange-brown
CREAM = "#F3E3B6"          # the ruff and the tail's tip
LINING = "#6A3A1E"         # inside the ears
EYE, PUPIL = "#3E2416", "#170C08"   # dark-brown eyes, a darker middle
NOSE, INK = "#2A1A14", "#2A1810"

# --- the frame (the skeleton's joints: body 0.17 up, head joint (0, -0.08, 0.27), ears (+-0.05, -0.07, 0.40),
#     forelegs (+-0.05, -0.06, 0.13), hind legs (+-0.05, 0.10, 0.13), tail (0, 0.14, 0.20)) ------------------------------
# Proportions measured on the turntable against the 3D reference (reference / this sculpt): the head, chin to skull
# top, of the whole height 0.34 / 0.32; the ears' rise above the skull against the head's height 0.9-1.0 / 0.8; the
# legs showing under the ruff, front view, of the height 0.22 / 0.16 (belly to ground from the side 0.21); the tail's
# height against the head's 1.27 / 1.05; an eye's height against the head's 0.35-0.4 / 0.34. The game's shots asked
# for a larger head and short legs under a deep body, as the drawing has them, more than the 3D model.
H = np.array([0, -0.102, 0.36])          # the head's centre, over and in front of its joint
HR = np.array([0.112, 0.099, 0.103])     # its radii: a little wider than tall (12% over the first build, about the bone)
LEG_X = 0.052                            # the legs' distance from the midline
FORE_Y, HIND_Y = -0.065, 0.1             # where the fore and hind legs stand
BODY_Z = 0.175                           # the body's middle height


def head_shape(V):
    """The head: round, a little fuller at the cheeks below its middle, so the face reads soft."""
    V = V.copy()
    V[:, 0] *= 1 + 0.06 * np.clip(-V[:, 2], 0, 1) * (1 - np.clip(-V[:, 2], 0, 1))
    return V


def add_head():
    k.add("head", ell(H, HR, deform=head_shape), BROWN, "head")
    # A small muzzle standing a little forward under the eyes, for the nose and mouth to sit on
    k.add("muzzle", ell(H + (0, -0.081, -0.034), (0.047, 0.034, 0.031)), BROWN, "head")
    # The neck from the shoulders up into the head, hidden by the ruff in front
    k.add("neck", tube([(0, -0.04, 0.2), (0, -0.07, 0.25), (0, -0.09, 0.3)], [0.048, 0.046, 0.05]), BROWN, "head")


def add_body():
    # A short body: a chest, a middle and a rump, one smooth line from the shoulders to the tail
    # Deep enough that its belly covers the tops of the legs: the legs show short under it, as in the pictures
    k.add("chest", ell((0, -0.045, BODY_Z - 0.008), (0.07, 0.072, 0.092)), BROWN, "body")
    k.add("middle", ell((0, 0.03, BODY_Z - 0.012), (0.064, 0.1, 0.085)), BROWN, "body")
    k.add("rump", ell((0, 0.095, BODY_Z - 0.01), (0.068, 0.064, 0.085)), BROWN, "body")


def add_legs(color=BROWN):
    # Slim straight legs with small rounded paws; the hind ones with fuller thighs
    k.add("leg", tube([(LEG_X - 0.004, FORE_Y + 0.005, 0.16), (LEG_X, FORE_Y, 0.09), (LEG_X + 0.002, FORE_Y - 0.004, 0.03)],
                      [0.039, 0.033, 0.03]), color, "leg{s}", mirror=True)
    k.add("paw", ell((LEG_X + 0.003, FORE_Y - 0.016, 0.021), (0.035, 0.043, 0.023)), color, "leg{s}", mirror=True)
    k.add("thigh", ell((LEG_X - 0.004, HIND_Y + 0.005, 0.145), (0.043, 0.056, 0.064)), color, "hind{s}", mirror=True)
    k.add("hind", tube([(LEG_X, HIND_Y + 0.01, 0.13), (LEG_X + 0.002, HIND_Y + 0.008, 0.08),
                        (LEG_X + 0.002, HIND_Y - 0.002, 0.03)], [0.037, 0.032, 0.03]), color, "hind{s}", mirror=True)
    k.add("hpaw", ell((LEG_X + 0.003, HIND_Y - 0.014, 0.021), (0.035, 0.043, 0.023)), color, "hind{s}", mirror=True)


def along(P, pts, n=200):
    """Nearest place on a polyline (|x| taken, so a mirrored part's both sides): its share of the length and the offset."""
    pts = np.array(pts, float)
    seg = np.linalg.norm(np.diff(pts, axis=0), axis=1)
    cum = np.concatenate([[0], np.cumsum(seg)]); L = cum[-1]
    s = np.linspace(0, L, n)
    line = np.stack([np.interp(s, cum, pts[:, i]) for i in range(3)], axis=1)
    Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
    j = np.concatenate([((Q[a:a + 4000, None, :] - line[None]) ** 2).sum(2).argmin(1) for a in range(0, len(Q), 4000)])
    return s[j] / L, Q - line[j]


# --- the ears: long, wide at the root and pointed, leaning out from the top corners of the head and a little back;
#     flat front to back, the dark lining on their front faces with a brown rim round it ------------------------------
EAR = [(0.05, -0.086, 0.41), (0.083, -0.08, 0.475), (0.115, -0.074, 0.54), (0.143, -0.068, 0.6)]
EAR_R = [0.046, 0.045, 0.03, 0.004]


def add_ears():
    k.add("ear", blade(EAR, EAR_R, (0, 1, 0), 0.32), BROWN, "ear{s}", mirror=True)
    d = unit(*(np.array(EAR[-1]) - np.array(EAR[0])))
    across = np.cross(d, (0, 1.0, 0)); across /= np.linalg.norm(across)

    def lining(P):
        t, off = along(P, EAR)
        half = 0.034 * np.sin(np.pi * np.clip((t + 0.12) / 1.0, 0, 1)) ** 0.7     # the lining's half width along the ear
        w = off @ across
        front = -off[:, 1]
        return (inside(np.abs(w) - half, 0.0025) * inside(-front + 0.002, 0.002) * inside(t - 0.9, 0.01)
                * (P[:, 2] > 0.4))
    k.paint(lining, LINING, on=["ear"])


# --- the ruff: a full collar of cream fur round the neck, deepest in front, in soft pointed tufts along its lower
#     edge and out at its sides, thinner round the back of the neck ---------------------------------------------------
RUFF = np.array([0, -0.1, 0.222])       # the middle of its front mass, hanging low over the chest


def add_ruff():
    # One full mass on the chest under the chin: a bib, not a rolled ring
    k.add("ruff", ell(RUFF, (0.084, 0.074, 0.078)), CREAM, "body")
    # Soft lumps over its front in two rows, so it reads as fur
    for i, a in enumerate(np.linspace(-1.25, 1.25, 5)):
        for row, (z, rr, out) in enumerate(((0.247, 0.046, 0.04), (0.195, 0.05, 0.046))):
            aa = a * (1 - 0.15 * row)
            c = (0.075 * math.sin(aa), RUFF[1] + 0.012 - out * math.cos(aa), z)
            k.add(f"ruffball{row}{i}", ell(c, (rr, rr * 0.9, rr * 0.9)), CREAM, "body")
    # Behind, low and soft: one broad flat band over the shoulders and three broad tufts lying back down the
    # neck (beads in a row there read as a rolled collar)
    k.add("ruffback", ell((0, -0.035, 0.238), (0.074, 0.05, 0.03), rot=(-20, 0, 0)), CREAM, "body")
    for i, x in enumerate((-0.045, 0.0, 0.045)):
        base = np.array((x, -0.03, 0.245))
        d = unit(x * 4, 1, -0.55)
        k.add(f"ruffback{i}", blade([base, base + d * 0.03, base + d * 0.05], [0.032, 0.02, 0.004], (0, 0.5, 1), 0.45),
              CREAM, "body")
    # Pointed tufts along the lower edge, pointing down and a little out, the middle ones longest
    for i, a in enumerate(np.linspace(-1.3, 1.3, 7)):
        base = np.array((0.07 * math.sin(a), RUFF[1] + 0.005 - 0.055 * math.cos(a), 0.165))
        d = unit(0.4 * math.sin(a), -0.35 * math.cos(a), -1.0)
        ln = 0.06 - 0.012 * abs(a)
        k.add(f"rufftuft{i}", blade([base, base + d * ln * 0.55, base + d * ln], [0.028, 0.017, 0.003],
                                    (math.sin(a), -math.cos(a), 0.0), 0.55),
              CREAM, "body")
    # A tuft at each side, pointing out and a little down
    base = np.array((0.085, -0.1, 0.235))
    d = unit(1, -0.2, -0.3)
    k.add("ruffside", blade([base, base + d * 0.025, base + d * 0.045], [0.026, 0.016, 0.003], (0, 1, 0), 0.55),
          CREAM, "body", mirror=True)


# --- the crown: a few short spikes of fur on top of the head ------------------------------------------------------
def add_crown():
    for i, (x, h, lean) in enumerate(((0.0, 0.06, 0.0), (0.024, 0.05, 0.35), (0.045, 0.038, 0.75))):
        base = H + (x, -0.005, HR[2] - 0.012)
        d = unit(lean, 0.15, 1)
        k.add(f"crown{i}", blade([base, base + d * h * 0.6, base + d * h], [0.02, 0.011, 0.002], (0, 1, 0), 0.6),
              BROWN, "head", mirror=x > 0)


# --- the tail: a big bushy plume from the rump, back and up, swung out to the model's +X side (its line about 40
#     degrees off straight back) so it shows beside the head from the front and the body shows beside it from behind;
#     flattened across its swing; its tip cream with a jagged edge -------------------------------------------------
TAIL = [(0.0, 0.13, 0.205), (0.03, 0.175, 0.245), (0.075, 0.205, 0.305), (0.105, 0.215, 0.37), (0.125, 0.21, 0.43)]
TAIL_R = [0.032, 0.07, 0.086, 0.07, 0.008]


def add_tail():
    k.add("tail", blade(TAIL, TAIL_R, unit(1, -0.6, 0), 0.68), BROWN, "tail")
    def tip(P):
        t, off = along(P, TAIL)
        jag = 0.6 + 0.06 * tri(np.arctan2(off[:, 2], off[:, 0] + 1e-9) * 3 / math.pi)
        return inside(jag - t, 0.004) * (np.linalg.norm(off, axis=1) < 0.1) * (P[:, 1] > 0.15)
    k.paint(tip, CREAM, on=["tail"])


# --- the face: patches on the fused head, laid before the body's build ------------------------------------------------
CB, CI = srgb(BROWN), srgb(INK)


def add_nose(at=H + (0, -0.112, -0.025), half=(0.012, 0.009), color=NOSE):
    """A small dark nose, a triangle point down, at the tip of the muzzle."""
    k.decal(at, unit(0, -1, 0.1), half, color, eps=0.003, outline=polygon([(-1, 0.7), (1, 0.7), (0, -1)]),
            rings=6, segs=24)


def mouth(u, v):
    """A little cat's mouth: one clean line, dipping in the middle and turning up at the corners."""
    c = np.tile(CB, (len(u), 1))
    c[(np.abs(v - (-0.2 + 1.4 * (np.abs(u) - 0.45) ** 2)) < 0.26) & (np.abs(u) < 0.88)] = CI
    return c


def add_mouth(at=H + (0, -0.107, -0.053), half=(0.024, 0.0145), look=mouth):
    k.decal(at, unit(0, -1, -0.2), half, look, eps=0.003, rings=16, segs=72)


# The eyes: big ovals, taller than wide, dark brown with a darker middle and a white glint up and out; no rim, the
# brown of the face is their lid. A grown form changes EYE_HALF, EYE_DIR or eye() and keeps the rest.
EYE_DIR = unit(0.47, -0.88, -0.02)       # set well apart, as the user asks of round heads
EYE_HALF = (0.025, 0.035)
CE, CP = srgb(EYE), srgb(PUPIL)


def eye(u, v):
    c = np.tile(CE, (len(u), 1))
    c[((u + 0.05) / 0.62) ** 2 + ((v + 0.08) / 0.7) ** 2 < 1] = CP
    c[((u - 0.2) / 0.3) ** 2 + ((v - 0.4) / 0.26) ** 2 < 1] = (1, 1, 1)
    return c
E = H + EYE_DIR * 0.0965 + np.array([0, 0, 0.0045])


def eyes(state):
    k.decal(E, EYE_DIR, EYE_HALF, eye_look(state, eye, CB, CI), eps=0.004, rings=20, segs=56, mirror=True)


add_body()
add_legs()
add_head()
add_ears()
add_ruff()
add_crown()
add_tail()
add_nose()
add_mouth()

k.build(voxel=0.0052)
k.eye_states(eyes)
k.export(out)

