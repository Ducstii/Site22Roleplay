# Site22Roleplay

## Description

A custom EXILED plugin for SCP: Secret Laboratory, focusing on enhancing roleplay with features like a private messaging system, custom commands for server management and player interaction, and potentially a web interface.

## Setup

This project targets **.NET Framework 4.8** and requires the **EXILED** framework for SCP: Secret Laboratory.

**Prerequisites:**

*   .NET Framework 4.8 SDK
*   EXILED installed on your SCP: SL Dedicated Server
*   Access to the game's managed DLLs (`Assembly-CSharp.dll`, `UnityEngine.CoreModule.dll`, `Assembly-CSharp-firstpass.dll`)

**Steps:**

1.  Clone the repository.
2.  Open the `.csproj` file in a compatible IDE (like Visual Studio or Rider).
3.  Ensure the `HintPath` references in the `.csproj` file point to the correct locations of your game's managed DLLs.
4.  Restore NuGet packages.
5.  Build the project in Release mode.
6.  Copy the compiled `Site22Roleplay.dll` from the `bin/Release` directory to your EXILED plugins folder (`EXILED/Plugins`).
7.  (Optional) Configure the plugin settings in the `Site22Roleplay.yml` file that will be generated in your `EXILED/Configs` directory after the first run. Refer to the `Config.cs` file for available settings.

## Usage

The plugin adds several commands and features:

*   **Remote Admin Commands (`s22`):** Accessible via the Remote Admin console.
    *   `.s22 openlobby`: Opens the lobby and teleports players.
    *   `.s22 initiateroleplay`: Initiates the roleplay scenario.
    *   `.s22 warpto <player id/name> <x> <y> <z>`: Warps a player to specified coordinates.
*   **Client Commands (`sms`):** Accessible via the in-game client console (`~`).
    *   `.sms help`: Shows SMS command help.
    *   `.sms contacts`: Shows available SMS contacts.
    *   `.sms send <number> <message>`: Sends an SMS message.
*   **Server Specific Menu:** Role selection is handled via the Server Specific menu (SSM), replacing the old `.role` commands.
*   **Web Server (Under Development):** [Describe the web server functionality if applicable and stable].

## Credits
- Site 12, massive inspiration. This was a project I wanted to attempt to make, and improve.
