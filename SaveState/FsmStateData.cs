using System.Collections.Generic;
using UnityEngine;

namespace SilksongManager.SaveState
{
    [System.Serializable]
    public class FsmStateData
    {
        public string FsmName;
        public string ActiveStateName;

        // Serialized variables (primitives only for now)
        public Dictionary<string, bool> BoolVariables = new Dictionary<string, bool>();
        public Dictionary<string, int> IntVariables = new Dictionary<string, int>();
        public Dictionary<string, float> FloatVariables = new Dictionary<string, float>();
        public Dictionary<string, string> StringVariables = new Dictionary<string, string>();
        // Vector3 is a struct, so it's serializable, but might need helper if using JSON. 
        // Unity's JSONUtility handles Vector3 fine usually, but Newtonsoft might need a wrapper.
        // Assuming we rely on standard serialization or Unity's.
        public Dictionary<string, Vector3> Vector3Variables = new Dictionary<string, Vector3>();
    }
}
