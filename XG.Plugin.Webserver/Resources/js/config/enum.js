//
//  enum.js
//  This file is part of XG - XDCC Grabscher
//  http://www.larsformella.de/lang/en/portfolio/programme-software/xg
//
//  Author:
//       Lars Formella <ich@larsformella.de>
//
//  Copyright (c) 2012 Lars Formella
//
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//

function Enum ()
{
}

Enum.TangoColor =
{
	Butter: { Light: "fce94f", Middle: "edd400", Dark: "c4a000"},
	Orange: { Light: "fcaf3e", Middle: "f57900", Dark: "ce5c00"},
	Chocolate: { Light: "e9b96e", Middle: "c17d11", Dark: "8f5902"},
	Chameleon: { Light: "8ae234", Middle: "73d216", Dark: "4e9a06"},
	SkyBlue: { Light: "729fcf", Middle: "3465a4", Dark: "204a87"},
	Plum: { Light: "ad7fa8", Middle: "75507b", Dark: "5c3566"},
	ScarletRed: { Light: "ef2929", Middle: "cc0000", Dark: "a40000"},
	Aluminium1: { Light: "eeeeec", Middle: "d3d7cf", Dark: "babdb6"},
	Aluminium2: { Light: "888a85", Middle: "555753", Dark: "2e3436"}
};

Enum.SnapshotValue =
{
	Timestamp: 0,

	Speed: 1,

	Servers: 2,
	ServersEnabled: 21,
	ServersDisabled: 22,
	ServersConnected: 3,
	ServersDisconnected: 4,

	Channels: 5,
	ChannelsEnabled: 23,
	ChannelsDisabled: 24,
	ChannelsConnected: 6,
	ChannelsDisconnected: 7,

	Bots: 8,
	BotsConnected: 9,
	BotsDisconnected: 10,
	BotsFreeSlots: 11,
	BotsFreeQueue: 12,
	BotsAverageCurrentSpeed: 19,
	BotsAverageMaxSpeed: 20,

	Packets: 13,
	PacketsConnected: 14,
	PacketsDisconnected: 15,
	PacketsSize: 16,
	PacketsSizeDownloading: 17,
	PacketsSizeNotDownloading: 18,
	PacketsSizeConnected: 25,
	PacketsSizeDisconnected: 26,

	FileSize: 30,
	FileSizeDownloaded: 27,
	FileSizeMissing: 28,
	FileTimeMissing: 29
};

Enum.NotificationType =
{
	PacketCompleted: 1,
	PacketIncomplete: 2,
	PacketBroken: 3,
	PacketFileMismatch: 7,
	PacketNameDifferent: 19,

	PacketRequested: 4,
	PacketRemoved: 5,

	FileCompleted: 6,
	FileBuildFailed: 8,

	ServerConnected: 9,
	ServerConnectFailed: 10,

	ChannelJoined: 11,
	ChannelJoinFailed: 12,
	ChannelBanned: 13,
	ChannelParted: 14,
	ChannelKicked: 15,

	BotConnected: 16,
	BotConnectFailed: 17,
	BotSubmittedWrongData: 18
};
