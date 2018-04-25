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
      path = Path.GetFullPath(path);
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

    public static bool FileExtensionIs(String path, String extension)
    {
      return Path.GetExtension(path).Equals(extension, StringComparison.InvariantCultureIgnoreCase);
    }
  }
}
