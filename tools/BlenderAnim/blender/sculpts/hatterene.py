# Hatterene sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/hatterene.py -- <the export's Hatterene.glb> <out.glb>
# Grown from hatenna.py and hattrem.py: the line's colours, white spots on the hair, the hollow its face looks out of,
# the glossy black eyes with a white glint.
#
# The design, from the references: a tall witch of hair. A great round hood of pale blue, white spots on its side,
# with an oval opening high at its front out of which a small pale lilac face looks: narrow black almond eyes with a
# glint, small pink marks over them, a tiny mouth; a little white striped ribbon under the chin. Over the hood a broad
# round brim tilted up at the front and toward one side, and from it a straight pointed cone leaning the other way.
# Below the hood the hair turns pink and twists into a thick braided column, a dark-edged pink sparkle on its chest,
# then flares into a bell of a gown that fades to white, white claw-like crescents curling up on its sides and a white
# lobe up its front, its hem a fringe of white tendrils on the ground. From under the brim on one side hangs a long
# blue tress through a pink cuff to a round blue hand with four long claws. The skeleton is a body, a head and armR
# (the tress, on +X: the art hangs it on the side the turntable shows as the viewer's left; the skeleton decides).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, polygon, lathe, outward, sibling, args

h = sibling("hatenna")
PINK, PINKDEEP, BLUE, BLUEDEEP, WHITE, INKH = h.PINK, h.PINKDEEP, h.BLUE, h.BLUEDEEP, h.WHITE, h.INKH
on_ell, spot = h.on_ell, h.spot
GOWN, FACE, SHADE, MARK = "#F2B6C6", "#ECE2F0", "#86A9D0", "#E58AA6"

# --- the frame (joints: body (0, 0, 0.5), head (0, -0.01, 0.58), armR (0.08, -0.01, 0.67)) -------------------------
HC = np.array([0.0, 0.01, 0.6])           # the hood
HR = np.array([0.205, 0.19, 0.205])        # a great round mass, 0.4 of the height wide
FC = np.array([0.0, -0.118, 0.655])        # the face in its opening
FR = np.array([0.09, 0.062, 0.102])
BRIM = np.array([0.0, 0.02, 0.8])


def egg(V):
    """The hood's lower half drawn in a little toward the waist, as an egg."""
    V = V.copy()
    w = 1 - 0.22 * np.clip(-V[:, 2], 0, 1) ** 1.6
    V[:, 0] *= w; V[:, 1] *= w
    return window(V)


def window(V):
    """The hood's front pushed in round an oval opening high on its front, where the face looks out."""
    V = V.copy()
    m = inside((V[:, 0] / 0.47) ** 2 + ((V[:, 2] - 0.2) / 0.52) ** 2 - 1, 0.3)
    m = m * smoothstep(0.0, -0.4, V[:, 1])
    return V * (1 - 0.3 * m)[:, None]


def hatterene(src, out):
    k = Kit(src, coat="fur")

    # --- the hood, its opening's inside a shade deeper, turning pink toward its lower edge
    k.add("hood", ell(HC, HR, deform=egg, sub=5), BLUE, "head")
    def deep(P):
        Q = (P - HC) / HR
        return inside(np.linalg.norm(Q, axis=1) - 0.9, 0.03) * inside(Q[:, 1] + 0.25, 0.05) * inside(-Q[:, 2] - 0.55)
    k.paint(deep, SHADE, on=["hood"])
    k.paint(lambda P: smoothstep(0.5, 0.42, P[:, 2]), GOWN, on=["hood"])
    k.add("face", ell(FC, FR), FACE, "head")

    # --- the braided column: a pinched waist under the hood, pink bulges twisted about a core
    k.add("core", tube([(0, 0.01, 0.46), (0, 0.01, 0.34), (0, 0.01, 0.2)], [0.065, 0.062, 0.08]), GOWN, "body")
    for i, (z, dx, r, tilt) in enumerate(((0.405, 0.0, 0.088, 22), (0.335, 0.012, 0.09, -24))):
        k.add(f"twist{i}", ell((dx, 0.01, z), (r, r * 0.9, 0.058), rot=(0, tilt, 35 * (i - 1))), GOWN, "body")

    # --- the gown's bell, swelling to 0.38 wide at the foot, fading to white, its hem a fringe of white tendrils
    def bell(V):
        V = V.copy()
        V[:, 2] = np.maximum(V[:, 2], -0.75)
        w = 1 - 0.42 * np.clip(V[:, 2], 0, 1) ** 1.3 + 0.5 * np.clip(-V[:, 2], 0, 1) ** 1.5
        V[:, 0] *= w; V[:, 1] *= w
        return V
    k.add("bell", ell((0, 0.01, 0.15), (0.185, 0.17, 0.155), deform=bell), GOWN, "body")
    for i in range(13):
        a = 2 * math.pi * i / 13 + 0.15
        c, s = math.sin(a), -math.cos(a)
        bend = 0.016 * math.sin(3 * a)
        pts = [(0.16 * c, 0.01 + 0.15 * s, 0.035), (0.215 * c, 0.01 + 0.2 * s, 0.016),
               (0.26 * c + bend * s, 0.01 + 0.24 * s - bend * c, 0.02)]
        k.add(f"tendril{i}", tube(pts, [0.03, 0.021, 0.01]), WHITE, "body")
    k.paint(lambda P: smoothstep(0.17, 0.07, P[:, 2]), WHITE, on=["bell", "core"])
    # white claws curling up on the gown's sides and a white lobe up its front
    def claws(P):
        ang = np.arctan2(P[:, 0], -(P[:, 1] - 0.01))
        x = ang * 0.16; z = P[:, 2]
        m = np.zeros(len(P))
        for s in (1, -1):
            cx = s * 0.12
            outer = np.hypot(x - cx, (z - 0.2) / 1.1) - 0.06
            inner = np.hypot(x - (cx + s * 0.03), (z - 0.228) / 1.1) - 0.053
            m = np.maximum(m, inside(outer, 0.003) * (1 - inside(inner, 0.003)))
        lobe = ((x / 0.055) ** 2 + ((z - 0.12) / 0.11) ** 2) - 1
        m = np.maximum(m, inside(lobe, 0.06))
        return m * inside(z - 0.29) * inside(P[:, 1] - 0.05)
    k.paint(claws, WHITE, on=["bell", "core", "twist"])

    # --- the sparkle on the chest: a pink four-pointed star edged in deep pink
    star = polygon([(math.cos(a) * (1.0 if j % 2 == 0 else 0.32), math.sin(a) * (1.0 if j % 2 == 0 else 0.32))
                    for j, a in enumerate(np.linspace(math.pi / 2, math.pi / 2 + 2 * math.pi, 8, endpoint=False))])
    sp = np.array([0.01, -0.072, 0.39])
    k.decal(sp, unit(0.1, -1, 0.05), (0.026, 0.026), BLUEDEEP, outline=star, eps=0.002, rings=10, segs=96)
    k.decal(sp, unit(0.1, -1, 0.05), (0.016, 0.016), MARK, outline=star, eps=0.0035, rings=10, segs=96)

    # --- the ribbon under the chin: white bars across blue
    Wc, Bc = srgb(WHITE), srgb(BLUE)
    def ribbon(u, v):
        c = np.tile(Bc, (len(u), 1))
        c[np.cos((u * 0.8 + v) * 3.2 * math.pi) > 0.1] = Wc
        return c
    rp, rn = on_ell(HC, HR, (0.12, -0.75, -0.62))
    k.decal(rp, rn, (0.038, 0.022), ribbon, eps=0.002, rings=10, segs=48)

    # --- white spots on the hood's -X side
    for d_, s in (((-0.72, -0.45, 0.45), 0.034), ((-0.88, -0.2, 0.25), 0.026), ((-0.5, -0.5, 0.68), 0.022),
                  ((-0.78, 0.3, 0.45), 0.029), ((-0.35, 0.2, 0.9), 0.024)):
        spot(k, HC, HR, d_, s)

    # --- the face's marks: a small pink mark over each eye, a tiny mouth
    for sx in (1, -1):
        mp, mn = on_ell(FC, FR, (0.36 * sx, -0.75, 0.55))
        k.decal(mp, mn, (0.011, 0.009), MARK, eps=0.002, rings=8, segs=32)
    I, L = srgb(INKH), srgb(FACE)
    def mouth(u, v):
        c = np.tile(L, (len(u), 1))
        c[(np.abs(v) < 0.4) & (np.abs(u) < 0.8)] = I
        return c
    mp, mn = on_ell(FC, FR, (0, -1, -0.45))
    k.decal(mp, mn, (0.011, 0.005), mouth, eps=0.002, rings=8, segs=40)

    # --- the hat: a broad thick brim, wider than the hood, tilted up at the front and toward -X's side; a cone thick
    #     at its foot leaning to -X
    bd = unit(0.22, 0.15, 1)
    k.add("brim", lathe(BRIM, bd, [(0, -0.018), (0.22, -0.018), (0.24, -0.006), (0.234, 0.012), (0.14, 0.02),
                                   (0, 0.022)], seg=72), BLUE, "head", fuse=False)
    cd = unit(-0.4, 0.12, 1)
    k.add("cone", lathe(BRIM + bd * 0.012, cd, [(0, 0), (0.132, 0), (0.126, 0.03), (0.075, 0.11), (0.03, 0.19),
                                                 (0, 0.23)], seg=56), BLUE, "head", fuse=False)

    # --- the tress on armR: a thick rope from under the brim's +X edge, hanging clear of the hood and the gown,
    #     through a pink cuff to a round hand with four claws
    tress = [(0.14, -0.01, 0.73), (0.212, -0.02, 0.715), (0.25, -0.035, 0.63), (0.256, -0.045, 0.5),
             (0.256, -0.05, 0.42)]
    k.add("tress", tube(tress, [0.042, 0.042, 0.042, 0.041, 0.04]), BLUE, "armR")
    k.add("cuff", ell((0.256, -0.05, 0.395), (0.066, 0.066, 0.038)), PINK, "armR")
    hc = np.array([0.256, -0.05, 0.315])
    k.add("hand", ell(hc, (0.075, 0.072, 0.072)), BLUE, "armR")
    for i, (dx, dy) in enumerate(((-0.4, -0.45), (-0.05, -0.6), (0.3, -0.5), (0.45, -0.35))):
        dv = np.array([dx, dy, 0.0]); dv /= np.linalg.norm(dv)
        pts = [hc + dv * 0.045 + (0, 0, -0.045), hc + dv * 0.08 + (0, 0, -0.12), hc + dv * 0.1 + (0, 0, -0.2)]
        k.add(f"claw{i}", tube(pts, [0.026, 0.02, 0.01]), BLUE, "armR")

    k.build(voxel=0.0072, smooth=4, band=0.007, wband=0.05)

    # --- the eyes: narrow black almonds, their outer corners raised, a white glint
    K, W = srgb("#141018"), srgb("#FFFFFF")
    def eye(u, v):
        c = np.tile(L, (len(u), 1))
        vv = v - 0.22 * u
        c[np.abs(vv) < 0.82 * np.clip(1 - u * u, 0, 1) ** 0.6] = K
        c[((u + 0.15) / 0.22) ** 2 + ((vv - 0.2) / 0.3) ** 2 < 1] = W
        return c
    ep, en = on_ell(FC, FR, (0.42, -0.88, 0.1))
    def lay(state):
        k.decal(ep, en, (0.029, 0.02), eye_look(state, eye, L, I, line=0.16), eps=0.0018, rings=20, segs=56,
                mirror=True)

    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    hatterene(*args())
