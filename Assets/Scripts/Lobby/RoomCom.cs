using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Realtime;
using Photon.Pun;
using ExitGames.Client.Photon;
using TouhouSha.Core;

namespace Com.Morenan.TouhouSha
{
    public class RoomCom : MonoBehaviourPunCallbacks, IPunObservable
    {
        private Queue<string> commits = new Queue<string>();
        private Queue<string> receives = new Queue<string>();

        private OutputBox output;
        public OutputBox Output
        {
            get
            {
                return this.output;
            }
            set
            {
                this.output = value;
                if (output == null) return;
                if (photonView == null) return;
                if (photonView.Owner == null) return;
                lock (receives)
                    while (receives.Count() > 0)
                    {
                        string receive = receives.Dequeue();
                        Output.AppendLine(String.Format("{0}：{1}", photonView.Owner.NickName, receive));
                    }
                        
            }
        }

        void Start()
        {
            Output = GameObject.Find("OutputBox")?.GetComponent<OutputBox>();
        }

        public void Commit(string text)
        {
            lock (commits) commits.Enqueue(text);
        }

        [PunRPC]
        public void BeKick()
        {
            LobbyManager lm = Output?.gameObject.GetComponentInParent<LobbyManager>();
            PhotonNetwork.LeaveRoom();
            if (lm != null) lm.MessageBox.Show("你已被踢出房间。");
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                string commit = null;
                lock (commits) if (commits.Count() > 0) commit = commits.Dequeue();
                if (!String.IsNullOrEmpty(commit))
                {
                    stream.SendNext(1);
                    stream.SendNext(commit);
                }
            }
            else
            {
                int code = (int)stream.ReceiveNext();
                switch (code)
                {
                    case 1:
                        {
                            string receive = (string)stream.ReceiveNext();
                            if (output != null)
                                output.AppendLine(String.Format("{0}：{1}", photonView.Owner.NickName, receive));
                            else  
                                lock (receives) receives.Enqueue(receive);
                            break;
                        }
                }
            }
        }
    
    }
}
