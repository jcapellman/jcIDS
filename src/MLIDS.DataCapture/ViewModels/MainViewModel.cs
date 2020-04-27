﻿using System;

using Microsoft.Win32;

using MLIDS.lib.ML.Objects;
using MLIDS.lib.Windows.ViewModels;
using PacketDotNet;

using SharpPcap;
using SharpPcap.Npcap;

namespace MLIDS.DataCapture.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private string _fileName;

        private bool _enableFileStream;

        public bool EnableFileStream
        {
            get => _enableFileStream;

            set
            {
                _enableFileStream = value;

                OnPropertyChanged();
            }
        }

        private bool _isCleanTraffic;

        public bool IsCleanTraffic
        {
            get => _isCleanTraffic;

            set
            {
                _isCleanTraffic = value;

                OnPropertyChanged();
            }
        }

        private bool _chkBxSaveEnabled;

        public bool ChkBxSaveEnabled
        {
            get => _chkBxSaveEnabled;

            set
            {
                _chkBxSaveEnabled = value;

                OnPropertyChanged();
            }
        }

        public MainViewModel()
        {
            ChkBxSaveEnabled = true;
        }

        public override void StartAction()
        {
            StartBtnEnabled = false;
            StopBtnEnabled = true;
            DeviceSelectionEnabled = false;
            ChkBxSaveEnabled = false;

            if (EnableFileStream)
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "LOG File|*.csv", Title = "Save a Log File"
                };

                saveDialog.ShowDialog();

                if (string.IsNullOrEmpty(saveDialog.FileName))
                {
                    StopAction();

                    return;
                }

                _fileName = saveDialog.FileName;
            }

            if (SelectedDevice is NpcapDevice)
            {
                var nPcap = SelectedDevice as NpcapDevice;

                nPcap?.Open(OpenFlags.DataTransferUdp | OpenFlags.NoCaptureLocal, 1000);
            }

            SelectedDevice.OnPacketArrival += Device_OnPacketArrival;
            SelectedDevice.StartCapture();
        }

        public override void StopAction()
        {
            try
            {
                SelectedDevice.OnPacketArrival -= Device_OnPacketArrival;

                SelectedDevice.StopCapture();
                SelectedDevice.Close();
            } catch (Exception) { }

            StartBtnEnabled = true;
            StopBtnEnabled = false;
            DeviceSelectionEnabled = true;
            ChkBxSaveEnabled = true;
        }

        private static PayloadItem ToPayloadItem(string protocolType, IPv4Packet sourcePacket,
            TransportPacket payloadPacket, bool cleanTraffic) =>
            new PayloadItem(protocolType, sourcePacket, payloadPacket, cleanTraffic);

        private PayloadItem GetPacket(Packet packet)
        {
            if (!(packet.PayloadPacket is IPv4Packet))
            {
                return null;
            }

            var ipPacket = (IPv4Packet) packet.PayloadPacket;

            switch (ipPacket.Protocol)
            {
                case ProtocolType.Tcp:
                    var tcpPacket = packet.Extract<PacketDotNet.TcpPacket>();

                    return ToPayloadItem("TCP", ipPacket, tcpPacket, IsCleanTraffic);
                case ProtocolType.Udp:
                    var udpPacket = packet.Extract<PacketDotNet.UdpPacket>();

                    return ToPayloadItem("UDP", ipPacket, udpPacket, IsCleanTraffic);
            }

            return null;
        }

        private void Device_OnPacketArrival(object sender, CaptureEventArgs e)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(delegate
            {
                var packet = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);

                if (!packet.HasPayloadPacket)
                {
                    return;
                }

                var payloadItem = GetPacket(packet);

                if (payloadItem == null)
                {
                    return;
                }

                _dataStorage.WritePacketAsync(payloadItem);

                Packets.Add(payloadItem.ToString());
            });
        }
    }
}