﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Il codice è stato generato da uno strumento.
//     Versione runtime:4.0.30319.42000
//
//     Le modifiche apportate a questo file possono provocare un comportamento non corretto e andranno perse se
//     il codice viene rigenerato.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CustomMediaPlayerUltimate.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.4.0.0")]
    public sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("50")]
        public double PlayerVolume {
            get {
                return ((double)(this["PlayerVolume"]));
            }
            set {
                this["PlayerVolume"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool PlayerLoop {
            get {
                return ((bool)(this["PlayerLoop"]));
            }
            set {
                this["PlayerLoop"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool PlayerShuffle {
            get {
                return ((bool)(this["PlayerShuffle"]));
            }
            set {
                this["PlayerShuffle"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool UseSearchResultsAsShuffleSource {
            get {
                return ((bool)(this["UseSearchResultsAsShuffleSource"]));
            }
            set {
                this["UseSearchResultsAsShuffleSource"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("59")]
        public short PlayPauseShortcutKey {
            get {
                return ((short)(this["PlayPauseShortcutKey"]));
            }
            set {
                this["PlayPauseShortcutKey"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("24")]
        public short IncreaseVolumeShortcutKey {
            get {
                return ((short)(this["IncreaseVolumeShortcutKey"]));
            }
            set {
                this["IncreaseVolumeShortcutKey"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("26")]
        public short DecreaseVolumeShortcutKey {
            get {
                return ((short)(this["DecreaseVolumeShortcutKey"]));
            }
            set {
                this["DecreaseVolumeShortcutKey"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("57")]
        public short NextSongShortcutKey {
            get {
                return ((short)(this["NextSongShortcutKey"]));
            }
            set {
                this["NextSongShortcutKey"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("45")]
        public short PreviousSongShortcutKey {
            get {
                return ((short)(this["PreviousSongShortcutKey"]));
            }
            set {
                this["PreviousSongShortcutKey"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("61")]
        public short ToggleLoopShortcutKey {
            get {
                return ((short)(this["ToggleLoopShortcutKey"]));
            }
            set {
                this["ToggleLoopShortcutKey"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("62")]
        public short ToggleShuffleShortcutKey {
            get {
                return ((short)(this["ToggleShuffleShortcutKey"]));
            }
            set {
                this["ToggleShuffleShortcutKey"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool PlaybackFadeIn {
            get {
                return ((bool)(this["PlaybackFadeIn"]));
            }
            set {
                this["PlaybackFadeIn"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool PlaybackFadeOut {
            get {
                return ((bool)(this["PlaybackFadeOut"]));
            }
            set {
                this["PlaybackFadeOut"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::System.Collections.Specialized.StringCollection SourceDirectories {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["SourceDirectories"]));
            }
            set {
                this["SourceDirectories"] = value;
            }
        }
    }
}
