using DOOMModLoader.Shared;
using System;
using System.IO;
using System.IO.Enumeration;

// Determines a resource's type and short name from its full path

using NonDecl = (string Type, string Filter, string? ShortName);

namespace DOOMModLoader.LoadMods;
static class ResourceType
{
	/* This correctly guesses the type for all of DOOM (2016)'s full resource paths, except...
	- "generated/decls/renderparm/decals.decl" and "lights.decl", where we guess "renderParm" but the game incorrectly reuses the previous resource's type ("renderProg")
	- "generated/swf/*.bimage", where we guess "image" but the game uses both "file" and "image"

	This also correctly guesses the short name for all full resource paths, except...
	- "cooked/model/*.bmodel", where we guess "*.lwo" but the game rarely replaces an underscore with a space or a hyphen
	- "decls/renderprogs/*.inc", where we guess "*" but the game maps a single full path to multiple short names
	- "fonts/ * /64_df.dat", where we guess "*" but the game only mostly replaces underscores with spaces
	- "generated/cm/*.bcm", where we guess "*.lwo" but the game rarely replaces an underscore with a space or a hyphen
	- "generated/decls/renderparm/decals.decl" and "lights.decl", where we guess "decals" and "lights" but the game incorrectly reuses the previous resource's short name ("alphatintoutside")
	- "generated/image/textures/*.bimage", where we guess "textures/*.tga" but the game rarely uses "textures/*.png"
	- "generated/image/*.bimage" (without "$"), where we guess "*.tga" but the game rarely uses "*"
	- "generated/lightatlas/*.bimage", where we guess "*" but the game rarely uses "*.tga"
	- "generated/maps/modules/*.baas_*", where we guess the full path but the game rarely removes one slash
	- "generated/models/*.pmodel", where we guess "*.lwo" but the game rarely removes one slash
	- "generated/spirv/*.*spv", where we guess "*" (without a slash in "_0000000000000001" if relevant) but the game rarely removes one slash */



	// All types used in DOOM (2016)'s, its demo's, SnapMap's, and DOOM VFR's "generated/decls/" resources
	// id Tech 6 likely also supports other decl types not present here
	static readonly string[] declTypes = [ // Must be pre-sorted by "StringComparer.OrdinalIgnoreCase"
		"accolade",
		"actorModifier",
		"actorPopulation",
		"advancedScreenViewShake",
		"aiBehavior",
		"aiBehaviorEvents",
		"aiBehaviorVo",
		"aiComponent_Attack",
		"aiComponent_Cacodemon",
		"aiComponent_Cyberdemon",
		"aiComponent_ExtendedSense",
		"aiComponent_Flight",
		"aiComponent_Hellknight",
		"aiComponent_Jetpack",
		"aiComponent_LaserTargeter",
		"aiComponent_LostSoul",
		"aiComponent_MissileBarrage",
		"aiComponent_OliviasGuard",
		"aiComponent_PathManager",
		"aiComponent_PositionAwareness",
		"aiComponent_Prototypes",
		"aiComponent_Resurrector",
		"aiComponent_Scythe",
		"aiComponent_SpiderMastermind",
		"aiComponent_Strategy",
		"aiComponent_TransientFocus",
		"aiComponent_WaveBlast",
		"aiEvent",
		"aiFSMManager",
		"aiGlobalSettings",
		"aimAssist",
		"aiMonsterTypeInheritance",
		"aiMovementGraph",
		"AIPainGraph",
		"aiPoolNumbers",
		"aiPositioningParms",
		"aiSensorySettings",
		"aiSpawnTimeSettings",
		"aiThreatManagement",
		"aiTurnParms",
		"aiVignetteGraph",
		"aiVoiceOver",
		"ammo",
		"animWeb",
		"arcadeScoring",
		"arGui",
		"articulatedFigure",
		"attackGraph",
		"audioLog",
		"audioLogStory",
		"AutomapViewerPOIList", // Only used in DOOM VFR
		"berzerkattack",
		"boostPack",
		"botBehaviorTreeManager",
		"botDef",
		"botSkillDef",
		"breakable",
		"cameraTrigger",
		"challengeList",
		"chargeattack",
		"cheatCode",
		"cloth",
		"codex",
		"collectibles",
		"combatEncounterScoring",
		"commodities",
		"commodityconversion",
		"commodityWallet",
		"credits",
		"damage",
		"deathCamDef",
		"decoyHologram",
		"designSystem_turret_upgrades", // Only used in SnapMap
		"designSystem_upgrades", // Only used in SnapMap and DOOM VFR
		"devInvLoadout",
		"devMenuOption",
		"dynamicChallenge",
		"ebolt",
		"entityDamage",
		"entityDef",
		"entityDefFloatList",
		"env",
		"explosion",
		"faction",
		"factionGraph",
		"FKGraph",
		"flare",
		"focusattack",
		"footstepEvents",
		"fx",
		"gameDifficulty",
		"gameEventCallout",
		"gamefact", // Only used in DOOM (2016)'s demo
		"gamefacts",
		"gameMode",
		"goreBehavior",
		"goreGraph",
		"gorewounds",
		"hackModule",
		"handsBobCycle",
		"hotSpot", // Only used in SnapMap
		"ignoreModeUI",
		"impactSounds",
		"interaction",
		"inventoryItem",
		"jumpBoots",
		"layer",
		"loadout",
		"loadoutArmor",
		"lootDrop",
		"mapEOLStats",
		"mapInfo",
		"material", // Also a non-"generated/decls/" type
		"md6Def",
		"metric",
		"missionSelectInfoList",
		"nineSlice",
		"objective",
		"onlineLevel",
		"outlines",
		"particle",
		"perkGroups",
		"perks",
		"playerProps",
		"progressionManager",
		"projectile",
		"projectileImpactEffect",
		"propArcadeMode",
		"propAttribs",
		"propBecomeDemon",
		"propBecomeDemonThink",
		"propBillboard", // Only used in SnapMap
		"propBreakable",
		"propCoopAmmo", // Only used in SnapMap
		"propCoopBackpack", // Only used in SnapMap
		"propCoopRotateBob",
		"propDamage",
		"propHealth",
		"propIncomingDemon",
		"propItem",
		"propMoveable",
		"propStatusEffect",
		"propThinkFreezeTag",
		"propThinkSoulDrop",
		"propUniversalAmmo",
		"propUseFreezeTag",
		"propUseSoulDrop",
		"pvpMap",
		"renderParm",
		"renderProg", // Also a non-"generated/decls/" type
		"renderProgFlag",
		"researchDossierOrder",
		"researchUnlockableGroup",
		"rewardTable",
		"ribbon",
		"rumble",
		"screenViewShake",
		"securityUnlock",
		"shellMenu",
		"simpleHealthComponents", // Only used in SnapMap and DOOM VFR
		"skins",
		"snapAIEncounter",
		"snapAIEncounterTable",
		"snapAIScore", // Only used in SnapMap
		"snapBackgroundDef",
		"snapCapList", // Only used in SnapMap
		"snapCustomIcon",
		"snapCustomTileOptions",
		"snapDroppable",
		"snapEditorAssetBank",
		"snapEditorEntityBank",
		"snapEditorEntityDef",
		"snapEditorSettings",
		"snapEntityStateIconBank", // Only used in SnapMap
		"snapLayers",
		"snapModuleInfo", // Only used in SnapMap
		"snapPalette", // Only used in SnapMap
		"snapPaletteGroup", // Only used in SnapMap
		"snapPropertyInspector_Angle", // Only used in SnapMap
		"snapPropertyInspector_AnimWebPath", // Only used in SnapMap
		"snapPropertyInspector_BitFlag",
		"snapPropertyInspector_BlockingVolumeRenderModel", // Only used in SnapMap
		"snapPropertyInspector_Bool",
		"snapPropertyInspector_CachedEntity",
		"snapPropertyInspector_CapDecl", // Only used in SnapMap
		"snapPropertyInspector_Color",
		"snapPropertyInspector_ColorSwatch", // Only used in SnapMap
		"snapPropertyInspector_Decal", // Only used in SnapMap
		"snapPropertyInspector_DoorProperties", // Only used in SnapMap and DOOM (2016)'s demo
		"snapPropertyInspector_DroppableDecl",
		"snapPropertyInspector_EchoProperty", // Only used in SnapMap
		"snapPropertyInspector_EnableBobAndRotate",
		"snapPropertyInspector_EncounterDecl", // Only used in SnapMap
		"snapPropertyInspector_EncounterDeclList", // Only used in SnapMap
		"snapPropertyInspector_EntityPicker",
		"snapPropertyInspector_Enum",
		"snapPropertyInspector_Enum_PowerCoreStation", // Only used in SnapMap
		"snapPropertyInspector_Environment",
		"snapPropertyInspector_Float",
		"snapPropertyInspector_GameCalloutDecl", // Only used in SnapMap
		"snapPropertyInspector_GuiScale", // Only used in SnapMap
		"snapPropertyInspector_HideAtStart", // Only used in SnapMap
		"snapPropertyInspector_HudInfo", // Only used in SnapMap
		"snapPropertyInspector_Icon",
		"snapPropertyInspector_Int",
		"snapPropertyInspector_InventoryDecl", // Only used in SnapMap
		"snapPropertyInspector_LightColor", // Only used in SnapMap
		"snapPropertyInspector_LightIntensity", // Only used in SnapMap
		"snapPropertyInspector_LoadoutWeapons", // Only used in SnapMap
		"snapPropertyInspector_Material", // Only used in SnapMap
		"snapPropertyInspector_MusicDecl", // Only used in SnapMap
		"snapPropertyInspector_Percentage", // Only used in SnapMap
		"snapPropertyInspector_PlayerModifier", // Only used in SnapMap
		"snapPropertyInspector_PlayerResource", // Only used in SnapMap
		"snapPropertyInspector_PropFX", // Only used in SnapMap
		"snapPropertyInspector_PublishedMap", // Only used in SnapMap
		"snapPropertyInspector_Radius", // Only used in SnapMap
		"snapPropertyInspector_Range_Float", // Only used in SnapMap
		"snapPropertyInspector_Range_Int", // Only used in SnapMap
		"snapPropertyInspector_Range_Time", // Only used in SnapMap
		"snapPropertyInspector_RenderModel", // Only used in SnapMap
		"snapPropertyInspector_Rotation", // Only used in SnapMap
		"snapPropertyInspector_SpawnerPair", // Only used in SnapMap
		"snapPropertyInspector_SpeakerDecl", // Only used in SnapMap
		"snapPropertyInspector_StatusEffectDecl", // Only used in SnapMap
		"snapPropertyInspector_StringBuilder", // Only used in SnapMap
		"snapPropertyInspector_StringParamList",
		"snapPropertyInspector_Text",
		"snapPropertyInspector_Time",
		"snapPropertyInspector_Vec2", // Only used in SnapMap
		"snapPropertyInspector_Vec3", // Only used in SnapMap
		"snapPropertyInspector_Vec4", // Only used in SnapMap
		"snapPropertyInspector_VoDecl", // Only used in SnapMap
		"snapPropertyInspector_WeaponComponentFromEntity", // Only used in SnapMap
		"snapPropertyInspector_WeaponFilter", // Only used in SnapMap
		"snapPropertyInspector_WhiteListEncounterDecl", // Only used in SnapMap
		"snapPropertyInspector_WhiteListEncounterDeclList",
		"snapTileImage",
		"snapTutorial", // Only used in SnapMap
		"snapTutorialMapList", // Only used in SnapMap
		"sound",
		"spawnInfluencer",
		"staticImage",
		"statusEffect",
		"summonweapon",
		"syncInteractions",
		"table",
		"targeting",
		"throwable",
		"tooltip",
		"trackingParms",
		"tutorialEvent",
		"twitchPain",
		"uicallout", // Only used in SnapMap
		"universalTraversalTable",
		"unlock",
		"unlockable",
		"unlockArmor",
		"unlockInventory",
		"unlockModel",
		"unlockSnapTile",
		"unlockVanity",
		"upgrade",
		"vrSelector", // Only used in DOOM VFR
		"walkIK",
		"weapon",
		"weaponDataArcCannon",
		"weaponDataChainGun",
		"weaponDataChainsaw",
		"weaponDataChainsawSlice",
		"weaponDataChargeablePistol",
		"weaponDataChargeball",
		"weaponDataDoubleBarrelShotgun",
		"weaponDataLightningGun",
		"weaponDataMancubusGland",
		"weaponDataPlasmaRifle",
		"weaponDataRailGun",
		"weaponDataSalvoCannon",
		"weaponDataStaticCannon",
		"weaponReticle",
		"weaponReticleSWFInfo",
	];

	// All remaining types used by resources in "generated"
	// If the short name is "null" here, it's identical to the full name
	static readonly NonDecl[] generatedTypes = [
		("aas",                 "generated/maps/*.baas_botplayer",       "maps/*.aas_botplayer"),
		("aas",                 "generated/maps/*.baas_monster16",       "maps/*.aas_monster16"), // Not used in the vanilla game
		("aas",                 "generated/maps/*.baas_monster24",       "maps/*.aas_monster24"), // Only used in DOOM VFR
		("aas",                 "generated/maps/*.baas_monster48",       "maps/*.aas_monster48"),
		("aas",                 "generated/maps/*.baas_monster96",       "maps/*.aas_monster96"),
		("aas",                 "generated/maps/*.baas_monster128",      "maps/*.aas_monster128"),
		("aas",                 "generated/maps/*.baas_monster256",      "maps/*.aas_monster256"),
		("aas",                 "generated/maps/*.baas_player",          "maps/*.aas_player"), // Not used in the vanilla game
		// "generated/anim/*.md6skl/_default.bmd6anim" would've been fine for DOOM (2016), SnapMap, and DOOM VFR,
		// but DOOM (2016)'s demo proves that any "generated/anim/*.bmd6anim" path can exist
		("anim",                "generated/anim/*.bmd6anim",             "*.md6anim"),
		("baseModel",           "generated/basemodel/*.bmd6model",       "*.md6mesh"),
		("binaryFile",          "generated/binaryfile/strings/*.bfile",  "strings/*.lang"), // Strings must come before CFGs
		("binaryFile",          "generated/binaryfile/*.bfile",          "*.cfg"), // CFGs must come after strings
		("cm",                  "generated/cm/*.bcm",                    "*.lwo"),
		("decalatlas",          "generated/decalatlas/fullscalebias_*.bimage",      "fullscalebias *.tga"), // "fullscalebias" must come before non-"fullscalebias"
		("decalatlas",          "generated/decalatlas/pad4_fullscalebias_*.bimage", "pad4 fullscalebias *.tga"), // "pad4_fullscalebias" must come before "pad4"
		("decalatlas",          "generated/decalatlas/pad4_*.bimage",    "pad4 *.tga"), // "pad4" must come before non-"pad4"
		("decalatlas",          "generated/decalatlas/*.bimage",         "*.tga"), // This must come after "fullscalebias" and "pad4"
		("discreteAnimation",   "generated/discreteanimation/*.dmodel",  "*.lwo"),
		("file",                "generated/buildgame/*.mapresources",    null),
		//("file",                "generated/swf/*.bimage",                null), // This is both a "file" and an "image"
		("file",                "generated/swf/*.bswf",                  null),
		//("image",               "generated/image/*.bimage",              "*.tga"), // Not always ".tga"...
		("image",               "generated/swf/*.bimage",                null),
		("lightatlas",          "generated/lightatlas/*.bimage",         "*"), // Rarely ".tga"...
		("model",               "generated/model/*.bmodel",              "*.lwo"),
		("renderProg",          "generated/renderprogs/permutations/*_0000000000000001.decl",          "*"),
		("renderProg",          "generated/renderprogs/permutations/*_0000000000000001_pc_vulkan.bin", "*"),
		("renderProg",          "generated/renderprogs/*_pc_vulkan.bin", "*"),
		// The next six lines are annoying, and must be handled manually instead
		//("renderProg",          "generated/spirv/generated/renderprogs/permutations/*_0000000000000001.cspv", "*"),
		//("renderProg",          "generated/spirv/generated/renderprogs/permutations/*_0000000000000001.fspv", "*"),
		//("renderProg",          "generated/spirv/generated/renderprogs/permutations/*_0000000000000001.vspv", "*"),
		//("renderProg",          "generated/spirv/*.cspv",                "*"),
		//("renderProg",          "generated/spirv/*.fspv",                "*"),
		//("renderProg",          "generated/spirv/*.vspv",                "*"),
		("skeleton",            "generated/skeleton/*.bmd6skl",          "*.md6skl"),
		// Some "staticParticleModel"s remove some, but not all, slashes...
		("staticParticleModel", "generated/models/*.pmodel",             "models/*.lwo"),
		("transsortatlas",      "generated/transsortatlas/*.bimage",     "*"),
	];

	// All types used by resources in "maps"
	static readonly NonDecl[] mapTypes = [
		("aas",        "maps/*.aas_botplayer",                   null), // Not used in the vanilla game
		("aas",        "maps/*.aas_monster16",                   null), // Not used in the vanilla game
		("aas",        "maps/*.aas_monster24",                   null), // Not used in the vanilla game
		("aas",        "maps/*.aas_monster48",                   null), // Not used in the vanilla game
		("aas",        "maps/*.aas_monster96",                   null), // Not used in the vanilla game
		("aas",        "maps/*.aas_monster128",                  null),
		("aas",        "maps/*.aas_monster256",                  null),
		("aas",        "maps/*.aas_player",                      null), // Not used in the vanilla game
		("cm",         "maps/*.bcm",                             null),
		("file",       "maps/*.ambientsh",                       null),
		("file",       "maps/*.entities",                        null),
		("file",       "maps/*.flight",                          null),
		("file",       "maps/*.pvs",                             null),
		("file",       "maps/*.sbsp",                            null),
		("file",       "maps/*/devmapinfo.json",                 null),
		("file",       "maps/*/_combo/*.proc",                   null),
		("file",       "maps/*/_combo/_world.shadows",           null),
		("file",       "maps/*/_combo/_world.tome",              null),
		("file",       "maps/*/lightprobes/*_compressed.bimage", null),
		("material",   "maps/*/mega.decl",                       null),
		("material",   "maps/*/megafoliagetrans.decl",           null),
		("material",   "maps/*/megatrans.decl",                  null),
		("model",      "maps/*.bmodel",                          null),
		("snapModule", "maps/modules/*.decl",                    null), // Only used in SnapMap
	];

	// All remaining non-empty types
	static readonly NonDecl[] otherTypes = [
		("anim",       "cooked/anim/*.bmd6anim",                      "*.md6anim"),
		("fga",        "fga/*.fga",                                   null),
		("file",       "snapmap_offline/maps/items/*.decl",           null), // Only used in SnapMap
		("file",       "snapmap_offline/maps/items/*.json",           null), // Only used in SnapMap
		("file",       "submission/orbis/save_data_icon.png",         null),
		("file",       "submission/orbis/save_data_icon_profile.png", null),
		("file",       "title_storage.json",                          null),
		("file",       "vulkan_pipelines.bin",                        null),
		// Most "font"s replace underscores with spaces, but some don't...
		("font",       "fonts/*/64_df.dat",                           "*"),
		("image",      "env/a1l3/start_prefiltered.bimage",           null),
		("image",      "env/default_hdr_prefiltered.bimage",          null),
		("image",      "textures/*.png",                              null),
		("json",       "snapmap_offline/broadcasts/*.json",           null), // Only used in SnapMap
		("json",       "snapmap_offline/maps/queries/*.json",         null), // Only used in SnapMap
		("md6rig",     "md6/*.md6rig",                                "*.md6rig"),
		("model",      "cooked/model/*.bmodel",                       "*.lwo"),
		("renderProg", "decls/renderprogs/*.inc",                     "*"), // One path maps to multiple names...
	];

	// For completeness' sake, all types used by empty resources
	/*static readonly string[] emptyTypes = [
		"aas",
		"aiBehaviorVo",
		"aimAssist", // Only empty in SnapMap
		"ammo",
		"anim",
		"articulatedFigure",
		"cm",
		"commodities", // Only empty in DOOM VFR
		"damage",
		"decalatlas",
		"entityDef",
		"file",
		"flare",
		"fx",
		"gameEventCallout",
		"gameMode",
		"image",
		"interaction",
		"inventoryItem",
		"layer",
		"mapInfo",
		"material",
		"md6Def",
		"model",
		"objective",
		"particle",
		"perks",
		"propComponent", // Only used in SnapMap, and exclusively empty!
		"propThink", // Exclusively empty!
		"renderParm",
		"skeleton",
		"snapAIEncounter",
		"snapEditorEntityDef", // Only empty in SnapMap
		"snapPalette", // Only empty in SnapMap
		"snapPropertyInspector_EmissiveColor", // Only used in SnapMap, and exclusively empty!
		"table",
		"tooltip",
		"unlockable",
		"unlockInventory",
	];*/

	static string previousDeclType = ""; // Store the found camelCase type across method calls for early returns



	// Returns the appropriate type and short name for a given resource path, or null if unrecognised
	// E.g. "generated/decls/entitydef/player.decl" will return ("entityDef", "player")
	// The short name doesn't match all vanilla resources, due to impossible-to-determine substitutions
	public static (string Type, string ShortName)? GetTypeAndShortName(string path)
	{
		// Decls - Check the third directory for the type, and everything after (except ".decl") for the short name
		if (path.StartsWithOrdinal("generated/decls/"))
		{
			int temp = path.IndexOf('/', "generated/decls/".Length);
			if (temp == -1 || Path.GetExtension(path) != ".decl")
				return null; // The file is directly in "generated/decls/", or is not a .decl file

			string type = path["generated/decls/".Length .. temp];
			string name = path[(temp+1) .. ^".decl".Length]; // "+1" to move past the "/"

			if (!type.EqualsOrdinalIgnoreCase(previousDeclType))
			{
				temp = Array.BinarySearch(declTypes, type, StringComparer.OrdinalIgnoreCase);
				if (temp <= -1)
					return null; // Unrecognised decl directory/type

				previousDeclType = declTypes[temp];
			}

			return (previousDeclType, name);
		}

		// Non-decls - Match a wildcard filter to find the type, and substitute the wildcard to find the short name
		NonDecl[] nonDeclTypes;
		if (path.StartsWithOrdinal("generated/"))
			nonDeclTypes = generatedTypes;
		else if (path.StartsWithOrdinal("maps/"))
			nonDeclTypes = mapTypes;
		else
			nonDeclTypes = otherTypes;

		foreach (NonDecl x in nonDeclTypes)
		{
			if (FileSystemName.MatchesSimpleExpression(x.Filter, path, ignoreCase: false))
			{
				if (x.ShortName is null) // If the short name is equal to the full path, just return the full path
					return (x.Type, path);

				// Otherwise, substitute the the short name's "*" with the full path's wildcarded text
				int index = x.Filter.IndexOfOrdinal('*');
				string name = x.ShortName.ReplaceOrdinal("*", path[index .. ^(x.Filter.Length - (index+1))]);
				return (x.Type, name);
			}
		}

		if (path.StartsWithOrdinal("generated/image/")) // "image"s could be wildcarded, but it's better this way
		{
			if (!path.EndsWithOrdinal(".bimage"))
				return null;

			// If a path contains ".tga" or "$", it's never suffixed with another ".tga"
			// But annoyingly, if it doesn't, there's little rhyme or reason to whether it gets suffixed
			// "generated/image/textures/snapedit/grid.bimage" is even suffixed by ".png" instead of ".tga"!
			// The best that we can do is simply to always append ".tga" if it's not already in the path
			string name = path["generated/image/".Length .. ^".bimage".Length];
			if (!name.ContainsOrdinal(".tga") && !name.ContainsOrdinal('$'))
				name = $"{name}.tga";
			return ("image", name);
		}

		if (path.StartsWithOrdinal("generated/spirv/")) // These "renderProgs" don't play nice with wildcards
		{
			if (Path.GetExtension(path) is not (".cspv" or ".fspv" or ".vspv"))
				return null;

			// For "generated/spirv/*.cspv", slashes in the full path are only sometimes skipped (so we don't skip them)
			string name;
			if (!path.StartsWithOrdinal("generated/spirv/generated/renderprogs/permutations/"))
			{
				name = path["generated/spirv/".Length .. ^".cspv".Length];
				return ("renderProg", name);
			}

			// For "generated/spirv/generated/renderprogs/permutations/*_0000000000000001.cspv",
			// slashes in the full path's number are always skipped in the short name (so we skip them too),
			// while slashes before the number are only sometimes skipped (so we don't skip them)
			int suffixLength = "_0000000000000001.cspv".Length;
			if (path.IndexOf('/', path.Length - suffixLength) != -1)
				suffixLength += 1; // There's a slash in the number
			if (path[^suffixLength .. ^".cspv".Length].ReplaceOrdinal("/", "") != "_0000000000000001")
				return null; // If it's not the expected number (with up to one slash in it), error

			name = path["generated/spirv/generated/renderprogs/permutations/".Length .. ^suffixLength];
			return ("renderProg", name);
		}

		// We didn't match a known resource type
		// Falling back to the "file" type is possible, but it's better to error about typos instead
		return null;
	}
}
