using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class AnimationBe : MonoBehaviour
{
    public string Name;

    void Update()
    {
        Animator animator = gameObject.GetComponent<Animator>();
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);  
        if (info.normalizedTime >= 1.0f)
        {
            GameBoard gb = gameObject.GetComponentInParent<GameBoard>();
            gb?.Hide(this);
        }
    }

    public void Play()
    {
        Animator animator = gameObject.GetComponent<Animator>();
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        animator.Update(-info.normalizedTime);
    }

}
