# WondrousTailsSolver

A Dalamud plugin that adds row probabilities to the Wondrous Tails display.

This is a maintained fork of [MidoriKami/EzWondrousTails](https://github.com/MidoriKami/EzWondrousTails), updated for current FFXIV patches and Dalamud API 15. The original plugin was created by daemitus and maintained by MidoriKami.

![math](https://github.com/user-attachments/assets/d4e00d8a-d3e9-4638-839a-2d93eb0ae928)

## Installation

Add this URL as a custom plugin repository in Dalamud:

```text
https://raw.githubusercontent.com/izzetus/EzWondrousTails2/master/pluginmaster.json
```

Then search for `ezWondrousTails 2` in the Dalamud plugin installer.

Disable or uninstall the original `ezWondrousTails` before enabling this fork. The fork uses a distinct internal plugin identity so Dalamud does not treat it as an unsafe replacement for the version formerly distributed through the official repository.

## Building

Clone the repository and its KamiToolKit submodule, then build with the .NET 10 SDK and a current Dalamud development installation:

```shell
git clone --recurse-submodules https://github.com/izzetus/EzWondrousTails2.git
dotnet build EzWondrousTails2/WondrousTailsSolver.sln -c Release
```
