using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [SerializeField] string nextScene;
    [SerializeField] float restartDelay = 1.2f;

    [Header("UI")]
    [SerializeField] Text triesText;
    [SerializeField] Text hpText;
    [SerializeField] Text timerText;
    [SerializeField] Text messageText;
    [SerializeField] GameObject winPanel;
    [SerializeField] Text winText;
    [SerializeField] GameObject nextButton;

    [Header("Sounds")]
    [SerializeField] AudioClip winSound;
    [SerializeField] AudioClip loseSound;

    public bool levelFinished;

    float timer;
    int tries;
    string sceneName;

    void Awake()
    {
        instance = this;
        Application.targetFrameRate = 60;
    }

    void Start()
    {
        sceneName = SceneManager.GetActiveScene().name;

        // count tries for the playtest (every load = new try)
        tries = PlayerPrefs.GetInt("Tries_" + sceneName, 0) + 1;
        PlayerPrefs.SetInt("Tries_" + sceneName, tries);

        triesText.text = "Попытка " + tries;
        messageText.text = "";
        winPanel.SetActive(false);
    }

    void Update()
    {
        Health hp = PlayerController.instance.health;
        hpText.text = "Жизни " + hp.currentHealth + "/" + hp.maxHealth;

        if (!levelFinished)
        {
            timer += Time.deltaTime;
            timerText.text = timer.ToString("F1") + " с";
        }
    }

    public void PlayerDied()
    {
        if (levelFinished) return;
        levelFinished = true;
        Sound.Play(loseSound);
        messageText.text = "Ты погиб";
        Invoke("Restart", restartDelay);
    }

    public void PlayerReachedExit()
    {
        if (levelFinished) return;
        levelFinished = true;
        Sound.Play(winSound);

        float best = PlayerPrefs.GetFloat("Best_" + sceneName, 9999f);
        if (timer < best)
        {
            best = timer;
            PlayerPrefs.SetFloat("Best_" + sceneName, best);
        }
        PlayerPrefs.DeleteKey("RandomSeed"); // next random level will be different

        winPanel.SetActive(true);
        nextButton.SetActive(nextScene != "");
        winText.text = "Уровень пройден\n\nПопыток: " + tries + "\nВремя: " + timer.ToString("F1") + " с\nРекорд: " + best.ToString("F1") + " с";
    }

    public void Restart()
    {
        SceneManager.LoadScene(sceneName);
    }

    public void LoadNext()
    {
        SceneManager.LoadScene(nextScene);
    }

    public void LoadMenu()
    {
        SceneManager.LoadScene("Menu");
    }

    // button for the playtest: reset tries before a new person plays
    public void NewTester()
    {
        PlayerPrefs.DeleteKey("Tries_" + sceneName);
        PlayerPrefs.DeleteKey("RandomSeed");
        Restart();
    }
}
