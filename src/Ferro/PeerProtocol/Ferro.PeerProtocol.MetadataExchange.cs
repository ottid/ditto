﻿using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Linq;

using Ferro.Common;

namespace Ferro
{
    public class MetadataExchange
    {
        // This is where we will store pieces of metadata.
        byte[][] metadataPieces;
        Int32 currentPiece = 0;
        Int64 totalMetadata;

        public MetadataExchange(Int64 metadataSize)
        {
            var numberOfPieces = metadataSize > 16384 ? (int)Math.Ceiling(metadataSize / 16384.0) : 1; // Each piece up to last is 16384 bytes -> 16kb
            metadataPieces = new byte[numberOfPieces][];
            totalMetadata = metadataSize;
        }

        // ourExtCode refers to the value we have associated with ut_metadata
        // theirExtCode refers to the value they have associated with ut_metadata
        public void RequestMetadata(NetworkStream stream, TcpClient connection, byte ourExtCode, byte theirExtCode, byte[] infoHash)
        {
            if (!connection.Connected)
            {
                throw new Exception("Disconnected from peer after handshake.");
            }

            // Request the first piece.
            var initialRequest = ConstructRequestMessage(ourExtCode, 0);
            Console.WriteLine("PEER/META: Sending request for first metadata piece: " + initialRequest.ToHuman());
            stream.Write(initialRequest);

            while (true) {
                Int32 theirLength = 0;
                // Read lengths until we get a non-zero (non-keepalive) length.
                while (theirLength == 0) {
                    var theirPrefix = stream.ReadBytes(4);
                    theirLength = theirPrefix.Decode32BitInteger();
                    if (theirLength == 0) {
                        Console.WriteLine("PEER/META: Got keepalive. Let's send our own!");
                        stream.Write(new byte[4]);
                    }
                }

                Console.WriteLine($"PEER/META: Got message with length {theirLength}.");
                var peerResponse = stream.ReadBytes(theirLength);
                var responseTypeId = peerResponse[0];
                switch (responseTypeId)
                {  
                    case 20:
                        Console.WriteLine("PEER/META: It's an extension message! Hurrah!");
                        var extensionId = peerResponse[1];

                        if (extensionId == theirExtCode) {
                            Console.WriteLine("PEER/META: It's a metadata exchange message!");
                            var data = peerResponse.Slice(2);
                            Int64 dictSize;
                            var dict = Bencoding.DecodeFirstDict(data, out dictSize);
                            var postDict = data.Slice((Int32) dictSize); // This is the metadata itself -- a bencoded dictionary of utf8 strings

                            if (dict.GetInt("piece") != currentPiece)
                            {
                                throw new Exception($"Expected piece {currentPiece}. Instead, received {dict.GetInt("piece")}");
                            }

                            Console.WriteLine($"PEER/META: Got BEP-9 {Bencoding.ToHuman(Bencoding.Encode(dict))} followed by {postDict.Length} bytes of data.");
                            Console.WriteLine("PEER/META: storing...");
                            metadataPieces[currentPiece] = postDict;
                            currentPiece++;
                            
                            if (currentPiece == metadataPieces.Length)
                            {
                                // verify metadata
                                var combinedPieces = new byte[totalMetadata];
                                var index = 0;
                                foreach (var piece in metadataPieces)
                                {
                                    piece.CopyTo(combinedPieces, index);
                                    index += piece.Length;
                                }

                                var hash = combinedPieces.Sha1();
                                if (Enumerable.SequenceEqual(hash, infoHash))
                                {
                                    Console.WriteLine("PEER/META: metadata verified!");
                                    DataHandler.SaveMetadata(combinedPieces);
                                    Console.WriteLine("PEER/META: metadata saved.");
                                }
                                else
                                {
                                    Console.WriteLine("PEER/META: metadata verification failed!");
                                }
                                
                                return;
                            }

                            var request = ConstructRequestMessage(ourExtCode, currentPiece);
                            Console.WriteLine("PEER/META: Requesting the next piece of metadata...");
                            stream.Write(request);

                        } else {
                            Console.WriteLine($"Warning: it's an unexpected message type, ID {extensionId}.");
                        }

                    break;

                    case 0:
                        Console.WriteLine("PEER/META: It's a choke message! :(");
                    break;

                    case 1:
                        Console.WriteLine("PEER/META: It's an unchoke message! :D");
                    break;

                    case 2:
                        Console.WriteLine("PEER/META: It's an interested message! <3");
                    break;

                    case 4:
                        Console.WriteLine("PEER/META: It's a not interested message! </3");
                    break;

                    default:
                        Console.WriteLine($"PEER/META: Unexpected message type {responseTypeId}; ignoring.");
                    break;
                }
            }
        }

        // For messages with msgType 0 (request) and 2 (reject)
        private static byte[] ConstructGenericMessage(int ourExtCode, int msgType, int piece)
        {
            var messageDict = new Dictionary<byte[], object>();
            messageDict.Set("msg_type", msgType);
            messageDict.Set("piece", piece);
            var encodedMsg = Bencoding.Encode(messageDict);

            var length = (encodedMsg.Length + 2).EncodeBytes();

            var message = new byte[encodedMsg.Length + 6];
            length.CopyTo(message, 0);
            message[4] = 20;
            message[5] = (byte) ourExtCode;
            encodedMsg.CopyTo(message, 6);

            return message;
        }

        private static byte[] ConstructRequestMessage(int ourExtCode, int piece)
        {
            return ConstructGenericMessage(ourExtCode, 0, piece);
        }

        private static byte[] ConstructRejectMessage(int ourExtCode, int piece)
        {
            return ConstructGenericMessage(ourExtCode, 2, piece);
        }

        // For messages with msgType 1 (data)
        private static byte[] ConstructRejectMessage(int ourExtCode, int piece, int length)
        {
            return new byte[4];
        }
    }
}