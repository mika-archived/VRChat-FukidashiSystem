/*-------------------------------------------------------------------------------------------
 * Copyright (c) Fuyuno Mikazuki / Natsuneko. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 *------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Mochizuki.VRChat.Extensions.Unity;
using Mochizuki.VRChat.Extensions.VRC;

using UnityEditor;
using UnityEditor.Animations;

using UnityEngine;

using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Mochizuki.VRChat.FukidashiSystem
{
    internal class FukidashiEditor : EditorWindow
    {
        private const string IconTextureGuid = "ae87f67e98a20294082a139386f57b45";
        private const string StageParametersId = "Mochizuki_FukidashiSystem";
        private const string PrefabGuid = "24493fbf74952924da5e7590ff84f993";
        private const string NextPageName = "Next Page";
        private const string OuterExprName = "Mochizuki Fukidashi System";

        private static readonly IReadOnlyList<(string, int)> InnerControlNamesP1 = new List<(string, int)>
        {
            ("Reset", 0),
            ("Message 1", 1),
            ("Message 2", 2),
            ("Message 3", 3),
            ("Message 4", 4),
            (NextPageName, -1)
        }.AsReadOnly();

        private static readonly IReadOnlyList<(string, int)> InnerControlNamesP2 = new List<(string, int)>
        {
            ("Message 5", 5),
            ("Message 6", 6),
            ("Message 7", 7),
            ("Message 8", 8),
            ("Message 9", 9),
            (NextPageName, -1)
        }.AsReadOnly();

        private static readonly IReadOnlyList<(string, int)> InnerControlNamesP3 = new List<(string, int)>
        {
            ("Message 10", 10),
            ("Message 11", 11),
            ("Message 12", 12),
            ("Message 13", 13),
            ("Message 14", 14),
            ("Message 15", 15),
            ("Message 16", 16)
        }.AsReadOnly();

        private VRCAvatarDescriptor _avatar;
        private bool _isMergeAnimatorController;
        private bool _isMergeExpressionsMenu;
        private bool _isMergeStageParameters;
        private bool _isShowOptions;
        private GameObject _parent;
        private GameObject _prefab;

        [MenuItem("Mochizuki/VRChat/Fukidashi Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<FukidashiEditor>();
            window.titleContent = new GUIContent("Fukidashi Editor");

            window.Show();
        }

        // ReSharper disable once UnusedMember.Local
        private void OnGUI()
        {
            EditorGUILayout.Space();
            GUILayout.Label("Mochizuki.FukidashiSystem for VRChat Avatars 3.0 / Version 0.1");
            EditorGUILayout.Space();

            EditorGUIUtility.labelWidth = 300;

            _avatar = EditorGUILayoutExtensions.ObjectPicker("VRC Avatar Descriptor", _avatar);
            _parent = EditorGUILayoutExtensions.ObjectPicker("Message Board Parent", _parent);
            _prefab = EditorGUILayoutExtensions.ReadonlyObjectPicker("Message Board Prefab", AssetDatabaseExtensions.LoadAssetFromGuid<GameObject>(PrefabGuid));

            _isShowOptions = EditorGUILayout.Foldout(_isShowOptions, "Merge Options");
            if (_isShowOptions)
                using (new IncreaseIndent())
                {
                    _isMergeAnimatorController = EditorGUILayout.Toggle("Merge with existing Animator Controller", _isMergeAnimatorController);
                    _isMergeExpressionsMenu = EditorGUILayout.Toggle("Merge with existing Expressions Menu", _isMergeExpressionsMenu);
                    _isMergeStageParameters = EditorGUILayout.Toggle("Merge with existing Expression Parameters", _isMergeStageParameters);
                }

            using (new DisabledGroup(_avatar == null || _parent == null))
            {
                if (GUILayout.Button("Generate Assets and Apply Changes"))
                    try
                    {
                        OnSubmit();
                    }
                    catch (Exception e)
                    {
                        GUILayout.Label($"An error occured in operation -> {e.GetType().Name}");
                        GUILayout.Label($"Error Message : {e.Message}");

                        Debug.LogError(e);
                    }
            }
        }

        private void OnSubmit()
        {
            var parameters = ConfigureStageParameters(_avatar, _isMergeStageParameters);
            var animations = CreateAnimations(_avatar, _parent);
            var controller = ConfigureAnimatorController(_avatar, animations, _isMergeAnimatorController);
            var innerExpr = CreateInnerExpressionsMenu();
            var outerExpr = ConfigureOuterExpressionsMenu(_avatar, innerExpr.First(), _isMergeExpressionsMenu);

            SetupAvatar(_avatar, _parent, _prefab, controller, parameters, outerExpr);
        }

        private static VRCExpressionParameters ConfigureStageParameters(VRCAvatarDescriptor avatar, bool isMergeStageParameters)
        {
            var dest = EditorUtilityExtensions.GetSaveFilePath("Save a created/merged Expression Parameters to...", "VRCExpressionParameters", "asset");
            if (avatar.customExpressions && isMergeStageParameters)
                return MergeWithExistsExpressionParameters(avatar, dest);
            return CreateStageParameters(dest);
        }

        private static VRCExpressionParameters CreateStageParameters(string dest)
        {
            var parameters = new VRCExpressionParameters();
            parameters.InitExpressionParameters();
            parameters.AddParametersToLastEmptySpace(StageParametersId, VRCExpressionParameters.ValueType.Int);

            AssetDatabase.CreateAsset(parameters, dest);

            return parameters;
        }

        private static VRCExpressionParameters MergeWithExistsExpressionParameters(VRCAvatarDescriptor avatar, string dest)
        {
            var parameters = AssetDatabaseExtensions.CopyAndLoadAsset(avatar.expressionParameters, dest);
            parameters.AddParametersToFirstEmptySpace(StageParametersId, VRCExpressionParameters.ValueType.Int);

            AssetDatabase.SaveAssets();

            return parameters;
        }

        private static List<AnimationClip> CreateAnimations(VRCAvatarDescriptor avatar, GameObject parent)
        {
            var animations = new List<AnimationClip>();
            var baseDir = EditorUtilityExtensions.GetSaveFolderPath("Save Animations to folder...", "Animations");

            var relative = avatar.gameObject.GetRelativePathFor(parent);

            for (var i = 0; i < 16; i++)
            {
                var animation = new AnimationClip();

                AnimationUtility.SetEditorCurve(animation, EditorCurveBinding.FloatCurve($"{relative}/MessageBoard", typeof(MeshRenderer), "material._TextureNo"), AnimationCurve.Linear(0, i, 1 / 60f, i));
                AnimationUtility.SetEditorCurve(animation, EditorCurveBinding.FloatCurve($"{relative}/MessageBoard", typeof(GameObject), "m_IsActive"), AnimationCurve.Linear(0, i == 0 ? 0 : 1, 1 / 60f, i == 0 ? 0 : 1));

                AssetDatabase.CreateAsset(animation, $"{baseDir}/SwitchTo{i}.anim");
                animations.Add(animation);
            }

            return animations;
        }

        private static AnimatorController ConfigureAnimatorController(VRCAvatarDescriptor avatar, List<AnimationClip> animations, bool isMergeAnimatorController)
        {
            var dest = EditorUtilityExtensions.GetSaveFilePath("Save a created/merged Animator Controller to...", "FXLayer", "controller");
            if (avatar.customizeAnimationLayers && avatar.HasAnimationLayer(VRCAvatarDescriptor.AnimLayerType.FX) && isMergeAnimatorController)
                return MergeWithExistsAnimatorController(avatar, animations, dest);
            return CreateAnimatorController(animations, dest);
        }

        private static AnimatorController CreateAnimatorController(List<AnimationClip> animations, string dest)
        {
            var controller = new AnimatorController();
            controller.AddParameter(StageParametersId, AnimatorControllerParameterType.Int);
            controller.AddLayer("Base Layer");

            var layer = controller.GetLayer("Base Layer");
            var stateMachine = layer.stateMachine;

            foreach (var (animation, index) in animations.Select((w, i) => (Value: w, Index: i)))
            {
                var state = stateMachine.AddState(animation.name);
                state.motion = animation;
                state.writeDefaultValues = false;

                var transition = stateMachine.AddAnyStateTransition(state);
                transition.AddCondition(AnimatorConditionMode.Equals, index, StageParametersId);
            }

            AssetDatabase.CreateAsset(controller, dest);

            return controller;
        }

        private static AnimatorController MergeWithExistsAnimatorController(VRCAvatarDescriptor avatar, List<AnimationClip> animations, string dest)
        {
            var controller = (AnimatorController) AssetDatabaseExtensions.CopyAndLoadAsset(avatar.GetAnimationLayer(VRCAvatarDescriptor.AnimLayerType.FX).animatorController, dest);
            if (!controller.HasParameter(StageParametersId))
                controller.AddParameter(StageParametersId, AnimatorControllerParameterType.Int);
            if (controller.HasLayer("Fukidashi System"))
                return controller; // already configured
            controller.AddLayer("Fukidashi System");

            var layer = controller.GetLayer("Fukidashi System");
            layer.defaultWeight = 1.0f;

            var stateMachine = layer.stateMachine;

            foreach (var (animation, index) in animations.Select((w, i) => (Value: w, Index: i)))
            {
                var state = stateMachine.AddState(animation.name);
                state.motion = animation;
                state.writeDefaultValues = false;

                var transition = stateMachine.AddAnyStateTransition(state);
                transition.AddCondition(AnimatorConditionMode.Equals, index, StageParametersId);
            }

            AssetDatabase.SaveAssets();

            return controller;
        }

        private static List<VRCExpressionsMenu> CreateInnerExpressionsMenu()
        {
            var dest = EditorUtilityExtensions.GetSaveFolderPath("Save a Inner Expressions Menu assets to...", "ExpressionsMenus");
            var innerExpressions = new List<VRCExpressionsMenu> { CreateExpressionsMenuFromName(InnerControlNamesP3, null, 3, dest) };
            innerExpressions.Add(CreateExpressionsMenuFromName(InnerControlNamesP2, innerExpressions.Last(), 2, dest));
            innerExpressions.Add(CreateExpressionsMenuFromName(InnerControlNamesP1, innerExpressions.Last(), 1, dest));
            innerExpressions.Reverse();

            return innerExpressions;
        }

        private static VRCExpressionsMenu CreateExpressionsMenuFromName(IReadOnlyList<(string, int)> names, VRCExpressionsMenu next, int index, string dest)
        {
            var path = Path.Combine(dest, $"FukidashiSystem_P{index}.asset");
            var expr = CreateInstance<VRCExpressionsMenu>();

            foreach (var (name, i) in names)
            {
                var control = new VRCExpressionsMenu.Control { name = name };
                if (name == NextPageName)
                {
                    control.type = VRCExpressionsMenu.Control.ControlType.SubMenu;
                    control.subMenu = next;
                }
                else
                {
                    control.type = VRCExpressionsMenu.Control.ControlType.Toggle;
                    control.parameter = new VRCExpressionsMenu.Control.Parameter { name = StageParametersId };
                    control.value = i;
                }

                expr.controls.Add(control);
            }

            AssetDatabase.CreateAsset(expr, path);

            return expr;
        }

        private static VRCExpressionsMenu ConfigureOuterExpressionsMenu(VRCAvatarDescriptor avatar, VRCExpressionsMenu first, bool isMergeExpressionsMenu)
        {
            var dest = EditorUtilityExtensions.GetSaveFilePath("Save a created/merged Expressions Menu to...", "VRCExpressionsMenu", "asset");
            if (avatar.customExpressions && avatar.expressionsMenu != null && isMergeExpressionsMenu)
                return MergeWithExistsOuterExpressionsMenu(avatar, first, dest);
            return CreateOuterExpressionsMenu(first, dest);
        }

        private static VRCExpressionsMenu CreateOuterExpressionsMenu(VRCExpressionsMenu first, string dest)
        {
            var expr = CreateInstance<VRCExpressionsMenu>();
            expr.controls.Add(new VRCExpressionsMenu.Control
            {
                name = OuterExprName,
                icon = AssetDatabaseExtensions.LoadAssetFromGuid<Texture2D>(IconTextureGuid),
                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                subMenu = first
            });

            AssetDatabase.CreateAsset(expr, dest);

            return expr;
        }

        private static VRCExpressionsMenu MergeWithExistsOuterExpressionsMenu(VRCAvatarDescriptor avatar, VRCExpressionsMenu first, string dest)
        {
            var expr = AssetDatabaseExtensions.CopyAndLoadAsset(avatar.expressionsMenu, dest);
            if (expr.controls.Any(w => w.name == OuterExprName))
            {
                var control = expr.controls.First(w => w.name == OuterExprName);
                control.subMenu = first;

                AssetDatabase.SaveAssets();

                return expr;
            }

            expr.controls.Add(new VRCExpressionsMenu.Control
            {
                name = OuterExprName,
                icon = AssetDatabaseExtensions.LoadAssetFromGuid<Texture2D>(IconTextureGuid),
                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                subMenu = first
            });

            AssetDatabase.SaveAssets();

            return expr;
        }

        private static void SetupAvatar(VRCAvatarDescriptor avatar, GameObject parent, GameObject prefab, AnimatorController controller, VRCExpressionParameters parameters, VRCExpressionsMenu expr)
        {
            avatar.SetAnimationLayer(VRCAvatarDescriptor.AnimLayerType.FX, controller);
            avatar.SetExpressions(expr, parameters);

            var instance = (GameObject) PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.transform.rotation = Quaternion.Euler(0, 180, 0);
            instance.transform.parent = parent.transform;
        }
    }
}