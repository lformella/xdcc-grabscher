[![XG](http://xg.bitpir.at/images/xg_bw.png?v=3)](http://www.larsformella.de/lang/en/portfolio/programme-software/xg)

XG, called __X__dcc __G__rabscher, is a XDCC download manager. Grabscher is the german word for grabber :-)


# What makes it special?
XG is just a command line app which connects to one or multiple IRC networks and handles the whole network communication. The IRC servers, channels, bots and packets are presented within a nice and stylish web frontend. There you can search and download packets.

You can run XG on every machine that supports C# / Mono - even root servers without x(org), or an old weak pc running linux without a monitor - and control your downloads with your browser from everywhere. You don't have to keep a big PC running, but just a small download box which handles all the IRC stuff.


# How do i use it?
Run the program and point your browser to __127.0.0.1:5556__. The default password is __xgisgreat__. If you already added some servers and channels, it will take some time untill the Webfrontend is up and running. This is due to the build in SQLite database which is not really performant and takes some time to load the saved objects.

![Password Dialog](http://xg.bitpir.at/images/help/login.png?v=3)

## At first: change the settings
You can do this directly in the web frontend. Just click on the __Config__ link in the options menu.

![Options](http://xg.bitpir.at/images/help/options.png?v=3)

This is a small explanation to help you set the correct options. If you don't want to use a special feature, just disable it.

__Note:__ The Elastic Search configuration is not available in the webfrontend anymore and can be changed by editing the config file manually.

![Settings part 1](http://xg.bitpir.at/images/help/settings_1.png?v=3)

The web server password is filled with __xgisgreat__ and the port ist __5556__. The IRC passport and email can be left blank and are just needed if you want use nickserv.

![Settings part 2](http://xg.bitpir.at/images/help/settings_2.png?v=3)

### Filehandlers
If a packet is downloaded you can run several commands. If the regex of a file handler matches the file name, the process is started. A process is defined by a command, arguments and the next process. The next process can be left empty and only is called if the current one is successfully executed.

![Settings part 3](http://xg.bitpir.at/images/help/settings_3.png?v=3)

The following handler matches all rar / zip archives. It will create a separate folder, extract the archive into it and removes the archive. Every process is executed only, if the previous one was successfully. Because of this, the handler won't delete the archive if he could not extract it.

![Settings part 4](http://xg.bitpir.at/images/help/settings_4.png?v=3)

You can add as many file handlers as you want. They are also stored in the settings file.

#### Arguments
You can use different placeholders in your arguments:

* __%PATH%__ = full path of the file, like __/the/full/path/to/file_complete.rar__
* __%FOLDER%__ = full path of the folder of the file, like __/the/full/path/to__
* __%FILE%__ = the complete file name, like __file_complete.rar__
* __%FILENAME%__ = just the file name, like __file_complete__
* __%EXTENSION%__ = just the file extension, like __rar__

### Change settings manually
If you want to change the settings manually, you have to change the file named __xg.config__ located in your user folder:

* Windows 7: C:\Users\Username\AppData\Roaming\XG
* Linux: /home/Username/.config/XG
* Mac: /Users/Username/.config/XG

## Add servers and channels
Now you have to add IRC networks and channels. The bots and packets are generated and updated automatically. If you don't know which server and channels to add, try the integrated [xg.bitpir.at](http://xg.bitpir.at) search or add a XDCC link.

Normally the bots will announce their pakets directly in the channel. If they are silent, you can check the option __Check user versions__ and XG will ask the voiced users about their version. If XG detects an iroffer he will try to send __xdcc list__ commands to get packet lists. __\_DO NOT\___ check the option unless you know, that the bots in this channel wont announce their packets. Otherwhise you mostly will be banned!

![Server / Channel Dialog](http://xg.bitpir.at/images/help/servers.png?v=3)

## Search
You can search for packets by entering a custom search term and just hit enter. If your want to save your search, just click on the thumb button. Deleting a search works the same. The search items are working with the internal and external search and are also saved into the database. If you want to exclude words from your search you can use "-". To search for packages and exclude TS releases you could use "Spiderman -TS".

![Search](http://xg.bitpir.at/images/help/search.png?v=3)

The results are displayed in a table and the packets are grouped by their bot. The grouping can be disabled in the settings menu, but you will lose some important informations. If you click on a packet icon, XG will try to download it and keeps you up to date with updated packet informations. The packet icon will match the file ending, so there are different versions.

![Packet Icons](http://xg.bitpir.at/images/help/search_results.png?v=3)

## Notifications
If something happens inside XG you will get a notification. This can also be shown via your browser if you allow it.

![Notification Icon](http://xg.bitpir.at/images/help/notification.png?v=3)

## XDCC Links
You can add XDCC links in the following dialog. A XDCC link must have the following structure:

> xdcc:// __server__ / __server-name__ / __channel__ / __bot__ / __packet-id__ / __file-name__ /

The server, channel and bot is automatically added. If the server is connected and the channel joined, the packet will be requested.

![XDCC Links](http://xg.bitpir.at/images/help/xdcc-links.png?v=3)

The server and channel are not deleted after the packet is complete, so if you dont need them anymore, you have to delete them yourself.

## Extended Stats / Snapshots
XG will collect every 5 minutes some statistical data and generate nice graphs. There you can enable and disable different values to get an optimal view of your running XG copy.

![Extended Statistics](http://xg.bitpir.at/images/help/graphs.png?v=3)

This feature wont work in older browsers like the good old IE8, so do yourself a favor and use a newer one ;-)

## API
XG v3.2 supports a rest api to control it via scripts. You can add api keys and enable / disable them. 

![Api](http://xg.bitpir.at/images/help/api.png?v=3)

The api method has to be entered after the __/api/__ path segment. After that is the method you want to call, for example __/downloadPacket/__. The data you want to pass to method has to be encoded in JSON. The content type must be JSON, too. The apiKey property is mandatory and has to match an api key wich is enabled. You can test this via curl:

```
curl -H "Content-Type:text/json" -s -XPUT localhost:5556/api/... -d '
{
  "apiKey": "615d86bb-f867-47c1-a860-ac24e09e976c",
  ...
}'
```

Api methods which create or update data, always return the following JSON encoded properties:

* __ReturnValue = -1__ - api key is invalid or disabled
* __ReturnValue = 0__ - there was an error calling the method
* __ReturnValue = 1__ - everything is fine
* __Message__ - a helpful message if an error occurred

### Download Packet
You can add and download an external packet. If you got a XDCC link, you can use this method to add a packet and download it instantly.

The server and channel are not deleted after the packet is complete, so if you dont need them anymore, you have to delete them yourself.

#### Url

> __PUT__ / __downloadPacket__

#### Example
```
curl -H "Content-Type:text/json" -s -XPUT localhost:5556/api/downloadPacket -d '
{
  "apiKey":"615d86bb-f867-47c1-a860-ac24e09e976c",
  "server":"irc.rizon.net",
  "channel":"#abjects",
  "bot":"[XDCC]Bot",
  "packetId":11,
  "packetName":"My.Super.Movie.mkv"
}'
```

#### Return Value
```
{
  "ReturnValue":0,
  "Message":"server is empty"
}
```

### Search Packets
You can add and download a packet. If you got a XDCC link, you can use this method to add a packet and download it instantly.

#### Url

> __GET__ / __searchPackets__

#### Example
```
curl -H "Content-Type:text/json" -s -XGET localhost:5556/api/searchPackets -d '
{
  "apiKey":"615d86bb-f867-47c1-a860-ac24e09e976c",
  "searchTerm":"german -mkv",
  "showOfflineBots":true,
  "maxResults":20,
  "page":1,
  "sortBy":"Id",
  "sort":"desc"
}'
```

The properties __showOfflineBots__, __maxResults__, __page__, __sortBy__, __sort__ can be left blank. If you leave __showOfflineBots__ blank, it will be filled with __false__ and the search request will just return packets, which bots are online.

#### Return Value
```
{
  "Packets":
  [
    {
      "Bot":
      {
        "State":0,
        "LastMessage": "joined channel #moviegods",
        "LastMessageTime":"2014-05-20T18:34:46",
        "LastContact":"2014-05-20T18:34:46",
        "QueuePosition":0,
        "QueueTime":0,
        "InfoSpeedMax":19691929,
        "InfoSpeedCurrent":4539801,
        "InfoSlotTotal":10,
        "InfoSlotCurrent":1,
        "InfoQueueTotal":100,
        "InfoQueueCurrent":6,
        "Speed":0,
        "HasNetworkProblems":false,
        "Type":"Bot",
        "ParentGuid":"494c5f9f-94da-46e6-b276-201e5b2a51a5",
        "Guid":"4bff3378-245e-4297-8a02-cd40727bf79f",
        "Name":"[MG]-HDTV|EU|S|0009",
        "Connected":false,
        "Enabled":false
      },
      "Id":1400,
      "Size":5557452,
      "Name":"Der.Aktionaer.2014.09.GERMAN.RETAiL.MAGAZiN.eBOOk-sUppLeX.tar",
      "LastUpdated":"2014-03-09T11:33:11",
      "LastMentioned":"2014-03-09T11:33:11",
      "Next":false,
      "Speed":0,
      "CurrentSize":0,
      "TimeMissing":0,
      "Type":"Packet",
      "ParentGuid":"4bff3378-245e-4297-8a02-cd40727bf79f",
      "Guid":"5e5903f3-71ab-41fd-919e-bdadbfd04abf",
      "Connected":false,
      "Enabled":false
    }
  ],
  "ResultCount":5584
}
```

### Enable / Disable Object / Download Packet
You can enable and disable objects. If you enable servers or channels, they will connected or disconnect. If you do this with a packet, it will be downloaded or cancelled.

#### Url

> __POST__ / __enable__

#### Example
```
curl -H "Content-Type:text/json" -s -XPOST localhost:5556/api/enable -d '
{
  "apiKey":"615d86bb-f867-47c1-a860-ac24e09e976c",
  "guid":"5e5903f3-71ab-41fd-919e-bdadbfd04abf",
  "enabled":true
}'
```

#### Return Value
```
{
  "ReturnValue":1,
  "Message":null
}
```

## Shutdown XG gracefully
If you want to shutdown XG, just ctrl+c the process or close the command window. You can also stop XG by using the shutdown button in the webfrontend.

# Upgrading XG
If you are upgrading from version 2 to 3, you should finish your downloads and write down your servers and channels, because XG 3 is not able to load the data generated by previous versions.
 
## Unnecessary Files
Because XG changed some internal routines you can safely delete the following files in the config folder:

### prior version 2
* XG/xgsnapshots.bin
* XG/xgsnapshots.bin.bak
* XG/statistics.xml

### prior version 3
* XG/xg.bin
* XG/xg.bin.bak
* XG/xgfiles.bin
* XG/xgfiles.bin.bak
* XG/xgsearches.bin
* XG/xgsearches.bin.bak
* XG/settings.xml


# Running XG

## On Windows
You need at least .net 4.5.

## On Linux with Mono
You need at least mono 3.x because some needed libs are running on .net 4.5 wich is not supported in earlier versions.

If you are using Debian / Ubuntu, take a look here to get newer mono packages:

> http://mono-project.com/DistroPackages/Debian

### Needed packets / libs
* mono-complete

#### Install command for Debian / Ubuntu to copy paste:
```bash
sudo apt-get install mono-complete
```
