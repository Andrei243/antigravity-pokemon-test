# Unown's question mark, built by unown.py's letter() with the exclamation mark's sleepy eye (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unownquestion.py -- <the export's Unown-Question.glb> <out.glb>
#
# The design, from the 3D picture (201_Unown-Question_3d.png): the eye's ring is the mark's dot, at the bottom, and out
# of its top a short neck rises and swings out to +X into a hook that curls up and back over to -X, its rounded end
# over the ring's middle. No dot apart from the body. The eye is half shut as the exclamation mark's (its lid, the
# black line along its edge and the small pupil just under it), laid by unownexclamation.py's sleepy().
#
# Measured on the picture: the ring's outside 222 px across with its middle at (243, 354); its bottom (465 px) is set
# at the export's lowest point (0.078), which gives the scale. The white is 0.72 of the ring's outer radius. The hook
# is one smooth sweep through its centre line (a description's lines would show their joints on so round a curve).
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args
u = sibling("unown")
ex = sibling("unownexclamation")
v = sibling("unownv")                   # its spline()

EX, EZ = 0.0, 0.205                     # the body bone: the eye's middle
CX, CY = 243, 354                       # the eye's middle on the picture
K = (EZ - 0.078) / (465 - CY)           # model units a pixel


def P(px, py):
    return (round(EX + (px - CX) * K, 4), round(EZ + (CY - py) * K, 4))


R = 111 * K
WHITE = 0.72 * R

UNOWN_QUESTION = {
    "eye": {"at": (EX, EZ), "white": round(WHITE, 4), "ring": round(R - WHITE - u.GAP, 4)},
    "strokes": [],
}
HOOK = [P(243, 236), P(250, 210), P(285, 186), P(343, 146), P(364, 104), P(355, 66), P(320, 45), P(280, 46),
        P(255, 64)]                     # the hook's centre line, from the ring's top to its end


def hook(k):
    k.add("hook", u.sweep(v.spline(HOOK, 8), (0.052, 0.045)), u.BODY, u.BONE)


if __name__ == "__main__":
    src, out = args()
    ex.mark(src, out, UNOWN_QUESTION, hook)
