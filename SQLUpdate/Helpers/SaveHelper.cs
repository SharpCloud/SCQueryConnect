using Microsoft.Win32;

namespace SCQueryConnect.Helpers
{
    public class SaveHelper
    {
        private const string RegKey = "SOFTWARE\\SharpCloud\\SQLUpdate";

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
