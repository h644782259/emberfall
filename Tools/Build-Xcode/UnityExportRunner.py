#!/usr/bin/env python3
"""Bound license initialization and recover once without interrupting other editors."""
import getpass
import os
from pathlib import Path
import shutil
import signal
import subprocess
import sys
import time


def stop(process):
    if process.poll() is None:
        process.terminate()
        try:
            process.wait(timeout=10)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait()


def reset_license_clients(command):
    rows = subprocess.check_output(['ps', '-axo', 'pid=,args='], text=True).splitlines()
    processes = [row.strip().split(None, 1) for row in rows if row.strip()]
    # Match executable paths, never shell commands containing Unity paths.
    if any(len(row) == 2 and row[1].startswith('/Applications/Unity/')
           and '/Unity.app/Contents/MacOS/Unity ' in row[1] for row in processes):
        print('检测到其他 Unity 编辑器，跳过自动重启授权服务。请关闭编辑器后重新运行。', flush=True)
        return False
    for row in processes:
        if len(row) == 2 and row[1].startswith('/Applications/Unity/') and '/MacOS/Unity.Licensing.Client ' in row[1]:
            try:
                os.kill(int(row[0]), signal.SIGTERM)
            except ProcessLookupError:
                pass
    # Wait for the old IPC owner to exit before starting the replacement.
    for _ in range(10):
        live = []
        for row in processes:
            if len(row) == 2 and row[1].startswith('/Applications/Unity/') and '/MacOS/Unity.Licensing.Client ' in row[1]:
                try:
                    os.kill(int(row[0]), 0)
                    live.append(int(row[0]))
                except ProcessLookupError:
                    pass
        if not live:
            break
        time.sleep(1)
    else:
        print('旧授权客户端未退出，请关闭 Unity Hub 后重试。', flush=True)
        return False
    client = Path(command[0]).parent.parent / 'Helpers/UnityLicensingClient.app/Contents/MacOS/Unity.Licensing.Client'
    service_log = Path(command[command.index('-logFile') + 1]).with_name('ios-license-service.log')
    with service_log.open('w') as stream:
        service = subprocess.Popen([str(client), '--namedPipe', 'Unity-LicenseClient-' + getpass.getuser(),
                                    '--disable-auto-shutdown'], stdout=stream, stderr=stream)
    # Pre-start the supported named-pipe service instead of relying on Editor's launcher.
    time.sleep(5)
    if service.poll() is not None:
        print('授权客户端启动失败，详细日志：' + str(service_log), flush=True)
        return False
    return True


def run(log, command, startup_timeout=120):
    # Restart an idle licensing service before the editor launch.
    # Existing editors prevent any shared-service mutation.
    if '-logFile' in command:
        print('检查并预启动 Unity 授权服务…', flush=True)
        if not reset_license_clients(command):
            return 1
    for attempt in range(2):
        log.write_text('')
        process = subprocess.Popen(command)
        started = time.monotonic()
        initialized = False
        license_stuck = False
        try:
            while process.poll() is None:
                content = log.read_text(errors='replace') if log.exists() else ''
                initialized |= 'Application.AssetDatabase Initial Refresh End' in content or 'BuildPlayer:' in content
                if not initialized and time.monotonic() - started >= startup_timeout:
                    license_stuck = 'Licensing is not yet initialized' in content or 'waiting for Licensing to initialize' in content
                    print('Unity 启动超过120秒，停止本次进程。', flush=True)
                    stop(process)
                    break
                time.sleep(1)
        except BaseException:
            stop(process)
            raise
        result = process.wait()
        if result == 0:
            return 0
        content = log.read_text(errors='replace') if log.exists() else ''
        license_stuck |= not initialized and any(message in content for message in (
            'Licensing initialization failed', "Channel LicenseClient-", 'waiting for Licensing to initialize'))
        if not license_stuck or attempt == 1:
            return result if result > 0 else 1
        if log.exists():
            shutil.copyfile(log, log.with_suffix('.attempt-1.log'))
        print('授权初始化超时，尝试恢复授权客户端并重试一次。', flush=True)
        if not reset_license_clients(command):
            return 1
    return 1


if __name__ == '__main__':
    def interrupted(signum, frame):
        raise KeyboardInterrupt
    signal.signal(signal.SIGTERM, interrupted)
    sys.exit(run(Path(sys.argv[1]), sys.argv[2:]))
