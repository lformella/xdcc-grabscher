[![XG](http://xg.bitpir.at/images/xg_bw.png?v=1)](http://www.larsformella.de/lang/en/portfolio/programme-software/xg)

XG, called __X__dcc __G__rabscher, is a XDCC download manager. Grabscher is the german word for grabber :-)


# What makes it special?
XG is just a command line app which connects to one or multiple IRC networks and handles the whole network communication. The IRC servers, channels, bots and packets are presented within a nice and stylish web frontend. There you can search and download packets.

You can run XG on every machine that supports c# / mono - even root servers without x(org), or an old weak pc running linux without a monitor - and control your downloads with your browser from everywhere. You don't have to keep a big PC running, but just a small download box which handles all the IRC stuff.


# How do i use it?
Run the program and point your browser to __127.0.0.1:5556__. The default password is __xgisgreat__. 
![Password Dialog](http://xg.bitpir.at/images/help/login.png?v=1)


## At first: change the settings
You can do this directly in the web frontend. Just click on the __Config__ link in the options menu.

![Options](http://xg.bitpir.at/images/help/options.png?v=1)

This is a small explanation to help you set the correct options. If you don't want to use a special feature, just disable it.

![Settings part 1](http://xg.bitpir.at/images/help/settings_1.png?v=1)

The web server password is filled with __xgisgreat__ and the port ist __5556__. The IRC passport and email can be left blank and are just needed if you want use nickserv.

![Settings part 2](http://xg.bitpir.at/images/help/settings_2.png?v=1)

### Filehandlers
If a packet is downloaded you can run several commands. If the regex of a file handler matches the file name, the process is started. A process is defined by a command, arguments and the next process. The next process can be left empty and only is called if the current one is successfully executed.

![Settings part 3](http://xg.bitpir.at/images/help/settings_3.png?v=1)

The following handler matches all rar archives. It will create a separate folder, extract the archive into it, removes the archive and moves the folder onto a different partition. Every process is executed only, if the previous one was successfully. Because of this, the handler won't delete the archive if he could not extract it.

![Settings part 4](http://xg.bitpir.at/images/help/settings_4.png?v=1)

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
Now you have to add IRC networks and channels. The bots and packets are generated and updated automatically. If you don't know which server and channels to add, try the integrated [xg.bitpir.at](http://xg.bitpir.at) search.

![Server / Channel Dialog](http://xg.bitpir.at/images/help/servers.png?v=1)

## Search
You can search for packets by entering a custom search term and just hit enter. If your want to save your search, just click on the thumb button. Deleting a search works the same. The search items are working with the internal and external search and are saved into a file. So you can hassle-free store your favorite searches.

![Searc](http://xg.bitpir.at/images/help/search.png?v=1)

The results are displayed in a table and the packets are grouped by their bot. If you click on a packet icon, XG will try to download it and keeps you up to date with updated packet informations. The packet icon will match the file ending, so there are different versions:

![Packet Icons](http://xg.bitpir.at/images/help/search_result.png?v=1)

## Notifications
If something happens inside XG you will get a notification. This can also be shown via your browser if you allow it.

![Notification Icon](http://xg.bitpir.at/images/help/notification.png?v=1)

## Extended Stats / Snapshots
XG will collect every 5 minutes some statistical data and generate nice graphs. There you can enable and disable different values to get an optimal view of your running XG copy.

![Extend Statistics](http://xg.bitpir.at/images/help/graphs.png?v=1)

This feature wont work in older browsers like the good old IE8, so do yourself a favor and use a newer one ;-)

## API
XG v3 supports a rest like api to control it via scripts. You can add api keys and enable / disable them.

![Api](http://xg.bitpir.at/images/help/api.png?v=1)

Currently you can just add xdcc links by calling the following url:

ht\*p://*your-own-host:5556*/api/__615d86bb-f867-47c1-a860-ac24e09e976c__/parseXdccLink/__irc.test.net/servername/channel/bot/1/filename/__

The api id has to be entered after the __/api/__ path segment. After that is the method you want to call, for example __/parseXdccLink/__. Finally you have to add the data you want to pass to method (must be a valid xdcc link in our example). Currently api methods can return the following json encoded results:

* __{"ReturnValue":-1}__ - api key is invalid or disabled
* __{"ReturnValue":0}__ - there was an error calling the method
* __{"ReturnValue":1}__ - everything is fine

# Upgrading XG

If you are upgrading from version 2 to 3, you should finish your downloads and write down your servers and channels, because XG 3 is not able to load the data generated by previous versions.

XG 3 is using NHibernate, so you can use your own config if you want to put the data into a mysql database for example. If you do not need this feature you can use the build in SQLite config which is loaded by default. The database is located in your user config folder and can be edited if XG is not running.

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

## on Ubuntu with Mono

The following packets must be installed:

* mono-runtime
* libmono-posix4.0-cil mono-dmcs
* libmono-system-web4.0-cil
* libmono-system-runtime-serialization4.0-cil
* libmono-system-xml-linq4.0-cil

### Install command to copy paste:

```bash
sudo apt-get install mono-runtime libmono-posix4.0-cil mono-dmcs libmono-system-web4.0-cil libmono-system-runtime-serialization4.0-cil libmono-system-xml-linq4.0-cil
```