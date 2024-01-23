namespace ElementObserver.Models;

[Serializable]
public class CharacterData
{
	public string Login { get; set; } = string.Empty;
	public string Password { get; set; } = string.Empty;
	public bool LowQuality { get; set; } = true;
	public string? Name { get; set; } = string.Empty;
}