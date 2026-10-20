import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

try:
    import pycdlib
except ImportError:
    pycdlib = None

ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts" / "make_iso.py"


@unittest.skipIf(pycdlib is None, "pycdlib packaging dependency is not installed")
class IsoPackagingTests(unittest.TestCase):
    def test_installer_and_readme_are_added_to_joliet_iso(self):
        with tempfile.TemporaryDirectory() as temp:
            folder = Path(temp) / "media"
            folder.mkdir()
            (folder / "AeroDesk-Setup.exe").write_bytes(b"test installer payload")
            (folder / "README.txt").write_text("AeroDesk setup media\n", encoding="utf-8")
            output = Path(temp) / "AeroDesk-test.iso"
            result = subprocess.run(
                [sys.executable, str(SCRIPT), "--input", str(folder), "--output", str(output)],
                capture_output=True, text=True, check=False,
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertGreater(output.stat().st_size, 0)
            image = pycdlib.PyCdlib()
            image.open(str(output))
            try:
                image.get_record(iso_path="/AERODSK.EXE;1")
                image.get_record(iso_path="/README.TXT;1")
            finally:
                image.close()

    def test_missing_installer_fails_clearly(self):
        with tempfile.TemporaryDirectory() as temp:
            folder = Path(temp)
            (folder / "README.txt").write_text("test", encoding="utf-8")
            result = subprocess.run(
                [sys.executable, str(SCRIPT), "--input", str(folder), "--output", str(folder / "bad.iso")],
                capture_output=True, text=True, check=False,
            )
            self.assertNotEqual(result.returncode, 0)
            self.assertIn("Installer not found", result.stderr)


if __name__ == "__main__":
    unittest.main()
