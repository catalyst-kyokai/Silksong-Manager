using BepInEx.Configuration;
using UnityEngine;

namespace SilksongManager.Damage
{
    /// <summary>
    /// Types of damage that can be customized.
    /// </summary>
    public enum DamageType
    {
        Nail,
        Tool,
        Spell,
        Summon
    }
    
    /// <summary>
    /// Custom damage system with support for negative, zero, and fractional values.
    /// Author: Catalyst (catalyst@kyokai.ru)
    /// </summary>
    public static class DamageSystem
    {
        // Enable flags
        private static bool _customNailEnabled = false;
        private static bool _customToolEnabled = false;
        private static bool _customSpellEnabled = false;
        private static bool _customSummonEnabled = false;
        
        // Custom damage values (float for fractions, negative for healing)
        private static float _nailDamage = 5f;
        private static float _toolDamage = 10f;
        private static float _spellDamage = 15f;
        private static float _summonDamage = 8f;
        
        // Multipliers
        private static float _nailMultiplier = 1f;
        private static float _toolMultiplier = 1f;
        private static float _spellMultiplier = 1f;
        private static float _summonMultiplier = 1f;
        private static float _globalMultiplier = 1f;
        
        // Config entries
        private static ConfigEntry<bool> _customNailEnabledConfig;
        private static ConfigEntry<bool> _customToolEnabledConfig;
        private static ConfigEntry<bool> _customSpellEnabledConfig;
        private static ConfigEntry<bool> _customSummonEnabledConfig;
        private static ConfigEntry<float> _nailDamageConfig;
        private static ConfigEntry<float> _toolDamageConfig;
        private static ConfigEntry<float> _spellDamageConfig;
        private static ConfigEntry<float> _summonDamageConfig;
        private static ConfigEntry<float> _nailMultiplierConfig;
        private static ConfigEntry<float> _toolMultiplierConfig;
        private static ConfigEntry<float> _spellMultiplierConfig;
        private static ConfigEntry<float> _summonMultiplierConfig;
        private static ConfigEntry<float> _globalMultiplierConfig;
        
        #region Properties
        
        public static bool CustomNailEnabled
        {
            get => _customNailEnabled;
            set
            {
                _customNailEnabled = value;
                if (_customNailEnabledConfig != null) _customNailEnabledConfig.Value = value;
            }
        }
        
        public static bool CustomToolEnabled
        {
            get => _customToolEnabled;
            set
            {
                _customToolEnabled = value;
                if (_customToolEnabledConfig != null) _customToolEnabledConfig.Value = value;
            }
        }
        
        public static bool CustomSpellEnabled
        {
            get => _customSpellEnabled;
            set
            {
                _customSpellEnabled = value;
                if (_customSpellEnabledConfig != null) _customSpellEnabledConfig.Value = value;
            }
        }
        
        public static bool CustomSummonEnabled
        {
            get => _customSummonEnabled;
            set
            {
                _customSummonEnabled = value;
                if (_customSummonEnabledConfig != null) _customSummonEnabledConfig.Value = value;
            }
        }
        
        public static float NailDamage
        {
            get => _nailDamage;
            set
            {
                _nailDamage = value;
                if (_nailDamageConfig != null) _nailDamageConfig.Value = value;
            }
        }
        
        public static float ToolDamage
        {
            get => _toolDamage;
            set
            {
                _toolDamage = value;
                if (_toolDamageConfig != null) _toolDamageConfig.Value = value;
            }
        }
        
        public static float SpellDamage
        {
            get => _spellDamage;
            set
            {
                _spellDamage = value;
                if (_spellDamageConfig != null) _spellDamageConfig.Value = value;
            }
        }
        
        public static float SummonDamage
        {
            get => _summonDamage;
            set
            {
                _summonDamage = value;
                if (_summonDamageConfig != null) _summonDamageConfig.Value = value;
            }
        }
        
        public static float NailMultiplier
        {
            get => _nailMultiplier;
            set
            {
                _nailMultiplier = value;
                if (_nailMultiplierConfig != null) _nailMultiplierConfig.Value = value;
            }
        }
        
        public static float ToolMultiplier
        {
            get => _toolMultiplier;
            set
            {
                _toolMultiplier = value;
                if (_toolMultiplierConfig != null) _toolMultiplierConfig.Value = value;
            }
        }
        
        public static float SpellMultiplier
        {
            get => _spellMultiplier;
            set
            {
                _spellMultiplier = value;
                if (_spellMultiplierConfig != null) _spellMultiplierConfig.Value = value;
            }
        }
        
        public static float SummonMultiplier
        {
            get => _summonMultiplier;
            set
            {
                _summonMultiplier = value;
                if (_summonMultiplierConfig != null) _summonMultiplierConfig.Value = value;
            }
        }
        
        public static float GlobalMultiplier
        {
            get => _globalMultiplier;
            set
            {
                _globalMultiplier = value;
                if (_globalMultiplierConfig != null) _globalMultiplierConfig.Value = value;
            }
        }
        
        #endregion
        
        /// <summary>
        /// Initialize damage system with config.
        /// </summary>
        public static void Initialize(ConfigFile config)
        {
            // Enabled flags
            _customNailEnabledConfig = config.Bind("Damage", "CustomNailEnabled", false, "Enable custom nail damage");
            _customToolEnabledConfig = config.Bind("Damage", "CustomToolEnabled", false, "Enable custom tool damage");
            _customSpellEnabledConfig = config.Bind("Damage", "CustomSpellEnabled", false, "Enable custom spell damage");
            _customSummonEnabledConfig = config.Bind("Damage", "CustomSummonEnabled", false, "Enable custom summon damage");
            
            // Damage values
            _nailDamageConfig = config.Bind("Damage", "NailDamage", 5f, "Custom nail damage value");
            _toolDamageConfig = config.Bind("Damage", "ToolDamage", 10f, "Custom tool damage value");
            _spellDamageConfig = config.Bind("Damage", "SpellDamage", 15f, "Custom spell damage value");
            _summonDamageConfig = config.Bind("Damage", "SummonDamage", 8f, "Custom summon damage value");
            
            // Multipliers
            _nailMultiplierConfig = config.Bind("Damage", "NailMultiplier", 1f, "Nail damage multiplier");
            _toolMultiplierConfig = config.Bind("Damage", "ToolMultiplier", 1f, "Tool damage multiplier");
            _spellMultiplierConfig = config.Bind("Damage", "SpellMultiplier", 1f, "Spell damage multiplier");
            _summonMultiplierConfig = config.Bind("Damage", "SummonMultiplier", 1f, "Summon damage multiplier");
            _globalMultiplierConfig = config.Bind("Damage", "GlobalMultiplier", 1f, "Global damage multiplier");
            
            // Load values
            _customNailEnabled = _customNailEnabledConfig.Value;
            _customToolEnabled = _customToolEnabledConfig.Value;
            _customSpellEnabled = _customSpellEnabledConfig.Value;
            _customSummonEnabled = _customSummonEnabledConfig.Value;
            
            _nailDamage = _nailDamageConfig.Value;
            _toolDamage = _toolDamageConfig.Value;
            _spellDamage = _spellDamageConfig.Value;
            _summonDamage = _summonDamageConfig.Value;
            
            _nailMultiplier = _nailMultiplierConfig.Value;
            _toolMultiplier = _toolMultiplierConfig.Value;
            _spellMultiplier = _spellMultiplierConfig.Value;
            _summonMultiplier = _summonMultiplierConfig.Value;
            _globalMultiplier = _globalMultiplierConfig.Value;
            
            Plugin.Log.LogInfo("DamageSystem initialized");
        }
        
        /// <summary>
        /// Calculate final damage for given type and base damage.
        /// Returns the modified damage value (can be negative for healing).
        /// </summary>
        public static float CalculateFinalDamage(DamageType type, float baseDamage)
        {
            float damage = baseDamage;
            float multiplier = _globalMultiplier;
            
            switch (type)
            {
                case DamageType.Nail:
                    if (_customNailEnabled)
                        damage = _nailDamage;
                    multiplier *= _nailMultiplier;
                    break;
                    
                case DamageType.Tool:
                    if (_customToolEnabled)
                        damage = _toolDamage;
                    multiplier *= _toolMultiplier;
                    break;
                    
                case DamageType.Spell:
                    if (_customSpellEnabled)
                        damage = _spellDamage;
                    multiplier *= _spellMultiplier;
                    break;
                    
                case DamageType.Summon:
                    if (_customSummonEnabled)
                        damage = _summonDamage;
                    multiplier *= _summonMultiplier;
                    break;
            }
            
            return damage * multiplier;
        }
        
        /// <summary>
        /// Get custom damage value for type, or null if not enabled.
        /// </summary>
        public static float? GetCustomDamage(DamageType type)
        {
            switch (type)
            {
                case DamageType.Nail:
                    return _customNailEnabled ? _nailDamage : (float?)null;
                case DamageType.Tool:
                    return _customToolEnabled ? _toolDamage : (float?)null;
                case DamageType.Spell:
                    return _customSpellEnabled ? _spellDamage : (float?)null;
                case DamageType.Summon:
                    return _customSummonEnabled ? _summonDamage : (float?)null;
                default:
                    return null;
            }
        }
        
        /// <summary>
        /// Check if custom damage is enabled for type.
        /// </summary>
        public static bool IsCustomEnabled(DamageType type)
        {
            switch (type)
            {
                case DamageType.Nail: return _customNailEnabled;
                case DamageType.Tool: return _customToolEnabled;
                case DamageType.Spell: return _customSpellEnabled;
                case DamageType.Summon: return _customSummonEnabled;
                default: return false;
            }
        }
        
        /// <summary>
        /// Toggle custom damage for type.
        /// </summary>
        public static void ToggleCustomDamage(DamageType type)
        {
            switch (type)
            {
                case DamageType.Nail:
                    CustomNailEnabled = !_customNailEnabled;
                    break;
                case DamageType.Tool:
                    CustomToolEnabled = !_customToolEnabled;
                    break;
                case DamageType.Spell:
                    CustomSpellEnabled = !_customSpellEnabled;
                    break;
                case DamageType.Summon:
                    CustomSummonEnabled = !_customSummonEnabled;
                    break;
            }
        }
        
        /// <summary>
        /// Set custom damage value for type.
        /// </summary>
        public static void SetDamage(DamageType type, float value)
        {
            switch (type)
            {
                case DamageType.Nail: NailDamage = value; break;
                case DamageType.Tool: ToolDamage = value; break;
                case DamageType.Spell: SpellDamage = value; break;
                case DamageType.Summon: SummonDamage = value; break;
            }
        }
        
        /// <summary>
        /// Get damage value for type.
        /// </summary>
        public static float GetDamage(DamageType type)
        {
            switch (type)
            {
                case DamageType.Nail: return _nailDamage;
                case DamageType.Tool: return _toolDamage;
                case DamageType.Spell: return _spellDamage;
                case DamageType.Summon: return _summonDamage;
                default: return 0;
            }
        }
        
        /// <summary>
        /// Adjust damage value by delta.
        /// </summary>
        public static void AdjustDamage(DamageType type, float delta)
        {
            SetDamage(type, GetDamage(type) + delta);
        }
        
        /// <summary>
        /// Set multiplier for type.
        /// </summary>
        public static void SetMultiplier(DamageType type, float value)
        {
            switch (type)
            {
                case DamageType.Nail: NailMultiplier = value; break;
                case DamageType.Tool: ToolMultiplier = value; break;
                case DamageType.Spell: SpellMultiplier = value; break;
                case DamageType.Summon: SummonMultiplier = value; break;
            }
        }
        
        /// <summary>
        /// Get multiplier for type.
        /// </summary>
        public static float GetMultiplier(DamageType type)
        {
            switch (type)
            {
                case DamageType.Nail: return _nailMultiplier;
                case DamageType.Tool: return _toolMultiplier;
                case DamageType.Spell: return _spellMultiplier;
                case DamageType.Summon: return _summonMultiplier;
                default: return 1f;
            }
        }
    }
}
