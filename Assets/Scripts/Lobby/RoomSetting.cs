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
    public class RoomSetting : MonoBehaviour
    {
        static public string AnyModeText = "任意模式";
        static public string StandardPlayers5 = "五人标准";
        static public string StandardPlayers8 = "八人标准";
        static public string KOF = "KOF模式";
        static public string FightLandlord = "斗地主";
        static public string Players2v2 = "2 vs 2";
        static public string Players3v3 = "3 vs 3";

        static public Enum_GameMode ParseGameMode(string text)
        {
            if (text?.Equals(StandardPlayers5) == true)
                return Enum_GameMode.StandardPlayers5;
            if (text?.Equals(StandardPlayers8) == true)
                return Enum_GameMode.StandardPlayers8;
            if (text?.Equals(KOF) == true)
                return Enum_GameMode.KOF;
            if (text?.Equals(FightLandlord) == true)
                return Enum_GameMode.FightLandlord;
            if (text?.Equals(Players2v2) == true)
                return Enum_GameMode.Players2v2;
            if (text?.Equals(Players3v3) == true)
                return Enum_GameMode.Players3v3;
            return default(Enum_GameMode);
        } 

        static public string GameModeToString(Enum_GameMode gamemode)
        {
            switch (gamemode)
            {
                case Enum_GameMode.StandardPlayers5: return StandardPlayers5;
                case Enum_GameMode.StandardPlayers8: return StandardPlayers8;
                case Enum_GameMode.KOF: return KOF;
                case Enum_GameMode.FightLandlord: return FightLandlord;
                case Enum_GameMode.Players2v2: return Players2v2;
                case Enum_GameMode.Players3v3: return Players3v3;
            }
            return "Unknowned";
        }

        public bool AnyMode = true;
        public RoomSettingMode SettingMode = RoomSettingMode.RandomJoin;
        public Enum_GameMode GameMode = Enum_GameMode.StandardPlayers8;
        public Dropdown GameModeList;
        public InputField Name;
        public InputField Description;
        public Button Ensure;
        public Button Cancel;

        private bool _ignore_gamemode;

        void Awake()
        {
            Ensure.onClick.AddListener(OnEnsure);
            Cancel.onClick.AddListener(OnCancel);
        }

        public void Show(RoomSettingMode settingmode)
        {
            List<string> gamemodetexts = new List<string>();
            string gamemodetext = AnyModeText;
            int gamemodeindex = 0;

            SettingMode = settingmode;
            if (SettingMode == RoomSettingMode.RandomJoin && AnyMode)
                gamemodetext = AnyModeText;
            else
                gamemodetext = GameModeToString(GameMode);
            if (SettingMode == RoomSettingMode.RandomJoin)
                gamemodetexts.Add(AnyModeText);
            foreach (Enum_GameMode gamemode in Enum.GetValues(typeof(Enum_GameMode)))
                gamemodetexts.Add(GameModeToString(gamemode));
            gamemodeindex = gamemodetexts.IndexOf(gamemodetext);
            if (gamemodeindex < 0)
                gamemodeindex = 0;

            _ignore_gamemode = true;
            GameModeList.ClearOptions();
            GameModeList.AddOptions(gamemodetexts);
            GameModeList.value = gamemodeindex;
            _ignore_gamemode = false;
            if (SettingMode == RoomSettingMode.RandomJoin)
                App.Hide(Cancel);
            else
                App.Show(Cancel);
            App.Show(this);
        }
        

        private void OnEnsure()
        {
            if (GameModeList.captionText.text?.Equals(AnyModeText) == true)
            {
                AnyMode = true;
            }
            else
            {
                AnyMode = false;
                GameMode = ParseGameMode(GameModeList.captionText.text);
            }
            if (SettingMode == RoomSettingMode.RandomJoin) return;
            LobbyManager lm = gameObject.GetComponentInParent<LobbyManager>();
            PhotonNetwork.LocalPlayer.NickName = App.NickName = lm.PlayerNickName.text;
            lm?.CreateRoom(GameMode, Name.text, Description.text);
            App.Hide(this);
        }

        private void OnCancel()
        {
            App.Hide(this);
        }
    }
    
    public enum RoomSettingMode
    {
        RandomJoin,
        Create,
        Modify,
    }
}


