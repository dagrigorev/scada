using Scada.Comm;
using Scada.Network.NdisApiWrapper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace Scada.Network
{
    public class Sniffer
    {
        private Log _networkLog;
        private IntPtr _driverPtr;
        private TCP_AdapterList _adaptersList;
        private AppDirs _appDirs;
        private int _adapterIndex;
        private bool _initiated;
        private uint _dwOldHwFilter;

        public bool Enabled { get; set; }
        public delegate void _onPacketCatched(string sourceIp, string destIp, uint packetCount);
        public event _onPacketCatched OnPacketCatched;

        public Sniffer()
        {
            Enabled = true;

            _initiated = false;
            _dwOldHwFilter = 0;

            _adapterIndex = -1;
            _appDirs = new AppDirs();
            _appDirs.Init((new System.Uri(Assembly.GetExecutingAssembly().CodeBase)).AbsolutePath + Path.DirectorySeparatorChar + "..");

            _networkLog = new Log(Log.Formats.Full);
            _networkLog.FileName = _appDirs.LogDir + "ScadaNetwork.log";

            var driverPtr = Ndisapi.OpenFilterDriver();
            if (!Ndisapi.IsDriverLoaded(driverPtr))
                throw new ApplicationException("Cannot load driver");

            // Retrieve adapter list
            var adList = new TCP_AdapterList();
            Ndisapi.GetTcpipBoundAdaptersInfo(driverPtr, ref adList);
            _adaptersList = adList;
        }

        private uint SetPromisciousMode()
        {
            uint dwOldHwFilter = 0;

            if (!Ndisapi.GetHwPacketFilter(_driverPtr, _adaptersList.m_nAdapterHandle[_adapterIndex], ref dwOldHwFilter))
                _networkLog.WriteAction("Failed to get current packet filter from the network interface.", Log.ActTypes.Error);
            else
                _networkLog.WriteAction($"Succeded to get current packet filter from the network interface. dwOldHwFilter = {dwOldHwFilter}");

            if (!Ndisapi.SetHwPacketFilter(_driverPtr, _adaptersList.m_nAdapterHandle[_adapterIndex], 0x00000020/*NDIS_PACKET_TYPE_PROMISCUOUS*/))
                _networkLog.WriteAction("Failed to set promiscuous mode for the network interface.", Log.ActTypes.Error);
            else
                _networkLog.WriteAction("Succeded to set promiscuous mode for the network interface.");

            var mode = new ADAPTER_MODE
            {
                dwFlags = Ndisapi.MSTCP_FLAG_SENT_LISTEN | Ndisapi.MSTCP_FLAG_RECV_LISTEN,
                hAdapterHandle = _adaptersList.m_nAdapterHandle[_adapterIndex]
            };
            mode.dwFlags = mode.dwFlags | Ndisapi.MSTCP_FLAG_FILTER_DIRECT | Ndisapi.MSTCP_FLAG_LOOPBACK_BLOCK;
            Ndisapi.SetAdapterMode(_driverPtr, ref mode);

            return dwOldHwFilter;
        }

        public void Init()
        {
            if (!_initiated)
                _dwOldHwFilter = SetPromisciousMode();
            _initiated = true;
        }

        public async void SniffAsync()
        {
            await Task.Run(() => DoSniff());
        }

        public void DoSniff()
        {
            Init();

            var buffer = new INTERMEDIATE_BUFFER();
            var bufferPtr = Marshal.AllocHGlobal(Marshal.SizeOf(buffer));
            Win32Api.ZeroMemory(bufferPtr, Marshal.SizeOf(buffer));

            var request = new ETH_REQUEST
            {
                hAdapterHandle = _adaptersList.m_nAdapterHandle[_adapterIndex],
                EthPacket = { Buffer = bufferPtr }
            };

            try
            {
                while (Enabled)
                {
                    if (Ndisapi.ReadPacket(_driverPtr, ref request))
                    {
                        buffer = (INTERMEDIATE_BUFFER)Marshal.PtrToStructure(bufferPtr, typeof(INTERMEDIATE_BUFFER));
                        WriteToLog(buffer, bufferPtr);
                    }
                    else
                        Thread.Sleep(250);
                }
                Marshal.FreeHGlobal(bufferPtr);
                Ndisapi.SetHwPacketFilter(_driverPtr, _adaptersList.m_nAdapterHandle[_adapterIndex], _dwOldHwFilter);
                Ndisapi.CloseFilterDriver(_driverPtr);
            }
            catch (Exception ex)
            {
                _networkLog.WriteException(ex);
            }
        }

        private unsafe void WriteToLog(INTERMEDIATE_BUFFER packetBuffer, IntPtr packetBufferPtr)
        {
            var ethernetHeader = (ETHER_HEADER*)((byte*)packetBufferPtr + (Marshal.OffsetOf(typeof(INTERMEDIATE_BUFFER), "m_IBuffer")).ToInt32());

            switch (Utils.ntohs(ethernetHeader->proto))
            {
                case ETHER_HEADER.ETH_P_IP:
                    {
                        var ipHeader = (IPHeader*)((byte*)ethernetHeader + Marshal.SizeOf(typeof(ETHER_HEADER)));

                        var sourceAddress = new IPAddress(ipHeader->Src);
                        var destinationAddress = new IPAddress(ipHeader->Dest);

                        if (sourceAddress.ToString() != "255.255.255.255" &&
                            destinationAddress.ToString() != "255.255.255.255")
                        {
                            _networkLog.WriteAction(packetBuffer.m_dwDeviceFlags == Ndisapi.PACKET_FLAG_ON_SEND
                                ? "\nMSTCP --> Interface"
                                : "\nInterface --> MSTCP");
                            _networkLog.WriteAction(string.Format("Packet size = {0}", packetBuffer.m_Length));

                            _networkLog.WriteAction(
                                string.Format("\tETHERNET {0:X2}{1:X2}{2:X2}{3:X2}{4:X2}{5:X2} --> {6:X2}{7:X2}{8:X2}{9:X2}{10:X2}{11:X2}",
                                ethernetHeader->source.b1,
                                ethernetHeader->source.b2,
                                ethernetHeader->source.b3,
                                ethernetHeader->source.b4,
                                ethernetHeader->source.b5,
                                ethernetHeader->source.b6,
                                ethernetHeader->dest.b1,
                                ethernetHeader->dest.b2,
                                ethernetHeader->dest.b3,
                                ethernetHeader->dest.b4,
                                ethernetHeader->dest.b5,
                                ethernetHeader->dest.b6
                            ));

                            OnPacketCatched(sourceAddress.ToString(), destinationAddress.ToString(), packetBuffer.m_Length);
                            _networkLog.WriteAction(string.Format("\tIP {0} --> {1} PROTOCOL: {2}", sourceAddress, destinationAddress,
                                ipHeader->P));

                            var tcpHeader = ipHeader->P == IPHeader.IPPROTO_TCP
                                ? (TcpHeader*)((byte*)ipHeader + ((ipHeader->IPLenVer) & 0xF) * 4)
                                : null;
                            var udpHeader = ipHeader->P == IPHeader.IPPROTO_UDP
                                ? (UdpHeader*)((byte*)ipHeader + ((ipHeader->IPLenVer) & 0xF) * 4)
                                : null;

                            if (udpHeader != null)
                                _networkLog.WriteAction(string.Format("\tUDP SRC PORT: {0} DST PORT: {1}", Utils.ntohs(udpHeader->th_sport), Utils.ntohs(udpHeader->th_dport)));

                            if (tcpHeader != null)
                                _networkLog.WriteAction(string.Format("\tTCP SRC PORT: {0} DST PORT: {1}", Utils.ntohs(tcpHeader->th_sport), Utils.ntohs(tcpHeader->th_dport)));
                        }
                    }
                    break;
                case ETHER_HEADER.ETH_P_RARP:
                    Console.WriteLine("\tReverse Addr Res packet");
                    break;
                case ETHER_HEADER.ETH_P_ARP:
                    Console.WriteLine("\tAddress Resolution packet");
                    break;
            }
        }
    }
}
