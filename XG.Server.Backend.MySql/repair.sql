UPDATE
    `xg`.`bot` orign,
    (
        SELECT b.`Guid` AS `Guid`, count(p.`Guid`) AS `PacketCount`
        FROM `xg`.`bot` b
        LEFT JOIN `xg`.`packet` p ON p.`ParentGuid` = b.`Guid` GROUP BY b.`Guid`
    ) AS sub
SET
    orign.`PacketCount` = sub.`PacketCount`
WHERE
    orign.`Guid` = sub.`Guid`;

UPDATE
    `xg`.`channel` orign,
    (
        SELECT c.`Guid` AS `Guid`, c.name, IFNULL(sum(b.`PacketCount`), 0) AS `PacketCount`, count(b.`Guid`) AS `BotCount`
        FROM `xg`.`channel` c
        LEFT JOIN `xg`.`bot` b ON b.`ParentGuid` = c.`Guid` GROUP BY c.`Guid`
    ) AS sub
SET
    orign.`PacketCount` = sub.`PacketCount`, orign.`BotCount` = sub.`BotCount`
WHERE
    orign.`Guid` = sub.`Guid`;

UPDATE
    `xg`.`server` orign,
    (
        SELECT s.`Guid` AS `Guid`, IFNULL(sum(c.`PacketCount`), 0) AS `PacketCount`, IFNULL(sum(c.`BotCount`), 0) AS `BotCount`, count(c.`Guid`) AS `ChannelCount`
        FROM `xg`.`server` s
        LEFT JOIN `xg`.`channel` c ON c.`ParentGuid` = s.`Guid` GROUP BY s.`Guid`
    ) AS sub
SET
    orign.`PacketCount` = sub.`PacketCount`, orign.`BotCount` = sub.`BotCount`, orign.`ChannelCount` = sub.`ChannelCount`
WHERE
    orign.`Guid` = sub.`Guid`;
