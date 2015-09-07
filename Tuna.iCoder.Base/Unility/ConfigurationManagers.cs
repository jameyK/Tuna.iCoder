using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Tuna.iCoder.Base.Unility
{
    public class ConfigurationManagers<T> where T : class
    {
        private T config = null;
        private static Dictionary<string, T> m_Cache = new Dictionary<string, T>();
        private static object m_Sync = new object();

        private static ConfigurationManagers<T> instance = null;
        public static ConfigurationManagers<T> Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (m_Sync)
                    {
                        if (instance == null)
                        {
                            instance = new ConfigurationManagers<T>();
                        }
                    }
                }
                return instance;
            }
        }

        public T GetConfiguration(string path)
        {
            string key = path.GetHashCode().ToString();
            config = m_Cache[key];
            if (config == null)
            {
                lock (m_Sync)
                {
                    if (config == null)
                    {
                        config = instance.GetConfiguration(key, path);
                    }
                }
            }

            return config;
        }

        private T GetConfiguration(string key, string path)
        {
            T obj = null;
            if (m_Cache[key] == null)
            {
                lock (m_Sync)
                {
                    if (m_Cache[key] == null)
                    {
                        obj = this.LoadFromXML(path);
                        if (obj != null)
                        {
                            m_Cache.Add(key, obj);
                        }
                    }
                }
            }
            return obj;
        }
        private T LoadFromXML(string path)
        {
            T obj = default(T);
            if (File.Exists(path))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                if (File.Exists(path))
                {
                    StreamReader reader = new StreamReader(path);
                    obj = xmlSerializer.Deserialize(reader) as T;
                    reader.Close();
                }
            }
            return obj;
        }
    }
}
