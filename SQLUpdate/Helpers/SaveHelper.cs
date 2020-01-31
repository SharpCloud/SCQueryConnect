using Microsoft.Win32;
using SCQueryConnect.Interfaces;
using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;

namespace SCQueryConnect.Helpers
{
    public class SaveHelper
    {
        public const string VersionKey = "Version";

        private const string RegKey = "SOFTWARE\\SharpCloud\\SQLUpdate";

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

        public static string RegRead(string keyName, string defVal)
        {
            // Opening the registry key
            var rk = Registry.CurrentUser;
            
            // Open a subKey as read-only
            var sk1 = rk.OpenSubKey(RegKey);
            
            if (sk1 != null)
            {
                var ret = (string) sk1.GetValue(keyName.ToUpper());
                if (ret == null)
                {
                    return defVal;
                }

                return ret;
            }
            return defVal;
        }
        
        public static void RegWrite(string keyName, object value)
        {
            // Setting
            var rk = Registry.CurrentUser;
            var sk1 = rk.CreateSubKey(RegKey);
            
            // Save the value
            sk1.SetValue(keyName.ToUpper(), value);
        }

        public static void RegDelete(string keyName)
        {
            // Opening the registry key
            var rk = Registry.CurrentUser;
            var sk1 = rk.OpenSubKey(RegKey, true);
            var ret = (string) sk1?.GetValue(keyName.ToUpper());
            if (ret != null)
            {
                sk1.DeleteValue(keyName);
            }
        }
    }
}
