<table align="center">
	<thead>
		<tr/><!--Skip uneven rows, so that we only see the differently-coloured even rows-->
		<tr>
			<th>Contents:</th>
		</tr>
	</thead>
	<tbody>
		<tr/>
		<tr>
			<td>
				<ul>
					<li><a href="#doomlegacymod"><b>[DoomLegacyMod]</b></a> - Unhides hidden console commands/variables</li>
					<li><a href="#run-doommodloader-from-a-text-editor"><b>[Run DOOMModLoader from a text editor]</b></a> - Test changes faster</li>
					<li><a href="#test-mods-without-re-running-doommodloader"><b>[Test mods without re-running DOOMModLoader]</b></a> - Useful for "<i>*.entities</i>" mods</li>
					<li><a href="#idcrypt"><b>[idCrypt]</b></a> - Decrypt or re-encrypt certain files</li>
				</ul>
			</td>
		</tr>
	</tbody>
</table>

---

## DoomLegacyMod
Unhides hidden console commands/variables, and reimplements the `infiniteHealth`, `noclip`, `noPlayerDeath`, `noPlayerKill`, and `noTarget` commands.\
Originally by [**@emoose**](https://github.com/emoose), updated for the 2024 Steam version by PowerBall253 ([**@brunoanc**](https://github.com/brunoanc)).

To install, make sure that you have the Steam version of DOOM (2016), download "*DOOMLegacyMod.-.v202407.beta.zip*" from [**[this page]**](https://github.com/brunoanc/DOOMLegacyMod/releases/latest), and extract the zip's "*dinput8.dll*" file into your DOOM (2016) installation. The DLL should be in the same folder as "*DOOMx64.exe*".

> [!NOTE]
> 🐧 **On Linux/SteamOS,** also right-click DOOM (2016) in your Steam library, choose "*Properties...*" > "*General*", and add `WINEDLLOVERRIDES="dinput8=n,b" %command%` to the "*Launch Options*" field. If there's already something there, then add this in front.

> [!WARNING]
> DoomLegacyMod doesn't support the GOG version of DOOM (2016), nor DOOM VFR.

---

## Run DOOMModLoader from a text editor
With an advanced raw text editor, you can often run programs from within the text editor itself. This can help speed up mod development by making it faster to load and test your changes.

[**[Notepad++]**](https://notepad-plus-plus.org/) is commonly used for DOOM (2016) modding on Windows, and [**[Kate]**](https://kate-editor.org/) is multiplatform and comes preinstalled on SteamOS. This page contains instructions for both:

<details>
	<summary>$\color{orange}\textsf{[Click to expand Notepad++ instructions]}$</summary>

At the top-left of Notepad++, click "*Plugins*" > "*Plugins Admin...*" > "*Available*", install NppExec if it's not already installed, and click "*Close*".\
Press F6 to open NppExec's script window. Copy and paste...
```
CD /D "D:\SteamLibrary\steamapps\common\DOOM"
".\DOOMModLoader.exe"
```
...into it, and change the `CD` path to where your DOOM (2016) installation is. Click "*Save...*", name the script "*DOOMModLoader*", click "*Save*", and click "*OK*".

DOOMModLoader should now open and run at the bottom of Notepad++'s window. From now on, you can press Ctrl+F6 to re-run DOOMModLoader while using Notepad++, immediately loading your latest changes.

> ℹ️ **Note**\
> After closing and relaunching Notepad++, Ctrl+F6 will open NppExec's script window, making you confirm which script to run once per Notepad++ session.\
> You can also open the script window on demand by pressing F6 without holding Ctrl; Ctrl+F6 simply re-runs the latest script.

> 💡 **Tip**\
> You can change NppExec's hotkey from F6 by changing "*Plugins*" > "*NppExec*" > "*Advanced Options...*" > "*Options*" > "*HotKey*".

> 💡 **Tip**\
> NppExec supports substitutions in scripts. This isn't useful for DOOMModLoader, but for other programs, you might enjoy e.g. `"$(FULL_CURRENT_PATH)"` pointing to the current file.
</details>

<details>
	<summary>$\color{orange}\textsf{[Click to expand Kate instructions]}$</summary>

At the top-left of Kate, click "*Settings*" > "*Configure Kate...*". Switch to the "*Plugins*" tab, make sure that "*External Tools*" is enabled, and click "*Apply*" if applicable.\
Switch to the "*External Tools*" tab, and click "*Add*" > "*Add Tool...*".

Set "*Name*" to "*DOOMModLoader*", click on the file picker icon next to "*Executable*" and select DOOMModLoader, click on the file picker icon next to "*Working directory*" and select the "*-/steamapps/common/DOOM/*" folder, and set "*Output*" to "*Display in Pane*".\
Now that the tool is set up, click "*OK*", and click "*OK*" again.

Click "*Tools*" > "*External Tools*" > "*Uncategorized*" > "*DOOMModLoader*", and DOOMModLoader should now open and run at the bottom of Kate's window.\
If there's an error while installing mods, you'll have to click "*External Tools*" at the bottom-left of Kate to see DOOMModLoader's output.

> 💡 **Tip**\
> You can set a hotkey by changing "*Settings*" > "*Configure Keyboard Shortcuts...*" > "*externaltools*" > "*DOOMModLoader*".

> 💡 **Tip**\
> Kate supports substitutions in tool arguments. This isn't useful for DOOMModLoader, but for other programs, you might enjoy e.g. `"%{Document:NativeFilePath}"` pointing to the current file.\
> For a list of all substitutions, click on the `{}` icon at the end of the "*Arguments*" field.
---
</details>

Other text editors often also support this. Try looking up "*run script in [name of text editor]*" or "*external tools in [name of text editor]*"!

---

## Test mods without re-running DOOMModLoader
With DoomLegacyMod, it's possible to load loose files directly in-game. This lets you iterate faster on mods, as you don't have to wait for DOOMModLoader to install your changes.

Install DoomLegacyMod ([**[see above]**](#doomlegacymod)), right-click DOOM (2016) in your Steam library, choose "*Properties...*" > "*General*", and add `+resource_loadLooseAssets 1` to the "*Launch Options*" field.\
DOOM (2016) will now load loose files from the "*base*" folder. Example paths:
```
-/steamapps/common/DOOM/base/generated/decls/weapon/weapon/zion/player/sp/pistol.decl
-/steamapps/common/DOOM/base/maps/game/sp/intro/intro.entities
```
This is most useful for map "*\*.entities*" file mods, as the file can be partially reloaded by restarting a mission or loading a checkpoint, or fully reloaded by exiting to the main menu and re-entering the map. Other resource types may require you to reboot the game to be reloaded.\
(Some entities are also stored in the save data, so some changes need the mission to be restarted.)

Don't forget to move the files into your mod's folder after working on them!

> [!TIP]
> You can combine this with DOOMModLoader; load certain files from the "*base*" folder while working on them, and load the rest with DOOMModLoader.

> [!WARNING]
> [**[Text string]**](Creating-Mods-~-Text-Strings) files ("*generated/binaryfile/strings/\*.bfile*") cause the game to crash when loaded this way. [**["*mod.decl*"]**](Creating-Mods-~-Mod.Decl) files are ignored.

---

## idCrypt
A few of DOOM (2016)'s resources are encrypted. DOOMModLoader automatically extracts and loads decrypted files, so you don't have to worry about this for DOOM (2016) or DOOM VFR.

However, if it's necessary for other id Tech games, you can manually invoke "*idCrypt*" to decrypt or re-encrypt a file. Examples:
```
.\DOOMModLoader.exe -decrypt "./english.blang" "strings/english.blang"
.\DOOMModLoader.exe -encrypt "./english.blang.dec" "strings/english.blang"
```

> [!NOTE]
> This isn't necessary for DOOM (2016) nor DOOM VFR.
