// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using ElementObserver;
using ElementObserver.Models;

Console.WriteLine("Start Application!");
SetupCurrentDirectory();


Console.WriteLine("Trying find GameInfo data!");
var gameInfo = GetOrCreateGameInfo();

Console.WriteLine("Launch Game instances");
var cts = new CancellationTokenSource();
Task.Factory.StartNew(() => { RunAndWatch(gameInfo, cts); }, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

Console.WriteLine("Wait Any Key for Stop");
Console.ReadLine();
cts.Cancel();


void SetupCurrentDirectory()
{
	Console.WriteLine("Trying setup Working Directory");
	var fileInfo = new FileInfo(Environment.GetCommandLineArgs().First());
	if (fileInfo.Exists == false || fileInfo.Directory == null)
	{
		Console.WriteLine("Не Знаю как ты это сломал. Но подумай еще раз.");
		throw new FileNotFoundException();
	}

	Environment.CurrentDirectory = fileInfo.Directory.FullName;
	Console.WriteLine($"Working Directory : {Environment.CurrentDirectory}");
}

GameInfo GetOrCreateGameInfo()
{
	var workDir = new DirectoryInfo(Environment.CurrentDirectory);
	var files = workDir.GetFiles("*.json");

	GameInfo gameInfo = null;
	foreach (var file in files)
		if (GameInfo.TryGetGameInfo(file, out gameInfo))
			break;

	if (gameInfo != null)
	{
		Console.WriteLine($"Find {gameInfo.FilePath}");
		return gameInfo;
	}

	Console.WriteLine("Failed find GameInfo Data.");
	Console.WriteLine("Start setup GameInfo in Working Dir.");

	gameInfo = new GameInfo
	{
		FilePath = Path.Combine(Environment.CurrentDirectory, "GameInfo.json")
	};

	FindGameFolder(gameInfo);
	AddCharactersData(gameInfo);
	return gameInfo;
}

void FindGameFolder(in GameInfo gameInfo)
{
	do
	{
		Console.WriteLine(@"Enter Game Folder path. Example D:\Game\JD3Classic ");
		Console.WriteLine("Game Folder Path:");
		var gameFolderPath = Console.ReadLine();
		if (string.IsNullOrEmpty(gameFolderPath))
		{
			Console.WriteLine("Try again!");
			continue;
		}

		var gameFolder = new DirectoryInfo(gameFolderPath);
		if (gameFolder.Exists)
		{
			Console.WriteLine("Try find elementclient.exe");
			var clientFiles = gameFolder.GetFiles("*elementclient.exe", SearchOption.AllDirectories);
			foreach (var clientFile in clientFiles)
			{
				gameInfo.ElementExePath = clientFile.FullName;
				Console.WriteLine($"Find {clientFile.FullName}");
				return;
			}
		}

		Console.WriteLine("Try again!");
	} while (true);
}

void AddCharactersData(in GameInfo gameInfo)
{
	var character = EnterCharacterData();
	gameInfo.Characters.Add(character);
	gameInfo.Save();

	Console.WriteLine("Need add one more account?");
	Console.WriteLine("1. Yes");
	Console.WriteLine("2. No");

	var needAdd = true;
	do
	{
		var key = Console.ReadKey();
		needAdd = key.Key is ConsoleKey.D1 or ConsoleKey.NumPad1 or ConsoleKey.Y;
		if (needAdd)
		{
			character = EnterCharacterData();
			gameInfo.Characters.Add(character);
			gameInfo.Save();
		}
	} while (needAdd);

	string GetEnteredText(bool needCheckNull, string? description)
	{
		string? enterText = null;
		do
		{
			if (description != null)
				Console.WriteLine(description);
			enterText = Console.ReadLine();
			if (needCheckNull == false) return enterText;
		} while (string.IsNullOrEmpty(enterText));

		return enterText;
	}

	CharacterData EnterCharacterData()
	{
		Console.WriteLine("Enter Character Data.");

		var characterData = new CharacterData
		{
			Login = GetEnteredText(true, "Enter Login:"),
			Password = GetEnteredText(true, "Enter Password:"),
			Name = GetEnteredText(false, "Enter CharacterName (optional):")
		};
		return characterData;
	}
}

bool TryFindExistProc(GameInfo gameData, out Process foundedProcess)
{
	foundedProcess = default;
	var file = new FileInfo(gameData.ElementExePath);
	var processes = Process.GetProcesses();
	foreach (var process in processes)
		try
		{
			if (file.Name.Contains(process.ProcessName, StringComparison.OrdinalIgnoreCase) == false)
				continue;

			Console.WriteLine($"Found element client process {process.ProcessName}:{process.Id}");
			var cmd = process.GetCommandLineArgs();
			var args = cmd.Split(' ');
			var loginNameData = (from s in args where s.Contains("user") select s.Split(':')).FirstOrDefault();
			if (loginNameData == null || loginNameData.Length < 2)
				continue;

			var logginName = loginNameData[1];
			var existProcess = gameData.Characters.FirstOrDefault(cd => cd.Login.Contains(logginName, StringComparison.OrdinalIgnoreCase));
			if (existProcess != null)
			{
				Console.WriteLine($"Attach to {process.ProcessName}:{process.Id}");
				foundedProcess = process;
				return true;
			}
		}
		catch (Exception e)
		{
		}

	return false;
}

void RunAndWatch(GameInfo gameData, CancellationTokenSource cancellationTokenSource)
{
	var tasks = new Task[gameData.Characters.Count];
	for (var i = 0; i < gameData.Characters.Count; i++)
	{
		var characterData = gameData.Characters[i];
		tasks[i] = new Task(async () => await WatchForClient(characterData), cancellationTokenSource.Token);
	}

	foreach (var task in tasks)
	{
		task.Start();
	}

	do
	{
		var failedTask = Task.WaitAny(tasks, cancellationTokenSource.Token);
		tasks[failedTask] = Task.Run(async () => await WatchForClient(gameData.Characters[failedTask]), cancellationTokenSource.Token);
	} while (cancellationTokenSource.IsCancellationRequested == false);

	return;

	async Task WatchForClient(CharacterData characterData)
	{
		if (TryFindExistProc(gameData, out var existProc)) await existProc.WaitForExitAsync(cancellationTokenSource.Token);

		var file = new FileInfo(gameData.ElementExePath);
		var additionalArgs = string.Empty;

		if (characterData.LowQuality) additionalArgs += "nogfx";

		var startInfo = new ProcessStartInfo
		{
			FileName = file.FullName, WorkingDirectory = file.Directory.FullName,
			Arguments =
				$@"{file.Name} startbypatcher arc:1 novid {additionalArgs} user:{characterData.Login} pwd:{characterData.Password} role:{characterData.Name}"
		};

		while (cancellationTokenSource.IsCancellationRequested == false)
		{
			Console.WriteLine($"Run {characterData.Login}:{characterData.Name} -> {DateTime.Now}");
			using var process = new Process();
			process.StartInfo = startInfo;
			process.Start();
			SpinWait.SpinUntil(() => process.HasExited || process.MainWindowHandle != IntPtr.Zero);

			if (process.HasExited) continue;

			process.SetWindowText($"{characterData.Login}:{characterData.Name}");
			await process.WaitForExitAsync(cancellationTokenSource.Token);
		}
	}
}