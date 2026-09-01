using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace KH
{
    public static class KHSaveSystem
    {
        #region FIELDS

        private static readonly string FILE_NAME = "save.json";
        private static readonly string TEMP_NAME = "save.json.tmp";
        private static readonly string BACKUP_NAME = "save.json.bak";

        // Toggle encryption:
        public static bool UseEncryption = true;
        // WARNING: For real security, don't hardcode keys in game builds.
        // Use platform secure storage, server-based keys, or device-specific keys.
        public static string EncryptionKey = "change_this_to_a_long_secure_key_32chars";

        private static string SavePath => Path.Combine(Application.persistentDataPath, FILE_NAME);
        private static string TempPath => Path.Combine(Application.persistentDataPath, TEMP_NAME);
        private static string BackupPath => Path.Combine(Application.persistentDataPath, BACKUP_NAME);

        #endregion
        #region BACKUP RECOVERY

        public static void RecoverFromBackup()
        {
            if (File.Exists(BackupPath))
                File.Copy(BackupPath, SavePath, true);
        }

        #endregion
        #region ENCRYPTION

        private static string Encrypt(string plainText) => Crypto(plainText, true);
        private static string Decrypt(string cipherText) => Crypto(cipherText, false);

        private static string Crypto(string text, bool encrypt)
        {
            if (string.IsNullOrEmpty(text)) return text;

            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            byte[] keyBytes = new byte[32];
            var rawKey = Encoding.UTF8.GetBytes(EncryptionKey);
            Array.Copy(rawKey, keyBytes, Math.Min(rawKey.Length, keyBytes.Length));
            aes.Key = keyBytes;

            if (encrypt)
            {
                aes.GenerateIV();
                using var enc = aes.CreateEncryptor();

                byte[] plain = Encoding.UTF8.GetBytes(text);
                byte[] cipher = enc.TransformFinalBlock(plain, 0, plain.Length);

                byte[] combined = new byte[aes.IV.Length + cipher.Length];
                Array.Copy(aes.IV, 0, combined, 0, aes.IV.Length);
                Array.Copy(cipher, 0, combined, aes.IV.Length, cipher.Length);

                return Convert.ToBase64String(combined);
            }
            else
            {
                var combined = Convert.FromBase64String(text);

                byte[] iv = new byte[16];
                Array.Copy(combined, 0, iv, 0, 16);
                aes.IV = iv;

                int cipherLen = combined.Length - 16;
                byte[] cipher = new byte[cipherLen];
                Array.Copy(combined, 16, cipher, 0, cipherLen);

                using var dec = aes.CreateDecryptor();
                byte[] plain = dec.TransformFinalBlock(cipher, 0, cipher.Length);

                return Encoding.UTF8.GetString(plain);
            }
        }

        #endregion
        #region LOAD

        public static T Load<T>() where T : new()
        {
            try
            {
                if (!File.Exists(SavePath))
                    return new T();

                string raw = File.ReadAllText(SavePath);
                string json = UseEncryption ? Decrypt(raw) : raw;

                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Load failed: {ex}");
                return new T();
            }
        }

        #endregion
        #region SAVE

        public static void Save<T>(T data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                string outData = UseEncryption ? Encrypt(json) : json;

                File.WriteAllText(TempPath, outData);

                if (File.Exists(BackupPath))
                    File.Delete(BackupPath);

                if (File.Exists(SavePath))
                    File.Move(SavePath, BackupPath);

                File.Move(TempPath, SavePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Save failed: {ex}");
            }
        }

        #endregion
    }
}
