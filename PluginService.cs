using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BhModule.PathingMapAlignPlugin
{
    public class PluginService
    {
        const string _pathingNamespace = "bh.community.pathing";
        Logger Logger => PathingMapAlignPluginModule.Logger;
        ModuleManager _pathingModuleManager;
        readonly List<Action> _hookDisposeActions = [];
        bool _error = false;
        int _mapWidth_max;
        bool DependenciesMet => PathingMapAlignPluginModule.InstanceManager.DependenciesMet;
        public void Upadate()
        {
            if (!DependenciesMet || _error) return;
            if (_pathingModuleManager is null)
            {
                try
                {
                    GetPathingModuleManager();
                    HookFlatMap();
                    HookRenderToMiniMap();
                }
                catch (Exception ex)
                {
                    _error = true;
                    OnPathingUnload(this, EventArgs.Empty);
                    LogError(ex);
                }
            }
        }
        public void Unload()
        {
            if (_pathingModuleManager is null) return;
            _pathingModuleManager.ModuleDisabled -= OnPathingUnload;
            OnPathingUnload(this, EventArgs.Empty);
        }
        void OnPathingUnload(object sender, EventArgs e)
        {
            foreach (var dispose in _hookDisposeActions)
            {
                dispose();
            }
            _hookDisposeActions.Clear();
        }
        void GetPathingModuleManager()
        {
            _pathingModuleManager = GameService.Module.Modules.FirstOrDefault(m => m.Manifest.Namespace == _pathingNamespace);
            _pathingModuleManager.ModuleDisabled += OnPathingUnload;
        }
        void HookFlatMap()
        {
            var pathingAssembly = Assembly.GetAssembly(_pathingModuleManager.ModuleInstance.GetType());
            var flatMapType = pathingAssembly.GetType("BhModule.Community.Pathing.Entity.FlatMap");
            _mapWidth_max = (int)flatMapType.GetField("MAPWIDTH_MAX", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

            var getOffsetMethodInfo = flatMapType.GetMethod("GetOffset", BindingFlags.NonPublic | BindingFlags.Instance);
            var hook1 = new Hook(getOffsetMethodInfo, GetOffset);
            _hookDisposeActions.Add(() => hook1.Dispose());

            var updateBoundsMethodInfo = flatMapType.GetMethod("UpdateBounds", BindingFlags.NonPublic | BindingFlags.Instance);
            var hook2 = new Hook(updateBoundsMethodInfo, UpdateBounds);
            _hookDisposeActions.Add(() => hook2.Dispose());
        }
        void HookRenderToMiniMap()
        {
            var pathingAssembly = Assembly.GetAssembly(_pathingModuleManager.ModuleInstance.GetType());

            var pathingMarkerType = pathingAssembly.GetType("BhModule.Community.Pathing.Entity.StandardMarker");
            var RenderToMiniMapMethodInfo1 = pathingMarkerType.GetMethod("RenderToMiniMap", BindingFlags.Public | BindingFlags.Instance);
            var hook1 = new Hook(RenderToMiniMapMethodInfo1, RenderToMiniMap);
            _hookDisposeActions.Add(() => hook1.Dispose());

            var pathingTrailType = pathingAssembly.GetType("BhModule.Community.Pathing.Entity.StandardTrail");
            var RenderToMiniMapMethodInfo2 = pathingTrailType.GetMethod("RenderToMiniMap", BindingFlags.Public | BindingFlags.Instance);
            var hook2 = new Hook(RenderToMiniMapMethodInfo2, RenderToMiniMap);
            _hookDisposeActions.Add(() => hook2.Dispose());
        }
        void LogError(Exception ex)
        {
            Logger.Error(ex.Message + "\n" + ex.StackTrace);
        }
        int GetOffset(Func<object, float, float, float, float, int> originFunc, object instance, float curr, float max, float min, float val)
        {
            var settings = PathingMapAlignPluginModule.Instance.Settings;
            var isWidth = max == _mapWidth_max;
            var offset = isWidth ? settings.Width.Value : settings.Height.Value;
            return originFunc(instance, curr, max, min, val) + offset;
        }
        void UpdateBounds(Action<Control> originFunc, Control instance)
        {
            originFunc(instance);
            var settings = PathingMapAlignPluginModule.Instance.Settings;
            var mumbleUI = GameService.Gw2Mumble.UI;
            var compassW = mumbleUI.CompassSize.Width;
            var compassH = mumbleUI.CompassSize.Height;
            if (compassW < 1 || compassH < 1 || mumbleUI.IsMapOpen) return;
            if (mumbleUI.IsCompassTopRight)
            {
                instance.Location = new(instance.Location.X + settings.X.Value, instance.Location.Y);
            }
            else
            {
                instance.Location = new(instance.Location.X + settings.X.Value, instance.Location.Y + settings.Y.Value);
            }
        }
        RectangleF? RenderToMiniMap(Func<object, SpriteBatch, Rectangle, double, double, double, float, RectangleF?> originFunc, object instance, SpriteBatch spriteBatch, Rectangle bounds, double offsetX, double offsetY, double scale, float opacity)
        {
            var settings = PathingMapAlignPluginModule.Instance.Settings;
            return originFunc(instance, spriteBatch, bounds, offsetX, offsetY, scale * settings.Scale.Value, opacity);
        }
    }
}
