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
You can search for packets by entering a custom search term and just hit enter. If your want to save your search, just click on the thumb button. Deleting a search works the same. The search items are working with the internal and external search and are also saved into the database. If you want to exclude words from your search you can use "-". To search for packages and exclude TS releases you could use **Spiderman -TS**. The size of packets can be controlled by the size box. Only packets which are bigger than the given value are displayed. If you don't want to use this feature, leave this field blank or zero.

![Search](http://xg.bitpir.at/images/help/search.png?v=3)

XG supportes wildcard searches to be able to search for tv shows. If you search for **under the dome s02e\*\*** you will get results for all season 2 episodes from 01 to 99. Even multiple wildcards are supported: **under the dome s\*\*e\*\*** will return results for all season from 01 to 10 and  episodes from 01 to 30. Because multiple wildcard searches are expensive, the results are limited to 10 seasons and 30 episodes.

The results are displayed in a table and the packets can be grouped by their bot or wildcard search. The grouping can be disabled, but you will lose some important informations. If you click on a packet icon, XG will try to download it and keeps you up to date with updated packet informations. The packet icon will match the file ending, so there are different versions.

![Packet Icons](http://xg.bitpir.at/images/help/search_results_bot.png?v=3)

![Packet Icons](http://xg.bitpir.at/images/help/search_results_wildcard.png?v=3)

## Notifications
If something happens inside XG you will get a notification. This can also be shown via your browser if you allow it.

![Notification Icon](http://xg.bitpir.at/images/help/notification.png?v=3)

## XDCC Links
You can add XDCC links in the following dialog. A XDCC link must have the following structure:

> xdcc:// __server__ / __server-name__ / __channel__ / __bot__ / __packet-id__ / __file-name__ /

The server, channel and bot is automatically added. If the server is connected and the channel joined, the packet will be requested.

![XDCC Links](http://xg.bitpir.at/images/help/xdcc-links.png?v=3)

The server and channel are not deleted after the packet is complete, so if you don't need them anymore, you have to delete them yourself.

## Extended Stats / Snapshots
XG will collect every 5 minutes some statistical data and generate nice graphs. There you can enable and disable different values to get an optimal view of your running XG copy.

![Extended Statistics](http://xg.bitpir.at/images/help/graphs.png?v=3)

This feature wont work in older browsers like the good old IE8, so do yourself a favor and use a newer one ;-)

## API
XG v3 supports a REST api to control it via external tools. You can add api keys and enable / disable them. 

![Api](http://xg.bitpir.at/images/help/api.png?v=3)

The following objects can be controlled with different methods via the api:

* servers
  * add
  * delete
  * enable / disable
  * list
* channels
  * add
  * delete
  * enable / disable
  * list
* bots
  * list
* packets
  * enable / disable
  * list
* files
  * delete
  * list
* searches
  * add
  * delete
  * list

The object name has to be entered after the __/api/1.0/__ path segment with the format in the wich the answer should be encoded, for example __/api/1.0/servers.json__. The data you want to pass to the method has to be encoded in same format. The content type must be the format, too. The __Authorization__ header is mandatory and has to match an api key which is enabled. If the apiKey was invalid or disabled, the method will result in an 401.

The currently allowed formats:

* json (preferred)

Api methods which create or update data, always return the following properties:

* __ReturnValue__ (int)
  * 0 - there was an error calling the method
  * 1 - everything is fine
* __Message__ (string)
  * a helpful message if an error occurred

---
### Delete
You can delete an object and all children.

#### URL
> __DELETE /api/1.0/[ servers | channels | files | searches ]/$guid.$format__

#### Example
```
curl -H "Authorization: 615d86bb-f867-47c1-a860-ac24e09e976c" -s -XDELETE localhost:5556/api/1.0/servers/deebd412-9b16-4726-b613-7ec98e714f59.json
```

#### Return Value
```
{
  "ReturnValue":1,
  "Message":null
}
```

---
### Get
Get a single object by its guid.

#### URL
> __GET /api/1.0/[ servers | channels | bots | packets | files | searches ]/$guid.$format__

#### Example
```
curl -H "Authorization: 615d86bb-f867-47c1-a860-ac24e09e976c" -s -XGET localhost:5556/api/1.0/servers/deebd412-9b16-4726-b613-7ec98e714f59.json
```

#### Return Value
```
{
	"Port":6667,
	"ErrorCode":0,
	"ParentGuid":"c31aa923-b615-4d03-840d-c82357c929d4",
	"Guid":"906bfd60-b6f1-4d38-a2f4-cb9cba983a24",
	"Name":"irc.abjects.net",
	"Connected":false,
	"Enabled":false
}
```

---
### Enable
If you enable servers and channels, they will be connected. If you enable a packet it will be downloaded.

#### URL
> __POST /api/1.0/[ servers | channels | packets ]/$guid/enable.$format__

#### Example
```
curl -H "Authorization: 615d86bb-f867-47c1-a860-ac24e09e976c" -s -XPOST localhost:5556/api/1.0/servers/deebd412-9b16-4726-b613-7ec98e714f59/enable.json
```

#### Return Value
```
{
  "ReturnValue":1,
  "Message":null
}
```

---
### Disable
If you disable servers and channels, they will be disconnected. If you disable a packet the download will be stopped and the file is beeing deleted.

#### URL
> __POST /api/1.0/[ servers | channels | packets ]/$guid/disable.$format__

#### Example
```
curl -H "Authorization: 615d86bb-f867-47c1-a860-ac24e09e976c" -s -XPOST localhost:5556/api/1.0/servers/deebd412-9b16-4726-b613-7ec98e714f59/disable.json
```

#### Return Value
```
{
  "ReturnValue":1,
  "Message":null
}
```

---
### Add
You can add an object. All parameters are mandatory.

If you got a XDCC link, you can use this method to add a packet and download it instantly. The server and channel are not deleted after the packet is complete, so if you dont need them anymore, you have to delete them yourself.

#### Url
> __PUT /api/1.0/[ servers | channels | packets | searches ].$format__

#### Post Parameters for servers
* server (string): irc.rizon.net
* port (integer): 11

#### Post Parameters for channels
* server (string): irc.rizon.net
* channel (string): #abjects

#### Post Parameters for packets
* server (string): irc.rizon.net
* channel (string): #abjects
* bot (string): [XDCC]Bot
* packetId (integer): 11
* packetName (string): My.Super.Movie.mkv

#### Post Parameters for searches
* search (string): german -mkv

#### Example
```
curl -H "Content-Type:application/json" -H "Authorization: 615d86bb-f867-47c1-a860-ac24e09e976c" -s -XPOST localhost:5556/api/1.0/servers.json -d '
{
  "server":"irc.rizon.net",
  "port": 6667
}'
```

```
curl -H "Content-Type:application/json" -H "Authorization: 615d86bb-f867-47c1-a860-ac24e09e976c" -s -XPOST localhost:5556/api/1.0/packets.json -d '
{
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

---
### List
You can list objects. If you want to list packets, you can controll the results.

#### Url
> __GET /api/1.0/[ servers | channels | packets | files | searches ].$format__

#### Get Parameters for packets
* __searchTerm__ (string) *: german -mkv
* __showOfflineBots__ (boolean): true | false
* __maxResults__ (integer)
* __page__ (integer)
* __sortBy__ (string): Id | Name | Size
* __sort__ (string): asc | desc

The properties __showOfflineBots__, __maxResults__, __page__, __sortBy__, __sort__ can be left blank. If you leave __showOfflineBots__ blank, it will be filled with __false__ and the search request will just return packets, which bots are online.

#### Example
```
curl -H "Authorization: 615d86bb-f867-47c1-a860-ac24e09e976c" -s -XGET 'localhost:5556/api/1.0/packets.json?searchTerm=mkv%20-seven&showOfflineBots=true'
```

#### Return Value
```
{
  "Results":
  [
    {
      ...
    }
  ],
  "ResultCount":5584
}
```
Results is an array containg the requestet objects.

## Shutdown XG gracefully
If you want to shutdown XG, just ctrl+c the process or close the command window. You can also stop XG by using the shutdown button in the webfrontend.

# Upgrading XG
If you are upgrading from version 2 to 3, you should finish your downloads and write down your servers and channels, because XG 3 is not able to load the data generated by previous versions.

If you are upgrading from XG 3.2 to 3.3 you should notice, that the db format switched from sqlite to db4o. Because of this, XG automatically transformes the db **xgobjects.db** into a db4o database **xgobjects.db4o** if it is not there already. The sqlite file can be safely deleted after the first start, but can also be keeped as backup. If you delete the db4o file, XG will start the transformation process again.
 
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
