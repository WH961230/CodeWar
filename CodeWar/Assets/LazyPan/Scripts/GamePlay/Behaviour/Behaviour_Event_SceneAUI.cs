using UnityEngine.UI;

namespace LazyPan {
    public class Behaviour_Event_SceneAUI : Behaviour {
        public Behaviour_Event_SceneAUI(Entity entity, string behaviourSign) : base(entity, behaviourSign) {
            Flo.Instance.GetFlow(out Flow_SceneA flow);
            Button jumpBtn = Cond.Instance.Get<Button>(flow.GetUI(), Label.JUMP);
            ButtonRegister.AddListener(jumpBtn, () => {
                flow.Next("SceneB");
            });
        }

        public override void DelayedExecute() {
            
        }

        public override void Clear() {
            base.Clear();
        }
    }
}