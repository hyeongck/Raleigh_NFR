using System;
using System.Collections.Generic;
using System.Linq;

namespace ClothoSharedItems
{
    public abstract class DevBase
    {
        private string m_name;
        private DevType m_type;

        public DevBase(string name, DevType type)
        {
            m_name = name; m_type = type;
        }

        public string Name { get { return m_name; } }
        public DevType Type { get { return m_type; } }

        public abstract bool Open(int groupId);

        public abstract void Close(int groupId);

        public abstract bool IsOpenBy(int groupdId);

        public abstract bool IsOpen { get; }

        public override string ToString()
        {
            return m_name;
        }

        private static DevBase[] m_devices = null;

        public static DevBase[] GetAllDevices()
        {
            if (m_devices == null)
            {
                List<DevBase> devList = new List<DevBase>();
                foreach (Type devType in typeof(DevBase).Assembly.GetTypes())
                {
                    if (devType.IsClass && devType.IsPublic && !devType.IsAbstract &&
                        devType.IsSubclassOf(typeof(DevBase)) && devType.GetConstructor(new Type[] { }) != null)
                    {
                        devList.Add((DevBase)Activator.CreateInstance(devType, new object[] { }));
                    }
                }
                m_devices = devList.ToArray();
            }
            return m_devices;
        }

        public static T[] GetAllDevicesOfType<T>() where T : DevBase
        {
            return GetAllDevices().OfType<T>().ToArray();
        }

        public static T GetFirstDeviceOfType<T>() where T : DevBase
        {
            var devicesMatched = GetAllDevicesOfType<T>();
            return devicesMatched.Length > 0 ? devicesMatched[0] : null;
        }
    }
}