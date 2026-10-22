#!/usr/bin/env python3
"""Opt-in orchestration only. No downloads, deployment, marker creation, or game bootstrap."""
import argparse,os,runpy,sys
from pathlib import Path
from guards import validate

STAGES={'native':'review_native.py','export':'native_export.py','motion':'moving_contours.py','udp':'network_smoke.py','performance':'performance.py','uncapped':'uncapped.py','dense-relative':'dense_relative.py'}
def main():
 p=argparse.ArgumentParser(description='Opt-in native 1.6.1 acceptance stages on explicitly marked disposable copies. Requires an already loaded authenticated bridge and operator-supplied RPC client. Canonical adaptation has not itself been native-executed.')
 p.add_argument('--stage',choices=STAGES,required=True)
 p.add_argument('--game-root',help='Disposable host copy with existing .frontlinemap-disposable marker; alternatively FLM_GAME_ROOT')
 p.add_argument('--client-root',help='Distinct marked disposable copy required for udp/dense-relative; alternatively FLM_CLIENT_ROOT')
 p.add_argument('--output',help='Required evidence directory; alternatively FLM_EVIDENCE_ROOT')
 p.add_argument('--rpc-script',help='Existing authenticated SDK RPC PowerShell client; alternatively FLM_RPC_SCRIPT')
 p.add_argument('--bridge-host',help='Configured Windows bridge interface; alternatively FLM_BRIDGE_HOST')
 p.add_argument('--presentmon',help='Existing authorized PresentMon executable for performance stages; alternatively FLM_PRESENTMON')
 p.add_argument('--dotnet',help='Installed .NET SDK executable for export replay; alternatively FLM_DOTNET')
 a=p.parse_args()
 for field,env in [('game_root','FLM_GAME_ROOT'),('client_root','FLM_CLIENT_ROOT'),('output','FLM_EVIDENCE_ROOT'),('rpc_script','FLM_RPC_SCRIPT'),('bridge_host','FLM_BRIDGE_HOST'),('presentmon','FLM_PRESENTMON'),('dotnet','FLM_DOTNET')]:
  value=getattr(a,field)
  if value:os.environ[env]=value
 try:validate(client=a.stage in ('udp','dense-relative'),performance=a.stage in ('udp','performance','uncapped','dense-relative'))
 except (ValueError,OSError) as e:p.error(str(e))
 from native_support import PROFILE,PORT,verify_running
 verify_running(PROFILE,PORT) # Read-only identity check before any mutating stage actions.
 sys.argv=[str(Path(__file__).with_name(STAGES[a.stage]))]
 runpy.run_path(sys.argv[0],run_name='__main__')
if __name__=='__main__':main()
