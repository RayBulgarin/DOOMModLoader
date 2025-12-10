First, you'll need to extract the vanilla game resources. After installing DOOMModLoader, open a terminal/command prompt in your DOOM (2016) installation, type `.\DOOMModLoader.exe -extract` (on Windows) or `./DOOMModLoader -extract` (on Linux/SteamOS), and press Enter. You should see a message like this:
```
D:\SteamLibrary\steamapps\common\DOOM>.\DOOMModLoader.exe -extract
    DOOMModLoader by Zwip-Zwap Zapony and PowerBall253, originally by infogram
      https://github.com/ZwipZwapZapony/DOOMModLoader

Resource Extraction:

Container to extract:
    D:\SteamLibrary\steamapps\common\DOOM\base\gameresources.pindex
Output directory:
    D:\SteamLibrary\steamapps\common\DOOM\gameresources\
Resources to extract: Useful only (Recommended)
Convert CRLF (\r\n) newlines to LF (\n): Yes (Recommended)
Expected size on disk: ~0.4 GiB

Use the recommended extraction settings?

(Press [Y] to begin extracting resources)
(Press [N] to customise settings)
```
If this looks all right to you, then press **[Y]** to begin extracting game resources. Press **[N]** if you want to read about and change the settings first. The extracted resources will be in a "*gameresources*" folder in your DOOM (2016) installation.

Next, create a new folder with any name you'd like within your "*Mods*" folder, and copy any of the extracted files into your folder while keeping the folder hierarchy intact.\
For example, "*-/DOOM/gameresources/generated/decls/weapon/weapon/zion/player/sp/pistol.decl*" would become "*-/DOOM/Mods/My Awesome Mod/generated/decls/weapon/weapon/zion/player/sp/pistol.decl*".\
Edit the copied files in your mod folder, and run DOOMModLoader to test them.

Once your mod is finished, create a zip of the mod folder's contents.\
Enter your mod folder, select all files and folders used by your mod (e.g. "*generated*" and/or "*maps*"), and right-click one of them. On Windows, choose "*Send to*" > "*Compressed (zipped) folder*". On SteamOS, choose "*Compress*" > "*Compress to "Archive.zip"*". Then, rename the zip to the name of your mod.

Lastly, try running DOOMModLoader with nothing but your zip in the "*Mods*" folder. If it works, congratulations! You can now share that zip online for people to enjoy!\
Otherwise, double-check that the folder hierarchy in the zip is correct - when you open the zip, the first thing that you should see is a folder like "*generated*" or "*maps*", not your mod folder's name.

<br/>

> [!TIP]
> You can set `verbose` to `true` in [**["*DOOMModLoaderSettings.txt*"]**](Installing-Mods-~-Settings) to see whether your custom file replaces an existing resource; if it says "*added*" instead, then you might have a typo in the file path.

> [!TIP]
> To extract SnapMap resources, use `.\DOOMModLoader.exe -extract -snapmap` or `./DOOMModLoader -extract -snapmap`. SnapMap resources will be extracted to a "*snap_gameresources*" folder.

---

### Further reading:
[**[Mod.Decl]**](Creating-Mods-~-Mod.Decl) - Auto-execute console commands/variables.\
[**[Text Strings]**](Creating-Mods-~-Text-Strings) - Add or replace lines of text.\
[**[Tips]**](Creating-Mods-~-Tips) - Miscellaneous tips and tricks.\
[**[Videos]**](Creating-Mods-~-Videos) - Convert and use custom videos.\
[**[https://wiki.eternalmods.com/]**](https://wiki.eternalmods.com/) - A community-run DOOM Eternal modding wiki, which also houses information useful for DOOM (2016).
