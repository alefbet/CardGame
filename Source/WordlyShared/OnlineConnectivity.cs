#if WINDOWS_UAP
using com.shephertz.app42.paas.sdk.windows;
using com.shephertz.app42.paas.sdk.windows.game;
using com.shephertz.app42.paas.sdk.windows.social;
#else
using com.shephertz.app42.paas.sdk.csharp;
using com.shephertz.app42.paas.sdk.csharp.game;
using com.shephertz.app42.paas.sdk.csharp.social;
#endif

using System;
using System.Collections.Generic;
using System.Text;
using com.shephertz.app42.gaming.multiplayer.client.listener;
using com.shephertz.app42.gaming.multiplayer.client.events;
using com.shephertz.app42.gaming.multiplayer.client.command;
using System.Diagnostics;
using com.shephertz.app42.gaming.multiplayer.client;


namespace WordGame
{

    public class OnRoomJoinEventArgs
    {
        public Dictionary<string, object> Properties { get; set; }
        public String[] Users { get; set; }
        public String RoomOwner { get; set; }
        public String RoomId { get; set; }
    }

#if WINDOWS_UAP
    public class OnSaveScoreCallback : App42Callback
    {
        public string foo {get;set;}
        
         void App42Callback.OnException(App42Exception exception)
        {
            
        }
         void App42Callback.OnSuccess(Object response)
        {
            
        }
    }
#endif

    public class OnlineConnectivity : ConnectionRequestListener, RoomRequestListener, NotifyListener, ZoneRequestListener, ChatRequestListener, UpdateRequestListener
    {
        string APP42_APPKEY = "33c63376a33cb750ce10d9e136df698975bcb56f9b59c46dd7490d10c879be4d";
        string APP42_APPSECRET = "67a2a211044b024871e84a2ce799b37c8c1596e5b939c53313863e1551508441";
        ServiceAPI serviceAPI;
        ScoreBoardService scoreSvc;
        SocialService socialSvc;

        public string CurrentUser
            {
                get
            {
                string user;
#if __ANDROID__
                user = Android.OS.Build.Serial;
#elif WINDOW_UAP
          
                if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.System.Profile.HardwareIdentification"))
                {
                    var token = HardwareIdentification.GetPackageSpecificToken(null);
                    var hardwareId = token.Id;
                    var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(hardwareId);

                    byte[] bytes = new byte[hardwareId.Length];
                    dataReader.ReadBytes(bytes);

                    user =  BitConverter.ToString(bytes).Replace("-", "");
                }
                else
                    user = Plugin.Settings.CrossSettings.Current.GetValueOrDefault("UserName", Guid.NewGuid().ToString());
            
                }
}
#else
                user = Plugin.Settings.CrossSettings.Current.GetValueOrDefault("UserName", Guid.NewGuid().ToString());
                Plugin.Settings.CrossSettings.Current.AddOrUpdateValue("UserName", user);
#endif
                return user;
            }
            }
    

        public EventHandler<String> UserJoined;
        public EventHandler<String> UserLeft;
        public EventHandler<OnRoomJoinEventArgs> GetInitialRoomState;
        public EventHandler<Dictionary<String, Object>> RoomStateChanged;
        public EventHandler<String> RecievedGameMessage;

        string roomId="";
        bool sentInitialRoomProperties = false;

        public  string GetRoomForGame (bool createRoom = false)
        {
            if (WarpClient.GetInstance().GetConnectionState() == 0)
            {
                
            }
            else
            {
                try
                {
                    WarpClient.GetInstance().Connect(CurrentUser);
                }
                catch (Exception e)
                {

                }    
            }

            return roomId;
        }

        public void Disconnect()
        {
            try
            {
                WarpClient.GetInstance().Disconnect();
            }
            catch (Exception e)
            {
            }
        }

        public OnlineConnectivity()
        {
            try
            {

                serviceAPI = new ServiceAPI(APP42_APPKEY, APP42_APPSECRET);
                scoreSvc = serviceAPI.BuildScoreBoardService();


                WarpClient.initialize(APP42_APPKEY, APP42_APPSECRET);
                WarpClient.GetInstance().AddConnectionRequestListener(this);
                WarpClient.GetInstance().AddRoomRequestListener(this);
                WarpClient.GetInstance().AddZoneRequestListener(this);
                WarpClient.GetInstance().AddNotificationListener(this);
                WarpClient.GetInstance().AddChatRequestListener(this);
                WarpClient.GetInstance().AddUpdateRequestListener(this);
            }
            catch (Exception e)
            {

            }
        }
                       

        public bool SubmitScore(string Level, int Score)
        {

#if WINDOWS_UAP
            var c = new OnSaveScoreCallback();
            scoreSvc.SaveUserScore(Level, CurrentUser, Score, c);
#else
            Game game = scoreSvc.SaveUserScore(Level, CurrentUser, Score);
#endif

            return true;
        }

        public void Connect()
        {
            WarpClient.GetInstance().Connect(CurrentUser);

        } 

        public void onConnectDone(ConnectEvent eventObj)
        {
            switch (eventObj.getResult())
            {
                case WarpResponseResultCode.SUCCESS:
                    // Launch Room
                    Debug.WriteLine("Connection success: " + eventObj.getResult().ToString());
                    WarpClient.GetInstance().JoinRoomInRange(0, 3, true);                                                     
                    break;
                default:
                    Debug.WriteLine("Connection issue: " + eventObj.getResult().ToString());
                    break;                    
            }
        }

        public void onDisconnectDone(ConnectEvent eventObj)
        {
            throw new NotImplementedException();
        }

        public void onInitUDPDone(byte resultCode)
        {
            throw new NotImplementedException();
        }

        public void onSubscribeRoomDone(RoomEvent eventObj)
        {
            //throw new NotImplementedException();
            if (roomId != eventObj.getData().getId())
                throw new Exception("Subscribed to wrong room");
            Debug.WriteLine("Room " + eventObj.getData().getId() + " found, subscribed");
            sentInitialRoomProperties = false;
            WarpClient.GetInstance().GetLiveRoomInfo(roomId);
            
        }

        public void onUnSubscribeRoomDone(RoomEvent eventObj)
        {
            throw new NotImplementedException();
        }

        public void onPrivateUpdateReceived(string s , byte[] b, bool fBool)
        {
            throw new NotImplementedException();
        }

        public void onNextTurnRequest(string s)            
        {
            throw new NotImplementedException();
        }

        public void onSendPrivateUpdateDone(byte b)
        {
            throw new NotImplementedException();
        }

        public void onJoinRoomDone(RoomEvent eventObj)
        {
            switch (eventObj.getResult())
            {
                case (WarpResponseResultCode.SUCCESS):

                    roomId = eventObj.getData().getId();
                    Debug.WriteLine("Room " + roomId + " found, joined, room owner:" + eventObj.getData().getRoomOwner());
                    
                    WarpClient.GetInstance().SubscribeRoom(roomId);
                    break;
                case (WarpResponseResultCode.RESOURCE_NOT_FOUND):
                    Debug.WriteLine("Room not found, try to create");
                    WarpClient.GetInstance().CreateRoom("PlayNow", CurrentUser, 4, null);
                    break;
                default:
                    Debug.WriteLine("GetLiveRoom Info: " + eventObj.getData() + eventObj.getResult());
                    break;
            }
        }

        public void onLeaveRoomDone(RoomEvent eventObj)
        {
            throw new NotImplementedException();
        }

        public void onGetLiveRoomInfoDone(LiveRoomInfoEvent eventObj)
        {
            if (!sentInitialRoomProperties)
            {                
                GetInitialRoomState?.Invoke(this, new OnRoomJoinEventArgs() { Properties = eventObj.getProperties(), Users = eventObj.getJoinedUsers(), RoomOwner = eventObj.getData().getRoomOwner(), RoomId = eventObj.getData().getId() });
                sentInitialRoomProperties = true;
            }
        }

        public void onSetCustomRoomDataDone(LiveRoomInfoEvent eventObj)
        {
            throw new NotImplementedException();
        }

        public void onUpdatePropertyDone(LiveRoomInfoEvent lifeLiveRoomInfoEvent)
        {
            throw new NotImplementedException();
        }

        public void onLockPropertiesDone(byte result)
        {
            throw new NotImplementedException();
        }

        public void onUnlockPropertiesDone(byte result)
        {
            throw new NotImplementedException();
        }

        public void onSendMoveDone(byte result)
        {
            throw new NotImplementedException();
        }

        public void onStartGameDone(byte result)
        {
            throw new NotImplementedException();
        }

        public void onStopGameDone(byte result)
        {
            throw new NotImplementedException();
        }

        public void onGetMoveHistoryDone(byte result, MoveEvent[] moves)
        {
            throw new NotImplementedException();
        }

        public void onRoomCreated(RoomData eventObj)
        {
            throw new NotImplementedException();
        }

        public void onRoomDestroyed(RoomData eventObj)
        {
            throw new NotImplementedException();
        }

        public void onUserLeftRoom(RoomData eventObj, string username)
        {
            Debug.WriteLine("User left room: " + username);
            UserLeft?.Invoke(this, username);
        }

        public void onUserJoinedRoom(RoomData eventObj, string username)
        {
            Debug.WriteLine("User joined room: " + username);
            UserJoined?.Invoke(this, username);
        }

        public void onUserLeftLobby(LobbyData eventObj, string username)
        {
            throw new NotImplementedException();
        }

        public void onUserJoinedLobby(LobbyData eventObj, string username)
        {
            throw new NotImplementedException();
        }

        public void onChatReceived(ChatEvent eventObj)
        {
            throw new NotImplementedException();
        }

        public void onUpdatePeersReceived(UpdateEvent eventObj)
        {
            throw new NotImplementedException();
        }

        public void onUserChangeRoomProperty(RoomData roomData, string sender, Dictionary<string, object> properties, Dictionary<string, string> lockedPropertiesTable)
        {
            throw new NotImplementedException();
        }

        public void onPrivateChatReceived(string sender, string message)
        {
            throw new NotImplementedException();
        }

        public void onMoveCompleted(MoveEvent moveEvent)
        {
            throw new NotImplementedException();
        }

        public void onUserPaused(string locid, bool isLobby, string username)
        {
            throw new NotImplementedException();
        }

        public void onUserResumed(string locid, bool isLobby, string username)
        {
            throw new NotImplementedException();
        }

        public void onGameStarted(string sender, string roomId, string nextTurn)
        {
            throw new NotImplementedException();
        }

        public void onGameStopped(string sender, string roomId)
        {
            throw new NotImplementedException();
        }

        public void onDeleteRoomDone(RoomEvent eventObj)
        {
            throw new NotImplementedException();
        }

        public void onGetAllRoomsDone(AllRoomsEvent eventObj)
        {
            throw new NotImplementedException();
        }

        public void onCreateRoomDone(RoomEvent eventObj)
        {
            WarpClient.GetInstance().JoinRoom(eventObj.getData().getId());
        }

        public void onGetOnlineUsersDone(AllUsersEvent eventObj)
        {
            throw new NotImplementedException();
        }

        public void onGetLiveUserInfoDone(LiveUserInfoEvent eventObj)
        {
            throw new NotImplementedException();
        }

        public void onSetCustomUserDataDone(LiveUserInfoEvent eventObj)
        {
            throw new NotImplementedException();
        }

        public void onGetMatchedRoomsDone(MatchedRoomsEvent matchedRoomsEvent)
        {
            throw new NotImplementedException();
        }

        public void onSendChatDone(byte result)
        {
            throw new NotImplementedException();
        }

        public void onSendPrivateChatDone(byte result)
        {
            throw new NotImplementedException();
        }

        public void onSendUpdateDone(byte result)
        {
            throw new NotImplementedException();
        }
    }
}
