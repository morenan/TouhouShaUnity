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
using UnityEngine.SceneManagement;
using ExitGames.Client.Photon;

namespace Com.Morenan.TouhouSha
{
    public class LobbyManager : MonoBehaviourPunCallbacks
    {
        #region Member

        public int RoomOfPages = 10;

        public ConnectWaiting ConnectWaiting;
        public MessageBox MessageBox;
        public GameObject LobbyPanel;
        public RoomIn RoomIn;
        public RoomSetting RoomSetting;
        public RectTransform RoomViewList;
        public InputField CurrentPage;
        public Text MaxinumPage;
        public InputField PlayerNickName;
        public Button PreviousPage;
        public Button NextPage;
        public Button CreateRoomButton;
        public Button RandomJoin;
        public Button RandomJoinSetting;
        public Button Home;

        private int pageindex = 0;
        private int pagemax = 0;
        private List<RoomListBoxItem> roomviews = new List<RoomListBoxItem>();
        private Dictionary<string, RoomInfo> roomcaches = new Dictionary<string, RoomInfo>();
        private RoomCom roomcom;

        private bool _ignore_pageinput;
        
        #endregion

        #region Mono

        void Awake()
        {
            PhotonNetwork.AutomaticallySyncScene = true;
            CurrentPage.onValueChanged.AddListener(OnCurrentPageInput);
            PreviousPage.onClick.AddListener(OnPreviousPage);
            NextPage.onClick.AddListener(OnNextPage);
            CreateRoomButton.onClick.AddListener(OnCreateRoomButtomClick);
            RandomJoin.onClick.AddListener(OnRandomJoin);
            RandomJoinSetting.onClick.AddListener(OnRandomJoinSetting);
            Home.onClick.AddListener(OnHome);
        }

        void Start()
        {
            foreach (RoomListBoxItem roomview in RoomViewList.GetComponentsInChildren<RoomListBoxItem>())
            {
                if (roomviews.Contains(roomview)) continue;
                roomviews.Add(roomview);
                App.Hide(roomview);
            }

            ConnectWaiting.Message.text = "正在连接服务器...";
            App.Show(ConnectWaiting);
            PlayerNickName.text = App.NickName;
            PhotonNetwork.LocalPlayer.NickName = App.NickName;
            PhotonNetwork.ConnectUsingSettings();
        }

        #endregion

        #region Pun

        #region Overrides

        public override void OnConnectedToMaster()
        {
            base.OnConnectedToMaster();
            ConnectWaiting.Message.text = "正在进入大厅...";
            PhotonNetwork.JoinLobby();
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            base.OnDisconnected(cause);
            App.Hide(ConnectWaiting);
            MessageBox.Show(String.Format("网络连接失败。错误代码：{0}", cause), () =>
            {
                SceneManager.LoadScene("MainMenu");
            });
        }

        public override void OnRoomListUpdate(List<RoomInfo> rooms)
        {
            base.OnRoomListUpdate(rooms);
            foreach (RoomInfo room in rooms)
            {
                if (!room.IsOpen || !room.IsVisible || room.RemovedFromList)
                {
                    if (roomcaches.ContainsKey(room.Name))
                        roomcaches.Remove(room.Name);
                }
                else
                {
                    if (!roomcaches.ContainsKey(room.Name))
                        roomcaches.Add(room.Name, room);
                    else
                        roomcaches[room.Name] = room;
                }
            }

            _ignore_pageinput = true;
            pagemax = (roomcaches.Count() - 1) / RoomOfPages;
            pageindex = Math.Min(pageindex, pagemax);
            MaxinumPage.text = (pagemax + 1).ToString();
            CurrentPage.text = (pageindex + 1).ToString();
            UpdateRoomViewList();
            _ignore_pageinput = false;
        }

        public override void OnJoinedLobby()
        {
            base.OnJoinedLobby();
            App.Hide(ConnectWaiting);
        }

        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();
            GameObject go = PhotonNetwork.Instantiate("RoomCom", new Vector3(0, 0, 0), Quaternion.identity);
            if (go != null)
            {
                roomcom = go.GetComponent<RoomCom>();
                if (roomcom != null) roomcom.Output = RoomIn.OutputBox;
            }
            AllocChair(PhotonNetwork.LocalPlayer);
            RoomIn.Core = PhotonNetwork.CurrentRoom;
            RoomIn.UpdateRoom(true);
            App.Hide(ConnectWaiting);
            App.Hide(LobbyPanel);
            App.Show(RoomIn);
        }

        public override void OnLeftRoom()
        {
            base.OnLeftRoom();
            if (roomcom != null)
            {
                PhotonNetwork.Destroy(roomcom.gameObject);
                //roomcom.Output = null;
                roomcom = null;
            }
            App.Show(LobbyPanel);
            App.Hide(RoomIn);
            RoomIn.Core = null;
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            base.OnJoinRoomFailed(returnCode, message);
            App.Hide(ConnectWaiting);
            MessageBox.Show(message);
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            base.OnJoinRandomFailed(returnCode, message);
            App.Hide(ConnectWaiting);
            MessageBox.Show(message);
        }

        public override void OnPlayerEnteredRoom(Photon.Realtime.Player that)
        {
            base.OnPlayerEnteredRoom(that);
            AllocChair(that);
            RoomIn.OutputBox.AppendLine(String.Format("<color=#FFA0A0>玩家 {0} 进入了房间。</color>", that.NickName));
        }

        public override void OnPlayerLeftRoom(Photon.Realtime.Player that)
        {
            base.OnPlayerLeftRoom(that);
            DeallocChair(that);
            RoomIn.OutputBox.AppendLine(String.Format("<color=#FFA0A0>玩家 {0} 离开了房间。</color>", that.NickName));
        }

        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            base.OnRoomPropertiesUpdate(propertiesThatChanged);
            RoomIn.UpdateRoom(true);
        }

        #endregion

        #endregion

        #region Room

        public void CreateRoom(Enum_GameMode gamemode, string nickname, string description)
        {
            DateTime date = DateTime.Now;
            RoomOptions options = new RoomOptions();
            options.PublishUserId = true;
            options.CustomRoomProperties = new Hashtable();
            options.CustomRoomProperties.Add("Mode", ((int)gamemode));
            options.CustomRoomProperties.Add("Name", nickname);
            options.CustomRoomProperties.Add("Desc", description);
            options.CustomRoomProperties.Add("Time", DateTime.Now.ToFileTime());
            options.CustomRoomPropertiesForLobby = new string[4];
            options.CustomRoomPropertiesForLobby[0] = "Mode";
            options.CustomRoomPropertiesForLobby[1] = "Name";
            options.CustomRoomPropertiesForLobby[2] = "Desc";
            options.CustomRoomPropertiesForLobby[3] = "Time";
            switch (gamemode)
            {
                case Enum_GameMode.StandardPlayers5:
                    options.MaxPlayers = 5;
                    break;
                case Enum_GameMode.StandardPlayers8:
                    options.MaxPlayers = 8;
                    break;
                case Enum_GameMode.KOF:
                    options.MaxPlayers = 2;
                    break;
                case Enum_GameMode.FightLandlord:
                    options.MaxPlayers = 3;
                    break;
                case Enum_GameMode.Players2v2:
                    options.MaxPlayers = 4;
                    break;
                case Enum_GameMode.Players3v3:
                    options.MaxPlayers = 6;
                    break;
            }
            ConnectWaiting.Message.text = "正在创建房间...";
            App.Show(ConnectWaiting);
            PhotonNetwork.CreateRoom(null, options, null);
        }

        protected bool AllocChair(Photon.Realtime.Player that)
        {
            if (!PhotonNetwork.IsMasterClient) return false;
            Room room = PhotonNetwork.CurrentRoom;
            if (room == null) return false;
            int ci = GetChairIndex(that);
            string key = null;
            if (ci < 0)
            {
                for (int i = 0; i < room.MaxPlayers; i++)
                {
                    key = "Chair " + i;
                    if (!room.CustomProperties.ContainsKey(key)
                     || String.IsNullOrEmpty(room.CustomProperties[key]?.ToString()))
                    {
                        ci = i;
                        break;
                    }
                }
            }
            if (ci < 0) return false;
            key = "Chair " + ci;
            Hashtable changed = new Hashtable();
            changed.Add(key, "Player:" + that.UserId);
            return room.SetCustomProperties(changed);
        }

        protected bool DeallocChair(Photon.Realtime.Player that)
        {
            if (!PhotonNetwork.IsMasterClient) return false;
            Room room = PhotonNetwork.CurrentRoom;
            if (room == null) return false;
            int ci = GetChairIndex(that);
            if (ci < 0) return false;
            string key = "Chair " + ci;
            Hashtable changed = new Hashtable();
            changed.Add(key, null);
            return room.SetCustomProperties(changed);
        }

        public int GetChairIndex(Photon.Realtime.Player that)
        {
            Room room = PhotonNetwork.CurrentRoom;
            if (room == null) return -1;
            int ci = -1;
            string key = null;
            for (int i = 0; i < room.MaxPlayers; i++)
            {
                key = "Chair " + i;
                if (room.CustomProperties.ContainsKey(key)
                 && room.CustomProperties[key].ToString().Equals("Player:" + that.UserId))
                {
                    ci = i;
                    break;
                }
            }
            return ci;
        }

        public bool AllocAI(int chairindex)
        {
            if (!PhotonNetwork.IsMasterClient) return false;
            Room room = PhotonNetwork.CurrentRoom;
            if (room == null) return false;
            string key = "Chair " + chairindex;
            if (room.CustomProperties.ContainsKey(key)
             && !String.IsNullOrEmpty(room.CustomProperties[key]?.ToString()))
                return false;
            Hashtable changed = new Hashtable();
            changed.Add(key, "AI");
            return room.SetCustomProperties(changed);
        }

        public bool DeallocAI(int chairindex)
        {
            if (!PhotonNetwork.IsMasterClient) return false;
            return DeallocChairOfIndex(chairindex);
        }

        public bool DeallocChairOfIndex(int chairindex)
        {
            Room room = PhotonNetwork.CurrentRoom;
            if (room == null) return false;
            string key = "Chair " + chairindex;
            Hashtable changed = new Hashtable();
            changed.Add(key, null);
            return room.SetCustomProperties(changed);
        }

        public bool KickPlayer(Photon.Realtime.Player that)
        {
            if (!PhotonNetwork.IsMasterClient) return false;
            if (roomcom == null) return false; 
            foreach (RoomCom othercom in roomcom.gameObject.transform.parent.gameObject.GetComponentsInChildren<RoomCom>())
            {
                if (othercom.photonView.Owner != that) continue;
                othercom.photonView.RPC("BeKick", that);
                return true;
            }
            return false;
        }

        public bool Stand(Photon.Realtime.Player that)
        {
            Room room = PhotonNetwork.CurrentRoom;
            if (room == null) return false;
            int ci = GetChairIndex(that);
            if (ci < 0) return false;
            string key = "Stand " + ci;
            Hashtable changed = new Hashtable();
            changed.Add(key, true);
            return room.SetCustomProperties(changed);
        }

        public bool StandCancel(Photon.Realtime.Player that)
        {
            Room room = PhotonNetwork.CurrentRoom;
            if (room == null) return false;
            int ci = GetChairIndex(that);
            if (ci < 0) return false;
            string key = "Stand " + ci;
            Hashtable changed = new Hashtable();
            changed.Add(key, false);
            return room.SetCustomProperties(changed);
        }

        public void Comment(string text)
        {
            roomcom?.Commit(text);
        }

        public void LeaveRoom()
        {
            StandCancel(PhotonNetwork.LocalPlayer);
            if (PhotonNetwork.IsMasterClient)
                DeallocChair(PhotonNetwork.LocalPlayer);
            PhotonNetwork.LeaveRoom();
        }

        #endregion 

        #region View

        #region Room List

        public void UpdateRoomViewList()
        {
            int roomstart = pageindex * RoomOfPages;
            List<KeyValuePair<long, RoomInfo>> date2rooms = roomcaches.Values.Select(_room =>
                new KeyValuePair<long, RoomInfo>((long)(_room.CustomProperties["Time"]), _room)).ToList();
            date2rooms.Sort((p0, p1) => p0.Key.CompareTo(p1.Key));
            while (roomviews.Count() < RoomOfPages)
            {
                GameObject go0 = roomviews[0].gameObject;
                GameObject go1 = GameObject.Instantiate(go0, RoomViewList.transform);
                RoomListBoxItem roomview = go1.GetComponent<RoomListBoxItem>();
                roomviews.Add(roomview);
                App.Hide(roomview);
            }
            for (int i = 0; i < RoomOfPages; i++)
            {
                RoomListBoxItem roomview = roomviews[i];
                RoomInfo room = null;
                if (roomstart + i < date2rooms.Count()) room = date2rooms[roomstart + i].Value;
                roomview.Core = room;
            }
            for (int i = RoomOfPages; i < roomviews.Count(); i++)
            {
                RoomListBoxItem roomview = roomviews[i];
                roomview.Core = null;
            }
        }

        #endregion

        #endregion

        #region Event Handler

        #region UI

        private void OnCurrentPageInput(string arg0)
        {
            if (_ignore_pageinput) return;
            int newpage = 0;
            if (!int.TryParse(CurrentPage.text, out newpage)) return;
            newpage--;
            if (newpage < 0) return;
            if (newpage > pagemax) return;
            pageindex = newpage;
            UpdateRoomViewList();
        }

        private void OnPreviousPage()
        {
            if (pageindex <= 0) return;
            _ignore_pageinput = true;
            CurrentPage.text = (--pageindex).ToString();
            UpdateRoomViewList();
            _ignore_pageinput = false;
        }

        private void OnNextPage()
        {
            if (pageindex >= pagemax) return;
            _ignore_pageinput = true;
            CurrentPage.text = (++pageindex).ToString();
            UpdateRoomViewList();
            _ignore_pageinput = false;
        }

        private void OnCreateRoomButtomClick()
        {
            RoomSetting.Show(RoomSettingMode.Create);
        }

        private void OnRandomJoin()
        {

        }

        private void OnRandomJoinSetting()
        {
            RoomSetting.Show(RoomSettingMode.RandomJoin);
        }

        private void OnHome()
        {
            PhotonNetwork.LeaveLobby();
            PhotonNetwork.Disconnect();
            SceneManager.LoadScene("MainMenu");
        }

        #endregion

        #endregion
    }
}
