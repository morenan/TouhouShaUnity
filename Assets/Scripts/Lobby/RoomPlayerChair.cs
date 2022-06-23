using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;
using Photon.Pun;

namespace Com.Morenan.TouhouSha
{
    public class RoomPlayerChair : MonoBehaviour
    {
        public int Index;

        private bool ai;
        public bool AI
        {
            get
            {
                return this.ai;
            }
            set
            {
                if (ai == value) return;
                this.ai = value;
                UpdateAll();
            }
        }

        private Player core;
        public Player Core
        {
            get
            {
                return this.core;
            }
            set
            {
                if (core == value) return;
                this.core = value;
                UpdateAll();
            }
        }


        public Text Name;
        public Button[] Buttons;

        private void Awake()
        {
            for (int i = 0; i < Buttons.Length; i++)
            {
                switch (i)
                {
                    case 0:
                        Buttons[i].onClick.AddListener(OnButton0Click);
                        break;
                    case 1:
                        Buttons[i].onClick.AddListener(OnButton1Click);
                        break;
                }
            }
        }

        public void UpdateAll()
        {
            if (AI)
            {
                Name.text = "<color=#000080>[电脑]</color>";
                Buttons[0].gameObject.GetComponentInChildren<Text>().text = "踢出房间";
                Buttons[0].interactable = PhotonNetwork.IsMasterClient;
                App.Show(Buttons[0]);
                gameObject.GetComponent<Image>().color = new Color(1, 1, 1, 1);
            }
            else if (Core != null)
            {
                if (PhotonNetwork.CurrentRoom != null
                 && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("Stand " + Index)
                 && PhotonNetwork.CurrentRoom.CustomProperties["Stand " + Index] is bool
                 && (bool)(PhotonNetwork.CurrentRoom.CustomProperties["Stand " + Index]))
                    Name.text = "<color=#008000>[玩家]</color>" + Core.NickName + "<color=#008000>(已准备)</color>";
                else
                    Name.text = "<color=#808080>[玩家]</color>" + Core.NickName;
                Buttons[0].gameObject.GetComponentInChildren<Text>().text = "踢出房间";
                Buttons[0].interactable = PhotonNetwork.IsMasterClient;
                if (Core == PhotonNetwork.LocalPlayer) App.Hide(Buttons[0]); else App.Show(Buttons[0]);
                gameObject.GetComponent<Image>().color = new Color(1, 1, 1, 1);
            }
            else
            {
                Name.text = "等待加入...";
                Buttons[0].gameObject.GetComponentInChildren<Text>().text = "添加电脑";
                if (PhotonNetwork.IsMasterClient) App.Show(Buttons[0]); else App.Hide(Buttons[0]);
                gameObject.GetComponent<Image>().color = new Color(1, 1, 1, 0);
            }
        }

        private void OnButton0Click()
        {
            LobbyManager lm = gameObject.GetComponentInParent<LobbyManager>();
            if (lm == null) return;
            if (AI)
                lm.DeallocAI(Index);
            else if (Core != null)
                lm.KickPlayer(Core);
            else
                lm.AllocAI(Index);
        }

        private void OnButton1Click()
        {

        }
    }
}
