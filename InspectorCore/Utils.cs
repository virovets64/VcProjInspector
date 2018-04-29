using System;
using System.IO;
using System.Linq;

namespace InspectorCore
{
  public class Utils
  {
    private static String[] drives = Directory.GetLogicalDrives();

    public static string GetActualFullPath(String path)
    {
      path = RemoveTrailigSlash(path);
      var dir = Path.GetDirectoryName(path);
      if (String.IsNullOrEmpty(dir))
      {
        var root = Path.GetPathRoot(path);
        return drives.First(x => x.Equals(root, StringComparison.InvariantCultureIgnoreCase));
      }
      else
      {
        return Directory.GetFileSystemEntries(GetActualFullPath(dir), Path.GetFileName(path)).First();
      }
    }

    public static string RemoveTrailigSlash(String path)
    {
      if (Path.GetDirectoryName(path) != null)
        path = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
      return path;
    }


    public static bool FileExtensionIs(String path, String extension)
    {
      return Path.GetExtension(path).Equals(extension, StringComparison.InvariantCultureIgnoreCase);
    }
  }
}
