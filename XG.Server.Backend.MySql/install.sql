CREATE TABLE IF NOT EXISTS `server` (
	`Guid` varchar(37) NOT NULL,
	`Enabled` int(1) DEFAULT '0',
	`Connected` int(1) DEFAULT '0',
	`Name` varchar(100) NOT NULL,
	`LastModified` bigint(11) DEFAULT '0',
	`Port` int(5) NOT NULL,
	PRIMARY KEY (`guid`)
) ENGINE=InnoDB DEFAULT CHARACTER SET = utf8 COLLATE = utf8_general_ci;

CREATE TABLE IF NOT EXISTS `channel` (
	`Guid` varchar(37) NOT NULL,
	`ParentGuid` varchar(37) NOT NULL,
	`Enabled` int(1) DEFAULT '0',
	`Connected` int(1) DEFAULT '0',
	`Name` varchar(100) NOT NULL,
	`LastModified` bigint(11) DEFAULT '0',
	PRIMARY KEY (`Guid`),
	INDEX `fk_server` (`ParentGuid` ASC) ,
	CONSTRAINT `fk_server`
		FOREIGN KEY (`ParentGuid`)
		REFERENCES `server` (`Guid`)
		ON DELETE CASCADE
		ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARACTER SET = utf8 COLLATE = utf8_general_ci;

CREATE TABLE IF NOT EXISTS `bot` (
	`Guid` varchar(37) NOT NULL,
	`ParentGuid` varchar(37) NOT NULL,
	`Enabled` int(1) DEFAULT '0',
	`Connected` int(1) DEFAULT '0',
	`Name` varchar(100) NOT NULL,
	`LastModified` bigint(11) DEFAULT '0',
	`LastUpdated` bigint(11) DEFAULT '0',
	`BotState` int(1) DEFAULT '0',
	`InfoQueueCurrent` int(4) DEFAULT '0',
	`InfoQueueTotal` int(4) DEFAULT '0',
	`InfoSlotCurrent` int(4) DEFAULT '0',
	`InfoSlotTotal` int(4) DEFAULT '0',
	`InfoSpeedCurrent` double DEFAULT '0',
	`InfoSpeedMax` double DEFAULT '0',
	`LastContact` bigint(11) DEFAULT '0',
	`LastMessage` varchar(255) DEFAULT '0',
	PRIMARY KEY (`Guid`),
	INDEX `fk_channel` (`ParentGuid` ASC) ,
	CONSTRAINT `fk_channel`
		FOREIGN KEY (`ParentGuid`)
		REFERENCES `channel` (`Guid`)
		ON DELETE CASCADE
		ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARACTER SET = utf8 COLLATE = utf8_general_ci;

CREATE TABLE IF NOT EXISTS `packet` (
	`Guid` varchar(37) NOT NULL,
	`ParentGuid` varchar(37) NOT NULL,
	`Enabled` int(1) DEFAULT '0',
	`Connected` int(1) DEFAULT '0',
	`Name` varchar(100) NOT NULL,
	`LastModified` bigint(11) DEFAULT '0',
	`LastUpdated` bigint(11) DEFAULT '0',
	`Id` int(4) DEFAULT '0',
	`Size` bigint(20) DEFAULT '0',
	PRIMARY KEY (`Guid`),
	INDEX `fk_bot` (`ParentGuid` ASC) ,
	CONSTRAINT `fk_bot`
		FOREIGN KEY (`ParentGuid`)
		REFERENCES `bot` (`Guid`)
		ON DELETE CASCADE
		ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARACTER SET = utf8 COLLATE = utf8_general_ci;
