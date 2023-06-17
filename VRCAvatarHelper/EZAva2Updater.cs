#if UNITY_EDITOR

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace EZAva2
{   
    internal class GitHubAPI
    {
        public string tag_name = "0.0.0";
    }

    internal class Updater
    {
        private static string fetched_tag = "0.0.0";
        private static bool isUpToDate = false;
        
        private static readonly HttpClient Client = new HttpClient();
        private static string repository = "NinDevs/EZAvatar";

        internal static async Task<GitHubAPI> GetLatestRelease()
        {
            Client.Timeout = TimeSpan.FromSeconds(10);
            Client.DefaultRequestHeaders.Add("User-Agent", "EZAvatar");
            Client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

            var url = $"https://api.github.com/repos/{repository}/releases/latest";
            var result = await Client.GetStringAsync(url);
            var json = JsonUtility.FromJson<GitHubAPI>(result);
            return json;         
        }

        public static void CheckForUpdates(bool initCall)
        {          
            var fetch = Task.Run(async () => { await FetchLatestRelease(initCall); });
            fetch.Wait();
            if (!isUpToDate && !initCall)
            {
                AssetDatabase.Refresh();
                AssetDatabase.ImportPackage($"Assets/EZAvatar_{ParseToDLName(fetched_tag)}.unitypackage", true);
                AssetDatabase.DeleteAsset($"Assets/EZAvatar_{ParseToDLName(fetched_tag)}.unitypackage");
            }
        }

        public static void onImportPackageSuccess (string packagename)
        {
            Debug.Log($"<color=cyan>[EZAvatar]</color>: Successfully updated to latest EZAvatar version ({fetched_tag}).");
            EZAvatar.debug = Helper.SetTextColor($"Successfully updated to latest EZAvatar version ({fetched_tag}).", "#1bfa53");
        }

        public static void onImportPackageStarted(string packagename)
        {
            Debug.Log($"<color=green>[EZAvatar]</color>: Fetched latest EZAvatar package ({packagename}), awaiting import..");
        }

        public static void onImportPackageCancelled(string packageName)
        {
            Debug.Log($"<color=yellow>[EZAvatar]</color>: Cancelled the import of package: {packageName}");
        }

        public static void onImportPackageFailed(string packagename, string errormessage)
        {
            Debug.Log($"<color=red>[EZAvatar]</color> Exited from importing package: {packagename} with error: {errormessage}");
        }

        static async Task FetchLatestRelease(bool initCall)
        {
            try
            {
                var latestRelease = await GetLatestRelease();
                var hasUpdate = ParseVersion(latestRelease.tag_name) > ParseVersion(EZAvatar.Version) ? true : false;
                fetched_tag = latestRelease.tag_name;
                if (hasUpdate)
                {
                    Debug.LogWarning($"<color=green>[EZAvatar]</color> New version {latestRelease.tag_name} is available!");
                    EZAvatar.debug = Helper.SetTextColor($"New version available! Your current version: {EZAvatar.Version}. Latest version: {latestRelease.tag_name}. Consider importing the latest package via <b> 'Check for Updates' </b> button.", "yellow");
                    if (!initCall)
                    {
                        var downloadClient = new WebClient();
                        downloadClient.UseDefaultCredentials = true;
                        var dlUrl = $"https://github.com/NinDevs/EZAvatar/releases/download/{latestRelease.tag_name}/EZAvatar_{ParseToDLName(latestRelease.tag_name)}.unitypackage";
                        downloadClient.DownloadFile(new Uri(dlUrl), $"Assets/EZAvatar_{ParseToDLName(latestRelease.tag_name)}.unitypackage");
                        EZAvatar.debug = Helper.SetTextColor("Downloaded latest EZAvatar package, awaiting import..", "#4dcce8");
                    }
                }
                else
                {
                    if (!initCall)
                    {
                        EZAvatar.debug = Helper.SetTextColor("EZAvatar is up to date!", "#1bfa53");
                        Debug.Log("<color=green>[EZAvatar]</color> Up to date!");
                    }
                    isUpToDate = true;
                }
            }
            catch (Exception e)
            {
                Debug.Log($"{e}. Couldn't fetch latest package.");
            }
        }

        internal static int ParseVersion(string version)
        {
            version = version.Substring(1);
            string parsedVersion = version.Replace(".", string.Empty);

            Int32.TryParse(parsedVersion, out int result);
            
            return result;
        }

        internal static string ParseToDLName(string version)
        {
            var dlVersion = version.Remove(version.LastIndexOf('.'), 1);
            var bruh = dlVersion.Substring(1, dlVersion.Length - 1);
         
            return bruh;
        }
    }
}

#endif