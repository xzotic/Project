using UnityEngine;

namespace AdvancedRogueLikeandPuzzleSystem
{
    public class KeyScript : MonoBehaviour
    {
        public int Key_ID = 0;
        public string Name;

        public void GrabIt()
        {
            HeroController.instance.Keys_Grabbed.Add(Key_ID);
            if (GameCanvas_Controller.instance != null) GameCanvas_Controller.instance.Show_Grabbed_Text(Name);
            if (AudioManager.instance != null) AudioManager.instance.Play_Grab();
            GameSession.MarkConsumed(gameObject);
            Destroy(gameObject);
        }
    }
}