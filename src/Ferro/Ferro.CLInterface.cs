using System;
using System.Net;

using Ferro.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.CommandLineUtils;

namespace Ferro {

    class CLInterface {
        // Releases should have a specific version, non-releases should be
        // tagged as -dev of the next reease.
        public static readonly string version = "v0.1-dev";

        ILogger logger { get; } = GlobalLogger.CreateLogger<CLInterface>();
         
        public static int Main(string[] args)
        {
            var cli = new CommandLineApplication {
                Name = "ferro",
                FullName = "Ferro"
            };

            cli.HelpOption("-h | --help | -?");
            cli.VersionOption("--version", version);

            cli.OnExecute(() =>
            {
                cli.ShowHelp();
                return 0;
            });

            cli.Command("get-meta", sub => {
                sub.Description = "Fetches the metadata for the specified torrent, saving it as a .torrent file.";
                sub.HelpOption("-h | --help | -?");

                var verboseOption = sub.Option(
                    "-v | --verbose", "Enables verbose logging", CommandOptionType.NoValue);

                var infohashArgument = sub.Argument(
                    "infohash",
                    "The infohash of the torrent to fetch.");

                sub.OnExecute(() => {
                    GlobalLogger.LoggerFactory.AddConsole(
                        verboseOption.HasValue() ? LogLevel.Debug : LogLevel.Information, true);

                    using (var client = new Ferro.BitTorrent.Client()) {
                        client.Example(new IPAddress[] { IPAddress.Loopback }).Wait();
                    }

                    return 0; 
                });
            });

            cli.Command("connect", sub => {
                sub.Description = "Refreshes the DHT connection as neccessary.";
                sub.HelpOption("-h | --help | -?");

                var verboseOption = sub.Option(
                    "-v | --verbose", "Enables verbose logging", CommandOptionType.NoValue);

                var bootstrapArguments = sub.Argument(
                    "bootstrap",
                    "The address:port pairs for any bootstrap nodes to use if neccessary.");

                sub.OnExecute(() => {
                    GlobalLogger.LoggerFactory.AddConsole(
                        verboseOption.HasValue() ? LogLevel.Debug : LogLevel.Information, true);

                    using (var client = new Ferro.BitTorrent.Client()) {
                        client.Example(new IPAddress[] { IPAddress.Loopback }).Wait();
                    }

                    return 0; 
                });
            });

            return cli.Execute(args);
        }

        static void writeHeader() {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Error.WriteLine($"Ferro BitTorrent CLIent {version}");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Error.WriteLine("https://banks.gitlab.io/ferro/");
            Console.Error.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Error.WriteLine(
                "WARNING: This software is still in an experimental state. It may misbehave towards other peers on the network or your own system. Please limit your use.");
            Console.ResetColor();
        }
    }
}