using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;

// "Press a key" prompts and coloured messages

namespace DOOMModLoader.Shared;
static class Prompts
{
	// Don't wait for keystrokes if opened within an existing terminal (batch file, command prompt, et cetera)
	static readonly bool skipPrompts = (Console.IsInputRedirected || (Process.GetCurrentProcess().MainWindowHandle == IntPtr.Zero));
	static bool ctrlCHandlerUsed = false; // Set to true if the custom Ctrl+C handler is executed



	// Waits for any keystroke (with a "to exit" message), unless "skipPrompts" is true, and exits
	[DoesNotReturn]
	public static void ExitPrompt(int exitCode = 1)
	{
		Config.File.Save();
		if (!skipPrompts)
		{
			Console.WriteLine();
			Console.Write("Press any key to exit...");
			WaitForKeystroke();
		}
		Environment.Exit(exitCode);
		return;
	}

	// Waits for 10 seconds, unless "skipPrompts" is true, and exits, unless Ctrl+C is pressed to stop the countdown
	[DoesNotReturn]
	public static void ExitTimer(int exitCode = 1)
	{
		Config.File.Save();
		if (!skipPrompts)
		{
			Console.CancelKeyPress += ExitCtrlCHandler;
			Console.WriteLine();
			Console.Write("Exiting in 10 seconds... (Press [Ctrl+C] to keep this window open...)");
			Thread.Sleep(10000);

			if (ctrlCHandlerUsed) // Using Ctrl+C mid-sleep will return to the post-sleep code when the sleep expires
				Thread.Sleep(Timeout.Infinite); // In that case, sleep again, and let the Ctrl+C handler end the process
		}
		Environment.Exit(exitCode);
		return;

		// Waits for any keystroke and exits, used when pressing Ctrl+C during the exit countdown
		static void ExitCtrlCHandler(object? sender, ConsoleCancelEventArgs e)
		{
			Console.CancelKeyPress -= ExitCtrlCHandler; // Don't allow "stackable" Ctrl+C events
			ctrlCHandlerUsed = true;
			Console.WriteLine(); // Not an empty line, just a line break after the prompt
			Console.WriteLine();
			Console.Write("Ctrl+C was pressed. Press any key to exit...");
			Thread.Sleep(500); // Sleep for half a second before accepting input
			WaitForKeystroke();
			Environment.Exit(exitCode: 0);
			return;
		}
	}

	// Returns true if Y was pressed, false if N was pressed, or null on failure, regardless of "skipPrompts"
	public static bool? GetYesOrNo()
	{
		if (Console.IsInputRedirected)
			return null;

		Thread.Sleep(500); // Sleep for half a second, to mitigate skipping past a question without reading it

		while (true)
		{
			try
			{
				while (Console.KeyAvailable)
					Console.ReadKey(true); // Skip queued keystrokes, in every loop
			}
			catch (IOException)
			{
				Console.WriteLine(); // Not an empty line, just a line break after the prompt
				return null; // This is the best that we can do here
			}

			ConsoleKey key = Console.ReadKey(true).Key;

			if (key == ConsoleKey.Y)
			{
				Console.WriteLine("Y");
				return true;
			}
			else if (key == ConsoleKey.N)
			{
				Console.WriteLine("N");
				return false;
			}
			else if (key is (>= ConsoleKey.A and <= ConsoleKey.Z) // Beep at unrecognised letters and numbers only
			or (>= ConsoleKey.D0 and <= ConsoleKey.D9))
				Console.Beep(); // Todo: Beep asynchronously?
		}
	}

	// Waits for a "reasonable" keystroke - Console.ReadKey alone is too generous, triggering even for the Windows key
	static void WaitForKeystroke()
	{
		if (skipPrompts)
			return;

		try
		{
			while (Console.KeyAvailable)
				Console.ReadKey(true); // Skip queued keystrokes
		}
		catch (IOException)
			{return;} // This is the best that we can do here

		while (true)
		{
			ConsoleKey key = Console.ReadKey(true).Key;

			if (key is (>= ConsoleKey.A and <= ConsoleKey.Z)
			or (>= ConsoleKey.D0 and <= ConsoleKey.D9)
			or (>= ConsoleKey.F1 and <= ConsoleKey.F24)
			or (>= ConsoleKey.NumPad0 and <= ConsoleKey.NumPad9)
			or (>= ConsoleKey.Backspace and <= ConsoleKey.Help)
			or (>= ConsoleKey.Multiply and <= ConsoleKey.Divide)
			or (>= ConsoleKey.Oem1 and <= ConsoleKey.Oem102)) // This doesn't match OemClear
				break;
		}
	}

	// Displays an error with red and yellow text
	public static void WriteError(string? text = null)
	{
		Console.BackgroundColor = ConsoleColor.DarkRed;
		Console.ForegroundColor = ConsoleColor.Yellow;

		if (!string.IsNullOrEmpty(text))
		{
			Console.Write(text); // Not "WriteLine"; we want to reset the colour before the newline
			Console.ResetColor();
			Console.WriteLine();
		}
	}

	// Displays a message with green and white text
	public static void WriteSuccess(string? text = null)
	{
		Console.BackgroundColor = ConsoleColor.DarkGreen;
		Console.ForegroundColor = ConsoleColor.White;

		if (!string.IsNullOrEmpty(text))
		{
			Console.Write(text); // Not "WriteLine"; we want to reset the colour before the newline
			Console.ResetColor();
			Console.WriteLine();
		}
	}

	// Displays a message with cyan text, if verbosity is enabled in the settings file or on the command-line
	public static void WriteVerbose(string? text = null)
	{
		if (!Config.Final.Verbose)
			return;

		if (Console.BackgroundColor != ConsoleColor.Cyan) // This check isn't reliable on Linux, sadly
			Console.ForegroundColor = ConsoleColor.Cyan;

		if (!string.IsNullOrEmpty(text))
		{
			Console.Write(text); // Not "WriteLine"; we want to reset the colour before the newline
			Console.ResetColor();
			Console.WriteLine();
		}
	}

	// Displays a warning with yellow text
	public static void WriteWarning(string? text = null)
	{
		if (Console.BackgroundColor != ConsoleColor.Yellow) // This check isn't reliable on Linux, sadly
			Console.ForegroundColor = ConsoleColor.Yellow;

		if (!string.IsNullOrEmpty(text))
		{
			Console.Write(text); // Not "WriteLine"; we want to reset the colour before the newline
			Console.ResetColor();
			Console.WriteLine();
		}
	}
}
