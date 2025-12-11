After [**[extracting game resources]**](Creating-Mods), each language's text strings can be found in the extracted "*generated/binaryfile/strings/\*.bfile*" files. If you open "*english.bfile*" in a raw text editor, you'll see the following:
```
// string table
//

{
	"
	"	"\t"
	"#STR_SGL04_MAPBRIEF_THEGAUNTLET01"	"Adjust the properties on doors in this level so you can walk from the start module to the exit without letting any demons hit the red triggers."
	"#STR_SGL04_MAPDESC_THEGAUNTLET"	"Learn how to work with doors and keycards"
	"#STR_SGL04_MAP_THEGAUNTLET"	"Doors and Keycards"
	"#STR_SGL10_MAPBRIEF_MODULEMASTERY"	"Find the module with the player start and connect it to the module with the exit"
	[...]
	"#str_dossier_arsenal_weapon_shotgun_desc"	"Effective at medium and close range, the Shotgun is a versatile weapon for most encounters."
	[...]
}
```
Ignoring the `\t` line, each string is comprised of two parts: The name (`"#str_dossier_arsenal_weapon_shotgun_desc"`), and the text (`"Effective at medium and close range, the Shotgun is a versatile weapon for most encounters."`).\
Some strings also contain two numbers after the text (`"#str_zion_weapon_shotgun" "Shotgun" "22" "22"`), but this just seems to be a "preferable max string length" for localisers, and can be ignored.

\
To create custom text strings, create a raw text file at "*generated/binaryfile/strings/english.bfile*" in your mod, add a pair of curly brackets (`{}`), and add each string you want on a separate line between them. Example:
```
{
	"#str_zion_weapon_shotgun"	"Scatter Gun"
	"#str_dossier_arsenal_weapon_shotgun_desc"	"Powered by ^nRed Eco^z, packs a punch up close."
	"#str_zion_weapon_heavy_rifle_heavy_ar"	"Blaster"
	"#str_dossier_arsenal_weapon_har_desc"	"Powered by ^dYellow Eco^z, fires a long-range shot."
}
```
> [!TIP]
> Changes to "*english.bfile*" will be applied to all languages. If you won't translate your text per-language, then you only need one file.

> [!NOTE]
> You shouldn't include any unchanged strings in your mod.

<br/>

`//` and `/* ... */` comments are supported.\
Quotes, backslashes, and line breaks must be escaped as `\"`, `\\`, and `\n`, respectively.

Carets (`^`) can be used to change the text's colour. In the above example, $"\textit{\color{#DE1133}Red Eco}"$ is red, and $"\textit{\color{#FDEA5A}Yellow Eco}"$ is yellow.\
The colour is determined by the first character after the caret:

<table>
	<tr/><!--Skip uneven rows, so that we only see the differently-coloured even rows-->
	<tr>
		<td title="^1: Red">
			$\color{#FF0000}\verb|^1|$
		</td>
		<td title="^2: Green">
			$\color{#00FF00}\verb|^2|$
		</td>
		<td title="^3: Yellow">
			$\color{#FFFF00}\verb|^3|$
		</td>
		<td title="^4: Blue">
			$\color{#0000FF}\verb|^4|$
		</td>
		<td title="^5: Cyan">
			$\color{#00FFFF}\verb|^5|$
		</td>
		<td title="^6: Magenta">
			$\color{#FF00FF}\verb|^6|$
		</td>
		<td title="^7: White">
			$\color{#FFFFFF}\verb|^7|$
		</td>
		<td title="^8: Grey">
			$\color{#7F7F7F}\verb|^8|$
		</td>
		<td title="^9: Black">
			$\color{#000000}\verb|^9|$
		</td>
	</tr>
	<tr/>
	<tr>
		<td title="^a/^J: DOOM Red (Muted brown)">
			$\color{#9B4938}\verb|^a|$<br/>
			$\color{#9B4938}\verb|^J|$
		</td>
		<td title="^b/^K: DOOM Blue (Tealish grey)">
			$\color{#4F707C}\verb|^b|$<br/>
			$\color{#4F707C}\verb|^K|$
		</td>
		<td title="^c/^L: DOOM Orange (Yellowish orange)">
			$\color{#FFA600}\verb|^c|$<br/>
			$\color{#FFA600}\verb|^L|$
		</td>
		<td title="^d/^M: GUI Yellow (Slightly desaturated yellow)">
			$\color{#FDEA5A}\verb|^d|$<br/>
			$\color{#FDEA5A}\verb|^M|$
		</td>
		<td title="^e/^N: GUI Red (Reddish orange)">
			$\color{#F6492B}\verb|^e|$<br/>
			$\color{#F6492B}\verb|^N|$
		</td>
		<td title="^f/^O: SnapMap GUI Blue (Sky blue)">
			$\color{#99CCFF}\verb|^f|$<br/>
			$\color{#99CCFF}\verb|^O|$
		</td>
		<td title="^g: Dossier Orange (Orange)">
			$\color{#FF6D18}\verb|^g|$<br/>
			<br/>
		</td>
		<td title="^h: Dossier Blue (Muted azure blue)">
			$\color{#1995CE}\verb|^h|$<br/>
			<br/>
		</td>
		<td title="^i: Feed Map (Muted cyanish blue)">
			$\color{#40ADD1}\verb|^i|$<br/>
			<br/>
		</td>
	</tr>
	<tr/>
	<tr>
		<td title="^j: Feed Category (Muted green)">
			$\color{#5FC443}\verb|^j|$
		</td>
		<td title="^k: Feed Your Item (Muted orange)">
			$\color{#DC7328}\verb|^k|$
		</td>
		<td title="^l: DOOM Username (White)">
			$\color{#FFFFFF}\verb|^l|$
		</td>
		<td title="^m: Platform Username (Very slightly bluish grey)">
			$\color{#808688}\verb|^m|$
		</td>
		<td title="^n: Menu Shell Red (Muted red)">
			$\color{#DE1133}\verb|^n|$
		</td>
		<td title="^o: Feed Sidebar Text (Very slightly bluish grey)">
			$\color{#808688}\verb|^o|$
		</td>
		<td title="^p: Feed Main Text (Silver)">
			$\color{#BEC5C8}\verb|^p|$
		</td>
	</tr>
	<tr/>
	<tr>
		<td align="center" colspan="9" title="^z: Default"><i>^z: Reset to the default colour</i></td>
	</tr>
</table>

If a caret is followed by any character besides 0-9, A-Z, a-p, z, and `` :;<=>?@[\]^_` ``, then the literal caret and the following character will be displayed without changing the text's colour.\
Carets cannot be escaped. If you need to display a literal caret before any of those characters, U+02C6 Modifier Letter Circumflex Accent (`ˆ`) looks similar and can sometimes be used instead.

> [!WARNING]
> Lowercase letters will result in unexpected colours if the game displays your string as all caps. Use `^J`-`^O` instead of `^a`-`^f` in this case.\
> There's no uppercase equivalent for `^g`-`^p` nor `^z`.

> [!WARNING]
> A string can only change colour up to 20 times, or less in some cases.
