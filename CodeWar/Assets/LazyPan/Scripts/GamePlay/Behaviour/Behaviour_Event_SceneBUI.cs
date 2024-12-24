using UnityEngine.UI;

namespace LazyPan {
    public class Behaviour_Event_SceneBUI : Behaviour {
        public Behaviour_Event_SceneBUI(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            Flo.Instance.GetFlow(out Flow_SceneB flow);
            Button jumpBtn = Cond.Instance.Get<Button>(flow.GetUI(), Label.JUMP);
            ButtonRegister.AddListener(jumpBtn, () => {
                flow.Next("SceneA");
            });
        }

        public override void DelayedExecute() {
            
        }

        public override void Clear() {
            base.Clear();
        }
    }
}