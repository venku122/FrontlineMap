"""Validate explicitly supplied, hash-bound runtime acceptance evidence.

This checks a trusted runner's attestation; it does not launch Unity or infer
scenario coverage from screenshots, counters, or a single success flag.
"""
import argparse
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import sys

REQUIRED = {
    'native': ('policy', 'renderer_faults', 'map_geometry', 'lifecycle', 'layered_contract', 'multiplayer_coexistence'),
    'performance': ('paired_capped', 'live_capped', 'uncapped_cpu_bound', 'moving_dense_multiplayer'),
}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def source_fingerprint(root):
    # Must match scripts/source-state.ps1, including ordinal path order and no final newline.
    names = ['FrontlineMap.csproj', 'Plugin.cs']
    names += [p.relative_to(root).as_posix() for p in (root / 'src').rglob('*.cs')]
    names += [p.name for p in root.iterdir() if p.is_file() and p.suffix in ('.props', '.targets')]
    data = '\n'.join(digest(root / name) + '  ' + name for name in sorted(names))
    return hashlib.sha256(data.encode('utf-8')).hexdigest()


def no_duplicates(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError('duplicate JSON key')
        result[key] = value
    return result


def read_json(path):
    data = json.loads(path.read_text(encoding='utf-8-sig'), object_pairs_hook=no_duplicates)
    if not isinstance(data, dict):
        raise ValueError('expected JSON object')
    return data


def require(condition, message):
    if not condition:
        raise ValueError(message)


def evaluate(root, binary, evidence_dir, gate, max_age_hours):
    required = REQUIRED[gate]
    result = dict(gate=gate, status='blocked', passed_checks=[], missing_checks=list(required))
    try:
        require(evidence_dir is not None, 'explicit --evidence-dir required; no historical evidence is selected automatically')
        manifest = read_json(evidence_dir / 'evidence.json')
        require(type(manifest.get('schema')) is int and manifest['schema'] == 1, 'unsupported evidence schema')
        source = source_fingerprint(root)
        dll_hash = digest(binary)
        build = read_json(root / 'artifacts/build/binary.json')
        require(build.get('source_tree_sha256') == source, 'build attestation is stale for current source; rebuild')
        require(str(build.get('sha256', '')).lower() == dll_hash, 'DLL differs from build attestation')
        references_hash = digest(root / 'artifacts/build/references.json')
        require(str(build.get('reference_manifest_sha256', '')).lower() == references_hash, 'target reference manifest differs from build attestation')
        require(manifest.get('reference_manifest_sha256') == references_hash, 'runtime evidence belongs to different target references')
        require(manifest.get('source_tree_sha256') == source, 'runtime evidence belongs to different source')
        require(manifest.get('binary_sha256') == dll_hash, 'runtime evidence belongs to different DLL')
        captured = datetime.fromisoformat(manifest['captured_at_utc'].replace('Z', '+00:00'))
        require(captured.tzinfo is not None, 'captured_at_utc must include timezone')
        age = (datetime.now(timezone.utc) - captured).total_seconds()
        require(0 <= age <= max_age_hours * 3600, 'runtime evidence expired or timestamp is in the future')
        gates = manifest.get('gates')
        require(isinstance(gates, dict), 'gates must be an object')
        reports = gates.get(gate)
        require(isinstance(reports, list) and reports, 'no reports for requested gate')
        passed = set()
        for entry in reports:
            require(isinstance(entry, dict), 'report entry must be an object')
            name = entry.get('path')
            require(isinstance(name, str) and name, 'report path required')
            path = (evidence_dir / name).resolve()
            require(path.is_relative_to(evidence_dir.resolve()) and path != evidence_dir.resolve(), 'report path escapes evidence directory')
            require(digest(path) == entry.get('sha256'), 'report SHA256 mismatch')
            report = read_json(path)
            require(report.get('binarySha256') == dll_hash, 'report loaded DLL differs from current build')
            require(report.get('success') is True, 'report did not succeed')
            checks = report.get('checks')
            require(isinstance(checks, dict) and checks, 'named scenario checks required; success alone does not establish coverage')
            for name in required:
                if name in checks:
                    require(checks[name] is True, 'required check failed or is not boolean true: ' + name)
                    passed.add(name)
        result.update(passed_checks=sorted(passed), missing_checks=sorted(set(required) - passed),
                      source_tree_sha256=source, binary_sha256=dll_hash)
        require(not result['missing_checks'], 'required scenario coverage is incomplete')
        result.update(status='pass', reason='Fresh hash-bound evidence covers the required scenarios on the attested target only')
    except (OSError, ValueError, KeyError, TypeError, AttributeError) as error:
        result['reason'] = str(error)
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parent.parent)
    parser.add_argument('--binary', type=Path)
    parser.add_argument('--evidence-dir', type=Path)
    parser.add_argument('--gate', choices=(*REQUIRED, 'target'), required=True)
    parser.add_argument('--max-age-hours', type=float, default=48)
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    if not 0 < args.max_age_hours <= 48:
        parser.error('--max-age-hours must be greater than zero and at most 48')
    binary = args.binary or args.root / 'bin/Release/net472/FrontlineMap.dll'
    gates = list(REQUIRED) if args.gate == 'target' else [args.gate]
    outcomes = [evaluate(args.root, binary, args.evidence_dir, gate, args.max_age_hours) for gate in gates]
    result = outcomes[0] if len(outcomes) == 1 else dict(gate='target_runtime', status='pass' if all(r['status'] == 'pass' for r in outcomes) else 'blocked', gates=outcomes,
        scope='Runtime evidence only; invoke validate.ps1 -Mode Target to execute Core as well')
    output = json.dumps(result, indent=2) + '\n'
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(output)
    print(output, end='')
    return 0 if result['status'] == 'pass' else 2

if __name__ == '__main__':
    sys.exit(main())
