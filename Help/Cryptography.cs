using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace WebAPIUtility.Help
{
    public class Cryptography
    {
		public static string MD5Encrypt(string plaintext)
		{
			if (string.IsNullOrWhiteSpace(plaintext))
			{
				return string.Empty;
			}
			else
			{
				MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();

				return BitConverter.ToString(md5.ComputeHash(Encoding.Unicode.GetBytes(plaintext))).Replace("-", string.Empty).ToUpper();
			}
		}

		private static byte[] GetAesKey(byte[] keyArray, string key)
		{
			byte[] array = new byte[16];
			if (keyArray.Length < 16)
			{
				for (int i = 0; i < array.Length; i++)
				{
					if (i >= keyArray.Length)
					{
						array[i] = 0;
					}
					else
					{
						array[i] = keyArray[i];
					}
				}
			}
			return array;
		}

		public static string Encrypt(string content, string key, bool autoHandle = true)
		{
			byte[] array = Encoding.UTF8.GetBytes(key);
			if (autoHandle)
			{
				array = GetAesKey(array, key);
			}
			byte[] bytes = Encoding.UTF8.GetBytes(content);
			SymmetricAlgorithm symmetricAlgorithm = Aes.Create();
			symmetricAlgorithm.Key = array;
			symmetricAlgorithm.Mode = CipherMode.ECB;
			symmetricAlgorithm.Padding = PaddingMode.PKCS7;
			ICryptoTransform cryptoTransform = symmetricAlgorithm.CreateEncryptor();
			byte[] inArray = cryptoTransform.TransformFinalBlock(bytes, 0, bytes.Length);
			return Convert.ToBase64String(inArray);
		}

		public static string Decrypt(string content, string key, bool autoHandle = true)
		{
			byte[] array = Encoding.UTF8.GetBytes(key);
			if (autoHandle)
			{
				array = GetAesKey(array, key);
			}
			byte[] array2 = Convert.FromBase64String(content);
			SymmetricAlgorithm symmetricAlgorithm = Aes.Create();
			symmetricAlgorithm.Key = array;
			symmetricAlgorithm.Mode = CipherMode.ECB;
			symmetricAlgorithm.Padding = PaddingMode.PKCS7;
			ICryptoTransform cryptoTransform = symmetricAlgorithm.CreateDecryptor();
			byte[] bytes = cryptoTransform.TransformFinalBlock(array2, 0, array2.Length);
			return Encoding.UTF8.GetString(bytes);
		}
	}
}