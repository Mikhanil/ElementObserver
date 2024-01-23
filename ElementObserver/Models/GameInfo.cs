using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElementObserver.Models;

[Serializable]
public class GameInfo
{
	[JsonIgnore] public string FilePath { get; set; } = string.Empty;

	public string ElementExePath { get; set; } = string.Empty;
	public List<CharacterData> Characters { get; set; } = new List<CharacterData>();

	public static bool TryGetGameInfo(FileInfo file, out GameInfo? gameInfo)
	{
		gameInfo = default;
		if (file.Exists == false)
			return false;

		gameInfo = JsonSerializer.Deserialize<GameInfo>(File.ReadAllText(file.FullName),
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		if (gameInfo == null) return false;

		if (string.IsNullOrEmpty(gameInfo.ElementExePath) == false)
		{
			gameInfo.FilePath = file.FullName;
			return true;
		}
		
		gameInfo = default;
		return false;
	}

	public void Save()
	{
		File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
	}
}