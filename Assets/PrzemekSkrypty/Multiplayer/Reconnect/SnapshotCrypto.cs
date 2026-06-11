using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ElementumDefense.Multiplayer.Reconnect
{
    /// <summary>
    /// Encrypt + sign helper for match snapshots stored in PlayerPrefs.
    ///
    /// <para>
    /// LAYERED ANTI-TAMPER — this is Layer 1. A snapshot blob is AES-encrypted
    /// and carries an HMAC-SHA256 signature. Casual PlayerPrefs editing breaks
    /// decryption / signature verification.
    /// </para>
    ///
    /// <para>
    /// HONEST LIMITATION: the key lives in the client binary, so a determined
    /// attacker who decompiles the build can extract it. This raises the bar
    /// against trivial cheating — it is NOT a cryptographic guarantee. The real
    /// protection against offline edits is Layer 2 (server-witnessed hash in a
    /// Photon Custom Property — see <see cref="MatchSnapshotService"/>).
    /// </para>
    ///
    /// Blob format (Base64 of):  [16-byte IV][32-byte HMAC][ciphertext]
    /// HMAC is computed over (IV + ciphertext).
    /// </summary>
    public static class SnapshotCrypto
    {
        // NOTE: obfuscated split to avoid a single grep-able literal. Not secure
        // against decompilation — see class summary.
        private static readonly string KEY_PART_A = "Elmntm";
        private static readonly string KEY_PART_B = "Dfns!Rcn";
        private static readonly string KEY_PART_C = "ct2024$Snp";

        private const int IV_SIZE = 16;
        private const int HMAC_SIZE = 32;

        private static byte[] DeriveKey()
        {
            // 32-byte key via SHA256 of the concatenated parts.
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(KEY_PART_A + KEY_PART_B + KEY_PART_C));
        }

        private static byte[] DeriveHmacKey()
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(KEY_PART_C + KEY_PART_A + KEY_PART_B));
        }

        /// <summary>Encrypts and signs <paramref name="json"/>. Returns a Base64 blob.</summary>
        public static string Encrypt(string json)
        {
            if (json == null) json = string.Empty;

            byte[] key = DeriveKey();
            byte[] plaintext = Encoding.UTF8.GetBytes(json);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();
            byte[] iv = aes.IV;

            byte[] ciphertext;
            using (var enc = aes.CreateEncryptor())
                ciphertext = enc.TransformFinalBlock(plaintext, 0, plaintext.Length);

            // HMAC over IV + ciphertext.
            byte[] signed = new byte[iv.Length + ciphertext.Length];
            Buffer.BlockCopy(iv, 0, signed, 0, iv.Length);
            Buffer.BlockCopy(ciphertext, 0, signed, iv.Length, ciphertext.Length);

            byte[] mac;
            using (var hmac = new HMACSHA256(DeriveHmacKey()))
                mac = hmac.ComputeHash(signed);

            using var ms = new MemoryStream();
            ms.Write(iv, 0, iv.Length);
            ms.Write(mac, 0, mac.Length);
            ms.Write(ciphertext, 0, ciphertext.Length);
            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// Verifies the signature and decrypts the blob. Returns false on any
        /// tamper / corruption (bad Base64, short blob, HMAC mismatch, bad padding).
        /// </summary>
        public static bool TryDecrypt(string blob, out string json)
        {
            json = null;
            if (string.IsNullOrEmpty(blob)) return false;

            byte[] all;
            try { all = Convert.FromBase64String(blob); }
            catch { return false; }

            if (all.Length < IV_SIZE + HMAC_SIZE) return false;

            byte[] iv = new byte[IV_SIZE];
            byte[] mac = new byte[HMAC_SIZE];
            int cipherLen = all.Length - IV_SIZE - HMAC_SIZE;
            byte[] ciphertext = new byte[cipherLen];

            Buffer.BlockCopy(all, 0, iv, 0, IV_SIZE);
            Buffer.BlockCopy(all, IV_SIZE, mac, 0, HMAC_SIZE);
            Buffer.BlockCopy(all, IV_SIZE + HMAC_SIZE, ciphertext, 0, cipherLen);

            // Recompute HMAC over IV + ciphertext and compare (constant time).
            byte[] signed = new byte[iv.Length + ciphertext.Length];
            Buffer.BlockCopy(iv, 0, signed, 0, iv.Length);
            Buffer.BlockCopy(ciphertext, 0, signed, iv.Length, ciphertext.Length);

            byte[] expected;
            using (var hmac = new HMACSHA256(DeriveHmacKey()))
                expected = hmac.ComputeHash(signed);

            if (!FixedTimeEquals(mac, expected)) return false;

            try
            {
                using var aes = Aes.Create();
                aes.Key = DeriveKey();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.IV = iv;
                using var dec = aes.CreateDecryptor();
                byte[] plaintext = dec.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
                json = Encoding.UTF8.GetString(plaintext);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SnapshotCrypto] Decrypt failed: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Layer 2 integrity hash — stored server-side (Photon Custom Property)
        /// at save time and compared on reconnect. Computed over the plaintext
        /// JSON so it is independent of the random AES IV.
        /// </summary>
        public static string Hash(string json)
        {
            if (json == null) json = string.Empty;
            using var hmac = new HMACSHA256(DeriveHmacKey());
            byte[] h = hmac.ComputeHash(Encoding.UTF8.GetBytes(json));
            return Convert.ToBase64String(h);
        }

        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
