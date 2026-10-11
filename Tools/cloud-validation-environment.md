# Cloud validation environment — 2026-10-01

## Installed successfully

- .NET SDK 8.0.425, from Microsoft's official `dotnet-install.sh`, in
  `/workspace/shared/emberfall-tools/dotnet`
- Unity Editor 6000.6.3f1, changeset `45d8eee7de74`, from the Linux archive on
  [Unity's official release page](https://unity.com/releases/editor/whats-new/6000.6.3f1), in
  `/workspace/shared/emberfall-tools/unity-6000.6.3f1`
- The editor executable reports the expected version and has no unresolved
  shared-library dependencies in `ldd`
- The repository's pinned UnityEngine.Modules 2021.3.33 reference package was
  downloaded from NuGet, and its SHA-512 was verified

These installation directories belong to this cloud workspace, not to either
source repository or a distributable game build.

## Editor execution blocked

Three bounded startup checks were attempted, including one using the reviewed
execution permission route and one using a complete standard HOME/XDG directory
configuration inside the writable workspace. All ended before usable project
import with exit code 1:

- Initial cache/preferences errors were resolved by setting writable
  `HOME`, `XDG_CONFIG_HOME`, `XDG_CACHE_HOME`, `XDG_DATA_HOME`,
  `XDG_RUNTIME_DIR`, and `TMPDIR`; Unity then successfully wrote its preferences
- LicensingClient local IPC returned `0x80000000`
- Unity Package Manager could not connect to its local IPC stream after 30 seconds

The remaining observed blocker is this executor's local IPC behavior. Account
activation was not reached; no license, credentials, or new agreement were supplied.
No further launch retries were made after the writable HOME/XDG check still
reported the same local IPC failures. Startup logs are retained in the ignored
`Tests/TestResults/Cloud-UnityStartup-20261001` directory.

Therefore this environment has not run the Editor's progression/JsonUtility
validation, Play Mode, frame captures, shaders, GUI, or a Windows/iOS build.
An installed executable and successful source compilation do not establish any
of those results.

## Available validation

```sh
bash Tests/Run-CloudValidation.sh \
  --dotnet /workspace/shared/emberfall-tools/dotnet/dotnet \
  --compile \
  --unity-editor /workspace/shared/emberfall-tools/unity-6000.6.3f1/Editor/Unity
```

This runs standalone logic tests and compile-only checks against both the
repository's legacy Unity references and the installed Unity 6.6 APIs.
Read the generated `Tests/TestResults/Cloud-*/report.json` and logs for results
from the specific source revision being checked. Earlier README release results
do not validate newly edited code.

## Win/iOS synchronization boundary

At the start of this work, iOS revision `fa8213624f65c4cd673fa21dbfb16745530b865b`
had byte-identical runtime scripts, standalone tests, and shared Editor tools to
Windows revision `1d50584`. Windows then added its `cce25f2` visual/gameplay changes.

Shared runtime/test fixes can move forward together, while preserving the iOS
repository's `IOSBuild.cs` and metadata, `Export-iOS.ps1`, `Export-iOS.sh`,
`Docs/iOS.md`, README and iOS-specific project settings. Do not replace the entire
iOS repository with the Windows checkout.
