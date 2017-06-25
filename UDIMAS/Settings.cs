using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDIMAS
{
    /// <summary>
    /// Class used in <see cref="Udimas.Settings"/>
    /// </summary>
    public class Settings : DynamicObject
    {
        #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static object CheckValue<T>(dynamic value, object defValue)
        //#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            if (value != null && value is T)
            {
                return value;
            }
            return defValue;
        }
        internal Settings() {
            if (File.Exists(Path.Combine(Udimas.SystemDirectory, "Settings.json")))
                try
                {
                    dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(
                        File.ReadAllText(Path.Combine(Udimas.SystemDirectory, "Settings.json")));
                }
                catch { dictionary = null; }
            dictionary = dictionary ?? new Dictionary<string, object>();
        }

        internal Dictionary<string, object> dictionary
            = new Dictionary<string, object>();

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override bool TryGetMember(
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
            GetMemberBinder binder, out object result)
        {
            string name = binder.Name.ToLower();

            dictionary.TryGetValue(name, out result);
            return true;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override bool TrySetMember(
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
            SetMemberBinder binder, object value)
        {
            dictionary[binder.Name.ToLower()] = value;

            Save();

            return true;
        }

        /// <summary>
        /// Saves settings to file
        /// </summary>
        public void Save()
        {
            File.WriteAllText(
                Path.Combine(Udimas.SystemDirectory, "Settings.json"),
                Newtonsoft.Json.JsonConvert.SerializeObject(dictionary, Newtonsoft.Json.Formatting.Indented));
        }
    }
}
