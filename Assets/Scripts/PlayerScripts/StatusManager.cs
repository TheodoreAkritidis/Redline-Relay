using UnityEngine;
using System.Collections.Generic;

public class StatusManager : MonoBehaviour
{
    [SerializeField] private PlayerHUD hud;

    private List<GameObject> statusArray = new List<GameObject>() {null, null, null, null};
    private List<float> xArray = new List<float> {0f, 77.7f, 155.4f, 233f};

    void Awake( )
    {
        if ( hud == null )
        {
            hud = FindFirstObjectByType<PlayerHUD>();
        }
    }

    // Find index of first empty status icon slot. If array full, return -1;
    private int GetFirstEmptySlot( )
    {
        for ( int i = 0; i < 4; i++ )
        {
            if ( statusArray[i] == null )
            {
                return i;
            }
        }

        return -1;
    }

    // Find index of desired icon. If icon isn't found, return -1;
    private int FindIconSlot( GameObject icon )
    {
        for ( int i = 0; i < 4; i++ )
        {
            if ( statusArray[i] == icon )
            {
                return i;
            }
        }

        return -1;
    }

    // Set new status icon at location of first empty status slot.
    public void SetNewStatusIcon( GameObject icon )
    {
        int index = GetFirstEmptySlot();

        if ( index < 0 )
        {
            return;
        }

        statusArray[index] = icon;

        float x = xArray[index];
        icon.transform.Translate(x, 0, 0);
        icon.SetActive(true);
    }

    // Remove status icon and reorganize any existing icons.
    public void RemoveStatusIcon( GameObject icon )
    {
        int index = FindIconSlot(icon);

        if ( index < 0 )
        {
            return;
        }

        GameObject _icon = statusArray[index];
        _icon.SetActive(false);

        statusArray[index] = null;
        
        index = GetFirstEmptySlot();
        statusArray.RemoveAt(index);
        statusArray.Add(null);
    }

}