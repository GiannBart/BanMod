using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using BepInEx;

namespace BanMod
{
    public static class BanModLogZipCollector
    {
        public static string CreateFullLogsZip(out string error)
        {
            error = "";

            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "BanModCommunication");
                Directory.CreateDirectory(tempDir);

                string zipPath = Path.Combine(tempDir, "banmod_logs_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + ".zip");

                if (File.Exists(zipPath))
                    File.Delete(zipPath);

                int added = 0;

                using (FileStream zipStream = new FileStream(zipPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
                {
                    if (AddLogIfExists(archive, "LogOutput.log"))
                        added++;

                    if (AddLogIfExists(archive, "ErrorLog.log"))
                        added++;

                    if (added <= 0)
                    {
                        ZipArchiveEntry entry = archive.CreateEntry("NO_LOGS_FOUND.txt", CompressionLevel.Optimal);
                        using (Stream entryStream = entry.Open())
                        using (StreamWriter writer = new StreamWriter(entryStream, Encoding.UTF8))
                        {
                            writer.WriteLine("Nessun file LogOutput.log o ErrorLog.log trovato nella cartella BepInEx.");
                            writer.WriteLine("BepInExRootPath: " + SafeBepInExRootPath());
                        }
                    }
                }

                return zipPath;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return "";
            }
        }

        public static void TryDeleteTempZip(string zipPath)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(zipPath) && File.Exists(zipPath))
                    File.Delete(zipPath);
            }
            catch { }
        }

        private static bool AddLogIfExists(ZipArchive archive, string fileName)
        {
            string path = GetBepInExLogPath(fileName);

            if (!File.Exists(path))
                return false;

            ZipArchiveEntry entry = archive.CreateEntry(fileName, CompressionLevel.Optimal);

            using (Stream entryStream = entry.Open())
            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fileStream.CopyTo(entryStream);
            }

            return true;
        }

        private static string GetBepInExLogPath(string fileName)
        {
            return Path.Combine(SafeBepInExRootPath(), fileName);
        }

        private static string SafeBepInExRootPath()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(Paths.BepInExRootPath))
                    return Paths.BepInExRootPath;
            }
            catch { }

            return "BepInEx";
        }
    }
}
