using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RecordedInputAsset", menuName = "Recording/RecordedInputs", order = -1)]
public class RecordedInputAsset : ScriptableObject {
    public List<InputSnapshot> inputSnapshots;

}
