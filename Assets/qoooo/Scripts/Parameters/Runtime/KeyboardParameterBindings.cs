using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace qoooo.Parameters.Runtime
{
    public enum KeyboardBindingAction
    {
        Toggle,
        SetValue,
        SetOption,
        NextState,
        ToggleStep,
        Trigger
    }

    [Serializable]
    public sealed class KeyboardParameterBinding
    {
        public string ParameterId;
        public Key Key;
        public bool Shift;
        public KeyboardBindingAction Action;
        public float Value;
        public int OptionIndex;
    }

    [Serializable]
    public sealed class KeyboardBindingData
    {
        public int SchemaVersion = 1;
        public string ProfileId = "trip26";
        public bool HasSavedData;
        public List<KeyboardParameterBinding> Bindings = new();
    }
}