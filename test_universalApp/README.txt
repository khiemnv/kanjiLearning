#saving data
  + saving data game - StorageContainer. 
    https://msdn.microsoft.com/en-us/library/bb203924.aspx
  + saving to temp file
    http://matthiasshapiro.com/2015/12/10/saving-and-loading-app-data-windows-store-c-uwp-8-1/
#install TTS
  + MSSpeech_TTS_ja-JP_Haruka.msi
    https://www.microsoft.com/en-us/download/details.aspx?id=27224
  + MicrosoftSpeechPlatformSDK.msi
  
#task
  + async void function using
    //not wait function return
    await function();
    //to wait function return
    var t = Task.Run(()=>function());
    t.Wait();
    
#db file
  offset->    //+0        |size
  +4          //          |isDeleted
  +8          //+8        |rKey
              //          |rMarked
              //          |....
              //+rKey     |key as string
              //          |....
              //+rMarked  |marked as string
              //          |....
  +size       //+size
    