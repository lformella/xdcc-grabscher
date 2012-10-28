
CREATE  TABLE IF NOT EXISTS `xg`.`servers` (
  `Guid` VARCHAR(37) CHARACTER SET 'utf8' COLLATE 'utf8_general_ci' NOT NULL ,
  `ParentGuid` VARCHAR(37) CHARACTER SET 'utf8' COLLATE 'utf8_general_ci' NOT NULL ,
  `Enabled` TINYINT(1) NULL DEFAULT 1 ,
  `Connected` TINYINT(1) NULL DEFAULT 0 ,
  `Name` VARCHAR(100) CHARACTER SET 'utf8' COLLATE 'utf8_general_ci' NOT NULL ,
  `Port` INT(5) NOT NULL ,
  `ErrorCode` INT(5) NULL DEFAULT 0 ,
  `ChannelCount` INT(5) NULL DEFAULT 0 ,
  `BotCount` INT(5) NULL DEFAULT 0 ,
  `PacketCount` INT(5) NULL DEFAULT 0 ,
  PRIMARY KEY (`Guid`) ,
  INDEX `index2` (`Name` ASC) )
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8
COLLATE = utf8_general_ci;

CREATE  TABLE IF NOT EXISTS `xg`.`channels` (
  `Guid` VARCHAR(37) CHARACTER SET 'utf8' COLLATE 'utf8_general_ci' NOT NULL ,
  `ParentGuid` VARCHAR(37) CHARACTER SET 'utf8' COLLATE 'utf8_general_ci' NOT NULL ,
  `Enabled` TINYINT(1) NULL DEFAULT 1 ,
  `Connected` TINYINT(1) NULL DEFAULT 0 ,
  `Name` VARCHAR(100) CHARACTER SET 'utf8' COLLATE 'utf8_general_ci' NOT NULL ,
  `ErrorCode` INT(3) NULL DEFAULT 0 ,
  `BotCount` INT(5) NULL DEFAULT 0 ,
  `PacketCount` INT(5) NULL DEFAULT 0 ,
  PRIMARY KEY (`Guid`) ,
  INDEX `index2` (`Name` ASC) ,
  INDEX `fk_channel_1` (`ParentGuid` ASC) ,
  CONSTRAINT `fk_channel_1`
    FOREIGN KEY (`ParentGuid` )
    REFERENCES `xg`.`servers` (`Guid` )
    ON DELETE CASCADE
    ON UPDATE NO ACTION)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8
COLLATE = utf8_general_ci;

CREATE  TABLE IF NOT EXISTS `xg`.`bots` (
  `Guid` VARCHAR(37) CHARACTER SET 'utf8' COLLATE 'utf8_general_ci' NOT NULL ,
  `ParentGuid` VARCHAR(37) CHARACTER SET 'utf8' COLLATE 'utf8_general_ci' NOT NULL ,
  `Enabled` TINYINT(1) NULL DEFAULT 1 ,
  `Connected` TINYINT(1) NULL DEFAULT 0 ,
  `Name` VARCHAR(100) CHARACTER SET 'utf8' COLLATE 'utf8_general_ci' NOT NULL ,
  `State` INT(1) NULL DEFAULT 0 ,
  `InfoQueueCurrent` INT(4) NULL DEFAULT 0 ,
  `InfoQueueTotal` INT(4) NULL DEFAULT 0 ,
  `InfoSlotCurrent` INT(4) NULL DEFAULT 0 ,
  `InfoSlotTotal` INT(4) NULL DEFAULT 0 ,
  `InfoSpeedCurrent` BIGINT(15) NULL DEFAULT 0 ,
  `InfoSpeedMax` BIGINT(15) NULL DEFAULT 0 ,
  `LastContact` BIGINT(11) NULL DEFAULT 0 ,
  `LastMessage` VARCHAR(255) CHARACTER SET 'utf8' COLLATE 'utf8_general_ci' NULL ,
  `PacketCount` INT(5) NULL DEFAULT 0 ,
  PRIMARY KEY (`Guid`) ,
  INDEX `index2` (`Name` ASC) ,
  INDEX `fk_bot_1` (`ParentGuid` ASC) ,
  CONSTRAINT `fk_bot_1`
    FOREIGN KEY (`ParentGuid` )
    REFERENCES `xg`.`channels` (`Guid` )
    ON DELETE CASCADE
    ON UPDATE NO ACTION)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8
COLLATE = utf8_general_ci;

CREATE  TABLE IF NOT EXISTS `xg`.`packets` (
  `Guid` VARCHAR(37) CHARACTER SET 'utf8' COLLATE 'utf8_general_ci' NOT NULL ,
  `ParentGuid` VARCHAR(37) CHARACTER SET 'utf8' COLLATE 'utf8_general_ci' NOT NULL ,
  `Enabled` TINYINT(1) NULL DEFAULT 1 ,
  `Connected` TINYINT(1) NULL DEFAULT 0 ,
  `Name` VARCHAR(100) CHARACTER SET 'utf8' COLLATE 'utf8_general_ci' NOT NULL ,
  `LastUpdated` BIGINT(11) NULL DEFAULT 0 ,
  `LastMentioned` BIGINT(11) NULL DEFAULT 0 ,
  `Id` INT(4) NOT NULL ,
  `Size` BIGINT(11) NOT NULL ,
  PRIMARY KEY (`Guid`) ,
  INDEX `index2` (`Name` ASC) ,
  INDEX `fk_packet_1` (`ParentGuid` ASC) ,
  CONSTRAINT `fk_packet_1`
    FOREIGN KEY (`ParentGuid` )
    REFERENCES `xg`.`bots` (`Guid` )
    ON DELETE CASCADE
    ON UPDATE NO ACTION)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8
COLLATE = utf8_general_ci;

CREATE  TABLE IF NOT EXISTS `xg`.`snapshots` (
  `Timestamp` BIGINT(11) UNSIGNED NOT NULL ,
  `Speed` BIGINT(15) UNSIGNED NULL DEFAULT 0 ,
  `Servers` BIGINT(15) UNSIGNED NULL DEFAULT 0 ,
  `ServersEnabled` BIGINT(15) UNSIGNED NULL DEFAULT 0 ,
  `ServersDisabled` BIGINT(15) UNSIGNED NULL DEFAULT 0 ,
  `ServersConnected` BIGINT(15) UNSIGNED NULL DEFAULT 0 ,
  `ServersDisconnected` BIGINT(15) UNSIGNED NULL DEFAULT 0 ,
  `Channels` BIGINT(15) UNSIGNED NULL DEFAULT 0 ,
  `ChannelsEnabled` BIGINT(15) UNSIGNED NULL DEFAULT 0 ,
  `ChannelsDisabled` BIGINT(15) UNSIGNED NULL DEFAULT 0 ,
  `ChannelsConnected` BIGINT(15) UNSIGNED NULL DEFAULT 0 ,
  `ChannelsDisconnected` BIGINT(15) UNSIGNED NULL DEFAULT 0 ,
  `Bots` BIGINT(15) UNSIGNED NULL DEFAULT 0 ,
  `BotsConnected` BIGINT(15) UNSIGNED NULL DEFAULT 0 ,
  `BotsDisconnected` BIGINT(15) UNSIGNED NULL DEFAULT 0 ,
  `BotsFreeSlots` BIGINT(15) UNSIGNED NULL DEFAULT 0 ,
  `BotsFreeQueue` BIGINT(15) UNSIGNED NULL DEFAULT 0 ,
  `BotsAverageCurrentSpeed` BIGINT(15) UNSIGNED NULL DEFAULT 0 ,
  `BotsAverageMaxSpeed` BIGINT(15) UNSIGNED NULL DEFAULT 0 ,
  `Packets` BIGINT(15) UNSIGNED NULL DEFAULT 0 ,
  `PacketsConnected` BIGINT(15) UNSIGNED NULL DEFAULT 0 ,
  `PacketsDisconnected` BIGINT(15) UNSIGNED NULL DEFAULT 0 ,
  `PacketsSize` BIGINT(15) UNSIGNED NULL DEFAULT 0 ,
  `PacketsSizeConnected` BIGINT(15) UNSIGNED NULL DEFAULT 0 ,
  `PacketsSizeDisconnected` BIGINT(15) UNSIGNED NULL DEFAULT 0 ,
  PRIMARY KEY (`Timestamp`) )
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8
COLLATE = utf8_general_ci;

DELIMITER |

CREATE TRIGGER insert_packet AFTER INSERT ON `xg`.`packets` FOR EACH ROW
BEGIN
	UPDATE `xg`.`bots` SET `PacketCount` = (SELECT COUNT(`Guid`) FROM `xg`.`packets` WHERE `ParentGuid` = NEW.`ParentGuid`) WHERE `Guid` = NEW.`ParentGuid`;
END;
|

CREATE TRIGGER delete_packet AFTER DELETE ON `xg`.`packets` FOR EACH ROW
BEGIN
	UPDATE `xg`.`bots` SET `PacketCount` = (SELECT COUNT(`Guid`) FROM `xg`.`packets` WHERE `ParentGuid` = OLD.`ParentGuid`) WHERE `Guid` = OLD.`ParentGuid`;
END;
|

CREATE TRIGGER insert_bot AFTER INSERT ON `xg`.`bots` FOR EACH ROW
BEGIN
	UPDATE `xg`.`channels` SET `BotCount` = (SELECT COUNT(`Guid`) FROM `xg`.`bots` WHERE `ParentGuid` = NEW.`ParentGuid`) WHERE `Guid` = NEW.`ParentGuid`;
END;
|

CREATE TRIGGER delete_bot AFTER DELETE ON `xg`.`bots` FOR EACH ROW
BEGIN
	UPDATE `xg`.`channels` SET `BotCount` = (SELECT COUNT(`Guid`) FROM `xg`.`bots` WHERE `ParentGuid` = OLD.`ParentGuid`) WHERE `Guid` = OLD.`ParentGuid`;
END;
|

CREATE TRIGGER update_bot AFTER UPDATE ON `xg`.`bots` FOR EACH ROW
BEGIN
	IF NEW.`PacketCount` != OLD.`PacketCount` THEN
		UPDATE `xg`.`channels` SET `PacketCount` = (SELECT SUM(`PacketCount`) FROM `xg`.`bots` WHERE `ParentGuid` = NEW.`ParentGuid`) WHERE `Guid` = NEW.`ParentGuid`;
	END IF;
END;
|

CREATE TRIGGER insert_channel AFTER INSERT ON `xg`.`channels` FOR EACH ROW
BEGIN
	UPDATE `xg`.`servers` SET `ChannelCount` = (SELECT COUNT(`Guid`) FROM `xg`.`channels` WHERE `ParentGuid` = NEW.`ParentGuid`) WHERE `Guid` = NEW.`ParentGuid`;
END;
|

CREATE TRIGGER delete_channel AFTER DELETE ON `xg`.`channels` FOR EACH ROW
BEGIN
	UPDATE `xg`.`servers` SET `ChannelCount` = (SELECT COUNT(`Guid`) FROM `xg`.`channels` WHERE `ParentGuid` = OLD.`ParentGuid`) WHERE `Guid` = OLD.`ParentGuid`;
END;
|

CREATE TRIGGER update_channel AFTER UPDATE ON `xg`.`channels` FOR EACH ROW
BEGIN
	IF NEW.`PacketCount` != OLD.`PacketCount` OR NEW.`BotCount` != OLD.`BotCount` THEN
		UPDATE `xg`.`servers` SET `PacketCount` = (SELECT SUM(`PacketCount`) FROM `xg`.`channels` WHERE `ParentGuid` = NEW.`ParentGuid`), `BotCount` = (SELECT SUM(`BotCount`) FROM `xg`.`channels` WHERE `ParentGuid` = NEW.`ParentGuid`) WHERE `Guid` = NEW.`ParentGuid`;
	END IF;
END;
|
