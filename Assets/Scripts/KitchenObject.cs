using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class KitchenObject : NetworkBehaviour {


    [SerializeField] private KitchenObjectSO kitchenObjectSO;


    private IKitchenObjectParent kitchenObjectParent;
    private FollowTransform followTransform;

    protected virtual void Awake()
    {
        followTransform = GetComponent<FollowTransform>();
    }
    public KitchenObjectSO GetKitchenObjectSO() {
        return kitchenObjectSO;
    }

    //Local function calls ServerRpc
    public void SetKitchenObjectParent(IKitchenObjectParent kitchenObjectParent)
    {
        SetKitchenParentServerRpc(kitchenObjectParent.GetNetworkObject());    
    } 

    //ServerRpc broadcasts this to all ClientRpc
    [ServerRpc(RequireOwnership =false)]
    private void SetKitchenParentServerRpc(NetworkObjectReference kitchenObjectParentNetworkObjectReference)
    {
        SetKitchenObjectParentClientRpc(kitchenObjectParentNetworkObjectReference);        
    }

    //ClientRpc recieves broadcast from server and synchs the parenting
    [ClientRpc]
    private void SetKitchenObjectParentClientRpc(NetworkObjectReference kitchenObjectParentNetworkObjectReference)
    {
        //First convert the NetworkObjectReference back to a NetworkObject
        kitchenObjectParentNetworkObjectReference.TryGet(out NetworkObject kitchenObjectParentNetworkObject);

        //Access the IKitchenObjectParent component that was originally there:
        IKitchenObjectParent kitchenObjectParent = kitchenObjectParentNetworkObject.GetComponent<IKitchenObjectParent>();
        
        if (this.kitchenObjectParent != null) {
            this.kitchenObjectParent.ClearKitchenObject();
        }

        this.kitchenObjectParent = kitchenObjectParent;

        if (kitchenObjectParent.HasKitchenObject()) {
            Debug.LogError("IKitchenObjectParent already has a KitchenObject!");
        }

        kitchenObjectParent.SetKitchenObject(this);

        followTransform.SetTargetTransform(kitchenObjectParent.GetKitchenObjectFollowTransform());

    }


    public IKitchenObjectParent GetKitchenObjectParent() {
        return kitchenObjectParent;
    }

    public void DestroySelf() {
        kitchenObjectParent.ClearKitchenObject();

        Destroy(gameObject);
    }

    public bool TryGetPlate(out PlateKitchenObject plateKitchenObject) {
        if (this is PlateKitchenObject) {
            plateKitchenObject = this as PlateKitchenObject;
            return true;
        } else {
            plateKitchenObject = null;
            return false;
        }
    }


    //Changing to Passtrough function. Calls the RPC version on the KitchenGameMultiplayer Manager class
    public static void SpawnKitchenObject(KitchenObjectSO kitchenObjectSO, IKitchenObjectParent kitchenObjectParent) {
        KitchenGameMultiplayer.Instance.SpawnKitchenObject( kitchenObjectSO, kitchenObjectParent);        
    }

}