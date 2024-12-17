namespace GWEN
{
    using UnityEngine;
    using UnityEngine.Networking;
    using TMPro;
    using System.Collections;
    using System.Text;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq; // ���F�ѪR JSON
    using System.Collections.Generic; // �� List ���Ѥ��using UnityEngine;

    public class NPCDialog : MonoBehaviour
    {
        [SerializeField] private GameObject dialogPanel; // ��ܮ� Panel
        [SerializeField] private TMP_Text dialogText; // ��ܹ�ܪ���r���

        private void Start()
        {
            // ��l�Ʈ����ù�ܮ�
            dialogPanel.SetActive(false);
        }

        // ��� NPC ��ܮ�
        public void ShowDialog(string message)
        {
            dialogPanel.SetActive(true); // ��ܹ�ܮ�
            dialogText.text = message;   // �]�m��ܤ��e
        }

        // ���� NPC ��ܮ�
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

        [SerializeField] private TMP_Text npcResponseText; // ��� AI �^������r���

        // �s�x��ܾ��v���C��
        private List<object> messages = new List<object>();

        // �s�W role �ݩ�
        //public string role = "default"; // �w�]����

        private void Awake()
        {
            // �j�w��J���
            inputField = GameObject.Find("��J���")?.GetComponent<TMP_InputField>();

            if (inputField == null)
            {
                Debug.LogError("�䤣��W�� '��J���' ������� TMP_InputField ���j�w�I");
                return;
            }

            inputField.onEndEdit.AddListener(PlayerInput);

            // ����]�m npcResponseText �X�{���D
            if (npcResponseText == null)
            {
                Debug.LogError("npcResponseText ���j�w�I�Цb Inspector ���]�m��������r����C");
            }

            // �[�J�G�ƭI�����ܾ��v
            messages.Add(new
            {
                role = "system",
                content = "�A�O�@�W���G�ƪ̡A���b�P��ܪ̪����t���G�ơG\n" +
                          "�D�ءG�@�W�k�l���i�˪L�A��M���U�ӡA�M��}�l�W�i�a�k�X�˪L�C�̲סA�L�˦b�a�W���F�C\n" +
                          "���D�G������L�|���H\n\n" +
                          "�G�ƭI���G�o�W�k�l�ظ@�F�@�W�a�H�b�I�͡A�a�H���F���Q���}�o��A�M�w�N�����̱����C\n" +
                          "�b�A����}�l�����O��A�i�D���a�G�ƻP���D�G������L�|���H�A�A�n�ھڪ��a�����ݡA�����L�̦^���O�B���O�λP���L���A���n���ܤӦh�A����L�̲q�X���T���ѵ��A�A�i�D�L�̧���G�ơC"
            });
        }

        private void PlayerInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                Debug.LogWarning("��J���e���šI");
                return;
            }

            Debug.Log($"���a��J���e: {input}");
            prompt = input;

            // �N���a����J�s�J��ܾ��v
            messages.Add(new
            {
                name = "user",
                content = prompt,
                role = "user"
            });

            // �����ܾ��v����
            TrimMessages();

            StartCoroutine(GetResult());
        }

        private IEnumerator GetResult()
        {
            var data = new
            {
                model = "llama-3.1-70b",
                messages = messages.ToArray(), // �ǻ����㪺��ܾ��v
                stop = new string[] { "<|eot_id|>", "<|end_of_text|>" },
                frequency_penalty = 0,
                max_tokens = 100,
                temperature = 0.2,
                top_p = 0.5,
                top_k = 20,
                stream = false
            };

            string json = JsonConvert.SerializeObject(data); // �ϥ� Newtonsoft.Json
            Debug.Log($"JSON �ШD���: {json}");

            byte[] postData = Encoding.UTF8.GetBytes(json);

            UnityWebRequest request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(postData);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + key);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"API ���~�G{request.error}");
                if (!string.IsNullOrEmpty(request.downloadHandler.text))
                {
                    Debug.LogError($"API �^�����e�G{request.downloadHandler.text}");
                }

                npcResponseText.text = "��p�A�ڵL�k�B�z�z���ШD�C";
                yield break;
            }

            // API ���\�^��
            Debug.Log($"API �^�����\: {request.downloadHandler.text}");

            try
            {
                // �ϥ� Newtonsoft.Json �ѪR API �^��
                var response = JsonConvert.DeserializeObject<JObject>(request.downloadHandler.text);
                string assistantMessage = response["choices"][0]["message"]["content"].ToString();

                // �N AI ���^���s�J��ܾ��v
                messages.Add(new
                {
                    name = "assistant",
                    content = assistantMessage,
                    role = "assistant"
                });

                // �����ܾ��v����
                TrimMessages();

                // ��� AI �^�����e
                Debug.Log($"AI �^��: {assistantMessage}");
                npcResponseText.text = assistantMessage;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"�ѪR�^���ɥX��: {ex.Message}");
                npcResponseText.text = "��p�A�L�k��� AI �^���C";
            }
        }

        // �����ܾ��v������
        private void TrimMessages()
        {
            // �u�O�d�̪� 20 �����
            if (messages.Count > 20)
            {
                messages.RemoveRange(0, messages.Count - 20);
            }
        }
    }
}