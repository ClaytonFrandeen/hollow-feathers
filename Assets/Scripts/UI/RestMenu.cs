using UnityEngine;
using UnityEngine.UI;

public class RestMenu : MonoBehaviour
{
    [SerializeField] public Button increaseConstitution;
    [SerializeField] public Button decreaseConstitution;
    [SerializeField] public Button increaseEndurance;
    [SerializeField] public Button decreaseEndurance;
    [SerializeField] public Button increaseStrength;
    [SerializeField] public Button decreaseStrength;
    [SerializeField] public GameObject playerUI;

    void Start()
    {
        gameObject.SetActive(false);
    }
    public void OpenMenu()
    {
        playerUI.SetActive(false);
        gameObject.SetActive(true);
    }


}
