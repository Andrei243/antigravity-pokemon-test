# Hatzegon (Hațegon, Carpathia No. 10142) sculpted from nothing on the skeleton and clips of the game's own export:
#   blender --background --factory-startup --python sculpts/hatzegon.py -- <the export's Hatzegon.glb> <out.glb>
# Hatzlet's frame grown (sibling("hatzlet")), its skeleton 1.4 times the chick's. From its picture: the giant pterosaur
# on all fours, ochre and brown; an enormous heavy head on a short powerful neck, a massive deep pale beak hooked at
# its tip, a crown of tall pale stone shards swept back off the skull and more shards over the shoulders; a narrow
# pale eye under a heavy brow; a muscular chest and arms walking on clawed hands, the folded wings dark brown blades
# rising behind; short hind legs; a long pointed tail out behind.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import args, sibling

if __name__ == "__main__":
    sibling("hatzlet").pterosaur(*args(), big=True)
