"""Gate integration fixtures; no game, deployment, or raw observation reads."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from datetime import datetime, timedelta, timezone

SCRIPT = Path(__file__).with_name('readiness.py')
NATIVE = ['policy', 'renderer_faults', 'map_geometry', 'lifecycle', 'layered_contract', 'multiplayer_coexistence']
PERFORMANCE = ['paired_capped', 'live_capped', 'uncapped_cpu_bound', 'moving_dense_multiplayer']

def sha(data):
    return hashlib.sha256(data).hexdigest()

class ReadinessTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        (self.root / 'src').mkdir()
        for name in ['FrontlineMap.csproj', 'Plugin.cs', 'src/Example.cs']:
            (self.root / name).write_text('fixture ' + name)
        names = ['FrontlineMap.csproj', 'Plugin.cs', 'src/Example.cs']
        self.source = sha('\n'.join(sha((self.root / name).read_bytes()) + '  ' + name for name in names).encode())
        self.binary = self.root / 'bin/Release/net472/FrontlineMap.dll'
        self.binary.parent.mkdir(parents=True)
        self.binary.write_bytes(b'test dll')
        self.evidence = self.root / 'evidence'
        self.evidence.mkdir()
        self.manifest = dict(schema=1, captured_at_utc=datetime.now(timezone.utc).isoformat(),
                             source_tree_sha256=self.source, binary_sha256=sha(b'test dll'), gates={})
        for gate, checks in [('native', NATIVE), ('performance', PERFORMANCE)]:
            report = dict(binarySha256=sha(b'test dll'), success=True, checks=dict.fromkeys(checks, True))
            p = self.evidence / (gate + '.json')
            p.write_text(json.dumps(report))
            self.manifest['gates'][gate] = [dict(path=p.name, sha256=sha(p.read_bytes()))]
        build = self.root / 'artifacts/build'
        build.mkdir(parents=True)
        (build / 'references.json').write_text('{"hashes":{"Managed/Assembly-CSharp.dll":"fixture-target"}}')
        references_hash = sha((build / 'references.json').read_bytes())
        self.manifest['reference_manifest_sha256'] = references_hash
        (build / 'binary.json').write_text(json.dumps(dict(source_tree_sha256=self.source, sha256=sha(b'test dll'), reference_manifest_sha256=references_hash)))

    def run_gate(self, gate='native', expected=0):
        (self.evidence / 'evidence.json').write_text(json.dumps(self.manifest))
        p = subprocess.run([sys.executable, str(SCRIPT), '--root', str(self.root), '--gate', gate,
                            '--evidence-dir', str(self.evidence)], text=True, capture_output=True)
        self.assertEqual(p.returncode, expected, p.stdout + p.stderr)
        self.assertTrue(p.stdout.strip(), "Gate did not produce its required JSON result: " + p.stderr)
        return json.loads(p.stdout)

    def test_complete_hash_bound_native_evidence_passes(self):
        self.assertEqual(self.run_gate()['status'], 'pass')

    def test_source_change_blocks_old_evidence(self):
        (self.root / 'Plugin.cs').write_text('patched')
        self.assertEqual(self.run_gate(expected=2)['status'], 'blocked')

    def test_binary_change_blocks_old_evidence(self):
        self.binary.write_bytes(b'patched dll')
        self.run_gate(expected=2)

    def test_target_references_changed_blocks(self):
        (self.root / 'artifacts/build/references.json').write_text('{}')
        self.run_gate(expected=2)

    def test_target_attestation_missing_blocks(self):
        del self.manifest['reference_manifest_sha256']
        self.run_gate(expected=2)

    def test_tampered_report_blocks(self):
        (self.evidence / 'native.json').write_text('{}')
        self.run_gate(expected=2)

    def test_missing_check_blocks_despite_success(self):
        self.change_report('native', lambda r: r['checks'].pop('policy'))
        self.run_gate(expected=2)

    def test_truthy_non_boolean_check_blocks(self):
        self.change_report('native', lambda r: r['checks'].update(policy='true'))
        self.run_gate(expected=2)

    def test_wrong_loaded_binary_blocks(self):
        self.change_report('native', lambda r: r.update(binarySha256='0' * 64))
        self.run_gate(expected=2)

    def test_old_or_future_timestamps_block(self):
        for offset in [-49, 2]:
            with self.subTest(offset=offset):
                self.manifest['captured_at_utc'] = (datetime.now(timezone.utc) + timedelta(hours=offset)).isoformat()
                self.run_gate(expected=2)

    def test_missing_dense_and_uncapped_checks_cannot_certify_performance(self):
        self.change_report('performance', lambda r: [r['checks'].pop(k) for k in ['uncapped_cpu_bound', 'moving_dense_multiplayer']])
        result = self.run_gate('performance', expected=2)
        self.assertIn('uncapped_cpu_bound', result['missing_checks'])
        self.assertIn('moving_dense_multiplayer', result['missing_checks'])

    def test_complete_performance_evidence_passes(self):
        self.assertEqual(self.run_gate('performance')['status'], 'pass')

    def test_path_escape_blocks(self):
        self.manifest['gates']['native'][0]['path'] = '../outside.json'
        self.run_gate(expected=2)

    def test_malformed_manifest_blocks_without_traceback(self):
        self.manifest['gates'] = []
        self.run_gate(expected=2)

    def test_duplicate_json_keys_block(self):
        p = self.evidence / 'native.json'
        p.write_text('{"success":false,"success":true}')
        self.manifest['gates']['native'][0]['sha256'] = sha(p.read_bytes())
        self.run_gate(expected=2)

    def change_report(self, gate, modify):
        p = self.evidence / (gate + '.json')
        report = json.loads(p.read_text())
        modify(report)
        p.write_text(json.dumps(report))
        self.manifest['gates'][gate][0]['sha256'] = sha(p.read_bytes())

if __name__ == '__main__':
    unittest.main()
