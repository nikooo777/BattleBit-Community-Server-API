CREATE DATABASE IF NOT EXISTS battlebit CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS player
(
    id            INT AUTO_INCREMENT PRIMARY KEY,
    steam_id      BIGINT UNIQUE NOT NULL,
    is_banned     BOOLEAN       NOT NULL,
    roles         INT,
    achievements  BLOB,
    selections    BLOB,
    tool_progress BLOB,
    created_at    TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at    TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;


CREATE TABLE IF NOT EXISTS chat_logs
(
    id        INT AUTO_INCREMENT PRIMARY KEY,
    message   TEXT COLLATE utf8mb4_unicode_ci NOT NULL,
    player_id INT                             NOT NULL,
    timestamp DATETIME                        NOT NULL,
    FOREIGN KEY (player_id) REFERENCES player (id) ON DELETE CASCADE ON UPDATE CASCADE
) CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

CREATE TABLE `player_progress`
(
    `id`                   int       NOT NULL AUTO_INCREMENT,
    `player_id`            int                DEFAULT NULL,
    `kill_count`           int unsigned       DEFAULT '0',
    `death_count`          int unsigned       DEFAULT '0',
    `leader_kills`         int unsigned       DEFAULT '0',
    `assault_kills`        int unsigned       DEFAULT '0',
    `medic_kills`          int unsigned       DEFAULT '0',
    `engineer_kills`       int unsigned       DEFAULT '0',
    `support_kills`        int unsigned       DEFAULT '0',
    `recon_kills`          int unsigned       DEFAULT '0',
    `win_count`            int unsigned       DEFAULT '0',
    `lose_count`           int unsigned       DEFAULT '0',
    `friendly_shots`       int unsigned       DEFAULT '0',
    `friendly_kills`       int unsigned       DEFAULT '0',
    `revived`              int unsigned       DEFAULT '0',
    `revived_team_mates`   int unsigned       DEFAULT '0',
    `assists`              int unsigned       DEFAULT '0',
    `prestige`             int unsigned       DEFAULT '0',
    `current_rank`         int unsigned       DEFAULT '0',
    `exp`                  int unsigned       DEFAULT '0',
    `shots_fired`          int unsigned       DEFAULT '0',
    `shots_hit`            int unsigned       DEFAULT '0',
    `headshots`            int unsigned       DEFAULT '0',
    `completed_objectives` int unsigned       DEFAULT '0',
    `healed_hps`           int unsigned       DEFAULT '0',
    `road_kills`           int unsigned       DEFAULT '0',
    `suicides`             int unsigned       DEFAULT '0',
    `vehicles_destroyed`   int unsigned       DEFAULT '0',
    `vehicle_hp_repaired`  int unsigned       DEFAULT '0',
    `longest_kill`         int unsigned       DEFAULT '0',
    `play_time_seconds`    int unsigned       DEFAULT '0',
    `leader_play_time`     int unsigned       DEFAULT '0',
    `assault_play_time`    int unsigned       DEFAULT '0',
    `medic_play_time`      int unsigned       DEFAULT '0',
    `engineer_play_time`   int unsigned       DEFAULT '0',
    `support_play_time`    int unsigned       DEFAULT '0',
    `recon_play_time`      int unsigned       DEFAULT '0',
    `leader_score`         int unsigned       DEFAULT '0',
    `assault_score`        int unsigned       DEFAULT '0',
    `medic_score`          int unsigned       DEFAULT '0',
    `engineer_score`       int unsigned       DEFAULT '0',
    `support_score`        int unsigned       DEFAULT '0',
    `recon_score`          int unsigned       DEFAULT '0',
    `total_score`          int unsigned       DEFAULT '0',
    `created_at`           timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`           timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `player_progress_player_uniq` (`player_id`),
    CONSTRAINT `player_progress_ibfk_1` FOREIGN KEY (`player_id`) REFERENCES `player` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS admins
(
    id       INT AUTO_INCREMENT PRIMARY KEY,
    name     VARCHAR(255) NOT NULL,
    steam_id BIGINT       NOT NULL UNIQUE,
    immunity INT          NOT NULL,
    flags    VARCHAR(255) NOT NULL
) CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS blocks
(
    id              INT AUTO_INCREMENT PRIMARY KEY,
    steam_id        BIGINT                      NOT NULL,
    block_type      ENUM ('BAN', 'GAG', 'MUTE') NOT NULL,
    reason          VARCHAR(255)                NOT NULL,
    expiry_date     DATETIME                    NOT NULL,
    issuer_admin_id INT                         NOT NULL,
    target_ip       VARCHAR(45) DEFAULT NULL,
    admin_ip        VARCHAR(45) DEFAULT NULL,
    FOREIGN KEY (issuer_admin_id) REFERENCES admins (id)
) CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

CREATE TABLE `player_reports`
(
    `id`                 int                                                                           NOT NULL AUTO_INCREMENT,
    `reporter_id`        int DEFAULT NULL,
    `reported_player_id` int                                                                           NOT NULL,
    `reason`             text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci                         NOT NULL,
    `timestamp`          datetime                                                                      NOT NULL,
    `status`             enum ('Pending','Reviewed','Resolved','Dismissed') COLLATE utf8mb4_unicode_ci NOT NULL,
    `admin_notes`        text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    PRIMARY KEY (`id`),
    KEY `player_reports_ibfk_1` (`reporter_id`),
    KEY `player_reports_ibfk_2` (`reported_player_id`),
    CONSTRAINT `player_reports_ibfk_1` FOREIGN KEY (`reporter_id`) REFERENCES `player` (`id`) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT `player_reports_ibfk_2` FOREIGN KEY (`reported_player_id`) REFERENCES `player` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci