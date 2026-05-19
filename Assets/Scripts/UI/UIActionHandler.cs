using UnityEngine;
using TMPro;

public class UIActionHandler : MonoBehaviour
{
    public Renderer targetRenderer;
    public Transform targetObject;
    public TMP_Text statusText;

    private bool isGreen = false;

    public void ChangeColor()
    {
        if (targetRenderer == null)
            return;

        isGreen = !isGreen;
        targetRenderer.material.color = isGreen ? Color.green : Color.white;

        if (statusText != null)
            statusText.text = "Цвет объекта изменён.";
    }

    public void MoveObject()
    {
        if (targetObject == null)
            return;

        targetObject.position += new Vector3(0f, 0f, 1f);

        if (statusText != null)
            statusText.text = "Объект перемещён.";
    }

    public void ShowMessage()
    {
        Debug.Log("Кнопка интерфейса вызвала метод ShowMessage().");

        if (statusText != null)
            statusText.text = "Сообщение выведено в Console.";
    }
}