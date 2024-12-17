using UnityEngine;

namespace GWEN
{
    /// <summary>
    /// NPC 資料
    /// </summary>
    public class NPCController : MonoBehaviour
    {
        [SerializeField, Header("NPC 資料")]
        private DataNPC dataNPC;
        [SerializeField, Header("動畫參數")]
        private string[] parameters =
        {
            "觸發Leave","觸發Hit","觸發Death","觸發run","觸發Attack"
        };


        private Animator ani;

        public DataNPC data => dataNPC;

        private void Awake()
        {
            ani = GetComponent<Animator>();

        }
        public void PlayAinmation(int index)
        {
            ani.SetTrigger(parameters[index]);
        }

    }
}
