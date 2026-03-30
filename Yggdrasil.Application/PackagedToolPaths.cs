namespace Yggdrasil.Application
{
    public static class PackagedToolPaths
    {
        public const string VtfCmdRelativePath = "Tools\\VTFCmd.exe";

        public static string GetVtfCmdPath()
        {
            return Path.Combine(AppContext.BaseDirectory, "Tools", "VTFCmd.exe");
        }

        public static bool HasVtfCmd()
        {
            return File.Exists(GetVtfCmdPath());
        }
    }
}
