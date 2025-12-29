<h2 align="center">DOOMModLoader</h2>
<p align="center">A mod loader for DOOM (2016)</p>
<p align="center"><a href="/../../releases/latest/download/DOOMModLoader-Windows-x64.zip" title="Download for Windows x64"><img src="ReadMe - Download for Windows.svg" alt="[Download for Windows x64]" width="212" height="32"/></a>&ensp;<a href="/../../releases/latest/download/DOOMModLoader-Linux-x64.zip" title="Download for Linux/SteamOS x64"><img src="ReadMe - Download for Linux.svg" alt="[Download for Linux/SteamOS x64]" width="249" height="32"/></a></p>

**Installation:** Right-click DOOM (2016) in your Steam library, and choose "*Manage*" > "*Browse local files*". This will open a File Explorer window in your DOOM (2016) installation folder. Download DOOMModLoader from one of the buttons above or from [**[the Releases page]**](/../../releases/latest), and extract it into that folder.

**Usage:** Place mod zips into a "*Mods*" folder in your DOOM (2016) installation (without extracting them!), and run DOOMModLoader to install them. To uninstall mods, move them out of the "*Mods*" folder and run DOOMModLoader again.\
After running DOOMModLoader once, a "*DOOMModLoaderSettings.txt*" file will be created, which you can edit with a raw text editor to configure settings.
###### 🐧 *Note: On Linux/SteamOS, you should right-click DOOMModLoader and choose "Run In Konsole", or otherwise run it in a terminal.<br/>On Steam Deck, you can press Steam+X to open a virtual keyboard when you need to press Y/N to continue.*

\
For more help installing or creating mods, see [**[the wiki]**](/../../wiki).\
<br/>

---

\
**Building:** To build/compile DOOMModLoader yourself, use .NET 10.0 SDK with `dotnet publish "./DOOMModLoader.csproj" --no-self-contained` for a runtime-dependent executable.\
Alternatively, fork this repository and push a commit to any branch. This will trigger [**[an automated native build]**](/../../actions).

## Special Thanks

[**@emoose**](https://github.com/emoose): Created the original DOOMExtract, DOOMModLoader, and idCrypt\
PowerBall253 ([**@brunoanc**](https://github.com/brunoanc)): Created the DOOM (2016) executable patch to not require developer mode
