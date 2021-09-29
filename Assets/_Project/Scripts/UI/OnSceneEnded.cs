using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnSceneEnded : StateMachineBehaviour {
    
    public MenuScenes sceneGettingClosed;

    // OnStateExit is called before OnStateExit is called on any state inside this state machine
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        MenuManager.inst.OnSceneExitedCallback(sceneGettingClosed);
    }
}
