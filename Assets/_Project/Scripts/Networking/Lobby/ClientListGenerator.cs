using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientLists {

    public Dictionary<ulong, ulong[]> onlyClient { get; private set; }
    public Dictionary<ulong, ulong[]> exceptClient { get; private set; }

    public ClientLists (Dictionary<ulong, ILocalPlayer> localPlayers) {
        onlyClient = new Dictionary<ulong, ulong[]>();
        exceptClient = new Dictionary<ulong, ulong[]>();

        foreach(KeyValuePair<ulong, ILocalPlayer> kvp in localPlayers) {
            onlyClient.Add(kvp.Key, new ulong[] { kvp.Key });
            exceptClient.Add(kvp.Key, new ulong[localPlayers.Count - 1]);

            int index = 0;
            foreach(KeyValuePair<ulong, ILocalPlayer> kvp2 in localPlayers) {
                if(kvp2.Key != kvp.Key) {
                    exceptClient[kvp.Key][index] = kvp2.Key;
                    index++;
                }
            }
        }
    }
}
