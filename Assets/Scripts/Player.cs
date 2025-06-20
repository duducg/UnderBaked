using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour, IKitchenObjectParent {

    //Static so this event belongs to class and not any specific player
    public static event EventHandler OnAnyPlayerSpawned;
    public static event EventHandler OnAnyPickedSomething;
    //Reset data function that's called when changing scenes. Clears events   
    public static void ResetStaticData() {
        OnAnyPlayerSpawned = null;
        OnAnyPickedSomething =null;
    }

    public static Player LocalInstance { get; private set; }
   
    public event EventHandler OnPickedSomething;
    public event EventHandler<OnSelectedCounterChangedEventArgs> OnSelectedCounterChanged;
    public class OnSelectedCounterChangedEventArgs : EventArgs {
        public BaseCounter selectedCounter;
    }


    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private LayerMask countersLayerMask;
    [SerializeField] private LayerMask collisionsLayerMask;
    [SerializeField] private Transform kitchenObjectHoldPoint;
    [SerializeField] private List<Vector3> spawnPositionsList;


    private bool isWalking;
    private Vector3 lastInteractDir;
    private BaseCounter selectedCounter;
    private KitchenObject kitchenObject;


    private void Start() {
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
        GameInput.Instance.OnInteractAlternateAction += GameInput_OnInteractAlternateAction;
    }

    private void GameInput_OnInteractAlternateAction(object sender, EventArgs e) {
        if (!KitchenGameManager.Instance.IsGamePlaying()) return;

        if (selectedCounter != null) {
            selectedCounter.InteractAlternate(this);
        }
    }
    public override void OnNetworkSpawn()
    {
        //When this object is spawned in the network &
        //The current client is the "Owner of the spawn":
        //Grab the instance and atribute it as such:
        if (IsOwner)
        {
            LocalInstance = this;
        }
        transform.position = spawnPositionsList[(int)OwnerClientId];
        
        OnAnyPlayerSpawned?.Invoke(this, EventArgs.Empty);
        //To know when a player disconnects:
        //This callback is ran on the server and on the client that disconects.
        //The most important listener is the server. We don't really care what happens to the disconecting client's game...
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
        }
        
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
        //If player was holding anything destroy that object on disconnect:
        if(clientId == OwnerClientId || HasKitchenObject())
        {
            KitchenObject.DestroyKitchenObject(GetKitchenObject());
        }
    }

    private void GameInput_OnInteractAction(object sender, System.EventArgs e) {
        if (!KitchenGameManager.Instance.IsGamePlaying()) return;

        if (selectedCounter != null) {
            selectedCounter.Interact(this);
        }
    }

    private void Update() {
        if(!IsOwner)
        {
            return; //If not owner (host) player then return
        }        
        HandleMovement();
        HandleInteractions();
    }

    public bool IsWalking() {
        return isWalking;
    }

    private void HandleInteractions() {
        Vector2 inputVector =  GameInput.Instance.GetMovementVectorNormalized();
        
        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

        if (moveDir != Vector3.zero) 
        {
            lastInteractDir = moveDir;
            
        }

        float interactDistance = 2f;
        if (Physics.Raycast(transform.position, lastInteractDir, out RaycastHit raycastHit, interactDistance, collisionsLayerMask)) {
            if (raycastHit.transform.TryGetComponent(out BaseCounter baseCounter)) {
                // Has ClearCounter
                if (baseCounter != selectedCounter) {
                    SetSelectedCounter(baseCounter);
                    
                }
            } else {
                SetSelectedCounter(null);

            }
        } else {
            SetSelectedCounter(null);
        }
    }   

    
    private void HandleMovement() {
        Vector2 inputVector =  GameInput.Instance.GetMovementVectorNormalized();

        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

        float moveDistance = moveSpeed * Time.deltaTime;
        float playerRadius = .7f;
        float playerHeight = 2f;
        bool canMove = !Physics.BoxCast(transform.position,Vector3.one*playerRadius, moveDir,Quaternion.identity, moveDistance,collisionsLayerMask);

        if (!canMove) {
            // Cannot move towards moveDir

            // Attempt only X movement
            Vector3 moveDirX = new Vector3(moveDir.x, 0, 0).normalized;
            canMove = (moveDir.x < -.5f || moveDir.x > +.5f) && !Physics.BoxCast(transform.position,Vector3.one*playerRadius, moveDirX,Quaternion.identity, moveDistance,collisionsLayerMask); 

            if (canMove) {
                // Can move only on the X
                moveDir = moveDirX;
            } else {
                // Cannot move only on the X

                // Attempt only Z movement
                Vector3 moveDirZ = new Vector3(0, 0, moveDir.z).normalized;
                canMove = (moveDir.z < -.5f || moveDir.z > +.5f) && !Physics.BoxCast(transform.position,Vector3.one*playerRadius, moveDirZ,Quaternion.identity, moveDistance,collisionsLayerMask); 

                if (canMove) {
                    // Can move only on the Z
                    moveDir = moveDirZ;
                } else {
                    // Cannot move in any direction
                }
            }
        }

        if (canMove) {
            transform.position += moveDir * moveDistance;
        }

        isWalking = moveDir != Vector3.zero;

        float rotateSpeed = 10f;
        transform.forward = Vector3.Slerp(transform.forward, moveDir, Time.deltaTime * rotateSpeed);
    }

    private void SetSelectedCounter(BaseCounter selectedCounter) {
        this.selectedCounter = selectedCounter;

        OnSelectedCounterChanged?.Invoke(this, new OnSelectedCounterChangedEventArgs {
            selectedCounter = selectedCounter
        });
    }

    public Transform GetKitchenObjectFollowTransform() {
        return kitchenObjectHoldPoint;
    }

    public void SetKitchenObject(KitchenObject kitchenObject) {
        this.kitchenObject = kitchenObject;

        if (kitchenObject != null) {
            OnPickedSomething?.Invoke(this, EventArgs.Empty);
            OnAnyPickedSomething?.Invoke(this, EventArgs.Empty);
        }
    }

    public KitchenObject GetKitchenObject() {
        return kitchenObject;
    }

    public void ClearKitchenObject() {
        kitchenObject = null;
    }

    public bool HasKitchenObject() {
        return kitchenObject != null;
    }

    //Player is already a networkBehavior so we can directly return it's NetworkObject instance like this:
    public NetworkObject GetNetworkObject()
    {
        return NetworkObject;
    }


}