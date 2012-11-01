UPDATE
    `xg`.`bots` orign,
    (
        SELECT b.`Guid` AS `Guid`, count(p.`Guid`) AS `PacketCount`
        FROM `xg`.`bots` b
        LEFT JOIN `xg`.`packets` p ON p.`ParentGuid` = b.`Guid` GROUP BY b.`Guid`
    ) AS sub
SET
    orign.`PacketCount` = sub.`PacketCount`
WHERE
    orign.`Guid` = sub.`Guid`;

UPDATE
    `xg`.`channels` orign,
    (
        SELECT c.`Guid` AS `Guid`, c.name, IFNULL(sum(b.`PacketCount`), 0) AS `PacketCount`, count(b.`Guid`) AS `BotCount`
        FROM `xg`.`channels` c
        LEFT JOIN `xg`.`bots` b ON b.`ParentGuid` = c.`Guid` GROUP BY c.`Guid`
    ) AS sub
SET
    orign.`PacketCount` = sub.`PacketCount`, orign.`BotCount` = sub.`BotCount`
WHERE
    orign.`Guid` = sub.`Guid`;

UPDATE
    `xg`.`servers` orign,
    (
        SELECT s.`Guid` AS `Guid`, IFNULL(sum(c.`PacketCount`), 0) AS `PacketCount`, IFNULL(sum(c.`BotCount`), 0) AS `BotCount`, count(c.`Guid`) AS `ChannelCount`
        FROM `xg`.`servers` s
        LEFT JOIN `xg`.`channels` c ON c.`ParentGuid` = s.`Guid` GROUP BY s.`Guid`
    ) AS sub
SET
    orign.`PacketCount` = sub.`PacketCount`, orign.`BotCount` = sub.`BotCount`, orign.`ChannelCount` = sub.`ChannelCount`
WHERE
    orign.`Guid` = sub.`Guid`;
