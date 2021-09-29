using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Discord;

public class DiscordController : MonoBehaviour {
    

    public Discord.Discord discord;
    private ActivityManager activityManager;
    private static DiscordController inst;


    public static DateTime ConvertFromUnixTimestamp (double timestamp) {
        DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        return origin.AddSeconds(timestamp);
    }
    
    public static double ConvertToUnixTimestamp (DateTime date) {
        DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        TimeSpan diff = date.ToUniversalTime() - origin;
        return Math.Floor(diff.TotalSeconds);
    }

    private void OnJoinRequest (ref User user) {
        Debug.Log(user.Username);
    }

    private void OnJoin (string secret) {
        Debug.Log("secret: " + secret);
    }


    private void Start () {
        inst = this;

        try {
            discord = new Discord.Discord(776841990355943444, (System.UInt64)Discord.CreateFlags.NoRequireDiscord);
            activityManager = discord.GetActivityManager();
            activityManager.OnActivityJoinRequest += OnJoinRequest;
            activityManager.OnActivityJoin += OnJoin;
        } catch {
            if(discord == null) {
                return;
            }
            discord.Dispose();
            Destroy(gameObject);
        }
        SetAsInMenu();
    }

    private void Update () {
        if(discord == null) {
            return;
        }
		discord.RunCallbacks();
	}

    private void OnDestroy () {
        if(discord == null) {
            return;
        }
        discord.Dispose();
    }



    public static void SetAsInMenu () {
        if(inst.discord == null) {
            return;
        }
        try {
            var activity = new Discord.Activity {
                State = "In Menu"
            };
            inst.activityManager.ClearActivity((res) => { });
            inst.activityManager.UpdateActivity(activity, (res) => {
                if(res == Discord.Result.Ok) {
                    // Everything be good
                }
            });
        } catch {

        }
    }

    public static void SetAsInLobby (int playerCount, int maxPlayers = 8) {
        if(inst.discord == null) {
            return;
        }
        try {
            var activity = new Discord.Activity {
                State = "In Lobby",
                Details = "Waiting to start",
                Party = new ActivityParty() {
                    Size = new PartySize() {
                        CurrentSize = playerCount,
                        MaxSize = maxPlayers
                    }
                }
            };
            inst.activityManager.ClearActivity((res) => { });
            inst.activityManager.UpdateActivity(activity, (res) => {
                if(res == Discord.Result.Ok) {
                    // Everything be good
                }
            });
        } catch {

        }
    }

    public static void SetAsGameStarted (int playerCount, int minutes, string mapName, string mapThumbnail, int maxPlayers = 8) {
        if(inst.discord == null) {
            return;
        }
        try {
            var activity = new Discord.Activity {
                State = "Playing",
                Details = "Knockout",
                Assets = new ActivityAssets() {
                    LargeImage = mapThumbnail,
                    LargeText = mapName
                },
                Party = new ActivityParty() {
                    Size = new PartySize() {
                        CurrentSize = playerCount,
                        MaxSize = maxPlayers
                    }
                },
                Timestamps = new ActivityTimestamps() {
                    End = (long)ConvertToUnixTimestamp(DateTime.Now.AddMinutes(minutes).AddSeconds(1))
                }
            };
            inst.activityManager.ClearActivity((res) => { });
            inst.activityManager.UpdateActivity(activity, (res) => {
                if(res == Discord.Result.Ok) {
                    // Everything be good
                }
            });
        } catch {

        }
    }
}
