﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[KSPAddonFixed(KSPAddon.Startup.MainMenu, true, typeof(HyperEditModule))]
public class HyperEditModule : MonoBehaviour
{
    public HyperEditModule()
    {
        HyperEdit.Immortal.AddImmortal<HyperEdit.HyperEditBehaviour>();
        HyperEdit.Model.PlanetEditor.ApplyFileDefaults();
    }
}

namespace HyperEdit
{
    public delegate bool TryParse<T>(string str,out T value);

    public static class Immortal
    {
        private static GameObject _gameObject;

        public static T AddImmortal<T>() where T : Component
        {
            if (_gameObject == null)
            {
                _gameObject = new GameObject("HyperEditImmortal", typeof(T));
                UnityEngine.Object.DontDestroyOnLoad(_gameObject);
            }
            return _gameObject.GetComponent<T>() ?? _gameObject.AddComponent<T>();
        }
    }

    public class HyperEditBehaviour : MonoBehaviour
    {
        ApplicationLauncherButton _appLauncherButton;
		Action createCoreView = null;

		private void CreateCoreView()
		{
			if (createCoreView == null)
			{
				createCoreView = View.CoreView.Create();
			}
			createCoreView();
		}

        public void Awake()
        {
            GameEvents.onGUIApplicationLauncherReady.Add(AddAppLauncher);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(RemoveAppLauncher);
        }

        private void AddAppLauncher()
        {
            if (_appLauncherButton != null)
            {
                Extentions.Log("Not adding to ApplicationLauncher, button already exists (yet onGUIApplicationLauncherReady was called?)");
                return;
            }
            var applauncher = ApplicationLauncher.Instance;
            if (applauncher == null)
            {
                Extentions.Log("Cannot add to ApplicationLauncher, instance was null");
                return;
            }
            const ApplicationLauncher.AppScenes scenes =
                ApplicationLauncher.AppScenes.FLIGHT |
                ApplicationLauncher.AppScenes.MAPVIEW |
                ApplicationLauncher.AppScenes.TRACKSTATION;
            var tex = new Texture2D(38, 38, TextureFormat.RGBA32, false);

            for (var x = 0; x < tex.width; x++)
                for (var y = 0; y < tex.height; y++)
                    tex.SetPixel(x, y, new Color(2 * (float)Math.Abs(x - tex.width / 2) / tex.width, 0.25f, 2 * (float)Math.Abs(y - tex.height / 2) / tex.height, 0));
            for (var x = 10; x < 12; x++)
                for (var y = 10; y < tex.height - 10; y++)
                    tex.SetPixel(x, y, new Color(1, 1, 1));
            for (var x = tex.width - 12; x < tex.width - 10; x++)
                for (var y = 10; y < tex.height - 10; y++)
                    tex.SetPixel(x, y, new Color(1, 1, 1));
            for (var x = 12; x < tex.width - 12; x++)
                for (var y = tex.height / 2; y < tex.height / 2 + 2; y++)
                    tex.SetPixel(x, y, new Color(1, 1, 1));

            tex.Apply();
            _appLauncherButton = applauncher.AddModApplication(() =>
                {
					CreateCoreView();
                    _appLauncherButton.SetFalse();
                }, () =>
                {
                }, () =>
                {
                }, () =>
                {
                }, () =>
                {
                }, () =>
                {
                }, scenes, tex);
        }

        private void RemoveAppLauncher()
        {
            var applauncher = ApplicationLauncher.Instance;
            if (applauncher == null)
            {
                Extentions.Log("Cannot remove from ApplicationLauncher, instance was null");
                return;
            }
            if (_appLauncherButton == null)
            {
                return;
            }
            applauncher.RemoveModApplication(_appLauncherButton);
            _appLauncherButton = null;
        }

        public void Update()
        {
            if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetKeyDown(KeyCode.H))
				CreateCoreView();
        }
    }

    public static class Extentions
    {
        public static void Log(string message)
        {
            Debug.Log("HyperEdit: " + message);
        }

        public static void TryGetValue<T>(this ConfigNode node, string key, ref T value, TryParse<T> tryParse)
        {
            var strvalue = node.GetValue(key);
            if (strvalue == null)
                return;
            T temp;
            if (tryParse(strvalue, out temp) == false)
                return;
            value = temp;
        }

        public static void ErrorPopup(string message)
        {
            PopupDialog.SpawnPopupDialog("Error", message, "Close", true, HighLogic.Skin);
        }

        private static GUIStyle _pressedButton;

        public static GUIStyle PressedButton
        {
            get
            {
                return _pressedButton ?? (_pressedButton = new GUIStyle(HighLogic.Skin.button)
                {
                    normal = HighLogic.Skin.button.active,
                    hover = HighLogic.Skin.button.active,
                    active = HighLogic.Skin.button.normal
                });
            }
        }

        public static double Soi(this CelestialBody body)
        {
            var radius = body.sphereOfInfluence * 0.95;
            if (double.IsNaN(radius) || double.IsInfinity(radius) || radius < 0 || radius > 200000000000)
                radius = 200000000000; // jool apo = 72,212,238,387
            return radius;
        }

        private static Dictionary<string, KeyCode> _keyCodeNames;

        public static Dictionary<string, KeyCode> KeyCodeNames
        {
            get
            {
                return _keyCodeNames ?? (_keyCodeNames =
                    Enum.GetNames(typeof(KeyCode)).Distinct().ToDictionary(k => k, k => (KeyCode)Enum.Parse(typeof(KeyCode), k)));
            }
        }

        public static bool KeyCodeTryParse(string str, out KeyCode[] value)
        {
            var split = str.Split('-', '+');
            if (split.Length == 0)
            {
                value = null;
                return false;
            }
            value = new KeyCode[split.Length];
            for (int i = 0; i < split.Length; i++)
            {
                if (KeyCodeNames.TryGetValue(split[i], out value[i]) == false)
                {
                    return false;
                }
            }
            return true;
        }

        public static string KeyCodeToString(this KeyCode[] values)
        {
            return string.Join("-", values.Select(v => v.ToString()).ToArray());
        }

        public static string OrbitDriverToString(this OrbitDriver driver)
        {
            if (driver == null)
                return null;
            var body = FlightGlobals.Bodies.FirstOrDefault(cb => cb.orbitDriver != null && cb.orbitDriver == driver);
            if (body != null)
                return body.bodyName;
            var vessel = FlightGlobals.Vessels.FirstOrDefault(v => v.orbitDriver != null && v.orbitDriver == driver);
            if (vessel != null)
                return vessel == FlightGlobals.ActiveVessel ? "Active vessel" : vessel.vesselName;
            if (string.IsNullOrEmpty(driver.name) == false)
                return driver.name;
            return "Unknown";
        }

        public static string CbToString(this CelestialBody body)
        {
            return body.bodyName;
        }

        public static bool CbTryParse(string bodyName, out CelestialBody body)
        {
            body = FlightGlobals.Bodies == null ? null : FlightGlobals.Bodies.FirstOrDefault(cb => cb.name == bodyName);
            return body != null;
        }

        private static string TrimUnityColor(string value)
        {
            value = value.Trim();
            if (value.StartsWith("RGBA", StringComparison.OrdinalIgnoreCase))
                value = value.Substring(4).Trim();
            value = value.Trim('(', ')');
            return value;
        }

        public static bool ColorTryParse(string value, out Color color)
        {
            color = new Color();
            string parseValue = TrimUnityColor(value);
            if (parseValue == null)
                return false;
            string[] values = parseValue.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (values.Length == 3 || values.Length == 4)
            {
                if (!float.TryParse(values[0], out color.r) ||
                    !float.TryParse(values[1], out color.g) ||
                    !float.TryParse(values[2], out color.b))
                    return false;
                if (values.Length == 3 && !float.TryParse(values[3], out color.a))
                    return false;
                return true;
            }
            return false;
        }
    }
}

// Credit to "Majiir" for "KSPAddonFixed" : KSPAddon with equality checking using an additional type parameter. Fixes the issue where AddonLoader prevents multiple start-once addons with the same start scene.
public class KSPAddonFixed : KSPAddon, IEquatable<KSPAddonFixed>
{
    private readonly Type type;

    public KSPAddonFixed(KSPAddon.Startup startup, bool once, Type type)
        : base(startup, once)
    {
        this.type = type;
    }

    public override bool Equals(object obj)
    {
        var other = obj as KSPAddonFixed;
        return other != null && Equals(other);
    }

    public bool Equals(KSPAddonFixed other)
    {
        return once == other.once && startup == other.startup && type == other.type;
    }

    public override int GetHashCode()
    {
        return startup.GetHashCode() ^ once.GetHashCode() ^ type.GetHashCode();
    }
}
