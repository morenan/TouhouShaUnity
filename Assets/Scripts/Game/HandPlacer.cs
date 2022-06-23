using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TouhouSha.Core;
using UnityEngine;
using UnityEngine.UI;

public class HandPlacer : MonoBehaviour, IGameBoardArea
{
    bool IGameBoardArea.KeptCards
    {
        get
        {
            return true;
        }
    }

    private List<Card> areacards = new List<Card>();
    IList<Card> IGameBoardArea.Cards
    {
        get
        {
            return areacards;
        }
    }

    Vector3? IGameBoardArea.GetExpectedPosition(Card card)
    {
        RectTransform rt = gameObject.GetComponent<RectTransform>();
        float aw = rt.rect.width;
        float ah = rt.rect.height;
        float uw = CardBe.DefaultWidth;
        float uh = CardBe.DefaultHeight;
        float uw0 = uw;
        int index = areacards.IndexOf(card);
        if (index < 0) return new Vector3(
            rt.position.x + uw / 2,
            rt.position.y);
        if (areacards.Count() > 1)
            uw = Math.Min(uw, (aw - uw) / (areacards.Count() - 1));
        return new Vector3(
            rt.position.x + uw0 * 0.5f + uw * index,
            rt.position.y,
            index);
    }
}
