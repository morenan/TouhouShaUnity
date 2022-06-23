using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;
using Photon.Pun;
using TouhouSha.Core;
using ExitGames.Client.Photon;

namespace Com.Morenan.TouhouSha
{
    public class RoomIn : MonoBehaviour
    {
        public Room Core;
        public List<Photon.Realtime.Player> PlayerList = new List<Photon.Realtime.Player>();

        public GameObject ChairPanel;
        public OutputBox OutputBox;
        public Button Modify;
        public ToggleButton Stand;
        public Button Leave;

        private List<RoomPlayerChair> chairs = new List<RoomPlayerChair>();
        private bool _ignore_stand;

        private void Awake()
        {
            Modify.onClick.AddListener(OnModify);
            Stand.CheckedOrNot += Stand_CheckedOrNot;
            Leave.onClick.AddListener(OnLeave);
            foreach (RoomPlayerChair chair in ChairPanel.GetComponentsInChildren<RoomPlayerChair>())
            {
                if (chairs.Contains(chair)) continue;
                chairs.Add(chair);
                App.Hide(chair);
            }
        }

        public void UpdateRoom(bool forceall = false)
        {
            if (Core == null) return;
            Modify.interactable = PhotonNetwork.IsMasterClient;
            while (chairs.Count() < Core.MaxPlayers)
            {
                GameObject go0 = chairs[0].gameObject;
                GameObject go1 = GameObject.Instantiate(go0, ChairPanel.transform);
                RoomPlayerChair chair = go1.GetComponent<RoomPlayerChair>();
                chairs.Add(chair);
                App.Hide(chair);
            }
            for (int i = 0; i < Core.MaxPlayers; i++)
            {
                RoomPlayerChair chair = chairs[i];
                string chairkey = "Chair " + i;
                chair.Index = i;
                App.Show(chair);
                if (!Core.CustomProperties.ContainsKey(chairkey)
                  || String.IsNullOrEmpty(Core.CustomProperties[chairkey]?.ToString()))
                {
                    chair.AI = false;
                    chair.Core = null;
                }
                else if (Core.CustomProperties[chairkey].ToString()?.Equals("AI") == true)
                {
                    chair.AI = true;
                    chair.Core = null;
                }
                else
                {
                    string chairvalue = Core.CustomProperties[chairkey].ToString();
                    Photon.Realtime.Player found = null;
                    foreach (Photon.Realtime.Player player in Core.Players.Values)
                    {
                        if (!chairvalue.Equals("Player:" + player.UserId)) continue;
                        found = player;
                        break;
                    }
                    chair.AI = false;
                    chair.Core = found;
                }
                if (forceall) chair.UpdateAll();
            }
            for (int i = Core.MaxPlayers; i < chairs.Count(); i++)
            {
                RoomPlayerChair chair = chairs[i];
                chair.Index = i;
                chair.AI = false;
                chair.Core = null;
                App.Hide(chair);
            }
        }

        private void OnModify()
        {
            LobbyManager lm = gameObject.GetComponentInParent<LobbyManager>();
            lm?.RoomSetting?.Show(RoomSettingMode.Modify);
        }

        private void OnLeave()
        {
            LobbyManager lm = gameObject.GetComponentInParent<LobbyManager>();
            lm?.LeaveRoom();
        }

        private void Stand_CheckedOrNot(object sender, EventArgs e)
        {
            if (_ignore_stand) return;
            if (Core == null) return;
            LobbyManager lm = gameObject.GetComponentInParent<LobbyManager>();
            if (lm == null) return;
            
            if (Stand.IsOn)
            {
                if (!lm.Stand(PhotonNetwork.LocalPlayer))
                {
                    _ignore_stand = true;
                    Stand.OnClick();
                    _ignore_stand = false;
                }
            }
            else
            {
                if (!lm.StandCancel(PhotonNetwork.LocalPlayer))
                {
                    _ignore_stand = true;
                    Stand.OnClick();
                    _ignore_stand = false;
                }
            }
        }
    }
}
