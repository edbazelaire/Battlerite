using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

public class GameHandler : MonoBehaviour
{
    public static GameHandler Instance { get; private set; }
    private Dictionary<int, List<ulong>> _teams = new ();
    private Dictionary<ulong, PlayerInfos> _playerInfos = new();

    class PlayerInfos
    {
        public ulong NetworkId;
        public int Team;
        public bool IsAlive;

        public PlayerInfos(ulong networkId, int team, bool isAlive)
        {
            NetworkId = networkId;
            Team = team;
            IsAlive = isAlive;
        }
    }

    private void Awake()
    {
        if (Instance != null)
        {
            throw new System.Exception("Multiple GameHandler defined!");
        }

        DontDestroyOnLoad(gameObject);
        Instance = this;
    }

    public void ConnectPlayer(ulong networkId, int team)
    {
        // check if player already registered
        if (_playerInfos.ContainsKey(networkId))
        {
            Debug.Log($"trying to connect Player {networkId} again");
            return;
        }

        _playerInfos[networkId] = new PlayerInfos(networkId, team, true);
        
        // check if player's team exists (otherwise create it)
        if (!_teams.ContainsKey(team))
        {
            _teams[team] = new();
        }

        var playerTeam = _teams[team];
        
        
        if (playerTeam.Contains(networkId))
        {
            Debug.Log($"player {networkId} already in team {team}");
            return;
        }
        
        playerTeam.Add(networkId);
    }

    public void PlayerDied(ulong networkId)
    {
        _playerInfos[networkId].IsAlive = false;

        var remaningTeams = new List<int>();
        foreach (var row in _teams)
        {
            foreach (var playerId in row.Value)
            {
                if (_playerInfos[playerId].IsAlive && ! remaningTeams.Contains(row.Key))
                {
                    remaningTeams.Add(row.Key);
                } 
            }
        }
        
        Assert.IsTrue(remaningTeams.Count > 0, "unable to find remaining teams");

        if (remaningTeams.Count == 1)
        {
            Debug.Log("WIN !");
        }
    }

    public bool IsAlly(ulong netId1, ulong netId2)
    {
        var player1Team = _playerInfos[netId1].Team;

        return _teams[player1Team].Contains(netId2);
    }
}
