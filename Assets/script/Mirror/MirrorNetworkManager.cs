using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using Unity.Mathematics;

public class MirrorNetworkManager : NetworkManager
{
    private int playerCount = 0;
    public int maxPlayerCount = 1;


    /// <lobby>
    public FadeInOutScreen fadeInOut;
    public string firstSceneToLoad;
    private string[] sceneToLoad;
    private bool subScenesLoaded;
    private readonly List<Scene> subScene = new List<Scene>();

    private List<NetworkConnectionToClient> networkConnectionToClientsList = new List<NetworkConnectionToClient>();
    private bool isInTransition;
    private bool isLoadingScene =false;
    private bool firstSceneLoaded;
    /// </lobby>
    private void Start()
    {
        Transform firstChild = transform.GetChild(0);
        firstChild.gameObject.SetActive(true);


        /// <lobby>
        int sceneCount = SceneManager.sceneCountInBuildSettings - 2;
        sceneToLoad = new string[sceneCount];

        for(int i =0 ;i < sceneCount ; i++){
            sceneToLoad[i] = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i+2)); 
        }
        /// </lobby>
    }

    /// <lobby>

    // public override void OnServerSceneChanged(string sceneName)
    // {
    //     isLoadingScene = true;
    //     base.OnServerSceneChanged(sceneName);

    //     if(fadeInOut !=null)
    //         fadeInOut.ShowScreenNoDelay();
    //     else
    //         Debug.LogError("Fade in out not found -- object missing");

    //     if(sceneName == "LobbyWaitingRoom"){
    //         StartCoroutine(ServerLoadSubScenes());
    //     }
    // }

    public override void OnClientSceneChanged()
    {
        if(isInTransition == false){
            base.OnClientSceneChanged(); 
        }
    }

    IEnumerator ServerLoadSubScenes(){
        foreach(var additiveScene in sceneToLoad){
            yield return SceneManager.LoadSceneAsync(additiveScene, new LoadSceneParameters{
                loadSceneMode = LoadSceneMode.Additive,
                localPhysicsMode = LocalPhysicsMode.Physics3D
            });
        }
        isLoadingScene = false;
        subScenesLoaded = true;
    }

    public override void OnClientChangeScene(string sceneName, SceneOperation sceneOperation, bool customHandling)
    {
        if(sceneOperation == SceneOperation.UnloadAdditive){
            StartCoroutine(UnloadAdditive(sceneName));
        }
        if(sceneOperation == SceneOperation.LoadAdditive){
            StartCoroutine(LoadAdditive(sceneName));
        }
        
    }

    IEnumerator LoadAdditive(string sceneName){
        isInTransition = true;

        yield return fadeInOut.FadeIn();
        if(mode == NetworkManagerMode.ClientOnly){
            loadingSceneAsync = SceneManager.LoadSceneAsync(sceneName , LoadSceneMode.Additive);

            while(loadingSceneAsync != null && !loadingSceneAsync.isDone){
                yield return null;
            }
        }

        NetworkClient.isLoadingScene = false;
        isInTransition = false;

        OnClientSceneChanged();

        if(firstSceneLoaded == false){
            firstSceneLoaded = true;
            yield return new WaitForSeconds(0.6f);
        }
        else{
            firstSceneLoaded = true;
            yield return new WaitForSeconds(0.5f);
        }

        yield return fadeInOut.FadeOut();
    }

    IEnumerator UnloadAdditive(string sceneName){
        isInTransition = true;

        yield return fadeInOut.FadeIn();

        if(mode == NetworkManagerMode.ClientOnly){
            yield return SceneManager.UnloadSceneAsync(sceneName);
            yield return Resources.UnloadUnusedAssets();
        }

        NetworkClient.isLoadingScene = false;
         
    }

    IEnumerator AddPlayerDelayed(NetworkConnectionToClient conn){
        Debug.Log("Client is ready: 1111" );
        while(subScenesLoaded == false) yield return null;
        Debug.Log("Client is ready: 2222" );
        NetworkIdentity[] allObjectsWithNetworkIdentity = FindObjectsOfType<NetworkIdentity>();
        foreach(var item in allObjectsWithNetworkIdentity){
            item.enabled = true;
        }
        firstSceneLoaded = false;
        // conn.Send(new SceneManager{sceneName = firstSceneToLoad , sceneOperation = SceneOperation.LoadAdditive , customHandling = true});
        Vector3 start = new Vector3(0, 40f, 0);
        GameObject player = Instantiate(playerPrefab , start , quaternion.identity);

        yield return new WaitForEndOfFrame();
        
        SceneManager.MoveGameObjectToScene(player, SceneManager.GetSceneByName(firstSceneToLoad));
        NetworkServer.AddPlayerForConnection(conn , player);
            
    }

    IEnumerator AddPlayerDelayed1(NetworkConnectionToClient conn){
        Vector3 start = new Vector3(0, 40f, 0);
        GameObject player = Instantiate(playerPrefab , start , quaternion.identity);

        yield return new WaitForEndOfFrame();
        NetworkServer.AddPlayerForConnection(conn , player);

        networkConnectionToClientsList.Add(conn);

        if(networkConnectionToClientsList.Count == maxPlayerCount){
            isLoadingScene = true;

            if(fadeInOut !=null)
                fadeInOut.ShowScreenNoDelay();
            else
                Debug.LogError("Fade in out not found -- object missing");
                
            Debug.Log(SceneManager.GetActiveScene().name);
            
            if(SceneManager.GetActiveScene().name == "OfflineScene"){
                StartCoroutine(ServerLoadSubScenes());
            }   
        }
            
    }
    /// </lobby>

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        Debug.Log("Client connected: " + conn.connectionId);
    }

    public override void OnServerReady(NetworkConnectionToClient conn)
    {
        base.OnServerReady(conn);
        Debug.Log("Client is ready: " + conn.connectionId);

        /// <lobby>
        if(conn.identity == null){
            // if(!isLoadingScene)  LoadGameScene();
            this.gameObject.GetComponentInChildren<Canvas>().enabled = false;
            StartCoroutine(AddPlayerDelayed1(conn));
        }
        /// </lobby>
    }

    // public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    // {
    //     Debug.Log("Entered OnServerAddPlayer");

    //     // Check if the connection already has a player
    //     if (conn.identity != null)
    //     {
    //         Debug.LogWarning("Connection already has a player");
    //         return;
    //     }

    //     Vector3 start = new Vector3(0, 40f, 0);
    //     GameObject player = Instantiate(playerPrefab, start, Quaternion.identity);
        
    //     NetworkServer.AddPlayerForConnection(conn, player);
    //     Debug.Log("Player spawned");
        
    //     playerCount++;
    //     if (playerCount == noOfPlayers)
    //     {
    //         LoadGameScene();
            
    //     }
    // }

    // public override void OnServerDisconnect(NetworkConnectionToClient conn)
    // {
    //     Debug.Log("OnServerDisconnect called");
    //     base.OnServerDisconnect(conn);

    //     playerCount--;

    //     if (playerCount == 0)
    //     {
    //         string newSceneName = "MirrorWaitingRoom"; // Replace with your scene name
    //         Debug.Log("Player Disconnected!!!");
    //         ServerChangeScene(newSceneName);
    //     }
    // }

    [Server]
    private void LoadGameScene()
    {
        string newSceneName = "LobbyWaitingRoom"; // Replace with your scene name
        ServerChangeScene(newSceneName);
    }

    // public override void OnStopClient()
    // {
    //     base.OnStopClient();
    //     Debug.Log("Client has stopped.");
    // }
}
