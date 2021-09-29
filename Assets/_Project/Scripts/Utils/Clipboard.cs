// C#
// ClipboardHelper.cs
using UnityEngine;
using System;
using System.Reflection;

[System.Serializable]
public class ClipboardHelper {
    public static string clipBoard {
        get {
            return GUIUtility.systemCopyBuffer;
        }
        set {
            GUIUtility.systemCopyBuffer = value;
        }
    }
}