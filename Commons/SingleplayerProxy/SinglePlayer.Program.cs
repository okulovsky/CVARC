﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Infrastructure;
using Ionic.Zip;
using ProxyCommon;

namespace SingleplayerProxy
{
    class Program
    {
        private static int currentVersion;

        static void Main(string[] args)
        {
            ReloadVersion();
            if (IsUnityUp())
            {
                Console.WriteLine("Error: Unity already up, close it before start proxy");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            UpdateUnityIfNeeded();
            StartUnity();

            var unityWaiterTask = WaitUntillUnityClosed();
            var listener = new TcpListener(SingleplayerProxyConfigurations.ProxyEndPoint);

            while (true)
            {
                var tasks = new[] {unityWaiterTask, listener.AcceptTcpClientAsync()};
                var index = Task.WaitAny(tasks);
                if (index == 0)
                    break;
                var client = ((Task<TcpClient>) tasks[1]).Result;
                PlayGame(client);
            }
        }

        static Process GetUnityProcess() =>
            Process.GetProcessesByName(SingleplayerProxyConfigurations.UnityProcessName).FirstOrDefault();

        static bool IsUnityUp() =>
            GetUnityProcess() != null;

        static void KillUnity() =>
            GetUnityProcess()?.Kill();

        static bool IsUpdateAvailable() =>
            WebHelper.ReadFromUrlAsync<int>(SingleplayerProxyConfigurations.UrlToGetVersion).Result > currentVersion;

        static void StartUnity() =>
            Process.Start(SingleplayerProxyConfigurations.UnityExePath);

        static void UpdateUnityIfNeeded()
        {
            Console.WriteLine("Checking update...");
            if (!IsUpdateAvailable())
            {
                Console.WriteLine("You use actual version");
                return;
            }
            Console.WriteLine("Update available! Start to download...");

            KillUnity();
            WebHelper.DownloadFile(SingleplayerProxyConfigurations.UrlToGetUpdate, "update.zip").Wait();
            InstallUpdate("update.zip");
            Console.WriteLine("Update successful!");
        }

        static async Task WaitUntillUnityClosed()
        {
            var unityProcess = GetUnityProcess();
            if (unityProcess == null)
                return;
            while (!unityProcess.HasExited)
                await Task.Delay(500);
        }

        static void PlayGame(TcpClient client)
        {
            var gameSettings = client.ReadJson<GameSettings>();
            // var worldState = client.ReadLine/ReadJObject()
            var mainConnection = ConnectToServer();
            mainConnection.WriteJson(gameSettings);
            //mainConnection.WriteLine/JObject(worldState)
            var server = ConnectToServer();
            Proxy.CreateChainAndStart(server, client);
            var result = mainConnection.ReadJson<GameResult>();
            Console.WriteLine("Game complete. Results: " + result);
        }

        static TcpClient ConnectToServer()
        {
            var client = new TcpClient();
            client.Connect(SingleplayerProxyConfigurations.UnityEndPoint);
            return client;
        }

        public static void InstallUpdate(string pathToZip)
        {
            using (var zip = ZipFile.Read(pathToZip))
                zip.ExtractAll(SingleplayerProxyConfigurations.UrlToUnityDir, ExtractExistingFileAction.OverwriteSilently);
        }

        public static void ReloadVersion()
        {
            if (File.Exists(SingleplayerProxyConfigurations.PathToVersionFile))
                File.WriteAllText(SingleplayerProxyConfigurations.PathToVersionFile, "-1");
            currentVersion = int.Parse(File.ReadAllText(SingleplayerProxyConfigurations.PathToVersionFile));
        }
    }
}