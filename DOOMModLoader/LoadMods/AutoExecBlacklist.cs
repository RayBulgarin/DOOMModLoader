using DOOMModLoader.Shared;
using System;
using System.Text;

// Makes sure that a console string doesn't use any blacklisted commands/variables

namespace DOOMModLoader.LoadMods;
static class AutoExecBlacklist
{
	static readonly string[] blacklist = [ // Must be pre-sorted by "StringComparer.OrdinalIgnoreCase"
		"activateCheckPoint",
		"activateConsole",
		"addClamp", // Bypasses the blacklist
		"addWrap", // Bypasses the blacklist
		"aiCanOnlyBeGloryKilled", // Archived?
		"ai_buildFlightMap",
		"ai_ScriptCmd",
		"ai_ScriptCmdEnt",
		"amqp_heartbeatDelay", // Read-only
		"amqp_maxChannels", // Read-only
		"amqp_maxFrameSize", // Read-only
		"bcm",
		"bind", // Archived
		"bindset",
		"BuildInfoToClipBoard",
		"buildSoundAmplitudeTable",
		"build_binaryCorrelation", // Read-only
		"build_binaryRelevantChangelists", // Read-only
		"build_binaryVersion", // Read-only
		"campaign_AllowMenuShellActivation", // Messes with player expectations
		"clear",
		"clearHistory", // Archived
		"ClearHydraProfile", // Archived?
		"cmap",
		"compressFile",
		"com_BethesdaMarketingEmailURI",
		"com_breakableDialogBoxOnPCFatalError", // Initialisation only
		"com_captureFrames",
		"com_capturePath",
		"com_captureSounds",
		"com_captureTGA",
		"com_cleanGeneratedFolder",
		"com_cookedReloadWritableOnly",
		"com_displayReferenceStrings", // Read-only
		"com_exitAfterTests", // Read-only
		"com_exitProcessOnError",
		"com_gameDLLPath",
		"com_gameMode",
		"com_gameModuleRestarts",
		"com_hidePrintWarnings", // Messes with player expectations
		"com_loadlooseLanguages",
		"com_memStampPrints", // Messes with player expectations
		"com_pid", // Initialisation only
		"com_preloadSnapEdit",
		"com_printFilter", // Messes with player expectations
		"com_printWarning", // Archived, messes with player expectations
		"com_prod_regression",
		"com_removeVmtrOnLoad",
		"com_restarted",
		"com_safemode",
		"com_screenshotMode",
		"com_sendSnapshots",
		"com_showCameraPosition", // Archived
		"com_showConsumerPerfMetrics", // Archived
		"com_showFPS", // Archived
		"com_showMemoryUsage", // Archived
		"com_showReminders", // Archived
		"com_skipIntroVideo", // Doesn't work when set this way
		"com_sleepGame",
		"com_sourceControl",
		"com_statsFile",
		"com_structuredLogFileName", // Initialisation only
		"com_superScriptDLLPath",
		"com_timeStampPrints", // Messes with player expectations
		"com_useCookedAssets",
		"com_useCookedMaterials",
		"com_useCookedMD6Anims",
		"com_useCookedModels",
		"com_useCookedPreCompressedBvimages",
		"com_useEntitiesFiles",
		"com_warningSeverityFilter", // Archived, messes with player expectations
		"conDump",
		"configVersion",
		"con_fontSize", // Archived, messes with player expectations
		"copy",
		"crash",
		"cvarAdd", // Bypasses the blacklist
		"cvarMultiply", // Bypasses the blacklist
		"cvarRandom", // Bypasses the blacklist
		"cvarsModified", // Messes with player expectations
		"cvar_restart",
		"debug_print_game_settings", // Messes with player expectations
		"decompressFile",
		"deleteGenerated",
		"devmap",
		"devMode_enable",
		"devMode_fatalErrorOnEnter",
		"devMode_skipCorruptedSaves",
		"disconnect",
		"dumpWarnings",
		"envshot",
		"error", // Closes the game
		"exec", // Bypasses the blacklist
		"exit",
		"exportCollisionModel",
		"exportDuplicateCollisionGeometryModel",
		"fastmap",
		"find", // Useless for mods
		"forge_currentNetwork", // Read-only
		"freeze",
		"fs_arbitraryZipSupport",
		"fs_atomicFileWrite",
		"fs_basepath", // Initialisation only
		"fs_benchmarkSeekMicroseconds",
		"fs_cachepath", // Initialisation only
		"fs_caseSensitiveFS",
		"fs_copyfiles",
		"fs_debug",
		"fs_generatedPath", // Initialisation only
		"fs_installpath", // Initialisation only
		"fs_mtpWholeReadThreshold",
		"fs_nfsRetries",
		"fs_nfsRetryWait",
		"fs_noCheckout",
		"fs_noOverlappedIO",
		"fs_OnDemandZipReads",
		"fs_pathDeclOverride", // Initialisation only
		"fs_readOnly",
		"fs_reportReads",
		"fs_savepath", // Initialisation only
		"fs_shareRetry",
		"fs_sourceControlEnable",
		"fs_sourceControlGetWholeFolders",
		"fs_sourceControlWorkspace",
		"fs_sourcepath", // Initialisation only
		"gamedate", // Read-only
		"gameError",
		"gameMode_defaultComponent",
		"gc_doubleHelixGroundLerpStyle", // Initialisation only
		"gotoShellScreen",
		"g_disasm",
		"g_gameDifficulty",
		"g_minLoadMapTimeMs", // Messes with player expectations
		"g_runFrames", // Messes with player expectations
		"g_setting_aim_assist", // Archived
		"g_setting_boss_health", // Archived
		"g_setting_combatScoring", // Archived
		"g_setting_compass", // Archived
		"g_setting_gk_highlight", // Archived
		"g_setting_hudNotifications", // Archived
		"g_setting_hud_show", // Archived
		"g_setting_interact_prompt", // Archived
		"g_setting_mp_challenge", // Archived
		"g_setting_mp_rating", // Archived
		"g_setting_mp_score", // Archived
		"g_setting_objectiveMarkers", // Archived
		"g_setting_objectiveUpdate", // Archived
		"g_setting_photomode", // Archived
		"g_setting_razerChroma", // Archived
		"g_setting_subtitles", // Archived
		"g_setting_tutorials", // Archived
		"g_showChallenges", // Archived?
		"g_sleep",
		"g_timer", // Useless for mods, messes with player expectations
		"history", // Useless for mods
		"hitch",
		"hydra_overrideHydraAPIKey", // Initialisation only
		"hydra_overrideHydraURL", // Initialisation only
		"image_screenshotQuality",
		"in_anglespeedkey", // Archived
		"in_joystick", // Archived
		"in_joystickRumble", // Archived
		"in_mouse", // Archived
		"in_noFocusJoystickInput", // Initialisation only
		"in_pitchspeed", // Archived
		"in_requireGameWindowActive", // Archived
		"in_unlockMouseInMenus", // Archived
		"in_yawspeed", // Archived
		"joy_circleToSquare", // Archived
		"joy_circleToSquarePower", // Archived
		"joy_deadZone", // Archived
		"joy_gammaLook", // Archived
		"joy_mergedDeadZoneAngle", // Archived
		"joy_mergedThreshold", // Archived
		"joy_pitchSpeed", // Archived
		"joy_powerScale", // Archived
		"joy_range", // Archived
		"joy_triggerThreshold", // Archived
		"joy_yawSpeed", // Archived
		"key_deviceBindOverride", // Messes with player expectations
		"launcher_launcherId",
		"launcher_launchId",
		"leaveGame",
		"listBinds", // Useless for mods
		"listCmds", // Useless for mods
		"listCollisionModels", // Useless for mods
		"listCvars", // Useless for mods
		"listDecls", // Useless for mods
		"listImages", // Useless for mods
		"listInventory", // Useless for mods
		"listLines", // Useless for mods
		"listModels", // Useless for mods
		"listspheres", // Useless for mods
		"listVirtualTextures", // Useless for mods
		"loadDevMenuOption",
		"loadGame",
		"loadGameIntoMap",
		"LoadPreviousMapFromSaveGame",
		"logFile",
		"logFileName",
		"logFilePathType",
		"logFilePersistent",
		"loosemap",
		"makeDeclTree",
		"map",
		"mem_phyMemBlockSizeM", // Initialisation only
		"mem_UseTwoDiffusePools", // Initialisation only
		"menu_enableBrightnessCalibration",
		"menu_liveTileLocalPathOverride",
		"mm_savedDevMenuDecl",
		"mm_savedDevMenuIndex",
		"mm_savedDevMenuLaunchType",
		"mpServerCloudClient_id", // Initialisation only
		"mpServerCloudInstance_exit",
		"mpServerCloudInstance_exitCode",
		"mpServerCloudInstance_id", // Initialisation only
		"mpServerCloudInstance_idleMaintenanceExitCode",
		"mpServerCloudInstance_idleMaintenanceFile",
		"mpServerCloudInstance_idleMaintenanceFileCheckFrequency",
		"mpServerCloudInstance_idleTimeout",
		"mpServerCloudInstance_idleTimeoutExitCode",
		"mpServerCloudInstance_reserveTimeout",
		"mpServerCloudInstance_setupTimeout",
		"mpServerCloudInstance_splunkHeartbeat",
		"mt_genPageCompression",
		"mt_genVmtrCompression",
		"mt_sizeOverride",
		"m_pitch", // Archived
		"m_smooth", // Archived
		"m_yaw", // Archived
		"net_bw_test_host_timeout_ms",
		"net_bw_test_interval_ms",
		"net_bw_test_numPackets",
		"net_bw_test_packetSizeBytes",
		"net_bw_test_throttle_byte_pct",
		"net_bw_test_throttle_rate_pct",
		"net_bw_test_throttle_seq_pct",
		"net_bw_test_timeout_ms",
		"net_fakeSearchDelay", // Messes with player expectations
		"net_ForceWriteSnap",
		"net_ForceWriteSnapAckMS",
		"net_ForceWriteSnapAckSEQ",
		"net_http_dump_requests",
		"net_maxLocalUsers",
		"net_MPCloudGobblerAPIKey", // Initialisation only
		"net_MPCloudGobblerEndpoint", // Initialisation only
		"net_overrideProfanityFilterAPIKey", // Initialisation only
		"net_overrideProfanityFilterURL", // Initialisation only
		"net_versionChecksum", // Initialisation only
		"net_voice",
		"net_voiceDevice", // Archived
		"net_voicePushToTalk",
		"net_voiceTalkTimeMS",
		"net_voiceTrackMicStatus",
		"net_voiceVolume",
		"net_voice_throttleStart",
		"nextmap",
		"parse", // Useless for mods
		"path", // Useless for mods
		"p_ForceFov", // Messes with player expectations
		"quit",
		"RandomizePlayerCustomization", // Archived?
		"reexportDecls",
		"reexportEntityDefs",
		"reg_excludeList",
		"reg_includeList",
		"reloadDecls",
		"reloadEntity",
		"reloadLanguage",
		"reloadMD6Models",
		"reloadWater",
		"reset", // Bypasses the blacklist
		"ResetProfileControllerConfig", // Archived
		"resourceExec", // Bypasses the blacklist
		"resource_bootVideo",
		"resource_dontPreloadBCMForSnap", // Read-only
		"resource_errorInGame",
		"resource_errorOnResolveFailure",
		"resource_loadLooseAssets",
		"resource_outPath",
		"resource_progressVideo",
		"resource_restartVideo",
		"resource_snap", // Read-only
		"resource_updatePakCacheTable",
		"resource_writePreloadModels",
		"RestartMap",
		"RestartMapAsIs",
		"RestartMapAtCheckPoint",
		"RestartMapFromLvlBackup",
		"RestartMapFromMemoryCheckpoint",
		"RestartMapFromSnapEditor",
		"RestartMapFromStart",
		"RestartMapHere",
		"restartmapwithlobby",
		"rs_debug", // Messes with player expectations
		"rs_debugView", // Messes with player expectations
		"rs_enable",
		"runEndOfGameCredits",
		"r_apiDump", // Useless for mods
		"r_debugContext", // Initialisation only
		"r_dumpGeneratedGLSL",
		"r_enableAmdShaderExtensions", // Initialisation only
		"r_enableAsyncCompute", // Initialisation only
		"r_enableNVDedicatedAllocation", // Initialisation only
		"r_feedbackBGRA", // Initialisation only
		"r_fencePoolSize", // Initialisation only
		"r_forceFullVirtualTextureLoad",
		"r_fullscreen", // Archived, local setting
		"r_generatedRenderprogPath",
		"r_glMajorVersion", // Initialisation only
		"r_glMinorVersion", // Initialisation only
		"r_glUseBufferStorage", // Initialisation only
		"r_gpuParticleLargeEmitterParticleCount", // Initialisation only
		"r_gpuParticleLargeEmitterSystemCount", // Initialisation only
		"r_gpuParticleMediumEmitterParticleCount", // Initialisation only
		"r_gpuParticleMediumEmitterSystemCount", // Initialisation only
		"r_gpuParticleSmallEmitterParticleCount", // Initialisation only
		"r_gpuParticleSmallEmitterSystemCount", // Initialisation only
		"r_initialModeHeight", // Archived, local setting
		"r_initialModeWidth", // Archived, local setting
		"r_initialMonitor", // Archived, local setting
		"r_loadPrecompiledShaders",
		"r_logFile",
		"r_lowGPUMemThreshold", // Initialisation only
		"r_mode", // Archived, local setting
		"r_multiSamples", // Archived, local setting
		"r_perfSaveFile",
		"r_perfSaveStatsToFile",
		"r_physicalPagesAspectRatio", // Archived, local setting
		"r_rebuildRenderprogs",
		"r_renderAPI", // Local setting
		"r_shadowAtlasHeight", // Local setting
		"r_shadowAtlasTileSize", // Local setting
		"r_shadowAtlasWidth", // Local setting
		"r_shadowMaxStaleFrames", // Local setting
		"r_shadowsDistanceFadeMultiplier", // Local setting
		"r_showPIDInTitle", // Archived, local setting, messes with software that looks at window titles
		"r_skipWatermark", // Initialisation only
		"r_sleep",
		"r_swapInterval", // Archived
		"r_takingScreenshot", // Read-only
		"r_useGeneratedRenderprogs",
		"r_useGPUTimer", // Archived
		"r_vmtrPhysicalPagesAspectRatio", // Archived, local setting
		"r_windowHeight", // Archived, local setting
		"r_windowPosX", // Archived, local setting
		"r_windowPosY", // Archived, local setting
		"r_windowWidth", // Archived, local setting
		"r_writePackedMaterials",
		"r_zfar", // Read-only
		"saveanddisconnect",
		"saveFSM",
		"saveGame",
		"saveGame_enable",
		"savegame_error",
		"savegame_ignoreUnverified",
		"savegame_minRequiredStorage",
		"savegame_winInduceDelay",
		"saveProgression",
		"saveWeaponFSM",
		"screenshot",
		"shell_resetProfileOnNewGame",
		"slowcombomap",
		"slowmap",
		"slowmapcheckpoint",
		"sm_buildAmbientFast",
		"sm_buildDynamicNavmesh",
		"sm_testIslandModulePvsFix", // Initialisation only
		"sm_testPortalSystem", // Initialisation only
		"snapEdit_consoleRenameMapHack", // Messes with player expectations
		"snapEdit_devMaps", // Read-only
		"snapEdit_dlc1Maps", // Read-only
		"snapEdit_skipRenameOnSave", // Messes with player expectations
		"spectator_changeLoadout",
		"spectator_showHackModules",
		"StartMapWithSaveData",
		"stripbcm",
		"stripStrings",
		"swf_imageCompressor",
		"swf_useSubtitles", // Messes with player expectations
		"sys_cpustring", // Initialisation only
		"sys_installToHDD", // Initialisation only
		"sys_langOverride", // Initialisation only
		"s_buildBankInfos",
		"s_customScheduling", // Initialisation only
		"s_customSchedulingPeriod", // Initialisation only
		"s_enableOrbisFlexibleMemory", // Initialisation only
		"s_forcePreLoadSoundBanks", // Initialisation only
		"s_loadBanks",
		"s_noSound",
		"s_playSoundInBackground",
		"s_reloadAudioEvents",
		"s_restart",
		"s_restartVirtualizedSounds",
		"s_soundCommandQueueSizeMegs", // Archived
		"s_soundHardwareMode",
		"s_streamGranularityKB", // Initialisation only
		"s_unloadBanks",
		"s_updateVoStrings",
		"s_volume_ambient",
		"s_volume_dB",
		"s_volume_mpvo",
		"s_volume_music",
		"s_volume_sfx",
		"s_volume_vo",
		"s_writeEventFile",
		"toggle", // Bypasses the blacklist
		"ToggleMainMenu",
		"touchDecl",
		"tss_enable", // Initialisation only
		"tw_save",
		"unbind", // Archived
		"unbindAll", // Archived
		"updateStringLengthFromGUIs",
		"verifiedExec", // Bypasses the blacklist
		"vid_restart",
		"vo_lipsyncToolPath",
		"vt_benchmark",
		"vt_delayedRestart",
		"vt_dumpHDPData",
		"vt_eliminateSeamsInVmtr",
		"vt_emptyCache",
		"vt_errorOnNoPageFile",
		"vt_filePath",
		"vt_filePathVmtrOverride",
		"vt_lockPages",
		"vt_logPageWrites",
		"vt_pageImageSizeUnique", // Archived, local setting
		"vt_pageImageSizeVmtr", // Archived, local setting
		"vt_productionFilePath",
		"vt_qualityBC6HLightmap",
		"vt_qualityBC7ColorMask",
		"vt_qualityBC7ColorMaskTranscode",
		"vt_qualityHDPDiffuse",
		"vt_qualityHDPLightmap",
		"vt_qualityHDPLossless",
		"vt_qualityHDPNormal",
		"vt_qualityHDPPower",
		"vt_qualityHDPSpecular",
		"vt_qualityLightmapRoundDenominator",
		"vt_reload",
		"vt_reloadMega2Vmtrs",
		"vt_restart",
		"vt_setSource",
		"vt_skipLZDecompress",
		"vt_transcodeBenchmark",
		"vt_uncompressedPhysicalImages", // Archived
		"vt_uncompressedVmtr", // Initialisation only
		"vt_useLightmapScale",
		"vt_useLZ4Compression",
		"vt_useSpecularScale",
		"vt_validateCache",
		"vt_writeDebugImage",
		"vulkan_stagingBufferCount", // Initialisation only
		"vulkan_stagingBufferSizeMB", // Initialisation only
		"vulkan_VRAMAllocatorBlockStepSize", // Initialisation only
		"vulkan_VRAMAllocatorMaxBlockSizeDeviceLocalMB", // Initialisation only
		"vulkan_VRAMAllocatorMaxBlockSizeHostVisibleMB", // Initialisation only
		"vulkan_VRAMAllocatorMinBlockSizeDeviceLocalMB", // Initialisation only
		"vulkan_VRAMAllocatorMinBlockSizeHostVisibleMB", // Initialisation only
		"wait",
		"win_allowMultipleInstances",
		"win_consoleVisibility",
		"win_crashDmp_enable",
		"win_crashDmp_path",
		"win_defaultGamerTagToUsername",
		"win_floatExceptions",
		"win_notaskkeys",
		"win_silentCrash",
		"win_spinOnCrash",
		"win_terminateOnCrash",
		"win_viewlog",
		"writeConfig",
		"writeDebugBinaryVirtualImages",
		"writeDebugLightmapImage",
		"writeDebugVmtrImage",
		"writeEntitiesFile",
		"writeEntitiesFileWithError",
		"writeImage",
	];



	// Returns an empty string if it's fine, or the first blacklisted command/variable encountered
	public static string ValidateAutoExec(ReadOnlySpan<byte> bytes)
	{
		string word;
		for (int i = 0; i < bytes.Length; i++)
		{
			if ((char)bytes[i] is '\n' or '\r' or '\t' or ' ') // Skip white space
				continue;

			if (bytes[i] == '"') // A quoted command/variable name
			{
				i++;
				int start = i;
				int index = MemoryExtensions.IndexOf(bytes[i .. ^0], (byte)'"');

				if (index == -1)
					goto Failure;

				i += index;
				word = Encoding.UTF8.GetString(bytes[start .. i]);
				i++; // Move past the quote
			}
			else if ((char)bytes[i] is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_')
			{
				// Unquoted words may contain a-z, A-Z, 0-9, and "_"
				int start = i;
				for (i++; i < bytes.Length; i++)
				{
					if ((char)bytes[i] is not ((>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_'))
						break;
				}
				word = Encoding.UTF8.GetString(bytes[start .. i]);
			}
			else // Unexpected character. Return the first UTF8 character
				return Encoding.UTF8.GetString(bytes[i .. ^0])[0].ToString();

			// Check if the command/variable is blacklisted
			if (Array.BinarySearch(blacklist, word, StringComparer.OrdinalIgnoreCase) >= 0)
				return word;

			// Find the next command/variable
			while (true)
			{
				int index = MemoryExtensions.IndexOfAny(bytes[i .. ^0], (byte)'"', (byte)';');
				if (index == -1)
					return ""; // No more commands/variables

				i += index;
				if (bytes[i] == ';')
					break; // Check the next command/variable

				i++; // Quoted words; move past the opening quote, and find the closing quote
				index = MemoryExtensions.IndexOf(bytes[i .. ^0], (byte)'"');
				if (index == -1)
					goto Failure;
				i += (index + 1); // "+1" to move past the quote, because this is a "while" loop
			}
		}

		return "";

Failure:
		Console.WriteLine();
		Prompts.WriteError("ERROR: Failed to install mods!");
		Prompts.WriteWarning("\"mod.decl\" > \"autoExec\": Unclosed inner quote (\\\")");
		Prompts.ExitPrompt();
		return "";
	}
}
