using UnityEngine;

public class UnderTheRope : StateMachineBehaviour
{
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    //override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stateInfo.normalizedTime < 0.3f)
        {
            animator.transform.position += Vector3.forward * Time.deltaTime * 0.1f;
        }
        else if (stateInfo.normalizedTime < 0.4f)
        {
            animator.transform.position += Vector3.forward * Time.deltaTime * 0.3f;

        }
        else if (stateInfo.normalizedTime < 0.6f)
        {
            animator.transform.position += Vector3.forward * Time.deltaTime * 0.4f;
        }
        else if (stateInfo.normalizedTime < 1.0f)
        {
            animator.transform.position += Vector3.forward * Time.deltaTime * 0.3f;
        }


    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 残っているトリガーをリフレッシュする
        animator.ResetTrigger("UnderTheRope");
    }

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}

}
