using KH;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Inspector-only way to ask for a confirmation: fill in the texts, wire a Button's OnClick to KH_Ask,
/// and hook onConfirm / onCancel to whatever should happen.
/// </summary>
[AddComponentMenu("KH/UI/" + nameof(KHConfirmTrigger))]
public class KHConfirmTrigger : KHManagedBehaviour
{
    [Tooltip("Optional. If empty, the KHUIManager on a parent Canvas is used.")]
    [SerializeField] private KHUIManager manager;

    [Header("Texts (leave empty to hide / use the prefab default)")]
    [SerializeField, TextArea] private string message = "Are you sure?";
    [SerializeField] private string confirmText = "Yes";
    [SerializeField] private string cancelText = "No";

    [Header("Events")]
    public UnityEvent onConfirm;
    public UnityEvent onCancel;

    public void KH_Ask()
    {
        if (manager == null)
            manager = GetComponentInParent<KHUIManager>(true);

        if (manager == null)
        {
            Debug.LogError($"No {nameof(KHUIManager)} found. Assign one or place this under the main Canvas.", this);
            return;
        }

        manager.Confirm(
            message: message,
            onConfirm: () => onConfirm?.Invoke(),
            onCancel: () => onCancel?.Invoke(),
            confirmText: confirmText,
            cancelText: cancelText);
    }

    // Lets other events change the message at runtime (e.g. from a localization system).
    public void KH_SetMessage(string newMessage) => message = newMessage;
}
