using UnityEngine;

namespace LazyPan {
    public class Flow_SceneB : Flow {
		private Comp UI_SceneB;

		private Entity Obj_Camera_Camera;
		private Entity Obj_Player_Player;
		private Entity Obj_Event_SceneBUI;

        public override void Init(Flow baseFlow) {
            base.Init(baseFlow);
            ConsoleEx.Instance.ContentSave("flow", "Flow_SceneB  战斗场景B流程");
			UI_SceneB = UI.Instance.Open("UI_SceneB");

			Obj_Camera_Camera = Obj.Instance.LoadEntity("Obj_Camera_Camera");
			Obj_Player_Player = Obj.Instance.LoadEntity("Obj_Player_Player");
			Obj_Event_SceneBUI = Obj.Instance.LoadEntity("Obj_Event_SceneBUI");

        }

		/*获取UI*/
		public Comp GetUI() {
			return UI_SceneB;
		}


        /*下一步*/
        public void Next(string teleportSceneSign) {
            Clear();
            Launch.instance.StageLoad(teleportSceneSign);
        }

        public override void Clear() {
            base.Clear();
			Obj.Instance.UnLoadEntity(Obj_Event_SceneBUI);
			Obj.Instance.UnLoadEntity(Obj_Player_Player);
			Obj.Instance.UnLoadEntity(Obj_Camera_Camera);

			UI.Instance.Close("UI_SceneB");

        }
    }
}