By creating a file called "*mod.decl*" at the root of your mod, you can specify special properties about your mod. You can use this template for the file:
```cpp
{
	autoExec = "";
	modLoaderVersion = "0.6";
}
```
Currently, this is only useful for automatically executing console commands/variables.

### `autoExec`
Executes commands or sets variables in the console when launching the game. To use multiple commands/variables, separate them with a semicolon. Quotes and backslashes must be escaped as `\"` and `\\`, respectively. Example: `autoExec = "echo \"Hello world\"; pm_crouchToggle 0";`

> [!NOTE]
> Some commands/variables are blacklisted from use, such as `bind`, `quit`, or `com_skipIntroVideo`.\
> This is to avoid altering variables that are permanently saved to your save data, running annoying commands, or using something that doesn't work with DOOMModLoader's console method.

### `modLoaderVersion`
If your mod has a "*mod.decl*" file, this is required. Specifies which DOOMModLoader version this mod is made for. Example: `modLoaderVersion = "0.6";`
