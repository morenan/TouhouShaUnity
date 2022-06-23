using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;
using TouhouSha.Core.UIs;

public class KOFCharactorList : MonoBehaviour
{
    private Player player;
    public Player Player
    {
        get
        {
            return this.player;
        }
        set
        {
            this.player = value;
            UpdateList();
        }
    }

    public Image[] Faces;
    public Text[] Names;

    public void UpdateList()
    {
        if (player == null) return;
        for (int i = 0; i < player.KOFList.Count(); i++)
        {
            Charactor kofchar = player.KOFList[i];
            Image face = Faces.Length > i ? Faces[i] : null;
            Text name = Names.Length > i ? Names[i] : null;
            if (face != null)
            {
                CharactorImageContext ctx = App.GetImageContext(kofchar);
                Sprite sprite = ImageHelper.CreateSprite(kofchar);
                RectTransform face_rt = face.gameObject.GetComponent<RectTransform>();
                float imageheight = sprite.rect.height / sprite.rect.width * face_rt.rect.width;
                face.sprite = ImageHelper.CreateSprite(kofchar);
                face_rt.localPosition = new Vector3(0, imageheight * (ctx.FacePoint.y - 0.5f));
                Color color = face.color;
                if (i < player.KOFAliveIndex)
                    color.r = color.g = color.b = 0.5f;
                else
                    color.r = color.g = color.b = 1.0f;
                face.color = color;
            }
            if (name != null)
            {
                name.text = kofchar.GetInfo().Name;
                Color color = name.color;
                if (i < player.KOFAliveIndex)
                    color.r = color.g = color.b = 0.5f;
                else
                    color.r = color.g = color.b = 1.0f;
                name.color = color;
            }
        }
    }
}

