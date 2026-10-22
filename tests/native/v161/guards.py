"""Explicit disposable-profile guards; this module never launches or contacts a game."""
import os
from pathlib import Path

MARKER = '.frontlinemap-disposable'

def profile(path):
    root = Path(path).expanduser().resolve(strict=True)
    if not root.is_dir() or not (root / MARKER).is_file():
        raise ValueError(f'{root}: existing {MARKER} marker required; verify a disposable copy yourself before creating it')
    if not (root / 'NuclearOption.exe').is_file():
        raise ValueError(f'{root}: NuclearOption.exe is missing')
    return root

def required_file(name):
    value = os.environ.get(name)
    if not value or not Path(value).expanduser().is_file():
        raise ValueError(f'{name} must name an existing operator-provided file')
    return Path(value).expanduser().resolve()

def validate(client=False, performance=False):
    if not os.environ.get('FLM_GAME_ROOT'):
        raise ValueError('Explicit FLM_GAME_ROOT or --game-root is required')
    game = profile(os.environ['FLM_GAME_ROOT'])
    os.environ['FLM_GAME_ROOT'] = str(game)
    if client:
        if os.environ.get('FLM_PORT', '42680') != '42680':
            raise ValueError('Two-process stages use bridge ports 42680 and 42681; configure those ports in the disposable bridges')
        if not os.environ.get('FLM_CLIENT_ROOT'):
            raise ValueError('UDP/dense-relative requires explicit FLM_CLIENT_ROOT or --client-root')
        other = profile(os.environ['FLM_CLIENT_ROOT'])
        if other == game:
            raise ValueError('Host and client must use distinct disposable copies')
        os.environ['FLM_CLIENT_ROOT'] = str(other)
    required_file('FLM_RPC_SCRIPT')
    if not os.environ.get('FLM_BRIDGE_HOST'):
        raise ValueError('FLM_BRIDGE_HOST must explicitly match the configured authenticated bridge interface')
    if not os.environ.get('FLM_EVIDENCE_ROOT'):
        raise ValueError('Explicit FLM_EVIDENCE_ROOT or --output is required')
    evidence = Path(os.environ['FLM_EVIDENCE_ROOT']).expanduser().resolve()
    if evidence == game or game in evidence.parents:
        raise ValueError('Keep evidence outside the disposable game profile')
    if client and (evidence == other or other in evidence.parents):
        raise ValueError('Keep evidence outside the disposable client profile')
    os.environ['FLM_EVIDENCE_ROOT'] = str(evidence)
    if performance:
        required_file('FLM_PRESENTMON')
    return game
