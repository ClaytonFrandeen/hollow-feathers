using UnityEngine;

public class RestPoint : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform restPosition;
    private bool isResting = false;
    public void Interact(Transform interactor)
    {   
        PlayerRest playerRest = interactor.GetComponent<PlayerRest>();
        if (isResting)
        {
            playerRest.EndRest();
            isResting = false;
            return;
        }

        if (playerRest != null)
        {
            playerRest.StartResting(restPosition);
            isResting = true;
        }
    }
}
