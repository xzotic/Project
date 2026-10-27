using UnityEngine;
using UnityEngine.UI;

namespace AdvancedRogueLikeandPuzzleSystem
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("SETUP")]
        [Tooltip("Scene loaded by New Game. Continue uses the scene stored in the save.")]
        public string GameplaySceneName = "DemoScene";

        [Header("WIRING")]
        public Button Button_Continue;
        public Button Button_NewGame;
        public Button Button_Quit;
        public Text Label_SaveInfo;

        [Header("OVERWRITE CONFIRMATION")]
        public GameObject Panel_ConfirmNewGame;
        public Button Button_ConfirmYes;
        public Button Button_ConfirmNo;

        private void Start()
        {
            Time.timeScale = 1f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (Panel_ConfirmNewGame != null) Panel_ConfirmNewGame.SetActive(false);

            if (Button_Continue != null) Button_Continue.onClick.AddListener(Click_Continue);
            if (Button_NewGame != null) Button_NewGame.onClick.AddListener(Click_NewGame);
            if (Button_Quit != null) Button_Quit.onClick.AddListener(Click_Quit);
            if (Button_ConfirmYes != null) Button_ConfirmYes.onClick.AddListener(Click_ConfirmNewGame);
            if (Button_ConfirmNo != null) Button_ConfirmNo.onClick.AddListener(Click_CancelNewGame);

            RefreshSaveState();
        }

        private void RefreshSaveState()
        {
            SaveData save = SaveSystem.Load();
            bool hasSave = save != null;

            if (Button_Continue != null)
            {
                Button_Continue.interactable = hasSave;

                Text label = Button_Continue.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    Color c = label.color;
                    c.a = hasSave ? 1f : 0.35f;
                    label.color = c;
                }
            }

            if (Label_SaveInfo != null)
            {
                Label_SaveInfo.text = hasSave
                    ? "Last save: " + save.DisplayDate + "   -   " + save.SceneName
                    : "No saved game yet";
            }
        }

        public void Click_Continue()
        {
            GameSession.ContinueGame(GameplaySceneName);
        }

        public void Click_NewGame()
        {
            if (SaveSystem.HasSave() && Panel_ConfirmNewGame != null)
            {
                Panel_ConfirmNewGame.SetActive(true);
                return;
            }
            StartFreshGame();
        }

        public void Click_ConfirmNewGame()
        {
            StartFreshGame();
        }

        public void Click_CancelNewGame()
        {
            if (Panel_ConfirmNewGame != null) Panel_ConfirmNewGame.SetActive(false);
        }

        private void StartFreshGame()
        {
            GameSession.StartNewGame(GameplaySceneName);
        }

        public void Click_Quit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
