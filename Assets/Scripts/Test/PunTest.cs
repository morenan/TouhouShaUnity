using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Photon.Pun;
using Photon.Realtime;

public class PunTest : MonoBehaviourPunCallbacks
{
    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();    
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        
        
    }
    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
    }

}
