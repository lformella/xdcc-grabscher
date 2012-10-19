# XG #

## What is XG? ##
XG, called Xdcc Grabscher, is a XDCC download tool.

## What makes it special? ##
XG consits of a server and a client. The server is a commandline app which connects to an irc network an handles all communications and downloading. The client can search and activate downloads on the server.
You can run the server on every machine that supports c# / mono - even root servers without x or an old weak pc running linux without a monitor - and controll your downloads with the client from everywhere you want.
So you dont have to keep your normal pc running, but just a small download pc wich handles all the irc stuff.

A so called killer feature is the multiple download function. Every good xdcc client can resume and check downloads of course, but xg is able to download the same packet / file from different bots / sources.
It can split downloads in multiple parts, download each part from a different bot and merge them back after the error free download. So you can use your bandwith with the maximum performance.

## Is it free? ##
Yes it is free and opensource, but you can donate of course :)

## Is it stable? ##
Uhm... it works well and i use it almost every day, but it is still in an early development branch.

## Where can i get it? ##
Look here.

## How do i start it? ##
Just copy the file settings.xml into the folder where the programm 'XG.Server.Cmd.exe' lives. Change the xml file if you want and run the programm on one PC and the 'XG.Client.Tcp.Gui.Gtk.exe' on another. The default port and password is '5555' and 'xgisgreat'.
On the client side you have to connect to the server and add irc networks and channels. The bots and packets are generated and updated automatically.
If you double click on a packet, the server will try to download it and keeps you up to date with updated packet informations.
