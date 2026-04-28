using System;
using TMPro;
using UnityEngine;

public class StoryPopup : MonoBehaviour, IInteractable
{

    [SerializeField]
    private TextMeshProUGUI text;
    
    SphereCollider sphereCollider;

    [SerializeField] private string storyText;
    
    private bool askOrShow = false;
    
    void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
    }
    
    public void DrawPopup()
    {
        
        if(!askOrShow)
        {
            
            text.SetText("Press E to read");
        }
        else
        {
             text.SetText(storyText);
        }
    }
    
    public void RemovePopup()
    {
        text.SetText("");
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            DrawPopup();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            RemovePopup();
            
            if(askOrShow)
            {
                askOrShow = false;
            }
        }
    }
    
    public void TogglePopup()
    {
        askOrShow = !askOrShow;
        DrawPopup();
    }
    
    public void Interact(Transform interactor)
    {
        TogglePopup();
    }
}
