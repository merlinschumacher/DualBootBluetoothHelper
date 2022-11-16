# DualBootBluetoothHelper

A tool to dump Windows Bluetooth connection keys. These can be imported into Linux and macOS systems.
The tool extracts Windows Bluetooth encryption keys from the registry and dumps them into a JSON file. From there you can import them into your other operating system. This relives you of the dual boot issues with Bluetooth hardware.

## Usage

To run the program you need [Sysinternals psexec](https://learn.microsoft.com/en-us/sysinternals/downloads/psexec). psexec is used to elevate from normal Administrator priviledges to System priviledges. This is necessary, because the Bluetooth keys are stored in a protected area of the registry.

Start the program as follows:

- Open a Windows PowerShell with Administrator priviledges.
- Run `C:\Where\Ever\psexec64.exe -s -i C:\Where\Ever\DualBootBluetoothHelper.exe C:\bt.json`

NOTE: You *must* pass the complete path to the DualBootBluetoothHelper.exe to psexec or else it won't work.
