using System.Security.Cryptography;
using System.Text;
using ChatApp.Utilities;

namespace ChatApp.Encryption
{
    public static class EncryptionRSA
    {
        private static string EncryptFailure = "EncryptFailure";

        public static string EncryptString(string plainText, string customerId, string salt)
        {
            byte[] key = DeriveKeyFromPassword(customerId, salt);
            byte[] iv = new byte[16];
            RandomNumberGenerator.Fill(iv);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(iv, 0, iv.Length);
                    using (
                        CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write)
                    )
                    using (StreamWriter sw = new StreamWriter(cs, Encoding.UTF8))
                    {
                        sw.Write(plainText);
                    }

                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string DecryptString(string encryptedText, string customerId, string salt)
        {
            byte[] encryptedData = Convert.FromBase64String(encryptedText);
            byte[] key = DeriveKeyFromPassword(customerId, salt);
            byte[] iv = new byte[16];
            Array.Copy(encryptedData, 0, iv, 0, iv.Length);

            byte[] cipherText = new byte[encryptedData.Length - iv.Length];
            Array.Copy(encryptedData, iv.Length, cipherText, 0, cipherText.Length);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using (MemoryStream ms = new MemoryStream(cipherText))
                using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (StreamReader sr = new StreamReader(cs, Encoding.UTF8))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        private static byte[] DeriveKeyFromPassword(string password, string salt)
        {
            const int KeySize = 32;

            using (var hmacsha256 = new HMACSHA256(Encoding.UTF8.GetBytes(salt)))
            {
                return hmacsha256
                    .ComputeHash(Encoding.UTF8.GetBytes(password))
                    .AsSpan(0, KeySize)
                    .ToArray();
            }
        }

        public static byte[] EncryptNew(byte[] fileData, string customerId, string salt)
        {
            byte[] key = DeriveKeyFromPassword(customerId, salt);
            byte[] iv = new byte[16];
            RandomNumberGenerator.Fill(iv);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(iv, 0, iv.Length);

                    using (
                        CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write)
                    )
                    {
                        cs.Write(fileData, 0, fileData.Length);
                    }

                    return ms.ToArray();
                }
            }
        }

        public static byte[] DecryptNew(byte[] encryptedData, string customerId, string salt)
        {
            try
            {
                byte[] key = DeriveKeyFromPassword(customerId, salt);
                byte[] iv = new byte[16];
                Array.Copy(encryptedData, 0, iv, 0, iv.Length);

                byte[] cipherText = new byte[encryptedData.Length - iv.Length];
                Array.Copy(encryptedData, iv.Length, cipherText, 0, cipherText.Length);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;

                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream ms = new MemoryStream(cipherText))
                    {
                        using (
                            CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read)
                        )
                        {
                            using (MemoryStream plainTextStream = new MemoryStream())
                            {
                                cs.CopyTo(plainTextStream);
                                return plainTextStream.ToArray();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return Convert.FromBase64String(ChatExtensions.DefaultBase64Image);
            }
        }

        public static (string publicKeyString, string privateKeyString) GeneratePublicKey()
        {
            using RSA rsa = RSA.Create(4096); // Generate RSA key pair
            var publicKey = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());
            var privateKey = Convert.ToBase64String(rsa.ExportPkcs8PrivateKey());
            return (publicKey, privateKey); // Send this to the sender
        }

        public static string EncryptMessage(string plainText, string publicKey)
        {
            try
            {
                using var rsa = RSA.Create();
                rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKey), out _);

                var encryptedBytes = rsa.Encrypt(
                    System.Text.Encoding.UTF8.GetBytes(plainText),
                    RSAEncryptionPadding.OaepSHA256
                );

                return Convert.ToBase64String(encryptedBytes);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return EncryptFailure;
            }
        }

        public static string DecryptMessage(string encryptedText, string privateKey)
        {
            try
            {
                using var rsa = RSA.Create();
                rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(privateKey), out _);

                var decryptedBytes = rsa.Decrypt(
                    Convert.FromBase64String(encryptedText),
                    RSAEncryptionPadding.OaepSHA256
                );

                return System.Text.Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
                return EncryptFailure;
            }
        }
    }
}
