using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TouhouSha.Core;
using UnityEngine;
using UnityEngine.UI;

public class DrawPlacer : MonoBehaviour, IGameBoardArea
{
    bool IGameBoardArea.KeptCards
    {
        get
        {
            return false;
        }
    }

    IList<Card> IGameBoardArea.Cards
    {
        get
        {
            return new List<Card>();
        }
    }

    Vector3? IGameBoardArea.GetExpectedPosition(Card card)
    {
        return gameObject.transform.position;
    }
}
