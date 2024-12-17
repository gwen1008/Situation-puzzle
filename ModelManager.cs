namespace GWEN
{
    using UnityEngine;
    using UnityEngine.Networking;
    using TMPro;
    using System.Collections;
    using System.Text;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq; // 為了解析 JSON
    using System.Collections.Generic; // 為 List 提供支持using UnityEngine;

    public class NPCDialog : MonoBehaviour
    {
        [SerializeField] private GameObject dialogPanel; // 對話框 Panel
        [SerializeField] private TMP_Text dialogText; // 顯示對話的文字欄位

        private void Start()
        {
            // 初始化時隱藏對話框
            dialogPanel.SetActive(false);
        }

        // 顯示 NPC 對話框
        public void ShowDialog(string message)
        {
            dialogPanel.SetActive(true); // 顯示對話框
            dialogText.text = message;   // 設置對話內容
        }

        // 隱藏 NPC 對話框
        public void HideDialog()
        {
            dialogPanel.SetActive(false);
        }
    }



    public class ModelManager : MonoBehaviour
    {
        private string url = "https://g.ubitus.ai/v1/chat/completions";
        private string key = "d4pHv5n2G3q2vkVMPen3vFMfUJic4huKiQbvMmGLWUVIr/ptUuGnsCx/zVdYmVtdrGPO9//2h8Fbp6HkSQ0/oA==";

        private TMP_InputField inputField;
        private string prompt;

        [SerializeField] private TMP_Text npcResponseText; // 顯示 AI 回應的文字欄位

        // 存儲對話歷史的列表
        private List<object> messages = new List<object>();

        // 新增 role 屬性
        //public string role = "default"; // 預設角色

        private void Awake()
        {
            // 綁定輸入欄位
            inputField = GameObject.Find("輸入欄位")?.GetComponent<TMP_InputField>();

            if (inputField == null)
            {
                Debug.LogError("找不到名為 '輸入欄位' 的物件或 TMP_InputField 未綁定！");
                return;
            }

            inputField.onEndEdit.AddListener(PlayerInput);

            // 防止未設置 npcResponseText 出現問題
            if (npcResponseText == null)
            {
                Debug.LogError("npcResponseText 未綁定！請在 Inspector 中設置對應的文字物件。");
            }

            // 加入故事背景到對話歷史
            messages.Add(new
            {
                role = "system",
                content = "你是一名講故事者，正在與對話者玩海龜湯故事：\n" +
                          "題目：一名男子走進森林，突然停下來，然後開始慌張地逃出森林。最終，他倒在地上死了。\n" +
                          "問題：為什麼他會死？\n\n" +
                          "故事背景：這名男子目睹了一名壞人在埋屍，壞人為了不被公開罪行，決定將目擊者殺掉。\n" +
                          "在你收到開始的指令後，告訴玩家故事與問題：為什麼他會死？，你要根據玩家的提問，給予他們回應是、不是或與此無關，不要提示太多，直到他們猜出正確的解答，再告訴他們完整故事。"
            });
        }

        private void PlayerInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                Debug.LogWarning("輸入內容為空！");
                return;
            }

            Debug.Log($"玩家輸入內容: {input}");
            prompt = input;

            // 將玩家的輸入存入對話歷史
            messages.Add(new
            {
                name = "user",
                content = prompt,
                role = "user"
            });

            // 限制對話歷史長度
            TrimMessages();

            StartCoroutine(GetResult());
        }

        private IEnumerator GetResult()
        {
            var data = new
            {
                model = "llama-3.1-70b",
                messages = messages.ToArray(), // 傳遞完整的對話歷史
                stop = new string[] { "<|eot_id|>", "<|end_of_text|>" },
                frequency_penalty = 0,
                max_tokens = 100,
                temperature = 0.2,
                top_p = 0.5,
                top_k = 20,
                stream = false
            };

            string json = JsonConvert.SerializeObject(data); // 使用 Newtonsoft.Json
            Debug.Log($"JSON 請求資料: {json}");

            byte[] postData = Encoding.UTF8.GetBytes(json);

            UnityWebRequest request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(postData);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + key);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"API 錯誤：{request.error}");
                if (!string.IsNullOrEmpty(request.downloadHandler.text))
                {
                    Debug.LogError($"API 回應內容：{request.downloadHandler.text}");
                }

                npcResponseText.text = "抱歉，我無法處理您的請求。";
                yield break;
            }

            // API 成功回應
            Debug.Log($"API 回應成功: {request.downloadHandler.text}");

            try
            {
                // 使用 Newtonsoft.Json 解析 API 回應
                var response = JsonConvert.DeserializeObject<JObject>(request.downloadHandler.text);
                string assistantMessage = response["choices"][0]["message"]["content"].ToString();

                // 將 AI 的回應存入對話歷史
                messages.Add(new
                {
                    name = "assistant",
                    content = assistantMessage,
                    role = "assistant"
                });

                // 限制對話歷史長度
                TrimMessages();

                // 顯示 AI 回應內容
                Debug.Log($"AI 回應: {assistantMessage}");
                npcResponseText.text = assistantMessage;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"解析回應時出錯: {ex.Message}");
                npcResponseText.text = "抱歉，無法顯示 AI 回應。";
            }
        }

        // 限制對話歷史的長度
        private void TrimMessages()
        {
            // 只保留最近 20 條對話
            if (messages.Count > 20)
            {
                messages.RemoveRange(0, messages.Count - 20);
            }
        }
    }
}