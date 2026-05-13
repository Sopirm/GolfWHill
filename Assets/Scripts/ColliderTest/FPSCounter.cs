using UnityEngine;
using UnityEngine.UI; // Добавляем using для UI
using TMPro; // Добавляем using для TextMeshPro
using System.Collections;

public class FPSCounter : MonoBehaviour
{
    public float updateInterval = 0.5f; // Интервал обновления FPS
    private float accum = 0; // Накопитель времени
    private int frames = 0; // Накопитель кадров
    private float timeleft; // Оставшееся время до обновления
    public TMPro.TextMeshProUGUI fpsText; // Ссылка на UI TextMeshProUGUI элемент

    void Start()
    {
        if (fpsText == null)
        {
            Debug.LogWarning("FPSCounter: Assign a UI Text element to fpsText in the Inspector.");
            enabled = false;
            return;
        }
        timeleft = updateInterval;
    }

    void Update()
    {
        timeleft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        ++frames;

        // Обновляем каждые updateInterval
        if (timeleft <= 0.0f)
        {
            float fps = accum / frames;
            string format = System.String.Format("{0:F2} FPS", fps);
            fpsText.text = format;

            if (fps < 30)
                fpsText.color = Color.yellow;
            else if (fps < 10)
                fpsText.color = Color.red;
            else
                fpsText.color = Color.green;

            timeleft = updateInterval;
            accum = 0.0f;
            frames = 0;
        }
    }
}
