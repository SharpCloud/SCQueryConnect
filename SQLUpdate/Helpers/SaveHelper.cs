using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;
using System.Xml;
using Microsoft.Win32;

namespace SCQueryConnect.Helpers
{
    public class SaveHelper
    {
        public interface IPostDeserializeAction<T>
        {
            void OnPostDeserialization(T model);
        }
        public static T DeepClone<T>(T obj)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                DataContractSerializer serializer = new DataContractSerializer(typeof(T));
                serializer.WriteObject(stream, obj);
                stream.Position = 0;
                T copyObj = (T)serializer.ReadObject(stream);
                if (copyObj is IPostDeserializeAction<T>)
                    ((IPostDeserializeAction<T>)copyObj).OnPostDeserialization(copyObj);

                return copyObj;
            }
        }

        public static string Serialize<T>(T obj)
        {
            byte[] array;
            using (MemoryStream stream = new MemoryStream())
            {
                DataContractSerializer serializer = new DataContractSerializer(typeof(T));
                serializer.WriteObject(stream, obj);
                array = stream.ToArray();
            }
            string data = System.Text.Encoding.UTF8.GetString(array, 0, array.Length);
            return data;
        }


        public static T Deserialize<T>(string xml)
        {
            using (StringReader sr = new StringReader(xml))
            {
                XmlReader xr = XmlReader.Create(sr);
                DataContractSerializer serializer = new DataContractSerializer(typeof(T));
                T obj = (T)serializer.ReadObject(xr);
                if (obj is IPostDeserializeAction<T>)
                    ((IPostDeserializeAction<T>)obj).OnPostDeserialization(obj);
                return obj;
            }
        }


        /// <summary>
        /// Serializes the JSON.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The obj.</param>
        /// <returns></returns>
        public static string SerializeJSON<T>(T obj)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));

                try
                {
                    serializer.WriteObject(stream, obj);
                    stream.Position = 0;
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
            return "";
        }

        /// <summary>
        /// Deserializes the JSON.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json">The json.</param>
        /// <returns></returns>
        public static T DeserializeJSON<T>(string json)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                ms.Position = 0;
                T obj = (T)serializer.ReadObject(ms);
                if (obj is IPostDeserializeAction<T>)
                    ((IPostDeserializeAction<T>)obj).OnPostDeserialization(obj);
                return obj;
            }
        }

        const string _RegKey = "SOFTWARE\\SharpCloud\\SQLUpdate";
        const string _RegKeyFolders = _RegKey + "\\Profiles";

        public static string RegRead(string KeyName, string defVal)
        {
            return RegRead(_RegKey, KeyName, defVal);
        }
        public static bool RegWrite(string KeyName, object Value)
        {
            return RegWrite(_RegKey, KeyName, Value);
        }

        public static string RegRead(string RegKey, string KeyName, string defVal)
        {
            // Opening the registry key
            RegistryKey rk = Registry.CurrentUser;
            // Open a subKey as read-only
            RegistryKey sk1 = rk.OpenSubKey(RegKey);
            if (sk1 != null)
            {
                var ret = (string)sk1.GetValue(KeyName.ToUpper());
                if (ret == null)
                    return defVal;
                return ret;
            }
            return defVal;
        }

        public static bool RegWrite(string RegKey, string KeyName, object Value)
        {
            // Setting
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey sk1 = rk.CreateSubKey(RegKey);
            // Save the value
            sk1.SetValue(KeyName.ToUpper(), Value);

            return true;
        }

        public static void RegDelete(string keyName)
        {
            // Opening the registry key
            var rk = Registry.CurrentUser;
            var sk1 = rk.OpenSubKey(_RegKey, true);
            var ret = (string) sk1?.GetValue(keyName.ToUpper());
            if (ret != null)
            {
                sk1.DeleteValue(keyName);
            }
        }
    }
}
