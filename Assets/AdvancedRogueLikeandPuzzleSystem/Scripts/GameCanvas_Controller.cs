using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;

namespace AdvancedRogueLikeandPuzzleSystem
{
    public class GameCanvas_Controller : MonoBehaviour
    {
        public GameObject MirrorUI;
        public GameObject Text_Warning;
        public GameObject Text_Grabbed;

        public Image Image_Health;
        public Image Image_Mana;
        public Text Text_Coin;
        public Text Text_InteractionKey;

        public GameObject Text_Level;
        public Image Image_Level;
        public GameObject Text_Name;
        public Image Image_Profile;

        public GameObject GamePlayPanel;
        public GameObject GameOverPanel;
        public GameObject Panel_Warning;

        public AudioSource audioSource;
        public GameObject Panel_EnemyStatusInfo;
        public Image Image_EnemyHealth;
        public Text Text_EnemyName;

        public Image Image_Damage;
        public Color Damage_Effect_Color;

        public GameObject Image_Health_Potion;
        public GameObject Image_Mana_Potion;
        public static GameCanvas_Controller instance;

        public GameObject Joystick;
        public GameObject Touchpad;
        public GameObject Button_Jump;
        public GameObject Button_Interact;
        public GameObject Button_Hit;
        public GameObject Button_Shield;
        public GameObject Button_Sprint;
        public GameObject Button_Pause;
        public GameObject Button_Inventory;

        private Text Label_Grabbed;
        private Text Label_Name;
        private Text Label_Warning;

        private Coroutine hideGrabbedTextRoutine;
        private Coroutine hideWarningRoutine;
        private Coroutine hideEnemyStatusRoutine;

        private void Awake()
        {
            instance = this;

            if (Text_Grabbed != null) Label_Grabbed = Text_Grabbed.GetComponentInChildren<Text>(true);
            if (Text_Name != null) Label_Name = Text_Name.GetComponentInChildren<Text>(true);
            if (Panel_Warning != null) Label_Warning = Panel_Warning.GetComponentInChildren<Text>(true);
        }

        private Image Fill_Health_Potion;
        private Image Fill_Mana_Potion;

        private void OnDestroy()
        {
            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.Changed -= Refresh_Potion_Slots;
            }
        }

        public void Refresh_Potion_Slots()
        {
            if (InventorySystem.Instance == null || HeroController.instance == null) return;

            if (Fill_Health_Potion == null && Image_Health_Potion != null)
            {
                Fill_Health_Potion = Image_Health_Potion.GetComponent<Image>();
            }
            if (Fill_Mana_Potion == null && Image_Mana_Potion != null)
            {
                Fill_Mana_Potion = Image_Mana_Potion.GetComponent<Image>();
            }

            int maxHealth = Mathf.Max(1, HeroController.instance.MaxHealthPot);
            int maxMana = Mathf.Max(1, HeroController.instance.MaxManaPot);

            if (Fill_Health_Potion != null)
            {
                Fill_Health_Potion.fillAmount = (float)InventorySystem.Instance.CountOfPotion(PotionType.Health) / maxHealth;
            }
            if (Fill_Mana_Potion != null)
            {
                Fill_Mana_Potion.fillAmount = (float)InventorySystem.Instance.CountOfPotion(PotionType.Mana) / maxMana;
            }
        }

        void Start()
        {
            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.Changed += Refresh_Potion_Slots;
            }

            Update_Health_Bar();
            Update_Text_Coin();
            Refresh_Potion_Slots();
            if (GameManager.Instance != null && Text_InteractionKey != null)
            {
                Text_InteractionKey.text = GameManager.Instance.Keycode_Interact.ToString();
            }
        }

        public void Configure_For_PCConsole()
        {
            Joystick.SetActive(false);
            Touchpad.SetActive(false);
            Button_Sprint.SetActive(false);
            Button_Jump.SetActive(false);
            Button_Interact.SetActive(false);
            Button_Pause.SetActive(false);
            Button_Hit.SetActive(false);
            Button_Shield.SetActive(false);
            if (Button_Inventory != null) Button_Inventory.SetActive(false);
        }

        public void Click_Button_Sprint()
        {
            ThirdPersonController.Instance.Sprint_Now();
        }

        public IEnumerator Show_GameOver_Panel()
        {
            yield return new WaitForSeconds(3);
            GameOverPanel.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Time.timeScale = 0;
        }

        public void Show_MirrorUI(bool decision)
        {
            if(GameManager.Instance.controllerType == ControllerType.KeyboardMouse)
            {
                MirrorUI.SetActive(decision);
            }
        }

        public void Configure_For_Mobile()
        {
            Joystick.SetActive(true);
            Touchpad.SetActive(true);
            Button_Sprint.SetActive(true);
            Button_Pause.SetActive(true);
            Button_Jump.SetActive(true);
            if (Button_Inventory != null) Button_Inventory.SetActive(true);
        }

        public void Click_Interact_Button()
        {
            HeroController.instance.CheckInteractionInputsForMobile();
        }

        public void Click_Hit_Button()
        {
            HeroController.instance.Hit();
        }

        public void Click_Button_Pause()
        {
            PauseTheGame();
        }

        public void Click_Defend_Button()
        {
            HeroController.instance.Defend();
        }

        public void Click_Button_Potion(int i)
        {
            HeroController.instance.CheckPotionInputsForMobile(i);
        }

        public void Hide_Button_Shield()
        {
            Button_Shield.SetActive(false);
        }

        public void Show_Button_Shield()
        {
            if (GameManager.Instance.controllerType == ControllerType.Mobile)
            {
                Button_Shield.SetActive(true);
            }
        }

        public void Show_Button_Interact()
        {
            Button_Interact.SetActive(true);
        }

        public void Hide_Button_Interact()
        {
            Button_Interact.SetActive(false);
        }

        public void Hide_Button_Hit()
        {
            Button_Hit.SetActive(false);
        }

        public void Show_Button_Hit()
        {
            if (GameManager.Instance.controllerType == ControllerType.Mobile)
            {
                Button_Hit.SetActive(true);
            }
        }

        public void Click_Button_Jump()
        {
            HeroController.instance.thirdPersonController.JumpNow();
        }

        public void Update_Mana_Bar(int Spended)
        {
            if (HeroController.instance == null || Image_Mana == null) return;
            Image_Mana.gameObject.SetActive(true);
            HeroController.instance.Mana = HeroController.instance.Mana - Spended;
            float total = HeroController.instance.TotalMana;
            Image_Mana.fillAmount = total > 0 ? HeroController.instance.Mana / total : 0f;
        }

        public GameObject Panel_Pause;

        public void PauseTheGame()
        {
            if (GameOverPanel.activeSelf) return;
            Panel_Pause.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Time.timeScale = 0;
            if(GameManager.Instance.controllerType == ControllerType.Mobile)
            {
                Button_Pause.SetActive(false);
            }
        }

        public void Click_Button_Continue()
        {
            if (GameOverPanel.activeSelf) return;
            Panel_Pause.SetActive(false);
            Time.timeScale = 1;
            if (GameManager.Instance.controllerType == ControllerType.Mobile)
            {
                Button_Pause.SetActive(true);
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        public void Click_Button_Quit()
        {
            SceneManager.LoadScene(MenuSceneName);
        }

        [Header("MENU / SAVE")]
        [Tooltip("Scene loaded by the pause menu's Main Menu button.")]
        public string MenuSceneName = "MenuScene";

        public void Click_Button_SaveGame()
        {
            if (GameSession.Instance == null)
            {
                Debug.LogError("[ARPS] No GameSession in the scene, so the game cannot be saved. Add the GameSession component to your GameManager object.", this);
                return;
            }
            GameSession.Instance.SaveNow();
        }

        public void Click_Button_MainMenu()
        {
            Time.timeScale = 1f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            SceneManager.LoadScene(MenuSceneName);
        }

        public void Click_Button_Inventory()
        {
            if (InventoryUI.Instance != null) InventoryUI.Instance.Toggle();
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.escapeAction == null) return;

            if(GameManager.Instance.escapeAction.WasReleasedThisFrame())
            {
                if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen)
                {
                    InventoryUI.Instance.Close();
                }
                else if (Time.timeScale == 0)
                {
                    Click_Button_Continue();
                }
                else
                {
                    PauseTheGame();
                }
            }
        }

        public void Click_Button_RestartGame()
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void Update_ProfileDetails(string name, Sprite profileImage)
        {
            if (Label_Name != null) Label_Name.text = name;
            if (Image_Profile != null) Image_Profile.sprite = profileImage;
        }

        public void Show_Grabbed_Text(string itemName)
        {
            Text_Grabbed.SetActive(false);
            if (Label_Grabbed != null) Label_Grabbed.text = "- " + itemName.ToUpper();
            Text_Grabbed.SetActive(true);
            if (hideGrabbedTextRoutine != null)
            {
                StopCoroutine(hideGrabbedTextRoutine);
            }
            hideGrabbedTextRoutine = StartCoroutine(Hide_Grabbed_TextAsync());
        }

        IEnumerator Hide_Grabbed_TextAsync()
        {
            yield return new WaitForSeconds(2);
            Text_Grabbed.SetActive(false);
            hideGrabbedTextRoutine = null;
        }

        public void Update_Text_Coin()
        {
            if (Text_Coin == null) return;
            // Read the hero's live value rather than PlayerPrefs: after GameSession.Apply()
            // restores a save, HeroController.Coin is the source of truth and PlayerPrefs
            // may still hold a value from another run.
            int coin = HeroController.instance != null ? HeroController.instance.Coin : PlayerPrefs.GetInt("Coin");
            Text_Coin.text = coin.ToString();
        }

        public void Show_Image_Potion(PotionType potionType)
        {
            switch (potionType)
            {
                case PotionType.Health:
                    Image_Health_Potion.SetActive(true);
                    break;
                case PotionType.Mana:
                    Image_Mana_Potion.SetActive(true);
                    break;
            }
        }

        public void Hide_Image_Potion(PotionType potionType)
        {
            switch (potionType)
            {
                case PotionType.Health:
                    Image_Health_Potion.SetActive(false);
                    break;
                case PotionType.Mana:
                    Image_Mana_Potion.SetActive(false);
                    break;
            }
        }

        public void Update_Health_Bar()
        {
            if (HeroController.instance != null && Image_Health != null)
            {
                float total = HeroController.instance.TotalHealth;
                Image_Health.fillAmount = total > 0 ? HeroController.instance.Health / total : 0f;
            }
        }

        public void Show_Damage_Effect()
        {
            Image_Damage.color = Color.clear;
            GameManager.Instance.ShakeTheCamera(0.25f);
            Image_Damage.color = Damage_Effect_Color;
            StartCoroutine(ResetDamageImageColor());
        }

        IEnumerator ResetDamageImageColor()
        {
            yield return new WaitForSeconds(0.5f);
            Image_Damage.color = Color.clear;
        }

        public void Show_Warning(String text)
        {
            Panel_Warning.SetActive(true);
            if (Label_Warning != null) Label_Warning.text = text;
            if (hideWarningRoutine != null)
            {
                StopCoroutine(hideWarningRoutine);
            }
            hideWarningRoutine = StartCoroutine(Hide_Warning());
        }

        IEnumerator Hide_Warning()
        {
            yield return new WaitForSecondsRealtime(1.5f);
            Panel_Warning.SetActive(false);
            hideWarningRoutine = null;
        }

        public void Show_EnemyHealthPanel(string name, int health, float totalhealth)
        {
            Panel_EnemyStatusInfo.SetActive(true);
            float newResult = (health > 0 && totalhealth > 0 ? health / totalhealth : 0);
            Image_EnemyHealth.fillAmount = newResult;
            Text_EnemyName.text = name;
            if (hideEnemyStatusRoutine != null)
            {
                StopCoroutine(hideEnemyStatusRoutine);
            }
            hideEnemyStatusRoutine = StartCoroutine(Hide_Panel_EnemyStatus());
        }

        IEnumerator Hide_Panel_EnemyStatus()
        {
            yield return new WaitForSeconds(4);
            Panel_EnemyStatusInfo.SetActive(false);
            hideEnemyStatusRoutine = null;
        }
    }
}