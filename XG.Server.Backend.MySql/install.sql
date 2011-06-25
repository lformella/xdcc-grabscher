
CREATE  TABLE IF NOT EXISTS `xg`.`server` (
  `Guid` VARCHAR(37) CHARACTER SET 'utf8' COLLATE 'utf8_general_ci' NOT NULL ,
  `Enabled` TINYINT(1) NULL DEFAULT 1 ,
  `Connected` TINYINT(1) NULL DEFAULT 0 ,
  `Name` VARCHAR(100) CHARACTER SET 'utf8' COLLATE 'utf8_general_ci' NOT NULL ,
  `LastModified` BIGINT(11) NULL DEFAULT 0 ,
  `Port` INT(5) NOT NULL ,
  `ErrorCode` INT(5) NULL DEFAULT 0 ,
  `ChannelCount` INT(5) NULL DEFAULT 0 ,
  `BotCount` INT(5) NULL ,
  `PacketCount` INT(5) NULL DEFAULT 0 ,
  PRIMARY KEY (`Guid`) ,
  INDEX `index2` (`Name` ASC) )
ENGINE = MEMORY
DEFAULT CHARACTER SET = utf8
COLLATE = utf8_general_ci;

CREATE  TABLE IF NOT EXISTS `xg`.`channel` (
  `Guid` VARCHAR(37) CHARACTER SET 'utf8' COLLATE 'utf8_general_ci' NOT NULL ,
  `ParentGuid` VARCHAR(37) CHARACTER SET 'utf8' COLLATE 'utf8_general_ci' NOT NULL ,
  `Enabled` TINYINT(1) NULL DEFAULT 1 ,
  `Connected` TINYINT(1) NULL DEFAULT 0 ,
  `Name` VARCHAR(100) CHARACTER SET 'utf8' COLLATE 'utf8_general_ci' NOT NULL ,
  `LastModified` BIGINT(11) NULL DEFAULT 0 ,
  `ErrorCode` INT(3) NULL DEFAULT 0 ,
  `BotCount` INT(5) NULL DEFAULT 0 ,
  `PacketCount` INT(5) NULL DEFAULT 0 ,
  PRIMARY KEY (`Guid`) ,
  INDEX `index2` (`Name` ASC) ,
  INDEX `fk_channel_1` (`ParentGuid` ASC) ,
  CONSTRAINT `fk_channel_1`
    FOREIGN KEY (`ParentGuid` )
    REFERENCES `xg`.`server` (`Guid` )
    ON DELETE CASCADE
    ON UPDATE NO ACTION)
ENGINE = MEMORY
DEFAULT CHARACTER SET = utf8
COLLATE = utf8_general_ci;

CREATE  TABLE IF NOT EXISTS `xg`.`bot` (
  `Guid` VARCHAR(37) CHARACTER SET 'utf8' COLLATE 'utf8_general_ci' NOT NULL ,
  `ParentGuid` VARCHAR(37) CHARACTER SET 'utf8' COLLATE 'utf8_general_ci' NOT NULL ,
  `Enabled` TINYINT(1) NULL DEFAULT 1 ,
  `Connected` TINYINT(1) NULL DEFAULT 0 ,
  `Name` VARCHAR(100) CHARACTER SET 'utf8' COLLATE 'utf8_general_ci' NOT NULL ,
  `LastModified` BIGINT(11) NULL DEFAULT 0 ,
  `BotState` INT(1) NULL DEFAULT 0 ,
  `InfoQueueCurrent` INT(4) NULL DEFAULT 0 ,
  `InfoQueueTotal` INT(4) NULL DEFAULT 0 ,
  `InfoSlotCurrent` INT(4) NULL DEFAULT 0 ,
  `InfoSlotTotal` INT(4) NULL DEFAULT 0 ,
  `InfoSpeedCurrent` DOUBLE NULL DEFAULT 0 ,
  `InfoSpeedMax` DOUBLE NULL DEFAULT 0 ,
  `LastContact` BIGINT(11) NULL DEFAULT 0 ,
  `LastMessage` VARCHAR(255) CHARACTER SET 'utf8' COLLATE 'utf8_general_ci' NULL ,
  `PacketCount` INT(5) NULL ,
  PRIMARY KEY (`Guid`) ,
  INDEX `index2` (`Name` ASC) ,
  INDEX `fk_bot_1` (`ParentGuid` ASC) ,
  CONSTRAINT `fk_bot_1`
    FOREIGN KEY (`ParentGuid` )
    REFERENCES `xg`.`channel` (`Guid` )
    ON DELETE CASCADE
    ON UPDATE NO ACTION)
ENGINE = MEMORY
DEFAULT CHARACTER SET = utf8
COLLATE = utf8_general_ci;

CREATE  TABLE IF NOT EXISTS `xg`.`packet` (
  `Guid` VARCHAR(37) CHARACTER SET 'utf8' COLLATE 'utf8_general_ci' NOT NULL ,
  `ParentGuid` VARCHAR(37) CHARACTER SET 'utf8' COLLATE 'utf8_general_ci' NOT NULL ,
  `Enabled` TINYINT(1) NULL DEFAULT 1 ,
  `Connected` TINYINT(1) NULL DEFAULT 0 ,
  `Name` VARCHAR(100) CHARACTER SET 'utf8' COLLATE 'utf8_general_ci' NOT NULL ,
  `LastModified` BIGINT(11) NULL DEFAULT 0 ,
  `LastUpdated` BIGINT(11) NULL DEFAULT 0 ,
  `LastMentioned` BIGINT(11) NULL DEFAULT 0 ,
  `Id` INT(4) NOT NULL ,
  `Size` BIGINT(11) NOT NULL ,
  PRIMARY KEY (`Guid`) ,
  INDEX `index2` (`Name` ASC) ,
  INDEX `fk_packet_1` (`ParentGuid` ASC) ,
  CONSTRAINT `fk_packet_1`
    FOREIGN KEY (`ParentGuid` )
    REFERENCES `xg`.`bot` (`Guid` )
    ON DELETE CASCADE
    ON UPDATE NO ACTION)
ENGINE = MEMORY
DEFAULT CHARACTER SET = utf8
COLLATE = utf8_general_ci;
