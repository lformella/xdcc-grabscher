# XG

## What is XG?
XG, called Xdcc Grabscher, is a XDCC download tool.

## What makes it special?
XG is a command line app which connects to an irc network an handles all communications and downloading. Via the web frontend you can search and activate downloads on the server.
You can run the server on every machine that supports c# / mono - even root servers without x or an old weak pc running linux without a monitor - and control your downloads with your browser from everywhere you want.
So you dont have to keep your normal pc running, but just a small download pc wich handles all the irc stuff.

A so called killer feature is the multiple download function. Every good xdcc client can resume and check downloads of course, but xg is able to download the same packet / file from different bots / sources.
It can split downloads in multiple parts, download each part from a different bot and merge them back after the error free download. So you can use your bandwidth with the maximum performance.

## How do i use it?
If you want to change the settings, create a file named __settings.xml__ using the example. The settings file must be located in the folder where the binary __Server.Cmd.exe__ lives. Change the xml file if you want, run the program and after that point your browser to 127.0.0.1:5556 or whatever you have just specified. The default password should be __xgisgreat__.

Now you have to add irc networks and channels. The bots and packets are generated and updated automatically. If you click on a packet icon, the server will try to download it and keeps you up to date with updated packet informations. If you don't know which server and channels to add, try the integrated [xg.bitpir.at](http://xg.bitpir.at) search.

## Settings

You should change the default settings and this explanation will help you:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Settings xmlns: xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns: xsd="http://www.w3.org/2001/XMLSchema">
  <IrcNick>anon1234</IrcNick> <!-- the nickname of the irc user -->
  <IrcPasswort>password123</IrcPasswort> <!-- nickserv password -->
  <IrcRegisterEmail>anon@ymous.org</IrcRegisterEmail> <!-- nickserv email -->
  <AutoRegisterNickserv>false</AutoRegisterNickserv> <!-- register nick if he does not exist -->
  <AutoJoinOnInvite>true</AutoJoinOnInvite> <!-- should xg join channels on invite -->
  
  <TempPath>./tmp/</TempPath> <!-- relative folder of the temporary download folder -->
  <ReadyPath>./dl/</ReadyPath> <!-- relative folder for ready downloads -->
  <EnableMultiDownloads>false</EnableMultiDownloads> <!-- enable multi bot dl feature -->
  <ClearReadyDownloads>false</ClearReadyDownloads> <!-- remove ready files from database -->
  
  <Password>xgisgreat</Password> <!-- server password -->
  
  <UseWebServer>true</UseWebServer> <!-- start the web server -->
  <WebServerPort>5556</WebServerPort> <!-- port of the web server -->
  
  <UseJabberClient>false</UseJabberClient> <!-- connect to the jabber server -->
  <JabberServer>jabber.org</JabberServer> <!-- server of the jabber user -->
  <JabberUser>user</JabberUser> <!-- name of the jabber user -->
  <JabberPassword>password</JabberPassword> <!-- password of the jabber user -->
  
  <FileHandlers></FileHandlers> <!-- commands to process ready downloads - see next chapter -->
</Settings>
```

### Filehandlers

If a packet is downloaded you can run several commands. If the regex of a file handler matches the file name, the process is started. A process is defined by a command, arguments and the next process. The next process only is called if the current one is successfully executed.

The following handler matches all rar archives. It will create a separate folder, extract the archive into it, removes the archive and moves the folder onto a different partition. Every process is executed only, if the previous one was successfully. Because of this, the handler won't delete the archive if he could not extract it.

```xml
<FileHandler>
  <Regex>.*\.rar</Regex>
  <Process>
    <Command>mkdir</Command>
    <Arguments>%FOLDER%/%FILENAME%</Arguments>
    <Next>
      <Command>unrar</Command>
      <Arguments>e -p- %PATH% %FOLDER%/%FILENAME%</Arguments>
      <Next>
        <Command>rm</Command>
        <Arguments>%PATH%</Arguments>
        <Next>
          <Command>mv</Command>
          <Arguments>%FOLDER%/%FILENAME% /media/data/%FILENAME%</Arguments>
        </Next>
      </Next>
    </Next>
  </Process>
</FileHandler>
```

#### Arguments

You can use different placeholders in your arguments:

* __%PATH%__ = full path of the file, like __/the/full/path/to/file_complete.rar__
* __%FOLDER%__ = full path of the folder of the file, like __/the/full/path/to__
* __%FILE%__ = the complete file name, like __file_complete.rar__
* __%FILENAME%__ = just the file name, like __file_complete__
* __%EXTENSION%__ = just the file extension, like __rar__

#### Examples

Just look to some more examples to extract tar archives, move all other files to a separate folder and send an email.

```xml
  <FileHandlers>
    <FileHandler>
      <Regex>.*\.tar</Regex>
      <Process>
        <Command>mkdir</Command>
        <Arguments>%FOLDER%/%FILENAME%</Arguments>
        <Next>
          <Command>tar</Command>
          <Arguments>-xf %PATH% -C %FOLDER%/%FILENAME%</Arguments>
          <Next>
            <Command>rm</Command>
            <Arguments>%PATH%</Arguments>
            <Next>
              <Command>mv</Command>
              <Arguments>%FOLDER%/%FILENAME% /media/data/%FILENAME%</Arguments>
            </Next>
          </Next>
        </Next>
      </Process>
    </FileHandler>
    <FileHandler>
      <Regex>.*\.[^tar]</Regex>
      <Process>
        <Command>mkdir</Command>
        <Arguments>%FOLDER%/%FILENAME%</Arguments>
        <Next>
          <Command>mv</Command>
          <Arguments>%PATH% %FOLDER%/%FILENAME%/%FILE%</Arguments>
          <Next>
            <Command>mv</Command>
            <Arguments>%FOLDER%/%FILENAME% /media/data/%FILENAME%</Arguments>
            <Next>
              <Command>send_email_command</Command>
              <Arguments>"hello, you have a new download: /media/data/%FILENAME%"</Arguments>
            </Next>
          </Next>
        </Next>
      </Process>
    </FileHandler>
  </FileHandlers>
```


