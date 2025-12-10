using DOOMModLoader.Shared;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

// Checks for updates with GitHub's REST API

namespace DOOMModLoader.LoadMods;
static class UpdateCheck
{
	const int maxBufferSize = 1024*1024; // Limit to 1 megabyte; we only expect around 10 kilobytes
	const int timeout = 8; // Wait for up to 8 seconds
	const string apiUrl = "https://api.github.com/repos/ZwipZwapZapony/DOOMModLoader/releases/latest";
	const string releaseUrl = "https://github.com/ZwipZwapZapony/DOOMModLoader/releases/latest";

	static bool stopWaitingMessage = false;



	// Asks whether or not to check for updates, if the user hasn't previously chosen
	public static void AskToCheck()
	{
		if (Config.Final.CheckForUpdates is not null)
			return; // Don't ask if it's already set

		Console.Write(
			""
			+ "\nDo you want to check for updates before installing mods?"
			+ "\nThis can be changed later by editing \"DOOMModLoaderSettings.txt\""
			+ "\n(No personal information will be collected; this only retrieves data from GitHub's API)"
			+ "\n"
			+ "\n(Press [Y] to check for updates every 30 days)"
			+ "\n(Press [N] to skip checking for updates)"
		);
		bool? choice = Prompts.GetYesOrNo();

		if (choice is null)
			Prompts.WriteWarning("Warning: Failed to detect keystroke");
		else if (choice == true)
		{
			Config.File.CheckForUpdates = 30;
			Config.ShouldSave = true;
		}
		else
		{
			// Most users should want to check for updates, so ensure that they really meant to disable it
			Console.WriteLine();
			Prompts.WriteWarning("Please confirm your choice:");
			Console.Write(
				""
				+ "\n(Press [Y] to check for updates - Recommended)"
				+ "\n(Press [N] again to skip checking for updates)"
			);
			Thread.Sleep(500); // Sleep longer than usual before accepting input
			choice = Prompts.GetYesOrNo();

			if (choice is null)
				Prompts.WriteWarning("Warning: Failed to detect keystroke");
			else if (choice == true)
			{
				Config.File.CheckForUpdates = 30;
				Config.ShouldSave = true;
			}
			else
			{
				Console.WriteLine("Disabling update checks...");
				Config.File.CheckForUpdates = -1;
				Config.ShouldSave = true;
			}
		}
	}

	// Shows the download URL and asks whether to open it
	// Returns whether the user chose yes or no
	public static void AskToOpenDownloadPage(bool abort)
	{
		Console.Write(
			$"    {releaseUrl}"
			+  "\n"
			+  "\nDo you want to open the download page now?"
			+  "\n"
			+  "\n(Press [Y] to open the download page in a web browser)"
			+ $"\n(Press [N] to {(abort ? "close" : "continue without updating")} DOOMModLoader)"
		);
		bool? choice = Prompts.GetYesOrNo();

		Console.WriteLine();
		if (choice is null)
		{
			Prompts.WriteError("ERROR: Failed to detect keystroke!");
			Prompts.ExitPrompt();
			return;
		}
		else if (choice == false)
			return;

		// Open the download page
		// Don't update "LastUpdateCheck"; check for updates the next time that DOOMModLoader is run
		ProcessStartInfo info = new()
		{
			FileName = releaseUrl,
			UseShellExecute = true,
		};

		try
		{
			Process.Start(info);
			Console.WriteLine("Opened the download page!");
		}
		catch (Win32Exception) // "Win32Exception" is multi-platform, used when the file doesn't exist
			{Prompts.WriteError("ERROR: Failed to open the download page!");}
		Prompts.ExitPrompt(); // Don't exit with 0, as that's reserved for successfully loading mods
		return;
	}

	// Displays a message with periods slowly added over time
	// Set "stopWaitingMessage" to true to stop this
	static void WaitingMessage()
	{
		Console.WriteLine();
		Console.Write("Checking for updates...");
		Task.Run(() => // Write another period each second, asynchronously
		{
			for (int i = 0; i < timeout - 1; i++)
			{
				Thread.Sleep(1000);
				if (stopWaitingMessage)
					break;
				Console.Write('.');
			}
		});
	}

	// Returns the latest GitHub release's tag name, or an empty string on failure
	static string GetLatestVersion()
	{
		// Normally, you should reuse one HttpClient instance throughout your codebase without disposing it,
		// but we only use it a single time throughout the program, so "using" (thus disposing) is fine in this case
		using HttpClientHandler handler = new()
		{
			AutomaticDecompression = DecompressionMethods.All, // Let GitHub send us gzipped data
		};
		using HttpClient client = new(handler);
		client.DefaultRequestHeaders.Clear();
		client.DefaultRequestHeaders.Accept.Add(new("application/vnd.github+json"));
		client.DefaultRequestHeaders.UserAgent.Add(new("DOOMModLoader", Program.VersionString));
		client.MaxResponseContentBufferSize = maxBufferSize;
		client.Timeout = TimeSpan.FromSeconds(timeout);

		try
		{
			using HttpResponseMessage response = client.Get(apiUrl);

			if (Config.Final.Verbose)
			{
				stopWaitingMessage = true;
				Console.WriteLine(); // Not an empty line, just a line break after the waiting message
				Prompts.WriteVerbose($"    Status code: {response.StatusCode:D}");
			}

			if (!response.IsSuccessStatusCode)
				return ""; // Something is wrong on GitHub's end

			// Parse the retrieved JSON data - GitHub serves UTF-8 bytes, no conversion necessary
			using Stream jsonStream = response.Content.ReadAsStream();
			Span<byte> jsonBytes = new byte[jsonStream.CanSeek ? jsonStream.Length : maxBufferSize];
			try
				{jsonStream.ReadExactly(jsonBytes);}
			catch (Exception e) when (e is EndOfStreamException or InvalidDataException)
				{} // Don't abort; attempt to parse the JSON first
			Utf8JsonReader json = new(jsonBytes);

			if (!json.Read() || json.TokenType != JsonTokenType.StartObject) // Validate the JSON file's start
				return ""; // Something is wrong on GitHub's end

			while (json.Read()) // Look for the "tag_name" property
			{
				if (json.TokenType == JsonTokenType.EndObject)
					break; // Didn't find "tag_name"
				else if (json.GetString() == "tag_name")
				{
					if (!json.Read() || json.TokenType != JsonTokenType.String)
						break; // "tag_name" is not a string
					return json.GetString()!;
				}
				else
					json.Skip(); // Skip irrelevant objects/arrays
			}

			return ""; // Something is wrong on GitHub's end
		}
		catch (Exception e) when (e is HttpRequestException or JsonException or OperationCanceledException)
			{return "";} // Something is wrong on the user's or GitHub's end
	}

	// Checks whether the user has the latest version, and prompts them to download the latest version if not
	public static void CheckForUpdates()
	{
		if (Config.Final.CheckForUpdates is null or -1) // If null, something's wrong. If -1, the user disabled this
			return;

		DateOnly today = DateOnly.FromDateTime(DateTime.Today);
		int daysSinceCheck = (today.DayNumber - (Config.File.LastUpdateCheck?.DayNumber ?? 0));
		if (daysSinceCheck >= 0 && daysSinceCheck < Config.Final.CheckForUpdates)
			return;

		WaitingMessage();
		string newVersion = GetLatestVersion();
		stopWaitingMessage = true;

		if (newVersion == $"v{Program.VersionString}") // Up-to-date
		{
			if (Config.Final.Verbose)
				Console.WriteLine("    Already up-to-date!");
			else
				Console.WriteLine(" Already up-to-date!"); // On the same line as the waiting message
			if (Config.File.CheckForUpdates >= 1) // Check "Config.File", not "Config.Final"
			{
				Config.File.LastUpdateCheck = today;
				Config.ShouldSave = true;
			}
		}
		else if (!string.IsNullOrEmpty(newVersion)) // Update available
		{
			if (Config.Final.Verbose)
				Console.WriteLine("    Update available!");
			else
				Console.WriteLine(" Update available!"); // On the same line as the waiting message

			if (newVersion[0] is not ('v' or 'V'))
				newVersion = $"v{newVersion}";
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine($"You currently have v{Program.VersionString} of DOOMModLoader");
			Console.WriteLine($"The latest version is {newVersion}:");
			AskToOpenDownloadPage(false);

			// The user chose not to open the download page
			if (Config.File.CheckForUpdates >= 1) // Check "Config.File", not "Config.Final"
			{
				Config.File.LastUpdateCheck = today;
				Config.ShouldSave = true;
			}
		}
		else // Failed to check
		{
			if (!Config.Final.Verbose)
				Console.WriteLine(); // Not an empty line, just a line break after the waiting message
			Prompts.WriteWarning("    Warning: Failed to check for updates!");
			if (Config.File.CheckForUpdates >= 1) // Check "Config.File", not "Config.Final"
			{
				// Set "LastUpdateCheck" such that it will check for updates tomorrow
				try
					{Config.File.LastUpdateCheck = today.AddDays(1 - Config.File.CheckForUpdates.Value);}
				catch (ArgumentOutOfRangeException)
					{Config.File.LastUpdateCheck = today;}
				Config.ShouldSave = true;
			}
		}
	}
}
