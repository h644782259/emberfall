"""Compare before/after full production factory recordings, not draw-call/GPU measurements."""
import json,sys
from pathlib import Path
old=json.loads(Path(sys.argv[1]).read_text());new=json.loads(Path(sys.argv[2]).read_text());rows=[]
assert len(old)==len(new)==7
for before,after in zip(old,new):
 assert before['factory']==after['factory']
 for key in ['navigationBoxes','navigationCircles','occlusion']:
  assert before[key]==after[key],(before['factory'],key)
 row={'factory':before['factory'],'navigationIdentical':True,'occlusionMembershipIdentical':True}
 for key in ['objects','renderers','uniqueMeshes','uniqueMaterials','uniqueSourceTriangles','instancedMeshTriangles','lights']:
  row[key]={'before':before[key],'after':after[key],'delta':after[key]-before[key]}
  assert after[key]<=before[key],(before['factory'],key,'budget grew')
 rows.append(row)
assert rows[1]['renderers']['delta']==-17
assert rows[2]['renderers']['delta']==rows[3]['renderers']['delta']==-24
assert rows[5]['renderers']['delta']==-7
print(json.dumps(rows,indent=2))
