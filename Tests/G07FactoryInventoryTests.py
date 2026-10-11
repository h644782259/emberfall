"""Full six affected production factories, authored resources enabled; no rendering claims."""
from pathlib import Path
import os,tempfile
os.environ.setdefault("G07_INVENTORY",str(Path(tempfile.gettempdir())/"Emberfall-G07-factory-inventory.json"))
source=Path(__file__).with_name('FixedSceneryEnabledIntegrationTests.py').read_text()
source=source[:source.index(' for file,old,new,expected in [')]
source=source.replace("actual+='namespace Emberfall{'+extract(world,'public sealed class WorldMotion')+'}'", "actual+='namespace Emberfall{'+extract(world,'public sealed class WorldMotion')+'}'\nactual+='namespace Emberfall{public static partial class WorldBuilder{'+extract((root/'Assets/Scripts/World/WorldBuilder.TacticalRooms.cs').read_text(),'public static GameObject MakeRoomObjective(')+'}}'\nactual+=(root/'Assets/Scripts/Core/RoomTacticalRegion.cs').read_text()")
source=source.replace("(harness/'FixedSceneryEnabledIntegrationTests.cs').read_text()","(harness/'G07FactoryInventoryTests.cs').read_text()")
exec(compile(source,str(Path(__file__).with_name('FixedSceneryEnabledIntegrationTests.py')),'exec'))
