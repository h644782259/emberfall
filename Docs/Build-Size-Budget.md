# Build size evidence and bounded reports

The save system limits new slots, write size and backup count. Visual meshes/materials are shared and transient effects have active caps. These runtime/storage rules do not prove a particular installer or IPA size.

`BuildSizeAudit` now records Unity's `BuildReport.summary.totalSize` after a real player build/export. The editor callback writes fixed names under the ignored `Builds/SizeReports` folder: latest and previous per target. It does not generate a timestamp archive. If the old latest JSON is unreadable, it is retained and a fixed recovered-latest report is used. No build, player save, imported asset or cache is deleted.

A comparison is valid only for matching target, build options, Unity version and metric type. More than20% growth **and** more than25MiB growth emits a warning; these thresholds are operational warning choices, not measured game performance. A first build has no comparison. An iOS Unity export is labelled as reported Xcode export bytes, never as signed IPA, store download or installed size.

For a deliberate hard CI limit, supply `-emberfallBuildBudgetMiB 256` (example, choose the budget for the intended output). Positive invariant-culture fractional MiB values are supported. If the actual reported bytes exceed the supplied cap, the callback fails the build and retains the output and size evidence. No arbitrary hard package ceiling is silently imposed on existing developer builds.

Verification in this cloud: pure boundary/invalid-input policy checks and exact Unity6000.6.3f1 editor API compilation. No real build callback was executed because Unity cannot start under the environment's IPC/socket restrictions. Consequently no Windows build size or iOS IPA size has been measured here.
