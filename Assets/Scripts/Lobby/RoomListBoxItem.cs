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

namespace Com.Morenan.TouhouSha
{
    public class RoomListBoxItem : MonoBehaviour
    {
        public Text Name;
        public Text Mode;
        public Text Description;
        public Text Players;

        private RoomInfo core;
        public RoomInfo Core
        {
            get
            {
                return this.core;
            }
            set
            {
                this.core = value;
                if (core == null)
                {
                    App.Hide(this);
                    return;
                }
                Mode.text = RoomSetting.GameModeToString((Enum_GameMode)((int)(core.CustomProperties["Mode"])));
                Name.text = core.CustomProperties["Name"]?.ToString();
                Description.text = core.CustomProperties["Desc"]?.ToString();
                Players.text = String.Format("{0} / {1}", core.PlayerCount, core.MaxPlayers);
                App.Show(this);
            }
        }

        void Awake()
        {
            Button button = gameObject.GetComponent<Button>();
            button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            if (Core == null) return;
            if (Core.PlayerCount >= Core.MaxPlayers) return;
            LobbyManager lm = gameObject.GetComponentInParent<LobbyManager>();
            if (lm == null) return;
            PhotonNetwork.LocalPlayer.NickName = App.NickName = lm.PlayerNickName.text;
            PhotonNetwork.JoinRoom(Core.Name);
        }
    }
}
