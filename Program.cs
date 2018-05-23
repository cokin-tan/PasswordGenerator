using System;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace PasswordGenerator
{
    public interface IReader
    {
        void Read(BinaryReader reader);
    }

    public interface IWriter
    {
        void Write(BinaryWriter writer);
    }

    internal class ConfigItem : IReader, IWriter
    {
        public string _urls = string.Empty;
        public string _username = string.Empty;
        public string _nowPswd = string.Empty;
        public List<string> _oldPswd = new List<string>();

        public override bool Equals(object obj)
        {
            ConfigItem item = obj as ConfigItem;
            if (null == item)
            {
                return false;
            }

            return _urls == item._urls && _username == item._username;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool IsEqual(string urls, string username)
        {
            return _urls == urls && _username == username;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("-------------------------------\n");
            sb.AppendFormat("url:{0}\nusername:{1}\nnow password:{2}\n", _urls, _username, _nowPswd, _oldPswd);
            sb.Append("old password:");
            if (_oldPswd.Count > 0)
            {
                sb.Append(string.Concat(_oldPswd.ToArray()));
            }
            sb.Append("\n-------------------------------\n");

            return sb.ToString();
        }

        public void Read(BinaryReader reader)
        {
            _urls = reader.ReadString();
            _username = reader.ReadString();
            _nowPswd = reader.ReadString();
            int count = reader.ReadInt32();
            _oldPswd = new List<string>(count);
            for (int index = 0; index < count; ++index)
            {
                _oldPswd.Add(reader.ReadString());
            }
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(_urls);
            writer.Write(_username);
            writer.Write(_nowPswd);
            writer.Write(_oldPswd.Count);
            for (int index = 0; index < _oldPswd.Count; ++index)
            {
                writer.Write(_oldPswd[index]);
            }
        }
    }

    internal class Token : IReader, IWriter
    {
        public string _token { get; private set; }

        public Token()
        {

        }

        public Token(string token)
        {
            _token = token;
        }

        public void Read(BinaryReader reader)
        {
            _token = reader.ReadString();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(_token);
        }

        public bool IsNullEmpty
        {
            get
            {
                return string.IsNullOrEmpty(_token);
            }
        }

        public override bool Equals(object obj)
        {
            string rhs = obj as string;
            return _token.Equals(rhs);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    internal class FileLoader
    {
        public static T Read<T>(string filePath) where T : IReader, new()
        {
            T result = new T();
            if (!File.Exists(filePath))
            {
                return result;
            }

            try
            {
                var data = File.ReadAllBytes(filePath);
                data = Util.D1(data);
                using (var ms = new MemoryStream(data))
                {
                    using (var reader = new BinaryReader(ms))
                    {
                        result.Read(reader);
                    }
                }
            }
            catch
            {

            }

            return result;
        }

        public static List<T> ReadToList<T>(string filePath) where T : IReader, new()
        {
            var result = new List<T>();
            if (!File.Exists(filePath))
            {
                return result;
            }

            try
            {
                var data = File.ReadAllBytes(filePath);
                data = Util.D1(data);
                using (var ms = new MemoryStream(data))
                {
                    using (var reader = new BinaryReader(ms))
                    {
                        int count = reader.ReadInt32();
                        while (count > 0)
                        {
                            var item = new T();
                            item.Read(reader);
                            result.Add(item);
                            --count;
                        }
                    }
                }
            }
            catch
            {

            }

            return result;
        }

        public static void Write<T>(List<T> lst, string filePath) where T : IWriter
        {
            if (lst.Count > 0)
            {
                using (var ms = new MemoryStream())
                {
                    using (var writer = new BinaryWriter(ms))
                    {
                        writer.Write(lst.Count);
                        for (int index = 0; index < lst.Count; ++index)
                        {
                            lst[index].Write(writer);
                        }
                    }

                    var data = ms.ToArray();
                    data = Util.E1(data);

                    WriteFile(filePath, data);
                }
            }
        }

        public static void Write<T>(T item, string filePath) where T : IWriter
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms))
                {
                    item.Write(writer);
                }

                var data = ms.ToArray();
                data = Util.E1(data);

                WriteFile(filePath, data);
            }
        }

        private static void WriteFile(string filePath, byte[] data)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.SetAttributes(filePath, FileAttributes.Normal);
                }

                File.WriteAllBytes(filePath, data);
                File.SetAttributes(filePath, FileAttributes.Encrypted | FileAttributes.ReadOnly | FileAttributes.System);
            }
            catch
            {
            }
        }
    }
    
    internal static class Util
    {
        public static byte[] MD5(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            using (var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider())
            {
                return md5.ComputeHash(bytes);
            }
        }

        public static byte[] D1(byte[] data)
        {
            var stream = new MemoryStream();
            var des = new System.Security.Cryptography.DESCryptoServiceProvider();
            des.Mode = System.Security.Cryptography.CipherMode.CBC;
            des.Padding = System.Security.Cryptography.PaddingMode.PKCS7;
            var cs = new System.Security.Cryptography.CryptoStream(stream, des.CreateDecryptor(ES.Key, ES.IV), System.Security.Cryptography.CryptoStreamMode.Write);
            cs.Write(data, 0, data.Length);
            cs.FlushFinalBlock();
            return stream.ToArray();
        }

        public static byte[] E1(byte[] data)
        {
            var stream = new MemoryStream();
            var des = new System.Security.Cryptography.DESCryptoServiceProvider();
            des.Mode = System.Security.Cryptography.CipherMode.CBC;
            des.Padding = System.Security.Cryptography.PaddingMode.PKCS7;
            var cs = new System.Security.Cryptography.CryptoStream(stream, des.CreateEncryptor(ES.Key, ES.IV), System.Security.Cryptography.CryptoStreamMode.Write);
            cs.Write(data, 0, (int)data.Length);
            cs.FlushFinalBlock();
            return stream.ToArray();
        }
    }

    internal static class ES
    {
        public static byte[] Key
        {
            get
            {
                var key = new byte[8];

                Func<int, int, int, byte> generateFunc = (int a, int b, int c) => { return (byte)(a * 100 + b * 10 + c); };

                goto _G_0;

                _G_7: key[7] = generateFunc(1, 0, 5); goto _G_8;
                _G_1: key[1] = generateFunc(2, 0, 8); goto _G_2;
                _G_6: key[6] = generateFunc(0, 6, 5); goto _G_7;
                _G_2: key[2] = generateFunc(1, 0, 9); goto _G_3;
                _G_5: key[5] = generateFunc(0, 0, 6); goto _G_6;
                _G_0: key[0] = generateFunc(1, 1, 5); goto _G_1;
                _G_4: key[4] = generateFunc(0, 1, 5); goto _G_5;
                _G_3: key[3] = generateFunc(0, 0, 6); goto _G_4;
                _G_8: return key;
            }
        }

        public static byte[] IV
        {
            get
            {
                var key = new byte[16];

                System.Func<int, int, int, byte> generateFunc = (int a, int b, int c) => { return (byte)(a * 100 + b * 10 + c); };

                goto _G_0;

                _G_7: key[7] = generateFunc(1, 0, 5); goto _G_8;
                _G_1: key[1] = generateFunc(0, 0, 8); goto _G_2;
                _G_10: key[10] = generateFunc(0, 1, 6); goto _G_11;
                _G_11: key[11] = generateFunc(1, 0, 6); goto _G_12;
                _G_14: key[14] = generateFunc(0, 0, 6); goto _G_15;
                _G_3: key[3] = generateFunc(0, 1, 2); goto _G_4;
                _G_9: key[9] = generateFunc(0, 0, 5); goto _G_10;
                _G_8: key[8] = generateFunc(0, 2, 4); goto _G_9;
                _G_12: key[12] = generateFunc(2, 0, 5); goto _G_13;
                _G_13: key[13] = generateFunc(2, 0, 0); goto _G_14;
                _G_6: key[6] = generateFunc(1, 6, 5); goto _G_7;
                _G_2: key[2] = generateFunc(0, 0, 9); goto _G_3;
                _G_5: key[5] = generateFunc(2, 0, 6); goto _G_6;
                _G_0: key[0] = generateFunc(0, 1, 5); goto _G_1;
                _G_4: key[4] = generateFunc(1, 1, 5); goto _G_5;
                _G_15: key[15] = generateFunc(0, 0, 6); goto _G_16;
                _G_16: return key;
            }

        }
    }

    class Program
    {
        private const string configPath = "c.bytes";
        private const string tokenPath = "f.bytes";
        private const string privateKeyPath = "p.bytes";

        private static string property = string.Empty;
        private static string privateKey = string.Empty;

        private static string GenericProperty()
        {
            StringBuilder sb = new StringBuilder();
            for (int index = 0; index < 10; ++index)
            {
                sb.Append(index);
            }

            for (char c = 'A'; c <= 'Z'; ++c)
            {
                sb.Append(c);
                sb.Append((char)(c + ('a' - 'A')));
            }
            sb.Append("!@#$&.");

            return sb.ToString();
        }

        private static bool IsAllNumber(string context)
        {
            int result = 0;
            return int.TryParse(context, out result);
        }

        private static string GeneratePassword(Random random)
        {
            int length = 16;
            StringBuilder sb = new StringBuilder();
            while (length > 0)
            {
                int index = random.Next() % property.Length;
                sb.Append(property[index]);
                --length;
            }

            return sb.ToString();
        }

        private static string GeneratePassword(string url, string username)
        {
            string str = string.Format("{0}_{1}_{2}", url, username, privateKey);
            var hash = Util.MD5(str);
            
            int seed = BitConverter.ToInt32(hash, 0);
            Random random = new Random(seed);

            string result = string.Empty;
            do
            {
                result = GeneratePassword(random);
            } while (IsAllNumber(result));
            
            return result;
        }

        private static List<ConfigItem> dataLst = new List<ConfigItem>();

        private static ConfigItem GetPassword(string urls, string username)
        {
            return dataLst.Find(data => { return data.IsEqual(urls, username); });
        }

        private static void GeneratePassword()
        {
            Console.Write("url:");
            string url = Console.ReadLine();
            
            Console.Write("username:");
            string userName = Console.ReadLine();

            var item = GetPassword(url, userName);
            if (null == item)
            {
                var password = GeneratePassword(url, userName);
                dataLst.Add(new ConfigItem() { _urls = url, _username = userName, _nowPswd = password });
                SaveData();
            }
        }
        
        private static void LoadData()
        {
            dataLst = FileLoader.ReadToList<ConfigItem>(configPath);
        }

        private static void SaveData()
        {
            FileLoader.Write(dataLst, configPath);
        }

        private static Token LoadToken()
        {
            return FileLoader.Read<Token>(tokenPath);
        }

        private static void SaveToken(string token)
        {
            var item = new Token(token);
            FileLoader.Write(item, tokenPath);
        }

        private static Token LoadPrivateKey()
        {
            return FileLoader.Read<Token>(privateKeyPath);
        }

        private static void SavePrivateKey(string key)
        {
            var item = new Token(key);
            FileLoader.Write(item, privateKeyPath);
        }

        private static void Search()
        {
            Console.Write("url:");
            string url = Console.ReadLine();

            Console.Write("username:");
            string userName = Console.ReadLine();

            var item = GetPassword(url, userName);
            if (null != item)
            {
                Console.WriteLine("The match password is : " + item._nowPswd);
            }
            else
            {
                Console.WriteLine("The does not has password match urls : {0} username : {1}", url, userName);
            }
        }

        private static void List()
        {
            if (dataLst.Count > 0)
            {
                for (int index = 0; index < dataLst.Count; ++index)
                {
                    Console.WriteLine(dataLst[index].ToString());
                }
            }
            else
            {
                Console.WriteLine("There not have data in list");
            }
        }

        private static void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
            }

            File.Delete(filePath);
        }

        private static void TestReset()
        {
            try
            {
                DeleteFile(tokenPath);
                DeleteFile(privateKeyPath);
            }
            catch
            {
            }
            Environment.Exit(0);
        }

        private static void ModifyPrivateKey()
        {
            if (CheckValue(privateKey, "private key"))
            {
                privateKey = InputConfirmValue("privateKey", SavePrivateKey);
            }
        }

        private static void ModifyPassword()
        {
            Console.Write("url:");
            string url = Console.ReadLine();

            Console.Write("username:");
            string userName = Console.ReadLine();

            var item = GetPassword(url, userName);
            if (null != item)
            {
                string newPassword = GeneratePassword(url, userName);
                if (newPassword != item._nowPswd)
                {
                    item._oldPswd.Add(item._nowPswd);
                    item._nowPswd = newPassword;
                    SaveData();
                }
                Console.WriteLine("new password : {0}", newPassword);
            }
            else
            {
                Console.WriteLine("Can't find the url:{0} username:{1} password", url, userName);
            }
        }

        private static void EnterProgram()
        {
            property = GenericProperty();
            string operation = string.Empty;
            LoadData();
            if (string.IsNullOrEmpty(privateKey))
            {
                privateKey = LoadPrivateKey()._token;
            }

            while (true)
            {
                Console.Write("please enter operation:");
                operation = Console.ReadLine();
                switch (operation.ToLower())
                {
                    case "q":
                        {
                            Environment.Exit(0);
                        }
                        break;
                    case "s":
                        {
                            Search();
                        }
                        break;
                    case "l":
                        {
                            List();
                        }
                        break;
                    case "g":
                        {
                            GeneratePassword();
                        }
                        break;
                    case "p":
                        {
                            ModifyPrivateKey();
                        }
                        break;
                    case "u":
                        {
                            ModifyPassword();
                        }
                        break;
                    case "r":
                        TestReset();
                        break;
                }
            }
        }

        private static bool CheckInputInvalid(char inputChar)
        {
            return char.IsLetterOrDigit(inputChar) || char.IsPunctuation(inputChar);
        }

        private static string ReadInput()
        {
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                var input = Console.ReadKey(true);
                if (input.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                else if (input.Key == ConsoleKey.Backspace)
                {
                    if (sb.Length > 0)
                    {
                        sb.Remove(sb.Length - 1, 1);
                    }
                }
                else if (CheckInputInvalid(input.KeyChar))
                {
                    sb.Append(input.KeyChar);
                }
            }
            return sb.ToString();
        }

        private static string InputConfirmValue(string title, Action<string> saveAction)
        {
            while (true)
            {
                Console.Write("please enter {0}:", title);
                string first = ReadInput();
                Console.Write("please confirm {0}:", title);
                string second = ReadInput();
                if (first == second && !string.IsNullOrEmpty(second))
                {
                    saveAction?.Invoke(second);
                    return second;
                }
                Console.WriteLine("the token is not same, please check!!!");
            }
        }

        private static bool CheckValue(string token, string title)
        {
            int remainCount = 5;
            while (--remainCount > 0)
            {
                Console.Write("please enter {0}:", title);
                string confirm = ReadInput();
                if (token.Equals(confirm))
                {
                    return true;
                }
                else
                {
                    Console.WriteLine("The {0} is invalid, you have {1} times to try!!!", title, remainCount);
                }
            }
            return false;
        }

        static void Main(string[] args)
        {
            var token = LoadToken();
            if (token.IsNullEmpty)
            {
                Console.WriteLine("The system is first time to use!!!");
                InputConfirmValue("token", SaveToken);
                privateKey = InputConfirmValue("privateKey", SavePrivateKey);
                EnterProgram();
            }
            else
            {
                if (CheckValue(token._token, "token"))
                {
                    EnterProgram();
                }
                else
                {
                    Environment.Exit(0);
                }
            }
        }
    }
}
