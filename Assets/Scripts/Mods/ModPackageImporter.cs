using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;

namespace Madness.Mods
{
    public sealed class ModImportResult
    {
        public bool Success;
        public string ModId;
        public string DisplayName;
        public string Message;
    }

    public static class ModPackageImporter
    {
        private const int MaxEntries = 4096;
        private const long MaxUncompressedBytes = 256L * 1024L * 1024L;
        private static readonly char[] InvalidIdChars = Path.GetInvalidFileNameChars();

        public static ModImportResult ImportPackage(string packagePath)
        {
            if (string.IsNullOrWhiteSpace(packagePath) || !File.Exists(packagePath))
                return Fail("Mod dosyası bulunamadı.");

            string extension = Path.GetExtension(packagePath).ToLowerInvariant();
            if (extension != ".zip" && extension != ".mod" && extension != ".madnessmod")
                return Fail("Desteklenen mod biçimleri: .zip, .mod veya .madnessmod.");

            ModRuntime runtime = ModRuntime.EnsureExists();
            string modsDirectory = runtime.ModsDirectory;
            string stagingRoot = Path.Combine(modsDirectory, ".staging", Guid.NewGuid().ToString("N"));
            string backupRoot = Path.Combine(modsDirectory, ".backup");

            try
            {
                Directory.CreateDirectory(stagingRoot);
                Directory.CreateDirectory(backupRoot);
                ExtractZipSafely(packagePath, stagingRoot);

                string[] manifests = Directory.GetFiles(stagingRoot, "manifest.json", SearchOption.AllDirectories);
                if (manifests.Length != 1)
                    return FailAndClean("Mod paketinde tam olarak bir manifest.json bulunmalıdır.", stagingRoot);

                string manifestPath = manifests[0];
                ModManifest manifest = JsonUtility.FromJson<ModManifest>(File.ReadAllText(manifestPath));
                string validationError = ValidateManifest(manifest);
                if (!string.IsNullOrEmpty(validationError))
                    return FailAndClean(validationError, stagingRoot);

                string packageRoot = Directory.GetParent(manifestPath).FullName;
                string targetDirectory = Path.Combine(modsDirectory, manifest.id);
                string backupDirectory = Path.Combine(backupRoot, manifest.id + "_" + DateTime.UtcNow.Ticks);

                if (!Path.GetFullPath(packageRoot).StartsWith(Path.GetFullPath(stagingRoot), StringComparison.OrdinalIgnoreCase))
                    return FailAndClean("Mod paket kökü staging klasörünün dışına taşamaz.", stagingRoot);

                if (Directory.Exists(targetDirectory))
                {
                    Directory.Move(targetDirectory, backupDirectory);
                }

                try
                {
                    Directory.Move(packageRoot, targetDirectory);
                    if (Directory.Exists(backupDirectory)) Directory.Delete(backupDirectory, true);
                    CleanupEmptyParent(stagingRoot);
                    runtime.RescanMods();
                    return new ModImportResult
                    {
                        Success = true,
                        ModId = manifest.id,
                        DisplayName = string.IsNullOrEmpty(manifest.name) ? manifest.id : manifest.name,
                        Message = "Mod başarıyla içe aktarıldı."
                    };
                }
                catch
                {
                    if (Directory.Exists(targetDirectory)) Directory.Delete(targetDirectory, true);
                    if (Directory.Exists(backupDirectory)) Directory.Move(backupDirectory, targetDirectory);
                    throw;
                }
            }
            catch (InvalidDataException ex)
            {
                return FailAndClean("Mod arşivi geçersiz: " + ex.Message, stagingRoot);
            }
            catch (IOException ex)
            {
                return FailAndClean("Mod dosyası kopyalanamadı: " + ex.Message, stagingRoot);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return FailAndClean("Mod içe aktarılırken beklenmeyen bir hata oluştu.", stagingRoot);
            }
        }

        private static void ExtractZipSafely(string zipPath, string destination)
        {
            long totalBytes = 0;
            int entryCount = 0;
            string destinationFull = EnsureTrailingSeparator(Path.GetFullPath(destination));

            using (FileStream stream = File.OpenRead(zipPath))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, false))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (++entryCount > MaxEntries) throw new InvalidDataException("Mod arşivinde çok fazla dosya var.");
                    if (entry.Length > MaxUncompressedBytes || totalBytes > MaxUncompressedBytes - entry.Length)
                        throw new InvalidDataException("Mod arşivi izin verilen boyutu aşıyor.");
                    totalBytes += entry.Length;

                    string relative = entry.FullName.Replace('\\', '/');
                    if (string.IsNullOrEmpty(relative) || relative.EndsWith("/", StringComparison.Ordinal)) continue;
                    if (relative.StartsWith("/", StringComparison.Ordinal) || relative.Contains(":") || HasParentSegment(relative))
                        throw new InvalidDataException("Mod arşivinde güvensiz dosya yolu bulundu.");

                    string outputPath = Path.GetFullPath(Path.Combine(destination, relative));
                    if (!outputPath.StartsWith(destinationFull, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Mod arşivi staging klasörü dışına yazamaz.");

                    string parent = Path.GetDirectoryName(outputPath);
                    if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
                    using (Stream input = entry.Open())
                    using (FileStream output = File.Create(outputPath))
                        input.CopyTo(output);
                }
            }
        }

        private static string ValidateManifest(ModManifest manifest)
        {
            if (manifest == null) return "manifest.json okunamadı.";
            if (string.IsNullOrWhiteSpace(manifest.id)) return "Manifest içinde mod id bulunmalıdır.";
            if (manifest.id.Length > 64 || manifest.id.IndexOfAny(InvalidIdChars) >= 0 || manifest.id.Contains(".."))
                return "Mod id geçersiz dosya adı içeriyor.";
            for (int i = 0; i < manifest.id.Length; i++)
            {
                char c = manifest.id[i];
                if (!(char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.'))
                    return "Mod id yalnızca harf, rakam, nokta, alt çizgi ve tire içerebilir.";
            }
            if (manifest.id != manifest.id.Trim()) return "Mod id başında veya sonunda boşluk içeremez.";
            if (manifest.apiVersion != ModRuntime.CurrentApiVersion.ToString())
                return "Mod, bu oyunun mod API sürümüyle uyumlu değil.";
            if (manifest.totalConversion && string.IsNullOrWhiteSpace(manifest.mainScene) && string.IsNullOrWhiteSpace(manifest.worldScene))
                return "Total conversion modunda mainScene veya worldScene belirtilmelidir.";
            if (!string.IsNullOrWhiteSpace(manifest.entryScript) && HasParentSegment(manifest.entryScript.Replace('\\', '/')))
                return "entryScript mod klasörü dışına çıkamaz.";
            return null;
        }

        private static bool HasParentSegment(string path)
        {
            return path.Split('/').Any(part => part == ".." || part == ".");
        }

        private static string EnsureTrailingSeparator(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path : path + Path.DirectorySeparatorChar;
        }

        private static void CleanupEmptyParent(string stagingRoot)
        {
            try
            {
                if (Directory.Exists(stagingRoot)) Directory.Delete(stagingRoot, true);
                string parent = Directory.GetParent(stagingRoot).FullName;
                if (Directory.Exists(parent) && !Directory.EnumerateFileSystemEntries(parent).Any()) Directory.Delete(parent);
            }
            catch (Exception ex) { Debug.LogWarning("[ModImporter] Geçici klasör temizlenemedi: " + ex.Message); }
        }

        private static ModImportResult Fail(string message)
        {
            return new ModImportResult { Success = false, Message = message };
        }

        private static ModImportResult FailAndClean(string message, string stagingRoot)
        {
            CleanupEmptyParent(stagingRoot);
            return Fail(message);
        }
    }
}
