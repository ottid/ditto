﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Ferro
{
    public class PeerConnector
    {
        // We probably want to keep this private and potentially constant
        // This is the port we'll be listening on
        private Int32 myPort = 6881;
        private IPAddress myIpAddress;
        private byte[] handshakeHeader = new byte[20];
        private byte[] handshakeBuffer = new byte[8];
        private byte[] peerId = new byte[20];

        public PeerConnector(IPAddress ipAddress)
        {
            handshakeHeader[0] = Convert.ToByte(19);
            Array.Copy("BitTorrent protocol".ToASCII(), 0, handshakeHeader, 1, 19);
            myIpAddress = ipAddress;
            peerId.FillRandom();
        }

        public void Handshake(IPAddress peerIP, Int32 peerPort, byte[] infoHash)
        {
            TcpListener connection = new TcpListener(myIpAddress, myPort);
            connection.Start();

            TcpClient client = new TcpClient();
            client.ConnectAsync(peerIP, peerPort).Wait();

            if (client.Connected)
            {
                Console.WriteLine("connected");
            } else
            {
                throw new Exception("Attempted to move on without connecting to peer");
            }

            // Put all of our handshake data into a byte array
            byte[] handshake = new byte[68];
            Array.Copy(handshakeHeader, 0, handshake, 0, handshakeHeader.Length);
            Array.Copy(handshakeBuffer, 0, handshake, handshakeHeader.Length, handshakeBuffer.Length);
            Array.Copy(infoHash, 0, handshake, handshakeHeader.Length + handshakeBuffer.Length, 20);
            Array.Copy(peerId, 0, handshake, handshakeHeader.Length + handshakeBuffer.Length + 20, 20);

            Console.WriteLine(handshake.FromASCII());

            NetworkStream stream = client.GetStream();
            stream.Write(handshake);

            if (stream.CanRead)
            {
                byte[] response = new byte[256];
                stream.Read(response, 0, response.Length);
                Console.WriteLine(response.FromASCII());
            }
            else
            {
                throw new Exception("Unable to read from the current network stream");
            }

            connection.Stop();
        }
    }
}
