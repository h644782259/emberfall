"""Legacy entrypoint retained; execute the named production behavior partition."""
import os,runpy
from pathlib import Path
os.environ['SCENERY_TEST_SCOPE']='hub'
runpy.run_path(str(Path(__file__).with_name('SceneryPresentationProductionTests.py')),run_name='__main__')
