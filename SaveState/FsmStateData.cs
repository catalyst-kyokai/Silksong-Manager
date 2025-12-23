using System.Collections.Generic;
using UnityEngine;

namespace SilksongManager.SaveState
{
    /// <summary>
    /// Represents the captured state of a PlayMaker FSM (Finite State Machine).
    /// Used to restore enemy AI behavior and game logic state.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    [System.Serializable]
    public class FsmStateData
    {
        /// <summary>Name of the FSM component (e.g., "Control", "Attack").</summary>
        public string FsmName;
        /// <summary>Name of the currently active state in the FSM.</summary>
        public string ActiveStateName;

        /// <summary>Captured boolean FSM variables (key: variable name, value: variable value).</summary>
        public Dictionary<string, bool> BoolVariables = new Dictionary<string, bool>();
        /// <summary>Captured integer FSM variables.</summary>
        public Dictionary<string, int> IntVariables = new Dictionary<string, int>();
        /// <summary>Captured float FSM variables.</summary>
        public Dictionary<string, float> FloatVariables = new Dictionary<string, float>();
        /// <summary>Captured string FSM variables.</summary>
        public Dictionary<string, string> StringVariables = new Dictionary<string, string>();
        /// <summary>Captured Vector3 FSM variables.</summary>
        public Dictionary<string, Vector3> Vector3Variables = new Dictionary<string, Vector3>();
    }
}
