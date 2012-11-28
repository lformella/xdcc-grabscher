[![XG](http://xg.bitpir.at/images/xg_bw.png)](http://www.larsformella.de/lang/en/portfolio/programme-software/xg)

XG, called __X__dcc __G__rabscher, is a XDCC download manager. Grabscher is the german word for grabber :-)

## What makes it special? (I am already using and happy with xWeasel.)
XG is just a command line app which connects to one or multiple IRC networks and handles the whole network communication. The IRC servers, channels, bots and packets are presented within a nice and stylish web frontend. There you can search and download packets.

You can run XG on every machine that supports c# / mono - even root servers without x(org), or an old weak pc running linux without a monitor - and control your downloads with your browser from everywhere. You don't have to keep a big PC running, but just a small download box which handles all the IRC stuff.

A so called killer feature is the multiple download function. Every good XDCC client can resume and check downloads of course, but XG is able to download the same packet / file from different bots / sources. It will split downloads into multiple parts, download each part from a different bot and merge them back after error free downloading, so you can use your maximum bandwidth. Because of the DCC protocol design there is no chance to implement a stable and clean error checking routine, so some downloaded files might be corrupt, even if XG assumes they are not. Use this feature at your own risk! (See the __EnableMultiDownloads__ flag in the settings chapter.

## How do i use it?
If you want to change the settings, create a file named __settings.xml__ using the example. If there is no __settings.xml__ file XG will create one using default values. The settings file must be located in your user folder:

* Windows 7: C:\Users\Benutzername\AppData\Roaming\XG
* Linux: /home/Benutzername/.config/XG
* Mac: dont know - send me an info if you are successfully running XG on mac :-)


Change the XML file if you want, run the program and after that point your browser to __127.0.0.1:5556__ or whatever you have just specified. The default password should be __xgisgreat__.

![Password Dialog](http://xg.bitpir.at/images/help/password_dialog.png)

Now you have to add IRC networks and channels. The bots and packets are generated and updated automatically. If you don't know which server and channels to add, try the integrated [xg.bitpir.at](http://xg.bitpir.at) search.

![Server / Channel Dialog](http://xg.bitpir.at/images/help/server_channel_dialog.png)

If you click on a packet icon, XG will try to download it and keeps you up to date with updated packet informations.

Packet icon = ![Packet Icon](http://xg.bitpir.at/images/help/packet_icon.png)

### If running on Ubuntu with Mono

The following packets must be installed:

* mono-runtime
* libmono-posix4.0-cil mono-dmcs
* libmono-system-web4.0-cil
* libmono-system-runtime-serialization4.0-cil
* libmono-system-xml-linq4.0-cil

### If running on Windows Vista and greater

If you dont want to run XG with admin rights, you have to execute the following command once using an admin shell: __netsh http add urlacl url=http://*:5556/ user=%USERDOMAIN%\%USERNAME%__ - you have to adjust the port if you changed it before, of course. Otherwise the integrated webserver wont work.

## Settings

You should change the default settings and this explanation will help you. If you don't want to use a special feature, just disable or remove it.

```xml
<?xml version="1.0" encoding="utf-8"?>
<Settings xmlns: xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns: xsd="http://www.w3.org/2001/XMLSchema">
  <IrcNick>anon1234</IrcNick> <!-- the nickname of the IRC user -->
  <IrcPasswort>password123</IrcPasswort> <!-- nickserv password -->
  <IrcRegisterEmail>anon@ymous.org</IrcRegisterEmail> <!-- nickserv email -->
  <AutoRegisterNickserv>false</AutoRegisterNickserv> <!-- register nick if he does not exist -->
  <AutoJoinOnInvite>true</AutoJoinOnInvite> <!-- should xg join channels on invite -->
  
  <TempPath>/home/user/.config/XG/tmp/</TempPath> <!-- absolute folder of the temporary download folder -->
  <ReadyPath>/home/user/.config/XG/dl/</ReadyPath> <!-- absolute folder for ready downloads -->
  <EnableMultiDownloads>false</EnableMultiDownloads> <!-- enable multi bot dl feature -->
  
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

If a packet is downloaded you can run several commands. If the regex of a file handler matches the file name, the process is started. A process is defined by a command, arguments and the next process. The next process can be left empty and only is called if the current one is successfully executed.

The following handler matches all rar archives. It will create a separate folder, extract the archive into it, removes the archive and moves the folder onto a different partition. Every process is executed only, if the previous one was successfully. Because of this, the handler won't delete the archive if he could not extract it.

```xml
<FileHandler>
  <Regex>.*\.rar</Regex> <!-- match rar archives -->
  <Process>
    <Command>mkdir</Command> <!-- generate a separate folder -->
    <Arguments>%FOLDER%/%FILENAME%</Arguments>
    <Next>
      <Command>unrar</Command> <!-- unrar command with password skip! -->
      <Arguments>e -p- %PATH% %FOLDER%/%FILENAME%</Arguments>
      <Next>
        <Command>rm</Command> <!-- remove file -->
        <Arguments>%PATH%</Arguments>
        <Next>
          <Command>mv</Command> <!-- move to another partition -->
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

Just look at some more examples.

```xml
<FileHandlers>
  <FileHandler>
    <Regex>.*\.tar</Regex> <!-- match all tar archives -->
    <Process>
      <Command>mkdir</Command> <!-- generate a separate folder -->
      <Arguments>%FOLDER%/%FILENAME%</Arguments>
      <Next>
        <Command>tar</Command> <!-- untar -->
        <Arguments>-xf %PATH% -C %FOLDER%/%FILENAME%</Arguments>
        <Next>
          <Command>rm</Command> <!-- remove file -->
          <Arguments>%PATH%</Arguments>
          <Next>
            <Command>mv</Command> <!-- move to another partition -->
            <Arguments>%FOLDER%/%FILENAME% /media/data/%FILENAME%</Arguments>
          </Next>
        </Next>
      </Next>
    </Process>
  </FileHandler>
  <FileHandler>
    <Regex>.*\.[^tar]</Regex>
    <Process>
      <Command>mkdir</Command> <!-- generate a separate folder -->
      <Arguments>%FOLDER%/%FILENAME%</Arguments>
      <Next>
        <Command>mv</Command> <!-- move into folder -->
        <Arguments>%PATH% %FOLDER%/%FILENAME%/%FILE%</Arguments>
        <Next>
          <Command>mv</Command> <!-- move to another partition -->
          <Arguments>%FOLDER%/%FILENAME% /media/data/%FILENAME%</Arguments>
          <Next>
            <Command>send_email_command</Command> <!-- send email to notify -->
            <Arguments>"hello, you have a new download: /media/data/%FILENAME%"</Arguments>
          </Next>
        </Next>
      </Next>
    </Process>
  </FileHandler>
</FileHandlers>
```
## Search for packets

You can search for packets by entering a custom search term and just clicking enter. Your search will be added to the search list and by clicking on one of these items the packet list will be updated.

![Search List](http://xg.bitpir.at/images/help/search_list.png)

The search items are working with the internal and external search and are saved into a file. So you can hassle-free store your favourite searches.

## Extended Stats / Snapshots

XG will collect every 5 minutes some statistical data and generate nice graphs. There you can enable and disable different values to get an optimal view of your running XG copy.

![Extend Statistic Dialog](http://xg.bitpir.at/images/help/snapshots.png)

This feature wont work in older browsers like the good old IE8, so do yourself a favour and use a newer one ;-)
