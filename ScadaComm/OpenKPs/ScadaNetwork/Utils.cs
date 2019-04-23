using Scada.Network.NdisApiWrapper;

namespace Scada.Network
{
    public static class Utils
    {
        /// <summary>
        /// Возвращает список доступных сетевых адаптеров
        /// </summary>
        /// <returns></returns>
        public static TCP_AdapterList GetAvailableAdapters()
        {
            var driverPtr = Ndisapi.OpenFilterDriver();
            if (!Ndisapi.IsDriverLoaded(driverPtr))
                throw new System.ApplicationException("Cannot load driver");
            var adList = new TCP_AdapterList();
            Ndisapi.GetTcpipBoundAdaptersInfo(driverPtr, ref adList);
            Ndisapi.CloseFilterDriver(driverPtr);
            return adList;
        }

        public static ushort ntohs(ushort netshort)
        {
            var hostshort = (ushort)(((netshort >> 8) & 0x00FF) | ((netshort << 8) & 0xFF00));
            return hostshort;
        }

        [System.Obsolete]
        public static long ToInt(string addr)
        {
            // careful of sign extension: convert to uint first;
            // unsigned NetworkToHostOrder ought to be provided.
            return (long)(uint)System.Net.IPAddress.NetworkToHostOrder(
                 (int)System.Net.IPAddress.Parse(addr).Address);
        }
    }
}
