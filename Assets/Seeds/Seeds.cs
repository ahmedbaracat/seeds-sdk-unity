﻿using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using UnityEngine;

public class Seeds : MonoBehaviour
{
    public static Seeds Instance { get; private set; }

    /// <summary>
    /// True if trace should be enabled, false otherwise.
    /// </summary>
    public bool TraceEnabled = false;

    /// <summary>
    /// True if Seeds SDK should auto-initialize during instantiation, false otherwise.
    /// </summary>
    public bool AutoInitialize = false;

    /// <summary>
    /// Server URL. Do not include trailing slash.
    /// </summary>
    public string ServerURL = "http://dash.playseeds.com";

    /// <summary>
    /// Application API key.
    /// </summary>
    public string ApplicationKey;

    public event Action OnInAppMessageClicked;
    public event Action OnInAppMessageClosedComplete;
    public event Action OnInAppMessageClosedIncomplete;
    public event Action OnInAppMessageLoadSucceeded;
    public event Action OnInAppMessageShownSuccessfully;
    public event Action OnInAppMessageShownInsuccessfully;
    public event Action OnNoInAppMessageFound;

    #if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject androidInstance;
    private AndroidJavaObject androidBridgeInstance;
    #endif

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Duplicate instance of Seeds. New instance will be destroyed.");
            enabled = false;
            DestroyObject(this);
            return;
        }

        DontDestroyOnLoad(this);
        Instance = this;
    }

    #if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern void Seeds_SetGameObjectName(string gameObjectName);

    [DllImport ("__Internal")]
    private static extern void Seeds_Setup(bool registerAsPlugin);
    #endif

    void Start()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        using (var seedsClass = new AndroidJavaClass("com.playseeds.android.sdk.Seeds"))
        {
            androidInstance = seedsClass.CallStatic<AndroidJavaObject>("sharedInstance");
        }
        using (var inAppMessageListenerBridgeClass = new AndroidJavaClass("com.playseeds.unity3d.androidbridge.InAppMessageListenerBridge"))
        {
            androidBridgeInstance = inAppMessageListenerBridgeClass.CallStatic<AndroidJavaObject>("create", gameObject.name);
        }
        #elif UNITY_IOS && !UNITY_EDITOR
        Seeds_SetGameObjectName(gameObject.name);
        Seeds_Setup(false);
        #endif

        if (AutoInitialize)
            Init(ServerURL, ApplicationKey);

        #if UNITY_ANDROID && !UNITY_EDITOR
        NotifyOnStart();
        #endif
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (TraceEnabled)
            Debug.Log(string.Format("[Seeds] OnApplicationPause({0})", pauseStatus));

        #if UNITY_ANDROID && !UNITY_EDITOR
        if (pauseStatus)
            NotifyOnStop();
        else
            NotifyOnStart();
        #endif
    }

    #if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject CurrentActivity
    {
        get
        {
            using (var unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                return unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
            }
        }
    }

    private void RunOnUIThread(AndroidJavaRunnable runnable)
    {
        CurrentActivity.Call("runOnUiThread", new AndroidJavaRunnable(runnable));
    }

    private AndroidJavaObject CreateMapFromDicrionary(IDictionary<string, string> dictionary)
    {
        AndroidJavaObject map;
        using (var helpersClass = new AndroidJavaClass("com.playseeds.unity3d.androidbridge.Helpers"))
        {
            map = helpersClass.CallStatic<AndroidJavaObject>("createHashMapOfStringString");
        }

        foreach (var entry in dictionary)
        {
            map.Call<AndroidJavaObject>("put", entry.Key, entry.Value);
        }

        return map;
    }

    private AndroidJavaObject CreateListFromEnumerable(IEnumerable<string> enumerable)
    {
        AndroidJavaObject list;
        using (var helpersClass = new AndroidJavaClass("com.playseeds.unity3d.androidbridge.Helpers"))
        {
            list = helpersClass.CallStatic<AndroidJavaObject>("createArrayListOfString");
        }

        foreach (var item in enumerable)
        {
            list.Call<bool>("add", item);
        }

        return list;
    }
    #endif

    private static void NotImplemented(string method)
    {
        Debug.LogError(string.Format("Method {0} not implemented for current platform", method));
    }

    void inAppMessageClicked(string notUsed)
    {
        if (TraceEnabled)
            Debug.Log("[Seeds] OnInAppMessageClicked");

        if (OnInAppMessageClicked != null)
            OnInAppMessageClicked();
    }

    void inAppMessageClosedComplete(string inAppMessageId)
    {
        if (TraceEnabled)
            Debug.Log("[Seeds] OnInAppMessageClosedComplete");
        
        if (OnInAppMessageClosedComplete != null)
            OnInAppMessageClosedComplete();
    }

    void inAppMessageClosedIncomplete(string inAppMessageId)
    {
        if (TraceEnabled)
            Debug.Log("[Seeds] OnInAppMessageClosedIncomplete");
        
        if (OnInAppMessageClosedIncomplete != null)
            OnInAppMessageClosedIncomplete();
    }

    void inAppMessageLoadSucceeded(string inAppMessageId)
    {
        if (TraceEnabled)
            Debug.Log("[Seeds] OnInAppMessageLoadSucceeded");
        
        if (OnInAppMessageLoadSucceeded != null)
            OnInAppMessageLoadSucceeded();
    }

    void inAppMessageShownSuccessfully(string inAppMessageId)
    {
        if (TraceEnabled)
            Debug.Log("[Seeds] OnInAppMessageShownSuccessfully");
        
        if (OnInAppMessageShownSuccessfully != null)
            OnInAppMessageShownSuccessfully();
    }

    void inAppMessageShownInsuccessfully(string inAppMessageId)
    {
        if (TraceEnabled)
            Debug.Log("[Seeds] OnInAppMessageShownInsuccessfully");
        
        if (OnInAppMessageShownInsuccessfully != null)
            OnInAppMessageShownInsuccessfully();
    }

    void noInAppMessageFound(string notUsed)
    {
        if (TraceEnabled)
            Debug.Log("[Seeds] OnNoInAppMessageFound");
        
        if (OnNoInAppMessageFound != null)
            OnNoInAppMessageFound();
    }

    #if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern void Seeds_Init(string serverUrl, string appKey);
    #endif

    public Seeds Init(string serverUrl, string appKey)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        RunOnUIThread(() => androidInstance.Call<AndroidJavaObject>("init", CurrentActivity, androidBridgeInstance, serverUrl,
            appKey));
        #elif UNITY_IOS && !UNITY_EDITOR
        Seeds_Init(serverUrl, appKey);
        #else
        NotImplemented("Init(string serverUrl, string appKey)");
        #endif

        return this;
    }

    #if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern void Seeds_InitWithDeviceId(string serverUrl, string appKey, string deviceId);
    #endif

    public Seeds Init(string serverUrl, string appKey, string deviceId)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        RunOnUIThread(() => androidInstance.Call<AndroidJavaObject>("init", CurrentActivity, androidBridgeInstance, serverUrl,
            appKey, deviceId));
        #elif UNITY_IOS && !UNITY_EDITOR
        Seeds_InitWithDeviceId(serverUrl, appKey, deviceId);
        #else
        NotImplemented("Init(string serverUrl, string appKey, string deviceId)");
        #endif

        return this;
    }

    #if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern bool Seeds_IsStarted();
    #endif

    public bool IsInitialized
    {
        get
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            return androidInstance.Call<bool>("isInitialized");
            #elif UNITY_IOS && !UNITY_EDITOR
            return Seeds_IsStarted();
            #else
            NotImplemented("IsInitialized::get");
            return false;
            #endif
        }
    }

    public void Halt()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call("halt");
        #else
        NotImplemented("Halt()");
        #endif
    }

    #if UNITY_ANDROID && !UNITY_EDITOR
    public void NotifyOnStart()
    {
        RunOnUIThread(() => androidInstance.Call("onStart"));
    }

    public void NotifyOnStop()
    {
        RunOnUIThread(() => androidInstance.Call("onStop"));
    }

    public void NotifyOnGCMRegistrationId(string registrationId)
    {
        androidInstance.Call("onRegistrationId", registrationId);
    }
    #endif

    #if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern void Seeds_RecordEvent1(string key);
    #endif

    public void RecordEvent(string key)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call("recordEvent", key);
        #elif UNITY_IOS && !UNITY_EDITOR
        Seeds_RecordEvent1(key);
        #else
        NotImplemented("RecordEvent(string key)");
        #endif
    }

    #if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern void Seeds_RecordEvent2(string key, int count);
    #endif

    public void RecordEvent(string key, int count)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call("recordEvent", key, count);
        #elif UNITY_IOS && !UNITY_EDITOR
        Seeds_RecordEvent2(key, count);
        #else
        NotImplemented("RecordEvent(string key, int count)");
        #endif
    }

    #if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern void Seeds_RecordEvent3(string key, int count, double sum);
    #endif

    public void RecordEvent(string key, int count, double sum)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call("recordEvent", key, count, sum);
        #elif UNITY_IOS && !UNITY_EDITOR
        Seeds_RecordEvent3(key, count, sum);
        #else
        NotImplemented("RecordEvent(string key, int count, double sum)");
        #endif
    }

    public void RecordEvent(string key, IDictionary<string, string> segmentation, int count)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call("recordEvent", key, CreateMapFromDicrionary(segmentation), count);
        #else
        NotImplemented("RecordEvent(string key, IDictionary<string, string> segmentation, int count)");
        #endif
    }

    public void RecordEvent(string key, IDictionary<string, string> segmentation, int count, double sum)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call("recordEvent", key, CreateMapFromDicrionary(segmentation), count, sum);
        #else
        NotImplemented("RecordEvent(string key, IDictionary<string, string> segmentation, int count, double sum)");
        #endif
    }

    #if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern void Seeds_RecordIAPEvent(string key, double price);
    #endif

    public void RecordIAPEvent(string key, double price)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call("recordIAPEvent", key, price);
        #elif UNITY_IOS && !UNITY_EDITOR
        Seeds_RecordIAPEvent(key, price);
        #else
        NotImplemented("RecordIAPEvent(string key, double price)");
        #endif
    }

    #if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern void Seeds_RecordSeedsIAPEvent(string key, double price);
    #endif

    public void RecordSeedsIAPEvent(string key, double price)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call("recordSeedsIAPEvent", key, price);
        #elif UNITY_IOS && !UNITY_EDITOR
        Seeds_RecordSeedsIAPEvent(key, price);
        #else
        NotImplemented("RecordSeedsIAPEvent(string key, double price)");
        #endif
    }

    public Seeds SetUserData(IDictionary<string, string> data)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call<AndroidJavaObject>("setUserData", CreateMapFromDicrionary(data));
        #else
        NotImplemented("SetUserData(IDictionary<string, string> data)");
        #endif

        return this;
    }

    public Seeds SetUserData(IDictionary<string, string> data, IDictionary<string, string> customData)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call<AndroidJavaObject>("setUserData", CreateMapFromDicrionary(data),
            CreateMapFromDicrionary(customData));
        #else
        NotImplemented("SetUserData(IDictionary<string, string> data, IDictionary<string, string> customData)");
        #endif

        return this;
    }

    public Seeds SetCustomUserData(IDictionary<string, string> customData)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call<AndroidJavaObject>("setCustomUserData", CreateMapFromDicrionary(customData));
        #else
        NotImplemented("SetCustomUserData(IDictionary<string, string> customData)");
        #endif

        return this;
    }

    #if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern void Seeds_SetLocation(double lat, double lon);
    #endif

    public Seeds SetLocation(double lat, double lon)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call<AndroidJavaObject>("setLocation", lat, lon);
        #elif UNITY_IOS && !UNITY_EDITOR
        Seeds_SetLocation(lat, lon);
        #else
        NotImplemented("SetLocation(double lat, double lon)");
        #endif

        return this;
    }

    public Seeds SetCustomCrashSegments(IDictionary<string, string> segments)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call<AndroidJavaObject>("setCustomCrashSegments", CreateMapFromDicrionary(segments));
        #else
        NotImplemented("SetCustomCrashSegments(IDictionary<string, string> segments)");
        #endif

        return this;
    }

    public Seeds AddCrashLog(string record)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call<AndroidJavaObject>("addCrashLog", record);
        #else
        NotImplemented("AddCrashLog(string record)");
        #endif

        return this;
    }

    public Seeds LogException(string exception)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call<AndroidJavaObject>("logException", exception);
        #else
        NotImplemented("LogException(string exception)");
        #endif

        return this;
    }

    #if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern void Seeds_EnableCrashReporting();
    #endif

    public Seeds EnableCrashReporting()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call<AndroidJavaObject>("enableCrashReporting");
        #elif UNITY_IOS && !UNITY_EDITOR
        Seeds_EnableCrashReporting();
        #else
        NotImplemented("EnableCrashReporting()");
        #endif

        return this;
    }

    public Seeds SetDisableUpdateSessionRequests(bool disable)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call<AndroidJavaObject>("setDisableUpdateSessionRequests", disable);
        #else
        NotImplemented("SetDisableUpdateSessionRequests(bool disable)");
        #endif

        return this;
    }

    public Seeds SetLoggingEnabled(bool enabled)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call<AndroidJavaObject>("setLoggingEnabled", enabled);
        #else
        NotImplemented("SetLoggingEnabled(bool enabled)");
        #endif

        return this;
    }

    public bool IsLoggingEnabled
    {
        get
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            return androidInstance.Call<bool>("isLoggingEnabled");
            #else
            NotImplemented("IsLoggingEnabled::get");
            return false;
            #endif
        }
        set
        {
            SetLoggingEnabled(value);
        }
    }

    public Seeds EnablePublicKeyPinning(IEnumerable<string> certificates)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call<AndroidJavaObject>("enablePublicKeyPinning", CreateListFromEnumerable(certificates));
        #else
        NotImplemented("EnablePublicKeyPinning(IEnumerable<string> certificates)");
        #endif

        return this;
    }

    #if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern bool Seeds_GetABTestingOn();
    #endif

    public bool IsABTestingOn
    {
        get
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            return androidInstance.Call<bool>("isA_bTestingOn");
            #elif UNITY_IOS && !UNITY_EDITOR
            return Seeds_GetABTestingOn();
            #else
            NotImplemented("IsABTestingOn::get");
            return false;
            #endif
        }
        set
        {
            SetABTestingOn(value);
        }
    }

    #if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern void Seeds_SetABTestingOn(bool abTestingOn);
    #endif

    public void SetABTestingOn(bool abTestingOn)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call("setA_bTestingOn", abTestingOn);
        #elif UNITY_IOS && !UNITY_EDITOR
        Seeds_SetABTestingOn(abTestingOn);
        #else
        NotImplemented("SetABTestingOn(bool abTestingOn)");
        #endif
    }

    #if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern void Seeds_SetMessageVariantName(string messageVariantName);
    #endif

    public void SetMessageVariantName(string messageVariantName)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call("setMessageVariantName", messageVariantName);
        #elif UNITY_IOS && !UNITY_EDITOR
        Seeds_SetMessageVariantName(messageVariantName);
        #else
        NotImplemented("SetMessageVariantName(string messageVariantName)");
        #endif
    }

    #if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern string Seeds_GetMessageVariantName();
    #endif

    public string GetMessageVariantName()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        return androidInstance.Call<string>("getMessageVariantName");
        #elif UNITY_IOS && !UNITY_EDITOR
        return Seeds_GetMessageVariantName();
        #else
        NotImplemented("GetMessageVariantName()");
        return null;
        #endif
    }

    public string MessageVariantName
    {
        get
        {
            return GetMessageVariantName();
        }
        set
        {
            SetMessageVariantName(value);
        }
    }

    #if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern void Seeds_RequestInAppMessage();
    #endif

    public void RequestInAppMessage()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call("requestInAppMessage");
        #elif UNITY_IOS && !UNITY_EDITOR
        Seeds_RequestInAppMessage();
        #else
        NotImplemented("RequestInAppMessage()");
        #endif
    }

    #if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern bool Seeds_IsInAppMessageLoaded();
    #endif

    public bool IsInAppMessageLoaded
    {
        get
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            return androidInstance.Call<bool>("isInAppMessageLoaded");
            #elif UNITY_IOS && !UNITY_EDITOR
            return Seeds_IsInAppMessageLoaded();
            #else
            NotImplemented("IsInAppMessageLoaded::get");
            return false;
            #endif
        }
    }

    #if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern void Seeds_ShowInAppMessage();
    #endif

    public void ShowInAppMessage()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        androidInstance.Call("showInAppMessage");
        #elif UNITY_IOS && !UNITY_EDITOR
        Seeds_ShowInAppMessage();
        #else
        NotImplemented("ShowInAppMessage()");
        #endif
    }
}
