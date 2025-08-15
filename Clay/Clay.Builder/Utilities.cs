using System.Runtime.CompilerServices;

namespace Clay.Builder;

public record struct BuildData(string MoveTo, FolderData[] Folders);
public record struct FolderData(string FromRelPath, FileData[] Files);
public record struct FileData(string Name, string Rename);

public static class Utilities
{
	public static string GetFilePath([CallerFilePath] string path = default!) => path;
}
