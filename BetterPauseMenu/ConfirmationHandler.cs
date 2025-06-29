using UnityEngine;
using System;

public class ConfirmationHandler : MonoBehaviour
{
    public Action onConfirm;

    public void Confirm()
    {
        onConfirm?.Invoke();
        Destroy(this); // clean up after use
    }

    public void Cancel()
    {
        Destroy(this); // clean up after use
    }
}
