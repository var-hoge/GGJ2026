using System;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace IsoTools.Internal {
	[CustomEditor(typeof(IsoObject)), CanEditMultipleObjects]
	class IsoObjectEditor : Editor {
		static bool _showWorldSettings = false;

		Dictionary<IsoWorld, List<IsoObject>> _isoObjects   = new Dictionary<IsoWorld, List<IsoObject>>();
		Dictionary<IsoWorld, List<IsoObject>> _otherObjects = new Dictionary<IsoWorld, List<IsoObject>>();
		Dictionary<IsoWorld, Vector3>         _viewCenters  = new Dictionary<IsoWorld, Vector3>();

		// ---------------------------------------------------------------------
		//
		//
		//
		// ---------------------------------------------------------------------
		/// <summary> ポジションに変更が加えられると呼び出される </summary>
		void PrepareTargets() {
			_isoObjects = targets
				.OfType<IsoObject>()
				.Where(p => p.isoWorld)
				.GroupBy(p => p.isoWorld)
				.ToDictionary(p => p.Key, p => p.ToList());
			_viewCenters = _isoObjects.ToDictionary(
				pair => pair.Key,
				pair =>{
					var iso_world = pair.Key; 
					pair.Value.Aggregate(Vector3.zero, (AccIn, p) => //なぜValue.Countが1なのにAggregateを使っているのか
					{
						var result = AccIn + IsoUtils.Vec3FromVec2(iso_world.IsoToScreen(p.position + p.size * 0.5f));
						return result;
					});
					return pair.Value.Aggregate(Vector3.zero, (AccIn, p) => {
						return AccIn + IsoUtils.Vec3FromVec2(
							iso_world.IsoToScreen(p.position + p.size * 0.5f));
					}) / pair.Value.Count;
				});
			_otherObjects = FindObjectsOfType<IsoObject>()
				.Where(p => p.IsActive() && p.isoWorld)
				.Where(p => _isoObjects.ContainsKey(p.isoWorld))
				.Where(p => !_isoObjects[p.isoWorld].Contains(p))
				.GroupBy(p => p.isoWorld)
				.ToDictionary(p => p.Key, p => p.ToList());
		}

		void DrawCustomInspector() {
			if ( DrawDetachedInspector() ) {
				return;
			}

			var iso_worlds = _isoObjects.Keys.ToArray();
			if ( iso_worlds.Length == 0 ) {
				return;
			}

			_showWorldSettings = IsoEditorUtils.DoFoldoutHeaderGroup(_showWorldSettings, "World Settings", () => {
				IsoEditorUtils.DrawSelfWorldProperty(iso_worlds);
				IsoEditorUtils.DrawWorldProperties(iso_worlds);
			});
		}

		bool DrawDetachedInspector() {
			var iso_object = targets.Length == 1 ? target as IsoObject : null;
			var detached = iso_object && iso_object.IsActive() && !iso_object.isoWorld;

			if ( detached ) {
				EditorGUILayout.HelpBox(
					"Detached IsoObject\nNeed to be a child of IsoWorld",
					MessageType.Warning,
					true);
			}

			return detached;
		}

		// ---------------------------------------------------------------------
		//
		//
		//
		// ---------------------------------------------------------------------

		void DisableCustomTools() {
			if ( Tools.hidden ) {
				Tools.hidden = false;
				Tools.current = Tool.Move;
			}
		}

		void DrawCustomTools() {
			if ( Tools.current == Tool.Move ) {
				Tools.hidden = true;
				ZMoveSliderTool();
				XYMoveSliderTool(Handles.xAxisColor, IsoUtils.vec3OneX);
				XYMoveSliderTool(Handles.yAxisColor, IsoUtils.vec3OneY);
				XYMoveRectangleTool();
			} else {
				Tools.hidden = false;
			}
		}

		void ZMoveSliderTool() {
			foreach ( var iso_world in _viewCenters.Keys.ToList() ) {
				EditorGUI.BeginChangeCheck(); //次の条件式にたどり着くまでのコード内に値の変更があった場合、EndChangeCheck()はtrueを返すようになる → はずなのに、trueが返ってこない
				var old_center = _viewCenters[iso_world]; //isoObject.position の中心座標のようだ → yはtransform.position.yを正しく取得できている気がするが、xはそうでもないようだ
				var new_center = IsoEditorUtils.GizmoSlider( //※IsoEditorUtils.GizmoSlider関数は、EditorGUI.BeginChangeCheck()の監視対象外らしい（ChatGPT）また同様に、Dictionary _viewCentersも監視対象外
					Handles.zAxisColor,
					old_center,
					IsoUtils.vec3OneY);
				if ( EditorGUI.EndChangeCheck() ) { //調査結果：常にfalse
					var old_delta = new_center - old_center;
					var new_delta = IsoEditorUtils.ZMoveIsoObjects(
						true,
						iso_world,
						_isoObjects,
						_otherObjects,
						old_delta.y / iso_world.tileHeight) * iso_world.tileHeight;
					_viewCenters[iso_world] = old_center + IsoUtils.Vec3FromY(new_delta);
				}
			}
		}

		void XYMoveSliderTool(Color color, Vector3 dir) {
			foreach ( var iso_world in _viewCenters.Keys.ToList() ) {
				EditorGUI.BeginChangeCheck();
				var old_center = _viewCenters[iso_world];
				var new_center = IsoEditorUtils.GizmoSlider(
					color,
					old_center,
					iso_world.IsoToScreen(dir));
				if ( EditorGUI.EndChangeCheck() ) {
					var old_delta = new_center - old_center;
					var new_delta = iso_world.IsoToScreen(IsoEditorUtils.XYMoveIsoObjects(
						true,
						iso_world,
						_isoObjects,
						_otherObjects,
						iso_world.ScreenToIso(old_delta)));
					_viewCenters[iso_world] = old_center + IsoUtils.Vec3FromVec2(new_delta);
				}
			}
		}

		void XYMoveRectangleTool() {
			foreach ( var iso_world in _viewCenters.Keys.ToList() ) {
				EditorGUI.BeginChangeCheck();
				var old_center = _viewCenters[iso_world];
				var new_center = IsoEditorUtils.GizmoRectangle(old_center);
				if ( EditorGUI.EndChangeCheck() ) {
					var old_delta = new_center - old_center;
					var new_delta = iso_world.IsoToScreen(IsoEditorUtils.XYMoveIsoObjects(
						true,
						iso_world,
						_isoObjects,
						_otherObjects,
						iso_world.ScreenToIso(old_delta)));
					_viewCenters[iso_world] = old_center + IsoUtils.Vec3FromVec2(new_delta);
				}
			}
		}

		// ---------------------------------------------------------------------
		//
		//
		//
		// ---------------------------------------------------------------------
		/// <summary> 呼び出されている </summary>
		void DirtyTargetPositions() {
			var iso_object = targets.Length == 1 ? target as IsoObject : null;
			if ( iso_object && iso_object.IsActive() && !Application.isPlaying ) {
				var position_prop = serializedObject.FindProperty("_position");
				if ( position_prop != null && !position_prop.prefabOverride ) { //position_prop.prefabOverride は インスタンス化しているGameObjectの値が変更された時 true を代入する
					var last_value = position_prop.vector3Value;
					position_prop.vector3Value = last_value + Vector3.one;
					PrefabUtility.RecordPrefabInstancePropertyModifications(target);
					serializedObject.ApplyModifiedProperties();
					position_prop.vector3Value = last_value; //position_propというプロパティに値を代入している → IsoObject.positionへ代入されている値と同じ
					PrefabUtility.RecordPrefabInstancePropertyModifications(target);
					serializedObject.ApplyModifiedProperties();
				}
			}
		}

		// ---------------------------------------------------------------------
		//
		// Messages
		//
		// ---------------------------------------------------------------------

		void OnEnable() {
			PrepareTargets();
		}

		void OnDisable() {
			DisableCustomTools();
		}
		/// <summary> Iso系コンポーネントを持ったGameObjectを選択した状態で、何かしらのキー入力を行った際に一度だけ呼び出される </summary>
		void OnSceneGUI() {
			DrawCustomTools();
		}
		/// <summary> 呼び出されている/調査結果:Z座標に関係する処理はひとつも無いらしい </summary>
		public override void OnInspectorGUI() {
			PrepareTargets();
			DirtyTargetPositions();
			DrawDefaultInspector();
			DrawCustomInspector();
		}
	}
}