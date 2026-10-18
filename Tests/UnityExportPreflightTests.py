"""A blocked export must preserve its previous log without presenting it as current."""
import importlib.util
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

root = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('runner', root / 'Tools/Build-Xcode/UnityExportRunner.py')
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)

class PreflightTests(unittest.TestCase):
    def test_editor_blocks_without_launch_or_old_success(self):
        for suffix in ('', ' -projectPath /tmp/example'):
            with self.subTest(suffix=suffix), tempfile.TemporaryDirectory() as tmp:
                log = Path(tmp) / 'latest.log'
                log.write_text('OLD EXPORT SUCCESS')
                executable = '/Applications/Unity/Hub/Editor/test/Unity.app/Contents/MacOS/Unity'
                with patch.object(runner.subprocess, 'check_output', return_value='123 ' + executable + suffix), patch.object(runner.subprocess, 'Popen') as launch, patch.object(runner.os, 'kill') as kill:
                    self.assertEqual(runner.run(log, [executable, '-logFile', str(log)]), 1)
                    launch.assert_not_called()
                    kill.assert_not_called()
                self.assertNotIn('OLD EXPORT SUCCESS', log.read_text())
                self.assertIn('尚未开始', log.read_text())
                self.assertEqual(log.with_suffix('.previous.log').read_text(), 'OLD EXPORT SUCCESS')

    def test_process_inspection_error_is_current_failure(self):
        with tempfile.TemporaryDirectory() as tmp:
            log = Path(tmp) / 'latest.log'
            with patch.object(runner.subprocess, 'check_output', side_effect=PermissionError('process inspection denied')), patch.object(runner.subprocess, 'Popen') as launch:
                self.assertEqual(runner.run(log, ['Unity', '-logFile', str(log)]), 1)
                launch.assert_not_called()
            self.assertIn('process inspection denied', log.read_text())

if __name__ == '__main__':
    unittest.main()
