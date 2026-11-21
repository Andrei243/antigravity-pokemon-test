# Mega Abomasnow sculpted on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/abomasnowmega.py -- <the export's Abomasnow-Mega.glb> <out.glb>
# Abomasnow's script (which builds as it is read) copied where it serves: the same snow white, teal of fir boughs,
# lilac eyes with a scowling cut and a pink mouth. The Mega's skeleton has no head: the face lies in the body. What the
# Mega changes, largest first: the whole of it is heaped with snow, a great round mass of flat white cards with stepped
# ends lying out over it like a fir's laden boughs, radiating from the face, teal boughs showing among them over the
# shoulders and back; four crystals of pale blue ice rising from the shoulders and the hips; short thick arms shaggy with
# cards reaching down to great teal hands on the ground beside the feet, clawed; short legs on teal clawed feet, the
# lavender-grey belly showing between them; two thick white brows in a V over the eyes, a pink beak of an open mouth
# below them.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, tri, unit, eye_look, args, slab, hull

src, out = args()
k = Kit(src, coat="fur")

SNOW, SHADE, TEAL, TEALD, MOUTH = "#F2F5F9", "#D6DDEB", "#3A8C87", "#2C6F6B", "#E07A78"
BELLY, ICE, ICEL = "#C3C8DD", "#94C8EC", "#C8E6F7"
C = np.array([0.0, 0.02, 0.52])          # the snow's middle
CR = np.array([0.29, 0.26, 0.29])
FC = np.array([0.0, -0.27, 0.62])        # the face's middle, on the front of the snow
rs = np.random.RandomState(7)

# --- the fused core: a round heap, the face flatter on its front, a lavender belly low between the legs ------------
k.add("core", ell(C, CR), SNOW, "body")
k.add("face", ell((0, -0.13, 0.61), (0.18, 0.15, 0.15)), SNOW, "body")
k.add("belly", ell((0, 0.0, 0.3), (0.22, 0.2, 0.16)), BELLY, "body")
k.paint(lambda P: inside(P[:, 2] - 0.31, 0.01) * inside(np.abs(P[:, 0]) - 0.24, 0.01), BELLY, on=["core", "belly"])

# --- legs: short and thick on teal clawed feet -----------------------------------------------------------------------
for s in (1, -1):
    sd = "R" if s > 0 else "L"
    k.add(f"leg{sd}", ell((0.12 * s, 0.0, 0.14), (0.085, 0.085, 0.1)), BELLY, f"leg{sd}")
    k.add(f"foot{sd}", ell((0.13 * s, -0.04, 0.045), (0.09, 0.105, 0.045)), TEAL, f"leg{sd}")
    for j, a in enumerate((-32, 0, 32)):
        r_ = math.radians(a + 10 * s)
        d = np.array([math.sin(r_), -math.cos(r_), -0.15])
        base = np.array([0.13 * s + math.sin(r_) * 0.05, -0.1, 0.035])
        tip = base + d * 0.06
        b1, b2 = np.cross(d, (0, 0, 1.0)), np.array([0, 0, 1.0])
        b1 /= np.linalg.norm(b1)
        k.add(f"toe{sd}{j}", hull([base + 0.022 * b1, base - 0.022 * b1, base + 0.02 * b2, base - 0.015 * b2, tip]),
              TEALD, f"leg{sd}", fuse=False, surface="leaf")
k.paint(lambda P: inside(P[:, 2] - 0.085, 0.004), TEAL, on=["leg"])

# --- arms: short and thick, out and down to great teal hands on the ground beside the feet -----------------------
SH = np.array([0.22, 0.0, 0.56])
HAND = np.array([0.37, -0.06, 0.12])
k.add("arm", tube([SH - (0.05, 0, 0), SH, (0.31, -0.03, 0.4), (0.355, -0.05, 0.24)], [0.085, 0.085, 0.08, 0.075]),
      SNOW, "arm{s}", mirror=True)
k.add("hand", ell(HAND, (0.1, 0.11, 0.105)), TEAL, "arm{s}", mirror=True)
for s in (1, -1):
    sd = "R" if s > 0 else "L"
    for j, (a, dz) in enumerate([(-40, -0.35), (-8, -0.4), (25, -0.35), (75, 0.2)]):
        r_ = math.radians(a * s)
        d = unit(math.sin(r_), -math.cos(r_), dz)
        base = HAND * np.array([s, 1, 1]) + d * 0.075
        tip = base + d * 0.1
        b1 = np.cross(d, (0, 0, 1.0)); b1 /= np.linalg.norm(b1); b2 = np.cross(b1, d)
        k.add(f"claw{sd}{j}", hull([base + 0.036 * b1, base - 0.036 * b1, base + 0.032 * b2, base - 0.032 * b2, tip]),
              TEALD, f"arm{sd}", fuse=False, surface="leaf")
# the snow over the wrist, its jagged edge onto the hand
k.paint(lambda P: inside(P[:, 2] - (0.19 + 0.022 * tri(np.arctan2(P[:, 1] - HAND[1], np.abs(P[:, 0]) - HAND[0]) * 1.6)),
                         0.0025) * inside(0.25 - np.abs(P[:, 0]), 0.003), TEAL, on=["arm", "hand"])
k.paint(lambda P: inside(P[:, 2] - 0.2, 0.003) * inside(0.25 - np.abs(P[:, 0]), 0.003), TEAL, on=["hand"])


# --- the cards of snow ------------------------------------------------------------------------------------------------
def card(name, root, d, n, L, w, col, bone, thick=0.03):
    """A flat card of snow from root along d, its face square to n, its end stepped."""
    d = d / np.linalg.norm(d)
    ez = np.cross(n, d); ez /= np.linalg.norm(ez)
    outline = [(-0.01, -w / 2), (L * 0.88, -w / 2), (L * 0.88, -w * 0.12), (L, -w * 0.12), (L, w / 2), (-0.01, w / 2)]
    if rs.rand() < 0.5:
        outline = [(a, -b) for a, b in outline]
    k.add(name, slab(outline, root, d, ez, thick), col, bone, fuse=False)


N = 230
g = math.pi * (3 - math.sqrt(5))
made = 0
for i in range(N):
    z = 1 - 2 * (i + 0.5) / N
    if z < -0.3:
        continue
    rr = math.sqrt(1 - z * z)
    u = np.array([rr * math.cos(g * i), rr * math.sin(g * i), z])
    p = C + u * CR
    nrm = u / CR; nrm /= np.linalg.norm(nrm)
    fd = p - FC
    if p[1] < -0.12 and np.linalg.norm((fd[0], fd[2] - 0.02)) < 0.16:
        continue                      # the face is left clear
    t = fd - nrm * (fd @ nrm) + np.array([0, 0, -0.55]) * (1 - abs(nrm[1]) * 0.3)
    t = t - nrm * (t @ nrm)
    if np.linalg.norm(t) < 1e-3:
        t = np.array([0, 0, -1.0]) - nrm * (-nrm[2])
    t /= np.linalg.norm(t)
    d = t + nrm * 0.45
    L = 0.13 + 0.07 * rs.rand()
    teal = abs(p[0]) > 0.1 and p[2] > 0.5 and p[1] > -0.12 and (i % 3 == 0)
    col = TEAL if teal else (SHADE if i % 4 == 1 else SNOW)
    card(f"card{i}", p - nrm * 0.02 + nrm * (0.0 if teal else 0.012), d, nrm, L * (1.15 if teal else 1.0),
         0.055 + 0.02 * rs.rand(), col, "body")
    made += 1
print("KIT: cards", made)

# cards hanging along the arms
for s in (1, -1):
    sd = "R" if s > 0 else "L"
    for j, t in enumerate((0.2, 0.5, 0.8)):
        a = SH + (np.array([0.355, -0.05, 0.24]) - SH) * t
        a = a * np.array([s, 1, 1])
        for m, ang in enumerate((-70, 0, 70, 140)):
            r_ = math.radians(ang)
            nrm = np.array([math.cos(r_) * s * 0.7 + 0.3 * s, -math.sin(r_) * 0.9, 0.2]); nrm /= np.linalg.norm(nrm)
            root = a + nrm * 0.06
            d = np.array([0.3 * s, 0, -1.0]) + nrm * 0.5
            card(f"armcard{sd}{j}{m}", root, d, nrm, 0.13, 0.055, SNOW if (j + m) % 3 else SHADE, f"arm{sd}")

# --- crystals of ice rising from the shoulders and the hips ------------------------------------------------------------
def crystal(name, root, tip, r, col):
    root, tip = np.array(root, float), np.array(tip, float)
    d = tip - root; d /= np.linalg.norm(d)
    a = np.cross(d, (0, 1.0, 0)); a /= np.linalg.norm(a); b = np.cross(d, a)
    ring = [root + r * (math.cos(t) * a + math.sin(t) * b) for t in np.linspace(0, 2 * math.pi, 6, endpoint=False)]
    mid = [root + (tip - root) * 0.62 + r * 1.05 * (math.cos(t) * a + math.sin(t) * b)
           for t in np.linspace(0, 2 * math.pi, 6, endpoint=False)]
    k.add(name, hull(ring + mid + [tip]), col, "body", fuse=False, surface="shell")
for s in (1, -1):
    sd = "R" if s > 0 else "L"
    crystal(f"ice{sd}0", (0.2 * s, 0.06, 0.72), (0.4 * s, 0.1, 0.97), 0.05, ICE)
    crystal(f"ice{sd}1", (0.12 * s, 0.12, 0.76), (0.2 * s, 0.2, 0.93), 0.035, ICEL)
    crystal(f"ice{sd}2", (0.26 * s, 0.08, 0.38), (0.45 * s, 0.12, 0.45), 0.035, ICE)

# --- the face: brows, the mouth ----------------------------------------------------------------------------------------
FN = unit(0, -1, 0.15)
for s in (1, -1):
    # a thick white brow from over the nose up and out, two cards over it rising out
    inner = np.array([0.02 * s, -0.285, 0.665])
    d = unit(0.85 * s, 0.15, 0.5)
    card(f"brow{'R' if s > 0 else 'L'}", inner, d, FN, 0.2, 0.05, SNOW, "body", thick=0.034)
    card(f"browtuft{'R' if s > 0 else 'L'}", inner + np.array([0.06 * s, 0.01, 0.06]), unit(0.5 * s, 0.1, 0.85), FN,
         0.12, 0.045, SHADE, "body")
# the open mouth: a pink beak pointing down between the eyes
k.add("mouth", hull([(-0.035, -0.29, 0.6), (0.035, -0.29, 0.6), (0, -0.32, 0.505), (0, -0.25, 0.6),
                     (-0.02, -0.26, 0.53), (0.02, -0.26, 0.53)]), MOUTH, "body", fuse=False, surface="shell")

# --- the eyes: Abomasnow's, pale lilac in a dark rim, the top cut sloping down to the nose ----------------------------
RIM, IRIS, PUP, LID = srgb("#35283F"), srgb("#CDB8E2"), srgb("#3A2A4A"), srgb(SNOW)
def eye(u, v):
    c = np.tile(RIM, (len(u), 1))
    top = 0.4 + 0.35 * u
    c[(u ** 2 + v ** 2 < 0.78 ** 2) & (v < top - 0.2)] = IRIS
    c[((u + 0.05) / 0.25) ** 2 + ((v + 0.14) / 0.27) ** 2 < 1] = PUP
    c[v > top] = LID
    return c
de = unit(0.3, -0.94, 0.12)
def eyes(state):
    k.decal((0.075, -0.275, 0.635), de, (0.05, 0.046), eye_look(state, eye, LID, RIM, line=0.2), eps=0.005, rings=20,
            segs=56, mirror=True)

k.build(voxel=0.0078)
k.eye_states(eyes)
k.export(out)
