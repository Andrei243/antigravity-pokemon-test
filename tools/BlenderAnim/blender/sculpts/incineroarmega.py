# Mega Incineroar (a Mega of our own, plan 06 · R20b) sculpted on the skeleton and clips of the game's own export of
# the form (sculptkit.py), grown from incineroar.py (sibling("incineroar"): its legs, arms, mane, head, mouth, tail and
# eyes as they are; its trunk copied here and broadened; its belt of fire taken away):
#   blender --background --factory-startup --python sculpts/incineroarmega.py -- <the export's Incineroar-Mega.glb> <out.glb>
# No pictures: the game's own sculptor (PokemonModels.Ours.Megas.cs, IncineroarMega) is the design. Broader in the
# chest and shoulders; its belt of fire become a champion's belt, a wide dark band with a great gold plate in front
# bearing a paw print and two small gold plates beside it; flames rising from the band behind (the largest change from
# behind, the plate from the front); red bracers on its forearms; the black mask spread up over the brow.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, sheet, outward, ring_band, inside, unit, args, sibling

ib = sibling("incineroar")
lt = ib.lt
RED, BLACK, GREY, INK = ib.RED, ib.BLACK, ib.GREY, ib.INK
GOLD, GOLDDARK, BAND, BRACER = "#ECBE46", "#94681E", "#3C2C2A", "#A82424"
BELT = ib.BELT
GROW = np.array([1.16, 1.1, 1.06])        # the chest and belly broadened about the belt's middle
WAIST = np.array([0.0, 0.012, BELT])
BA, BB = 0.142, 0.118                     # the belt's half widths across and front to back


def add_body(k):
    """Incineroar's trunk, the grey chest and belly broadened, the hips as they were."""
    def g(c, r):
        return WAIST + (np.array(c) - WAIST) * GROW, np.array(r) * GROW
    k.add("chest", ell(*g((0, -0.005, 0.53), (0.155, 0.112, 0.1))), GREY, "body")
    k.add("abs", ell(*g((0, -0.0, 0.435), (0.112, 0.092, 0.1))), GREY, "body")
    k.add("hips", ell((0, 0.012, 0.33), (0.13, 0.102, 0.075)), RED, "body")
    k.add("neck", tube([(0, -0.02, 0.58), (0, -0.045, 0.65)], [0.085, 0.08]), RED, "head")
    k.paint(lambda P: inside(P[:, 2] - (BELT - 0.004), 0.003), RED, on=["abs", "hips", "chest"])
    def marks(P):
        x, y, z = P[:, 0] / GROW[0], P[:, 1], P[:, 2]
        ax = np.abs(x)
        front = (y < -0.04)
        across = inside(np.abs(z - 0.5) - 0.018 + 0.12 * np.clip(ax - 0.05, 0, 1), 0.003) * inside(ax - 0.1, 0.003)
        down = inside(ax - 0.015, 0.003) * inside(z - 0.51, 0.003) * inside(0.405 - z, 0.003)
        slant = inside(np.abs(z - (0.455 - 0.5 * (ax - 0.01))) - 0.012, 0.003) * inside(ax - 0.07, 0.003)
        return np.maximum.reduce([across, down, slant]) * front
    k.paint(marks, BLACK)
    k.paint(lambda P: inside(-0.015 - P[:, 1], 0.004) * inside(BELT - P[:, 2], 0.003), BLACK, on=["chest", "abs"])


def add_belt(k):
    """The champion's belt: a wide dark band round the waist, a great gold plate in front with a paw print, two small
    plates beside it, and tongues of flame rising from the band behind."""
    def at(u, v):
        th = 2 * math.pi * u + math.pi / 2                 # the seam behind
        z = BELT - 0.03 + 0.06 * v
        return np.stack([BA * np.cos(th), WAIST[1] + BB * np.sin(th), z], axis=-1)
    k.add("beltband", outward(sheet(at, 73, 4, 0.014)), BAND, "body", fuse=False, surface="leather")
    front = np.array([0.0, WAIST[1] - BB - 0.012, BELT])
    k.add("beltplate", hull([front + (sx * 0.072, dy, dz) for sx in (-1, 1) for dy in (-0.008, 0.012)
                             for dz in (-0.05, 0.05)] + [front + (sx * 0.05, -0.016, dz) for sx in (-1, 1) for dz in (-0.035, 0.035)]),
          GOLD, "body", fuse=False, surface="metal")
    face = front + (0, -0.017, 0)
    pads = [ell(face + (0, 0, -0.012), (0.019, 0.004, 0.015), sub=3)]
    pads += [ell(face + (x, 0, z), (0.008, 0.004, 0.009), sub=3) for x, z in
             ((-0.026, 0.01), (-0.009, 0.022), (0.009, 0.022), (0.026, 0.01))]
    for i, pd in enumerate(pads):
        k.add(f"paw{i}", outward(pd), GOLDDARK, "body", fuse=False, surface="metal")
    for sx in (1, -1):
        th = math.radians(-90 + 40 * sx)
        c = np.array([BA * math.cos(th), WAIST[1] + BB * math.sin(th), BELT])
        out = unit(math.cos(th) / BA, math.sin(th) / BB, 0)
        tang = np.cross((0, 0, 1.0), out)
        k.add(f"beltstud{'RL'[sx < 0]}", hull([c + out * o + tang * t + (0, 0, z) for o in (0.004, 0.016)
                                               for t in (-0.02, 0.02) for z in (-0.028, 0.028)]),
              GOLD, "body", fuse=False, surface="metal")
    red, mid, core = ib.Pieces(), ib.Pieces(), ib.Pieces()
    for i in range(7):
        a = math.radians(110 + 23 * i)                     # round the back, from one hip to the other
        o = np.array([math.sin(a), -math.cos(a), 0.0])
        base = np.array([BA * 0.94 * o[0], WAIST[1] + BB * 0.94 * o[1], BELT + 0.02])
        ln = 0.15 + 0.04 * (i % 2) - 0.02 * abs(i - 3) / 3
        (red if i % 2 else mid).put(ib.tongue(base, o * 0.3 + (0, 0, 0.95), ln, 0.04, bend=0.08))
        core.put(ib.tongue(base + o * 0.012 + (0, 0, 0.005), o * 0.25 + (0, 0, 0.95), ln * 0.6, 0.027))
    k.add("flamered", red.shape(), ib.FLAME_RED, "body", fuse=False, glow=ib.FLAME_RED)
    k.add("flamemid", mid.shape(), ib.FLAME, "body", fuse=False, glow=ib.FLAME)
    k.add("flamecore", core.shape(), ib.FLAME_CORE, "body", fuse=False)


def add_bracers(k):
    """Two dark red bracers round each forearm, either side of its black band."""
    for sx in (1, -1):
        m = np.array([sx, 1, 1.0])
        el, wr = ib.EL * m, ib.WR * m
        ax = unit(*(wr - el))
        for j, t in enumerate((0.3, 0.8)):
            c = el + (wr - el) * t
            rr = 0.052 + (0.048 - 0.052) * t + 0.01
            k.add(f"bracer{'RL'[sx < 0]}{j}", ring_band(c, ax, rr, 0.034, thick=0.016), BRACER, f"arm{'RL'[sx < 0]}",
                  fuse=False, surface="leather")


def brow(P):
    """The black mask spread over the brow: a band across the head's front over the eyes."""
    rel = P - ib.H
    return (inside(rel[:, 2] - 0.085, 0.003) * inside(0.02 - rel[:, 2], 0.003)
            * (rel[:, 1] < -0.04) * inside(np.abs(rel[:, 0]) - 0.11, 0.004))


def incineroarmega(src, out_path):
    k = Kit(src, coat="fur")
    add_body(k)
    add_belt(k)
    ib.add_legs(k)
    ib.add_arms(k)
    add_bracers(k)
    ib.add_mane(k)
    ib.add_head(k)
    k.paint(brow, BLACK, on=["head"])
    ib.add_tail(k)
    ib.add_mouth(k)
    k.build(voxel=0.0075, smooth=4, band=0.003, wband=0.06)
    look = ib.eye_colour()
    H, HR = ib.H, ib.HR
    k.eye_states(lambda s: lt.lay_eyes(k, s, H, HR, x=0.054, dz=0.006, half=(0.034, 0.026), lid=RED, line=INK,
                                       outline=ib.OUTLINE, look=look, eps=0.0018))
    k.export(out_path)


if __name__ == "__main__":
    incineroarmega(*args())
