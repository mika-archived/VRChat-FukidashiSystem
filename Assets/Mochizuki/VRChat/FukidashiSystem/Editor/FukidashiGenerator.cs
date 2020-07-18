/*-------------------------------------------------------------------------------------------
 * Copyright (c) Fuyuno Mikazuki / Natsuneko. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 *------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEditor.Animations;

using UnityEngine;

using VRC.SDK3.Components;
using VRC.SDK3.ScriptableObjects;

using Object = UnityEngine.Object;

namespace Mochizuki.VRChat.FukidashiSystem
{
    public class FukidashiGenerator : EditorWindow
    {
        private const string AnimatorControllerGuid = "43179b4d0bbbb064dbb0e32e47d1e556";
        private const string DefaultExpressionMenuGuid = "697fad417fb68814b98a74442310d8b3";
        private const string FirstPageExpressionMenuGuid = "38198afe64b58ff4ea540a72280b5c7a";
        private const string IconTextureGuid = "ae87f67e98a20294082a139386f57b45";
        private const string QuadPrefabGuid = "24493fbf74952924da5e7590ff84f993";
        private const string StageParametersId = "Mochizuki_FukidashiSystem";

        private VRCAvatarDescriptor _avatar;
        private bool _isAppendToExistsExpression;
        private bool _isOverrideFxLayer;
        private GameObject _parent;
        private GameObject _prefab;

        [MenuItem("Mochizuki/VRChat/Fukidashi Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<FukidashiGenerator>();
            window.titleContent = new GUIContent("Fukidashi Generator");

            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            GUILayout.Label("Mochizuki.FukidashiSystem Version 0.1 (SDK-AVATARS 3.0)");
            EditorGUILayout.Space();

            EditorGUIUtility.labelWidth = 350;

            _avatar = ObjectPicker("VRC Avatar", _avatar);
            _parent = ObjectPicker("Message Board Parent", _parent);
            _prefab = ReadonlyObjectPicker("Message Board Prefab", GetObjectFromGuid<GameObject>(QuadPrefabGuid));

            if (_avatar != null)
            {
                _isAppendToExistsExpression = EditorGUILayout.Toggle("Append expression and state parameters to exists one?", _isAppendToExistsExpression);
                _isOverrideFxLayer = EditorGUILayout.Toggle("Override animator controller when already exists?", _isOverrideFxLayer);
            }

            EditorGUI.BeginDisabledGroup(_avatar == null || _parent == null);

            if (GUILayout.Button("Generate Animations and Setup Avatar") && !OnSubmit())
                GUILayout.Label("Failed to generate or setup FukidashiSystem for your avatar. Please check Console Logs and report it to developer.");

            EditorGUI.EndDisabledGroup();
        }

        private bool OnSubmit()
        {
            var stage = ConfigureStageParameters();
            if (string.IsNullOrWhiteSpace(stage))
                return false;

            var animations = GenerateAnimations();
            if (animations.Count == 0)
                return false;

            var controller = GenerateAnimatorController(animations, stage);
            if (controller == null)
                return false;

            return SetupAvatar(controller);
        }

        private string ConfigureStageParameters()
        {
            if (_avatar.expressionsMenu == null)
                return "Stage16"; // use default

            var parameters = _avatar.expressionsMenu.stageParameters;
            var blank = parameters.stageParameters.Select((w, i) => new { Value = w, Index = i }).FirstOrDefault(w => w.Value.name == StageParametersId || string.IsNullOrWhiteSpace(w.Value.name));
            return blank == null ? null : $"Stage{blank.Index + 1}";
        }

        private List<AnimationClip> GenerateAnimations()
        {
            var animations = new List<AnimationClip>();
            var baseDir = GetSaveFolderPath("Save Animations to folder...", "Animations");
            if (string.IsNullOrWhiteSpace(baseDir))
                return animations;

            var path = GetPathBetweenGameObjects(_avatar.gameObject, _parent);

            for (var i = 0; i <= 16; i++)
            {
                var animation = new AnimationClip();

                AnimationUtility.SetEditorCurve(animation, EditorCurveBinding.FloatCurve($"{path}/MessageBoard", typeof(MeshRenderer), "material._TextureNo"), AnimationCurve.Linear(0, i, 1 / 60f, i));
                AnimationUtility.SetEditorCurve(animation, EditorCurveBinding.FloatCurve($"{path}/MessageBoard", typeof(GameObject), "m_IsActive"), AnimationCurve.Linear(0, i == 0 ? 0 : 1, 1 / 60f, i == 0 ? 0 : 1));

                AssetDatabase.CreateAsset(animation, $"{baseDir}/SwitchTo{i}.anim");
                animations.Add(animation);
            }

            return animations;
        }

        private AnimatorController GenerateAnimatorController(IReadOnlyList<AnimationClip> animations, string stage)
        {
            var dest = GetSaveFilePath("Save AnimatorController to file...", "FukidashiSystem_FX", "controller");
            if (string.IsNullOrWhiteSpace(dest))
                return null;

            AssetDatabase.CopyAsset(AssetDatabase.GUIDToAssetPath(AnimatorControllerGuid), dest);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(dest);

            var sm = controller.layers[0].stateMachine;
            foreach (var state in sm.states)
            {
                if (!state.state.name.StartsWith("SwitchTo"))
                {
                    // maybe Default (Blank)
                    state.state.motion = animations.Last();
                    continue;
                }

                var r = int.TryParse(state.state.name.Replace("SwitchTo", ""), out var i);
                if (!r)
                    return null;

                state.state.motion = animations[i];
            }

            controller.RemoveParameter(0);
            controller.AddParameter(stage, AnimatorControllerParameterType.Int);

            foreach (var transition in sm.anyStateTransitions)
            {
                if (transition.conditions.Length == 0)
                    continue;

                var condition = transition.conditions[0];
                transition.RemoveCondition(condition);
                transition.AddCondition(condition.mode, condition.threshold, stage);
            }

            return controller;
        }

        private bool SetupAvatar(RuntimeAnimatorController controller)
        {
            SetupFxAnimationLayer(controller);
            SetupExpressions();

            var prefab = (GameObject) PrefabUtility.InstantiatePrefab(_prefab);
            PrefabUtility.UnpackPrefabInstance(prefab, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            prefab.transform.rotation = Quaternion.Euler(0, 180, 0);
            prefab.transform.parent = _parent.transform;

            return true;
        }

        private void SetupFxAnimationLayer(RuntimeAnimatorController controller)
        {
            if (!_avatar.customizeAnimationLayers)
                _avatar.customizeAnimationLayers = true;

            var (fx, index) = _avatar.baseAnimationLayers.Select((value, i) => (value, i)).First(w => w.value.type == VRCAvatarDescriptor.AnimLayerType.FX);
            if (fx.isDefault)
                fx.isDefault = false;

            if (fx.animatorController == null)
                fx.animatorController = controller;
            else if (_isOverrideFxLayer)
                fx.animatorController = controller;

            _avatar.baseAnimationLayers[index] = fx;
        }

        private void SetupExpressions()
        {
            if (_avatar.expressionsMenu == null)
            {
                _avatar.expressionsMenu = GetObjectFromGuid<VRCExpressionsMenu>(DefaultExpressionMenuGuid);
            }
            else if (_isAppendToExistsExpression)
            {
                var expressions = CreateExpressionsMenuWithExistsOne(_avatar.expressionsMenu);
                if (expressions == null)
                    return;
                _avatar.expressionsMenu = expressions;
            }
        }

        private VRCExpressionsMenu CreateExpressionsMenuWithExistsOne(VRCExpressionsMenu expressions)
        {
            var parameters = CreateStageParameters(expressions.stageParameters);
            if (parameters == null)
                return null;

            expressions.stageParameters = parameters;

            if (expressions.controls.Any(w => w.subMenu == GetObjectFromGuid<VRCExpressionsMenu>(FirstPageExpressionMenuGuid)))
                return expressions;

            expressions.controls.Add(new VRCExpressionsMenu.Control
            {
                name = "Mochizuki Fukidashi System",
                icon = GetObjectFromGuid<Texture2D>(IconTextureGuid),
                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                subMenu = GetObjectFromGuid<VRCExpressionsMenu>(FirstPageExpressionMenuGuid)
            });

            return expressions;
        }

        private VRCStageParameters CreateStageParameters(VRCStageParameters parameters)
        {
            if (parameters.stageParameters.Any(w => w.name == StageParametersId))
                return parameters;

            var stage = parameters.stageParameters.First(w => string.IsNullOrWhiteSpace(w.name));
            stage.name = StageParametersId;
            stage.valueType = VRCStageParameters.Parameter.ValyeType.Int;

            return parameters;
        }

        private static string GetPathBetweenGameObjects(GameObject root, GameObject child)
        {
            var paths = new List<string>();
            var current = child.transform;

            while (current != null && current != root.transform)
            {
                paths.Add(current.name);
                current = current.parent;
            }

            paths.Reverse();
            return string.Join("/", paths);
        }

        private static T GetObjectFromGuid<T>(string guid) where T : Object
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static string GetSaveFilePath(string title, string name, string extension)
        {
            var file = EditorUtility.SaveFilePanel(title, "", name, extension);
            return string.IsNullOrWhiteSpace(file) ? null : $"Assets{file.Replace(Application.dataPath, "")}";
        }

        private static string GetSaveFolderPath(string title, string name)
        {
            var dir = EditorUtility.SaveFolderPanel(title, "", name);
            return string.IsNullOrWhiteSpace(dir) ? null : $"Assets{dir.Replace(Application.dataPath, "")}";
        }

        private static T ObjectPicker<T>(string label, T obj) where T : Object
        {
            return EditorGUILayout.ObjectField(new GUIContent(label), obj, typeof(T), true) as T;
        }

        private static T ReadonlyObjectPicker<T>(string label, T obj) where T : Object
        {
            using (new DisabledGroup())
                return ObjectPicker(label, obj);
        }

        private class DisabledGroup : IDisposable
        {
            public DisabledGroup()
            {
                EditorGUI.BeginDisabledGroup(true);
            }

            public void Dispose()
            {
                EditorGUI.EndDisabledGroup();
            }
        }
    }
}