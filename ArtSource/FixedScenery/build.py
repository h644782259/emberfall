"""Original F4 fixed scenery. Blender 4.3.2. No external assets or textures.
Meshes retain Unity primitive unit envelopes; .bytes are metre-local Y-up data.
"""
import bpy,math,struct,json,hashlib
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];SRC=Path(__file__).resolve().parent;OUT=ROOT/'Assets/Resources/FixedScenery'
# Stable source GUID schema for restoring absent resource metas. Existing metas remain untouched.
GUIDS={'ColumnBand': 'e0d2654e439e474981bb71ee9ecc0cd4',
 'ObservatoryDais': '291eac842c6b7c5c655b8200b3fbe16d',
 'Collar': '0d2fad07d1417ede41a22529015a4f91',
 'RoofTiles': 'e6c6a03bf3a73b177127575975027dee',
 'NpcHead': '5860f8bd536a7115cbc7da2ce4c29c0b',
 'Apron': 'bdcba92e12af4e0873559964252cd253',
 'ColumnBase': 'de9513bb6265349a0516e07cba1ba908',
 'MerchantCap': '5ee849179dcbd027ca5f226024817513',
 'Capital': '3e1235196f5fd5ee7557677b21e1863e',
 'GateFrame': '0329a4976501a163eb02554f9f056b96',
 'ChimneyCap': 'b265b0aa76e2bda3488c460c09f9fc22',
 'NpcTunic': 'd01e5941d652b5af0f229d169987515f',
 'Eave': 'fae10ead19b1bcaa8629117e07be9175',
 'NpcSleeve': 'c17d98b3115da66d5960e0827ee3bc87',
 'ApprenticeCrest': '31de4540268e86e40add4ecec28d34a3',
 'FlutedColumn': '01249121e05bf58ba4277c1114840fc6',
 'FacilityBase': '470823ca92f55154681bd3e6732e544f',
 'Anvil': 'aebffca74eb6a953654d38b25bad72ad',
 'CodexCrest': '8e7093c1b36aefc7d3f9423921e06b52',
 'Ridge': 'abf0756133544b991169b1737809c408',
 'Stump': '97823c947d2e71cb605c4ed5dac5b29c',
 'NpcBoot': '9dcb37f92beeb0e9fca96a2a4dbbf1b0',
 'GatePlinth': '155eb8e5794b5771f3aa1bb1bbfd6f45',
 'Shelf': '0f99e23d87043029750eadffdb0e3c18',
 'CoreCrest': '68276eb3789c97ea53befbee886c382d',
 'Lectern': '704b3b232a4c559ddf2652a75a2f486d',
 'TrialCrest': '8336e597c77e669f5c66466ed88b5bd7'}
bpy.ops.wm.read_factory_settings(use_empty=True);OUT.mkdir(parents=True,exist_ok=True);records={};assets=[]
def export(name,verts,faces):
 m=bpy.data.meshes.new(name);m.from_pydata([(x,-z,y) for x,y,z in verts],[],faces);m.update();m.calc_loop_triangles()
 o=bpy.data.objects.new(name,m);bpy.context.collection.objects.link(o);assets.append(o)
 # Flat split normals are explicit; each triangle owns vertices for dependable stone/cloth facets.
 rows=[];indices=[]
 for tri in m.loop_triangles:
  normal=tri.normal
  if normal.length<.01:continue
  for idx in tri.vertices:
   x,y,z=m.vertices[idx].co;nx,ny,nz=normal;indices.append(len(rows));rows.append((x,z,-y,nx,nz,-ny,(x+.5),max(0,min(1,z))))
 data=bytearray(struct.pack('<Iii',0x45465334,len(rows),len(indices)))
 for v in rows:data.extend(struct.pack('<8f',*v))
 data.extend(struct.pack('<%di'%len(indices),*indices));(OUT/(name+'.bytes')).write_bytes(data)
 meta=OUT/(name+'.bytes.meta')
 if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+GUIDS[name]+'\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
 assert len(indices)//3<=2000
 records[name]={'triangles':len(indices)//3,'vertices':len(rows),'materials':1,'textures':0,'source_bytes':len(data),'bounds_unity':[[min(v[i] for v in rows) for i in range(3)],[max(v[i] for v in rows) for i in range(3)]],'sha256':hashlib.sha256(data).hexdigest()}
 return o
def loft(name,levels,n=12,square=False,flute=False):
 v=[]
 for y,r in levels:
  for i in range(n):
   a=(i+.5)*math.tau/n
   if square:
    # chamfered square follows an octagonal perimeter
    x,z=[(-1,-.76),(-.76,-1),(.76,-1),(1,-.76),(1,.76),(.76,1),(-.76,1),(-1,.76)][i]
   else:x,z=math.cos(a),math.sin(a)
   f=.87 if flute and i%2 else 1;v.append((x*r*f,y,z*r*f))
 f=[tuple(reversed(range(n))),tuple((len(levels)-1)*n+i for i in range(n))]
 for j in range(len(levels)-1):
  for i in range(n):f.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
 return export(name,v,[tuple(reversed(face)) for face in f])
def boxes(name,boxes):
 v=[];f=[]
 for center,size in boxes:
  x,y,z=center;sx,sy,sz=(a/2 for a in size);k=len(v)
  v.extend([(x+dx*sx,y+dy*sy,z+dz*sz) for dx,dy,dz in [(-1,-1,-1),(1,-1,-1),(1,-1,1),(-1,-1,1),(-1,1,-1),(1,1,-1),(1,1,1),(-1,1,1)]])
  f.extend([tuple(k+i for i in face) for face in [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]])
 return export(name,v,[tuple(reversed(face)) for face in f])
loft('ColumnBase',[(-.5,.46),(-.40,.5),(-.24,.5),(-.16,.43),(.08,.43),(.19,.34),(.5,.34)],8,True)
loft('FlutedColumn',[(-1,.5),(-.91,.5),(-.83,.46),(.75,.41),(.85,.46),(1,.46)],20,False,True)
loft('Capital',[(-.5,.30),(-.30,.32),(.10,.47),(.25,.5),(.5,.5)],8,True)
loft('Collar',[(-1,.38),(-.65,.5),(.40,.5),(1,.39)])
loft('ColumnBand',[(-1,.44),(-.65,.5),(.65,.5),(1,.44)],16)
loft('GatePlinth',[(-1,.47),(-.60,.5),(-.20,.5),(.15,.46),(.55,.46),(1,.41)],20)
# Segmented cut-stone ring, outer radius 1.05 and inner .945 match existing width/radius.
v=[];f=[]
for i in range(32):
 a=(i+.06)*math.tau/32;b=(i+.94)*math.tau/32;k=len(v)
 for z in (-.065,.065):
  for r,t in [(1.05,a),(1.05,b),(.945,b),(.945,a)]:v.append((math.cos(t)*r,math.sin(t)*r,z))
 f.extend([tuple(k+j for j in face) for face in [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]])
export('GateFrame',v,f)
# Ten raised tile courses joined into a single material mesh inside the old panel envelope.
boxes('RoofTiles',[((0,-.22,0),(1,.56,1))]+[((0,.06+(.08 if i%2 else 0),-.45+i*.1),(.98,.62,.085)) for i in range(10)])
loft('Eave',[(-.5,.5),(-.05,.5),(.12,.39),(.5,.34)],8,True)
loft('Ridge',[(-.5,.5),(-.2,.5),(.5,.20)],8,True)
boxes('ChimneyCap',[((0,-.36,0),(1,.28,1)),((-.39,.10,0),(.22,.8,1)),((.39,.10,0),(.22,.8,1)),((0,.10,-.39),(.56,.8,.22)),((0,.10,.39),(.56,.8,.22))])
loft('FacilityBase',[(-1,.44),(-.84,.5),(-.55,.5),(-.33,.39),(.5,.31),(.7,.45),(1,.45)],12)
boxes('CoreCrest',[((0,-.28,0),(.85,.35,.85))]+[((x,.15,z),(.16,.7,.16)) for x,z in [(-.32,-.32),(.32,-.32),(-.32,.32),(.32,.32)]])
loft('ApprenticeCrest',[(-.5,.45),(-.05,.45),(.1,.35),(.5,.17)],12)
# Open book: two sloping page volumes, normalized unit box.
v=[(-.5,-.5,-.4),(0,-.5,-.4),(.5,-.5,-.4),(-.5,-.5,.4),(0,-.5,.4),(.5,-.5,.4),(-.5,.5,-.4),(0,.0,-.4),(.5,.5,-.4),(-.5,.5,.4),(0,0,.4),(.5,.5,.4)]
export('CodexCrest',v,[(0,1,4,3),(1,2,5,4),(6,9,10,7),(7,10,11,8),(0,6,7,1),(1,7,8,2),(3,4,10,9),(4,5,11,10),(0,3,9,6),(2,8,11,5)])
loft('TrialCrest',[(-.5,.43),(-.2,.5),(.2,.5),(.5,.30)],8,True)
# Practical workshop furnishing with visible braced/recessed silhouettes.
boxes('Shelf',[((0,-.18,0),(1,.64,1)),((0,.32,-.40),(1,.36,.20)),((-.43,.25,0),(.14,.5,.8)),((.43,.25,0),(.14,.5,.8))])
loft('Anvil',[(-.5,.34),(-.23,.34),(-.14,.18),(.20,.23),(.32,.5),(.5,.5)],8,True)
loft('Stump',[(-1,.46),(-.8,.5),(.7,.44),(1,.43)],10,False,True)
boxes('Lectern',[((0,-.41,0),(1,.18,1)),((0,-.02,0),(.30,.68,.45)),((0,.36,0),(1,.28,1)),((0,.49,.43),(1,.02,.14))])
loft('ObservatoryDais',[(-1,.45),(-.6,.5),(-.10,.5),(.2,.46),(1,.43)],24)
# NPC set uses exact Capsule 1x2x1/Sphere 1x1x1 envelopes, no rig replacement.
loft('NpcTunic',[(-1,.42),(-.8,.5),(-.2,.37),(.48,.5),(.82,.38),(1,.21)],8,True)
loft('NpcSleeve',[(-1,.22),(-.78,.32),(.05,.43),(.70,.5),(1,.33)],8,True)
loft('NpcBoot',[(-1,.41),(-.72,.5),(-.30,.47),(-.15,.33),(1,.30)],8,True)
loft('NpcHead',[(-.5,.19),(-.34,.34),(-.05,.46),(.22,.5),(.42,.37),(.5,.10)],10)
loft('MerchantCap',[(-1,.5),(-.6,.5),(-.5,.34),(.45,.39),(1,.20)],12)
loft('Apron',[(-.5,.34),(-.3,.48),(.2,.41),(.5,.24)],8,True)
# Apron must remain shallow along z; shape envelope already normalized and original scale is .06.
assert sum(x['source_bytes'] for x in records.values())<=1048576
(SRC/'budget.json').write_text(json.dumps({'blender':bpy.app.version_string,'assets':records,'runtime_bytes':sum(x['source_bytes'] for x in records.values()),'new_textures':0,'new_materials':0,'scope':'27 normalized fixed modules; world palette/transform/navigation remains authoritative'},indent=2)+'\n')
# Editable individual meshes remain at authored origin, arranged only for source inspection.
for i,o in enumerate(assets):o.location=((i%7)*2.8,(i//7)*3,0)
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'Emberfall-Fixed-Scenery.blend'))
print('F4_BUILD_OK',len(records),'modules',sum(x['source_bytes'] for x in records.values()),'bytes',max(x['triangles'] for x in records.values()),'max tris')
