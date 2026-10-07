using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KH
{
    public static class KHSaveSystem
    {
        #region FIELDS

        private const string FILE_NAME = "save.json";
        private const string TEMP_NAME = "save.json.tmp";
        private const string BACKUP_NAME = "save.json.bak";

        private const int IV_SIZE = 16;
        private const int MAC_SIZE = 32; // HMAC-SHA256

        // Toggle encryption.
        public static bool UseEncryption = true;

        // WARNING: this is obfuscation, not real security. A key hardcoded in the build
        // can be extracted. It stops casual editing and detects tampering/corruption.
        public static string EncryptionKey = "change_this_to_a_long_secure_key_32chars";

        private static string SavePath => Path.Combine(Application.persistentDataPath, FILE_NAME);
        private static string TempPath => Path.Combine(Application.persistentDataPath, TEMP_NAME);
        private static string BackupPath => Path.Combine(Application.persistentDataPath, BACKUP_NAME);

        public static bool HasSave => File.Exists(SavePath) || File.Exists(BackupPath);

        #endregion
        #region LOAD

        /// <summary>
        /// Loads the save: tries save.json, then save.json.bak.
        /// Raw JSON is migrated to the latest version before deserializing.
        /// Returns a new T if nothing could be loaded.
        /// </summary>
        public static T Load<T>() where T : new()
        {
            if (TryLoadFrom(SavePath, out T data))
                return data;

            if (File.Exists(BackupPath) && TryLoadFrom(BackupPath, out data))
            {
                Debug.LogWarning("Main save was missing or corrupt. Restored from backup.");

                // Put the good backup back as the main save, so the next Save()
                // doesn't rotate a corrupt main file over the good backup.
                try { File.Copy(BackupPath, SavePath, true); }
                catch (Exception ex) { Debug.LogError($"Failed to restore backup: {ex}"); }

                return data;
            }

            return CreateNew<T>();
        }

        private static T CreateNew<T>() where T : new()
        {
            var data = new T();
            if (data is IKHSaveDefaults withDefaults)
                withDefaults.InitializeDefaults();
            return data;
        }

        private static bool TryLoadFrom<T>(string path, out T data) where T : new()
        {
            data = default;
            if (!File.Exists(path)) return false;

            try
            {
                string raw = File.ReadAllText(path);
                string json = UseEncryption ? Decrypt(raw) : raw;

                JObject root = JObject.Parse(json);
                KHSaveMigrationSystem.Migrate<T>(root);

                data = root.ToObject<T>();
                if (data == null) return false;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Load failed for {path}: {ex}");
                PreserveCorruptCopy(path);
                return false;
            }
        }

        private static void PreserveCorruptCopy(string path)
        {
            try { File.Copy(path, path + ".corrupt", true); }
            catch { /* best effort */ }
        }

        #endregion
        #region SAVE

        public static bool Save<T>(T data)
        {
            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                string output = UseEncryption ? Encrypt(json) : json;

                // 1. Write the new data to a temp file first.
                File.WriteAllText(TempPath, output);

                // 2. Swap it in, keeping the previous save as the backup.
                if (File.Exists(SavePath))
                {
                    try
                    {
                        File.Replace(TempPath, SavePath, BackupPath);
                    }
                    catch (Exception)
                    {
                        // File.Replace isn't supported on every platform/filesystem.
                        File.Copy(SavePath, BackupPath, true);
                        File.Copy(TempPath, SavePath, true);
                        File.Delete(TempPath);
                    }
                }
                else
                {
                    File.Move(TempPath, SavePath);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Save failed: {ex}");
                return false;
            }
        }

        #endregion
        #region DELETE

        /// <summary>Deletes the save, backup and temp files.</summary>
        public static void DeleteAll()
        {
            TryDelete(SavePath);
            TryDelete(BackupPath);
            TryDelete(TempPath);
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception ex) { Debug.LogError($"Delete failed for {path}: {ex}"); }
        }

        #endregion
        #region ENCRYPTION

        // Format (base64): IV (16) | AES-CBC ciphertext | HMAC-SHA256 over IV+ciphertext (32)

        private static byte[] DeriveKey(string purpose)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(EncryptionKey + "|" + purpose));
        }

        private static string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = DeriveKey("enc");
            aes.GenerateIV();

            byte[] plain = Encoding.UTF8.GetBytes(plainText);
            byte[] cipher;
            using (var enc = aes.CreateEncryptor())
                cipher = enc.TransformFinalBlock(plain, 0, plain.Length);

            byte[] combined = new byte[IV_SIZE + cipher.Length + MAC_SIZE];
            Buffer.BlockCopy(aes.IV, 0, combined, 0, IV_SIZE);
            Buffer.BlockCopy(cipher, 0, combined, IV_SIZE, cipher.Length);

            using (var hmac = new HMACSHA256(DeriveKey("mac")))
            {
                byte[] tag = hmac.ComputeHash(combined, 0, IV_SIZE + cipher.Length);
                Buffer.BlockCopy(tag, 0, combined, IV_SIZE + cipher.Length, MAC_SIZE);
            }

            return Convert.ToBase64String(combined);
        }

        private static string Decrypt(string cipherText)
        {
            byte[] combined = Convert.FromBase64String(cipherText);
            if (combined.Length < IV_SIZE + 16 + MAC_SIZE)
                throw new InvalidDataException("Save file is too short.");

            int cipherLen = combined.Length - IV_SIZE - MAC_SIZE;

            // Verify integrity BEFORE decrypting.
            byte[] expected;
            using (var hmac = new HMACSHA256(DeriveKey("mac")))
                expected = hmac.ComputeHash(combined, 0, IV_SIZE + cipherLen);

            byte[] actual = new byte[MAC_SIZE];
            Buffer.BlockCopy(combined, IV_SIZE + cipherLen, actual, 0, MAC_SIZE);

            if (!FixedTimeEquals(expected, actual))
                throw new CryptographicException("Save file failed integrity check (tampered, corrupt or wrong key).");

            byte[] iv = new byte[IV_SIZE];
            Buffer.BlockCopy(combined, 0, iv, 0, IV_SIZE);

            byte[] cipher = new byte[cipherLen];
            Buffer.BlockCopy(combined, IV_SIZE, cipher, 0, cipherLen);

            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = DeriveKey("enc");
            aes.IV = iv;

            using var dec = aes.CreateDecryptor();
            byte[] plain = dec.TransformFinalBlock(cipher, 0, cipher.Length);
            return Encoding.UTF8.GetString(plain);
        }

        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }

        #endregion
    }
}
